using System.ComponentModel.DataAnnotations;

namespace Webster.AptitudePortal.Api.Models.DTOs;

// ============================================================================
// AUTHENTICATION DTOS
// ============================================================================
public record LoginRequest(
    [Required] string Identifier, // Email for Manager, Username or Email for Candidate
    [Required] string Password
);

public record LoginResponse(
    string Token,
    string Role,
    int UserId,
    string FullName,
    string Email,
    DateTime ExpiresAtUtc
);

// ============================================================================
// CANDIDATE PROFILE & CREDENTIAL DTOS
// ============================================================================
public record CandidateEducationDto(
    [Required] string DegreeName,
    [Required] string InstituteName,
    [Range(1970, 2100)] int PassingYear,
    [Range(0, 100)] decimal PercentageOrCGPA,
    [Required] string Specialization
);

public record CandidateExperienceDto(
    [Required] string CompanyName,
    [Required] string Designation,
    [Range(0, 50)] decimal TotalYears,
    [Required] string KeySkills
);

public record CreateCandidateRequest(
    [Required, MaxLength(100)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, Phone] string Phone,
    [Required] DateTime DateOfBirth,
    [Required, MaxLength(250)] string CurrentAddress,
    string? CustomUsername, // Auto-generated if null
    string? PlainPassword,  // Auto-generated if null
    List<CandidateEducationDto>? Educations,
    List<CandidateExperienceDto>? Experiences
);

public record CandidateCredentialResponse(
    int CandidateId,
    string RegistrationNumber,
    string FullName,
    string Username,
    string GeneratedPassword,
    string Email,
    string LoginUrl,
    string Message
);

public record CandidateDetailDto(
    int CandidateId,
    string RegistrationNumber,
    string FullName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    string CurrentAddress,
    string Username,
    DateTime EnrolledAt,
    string EnrolledByManager,
    List<CandidateEducationDto> Educations,
    List<CandidateExperienceDto> Experiences,
    string? TestStatus,
    decimal? PercentageScore,
    bool? IsPassed
);

public record CandidateSummaryDto(
    int CandidateId,
    string RegistrationNumber,
    string FullName,
    string Email,
    string Phone,
    DateTime EnrolledAt,
    string TestStatus,
    bool IsPassed
);

// ============================================================================
// QUESTION BANK DTOS
// ============================================================================
public record QuestionOptionCreateDto(
    [Required, RegularExpression("^[A-D]$")] string OptionLabel,
    [Required] string OptionText,
    bool IsCorrect
);

public record QuestionOptionDto(
    int OptionId,
    string OptionLabel,
    string OptionText,
    bool IsCorrect
);

public record CreateQuestionRequest(
    [Range(1, 3)] int SectionId,
    [Required] string QuestionText,
    [Range(1, 5)] int Marks,
    [Required, MinLength(4)] List<QuestionOptionCreateDto> Options
);

public record UpdateQuestionRequest(
    string? QuestionText,
    [Range(1, 5)] int? Marks,
    bool? IsActive,
    List<QuestionOptionCreateDto>? Options
);

public record QuestionDetailDto(
    int QuestionId,
    int SectionId,
    string SectionName,
    string QuestionText,
    int Marks,
    bool IsActive,
    DateTime UpdatedAt,
    List<QuestionOptionDto> Options
);

// Candidate Secure Question View (Does NOT reveal correct answers)
public record CandidateQuestionOptionDto(
    int OptionId,
    string OptionLabel,
    string OptionText
);

public record CandidateQuestionDto(
    int QuestionId,
    int Marks,
    string QuestionText,
    List<CandidateQuestionOptionDto> Options
);

// ============================================================================
// ASSESSMENT ENGINE DTOS
// ============================================================================
public record StartAssessmentResponse(
    int AttemptId,
    int CandidateId,
    string CandidateName,
    int SectionId,
    string SectionName,
    int SequenceOrder,
    int TotalQuestions,
    int TimeLimitMinutes,
    DateTime SectionStartedAtUtc,
    int RemainingSeconds,
    List<CandidateQuestionDto> Questions,
    string Instructions
);

public record QuestionAnswerDto(
    int QuestionId,
    int? SelectedOptionId
);

public record SubmitSectionRequest(
    int AttemptId,
    int SectionId,
    [Required] List<QuestionAnswerDto> Answers
);

public record SubmitSectionResponse(
    int AttemptId,
    string Status, // 'TRANSITIONED_TO_NEXT_SECTION' or 'PENDING_FINALIZATION'
    int? NextSectionId,
    string? NextSectionName,
    int? TimeLimitMinutes,
    int? RemainingSeconds,
    List<CandidateQuestionDto>? NextSectionQuestions,
    string Message
);

public record AssessmentResultDto(
    int AttemptId,
    int CandidateId,
    string CandidateName,
    string RegistrationNumber,
    decimal TotalScoreObtained,
    decimal MaxPossibleScore,
    decimal PercentageScore,
    bool IsPassed,
    string AttemptStatus,
    string VerdictMessage,
    DateTime CompletedAtUtc
);

public record AssessmentStatusResponse(
    int AttemptId,
    int CandidateId,
    string AttemptStatus,
    int CurrentSectionId,
    string CurrentSectionName,
    int CurrentSequenceOrder,
    int RemainingSeconds,
    bool IsTimeExpired,
    decimal TotalScoreObtained,
    bool IsPassed
);

// ============================================================================
// REPORTS & HR CLEARANCE DTOS
// ============================================================================
public record RecruitmentSummaryReportDto(
    string FilterType,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int TotalEnrolledCandidates,
    int TotalAppearedForAssessment,
    int TotalClearedAptitude,
    int TotalFailedAptitude,
    int TotalTransferredToHr,
    decimal AverageCandidatePercentage,
    decimal HighestScoreObtained,
    decimal FunnelConversionRatePercentage
);

public record AptiClearedCandidateDto(
    int ClearanceId,
    int CandidateId,
    string RegistrationNumber,
    string FullName,
    string Email,
    string Phone,
    string HighestDegree,
    string InstituteName,
    decimal AcademicPercentage,
    decimal TotalExperienceYears,
    string KeySkills,
    decimal OverallScore,
    decimal PercentageScore,
    DateTime ClearanceDateUtc,
    string HrInterviewStatus,
    string? HrRemarks
);

public record UpdateHrStatusRequest(
    [Required, RegularExpression("^(Pending|Scheduled|Selected|Rejected)$")] string HrInterviewStatus,
    string? HrRemarks
);
