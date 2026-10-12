// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1515–1535; actual CLR exception stack boundary, no fabricated Go frames.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private void Recover(RequestMessage request, Exception panic)
    {
        var stack = ByteStringValue.FromString(GoString.ToUtf8(panic.StackTrace ?? ""));
        logger.Errorf("panic handling request %s: %v\n%s", request.Method.Value, panic, stack);
        if (request.ID is not null)
            _ = SendError(request.ID, new ProtocolErrorException(ErrorCode.InternalError, "panic handling request " + GoString.ToString(request.Method.Value.Materialize()!) + ": " + panic.Message));
        else
            logger.Error(ByteStringValue.FromString(GoString.ToUtf8("unhandled panic in notification " + GoString.ToString(request.Method.Value.Materialize()!) + " " + panic.Message)));
        if (telemetryEnabled)
            _ = SendNotification(Method.TelemetryEventInfo, new RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull { RequestFailureTelemetryEvent = new RequestFailureTelemetryEvent { Properties = new RequestFailureTelemetryProperties { ErrorCode = ErrorCode.InternalError.String(), RequestMethod = ReplaceMethodSeparators(request.Method.Value), // Original sanitizer admits Go runtime/debug.Stack frames only.
 // A CLR stack does not acquire an invented Go marker/shape.
            Stack = StackSanitizer.SanitizeStackTrace(stack) } } });
    }

    private static ByteStringValue ReplaceMethodSeparators(ByteStringValue value)
    {
        var result = new GoBuilder();
        result.Grow(value.Length);
        for (int index = 0; index < value.Length; index++)
            result.WriteByte((byte)(value[index] == '/' ? '.' : value[index]));
        return result.Value();
    }
}
