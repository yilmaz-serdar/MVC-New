namespace DependencyAnalyzer.Mvc.Models;

internal static class TopologyLayout
{
    // Cards are 220 x 78 px. Horizontal/vertical spacing includes room for edges.
    private const int ColumnSpacing = 292;
    private const int RowSpacing = 110;
    private const int LeftPadding = 32;
    private const int TopPadding = 24;

    public static IReadOnlyList<ServiceNode> Arrange(IReadOnlyList<ServiceNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Count == 0) return Array.Empty<ServiceNode>();

        // Snapshot caller-owned collections so later changes cannot invalidate the layout.
        var snapshot = nodes.Select(node => node with
        {
            Children = Array.AsReadOnly(node.Children.Distinct(StringComparer.Ordinal).ToArray())
        }).ToArray();
        var byId = new Dictionary<string, ServiceNode>(StringComparer.Ordinal);
        foreach (var node in snapshot)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || !byId.TryAdd(node.Id, node))
                throw new ArgumentException("Service identifiers must be non-empty and unique.", nameof(nodes));
        }
        var incoming = snapshot.ToDictionary(node => node.Id, _ => 0, StringComparer.Ordinal);
        foreach (var node in snapshot)
        foreach (var child in node.Children)
        {
            if (!byId.ContainsKey(child))
                throw new ArgumentException($"Unknown service reference: {child}", nameof(nodes));
            incoming[child]++;
        }

        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        void Seed(string id)
        {
            if (levels.TryAdd(id, 0)) queue.Enqueue(id);
        }
        void Traverse()
        {
            while (queue.TryDequeue(out var id))
            {
                foreach (var child in byId[id].Children)
                {
                    // Visit once: shared descendants, self-calls and cycles are safe.
                    if (levels.TryAdd(child, levels[id] + 1)) queue.Enqueue(child);
                }
            }
        }

        foreach (var node in snapshot.Where(node => node.IsRoot)) Seed(node.Id);
        Traverse();
        foreach (var node in snapshot.Where(node => incoming[node.Id] == 0)) Seed(node.Id);
        Traverse();
        // A disconnected cyclic component may have neither a root nor a zero in-degree node.
        foreach (var node in snapshot)
        {
            if (levels.ContainsKey(node.Id)) continue;
            Seed(node.Id);
            Traverse();
        }

        var columns = snapshot.GroupBy(node => levels[node.Id]).ToArray();
        var maximumRows = columns.Max(column => column.Count());
        var positioned = new Dictionary<string, ServiceNode>(StringComparer.Ordinal);
        foreach (var column in columns)
        {
            var row = 0;
            var offset = (maximumRows - column.Count()) * RowSpacing / 2;
            foreach (var node in column)
            {
                positioned[node.Id] = node with
                {
                    X = LeftPadding + column.Key * ColumnSpacing,
                    Y = TopPadding + offset + row++ * RowSpacing
                };
            }
        }
        return Array.AsReadOnly(snapshot.Select(node => positioned[node.Id]).ToArray());
    }
}
