using DependencyAnalyzer.Mvc.Models;

namespace DependencyAnalyzer.Mvc.Services;

// Replace this implementation with your database/HTTP analysis provider.
// No traffic data is generated in the browser.
public sealed class DemoAnalysisService : IAnalysisService
{
    public Task<AnalysisResult> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var name = request.ServiceName.Trim();
        var days = request.Days;
        var to = DateTimeOffset.UtcNow;
        var nodes = new ServiceNode[]
        {
            new("root", name, "Analiz edilen servis", 24860L * days, 124, true, new[] { "auth", "customer", "transfer" }),
            new("auth", "Auth-Service", "Mobil-Login", 12480L * days, 42, false, new[] { "token", "session" }),
            new("customer", "Customer-Service", "Customer-Info", 8260L * days, 68, false, new[] { "profile" }),
            new("transfer", "Transfer-Service", "Card-EFT", 4120L * days, 96, false, new[] { "account", "payment" }),
            new("token", "Token-Service", "Mobil-Login", 9340L * days, 18, false, Array.Empty<string>()),
            new("session", "Session-Service", "Mobil-Login", 3140L * days, 24, false, Array.Empty<string>()),
            new("profile", "Profile-Service", "Customer-Info", 8260L * days, 32, false, Array.Empty<string>()),
            new("account", "Account-Service", "Card-EFT", 2860L * days, 45, false, Array.Empty<string>()),
            new("payment", "Payment-Service", "Card-EFT", 1260L * days, 51, false, new[] { "ledger" }),
            new("ledger", "Ledger-Service", "Card-EFT", 1260L * days, 16, false, new[] { "ledger-temp-1" }),
            new("ledger-temp-1", "Ledger-Temp-Service", "Temp-EFT", 1260L * days, 16, false, new[] { "ledger-temp-2" }),
            new("ledger-temp-2", "Ledger-Temp-Service", "Temp-EFT", 1260L * days, 16, false, Array.Empty<string>())


        };
        var descriptions = new Dictionary<string, string>
        {
            ["Mobil-Login"] = "Kimlik ve oturum yönetimi",
            ["Customer-Info"] = "Müşteri bilgi servisleri",
            ["Card-EFT"] = "Kart ve para transferleri"
        };
        return Task.FromResult(new AnalysisResult
        {
            ServiceName = name,
            Days = days,
            From = to.AddDays(-days),
            To = to,
            IsDemo = true,
            Nodes = nodes,
            Projects = nodes.Where(node => !node.IsRoot).GroupBy(node => node.Project)
                .Select(group => new ProjectDependency(group.Key, descriptions[group.Key], group.Count())).ToArray()
        });
    }
}

