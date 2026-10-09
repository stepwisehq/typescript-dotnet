// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving translation of pinned internal/tspath/path_test.go literals.
// Generated from original assertion bodies; not generated from C# output.
using TypeScript.Scanner;

namespace TypeScript.Projects;
public static partial class ProjectSupportChecks
{
    private static void TestGetDirectoryPath()
    {
        Equal(TsPath.GetDirectoryPath(""), "");
        Equal(TsPath.GetDirectoryPath("a"), "");
        Equal(TsPath.GetDirectoryPath("a/b"), "a");
        Equal(TsPath.GetDirectoryPath("/"), "/");
        Equal(TsPath.GetDirectoryPath("/a"), "/");
        Equal(TsPath.GetDirectoryPath("/a/"), "/");
        Equal(TsPath.GetDirectoryPath("/a/b"), "/a");
        Equal(TsPath.GetDirectoryPath("/a/b/"), "/a");
        Equal(TsPath.GetDirectoryPath("c:"), "c:");
        Equal(TsPath.GetDirectoryPath("c:d"), "");
        Equal(TsPath.GetDirectoryPath("c:/"), "c:/");
        Equal(TsPath.GetDirectoryPath("c:/path"), "c:/");
        Equal(TsPath.GetDirectoryPath("c:/path/"), "c:/");
        Equal(TsPath.GetDirectoryPath("//server"), "//server");
        Equal(TsPath.GetDirectoryPath("//server/"), "//server/");
        Equal(TsPath.GetDirectoryPath("//server/share"), "//server/");
        Equal(TsPath.GetDirectoryPath("//server/share/"), "//server/");
        Equal(TsPath.GetDirectoryPath("\\\\server"), "//server");
        Equal(TsPath.GetDirectoryPath("\\\\server\\"), "//server/");
        Equal(TsPath.GetDirectoryPath("\\\\server\\share"), "//server/");
        Equal(TsPath.GetDirectoryPath("\\\\server\\share\\"), "//server/");
        Equal(TsPath.GetDirectoryPath("file:///"), "file:///");
        Equal(TsPath.GetDirectoryPath("file:///path"), "file:///");
        Equal(TsPath.GetDirectoryPath("file:///path/"), "file:///");
        Equal(TsPath.GetDirectoryPath("file:///c:"), "file:///c:");
        Equal(TsPath.GetDirectoryPath("file:///c:d"), "file:///");
        Equal(TsPath.GetDirectoryPath("file:///c:/"), "file:///c:/");
        Equal(TsPath.GetDirectoryPath("file:///c:/path"), "file:///c:/");
        Equal(TsPath.GetDirectoryPath("file:///c:/path/"), "file:///c:/");
        Equal(TsPath.GetDirectoryPath("file://server"), "file://server");
        Equal(TsPath.GetDirectoryPath("file://server/"), "file://server/");
        Equal(TsPath.GetDirectoryPath("file://server/path"), "file://server/");
        Equal(TsPath.GetDirectoryPath("file://server/path/"), "file://server/");
        Equal(TsPath.GetDirectoryPath("http://server"), "http://server");
        Equal(TsPath.GetDirectoryPath("http://server/"), "http://server/");
        Equal(TsPath.GetDirectoryPath("http://server/path"), "http://server/");
        Equal(TsPath.GetDirectoryPath("http://server/path/"), "http://server/");
    }

