// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/lsproto/jsonrpc.go. Deferred JSON values own decoder-independent bytes.
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal sealed class Message
{
    internal MessageKind Kind;
    internal object? Payload;
    internal RequestMessage AsRequest() => (RequestMessage)Payload!;
    internal ResponseMessage AsResponse() => (ResponseMessage)Payload!;
    internal void UnmarshalJSON(ByteStringValue input)
    {
        ProtocolMessageFields raw;
        try
        {
            raw = ProtocolMessageFields.Decode(input);
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            throw new ProtocolErrorException(ErrorCode.InvalidRequest, error.Message, error);
        }

        if (raw.ID is not null && raw.Method.Value.Length == 0)
        {
            Kind = MessageKind.Response;
            Payload = new ResponseMessage
            {
                ID = raw.ID,
                Result = raw.Result ?? RawJsonValue.Nil,
                Error = raw.Error
            };
            return;
        }

        Kind = raw.ID is null ? MessageKind.Notification : MessageKind.Request;
        Payload = new RequestMessage
        {
            ID = raw.ID,
            Method = raw.Method,
            Params = raw.Params
        };
    }

    internal ByteStringValue MarshalJSON()
    {
        var encoder = new ProtocolEncoder();
        encoder.WriteAny(Payload);
        return encoder.Finish();
    }
}

internal sealed partial class RequestMessage : IProtocolWritable
{
    internal ID? ID;
    internal Method Method;
    internal object? Params;
    internal Message Message() => new()
    {
        Kind = ID is null ? MessageKind.Notification : MessageKind.Request,
        Payload = this,
    };
    internal void UnmarshalJSON(ByteStringValue input)
    {
        ProtocolMessageFields raw;
        try
        {
            raw = ProtocolMessageFields.Decode(input, requestOnly: true);
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            throw new ProtocolErrorException(ErrorCode.InvalidRequest, error.Message, error);
        }

        ID = raw.ID;
        Method = raw.Method;
        // The original reused request retains Params when the field is absent.
        if (raw.Params is not null && !raw.Params.Bytes.IsEmpty)
            Params = raw.Params;
    }

    public void WriteProtocolJSON(ProtocolEncoder encoder)
    {
        encoder.BeginObject();
        encoder.WriteProperty("jsonrpc"u8);
        encoder.WriteString(ByteStringValue.FromString("2.0"));
        if (ID is not null)
        {
            encoder.WriteProperty("id"u8);
            ProtocolRpcCodec.WriteID(encoder, ID);
        }

        encoder.WriteProperty("method"u8);
        encoder.WriteString(Method.Value);
        if (Params is not null)
        {
            encoder.WriteProperty("params"u8);
            encoder.WriteAny(Params);
        }

        encoder.EndObject();
    }
}

internal sealed class ResponseMessage : IProtocolWritable
{
    internal ID? ID;
    internal object? Result;
    internal ResponseError? Error;
    internal Message Message() => new()
    {
        Kind = MessageKind.Response,
        Payload = this
    };
    public void WriteProtocolJSON(ProtocolEncoder encoder)
    {
        encoder.BeginObject();
        encoder.WriteProperty("jsonrpc"u8);
        encoder.WriteString(ByteStringValue.FromString("2.0"));
        encoder.WriteProperty("id"u8);
        ProtocolRpcCodec.WriteID(encoder, ID);
        if (Result is not null)
        {
            encoder.WriteProperty("result"u8);
            encoder.WriteAny(Result);
        }

        if (Error is not null)
        {
            encoder.WriteProperty("error"u8);
            ProtocolRpcCodec.WriteResponseError(encoder, Error);
        }

        encoder.EndObject();
    }
}

internal struct ProtocolMessageFields
{
    internal ID? ID;
    internal Method Method;
    internal RawJsonValue? Params;
    internal RawJsonValue? Result;
    internal ResponseError? Error;
    internal static ProtocolMessageFields Decode(ByteStringValue input, bool requestOnly = false)
    {
        ProtocolMessageFields value = default;
        var decoder = new ProtocolDecoder(input);
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            decoder.Finish();
            return value;
        }

        string goType = requestOnly ? """struct { JSONRPC jsonrpc.JSONRPCVersion "json:\"jsonrpc\""; ID *jsonrpc.ID "json:\"id\""; Method lsproto.Method "json:\"method\""; Params jsontext.Value "json:\"params\"" }""" : """struct { JSONRPC jsonrpc.JSONRPCVersion "json:\"jsonrpc\""; Method lsproto.Method "json:\"method\""; ID *jsonrpc.ID "json:\"id,omitzero\""; Params jsontext.Value "json:\"params\""; Result jsontext.Value "json:\"result,omitzero\""; Error *jsonrpc.ResponseError "json:\"error,omitzero\"" }""";
        decoder.ReadObjectStart(goType);
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadDecodedPropertyName();
            if (name.EqualsBytes("jsonrpc"u8))
                ProtocolRpcCodec.ReadVersion(ref decoder);
            else if (name.EqualsBytes("method"u8))
                value.Method = new(decoder.ReadString("lsproto.Method"));
            else if (name.EqualsBytes("id"u8))
                ProtocolRpcCodec.ReadID(ref decoder, ref value.ID);
            else if (name.EqualsBytes("params"u8))
                value.Params = decoder.ReadValue().Clone();
            else if (!requestOnly && name.EqualsBytes("result"u8))
                value.Result = decoder.ReadValue().Clone();
            else if (!requestOnly && name.EqualsBytes("error"u8))
                ProtocolRpcCodec.ReadResponseError(ref decoder, ref value.Error);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
        decoder.Finish();
        return value;
    }
}
