// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete mapped owner; pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using Diagnostics = TypeScript.Scanner.Diagnostics;
using TypeScript.ContentMapper;
using TypeScript.LanguageServices;
using IProject = TypeScript.LanguageServices.IProject;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem.Logging;
using TypeScript.ProjectSystem.Ata;
using TypeScript.Vfs;
using TypeScript.Collections;
using Diagnostic = TypeScript.Scanner.Diagnostic;
using TelemetryEvent = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace TypeScript.ProjectSystem;
internal sealed partial class Session : INpmExecutor
{
    internal static readonly Exception ErrNoProjectForUnknownScriptKind = new InvalidOperationException("no project for unknown script kind");
    internal SnapshotHost SnapshotHost = null!;
    internal SessionOptions options = null!;
    internal ILogger logger = null!;
    internal ProtocolRequestContext backgroundCtx = null!;
    private CancellationTokenSource backgroundCancel = null!;
    internal CaseSensitivity caseSensitivity;
    internal IProjectClient? client;
    private long startTime;
    internal INpmExecutor? npmExecutor;
    internal OverlayFS fs = null!;
    private Timings contentMapperTimings;
    private readonly object contentMapperTimingsMu = new();
    private Slice<ByteStringValue> registeredContentMapperExtensions;
    private ulong registeredContentMapperSnapshotID;
    private readonly object contentMapperRegistrationMu = new();
    internal UserPreferences initialUserPreferences, workspaceUserPreferences;
    internal CompilerOptions? compilerOptionsForInferredProjects;
    private TypingsInstaller? typingsInstaller;
    private Background.Queue backgroundQueue = null!;
    internal Snapshot? snapshot;
    private readonly ReaderWriterLockSlim snapshotMu = new(LockRecursionPolicy.NoRecursion);
    private readonly object snapshotUpdateMu = new();
    private CancellationTokenSource? scheduledSnapshotUpdateCancel, diagnosticsRefreshCancel, performanceTelemetryCancel;
    private Action? warmAutoImportCancel;
    private ulong scheduledSnapshotUpdateGeneration, diagnosticsRefreshGeneration;
    private readonly object scheduledSnapshotUpdateMu = new(), diagnosticsRefreshMu = new(), warmAutoImportMu = new();
    private bool pendingUserConfigChanges;
    private readonly object configureMu = new(), userConfigRWMu = new(), pendingATAChangesMu = new();
    internal readonly object pendingFileChangesMu = new();
    internal Slice<FileChange> pendingFileChanges;
    private Dictionary<ID, ATAStateChange> pendingATAChanges = new();
    private readonly object idleCacheCleanMu = new();
    private IdleTimer? idleCacheCleanTimer;
    private int idleCacheCleanPending;
    private bool idleCacheCleanClosed;
    private readonly SyncMap<ID, bool> seenProjects = new();
    private WatchRegistry watches = null!;
    private int globalDiagPublishPending;
    // session.go:213.
    internal static Session NewSession(SessionInit init)
    {
        var host = TypeScript.ProjectSystem.SnapshotHost.NewSnapshotHost(init);
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(init.BackgroundContext.Cancellation);
        var session = new Session
        {
            SnapshotHost = host,
            options = init.Options,
            logger = init.Logger ?? Logger.NewNopLogger(),
            backgroundCtx = init.BackgroundContext.WithCancellationToken(cancellation.Token),
            backgroundCancel = cancellation,
            caseSensitivity = host.caseSensitivity,
            client = init.Client,
            npmExecutor = init.NpmExecutor,
            fs = new OverlayFS(host.fs, new Dictionary<PathKey, Overlay>(), init.Options.PositionEncoding),
            backgroundQueue = Background.Queue.NewQueue(),
            startTime = System.Diagnostics.Stopwatch.GetTimestamp(),
            snapshot = host.NewRootSnapshot(0, ProtocolContexts.GetClientCapabilities(init.BackgroundContext).Workspace.DidChangeWatchedFiles.RelativePatternSupport),
            initialUserPreferences = LsUtil.NewDefaultUserPreferences(),
            workspaceUserPreferences = LsUtil.NewDefaultUserPreferences(),
            watches = new WatchRegistry()
        };
        if (init.Options.TypingsLocation.AsValue().Length != 0 && init.NpmExecutor is not null)
            session.typingsInstaller = TypingsInstaller.NewTypingsInstaller(new TypingsInstallerOptions { TypingsLocation = init.Options.TypingsLocation, ThrottleLimit = 5 }, session.FS(), session);
        if (host.contentMapperHost is not null)
            session.contentMapperTimings = host.contentMapperHost.Timings();
        return session;
    }

    // session.go:256/261/265.
    internal IFS FS() => fs;
    internal RootedDirectoryPath GetCurrentDirectory() => options.CurrentDirectory;
    internal RootedDirectoryPath DefaultLibraryPath() => options.DefaultLibraryPath;
    // session.go:270/276/280.
    internal UserPreferences Config()
    {
        lock (userConfigRWMu)
            return workspaceUserPreferences;
    }

    private ProtocolRequestContext BackgroundContext() => WithCurrentLocale(backgroundCtx);
    internal ProtocolRequestContext WithCurrentLocale(ProtocolRequestContext context) => client is null ? context : context.WithLocale(client.GetLocale());
    // session.go:288. Preference value is copied, while slice/map headers retain upstream owners.
    internal void Configure(UserPreferences configuration)
    {
        lock (configureMu)
        {
            UserPreferences old;
            lock (userConfigRWMu)
            {
                pendingUserConfigChanges = true;
                old = workspaceUserPreferences;
                workspaceUserPreferences = configuration;
            }

            if (configuration.Locale.Length != 0)
            {
                var oldLocale = client!.GetLocale();
                client.SetLocale(ByteStringValue.FromString(configuration.Locale));
                var nextLocale = client.GetLocale();
                if (oldLocale.String() != nextLocale.String())
                    SnapshotHost.contentMapperHost?.SetLocale(nextLocale);
            }

            RefreshInlayHintsIfNeeded(old, configuration);
            RefreshCodeLensIfNeeded(old, configuration);
            RefreshDiagnosticsIfNeeded(old, configuration);
            RefreshATAIfNeeded(old, configuration);
        }
    }

    // session.go:318.
    internal void InitializeWithUserConfig(UserPreferences configuration)
    {
        initialUserPreferences = configuration;
        Configure(configuration);
    }

    // session.go:323.
    internal void DidOpenFile(ProtocolRequestContext context, DocumentUri uri, int version, ByteStringValue content, LanguageKind languageKind)
    {
        CancelWarmAutoImportCache();
        ScheduleIdleCacheClean();
        CancelScheduledSnapshotUpdate();
        lock (snapshotUpdateMu)
        {
            (FileChangeSummary Changes, Dictionary<PathKey, Overlay>? Overlays) flushed;
            lock (pendingFileChangesMu)
            {
                pendingFileChanges = pendingFileChanges.Append(new FileChange { Kind = FileChangeKind.Open, URI = uri, Version = version, Content = content, LanguageKind = languageKind });
                flushed = FlushChangesLocked(context);
            }

            UpdateSnapshot(context, flushed.Overlays, new SnapshotChange { reason = UpdateReason.DidOpenFile, fileChanges = flushed.Changes, ResourceRequest = new ResourceRequest { Documents = Slice<DocumentUri>.FromArray([uri]) } });
        }
    }

    // session.go:349.
    internal void SetContentMapperContributions(ProtocolRequestContext context, ContentMapperContributions contributions, Slice<DocumentUri> documents)
    {
        if (!options.RunExternalCode)
            return;
        CancelScheduledSnapshotUpdate();
        lock (snapshotUpdateMu)
        {
            (FileChangeSummary Changes, Dictionary<PathKey, Overlay>? Overlays) flushed;
            lock (pendingFileChangesMu)
                flushed = FlushChangesLocked(context);
            UpdateSnapshot(context, flushed.Overlays, new SnapshotChange { reason = UpdateReason.DidChangeContentMapperContributions, fileChanges = flushed.Changes, contentMapperContributions = contributions, ResourceRequest = new ResourceRequest { ConfiguredProjectDocuments = documents } });
            _ = UpdateContentMapperRegistrations(context, Snapshot());
        }
    }

    // session.go:370/383.
    internal void DidCloseFile(ProtocolRequestContext context, DocumentUri uri)
    {
        CancelWarmAutoImportCache();
        ScheduleIdleCacheClean();
        lock (pendingFileChangesMu)
            pendingFileChanges = pendingFileChanges.Append(new FileChange { Kind = FileChangeKind.Close, URI = uri });
        ScheduleSnapshotUpdate(UpdateReason.DidCloseFile);
    }

    internal void DidChangeFile(ProtocolRequestContext context, DocumentUri uri, int version, Slice<TextDocumentContentChangePartialOrWholeDocument> changes)
    {
        CancelWarmAutoImportCache();
        ScheduleIdleCacheClean();
        lock (pendingFileChangesMu)
            pendingFileChanges = pendingFileChanges.Append(new FileChange { Kind = FileChangeKind.Change, URI = uri, Version = version, Changes = changes });
        if (IsContentMapperFile(uri))
            ScheduleDiagnosticsRefresh(TimeSpan.Zero);
        else
            CancelDiagnosticsRefresh();
    }

    // session.go:409.
    private bool IsContentMapperFile(DocumentUri uri)
    {
        var current = Snapshot();
        var extensions = GoSlices.Clone(current.ConfigFileRegistry.ContentMappers().Extensions).Append(current.inferredProjectContentMapperExtensions);
        foreach (var extension in extensions)
            if (uri.FileName().AsValue().Length > extension.Length && uri.FileName().AsValue().EndsWith(extension))
                return true;
        return false;
    }

