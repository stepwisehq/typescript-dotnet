// Authored corpus reader and strict value comparison. Expectations are immutable
// original-Go bytes; request/expected hashes and counts are checked before use.
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Configuration.Checks;

namespace Expressions.Checks;
public static class ExpressionsCorpus
{
    private sealed record Row(string Request, string Expected);
    private static readonly Lazy<IReadOnlyDictionary<string, Row>> rows = new(Load);
    public static IEnumerable<string> Names => rows.Value.Keys;

    private static string Root()
    {
        string? directory = Environment.CurrentDirectory;
        while (directory is not null && !File.Exists(Path.Combine(directory, "validation/expressions-corpus/manifest.json")))
            directory = Path.GetDirectoryName(directory);
        return directory ?? throw new InvalidOperationException("Expressions corpus manifest is unavailable.");
    }

    private static IReadOnlyDictionary<string, Row> Load()
    {
        string root = Root();
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, "validation/expressions-corpus/manifest.json")));
        Dictionary<string, Row> result = [];
        foreach (var group in document.RootElement.GetProperty("groups").EnumerateArray())
        {
            string requestsPath = Path.Combine(root, group.GetProperty("requests").GetString()!);
            string expectedPath = Path.Combine(root, group.GetProperty("expected").GetString()!);
            foreach (var(path, key)in new[]
            {
                (requestsPath, "requestsSha256"),
                (expectedPath, "expectedSha256")
            }

            )
                Check.Equal(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))), group.GetProperty(key).GetString(), "Canonical corpus hash " + path);
            using var requests = new StreamReader(requestsPath, new UTF8Encoding(false, true));
            using Stream file = File.OpenRead(expectedPath);
            using Stream decoded = expectedPath.EndsWith(".gz", StringComparison.Ordinal) ? new GZipStream(file, CompressionMode.Decompress) : file;
            using var expected = new StreamReader(decoded, new UTF8Encoding(false, true));
            int count = 0;
            while (requests.ReadLine()is { } request)
            {
                string response = expected.ReadLine() ?? throw new InvalidOperationException("Missing original expected row.");
                using var requestDoc = JsonDocument.Parse(request);
                using var expectedDoc = JsonDocument.Parse(response);
                string name = requestDoc.RootElement.GetProperty("Name").GetString()!;
                Check.Equal(expectedDoc.RootElement.GetProperty("name").GetString(), name, "Corpus request identity");
                result.Add(name, new Row(request, response));
                count++;
            }

            Check.True(expected.ReadLine()is null, "No extra expected rows");
            Check.Equal(count, group.GetProperty("cases").GetInt32(), "Corpus group cardinality");
        }

        Check.Equal(result.Count, document.RootElement.GetProperty("cases").GetInt32(), "Complete corpus cardinality");
        return result;
    }

    public static void Run(string name)
    {
        Row row = rows.Value[name];
        string actual = global::Program.Checks.ProgramRunner.Run(row.Request);
        using var expectedDoc = JsonDocument.Parse(row.Expected);
        using var actualDoc = JsonDocument.Parse(actual);
        Equal(expectedDoc.RootElement, actualDoc.RootElement, name);
    }

    private static void Equal(JsonElement expected, JsonElement actual, string path)
    {
        Check.Equal(actual.ValueKind, expected.ValueKind, path + " JSON kind");
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedFields = expected.EnumerateObject().ToDictionary(field => field.Name, field => field.Value);
                var actualFields = actual.EnumerateObject().ToDictionary(field => field.Name, field => field.Value);
                Check.Equal(actualFields.Count, expectedFields.Count, path + " field count");
                foreach (var(key, value)in expectedFields)
                {
                    Check.True(actualFields.TryGetValue(key, out JsonElement field), path + " missing field " + key);
                    Equal(value, field, path + "." + key);
                }

                break;
            case JsonValueKind.Array:
                Check.Equal(actual.GetArrayLength(), expected.GetArrayLength(), path + " array length");
                for (int index = 0; index < expected.GetArrayLength(); index++)
                    Equal(expected[index], actual[index], path + "[" + index + "]");
                break;
            case JsonValueKind.String:
                Check.Equal(actual.GetString(), expected.GetString(), path + " string");
                break;
            case JsonValueKind.Number:
                Check.Equal(actual.GetRawText(), expected.GetRawText(), path + " numeric spelling");
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                break;
            default:
                throw new InvalidOperationException("Unexpected corpus JSON value.");
        }
    }
}
