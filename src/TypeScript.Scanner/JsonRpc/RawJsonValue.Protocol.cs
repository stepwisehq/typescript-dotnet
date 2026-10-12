// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// jsontext.Value nil slice identity is retained across a Go any boundary.
namespace TypeScript.JsonRpc;
internal sealed partial class RawJsonValue
{
    internal readonly bool IsNil;
    internal static readonly RawJsonValue Nil = new(nil: true);
    private RawJsonValue(bool nil)
    {
        bytes = Array.Empty<byte>();
        IsNil = nil;
    }
}
