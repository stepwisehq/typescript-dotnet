// Authored opt-in observations: fresh binding precedes optional lazy syntax getters.
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TypeScript.Scanner.ParserValidation;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;

namespace TypeScript.Scanner.BinderValidation;
public sealed class CompletionRequest
{
    public Request Parse { get; set; } = new();
    public bool WarmBeforeBind { get; set; }
    public bool ResolveJSDocAfterBind { get; set; }
}

public sealed record SymbolAPI(int SymbolId, bool SourceOwned, bool ExternalModule, bool Static, uint CombinedFlags, BoundName DisplayName, uint CheckFlags);
public sealed record ContainerAPI(int NodeId, uint Flags);
public sealed class CompletionResult
{
    public Result Graph { get; set; } = null!;
    public Dictionary<string, bool> MetadataNulls { get; set; } = [];
    public List<string> ObservationOrder { get; set; } = [];
    public bool BindDiagnosticsNull { get; set; }
    public List<CanonicalDiagnosticState> BindDiagnosticStates { get; set; } = [];
    public List<SymbolAPI> SymbolHelpers { get; set; } = [];
    public List<ContainerAPI> ContainerHelpers { get; set; } = [];
    public Response? ObservedSyntax { get; set; }
}

public sealed record CompletionSnapshot(Response After, Binding Binding, Dictionary<string, bool> MetadataNulls, bool BindDiagnosticsNull, List<CanonicalDiagnosticState> BindDiagnosticStates);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 2048)]
[JsonSerializable(typeof(CompletionSnapshot))]
[JsonSerializable(typeof(CompletionRequest))]
[JsonSerializable(typeof(CompletionResult))]
public partial class CompletionJson : JsonSerializerContext;
public sealed partial class Graph
{
    public List<SymbolAPI> SymbolObservations(SourceFile file) => symbolIds.OrderBy(pair => pair.Value).Select(pair => new SymbolAPI(pair.Value, ReferenceEquals(Ast.GetSourceFileOfSymbol(pair.Key), file), pair.Key.IsExternalModule(), pair.Key.IsStatic(), (uint)pair.Key.CombinedLocalAndExportSymbolFlags(), Name(Ast.SymbolName(pair.Key), pair.Key), (uint)pair.Key.CheckFlags)).ToList();
    public List<ContainerAPI> ContainerObservations() => syntax.RawNodes.Select((node, id) => new ContainerAPI(id, (uint)Binder.GetContainerFlags(node))).ToList();
}

public static class CompletionRunner
{
    private static Request Cold(Request r) => new()
    {
        Name = r.Name,
        Text = r.Text,
        TextBytesBase64 = r.TextBytesBase64,
        ScriptKind = r.ScriptKind,
        FileName = r.FileName,
        ExternalModuleJSX = r.ExternalModuleJSX,
        ExternalModuleForce = r.ExternalModuleForce,
        ResolveJSDoc = false,
        FoundationObservations = false,
        TextProbeIds = [],
        TextProbeIncludeJSDoc = r.TextProbeIncludeJSDoc,
    };
    private static (Response After, Binding Binding, Graph Graph) Snapshot(SourceFile file, Request r)
    {
        var encoder = new Encoder(file, false);
        var after = ParserValidation.Runner.Encode(file, r, encoder);
        var graph = new Graph(encoder, lexicalPrivateOwner: true);
        return (after, graph.Encode(file), graph);
    }

