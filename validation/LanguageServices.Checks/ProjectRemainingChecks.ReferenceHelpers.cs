// Complete original reference-program fixture helpers, projectreferencesprogram_test.go:455.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private static void AddPackageConfig(Dictionary<string, Input> files, string package, bool preserve, string[]? references)
    {
        var options = new Dictionary<string, object?>
        {
            ["outDir"] = "lib",
            ["rootDir"] = "src",
            ["composite"] = true
        };
        if (preserve)
            options["preserveSymlinks"] = true;
        List<object?>? refs = null;
        if (references is not null)
            foreach (var reference in references)
                (refs ??= new()).Add(new Dictionary<string, object?> { ["path"] = reference });
        var(text, error) = Core.StringifyJson(new Dictionary<string, object?> { ["compilerOptions"] = options, ["include"] = new List<object?> { "src" }, ["references"] = refs }, ByteStringValue.FromString("    "), ByteStringValue.FromString("  "));
        Check(error is null);
        var path = "/user/username/projects/myproject/packages/" + package + "/tsconfig.json";
        files[path] = new(path, text.Materialize()!);
    }

    private static (Dictionary<string, Input> Files, string Entry, string Foo, string Bar) SymlinkReferenceFiles(bool preserve, string scope, bool subfolder)
    {
        var entry = "/user/username/projects/myproject/packages/A/src/" + (subfolder ? "test.ts" : "index.ts");
        var foo = "/user/username/projects/myproject/packages/B/src/" + (subfolder ? "foo.ts" : "index.ts");
        var bar = "/user/username/projects/myproject/packages/B/src/" + (subfolder ? "bar/foo.ts" : "bar.ts");
        var package = "/user/username/projects/myproject/packages/B/package.json";
        var link = (subfolder ? filesForSymlinkReferencesInSubfolderRaw4 : filesForSymlinkReferencesRaw4).Replace("%s", scope, StringComparison.Ordinal);
        var files = new Dictionary<string, Input>
        {
            [package] = new(package, subfolder ? filesForSymlinkReferencesInSubfolderRaw0 : filesForSymlinkReferencesRaw0),
            [entry] = new(entry, (subfolder ? filesForSymlinkReferencesInSubfolderRaw1 : filesForSymlinkReferencesRaw1).Replace("%s", scope, StringComparison.Ordinal)),
            [foo] = new(foo, subfolder ? filesForSymlinkReferencesInSubfolderRaw2 : filesForSymlinkReferencesRaw2),
            [bar] = new(bar, subfolder ? filesForSymlinkReferencesInSubfolderRaw3 : filesForSymlinkReferencesRaw3),
            [link] = new(link, Symlink: "/user/username/projects/myproject/packages/B")
        };
        AddPackageConfig(files, "A", preserve, ["../B"]);
        AddPackageConfig(files, "B", preserve, null);
        return (files, entry, foo, bar);
    }

    private static (Dictionary<string, Input> Files, string Entry, string File) DirectorySubpathFiles(string scope)
    {
        const string entry = "/user/username/projects/myproject/packages/a/src/index.ts";
        const string file = "/user/username/projects/myproject/packages/b/src/File/index.ts";
        const string package = "/user/username/projects/myproject/packages/b/package.json";
        var link = "/user/username/projects/myproject/node_modules/" + scope + "b";
        var files = new Dictionary<string, Input>
        {
            [package] = new(package, filesForDirectorySubpathSymlinkReferencesRaw0),
            [entry] = new(entry, filesForDirectorySubpathSymlinkReferencesRaw1.Replace("%s", scope, StringComparison.Ordinal)),
            [file] = new(file, filesForDirectorySubpathSymlinkReferencesRaw2),
            [link] = new(link, Symlink: "/user/username/projects/myproject/packages/b")
        };
        AddPackageConfig(files, "a", false, ["../b"]);
        AddPackageConfig(files, "b", false, null);
        return (files, entry, file);
    }
}
