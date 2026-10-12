// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct lsproto/lsp.go helpers with streaming semantic JSON error routing.
using System.Text.Json;
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static class ProtocolErrors
{
    internal static JsonException Text(ByteStringValue message) => new(GoString.ToString(message.Materialize() ?? ""));
    private static string Kind(byte kind) => kind switch
    {
        0 => "invalid",
        (byte)'n' => "null",
        (byte)'f' => "false",
        (byte)'t' => "true",
        (byte)'"' => "string",
        (byte)'0' => "number",
        (byte)'{' => "{",
        (byte)'}' => "}",
        (byte)'[' => "[",
        (byte)']' => "]",
        _ => throw new InvalidOperationException("invalid normalized JSON kind")};
    internal static JsonException NotObject(byte kind) => new("expected object start, but encountered " + Kind(kind));
    internal static JsonException Null(ByteStringValue name)
    {
        var quoted = new GoBuilder();
        JsonCodec.QuoteToBuilder(quoted, name);
        return Text(ByteStringValue.Concat(ByteStringValue.FromString("null value is not allowed for field "), quoted.Value()));
    }

    internal static JsonException Missing(Slice<ByteStringValue> names)
    {
        var message = new GoBuilder();
        message.WriteString("missing required properties: ");
        for (int i = 0; i < names.Length; i++)
        {
            if (i != 0)
                message.WriteString(", ");
            message.WriteString(names[i]);
        }

        return Text(message.Value());
    }

    internal static JsonException InvalidKind(string typeName, byte kind) => new("invalid " + typeName + ": got " + Kind(kind));
    internal static JsonException InvalidValue(string typeName, ByteStringValue data) => Text(ByteStringValue.Concat(ByteStringValue.FromString("invalid " + typeName + ": "), data));
    internal static JsonException LiteralMismatch(string typeName, string expected, ByteStringValue got) => Text(ByteStringValue.Concat(ByteStringValue.FromString("expected " + typeName + " value " + expected + ", got "), got));
    internal static JsonException ArrayLength(int actual, int expected) => new(actual > expected ? "too many array elements" : "too few array elements");
}
