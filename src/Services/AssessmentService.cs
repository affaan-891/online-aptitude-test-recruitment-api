using Microsoft.EntityFrameworkCore;
using Webster.AptitudePortal.Api.Data;
using Webster.AptitudePortal.Api.Models.DTOs;
using Webster.AptitudePortal.Api.Models.Entities;

namespace Webster.AptitudePortal.Api.Services;

public class AssessmentService : IAssessmentService
{
    private readonly AptitudeDbContext _dbContext;
    private readonly ILogger<AssessmentService> _logger;
    private const decimal CumulativePassingPercentage = 60.00m;
    private const int NetworkGraceSeconds = 15;

    public AssessmentService(AptitudeDbContext dbContext, ILogger<AssessmentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StartAssessmentResponse> StartAssessmentAsync(int candidateId)
    {
        var candidate = await _dbContext.Candidates.FindAsync(candidateId)
            ?? throw new KeyNotFoundException($"Candidate with ID {candidateId} not found.");

        var existingAttempt = await _dbContext.TestAttempts
            .Include(a => a.CurrentSection)
            .FirstOrDefaultAsync(a => a.CandidateId == candidateId);

        if (existingAttempt != null)
        {
            if (existingAttempt.AttemptStatus == AttemptStatusConstants.Completed)
            {
                throw new InvalidOperationException("You have already completed the aptitude assessment. Multiple attempts are strictly disallowed.");
            }

            // Return active section
            var activeSection = existingAttempt.CurrentSection!;
            var elapsed = (int)(DateTime.UtcNow - existingAttempt.SectionStartedAt).TotalSeconds;
            var remaining = Math.Max(0, (activeSection.TimeLimitMinutes * 60) - elapsed);

            var activeQuestions = await GetSanitizedQuestionsForSectionAsync(activeSection.SectionId);

            return new StartAssessmentResponse(
                existingAttempt.AttemptId,
                candidate.CandidateId,
                candidate.FullName,
                activeSection.SectionId,
                activeSection.SectionName,
                activeSection.SequenceOrder,
                activeQuestions.Count,
                activeSection.TimeLimitMinutes,
                existingAttempt.SectionStartedAt,
                remaining,
                activeQuestions,
                $"Resumed active section '{activeSection.SectionName}'. Complete all questions before the server countdown reaches 0."
            );
        }

        // Section 1: General Knowledge
        var firstSection = await _dbContext.TestSections
            .OrderBy(s => s.SequenceOrder)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No assessment sections are configured in the system.");

        var newAttempt = new TestAttempt
        {
            CandidateId = candidateId,
            CurrentSectionId = firstSection.SectionId,
            StartedAt = DateTime.UtcNow,
            SectionStartedAt = DateTime.UtcNow,
            AttemptStatus = AttemptStatusConstants.InProgress
        };

        _dbContext.TestAttempts.Add(newAttempt);

        _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
        {
            CandidateId = candidateId,
            ActionType = "ASSESSMENT_STARTED",
            SectionName = firstSection.SectionName,
            Details = $"Candidate started aptitude test Round 1: {firstSection.SectionName}."
        });

        await _dbContext.SaveChangesAsync();

        var questions = await GetSanitizedQuestionsForSectionAsync(firstSection.SectionId);

        return new StartAssessmentResponse(
            newAttempt.AttemptId,
            candidate.CandidateId,
            candidate.FullName,
            firstSection.SectionId,
            firstSection.SectionName,
            firstSection.SequenceOrder,
            questions.Count,
            firstSection.TimeLimitMinutes,
            newAttempt.SectionStartedAt,
            firstSection.TimeLimitMinutes * 60,
            questions,
            "Aptitude assessment commenced. You are currently on Round 1 (General Knowledge). Once submitted, this section will lock permanently."
        );
    }

    public async Task<SubmitSectionResponse> SubmitSectionAsync(int candidateId, SubmitSectionRequest request)
    {
        var attempt = await _dbContext.TestAttempts
            .Include(a => a.CurrentSection)
            .FirstOrDefaultAsync(a => a.AttemptId == request.AttemptId && a.CandidateId == candidateId)
            ?? throw new KeyNotFoundException($"Assessment attempt {request.AttemptId} not found for candidate.");

        if (attempt.AttemptStatus != AttemptStatusConstants.InProgress)
        {
            throw new InvalidOperationException($"Attempt is not in progress (Current status: {attempt.AttemptStatus}).");
        }

        if (attempt.CurrentSectionId != request.SectionId)
        {
            throw new InvalidOperationException($"Linearity violation: Submitted Section ID {request.SectionId} does not match active Section ID {attempt.CurrentSectionId}.");
        }

        var currentSection = attempt.CurrentSection!;

        // Validate server-side countdown timer
        var elapsedSeconds = (DateTime.UtcNow - attempt.SectionStartedAt).TotalSeconds;
        var maxAllowedSeconds = (currentSection.TimeLimitMinutes * 60) + NetworkGraceSeconds;

        if (elapsedSeconds > maxAllowedSeconds)
        {
            _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
            {
                CandidateId = candidateId,
                ActionType = "SECTION_TIMEOUT_FLAG",
                SectionName = currentSection.SectionName,
                Details = $"Candidate submitted section after time limit expired. Elapsed: {elapsedSeconds:F1}s, Allowed: {maxAllowedSeconds}s."
            });

            if (elapsedSeconds > (currentSection.TimeLimitMinutes * 60) + 120)
            {
                attempt.AttemptStatus = AttemptStatusConstants.TimedOut;
                attempt.CompletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                throw new InvalidOperationException("Time limit significantly exceeded. The assessment session has been locked as Timed Out.");
            }
        }