    private static Dictionary<string, bool> MetadataNulls(SourceFile file) => new()
    {
        ["imports"] = file.Imports().IsNil,
        ["moduleAugmentations"] = file.ModuleAugmentations.IsNil,
        ["ambientModuleNames"] = file.AmbientModuleNames.IsNil,
        ["commentDirectives"] = file.CommentDirectives.IsNil,
        ["reparsedClones"] = file.ReparsedClones.IsNil,
        ["pragmas"] = file.Pragmas.IsNil,
        ["referencedFiles"] = file.ReferencedFiles.IsNil,
        ["typeReferenceDirectives"] = file.TypeReferenceDirectives.IsNil,
        ["libReferenceDirectives"] = file.LibReferenceDirectives.IsNil,
        ["parseDiagnostics"] = file.Diagnostics().IsNil,
        ["jsDiagnostics"] = file.JSDiagnostics().IsNil,
        ["jsDocDiagnostics"] = file.JSDocDiagnostics().IsNil,
    };
    private static List<CanonicalDiagnosticState> States(Slice<Diagnostic> diagnostics, SourceFile file) => States(diagnostics.IsNil ? null : diagnostics.AsSpan().ToArray(), file);
    private static List<CanonicalDiagnosticState> States(IEnumerable<Diagnostic>? diagnostics, SourceFile file) => diagnostics?.Select(d => new CanonicalDiagnosticState { FileNameBase64 = d.File()is { } diagnosticFile ? Convert.ToBase64String(Encoding.UTF8.GetBytes(diagnosticFile.FileName())) : null, FileMatchesSource = ReferenceEquals(d.File(), file), SourceBase64 = Encoder.Bytes(d.Source()), MessageTextBase64 = Encoder.Bytes(d.MessageText()), ArgsNull = d.MessageArgs()is null, ChainNull = d.MessageChain().IsNil, RelatedNull = d.RelatedInformation().IsNil, ReportsUnnecessary = d.ReportsUnnecessary(), ReportsDeprecated = d.ReportsDeprecated(), SkippedOnNoEmit = d.SkippedOnNoEmit(), Chain = States(d.MessageChain(), file), Related = States(d.RelatedInformation(), file), }).ToList() ?? [];
    public static CompletionResult Run(CompletionRequest request)
    {
        var cold = Cold(request.Parse);
        // A distinct parse supplies the unbound baseline. No observer touches the primary source before binding.
        var before = ParserValidation.Runner.Run(cold);
        var file = ParserValidation.Runner.Parse(cold);
        var order = new List<string>
        {
            "independent-unbound-parse-snapshot",
            "fresh-primary-parse"
        };
        if (request.WarmBeforeBind)
        {
            ParserValidation.Runner.Encode(file, request.Parse);
            order.Add("explicit-prebind-parser-observations");
        }

        Binder.BindSourceFile(file);
        order.AddRange(["bind", "readonly-cold-graph-snapshot"]);
        var first = Snapshot(file, cold);
        var firstNulls = MetadataNulls(file);
        byte[] firstBytes = JsonSerializer.SerializeToUtf8Bytes(new CompletionSnapshot(first.After, first.Binding, firstNulls, file.BindDiagnostics().IsNil, States(file.BindDiagnostics(), file)), CompletionJson.Default.CompletionSnapshot);
        Binder.BindSourceFile(file);
        var second = Snapshot(file, cold);
        first.Graph.AssertSameIdentities(second.Graph);
        byte[] secondBytes = JsonSerializer.SerializeToUtf8Bytes(new CompletionSnapshot(second.After, second.Binding, MetadataNulls(file), file.BindDiagnostics().IsNil, States(file.BindDiagnostics(), file)), CompletionJson.Default.CompletionSnapshot);
        if (!firstBytes.AsSpan().SequenceEqual(secondBytes))
            throw new InvalidOperationException("Cold repeated bind changed published graph");
        order.Add("repeat-bind-identities-and-values-before-helper-observations");
        var result = new CompletionResult
        {
            Graph = new(before, first.After, first.Binding, true),
            MetadataNulls = firstNulls,
            ObservationOrder = order,
            BindDiagnosticsNull = file.BindDiagnostics().IsNil,
            BindDiagnosticStates = States(file.BindDiagnostics(), file),
            SymbolHelpers = first.Graph.SymbolObservations(file),
            ContainerHelpers = first.Graph.ContainerObservations(),
        };
        order.Add("published-symbol-ownership-name-flags-and-container-helpers");
        if (request.ResolveJSDocAfterBind)
        {
            cold.ResolveJSDoc = true;
            result.ObservedSyntax = ParserValidation.Runner.Encode(file, cold);
            order.Add("explicit-lazy-jsdoc-syntax-observation-after-binding");
        }

        return result;
    }
}
