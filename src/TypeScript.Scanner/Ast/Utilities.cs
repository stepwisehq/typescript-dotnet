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
