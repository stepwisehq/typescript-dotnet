// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/emitresolver.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Only symbol accessibility result models needed by diagnostic serialization.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal enum SymbolAccessibility
{
    Accessible,
    NotAccessible,
    CannotBeNamed,
    NotResolved
}

internal sealed class SymbolAccessibilityResult
{
    internal SymbolAccessibility Accessibility;
    internal List<Node> AliasesToMakeVisible;
    internal string ErrorSymbolName, ErrorModuleName;
    internal Node ErrorNode;
}