    // session.go:417.
    internal void DidSaveFile(ProtocolRequestContext context, DocumentUri uri)
    {
        ScheduleIdleCacheClean();
        lock (pendingFileChangesMu)
            pendingFileChanges = pendingFileChanges.Append(new FileChange { Kind = FileChangeKind.Save, URI = uri });
    }

    // session.go:506.
    internal void DidChangeCompilerOptionsForInferredProjects(ProtocolRequestContext context, CompilerOptions value)
    {
        compilerOptionsForInferredProjects = value;
        UpdateSnapshot(context, fs.Overlays(), new SnapshotChange { reason = UpdateReason.DidChangeCompilerOptionsForInferredProjects, compilerOptionsForInferredProjects = value });
    }
}

// session.go:87. Shared immutable initialization ownership; every optional
// injected service remains explicit, including trust-gated process spawning.
internal sealed class SessionInit
{
    internal ProtocolRequestContext BackgroundContext = null!;
    internal SessionOptions Options = null!;
    internal IFS FS = null!;
    internal IProjectClient? Client;
    internal ILogger? Logger;
    internal INpmExecutor? NpmExecutor;
    internal ISpawner? Spawner;
    internal Action<ByteStringValue>? ContentMapperLogger;
    internal RefCountCache<ParseCacheKey, SourceFile, IFileHandle>? ParseCache;
    internal RefCountCache<ContentMappedParseCacheKey, SourceFiles, ValueTuple>? ContentMappedParseCache;
}

internal sealed partial class Session
{
    // session.go:202. Only actual missing trust or spawner disables the process host.
    internal static IHost? NewContentMapperHost(SessionInit init)
    {
        if (!init.Options.RunExternalCode || init.Spawner is null)
            return null;
        var locale = init.Client is { } client ? client.GetLocale() : TypeScript.Locale.Locales.Default;
        return ProcessHosts.NewHostWithOptions(init.BackgroundContext.Cancellation, init.Spawner, locale, new HostOptions(init.ContentMapperLogger));
    }
}

// session.go:57. Shared contribution owner retains original slice headers.
internal sealed class ContentMapperContributions
{
    internal Slice<Mapper> Mappers;
    internal Slice<ByteStringValue> Extensions;
}

internal sealed partial class Session
{
    // session.go:1055. Returning this owner does not add a caller reference.
    internal Snapshot Snapshot()
    {
        snapshotMu.EnterReadLock();
        try
        {
            return snapshot!;
        }
        finally
        {
            snapshotMu.ExitReadLock();
        }
    }

    // session.go:1065. Retain is atomic with canonical snapshot selection.
    private Snapshot GetSnapshot(ProtocolRequestContext context, ResourceRequest request, bool callerRef)
    {
        lock (snapshotUpdateMu)
        {
            CancelScheduledSnapshotUpdate();
            var(changes, overlays, ataChanges, configuration) = FlushChanges(context);
            if (!changes.IsEmpty() || ataChanges.Count != 0 || configuration is not null)
                return UpdateSnapshot(context, overlays, new SnapshotChange { reason = UpdateReason.RequestedLanguageServicePendingChanges, fileChanges = changes, ataChanges = ataChanges, newConfig = configuration, ResourceRequest = request }, callerRef)!;
            UpdateReason reason = UpdateReason.Unknown;
            snapshotMu.EnterReadLock();
            try
            {
                var current = snapshot!;
                if (request.Projects.Length != 0)
                    reason = UpdateReason.RequestedLanguageServiceProjectDirty;
                else if (request.ProjectTree is not null)
                    reason = UpdateReason.RequestedLoadProjectTree;
                else if (request.AutoImports.Value.Length != 0)
                    reason = UpdateReason.RequestedLanguageServiceWithAutoImports;
                else
                {
                    foreach (var uri in request.Documents)
                    {
                        var project = current.GetDefaultProject(uri);
                        if (project is null)
                            reason = UpdateReason.RequestedLanguageServiceProjectNotLoaded;
                        else if (project.dirty)
                            reason = UpdateReason.RequestedLanguageServiceProjectDirty;
                    }

                    if (reason == UpdateReason.Unknown)
                        foreach (var uri in request.ConfiguredProjectDocuments)
                            if (current.IsOpenFile(uri.FileName()))
                            {
                                var project = current.GetDefaultProject(uri);
                                if (project is null)
                                    reason = UpdateReason.RequestedLanguageServiceProjectNotLoaded;
                                else if (project.dirty)
                                    reason = UpdateReason.RequestedLanguageServiceProjectDirty;
                            }
                            else
                                reason = UpdateReason.RequestedLanguageServiceForFileNotOpen;
                }

                if (reason == UpdateReason.Unknown)
                {
                    if (callerRef)
                        current.Ref();
                    return current;
                }
            }
            finally
            {
                snapshotMu.ExitReadLock();
            }

            return UpdateSnapshot(context, overlays, new SnapshotChange { reason = reason, ResourceRequest = request }, callerRef)!;
        }
    }

    // session.go:1124. The wrapped sentinel remains available through InnerException.
    private (Snapshot? Snapshot, Project? Project, LanguageService? Service, Exception? Error) GetSnapshotAndDefaultProject(ProtocolRequestContext context, DocumentUri uri, bool callerRef)
    {
        var current = GetSnapshot(context, new ResourceRequest { Documents = Slice<DocumentUri>.FromArray([uri]) }, callerRef);
        var project = current.GetDefaultProject(uri);
        if (project is null)
        {
            if (callerRef)
                current.Deref();
            var text = Logging.LogFormat.Sprintf("no project found for URI %s", uri.Value);
            if (current.GetFile(uri.FileName())is { } file && file.Kind() == ScriptKind.Unknown)
                return (null, null, null, new InvalidOperationException(ErrNoProjectForUnknownScriptKind.Message + ": " + text, ErrNoProjectForUnknownScriptKind));
            return (null, null, null, new InvalidOperationException(text));
        }

        return (current, project, LanguageService.NewLanguageService(project.ID(), project.GetProgram()!, current, uri.FileName().AsValue()), null);
    }

    // session.go:1145.
    internal (LanguageService? Service, Exception? Error) GetLanguageService(ProtocolRequestContext context, DocumentUri uri)
    {
        var(_, _, service, error) = GetSnapshotAndDefaultProject(context, uri, false);
        return (service, error);
    }

    // session.go:1153. Original symlink-project TODO remains unchanged.
    internal (Project? Project, LanguageService? Service, Slice<IProject> Projects, Exception? Error) GetLanguageServiceAndProjectsForFile(ProtocolRequestContext context, DocumentUri uri)
    {
        var(current, project, service, error) = GetSnapshotAndDefaultProject(context, uri, false);
        return error is not null ? (null, null, default, error) : (project, service, current!.GetLanguageServiceProjectsContainingFile(uri), null);
    }

    // session.go:1166. Original symlink-project TODO remains unchanged.
    internal (Slice<IProject> Projects, Exception? Error) GetProjectsForFile(ProtocolRequestContext context, DocumentUri uri)
    {
        var current = GetSnapshot(context, new ResourceRequest { ConfiguredProjectDocuments = Slice<DocumentUri>.FromArray([uri]) }, false);
        return (current.GetLanguageServiceProjectsContainingFile(uri), null);
    }

    // session.go:1185.
    internal Slice<LanguageService> GetLanguageServicesForDocumentsLoadingProjectTree(ProtocolRequestContext context, Slice<DocumentUri> uris)
    {
        var current = GetSnapshot(context, new ResourceRequest { Documents = uris, ProjectTree = new ProjectTreeRequest() }, false);
        var active = uris.Length == 0 ? default : uris[0].FileName().AsValue();
        var projects = current.ProjectCollection.LanguageServiceProjects();
        var services = Slice<LanguageService>.Make(0, projects.Length);
        foreach (var project in projects)
            if (project.GetProgram()is { } program)
                services = services.Append(LanguageService.NewLanguageService(project.ID(), program, current, active));
        return services;
    }

    // session.go:1214.
    internal LanguageService? GetLanguageServiceForProjectWithFile(ProtocolRequestContext context, Project project, DocumentUri uri)
    {
        var current = GetSnapshot(context, new ResourceRequest { Projects = Slice<ID>.FromArray([project.ID()]) }, false);
        var updated = current.ProjectCollection.GetProject(project.ID());
        if (updated is null || !updated.HasFile(uri.FileName()))
            return null;
        return LanguageService.NewLanguageService(updated.ID(), updated.GetProgram()!, current, uri.FileName().AsValue());
    }

    // session.go:1237/1256.
    internal void WithSnapshotLoadingProjectTree(ProtocolRequestContext context, HashSet<PathKey>? trees, Action<Snapshot> callback)
    {
        var current = GetSnapshot(context, new ResourceRequest { ProjectTree = new ProjectTreeRequest { referencedProjects = trees } }, true);
        try
        {
            callback(current);
        }
        finally
        {
            current.Deref();
        }
    }

    internal void WithSnapshotForDocument(ProtocolRequestContext context, DocumentUri uri, Action<Snapshot> callback)
    {
        var current = GetSnapshot(context, new ResourceRequest { Documents = Slice<DocumentUri>.FromArray([uri]) }, true);
        try
        {
            callback(current);
        }
        finally
        {
            current.Deref();
        }
    }

