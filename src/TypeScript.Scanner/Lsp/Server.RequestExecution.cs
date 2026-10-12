// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1180–1259; ignored whole-server draft.
using System.Diagnostics;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using ProtocolDiagnostic = TypeScript.Lsp.Protocol.Diagnostic;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (Func<Exception?>? Work, Exception? Error) HandleRequestOrNotification(ProtocolRequestContext context, RequestMessage request)
    {
        context = context.WithClientCapabilities(clientCapabilities);
        if (HandlerRegistry.Value.TryGetValue(request.Method, out var handler))
        {
            long start = Stopwatch.GetTimestamp();
            var(work, error) = handler(this, context, request);
            string id = request.ID is null ? "" : " (" + request.ID.String() + ")";
            void LogHandled() => logger.Info("handled method '", request.Method.Value, "'", id, " in ", Stopwatch.GetElapsedTime(start));
            if (error is not null)
            {
                var(fallback, found) = ContentMapperFallbackResponse(request.Method, error);
                if (found)
                {
                    if (!logger.IsTracing())
                        LogHandled();
                    return (null, SendResult(request.ID, fallback));
                }

                if (!HasUserFacingError(error))
                    logger.Error("error handling method '", request.Method.Value, "'", id, ": ", error);
                else if (!logger.IsTracing())
                    LogHandled();
                return (null, error);
            }

            if (work is not null)
                return (() =>
                {
                    // Cancellation is checked by the actual work so its cleanup runs.
                    var asyncError = work();
                    if (asyncError is not null && !HasUserFacingError(asyncError))
                        logger.Info("error handling method '", request.Method.Value, "'", id, " in ", Stopwatch.GetElapsedTime(start));
                    else if (!logger.IsTracing())
                        LogHandled();
                    return asyncError;
                }, null);
            if (!logger.IsTracing())
                LogHandled();
            return (null, null);
        }

        logger.Warn("unknown method '", request.Method.Value, "'");
        return (null, request.ID is not null ? SendError(request.ID, new ProtocolErrorException(ErrorCode.InvalidRequest)) : null);
    }

    private static bool HasUserFacingError(Exception error) => error is UserFacingRequestFailedError || error.InnerException is { } inner && HasUserFacingError(inner);
    private static bool IsUnknownScriptError(Exception error) => ReferenceEquals(error, TypeScript.ProjectSystem.Session.ErrNoProjectForUnknownScriptKind) || error.InnerException is { } inner && IsUnknownScriptError(inner);
    private static (object? Response, bool Found) ContentMapperFallbackResponse(Method method, Exception error)
    {
        if (!IsUnknownScriptError(error))
            return (null, false);
        if (method == Method.TextDocumentDiagnostic)
        {
            var response = new RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport
            {
                FullDocumentDiagnosticReport = new RelatedFullDocumentDiagnosticReport
                {
                    Items = Slice<ProtocolDiagnostic?>.Make(0)
                }
            };
            return (new ProtocolPayload<RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport>(Method.TextDocumentDiagnosticInfo.Result, response), true);
        }

        if (method == Method.TextDocumentHover || method == Method.TextDocumentSignatureHelp || method == Method.TextDocumentDefinition || method == Method.TextDocumentTypeDefinition || method == Method.TextDocumentImplementation || method == Method.TextDocumentReferences || method == Method.TextDocumentDocumentHighlight || method == Method.TextDocumentCompletion || method == Method.TextDocumentRename)
            return (new ProtocolPayload<Null>(ProtocolCodec.GetType_fa1d2db62d4d952e(), new Null()), true);
        return (null, false);
    }
}
