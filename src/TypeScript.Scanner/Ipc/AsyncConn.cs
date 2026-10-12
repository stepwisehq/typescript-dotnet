// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ipc/conn_async.go; CLR channel and waitgroup owners are local adaptations.
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.Ipc;
internal sealed class AsyncConn : IConn
{
    private readonly Stream? rwc;
    private readonly IProtocol protocol;
    private readonly IHandler handler;
    private TimingCollector? timing;
    private long sequence;
    private readonly Dictionary<IDKey, Channel<GenericMessage>> pending = new();
    private readonly object pendingMutex = new();
    private Exception? terminal;
    private bool hasCause;
    private readonly object writeMutex = new();
    private readonly object handlersMutex = new();
    private int handlers;
    private AsyncConn(Stream? stream, IProtocol protocol, IHandler handler)
    {
        rwc = stream;
        this.protocol = protocol;
        this.handler = handler;
    }

    internal static AsyncConn NewAsyncConn(Stream stream, IHandler handler) => NewAsyncConnWithProtocol(stream, JSONRPCProtocol.NewJSONRPCProtocol(stream), handler);
    internal static AsyncConn NewAsyncConnWithProtocol(Stream? stream, IProtocol protocol, IHandler handler) => new(stream, protocol, handler);
    internal void SetCollectTiming(bool enabled) => timing = enabled ? TimingCollector.NewTimingCollector() : null;
    public Exception? Run(CancellationToken context)
    {
        using var handlerContext = CancellationTokenSource.CreateLinkedTokenSource(context);
        var requestErrors = Channel.CreateBounded<Exception>(1);
        Exception? error = null;
        try
        {
            while (true)
            {
                if (context.IsCancellationRequested)
                {
                    error = CancellationError(context);
                    break;
                }

                var(message, readError) = protocol.ReadMessage();
                if (readError is not null)
                {
                    error = readError is EndOfStreamException ? null : readError;
                    break;
                }

                if (message!.IsResponse())
                    HandleResponse(message);
                else if (message.IsRequest())
                    StartHandler(() =>
                    {
                        var requestError = HandleRequest(handlerContext.Token, message);
                        if (requestError is not null && RecordRequestError(requestError, requestErrors))
                        {
                            try
                            {
                                rwc?.Dispose();
                            }
                            catch (Exception closeError)when (closeError is IOException or ObjectDisposedException)
                            {
                            }
                        }
                    });
                else if (message.IsNotification())
                    StartHandler(() => HandleNotification(handlerContext.Token, message));
            }
        }
        finally
        {
            ClosePendingCalls(error);
            handlerContext.Cancel();
            lock (handlersMutex)
                while (handlers != 0)
                    Monitor.Wait(handlersMutex);
            if (requestErrors.Reader.TryRead(out var requestError))
                error = Join(error, requestError);
        }

        return error;
    }

    // WaitGroup.Go owns only a live worker count, not a retained list of all Tasks.
    // An unrecovered notification exception escapes the void worker, as a Go
    // goroutine panic does; only HandleRequest has an actual recovery boundary.
    private void StartHandler(Action action)
    {
        lock (handlersMutex)
            handlers++;
        ThreadPool.UnsafeQueueUserWorkItem(static state =>
        {
            try
            {
                state.Action();
            }
            finally
            {
                lock (state.Owner.handlersMutex)
                {
                    state.Owner.handlers--;
                    Monitor.PulseAll(state.Owner.handlersMutex);
                }
            }
        }, (Owner: this, Action: action), preferLocal: false);
    }

    private void ClosePendingCalls(Exception? runError)
    {
        lock (pendingMutex)
        {
            RecordTerminalErrorLocked(runError);
            ClosePendingCallsLocked();
        }
    }

    private bool RecordRequestError(Exception requestError, Channel<Exception> requestErrors)
    {
        lock (pendingMutex)
        {
            if (!RecordTerminalErrorLocked(requestError))
                return false;
            requestErrors.Writer.TryWrite(requestError);
            ClosePendingCallsLocked();
            return true;
        }
    }

    private bool RecordTerminalErrorLocked(Exception? error)
    {
        if (terminal is null)
        {
            terminal = Ipc.ErrConnClosed;
            if (error is not null)
            {
                terminal = Join(terminal, error);
                hasCause = true;
                return true;
            }
        }
        else if (!hasCause && error is not null)
        {
            terminal = Join(terminal, error);
            hasCause = true;
            return true;
        }

        return false;
    }

    private void ClosePendingCallsLocked()
    {
        foreach (var response in pending.Values)
            response.Writer.TryComplete();
        pending.Clear();
    }

