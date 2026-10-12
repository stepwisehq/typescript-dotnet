// Copyright (c) Microsoft Corporation. All rights reserved.
// Pinned internal/fswatch/event.go; see licenses/FsWatch-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal enum EventKind
{
    Update = 1,
    Delete = 2
}

internal static partial class FsWatch
{
    internal static ByteStringValue EventKindString(EventKind kind) => ByteStringValue.FromString(kind switch
    {
        EventKind.Update => "update",
        EventKind.Delete => "delete",
        _ => "unknown",
    });
}

internal struct WatchEvent
{
    internal EventKind Kind;
    internal ByteStringValue Path;
    internal bool IncludedWatchRoot;
}

internal sealed class EventEntry
{
    internal ulong CreatedSeq, UpdatedSeq, DeletedSeq;
    internal bool IncludedWatchRoot;
    internal bool IsDeleted() => DeletedSeq > CreatedSeq && DeletedSeq > UpdatedSeq;
    internal (EventKind Kind, bool Ok) KindSince(ulong startSeq)
    {
        if (DeletedSeq > startSeq)
        {
            if (CreatedSeq > startSeq && CreatedSeq < DeletedSeq && UpdatedSeq < DeletedSeq)
                return default;
            return (EventKind.Delete, true);
        }

        var sequence = Math.Max(CreatedSeq, UpdatedSeq);
        return sequence > startSeq ? (EventKind.Update, true) : default;
    }
}

internal sealed class EventList
{
    private readonly object mu = new();
    private Dictionary<ByteStringKey, EventEntry>? entries;
    private Exception? error;
    private ulong seq;
    internal void Create(ByteStringValue path)
    {
        lock (mu)
        {
            var sequence = NextSeqLocked();
            CreateLocked(path, sequence);
        }
    }

    internal void CreateAt(ByteStringValue path, ulong sequence)
    {
        lock (mu)
        {
            AdvanceSeqLocked(sequence);
            CreateLocked(path, sequence);
        }
    }

    private void CreateLocked(ByteStringValue path, ulong sequence)
    {
        var entry = GetOrCreate(path);
        if (entry.IsDeleted())
        {
            entry.DeletedSeq = 0;
            entry.CreatedSeq = 0;
            entry.UpdatedSeq = sequence;
        }
        else
            entry.CreatedSeq = sequence;
    }

    internal void Update(ByteStringValue path)
    {
        lock (mu)
        {
            var sequence = NextSeqLocked();
            UpdateLocked(path, sequence);
        }
    }

    internal void UpdateAt(ByteStringValue path, ulong sequence)
    {
        lock (mu)
        {
            AdvanceSeqLocked(sequence);
            UpdateLocked(path, sequence);
        }
    }

    internal void UpdateWatchRootAt(ByteStringValue path, ulong sequence)
    {
        lock (mu)
        {
            AdvanceSeqLocked(sequence);
            UpdateLocked(path, sequence);
            GetOrCreate(path).IncludedWatchRoot = true;
        }
    }

    private void UpdateLocked(ByteStringValue path, ulong sequence) => GetOrCreate(path).UpdatedSeq = sequence;
    internal void Remove(ByteStringValue path)
    {
        lock (mu)
        {
            var sequence = NextSeqLocked();
            RemoveLocked(path, sequence);
        }
    }

    internal ulong RemoveAndGetSequence(ByteStringValue path)
    {
        lock (mu)
        {
            var sequence = NextSeqLocked();
            RemoveLocked(path, sequence);
            return sequence;
        }
    }

    internal void RemoveAt(ByteStringValue path, ulong sequence)
    {
        lock (mu)
        {
            AdvanceSeqLocked(sequence);
            RemoveLocked(path, sequence);
        }
    }

    internal void RemoveWatchRootAt(ByteStringValue path, ulong sequence)
    {
        lock (mu)
        {
            AdvanceSeqLocked(sequence);
            RemoveLocked(path, sequence);
            GetOrCreate(path).IncludedWatchRoot = true;
        }
    }

    private void RemoveLocked(ByteStringValue path, ulong sequence) => GetOrCreate(path).DeletedSeq = sequence;
    internal int Size()
    {
        lock (mu)
            return entries?.Count ?? 0;
    }

    private Slice<WatchEvent> SnapshotLocked() => SnapshotSinceLocked(0);
    private Slice<WatchEvent> SnapshotSinceLocked(ulong startSequence)
    {
        var output = Slice<WatchEvent>.Make(0, entries?.Count ?? 0);
        if (entries is not null)
            foreach (var(path, entry)in entries)
            {
                var(kind, ok) = entry.KindSince(startSequence);
                if (!ok)
                    continue;
                output = output.Append(new WatchEvent { Kind = kind, Path = path.Value, IncludedWatchRoot = entry.IncludedWatchRoot });
            }

        return output;
    }

    internal Slice<WatchEvent> GetEvents()
    {
        lock (mu)
            return SnapshotLocked();
    }

    internal (Slice<WatchEvent> Events, Exception? Error) Drain()
    {
        lock (mu)
        {
            var output = SnapshotLocked();
            var resultError = error;
            entries = null;
            error = null;
            return (output, resultError);
        }
    }

    internal (Slice<Slice<WatchEvent>> Events, Exception? Error) DrainForSequences(Slice<ulong> startSequences)
    {
        lock (mu)
        {
            var output = Slice<Slice<WatchEvent>>.Make(startSequences.Count);
            for (var i = 0; i < startSequences.Count; i++)
                output[i] = SnapshotSinceLocked(startSequences[i]);
            var resultError = error;
            entries = null;
            error = null;
            return (output, resultError);
        }
    }

    internal void SetError(Exception? value)
    {
        lock (mu)
            error ??= value;
    }

    internal bool HasError()
    {
        lock (mu)
            return error is not null;
    }

    internal Exception? GetError()
    {
        lock (mu)
            return error;
    }

    private EventEntry GetOrCreate(ByteStringValue path)
    {
        entries ??= new();
        var key = ByteStringKey.FromValue(path);
        if (entries.TryGetValue(key, out var entry))
            return entry;
        entry = new();
        entries.Add(key, entry);
        return entry;
    }

    internal ulong Sequence()
    {
        lock (mu)
            return seq;
    }

    private ulong NextSeqLocked() => seq = unchecked(seq + 1);
    private void AdvanceSeqLocked(ulong sequence)
    {
        if (sequence > seq)
            seq = sequence;
    }
}
