// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/core/core.go:520 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// The complete raw JsonCodec.MarshalIndentValue worker preserves output and error.
namespace TypeScript.Scanner;
internal static partial class Core
{
    internal static (ByteStringValue Output, Exception? Error) StringifyJson(object? input, ByteStringValue prefix, ByteStringValue indent) => TypeScript.Json.JsonCodec.MarshalIndentValue(input, prefix, indent);
}
