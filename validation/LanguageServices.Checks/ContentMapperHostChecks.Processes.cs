// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Ipc;
using TypeScript.Scanner;
using LocaleValue = TypeScript.Locale.Locale;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    private sealed class ExitOnCloseStream(Stream inner) : ProcessStream(inner)
    {
        private int exited;
        internal override Exception? CloseProcess()
        {
            Volatile.Write(ref exited, 1);
            return CloseInner(Inner);
        }

        public override (int Code, bool Exited) ExitCode() => (1, Volatile.Read(ref exited) != 0);
    }

    private sealed class ExitedStream(Stream inner, int code) : ProcessStream(inner)
    {
        internal override Exception? CloseProcess() => CloseInner(Inner);
        public override (int Code, bool Exited) ExitCode() => (code, true);
    }

    private sealed class CloseSignalStream(Stream inner, ManualResetEventSlim closed) : ProcessStream(inner)
    {
        private int signaled;
        internal override Exception? CloseProcess()
        {
            var error = CloseInner(Inner);
            if (Interlocked.Exchange(ref signaled, 1) == 0)
                closed.Set();
            return error;
        }
    }

    // host_test.go:329. Broken framing after a successful handshake must close
    // the actual process owner when its live read loop terminates.
    private static void HostClosesProcessWhenReadLoopFails()
    {
        using var closed = new ManualResetEventSlim();
        var spawner = new SpawnerFunc((command, directory, stderr) =>
        {
            var(client, server) = IpcTestPipe.Pair();
            ThreadPool.UnsafeQueueUserWorkItem(static stream =>
            {
                try
                {
                    var protocol = JSONRPCProtocol.NewJSONRPCProtocol(stream);
                    var(message, error) = protocol.ReadMessage();
                    Assert(error is null && message is not null, "initialize frame");
                    Assert(message!.Method.EqualsValue(ProcessHosts.MethodInitialize), "initialize method");
                    var writeError = protocol.WriteResponse(message.ID, Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = PositionEncoding.UTF8, DiagnosticSource = Text("mapper") }));
                    Assert(writeError is null, "initialize response");
                    stream.Write("oops\n"u8);
                }
                finally
                {
                    stream.Dispose();
                }
            }, server, preferLocal: false);
            return (new CloseSignalStream(client, closed), null);
        });
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var(_, error) = host.Transform(Mapper(), new Request { FileName = new RootedFilePath("/a.vue"), Content = "" });
            Assert(error is not null, "failed framing transform error");
            Assert(closed.Wait(TimeSpan.FromSeconds(1)), "mapper process was not closed after read loop failed");
        }
        finally
        {
            _ = host.Close();
        }
    }

    // host_test.go:364. Actual five-second host initialize timeout precedes
    // closing a child that reports exit only after close.
    private static void HostReportsInitializationTimeoutBeforeClosingProcess()
    {
        using var testContext = new CancellationTokenSource();
        var spawner = new SpawnerFunc((command, directory, stderr) =>
        {
            var(client, server) = IpcTestPipe.Pair();
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                try
                {
                    _ = JSONRPCProtocol.NewJSONRPCProtocol(state.Server).ReadMessage();
                    state.Context.WaitHandle.WaitOne();
                }
                finally
                {
                    state.Server.Dispose();
                }
            }, (Server: server, Context: testContext.Token), preferLocal: false);
            return (new ExitOnCloseStream(client), null);
        });
        var host = ProcessHosts.NewHost(testContext.Token, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var(_, error) = host.Transform(Mapper(), new Request { FileName = new RootedFilePath("/a.vue"), Content = "" });
            var initialize = ErrorAs<InitializeError>(error);
            Assert(initialize is not null && initialize.Kind == InitializeErrorKind.NoResponse, "timeout classified before process close");
        }
        finally
        {
            testContext.Cancel();
            _ = host.Close();
        }
    }

    // host_test.go:384. Already-exited process preserves actual code42.
    private static void HostReportsProcessExitBeforeInitialization()
    {
        var spawner = new SpawnerFunc((command, directory, stderr) =>
        {
            var(client, server) = IpcTestPipe.Pair();
            server.Dispose();
            return (new ExitedStream(client, 42), null);
        });
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var(_, error) = host.Transform(Mapper(), new Request { FileName = new RootedFilePath("/a.vue"), Content = "" });
            var initialize = ErrorAs<InitializeError>(error);
            Assert(initialize is not null && initialize.Kind == InitializeErrorKind.ProcessExit && initialize.ExitCode == 42, "original process-exit kind/code");
        }
        finally
        {
            _ = host.Close();
        }
    }
}
