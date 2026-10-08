// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/utilities.go; parser-required syntax dependency slice.
#nullable disable
namespace TypeScript.Scanner;
public static partial class Ast
{
    // Upstream function: NodeIsMissing
    public static bool NodeIsMissing(Node node)
    {
        return ((node == null) || (((node.Loc.Pos() == node.Loc.End()) && (node.Loc.Pos() >= 0)) && (node.Kind != Kind.EndOfFile)));
    }

    // Upstream function: NodeIsPresent
    public static bool NodeIsPresent(Node node)
    {
        return !NodeIsMissing(node);
    }

    // Upstream function: NodeKindIs
    public static bool NodeKindIs(Node node, params Kind[] kinds)
    {
        return kinds.Contains(node.Kind);
    }

    // Upstream function: IsModifier
    public static bool IsModifier(Node node)
    {
        return IsModifierKind(node.Kind);
    }

    // Upstream function: IsAssignmentExpression
    public static bool IsAssignmentExpression(Node node, bool excludeCompoundAssignment)
    {
        if ((node.Kind == Kind.BinaryExpression))
        {
            var expr = node.AsBinaryExpression();
            return ((((expr.OperatorToken.Kind == Kind.EqualsToken) || (!excludeCompoundAssignment && IsAssignmentOperator(expr.OperatorToken.Kind)))) && IsLeftHandSideExpression(expr.Left));
        }

        return false;
    }

    // Upstream function: GetRightMostAssignedExpression
    public static Node GetRightMostAssignedExpression(Node node)
    {
        while (IsAssignmentExpression(node, false))
        {
            node = node.AsBinaryExpression().Right;
        }

        return node;
    }

    // Upstream function: IsStringLiteralLike
    public static bool IsStringLiteralLike(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream function: IsStringOrNumericLiteralLike
    public static bool IsStringOrNumericLiteralLike(Node node)
    {
        return (IsStringLiteralLike(node) || IsNumericLiteral(node));
    }

    // Upstream function: IsOptionalChain
    public static bool IsOptionalChain(Node node)
    {
        if (((node.Flags & NodeFlags.OptionalChain) != 0))
        {
            switch (node.Kind)
            {
                case Kind.PropertyAccessExpression:
                case Kind.ElementAccessExpression:
                case Kind.CallExpression:
                case Kind.NonNullExpression:
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream function: isLeftHandSideExpressionKind
    public static bool isLeftHandSideExpressionKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            case Kind.NewExpression:
            case Kind.CallExpression:
            case Kind.JsxElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxFragment:
            case Kind.TaggedTemplateExpression:
            case Kind.ArrayLiteralExpression:
            case Kind.ParenthesizedExpression:
            case Kind.ObjectLiteralExpression:
            case Kind.ClassExpression:
            case Kind.FunctionExpression:
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
            case Kind.RegularExpressionLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateExpression:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
            case Kind.ThisKeyword:
            case Kind.TrueKeyword:
            case Kind.SuperKeyword:
            case Kind.NonNullExpression:
            case Kind.ExpressionWithTypeArguments:
            case Kind.MetaProperty:
            case Kind.ImportKeyword:
            case Kind.MissingDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream function: IsLeftHandSideExpression
    public static bool IsLeftHandSideExpression(Node node)
    {
        return isLeftHandSideExpressionKind(SkipPartiallyEmittedExpressions(node).Kind);
    }

    // Upstream function: IsAccessExpression
    public static bool IsAccessExpression(Node node)
    {
        return ((node.Kind == Kind.PropertyAccessExpression) || (node.Kind == Kind.ElementAccessExpression));
    }

    // Upstream function: isFunctionLikeDeclarationKind
    public static bool isFunctionLikeDeclarationKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.MethodDeclaration:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream function: IsFunctionLikeKind
    public static bool IsFunctionLikeKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.MethodSignature:
            case Kind.CallSignature:
            case Kind.JSDocSignature:
            case Kind.ConstructSignature:
            case Kind.IndexSignature:
            case Kind.FunctionType:
            case Kind.ConstructorType:
            {
                return true;
            }
        }

        return isFunctionLikeDeclarationKind(kind);
    }

    // Upstream function: IsFunctionLike
    public static bool IsFunctionLike(Node node)
    {
        return ((node != null) && IsFunctionLikeKind(node.Kind));
    }

    // Upstream function: IsTypeNodeKind
    public static bool IsTypeNodeKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.ObjectKeyword:
            case Kind.BooleanKeyword:
            case Kind.StringKeyword:
            case Kind.SymbolKeyword:
            case Kind.VoidKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NeverKeyword:
            case Kind.IntrinsicKeyword:
            case Kind.ExpressionWithTypeArguments:
            case Kind.JSDocAllType:
            case Kind.JSDocNullableType:
            case Kind.JSDocNonNullableType:
            case Kind.JSDocOptionalType:
            case Kind.JSDocVariadicType:
            {
                return true;
            }
        }

        return ((kind >= Kind.FirstTypeNode) && (kind <= Kind.LastTypeNode));
    }

    // Upstream function: IsTypeNode
    public static bool IsTypeNode(Node node)
    {
        return IsTypeNodeKind(node.Kind);
    }

    // Upstream function: IsJSDocTypeAssertion
    public static bool IsJSDocTypeAssertion(Node node)
    {
        if ((((node == null) || !IsParenthesizedExpression(node)) || !IsInJSFile(node)))
        {
            return false;
        }

        var expr = node.Expression();
        return ((IsAsExpression(expr) && (expr.Type() != null)) && ((expr.Type().Flags & NodeFlags.Reparsed) != 0));
    }

    // Upstream function: IsOuterExpression
    public static bool IsOuterExpression(Node node, OuterExpressionKinds kinds)
    {
        switch (node.Kind)
        {
            case Kind.ParenthesizedExpression:
            {
                return (((kinds & OuterExpressionKinds.Parentheses) != 0) && !((((kinds & OuterExpressionKinds.ExcludeJSDocTypeAssertion) != 0) && IsJSDocTypeAssertion(node))));
            }

            case Kind.TypeAssertionExpression:
            case Kind.AsExpression:
            {
                return ((kinds & OuterExpressionKinds.TypeAssertions) != 0);
            }

            case Kind.SatisfiesExpression:
            {
                return ((kinds & ((OuterExpressionKinds.ExpressionsWithTypeArguments | OuterExpressionKinds.Satisfies))) != 0);
            }

            case Kind.ExpressionWithTypeArguments:
            {
                return ((kinds & OuterExpressionKinds.ExpressionsWithTypeArguments) != 0);
            }

            case Kind.NonNullExpression:
            {
                return ((kinds & OuterExpressionKinds.NonNullAssertions) != 0);
            }

            case Kind.PartiallyEmittedExpression:
            {
                return ((kinds & OuterExpressionKinds.PartiallyEmittedExpressions) != 0);
            }

            case Kind.BinaryExpression:
            {
                switch (node.AsBinaryExpression().OperatorToken.Kind)
                {
                    case Kind.EqualsToken:
                    {
                        return ((kinds & OuterExpressionKinds.Assignments) != 0);
                    }

                    case Kind.CommaToken:
                    {
                        return ((kinds & OuterExpressionKinds.Comma) != 0);
                    }
                }

                break;
            }
        }

        return false;
    }

    // Upstream function: SkipOuterExpressions
    public static Node SkipOuterExpressions(Node node, OuterExpressionKinds kinds)
    {
        while (IsOuterExpression(node, kinds))
        {
            if (IsBinaryExpression(node))
            {
                node = node.AsBinaryExpression().Right;
            }
            else
            {
                node = node.Expression();
            }
        }

        return node;
    }

    // Upstream function: SkipParentheses
    public static Node SkipParentheses(Node node)
    {
        return SkipOuterExpressions(node, OuterExpressionKinds.Parentheses);
    }

    // Upstream function: SkipPartiallyEmittedExpressions
    public static Node SkipPartiallyEmittedExpressions(Node node)
    {
        return SkipOuterExpressions(node, OuterExpressionKinds.PartiallyEmittedExpressions);
    }

    // Upstream function: GetSourceFileOfNode
    public static SourceFile GetSourceFileOfNode(Node node)
    {
        while ((node != null))
        {
            if ((node.Kind == Kind.SourceFile))
            {
                return node.AsSourceFile();
            }

            node = node.Parent;
        }

        return null;
    }

    // Upstream function: SetImportsOfSourceFile
    public static void SetImportsOfSourceFile(SourceFile node, List<Node> imports)
    {
        node.imports = imports;
    }

    // Upstream function: ModifierToFlag
    public static ModifierFlags ModifierToFlag(Kind token)
    {
        switch (token)
        {
            case Kind.StaticKeyword:
            {
                return ModifierFlags.Static;
            }

            case Kind.PublicKeyword:
            {
                return ModifierFlags.Public;
            }

            case Kind.ProtectedKeyword:
            {
                return ModifierFlags.Protected;
            }

            case Kind.PrivateKeyword:
            {
                return ModifierFlags.Private;
            }

            case Kind.AbstractKeyword:
            {
                return ModifierFlags.Abstract;
            }

            case Kind.AccessorKeyword:
            {
                return ModifierFlags.Accessor;
            }

            case Kind.ExportKeyword:
            {
                return ModifierFlags.Export;
            }

            case Kind.DeclareKeyword:
            {
                return ModifierFlags.Ambient;
            }

            case Kind.ConstKeyword:
            {
                return ModifierFlags.Const;
            }

            case Kind.DefaultKeyword:
            {
                return ModifierFlags.Default;
            }

            case Kind.AsyncKeyword:
            {
                return ModifierFlags.Async;
            }

            case Kind.ReadonlyKeyword:
            {
                return ModifierFlags.Readonly;
            }

            case Kind.OverrideKeyword:
            {
                return ModifierFlags.Override;
            }

            case Kind.InKeyword:
            {
                return ModifierFlags.In;
            }

            case Kind.OutKeyword:
            {
                return ModifierFlags.Out;
            }

            case Kind.Decorator:
            {
                return ModifierFlags.Decorator;
            }
        }

        return ModifierFlags.None;
    }

    // Upstream function: ModifiersToFlags
    public static ModifierFlags ModifiersToFlags(List<Node> modifiers)
    {
        ModifierFlags flags = default;
        foreach (var modifier in modifiers ?? [])
        {
            flags |= ModifierToFlag(modifier.Kind);
        }

        return flags;
    }

    // Upstream function: HasSyntacticModifier
    public static bool HasSyntacticModifier(Node node, ModifierFlags flags)
    {
        return ((node.ModifierFlags() & flags) != 0);
    }

