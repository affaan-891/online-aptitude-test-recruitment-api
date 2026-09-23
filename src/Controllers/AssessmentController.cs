using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webster.AptitudePortal.Api.Filters;
using Webster.AptitudePortal.Api.Models.DTOs;
using Webster.AptitudePortal.Api.Services;

namespace Webster.AptitudePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Candidate")]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(IAssessmentService assessmentService, ILogger<AssessmentController> logger)
    {
        _assessmentService = assessmentService;
        _logger = logger;
    }

    /// <summary>
    /// Begins Round 1 (General Knowledge) or resumes active attempt with server-side countdown timer.
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartAssessmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> StartAssessment()
    {
        var candidateId = GetCurrentCandidateId();
        var response = await _assessmentService.StartAssessmentAsync(candidateId);
        return Ok(response);
    }

    /// <summary>
    /// Submits answers for current section, validates server timer, records marks, and unlocks next round.
    /// Strictly guarded against backward navigation or stage skipping.
    /// </summary>
    [HttpPost("submit-section")]
    [ValidateTestLinearity]
    [ProducesResponseType(typeof(SubmitSectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitSection([FromBody] SubmitSectionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var candidateId = GetCurrentCandidateId();
        var response = await _assessmentService.SubmitSectionAsync(candidateId, request);
        return Ok(response);
    }

    /// <summary>
    /// Finalizes the overall assessment, calculates cumulative score, and evaluates passing cutoff.
    /// If passed, automatically triggers transfer to AptiClearedCandidates for HR Round.
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(AssessmentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteAssessment()
    {
        var candidateId = GetCurrentCandidateId();
        var result = await _assessmentService.CompleteAssessmentAsync(candidateId);
        return Ok(result);
    }

    /// <summary>
    /// Checks candidate's active test status, remaining seconds, and progress.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AssessmentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus()
    {
        var candidateId = GetCurrentCandidateId();
        var status = await _assessmentService.GetAssessmentStatusAsync(candidateId);
        return Ok(status);
    }

    private int GetCurrentCandidateId()
    {
        var candidateIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(candidateIdClaim, out int candidateId))
        {
            throw new UnauthorizedAccessException("Valid candidate authentication token required.");
        }
        return candidateId;
    }
}
