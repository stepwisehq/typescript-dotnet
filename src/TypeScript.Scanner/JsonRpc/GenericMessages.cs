// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/jsonrpc/jsonrpc.go generic message owners and closed default field codecs.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.JsonRpc;
internal sealed class GenericMessage : IProtocolWritable
{
    internal ID? ID;
    internal ByteStringValue Method;
    internal RawJsonValue? Params;
    internal RawJsonValue? Result;
    internal ResponseError? Error;
    internal MessageKind Kind() => ID is not null && Method.Length == 0 ? MessageKind.Response : ID is null ? MessageKind.Notification : MessageKind.Request;
    internal bool IsRequest() => ID is not null && Method.Length != 0;
    internal bool IsNotification() => ID is null && Method.Length != 0;
    internal bool IsResponse() => ID is not null && Method.Length == 0;
    internal void UnmarshalJSON(ByteStringValue input)
    {
        var decoder = new ProtocolDecoder(input);
        var previous = decoder.Capture();
        try
        {
            if (decoder.PeekKind() == 'n')
            {
                decoder.ReadToken((byte)'n');
                ID = null;
                Method = default;
                Params = null;
                Result = null;
                Error = null;
                decoder.Finish();
                return;
            }

            decoder.ReadObjectStart("jsonrpc.Message");
            while (decoder.PeekKind() != '}')
            {
                var name = decoder.ReadDecodedPropertyName();
                if (name.EqualsBytes("jsonrpc"u8))
                    ProtocolRpcCodec.ReadVersion(ref decoder);
                else if (name.EqualsBytes("id"u8))
                    ProtocolRpcCodec.ReadID(ref decoder, ref ID);
                else if (name.EqualsBytes("method"u8))
                    Method = decoder.ReadString();
                else if (name.EqualsBytes("params"u8))
                    Params = decoder.ReadValue().Clone();
                else if (name.EqualsBytes("result"u8))
                    Result = decoder.ReadValue().Clone();
                else if (name.EqualsBytes("error"u8))
                    ProtocolRpcCodec.ReadResponseError(ref decoder, ref Error);
                else
                    decoder.SkipValue();
            }

            decoder.ReadToken((byte)'}');
            decoder.Finish();
        }
        catch (System.Text.Json.JsonException error)
        {
            throw decoder.Enrich("jsonrpc.Message", previous, error);
        }
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

        if (Method.Length != 0)
        {
            encoder.WriteProperty("method"u8);
            encoder.WriteString(Method);
        }

        if (Params is not null && !Params.Bytes.IsEmpty)
        {
            encoder.WriteProperty("params"u8);
            encoder.WriteAny(Params);
        }

        if (Result is not null && !Result.Bytes.IsEmpty)
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

internal sealed class GenericRequestMessage : IProtocolWritable
{
    internal ID? ID;
    internal ByteStringValue Method;
    internal object? Params;
    internal void UnmarshalJSON(ByteStringValue input)
    {
        var decoder = new ProtocolDecoder(input);
        var previous = decoder.Capture();
        try
        {
            if (decoder.PeekKind() == 'n')
            {
                decoder.ReadToken((byte)'n');
                ID = null;
                Method = default;
                Params = null;
                decoder.Finish();
                return;
            }

            decoder.ReadObjectStart("jsonrpc.RequestMessage");
            while (decoder.PeekKind() != '}')
            {
                var name = decoder.ReadDecodedPropertyName();
                if (name.EqualsBytes("jsonrpc"u8))
                    ProtocolRpcCodec.ReadVersion(ref decoder);
                else if (name.EqualsBytes("id"u8))
                    ProtocolRpcCodec.ReadID(ref decoder, ref ID);
                else if (name.EqualsBytes("method"u8))
                    Method = decoder.ReadString();
                else if (name.EqualsBytes("params"u8))
                    decoder.ReadAny(ref Params);
                else
                    decoder.SkipValue();
            }

            decoder.ReadToken((byte)'}');
            decoder.Finish();
        }
        catch (System.Text.Json.JsonException error)
        {
            throw decoder.Enrich("jsonrpc.RequestMessage", previous, error);
        }
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
        encoder.WriteString(Method);
        if (Params is not null)
        {
            encoder.WriteProperty("params"u8);
            encoder.WriteAny(Params);
        }

        encoder.EndObject();
    }
}

internal sealed class GenericResponseMessage : IProtocolWritable
{
    internal ID? ID;
    internal object? Result;
    internal ResponseError? Error;
    internal void UnmarshalJSON(ByteStringValue input)
    {
        var decoder = new ProtocolDecoder(input);
        var previous = decoder.Capture();
        try
        {
            if (decoder.PeekKind() == 'n')
            {
                decoder.ReadToken((byte)'n');
                ID = null;
                Result = null;
                Error = null;
                decoder.Finish();
                return;
            }

            decoder.ReadObjectStart("jsonrpc.ResponseMessage");
            while (decoder.PeekKind() != '}')
            {
                var name = decoder.ReadDecodedPropertyName();
                if (name.EqualsBytes("jsonrpc"u8))
                    ProtocolRpcCodec.ReadVersion(ref decoder);
                else if (name.EqualsBytes("id"u8))
                    ProtocolRpcCodec.ReadID(ref decoder, ref ID);
                else if (name.EqualsBytes("result"u8))
                    decoder.ReadAny(ref Result);
                else if (name.EqualsBytes("error"u8))
                    ProtocolRpcCodec.ReadResponseError(ref decoder, ref Error);
                else
                    decoder.SkipValue();
            }

            decoder.ReadToken((byte)'}');
            decoder.Finish();
        }
        catch (System.Text.Json.JsonException error)
        {
            throw decoder.Enrich("jsonrpc.ResponseMessage", previous, error);
        }
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
