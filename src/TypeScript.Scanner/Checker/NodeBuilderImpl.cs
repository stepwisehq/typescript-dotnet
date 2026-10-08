// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilderimpl.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    private readonly Checker ch;
    private readonly NodeFactory f;
    internal readonly TypeScript.DiagnosticPrinting.EmitContext e = new();
    private readonly Dictionary<Node, Symbol> idToSymbol = new();
    // NodeBuilderContext selected fields (nodebuilderimpl.go:61). Cache cells
    // are builder-owned; context state is stacked and replaced for nested diagnostics.
    internal sealed class NodeBuilderContext
    {
        internal Node enclosingDeclaration;
        internal SourceFile enclosingFile;
        internal int approximateLength;
        internal bool truncating, encounteredError, reportedDiagnostic, inObjectTypeLiteral, useFullyQualifiedType, suppressReportInferenceFallback;
        internal readonly HashSet<uint> visitedTypes = new();
        internal readonly Dictionary<ulong, int> symbolDepth = new();
        internal readonly Dictionary<ulong, Type> enclosingSymbolTypes = new();
        internal SymbolTrackerImpl tracker;
    }

    internal NodeBuilderContext ctx;
    internal int approximateLength { get => ctx.approximateLength; set => ctx.approximateLength = value; }
    private bool truncating { get => ctx.truncating; set => ctx.truncating = value; }
    private bool encounteredError { get => ctx.encounteredError; set => ctx.encounteredError = value; }
    private bool inObjectTypeLiteral { get => ctx.inObjectTypeLiteral; set => ctx.inObjectTypeLiteral = value; }
    private bool useFullyQualifiedType { get => ctx.useFullyQualifiedType; set => ctx.useFullyQualifiedType = value; }
    private HashSet<uint> visitedTypes => ctx.visitedTypes;
    private Dictionary<ulong, int> symbolDepth => ctx.symbolDepth;

    private readonly PseudoChecker pc;
    // Fixed diagnostic flags/internalFlags/no infer parameters are projected onto
    // the genuinely varying fully-qualified and InObjectTypeLiteral flags.
    private readonly record struct CompositeTypeCacheIdentity(uint typeId, bool fullyQualified, bool inObject);
    private sealed record SerializedTypeEntry(Node node, bool truncating, int addedLength);
    private sealed class NodeBuilderLinks
    {
        internal Dictionary<CompositeTypeCacheIdentity, SerializedTypeEntry> serializedTypes;
    }

    private readonly Dictionary<Node, NodeBuilderLinks> links = new();
    private NodeBuilderLinks nilLinks;
    private NodeBuilderLinks getLinks(Node node)
    {
        if (node == null)
            return nilLinks ??= new();
        if (!links.TryGetValue(node, out var cell))
            links[node] = cell = new();
        return cell;
    }

    internal void enterContext(Node enclosingDeclaration = null, bool fullyQualified = false)
    {
        ctx = new()
        {
            enclosingDeclaration = enclosingDeclaration,
            enclosingFile = Ast.GetSourceFileOfNode(enclosingDeclaration),
            useFullyQualifiedType = fullyQualified,
            tracker = null
        };
        ctx.tracker = SymbolTrackerImpl.NewSymbolTrackerImpl(ctx, null);
    }

    internal NodeBuilderImpl(Checker checker)
    {
        ch = checker;
        f = Ast.NewNodeFactory(new NodeFactoryHooks { OnClone = (node, original) => e.SetOriginal(node, original), OnUpdate = (node, original) => e.SetOriginal(node, original) });
        pc = PseudoChecker.NewPseudoChecker(checker.strictNullChecks, checker.exactOptionalPropertyTypes);
    }

    // typeToTypeNode (3358): intrinsic/literal branches; no alias, reduction or expansion.
    internal Node typeToTypeNode(Type t)
    {
        if (t == null)
            throw Checker.unsupported("typeToTypeNode: empty type");
        if (t.alias != null || (t.flags & (TypeFlags.Instantiable | TypeFlags.EnumLike | TypeFlags.Intersection)) != 0)
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
            e.AddEmitFlags(lit, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
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
        {
            if (t.AsUnionType().origin != null)
                t = t.AsUnionType().origin;
            if ((t.flags & TypeFlags.Union) == 0)
                return typeToTypeNode(t);
            var types = ch.formatUnionTypes(t.AsUnionType().types, false);
            if (types.Count == 1)
                return typeToTypeNode(types[0]);
            var nodes = mapToTypeNodes(types, true);
            if (nodes != null && nodes.Nodes is { Count: > 0 })
                return f.NewUnionTypeNode(nodes);
            encounteredError = true;
            return null;
        }

        if ((t.objectFlags & ObjectFlags.Anonymous) != 0)
            return createAnonymousTypeNode(t);
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
        if ((symbol.Flags & (SymbolFlags.Alias | SymbolFlags.Class | SymbolFlags.Namespace)) != 0)
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
        if (symbol.Name == Ast.InternalSymbolNameDefault)
            throw Checker.unsupported("getNameOfSymbolAsWritten: default/transformed symbol");
        if (symbol.Declarations is { Count: > 0 })
        {
            var name = Core.FirstNonNil(symbol.Declarations, Ast.GetNameOfDeclaration);
            if (name != null)
            {
                if (name.Kind is not (Kind.Identifier or Kind.StringLiteral or Kind.NumericLiteral))
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

    // nodebuilderimpl.go:147. Fixed default context, no NoTruncation/verbosity options.
    private bool checkTruncationLength()
    {
        if (truncating)
            return truncating;
        truncating = approximateLength > 160;
        return truncating;
    }

    // nodebuilderimpl.go:342/3017. IgnoreErrors is fixed by the diagnostic API.
    private Node createElidedInformationPlaceholder()
    {
        approximateLength += 3;
        return f.NewTypeReferenceNode(f.NewIdentifier("..."), null);
    }

    private Node createCyclicStructurePlaceholder()
    {
        encounteredError = true;
        ctx.tracker.ReportCyclicStructureError();
        return createElidedInformationPlaceholder();
    }

    // nodebuilderimpl.go:350. No source aliases/generic names; generated ellipsis
    // references still participate in the original name-collision pass.
    private NodeList mapToTypeNodes(List<Type> list, bool isBareList)
    {
        if (list == null || list.Count == 0)
            return null;
        if (checkTruncationLength())
        {
            if (!isBareList)
                return f.NewNodeList([f.NewTypeReferenceNode(f.NewIdentifier("..."), null)]);
            if (list.Count > 2)
            {
                List<Node> nodes = [typeToTypeNode(list[0]), null, typeToTypeNode(list[^1])];
                nodes[1] = f.NewTypeReferenceNode(f.NewIdentifier($"... {list.Count - 2} more ..."), null);
                return f.NewNodeList(nodes);
            }
        }

        var seenNames = useFullyQualifiedType ? null : new Dictionary<string, List<(Type type, int index)>>(StringComparer.Ordinal);
        var result = new List<Node>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            int displayIndex = i + 1;
            if (checkTruncationLength() && displayIndex + 2 < list.Count - 1)
            {
                result.Add(f.NewTypeReferenceNode(f.NewIdentifier($"... {list.Count - displayIndex} more ..."), null));
                var last = typeToTypeNode(list[^1]);
                if (last != null)
                    result.Add(last);
                break;
            }

            approximateLength += 2;
            var node = typeToTypeNode(list[i]);
            if (node == null)
                continue;
            result.Add(node);
            if (seenNames != null && node.Kind == Kind.TypeReference && node.AsTypeReferenceNode().TypeName.Kind == Kind.Identifier)
            {
                string name = node.AsTypeReferenceNode().TypeName.Text();
                if (!seenNames.TryGetValue(name, out var entries))
                    seenNames[name] = entries = new();
                entries.Add((list[i], result.Count - 1));
            }
        }

        if (seenNames != null)
        {
            bool saved = useFullyQualifiedType;
            useFullyQualifiedType = true;
            foreach (var entries in seenNames.Values)
                if (entries.Any(e => !ReferenceEquals(e.type, entries[0].type)))
                    foreach (var entry in entries)
                        result[entry.index] = typeToTypeNode(entry.type);
            useFullyQualifiedType = saved;
        }

        return f.NewNodeList(result);
    }

    // nodebuilderimpl.go:2469, static property-name branch.
    private enum propertyNameNodeKind
    {
        Identifier,
        NumericLiteral,
        StringLiteral
    }

    private static propertyNameNodeKind classifyPropertyName(string name, bool stringNamed, bool isMethod)
    {
        if (isMethod && name == "new")
            return propertyNameNodeKind.StringLiteral;
        if (TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name, LanguageVariant.Standard))
            return propertyNameNodeKind.Identifier;
        return !stringNamed && JsNum.FromString(name).String() == name && JsNum.FromString(name).Value >= 0 ? propertyNameNodeKind.NumericLiteral : propertyNameNodeKind.StringLiteral;
    }

    private Node createPropertyNameNodeForIdentifierOrLiteral(string name, bool singleQuote, bool stringNamed, bool isMethod, Symbol symbol)
    {
        switch (classifyPropertyName(name, stringNamed, isMethod))
        {
            case propertyNameNodeKind.Identifier:
                return newIdentifier(name, symbol);
            case propertyNameNodeKind.NumericLiteral:
                return f.NewNumericLiteral(name, TokenFlags.None);
            default:
                return f.NewStringLiteral(name, singleQuote ? TokenFlags.SingleQuote : TokenFlags.None);
        }
    }

    private bool isStringNamed(Node declaration)
    {
        var name = Ast.GetNameOfDeclaration(declaration);
        if (name == null)
            return false;
        if (name.Kind is Kind.ComputedPropertyName or Kind.ElementAccessExpression)
            throw Checker.unsupported("isStringNamed: dynamic name");
        return Ast.IsStringLiteral(name);
    }

    private bool isSingleQuotedStringNamed(Node declaration)
    {
        var name = Ast.GetNameOfDeclaration(declaration);
        return name != null && Ast.IsStringLiteral(name) && (name.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) != 0;
    }

    private Node getPropertyNameNodeForSymbol(Symbol symbol)
    {
        if (symbol.ValueDeclaration != null && symbol.ValueDeclaration.Name()?.Kind == Kind.PrivateIdentifier)
            throw Checker.unsupported("getPropertyNameNodeForSymbol: private name");
        bool stringNamed = symbol.Declarations is { Count: > 0 } && symbol.Declarations.All(isStringNamed);
        bool singleQuote = symbol.Declarations is { Count: > 0 } && symbol.Declarations.All(isSingleQuotedStringNamed);
        if (ch.valueSymbolLinks.TryGet(symbol)?.nameType != null)
            throw Checker.unsupported("getPropertyNameNodeForSymbol: computed name type");
        if (symbol.Name.StartsWith(Ast.InternalSymbolNamePrefix + "#", StringComparison.Ordinal))
            throw Checker.unsupported("getPropertyNameNodeForSymbol: generated private name");
        return createPropertyNameNodeForIdentifierOrLiteral(symbol.Name, singleQuote, stringNamed, (symbol.Flags & SymbolFlags.Method) != 0, symbol);
    }

    // nodebuilderimpl.go:2253. Selected data-property declaration serialization
    // retains nonnil diagnostic context and actual pseudochecker reuse.
    // Reverse mapped implicit-undefined behavior and accessors remain excluded.
    private Node serializeTypeForDeclaration(Node declaration, Type t, Symbol symbol, bool tryReuse)
    {
        if (declaration == null && symbol != null)
            declaration = symbol.ValueDeclaration ?? symbol.Declarations?.FirstOrDefault();
        if (symbol == null)
            symbol = ch.getSymbolOfDeclaration(declaration);
        if (t == null)
        {
            if (symbol == null)
                throw Checker.unsupported("serializeTypeForDeclaration: unknown property declaration");
            if (!ctx.enclosingSymbolTypes.TryGetValue(Ast.GetSymbolId(symbol), out t) || t == null)
            {
                if ((symbol.Flags & SymbolFlags.Accessor) != 0 && declaration.Kind == Kind.SetAccessor)
                    throw Checker.unsupported("serializeTypeForDeclaration: accessor write type/mapper");
                if ((symbol.Flags & (SymbolFlags.TypeLiteral | SymbolFlags.Signature)) == 0)
                    t = ch.getWidenedLiteralType(ch.getTypeOfSymbol(symbol));
                else
                    t = ch.errorType;
            }
        }

        if ((symbol.CheckFlags & CheckFlags.ReverseMapped) != 0 || (symbol.Flags & SymbolFlags.Accessor) != 0)
            throw Checker.unsupported("serializeTypeForDeclaration: mapped/accessor");
        bool requiresAddingUndefined = declaration != null && (declaration.Kind is Kind.Parameter or Kind.PropertySignature or Kind.PropertyDeclaration) && ch.requiresAddingImplicitUndefined(declaration, symbol, ctx.enclosingDeclaration);
        if (requiresAddingUndefined)
            throw Checker.unsupported("serializeTypeForDeclaration: implicit mapped/parameter undefined");
        Node result = null;
        bool reportedInferenceFallback = false;
        if (tryReuse && ctx.enclosingDeclaration != null && declaration != null && Ast.HasInferredType(declaration) && !Ast.NodeIsSynthesized(declaration) && (t.objectFlags & ObjectFlags.RequiresWidening) == 0)
        {
            var remove = addSymbolTypeToContext(symbol, t);
            var pt = pc.GetTypeOfDeclaration(declaration);
            bool reportErrors = !ctx.suppressReportInferenceFallback;
            bool optionalAnnotated = (declaration.Kind is Kind.Parameter or Kind.PropertySignature or Kind.PropertyDeclaration) && Checker.isOptionalDeclaration(declaration);
            if (pseudoTypeEquivalentToType(pt, t, optionalAnnotated, reportErrors))
                result = pseudoTypeToNodeWithCheckerFallback(pt, t);
            else
            {
                reportedInferenceFallback = reportErrors && pt.Kind == PseudoTypeKind.Inferred && (pt.AsPseudoTypeInferred().ErrorNodes?.Count ?? 0) > 0;
            // requiresAddingUndefined is false after the actual selected predicate.
            }

            remove();
        }

        if (result == null)
        {
            bool saved = ctx.suppressReportInferenceFallback;
            if (reportedInferenceFallback)
                ctx.suppressReportInferenceFallback = true;
            result = typeToTypeNode(t);
            ctx.suppressReportInferenceFallback = saved;
        }

        return result ?? f.NewKeywordTypeNode(Kind.AnyKeyword);
    }

    // nodebuilderimpl.go:2581. Finite data-property branch. All excluded cases
    // are guarded before assuming their declaration/context caches are absent.
    private List<Node> addPropertyToElementList(Symbol propertySymbol, List<Node> typeElements)
    {
        if ((propertySymbol.CheckFlags & CheckFlags.ReverseMapped) != 0 || (propertySymbol.Flags & (SymbolFlags.Accessor | SymbolFlags.Function | SymbolFlags.Method)) != 0 || propertySymbol.Name.StartsWith(Ast.InternalSymbolNameComputed, StringComparison.Ordinal))
            throw Checker.unsupported("addPropertyToElementList: nondata/dynamic property");
        var propertyType = ch.getNonMissingTypeOfSymbol(propertySymbol);
        var propertyName = getPropertyNameNodeForSymbol(propertySymbol);
        approximateLength += Ast.SymbolName(propertySymbol).Length + 1;
        var optionalToken = (propertySymbol.Flags & SymbolFlags.Optional) != 0 ? f.NewToken(Kind.QuestionToken) : null;
        var typeNode = propertyType != null ? serializeTypeForDeclaration(null, propertyType, propertySymbol, true) : f.NewKeywordTypeNode(Kind.AnyKeyword);
        ModifierList modifiers = null;
        if (ch.isReadonlySymbol(propertySymbol))
        {
            modifiers = f.NewModifierList([f.NewModifier(Kind.ReadonlyKeyword)]);
            approximateLength += 9;
        }

        typeElements ??= new();
        typeElements.Add(f.NewPropertySignatureDeclaration(modifiers, propertyName, optionalToken, typeNode, null));
        // SetCommentRange is omitted: diagnostic printer removes comments.
        return typeElements;
    }

    // nodebuilderimpl.go:2722. Original truncation/order loop; signatures/indexes
    // remain genuinely absent and nonempty values are explicit unsupported paths.
    private NodeList createTypeNodesFromResolvedType(StructuredType resolved)
    {
        if (checkTruncationLength())
            return f.NewNodeList([f.NewPropertySignatureDeclaration(null, f.NewIdentifier("..."), null, null, null)]);
        if ((resolved.signatures?.Count ?? 0) != 0 || (resolved.indexInfos?.Count ?? 0) != 0)
            throw Checker.unsupported("createTypeNodesFromResolvedType: signatures/indexes");
        List<Node> elements = null;
        var properties = resolved.properties;
        if (properties == null || properties.Count == 0)
            return f.NewNodeList(elements);
        int i = 0;
        foreach (var property in properties)
        {
            i++;
            if (checkTruncationLength() && i + 2 < properties.Count - 1)
            {
                elements ??= new();
                elements.Add(f.NewPropertySignatureDeclaration(null, f.NewIdentifier($"... {properties.Count - i} more ..."), null, null, null));
                elements = addPropertyToElementList(properties[^1], elements);
                break;
            }

            elements = addPropertyToElementList(property, elements);
        }

        return elements is { Count: > 0 } ? f.NewNodeList(elements) : null;
    }

    // nodebuilderimpl.go:2789. Anonymous records with no signature/index members.
    private Node createTypeNodeFromObjectType(Type t)
    {
        var resolved = ch.resolveStructuredTypeMembers(t);
        if ((resolved.signatures?.Count ?? 0) != 0 || (resolved.indexInfos?.Count ?? 0) != 0)
            throw Checker.unsupported("createTypeNodeFromObjectType: signatures/indexes");
        if (resolved.properties == null || resolved.properties.Count == 0)
        {
            approximateLength += 2;
            return f.NewTypeLiteralNode(f.NewNodeList([]));
        }

        bool saved = inObjectTypeLiteral;
        inObjectTypeLiteral = true;
        var members = createTypeNodesFromResolvedType(resolved);
        inObjectTypeLiteral = saved;
        approximateLength += 2;
        return f.NewTypeLiteralNode(members);
    // EFSingleLine is a fixed diagnostic printer option, not general emit state.
    }

    // nodebuilderimpl.go:2897/2912. Anonymous static records only. Retain actual
    // cycle/symbol-depth guards; named, instantiated or mapped objects reject.
    private Node createAnonymousTypeNode(Type t) => createAnonymousTypeNodeEx(t, false, false);
    private Node createAnonymousTypeNodeEx(Type t, bool forceClassExpansion, bool forceExpansion)
    {
        if ((t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Reference | ObjectFlags.Mapped | ObjectFlags.ReverseMapped | ObjectFlags.Instantiated | ObjectFlags.InstantiationExpressionType)) != 0 || t.AsObjectType().target != null)
            throw Checker.unsupported("createAnonymousTypeNodeEx: named/instantiated/mapped object");
        if (t.symbol != null && (t.symbol.Flags & (SymbolFlags.Class | SymbolFlags.Function | SymbolFlags.Method | SymbolFlags.Enum | SymbolFlags.Module)) != 0)
            throw Checker.unsupported("createAnonymousTypeNodeEx: named object symbol");
        if (visitedTypes.Contains(t.id))
            return createCyclicStructurePlaceholder();
        return visitAndTransformType(t, createTypeNodeFromObjectType);
    }

    // nodebuilderimpl.go:3234. Selected diagnostic contexts retain original cache
    // reads/writes, visited types and symbol recursion depth. Generic/array origins,
    // mappers, external trackers and verbosity are excluded.
    private Node visitAndTransformType(Type t, Func<Type, Node> transform)
    {
        if (checkTruncationLength())
            return createElidedInformationPlaceholder();
        uint typeId = t.id;
        if (visitedTypes.Contains(typeId))
            return createCyclicStructurePlaceholder();
        var key = new CompositeTypeCacheIdentity(typeId, useFullyQualifiedType, inObjectTypeLiteral);
        if (ctx.enclosingDeclaration != null && links.TryGetValue(ctx.enclosingDeclaration, out var existingLinks) && existingLinks.serializedTypes?.TryGetValue(key, out var cachedResult) == true)
        {
            if (cachedResult.truncating)
                truncating = true;
            approximateLength += cachedResult.addedLength;
            return f.DeepCloneNode(cachedResult.node);
        }

        ulong? symbolId = t.symbol != null ? Ast.GetSymbolId(t.symbol) : null;
        int depth = 0;
        if (symbolId is { } id)
        {
            symbolDepth.TryGetValue(id, out depth);
            if (depth > 10)
            {
                truncating = true;
                return createElidedInformationPlaceholder();
            }

            symbolDepth[id] = depth + 1;
        }

        visitedTypes.Add(typeId);
        int startLength = approximateLength;
        var result = transform(t);
        int addedLength = approximateLength - startLength;
        if (!ctx.reportedDiagnostic && !encounteredError)
        {
            var cell = getLinks(ctx.enclosingDeclaration);
            (cell.serializedTypes ??= new())[key] = new(result, truncating, addedLength);
        }

        visitedTypes.Remove(typeId);
        if (symbolId is { } restoreId)
            symbolDepth[restoreId] = depth;
        return result;
    }

    // nodebuilderimpl.go:508/526. The selected source is TS and excludes named
    // references; the JSDoc-intended-type branch is unreachable by admission.
    private bool canReuseExistingJSTypeNode(Node existing, Type t)
    {
        if (Ast.IsInJSFile(existing))
            throw Checker.unsupported("canReuseExistingJSTypeNode: JSDoc type reference");
        return existingTypeNodeIsNotReferenceOrIsReferenceWithCompatibleTypeArgumentCount(existing, t);
    }

    private bool existingTypeNodeIsNotReferenceOrIsReferenceWithCompatibleTypeArgumentCount(Node existing, Type t)
    {
        if ((t.objectFlags & ObjectFlags.Reference) == 0)
            return true;
        if (existing.Kind != Kind.TypeReference)
            return true;
        throw Checker.unsupported("existingTypeNodeIsNotReferenceOrIsReferenceWithCompatibleTypeArgumentCount: named reference");
    }

    private Node mostOriginal(Node node) => e.MostOriginal(node);
    // nodebuilderimpl.go:1435. Emit-context original links are represented by
    // builder-owned identity maps; no public/general emit state is introduced.
    private Node setTextRange(Node range, Node location)
    {
        if (range == null)
            return range;
        if (!Ast.NodeIsSynthesized(range) || (range.Flags & NodeFlags.Synthesized) == 0 || ctx.enclosingFile == null || ctx.enclosingFile != Ast.GetSourceFileOfNode(mostOriginal(range)))
        {
            var original = range;
            range = range.Clone(f);
            range.Loc = new(-1, -1);
            if (idToSymbol.TryGetValue(original, out var symbol))
                idToSymbol[range] = symbol;
        }

        if (range == location || location == null)
            return range;
        var prior = e.Original(range);
        while (prior != null && prior != location)
            prior = e.Original(prior);
        if (prior == null)
            e.SetOriginalEx(range, location, true);
        range.Loc = ctx.enclosingFile != null && ctx.enclosingFile == Ast.GetSourceFileOfNode(mostOriginal(location)) ? location.Loc : new(-1, -1);
        return range;
    }
}
