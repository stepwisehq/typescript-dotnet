// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/server.go:60–102,171–247; ignored coherent-server owner draft.
using System.Threading.Channels;
using TypeScript.Collections;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using TypeScript.Vfs;
using LocaleValue = TypeScript.Locale.Locale;
using ParseCache = TypeScript.ProjectSystem.RefCountCache<TypeScript.ProjectSystem.ParseCacheKey, TypeScript.Scanner.SourceFile, TypeScript.ProjectSystem.IFileHandle>;

namespace TypeScript.Lsp;
internal sealed partial class Server : IProjectClient, TypeScript.ProjectSystem.Ata.INpmExecutor
{
    private readonly ILspReader r;
    private readonly ILspWriter w;
    internal readonly Stream stderr;
    internal readonly ServerLogger logger;
    internal int initStarted;
    private int clientSeq;
    internal readonly DynamicQueue<RequestMessage> requestQueue = new();
    internal readonly DynamicQueue<TypeScript.Lsp.Protocol.Message> outgoingQueue = new();
    private readonly Dictionary<IDKey, PendingClientRequest> pendingClientRequests = new();
    private readonly object pendingClientRequestsMu = new();
    private readonly Dictionary<IDKey, Channel<ResponseMessage>> pendingServerRequests = new();
    private readonly object pendingServerRequestsMu = new();
    private readonly RootedDirectoryPath cwd;
    private readonly IFS fs;
    private readonly RootedDirectoryPath defaultLibraryPath;
    private readonly RootedDirectoryPath typingsLocation;
    private InitializeParams? initializeParams;
    private InitializationOptions? initializationOptions;
    private ResolvedClientCapabilities clientCapabilities;
    private PositionEncodingKind positionEncoding;
    private readonly ReaderWriterLockSlim localeMu = new();
    private LocaleValue locale;
    private LocaleValue initLocale;
    private TypeScript.Lsp.Watching.Watcher? builtinWatcher;
    private bool watchEnabled;
    private bool telemetryEnabled;
    private readonly SyncSet<WatcherID> watchers = new();
    private readonly object contentMapperRegistrationMu = new();
    private bool contentMapperExtensionsRegistered;
    private long lastRequestTimeMs;
    internal Session? session;
    private readonly TaskCompletionSource initComplete = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CompilerOptions? compilerOptionsForInferredProjects;
    private readonly ParseCache? parseCache;
    private readonly Func<ProtocolRequestContext, ByteStringValue, Slice<ByteStringValue>, (byte[]? Output, Exception? Error)>? npmInstall;
    private readonly Func<Slice<ByteStringValue>, ByteStringValue, Stream, (Stream? Process, Exception? Error)>? spawn;
    private readonly TimeSpan progressDelay;
    private ProjectLoadingProgress? projectProgress;
    private readonly Action<long>? startWatchdog;
    private DiagnosticFlakeLogLevel flakeLogging;
    internal CancellationToken backgroundCtx;
    private readonly record struct PendingClientRequest(RequestMessage Request, CancellationTokenSource Cancel);
    private Server(ServerOptions options)
    {
        if (options.Cwd.AsValue().Length == 0)
            throw new InvalidOperationException("Cwd is required");
        r = options.In;
        w = options.Out;
        stderr = options.Err;
        cwd = options.Cwd;
        fs = options.FS;
        defaultLibraryPath = options.DefaultLibraryPath;
        typingsLocation = options.TypingsLocation;
        parseCache = options.ParseCache;
        npmInstall = options.NpmInstall;
        spawn = options.Spawn;
        startWatchdog = options.SetParentProcessID;
        progressDelay = options.ProgressDelay;
        logger = ServerLogger.NewLogger(this);
    }

    internal static Server NewServer(ServerOptions options) => new(options);
    internal Session Session() => session!;
    internal Task InitComplete() => initComplete.Task;
    internal LocaleValue GetLocale()
    {
        localeMu.EnterReadLock();
        try
        {
            return locale;
        }
        finally
        {
            localeMu.ExitReadLock();
        }
    }
}
