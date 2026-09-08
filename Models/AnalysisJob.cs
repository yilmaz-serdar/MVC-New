namespace DependencyAnalyzer.Mvc.Models;

public sealed class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AnalysisRequest Request { get; set; } = new();
    public string State { get; set; } = "Pending";
    public string EmailState { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public AnalysisResult? Result { get; set; }
}
