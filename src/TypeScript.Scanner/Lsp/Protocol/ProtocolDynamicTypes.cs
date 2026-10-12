// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed fmt %T adaptation for lsproto's dynamic message-boundary errors.
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal interface IProtocolTypeNamed
{
    string GoType { get; }
}

internal static partial class ProtocolDynamicTypes
{
    internal static string Name(object? value) => value switch
    {
        null => "<nil>",
        IProtocolTypeNamed named => named.GoType,
        RawJsonValue => "jsontext.Value",
        bool => "bool",
        string or ByteStringValue => "string",
        int => "int32",
        uint => "uint32",
        double => "float64",
        Dictionary<string, object?> => "map[string]interface {}",
        List<object?> => "[]interface {}",
        _ => IntrinsicName(value),
    };
}
