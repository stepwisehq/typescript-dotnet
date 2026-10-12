// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/utilities.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using Kind = TypeScript.Scanner.Kind;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using LspRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal sealed class PossibleTypeArgumentInfo
{
    internal Node called = null!;
    internal int nTypeArguments;
}

internal sealed partial class LanguageService
{
    // utilities.go:26. These mixed-width keys select Go's generic replacement engine.
    // The .NET Lazy owner preserves sync.Once publication and retained input cleanup.
    private sealed class QuoteReplacer
    {
        private Slice<ByteStringValue> oldNew;
        private readonly Lazy<GoGenericReplacer> engine;
        internal QuoteReplacer(Slice<ByteStringValue> pairs)
        {
            if ((pairs.Length & 1) != 0)
                throw new InvalidOperationException("strings.NewReplacer: odd argument count");
            oldNew = pairs.IsNil ? default : Slice<ByteStringValue>.FromArray(pairs.AsSpan().ToArray());
            engine = new Lazy<GoGenericReplacer>(BuildOnce, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private GoGenericReplacer BuildOnce()
        {
            var result = GoGenericReplacer.Make(oldNew);
            oldNew = default;
            return result;
        }

        internal ByteStringValue Replace(ByteStringValue text) => engine.Value.Replace(text);
    }

    private static readonly QuoteReplacer quoteReplacer = new(Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("'"), ByteStringValue.FromString(@"\'"), ByteStringValue.FromString("\\\""), ByteStringValue.FromString("\"")]));
    // utilities.go:323. Original retained kind set, with the same membership.
    private static readonly HashSet<Kind> typeKeywords = new()
    {
        Kind.AnyKeyword,
        Kind.AssertsKeyword,
        Kind.BigIntKeyword,
        Kind.BooleanKeyword,
        Kind.FalseKeyword,
        Kind.InferKeyword,
        Kind.KeyOfKeyword,
        Kind.NeverKeyword,
        Kind.NullKeyword,
        Kind.NumberKeyword,
        Kind.ObjectKeyword,
        Kind.ReadonlyKeyword,
        Kind.StringKeyword,
        Kind.SymbolKeyword,
        Kind.TypeOfKeyword,
        Kind.TrueKeyword,
        Kind.VoidKeyword,
        Kind.UndefinedKeyword,
        Kind.UniqueKeyword,
        Kind.UnknownKeyword
    };
    // utilities.go:29.
    internal static bool IsInString(SourceFile sourceFile, int position, Node? previousToken)
    {
        if (previousToken != null && Ast.IsStringTextContainingNode(previousToken))
        {
            int start = AstNav.GetStartOfNode(previousToken, sourceFile, false);
            int end = previousToken.End();
            if (start < position && position < end)
                return true;
            if (position == end)
                return Ast.IsUnterminatedLiteral(previousToken);
        }

        return false;
    }

    private static bool isModuleSpecifierLike(Node node)
    {
        if (!Ast.IsStringLiteralLike(node))
            return false;
        if (Ast.IsRequireCall(node.Parent, false) || Ast.IsImportCall(node.Parent))
            return node.Parent.Arguments()[0] == node;
        return node.Parent.Kind is Kind.ExternalModuleReference or Kind.ImportDeclaration or Kind.JSImportDeclaration;
    }

    private static Symbol? getNonModuleSymbolOfMergedModuleSymbol(Symbol symbol)
    {
        if (symbol.Declarations.Count == 0 || (symbol.Flags & (SymbolFlags.Module | SymbolFlags.Transient)) == 0)
            return null;
        var declaration = Core.Find(symbol.Declarations, static node => !Ast.IsSourceFile(node) && !Ast.IsModuleDeclaration(node));
        return declaration?.Symbol();
    }

    private static Symbol getLocalSymbolForExportSpecifier(Node referenceLocation, Symbol referenceSymbol, ExportSpecifier exportSpecifier, Checker checker)
    {
        if (isExportSpecifierAlias(referenceLocation, exportSpecifier))
        {
            var symbol = checker.GetExportSpecifierLocalTargetSymbol(exportSpecifier.AsNode());
            if (symbol != null)
                return symbol;
        }

        return referenceSymbol;
    }

    private static bool isExportSpecifierAlias(Node referenceLocation, ExportSpecifier exportSpecifier)
    {
        TypeScript.Scanner.Debug.Assert(exportSpecifier.PropertyName == referenceLocation || exportSpecifier.Name() == referenceLocation, "referenceLocation is not export specifier name or property name");
        var propertyName = exportSpecifier.PropertyName;
        return propertyName != null ? propertyName == referenceLocation : exportSpecifier.Parent.Parent.ModuleSpecifier() == null;
    }

    private static CommentRange? isInComment(SourceFile file, int position, Node tokenAtPosition) => getRangeOfEnclosingComment(file, position, AstNav.FindPrecedingToken(file, position), tokenAtPosition);
    private static bool positionBelongsToNode(Node candidate, int position, SourceFile file) => LsUtil.PositionBelongsToNode(candidate, position, file);
    // utilities.go:112. Token balancing preserves incomplete generic argument syntax.
    private static PossibleTypeArgumentInfo? getPossibleTypeArgumentsInfo(Node tokenIn, SourceFile sourceFile)
    {
        if (sourceFile.TextValue().LastIndexOf((byte)'<') == -1)
            return null;
        Node? token = tokenIn;
        int remainingLessThanTokens = 0, nTypeArguments = 0;
        while (token != null)
        {
            switch (token.Kind)
            {
                case Kind.LessThanToken:
                    token = AstNav.FindPrecedingToken(sourceFile, token.Pos());
                    if (token != null && token.Kind == Kind.QuestionDotToken)
                        token = AstNav.FindPrecedingToken(sourceFile, token.Pos());
                    if (token == null || !Ast.IsIdentifier(token))
                        return null;
                    if (remainingLessThanTokens == 0)
                    {
                        if (Ast.IsDeclarationName(token))
                            return null;
                        return new PossibleTypeArgumentInfo
                        {
                            called = token,
                            nTypeArguments = nTypeArguments
                        };
                    }

                    remainingLessThanTokens--;
                    break;
                case Kind.GreaterThanGreaterThanGreaterThanToken:
                    remainingLessThanTokens += 3;
                    break;
                case Kind.GreaterThanGreaterThanToken:
                    remainingLessThanTokens += 2;
                    break;
                case Kind.GreaterThanToken:
                    remainingLessThanTokens++;
                    break;
                case Kind.CloseBraceToken:
                    token = findPrecedingMatchingToken(token, Kind.OpenBraceToken, sourceFile);
                    if (token == null)
                        return null;
                    break;
                case Kind.CloseParenToken:
                    token = findPrecedingMatchingToken(token, Kind.OpenParenToken, sourceFile);
                    if (token == null)
                        return null;
                    break;
                case Kind.CloseBracketToken:
                    token = findPrecedingMatchingToken(token, Kind.OpenBracketToken, sourceFile);
                    if (token == null)
                        return null;
                    break;
                case Kind.CommaToken:
                    nTypeArguments++;
                    break;
                case Kind.EqualsGreaterThanToken:
                case Kind.Identifier:
                case Kind.StringLiteral:
                case Kind.NumericLiteral:
                case Kind.BigIntLiteral:
                case Kind.TrueKeyword:
                case Kind.FalseKeyword:
                case Kind.TypeOfKeyword:
                case Kind.ExtendsKeyword:
                case Kind.KeyOfKeyword:
                case Kind.DotToken:
                case Kind.BarToken:
                case Kind.QuestionToken:
                case Kind.ColonToken:
                    break;
                default:
                    if (!Ast.IsTypeNode(token))
                        return null;
                    break;
            }

            token = AstNav.FindPrecedingToken(sourceFile, token.Pos());
        }

        return null;
    }

