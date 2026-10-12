// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ipc/protocol_jsonrpc.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Ipc;
internal sealed class JSONRPCProtocol : IProtocol
{
    private readonly Reader reader;
    private readonly Writer writer;
    private JSONRPCProtocol(Stream stream)
    {
        reader = Reader.NewReader(stream);
        writer = Writer.NewWriter(stream);
    }

    internal static JSONRPCProtocol NewJSONRPCProtocol(Stream stream) => new(stream);
    public (GenericMessage? Message, Exception? Error) ReadMessage()
    {
        byte[] data;
        try
        {
            data = reader.Read();
        }
        catch (Exception error)when (error is IOException or ObjectDisposedException)
        {
            return (null, error);
        }

        var message = new GenericMessage();
        try
        {
            message.UnmarshalJSON(ByteStringValue.FromView(ByteStringView.FromOwnedBytes(data)));
        }
        catch (System.Text.Json.JsonException error)
        {
            return (null, error);
        }

        return (message, null);
    }

    public Exception? WriteRequest(ID? id, ByteStringValue method, object? parameters) => Write(new GenericRequestMessage { ID = id, Method = method, Params = parameters });
    public Exception? WriteNotification(ByteStringValue method, object? parameters) => Write(new GenericRequestMessage { Method = method, Params = parameters });
    public Exception? WriteResponse(ID? id, object? result) => Write(new GenericResponseMessage { ID = id, Result = result ?? new RawJsonValue("null"u8) });
    public Exception? WriteError(ID? id, ResponseError? error) => Write(new GenericResponseMessage { ID = id, Error = error });
    // The source marshals each complete message before touching the writer; codec
    // errors and I/O errors remain distinct from panic-like handler exceptions.
    private Exception? Write(IProtocolWritable message)
    {
        ByteStringValue data;
        try
        {
            var encoder = new ProtocolEncoder();
            message.WriteProtocolJSON(encoder);
            data = encoder.Finish();
        }
        catch (System.Text.Json.JsonException error)
        {
            return error;
        }

        try
        {
            writer.Write(data.ByteSpan);
        }
        catch (Exception error)when (error is IOException or ObjectDisposedException)
        {
            return error;
        }

        return null;
    }
}
