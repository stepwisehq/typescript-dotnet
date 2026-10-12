// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused real shared workers from internal/ls/completions.go; full completion feature remains unfinished.
// Pin fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    // completions.go:3556.
    private static TypeScript.Checking.Type? getSwitchedType(Node caseClause, Checker checker) => checker.GetTypeAtLocation(caseClause.Parent.Parent.Expression());
    // completions.go:3560.
    private static bool isEqualityOperatorKind(Kind kind) => kind is Kind.EqualsEqualsEqualsToken or Kind.EqualsEqualsToken or Kind.ExclamationEqualsEqualsToken or Kind.ExclamationEqualsToken;
}
