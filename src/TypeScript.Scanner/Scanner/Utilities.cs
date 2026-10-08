// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/scanner/utilities.go, upstream fed0bf24149f.
// Only the scanner-used token predicate is included; compiler-facing helpers are excluded.
namespace TypeScript.Scanner;

public partial class Scanner
{
    private static bool tokenIsIdentifierOrKeyword(Kind token) => token >= Kind.Identifier;
}