    private static void TestGetPathComponents()
    {
        Sequence(TsPath.GetPathComponents(""), new List<string> { "" });
        Sequence(TsPath.GetPathComponents("a"), new List<string> { "", "a" });
        Sequence(TsPath.GetPathComponents("./a"), new List<string> { "", ".", "a" });
        Sequence(TsPath.GetPathComponents("/"), new List<string> { "/" });
        Sequence(TsPath.GetPathComponents("/a"), new List<string> { "/", "a" });
        Sequence(TsPath.GetPathComponents("/a/"), new List<string> { "/", "a" });
        Sequence(TsPath.GetPathComponents("c:"), new List<string> { "c:" });
        Sequence(TsPath.GetPathComponents("c:d"), new List<string> { "", "c:d" });
        Sequence(TsPath.GetPathComponents("c:/"), new List<string> { "c:/" });
        Sequence(TsPath.GetPathComponents("c:/path"), new List<string> { "c:/", "path" });
        Sequence(TsPath.GetPathComponents("//server"), new List<string> { "//server" });
        Sequence(TsPath.GetPathComponents("//server/"), new List<string> { "//server/" });
        Sequence(TsPath.GetPathComponents("//server/share"), new List<string> { "//server/", "share" });
        Sequence(TsPath.GetPathComponents("file:///"), new List<string> { "file:///" });
        Sequence(TsPath.GetPathComponents("file:///path"), new List<string> { "file:///", "path" });
        Sequence(TsPath.GetPathComponents("file:///c:"), new List<string> { "file:///c:" });
        Sequence(TsPath.GetPathComponents("file:///c:d"), new List<string> { "file:///", "c:d" });
        Sequence(TsPath.GetPathComponents("file:///c:/"), new List<string> { "file:///c:/" });
        Sequence(TsPath.GetPathComponents("file:///c:/path"), new List<string> { "file:///c:/", "path" });
        Sequence(TsPath.GetPathComponents("file://server"), new List<string> { "file://server" });
        Sequence(TsPath.GetPathComponents("file://server/"), new List<string> { "file://server/" });
        Sequence(TsPath.GetPathComponents("file://server/path"), new List<string> { "file://server/", "path" });
        Sequence(TsPath.GetPathComponents("http://server"), new List<string> { "http://server" });
        Sequence(TsPath.GetPathComponents("http://server/"), new List<string> { "http://server/" });
        Sequence(TsPath.GetPathComponents("http://server/path"), new List<string> { "http://server/", "path" });
    }