    // session.go:1277.
    internal (LanguageService? Service, Exception? Error) GetCurrentLanguageServiceWithAutoImports(ProtocolRequestContext context, DocumentUri uri)
    {
        var current = GetSnapshot(context, new ResourceRequest { Documents = Slice<DocumentUri>.FromArray([uri]), AutoImports = uri }, false);
        var project = current.GetDefaultProject(uri);
        return project is null ? (null, new InvalidOperationException(Logging.LogFormat.Sprintf("no project found for URI %s", uri.Value))) : (LanguageService.NewLanguageService(project.ID(), project.GetProgram()!, current, uri.FileName().AsValue()), null);
    }

    // session.go:1299. The retained snapshot transfers into the returned async closure.
    internal (Func<Exception?>? Work, Exception? Error) WithLanguageServiceAndSnapshot(ProtocolRequestContext context, DocumentUri uri, Func<LanguageService, Snapshot, (Func<Exception?>? Work, Exception? Error)> callback)
    {
        var(current, _, service, error) = GetSnapshotAndDefaultProject(context, uri, true);
        if (error is not null)
            return (null, error);
        var(work, callbackError) = callback(service!, current!);
        if (callbackError is not null || work is null)
        {
            current!.Deref();
            return (null, callbackError);
        }

        return (() =>
        {
            try
            {
                return work();
            }
            finally
            {
                current!.Deref();
            }
        }, null);
    }

    // session.go:1327.
    internal (LanguageService? Service, Exception? Error) GetLanguageServiceWithAutoImports(ProtocolRequestContext context, Snapshot baseline, DocumentUri uri)
    {
        var next = SnapshotHost.CloneSnapshotWithAutoImports(context, baseline, uri, logger);
        var project = next.GetDefaultProject(uri);
        if (project is null)
        {
            next.Deref();
            return (null, new InvalidOperationException(Logging.LogFormat.Sprintf("no project found for URI %s", uri.Value)));
        }

        TryAdoptSnapshotChangeInBackground(baseline, next);
        return (LanguageService.NewLanguageService(project.ID(), project.GetProgram()!, next, uri.FileName().AsValue()), null);
    }

    // session.go:1343. The clone's initial reference transfers into the queued adoption.
    private void TryAdoptSnapshotChangeInBackground(Snapshot baseline, Snapshot next) => backgroundQueue.Enqueue(BackgroundContext().Cancellation, _ => AdoptSnapshotChange(baseline, next));
}

internal sealed partial class Session
{
    // Go time.After accepts every nonpositive duration as immediately ready and
    // the full positive time.Duration range. CLR WaitOne accepts only int32-ms
    // intervals; bounded waits retain one monotonic deadline and cancellation.
    private static bool WaitForCancellation(CancellationToken cancellation, TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
            return cancellation.IsCancellationRequested;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        TimeSpan remaining = delay;
        TimeSpan maximumWait = TimeSpan.FromMilliseconds(int.MaxValue);
        while (true)
        {
            if (cancellation.WaitHandle.WaitOne(remaining > maximumWait ? maximumWait : remaining))
                return true;
            remaining = delay - System.Diagnostics.Stopwatch.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero)
                return false;
        }
    }

    // session.go:514/520. Generation checks preserve cancellation versus delayed execution.
    internal void ScheduleDiagnosticsRefresh() => ScheduleDiagnosticsRefresh(options.DebounceDelay);
    private void ScheduleDiagnosticsRefresh(TimeSpan delay)
    {
        lock (diagnosticsRefreshMu)
        {
            if (diagnosticsRefreshCancel is not null)
            {
                diagnosticsRefreshCancel.Cancel();
                logger.Log("Delaying scheduled diagnostics refresh...");
            }
            else
                logger.Log("Scheduling new diagnostics refresh...");
            var context = BackgroundContext();
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
            var generation = ++diagnosticsRefreshGeneration;
            diagnosticsRefreshCancel = cancel;
            backgroundQueue.Enqueue(cancel.Token, token =>
            {
                try
                {
                    if (WaitForCancellation(token, delay))
                        return;
                    lock (diagnosticsRefreshMu)
                    {
                        if (diagnosticsRefreshGeneration != generation)
                            return;
                        diagnosticsRefreshCancel = null;
                    }

                    if (options.LoggingEnabled)
                        logger.Log("Running scheduled diagnostics refresh");
                    if (client!.RefreshDiagnostics(BackgroundContext())is { } error && options.LoggingEnabled)
                        logger.Logf("Error refreshing diagnostics: %v", error);
                }
                finally
                {
                    cancel.Cancel();
                }
            });
        }
    }

    // session.go:578.
    private void CancelDiagnosticsRefresh()
    {
        lock (diagnosticsRefreshMu)
            if (diagnosticsRefreshCancel is not null)
            {
                diagnosticsRefreshCancel.Cancel();
                logger.Log("Canceled scheduled diagnostics refresh");
                diagnosticsRefreshCancel = null;
                diagnosticsRefreshGeneration++;
            }
    }

    // session.go:590.
    internal void ScheduleSnapshotUpdate(UpdateReason reason)
    {
        lock (scheduledSnapshotUpdateMu)
        {
            if (scheduledSnapshotUpdateCancel is not null)
            {
                scheduledSnapshotUpdateCancel.Cancel();
                if (options.LoggingEnabled)
                    logger.Log("Delaying scheduled snapshot update...");
            }
            else if (options.LoggingEnabled)
                logger.Log("Scheduling new snapshot update...");
            var context = BackgroundContext();
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
            var generation = ++scheduledSnapshotUpdateGeneration;
            scheduledSnapshotUpdateCancel = cancel;
            backgroundQueue.Enqueue(cancel.Token, token =>
            {
                try
                {
                    if (WaitForCancellation(token, options.DebounceDelay))
                        return;
                    lock (scheduledSnapshotUpdateMu)
                    {
                        if (scheduledSnapshotUpdateGeneration != generation)
                            return;
                        scheduledSnapshotUpdateCancel = null;
                    }

                    if (options.LoggingEnabled)
                        logger.Log("Running scheduled snapshot update");
                    lock (snapshotUpdateMu)
                    {
                        var(changes, overlays, ata, configuration) = FlushChanges(context.WithCancellationToken(token));
                        if (changes.IsEmpty() && ata.Count == 0 && configuration is null)
                            return;
                        UpdateSnapshot(context.WithCancellationToken(token), overlays, new SnapshotChange { reason = reason, fileChanges = changes, ataChanges = ata, newConfig = configuration });
                    }
                }
                finally
                {
                    cancel.Cancel();
                }
            });
        }
    }

    // session.go:652/668.
    private void CancelScheduledSnapshotUpdate()
    {
        lock (scheduledSnapshotUpdateMu)
            if (scheduledSnapshotUpdateCancel is not null)
            {
                scheduledSnapshotUpdateCancel.Cancel();
                if (options.LoggingEnabled)
                    logger.Log("Canceled scheduled snapshot update");
                scheduledSnapshotUpdateCancel = null;
                scheduledSnapshotUpdateGeneration++;
            }
    }

    private void CancelWarmAutoImportCache()
    {
        lock (warmAutoImportMu)
            if (warmAutoImportCancel is not null)
            {
                warmAutoImportCancel();
                warmAutoImportCancel = null;
            }
    }

    // time.AfterFunc/Timer.Stop adaptation: claim before callback; a successful
    // stop decrements pending exactly once, while a claimed callback owns Done.
    private sealed class IdleTimer(Session owner)
    {
        private int state;
        internal Timer Timer = null!;
        internal bool Stop()
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0)
                return false;
            Timer.Dispose();
            return true;
        }

        internal void Run()
        {
            if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
                return;
            try
            {
                lock (owner.idleCacheCleanMu)
                    if (ReferenceEquals(owner.idleCacheCleanTimer, this))
                        owner.idleCacheCleanTimer = null;
                lock (owner.snapshotUpdateMu)
                {
                    owner.CancelScheduledSnapshotUpdate();
                    var context = owner.BackgroundContext();
                    var(changes, overlays, ata, configuration) = owner.FlushChanges(context);
                    owner.UpdateSnapshot(context, overlays, new SnapshotChange { reason = UpdateReason.IdleCleanDiskCache, fileChanges = changes, ataChanges = ata, newConfig = configuration, cleanFileCache = true });
                    ThreadPool.UnsafeQueueUserWorkItem(static _ => GC.Collect(), (object? )null);
                }
            }
            finally
            {
                Timer.Dispose();
                lock (owner.idleCacheCleanMu)
                {
                    owner.idleCacheCleanPending--;
                    if (owner.idleCacheCleanPending == 0)
                        Monitor.PulseAll(owner.idleCacheCleanMu);
                }
            }
        }
    }

    // session.go:685. The timer callback is tracked independently of backgroundQueue.
    private void ScheduleIdleCacheClean()
    {
        lock (idleCacheCleanMu)
        {
            if (idleCacheCleanClosed)
                return;
            if (idleCacheCleanTimer?.Stop() == true)
                idleCacheCleanPending--;
            idleCacheCleanPending++;
            var timer = new IdleTimer(this);
            timer.Timer = new Timer(static state => ((IdleTimer)state!).Run(), timer, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            idleCacheCleanTimer = timer;
            timer.Timer.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
        }
    }

    // session.go:729/741.
    private void CancelIdleCacheClean()
    {
        lock (idleCacheCleanMu)
        {
            if (idleCacheCleanTimer?.Stop() == true)
            {
                idleCacheCleanPending--;
                if (idleCacheCleanPending == 0)
                    Monitor.PulseAll(idleCacheCleanMu);
            }

            idleCacheCleanTimer = null;
        }
    }

    private void CloseIdleCacheClean()
    {
        lock (idleCacheCleanMu)
        {
            idleCacheCleanClosed = true;
            if (idleCacheCleanTimer?.Stop() == true)
                idleCacheCleanPending--;
            idleCacheCleanTimer = null;
            while (idleCacheCleanPending != 0)
                Monitor.Wait(idleCacheCleanMu);
        }
    }

    // session.go:760/785.
    internal void StartPerformanceTelemetry()
    {
        if (!options.TelemetryEnabled)
            return;
        var context = BackgroundContext();
        var cancel = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
        performanceTelemetryCancel = cancel;
        backgroundQueue.Enqueue(cancel.Token, token =>
        {
            using var ticker = new PeriodicTimer(TimeSpan.FromMinutes(5));
            try
            {
                while (ticker.WaitForNextTickAsync(token).AsTask().GetAwaiter().GetResult())
                    if (client is not null && client.IsActive())
                        SendPerformanceTelemetry(context.WithCancellationToken(token));
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
            }
        });
    }

    private void StopPerformanceTelemetry()
    {
        if (performanceTelemetryCancel is not null)
        {
            performanceTelemetryCancel.Cancel();
            performanceTelemetryCancel = null;
        }
    }

    // session.go:1501.
    internal void WaitForBackgroundTasks()
    {
        CancelIdleCacheClean();
        backgroundQueue.Wait();
    }
}

