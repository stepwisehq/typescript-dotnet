// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// C# port of tsc/internal/checker/nodecopy.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Declaration emit and retained diagnostics share the original syntax reuse/mapper path.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    private Node reuseNode(Node node) => node == null ? null : tryReuseExistingNodeHelper(node);
    // nodecopy.go:20.
    internal Node tryJSTypeNodeToTypeNode(Node node) => reuseNode(node);
    // nodecopy.go:280. Fake serialization scopes do not establish a unique-symbol owner.
    private Node getEnclosingDeclarationIgnoringFakeScope()
    {
        var enclosing = ctx.enclosingDeclaration;
        while (enclosing != null && getLinks(enclosing).fakeScopeForSignatureDeclaration != null)
            enclosing = enclosing.Parent;
        return enclosing;
    }

    private Node reuseName(Node node, bool isMethod)
    {
        var res = reuseNode(node);
        if (res == null)
            return res;
        var(text, hasText) = Ast.TryGetTextValueOfPropertyName(res);
        if (!hasText)
            return res;
        var kind = classifyPropertyName(text, Ast.IsStringLiteral(res), isMethod);
        if (Ast.IsIdentifier(res) && kind == propertyNameNodeKind.Identifier || Ast.IsStringLiteral(res) && kind == propertyNameNodeKind.StringLiteral)
            return res;
        Node renamed;
        if (kind == propertyNameNodeKind.Identifier)
            renamed = newIdentifier(text, null);
        else if (kind == propertyNameNodeKind.StringLiteral)
            renamed = f.NewStringLiteralWithTextValue(text, TokenFlags.None);
        else
            return res;
        e.SetOriginal(renamed, res);
        return setTextRange(renamed, res);
    }

    private Node reuseTypeNode(Node node)
    {
        if (node == null)
            return node;
        var result = reuseNode(node);
        if (result != null)
        {
            // nodecopy.go:63. Reused syntax bypasses type serialization, so hover
            // still probes its named references for the next expansion level.
            if (ctx.maxExpansionDepth >= 0 && !ctx.canIncreaseExpansionDepth)
                walkNodeForExpandability(node);
            return result;
        }
        ctx.tracker.ReportInferenceFallback(node);
        return typeToTypeNode(getTypeFromTypeNode(node, false));
    }

    // nodecopy.go:78. Retain the original preorder and first-expandable short circuit.
    private void walkNodeForExpandability(Node node)
    {
        if (ctx.canIncreaseExpansionDepth || node == null)
            return;
        if (Ast.IsTypeReferenceNode(node) || Ast.IsExpressionWithTypeArguments(node) || Ast.IsTypePredicateNode(node) || Ast.IsImportTypeNode(node))
        {
            var type = getTypeFromTypeNode(node, false);
            if (type != null)
            {
                checkTypeExpandability(type);
                if (ctx.canIncreaseExpansionDepth)
                    return;
            }
        }
        node.ForEachChild(child =>
        {
            walkNodeForExpandability(child);
            return ctx.canIncreaseExpansionDepth;
        });
    }

    // nodecopy.go:100/184/192. Restore the actual recovery tracker and
    // symbol-recording boundary now reached by named type references.
    private sealed class recoveryBoundary
    {
        internal NodeBuilderContext ctx;
        internal bool hadError;
        internal Slice<Action> deferredReports;
        internal SymbolTrackerImpl oldTracker;
        internal Slice<TrackedSymbolArgs> oldTrackedSymbols, trackedSymbols;
        internal bool oldEncounteredError;
        internal int oldApproximateLength;
        internal originalRecoveryScopeState startRecoveryScope() => new(ctx.trackedSymbols.Length, deferredReports.Length, hadError);
        internal void endRecoveryScope(originalRecoveryScopeState state)
        {
            hadError = state.hadError;
            ctx.trackedSymbols = ctx.trackedSymbols.Slice2(0, state.trackedSymbolsTop);
            deferredReports = deferredReports.Slice2(0, state.unreportedErrorsTop);
        }

        internal void markError(Action callback)
        {
            hadError = true;
            if (callback != null)
                deferredReports = deferredReports.Append(callback);
        }
    }

    private readonly record struct originalRecoveryScopeState(int trackedSymbolsTop, int unreportedErrorsTop, bool hadError);
    private sealed class wrappingTracker(SymbolTrackerImpl wrapped, recoveryBoundary bound) : TypeScript.NodeBuilding.SymbolTracker
    {
        public bool TrackSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
        {
            bound.trackedSymbols = bound.trackedSymbols.Append(new TrackedSymbolArgs(symbol, enclosingDeclaration, meaning));
            return false;
        }

        public void PushErrorFallbackNode(Node node) => wrapped.PushErrorFallbackNode(node);
        public void PopErrorFallbackNode() => wrapped.PopErrorFallbackNode();
        public void ReportNonlocalAugmentation(SourceFile containingFile, Symbol parentSymbol, Symbol augmentingSymbol) => wrapped.ReportNonlocalAugmentation(containingFile, parentSymbol, augmentingSymbol);
        public void ReportPrivateInBaseOfClassExpression(string propertyName) => bound.markError(() => wrapped.ReportPrivateInBaseOfClassExpression(propertyName));
        public void ReportNonSerializableProperty(string propertyName) => bound.markError(() => wrapped.ReportNonSerializableProperty(propertyName));
        public void ReportCyclicStructureError() => bound.markError(wrapped.ReportCyclicStructureError);
        public void ReportInaccessibleThisError() => bound.markError(wrapped.ReportInaccessibleThisError);
        public void ReportInaccessibleUniqueSymbolError() => bound.markError(wrapped.ReportInaccessibleUniqueSymbolError);
        public void ReportInferenceFallback(Node node) => wrapped.ReportInferenceFallback(node);
        public void ReportTruncationError() => wrapped.ReportTruncationError();
        public void ReportLikelyUnsafeImportRequiredError(string specifier, string symbolName) => bound.markError(() => wrapped.ReportLikelyUnsafeImportRequiredError(specifier, symbolName));
    }

    private recoveryBoundary createRecoveryBoundary()
    {
        ch.checkNotCanceled();
        var bound = new recoveryBoundary
        {
            ctx = ctx,
            oldTracker = ctx.tracker,
            oldTrackedSymbols = ctx.trackedSymbols,
            oldEncounteredError = encounteredError,
            oldApproximateLength = approximateLength
        };
        ctx.tracker = SymbolTrackerImpl.NewSymbolTrackerImpl(ctx, new wrappingTracker(ctx.tracker, bound));
        ctx.trackedSymbols = default;
        return bound;
    }

    private bool finalizeBoundary(recoveryBoundary bound)
    {
        ctx.tracker = bound.oldTracker;
        ctx.trackedSymbols = bound.oldTrackedSymbols;
        encounteredError = bound.oldEncounteredError;
        approximateLength = bound.oldApproximateLength;
        foreach (var callback in bound.deferredReports)
            callback();
        if (bound.hadError)
            return false;
        foreach (var argument in bound.trackedSymbols)
            ctx.tracker.TrackSymbol(argument.symbol, argument.enclosingDeclaration, argument.meaning);
        return true;
    }

    private Node tryReuseExistingNodeHelper(Node existing)
    {
        var bound = createRecoveryBoundary();
        var visitor = getExistingNodeTreeVisitor(bound);
        var transformed = visitor.VisitNode(existing);
        if (!finalizeBoundary(bound))
            return null;
        approximateLength += existing.End() - existing.Pos();
        return transformed;
    }

    // nodecopy.go:234–278. Reused import types are rewritten through real module naming.
    private ByteStringValue getModuleSpecifierOverride(Node parent, Node literal)
    {
        if (ctx.enclosingFile != Ast.GetSourceFileOfNode(literal))
        {
            var import = parent.AsImportTypeNode();
            var mode = ResolutionMode.None;
            if (import.Attributes != null)
                (mode, _) = import.Attributes.GetResolutionModeOverride(null);
            var name = literal.TextValue();
            var originalName = name;
            var nodeSymbol = tryGetResolvedSymbolFromTypeNode(parent);
            var meaning = import.IsTypeOf ? SymbolFlags.Value : SymbolFlags.Type;
            Symbol parentSymbol = null;
            if (nodeSymbol != null && ch.IsSymbolAccessible(nodeSymbol, ctx.enclosingDeclaration, meaning, false).Accessibility == TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible)
                parentSymbol = lookupSymbolChain(nodeSymbol, meaning, true)[0];
            if (parentSymbol != null && Checker.IsExternalModuleSymbol(parentSymbol))
                name = getSpecifierForModuleSymbol(parentSymbol, mode).specifier;
            else
            {
                var file = ch.getExternalModuleFileFromDeclaration(parent);
                if (file != null)
                    name = getSpecifierForModuleSymbol(file.Symbol, mode).specifier;
            }

            if (name.Length > 0 && name.Contains("/node_modules/"u8))
            {
                ctx.encounteredError = true;
                ctx.tracker.ReportLikelyUnsafeImportRequiredError(name.Materialize(), "");
            }

            if (!name.EqualsValue(originalName))
                return name;
        }

        return default;
    }

    private Node rewriteModuleSpecifier(Node parent, Node literal)
    {
        var name = getModuleSpecifierOverride(parent, literal);
        if (name.Length == 0)
            return literal;
        var result = f.NewStringLiteralWithTextValue(name, TokenFlags.None);
        e.SetOriginal(result, literal);
        return result;
    }

    private NodeVisitor getExistingNodeTreeVisitor(recoveryBoundary bound)
    {
        NodeVisitor visitor = null;
        bool nonLocalNode = true;
        // nodecopy.go:292/314. Retain resolution in both original and target
        // scopes; a spelling match is insufficient when names are shadowed.
        Node attachSymbolToLeftmostIdentifier(Node leftmost, Node node, Symbol symbol)
        {
            NodeVisitor attaching = null;
            Node Attach(Node current)
            {
                if (current == leftmost)
                {
                    Node name = null;
                    if (symbol != null)
                    {
                        var type = ch.getDeclaredTypeOfSymbol(symbol);
                        if ((symbol.Flags & SymbolFlags.TypeParameter) != 0)
                            name = typeParameterToName(type);
                    }

                    name ??= newIdentifier(current.TextValue(), symbol);
                    name = setTextRange(name, current);
                    e.AddEmitFlags(name, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                    return name;
                }

                return setTextRange(attaching.VisitEachChild(current), current);
            }

            attaching = new NodeVisitor(Attach, f, new());
            return Attach(node);
        }

        (bool introducesError, Node newName, Symbol symbol) trackExistingEntityName(Node node)
        {
            var leftmost = Ast.GetFirstIdentifier(node);
            // nodecopy.go:323. CommonJS exports names cannot be transplanted
            // from a JSDoc annotation; use the genuine semantic serialization.
            if (Ast.IsInJSFile(node) && (Ast.IsExportsIdentifier(leftmost) ||
                Ast.IsModuleExportsAccessExpression(leftmost.Parent) ||
                Ast.IsQualifiedName(leftmost.Parent) &&
                Ast.IsModuleIdentifier(leftmost.Parent.AsQualifiedName().Left) &&
                Ast.IsExportsIdentifier(leftmost.Parent.AsQualifiedName().Right)))
                return (true, setTextRange(f.DeepCloneNode(node), node), null);
            var meaning = Checker.getMeaningOfEntityNameReference(node);
            if (Ast.IsThisIdentifier(leftmost))
            {
                var thisSymbol = ch.getSymbolOfDeclaration(ch.getThisContainer(leftmost, false, false));
                bool inaccessible = ch.IsSymbolAccessible(thisSymbol, leftmost, meaning, false).Accessibility !=
                    TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible;
                if (inaccessible)
                    ctx.tracker.ReportInaccessibleThisError();
                return (inaccessible, attachSymbolToLeftmostIdentifier(leftmost, node, thisSymbol), null);
            }
            var symbol = ch.resolveEntityName(leftmost, meaning, true, true, null);
            if (ctx.enclosingDeclaration != null && !(symbol != null && (symbol.Flags & SymbolFlags.TypeParameter) != 0))
            {
                symbol = ch.getExportSymbolOfValueSymbolIfExported(symbol);
                var atLocation = ch.resolveEntityName(leftmost, meaning, true, true, ctx.enclosingDeclaration);
                if (atLocation == ch.unknownSymbol || atLocation == null && symbol != null || atLocation != null && symbol != null && ch.getSymbolIfSameReference(ch.getExportSymbolOfValueSymbolIfExported(atLocation), symbol) == null)
                {
                    if (atLocation != ch.unknownSymbol)
                        ctx.tracker.ReportInferenceFallback(node);
                    return (true, setTextRange(f.DeepCloneNode(node), node), symbol);
                }

                symbol = atLocation;
            }

            if (symbol != null)
            {
                // A parameter resolved in this context is already visible.
                if ((symbol.Flags & SymbolFlags.FunctionScopedVariable) != 0 && symbol.ValueDeclaration != null && (Ast.IsPartOfParameterDeclaration(symbol.ValueDeclaration) || Ast.IsJSDocParameterTag(symbol.ValueDeclaration)))
                    return (false, attachSymbolToLeftmostIdentifier(leftmost, node, symbol), null);
                bool introducesError = (symbol.Flags & SymbolFlags.TypeParameter) == 0 && !Ast.IsDeclarationName(node) && ch.IsSymbolAccessible(symbol, ctx.enclosingDeclaration, meaning, false).Accessibility != TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible;
                if (introducesError)
                    ctx.tracker.ReportInferenceFallback(node);
                else
                    ctx.tracker.TrackSymbol(symbol, ctx.enclosingDeclaration, meaning);
                return (introducesError, attachSymbolToLeftmostIdentifier(leftmost, node, symbol), null);
            }

            return (false, setTextRange(f.DeepCloneNode(node), node), null);
        }

        Node tryVisitTypeReference(Node node)
        {
            if (Ast.IsConstTypeReference(node))
                return null;
            var symbol = tryGetResolvedSymbolFromTypeNode(node);
            if (symbol == null)
                return null;
            if ((symbol.Flags & SymbolFlags.TypeParameter) != 0 && ctx.mapper != null && Checker.getMappedType(ch.getDeclaredTypeOfSymbol(symbol), ctx.mapper) != ch.getDeclaredTypeOfSymbol(symbol))
                return null;
            if (!canReuseExistingJSTypeNode(node, getTypeFromTypeNode(node, false)))
                return null;
            var(error, name, _) = trackExistingEntityName(node.AsTypeReferenceNode().TypeName);
            var arguments = visitor.VisitNodes(node.AsTypeReferenceNode().TypeArguments);
            if (!error)
                return setTextRange(f.UpdateTypeReferenceNode(node.AsTypeReferenceNode(), name, arguments), node);
            var serialized = serializeTypeName(node.AsTypeReferenceNode().TypeName, false, arguments);
            return serialized != null ? setTextRange(serialized, node.AsTypeReferenceNode().TypeName) : null;
        }

        Node tryVisitSimpleTypeNode(Node node)
        {
            var inner = Ast.SkipParentheses(node);
            if (inner.Kind == Kind.TypeReference)
                return tryVisitTypeReference(inner);
            if (inner.Kind == Kind.TypeQuery)
                return tryVisitTypeQuery(inner);
            if (inner.Kind == Kind.IndexedAccessType)
                return tryVisitIndexedAccess(inner);
            if (inner.Kind == Kind.TypeOperator && inner.AsTypeOperatorNode().Operator == Kind.KeyOfKeyword)
                return tryVisitKeyOf(inner);
            return visitor.VisitNode(node);
        }

        Node tryVisitIndexedAccess(Node node)
        {
            var data = node.AsIndexedAccessTypeNode();
            var objectType = tryVisitSimpleTypeNode(data.ObjectType);
            return objectType == null ? null : setTextRange(f.UpdateIndexedAccessTypeNode(data, objectType, visitor.VisitNode(data.IndexType)), node);
        }

        Node tryVisitKeyOf(Node node)
        {
            var data = node.AsTypeOperatorNode();
            var type = tryVisitSimpleTypeNode(data.Type);
            return type == null ? null : setTextRange(f.UpdateTypeOperatorNode(data, data.Operator, type), node);
        }

        Node tryVisitTypeQuery(Node node)
        {
            var data = node.AsTypeQueryNode();
            var(error, name, _) = trackExistingEntityName(data.ExprName);
            if (!error)
                return setTextRange(f.UpdateTypeQueryNode(data, name, visitor.VisitNodes(data.TypeArguments)), node);
            var serialized = serializeTypeName(data.ExprName, true, visitor.VisitNodes(data.TypeArguments));
            return serialized == null ? null : setTextRange(serialized, data.ExprName);
        }

        Node worker(Node node)
        {
            // nodecopy.go:467–536. Original JSDoc syntax is converted before ordinary traversal.
            if (node.Kind == Kind.JSDocTypeExpression)
                return visitor.VisitNode(node.AsJSDocTypeExpression().Type);
            if (node.Kind == Kind.JSDocAllType)
                return f.NewKeywordTypeNode(Kind.AnyKeyword);
            if (node.Kind == Kind.JSDocNullableType)
                return f.NewUnionTypeNode(f.NewNodeList(Slice<Node>.FromArray([visitor.VisitNode(node.AsJSDocNullableType().Type), f.NewLiteralTypeNode(f.NewKeywordExpression(Kind.NullKeyword))])));
            if (node.Kind == Kind.JSDocOptionalType)
                return f.NewUnionTypeNode(f.NewNodeList(Slice<Node>.FromArray([visitor.VisitNode(node.AsJSDocOptionalType().Type), f.NewKeywordTypeNode(Kind.UndefinedKeyword)])));
            if (node.Kind == Kind.JSDocNonNullableType)
                return visitor.VisitNode(node.AsJSDocNonNullableType().Type);
            if (node.Kind == Kind.JSDocVariadicType)
                return f.NewArrayTypeNode(visitor.VisitNode(node.AsJSDocVariadicType().Type));
            if (node.Kind == Kind.JSDocTypeLiteral)
            {
                Slice<Node> members = default;
                foreach (var tag in node.AsJSDocTypeLiteral().JSDocPropertyTags)
                {
                    if (tag.Kind is not (Kind.JSDocPropertyTag or Kind.JSDocParameterTag))
                        continue;
                    var name = tag.Name();
                    var targetName = Ast.IsIdentifier(name) ? name : name.AsQualifiedName().Right;
                    var optional = tag.AsJSDocParameterOrPropertyTag().IsBracketed || tag.TypeExpression() != null && tag.TypeExpression().Kind == Kind.JSDocOptionalType;
                    members = members.Append(f.NewPropertySignatureDeclaration(null, visitor.VisitNode(targetName), optional ? f.NewToken(Kind.QuestionToken) : null, visitor.VisitNode(tag.TypeExpression()), null));
                }

                return f.NewTypeLiteralNode(f.NewNodeList(members));
            }

            if (Ast.IsLiteralImportTypeNode(node))
            {
                var import = node.AsImportTypeNode();
                if (import.Attributes != null && import.Attributes.AsImportAttributes().Token == Kind.AssertKeyword)
                {
                    bound.markError(null);
                    return node;
                }

                if (getTypeFromTypeNode(node, true) == null)
                {
                    bound.markError(null);
                    return node;
                }

                var originalSpecifier = import.Argument.AsLiteralTypeNode().Literal;
                var specifier = rewriteModuleSpecifier(node, originalSpecifier);
                if (specifier == originalSpecifier)
                    specifier = visitor.VisitNode(specifier);
                var argument = specifier == originalSpecifier ? import.Argument : f.NewLiteralTypeNode(specifier);
                return f.UpdateImportTypeNode(import, import.IsTypeOf, argument, visitor.VisitNode(import.Attributes), visitor.VisitNode(import.Qualifier), visitor.VisitNodes(import.TypeArguments));
            }

            // nodecopy.go:649–661. Dynamic-name reuse keeps the exact AllowUnresolvedNames exception.
            if (node.Name() != null && node.Name().Kind == Kind.ComputedPropertyName && !ch.hasLateBindableName(node))
            {
                if (!Ast.HasDynamicName(node))
                    return visitor.VisitEachChild(node);
                bool removeDeclaration = !((ctx.internalFlags & TypeScript.NodeBuilding.InternalFlags.AllowUnresolvedNames) != 0 && Ast.IsEntityNameExpression(node.Name().AsComputedPropertyName().Expression) && (ch.checkComputedPropertyName(node.Name()).flags & TypeFlags.Any) != 0);
                if (removeDeclaration)
                    return null;
            }

            if (Ast.IsComputedPropertyName(node) && Ast.IsEntityNameExpression(node.AsComputedPropertyName().Expression))
            {
                var(introducesError, result, _) = trackExistingEntityName(node.AsComputedPropertyName().Expression);
                if (!introducesError)
                    return f.UpdateComputedPropertyName(node.AsComputedPropertyName(), result);
                bound.markError(null);
                return visitor.VisitEachChild(node);
            }

            if (Ast.IsKeywordTypeNode(node) || node.Kind is Kind.Identifier or Kind.NumericLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword or Kind.ReadonlyKeyword or Kind.QuestionToken or Kind.MinusToken or Kind.PlusToken or Kind.DotDotDotToken or Kind.InKeyword or Kind.OutKeyword or Kind.ConstKeyword or Kind.AbstractKeyword)
                return visitor.VisitEachChild(node);
            if (node.Kind == Kind.PropertySignature)
            {
                var prop = node.AsPropertySignatureDeclaration();
                if (prop.Type == null && prop.Initializer == null)
                {
                    var visited = visitor.VisitEachChild(node);
                    if (visited == node)
                        visited = setTextRange(node.Clone(f), node);
                    var data = visited.AsPropertySignatureDeclaration();
                    return f.UpdatePropertySignatureDeclaration(data, data.Modifiers(), data.Name(), data.PostfixToken, f.NewKeywordTypeNode(Kind.AnyKeyword), null);
                }

                return visitor.VisitEachChild(node);
            }

            if (Ast.IsFunctionLike(node) && node.Type() == null || Ast.IsPropertyDeclaration(node) && node.Type() == null && node.Initializer() == null || Ast.IsParameterDeclaration(node) && node.Type() == null && node.Initializer() == null)
            {
                var visited = visitor.VisitEachChild(node);
                if (visited == node)
                    visited = setTextRange(node.Clone(f), node);
                var anyType = f.NewKeywordTypeNode(Kind.AnyKeyword);
                switch (visited.Kind)
                {
                    case Kind.PropertyDeclaration:
                        return f.UpdatePropertyDeclaration(visited.AsPropertyDeclaration(), visited.Modifiers(), visited.Name(), visited.PostfixToken(), anyType, null);
                    case Kind.JSDocSignature:
                        var jsdoc = visited.AsJSDocSignature();
                        return f.UpdateJSDocSignature(jsdoc, jsdoc.TypeParameters, jsdoc.Parameters, anyType);
                    case Kind.Parameter:
                        var parameter = visited.AsParameterDeclaration();
                        return f.UpdateParameterDeclaration(parameter, null, parameter.DotDotDotToken, visited.Name(), parameter.QuestionToken, anyType, null);
                    case Kind.MethodSignature:
                        var method = visited.AsMethodSignatureDeclaration();
                        return f.UpdateMethodSignatureDeclaration(method, visited.Modifiers(), visited.Name(), method.PostfixToken, method.TypeParameters, method.Parameters, anyType);
                    case Kind.CallSignature:
                        var call = visited.AsCallSignatureDeclaration();
                        return f.UpdateCallSignatureDeclaration(call, call.TypeParameters, call.Parameters, anyType);
                    case Kind.ConstructSignature:
                        var construct = visited.AsConstructSignatureDeclaration();
                        return f.UpdateConstructSignatureDeclaration(construct, construct.TypeParameters, construct.Parameters, anyType);
                    case Kind.IndexSignature:
                        var index = visited.AsIndexSignatureDeclaration();
                        return f.UpdateIndexSignatureDeclaration(index, visited.Modifiers(), index.Parameters, anyType);
                    case Kind.ConstructorType:
                        var constructor = visited.AsConstructorTypeNode();
                        return f.UpdateConstructorTypeNode(constructor, visited.Modifiers(), constructor.TypeParameters, constructor.Parameters, anyType);
                    case Kind.FunctionType:
                        var function = visited.AsFunctionTypeNode();
                        return f.UpdateFunctionTypeNode(function, function.TypeParameters, function.Parameters, anyType);
                }
            }

            if (node.Kind == Kind.TypePredicate)
            {
                var predicate = node.AsTypePredicateNode();
                Node parameterName;
                if (Ast.IsIdentifier(predicate.ParameterName))
                {
                    var(introducesError, name, _) = trackExistingEntityName(predicate.ParameterName);
                    if (introducesError)
                        bound.markError(null);
                    parameterName = name;
                }
                else
                    parameterName = predicate.ParameterName.Clone(f);
                return f.UpdateTypePredicateNode(predicate, visitor.VisitNode(predicate.AssertsModifier), parameterName, visitor.VisitNode(predicate.Type));
            }

            if (Ast.IsTypeParameterDeclaration(node))
            {
                var(_, name, _) = trackExistingEntityName(node.Name());
                var parameter = node.AsTypeParameterDeclaration();
                return f.UpdateTypeParameterDeclaration(parameter, visitor.VisitModifiers(node.Modifiers()), name, visitor.VisitNode(parameter.Constraint), visitor.VisitNode(parameter.Expression), visitor.VisitNode(parameter.DefaultType));
            }

            if (Ast.IsConditionalTypeNode(node))
            {
                var conditional = node.AsConditionalTypeNode();
                var check = visitor.VisitNode(conditional.CheckType);
                var dispose = enterNewScope(node, default, ch.getInferTypeParameters(node), default, null);
                var extendsType = visitor.VisitNode(conditional.ExtendsType);
                var trueType = visitor.VisitNode(conditional.TrueType);
                dispose();
                var falseType = visitor.VisitNode(conditional.FalseType);
                return f.UpdateConditionalTypeNode(conditional, check, extendsType, trueType, falseType);
            }

            if (Ast.IsIndexedAccessTypeNode(node))
            {
                var indexed = tryVisitIndexedAccess(node);
                if (indexed != null)
                    return indexed;
                bound.markError(null);
                return node;
            }

            if (Ast.IsTypeQueryNode(node))
            {
                var query = tryVisitTypeQuery(node);
                if (query != null)
                    return query;
                bound.markError(null);
                return node;
            }

            if (Ast.IsTypeOperatorNode(node))
            {
                var typeOperator = node.AsTypeOperatorNode();
                if (typeOperator.Operator == Kind.UniqueKeyword && typeOperator.Type.Kind == Kind.SymbolKeyword)
                {
                    var enclosing = getEnclosingDeclarationIgnoringFakeScope();
                    if (Ast.FindAncestor(node, ancestor => ancestor == enclosing) == null)
                    {
                        bound.markError(null);
                        return node;
                    }
                }
                else if (typeOperator.Operator == Kind.KeyOfKeyword)
                {
                    var key = tryVisitKeyOf(node);
                    if (key != null)
                        return key;
                    bound.markError(null);
                    return node;
                }
            }

            if (Ast.IsTupleTypeNode(node) || (ctx.flags & TypeScript.NodeBuilding.Flags.MultilineObjectLiterals) == 0 && Ast.IsTypeLiteralNode(node) || Ast.IsMappedTypeNode(node))
            {
                var res = visitor.VisitEachChild(node);
                if (res == node)
                    res = setTextRange(res.Clone(f), node);
                e.AddEmitFlags(res, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                return res;
            }

            if (Ast.IsStringLiteralLike(node))
            {
                var clone = node.Clone(f);
                // nodecopy.go:823. Reused literal types honor the requested quote style.
                if (Ast.IsStringLiteral(node) && (ctx.flags & TypeScript.NodeBuilding.Flags.UseSingleQuotesForStringLiteralType) != 0 && (node.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) == 0)
                    clone.AsStringLiteral().TokenFlags ^= TokenFlags.SingleQuote;
                e.AddEmitFlags(clone, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                return clone;
            }

            if (node.Kind == Kind.ThisType)
                return node;
            if (node.Kind == Kind.TypeReference)
            {
                if (node.AsTypeReferenceNode().TypeName.Kind == Kind.Identifier && node.AsTypeReferenceNode().TypeName.TextValue().Length == 0)
                {
                    var replacement = f.NewKeywordTypeNode(Kind.AnyKeyword);
                    e.SetOriginal(replacement, node);
                    return replacement;
                }

                var reference = tryVisitTypeReference(node);
                if (reference != null)
                    return reference;
                bound.markError(null);
                return node;
            }

            if (node.Kind is Kind.LiteralType or Kind.ParenthesizedType or Kind.PrefixUnaryExpression or Kind.UnionType or Kind.IntersectionType or Kind.FunctionType or Kind.CallSignature or Kind.MethodSignature or Kind.Parameter or Kind.ArrayType or Kind.TupleType or Kind.OptionalType or Kind.RestType or Kind.NamedTupleMember or Kind.ConditionalType or Kind.InferType or Kind.IndexedAccessType or Kind.TypeOperator or Kind.TypeParameter or Kind.MappedType or Kind.ConstructSignature or Kind.ConstructorType or Kind.TemplateLiteralType or Kind.TemplateLiteralTypeSpan or Kind.IndexSignature)
                return visitor.VisitEachChild(node);
            return visitor.VisitEachChild(node);
        }

        visitor = new NodeVisitor(node =>
        {
            if (bound.hadError)
                return node;
            var recovery = bound.startRecoveryScope();
            Action exit = null;
            if (Ast.IsFunctionLike(node) || Ast.IsMappedTypeNode(node))
            {
                Slice<Symbol> parameters = default;
                Slice<Type> typeParameters;
                if (Ast.IsFunctionLike(node))
                {
                    var signature = ch.getSignatureFromDeclaration(node);
                    parameters = signature.parameters;
                    typeParameters = signature.typeParameters;
                }
                else
                    typeParameters = Slice<Type>.FromArray([ch.getDeclaredTypeOfTypeParameter(ch.getSymbolOfDeclaration(node.AsMappedTypeNode().TypeParameter))]);
                exit = enterNewScope(node, parameters, typeParameters, default, null);
            }

            var result = worker(node);
            exit?.Invoke();
            if (result == node && !Ast.NodeIsSynthesized(node))
                result = f.DeepCloneNode(node);
            result = setTextRange(result, node);
            if (bound.hadError)
            {
                if (Ast.IsTypeNode(node) && node.Kind != Kind.TypePredicate)
                {
                    bound.endRecoveryScope(recovery);
                    return typeToTypeNode(getTypeFromTypeNode(node, false));
                }

                return setTextRange(node.Clone(f), node);
            }

            return result;
        }, f, new NodeVisitorHooks { VisitNodes = (nodes, v) =>
        {
            var res = v.VisitNodes(nodes);
            if (nonLocalNode && res != null)
            {
                if (res == nodes)
                    res = nodes.Clone(f);
                res.Loc = new(-1, -1);
            }

            return res;
        }, VisitNode = (node, v) =>
        {
            bool old = nonLocalNode;
            nonLocalNode = ctx.enclosingFile == null || ctx.enclosingFile != Ast.GetSourceFileOfNode(mostOriginal(node));
            var res = v.VisitNode(node);
            nonLocalNode = old;
            return res;
        } });
        return visitor;
    }
}
