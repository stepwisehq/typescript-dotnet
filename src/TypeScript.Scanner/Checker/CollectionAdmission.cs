// Authored source boundary for Collections. The pinned library ASTs bypass this
// user-source gate; demanded library semantics still run through the real checker.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal static class CollectionAdmission
{
    [DoesNotReturn]
    private static void Reject(SourceFile file, Node node, string feature) => throw new UnsupportedSemanticFeatureException(file, node, feature);
    internal static void Validate(SourceFile file, bool project = false, bool allowNonRelativeModuleSpecifiers = false)
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
        if ((!project && file.ExternalModuleIndicator is not null) || file.CommonJSModuleIndicator is not null)
            Reject(file, root, "module");
        Visit(file, root, project, allowNonRelativeModuleSpecifiers);
    }

    private static Node? Field(Node node, string role) => node.SyntaxFields().FirstOrDefault(f => f.Role == role).Value as Node;
    private static Slice<Node> List(Node node, string role) => (node.SyntaxFields().FirstOrDefault(f => f.Role == role).Value as NodeList)?.Nodes ?? default;
    private static void Visit(SourceFile file, Node node, bool project, bool allowNonRelativeModuleSpecifiers)
    {
        // Preserve ordinary documentation; semantic source JSDoc remains excluded.
        foreach (Node doc in node.JSDoc(file))
            foreach (Node tag in doc.AsJSDoc().Tags?.Nodes ?? default)
                Reject(file, tag, "jsDocTag");
        if (Field(node, "AsteriskToken")is not null)
            Reject(file, node, "generator");
        if (node.Kind is Kind.ObjectBindingPattern or Kind.ArrayBindingPattern or Kind.BindingElement)
            Reject(file, node, "bindingPattern");
        if (node.Kind == Kind.TypeParameter && node.ModifierNodes()is { Count: > 0 })
            Reject(file, node, "typeParameterModifiers");
        if (node.Kind == Kind.VariableDeclarationList && (node.Flags & NodeFlags.Using) != 0)
            Reject(file, node, "using");
        if (node.Kind == Kind.ForOfStatement && Field(node, "AwaitModifier")is not null)
            Reject(file, node, "asyncIteration");
        if (node.Kind is Kind.BreakStatement or Kind.ContinueStatement && Field(node, "Label")is not null)
            Reject(file, node, "label");
        if (node.Kind == Kind.MappedType && node.AsMappedTypeNode().NameType is not null)
            Reject(file, node, "mappedKeyRemapping");
        if (node.Kind == Kind.TypePredicate && Field(node, "AssertsModifier")is not null)
            Reject(file, node, "assertionSignature");
        if (node.Kind == Kind.TypeOperator && node.AsTypeOperatorNode().Operator is not (Kind.KeyOfKeyword or Kind.ReadonlyKeyword))
            Reject(file, node, "typeOperator");
        // Generic variadic/type-parameter rest exclusions are checked at the
        // authentic resolved-type boundary, so alias spelling and shadowing
        // cannot change admission. Array aliases and infer patterns stay real.
        if (node.Kind == Kind.SpreadAssignment)
            Reject(file, node, "objectSpread");
        if (node.Kind == Kind.ComputedPropertyName)
            Reject(file, node, "computedProperty");
        if (project && node.Kind is Kind.ImportDeclaration or Kind.ExportDeclaration)
        {
            if (Field(node, "Attributes") is not null)
                Reject(file, node, "importAttributes");
            if (Field(node, "ModuleSpecifier") is { } specifier)
                ProjectAdmission.ValidateSpecifier(file, specifier, allowNonRelativeModuleSpecifiers);
        }
        if (project && node.Kind == Kind.ExportAssignment && node.AsExportAssignment().IsExportEquals)
            Reject(file, node, "exportEquals");
        if (Field(node, "QuestionDotToken")is not null)
            Reject(file, node, "optionalChain");
        if (node.Kind == Kind.BinaryExpression && !BinaryOperator(node.AsBinaryExpression().OperatorToken.Kind))
            Reject(file, node, "binaryOperator");
        if (node.Kind == Kind.BinaryExpression && node.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken && Ast.IsAssignmentPattern(Ast.SkipParentheses(node.AsBinaryExpression().Left)))
            Reject(file, node, "assignmentPattern");
        if (node.Kind == Kind.PrefixUnaryExpression && node.AsPrefixUnaryExpression().Operator is not (Kind.PlusToken or Kind.MinusToken or Kind.ExclamationToken or Kind.TildeToken or Kind.PlusPlusToken or Kind.MinusMinusToken))
            Reject(file, node, "unaryOperator");
        if (!Allowed(node.Kind) && !(project && ProjectAdmission.AllowedModuleSyntax(node.Kind)))
            Reject(file, node, "syntax");
        node.ForEachChild(child =>
        {
            Visit(file, child, project, allowNonRelativeModuleSpecifiers);
            return false;
        });
    }

    private static bool BinaryOperator(Kind kind) => kind is Kind.PlusToken or Kind.MinusToken or Kind.AsteriskToken or Kind.AsteriskAsteriskToken or Kind.SlashToken or Kind.PercentToken or Kind.LessThanLessThanToken or Kind.GreaterThanGreaterThanToken or Kind.GreaterThanGreaterThanGreaterThanToken or Kind.AmpersandToken or Kind.BarToken or Kind.CaretToken or Kind.LessThanToken or Kind.LessThanEqualsToken or Kind.GreaterThanToken or Kind.GreaterThanEqualsToken or Kind.EqualsEqualsToken or Kind.EqualsEqualsEqualsToken or Kind.ExclamationEqualsToken or Kind.ExclamationEqualsEqualsToken or Kind.AmpersandAmpersandToken or Kind.BarBarToken or Kind.QuestionQuestionToken or Kind.EqualsToken or Kind.PlusEqualsToken or Kind.MinusEqualsToken or Kind.AsteriskEqualsToken or Kind.AsteriskAsteriskEqualsToken or Kind.SlashEqualsToken or Kind.PercentEqualsToken or Kind.LessThanLessThanEqualsToken or Kind.GreaterThanGreaterThanEqualsToken or Kind.GreaterThanGreaterThanGreaterThanEqualsToken or Kind.AmpersandEqualsToken or Kind.BarEqualsToken or Kind.CaretEqualsToken or Kind.AmpersandAmpersandEqualsToken or Kind.BarBarEqualsToken or Kind.QuestionQuestionEqualsToken;
    private static bool Allowed(Kind kind) => kind is Kind.SourceFile or Kind.EndOfFile or Kind.Identifier or Kind.NumericLiteral or Kind.StringLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword or Kind.ThisKeyword or Kind.NumberKeyword or Kind.StringKeyword or Kind.BooleanKeyword or Kind.AnyKeyword or Kind.UnknownKeyword or Kind.NeverKeyword or Kind.VoidKeyword or Kind.UndefinedKeyword or Kind.BigIntKeyword or Kind.SymbolKeyword or Kind.ObjectKeyword or Kind.Block or Kind.EmptyStatement or Kind.VariableStatement or Kind.VariableDeclarationList or Kind.VariableDeclaration or Kind.ExpressionStatement or Kind.IfStatement or Kind.ReturnStatement or Kind.ThrowStatement or Kind.ForStatement or Kind.WhileStatement or Kind.DoStatement or Kind.ForOfStatement or Kind.BreakStatement or Kind.ContinueStatement or Kind.FunctionDeclaration or Kind.FunctionExpression or Kind.ArrowFunction or Kind.Parameter or Kind.TypeParameter or Kind.MethodDeclaration or Kind.MethodSignature or Kind.CallSignature or Kind.FunctionType or Kind.InterfaceDeclaration or Kind.TypeAliasDeclaration or Kind.HeritageClause or Kind.ExpressionWithTypeArguments or Kind.TypeReference or Kind.QualifiedName or Kind.TypeLiteral or Kind.PropertySignature or Kind.IndexSignature or Kind.TypePredicate or Kind.ArrayType or Kind.TupleType or Kind.NamedTupleMember or Kind.OptionalType or Kind.RestType or Kind.ParenthesizedType or Kind.UnionType or Kind.IntersectionType or Kind.LiteralType or Kind.TypeOperator or Kind.IndexedAccessType or Kind.MappedType or Kind.ConditionalType or Kind.InferType or Kind.ArrayLiteralExpression or Kind.OmittedExpression or Kind.SpreadElement or Kind.ObjectLiteralExpression or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.ParenthesizedExpression or Kind.PropertyAccessExpression or Kind.ElementAccessExpression or Kind.CallExpression or Kind.BinaryExpression or Kind.PrefixUnaryExpression or Kind.PostfixUnaryExpression or Kind.ConditionalExpression or Kind.TypeOfExpression or Kind.VoidExpression or Kind.DeclareKeyword or Kind.ReadonlyKeyword or Kind.PublicKeyword or Kind.PrivateKeyword or Kind.ProtectedKeyword or Kind.AbstractKeyword or Kind.StaticKeyword or Kind.QuestionToken or Kind.ColonToken or Kind.DotDotDotToken or Kind.ExclamationToken or Kind.EqualsGreaterThanToken or Kind.PlusToken or Kind.MinusToken or Kind.AsteriskToken or Kind.AsteriskAsteriskToken or Kind.SlashToken or Kind.PercentToken or Kind.LessThanLessThanToken or Kind.GreaterThanGreaterThanToken or Kind.GreaterThanGreaterThanGreaterThanToken or Kind.AmpersandToken or Kind.BarToken or Kind.CaretToken or Kind.LessThanToken or Kind.LessThanEqualsToken or Kind.GreaterThanToken or Kind.GreaterThanEqualsToken or Kind.EqualsEqualsToken or Kind.EqualsEqualsEqualsToken or Kind.ExclamationEqualsToken or Kind.ExclamationEqualsEqualsToken or Kind.AmpersandAmpersandToken or Kind.BarBarToken or Kind.QuestionQuestionToken or Kind.EqualsToken or Kind.PlusEqualsToken or Kind.MinusEqualsToken or Kind.AsteriskEqualsToken or Kind.AsteriskAsteriskEqualsToken or Kind.SlashEqualsToken or Kind.PercentEqualsToken or Kind.LessThanLessThanEqualsToken or Kind.GreaterThanGreaterThanEqualsToken or Kind.GreaterThanGreaterThanGreaterThanEqualsToken or Kind.AmpersandEqualsToken or Kind.BarEqualsToken or Kind.CaretEqualsToken or Kind.AmpersandAmpersandEqualsToken or Kind.BarBarEqualsToken or Kind.QuestionQuestionEqualsToken;
}
