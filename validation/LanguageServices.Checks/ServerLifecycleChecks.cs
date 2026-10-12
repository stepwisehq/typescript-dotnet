// Complete three original internal/lsp/server_test.go assertions.
// Pinned fed0bf24149fb1ed36039212648bafdafc1ea10e; same-package access joins pending.
using TypeScript.JsonRpc;
using ID = TypeScript.JsonRpc.ID;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static class ServerLifecycleChecks
{
    private sealed class Reader : ILspReader
    {
        public (ProtocolMessage? Message, Exception? Error) Read() => (null, new EndOfStreamException());
    }

    private sealed class Writer : ILspWriter
    {
        public Exception? Write(ProtocolMessage message) => null;
    }

    private sealed class CancelingWriter(ILspWriter inner, CancellationTokenSource cancellation) : ILspWriter
    {
        internal readonly List<ProtocolMessage> Messages = new();
        public Exception? Write(ProtocolMessage message)
        {
            var error = inner.Write(message);
            if (error is not null)
                return error;
            Messages.Add(message);
            if (Messages.Count == 2)
                cancellation.Cancel();
            return null;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static Server NewServer(ILspWriter? writer = null) => Server.NewServer(new ServerOptions { In = new Reader(), Out = writer ?? new Writer(), Err = Stream.Null, Cwd = RootedDirectoryPath.FromGoString("/test") });
    public static void TestServerShutdownNoDeadlock()
    {
        var fs = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, [new Input("/test/tsconfig.json", "{}"), new Input("/test/index.ts", "const x = 1;")]));
        var server = Server.NewServer(new ServerOptions { In = new Reader(), Out = new Writer(), Err = Stream.Null, Cwd = RootedDirectoryPath.FromGoString("/test"), FS = fs, DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath() });
        using var cancellation = new CancellationTokenSource();
        server.backgroundCtx = cancellation.Token;
        var writeLoop = Task.Factory.StartNew(() => server.WriteLoop(cancellation.Token), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        server.initStarted = 1;
        server.session = Session.NewSession(new SessionInit { BackgroundContext = ProtocolRequestContext.Default.WithCancellationToken(cancellation.Token), Options = new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/test"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = false, LoggingEnabled = true }, FS = fs, Logger = server.logger });
        var context = ProtocolRequestContext.Default.WithCancellationToken(cancellation.Token);
        var uri = new DocumentUri(ByteStringValue.FromString("file:///test/index.ts"));
        server.session.DidOpenFile(context, uri, 1, ByteStringValue.FromString("const x = 1;"), LanguageKind.TypeScript);
        server.session.WaitForBackgroundTasks();
        cancellation.Cancel();
        // Source receive is untimed; this is deadlock safety, not a scheduling assertion.
        writeLoop.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        server.session.DidChangeFile(context, uri, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new TextDocumentContentChangeWholeDocument { Text = ByteStringValue.FromString("const x = 2;") } }]));
        server.session.GetLanguageService(context, uri);
        server.session.WaitForBackgroundTasks();
        server.session.Close();
    }

    public static void TestServerOutgoingQueueDoesNotBlockWithoutWriter()
    {
        var server = NewServer();
        server.backgroundCtx = CancellationToken.None;
        var message = Method.WindowLogMessageInfo.NewNotificationMessage(new LogMessageParams { Type = MessageType.Info, Message = ByteStringValue.FromString("queued") }).Message();
        var worker = Task.Factory.StartNew(() =>
        {
            for (int i = 0; i < 1000; i++)
                if (server.Send(message)is { } error)
                    throw error;
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        worker.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
    }

    public static void TestWriteLoopRecoversFromUnserializableResponse()
    {
        using var cancellation = new CancellationTokenSource();
        var writer = new CancelingWriter(LspTransport.ToWriter(Stream.Null), cancellation);
        var server = NewServer(writer);
        server.backgroundCtx = cancellation.Token;
        SelectionRange? deep = null;
        for (int i = 0; i < 20000; i++)
            deep = new SelectionRange
            {
                Parent = deep
            };
        // The closed union serializes the same original one-element []*SelectionRange
        // JSON array; actual recursive writer/error and subsequent recovery are retained.
        var badID = ID.NewIDString(ByteStringValue.FromString("bad"));
        var bad = new SelectionRangesOrNull
        {
            SelectionRanges = Slice<SelectionRange?>.FromArray([deep])
        };
        Check(server.Send(new ResponseMessage { ID = badID, Result = new ProtocolPayload<SelectionRangesOrNull>(ProtocolCodec.GetType_130b4f72041fba63(), bad) }.Message())is null, "failed to enqueue bad response");
        var goodID = ID.NewIDString(ByteStringValue.FromString("good"));
        Check(server.Send(new ResponseMessage { ID = goodID, Result = new ProtocolPayload<SelectionRangesOrNull>(ProtocolCodec.GetType_130b4f72041fba63(), new()) }.Message())is null, "failed to enqueue good response");
        Check(server.WriteLoop(cancellation.Token) is OperationCanceledException, "write loop exited unexpectedly");
        bool sawError = false, sawGood = false;
        foreach (var message in writer.Messages)
        {
            var response = message.AsResponse();
            if (response.ID is { } id && IDKey.FromID(id).Equals(IDKey.FromID(badID)))
            {
                Check(response.Error?.Code == (int)ErrorCode.InternalError, "expected InternalError response for unserializable request");
                sawError = true;
            }
            else if (response.ID is { } other && IDKey.FromID(other).Equals(IDKey.FromID(goodID)))
            {
                Check(response.Error is null, "expected successful good response");
                sawGood = true;
            }
            else
                throw new InvalidOperationException("unexpected response id");
        }

        Check(sawError, "did not receive error response for unserializable request");
        Check(sawGood, "did not receive subsequent well-formed response");
    }
}
