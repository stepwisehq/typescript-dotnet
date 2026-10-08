using System.Text.Json;
using TypeScript.Checking.Validation;
using Xunit;

namespace TypeScript.Checking.Tests;
public class ObjectOracleTests
{
    public static IEnumerable<object[]> Cases()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "object-corpus");
        foreach (string path in Directory.GetFiles(directory, "*-requests.jsonl").Order())
        {
            using var requests = File.ReadLines(path).GetEnumerator();
            using var expected = File.ReadLines(path.Replace("-requests.jsonl", "-expected.jsonl", StringComparison.Ordinal)).GetEnumerator();
            while (requests.MoveNext())
            {
                if (!expected.MoveNext())
                    throw new InvalidDataException("Object oracle outputs incomplete");
                var request = JsonSerializer.Deserialize(requests.Current, CheckerJson.Default.Request)!;
                yield return[request.Name, requests.Current, expected.Current];
            }

            if (expected.MoveNext())
                throw new InvalidDataException("Extra object oracle outputs");
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void MatchesIndependentOriginalObjectTypesPropertiesRelationsAndDiagnostics(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, CheckerJson.Default.Request)!;
        using var want = JsonDocument.Parse(expected);
        using var actual = JsonDocument.Parse(JsonSerializer.Serialize(Runner.Run(request), CheckerJson.Default.Result));
        string? difference = OracleTests.FirstDifference(want.RootElement, actual.RootElement, "$", 0);
        Assert.True(difference is null, name + ": " + difference);
    }

    [Fact]
    public void HistoricalObjectNegativesWereExplicitlyReplacedByPositiveScopeCoverage()
    {
        foreach (string text in new[]
        {
            "const value={};",
            "let value:{}={};"
        }

        )
            Assert.True(Runner.Admission(new() { Text = text }).Admitted);
    }
}
