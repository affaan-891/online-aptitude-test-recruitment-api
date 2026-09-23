using Webster.AptitudePortal.Api.Models.DTOs;

namespace Webster.AptitudePortal.Api.Services;

public interface IAssessmentService
{
    Task<StartAssessmentResponse> StartAssessmentAsync(int candidateId);
    Task<SubmitSectionResponse> SubmitSectionAsync(int candidateId, SubmitSectionRequest request);
    Task<AssessmentResultDto> CompleteAssessmentAsync(int candidateId);
    Task<AssessmentStatusResponse> GetAssessmentStatusAsync(int candidateId);
}
