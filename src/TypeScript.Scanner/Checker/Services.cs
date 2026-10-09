// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/services.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Selected module export-list service;
// other language-service methods remain outside this fixed checker surface.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // services.go:131. Exact exported-symbol list projection; reserved keys excluded.
    public List<Symbol> GetExportsOfModule(Symbol symbol)
    {
        validateOwnedProjectSymbol(symbol);
        return symbolsToArray(getExportsOfModule(symbol));
    }
}
