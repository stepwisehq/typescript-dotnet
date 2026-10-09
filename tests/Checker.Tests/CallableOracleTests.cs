using System.Text.Json;
using TypeScript.Checking.Validation;
using Xunit;

namespace TypeScript.Checking.Tests;
public sealed class CallableOracleTests
{
    public static IEnumerable<object[]> Cases()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "callable-corpus");
        foreach (string path in Directory.GetFiles(folder, "*-requests.jsonl").Order(StringComparer.Ordinal))
        {
            using var expected = File.ReadLines(path.Replace("-requests.jsonl", "-expected.jsonl", StringComparison.Ordinal)).GetEnumerator();
            foreach (string input in File.ReadLines(path))
            {
                if (!expected.MoveNext())
                    throw new InvalidDataException("Missing callable oracle row");
                var request = JsonSerializer.Deserialize(input, CheckerJson.Default.Request)!;
                if (request.Profile != "Callable" || !request.CallableObservations)
                    throw new InvalidDataException("Callable profile/graph missing");
                yield return[request.Name, input, expected.Current];
            }

            if (expected.MoveNext())
                throw new InvalidDataException("Extra callable oracle row");
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void MatchesOriginalSignaturesCallsContextualBodiesAndDiagnostics(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, CheckerJson.Default.Request)!;
        using var want = JsonDocument.Parse(expected);
        using var actual = JsonDocument.Parse(JsonSerializer.Serialize(Runner.Run(request), CheckerJson.Default.Result));
        var difference = OracleTests.FirstDifference(want.RootElement, actual.RootElement, "$", 0);
        Assert.True(difference is null, name + ": " + difference);
    }
}
