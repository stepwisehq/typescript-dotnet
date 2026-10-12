// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/overlayfs.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.SourceMap;
using TypeScript.Vfs;

namespace TypeScript.ProjectSystem;
internal interface IFileContent
{
    ByteStringValue ContentValue();
    UInt128 Hash();
}

internal interface IFileHandle : IFileContent
{
    RootedFilePath FileName();
    int Version();
    bool MatchesDiskText();
    bool IsOverlay();
    LSPLineMap LSPLineMap();
    ECMALineInfo ECMALineInfo();
    ScriptKind Kind();
}

internal abstract class FileBase : IFileContent
{
    protected readonly RootedFilePath fileName;
    protected ByteStringValue content;
    protected UInt128 hash;
    private readonly object lineGate = new();
    private LSPLineMap? lineMap;
    private ECMALineInfo? lineInfo;
    protected FileBase(RootedFilePath fileName, ByteStringValue content, UInt128? retainedHash = null)
    {
        this.fileName = fileName;
        this.content = content.IsBytes ? content.Snapshot() : ByteStringValue.FromView(FileContentHash.ByteView(content));
        hash = retainedHash ?? FileContentHash.Hash(this.content);
    }

    protected void ResetLineCaches()
    {
        lock (lineGate)
        {
            Volatile.Write(ref lineMap, null);
            Volatile.Write(ref lineInfo, null);
        }
    }

    public RootedFilePath FileName() => fileName;
    public UInt128 Hash() => hash;
    public ByteStringValue ContentValue() => content;
    public LSPLineMap LSPLineMap()
    {
        var result = Volatile.Read(ref lineMap);
        if (result is not null)
            return result;
        lock (lineGate)
        {
            if (lineMap is null)
                Volatile.Write(ref lineMap, Lsconv.ComputeLSPLineStarts(content));
            return lineMap;
        }
    }

    public ECMALineInfo ECMALineInfo()
    {
        var result = Volatile.Read(ref lineInfo);
        if (result is not null)
            return result;
        lock (lineGate)
        {
            if (lineInfo is null)
                Volatile.Write(ref lineInfo, TypeScript.SourceMap.ECMALineInfo.CreateECMALineInfo(content.ByteView, Core.ComputeECMALineStarts(content.ByteView)));
            return lineInfo;
        }
    }
}

internal sealed class CachedFile : FileBase, IFileHandle, TypeScript.ProjectSystem.Dirty.ICloneable<CachedFile>
{
    public CachedFile(RootedFilePath fileName, ByteStringValue content) : base(fileName, content)
    {
    }

    internal CachedFile(RootedFilePath fileName, ByteStringValue content, UInt128 retainedHash) : base(fileName, content, retainedHash)
    {
    }

    internal bool NeedsReload;
    internal PathKey RealpathPath;
    public int Version() => 0;
    public bool MatchesDiskText() => !NeedsReload;
    public bool IsOverlay() => false;
    public ScriptKind Kind() => Core.GetScriptKindFromFileName(fileName);
    // snapshotfs.go reload owners mutate only the clone produced by dirty.Change.
    internal void Reload(ByteStringValue value)
    {
        content = value.IsBytes ? value.Snapshot() : ByteStringValue.FromView(FileContentHash.ByteView(value));
        hash = FileContentHash.Hash(content);
        NeedsReload = false;
        ResetLineCaches();
    }

    public CachedFile Clone() => new(fileName, content, hash)
    {
        RealpathPath = RealpathPath
    };
}

internal sealed class Overlay(RootedFilePath fileName, ByteStringValue content, int version, ScriptKind kind) : FileBase(fileName, content), IFileHandle, IScript
{
    internal int version = version;
    internal readonly ScriptKind kind = kind;
    internal bool matchesDiskText;
    public int Version() => version;
    internal void RecomputeHash() => hash = FileContentHash.Hash(content);
    public ByteStringValue TextValue() => content;
    public RootedFilePath OriginalFileName() => fileName;
    public TypeScript.SpanMap.SpanMap? SpanMap() => null;
    public ByteStringValue OriginalTextValue() => content;
    public bool MatchesDiskText() => matchesDiskText;
    internal (bool Matches, bool Exists) ComputeMatchesDiskText(IFS fs)
    {
        if (fileName.IsDynamic())
            return (false, false);
        var(bytes, ok) = SourceTextReaders.Read(fs, fileName);
        return ok ? (FileContentHash.Hash(ByteStringValue.FromView(bytes)) == hash, true) : (false, false);
    }