    private void HandleResponse(GenericMessage message)
    {
        Channel<GenericMessage>? response;
        lock (pendingMutex)
        {
            var key = IDKey.FromID(message.ID!);
            if (!pending.Remove(key, out response))
                return;
        }

        response.Writer.TryWrite(message);
        response.Writer.TryComplete();
    }

    private Exception? HandleRequest(CancellationToken context, GenericMessage message)
    {
        if (message.Method.EqualsValue(Ipc.MethodGetServerTiming))
        {
            Exception? error;
            lock (writeMutex)
                error = protocol.WriteResponse(message.ID, Ipc.ServerTimingSnapshot(timing));
            return error is null ? null : Wrap("ipc: failed to write server timing response: ", error);
        }

        if (message.Method.EqualsValue(Ipc.MethodResetServerTiming))
        {
            timing?.Reset();
            Exception? error;
            lock (writeMutex)
                error = protocol.WriteResponse(message.ID, null);
            return error is null ? null : Wrap("ipc: failed to write reset server timing response: ", error);
        }

        long start = timing is not null ? Stopwatch.GetTimestamp() : 0;
        try
        {
            var(result, handlerError) = handler.HandleRequest(context, message.Method, message.Params);
            if (timing is not null)
            {
                long nanoseconds = (long)((Stopwatch.GetTimestamp() - start) * (1_000_000_000d / Stopwatch.Frequency));
                timing.Record(message.Method, nanoseconds);
            }

            Exception? writeError;
            lock (writeMutex)
                writeError = handlerError is not null ? protocol.WriteError(message.ID, new ResponseError { Code = ErrorCodes.InternalError, Text = ByteStringValue.FromString(GoString.ToUtf8(handlerError.Message)) }) : protocol.WriteResponse(message.ID, result);
            return writeError is null ? null : Wrap("ipc: failed to write response: ", writeError);
        }
        catch (Exception panic)
        {
            // Go's debug.Stack text is a runtime boundary: retain CLR stack rather
            // than fabricate Go frames, while preserving panic response ownership.
            var description = "panic: " + panic.Message + "\n" + new StackTrace(true);
            Exception? writeError;
            lock (writeMutex)
                writeError = protocol.WriteError(message.ID, new ResponseError { Code = ErrorCodes.InternalError, Text = ByteStringValue.FromString(GoString.ToUtf8(description)) });
            return writeError is null ? null : new IOException("ipc: failed to write panic error response: " + writeError.Message + " (original panic: " + panic.Message + ")", writeError);
        }
    }

    private void HandleNotification(CancellationToken context, GenericMessage message) => handler.HandleNotification(context, message.Method, message.Params);
    public (RawJsonValue? Result, Exception? Error) Call(CancellationToken context, ByteStringValue method, object? parameters)
    {
        var id = ID.NewIDString(ByteStringValue.FromString("api" + Interlocked.Increment(ref sequence).ToString(CultureInfo.InvariantCulture)));
        var key = IDKey.FromID(id);
        var response = Channel.CreateBounded<GenericMessage>(1);
        lock (pendingMutex)
        {
            if (terminal is not null)
                return (null, terminal);
            pending[key] = response;
        }

        try
        {
            Exception? error;
            lock (writeMutex)
                error = protocol.WriteRequest(id, method, parameters);
            if (error is not null)
                return (null, error);
            GenericMessage message;
            try
            {
                message = response.Reader.ReadAsync(context).AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                return (null, CancellationError(context));
            }
            catch (ChannelClosedException)
            {
                lock (pendingMutex)
                    return (null, terminal);
            }

            if (message.Error is not null)
                return (null, new IOException("ipc: remote error [" + message.Error.Code.ToString(CultureInfo.InvariantCulture) + "]: " + GoString.ToString(message.Error.Text.Materialize()!)));
            return (message.Result, null);
        }
        finally
        {
            lock (pendingMutex)
                if (pending.Remove(key, out var remaining))
                    remaining.Writer.TryComplete();
        }
    }

    public Exception? Notify(CancellationToken context, ByteStringValue method, object? parameters)
    {
        lock (pendingMutex)
            if (terminal is not null)
                return terminal;
        lock (writeMutex)
            return protocol.WriteNotification(method, parameters);
    }

    private static Exception CancellationError(CancellationToken token) => new OperationCanceledException("context canceled", token);
    private static Exception Wrap(string prefix, Exception cause) => new IOException(prefix + cause.Message, cause);
    private static Exception? Join(Exception? first, Exception? second) => first is null ? second : second is null ? first : new JoinedError(first, second);
    internal sealed class JoinedError(Exception first, Exception second) : Exception(first.Message + "\n" + second.Message, first)
    {
        internal readonly Exception Second = second;
    }
}
