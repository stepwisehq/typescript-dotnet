// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/modulespecifiers/compare.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ModuleSpecifiers;
internal static partial class ModuleSpecifiers
{
    internal static int CountPathComponents(string path) => CountPathComponents(TypeScript.Scanner.ByteStringValue.FromString(path));
    internal static int CountPathComponents(TypeScript.Scanner.ByteStringValue path)
    {
        int initial = path.StartsWith("./"u8) ? 2 : 0;
        int count = 0;
        for (int i = initial; i < path.Length; i++)
            if (path[i] == '/')
                count++;
        return count;
    }
}
