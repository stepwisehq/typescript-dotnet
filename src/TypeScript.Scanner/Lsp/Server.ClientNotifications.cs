// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:743–819. Ignored actual project-client callback draft.
using TypeScript.Lsp.Protocol;
using DiagnosticMessage = TypeScript.Scanner.Message;
using TelemetryEvent = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal Exception? RefreshDiagnostics(ProtocolRequestContext context)
    {
        if (!clientCapabilities.Workspace.Diagnostics.RefreshSupport)
            return null;
        if (context.Cancellation.IsCancellationRequested)
            return new OperationCanceledException("context canceled", context.Cancellation);
        // Original fire-and-forget request has no pending result owner.
        var error = SendClientRequestFireAndForget(Method.WorkspaceDiagnosticRefreshInfo, new NoParams());
        return error is null ? null : new InvalidOperationException("failed to refresh diagnostics: " + error.Message, error);
    }

    internal Exception? PublishDiagnostics(ProtocolRequestContext context, PublishDiagnosticsParams parameters) => SendNotification(Method.TextDocumentPublishDiagnosticsInfo, parameters);
    internal Exception? SendTelemetry(ProtocolRequestContext context, TelemetryEvent telemetry)
    {
        if (!telemetryEnabled)
            throw new InvalidOperationException("SendTelemetry called with telemetry disabled");
        return SendNotification(Method.TelemetryEventInfo, telemetry);
    }

    internal Exception? RefreshInlayHints(ProtocolRequestContext context)
    {
        if (!clientCapabilities.Workspace.InlayHint.RefreshSupport)
            return null;
        var error = SendClientRequestFireAndForget(Method.WorkspaceInlayHintRefreshInfo, new NoParams());
        return error is null ? null : new InvalidOperationException("failed to refresh inlay hints: " + error.Message, error);
    }

    internal Exception? RefreshCodeLens(ProtocolRequestContext context)
    {
        if (!clientCapabilities.Workspace.CodeLens.RefreshSupport)
            return null;
        var error = SendClientRequestFireAndForget(Method.WorkspaceCodeLensRefreshInfo, new NoParams());
        return error is null ? null : new InvalidOperationException("failed to refresh code lens: " + error.Message, error);
    }

    internal void ProgressStart(DiagnosticMessage message, params object? [] arguments) => projectProgress?.Start(message, arguments);
    internal void ProgressFinish(DiagnosticMessage message, params object? [] arguments) => projectProgress?.Finish(message, arguments);
}
