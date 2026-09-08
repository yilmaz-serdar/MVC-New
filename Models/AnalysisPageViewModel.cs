namespace DependencyAnalyzer.Mvc.Models;

public sealed class AnalysisPageViewModel
{
    public AnalysisRequest Request { get; set; } = new();
    public AnalysisResult? Result { get; set; }
    public IReadOnlyList<int> DurationOptions { get; } = new[] { 1, 3, 7 };
}
