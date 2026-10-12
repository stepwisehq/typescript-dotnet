// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct internal/lsp/server.go:1089–1177 messaging owners, draft only.
using System.Threading.Channels;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    // The copied key retains Go ID value equality even if a caller reuses its ID owner.
    internal (TResult Value, Exception? Error) SendClientRequest<TParams, TResult>(ProtocolRequestContext context, RequestInfo<TParams, TResult> info, TParams parameters)
    {
        var id = ID.NewIDString(ByteStringValue.FromString("ts" + Interlocked.Increment(ref clientSeq).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var request = info.NewRequestMessage(id, parameters);
        var responses = Channel.CreateBounded<ResponseMessage>(new BoundedChannelOptions(1) { SingleReader = true });
        var key = IDKey.FromID(id);
        lock (pendingServerRequestsMu)
            pendingServerRequests[key] = responses;
        try
        {
            var error = Send(request.Message());
            if (error is not null)
                return (default!, error);
            ResponseMessage response;
            try
            {
                response = responses.Reader.ReadAsync(context.Cancellation).AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)when (context.Cancellation.IsCancellationRequested)
            {
                return (default!, new OperationCanceledException("context canceled", context.Cancellation));
            }

            if (response.Error is not null)
                return (default!, new InvalidOperationException(GoString.ToString(ByteStringValue.Concat(ByteStringValue.FromString("request failed: "), response.Error.String()).Materialize() ?? "")));
            return info.UnmarshalResult(response.Result);
        }
        finally
        {
            lock (pendingServerRequestsMu)
                if (pendingServerRequests.Remove(key, out var pending))
                    pending.Writer.TryComplete();
        }
    }

    // Original fire-and-forget requests deliberately have no pending response owner.
    internal Exception? SendClientRequestFireAndForget<TParams, TResult>(RequestInfo<TParams, TResult> info, TParams parameters)
    {
        var id = ID.NewIDString(ByteStringValue.FromString("ts" + Interlocked.Increment(ref clientSeq).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return Send(info.NewRequestMessage(id, parameters).Message());
    }

    private Exception? SendResult(ID? id, object? result) => SendResponse(new ResponseMessage { ID = id, Result = result });
    private sealed class UserFacingRequestFailedError(ByteStringValue text) : Exception(GoString.ToString(text.Materialize() ?? ""))
    {
        internal readonly ErrorCode Code = ErrorCode.RequestFailed;
    }

    private static ErrorCode? FindErrorCode(Exception error)
    {
        if (error is ProtocolErrorException protocol)
            return protocol.Code;
        if (error is UserFacingRequestFailedError)
            return ErrorCode.RequestFailed;
        if (error is MessageMarshalError)
            return ErrorCode.InternalError;
        return error.InnerException is { } inner ? FindErrorCode(inner) : null;
    }

    private Exception? SendError(ID? id, Exception error)
    {
        var classified = FindErrorCode(error);
        if (id is null && classified != ErrorCode.InvalidRequest)
        {
            logger.Errorf("error handling notification: %s", error);
            return null;
        }

        return SendResponse(new ResponseMessage { ID = id, Error = new ResponseError { Code = (int)(classified ?? ErrorCode.InternalError), Text = ByteStringValue.FromString(GoString.ToUtf8(error.Message)) } });
    }

    internal Exception? SendNotification<TParams>(NotificationInfo<TParams> info, TParams parameters) => Send(info.NewNotificationMessage(parameters).Message());
    private Exception? SendResponse(ResponseMessage response) => Send(response.Message());
    internal Exception? Send(ProtocolMessage message) => outgoingQueue.Put(backgroundCtx, message);
}
