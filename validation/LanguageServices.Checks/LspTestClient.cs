// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Full internal/testutil/lsptestutil/lspclient.go owner. Genuine framed byte pipes
// reuse the accepted rendezvous pipe fixture; each direction has its own owner.
using System.Threading.Channels;
using TypeScript.JsonRpc;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;

namespace LanguageServices.Checks;
internal sealed class LspTestClient : IDisposable
{
    internal readonly Server Server;
    private readonly ILspWriter inputWriter;
    private readonly ILspReader outputReader;
    private readonly IpcTestPipe clientOutput, serverOutput;
    private readonly CancellationTokenSource cancellation = new();
    // Untimed original t.Context waits get only a test deadlock-safety deadline.
    private readonly CancellationTokenSource testDeadline = new(TimeSpan.FromSeconds(30));
    private readonly object inputWriterMutex = new(), pendingMutex = new();
    private readonly Dictionary<IDKey, Channel<ResponseMessage>> pending = new();
    private readonly Func<ProtocolRequestContext, RequestMessage, ResponseMessage?>? onServerRequest;
    internal Action<ProtocolRequestContext, RequestMessage>? OnServerNotification;
    private readonly Task<Exception?> serverTask, routerTask;
    private int id;
    private int disposed;
    private Exception? firstWorkerError;
    internal LspTestClient(ServerOptions options, Func<ProtocolRequestContext, RequestMessage, ResponseMessage?>? onServerRequest = null)
    {
        var clientToServer = IpcTestPipe.Pair();
        var serverToClient = IpcTestPipe.Pair();
        clientOutput = clientToServer.Client;
        serverOutput = serverToClient.Server;
        options.In = LspTransport.ToReader(clientToServer.Server);
        options.Out = LspTransport.ToWriter(serverOutput);
        inputWriter = LspTransport.ToWriter(clientOutput);
        outputReader = LspTransport.ToReader(serverToClient.Client);
        Server = Server.NewServer(options);
        this.onServerRequest = onServerRequest;
        serverTask = Task.Factory.StartNew(() =>
        {
            try
            {
                var error = Server.Run(cancellation.Token);
                if (error is not null)
                {
                    Interlocked.CompareExchange(ref firstWorkerError, error, null);
                    cancellation.Cancel();
                }
                return error;
            }
            finally
            {
                serverOutput.Dispose();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        routerTask = Task.Factory.StartNew(() =>
        {
            var error = MessageRouter(cancellation.Token);
            if (error is not null)
            {
                Interlocked.CompareExchange(ref firstWorkerError, error, null);
                cancellation.Cancel();
            }
            return error;
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private Exception? WriteToServer(ProtocolMessage message)
    {
        lock (inputWriterMutex)
            return inputWriter.Write(message);
    }

    internal int NextID() => id++;
    private Exception? MessageRouter(CancellationToken token)
    {
        while (true)
        {
            var(message, error) = outputReader.Read();
            if (error is not null)
            {
                if (error is EndOfStreamException || token.IsCancellationRequested)
                    return null;
                return new IOException("failed to read message: " + error.Message, error);
            }

            if (token.IsCancellationRequested)
                continue;
            try
            {
                _ = message!.MarshalJSON();
            }
            catch (Exception exception)when (exception is System.Text.Json.JsonException or FormatException)
            {
                if (token.IsCancellationRequested)
                    continue;
                return new IOException("failed to encode message as JSON: " + exception.Message, exception);
            }

            switch (message!.Kind)
            {
                case MessageKind.Response:
                    HandleResponse(token, message.AsResponse());
                    break;
                case MessageKind.Request:
                    if (HandleServerRequest(token, message.AsRequest())is { } requestError)
                        return requestError;
                    break;
                case MessageKind.Notification:
                    OnServerNotification?.Invoke(ProtocolRequestContext.Default.WithCancellationToken(token), message.AsRequest());
                    break;
            }
        }
    }

    private void HandleResponse(CancellationToken token, ResponseMessage response)
    {
        if (response.ID is null)
            return;
        Channel<ResponseMessage>? channel;
        lock (pendingMutex)
            pending.Remove(IDKey.FromID(response.ID), out channel);
        if (channel is not null)
        {
            try
            {
                channel.Writer.WriteAsync(response, token).AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
            }
        }
    }

    private Exception? HandleServerRequest(CancellationToken token, RequestMessage request)
    {
        var response = onServerRequest?.Invoke(ProtocolRequestContext.Default.WithCancellationToken(token), request);
        response ??= new ResponseMessage
        {
            ID = request.ID,
            Error = new ResponseError
            {
                Code = (int)ErrorCode.MethodNotFound,
                Text = ByteStringValue.Concat(ByteStringValue.FromString("Unknown method: "), request.Method.Value)
            }
        };
        if (token.IsCancellationRequested)
            return null;
        var error = WriteToServer(response.Message());
        return error is null || token.IsCancellationRequested ? null : new IOException("failed to write server request response: " + error.Message, error);
    }

    internal void WriteMessage(ProtocolMessage message)
    {
        _ = message.MarshalJSON();
        if (WriteToServer(message)is { } error)
            throw new IOException("failed to write message: " + error.Message, error);
    }

    private Channel<ResponseMessage> StartRequest(RequestMessage request, ID requestID)
    {
        var responses = Channel.CreateBounded<ResponseMessage>(1);
        lock (pendingMutex)
            pending[IDKey.FromID(requestID)] = responses;
        WriteMessage(request.Message());
        return responses;
    }

    private (ResponseMessage? Response, bool Ok) WaitForResponse(ID requestID, Channel<ResponseMessage> channel)
    {
        try
        {
            return (channel.Reader.ReadAsync(testDeadline.Token).AsTask().GetAwaiter().GetResult(), true);
        }
        catch (OperationCanceledException)when (testDeadline.IsCancellationRequested)
        {
            lock (pendingMutex)
                pending.Remove(IDKey.FromID(requestID));
            throw new TimeoutException("Request cancelled: original untimed wait exceeded test deadlock guard");
        }
    }

    internal (ProtocolMessage? Message, TResult Result, bool Ok) SendRequest<TParams, TResult>(RequestInfo<TParams, TResult> info, TParams parameters) => SendRequestAsync(info, parameters)();
    internal Func<(ProtocolMessage? Message, TResult Result, bool Ok)> SendRequestAsync<TParams, TResult>(RequestInfo<TParams, TResult> info, TParams parameters)
    {
        var requestID = ID.NewIDInt(NextID());
        var responses = StartRequest(info.NewRequestMessage(requestID, parameters), requestID);
        return () =>
        {
            var(response, ok) = WaitForResponse(requestID, responses);
            if (!ok || response is null)
                return (null, default!, false);
            var(result, error) = info.UnmarshalResult(response.Result);
            return (response.Message(), result, error is null);
        };
    }

    internal (ResponseMessage? Response, bool Ok) SendRequestWorker(RequestMessage request, ID requestID) => WaitForResponse(requestID, StartRequest(request, requestID));
    internal void SendNotification<TParams>(NotificationInfo<TParams> info, TParams parameters) => WriteMessage(info.NewNotificationMessage(parameters).Message());
    internal void SetCompilerOptionsForInferredProjects(CompilerOptions options) => Server.SetCompilerOptionsForInferredProjects(ProtocolRequestContext.Default.WithCancellationToken(cancellation.Token), options);
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        cancellation.Cancel();
        clientOutput.Dispose();
        Task.WhenAll(serverTask, routerTask).WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        Exception? error = firstWorkerError;
        testDeadline.Dispose();
        cancellation.Dispose();
        if (error is not null && error is not OperationCanceledException)
            throw error;
    }
}
