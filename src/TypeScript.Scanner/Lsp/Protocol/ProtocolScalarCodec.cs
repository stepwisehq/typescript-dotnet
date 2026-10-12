// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// lsproto/lsp.go Null methods and json/v2 empty-struct default algorithms.
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolCodec
{
    internal static void ReadNull(ref ProtocolDecoder decoder, ref Null value)
    {
        var data = decoder.ReadValue().TextValue;
        if (!data.EqualsBytes("null"u8))
            throw ProtocolErrors.Text(ByteStringValue.Concat(ByteStringValue.FromString("expected null, got "), data));
    }

    internal static void WriteNull(ProtocolEncoder encoder, Null value) => encoder.WriteNull();
    internal static void ReadNoParams(ref ProtocolDecoder decoder, ref NoParams value) => ReadEmptyStruct(ref decoder, "lsproto.NoParams");
    internal static void WriteNoParams(ProtocolEncoder encoder, NoParams value)
    {
        encoder.BeginObject();
        encoder.EndObject();
    }

    internal static void ReadEmptyProtocolObject(ref ProtocolDecoder decoder, ref EmptyProtocolObject value) => ReadEmptyStruct(ref decoder, "struct {}");
    internal static void WriteEmptyProtocolObject(ProtocolEncoder encoder, EmptyProtocolObject value)
    {
        encoder.BeginObject();
        encoder.EndObject();
    }

    private static void ReadEmptyStruct(ref ProtocolDecoder decoder, string goType)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return;
        }

        decoder.ReadObjectStart(goType);
        while (decoder.PeekKind() != '}')
        {
            decoder.ReadDecodedPropertyName();
            decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    internal static void ReadDocumentUri(ref ProtocolDecoder decoder, ref DocumentUri value) => value = new(decoder.ReadString("lsproto.DocumentUri"));
    internal static void ReadURI(ref ProtocolDecoder decoder, ref URI value) => value = new(decoder.ReadString("lsproto.URI"));
    internal static void ReadMethod(ref ProtocolDecoder decoder, ref Method value) => value = new(decoder.ReadString("lsproto.Method"));
    internal static void WriteDocumentUri(ProtocolEncoder encoder, DocumentUri value) => encoder.WriteString(value.Value);
    internal static void WriteURI(ProtocolEncoder encoder, URI value) => encoder.WriteString(value.Value);
    internal static void WriteMethod(ProtocolEncoder encoder, Method value) => encoder.WriteString(value.Value);
}
