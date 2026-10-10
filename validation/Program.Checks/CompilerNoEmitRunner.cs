// Authored complete original compiler no-emit channel/walker observation.
// Uses genuine Program and unchanged options/environment, with public behavior
// primary. Historical Program observer and its absent-field protocol stay intact.
using System.Text.Json.Nodes;
using TypeScript.Compiler;
using TypeScript.Scanner;
using CP = TypeScript.Compiler.Program;

namespace Program.Checks;
public static partial class ProgramRunner
{
    private sealed class NoEmitObserver
    {
        private readonly Dictionary<SourceFile, int> files = new(ReferenceEqualityComparer.Instance);
        internal readonly JsonArray Owners = [];
        private int? File(SourceFile? file)
        {
            if (file is null) return null;
            if (!files.TryGetValue(file, out int id))
            {
                id = files.Count; files.Add(file, id);
                Owners.Add((JsonNode)new JsonObject { ["id"] = id, ["nameBase64"] = Bytes(file.FileNameGoBytes()), ["pathBase64"] = Bytes(file.PathKey()), ["textSha256"] = Hash(file.Text()), ["textBytes"] = file.Text().Length });
            }
            return id;
        }
        internal JsonNode? Diagnostics(IEnumerable<Diagnostic>? diagnostics)
        {
            if (diagnostics is null) return null;
            JsonArray result = [];
            foreach (var d in diagnostics)
            {
                var info = d.RepopulateInfo();
                result.Add((JsonNode)new JsonObject
                {
                    ["code"] = d.Code(), ["category"] = (int)d.Category(), ["key"] = d.MessageKey(), ["textBase64"] = Bytes(GoString.ToUtf8(d.String())),
                    ["args"] = Value(d.MessageArgs()), ["pos"] = d.Pos(), ["end"] = d.End(), ["file"] = File(d.File()), ["sourceBase64"] = Bytes(d.Source()),
                    ["reportsUnnecessary"] = d.ReportsUnnecessary(), ["reportsDeprecated"] = d.ReportsDeprecated(), ["skippedOnNoEmit"] = d.SkippedOnNoEmit(),
                    ["chain"] = Diagnostics(d.MessageChain()), ["related"] = Diagnostics(d.RelatedInformation()),
                    ["repopulateInfo"] = info is null ? null : Object(("Kind", info.Kind), ("ModuleReference", info.ModuleReference), ("Mode", info.Mode), ("PackageName", info.PackageName))
                });
            }
            return result;
        }
    }
    private sealed record NoEmitChannels(List<Diagnostic>? Config, List<Diagnostic>? Program,
        List<Diagnostic>? Syntactic, List<Diagnostic>? Semantic, List<Diagnostic>? Global,
        List<Diagnostic>? Suggestions, List<Diagnostic>? Sorted);
    private static NoEmitChannels CollectNoEmit(CP program, CompilerNoEmitHarness.HarnessOptions harness)
    {
        var config = program.GetConfigFileParsingDiagnostics();
        var construction = program.GetProgramDiagnostics();
        var syntactic = program.GetSyntacticDiagnostics();
        var semantic = program.GetSemanticDiagnostics();
        var global = program.GetGlobalDiagnostics();
        var suggestions = harness.CaptureSuggestions ? program.GetSuggestionDiagnostics() : null;
        List<Diagnostic>? all = null;
        foreach (var channel in new[] { config, construction, syntactic, semantic, global, suggestions })
            if (channel is { Count: > 0 }) (all ??= []).AddRange(channel);
        return new(config, construction, syntactic, semantic, global, suggestions, CP.SortAndDeduplicateDiagnostics(all));
    }
    private static JsonObject ObserveNoEmitChannels(NoEmitObserver observer, NoEmitChannels channels) => new()
    {
        ["config"] = observer.Diagnostics(channels.Config), ["program"] = observer.Diagnostics(channels.Program),
        ["syntactic"] = observer.Diagnostics(channels.Syntactic), ["semantic"] = observer.Diagnostics(channels.Semantic),
        ["global"] = observer.Diagnostics(channels.Global), ["suggestions"] = observer.Diagnostics(channels.Suggestions),
        ["sortedAndDeduplicated"] = observer.Diagnostics(channels.Sorted)
    };
    public static JsonObject ObserveCompilerNoEmit(string name, string sourceName, string sourceHash, string configurationName,
        string content, Dictionary<string, string> configuration, string testDataDirectory, bool originalPreConfigurationSkip)
    {
        var compilation = CompilerNoEmitHarness.Prepare(sourceName, content, configuration, testDataDirectory);
        var pre = CollectNoEmit(compilation.PreProgram, compilation.Harness);
        compilation = compilation.ConstructActualProgram();
        var checking = CollectNoEmit(compilation.Program, compilation.Harness);
        compilation = compilation with { Trace = compilation.Tracer.ToString() }; // Original capture point: after actual checking, before baseline queries.
        var observer = new NoEmitObserver();
        JsonObject result = new()
        {
            ["name"] = name, ["status"] = "observed", ["sourceSha256"] = sourceHash, ["configuration"] = configurationName,
            ["originalPreConfigurationSkip"] = originalPreConfigurationSkip, ["originalOptionPolicy"] = OriginalNoEmitOptionPolicy(compilation.Program.Options()),
            ["preChecking"] = ObserveNoEmitChannels(observer, pre), ["checking"] = ObserveNoEmitChannels(observer, checking),
            ["owners"] = observer.Owners, ["typesAndSymbolsDisabled"] = compilation.Harness.NoTypesAndSymbols
        };
        var files = compilation.BaselineFiles.Where(file => compilation.Program.GetSourceFile(RootedFilePath.FromGoString(file.UnitName)) is not null).ToList();
        // Original mapped-source text replacement is performed before baselining.
        for (int i = 0; i < files.Count; i++)
        {
            var sf = compilation.Program.GetSourceFile(RootedFilePath.FromGoString(files[i].UnitName));
            if (sf!.ContentMapper() != "") files[i] = files[i] with { Content = sf.Text() };
        }
        var baselines = compilation.Harness.NoTypesAndSymbols ? null : CompilerBaselineWalker.Observe(compilation.Program, files, compilation.Header, (checking.Sorted?.Count ?? 0) > 0);
        result["typesBase64"] = baselines is null ? null : Bytes(baselines.Types);
        result["symbolsBase64"] = baselines is null ? null : Bytes(baselines.Symbols);
        result["traceBase64"] = Bytes(compilation.Trace);
        CompilerOriginalInvariantChecks.Validate(compilation.Program);
        result["originalAssertions"] = new JsonObject { ["unionOrdering"] = true, ["parentPointers"] = true };
        return result;
    }
    private static JsonObject OriginalNoEmitOptionPolicy(CompilerOptions options)
    {
        string decision = "run", reason = "";
        if (options.Module == ModuleKind.AMD) { decision = "fatal"; reason = "unsupported module kind " + options.Module.String(); }
        else if (options.OutFile.AsString() != "") { decision = "fatal"; reason = "unsupported outFile " + options.OutFile.AsString(); }
        else if (options.Module is ModuleKind.UMD or ModuleKind.System) { decision = "skip"; reason = "unsupported module kind " + options.Module.String(); }
        else if (options.ModuleResolution is ModuleResolutionKind.Node10 or ModuleResolutionKind.Classic) { decision = "skip"; reason = "unsupported module resolution kind " + (int)options.ModuleResolution; }
        else if (options.ESModuleInterop == Tristate.False) { decision = "skip"; reason = "esModuleInterop=false is unsupported"; }
        else if (options.AllowSyntheticDefaultImports == Tristate.False) { decision = "skip"; reason = "allowSyntheticDefaultImports=false is unsupported"; }
        else if (options.BaseUrl.AsString() != "") { decision = "skip"; reason = "unsupported baseUrl " + options.BaseUrl.AsString(); }
        else if (options.Target == ScriptTarget.ES5) { decision = "skip"; reason = "unsupported target ES5"; }
        else if (options.AlwaysStrict == Tristate.False) { decision = "skip"; reason = "alwaysStrict=false is unsupported"; }
        return new() { ["decision"] = decision, ["reasonBase64"] = Bytes(reason) };
    }
}
