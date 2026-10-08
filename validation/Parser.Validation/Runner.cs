using System.Globalization;
using System.Text;

namespace TypeScript.Scanner.ParserValidation;

public sealed class Encoder
{
    private readonly SourceFile file;
    private readonly bool resolveJsDoc;
    private readonly Dictionary<Node, int> ids = new(ReferenceEqualityComparer.Instance);
    private readonly List<Node> raw = [];
    private readonly Dictionary<Node, List<Node>?> resolvedDocs = new(ReferenceEqualityComparer.Instance);
    public List<CanonicalNode> Nodes { get; } = [];
    public IReadOnlyList<Node> RawNodes => raw;
    public Encoder(SourceFile file, bool resolveJsDoc) { this.file = file; this.resolveJsDoc = resolveJsDoc; }
    public static string Bytes(string text) => Convert.ToBase64String(Encoding.Latin1.GetBytes(text));
    public int Add(Node node)
    {
        if (ids.TryGetValue(node, out int id)) return id;
        id = Nodes.Count;
        ids.Add(node, id); raw.Add(node);
        var output = new CanonicalNode { Id = id, Kind = (int)node.Kind, Pos = node.Pos(), End = node.End(), Flags = (uint)node.Flags, DataType = node.DataType() };
        Nodes.Add(output);
        foreach (var field in node.SyntaxFields())
        {
            string role = char.ToUpperInvariant(field.Role[0]) + field.Role[1..];
            var value = new CanonicalField { Name = role, Type = field.Type, IsNull = field.Value is null };
            if (field.Value is not null)
            {
                switch (field.Type)
                {
                    case "node": value.NodeId = Add((Node)field.Value); break;
                    case "list": value.List = AddList((NodeList)field.Value); break;
                    case "bytes": value.BytesBase64 = Bytes((string)field.Value); break;
                    case "number": value.Number = Convert.ToInt64(field.Value, CultureInfo.InvariantCulture); break;
                    case "bool": value.Boolean = (bool)field.Value; break;
                    case "nodes": value.NodeIds = ((IEnumerable<Node>)field.Value).Select(Add).ToList(); break;
                    case "strings": value.StringsBase64 = ((IEnumerable<string>)field.Value).Select(Bytes).ToList(); break;
                    default: throw new InvalidOperationException($"Unclassified syntax field {output.DataType}.{role}: {field.Type}");
                }
            }
            output.Fields.Add(value);
        }
        node.ForEachChild(child => { output.ChildOrder.Add(Add(child)); return false; });
        if (resolveJsDoc)
        {
            var docs = node.JSDoc(file);
            resolvedDocs[node] = docs?.ToList();
            foreach (var doc in docs ?? []) output.JsDoc.Add(Add(doc));
        }
        return id;
    }
    private CanonicalList AddList(NodeList list) => new()
    {
        Pos = list.Pos(), End = list.End(), NodesNull = list.Nodes is null,
        Missing = list.IsMissing, TrailingComma = list.HasTrailingComma(),
        ModifierFlags = list is ModifierList modifiers ? (uint)modifiers.ModifierFlags : null,
        Items = list.Nodes?.Select(Add).ToList() ?? [],
    };
    public void FinishParents()
    {
        for (int i = 0; i < raw.Count; i++)
        {
            if (resolveJsDoc)
            {
                var first = raw[i].JSDoc(file);
                var second = raw[i].JSDoc(file);
                var cold = resolvedDocs[raw[i]];
                if ((cold is null) != (first is null) || (cold is not null && !cold.SequenceEqual(first!)) || (first is null) != (second is null) || (first is not null && !first.SequenceEqual(second!)))
                    throw new InvalidOperationException($"Unstable warm JSDoc at canonical node {i}");
            }
            if (raw[i].Parent is { } parent)
                Nodes[i].Parent = new() { Id = ids.TryGetValue(parent, out int id) ? id : null, Kind = (int)parent.Kind, Pos = parent.Pos(), End = parent.End() };
            var seen = new HashSet<Node>(ReferenceEqualityComparer.Instance);
            for (var ancestor = raw[i]; ancestor is not null; ancestor = ancestor.Parent)
                if (!seen.Add(ancestor)) throw new InvalidOperationException($"Parent cycle at canonical node {i}");
        }
    }
    public int? Indicator(Node? node) => node is null ? null : Add(node);
    public List<int> AddNodes(IEnumerable<Node>? nodes) => nodes?.Select(Add).ToList() ?? [];
    public CanonicalTextProbe TextProbe(int id, bool includeJSDoc)
    {
        if ((uint)id >= (uint)raw.Count) throw new ArgumentException("Invalid text probe canonical node ID");
        return new(id, Convert.ToBase64String(Scanner.GetTextOfNodeBytes(raw[id])), Scanner.GetTokenPosOfNode(raw[id], file, includeJSDoc));
    }
    public static List<CanonicalDiagnostic> Diagnostics(IEnumerable<Diagnostic>? diagnostics) => diagnostics?.Select(d => new CanonicalDiagnostic
    {
        Code = d.Code(), Category = (int)d.Category(), Pos = d.Pos(), End = d.End(), Key = d.MessageKey(),
        ArgsBase64 = d.MessageArgs()?.Select(Bytes).ToList() ?? [], Text = d.String(),
        Related = Diagnostics(d.RelatedInformation()), Chain = Diagnostics(d.MessageChain()),
    }).ToList() ?? [];
    public static List<CanonicalReference> References(IEnumerable<FileReference>? refs) => refs?.Select(r => new CanonicalReference(r.Pos(), r.End(), Bytes(r.FileName), (int)r.ResolutionMode, r.Preserve)).ToList() ?? [];
}

