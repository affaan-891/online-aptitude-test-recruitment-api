-- ============================================================================
-- WEBSTER ORGANISATION - ONLINE APTITUDE TEST & RECRUITMENT SYSTEM
-- Script 03: Analytical Views & 5 Academic Viva Defense Queries (T-SQL)
-- ============================================================================

USE WebsterAptitudeDb;
GO

-- ============================================================================
-- VIEW 1: vw_HrCandidatePipeline
-- Complete 360-degree applicant profile for HR evaluation round
-- Joins Candidates, Academics, Experience, TestScores, and HR clearance status
-- ============================================================================
IF OBJECT_ID(N'dbo.vw_HrCandidatePipeline', N'V') IS NOT NULL
    DROP VIEW dbo.vw_HrCandidatePipeline;
GO

CREATE VIEW dbo.vw_HrCandidatePipeline
AS
SELECT 
    acc.ClearanceId,
    c.CandidateId,
    c.RegistrationNumber,
    c.FullName,
    c.Email,
    c.Phone,
    c.DateOfBirth,
    DATEDIFF(YEAR, c.DateOfBirth, CAST(SYSUTCDATETIME() AS DATE)) AS AgeInYears,
    c.CurrentAddress,
    
    -- Top Academic Credential
    edu.DegreeName AS HighestDegree,
    edu.InstituteName AS UniversityOrInstitute,
    edu.PassingYear AS GraduationYear,
    edu.PercentageOrCGPA AS AcademicPercentage,
    edu.Specialization,

    -- Work Experience Summary
    ISNULL(exp.CompanyName, N'Fresh Graduate') AS PreviousEmployer,
    ISNULL(exp.Designation, N'Entry-Level Trainee') AS PriorDesignation,
    ISNULL(exp.TotalYears, 0.0) AS TotalExperienceYears,
    ISNULL(exp.KeySkills, N'Core Aptitude / Fundamentals') AS CoreSkills,

    -- Assessment Performance
    a.AttemptId,
    acc.OverallScore AS AptitudeScoreObtained,
    a.MaxPossibleScore AS AptitudeMaxMarks,
    acc.PercentageScore AS AptitudePercentage,
    acc.ClearanceDate AS ClearedOnUtc,
    
    -- HR Clearance Status
    acc.HrInterviewStatus,
    acc.HrRemarks,
    m.FullName AS EnrolledByManager,
    m.BranchLocation AS BranchOffice
FROM dbo.AptiClearedCandidates acc
INNER JOIN dbo.Candidates c ON acc.CandidateId = c.CandidateId
INNER JOIN dbo.TestAttempts a ON acc.AttemptId = a.AttemptId
INNER JOIN dbo.Managers m ON c.CreatedByManagerId = m.ManagerId
OUTER APPLY (
    SELECT TOP 1 
        e.DegreeName, e.InstituteName, e.PassingYear, e.PercentageOrCGPA, e.Specialization
    FROM dbo.CandidateEducations e
    WHERE e.CandidateId = c.CandidateId
    ORDER BY e.PassingYear DESC, e.PercentageOrCGPA DESC
) edu
OUTER APPLY (
    SELECT TOP 1 
        x.CompanyName, x.Designation, x.TotalYears, x.KeySkills
    FROM dbo.CandidateExperiences x
    WHERE x.CandidateId = c.CandidateId
    ORDER BY x.TotalYears DESC
) exp;
GO

-- ============================================================================
-- VIEW 2: vw_SectionalPerformanceBenchmark
-- Aggregates sectional performance across General Knowledge, Mathematics, 
-- and Computer Technology
-- ============================================================================
IF OBJECT_ID(N'dbo.vw_SectionalPerformanceBenchmark', N'V') IS NOT NULL
    DROP VIEW dbo.vw_SectionalPerformanceBenchmark;
GO