    private static bool isNameOfModuleDeclaration(Node node) => node.Parent.Kind == Kind.ModuleDeclaration && node.Parent.Name() == node;
    private static bool isExpressionOfExternalModuleImportEqualsDeclaration(Node node) => Ast.IsExternalModuleImportEqualsDeclaration(node.Parent.Parent) && Ast.GetExternalModuleImportEqualsDeclarationExpression(node.Parent.Parent) == node;
    private static bool isNamespaceReference(Node node) => isQualifiedNameNamespaceReference(node) || isPropertyAccessNamespaceReference(node);
    private static bool isQualifiedNameNamespaceReference(Node node)
    {
        var root = node;
        bool isLastClause = true;
        if (root.Parent!.Kind == Kind.QualifiedName)
        {
            while (root.Parent != null && root.Parent!.Kind == Kind.QualifiedName)
                root = root.Parent;
            isLastClause = root.AsQualifiedName().Right == node;
        }

        return root.Parent!.Kind == Kind.TypeReference && !isLastClause;
    }

    private static bool isPropertyAccessNamespaceReference(Node node)
    {
        var root = node;
        bool isLastClause = true;
        if (root.Parent!.Kind == Kind.PropertyAccessExpression)
        {
            while (root.Parent != null && root.Parent!.Kind == Kind.PropertyAccessExpression)
                root = root.Parent;
            isLastClause = root.Name() == node;
        }

        if (!isLastClause && root.Parent!.Kind == Kind.ExpressionWithTypeArguments && root.Parent.Parent.Kind == Kind.HeritageClause)
        {
            var declaration = root.Parent.Parent.Parent;
            return declaration.Kind == Kind.ClassDeclaration && root.Parent.Parent.AsHeritageClause().Token == Kind.ImplementsKeyword
                || declaration.Kind == Kind.InterfaceDeclaration && root.Parent.Parent.AsHeritageClause().Token == Kind.ExtendsKeyword;
        }

        return false;
    }

    private static bool isThis(Node node) => node.Kind == Kind.ThisKeyword || node.Kind == Kind.Identifier && node.TextValue().EqualsString("this") && node.Parent.Kind == Kind.Parameter;
    private static bool isTypeReference(Node node)
    {
        if (Ast.IsRightSideOfQualifiedNameOrPropertyAccess(node))
            node = node.Parent;
        if (node.Kind == Kind.ThisKeyword)
            return !Ast.IsExpressionNode(node);
        if (node.Kind == Kind.ThisType)
            return true;
        switch (node.Parent.Kind)
        {
            case Kind.TypeReference:
                return true;
            case Kind.ImportType:
                return !node.Parent.AsImportTypeNode().IsTypeOf;
            case Kind.ExpressionWithTypeArguments:
                return Ast.IsPartOfTypeNode(node.Parent);
            default:
                return false;
        }
    }

    private static bool isInRightSideOfInternalImportEqualsDeclaration(Node node)
    {
        if (node.Parent == null)
            return false;
        while (node.Parent.Kind == Kind.QualifiedName)
            node = node.Parent;
        return Ast.IsInternalModuleImportEqualsDeclaration(node.Parent) && node.Parent.AsImportEqualsDeclaration().ModuleReference == node;
    }

    private (LspRange Range, Fidelity Fidelity) createLspRangeFromNode(Node node, SourceFile file) => createLspRangeFromBounds(ScannerApi.GetTokenPosOfNode(node, file, false), node.End(), file);
    private (LspRange Range, Fidelity Fidelity) createLspRangeFromNodeForFeature(Node node, SourceFile file, Feature feature) => converters.ToLSPRangeForFeature(file, createRangeFromNode(node, file), feature);
    private static TextRange createRangeFromNode(Node node, SourceFile file) => new(ScannerApi.GetTokenPosOfNode(node, file, false), node.End());
    private (LspRange Range, Fidelity Fidelity) createLspRangeFromBounds(int start, int end, SourceFile file) => converters.ToLSPRange(file, new TextRange(start, end));
    private (LspRange Range, Fidelity Fidelity) createLspRangeFromRange(TextRange range, IScript? script) => converters.ToLSPRange(script!, range);
    private (Position Position, Fidelity Fidelity) createLspPosition(int position, SourceFile file) => converters.ToLSPPosition(file, position);
    // utilities.go:313. Ignore the same marshal error as upstream, then perform one
    // non-overlapping replacement pass over the JSON-escaped interior.
    private static ByteStringValue quote(SourceFile file, UserPreferences preferences, ByteStringValue text)
    {
        var preference = LsUtil.GetQuotePreference(file, preferences);
        var (quoted, _) = Core.StringifyJson(text, default, default);
        if (preference == QuotePreference.Single)
            quoted = ByteStringValue.Concat(ByteStringValue.FromString("'"), quoteReplacer.Replace(StringUtil.StripQuotesValue(quoted)), ByteStringValue.FromString("'"));
        return quoted;
    }

