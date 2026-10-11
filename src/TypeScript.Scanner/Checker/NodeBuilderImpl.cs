// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilderimpl.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using TypeScript.PseudoChecking;
using TypeScript.Modules;
using TypeScript.ModuleSpecifiers;
using ModuleSpecifierFunctions = TypeScript.ModuleSpecifiers.ModuleSpecifiers;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    internal const int defaultMaximumTruncationLength = 160;
    internal const int noTruncationMaximumTruncationLength = 1_000_000;
    private readonly Checker ch;
    private readonly NodeFactory f;
    private readonly NodeVisitor cloneBindingNameVisitor;
    internal readonly TypeScript.DiagnosticPrinting.EmitContext e;
    private readonly Dictionary<Node, Symbol> idToSymbol;
    // NodeBuilderContext selected fields (nodebuilderimpl.go:61). Cache cells
    // are builder-owned; context state is stacked and replaced for nested diagnostics.
    internal sealed record TrackedSymbolArgs(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning);
    internal readonly record struct CompositeSymbolIdentity(bool isConstructorObject, ulong symbolId, ulong nodeId);
    internal sealed class NodeBuilderContext
    {
        internal IModuleSpecifierGenerationHost host;
        internal Node enclosingDeclaration;
        internal int depth = 0, maxExpansionDepth = -1;
        internal SourceFile enclosingFile;
        internal int approximateLength;
        internal bool truncating, encounteredError, reportedDiagnostic, suppressReportInferenceFallback;
        internal readonly HashSet<uint> visitedTypes = new();
        internal readonly Dictionary<CompositeSymbolIdentity, int> symbolDepth = new();
        internal readonly Dictionary<ulong, Type> enclosingSymbolTypes = new();
        internal SymbolTrackerImpl tracker;
        internal List<TrackedSymbolArgs> trackedSymbols;
        internal NodeBuilderFlags flags;
        internal TypeScript.NodeBuilding.InternalFlags internalFlags;
        internal TypeMapper mapper;
        internal List<Type> inferTypeParameters;
        internal List<Symbol> reverseMappedStack;
        internal Dictionary<ulong, Symbol> remappedSymbolReferences = new();
        internal Dictionary<uint, Node> typeParameterNames = new();
        internal HashSet<string> typeParameterNamesByText = new(StringComparer.Ordinal);
        internal Dictionary<string, int> typeParameterNamesByTextNextNameCount = new(StringComparer.Ordinal);
        internal HashSet<ulong> typeParameterSymbolList = new();
    }

    private readonly LinkStore<Symbol, NodeBuilderSymbolLinks> symbolLinks = new();
    private sealed class NodeBuilderSymbolLinks
    {
        internal Dictionary<ModeAwareCacheKey, moduleSpecifierResult> specifierCache;
    }

    private readonly record struct moduleSpecifierResult(string specifier, Type importAttributesType = null);
    internal NodeBuilderContext ctx;
    internal int approximateLength { get => ctx.approximateLength; set => ctx.approximateLength = value; }
    private bool truncating { get => ctx.truncating; set => ctx.truncating = value; }
    private bool encounteredError { get => ctx.encounteredError; set => ctx.encounteredError = value; }

    private bool inObjectTypeLiteral
    {
        get => (ctx.flags & NodeBuilderFlags.InObjectTypeLiteral) != 0;
        set
        {
            if (value)
                ctx.flags |= NodeBuilderFlags.InObjectTypeLiteral;
            else
                ctx.flags &= ~NodeBuilderFlags.InObjectTypeLiteral;
        }
    }

    private bool useFullyQualifiedType
    {
        get => (ctx.flags & NodeBuilderFlags.UseFullyQualifiedType) != 0;
        set
        {
            if (value)
                ctx.flags |= NodeBuilderFlags.UseFullyQualifiedType;
            else
                ctx.flags &= ~NodeBuilderFlags.UseFullyQualifiedType;
        }
    }

    private HashSet<uint> visitedTypes => ctx.visitedTypes;
    private Dictionary<CompositeSymbolIdentity, int> symbolDepth => ctx.symbolDepth;

    private readonly PseudoChecker pc;
    // Selected diagnostic flags and internal flags retain their complete values;
    // ordered infer-parameter identity is included; configured hover expansion is excluded.
    private readonly record struct CompositeTypeCacheIdentity(uint typeId, bool fullyQualified, bool inObject, NodeBuilderFlags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, Checker.CacheHashKey inferTypeParameters);
    private sealed record SerializedTypeEntry(Node node, bool truncating, int addedLength, List<TrackedSymbolArgs> trackedSymbols);
    private sealed class NodeBuilderLinks
    {
        internal Dictionary<CompositeTypeCacheIdentity, SerializedTypeEntry> serializedTypes;
        internal string fakeScopeForSignatureDeclaration;
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

    internal void enterContext(Node enclosingDeclaration = null, bool fullyQualified = false, bool noTypeReduction = false)
    {
        ctx = new()
        {
            host = ch.program,
            enclosingDeclaration = enclosingDeclaration,
            enclosingFile = Ast.GetSourceFileOfNode(enclosingDeclaration),
            flags = NodeBuilderFlags.IgnoreErrors | NodeBuilderFlags.AllowUniqueESSymbolType | NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope | (fullyQualified ? NodeBuilderFlags.UseFullyQualifiedType : 0) | (noTypeReduction ? NodeBuilderFlags.NoTypeReduction : 0),
            tracker = null
        };
        ctx.tracker = SymbolTrackerImpl.NewSymbolTrackerImpl(ctx, null);
    }

    // nodebuilder.go:34. Non-hover source context with explicit flags and optional tracker.
    internal void enterContext(Node enclosingDeclaration, NodeBuilderFlags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker)
    {
        ctx = new()
        {
            host = ch.program,
            enclosingDeclaration = enclosingDeclaration,
            enclosingFile = Ast.GetSourceFileOfNode(enclosingDeclaration),
            flags = flags,
            internalFlags = internalFlags,
            inferTypeParameters = new(),
            trackedSymbols = new(),
            reverseMappedStack = new()
        };
        ctx.tracker = SymbolTrackerImpl.NewSymbolTrackerImpl(ctx, tracker);
    }

    internal NodeBuilderImpl(Checker checker) : this(checker, new TypeScript.DiagnosticPrinting.EmitContext())
    {
    }

    internal NodeBuilderImpl(Checker checker, TypeScript.DiagnosticPrinting.EmitContext context, Dictionary<Node, Symbol> symbols = null)
    {
        ch = checker;
        e = context;
        idToSymbol = symbols ?? new();
        f = e.Factory;
        cloneBindingNameVisitor = new NodeVisitor(cloneBindingName, f, new NodeVisitorHooks());
        pc = PseudoChecker.NewPseudoChecker(checker.strictNullChecks, checker.exactOptionalPropertyTypes);
    }

    // typeToTypeNode (3358): selected scalar, alias, reference and composite branches; default expansion is disabled.
    internal Node typeToTypeNode(Type t)
    {
        if (t == null)
        {
            if ((ctx.flags & NodeBuilderFlags.AllowEmptyUnionOrIntersection) == 0)
            {
                encounteredError = true;
                return null;
            }

            approximateLength += 3;
            return f.NewKeywordTypeNode(Kind.AnyKeyword);
        }

        var inTypeAlias = ctx.flags & NodeBuilderFlags.InTypeAlias;
        ctx.flags &= ~NodeBuilderFlags.InTypeAlias;
        t = Checker.getNonDistributedTypeParameter(t);
        if ((ctx.flags & NodeBuilderFlags.NoTypeReduction) == 0)
            t = ch.getReducedType(t);
        if ((t.flags & TypeFlags.Any) != 0)
        {
            if (t.alias != null)
                return aliasToTypeReferenceNode(t.alias);
            if (t == ch.unresolvedType)
                return e.AddSyntheticLeadingComment(f.NewKeywordTypeNode(Kind.AnyKeyword), Kind.MultiLineCommentTrivia, "unresolved", false);
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

        if ((t.flags & TypeFlags.Boolean) != 0 && t.alias == null)
        {
            approximateLength += 7;
            return f.NewKeywordTypeNode(Kind.BooleanKeyword);
        }

        // nodebuilderimpl.go:3417. Enum members retain their parent reference
        // and actual member spelling; enum unions expand only when requested.
        bool expandingEnum = false;
        if ((t.flags & TypeFlags.EnumLike) != 0)
        {
            if ((t.symbol.Flags & SymbolFlags.EnumMember) != 0)
            {
                var parent = ch.getParentOfSymbol(t.symbol);
                var parentName = symbolToTypeNode(parent, SymbolFlags.Type, null);
                if (ch.getDeclaredTypeOfSymbol(parent) == t)
                    return parentName;
                var memberName = Ast.SymbolName(t.symbol);
                if (TypeScript.Scanner.Scanner.IsIdentifierTextByteString(memberName, LanguageVariant.Standard))
                    return appendReferenceToType(parentName, f.NewTypeReferenceNode(f.NewIdentifier(memberName), null));
                if (Ast.IsImportTypeNode(parentName))
                {
                    parentName.AsImportTypeNode().IsTypeOf = true;
                    return f.NewIndexedAccessTypeNode(parentName, f.NewLiteralTypeNode(newStringLiteral(memberName)));
                }
                if (Ast.IsTypeReferenceNode(parentName))
                    return f.NewIndexedAccessTypeNode(f.NewTypeQueryNode(parentName.AsTypeReferenceNode().TypeName, null),
                        f.NewLiteralTypeNode(newStringLiteral(memberName)));
                throw new InvalidOperationException("Unhandled enum parent type node kind");
            }
            if ((t.flags & TypeFlags.Union) == 0 || !shouldExpandType(t, false))
                return symbolToTypeNode(t.symbol, SymbolFlags.Type, null);
            expandingEnum = true;
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

        if ((t.flags & TypeFlags.BigIntLiteral) != 0)
        {
            var text = Checker.pseudoBigIntToString(Checker.getBigIntLiteralValue(t));
            approximateLength += text.Length + 1;
            return f.NewLiteralTypeNode(f.NewBigIntLiteral(text + "n", TokenFlags.None));
        }

        if ((t.flags & TypeFlags.BooleanLiteral) != 0)
        {
            bool value = (bool)t.AsLiteralType().value;
            approximateLength += value ? 4 : 5;
            return f.NewLiteralTypeNode(f.NewKeywordExpression(value ? Kind.TrueKeyword : Kind.FalseKeyword));
        }

        // nodebuilderimpl.go:3470. Default diagnostic flags allow the unique
        // operator; declaration reuse also preserves the accessible-symbol arm.
        if ((t.flags & TypeFlags.UniqueESSymbol) != 0)
        {
            if ((ctx.flags & NodeBuilderFlags.AllowUniqueESSymbolType) == 0)
            {
                if (ch.IsValueSymbolAccessible(t.symbol, ctx.enclosingDeclaration))
                {
                    approximateLength += 6;
                    return symbolToTypeNode(t.symbol, SymbolFlags.Value, null);
                }

                ctx.tracker.ReportInaccessibleUniqueSymbolError();
            }

            approximateLength += 13;
            return f.NewTypeOperatorNode(Kind.UniqueKeyword, f.NewKeywordTypeNode(Kind.SymbolKeyword));
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

        if ((t.flags & TypeFlags.ESSymbol) != 0)
        {
            approximateLength += 6;
            return f.NewKeywordTypeNode(Kind.SymbolKeyword);
        }

        if ((t.flags & TypeFlags.NonPrimitive) != 0)
        {
            approximateLength += 6;
            return f.NewKeywordTypeNode(Kind.ObjectKeyword);
        }

        if ((t.flags & TypeFlags.TypeParameter) != 0 && t.AsTypeParameter().isThisType)
        {
            if (inObjectTypeLiteral)
            {
                if (!encounteredError && (ctx.flags & NodeBuilderFlags.AllowThisInObjectLiteral) == 0)
                    encounteredError = true;
                ctx.tracker.ReportInaccessibleThisError();
            }

            approximateLength += 4;
            return f.NewThisTypeNode();
        }

        // nodebuilderimpl.go:3516. Default diagnostics allow aliases from outside
        // the current scope; other selected contexts use actual accessibility.
        if (inTypeAlias == 0 && t.alias != null && ((ctx.flags & NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope) != 0 || ch.IsTypeSymbolAccessible(t.alias.Symbol(), ctx.enclosingDeclaration)))
        {
            // Configured structural expansion is a service-only consumer. Ordinary
            // diagnostics preserve the source alias/member-name and array branches.
            var symbol = t.alias.Symbol();
            var typeArgumentNodes = mapToTypeNodes(t.alias.TypeArguments(), false);
            if (Checker.isReservedMemberName(symbol.Name) && (symbol.Flags & SymbolFlags.Class) == 0)
                return f.NewTypeReferenceNode(f.NewIdentifier(""), typeArgumentNodes);
            if (typeArgumentNodes?.Nodes.Count == 1 && symbol == ch.globalArrayType.symbol)
                return f.NewArrayTypeNode(typeArgumentNodes.Nodes[0]);
            return symbolToTypeNode(symbol, SymbolFlags.Type, typeArgumentNodes);
        }
        if ((t.objectFlags & ObjectFlags.Reference) != 0)
        {
            if (ch.isArrayOrTupleType(t))
                return visitAndTransformType(t, arrayOrTupleTypeToNode);
            if (t.AsTypeReference().node != null)
                return visitAndTransformType(t, typeReferenceToTypeNode);
            return typeReferenceToTypeNode(t);
        }

        if ((t.flags & TypeFlags.TypeParameter) != 0 || (t.objectFlags & ObjectFlags.ClassOrInterface) != 0)
        {
            if ((t.flags & TypeFlags.TypeParameter) != 0 && Core.Some(ctx.inferTypeParameters, parameter => ReferenceEquals(parameter, t)))
            {
                approximateLength += Ast.SymbolName(t.symbol).Length + 6;
                Node constraintNode = null;
                var constraint = ch.getConstraintOfTypeParameter(t);
                if (constraint != null)
                {
                    var inferredConstraint = ch.getInferredTypeParameterConstraint(t, true);
                    if (inferredConstraint == null || !ch.isTypeIdenticalTo(constraint, inferredConstraint))
                    {
                        approximateLength += 9;
                        constraintNode = typeToTypeNode(constraint);
                    }
                }

                return f.NewInferTypeNode(typeParameterToDeclarationWithConstraint(t, constraintNode));
            }


            if ((ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0 && (t.flags & TypeFlags.TypeParameter) != 0)
            {
                var parameterName = typeParameterToName(t);
                approximateLength += parameterName.Text().Length;
                return f.NewTypeReferenceNode(newIdentifier(parameterName.Text(), t.symbol), null);
            }

            if (t.symbol != null)
                return symbolToTypeNode(t.symbol, SymbolFlags.Type, null);
            string name;
            if ((t == ch.markerSuperTypeForCheck || t == ch.markerSubTypeForCheck) &&
                ch.varianceTypeParameter?.symbol != null)
                name = (t == ch.markerSubTypeForCheck ? "sub-" : "super-") + Ast.SymbolName(ch.varianceTypeParameter.symbol);
            else
                name = "?";
            return f.NewTypeReferenceNode(newIdentifier(name, null), null);
        }

        if ((t.flags & TypeFlags.Union) != 0 && t.AsUnionType().origin != null)
            t = t.AsUnionType().origin;
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
        {
            var types = (t.flags & TypeFlags.Union) != 0 ? ch.formatUnionTypes(t.Types(), expandingEnum) : t.Types();
            if (types.Count == 1)
                return typeToTypeNode(types[0]);
            var nodes = mapToTypeNodes(types, true);
            if (nodes != null && nodes.Nodes is { Count: > 0 })
                return (t.flags & TypeFlags.Union) != 0 ? f.NewUnionTypeNode(nodes) : f.NewIntersectionTypeNode(nodes);
            if (!encounteredError && (ctx.flags & NodeBuilderFlags.AllowEmptyUnionOrIntersection) == 0)
                encounteredError = true;
            return null;
        }

        if ((t.objectFlags & (ObjectFlags.Anonymous | ObjectFlags.Mapped)) != 0)
            return createAnonymousTypeNode(t);
        if ((t.flags & TypeFlags.Index) != 0)
        {
            approximateLength += 6;
            return f.NewTypeOperatorNode(Kind.KeyOfKeyword, typeToTypeNode(t.Target()));
        }

        // nodebuilderimpl.go:3634. Internal template keys retain real placeholder types.
        if ((t.flags & TypeFlags.TemplateLiteral) != 0)
        {
            var texts = t.AsTemplateLiteralType().texts;
            var types = t.AsTemplateLiteralType().types;
            var head = f.NewTemplateHead(texts[0], "", TokenFlags.None);
            e.AddEmitFlags(head, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
            var spans = f.NewNodeList(Core.MapIndexSlice(types, (type, i) =>
            {
                var literal = i < types.Count - 1 ? f.NewTemplateMiddle(texts[i + 1], "", TokenFlags.None) : f.NewTemplateTail(texts[i + 1], "", TokenFlags.None);
                e.AddEmitFlags(literal, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                return f.NewTemplateLiteralTypeSpan(typeToTypeNode(type), literal);
            }));
            approximateLength += 2;
            return f.NewTemplateLiteralTypeNode(head, spans);
        }

        if ((t.flags & TypeFlags.StringMapping) != 0)
        {
            var typeNode = typeToTypeNode(t.Target());
            return symbolToTypeNode(t.symbol, SymbolFlags.Type, f.NewNodeList(Slice<Node>.FromArray([typeNode])));
        }

        if ((t.flags & TypeFlags.IndexedAccess) != 0)
        {
            var objectType = typeToTypeNode(t.AsIndexedAccessType().objectType);
            var indexType = typeToTypeNode(t.AsIndexedAccessType().indexType);
            approximateLength += 2;
            return f.NewIndexedAccessTypeNode(objectType, indexType);
        }

        if ((t.flags & TypeFlags.Conditional) != 0)
            return visitAndTransformType(t, conditionalTypeToTypeNode);
        if ((t.flags & TypeFlags.Substitution) != 0)
        {
            var typeNode = typeToTypeNode(t.AsSubstitutionType().baseType);
            if (!ch.isNoInferType(t))
                return typeNode;
            var noInferSymbol = ch.getGlobalTypeAliasSymbol("NoInfer", 1, false);
            return noInferSymbol != null ? symbolToTypeNode(noInferSymbol, SymbolFlags.Type, f.NewNodeList(Slice<Node>.FromArray([typeNode]))) : typeNode;
        }

        throw Checker.unsupported("typeToTypeNode: type payload");
    }

    // nodebuilderimpl.go:3696/641. Type-alias identity is retained in syntax.
    private Node aliasToTypeReferenceNode(TypeAlias alias) => f.NewTypeReferenceNode(symbolToEntityNameNode(alias.Symbol()), mapToTypeNodes(alias.TypeArguments(), false));
    private Node symbolToEntityNameNode(Symbol symbol)
    {
        var identifier = newIdentifier(symbol.Name, symbol);
        return symbol.Parent != null ? f.NewQualifiedName(symbolToEntityNameNode(symbol.Parent), identifier) : identifier;
    }

    // nodebuilderimpl.go:650. Real accessible/qualified chains and generated
    // diagnostic import types, including portable Node16/NodeNext mode overrides.
    private Node symbolToTypeNode(Symbol symbol, SymbolFlags meaning, NodeList typeArguments)
    {
        var chain = lookupSymbolChain(symbol, meaning, (ctx.flags & NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope) == 0);
        if (GoRuntime.Len(chain) == 0)
            return null;
        bool isTypeOf = meaning == SymbolFlags.Value;
        if (Core.Some(chain[0].Declarations, Checker.hasNonGlobalAugmentationExternalModuleSymbol))
        {
            var nonRootParts = chain.Count > 1 ? createAccessFromSymbolChain(chain, chain.Count - 1, 1, typeArguments) : null;
            var typeParameterNodes = typeArguments ?? lookupTypeParameterNodes(chain, 0);
            var contextFile = Ast.GetSourceFileOfNode(e.MostOriginal(ctx.enclosingDeclaration));
            var targetFile = Ast.GetSourceFileOfModule(chain[0]);
            moduleSpecifierResult result = default;
            var importModeOverride = ResolutionMode.None;
            if (ch.compilerOptions.GetModuleResolutionKind() is ModuleResolutionKind.Node16 or ModuleResolutionKind.NodeNext)
            {
                if (targetFile != null && contextFile != null &&
                    ch.program.GetEmitModuleFormatOfFile(targetFile) == ModuleKind.ESNext &&
                    ch.program.GetEmitModuleFormatOfFile(targetFile) != ch.program.GetEmitModuleFormatOfFile(contextFile))
                {
                    result = getSpecifierForModuleSymbol(chain[0], ResolutionMode.ESNext);
                    importModeOverride = ResolutionMode.ESNext;
                }
            }
            if (string.IsNullOrEmpty(result.specifier))
                result = getSpecifierForModuleSymbol(chain[0], ResolutionMode.None);
            if ((ctx.flags & NodeBuilderFlags.AllowNodeModulesRelativePaths) == 0 && result.specifier.Contains("/node_modules/", StringComparison.Ordinal))
            {
                var oldResult = result;
                if (ch.compilerOptions.GetModuleResolutionKind() is ModuleResolutionKind.Node16 or ModuleResolutionKind.NodeNext)
                {
                    var swappedMode = ch.program.GetEmitModuleFormatOfFile(contextFile) == ModuleKind.ESNext
                        ? ResolutionMode.CommonJS : ResolutionMode.ESNext;
                    result = getSpecifierForModuleSymbol(chain[0], swappedMode);
                    if (result.specifier.Contains("/node_modules/", StringComparison.Ordinal))
                        result = oldResult;
                    else
                        importModeOverride = swappedMode;
                }
                if (importModeOverride == ResolutionMode.None)
                {
                    ctx.encounteredError = true;
                    ctx.tracker.ReportLikelyUnsafeImportRequiredError(oldResult.specifier, symbol.Name);
                }
            }
            var attributes = createImportAttributesForModuleSpecifier(result, importModeOverride);
            var literal = f.NewLiteralTypeNode(newStringLiteral(result.specifier));
            approximateLength += result.specifier.Length + 10;
            if (nonRootParts == null || Ast.IsEntityName(nonRootParts))
                return f.NewImportTypeNode(isTypeOf, literal, attributes, nonRootParts, typeParameterNodes);
            var splitNode = getTopmostIndexedAccessType(nonRootParts.AsIndexedAccessTypeNode());
            return f.NewIndexedAccessTypeNode(f.NewImportTypeNode(isTypeOf, literal, attributes, splitNode.ObjectType.AsTypeReferenceNode().TypeName, typeParameterNodes), splitNode.IndexType);
        }

        var entityName = createAccessFromSymbolChain(chain, chain.Count - 1, 0, typeArguments);
        if (Ast.IsIndexedAccessTypeNode(entityName))
            return entityName;
        if (Ast.IsEntityName(entityName))
            return isTypeOf ? f.NewTypeQueryNode(entityName, null) : f.NewTypeReferenceNode(entityName, typeArguments);
        if (isTypeOf && Ast.IsExpressionWithTypeArguments(entityName))
            return f.NewTypeQueryNode(f.DeepCloneNode(entityName.Expression()), entityName.AsExpressionWithTypeArguments().TypeArguments);
        return entityName;
    }

    // nodebuilderimpl.go:753. Ordered alias/export/member-qualified access chain.
    private Node createAccessFromSymbolChain(List<Symbol> chain, int index, int stopper, NodeList overrideTypeArguments)
    {
        var typeParameterNodes = overrideTypeArguments;
        if (index != chain.Count - 1)
            typeParameterNodes = lookupTypeParameterNodes(chain, index);
        var symbol = chain[index];
        var parent = index > 0 ? chain[index - 1] : null;
        var symbolName = "";
        if (index == 0)
        {
            ctx.flags |= NodeBuilderFlags.InInitialEntityName;
            symbolName = getNameOfSymbolAsWritten(symbol);
            approximateLength += symbolName.Length + 1;
            ctx.flags ^= NodeBuilderFlags.InInitialEntityName;
        }
        else if (parent != null)
        {
            var exports = ch.getExportsOfSymbol(parent);
            if (exports != null)
            {
                var quick = exports[symbol.Name];
                if (symbol.Name != Ast.InternalSymbolNameExportEquals && !Checker.isLateBoundName(symbol.Name) && quick != null && ch.getSymbolIfSameReference(quick, symbol) != null)
                    symbolName = symbol.Name;
                else
                {
                    var results = new Dictionary<Symbol, string>();
                    foreach (var(name, exported)in exports)
                        if (ch.getSymbolIfSameReference(exported, symbol) != null && !Checker.isLateBoundName(name) && name != Ast.InternalSymbolNameExportEquals)
                            results[exported] = name;
                    var resultSymbols = results.Keys.ToList();
                    if (resultSymbols.Count > 0)
                    {
                        ch.sortSymbols(resultSymbols);
                        symbolName = results[resultSymbols[0]];
                    }
                }
            }
        }

        if (symbolName.Length == 0)
        {
            var name = Core.FirstNonNil(symbol.Declarations, Ast.GetNameOfDeclaration);
            if (name != null && Ast.IsComputedPropertyName(name) && Ast.IsEntityName(name.Expression()))
            {
                var lhs = createAccessFromSymbolChain(chain, index - 1, stopper, overrideTypeArguments);
                return Ast.IsEntityName(lhs) ? f.NewIndexedAccessTypeNode(f.NewParenthesizedTypeNode(f.NewTypeQueryNode(lhs, null)), f.NewTypeQueryNode(name.Expression(), null)) : lhs;
            }

            symbolName = getNameOfSymbolAsWritten(symbol);
        }

        approximateLength += symbolName.Length + 1;
        if ((ctx.flags & NodeBuilderFlags.ForbidIndexedAccessSymbolReferences) == 0 && parent != null && ch.getMembersOfSymbol(parent) != null && ch.getMembersOfSymbol(parent)[symbol.Name] != null && ch.getSymbolIfSameReference(ch.getMembersOfSymbol(parent)[symbol.Name], symbol) != null)
        {
            var lhs = createAccessFromSymbolChain(chain, index - 1, stopper, overrideTypeArguments);
            return f.NewIndexedAccessTypeNode(Ast.IsIndexedAccessTypeNode(lhs) ? lhs : f.NewTypeReferenceNode(lhs, typeParameterNodes), f.NewLiteralTypeNode(newStringLiteral(symbolName)));
        }

        var identifier = newIdentifier(symbolName, symbol);
        e.AddEmitFlags(identifier, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
        if (index > stopper)
        {
            var lhs = createAccessFromSymbolChain(chain, index - 1, stopper, overrideTypeArguments);
            if ((ctx.flags & NodeBuilderFlags.UseInstantiationExpressions) == 0 || Ast.IsEntityName(lhs) && GoRuntime.Len(typeParameterNodes?.Nodes ?? default) == 0)
                return f.NewQualifiedName(lhs, identifier);
            return createExpressionWithTypeArguments(f.NewPropertyAccessExpression(createAccessExpression(lhs), null, identifier, NodeFlags.None), typeParameterNodes);
        }

        return identifier;
    }

    // nodebuilderimpl.go:3160. Nongeneric interface references can have a hidden
    // this argument; the original declared-parameter count elides that argument.
    private Node typeReferenceToTypeNode(Type t)
    {
        List<Type> typeArguments = this.ch.getTypeArguments(t);
        if ((((((this.ctx.flags & NodeBuilderFlags.WriteClassExpressionAsTypeLiteral) != 0) && (t.symbol.ValueDeclaration != null)) && Ast.IsClassLike(t.symbol.ValueDeclaration)) && !this.ch.IsValueSymbolAccessible(t.symbol, this.ctx.enclosingDeclaration)))
        {
            return this.createAnonymousTypeNode(t);
        }
        else
        {
            var outerTypeParameters = t.Target().AsInterfaceType().OuterTypeParameters();
            var i = 0;
            Node resultType = default;
            if ((outerTypeParameters != null))
            {
                var length = GoRuntime.Len(outerTypeParameters);
                while ((i < length))
                {
                    var start = i;
                    var parent = this.getParentSymbolOfTypeParameter(outerTypeParameters[i].AsTypeParameter());
                    {
                        var ok = true;
                        for (; ok; ok = ((i < length) && (this.getParentSymbolOfTypeParameter(outerTypeParameters[i].AsTypeParameter()) == parent)))
                        {
                            i++;
                        }
                    }

                    if (!GoSlices.Equal(GoRuntime.Slice(outerTypeParameters, start, i), GoRuntime.Slice(typeArguments, start, i)))
                    {
                        var typeArgumentSlice = this.mapToTypeNodes(GoRuntime.Slice(typeArguments, start, i), false);
                        var restoreFlags = this.saveRestoreFlags();
                        this.ctx.flags |= NodeBuilderFlags.ForbidIndexedAccessSymbolReferences;
                        var @ref = this.symbolToTypeNode(parent, SymbolFlags.Type, typeArgumentSlice);
                        restoreFlags();
                        if ((resultType == null))
                        {
                            resultType = @ref;
                        }
                        else
                        {
                            resultType = this.appendReferenceToType(resultType, @ref);
                        }
                    }
                }
            }

            NodeList typeArgumentNodes = default;
            if ((GoRuntime.Len(typeArguments) > 0))
            {
                var typeParameterCount = 0;
                var typeParams = t.Target().AsInterfaceType().TypeParameters();
                if ((typeParams != null))
                {
                    typeParameterCount = Math.Min(GoRuntime.Len(typeParams), GoRuntime.Len(typeArguments));
                    if ((((this.ch.isReferenceToType(t, this.ch.getGlobalIterableType()) || this.ch.isReferenceToType(t, this.ch.getGlobalIterableIteratorType())) || this.ch.isReferenceToType(t, this.ch.getGlobalAsyncIterableType())) || this.ch.isReferenceToType(t, this.ch.getGlobalAsyncIterableIteratorType())))
                    {
                        if (((((t.AsTypeReference().node == null) || !Ast.IsTypeReferenceNode(t.AsTypeReference().node)) || (t.AsTypeReference().node.TypeArguments().IsNil)) || (GoRuntime.Len(t.AsTypeReference().node.TypeArguments()) < typeParameterCount)))
                        {
                            while ((typeParameterCount > 0))
                            {
                                var typeArgument = typeArguments[(typeParameterCount - 1)];
                                var typeParameter = t.Target().AsInterfaceType().TypeParameters()[(typeParameterCount - 1)];
                                var defaultType = this.ch.getDefaultFromTypeParameter(typeParameter);
                                if (((defaultType == null) || !this.ch.isTypeIdenticalTo(typeArgument, defaultType)))
                                {
                                    break;
                                }

                                typeParameterCount--;
                            }
                        }
                    }
                }

                typeArgumentNodes = this.mapToTypeNodes(GoRuntime.Slice(typeArguments, i, typeParameterCount), false);
            }

            var restoreFlags2 = this.saveRestoreFlags();
            this.ctx.flags |= NodeBuilderFlags.ForbidIndexedAccessSymbolReferences;
            var finalRef = this.symbolToTypeNode(t.symbol, SymbolFlags.Type, typeArgumentNodes);
            restoreFlags2();
            if ((resultType == null))
            {
                return finalRef;
            }
            else
            {
                return this.appendReferenceToType(resultType, finalRef);
            }
        }
    }

    // nodebuilderimpl.go:443/512/2994. Actual source reuse, resolution cache and mapper.
    private Node serializeTypeName(Node node, bool isTypeOf, NodeList typeArguments)
    {
        var meaning = isTypeOf ? SymbolFlags.Value : SymbolFlags.Type;
        var symbol = ch.resolveEntityName(node, meaning, true, false, node);
        if (symbol == null)
            return null;
        var resolvedSymbol = (symbol.Flags & SymbolFlags.Alias) != 0 ? ch.resolveAlias(symbol) : symbol;
        if (ch.IsSymbolAccessible(symbol, ctx.enclosingDeclaration, meaning, false).Accessibility != TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible)
            return null;
        return symbolToTypeNode(resolvedSymbol, meaning, typeArguments);
    }

    private Symbol tryGetResolvedSymbolFromTypeNode(Node node)
    {
        if (node == null || node.Parent == null)
            return null;
        ch.getTypeFromTypeNode(node);
        return ch.symbolNodeLinks.TryGet(node)?.resolvedSymbol;
    }

    private Type getTypeFromTypeNode(Node node, bool noMappedTypes)
    {
        if (node.Parent == null)
            return ch.errorType;
        var type = ch.getTypeFromTypeNode(node);
        if (ctx.mapper == null)
            return type;
        var instantiated = ch.instantiateType(type, ctx.mapper);
        return noMappedTypes && instantiated != type ? null : instantiated;
    }

    // newStringLiteral/newStringLiteralEx (3681); default double quote context.
    private Node newStringLiteral(string text) => newStringLiteralEx(text, false);
    private Node newStringLiteralEx(string text, bool singleQuote) => f.NewStringLiteral(text, singleQuote || (ctx.flags & NodeBuilderFlags.UseSingleQuotesForStringLiteralType) != 0 ? TokenFlags.SingleQuote : TokenFlags.None);
    // nodebuilderimpl.go:585/849/854. Selected flags disable computed-property
    // writing; actual selected enclosing declarations and trackers are retained.
    internal Node symbolToNode(Symbol symbol, SymbolFlags meaning) => symbolToExpression(symbol, meaning);
    private Node symbolToExpression(Symbol symbol, SymbolFlags mask)
    {
        ctx.tracker.TrackSymbol(symbol, ctx.enclosingDeclaration, mask);
        return symbolToExpressionWorker(symbol, mask);
    }

    private Node symbolToExpressionWorker(Symbol symbol, SymbolFlags mask)
    {
        var chain = lookupSymbolChainWorker(symbol, mask, false);
        return createExpressionFromSymbolChain(chain, chain.Count - 1);
    }

    // nodebuilderimpl.go:1067. With no enclosing declaration and no fully
    // qualified flag, upstream appends only the supplied symbol to the chain.
    private List<Symbol> lookupSymbolChainWorker(Symbol symbol, SymbolFlags meaning, bool yieldModuleSymbol)
    {
        if ((symbol.Flags & SymbolFlags.TypeParameter) == 0 && (ctx.enclosingDeclaration != null || useFullyQualifiedType) && (ctx.internalFlags & TypeScript.NodeBuilding.InternalFlags.DoNotIncludeSymbolChain) == 0)
            return getSymbolChain(symbol, meaning, true, yieldModuleSymbol);
        return[symbol];
    }

    // nodebuilderimpl.go:1062. Only the requested symbol is tracked here.
    private List<Symbol> lookupSymbolChain(Symbol symbol, SymbolFlags meaning, bool yieldModuleSymbol)
    {
        ctx.tracker.TrackSymbol(symbol, ctx.enclosingDeclaration, meaning);
        return lookupSymbolChainWorker(symbol, meaning, yieldModuleSymbol);
    }

    // nodebuilderimpl.go:1092. Actual accessible/parent/fallback chain preference.
    private List<Symbol> getSymbolChain(Symbol symbol, SymbolFlags meaning, bool endOfChain, bool yieldModuleSymbol)
    {
        var chain = ch.getAccessibleSymbolChain(symbol, ctx.enclosingDeclaration, meaning, (ctx.flags & NodeBuilderFlags.UseOnlyExternalAliasing) != 0);
        var qualifierMeaning = GoRuntime.Len(chain) > 1 ? Checker.getQualifiedLeftMeaning(meaning) : meaning;
        if (GoRuntime.Len(chain) == 0 || ch.needsQualification(chain[0], ctx.enclosingDeclaration, qualifierMeaning))
        {
            var root = GoRuntime.Len(chain) > 0 ? chain[0] : symbol;
            var parents = ch.getContainersOfSymbol(root, ctx.enclosingDeclaration, meaning);
            if (GoRuntime.Len(parents) > 0)
            {
                var pairs = Core.Map(parents, parent => new sortedSymbolNamePair(parent, Core.Some(parent.Declarations, Checker.hasNonGlobalAugmentationExternalModuleSymbol) ? getSpecifierForModuleSymbol(parent, ResolutionMode.None).specifier : ""));
                pairs = pairs.OrderBy(pair => pair, Comparer<sortedSymbolNamePair>.Create(sortByBestName)).ToList();
                foreach (var pair in pairs)
                {
                    var parent = pair.sym;
                    var parentChain = getSymbolChain(parent, Checker.getQualifiedLeftMeaning(meaning), false, yieldModuleSymbol);
                    if (GoRuntime.Len(parentChain) == 0)
                        continue;
                    var exported = parent.Exports?[Ast.InternalSymbolNameExportEquals];
                    if (exported != null && ch.getSymbolIfSameReference(exported, symbol) != null)
                    {
                        chain = parentChain;
                        break;
                    }

                    var next = chain;
                    if (GoRuntime.Len(next) == 0)
                        next = GoRuntime.Append(next, ch.getAliasForSymbolInContainer(parent, symbol) ?? symbol);
                    chain = GoRuntime.Append(parentChain, next);
                    break;
                }
            }
        }

        if (GoRuntime.Len(chain) > 0)
            return chain;
        if (endOfChain || (symbol.Flags & (SymbolFlags.TypeLiteral | SymbolFlags.ObjectLiteral)) == 0)
        {
            if (!endOfChain && !yieldModuleSymbol && Core.Some(symbol.Declarations, Checker.hasNonGlobalAugmentationExternalModuleSymbol))
                return null;
            return[symbol];
        }

        return null;
    }

    // nodebuilderimpl.go:859. Actual qualified expression/module-literal chain.
    private Node createExpressionFromSymbolChain(List<Symbol> chain, int index)
    {
        var typeParameterNodes = lookupExpressionChainTypeArgumentNodes(chain, index);
        var symbol = chain[index];
        if (index == 0)
            ctx.flags |= NodeBuilderFlags.InInitialEntityName;
        var symbolName = getNameOfSymbolAsWritten(symbol);
        if (index == 0)
            ctx.flags ^= NodeBuilderFlags.InInitialEntityName;
        if (startsWithSingleOrDoubleQuote(symbolName) && Core.Some(symbol.Declarations, Checker.hasNonGlobalAugmentationExternalModuleSymbol))
        {
            var result = getSpecifierForModuleSymbol(symbol, ResolutionMode.None);
            approximateLength += 2 + result.specifier.Length;
            return newStringLiteral(result.specifier);
        }

        if (index == 0 || canUsePropertyAccess(symbolName))
        {
            var identifier = newIdentifier(symbolName, symbol);
            e.AddEmitFlags(identifier, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
            approximateLength += 1 + symbolName.Length;
            if (index > 0)
            {
                var result = f.NewPropertyAccessExpression(createExpressionFromSymbolChain(chain, index - 1), null, identifier, NodeFlags.None);
                e.AddEmitFlags(result, TypeScript.DiagnosticPrinting.EmitFlags.NoIndentation);
                return createExpressionWithTypeArguments(result, typeParameterNodes);
            }

            return createExpressionWithTypeArguments(identifier, typeParameterNodes);
        }

        if (symbolName.StartsWith("[", StringComparison.Ordinal))
            symbolName = symbolName[1..^1];
        Node expression = null;
        if (startsWithSingleOrDoubleQuote(symbolName) && (symbol.Flags & SymbolFlags.EnumMember) == 0)
        {
            var literalText = StringUtil.UnquoteString(symbolName);
            approximateLength += literalText.Length + 2;
            expression = newStringLiteralEx(literalText, symbolName[0] == '\'');
        }
        else if (JsNum.FromString(symbolName).String() == symbolName)
        {
            approximateLength += symbolName.Length;
            expression = f.NewNumericLiteral(symbolName, TokenFlags.None);
        }

        if (expression == null)
        {
            approximateLength += symbolName.Length;
            expression = newIdentifier(symbolName, symbol);
            e.AddEmitFlags(expression, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
        }

        approximateLength += 2;
        return createExpressionWithTypeArguments(f.NewElementAccessExpression(createExpressionFromSymbolChain(chain, index - 1), null, expression, NodeFlags.None), typeParameterNodes);
    }

    // nodebuilderimpl.go:933/937/974. Actual declared/default/synthesized names.
    private static bool isDefaultBindingContext(Node location) => location.Kind == Kind.SourceFile || Ast.IsAmbientModule(location);
    private string getNameOfSymbolFromNameType(Symbol symbol)
    {
        if (!ch.valueSymbolLinks.Has(symbol))
            return "";
        var nameType = ch.valueSymbolLinks.Get(symbol).nameType;
        if (nameType == null)
            return "";
        if ((nameType.flags & TypeFlags.StringOrNumberLiteral) != 0)
        {
            var name = nameType.AsLiteralType().value switch
            {
                string text => text,
                JsNum.Number number => number.String(),
                _ => ""
            };
            if (!TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name, LanguageVariant.Standard) && !Checker.isNumericLiteralName(name))
                return Checker.ValueToString(nameType.AsLiteralType().value);
            if (Checker.isNumericLiteralName(name) && name.StartsWith("-", StringComparison.Ordinal))
                return "[" + name + "]";
            return name;
        }

        if ((nameType.flags & TypeFlags.UniqueESSymbol) != 0)
            return "[" + getNameOfSymbolAsWritten(nameType.symbol) + "]";
        return "";
    }

    private string getNameOfSymbolAsWritten(Symbol symbol)
    {
        if (ctx.remappedSymbolReferences.TryGetValue(Ast.GetSymbolId(symbol), out var remapped))
            symbol = remapped;
        if (symbol.Name == Ast.InternalSymbolNameDefault && (ctx.flags & NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope) == 0 && ((ctx.flags & NodeBuilderFlags.InInitialEntityName) == 0 || GoRuntime.Len(symbol.Declarations) == 0 || ctx.enclosingDeclaration != null && Ast.FindAncestor(symbol.Declarations[0], isDefaultBindingContext) != Ast.FindAncestor(ctx.enclosingDeclaration, isDefaultBindingContext)))
            return "default";
        if (symbol.Declarations is { Count: > 0 })
        {
            var name = Core.FirstNonNil(symbol.Declarations, Ast.GetNameOfDeclaration);
            if (name != null)
            {
                if (Ast.IsComputedPropertyName(name) && (symbol.CheckFlags & CheckFlags.Late) == 0 && ch.valueSymbolLinks.Has(symbol))
                {
                    var nameType = ch.valueSymbolLinks.Get(symbol).nameType;
                    if (nameType != null && (nameType.flags & TypeFlags.StringOrNumberLiteral) != 0)
                    {
                        var result = getNameOfSymbolFromNameType(symbol);
                        if (result.Length > 0)
                            return result;
                    }
                }

                return TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name);
            }

            var declaration = symbol.Declarations[0];
            if (declaration.Parent?.Kind == Kind.VariableDeclaration)
                return TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(declaration.Parent.Name());
            if (Ast.IsClassExpression(declaration) || Ast.IsFunctionExpression(declaration) || Ast.IsArrowFunction(declaration))
            {
                if (ctx != null && !ctx.encounteredError && (ctx.flags & NodeBuilderFlags.AllowAnonymousIdentifier) == 0)
                    ctx.encounteredError = true;
                return Ast.IsClassExpression(declaration) ? "(Anonymous class)" : "(Anonymous function)";
            }
        }

        var fromNameType = getNameOfSymbolFromNameType(symbol);
        return fromNameType.Length > 0 ? fromNameType : Ast.EscapeInternalSymbolName(symbol.Name);
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
        return f.NewExpressionWithTypeArguments(expression, typeArguments);
    }

    private NodeList lookupExpressionChainTypeArgumentNodes(List<Symbol> chain, int index)
    {
        if (shouldWriteTypeParametersInQualifiedName(chain, index))
        {
            var symbol = chain[index];
            var id = Ast.GetSymbolId(symbol);
            if (!ctx.typeParameterSymbolList.Add(id))
                return null;
            var args = lookupInstantiatedTypeArgumentNodes(chain, index);
            if (args != null)
                return args;
            var parameters = typeParametersToTypeParameterDeclarations(symbol);
            if (GoRuntime.Len(parameters) > 0)
                return f.NewNodeList(parameters);
        }

        return null;
    }

    // nodebuilderimpl.go:147. Compiler/diagnostic NoTruncation uses the original
    // larger finite limit; configured service verbosity remains external.
    private bool checkTruncationLength()
    {
        if (truncating)
            return truncating;
        var maxLength = (ctx.flags & NodeBuilderFlags.NoTruncation) != 0 ?
            noTruncationMaximumTruncationLength : defaultMaximumTruncationLength;
        truncating = approximateLength > maxLength;
        return truncating;
    }

    // nodebuilderimpl.go:342/3017. Elision preserves the source NoTruncation node.
    private Node createElidedInformationPlaceholder()
    {
        approximateLength += 3;
        return (ctx.flags & NodeBuilderFlags.NoTruncation) == 0 ?
            f.NewTypeReferenceNode(f.NewIdentifier("..."), null) :
            e.AddSyntheticLeadingComment(f.NewKeywordTypeNode(Kind.AnyKeyword),
                Kind.MultiLineCommentTrivia, "elided", false);
    }

    private Node createCyclicStructurePlaceholder()
    {
        if ((ctx.flags & NodeBuilderFlags.AllowAnonymousIdentifier) == 0)
        {
            encounteredError = true;
            ctx.tracker.ReportCyclicStructureError();
        }

        return createElidedInformationPlaceholder();
    }

    // nodebuilderimpl.go:350. Generated elision nodes and named references
    // retain the original truncation and name-collision branches.
    private NodeList mapToTypeNodes(List<Type> list, bool isBareList)
    {
        if (list == null || list.Count == 0)
            return null;
        if (checkTruncationLength())
        {
            if (!isBareList)
            {
                var node = (ctx.flags & NodeBuilderFlags.NoTruncation) != 0 ?
                    e.AddSyntheticLeadingComment(f.NewKeywordTypeNode(Kind.AnyKeyword),
                        Kind.MultiLineCommentTrivia, "elided", false) :
                    f.NewTypeReferenceNode(f.NewIdentifier("..."), null);
                return f.NewNodeList(Slice<Node>.FromArray([node]));
            }
            if (list.Count > 2)
            {
                var nodes = Slice<Node>.FromArray([typeToTypeNode(list[0]), null, typeToTypeNode(list[^1])]);
                nodes[1] = (ctx.flags & NodeBuilderFlags.NoTruncation) != 0 ?
                    e.AddSyntheticLeadingComment(f.NewKeywordTypeNode(Kind.AnyKeyword),
                        Kind.MultiLineCommentTrivia, $"... {list.Count - 2} more elided ...", false) :
                    f.NewTypeReferenceNode(f.NewIdentifier($"... {list.Count - 2} more ..."), null);
                return f.NewNodeList(nodes);
            }
        }

        var seenNames = useFullyQualifiedType ? null : new Dictionary<string, List<(Type type, int index)>>(StringComparer.Ordinal);
        var result = Slice<Node>.Make(0, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            int displayIndex = i + 1;
            if (checkTruncationLength() && displayIndex + 2 < list.Count - 1)
            {
                result = result.Append((ctx.flags & NodeBuilderFlags.NoTruncation) != 0 ?
                    e.AddSyntheticLeadingComment(f.NewKeywordTypeNode(Kind.AnyKeyword),
                        Kind.MultiLineCommentTrivia, $"... {list.Count - displayIndex} more elided ...", false) :
                    f.NewTypeReferenceNode(f.NewIdentifier($"... {list.Count - displayIndex} more ..."), null));
                var last = typeToTypeNode(list[^1]);
                if (last != null)
                    result = result.Append(last);
                break;
            }

            approximateLength += 2;
            var node = typeToTypeNode(list[i]);
            if (node == null)
                continue;
            result = result.Append(node);
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
                if (entries.Any(e => !typesAreSameReference(e.type, entries[0].type)))
                    foreach (var entry in entries)
                        result[entry.index] = typeToTypeNode(entry.type);
            useFullyQualifiedType = saved;
        }

        return f.NewNodeList(result);
    }

    // nodebuilderimpl.go:482. Instantiations of the same named type do not
    // require qualification solely because their type objects differ.
    private static bool typesAreSameReference(Type a, Type b) =>
        a == b || a.symbol != null && a.symbol == b.symbol || a.alias != null && a.alias == b.alias;

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
        if (Ast.IsComputedPropertyName(name))
            return (ch.checkExpression(name.Expression()).flags & TypeFlags.StringLike) != 0;
        if (Ast.IsElementAccessExpression(name))
            return (ch.checkExpression(name.AsElementAccessExpression().ArgumentExpression).flags & TypeFlags.StringLike) != 0;
        return Ast.IsStringLiteral(name);
    }

    private bool isSingleQuotedStringNamed(Node declaration)
    {
        var name = Ast.GetNameOfDeclaration(declaration);
        return name != null && Ast.IsStringLiteral(name) && (name.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) != 0;
    }

    private Node getPropertyNameNodeForSymbol(Symbol symbol, Node enclosingDeclaration)
    {
        if (symbol.ValueDeclaration != null && symbol.ValueDeclaration.Name()?.Kind == Kind.PrivateIdentifier)
            return f.DeepCloneNode(symbol.ValueDeclaration.Name());
        bool stringNamed = symbol.Declarations is { Count: > 0 } && symbol.Declarations.All(isStringNamed);
        bool singleQuote = symbol.Declarations is { Count: > 0 } && symbol.Declarations.All(isSingleQuotedStringNamed);
        var fromNameType = getPropertyNameNodeForSymbolFromNameType(symbol, enclosingDeclaration, singleQuote, stringNamed, (symbol.Flags & SymbolFlags.Method) != 0);
        if (fromNameType != null)
            return fromNameType;
        var name = symbol.Name;
        const string prefix = Ast.InternalSymbolNamePrefix + "#";
        if (name.StartsWith(prefix, StringComparison.Ordinal))
        {
            name = name[prefix.Length..];
            int digits = 0;
            while (digits < name.Length && name[digits] >= '0' && name[digits] <= '9')
                digits++;
            name = "__#private" + name[digits..];
        }
        return createPropertyNameNodeForIdentifierOrLiteral(name, singleQuote, stringNamed, (symbol.Flags & SymbolFlags.Method) != 0, symbol);
    }

    // nodebuilderimpl.go:2530. Mapped and instantiated property names retain
    // their real literal or unique-symbol payload rather than using the raw key.
    private Node getPropertyNameNodeForSymbolFromNameType(Symbol symbol, Node enclosingDeclaration, bool singleQuote, bool stringNamed, bool isMethod)
    {
        if (!ch.valueSymbolLinks.Has(symbol))
            return null;
        var nameType = ch.valueSymbolLinks.TryGet(symbol).nameType;
        if (nameType == null)
            return null;
        var enumEnclosingDeclaration = enclosingDeclaration;
        if (enumEnclosingDeclaration == null && ctx.enclosingFile != null)
            enumEnclosingDeclaration = ctx.enclosingFile.AsNode();
        if ((nameType.flags & TypeFlags.EnumLiteral) != 0)
        {
            var enumSymbol = nameType.symbol.Parent ?? nameType.symbol;
            if (enumEnclosingDeclaration != null && ch.IsSymbolAccessibleByFlags(enumSymbol, enumEnclosingDeclaration, SymbolFlags.Value))
            {
                var saved = ctx.enclosingDeclaration;
                ctx.enclosingDeclaration = enumEnclosingDeclaration;
                var result = f.NewComputedPropertyName(symbolToExpression(nameType.symbol, SymbolFlags.Value));
                ctx.enclosingDeclaration = saved;
                return result;
            }
        }
        if ((nameType.flags & TypeFlags.StringOrNumberLiteral) != 0)
        {
            string name = nameType.AsLiteralType().value switch
            {
                JsNum.Number number => number.String(),
                string text => text,
                _ => ""
            };
            if (!TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name, LanguageVariant.Standard) && (stringNamed || !Checker.isNumericLiteralName(name)))
                return f.NewStringLiteral(name, singleQuote ? TokenFlags.SingleQuote : TokenFlags.None);
            if (Checker.isNumericLiteralName(name) && name[0] == '-')
                return f.NewComputedPropertyName(f.NewPrefixUnaryExpression(Kind.MinusToken, f.NewNumericLiteral(name[1..], TokenFlags.None)));
            return createPropertyNameNodeForIdentifierOrLiteral(name, singleQuote, stringNamed, isMethod, symbol);
        }

        if ((nameType.flags & TypeFlags.UniqueESSymbol) != 0)
            return f.NewComputedPropertyName(symbolToExpressionWorker(nameType.symbol, SymbolFlags.Value));
        return null;
    }

    // nodebuilderimpl.go:2253. Selected data-property declaration serialization
    // retains nonnil diagnostic context and actual pseudochecker reuse.
    // Reverse-mapped implicit-undefined behavior is retained; source accessors remain excluded.
    private Node serializeTypeForDeclaration(Node declaration, Type t, Symbol symbol, bool tryReuse)
    {
        if (declaration == null && symbol != null)
            declaration = symbol.ValueDeclaration ?? symbol.Declarations?.FirstOrDefault();
        if (symbol == null)
            symbol = ch.getSymbolOfDeclaration(declaration);
        if (t == null)
        {
            if (symbol == null)
                t = Ast.IsVariableLike(declaration) ? ch.getTypeForVariableLikeDeclaration(declaration, false, Checker.CheckMode.Normal) : ch.errorType;
            else if (!ctx.enclosingSymbolTypes.TryGetValue(Ast.GetSymbolId(symbol), out t) || t == null)
            {
                if ((symbol.Flags & SymbolFlags.Accessor) != 0 && declaration.Kind == Kind.SetAccessor)
                    t = ch.instantiateType(ch.getWriteTypeOfSymbol(symbol), ctx.mapper);
                else if ((symbol.Flags & (SymbolFlags.TypeLiteral | SymbolFlags.Signature)) == 0)
                    t = ch.instantiateType(ch.getWidenedLiteralType(ch.getTypeOfSymbol(symbol)), ctx.mapper);
                else
                    t = ch.errorType;
            }
        }


        bool requiresAddingUndefined = declaration != null && (declaration.Kind is Kind.Parameter or Kind.PropertySignature or Kind.PropertyDeclaration) && ch.requiresAddingImplicitUndefined(declaration, symbol, ctx.enclosingDeclaration);
        if (requiresAddingUndefined && Ast.IsParameterDeclaration(declaration))
            t = ch.getOptionalType(t, false);
        var restoreFlags = saveRestoreFlags();
        if ((t.flags & TypeFlags.UniqueESSymbol) != 0 && t.symbol == symbol && (ctx.enclosingDeclaration == null || Core.Some(symbol.Declarations, d => Ast.GetSourceFileOfNode(d) == ctx.enclosingFile)))
            ctx.flags |= NodeBuilderFlags.AllowUniqueESSymbolType;
        Node result = null;
        bool reportedInferenceFallback = false;
        if (!isActivelyExpanding() && tryReuse && ctx.enclosingDeclaration != null && declaration != null &&
            (Ast.IsAccessor(declaration) || Ast.HasInferredType(declaration) && !Ast.NodeIsSynthesized(declaration) &&
                (t.objectFlags & ObjectFlags.RequiresWidening) == 0))
        {
            var remove = symbol != null ? addSymbolTypeToContext(symbol, t) : null;
            var pt = Ast.IsAccessor(declaration) ? pc.GetTypeOfAccessor(declaration) : pc.GetTypeOfDeclaration(declaration);
            if ((pt == null || pt.Kind == PseudoTypeKind.NoResult) && Ast.IsBinaryExpression(declaration) && symbol != null)
            {
                var annotated = symbol.Declarations?.FirstOrDefault(hasTypeAnnotation);
                if (annotated != null)
                    pt = pc.GetTypeOfDeclaration(annotated);
            }

            bool reportErrors = !ctx.suppressReportInferenceFallback;
            bool optionalAnnotated = !requiresAddingUndefined && (declaration.Kind is Kind.Parameter or Kind.PropertySignature or Kind.PropertyDeclaration) && Checker.isOptionalDeclaration(declaration);
            if (pseudoTypeEquivalentToType(pt, t, optionalAnnotated, reportErrors))
            {
                var ptt = pseudoTypeToType(pt);
                if (ptt != null && requiresAddingUndefined && Checker.containsNonMissingUndefinedType(ch, t) && !Checker.containsNonMissingUndefinedType(ch, ptt))
                    pt = PseudoType.NewPseudoTypeUnion([pt, PseudoType.PseudoTypeUndefined]);
                result = pseudoTypeToNodeWithCheckerFallback(pt, t);
            }
            else
            {
                reportedInferenceFallback = reportErrors && pt.Kind == PseudoTypeKind.Inferred && (pt.AsPseudoTypeInferred().ErrorNodes?.Count ?? 0) > 0;
                bool shouldAddUndefined = false;
                if (requiresAddingUndefined)
                {
                    var ptt = pseudoTypeToType(pt);
                    shouldAddUndefined = ptt != null ? !Checker.containsNonMissingUndefinedType(ch, ptt) : !PseudoChecker.CouldAlreadyReferToUndefinedType(pt);
                }

                if (shouldAddUndefined)
                {
                    pt = PseudoType.NewPseudoTypeUnion([pt, PseudoType.PseudoTypeUndefined]);
                    if (pseudoTypeEquivalentToType(pt, t, false, reportErrors))
                    {
                        result = pseudoTypeToNodeWithCheckerFallback(pt, t);
                        reportedInferenceFallback = false;
                    }
                }
            }

            remove?.Invoke();
        }

        if (result == null)
        {
            bool saved = ctx.suppressReportInferenceFallback;
            if (reportedInferenceFallback)
                ctx.suppressReportInferenceFallback = true;
            result = typeToTypeNode(t);
            ctx.suppressReportInferenceFallback = saved;
        }

        restoreFlags();
        return result ?? f.NewKeywordTypeNode(Kind.AnyKeyword);
    }

    // nodebuilderimpl.go:2234.
    private static bool hasTypeAnnotation(Node declaration) => declaration != null && declaration.Type() != null && !Ast.IsTypeAliasDeclaration(declaration) && !Ast.IsJSTypeAliasDeclaration(declaration);
    // nodebuilderimpl.go:2581. Finite data-property branch. All excluded cases
    // are guarded before assuming their declaration/context caches are absent.
    private Slice<Node> addPropertyToElementList(Symbol propertySymbol, Slice<Node> typeElements)
    {

        bool propertyIsReverseMapped = (propertySymbol.CheckFlags & CheckFlags.ReverseMapped) != 0;
        var propertyType = shouldUsePlaceholderForProperty(propertySymbol) ? ch.anyType : ch.getNonMissingTypeOfSymbol(propertySymbol);
        var saveEnclosingDeclaration = ctx.enclosingDeclaration;
        ctx.enclosingDeclaration = null;
        if (Checker.isLateBoundName(propertySymbol.Name))
        {
            if (propertySymbol.Declarations is { Count: > 0 })
            {
                var declaration = propertySymbol.Declarations[0];
                if (ch.hasLateBindableName(declaration))
                {
                    if (Ast.IsBinaryExpression(declaration))
                    {
                        var name = Ast.GetNameOfDeclaration(declaration);
                        if (name != null && Ast.IsElementAccessExpression(name) &&
                            Ast.IsPropertyAccessEntityNameExpression(name.AsElementAccessExpression().ArgumentExpression, false))
                            trackComputedName(name.AsElementAccessExpression().ArgumentExpression, saveEnclosingDeclaration);
                    }
                    else
                        trackComputedName(declaration.Name().Expression(), saveEnclosingDeclaration);
                }
            }
            else
                ctx.tracker.ReportNonSerializableProperty(ch.symbolToString(propertySymbol));
        }
        if (propertySymbol.ValueDeclaration != null)
            ctx.enclosingDeclaration = propertySymbol.ValueDeclaration;
        else if (propertySymbol.Declarations is { Count: > 0 } && propertySymbol.Declarations[0] != null)
            ctx.enclosingDeclaration = propertySymbol.Declarations[0];
        else
            ctx.enclosingDeclaration = saveEnclosingDeclaration;
        var propertyName = getPropertyNameNodeForSymbol(propertySymbol, saveEnclosingDeclaration);
        ctx.enclosingDeclaration = saveEnclosingDeclaration;
        approximateLength += Ast.SymbolName(propertySymbol).Length + 1;
        // nodebuilderimpl.go:2618. Separate accessor signatures preserve distinct
        // read/write types and original generic member instantiation.
        if ((propertySymbol.Flags & SymbolFlags.Accessor) != 0)
        {
            var writeType = ch.getWriteTypeOfSymbol(propertySymbol);
            if (!ch.isErrorType(propertyType) && !ch.isErrorType(writeType))
            {
                var propertyDeclaration = Ast.GetDeclarationOfKind(propertySymbol, Kind.PropertyDeclaration);
                bool classMember = propertySymbol.Parent != null && (propertySymbol.Parent.Flags & SymbolFlags.Class) != 0;
                if (propertyType != writeType || classMember && propertyDeclaration == null)
                {
                    var mapper = ch.valueSymbolLinks.Get(propertySymbol).mapper;
                    foreach (var kind in new[] { Kind.GetAccessor, Kind.SetAccessor })
                    {
                        var declaration = Ast.GetDeclarationOfKind(propertySymbol, kind);
                        if (declaration == null)
                            continue;
                        var signature = ch.getSignatureFromDeclaration(declaration);
                        if (mapper != null)
                            signature = ch.instantiateSignature(signature, mapper);
                        typeElements = typeElements.Append(signatureToSignatureDeclarationHelper(signature, kind, new() { name = propertyName }));
                    }
                    return typeElements;
                }
                if (classMember && propertyDeclaration != null && Core.Some(propertyDeclaration.ModifierNodes(), m => m.Kind == Kind.AccessorKeyword))
                {
                    var getter = ch.newSignature(SignatureFlags.None, null, null, null, null, propertyType, null, 0);
                    typeElements = typeElements.Append(signatureToSignatureDeclarationHelper(getter, Kind.GetAccessor, new() { name = propertyName }));
                    var parameter = ch.newSymbol(SymbolFlags.FunctionScopedVariable, "arg");
                    ch.valueSymbolLinks.Get(parameter).resolvedType = writeType;
                    var setter = ch.newSignature(SignatureFlags.None, null, null, null, new List<Symbol> { parameter }, ch.voidType, null, 0);
                    typeElements = typeElements.Append(signatureToSignatureDeclarationHelper(setter, Kind.SetAccessor, new() { name = propertyName }));
                    return typeElements;
                }
            }
        }

        var optionalToken = (propertySymbol.Flags & SymbolFlags.Optional) != 0 ? f.NewToken(Kind.QuestionToken) : null;
        if ((propertySymbol.Flags & (SymbolFlags.Function | SymbolFlags.Method)) != 0 && GoRuntime.Len(ch.getPropertiesOfObjectType(propertyType)) == 0 && !ch.isReadonlySymbol(propertySymbol))
        {
            var signatures = ch.getSignaturesOfType(ch.filterType(propertyType, t => (t.flags & TypeFlags.Undefined) == 0), SignatureKind.Call);
            foreach (var signature in signatures)
                typeElements = typeElements.Append(signatureToSignatureDeclarationHelper(signature, Kind.MethodSignature, new() { name = propertyName, questionToken = optionalToken }));
            if (GoRuntime.Len(signatures) != 0 || optionalToken == null)
                return typeElements;
        }

        Node typeNode;
        if (shouldUsePlaceholderForProperty(propertySymbol))
            typeNode = createElidedInformationPlaceholder();
        else
        {
            if (propertyIsReverseMapped)
                (ctx.reverseMappedStack ??= new()).Add(propertySymbol);
            typeNode = propertyType != null ? serializeTypeForDeclaration(null, propertyType, propertySymbol, true) : f.NewKeywordTypeNode(Kind.AnyKeyword);
            if (propertyIsReverseMapped)
                ctx.reverseMappedStack.RemoveAt(ctx.reverseMappedStack.Count - 1);
        }

        ModifierList modifiers = null;
        if (ch.isReadonlySymbol(propertySymbol))
        {
            modifiers = f.NewModifierList(Slice<Node>.FromArray([f.NewModifier(Kind.ReadonlyKeyword)]));
            approximateLength += 9;
        }

        var propertySignature = f.NewPropertySignatureDeclaration(modifiers, propertyName, optionalToken, typeNode, null);
        typeElements = typeElements.Append(propertySignature);
        setCommentRange(propertySignature, propertySymbol.ValueDeclaration);
        return typeElements;
    }

    // nodebuilderimpl.go:2151/2159. Prefer visible original computed components;
    // reuse failure falls through to the source index-signature producer.
    private bool isTriviallySerializableComputedName(Node element)
    {
        if (element == null || element.Name() == null || !Ast.IsComputedPropertyName(element.Name()) || !Ast.IsEntityNameExpression(element.Name().Expression()))
            return false;
        return ch.isEntityNameVisible(element.Name().Expression(), ctx.enclosingDeclaration, false).Accessibility == TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible;
    }

    private Slice<Node> indexInfoToObjectComputedNamesOrSignatureDeclaration(IndexInfo info, Node typeNode)
    {
        if (info.components is { Count: > 0 })
        {
            bool serializable = ctx.enclosingDeclaration != null && Core.Every(info.components, isTriviallySerializableComputedName);
            if (serializable)
            {
                var components = Core.Filter(info.components, component => !ch.hasLateBindableName(component));
                bool bailed = false;
                var results = Core.MapSlice(components, element =>
                {
                    var name = reuseNode(element.Name());
                    if (name == null)
                    {
                        bailed = true;
                        return null;
                    }
                    trackComputedName(element.Name().Expression(), ctx.enclosingDeclaration);
                    ModifierList modifiers = info.isReadonly ? f.NewModifierList(Slice<Node>.FromArray([f.NewModifier(Kind.ReadonlyKeyword)])) : null;
                    var postfixToken = element.PostfixToken()?.Clone(f);
                    var currentTypeNode = typeNode != null ? f.DeepCloneNode(typeNode) : typeToTypeNode(ch.getTypeOfSymbol(element.Symbol()));
                    var signature = f.NewPropertySignatureDeclaration(modifiers, name, postfixToken, currentTypeNode, null);
                    signature.Loc = element.Loc;
                    return signature;
                });
                if (!bailed)
                    return results;
            }
        }
        return Slice<Node>.FromArray([indexInfoToIndexSignatureDeclarationHelper(info, typeNode)]);
    }

    private Node indexInfoToIndexSignatureDeclarationHelper(IndexInfo info, Node typeNode)
    {
        string name = Checker.getNameFromIndexInfo(info);
        var keyNode = typeToTypeNode(info.keyType);
        var parameter = f.NewParameterDeclaration(null, null, newIdentifier(name, null), null, keyNode, null);
        typeNode ??= info.valueType == null ? f.NewKeywordTypeNode(Kind.AnyKeyword) : typeToTypeNode(info.valueType);
        if (info.valueType == null && (ctx.flags & NodeBuilderFlags.AllowEmptyIndexInfoType) == 0)
            encounteredError = true;
        approximateLength += name.Length + 4;
        ModifierList modifiers = null;
        if (info.isReadonly)
        {
            approximateLength += 9;
            modifiers = f.NewModifierList(Slice<Node>.FromArray([f.NewModifier(Kind.ReadonlyKeyword)]));
        }

        return f.NewIndexSignatureDeclaration(modifiers, f.NewNodeList(Slice<Node>.FromArray([parameter])), typeNode);
    }

    // nodebuilderimpl.go:2722. Original truncation/order loop retains ordinary
    // calls, nonabstract constructors and index/property information.
    private NodeList createTypeNodesFromResolvedType(StructuredType resolved)
    {
        if (checkTruncationLength())
        {
            if ((ctx.flags & NodeBuilderFlags.NoTruncation) != 0)
            {
                var element = f.NewNotEmittedTypeElement();
                return f.NewNodeList(Slice<Node>.FromArray([e.AddSyntheticTrailingComment(element,
                    Kind.MultiLineCommentTrivia, "elided", false)]));
            }
            return f.NewNodeList(Slice<Node>.FromArray([f.NewPropertySignatureDeclaration(null, f.NewIdentifier("..."), null, null, null)]));
        }
        Slice<Node> elements = default;
        foreach (var signature in resolved.CallSignaturesSlice())
            elements = elements.Append(signatureToSignatureDeclarationHelper(signature, Kind.CallSignature, null));
        foreach (var signature in resolved.ConstructSignaturesSlice())
            if ((signature.flags & SignatureFlags.Abstract) == 0)
                elements = elements.Append(signatureToSignatureDeclarationHelper(signature, Kind.ConstructSignature, null));
        foreach (var info in resolved.indexInfos ?? [])
        {
            var typeNode = (resolved.AsType().objectFlags & ObjectFlags.ReverseMapped) != 0 && (ctx.flags & NodeBuilderFlags.AllowAnonymousIdentifier) != 0 ? createElidedInformationPlaceholder() : null;
            elements = GoSlices.Concat(elements, indexInfoToObjectComputedNamesOrSignatureDeclaration(info, typeNode));
        }

        var properties = resolved.properties;
        if (properties == null || properties.Count == 0)
            return f.NewNodeList(elements);
        int i = 0;
        foreach (var property in properties)
        {
            i++;
            if (checkTruncationLength() && i + 2 < properties.Count - 1)
            {
                if ((ctx.flags & NodeBuilderFlags.NoTruncation) != 0)
                    elements[^1] = e.AddSyntheticTrailingComment(elements[^1],
                        Kind.MultiLineCommentTrivia, $"... {properties.Count - i} more elided ...", false);
                else
                    elements = elements.Append(f.NewPropertySignatureDeclaration(null, f.NewIdentifier($"... {properties.Count - i} more ..."), null, null, null));
                elements = addPropertyToElementList(properties[^1], elements);
                break;
            }

            elements = addPropertyToElementList(property, elements);
        }

        return elements is { Count: > 0 } ? f.NewNodeList(elements) : null;
    }

    // nodebuilderimpl.go:2789. Mapped, anonymous and internal callable/constructable object serialization.
    private Node createTypeNodeFromObjectType(Type t)
    {
        if (ch.isGenericMappedType(t) || ((t.objectFlags & ObjectFlags.Mapped) != 0 && t.AsMappedType().containsError))
            return createMappedTypeNodeFromType(t);
        var resolved = ch.resolveStructuredTypeMembers(t);
        var callSigs = resolved.CallSignaturesSlice();
        var ctorSigs = resolved.ConstructSignaturesSlice();
        if (GoRuntime.Len(resolved.properties) == 0 && GoRuntime.Len(resolved.indexInfos) == 0)
        {
            if (GoRuntime.Len(callSigs) == 0 && GoRuntime.Len(ctorSigs) == 0)
            {
                approximateLength += 2;
                var result = f.NewTypeLiteralNode(f.NewNodeList(Slice<Node>.Make(0)));
                e.SetEmitFlags(result, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                return result;
            }

            if (GoRuntime.Len(callSigs) == 1 && GoRuntime.Len(ctorSigs) == 0)
                return signatureToSignatureDeclarationHelper(callSigs[0], Kind.FunctionType, null);
            if (GoRuntime.Len(ctorSigs) == 1 && GoRuntime.Len(callSigs) == 0)
                return signatureToSignatureDeclarationHelper(ctorSigs[0], Kind.ConstructorType, null);
        }

        var abstractSignatures = Core.Filter(ctorSigs, signature => (signature.flags & SignatureFlags.Abstract) != 0);
        if (GoRuntime.Len(abstractSignatures) > 0)
        {
            var types = Core.Map(abstractSignatures, ch.getOrCreateTypeFromSignature);
            int count = GoRuntime.Len(callSigs) + GoRuntime.Len(ctorSigs) - abstractSignatures.Length + GoRuntime.Len(resolved.indexInfos) + ((ctx.flags & NodeBuilderFlags.WriteClassExpressionAsTypeLiteral) != 0 ? Core.CountWhere(resolved.properties, property => (property.Flags & SymbolFlags.Prototype) == 0) : GoRuntime.Len(resolved.properties));
            if (count != 0)
                types.Add(getResolvedTypeWithoutAbstractConstructSignatures(resolved));
            return typeToTypeNode(ch.getIntersectionType(types));
        }

        var restoreFlags = saveRestoreFlags();
        ctx.flags |= NodeBuilderFlags.InObjectTypeLiteral;
        var members = createTypeNodesFromResolvedType(resolved);
        restoreFlags();
        var node = f.NewTypeLiteralNode(members);
        approximateLength += 2;
        e.SetEmitFlags(node, (ctx.flags & NodeBuilderFlags.MultilineObjectLiterals) != 0 ? 0 : TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
        return node;
    }

    // nodebuilderimpl.go:2897/2912. Anonymous records and callable objects retain mapping,
    // cycles, reverse-mapped forms, instantiation expressions and alias fallback.
    private Node createAnonymousTypeNode(Type t) => createAnonymousTypeNodeEx(t, false, false);
    private Node createAnonymousTypeNodeEx(Type t, bool forceClassExpansion, bool forceExpansion)
    {
        var typeId = t.id;
        var symbol = t.symbol;
        if (symbol != null)
        {
            if ((t.objectFlags & ObjectFlags.InstantiationExpressionType) != 0)
            {
                var existing = t.AsInstantiationExpressionType().node;
                // A constituent can retain a typeof node belonging to the whole type;
                // reuse it only when its actual type is this instance.
                if (Ast.IsTypeQueryNode(existing) && getTypeFromTypeNode(existing, false) == t)
                {
                    if (visitedTypes.Contains(typeId))
                        return createCyclicStructurePlaceholder();
                    visitedTypes.Add(typeId);
                    var typeNode = tryReuseExistingNonParameterTypeNode(existing, t, null, null);
                    visitedTypes.Remove(typeId);
                    if (typeNode != null)
                        return typeNode;
                }
                if (visitedTypes.Contains(typeId))
                    return createCyclicStructurePlaceholder();
                return visitAndTransformType(t, createTypeNodeFromObjectType);
            }
            var instanceSide = isClassInstanceSide(ch, t) ? SymbolFlags.Type : SymbolFlags.Value;
            var(emitTypeOf, emittedSymbol) = shouldEmitTypeOfSymbol(forceExpansion, forceClassExpansion, instanceSide, symbol, typeId);
            if (emitTypeOf)
            {
                if (shouldExpandType(t, false))
                    ctx.depth++;
                else
                    return symbolToTypeNode(emittedSymbol, instanceSide, null);
            }

            if (visitedTypes.Contains(typeId))
            {
                var alias = getTypeAliasForTypeLiteral(ch, t);
                return alias != null ? symbolToTypeNode(alias, SymbolFlags.Type, null) : createCyclicStructurePlaceholder();
            }

            return visitAndTransformType(t, createTypeNodeFromObjectType);
        }

        if ((t.objectFlags & ObjectFlags.ReverseMapped) != 0 && (ctx.flags & NodeBuilderFlags.AllowAnonymousIdentifier) == 0)
        {
            if (visitedTypes.Contains(typeId))
                return createCyclicStructurePlaceholder();
            return visitAndTransformType(t, createTypeNodeFromObjectType);
        }

        return createTypeNodeFromObjectType(t);
    }

    // nodebuilderimpl.go:2859/2901. Ordinary nonlocal function symbols and recursive
    // callable types retain typeof selection; class/static method paths remain excluded.
    private (bool result, Symbol symbol) shouldWriteTypeOfFunctionSymbol(Symbol symbol, uint typeId)
    {
        bool isStaticMethodSymbol = (symbol.Flags & SymbolFlags.Method) != 0 && TypeScript.Scanner.Scanner.IsIdentifierTextByteString(symbol.Name, LanguageVariant.Standard) && Core.Some(symbol.Declarations, declaration =>
        {
            return Ast.IsStatic(declaration) && !ch.isLateBindableIndexSignature(Ast.GetNameOfDeclaration(declaration));
        });
        bool isNonLocalFunctionSymbol = false, isFunctionExpressionSymbol = false;
        if ((symbol.Flags & SymbolFlags.Function) != 0)
        {
            if (symbol.Parent != null)
                isNonLocalFunctionSymbol = true;
            else
                foreach (var declaration in symbol.Declarations ?? [])
                {
                    if (declaration.Parent.Kind is Kind.SourceFile or Kind.ModuleBlock)
                    {
                        isNonLocalFunctionSymbol = true;
                        break;
                    }

                    if (Ast.IsFunctionExpressionOrArrowFunction(declaration) && Ast.IsVariableDeclaration(declaration.Parent) && Ast.IsVariableDeclarationList(declaration.Parent.Parent) && Ast.IsVariableStatement(declaration.Parent.Parent.Parent) && declaration.Parent.Parent.Parent.Parent != null && declaration.Parent.Parent.Parent.Parent.Kind is Kind.SourceFile or Kind.ModuleBlock)
                    {
                        isNonLocalFunctionSymbol = true;
                        isFunctionExpressionSymbol = true;
                        break;
                    }
                }
        }

        if (isStaticMethodSymbol || isNonLocalFunctionSymbol)
        {
            if (isFunctionExpressionSymbol && symbol.ValueDeclaration?.Parent != null && symbol.ValueDeclaration.Parent != ctx.enclosingDeclaration)
                symbol = ch.getMergedSymbol(symbol.ValueDeclaration.Parent.Symbol());
            return ((ctx.flags & NodeBuilderFlags.UseTypeOfFunction) != 0 || visitedTypes.Contains(typeId)) && ((ctx.flags & NodeBuilderFlags.UseStructuralFallback) == 0 || ch.IsValueSymbolAccessible(symbol, ctx.enclosingDeclaration)) ? (true, symbol) : (false, symbol);
        }

        return (false, symbol);
    }

    private (bool result, Symbol symbol) shouldEmitTypeOfSymbol(bool forceExpansion, bool forceClassExpansion, SymbolFlags isInstanceType, Symbol symbol, uint typeId)
    {
        if (forceExpansion)
            return (false, symbol);
        bool classReference = (symbol.Flags & SymbolFlags.Class) != 0 && !forceClassExpansion &&
            ch.getBaseTypeVariableOfClass(symbol) == null &&
            !(symbol.ValueDeclaration != null && Ast.IsClassLike(symbol.ValueDeclaration) &&
              (ctx.flags & NodeBuilderFlags.WriteClassExpressionAsTypeLiteral) != 0 &&
              (!Ast.IsClassDeclaration(symbol.ValueDeclaration) ||
               ch.IsSymbolAccessible(symbol, ctx.enclosingDeclaration, isInstanceType, false).Accessibility !=
                   TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible));
        if (classReference || (symbol.Flags & (SymbolFlags.Enum | SymbolFlags.ValueModule)) != 0)
            return (true, symbol);
        return shouldWriteTypeOfFunctionSymbol(symbol, typeId);
    }

    // nodebuilderimpl.go:2849.
    private static Symbol getTypeAliasForTypeLiteral(Checker checker, Type t)
    {
        if (t.symbol != null && (t.symbol.Flags & SymbolFlags.TypeLiteral) != 0 && t.symbol.Declarations != null)
        {
            var node = Ast.WalkUpParenthesizedTypes(t.symbol.Declarations[0].Parent);
            if (Ast.IsTypeAliasDeclaration(node))
                return checker.getSymbolOfDeclaration(node);
        }

        return null;
    }

    // nodebuilderimpl.go:3234. Selected diagnostic contexts retain original cache
    // reads/writes, ordered infer parameters, array-cycle identities and reverse-mapped
    // declaration depth. External tracker APIs and configured hover verbosity remain excluded.
    private Node visitAndTransformType(Type t, Func<Type, Node> transform)
    {
        if (checkTruncationLength())
            return createElidedInformationPlaceholder();
        uint typeId = t.id;
        bool isArrayOrTuple = ch.isArrayOrTupleType(t);
        if (isArrayOrTuple)
            typeId = ch.createTypeReference(t.Target(), ch.getTypeArguments(t)).id;
        if (visitedTypes.Contains(typeId))
            return createCyclicStructurePlaceholder();
        var key = new CompositeTypeCacheIdentity(typeId, useFullyQualifiedType, inObjectTypeLiteral, ctx.flags, ctx.internalFlags, GoRuntime.Len(ctx.inferTypeParameters) != 0 ? Checker.getTypeListKey(ctx.inferTypeParameters) : default);
        if (ctx.enclosingDeclaration != null && links.TryGetValue(ctx.enclosingDeclaration, out var existingLinks) && existingLinks.serializedTypes?.TryGetValue(key, out var cachedResult) == true)
        {
            foreach (var arg in cachedResult.trackedSymbols ?? [])
                ctx.tracker.TrackSymbol(arg.symbol, arg.enclosingDeclaration, arg.meaning);
            if (cachedResult.truncating)
                truncating = true;
            approximateLength += cachedResult.addedLength;
            return f.DeepCloneNode(cachedResult.node);
        }

        bool isConstructorObject = (t.objectFlags & ObjectFlags.Anonymous) != 0 && t.symbol != null &&
            (t.symbol.Flags & SymbolFlags.Class) != 0;
        CompositeSymbolIdentity? symbolId = isArrayOrTuple ? null :
            (t.objectFlags & ObjectFlags.Reference) != 0 && t.AsTypeReference().node != null ?
                new(false, 0, Ast.GetNodeId(t.AsTypeReference().node)) :
            (t.flags & TypeFlags.Conditional) != 0 ?
                new(false, 0, Ast.GetNodeId(t.AsConditionalType().root.node.AsNode())) :
            t.symbol != null ? new(isConstructorObject, Ast.GetSymbolId(t.symbol), 0) : null;
        CompositeSymbolIdentity? reverseOrigin = null;
        int reverseDepth = 0;
        if ((t.objectFlags & ObjectFlags.ReverseMapped) != 0)
        {
            reverseOrigin = new(false, 0, Ast.GetNodeId(t.AsReverseMappedType().mappedType.AsMappedType().declaration.AsNode()));
            symbolDepth.TryGetValue(reverseOrigin.Value, out reverseDepth);
            if (reverseDepth >= 100)
            {
                truncating = true;
                return createElidedInformationPlaceholder();
            }

            symbolDepth[reverseOrigin.Value] = reverseDepth + 1;
        }

        try
        {
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
            var previousTrackedSymbols = ctx.trackedSymbols;
            ctx.trackedSymbols = null;
            int startLength = approximateLength;
            var result = transform(t);
            int addedLength = approximateLength - startLength;
            if (!ctx.reportedDiagnostic && !encounteredError)
            {
                var cell = getLinks(ctx.enclosingDeclaration);
                (cell.serializedTypes ??= new())[key] = new(result, truncating, addedLength, ctx.trackedSymbols);
            }

            visitedTypes.Remove(typeId);
            if (symbolId is { } restoreId)
                symbolDepth[restoreId] = depth;
            ctx.trackedSymbols = previousTrackedSymbols;
            return result;
        }
        finally
        {
            if (reverseOrigin is { } origin)
                symbolDepth[origin] = reverseDepth;
        }
    }

    // nodebuilderimpl.go:508. JSDoc intended types cannot reuse their original syntax.
    private bool canReuseExistingJSTypeNode(Node existing, Type t) => ch.getIntendedTypeFromJSDocTypeReference(existing) == null && existingTypeNodeIsNotReferenceOrIsReferenceWithCompatibleTypeArgumentCount(existing, t);
    // nodebuilderimpl.go:493/549. Preserve optional annotation equivalence and
    // the ordinary recovery-boundary visitor when reusing an existing type node.
    private bool typeNodeIsEquivalentToType(Node annotatedDeclaration, Type t, Type typeFromTypeNode)
    {
        if (typeFromTypeNode == t)
            return true;
        if (annotatedDeclaration == null)
            return false;
        return Checker.isOptionalDeclaration(annotatedDeclaration) && ch.getTypeWithFacts(t, Checker.TypeFacts.NEUndefined) == typeFromTypeNode;
    }

    private Node tryReuseExistingNonParameterTypeNode(Node existing, Type t, Node host, Type annotationType)
    {
        host ??= ctx.enclosingDeclaration;
        annotationType ??= getTypeFromTypeNode(existing, true);
        if (annotationType != null && typeNodeIsEquivalentToType(host, t, annotationType) && canReuseExistingJSTypeNode(existing, t))
        {
            var result = tryReuseExistingNodeHelper(existing);
            if (result != null)
                return result;
        }
        return null;
    }

    private bool existingTypeNodeIsNotReferenceOrIsReferenceWithCompatibleTypeArgumentCount(Node existing, Type t)
    {
        if ((t.objectFlags & ObjectFlags.Reference) == 0)
            return true;
        if (existing.Kind != Kind.TypeReference)
            return true;
        var symbol = tryGetResolvedSymbolFromTypeNode(existing);
        if (symbol == null)
            return true;
        var target = ch.getDeclaredTypeOfSymbol(symbol);
        if (target == null || target != t.AsTypeReference().target)
            return true;
        var parameters = t.AsTypeReference().target.AsInterfaceType().TypeParameters();
        return (existing.TypeArguments().Count) >= ch.getMinTypeArgumentCount(parameters);
    }

    // nodebuilderimpl.go:486. Original syntax comments are reusable only within the enclosing file.
    private void setCommentRange(Node node, Node range)
    {
        if (range != null && ctx.enclosingFile != null && ctx.enclosingFile == Ast.GetSourceFileOfNode(range))
            e.AssignCommentRange(node, range);
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

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1647:1.
    internal Node typePredicateToTypePredicateNode(TypePredicate predicate)
    {
        Node assertsModifier = default;
        if (((predicate.kind == TypePredicateKind.AssertsIdentifier) || (predicate.kind == TypePredicateKind.AssertsThis)))
        {
            assertsModifier = this.f.NewToken(Kind.AssertsKeyword);
        }

        Node parameterName = default;
        if (((predicate.kind == TypePredicateKind.Identifier) || (predicate.kind == TypePredicateKind.AssertsIdentifier)))
        {
            parameterName = this.f.NewIdentifier(predicate.parameterName);
            this.e.AddEmitFlags(parameterName, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
        }
        else
        {
            parameterName = this.f.NewThisTypeNode();
        }

        Node typeNode = default;
        if ((predicate.t != null))
        {
            typeNode = this.typeToTypeNode(predicate.t);
        }

        return this.f.NewTypePredicateNode(assertsModifier, parameterName, typeNode);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1716:1.
    private static Node getEffectiveParameterDeclaration(Symbol symbol)
    {
        var parameterDeclaration = Ast.GetDeclarationOfKind(symbol, Kind.Parameter);
        if ((parameterDeclaration != null))
        {
            return parameterDeclaration;
        }

        if (((symbol.Flags & SymbolFlags.Transient) == 0))
        {
            return Ast.GetDeclarationOfKind(symbol, Kind.JSDocParameterTag);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1727:1.
    private Node symbolToParameterDeclaration(Symbol parameterSymbol, bool preserveModifierFlags)
    {
        var parameterDeclaration = getEffectiveParameterDeclaration(parameterSymbol);
        var parameterType = this.ch.getTypeOfSymbol(parameterSymbol);
        var parameterTypeNode = this.serializeTypeForDeclaration(parameterDeclaration, parameterType, parameterSymbol, true);
        ModifierList modifiers = default;
        if ((((((this.ctx.flags & NodeBuilderFlags.OmitParameterModifiers) == 0) && preserveModifierFlags) && (parameterDeclaration != null)) && Ast.CanHaveModifiers(parameterDeclaration)))
        {
            var originals = Core.Filter(parameterDeclaration.ModifierNodes(), Ast.IsModifier);
            var clones = Core.MapSlice(originals, (node) =>
            {
                return node.Clone(this.f);
            });
            if ((GoRuntime.Len(clones) > 0))
            {
                modifiers = this.f.NewModifierList(clones);
            }
        }

        var isRest = (((parameterDeclaration != null) && Checker.isRestParameter(parameterDeclaration)) || ((parameterSymbol.CheckFlags & CheckFlags.RestParameter) != 0));
        Node dotDotDotToken = default;
        if (isRest)
        {
            dotDotDotToken = f.NewToken(Kind.DotDotDotToken);
        }

        var name = this.parameterToParameterDeclarationName(parameterSymbol, parameterDeclaration);
        var isOptional = (((parameterDeclaration != null) && this.ch.isOptionalParameter(parameterDeclaration)) || ((parameterSymbol.CheckFlags & CheckFlags.OptionalParameter) != 0));
        Node questionToken = default;
        if (isOptional)
        {
            questionToken = this.f.NewToken(Kind.QuestionToken);
        }

        var parameterNode = this.f.NewParameterDeclaration(modifiers, dotDotDotToken, name, questionToken, parameterTypeNode, null);
        this.ctx.approximateLength += (GoRuntime.Len(parameterSymbol.Name) + 3);
        return parameterNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1764:1.
    private Node parameterToParameterDeclarationName(Symbol parameterSymbol, Node parameterDeclaration)
    {
        if (((parameterDeclaration == null) || (parameterDeclaration.Name() == null)))
        {
            return this.newIdentifier(parameterSymbol.Name, parameterSymbol);
        }

        var name = parameterDeclaration.Name();
        switch (name.Kind)
        {
            case Kind.Identifier:
            {
                var cloned = this.f.DeepCloneNode(name);
                this.e.SetEmitFlags(cloned, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                this.idToSymbol[cloned] = parameterSymbol;
                return cloned;
            }

            case Kind.QualifiedName:
            {
                var cloned2 = this.f.DeepCloneNode(name.AsQualifiedName().Right);
                this.e.SetEmitFlags(cloned2, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                this.idToSymbol[cloned2] = parameterSymbol;
                return cloned2;
            }

            default:
            {
                return cloneBindingName(name);
            }
        }
    }

    // nodebuilderimpl.go:1786. Display binding names without runtime initializers.
    private Node cloneBindingName(Node node)
    {
        if (Ast.IsComputedPropertyName(node) && ch.isLateBindableName(node))
            trackComputedName(node.Expression(), ctx.enclosingDeclaration);
        var visited = cloneBindingNameVisitor.VisitEachChild(node);
        if (Ast.IsBindingElement(visited))
        {
            var element = visited.AsBindingElement();
            visited = f.UpdateBindingElement(element, element.DotDotDotToken, element.PropertyName, element.Name(), null);
        }
        if (!Ast.NodeIsSynthesized(visited))
            visited = f.DeepCloneNode(visited);
        e.SetEmitFlags(visited, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine | TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
        return visited;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1818:1.
    private Node serializeInferredReturnTypeForSignature(Signature signature, Type returnType)
    {
        var oldSuppressReportInferenceFallback = this.ctx.suppressReportInferenceFallback;
        this.ctx.suppressReportInferenceFallback = true;
        var typePredicate = this.ch.getTypePredicateOfSignature(signature);
        Node returnTypeNode = default;
        if ((typePredicate != null))
        {
            TypePredicate predicate = default;
            if ((this.ctx.mapper != null))
            {
                predicate = this.ch.instantiateTypePredicate(typePredicate, this.ctx.mapper);
            }
            else
            {
                predicate = typePredicate;
            }

            returnTypeNode = this.typePredicateToTypePredicateNodeHelper(predicate);
        }
        else
        {
            returnTypeNode = this.typeToTypeNode(returnType);
        }

        this.ctx.suppressReportInferenceFallback = oldSuppressReportInferenceFallback;
        return returnTypeNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1838:1.
    private Node typePredicateToTypePredicateNodeHelper(TypePredicate typePredicate)
    {
        Node assertsModifier = default;
        if (((typePredicate.kind == TypePredicateKind.AssertsThis) || (typePredicate.kind == TypePredicateKind.AssertsIdentifier)))
        {
            assertsModifier = this.f.NewToken(Kind.AssertsKeyword);
        }
        else
        {
            assertsModifier = null;
        }

        Node parameterName = default;
        if (((typePredicate.kind == TypePredicateKind.Identifier) || (typePredicate.kind == TypePredicateKind.AssertsIdentifier)))
        {
            parameterName = this.newIdentifier(typePredicate.parameterName, null);
            this.e.SetEmitFlags(parameterName, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
        }
        else
        {
            parameterName = this.f.NewThisTypeNode();
        }

        Node typeNode = default;
        if ((typePredicate.t != null))
        {
            typeNode = this.typeToTypeNode(typePredicate.t);
        }

        return this.f.NewTypePredicateNode(assertsModifier, parameterName, typeNode);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1865:1.
    internal Node signatureToSignatureDeclarationHelper(Signature signature, Kind kind, SignatureToSignatureDeclarationOptions options)
    {
        Slice<Node> typeParameters = default;
        var tuple1 = this.enterSignatureScope(signature);
        var expandedParams = tuple1.Item1;
        var cleanup = tuple1.Item2;
        this.ctx.approximateLength += 3;
        if ((ctx.flags & NodeBuilderFlags.WriteTypeArgumentsOfSignature) != 0 && signature.target != null && signature.mapper != null && GoRuntime.Len(signature.target.typeParameters) != 0)
            foreach (var parameter in signature.target.typeParameters)
                typeParameters = GoRuntime.Append(typeParameters, typeToTypeNode(ch.instantiateType(parameter, signature.mapper)));
        else
            foreach (var parameter in signature.typeParameters ?? [])
                typeParameters = GoRuntime.Append(typeParameters, typeParameterToDeclaration(parameter));
        var restoreFlags = this.saveRestoreFlags();
        this.ctx.flags &= ~(NodeBuilderFlags.SuppressAnyReturnType);
        var parameters = Core.MapSlice(Core.IfElse(Core.Some(expandedParams, (p) =>
        {
            return ((p != expandedParams[(GoRuntime.Len(expandedParams) - 1)]) && ((p.CheckFlags & CheckFlags.RestParameter) != 0));
        }), signature.parameters, expandedParams), (parameter3) =>
        {
            return this.symbolToParameterDeclaration(parameter3, (kind == Kind.Constructor));
        });
        Node thisParameter = default;
        if (((this.ctx.flags & NodeBuilderFlags.OmitThisParameter) != 0))
        {
            thisParameter = null;
        }
        else
        {
            thisParameter = this.tryGetThisParameterDeclaration(signature);
        }

        if ((thisParameter != null))
        {
            parameters = GoRuntime.Append(Slice<Node>.FromArray([thisParameter]), parameters);
        }

        restoreFlags();
        var returnTypeNode = this.serializeReturnTypeForSignature(signature, true);
        Slice<Node> modifiers = default;
        if ((options != null))
        {
            modifiers = options.modifiers;
        }

        if ((((kind == Kind.ConstructorType)) && ((signature.flags & SignatureFlags.Abstract) != 0)))
        {
            var flags = Ast.ModifiersToFlags(modifiers);
            modifiers = Ast.CreateModifiersFromModifierFlags((flags | ModifierFlags.Abstract), this.f.NewModifier);
        }

        var paramList = this.f.NewNodeList(parameters);
        NodeList typeParamList = default;
        if ((GoRuntime.Len(typeParameters) != 0))
        {
            typeParamList = this.f.NewNodeList(typeParameters);
        }

        ModifierList modifierList = default;
        if ((GoRuntime.Len(modifiers) > 0))
        {
            modifierList = this.f.NewModifierList(modifiers);
        }

        Node name = default;
        if ((options != null))
        {
            name = options.name;
        }

        if ((name == null))
        {
            name = this.f.NewIdentifier("");
        }

        Node node = default;
        switch (true)
        {
            case true when (kind == Kind.CallSignature):
            {
                node = this.f.NewCallSignatureDeclaration(typeParamList, paramList, returnTypeNode);
                break;
            }

            case true when (kind == Kind.ConstructSignature):
            {
                node = f.NewConstructSignatureDeclaration(typeParamList, paramList, returnTypeNode);
                break;
            }

            case true when (kind == Kind.MethodSignature):
            {
                Node questionToken = default;
                if ((options != null))
                {
                    questionToken = options.questionToken;
                }

                node = this.f.NewMethodSignatureDeclaration(modifierList, name, questionToken, typeParamList, paramList, returnTypeNode);
                break;
            }

            case true when (kind == Kind.MethodDeclaration):
            {
                node = this.f.NewMethodDeclaration(modifierList, null, name, null, typeParamList, paramList, returnTypeNode, null, null);
                break;
            }

            case true when (kind == Kind.Constructor):
            {
                node = f.NewConstructorDeclaration(modifierList, null, paramList, null, null, null);
                break;
            }

            case true when (kind == Kind.GetAccessor):
            {
                node = f.NewGetAccessorDeclaration(modifierList, name, null, paramList, returnTypeNode, null, null);
                break;
            }

            case true when (kind == Kind.SetAccessor):
            {
                node = f.NewSetAccessorDeclaration(modifierList, name, null, paramList, null, null, null);
                break;
            }

            case true when (kind == Kind.IndexSignature):
            {
                node = this.f.NewIndexSignatureDeclaration(modifierList, paramList, returnTypeNode);
                break;
            }

            case true when (kind == Kind.FunctionType):
            {
                if ((returnTypeNode == null))
                {
                    returnTypeNode = this.f.NewTypeReferenceNode(this.f.NewIdentifier(""), null);
                }

                node = this.f.NewFunctionTypeNode(typeParamList, paramList, returnTypeNode);
                break;
            }

            case true when (kind == Kind.ConstructorType):
            {
                returnTypeNode ??= f.NewTypeReferenceNode(f.NewIdentifier(""), null);
                node = f.NewConstructorTypeNode(modifierList, typeParamList, paramList, returnTypeNode);
                break;
            }

            case true when (kind == Kind.FunctionDeclaration):
            {
                node = this.f.NewFunctionDeclaration(modifierList, null, name, typeParamList, paramList, returnTypeNode, null, null);
                break;
            }

            case true when (kind == Kind.FunctionExpression):
            {
                node = this.f.NewFunctionExpression(modifierList, null, name, typeParamList, paramList, returnTypeNode, null, this.f.NewBlock(this.f.NewNodeList(Slice<Node>.Make(0)), false));
                break;
            }

            case true when (kind == Kind.ArrowFunction):
            {
                node = this.f.NewArrowFunction(modifierList, typeParamList, paramList, returnTypeNode, null, null, this.f.NewBlock(this.f.NewNodeList(Slice<Node>.Make(0)), false));
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled kind in signatureToSignatureDeclarationHelper");
            }
        }

        cleanup();
        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:2073:1.
    private Node tryGetThisParameterDeclaration(Signature signature)
    {
        if ((signature.thisParameter != null))
        {
            return this.symbolToParameterDeclaration(signature.thisParameter, false);
        }

        // nodebuilderimpl.go:2073: the pinned JS/JSDoc this-tag fallback is a TODO,
        // so a signature without an actual thisParameter returns nil in all files.
        return null;
    }

    // *
    // * Serializes the return type of the signature by first trying to use the syntactic printer if possible and falling back to the checker type if not.
    //
    // Upstream upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:2096:1.
    private Node serializeReturnTypeForSignature(Signature signature, bool tryReuse)
    {
        var suppressAny = ((this.ctx.flags & NodeBuilderFlags.SuppressAnyReturnType) != 0);
        var restoreFlags = this.saveRestoreFlags();
        if (suppressAny)
        {
            this.ctx.flags &= ~(NodeBuilderFlags.SuppressAnyReturnType);
        }

        Node returnTypeNode = default;
        Type returnType = default;
        if (((signature.declaration != null) && !Ast.NodeIsSynthesized(signature.declaration)))
        {
            var symbol = this.ch.getSymbolOfDeclaration(signature.declaration);
            var ok = this.ctx.enclosingSymbolTypes.TryGetValue(Ast.GetSymbolId(symbol), out returnType);
            if ((!ok || (returnType == null)))
            {
                returnType = this.ch.instantiateType(this.ch.getReturnTypeOfSignature(signature), this.ctx.mapper);
            }
        }
        else
        {
            returnType = this.ch.getReturnTypeOfSignature(signature);
        }

        if (!((suppressAny && Checker.IsTypeAny(returnType))))
        {
            if (((((!this.isActivelyExpanding() && tryReuse) && (this.ctx.enclosingDeclaration != null)) && (signature.declaration != null)) && !Ast.NodeIsSynthesized(signature.declaration)))
            {
                var declarationSymbol = this.ch.getSymbolOfDeclaration(signature.declaration);
                var restore = this.addSymbolTypeToContext(declarationSymbol, returnType);
                var pt = this.pc.GetReturnTypeOfSignature(signature.declaration);
                if (this.pseudoTypeEquivalentToType(pt, returnType, false, !this.ctx.suppressReportInferenceFallback))
                {
                    var typePredicate = this.ch.getTypePredicateOfSignature(signature);
                    if (((typePredicate != null) && !this.pseudoReturnTypeMatchesPredicate(pt, typePredicate)))
                    {
                        if (!this.ctx.suppressReportInferenceFallback)
                        {
                            this.ctx.tracker.ReportInferenceFallback(signature.declaration);
                        }

                        pt = null;
                    }

                    if ((pt != null))
                    {
                        returnTypeNode = this.pseudoTypeToNodeWithCheckerFallback(pt, returnType);
                    }
                }

                restore();
            }

            if ((returnTypeNode == null))
            {
                returnTypeNode = this.serializeInferredReturnTypeForSignature(signature, returnType);
            }
        }

        if (((returnTypeNode == null) && !suppressAny))
        {
            returnTypeNode = this.f.NewKeywordTypeNode(Kind.AnyKeyword);
        }

        restoreFlags();
        return returnTypeNode;
    }

    // nodebuilderimpl.go:1859.
    internal sealed class SignatureToSignatureDeclarationOptions
    {
        internal Slice<Node> modifiers = default;
        internal Node name = null, questionToken = null;
    }

    // nodebuilderimpl.go:184/168. Default diagnostic verbosity has no active expansion.
    private bool isActivelyExpanding() => ctx.maxExpansionDepth > 0 && ctx.depth < ctx.maxExpansionDepth;
    private Action saveRestoreFlags()
    {
        var saved = ctx.flags;
        var savedInternal = ctx.internalFlags;
        var savedDepth = ctx.depth;
        return () =>
        {
            ctx.flags = saved;
            ctx.internalFlags = savedInternal;
            ctx.depth = savedDepth;
        };
    }
}

public sealed partial class Checker
{
    // nodebuilderimpl.go:1985. No tuple/rest expansion is admitted.
    internal List<List<Symbol>> getExpandedParameters(Signature sig, bool skipUnionExpanding)
    {
        if (signatureHasRestParameter(sig))
        {
            var restIndex = (GoRuntime.Len(sig.parameters) - 1);
            var restSymbol = sig.parameters[restIndex];
            var restType = this.getTypeOfSymbol(restSymbol);
            Func<Type, Symbol, List<string>> getUniqAssociatedNamesFromTupleType = (t, restSymbol2) =>
            {
                var names = Core.MapIndex(t.Target().AsTupleType().elementInfos, (info, i) =>
                {
                    return this.getTupleElementLabel(info, restSymbol2, i);
                });
                if ((GoRuntime.Len(names) > 0))
                {
                    var duplicates = new List<int>
                    {
                    };
                    var uniqueNames = new Dictionary<string, bool>();
                    {
                        foreach (var range7 in GoRuntime.Range(names))
                        {
                            var i2 = range7.Key;
                            var name = range7.Value;
                            var ok = uniqueNames.TryGetValue(name, out var _);
                            if (ok)
                            {
                                duplicates = GoRuntime.Append(duplicates, i2);
                            }
                            else
                            {
                                uniqueNames[name] = true;
                            }
                        }
                    }

                    var counters = new Dictionary<string, int>();
                    {
                        foreach (var range8 in GoRuntime.Range(duplicates))
                        {
                            var _2 = range8.Key;
                            var i3 = range8.Value;
                            var ok2 = counters.TryGetValue(names[i3], out var counter);
                            if (!ok2)
                            {
                                counter = 1;
                            }

                            string name2 = "";
                            while (true)
                            {
                                name2 = (names[i3] + "_" + Strconv.Itoa(counter));
                                var ok3 = uniqueNames.TryGetValue(name2, out var _3);
                                if (ok3)
                                {
                                    counter++;
                                    continue;
                                }
                                else
                                {
                                    uniqueNames[name2] = true;
                                    break;
                                }
                            }

                            names[i3] = name2;
                            counters[names[i3]] = (counter + 1);
                        }
                    }
                }

                return names;
            };
            Func<Type, int, Symbol, List<Symbol>> expandSignatureParametersWithTupleMembers = (restType2, restIndex2, restSymbol3) =>
            {
                var elementTypes = this.getTypeArguments(restType2);
                var associatedNames = getUniqAssociatedNamesFromTupleType(restType2, restSymbol3);
                var restParams = Core.MapIndex(elementTypes, (t2, i4) =>
                {
                    var name3 = associatedNames[i4];
                    var flags = restType2.Target().AsTupleType().elementInfos[i4].flags;
                    CheckFlags checkFlags = default;
                    switch (true)
                    {
                        case true when ((flags & ElementFlags.Variable) != 0):
                        {
                            checkFlags = CheckFlags.RestParameter;
                            break;
                        }

                        case true when ((flags & ElementFlags.Optional) != 0):
                        {
                            checkFlags = CheckFlags.OptionalParameter;
                            break;
                        }
                    }

                    var symbol = this.newSymbolEx(SymbolFlags.FunctionScopedVariable, name3, checkFlags);
                    var links = this.valueSymbolLinks.Get(symbol);
                    if (((flags & ElementFlags.Rest) != 0))
                    {
                        links.resolvedType = this.createArrayType(t2);
                    }
                    else
                    {
                        links.resolvedType = t2;
                    }

                    return symbol;
                });
                return Core.Concatenate(GoRuntime.Slice(sig.parameters, 0, restIndex2), restParams);
            };
            if (isTupleType(restType))
            {
                return new List<List<Symbol>>
                {
                    expandSignatureParametersWithTupleMembers(restType, restIndex, restSymbol)
                };
            }
            else
            {
                if (((!skipUnionExpanding && ((restType.flags & TypeFlags.Union) != 0)) && Core.Every(restType.AsUnionType().types, isTupleType)))
                {
                    return Core.Map(restType.AsUnionType().types, (t3) =>
                    {
                        return expandSignatureParametersWithTupleMembers(t3, restIndex, restSymbol);
                    });
                }
            }
        }

        return new List<List<Symbol>>
        {
            sig.parameters
        };
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node typeParameterToDeclarationWithConstraint(Type typeParameter, Node constraintNode)
    {
        var restoreFlags = this.saveRestoreFlags();
        this.ctx.flags &= ~(NodeBuilderFlags.WriteTypeParametersInQualifiedName);
        var modifiers = Ast.CreateModifiersFromModifierFlags(this.ch.getTypeParameterModifiers(typeParameter), (nodeKind) => this.f.NewModifier(nodeKind));
        ModifierList modifiersList = default;
        if ((GoRuntime.Len(modifiers) > 0))
        {
            modifiersList = this.f.NewModifierList(modifiers);
        }

        var name = this.typeParameterToName(typeParameter);
        var defaultParameter = this.ch.getDefaultFromTypeParameter(typeParameter);
        Node defaultParameterDeclarationNode = default;
        if ((defaultParameter != null))
        {
            defaultParameterDeclarationNode = this.typeToTypeNode(defaultParameter);
        }

        restoreFlags();
        return this.f.NewTypeParameterDeclaration(modifiersList, name.AsNode(), constraintNode, null, defaultParameterDeclarationNode);
    }
}

internal sealed partial class NodeBuilderImpl
{
    private bool isMappedTypeHomomorphic(Type mapped)
    {
        return (this.ch.getHomomorphicTypeVariable(mapped) != null);
    }
}

internal sealed partial class NodeBuilderImpl
{
    private bool isHomomorphicMappedTypeWithNonHomomorphicInstantiation(MappedType mapped)
    {
        return (((mapped.target != null) && !this.isMappedTypeHomomorphic(mapped.AsType())) && this.isMappedTypeHomomorphic(mapped.target));
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node createMappedTypeNodeFromType(Type t)
    {
        Debug.Assert(((t.Flags() & TypeFlags.Object) != 0));
        var mapped = t.AsMappedType();
        Node @readonlyToken = default;
        if ((mapped.declaration.ReadonlyToken != null))
        {
            @readonlyToken = this.f.NewToken(mapped.declaration.ReadonlyToken.Kind);
        }

        Node questionToken = default;
        if ((mapped.declaration.QuestionToken != null))
        {
            questionToken = this.f.NewToken(mapped.declaration.QuestionToken.Kind);
        }

        Node appropriateConstraintTypeNode = default;
        Node newTypeVariable = default;
        var templateType = this.ch.getTemplateTypeFromMappedType(t);
        var typeParameter = this.ch.getTypeParameterFromMappedType(t);
        var needsModifierPreservingWrapper = (((!this.ch.isMappedTypeWithKeyofConstraintDeclaration(t) && ((this.ch.getModifiersTypeFromMappedType(t).flags & TypeFlags.Unknown) == 0)) && ((this.ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0)) && !((((this.ch.getConstraintTypeFromMappedType(t).flags & TypeFlags.TypeParameter) != 0) && ((this.ch.getConstraintOfTypeParameter(this.ch.getConstraintTypeFromMappedType(t)).flags & TypeFlags.Index) != 0))));
        if (this.ch.isMappedTypeWithKeyofConstraintDeclaration(t))
        {
            if ((((this.ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0) && this.isHomomorphicMappedTypeWithNonHomomorphicInstantiation(mapped)))
            {
                var newConstraintParam = this.ch.newTypeParameter(this.ch.newSymbol(SymbolFlags.TypeParameter, "T"));
                var name = this.typeParameterToName(newConstraintParam);
                var target = t.Target();
                newTypeVariable = this.f.NewTypeReferenceNode(name.AsNode(), null);
                templateType = this.ch.instantiateType(this.ch.getTemplateTypeFromMappedType(target), Checker.newTypeMapper(new List<Type> { this.ch.getTypeParameterFromMappedType(target), this.ch.getModifiersTypeFromMappedType(target) }, new List<Type> { typeParameter, newConstraintParam }));
            }

            var indexTarget = newTypeVariable;
            if ((indexTarget == null))
            {
                indexTarget = this.typeToTypeNode(this.ch.getModifiersTypeFromMappedType(t));
            }

            appropriateConstraintTypeNode = this.f.NewTypeOperatorNode(Kind.KeyOfKeyword, indexTarget);
        }
        else
        {
            if (needsModifierPreservingWrapper)
            {
                var newParam = this.ch.newTypeParameter(this.ch.newSymbol(SymbolFlags.TypeParameter, "T"));
                var name2 = this.typeParameterToName(newParam);
                newTypeVariable = this.f.NewTypeReferenceNode(name2.AsNode(), null);
                appropriateConstraintTypeNode = newTypeVariable;
            }
            else
            {
                appropriateConstraintTypeNode = this.typeToTypeNode(this.ch.getConstraintTypeFromMappedType(t));
            }
        }

        var cleanup = this.enterNewScope(mapped.declaration.AsNode(), null, new List<Type> { this.ch.getTypeParameterFromMappedType(t) }, null, null);
        var typeParameterDeclarationNode = this.typeParameterToDeclarationWithConstraint(typeParameter, appropriateConstraintTypeNode);
        Node nameTypeNode = default;
        if ((mapped.declaration.NameType != null))
        {
            nameTypeNode = this.typeToTypeNode(this.ch.getNameTypeFromMappedType(t));
        }

        var templateTypeNode = this.typeToTypeNode(this.ch.removeMissingType(templateType, ((Checker.getMappedTypeModifiers(t) & Checker.MappedTypeModifiers.IncludeOptional) != 0)));
        cleanup();
        var result = this.f.NewMappedTypeNode(@readonlyToken, typeParameterDeclarationNode, nameTypeNode, questionToken, templateTypeNode, null);
        this.ctx.approximateLength += 10;
        this.e.AddEmitFlags(result, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
        if ((((this.ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0) && this.isHomomorphicMappedTypeWithNonHomomorphicInstantiation(mapped)))
        {
            var rawConstraintTypeFromDeclaration = this.getTypeFromTypeNode(mapped.declaration.TypeParameter.AsTypeParameterDeclaration().Constraint.Type(), false);
            if ((rawConstraintTypeFromDeclaration != null))
            {
                rawConstraintTypeFromDeclaration = this.ch.getConstraintOfTypeParameter(rawConstraintTypeFromDeclaration);
            }

            if ((rawConstraintTypeFromDeclaration == null))
            {
                rawConstraintTypeFromDeclaration = this.ch.unknownType;
            }

            var originalConstraint = this.ch.instantiateType(rawConstraintTypeFromDeclaration, mapped.mapper);
            Node originalConstraintNode = default;
            if (((originalConstraint.flags & TypeFlags.Unknown) == 0))
            {
                originalConstraintNode = this.typeToTypeNode(originalConstraint);
            }

            return this.f.NewConditionalTypeNode(this.typeToTypeNode(this.ch.getModifiersTypeFromMappedType(t)), this.f.NewInferTypeNode(this.f.NewTypeParameterDeclaration(null, newTypeVariable.AsTypeReferenceNode().TypeName.Clone(this.f), originalConstraintNode, null, null)), result, this.f.NewKeywordTypeNode(Kind.NeverKeyword));
        }
        else
        {
            if (needsModifierPreservingWrapper)
            {
                return this.f.NewConditionalTypeNode(this.typeToTypeNode(this.ch.getConstraintTypeFromMappedType(t)), this.f.NewInferTypeNode(this.f.NewTypeParameterDeclaration(null, newTypeVariable.AsTypeReferenceNode().TypeName.Clone(this.f), this.f.NewTypeOperatorNode(Kind.KeyOfKeyword, this.typeToTypeNode(this.ch.getModifiersTypeFromMappedType(t))), null, null)), result, this.f.NewKeywordTypeNode(Kind.NeverKeyword));
            }
        }

        return result;
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node typeParameterToDeclaration(Type parameter)
    {
        var constraint = this.ch.getConstraintOfTypeParameter(parameter);
        Node constraintNode = default;
        if ((constraint != null))
        {
            constraintNode = this.typeToTypeNodeHelperWithPossibleReusableTypeNode(constraint, this.ch.getConstraintDeclaration(parameter));
        }

        return this.typeParameterToDeclarationWithConstraint(parameter, constraintNode);
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node typeToTypeNodeOrCircularityElision(Type t)
    {
        if (((t.flags & TypeFlags.Union) != 0))
        {
            if (this.ctx.visitedTypes.Contains(t.id))
            {
                return this.createCyclicStructurePlaceholder();
            }

            return this.visitAndTransformType(t, this.typeToTypeNode);
        }

        return this.typeToTypeNode(t);
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node conditionalTypeToTypeNode(Type _t)
    {
        if (this.checkTruncationLength())
        {
            return this.createElidedInformationPlaceholder();
        }

        var t = _t.AsConditionalType();
        var checkTypeNode = this.typeToTypeNode(t.checkType);
        this.ctx.approximateLength += 15;
        if (((((this.ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0) && t.root.isDistributive) && ((t.checkType.flags & TypeFlags.TypeParameter) == 0)))
        {
            var newParam = this.ch.newTypeParameter(this.ch.newSymbol(SymbolFlags.TypeParameter, "T"));
            var name = this.typeParameterToName(newParam);
            var newTypeVariable = this.f.NewTypeReferenceNode(name.AsNode(), null);
            this.ctx.approximateLength += 37;
            var newMapper = Checker.prependTypeMapping(t.root.checkType, newParam, t.mapper);
            var saveInferTypeParameters = this.ctx.inferTypeParameters;
            this.ctx.inferTypeParameters = t.root.inferTypeParameters;
            var extendsTypeNode = this.typeToTypeNode(this.ch.instantiateType(t.root.extendsType, newMapper));
            this.ctx.inferTypeParameters = saveInferTypeParameters;
            var trueTypeNode = this.typeToTypeNodeOrCircularityElision(this.ch.instantiateType(this.getTypeFromTypeNode(t.root.node.TrueType, false), newMapper));
            var falseTypeNode = this.typeToTypeNodeOrCircularityElision(this.ch.instantiateType(this.getTypeFromTypeNode(t.root.node.FalseType, false), newMapper));
            var newId = newTypeVariable.AsTypeReferenceNode().TypeName.AsIdentifier().Clone(this.f);
            var syntheticExtendsNode = this.f.NewInferTypeNode(this.f.NewTypeParameterDeclaration(null, newId, null, null, null));
            var innerCheckConditionalNode = this.f.NewConditionalTypeNode(newTypeVariable, extendsTypeNode, trueTypeNode, falseTypeNode);
            var syntheticTrueNode = this.f.NewConditionalTypeNode(this.f.NewTypeReferenceNode(name.Clone(this.f), null), this.f.DeepCloneNode(checkTypeNode), innerCheckConditionalNode, this.f.NewKeywordTypeNode(Kind.NeverKeyword));
            return this.f.NewConditionalTypeNode(checkTypeNode, syntheticExtendsNode, syntheticTrueNode, this.f.NewKeywordTypeNode(Kind.NeverKeyword));
        }

        var saveInferTypeParameters2 = this.ctx.inferTypeParameters;
        this.ctx.inferTypeParameters = t.root.inferTypeParameters;
        var extendsTypeNode2 = this.typeToTypeNode(t.extendsType);
        this.ctx.inferTypeParameters = saveInferTypeParameters2;
        var trueTypeNode2 = this.typeToTypeNodeOrCircularityElision(this.ch.getTrueTypeFromConditionalType(_t));
        var falseTypeNode2 = this.typeToTypeNodeOrCircularityElision(this.ch.getFalseTypeFromConditionalType(_t));
        return this.f.NewConditionalTypeNode(checkTypeNode, extendsTypeNode2, trueTypeNode2, falseTypeNode2);
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Symbol getParentSymbolOfTypeParameter(TypeParameter typeParameter)
    {
        var tp = Ast.GetDeclarationOfKind(typeParameter.AsType().symbol, Kind.TypeParameter);
        Node host = default;
        host = tp.Parent;
        if ((host == null))
        {
            return null;
        }

        return this.ch.getSymbolOfNode(host);
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node arrayOrTupleTypeToNode(Type t)
    {
        List<Type> typeArguments = this.ch.getTypeArguments(t);
        if (((t.Target() == this.ch.globalArrayType) || (t.Target() == this.ch.globalReadonlyArrayType)))
        {
            if (((this.ctx.flags & NodeBuilderFlags.WriteArrayAsGenericType) != 0))
            {
                var typeArgumentNode = this.typeToTypeNode(typeArguments[0]);
                return this.f.NewTypeReferenceNode(this.newIdentifier(Core.IfElse((t.Target() == this.ch.globalArrayType), "Array", "ReadonlyArray"), t.Target().symbol), this.f.NewNodeList(Slice<Node>.FromArray([typeArgumentNode])));
            }

            var elementType = this.typeToTypeNode(typeArguments[0]);
            var arrayType = this.f.NewArrayTypeNode(elementType);
            if ((t.Target() == this.ch.globalArrayType))
            {
                return arrayType;
            }
            else
            {
                return this.f.NewTypeOperatorNode(Kind.ReadonlyKeyword, arrayType);
            }
        }
        else
        {
            Debug.Assert(((t.Target().objectFlags & ObjectFlags.Tuple) != 0));
            typeArguments = Core.SameMapIndex(typeArguments, (arg, i) =>
            {
                var isOptional = false;
                if ((i < GoRuntime.Len(t.Target().AsTupleType().elementInfos)))
                {
                    isOptional = ((t.Target().AsTupleType().elementInfos[i].flags & ElementFlags.Optional) != 0);
                }

                return this.ch.removeMissingType(arg, isOptional);
            });
            if ((GoRuntime.Len(typeArguments) > 0))
            {
                var arity = this.ch.getTypeReferenceArity(t);
                var tupleConstituentNodes = this.mapToTypeNodes(GoRuntime.Slice(typeArguments, 0, arity), false);
                if ((tupleConstituentNodes != null))
                {
                    {
                        var i2 = 0;
                        for (; (i2 < GoRuntime.Len(tupleConstituentNodes.Nodes)); i2++)
                        {
                            var flags = t.Target().AsTupleType().elementInfos[i2].flags;
                            var labeledElementDeclaration = t.Target().AsTupleType().elementInfos[i2].labeledDeclaration;
                            if ((labeledElementDeclaration != null))
                            {
                                tupleConstituentNodes.Nodes[i2] = this.f.NewNamedTupleMember(Core.IfElse(((flags & ElementFlags.Variable) != 0), this.f.NewToken(Kind.DotDotDotToken), null), this.newIdentifier(this.ch.getTupleElementLabel(t.Target().AsTupleType().elementInfos[i2], null, i2), null), Core.IfElse(((flags & ElementFlags.Optional) != 0), this.f.NewToken(Kind.QuestionToken), null), Core.IfElse(((flags & ElementFlags.Rest) != 0), this.f.NewArrayTypeNode(tupleConstituentNodes.Nodes[i2]), tupleConstituentNodes.Nodes[i2]));
                            }
                            else
                            {
                                switch (true)
                                {
                                    case true when ((flags & ElementFlags.Variable) != 0):
                                    {
                                        tupleConstituentNodes.Nodes[i2] = this.f.NewRestTypeNode(Core.IfElse(((flags & ElementFlags.Rest) != 0), this.f.NewArrayTypeNode(tupleConstituentNodes.Nodes[i2]), tupleConstituentNodes.Nodes[i2]));
                                        break;
                                    }

                                    case true when ((flags & ElementFlags.Optional) != 0):
                                    {
                                        tupleConstituentNodes.Nodes[i2] = this.f.NewOptionalTypeNode(tupleConstituentNodes.Nodes[i2]);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    var tupleTypeNode = this.f.NewTupleTypeNode(tupleConstituentNodes);
                    this.e.SetEmitFlags(tupleTypeNode, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                    if (t.Target().AsTupleType().@readonly)
                    {
                        return this.f.NewTypeOperatorNode(Kind.ReadonlyKeyword, tupleTypeNode);
                    }
                    else
                    {
                        return tupleTypeNode;
                    }
                }
            }

            if ((this.ctx.encounteredError || (((this.ctx.flags & NodeBuilderFlags.AllowEmptyTuple) != 0))))
            {
                var tupleTypeNode2 = this.f.NewTupleTypeNode(this.f.NewNodeList(Slice<Node>.Make(0)));
                this.e.SetEmitFlags(tupleTypeNode2, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                if (t.Target().AsTupleType().@readonly)
                {
                    return this.f.NewTypeOperatorNode(Kind.ReadonlyKeyword, tupleTypeNode2);
                }
                else
                {
                    return tupleTypeNode2;
                }
            }

            this.ctx.encounteredError = true;
            return null;
        }
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node appendReferenceToType(Node root, Node @ref)
    {
        if (Ast.IsImportTypeNode(root))
        {
            var imprt = root.AsImportTypeNode();
            var ids = getAccessStack(@ref);
            var qualifier = root.AsImportTypeNode().Qualifier;
            {
                foreach (var id in GoRuntime.RangeValues(ids))
                {
                    if ((qualifier != null))
                    {
                        qualifier = this.f.NewQualifiedName(qualifier, id);
                    }
                    else
                    {
                        qualifier = id;
                    }
                }
            }

            return this.f.UpdateImportTypeNode(root.AsImportTypeNode(), imprt.IsTypeOf, imprt.Argument, imprt.Attributes, qualifier, @ref.TypeArgumentList());
        }
        else
        {
            if (Ast.IsTypeReferenceNode(root))
            {
                var typeRef = root.AsTypeReferenceNode();
                if (((((this.ctx.flags & NodeBuilderFlags.UseInstantiationExpressions) != 0) && (typeRef.TypeArguments != null)) && (GoRuntime.Len(typeRef.TypeArguments.Nodes) != 0)))
                {
                    var expr = this.createExpressionWithTypeArguments(this.createAccessExpression(typeRef.TypeName), typeRef.TypeArguments);
                    {
                        foreach (var range2 in GoRuntime.Range(getAccessStack(@ref)))
                        {
                            var _2 = range2.Key;
                            var id2 = range2.Value;
                            expr = this.f.NewPropertyAccessExpression(expr, null, id2, NodeFlags.None);
                        }
                    }

                    return expr;
                }

                Node typeName = typeRef.TypeName;
                {
                    foreach (var range3 in GoRuntime.Range(getAccessStack(@ref)))
                    {
                        var _3 = range3.Key;
                        var id3 = range3.Value;
                        typeName = this.f.NewQualifiedName(typeName, id3);
                    }
                }

                return this.f.UpdateTypeReferenceNode(root.AsTypeReferenceNode(), typeName, @ref.TypeArgumentList());
            }
        }

        var expr2 = this.createAccessExpression(root);
        {
            foreach (var range4 in GoRuntime.Range(getAccessStack(@ref)))
            {
                var _4 = range4.Key;
                var id4 = range4.Value;
                expr2 = this.f.NewPropertyAccessExpression(expr2, null, id4, NodeFlags.None);
            }
        }

        return expr2;
    }
}

internal sealed partial class NodeBuilderImpl
{
    internal Node symbolToName(Symbol symbol, SymbolFlags meaning, bool expectsIdentifier)
    {
        var chain = this.lookupSymbolChain(symbol, meaning, false);
        if ((((expectsIdentifier && (GoRuntime.Len(chain) != 1)) && !this.ctx.encounteredError) && (((this.ctx.flags & NodeBuilderFlags.AllowQualifiedNameInPlaceOfIdentifier) != 0))))
        {
            this.ctx.encounteredError = true;
        }

        return this.createEntityNameFromSymbolChain(chain, (GoRuntime.Len(chain) - 1));
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node createEntityNameFromSymbolChain(List<Symbol> chain, int index)
    {
        var symbol = chain[index];
        if ((index == 0))
        {
            this.ctx.flags |= NodeBuilderFlags.InInitialEntityName;
        }

        var symbolName = this.getNameOfSymbolAsWritten(symbol);
        if ((index == 0))
        {
            this.ctx.flags ^= NodeBuilderFlags.InInitialEntityName;
        }

        var identifier = this.newIdentifier(symbolName, symbol);
        this.e.AddEmitFlags(identifier, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
        if ((index > 0))
        {
            return this.f.NewQualifiedName(this.createEntityNameFromSymbolChain(chain, (index - 1)), identifier);
        }

        return identifier;
    }
}

internal sealed partial class NodeBuilderImpl
{
    private bool typeParameterShadowsOtherTypeParameterInScope(string name, Type typeParameter)
    {
        var result = this.ch.nameResolver.Resolve(this.ctx.enclosingDeclaration, name, SymbolFlags.Type, null, false, false);
        if (((result != null) && ((result.Flags & SymbolFlags.TypeParameter) != 0)))
        {
            return (result != typeParameter.symbol);
        }

        return false;
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node typeParameterToName(Type typeParameter)
    {
        if (((this.ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0))
        {
            {
                var ok = this.ctx.typeParameterNames.TryGetValue(typeParameter.id, out var cached);
                if (ok)
                {
                    return cached;
                }
            }
        }

        var result = this.symbolToName(typeParameter.symbol, SymbolFlags.Type, true);
        if (!Ast.IsIdentifier(result))
        {
            return this.f.NewIdentifier("(Missing type parameter)");
        }

        if (((typeParameter.symbol != null) && (GoRuntime.Len(typeParameter.symbol.Declarations) > 0)))
        {
            var decl = typeParameter.symbol.Declarations[0];
            if (((decl != null) && Ast.IsTypeParameterDeclaration(decl)))
            {
                result = this.setTextRange(result, decl.Name());
            }
        }

        if (((this.ctx.flags & NodeBuilderFlags.GenerateNamesForShadowedTypeParams) != 0))
        {
            var rawText = result.Text();
            this.ctx.typeParameterNamesByTextNextNameCount.TryGetValue(rawText, out var i);
            var text = rawText;
            while (true)
            {
                if ((!this.ctx.typeParameterNamesByText.Contains(text) && !this.typeParameterShadowsOtherTypeParameterInScope(text, typeParameter)))
                {
                    break;
                }

                i++;
                text = (rawText + "_" + Strconv.Itoa(i));
            }

            if ((text != rawText))
            {
                result = this.newIdentifier(text, typeParameter.symbol);
            }

            this.ctx.typeParameterNamesByTextNextNameCount[rawText] = i;
            this.ctx.typeParameterNames[typeParameter.id] = result;
            this.ctx.typeParameterNamesByText.Add(text);
        }

        return result;
    }
}

internal sealed partial class NodeBuilderImpl
{
    private static Slice<Node> getAccessStack(Node @ref)
    {
        Node state = @ref.AsTypeReferenceNode().TypeName;
        var ids = Slice<Node>.Make(0);
        while (!Ast.IsIdentifier(state))
        {
            var entity = state.AsQualifiedName();
            ids = GoRuntime.Append(Slice<Node>.FromArray([entity.Right]), ids);
            state = entity.Left;
        }

        ids = GoRuntime.Append(Slice<Node>.FromArray([state]), ids);
        return ids;
    }
}

internal sealed partial class NodeBuilderImpl
{
    private Node createAccessExpression(Node node)
    {
        switch (true)
        {
            case true when Ast.IsQualifiedName(node):
            {
                return this.f.NewPropertyAccessExpression(this.createAccessExpression(node.AsQualifiedName().Left), null, this.f.DeepCloneNode(node.AsQualifiedName().Right), NodeFlags.None);
            }

            case true when Ast.IsIdentifier(node):
            case true when Ast.IsPropertyAccessExpression(node):
            case true when Ast.IsExpressionWithTypeArguments(node):
            {
                return this.f.DeepCloneNode(node);
            }

            default:
            {
                throw new InvalidOperationException(("unexpected access node kind: " + node.Kind.ToString()));
            }
        }
    }
}

internal sealed partial class NodeBuilderImpl
{
    // nodebuilderimpl.go:1669. Default diagnostics disable expansion depth.
    private Node typeToTypeNodeHelperWithPossibleReusableTypeNode(Type t, Node typeNode)
    {
        if (t == null)
            return f.NewKeywordTypeNode(Kind.AnyKeyword);
        if (!isActivelyExpanding() && typeNode != null && getTypeFromTypeNode(typeNode, false) == t)
        {
            var reused = tryReuseExistingNodeHelper(typeNode);
            if (reused != null)
            {
                checkTypeExpandability(t);
                return reused;
            }
        }

        return typeToTypeNode(t);
    }

    // nodebuilderimpl.go:240. No public selected API enables expansion depth.
    private void checkTypeExpandability(Type t)
    {
        if (ctx.maxExpansionDepth < 0 || t == null)
            return;
        throw Checker.unsupported("checkTypeExpandability: configured expansion depth");
    }
}

internal sealed partial class NodeBuilderImpl
{
    // nodebuilderimpl.go:565. Preserve the actual object-side cache when abstract
    // constructor signatures are factored out of a diagnostic intersection.
    private Type getResolvedTypeWithoutAbstractConstructSignatures(StructuredType t)
    {
        if (GoRuntime.Len(t.ConstructSignaturesSlice()) == 0)
            return t.AsType();
        if (t.objectTypeWithoutAbstractConstructSignatures != null)
            return t.objectTypeWithoutAbstractConstructSignatures;
        var constructors = Core.Filter(t.ConstructSignaturesSlice(), signature => (signature.flags & SignatureFlags.Abstract) == 0);
        if (GoRuntime.Len(constructors) == GoRuntime.Len(t.ConstructSignaturesSlice()))
        {
            t.objectTypeWithoutAbstractConstructSignatures = t.AsType();
            return t.AsType();
        }

        var copy = ch.newAnonymousType(t.AsType().symbol, t.members, t.CallSignaturesSlice(), GoRuntime.Len(constructors) > 0 ? constructors : Slice<Signature>.Make(0), t.indexInfos);
        t.objectTypeWithoutAbstractConstructSignatures = copy;
        copy.AsStructuredType().objectTypeWithoutAbstractConstructSignatures = copy;
        return copy;
    }
}

internal sealed partial class NodeBuilderImpl
{
    // nodebuilderimpl.go:213. Configured hover expansion is not exposed by the
    // default diagnostic APIs; retain the actual disabled early-return arm.
    private bool shouldExpandType(Type t, bool isAlias)
    {
        if (ctx.maxExpansionDepth < 0)
            return false;
        throw Checker.unsupported("shouldExpandType: configured hover expansion");
    }

    // nodebuilderimpl.go:2372. Reverse-mapped placeholders are display-only.
    private bool shouldUsePlaceholderForProperty(Symbol propertySymbol)
    {
        if ((ctx.flags & NodeBuilderFlags.AllowAnonymousIdentifier) == 0 || (propertySymbol.CheckFlags & CheckFlags.ReverseMapped) == 0)
            return false;
        if (GoSlices.Contains(ctx.reverseMappedStack, propertySymbol))
            return true;
        int length = GoRuntime.Len(ctx.reverseMappedStack);
        if (length > 0)
        {
            var last = ctx.reverseMappedStack[length - 1];
            if (ch.ReverseMappedSymbolLinks.Has(last))
            {
                var propertyType = ch.ReverseMappedSymbolLinks.TryGet(last).propertyType;
                if (propertyType != null && (propertyType.objectFlags & ObjectFlags.Anonymous) == 0)
                    return true;
            }
        }

        const int maxDepth = 3;
        if (length < maxDepth || !ch.ReverseMappedSymbolLinks.Has(propertySymbol))
            return false;
        var propMappedType = ch.ReverseMappedSymbolLinks.TryGet(propertySymbol).mappedType;
        if (propMappedType?.symbol == null)
            return false;
        for (int i = 0; i < length; i++)
        {
            if (i > maxDepth)
                break;
            var prop = ctx.reverseMappedStack[length - 1 - i];
            if (ch.ReverseMappedSymbolLinks.Has(prop))
            {
                var mappedType = ch.ReverseMappedSymbolLinks.TryGet(prop).mappedType;
                if (mappedType != null && mappedType.symbol == propMappedType.symbol)
                    return true;
            }
        }

        return false;
    }

    // nodebuilderimpl.go:746/1039/1697/3726/3753.
    private static IndexedAccessTypeNode getTopmostIndexedAccessType(IndexedAccessTypeNode node) => Ast.IsIndexedAccessTypeNode(node.ObjectType) ? getTopmostIndexedAccessType(node.ObjectType.AsIndexedAccessTypeNode()) : node;
    private bool shouldWriteTypeParametersInQualifiedName(List<Symbol> chain, int index) => (ctx.flags & NodeBuilderFlags.WriteTypeParametersInQualifiedName) != 0 && index < chain.Count - 1;
    private NodeList lookupTypeParameterNodes(List<Symbol> chain, int index)
    {
        var symbol = chain[index];
        if (!ctx.typeParameterSymbolList.Add(Ast.GetSymbolId(symbol)))
            return null;
        if (shouldWriteTypeParametersInQualifiedName(chain, index))
        {
            var arguments = lookupInstantiatedTypeArgumentNodes(chain, index);
            if (arguments != null)
                return arguments;
            var parameters = typeParametersToTypeParameterDeclarations(symbol);
            if (GoRuntime.Len(parameters) > 0)
                return f.NewNodeList(parameters);
        }

        return null;
    }

    private List<Type> getTypeParametersOfClassOrInterface(Symbol symbol) => GoRuntime.Append(ch.getOuterTypeParametersOfClassOrInterface(symbol)is { } outer ? new List<Type>(outer) : new List<Type>(), ch.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol) ?? []);
    private NodeList lookupInstantiatedTypeArgumentNodes(List<Symbol> chain, int index)
    {
        if (!shouldWriteTypeParametersInQualifiedName(chain, index))
            return null;
        var symbol = chain[index];
        var nextSymbol = chain[index + 1];
        if ((nextSymbol.CheckFlags & CheckFlags.Instantiated) == 0)
            return null;
        var target = symbol;
        if ((symbol.Flags & SymbolFlags.Alias) != 0 && !ch.canGetTypeParametersOfClassOrInterface(symbol))
            target = ch.resolveAlias(symbol);
        if (!ch.canGetTypeParametersOfClassOrInterface(target))
            return null;
        var parameters = getTypeParametersOfClassOrInterface(target);
        var mapper = ch.valueSymbolLinks.Get(nextSymbol).mapper;
        if (mapper != null)
            parameters = Core.Map(parameters, mapper.Map);
        return mapToTypeNodes(parameters, false);
    }

    private Slice<Node> typeParametersToTypeParameterDeclarations(Symbol symbol)
    {
        var target = ch.getTargetSymbol(symbol);
        Slice<Node> results = default;
        if ((target.Flags & (SymbolFlags.Class | SymbolFlags.Interface | SymbolFlags.Alias)) != 0)
        {
            foreach (var parameter in ch.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol) ?? [])
                results = GoRuntime.Append(results, typeParameterToDeclaration(parameter));
        }
        else if ((target.Flags & SymbolFlags.Function) != 0)
        {
            foreach (var parameter in ch.getTypeParametersFromDeclaration(symbol.ValueDeclaration) ?? [])
                results = GoRuntime.Append(results, typeParameterToDeclaration(parameter));
        }

        return results;
    }

    private static bool startsWithSingleOrDoubleQuote(string text) => text.StartsWith("'", StringComparison.Ordinal) || text.StartsWith("\"", StringComparison.Ordinal);
    private static bool canUsePropertyAccess(string name) => name.Length > 0 && (name.StartsWith("#", StringComparison.Ordinal) ? name.Length > 1 && TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name[1..], LanguageVariant.Standard) : TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name, LanguageVariant.Standard));
    private readonly record struct sortedSymbolNamePair(Symbol sym, string name);
    private int sortByBestName(sortedSymbolNamePair first, sortedSymbolNamePair second)
    {
        if (first.name.Length > 0 && second.name.Length > 0)
        {
            bool firstRelative = TsPath.PathIsRelative(first.name);
            bool secondRelative = TsPath.PathIsRelative(second.name);
            if (firstRelative == secondRelative)
                return ModuleSpecifierFunctions.CountPathComponents(first.name) - ModuleSpecifierFunctions.CountPathComponents(second.name);
            return secondRelative ? -1 : 1;
        }

        return ch.compareSymbols(first.sym, second.sym);
    }

    // nodebuilderimpl.go:1175/1194. Original specifier-source dispatch; source
    // require/import-call/import-equals forms remain outside the project gate.
    private static bool canHaveModuleSpecifier(Node node) => node != null && node.Kind is Kind.VariableDeclaration or Kind.BindingElement or Kind.ImportDeclaration or Kind.ExportDeclaration or Kind.ImportEqualsDeclaration or Kind.ImportClause or Kind.NamespaceExport or Kind.NamespaceImport or Kind.ExportSpecifier or Kind.ImportSpecifier or Kind.ImportType;
    internal static Node TryGetModuleSpecifierFromDeclaration(Node node)
    {
        var result = tryGetModuleSpecifierFromDeclarationWorker(node);
        return result != null && Ast.IsStringLiteral(result) ? result : null;
    }

    private static Node tryGetModuleSpecifierFromDeclarationWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.BindingElement:
                var moduleCall = Ast.FindAncestor(node.Initializer(), n => Ast.IsRequireCall(n, true) || Ast.IsImportCall(n));
                return moduleCall?.Arguments()[0];
            case Kind.ImportDeclaration:
            case Kind.ExportDeclaration:
            case Kind.JSDocImportTag:
                return node.ModuleSpecifier();
            case Kind.ImportEqualsDeclaration:
                var reference = node.AsImportEqualsDeclaration().ModuleReference;
                return reference.Kind == Kind.ExternalModuleReference ? reference.Expression() : null;
            case Kind.ImportClause:
            case Kind.NamespaceExport:
                return node.Parent.ModuleSpecifier();
            case Kind.NamespaceImport:
            case Kind.ExportSpecifier:
                return node.Parent.Parent.ModuleSpecifier();
            case Kind.ImportSpecifier:
                return node.Parent.Parent.Parent.ModuleSpecifier();
            case Kind.ImportType:
                return Ast.IsLiteralImportTypeNode(node) ? node.AsImportTypeNode().Argument.AsLiteralTypeNode().Literal : null;
            default:
                throw new InvalidOperationException("unexpected module specifier declaration");
        }
    }

    // nodebuilderimpl.go:1250. Actual per-symbol, per-context-file/mode cache.
    private moduleSpecifierResult getSpecifierForModuleSymbol(Symbol symbol, ResolutionMode overrideImportMode)
    {
        var enclosingDeclaration = e.MostOriginal(ctx.enclosingDeclaration);
        var originalSpecifier = canHaveModuleSpecifier(enclosingDeclaration) ? TryGetModuleSpecifierFromDeclaration(enclosingDeclaration) : null;
        var originalAttributes = originalSpecifier != null ? ch.getImportAttributesTypeForModuleSpecifier(originalSpecifier) : null;
        var file = Ast.GetDeclarationOfKind(symbol, Kind.SourceFile);
        if (file == null)
        {
            var equivalent = Core.FirstNonNil(symbol.Declarations, d => ch.getFileSymbolIfFileSymbolExportEqualsContainer(d, symbol));
            if (equivalent != null)
                file = Ast.GetDeclarationOfKind(equivalent, Kind.SourceFile);
        }

        if (file == null)
        {
            var declaration = Core.Find(symbol.Declarations, Ast.IsModuleWithStringLiteralName);
            if (declaration != null)
            {
                var specifier = declaration.Name().Text();
                if (originalAttributes != null && moduleSpecifierResolvesToSymbol(specifier, originalAttributes, symbol))
                    return new(specifier, originalAttributes);
                return new(specifier, ch.getTypeOfModuleImportAttributes(symbol));
            }

            var(ambientName, hasAmbientName) = Ast.TryGetAmbientModuleNameFromSymbolName(symbol.Name);
            if (hasAmbientName)
                return new(ambientName);
        }

        if (ctx.enclosingFile == null)
        {
            var(ambientName, hasAmbientName) = Ast.TryGetAmbientModuleNameFromSymbolName(symbol.Name);
            return new(hasAmbientName ? ambientName : Ast.GetSourceFileOfModule(symbol).FileNameGoBytes());
        }

        var contextFile = ctx.enclosingFile;
        var resolutionMode = overrideImportMode;
        if (resolutionMode == ResolutionMode.None && originalSpecifier != null)
            resolutionMode = ch.program.GetModeForUsageLocation(contextFile, originalSpecifier);
        else if (resolutionMode == ResolutionMode.None && contextFile != null)
            resolutionMode = ch.program.GetDefaultResolutionModeForFile(contextFile);
        var cacheKey = new ModeAwareCacheKey(contextFile.PathKey(), resolutionMode);
        var links = symbolLinks.Get(symbol);
        links.specifierCache ??= new();
        if (links.specifierCache.TryGetValue(cacheKey, out var existing))
            return moduleSpecifierResultForSymbol(existing, originalAttributes, symbol);
        var generated = ModuleSpecifierFunctions.GetModuleSpecifiers(symbol, ch, ch.compilerOptions, contextFile, ctx.host, new UserPreferences { ImportModuleSpecifierPreference = ImportModuleSpecifierPreference.ProjectRelative, ImportModuleSpecifierEnding = resolutionMode == ResolutionMode.ESNext ? ImportModuleSpecifierEndingPreference.Js : ImportModuleSpecifierEndingPreference.None }, new ModuleSpecifierOptions { OverrideImportMode = overrideImportMode }, false);
        System.Diagnostics.Debug.Assert(GoRuntime.Len(generated.Specifiers) > 0);
        var result = new moduleSpecifierResult(generated.Specifiers[0].AsString());
        if (generated.AmbientModuleSymbol != null)
            result = result with
            {
                importAttributesType = ch.getTypeOfModuleImportAttributes(generated.AmbientModuleSymbol)
            };
        links.specifierCache[cacheKey] = result;
        return moduleSpecifierResultForSymbol(result, originalAttributes, symbol);
    }

    private moduleSpecifierResult moduleSpecifierResultForSymbol(moduleSpecifierResult result, Type attributes, Symbol symbol) => attributes != null && moduleSpecifierResolvesToSymbol(result.specifier, attributes, symbol) ? result with
    {
        importAttributesType = attributes
    }

    : result;
    private bool moduleSpecifierResolvesToSymbol(string specifier, Type attributes, Symbol symbol)
    {
        var location = ctx.enclosingDeclaration ?? ctx.enclosingFile?.AsNode();
        if (location == null)
            return false;
        var resolved = ch.resolveExternalModule(location, specifier, null, null, false, attributes);
        return resolved != null && ch.getMergedSymbol(resolved) == ch.getMergedSymbol(symbol);
    }

    // nodebuilderimpl.go:1359–1399. Stable attribute ordering and source resolution-mode metadata.
    private Node createImportAttributesForModuleSpecifier(moduleSpecifierResult result, ResolutionMode mode)
    {
        bool empty = result.importAttributesType == null || ch.isEmptyObjectType(result.importAttributesType);
        if (empty && mode == ResolutionMode.None)
            return null;
        var properties = empty ? null : GoSlices.Clone(ch.getPropertiesOfType(result.importAttributesType));
        properties?.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        var attributes = Slice<Node>.Make(0, GoRuntime.Len(properties) + 1);
        if (mode != ResolutionMode.None)
        {
            var text = mode == ResolutionMode.ESNext ? "import" : "require";
            attributes = attributes.Append(f.NewImportAttribute(newStringLiteral("resolution-mode"), newStringLiteral(text)));
            approximateLength += "resolution-mode".Length + text.Length + 6;
        }

        foreach (var property in properties ?? [])
        {
            var type = ch.getTypeOfSymbol(property);
            if ((type.flags & TypeFlags.StringLiteral) == 0)
                continue;
            var value = Checker.getStringLiteralValue(type);
            Node name;
            if (TypeScript.Scanner.Scanner.IsIdentifierText(property.Name, LanguageVariant.Standard))
                name = f.NewIdentifier(property.Name);
            else
            {
                name = newStringLiteral(property.Name);
                approximateLength += 2;
            }

            attributes = attributes.Append(f.NewImportAttribute(name, newStringLiteral(value)));
            approximateLength += property.Name.Length + value.Length + 4;
        }

        if (attributes.Count == 0)
            return null;
        approximateLength += 16 + 2 * (attributes.Count - 1);
        return f.NewImportAttributes(Kind.WithKeyword, f.NewNodeList(attributes), false);
    }
}

internal sealed partial class NodeBuilderImpl
{
    // nodebuilderimpl.go:338. Preserve class instance clones as instance-side names.
    private static bool isClassInstanceSide(Checker checker, Type type) =>
        type.symbol != null && (type.symbol.Flags & SymbolFlags.Class) != 0 &&
        (type == checker.getDeclaredTypeOfClassOrInterface(type.symbol) ||
         (type.flags & TypeFlags.Object) != 0 && (type.objectFlags & ObjectFlags.IsClassInstanceClone) != 0);
}

internal sealed partial class NodeBuilderImpl
{
    // nodebuilderimpl.go:2436. Track the first entity-name identifier in the
    // destination scope, falling back to its actual source scope if unresolved.
    private void trackComputedName(Node expression, Node enclosingDeclaration)
    {
        var first = Ast.GetFirstIdentifier(expression);
        var symbol = ch.resolveName(enclosingDeclaration, first.Text(), SymbolFlags.Value | SymbolFlags.ExportValue, null, true, false);
        if (symbol != null)
            ctx.tracker.TrackSymbol(symbol, enclosingDeclaration, SymbolFlags.Value);
        else
        {
            var fallback = ch.resolveName(first, first.Text(), SymbolFlags.Value | SymbolFlags.ExportValue, null, true, false);
            if (fallback != null)
                ctx.tracker.TrackSymbol(fallback, enclosingDeclaration, SymbolFlags.Value);
        }
    }
}
