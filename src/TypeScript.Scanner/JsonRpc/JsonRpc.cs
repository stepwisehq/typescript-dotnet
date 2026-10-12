// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/jsonrpc/jsonrpc.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.Json;

namespace TypeScript.JsonRpc;
internal readonly struct JSONRPCVersion
{
    internal static readonly Exception ErrInvalidJSONRPCVersion = new InvalidDataException("invalid JSON-RPC version");
    internal static void Unmarshal(ReadOnlySpan<byte> data)
    {
        if (!data.SequenceEqual("\"2.0\""u8))
            throw ErrInvalidJSONRPCVersion;
    }

    internal void Marshal(Utf8JsonWriter writer) => writer.WriteStringValue("2.0");
}

internal sealed partial class ID
{
    private ByteStringValue str;
    private int integer;
    internal static ID NewID(IntegerOrString rawValue) => rawValue.String is { } text ? NewIDString(text) : NewIDInt(rawValue.Integer!.Value);
    internal static ID NewIDString(ByteStringValue str) => new()
    {
        str = str
    };
    internal static ID NewIDInt(int integer) => new()
    {
        integer = integer
    };
    // Empty string is intentionally the integer-zero representation, as in Go.
    internal ByteStringValue String() => str.Length != 0 ? str : ByteStringValue.FromString(integer.ToString(CultureInfo.InvariantCulture));
    internal (int Integer, bool Ok) TryInt() => str.Length == 0 ? (integer, true) : (0, false);
    internal int MustInt() => str.Length == 0 ? integer : throw new InvalidOperationException("ID is not an integer");
    internal void Unmarshal(ReadOnlySpan<byte> data)
    {
        UnmarshalProtocolJSON(ByteStringValue.FromView(ByteStringView.CopyFrom(data)));
    }

    internal void Marshal(Utf8JsonWriter writer)
    {
        if (str.Length != 0)
            RpcJson.WriteString(writer, str);
        else
            writer.WriteNumberValue(integer);
    }
}

internal struct IntegerOrString
{
    public int? Integer;
    public ByteStringValue? String;
}

internal enum MessageKind
{
    Notification,
    Request,
    Response
}

internal static class ErrorCodes
{
    internal const int ParseError = -32700;
    internal const int InvalidRequest = -32600;
    internal const int MethodNotFound = -32601;
    internal const int InvalidParams = -32602;
    internal const int InternalError = -32603;
}

internal sealed class ResponseError : Exception
{
    public int Code;
    public ByteStringValue Text;
    public new object? Data;
    internal ByteStringValue String()
    {
        byte[]? data = null;
        Exception? error = null;
        try
        {
            var encoder = new TypeScript.Lsp.Protocol.ProtocolEncoder();
            encoder.WriteAny(Data);
            data = encoder.Finish().ByteSpan.ToArray();
        }
        catch (Exception failure)when (failure is JsonException or FormatException)
        {
            error = failure;
        }

        var message = ByteStringValue.Concat(ByteStringValue.FromString("[" + Code.ToString(CultureInfo.InvariantCulture) + "]: "), Text);
        if (error is null)
            return message;
        // fmt's %v for []byte prints decimal bytes with spaces, not JSON.
        var formatted = new GoBuilder();
        formatted.WriteString(message);
        formatted.WriteString("\n[");
        if (data is not null)
            for (int index = 0; index < data.Length; index++)
            {
                if (index != 0)
                    formatted.WriteByte(' ');
                formatted.WriteString(data[index].ToString(CultureInfo.InvariantCulture));
            }

        formatted.WriteByte(']');
        return formatted.Value();
    }

    public override string Message => GoString.ToString(String().Materialize() ?? "");
}
