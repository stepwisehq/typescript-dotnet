// Copyright 2020 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Selected encoding/json/v2/errors.go SemanticError ownership and rendering.
using System.Text.Json;
using TypeScript.Json.Internal;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal sealed class ProtocolSemanticError : JsonException
{
    internal int ByteOffset;
    internal ByteStringValue JsonPointer;
    internal byte JsonKind;
    internal ByteStringValue JsonValue;
    internal string GoType = "";
    internal string? Cause;
    // Metadata is enriched in-place when streaming custom UnmarshalJSONFrom
    // methods unwind, just as Go reuses an existing *SemanticError.
    public override string Message => GoString.ToString(JsonTextErrors.SemanticError("unmarshal", KindName(JsonKind), JsonValue.Materialize() ?? "", GoType, JsonPointer.Materialize() ?? "", ByteOffset, Cause));

    private static string KindName(byte kind) => kind switch
    {
        0 => "",
        (byte)'n' => "null",
        (byte)'t' or (byte)'f' => "boolean",
        (byte)'"' => "string",
        (byte)'0' => "number",
        (byte)'{' or (byte)'}' => "object",
        (byte)'[' or (byte)']' => "array",
        _ => throw new InvalidOperationException("invalid JSON semantic error kind")};
}
