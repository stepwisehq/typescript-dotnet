// Authored admission boundary for the bounded upstream semantic checking slice.
using TypeScript.Scanner;
using System.Diagnostics.CodeAnalysis;

namespace TypeScript.Checking;
public sealed class UnsupportedSemanticFeatureException : NotSupportedException
{
    public string Feature { get; }
    public Kind Kind { get; }
    public int Pos { get; }
    public int End { get; }
    public string FileName { get; }

    internal UnsupportedSemanticFeatureException(string feature) : base($"Semantic checking does not support {feature}.")
    {
        Feature = feature;
        Kind = Kind.Unknown;
        Pos = -1;
        End = -1;
        FileName = "";
    }

    internal UnsupportedSemanticFeatureException(SourceFile file, Node node, string feature) : base($"Semantic checking does not support {feature} ({node.Kind}) at byte range [{node.Pos()}, {node.End()}).")
    {
        Feature = feature;
        Kind = node.Kind;
        Pos = node.Pos();
        End = node.End();
        FileName = file.FileName() ?? "";
    }
}

public sealed partial class Checker
{
    public static Checker Create(SourceFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        SemanticAdmission.Validate(file);
        return CreateCore(file);
    }
}

internal static class SemanticAdmission
{
    internal static void ValidateOwnedNode(SourceFile file, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var seen = new HashSet<Node>(ReferenceEqualityComparer.Instance);
        for (Node? current = node; current is not null && seen.Add(current); current = current.Parent)
            if (ReferenceEquals(current, file.AsNode()))
                return;
        Reject(file, node, "foreignNode");
    }

    [DoesNotReturn]
    private static void Reject(SourceFile file, Node node, string feature) => throw new UnsupportedSemanticFeatureException(file, node, feature);
    public static void Validate(SourceFile file)
    {
        Node root = file.AsNode();
        if (file.ScriptKind != ScriptKind.TS)
            Reject(file, root, "scriptKind");
        if (file.IsDeclarationFile)
            Reject(file, root, "declarationFile");
        if (file.Diagnostics()is { Count: > 0 })
            Reject(file, root, "parseErrors");
        if (file.CheckJsDirective is not null || file.CommentDirectives is { Count: > 0 })
            Reject(file, root, "checkingDirective");
        if (file.ReferencedFiles is { Count: > 0 } || file.TypeReferenceDirectives is { Count: > 0 } || file.LibReferenceDirectives is { Count: > 0 })
            Reject(file, root, "externalReference");
        if (file.ExternalModuleIndicator is not null || file.CommonJSModuleIndicator is not null)
            Reject(file, root, "module");
        foreach (Node statement in file.Statements.Nodes ?? [])
            Statement(file, statement);
    }

    private static void Documentation(SourceFile file, Node node)
    {
        foreach (Node doc in node.JSDoc(file) ?? [])
            foreach (Node tag in doc.AsJSDoc().Tags?.Nodes ?? [])
                Reject(file, tag, "jsDocTag");
    }

    private static void Statement(SourceFile file, Node node)
    {
        Documentation(file, node);
        if (node.ModifierNodes()is { Count: > 0 })
            Reject(file, node, "modifiers");
        switch (node.Kind)
        {
            case Kind.EmptyStatement:
                return;
            case Kind.TypeAliasDeclaration:
                var alias = node.AsTypeAliasDeclaration();
                if (alias.TypeParameters is not null)
                    Reject(file, node, "typeParameters");
                Annotation(file, alias.Type);
                return;
            case Kind.InterfaceDeclaration:
                var shape = node.AsInterfaceDeclaration();
                if (shape.TypeParameters is not null)
                    Reject(file, node, "typeParameters");
                foreach (Node clause in shape.HeritageClauses?.Nodes ?? [])
                {
                    if (clause.AsHeritageClause().Token != Kind.ExtendsKeyword)
                        Reject(file, clause, "heritageKind");
                    foreach (Node basis in clause.AsHeritageClause().Types?.Nodes ?? [])
                    {
                        if (basis.Kind == Kind.TypeReference)
                        {
                            var heritage = basis.AsTypeReferenceNode();
                            if (heritage.TypeArguments is not null)
                                Reject(file, basis, "typeArguments");
                            EntityName(file, heritage.TypeName);
                        }
                        else if (basis.Kind == Kind.ExpressionWithTypeArguments)
                        {
                            var heritage = basis.AsExpressionWithTypeArguments();
                            if (heritage.TypeArguments is not null)
                                Reject(file, basis, "typeArguments");
                            EntityName(file, heritage.Expression);
                        }
                        else
                            Reject(file, basis, "typeName");
                    }
                }

                DataMembers(file, shape.Members?.Nodes);
                return;
            case Kind.Block:
                foreach (Node child in node.AsBlock().Statements?.Nodes ?? [])
                    Statement(file, child);
                return;
            case Kind.VariableStatement:
                var list = node.AsVariableStatement().DeclarationList;
                if ((list.Flags & NodeFlags.Using) != 0)
                    Reject(file, list, "using");
                foreach (Node declaration in list.AsVariableDeclarationList().Declarations?.Nodes ?? [])
                {
                    Documentation(file, declaration);
                    var variable = declaration.AsVariableDeclaration();
                    if (variable.Name().Kind != Kind.Identifier)
                        Reject(file, variable.Name(), "bindingPattern");
                    if (variable.ExclamationToken is not null)
                        Reject(file, variable.ExclamationToken, "definiteAssignmentAssertion");
                    if (variable.Type is not null)
                        Annotation(file, variable.Type);
                    if (variable.Initializer is not null)
                        Expression(file, variable.Initializer);
                }

                return;
            default:
                Reject(file, node, "statement");
                return;
        }
    }

