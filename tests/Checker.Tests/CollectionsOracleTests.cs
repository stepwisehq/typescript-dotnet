// Authored differential input reuse. Expected responses are actual pinned Go
// observations; this wrapper calls the same source-generated runner as native.
using System.IO.Compression;
using System.Text.Json;
using TypeScript.Checking.CollectionsValidation;
using Xunit;

namespace TypeScript.Checking.Tests;
public sealed class CollectionsOracleTests
{
    public static IEnumerable<object[]> Cases()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "collections-corpus");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")));
        int total = 0;
        foreach (var group in manifest.RootElement.GetProperty("groups").EnumerateArray())
        {
            string requestsPath = Path.Combine(folder, Path.GetFileName(group.GetProperty("requests").GetString()!));
            string expectedPath = Path.Combine(folder, Path.GetFileName(group.GetProperty("expected").GetString()!));
            using var raw = File.OpenRead(expectedPath);
            using var zipped = new GZipStream(raw, CompressionMode.Decompress);
            using var expected = new StreamReader(zipped);
            int count = 0;
            foreach (string input in File.ReadLines(requestsPath))
            {
                string row = expected.ReadLine() ?? throw new InvalidDataException("Missing collection oracle row");
                var request = JsonSerializer.Deserialize(input, CollectionsJson.Default.Request)!;
                if (request.Profile != "Collections" || request.StoredObservations != group.GetProperty("storedObservations").GetBoolean())
                    throw new InvalidDataException("Collection profile/observation controls differ from manifest");
                count++;
                total++;
                yield return[request.Name, input, row];
            }

            if (expected.ReadLine() != null || count != group.GetProperty("cases").GetInt32())
                throw new InvalidDataException("Collection oracle group count differs from manifest");
        }

        if (total != manifest.RootElement.GetProperty("cases").GetInt32())
            throw new InvalidDataException("Collection oracle registry count differs from manifest");
    }

    [Theory, MemberData(nameof(Cases))]
    public void MatchesOriginalFixedLibraryGenericsCollectionsDiagnosticsAndStoredState(string name, string input, string expected)
    {
        var request = JsonSerializer.Deserialize(input, CollectionsJson.Default.Request)!;
        using var want = JsonDocument.Parse(expected);
        using var actual = JsonDocument.Parse(JsonSerializer.Serialize(Runner.Run(request), CollectionsJson.Default.Result));
        string? difference = OracleTests.FirstDifference(want.RootElement, actual.RootElement, "$", 0);
        Assert.True(difference is null, name + ": " + difference);
    }
}
