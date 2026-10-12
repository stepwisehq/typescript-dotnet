// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete tstransforms/metadata.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers.TsTransforms;
internal sealed class MetadataTransformer : Transformer
{
    // Upstream selectable format remains false; both original algorithms are retained.
    private static readonly bool USE_NEW_TYPE_METADATA_FORMAT = false;
    private bool legacyDecorators;
    private IEmitResolver resolver;
    private MetadataSerializer serializer;
    private ScriptTarget languageVersion;
    private bool strictNullChecks;
    private Node parent, currentLexicalScope;
    // metadata.go:24.
    internal static Transformer NewMetadataTransformer(TransformOptions options)
    {
        var tx = new MetadataTransformer { legacyDecorators = options.CompilerOptions.ExperimentalDecorators == Tristate.True, resolver = options.EmitResolver,
            languageVersion = options.CompilerOptions.GetEmitScriptTarget(), strictNullChecks = options.CompilerOptions.GetStrictOptionValue(options.CompilerOptions.StrictNullChecks) };
        return tx.NewTransformer(tx.visit, options.Context);
    }
    // metadata.go:34.
    private Node visit(Node node)
    {
        if (((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsDecorators) == 0) return node;
        switch (node.Kind)
        {
            case Kind.ClassDeclaration: return visitClassDeclaration(node.AsClassDeclaration());
            case Kind.ClassExpression: return visitClassExpression(node.AsClassExpression());
            case Kind.ObjectLiteralExpression: return visitObjectLiteralExpression(node);
            case Kind.PropertyDeclaration: return visitPropertyDeclaration(node.AsPropertyDeclaration());
            case Kind.MethodDeclaration: return visitMethodDeclaration(node.AsMethodDeclaration());
            case Kind.SetAccessor: return visitSetAccessor(node.AsSetAccessorDeclaration());
            case Kind.GetAccessor: return visitGetAccessor(node.AsGetAccessorDeclaration());
            case Kind.SourceFile:
                parent = null; currentLexicalScope = node;
                try
                {
                    serializer = new MetadataSerializer(resolver, Factory(), EmitContext(), languageVersion, strictNullChecks);
                    var updated = Visitor().VisitEachChild(node);
                    EmitContext().AddEmitHelper(updated, EmitContext().ReadEmitHelpers());
                    return updated;
                }
                finally
                {
                    setCurrentLexicalScope(null);
                    setParent(null);
                }
            case Kind.ModuleBlock: case Kind.Block: case Kind.CaseBlock:
                var oldScope = currentLexicalScope; currentLexicalScope = node;
                try { return Visitor().VisitEachChild(node); } finally { setCurrentLexicalScope(oldScope); }
            default: return Visitor().VisitEachChild(node);
        }
    }
    // metadata.go:73.
    private void setParent(Node node) => parent = node;
    // metadata.go:77.
    private void setCurrentLexicalScope(Node node) => currentLexicalScope = node;
    // metadata.go:81.
    private Node visitObjectLiteralExpression(Node node)
    {
        var old = parent;
        parent = node;
        try
        {
            return Visitor().VisitEachChild(node);
        }
        finally
        {
            setParent(old);
        }
    }
    // metadata.go:89.
    private Node visitClassExpression(ClassExpression node)
    {
        var old = parent;
        parent = node.AsNode();
        try
        {
            if (!Ast.ClassOrConstructorParameterIsDecorated(legacyDecorators, node.AsNode())) return Visitor().VisitEachChild(node.AsNode());
            var modifiers = injectClassTypeMetadata(Visitor().VisitModifiers(node.Modifiers()), node.AsNode());
            return Factory().UpdateClassExpression(
                node,
                modifiers,
                Visitor().VisitNode(node.Name()),
                Visitor().VisitNodes(node.TypeParameters),
                Visitor().VisitNodes(node.HeritageClauses),
                Visitor().VisitNodes(node.Members));
        }
        finally
        {
            setParent(old);
        }
    }
    // metadata.go:108.
    private Node visitClassDeclaration(ClassDeclaration node)
    {
        var old = parent;
        parent = node.AsNode();
        try
        {
            if (!Ast.ClassOrConstructorParameterIsDecorated(legacyDecorators, node.AsNode())) return Visitor().VisitEachChild(node.AsNode());
            var modifiers = injectClassTypeMetadata(Visitor().VisitModifiers(node.Modifiers()), node.AsNode());
            return Factory().UpdateClassDeclaration(
                node,
                modifiers,
                Visitor().VisitNode(node.Name()),
                Visitor().VisitNodes(node.TypeParameters),
                Visitor().VisitNodes(node.HeritageClauses),
                Visitor().VisitNodes(node.Members));
        }
        finally
        {
            setParent(old);
        }
    }
    // metadata.go:127.
    private Node visitPropertyDeclaration(PropertyDeclaration node)
    {
        if (!Ast.HasDecorators(node.AsNode())) return Visitor().VisitEachChild(node.AsNode());
        var modifiers = injectClassElementTypeMetadata(Visitor().VisitModifiers(node.Modifiers()), node.AsNode(), parent);
        return Factory().UpdatePropertyDeclaration(
            node,
            modifiers,
            Visitor().VisitNode(node.Name()),
            Visitor().VisitNode(node.PostfixToken),
            Visitor().VisitNode(node.Type),
            Visitor().VisitNode(node.Initializer));
    }
    // metadata.go:143.
    private Node visitMethodDeclaration(MethodDeclaration node)
    {
        if (!Ast.HasDecorators(node.AsNode()) && LegacyDecoratorsTransformer.GetDecoratorsOfParameters(node.AsNode()).Count == 0) return Visitor().VisitEachChild(node.AsNode());
        var modifiers = injectClassElementTypeMetadata(Visitor().VisitModifiers(node.Modifiers()), node.AsNode(), parent);
        return Factory().UpdateMethodDeclaration(
            node,
            modifiers,
            Visitor().VisitNode(node.AsteriskToken),
            Visitor().VisitNode(node.Name()),
            Visitor().VisitNode(node.PostfixToken),
            Visitor().VisitNodes(node.TypeParameters),
            Visitor().VisitNodes(node.Parameters),
            Visitor().VisitNode(node.Type),
            Visitor().VisitNode(node.FullSignature),
            Visitor().VisitNode(node.Body));
    }
    // metadata.go:163.
    private Node visitSetAccessor(SetAccessorDeclaration node)
    {
        if (!Ast.HasDecorators(node.AsNode()) && LegacyDecoratorsTransformer.GetDecoratorsOfParameters(node.AsNode()).Count == 0) return Visitor().VisitEachChild(node.AsNode());
        var modifiers = injectClassElementTypeMetadata(Visitor().VisitModifiers(node.Modifiers()), node.AsNode(), parent);
        return Factory().UpdateSetAccessorDeclaration(
            node,
            modifiers,
            Visitor().VisitNode(node.Name()),
            Visitor().VisitNodes(node.TypeParameters),
            Visitor().VisitNodes(node.Parameters),
            Visitor().VisitNode(node.Type),
            Visitor().VisitNode(node.FullSignature),
            Visitor().VisitNode(node.Body));
    }
    // metadata.go:181.
    private Node visitGetAccessor(GetAccessorDeclaration node)
    {
        if (!Ast.HasDecorators(node.AsNode())) return Visitor().VisitEachChild(node.AsNode());
        var modifiers = injectClassElementTypeMetadata(Visitor().VisitModifiers(node.Modifiers()), node.AsNode(), parent);
        return Factory().UpdateGetAccessorDeclaration(
            node,
            modifiers,
            Visitor().VisitNode(node.Name()),
            Visitor().VisitNodes(node.TypeParameters),
            Visitor().VisitNodes(node.Parameters),
            Visitor().VisitNode(node.Type),
            Visitor().VisitNode(node.FullSignature),
            Visitor().VisitNode(node.Body));
    }
    // metadata.go:199.
    private ModifierList injectClassTypeMetadata(ModifierList list, Node node)
    {
        var metadata = getTypeMetadata(node, node);
        if (metadata.Count > 0)
        {
            var original = list == null ? default : list.Nodes;
            if (original.Count == 0)
            {
                var result = Factory().NewModifierList(metadata);
                if (list != null) result.Loc = list.Loc;
                return result;
            }
            Slice<Node> modifiers = default;
            if (Ast.IsModifier(original[0]) && original[0].Kind is Kind.DefaultKeyword or Kind.ExportKeyword)
            {
                modifiers = modifiers.Append(original[0]);
                if (original.Count > 1 && original[1].Kind is Kind.DefaultKeyword or Kind.ExportKeyword) modifiers = modifiers.Append(original[1]);
            }
            int restStart = modifiers.Count;
            modifiers = modifiers.Append(Core.Filter(original, Ast.IsDecorator));
            modifiers = modifiers.Append(metadata);
            modifiers = modifiers.Append(Core.Filter(original.Slice2(restStart, original.Count), Ast.IsModifier));
            var res = Factory().NewModifierList(modifiers);
            res.Loc = list.Loc;
            return res;
        }
        return list;
    }
    // metadata.go:233.
    private ModifierList injectClassElementTypeMetadata(ModifierList list, Node node, Node container)
    {
        if (!Ast.IsClassLike(container)) return list;
        if (!Ast.ClassElementOrClassElementParameterIsDecorated(legacyDecorators, node, container)) return list;
        var metadata = getTypeMetadata(node, container);
        if (metadata.Count > 0)
        {
            var original = list == null ? default : list.Nodes;
            if (original.Count == 0)
            {
                var result = Factory().NewModifierList(metadata);
                if (list != null) result.Loc = list.Loc;
                return result;
            }
            Slice<Node> modifiers = default;
            modifiers = modifiers.Append(Core.Filter(original, Ast.IsDecorator));
            modifiers = modifiers.Append(metadata);
            modifiers = modifiers.Append(Core.Filter(original, Ast.IsModifier));
            var res = Factory().NewModifierList(modifiers);
            res.Loc = list.Loc;
            return res;
        }
        return list;
    }
    // metadata.go:271.
    private Slice<Node> getTypeMetadata(Node node, Node container)
    {
        if (!legacyDecorators) return default;
        return USE_NEW_TYPE_METADATA_FORMAT ? getNewTypeMetadata(node, container) : getOldTypeMetadata(node, container);
    }
    // metadata.go:282.
    private Slice<Node> getOldTypeMetadata(Node node, Node container)
    {
        Slice<Node> decorators = default;
        if (shouldAddTypeMetadata(node))
        {
            var typeMetadata = Factory().NewMetadataHelper(
                "design:type",
                serializer.SerializeTypeOfNode(
                    new() { currentLexicalScope = currentLexicalScope, currentNameScope = container }, node, container));
            decorators = decorators.Append(Factory().NewDecorator(typeMetadata));
        }
        if (shouldAddParamTypesMetadata(node))
        {
            var paramTypesMetadata = Factory().NewMetadataHelper(
                "design:paramtypes",
                serializer.SerializeParameterTypesOfNode(
                    new() { currentLexicalScope = currentLexicalScope, currentNameScope = container }, node, container));
            decorators = decorators.Append(Factory().NewDecorator(paramTypesMetadata));
        }
        if (shouldAddReturnTypeMetadata(node))
        {
            var returnTypeMetadata = Factory().NewMetadataHelper(
                "design:returntype",
                serializer.SerializeReturnTypeOfNode(
                    new() { currentLexicalScope = currentLexicalScope, currentNameScope = container }, node));
            decorators = decorators.Append(Factory().NewDecorator(returnTypeMetadata));
        }
        return decorators;
    }
    // metadata.go:299. Both source formats are retained; current source selector remains false.
    private Slice<Node> getNewTypeMetadata(Node node, Node container)
    {
        Slice<Node> properties = default;
        if (shouldAddTypeMetadata(node))
        {
            properties = properties.Append(Factory().NewPropertyAssignment(
                null,
                Factory().NewIdentifier("type"),
                null,
                null,
                Factory().NewArrowFunction(
                    null,
                    null,
                    Factory().NewNodeList(Slice<Node>.Make(0)),
                    null,
                    null,
                    Factory().NewToken(Kind.EqualsGreaterThanToken),
                    serializer.SerializeTypeOfNode(
                        new() { currentLexicalScope = currentLexicalScope, currentNameScope = container }, node, container))));
        }
        if (shouldAddParamTypesMetadata(node))
        {
            properties = properties.Append(Factory().NewPropertyAssignment(
                null,
                Factory().NewIdentifier("paramTypes"),
                null,
                null,
                Factory().NewArrowFunction(
                    null,
                    null,
                    Factory().NewNodeList(Slice<Node>.Make(0)),
                    null,
                    null,
                    Factory().NewToken(Kind.EqualsGreaterThanToken),
                    serializer.SerializeParameterTypesOfNode(
                        new() { currentLexicalScope = currentLexicalScope, currentNameScope = container }, node, container))));
        }
        if (shouldAddReturnTypeMetadata(node))
        {
            properties = properties.Append(Factory().NewPropertyAssignment(
                null,
                Factory().NewIdentifier("returnType"),
                null,
                null,
                Factory().NewArrowFunction(
                    null,
                    null,
                    Factory().NewNodeList(Slice<Node>.Make(0)),
                    null,
                    null,
                    Factory().NewToken(Kind.EqualsGreaterThanToken),
                    serializer.SerializeReturnTypeOfNode(
                        new() { currentLexicalScope = currentLexicalScope, currentNameScope = container }, node))));
        }
        if (properties.Count > 0)
        {
            var typeInfoMetadata = Factory().NewMetadataHelper(
                "design:typeinfo", Factory().NewObjectLiteralExpression(Factory().NewNodeList(properties), true));
            return Slice<Node>.FromArray([Factory().NewDecorator(typeInfoMetadata)]);
        }
        return default;
    }
    // metadata.go:366.
    private static bool shouldAddTypeMetadata(Node node) => node.Kind is Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor or Kind.PropertyDeclaration;
    // metadata.go:381.
    private static bool shouldAddReturnTypeMetadata(Node node) => node.Kind == Kind.MethodDeclaration;
    // metadata.go:392.
    private static bool shouldAddParamTypesMetadata(Node node) => node.Kind switch
    { Kind.ClassDeclaration or Kind.ClassExpression => Ast.GetFirstConstructorWithBody(node) != null, Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor => true, _ => false };
}
