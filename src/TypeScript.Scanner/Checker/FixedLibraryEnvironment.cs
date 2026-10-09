// Authored fixed-environment adapter. Library declarations are exact upstream bytes.
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal static class FixedLibraryEnvironment
{
    private const string Revision = "fed0bf24149fb1ed36039212648bafdafc1ea10e";
    private static readonly (string Name, string Hash)[] Expected = [("lib.es5.d.ts", "45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215"), ("lib.decorators.d.ts", "1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4"), ("lib.decorators.legacy.d.ts", "b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da")];
    private static readonly byte[][] Texts = ReadAndVerify();
    // Keep AST/binder/checker state isolated: only immutable private bytes are cached.
    internal static IReadOnlyList<SourceFile> Load()
    {
        var files = new SourceFile[Expected.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = "/lib/" + Expected[i].Name;
            files[i] = Parser.ParseSourceFile(new SourceFileParseOptions(fileName, fileName), Texts[i], ScriptKind.TS);
        }

        return files;
    }

    private static byte[][] ReadAndVerify()
    {
        Assembly assembly = typeof(FixedLibraryEnvironment).Assembly;
        using Stream manifestStream = assembly.GetManifestResourceStream("TypeScript.Checking.Libraries.Manifest.json") ?? throw new InvalidDataException("Fixed library manifest is missing.");
        using JsonDocument manifest = JsonDocument.Parse(manifestStream);
        if (manifest.RootElement.GetProperty("upstreamRevision").GetString() != Revision)
            throw new InvalidDataException("Fixed library revision differs from the accepted pin.");
        JsonElement rows = manifest.RootElement.GetProperty("files");
        if (rows.GetArrayLength() != Expected.Length)
            throw new InvalidDataException("Fixed library manifest has an unexpected file count.");
        var result = new byte[Expected.Length][];
        for (int i = 0; i < Expected.Length; i++)
        {
            var expected = Expected[i];
            JsonElement row = rows[i];
            string resource = "TypeScript.Checking.Libraries." + expected.Name;
            if (row.GetProperty("fileName").GetString() != "/lib/" + expected.Name || row.GetProperty("resourceName").GetString() != resource || row.GetProperty("sha256").GetString() != expected.Hash)
                throw new InvalidDataException("Fixed library manifest order or identity differs from the contract.");
            using Stream input = assembly.GetManifestResourceStream(resource) ?? throw new InvalidDataException("Fixed library resource is missing: " + resource);
            using var output = new MemoryStream();
            input.CopyTo(output);
            byte[] bytes = output.ToArray();
            if (bytes.Length != row.GetProperty("bytes").GetInt32() || Convert.ToHexStringLower(SHA256.HashData(bytes)) != expected.Hash)
                throw new InvalidDataException("Fixed library resource bytes differ from the upstream pin: " + expected.Name);
            result[i] = bytes;
        }

        return result;
    }
}