    private static bool isTypeKeyword(Kind kind) => typeKeywords.Contains(kind);
    private static bool isSeparator(Node node, Node? candidate) => candidate != null && node.Parent != null && (candidate.Kind == Kind.CommaToken || candidate.Kind == Kind.SemicolonToken && node.Parent.Kind == Kind.ObjectLiteralExpression);
    private static bool isLiteralNameOfPropertyDeclarationOrIndexAccess(Node node)
    {
        switch (node.Parent.Kind)
        {
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.PropertyAssignment:
            case Kind.EnumMember:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.ModuleDeclaration:
                return Ast.GetNameOfDeclaration(node.Parent) == node;
            case Kind.ElementAccessExpression:
                return node.Parent.AsElementAccessExpression().ArgumentExpression == node;
            case Kind.ComputedPropertyName:
                return true;
            case Kind.LiteralType:
                return node.Parent.Parent.Kind == Kind.IndexedAccessType;
            default:
                return false;
        }
    }

    private static bool isObjectBindingElementWithoutPropertyName(Node bindingElement) => bindingElement.Kind == Kind.BindingElement
        && bindingElement.Parent.Kind == Kind.ObjectBindingPattern && bindingElement.Name().Kind == Kind.Identifier
        && bindingElement.PropertyName() == null;
    private static bool isRightSideOfPropertyAccess(Node node) => node.Parent != null && node.Parent.Kind == Kind.PropertyAccessExpression && node.Parent.Name() == node;
    private static bool isStaticSymbol(Symbol symbol) => symbol.ValueDeclaration != null && (symbol.ValueDeclaration.ModifierFlags() & ModifierFlags.Static) != 0;
    private static bool isImplementation(Node node)
    {
        if ((node.Flags & NodeFlags.Ambient) != 0)
            return node.Kind is not (Kind.InterfaceDeclaration or Kind.TypeAliasDeclaration);
        if (Ast.IsVariableLike(node))
            return Ast.HasInitializer(node);
        if (Ast.IsFunctionLikeDeclaration(node))
            return node.Body() != null;
        return Ast.IsClassLike(node) || Ast.IsModuleOrEnumDeclaration(node);
    }

