using DependencyAnalyzer.Mvc.Models;

namespace DependencyAnalyzer.Mvc.Services;

public interface IAnalysisService
{
    Task<AnalysisResult> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}
