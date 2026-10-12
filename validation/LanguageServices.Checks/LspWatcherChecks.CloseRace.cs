// Copyright (c) Microsoft Corporation. All rights reserved.
// Original lspwatcher_test.go:831–888 explicit concurrent close/reconcile assertion.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using TypeScript.Lsp.Watching;
using LspWatcher = TypeScript.Lsp.Watching.Watcher;
using LspWatchCallback = TypeScript.Lsp.Watching.WatchCallback;

namespace LanguageServices.Checks;
public static partial class LspWatcherChecks
{
    private sealed class BlockingBackend : IWatcherBackend
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public (IWatch? Subscription, Exception? Error) WatchDirectory(RootedDirectoryPath directory, LspWatchCallback callback, Slice<IWatchOption> options = default)
        {
            Entered.SetResult();
            Release.Task.GetAwaiter().GetResult();
            return (new FakeWatch(static () => null), null);
        }
    }

    private static void TestWatcher_CloseWhileWatchFilesReconciles()
    {
        using var directory = new DirectoryOwner();
        var backend = new BlockingBackend();
        var watcher = LspWatcher.NewWithBackend(Fs, backend, static _ =>
        {
        }, TypeScript.ProjectSystem.Logging.Logger.NewLogger(Console.OpenStandardError()));
        var done = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = Requests(directory.Root + "/**/*");
        new Thread(() => done.SetResult(watcher.WatchFiles(Text("id"), request)))
        {
            IsBackground = true
        }.Start();
        backend.Entered.Task.GetAwaiter().GetResult();
        watcher.Close();
        backend.Release.SetResult();
        Check(done.Task.GetAwaiter().GetResult()is not null, "expected WatchFiles to report that the watcher was closed");
    }
}
