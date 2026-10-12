// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct lsp.go raw-object helpers; source encounter order is retained.
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolCodec
{
    // lsp.go:248. Object encounter order determines the first matching key.
    private static int ProtocolObjectHasKey(ProtocolRawValue raw, ReadOnlySpan<ByteStringValue> keys)
    {
        var decoder = new ProtocolDecoder(raw.TextValue);
        try
        {
            if (decoder.PeekKind() != '{')
                return -1;
            decoder.ReadToken((byte)'{');
            while (decoder.PeekKind() != '}')
            {
                var name = decoder.ReadPropertyName();
                for (int i = 0; i < keys.Length; i++)
                    if (name.EqualsValue(keys[i]))
                        return i;
                decoder.SkipValue();
            }
        }
        catch (Exception error)when (error is JsonException or FormatException)
        {
            return -1;
        }

        return -1;
    }

    // lsp.go:218. The raw value remains borrowed from the caller's decoder owner.
    private static ProtocolRawValue? ProtocolObjectRawField(ProtocolRawValue raw, ByteStringValue field)
    {
        var decoder = new ProtocolDecoder(raw.TextValue);
        try
        {
            if (decoder.PeekKind() != '{')
                return null;
            decoder.ReadToken((byte)'{');
            while (decoder.PeekKind() != '}')
            {
                var name = decoder.ReadPropertyName();
                if (name.EqualsValue(field))
                    return decoder.ReadValue();
                decoder.SkipValue();
            }
        }
        catch (Exception error)when (error is JsonException or FormatException)
        {
            return null;
        }

        return null;
    }
}
