using System.Text.Json;
using DependencyAnalyzer.Mvc.Models;

namespace DependencyAnalyzer.Mvc.Services;

// Server-side fixture provider. Replace its stream with your HTTP response body when
// the endpoint and request contract are available. Browser code never reads this file.
public sealed class JsonAnalysisService : IAnalysisService
{
    private readonly IWebHostEnvironment _environment;

    public JsonAnalysisService(IWebHostEnvironment environment) => _environment = environment;

    public async Task<AnalysisResult> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_environment.ContentRootPath, "Data", "service-topology.json");
        await using var stream = File.OpenRead(path);
        var topology = await JsonSerializer.DeserializeAsync<ServiceTopologyResponse>(stream,
            cancellationToken: cancellationToken) ?? throw new InvalidDataException("Empty topology response.");
        topology.Validate();
        var to = DateTimeOffset.UtcNow;
        return new AnalysisResult
        {
            ServiceName = request.ServiceName.Trim(), Days = request.Days,
            From = to.AddDays(-request.Days), To = to, IsDemo = true, Topology = topology
        };
    }
}
