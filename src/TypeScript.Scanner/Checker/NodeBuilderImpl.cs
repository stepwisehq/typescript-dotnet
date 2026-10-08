// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilderimpl.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal sealed class NodeBuilderImpl
{
    private readonly Checker ch;
    private readonly NodeFactory f = Ast.NewNodeFactory(new NodeFactoryHooks());
    private readonly Dictionary<Node, Symbol> idToSymbol = new();
    internal int approximateLength;
    internal NodeBuilderImpl(Checker checker) => ch = checker;
    // typeToTypeNode (3358): intrinsic/literal branches; no alias, reduction or expansion.
    internal Node typeToTypeNode(Type t)
    {
        if (t == null)
            throw Checker.unsupported("typeToTypeNode: empty type");
        if (t.alias != null || (t.flags & (TypeFlags.Object | TypeFlags.Instantiable | TypeFlags.EnumLike | TypeFlags.Intersection)) != 0)
            throw Checker.unsupported("typeToTypeNode: named/structured type");
        if ((t.flags & TypeFlags.Any) != 0)
        {
            if (t == ch.unresolvedType)
                throw Checker.unsupported("typeToTypeNode: unresolved synthetic comment");
            approximateLength += 3;
            return f.NewKeywordTypeNode(t == ch.intrinsicMarkerType ? Kind.IntrinsicKeyword : Kind.AnyKeyword);
        }

        if ((t.flags & TypeFlags.Unknown) != 0)
            return f.NewKeywordTypeNode(Kind.UnknownKeyword);
        if ((t.flags & TypeFlags.String) != 0)
        {
            approximateLength += 6;
            return f.NewKeywordTypeNode(Kind.StringKeyword);
        }

        if ((t.flags & TypeFlags.Number) != 0)
        {
            approximateLength += 6;
            return f.NewKeywordTypeNode(Kind.NumberKeyword);
        }

        if ((t.flags & TypeFlags.BigInt) != 0)
        {
            approximateLength += 6;
            return f.NewKeywordTypeNode(Kind.BigIntKeyword);
        }

        if ((t.flags & TypeFlags.Boolean) != 0)
        {
            approximateLength += 7;
            return f.NewKeywordTypeNode(Kind.BooleanKeyword);
        }

        if ((t.flags & TypeFlags.StringLiteral) != 0)
        {
            var value = (string)t.AsLiteralType().value;
            approximateLength += value.Length + 2;
            var lit = newStringLiteral(value);
            // Upstream EFNoAsciiEscaping retained as diagnostic-printer input metadata.
            return f.NewLiteralTypeNode(lit);
        }

        if ((t.flags & TypeFlags.NumberLiteral) != 0)
        {
            var value = (JsNum.Number)t.AsLiteralType().value;
            var text = value.String();
            approximateLength += text.Length;
            if (value.Value < 0)
                return f.NewLiteralTypeNode(f.NewPrefixUnaryExpression(Kind.MinusToken, f.NewNumericLiteral(text[1..], TokenFlags.None)));
            return f.NewLiteralTypeNode(f.NewNumericLiteral(text, TokenFlags.None));
        }

        if ((t.flags & TypeFlags.BooleanLiteral) != 0)
        {
            bool value = (bool)t.AsLiteralType().value;
            approximateLength += value ? 4 : 5;
            return f.NewLiteralTypeNode(f.NewKeywordExpression(value ? Kind.TrueKeyword : Kind.FalseKeyword));
        }

        if ((t.flags & TypeFlags.ESSymbol) != 0)
        {
            approximateLength += 6;
            return f.NewKeywordTypeNode(Kind.SymbolKeyword);
        }

        if ((t.flags & TypeFlags.Void) != 0)
        {
            approximateLength += 4;
            return f.NewKeywordTypeNode(Kind.VoidKeyword);
        }

        if ((t.flags & TypeFlags.Undefined) != 0)
        {
            approximateLength += 9;
            return f.NewKeywordTypeNode(Kind.UndefinedKeyword);
        }

        if ((t.flags & TypeFlags.Null) != 0)
        {
            approximateLength += 4;
            return f.NewLiteralTypeNode(f.NewKeywordExpression(Kind.NullKeyword));
        }

        if ((t.flags & TypeFlags.Never) != 0)
        {
            approximateLength += 5;
            return f.NewKeywordTypeNode(Kind.NeverKeyword);
        }

        if ((t.flags & TypeFlags.Union) != 0)
            throw Checker.unsupported("typeToTypeNode: nonboolean union serialization");
        throw Checker.unsupported("typeToTypeNode: type payload");
    }

    // newStringLiteral/newStringLiteralEx (3681); default double quote context.
    private Node newStringLiteral(string text) => newStringLiteralEx(text, false);
    private Node newStringLiteralEx(string text, bool singleQuote) => f.NewStringLiteral(text, singleQuote ? TokenFlags.SingleQuote : TokenFlags.None);
    // nodebuilderimpl.go:585/849/854. Selected flags disable computed-property
    // writing; enclosing declaration/tracker are nil in diagnostic context.
    internal Node symbolToNode(Symbol symbol, SymbolFlags meaning) => symbolToExpression(symbol, meaning);
    private Node symbolToExpression(Symbol symbol, SymbolFlags mask) => symbolToExpressionWorker(symbol, mask);
    private Node symbolToExpressionWorker(Symbol symbol, SymbolFlags mask)
    {
        var chain = lookupSymbolChainWorker(symbol, mask, false);
        return createExpressionFromSymbolChain(chain, chain.Count - 1);
    }

    // nodebuilderimpl.go:1067. With no enclosing declaration and no fully
    // qualified flag, upstream appends only the supplied symbol to the chain.
    private List<Symbol> lookupSymbolChainWorker(Symbol symbol, SymbolFlags meaning, bool yieldModuleSymbol)
    {
        if ((symbol.Flags & (SymbolFlags.Alias | SymbolFlags.Class | SymbolFlags.Namespace)) != 0 || symbol.Parent != null)
            throw Checker.unsupported("lookupSymbolChainWorker: qualified/named symbol");
        return[symbol];
    }

    // nodebuilderimpl.go:859. Single-part scalar declaration/synthetic builtin arm.
    private Node createExpressionFromSymbolChain(List<Symbol> chain, int index)
    {
        var typeParameterNodes = lookupExpressionChainTypeArgumentNodes(chain, index);
        var symbol = chain[index];
        if (index != 0)
            throw Checker.unsupported("createExpressionFromSymbolChain: property access");
        var symbolName = getNameOfSymbolAsWritten(symbol);
        if (symbolName.StartsWith('"') || symbolName.StartsWith('\''))
            throw Checker.unsupported("createExpressionFromSymbolChain: external module name");
        var identifier = newIdentifier(symbolName, symbol);
        // EFNoAsciiEscaping is fixed for these synthesized diagnostic identifiers.
        approximateLength += 1 + symbolName.Length;
        return createExpressionWithTypeArguments(identifier, typeParameterNodes);
    }

    // nodebuilderimpl.go:974. Only ordinary variable declarations and synthesized
    // builtin symbols are selected; remapping/default/computed names are excluded.
    private string getNameOfSymbolAsWritten(Symbol symbol)
    {
        if (symbol.Name == Ast.InternalSymbolNameDefault || symbol.CheckFlags != CheckFlags.None)
            throw Checker.unsupported("getNameOfSymbolAsWritten: default/transformed symbol");
        if (symbol.Declarations is { Count: > 0 })
        {
            var name = Core.FirstNonNil(symbol.Declarations, Ast.GetNameOfDeclaration);
            if (name != null)
            {
                if (name.Kind != Kind.Identifier)
                    throw Checker.unsupported("getNameOfSymbolAsWritten: computed/general name");
                return TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name);
            }

            throw Checker.unsupported("getNameOfSymbolAsWritten: nameless declaration");
        }

        // nameType is never assigned in the selected ordinary-variable closure.
        return Ast.EscapeInternalSymbolName(symbol.Name);
    }

    // nodebuilderimpl.go:3700.
    private Node newIdentifier(string text, Symbol symbol)
    {
        var id = f.NewIdentifier(text);
        if (symbol != null)
            idToSymbol[id] = symbol;
        return id;
    }

    // nodebuilderimpl.go:3719/3753. No chain suffix exists in the selected arm.
    private Node createExpressionWithTypeArguments(Node expression, NodeList typeArguments)
    {
        if (typeArguments == null || GoRuntime.Len(typeArguments.Nodes) == 0)
            return expression;
        throw Checker.unsupported("createExpressionWithTypeArguments: type arguments");
    }

    private NodeList lookupExpressionChainTypeArgumentNodes(List<Symbol> chain, int index)
    {
        if (index < chain.Count - 1)
            throw Checker.unsupported("lookupExpressionChainTypeArgumentNodes: qualified chain");
        return null;
    }
}
