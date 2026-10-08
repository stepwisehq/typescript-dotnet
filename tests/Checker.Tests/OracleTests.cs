using System.Text.Json;
using TypeScript.Checking.Validation;
using Xunit;

namespace TypeScript.Checking.Tests;
public class OracleTests
{
    public static IEnumerable<object[]> Cases()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "corpus");
        foreach (string requestsPath in Directory.GetFiles(directory, "*-requests.jsonl").Order())
        {
            string expectedPath = requestsPath.Replace("-requests.jsonl", "-expected.jsonl", StringComparison.Ordinal);
            using var requests = File.ReadLines(requestsPath).GetEnumerator();
            using var expected = File.ReadLines(expectedPath).GetEnumerator();
            while (requests.MoveNext())
            {
                if (!expected.MoveNext())
                    throw new InvalidDataException("Checker oracle outputs are incomplete");
                var request = JsonSerializer.Deserialize(requests.Current, CheckerJson.Default.Request)!;
                yield return[request.Name, requests.Current, expected.Current];
            }

            if (expected.MoveNext())
                throw new InvalidDataException("Checker oracle outputs have extra rows");
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndependentPinnedGoChecker(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, CheckerJson.Default.Request)!;
        string actual = JsonSerializer.Serialize(Runner.Run(request), CheckerJson.Default.Result);
        using var want = JsonDocument.Parse(expected);
        using var got = JsonDocument.Parse(actual);
        string? difference = FirstDifference(want.RootElement, got.RootElement, "$", 0);
        Assert.True(difference is null, $"{name}: {difference}");
    }

    internal static string? FirstDifference(JsonElement want, JsonElement got, string path, int depth)
    {
        if (depth > 2048)
            return path + ": comparison depth limit";
        if (want.ValueKind != got.ValueKind)
            return $"{path}: JSON kind expected {want.ValueKind}, actual {got.ValueKind}";
        if (want.ValueKind == JsonValueKind.Object)
        {
            foreach (var field in want.EnumerateObject())
            {
                if (!got.TryGetProperty(field.Name, out var actual))
                    return path + "." + field.Name + ": missing field";
                if (FirstDifference(field.Value, actual, path + "." + field.Name, depth + 1)is { } difference)
                    return difference;
            }

            foreach (var field in got.EnumerateObject())
                if (!want.TryGetProperty(field.Name, out _))
                    return path + "." + field.Name + ": unexpected field";
            return null;
        }

        if (want.ValueKind == JsonValueKind.Array)
        {
            if (want.GetArrayLength() != got.GetArrayLength())
                return $"{path}: length expected {want.GetArrayLength()}, actual {got.GetArrayLength()}";
            int i = 0;
            foreach (var field in want.EnumerateArray())
            {
                if (FirstDifference(field, got[i], $"{path}[{i}]", depth + 1)is { } difference)
                    return difference;
                i++;
            }

            return null;
        }

        static string Short(string value) => value.Length <= 120 ? value : value[..120] + "…";
        return want.GetRawText() == got.GetRawText() || (want.ValueKind == JsonValueKind.String && want.GetString() == got.GetString()) ? null : $"{path}: expected {Short(want.GetRawText())}, actual {Short(got.GetRawText())}";
    }
}
