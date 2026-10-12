// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/lsproto/lsp.go RequestInfo, NotificationInfo and UnmarshalParams.
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal readonly struct RequestInfo<TParams, TResult>(Method method, ProtocolType<TParams> parameters, ProtocolType<TResult> result)
{
    internal readonly Method Method = method;
    internal readonly ProtocolType<TParams> Parameters = parameters;
    internal readonly ProtocolType<TResult> Result = result;
    internal (TResult Value, Exception? Error) UnmarshalResult(object? value)
    {
        if (value is not RawJsonValue raw)
            return (default!, ProtocolErrors.Text(ByteStringValue.FromString("expected json.Value, got " + ProtocolDynamicTypes.Name(value))));
        TResult decoded = default!;
        try
        {
            ProtocolCodec.Unmarshal(Result, raw.TextValue, ref decoded);
            return (decoded, null);
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            // The original deliberately discards any partially decoded result.
            return (default!, error);
        }
    }

    internal RequestMessage NewRequestMessage(ID? id, TParams value) => new()
    {
        ID = id,
        Method = Method,
        Params = new ProtocolPayload<TParams>(Parameters, value),
    };
}

internal readonly struct NotificationInfo<TParams>(Method method, ProtocolType<TParams> parameters)
{
    internal readonly Method Method = method;
    internal readonly ProtocolType<TParams> Parameters = parameters;
    internal RequestMessage NewNotificationMessage(TParams value) => new()
    {
        Method = Method,
        Params = new ProtocolPayload<TParams>(Parameters, value),
    };
}

internal sealed class ProtocolErrorException : Exception
{
    internal readonly ErrorCode Code;
    // lsp.go ErrorCode.Error is the code spelling without a wrapped-message suffix.
    internal ProtocolErrorException(ErrorCode code) : base(GoString.ToString(code.String().Materialize() ?? "")) => Code = code;
    internal ProtocolErrorException(ErrorCode code, string message, Exception? cause = null) : base(GoString.ToString(code.String().Materialize() ?? "") + ": " + message, cause) => Code = code;
}

internal sealed partial class RequestMessage
{
    internal (T Value, Exception? Error) UnmarshalParams<T>(ProtocolType<T> type)
    {
        T parameters = default!;
        RawJsonValue? raw = null;
        if (Params is not null)
        {
            if (Params is not RawJsonValue value)
                return (parameters, new ProtocolErrorException(ErrorCode.InvalidParams, "unexpected params type " + ProtocolDynamicTypes.Name(Params)));
            raw = value;
        }

        if (type.NoParams)
        {
            if (raw is not null && !raw.Bytes.IsEmpty)
                return (parameters, new ProtocolErrorException(ErrorCode.InvalidParams, "expected no params, got " + GoString.ToString(raw.TextValue.Materialize() ?? "")));
            return (parameters, null);
        }

        if (raw is null || raw.Kind is not ((byte)'{' or (byte)'['))
            return (parameters, new ProtocolErrorException(ErrorCode.InvalidParams, "params must be an object or array"));
        try
        {
            ProtocolCodec.Unmarshal(type, raw.TextValue, ref parameters);
            return (parameters, null);
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            // Unlike UnmarshalResult, params retain the original partial decode.
            return (parameters, new ProtocolErrorException(ErrorCode.InvalidParams, error.Message, error));
        }
    }
}
