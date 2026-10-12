// Authored runtime adaptation: Go's growable goroutine stack has no direct CLR equivalent.
// Continue a synchronous recursive owner on a fresh standard thread only when the
// runtime reports insufficient remaining execution stack. This changes no parser algorithm.
using System.Runtime.ExceptionServices;

namespace TypeScript.Scanner;
internal static class SynchronousStackContinuation
{
    // A bounded stack segment, independent of source text/depth. Deep input may
    // require several segments/OS threads; ordinary input creates none.
    private const int SegmentStackBytes = 4 * 1024 * 1024;

    internal static TResult Run<TState, TResult>(TState state, Func<TState, TResult> callback)
    {
        var work = new Work<TState, TResult>(state, callback, ExecutionContext.Capture());
        var thread = new Thread(static value => ((Work<TState, TResult>)value!).Run(), SegmentStackBytes)
        {
            IsBackground = true,
            Name = "TypeScript parser stack continuation",
        };
        thread.Start(work);
        // Never release/recycle the retained parser while a continuation is live,
        // even if external CLR thread interruption interrupts the parent's Join.
        ExceptionDispatchInfo? interrupted = null;
        while (true)
        {
            try { thread.Join(); break; }
            catch (ThreadInterruptedException error) { interrupted ??= ExceptionDispatchInfo.Capture(error); }
        }
        interrupted?.Throw();
        work.Failure?.Throw();
        return work.Result!;
    }

    private sealed class Work<TState, TResult>(TState state, Func<TState, TResult> callback, ExecutionContext? context)
    {
        internal TResult? Result;
        internal ExceptionDispatchInfo? Failure;
        internal void Run()
        {
            try
            {
                if (context == null) Execute();
                else ExecutionContext.Run(context, static value => ((Work<TState, TResult>)value!).Execute(), this);
            }
            catch (Exception error) { Failure = ExceptionDispatchInfo.Capture(error); }
        }
        private void Execute() => Result = callback(state);
    }
}