internal sealed partial class Session
{
    // session.go:1359. Each branch consumes the clone's initial reference once.
    private void AdoptSnapshotChange(Snapshot baseline, Snapshot next)
    {
        Snapshot old;
        bool adopted;
        Timings timings = default;
        snapshotMu.EnterWriteLock();
        try
        {
            old = snapshot!;
            adopted = ReferenceEquals(old, baseline);
            if (adopted)
            {
                snapshot = next;
                old.Deref();
                timings = TakeContentMapperTimingDelta();
            }
        }
        finally
        {
            snapshotMu.ExitWriteLock();
        }

        if (adopted)
        {
            if (options.LoggingEnabled)
            {
                logger.Logf("Adopted snapshot %d (parent %d) as current session snapshot (replacing %d)", next.id, next.parentId, old.id);
                if (next.builderLogs is not null)
                    logger.Log(next.builderLogs.String());
                LogContentMapperTimings(timings);
            }
        }
        else
        {
            if (options.LoggingEnabled)
            {
                logger.Logf("Discarded snapshot %d (parent %d); session has moved on to snapshot %d", next.id, next.parentId, old.id);
                if (next.builderLogs is not null && next.builderLogs.String()is { Length: > 0 } logs)
                {
                    logger.Logf("--- Discarded snapshot %d builder logs (NOT adopted) ---", next.id);
                    logger.Log(logs);
                    logger.Logf("--- End discarded snapshot %d builder logs ---", next.id);
                }
            }

            next.Deref();
        }
    }

    // session.go:1399/1407/1411.
    internal void UpdateSnapshot(ProtocolRequestContext context, Dictionary<PathKey, Overlay>? overlays, SnapshotChange change) => _ = UpdateSnapshot(context, overlays, change, false);
    private Snapshot UpdateSnapshotRef(ProtocolRequestContext context, Dictionary<PathKey, Overlay>? overlays, SnapshotChange change) => UpdateSnapshot(context, overlays, change, true)!;
    private Snapshot? UpdateSnapshot(ProtocolRequestContext context, Dictionary<PathKey, Overlay>? overlays, SnapshotChange change, bool callerRef)
    {
        Snapshot old, next;
        Timings timings = default;
        snapshotMu.EnterWriteLock();
        try
        {
            old = snapshot!;
            if (!context.HasLocale)
                context = WithCurrentLocale(context);
            next = old.Clone(context, change, overlays, logger, client);
            if (next.apiError is not null)
            {
                if (callerRef)
                    return next;
                next.Deref();
                return null;
            }

            snapshot = next;
            if (callerRef)
                next.Ref();
            if (!ReferenceEquals(next, old))
            {
                old.Deref();
                timings = TakeContentMapperTimingDelta();
            }
        }
        finally
        {
            snapshotMu.ExitWriteLock();
        }

        if (typingsInstaller is not null && !Config().IsATADisabled())
            TriggerATAForUpdatedProjects(next);
        var background = BackgroundContext();
        backgroundQueue.Enqueue(background.Cancellation, token =>
        {
            var ctx = background.WithCancellationToken(token);
            if (options.LoggingEnabled)
            {
                logger.Logf("Adopted snapshot %d (parent %d) as current session snapshot (replacing %d)", next.id, next.parentId, old.id);
                if (next.builderLogs is not null)
                    logger.Log(next.builderLogs.String());
                LogProjectChanges(old, next);
                LogContentMapperTimings(timings);
                logger.Log("");
            }

            if (options.WatchEnabled && UpdateWatches(old, next)is { } watchError && options.LoggingEnabled)
                logger.Log(watchError);
            _ = UpdateContentMapperRegistrations(ctx, next);
            PublishProgramDiagnostics(old, next);
            SendProjectInfoTelemetryForNewProjects(old, next);
            WarmAutoImportCache(ctx, change, old, next);
        });
        return next;
    }

    // session.go:1478.
    private Timings TakeContentMapperTimingDelta()
    {
        if (SnapshotHost.contentMapperHost is null)
            return default;
        var current = SnapshotHost.contentMapperHost.Timings();
        lock (contentMapperTimingsMu)
        {
            var delta = current.Since(contentMapperTimings);
            contentMapperTimings = current;
            return delta;
        }
    }

    // session.go:1708. Cancellation precedes queue drain and canonical release.
    internal void Close()
    {
        CancelScheduledSnapshotUpdate();
        CancelDiagnosticsRefresh();
        CancelWarmAutoImportCache();
        CloseIdleCacheClean();
        StopPerformanceTelemetry();
        backgroundCancel.Cancel();
        backgroundQueue.Close();
        lock (snapshotUpdateMu)
        {
            Snapshot? old;
            snapshotMu.EnterWriteLock();
            try
            {
                old = snapshot;
                snapshot = null;
            }
            finally
            {
                snapshotMu.ExitWriteLock();
            }

            old?.Deref();
            SnapshotHost.Close();
        }
    }

    // session.go:1740. Swap the pending ATA map before processing changes.
    private (FileChangeSummary Changes, Dictionary<PathKey, Overlay>? Overlays, Dictionary<ID, ATAStateChange> ATA, UserPreferences? Configuration) FlushChanges(ProtocolRequestContext context)
    {
        lock (pendingFileChangesMu)
            lock (pendingATAChangesMu)
            {
                var ata = pendingATAChanges;
                pendingATAChanges = new();
                var(changes, overlays) = FlushChangesLocked(context);
                lock (userConfigRWMu)
                {
                    UserPreferences? configuration = pendingUserConfigChanges ? workspaceUserPreferences : null;
                    pendingUserConfigChanges = false;
                    return (changes, overlays, ata, configuration);
                }
            }
    }

    // session.go:1761. File events are applied in their retained append order.
    private (FileChangeSummary Changes, Dictionary<PathKey, Overlay>? Overlays) FlushChangesLocked(ProtocolRequestContext context)
    {
        if (pendingFileChanges.Length == 0)
            return (default, fs.Overlays());
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = fs.ProcessChanges(pendingFileChanges);
        if (options.LoggingEnabled)
            logger.Logf("Processed %d file changes in %v", pendingFileChanges.Length, System.Diagnostics.Stopwatch.GetElapsedTime(start));
        pendingFileChanges = default;
        return result;
    }

    // session.go:2086.
    private void WarmAutoImportCache(ProtocolRequestContext context, SnapshotChange change, Snapshot old, Snapshot next)
    {
        if ((change.fileChanges.Changed?.Count ?? 0) != 1)
            return;
        DocumentUri uri = default;
        foreach (var changed in change.fileChanges.Changed!)
            uri = changed;
        if (!next.IsOpenFile(uri.FileName()))
            return;
        var preferences = next.UserPreferences();
        if (preferences.IncludeCompletionsForModuleExports == Tristate.False)
            return;
        var project = next.GetDefaultProject(uri);
        if (project is null || Registry.IsPreparedForImportingFile(next.AutoImports, uri.FileName(), project.ID(), preferences))
            return;
        var warm = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
        lock (warmAutoImportMu)
        {
            warmAutoImportCancel?.Invoke();
            if (!warm.IsCancellationRequested)
                warmAutoImportCancel = () =>
                {
                    if (warm.IsCancellationRequested)
                        return;
                    logger.Logf("Cancelling auto-import warming for file %s", uri.FileName());
                    warm.Cancel();
                };
        }

        if (warm.IsCancellationRequested)
        {
            warm.Cancel();
            return;
        }

        try
        {
            if (!next.TryRef())
                return;
            try
            {
                var warmChange = new SnapshotChange
                {
                    reason = UpdateReason.RequestedLanguageServiceWithAutoImports,
                    ResourceRequest = new ResourceRequest
                    {
                        Documents = Slice<DocumentUri>.FromArray([uri]),
                        AutoImports = uri
                    }
                };
                var clone = next.Clone(context.WithCancellationToken(warm.Token), warmChange, next.Overlays(), logger, client);
                if (warm.IsCancellationRequested)
                {
                    clone.Deref();
                    return;
                }

                AdoptSnapshotChange(next, clone);
            }
            finally
            {
                next.Deref();
            }
        }
        finally
        {
            warm.Cancel();
        }
    }
}

