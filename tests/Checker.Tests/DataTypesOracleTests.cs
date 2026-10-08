using System.Text.Json;
using TypeScript.Checking.Validation;
using Xunit;

namespace TypeScript.Checking.Tests;
public class DataTypesOracleTests
{
    public static IEnumerable<object[]> Cases()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "data-corpus");
        // Thin integration fixtures are historical triage, not this acceptance
        // contract. Every accepted row must request the richer graph.
        foreach (string group in new[]
        {
            "tiny",
            "matrix"
        }

        )
        {
            using var requests = File.ReadLines(Path.Combine(folder, group + "-requests.jsonl")).GetEnumerator();
            using var expected = File.ReadLines(Path.Combine(folder, group + "-expected.jsonl")).GetEnumerator();
            while (requests.MoveNext())
            {
                if (!expected.MoveNext())
                    throw new InvalidDataException("Missing data-type oracle row");
                var request = JsonSerializer.Deserialize(requests.Current, CheckerJson.Default.Request)!;
                if (!request.DataObservations)
                    throw new InvalidDataException("Richer graph missing");
                yield return[request.Name, requests.Current, expected.Current];
            }

            if (expected.MoveNext())
                throw new InvalidDataException("Extra data-type oracle row");
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void MatchesOriginalNamedRecursiveCompositeGraphsAndDiagnostics(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, CheckerJson.Default.Request)!;
        using var want = JsonDocument.Parse(expected);
        using var actual = JsonDocument.Parse(JsonSerializer.Serialize(Runner.Run(request), CheckerJson.Default.Result));
        var difference = OracleTests.FirstDifference(want.RootElement, actual.RootElement, "$", 0);
        Assert.True(difference is null, name + ": " + difference);
    }

    [Fact]
    public void RetiredControlsHaveVisibleReasonsAndAreAdmittedByCurrentBoundary()
    {
        string folder = AppContext.BaseDirectory;
        using var retired = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "data-corpus/retired-admission-controls.json")));
        foreach (var item in retired.RootElement.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("reason").GetString()));
            string corpus = item.GetProperty("corpus").GetString() == "checker-corpus" ? "corpus" : "object-corpus";
            using var controls = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, corpus, "admission-controls.json")));
            var original = controls.RootElement.EnumerateArray().Single(c => c.GetProperty("name").GetString() == item.GetProperty("name").GetString());
            var request = JsonSerializer.Deserialize(original.GetRawText(), CheckerJson.Default.Request)!;
            Assert.True(Runner.Admission(request).Admitted, request.Name);
        }
    }
}
