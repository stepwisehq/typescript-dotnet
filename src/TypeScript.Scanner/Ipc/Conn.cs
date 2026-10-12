// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete required internal/ipc/conn.go contracts; closed descriptors replace reflection.
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Ipc;
internal interface IHandler
{
    (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters);
    Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters);
}

internal interface IConn
{
    Exception? Run(CancellationToken context);
    (RawJsonValue? Result, Exception? Error) Call(CancellationToken context, ByteStringValue method, object? parameters);
    Exception? Notify(CancellationToken context, ByteStringValue method, object? parameters);
}

internal static partial class Ipc
{
    internal static readonly Exception ErrConnClosed = new IOException("ipc: connection closed");
    internal static readonly Exception ErrRequestTimeout = new IOException("ipc: request timeout");
    internal static (ProtocolPointer<T>? Value, Exception? Error) UnmarshalParams<T>(RawJsonValue? parameters, ProtocolType<T> descriptor)
    {
        if (parameters is null || parameters.Bytes.IsEmpty)
            return (null, null);
        T value = default!;
        try
        {
            ProtocolCodec.Unmarshal(descriptor, parameters.TextValue, ref value);
            return (new ProtocolPointer<T>(value), null);
        }
        catch (System.Text.Json.JsonException error)
        {
            return (null, error);
        }
    }
}
