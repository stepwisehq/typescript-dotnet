// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/module/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace TypeScript.Modules;
internal static class ModuleUtilities
{
    // util.go:132. These original TS forms are always allowed. Selected disk
    // admission permits only .ts implementation winners; other file families
    // cannot silently enter a successful fixed-profile checker call.
    internal static Message? GetResolutionDiagnostic(CompilerOptions options, ResolvedModule resolvedModule, SourceFile file)
    {
        if (resolvedModule.ResolvedUsingExtraExtensions)
            throw new ProjectException("extraExtensions", "Custom resolution extensions are unsupported.", resolvedModule.ResolvedFileName);
        if (resolvedModule.Extension is ".ts" or ".d.ts" or ".mts" or ".d.mts" or ".cts" or ".d.cts")
            return null;
        throw new ProjectException("resolvedFileForm", "Resolution diagnostics for this file form are unsupported: " + resolvedModule.Extension, resolvedModule.ResolvedFileName);
    }
}
