-- ============================================================================
-- WEBSTER ORGANISATION - ONLINE APTITUDE TEST & RECRUITMENT SYSTEM
-- Script 02: Triggers and ACID Stored Procedures (T-SQL, Microsoft SQL Server)
-- ============================================================================

USE WebsterAptitudeDb;
GO

-- ============================================================================
-- TRIGGER 1: trg_TransferAptiClearedCandidate
-- Automated candidate transfer into AptiClearedCandidates upon clearing assessment
-- ============================================================================
IF OBJECT_ID(N'dbo.trg_TransferAptiClearedCandidate', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_TransferAptiClearedCandidate;
GO

CREATE TRIGGER dbo.trg_TransferAptiClearedCandidate
ON dbo.TestAttempts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Trigger fires only when AttemptStatus transitions to 'Completed' AND IsPassed = 1
    IF UPDATE(AttemptStatus) OR UPDATE(IsPassed)
    BEGIN
        INSERT INTO dbo.AptiClearedCandidates (
            CandidateId,
            AttemptId,
            OverallScore,
            PercentageScore,
            ClearanceDate,
            HrInterviewStatus,
            HrRemarks
        )
        SELECT 
            i.CandidateId,
            i.AttemptId,
            i.TotalScoreObtained,
            i.PercentageScore,
            SYSUTCDATETIME(),
            'Pending',
            N'Automated transfer: Candidate cleared aptitude test with ' + 
            CAST(i.PercentageScore AS NVARCHAR(10)) + N'% aggregate. Next stage: HR Round.'
        FROM inserted i
        INNER JOIN deleted d ON i.AttemptId = d.AttemptId
        WHERE i.AttemptStatus = 'Completed'
          AND i.IsPassed = 1
          AND (d.AttemptStatus <> 'Completed' OR d.IsPassed = 0)
          AND NOT EXISTS (
              SELECT 1 FROM dbo.AptiClearedCandidates acc 
              WHERE acc.CandidateId = i.CandidateId
          );

        -- Record security audit entry
        INSERT INTO dbo.AssessmentAuditLogs (CandidateId, ActionType, SectionName, Details, LoggedAt)
        SELECT 
            i.CandidateId,
            'HR_PIPELINE_TRANSFER',
            'Final_Round',
            N'Candidate automatically transferred to AptiClearedCandidates table. Message: You have cleared this round, next round would be HR Round.',
            SYSUTCDATETIME()
        FROM inserted i
        INNER JOIN deleted d ON i.AttemptId = d.AttemptId
        WHERE i.AttemptStatus = 'Completed' AND i.IsPassed = 1;
    END
END;
GO

-- ============================================================================
-- TRIGGER 2: trg_EnforceLinearSectionFlow
-- Enforces linear section gating (GK -> Mathematics -> Computer_Technology)
-- Blocks stage skipping, backward regression, or post-completion modifications
-- ============================================================================
IF OBJECT_ID(N'dbo.trg_EnforceLinearSectionFlow', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_EnforceLinearSectionFlow;
GO

CREATE TRIGGER dbo.trg_EnforceLinearSectionFlow
ON dbo.TestAttempts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Check 1: Modification after completion is strictly prohibited
    IF EXISTS (
        SELECT 1 
        FROM deleted d
        INNER JOIN inserted i ON d.AttemptId = i.AttemptId
        WHERE d.AttemptStatus = 'Completed'
          AND (i.AttemptStatus <> 'Completed' OR i.CurrentSectionId <> d.CurrentSectionId)
    )
    BEGIN
        RAISERROR (N'ACID Security Violation: Assessment has already been Completed and locked. Further stage transitions are forbidden.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Check 2: Linear Stage Progression & Anti-Regression Guard
    IF UPDATE(CurrentSectionId)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM inserted i
            INNER JOIN deleted d ON i.AttemptId = d.AttemptId
            INNER JOIN dbo.TestSections s_new ON i.CurrentSectionId = s_new.SectionId
            INNER JOIN dbo.TestSections s_old ON d.CurrentSectionId = s_old.SectionId
            WHERE s_new.SequenceOrder < s_old.SequenceOrder -- Backward navigation attempt
               OR s_new.SequenceOrder > (s_old.SequenceOrder + 1) -- Stage skipping attempt
        )
        BEGIN
            RAISERROR (N'Linearity Violation: Candidates must sequentially complete sections (General Knowledge -> Mathematics -> Computer Technology). Backward navigation or stage skipping is blocked.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
END;
GO

-- ============================================================================
-- PROCEDURE 1: sp_SubmitSectionAnswers
-- Submits responses for a test section, validates server timer, calculates marks,
-- and advances linear progression pointer.
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_SubmitSectionAnswers', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SubmitSectionAnswers;
GO

CREATE PROCEDURE dbo.sp_SubmitSectionAnswers
    @AttemptId INT,
    @SubmittedSectionId INT,
    @ResponsesJson NVARCHAR(MAX) -- JSON Array: [{"QuestionId":1, "SelectedOptionId":3}, ...]
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @CandidateId INT, @CurrentSectionId INT, @Status NVARCHAR(30);
        DECLARE @SectionStartedAt DATETIME2(3), @TimeLimitMinutes INT, @ElapsedSeconds INT;
        DECLARE @CurrentSequence INT, @NextSectionId INT, @NextSectionName NVARCHAR(50);

        -- 1. Validate Attempt and fetch current state
        SELECT 
            @CandidateId = a.CandidateId,
            @CurrentSectionId = a.CurrentSectionId,
            @Status = a.AttemptStatus,
            @SectionStartedAt = a.SectionStartedAt,
            @TimeLimitMinutes = s.TimeLimitMinutes,
            @CurrentSequence = s.SequenceOrder
        FROM dbo.TestAttempts a WITH (UPDLOCK, ROWLOCK)
        INNER JOIN dbo.TestSections s ON a.CurrentSectionId = s.SectionId
        WHERE a.AttemptId = @AttemptId;

        IF @CandidateId IS NULL
        BEGIN
            RAISERROR(N'Invalid Assessment Attempt ID.', 16, 1);
        END

        IF @Status <> 'In_Progress'
        BEGIN
            RAISERROR(N'Assessment is not currently active (Status: %s).', 16, 1, @Status);
        END

        IF @CurrentSectionId <> @SubmittedSectionId
        BEGIN
            RAISERROR(N'Section submission mismatch. Expected SectionId %d, received %d.', 16, 1, @CurrentSectionId, @SubmittedSectionId);
        END

        -- 2. Validate Server-side Countdown Timer (with 15s grace buffer for network latency)
        SET @ElapsedSeconds = DATEDIFF(SECOND, @SectionStartedAt, SYSUTCDATETIME());
        IF @ElapsedSeconds > (@TimeLimitMinutes * 60 + 15)
        BEGIN
            -- Record timeout violation in audit
            INSERT INTO dbo.AssessmentAuditLogs (CandidateId, ActionType, SectionName, Details, LoggedAt)
            VALUES (@CandidateId, 'SECTION_TIMEOUT_OVERRUN', CAST(@SubmittedSectionId AS NVARCHAR(10)), 
                    N'Candidate exceeded allotted time limit of ' + CAST(@TimeLimitMinutes AS NVARCHAR(10)) + N' minutes. Elapsed: ' + CAST(@ElapsedSeconds AS NVARCHAR(10)) + N's.', SYSUTCDATETIME());
            
            -- Automatically mark as Timed_Out if overrun exceeds 2 minutes
            IF @ElapsedSeconds > (@TimeLimitMinutes * 60 + 120)
            BEGIN
                UPDATE dbo.TestAttempts
                SET AttemptStatus = 'Timed_Out',
                    CompletedAt = SYSUTCDATETIME()
                WHERE AttemptId = @AttemptId;

                RAISERROR(N'Time expired. The section duration has exceeded allowable limit.', 16, 1);
            END
        END

        -- 3. Ingest Responses and Grade Against QuestionOptions Key
        MERGE dbo.CandidateResponses AS target
        USING (
            SELECT 
                r.QuestionId,
                r.SelectedOptionId,
                CASE 
                    WHEN opt.IsCorrect = 1 THEN q.Marks
                    ELSE 0.00
                END AS MarksAwarded
            FROM OPENJSON(@ResponsesJson)
            WITH (
                QuestionId INT '$.QuestionId',
                SelectedOptionId INT '$.SelectedOptionId'
            ) AS r
            INNER JOIN dbo.Questions q ON r.QuestionId = q.QuestionId
            LEFT JOIN dbo.QuestionOptions opt ON r.SelectedOptionId = opt.OptionId AND opt.QuestionId = q.QuestionId
            WHERE q.SectionId = @SubmittedSectionId
        ) AS source
        ON (target.AttemptId = @AttemptId AND target.QuestionId = source.QuestionId)
        WHEN MATCHED THEN
            UPDATE SET 
                target.SelectedOptionId = source.SelectedOptionId,
                target.MarksAwarded = source.MarksAwarded,
                target.AnsweredAt = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT (AttemptId, QuestionId, SelectedOptionId, MarksAwarded, AnsweredAt)
            VALUES (@AttemptId, source.QuestionId, source.SelectedOptionId, source.MarksAwarded, SYSUTCDATETIME());

        -- 4. Advance to Next Linear Section or Flag for Finalization
        SELECT TOP 1 
            @NextSectionId = SectionId,
            @NextSectionName = SectionName
        FROM dbo.TestSections
        WHERE SequenceOrder = @CurrentSequence + 1
        ORDER BY SequenceOrder ASC;

        IF @NextSectionId IS NOT NULL
        BEGIN
            -- Advance to the next round
            UPDATE dbo.TestAttempts
            SET CurrentSectionId = @NextSectionId,
                SectionStartedAt = SYSUTCDATETIME()
            WHERE AttemptId = @AttemptId;

            INSERT INTO dbo.AssessmentAuditLogs (CandidateId, ActionType, SectionName, Details, LoggedAt)
            VALUES (@CandidateId, 'SECTION_TRANSITION', @NextSectionName, 
                    N'Candidate successfully submitted Section Sequence ' + CAST(@CurrentSequence AS NVARCHAR(10)) + N' and advanced to ' + @NextSectionName, SYSUTCDATETIME());

            COMMIT TRANSACTION;

            SELECT 
                @AttemptId AS AttemptId,
                'TRANSITIONED_TO_NEXT_SECTION' AS ResultStatus,
                @NextSectionId AS NextSectionId,
                @NextSectionName AS NextSectionName,
                N'Section submitted successfully. Proceeding to next round.' AS Message;
        END
        ELSE
        BEGIN
            -- Final round submitted -> Ready for Final Scoring
            COMMIT TRANSACTION;

            SELECT 
                @AttemptId AS AttemptId,
                'ASSESSMENT_COMPLETED_PENDING_FINALIZATION' AS ResultStatus,
                NULL AS NextSectionId,
                NULL AS NextSectionName,
                N'Final section submitted. Finalizing cumulative assessment score...' AS Message;
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- PROCEDURE 2: sp_FinalizeAssessmentAndScore
-- Computes cumulative aggregate score, evaluates pass/fail status against cutoff,
-- locks the exam, and outputs qualification prompt.
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_FinalizeAssessmentAndScore', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_FinalizeAssessmentAndScore;
GO

CREATE PROCEDURE dbo.sp_FinalizeAssessmentAndScore
    @AttemptId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @CandidateId INT, @Status NVARCHAR(30);
        DECLARE @TotalObtained DECIMAL(6,2), @MaxScore DECIMAL(6,2), @Percentage DECIMAL(5,2);
        DECLARE @CutoffPercentage DECIMAL(5,2) = 60.00; -- 60% minimum recruitment aggregate
        DECLARE @IsPassed BIT = 0;
        DECLARE @CandidateMessage NVARCHAR(250);

        SELECT 
            @CandidateId = CandidateId,
            @Status = AttemptStatus
        FROM dbo.TestAttempts WITH (UPDLOCK, ROWLOCK)
        WHERE AttemptId = @AttemptId;

        IF @CandidateId IS NULL
        BEGIN
            RAISERROR(N'Assessment Attempt record not found.', 16, 1);
        END

        IF @Status = 'Completed'
        BEGIN
            -- Already completed, return existing result
            SELECT 
                AttemptId,
                CandidateId,
                TotalScoreObtained,
                MaxPossibleScore,
                PercentageScore,
                IsPassed,
                AttemptStatus,
                CASE 
                    WHEN IsPassed = 1 THEN N'You have cleared this round, next round would be HR Round'
                    ELSE N'Assessment completed. Unfortunately, you did not meet the required cutoff threshold.'
                END AS VerdictMessage
            FROM dbo.TestAttempts
            WHERE AttemptId = @AttemptId;

            COMMIT TRANSACTION;
            RETURN;
        END

        -- Calculate cumulative marks obtained from CandidateResponses
        SELECT 
            @TotalObtained = ISNULL(SUM(r.MarksAwarded), 0.00)
        FROM dbo.CandidateResponses r
        WHERE r.AttemptId = @AttemptId;

        -- Calculate total possible maximum marks across all questions in the 3 sections
        SELECT 
            @MaxScore = ISNULL(SUM(q.Marks), 15.00)
        FROM dbo.Questions q
        INNER JOIN dbo.TestSections s ON q.SectionId = s.SectionId
        WHERE q.IsActive = 1;

        IF @MaxScore = 0 SET @MaxScore = 15.00;

        SET @Percentage = CAST((@TotalObtained / @MaxScore) * 100.0 AS DECIMAL(5,2));

        IF @Percentage >= @CutoffPercentage
        BEGIN
            SET @IsPassed = 1;
            SET @CandidateMessage = N'You have cleared this round, next round would be HR Round';
        END
        ELSE
        BEGIN
            SET @IsPassed = 0;
            SET @CandidateMessage = N'Assessment completed. Unfortunately, you did not meet the required cutoff threshold.';
        END

        -- Atomically update attempt status and final scores
        UPDATE dbo.TestAttempts
        SET TotalScoreObtained = @TotalObtained,
            MaxPossibleScore = @MaxScore,
            PercentageScore = @Percentage,
            IsPassed = @IsPassed,
            AttemptStatus = 'Completed',
            CompletedAt = SYSUTCDATETIME()
        WHERE AttemptId = @AttemptId;

        -- trg_TransferAptiClearedCandidate automatically fires on this UPDATE if @IsPassed = 1

        -- Log final completion in audit trail
        INSERT INTO dbo.AssessmentAuditLogs (CandidateId, ActionType, SectionName, Details, LoggedAt)
        VALUES (@CandidateId, 'ASSESSMENT_FINALIZED', 'Overall', 
                N'Assessment finalized. Score: ' + CAST(@TotalObtained AS NVARCHAR(10)) + N'/' + CAST(@MaxScore AS NVARCHAR(10)) + 
                N' (' + CAST(@Percentage AS NVARCHAR(10)) + N'%). Result: ' + CASE WHEN @IsPassed = 1 THEN N'PASSED' ELSE N'FAILED' END, 
                SYSUTCDATETIME());

        COMMIT TRANSACTION;

        -- Return payload to client
        SELECT 
            @AttemptId AS AttemptId,
            @CandidateId AS CandidateId,
            @TotalObtained AS TotalScoreObtained,
            @MaxScore AS MaxPossibleScore,
            @Percentage AS PercentageScore,
            @IsPassed AS IsPassed,
            'Completed' AS AttemptStatus,
            @CandidateMessage AS VerdictMessage;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- PROCEDURE 3: sp_GetRecruitmentSummaryReport
-- Aggregates recruitment funnel statistics filtered by DAILY, WEEKLY, or MONTHLY
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_GetRecruitmentSummaryReport', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetRecruitmentSummaryReport;
GO

CREATE PROCEDURE dbo.sp_GetRecruitmentSummaryReport
    @FilterType NVARCHAR(10) = 'WEEKLY' -- 'DAILY', 'WEEKLY', 'MONTHLY'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATETIME2(3);
    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

    IF UPPER(@FilterType) = 'DAILY'
        SET @StartDate = DATEADD(DAY, -1, @Now);
    ELSE IF UPPER(@FilterType) = 'WEEKLY'
        SET @StartDate = DATEADD(DAY, -7, @Now);
    ELSE IF UPPER(@FilterType) = 'MONTHLY'
        SET @StartDate = DATEADD(MONTH, -1, @Now);
    ELSE
        SET @StartDate = DATEADD(DAY, -7, @Now);

    SELECT 
        UPPER(@FilterType) AS ReportFilter,
        @StartDate AS PeriodStartUtc,
        @Now AS PeriodEndUtc,
        
        -- Funnel Metrics
        COUNT(DISTINCT c.CandidateId) AS TotalEnrolledCandidates,
        COUNT(DISTINCT CASE WHEN a.AttemptStatus IN ('In_Progress', 'Completed', 'Timed_Out') THEN a.CandidateId END) AS TotalAppearedForAssessment,
        COUNT(DISTINCT CASE WHEN a.AttemptStatus = 'Completed' AND a.IsPassed = 1 THEN a.CandidateId END) AS TotalClearedAptitude,
        COUNT(DISTINCT CASE WHEN a.AttemptStatus = 'Completed' AND a.IsPassed = 0 THEN a.CandidateId END) AS TotalFailedAptitude,
        COUNT(DISTINCT acc.CandidateId) AS TotalTransferredToHr,
        
        -- Score Analytics
        ISNULL(AVG(CASE WHEN a.AttemptStatus = 'Completed' THEN a.PercentageScore END), 0.00) AS AverageCandidatePercentage,
        ISNULL(MAX(CASE WHEN a.AttemptStatus = 'Completed' THEN a.TotalScoreObtained END), 0.00) AS HighestScoreObtained,
        
        -- Funnel Conversion Ratios
        CAST(
            CASE 
                WHEN COUNT(DISTINCT c.CandidateId) = 0 THEN 0.00
                ELSE (CAST(COUNT(DISTINCT acc.CandidateId) AS DECIMAL(6,2)) / CAST(COUNT(DISTINCT c.CandidateId) AS DECIMAL(6,2))) * 100.0
            END AS DECIMAL(5,2)
        ) AS FunnelConversionRatePercentage
    FROM dbo.Candidates c
    LEFT JOIN dbo.TestAttempts a ON c.CandidateId = a.CandidateId
    LEFT JOIN dbo.AptiClearedCandidates acc ON c.CandidateId = acc.CandidateId
    WHERE c.EnrolledAt >= @StartDate;
END;
GO

PRINT 'Successfully created ACID triggers and stored procedures for Webster Organisation.';
GO
