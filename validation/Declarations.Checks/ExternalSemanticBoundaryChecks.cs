// Authored current-capability controls, promoted from the obsolete external
// restrictions. Exact original inputs/options and complete pinned-Go public
// observations are retained by authored-capability-migration.json.
using System.IO.Compression;
using System.Text.Json.Nodes;
using Configuration.Checks;
using Program.Checks;

namespace Declarations.Checks;

public static class ExternalSemanticBoundaryChecks
{
    // Historical dispatcher identity is retained; the provenance explicitly
    // changes this authored assertion from an external ban to source agreement.
    public static IReadOnlyList<string> Cases { get; } = ["Authored.ExternalSemanticConsumers"];

    public static void Run(string name)
    {
        if (name != Cases[0]) throw new ArgumentOutOfRangeException(nameof(name));
        using var resource = typeof(ExternalSemanticBoundaryChecks).Assembly
            .GetManifestResourceStream("Declarations.Checks.AuthoredCapabilityChecks")
            ?? throw new InvalidOperationException("Qualified current-capability observations are missing.");
        using var reader = new StreamReader(new GZipStream(resource, CompressionMode.Decompress));
        string[] names = ["CurrentCapability.CheckedJS", "CurrentCapability.SkippedJSQuery",
            "CurrentCapability.JSX", "CurrentCapability.LegacyDecorator"];
        int index = 0;
        while (reader.ReadLine() is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            Check.Equal(row.Count, 2, "current capability resource fields");
            var request = row["request"]!.AsObject();
            var expected = row["expected"]!.AsObject();
            Check.True(index < names.Length, "current capability registry has no extra rows");
            Check.Equal(request["Name"]!.GetValue<string>(), names[index], "current capability request order");
            Check.Equal(expected["name"]!.GetValue<string>(), names[index], "current capability reference identity");
            var actual = JsonNode.Parse(ProgramRunner.Run(request.ToJsonString()));
            Check.True(JsonNode.DeepEquals(expected, actual),
                "complete pinned-Go diagnostics/environment/public queries: " + names[index]);
            index++;
        }
        Check.Equal(index, names.Length, "all four original authored inputs are promoted");
    }
}
