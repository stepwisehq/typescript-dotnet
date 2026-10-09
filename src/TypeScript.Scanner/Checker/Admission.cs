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

/// <summary>Authored capability/option profiles for the same upstream checker.</summary>
public enum CheckerProfile
{
    Legacy,
    Callable,
    Collections,
    Projects
}

public sealed partial class Checker
{
    public static Checker Create(SourceFile file) => Create(file, CheckerProfile.Legacy);
    public static Checker Create(SourceFile file, CheckerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (profile is not (CheckerProfile.Legacy or CheckerProfile.Callable or CheckerProfile.Collections or CheckerProfile.Projects))
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (profile == CheckerProfile.Projects)
            throw new UnsupportedSemanticFeatureException("projectHost; load a project through TypeScript.Compiler.Project.Load");
        if (profile == CheckerProfile.Collections)
        {
            CollectionAdmission.Validate(file);
            return CreateCore(file, profile, FixedLibraryEnvironment.Load());
        }

        SemanticAdmission.Validate(file, profile == CheckerProfile.Callable);
        return CreateCore(file, profile);
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
    public static void Validate(SourceFile file, bool callable = false)
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
            Statement(file, statement, callable);
    }

    private static void Documentation(SourceFile file, Node node)
    {
        foreach (Node doc in node.JSDoc(file) ?? [])
            foreach (Node tag in doc.AsJSDoc().Tags?.Nodes ?? [])
                Reject(file, tag, "jsDocTag");
    }