internal sealed partial class Session
{
    // session.go:1507. Acquire every new glob before releasing previous owners.
    private Slice<Exception> UpdateWatch<T>(ProtocolRequestContext context, WatchedFiles<T>? old, WatchedFiles<T>? next)
    {
        Slice<Exception> errors = default;
        if (next is not null)
        {
            var computed = next.Watchers();
            var all = computed.WorkspaceWatchers.Append(computed.OutsideWorkspaceWatchers);
            if (all.Length != 0)
            {
                var acquired = new OrderedMap<WatcherID, FileSystemWatcher>();
                for (int i = 0; i < all.Length; i++)
                {
                    var id = new WatcherID(ByteStringValue.FromString(LogFormat.Sprintf("%s.%d", computed.WatcherID, i)));
                    if (watches.Acquire(all[i], id))
                        acquired.Set(id, all[i]);
                }

                Slice<Exception> registrationErrors = default;
                foreach (var(id, watcher)in acquired.Entries())
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
                    timeout.CancelAfter(TimeSpan.FromSeconds(1));
                    var error = client!.WatchFiles(context.WithCancellationToken(timeout.Token), id, Slice<FileSystemWatcher>.FromArray([watcher]));
                    if (error is not null)
                        registrationErrors = registrationErrors.Append(error);
                    else
                    {
                        logger.Logf(old is null ? "Added new watch: %s" : "Updated watch: %s", id);
                        logger.Log("\t" + ProjectWatches.FileSystemWatcherGlobString(watcher));
                        logger.Log("");
                    }
                }

                if (registrationErrors.Length != 0)
                {
                    foreach (var(_, watcher)in acquired.Entries())
                        watches.Release(watcher);
                    watches.MarkPending(computed.WatcherID);
                    errors = errors.Append(registrationErrors);
                }
                else
                    watches.ClearPending(computed.WatcherID);
                if ((computed.IgnoredPaths?.Count ?? 0) != 0)
                {
                    logger.Logf("%d paths ineligible for watching", computed.IgnoredPaths!.Count);
                    if (logger.IsVerbose())
                        foreach (var path in computed.IgnoredPaths)
                            logger.Log("\t" + path.AsValue().Materialize());
                }
            }
        }

        if (old is not null)
        {
            var computed = old.Watchers();
            var all = computed.WorkspaceWatchers.Append(computed.OutsideWorkspaceWatchers);
            if (all.Length != 0)
            {
                Slice<WatcherID> removed = default;
                foreach (var watcher in all)
                {
                    var(id, didRemove) = watches.Release(watcher);
                    if (didRemove)
                        removed = removed.Append(id);
                }

                foreach (var id in removed)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
                    timeout.CancelAfter(TimeSpan.FromSeconds(1));
                    var error = client!.UnwatchFiles(context.WithCancellationToken(timeout.Token), id);
                    if (error is not null)
                        errors = errors.Append(error);
                    else if (next is null)
                        logger.Logf("Removed watch: %s", id);
                }
            }
        }

        return errors;
    }

    // session.go:1600. Registration failure leaves both retained state fields unchanged.
    private Exception? UpdateContentMapperRegistrations(ProtocolRequestContext context, Snapshot current)
    {
        if (client is null)
            return null;
        var extensions = GoSlices.Clone(current.ConfigFileRegistry.ContentMappers().Extensions).Append(current.inferredProjectContentMapperExtensions);
        GoSlices.SortFunc(extensions, static (a, b) => ByteStringKey.FromValue(a).CompareTo(ByteStringKey.FromValue(b)));
        int length = 0;
        for (int i = 0; i < extensions.Length; i++)
            if (length == 0 || !extensions[i].EqualsValue(extensions[length - 1]))
                extensions[length++] = extensions[i];
        extensions.AsSpan().Slice(length).Clear();
        extensions = extensions.Slice2(0, length);
        lock (contentMapperRegistrationMu)
        {
            if (current.ID() <= registeredContentMapperSnapshotID)
                return null;
            bool equal = extensions.Length == registeredContentMapperExtensions.Length;
            if (equal)
                for (int i = 0; i < extensions.Length; i++)
                    if (!extensions[i].EqualsValue(registeredContentMapperExtensions[i]))
                    {
                        equal = false;
                        break;
                    }

            if (equal)
            {
                registeredContentMapperSnapshotID = current.ID();
                return null;
            }

            var error = client.RegisterContentMapperExtensions(context, extensions);
            if (error is not null)
            {
                if (options.LoggingEnabled)
                    logger.Log(error);
                return error;
            }

            registeredContentMapperExtensions = extensions;
            registeredContentMapperSnapshotID = current.ID();
            return null;
        }
    }

    // session.go:1636. Independent retries are keyed by the retained watcher ID.
    private Exception? UpdateWatches(Snapshot old, Snapshot next)
    {
        Slice<Exception> errors = default;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        var context = BackgroundContext();
        Core.DiffMapsFunc(old.ConfigFileRegistry.Configs, next.ConfigFileRegistry.Configs, static (a, b) => ProjectWatches.ID(a.RootFilesWatch) == ProjectWatches.ID(b.RootFilesWatch), (_, added) => errors = errors.Append(UpdateWatch(context, null, added.RootFilesWatch)), (_, removed) => errors = errors.Append(UpdateWatch(context, removed.RootFilesWatch, null)), (_, before, after) => errors = errors.Append(UpdateWatch(context, before.RootFilesWatch, after.RootFilesWatch)));
        if (next.ConfigFileRegistry.Configs is not null)
            foreach (var(path, entry)in next.ConfigFileRegistry.Configs)
                if (old.ConfigFileRegistry.Configs?.TryGetValue(path, out var previous) == true && ProjectWatches.ID(previous.RootFilesWatch) == ProjectWatches.ID(entry.RootFilesWatch) && watches.IsPending(ProjectWatches.ID(entry.RootFilesWatch)))
                    errors = errors.Append(UpdateWatch(context, null, entry.RootFilesWatch));
        TypeScript.Collections.OrderedMaps.DiffOrderedMaps(old.ProjectCollection.ProjectsByID(), next.ProjectCollection.ProjectsByID(), (_, added) =>
        {
            errors = errors.Append(UpdateWatch(context, null, added.programFilesWatch));
            errors = errors.Append(UpdateWatch(context, null, added.typingsWatch));
            errors = errors.Append(UpdateWatch(context, null, added.contentMapperWatch));
        }, (_, removed) =>
        {
            errors = errors.Append(UpdateWatch(context, removed.programFilesWatch, null));
            errors = errors.Append(UpdateWatch(context, removed.typingsWatch, null));
            errors = errors.Append(UpdateWatch(context, removed.contentMapperWatch, null));
        }, (_, before, after) =>
        {
            errors = errors.Append(UpdateWatchChange(context, before.programFilesWatch, after.programFilesWatch));
            errors = errors.Append(UpdateWatchChange(context, before.typingsWatch, after.typingsWatch));
            errors = errors.Append(UpdateWatchChange(context, before.contentMapperWatch, after.contentMapperWatch));
        });
        errors = errors.Append(UpdateWatchChange(context, old.autoImportsWatch, next.autoImportsWatch));
        if (errors.Length != 0)
            return new InvalidOperationException("errors updating watches: " + LogFormat.Sprintf("%v", errors));
        if (options.LoggingEnabled)
            logger.Logf("Updated watches in %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
        return null;
    }

    // Shared local spelling of the three identical source branches, no new ownership.
    private Slice<Exception> UpdateWatchChange<T>(ProtocolRequestContext context, WatchedFiles<T>? old, WatchedFiles<T>? next)
    {
        if (ProjectWatches.ID(old) != ProjectWatches.ID(next))
            return UpdateWatch(context, old, next);
        return watches.IsPending(ProjectWatches.ID(next)) ? UpdateWatch(context, null, next) : default;
    }

    // session.go:424.
    internal void DidChangeWatchedFiles(ProtocolRequestContext context, Slice<FileEvent> changes)
    {
        var events = Slice<FileChange>.Make(0, changes.Length);
        bool relevant = false, configChange = false;
        var current = Snapshot();
        var(extensions, watched) = current.ContentMapperWatchState();
        foreach (var change in changes)
        {
            FileChangeKind kind;
            switch (change.Type)
            {
                case FileChangeType.Created:
                    kind = FileChangeKind.WatchCreate;
                    break;
                case FileChangeType.Changed:
                    kind = FileChangeKind.WatchChange;
                    break;
                case FileChangeType.Deleted:
                    kind = FileChangeKind.WatchDelete;
                    break;
                default:
                    continue;
            }

            events = events.Append(new FileChange { Kind = kind, URI = change.Uri });
            if (!configChange && current.ConfigFileRegistry.IsTracked(change.Uri.PathKey(caseSensitivity)))
                configChange = true;
            if (!relevant)
            {
                var filePath = change.Uri.Path();
                var path = caseSensitivity.PathKey(filePath).RemoveTrailingDirectorySeparator();
                if (watched?.Contains(path) == true)
                {
                    relevant = true;
                    continue;
                }

                var extension = path.ExtensionValue();
                if (extension.Length == 0)
                {
                    if (kind != FileChangeKind.WatchDelete)
                        relevant = fs.DirectoryExists(TsPath.RootedDirectoryPathFromPath(filePath));
                    else
                    {
                        var snapshot = Snapshot();
                        if (snapshot.fs.cacheDirectories?.ContainsKey(path) == true || snapshot.HasOverlayWithin(path) || (path.AsValue().Contains("/node_modules/"u8) || path.AsValue().EndsWith("/node_modules"u8)))
                            relevant = true;
                    }
                }
                else
                {
                    relevant = IsRelevantExtension(extension);
                    if (!relevant)
                        foreach (var item in extensions)
                            if (path.AsValue().Length > item.Length && path.AsValue().EndsWith(item))
                            {
                                relevant = true;
                                break;
                            }
                }
            }
        }

        lock (pendingFileChangesMu)
            pendingFileChanges = pendingFileChanges.Append(events);
        if (relevant)
            ScheduleDiagnosticsRefresh();
        if (configChange)
            ScheduleSnapshotUpdate(UpdateReason.DidChangeConfigFile);
        CancelWarmAutoImportCache();
        ScheduleIdleCacheClean();
    }

    // snapshotfs.go:638; the source shared worker is moved to its owner at promotion.
    private static bool IsRelevantExtension(ByteStringValue extension) => extension.EqualsBytes(".js"u8) || extension.EqualsBytes(".jsx"u8) || extension.EqualsBytes(".mjs"u8) || extension.EqualsBytes(".cjs"u8) || extension.EqualsBytes(".ts"u8) || extension.EqualsBytes(".tsx"u8) || extension.EqualsBytes(".mts"u8) || extension.EqualsBytes(".cts"u8) || extension.EqualsBytes(".json"u8);
}

