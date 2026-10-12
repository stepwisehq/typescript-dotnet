// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Required emit API leaves from pinned exports.go:219 and services.go:869.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal ModifierFlags GetEffectiveDeclarationFlags(Node node, ModifierFlags flags) => getEffectiveDeclarationFlags(node, flags);
    internal object GetConstantValue(Node node)
    {
        if (node.Kind == Kind.EnumMember) return getEnumMemberValue(node).Value;
        if (symbolNodeLinks.Get(node).resolvedSymbol == null) checkExpressionCached(node);
        var symbol = symbolNodeLinks.Get(node).resolvedSymbol;
        if (symbol == null && Ast.IsEntityNameExpression(node)) symbol = resolveEntityName(node, SymbolFlags.Value, true, false, null);
        if (symbol != null && (symbol.Flags & SymbolFlags.EnumMember) != 0)
        {
            var member = symbol.ValueDeclaration;
            if (Ast.IsEnumConst(member.Parent)) return getEnumMemberValue(member).Value;
        }
        return null;
    }
}
