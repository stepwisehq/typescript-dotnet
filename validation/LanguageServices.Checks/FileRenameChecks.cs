// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Two complete original internal/ls/file_rename_test.go assertion functions.
using TypeScript.Scanner;
using TypeScript.LanguageServices;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.SourceMap;

namespace LanguageServices.Checks;
public static class FileRenameChecks
{
    public static readonly string[] Names = ["TestTryRemoveIndexFileName", "TestCreatePathUpdaterCaseFoldingShrinksOldPath"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        if (name == Names[0])
        {
            (string Input, string Expected)[] rows = [("/project/index.ts", "/project"), ("/index.ts", ""), ("c:/index.ts", "c:/"), ("^/index.ts", "")];
            foreach (var row in rows)
                if (LanguageService.tryRemoveIndexFileName(RootedFilePath.FromGoString(row.Input)) != RootedPath.FromGoString(row.Expected))
                    throw new InvalidOperationException("Original removable index path differs: " + row.Input);
        }
        else if (name == Names[1])
        {
            var service = new LanguageService(new CaseInsensitiveHost());
            var oldPath = RootedPath.FromGoString(GoString.ToUtf8("/a/\u212A\u212A\u212A\u212A"));
            var updater = service.createPathUpdater(oldPath, RootedPath.FromGoString("/a/new"));
            var(updated, ok) = updater(RootedFilePath.FromGoString("/a/kkkk/x.ts"));
            if (!ok || updated != RootedFilePath.FromGoString("/a/new/x.ts"))
                throw new InvalidOperationException("Original case-folded descendant update differs");
        }
        else
            throw new ArgumentException(name);
    }

    // The original fixture panics on every other method; no miniature semantic host.
    private sealed class CaseInsensitiveHost : ILanguageServiceHost
    {
        public CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseInsensitive;
        public (ByteStringValue Contents, bool Ok) ReadFile(RootedFilePath path) => throw new InvalidOperationException("not implemented");
        public Converters Converters() => throw new InvalidOperationException("not implemented");
        public UserPreferences GetPreferences(ByteStringValue activeFile) => throw new InvalidOperationException("not implemented");
        public ECMALineInfo? GetECMALineInfo(RootedFilePath name) => throw new InvalidOperationException("not implemented");
        public Registry? AutoImportRegistry() => throw new InvalidOperationException("not implemented");
        public Slice<RootedFilePath> ReadDirectory(RootedDirectoryPath path, Slice<ByteStringValue> extensions, Slice<ByteStringValue> excludes, Slice<ByteStringValue> includes, int depth) => throw new InvalidOperationException("not implemented");
        public Slice<ByteStringValue> GetDirectories(RootedDirectoryPath path) => throw new InvalidOperationException("not implemented");
        public bool DirectoryExists(RootedDirectoryPath path) => throw new InvalidOperationException("not implemented");
        public bool FileExists(RootedFilePath path) => throw new InvalidOperationException("not implemented");
    }
}
