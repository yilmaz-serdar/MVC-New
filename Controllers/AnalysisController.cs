using DependencyAnalyzer.Mvc.Models;
using DependencyAnalyzer.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace DependencyAnalyzer.Mvc.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AnalysisController : Controller
{
    private readonly AnalysisJobStore _store;
    private readonly AnalysisEmailSender _sender;
    public AnalysisController(AnalysisJobStore store, AnalysisEmailSender sender)
    { _store = store; _sender = sender; }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(new AnalysisPageViewModel
    { Request = new AnalysisRequest { ServiceName = "ACQUIRER_PROCESS_REST_001", Days = 1 }, History = await _store.HistoryAsync(null, cancellationToken) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze([Bind(Prefix = "Request")] AnalysisRequest request, CancellationToken cancellationToken, bool force = false)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", new AnalysisPageViewModel { Request = request, History = await _store.HistoryAsync(request.ServiceName, cancellationToken) });
        }
        var history = await _store.HistoryAsync(request.ServiceName, cancellationToken);
        if (!force && history.Count > 0)
        {
            ViewData["ExistingAnalysis"] = true;
            return View("Index", new AnalysisPageViewModel { Request = request, History = history });
        }
        var job = new AnalysisJob { Request = request };
        await _store.SaveAsync(job, cancellationToken);
        return RedirectToAction(nameof(Started), new { id = job.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Analyze(Guid? id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return NotFound("Analiz bulunamadı.");
        if (id is null) return RedirectToAction(nameof(Index));
        var job = await _store.GetAsync(id.Value, cancellationToken);
        if (job is null) return NotFound("Analiz bulunamadı.");
        if (job.State != "Completed" || job.Result is null) return StatusView(job);
        ViewData["EmailState"] = job.EmailState;
        return View("Index", new AnalysisPageViewModel { Request = job.Request, Result = job.Result, History = await _store.HistoryAsync(job.Request.ServiceName, cancellationToken) });
    }

    [HttpGet]
    public async Task<IActionResult> Started(Guid id, CancellationToken cancellationToken)
    {
        var job = await _store.GetAsync(id, cancellationToken);
        return job is null ? NotFound("Analiz bulunamadı.") : StatusView(job);
    }
    private IActionResult StatusView(AnalysisJob job)
    {
        ViewData["IsPickup"] = _sender.IsPickup;
        return View("Started", job);
    }
    public IActionResult Error() => View();
}
