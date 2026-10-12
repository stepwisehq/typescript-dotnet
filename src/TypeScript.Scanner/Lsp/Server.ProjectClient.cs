// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Actual project.Client and ata.NpmExecutor contract forwards; no alternate state owner.
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.ProjectSystem.Ata;
using TypeScript.Scanner;
using ProtocolWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;
using TelemetryEvent = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    Exception? IProjectClient.WatchFiles(ProtocolRequestContext context, WatcherID id, Slice<ProtocolWatcher> requests) => WatchFiles(context, id, requests);
    Exception? IProjectClient.UnwatchFiles(ProtocolRequestContext context, WatcherID id) => UnwatchFiles(context, id);
    Exception? IProjectClient.RegisterContentMapperExtensions(ProtocolRequestContext context, Slice<ByteStringValue> extensions) => RegisterContentMapperExtensions(context, extensions);
    Exception? IProjectClient.RefreshDiagnostics(ProtocolRequestContext context) => RefreshDiagnostics(context);
    Exception? IProjectClient.PublishDiagnostics(ProtocolRequestContext context, PublishDiagnosticsParams parameters) => PublishDiagnostics(context, parameters);
    Exception? IProjectClient.RefreshInlayHints(ProtocolRequestContext context) => RefreshInlayHints(context);
    Exception? IProjectClient.RefreshCodeLens(ProtocolRequestContext context) => RefreshCodeLens(context);
    void IProjectClient.ProgressStart(TypeScript.Scanner.Message message, params object? [] arguments) => ProgressStart(message, arguments);
    void IProjectClient.ProgressFinish(TypeScript.Scanner.Message message, params object? [] arguments) => ProgressFinish(message, arguments);
    Exception? IProjectClient.SendTelemetry(ProtocolRequestContext context, TelemetryEvent telemetry) => SendTelemetry(context, telemetry);
    bool IProjectClient.IsActive() => IsActive();
    void IProjectClient.SetLocale(ByteStringValue value) => SetLocale(value);
    TypeScript.Locale.Locale IProjectClient.GetLocale() => GetLocale();
    (Slice<byte> Output, Exception? Error) INpmExecutor.NpmInstall(CancellationToken context, RootedDirectoryPath directory, Slice<ByteStringValue> arguments)
    {
        // Existing ATA contract carries cancellation only. The callback is synchronous;
        // its returned already-owned byte buffer becomes the same retained slice header.
        var(output, error) = NpmInstall(ProtocolRequestContext.Default.WithCancellationToken(context), directory, arguments);
        return (output is null ? default : Slice<byte>.FromArray(output), error);
    }
}