CREATE VIEW dbo.vw_SectionalPerformanceBenchmark
AS
SELECT 
    s.SectionId,
    s.SectionName,
    s.SequenceOrder,
    s.TimeLimitMinutes,
    COUNT(DISTINCT q.QuestionId) AS TotalQuestionsInBank,
    SUM(q.Marks) AS SectionTotalMarks,
    COUNT(DISTINCT r.ResponseId) AS TotalResponsesEvaluated,
    ISNULL(SUM(r.MarksAwarded), 0.00) AS CumulativeMarksAwarded,
    ISNULL(AVG(r.MarksAwarded), 0.00) AS AverageMarksPerQuestion,
    CAST(
        CASE 
            WHEN COUNT(DISTINCT r.ResponseId) = 0 THEN 0.00
            ELSE (CAST(SUM(CASE WHEN r.MarksAwarded > 0 THEN 1 ELSE 0 END) AS DECIMAL(10,2)) / CAST(COUNT(DISTINCT r.ResponseId) AS DECIMAL(10,2))) * 100.0
        END AS DECIMAL(5,2)
    ) AS QuestionAccuracyRatePercentage
FROM dbo.TestSections s
LEFT JOIN dbo.Questions q ON s.SectionId = q.SectionId AND q.IsActive = 1
LEFT JOIN dbo.CandidateResponses r ON q.QuestionId = r.QuestionId
GROUP BY 
    s.SectionId,
    s.SectionName,
    s.SequenceOrder,
    s.TimeLimitMinutes;
GO

-- ============================================================================
-- 5 ACADEMIC VIVA VOCE DEFENSE QUERIES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Viva Query 1: Anti-Join Pattern using NOT EXISTS vs LEFT JOIN / IS NULL
-- Objective: Identify all enrolled candidates who have not yet initiated their test,
-- optimizing query optimizer cardinality estimation.
-- ----------------------------------------------------------------------------
-- EXPLANATION FOR EXAMINER: NOT EXISTS produces an Anti-Semi-Join plan operator
-- that halts row evaluation upon the first match, avoiding full hash spooling.
SELECT 
    c.CandidateId,
    c.RegistrationNumber,
    c.FullName,
    c.Email,
    c.Phone,
    c.EnrolledAt,
    m.FullName AS RegisteredByManager
FROM dbo.Candidates c
INNER JOIN dbo.Managers m ON c.CreatedByManagerId = m.ManagerId
WHERE NOT EXISTS (
    SELECT 1 
    FROM dbo.TestAttempts a 
    WHERE a.CandidateId = c.CandidateId
)
ORDER BY c.EnrolledAt DESC;
GO

-- ----------------------------------------------------------------------------
-- Viva Query 2: Leaderboard Ranking using DENSE_RANK() Window Function
-- Objective: Rank candidates by cumulative assessment score, handling identical
-- scores without skipping rank sequence numbers (unlike RANK()).
-- ----------------------------------------------------------------------------
SELECT 
    DENSE_RANK() OVER (ORDER BY a.TotalScoreObtained DESC, a.PercentageScore DESC) AS MeritRank,
    c.RegistrationNumber,
    c.FullName,
    c.Email,
    a.TotalScoreObtained,
    a.MaxPossibleScore,
    a.PercentageScore,
    a.AttemptStatus,
    CASE 
        WHEN a.IsPassed = 1 THEN 'Qualified for HR'
        ELSE 'Disqualified'
    END AS QualificationStatus,
    a.CompletedAt
FROM dbo.TestAttempts a
INNER JOIN dbo.Candidates c ON a.CandidateId = c.CandidateId
WHERE a.AttemptStatus = 'Completed'
ORDER BY MeritRank ASC;
GO

-- ----------------------------------------------------------------------------
-- Viva Query 3: Assessment Duration & Section Speed Analysis
-- Objective: Compute exact test completion duration in minutes and compare against
-- the sum of configured section limits to detect anomaly speed or timeouts.
-- ----------------------------------------------------------------------------
SELECT 
    c.FullName,
    c.RegistrationNumber,
    a.StartedAt,
    a.CompletedAt,
    DATEDIFF(SECOND, a.StartedAt, a.CompletedAt) AS TotalDurationSeconds,
    CAST(DATEDIFF(SECOND, a.StartedAt, a.CompletedAt) / 60.0 AS DECIMAL(5,2)) AS TotalDurationMinutes,
    (SELECT SUM(TimeLimitMinutes) FROM dbo.TestSections) AS TotalPermittedMinutes,
    a.PercentageScore,
    a.AttemptStatus
