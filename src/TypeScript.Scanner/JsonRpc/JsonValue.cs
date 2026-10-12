// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Raw json.Value ownership adapter for jsonrpc/lsproto deferred fields.
using System.Text.Json;
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.JsonRpc;
// Retained decoder values own their bytes. No JsonElement or pooled decoder
// buffer escapes; this matches jsontext.Value.Clone on deferred union fields.
internal sealed partial class RawJsonValue
{
    private readonly byte[] bytes;
    internal RawJsonValue(ReadOnlySpan<byte> value) => bytes = value.ToArray();
    // Go json.Value assignment copies a slice header. This separate factory is
    // for already-owned buffers retained by outgoing DTOs; decoding still uses
    // the copying constructor above. Both owners must treat the buffer as immutable.
    internal static RawJsonValue FromRetainedBytes(byte[] value) => new(value, retained: true);
    private RawJsonValue(byte[] value, bool retained)
    {
        ArgumentNullException.ThrowIfNull(value);
        bytes = value;
        IsNil = false;
    }
    internal ReadOnlySpan<byte> Bytes => bytes;
    // Required content-mapper json.Value join: read-only consumers borrow the
    // retained immutable owner rather than cloning before FormatRawValue.
    // Callers must not mutate this array or expose it as a writable public buffer.
    internal byte[] BorrowOwnedBytes() => bytes;

    internal byte Kind
    {
        get
        {
            int position = 0;
            while (position < bytes.Length && bytes[position] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                position++;
            return position < bytes.Length ? bytes[position] : (byte)0;
        }
    }

    internal ByteStringValue TextValue => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
}

internal static class RpcJson
{
    internal static void Validate(ReadOnlySpan<byte> data)
    {
        var error = JsonCodec.ValidateValue(new JsonValue(data.ToArray()));
        if (error is not null)
            throw error;
    }

    internal static ByteStringValue ReadString(ReadOnlySpan<byte> data)
    {
        Validate(data);
        var reader = new Utf8JsonReader(data);
        if (!reader.Read())
            throw new JsonException("unexpected end of JSON input");
        if (reader.TokenType == JsonTokenType.Null)
            return default;
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("cannot unmarshal JSON value into Go string");
        // Go decoding owns the resulting string. CopyString unescapes directly
        // into UTF8 and avoids an intermediate managed UTF16 string owner.
        if (!reader.ValueIsEscaped)
            return ByteStringValue.FromView(ByteStringView.CopyFrom(reader.ValueSpan));
        var owned = new byte[reader.ValueSpan.Length];
        int written = reader.CopyString(owned);
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(owned)).Slice(0, written);
    }

    internal static int ReadInt32(ReadOnlySpan<byte> data)
    {
        Validate(data);
        var reader = new Utf8JsonReader(data);
        if (!reader.Read())
            throw new JsonException("unexpected end of JSON input");
        if (reader.TokenType == JsonTokenType.Null)
            return 0;
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
            throw new JsonException("cannot unmarshal JSON value into Go int32");
        return value;
    }

    internal static void WriteString(Utf8JsonWriter writer, ByteStringValue value)
    {
        // Write the actual Go quotation as a raw value so Utf8JsonWriter's
        // independent Unicode block-list escaping cannot change protocol bytes.
        var output = new GoBuilder();
        JsonCodec.QuoteToBuilder(output, value);
        var quoted = output.Value();
        writer.WriteRawValue(quoted.ByteSpan, skipInputValidation: true);
    }
}
