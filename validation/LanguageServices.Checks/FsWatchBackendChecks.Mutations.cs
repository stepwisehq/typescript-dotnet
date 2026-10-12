// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:1978–2180 assertions and real eight-worker concurrency.
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestSubscribeDeepNestedCreate() => RunForEachWatcher(nameof(TestSubscribeDeepNestedCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string a = Path.Combine(dir, "a"), b = Path.Combine(a, "b"), c = Path.Combine(b, "c");
        foreach (string directory in new[]
        {
            a,
            b,
            c
        }

        )
        {
            MakeDirectory(directory);
            Thread.Sleep(150);
        }

        string file = Path.Combine(c, "deep.txt");
        WriteFile(file, "deep");
        WantEvent[] expected = [new WantEvent(EventKind.Update, Text(a)), new WantEvent(EventKind.Update, Text(file))];
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        foreach (var value in expected)
            Check(Contains(actual, value.Kind, value.Path), "expected nested create event");
    });
    public static void TestSubscribeManyFilesAtOnce() => RunForEachWatcher(nameof(TestSubscribeManyFilesAtOnce), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        const int count = 50;
        var paths = new string[count];
        var expected = new WantEvent[count];
        for (int index = 0; index < count; index++)
        {
            paths[index] = UniqueName(dir);
            WriteFile(paths[index], "x");
        }

        for (int index = 0; index < count; index++)
            expected[index] = new WantEvent(EventKind.Update, Text(paths[index]));
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        for (int attempt = 0; attempt < 3 && !HaveAll(actual, expected); attempt++)
        {
            foreach (string path in paths)
                if (!Contains(actual, EventKind.Update, Text(path)))
                {
                    // Original retry ignores os.WriteFile error; keep that error disposition.
                    try
                    {
                        File.WriteAllText(path, "x");
                    }
                    catch (Exception error)when (error is IOException or UnauthorizedAccessException)
                    {
                    }
                }

            actual = actual.Append(recorder.WaitForAll(recorder.Deadline(), expected));
        }

        foreach (string path in paths)
            Check(Contains(actual, EventKind.Update, Text(path)), "missing create for " + path);
    });
    public static void TestSubscribeTruncateFile() => RunForEachWatcher(nameof(TestSubscribeTruncateFile), (fixture, watcher) =>
    {
        string file = UniqueName(fixture.NewTmpDir());
        WriteFile(file, "hello world");
        var(recorder, _) = fixture.Subscribe(watcher, Path.GetDirectoryName(file)!);
        FileAction(() =>
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Write);
            stream.SetLength(0);
        });
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestSubscribeConcurrentSubscribeUnsubscribe() => RunForEachWatcher(nameof(TestSubscribeConcurrentSubscribeUnsubscribe), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var workers = new Thread[8];
        for (int index = 0; index < workers.Length; index++)
        {
            workers[index] = new Thread(() =>
            {
                var recorder = new Recorder
                {
                    Attempt = fixture.Attempt
                };
                var(watch, error) = watcher.WatchDirectory(Text(dir), recorder.Callback);
                if (error is not null)
                    return;
                _ = watch!.Close();
            });
            workers[index].Start();
        }

        foreach (var worker in workers)
            worker.Join();
    });
    public static void TestSubscribeRenameDir() => RunForEachWatcher(nameof(TestSubscribeRenameDir), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), before = Path.Combine(dir, "before");
        MakeDirectory(before);
        WriteFile(Path.Combine(before, "file.txt"), "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string after = Path.Combine(dir, "after");
        FileAction(() => Directory.Move(before, after));
        WantEvent[] expected = [new WantEvent(EventKind.Update, Text(after)), new WantEvent(EventKind.Delete, Text(before))];
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        foreach (var value in expected)
            Check(Contains(actual, value.Kind, value.Path), "expected renamed directory event");
    });
    public static void TestSubscribeReplaceFileWithDir() => RunForEachWatcher(nameof(TestSubscribeReplaceFileWithDir), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), target = UniqueName(dir);
        WriteFile(target, "file");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => File.Delete(target));
        MakeDirectory(target);
        var actual = recorder.WaitForEvent(recorder.Deadline(), value => value.Path.EqualsValue(Text(target)));
        Check(Contains(actual, EventKind.Delete, Text(target)) || Contains(actual, EventKind.Update, Text(target)), "expected events for file-to-dir replacement");
    });
    public static void TestSubscribeAppendToFile() => RunForEachWatcher(nameof(TestSubscribeAppendToFile), (fixture, watcher) =>
    {
        string file = UniqueName(fixture.NewTmpDir());
        WriteFile(file, "initial");
        var(recorder, _) = fixture.Subscribe(watcher, Path.GetDirectoryName(file)!);
        FileStream? stream = null;
        FileAction(() => stream = new FileStream(file, FileMode.Open, FileAccess.Write));
        // Original OpenFile errors fail, but WriteString and Close errors are ignored.
        try
        {
            stream!.Seek(0, SeekOrigin.End);
            stream.Write(" appended"u8);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        finally
        {
            try { stream!.Dispose(); } catch (IOException) { }
        }
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestSubscribeNoEventsAfterUnsubscribe() => RunForEachWatcher(nameof(TestSubscribeNoEventsAfterUnsubscribe), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, watch) = fixture.Subscribe(watcher, dir);
        Check(watch.Close()is null, "closeWatch failed");
        WriteFile(UniqueName(dir), "x");
        Check(recorder.DrainQuiet(TimeSpan.FromMilliseconds(500)).Length == 0, "expected no events after closeWatch");
    });
}