    public bool IsOverlay() => true;
    public ScriptKind Kind() => kind;
}

internal static partial class OverlayFiles
{
    // overlayfs.go:99 NewCachedFileHandle; the constructor is newCachedFile.
    public static IFileHandle NewCachedFileHandle(RootedFilePath fileName, ByteStringValue content) => new CachedFile(fileName, content);
}

// overlayfs.go:182–193. Rebasable layers preserve their owning wrapper.
internal interface ILayeredFileSystem : IFS, IFileHandleSource
{
    Dictionary<PathKey, Overlay>? Overlays();
}

internal interface IRebasableFileSystem : IFS
{
    IFS BaseFileSystem();
    ILayeredFileSystem WithBaseFileSystem(IFS baseFileSystem);
}

internal sealed class OverlayFS : ILayeredFileSystem, ISourceTextFileSystem, IOutputTextFileSystem, IMutableFileSystem
{
    private readonly CaseSensitivity sensitivity;
    private readonly IFS host;
    internal IFS BaseHost => host;

    private readonly TypeScript.Lsp.Protocol.PositionEncodingKind positionEncoding;
    private readonly ReaderWriterLockSlim gate = new();
    private Dictionary<PathKey, Overlay>? overlays;
    private Dictionary<PathKey, Dictionary<PathKey, ByteStringValue>> overlayDirectories;
    // overlayfs.go:196.
    internal OverlayFS(IFS host, Dictionary<PathKey, Overlay>? overlays, TypeScript.Lsp.Protocol.PositionEncodingKind positionEncoding)
    {
        this.host = host;
        sensitivity = host.CaseSensitivity();
        this.overlays = overlays;
        this.positionEncoding = positionEncoding;
        overlayDirectories = CreateOverlayDirectories(overlays);
    }

    // overlayfs.go:220. This retained owner is replaced, never mutated in place.
    public Dictionary<PathKey, Overlay>? Overlays()
    {
        gate.EnterReadLock();
        try
        {
            return overlays;
        }
        finally
        {
            gate.ExitReadLock();
        }
    }

    // overlayfs.go:242.
    public IFileHandle? GetFile(RootedFilePath fileName) => GetFileByPath(fileName, sensitivity.PathKey(fileName.AsPath()));
    public IFileHandle? GetFileByPath(RootedFilePath fileName, PathKey path)
    {
        Overlay? overlay;
        bool directory;
        gate.EnterReadLock();
        try
        {
            overlay = overlays?.GetValueOrDefault(path);
            directory = overlayDirectories.ContainsKey(path);
        }
        finally
        {
            gate.ExitReadLock();
        }

        if (overlay is not null)
            return overlay;
        if (directory)
            return null;
        if (host is IFileHandleSource source)
            return source.GetFileByPath(fileName, path);
        var(content, ok) = SourceTextReaders.Read(host, fileName);
        return ok ? new CachedFile(fileName, ByteStringValue.FromView(content)) : null;
    }

    public CaseSensitivity CaseSensitivity() => sensitivity;
    public bool FileExists(RootedFilePath fileName)
    {
        var path = sensitivity.PathKey(fileName.AsPath());
        bool file, directory;
        gate.EnterReadLock();
        try
        {
            file = overlays?.ContainsKey(path) == true;
            directory = overlayDirectories.ContainsKey(path);
        }
        finally
        {
            gate.ExitReadLock();
        }

        return file || !directory && host.FileExists(fileName);
    }

    public (string Contents, bool Ok) ReadFile(RootedFilePath fileName)
    {
        var file = GetFile(fileName);
        return file is null ? ("", false) : (file.ContentValue().Materialize()!, true);
    }

