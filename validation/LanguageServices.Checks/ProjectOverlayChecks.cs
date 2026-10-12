// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/project/overlayfs_test.go assertions, sequential subtest schedule.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static class ProjectOverlayChecks
{
    public static readonly string[] Names = ["TestProcessChanges", "TestOverlayFSFileSystem"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    private static DocumentUri Uri(string value) => new(ByteStringValue.FromString(value));
    private static FileChange Open(DocumentUri uri, string text = "const x = 1;", LanguageKind kind = default, int version = 1) => new FileChange(FileChangeKind.Open, uri, version, ByteStringValue.FromString(text), kind == default ? LanguageKind.TypeScript : kind);
    private static OverlayFS Create() => new(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/test1.ts", "// existing content"), new Input("/test2.ts", "// existing content"), new Input("/script", "// extensionless content")), new(), PositionEncodingKind.UTF16);
    private static FileChangeSummary Apply(OverlayFS fs, params FileChange[] changes) => fs.ProcessChanges(Slice<FileChange>.FromArray(changes)).Summary;
    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException("Original OverlayFS assertion failed");
    }

    private static bool Has(HashSet<DocumentUri>? set, DocumentUri uri) => set?.Contains(uri) == true;
    public static void Run(string name)
    {
        if (name == Names[1])
        {
            FileSystem();
            return;
        }

        if (name != Names[0])
            throw new ArgumentException(name);
        var first = Uri("file:///test1.ts");
        var second = Uri("file:///test2.ts");
        var fs = Create();
        bool panicked = false;
        try
        {
            Apply(fs, Open(first), Open(second, "const y = 2;"));
        }
        catch (InvalidOperationException)
        {
            panicked = true;
        }

        Require(panicked);
        fs = Create();
        Require(Apply(fs, new FileChange(FileChangeKind.WatchCreate, first), new FileChange(FileChangeKind.WatchDelete, first)).IsEmpty());
        fs = Create();
        var result = Apply(fs, new FileChange(FileChangeKind.WatchDelete, first), new FileChange(FileChangeKind.WatchCreate, first));
        Require((result.Created?.Count ?? 0) == 0 && (result.Deleted?.Count ?? 0) == 0 && Has(result.Changed, first));
        fs = Create();
        result = Apply(fs, new FileChange(FileChangeKind.WatchChange, first), new FileChange(FileChangeKind.WatchChange, first), new FileChange(FileChangeKind.WatchChange, first));
        Require(Has(result.Changed, first) && result.Changed!.Count == 1);
        fs = Create();
        Apply(fs, Open(first));
        Require(Apply(fs, new FileChange(FileChangeKind.Save, first)).IsEmpty());
        Require(fs.GetFile(first.FileName())!.MatchesDiskText());
        fs = Create();
        var mts = Uri("file:///test.mts");
        Apply(fs, Open(mts, "export const x = 1;", new(ByteStringValue.FromString("mts"))));
        Require(fs.GetFile(mts.FileName())!.Kind() == ScriptKind.TS);
        fs = Create();
        var script = Uri("file:///script");
        Apply(fs, Open(script, kind: new(ByteStringValue.FromString("plaintext"))));
        Require(fs.GetFile(script.FileName())!.Kind() == ScriptKind.Unknown);
        fs = Create();
        Require(fs.GetFile(RootedFilePath.FromGoString("/script"))!.Kind() == ScriptKind.Unknown);
        fs = Create();
        Apply(fs, Open(first));
        Require(!fs.GetFile(first.FileName())!.MatchesDiskText());
        Apply(fs, new FileChange(FileChangeKind.Save, first));
        Require(fs.GetFile(first.FileName())!.MatchesDiskText());
        Apply(fs, new FileChange(FileChangeKind.WatchChange, first));
        Require(!fs.GetFile(first.FileName())!.MatchesDiskText());
        fs = Create();
        Require(Has(Apply(fs, new FileChange(FileChangeKind.Save, first)).Changed, first));
        fs = Create();
        Apply(fs, Open(first));
        Apply(fs, new FileChange(FileChangeKind.Close, first));
        Require(Apply(fs, new FileChange(FileChangeKind.Close, first)).IsEmpty());
        Require(Apply(fs, new FileChange(FileChangeKind.Change, first, 2, Changes: Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("const x = 1;") } }]))).IsEmpty());
        fs = Create();
        Apply(fs, Open(first));
        result = Apply(fs, new FileChange(FileChangeKind.Close, first), Open(first, "const x = 2;", version: 0));
        Require(result.Opened == default && Has(result.Changed, first));
        Require(fs.GetFile(first.FileName())!.ContentValue().EqualsBytes("const x = 2;"u8));
    }

    private static void FileSystem()
    {
        var path = RootedFilePath.FromGoString("/virtual/nested/file.ts");
        var fs = new OverlayFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/virtual", "host file")), new() { [CaseSensitivity.CaseInsensitive.PathKey(path.AsPath())] = new(path, ByteStringValue.FromString("overlay"), 1, ScriptKind.TS) }, PositionEncodingKind.UTF16);
        Require(fs.DirectoryExists(RootedDirectoryPath.FromGoString("/virtual")));
        Require(!fs.FileExists(RootedFilePath.FromGoString("/virtual")));
        Require(fs.Stat(RootedPath.FromGoString("/virtual"))!.IsDir());
        var(content, ok) = fs.ReadFile(path);
        Require(ok && content == "overlay");
        var entries = fs.GetAccessibleEntries(RootedDirectoryPath.FromGoString("/"));
        Require(Contains(entries.Directories, "virtual"u8));
        Require(!Contains(entries.Files, "virtual"u8));
    }

    private static bool Contains(Slice<ByteStringValue> values, ReadOnlySpan<byte> name)
    {
        foreach (var value in values)
            if (value.EqualsBytes(name))
                return true;
        return false;
    }
}
