// Copyright (c) Microsoft Corporation. All rights reserved.
// Pinned internal/lsp/lspwatcher/lspwatcher.go:223–385 subscription lifecycle.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using TypeScript.Lsp.Protocol;
using FsWatchApi = TypeScript.FsWatch.FsWatch;

namespace TypeScript.Lsp.Watching;
internal sealed partial class Watcher
{
    private sealed class Watch(Watcher watcher, RootedDirectoryPath requestedDirectory, WatchKind kind, bool recursive)
    {
        private readonly object mu = new();
        private IWatch? subscription;
        private RootedDirectoryPath watchedDirectory;
        private bool watchingTarget;
        private bool closed;
        internal void Close()
        {
            IWatch? previous;
            lock (mu)
            {
                closed = true;
                previous = subscription;
                subscription = null;
                watchedDirectory = default;
            }

            if (previous is not null)
                _ = previous.Close();
        }

        internal Exception? Reconcile(bool emitSyntheticCreates)
        {
            lock (mu)
            {
                while (true)
                {
                    if (closed)
                        return null;
                    if (watcher.fs.DirectoryExists(requestedDirectory))
                    {
                        if (watchingTarget && subscription is not null)
                            return null;
                        var target = requestedDirectory;
                        Slice<IWatchOption> options = default;
                        if (recursive)
                            options = options.Append(FsWatchApi.WithRecursive());
                        var(next, error) = watcher.backend.WatchDirectory(target, TargetCallback(target), options);
                        if (error is not null)
                            return error;
                        var previous = subscription;
                        subscription = next;
                        watchedDirectory = target;
                        watchingTarget = true;
                        if (previous is not null)
                            _ = previous.Close();
                        if (emitSyntheticCreates)
                            watcher.EmitSyntheticCreates(target, kind, recursive);
                        return null;
                    }

                    var(ancestor, ok) = NearestExistingAncestor(watcher.fs, requestedDirectory);
                    if (!ok)
                    {
                        if (subscription is not null)
                        {
                            var previous = subscription;
                            subscription = null;
                            watchedDirectory = default;
                            watchingTarget = false;
                            _ = previous.Close();
                        }

                        return null;
                    }

                    if (!watchingTarget && subscription is not null && watchedDirectory == ancestor)
                        return null;
                    var(nextAncestor, ancestorError) = watcher.backend.WatchDirectory(ancestor, AncestorCallback());
                    if (ancestorError is not null)
                        return ancestorError;
                    var prior = subscription;
                    subscription = nextAncestor;
                    watchedDirectory = ancestor;
                    watchingTarget = false;
                    if (prior is not null)
                        _ = prior.Close();
                    // Recheck after arming the ancestor: missing→present races
                    // must synthesize creates rather than lose the gap's files.
                    emitSyntheticCreates = true;
                }
            }
        }

        private WatchCallback TargetCallback(RootedDirectoryPath directory) => (events, error) =>
        {
            bool terminated = false;
            if (error is not null)
            {
                if (FsWatchApi.IsError(error, FsWatchApi.ErrOverflow))
                    watcher.logger.Logf("lspwatcher: watch overflow in %q (some events may have been dropped): %v", directory, error);
                else if (FsWatchApi.IsError(error, FsWatchApi.ErrWatchTerminated))
                {
                    terminated = true;
                    watcher.logger.Logf("lspwatcher: watch terminated in %q (directory removed): %v", directory, error);
                }
                else
                    watcher.logger.Logf("lspwatcher: watch error in %q: %v", directory, error);
            }

            if (events.Count > 0)
                watcher.ForwardEvents(kind, events);
            if (terminated)
                HandleTerminated();
        };
        private void HandleTerminated()
        {
            IWatch? previous;
            lock (mu)
            {
                if (closed)
                    return;
                previous = subscription;
                subscription = null;
                watchedDirectory = default;
                watchingTarget = false;
            }

            if (previous is not null)
                _ = previous.Close();
            _ = Reconcile(true);
        }

        private WatchCallback AncestorCallback() => (events, error) =>
        {
            _ = Reconcile(true);
        };
    }
}
