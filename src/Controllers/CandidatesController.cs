using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webster.AptitudePortal.Api.Models.DTOs;
using Webster.AptitudePortal.Api.Services;

namespace Webster.AptitudePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager")]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;
    private readonly ILogger<CandidatesController> _logger;

    public CandidatesController(ICandidateService candidateService, ILogger<CandidatesController> logger)
    {
        _candidateService = candidateService;
        _logger = logger;
    }

    /// <summary>
    /// Enrolls a new candidate with educational qualifications, work experience, and generates access credentials.
    /// </summary>
    [HttpPost("enroll")]
    [ProducesResponseType(typeof(CandidateCredentialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EnrollCandidate([FromBody] CreateCandidateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(managerIdClaim, out int managerId))
        {
            return Unauthorized("Manager session identity is invalid.");
        }

        var response = await _candidateService.EnrollCandidateAsync(managerId, request);
        return CreatedAtAction(nameof(GetCandidateById), new { id = response.CandidateId }, response);
    }

    /// <summary>
    /// Retrieves all candidates with their current assessment progress
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CandidateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCandidates()
    {
        var list = await _candidateService.GetAllCandidatesAsync();
        return Ok(list);
    }

    /// <summary>
    /// Retrieves a complete candidate dossier including education, experience, and assessment attempt
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CandidateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidateById(int id)
    {
        var candidate = await _candidateService.GetCandidateByIdAsync(id);
        return Ok(candidate);
    }
}
