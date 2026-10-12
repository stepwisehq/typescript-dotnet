// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed descriptors for existing internal/jsonrpc/jsonrpc.go message roots.
// Value roots retain a Go zero struct; pointer roots retain actual JSON null.
using TypeScript.Lsp.Protocol;

namespace TypeScript.JsonRpc;
internal static class GenericMessageCodecs
{
    internal static readonly ProtocolType<GenericMessage?> MessagePointer = new("*jsonrpc.Message", ReadMessagePointer, WriteMessagePointer);
    internal static readonly ProtocolType<GenericRequestMessage> RequestValue = new("jsonrpc.RequestMessage", ReadRequest, WriteRequest);
    internal static readonly ProtocolType<GenericResponseMessage> ResponseValue = new("jsonrpc.ResponseMessage", ReadResponse, WriteResponse);
    private static void ReadMessagePointer(ref ProtocolDecoder decoder, ref GenericMessage? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        value ??= new();
        value.UnmarshalJSON(decoder.ReadValue().TextValue);
    }

    private static void WriteMessagePointer(ProtocolEncoder encoder, GenericMessage? value)
    {
        if (value is null)
            encoder.WriteNull();
        else
            value.WriteProtocolJSON(encoder);
    }

    private static void ReadRequest(ref ProtocolDecoder decoder, ref GenericRequestMessage value)
    {
        value ??= new();
        value.UnmarshalJSON(decoder.ReadValue().TextValue);
    }

    private static void WriteRequest(ProtocolEncoder encoder, GenericRequestMessage value) => (value ?? new()).WriteProtocolJSON(encoder);
    private static void ReadResponse(ref ProtocolDecoder decoder, ref GenericResponseMessage value)
    {
        value ??= new();
        value.UnmarshalJSON(decoder.ReadValue().TextValue);
    }

    private static void WriteResponse(ProtocolEncoder encoder, GenericResponseMessage value) => (value ?? new()).WriteProtocolJSON(encoder);
}