    private static void TestReducePathComponents()
    {
        Sequence(TsPath.ReducePathComponents(new List<string> { "" }), new List<string> { "" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", "." }), new List<string> { "" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", ".", "a" }), new List<string> { "", "a" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", "a", "." }), new List<string> { "", "a" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", ".." }), new List<string> { "", ".." });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", "..", ".." }), new List<string> { "", "..", ".." });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", "..", ".", ".." }), new List<string> { "", "..", ".." });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", "a", ".." }), new List<string> { "" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "", "..", "a" }), new List<string> { "", "..", "a" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "/" }), new List<string> { "/" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "/", "." }), new List<string> { "/" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "/", ".." }), new List<string> { "/" });
        Sequence(TsPath.ReducePathComponents(new List<string> { "/", "a", ".." }), new List<string> { "/" });
    }

    private static void TestCombinePaths()
    {
        Equal(TsPath.CombinePaths("path", "to", "file.ext"), "path/to/file.ext");
        Equal(TsPath.CombinePaths("path", "dir", "..", "to", "file.ext"), "path/dir/../to/file.ext");
        Equal(TsPath.CombinePaths("/path", "to", "file.ext"), "/path/to/file.ext");
        Equal(TsPath.CombinePaths("/path", "/to", "file.ext"), "/to/file.ext");
        Equal(TsPath.CombinePaths("c:/path", "to", "file.ext"), "c:/path/to/file.ext");
        Equal(TsPath.CombinePaths("c:/path", "c:/to", "file.ext"), "c:/to/file.ext");
        Equal(TsPath.CombinePaths("file:///path", "to", "file.ext"), "file:///path/to/file.ext");
        Equal(TsPath.CombinePaths("file:///path", "file:///to", "file.ext"), "file:///to/file.ext");
        Equal(TsPath.CombinePaths("/", "/node_modules/@types"), "/node_modules/@types");
        Equal(TsPath.CombinePaths("/a/..", ""), "/a/..");
        Equal(TsPath.CombinePaths("/a/..", "b"), "/a/../b");
        Equal(TsPath.CombinePaths("/a/..", "b/"), "/a/../b/");
        Equal(TsPath.CombinePaths("/a/..", "/"), "/");
        Equal(TsPath.CombinePaths("/a/..", "/b"), "/b");
    }

    private static void TestResolvePath()
    {
        Equal(TsPath.ResolvePath(""), "");
        Equal(TsPath.ResolvePath("."), "");
        Equal(TsPath.ResolvePath("./"), "");
        Equal(TsPath.ResolvePath(".."), "..");
        Equal(TsPath.ResolvePath("../"), "../");
        Equal(TsPath.ResolvePath("/"), "/");
        Equal(TsPath.ResolvePath("/."), "/");
        Equal(TsPath.ResolvePath("/./"), "/");
        Equal(TsPath.ResolvePath("/../"), "/");
        Equal(TsPath.ResolvePath("/a"), "/a");
        Equal(TsPath.ResolvePath("/a/"), "/a/");
        Equal(TsPath.ResolvePath("/a/."), "/a");
        Equal(TsPath.ResolvePath("/a/./"), "/a/");
        Equal(TsPath.ResolvePath("/a/./b"), "/a/b");
        Equal(TsPath.ResolvePath("/a/./b/"), "/a/b/");
        Equal(TsPath.ResolvePath("/a/.."), "/");
        Equal(TsPath.ResolvePath("/a/../"), "/");
        Equal(TsPath.ResolvePath("/a/../b"), "/b");
        Equal(TsPath.ResolvePath("/a/../b/"), "/b/");
        Equal(TsPath.ResolvePath("/a/..", "b"), "/b");
        Equal(TsPath.ResolvePath("/a/..", "/"), "/");
        Equal(TsPath.ResolvePath("/a/..", "b/"), "/b/");
        Equal(TsPath.ResolvePath("/a/..", "/b"), "/b");
        Equal(TsPath.ResolvePath("/a/.", "b"), "/a/b");
        Equal(TsPath.ResolvePath("/a/.", "."), "/a");
        Equal(TsPath.ResolvePath("a", "b", "c"), "a/b/c");
        Equal(TsPath.ResolvePath("a", "b", "/c"), "/c");
        Equal(TsPath.ResolvePath("a", "b", "../c"), "a/c");
    }

    private static void TestResolvePathWithoutTrailingDirectorySeparator()
    {
        Equal(TsPath.ResolvePathWithoutTrailingDirectorySeparator("/"), "/");
        Equal(TsPath.ResolvePathWithoutTrailingDirectorySeparator("c:/"), "c:/");
        Equal(TsPath.ResolvePathWithoutTrailingDirectorySeparator("/a/"), "/a");
        Equal(TsPath.ResolvePathWithoutTrailingDirectorySeparator("a", "b/"), "a/b");
    }

    private static void TestNormalizePathDriveRoot()
    {
        Equal(TsPath.NormalizePath("c:"), "c:/");
    }

    private static void TestGetNormalizedAbsolutePath()
    {
        Equal(TsPath.GetNormalizedAbsolutePath("/", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/.", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/./", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/../", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a", ""), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/", ""), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/.", ""), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/foo.", ""), "/a/foo.");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/./", ""), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/./b", ""), "/a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/./b/", ""), "/a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/..", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/../", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/../", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/../b", ""), "/b");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/../b/", ""), "/b");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/..", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/..", "/"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/..", "b/"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/..", "/b"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/.", "b"), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/.", "."), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("\\", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\.", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\.\\", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\..\\", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\.\\", ""), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\.\\b", ""), "/a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\.\\b\\", ""), "/a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..\\", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..\\", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..\\b", ""), "/b");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..\\b\\", ""), "/b");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..", "\\"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..", "b\\"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\..", "\\b"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\.", "b"), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\.", "."), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("", ""), "");
        Equal(TsPath.GetNormalizedAbsolutePath(".", ""), "");
        Equal(TsPath.GetNormalizedAbsolutePath("./", ""), "");
        Equal(TsPath.GetNormalizedAbsolutePath("..", ""), "..");
        Equal(TsPath.GetNormalizedAbsolutePath("../", ""), "..");
        Equal(TsPath.GetNormalizedAbsolutePath("", "/home"), "/home");
        Equal(TsPath.GetNormalizedAbsolutePath(".", "/home"), "/home");
        Equal(TsPath.GetNormalizedAbsolutePath("./", "/home"), "/home");
        Equal(TsPath.GetNormalizedAbsolutePath("..", "/home"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("../", "/home"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("a", "b"), "b/a");
        Equal(TsPath.GetNormalizedAbsolutePath("a", "b/c"), "b/c/a");
        Equal(TsPath.GetNormalizedAbsolutePath(".a", ""), ".a");
        Equal(TsPath.GetNormalizedAbsolutePath("..a", ""), "..a");
        Equal(TsPath.GetNormalizedAbsolutePath("a.", ""), "a.");
        Equal(TsPath.GetNormalizedAbsolutePath("a..", ""), "a..");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/./.a", ""), "/base/.a");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/../.a", ""), "/.a");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/./..a", ""), "/base/..a");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/../..a", ""), "/..a");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/./..a/b", ""), "/base/..a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/../..a/b", ""), "/..a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/./a.", ""), "/base/a.");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/../a.", ""), "/a.");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/./a..", ""), "/base/a..");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/../a..", ""), "/a..");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/./a../b", ""), "/base/a../b");
        Equal(TsPath.GetNormalizedAbsolutePath("/base/../a../b", ""), "/a../b");
        Equal(TsPath.GetNormalizedAbsolutePath("a/..", ""), "");
        Equal(TsPath.GetNormalizedAbsolutePath("/a//", ""), "/a");
        Equal(TsPath.GetNormalizedAbsolutePath("//a", "a"), "//a/");
        Equal(TsPath.GetNormalizedAbsolutePath("/\\", ""), "//");
        Equal(TsPath.GetNormalizedAbsolutePath("a///", "a"), "a/a");
        Equal(TsPath.GetNormalizedAbsolutePath("/.//", ""), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("//\\\\", ""), "///");
        Equal(TsPath.GetNormalizedAbsolutePath(".//a", "."), "a");
        Equal(TsPath.GetNormalizedAbsolutePath("a/../..", ""), "..");
        Equal(TsPath.GetNormalizedAbsolutePath("../..", "\\a"), "/");
        Equal(TsPath.GetNormalizedAbsolutePath("a:", "b"), "a:/");
        Equal(TsPath.GetNormalizedAbsolutePath("a/../..", ".."), "../..");
        Equal(TsPath.GetNormalizedAbsolutePath("a/../..", "b"), "");
        Equal(TsPath.GetNormalizedAbsolutePath("a//../..", ".."), "../..");
        Equal(TsPath.GetNormalizedAbsolutePath("a//b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a///b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a/b//c", ""), "a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("/a/b//c", ""), "/a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("//a/b//c", ""), "//a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("a\\\\b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a\\\\\\b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a\\b\\\\c", ""), "a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\b\\\\c", ""), "/a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("\\\\a\\b\\\\c", ""), "//a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("a/\\b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a\\/b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a\\/\\b", ""), "a/b");
        Equal(TsPath.GetNormalizedAbsolutePath("a\\b//c", ""), "a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("\\a\\b\\\\c", ""), "/a/b/c");
        Equal(TsPath.GetNormalizedAbsolutePath("\\\\a\\b\\\\c", ""), "//a/b/c");
    }
}
