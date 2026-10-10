using System.IO.Compression;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class CompilerNoEmitTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(name + " must identify the qualified whole-corpus input.");
    private static readonly Lazy<Dictionary<string, JsonObject>> Requests = new(() =>
        File.ReadLines(Required("CHECKER_NOEMIT_REQUESTS"))
            .Select(line => JsonNode.Parse(line)!.AsObject()).ToDictionary(row => row["Name"]!.GetValue<string>(), StringComparer.Ordinal));
    private static readonly Lazy<Dictionary<string, JsonObject>> Reference = new(() =>
    {
        using var input = new StreamReader(new GZipStream(File.OpenRead(Required("CHECKER_NOEMIT_REFERENCE")), CompressionMode.Decompress));
        Dictionary<string, JsonObject> result = new(StringComparer.Ordinal);
        while (input.ReadLine() is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            if (row["status"]!.GetValue<string>() != "observed")
                throw new InvalidOperationException("Failed original observations are not successful reference rows.");
            result.Add(row["name"]!.GetValue<string>(), row);
        }
        if (!result.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(Requests.Value.Keys))
            throw new InvalidOperationException("Original reference and selected request names differ.");
        return result;
    });
    private static readonly Lazy<CompilerNoEmitCases> Consumer = new(() => new(Required("CHECKER_NOEMIT_ROOT")));

    public static IEnumerable<object[]> Cases => Requests.Value.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalNoEmitDiagnosticsQueriesAndInvariants(string name)
    {
        var expected = Reference.Value[name];
        var actual = Consumer.Value.Observe(Requests.Value[name]);
        // Same-class theory rows are sequential under the unchanged xUnit defaults.
        // Persist complete records so final proof readers can verify the actual row set.
        File.AppendAllText(Required("CHECKER_NOEMIT_ACTUAL"), actual.ToJsonString() + "\n");
        Assert.True(actual["originalAssertions"]!["unionOrdering"]!.GetValue<bool>());
        Assert.True(actual["originalAssertions"]!["parentPointers"]!.GetValue<bool>());
        Assert.True(JsonNode.DeepEquals(expected, actual), "Complete original no-emit record differs: " + name);
    }
}
