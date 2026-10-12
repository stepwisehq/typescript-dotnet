// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of internal/vfs/wrapvfs/wrapvfs.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Vfs;
internal static class WrapVfs
{
    // Go copies this callback header into wrappedFS. Byte-valued internal callbacks
    // retain original string owners; public IFS methods materialize at that boundary.
    internal struct Replacements
    {
        internal Func<CaseSensitivity>? CaseSensitivity;
        internal Func<RootedFilePath, bool>? FileExists;
        internal Func<RootedFilePath, (ByteStringValue Contents, bool Ok)>? ReadFile;
        internal Func<RootedFilePath, ByteStringValue, Exception?>? WriteFile;
        internal Func<RootedFilePath, ByteStringValue, Exception?>? AppendFile;
        internal Func<RootedPath, Exception?>? Remove;
        internal Func<RootedPath, DateTimeOffset, DateTimeOffset, Exception?>? Chtimes;
        internal Func<RootedDirectoryPath, bool>? DirectoryExists;
        internal Func<RootedDirectoryPath, Entries>? GetAccessibleEntries;
        internal Func<RootedPath, FileInfo?>? Stat;
        internal Func<RootedPath, RootedPath>? Realpath;
    }

    // wrapvfs.go:25
    internal static IFS Wrap(IFS fs, Replacements replacements) => new WrappedFS(fs, replacements);
    private sealed class WrappedFS : IFS, IMutableFileSystem, ISourceTextFileSystem, IOutputTextFileSystem
    {
        private readonly IFS fs;
        private readonly Replacements replacements;
        internal WrappedFS(IFS fs, Replacements replacements)
        {
            this.fs = fs;
            this.replacements = replacements;
        }

        // wrapvfs.go:39
        public CaseSensitivity CaseSensitivity() => replacements.CaseSensitivity is { } callback ? callback() : fs.CaseSensitivity();
        // wrapvfs.go:47
        public bool FileExists(RootedFilePath path) => replacements.FileExists is { } callback ? callback(path) : fs.FileExists(path);
        // wrapvfs.go:55
        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            if (replacements.ReadFile is not { } callback)
                return fs.ReadFile(path);
            var(contents, ok) = callback(path);
            return (contents.Materialize()!, ok);
        }

        public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path)
        {
            if (replacements.ReadFile is not { } callback)
                return SourceTextReaders.Read(fs, path);
            var(contents, ok) = callback(path);
            ByteStringView view = contents.Length == 0 ? default : contents.IsBytes ? contents.ByteView : ByteStringView.FromGoBytes(contents.Materialize()!);
            return (view, ok);
        }

        // wrapvfs.go:63
        public Exception? WriteFile(RootedFilePath path, string data) => replacements.WriteFile is { } callback ? callback(path, ByteStringValue.FromString(data)) : fs.WriteFile(path, data);
        public Exception? WriteOutputText(RootedFilePath path, ByteStringValue data) => replacements.WriteFile is { } callback ? callback(path, data) : OutputTextWriters.Write(fs, path, data);
        // wrapvfs.go:71
        public Exception? AppendFile(RootedFilePath path, ByteStringValue data) => replacements.AppendFile is { } callback ? callback(path, data) : FileSystemMutations.AppendFile(fs, path, data);
        // wrapvfs.go:79
        public Exception? Remove(RootedPath path) => replacements.Remove is { } callback ? callback(path) : FileSystemMutations.Remove(fs, path);
        // wrapvfs.go:87
        public Exception? Chtimes(RootedPath path, DateTimeOffset accessTime, DateTimeOffset modificationTime) => replacements.Chtimes is { } callback ? callback(path, accessTime, modificationTime) : FileSystemMutations.Chtimes(fs, path, accessTime, modificationTime);
        // wrapvfs.go:95
        public bool DirectoryExists(RootedDirectoryPath path) => replacements.DirectoryExists is { } callback ? callback(path) : fs.DirectoryExists(path);
        // wrapvfs.go:103
        public Entries GetAccessibleEntries(RootedDirectoryPath path) => replacements.GetAccessibleEntries is { } callback ? callback(path) : fs.GetAccessibleEntries(path);
        // wrapvfs.go:111
        public FileInfo? Stat(RootedPath path) => replacements.Stat is { } callback ? callback(path) : fs.Stat(path);
        // wrapvfs.go:119
        public RootedPath Realpath(RootedPath path) => replacements.Realpath is { } callback ? callback(path) : fs.Realpath(path);
    }
}
