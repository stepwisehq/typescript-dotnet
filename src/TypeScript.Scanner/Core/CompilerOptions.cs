// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Partial port of tsc/internal/core/compileroptions.go: ResolutionMode alias and selected checker option accessors.
namespace TypeScript.Scanner;
public enum ResolutionMode
{
    None = 0,
    CommonJS = 1,
    ESNext = 99
}

// Selected exact accessors from compileroptions.go; other option APIs excluded.
public sealed partial class CompilerOptions
{
    // Upstream compileroptions.go:23.
    public ScriptTarget GetEmitScriptTarget()
    {
        if (Target != ScriptTarget.None)
            return Target;
        return ScriptTarget.LatestStandard;
    }

    // Upstream compileroptions.go:30.
    public ModuleKind GetEmitModuleKind()
    {
        if (Module != ModuleKind.None)
            return Module;
        var target = GetEmitScriptTarget();
        if (target == ScriptTarget.ESNext)
            return ModuleKind.ESNext;
        if (target >= ScriptTarget.ES2022)
            return ModuleKind.ES2022;
        if (target >= ScriptTarget.ES2020)
            return ModuleKind.ES2020;
        if (target >= ScriptTarget.ES2015)
            return ModuleKind.ES2015;
        return ModuleKind.CommonJS;
    }

    // Upstream compileroptions.go:122.
    public bool GetStrictOptionValue(Tristate value)
    {
        if (value != Tristate.Unknown)
            return value == Tristate.True;
        return Strict != Tristate.False;
    }

    // Upstream compileroptions.go:153.
    public bool UsesWildcardTypes() => Types?.Contains("*") == true;
    // Upstream compileroptions.go:157.
    public bool GetIsolatedModules() => IsolatedModules == Tristate.True || VerbatimModuleSyntax == Tristate.True;
    // Upstream compileroptions.go:166.
    public bool GetEmitStandardClassFields() => UseDefineForClassFields != Tristate.False && GetEmitScriptTarget() >= ScriptTarget.ES2022;
}
