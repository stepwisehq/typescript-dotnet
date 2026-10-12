// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Raw owner/consumer joins over unchanged checker exports.go workers.
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal ByteStringValue GetJsxNamespaceValue(Node location) => getJsxNamespace(location);
    internal Symbol? ResolveName(ByteStringValue name, Node location, SymbolFlags meaning, bool excludeGlobals) => resolveName(location, name, meaning, null, true, excludeGlobals);
    // checker/printer.go:348/353, genuine identifier-map builder ownership.
    internal Node? TypeToTypeNodeEx(Type type, Node declaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, Dictionary<Node, Symbol>? idToSymbol) => getNodeBuilderEx(idToSymbol).TypeToTypeNode(type, declaration, flags, internalFlags, null);
    // checker/exports.go:386. Nil symbol admission and enclosing declaration are original.
    internal bool RequiresAddingImplicitUndefined(Node node)
    {
        var enclosing = Ast.FindAncestor(node, Ast.IsDeclaration) ?? Ast.GetSourceFileOfNode(node);
        var symbol = node.Symbol();
        return symbol is not null && requiresAddingImplicitUndefined(node, symbol, enclosing);
    }
}
