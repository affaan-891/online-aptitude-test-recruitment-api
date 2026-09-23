-- ============================================================================
-- WEBSTER ORGANISATION - ONLINE APTITUDE TEST & RECRUITMENT SYSTEM
-- Script 01: Relational Schema DDL (3NF Normalized, Microsoft SQL Server)
-- ============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'WebsterAptitudeDb')
BEGIN
    CREATE DATABASE WebsterAptitudeDb;
END
GO

USE WebsterAptitudeDb;
GO

-- Drop tables in reverse dependency order if executing a clean reset
IF OBJECT_ID(N'dbo.AssessmentAuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AssessmentAuditLogs;
IF OBJECT_ID(N'dbo.AptiClearedCandidates', N'U') IS NOT NULL DROP TABLE dbo.AptiClearedCandidates;
IF OBJECT_ID(N'dbo.CandidateResponses', N'U') IS NOT NULL DROP TABLE dbo.CandidateResponses;
IF OBJECT_ID(N'dbo.TestAttempts', N'U') IS NOT NULL DROP TABLE dbo.TestAttempts;
IF OBJECT_ID(N'dbo.QuestionOptions', N'U') IS NOT NULL DROP TABLE dbo.QuestionOptions;
IF OBJECT_ID(N'dbo.Questions', N'U') IS NOT NULL DROP TABLE dbo.Questions;
IF OBJECT_ID(N'dbo.TestSections', N'U') IS NOT NULL DROP TABLE dbo.TestSections;
IF OBJECT_ID(N'dbo.CandidateExperiences', N'U') IS NOT NULL DROP TABLE dbo.CandidateExperiences;
IF OBJECT_ID(N'dbo.CandidateEducations', N'U') IS NOT NULL DROP TABLE dbo.CandidateEducations;
IF OBJECT_ID(N'dbo.Candidates', N'U') IS NOT NULL DROP TABLE dbo.Candidates;
IF OBJECT_ID(N'dbo.Managers', N'U') IS NOT NULL DROP TABLE dbo.Managers;
GO

-- ----------------------------------------------------------------------------
-- Table 1: Managers (System Administrators & HR Recruitment Officers)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Managers (
    ManagerId INT IDENTITY(1,1) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    BranchLocation NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Managers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Managers PRIMARY KEY CLUSTERED (ManagerId),
    CONSTRAINT UQ_Managers_Email UNIQUE (Email)
);
GO

-- ----------------------------------------------------------------------------
-- Table 2: Candidates (Job Applicants Enrolled for Aptitude Assessment)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Candidates (
    CandidateId INT IDENTITY(1,1) NOT NULL,
    RegistrationNumber NVARCHAR(50) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    DateOfBirth DATE NOT NULL,
    CurrentAddress NVARCHAR(250) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedByManagerId INT NOT NULL,
    EnrolledAt DATETIME2(3) NOT NULL CONSTRAINT DF_Candidates_EnrolledAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Candidates PRIMARY KEY CLUSTERED (CandidateId),
    CONSTRAINT UQ_Candidates_RegNo UNIQUE (RegistrationNumber),
    CONSTRAINT UQ_Candidates_Email UNIQUE (Email),
    CONSTRAINT UQ_Candidates_Phone UNIQUE (Phone),
    CONSTRAINT UQ_Candidates_Username UNIQUE (Username),
    CONSTRAINT FK_Candidates_Managers FOREIGN KEY (CreatedByManagerId)
        REFERENCES dbo.Managers (ManagerId) ON DELETE NO ACTION
);
GO

-- ----------------------------------------------------------------------------
-- Table 3: CandidateEducations (Academic Qualifications - 3NF Relation)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.CandidateEducations (
    EducationId INT IDENTITY(1,1) NOT NULL,
    CandidateId INT NOT NULL,
    DegreeName NVARCHAR(100) NOT NULL,
    InstituteName NVARCHAR(150) NOT NULL,
    PassingYear INT NOT NULL CONSTRAINT CK_Educations_PassingYear CHECK (PassingYear BETWEEN 1970 AND 2100),
    PercentageOrCGPA DECIMAL(5,2) NOT NULL CONSTRAINT CK_Educations_Percentage CHECK (PercentageOrCGPA >= 0.00 AND PercentageOrCGPA <= 100.00),
    Specialization NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_CandidateEducations PRIMARY KEY CLUSTERED (EducationId),
    CONSTRAINT FK_CandidateEducations_Candidates FOREIGN KEY (CandidateId)
        REFERENCES dbo.Candidates (CandidateId) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------------------------
-- Table 4: CandidateExperiences (Employment History - 3NF Relation)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.CandidateExperiences (
    ExperienceId INT IDENTITY(1,1) NOT NULL,
    CandidateId INT NOT NULL,
    CompanyName NVARCHAR(150) NOT NULL,
    Designation NVARCHAR(100) NOT NULL,
    TotalYears DECIMAL(4,1) NOT NULL CONSTRAINT CK_Experiences_TotalYears CHECK (TotalYears >= 0.0),
    KeySkills NVARCHAR(250) NOT NULL,
    CONSTRAINT PK_CandidateExperiences PRIMARY KEY CLUSTERED (ExperienceId),
    CONSTRAINT FK_CandidateExperiences_Candidates FOREIGN KEY (CandidateId)
        REFERENCES dbo.Candidates (CandidateId) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------------------------
-- Table 5: TestSections (Configurable Linear Test Rounds)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.TestSections (
    SectionId INT IDENTITY(1,1) NOT NULL,
    SectionName NVARCHAR(50) NOT NULL,
    SequenceOrder INT NOT NULL,
    TotalQuestions INT NOT NULL CONSTRAINT DF_TestSections_TotalQuestions DEFAULT 5 CONSTRAINT CK_TestSections_TotalQuestions CHECK (TotalQuestions > 0),
    TimeLimitMinutes INT NOT NULL CONSTRAINT CK_TestSections_TimeLimit CHECK (TimeLimitMinutes > 0),
    CutoffPercentage DECIMAL(5,2) NOT NULL CONSTRAINT CK_TestSections_Cutoff CHECK (CutoffPercentage >= 0.00 AND CutoffPercentage <= 100.00),
    CONSTRAINT PK_TestSections PRIMARY KEY CLUSTERED (SectionId),
    CONSTRAINT UQ_TestSections_SectionName UNIQUE (SectionName),
    CONSTRAINT UQ_TestSections_SequenceOrder UNIQUE (SequenceOrder),
    CONSTRAINT CK_TestSections_Name CHECK (SectionName IN ('General_Knowledge', 'Mathematics', 'Computer_Technology'))
);
GO

-- ----------------------------------------------------------------------------
-- Table 6: Questions (Manager Question Bank per Section)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Questions (
    QuestionId INT IDENTITY(1,1) NOT NULL,
    SectionId INT NOT NULL,
    QuestionText NVARCHAR(MAX) NOT NULL,
    Marks INT NOT NULL CONSTRAINT DF_Questions_Marks DEFAULT 1 CONSTRAINT CK_Questions_Marks CHECK (Marks BETWEEN 1 AND 5),
    IsActive BIT NOT NULL CONSTRAINT DF_Questions_IsActive DEFAULT 1,
    CreatedByManagerId INT NOT NULL,
    UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Questions_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Questions PRIMARY KEY CLUSTERED (QuestionId),
    CONSTRAINT FK_Questions_TestSections FOREIGN KEY (SectionId)
        REFERENCES dbo.TestSections (SectionId) ON DELETE NO ACTION,
    CONSTRAINT FK_Questions_Managers FOREIGN KEY (CreatedByManagerId)
        REFERENCES dbo.Managers (ManagerId) ON DELETE NO ACTION
);
GO

-- ----------------------------------------------------------------------------
-- Table 7: QuestionOptions (Multiple Choice Answers per Question)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.QuestionOptions (
    OptionId INT IDENTITY(1,1) NOT NULL,
    QuestionId INT NOT NULL,
    OptionLabel CHAR(1) NOT NULL CONSTRAINT CK_Options_Label CHECK (OptionLabel IN ('A', 'B', 'C', 'D')),
    OptionText NVARCHAR(500) NOT NULL,
    IsCorrect BIT NOT NULL CONSTRAINT DF_QuestionOptions_IsCorrect DEFAULT 0,
    CONSTRAINT PK_QuestionOptions PRIMARY KEY CLUSTERED (OptionId),
    CONSTRAINT UQ_QuestionOptions_QuestionLabel UNIQUE (QuestionId, OptionLabel),
    CONSTRAINT FK_QuestionOptions_Questions FOREIGN KEY (QuestionId)
        REFERENCES dbo.Questions (QuestionId) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------------------------
-- Table 8: TestAttempts (Master Assessment State & Linear Progress Engine)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.TestAttempts (
    AttemptId INT IDENTITY(1,1) NOT NULL,
    CandidateId INT NOT NULL,
    CurrentSectionId INT NOT NULL,
    StartedAt DATETIME2(3) NOT NULL CONSTRAINT DF_TestAttempts_StartedAt DEFAULT SYSUTCDATETIME(),
    SectionStartedAt DATETIME2(3) NOT NULL CONSTRAINT DF_TestAttempts_SectionStartedAt DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2(3) NULL,
    TotalScoreObtained DECIMAL(6,2) NOT NULL CONSTRAINT DF_TestAttempts_Score DEFAULT 0.00,
    MaxPossibleScore DECIMAL(6,2) NOT NULL CONSTRAINT DF_TestAttempts_MaxScore DEFAULT 0.00,
    PercentageScore DECIMAL(5,2) NOT NULL CONSTRAINT DF_TestAttempts_Percentage DEFAULT 0.00,
    IsPassed BIT NOT NULL CONSTRAINT DF_TestAttempts_IsPassed DEFAULT 0,
    AttemptStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_TestAttempts_Status DEFAULT 'Not_Started',
    CONSTRAINT PK_TestAttempts PRIMARY KEY CLUSTERED (AttemptId),
    CONSTRAINT UQ_TestAttempts_Candidate UNIQUE (CandidateId),
    CONSTRAINT FK_TestAttempts_Candidates FOREIGN KEY (CandidateId)
        REFERENCES dbo.Candidates (CandidateId) ON DELETE CASCADE,
    CONSTRAINT FK_TestAttempts_CurrentSection FOREIGN KEY (CurrentSectionId)
        REFERENCES dbo.TestSections (SectionId) ON DELETE NO ACTION,
    CONSTRAINT CK_TestAttempts_Status CHECK (AttemptStatus IN ('Not_Started', 'In_Progress', 'Timed_Out', 'Completed'))
);
GO

-- ----------------------------------------------------------------------------
-- Table 9: CandidateResponses (Individual Answers Submitted per Question)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.CandidateResponses (
    ResponseId INT IDENTITY(1,1) NOT NULL,
    AttemptId INT NOT NULL,
    QuestionId INT NOT NULL,
    SelectedOptionId INT NULL,
    MarksAwarded DECIMAL(4,2) NOT NULL CONSTRAINT DF_CandidateResponses_Marks DEFAULT 0.00,
    AnsweredAt DATETIME2(3) NOT NULL CONSTRAINT DF_CandidateResponses_AnsweredAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CandidateResponses PRIMARY KEY CLUSTERED (ResponseId),
    CONSTRAINT UQ_CandidateResponses_AttemptQuestion UNIQUE (AttemptId, QuestionId),
    CONSTRAINT FK_CandidateResponses_TestAttempts FOREIGN KEY (AttemptId)
        REFERENCES dbo.TestAttempts (AttemptId) ON DELETE CASCADE,
    CONSTRAINT FK_CandidateResponses_Questions FOREIGN KEY (QuestionId)
        REFERENCES dbo.Questions (QuestionId) ON DELETE NO ACTION,
    CONSTRAINT FK_CandidateResponses_QuestionOptions FOREIGN KEY (SelectedOptionId)
        REFERENCES dbo.QuestionOptions (OptionId) ON DELETE NO ACTION
);
GO

-- ----------------------------------------------------------------------------
-- Table 10: AptiClearedCandidates (HR Evaluation Pipeline - Automated Transfer Table)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.AptiClearedCandidates (
    ClearanceId INT IDENTITY(1,1) NOT NULL,
    CandidateId INT NOT NULL,
    AttemptId INT NOT NULL,
    OverallScore DECIMAL(6,2) NOT NULL,
    PercentageScore DECIMAL(5,2) NOT NULL,
    ClearanceDate DATETIME2(3) NOT NULL CONSTRAINT DF_AptiCleared_Date DEFAULT SYSUTCDATETIME(),
    HrInterviewStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_AptiCleared_Status DEFAULT 'Pending',
    HrRemarks NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AptiClearedCandidates PRIMARY KEY CLUSTERED (ClearanceId),
    CONSTRAINT UQ_AptiCleared_Candidate UNIQUE (CandidateId),
    CONSTRAINT UQ_AptiCleared_Attempt UNIQUE (AttemptId),
    CONSTRAINT FK_AptiCleared_Candidates FOREIGN KEY (CandidateId)
        REFERENCES dbo.Candidates (CandidateId) ON DELETE NO ACTION,
    CONSTRAINT FK_AptiCleared_TestAttempts FOREIGN KEY (AttemptId)
        REFERENCES dbo.TestAttempts (AttemptId) ON DELETE NO ACTION,
    CONSTRAINT CK_AptiCleared_HrStatus CHECK (HrInterviewStatus IN ('Pending', 'Scheduled', 'Selected', 'Rejected'))
);
GO

-- ----------------------------------------------------------------------------
-- Table 11: AssessmentAuditLogs (Security, Linear Flow & Time Tracking Logs)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.AssessmentAuditLogs (
    LogId INT IDENTITY(1,1) NOT NULL,
    CandidateId INT NULL,
    ActionType NVARCHAR(50) NOT NULL,
    SectionName NVARCHAR(50) NULL,
    Details NVARCHAR(MAX) NULL,
    LoggedAt DATETIME2(3) NOT NULL CONSTRAINT DF_AssessmentAudit_LoggedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AssessmentAuditLogs PRIMARY KEY CLUSTERED (LogId)
);
GO

-- ============================================================================
-- PERFORMANCE & INTEGRITY INDEXES
-- ============================================================================

-- Index for searching candidate qualifications
CREATE NONCLUSTERED INDEX IX_CandidateEducations_CandidateId
    ON dbo.CandidateEducations (CandidateId)
    INCLUDE (DegreeName, PassingYear, PercentageOrCGPA);
GO

-- Index for searching candidate experience
CREATE NONCLUSTERED INDEX IX_CandidateExperiences_CandidateId
    ON dbo.CandidateExperiences (CandidateId)
    INCLUDE (CompanyName, Designation, TotalYears);
GO

-- Index on Questions by Section and Active status
CREATE NONCLUSTERED INDEX IX_Questions_Section_Active
    ON dbo.Questions (SectionId, IsActive)
    INCLUDE (Marks, QuestionText);
GO

-- Index on QuestionOptions by QuestionId
CREATE NONCLUSTERED INDEX IX_QuestionOptions_QuestionId
    ON dbo.QuestionOptions (QuestionId)
    INCLUDE (OptionLabel, IsCorrect);
GO

-- Index on TestAttempts for active state searches
CREATE NONCLUSTERED INDEX IX_TestAttempts_Status_Section
    ON dbo.TestAttempts (AttemptStatus, CurrentSectionId)
    INCLUDE (CandidateId, StartedAt, SectionStartedAt);
GO

-- Index on CandidateResponses for score aggregation
CREATE NONCLUSTERED INDEX IX_CandidateResponses_AttemptId
    ON dbo.CandidateResponses (AttemptId)
    INCLUDE (QuestionId, SelectedOptionId, MarksAwarded);
GO

-- Index on AptiClearedCandidates for HR Queue filtration
CREATE NONCLUSTERED INDEX IX_AptiClearedCandidates_HrStatus_Date
    ON dbo.AptiClearedCandidates (HrInterviewStatus, ClearanceDate)
    INCLUDE (CandidateId, OverallScore, PercentageScore);
GO

-- Index on AssessmentAuditLogs for Candidate audit trail
CREATE NONCLUSTERED INDEX IX_AssessmentAuditLogs_Candidate_Action
    ON dbo.AssessmentAuditLogs (CandidateId, ActionType, LoggedAt);
GO

PRINT 'Successfully created Webster Organisation 11-table 3NF schema and performance indexes.';
GO
