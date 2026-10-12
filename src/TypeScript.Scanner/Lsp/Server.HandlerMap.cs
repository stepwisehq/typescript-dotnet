// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1260,1336–1407; ignored full synchronous/document wrapper owners.
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private delegate (Func<Exception?>? Work, Exception? Error) Handler(Server server, ProtocolRequestContext context, RequestMessage request);
    private sealed class HandlerMap : Dictionary<Method, Handler>
    {
        internal void RegisterNotificationHandler<TParams>(NotificationInfo<TParams> info, Func<Server, ProtocolRequestContext, TParams, Exception?> function)
        {
            this[info.Method] = (server, context, request) =>
            {
                if (server.session is null && request.Method != Method.Initialized)
                    return (null, new ProtocolErrorException(ErrorCode.ServerNotInitialized));
                var(parameters, error) = request.UnmarshalParams(info.Parameters);
                if (error is not null)
                    return (null, error);
                error = function(server, context, parameters);
                return (null, error ?? CancellationError(context));
            };
        }

        internal void RegisterRequestHandler<TParams, TResult>(RequestInfo<TParams, TResult> info, Func<Server, ProtocolRequestContext, TParams, RequestMessage, (TResult Value, Exception? Error)> function)
        {
            this[info.Method] = (server, context, request) =>
            {
                if (server.session is null && request.Method != Method.Initialize)
                    return (null, new ProtocolErrorException(ErrorCode.ServerNotInitialized));
                var(parameters, error) = request.UnmarshalParams(info.Parameters);
                if (error is not null)
                    return (null, error);
                var(response, requestError) = function(server, context, parameters, request);
                if (requestError is not null)
                    return (null, requestError);
                if (CancellationError(context)is { } canceled)
                    return (null, canceled);
                return (null, server.SendResult(request.ID, new ProtocolPayload<TResult>(info.Result, response)));
            };
        }

        internal void RegisterLanguageServiceDocumentRequestHandler<TParams, TResult>(RequestInfo<TParams, TResult> info, Func<Server, ProtocolRequestContext, LanguageService, TParams, (TResult Value, Exception? Error)> function)
            where TParams : IHasTextDocumentURI?
        {
            this[info.Method] = (server, context, request) =>
            {
                var(parameters, error) = request.UnmarshalParams(info.Parameters);
                if (error is not null)
                    return (null, error);
                var(service, serviceError) = server.session!.GetLanguageService(context, parameters!.TextDocumentURI());
                if (serviceError is not null)
                    return (null, serviceError);
                return (() =>
                {
                    try
                    {
                        var(response, languageServiceError) = function(server, context, service!, parameters);
                        server.session.EnqueuePublishGlobalDiagnostics();
                        if (languageServiceError is not null)
                            return languageServiceError;
                        if (CancellationError(context)is { } canceled)
                            return canceled;
                        return server.SendResult(request.ID, new ProtocolPayload<TResult>(info.Result, response));
                    }
                    catch (Exception panic)
                    {
                        server.Recover(request, panic);
                        return null;
                    }
                }, null);
            };
        }

        // server.go:1404–1441. The Session callback retains the baseline snapshot
        // through returned work; an auto-import retry uses that exact retained owner.
        internal void RegisterLanguageServiceWithAutoImportsRequestHandler<TParams, TResult>(RequestInfo<TParams, TResult> info, Func<Server, ProtocolRequestContext, LanguageService, TParams, (TResult Value, Exception? Error)> function)
            where TParams : IHasTextDocumentURI?
        {
            this[info.Method] = (server, context, request) =>
            {
                var(parameters, error) = request.UnmarshalParams(info.Parameters);
                if (error is not null)
                    return (null, error);
                return server.session!.WithLanguageServiceAndSnapshot(context, parameters!.TextDocumentURI(), (service, snapshot) => (() =>
                {
                    try
                    {
                        var(response, serviceError) = function(server, context, service, parameters);
                        if (NeedsAutoImports(serviceError))
                        {
                            var(withImports, importsError) = server.session.GetLanguageServiceWithAutoImports(context, snapshot, parameters!.TextDocumentURI());
                            if (importsError is not null)
                                return importsError;
                            if (CancellationError(context)is { } canceled)
                                return canceled;
                            (response, serviceError) = function(server, context, withImports!, parameters);
                            if (NeedsAutoImports(serviceError))
                                throw new InvalidOperationException(GoString.ToString(info.Method.Value.Materialize()!) + " returned ErrNeedsAutoImports even after enabling auto imports");
                        }

                        if (serviceError is not null)
                            return serviceError;
                        if (CancellationError(context)is { } cancellation)
                            return cancellation;
                        return server.SendResult(request.ID, new ProtocolPayload<TResult>(info.Result, response));
                    }
                    catch (Exception panic)
                    {
                        server.Recover(request, panic);
                        return null;
                    }
                }, null));
            };
        }

        // server.go:1443–1469. The real default service and orchestrator retain
        // all projects; semantic consumers perform the original cross-project work.
        internal void RegisterMultiProjectReferenceRequestHandler<TParams, TResult>(RequestInfo<TParams, TResult> info, Func<LanguageService, ProtocolRequestContext, TParams, ICrossProjectOrchestrator, (TResult Value, Exception? Error)> function)
            where TParams : IHasTextDocumentPosition?
        {
            this[info.Method] = (server, context, request) =>
            {
                var(parameters, error) = request.UnmarshalParams(info.Parameters);
                if (error is not null)
                    return (null, error);
                var(service, orchestrator, serviceError) = server.GetLanguageServiceAndCrossProjectOrchestrator(context, parameters!.TextDocumentURI(), request);
                if (serviceError is not null)
                    return (null, serviceError);
                return (() =>
                {
                    try
                    {
                        var(response, requestError) = function(service!, context, parameters, orchestrator!);
                        if (requestError is not null)
                            return requestError;
                        if (CancellationError(context)is { } canceled)
                            return canceled;
                        return server.SendResult(request.ID, new ProtocolPayload<TResult>(info.Result, response));
                    }
                    catch (Exception panic)
                    {
                        server.Recover(request, panic);
                        return null;
                    }
                }, null);
            };
        }

        private static bool NeedsAutoImports(Exception? error) => ReferenceEquals(error, LanguageService.ErrNeedsAutoImports) || error?.InnerException is { } inner && NeedsAutoImports(inner);
    }

    private static Exception? CancellationError(ProtocolRequestContext context) => context.Cancellation.IsCancellationRequested ? new OperationCanceledException("context canceled", context.Cancellation) : null;
}
