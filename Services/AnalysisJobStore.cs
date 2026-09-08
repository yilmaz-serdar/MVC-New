using System.Text.Json;
using DependencyAnalyzer.Mvc.Models;

namespace DependencyAnalyzer.Mvc.Services;

// Single-instance local persistence. Use a database/queue for multiple application instances.
public sealed class AnalysisJobStore
{
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public AnalysisJobStore(IWebHostEnvironment environment)
    {
        _directory = Path.Combine(environment.ContentRootPath, "App_Data", "Jobs");
        Directory.CreateDirectory(_directory);
    }
    public async Task SaveAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(_directory, $"{job.Id:N}.json");
            var temporary = path + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(job), cancellationToken);
            File.Move(temporary, path, true);
        }
        finally { _gate.Release(); }
    }
    public async Task<AnalysisJob?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(_directory, $"{id:N}.json");
            return File.Exists(path) ? JsonSerializer.Deserialize<AnalysisJob>(await File.ReadAllTextAsync(path, cancellationToken)) : null;
        }
        finally { _gate.Release(); }
    }
    public async Task<IReadOnlyList<AnalysisJob>> HistoryAsync(string? serviceName, CancellationToken cancellationToken)
    {
        var jobs = new List<AnalysisJob>();
        foreach (var id in Ids())
        {
            var job = await GetAsync(id, cancellationToken);
            if (job?.State == "Completed" && job.Result is not null &&
                (string.IsNullOrWhiteSpace(serviceName) || string.Equals(job.Request.ServiceName.Trim(), serviceName.Trim(), StringComparison.OrdinalIgnoreCase)))
                jobs.Add(job);
        }
        return jobs.OrderByDescending(job => job.CreatedAt).ToArray();
    }
    public IEnumerable<Guid> Ids() => Directory.EnumerateFiles(_directory, "*.json")
        .Select(Path.GetFileNameWithoutExtension).Where(name => Guid.TryParse(name, out _)).Select(name => Guid.Parse(name!)).ToArray();
}
