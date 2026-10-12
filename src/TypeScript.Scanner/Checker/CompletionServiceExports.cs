// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Borrowed counterparts of existing checker exports.go and printer.go service boundaries.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal Type GetTypeOfPropertyOfContextualType(Type type, ByteStringValue name) => getTypeOfPropertyOfContextualType(type, name);
    internal Type GetTypeOfPropertyOfType(Type type, ByteStringValue name) => getTypeOfPropertyOfType(type, name);
    internal ByteStringValue SymbolToStringValue(Symbol symbol) => SymbolToStringExValue(symbol, null, SymbolFlags.All, SymbolFormatFlags.AllowAnyNodeKind);
    internal bool IsValidPropertyAccess(Node node, ByteStringValue name) => isValidPropertyAccess(node, name);
}
