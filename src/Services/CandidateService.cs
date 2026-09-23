using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Webster.AptitudePortal.Api.Data;
using Webster.AptitudePortal.Api.Models.DTOs;
using Webster.AptitudePortal.Api.Models.Entities;

namespace Webster.AptitudePortal.Api.Services;

public class CandidateService : ICandidateService
{
    private readonly AptitudeDbContext _dbContext;
    private readonly ILogger<CandidateService> _logger;

    public CandidateService(AptitudeDbContext dbContext, ILogger<CandidateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CandidateCredentialResponse> EnrollCandidateAsync(int managerId, CreateCandidateRequest request)
    {
        // 1. Validation for duplicate email, phone
        if (await _dbContext.Candidates.AnyAsync(c => c.Email == request.Email))
        {
            throw new InvalidOperationException($"A candidate with email '{request.Email}' already exists in the system.");
        }

        if (await _dbContext.Candidates.AnyAsync(c => c.Phone == request.Phone))
        {
            throw new InvalidOperationException($"A candidate with phone number '{request.Phone}' already exists in the system.");
        }

        // 2. Generate Unique Registration Number
        var candidateCount = await _dbContext.Candidates.CountAsync();
        var regNo = $"WEB-{DateTime.UtcNow.Year}-{(candidateCount + 1):D3}";

        // 3. Username generation or validation
        string username = string.IsNullOrWhiteSpace(request.CustomUsername)
            ? GenerateCleanUsername(request.FullName)
            : request.CustomUsername.Trim().ToLowerInvariant();

        if (await _dbContext.Candidates.AnyAsync(c => c.Username == username))
        {
            username = $"{username}.{RandomNumberGenerator.GetInt32(100, 999)}";
        }

        // 4. Generate random password if not supplied
        string plainPassword = string.IsNullOrWhiteSpace(request.PlainPassword)
            ? $"Webster@{RandomNumberGenerator.GetInt32(1000, 9999)}#"
            : request.PlainPassword;

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        // 5. Create Candidate record
        var candidate = new Candidate
        {
            RegistrationNumber = regNo,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            DateOfBirth = request.DateOfBirth,
            CurrentAddress = request.CurrentAddress.Trim(),
            Username = username,
            PasswordHash = hashedPassword,
            CreatedByManagerId = managerId,
            EnrolledAt = DateTime.UtcNow
        };

        // Add Educations
        if (request.Educations != null && request.Educations.Any())
        {
            foreach (var edu in request.Educations)
            {
                candidate.Educations.Add(new CandidateEducation
                {
                    DegreeName = edu.DegreeName.Trim(),
                    InstituteName = edu.InstituteName.Trim(),
                    PassingYear = edu.PassingYear,
                    PercentageOrCGPA = edu.PercentageOrCGPA,
                    Specialization = edu.Specialization.Trim()
                });
            }
        }

        // Add Experiences
        if (request.Experiences != null && request.Experiences.Any())
        {
            foreach (var exp in request.Experiences)
            {
                candidate.Experiences.Add(new CandidateExperience
                {
                    CompanyName = exp.CompanyName.Trim(),
                    Designation = exp.Designation.Trim(),
                    TotalYears = exp.TotalYears,
                    KeySkills = exp.KeySkills.Trim()
                });
            }
        }

        _dbContext.Candidates.Add(candidate);

        _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
        {
            ActionType = "CANDIDATE_ENROLLED",
            SectionName = "Administration",
            Details = $"Candidate '{candidate.FullName}' (Reg: {regNo}) enrolled by Manager ID {managerId}."
        });

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Enrolled Candidate {CandidateId} ({RegNo}) by Manager {ManagerId}", candidate.CandidateId, regNo, managerId);

        return new CandidateCredentialResponse(
            candidate.CandidateId,
            candidate.RegistrationNumber,
            candidate.FullName,
            candidate.Username,
            plainPassword,
            candidate.Email,
            "/api/auth/login",
            "Candidate enrolled successfully. Provide these credentials to the applicant to begin the assessment."
        );
    }

    public async Task<List<CandidateSummaryDto>> GetAllCandidatesAsync()
    {
        return await _dbContext.Candidates
            .Include(c => c.TestAttempt)
            .OrderByDescending(c => c.EnrolledAt)
            .Select(c => new CandidateSummaryDto(
                c.CandidateId,
                c.RegistrationNumber,
                c.FullName,
                c.Email,
                c.Phone,
                c.EnrolledAt,
                c.TestAttempt != null ? c.TestAttempt.AttemptStatus : AttemptStatusConstants.NotStarted,
                c.TestAttempt != null && c.TestAttempt.IsPassed
            ))
            .ToListAsync();
    }

    public async Task<CandidateDetailDto> GetCandidateByIdAsync(int candidateId)
    {
        var candidate = await _dbContext.Candidates
            .Include(c => c.CreatedByManager)
            .Include(c => c.Educations)
            .Include(c => c.Experiences)
            .Include(c => c.TestAttempt)
            .FirstOrDefaultAsync(c => c.CandidateId == candidateId)
            ?? throw new KeyNotFoundException($"Candidate with ID {candidateId} not found.");

        return new CandidateDetailDto(
            candidate.CandidateId,
            candidate.RegistrationNumber,
            candidate.FullName,
            candidate.Email,
            candidate.Phone,
            candidate.DateOfBirth,
            candidate.CurrentAddress,
            candidate.Username,
            candidate.EnrolledAt,
            candidate.CreatedByManager?.FullName ?? "System",
            candidate.Educations.Select(e => new CandidateEducationDto(
                e.DegreeName, e.InstituteName, e.PassingYear, e.PercentageOrCGPA, e.Specialization
            )).ToList(),
            candidate.Experiences.Select(x => new CandidateExperienceDto(
                x.CompanyName, x.Designation, x.TotalYears, x.KeySkills
            )).ToList(),
            candidate.TestAttempt?.AttemptStatus,
            candidate.TestAttempt?.PercentageScore,
            candidate.TestAttempt?.IsPassed
        );
    }

    private static string GenerateCleanUsername(string fullName)
    {
        var parts = fullName.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0]}.{parts[^1]}";
        }
        return parts.Length == 1 ? parts[0] : "applicant";
    }
}
