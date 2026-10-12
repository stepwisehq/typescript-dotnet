// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1713–1825. Ignored whole initialized lifecycle owner.
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using FsWatchApi = TypeScript.FsWatch.FsWatch;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private Exception? HandleInitialized(ProtocolRequestContext context, InitializedParams? parameters)
    {
        bool disablePushDiagnostics = initializationOptions!.DisablePushDiagnostics ?? false;
        bool enableTelemetry = initializationOptions.EnableTelemetry ?? false;
        bool runExternalCode = initializationOptions.RunExternalCode ?? false;
        if (clientCapabilities.Workspace.DidChangeWatchedFiles.DynamicRegistration)
        {
            logger.Logf("file watching: using LSP client-side watching (client supports dynamic registration)");
            watchEnabled = true;
        }
        else if (FsWatchApi.Default().HasFastRecursiveBackend())
        {
            logger.Logf("file watching: using builtin in-process watcher (client lacks dynamic watch registration)");
            watchEnabled = true;
            builtinWatcher = Watching.Watcher.New(fs, changes =>
            {
                if (session is not null)
                    session.DidChangeWatchedFiles(ProtocolRequestContext.Default.WithCancellationToken(backgroundCtx), changes);
            }, logger);
        }
        else
            logger.Logf("file watching: disabled (client lacks dynamic watch registration and builtin watcher backend is not fast-recursive)");
        var currentDirectory = cwd;
        if (clientCapabilities.Workspace.WorkspaceFolders && initializeParams!.WorkspaceFolders?.WorkspaceFolders is { } folders && folders.Length == 1)
        {
            var fileName = new DocumentUri(folders[0]!.Uri.Value).FileName();
            if (fileName.AsValue().Length != 0)
                currentDirectory = TsPath.RootedDirectoryPathFromPath(new RootedPath(fileName.AsValue()));
        }
        else if (initializeParams!.RootUri.DocumentUri is { } rootUri)
        {
            var fileName = rootUri.FileName();
            if (fileName.AsValue().Length != 0)
                currentDirectory = TsPath.RootedDirectoryPathFromPath(new RootedPath(fileName.AsValue()));
        }
        else if (initializeParams.RootPath?.String is { } rootPath && TsPath.PathIsAbsolute(rootPath))
            currentDirectory = TsPath.RootedDirectoryPathFromAbsolute(rootPath);
        telemetryEnabled = enableTelemetry;
        session = TypeScript.ProjectSystem.Session.NewSession(new SessionInit { BackgroundContext = ProtocolRequestContext.Default.WithCancellationToken(backgroundCtx).WithClientCapabilities(clientCapabilities), Options = new SessionOptions { CurrentDirectory = currentDirectory, DefaultLibraryPath = defaultLibraryPath, TypingsLocation = typingsLocation, PositionEncoding = positionEncoding, WatchEnabled = watchEnabled, LoggingEnabled = true, TelemetryEnabled = enableTelemetry, DebounceDelay = TimeSpan.FromMilliseconds(500), PushDiagnosticsEnabled = !disablePushDiagnostics, RunExternalCode = runExternalCode }, FS = fs, Logger = logger, Client = this, NpmExecutor = this, Spawner = ContentMapperSpawner(), ContentMapperLogger = ContentMapperLogger(), ParseCache = parseCache });
        var(preferences, error) = RequestConfiguration(context);
        if (error is not null)
            return error;
        session.InitializeWithUserConfig(preferences);
        (_, error) = SendClientRequest(context, Method.ClientRegisterCapabilityInfo, new RegistrationParams { Registrations = Slice<Registration?>.FromArray([new Registration { Id = ByteStringValue.FromString("typescript-config-watch-id"), RegisterOptions = new RegisterOptions { WorkspaceDidChangeConfiguration = new DidChangeConfigurationRegistrationOptions { Section = new StringOrStrings { Strings = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("js/ts"), ByteStringValue.FromString("typescript"), ByteStringValue.FromString("javascript"), ByteStringValue.FromString("editor")]) } } } }]) });
        if (error is not null)
            return new InvalidOperationException("failed to register configuration change watcher: " + error.Message, error);
        if (compilerOptionsForInferredProjects is not null)
            session.DidChangeCompilerOptionsForInferredProjects(context, compilerOptionsForInferredProjects);
        session.StartPerformanceTelemetry();
        initComplete.SetResult();
        return null;
    }
}
