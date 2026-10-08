using System.Text.Json;
using System.Text.Json.Nodes;
using TypeScript.Scanner.Validation;
using Xunit;

namespace TypeScript.Scanner.Tests;

public class OracleTests
{
    public static IEnumerable<object[]> Cases()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "corpus");
        using var requests = File.ReadLines(Path.Combine(directory, "requests.jsonl")).GetEnumerator();
        using var expected = File.ReadLines(Path.Combine(directory, "expected.jsonl")).GetEnumerator();
        while (requests.MoveNext())
        {
            if (!expected.MoveNext()) throw new InvalidDataException("Oracle outputs are incomplete");
            var request = JsonSerializer.Deserialize(requests.Current, ContractJson.Default.Request)!;
            yield return [request.Name, requests.Current, expected.Current];
        }
        if (expected.MoveNext()) throw new InvalidDataException("Oracle outputs have extra rows");
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesPinnedGoScanner(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, ContractJson.Default.Request)!;
        string actual = JsonSerializer.Serialize(Runner.Run(request), ContractJson.Default.Response);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)), $"{name}: scanner response differs from pinned Go oracle; tools/compare.py reports the first differing field.");
    }

    [Fact]
    public void PublicTextSetterUsesUtf8BytePositions()
    {
        var scanner = new Scanner();
        scanner.SetText("α🦀");
        scanner.Scan();
        Assert.Equal(0, scanner.TokenStart());
        Assert.Equal(2, scanner.TokenEnd());
        Assert.Equal("α", scanner.TokenText());
        Assert.Equal(new byte[] { 0xCE, 0xB1 }, scanner.TokenTextBytes());
    }
    [Fact]
    public void ScanStringPreservesLoneSurrogatesLikeUpstream()
    {
        TypeScript.Foundation.ScannerChecks.Run("TestScanStringPreservesLoneSurrogates");
    }

    [Fact]
    public void ScanSourceKeywordLikeUpstream()
    {
        TypeScript.Foundation.ScannerChecks.Run("TestScanSourceKeyword");
    }

}