FROM dbo.TestAttempts a
INNER JOIN dbo.Candidates c ON a.CandidateId = c.CandidateId
WHERE a.CompletedAt IS NOT NULL;
GO

-- ----------------------------------------------------------------------------
-- Viva Query 4: Correlation of Prior Work Experience with Aptitude Pass Rate
-- Objective: Group candidates by experience bracket and compute pass percentages.
-- Demonstrates multi-table sub-aggregation with CASE constructs.
-- ----------------------------------------------------------------------------
SELECT 
    CASE 
        WHEN ISNULL(exp.TotalYears, 0) = 0 THEN '0 Years (Freshers)'
        WHEN exp.TotalYears BETWEEN 0.1 AND 2.0 THEN '1-2 Years (Junior)'
        ELSE '3+ Years (Experienced)'
    END AS ExperienceBracket,
    COUNT(DISTINCT c.CandidateId) AS TotalCandidates,
    SUM(CASE WHEN a.IsPassed = 1 THEN 1 ELSE 0 END) AS PassedCandidates,
    CAST(
        (CAST(SUM(CASE WHEN a.IsPassed = 1 THEN 1 ELSE 0 END) AS DECIMAL(6,2)) / 
         CAST(COUNT(DISTINCT c.CandidateId) AS DECIMAL(6,2))) * 100.0 AS DECIMAL(5,2)
    ) AS PassPercentage
FROM dbo.Candidates c
LEFT JOIN dbo.CandidateExperiences exp ON c.CandidateId = exp.CandidateId
LEFT JOIN dbo.TestAttempts a ON c.CandidateId = a.CandidateId
GROUP BY 
    CASE 
        WHEN ISNULL(exp.TotalYears, 0) = 0 THEN '0 Years (Freshers)'
        WHEN exp.TotalYears BETWEEN 0.1 AND 2.0 THEN '1-2 Years (Junior)'
        ELSE '3+ Years (Experienced)'
    END
ORDER BY ExperienceBracket;
GO

-- ----------------------------------------------------------------------------
-- Viva Query 5: Targeted Question Difficulty Profiling (Hardest Questions)
-- Objective: Pinpoint questions with lowest candidate success rate to identify
-- potential syllabus flaws or high-discrimination items in the question bank.
-- ----------------------------------------------------------------------------
SELECT TOP 5
    s.SectionName,
    q.QuestionId,
    CAST(q.QuestionText AS NVARCHAR(120)) + '...' AS QuestionSummary,
    q.Marks AS MaxMarks,
    COUNT(r.ResponseId) AS TimesAttempted,
    SUM(CASE WHEN r.MarksAwarded > 0 THEN 1 ELSE 0 END) AS TimesAnsweredCorrectly,
    CAST(
        (CAST(SUM(CASE WHEN r.MarksAwarded > 0 THEN 1 ELSE 0 END) AS DECIMAL(6,2)) / 
         CAST(NULLIF(COUNT(r.ResponseId), 0) AS DECIMAL(6,2))) * 100.0 AS DECIMAL(5,2)
    ) AS SuccessRatePercentage
FROM dbo.Questions q
INNER JOIN dbo.TestSections s ON q.SectionId = s.SectionId
LEFT JOIN dbo.CandidateResponses r ON q.QuestionId = r.QuestionId
GROUP BY s.SectionName, q.QuestionId, CAST(q.QuestionText AS NVARCHAR(120)), q.Marks
HAVING COUNT(r.ResponseId) > 0
ORDER BY SuccessRatePercentage ASC;
GO

PRINT 'Successfully created analytical views and 5 academic viva queries.';
GO
