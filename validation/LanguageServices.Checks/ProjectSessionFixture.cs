// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Actual projecttestutil setup/client/npm boundary; real Session and filesystem owners.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.ProjectSystem.Ata;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;
using Watcher = TypeScript.Lsp.Protocol.FileSystemWatcher;
using Telemetry = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;

namespace LanguageServices.Checks;
internal sealed class ProjectSessionFixture : IDisposable
{
    internal readonly ProjectTestFileSystem MutableFS;
    internal readonly Session Session;
    internal readonly TestClient Client = new();
    internal readonly ILogCollector Logger = LogCollector.NewTestLogger();
    internal readonly NpmExecutor Npm;
    internal ProjectSessionFixture(IEnumerable<BaselineFS.Input> files, SessionOptions? options = null, IEnumerable<string>? typesRegistry = null, Dictionary<string, string>? packages = null, TypeScript.ContentMapper.ISpawner? spawner = null, ProtocolRequestContext? backgroundContext = null)
    {
        MutableFS = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files.ToArray());
        Npm = new NpmExecutor(MutableFS, typesRegistry?.ToArray() ?? [], packages ?? new());
        Session = Session.NewSession(new SessionInit { BackgroundContext = backgroundContext ?? ProtocolRequestContext.Default, FS = TypeScript.Bundled.Bundled.WrapFS(MutableFS), Client = Client, NpmExecutor = Npm, Logger = Logger, Spawner = spawner, Options = options ?? new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = true, LoggingEnabled = true, PushDiagnosticsEnabled = true } });
    }

    internal bool WatchesFile(ByteStringValue filePath)
    {
        var sensitivity = MutableFS.CaseSensitivity();
        var fileName = TsPath.ToRootedFilePath(filePath, RootedDirectoryPath.FromGoString("/"));
        foreach (var call in Client.WatchFilesCalls())
            foreach (var watcher in call.Watchers)
            {
                if (watcher.GlobPattern.Pattern is { } absolute)
                {
                    var path = fileName.AsValue();
                    if (!sensitivity.IsCaseSensitive())
                    {
                        absolute = sensitivity.Canonicalize(absolute);
                        path = sensitivity.Canonicalize(path);
                    }

                    var(glob, error) = TypeScript.Glob.Glob.Parse(absolute);
                    if (error is null && glob!.Match(path))
                        return true;
                }
                else if (watcher.GlobPattern.RelativePattern is { } relative)
                {
                    var baseDirectory = TsPath.RootedDirectoryPathFromPath(new DocumentUri(relative.BaseUri.URI!.Value.Value).FileName().AsPath());
                    var(path, ok) = sensitivity.RelativeFilePathFromDirectory(baseDirectory, fileName);
                    if (!ok)
                        continue;
                    var pattern = relative.Pattern;
                    if (!sensitivity.IsCaseSensitive())
                    {
                        pattern = sensitivity.Canonicalize(pattern);
                        path = sensitivity.CanonicalRelativePath(path);
                    }

                    var(glob, error) = TypeScript.Glob.Glob.Parse(pattern);
                    if (error is null && glob!.Match(path.AsValue()))
                        return true;
                }
            }

        return false;
    }

    private int disposed;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            Session.Close();
    }

    internal sealed class TestClient : IProjectClient
    {
        private readonly object gate = new();
        internal readonly List<(ProtocolRequestContext Context, WatcherID ID, Slice<Watcher> Watchers)> WatchCalls = new();
        internal readonly List<(ProtocolRequestContext Context, WatcherID ID)> UnwatchCalls = new();
        internal readonly List<(ProtocolRequestContext Context, PublishDiagnosticsParams Parameters)> Diagnostics = new();
        internal readonly List<ProtocolRequestContext> DiagnosticsRefreshes = new(), InlayRefreshes = new(), CodeLensRefreshes = new();
        internal Func<ProtocolRequestContext, WatcherID, Slice<Watcher>, Exception?>? WatchFilesFunc;
        internal Func<TypeScript.Locale.Locale>? GetLocaleFunc;
        internal Action<ByteStringValue>? SetLocaleFunc = null;
        internal Func<bool>? IsActiveFunc = null;
        internal Func<ProtocolRequestContext, Exception?>? RefreshCodeLensFunc;
        internal readonly List<ByteStringValue> SetLocaleCalls = new();
        internal (ProtocolRequestContext Context, WatcherID ID, Slice<Watcher> Watchers)[] WatchFilesCalls()
        {
            lock (gate)
                return WatchCalls.ToArray();
        }

        internal (ProtocolRequestContext Context, WatcherID ID)[] UnwatchFilesCalls()
        {
            lock (gate)
                return UnwatchCalls.ToArray();
        }

        internal (ProtocolRequestContext Context, PublishDiagnosticsParams Parameters)[] PublishDiagnosticsCalls()
        {
            lock (gate)
                return Diagnostics.ToArray();
        }

        internal int RefreshDiagnosticsCallCount()
        {
            lock (gate)
                return DiagnosticsRefreshes.Count;
        }

        internal ProtocolRequestContext[] CodeLensRefreshCalls()
        {
            lock (gate)
                return CodeLensRefreshes.ToArray();
        }

        internal ProtocolRequestContext[] InlayHintsRefreshCalls()
        {
            lock (gate)
                return InlayRefreshes.ToArray();
        }

        internal ByteStringValue[] LocaleCalls()
        {
            lock (gate)
                return SetLocaleCalls.ToArray();
        }

        public Exception? WatchFiles(ProtocolRequestContext context, WatcherID id, Slice<Watcher> watchers)
        {
            lock (gate)
                WatchCalls.Add((context, id, watchers));
            return WatchFilesFunc?.Invoke(context, id, watchers);
        }

        public Exception? UnwatchFiles(ProtocolRequestContext context, WatcherID id)
        {
            lock (gate)
                UnwatchCalls.Add((context, id));
            return null;
        }

        private readonly object mapperExtensionsGate = new();
        private readonly List<ByteStringValue[]> mapperExtensionCalls = new();
        internal ByteStringValue[][] RegisterContentMapperExtensionsCalls()
        {
            lock (mapperExtensionsGate)
                return mapperExtensionCalls.ToArray();
        }

        public Exception? RegisterContentMapperExtensions(ProtocolRequestContext context, Slice<ByteStringValue> extensions)
        {
            var retained = new ByteStringValue[extensions.Length];
            int index = 0;
            foreach (var extension in extensions)
                retained[index++] = extension;
            lock (mapperExtensionsGate)
                mapperExtensionCalls.Add(retained);
            return null;
        }

        public Exception? RefreshDiagnostics(ProtocolRequestContext context)
        {
            lock (gate)
                DiagnosticsRefreshes.Add(context);
            return null;
        }

        public Exception? PublishDiagnostics(ProtocolRequestContext context, PublishDiagnosticsParams parameters)
        {
            lock (gate)
                Diagnostics.Add((context, parameters));
            return null;
        }

        public Exception? RefreshInlayHints(ProtocolRequestContext context)
        {
            lock (gate)
                InlayRefreshes.Add(context);
            return null;
        }

        public Exception? RefreshCodeLens(ProtocolRequestContext context)
        {
            lock (gate)
                CodeLensRefreshes.Add(context);
            return RefreshCodeLensFunc?.Invoke(context);
        }

        internal readonly record struct ProgressCall(TypeScript.Scanner.Message Message, object? [] Arguments);
        private readonly List<ProgressCall> progressStarts = new();
        private readonly List<ProgressCall> progressFinishes = new();
        internal ProgressCall[] ProgressStartCalls()
        {
            lock (gate)
                return progressStarts.ToArray();
        }

        internal ProgressCall[] ProgressFinishCalls()
        {
            lock (gate)
                return progressFinishes.ToArray();
        }

        public void ProgressStart(TypeScript.Scanner.Message message, params object? [] arguments)
        {
            lock (gate)
                progressStarts.Add(new(message, arguments));
        }

        public void ProgressFinish(TypeScript.Scanner.Message message, params object? [] arguments)
        {
            lock (gate)
                progressFinishes.Add(new(message, arguments));
        }

        public Exception? SendTelemetry(ProtocolRequestContext context, Telemetry value) => null;
        public bool IsActive() => IsActiveFunc?.Invoke() ?? false;
        public void SetLocale(ByteStringValue value)
        {
            lock (gate)
                SetLocaleCalls.Add(value);
            SetLocaleFunc?.Invoke(value);
        }

        private readonly List<TypeScript.Locale.Locale> localeReads = new();
        internal TypeScript.Locale.Locale[] GetLocaleCalls()
        {
            lock (gate)
                return localeReads.ToArray();
        }

        public TypeScript.Locale.Locale GetLocale()
        {
            var value = GetLocaleFunc?.Invoke() ?? default;
            lock (gate)
                localeReads.Add(value);
            return value;
        }
    }

    internal sealed class NpmExecutor(ProjectTestFileSystem fs, string[] registry, Dictionary<string, string> packages) : INpmExecutor
    {
        internal readonly record struct InstallCall(RootedDirectoryPath Cwd, string[] Args);
        private readonly object callsGate = new();
        private readonly List<InstallCall> calls = new();
        internal InstallCall[] NpmInstallCalls()
        {
            lock (callsGate)
                return calls.ToArray();
        }

        public (Slice<byte> Output, Exception? Error) NpmInstall(CancellationToken context, RootedDirectoryPath cwd, Slice<ByteStringValue> arguments)
        {
            var recordedArguments = new string[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                recordedArguments[i] = arguments[i].Materialize()!;
            lock (callsGate)
                calls.Add(new(cwd, recordedArguments));
            if (arguments.Length < 3)
                return (default, new InvalidOperationException(LogFormat.Sprintf("unexpected npm install: %s %v", cwd, arguments)));
            if (arguments.Length == 3 && arguments[2].EqualsBytes("types-registry@latest"u8))
            {
                var names = new List<string>(registry);
                foreach (var name in packages.Keys)
                    if (!names.Contains(name, StringComparer.Ordinal))
                        names.Add(name);
                string configuration = "\"latest\":\"1.3.0\",\"ts2.0\":\"1.0.0\",\"ts2.1\":\"1.0.0\",\"ts2.2\":\"1.2.0\",\"ts2.3\":\"1.3.0\",\"ts2.4\":\"1.3.0\",\"ts2.5\":\"1.3.0\",\"ts2.6\":\"1.3.0\",\"ts2.7\":\"1.3.0\"";
                string content = "{\n  \"entries\": {" + string.Join(",", names.Select(name => "\n    \"" + name + "\": {" + string.Join(",", configuration.Split(',').Select(item => "\n      " + item.Replace(":", ": "))) + "\n    }")) + "\n  }\n}";
                return (default, fs.WriteFile(cwd.ResolveFile(ByteStringValue.FromString("node_modules/types-registry/index.json")), content));
            }

            for (int i = 2; i < arguments.Length && !arguments[i].StartsWith("--"u8); i++)
            {
                string package = arguments[i].Materialize()!;
                int version = package.LastIndexOf('@');
                if (version > 6)
                    package = package[..version];
                string name = package[7..];
                if (!packages.TryGetValue(name, out var content))
                    return (default, new InvalidOperationException("content not provided for " + name));
                var error = fs.WriteFile(cwd.ResolveFile(ByteStringValue.FromString("node_modules/" + package + "/index.d.ts")), content);
                if (error is not null)
                    return (default, error);
            }

            return (default, null);
        }
    }
}
