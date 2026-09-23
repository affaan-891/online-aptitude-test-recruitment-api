using Microsoft.EntityFrameworkCore;
using Webster.AptitudePortal.Api.Models.Entities;

namespace Webster.AptitudePortal.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AptitudeDbContext context)
    {
        // For relational databases, ensure created
        if (context.Database.IsRelational())
        {
            await context.Database.EnsureCreatedAsync();
        }

        // Avoid re-seeding if data already exists
        if (await context.Managers.AnyAsync())
        {
            return;
        }

        // Standard hash for 'Admin@123' and 'Candidate@123'
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");

        // 1. Seed Managers
        var manager1 = new Manager
        {
            FullName = "Sarah Jenkins",
            Email = "sarah.jenkins@webster.org",
            PasswordHash = defaultPasswordHash,
            BranchLocation = "New York Global HQ",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var manager2 = new Manager
        {
            FullName = "David Sterling",
            Email = "david.sterling@webster.org",
            PasswordHash = defaultPasswordHash,
            BranchLocation = "London EMEA Operations",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.Managers.AddRange(manager1, manager2);
        await context.SaveChangesAsync();

        // 2. Seed Test Sections (Linear: GK -> Math -> CS)
        var secGk = new TestSection
        {
            SectionName = "General_Knowledge",
            SequenceOrder = 1,
            TotalQuestions = 5,
            TimeLimitMinutes = 5,
            CutoffPercentage = 50.00m
        };

        var secMath = new TestSection
        {
            SectionName = "Mathematics",
            SequenceOrder = 2,
            TotalQuestions = 5,
            TimeLimitMinutes = 8,
            CutoffPercentage = 50.00m
        };

        var secCs = new TestSection
        {
            SectionName = "Computer_Technology",
            SequenceOrder = 3,
            TotalQuestions = 5,
            TimeLimitMinutes = 7,
            CutoffPercentage = 50.00m
        };

        context.TestSections.AddRange(secGk, secMath, secCs);
        await context.SaveChangesAsync();

        // 3. Seed Candidates with Academics and Experiences
        var candidates = new List<Candidate>
        {
            new Candidate
            {
                RegistrationNumber = "WEB-2026-001",
                FullName = "Alexander Hayes",
                Email = "alex.hayes@example.com",
                Phone = "+1-555-0101",
                DateOfBirth = new DateTime(1999, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                CurrentAddress = "452 Madison Ave, New York, NY",
                Username = "alex.hayes",
                PasswordHash = defaultPasswordHash,
                CreatedByManagerId = manager1.ManagerId,
                EnrolledAt = DateTime.UtcNow.AddDays(-10),
                Educations = new List<CandidateEducation>
                {
                    new CandidateEducation
                    {
                        DegreeName = "Bachelor of Science in Computer Science",
                        InstituteName = "Columbia University",
                        PassingYear = 2021,
                        PercentageOrCGPA = 88.50m,
                        Specialization = "Software Systems & Algorithms"
                    }
                },
                Experiences = new List<CandidateExperience>
                {
                    new CandidateExperience
                    {
                        CompanyName = "Apex FinTech Labs",
                        Designation = "Junior Software Developer",
                        TotalYears = 2.5m,
                        KeySkills = "C#, .NET Core, SQL Server, RESTful APIs, Git"
                    }
                }
            },
            new Candidate
            {
                RegistrationNumber = "WEB-2026-002",
                FullName = "Priya Sharma",
                Email = "priya.sharma@example.com",
                Phone = "+1-555-0102",
                DateOfBirth = new DateTime(2000, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                CurrentAddress = "78 Baker Street, London, UK",
                Username = "priya.sharma",
                PasswordHash = defaultPasswordHash,
                CreatedByManagerId = manager2.ManagerId,
                EnrolledAt = DateTime.UtcNow.AddDays(-8),
                Educations = new List<CandidateEducation>
                {
                    new CandidateEducation
                    {
                        DegreeName = "Master of Science in Data Science",
                        InstituteName = "Imperial College London",
                        PassingYear = 2022,
                        PercentageOrCGPA = 91.20m,
                        Specialization = "Machine Learning & Statistics"
                    }
                },
                Experiences = new List<CandidateExperience>
                {
                    new CandidateExperience
                    {
                        CompanyName = "DataStream Analytics",
                        Designation = "Data Analyst",
                        TotalYears = 1.8m,
                        KeySkills = "Python, SQL, Power BI, Statistics, Predictive Modeling"
                    }
                }
            },
            new Candidate
            {
                RegistrationNumber = "WEB-2026-003",
                FullName = "Marcus Vance",
                Email = "marcus.vance@example.com",
                Phone = "+1-555-0103",
                DateOfBirth = new DateTime(1998, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                CurrentAddress = "120 Wall Street, New York, NY",
                Username = "marcus.vance",
                PasswordHash = defaultPasswordHash,
                CreatedByManagerId = manager1.ManagerId,
                EnrolledAt = DateTime.UtcNow.AddDays(-5),
                Educations = new List<CandidateEducation>
                {
                    new CandidateEducation
                    {
                        DegreeName = "Bachelor of Engineering in Information Technology",
                        InstituteName = "New York University",
                        PassingYear = 2020,
                        PercentageOrCGPA = 74.00m,
                        Specialization = "Network Security & Databases"
                    }
                },
                Experiences = new List<CandidateExperience>
                {
                    new CandidateExperience
                    {
                        CompanyName = "Titan Cyber Security",
                        Designation = "Associate Systems Analyst",
                        TotalYears = 3.0m,
                        KeySkills = "Windows Server, Linux, SQL Queries, Networking"
                    }
                }
            },
            new Candidate
            {
                RegistrationNumber = "WEB-2026-004",
                FullName = "Elena Rostova",
                Email = "elena.rostova@example.com",
                Phone = "+1-555-0104",
                DateOfBirth = new DateTime(2001, 2, 18, 0, 0, 0, DateTimeKind.Utc),
                CurrentAddress = "34 Canary Wharf, London, UK",
                Username = "elena.rostova",
                PasswordHash = defaultPasswordHash,
                CreatedByManagerId = manager2.ManagerId,
                EnrolledAt = DateTime.UtcNow.AddDays(-3),
                Educations = new List<CandidateEducation>
                {
                    new CandidateEducation
                    {
                        DegreeName = "Bachelor of Science in Mathematics",
                        InstituteName = "King's College London",
                        PassingYear = 2023,
                        PercentageOrCGPA = 85.00m,
                        Specialization = "Applied Pure Mathematics"
                    }
                }
            },
            new Candidate
            {
                RegistrationNumber = "WEB-2026-005",
                FullName = "Chen Wei",
                Email = "chen.wei@example.com",
                Phone = "+1-555-0105",
                DateOfBirth = new DateTime(1999, 9, 30, 0, 0, 0, DateTimeKind.Utc),
                CurrentAddress = "89 Broadway, New York, NY",
                Username = "chen.wei",
                PasswordHash = defaultPasswordHash,
                CreatedByManagerId = manager1.ManagerId,
                EnrolledAt = DateTime.UtcNow.AddDays(-2),
                Educations = new List<CandidateEducation>
                {
                    new CandidateEducation
                    {
                        DegreeName = "Master of Computer Applications",
                        InstituteName = "Boston University",
                        PassingYear = 2022,
                        PercentageOrCGPA = 79.50m,
                        Specialization = "Cloud Architecture"
                    }
                },
                Experiences = new List<CandidateExperience>
                {
                    new CandidateExperience
                    {
                        CompanyName = "CloudPeak Solutions",
                        Designation = ".NET Backend Intern",
                        TotalYears = 1.0m,
                        KeySkills = "C#, ASP.NET, Entity Framework, Azure"
                    }
                }
            },
            new Candidate
            {
                RegistrationNumber = "WEB-2026-006",
                FullName = "Sofia Mendez",
                Email = "sofia.mendez@example.com",
                Phone = "+1-555-0106",
                DateOfBirth = new DateTime(2000, 6, 14, 0, 0, 0, DateTimeKind.Utc),
                CurrentAddress = "215 Fleet St, London, UK",
                Username = "sofia.mendez",
                PasswordHash = defaultPasswordHash,
                CreatedByManagerId = manager2.ManagerId,
                EnrolledAt = DateTime.UtcNow.AddDays(-1),
                Educations = new List<CandidateEducation>
                {
                    new CandidateEducation
                    {
                        DegreeName = "Bachelor of Business Information Systems",
                        InstituteName = "University College London",
                        PassingYear = 2022,
                        PercentageOrCGPA = 82.00m,
                        Specialization = "Enterprise Analysis"
                    }
                }
            }
        };

        context.Candidates.AddRange(candidates);
        await context.SaveChangesAsync();

        // 4. Seed 15 Questions across 3 sections (5 questions each) with 4 options per question
        var questions = new List<Question>
        {
            // Section 1: General Knowledge
            new Question
            {
                SectionId = secGk.SectionId,
                QuestionText = "Which international organization is headquartered in Geneva, Switzerland and oversees international public health?",
                Marks = 1,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "World Health Organization (WHO)", IsCorrect = true },
                    new QuestionOption { OptionLabel = "B", OptionText = "UNESCO", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "International Monetary Fund (IMF)", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "World Bank", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secGk.SectionId,
                QuestionText = "What is the currency of Japan?",
                Marks = 1,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "Yuan", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "Won", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "Yen", IsCorrect = true },
                    new QuestionOption { OptionLabel = "D", OptionText = "Ringgit", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secGk.SectionId,
                QuestionText = "Which treaty established the European Union and introduced the criteria for the single European currency?",
                Marks = 2,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "Treaty of Versailles", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "Maastricht Treaty", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "Treaty of Rome", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "Treaty of Lisbon", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secGk.SectionId,
                QuestionText = "Who is widely regarded as the father of modern economics for his work 'The Wealth of Nations'?",
                Marks = 1,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "John Maynard Keynes", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "Milton Friedman", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "Karl Marx", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "Adam Smith", IsCorrect = true }
                }
            },
            new Question
            {
                SectionId = secGk.SectionId,
                QuestionText = "Which canal connects the Mediterranean Sea to the Red Sea, facilitating global maritime trade?",
                Marks = 2,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "Panama Canal", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "Suez Canal", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "Kiel Canal", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "Corinth Canal", IsCorrect = false }
                }
            },

            // Section 2: Mathematics
            new Question
            {
                SectionId = secMath.SectionId,
                QuestionText = "If a train travels 360 km in 4 hours, what is its average speed in meters per second (m/s)?",
                Marks = 2,
                CreatedByManagerId = manager2.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "20 m/s", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "25 m/s", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "30 m/s", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "90 m/s", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secMath.SectionId,
                QuestionText = "A product originally priced at $250 is offered at a 20% discount. If an 8% sales tax is then applied to the discounted price, what is the final cost?",
                Marks = 2,
                CreatedByManagerId = manager2.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "$210.00", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "$216.00", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "$220.00", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "$200.00", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secMath.SectionId,
                QuestionText = "What is the value of x in the quadratic equation: x^2 - 7x + 12 = 0?",
                Marks = 2,
                CreatedByManagerId = manager2.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "x = 2 or x = 5", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "x = 3 or x = 4", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "x = -3 or x = -4", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "x = 1 or x = 12", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secMath.SectionId,
                QuestionText = "In a bag containing 4 red, 6 blue, and 5 green marbles, what is the probability of randomly drawing one marble that is NOT blue?",
                Marks = 1,
                CreatedByManagerId = manager2.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "2/5", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "3/5", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "1/3", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "4/15", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secMath.SectionId,
                QuestionText = "If 8 workers can complete an enterprise construction task in 15 days, how many days will 12 workers take to complete the same task at the same pace?",
                Marks = 2,
                CreatedByManagerId = manager2.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "10 days", IsCorrect = true },
                    new QuestionOption { OptionLabel = "B", OptionText = "12 days", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "8 days", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "14 days", IsCorrect = false }
                }
            },

            // Section 3: Computer Technology
            new Question
            {
                SectionId = secCs.SectionId,
                QuestionText = "In Relational Database Management Systems (RDBMS), what does the 'A' represent in the ACID transaction properties?",
                Marks = 1,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "Atomicity", IsCorrect = true },
                    new QuestionOption { OptionLabel = "B", OptionText = "Availability", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "Authenticity", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "Asynchronous", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secCs.SectionId,
                QuestionText = "Which data structure operates strictly on a Last-In, First-Out (LIFO) order of element retrieval?",
                Marks = 1,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "Queue", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "Stack", IsCorrect = true },
                    new QuestionOption { OptionLabel = "C", OptionText = "Linked List", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "Binary Tree", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secCs.SectionId,
                QuestionText = "In modern .NET 8 C# architecture, what is the primary purpose of the Dependency Injection (DI) pattern?",
                Marks = 2,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "To achieve loose coupling, modularity, and testability across software layers", IsCorrect = true },
                    new QuestionOption { OptionLabel = "B", OptionText = "To encrypt database connection strings in memory", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "To compile C# IL bytecode into native machine instructions", IsCorrect = false },
                    new QuestionOption { OptionLabel = "D", OptionText = "To manage multi-threaded garbage collection cycles", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secCs.SectionId,
                QuestionText = "What is the computational time complexity of looking up a value in an optimized Hash Table under average circumstances?",
                Marks = 1,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "O(log n)", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "O(n)", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "O(1)", IsCorrect = true },
                    new QuestionOption { OptionLabel = "D", OptionText = "O(n log n)", IsCorrect = false }
                }
            },
            new Question
            {
                SectionId = secCs.SectionId,
                QuestionText = "In SQL Server, which type of index dictates the actual physical order of data pages stored on disk?",
                Marks = 2,
                CreatedByManagerId = manager1.ManagerId,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { OptionLabel = "A", OptionText = "Non-Clustered Index", IsCorrect = false },
                    new QuestionOption { OptionLabel = "B", OptionText = "Filtered Index", IsCorrect = false },
                    new QuestionOption { OptionLabel = "C", OptionText = "Clustered Index", IsCorrect = true },
                    new QuestionOption { OptionLabel = "D", OptionText = "Full-Text Index", IsCorrect = false }
                }
            }
        };

        context.Questions.AddRange(questions);
        await context.SaveChangesAsync();

        // 5. Seed Test Attempts (Simulated completed tests & transferred HR records)
        var alex = candidates[0];
        var attemptAlex = new TestAttempt
        {
            CandidateId = alex.CandidateId,
            CurrentSectionId = secCs.SectionId,
            StartedAt = DateTime.UtcNow.AddMinutes(-40),
            SectionStartedAt = DateTime.UtcNow.AddMinutes(-15),
            CompletedAt = DateTime.UtcNow.AddMinutes(-10),
            TotalScoreObtained = 20.00m,
            MaxPossibleScore = 23.00m,
            PercentageScore = 86.96m,
            IsPassed = true,
            AttemptStatus = AttemptStatusConstants.Completed
        };

        var priya = candidates[1];
        var attemptPriya = new TestAttempt
        {
            CandidateId = priya.CandidateId,
            CurrentSectionId = secCs.SectionId,
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            SectionStartedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            TotalScoreObtained = 23.00m,
            MaxPossibleScore = 23.00m,
            PercentageScore = 100.00m,
            IsPassed = true,
            AttemptStatus = AttemptStatusConstants.Completed
        };

        var marcus = candidates[2];
        var attemptMarcus = new TestAttempt
        {
            CandidateId = marcus.CandidateId,
            CurrentSectionId = secCs.SectionId,
            StartedAt = DateTime.UtcNow.AddMinutes(-50),
            SectionStartedAt = DateTime.UtcNow.AddMinutes(-20),
            CompletedAt = DateTime.UtcNow.AddMinutes(-15),
            TotalScoreObtained = 8.00m,
            MaxPossibleScore = 23.00m,
            PercentageScore = 34.78m,
            IsPassed = false,
            AttemptStatus = AttemptStatusConstants.Completed
        };

        context.TestAttempts.AddRange(attemptAlex, attemptPriya, attemptMarcus);
        await context.SaveChangesAsync();

        // 6. Seed Cleared Candidates (HR Review Pipeline)
        var clearedAlex = new AptiClearedCandidate
        {
            CandidateId = alex.CandidateId,
            AttemptId = attemptAlex.AttemptId,
            OverallScore = 20.00m,
            PercentageScore = 86.96m,
            ClearanceDate = DateTime.UtcNow.AddMinutes(-10),
            HrInterviewStatus = HrInterviewStatusConstants.Scheduled,
            HrRemarks = "Technical proficiency high in .NET & Algorithms. Scheduled HR round for Friday 10:00 AM."
        };

        var clearedPriya = new AptiClearedCandidate
        {
            CandidateId = priya.CandidateId,
            AttemptId = attemptPriya.AttemptId,
            OverallScore = 23.00m,
            PercentageScore = 100.00m,
            ClearanceDate = DateTime.UtcNow.AddMinutes(-5),
            HrInterviewStatus = HrInterviewStatusConstants.Pending,
            HrRemarks = "Perfect aptitude score across all 3 sections. Awaiting HR panel assignment."
        };

        context.AptiClearedCandidates.AddRange(clearedAlex, clearedPriya);
        await context.SaveChangesAsync();
    }
}
