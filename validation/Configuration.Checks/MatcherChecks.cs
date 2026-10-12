// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// All twenty pinned vfsmatch_test.go functions. Literal table predicates are
// source-extracted; remaining bodies below preserve every original subtest/assertion.
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.Vfs.VfsMatch;
using M = TypeScript.Vfs.VfsMatch.VfsMatch;

namespace Configuration.Checks;
public static partial class MatcherChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestReadDirectory", "TestIsImplicitGlob", "TestReadDirectoryEdgeCases", "TestReadDirectoryEmptyIncludes", "TestReadDirectorySymlinkCycle", "TestReadDirectoryMatchesTypeScriptBaselines", "TestReadDirectoryWithExtendedDynamicRoot", "TestReadDirectoryDynamicRootUsesCaseSensitivePatterns", "TestDynamicAbsoluteGlobUsesCaseSensitivePattern", "TestSpecMatcher", "TestSpecMatcher_MatchString", "TestSingleSpecMatcher_MatchString", "TestSpecMatchers_MatchIndex", "TestSingleSpecMatcher", "TestSpecMatchers", "TestGlobPatternInternals", "TestMatchSegmentsEdgeCases", "TestReadDirectoryConsecutiveSlashes", "TestGlobPatternLiteralWithPackageFolders", "TestGetBasePathsCaseSensitivity"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestReadDirectory":
                TestReadDirectory();
                break;
            case "TestIsImplicitGlob":
                TestIsImplicitGlob();
                break;
            case "TestReadDirectoryEdgeCases":
                TestReadDirectoryEdgeCases();
                break;
            case "TestReadDirectoryEmptyIncludes":
                TestReadDirectoryEmptyIncludes();
                break;
            case "TestReadDirectorySymlinkCycle":
                TestReadDirectorySymlinkCycle();
                break;
            case "TestReadDirectoryMatchesTypeScriptBaselines":
                TestReadDirectoryMatchesTypeScriptBaselines();
                break;
            case "TestReadDirectoryWithExtendedDynamicRoot":
                TestReadDirectoryWithExtendedDynamicRoot();
                break;
            case "TestReadDirectoryDynamicRootUsesCaseSensitivePatterns":
                TestReadDirectoryDynamicRootUsesCaseSensitivePatterns();
                break;
            case "TestDynamicAbsoluteGlobUsesCaseSensitivePattern":
                TestDynamicAbsoluteGlobUsesCaseSensitivePattern();
                break;
            case "TestSpecMatcher":
                TestSpecMatcher();
                break;
            case "TestSpecMatcher_MatchString":
                TestSpecMatcher_MatchString();
                break;
            case "TestSingleSpecMatcher_MatchString":
                TestSingleSpecMatcher_MatchString();
                break;
            case "TestSpecMatchers_MatchIndex":
                TestSpecMatchers_MatchIndex();
                break;
            case "TestSingleSpecMatcher":
                TestSingleSpecMatcher();
                break;
            case "TestSpecMatchers":
                TestSpecMatchers();
                break;
            case "TestGlobPatternInternals":
                TestGlobPatternInternals();
                break;
            case "TestMatchSegmentsEdgeCases":
                TestMatchSegmentsEdgeCases();
                break;
            case "TestReadDirectoryConsecutiveSlashes":
                TestReadDirectoryConsecutiveSlashes();
                break;
            case "TestGlobPatternLiteralWithPackageFolders":
                TestGlobPatternLiteralWithPackageFolders();
                break;
            case "TestGetBasePathsCaseSensitivity":
                TestGetBasePathsCaseSensitivity();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original matcher test.");
        }
    }

    private static bool Contains(IReadOnlyList<string>? values, string value) => values?.Contains(value) == true;
    private static RootedDirectoryPath D(string path) => RootedDirectoryPath.FromGoString(path);
    private static List<string>? Read(IFS host, string root, List<string>? extensions, List<string>? excludes, List<string>? includes) => GoSlices.ToListOrNull(M.ReadDirectory(host, D(root), extensions, excludes, includes, M.UnlimitedDepth))?.Select(path => path.AsString()).ToList();
    private static void TestReadDirectoryWithExtendedDynamicRoot()
    {
        const string packageDirectory = "^/~ts-uri~/custom/ts-nul-authority/node_modules/Pkg";
        var host = new ConfigurationFileSystem([new(packageDirectory + "/value.d.ts"), new(packageDirectory + "/node_modules/dep/index.d.ts")]);
        var got = Read(host, packageDirectory, [".d.ts"], null, ["**/*"]);
        Check.Sequence(got, new[] { packageDirectory + "/value.d.ts" }, "extended dynamic root");
    }

    private static void TestReadDirectoryDynamicRootUsesCaseSensitivePatterns()
    {
        const string root = "^/~ts-uri~/custom/ts-nul-authority";
        // Original wrapper changes only CaseSensitivity; the backing map remains
        // case-sensitive and therefore retains BOTH differently cased directories.
        IFS host = new ReadFileSystemOverrides(new ConfigurationFileSystem([new(root + "/Foo/a.ts"), new(root + "/foo/b.ts")]))
        {
            Sensitivity = () => CaseSensitivity.CaseInsensitive
        };
        var got = M.ReadDirectory(host, TsPath.RootedDirectoryPathFromNormalized(root + "/"), [".ts"], (List<string>? )null, ["Foo/**/*.ts"], M.UnlimitedDepth);
        Check.Sequence(GoSlices.ToListOrNull(got)?.Select(path => path.AsString()).ToList(), new[] { root + "/Foo/a.ts" }, "dynamic sensitivity");
    }

    private static void TestDynamicAbsoluteGlobUsesCaseSensitivePattern()
    {
        const string root = "^/~ts-uri~/custom/ts-nul-authority";
        var matcher = M.NewSpecMatcher((List<string>? )[root + "/Foo/**/*.ts"], D("/dev"), Usage.Files, CaseSensitivity.CaseInsensitive);
        Check.True(matcher is not null, "dynamic matcher nonnil");
        Check.True(matcher!.MatchString(root + "/Foo/a.ts"), "dynamic exact case");
        Check.True(!matcher.MatchString(root + "/foo/a.ts"), "dynamic changed case");
    }

    private static (ByteStringValue Part, int NextOffset, bool Ok) NextPathPartParts(string prefix, string suffix, int offset) => M.nextPathPartParts(ByteStringValue.FromString(prefix), ByteStringValue.FromString(suffix), offset);

    private static void AssertPart(string prefix, string suffix, int offset, string? part = null, int? next = null, bool ok = true)
    {
        var result = NextPathPartParts(prefix, suffix, offset);
        Check.Equal(result.Ok, ok, "nextPathPartParts status");
        if (part is not null)
            Check.Equal(result.Part.Materialize(), part, "nextPathPartParts part");
        if (next is not null)
            Check.Equal(result.NextOffset, next.Value, "nextPathPartParts offset");
    }

    private static void TestGlobPatternInternals()
    {
        // Each source call's exact asserted fields are retained; offsets not
        // asserted by upstream are only carried to the next source call.
        const string path = "/dev//foo///bar";
        AssertPart(path, "", 0, "", 1);
        var result = NextPathPartParts(path, "", 1);
        Check.True(result.Ok, "consecutive slashes dev");
        Check.Equal(result.Part.Materialize(), "dev", "dev");
        result = NextPathPartParts(path, "", result.NextOffset);
        Check.True(result.Ok, "consecutive slashes foo");
        Check.Equal(result.Part.Materialize(), "foo", "foo");
        result = NextPathPartParts(path, "", result.NextOffset);
        Check.True(result.Ok, "consecutive slashes bar");
        Check.Equal(result.Part.Materialize(), "bar", "bar");
        result = NextPathPartParts("/dev/", "", 0);
        Check.True(result.Ok, "trailing slash root");
        result = NextPathPartParts("/dev/", "", result.NextOffset);
        Check.True(result.Ok, "trailing slash dev");
        result = NextPathPartParts("/dev/", "", result.NextOffset);
        Check.True(!result.Ok, "trailing slash end");
        AssertPart("", "/dev//foo", 0, "", 1);
        result = NextPathPartParts("", "/dev//foo", 1);
        Check.True(result.Ok, "empty prefix dev");
        Check.Equal(result.Part.Materialize(), "dev", "dev");
        result = NextPathPartParts("", "/dev//foo", result.NextOffset);
        Check.True(result.Ok, "empty prefix foo");
        Check.Equal(result.Part.Materialize(), "foo", "foo");
        result = NextPathPartParts("/dev/", "foo", 0);
        Check.True(result.Ok, "split root");
        result = NextPathPartParts("/dev/", "foo", result.NextOffset);
        Check.True(result.Ok, "split dev");
        Check.Equal(result.Part.Materialize(), "dev", "dev");
        result = NextPathPartParts("/dev/", "foo", result.NextOffset);
        Check.True(result.Ok, "split foo");
        Check.Equal(result.Part.Materialize(), "foo", "foo");
        Check.Equal(result.NextOffset, "/dev/".Length + "foo".Length, "split total length");
        AssertPart("/dev/", "foo", result.NextOffset, ok: false);
        AssertPart("/", "a", 0, "", 1);
        AssertPart("/", "a", 1, "a");
        AssertPattern("a?", [("/ab", true), ("/a", false)], "question at end");
        AssertPattern("a*b*c", [("/abc", true), ("/aXbYc", true), ("/aXXXbYYYc", true), ("/aXbY", false)], "complex star");
        var host = new ConfigurationFileSystem([new("/dev/node_modules/pkg/index.ts")], CaseSensitivity.CaseInsensitive);
        Check.True(Contains(Read(host, "/dev", [".ts"], null, ["node_modules/pkg/index.ts"]), "/dev/node_modules/pkg/index.ts"), "explicit package literal");
    }

    private static void AssertPattern(string spec, (string Path, bool Expected)[] rows, string context)
    {
        // Source literals are ordinary Unicode in this explicit body; convert at
        // the input boundary, matching Go compiler string-literal UTF8 bytes.
        var(pattern, ok) = M.compileGlobPattern(ByteStringValue.FromString(GoString.ToUtf8(spec)), D("/"), Usage.Files, CaseSensitivity.CaseSensitive);
        Check.True(ok, context + " compile");
        foreach (var row in rows)
            Check.Equal(pattern!.Value.matches(ByteStringValue.FromString(GoString.ToUtf8(row.Path))), row.Expected, context + row.Path);
    }

    private static void TestMatchSegmentsEdgeCases()
    {
        AssertPattern("a?b", [("/aXb", true), ("/ab", false), ("/aXYb", false)], "question slash");
        AssertPattern("a*", [("/a", true), ("/abc", true), ("/aXYZ", true)], "star end");
        AssertPattern("*a*", [("/a", true), ("/Xa", true), ("/aX", true), ("/XaY", true), ("/XYZ", false)], "multiple stars");
        AssertPattern("*a*a", [("/aa", true), ("/Xaa", true), ("/aXa", true), ("/XaYa", true), ("/aaaa", true), ("/a", false), ("/Xa", false), ("/aX", false), ("/XaYaZ", false)], "backtrack two a");
        AssertPattern("*a*b*c", [("/abc", true), ("/XaYbZc", true), ("/aXbYc", true), ("/aaabbbccc", true), ("/ab", false), ("/ac", false), ("/cba", false), ("/abcX", false)], "backtrack abc");
        AssertPattern("*a*a*a", [("/aaa", true), ("/aXaYa", true), ("/XaYaZa", true), ("/aa", false), ("/aaX", false)], "backtrack three a");
        AssertPattern("a*b*a", [("/aba", true), ("/aXbYa", true), ("/abba", true), ("/ab", false), ("/aba ", false), ("/Xaba", false)], "backtrack aba");
        AssertPattern("*a*a*a*a*b", [("/aaaaaaaaaaaaaaaa", false), ("/aaaaaaaaaaaaaaaaX", false), ("/aaaab", true), ("/XaYaZaWab", true)], "pathological pattern");
        AssertPattern("abcdefgh.ts", [("/abc.ts", false), ("/abcdefgh.ts", true)], "literal length");
        AssertPattern("?.ts", [("/a.ts", true), ("/é.ts", true), ("/中.ts", true), ("/🎉.ts", true), ("/.ts", false), ("/ab.ts", false)], "one rune");
        AssertPattern("??.ts", [("/ab.ts", true), ("/é中.ts", true), ("/🎉é.ts", true), ("/a.ts", false), ("/abc.ts", false)], "two runes");
        AssertPattern("*é.ts", [("/é.ts", true), ("/café.ts", true), ("/cafe.ts", false)], "star multibyte");
        AssertPattern("*🎉*", [("/🎉", true), ("/a🎉b", true), ("/abc", false)], "star emoji");
    }

    private static void TestReadDirectoryConsecutiveSlashes()
    {
        var host = new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/x/b.ts")], CaseSensitivity.CaseInsensitive);
        var got = Read(host, "/dev", [".ts"], null, ["**/*.ts"]);
        Check.True((got?.Count ?? 0) >= 2, "should find files");
        Check.True(Contains(got, "/dev/a.ts"), "a.ts");
        Check.True(Contains(got, "/dev/x/b.ts"), "x/b.ts");
    }

    private static void TestGlobPatternLiteralWithPackageFolders()
    {
        var host = new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/node_modules/b.ts")], CaseSensitivity.CaseInsensitive);
        Check.True(!Contains(Read(host, "/dev", [".ts"], null, ["*/*.ts"]), "/dev/node_modules/b.ts"), "wildcard skips package");
        host = new ConfigurationFileSystem([new("/dev/node_modules/b.ts")], CaseSensitivity.CaseInsensitive);
        Check.True(Contains(Read(host, "/dev", [".ts"], null, ["node_modules/b.ts"]), "/dev/node_modules/b.ts"), "explicit package folder");
    }

    private static void TestGetBasePathsCaseSensitivity()
    {
        var paths = M.getBasePaths<string>(D("/root"), ["../Other/**/*.ts", "../other/**/*.ts"], CaseSensitivity.CaseSensitive, static text => ByteStringValue.FromString(text)).AsSpan().ToArray().Select(path => path.AsString()).ToList();
        Check.True(paths.Contains("/Other"), "case-sensitive /Other");
        Check.True(paths.Contains("/other"), "case-sensitive /other");
        paths = M.getBasePaths<string>(D("/root"), ["../Other/**/*.ts", "../other/**/*.ts"], CaseSensitivity.CaseInsensitive, static text => ByteStringValue.FromString(text)).AsSpan().ToArray().Select(path => path.AsString()).ToList();
        int count = 0;
        foreach (string path in paths)
            if (path is "/Other" or "/other")
                count++;
        Check.True(count <= 1, "case-insensitive count <=1");
    }
}

