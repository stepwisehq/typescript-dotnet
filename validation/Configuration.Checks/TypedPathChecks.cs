// Original pinned typed/untitled path assertions. Raw strings enter through
// Go-byte wrappers, not the scalar disk host. Process scheduling is adapted;
// mixed free-string APIs and JSON text callbacks have explicit provenance.
using TypeScript.Scanner;
using TypeScript.Json;
using static TypeScript.Scanner.TsPath;

namespace Configuration.Checks;
public static partial class TypedPathChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestToRootedFilePath", "TestEncodedDynamicPathsPreserveOpaqueIdentity", "TestPathKeyFromCanonicalRejectsURLSuffix", "TestDynamicPathKeyCaseInsensitiveKeyPreservesIdentity", "TestExtensionMutationsPreserveNormalizedInvariant", "TestFileNameStemsPreserveFilenamePrefixes", "TestCompareFileNameStemsKeepsLiteralDotComponentsAndCasingPolicy", "TestSplitAtRootLevelComponentKeepsRoot", "TestToRootedFilePathRequiresRoot", "TestRootedFilePathFromAbsolute", "TestRootedFilePathFromNormalized", "TestTypedPathConstructorsAndDecoders", "TestRelativePath", "TestTypedPathComponents", "TestTryRelativePathBetweenFilePaths", "TestRootedFilePathDirectory", "TestRootedFilePathWithoutRoot", "TestRootedFilePathRootAndRelativePath", "TestCommonDirectoryOfFiles", "TestRelativePathsFromTypedPaths", "TestContainsFilePath", "TestSourceMapLocation", "TestRootedPath", "TestRootedPathCompare", "TestPathOperationsSeparateRooting", "TestRootedFilePathExtensionOperationsPreserveInvariants", "TestForEachAncestorDirectoryPath", "TestRootedFilePathComponents", "TestCaseSensitivityKey", "TestPathKeyConstructionMethods", "TestRootedDirectoryPathResolutionMatchesGeneralRooting", "TestUntitledPathHandling", "TestUntitledPathEdgeCases", ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestToRootedFilePath":
                TestToRootedFilePath();
                break;
            case "TestEncodedDynamicPathsPreserveOpaqueIdentity":
                TestEncodedDynamicPathsPreserveOpaqueIdentity();
                break;
            case "TestPathKeyFromCanonicalRejectsURLSuffix":
                TestPathKeyFromCanonicalRejectsURLSuffix();
                break;
            case "TestDynamicPathKeyCaseInsensitiveKeyPreservesIdentity":
                TestDynamicPathKeyCaseInsensitiveKeyPreservesIdentity();
                break;
            case "TestExtensionMutationsPreserveNormalizedInvariant":
                TestExtensionMutationsPreserveNormalizedInvariant();
                break;
            case "TestFileNameStemsPreserveFilenamePrefixes":
                TestFileNameStemsPreserveFilenamePrefixes();
                break;
            case "TestCompareFileNameStemsKeepsLiteralDotComponentsAndCasingPolicy":
                TestCompareFileNameStemsKeepsLiteralDotComponentsAndCasingPolicy();
                break;
            case "TestSplitAtRootLevelComponentKeepsRoot":
                TestSplitAtRootLevelComponentKeepsRoot();
                break;
            case "TestToRootedFilePathRequiresRoot":
                TestToRootedFilePathRequiresRoot();
                break;
            case "TestRootedFilePathFromAbsolute":
                TestRootedFilePathFromAbsolute();
                break;
            case "TestRootedFilePathFromNormalized":
                TestRootedFilePathFromNormalized();
                break;
            case "TestTypedPathConstructorsAndDecoders":
                TestTypedPathConstructorsAndDecoders();
                break;
            case "TestRelativePath":
                TestRelativePath();
                break;
            case "TestTypedPathComponents":
                TestTypedPathComponents();
                break;
            case "TestTryRelativePathBetweenFilePaths":
                TestTryRelativePathBetweenFilePaths();
                break;
            case "TestRootedFilePathDirectory":
                TestRootedFilePathDirectory();
                break;
            case "TestRootedFilePathWithoutRoot":
                TestRootedFilePathWithoutRoot();
                break;
            case "TestRootedFilePathRootAndRelativePath":
                TestRootedFilePathRootAndRelativePath();
                break;
            case "TestCommonDirectoryOfFiles":
                TestCommonDirectoryOfFiles();
                break;
            case "TestRelativePathsFromTypedPaths":
                TestRelativePathsFromTypedPaths();
                break;
            case "TestContainsFilePath":
                TestContainsFilePath();
                break;
            case "TestSourceMapLocation":
                TestSourceMapLocation();
                break;
            case "TestRootedPath":
                TestRootedPath();
                break;
            case "TestRootedPathCompare":
                TestRootedPathCompare();
                break;
            case "TestPathOperationsSeparateRooting":
                TestPathOperationsSeparateRooting();
                break;
            case "TestRootedFilePathExtensionOperationsPreserveInvariants":
                TestRootedFilePathExtensionOperationsPreserveInvariants();
                break;
            case "TestForEachAncestorDirectoryPath":
                TestForEachAncestorDirectoryPath();
                break;
            case "TestRootedFilePathComponents":
                TestRootedFilePathComponents();
                break;
            case "TestCaseSensitivityKey":
                TestCaseSensitivityKey();
                break;
            case "TestPathKeyConstructionMethods":
                TestPathKeyConstructionMethods();
                break;
            case "TestRootedDirectoryPathResolutionMatchesGeneralRooting":
                TestRootedDirectoryPathResolutionMatchesGeneralRooting();
                break;
            case "TestUntitledPathHandling":
                TestUntitledPathHandling();
                break;
            case "TestUntitledPathEdgeCases":
                TestUntitledPathEdgeCases();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static readonly CaseSensitivity S = CaseSensitivity.CaseSensitive, I = CaseSensitivity.CaseInsensitive;
    private static string B(string value) => GoString.ToUtf8(value);
    private static RootedFilePath F(string value) => RootedFilePath.FromGoString(B(value));
    private static RootedDirectoryPath D(string value) => RootedDirectoryPath.FromGoString(B(value));
    private static RootedPath P(string value) => RootedPath.FromGoString(B(value));
    private static PathKey K(string value) => PathKey.FromGoString(B(value));
    private static RelativePath R(string value) => RelativePath.FromGoString(B(value));
    private static ModuleSpecifier M(string value) => ModuleSpecifier.FromGoString(B(value));
    private static FileNameStem Stem(string value) => FileNameStem.FromGoString(B(value));
    private static void E<T>(T actual, T expected) => Check.Equal(actual, expected, "Original typed path assertion");
    private static void A(bool actual) => Check.True(actual, "Original typed path predicate");
    private static void Panic(Action operation)
    {
        bool failed = false;
        try
        {
            operation();
        }
        catch (ArgumentException)
        {
            failed = true;
        }

        A(failed);
    }

    private static void TestToRootedFilePath()
    {
        E(ToRootedFilePath("./src/../src/a.ts", D("/project")).AsString(), "/project/src/a.ts");
        E(ToRootedFilePath("/project/src/", D("/ignored")).AsString(), "/project/src");
        E(ToRootedFilePath("/", D("/ignored")).AsString(), "/");
        E(ToRootedFilePath("file:///project/src/a.ts", D("/ignored")).AsString(), "file:///project/src/a.ts");
        E(ToRootedFilePath("^/untitled/ts-nul-authority/Untitled-1", D("/ignored")).AsString(), "^/untitled/ts-nul-authority/Untitled-1");
        foreach (var row in new[]
        {
            ("c:", "c:/"),
            ("//server", "//server/"),
            ("file://server", "file://server/"),
            ("^/~ts-uri~/custom/ts-nul-authority", "^/~ts-uri~/custom/ts-nul-authority/"),
            ("^/~ts-uri~/custom/authority?query", "^/~ts-uri~/custom/authority?query/")
        }

        )
        {
            E(ToRootedPath(row.Item1, D("/ignored")).AsString(), row.Item2);
            E(ToRootedFilePath(row.Item1, D("/ignored")).AsString(), row.Item2);
            E(ToRootedDirectoryPath(row.Item1, D("/ignored")).AsString(), row.Item2);
        }

        var disk = ToRootedPath("/a://b?x/../y", D("/ignored"));
        E(disk.AsString(), "/a:/y");
        A(TryRootedPathFromNormalized(disk.AsString()).Ok);
        foreach (string input in new[]
        {
            "http://server?query#fragment",
            "http://server?x/../y",
            "file:///c:?query/path"
        }

        )
        {
            Panic(() => ToRootedPath(input, D("/ignored")));
            A(!TryRootedPathFromAbsolute(input).Ok);
            A(!TryRootedPathFromNormalized(input).Ok);
        }

        var directory = RootedDirectoryPathFromNormalized("http://server/base");
        Action[] invalid = [() => ToRootedPath("file.ts?query/..", directory), () => directory.ResolveFile("file.ts?query"), () => directory.ResolveFile("file.ts?query/.."), () => directory.ResolveRelativeFile(R("file.ts?query")), () => directory.ResolveFileFromNormalizedRelative("file.ts?query"), () => F("http://server/file.ts").AppendSuffix("?query"), () => K("http://server/file.ts").AppendCanonicalSuffix("#fragment"), () => K("http://server/base").AppendCanonicalComponent("file.ts?query"), () => F("http://server/file.ts").ChangeExtension(".js?query"), () => F("http://server/file.ts").ChangeFullExtension(".js#fragment"), () => F("http://server/file.ts").ChangeAnyExtension(".js?query", [".ts"], S)];
        foreach (var op in invalid)
            Panic(op);
        E(directory.ResolveFile("/disk/file?name.ts"), F("/disk/file?name.ts"));
        E(directory.ResolveDirectory("^/~ts-uri~/custom/authority?query/"), D("^/~ts-uri~/custom/authority?query/"));
    }

    private static void TestEncodedDynamicPathsPreserveOpaqueIdentity()
    {
        const string root = "^/~ts-uri~/custom/authority";
        var upper = RootedFilePathFromNormalized(root + "/Foo.ts");
        var lower = RootedFilePathFromNormalized(root + "/foo.ts");
        E(GetRootLength(root), root.Length);
        E(I.PathKey(P(root)), I.PathKey(P(root + "/")));
        A(I.CompareFilePaths(upper, lower) != 0);
        A(!I.ContainsFilePath(D(root + "/Folder"), F(root + "/folder/file.ts")));
        var(relative, ok) = I.RelativeFilePathFromDirectory(D(root), upper);
        A(ok);
        E(relative, R("Foo.ts"));
        A(!I.RelativePathFromPath(D("^/~ts-uri~/custom/Authority/src"), P("^/~ts-uri~/custom/authority/lib/x.ts")).Ok);
        A(!K("^/~ts-uri~/custom/Authority/src").ContainsPath(K("^/~ts-uri~/custom/authority/src/file.ts")));
    }

    private static void TestPathKeyFromCanonicalRejectsURLSuffix() => A(!TryPathKeyFromCanonical("http://server/?x/../y").Ok);
    private static void TestDynamicPathKeyCaseInsensitiveKeyPreservesIdentity()
    {
        var upper = I.PathKey(F("^/~ts-uri~/custom/ts-nul-authority/Foo.ts").AsPath());
        var lower = I.PathKey(F("^/~ts-uri~/custom/ts-nul-authority/foo.ts").AsPath());
        E(upper.CaseInsensitiveKey(), upper);
        E(lower.CaseInsensitiveKey(), lower);
        A(upper.CaseInsensitiveKey() != lower.CaseInsensitiveKey());
    }

    private static void TestExtensionMutationsPreserveNormalizedInvariant()
    {
        foreach (string file in new[]
        {
            "/project/.ts",
            "/project/..ts"
        }

        )
        {
            Panic(() => F(file).RemoveFileExtension().AppendSuffix(""));
            Panic(() => F(file).RemoveExtension(".ts").AppendSuffix(""));
            Panic(() => F(file).ChangeExtension(""));
            Panic(() => F(file).ChangeAnyExtension("", [".ts"], S));
        }

        Panic(() => F("/project/.d.ts").ChangeFullExtension(""));
        Panic(() => F("/project/...ts").ChangeFullExtension(""));
        Panic(() => F("http://example.com/file.ts").ChangeAnyExtension("", [".com/file.ts"], S));
        foreach (var row in new[]
        {
            ("//node_modules/pkg/index.d.ts", "node_modules"),
            ("http://node_modules/pkg/index.d.ts", "node_modules"),
            ("file:///c:/pkg/index.d.ts", "c:"),
            ("^/~ts-uri~/https/node_modules/file.ts", "node_modules")
        }

        )
        {
            var file = F(row.Item1);
            A(!file.SplitAtComponent(row.Item2).Ok);
            A(!file.SplitAtLastComponent(row.Item2).Ok);
            A(!S.PathKey(file.AsPath()).SplitAtCanonicalComponent(row.Item2).Ok);
        }

        A(!K("").SplitAtCanonicalComponent("node_modules").Ok);
        A(!F("").SplitAtComponent("node_modules").Ok);
    }

    private static void TestFileNameStemsPreserveFilenamePrefixes()
    {
        foreach (var row in stemRows)
        {
            var file = F(row.File);
            var stem = Stem(row.Stem);
            var output = F(row.Output);
            E(file.AsStem().AsString(), file.AsString());
            E(file.AsStem().AppendSuffix(""), file);
            E(file.RemoveFileExtension(), stem);
            if (row.File == "")
            {
                E(stem.AppendSuffix(""), output);
                Panic(() => stem.AppendSuffix(".js"));
            }
            else
            {
                var actual = stem.AppendSuffix(".js");
                E(actual, output);
                E(RootedFilePathFromNormalized(actual.AsString()), actual);
                E(file.RemoveExtension(".ts").AppendSuffix(".ts"), file);
            }
        }

        foreach (string file in new[]
        {
            "/project/.ts",
            "/project/..ts",
            "/project/...ts"
        }

        )
            Panic(() => F(file).RemoveFileExtension().AppendSuffix(""));
        var ordinary = F("/project/file.ts").RemoveFileExtension();
        Panic(() => ordinary.AppendSuffix("/other.js"));
        Panic(() => ordinary.AppendSuffix(@"\other.js"));
        Panic(() => F("http://example.com/.ts").RemoveFileExtension().AppendSuffix("?query"));
        Panic(() => F("/project/file.ts").RemoveExtension(".js"));
        Panic(() => F("/project/file.ts").RemoveExtension("/file.ts"));
    }

    private static void TestCompareFileNameStemsKeepsLiteralDotComponentsAndCasingPolicy()
    {
        foreach (var row in stemCompareRows)
        {
            var a = F(row.First).RemoveFileExtension();
            var b = F(row.Second).RemoveFileExtension();
            E(S.CompareFileNameStems(a, b) == 0, row.Sensitive);
            E(I.CompareFileNameStems(a, b) == 0, row.Insensitive);
        }
    }

    private static void TestSplitAtRootLevelComponentKeepsRoot()
    {
        foreach (string text in new[]
        {
            "/node_modules/pkg/index.d.ts",
            "c:/node_modules/pkg/index.d.ts",
            "file:///node_modules/pkg/index.d.ts"
        }

        )
        {
            var file = F(text);
            var(root, _) = file.RootAndRelativePath();
            foreach (var row in new[]
            {
                file.SplitAtComponent("node_modules"),
                file.SplitAtLastComponent("node_modules")
            }

            )
            {
                A(row.Ok);
                E(row.Before, root);
                E(row.Through, root.ResolveDirectory("node_modules"));
            }

            var key = S.PathKey(file.AsPath()).SplitAtCanonicalComponent("node_modules");
            A(key.Ok);
            E(key.Before, S.PathKey(root.AsPath()));
            E(key.Through, S.PathKey(root.ResolveDirectory("node_modules").AsPath()));
        }
    }

    private static void TestToRootedFilePathRequiresRoot()
    {
        Panic(() => ToRootedFilePath("", D("/project")));
        Panic(() => ToRootedFilePath("src/a.ts", D("")));
    }

    private static void TestRootedFilePathFromAbsolute()
    {
        E(RootedFilePathFromAbsolute(@"C:\project\src\..\a.ts"), F("C:/project/a.ts"));
        Panic(() => RootedFilePathFromAbsolute("src/a.ts"));
        A(!TryRootedFilePathFromAbsolute("src/a.ts").Ok);
        var(path, ok) = TryRootedFilePathFromAbsolute("/project/src/../a.ts");
        A(ok);
        E(path, F("/project/a.ts"));
        foreach (var row in new[]
        {
            ("c:", "c:/"),
            ("//server", "//server/"),
            ("file://server", "file://server/")
        }

        )
            E(RootedFilePathFromAbsolute(row.Item1).AsString(), row.Item2);
    }

    private static void TestRootedFilePathFromNormalized()
    {
        string[] good = ["/project/src/a.ts", "c:/", "//server/", "file://server/"];
        foreach (string file in good)
        {
            E(RootedFilePathFromNormalized(file).AsString(), file);
            var(result, ok) = TryRootedFilePathFromNormalized(file);
            A(ok);
            E(result.AsString(), file);
        }

        foreach (string file in new[]
        {
            "/project/src/../a.ts",
            "src/a.ts",
            "/project/src/",
            @"/project\src\a.ts",
            "/project//src/a.ts",
            "c:",
            "//server",
            "file://server"
        }

        )
            Panic(() => RootedFilePathFromNormalized(file));
        foreach (string file in new[]
        {
            "",
            "/project/src/../a.ts",
            "src/a.ts",
            "/project/src/",
            @"/project\src\a.ts",
            "/project//src/a.ts",
            "c://project/src/a.ts",
            "//server//share/a.ts",
            "file://server//a.ts",
            "c:",
            "//server",
            "file://server"
        }

        )
            A(!TryRootedFilePathFromNormalized(file).Ok);
    }

    private static void TestTypedPathConstructorsAndDecoders()
    {
        E(ToRootedDirectoryPath("./src", D("/project")).AsString(), "/project/src");
        foreach (string file in new[]
        {
            "/project/src/../lib/",
            @"c:\project\src\..\lib\",
            "file:///project/src/../lib/"
        }

        )
            E(RootedDirectoryPathFromAbsolute(file).AsString(), file.StartsWith("c:") ? "c:/project/lib" : file.StartsWith("file:") ? "file:///project/lib" : "/project/lib");
        E(RootedDirectoryPathFromNormalized("/project/src").AsString(), "/project/src");
        E(PathKeyFromCanonical("/project/src").AsString(), "/project/src");
        E(S.PathKey(RootedPathFromAbsolute("/project/src/")), K("/project/src"));
        var(path, ok) = TryPathKeyFromCanonical("/project/src");
        A(ok);
        E(path.AsString(), "/project/src");
        E(ToModuleSpecifier("./src").AsString(), "./src");
        Panic(() => RootedDirectoryPathFromAbsolute("project/src"));
        Panic(() => RootedDirectoryPathFromNormalized("/project/src/"));
        Panic(() => PathKeyFromCanonical("/project/../src"));
        Panic(() => PathKeyFromCanonical("project/src"));
        foreach (string text in new[]
        {
            "/project/../src",
            "project/src",
            "c:",
            "//server",
            "http://server",
            "file:///c:"
        }

        )
            A(!TryPathKeyFromCanonical(text).Ok);
        Panic(() => S.PathKey(ToRootedPath("project/src", D(""))));
    }

    private static void TestRelativePath()
    {
        E(ToRelativePath(@".\src\..\lib\file.ts").AsString(), "lib/file.ts");
        E(RelativePathFromNormalized("../lib/file.ts").AsString(), "../lib/file.ts");
        E(RelativePathFromNormalized("").AsString(), "");
        Panic(() => RelativePathFromNormalized("./lib/file.ts"));
        Panic(() => ToRelativePath("/lib/file.ts"));
        E(R("lib/file.ts").AsModuleSpecifier().AsString(), "./lib/file.ts");
        E(R("../lib/file.ts").AsModuleSpecifier().AsString(), "../lib/file.ts");
        E(F("/project/lib/file.ts").AsModuleSpecifier(), M("/project/lib/file.ts"));
        A(M("./lib/file.ts").IsRelative());
        A(!M("lib").IsRelative());
        A(M("/project/lib").IsAbsolute());
        A(!M("lib").IsAbsolute());
        E(M("pkg").Resolve("./dist", "file.js"), M("pkg/dist/file.js"));
        E(M("pkg").ResolveRelative(R("lib/file.js")), M("pkg/lib/file.js"));
        E(M("pkg").CombineRelative(R("lib/file.js")), M("pkg/lib/file.js"));
        E(M("pkg/lib/file.d.ts").RemoveFileExtension(), M("pkg/lib/file"));
        A(R("../lib/file.ts").IsParentRelative());
        A(!R("..file.ts").IsParentRelative());
        A(R("lib/").HasTrailingDirectorySeparator());
        E(R("lib").WithTrailingDirectorySeparator(), R("lib/"));
        E(D("/project/dist").ResolveRelativeFile(R("../src/file.ts")), F("/project/src/file.ts"));
    }

    private static void TestTypedPathComponents()
    {
        A(F("/project/node_modules/pkg/index.ts").ContainsLowercaseDirectorySequence("/node_modules/"));
        A(!F("/project/node_modules").ContainsLowercaseDirectorySequence("/node_modules/"));
        A(!F("/project/not_node_modules/pkg/index.ts").ContainsLowercaseDirectorySequence("/node_modules/"));
        A(K("/project/node_modules/@types/node/index.d.ts").ContainsLowercaseDirectorySequence("/node_modules/@types/node/"));
        A(!K("/project/node_modules/@types/node").ContainsLowercaseDirectorySequence("/node_modules/@types/node/"));
    }

    private static void TestTryRelativePathBetweenFilePaths()
    {
        var(relative, ok) = S.RelativePathFromDirectory(D("/project/src"), F("/project/lib/file.ts"));
        A(ok);
        E(relative.AsString(), "../lib/file.ts");
        A(!S.RelativePathFromDirectory(D("c:/project/src"), F("d:/project/lib/file.ts")).Ok);
    }

    private static void TestRootedFilePathDirectory()
    {
        foreach (var row in new[]
        {
            ("/project/src/a.ts", "/project/src"),
            ("/", "/"),
            ("c:/project/src/a.ts", "c:/project/src"),
            ("file:///project/src/a.ts", "file:///project/src")
        }

        )
            E(F(row.Item1).Directory().AsString(), row.Item2);
    }

    private static void TestRootedFilePathWithoutRoot()
    {
        foreach (string file in new[]
        {
            "/project/src/a.ts",
            "c:/project/src/a.ts",
            "file:///project/src/a.ts"
        }

        )
            E(F(file).WithoutRoot(), "project/src/a.ts");
    }

    private static void TestRootedFilePathRootAndRelativePath()
    {
        var(root, relative) = F("file:///project/src/a.ts").RootAndRelativePath();
        E(root.AsString(), "file:///");
        E(relative, "project/src/a.ts");
        E(root.ResolveFileFromNormalizedRelative(relative), F("file:///project/src/a.ts"));
        E(D("/").ResolveFileFromNormalizedRelative("C:/src/a.ts"), F("/C:/src/a.ts"));
        Panic(() => root.ResolveFileFromNormalizedRelative(""));
        Panic(() => root.ResolveFileFromNormalizedRelative("../a.ts"));
    }

    private static void TestCommonDirectoryOfFiles()
    {
        RootedFilePath[] files = [F("/Project/src/a.ts"), F("/project/src/nested/b.ts"), F("/project/test/c.ts")];
        E(I.CommonDirectoryOfFiles(files), D("/Project"));
        E(S.CommonDirectoryOfFiles(files), D("/"));
        E(I.CommonDirectoryOfFiles([F("/repo/İproject/a.ts"), F("/repo/iproject/b.ts")]), D("/repo"));
    }

    private static void TestRelativePathsFromTypedPaths()
    {
        var dir = D("/project/src");
        var file = F("/project/src/index.ts");
        var target = F("/project/lib/util.ts");
        var(relative, ok) = S.RelativePathFromDirectory(dir, target);
        A(ok);
        E(relative.AsString(), "../lib/util.ts");
        (relative, ok) = S.RelativePathFromFile(file, target);
        A(ok);
        E(relative.AsString(), "../lib/util.ts");
        (relative, ok) = I.RelativePathFromDirectory(D("/PROJECT/src"), target);
        A(ok);
        E(relative.AsString(), "../lib/util.ts");
        (relative, ok) = I.RelativeFilePathFromDirectory(D("/repo/K"), F("/repo/k/a.ts"));
        A(ok);
        E(relative.AsString(), "a.ts");
    }

    private static void TestContainsFilePath()
    {
        foreach (var row in containsRows)
        {
            var sensitivity = row.Insensitive ? I : S;
            var directory = D(row.Directory);
            var file = F(row.File);
            E(sensitivity.ContainsFilePath(directory, file), row.Contained);
            E(sensitivity.ContainsPath(directory, file.AsPath()), row.Contained);
            E(sensitivity.StartsWithDirectory(file, directory), row.Contained && row.Relative != "");
            var(relative, ok) = sensitivity.RelativeFilePathFromDirectory(directory, file);
            E(ok, row.Contained);
            if (ok)
                E(relative.AsString(), row.Relative);
        }

        A(S.PathKey(D("C:/project").AsPath()).ContainsPath(S.PathKey(F("c:/project/a.ts").AsPath())));
        var(r, o) = F("c:/project/a.ts").RelativeTo(D("C:/project"));
        A(o);
        E(r, R("a.ts"));
    }

    private static void TestSourceMapLocation()
    {
        var relative = ToSourceMapLocation(@"maps\generated");
        E(relative.AsString(), "maps/generated");
        A(relative.IsRelative());
        E(relative.ResolveDirectory(D("/project/src"), D("/project")).AsString(), "/project/src/maps/generated");
        var rooted = ToSourceMapLocation(@"/maps\generated");
        A(!rooted.IsRelative());
        E(rooted.ResolveDirectory(D("/project/src"), D("/project")).AsString(), "/maps/generated");
        // Explicit selected text-unmarshal adapter: source callback is ToSourceMapLocation.
        var(value, error) = JsonCodec.Parse(B("\"maps\\\\generated\""));
        E(error, null);
        var decoded = ToSourceMapLocation((string)value!);
        E(decoded.AsString(), "maps/generated");
        E(JsonCodec.MarshalString(decoded.AsString()), "\"maps/generated\"");
    }

    private static void TestRootedPath()
    {
        var path = ToRootedPath(@"src\config.json", D("/project"));
        E(path.AsString(), "/project/src/config.json");
        E(RootedFilePathFromPath(path), F("/project/src/config.json"));
        E(RootedDirectoryPathFromPath(path), D("/project/src/config.json"));
        E(D("/project/src").AsPath(), P("/project/src"));
    }

    private static void TestRootedPathCompare()
    {
        E(P("/a").Compare(P("/b")), -1);
        E(F("/a").Compare(F("/a")), 0);
        E(D("/b").Compare(D("/a")), 1);
    }

    private static void TestPathOperationsSeparateRooting()
    {
        // Selected preexisting relative helper assertion retained. Free-string
        // ComparePaths/ContainsPath/relative-resolution APIs are not selected.
        E(GetRelativePathFromDirectory("src", "lib/a.ts"), "../lib/a.ts");
    }

    private static void TestRootedFilePathExtensionOperationsPreserveInvariants()
    {
        var file = F("/project/src/file.ts");
        E(file.RemoveFileExtension(), Stem("/project/src/file"));
        E(file.ChangeExtension(".js"), F("/project/src/file.js"));
        E(F("/project/src/file.d.ts").ChangeFullExtension(""), F("/project/src/file"));
        Panic(() => file.ChangeExtension("../other"));
        Panic(() => file.ChangeFullExtension(@"\nested"));
        Panic(() => file.RemoveExtension(".js"));
    }

    private static void TestForEachAncestorDirectoryPath()
    {
        var values = new List<RootedDirectoryPath>();
        D("/project/src/lib").ForEachAncestorDirectory(directory =>
        {
            values.Add(directory);
            return (0, false);
        });
        Check.Sequence(values, new[] { D("/project/src/lib"), D("/project/src"), D("/project"), D("/") }, "Original ancestors");
    }

    private static void TestRootedFilePathComponents()
    {
        var file = F("/store/node_modules/pkg/node_modules/dep/index.d.ts");
        E(file.DirectoryBefore(23), D("/store/node_modules/pkg"));
        E(file.SuffixAfterSeparator(23), "node_modules/dep/index.d.ts");
        var(relative, ok) = file.RelativeTo(D("/store/node_modules/pkg"));
        A(ok);
        E(relative.AsString(), "node_modules/dep/index.d.ts");
        A(!file.RelativeTo(D("/other")).Ok);
        var first = file.SplitAtComponent("node_modules");
        A(first.Ok);
        E(first.Before, D("/store"));
        E(first.Through, D("/store/node_modules"));
        var last = file.SplitAtLastComponent("node_modules");
        A(last.Ok);
        E(last.Before, D("/store/node_modules/pkg"));
        E(last.Through, D("/store/node_modules/pkg/node_modules"));
        Panic(() => file.DirectoryBefore(22));
        Panic(() => file.SuffixAfterSeparator(22));
    }

    private static void TestCaseSensitivityKey()
    {
        var file = F("/Project/SRC/a.ts");
        E(S.PathKey(file.AsPath()).AsString(), "/Project/SRC/a.ts");
        E(I.PathKey(file.AsPath()).AsString(), "/project/src/a.ts");
        E(S.CompareFilePaths(file, F("/Project/SRC/b.ts")), -1);
        E(I.CompareFilePaths(file, F("/project/src/A.ts")), 0);
        E(file.DirectorySeparatorCount(), 3);
    }

    private static void TestPathKeyConstructionMethods()
    {
        var path = K("/project/src");
        E(path.AppendCanonicalComponent("node_modules").AsString(), "/project/src/node_modules");
        E(path.AppendCanonicalSuffix(".0.ts").AsString(), "/project/src.0.ts");
        E(path.AppendCanonicalSuffix(".ts").Extension(), ".ts");
        var row = K("/project/node_modules/pkg/index.d.ts").SplitAtCanonicalComponent("node_modules");
        A(row.Ok);
        E(row.Before, K("/project"));
        E(row.Through, K("/project/node_modules"));
        A(!K("/project/not_node_modules/pkg").SplitAtCanonicalComponent("node_modules").Ok);
        Action[] invalid = [() => path.AppendCanonicalComponent("../src"), () => path.AppendCanonicalSuffix("/src"), () => K("").AppendCanonicalComponent("src"), () => K("").AppendCanonicalSuffix(".ts"), () => path.SplitAtCanonicalComponent("../node_modules"), () => F("").AppendSuffix(".ts"), () => D("").ResolveFile("file.ts"), () => D("").ResolveDirectory("types")];
        foreach (var op in invalid)
            Panic(op);
        E(D("/project/src").ResolveDirectory("types").AsString(), "/project/src/types");
        E(D("/project/src").ResolveDirectory("types/").AsString(), "/project/src/types");
        E(D("/project/src").ResolveDirectory("").AsString(), "/project/src");
        E(D("/project/src").ResolveFile("file.ts/").AsString(), "/project/src/file.ts");
        E(D("/project/src").ResolveFile("").AsString(), "/project/src");
    }

    private static void TestRootedDirectoryPathResolutionMatchesGeneralRooting()
    {
        var directory = D("/project/src");
        foreach (string path in new[]
        {
            "file.ts",
            "nested/file.ts",
            "./file.ts",
            "../file.ts",
            @"nested\file.ts",
            "nested/file.ts/",
            "/absolute/file.ts",
            "c:/absolute/file.ts",
            "file:///absolute/file.ts"
        }

        )
        {
            E(directory.ResolveFile(path), ToRootedFilePath(path, directory));
            E(directory.ResolveDirectory(path), ToRootedDirectoryPath(path, directory));
        }
    }

    private static void TestUntitledPathHandling()
    {
        const string path = "^/untitled/ts-nul-authority/Untitled-2";
        E(GetEncodedRootLength(path), 2);
        A(IsRootedDiskPath(path));
        E(S.PathKey(ToRootedPath(path, D("/home/user/project"))).AsString(), path);
        E(GetNormalizedAbsolutePath(path, "/home/user/project"), path);
    }

    private static void TestUntitledPathEdgeCases()
    {
        foreach (var row in new[]
        {
            ("^/", 2, true),
            ("^/untitled/ts-nul-authority/test", 2, true),
            ("^", 0, false),
            ("^x", 0, false),
            ("^^/", 0, false),
            ("x^/", 0, false),
            ("^/untitled/ts-nul-authority/path/with/deeper/structure", 2, true)
        }

        )
        {
            E(GetEncodedRootLength(row.Item1), row.Item2);
            E(IsRootedDiskPath(row.Item1), row.Item3);
        }
    }
}
