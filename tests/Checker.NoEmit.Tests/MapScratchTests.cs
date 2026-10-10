using System.IO.Compression;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class MapScratchTests
{
    [Fact]
    public void ContextualUnionMappingAcrossInlineBoundaryMatchesPinnedGo()
    {
        using var stream = typeof(MapScratchTests).Assembly.GetManifestResourceStream("Checker.NoEmit.Tests.MapScratchProgramCases")
            ?? throw new InvalidOperationException("Pinned-Go map scratch reference missing.");
        using var reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress));
        var pair = JsonNode.Parse(reader.ReadLine() ?? throw new InvalidOperationException("Map scratch reference empty."))!.AsObject();
        Assert.Null(reader.ReadLine());
        Assert.Equal("map-scratch-tuple-union-boundaries", pair["request"]!["Name"]!.GetValue<string>());
        Assert.Equal(pair["request"]!["Name"]!.GetValue<string>(), pair["expected"]!["name"]!.GetValue<string>());
        var actual = JsonNode.Parse(ProgramRunner.Run(pair["request"]!.ToJsonString()));
        Assert.True(JsonNode.DeepEquals(pair["expected"], actual), "Complete pinned-Go contextual union mapping record differs.");
    }
}
