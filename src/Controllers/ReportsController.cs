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
public class ReportsController : ControllerBase
{
    private readonly AptitudeDbContext _dbContext;

    public ReportsController(AptitudeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates recruitment funnel analytics filtered by DAILY, WEEKLY, or MONTHLY
    /// </summary>
    [HttpGet("recruitment-summary")]
    [ProducesResponseType(typeof(RecruitmentSummaryReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecruitmentSummary([FromQuery] string filterType = "WEEKLY")
    {
        var normalizedFilter = filterType.ToUpperInvariant();
        var now = DateTime.UtcNow;
        DateTime startDate = normalizedFilter switch
        {
            "DAILY" => now.AddDays(-1),
            "WEEKLY" => now.AddDays(-7),
            "MONTHLY" => now.AddMonths(-1),
            _ => now.AddDays(-7)
        };

        var totalEnrolled = await _dbContext.Candidates
            .CountAsync(c => c.EnrolledAt >= startDate);

        var totalAppeared = await _dbContext.TestAttempts
            .CountAsync(a => a.StartedAt >= startDate && a.AttemptStatus != AttemptStatusConstants.NotStarted);

        var totalCleared = await _dbContext.TestAttempts
            .CountAsync(a => a.CompletedAt >= startDate && a.AttemptStatus == AttemptStatusConstants.Completed && a.IsPassed);

        var totalFailed = await _dbContext.TestAttempts
            .CountAsync(a => a.CompletedAt >= startDate && a.AttemptStatus == AttemptStatusConstants.Completed && !a.IsPassed);

        var totalTransferredToHr = await _dbContext.AptiClearedCandidates
            .CountAsync(acc => acc.ClearanceDate >= startDate);

        var completedAttempts = await _dbContext.TestAttempts
            .Where(a => a.CompletedAt >= startDate && a.AttemptStatus == AttemptStatusConstants.Completed)
            .ToListAsync();

        decimal avgPercentage = completedAttempts.Any()
            ? Math.Round(completedAttempts.Average(a => a.PercentageScore), 2)
            : 0.00m;

        decimal topScore = completedAttempts.Any()
            ? completedAttempts.Max(a => a.TotalScoreObtained)
            : 0.00m;

        decimal conversionRate = totalEnrolled > 0
            ? Math.Round(((decimal)totalTransferredToHr / totalEnrolled) * 100m, 2)
            : 0.00m;

        var report = new RecruitmentSummaryReportDto(
            normalizedFilter,
            startDate,
            now,
            totalEnrolled,
            totalAppeared,
            totalCleared,
            totalFailed,
            totalTransferredToHr,
            avgPercentage,
            topScore,
            conversionRate
        );

        return Ok(report);
    }

    /// <summary>
    /// Aggregates sectional performance benchmark metrics across GK, Math, and CS
    /// </summary>
    [HttpGet("sectional-benchmarks")]
    public async Task<IActionResult> GetSectionalBenchmarks()
    {
        var sections = await _dbContext.TestSections
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync();

        var questions = await _dbContext.Questions.ToListAsync();
        var responses = await _dbContext.CandidateResponses.ToListAsync();

        var benchmarks = sections.Select(s =>
        {
            var secQuestions = questions.Where(q => q.SectionId == s.SectionId).ToList();
            var secQuestionIds = secQuestions.Select(q => q.QuestionId).ToHashSet();
            var secResponses = responses.Where(r => secQuestionIds.Contains(r.QuestionId)).ToList();

            var correctCount = secResponses.Count(r => r.MarksAwarded > 0);
            var totalCount = secResponses.Count;
            var accuracy = totalCount > 0 ? Math.Round(((decimal)correctCount / totalCount) * 100m, 2) : 0m;

            return new
            {
                s.SectionId,
                s.SectionName,
                s.SequenceOrder,
                s.TimeLimitMinutes,
                TotalQuestions = secQuestions.Count,
                TotalMarks = secQuestions.Sum(q => q.Marks),
                ResponsesEvaluated = totalCount,
                AccuracyPercentage = accuracy
            };
        });

        return Ok(benchmarks);
    }
}
