// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/filechange.go counterpart at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem;
internal interface IFileChangeExpander
{
    FileChangeSummary ExpandFileChanges(FileChangeSummary summary);
}

internal enum FileChangeKind
{
    Open,
    Close,
    Change,
    Save,
    WatchCreate,
    WatchChange,
    WatchDelete
}

internal static class FileChanges
{
    internal const int ExcessiveChangeThreshold = 1000;
    // filechange.go:26. Only filesystem notification kinds count as watch events.
    internal static bool IsWatchKind(this FileChangeKind kind) => kind is FileChangeKind.WatchCreate or FileChangeKind.WatchChange or FileChangeKind.WatchDelete;
    // filechange.go:74. Open/close/reopen belong to ordered document changes;
    // this merge combines filesystem invalidation sets only.
    internal static void MergeFileChangeSummary(ref FileChangeSummary destination, FileChangeSummary source)
    {
        if (source.IsEmpty())
            return;
        if (source.InvalidateAll)
            destination.InvalidateAll = true;
        AddAll(ref destination.Changed, source.Changed);
        AddAll(ref destination.Created, source.Created);
        AddAll(ref destination.Deleted, source.Deleted);
        if (source.IncludesWatchChangeOutsideNodeModules)
            destination.IncludesWatchChangeOutsideNodeModules = true;
    }

    private static void AddAll(ref HashSet<DocumentUri>? target, HashSet<DocumentUri>? source)
    {
        if (source is null || source.Count == 0)
            return;
        target ??= new();
        foreach (var uri in source)
            target.Add(uri);
    }
}

internal readonly record struct FileChange(FileChangeKind Kind, DocumentUri URI, int Version = 0, ByteStringValue Content = default, LanguageKind LanguageKind = default, Slice<TextDocumentContentChangePartialOrWholeDocument> Changes = default);
// Go struct snapshots retain set owners until Clone; nullable HashSet represents
// the nil, zero-length map and is allocated only on actual insertion.
internal struct FileChangeSummary
{
    internal DocumentUri Opened;
    internal DocumentUri Reopened;
    internal HashSet<DocumentUri>? Closed;
    internal HashSet<DocumentUri>? Changed;
    internal HashSet<DocumentUri>? Created;
    internal HashSet<DocumentUri>? Deleted;
    internal bool IncludesWatchChangeOutsideNodeModules;
    internal bool InvalidateAll;
    // filechange.go:52. Clone every map owner, preserving nil and key equality.
    internal readonly FileChangeSummary Clone() => new()
    {
        Opened = Opened,
        Reopened = Reopened,
        Closed = Copy(Closed),
        Changed = Copy(Changed),
        Created = Copy(Created),
        Deleted = Copy(Deleted),
        IncludesWatchChangeOutsideNodeModules = IncludesWatchChangeOutsideNodeModules,
        InvalidateAll = InvalidateAll,
    };
    private static HashSet<DocumentUri>? Copy(HashSet<DocumentUri>? source) => source is null ? null : new(source, source.Comparer);
    // filechange.go:60.
    internal readonly bool IsEmpty() => !InvalidateAll && Opened.Value.Length == 0 && Reopened.Value.Length == 0 && (Closed?.Count ?? 0) == 0 && (Changed?.Count ?? 0) == 0 && (Created?.Count ?? 0) == 0 && (Deleted?.Count ?? 0) == 0;
    // filechange.go:64.
    internal readonly bool HasExcessiveWatchEvents() => InvalidateAll || (Created?.Count ?? 0) + (Deleted?.Count ?? 0) + (Changed?.Count ?? 0) > FileChanges.ExcessiveChangeThreshold;
    // filechange.go:68.
    internal readonly bool HasExcessiveNonCreateWatchEvents() => InvalidateAll || (Deleted?.Count ?? 0) + (Changed?.Count ?? 0) > FileChanges.ExcessiveChangeThreshold;
}
