using System.Text.Json;
using System.Text.Json.Nodes;
using TypeScript.Scanner.Validation;
using Xunit;

namespace TypeScript.Scanner.Tests;

// Authored inputs observed by the pinned Go scanner, not upstream test bodies.
public sealed class ScannerSnapshotTests
{
    public static IEnumerable<object[]> Cases()
    {
        var corpus = Path.Combine(AppContext.BaseDirectory, "corpus");
        var inputs = File.ReadAllLines(Path.Combine(corpus, "scanner-snapshot-requests.jsonl"));
        var expected = File.ReadAllLines(Path.Combine(corpus, "scanner-snapshot-expected.jsonl"));
        string[] names = ["snapshot-zero-before-mark", "snapshot-directive-rewind", "snapshot-borrowed-token-rewind"];
        if (inputs.Length != names.Length || expected.Length != names.Length)
            throw new InvalidDataException("Snapshot reference row count changed");
        for (int i = 0; i < names.Length; i++)
        {
            var request = JsonSerializer.Deserialize(inputs[i], ContractJson.Default.Request)
                ?? throw new InvalidDataException("Missing snapshot request");
            if (request.Name != names[i]) throw new InvalidDataException("Snapshot reference identity/order changed");
            yield return [request.Name, inputs[i], expected[i]];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CompleteRecordMatchesPinnedGo(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, ContractJson.Default.Request)!;
        var actual = JsonSerializer.Serialize(Runner.Run(request), ContractJson.Default.Response);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)),
            $"{name}: complete scanner observation differs from pinned Go");
    }

    [Fact]
    public void SavedDirectiveHeadersShareWritesIntoSpareBackingLikePinnedGo()
    {
        // scanner.go:201,293-298 copies the slice header, retaining its backing.
        // Exact public observations are from tools/scanner-snapshot-reference/main.go.
        const string source = "// @ts-ignore\na\n// @ts-ignore\nb\n// @ts-ignore\nc\n// @ts-ignore\nd\n";
        const string alternate = "// @ts-ignore\na\n// @ts-ignore\nb\n// @ts-ignore\nc\n// @ts-expect-error\nd\n";
        var scanner = new Scanner();
        scanner.SetText(source);
        scanner.Scan(); scanner.Scan(); scanner.Scan();
        var shorter = scanner.Mark();
        var shortBefore = Observe(scanner);
        scanner.Scan();
        var longer = scanner.Mark();
        var longBefore = Observe(scanner);
        // Source text is not a field of the saved scanner state.
        scanner.SetText(alternate);
        scanner.Rewind(shorter);
        scanner.Scan();
        var afterReplacement = Observe(scanner);
        scanner.Rewind(longer);
        var rewoundLong = Observe(scanner);
        var actual = JsonSerializer.Serialize(new { shortBefore, longBefore, afterReplacement, rewoundLong });
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "corpus", "scanner-snapshot-sharing-expected.jsonl"));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)),
            "Copied directive headers must retain the pinned Go shared-write consequence");
    }

    private static object[] Observe(Scanner scanner) => (scanner.CommentDirectives() ?? [])
        .Select(d => (object)new { start = d.Loc.Pos(), end = d.Loc.End(), type = (int)d.Kind }).ToArray();
}