internal sealed partial class Session
{
    // session.go:1868. The actual injected npm host owns process execution.
    public (Slice<byte> Output, Exception? Error) NpmInstall(CancellationToken context, RootedDirectoryPath cwd, Slice<ByteStringValue> arguments) => npmExecutor!.NpmInstall(context, cwd, arguments);
    // session.go:1872/1880. Only comparable source preference fields participate.
    private void RefreshInlayHintsIfNeeded(UserPreferences old, UserPreferences next)
    {
        var a = old.InlayHintsPreferences;
        var b = next.InlayHintsPreferences;
        bool equal = a.IncludeInlayEnumMemberValueHints == b.IncludeInlayEnumMemberValueHints && a.IncludeInlayFunctionLikeReturnTypeHints == b.IncludeInlayFunctionLikeReturnTypeHints && a.IncludeInlayFunctionParameterTypeHints == b.IncludeInlayFunctionParameterTypeHints && a.IncludeInlayParameterNameHints == b.IncludeInlayParameterNameHints && a.IncludeInlayParameterNameHintsWhenArgumentMatchesName == b.IncludeInlayParameterNameHintsWhenArgumentMatchesName && a.IncludeInlayPropertyDeclarationTypeHints == b.IncludeInlayPropertyDeclarationTypeHints && a.IncludeInlayVariableTypeHints == b.IncludeInlayVariableTypeHints && a.IncludeInlayVariableTypeHintsWhenTypeMatchesName == b.IncludeInlayVariableTypeHintsWhenTypeMatchesName;
        if (!equal && client!.RefreshInlayHints(BackgroundContext())is { } error && options.LoggingEnabled)
            logger.Logf("Error refreshing inlay hints: %v", error);
    }

    private void RefreshCodeLensIfNeeded(UserPreferences old, UserPreferences next)
    {
        var a = old.CodeLensUserPreferences;
        var b = next.CodeLensUserPreferences;
        bool equal = a.ImplementationsCodeLensEnabled == b.ImplementationsCodeLensEnabled && a.ImplementationsCodeLensShowOnAllClassMethods == b.ImplementationsCodeLensShowOnAllClassMethods && a.ImplementationsCodeLensShowOnInterfaceMethods == b.ImplementationsCodeLensShowOnInterfaceMethods && a.ReferencesCodeLensEnabled == b.ReferencesCodeLensEnabled && a.ReferencesCodeLensShowOnAllFunctions == b.ReferencesCodeLensShowOnAllFunctions;
        if (!equal && client!.RefreshCodeLens(BackgroundContext())is { } error && options.LoggingEnabled)
            logger.Logf("Error refreshing code lens: %v", error);
    }

    // session.go:1888/1896.
    private void RefreshDiagnosticsIfNeeded(UserPreferences old, UserPreferences next)
    {
        if (old.CustomConfigFileName != next.CustomConfigFileName || old.ReportStyleChecksAsWarnings != next.ReportStyleChecksAsWarnings || old.ValidateEnabled != next.ValidateEnabled)
            ScheduleDiagnosticsRefresh();
    }

    private void RefreshATAIfNeeded(UserPreferences old, UserPreferences next)
    {
        if (old.IsATADisabled() && !next.IsATADisabled())
            ScheduleDiagnosticsRefresh();
    }

    // session.go:1904.
    private void PublishProgramDiagnostics(Snapshot old, Snapshot next)
    {
        if (!options.PushDiagnosticsEnabled)
            return;
        var oldProjects = old.ProjectCollection.ProjectsByID();
        var oldOpen = old.ProjectCollection.GetOpenConfiguredProjects();
        if (next.UserPreferences().ValidateEnabled == Tristate.False)
        {
            if (old.UserPreferences().ValidateEnabled == Tristate.False)
                return;
            foreach (var(_, project)in oldProjects.Entries())
            {
                var(configuredID, configured) = project.ID().Configured();
                if (configured && oldOpen?.Contains(configuredID) == true)
                    PublishProjectDiagnostics(BackgroundContext(), project.ConfigFileName(), default, old.converters);
            }

            return;
        }

        var context = BackgroundContext();
        var nextProjects = next.ProjectCollection.ProjectsByID();
        var nextOpen = next.ProjectCollection.GetOpenConfiguredProjects();
        TypeScript.Collections.OrderedMaps.DiffOrderedMaps(oldProjects, nextProjects, (_, added) =>
        {
            var(id, configured) = added.ID().Configured();
            if (ShouldPublishProgramDiagnostics(added, next.ID()) && configured && nextOpen?.Contains(id) == true)
                PublishProjectDiagnostics(context, added.ConfigFileName(), added.GetProjectDiagnostics(context), next.converters);
        }, (_, removed) =>
        {
            if (removed.Kind == ProjectKind.Configured)
                PublishProjectDiagnostics(context, removed.ConfigFileName(), default, old.converters);
        }, (_, before, after) =>
        {
            var(id, configured) = after.ID().Configured();
            if (ShouldPublishProgramDiagnostics(after, next.ID()) && configured && nextOpen?.Contains(id) == true)
                PublishProjectDiagnostics(context, after.ConfigFileName(), after.GetProjectDiagnostics(context), next.converters);
        });
        foreach (var(id, project)in nextProjects.Entries())
        {
            if (project.Kind != ProjectKind.Configured || !oldProjects.Has(id))
                continue;
            var(configuredID, _) = project.ID().Configured();
            var(before, _) = oldProjects.Get(id);
            bool isOpen = nextOpen?.Contains(configuredID) == true, wasOpen = oldOpen?.Contains(configuredID) == true;
            if (isOpen && !wasOpen && (ReferenceEquals(project, before) || !ShouldPublishProgramDiagnostics(project, next.ID())))
                PublishProjectDiagnostics(context, project.ConfigFileName(), project.GetProjectDiagnostics(context), next.converters);
            else if (!isOpen && wasOpen)
                PublishProjectDiagnostics(context, project.ConfigFileName(), default, next.converters);
        }
    }

    // session.go:1984.
    private static bool ShouldPublishProgramDiagnostics(Project project, ulong id) => project.Kind == ProjectKind.Configured && project.Program is not null && project.ProgramLastUpdate == id && project.ProgramUpdateKind > ProgramUpdateKind.Cloned;
    // session.go:1991.
    private void PublishProjectDiagnostics(ProtocolRequestContext context, RootedFilePath name, Slice<Diagnostic> diagnostics, Converters converters)
    {
        if (Config().ValidateEnabled == Tristate.False)
            diagnostics = default;
        context = WithCurrentLocale(context);
        var output = Slice<TypeScript.Lsp.Protocol.Diagnostic?>.Make(0, diagnostics.Length);
        foreach (var diagnostic in diagnostics)
            output = output.Append(Lsconv.DiagnosticToLSPPush(context, converters, diagnostic));
        if (client!.PublishDiagnostics(context, new PublishDiagnosticsParams { Uri = Lsconv.FileNameToDocumentURI(name), Diagnostics = output })is { } error && options.LoggingEnabled)
            logger.Logf("Error publishing diagnostics: %v", error);
    }

    // session.go:2013/2022.
    internal void EnqueuePublishGlobalDiagnostics()
    {
        if (!options.PushDiagnosticsEnabled || Config().ValidateEnabled == Tristate.False)
            return;
        if (Interlocked.CompareExchange(ref globalDiagPublishPending, 1, 0) == 0)
        {
            var context = BackgroundContext();
            backgroundQueue.Enqueue(context.Cancellation, token => PublishGlobalDiagnostics(context.WithCancellationToken(token)));
        }
    }

    private void PublishGlobalDiagnostics(ProtocolRequestContext context)
    {
        try
        {
            Snapshot current;
            snapshotMu.EnterReadLock();
            try
            {
                current = snapshot!;
                current.Ref();
            }
            finally
            {
                snapshotMu.ExitReadLock();
            }

            try
            {
                foreach (var project in current.ProjectCollection.Projects())
                    if (project.Kind == ProjectKind.Configured && project.checkerPool is not null && project.checkerPool.TakeNewGlobalDiagnostics())
                        PublishProjectDiagnostics(context, project.ConfigFileName(), project.GetProjectDiagnostics(context), current.converters);
            }
            finally
            {
                current.Deref();
            }
        }
        finally
        {
            Volatile.Write(ref globalDiagPublishPending, 0);
        }
    }

