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
    private static (ByteStringValue Remaining, bool Ok) SplitPath(ByteStringValue path) => path.StartsWith("bundled:///"u8) ? (path.Slice(scheme.Length), true) : (default, false);
    private static string LibPathText() => scheme + "libs";
    public static bool IsBundled(string path) => IsBundled(ByteStringValue.FromString(path is null ? throw new NullReferenceException() : path));
    internal static bool IsBundled(ByteStringValue path) => SplitPath(path).Ok;
    private static readonly Lazy<Dictionary<ByteStringKey, ByteStringView>> embeddedContents = new(() =>
    {
        var files = new Dictionary<ByteStringKey, ByteStringView>(ByteStringKey.Comparer);
        foreach (string name in LibNames)
        {
            using Stream input = typeof(Bundled).Assembly.GetManifestResourceStream("TypeScript.Bundled.Libs." + name) ?? throw new InvalidDataException("Bundled library resource is missing: " + name);
            using var bytes = new MemoryStream();
            input.CopyTo(bytes);
            files.Add(ByteStringKey.FromString("libs/" + name), ByteStringView.FromOwnedBytes(bytes.ToArray()));
        }

        return files;
    });
    // LibNames is declared in another generated partial. The static constructor
    // runs after all field initializers and publishes one shared immutable header.
    private static readonly Slice<ByteStringValue> libNameValues;
    static Bundled() => libNameValues = Core.MapSlice(LibNames, static name => ByteStringValue.FromString(name));
    private sealed class WrappedFS(IFS fs) : IFS, ISourceTextFileSystem, IOutputTextFileSystem, IMutableFileSystem
    {
        public CaseSensitivity CaseSensitivity() => fs.CaseSensitivity();
        // embed.go:106. Embedded files are not writable, matching the original panic.
        public Exception? AppendFile(RootedFilePath path, ByteStringValue data)
        {
            if (SplitPath(path.AsValue()).Ok)
                throw new InvalidOperationException("cannot write to embedded file system");
            return FileSystemMutations.AppendFile(fs, path, data);
        }

        public Exception? Remove(RootedPath path)
        {
            if (SplitPath(path.AsValue()).Ok)
                throw new InvalidOperationException("cannot remove from embedded file system");
            return FileSystemMutations.Remove(fs, path);
        }

        public Exception? Chtimes(RootedPath path, DateTimeOffset accessTime, DateTimeOffset modificationTime)
        {
            if (SplitPath(path.AsValue()).Ok)
                throw new InvalidOperationException("cannot change times on embedded file system");
            return FileSystemMutations.Chtimes(fs, path, accessTime, modificationTime);
        }

        public Exception? WriteFile(RootedFilePath path, string data) => WriteOutputText(path, ByteStringValue.FromString(data));
        public Exception? WriteOutputText(RootedFilePath path, ByteStringValue data)
        {
            if (SplitPath(path.AsValue()).Ok)
                throw new InvalidOperationException("cannot write to embedded file system");
            return OutputTextWriters.Write(fs, path, data);
        }

        public bool FileExists(RootedFilePath path)
        {
            var(rest, ok) = SplitPath(path.AsValue());
            return ok ? embeddedContents.Value.ContainsKey(ByteStringKey.FromValue(rest)) : fs.FileExists(path);
        }

        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            var(rest, ok) = SplitPath(path.AsValue());
            if (!ok)
                return fs.ReadFile(path);
            return rest.TryGetValue(embeddedContents.Value, out var content) ? (content.Materialize(), true) : ("", false);
        }

        public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path)
        {
            var(rest, ok) = SplitPath(path.AsValue());
            if (!ok)
                return SourceTextReaders.Read(fs, path);
            return rest.TryGetValue(embeddedContents.Value, out var content) ? (content, true) : (default, false);
        }

        public bool DirectoryExists(RootedDirectoryPath path)
        {
            var(rest, ok) = SplitPath(path.AsValue());
            return ok ? rest.EqualsBytes("libs"u8) : fs.DirectoryExists(path);
        }

        public Entries GetAccessibleEntries(RootedDirectoryPath path)
        {
            var(rest, ok) = SplitPath(path.AsValue());
            if (!ok)
                return fs.GetAccessibleEntries(path);
            var result = new Entries();
            if (rest.Length == 0)
                result.Directories = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("libs")]);
            else if (rest.EqualsBytes("libs"u8))
                result.Files = libNameValues;
            return result;
        }

        public FileInfo? Stat(RootedPath path)
        {
            var(rest, ok) = SplitPath(path.AsValue());
            if (!ok)
                return fs.Stat(path);
            if (rest.Length == 0 || rest.EqualsBytes("libs"u8))
                return new EmbeddedFileInfo
                {
                    name = rest,
                    mode = FileMode.Dir
                };
            if (rest.TryGetValue(embeddedContents.Value, out var lib))
                return new EmbeddedFileInfo
                {
                    name = rest.StartsWith("libs/"u8) ? rest.Slice(5) : rest,
                    size = lib.Length
                };
            return null;
        }

        public RootedPath Realpath(RootedPath path) => IsBundled(path.AsValue()) ? path : fs.Realpath(path);
    }

    private sealed class EmbeddedFileInfo : FileInfo, DirEntry
    {
        internal FileMode mode;
        internal ByteStringValue name;
        internal long size;
        public bool IsDir() => (mode & FileMode.Dir) != 0;
        public DateTimeOffset ModTime() => DateTimeOffset.MinValue;
        public FileMode Mode() => mode;
        public string Name() => name.Materialize()!;
        public long Size() => size;
        public object? Sys() => null;
        public (FileInfo? Info, Exception? Error) Info() => (this, null);
        public FileMode Type() => mode & FileMode.Type;
    }
}
