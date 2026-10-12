// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct internal/lsp/server.go:1004–1066; ignored coherent-server draft.
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private Exception? DispatchLoop(CancellationToken context)
    {
        // Async handlers may outlive dispatch; their captured cancellation owner must
        // remain usable after this loop returns, like the retained Go cancel closure.
        var exit = CancellationTokenSource.CreateLinkedTokenSource(context);
        Exception? exitCause = null;
        void Exit(Exception? cause)
        {
            if (cause is not null)
                Interlocked.CompareExchange(ref exitCause, cause, null);
            exit.Cancel();
        }

        while (true)
        {
            var(request, queueError) = requestQueue.Get(exit.Token);
            if (queueError is not null)
                return exitCause ?? queueError;
            var req = request!;
            Interlocked.Exchange(ref lastRequestTimeMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var requestContext = ProtocolRequestContext.Default.WithCancellationToken(exit.Token).WithLocale(GetLocale());
            CancellationTokenSource? cancel = null;
            if (req.ID is not null)
            {
                cancel = CancellationTokenSource.CreateLinkedTokenSource(requestContext.Cancellation);
                requestContext = requestContext.WithRequestID(req.ID.String()).WithCancellationToken(cancel.Token);
                lock (pendingClientRequestsMu)
                    pendingClientRequests[IDKey.FromID(req.ID)] = new PendingClientRequest(req, cancel);
            }

            void HandleError(Exception error)
            {
                if (error is OperationCanceledException)
                {
                    if (SendError(req.ID, new ProtocolErrorException(ErrorCode.RequestCancelled))is { } sendError)
                        Exit(sendError);
                }
                else if (error is EndOfStreamException)
                    Exit(null);
                else if (SendError(req.ID, error)is { } sendError)
                    Exit(sendError);
            }

            void RemoveRequest()
            {
                if (req.ID is null)
                    return;
                // Source defers cancellation until after the pending-map lock releases.
                lock (pendingClientRequestsMu)
                    pendingClientRequests.Remove(IDKey.FromID(req.ID));
                cancel!.Cancel();
                cancel.Dispose();
            }

            var(asyncWork, error) = HandleRequestOrNotification(requestContext, req);
            if (error is not null)
            {
                HandleError(error);
                RemoveRequest();
            }
            else if (asyncWork is not null)
            {
                // A void worker preserves unhandled goroutine panic behavior; source
                // cleanup occurs only after a normally returned work/error value.
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    if (asyncWork()is { } asyncError)
                        HandleError(asyncError);
                    RemoveRequest();
                }, (object? )null);
            }
            else
                RemoveRequest();
        }
    }
}
