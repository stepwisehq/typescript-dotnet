// Authored no-emit harness environment adapter. Public declaration bytes come
// from the original repository's locked TypeScript package, not a substitute API.
// This changes only available virtual files, never options or program roots.
using System.Security.Cryptography;
using System.Text.Json;
using TypeScript.Scanner;

namespace Program.Checks;

public static class CompilerApiEnvironment
{
    private const string RelativeDirectory = "validation/checker-noemit-environment/typescript-6.0.3";
    private static readonly string[] Members =
        ["lib/typescript.d.ts", "lib/tsserverlibrary.d.ts", "LICENSE.txt", "ThirdPartyNoticeText.txt", "package.json"];
    private static readonly (string Virtual, string Member)[] Roles =
        [("/.ts/typescript.d.ts", "lib/typescript.d.ts"), ("/.ts/tsserverlibrary.d.ts", "lib/tsserverlibrary.d.ts")];

    public static void AddToFiles(Dictionary<string, CompilerBaselineFileSystem.Input> files, string testDataDirectory)
    {
        if (!files.Values.Any(file => Roles.Any(role => file.Contents.Contains(role.Virtual, StringComparison.Ordinal))))
            return;

        string repository = FindRepository(testDataDirectory);
        string expectedDirectory = Path.GetFullPath(Path.Combine(repository, RelativeDirectory));
        string directory = Environment.GetEnvironmentVariable("CHECKER_NOEMIT_API_ENVIRONMENT") is { Length: > 0 } configured
            ? Path.GetFullPath(configured) : expectedDirectory;
        Require(directory == expectedDirectory, "API environment must use the fixed repository package location.");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "provenance.json")));
        JsonElement provenance = document.RootElement;
        Require(provenance.GetProperty("classification").GetString() == "authentic-upstream-locked-public-api-environment-adaptation" &&
            provenance.GetProperty("upstream").GetString() == "fed0bf24149fb1ed36039212648bafdafc1ea10e" &&
            provenance.GetProperty("version").GetString() == "6.0.3", "Unrecognized API environment provenance.");
        Require(provenance.GetProperty("lockSource").GetString() == "upstream/TypeScript/package-lock.json", "Wrong API lock source.");
        Require(Hash(File.ReadAllBytes(Path.Combine(repository, "upstream/TypeScript/package-lock.json"))) ==
            provenance.GetProperty("lockSourceSha256").GetString(), "API lock source changed.");
        Require(provenance.GetProperty("generator").GetString() == "tools/prepare-checker-api-environment.py" &&
            Hash(File.ReadAllBytes(Path.Combine(repository, "tools/prepare-checker-api-environment.py"))) ==
            provenance.GetProperty("generatorSha256").GetString(), "API environment preparation changed.");
        JsonElement metadata = provenance.GetProperty("files"), virtualRoles = provenance.GetProperty("virtualRoles");
        Require(metadata.EnumerateObject().Count() == Members.Length && virtualRoles.EnumerateObject().Count() == Roles.Length,
            "API environment member or role set changed.");
        Dictionary<string, byte[]> payloads = new(StringComparer.Ordinal);
        foreach (string member in Members)
        {
            JsonElement entry = metadata.GetProperty(member);
            byte[] bytes = File.ReadAllBytes(Path.Combine(directory, member));
            Require(entry.GetProperty("originalMember").GetString() == "package/" + member &&
                bytes.Length == entry.GetProperty("bytes").GetInt32() && Hash(bytes) == entry.GetProperty("sha256").GetString(),
                "API package member differs: " + member);
            payloads.Add(member, bytes);
        }
        Require(provenance.GetProperty("unavailableOriginalRoles").EnumerateArray().Select(value => value.GetString()).SequenceEqual(
            new[] { "/.ts/typescript.internal.d.ts", "/.ts/tsserverlibrary.internal.d.ts" }), "Internal API role disposition changed.");
        // Validate everything before modifying the fixture dictionary. An original
        // explicit role is retained only when it already has the exact same bytes.
        foreach (var role in Roles)
        {
            Require(virtualRoles.GetProperty(role.Virtual).GetString() == role.Member, "API virtual role changed.");
            if (files.TryGetValue(role.Virtual, out var existing))
                Require(!existing.Directory && existing.Symlink == null && !existing.ReadFailure &&
                    existing.Contents == GoString.FromBytes(payloads[role.Member]), "Conflicting explicit API fixture role.");
        }
        foreach (var role in Roles)
            files[role.Virtual] = new(role.Virtual, GoString.FromBytes(payloads[role.Member]));
    }

    private static string FindRepository(string testDataDirectory)
    {
        for (DirectoryInfo? directory = new(Path.GetFullPath(testDataDirectory)); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, RelativeDirectory, "provenance.json")))
                return directory.FullName;
        throw new InvalidDataException("Authentic locked API environment is unavailable.");
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
