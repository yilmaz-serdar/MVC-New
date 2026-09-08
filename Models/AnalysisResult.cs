namespace DependencyAnalyzer.Mvc.Models;

public sealed class AnalysisResult
{
    public string ServiceName { get; init; } = string.Empty;
    public int Days { get; init; }
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
    public bool IsDemo { get; init; }
    public ServiceTopologyResponse Topology { get; init; } = new();
    // Legacy demo contract, retained for existing providers. The current view uses Topology.
    private readonly IReadOnlyList<ServiceNode> _nodes = Array.Empty<ServiceNode>();
    public IReadOnlyList<ServiceNode> Nodes
    {
        get => _nodes;
        init => _nodes = TopologyLayout.Arrange(value);
    }
    public IReadOnlyList<ProjectDependency> Projects { get; init; } = Array.Empty<ProjectDependency>();
    public int ConnectionCount => Nodes.Sum(node => node.Children.Count);
    public long RootCalls => Nodes.FirstOrDefault(node => node.IsRoot)?.Calls ?? 0;
    public int GraphWidth => Math.Max(1160, Nodes.Select(node => node.X + 252).DefaultIfEmpty(1160).Max());
    public int GraphHeight => Math.Max(590, Nodes.Select(node => node.Y + 136).DefaultIfEmpty(590).Max());
}

public sealed record ServiceNode(
    string Id, string Name, string Project, long Calls, int Latency,
    bool IsRoot, IReadOnlyList<string> Children)
{
    // Assigned by AnalysisResult.Nodes; data providers supply only service relationships.
    public int X { get; internal init; }
    public int Y { get; internal init; }
}

public sealed record ProjectDependency(string Name, string Description, int ServiceCount);
