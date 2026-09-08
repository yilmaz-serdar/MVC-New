using DependencyAnalyzer.Mvc.Models;
using DependencyAnalyzer.Mvc.Services;

static ServiceNode Node(string id, bool root = false, params string[] children) =>
    new(id, id, "Project", 1, 1, root, children);
static AnalysisResult Layout(params ServiceNode[] nodes) => new() { Nodes = nodes };
static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void CheckBounds(AnalysisResult result)
{
    foreach (var node in result.Nodes)
    {
        Check(node.X >= 0 && node.Y >= 0, "Negative position");
        Check(node.X + 220 <= result.GraphWidth && node.Y + 38 + 78 <= result.GraphHeight, "Clipped card");
    }
    for (var i = 0; i < result.Nodes.Count; i++)
    for (var j = i + 1; j < result.Nodes.Count; j++)
    {
        var a = result.Nodes[i]; var b = result.Nodes[j];
        Check(Math.Abs(a.X - b.X) >= 220 || Math.Abs(a.Y - b.Y) >= 78, "Overlapping cards");
    }
}
static void Reject(params ServiceNode[] nodes)
{
    try { Layout(nodes); }
    catch (ArgumentException) { return; }
    throw new Exception("Invalid graph accepted");
}

Check(Layout().Nodes.Count == 0, "Empty graph");
CheckBounds(Layout(Node("only", true)));
var chain = Layout(Node("a", true, "b"), Node("b", false, "c"), Node("c"));
Check(chain.Nodes[0].X < chain.Nodes[1].X && chain.Nodes[1].X < chain.Nodes[2].X, "Chain levels");
CheckBounds(chain);
var diamond = Layout(Node("a", true, "b", "c"), Node("b", false, "d"), Node("c", false, "d"), Node("d"));
Check(diamond.Nodes[1].X == diamond.Nodes[2].X, "Sibling levels");
Check(diamond.Nodes[3].X > diamond.Nodes[1].X, "Shared descendant");
CheckBounds(diamond);
CheckBounds(Layout(Node("a", true, "a", "b"), Node("b", false, "a"), Node("orphan"), Node("x", false, "y"), Node("y", false, "x")));
var multiple = Layout(Node("a", true, "c"), Node("b", true, "c"), Node("c"));
Check(multiple.Nodes[0].X == multiple.Nodes[1].X, "Multiple roots");
CheckBounds(multiple);
var unmarked = Layout(Node("child"), Node("parent", false, "child"));
Check(unmarked.Nodes[1].X < unmarked.Nodes[0].X, "Inferred root");
var wide = Layout(new[] { Node("root", true, Enumerable.Range(0, 30).Select(i => $"n{i}").ToArray()) }
    .Concat(Enumerable.Range(0, 30).Select(i => Node($"n{i}"))).ToArray());
Check(wide.GraphHeight > 590, "Tall canvas must grow");
CheckBounds(wide);
var deep = Layout(Enumerable.Range(0, 100).Select(i => Node($"n{i}", i == 0, i < 99 ? new[] { $"n{i + 1}" } : Array.Empty<string>())).ToArray());
Check(deep.GraphWidth > 1160, "Wide canvas must grow");
CheckBounds(deep);
var original = new[] { Node("a", true, "b"), Node("b") };
var first = Layout(original); var second = Layout(original);
Check(first.Nodes.Select(n => (n.X, n.Y)).SequenceEqual(second.Nodes.Select(n => (n.X, n.Y))), "Deterministic layout");
Check(original.All(n => n.X == 0 && n.Y == 0), "Caller data mutated");
var children = new[] { "b", "b" };
var snapshot = Layout(Node("a", true, children), Node("b"));
children[0] = "unknown";
Check(snapshot.ConnectionCount == 1 && snapshot.Nodes[0].Children[0] == "b", "Relationship snapshot/deduplication");
Reject(Node("a"), Node("a")); Reject(Node("a", true, "missing")); Reject(Node(""));
var fixturePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Data/service-topology.json"));
var topology = System.Text.Json.JsonSerializer.Deserialize<ServiceTopologyResponse>(File.ReadAllText(fixturePath))!;
topology.Validate();
Check(topology.Nodes.Count == 100 && topology.Edges.Count == 135 && topology.Dependencies.Count == 20, "Response counts");
Check(topology.Edges.Select(e => e.CallType).Distinct().Count() == 5, "Call types lost");
var serialized = System.Text.Json.JsonSerializer.Serialize(topology);
Check(serialized.Contains("\"callType\"") && serialized.Contains("\"dependencies\""), "Wire property names changed");
var roundTrip = System.Text.Json.JsonSerializer.Deserialize<ServiceTopologyResponse>(serialized)!;
Check(roundTrip.Nodes.SequenceEqual(topology.Nodes) && roundTrip.Edges.SequenceEqual(topology.Edges), "Response round-trip changed");
var cyclic = new ServiceTopologyResponse {
    Nodes = new[] { new TopologyNode("x", "X"), new TopologyNode("y", "Y") },
    Edges = new[] { new TopologyEdge("x", "x", "NORMAL"), new TopologyEdge("x", "y", "REST"), new TopologyEdge("x", "y", "SOAP"), new TopologyEdge("y", "x", "gRPC") }
};
cyclic.Validate();
try { new ServiceTopologyResponse { Nodes = topology.Nodes, Edges = new[] { new TopologyEdge("missing", "x", "REST") } }.Validate(); throw new Exception("Dangling reference accepted"); }
catch (InvalidDataException) { }
try { System.Text.Json.JsonSerializer.Deserialize<ServiceTopologyResponse>("{}"); throw new Exception("Missing fields accepted"); }
catch (System.Text.Json.JsonException) { }
try { new ServiceTopologyResponse { Nodes = new[] { new TopologyNode("x", "X"), new TopologyNode("x", "X") } }.Validate(); throw new Exception("Duplicate node accepted"); }
catch (InvalidDataException) { }
Console.WriteLine("PASS: response schema, 100 nodes/135 edges/20 dependencies, call types, round-trip, cycles/parallel calls, malformed response rejection and legacy layout compatibility.");
