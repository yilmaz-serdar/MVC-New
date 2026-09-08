namespace DependencyAnalyzer.Mvc.Services;

public sealed class AnalysisWorker : BackgroundService
{
    private readonly AnalysisJobStore _store;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AnalysisWorker> _logger;
    public AnalysisWorker(AnalysisJobStore store, IServiceScopeFactory scopes, ILogger<AnalysisWorker> logger)
    { _store = store; _scopes = scopes; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var id in _store.Ids())
            {
                try
                {
                    var job = await _store.GetAsync(id, stoppingToken);
                    if (job is null || job.State == "Failed" || (job.State == "Completed" && job.EmailState != "Pending")) continue;
                    using var scope = _scopes.CreateScope();
                    if (job.Result is null)
                    {
                        job.State = "Processing";
                        await _store.SaveAsync(job, stoppingToken);
                        try
                        {
                            job.Result = await scope.ServiceProvider.GetRequiredService<IAnalysisService>().AnalyzeAsync(job.Request, stoppingToken);
                            job.Result.Topology.Validate();
                            job.State = "Completed";
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            _logger.LogError(exception, "Analysis {Id} failed", id);
                            job.State = "Failed";
                            await _store.SaveAsync(job, stoppingToken);
                            continue;
                        }
                        await _store.SaveAsync(job, stoppingToken);
                    }
                    var sender = scope.ServiceProvider.GetRequiredService<AnalysisEmailSender>();
                    try
                    {
                        await sender.SendAsync(job, stoppingToken);
                        job.EmailState = sender.IsPickup ? "Prepared" : "Sent";
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        _logger.LogError(exception, "Email for analysis {Id} failed", id);
                        job.EmailState = "Failed";
                    }
                    await _store.SaveAsync(job, stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                { _logger.LogError(exception, "Could not process saved analysis {Id}", id); }
            }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
