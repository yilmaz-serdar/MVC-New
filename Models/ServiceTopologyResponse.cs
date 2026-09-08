using System.Text.Json.Serialization;

namespace DependencyAnalyzer.Mvc.Models;

// Matches the analysis service response without inferring project membership or traffic counts.
public sealed class ServiceTopologyResponse
{
    [JsonRequired, JsonPropertyName("nodes")]
    public IReadOnlyList<TopologyNode> Nodes { get; init; } = Array.Empty<TopologyNode>();

    [JsonRequired, JsonPropertyName("edges")]
    public IReadOnlyList<TopologyEdge> Edges { get; init; } = Array.Empty<TopologyEdge>();

    [JsonRequired, JsonPropertyName("dependencies")]
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    public void Validate()
    {
        if (Nodes is null || Edges is null || Dependencies is null)
            throw new InvalidDataException("Topology collections cannot be null.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Nodes)
            if (node is null || string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Label) || !ids.Add(node.Id))
                throw new InvalidDataException("Each node must have a unique id and a label.");
        foreach (var edge in Edges)
            if (edge is null || string.IsNullOrWhiteSpace(edge.Source) || string.IsNullOrWhiteSpace(edge.Target)
                || !ids.Contains(edge.Source) || !ids.Contains(edge.Target) || string.IsNullOrWhiteSpace(edge.CallType))
                throw new InvalidDataException("Each edge must reference existing nodes and include callType.");
        if (Dependencies.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Dependency names cannot be empty.");
    }
}

public sealed record TopologyNode(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string Label);

public sealed record TopologyEdge(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("callType")] string CallType);
