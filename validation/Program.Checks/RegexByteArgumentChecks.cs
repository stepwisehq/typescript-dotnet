using System.IO.Compression;
using System.Text.Json.Nodes;

namespace Program.Checks;

// Authored byte-role regressions for checker/grammarchecks.go:69 and scanner.errorAt.
// The same complete pinned-Go Program records are checked by xUnit and NativeAOT.
public static class RegexByteArgumentChecks
{
    public static readonly string[] Names = ["regex-unicode-invalid", "regex-ascii-invalid", "regex-unicode-valid"];

    public static Dictionary<string, JsonObject> Read(Stream compressed) => Read(compressed, Names);

    // Same complete-record seam for bounded additional pinned-Go grammar controls.
    public static Dictionary<string, JsonObject> Read(Stream compressed, IEnumerable<string> expectedNames)
    {
        using var reader = new StreamReader(new GZipStream(compressed, CompressionMode.Decompress));
        Dictionary<string, JsonObject> result = new(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            string name = row["request"]!["Name"]!.GetValue<string>();
            if (row["expected"]!["name"]!.GetValue<string>() != name)
                throw new InvalidOperationException("Regex reference identity differs.");
            result.Add(name, row);
        }
        if (!result.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedNames))
            throw new InvalidOperationException("Regex reference cases differ.");
        return result;
    }

    public static void Check(JsonObject row)
    {
        var actual = JsonNode.Parse(ProgramRunner.Run(row["request"]!.ToJsonString()));
        if (!JsonNode.DeepEquals(row["expected"], actual))
            throw new InvalidOperationException("Complete pinned-Go regex observation differs: " + row["request"]!["Name"]);
    }

    public static void Run(string resourcePath)
    {
        using var input = File.OpenRead(resourcePath);
        var rows = Read(input);
        foreach (string name in Names) Check(rows[name]);
        Console.WriteLine("regex-byte-argument-checks: 3 passed");
    }
}
