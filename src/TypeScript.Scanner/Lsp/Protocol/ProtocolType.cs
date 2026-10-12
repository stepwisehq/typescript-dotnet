// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed AOT adaptation of json/v2's cached typed arshalers. Actual generated
// method descriptors retain the Go root shape, including pointer vs value.
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal delegate void ProtocolRead<T>(ref ProtocolDecoder decoder, ref T value);
internal delegate void ProtocolWrite<T>(ProtocolEncoder encoder, T value);
internal sealed class ProtocolType<T>(string goType, ProtocolRead<T> read, ProtocolWrite<T> write, bool noParams = false)
{
    internal readonly string GoType = goType;
    internal readonly ProtocolRead<T> Read = read;
    internal readonly ProtocolWrite<T> Write = write;
    internal readonly bool NoParams = noParams;
}

internal static partial class ProtocolCodec
{
    internal static void Unmarshal<T>(ProtocolType<T> type, ByteStringValue input, ref T value)
    {
        var decoder = new ProtocolDecoder(input);
        var previous = decoder.Capture();
        try
        {
            type.Read(ref decoder, ref value);
            decoder.Finish();
        }
        catch (JsonException error)
        {
            throw decoder.Enrich(type.GoType, previous, error);
        }
    }

    internal static ByteStringValue Marshal<T>(ProtocolType<T> type, T value)
    {
        var encoder = new ProtocolEncoder();
        type.Write(encoder, value);
        return encoder.Finish();
    }
}

// A retained Go `any` carries dynamic type information even for a typed nil
// pointer. Keep the actual root descriptor when the payload crosses a generic
// message slot, so a typed nil is present JSON null instead of absent any.
internal struct ProtocolPayload<T>(ProtocolType<T> type, T value) : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    private readonly ProtocolType<T> descriptor = type;
    private T payload = value;
    string IProtocolTypeNamed.GoType => descriptor.GoType;

    public readonly void WriteProtocolJSON(ProtocolEncoder encoder) => descriptor.Write(encoder, payload);
    public void ReadProtocolJSON(ref ProtocolDecoder decoder) => descriptor.Read(ref decoder, ref payload);
}
