using System.IO.Compression;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class TupleSpreadTests
{
    [Fact]
    public void TupleExpansionPreservesSuffixesAndRepeatedChecksAgainstPinnedGo()
    {
        using var stream = typeof(TupleSpreadTests).Assembly.GetManifestResourceStream("Checker.NoEmit.Tests.TupleSpreadProgramCases")
            ?? throw new InvalidOperationException("Pinned-Go tuple spread reference missing.");
        using var reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress));
        var pair = JsonNode.Parse(reader.ReadLine() ?? throw new InvalidOperationException("Tuple spread reference empty."))!.AsObject();
        Assert.Null(reader.ReadLine());
        Assert.Equal("tuple-spread-suffix-repeat", pair["request"]!["Name"]!.GetValue<string>());
        Assert.Equal(pair["request"]!["Name"]!.GetValue<string>(), pair["expected"]!["name"]!.GetValue<string>());
        Assert.NotNull(pair["request"]!["Semantic"]);
        Assert.NotNull(pair["expected"]!["snapshots"]![0]!["semantic"]!["bulk"]);
        var expectedDiagnostics = pair["expected"]!["snapshots"]![0]!["semantic"]!["bulk"]!.AsArray();
        Assert.Single(expectedDiagnostics);
        Assert.Equal(2345, expectedDiagnostics[0]!["code"]!.GetValue<int>());
        var actual = JsonNode.Parse(ProgramRunner.Run(pair["request"]!.ToJsonString()));
        Assert.True(JsonNode.DeepEquals(pair["expected"], actual), "Complete pinned-Go tuple spread record differs.");
    }
}