public static class Runner
{
    public static SourceFile Parse(Request request)
    {
        var kind = (ScriptKind)(request.ScriptKind == 0 ? 3 : request.ScriptKind);
        string extension = kind switch { ScriptKind.JS => ".js", ScriptKind.JSX => ".jsx", ScriptKind.TS => ".ts", ScriptKind.TSX => ".tsx", ScriptKind.JSON => ".json", _ => throw new ArgumentException("Unsupported ScriptKind") };
        string fileName = request.FileName.Length == 0 ? "/index" + extension : request.FileName;
        byte[] bytes = request.TextBytesBase64 is { } encoded ? Convert.FromBase64String(encoded) : Encoding.UTF8.GetBytes(request.Text);
        return Parser.ParseSourceFile(new(fileName, fileName, new(request.ExternalModuleJSX, request.ExternalModuleForce)), bytes, kind);
    }
    public static Response Run(Request request) => Encode(Parse(request), request);
    public static Response Encode(SourceFile file, Request request, Encoder? suppliedEncoder = null)
    {
        var encoder = suppliedEncoder ?? new Encoder(file, request.ResolveJSDoc);
        encoder.Add(file.AsNode());
        var metadata = new Metadata
        {
            ScriptKind = (int)file.ScriptKind, LanguageVariant = (int)file.LanguageVariant, IsDeclarationFile = file.IsDeclarationFile,
            NodeCount = file.NodeCount, IdentifierCount = file.IdentifierCount, TextCount = file.TextCount,
            ExternalModuleIndicator = encoder.Indicator(file.ExternalModuleIndicator), CommonJSModuleIndicator = encoder.Indicator(file.CommonJSModuleIndicator),
            CommentDirectives = file.CommentDirectives?.Select(d => new CanonicalDirective(d.Loc.Pos(), d.Loc.End(), (int)d.Kind)).ToList() ?? [],
            ReferencedFiles = Encoder.References(file.ReferencedFiles), TypeReferenceDirectives = Encoder.References(file.TypeReferenceDirectives), LibReferenceDirectives = Encoder.References(file.LibReferenceDirectives),
            ReparsedClones = encoder.AddNodes(file.ReparsedClones), Imports = encoder.AddNodes(file.Imports()), ModuleAugmentations = encoder.AddNodes(file.ModuleAugmentations),
            AmbientModuleNamesBase64 = file.AmbientModuleNames?.Select(Encoder.Bytes).ToList() ?? [], UsesUriStyleNodeCoreModules = (int)file.UsesUriStyleNodeCoreModules,
            CheckJsDirective = file.CheckJsDirective is { } check ? new(check.Enabled, check.Range.Pos, check.Range.End, (int)check.Range.Kind, check.Range.HasTrailingNewLine) : null,
            Pragmas = file.Pragmas?.Select(p => new CanonicalPragma(p.CommentRange.Pos, p.CommentRange.End, (int)p.CommentRange.Kind, p.CommentRange.HasTrailingNewLine, p.Name,
                p.Args?.ToDictionary(kv => kv.Key, kv => new CanonicalPragmaArgument(kv.Value.TextRange.Pos(), kv.Value.TextRange.End(), Encoder.Bytes(kv.Value.Value))) ?? [])).ToList() ?? [],
        };
        encoder.FinishParents();
        return new() { Nodes = encoder.Nodes, Metadata = metadata, ParseDiagnostics = Encoder.Diagnostics(file.Diagnostics()), JsDiagnostics = Encoder.Diagnostics(file.JSDiagnostics()), JsDocDiagnostics = Encoder.Diagnostics(file.JSDocDiagnostics()), TextProbes = (request.TextProbeIds ?? []).Select(id => encoder.TextProbe(id, request.TextProbeIncludeJSDoc)).ToList() };
    }
}