// Local adaptation of original wrapvfs.Replacements: forwards all unchanged reads.
internal sealed class ReadFileSystemOverrides(IFS basis) : IFS
{
    internal Func<CaseSensitivity>? Sensitivity = null;
    internal Func<RootedDirectoryPath, Entries>? Entries = null;
    internal Func<RootedPath, RootedPath>? Resolve = null;
    internal Func<RootedPath, TypeScript.Vfs.FileInfo?>? Info = null;
    public CaseSensitivity CaseSensitivity() => Sensitivity?.Invoke() ?? basis.CaseSensitivity();
    public bool FileExists(RootedFilePath path) => basis.FileExists(path);
    public bool DirectoryExists(RootedDirectoryPath path) => basis.DirectoryExists(path);
    public Exception? WriteFile(RootedFilePath path, string data) => basis.WriteFile(path, data);
    public (string Contents, bool Ok) ReadFile(RootedFilePath path) => basis.ReadFile(path);
    public Entries GetAccessibleEntries(RootedDirectoryPath path) => Entries?.Invoke(path) ?? basis.GetAccessibleEntries(path);
    public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => Info is null ? basis.Stat(path) : Info(path);
    public RootedPath Realpath(RootedPath path) => Resolve?.Invoke(path) ?? basis.Realpath(path);
}
