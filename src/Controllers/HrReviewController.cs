using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webster.AptitudePortal.Api.Data;
using Webster.AptitudePortal.Api.Models.DTOs;
using Webster.AptitudePortal.Api.Models.Entities;

namespace Webster.AptitudePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager")]
public class HrReviewController : ControllerBase
{
    private readonly AptitudeDbContext _dbContext;
    private readonly ILogger<HrReviewController> _logger;

    public HrReviewController(AptitudeDbContext dbContext, ILogger<HrReviewController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all candidates who passed the aptitude test and have been transferred for HR interview
    /// </summary>
    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(List<AptiClearedCandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHrPipeline([FromQuery] string? status)
    {
        var query = _dbContext.AptiClearedCandidates
            .Include(acc => acc.Candidate)
                .ThenInclude(c => c!.Educations)
            .Include(acc => acc.Candidate)
                .ThenInclude(c => c!.Experiences)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(acc => acc.HrInterviewStatus.ToLower() == status.ToLower());
        }

        var results = await query
            .OrderByDescending(acc => acc.ClearanceDate)
            .Select(acc => new AptiClearedCandidateDto(
                acc.ClearanceId,
                acc.CandidateId,
                acc.Candidate != null ? acc.Candidate.RegistrationNumber : "N/A",
                acc.Candidate != null ? acc.Candidate.FullName : "N/A",
                acc.Candidate != null ? acc.Candidate.Email : "N/A",
                acc.Candidate != null ? acc.Candidate.Phone : "N/A",
                acc.Candidate != null && acc.Candidate.Educations.Any()
                    ? acc.Candidate.Educations.OrderByDescending(e => e.PassingYear).First().DegreeName
                    : "Not Specified",
                acc.Candidate != null && acc.Candidate.Educations.Any()
                    ? acc.Candidate.Educations.OrderByDescending(e => e.PassingYear).First().InstituteName
                    : "Not Specified",
                acc.Candidate != null && acc.Candidate.Educations.Any()
                    ? acc.Candidate.Educations.OrderByDescending(e => e.PassingYear).First().PercentageOrCGPA
                    : 0.00m,
                acc.Candidate != null && acc.Candidate.Experiences.Any()
                    ? acc.Candidate.Experiences.Sum(x => x.TotalYears)
                    : 0.0m,
                acc.Candidate != null && acc.Candidate.Experiences.Any()
                    ? acc.Candidate.Experiences.First().KeySkills
                    : "Aptitude Evaluated",
                acc.OverallScore,
                acc.PercentageScore,
                acc.ClearanceDate,
                acc.HrInterviewStatus,
                acc.HrRemarks
            ))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Updates the HR interview outcome (Scheduled, Selected, Rejected) and remarks for a cleared candidate
    /// </summary>
    [HttpPatch("{clearanceId:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHrStatus(int clearanceId, [FromBody] UpdateHrStatusRequest request)
    {
        var record = await _dbContext.AptiClearedCandidates.FindAsync(clearanceId);
        if (record == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Clearance Record Not Found",
                Detail = $"No HR clearance record found with ID {clearanceId}."
            });
        }

        record.HrInterviewStatus = request.HrInterviewStatus;
        if (!string.IsNullOrWhiteSpace(request.HrRemarks))
        {
            record.HrRemarks = request.HrRemarks;
        }

        _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
        {
            CandidateId = record.CandidateId,
            ActionType = "HR_STATUS_UPDATED",
            SectionName = "HR_Round",
            Details = $"HR Interview status updated to '{request.HrInterviewStatus}'. Remarks: {request.HrRemarks}"
        });

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated HR status for Clearance {ClearanceId} to {Status}", clearanceId, request.HrInterviewStatus);

        return Ok(new
        {
            Message = $"Candidate HR status updated to '{request.HrInterviewStatus}'.",
            record.ClearanceId,
            record.CandidateId,
            record.HrInterviewStatus,
            record.HrRemarks
        });
    }
}
