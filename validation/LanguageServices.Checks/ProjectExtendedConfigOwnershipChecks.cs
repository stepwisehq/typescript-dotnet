using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;
using Telemetry = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;
using Watcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace LanguageServices.Checks;
public static partial class ProjectExtendedConfigOwnershipChecks
{
    private sealed class NoopClient : IProjectClient
    {
        public Exception? WatchFiles(ProtocolRequestContext context, WatcherID id, Slice<Watcher> watchers) => null;
        public Exception? UnwatchFiles(ProtocolRequestContext context, WatcherID id) => null;
        public Exception? RegisterContentMapperExtensions(ProtocolRequestContext context, Slice<ByteStringValue> extensions) => null;
        public Exception? RefreshDiagnostics(ProtocolRequestContext context) => null;
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

    private static RootedFilePath File(string path) => RootedFilePath.FromGoString(path);
    private static PathKey Key(Session session, string path) => session.SnapshotHost.FS().CaseSensitivity().PathKey(File(path).AsPath());
    private static DocumentUri Uri(string path) => new(ByteStringValue.FromString("file://" + path));
    private static void Check([DoesNotReturnIf(false)] bool value)
    {
        if (!value)
            throw new InvalidOperationException("original extended-config ownership assertion failed");
    }

    private static Session Setup(Input[] files) => Session.NewSession(new SessionInit { BackgroundContext = ProtocolRequestContext.Default, FS = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files)), Client = new NoopClient(), Logger = LogCollector.NewTestLogger(), NpmExecutor = null, Options = new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = false, LoggingEnabled = false } });
    private static void Open(Session session, string path, string text) => session.DidOpenFile(ProtocolRequestContext.Default, Uri(path), 1, ByteStringValue.FromString(text), LanguageKind.TypeScript);
    private static int OwnerCount(Session session, PathKey path)
    {
        var(entry, ok) = session.SnapshotHost.extendedConfigCache.entries.Load(path);
        return ok ? entry.Owners.Count : 0;
    }

    private static void AssertCounts(Session session, Snapshot snapshot)
    {
        Dictionary<PathKey, int> expected = new();
        if (snapshot.ConfigFileRegistry.Configs is not null)
            foreach (var config in snapshot.ConfigFileRegistry.Configs.Values)
                if (config.CommandLine?.ConfigFile is not null)
                    foreach (var file in config.CommandLine.ExtendedSourceFiles() ?? [])
                    {
                        var key = session.SnapshotHost.FS().CaseSensitivity().PathKey(file.AsPath());
                        expected[key] = expected.GetValueOrDefault(key) + 1;
                    }

        foreach (var pair in expected)
            Check(OwnerCount(session, pair.Key) == pair.Value);
    }

    public static int RunAll()
    {
        TestExtendedConfigCacheOwnership();
        return 1;
    }

    public static void TestExtendedConfigCacheOwnership()
    {
        List<Exception>? failures = null;
        foreach (var group in new Action[]
        {
            SharedAncestor,
            BypassCache,
            CaseOnly,
            Transitive
        }

        )
            try
            {
                group();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }

        if (failures is not null)
            throw new AggregateException(failures);
    }

    private static void SharedAncestor()
    {
        var files = Files0();
        var session = Setup(files);
        try
        {
            Open(session, "/project/src/main.ts", files.Single(file => file.Path == "/project/src/main.ts").Contents);
            var snapshot = session.Snapshot();
            var config = snapshot.ConfigFileRegistry.GetConfig(Key(session, "/project/tsconfig.json"));
            Check(config is not null);
            Check(config.ExtendedSourceFiles()!.Count(file => file == File("/project/tsconfig.root.json")) == 1);
            AssertCounts(session, snapshot);
            session.DidCloseFile(ProtocolRequestContext.Default, Uri("/project/src/main.ts"));
            session.DidOpenFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("untitled:Untitled-1")), 1, default, LanguageKind.TypeScript);
            foreach (var path in new[]
            {
                "/project/tsconfig.base1.json",
                "/project/tsconfig.base2.json",
                "/project/tsconfig.root.json"
            }

            )
                Check(!session.SnapshotHost.extendedConfigCache.entries.Load(Key(session, path)).Ok);
        }
        finally
        {
            session.Close();
        }
    }

    private static void BypassCache()
    {
        var fs = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, Files1()));
        var(command, diagnostics) = TypeScript.TsOptions.TsOptions.GetParsedCommandLineOfConfigFile(File("/project/tsconfig.json"), null, null, fs, null);
        Check(diagnostics.Length == 0);
        Check(command is not null);
        var extended = command.ExtendedSourceFiles();
        Check(extended is not null && extended.Count == 2);
        Check(extended[0] == File("/project/Shared.json"));
        Check(extended[1] == File("/project/shared.json"));
    }

    private static void CaseOnly()
    {
        var files = Files2();
        var session = Setup(files);
        try
        {
            Open(session, "/project/src/main.ts", files.Single(file => file.Path == "/project/src/main.ts").Contents);
            var config = session.Snapshot().ConfigFileRegistry.GetConfig(Key(session, "/project/tsconfig.json"));
            Check(config is not null);
            var extended = config.ExtendedSourceFiles();
            Check(extended is not null && extended.Count == 1);
            Check(session.SnapshotHost.FS().CaseSensitivity().PathKey(extended[0].AsPath()) == Key(session, "/project/shared.json"));
        }
        finally
        {
            session.Close();
        }
    }

    private static void Transitive()
    {
        var files = Files3();
        var session = Setup(files);
        const string a = "/user/username/projects/projectA/src/main.ts", b = "/user/username/projects/projectB/src/main.ts", other = "/user/username/projects/other/src/main.ts";
        try
        {
            Open(session, a, files.Single(file => file.Path == a).Contents);
            var(baseEntry, baseOk) = session.SnapshotHost.extendedConfigCache.entries.Load(Key(session, "/user/username/projects/shared/tsconfig.base.json"));
            var(commonEntry, commonOk) = session.SnapshotHost.extendedConfigCache.entries.Load(Key(session, "/user/username/projects/shared/tsconfig.common.json"));
            Check(baseOk);
            Check(commonOk);
            Check(baseEntry.Owners.Count == 1);
            Check(commonEntry.Owners.Count == 1);
            Open(session, b, files.Single(file => file.Path == b).Contents);
            session.DidCloseFile(ProtocolRequestContext.Default, Uri(a));
            Open(session, other, files.Single(file => file.Path == other).Contents);
            session.DidCloseFile(ProtocolRequestContext.Default, Uri(other));
            session.DidChangeFile(ProtocolRequestContext.Default, Uri(b), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { Partial = new TextDocumentContentChangePartial { Range = new() { Start = new() { Line = 0, Character = 0 }, End = new() { Line = 0, Character = 12 } }, Text = ByteStringValue.FromString("const b = 3;") } }]));
            var(_, error) = session.GetLanguageService(ProtocolRequestContext.Default, Uri(b));
            Check(error is null);
        }
        finally
        {
            session.Close();
        }
    }
}
