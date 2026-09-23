using System.Security.Claims;
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
public class QuestionsController : ControllerBase
{
    private readonly AptitudeDbContext _dbContext;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(AptitudeDbContext dbContext, ILogger<QuestionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all questions across sections, optionally filtered by Section ID
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<QuestionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions([FromQuery] int? sectionId)
    {
        var query = _dbContext.Questions
            .Include(q => q.Section)
            .Include(q => q.Options)
            .AsQueryable();

        if (sectionId.HasValue)
        {
            query = query.Where(q => q.SectionId == sectionId.Value);
        }

        var results = await query
            .OrderBy(q => q.SectionId)
            .ThenBy(q => q.QuestionId)
            .Select(q => new QuestionDetailDto(
                q.QuestionId,
                q.SectionId,
                q.Section != null ? q.Section.SectionName : "Unknown",
                q.QuestionText,
                q.Marks,
                q.IsActive,
                q.UpdatedAt,
                q.Options.OrderBy(o => o.OptionLabel).Select(o => new QuestionOptionDto(
                    o.OptionId, o.OptionLabel, o.OptionText, o.IsCorrect
                )).ToList()
            ))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Retrieves a specific question by ID with all options and answer key
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(QuestionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionById(int id)
    {
        var q = await _dbContext.Questions
            .Include(q => q.Section)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.QuestionId == id);

        if (q == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Question Not Found",
                Detail = $"Question with ID {id} does not exist."
            });
        }

        var result = new QuestionDetailDto(
            q.QuestionId,
            q.SectionId,
            q.Section?.SectionName ?? "Unknown",
            q.QuestionText,
            q.Marks,
            q.IsActive,
            q.UpdatedAt,
            q.Options.OrderBy(o => o.OptionLabel).Select(o => new QuestionOptionDto(
                o.OptionId, o.OptionLabel, o.OptionText, o.IsCorrect
            )).ToList()
        );

        return Ok(result);
    }

    /// <summary>
    /// Creates a new question in the specified section with options and marks
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(QuestionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var sectionExists = await _dbContext.TestSections.AnyAsync(s => s.SectionId == request.SectionId);
        if (!sectionExists)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Section",
                Detail = $"Section with ID {request.SectionId} does not exist."
            });
        }

        if (request.Options.Count(o => o.IsCorrect) != 1)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Options Configuration",
                Detail = "A question must have exactly one correct option marked."
            });
        }

        var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(managerIdClaim, out int managerId);

        var question = new Question
        {
            SectionId = request.SectionId,
            QuestionText = request.QuestionText.Trim(),
            Marks = request.Marks,
            IsActive = true,
            CreatedByManagerId = managerId > 0 ? managerId : 1,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var opt in request.Options)
        {
            question.Options.Add(new QuestionOption
            {
                OptionLabel = opt.OptionLabel.ToUpperInvariant(),
                OptionText = opt.OptionText.Trim(),
                IsCorrect = opt.IsCorrect
            });
        }

        _dbContext.Questions.Add(question);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Manager {ManagerId} created Question {QuestionId} in Section {SectionId}", managerId, question.QuestionId, question.SectionId);

        return CreatedAtAction(nameof(GetQuestionById), new { id = question.QuestionId }, question);
    }

    /// <summary>
    /// Updates question details, marks, active status, or option definitions
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(QuestionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] UpdateQuestionRequest request)
    {
        var question = await _dbContext.Questions
            .Include(q => q.Options)
            .Include(q => q.Section)
            .FirstOrDefaultAsync(q => q.QuestionId == id);

        if (question == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Question Not Found",
                Detail = $"Question with ID {id} does not exist."
            });
        }

        if (!string.IsNullOrWhiteSpace(request.QuestionText))
        {
            question.QuestionText = request.QuestionText.Trim();
        }

        if (request.Marks.HasValue)
        {
            question.Marks = request.Marks.Value;
        }

        if (request.IsActive.HasValue)
        {
            question.IsActive = request.IsActive.Value;
        }

        if (request.Options != null && request.Options.Any())
        {
            if (request.Options.Count(o => o.IsCorrect) != 1)
            {
                return BadRequest("A question must have exactly one correct option.");
            }

            _dbContext.QuestionOptions.RemoveRange(question.Options);

            foreach (var opt in request.Options)
            {
                question.Options.Add(new QuestionOption
                {
                    OptionLabel = opt.OptionLabel.ToUpperInvariant(),
                    OptionText = opt.OptionText.Trim(),
                    IsCorrect = opt.IsCorrect
                });
            }
        }

        question.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new QuestionDetailDto(
            question.QuestionId,
            question.SectionId,
            question.Section?.SectionName ?? "Unknown",
            question.QuestionText,
            question.Marks,
            question.IsActive,
            question.UpdatedAt,
            question.Options.OrderBy(o => o.OptionLabel).Select(o => new QuestionOptionDto(
                o.OptionId, o.OptionLabel, o.OptionText, o.IsCorrect
            )).ToList()
        ));
    }

    /// <summary>
    /// Soft-deletes a question by marking IsActive = false
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _dbContext.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound();
        }

        question.IsActive = false;
        question.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
