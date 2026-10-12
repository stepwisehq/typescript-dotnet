// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused owner projection for server.go copied *jsonrpc.ID map keys. Draft only.
using TypeScript.Scanner;

namespace TypeScript.JsonRpc;
internal sealed partial class ID
{
    // Both fields participate in Go struct map equality. Normal UnmarshalJSON
    // resets both fields before decoding; snapshot the immutable string header.
    internal (ByteStringValue Text, int Number) MapKeyParts() => (str.Snapshot(), integer);
}
