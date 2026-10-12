// Authored public diagnostic projection, matching accepted no-emit public-field normalizer.
// Fixture root relocation is explicit; no private checker state or diagnostic admission.
#nullable disable
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TypeScript.Scanner;
namespace Emit.Checks;
internal sealed record TsDiagnosticValue(string File, int Start, int Length, int End, int Code,
    int Category, string Key, string Message, string[] Args, string Source,
    bool ReportsUnnecessary, bool ReportsDeprecated, bool SkippedOnNoEmit,
    TsDiagnosticValue[] Chain, TsDiagnosticValue[] Related, TsRepopulateValue Repopulate);
internal sealed record TsRepopulateValue(int Kind, string ModuleReference, int Mode, string PackageName);
internal sealed record TsDiagnosticChannels(TsDiagnosticValue[] Config, TsDiagnosticValue[] Program,
    TsDiagnosticValue[] Syntactic, TsDiagnosticValue[] Global, TsDiagnosticValue[] Semantic);
[JsonSerializable(typeof(TsDiagnosticChannels))]
internal partial class TsDiagnosticJsonContext : JsonSerializerContext { }
internal static class TsDiagnosticProjection
{
    internal static TsDiagnosticValue[] Project(Slice<Diagnostic> diagnostics, string root)
    {
        var result = new TsDiagnosticValue[diagnostics.Count];
        for (int i = 0; i < result.Length; i++)
        {
            var d = diagnostics[i];
            var args = d.MessageArgs();
            string[] projectedArgs = args == null ? null : new string[args.Count];
            if (args != null)
                for (int j = 0; j < args.Count; j++) projectedArgs[j] = Normalize(ScannerString.ToJsString(args[j]), root);
            var repopulate = d.RepopulateInfo();
            result[i] = new(d.File() == null ? null : Normalize(ScannerString.ToJsString(d.File().FileName()), root),
                d.Pos(), d.Len(), d.End(), d.Code(), (int)d.Category(), d.MessageKey(),
                Normalize(d.String(), root), projectedArgs, ScannerString.ToJsString(d.Source()),
                d.ReportsUnnecessary(), d.ReportsDeprecated(), d.SkippedOnNoEmit(),
                Project(d.MessageChain(), root), Project(d.RelatedInformation(), root),
                repopulate == null ? null : new((int)repopulate.Kind, ScannerString.ToJsString(repopulate.ModuleReference),
                    (int)repopulate.Mode, ScannerString.ToJsString(repopulate.PackageName)));
        }
        return result;
    }
    private static string Normalize(string text, string root) => text.Replace(root.Replace('\\', '/') + "/", "/", StringComparison.Ordinal);
    internal static void Check(string name, TsDiagnosticChannels actual, string expected)
    {
        string json = JsonSerializer.Serialize(actual, TsDiagnosticJsonContext.Default.TsDiagnosticChannels);
        if (!JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(expected)))
            throw new InvalidOperationException($"{name}: full public Program diagnostics differ.\nExpected:\n{expected}\nActual:\n{json}");
    }
}
