using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Webster.AptitudePortal.Api.Data;
using Webster.AptitudePortal.Api.Models.DTOs;
using Webster.AptitudePortal.Api.Models.Entities;

namespace Webster.AptitudePortal.Api.Filters;

public class ValidateTestLinearityAttribute : TypeFilterAttribute
{
    public ValidateTestLinearityAttribute() : base(typeof(ValidateTestLinearityFilter))
    {
    }

    private class ValidateTestLinearityFilter : IAsyncActionFilter
    {
        private readonly AptitudeDbContext _dbContext;
        private readonly ILogger<ValidateTestLinearityFilter> _logger;

        public ValidateTestLinearityFilter(AptitudeDbContext dbContext, ILogger<ValidateTestLinearityFilter> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var candidateIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(candidateIdClaim) || !int.TryParse(candidateIdClaim, out int candidateId))
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Valid candidate authentication token is required."
                });
                return;
            }

            // Look up active test attempt
            var attempt = await _dbContext.TestAttempts
                .Include(a => a.CurrentSection)
                .FirstOrDefaultAsync(a => a.CandidateId == candidateId);

            if (attempt == null)
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "No Active Assessment Found",
                    Detail = "Candidate has not initiated the aptitude assessment. Call /api/assessment/start first."
                });
                return;
            }

            if (attempt.AttemptStatus != AttemptStatusConstants.InProgress)
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Assessment State",
                    Detail = $"Assessment cannot be modified. Current status: '{attempt.AttemptStatus}'."
                });
                return;
            }

            // If action parameter contains SubmitSectionRequest, validate requested SectionId matches CurrentSectionId
            if (context.ActionArguments.Values.FirstOrDefault(arg => arg is SubmitSectionRequest) is SubmitSectionRequest submitRequest)
            {
                if (submitRequest.SectionId != attempt.CurrentSectionId)
                {
                    _logger.LogWarning("Linearity violation by Candidate {CandidateId}. Active section {CurrentSection}, submitted {SubmittedSection}", 
                        candidateId, attempt.CurrentSectionId, submitRequest.SectionId);

                    context.Result = new BadRequestObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Linear Stage Violation",
                        Detail = $"Stage gating error: You are currently on section '{attempt.CurrentSection?.SectionName}' (ID: {attempt.CurrentSectionId}). Backward navigation or skipping ahead to Section {submitRequest.SectionId} is strictly blocked."
                    });
                    return;
                }
            }

            await next();
        }
    }
}
