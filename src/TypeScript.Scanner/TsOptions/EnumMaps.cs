// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/enummaps.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    public static (string Value, bool Ok) GetLibFileName(string libName)
    {
        var (value, ok) = GetLibFileName(ByteStringValue.FromString(libName));
        return (value.Materialize()!, ok);
    }
    internal static (ByteStringValue Value, bool Ok) GetLibFileName(ByteStringValue libName)
    {
        libName = CaseSensitivity.CaseInsensitive.Canonicalize(libName);
        if (libName.ContainsIn(LibFilesSet))
            return (libName, true);
        var (value, ok) = LibMap.Get(libName);
        return ok ? (ByteStringValue.FromString((string)value!), true) : (ByteStringValue.FromString(""), false);
    }

    public static Dictionary<ScriptTarget, string> TargetToLibMap() => targetToLibMap;
    public static string GetDefaultLibFileName(CompilerOptions options) => targetToLibMap.TryGetValue(options.GetEmitScriptTarget(), out var name) ? name : "lib.d.ts";
}
