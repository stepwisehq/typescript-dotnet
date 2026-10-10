// Authored case selection shared by the whole no-emit process and xUnit consumers.
// Checking, original walker and original invariants remain in CompilerNoEmitRunner.
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace Program.Checks;

public sealed class CompilerNoEmitCases
{
    private readonly string root;
    private readonly Dictionary<string, JsonObject> inventory = new(StringComparer.Ordinal);

    public CompilerNoEmitCases(string repositoryRoot)
    {
        root = Path.GetFullPath(repositoryRoot);
        using var input = new StreamReader(new GZipStream(File.OpenRead(Path.Combine(root,
            "validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz")), CompressionMode.Decompress));
        while (input.ReadLine() is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            inventory.Add(Path.GetFullPath(Path.Combine(root, row["source"]!.GetValue<string>())), row);
        }
    }

    public JsonObject Observe(JsonObject request)
    {
        if (request.Count != 4 || request.Any(pair => pair.Key is not ("Name" or "Source" or "SourceSha256" or "Configuration")))
            throw new ArgumentException("Exactly Name, Source, SourceSha256 and Configuration are required.");
        string name = request["Name"]!.GetValue<string>();
        string source = Path.GetFullPath(request["Source"]!.GetValue<string>());
        string configuration = request["Configuration"]!.GetValue<string>();
        var row = inventory[source];
        byte[] bytes = File.ReadAllBytes(source);
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (hash != request["SourceSha256"]!.GetValue<string>() || hash != row["sourceSha256"]!.GetValue<string>())
            throw new InvalidOperationException("Fixture hash changed.");
        string decoded = DecodeSource(bytes);
        var variants = CompilerFixtureConfigurations.Expand(CompilerFixtureParser.ExtractCompilerSettings(decoded));
        var variant = variants.Single(item => item.Name == configuration);
        return ProgramRunner.ObserveCompilerNoEmit(name, GoString.ToUtf8(source), hash, configuration, decoded,
            variant.Config ?? new Dictionary<string, string>(StringComparer.Ordinal),
            Path.Combine(root, "upstream/TypeScript/tsc/testdata"), row["originalPreConfigurationSkip"]!.GetValue<bool>());
    }

    public static string DecodeSource(byte[] bytes) => VfsInternal.DecodeBytes(GoString.FromBytes(bytes)).Contents;
}
