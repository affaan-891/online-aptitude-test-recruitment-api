using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webster.AptitudePortal.Api.Models.Entities;

public enum SectionType
{
    General_Knowledge = 1,
    Mathematics = 2,
    Computer_Technology = 3
}

public static class AttemptStatusConstants
{
    public const string NotStarted = "Not_Started";
    public const string InProgress = "In_Progress";
    public const string TimedOut = "Timed_Out";
    public const string Completed = "Completed";
}

public static class HrInterviewStatusConstants
{
    public const string Pending = "Pending";
    public const string Scheduled = "Scheduled";
    public const string Selected = "Selected";
    public const string Rejected = "Rejected";
}

[Table("Managers")]
public class Manager
{
    [Key]
    public int ManagerId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string BranchLocation { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Candidate> CandidatesEnrolled { get; set; } = new List<Candidate>();
    public ICollection<Question> QuestionsCreated { get; set; } = new List<Question>();
}

[Table("Candidates")]
public class Candidate
{
    [Key]
    public int CandidateId { get; set; }

    [Required, MaxLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    [Required, MaxLength(250)]
    public string CurrentAddress { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public int CreatedByManagerId { get; set; }
    public Manager? CreatedByManager { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public ICollection<CandidateEducation> Educations { get; set; } = new List<CandidateEducation>();
    public ICollection<CandidateExperience> Experiences { get; set; } = new List<CandidateExperience>();
    public TestAttempt? TestAttempt { get; set; }
    public AptiClearedCandidate? AptiClearedRecord { get; set; }
}

[Table("CandidateEducations")]
public class CandidateEducation
{
    [Key]
    public int EducationId { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [Required, MaxLength(100)]
    public string DegreeName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string InstituteName { get; set; } = string.Empty;

    [Range(1970, 2100)]
    public int PassingYear { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PercentageOrCGPA { get; set; }

    [Required, MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;
}

[Table("CandidateExperiences")]
public class CandidateExperience
{
    [Key]
    public int ExperienceId { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [Required, MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Designation { get; set; } = string.Empty;

    [Column(TypeName = "decimal(4,1)")]
    public decimal TotalYears { get; set; }

    [Required, MaxLength(250)]
    public string KeySkills { get; set; } = string.Empty;
}

[Table("TestSections")]
public class TestSection
{
    [Key]
    public int SectionId { get; set; }

    [Required, MaxLength(50)]
    public string SectionName { get; set; } = string.Empty;

    public int SequenceOrder { get; set; }

    public int TotalQuestions { get; set; } = 5;

    public int TimeLimitMinutes { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CutoffPercentage { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

[Table("Questions")]
public class Question
{
    [Key]
    public int QuestionId { get; set; }

    public int SectionId { get; set; }
    public TestSection? Section { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Marks { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public int CreatedByManagerId { get; set; }
    public Manager? CreatedByManager { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}

[Table("QuestionOptions")]
public class QuestionOption
{
    [Key]
    public int OptionId { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    [Required, MaxLength(1)]
    public string OptionLabel { get; set; } = string.Empty; // 'A', 'B', 'C', 'D'

    [Required, MaxLength(500)]
    public string OptionText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; } = false;
}

[Table("TestAttempts")]
public class TestAttempt
{
    [Key]
    public int AttemptId { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    public int CurrentSectionId { get; set; }
    public TestSection? CurrentSection { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime SectionStartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal TotalScoreObtained { get; set; } = 0.00m;

    [Column(TypeName = "decimal(6,2)")]
    public decimal MaxPossibleScore { get; set; } = 0.00m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal PercentageScore { get; set; } = 0.00m;

    public bool IsPassed { get; set; } = false;

    [Required, MaxLength(30)]
    public string AttemptStatus { get; set; } = AttemptStatusConstants.NotStarted;

    public ICollection<CandidateResponse> Responses { get; set; } = new List<CandidateResponse>();
    public AptiClearedCandidate? AptiClearedRecord { get; set; }
}

[Table("CandidateResponses")]
public class CandidateResponse
{
    [Key]
    public int ResponseId { get; set; }

    public int AttemptId { get; set; }
    public TestAttempt? Attempt { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public int? SelectedOptionId { get; set; }
    public QuestionOption? SelectedOption { get; set; }

    [Column(TypeName = "decimal(4,2)")]
    public decimal MarksAwarded { get; set; } = 0.00m;

    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

[Table("AptiClearedCandidates")]
public class AptiClearedCandidate
{
    [Key]
    public int ClearanceId { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    public int AttemptId { get; set; }
    public TestAttempt? Attempt { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal OverallScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PercentageScore { get; set; }

    public DateTime ClearanceDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(30)]
    public string HrInterviewStatus { get; set; } = HrInterviewStatusConstants.Pending;

    public string? HrRemarks { get; set; }
}

[Table("AssessmentAuditLogs")]
public class AssessmentAuditLog
{
    [Key]
    public int LogId { get; set; }

    public int? CandidateId { get; set; }

    [Required, MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SectionName { get; set; }

    public string? Details { get; set; }

    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