    // session.go:2042.
    private void TriggerATAForUpdatedProjects(Snapshot current)
    {
        foreach (var project in current.ProjectCollection.Projects())
            if (project.ShouldTriggerATA(current.ID()))
            {
                var context = BackgroundContext();
                backgroundQueue.Enqueue(context.Cancellation, token =>
                {
                    LogTree? logs = options.LoggingEnabled ? LogTree.NewLogTree("Triggering ATA for project " + project.ID().Value.Materialize()) : null;
                    var info = project.ComputeTypingsInfo();
                    var files = project.Program!.GetSourceFiles();
                    var fileNames = Slice<RootedFilePath>.Make(files.Count);
                    for (int i = 0; i < files.Count; i++)
                        fileNames[i] = RootedFilePath.FromValue(files[i].FileNameGoValue());
                    var request = new TypingsInstallRequest
                    {
                        ProjectID = project.ID(),
                        TypingsInfo = info,
                        FileNames = fileNames,
                        ProjectRootPath = project.projectDirectory,
                        FS = fs,
                        Logger = logs!
                    };
                    var display = project.DisplayName(options.CurrentDirectory);
                    client?.ProgressStart(Diagnostics.Installing_types_for_0, display);
                    var(result, error) = typingsInstaller!.InstallTypings(token, request);
                    client?.ProgressFinish(Diagnostics.Installing_types_for_0, display);
                    if (error is not null)
                    {
                        if (logs is not null)
                        {
                            logger.Logf("ATA installation failed for project %s: %v", project.ID(), error);
                            logger.Log(logs.String());
                        }
                    }
                    else if (!result!.TypingsFiles.AsSpan().SequenceEqual(project.typingsFiles.AsSpan()))
                    {
                        lock (pendingATAChangesMu)
                        {
                            pendingATAChanges[project.ID()] = new ATAStateChange
                            {
                                TypingsInfo = info,
                                TypingsFiles = result.TypingsFiles,
                                TypingsFilesToWatch = result.FilesToWatch,
                                Logs = logs
                            };
                            ScheduleDiagnosticsRefresh();
                        }
                    }
                });
            }
    }
}

