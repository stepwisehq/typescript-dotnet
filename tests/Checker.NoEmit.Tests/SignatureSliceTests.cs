using System.IO.Compression;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class SignatureSliceTests
{
    [Theory]
    [InlineData("clipped-boundaries")]
    [InlineData("producer-normalization")]
    [InlineData("same-and-map")]
    [InlineData("public-snapshots")]
    public void PreserveSourceSignatureBoundaries(string name) => SignatureSliceBoundaryChecks.Run(name);

    [Theory]
    [InlineData("signature-mixed-call-new")]
    [InlineData("signature-generic-repeated")]
    [InlineData("signature-filtered-instantiation")]
    [InlineData("signature-empty-and-single-family")]
    public void CompleteProgramMatchesPinnedGo(string name)
    {
        using var stream = typeof(SignatureSliceTests).Assembly.GetManifestResourceStream("Checker.NoEmit.Tests.SignatureSliceProgramCases")
            ?? throw new InvalidOperationException("Pinned-Go signature references missing.");
        using var reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress));
        Dictionary<string, JsonObject> pairs = new(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var pair = JsonNode.Parse(line)!.AsObject();
            string key = pair["request"]!["Name"]!.GetValue<string>();
            Assert.Equal(key, pair["expected"]!["name"]!.GetValue<string>());
            pairs.Add(key, pair);
        }
        Assert.Equal(new[] { "signature-empty-and-single-family", "signature-filtered-instantiation", "signature-generic-repeated", "signature-mixed-call-new" },
            pairs.Keys.Order(StringComparer.Ordinal));
        var selected = pairs[name];
        var actual = JsonNode.Parse(ProgramRunner.Run(selected["request"]!.ToJsonString()));
        Assert.True(JsonNode.DeepEquals(selected["expected"], actual), "Complete pinned-Go signature Program differs: " + name);
    }
}