        // Fetch questions and correct options for grading
        var sectionQuestions = await _dbContext.Questions
            .Include(q => q.Options)
            .Where(q => q.SectionId == request.SectionId && q.IsActive)
            .ToListAsync();

        var existingResponses = await _dbContext.CandidateResponses
            .Where(r => r.AttemptId == attempt.AttemptId)
            .ToListAsync();

        foreach (var answer in request.Answers)
        {
            var question = sectionQuestions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
            if (question == null) continue;

            decimal marksAwarded = 0m;
            if (answer.SelectedOptionId.HasValue)
            {
                var chosenOption = question.Options.FirstOrDefault(o => o.OptionId == answer.SelectedOptionId.Value);
                if (chosenOption != null && chosenOption.IsCorrect)
                {
                    marksAwarded = question.Marks;
                }
            }

            var resp = existingResponses.FirstOrDefault(r => r.QuestionId == answer.QuestionId);
            if (resp != null)
            {
                resp.SelectedOptionId = answer.SelectedOptionId;
                resp.MarksAwarded = marksAwarded;
                resp.AnsweredAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.CandidateResponses.Add(new CandidateResponse
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId,
                    MarksAwarded = marksAwarded,
                    AnsweredAt = DateTime.UtcNow
                });
            }
        }

        // Check for next section in sequence
        var nextSection = await _dbContext.TestSections
            .Where(s => s.SequenceOrder > currentSection.SequenceOrder)
            .OrderBy(s => s.SequenceOrder)
            .FirstOrDefaultAsync();

        if (nextSection != null)
        {
            // Advance state machine to next round
            attempt.CurrentSectionId = nextSection.SectionId;
            attempt.SectionStartedAt = DateTime.UtcNow;

            _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
            {
                CandidateId = candidateId,
                ActionType = "SECTION_ADVANCED",
                SectionName = nextSection.SectionName,
                Details = $"Candidate successfully completed '{currentSection.SectionName}' and advanced to '{nextSection.SectionName}'."
            });

            await _dbContext.SaveChangesAsync();

            var nextQuestions = await GetSanitizedQuestionsForSectionAsync(nextSection.SectionId);

            return new SubmitSectionResponse(
                attempt.AttemptId,
                "TRANSITIONED_TO_NEXT_SECTION",
                nextSection.SectionId,
                nextSection.SectionName,
                nextSection.TimeLimitMinutes,
                nextSection.TimeLimitMinutes * 60,
                nextQuestions,
                $"Section '{currentSection.SectionName}' submitted and locked. You have advanced to Round {nextSection.SequenceOrder}: {nextSection.SectionName}."
            );
        }
        else
        {
            // Final round completed, ready for finalization
            await _dbContext.SaveChangesAsync();

            return new SubmitSectionResponse(
                attempt.AttemptId,
                "PENDING_FINALIZATION",
                null,
                null,
                null,
                0,
                null,
                "Section 3 (Computer Technology) completed. Call /api/assessment/complete to compute aggregate marks and receive qualification verdict."
            );
        }
    }

    public async Task<AssessmentResultDto> CompleteAssessmentAsync(int candidateId)
    {
        var candidate = await _dbContext.Candidates.FindAsync(candidateId)
            ?? throw new KeyNotFoundException($"Candidate with ID {candidateId} not found.");

        var attempt = await _dbContext.TestAttempts
            .Include(a => a.Responses)
            .FirstOrDefaultAsync(a => a.CandidateId == candidateId)
            ?? throw new KeyNotFoundException("No assessment attempt found for this candidate.");

        if (attempt.AttemptStatus == AttemptStatusConstants.Completed)
        {
            // Already finalized
            var msg = attempt.IsPassed
                ? "You have cleared this round, next round would be HR Round"
                : "Assessment completed. Unfortunately, you did not meet the required cutoff threshold.";

            return new AssessmentResultDto(
                attempt.AttemptId,
                candidate.CandidateId,
                candidate.FullName,
                candidate.RegistrationNumber,
                attempt.TotalScoreObtained,
                attempt.MaxPossibleScore,
                attempt.PercentageScore,
                attempt.IsPassed,
                attempt.AttemptStatus,
                msg,
                attempt.CompletedAt ?? DateTime.UtcNow
            );
        }

        // Sum marks awarded
        var totalObtained = attempt.Responses.Sum(r => r.MarksAwarded);

        // Calculate max possible score across all active questions in the system
        var totalPossible = await _dbContext.Questions
            .Where(q => q.IsActive)
            .SumAsync(q => (decimal)q.Marks);

        if (totalPossible <= 0) totalPossible = 15.00m;

        var percentage = Math.Round((totalObtained / totalPossible) * 100m, 2);
        var isPassed = percentage >= CumulativePassingPercentage;

        attempt.TotalScoreObtained = totalObtained;
        attempt.MaxPossibleScore = totalPossible;
        attempt.PercentageScore = percentage;
        attempt.IsPassed = isPassed;
        attempt.AttemptStatus = AttemptStatusConstants.Completed;
        attempt.CompletedAt = DateTime.UtcNow;

        string verdictMessage;
        if (isPassed)
        {
            verdictMessage = "You have cleared this round, next round would be HR Round";

            // Automated transfer engine to AptiClearedCandidates for HR evaluation
            var existingClearance = await _dbContext.AptiClearedCandidates
                .FirstOrDefaultAsync(acc => acc.CandidateId == candidateId);

            if (existingClearance == null)
            {
                var clearedCandidate = new AptiClearedCandidate
                {
                    CandidateId = candidateId,
                    AttemptId = attempt.AttemptId,
                    OverallScore = totalObtained,
                    PercentageScore = percentage,
                    ClearanceDate = DateTime.UtcNow,
                    HrInterviewStatus = HrInterviewStatusConstants.Pending,
                    HrRemarks = $"Automated Transfer: Cleared online aptitude assessment with {percentage}% aggregate. Transferred to HR round queue."
                };

                _dbContext.AptiClearedCandidates.Add(clearedCandidate);

                _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
                {
                    CandidateId = candidateId,
                    ActionType = "HR_PIPELINE_TRANSFER",
                    SectionName = "Final",
                    Details = "Candidate successfully transferred to AptiClearedCandidates table for HR round scheduling."
                });
            }
        }
        else
        {
            verdictMessage = "Assessment completed. Unfortunately, you did not meet the required cutoff threshold.";
        }

        _dbContext.AssessmentAuditLogs.Add(new AssessmentAuditLog
        {
            CandidateId = candidateId,
            ActionType = "ASSESSMENT_COMPLETED",
            SectionName = "Overall",
            Details = $"Assessment completed. Score: {totalObtained}/{totalPossible} ({percentage}%). Result: {(isPassed ? "PASSED" : "FAILED")}."
        });

        await _dbContext.SaveChangesAsync();

        return new AssessmentResultDto(
            attempt.AttemptId,
            candidate.CandidateId,
            candidate.FullName,
            candidate.RegistrationNumber,
            totalObtained,
            totalPossible,
            percentage,
            isPassed,
            attempt.AttemptStatus,
            verdictMessage,
            attempt.CompletedAt.Value
        );
    }

    public async Task<AssessmentStatusResponse> GetAssessmentStatusAsync(int candidateId)
    {
        var attempt = await _dbContext.TestAttempts
            .Include(a => a.CurrentSection)
            .FirstOrDefaultAsync(a => a.CandidateId == candidateId)
            ?? throw new KeyNotFoundException("No assessment record exists for this candidate.");

        var activeSection = attempt.CurrentSection!;
        var elapsed = (int)(DateTime.UtcNow - attempt.SectionStartedAt).TotalSeconds;
        var remaining = Math.Max(0, (activeSection.TimeLimitMinutes * 60) - elapsed);

        return new AssessmentStatusResponse(
            attempt.AttemptId,
            candidateId,
            attempt.AttemptStatus,
            activeSection.SectionId,
            activeSection.SectionName,
            activeSection.SequenceOrder,
            remaining,
            remaining <= 0,
            attempt.TotalScoreObtained,
            attempt.IsPassed
        );
    }

    private async Task<List<CandidateQuestionDto>> GetSanitizedQuestionsForSectionAsync(int sectionId)
    {
        var questions = await _dbContext.Questions
            .Include(q => q.Options)
            .Where(q => q.SectionId == sectionId && q.IsActive)
            .ToListAsync();

        return questions.Select(q => new CandidateQuestionDto(
            q.QuestionId,
            q.Marks,
            q.QuestionText,
            q.Options.OrderBy(o => o.OptionLabel).Select(o => new CandidateQuestionOptionDto(
                o.OptionId,
                o.OptionLabel,
                o.OptionText
            )).ToList()
        )).ToList();
    }
}
