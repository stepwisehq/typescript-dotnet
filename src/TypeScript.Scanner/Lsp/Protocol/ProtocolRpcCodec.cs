// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/jsonrpc/jsonrpc.go typed field joins, retaining pointer allocation and partial writes.
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static class ProtocolRpcCodec
{
    internal static void ReadVersion(ref ProtocolDecoder decoder)
    {
        var raw = decoder.ReadValue();
        if (!raw.TextValue.ByteSpan.SequenceEqual("\"2.0\""u8))
            throw decoder.ErrorAfter("jsonrpc.JSONRPCVersion", "invalid JSON-RPC version");
    }

    internal static void ReadID(ref ProtocolDecoder decoder, ref ID? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        value ??= ID.NewIDInt(0);
        var raw = decoder.ReadValue();
        try
        {
            value.UnmarshalProtocolJSON(raw.TextValue);
        }
        catch (ProtocolSemanticError error)
        {
            // json/v2 byte-method adapters wrap then collapse inner semantic
            // metadata by adding the outer value's offset and JSON pointer.
            var outer = decoder.ErrorAfter("jsonrpc.ID");
            error.ByteOffset = checked(outer.ByteOffset + error.ByteOffset);
            error.JsonPointer = ByteStringValue.Concat(outer.JsonPointer, error.JsonPointer);
            throw;
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            throw decoder.ErrorAfter("jsonrpc.ID", GoString.ToUtf8(error.Message));
        }
    }

    internal static void WriteID(ProtocolEncoder encoder, ID? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        var(number, integer) = value.TryInt();
        if (integer)
            encoder.WriteNumber(number);
        else
            encoder.WriteString(value.String());
    }

    internal static void ReadResponseError(ref ProtocolDecoder decoder, ref ResponseError? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        value ??= new ResponseError();
        decoder.ReadObjectStart("jsonrpc.ResponseError");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadDecodedPropertyName();
            if (name.EqualsBytes("code"u8))
                value.Code = decoder.ReadInt32();
            else if (name.EqualsBytes("message"u8))
                value.Text = decoder.ReadString();
            else if (name.EqualsBytes("data"u8))
                decoder.ReadAny(ref value.Data);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    internal static void WriteResponseError(ProtocolEncoder encoder, ResponseError value)
    {
        encoder.BeginObject();
        encoder.WriteProperty("code"u8);
        encoder.WriteNumber(value.Code);
        encoder.WriteProperty("message"u8);
        encoder.WriteString(value.Text);
        if (value.Data is not null)
        {
            encoder.WriteProperty("data"u8);
            encoder.WriteAny(value.Data);
        }

        encoder.EndObject();
    }
}
