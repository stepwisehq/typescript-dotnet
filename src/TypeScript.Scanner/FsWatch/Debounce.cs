// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/debounce.go; MIT license in licenses/FsWatch-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed class Debounce
{
    internal static readonly TimeSpan DefaultMinWaitTime = TimeSpan.FromMilliseconds(50);
    internal static readonly TimeSpan DefaultMaxWaitTime = TimeSpan.FromMilliseconds(500);
    internal static TimeSpan MinWaitTime = DefaultMinWaitTime;
    internal static TimeSpan MaxWaitTime = DefaultMaxWaitTime;
    private readonly object mu = new();
    private readonly Dictionary<object, Action> callbacks = new();
    private long? lastTime;
    private readonly object latchMu = new();
    private TaskCompletionSource? waitCh;
    private TaskCompletionSource? triggerCh;
    private bool notified;
    private readonly TimeProvider time;
    private Debounce(TimeProvider time)
    {
        this.time = time;
        // Process-lifetime background loop, exactly one per lazily created backend.
        // A void thread preserves callback panic rather than swallowing a Task fault.
        new Thread(Loop)
        {
            IsBackground = true
        }.Start();
    }

    internal static Debounce NewDebounce(TimeProvider? time = null) => new(time ?? TimeProvider.System);
    internal void Add(object key, Action callback)
    {
        lock (mu)
            callbacks[key] = callback;
    }

    internal void Remove(object key)
    {
        lock (mu)
            callbacks.Remove(key);
    }

    internal void Trigger()
    {
        lock (latchMu)
        {
            if (!notified)
            {
                notified = true;
                WaitChLocked().TrySetResult();
            }

            TriggerChLocked().TrySetResult();
            triggerCh = NewChannel();
        }
    }

    private void Loop()
    {
        while (true)
        {
            LatchWait();
            NotifyIfReady();
        }
    }

    private void NotifyIfReady()
    {
        var fire = false;
        lock (mu)
        {
            var now = time.GetTimestamp();
            // Go's zero time precedes the initial Now by more than maxWaitTime;
            // later gaps use its monotonic clock component, mapped to TimeProvider.
            if (lastTime is null || time.GetElapsedTime(lastTime.Value, now) > MaxWaitTime)
            {
                lastTime = now;
                fire = true;
            }
        }

        if (fire)
            FireCallbacks();
        else
            CoalesceWait();
    }

    private void CoalesceWait()
    {
        Task channel;
        lock (latchMu)
            channel = TriggerChLocked().Task;
        // Closed-channel signals are persistent, not consumed; a losing await
        // cannot steal a later trigger. The short timer is allowed to expire.
        var timer = DelayAfter(MinWaitTime);
        var winner = Task.WhenAny(channel, timer).GetAwaiter().GetResult();
        if (ReferenceEquals(winner, timer))
            FireCallbacks();
    }

    // debounce.go:time.After. Nonpositive Go durations are immediately ready.
    // CLR timers have a finite per-timer millisecond range; retain the original
    // absolute monotonic deadline using bounded timers for larger durations.
    private Task DelayAfter(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return Task.CompletedTask;
        var maximumTimer = TimeSpan.FromMilliseconds(int.MaxValue);
        return duration <= maximumTimer ? Task.Delay(duration, time) : DelayLong(duration, maximumTimer);
    }

    private async Task DelayLong(TimeSpan duration, TimeSpan maximumTimer)
    {
        long start = time.GetTimestamp();
        while (true)
        {
            var remaining = duration - time.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero)
                return;
            await Task.Delay(remaining <= maximumTimer ? remaining : maximumTimer, time).ConfigureAwait(false);
        }
    }

    private void FireCallbacks()
    {
        Slice<Action> copy;
        lock (mu)
        {
            lastTime = time.GetTimestamp();
            copy = Slice<Action>.Make(0, callbacks.Count);
            foreach (var callback in callbacks.Values)
                copy = copy.Append(callback);
        }

        LatchReset();
        foreach (var callback in copy)
            callback();
    }

    private static TaskCompletionSource NewChannel() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource WaitChLocked() => waitCh ??= NewChannel();
    private TaskCompletionSource TriggerChLocked() => triggerCh ??= NewChannel();
    private void LatchWait()
    {
        Task channel;
        lock (latchMu)
            channel = WaitChLocked().Task;
        channel.GetAwaiter().GetResult();
    }

    private void LatchReset()
    {
        lock (latchMu)
            if (notified)
            {
                notified = false;
                waitCh = NewChannel();
            }
    }
}
