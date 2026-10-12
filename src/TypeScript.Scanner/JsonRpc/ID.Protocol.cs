// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct ID.UnmarshalJSON state owner; partial declaration joins existing ID storage.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.JsonRpc;
internal sealed partial class ID
{
    internal void UnmarshalProtocolJSON(ByteStringValue data)
    {
        str = default;
        integer = 0;
        var decoder = new ProtocolDecoder(data);
        if (data.Length != 0 && data.ByteSpan[0] == '"')
            str = decoder.ReadString();
        else
            integer = decoder.ReadInt32();
        decoder.Finish();
    }
}
