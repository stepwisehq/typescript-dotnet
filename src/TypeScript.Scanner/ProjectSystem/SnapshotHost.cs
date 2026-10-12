// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete mapped owner; pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.ContentMapper;
using TypeScript.Vfs;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem.Logging;
using TypeScript.LanguageServices.Utilities;

namespace TypeScript.ProjectSystem;
internal sealed partial class SnapshotHost
{
    internal SessionOptions options = null!;
    internal CaseSensitivity caseSensitivity;
    internal IFS fs = null!;
    internal RefCountCache<ParseCacheKey, SourceFile, IFileHandle> parseCache = null!;
    internal RefCountCache<ContentMappedParseCacheKey, SourceFiles, ValueTuple> contentMappedParseCache = null!;
    internal OwnerCache<PathKey, ExtendedConfigCacheEntry, ExtendedConfigParseArgs> extendedConfigCache = null!;
    internal ProgramCounter programCounter = null!;
    internal IHost? contentMapperHost;
    private long snapshotID;
    // snapshothost.go:52. Atomic uint64 wrap semantics use the same bits.
    internal ulong NextSnapshotID() => unchecked((ulong)Interlocked.Increment(ref snapshotID));
    // snapshothost.go:56. Acquire retains the original cache key and source owner.
    internal SourceFileLease AcquireSourceFile(SourceFileParseOptions options, ByteStringValue text, ScriptKind kind)
    {
        var handle = OverlayFiles.NewCachedFileHandle(RootedFilePath.FromValue(options.FileNameValue), text);
        var key = ParseCacheKey.New(options, handle.Hash(), kind);
        return new()
        {
            cache = parseCache,
            key = key,
            sourceFile = parseCache.Acquire(key, handle)
        };
    }

    // snapshothost.go:66. Missing cache owner remains nil, never a new parse.
    internal SourceFileLease? AcquireExistingSourceFile(ParseCacheKey key)
    {
        var(source, found) = parseCache.AcquireExisting(key);
        return found ? new()
        {
            cache = parseCache,
            key = key,
            sourceFile = source
        }

        : null;
    }

    // snapshothost.go:80. Actual optional cache injection and process trust gate.
    internal static SnapshotHost NewSnapshotHost(SessionInit init) => new()
    {
        options = init.Options,
        caseSensitivity = init.FS.CaseSensitivity(),
        fs = init.FS,
        parseCache = init.ParseCache ?? ParseCaches.NewParseCache(default),
        contentMappedParseCache = init.ContentMappedParseCache ?? ParseCaches.NewContentMappedParseCache(default),
        extendedConfigCache = ExtendedConfigCaches.NewExtendedConfigCache(),
        programCounter = new(),
        contentMapperHost = Session.NewContentMapperHost(init)
    };
    // snapshothost.go:105.
    internal Snapshot NewRootSnapshot() => NewRootSnapshot(0, false);
    // snapshothost.go:110.
    internal void RetainSnapshot(Snapshot snapshot) => snapshot.Ref();
    // snapshothost.go:116. Detached update never adopts canonical Session state.
    internal (Snapshot Snapshot, Exception? Error) CloneSnapshot(ProtocolRequestContext context, Snapshot baseline, FileChangeSummary changes, APISnapshotRequest? request)
    {
        SnapshotChange change = new()
        {
            apiRequest = request,
            fileChanges = changes
        };
        if (request is not null)
        {
            change.fs = request.FileSystem;
            change.fileSystemOverride = request.FileSystem is not null;
            change.replaceFileSystem = request.ReplaceFileSystem;
            change.newConfig = request.UserPreferences;
            if (request.PrepareAutoImports.Value.Length != 0)
            {
                change.ResourceRequest = baseline.ResourceRequestForDocument(request.PrepareAutoImports);
                change.ResourceRequest.AutoImports = request.PrepareAutoImports;
            }
        }

        var snapshot = Update(context, baseline, change);
        return (snapshot, snapshot.apiError);
    }

    // snapshothost.go:146.
    internal Snapshot Update(ProtocolRequestContext context, Snapshot baseline, SnapshotChange change) => baseline.Clone(context, change, baseline.Overlays(), null, null);
    // snapshothost.go:152. Real autoimport preparation belongs to clone ownership.
    internal Snapshot CloneSnapshotWithAutoImports(ProtocolRequestContext context, Snapshot baseline, DocumentUri uri, ILogger? logger)
    {
        SnapshotChange change = new()
        {
            reason = UpdateReason.RequestedLanguageServiceWithAutoImports,
            fs = baseline.fs.fs,
            fileSystemOverride = baseline.fileSystemOverride,
            ResourceRequest = baseline.ResourceRequestForDocument(uri)
        };
        change.ResourceRequest.AutoImports = uri;
        return baseline.Clone(context, change, baseline.Overlays(), logger, null);
    }

    // snapshothost.go:163. Original watcher closure has one owner per root snapshot.
    internal Snapshot NewRootSnapshot(ulong id, bool relativePatternSupport)
    {
        var overlays = new OverlayFS(fs, null, options.PositionEncoding);
        var watch = new WatchedFiles<Dictionary<PathKey, RootedDirectoryPath>>(ByteStringValue.FromString("auto-import"), WatchKind.Create | WatchKind.Change | WatchKind.Delete, relativePatternSupport, static directories =>
        {
            var patterns = Slice<ByteStringValue>.Make(0, directories.Count);
            foreach (var directory in directories.Values)
                patterns = patterns.Append(ProjectWatches.GetRecursiveGlobPattern(directory));
            GoSlices.SortFunc(patterns, static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
            return new PatternsAndIgnored
            {
                PatternsInsideWorkspace = patterns
            };
        });
        return NewSnapshot(id, new SnapshotFS(caseSensitivity, overlays, null, null, null), new ConfigFileRegistry(), null, LsUtil.NewDefaultUserPreferences(), null, watch);
    }

    // snapshothost.go:198–206.
    internal IFS FS() => fs;
    internal RootedDirectoryPath GetCurrentDirectory() => options.CurrentDirectory;
    internal RootedDirectoryPath DefaultLibraryPath() => options.DefaultLibraryPath;
    // snapshothost.go:210.
    internal void Close()
    {
        if (contentMapperHost is not null)
            _ = contentMapperHost.Close();
    }
}

// internal/project/snapshothost.go:34–49. The lease retains the exact cache/key owner.
internal sealed class SourceFileLease
{
    internal RefCountCache<ParseCacheKey, SourceFile, IFileHandle> cache = null!;
    internal ParseCacheKey key;
    internal SourceFile sourceFile = null!;
    private readonly ProjectOnce releaseOnce = new();
    internal SourceFile SourceFile() => sourceFile;
    internal void Release() => releaseOnce.Do(() => cache.Deref(key));
}
