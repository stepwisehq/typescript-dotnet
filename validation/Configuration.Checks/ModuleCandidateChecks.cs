// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Complete original candidate-normalization assertion domain; friend accessor calls sole source helper.
using TypeScript.Modules;
using TypeScript.Scanner;

namespace Configuration.Checks;

public static class ModuleCandidateChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestNormalizePathForCJSResolutionPreservesDirectoryIntent"];

    public static void Run(string name)
    {
        if (name != Cases[0])
            throw new ArgumentOutOfRangeException(nameof(name));

        foreach (var row in new (string Name, string Directory, string ModuleName, string Path, string Text, bool DirectoryOnly)[]
        {
            ("file", "/project", "file", "/project/file", "/project/file", false),
            ("trailing separator", "/project", "directory/", "/project/directory", "/project/directory/", true),
            ("current directory", "/project", ".", "/project", "/project/", true),
            ("current directory with backslash", "/project", ".\\", "/project", "/project/", true),
            ("nested parent directory", "/project/src", "../lib/", "/project/lib", "/project/lib/", true),
            ("nested parent directory with backslashes", "/project/src", "..\\lib\\", "/project/lib", "/project/lib/", true),
            ("posix root", "/project", "..", "/", "/", true),
            ("drive root", "c:/project", "..", "c:/", "c:/", true),
            ("drive root without separator", "/project", "c:", "c:/", "c:/", true),
            ("UNC root without separator", "/project", "//server", "//server/", "//server/", true),
            ("URL root", "file:///project", "..", "file:///", "file:///", true),
            ("URL authority root without separator", "/project", "file://server", "file://server/", "file://server/", true),
            ("dynamic reserved prefix", "^/~ts-uri~/custom/ts-nul-authority/folder", "./~ts-uri-escape~file", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e66696c65~", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e66696c65~", false),
            ("dynamic reserved prefix with extension", "^/~ts-uri~/custom/ts-nul-authority/folder", "./~ts-uri-escape~file.ts", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e66696c65~.ts", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e66696c65~.ts", false),
            ("dynamic reserved directory prefix", "^/~ts-uri~/custom/ts-nul-authority/folder", "./~ts-uri-escape~dir/file", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e646972~/file", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e646972~/file", false),
            ("dynamic dotted directory intent", "^/~ts-uri~/custom/ts-nul-authority/folder", "./~ts-uri-escape~dir.js/", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e6469722e6a73~", "^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~7e74732d7572692d6573636170657e6469722e6a73~/", true),
        })
        {
            var actual = DefaultResolver.VerificationNormalizePathForCJSResolution(
                TsPath.RootedDirectoryPathFromNormalized(row.Directory), row.ModuleName);
            Check.Equal(actual.Path, TsPath.RootedFilePathFromNormalized(row.Path).AsPath(), row.Name + " original path");
            Check.Equal(actual.DirectoryOnly, row.DirectoryOnly, row.Name + " original directoryOnly");
            Check.Equal(actual.Text, row.Text, row.Name + " original AsString");
            Check.Equal(actual.HasTrailingDirectorySeparator, row.DirectoryOnly, row.Name + " original trailing separator");
        }
    }
}
