// Authored differential inputs; expected hashes come from the unchanged pinned
// checker.go:17764–17828 builder/writer bodies and zeebo/xxh3 v1.1.0.
// No private cache inventory or allocator-capacity expectations.
using System.Globalization;
using System.Text.Json;
using TypeScript.Checking;
using TypeScript.Scanner;
using CacheHashKey = TypeScript.Checking.Checker.CacheHashKey;

namespace TypeScript.Foundation;

public static class KeyBuilderChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["empty", "byte-191", "byte-192", "byte-193", "string-boundaries", "large-string-spill",
     "integer-boundaries", "mixed-raw-and-integers", "hash-then-append", "recursive-ref-stream"];

    private static readonly Lazy<JsonDocument> vectors = new(() =>
    {
        using Stream stream = typeof(KeyBuilderChecks).Assembly.GetManifestResourceStream("TypeScript.Foundation.KeyBuilderVectors.json")
            ?? throw new InvalidOperationException("Missing genuine Go key builder vectors.");
        var document = JsonDocument.Parse(stream);
        var names = document.RootElement.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();
        if (!names.SequenceEqual(Cases)) throw new InvalidOperationException("Go key vector identities/order differ.");
        return document;
    });

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void Run(string name)
    {
        int index = Cases.ToList().IndexOf(name);
        if (index < 0) throw new ArgumentException("Unknown key builder check: " + name, nameof(name));
        JsonElement row = vectors.Value.RootElement[index];
        var builder = new Checker.keyBuilder();
        Require(default(CacheHashKey).IsZero(), "Default key remains the zero sentinel");
        Require(!builder.hash().IsZero(), "Hash of an empty stream differs from the default zero sentinel");
        var snapshots = row.GetProperty("snapshots").EnumerateArray().ToArray();
        var originalOperations = row.GetProperty("operations");
        var operations = originalOperations.ValueKind == JsonValueKind.Null ? Array.Empty<JsonElement>() : originalOperations.EnumerateArray().ToArray();
        Require(snapshots.Length == operations.Length + 1, "Each writer boundary has an original hash observation");
        CheckHash(ref builder, snapshots[0], 0);
        for (int i = 0; i < operations.Length; i++)
        {
            var op = operations[i];
            switch (op.GetProperty("kind").GetString())
            {
                case "bytes":
                    for (int j = 0; j < op.GetProperty("count").GetInt32(); j++) builder.writeByte(unchecked((byte)(j * 137 + 17)));
                    break;
                case "raw": builder.writeString(GoString.FromBytes(Convert.FromHexString(op.GetProperty("value").GetString()!))); break;
                case "u32": builder.writeUint32(uint.Parse(op.GetProperty("value").GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture)); break;
                case "u64": builder.writeUint64(ulong.Parse(op.GetProperty("value").GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture)); break;
                case "int": builder.writeInt(int.Parse(op.GetProperty("value").GetString()!, CultureInfo.InvariantCulture)); break;
                case "recurse": WriteRecursive(ref builder, op.GetProperty("count").GetInt32()); break;
                default: throw new InvalidOperationException("Unknown original key vector writer.");
            }
            CheckHash(ref builder, snapshots[i + 1], i + 1);
        }
    }

    private static void CheckHash(ref Checker.keyBuilder builder, JsonElement expected, int after)
    {
        Require(expected.GetProperty("after").GetInt32() == after, "Original writer observation order");
        var key = builder.hash();
        Require(key.Value.ToString("x32", CultureInfo.InvariantCulture) == expected.GetProperty("hash").GetString(), "XXH3-128 high/low value matches actual pinned Go stream hash");
        Require(builder.hash() == key, "Repeated hashing preserves the logical stream before later writes");
        var dictionary = new Dictionary<CacheHashKey, string> { [key] = "present" };
        Require(dictionary.TryGetValue(builder.hash(), out string? value) && value == "present", "Value-key equality and hash code support fresh equal cache lookups");
    }

    // Same authored operation sequence as the Go pointer witness; ref is necessary
    // now that the production builder is a value descriptor.
    private static void WriteRecursive(ref Checker.keyBuilder builder, int depth)
    {
        builder.writeByte((byte)depth);
        builder.writeUint32((uint)(depth * 17 + 3));
        if (depth > 0) WriteRecursive(ref builder, depth - 1);
        builder.writeByte(0xff);
    }
}