    // Upstream function: CanHaveIllegalDecorators
    public static bool CanHaveIllegalDecorators(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.FunctionDeclaration:
            case Kind.Constructor:
            case Kind.IndexSignature:
            case Kind.ClassStaticBlockDeclaration:
            case Kind.MissingDeclaration:
            case Kind.VariableStatement:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.EnumDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.NamespaceExportDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ExportAssignment:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream function: CanHaveDecorators
    public static bool CanHaveDecorators(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.ClassExpression:
            case Kind.ClassDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream function: IsImportMeta
    public static bool IsImportMeta(Node node)
    {
        if ((node.Kind == Kind.MetaProperty))
        {
            return ((node.AsMetaProperty().KeywordToken == Kind.ImportKeyword) && (node.AsMetaProperty().Name().Text() == "meta"));
        }

        return false;
    }

    // Upstream function: IsInJSFile
    public static bool IsInJSFile(Node node)
    {
        return ((node != null) && ((node.Flags & NodeFlags.JavaScriptFile) != 0));
    }

    // Upstream function: IsLiteralImportTypeNode
    public static bool IsLiteralImportTypeNode(Node node)
    {
        return ((IsImportTypeNode(node) && IsLiteralTypeNode(node.AsImportTypeNode().Argument)) && IsStringLiteral(node.AsImportTypeNode().Argument.AsLiteralTypeNode().Literal));
    }

    // Upstream function: IsExportsIdentifier
    public static bool IsExportsIdentifier(Node node)
    {
        return (IsIdentifier(node) && (node.Text() == "exports"));
    }

    // Upstream function: IsModuleIdentifier
    public static bool IsModuleIdentifier(Node node)
    {
        return (IsIdentifier(node) && (node.Text() == "module"));
    }

    // Upstream function: IsThisIdentifier
    public static bool IsThisIdentifier(Node node)
    {
        return (IsIdentifier(node) && (node.Text() == "this"));
    }

    // Upstream function: IsBindableStaticAccessExpression
    public static bool IsBindableStaticAccessExpression(Node node, bool excludeThisKeyword)
    {
        return ((IsPropertyAccessExpression(node) && (((!excludeThisKeyword && (node.Expression().Kind == Kind.ThisKeyword)) || (IsIdentifier(node.Name()) && IsBindableStaticNameExpression(node.Expression(), true))))) || IsBindableStaticElementAccessExpression(node, excludeThisKeyword));
    }

    // Upstream function: IsBindableStaticElementAccessExpression
    public static bool IsBindableStaticElementAccessExpression(Node node, bool excludeThisKeyword)
    {
        return (IsLiteralLikeElementAccess(node) && (((((!excludeThisKeyword && (node.Expression().Kind == Kind.ThisKeyword))) || IsEntityNameExpression(node.Expression())) || IsBindableStaticAccessExpression(node.Expression(), true))));
    }

    // Upstream function: IsLiteralLikeElementAccess
    public static bool IsLiteralLikeElementAccess(Node node)
    {
        return (IsElementAccessExpression(node) && IsStringOrNumericLiteralLike(node.AsElementAccessExpression().ArgumentExpression));
    }

    // Upstream function: IsBindableStaticNameExpression
    public static bool IsBindableStaticNameExpression(Node node, bool excludeThisKeyword)
    {
        return (IsEntityNameExpression(node) || IsBindableStaticAccessExpression(node, excludeThisKeyword));
    }

    // Upstream function: GetElementOrPropertyAccessName
    public static Node GetElementOrPropertyAccessName(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAccessExpression:
            {
                if (IsIdentifier(node.Name()))
                {
                    return node.Name();
                }

                return null;
            }

            case Kind.ElementAccessExpression:
            {
                {
                    var arg = SkipParentheses(node.AsElementAccessExpression().ArgumentExpression);
                    if (IsStringOrNumericLiteralLike(arg))
                    {
                        return arg;
                    }
                }

                return null;
            }
        }

        throw new InvalidOperationException("Unhandled case in GetElementOrPropertyAccessName");
    }

    // Upstream function: GetAssignmentDeclarationKind
    public static JSDeclarationKind GetAssignmentDeclarationKind(Node node)
    {
        switch (node.Kind)
        {
            case Kind.BinaryExpression:
            {
                var bin = node.AsBinaryExpression();
                if (((bin.OperatorToken.Kind == Kind.EqualsToken) && IsAccessExpression(bin.Left)))
                {
                    if (IsInJSFile(bin.Left))
                    {
                        if ((IsModuleExportsAccessExpression(bin.Left) && !IsExportsIdentifier(bin.Right)))
                        {
                            return JSDeclarationKind.ModuleExports;
                        }

                        if ((((IsModuleExportsAccessExpression(bin.Left.Expression()) || IsExportsIdentifier(bin.Left.Expression()))) && (GetElementOrPropertyAccessName(bin.Left) != null)))
                        {
                            return JSDeclarationKind.ExportsProperty;
                        }

                        if ((bin.Left.Expression().Kind == Kind.ThisKeyword))
                        {
                            return JSDeclarationKind.ThisProperty;
                        }
                    }

                    if (((((bin.Left.Kind == Kind.PropertyAccessExpression) && IsEntityNameExpressionEx(bin.Left.Expression(), IsInJSFile(bin.Left))) && IsIdentifier(bin.Left.Name())) || ((bin.Left.Kind == Kind.ElementAccessExpression) && IsEntityNameExpressionEx(bin.Left.Expression(), IsInJSFile(bin.Left)))))
                    {
                        return JSDeclarationKind.Property;
                    }
                }

                break;
            }

            case Kind.CallExpression:
            {
                if ((IsInJSFile(node) && IsBindableObjectDefinePropertyCall(node)))
                {
                    var entityName = node.Arguments()[0];
                    if ((IsExportsIdentifier(entityName) || IsModuleExportsAccessExpression(entityName)))
                    {
                        return JSDeclarationKind.ObjectDefinePropertyExports;
                    }

                    return JSDeclarationKind.ObjectDefinePropertyValue;
                }

                break;
            }
        }

        return JSDeclarationKind.None;
    }

    // Upstream function: IsBindableObjectDefinePropertyCall
    public static bool IsBindableObjectDefinePropertyCall(Node node)
    {
        {
            var args = node.Arguments();
            if ((GoRuntime.Len(args) == 3))
            {
                {
                    var expr = node.Expression();
                    if ((((((IsPropertyAccessExpression(expr) && IsIdentifier(expr.Expression())) && (expr.Expression().Text() == "Object")) && (expr.Name().Text() == "defineProperty")) && IsStringOrNumericLiteralLike(args[1])) && IsBindableStaticNameExpression(args[0], true)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream function: IsEntityNameExpression
    public static bool IsEntityNameExpression(Node node)
    {
        return IsEntityNameExpressionEx(node, false);
    }

    // Upstream function: IsEntityNameExpressionEx
    public static bool IsEntityNameExpressionEx(Node node, bool allowJS)
    {
        return ((IsIdentifier(node) || IsPropertyAccessEntityNameExpression(node, allowJS)) || (allowJS && (((node.Kind == Kind.ThisKeyword) || isElementAccessEntityNameExpression(node, allowJS)))));
    }

    // Upstream function: IsPropertyAccessEntityNameExpression
    public static bool IsPropertyAccessEntityNameExpression(Node node, bool allowJS)
    {
        return ((IsPropertyAccessExpression(node) && IsIdentifier(node.Name())) && IsEntityNameExpressionEx(node.Expression(), allowJS));
    }

    // Upstream function: isElementAccessEntityNameExpression
    public static bool isElementAccessEntityNameExpression(Node node, bool allowJS)
    {
        return ((IsElementAccessExpression(node) && IsStringOrNumericLiteralLike(node.AsElementAccessExpression().ArgumentExpression)) && IsEntityNameExpressionEx(node.Expression(), allowJS));
    }

    // Upstream function: HasSamePropertyAccessName
    public static bool HasSamePropertyAccessName(Node node1, Node node2)
    {
        if (((node1.Kind == Kind.Identifier) && (node2.Kind == Kind.Identifier)))
        {
            return (node1.Text() == node2.Text());
        }
        else
        {
            if (((node1.Kind == Kind.PropertyAccessExpression) && (node2.Kind == Kind.PropertyAccessExpression)))
            {
                return ((node1.AsPropertyAccessExpression().Name().Text() == node2.AsPropertyAccessExpression().Name().Text()) && HasSamePropertyAccessName(node1.Expression(), node2.Expression()));
            }
        }

        return false;
    }

    // Upstream function: IsAmbientModule
    public static bool IsAmbientModule(Node node)
    {
        return (IsModuleDeclaration(node) && (((node.AsModuleDeclaration().Name().Kind == Kind.StringLiteral) || IsGlobalScopeAugmentation(node))));
    }

    // Upstream function: IsExternalModule
    public static bool IsExternalModule(SourceFile file)
    {
        return (file.ExternalModuleIndicator != null);
    }

    // Upstream function: IsGlobalScopeAugmentation
    public static bool IsGlobalScopeAugmentation(Node node)
    {
        return (IsModuleDeclaration(node) && (node.AsModuleDeclaration().Keyword == Kind.GlobalKeyword));
    }

    // Upstream function: IsAnyImportOrReExport
    public static bool IsAnyImportOrReExport(Node node)
    {
        return (IsImportNode(node) || IsExportDeclaration(node));
    }

    // Upstream function: IsImportNode
    public static bool IsImportNode(Node node)
    {
        return (IsAnyImportSyntax(node) || NodeKindIs(node, Kind.JSImportDeclaration));
    }

    // Upstream function: IsAnyImportSyntax
    public static bool IsAnyImportSyntax(Node node)
    {
        return NodeKindIs(node, Kind.ImportDeclaration, Kind.ImportEqualsDeclaration);
    }

    // Upstream function: GetExternalModuleName
    public static Node GetExternalModuleName(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportDeclaration:
            {
                return node.ModuleSpecifier();
            }

            case Kind.ImportEqualsDeclaration:
            {
                if ((node.AsImportEqualsDeclaration().ModuleReference.Kind == Kind.ExternalModuleReference))
                {
                    return node.AsImportEqualsDeclaration().ModuleReference.Expression();
                }

                return null;
            }

            case Kind.ImportType:
            {
                return getImportTypeNodeLiteral(node);
            }

            case Kind.CallExpression:
            {
                return Core.FirstOrNil(node.Arguments());
            }

            case Kind.ModuleDeclaration:
            {
                if (IsStringLiteral(node.AsModuleDeclaration().Name()))
                {
                    return node.AsModuleDeclaration().Name();
                }

                return null;
            }
        }

        throw new InvalidOperationException("Unhandled case in getExternalModuleName");
    }

    // Upstream function: getImportTypeNodeLiteral
    public static Node getImportTypeNodeLiteral(Node node)
    {
        if (IsImportTypeNode(node))
        {
            var importTypeNode = node.AsImportTypeNode();
            if (IsLiteralTypeNode(importTypeNode.Argument))
            {
                var literalTypeNode = importTypeNode.Argument.AsLiteralTypeNode();
                if (IsStringLiteral(literalTypeNode.Literal))
                {
                    return literalTypeNode.Literal;
                }
            }
        }

        return null;
    }

    // Upstream function: IsImportCall
    public static bool IsImportCall(Node node)
    {
        if (!IsCallExpression(node))
        {
            return false;
        }

        var e = node.Expression();
        return ((e.Kind == Kind.ImportKeyword) || IsImportPhaseMetaProperty(e));
    }

    // Upstream function: IsImportPhaseMetaProperty
    public static bool IsImportPhaseMetaProperty(Node node)
    {
        return (IsImportDeferMetaProperty(node) || IsImportSourceMetaProperty(node));
    }

    // Upstream function: IsImportDeferMetaProperty
    public static bool IsImportDeferMetaProperty(Node node)
    {
        return isImportMetaProperty(node, "defer");
    }

    // Upstream function: IsImportSourceMetaProperty
    public static bool IsImportSourceMetaProperty(Node node)
    {
        return isImportMetaProperty(node, "source");
    }

    // Upstream function: isImportMetaProperty
    public static bool isImportMetaProperty(Node node, string name)
    {
        return ((IsMetaProperty(node) && (node.AsMetaProperty().KeywordToken == Kind.ImportKeyword)) && (node.AsMetaProperty().Name().Text() == name));
    }

    // Upstream function: IsQuestionToken
    public static bool IsQuestionToken(Node node)
    {
        return ((node != null) && (node.Kind == Kind.QuestionToken));
    }

    // Upstream function: GetNodeAtPosition
    public static Node GetNodeAtPosition(SourceFile file, int position, bool includeJSDoc)
    {
        var current = file.AsNode();
        while (true)
        {
            Node child = default;
            if (includeJSDoc)
            {
                foreach (var jsdoc in current.JSDoc(file) ?? [])
                {
                    if (nodeContainsPosition(jsdoc, position))
                    {
                        child = jsdoc;
                        break;
                    }
                }
            }

            if ((child == null))
            {
                current.ForEachChild((node) =>
                {
                    if (nodeContainsPosition(node, position))
                    {
                        child = node;
                        return true;
                    }

                    return false;
                });
            }

            if (((child == null) || IsMetaProperty(child)))
            {
                return current;
            }

            current = child;
        }
    }

    // Upstream function: nodeContainsPosition
    public static bool nodeContainsPosition(Node node, int position)
    {
        return (((node.Kind >= Kind.FirstNode) && (node.Pos() <= position)) && (((position < node.End()) || ((position == node.End()) && (node.Kind == Kind.EndOfFile)))));
    }

    // Upstream function: findImportOrRequire
    public static (int, int) findImportOrRequire(string text, int start)
    {
        int index = default;
        int size = default;
        index = Math.Max(start, 0);
        var n = GoRuntime.Len(text);
        while ((index < n))
        {
            var next = text.AsSpan(index).IndexOfAny('i', 'r');
            if ((next < 0))
            {
                break;
            }

            index += next;
            string expected = "";
            if ((text[index] == 'i'))
            {
                size = 6;
                expected = "import";
            }
            else
            {
                size = 7;
                expected = "require";
            }

            if ((((index + size) <= n) && (text.Substring(index, (index + size) - (index)) == expected)))
            {
                return (index, size);
            }

            index++;
        }

        return (-1, 0);
    }

    // Upstream function: ForEachDynamicImportOrRequireCall
    public static bool ForEachDynamicImportOrRequireCall(SourceFile file, bool includeTypeSpaceImports, bool requireStringLiteralLikeArgument, Func<Node, Node, bool> cb)
    {
        var isJavaScriptFile = IsInJSFile(file.AsNode());
        var(lastIndex, size) = findImportOrRequire(file.Text(), 0);
        while ((lastIndex >= 0))
        {
            var node = GetNodeAtPosition(file, lastIndex, (isJavaScriptFile && includeTypeSpaceImports));
            if ((isJavaScriptFile && IsRequireCall(node, requireStringLiteralLikeArgument)))
            {
                if (cb(node, node.Arguments()[0]))
                {
                    return true;
                }
            }
            else
            {
                if (((IsImportCall(node) && (GoRuntime.Len(node.Arguments()) > 0)) && ((!requireStringLiteralLikeArgument || IsStringLiteralLike(node.Arguments()[0])))))
                {
                    if (cb(node, node.Arguments()[0]))
                    {
                        return true;
                    }
                }
                else
                {
                    if ((includeTypeSpaceImports && IsLiteralImportTypeNode(node)))
                    {
                        if (cb(node, node.AsImportTypeNode().Argument.AsLiteralTypeNode().Literal))
                        {
                            return true;
                        }
                    }
                }
            }

            lastIndex += size;
            (lastIndex, size) = findImportOrRequire(file.Text(), lastIndex);
        }

        return false;
    }

    // Upstream function: IsRequireCall
    public static bool IsRequireCall(Node node, bool requireStringLiteralLikeArgument)
    {
        if (!IsCallExpression(node))
        {
            return false;
        }

        var call = node.AsCallExpression();
        if ((!IsIdentifier(call.Expression) || (call.Expression.Text() != "require")))
        {
            return false;
        }

        if ((GoRuntime.Len(call.Arguments.Nodes) != 1))
        {
            return false;
        }

        return (!requireStringLiteralLikeArgument || IsStringLiteralLike(call.Arguments.Nodes[0]));
    }

    // Upstream function: IsModuleExportsAccessExpression
    public static bool IsModuleExportsAccessExpression(Node node)
    {
        if ((IsAccessExpression(node) && IsModuleIdentifier(node.Expression())))
        {
            {
                var name = GetElementOrPropertyAccessName(node);
                if ((name != null))
                {
                    return (name.Text() == "exports");
                }
            }
        }

        return false;
    }

    // Upstream function: IsClassMemberModifier
    public static bool IsClassMemberModifier(Kind token)
    {
        return (((IsParameterPropertyModifier(token) || (token == Kind.StaticKeyword)) || (token == Kind.OverrideKeyword)) || (token == Kind.AccessorKeyword));
    }

    // Upstream function: IsParameterPropertyModifier
    public static bool IsParameterPropertyModifier(Kind kind)
    {
        return ((ModifierToFlag(kind) & ModifierFlags.ParameterPropertyModifier) != 0);
    }

    // Upstream function: IsJsxOpeningLikeElement
    public static bool IsJsxOpeningLikeElement(Node node)
    {
        return (IsJsxOpeningElement(node) || IsJsxSelfClosingElement(node));
    }

    // Upstream function: CompareNodePositions
    public static int CompareNodePositions(Node n1, Node n2)
    {
        return Core.CompareTextRanges(n1.Loc, n2.Loc);
    }

    // Upstream function: IsKeyword
    public static bool IsKeyword(Kind token)
    {
        return ((Kind.FirstKeyword <= token) && (token <= Kind.LastKeyword));
    }

    // Upstream function: TagNamesAreEquivalent
    public static bool TagNamesAreEquivalent(Node lhs, Node rhs)
    {
        if ((lhs.Kind != rhs.Kind))
        {
            return false;
        }

        switch (lhs.Kind)
        {
            case Kind.Identifier:
            {
                return (lhs.Text() == rhs.Text());
            }

            case Kind.ThisKeyword:
            {
                return true;
            }

            case Kind.JsxNamespacedName:
            {
                return ((lhs.AsJsxNamespacedName().Namespace.Text() == rhs.AsJsxNamespacedName().Namespace.Text()) && (lhs.AsJsxNamespacedName().Name().Text() == rhs.AsJsxNamespacedName().Name().Text()));
            }

            case Kind.PropertyAccessExpression:
            {
                return ((lhs.AsPropertyAccessExpression().Name().Text() == rhs.AsPropertyAccessExpression().Name().Text()) && TagNamesAreEquivalent(lhs.Expression(), rhs.Expression()));
            }
        }

        throw new InvalidOperationException("Unhandled case in TagNamesAreEquivalent");
    }

    // Upstream function: GetReparsedNodeForNode
    public static Node GetReparsedNodeForNode(Node node)
    {
        if ((((node != null) && ((node.Flags & NodeFlags.JSDoc) != 0)) && ((node.Flags & NodeFlags.Reparsed) == 0)))
        {
            {
                var file = GetSourceFileOfNode(node);
                if (((file != null) && (GoRuntime.Len(file.ReparsedClones) != 0)))
                {
                    var(pos, found) = GoSlices.BinarySearchFunc(file.ReparsedClones, node, CompareNodePositions);
                    if ((!found && (pos > 0)))
                    {
                        pos--;
                    }

                    var candidate = file.ReparsedClones[pos];
                    if (node.Loc.ContainedBy(candidate.Loc))
                    {
                        {
                            var reparsed = findCloneInNode(candidate, node);
                            if ((reparsed != null))
                            {
                                return reparsed;
                            }
                        }
                    }
                }
            }
        }

        return node;
    }

    // Upstream function: findCloneInNode
    public static Node findCloneInNode(Node node, Node original)
    {
        while (true)
        {
            if (((node.Kind == original.Kind) && (node.Loc == original.Loc)))
            {
                return node;
            }

            var foundContainingChild = node.ForEachChild((n) =>
            {
                if (original.Loc.ContainedBy(n.Loc))
                {
                    node = n;
                    return true;
                }

                return false;
            });
            if (!foundContainingChild)
            {
                return null;
            }
        }
    }
}

public enum OuterExpressionKinds : ushort
{
    Parentheses = 1 << 0,
    TypeAssertions = 1 << 1,
    NonNullAssertions = 1 << 2,
    PartiallyEmittedExpressions = 1 << 3,
    ExpressionsWithTypeArguments = 1 << 4,
    Satisfies = 1 << 5,
    ExcludeJSDocTypeAssertion = 1 << 6,
    Assignments = 1 << 7,
    Comma = 1 << 8,
    Assertions = TypeAssertions | NonNullAssertions | Satisfies,
    All = Parentheses | Assertions | PartiallyEmittedExpressions | ExpressionsWithTypeArguments,
    AllExceptAssertionsOrExpressionsWithTypeArguments = All & ~Assertions & ~ExpressionsWithTypeArguments,
    ExpressionTypePassthrough = Parentheses | Assignments | Comma,
}

public enum JSDeclarationKind : int
{
    None = 0,
    ModuleExports = 1,
    ExportsProperty = 2,
    ThisProperty = 3,
    Property = 4,
    ObjectDefinePropertyValue = 5,
    ObjectDefinePropertyExports = 6,
}

// BEGIN MANUAL BINDER ATOMIC/TABLE HELPERS
public static partial class Ast
{
    private static long nextNodeId, nextSymbolId;
    public static ulong GetNodeId(Node node)
    {
        long id = System.Threading.Volatile.Read(ref node.id);
        if (id == 0)
        {
            id = System.Threading.Interlocked.Increment(ref nextNodeId);
            long previous = System.Threading.Interlocked.CompareExchange(ref node.id, id, 0);
            if (previous != 0)
                id = previous;
        }

        return unchecked((ulong)id);
    }

    public static ulong GetSymbolId(Symbol symbol)
    {
        long id = System.Threading.Volatile.Read(ref symbol.id);
        if (id == 0)
        {
            id = System.Threading.Interlocked.Increment(ref nextSymbolId);
            long previous = System.Threading.Interlocked.CompareExchange(ref symbol.id, id, 0);
            if (previous != 0)
                id = previous;
        }

        return unchecked((ulong)id);
    }

    internal static SymbolTable GetSymbolTable(GoRef<SymbolTable> data)
    {
        if (data.Value is null)
            data.Value = new SymbolTable();
        return data.Value;
    }

    public static SymbolTable GetMembers(Symbol symbol) => symbol.Members ??= new SymbolTable();
    public static SymbolTable GetExports(Symbol symbol) => symbol.Exports ??= new SymbolTable();
    public static SymbolTable GetLocals(Node container) => container.LocalsContainerData().Locals ??= new SymbolTable();
}

public static partial class Ast
{
    public static bool IsMethodOrAccessor(Node node) => node.Kind is Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor;
    public static bool IsPrivateIdentifierClassElementDeclaration(Node node) => (IsPropertyDeclaration(node) || IsMethodOrAccessor(node)) && IsPrivateIdentifier(node.Name());
    public static bool IsAmbientModuleSymbolName(string s) => TryGetAmbientModuleNameFromSymbolName(s).Item2;
    public static (string, bool) TryGetAmbientModuleNameFromSymbolName(string s)
    {
        if (s.StartsWith("\"", StringComparison.Ordinal) && s.EndsWith("\"", StringComparison.Ordinal))
            return (s.Substring(1, s.Length - 2), true);
        string prefix = InternalSymbolNamePrefix + "\"";
        if (!s.StartsWith(prefix, StringComparison.Ordinal))
            return ("", false);
        string rest = s.Substring(prefix.Length);
        int markerIndex = rest.LastIndexOf("\"pattern@", StringComparison.Ordinal);
        if (markerIndex < 1)
            return ("", false);
        return (rest.Substring(0, markerIndex), true);
    }
} // END MANUAL BINDER ATOMIC/TABLE HELPERS

// BEGIN MANUAL CHECKER RESOLVER AST HELPERS (ast/utilities.go)
public static partial class Ast
{
    // Upstream utilities.go:498; excludes signature declarations.
    public static bool IsFunctionLikeDeclaration(Node node) => node is not null && isFunctionLikeDeclarationKind(node.Kind);
    // Upstream utilities.go:1334.
    public static bool IsModuleOrEnumDeclaration(Node node) => node.Kind == Kind.ModuleDeclaration || node.Kind == Kind.EnumDeclaration;
    // Upstream utilities.go:2545.
    public static bool IsConstAssertion(Node node)
    {
        switch (node.Kind)
        {
            case Kind.AsExpression:
            case Kind.TypeAssertionExpression:
                return IsConstTypeReference(node.Type());
        }

        return false;
    }

    // Upstream utilities.go:2553.
    public static bool IsConstTypeReference(Node node) => IsTypeReferenceNode(node) && GoRuntime.Len(node.TypeArguments()) == 0 && IsIdentifier(node.AsTypeReferenceNode().TypeName) && node.AsTypeReferenceNode().TypeName.Text() == "const";
    // Upstream utilities.go:2557.
    public static bool IsGlobalSourceFile(Node node) => node.Kind == Kind.SourceFile && !IsExternalOrCommonJSModule(node.AsSourceFile());
    // Upstream utilities.go:2569.
    public static Node GetDeclarationOfKind(Symbol symbol, Kind kind)
    {
        foreach (var declaration in GoRuntime.RangeValues(symbol.Declarations))
            if (declaration.Kind == kind)
                return declaration;
        return null;
    }

    // Upstream utilities.go:2578.
    public static Node FindConstructorDeclaration(Node node)
    {
        foreach (var member in GoRuntime.RangeValues(node.Members()))
            if (IsConstructorDeclaration(member) && NodeIsPresent(member.Body()))
                return member;
        return null;
    }
}

// END MANUAL CHECKER RESOLVER AST HELPERS
// BEGIN MANUAL CHECKER SYNTAX HELPERS
public static partial class Ast
{
    // Upstream utilities.go:319.
    public static bool IsLiteralExpression(Node node) => IsLiteralKind(node.Kind);
    // Upstream utilities.go:413.
    private static bool isUnaryExpressionKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.PrefixUnaryExpression:
            case Kind.PostfixUnaryExpression:
            case Kind.DeleteExpression:
            case Kind.TypeOfExpression:
            case Kind.VoidExpression:
            case Kind.AwaitExpression:
            case Kind.TypeAssertionExpression:
                return true;
        }

        return isLeftHandSideExpressionKind(kind);
    }

    // Upstream utilities.go:434.
    private static bool isExpressionKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.ConditionalExpression:
            case Kind.YieldExpression:
            case Kind.ArrowFunction:
            case Kind.BinaryExpression:
            case Kind.SpreadElement:
            case Kind.AsExpression:
            case Kind.OmittedExpression:
            case Kind.PartiallyEmittedExpression:
            case Kind.SatisfiesExpression:
                return true;
        }

        return isUnaryExpressionKind(kind);
    }

    // Upstream utilities.go:451.
    public static bool IsExpression(Node node) => isExpressionKind(SkipPartiallyEmittedExpressions(node).Kind);
    // Upstream utilities.go:527.
    public static bool IsFunctionOrSourceFile(Node node) => IsFunctionLike(node) || IsSourceFile(node);
    // Upstream utilities.go:1310.
    public static bool IsDeclarationName(Node name) => !IsSourceFile(name) && !IsBindingPattern(name) && IsDeclaration(name.Parent) && name.Parent.Name() == name;
    // Upstream utilities.go:2251.
    public static Node GetEnclosingBlockScopeContainer(Node node) => FindAncestor(node.Parent, current => IsBlockScope(current, current.Parent));
    // Upstream utilities.go:2257.
    public static bool IsBlockScope(Node node, Node parentNode)
    {
        switch (node.Kind)
        {
            case Kind.SourceFile:
            case Kind.CaseBlock:
            case Kind.CatchClause:
            case Kind.ModuleDeclaration:
            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.Constructor:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.PropertyDeclaration:
            case Kind.ClassStaticBlockDeclaration:
                return true;
            case Kind.Block:
                return !IsFunctionLikeOrClassStaticBlockDeclaration(parentNode);
        }

        return false;
    }

    // Upstream utilities.go:3658.
    public static bool IsTypeDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.TypeParameter:
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.EnumDeclaration:
                return true;
            case Kind.ImportClause:
                return node.IsTypeOnly() && node.AsImportClause().Name() != null;
            case Kind.ImportSpecifier:
            case Kind.ExportSpecifier:
                return node.Parent.Parent.IsTypeOnly();
            default:
                return false;
        }
    }
}

