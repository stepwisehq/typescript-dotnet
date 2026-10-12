// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct internal/lsp/server.go:895–1087 Run/read/dispatch/write owners; draft only.
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;
using IntegerOrString = TypeScript.Lsp.Protocol.IntegerOrString;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal Exception? Run(CancellationToken context)
    {
        using var groupContext = CancellationTokenSource.CreateLinkedTokenSource(context);
        backgroundCtx = groupContext.Token;
        using var completion = new CountdownEvent(3);
        Exception? firstError = null;
        var readResult = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void GroupWorker(Func<Exception?> operation)
        {
            // Returned errors cancel sibling loops; thrown panic escapes the void
            // thread entry exactly as the source goroutine rather than a detached Task.
            StartLoopThread(() =>
            {
                try
                {
                    var error = operation();
                    if (error is not null && Interlocked.CompareExchange(ref firstError, error, null)is null)
                        groupContext.Cancel();
                }
                finally
                {
                    completion.Signal();
                }
            });
        }

        GroupWorker(() => DispatchLoop(backgroundCtx));
        GroupWorker(() => WriteLoop(backgroundCtx));
        GroupWorker(() =>
        {
            try
            {
                return readResult.Task.WaitAsync(backgroundCtx).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)when (backgroundCtx.IsCancellationRequested)
            {
                return new OperationCanceledException("context canceled", backgroundCtx);
            }
        });
        // Read is deliberately not joined: the original stdin reader cannot be
        // cancelled and sends to its capacity-one completion channel after Run exits.
        StartLoopThread(() => readResult.SetResult(ReadLoop(backgroundCtx)));
        completion.Wait();
        groupContext.Cancel(); // errgroup.Wait cancels its derived context even on nil.
        return firstError is not null && firstError is not EndOfStreamException ? firstError : null;
    }

    private static void StartLoopThread(Action operation)
    {
        // Dedicated synchronous loop threads are the bounded goroutine adaptation:
        // starting a blocked read/Monitor wait cannot prevent a sibling loop starting.
        new Thread(() => operation())
        {
            IsBackground = true
        }.Start();
    }

    private Exception? ReadLoop(CancellationToken context)
    {
        while (true)
        {
            if (context.IsCancellationRequested)
                return new OperationCanceledException("context canceled", context);
            var(message, error) = Read();
            if (error is not null)
            {
                var code = FindErrorCode(error);
                if (code is ErrorCode.InvalidRequest or ErrorCode.InvalidParams)
                {
                    ID? id = code == ErrorCode.InvalidParams && message?.Kind == MessageKind.Request ? message.AsRequest().ID : null;
                    if (SendError(id, error)is { } sendError)
                        return sendError;
                    continue;
                }

                return error;
            }

            if (initializeParams is null && message!.Kind == MessageKind.Request)
            {
                var request = message.AsRequest();
                if (request.Method == Method.Initialize)
                {
                    var(parameters, parameterError) = request.UnmarshalParams(Method.InitializeInfo.Parameters);
                    if (parameterError is not null)
                    {
                        if (SendError(request.ID, parameterError)is { } sendError)
                            return sendError;
                        continue;
                    }

                    var(response, initializeError) = HandleInitialize(new ProtocolRequestContext(context, GetLocale(), clientCapabilities), parameters!, request);
                    if (initializeError is not null)
                        return initializeError;
                    if (SendResult(request.ID, new ProtocolPayload<InitializeResult?>(Method.InitializeInfo.Result, response))is { } responseError)
                        return responseError;
                }
                else if (SendError(request.ID, new ProtocolErrorException(ErrorCode.ServerNotInitialized))is { } errorBeforeInitialize)
                    return errorBeforeInitialize;
                continue;
            }

            if (message!.Kind == MessageKind.Response)
            {
                var response = message.AsResponse();
                lock (pendingServerRequestsMu)
                    if (pendingServerRequests.Remove(IDKey.FromID(response.ID!), out var pending))
                    {
                        pending.Writer.TryWrite(response);
                        pending.Writer.TryComplete();
                    }
            }
            else
            {
                var request = message.AsRequest();
                if (request.Method == Method.CancelRequest)
                {
                    var(parameters, parameterError) = request.UnmarshalParams(Method.CancelRequestInfo.Parameters);
                    if (parameterError is null && parameters is not null)
                        CancelRequest(parameters.Id);
                }
                else if (requestQueue.Put(context, request)is { } queueError)
                    return queueError;
            }
        }
    }

    private void CancelRequest(IntegerOrString rawID)
    {
        var id = ID.NewID(new TypeScript.JsonRpc.IntegerOrString { Integer = rawID.Integer, String = rawID.String });
        lock (pendingClientRequestsMu)
            if (pendingClientRequests.Remove(IDKey.FromID(id), out var pending))
                pending.Cancel.Cancel();
    }

    private (ProtocolMessage? Message, Exception? Error) Read() => r.Read();
    internal Exception? WriteLoop(CancellationToken context)
    {
        while (true)
        {
            var(message, queueError) = outgoingQueue.Get(context);
            if (queueError is not null)
                return queueError;
            if (w.Write(message)is { } error)
            {
                if (error is MessageMarshalError marshalError && message.Kind == MessageKind.Response)
                {
                    var response = message.AsResponse();
                    if (response.ID is not null && response.Error is null)
                    {
                        logger.Errorf("failed to marshal response for request %s: %v", response.ID.String(), marshalError);
                        if (SendError(response.ID, marshalError)is { } sendError)
                            return sendError;
                        continue;
                    }
                }

                return new IOException("failed to write message: " + error.Message, error);
            }
        }
    }
}
