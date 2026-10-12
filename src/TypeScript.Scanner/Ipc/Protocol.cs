// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ipc/protocol.go interface. GenericMessage retains owned deferred fields.
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.Ipc;
internal interface IProtocol
{
    (GenericMessage? Message, Exception? Error) ReadMessage();
    Exception? WriteRequest(ID? id, ByteStringValue method, object? parameters);
    Exception? WriteNotification(ByteStringValue method, object? parameters);
    Exception? WriteResponse(ID? id, object? result);
    Exception? WriteError(ID? id, ResponseError? error);
}
