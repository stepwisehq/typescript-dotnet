// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/progress.go worker and reporter contract. Draft outside compilation.
using System.Threading.Channels;
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using DiagnosticMessage = TypeScript.Scanner.Message;

namespace TypeScript.Lsp;
internal interface IProgressReporter
{
    CancellationToken Done { get; }

    ByteStringValue Localize(DiagnosticMessage message, params object? [] arguments);
    void CreateWorkDoneProgress(ByteStringValue token);
    void SendProgress(ByteStringValue token, WorkDoneProgressBeginOrReportOrEnd value);
}

// progress.go:38–61, actual Server transport/locale adaptation.
internal sealed class ServerProgressReporter(Server server) : IProgressReporter
{
    public CancellationToken Done => server.backgroundCtx;

    public ByteStringValue Localize(DiagnosticMessage message, params object? [] arguments) => ByteStringValue.FromString(message.Localize(server.GetLocale(), arguments));
    public void CreateWorkDoneProgress(ByteStringValue token) => server.SendClientRequestFireAndForget(Method.WindowWorkDoneProgressCreateInfo, new WorkDoneProgressCreateParams { Token = new IntegerOrString { String = token } });
    public void SendProgress(ByteStringValue token, WorkDoneProgressBeginOrReportOrEnd value) => server.SendNotification(Method.ProgressInfo, new ProgressParams { Token = new IntegerOrString { String = token }, Value = value });
}

