// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// ast/utilities.go:2189-2205. Raw-byte counterpart of the retained public string worker.
namespace TypeScript.Scanner;
public static partial class Ast
{
    internal static ByteStringValue EntityNameToStringValue(Node name, Func<Node, ByteStringValue>? getTextOfNode)
    {
        switch (name.Kind)
        {
            case Kind.ThisKeyword:
                return ByteStringValue.FromString("this");
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
                return NodeIsSynthesized(name) || getTextOfNode is null ? name.TextValue() : getTextOfNode(name);
            case Kind.QualifiedName:
                return ByteStringValue.Concat(EntityNameToStringValue(name.AsQualifiedName().Left, getTextOfNode), ByteStringValue.FromString("."), EntityNameToStringValue(name.AsQualifiedName().Right, getTextOfNode));
            case Kind.PropertyAccessExpression:
                return ByteStringValue.Concat(EntityNameToStringValue(name.Expression(), getTextOfNode), ByteStringValue.FromString("."), EntityNameToStringValue(name.Name(), getTextOfNode));
            case Kind.JsxNamespacedName:
                return ByteStringValue.Concat(EntityNameToStringValue(name.AsJsxNamespacedName().Namespace, getTextOfNode), ByteStringValue.FromString(":"), EntityNameToStringValue(name.Name(), getTextOfNode));
            default:
                throw new InvalidOperationException("Unhandled case in EntityNameToString");
        }
    }
}
