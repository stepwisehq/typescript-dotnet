// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct counterpart of core/modulekind_stringer_generated.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e; compile-time names replace substring tables.
using System.Globalization;
namespace TypeScript.Scanner;
public static class ModuleKindStringer
{
    public static string String(this ModuleKind value) => value switch
    {
        ModuleKind.None => "None", ModuleKind.CommonJS => "CommonJS",
        ModuleKind.AMD => "AMD", ModuleKind.UMD => "UMD", ModuleKind.System => "System",
        ModuleKind.ES2015 => "ES2015", ModuleKind.ES2020 => "ES2020", ModuleKind.ES2022 => "ES2022",
        ModuleKind.ESNext => "ESNext", ModuleKind.Node16 => "Node16", ModuleKind.Node18 => "Node18",
        ModuleKind.Node20 => "Node20", ModuleKind.NodeNext => "NodeNext", ModuleKind.Preserve => "Preserve",
        _ => "ModuleKind(" + ((int)value).ToString(CultureInfo.InvariantCulture) + ")"
    };
}
