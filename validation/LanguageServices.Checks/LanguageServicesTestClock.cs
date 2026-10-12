// Authored deterministic counterpart of this owner's Go synctest.Wait/Sleep only.
// Standard TimeProvider drives actual Task.Delay; no events or outputs are injected.
namespace LanguageServices.Checks;
internal sealed class LanguageServicesTestClock : TimeProvider
{
    private readonly object gate = new();
    private readonly List<Timer> timers = new();
    private long ticks;
    public override DateTimeOffset GetUtcNow()
    {
        lock (gate)
            return DateTimeOffset.UnixEpoch.AddTicks(ticks);
    }

    public override long GetTimestamp()
    {
        lock (gate)
            return ticks;
    }

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new Timer(this, callback, state);
        lock (gate)
            timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    internal void Advance(TimeSpan duration)
    {
        List<Timer> fire = new();
        lock (gate)
        {
            ticks = checked(ticks + duration.Ticks);
            foreach (var timer in timers)
            {
                if (timer.Disposed || timer.Due < 0 || timer.Due > ticks)
                    continue;
                timer.Due = timer.Period < 0 ? -1 : checked(ticks + timer.Period);
                fire.Add(timer);
            }
        }

        foreach (var timer in fire)
            timer.Callback(timer.State);
    }

    private sealed class Timer(LanguageServicesTestClock clock, TimerCallback callback, object? state) : ITimer
    {
        internal readonly TimerCallback Callback = callback;
        internal readonly object? State = state;
        internal long Due = -1;
        internal long Period = -1;
        internal bool Disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (clock.gate)
            {
                if (Disposed)
                    return false;
                Due = dueTime == Timeout.InfiniteTimeSpan ? -1 : checked(clock.ticks + dueTime.Ticks);
                Period = period == Timeout.InfiniteTimeSpan ? -1 : period.Ticks;
                return true;
            }
        }

        public void Dispose()
        {
            lock (clock.gate)
            {
                Disposed = true;
                Due = -1;
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
