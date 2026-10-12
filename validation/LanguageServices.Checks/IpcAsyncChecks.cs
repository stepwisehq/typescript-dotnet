// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original internal/ipc/conn_async_test.go seven test functions.
using System.Threading.Channels;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class IpcAsyncChecks
{
    public static IEnumerable<string> Names => ["TestAsyncConnRunWaitsForHandlers", "TestAsyncConnRunCancelsHandlersOnEOF", "TestAsyncConnResponseWriteFailureWithNilTransport", "TestAsyncConnCallReturnsWhenPeerCloses", "TestAsyncConnCallAfterReadLoopFailureReturnsImmediately", "TestAsyncConnTerminalErrorIncludesResponseWriteFailure", "TestAsyncConnRunWaitsForRequestAfterPeerCloses"];

    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return 7;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestAsyncConnRunWaitsForHandlers":
                RunWaitsForHandlers();
                break;
            case "TestAsyncConnRunCancelsHandlersOnEOF":
                CancelsOnEOF();
                break;
            case "TestAsyncConnResponseWriteFailureWithNilTransport":
                NilTransportFailure();
                break;
            case "TestAsyncConnCallReturnsWhenPeerCloses":
                PeerCloses();
                break;
            case "TestAsyncConnCallAfterReadLoopFailureReturnsImmediately":
                ReadFailure();
                break;
            case "TestAsyncConnTerminalErrorIncludesResponseWriteFailure":
                TerminalIncludesFailure();
                break;
            case "TestAsyncConnRunWaitsForRequestAfterPeerCloses":
                WaitsAfterPeerCloses();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static ByteStringValue Text(string text) => ByteStringValue.FromString(text);
    private static GenericMessage Request(string method) => new()
    {
        ID = ID.NewIDString(Text("1")),
        Method = Text(method)
    };
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Nil(Exception? error) => Assert(error is null, error?.ToString() ?? "");
    private static void Contains(Exception? error, string text) => Assert(error?.Message.Contains(text, StringComparison.Ordinal) == true, "missing error: " + text + "; actual " + error);
    private static bool Is(Exception? error, Exception wanted) => ReferenceEquals(error, wanted) || error is not null && (Is(error.InnerException, wanted) || error is AsyncConn.JoinedError joined && Is(joined.Second, wanted));
    private static T Await<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
    // Authored catastrophic probe: invoke only in an isolated Native child.
    // Go has no notification recovery; its panic must not become successful Run.
    public static void RunFatalNotificationControl()
    {
        var connection = AsyncConn.NewAsyncConnWithProtocol(null, new FatalLiveReadProtocol(), new FatalNotificationHandler());
        connection.Run(default);
        throw new InvalidOperationException("fatal notification unexpectedly returned");
    }

    private sealed class FatalLiveReadProtocol : IProtocol
    {
        private bool delivered;
        public (GenericMessage? , Exception? ) ReadMessage()
        {
            if (!delivered)
            {
                delivered = true;
                return (new GenericMessage { Method = Text("panic") }, null);
            }

            using var liveRead = new ManualResetEventSlim();
            liveRead.Wait();
            throw new InvalidOperationException("fatal probe live read unexpectedly resumed");
        }

        public Exception? WriteRequest(ID? id, ByteStringValue method, object? parameters) => null;
        public Exception? WriteNotification(ByteStringValue method, object? parameters) => null;
        public Exception? WriteResponse(ID? id, object? result) => null;
        public Exception? WriteError(ID? id, ResponseError? error) => null;
    }

    private sealed class FatalNotificationHandler : IHandler
    {
        public (object? , Exception? ) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => (null, null);
        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => throw new InvalidOperationException("authored fatal IPC notification");
    }

    private sealed class NoOpHandler : IHandler
    {
        public (object? , Exception? ) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => (null, null);
        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private sealed class QueuedProtocol(Slice<GenericMessage> messages, Exception? responseError = null) : IProtocol
    {
        private Slice<GenericMessage> messages = messages;
        public (GenericMessage? , Exception? ) ReadMessage()
        {
            if (messages.Length == 0)
                return (null, new EndOfStreamException("EOF"));
            var message = messages[0];
            messages = messages.Slice2(1);
            return (message, null);
        }

        public Exception? WriteRequest(ID? id, ByteStringValue method, object? parameters) => null;
        public Exception? WriteNotification(ByteStringValue method, object? parameters) => null;
        public Exception? WriteResponse(ID? id, object? result) => responseError;
        public Exception? WriteError(ID? id, ResponseError? error) => responseError;
    }

    private sealed class BlockingHandler : IHandler, IDisposable
    {
        internal readonly Channel<bool> Started = Channel.CreateUnbounded<bool>();
        internal readonly ManualResetEventSlim Release = new();
        private void Block()
        {
            Started.Writer.TryWrite(true);
            Release.Wait();
        }

        public (object? , Exception? ) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            Block();
            return (null, null);
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            Block();
            return null;
        }

        public void Dispose()
        {
            Release.Set();
        }
    }

    private sealed class ContextHandler : IHandler
    {
        public (object? , Exception? ) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            context.WaitHandle.WaitOne();
            return (null, new OperationCanceledException("context canceled", context));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            context.WaitHandle.WaitOne();
            return new OperationCanceledException("context canceled", context);
        }
    }

    private static void RunWaitsForHandlers()
    {
        var protocol = new QueuedProtocol(Slice<GenericMessage>.FromArray([Request("request"), new() { Method = Text("notification") }]));
        using var handler = new BlockingHandler();
        var connection = AsyncConn.NewAsyncConnWithProtocol(null, protocol, handler);
        var run = Task.Run(() => connection.Run(default));
        Await(handler.Started.Reader.ReadAsync().AsTask());
        Await(handler.Started.Reader.ReadAsync().AsTask());
        Assert(!run.IsCompleted, "Run returned while handlers were active");
        handler.Release.Set();
        Nil(Await(run));
    }

    private static void CancelsOnEOF()
    {
        var connection = AsyncConn.NewAsyncConnWithProtocol(null, new QueuedProtocol(Slice<GenericMessage>.FromArray([Request("request")])), new ContextHandler());
        Nil(Await(Task.Run(() => connection.Run(default))));
    }

    private static void NilTransportFailure()
    {
        var failure = new IOException("response write failed");
        var connection = AsyncConn.NewAsyncConnWithProtocol(null, new QueuedProtocol(Slice<GenericMessage>.FromArray([Request("request")]), failure), new NoOpHandler());
        Assert(Is(connection.Run(default), failure), "expected response write error");
    }

    private sealed class CloseSignal : Stream
    {
        internal readonly ManualResetEventSlim Closed = new();
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Closed.Set();
            base.Dispose(disposing);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private sealed class FailingResponseProtocol(ManualResetEventSlim closed, Exception responseError) : IProtocol
    {
        private bool requestRead;
        public (GenericMessage? , Exception? ) ReadMessage()
        {
            if (!requestRead)
            {
                requestRead = true;
                return (new() { ID = ID.NewIDInt(1), Method = Text("transform") }, null);
            }

            closed.Wait();
            return (null, new IOException("io: read/write on closed pipe"));
        }

        public Exception? WriteRequest(ID? id, ByteStringValue method, object? parameters) => null;
        public Exception? WriteNotification(ByteStringValue method, object? parameters) => null;
        public Exception? WriteResponse(ID? id, object? result) => responseError;
        public Exception? WriteError(ID? id, ResponseError? error) => responseError;
    }

    private static void PeerCloses()
    {
        var(client, server) = IpcTestPipe.Pair();
        using var clientOwner = client;
        using var serverOwner = server;
        var connection = AsyncConn.NewAsyncConn(client, new NoOpHandler());
        var run = Task.Run(() => connection.Run(default));
        var call = Task.Run(() => connection.Call(default, Text("transform"), null));
        _ = server.Read(new byte[1024]);
        server.Dispose();
        Nil(Await(run));
        Assert(Is(Await(call).Error, Ipc.ErrConnClosed), "expected ErrConnClosed");
    }

    private static void ReadFailure()
    {
        var(client, server) = IpcTestPipe.Pair();
        using var clientOwner = client;
        using var serverOwner = server;
        var connection = AsyncConn.NewAsyncConn(client, new NoOpHandler());
        var run = Task.Run(() => connection.Run(default));
        server.Write("oops\n"u8);
        Contains(Await(run), "invalid header");
        _ = Task.Run(() => server.CopyTo(Stream.Null));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var(_, error) = connection.Call(cancellation.Token, Text("transform"), null);
        Assert(Is(error, Ipc.ErrConnClosed), "expected ErrConnClosed");
        Assert(error is not OperationCanceledException, "call waited for its context deadline");
        Assert(Is(connection.Notify(cancellation.Token, Text("changed"), null), Ipc.ErrConnClosed), "expected ErrConnClosed");
    }

    private static void TerminalIncludesFailure()
    {
        var failure = new IOException("response write failed");
        using var stream = new CloseSignal();
        var connection = AsyncConn.NewAsyncConnWithProtocol(stream, new FailingResponseProtocol(stream.Closed, failure), new NoOpHandler());
        Assert(Is(connection.Run(default), failure), "expected response write error");
        var(_, error) = connection.Call(default, Text("transform"), null);
        Assert(Is(error, failure), "expected terminal response write error");
        Assert(error!.Message.Split(failure.Message, StringSplitOptions.None).Length - 1 == 1, "response write error must appear once");
    }

    private static void WaitsAfterPeerCloses()
    {
        var(client, server) = IpcTestPipe.Pair();
        using var clientOwner = client;
        using var serverOwner = server;
        using var handler = new BlockingHandler();
        var connection = AsyncConn.NewAsyncConn(server, handler);
        var run = Task.Run(() => connection.Run(default));
        Nil(JSONRPCProtocol.NewJSONRPCProtocol(client).WriteRequest(ID.NewIDInt(1), Text("transform"), null));
        Await(handler.Started.Reader.ReadAsync().AsTask());
        client.Dispose();
        Assert(!run.Wait(TimeSpan.FromMilliseconds(100)), "connection stopped while request handler was blocked");
        handler.Release.Set();
        Contains(Await(run), "ipc: failed to write response");
        Contains(connection.Call(default, Text("transform"), null).Error, "ipc: failed to write response");
        Contains(connection.Notify(default, Text("changed"), null), "ipc: failed to write response");
    }
}
