// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Read-side port of tsc/internal/bundled/embed.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Writable forwarding/panics are outside the read-side IFS boundary.
using TypeScript.Scanner;
using TypeScript.Vfs;
using FileInfo = TypeScript.Vfs.FileInfo;
using FileMode = TypeScript.Vfs.FileMode;

namespace TypeScript.Bundled;
public static partial class Bundled
{
    private const string scheme = "bundled:///";
    private static (string Remaining, bool Ok) SplitPath(string path) => path.StartsWith(scheme, StringComparison.Ordinal) ? (path[scheme.Length..], true) : ("", false);
    private static string LibPathText() => scheme + "libs";
    public static bool IsBundled(string path) => SplitPath(path).Ok;
    private static readonly Lazy<Dictionary<string, ByteStringView>> embeddedContents = new(() =>
    {
        var files = new Dictionary<string, ByteStringView>(StringComparer.Ordinal);
        foreach (string name in LibNames!)
        {
            using Stream input = typeof(Bundled).Assembly.GetManifestResourceStream("TypeScript.Bundled.Libs." + name)
                ?? throw new InvalidDataException("Bundled library resource is missing: " + name);
            using var bytes = new MemoryStream();
            input.CopyTo(bytes);
            files.Add("libs/" + name, ByteStringView.FromOwnedBytes(bytes.ToArray()));
        }
        return files;
    });

    private sealed class WrappedFS(IFS fs) : IFS, ISourceTextFileSystem
    {
        public CaseSensitivity CaseSensitivity() => fs.CaseSensitivity();
        public bool FileExists(RootedFilePath path)
        {
            var (rest, ok) = SplitPath(path.AsString());
            return ok ? embeddedContents.Value.ContainsKey(rest) : fs.FileExists(path);
        }
        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            var (rest, ok) = SplitPath(path.AsString());
            if (!ok) return fs.ReadFile(path);
            return embeddedContents.Value.TryGetValue(rest, out var content) ? (content.Materialize(), true) : ("", false);
        }
        public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path)
        {
            var (rest, ok) = SplitPath(path.AsString());
            if (!ok) return SourceTextReaders.Read(fs, path);
            return embeddedContents.Value.TryGetValue(rest, out var content) ? (content, true) : (default, false);
        }
        public bool DirectoryExists(RootedDirectoryPath path)
        {
            var (rest, ok) = SplitPath(path.AsString());
            return ok ? rest == "libs" : fs.DirectoryExists(path);
        }
        public Entries GetAccessibleEntries(RootedDirectoryPath path)
        {
            var (rest, ok) = SplitPath(path.AsString());
            if (!ok) return fs.GetAccessibleEntries(path);
            var result = new Entries();
            if (rest == "") result.Directories = ["libs"];
            else if (rest == "libs") result.Files = LibNames;
            return result;
        }
        public FileInfo? Stat(RootedPath path)
        {
            var (rest, ok) = SplitPath(path.AsString());
            if (!ok) return fs.Stat(path);
            if (rest == "" || rest == "libs") return new EmbeddedFileInfo { name = rest, mode = FileMode.Dir };
            if (embeddedContents.Value.TryGetValue(rest, out var lib))
                return new EmbeddedFileInfo { name = rest.StartsWith("libs/", StringComparison.Ordinal) ? rest[5..] : rest, size = lib.Length };
            return null;
        }
        public RootedPath Realpath(RootedPath path) => IsBundled(path.AsString()) ? path : fs.Realpath(path);
    }

    private sealed class EmbeddedFileInfo : FileInfo, DirEntry
    {
        internal FileMode mode;
        internal string name = "";
        internal long size;
        public bool IsDir() => (mode & FileMode.Dir) != 0;
        public DateTimeOffset ModTime() => DateTimeOffset.MinValue;
        public FileMode Mode() => mode;
        public string Name() => name;
        public long Size() => size;
        public object? Sys() => null;
        public (FileInfo? Info, Exception? Error) Info() => (this, null);
        public FileMode Type() => mode & FileMode.Type;
    }
}
