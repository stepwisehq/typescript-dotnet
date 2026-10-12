// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused byte-string instantiation of internal/core/core.go:828 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class Core
{
    // ByteStringValue is a header; Go comparable string semantics compare its bytes.
    // Keep the original nil/empty/unchanged header and clone only at the first duplicate.
    internal static Slice<ByteStringValue> Deduplicate(Slice<ByteStringValue> values)
    {
        bool contains(Slice<ByteStringValue> slice, ByteStringValue value)
        {
            foreach (var candidate in slice)
                if (candidate.EqualsValue(value))
                    return true;
            return false;
        }

        if (values.Length > 1)
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (!contains(values.Slice2(0, i), value))
                    continue;
                var result = GoSlices.Clone(values.Slice2(0, i));
                for (i++; i < values.Length; i++)
                {
                    value = values[i];
                    if (!contains(result, value))
                        result = result.Append(value);
                }

                return result;
            }

        return values;
    }
}
