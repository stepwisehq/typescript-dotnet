// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/core/bfs.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;

namespace TypeScript.Scanner;
internal readonly record struct BreadthFirstSearchResult<N>(bool Stopped, Slice<N> Path);
internal sealed class BreadthFirstSearchJob<N>(N node, BreadthFirstSearchJob<N>? parent = null)
{
    internal readonly N Node = node;
    internal readonly BreadthFirstSearchJob<N>? Parent = parent;
}

internal sealed class BreadthFirstSearchLevel<K, N>(OrderedMap<K, BreadthFirstSearchJob<N>> jobs)
    where K : notnull
{
    // bfs.go:25/29/33.
    internal bool Has(K key) => jobs.Has(key);
    internal void Delete(K key) => jobs.Delete(key);
    internal void Range(Func<N, bool> action)
    {
        foreach (var entry in jobs.EntryValues())
            if (!action(entry.Value.Node))
                return;
    }
}

internal readonly record struct BreadthFirstSearchOptions<K, N>(SyncSet<K>? Visited = null, Action<BreadthFirstSearchLevel<K, N>>? PreprocessLevel = null)
    where K : notnull;
internal static partial class Core
{
    // bfs.go:53.
    internal static BreadthFirstSearchResult<N> BreadthFirstSearchParallel<N>(N start, Func<N, Slice<N>> neighbors, Func<N, (bool IsResult, bool Stop)> visit)
        where N : notnull => BreadthFirstSearchParallelEx(start, neighbors, visit, new BreadthFirstSearchOptions<N, N>(), static node => node);
    // bfs.go:64. Ordered levels preserve the lowest result index despite parallel completion.
    internal static BreadthFirstSearchResult<N> BreadthFirstSearchParallelEx<K, N>(N start, Func<N, Slice<N>> neighbors, Func<N, (bool IsResult, bool Stop)> visit, BreadthFirstSearchOptions<K, N> options, Func<N, K> getKey)
        where K : notnull
    {
        var visited = options.Visited ?? new SyncSet<K>();
        BreadthFirstSearchJob<N>? fallback = null;
        (bool Stop, BreadthFirstSearchJob<N>? Job, OrderedMap<K, BreadthFirstSearchJob<N>>? Next) ProcessLevel(int levelIndex, OrderedMap<K, BreadthFirstSearchJob<N>> jobs)
        {
            long lowestFallback = long.MaxValue, lowestGoal = long.MaxValue, nextJobCount = 0;
            options.PreprocessLevel?.Invoke(new(jobs));
            var next = Slice<Slice<BreadthFirstSearchJob<N>>>.Make(jobs.Size());
            var group = NewWorkGroup(false);
            int index = 0;
            foreach (var entry in jobs.EntryValues())
            {
                int i = index++;
                var job = entry.Value;
                group.Queue(() =>
                {
                    if (i >= Volatile.Read(ref lowestGoal))
                        return;
                    if (!visited.AddIfAbsent(getKey(job.Node)))
                        return;
                    var(isResult, stop) = visit(job.Node);
                    if (isResult)
                    {
                        if (stop)
                        {
                            UpdateSearchMin(ref lowestGoal, i);
                            return;
                        }

                        if (fallback == null)
                            UpdateSearchMin(ref lowestFallback, i);
                    }

                    if (i >= Volatile.Read(ref lowestGoal))
                        return;
                    var children = neighbors(job.Node);
                    if (children.Count > 0)
                    {
                        Interlocked.Add(ref nextJobCount, children.Count);
                        var childJobs = Slice<BreadthFirstSearchJob<N>>.Make(children.Count);
                        for (int childIndex = 0; childIndex < children.Count; childIndex++)
                            childJobs[childIndex] = new(children[childIndex], job);
                        next[i] = childJobs;
                    }
                });
            }

            group.RunAndWait();
            if (lowestGoal != long.MaxValue)
                return (true, jobs.EntryAt(checked((int)lowestGoal)).Value, null);
            if (fallback == null && lowestFallback != long.MaxValue)
                fallback = jobs.EntryAt(checked((int)lowestFallback)).Value;
            var nextJobs = new OrderedMap<K, BreadthFirstSearchJob<N>>(checked((int)nextJobCount));
            foreach (var children in next)
                foreach (var child in children)
                    if (!nextJobs.Has(getKey(child.Node)))
                        nextJobs.Set(getKey(child.Node), child);
            return (false, null, nextJobs);
        }

        static Slice<N> CreatePath(BreadthFirstSearchJob<N>? job)
        {
            Slice<N> path = default;
            for (; job != null; job = job.Parent)
                path = path.Append(job.Node);
            return path;
        }

        int levelIndex = 0;
        var level = new OrderedMap<K, BreadthFirstSearchJob<N>>(1);
        level.Set(getKey(start), new(start));
        while (level.Size() > 0)
        {
            var result = ProcessLevel(levelIndex, level);
            if (result.Stop)
                return new(true, CreatePath(result.Job));
            if (result.Job != null && fallback == null)
                fallback = result.Job;
            level = result.Next!;
            levelIndex++;
        }

        return new(false, CreatePath(fallback));
    }

    // bfs.go:196. Equal candidate values still perform the original CAS.
    private static bool UpdateSearchMin(ref long value, long candidate)
    {
        while (true)
        {
            long current = Volatile.Read(ref value);
            if (current < candidate)
                return false;
            if (Interlocked.CompareExchange(ref value, candidate, current) == current)
                return true;
        }
    }
}
