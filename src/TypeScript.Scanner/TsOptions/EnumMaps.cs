// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/enummaps.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    public static (string Value, bool Ok) GetLibFileName(string libName)
    {
        libName = CaseSensitivity.CaseInsensitive.Canonicalize(libName);
        if (LibFilesSet.Contains(libName))
            return (libName, true);
        var(value, ok) = LibMap.Get(libName);
        return ok ? ((string)value!, true) : ("", false);
    }

    public static Dictionary<ScriptTarget, string> TargetToLibMap() => targetToLibMap;
    public static string GetDefaultLibFileName(CompilerOptions options) => targetToLibMap.TryGetValue(options.GetEmitScriptTarget(), out var name) ? name : "lib.d.ts";
}
