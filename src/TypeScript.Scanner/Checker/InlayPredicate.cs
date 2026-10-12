// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete checker/printer.go:353 wrapper, actual InlayHint identifier-map consumer.
using TypeScript.Scanner;
using Flags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal Node TypePredicateToTypePredicateNode(TypePredicate predicate, Node enclosingDeclaration, Flags flags, Dictionary<Node, Symbol>? idToSymbol)
    {
        var builder = getNodeBuilderEx(idToSymbol);
        return builder.TypePredicateToTypePredicateNode(predicate, enclosingDeclaration, flags, InternalFlags.None, null);
    }
}
