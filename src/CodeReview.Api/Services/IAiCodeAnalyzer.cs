using CodeReview.Api.Models;

namespace CodeReview.Api.Services;

public interface IAiCodeAnalyzer
{
    Task<AnalysisResponse> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}
