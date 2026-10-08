using System.Text;

namespace TypeScript.Scanner.ParserValidation;
public sealed class CanonicalFact
{
    public int NodeId { get; set; }
    public uint? InitialCache { get; set; }
    public uint First { get; set; }
    public uint? CacheAfterFirst { get; set; }
    public uint Repeated { get; set; }
    public uint? CacheAfterRepeated { get; set; }
}

public sealed record CanonicalPosition(int Offset, int Utf8ToUtf16, int Utf16ToUtf8);
public sealed record CanonicalVisit(int NodeId, bool Same, List<int> ChildOrder);
public sealed class CanonicalDiagnosticState
{
    public string? FileNameBase64 { get; set; }
    public bool FileMatchesSource { get; set; }
    public string SourceBase64 { get; set; } = "";
    public string MessageTextBase64 { get; set; } = "";
    public bool ArgsNull { get; set; }
    public bool ChainNull { get; set; }
    public bool RelatedNull { get; set; }
    public bool ReportsUnnecessary { get; set; }
    public bool ReportsDeprecated { get; set; }
    public bool SkippedOnNoEmit { get; set; }
    public List<CanonicalDiagnosticState> Chain { get; set; } = [];
    public List<CanonicalDiagnosticState> Related { get; set; } = [];
}

public sealed class CanonicalFoundation
{
    public List<CanonicalFact> Facts { get; set; } = [];
    public List<CanonicalVisit> Visits { get; set; } = [];
    public bool AsciiOnly { get; set; }
    public bool PositionMapReused { get; set; }
    public List<CanonicalPosition> Positions { get; set; } = [];
    public List<CanonicalDiagnosticState> ParseStates { get; set; } = [];
    public List<CanonicalDiagnosticState> JsStates { get; set; } = [];
    public List<CanonicalDiagnosticState> JsDocStates { get; set; } = [];
    public List<bool> DiagnosticChannelsNull { get; set; } = [];
}

internal static class FoundationEncoder
{
    private static uint? Cache(Node node) => node.data.CacheSubtreeFacts ? node.data.CachedSubtreeFacts : null;
    private static List<CanonicalDiagnosticState> States(IEnumerable<Diagnostic>? diagnostics, SourceFile file) => diagnostics?.Select(d => new CanonicalDiagnosticState { FileNameBase64 = d.File()is { } diagnosticFile ? Convert.ToBase64String(Encoding.UTF8.GetBytes(diagnosticFile.FileName())) : null, FileMatchesSource = ReferenceEquals(d.File(), file), SourceBase64 = Encoder.Bytes(d.Source()), MessageTextBase64 = Encoder.Bytes(d.MessageText()), ArgsNull = d.MessageArgs()is null, ChainNull = d.MessageChain()is null, RelatedNull = d.RelatedInformation()is null, ReportsUnnecessary = d.ReportsUnnecessary(), ReportsDeprecated = d.ReportsDeprecated(), SkippedOnNoEmit = d.SkippedOnNoEmit(), Chain = States(d.MessageChain(), file), Related = States(d.RelatedInformation(), file), }).ToList() ?? [];
    internal static CanonicalFoundation Observe(SourceFile file, IReadOnlyList<Node> nodes)
    {
        // Snapshot every slot before observer calls. Calls then follow canonical
        // DFS identity order, preserving descendant cache side effects from Go.
        var result = new CanonicalFoundation
        {
            Facts = nodes.Select((node, id) => new CanonicalFact { NodeId = id, InitialCache = Cache(node) }).ToList()
        };
        for (int id = 0; id < nodes.Count; id++)
        {
            var fact = result.Facts[id];
            fact.First = (uint)nodes[id].SubtreeFacts();
            fact.CacheAfterFirst = Cache(nodes[id]);
            fact.Repeated = (uint)nodes[id].SubtreeFacts();
            fact.CacheAfterRepeated = Cache(nodes[id]);
        }

        var ids = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
        for (int id = 0; id < nodes.Count; id++)
            ids.Add(nodes[id], id);
        for (int id = 0; id < nodes.Count; id++)
        {
            var order = new List<int>();
            var visitor = Ast.NewNodeVisitor(child =>
            {
                if (!ids.TryGetValue(child, out int childId))
                    throw new InvalidOperationException("Unregistered identity-visitor child");
                order.Add(childId);
                return child;
            }, null!, new());
            var updated = nodes[id].VisitEachChild(visitor);
            result.Visits.Add(new(id, ReferenceEquals(updated, nodes[id]), order));
        }

        var map = file.GetPositionMap();
        result.AsciiOnly = map.IsAsciiOnly();
        result.PositionMapReused = ReferenceEquals(map, file.GetPositionMap());
        int length = file.Text().Length; // SourceFile.Text is the authoritative byte-string.
        var offsets = nodes.SelectMany(node => new[] { node.Pos(), node.End() }).Concat(new[] { -1, 0, length, length + 1 }).Distinct().Order();
        result.Positions = offsets.Select(offset => new CanonicalPosition(offset, map.UTF8ToUTF16(offset), map.UTF16ToUTF8(offset))).ToList();
        result.ParseStates = States(file.Diagnostics(), file);
        result.JsStates = States(file.JSDiagnostics(), file);
        result.JsDocStates = States(file.JSDocDiagnostics(), file);
        result.DiagnosticChannelsNull = [file.Diagnostics()is null, file.JSDiagnostics()is null, file.JSDocDiagnostics()is null ];
        return result;
    }
}
