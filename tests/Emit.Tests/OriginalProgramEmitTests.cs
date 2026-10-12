using System.IO.Compression;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Emit.Tests;
// Original compiler configuration identities and public expectations are supplied
// by the frozen reference corpus and its exact eleven-record collision supplement.
// The same full formatted-channel assertions execute through the real corpus consumer.
public sealed class OriginalProgramEmitTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " must identify the qualified original emit batch.");
    private static readonly Lazy<Dictionary<string, JsonObject>> Requests = new(() => File.ReadLines(Required("EMIT_CORPUS_REQUESTS")).Select(line => JsonNode.Parse(line)!.AsObject()).ToDictionary(row => row["Name"]!.GetValue<string>(), StringComparer.Ordinal));
    private static readonly Lazy<Dictionary<string, JsonObject>> Reference = new(() =>
    {
        using var input = new StreamReader(new GZipStream(File.OpenRead(Required("EMIT_CORPUS_REFERENCE")), CompressionMode.Decompress));
        Dictionary<string, JsonObject> result = new(StringComparer.Ordinal);
        while (input.ReadLine()is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            string name = row["name"]!.GetValue<string>();
            if (!Requests.Value.ContainsKey(name))
                continue;
            if (row["status"]!.GetValue<string>() != "passed-original-full-harness")
                throw new InvalidOperationException("Full original assertion passes are required for this selected xUnit batch: " + name);
            result.Add(name, row);
        }

        if (!result.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(Requests.Value.Keys))
            throw new InvalidOperationException("Original reference and selected request identities differ.");
        return result;
    });
    private static readonly Lazy<CompilerNoEmitCases> Consumer = new(() => new(Required("EMIT_CORPUS_ROOT")));
    public static IEnumerable<object[]> Cases => Requests.Value.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalProgramOutputsDiagnosticsAndInvariants(string name)
    {
        var expected = Reference.Value[name];
        var actual = Consumer.Value.ObserveEmit(Requests.Value[name]);
        File.AppendAllText(Required("EMIT_CORPUS_ACTUAL"), actual.ToJsonString() + "\n");
        JsonObject? supplement = null;
        string supplementalPath = Path.Combine(Required("EMIT_CORPUS_ROOT"), "artifacts/emit/whole-reference-record-supplement/original.jsonl");
        foreach (string line in File.ReadLines(supplementalPath))
        {
            var row = JsonNode.Parse(line)!.AsObject();
            if (row["name"]!.GetValue<string>() == name)
            {
                supplement = row;
                break;
            }
        }

        CompilerEmitReferenceAssertions.Check(expected, actual, supplement);
    }
}