// Upstream utilities.go:523/941.
public static partial class Ast
{
    public static bool IsFunctionLikeOrClassStaticBlockDeclaration(Node node) => node != null && (IsFunctionLike(node) || IsClassStaticBlockDeclaration(node));
    public static Node FindAncestorKind(Node node, Kind kind)
    {
        while (node != null)
        {
            if (node.Kind == kind)
                return node;
            node = node.Parent;
        }

        return null;
    }
}

// Upstream utilities.go:1147.
public static partial class Ast
{
    public static bool IsFunctionOrModuleBlock(Node node) => IsSourceFile(node) || IsModuleBlock(node) || IsBlock(node) && IsFunctionLike(node.Parent);
}

// Upstream utilities.go:76.
public static partial class Ast
{
    // Upstream utilities.go:85.
    public static bool PositionIsSynthesized(int pos) => pos < 0;
    public static bool NodeIsSynthesized(Node node) => PositionIsSynthesized(node.Loc.Pos()) || PositionIsSynthesized(node.Loc.End());
}

// END MANUAL CHECKER SYNTAX HELPERS
// BEGIN GENERATED CHECKER QUERY AST CLOSURE
public static partial class Ast
{
    // Like 'isDeclarationName', but returns true for LHS of `import { x as y }` or `export { x as y }`.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1315:1.
    public static bool IsDeclarationNameOrImportPropertyName(Node name)
    {
        switch (name.Parent.Kind)
        {
            case Kind.ImportSpecifier:
            case Kind.ExportSpecifier:
            {
                return (IsIdentifier(name) || (name.Kind == Kind.StringLiteral));
            }

            default:
            {
                return IsDeclarationName(name);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1342:1.
    public static bool IsJsxTagName(Node node)
    {
        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.JsxOpeningElement:
            case Kind.JsxClosingElement:
            case Kind.JsxSelfClosingElement:
            {
                return (parent.TagName() == node);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1997:1.
    public static bool IsExpressionNode(Node node)
    {
        switch (node.Kind)
        {
            case Kind.SuperKeyword:
            case Kind.NullKeyword:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.RegularExpressionLiteral:
            case Kind.ArrayLiteralExpression:
            case Kind.ObjectLiteralExpression:
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.TaggedTemplateExpression:
            case Kind.AsExpression:
            case Kind.TypeAssertionExpression:
            case Kind.SatisfiesExpression:
            case Kind.NonNullExpression:
            case Kind.ParenthesizedExpression:
            case Kind.FunctionExpression:
            case Kind.ClassExpression:
            case Kind.ArrowFunction:
            case Kind.VoidExpression:
            case Kind.DeleteExpression:
            case Kind.TypeOfExpression:
            case Kind.PrefixUnaryExpression:
            case Kind.PostfixUnaryExpression:
            case Kind.BinaryExpression:
            case Kind.ConditionalExpression:
            case Kind.SpreadElement:
            case Kind.TemplateExpression:
            case Kind.OmittedExpression:
            case Kind.JsxElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxFragment:
            case Kind.YieldExpression:
            case Kind.AwaitExpression:
            {
                return true;
            }

            case Kind.MetaProperty:
            {
                return (!IsImportCall(node.Parent) || (node.Parent.Expression() != node));
            }

            case Kind.ExpressionWithTypeArguments:
            {
                return !IsHeritageClause(node.Parent);
            }

            case Kind.QualifiedName:
            {
                while ((node.Parent.Kind == Kind.QualifiedName))
                {
                    node = node.Parent;
                }

                return (((IsTypeQueryNode(node.Parent) || IsJSDocLinkLike(node.Parent)) || IsJSDocNameReference(node.Parent)) || IsJsxTagName(node));
            }

            case Kind.PrivateIdentifier:
            {
                return ((IsBinaryExpression(node.Parent) && (node.Parent.AsBinaryExpression().Left == node)) && (node.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.InKeyword));
            }

            case Kind.Identifier:
            {
                if ((((IsTypeQueryNode(node.Parent) || IsJSDocLinkLike(node.Parent)) || IsJSDocNameReference(node.Parent)) || IsJsxTagName(node)))
                {
                    return true;
                }

                goto case Kind.NumericLiteral;
            }

            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.ThisKeyword:
            {
                return IsInExpressionContext(node);
            }

            default:
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2032:1.
    public static bool IsInExpressionContext(Node node)
    {
        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.EnumMember:
            case Kind.PropertyAssignment:
            case Kind.BindingElement:
            {
                return (parent.Initializer() == node);
            }

            case Kind.ExpressionStatement:
            case Kind.IfStatement:
            case Kind.DoStatement:
            case Kind.WhileStatement:
            case Kind.ReturnStatement:
            case Kind.WithStatement:
            case Kind.SwitchStatement:
            case Kind.CaseClause:
            case Kind.DefaultClause:
            case Kind.ThrowStatement:
            case Kind.TypeAssertionExpression:
            case Kind.AsExpression:
            case Kind.TemplateSpan:
            case Kind.ComputedPropertyName:
            case Kind.SatisfiesExpression:
            {
                return (parent.Expression() == node);
            }

            case Kind.ForStatement:
            {
                var s = parent.AsForStatement();
                return ((((s.Initializer == node) && (s.Initializer.Kind != Kind.VariableDeclarationList)) || (s.Condition == node)) || (s.Incrementor == node));
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                var s2 = parent.AsForInOrOfStatement();
                return (((s2.Initializer == node) && (s2.Initializer.Kind != Kind.VariableDeclarationList)) || (s2.Expression == node));
            }

            case Kind.Decorator:
            case Kind.JsxExpression:
            case Kind.JsxSpreadAttribute:
            case Kind.SpreadAssignment:
            {
                return true;
            }

            case Kind.ExpressionWithTypeArguments:
            {
                return ((parent.Expression() == node) && !IsPartOfTypeNode(parent));
            }

            case Kind.ShorthandPropertyAssignment:
            {
                return (parent.AsShorthandPropertyAssignment().ObjectAssignmentInitializer == node);
            }

            case Kind.FunctionExpression:
            case Kind.ClassExpression:
            {
                return (parent.Name() != node);
            }

            default:
            {
                return IsExpressionNode(parent);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2061:1.
    public static bool IsPartOfTypeNode(Node node)
    {
        var kind = node.Kind;
        if (((kind >= Kind.FirstTypeNode) && (kind <= Kind.LastTypeNode)))
        {
            return true;
        }

        switch (node.Kind)
        {
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.StringKeyword:
            case Kind.BooleanKeyword:
            case Kind.SymbolKeyword:
            case Kind.ObjectKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NullKeyword:
            case Kind.NeverKeyword:
            {
                return true;
            }

            case Kind.VoidKeyword:
            {
                return (node.Parent.Kind != Kind.VoidExpression);
            }

            case Kind.ExpressionWithTypeArguments:
            {
                return isPartOfTypeExpressionWithTypeArguments(node);
            }

            case Kind.TypeParameter:
            {
                return ((node.Parent.Kind == Kind.MappedType) || (node.Parent.Kind == Kind.InferType));
            }

            case Kind.Identifier:
            {
                var parent = node.Parent;
                if ((IsQualifiedName(parent) && (parent.AsQualifiedName().Right == node)))
                {
                    return isPartOfTypeNodeInParent(parent);
                }

                if ((IsPropertyAccessExpression(parent) && (parent.AsPropertyAccessExpression().Name() == node)))
                {
                    return isPartOfTypeNodeInParent(parent);
                }

                return isPartOfTypeNodeInParent(node);
            }

            case Kind.QualifiedName:
            case Kind.PropertyAccessExpression:
            case Kind.ThisKeyword:
            {
                return isPartOfTypeNodeInParent(node);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2092:1.
    private static bool isPartOfTypeNodeInParent(Node node)
    {
        var parent = node.Parent;
        if ((parent.Kind == Kind.TypeQuery))
        {
            return false;
        }

        if ((parent.Kind == Kind.ImportType))
        {
            return !parent.AsImportTypeNode().IsTypeOf;
        }

        if (((parent.Kind >= Kind.FirstTypeNode) && (parent.Kind <= Kind.LastTypeNode)))
        {
            return true;
        }

        switch (parent.Kind)
        {
            case Kind.ExpressionWithTypeArguments:
            {
                return isPartOfTypeExpressionWithTypeArguments(parent);
            }

            case Kind.TypeParameter:
            {
                return (node == parent.AsTypeParameterDeclaration().Constraint);
            }

            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.Constructor:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.IndexSignature:
            case Kind.TypeAssertionExpression:
            {
                return (node == parent.Type());
            }

            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.TaggedTemplateExpression:
            {
                return (parent.TypeArguments()?.Contains(node) == true);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2126:1.
    private static bool isPartOfTypeExpressionWithTypeArguments(Node node)
    {
        var parent = node.Parent;
        return (((IsHeritageClause(parent) && ((!IsClassLike(parent.Parent) || (parent.AsHeritageClause().Token == Kind.ImplementsKeyword)))) || IsJSDocImplementsTag(parent)) || IsJSDocAugmentsTag(parent));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2133:1.
    public static bool IsJSDocLinkLike(Node node)
    {
        return NodeKindIs(node, Kind.JSDocLink, Kind.JSDocLinkCode, Kind.JSDocLinkPlain);
    }
}

// END GENERATED CHECKER QUERY AST CLOSURE
// BEGIN GENERATED BINDER UTILITY CLOSURE
// Port of tsc/internal/ast/utilities.go; additional source-file binder dependency closure.
public static partial class Ast
{
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:132:1.
    public static bool IsDestructuringAssignment(Node node)
    {
        if (IsAssignmentExpression(node, true))
        {
            var kind = node.AsBinaryExpression().Left.Kind;
            return ((kind == Kind.ObjectLiteralExpression) || (kind == Kind.ArrayLiteralExpression));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:166:1.
    public static bool IsBindingPattern(Node node)
    {
        return ((node.Kind == Kind.ObjectBindingPattern) || (node.Kind == Kind.ArrayBindingPattern));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:170:1.
    public static bool IsForInOrOfStatement(Node node)
    {
        return ((node != null) && (((node.Kind == Kind.ForInStatement) || (node.Kind == Kind.ForOfStatement))));
    }

    // A node is an assignment target if it is on the left hand side of an '=' token, if it is parented by a property
    // assignment in an object literal that is an assignment target, or if it is parented by an array literal that is
    // an assignment target. Examples include 'a = xxx', '{ p: a } = xxx', '[{ a }] = xxx'.
    // (Note that `p` is not a target in the above examples, only `a`.)
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:178:1.
    public static bool IsAssignmentTarget(Node node)
    {
        return (GetAssignmentTarget(node) != null);
    }

    // Returns the BinaryExpression, PrefixUnaryExpression, PostfixUnaryExpression, or ForInOrOfStatement that references
    // the given node as an assignment target
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:184:1.
    public static Node GetAssignmentTarget(Node node)
    {
        while (true)
        {
            var parent = node.Parent;
            switch (parent.Kind)
            {
                case Kind.BinaryExpression:
                {
                    if ((IsAssignmentOperator(parent.AsBinaryExpression().OperatorToken.Kind) && (parent.AsBinaryExpression().Left == node)))
                    {
                        return parent;
                    }

                    return null;
                }

                case Kind.PrefixUnaryExpression:
                {
                    if (((parent.AsPrefixUnaryExpression().Operator == Kind.PlusPlusToken) || (parent.AsPrefixUnaryExpression().Operator == Kind.MinusMinusToken)))
                    {
                        return parent;
                    }

                    return null;
                }

                case Kind.PostfixUnaryExpression:
                {
                    if (((parent.AsPostfixUnaryExpression().Operator == Kind.PlusPlusToken) || (parent.AsPostfixUnaryExpression().Operator == Kind.MinusMinusToken)))
                    {
                        return parent;
                    }

                    return null;
                }

                case Kind.ForInStatement:
                case Kind.ForOfStatement:
                {
                    if ((parent.Initializer() == node))
                    {
                        return parent;
                    }

                    return null;
                }

                case Kind.ParenthesizedExpression:
                case Kind.ArrayLiteralExpression:
                case Kind.SpreadElement:
                case Kind.NonNullExpression:
                {
                    node = parent;
                    break;
                }

                case Kind.SpreadAssignment:
                {
                    node = parent.Parent;
                    break;
                }

                case Kind.ShorthandPropertyAssignment:
                {
                    if ((parent.AsShorthandPropertyAssignment().Name() != node))
                    {
                        return null;
                    }

                    node = parent.Parent;
                    break;
                }

                case Kind.PropertyAssignment:
                {
                    if ((parent.AsPropertyAssignment().Name() == node))
                    {
                        return null;
                    }

                    node = parent.Parent;
                    break;
                }

                default:
                {
                    return null;
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:228:1.
    public static bool IsLogicalBinaryOperator(Kind token)
    {
        return ((token == Kind.BarBarToken) || (token == Kind.AmpersandAmpersandToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:232:1.
    public static bool IsLogicalOrCoalescingBinaryOperator(Kind token)
    {
        return (IsLogicalBinaryOperator(token) || (token == Kind.QuestionQuestionToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:236:1.
    public static bool IsLogicalOrCoalescingBinaryExpression(Node expr)
    {
        return (IsBinaryExpression(expr) && IsLogicalOrCoalescingBinaryOperator(expr.AsBinaryExpression().OperatorToken.Kind));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:240:1.
    public static bool IsLogicalOrCoalescingAssignmentExpression(Node expr)
    {
        return (IsBinaryExpression(expr) && IsLogicalOrCoalescingAssignmentOperator(expr.AsBinaryExpression().OperatorToken.Kind));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:244:1.
    public static bool IsLogicalExpression(Node node)
    {
        while (true)
        {
            if ((node.Kind == Kind.ParenthesizedExpression))
            {
                node = node.Expression();
            }
            else
            {
                if (((node.Kind == Kind.PrefixUnaryExpression) && (node.AsPrefixUnaryExpression().Operator == Kind.ExclamationToken)))
                {
                    node = node.AsPrefixUnaryExpression().Operand;
                }
                else
                {
                    return IsLogicalOrCoalescingBinaryExpression(node);
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:260:1.
    public static bool IsPropertyNameLiteral(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
            {
                return true;
            }
        }

        return false;
    }

    // Return true if the given identifier is classified as an IdentifierName by inspecting the parent of the node
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:292:1.
    public static bool IsIdentifierName(Node node)
    {
        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.EnumMember:
            case Kind.PropertyAssignment:
            case Kind.PropertyAccessExpression:
            {
                return (parent.Name() == node);
            }

            case Kind.QualifiedName:
            {
                return (parent.AsQualifiedName().Right == node);
            }

            case Kind.BindingElement:
            {
                return (parent.PropertyName() == node);
            }

            case Kind.ImportSpecifier:
            {
                return (parent.PropertyName() == node);
            }

            case Kind.ExportSpecifier:
            case Kind.JsxAttribute:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxOpeningElement:
            case Kind.JsxClosingElement:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:310:1.
    public static bool IsPushOrUnshiftIdentifier(Node node)
    {
        var text = node.Text();
        return ((text == "push") || (text == "unshift"));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:315:1.
    public static bool IsBooleanLiteral(Node node)
    {
        return ((node.Kind == Kind.TrueKeyword) || (node.Kind == Kind.FalseKeyword));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:335:1.
    public static bool IsSignedNumericLiteral(Node node)
    {
        if ((node.Kind == Kind.PrefixUnaryExpression))
        {
            var node2 = node.AsPrefixUnaryExpression();
            return ((((node2.Operator == Kind.PlusToken) || (node2.Operator == Kind.MinusToken))) && IsNumericLiteral(node2.Operand));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:357:1.
    private static Node getQuestionDotToken(Node node)
    {
        return node.QuestionDotToken();
    }

    // Determines if node is the root expression of an OptionalChain
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:362:1.
    public static bool IsOptionalChainRoot(Node node)
    {
        return ((IsOptionalChain(node) && !IsNonNullExpression(node)) && (getQuestionDotToken(node) != null));
    }

    // Determines whether a node is the outermost `OptionalChain` in an ECMAScript `OptionalExpression`:
    //
    //  1. For `a?.b.c`, the outermost chain is `a?.b.c` (`c` is the end of the chain starting at `a?.`)
    //  2. For `a?.b!`, the outermost chain is `a?.b` (`b` is the end of the chain starting at `a?.`)
    //  3. For `(a?.b.c).d`, the outermost chain is `a?.b.c` (`c` is the end of the chain starting at `a?.` since parens end the chain)
    //  4. For `a?.b.c?.d`, both `a?.b.c` and `a?.b.c?.d` are outermost (`c` is the end of the chain starting at `a?.`, and `d` is
    //     the end of the chain starting at `c?.`)
    //  5. For `a?.(b?.c).d`, both `b?.c` and `a?.(b?.c)d` are outermost (`c` is the end of the chain starting at `b`, and `d` is
    //     the end of the chain starting at `a?.`)
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:375:1.
    public static bool IsOutermostOptionalChain(Node node)
    {
        var parent = node.Parent;
        return ((!IsOptionalChain(parent) || IsOptionalChainRoot(parent)) || (node != parent.Expression()));
    }

    // Determines whether a node is the expression preceding an optional chain (i.e. `a` in `a?.b`).
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:383:1.
    public static bool IsExpressionOfOptionalChainRoot(Node node)
    {
        return (IsOptionalChainRoot(node.Parent) && (node.Parent.Expression() == node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:387:1.
    public static bool IsNullishCoalesce(Node node)
    {
        return ((node.Kind == Kind.BinaryExpression) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.QuestionQuestionToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:531:1.
    public static bool IsClassLike(Node node)
    {
        return ((node.Kind == Kind.ClassDeclaration) || (node.Kind == Kind.ClassExpression));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:539:1.
    public static bool IsClassElement(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Constructor:
            case Kind.PropertyDeclaration:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.IndexSignature:
            case Kind.ClassStaticBlockDeclaration:
            case Kind.SemicolonClassElement:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:566:1.
    public static bool IsObjectLiteralOrClassExpressionMethodOrAccessor(Node node)
    {
        var kind = node.Kind;
        return (((((kind == Kind.MethodDeclaration) || (kind == Kind.GetAccessor)) || (kind == Kind.SetAccessor))) && (((node.Parent.Kind == Kind.ObjectLiteralExpression) || (node.Parent.Kind == Kind.ClassExpression))));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:600:1.
    public static bool IsObjectLiteralMethod(Node node)
    {
        return (((node != null) && (node.Kind == Kind.MethodDeclaration)) && (node.Parent.Kind == Kind.ObjectLiteralExpression));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:604:1.
    public static bool IsAutoAccessorPropertyDeclaration(Node node)
    {
        return (IsPropertyDeclaration(node) && HasAccessorModifier(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:608:1.
    public static bool IsParameterPropertyDeclaration(Node node, Node parent)
    {
        return ((IsParameterDeclaration(node) && HasSyntacticModifier(node, ModifierFlags.ParameterPropertyModifier)) && (parent.Kind == Kind.Constructor));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:628:1.
    private static bool isDeclarationStatementKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.MissingDeclaration:
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.EnumDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ExportAssignment:
            case Kind.NamespaceExportDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Determines whether a node is a DeclarationStatement. Ideally this does not use Parent pointers, but it may use them
    // to rule out a Block node that is part of `try` or `catch` or is the Block-like body of a function.
    //
    // NOTE: ECMA262 would just call this a Declaration
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:653:1.
    public static bool IsDeclarationStatement(Node node)
    {
        return isDeclarationStatementKind(node.Kind);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:717:1.
    public static bool IsBlockOrCatchScoped(Node declaration)
    {
        return (((GetCombinedNodeFlags(declaration) & NodeFlags.BlockScoped) != 0) || IsCatchClauseVariableDeclarationOrBindingElement(declaration));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:721:1.
    public static bool IsCatchClauseVariableDeclarationOrBindingElement(Node declaration)
    {
        var node = GetRootDeclaration(declaration);
        return ((node.Kind == Kind.VariableDeclaration) && (node.Parent.Kind == Kind.CatchClause));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:767:1.
    public static bool IsPrologueDirective(Node node)
    {
        return ((node.Kind == Kind.ExpressionStatement) && (node.Expression().Kind == Kind.StringLiteral));
    }

    // Walks up the parents of a node to find the ancestor that matches the callback
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:911:1.
    public static Node FindAncestor(Node node, Func<Node, bool> callback)
    {
        while ((node != null))
        {
            if (callback(node))
            {
                return node;
            }

            node = node.Parent;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1040:1.
    public static bool HasAccessorModifier(Node node)
    {
        return HasSyntacticModifier(node, ModifierFlags.Accessor);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1044:1.
    public static bool HasStaticModifier(Node node)
    {
        return HasSyntacticModifier(node, ModifierFlags.Static);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1048:1.
    public static bool IsStatic(Node node)
    {
        return ((IsClassElement(node) && HasStaticModifier(node)) || IsClassStaticBlockDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1151:1.
    public static bool IsFunctionExpressionOrArrowFunction(Node node)
    {
        return (IsFunctionExpression(node) || IsArrowFunction(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1173:1.
    public static Node GetRootDeclaration(Node node)
    {
        while ((node.Kind == Kind.BindingElement))
        {
            node = node.Parent.Parent;
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1180:1.
    public static ModifierFlags GetCombinedModifierFlags(Node node)
    {
        node = GetRootDeclaration(node);
        var flags = node.ModifierFlags();
        if ((node.Kind == Kind.VariableDeclaration))
        {
            node = node.Parent;
        }

        if (((node != null) && (node.Kind == Kind.VariableDeclarationList)))
        {
            flags |= node.ModifierFlags();
            node = node.Parent;
        }

        if (((node != null) && (node.Kind == Kind.VariableStatement)))
        {
            flags |= node.ModifierFlags();
        }

        return flags;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1196:1.
    public static NodeFlags GetCombinedNodeFlags(Node node)
    {
        node = GetRootDeclaration(node);
        var flags = node.Flags;
        if ((node.Kind == Kind.VariableDeclaration))
        {
            node = node.Parent;
        }

        if (((node != null) && (node.Kind == Kind.VariableDeclarationList)))
        {
            flags |= node.Flags;
            node = node.Parent;
        }

        if (((node != null) && (node.Kind == Kind.VariableStatement)))
        {
            flags |= node.Flags;
        }

        return flags;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1302:1.
    public static bool IsDeclaration(Node node)
    {
        if ((node.Kind == Kind.TypeParameter))
        {
            return (node.Parent != null);
        }

        return IsDeclarationNode(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1453:1.
    public static Node GetNameOfDeclaration(Node declaration)
    {
        if ((declaration == null))
        {
            return null;
        }

        var nonAssignedName = GetNonAssignedNameOfDeclaration(declaration);
        if ((nonAssignedName != null))
        {
            return nonAssignedName;
        }

        if (((IsFunctionExpression(declaration) || IsArrowFunction(declaration)) || IsClassExpression(declaration)))
        {
            return GetAssignedName(declaration);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1467:1.
    public static Node GetNonAssignedNameOfDeclaration(Node declaration)
    {
        switch (declaration.Kind)
        {
            case Kind.BinaryExpression:
            case Kind.CallExpression:
            {
                switch (GetAssignmentDeclarationKind(declaration))
                {
                    case JSDeclarationKind.Property:
                    case JSDeclarationKind.ThisProperty:
                    case JSDeclarationKind.ExportsProperty:
                    {
                        var left = declaration.AsBinaryExpression().Left;
                        {
                            var name = GetElementOrPropertyAccessName(left);
                            if ((name != null))
                            {
                                return name;
                            }
                        }

                        return left;
                    }

                    case JSDeclarationKind.ObjectDefinePropertyValue:
                    case JSDeclarationKind.ObjectDefinePropertyExports:
                    {
                        return declaration.Arguments()[1];
                    }
                }

                return null;
            }

            case Kind.ExportAssignment:
            {
                var expr = declaration.Expression();
                if (IsIdentifier(expr))
                {
                    return expr;
                }

                return null;
            }
        }

        return declaration.Name();
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1492:1.
    public static Node GetAssignedName(Node node)
    {
        var parent = node.Parent;
        if ((parent != null))
        {
            switch (parent.Kind)
            {
                case Kind.PropertyAssignment:
                {
                    return parent.AsPropertyAssignment().Name();
                }

                case Kind.BindingElement:
                {
                    return parent.AsBindingElement().Name();
                }

                case Kind.BinaryExpression:
                {
                    if ((node == parent.AsBinaryExpression().Right))
                    {
                        var left = parent.AsBinaryExpression().Left;
                        switch (left.Kind)
                        {
                            case Kind.Identifier:
                            {
                                return left;
                            }

                            case Kind.PropertyAccessExpression:
                            {
                                return left.AsPropertyAccessExpression().Name();
                            }

                            case Kind.ElementAccessExpression:
                            {
                                var arg = SkipParentheses(left.AsElementAccessExpression().ArgumentExpression);
                                if (IsStringOrNumericLiteralLike(arg))
                                {
                                    return arg;
                                }

                                break;
                            }
                        }
                    }

                    break;
                }

                case Kind.VariableDeclaration:
                {
                    var name = parent.AsVariableDeclaration().Name();
                    if (IsIdentifier(name))
                    {
                        return name;
                    }

                    break;
                }
            }
        }

        return null;
    }

    // *
    // * A declaration has a dynamic name if all of the following are true:
    // *   1. The declaration has a computed property name.
    // *   2. The computed name is *not* expressed as a StringLiteral.
    // *   3. The computed name is *not* expressed as a NumericLiteral.
    // *   4. The computed name is *not* expressed as a PlusToken or MinusToken
    // *      immediately followed by a NumericLiteral.
    // 
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1602:1.
    public static bool HasDynamicName(Node declaration)
    {
        var name = GetNameOfDeclaration(declaration);
        return ((name != null) && IsDynamicName(name));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1607:1.
    public static bool IsDynamicName(Node name)
    {
        Node expr = default;
        switch (name.Kind)
        {
            case Kind.ComputedPropertyName:
            {
                expr = name.Expression();
                break;
            }

            case Kind.ElementAccessExpression:
            {
                expr = SkipParentheses(name.AsElementAccessExpression().ArgumentExpression);
                break;
            }

            default:
            {
                return false;
            }
        }

        return (!IsStringOrNumericLiteralLike(expr) && !IsSignedNumericLiteral(expr));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1638:1.
    public static bool IsDottedName(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.MetaProperty:
            {
                return true;
            }

            case Kind.PropertyAccessExpression:
            case Kind.ParenthesizedExpression:
            {
                return IsDottedName(node.Expression());
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1690:1.
    public static bool IsExternalOrCommonJSModule(SourceFile file)
    {
        return ((file.ExternalModuleIndicator != null) || (file.CommonJSModuleIndicator != null));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1720:1.
    public static bool IsModuleAugmentationExternal(Node node)
    {
        switch (node.Parent.Kind)
        {
            case Kind.SourceFile:
            {
                return IsExternalModule(node.Parent.AsSourceFile());
            }

            case Kind.ModuleBlock:
            {
                var grandParent = node.Parent.Parent;
                return ((IsAmbientModule(grandParent) && IsSourceFile(grandParent.Parent)) && !IsExternalModule(grandParent.Parent.AsSourceFile()));
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1738:1.
    public static Node GetContainingClass(Node node)
    {
        return FindAncestor(node.Parent, IsClassLike);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1799:1.
    public static bool IsPartOfTypeQuery(Node node)
    {
        while (((node.Kind == Kind.QualifiedName) || (node.Kind == Kind.Identifier)))
        {
            node = node.Parent;
        }

        return (node.Kind == Kind.TypeQuery);
    }

    // *
    // * This function returns true if the this node's root declaration is a parameter.
    // * For example, passing a `ParameterDeclaration` will return true, as will passing a
    // * binding element that is a child of a `ParameterDeclaration`.
    // *
    // * If you are looking to test that a `Node` is a `ParameterDeclaration`, use `isParameter`.
    // 
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1813:1.
    public static bool IsPartOfParameterDeclaration(Node node)
    {
        return (GetRootDeclaration(node).Kind == Kind.Parameter);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1817:1.
    public static bool IsInTopLevelContext(Node node)
    {
        if (IsIdentifier(node))
        {
            var parent = node.Parent;
            if ((((IsClassDeclaration(parent) || IsFunctionDeclaration(parent))) && (parent.Name() == node)))
            {
                node = parent;
            }
        }

        var container = GetThisContainer(node, true, false);
        return IsSourceFile(container);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1829:1.
    public static Node GetThisContainer(Node node, bool includeArrowFunctions, bool includeClassComputedPropertyName)
    {
        while (true)
        {
            node = node.Parent;
            if ((node == null))
            {
                throw new InvalidOperationException("nil parent in getThisContainer");
            }

            switch (node.Kind)
            {
                case Kind.ComputedPropertyName:
                {
                    if ((includeClassComputedPropertyName && IsClassLike(node.Parent.Parent)))
                    {
                        return node;
                    }

                    node = node.Parent.Parent;
                    break;
                }

                case Kind.Decorator:
                {
                    if (((node.Parent.Kind == Kind.Parameter) && IsClassElement(node.Parent.Parent)))
                    {
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        if (IsClassElement(node.Parent))
                        {
                            node = node.Parent;
                        }
                    }

                    break;
                }

                case Kind.ArrowFunction:
                {
                    if (includeArrowFunctions)
                    {
                        return node;
                    }

                    break;
                }

                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                case Kind.ModuleDeclaration:
                case Kind.ClassStaticBlockDeclaration:
                case Kind.PropertyDeclaration:
                case Kind.PropertySignature:
                case Kind.MethodDeclaration:
                case Kind.MethodSignature:
                case Kind.Constructor:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.CallSignature:
                case Kind.ConstructSignature:
                case Kind.IndexSignature:
                case Kind.EnumDeclaration:
                case Kind.SourceFile:
                {
                    return node;
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1892:1.
    public static Node GetImmediatelyInvokedFunctionExpression(Node fn)
    {
        if (IsFunctionExpressionOrArrowFunction(fn))
        {
            var prev = fn;
            var parent = fn.Parent;
            while (IsParenthesizedExpression(parent))
            {
                prev = parent;
                parent = parent.Parent;
            }

            if ((IsCallExpression(parent) && (parent.Expression() == prev)))
            {
                return parent;
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1907:1.
    public static bool IsEnumConst(Node node)
    {
        return ((GetCombinedModifierFlags(node) & ModifierFlags.Const) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1911:1.
    public static bool ExpressionIsAlias(Node node)
    {
        return (IsEntityNameExpression(node) || IsClassExpression(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1934:1.
    public static bool IsJsonSourceFile(SourceFile file)
    {
        return (file.ScriptKind == ScriptKind.JSON);
    }

    // Push a virtual parent pointer onto `ancestors` and return it.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2379:1.
    private static List<Node> pushAncestor(List<Node> ancestors, Node parent)
    {
        return GoRuntime.Append(ancestors, parent);
    }

    // If a virtual `Parent` exists on the stack, returns the previous stack entry and the virtual `Parent“.
    // Otherwise, we return `nil` and the value of `node.Parent`.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2385:1.
    private static (List<Node>, Node) popAncestor(List<Node> ancestors, Node node)
    {
        if ((GoRuntime.Len(ancestors) == 0))
        {
            return (null, node.Parent);
        }

        var n = (GoRuntime.Len(ancestors) - 1);
        return (GoRuntime.Slice(ancestors, 0, n), ancestors[n]);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2402:1.
    public static ModuleInstanceState GetModuleInstanceState(Node node)
    {
        return getModuleInstanceState(node, null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2406:1.
    private static ModuleInstanceState getModuleInstanceState(Node node, List<Node> ancestors, Dictionary<ulong, ModuleInstanceState> visited)
    {
        var module = node.AsModuleDeclaration();
        if ((module.Body != null))
        {
            return getModuleInstanceStateCached(module.Body, pushAncestor(ancestors, node), visited);
        }
        else
        {
            return ModuleInstanceState.Instantiated;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2415:1.
    private static ModuleInstanceState getModuleInstanceStateCached(Node node, List<Node> ancestors, Dictionary<ulong, ModuleInstanceState> visited)
    {
        if ((visited == null))
        {
            visited = new Dictionary<ulong, ModuleInstanceState>();
        }

        var nodeId = GetNodeId(node);
        {
            var ok = visited.TryGetValue(nodeId, out var cached);
            if (ok)
            {
                if ((cached != ModuleInstanceState.Unknown))
                {
                    return cached;
                }

                return ModuleInstanceState.NonInstantiated;
            }
        }

        visited[nodeId] = ModuleInstanceState.Unknown;
        var result = getModuleInstanceStateWorker(node, ancestors, visited);
        visited[nodeId] = result;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2432:1.
    private static ModuleInstanceState getModuleInstanceStateWorker(Node node, List<Node> ancestors, Dictionary<ulong, ModuleInstanceState> visited)
    {
        switch (node.Kind)
        {
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            {
                return ModuleInstanceState.NonInstantiated;
            }

            case Kind.EnumDeclaration:
            {
                if (IsEnumConst(node))
                {
                    return ModuleInstanceState.ConstEnumOnly;
                }

                break;
            }

            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ImportEqualsDeclaration:
            {
                if (!HasSyntacticModifier(node, ModifierFlags.Export))
                {
                    return ModuleInstanceState.NonInstantiated;
                }

                break;
            }

            case Kind.ExportDeclaration:
            {
                var decl = node.AsExportDeclaration();
                if ((((decl.ModuleSpecifier == null) && (decl.ExportClause != null)) && (decl.ExportClause.Kind == Kind.NamedExports)))
                {
                    var state = ModuleInstanceState.NonInstantiated;
                    ancestors = pushAncestor(ancestors, node);
                    ancestors = pushAncestor(ancestors, decl.ExportClause);
                    {
                        foreach (var specifier in GoRuntime.RangeValues(decl.ExportClause.Elements()))
                        {
                            var specifierState = getModuleInstanceStateForAliasTarget(specifier, ancestors, visited);
                            if ((specifierState > state))
                            {
                                state = specifierState;
                            }

                            if ((state == ModuleInstanceState.Instantiated))
                            {
                                return state;
                            }
                        }
                    }

                    return state;
                }

                break;
            }

            case Kind.ModuleBlock:
            {
                var state2 = ModuleInstanceState.NonInstantiated;
                ancestors = pushAncestor(ancestors, node);
                node.ForEachChild((n) =>
                {
                    var childState = getModuleInstanceStateCached(n, ancestors, visited);
                    switch (childState)
                    {
                        case ModuleInstanceState.NonInstantiated:
                        {
                            return false;
                        }

                        case ModuleInstanceState.ConstEnumOnly:
                        {
                            state2 = ModuleInstanceState.ConstEnumOnly;
                            return false;
                        }

                        case ModuleInstanceState.Instantiated:
                        {
                            state2 = ModuleInstanceState.Instantiated;
                            return true;
                        }
                    }

                    throw new InvalidOperationException("Unhandled case in getModuleInstanceStateWorker");
                });
                return state2;
            }

            case Kind.ModuleDeclaration:
            {
                return getModuleInstanceState(node, ancestors, visited);
            }
        }

        return ModuleInstanceState.Instantiated;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2486:1.
    private static ModuleInstanceState getModuleInstanceStateForAliasTarget(Node node, List<Node> ancestors, Dictionary<ulong, ModuleInstanceState> visited)
    {
        var name = node.PropertyNameOrName();
        if ((name.Kind != Kind.Identifier))
        {
            return ModuleInstanceState.Instantiated;
        }

        {
            var tuple2 = popAncestor(ancestors, node);
            var ancestors2 = tuple2.Item1;
            var p = tuple2.Item2;
            for (; (p != null); (ancestors2, p) = popAncestor(ancestors2, p))
            {
                if (((IsBlock(p) || IsModuleBlock(p)) || IsSourceFile(p)))
                {
                    var found = ModuleInstanceState.Unknown;
                    var statementsAncestors = pushAncestor(ancestors2, p);
                    {
                        foreach (var statement in GoRuntime.RangeValues(p.Statements()))
                        {
                            if (NodeHasName(statement, name))
                            {
                                var state = getModuleInstanceStateCached(statement, statementsAncestors, visited);
                                if (((found == ModuleInstanceState.Unknown) || (state > found)))
                                {
                                    found = state;
                                }

                                if ((found == ModuleInstanceState.Instantiated))
                                {
                                    return found;
                                }

                                if ((statement.Kind == Kind.ImportEqualsDeclaration))
                                {
                                    found = ModuleInstanceState.Instantiated;
                                }
                            }
                        }
                    }

                    if ((found != ModuleInstanceState.Unknown))
                    {
                        return found;
                    }
                }
            }
        }

        return ModuleInstanceState.Instantiated;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2529:1.
    public static bool NodeHasName(Node statement, Node id)
    {
        var name = statement.Name();
        if ((name != null))
        {
            return (IsIdentifier(name) && (name.Text() == id.Text()));
        }

        if (IsVariableStatement(statement))
        {
            var declarations = statement.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
            return Core.Some(declarations, (d) =>
            {
                return NodeHasName(d, id);
            });
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2619:1.
    public static bool ModuleExportNameIsDefault(Node node)
    {
        return (node.Text() == InternalSymbolNameDefault);
    }

    // Of the form: `const x = require("x")` or `const { x } = require("x")` or with `var` or `let`
    // The variable must not be exported and must not have a type annotation, even a jsdoc one.
    // The initializer must be a call to `require` with a string literal or a string literal-like argument.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2905:1.
    public static bool IsVariableDeclarationInitializedToRequire(Node node)
    {
        if ((node.Kind == Kind.BindingElement))
        {
            node = node.Parent.Parent;
        }

        return isVariableDeclarationInitializedWithRequireHelper(node, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2916:1.
    private static bool isVariableDeclarationInitializedWithRequireHelper(Node node, bool allowAccessedRequire)
    {
        if (!IsInJSFile(node))
        {
            return false;
        }

        if ((node.Kind != Kind.VariableDeclaration))
        {
            return false;
        }

        var initializer = node.Initializer();
        if ((initializer == null))
        {
            return false;
        }

        if (allowAccessedRequire)
        {
            initializer = GetLeftmostAccessExpression(initializer);
        }

        return ((((node.Parent.Parent.ModifierFlags() & ModifierFlags.Export) == 0) && (node.Type() == null)) && IsRequireCall(initializer, true));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2973:1.
    public static Node GetLeftmostAccessExpression(Node expr)
    {
        while (IsAccessExpression(expr))
        {
            expr = expr.Expression();
        }

        return expr;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4209:1.
    public static bool IsExpandoInitializer(Node declaration, Node initializer)
    {
        if ((initializer == null))
        {
            return false;
        }

        if (IsFunctionExpressionOrArrowFunction(initializer))
        {
            return true;
        }

        if (IsInJSFile(initializer))
        {
            return (IsClassExpression(initializer) || (((IsObjectLiteralExpression(initializer) && (GoRuntime.Len(initializer.Properties()) == 0)) && (declaration.Type() == null))));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4257:1.
    public static bool IsImplicitlyExportedJSDocDeclaration(Node node)
    {
        if ((!IsSourceFile(node.Parent) || !IsExternalOrCommonJSModule(node.Parent.AsSourceFile())))
        {
            return false;
        }

        if (IsJSTypeAliasDeclaration(node))
        {
            return true;
        }

        return (IsModuleDeclaration(node) && ((node.Flags & NodeFlags.Reparsed) != 0));
    }

    // Returns true for nodes that are considered executable for the purposes of unreachable code detection.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4302:1.
    public static bool IsPotentiallyExecutableNode(Node node)
    {
        if (((Kind.FirstStatement <= node.Kind) && (node.Kind <= Kind.LastStatement)))
        {
            if (IsVariableStatement(node))
            {
                var declarationList = node.AsVariableStatement().DeclarationList;
                if (((GetCombinedNodeFlags(declarationList) & NodeFlags.BlockScoped) != 0))
                {
                    return true;
                }

                var declarations = declarationList.AsVariableDeclarationList().Declarations.Nodes;
                return Core.Some(declarations, (d) =>
                {
                    return (d.Initializer() != null);
                });
            }

            return true;
        }

        return ((IsClassDeclaration(node) || IsEnumDeclaration(node)) || IsModuleDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4507:1.
    public static bool IsAsyncFunction(Node node)
    {
        switch (node.Kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
            {
                var data = node.BodyData();
                return (((data.Body != null) && (data.AsteriskToken == null)) && HasSyntacticModifier(node, ModifierFlags.Async));
            }
        }

        return false;
    }
}

public enum ModuleInstanceState : int
{
    Unknown = 0,
    NonInstantiated = 1,
    Instantiated = 2,
    ConstEnumOnly = 3
} // END GENERATED BINDER UTILITY CLOSURE

// ast/utilities.go:1223/1241/1246. Lazy documentation lookup is real; tagged
// documents remain rejected by semantic admission, rather than assumed absent.
public static partial class Ast
{
    public static Node GetJSDocDeprecatedTag(Node node)
    {
        foreach (var jsdoc in node.JSDoc(null) ?? [])
            if (jsdoc.AsJSDoc().Tags is { } tags)
                foreach (var tag in tags.Nodes ?? [])
                    if (IsJSDocDeprecatedTag(tag))
                        return tag;
        return null;
    }

    public static bool IsDeprecatedDeclaration(Node declaration) => IsDeprecatedDeclarationWithCachedFlags(declaration, GetCombinedNodeFlags(declaration));
    public static bool IsDeprecatedDeclarationWithCachedFlags(Node declaration, NodeFlags combinedFlags)
    {
        if ((combinedFlags & NodeFlags.PossiblyContainsDeprecatedTag) == 0)
            return false;
        for (var n = declaration; n != null; n = n.Parent)
            if ((n.Flags & NodeFlags.PossiblyContainsDeprecatedTag) != 0)
                return GetJSDocDeprecatedTag(n) != null;
        return false;
    }
}

public static partial class Ast
{
    // utilities.go:2189
    public static string EntityNameToString(Node name, Func<Node, string> getTextOfNode)
    {
        switch (name.Kind)
        {
            case Kind.ThisKeyword:
                return "this";
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
                return NodeIsSynthesized(name) || getTextOfNode == null ? name.Text() : getTextOfNode(name);
            case Kind.QualifiedName:
                return EntityNameToString(name.AsQualifiedName().Left, getTextOfNode) + "." + EntityNameToString(name.AsQualifiedName().Right, getTextOfNode);
            case Kind.PropertyAccessExpression:
                return EntityNameToString(name.Expression(), getTextOfNode) + "." + EntityNameToString(name.Name(), getTextOfNode);
            case Kind.JsxNamespacedName:
                return EntityNameToString(name.AsJsxNamespacedName().Namespace, getTextOfNode) + ":" + EntityNameToString(name.Name(), getTextOfNode);
            default:
                throw new InvalidOperationException("Unhandled case in EntityNameToString");
        }
    }

    // utilities.go:3236
    public static string GetPropertyNameForPropertyNameNode(Node name)
    {
        switch (name.Kind)
        {
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.JsxNamespacedName:
                return name.Text();
            case Kind.ComputedPropertyName:
                var expression = name.Expression();
                if (IsStringOrNumericLiteralLike(expression))
                    return expression.Text();
                if (IsSignedNumericLiteral(expression))
                {
                    var prefix = expression.AsPrefixUnaryExpression();
                    var text = prefix.Operand.Text();
                    if (prefix.Operator == Kind.MinusToken)
                        text = "-" + text;
                    return text;
                }

                return InternalSymbolNameMissing;
            default:
                throw new InvalidOperationException("Unhandled case in getPropertyNameForPropertyNameNode");
        }
    }
}

public static partial class Ast
{
    // utilities.go:279/391/587.
    public static bool IsPropertyName(Node node) => node.Kind is Kind.Identifier or Kind.PrivateIdentifier or Kind.StringLiteral or Kind.NumericLiteral or Kind.ComputedPropertyName;
    public static bool IsAssertionExpression(Node node) => node.Kind is Kind.TypeAssertionExpression or Kind.AsExpression;
    public static bool IsObjectLiteralElement(Node node) => node.Kind is Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.SpreadAssignment or Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor;
    // utilities.go:4150/4173.
    public static bool IsPrimitiveLiteralValue(Node node, bool includeBigInt)
    {
        switch (node.Kind)
        {
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NumericLiteral:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
                return true;
            case Kind.BigIntLiteral:
                return includeBigInt;
            case Kind.PrefixUnaryExpression:
                var unary = node.AsPrefixUnaryExpression();
                if (unary.Operator == Kind.MinusToken)
                    return IsNumericLiteral(unary.Operand) || includeBigInt && IsBigIntLiteral(unary.Operand);
                if (unary.Operator == Kind.PlusToken)
                    return IsNumericLiteral(unary.Operand);
                return false;
            default:
                return false;
        }
    }

    public static bool HasInferredType(Node node) => node.Kind is Kind.Parameter or Kind.PropertySignature or Kind.PropertyDeclaration or Kind.BindingElement or Kind.PropertyAccessExpression or Kind.ElementAccessExpression or Kind.BinaryExpression or Kind.CallExpression or Kind.VariableDeclaration or Kind.ExportAssignment or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.JSDocParameterTag or Kind.JSDocPropertyTag;
}

public static partial class Ast
{
    // utilities.go:2731.
    public static bool IsParseTreeNode(Node node) => (node.Flags & NodeFlags.Synthesized) == 0;
}

public static partial class Ast
{
    // utilities.go:3787.
    public static bool HasQuestionToken(Node node) => IsQuestionToken(node.QuestionToken());
}

public static partial class Ast
{
    // utilities.go:3766.
    public static bool IsRightSideOfQualifiedNameOrPropertyAccess(Node node)
    {
        var parent = node.Parent;
        return parent.Kind switch
        {
            Kind.QualifiedName => parent.AsQualifiedName().Right == node,
            Kind.PropertyAccessExpression => parent.AsPropertyAccessExpression().Name() == node,
            Kind.MetaProperty => parent.AsMetaProperty().Name() == node,
            _ => false
        };
    }
}