    private static void Annotation(SourceFile file, Node node)
    {
        switch (node.Kind)
        {
            case Kind.NumberKeyword:
            case Kind.StringKeyword:
            case Kind.BooleanKeyword:
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.NeverKeyword:
            case Kind.VoidKeyword:
            case Kind.UndefinedKeyword:
            case Kind.BigIntKeyword:
            case Kind.SymbolKeyword:
            case Kind.ObjectKeyword:
                return;
            case Kind.TypeReference:
                var reference = node.AsTypeReferenceNode();
                if (reference.TypeArguments is not null)
                    Reject(file, node, "typeArguments");
                EntityName(file, reference.TypeName);
                return;
            case Kind.UnionType:
                foreach (Node member in node.AsUnionTypeNode().Types?.Nodes ?? [])
                    Annotation(file, member);
                return;
            case Kind.IntersectionType:
                foreach (Node member in node.AsIntersectionTypeNode().Types?.Nodes ?? [])
                    Annotation(file, member);
                return;
            case Kind.ParenthesizedType:
                Annotation(file, node.AsParenthesizedTypeNode().Type);
                return;
            case Kind.TypeLiteral:
                DataMembers(file, node.AsTypeLiteralNode().Members?.Nodes);
                return;
            case Kind.LiteralType:
                var literal = node.AsLiteralTypeNode().Literal;
                if (literal.Kind is Kind.StringLiteral or Kind.NumericLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword)
                    return;
                Reject(file, literal, "literalType");
                return;
            default:
                Reject(file, node, "type");
                return;
        }
    }

    private static void EntityName(SourceFile file, Node node)
    {
        if (node.Kind == Kind.Identifier)
            return;
        if (node.Kind == Kind.QualifiedName)
        {
            EntityName(file, node.AsQualifiedName().Left);
            EntityName(file, node.AsQualifiedName().Right);
            return;
        }

        if (node.Kind == Kind.PropertyAccessExpression && node.AsPropertyAccessExpression().QuestionDotToken is null)
        {
            EntityName(file, node.Expression());
            EntityName(file, node.Name()!);
            return;
        }

        Reject(file, node, "typeName");
    }

    private static void DataMembers(SourceFile file, List<Node>? members)
    {
        foreach (Node member in members ?? [])
        {
            Documentation(file, member);
            if (member.Kind != Kind.PropertySignature)
                Reject(file, member, "typeMember");
            StaticPropertyName(file, member.Name());
            foreach (Node modifier in member.ModifierNodes() ?? [])
                if (modifier.Kind != Kind.ReadonlyKeyword)
                    Reject(file, modifier, "propertyModifier");
            var property = member.AsPropertySignatureDeclaration();
            if (property.PostfixToken is { Kind: not Kind.QuestionToken })
                Reject(file, property.PostfixToken, "propertyPostfix");
            if (property.Initializer is not null)
                Reject(file, property.Initializer, "propertyInitializer");
            if (property.Type is null)
                Reject(file, member, "missingPropertyType");
            Annotation(file, property.Type);
        }
    }

    private static void Expression(SourceFile file, Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
                return;
            case Kind.Identifier:
                return;
            case Kind.ParenthesizedExpression:
                Expression(file, node.AsParenthesizedExpression().Expression);
                return;
            case Kind.ObjectLiteralExpression:
                foreach (Node member in node.Properties() ?? [])
                {
                    Documentation(file, member);
                    if (member.ModifierNodes()is { Count: > 0 })
                        Reject(file, member, "propertyModifier");
                    if (member.Kind is not (Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment))
                        Reject(file, member, "objectMember");
                    StaticPropertyName(file, member.Name());
                    if (member.Kind == Kind.PropertyAssignment)
                    {
                        var property = member.AsPropertyAssignment();
                        if (property.Type is not null || property.PostfixToken is not null)
                            Reject(file, member, "propertyShape");
                        Expression(file, property.Initializer);
                    }
                    else if (member.Kind == Kind.ShorthandPropertyAssignment)
                    {
                        var property = member.AsShorthandPropertyAssignment();
                        if (property.EqualsToken is not null || property.ObjectAssignmentInitializer is not null)
                            Reject(file, member, "shorthandDefault");
                    }
                    else
                        Reject(file, member, "objectMember");
                }

                return;
            case Kind.PropertyAccessExpression:
                var access = node.AsPropertyAccessExpression();
                if (access.QuestionDotToken is not null)
                    Reject(file, access.QuestionDotToken, "optionalChain");
                if (access.Name().Kind != Kind.Identifier)
                    Reject(file, access.Name(), "propertyName");
                Expression(file, access.Expression);
                return;
            default:
                Reject(file, node, "expression");
                return;
        }
    }

    private static void StaticPropertyName(SourceFile file, Node? node)
    {
        if (node is null)
            Reject(file, file.AsNode(), "objectMember");
        if (node!.Kind is not (Kind.Identifier or Kind.StringLiteral or Kind.NumericLiteral))
            Reject(file, node, "propertyName");
    }
}
