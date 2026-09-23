-- ============================================================================
-- WEBSTER ORGANISATION - ONLINE APTITUDE TEST & RECRUITMENT SYSTEM
-- Script 04: Production Seed Data (Managers, Candidates, Sections, Questions & Attempts)
-- ============================================================================

USE WebsterAptitudeDb;
GO

-- ----------------------------------------------------------------------------
-- 1. SEED MANAGERS (Password is 'Admin@123' hashed with BCrypt)
-- Hash: $2a$11$w8NlQyDqGjN7x3E1K6P9UuH2J3K4L5M6N7O8P9Q0R1S2T3U4V5W6
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Managers ON;

INSERT INTO dbo.Managers (ManagerId, FullName, Email, PasswordHash, BranchLocation, CreatedAt)
VALUES
(1, N'Sarah Jenkins', N'sarah.jenkins@webster.org', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', N'New York Global HQ', SYSUTCDATETIME()),
(2, N'David Sterling', N'david.sterling@webster.org', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', N'London EMEA Operations', SYSUTCDATETIME());

SET IDENTITY_INSERT dbo.Managers OFF;
GO

-- ----------------------------------------------------------------------------
-- 2. SEED TEST SECTIONS (General Knowledge -> Mathematics -> Computer Technology)
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.TestSections ON;

INSERT INTO dbo.TestSections (SectionId, SectionName, SequenceOrder, TotalQuestions, TimeLimitMinutes, CutoffPercentage)
VALUES
(1, N'General_Knowledge', 1, 5, 5, 50.00),
(2, N'Mathematics', 2, 5, 8, 50.00),
(3, N'Computer_Technology', 3, 5, 7, 50.00);

SET IDENTITY_INSERT dbo.TestSections OFF;
GO

-- ----------------------------------------------------------------------------
-- 3. SEED CANDIDATES (Password is 'Candidate@123' hashed with BCrypt)
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Candidates ON;

INSERT INTO dbo.Candidates (CandidateId, RegistrationNumber, FullName, Email, Phone, DateOfBirth, CurrentAddress, Username, PasswordHash, CreatedByManagerId, EnrolledAt)
VALUES
(1, N'WEB-2026-001', N'Alexander Hayes', N'alex.hayes@example.com', N'+1-555-0101', '1999-04-12', N'452 Madison Ave, New York, NY', N'alex.hayes', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', 1, DATEADD(DAY, -10, SYSUTCDATETIME())),
(2, N'WEB-2026-002', N'Priya Sharma', N'priya.sharma@example.com', N'+1-555-0102', '2000-08-25', N'78 Baker Street, London, UK', N'priya.sharma', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', 2, DATEADD(DAY, -8, SYSUTCDATETIME())),
(3, N'WEB-2026-003', N'Marcus Vance', N'marcus.vance@example.com', N'+1-555-0103', '1998-11-05', N'120 Wall Street, New York, NY', N'marcus.vance', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', 1, DATEADD(DAY, -5, SYSUTCDATETIME())),
(4, N'WEB-2026-004', N'Elena Rostova', N'elena.rostova@example.com', N'+1-555-0104', '2001-02-18', N'34 Canary Wharf, London, UK', N'elena.rostova', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', 2, DATEADD(DAY, -3, SYSUTCDATETIME())),
(5, N'WEB-2026-005', N'Chen Wei', N'chen.wei@example.com', N'+1-555-0105', '1999-09-30', N'89 Broadway, New York, NY', N'chen.wei', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', 1, DATEADD(DAY, -2, SYSUTCDATETIME())),
(6, N'WEB-2026-006', N'Sofia Mendez', N'sofia.mendez@example.com', N'+1-555-0106', '2000-06-14', N'215 Fleet St, London, UK', N'sofia.mendez', N'$2a$11$eOQ0rT0uI1xZyGnmq8UuqeW9b7j8fK92YyFpG4eY7n9fQ4E9mN5Oi', 2, DATEADD(DAY, -1, SYSUTCDATETIME()));

SET IDENTITY_INSERT dbo.Candidates OFF;
GO

-- ----------------------------------------------------------------------------
-- 4. SEED CANDIDATE ACADEMICS
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.CandidateEducations ON;

INSERT INTO dbo.CandidateEducations (EducationId, CandidateId, DegreeName, InstituteName, PassingYear, PercentageOrCGPA, Specialization)
VALUES
(1, 1, N'Bachelor of Science in Computer Science', N'Columbia University', 2021, 88.50, N'Software Systems & Algorithms'),
(2, 2, N'Master of Science in Data Science', N'Imperial College London', 2022, 91.20, N'Machine Learning & Statistics'),
(3, 3, N'Bachelor of Engineering in Information Technology', N'New York University', 2020, 74.00, N'Network Security & Databases'),
(4, 4, N'Bachelor of Science in Mathematics', N'King''s College London', 2023, 85.00, N'Applied Pure Mathematics'),
(5, 5, N'Master of Computer Applications', N'Boston University', 2022, 79.50, N'Cloud Architecture'),
(6, 6, N'Bachelor of Business Information Systems', N'University College London', 2022, 82.00, N'Enterprise Analysis');

SET IDENTITY_INSERT dbo.CandidateEducations OFF;
GO

-- ----------------------------------------------------------------------------
-- 5. SEED CANDIDATE WORK EXPERIENCES
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.CandidateExperiences ON;

INSERT INTO dbo.CandidateExperiences (ExperienceId, CandidateId, CompanyName, Designation, TotalYears, KeySkills)
VALUES
(1, 1, N'Apex FinTech Labs', N'Junior Software Developer', 2.5, N'C#, .NET Core, SQL Server, RESTful APIs, Git'),
(2, 2, N'DataStream Analytics', N'Data Analyst', 1.8, N'Python, SQL, Power BI, Statistics, Predictive Modeling'),
(3, 3, N'Titan Cyber Security', N'Associate Systems Analyst', 3.0, N'Windows Server, Linux, SQL Queries, Networking'),
(4, 5, N'CloudPeak Solutions', N'.NET Backend Intern', 1.0, N'C#, ASP.NET, Entity Framework, Azure');

SET IDENTITY_INSERT dbo.CandidateExperiences OFF;
GO

-- ----------------------------------------------------------------------------
-- 6. SEED QUESTIONS (15 Questions: 5 GK, 5 Math, 5 Computer Technology)
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Questions ON;

-- SECTION 1: General Knowledge (Questions 1 - 5)
INSERT INTO dbo.Questions (QuestionId, SectionId, QuestionText, Marks, IsActive, CreatedByManagerId, UpdatedAt)
VALUES
(1, 1, N'Which international organization is headquartered in Geneva, Switzerland and oversees international public health?', 1, 1, 1, SYSUTCDATETIME()),
(2, 1, N'What is the currency of Japan?', 1, 1, 1, SYSUTCDATETIME()),
(3, 1, N'Which treaty established the European Union and introduced the criteria for the single European currency?', 2, 1, 1, SYSUTCDATETIME()),
(4, 1, N'Who is widely regarded as the father of modern economics for his work "The Wealth of Nations"?', 1, 1, 1, SYSUTCDATETIME()),
(5, 1, N'Which canal connects the Mediterranean Sea to the Red Sea, facilitating global maritime trade?', 2, 1, 1, SYSUTCDATETIME());

-- SECTION 2: Mathematics (Questions 6 - 10)
INSERT INTO dbo.Questions (QuestionId, SectionId, QuestionText, Marks, IsActive, CreatedByManagerId, UpdatedAt)
VALUES
(6, 2, N'If a train travels 360 km in 4 hours, what is its average speed in meters per second (m/s)?', 2, 1, 2, SYSUTCDATETIME()),
(7, 2, N'A product originally priced at $250 is offered at a 20% discount. If an 8% sales tax is then applied to the discounted price, what is the final cost?', 2, 1, 2, SYSUTCDATETIME()),
(8, 2, N'What is the value of x in the quadratic equation: x^2 - 7x + 12 = 0?', 2, 1, 2, SYSUTCDATETIME()),
(9, 2, N'In a bag containing 4 red, 6 blue, and 5 green marbles, what is the probability of randomly drawing one marble that is NOT blue?', 1, 1, 2, SYSUTCDATETIME()),
(10, 2, N'If 8 workers can complete an enterprise construction task in 15 days, how many days will 12 workers take to complete the same task at the same pace?', 2, 1, 2, SYSUTCDATETIME());

-- SECTION 3: Computer Technology (Questions 11 - 15)
INSERT INTO dbo.Questions (QuestionId, SectionId, QuestionText, Marks, IsActive, CreatedByManagerId, UpdatedAt)
VALUES
(11, 3, N'In Relational Database Management Systems (RDBMS), what does the "A" represent in the ACID transaction properties?', 1, 1, 1, SYSUTCDATETIME()),
(12, 3, N'Which data structure operates strictly on a Last-In, First-Out (LIFO) order of element retrieval?', 1, 1, 1, SYSUTCDATETIME()),
(13, 3, N'In modern .NET 8 C# architecture, what is the primary purpose of the Dependency Injection (DI) pattern?', 2, 1, 1, SYSUTCDATETIME()),
(14, 3, N'What is the computational time complexity of looking up a value in an optimized Hash Table under average circumstances?', 1, 1, 1, SYSUTCDATETIME()),
(15, 3, N'In SQL Server, which type of index dictates the actual physical order of data pages stored on disk?', 2, 1, 1, SYSUTCDATETIME());

SET IDENTITY_INSERT dbo.Questions OFF;
GO

-- ----------------------------------------------------------------------------
-- 7. SEED QUESTION OPTIONS (4 Options per Question = 60 Options)
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.QuestionOptions ON;

-- Question 1: WHO
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(1, 1, 'A', N'World Health Organization (WHO)', 1),
(2, 1, 'B', N'United Nations Educational, Scientific and Cultural Organization (UNESCO)', 0),
(3, 1, 'C', N'International Monetary Fund (IMF)', 0),
(4, 1, 'D', N'World Bank', 0);

-- Question 2: Yen
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(5, 2, 'A', N'Yuan', 0),
(6, 2, 'B', N'Won', 0),
(7, 2, 'C', N'Yen', 1),
(8, 2, 'D', N'Ringgit', 0);

-- Question 3: Maastricht Treaty
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(9, 3, 'A', N'Treaty of Versailles', 0),
(10, 3, 'B', N'Maastricht Treaty', 1),
(11, 3, 'C', N'Treaty of Rome', 0),
(12, 3, 'D', N'Treaty of Lisbon', 0);

-- Question 4: Adam Smith
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(13, 4, 'A', N'John Maynard Keynes', 0),
(14, 4, 'B', N'Milton Friedman', 0),
(15, 4, 'C', N'Karl Marx', 0),
(16, 4, 'D', N'Adam Smith', 1);

-- Question 5: Suez Canal
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(17, 5, 'A', N'Panama Canal', 0),
(18, 5, 'B', N'Suez Canal', 1),
(19, 5, 'C', N'Kiel Canal', 0),
(20, 5, 'D', N'Corinth Canal', 0);

-- Question 6: Math Speed
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(21, 6, 'A', N'20 m/s', 0),
(22, 6, 'B', N'25 m/s', 1),
(23, 6, 'C', N'30 m/s', 0),
(24, 6, 'D', N'90 m/s', 0);

-- Question 7: Math Discount + Tax
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(25, 7, 'A', N'$210.00', 0),
(26, 7, 'B', N'$216.00', 1),
(27, 7, 'C', N'$220.00', 0),
(28, 7, 'D', N'$200.00', 0);

-- Question 8: Quadratic Roots
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(29, 8, 'A', N'x = 2 or x = 5', 0),
(30, 8, 'B', N'x = 3 or x = 4', 1),
(31, 8, 'C', N'x = -3 or x = -4', 0),
(32, 8, 'D', N'x = 1 or x = 12', 0);

-- Question 9: Marble Probability (4 red + 5 green = 9 / 15 = 3/5)
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(33, 9, 'A', N'2/5', 0),
(34, 9, 'B', N'3/5', 1),
(35, 9, 'C', N'1/3', 0),
(36, 9, 'D', N'4/15', 0);

-- Question 10: Work & Days (8 * 15 = 120 / 12 = 10)
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(37, 10, 'A', N'10 days', 1),
(38, 10, 'B', N'12 days', 0),
(39, 10, 'C', N'8 days', 0),
(40, 10, 'D', N'14 days', 0);

-- Question 11: CS Atomicity
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(41, 11, 'A', N'Atomicity', 1),
(42, 11, 'B', N'Availability', 0),
(43, 11, 'C', N'Authenticity', 0),
(44, 11, 'D', N'Asynchronous', 0);

-- Question 12: Stack LIFO
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(45, 12, 'A', N'Queue', 0),
(46, 12, 'B', N'Stack', 1),
(47, 12, 'C', N'Linked List', 0),
(48, 12, 'D', N'Binary Tree', 0);

-- Question 13: DI Pattern
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(49, 13, 'A', N'To achieve loose coupling, modularity, and testability across software layers', 1),
(50, 13, 'B', N'To encrypt database connection strings in memory', 0),
(51, 13, 'C', N'To compile C# IL bytecode into native machine instructions', 0),
(52, 13, 'D', N'To manage multi-threaded garbage collection cycles', 0);

-- Question 14: Hash Table Average O(1)
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(53, 14, 'A', N'O(log n)', 0),
(54, 14, 'B', N'O(n)', 0),
(55, 14, 'C', N'O(1)', 1),
(56, 14, 'D', N'O(n log n)', 0);

-- Question 15: Clustered Index
INSERT INTO dbo.QuestionOptions (OptionId, QuestionId, OptionLabel, OptionText, IsCorrect) VALUES
(57, 15, 'A', N'Non-Clustered Index', 0),
(58, 15, 'B', N'Filtered Index', 0),
(59, 15, 'C', N'Clustered Index', 1),
(60, 15, 'D', N'Full-Text Index', 0);

SET IDENTITY_INSERT dbo.QuestionOptions OFF;
GO

-- ----------------------------------------------------------------------------
-- 8. SEED TEST ATTEMPTS & RESPONSES FOR SIMULATION
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.TestAttempts ON;

-- Candidate 1: Alex Hayes -> Completed and Passed (Score 20/23 = 86.96%)
INSERT INTO dbo.TestAttempts (AttemptId, CandidateId, CurrentSectionId, StartedAt, SectionStartedAt, CompletedAt, TotalScoreObtained, MaxPossibleScore, PercentageScore, IsPassed, AttemptStatus)
VALUES
(1, 1, 3, DATEADD(MINUTE, -40, SYSUTCDATETIME()), DATEADD(MINUTE, -15, SYSUTCDATETIME()), DATEADD(MINUTE, -10, SYSUTCDATETIME()), 20.00, 23.00, 86.96, 1, 'Completed');

-- Candidate 2: Priya Sharma -> Completed and Passed (Score 23/23 = 100%)
INSERT INTO dbo.TestAttempts (AttemptId, CandidateId, CurrentSectionId, StartedAt, SectionStartedAt, CompletedAt, TotalScoreObtained, MaxPossibleScore, PercentageScore, IsPassed, AttemptStatus)
VALUES
(2, 2, 3, DATEADD(MINUTE, -30, SYSUTCDATETIME()), DATEADD(MINUTE, -10, SYSUTCDATETIME()), DATEADD(MINUTE, -5, SYSUTCDATETIME()), 23.00, 23.00, 100.00, 1, 'Completed');

-- Candidate 3: Marcus Vance -> Completed and Failed (Score 8/23 = 34.78%)
INSERT INTO dbo.TestAttempts (AttemptId, CandidateId, CurrentSectionId, StartedAt, SectionStartedAt, CompletedAt, TotalScoreObtained, MaxPossibleScore, PercentageScore, IsPassed, AttemptStatus)
VALUES
(3, 3, 3, DATEADD(MINUTE, -50, SYSUTCDATETIME()), DATEADD(MINUTE, -20, SYSUTCDATETIME()), DATEADD(MINUTE, -15, SYSUTCDATETIME()), 8.00, 23.00, 34.78, 0, 'Completed');

-- Candidate 4: Elena Rostova -> In Progress (Currently on Section 2: Mathematics)
INSERT INTO dbo.TestAttempts (AttemptId, CandidateId, CurrentSectionId, StartedAt, SectionStartedAt, CompletedAt, TotalScoreObtained, MaxPossibleScore, PercentageScore, IsPassed, AttemptStatus)
VALUES
(4, 4, 2, DATEADD(MINUTE, -6, SYSUTCDATETIME()), DATEADD(MINUTE, -2, SYSUTCDATETIME()), NULL, 7.00, 23.00, 0.00, 0, 'In_Progress');

SET IDENTITY_INSERT dbo.TestAttempts OFF;
GO

-- ----------------------------------------------------------------------------
-- 9. SEED APTI CLEARED CANDIDATES (Alex Hayes & Priya Sharma transferred to HR)
-- ----------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.AptiClearedCandidates ON;

INSERT INTO dbo.AptiClearedCandidates (ClearanceId, CandidateId, AttemptId, OverallScore, PercentageScore, ClearanceDate, HrInterviewStatus, HrRemarks)
VALUES
(1, 1, 1, 20.00, 86.96, DATEADD(MINUTE, -10, SYSUTCDATETIME()), 'Scheduled', N'Technical proficiency high in .NET & Algorithms. Scheduled HR round for Friday 10:00 AM.'),
(2, 2, 2, 23.00, 100.00, DATEADD(MINUTE, -5, SYSUTCDATETIME()), 'Pending', N'Perfect aptitude score across all 3 sections. Awaiting HR panel assignment.');

SET IDENTITY_INSERT dbo.AptiClearedCandidates OFF;
GO

PRINT 'Successfully seeded database with Managers, Candidates, Questions, Options, and Attempts.';
GO
