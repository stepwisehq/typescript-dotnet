// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete tstransforms/typeserializer.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using Factory = TypeScript.DiagnosticPrinting.NodeFactory;
namespace TypeScript.Transformers.TsTransforms;
internal sealed class MetadataSerializer
{
    internal struct Context
    {
        internal Node currentLexicalScope, currentNameScope;
        internal bool serializingConditionalTypeBranch;
    }
    private readonly IEmitResolver resolver;
    private readonly ScriptTarget languageVersion;
    private readonly bool strictNullChecks;
    private readonly Factory f;
    private readonly EmitContext ec;
    private Context c;
    // typeserializer.go:26. Managed constructor represents newMetadataSerializer.
    internal MetadataSerializer(IEmitResolver resolver, Factory f, EmitContext ec, ScriptTarget languageVersion, bool strictNullChecks)
    {
        this.resolver = resolver;
        this.f = f;
        this.ec = ec;
        this.languageVersion = languageVersion;
        this.strictNullChecks = strictNullChecks;
    }
    // typeserializer.go:30.
    private void setContext(Context context) => c = context;
    // typeserializer.go:34. Context is copied by value and restored on every exit.
    internal Node SerializeTypeOfNode(Context context, Node node, Node container)
    {
        var old = c;
        c = context;
        try
        {
            return serializeTypeOfNode(node, container);
        }
        finally
        {
            setContext(old);
        }
    }
    // typeserializer.go:41.
    internal Node SerializeParameterTypesOfNode(Context context, Node node, Node container)
    {
        var old = c;
        c = context;
        try
        {
            return serializeParameterTypesOfNode(node, container);
        }
        finally
        {
            setContext(old);
        }
    }
    // typeserializer.go:48.
    internal Node SerializeReturnTypeOfNode(Context context, Node node)
    {
        var old = c;
        c = context;
        try
        {
            return serializeReturnTypeOfNode(node);
        }
        finally
        {
            setContext(old);
        }
    }
    // typeserializer.go:55.
    internal static Node GetSetAccessorValueParameter(SetAccessorDeclaration node)
    {
        if (node != null && node.Parameters.Nodes.Count > 0)
        {
            if (node.Parameters.Nodes.Count >= 2 && Ast.IsThisParameter(node.Parameters.Nodes[0])) return node.Parameters.Nodes[1];
            return node.Parameters.Nodes[0];
        }
        return null;
    }
    // typeserializer.go:70.
    private static Node getSetAccessorTypeAnnotationNode(SetAccessorDeclaration node)
    { var parameter = GetSetAccessorValueParameter(node); return parameter != null && parameter.Type() != null ? parameter.Type() : null; }
    // typeserializer.go:78.
    private static Node getAccessorTypeNode(Node node, Node container)
    {
        var accessors = Ast.GetAllAccessorDeclarations(container.Members(), node);
        if (accessors.SetAccessor != null) return getSetAccessorTypeAnnotationNode(accessors.SetAccessor);
        if (accessors.GetAccessor != null) return accessors.GetAccessor.Type;
        return null;
    }
    // typeserializer.go:93.
    private Node serializeTypeOfNode(Node node, Node container)
    {
        switch (node.Kind)
        {
            case Kind.PropertyDeclaration: case Kind.Parameter: return serializeTypeNode(node.Type());
            case Kind.GetAccessor: case Kind.SetAccessor: return serializeTypeNode(getAccessorTypeNode(node, container));
            case Kind.ClassDeclaration: case Kind.ClassExpression: case Kind.MethodDeclaration: return f.NewIdentifier("Function");
            default: return f.NewVoidZeroExpression();
        }
    }
    // typeserializer.go:110.
    private Node serializeParameterTypesOfNode(Node node, Node container)
    {
        Node valueDeclaration = null;
        if (Ast.IsClassLike(node)) valueDeclaration = Ast.GetFirstConstructorWithBody(node);
        else if (Ast.IsFunctionLike(node) && Ast.NodeIsPresent(node.Body())) valueDeclaration = node;
        if (valueDeclaration == null) return f.NewArrayLiteralExpression(f.NewNodeList(Slice<Node>.Make(0)), false);
        Slice<Node> expressions = default;
        var parameters = getParametersOfDecoratedDeclaration(valueDeclaration, container);
        for (int i = 0; i < parameters.Nodes.Count; i++)
        {
            var parameter = parameters.Nodes[i];
            if (i == 0 && Ast.IsIdentifier(parameter.Name()) && parameter.Name().TextEquals("this"u8)) continue;
            expressions = expressions.Append(parameter.AsParameterDeclaration().DotDotDotToken != null
                ? serializeTypeNode(Ast.GetRestParameterElementType(parameter.Type())) : serializeTypeOfNode(parameter, container));
        }
        return f.NewArrayLiteralExpression(f.NewNodeList(expressions), false);
    }
    // typeserializer.go:137.
    private static NodeList getParametersOfDecoratedDeclaration(Node node, Node container)
    {
        if (container != null && node.Kind == Kind.GetAccessor)
        {
            var accessors = Ast.GetAllAccessorDeclarations(container.Members(), node);
            if (accessors.SetAccessor != null) return accessors.SetAccessor.Parameters;
        }
        return node.ParameterList();
    }
    // typeserializer.go:151.
    private Node serializeReturnTypeOfNode(Node node)
    {
        if (Ast.IsFunctionLike(node) && node.Type() != null) return serializeTypeNode(node.Type());
        if (Ast.IsAsyncFunction(node)) return f.NewIdentifier("Promise");
        return f.NewVoidZeroExpression();
    }
    // typeserializer.go:178.
    private Node serializeTypeNode(Node node)
    {
        if (node == null) return f.NewIdentifier("Object");
        node = Ast.SkipTypeParentheses(node);
        switch (node.Kind)
        {
            case Kind.VoidKeyword: case Kind.UndefinedKeyword: case Kind.NeverKeyword: return f.NewVoidZeroExpression();
            case Kind.FunctionType: case Kind.ConstructorType: return f.NewIdentifier("Function");
            case Kind.ArrayType: case Kind.TupleType: return f.NewIdentifier("Array");
            case Kind.TypePredicate: return node.AsTypePredicateNode().AssertsModifier != null ? f.NewVoidZeroExpression() : f.NewIdentifier("Boolean");
            case Kind.BooleanKeyword: return f.NewIdentifier("Boolean");
            case Kind.TemplateLiteralType: case Kind.StringKeyword: return f.NewIdentifier("String");
            case Kind.ObjectKeyword: return f.NewIdentifier("Object");
            case Kind.LiteralType: return serializeLiteralOfLiteralTypeNode(node.AsLiteralTypeNode().Literal);
            case Kind.NumberKeyword: return f.NewIdentifier("Number");
            case Kind.BigIntKeyword: return serializeBigIntConstructor();
            case Kind.SymbolKeyword: return f.NewIdentifier("Symbol");
            case Kind.TypeReference: return serializeTypeReferenceNode(node.AsTypeReferenceNode());
            case Kind.IntersectionType: return serializeUnionOrIntersectionConstituents(node.AsIntersectionTypeNode().Types.Nodes, true);
            case Kind.UnionType: return serializeUnionOrIntersectionConstituents(node.AsUnionTypeNode().Types.Nodes, false);
            case Kind.ConditionalType:
                var old = c.serializingConditionalTypeBranch;
                c.serializingConditionalTypeBranch = true;
                try
                {
                    return serializeUnionOrIntersectionConstituents(
                        Slice<Node>.FromArray([node.AsConditionalTypeNode().TrueType, node.AsConditionalTypeNode().FalseType]), false);
                }
                finally
                {
                    c.serializingConditionalTypeBranch = old;
                }
            case Kind.TypeOperator:
                if (node.AsTypeOperatorNode().Operator == Kind.ReadonlyKeyword) return serializeTypeNode(node.Type());
                break;
            case Kind.TypeQuery: case Kind.IndexedAccessType: case Kind.MappedType: case Kind.TypeLiteral: case Kind.AnyKeyword: case Kind.UnknownKeyword: case Kind.ThisType: case Kind.ImportType:
            case Kind.JSDocAllType: case Kind.JSDocVariadicType: break;
            case Kind.JSDocNullableType: case Kind.JSDocNonNullableType: case Kind.JSDocOptionalType: return serializeTypeNode(node.Type());
            default: throw new InvalidOperationException($"Bad metadata syntax kind: {node.Kind}");
        }
        return f.NewIdentifier("Object");
    }
    // typeserializer.go:242.
    private Node serializeUnionOrIntersectionConstituents(Slice<Node> types, bool isIntersection)
    {
        Node serializedType = null;
        foreach (var original in types)
        {
            var typeNode = Ast.SkipTypeParentheses(original);
            if (typeNode.Kind == Kind.NeverKeyword)
            {
                if (isIntersection)
                    return f.NewVoidZeroExpression();
                continue;
            }
            if (typeNode.Kind == Kind.UnknownKeyword)
            {
                if (!isIntersection)
                    return f.NewIdentifier("Object");
                continue;
            }
            if (typeNode.Kind == Kind.AnyKeyword) return f.NewIdentifier("Object");
            if (!strictNullChecks && (Ast.IsLiteralTypeNode(typeNode) && typeNode.AsLiteralTypeNode().Literal.Kind == Kind.NullKeyword || typeNode.Kind == Kind.UndefinedKeyword)) continue;
            var constituent = serializeTypeNode(typeNode);
            if (Ast.IsIdentifier(constituent) && constituent.TextEquals("Object"u8)) return constituent;
            if (serializedType != null)
            {
                if (!equateSerializedTypeNodes(serializedType, constituent))
                    return f.NewIdentifier("Object");
            }
            else
            {
                serializedType = constituent;
            }
        }
        return serializedType ?? f.NewVoidZeroExpression();
    }
    // typeserializer.go:295.
    private Node serializeLiteralOfLiteralTypeNode(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral: case Kind.NoSubstitutionTemplateLiteral: return f.NewIdentifier("String");
            case Kind.PrefixUnaryExpression:
                var operand = node.AsPrefixUnaryExpression().Operand;
                if (operand.Kind is Kind.NumericLiteral or Kind.BigIntLiteral) return serializeLiteralOfLiteralTypeNode(operand);
                throw new InvalidOperationException($"Bad metadata literal syntax kind: {operand.Kind}");
            case Kind.NumericLiteral: return f.NewIdentifier("Number");
            case Kind.BigIntLiteral: return serializeBigIntConstructor();
            case Kind.TrueKeyword: case Kind.FalseKeyword: return f.NewIdentifier("Boolean");
            case Kind.NullKeyword: return f.NewVoidZeroExpression();
            default: throw new InvalidOperationException($"Bad metadata literal syntax kind: {node.Kind}");
        }
    }
    // typeserializer.go:326.
    private Node serializeTypeReferenceNode(TypeReferenceNode node)
    {
        var serialScope = c.currentNameScope ?? c.currentLexicalScope;
        var kind = resolver.GetTypeReferenceSerializationKind(ec.ParseNode(node.TypeName), ec.ParseNode(serialScope));
        switch (kind)
        {
            case TypeReferenceSerializationKind.Unknown:
                if (c.serializingConditionalTypeBranch) return f.NewIdentifier("Object");
                var serialized = serializeEntityNameAsExpressionFallback(node.TypeName);
                var temp = f.NewTempVariable();
                ec.AddVariableDeclaration(temp);
                return f.NewConditionalExpression(
                    f.NewTypeCheck(f.NewAssignmentExpression(temp, serialized), "function"),
                    f.NewToken(Kind.QuestionToken),
                    temp,
                    f.NewToken(Kind.ColonToken),
                    f.NewIdentifier("Object"));
            case TypeReferenceSerializationKind.TypeWithConstructSignatureAndValue: return serializeEntityNameAsExpression(node.TypeName);
            case TypeReferenceSerializationKind.VoidNullableOrNeverType: return f.NewVoidZeroExpression();
            case TypeReferenceSerializationKind.BigIntLikeType: return serializeBigIntConstructor();
            case TypeReferenceSerializationKind.BooleanType: return f.NewIdentifier("Boolean");
            case TypeReferenceSerializationKind.NumberLikeType: return f.NewIdentifier("Number");
            case TypeReferenceSerializationKind.StringLikeType: return f.NewIdentifier("String");
            case TypeReferenceSerializationKind.ArrayLikeType: return f.NewIdentifier("Array");
            case TypeReferenceSerializationKind.ESSymbolType: return f.NewIdentifier("Symbol");
            case TypeReferenceSerializationKind.TypeWithCallSignature: return f.NewIdentifier("Function");
            case TypeReferenceSerializationKind.Promise: return f.NewIdentifier("Promise");
            case TypeReferenceSerializationKind.ObjectType: return f.NewIdentifier("Object");
            default: throw new InvalidOperationException($"Unknown type reference serialization kind: {kind}");
        }
    }
    // typeserializer.go:388.
    private Node serializeBigIntConstructor()
    {
        if (languageVersion >= ScriptTarget.ES2020)
            return f.NewIdentifier("BigInt");
        return f.NewConditionalExpression(
            f.NewTypeCheck(f.NewIdentifier("BigInt"), "function"),
            f.NewToken(Kind.QuestionToken),
            f.NewIdentifier("BigInt"),
            f.NewToken(Kind.ColonToken),
            f.NewIdentifier("Object"));
    }
    // typeserializer.go:405.
    private Node serializeEntityNameAsExpression(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                var name = node.Clone(f);
                name.Loc = node.Loc;
                ec.UnsetOriginal(name);
                name.Parent = ec.ParseNode(c.currentLexicalScope);
                return name;
            case Kind.QualifiedName: return serializeQualifiedNameAsExpression(node.AsQualifiedName());
        }
        return null;
    }
    // typeserializer.go:425.
    private Node serializeQualifiedNameAsExpression(QualifiedName node) => f.NewPropertyAccessExpression(serializeEntityNameAsExpression(node.Left), null, node.Right, NodeFlags.None);
    // typeserializer.go:433.
    private Node serializeEntityNameAsExpressionFallback(Node node)
    {
        if (node.Kind == Kind.Identifier)
        {
            var copied = serializeEntityNameAsExpression(node);
            return createCheckedValue(copied, copied);
        }
        if (node.AsQualifiedName().Left.Kind == Kind.Identifier) return createCheckedValue(serializeEntityNameAsExpression(node.AsQualifiedName().Left), serializeEntityNameAsExpression(node));
        var left = serializeEntityNameAsExpressionFallback(node.AsQualifiedName().Left);
        var temp = f.NewTempVariable();
        ec.AddVariableDeclaration(temp);
        return f.NewLogicalANDExpression(
            f.NewLogicalANDExpression(
                left.AsBinaryExpression().Left,
                f.NewStrictInequalityExpression(
                    f.NewAssignmentExpression(temp, left.AsBinaryExpression().Right),
                    f.NewVoidZeroExpression())),
            f.NewPropertyAccessExpression(temp, null, node.AsQualifiedName().Right, NodeFlags.None));
    }
    // typeserializer.go:467.
    private Node createCheckedValue(Node left, Node right) => f.NewLogicalANDExpression(f.NewStrictInequalityExpression(f.NewTypeOfExpression(left), f.NewStringLiteral("undefined", TokenFlags.None)), right);
    // typeserializer.go:474.
    private bool equateSerializedTypeNodes(Node left, Node right)
    {
        if (Transformers.IsGeneratedIdentifier(ec, left)) return Transformers.IsGeneratedIdentifier(ec, right);
        if (Ast.IsIdentifier(left)) return Ast.IsIdentifier(right) && left.TextValue().EqualsValue(right.TextValue());
        if (Ast.IsPropertyAccessExpression(left)) return Ast.IsPropertyAccessExpression(right) && equateSerializedTypeNodes(left.Expression(), right.Expression()) && equateSerializedTypeNodes(left.Name(), right.Name());
        if (Ast.IsVoidExpression(left)) return Ast.IsVoidExpression(right) && Ast.IsNumericLiteral(left.Expression()) && Ast.IsNumericLiteral(right.Expression()) && left.Expression().TextEquals("0"u8) && right.Expression().TextEquals("0"u8);
        if (Ast.IsStringLiteral(left)) return Ast.IsStringLiteral(right) && left.TextValue().EqualsValue(right.TextValue());
        if (Ast.IsTypeOfExpression(left)) return Ast.IsTypeOfExpression(right) && equateSerializedTypeNodes(left.Expression(), right.Expression());
        if (Ast.IsParenthesizedExpression(left)) return Ast.IsParenthesizedExpression(right) && equateSerializedTypeNodes(left.Expression(), right.Expression());
        if (Ast.IsConditionalExpression(left)) return Ast.IsConditionalExpression(right) && equateSerializedTypeNodes(left.AsConditionalExpression().Condition, right.AsConditionalExpression().Condition) && equateSerializedTypeNodes(left.AsConditionalExpression().WhenTrue, right.AsConditionalExpression().WhenTrue) && equateSerializedTypeNodes(left.AsConditionalExpression().WhenFalse, right.AsConditionalExpression().WhenFalse);
        if (Ast.IsBinaryExpression(left)) return Ast.IsBinaryExpression(right) && left.AsBinaryExpression().OperatorToken.Kind == right.AsBinaryExpression().OperatorToken.Kind && equateSerializedTypeNodes(left.AsBinaryExpression().Left, right.AsBinaryExpression().Left) && equateSerializedTypeNodes(left.AsBinaryExpression().Right, right.AsBinaryExpression().Right);
        return false;
    }
}
