// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/pseudochecker/checker.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only data-property/scalar reuse; callable/tuple/emit paths excluded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.PseudoChecking;
internal sealed partial class PseudoChecker
{
    private readonly bool strictNullChecks, exactOptionalPropertyTypes;
    private PseudoChecker(bool strictNullChecks, bool exactOptionalPropertyTypes)
    {
        this.strictNullChecks = strictNullChecks;
        this.exactOptionalPropertyTypes = exactOptionalPropertyTypes;
    }

    internal static PseudoChecker NewPseudoChecker(bool strictNullChecks, bool exactOptionalPropertyTypes) => new(strictNullChecks, exactOptionalPropertyTypes);
}