    public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath fileName)
    {
        var file = GetFile(fileName);
        return file is null ? (default, false) : (FileContentHash.ByteView(file.ContentValue()), true);
    }

    public Exception? WriteFile(RootedFilePath path, string content) => host.WriteFile(path, content);
    public Exception? WriteOutputText(RootedFilePath path, ByteStringValue content) => OutputTextWriters.Write(host, path, content);
    public Exception? AppendFile(RootedFilePath path, ByteStringValue content) => FileSystemMutations.AppendFile(host, path, content);
    public Exception? Remove(RootedPath path) => FileSystemMutations.Remove(host, path);
    public Exception? Chtimes(RootedPath path, DateTimeOffset accessTime, DateTimeOffset modificationTime) => FileSystemMutations.Chtimes(host, path, accessTime, modificationTime);
    public bool DirectoryExists(RootedDirectoryPath directoryName)
    {
        var path = sensitivity.PathKey(directoryName.AsPath());
        bool file, directory;
        gate.EnterReadLock();
        try
        {
            file = overlays?.ContainsKey(path) == true;
            directory = overlayDirectories.ContainsKey(path);
        }
        finally
        {
            gate.ExitReadLock();
        }

        return directory || !file && host.DirectoryExists(directoryName);
    }

    // overlayfs.go:307. Clone each host collection before overlay deletion/appends.
    public Entries GetAccessibleEntries(RootedDirectoryPath directoryName)
    {
        var path = sensitivity.PathKey(directoryName.AsPath());
        Dictionary<PathKey, ByteStringValue>? directory;
        Dictionary<PathKey, Overlay>? retainedOverlays;
        bool file;
        gate.EnterReadLock();
        try
        {
            file = overlays?.ContainsKey(path) == true;
            directory = overlayDirectories.TryGetValue(path, out var found) ? new(found, found.Comparer) : null;
            retainedOverlays = overlays;
        }
        finally
        {
            gate.ExitReadLock();
        }

        if (file)
            return default;
        var source = host.GetAccessibleEntries(directoryName);
        var entries = new Entries
        {
            Files = GoSlices.Clone(source.Files),
            Directories = GoSlices.Clone(source.Directories),
            Symlinks = source.Symlinks is null ? null : new(source.Symlinks, source.Symlinks.Comparer),
        };
        if (directory is not null)
            foreach (var(childPath, childName)in directory)
            {
                entries.Files = DeleteName(entries.Files, childName);
                entries.Directories = DeleteName(entries.Directories, childName);
                if (entries.Symlinks is not null)
                    foreach (var name in entries.Symlinks.ToArray())
                        if (sensitivity.CompareValues(name.Value, childName) == 0)
                            entries.Symlinks.Remove(name);
                if (retainedOverlays?.ContainsKey(childPath) == true)
                    entries.Files = entries.Files.Append(childName);
                else
                    entries.Directories = entries.Directories.Append(childName);
            }

        return entries;
    }

    private Slice<ByteStringValue> DeleteName(Slice<ByteStringValue> names, ByteStringValue target)
    {
        int write = 0;
        for (int read = 0; read < names.Length; read++)
            if (sensitivity.CompareValues(names[read], target) != 0)
                names[write++] = names[read];
        names.AsSpan()[write..].Clear();
        return names.Slice2(0, write);
    }

    public TypeScript.Vfs.FileInfo? Stat(RootedPath path)
    {
        Overlay? overlay;
        bool directory;
        gate.EnterReadLock();
        try
        {
            var canonical = sensitivity.PathKey(path);
            overlay = overlays?.GetValueOrDefault(canonical);
            directory = overlayDirectories.ContainsKey(canonical);
        }
        finally
        {
            gate.ExitReadLock();
        }

        if (overlay is not null)
            return new OverlayFileInfo(overlay);
        if (directory)
            return new OverlayDirectoryInfo(path.BaseNameValue());
        return host.Stat(path);
    }

    public RootedPath Realpath(RootedPath path) => host.Realpath(path);
    // overlayfs.go:389. Ancestor directories retain source spelling while keyed canonically.
    private static Dictionary<PathKey, Dictionary<PathKey, ByteStringValue>> CreateOverlayDirectories(Dictionary<PathKey, Overlay>? overlays)
    {
        var directories = new Dictionary<PathKey, Dictionary<PathKey, ByteStringValue>>();
        if (overlays is not null)
            foreach (var(path, overlay)in overlays)
            {
                var childPath = path;
                var child = overlay.FileName().AsPath();
                for (;;)
                {
                    var parentPath = childPath.Parent();
                    var parent = child.Directory();
                    if (childPath == parentPath)
                        break;
                    if (!directories.TryGetValue(parentPath, out var directory))
                        directories[parentPath] = directory = new();
                    directory[childPath] = child.BaseNameValue();
                    childPath = parentPath;
                    child = parent.AsPath();
                }
            }

        return directories;
    }

    private sealed class FileEvents
    {
        internal FileChange? Open, Close;
        internal bool WatchChanged, Saved, Created, Deleted;
        internal Slice<FileChange> Changes;
    }

    // overlayfs.go:411. Reduce a batch first, then publish new immutable owners.
    internal (FileChangeSummary Summary, Dictionary<PathKey, Overlay>? Overlays) ProcessChanges(Slice<FileChange> changes)
    {
        gate.EnterWriteLock();
        try
        {
            FileChangeSummary result = default;
            var next = overlays is null ? null : new Dictionary<PathKey, Overlay>(overlays, overlays.Comparer);
            var eventsByUri = new Dictionary<TypeScript.Lsp.Protocol.DocumentUri, FileEvents>();
            foreach (var change in changes)
            {
                var uri = change.URI;
                if (eventsByUri.TryGetValue(uri, out var events))
                {
                    if (events.Open is not null)
                        throw new InvalidOperationException("should see no changes after open");
                }
                else
                    eventsByUri[uri] = events = new();
                if (!result.IncludesWatchChangeOutsideNodeModules && change.Kind.IsWatchKind() && !uri.Value.Contains("/node_modules/"u8))
                    result.IncludesWatchChangeOutsideNodeModules = true;
                switch (change.Kind)
                {
                    case FileChangeKind.Open:
                        events.Close = null;
                        events.Open = change;
                        events.WatchChanged = false;
                        events.Changes = default;
                        events.Saved = events.Created = events.Deleted = false;
                        break;
                    case FileChangeKind.Close:
                        events.Close = change;
                        events.Changes = default;
                        events.Saved = events.WatchChanged = false;
                        break;
                    case FileChangeKind.Change:
                        if (events.Close is not null)
                            throw new InvalidOperationException("should see no changes after close");
                        events.Changes = events.Changes.Append(change);
                        events.Saved = events.WatchChanged = false;
                        break;
                    case FileChangeKind.Save:
                        events.Saved = true;
                        break;
                    case FileChangeKind.WatchCreate:
                        if (events.Deleted)
                        {
                            events.Deleted = false;
                            events.WatchChanged = true;
                        }
                        else
                            events.Created = true;
                        break;
                    case FileChangeKind.WatchChange:
                        if (!events.Created)
                        {
                            events.WatchChanged = true;
                            events.Saved = false;
                        }

                        break;
                    case FileChangeKind.WatchDelete:
                        events.WatchChanged = events.Saved = false;
                        if (events.Created)
                            events.Created = false;
                        else
                            events.Deleted = true;
                        break;
                }
            }

            foreach (var(uri, events)in eventsByUri)
            {
                var path = uri.PathKey(sensitivity);
                var overlay = next?.GetValueOrDefault(path);
                if (events.Open is { } open)
                {
                    if (result.Opened.Value.Length != 0 || result.Reopened.Value.Length != 0)
                        throw new InvalidOperationException("can only process one file open event at a time");
                    if (overlay is not null && !overlay.ContentValue().EqualsValue(open.Content))
                        Add(ref result.Changed, uri);
                    else if (overlay is null)
                        result.Opened = uri;
                    else
                        result.Reopened = uri;
                    var scriptKind = Lsconv.LanguageKindToScriptKind(open.LanguageKind);
                    if (scriptKind == ScriptKind.Unknown)
                        scriptKind = Core.GetScriptKindFromFileName(uri.FileName());
                    next![path] = new Overlay(uri.FileName(), open.Content, open.Version, scriptKind);
                    continue;
                }

                if (events.Close is not null && overlay is not null)
                {
                    Add(ref result.Closed, uri);
                    next!.Remove(path);
                    overlay = null;
                }

                if (events.WatchChanged)
                {
                    if (overlay is null)
                        Add(ref result.Changed, uri);
                    else if (!events.Saved)
                    {
                        var(matches, _) = overlay.ComputeMatchesDiskText(host);
                        if (matches != overlay.MatchesDiskText())
                        {
                            overlay = new Overlay(overlay.FileName(), overlay.ContentValue(), overlay.Version(), overlay.kind)
                            {
                                matchesDiskText = matches
                            };
                            next![path] = overlay;
                        }
                    }
                }

                if (events.Changes.Length > 0 && overlay is not null)
                {
                    Add(ref result.Changed, uri);
                    foreach (var change in events.Changes)
                    {
                        var converters = Converters.NewConverters(positionEncoding, _ => overlay!.LSPLineMap());
                        foreach (var textChange in change.Changes)
                        {
                            if (textChange.Partial is { } partial)
                            {
                                var ranges = converters.FromLSPRange(overlay, partial.Range, TypeScript.SpanMap.Feature.All);
                                Debug.Assert(ranges.Length == 1, "expected exactly one range for partial change");
                                var edit = new TextChange(ranges[0].Span, partial.Text);
                                overlay = new Overlay(overlay.FileName(), edit.ApplyTo(overlay.ContentValue()), change.Version, overlay.kind);
                            }
                            else if (textChange.WholeDocument is { } whole)
                                overlay = new Overlay(overlay.FileName(), whole.Text, change.Version, overlay.kind);
                        }

                        if (change.Changes.Length > 0)
                        {
                            overlay.version = change.Version;
                            overlay.RecomputeHash();
                            overlay.matchesDiskText = false;
                            next![path] = overlay;
                        }
                    }
                }

                if (events.Saved)
                {
                    if (overlay is not null)
                    {
                        overlay = new Overlay(overlay.FileName(), overlay.ContentValue(), overlay.Version(), overlay.kind)
                        {
                            matchesDiskText = true
                        };
                        next![path] = overlay;
                    }
                    else if (!events.WatchChanged)
                        Add(ref result.Changed, uri);
                }

                if (events.Created && overlay is null)
                    Add(ref result.Created, uri);
                if (events.Deleted && overlay is null)
                    Add(ref result.Deleted, uri);
            }

            overlays = next;
            overlayDirectories = CreateOverlayDirectories(next);
            return (result, next);
        }
        finally
        {
            gate.ExitWriteLock();
        }
    }

    private static void Add(ref HashSet<TypeScript.Lsp.Protocol.DocumentUri>? set, TypeScript.Lsp.Protocol.DocumentUri uri)
    {
        set ??= new();
        set.Add(uri);
    }

    private sealed class OverlayFileInfo(Overlay overlay) : TypeScript.Vfs.FileInfo
    {
        public string Name() => overlay.FileName().BaseNameValue().Materialize()!;
        public long Size() => overlay.ContentValue().Length;
        public TypeScript.Vfs.FileMode Mode() => (TypeScript.Vfs.FileMode)0x124;
        public DateTimeOffset ModTime() => default;
        public bool IsDir() => false;
        public object? Sys() => null;
    }

    private sealed class OverlayDirectoryInfo(ByteStringValue name) : TypeScript.Vfs.FileInfo
    {
        public string Name() => name.Materialize()!;
        public long Size() => 0;
        public TypeScript.Vfs.FileMode Mode() => TypeScript.Vfs.FileMode.Dir | (TypeScript.Vfs.FileMode)0x16D;
        public DateTimeOffset ModTime() => default;
        public bool IsDir() => true;
        public object? Sys() => null;
    }
}

internal static partial class OverlayFiles
{
    // overlayfs.go:227. Strip a prior overlay below a rebasable owner, then rebase it.
    internal static ILayeredFileSystem LayerOverlayFileSystem(IFS fileSystem, Dictionary<PathKey, Overlay>? overlays, TypeScript.Lsp.Protocol.PositionEncodingKind positionEncoding)
    {
        var layer = fileSystem as IRebasableFileSystem;
        var baseFileSystem = layer?.BaseFileSystem() ?? fileSystem;
        if (baseFileSystem is OverlayFS previous)
            baseFileSystem = previous.BaseHost;
        var overlay = new OverlayFS(baseFileSystem, overlays, positionEncoding);
        return layer?.WithBaseFileSystem(overlay) ?? overlay;
    }
}
