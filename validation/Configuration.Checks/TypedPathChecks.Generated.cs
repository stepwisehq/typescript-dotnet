// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Generated unchanged original typed_paths_test.go literals, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace Configuration.Checks;
public static partial class TypedPathChecks
{
    private sealed record StemRow(string File, string Stem, string Output);
    private sealed record StemCompareRow(string First, string Second, bool Sensitive, bool Insensitive);
    private sealed record ContainsRow(string Directory, string File, string Relative, bool Insensitive, bool Contained);
    private static readonly StemRow[] stemRows =
    [
        new("", "", ""),
        new("/project/file.ts", "/project/file", "/project/file.js"),
        new("/project/.ts", "/project/", "/project/.js"),
        new("/project/..ts", "/project/.", "/project/..js"),
        new("/project/...ts", "/project/..", "/project/...js"),
        new("/project/.d.ts", "/project/", "/project/.js"),
        new("/.ts", "/", "/.js"),
        new("c:/.ts", "c:/", "c:/.js"),
        new("//server/.ts", "//server/", "//server/.js"),
        new("file:///.ts", "file:///", "file:///.js"),
        new("^/~ts-uri~/custom/ts-nul-authority/.ts", "^/~ts-uri~/custom/ts-nul-authority/", "^/~ts-uri~/custom/ts-nul-authority/.js"),
    ];
    private static readonly StemCompareRow[] stemCompareRows =
    [
        new("/project/.ts", "/project/.d.ts", true, true),
        new("/project/..ts", "/project.ts", false, false),
        new("/project/...ts", "/.ts", false, false),
        new("c:/project/.ts", "C:/project/.d.ts", true, true),
        new("/project/File.ts", "/project/file.js", false, true),
        new("^/~ts-uri~/custom/ts-nul-authority/Foo.ts", "^/~ts-uri~/custom/ts-nul-authority/foo.js", false, false),
    ];
    private static readonly ContainsRow[] containsRows =
    [
        new("/project/src", "/project/src", "", false, true),
        new("/project/src", "/project/src/a.ts", "a.ts", false, true),
        new("/project/src", "/project/source/a.ts", "", false, false),
        new("/", "/project/src/a.ts", "project/src/a.ts", false, true),
        new("/PROJECT", "/project/a.ts", "", false, false),
        new("/PROJECT", "/project/a.ts", "a.ts", true, true),
        new("/repo/\u212a", "/repo/k/a.ts", "a.ts", true, true),
        new("C:/project", "c:/project/a.ts", "a.ts", false, true),
        new("FILE:///C:/project", "file:///c:/project/a.ts", "a.ts", false, true),
        new("file://LOCALHOST/C:/project", "file://localhost/c:/project/a.ts", "a.ts", false, true),
        new("c:/project", "d:/project/a.ts", "", true, false),
    ];
}