internal sealed partial class Session
{
    // session.go:792. Go-only runtime metric fields retain schema defaults, with
    // availability documented separately. They are not fabricated CLR measurements.
    private void SendPerformanceTelemetry(ProtocolRequestContext context)
    {
        if (client is null || !options.TelemetryEnabled)
            return;
        var current = Snapshot();
        var measurements = new PerformanceStatsTelemetryMeasurements
        {
            OpenFileCount = current.Overlays()?.Count ?? 0,
            UptimeSeconds = System.Diagnostics.Stopwatch.GetElapsedTime(startTime).TotalSeconds,
            ProjectCount = current.ProjectCollection.Projects().Length,
            ConfigCount = current.ConfigFileRegistry.Configs?.Count ?? 0,
            CachedDiskFileCount = current.fs.cacheFiles?.Count ?? 0
        };
        // go-osstat/memory's Linux source reads the same OS counters and chooses
        // total-available when supported; missing/failed host observations stay zero.
        try
        {
            if (OperatingSystem.IsLinux())
            {
                ulong total = 0, free = 0, available = 0, buffers = 0, cached = 0;
                bool hasAvailable = false;
                foreach (var line in File.ReadLines("/proc/meminfo"))
                {
                    int separator = line.IndexOf(':');
                    if (separator < 0)
                        continue;
                    var key = line.AsSpan(0, separator);
                    var text = line.AsSpan(separator + 1).Trim();
                    if (text.EndsWith("kB"))
                        text = text[..^2].Trim();
                    if (!ulong.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                        continue;
                    ulong bytes = unchecked(parsed * 1024);
                    if (key.SequenceEqual("MemTotal"))
                        total = bytes;
                    else if (key.SequenceEqual("MemFree"))
                        free = bytes;
                    else if (key.SequenceEqual("MemAvailable"))
                    {
                        available = bytes;
                        hasAvailable = true;
                    }
                    else if (key.SequenceEqual("Buffers"))
                        buffers = bytes;
                    else if (key.SequenceEqual("Cached"))
                        cached = bytes;
                }

                measurements.SystemMemTotal = total;
                measurements.SystemMemUsed = hasAvailable ? unchecked(total - available) : unchecked(total - free - buffers - cached);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        if (current.AutoImportRegistry()is { } registry)
        {
            var stats = registry.GetCacheStats();
            measurements.AutoImportProjectBucketCount = stats.ProjectBuckets.Length;
            measurements.AutoImportNodeModulesBucketCount = stats.NodeModulesBuckets.Length;
            measurements.AutoImportUniquePackageCount = stats.UniquePackageCount;
            foreach (var bucket in stats.ProjectBuckets)
            {
                measurements.AutoImportProjectExportCount += bucket.ExportCount;
                measurements.AutoImportProjectFileCount += bucket.FileCount;
            }

            foreach (var bucket in stats.NodeModulesBuckets)
            {
                measurements.AutoImportNodeModulesExportCount += bucket.ExportCount;
                measurements.AutoImportNodeModulesFileCount += bucket.FileCount;
                if (bucket.DependencyNames is null)
                    measurements.AutoImportNodeModulesUnfilteredBucketCount++;
            }
        }

        if (client.SendTelemetry(context, new TelemetryEvent { PerformanceStatsTelemetryEvent = new PerformanceStatsTelemetryEvent { Measurements = measurements } })is { } error && options.LoggingEnabled)
            logger.Logf("Error sending performance telemetry: %v", error);
    }

    // session.go:899/916. Mark seen only after successful send.
    private void SendProjectInfoTelemetryForNewProjects(Snapshot old, Snapshot next)
    {
        if (!options.TelemetryEnabled)
            return;
        var context = BackgroundContext();
        TypeScript.Collections.OrderedMaps.DiffOrderedMaps(old.ProjectCollection.ProjectsByID(), next.ProjectCollection.ProjectsByID(), (_, added) => SendProjectInfoTelemetry(context, added), static (_, _) =>
        {
        }, static (_, _, _) =>
        {
        });
    }

    private void SendProjectInfoTelemetry(ProtocolRequestContext context, Project project)
    {
        if (client is null || !options.TelemetryEnabled || seenProjects.Load(project.ID()).Ok || project.Program is null || project.CommandLine is null)
            return;
        var info = CollectProjectInfoTelemetry(project);
        if (client.SendTelemetry(context, info)is { } error)
        {
            if (options.LoggingEnabled)
                logger.Logf("Error sending project info telemetry: %v", error);
            return;
        }

        seenProjects.Store(project.ID(), true);
    }

    // session.go:937.
    private TelemetryEvent CollectProjectInfoTelemetry(Project project)
    {
        var options = project.CommandLine!.CompilerOptions() ?? new CompilerOptions();
        ByteStringValue configurationName = ByteStringValue.FromString("other");
        if (project.Kind == ProjectKind.Configured)
        {
            var name = project.ConfigFileName().BaseNameValue();
            if (name.EqualsBytes("tsconfig.json"u8) || name.EqualsBytes("jsconfig.json"u8))
                configurationName = name;
        }

        var properties = ProtocolMap<ByteStringKey, ByteStringValue>.Make();
        properties[ByteStringKey.FromString("configFileName")] = configurationName;
        properties[ByteStringKey.FromString("projectType")] = ByteStringValue.FromString(project.Kind == ProjectKind.Configured ? "configured" : "inferred");
        properties[ByteStringKey.FromString("version")] = ByteStringValue.FromString(Core.Version());
        var compilerOptions = new Dictionary<string, object?>();
        SetTristate(compilerOptions, "strict", options.Strict);
        SetTristate(compilerOptions, "noImplicitAny", options.NoImplicitAny);
        SetTristate(compilerOptions, "noImplicitThis", options.NoImplicitThis);
        SetTristate(compilerOptions, "strictNullChecks", options.StrictNullChecks);
        SetTristate(compilerOptions, "strictFunctionTypes", options.StrictFunctionTypes);
        SetTristate(compilerOptions, "strictBindCallApply", options.StrictBindCallApply);
        SetTristate(compilerOptions, "strictPropertyInitialization", options.StrictPropertyInitialization);
        SetTristate(compilerOptions, "strictBuiltinIteratorReturn", options.StrictBuiltinIteratorReturn);
        SetTristate(compilerOptions, "useUnknownInCatchVariables", options.UseUnknownInCatchVariables);
        SetTristate(compilerOptions, "exactOptionalPropertyTypes", options.ExactOptionalPropertyTypes);
        SetTristate(compilerOptions, "allowJs", options.AllowJs);
        SetTristate(compilerOptions, "checkJs", options.CheckJs);
        SetTristate(compilerOptions, "noEmit", options.NoEmit);
        SetTristate(compilerOptions, "declaration", options.Declaration);
        SetTristate(compilerOptions, "composite", options.Composite);
        SetTristate(compilerOptions, "isolatedModules", options.IsolatedModules);
        SetTristate(compilerOptions, "skipLibCheck", options.SkipLibCheck);
        SetTristate(compilerOptions, "incremental", options.Incremental);
        if (options.Target != ScriptTarget.None)
            compilerOptions["target"] = ByteStringValue.FromString(ScriptTargetString(options.Target));
        if (options.Module != ModuleKind.None)
            compilerOptions["module"] = ByteStringValue.FromString(options.Module.String());
        if (options.ModuleResolution != ModuleResolutionKind.Unknown)
            compilerOptions["moduleResolution"] = ByteStringValue.FromString(options.ModuleResolution.String());
        if (options.Jsx != JsxEmit.None)
            compilerOptions["jsx"] = ByteStringValue.FromString(options.Jsx.String());
        var encoder = new ProtocolEncoder();
        encoder.WriteAny(compilerOptions);
        properties[ByteStringKey.FromString("compilerOptions")] = encoder.Finish();
        if (project.CommandLine.Raw is OrderedMap<string, object?> raw)
        {
            properties[ByteStringKey.FromString("extends")] = BoolTelemetry(raw.Has("extends"));
            properties[ByteStringKey.FromString("files")] = BoolTelemetry(raw.Has("files"));
            properties[ByteStringKey.FromString("include")] = BoolTelemetry(raw.Has("include"));
            properties[ByteStringKey.FromString("exclude")] = BoolTelemetry(raw.Has("exclude"));
        }

        return new TelemetryEvent
        {
            ProjectInfoTelemetryEvent = new ProjectInfoTelemetryEvent
            {
                Properties = properties,
                Measurements = CountFileStats(project.Program!.GetSourceFiles())
            }
        };
    }

    // session.go:1005/1013.
    private static void SetTristate(Dictionary<string, object?> values, string name, Tristate value)
    {
        if (value == Tristate.True)
            values[name] = true;
        else if (value == Tristate.False)
            values[name] = false;
    }

    private static ByteStringValue BoolTelemetry(bool value) => ByteStringValue.FromString(value ? "true" : "false");
    // session.go:1020. Source-file End counts original UTF8 bytes.
    // core/scripttarget_stringer_generated.go:41; telemetry's exact Go spelling.
    private static string ScriptTargetString(ScriptTarget target)
    {
        int value = (int)target;
        return value switch
        {
            0 => "None",
            1 => "ES5",
            >= 2 and <= 13 => "ES" + (2013 + value).ToString(System.Globalization.CultureInfo.InvariantCulture),
            99 => "ESNext",
            100 => "JSON",
            _ => "ScriptTarget(" + value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"
        };
    }

    private static ProjectInfoTelemetryMeasurements CountFileStats(Slice<SourceFile> files)
    {
        var stats = new ProjectInfoTelemetryMeasurements();
        foreach (var file in files)
        {
            double size = file.End();
            switch (file.ScriptKind)
            {
                case ScriptKind.JS:
                    stats.JsFileCount++;
                    stats.JsFileSize += size;
                    break;
                case ScriptKind.JSX:
                    stats.JsxFileCount++;
                    stats.JsxFileSize += size;
                    break;
                case ScriptKind.TS:
                    if (file.FileName().IsDeclarationFile())
                    {
                        stats.DtsFileCount++;
                        stats.DtsFileSize += size;
                    }
                    else
                    {
                        stats.TsFileCount++;
                        stats.TsFileSize += size;
                    }

                    break;
                case ScriptKind.TSX:
                    stats.TsxFileCount++;
                    stats.TsxFileSize += size;
                    break;
            }
        }

        return stats;
    }
}

internal sealed partial class Session
{
    // session.go:1460.
    private void LogContentMapperTimings(Timings timings)
    {
        if (timings.RequestWait == 0 && !HasContentMapperOperationTimings(timings.Mappers))
            return;
        logger.Log("Content mapper timings since previous snapshot adoption:");
        if (timings.RequestWait != 0)
            logger.Logf("  Request wait time: %v", new GoLogDuration(timings.RequestWait));
        var identities = Slice<string>.Make(timings.Mappers?.Count ?? 0);
        int index = 0;
        if (timings.Mappers is not null)
            foreach (var identity in timings.Mappers.Keys)
                identities[index++] = identity;
        GoSlices.SortFunc(identities, static (a, b) => string.CompareOrdinal(a, b));
        foreach (var identity in identities)
        {
            var mapper = timings.Mappers![identity];
            if (!HasContentMapperOperationTiming(mapper))
                continue;
            logger.Logf("  %s:", identity);
            if (mapper.Spawn.Count != 0)
                logger.Logf("    Initializations: %d (%v)", mapper.Spawn.Count, new GoLogDuration(unchecked(mapper.Spawn.Duration + mapper.Initialize.Duration)));
            if (mapper.OpenProject.Count != 0)
                logger.Logf("    openProject requests: %d (%v)", mapper.OpenProject.Count, new GoLogDuration(mapper.OpenProject.Duration));
            if (mapper.CloseProject.Count != 0)
                logger.Logf("    closeProject requests: %d (%v)", mapper.CloseProject.Count, new GoLogDuration(mapper.CloseProject.Duration));
            if (mapper.Transform.Count != 0)
                logger.Logf("    Transforms: %d (%v)", mapper.Transform.Count, new GoLogDuration(mapper.Transform.Duration));
        }
    }

    // session.go:1492/1501.
    private static bool HasContentMapperOperationTimings(Dictionary<string, MapperTimings>? values)
    {
        if (values is not null)
            foreach (var value in values.Values)
                if (HasContentMapperOperationTiming(value))
                    return true;
        return false;
    }

    private static bool HasContentMapperOperationTiming(MapperTimings value) => value.Spawn.Count != 0 || value.OpenProject.Count != 0 || value.CloseProject.Count != 0 || value.Transform.Count != 0;
    // session.go:1773.
    private void LogProjectChanges(Snapshot old, Snapshot next)
    {
        bool logged = false;
        void LogProject(Project project)
        {
            var builder = new GoBuilder();
            project.Print(logger.IsVerbose(), logger.IsVerbose(), builder);
            logger.Log(builder.String());
            logged = true;
        }

        TypeScript.Collections.OrderedMaps.DiffOrderedMaps(old.ProjectCollection.ProjectsByID(), next.ProjectCollection.ProjectsByID(), (_, added) => LogProject(added), (_, removed) => logger.Logf("\nProject '%s' removed\n%s", removed.ID(), "-----------------------------------------------"), (_, before, after) =>
        {
            if (after.ProgramUpdateKind == ProgramUpdateKind.NewFiles)
                LogProject(after);
        });
        if (logged || logger.IsVerbose())
            LogCacheStats(next);
    }

    // session.go:1802.
    private void LogCacheStats(Snapshot current)
    {
        int parseCount = 0, extendedCount = 0;
        if (logger.IsVerbose())
        {
            parseCount = SnapshotHost.parseCache.EntryCount();
            extendedCount = SnapshotHost.extendedConfigCache.EntryCount();
        }

        logger.Log("\n======== Cache Statistics ========");
        logger.Logf("Open file count:   %6d", current.Overlays()?.Count ?? 0);
        logger.Logf("Cached disk files: %6d", current.fs.cacheFiles?.Count ?? 0);
        logger.Logf("Realpath aliases:  %6d", current.fs.nodeModulesRealpathAliases?.Count ?? 0);
        logger.Logf("Project count:     %6d", current.ProjectCollection.Projects().Length);
        logger.Logf("Config count:      %6d", current.ConfigFileRegistry.Configs?.Count ?? 0);
        if (!logger.IsVerbose())
            return;
        logger.Logf("Parse cache size:           %6d", parseCount);
        logger.Logf("Program count:              %6d", SnapshotHost.programCounter.Len());
        logger.Logf("Extended config cache size: %6d", extendedCount);
        logger.Log("Auto Imports:");
        var stats = current.AutoImportRegistry()!.GetCacheStats();
        logger.Logf("\tUnique packages (by realpath): %d", stats.UniquePackageCount);
        if (stats.ProjectBuckets.Length != 0)
        {
            logger.Log("\tProject buckets:");
            foreach (var bucket in stats.ProjectBuckets)
            {
                logger.Logf("\t\t%s%s:", bucket.Name, bucket.State.Dirty() ? " (dirty)" : "");
                logger.Logf("\t\t\tFiles: %d", bucket.FileCount);
                logger.Logf("\t\t\tExports: %d", bucket.ExportCount);
            }
        }

        if (stats.NodeModulesBuckets.Length != 0)
        {
            logger.Log("\tnode_modules buckets:");
            foreach (var bucket in stats.NodeModulesBuckets)
            {
                logger.Logf("\t\t%s%s:", bucket.Name, bucket.State.Dirty() ? " (dirty)" : "");
                if (bucket.State.DirtyPackages()is { } dirty)
                    foreach (var name in dirty)
                        logger.Logf("\t\t\tNeeds granular update: %s", name);
                if (bucket.DependencyNames is not null)
                    logger.Logf("\t\t\tCollected packages: %d", bucket.DependencyNames.Count);
                else
                    logger.Log("\t\t\tCollected packages: all, due to no package.json!");
                logger.Logf("\t\t\tTotal packages: %d", bucket.PackageNames?.Count ?? 0);
                logger.Logf("\t\t\tFiles: %d", bucket.FileCount);
                logger.Logf("\t\t\tExports: %d", bucket.ExportCount);
                var recursive = bucket.State.RecursiveSearchPackages();
                if (recursive is null)
                    logger.Log("\t\t\tRecursive search: all");
                else if (recursive.Count != 0)
                    logger.Logf("\t\t\tRecursive search: %d packages", recursive.Count);
                else
                    logger.Log("\t\t\tRecursive search: none");
            }
        }
    }
}

// internal/project/session.go:36. Snapshot reason is shared across host/session updates.
internal enum UpdateReason
{
    Unknown,
    DidOpenFile,
    DidCloseFile,
    DidChangeCompilerOptionsForInferredProjects,
    RequestedLanguageServicePendingChanges,
    RequestedLanguageServiceProjectNotLoaded,
    RequestedLanguageServiceForFileNotOpen,
    RequestedLanguageServiceProjectDirty,
    RequestedLoadProjectTree,
    RequestedLanguageServiceWithAutoImports,
    IdleCleanDiskCache,
    DidChangeConfigFile,
    DidChangeContentMapperContributions
}