    private static bool isImplementationExpression(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ParenthesizedExpression:
                return isImplementationExpression(node.Expression());
            case Kind.ArrowFunction:
            case Kind.FunctionExpression:
            case Kind.ObjectLiteralExpression:
            case Kind.ClassExpression:
            case Kind.ArrayLiteralExpression:
                return true;
            default:
                return false;
        }
    }

    private static bool isReadonlyTypeOperator(Node node) => node.Kind == Kind.ReadonlyKeyword && node.Parent.Kind == Kind.TypeOperator && node.Parent.AsTypeOperatorNode().Operator == Kind.ReadonlyKeyword;
    private static bool isJumpStatementTarget(Node node) => node.Kind == Kind.Identifier && Ast.IsBreakOrContinueStatement(node.Parent) && node.Parent.Label() == node;
    private static bool isLabelOfLabeledStatement(Node node) => node.Kind == Kind.Identifier && node.Parent.Kind == Kind.LabeledStatement && node.Parent.Label() == node;
    private static FileReference? findReferenceInPosition(Slice<FileReference> references, int position) => Core.Find(references, position, static (reference, pos) => reference.TextRange.ContainsInclusive(pos));
    private static Node? getContainingNodeIfInHeritageClause(Node node)
    {
        if (node.Kind is Kind.Identifier or Kind.QualifiedName or Kind.PropertyAccessExpression)
            return getContainingNodeIfInHeritageClause(node.Parent);
        if ((node.Kind is Kind.ExpressionWithTypeArguments or Kind.TypeReference) && Ast.IsHeritageClause(node.Parent) && (Ast.IsClassLike(node.Parent.Parent) || node.Parent.Parent.Kind == Kind.InterfaceDeclaration))
            return node.Parent.Parent;
        return null;
    }

    private static Node? getContainerNode(Node node)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            switch (parent.Kind)
            {
                case Kind.SourceFile:
                case Kind.MethodDeclaration:
                case Kind.MethodSignature:
                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.ClassDeclaration:
                case Kind.InterfaceDeclaration:
                case Kind.EnumDeclaration:
                case Kind.ModuleDeclaration:
                    return parent;
            }
        }

        return null;
    }

    // utilities.go:459. Preserve exact keyword adjustment and rename-specific branches.
    private static Node getAdjustedLocation(Node node, bool forRename, SourceFile? sourceFile)
    {
        var parent = node.Parent;
        static bool isModifier(Node node, Node parent, bool forRename)
        {
            if (Ast.IsModifier(node) && (forRename || node.Kind != Kind.DefaultKeyword))
                return Ast.CanHaveModifiers(parent) && Core.Some(parent.ModifierNodes(), node, static (modifier, target) => modifier == target);
            switch (node.Kind)
            {
                case Kind.ClassKeyword:
                    return Ast.IsClassDeclaration(parent) || Ast.IsClassExpression(node);
                case Kind.FunctionKeyword:
                    return Ast.IsFunctionDeclaration(parent) || Ast.IsFunctionExpression(node);
                case Kind.InterfaceKeyword:
                    return Ast.IsInterfaceDeclaration(parent);
                case Kind.EnumKeyword:
                    return Ast.IsEnumDeclaration(parent);
                case Kind.TypeKeyword:
                    return Ast.IsTypeAliasDeclaration(parent);
                case Kind.NamespaceKeyword:
                case Kind.ModuleKeyword:
                    return Ast.IsModuleDeclaration(parent);
                case Kind.ImportKeyword:
                    return Ast.IsImportEqualsDeclaration(parent);
                case Kind.GetKeyword:
                    return Ast.IsGetAccessorDeclaration(parent);
                case Kind.SetKeyword:
                    return Ast.IsSetAccessorDeclaration(parent);
                default:
                    return false;
            }
        }

        if (isModifier(node, parent, forRename))
        {
            sourceFile ??= Ast.GetSourceFileOfNode(node);
            var location = getAdjustedLocationForDeclaration(parent, forRename, sourceFile);
            if (location != null)
                return location;
        }

        if ((node.Kind is Kind.VarKeyword or Kind.ConstKeyword or Kind.LetKeyword) && Ast.IsVariableDeclarationList(parent) && parent.AsVariableDeclarationList().Declarations.Nodes.Count == 1)
        {
            var declaration = parent.AsVariableDeclarationList().Declarations.Nodes[0].AsVariableDeclaration();
            if (Ast.IsIdentifier(declaration.Name()))
                return declaration.Name();
        }

        if (node.Kind == Kind.TypeKeyword)
        {
            if (Ast.IsImportClause(parent) && parent.IsTypeOnly())
            {
                var location = getAdjustedLocationForImportDeclaration(parent.Parent.AsImportDeclaration(), forRename);
                if (location != null)
                    return location;
            }

            if (Ast.IsExportDeclaration(parent) && parent.IsTypeOnly())
            {
                var location = getAdjustedLocationForExportDeclaration(parent.AsExportDeclaration(), forRename);
                if (location != null)
                    return location;
            }
        }

        if (node.Kind == Kind.AsKeyword)
        {
            if (parent.Kind == Kind.ImportSpecifier && parent.PropertyName() != null || parent.Kind == Kind.ExportSpecifier && parent.PropertyName() != null || parent.Kind == Kind.NamespaceImport || parent.Kind == Kind.NamespaceExport)
                return parent.Name();
            if (parent.Kind == Kind.ExportDeclaration)
            {
                var clause = parent.AsExportDeclaration().ExportClause;
                if (clause != null && clause.Kind == Kind.NamespaceExport)
                    return clause.Name();
            }
        }

        if (node.Kind == Kind.ImportKeyword && parent.Kind == Kind.ImportDeclaration)
        {
            var location = getAdjustedLocationForImportDeclaration(parent.AsImportDeclaration(), forRename);
            if (location != null)
                return location;
        }

        if (node.Kind == Kind.ExportKeyword)
        {
            if (parent.Kind == Kind.ExportDeclaration)
            {
                var location = getAdjustedLocationForExportDeclaration(parent.AsExportDeclaration(), forRename);
                if (location != null)
                    return location;
            }

            if (parent.Kind == Kind.ExportAssignment)
                return Ast.SkipOuterExpressions(parent.Expression(), OuterExpressionKinds.All);
        }

        if (node.Kind == Kind.RequireKeyword && parent.Kind == Kind.ExternalModuleReference)
            return parent.Expression();
        if (node.Kind == Kind.FromKeyword && (parent.Kind is Kind.ImportDeclaration or Kind.ExportDeclaration) && parent.ModuleSpecifier() != null)
            return parent.ModuleSpecifier();
        if ((node.Kind is Kind.ExtendsKeyword or Kind.ImplementsKeyword) && parent.Kind == Kind.HeritageClause && parent.AsHeritageClause().Token == node.Kind)
        {
            static Node? getAdjustedLocationForHeritageClause(HeritageClause clause) => clause.Types.Nodes.Count == 1 ? Ast.GetHeritageClauseElementName(clause.Types.Nodes[0]) : null;
            var location = getAdjustedLocationForHeritageClause(parent.AsHeritageClause());
            if (location != null)
                return location;
        }

        if (node.Kind == Kind.ExtendsKeyword)
        {
            if (parent.Kind == Kind.TypeParameter)
            {
                var constraint = parent.AsTypeParameterDeclaration().Constraint;
                if (constraint != null && constraint.Kind == Kind.TypeReference)
                    return constraint.AsTypeReferenceNode().TypeName;
            }

            if (parent.Kind == Kind.ConditionalType)
            {
                var extendsType = parent.AsConditionalTypeNode().ExtendsType;
                if (extendsType != null && extendsType.Kind == Kind.TypeReference)
                    return extendsType.AsTypeReferenceNode().TypeName;
            }
        }

        if (node.Kind == Kind.InferKeyword && parent.Kind == Kind.InferType)
            return parent.AsInferTypeNode().TypeParameter.Name();
        if (node.Kind == Kind.InKeyword && parent.Kind == Kind.TypeParameter && parent.Parent.Kind == Kind.MappedType)
            return parent.Name();
        if (node.Kind == Kind.KeyOfKeyword && parent.Kind == Kind.TypeOperator && parent.AsTypeOperatorNode().Operator == Kind.KeyOfKeyword)
        {
            var type = parent.Type();
            if (type != null && type.Kind == Kind.TypeReference)
                return type.AsTypeReferenceNode().TypeName;
        }

        if (node.Kind == Kind.ReadonlyKeyword && parent.Kind == Kind.TypeOperator && parent.AsTypeOperatorNode().Operator == Kind.ReadonlyKeyword)
        {
            var type = parent.Type();
            if (type != null && type.Kind == Kind.ArrayType && type.AsArrayTypeNode().ElementType.Kind == Kind.TypeReference)
                return type.AsArrayTypeNode().ElementType.AsTypeReferenceNode().TypeName;
        }

        if (!forRename)
        {
            if (node.Kind == Kind.NewKeyword && parent.Kind == Kind.NewExpression
                || node.Kind == Kind.VoidKeyword && parent.Kind == Kind.VoidExpression
                || node.Kind == Kind.TypeOfKeyword && parent.Kind == Kind.TypeOfExpression
                || node.Kind == Kind.AwaitKeyword && parent.Kind == Kind.AwaitExpression
                || node.Kind == Kind.YieldKeyword && parent.Kind == Kind.YieldExpression
                || node.Kind == Kind.DeleteKeyword && parent.Kind == Kind.DeleteExpression)
            {
                var expression = parent.Expression();
                if (expression != null)
                    return Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All);
            }

            if ((node.Kind is Kind.InKeyword or Kind.InstanceOfKeyword) && parent.Kind == Kind.BinaryExpression && parent.AsBinaryExpression().OperatorToken == node)
                return Ast.SkipOuterExpressions(parent.AsBinaryExpression().Right, OuterExpressionKinds.All);
            if (node.Kind == Kind.AsKeyword && parent.Kind == Kind.AsExpression)
            {
                var type = parent.Type();
                if (type != null && type.Kind == Kind.TypeReference)
                    return type.AsTypeReferenceNode().TypeName;
            }

            if (node.Kind == Kind.InKeyword && parent.Kind == Kind.ForInStatement || node.Kind == Kind.OfKeyword && parent.Kind == Kind.ForOfStatement)
                return Ast.SkipOuterExpressions(parent.Expression(), OuterExpressionKinds.All);
        }

        return node;
    }

    // utilities.go:697. The original predicate intentionally reads the enclosing node.
    private static Node? getAdjustedLocationForDeclaration(Node node, bool forRename, SourceFile sourceFile)
    {
        if (node.Name() != null)
            return node.Name();
        if (forRename)
            return null;
        switch (node.Kind)
        {
            case Kind.ClassDeclaration:
            case Kind.FunctionDeclaration:
                return Core.Find(node.ModifierNodes(), node, static (_, declaration) => declaration.Kind == Kind.DefaultKeyword);
            case Kind.ClassExpression:
                return AstNav.FindChildOfKind(node, Kind.ClassKeyword, sourceFile);
            case Kind.FunctionExpression:
                return AstNav.FindChildOfKind(node, Kind.FunctionKeyword, sourceFile);
            case Kind.Constructor:
                return node;
            default:
                return null;
        }
    }

    private static Node? getAdjustedLocationForImportDeclaration(ImportDeclaration node, bool forRename)
    {
        if (node.ImportClause != null)
        {
            var name = node.ImportClause.Name();
            if (name != null)
            {
                if (node.ImportClause.AsImportClause().NamedBindings != null)
                    return null;
                return node.ImportClause.Name();
            }

            var bindings = node.ImportClause.AsImportClause().NamedBindings;
            if (bindings != null)
            {
                switch (bindings.Kind)
                {
                    case Kind.NamedImports:
                        var elements = bindings.Elements();
                        return elements.Count != 1 ? null : elements[0].Name();
                    case Kind.NamespaceImport:
                        return bindings.Name();
                }
            }
        }

        return forRename ? null : node.ModuleSpecifier;
    }

    private static Node? getAdjustedLocationForExportDeclaration(ExportDeclaration node, bool forRename)
    {
        if (node.ExportClause != null)
        {
            switch (node.ExportClause.Kind)
            {
                case Kind.NamedExports:
                    var elements = node.ExportClause.Elements();
                    return elements.Count != 1 ? null : elements[0].Name();
                case Kind.NamespaceExport:
                    return node.ExportClause.Name();
            }
        }

        return forRename ? null : node.ModuleSpecifier;
    }

    // utilities.go:790. Preserve priority rather than combining all requested meaning tests.
    private static bool symbolFlagsHaveMeaning(SymbolFlags flags, SemanticMeaning meaning)
    {
        if (meaning == SemanticMeaning.All)
            return true;
        if ((meaning & SemanticMeaning.Value) != 0)
            return (flags & SymbolFlags.Value) != 0;
        if ((meaning & SemanticMeaning.Type) != 0)
            return (flags & SymbolFlags.Type) != 0;
        if ((meaning & SemanticMeaning.Namespace) != 0)
            return (flags & SymbolFlags.Namespace) != 0;
        return false;
    }

    private static SemanticMeaning getMeaningFromLocation(Node node)
    {
        node = getAdjustedLocation(Ast.GetReparsedNodeForNode(node), false, null);
        var parent = node.Parent;
        if (Ast.IsSourceFile(node))
            return SemanticMeaning.Value;
        if (parent.Kind is Kind.ExportAssignment or Kind.ExportSpecifier or Kind.ExternalModuleReference or Kind.ImportSpecifier or Kind.ImportClause || parent.Kind == Kind.ImportEqualsDeclaration && node == parent.Name())
            return SemanticMeaning.All;
        if (isInRightSideOfInternalImportEqualsDeclaration(node))
        {
            Node? name = node;
            if (node.Kind != Kind.QualifiedName)
                name = node.Parent.Kind == Kind.QualifiedName && node.Parent.AsQualifiedName().Right == node ? node.Parent : null;
            if (name != null && name.Parent.Kind == Kind.ImportEqualsDeclaration)
                return SemanticMeaning.All;
            return SemanticMeaning.Namespace;
        }

        if (Ast.IsDeclarationName(node))
            return getMeaningFromDeclaration(parent);
        if (Ast.IsEntityName(node) && Ast.IsJSDocNameReferenceContext(node))
            return SemanticMeaning.All;
        if (isTypeReference(node))
            return SemanticMeaning.Type;
        if (isNamespaceReference(node))
            return SemanticMeaning.Namespace;
        if (Ast.IsTypeParameterDeclaration(parent))
            return SemanticMeaning.Type;
        if (Ast.IsLiteralTypeNode(parent))
            return SemanticMeaning.Type | SemanticMeaning.Value;
        return SemanticMeaning.Value;
    }

    private static SemanticMeaning getMeaningFromDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.BindingElement:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.CatchClause:
            case Kind.JsxAttribute:
                return SemanticMeaning.Value;
            case Kind.TypeParameter:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.TypeLiteral:
                return SemanticMeaning.Type;
            case Kind.EnumMember:
            case Kind.ClassDeclaration:
                return SemanticMeaning.Value | SemanticMeaning.Type;
            case Kind.EnumDeclaration:
            case Kind.NamedImports:
            case Kind.ImportSpecifier:
            case Kind.ImportEqualsDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportAssignment:
            case Kind.ExportDeclaration:
                return SemanticMeaning.All;
            case Kind.SourceFile:
                return SemanticMeaning.Namespace | SemanticMeaning.Value;
            case Kind.ModuleDeclaration:
                return Ast.IsAmbientModule(node) || Ast.GetModuleInstanceState(node) == ModuleInstanceState.Instantiated ? SemanticMeaning.Namespace | SemanticMeaning.Value : SemanticMeaning.Namespace;
            default:
                return SemanticMeaning.All;
        }
    }

    private static SemanticMeaning getIntersectingMeaningFromDeclarations(Node? node, Symbol symbol, SemanticMeaning defaultMeaning)
    {
        if (node == null)
            return defaultMeaning;
        var meaning = getMeaningFromLocation(node);
        var declarations = symbol.Declarations;
        if (declarations.Count == 0)
            return meaning;
        var lastIterationMeaning = meaning;
        static SemanticMeaning iteration(SemanticMeaning meaning, Slice<Node> declarations)
        {
            foreach (var declaration in declarations)
            {
                var declarationMeaning = getMeaningFromDeclaration(declaration);
                if ((declarationMeaning & meaning) != 0)
                    meaning |= declarationMeaning;
            }

            return meaning;
        }

        meaning = iteration(meaning, declarations);
        while (meaning != lastIterationMeaning)
        {
            lastIterationMeaning = meaning;
            meaning = iteration(meaning, declarations);
        }

        return meaning;
    }

    private static Slice<Node> getAllSuperTypeNodes(Node node)
    {
        if (Ast.IsInterfaceDeclaration(node))
            return Ast.GetHeritageElements(node, Kind.ExtendsKeyword);
        if (Ast.IsClassLike(node))
            return Core.SingleElementSlice(Ast.GetClassExtendsHeritageElement(node)).Append(Ast.GetImplementsHeritageClauseElements(node));
        return default;
    }

    private static Slice<Symbol> getParentSymbolsOfPropertyAccess(Node location, Symbol symbol, Checker checker)
    {
        if (!isRightSideOfPropertyAccess(location))
            return default;
        var leftType = checker.GetTypeAtLocation(location.Parent.Expression());
        if (leftType == null)
            return default;
        Slice<TypeScript.Checking.Type> possibleSymbols = default;
        if ((leftType.Flags() & TypeFlags.UnionOrIntersection) != 0)
            possibleSymbols = leftType.Types();
        else if (leftType.Symbol() != symbol.Parent)
            possibleSymbols = Slice<TypeScript.Checking.Type>.FromArray([leftType]);
        return Core.MapNonNilSlice<TypeScript.Checking.Type, Symbol>(possibleSymbols, static type => type.Symbol() != null && (type.Symbol().Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0 ? type.Symbol() : null!);
    }

    // utilities.go:957. Recursive worker and its one seen-set owner retain the original query lifetime.
    private static Symbol? getPropertySymbolsFromBaseTypes(Symbol symbol, ByteStringValue propertyName, Checker checker, Func<Symbol, Symbol?> callback)
    {
        HashSet<Symbol>? seen = null;
        static Symbol? recur(Symbol symbol, ByteStringValue propertyName, Checker checker, Func<Symbol, Symbol?> callback, ref HashSet<Symbol>? seen)
        {
            if ((symbol.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) == 0 || !(seen ??= new()).Add(symbol))
                return null;
            foreach (var declaration in symbol.Declarations)
                foreach (var typeReference in getAllSuperTypeNodes(declaration))
                {
                    var propertyType = checker.GetTypeAtLocation(typeReference);
                    if (propertyType != null && propertyType.Symbol() != null)
                    {
                        var property = checker.GetPropertyOfType(propertyType, propertyName);
                        if (property != null)
                            foreach (var rootSymbol in checker.GetRootSymbols(property))
                            {
                                var result = callback(rootSymbol);
                                if (result != null)
                                    return result;
                            }

                        var recursiveResult = recur(propertyType.Symbol(), propertyName, checker, callback, ref seen);
                        if (recursiveResult != null)
                            return recursiveResult;
                    }
                }

            return null;
        }

        return recur(symbol, propertyName, checker, callback, ref seen);
    }

    private static Symbol? getPropertySymbolFromBindingElement(Checker checker, Node bindingElement)
    {
        var patternType = checker.GetTypeAtLocation(bindingElement.Parent);
        return patternType != null ? checker.GetPropertyOfType(patternType, bindingElement.Name().TextValue()) : null;
    }

    private static Symbol? getPropertySymbolOfObjectBindingPatternWithoutPropertyName(Symbol symbol, Checker checker)
    {
        var bindingElement = Ast.GetDeclarationOfKind(symbol, Kind.BindingElement);
        return bindingElement != null && isObjectBindingElementWithoutPropertyName(bindingElement) ? getPropertySymbolFromBindingElement(checker, bindingElement) : null;
    }

    private static Node? getTargetLabel(Node? referenceNode, ByteStringValue labelName)
    {
        while (referenceNode != null)
        {
            if (referenceNode.Kind == Kind.LabeledStatement && referenceNode.Label().TextValue().EqualsValue(labelName))
                return referenceNode.Label();
            referenceNode = referenceNode.Parent;
        }

        return null;
    }

    private static TypeScript.Checking.Type skipConstraint(TypeScript.Checking.Type type, Checker checker)
    {
        if (type.IsTypeParameter())
        {
            var constraint = checker.GetBaseConstraintOfType(type);
            if (constraint != null)
                return constraint;
        }

        return type;
    }

    // utilities.go:1033/1075. Go Set value owners start with nil backing maps; allocate on first insertion.
    private sealed class CaseClauseTrackerState
    {
        private HashSet<ByteStringKey>? existingStrings;
        private HashSet<JsNum.Number>? existingNumbers;
        private HashSet<JsNum.PseudoBigInt>? existingBigInts;
        internal void addValue(object? value)
        {
            switch (value)
            {
                case string text:
                    (existingStrings ??= new()).Add(ByteStringKey.FromValue(ByteStringValue.FromString(text)));
                    break;
                case ByteStringValue text:
                    (existingStrings ??= new()).Add(ByteStringKey.FromValue(text));
                    break;
                case JsNum.Number number:
                    (existingNumbers ??= new()).Add(number);
                    break;
                default:
                    throw unsupportedTrackerValue(value);
            }
        }

        internal bool hasValue(object? value) => value switch
        {
            string text => existingStrings?.Contains(ByteStringKey.FromValue(ByteStringValue.FromString(text))) ?? false,
            ByteStringValue text => existingStrings?.Contains(ByteStringKey.FromValue(text)) ?? false,
            JsNum.Number number => existingNumbers?.Contains(number) ?? false,
            JsNum.PseudoBigInt number => existingBigInts?.Contains(number) ?? false,
            _ => throw unsupportedTrackerValue(value)};
        // The source's private producer union is string/Number/PseudoBigInt.
        // Its invalid add branch can receive the BigInt variant; nil keeps fmt's spelling.
        private static InvalidOperationException unsupportedTrackerValue(object? value) => new("Unsupported type: " + (value switch
        {
            null => "<nil>",
            JsNum.PseudoBigInt => "jsnum.PseudoBigInt",
            JsNum.Number => "jsnum.Number",
            string or ByteStringValue => "string",
            bool => "bool",
            int or long => "int",
            double => "float64",
            _ => throw new InvalidOperationException("case clause tracker received a value outside its source producer union")}));
        internal void addString(ByteStringValue value) => (existingStrings ??= new()).Add(ByteStringKey.FromValue(value));
        internal void addNumber(JsNum.Number value) => (existingNumbers ??= new()).Add(value);
        internal void addBigInt(JsNum.PseudoBigInt value) => (existingBigInts ??= new()).Add(value);
    }

    private static CaseClauseTrackerState newCaseClauseTracker(Checker checker, Slice<Node> clauses)
    {
        var tracker = new CaseClauseTrackerState();
        foreach (var clause in clauses)
        {
            if (Ast.IsDefaultClause(clause))
                continue;
            var expression = Ast.SkipParentheses(clause.Expression());
            if (Ast.IsLiteralExpression(expression))
            {
                switch (expression.Kind)
                {
                    case Kind.NoSubstitutionTemplateLiteral:
                    case Kind.StringLiteral:
                        tracker.addString(expression.TextValue());
                        break;
                    case Kind.NumericLiteral:
                        tracker.addNumber(JsNum.FromString(expression.TextValue()));
                        break;
                    case Kind.BigIntLiteral:
                        tracker.addBigInt(JsNum.ParseValidBigInt(expression.TextValue()));
                        break;
                }
            }
            else
            {
                var symbol = checker.GetSymbolAtLocation(clause.Expression());
                if (symbol?.ValueDeclaration != null && Ast.IsEnumMember(symbol.ValueDeclaration))
                {
                    var value = checker.GetConstantValue(symbol.ValueDeclaration);
                    if (value != null)
                        tracker.addValue(value);
                }
            }
        }

        return tracker;
    }

    // utilities.go:1108.
    internal static bool RangeContainsRange(TextRange outer, TextRange inner) => startEndContainsRange(outer.Pos(), outer.End(), inner);
    private static bool startEndContainsRange(int start, int end, TextRange range) => start <= range.Pos() && end >= range.End();
    private static Slice<Signature> getPossibleGenericSignatures(Node called, int typeArgumentCount, Checker checker)
    {
        var type = checker.GetTypeAtLocation(called);
        if (Ast.IsOptionalChain(called.Parent))
            type = removeOptionality(type, Ast.IsOptionalChainRoot(called.Parent), true, checker);
        var signatures = checker.GetSignaturesOfTypeSlice(type, Ast.IsNewExpression(called.Parent) ? SignatureKind.Construct : SignatureKind.Call);
        return Core.Filter(signatures, typeArgumentCount, static (signature, count) => !signature.TypeParameters().IsNil && signature.TypeParameters().Length >= count);
    }

    private static TypeScript.Checking.Type removeOptionality(TypeScript.Checking.Type type, bool isOptionalExpression, bool isOptionalChain, Checker checker)
    {
        if (isOptionalExpression)
            return checker.GetNonNullableType(type);
        if (isOptionalChain)
            return checker.GetNonOptionalType(type);
        return type;
    }

    private static bool isNoSubstitutionTemplateLiteral(Node node) => node.Kind == Kind.NoSubstitutionTemplateLiteral;
    private static bool isTaggedTemplateExpression(Node node) => node.Kind == Kind.TaggedTemplateExpression;
    private static bool isInsideTemplateLiteral(Node node, int position, SourceFile file) => Ast.IsTemplateLiteralKind(node.Kind)
        && (ScannerApi.GetTokenPosOfNode(node, file, false) < position && position < node.End()
        || Ast.IsUnterminatedLiteral(node) && position == node.End());
    private static bool isTemplateHead(Node node) => node.Kind == Kind.TemplateHead;
    private static bool isTemplateTail(Node node) => node.Kind == Kind.TemplateTail;
    // utilities.go:1149. The textual fast path uses byte offsets, including malformed UTF8.
    private static Node? findPrecedingMatchingToken(Node token, Kind matchingKind, SourceFile file)
    {
        var closeText = ByteStringValue.FromString(ScannerApi.TokenToString(token.Kind));
        var matchingText = ByteStringValue.FromString(ScannerApi.TokenToString(matchingKind));
        int bestGuess = file.TextValue().LastIndexOf(matchingText);
        if (bestGuess == -1)
            return null;
        if (file.TextValue().LastIndexOf(closeText) < bestGuess)
        {
            var guessed = AstNav.FindPrecedingToken(file, bestGuess + 1);
            if (guessed != null && guessed.Kind == matchingKind)
                return guessed;
        }

        var tokenKind = token.Kind;
        int remaining = 0;
        while (true)
        {
            var preceding = AstNav.FindPrecedingToken(file, token.Pos());
            if (preceding == null)
                return null;
            token = preceding;
            if (token.Kind == matchingKind)
            {
                if (remaining == 0)
                    return token;
                remaining--;
            }
            else if (token.Kind == tokenKind)
                remaining++;
        }
    }

    // utilities.go:1181. This query owns one pair of callbacks, as the original does;
    // its child visitor is not retained by any AST owner.
    private static NodeList? findContainingList(Node node, SourceFile file)
    {
        NodeList? list = null;
        AstNav.VisitEachChildAndJSDoc(node.Parent, file, static (child, visitor) => child, (nodes, visitor) =>
        {
            if (nodes != null && RangeContainsRange(nodes.Loc, node.Loc))
                list = nodes;
            return nodes;
        });
        return list;
    }

    private static ScannerApi.CommentRangeSequence getLeadingCommentRangesOfNode(Node node, SourceFile file) => node.Kind == Kind.JsxText ? default : ScannerApi.GetLeadingCommentRangesValue(null, file.TextView(), node.Pos());
    // utilities.go:1206. Keep the required collected children and token output owners.
    private static Slice<Node> getChildrenFromNonJSDocNode(Node node, SourceFile file)
    {
        Slice<Node> childNodes = default;
        node.ForEachChild(child =>
        {
            childNodes = childNodes.Append(child);
            return false;
        });
        if (childNodes.Length == 0)
            return default;
        Slice<Node> children = default;
        int position = node.Pos();
        foreach (var child in childNodes)
        {
            var scanner = ScannerApi.GetScannerForSourceFile(file, position);
            while (position < child.Pos())
            {
                int end = scanner.TokenEnd();
                children = children.Append(file.GetOrCreateToken(scanner.Token(), scanner.TokenFullStart(), end, node, scanner.TokenFlags()));
                position = end;
                scanner.Scan();
            }

            children = children.Append(child);
            position = child.End();
        }

        var trailingScanner = ScannerApi.GetScannerForSourceFile(file, position);
        while (position < node.End())
        {
            int end = trailingScanner.TokenEnd();
            children = children.Append(file.GetOrCreateToken(trailingScanner.Token(), trailingScanner.TokenFullStart(), end, node, trailingScanner.TokenFlags()));
            position = end;
            trailingScanner.Scan();
        }

        return children;
    }

    private static Node? getContainingObjectLiteralElement(Node node)
    {
        var element = getContainingObjectLiteralElementWorker(node);
        return element != null && (Ast.IsObjectLiteralExpression(element.Parent) || Ast.IsJsxAttributes(element.Parent)) ? element : null;
    }

    private static Node? getContainingObjectLiteralElementWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
                if (node.Parent.Kind == Kind.ComputedPropertyName)
                    return isObjectLiteralOrJsxElement(node.Parent.Parent) ? node.Parent.Parent : null;
                goto case Kind.Identifier;
            case Kind.Identifier:
            case Kind.JsxNamespacedName:
                if (isObjectLiteralOrJsxElement(node.Parent) && node.Parent.Parent.Kind is Kind.ObjectLiteralExpression or Kind.JsxAttributes && node.Parent.Name() == node)
                    return node.Parent;
                break;
        }

        return null;
    }

    private static bool isObjectLiteralOrJsxElement(Node node) => Ast.IsObjectLiteralElement(node) || Ast.IsJsxAttribute(node) || Ast.IsJsxSpreadAttribute(node);
    private static Func<Node, bool> nodeSeenTracker()
    {
        HashSet<Node>? seen = null;
        return node => (seen ??= new()).Add(node);
    }

    private static TextRange? toContextRange(TextRange? range, SourceFile file, Node? context)
    {
        if (context == null)
            return range;
        var contextRange = getRangeOfNode(context, file, null);
        return contextRange.Pos() != range!.Value.Pos() || contextRange.End() != range.Value.End() ? contextRange : null;
    }

    // utilities.go:1307. Preserve the original resolution and unverified-flag spelling.
    private static RefInfo? getReferenceAtPosition(SourceFile file, int position, TypeScript.Compiler.Program program)
    {
        var referencePath = findReferenceInPosition(file.ReferencedFiles, position);
        if (referencePath != null)
        {
            var referencedFile = program.GetSourceFileFromReference(file, referencePath);
            return referencedFile != null ? new RefInfo
            {
                reference = referencePath,
                fileName = RootedFilePath.FromValue(referencedFile.FileNameGoValue()),
                file = referencedFile
            }

            : null;
        }

        var typeReference = findReferenceInPosition(file.TypeReferenceDirectives, position);
        if (typeReference != null)
        {
            var resolution = program.GetResolvedTypeReferenceDirectiveFromTypeReferenceDirective(typeReference, file);
            var referencedFile = resolution != null ? program.GetSourceFile(resolution.ResolvedFileName) : null;
            return referencedFile != null ? new RefInfo
            {
                reference = typeReference,
                fileName = RootedFilePath.FromValue(referencedFile.FileNameGoValue()),
                file = referencedFile
            }

            : null;
        }

        var libReference = findReferenceInPosition(file.LibReferenceDirectives, position);
        if (libReference != null)
        {
            var referencedFile = program.GetLibFileFromReference(libReference);
            return referencedFile != null ? new RefInfo
            {
                reference = libReference,
                fileName = RootedFilePath.FromValue(referencedFile.FileNameGoValue()),
                file = referencedFile
            }

            : null;
        }

        if (file.Imports().Length == 0 && file.ModuleAugmentations.Length == 0)
            return null;
        var node = AstNav.GetTouchingToken(file, position);
        if (!isModuleSpecifierLike(node) || !TsPath.IsExternalModuleNameRelative(node.TextValue()))
            return null;
        var module = program.GetResolvedModuleFromModuleSpecifier(file, node);
        if (module == null)
            return null;
        var verifiedName = module.ResolvedFileName;
        var fileName = verifiedName;
        if (fileName.AsValue().Length == 0)
            fileName = RootedFilePath.FromValue(file.FileNameGoValue()).Directory().ResolveFile(node.TextValue());
        return new RefInfo
        {
            file = program.GetSourceFile(fileName),
            fileName = fileName,
            reference = null,
            unverified = verifiedName.AsValue().Length != 0
        };
    }

    private static TypeScript.Checking.Type? getContextualTypeFromParent(Node node, Checker checker, ContextFlags flags)
    {
        var parent = Ast.WalkUpParenthesizedExpressions(node.Parent);
        switch (parent.Kind)
        {
            case Kind.NewExpression:
                return checker.GetContextualType(parent, flags);
            case Kind.BinaryExpression:
                if (isEqualityOperatorKind(parent.AsBinaryExpression().OperatorToken.Kind))
                    return checker.GetTypeAtLocation(node == parent.AsBinaryExpression().Right ? parent.AsBinaryExpression().Left : parent.AsBinaryExpression().Right);
                return checker.GetContextualType(node, flags);
            case Kind.CaseClause:
                return getSwitchedType(parent, checker);
            default:
                return checker.GetContextualType(node, flags);
        }
    }

    private static TypeScript.Checking.Type? getContextualTypeFromParentOrAncestorTypeNode(Node node, Checker checker)
    {
        if ((node.Flags & NodeFlags.JSDoc) != 0 && (node.Flags & NodeFlags.JavaScriptFile) == 0)
            return null;
        var contextual = getContextualTypeFromParent(node, checker, ContextFlags.None);
        if (contextual != null)
            return contextual;
        var ancestor = getAncestorTypeNode(node);
        return ancestor != null ? checker.GetTypeAtLocation(ancestor) : null;
    }

    private static Node? getAncestorTypeNode(Node node)
    {
        Node? lastTypeNode = null;
        Ast.FindAncestor(node, ancestor =>
        {
            if (Ast.IsTypeNode(ancestor))
                lastTypeNode = ancestor;
            return !Ast.IsQualifiedName(ancestor.Parent) && !Ast.IsTypeNode(ancestor.Parent) && !Ast.IsTypeElement(ancestor.Parent);
        });
        return lastTypeNode;
    }

    private static bool isSourceFileWithGlobalExports(Node? node) => node != null && Ast.IsSourceFile(node) && node.AsSourceFile().GlobalExports != null;
}
