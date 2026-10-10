using System.IO.Compression;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

// Authored regressions for checker/relater.go:1044 TypeDiscriminator.matches.
// Complete observations come from pinned fed0bf24149fb1ed36039212648bafdafc1ea10e
// with genuine Program, strict ES2022 libraries, unchanged inputs and options.
public sealed class DiscriminantNeverTests
{
    [Theory]
    [InlineData("never-discriminator-valid")]
    [InlineData("never-discriminator-excess")]
    [InlineData("ordinary-discriminator-valid")]
    public void DiscriminantDistributionMatchesOriginalProgram(string name)
    {
        using var resource = typeof(DiscriminantNeverTests).Assembly
            .GetManifestResourceStream("Checker.NoEmit.Tests.DiscriminantNeverCases")
            ?? throw new InvalidOperationException("Pinned-Go discriminant references are missing.");
        using var reader = new StreamReader(new GZipStream(resource, CompressionMode.Decompress));
        Dictionary<string, JsonObject> cases = new(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            var request = row["request"]!.AsObject();
            var expected = row["expected"]!.AsObject();
            Assert.Equal(request["Name"]!.GetValue<string>(), expected["name"]!.GetValue<string>());
            cases.Add(request["Name"]!.GetValue<string>(), row);
        }
        Assert.Equal(new[] { "never-discriminator-excess", "never-discriminator-valid", "ordinary-discriminator-valid" },
            cases.Keys.OrderBy(value => value, StringComparer.Ordinal));
        var selected = cases[name];
        var actual = JsonNode.Parse(ProgramRunner.Run(selected["request"]!.ToJsonString()));
        // The excess case retains 2353 after unmatched discriminants restore the
        // target union; valid cases and repeated public type queries also agree.
        Assert.True(JsonNode.DeepEquals(selected["expected"], actual),
            "Complete pinned-Go Program observation differs: " + name);
    }
}