internal sealed class ProjectLoadingProgress
{
    internal readonly record struct ProgressEvent(DiagnosticMessage Message, object? [] Arguments, bool Finish);
    private readonly IProgressReporter reporter;
    internal readonly Channel<ProgressEvent> channel;
    private readonly TimeSpan delay;
    private readonly TimeProvider clock;
    internal readonly Task Worker;
    private readonly Action<Task, Task?>? onWait;
    private ProjectLoadingProgress(IProgressReporter reporter, TimeSpan delay, TimeProvider clock, Action<Task, Task?>? onWait)
    {
        this.reporter = reporter;
        this.delay = delay;
        this.clock = clock;
        this.onWait = onWait;
        // Only the explicit synctest adapter admits synchronous continuations: a
        // fixture Send must complete the pending read Task before observing idle.
        // Production keeps the standard asynchronous channel continuation policy.
        channel = Channel.CreateBounded<ProgressEvent>(new BoundedChannelOptions(64) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = onWait is not null });
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Worker = completion.Task;
        ThreadPool.UnsafeQueueUserWorkItem(static state => state.Owner.RunWorker(state.Completion), (Owner: this, Completion: completion), preferLocal: false);
    }

    internal static ProjectLoadingProgress NewProjectLoadingProgress(Server server, TimeSpan delay) => NewFromReporter(new ServerProgressReporter(server), delay);
    internal static ProjectLoadingProgress NewFromReporter(IProgressReporter reporter, TimeSpan delay, TimeProvider? clock = null, Action<Task, Task?>? onWait = null) => new(reporter, delay, clock ?? TimeProvider.System, onWait);
    internal void Start(DiagnosticMessage message, params object? [] arguments) => Send(new(message, arguments, false));
    internal void Finish(DiagnosticMessage message, params object? [] arguments) => Send(new(message, arguments, true));
    private void Send(ProgressEvent value)
    {
        try
        {
            channel.Writer.WriteAsync(value, reporter.Done).AsTask().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)when (reporter.Done.IsCancellationRequested)
        {
        // The source selects the server-done arm and drops the event.
        }
    }

    // Unhandled reporter panic escapes the void worker, matching the Go goroutine.
    private async void RunWorker(TaskCompletionSource completion)
    {
        try
        {
            await Run().ConfigureAwait(false);
        }
        finally
        {
            completion.TrySetResult();
        }
    }

    // One Go timer deadline; bounded CLR waits preserve the full positive range.
    private Task DelayAfter(TimeSpan duration, CancellationToken cancellation)
    {
        var maximum = TimeSpan.FromMilliseconds(int.MaxValue);
        return duration <= maximum ? Task.Delay(duration, clock, cancellation) : DelayLong(duration, maximum, cancellation);
    }

    private async Task DelayLong(TimeSpan duration, TimeSpan maximum, CancellationToken cancellation)
    {
        long start = clock.GetTimestamp();
        while (true)
        {
            var remaining = duration - clock.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero)
                return;
            await Task.Delay(remaining <= maximum ? remaining : maximum, clock, cancellation).ConfigureAwait(false);
        }
    }

    private async Task Run()
    {
        var loading = new OrderedMap<ByteStringKey, long>();
        var token = default(ByteStringValue);
        long tokenID = 0;
        var begun = false;
        CancellationTokenSource? delayCancellation = null;
        Task? delayEvent = null;
        var delayFired = false;
        Task<ProgressEvent>? read = null;
        void StopDelay()
        {
            delayCancellation?.Cancel();
            delayCancellation?.Dispose();
            delayCancellation = null;
            delayEvent = null;
        }

        try
        {
            while (true)
            {
                // Retain one pending read across timer wins. Starting competing
                // reads would orphan-consume the next event, unlike Go select.
                if (read is null)
                {
                    read = channel.Reader.ReadAsync(reporter.Done).AsTask();
                }

                // Optional original synctest adapter observes only the actual wait boundary.
                // It is absent in production; completed reads/timers are not idle.
                onWait?.Invoke(read, delayEvent);
                var winner = delayEvent == null ? read : await Task.WhenAny(read, delayEvent).ConfigureAwait(false);
                if (ReferenceEquals(winner, read))
                {
                    var value = await read.ConfigureAwait(false);
                    read = null;
                    var text = reporter.Localize(value.Message, value.Arguments);
                    var key = ByteStringKey.FromValue(text);
                    if (!value.Finish)
                    {
                        loading.Set(key, loading.GetOrZero(key) + 1);
                        if (token.Length == 0)
                        {
                            tokenID++;
                            token = ByteStringValue.FromString("tsgo-loading-" + tokenID.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            begun = false;
                            if (delay <= TimeSpan.Zero)
                            {
                                delayFired = true;
                                reporter.CreateWorkDoneProgress(token);
                            }
                            else
                            {
                                delayFired = false;
                                delayCancellation = new CancellationTokenSource();
                                delayEvent = DelayAfter(delay, delayCancellation.Token);
                            }
                        }

                        if (delayFired)
                            begun = BeginOrReport(token, text, begun);
                    }
                    else
                    {
                        var count = loading.GetOrZero(key);
                        if (count <= 1)
                            loading.Delete(key);
                        else
                            loading.Set(key, count - 1);
                        if (token.Length == 0)
                            continue;
                        if (loading.Size() == 0)
                        {
                            if (begun)
                                reporter.SendProgress(token, new() { End = new WorkDoneProgressEnd() });
                            StopDelay();
                            token = default;
                        }
                        else if (delayFired)
                        {
                            var first = loading.EntryAt(0).Key.Value;
                            reporter.SendProgress(token, new() { Report = new WorkDoneProgressReport { Message = first } });
                        }
                    }
                }
                else
                {
                    await delayEvent!.ConfigureAwait(false);
                    delayEvent = null;
                    delayFired = true;
                    if (token.Length != 0 && loading.Size() > 0)
                    {
                        reporter.CreateWorkDoneProgress(token);
                        var first = loading.EntryAt(0).Key.Value;
                        begun = BeginOrReport(token, first, begun);
                    }
                }
            }
        }
        catch (OperationCanceledException)when (reporter.Done.IsCancellationRequested)
        {
        // The original worker's shutdown arm stops its timer and returns.
        }
        finally
        {
            StopDelay();
        }
    }

    private bool BeginOrReport(ByteStringValue token, ByteStringValue text, bool begun)
    {
        if (!begun)
            reporter.SendProgress(token, new() { Begin = new WorkDoneProgressBegin { Title = reporter.Localize(Diagnostics.Loading), Message = text } });
        else
            reporter.SendProgress(token, new() { Report = new WorkDoneProgressReport { Message = text } });
        return true;
    }
}
