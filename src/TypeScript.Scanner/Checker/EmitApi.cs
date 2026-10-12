// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Required emit API leaves from pinned exports.go:219 and services.go:869.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal ModifierFlags GetEffectiveDeclarationFlags(Node node, ModifierFlags flags) => getEffectiveDeclarationFlags(node, flags);
}
