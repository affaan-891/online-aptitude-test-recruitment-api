using Webster.AptitudePortal.Api.Models.DTOs;

namespace Webster.AptitudePortal.Api.Services;

public interface ICandidateService
{
    Task<CandidateCredentialResponse> EnrollCandidateAsync(int managerId, CreateCandidateRequest request);
    Task<List<CandidateSummaryDto>> GetAllCandidatesAsync();
    Task<CandidateDetailDto> GetCandidateByIdAsync(int candidateId);
}
