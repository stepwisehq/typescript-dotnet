// Authored runtime-duration boundary controls; no original-function credit.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Lsp.Protocol;
using Watcher = TypeScript.Lsp.Protocol.FileSystemWatcher;
using Telemetry = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static class ProjectSessionSchedulingChecks
{
    public static int RunAll()
    {
        foreach (var delay in new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(-1),
            TimeSpan.FromMilliseconds(-2)
        }

        )
        {
            var client = new Client();
            var session = NewSession(delay, client);
            Thread? drain = null;
            Exception? failure = null;
            Snapshot? retained = null;
            try
            {
                var uri = new DocumentUri(ByteStringValue.FromString("file:///src/index.ts"));
                session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString("export const x = 1;"), LanguageKind.TypeScript);
                var before = session.Snapshot();
                before.Ref();
                retained = before;
                var file = RootedFilePath.FromGoString("/src/index.ts");
                Require(before.IsOpenFile(file), "original snapshot should retain the open overlay");
                session.DidCloseFile(ProtocolRequestContext.Default, uri);
                session.ScheduleDiagnosticsRefresh();
                drain = new Thread(() =>
                {
                    try
                    {
                        session.WaitForBackgroundTasks();
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                })
                {
                    IsBackground = true
                };
                drain.Start();
                Require(drain.Join(TimeSpan.FromSeconds(10)), "nonpositive debounce must not block or reject either scheduled worker");
                if (failure is not null)
                    throw failure;
                Require(client.RefreshCount == 1, "the actual scheduled diagnostic refresh must run");
                var after = session.Snapshot();
                Require(!ReferenceEquals(before, after) && !after.IsOpenFile(file), "actual scheduled close must create a closed-overlay snapshot");
                Require(before.IsOpenFile(file), "snapshot transition must preserve the old overlay owner");
            }
            finally
            {
                session.Close();
                retained?.Deref();
                if (drain is not null)
                    Require(drain.Join(TimeSpan.FromSeconds(10)), "canceled scheduled work did not drain");
            }
        }

        var longClient = new Client();
        var longSession = NewSession(TimeSpan.FromDays(40), longClient);
        longSession.ScheduleSnapshotUpdate(UpdateReason.DidCloseFile);
        longSession.ScheduleDiagnosticsRefresh();
        Exception? closeFailure = null;
        var close = new Thread(() =>
        {
            try
            {
                longSession.Close();
            }
            catch (Exception error)
            {
                closeFailure = error;
            }
        })
        {
            IsBackground = true
        };
        close.Start();
        Require(close.Join(TimeSpan.FromSeconds(10)), "large positive debounce must remain cancelable without CLR timer rejection");
        if (closeFailure is not null)
            throw closeFailure;
        Require(longClient.RefreshCount == 0, "closing must cancel the delayed refresh");
        return 1;
    }

    private static Session NewSession(TimeSpan delay, Client client) => Session.NewSession(new SessionInit { BackgroundContext = ProtocolRequestContext.Default, Options = new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), PositionEncoding = PositionEncodingKind.UTF8, DebounceDelay = delay }, FS = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new BaselineFS.Input("/src/tsconfig.json", "{ \"compilerOptions\": { \"noLib\": true } }"), new BaselineFS.Input("/src/index.ts", "export const x = 1;"))), Logger = LogCollector.NewTestLogger(), Client = client });
    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    // Injected client boundary records genuine Session callbacks, not simulated scheduling.
    private sealed class Client : IProjectClient
    {
        private int refreshCount;
        internal int RefreshCount => Volatile.Read(ref refreshCount);

        public Exception? RefreshDiagnostics(ProtocolRequestContext context)
        {
            Interlocked.Increment(ref refreshCount);
            return null;
        }

        public Exception? WatchFiles(ProtocolRequestContext context, WatcherID id, Slice<Watcher> watchers) => null;
        public Exception? UnwatchFiles(ProtocolRequestContext context, WatcherID id) => null;
        public Exception? RegisterContentMapperExtensions(ProtocolRequestContext context, Slice<ByteStringValue> extensions) => null;
        public Exception? PublishDiagnostics(ProtocolRequestContext context, PublishDiagnosticsParams parameters) => null;
        public Exception? RefreshInlayHints(ProtocolRequestContext context) => null;
        public Exception? RefreshCodeLens(ProtocolRequestContext context) => null;
        public void ProgressStart(TypeScript.Scanner.Message message, params object? [] arguments)
        {
        }

        public void ProgressFinish(TypeScript.Scanner.Message message, params object? [] arguments)
        {
        }

        public Exception? SendTelemetry(ProtocolRequestContext context, Telemetry telemetry) => null;
        public bool IsActive() => true;
        public void SetLocale(ByteStringValue locale)
        {
        }

        public TypeScript.Locale.Locale GetLocale() => TypeScript.Locale.Locales.Default;
    }
}