    private static void Statement(SourceFile file, Node node, bool callable)
    {
        Documentation(file, node);
        if (!callable && node.ModifierNodes()is { Count: > 0 })
            Reject(file, node, "modifiers");
        if (callable && node.Kind != Kind.FunctionDeclaration)
            foreach (Node modifier in node.ModifierNodes() ?? [])
                if (!(modifier.Kind == Kind.DeclareKeyword && node.Kind is Kind.FunctionDeclaration or Kind.VariableStatement))
                    Reject(file, modifier, "modifiers");
        switch (node.Kind)
        {
            case Kind.FunctionDeclaration when callable:
                Function(file, node);
                return;
            case Kind.ExpressionStatement when callable:
                Expression(file, node.Expression(), true);
                return;
            case Kind.IfStatement when callable:
                var branch = node.AsIfStatement();
                Expression(file, branch.Expression, true);
                Statement(file, branch.ThenStatement, true);
                if (branch.ElseStatement is not null)
                    Statement(file, branch.ElseStatement, true);
                return;
            case Kind.ReturnStatement when callable:
            case Kind.ThrowStatement when callable:
                if (node.Expression()is { } returned)
                    Expression(file, returned, true);
                return;
            case Kind.EmptyStatement:
                return;
            case Kind.TypeAliasDeclaration:
                var alias = node.AsTypeAliasDeclaration();
                if (alias.TypeParameters is not null)
                    Reject(file, node, "typeParameters");
                Annotation(file, alias.Type, callable);
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

                DataMembers(file, shape.Members?.Nodes, callable);
                return;
            case Kind.Block:
                foreach (Node child in node.AsBlock().Statements?.Nodes ?? [])
                    Statement(file, child, callable);
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
                        Annotation(file, variable.Type, callable);
                    if (variable.Initializer is not null)
                        Expression(file, variable.Initializer, callable);
                }

                return;
            default:
                Reject(file, node, "statement");
                return;
        }
    }

    private static void Annotation(SourceFile file, Node node, bool callable)
    {
        switch (node.Kind)
        {
            case Kind.FunctionType when callable:
                Function(file, node);
                return;
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
                    Annotation(file, member, callable);
                return;
            case Kind.IntersectionType:
                foreach (Node member in node.AsIntersectionTypeNode().Types?.Nodes ?? [])
                    Annotation(file, member, callable);
                return;
            case Kind.ParenthesizedType:
                Annotation(file, node.AsParenthesizedTypeNode().Type, callable);
                return;
            case Kind.TypeLiteral:
                DataMembers(file, node.AsTypeLiteralNode().Members?.Nodes, callable);
                return;
            case Kind.LiteralType:
                var literal = node.AsLiteralTypeNode().Literal;
                if (callable && literal.Kind == Kind.PrefixUnaryExpression && literal.AsPrefixUnaryExpression().Operator == Kind.MinusToken && literal.AsPrefixUnaryExpression().Operand.Kind == Kind.NumericLiteral)
                    return;
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

    private static void DataMembers(SourceFile file, List<Node>? members, bool callable)
    {
        foreach (Node member in members ?? [])
        {
            Documentation(file, member);
            if (callable && member.Kind is Kind.CallSignature or Kind.MethodSignature)
            {
                if (member.Kind == Kind.MethodSignature)
                    StaticPropertyName(file, member.Name());
                Function(file, member);
                continue;
            }

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
            Annotation(file, property.Type, callable);
        }
    }

    private static void Expression(SourceFile file, Node node, bool callable)
    {
        switch (node.Kind)
        {
            case Kind.ThisKeyword when callable:
                return;
            case Kind.FunctionExpression or Kind.ArrowFunction when callable:
                Function(file, node);
                return;
            case Kind.CallExpression when callable:
                var call = node.AsCallExpression();
                if (call.QuestionDotToken is not null)
                    Reject(file, call.QuestionDotToken, "optionalChain");
                if (call.TypeArguments is not null)
                    Reject(file, node, "typeArguments");
                Expression(file, call.Expression, true);
                foreach (Node arg in call.Arguments?.Nodes ?? [])
                    Expression(file, arg, true);
                return;
            case Kind.ConditionalExpression when callable:
                var conditional = node.AsConditionalExpression();
                Expression(file, conditional.Condition, true);
                Expression(file, conditional.WhenTrue, true);
                Expression(file, conditional.WhenFalse, true);
                return;
            case Kind.TypeOfExpression or Kind.VoidExpression when callable:
                Expression(file, node.Expression(), true);
                return;
            case Kind.PrefixUnaryExpression when callable:
                var unary = node.AsPrefixUnaryExpression();
                if (unary.Operator is not (Kind.PlusToken or Kind.MinusToken or Kind.ExclamationToken or Kind.TildeToken))
                    Reject(file, node, "unaryOperator");
                Expression(file, unary.Operand, true);
                return;
            case Kind.BinaryExpression when callable:
                var binary = node.AsBinaryExpression();
                if (binary.OperatorToken.Kind == Kind.EqualsToken)
                {
                    Node left = binary.Left;
                    while (left.Kind == Kind.ParenthesizedExpression)
                        left = left.Expression();
                    if (left.Kind is not (Kind.Identifier or Kind.PropertyAccessExpression))
                        Reject(file, left, "assignmentTarget");
                }
                else if (binary.OperatorToken.Kind is not (Kind.PlusToken or Kind.MinusToken or Kind.AsteriskToken or Kind.AsteriskAsteriskToken or Kind.SlashToken or Kind.PercentToken or Kind.LessThanToken or Kind.LessThanEqualsToken or Kind.GreaterThanToken or Kind.GreaterThanEqualsToken or Kind.EqualsEqualsToken or Kind.EqualsEqualsEqualsToken or Kind.ExclamationEqualsToken or Kind.ExclamationEqualsEqualsToken or Kind.AmpersandToken or Kind.BarToken or Kind.CaretToken or Kind.LessThanLessThanToken or Kind.GreaterThanGreaterThanToken or Kind.GreaterThanGreaterThanGreaterThanToken or Kind.AmpersandAmpersandToken or Kind.BarBarToken or Kind.QuestionQuestionToken))
                    Reject(file, binary.OperatorToken, "binaryOperator");
                Expression(file, binary.Left, true);
                Expression(file, binary.Right, true);
                return;
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
                return;
            case Kind.Identifier:
                return;
            case Kind.ParenthesizedExpression:
                Expression(file, node.AsParenthesizedExpression().Expression, callable);
                return;
            case Kind.ObjectLiteralExpression:
                foreach (Node member in node.Properties() ?? [])
                {
                    Documentation(file, member);
                    if (callable && member.Kind == Kind.MethodDeclaration)
                    {
                        StaticPropertyName(file, member.Name());
                        Function(file, member);
                        continue;
                    }

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
                        Expression(file, property.Initializer, callable);
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
                Expression(file, access.Expression, callable);
                return;
            default:
                Reject(file, node, "expression");
                return;
        }
    }

    private static void Function(SourceFile file, Node node)
    {
        Documentation(file, node);
        foreach (Node modifier in node.ModifierNodes() ?? [])
            if (!(modifier.Kind == Kind.DeclareKeyword && node.Kind == Kind.FunctionDeclaration) && !(node.Kind is (Kind.MethodSignature or Kind.MethodDeclaration) && modifier.Kind is (Kind.ReadonlyKeyword or Kind.PublicKeyword or Kind.PrivateKeyword or Kind.ProtectedKeyword)))
                Reject(file, modifier, "functionModifier");
        if (node.TypeParameterList()is not null)
            Reject(file, node, "typeParameters");
        if ((Ast.GetFunctionFlags(node) & (FunctionFlags.Async | FunctionFlags.Generator)) != 0)
            Reject(file, node, "asyncGenerator");
        foreach (Node parameter in node.Parameters() ?? [])
        {
            Documentation(file, parameter);
            var p = parameter.AsParameterDeclaration();
            foreach (var modifier in parameter.ModifierNodes() ?? [])
                if (modifier.Kind is not (Kind.ReadonlyKeyword or Kind.PublicKeyword or Kind.PrivateKeyword or Kind.ProtectedKeyword))
                    Reject(file, parameter, "parameterModifier");
            if (p.DotDotDotToken is not null)
                Reject(file, p.DotDotDotToken, "restParameter");
            if (p.Name().Kind != Kind.Identifier)
                Reject(file, p.Name(), "bindingPattern");
            if (p.Type is not null)
                Annotation(file, p.Type, true);
            if (p.Initializer is not null)
                Expression(file, p.Initializer, true);
        }

        if (node.Type()is { } annotation)
            Annotation(file, annotation, true);
        if (node.Body()is { } body)
        {
            if (body.Kind == Kind.Block)
                Statement(file, body, true);
            else
                Expression(file, body, true);
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
