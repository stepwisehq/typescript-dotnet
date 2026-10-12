// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete original watcher_test.go file/directory CRUD assertions.
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    private static void FileAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error)when (error is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(error.Message, error);
        }
    }

    private static void WriteFile(string path, string content) => FileAction(() => File.WriteAllText(path, content));
    private static void MakeDirectory(string path) => FileAction(() => Directory.CreateDirectory(path));
    public static void TestWatchFileCreate() => RunForEachWatcher(nameof(TestWatchFileCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(dir);
        WriteFile(file, "hello");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestWatchFileUpdate() => RunForEachWatcher(nameof(TestWatchFileUpdate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(dir);
        WriteFile(file, "v1");
        _ = recorder.WaitForEvent(recorder.Deadline(), static _ => true);
        WriteFile(file, "v2-longer");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestWatchFileRename() => RunForEachWatcher(nameof(TestWatchFileRename), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), first = UniqueName(dir), second = UniqueName(dir);
        WriteFile(first, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => File.Move(first, second));
        ExpectEventSet(recorder, new WantEvent(EventKind.Delete, Text(first)), new WantEvent(EventKind.Update, Text(second)));
    });
    public static void TestWatchFileRenameExisting() => RunForEachWatcher(nameof(TestWatchFileRenameExisting), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), first = UniqueName(dir);
        WriteFile(first, "hi");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string second = UniqueName(dir);
        FileAction(() => File.Move(first, second));
        ExpectEventSet(recorder, new WantEvent(EventKind.Delete, Text(first)), new WantEvent(EventKind.Update, Text(second)));
    });
    public static void TestWatchFileDelete() => RunForEachWatcher(nameof(TestWatchFileDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), file = UniqueName(dir);
        WriteFile(file, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => File.Delete(file));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(file)));
    });
    public static void TestSubscribeDirCreate() => RunForEachWatcher(nameof(TestSubscribeDirCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string child = UniqueName(dir);
        MakeDirectory(child);
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(child)));
    });
    public static void TestSubscribeNonASCIIPath() => RunForEachWatcher(nameof(TestSubscribeNonASCIIPath), (fixture, watcher) =>
    {
        string parent = fixture.NewTmpDir(), dir = Path.Combine(parent, "café-dir");
        MakeDirectory(dir);
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string child = Path.Combine(dir, "résumé.txt");
        WriteFile(child, "hi");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(child)));
    });
    public static void TestSubscribeDirRename() => RunForEachWatcher(nameof(TestSubscribeDirRename), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), first = UniqueName(dir);
        MakeDirectory(first);
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string second = UniqueName(dir);
        FileAction(() => Directory.Move(first, second));
        ExpectEventSet(recorder, new WantEvent(EventKind.Delete, Text(first)), new WantEvent(EventKind.Update, Text(second)));
    });
    public static void TestSubscribeDirDelete() => RunForEachWatcher(nameof(TestSubscribeDirDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), child = UniqueName(dir);
        MakeDirectory(child);
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(child, recursive: true));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(child)));
    });
    public static void TestSubscribeWatchedDirDeleted() => RunForEachWatcher(nameof(TestSubscribeWatchedDirDeleted), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(dir, recursive: true));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(dir)));
        long start = Stopwatch.GetTimestamp();
        var deadline = recorder.Deadline();
        while (Stopwatch.GetElapsedTime(start) < deadline)
        {
            lock (recorder.Gate)
                if (recorder.Errors.Length != 0)
                    break;
            Thread.Sleep(20);
        }

        Slice<Exception> errors;
        lock (recorder.Gate)
        {
            errors = GoSlices.Clone(recorder.Errors);
            recorder.Errors = default;
        }

        bool terminated = false;
        foreach (var error in errors)
            if (FsWatch.IsError(error, FsWatch.ErrWatchTerminated))
            {
                terminated = true;
                break;
            }

        Check(terminated, "expected ErrWatchTerminated after watched dir deletion");
        MakeDirectory(dir);
        Check(recorder.DrainQuiet(TimeSpan.FromMilliseconds(200)).Length == 0, "expected no follow-up events for stale watch");
    });
}
