// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/utilities.go; parser-required syntax dependency slice.
#nullable disable
namespace TypeScript.Scanner;
public static partial class Ast
{
    // utilities.go:1695/1699.
    public static bool IsEffectiveExternalModule(SourceFile node, CompilerOptions compilerOptions) =>
        IsExternalModule(node) || isCommonJSContainingModuleKind(compilerOptions.GetEmitModuleKind()) && node.CommonJSModuleIndicator != null;
    // utilities.go:3025.
    public static Node GetClassLikeDeclarationOfSymbol(Symbol symbol) => Core.Find(symbol.Declarations, IsClassLike);

    // utilities.go:2632–2671. Typed filenames contain original Go bytes.
    public static ModuleKind GetImpliedNodeFormatForFile(RootedFilePath fileName, string packageJsonType)
    {
        if (fileName.ExtensionIsOneOf([TsPath.ExtensionDmts, TsPath.ExtensionMts, TsPath.ExtensionMjs]))
            return ModuleKind.ESNext;
        if (fileName.ExtensionIsOneOf([TsPath.ExtensionDcts, TsPath.ExtensionCts, TsPath.ExtensionCjs]))
            return ModuleKind.CommonJS;
        if (fileName.ExtensionIsOneOf([TsPath.ExtensionDts, TsPath.ExtensionTs, TsPath.ExtensionTsx, TsPath.ExtensionJs, TsPath.ExtensionJsx]))
            return packageJsonType == "module" ? ModuleKind.ESNext : ModuleKind.CommonJS;
        return ModuleKind.None;
    }

    public static ModuleKind GetEmitModuleFormatOfFileWorker(RootedFilePath fileName, CompilerOptions options, SourceFileMetaData metadata)
    {
        ModuleKind result = (ModuleKind)GetImpliedNodeFormatForEmitWorker(fileName, options.GetEmitModuleKind(), metadata);
        return result != ModuleKind.None ? result : options.GetEmitModuleKind();
    }

    public static ResolutionMode GetImpliedNodeFormatForEmitWorker(RootedFilePath fileName, ModuleKind emitModuleKind, SourceFileMetaData metadata)
    {
        if (emitModuleKind >= ModuleKind.Node16 && emitModuleKind <= ModuleKind.NodeNext)
            return metadata.ImpliedNodeFormat;
        if (metadata.ImpliedNodeFormat == ResolutionMode.CommonJS &&
            (metadata.PackageJsonType == "commonjs" || fileName.ExtensionIsOneOf([TsPath.ExtensionCjs, TsPath.ExtensionCts])))
            return ResolutionMode.CommonJS;
        if (metadata.ImpliedNodeFormat == ResolutionMode.ESNext &&
            (metadata.PackageJsonType == "module" || fileName.ExtensionIsOneOf([TsPath.ExtensionMjs, TsPath.ExtensionMts])))
            return ResolutionMode.ESNext;
        return ResolutionMode.None;
    }

    // utilities.go:2851–2880. These are loader synthetic-dependency producers.
    public static string GetJSXImplicitImportBase(CompilerOptions options, SourceFile file)
    {
        Pragma source = GetPragmaFromSourceFile(file, "jsximportsource");
        Pragma runtime = GetPragmaFromSourceFile(file, "jsxruntime");
        if (GetPragmaArgument(runtime, "factory") == "classic")
            return "";
        if (options.Jsx is JsxEmit.ReactJSX or JsxEmit.ReactJSXDev || options.JsxImportSource != "" ||
            source is not null || GetPragmaArgument(runtime, "factory") == "automatic")
        {
            string result = GetPragmaArgument(source, "factory");
            if (result == "") result = options.JsxImportSource;
            return result == "" ? "react" : result;
        }
        return "";
    }

    public static string GetJSXRuntimeImport(string importBase, CompilerOptions options) => importBase == "" ? importBase :
        importBase + "/" + (options.Jsx == JsxEmit.ReactJSXDev ? "jsx-dev-runtime" : "jsx-runtime");

    // utilities.go:3779. Filename is retained in the source API although unused by this producer.
    public static bool ShouldTransformImportCall(RootedFilePath fileName, CompilerOptions options, ModuleKind impliedNodeFormatForEmit)
    {
        ModuleKind kind = options.GetEmitModuleKind();
        if ((kind >= ModuleKind.Node16 && kind <= ModuleKind.NodeNext) || kind == ModuleKind.Preserve)
            return false;
        return impliedNodeFormatForEmit < ModuleKind.ES2015;
    }

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
    public static ModifierFlags ModifiersToFlags(Slice<Node> modifiers)
    {
        ModifierFlags flags = default;
        foreach (var modifier in modifiers)
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
            return ((node.AsMetaProperty().KeywordToken == Kind.ImportKeyword) && (node.AsMetaProperty().Name().TextEquals("meta"u8)));
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
        return (IsIdentifier(node) && (node.TextEquals("exports"u8)));
    }

    // Upstream function: IsModuleIdentifier
    public static bool IsModuleIdentifier(Node node)
    {
        return (IsIdentifier(node) && (node.TextEquals("module"u8)));
    }

    // Upstream function: IsThisIdentifier
    public static bool IsThisIdentifier(Node node)
    {
        return (IsIdentifier(node) && (node.TextEquals("this"u8)));
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
                    if ((((((IsPropertyAccessExpression(expr) && IsIdentifier(expr.Expression())) && (expr.Expression().TextEquals("Object"u8))) && (expr.Name().TextEquals("defineProperty"u8))) && IsStringOrNumericLiteralLike(args[1])) && IsBindableStaticNameExpression(args[0], true)))
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
            return (node1.TextValue().EqualsValue(node2.TextValue()));
        }
        else
        {
            if (((node1.Kind == Kind.PropertyAccessExpression) && (node2.Kind == Kind.PropertyAccessExpression)))
            {
                return ((node1.AsPropertyAccessExpression().Name().TextValue().EqualsValue(node2.AsPropertyAccessExpression().Name().TextValue())) && HasSamePropertyAccessName(node1.Expression(), node2.Expression()));
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
                foreach (var jsdoc in current.JSDoc(file))
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
    public static (int, int) findImportOrRequire(string text, int start) => findImportOrRequire(ByteStringView.FromGoBytes(text), start);
    internal static (int, int) findImportOrRequire(ByteStringView text, int start)
    {
        int index = default;
        int size = default;
        index = Math.Max(start, 0);
        var n = GoRuntime.Len(text);
        while ((index < n))
        {
            var next = text.AsSpan(index).IndexOfAny((byte)'i', (byte)'r');
            if ((next < 0))
            {
                break;
            }

            index += next;
            ReadOnlySpan<byte> expected;
            if ((text[index] == 'i'))
            {
                size = 6;
                expected = "import"u8;
            }
            else
            {
                size = 7;
                expected = "require"u8;
            }

            if ((((index + size) <= n) && (GoStrings.Equal(text.AsSpan(index, size), expected))))
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
        var(lastIndex, size) = findImportOrRequire(file.TextView(), 0);
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
            (lastIndex, size) = findImportOrRequire(file.TextView(), lastIndex);
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
        if ((!IsIdentifier(call.Expression) || (!call.Expression.TextEquals("require"u8))))
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
                    return (name.TextEquals("exports"u8));
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
                return (lhs.TextValue().EqualsValue(rhs.TextValue()));
            }

            case Kind.ThisKeyword:
            {
                return true;
            }

            case Kind.JsxNamespacedName:
            {
                return ((lhs.AsJsxNamespacedName().Namespace.TextValue().EqualsValue(rhs.AsJsxNamespacedName().Namespace.TextValue())) && (lhs.AsJsxNamespacedName().Name().TextValue().EqualsValue(rhs.AsJsxNamespacedName().Name().TextValue())));
            }

            case Kind.PropertyAccessExpression:
            {
                return ((lhs.AsPropertyAccessExpression().Name().TextValue().EqualsValue(rhs.AsPropertyAccessExpression().Name().TextValue())) && TagNamesAreEquivalent(lhs.Expression(), rhs.Expression()));
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
    public static bool IsConstTypeReference(Node node) => IsTypeReferenceNode(node) && GoRuntime.Len(node.TypeArguments()) == 0 && IsIdentifier(node.AsTypeReferenceNode().TypeName) && node.AsTypeReferenceNode().TypeName.TextEquals("const"u8);
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
                return (GoSlices.Contains(parent.TypeArguments(), node));
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
            return (IsIdentifier(name) && (name.TextValue().EqualsValue(id.TextValue())));
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
        return (node.TextValue().EqualsString(InternalSymbolNameDefault));
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
        foreach (var jsdoc in node.JSDoc(null))
            if (jsdoc.AsJSDoc().Tags is { } tags)
                foreach (var tag in tags.Nodes)
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

public static partial class Ast
{
    // utilities.go:1726/1742/1750/1759/3671; data-interface syntax closure.
    public static Node GetHeritageClause(Node node, Kind kind)
    {
        foreach (var clause in getHeritageClauses(node)?.Nodes ?? default)
            if (clause.AsHeritageClause().Token == kind)
                return clause;
        return null;
    }

    private static NodeList getHeritageClauses(Node node) => node.Kind switch
    {
        Kind.ClassDeclaration => node.AsClassDeclaration().HeritageClauses,
        Kind.ClassExpression => node.AsClassExpression().HeritageClauses,
        Kind.InterfaceDeclaration => node.AsInterfaceDeclaration().HeritageClauses,
        _ => null
    };
    public static Slice<Node> GetHeritageElements(Node node, Kind kind) => GetHeritageClause(node, kind)?.AsHeritageClause().Types.Nodes ?? default;
    public static Slice<Node> GetExtendsHeritageClauseElements(Node node) => GetHeritageElements(node, Kind.ExtendsKeyword);
    public static Node GetHeritageClauseElementName(Node node) => IsTypeReferenceNode(node) ? node.AsTypeReferenceNode().TypeName : node.AsExpressionWithTypeArguments().Expression;
    public static bool IsTypeDeclarationName(Node name) => name.Kind == Kind.Identifier && IsTypeDeclaration(name.Parent) && GetNameOfDeclaration(name.Parent) == name;
    // ast/utilities.go:2670.
    public static Node GetDeclarationContainer(Node node) => FindAncestor(GetRootDeclaration(node), candidate => candidate.Kind is not (Kind.VariableDeclaration or Kind.VariableDeclarationList or Kind.ImportSpecifier or Kind.NamedImports or Kind.NamespaceImport or Kind.ImportClause)).Parent;
    // ast/utilities.go:275/853/1734.
    public static bool IsEntityName(Node node) => node.Kind is Kind.Identifier or Kind.QualifiedName;
    public static Node WalkUpParenthesizedTypes(Node node)
    {
        while (node != null && node.Kind == Kind.ParenthesizedType)
            node = node.Parent;
        return node;
    }

    public static bool IsModuleWithStringLiteralName(Node node) => IsModuleDeclaration(node) && node.Name().Kind == Kind.StringLiteral;
    // ast/utilities.go:2587.
    public static Node GetFirstIdentifier(Node node) => node.Kind switch
    {
        Kind.Identifier => node,
        Kind.QualifiedName => GetFirstIdentifier(node.AsQualifiedName().Left),
        Kind.PropertyAccessExpression => GetFirstIdentifier(node.AsPropertyAccessExpression().Expression),
        _ => throw new InvalidOperationException("Unhandled case in GetFirstIdentifier")};
}

public static partial class Ast
{
    // ast/utilities.go:2688.
    public static bool IsNonLocalAlias(Symbol symbol, SymbolFlags excludes)
    {
        if (symbol == null)
            return false;
        return (symbol.Flags & (SymbolFlags.Alias | excludes)) == SymbolFlags.Alias || (symbol.Flags & SymbolFlags.Alias) != 0 && (symbol.Flags & SymbolFlags.Assignment) != 0;
    }
}

public static partial class Ast
{
    // ast/utilities.go:1766.
    public static bool IsNameOfHeritageClauseTypeReference(Node node)
    {
        while (IsQualifiedName(node.Parent))
            node = node.Parent;
        return IsTypeReferenceNode(node.Parent) && node.Parent.AsTypeReferenceNode().TypeName == node && IsHeritageClause(node.Parent.Parent);
    }
}

public static partial class Ast
{
    public static bool IsAssignmentPattern(Node node) => node.Kind is Kind.ArrayLiteralExpression or Kind.ObjectLiteralExpression;
    public static Slice<Node> GetElementsOfBindingOrAssignmentPattern(Node name)
    {
        switch (name.Kind)
        {
            case Kind.ObjectBindingPattern:
            case Kind.ArrayBindingPattern:
            case Kind.ArrayLiteralExpression:
                return name.Elements();
            case Kind.ObjectLiteralExpression:
                return name.Properties();
        }

        return default;
    }

    public static bool IsDeclarationBindingElement(Node bindingElement) => bindingElement.Kind is Kind.VariableDeclaration or Kind.Parameter or Kind.BindingElement;
    public static Node GetTargetOfBindingOrAssignmentElement(Node bindingElement)
    {
        if (IsDeclarationBindingElement(bindingElement))
            return bindingElement.Name();
        if (IsObjectLiteralElement(bindingElement))
        {
            switch (bindingElement.Kind)
            {
                case Kind.PropertyAssignment:
                    return GetTargetOfBindingOrAssignmentElement(bindingElement.Initializer());
                case Kind.ShorthandPropertyAssignment:
                    return bindingElement.Name();
                case Kind.SpreadAssignment:
                    return GetTargetOfBindingOrAssignmentElement(bindingElement.Expression());
            }

            return null;
        }

        if (IsAssignmentExpression(bindingElement, true))
            return GetTargetOfBindingOrAssignmentElement(bindingElement.AsBinaryExpression().Left);
        if (IsSpreadElement(bindingElement))
            return GetTargetOfBindingOrAssignmentElement(bindingElement.Expression());
        return bindingElement;
    }

    public static bool ContainsObjectRestOrSpread(Node node)
    {
        if ((node.SubtreeFacts() & SubtreeFacts.ContainsObjectRestOrSpread) != 0)
            return true;
        if ((node.SubtreeFacts() & SubtreeFacts.ContainsESObjectRestOrSpread) != 0)
        {
            foreach (var element in GetElementsOfBindingOrAssignmentPattern(node))
            {
                var target = GetTargetOfBindingOrAssignmentElement(element);
                if (target is not null && IsAssignmentPattern(target))
                {
                    if ((target.SubtreeFacts() & SubtreeFacts.ContainsObjectRestOrSpread) != 0)
                        return true;
                    if ((target.SubtreeFacts() & SubtreeFacts.ContainsESObjectRestOrSpread) != 0 && ContainsObjectRestOrSpread(target))
                        return true;
                }
            }
        }

        return false;
    }
}

// Foundation syntax closure from utilities.go:1294,4556-4574.
public static partial class Ast
{
    public static bool IsSourceFileJS(SourceFile file) => file.ScriptKind is ScriptKind.JS or ScriptKind.JSX;
    public static bool IsJSDocTag(Node node) => node.Kind >= Kind.FirstJSDocTagNode && node.Kind <= Kind.LastJSDocTagNode;
    public static bool IsTagName(Node node) => node.Parent != null && IsJSDocTag(node.Parent) && node.Parent.TagName() == node;
    public static bool IsLiteralComputedPropertyDeclarationName(Node node) => IsStringOrNumericLiteralLike(node) && node.Parent.Kind == Kind.ComputedPropertyName && IsDeclaration(node.Parent.Parent);
    internal static bool literalIsName(Node node) => IsDeclarationName(node) || node.Parent.Kind == Kind.ExternalModuleReference || isArgumentOfElementAccessExpression(node) || IsLiteralComputedPropertyDeclarationName(node);
    private static bool isArgumentOfElementAccessExpression(Node node) => node != null && node.Parent != null && node.Parent.Kind == Kind.ElementAccessExpression && node.Parent.AsElementAccessExpression().ArgumentExpression == node;
}

// BEGIN FOUNDATION SIMPLE SYNTAX HELPERS
// Foundation syntax fragments from pinned ast/utilities.go. Reviewed C# source is authoritative.
public static partial class Ast
{
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:108:1.
    public static bool IsModifierLike(Node node)
    {
        return (IsModifier(node) || IsDecorator(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:112:1.
    public static bool IsCompoundAssignment(Kind token)
    {
        return ((token >= Kind.FirstCompoundAssignment) && (token <= Kind.LastCompoundAssignment));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:140:1.
    public static bool IsObjectBindingOrAssignmentElement(Node node)
    {
        switch (node.Kind)
        {
            case Kind.BindingElement:
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.SpreadAssignment:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:151:1.
    public static bool IsArrayBindingOrAssignmentElement(Node node)
    {
        switch (node.Kind)
        {
            case Kind.BindingElement:
            case Kind.OmittedExpression:
            case Kind.SpreadElement:
            case Kind.ArrayLiteralExpression:
            case Kind.ObjectLiteralExpression:
            case Kind.Identifier:
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                return true;
            }
        }

        return IsAssignmentExpression(node, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:256:1.
    public static bool IsAccessor(Node node)
    {
        return ((node.Kind == Kind.GetAccessor) || (node.Kind == Kind.SetAccessor));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:271:1.
    public static bool IsMemberName(Node node)
    {
        return ((node.Kind == Kind.Identifier) || (node.Kind == Kind.PrivateIdentifier));
    }

    // Determines whether a node is a UnaryExpression based only on its kind.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:430:1.
    public static bool IsUnaryExpression(Node node)
    {
        return isUnaryExpressionKind(SkipPartiallyEmittedExpressions(node).Kind);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:455:1.
    public static bool IsCommaExpression(Node node)
    {
        return ((node.Kind == Kind.BinaryExpression) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:459:1.
    public static bool IsCommaSequence(Node node)
    {
        return IsCommaExpression(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:463:1.
    public static bool IsIterationStatement(Node node, bool lookInLabeledStatements)
    {
        switch (node.Kind)
        {
            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.DoStatement:
            case Kind.WhileStatement:
            {
                return true;
            }

            case Kind.LabeledStatement:
            {
                return (lookInLabeledStatements && IsIterationStatement(node.Statement(), lookInLabeledStatements));
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:535:1.
    public static bool IsClassOrInterfaceLike(Node node)
    {
        return (((node.Kind == Kind.ClassDeclaration) || (node.Kind == Kind.ClassExpression)) || (node.Kind == Kind.InterfaceDeclaration));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:572:1.
    public static bool IsTypeElement(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ConstructSignature:
            case Kind.CallSignature:
            case Kind.PropertySignature:
            case Kind.MethodSignature:
            case Kind.IndexSignature:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.NotEmittedTypeElement:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:612:1.
    public static bool IsJsxChild(Node node)
    {
        switch (node.Kind)
        {
            case Kind.JsxElement:
            case Kind.JsxExpression:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxText:
            case Kind.JsxFragment:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:624:1.
    public static bool IsJsxAttributeLike(Node node)
    {
        return (IsJsxAttribute(node) || IsJsxSpreadAttribute(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:657:1.
    private static bool isStatementKindButNotDeclarationKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.BreakStatement:
            case Kind.ContinueStatement:
            case Kind.DebuggerStatement:
            case Kind.DoStatement:
            case Kind.ExpressionStatement:
            case Kind.EmptyStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.ForStatement:
            case Kind.IfStatement:
            case Kind.LabeledStatement:
            case Kind.ReturnStatement:
            case Kind.SwitchStatement:
            case Kind.ThrowStatement:
            case Kind.TryStatement:
            case Kind.VariableStatement:
            case Kind.WhileStatement:
            case Kind.WithStatement:
            case Kind.NotEmittedStatement:
            {
                return true;
            }
        }

        return false;
    }

    // Determines whether a node is a Statement that is not also a Declaration. Ideally this does not use Parent pointers,
    // but it may use them to rule out a Block node that is part of `try` or `catch` or is the Block-like body of a function.
    //
    // NOTE: ECMA262 would just call this a Statement
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:687:1.
    public static bool IsStatementButNotDeclaration(Node node)
    {
        return isStatementKindButNotDeclarationKind(node.Kind);
    }

    // Determines whether a node is a Statement. Ideally this does not use Parent pointers, but it may use
    // them to rule out a Block node that is part of `try` or `catch` or is the Block-like body of a function.
    //
    // NOTE: ECMA262 would call this either a StatementListItem or ModuleListItem
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:695:1.
    public static bool IsStatement(Node node)
    {
        var kind = node.Kind;
        return ((isStatementKindButNotDeclarationKind(kind) || isDeclarationStatementKind(kind)) || isBlockStatement(node));
    }

    // Determines whether a node is a BlockStatement. If parents are available, this ensures the Block is
    // not part of a `try` statement, `catch` clause, or the Block-like body of a function
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:702:1.
    private static bool isBlockStatement(Node node)
    {
        if ((node.Kind != Kind.Block))
        {
            return false;
        }

        if (((node.Parent != null) && (((node.Parent.Kind == Kind.TryStatement) || (node.Parent.Kind == Kind.CatchClause)))))
        {
            return false;
        }

        return !IsFunctionBlock(node);
    }

    // Determines whether a node is the Block-like body of a function by walking the parent of the node
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:713:1.
    public static bool IsFunctionBlock(Node node)
    {
        return ((((node != null) && (node.Kind == Kind.Block)) && (node.Parent != null)) && IsFunctionLike(node.Parent));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:755:1.
    public static bool IsJSDocKind(Kind kind)
    {
        return ((Kind.FirstJSDocNode <= kind) && (kind <= Kind.LastJSDocNode));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:980:1.
    public static bool IsNodeDescendantOf(Node node, Node ancestor)
    {
        while ((node != null))
        {
            if ((node == ancestor))
            {
                return true;
            }

            node = node.Parent;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1053:1.
    public static bool CanHaveSymbol(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ArrowFunction:
            case Kind.BinaryExpression:
            case Kind.BindingElement:
            case Kind.CallExpression:
            case Kind.CallSignature:
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.ClassStaticBlockDeclaration:
            case Kind.Constructor:
            case Kind.ConstructorType:
            case Kind.ConstructSignature:
            case Kind.ElementAccessExpression:
            case Kind.EnumDeclaration:
            case Kind.EnumMember:
            case Kind.ExportAssignment:
            case Kind.ExportDeclaration:
            case Kind.ExportSpecifier:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.FunctionType:
            case Kind.GetAccessor:
            case Kind.ImportClause:
            case Kind.ImportEqualsDeclaration:
            case Kind.ImportSpecifier:
            case Kind.IndexSignature:
            case Kind.InterfaceDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.JsxAttribute:
            case Kind.JsxAttributes:
            case Kind.JsxSpreadAttribute:
            case Kind.MappedType:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.ModuleDeclaration:
            case Kind.NamedTupleMember:
            case Kind.NamespaceExport:
            case Kind.NamespaceExportDeclaration:
            case Kind.NamespaceImport:
            case Kind.NewExpression:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
            case Kind.ObjectLiteralExpression:
            case Kind.Parameter:
            case Kind.PropertyAccessExpression:
            case Kind.PropertyAssignment:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.SetAccessor:
            case Kind.ShorthandPropertyAssignment:
            case Kind.SourceFile:
            case Kind.SpreadAssignment:
            case Kind.StringLiteral:
            case Kind.TypeAliasDeclaration:
            case Kind.TypeLiteral:
            case Kind.TypeParameter:
            case Kind.VariableDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1088:1.
    public static bool CanHaveIllegalModifiers(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ClassStaticBlockDeclaration:
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.MissingDeclaration:
            case Kind.NamespaceExportDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Gets whether a bound `VariableDeclaration` or `VariableDeclarationList` is part of an `await using` declaration.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1213:1.
    public static bool IsVarAwaitUsing(Node node)
    {
        return ((GetCombinedNodeFlags(node) & NodeFlags.BlockScoped) == NodeFlags.AwaitUsing);
    }

    // Gets whether a bound `VariableDeclaration` or `VariableDeclarationList` is part of a `using` declaration.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1218:1.
    public static bool IsVarUsing(Node node)
    {
        return ((GetCombinedNodeFlags(node) & NodeFlags.BlockScoped) == NodeFlags.Using);
    }

    // Gets whether a bound `VariableDeclaration` or `VariableDeclarationList` is part of a `const` declaration.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1261:1.
    public static bool IsVarConst(Node node)
    {
        return ((GetCombinedNodeFlags(node) & NodeFlags.BlockScoped) == NodeFlags.Const);
    }

    // Gets whether a bound `VariableDeclaration` or `VariableDeclarationList` is part of a `const`, `using` or `await using` declaration.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1266:1.
    public static bool IsVarConstLike(Node node)
    {
        switch ((GetCombinedNodeFlags(node) & NodeFlags.BlockScoped))
        {
            case NodeFlags.Const:
            case NodeFlags.Using:
            case NodeFlags.AwaitUsing:
            {
                return true;
            }
        }

        return false;
    }

    // Gets whether a bound `VariableDeclaration` or `VariableDeclarationList` is part of a `let` declaration.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1275:1.
    public static bool IsVarLet(Node node)
    {
        return ((GetCombinedNodeFlags(node) & NodeFlags.BlockScoped) == NodeFlags.Let);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1330:1.
    public static bool IsExternalModuleImportEqualsDeclaration(Node node)
    {
        return ((node.Kind == Kind.ImportEqualsDeclaration) && (node.AsImportEqualsDeclaration().ModuleReference.Kind == Kind.ExternalModuleReference));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1351:1.
    public static bool IsImportOrExportSpecifier(Node node)
    {
        return (IsImportSpecifier(node) || IsExportSpecifier(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1355:1.
    public static bool IsVoidZero(Node node)
    {
        return ((IsVoidExpression(node) && IsNumericLiteral(node.Expression())) && (node.Expression().TextEquals("0"u8)));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1371:1.
    public static bool IsThisParameter(Node node)
    {
        return ((IsParameterDeclaration(node) && (node.Name() != null)) && IsThisIdentifier(node.Name()));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1388:1.
    public static bool IsPrototypeAccess(Node node)
    {
        if (IsBindableStaticAccessExpression(node, false))
        {
            {
                var name = GetElementOrPropertyAccessName(node);
                if ((name != null))
                {
                    return (name.TextEquals("prototype"u8));
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1430:1.
    public static bool IsExpressionWithTypeArgumentsInClassExtendsClause(Node node)
    {
        return (TryGetClassExtendingExpressionWithTypeArguments(node) != null);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1434:1.
    public static Node TryGetClassExtendingExpressionWithTypeArguments(Node node)
    {
        if (!IsExpressionWithTypeArguments(node))
        {
            return null;
        }

        var tuple1 = TryGetClassImplementingOrExtendingHeritageClauseElement(node);
        var cls = tuple1.Item1;
        var isImplements = tuple1.Item2;
        if (((cls != null) && !isImplements))
        {
            return cls;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1445:1.
    public static (Node, bool) TryGetClassImplementingOrExtendingHeritageClauseElement(Node node)
    {
        if (((((IsExpressionWithTypeArguments(node) || IsTypeReferenceNode(node))) && IsHeritageClause(node.Parent)) && IsClassLike(node.Parent.Parent)))
        {
            return (node.Parent.Parent, (node.Parent.AsHeritageClause().Token == Kind.ImplementsKeyword));
        }

        return (null, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1703:1.
    public static bool IsExternalModuleIndicator(Node node)
    {
        return ((IsAnyImportOrReExport(node) || IsExportAssignment(node)) || HasSyntacticModifier(node, ModifierFlags.Export));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1708:1.
    public static bool IsExportNamespaceAsDefaultDeclaration(Node node)
    {
        if (IsExportDeclaration(node))
        {
            var decl = node.AsExportDeclaration();
            return (IsNamespaceExport(decl.ExportClause) && ModuleExportNameIsDefault(decl.ExportClause.Name()));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1915:1.
    public static bool IsInstanceOfExpression(Node node)
    {
        return (IsBinaryExpression(node) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.InstanceOfKeyword));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1938:1.
    public static bool IsInJsonFile(Node node)
    {
        return ((node.Flags & NodeFlags.JsonFile) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1964:1.
    public static bool HasImportAttributes(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ImportType:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2141:1.
    public static bool IsSuperCall(Node node)
    {
        return (IsCallExpression(node) && (node.Expression().Kind == Kind.SuperKeyword));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2169:1.
    public static bool IsSourcePhaseImport(Node node)
    {
        if (IsImportDeclaration(node))
        {
            var clause = node.AsImportDeclaration().ImportClause;
            return ((clause != null) && (clause.AsImportClause().PhaseModifier == Kind.SourceKeyword));
        }

        return IsSourcePhaseImportCall(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2177:1.
    public static bool IsSourcePhaseImportCall(Node node)
    {
        return (IsCallExpression(node) && IsImportSourceMetaProperty(node.Expression()));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2181:1.
    public static bool IsComputedNonLiteralName(Node name)
    {
        return (IsComputedPropertyName(name) && !IsStringOrNumericLiteralLike(name.Expression()));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2228:1.
    public static bool IsJSDocNode(Node node)
    {
        return ((node.Kind >= Kind.FirstJSDocNode) && (node.Kind <= Kind.LastJSDocNode));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2232:1.
    public static bool IsNonWhitespaceToken(Node node)
    {
        return (IsTokenKind(node.Kind) && !IsWhitespaceOnlyJsxText(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2236:1.
    public static bool IsWhitespaceOnlyJsxText(Node node)
    {
        return ((node.Kind == Kind.JsxText) && node.AsJsxText().ContainsOnlyTriviaWhiteSpaces);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2339:1.
    public static bool IsPropertyAccessOrQualifiedName(Node node)
    {
        return ((node.Kind == Kind.PropertyAccessExpression) || (node.Kind == Kind.QualifiedName));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2343:1.
    public static bool IsLabelName(Node node)
    {
        return (IsLabelOfLabeledStatement(node) || IsJumpStatementTarget(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2347:1.
    public static bool IsLabelOfLabeledStatement(Node node)
    {
        if (!IsIdentifier(node))
        {
            return false;
        }

        if (!IsLabeledStatement(node.Parent))
        {
            return false;
        }

        return (node == node.Parent.Label());
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2357:1.
    public static bool IsJumpStatementTarget(Node node)
    {
        if (!IsIdentifier(node))
        {
            return false;
        }

        if (!IsBreakOrContinueStatement(node.Parent))
        {
            return false;
        }

        return (node == node.Parent.Label());
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2367:1.
    public static bool IsBreakOrContinueStatement(Node node)
    {
        return NodeKindIs(node, Kind.BreakStatement, Kind.ContinueStatement);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2541:1.
    public static bool IsInternalModuleImportEqualsDeclaration(Node node)
    {
        return (IsImportEqualsDeclaration(node) && (node.AsImportEqualsDeclaration().ModuleReference.Kind != Kind.ExternalModuleReference));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2561:1.
    public static bool IsParameterLike(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
            case Kind.TypeParameter:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2623:1.
    public static bool IsDefaultImport(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                var importClause = node.ImportClause();
                return ((importClause != null) && (importClause.AsImportClause().Name() != null));
            }
        }

        return false;
    }

    // An alias symbol is created by one of the following declarations:
    //
    //	import <symbol> = ...
    //	const <symbol> = ... (JS only)
    //	const { <symbol>, ... } = ... (JS only)
    //	import <symbol> from ...
    //	import * as <symbol> from ...
    //	import { x as <symbol> } from ...
    //	export { x as <symbol> } from ...
    //	export * as ns <symbol> from ...
    //	export = <EntityNameExpression>
    //	export default <EntityNameExpression>
    //	module.exports = <EntityNameExpression> (JS only)
    //	module.exports.<symbol> = <EntityNameExpression> (JS only)
    //	exports.<symbol> = <EntityNameExpression> (JS only)
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2711:1.
    public static bool IsAliasSymbolDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportEqualsDeclaration:
            case Kind.NamespaceExportDeclaration:
            case Kind.NamespaceImport:
            case Kind.NamespaceExport:
            case Kind.ImportSpecifier:
            case Kind.ExportSpecifier:
            {
                return true;
            }

            case Kind.ImportClause:
            {
                return (node.AsImportClause().Name() != null);
            }

            case Kind.ExportAssignment:
            {
                return ExpressionIsAlias(node.Expression());
            }

            case Kind.VariableDeclaration:
            case Kind.BindingElement:
            {
                return IsVariableDeclarationInitializedToRequire(node);
            }

            case Kind.BinaryExpression:
            {
                switch (GetAssignmentDeclarationKind(node))
                {
                    case JSDeclarationKind.ModuleExports:
                    case JSDeclarationKind.ExportsProperty:
                    {
                        return ExpressionIsAlias(node.AsBinaryExpression().Right);
                    }
                }

                break;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2842:1.
    public static bool IsRequireVariableStatement(Node node)
    {
        if (IsVariableStatement(node))
        {
            {
                var declarations = node.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
                if ((GoRuntime.Len(declarations) > 0))
                {
                    return Core.Every(declarations, IsVariableDeclarationInitializedToRequire);
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2912:1.
    public static bool IsVariableDeclarationInitializedToBareOrAccessedRequire(Node node)
    {
        return isVariableDeclarationInitializedWithRequireHelper(node, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2958:1.
    public static bool IsModuleExportsQualifiedName(Node node)
    {
        return ((IsQualifiedName(node) && IsModuleIdentifier(node.AsQualifiedName().Left)) && (node.AsQualifiedName().Right.TextEquals("exports"u8)));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2969:1.
    public static bool IsPlainJSFile(SourceFile file, Tristate checkJs)
    {
        return ((((file != null) && (((file.ScriptKind == ScriptKind.JS) || (file.ScriptKind == ScriptKind.JSX)))) && (file.CheckJsDirective == null)) && (checkJs == Tristate.Unknown));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2980:1.
    public static bool IsTypeOnlyImportDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportSpecifier:
            {
                return (node.IsTypeOnly() || node.Parent.Parent.IsTypeOnly());
            }

            case Kind.NamespaceImport:
            {
                return node.Parent.IsTypeOnly();
            }

            case Kind.ImportClause:
            case Kind.ImportEqualsDeclaration:
            {
                return node.IsTypeOnly();
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2992:1.
    private static bool isTypeOnlyExportDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ExportSpecifier:
            {
                return (node.IsTypeOnly() || node.Parent.Parent.IsTypeOnly());
            }

            case Kind.ExportDeclaration:
            {
                var d = node.AsExportDeclaration();
                return ((d.IsTypeOnly && (d.ModuleSpecifier != null)) && (d.ExportClause == null));
            }

            case Kind.NamespaceExport:
            {
                return node.Parent.IsTypeOnly();
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3005:1.
    public static bool IsTypeOnlyImportOrExportDeclaration(Node node)
    {
        return (IsTypeOnlyImportDeclaration(node) || isTypeOnlyExportDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3009:1.
    public static bool IsExclusivelyTypeOnlyImportOrExport(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ExportDeclaration:
            {
                return node.IsTypeOnly();
            }

            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                {
                    var importClause = node.ImportClause();
                    if ((importClause != null))
                    {
                        return importClause.IsTypeOnly();
                    }
                }

                break;
            }

            case Kind.JSDocImportTag:
            {
                {
                    var importClause2 = node.ImportClause();
                    if ((importClause2 != null))
                    {
                        return importClause2.IsTypeOnly();
                    }
                }

                break;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3029:1.
    public static bool IsCallLikeExpression(Node node)
    {
        switch (node.Kind)
        {
            case Kind.JsxOpeningElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxOpeningFragment:
            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.TaggedTemplateExpression:
            case Kind.Decorator:
            {
                return true;
            }

            case Kind.BinaryExpression:
            {
                return (node.AsBinaryExpression().OperatorToken.Kind == Kind.InstanceOfKeyword);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3040:1.
    public static bool IsJsxCallLike(Node node)
    {
        switch (node.Kind)
        {
            case Kind.JsxOpeningElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxOpeningFragment:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3048:1.
    public static bool IsCallLikeOrFunctionLikeExpression(Node node)
    {
        return (IsCallLikeExpression(node) || IsFunctionExpressionOrArrowFunction(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3052:1.
    public static bool NodeHasKind(Node node, Kind kind)
    {
        if ((node == null))
        {
            return false;
        }

        return (node.Kind == kind);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3059:1.
    public static bool IsContextualKeyword(Kind token)
    {
        return ((Kind.FirstContextualKeyword <= token) && (token <= Kind.LastContextualKeyword));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3063:1.
    public static bool IsThisInTypeQuery(Node node)
    {
        if (!IsThisIdentifier(node))
        {
            return false;
        }

        while ((IsQualifiedName(node.Parent) && (node.Parent.AsQualifiedName().Left == node)))
        {
            node = node.Parent;
        }

        return (node.Parent.Kind == Kind.TypeQuery);
    }

    // Gets whether a bound `VariableDeclaration` or `VariableDeclarationList` is part of a `let` declaration.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3074:1.
    public static bool IsLet(Node node)
    {
        return ((GetCombinedNodeFlags(node) & NodeFlags.BlockScoped) == NodeFlags.Let);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3094:1.
    public static bool HasTypeArguments(Node node)
    {
        switch (node.Kind)
        {
            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.TaggedTemplateExpression:
            case Kind.TypeReference:
            case Kind.ExpressionWithTypeArguments:
            case Kind.ImportType:
            case Kind.TypeQuery:
            case Kind.JsxOpeningElement:
            case Kind.JsxSelfClosingElement:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3104:1.
    public static bool IsTypeReferenceType(Node node)
    {
        return ((node.Kind == Kind.TypeReference) || (node.Kind == Kind.ExpressionWithTypeArguments));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3108:1.
    public static bool IsVariableLike(Node node)
    {
        switch (node.Kind)
        {
            case Kind.BindingElement:
            case Kind.EnumMember:
            case Kind.Parameter:
            case Kind.PropertyAssignment:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.ShorthandPropertyAssignment:
            case Kind.VariableDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3117:1.
    public static bool HasInitializer(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.BindingElement:
            case Kind.PropertyDeclaration:
            case Kind.PropertyAssignment:
            case Kind.EnumMember:
            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.JsxAttribute:
            {
                return (node.Initializer() != null);
            }

            default:
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3128:1.
    public static bool IsVariableParameterOrProperty(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.PropertySignature:
            case Kind.PropertyDeclaration:
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3154:1.
    public static bool IsObjectTypeDeclaration(Node node)
    {
        return ((IsClassLike(node) || IsInterfaceDeclaration(node)) || IsTypeLiteralNode(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3158:1.
    public static bool IsClassOrTypeElement(Node node)
    {
        return (IsClassElement(node) || IsTypeElement(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3170:1.
    public static bool IsTypeKeywordToken(Node node)
    {
        return (node.Kind == Kind.TypeKeyword);
    }

    // See `IsJSDocSingleCommentNode`.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3175:1.
    public static bool IsJSDocSingleCommentNodeList(NodeList nodeList)
    {
        if (((nodeList == null) || (GoRuntime.Len(nodeList.Nodes) == 0)))
        {
            return false;
        }

        var parent = nodeList.Nodes[0].Parent;
        if ((parent == null))
        {
            return false;
        }

        return (IsJSDocSingleCommentNode(parent) && (nodeList == parent.CommentList()));
    }

    // See `IsJSDocSingleCommentNode`.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3187:1.
    public static bool IsJSDocSingleCommentNodeComment(Node node)
    {
        if (((node == null) || (node.Parent == null)))
        {
            return false;
        }

        return (IsJSDocSingleCommentNode(node.Parent) && (node == node.Parent.CommentList().Nodes[0]));
    }

    // In Strada, if a JSDoc node has a single comment, that comment is represented as a string property
    // as a simplification, and therefore that comment is not visited by `forEachChild`.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3196:1.
    public static bool IsJSDocSingleCommentNode(Node node)
    {
        return ((hasComment(node.Kind) && (node.CommentList() != null)) && (GoRuntime.Len(node.CommentList().Nodes) == 1));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3200:1.
    public static bool IsValidTypeOnlyAliasUseSite(Node useSite)
    {
        return ((((((useSite.Flags & ((NodeFlags.Ambient | NodeFlags.JSDoc))) != 0) || IsPartOfTypeQuery(useSite)) || isIdentifierInNonEmittingHeritageClause(useSite)) || isPartOfPossiblyValidTypeOrAbstractComputedPropertyName(useSite)) || !((IsExpressionNode(useSite) || isShorthandPropertyNameUseSite(useSite))));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3208:1.
    private static bool isIdentifierInNonEmittingHeritageClause(Node node)
    {
        if (!IsIdentifier(node))
        {
            return false;
        }

        var parent = node.Parent;
        while ((IsPropertyAccessExpression(parent) || IsExpressionWithTypeArguments(parent)))
        {
            parent = parent.Parent;
        }

        return (IsHeritageClause(parent) && (((parent.AsHeritageClause().Token == Kind.ImplementsKeyword) || IsInterfaceDeclaration(parent.Parent))));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3219:1.
    private static bool isPartOfPossiblyValidTypeOrAbstractComputedPropertyName(Node node)
    {
        while (NodeKindIs(node, Kind.Identifier, Kind.PropertyAccessExpression))
        {
            node = node.Parent;
        }

        if ((node.Kind != Kind.ComputedPropertyName))
        {
            return false;
        }

        if (HasSyntacticModifier(node.Parent, ModifierFlags.Abstract))
        {
            return true;
        }

        return NodeKindIs(node.Parent.Parent, Kind.InterfaceDeclaration, Kind.TypeLiteral);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3232:1.
    private static bool isShorthandPropertyNameUseSite(Node useSite)
    {
        return ((IsIdentifier(useSite) && IsShorthandPropertyAssignment(useSite.Parent)) && (useSite.Parent.AsShorthandPropertyAssignment().Name() == useSite));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3258:1.
    public static bool IsPartOfTypeOnlyImportOrExportDeclaration(Node node)
    {
        return (FindAncestor(node, IsTypeOnlyImportOrExportDeclaration) != null);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3262:1.
    public static bool IsEmittableImport(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            {
                return ((node.ImportClause() != null) && !node.ImportClause().IsTypeOnly());
            }

            case Kind.ExportDeclaration:
            case Kind.ImportEqualsDeclaration:
            {
                return !node.IsTypeOnly();
            }

            case Kind.CallExpression:
            {
                return IsImportCall(node);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3274:1.
    public static bool IsResolutionModeOverrideHost(Node node)
    {
        if ((node == null))
        {
            return false;
        }

        switch (node.Kind)
        {
            case Kind.ImportType:
            case Kind.ExportDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3285:1.
    public static bool HasResolutionModeOverride(Node node)
    {
        if ((node == null))
        {
            return false;
        }

        Node attributes = default;
        switch (node.Kind)
        {
            case Kind.ImportType:
            {
                attributes = node.AsImportTypeNode().Attributes;
                break;
            }

            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                attributes = node.AsImportDeclaration().Attributes;
                break;
            }

            case Kind.ExportDeclaration:
            {
                attributes = node.AsExportDeclaration().Attributes;
                break;
            }
        }

        if ((attributes != null))
        {
            var tuple2 = attributes.GetResolutionModeOverride(null);
            var ok = tuple2.Item2;
            return ok;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3305:1.
    public static bool IsStringTextContainingNode(Node node)
    {
        return ((node.Kind == Kind.StringLiteral) || IsTemplateLiteralKind(node.Kind));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3309:1.
    public static bool IsTemplateLiteralKind(Kind kind)
    {
        return ((Kind.FirstTemplateToken <= kind) && (kind <= Kind.LastTemplateToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3313:1.
    public static bool IsTemplateLiteralToken(Node node)
    {
        return IsTemplateLiteralKind(node.Kind);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3621:1.
    public static bool IsLateVisibilityPaintedStatement(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.VariableStatement:
            case Kind.ClassDeclaration:
            case Kind.FunctionDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.EnumDeclaration:
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3640:1.
    public static bool IsExternalModuleAugmentation(Node node)
    {
        return (IsAmbientModule(node) && IsModuleAugmentationExternal(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3677:1.
    public static bool IsRightSideOfPropertyAccess(Node node)
    {
        return ((node.Parent.Kind == Kind.PropertyAccessExpression) && (node.Parent.Name() == node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3681:1.
    public static bool IsArgumentExpressionOfElementAccess(Node node)
    {
        return (((node.Parent != null) && (node.Parent.Kind == Kind.ElementAccessExpression)) && (node.Parent.AsElementAccessExpression().ArgumentExpression == node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3685:1.
    public static Node ClimbPastPropertyAccess(Node node)
    {
        if (IsRightSideOfPropertyAccess(node))
        {
            return node.Parent;
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3692:1.
    private static Node climbPastPropertyOrElementAccess(Node node)
    {
        if ((IsRightSideOfPropertyAccess(node) || IsArgumentExpressionOfElementAccess(node)))
        {
            return node.Parent;
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3699:1.
    private static Node selectExpressionOfCallOrNewExpressionOrDecorator(Node node)
    {
        if (((IsCallExpression(node) || IsNewExpression(node)) || IsDecorator(node)))
        {
            return node.Expression();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3706:1.
    private static Node selectTagOfTaggedTemplateExpression(Node node)
    {
        if (IsTaggedTemplateExpression(node))
        {
            return node.AsTaggedTemplateExpression().Tag;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3713:1.
    private static Node selectTagNameOfJsxOpeningLikeElement(Node node)
    {
        if ((IsJsxOpeningElement(node) || IsJsxSelfClosingElement(node)))
        {
            return node.TagName();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3720:1.
    public static bool IsCallExpressionTarget(Node node, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        return isCalleeWorker(node, IsCallExpression, selectExpressionOfCallOrNewExpressionOrDecorator, includeElementAccess, skipPastOuterExpressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3724:1.
    public static bool IsNewExpressionTarget(Node node, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        return isCalleeWorker(node, IsNewExpression, selectExpressionOfCallOrNewExpressionOrDecorator, includeElementAccess, skipPastOuterExpressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3728:1.
    public static bool IsCallOrNewExpressionTarget(Node node, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        return isCalleeWorker(node, IsCallOrNewExpression, selectExpressionOfCallOrNewExpressionOrDecorator, includeElementAccess, skipPastOuterExpressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3732:1.
    public static bool IsTaggedTemplateTag(Node node, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        return isCalleeWorker(node, IsTaggedTemplateExpression, selectTagOfTaggedTemplateExpression, includeElementAccess, skipPastOuterExpressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3736:1.
    public static bool IsDecoratorTarget(Node node, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        return isCalleeWorker(node, IsDecorator, selectExpressionOfCallOrNewExpressionOrDecorator, includeElementAccess, skipPastOuterExpressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3740:1.
    public static bool IsJsxOpeningLikeElementTagName(Node node, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        return isCalleeWorker(node, IsJsxOpeningLikeElement, selectTagNameOfJsxOpeningLikeElement, includeElementAccess, skipPastOuterExpressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3744:1.
    private static bool isCalleeWorker(Node node, Func<Node, bool> pred, Func<Node, Node> calleeSelector, bool includeElementAccess, bool skipPastOuterExpressions)
    {
        Node target = default;
        if (includeElementAccess)
        {
            target = climbPastPropertyOrElementAccess(node);
        }
        else
        {
            target = ClimbPastPropertyAccess(node);
        }

        if (skipPastOuterExpressions)
        {
            if (IsExpression(target))
            {
                target = SkipOuterExpressions(target, OuterExpressionKinds.All);
            }
        }

        return ((((target != null) && (target.Parent != null)) && pred(target.Parent)) && (calleeSelector(target.Parent) == target));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3810:1.
    public static bool IsCallOrNewExpression(Node node)
    {
        return (IsCallExpression(node) || IsNewExpression(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3826:1.
    public static bool IsUnterminatedLiteral(Node node)
    {
        return ((IsLiteralKind(node.Kind) && ((node.LiteralLikeData().TokenFlags & TokenFlags.Unterminated) != 0)) || (IsTemplateLiteralKind(node.Kind) && ((node.TemplateLiteralLikeData().TemplateFlags & TokenFlags.Unterminated) != 0)));
    }

    // Gets a value indicating whether a class element is either a static or an instance property declaration with an initializer.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3832:1.
    public static bool IsInitializedProperty(Node member)
    {
        return ((member.Kind == Kind.PropertyDeclaration) && (member.Initializer() != null));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3837:1.
    public static bool IsTrivia(Kind token)
    {
        return ((Kind.FirstTriviaToken <= token) && (token <= Kind.LastTriviaToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3841:1.
    public static bool HasDecorators(Node node)
    {
        return HasSyntacticModifier(node, ModifierFlags.Decorator);
    }

    // Returns true if the node kind has a comment property.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3879:1.
    private static bool hasComment(Kind kind)
    {
        switch (kind)
        {
            case Kind.JSDoc:
            case Kind.JSDocUnknownTag:
            case Kind.JSDocAugmentsTag:
            case Kind.JSDocImplementsTag:
            case Kind.JSDocDeprecatedTag:
            case Kind.JSDocPublicTag:
            case Kind.JSDocPrivateTag:
            case Kind.JSDocProtectedTag:
            case Kind.JSDocReadonlyTag:
            case Kind.JSDocOverrideTag:
            case Kind.JSDocCallbackTag:
            case Kind.JSDocOverloadTag:
            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            case Kind.JSDocReturnTag:
            case Kind.JSDocThisTag:
            case Kind.JSDocTypeTag:
            case Kind.JSDocTemplateTag:
            case Kind.JSDocTypedefTag:
            case Kind.JSDocSeeTag:
            case Kind.JSDocThrowsTag:
            case Kind.JSDocSatisfiesTag:
            case Kind.JSDocImportTag:
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4069:1.
    public static bool IsEmptyObjectLiteral(Node expression)
    {
        return (IsObjectLiteralExpression(expression) && (GoRuntime.Len(expression.Properties()) == 0));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4073:1.
    public static bool IsEmptyArrayLiteral(Node expression)
    {
        return (IsArrayLiteralExpression(expression) && (GoRuntime.Len(expression.Elements()) == 0));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4089:1.
    public static bool IsJSDocNameReferenceContext(Node node)
    {
        return (((node.Flags & NodeFlags.JSDoc) != 0) && (FindAncestor(node, (node2) =>
        {
            return (IsJSDocNameReference(node2) || IsJSDocLinkLike(node2));
        }) != null));
    }

    // GetJSDocRoot returns the containing JSDoc node for a node inside a JSDoc comment.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4096:1.
    public static Node GetJSDocRoot(Node node)
    {
        return FindAncestor(node.Parent, (n) =>
        {
            return (n.Kind == Kind.JSDoc);
        });
    }

    // GetJSDocHost returns the declaration that the JSDoc comment containing the given node is attached to.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4103:1.
    public static Node GetJSDocHost(Node node)
    {
        var jsDoc = GetJSDocRoot(node);
        if ((jsDoc == null))
        {
            return null;
        }

        return jsDoc.Parent;
    }

    // GetHostSignatureFromJSDoc returns the function-like declaration that hosts the JSDoc comment
    // containing the given node. This is used to resolve @link references to parameters.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4113:1.
    public static Node GetHostSignatureFromJSDoc(Node node)
    {
        var host = GetJSDocHost(node);
        if ((host == null))
        {
            return null;
        }

        if (((IsPropertySignatureDeclaration(host) && (host.Type() != null)) && IsFunctionLike(host.Type())))
        {
            return host.Type();
        }

        if (IsFunctionLike(host))
        {
            return host;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4146:1.
    public static bool IsImportOrImportEqualsDeclaration(Node node)
    {
        return (IsImportDeclaration(node) || IsImportEqualsDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4201:1.
    public static bool IsNonContextualKeyword(Kind token)
    {
        return (IsKeyword(token) && !IsContextualKeyword(token));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4205:1.
    public static bool HasModifier(Node node, ModifierFlags flags)
    {
        return ((node.ModifierFlags() & flags) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4269:1.
    public static bool HasContextSensitiveParameters(Node node)
    {
        if ((node.TypeParameters().IsNil))
        {
            if (Core.Some(node.Parameters(), (p) =>
            {
                return (p.Type() == null);
            }))
            {
                return true;
            }

            if (!IsArrowFunction(node))
            {
                var parameter = Core.FirstOrNil(node.Parameters());
                if (((parameter == null) || !IsThisParameter(parameter)))
                {
                    return ((node.Flags & NodeFlags.ContainsThis) != 0);
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4288:1.
    public static bool IsInfinityOrNaNString(string name)
    {
        return (((name == "Infinity") || (name == "-Infinity")) || (name == "NaN"));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4319:1.
    public static bool HasAbstractModifier(Node node)
    {
        return HasSyntacticModifier(node, ModifierFlags.Abstract);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4323:1.
    public static bool HasAmbientModifier(Node node)
    {
        return HasSyntacticModifier(node, ModifierFlags.Ambient);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4615:1.
    public static bool IsExpandoPropertyDeclaration(Node node)
    {
        return ((node != null) && IsBinaryExpression(node));
    }

    // IsSuperProperty checks if a node is super.x or super[x].
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4620:1.
    public static bool IsSuperProperty(Node node)
    {
        return (((IsPropertyAccessExpression(node) || IsElementAccessExpression(node))) && (node.Expression().Kind == Kind.SuperKeyword));
    }

    // Indicates whether a node is a potential source of an assigned name for a class, function, or arrow function.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4626:1.
    public static bool IsNamedEvaluationSource(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAssignment:
            {
                return !IsProtoSetter(node.AsPropertyAssignment().Name());
            }

            case Kind.ShorthandPropertyAssignment:
            {
                return (node.AsShorthandPropertyAssignment().ObjectAssignmentInitializer != null);
            }

            case Kind.VariableDeclaration:
            {
                return (IsIdentifier(node.AsVariableDeclaration().Name()) && (node.Initializer() != null));
            }

            case Kind.Parameter:
            {
                return ((IsIdentifier(node.AsParameterDeclaration().Name()) && (node.Initializer() != null)) && (node.AsParameterDeclaration().DotDotDotToken == null));
            }

            case Kind.BindingElement:
            {
                return ((IsIdentifier(node.AsBindingElement().Name()) && (node.Initializer() != null)) && (node.AsBindingElement().DotDotDotToken == null));
            }

            case Kind.PropertyDeclaration:
            {
                return (node.Initializer() != null);
            }

            case Kind.BinaryExpression:
            {
                switch (node.AsBinaryExpression().OperatorToken.Kind)
                {
                    case Kind.EqualsToken:
                    case Kind.AmpersandAmpersandEqualsToken:
                    case Kind.BarBarEqualsToken:
                    case Kind.QuestionQuestionEqualsToken:
                    {
                        return IsIdentifier(node.AsBinaryExpression().Left);
                    }
                }

                break;
            }

            case Kind.ExportAssignment:
            {
                return true;
            }
        }

        return false;
    }

    // Indicates whether a property name is the special `__proto__` property.
    // Per the ECMA-262 spec, this only matters for property assignments whose name is
    // the Identifier `__proto__`, or the string literal `"__proto__"`, but not for
    // computed property names.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4655:1.
    public static bool IsProtoSetter(Node node)
    {
        return (((IsIdentifier(node) || IsStringLiteral(node))) && (node.TextEquals("__proto__"u8)));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4659:1.
    public static bool IsStringLiteralLikeType(Node node)
    {
        return ((node.Kind == Kind.LiteralType) && IsStringLiteralLike(node.AsLiteralTypeNode().Literal));
    }
}

// END FOUNDATION SIMPLE SYNTAX HELPERS
// BEGIN FOUNDATION SYNTAX ALGORITHMS
// Foundation syntax fragments from pinned ast/utilities.go. Reviewed C# source is authoritative.
public static partial class Ast
{
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:80:1.
    public static bool RangeIsSynthesized(TextRange loc)
    {
        return (PositionIsSynthesized(loc.Pos()) || PositionIsSynthesized(loc.End()));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:89:1.
    public static Node FindLastVisibleNode(Slice<Node> nodes)
    {
        var fromEnd = 1;
        while (((fromEnd <= GoRuntime.Len(nodes)) && ((nodes[(GoRuntime.Len(nodes) - fromEnd)].Flags & NodeFlags.Reparsed) != 0)))
        {
            fromEnd++;
        }

        if ((fromEnd <= GoRuntime.Len(nodes)))
        {
            return nodes[(GoRuntime.Len(nodes) - fromEnd)];
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:833:1.
    public static Node SkipTypeParentheses(Node node)
    {
        while (IsParenthesizedTypeNode(node))
        {
            node = node.Type();
        }

        return node;
    }

    // Walks up the parents of a parenthesized expression to find the containing node
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:845:1.
    public static Node WalkUpParenthesizedExpressions(Node node)
    {
        while (((node != null) && (node.Kind == Kind.ParenthesizedExpression)))
        {
            node = node.Parent;
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:959:1.
    public static FindAncestorResult ToFindAncestorResult(bool b)
    {
        if (b)
        {
            return FindAncestorResult.True;
        }

        return FindAncestorResult.False;
    }

    // Walks up the parents of a node to find the ancestor that matches the callback
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:967:1.
    public static Node FindAncestorOrQuit(Node node, Func<Node, FindAncestorResult> callback)
    {
        while ((node != null))
        {
            switch (callback(node))
            {
                case FindAncestorResult.Quit:
                {
                    return null;
                }

                case FindAncestorResult.True:
                {
                    return node;
                }
            }

            node = node.Parent;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1100:1.
    public static bool CanHaveModifiers(Node node)
    {
        switch (node.Kind)
        {
            case Kind.TypeParameter:
            case Kind.Parameter:
            case Kind.PropertySignature:
            case Kind.PropertyDeclaration:
            case Kind.MethodSignature:
            case Kind.MethodDeclaration:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.IndexSignature:
            case Kind.ConstructorType:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.ClassExpression:
            case Kind.VariableStatement:
            case Kind.FunctionDeclaration:
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.EnumDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportAssignment:
            case Kind.ExportDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Warning: This has the same semantics as the forEach family of functions in that traversal terminates
    // in the event that 'visitor' returns true.
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1157:1.
    public static bool ForEachReturnStatement(Node body, Func<Node, bool> visitor)
    {
        Func<Node, bool> traverse = default;
        traverse = (node) =>
        {
            switch (node.Kind)
            {
                case Kind.ReturnStatement:
                {
                    return visitor(node);
                }

                case Kind.CaseBlock:
                case Kind.Block:
                case Kind.IfStatement:
                case Kind.DoStatement:
                case Kind.WhileStatement:
                case Kind.ForStatement:
                case Kind.ForInStatement:
                case Kind.ForOfStatement:
                case Kind.WithStatement:
                case Kind.SwitchStatement:
                case Kind.CaseClause:
                case Kind.DefaultClause:
                case Kind.LabeledStatement:
                case Kind.TryStatement:
                case Kind.CatchClause:
                {
                    return node.ForEachChild(child => traverse(child));
                }
            }

            return false;
        };
        return traverse(body);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1286:1.
    public static Node WalkUpBindingElementsAndPatterns(Node binding)
    {
        var node = binding.Parent;
        while (IsBindingElement(node.Parent))
        {
            node = node.Parent.Parent;
        }

        return node.Parent;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1423:1.
    public static Node GetInitializerOfBinaryExpression(BinaryExpression expr)
    {
        while (IsBinaryExpression(expr.Right))
        {
            expr = expr.Right.AsBinaryExpression();
        }

        return expr.Right.Expression();
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1746:1.
    public static Slice<Node> GetImplementsHeritageClauseElements(Node node)
    {
        return GetHeritageElements(node, Kind.ImplementsKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1864:1.
    public static Node GetSuperContainer(Node node, bool stopOnFunctions)
    {
        {
            node = node.Parent;
            for (; (node != null); node = node.Parent)
            {
                switch (node.Kind)
                {
                    case Kind.ComputedPropertyName:
                    {
                        node = node.Parent;
                        break;
                    }

                    case Kind.FunctionDeclaration:
                    case Kind.FunctionExpression:
                    case Kind.ArrowFunction:
                    {
                        if (!stopOnFunctions)
                        {
                            continue;
                        }

                        return node;
                    }

                    case Kind.PropertyDeclaration:
                    case Kind.PropertySignature:
                    case Kind.MethodDeclaration:
                    case Kind.MethodSignature:
                    case Kind.Constructor:
                    case Kind.GetAccessor:
                    case Kind.SetAccessor:
                    case Kind.ClassStaticBlockDeclaration:
                    {
                        return node;
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
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:1972:1.
    public static Node GetImportAttributes(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                return node.AsImportDeclaration().Attributes;
            }

            case Kind.ExportDeclaration:
            {
                return node.AsExportDeclaration().Attributes;
            }

            case Kind.ImportType:
            {
                return node.AsImportTypeNode().Attributes;
            }
        }

        throw new InvalidOperationException(("Unhandled case in getImportAttributes: " + node.Kind.String()));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2208:1.
    public static string GetTextOfPropertyName(Node name)
    {
        var tuple1 = TryGetTextOfPropertyName(name);
        var text = tuple1.Item1;
        return text;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2213:1.
    public static (string, bool) TryGetTextOfPropertyName(Node name)
    {
        switch (name.Kind)
        {
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            {
                return (name.Text(), true);
            }

            case Kind.ComputedPropertyName:
            {
                if (IsStringOrNumericLiteralLike(name.Expression()))
                {
                    return (name.Expression().Text(), true);
                }

                break;
            }

            case Kind.JsxNamespacedName:
            {
                return (((name.AsJsxNamespacedName().Namespace.Text() + ":") + name.Name().Text()), true);
            }
        }

        return ("", false);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2240:1.
    public static Node GetNewTargetContainer(Node node)
    {
        var container = GetThisContainer(node, false, false);
        if ((container != null))
        {
            switch (container.Kind)
            {
                case Kind.Constructor:
                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                {
                    return container;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2281:1.
    public static SemanticMeaning GetMeaningFromDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            {
                return SemanticMeaning.Value;
            }

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
            {
                return SemanticMeaning.Value;
            }

            case Kind.TypeParameter:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.TypeLiteral:
            {
                return SemanticMeaning.Type;
            }

            case Kind.EnumMember:
            case Kind.ClassDeclaration:
            {
                return (SemanticMeaning.Value | SemanticMeaning.Type);
            }

            case Kind.ModuleDeclaration:
            {
                if (IsAmbientModule(node))
                {
                    return (SemanticMeaning.Namespace | SemanticMeaning.Value);
                }
                else
                {
                    if ((GetModuleInstanceState(node) == ModuleInstanceState.Instantiated))
                    {
                        return (SemanticMeaning.Namespace | SemanticMeaning.Value);
                    }
                    else
                    {
                        return SemanticMeaning.Namespace;
                    }
                }
            }

            case Kind.EnumDeclaration:
            case Kind.NamedImports:
            case Kind.ImportSpecifier:
            case Kind.ImportEqualsDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportAssignment:
            case Kind.ExportDeclaration:
            {
                return SemanticMeaning.All;
            }

            case Kind.SourceFile:
            {
                return (SemanticMeaning.Namespace | SemanticMeaning.Value);
            }
        }

        return SemanticMeaning.All;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2523:1.
    public static bool IsInstantiatedModule(Node node, bool preserveConstEnums)
    {
        var moduleState = GetModuleInstanceState(node);
        return ((moduleState == ModuleInstanceState.Instantiated) || ((preserveConstEnums && (moduleState == ModuleInstanceState.ConstEnumOnly))));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2599:1.
    public static Node GetNamespaceDeclarationNode(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                var importClause = node.ImportClause();
                if ((((importClause != null) && (importClause.AsImportClause().NamedBindings != null)) && IsNamespaceImport(importClause.AsImportClause().NamedBindings)))
                {
                    return importClause.AsImportClause().NamedBindings;
                }

                break;
            }

            case Kind.ImportEqualsDeclaration:
            {
                return node;
            }

            case Kind.ExportDeclaration:
            {
                var exportClause = node.AsExportDeclaration().ExportClause;
                if (((exportClause != null) && IsNamespaceExport(exportClause)))
                {
                    return exportClause;
                }

                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in getNamespaceDeclarationNode");
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2881:1.
    public static Pragma GetPragmaFromSourceFile(SourceFile file, string name)
    {
        Pragma result = default;
        if ((file != null))
        {
            {
                foreach (var i in GoRuntime.RangeKeys(file.Pragmas))
                {
                    if ((file.Pragmas[i].Name == name))
                    {
                        result = file.Pragmas[i];
                    }
                }
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2893:1.
    public static string GetPragmaArgument(Pragma pragma, string name)
    {
        if ((pragma != null))
        {
            {
                if (pragma.Args != null && pragma.Args.TryGetValue(name, out var arg))
                {
                    return arg.Value;
                }
            }
        }

        return "";
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:2936:1.
    public static Node GetModuleSpecifierOfBareOrAccessedRequire(Node node)
    {
        if (isVariableDeclarationInitializedWithRequireHelper(node, false))
        {
            return node.Initializer().Arguments()[0];
        }

        if (isVariableDeclarationInitializedWithRequireHelper(node, true))
        {
            var leftmost = GetLeftmostAccessExpression(node.Initializer());
            if (IsRequireCall(leftmost, true))
            {
                return leftmost.Arguments()[0];
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3087:1.
    public static bool ForEachChildAndJSDoc(Node node, SourceFile sourceFile, Visitor v)
    {
        if (VisitNodes(v, node.JSDoc(sourceFile)))
        {
            return true;
        }

        return node.ForEachChild(v);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3137:1.
    public static Node GetTypeAnnotationNode(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.PropertySignature:
            case Kind.PropertyDeclaration:
            case Kind.TypePredicate:
            case Kind.ParenthesizedType:
            case Kind.TypeOperator:
            case Kind.MappedType:
            case Kind.TypeAssertionExpression:
            case Kind.AsExpression:
            case Kind.SatisfiesExpression:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.NamedTupleMember:
            case Kind.OptionalType:
            case Kind.RestType:
            case Kind.TemplateLiteralTypeSpan:
            case Kind.JSDocTypeExpression:
            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            case Kind.JSDocNullableType:
            case Kind.JSDocNonNullableType:
            case Kind.JSDocOptionalType:
            {
                return node.Type();
            }

            default:
            {
                var funcLike = node.FunctionLikeData();
                if ((funcLike != null))
                {
                    return funcLike.Type;
                }

                return null;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3162:1.
    public static Node GetClassExtendsHeritageElement(Node node)
    {
        var heritageElements = GetHeritageElements(node, Kind.ExtendsKeyword);
        if ((GoRuntime.Len(heritageElements) > 0))
        {
            return heritageElements[0];
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3317:1.
    public static Node GetExternalModuleImportEqualsDeclarationExpression(Node node)
    {
        Debug.Assert(IsExternalModuleImportEqualsDeclaration(node));
        return node.AsImportEqualsDeclaration().ModuleReference.Expression();
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3322:1.
    public static Slice<Node> CreateModifiersFromModifierFlags(ModifierFlags flags, Func<Kind, Node> createModifier)
    {
        Slice<Node> result = default;
        if (((flags & ModifierFlags.Export) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.ExportKeyword));
        }

        if (((flags & ModifierFlags.Ambient) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.DeclareKeyword));
        }

        if (((flags & ModifierFlags.Default) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.DefaultKeyword));
        }

        if (((flags & ModifierFlags.Const) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.ConstKeyword));
        }

        if (((flags & ModifierFlags.Public) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.PublicKeyword));
        }

        if (((flags & ModifierFlags.Private) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.PrivateKeyword));
        }

        if (((flags & ModifierFlags.Protected) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.ProtectedKeyword));
        }

        if (((flags & ModifierFlags.Abstract) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.AbstractKeyword));
        }

        if (((flags & ModifierFlags.Static) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.StaticKeyword));
        }

        if (((flags & ModifierFlags.Override) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.OverrideKeyword));
        }

        if (((flags & ModifierFlags.Readonly) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.ReadonlyKeyword));
        }

        if (((flags & ModifierFlags.Accessor) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.AccessorKeyword));
        }

        if (((flags & ModifierFlags.Async) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.AsyncKeyword));
        }

        if (((flags & ModifierFlags.In) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.InKeyword));
        }

        if (((flags & ModifierFlags.Out) != 0))
        {
            result = GoRuntime.Append(result, createModifier(Kind.OutKeyword));
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3372:1.
    public static Node GetThisParameter(Node signature)
    {
        if ((GoRuntime.Len(signature.Parameters()) != 0))
        {
            var thisParameter = signature.Parameters()[0];
            if (IsThisParameter(thisParameter))
            {
                return thisParameter;
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3383:1.
    public static Node ReplaceModifiers(NodeFactory factory, Node node, ModifierList modifierArray)
    {
        switch (node.Kind)
        {
            case Kind.TypeParameter:
            {
                return factory.UpdateTypeParameterDeclaration(node.AsTypeParameterDeclaration(), modifierArray, node.Name(), node.AsTypeParameterDeclaration().Constraint, node.AsTypeParameterDeclaration().Expression, node.AsTypeParameterDeclaration().DefaultType);
            }

            case Kind.Parameter:
            {
                return factory.UpdateParameterDeclaration(node.AsParameterDeclaration(), modifierArray, node.AsParameterDeclaration().DotDotDotToken, node.Name(), node.QuestionToken(), node.Type(), node.Initializer());
            }

            case Kind.ConstructorType:
            {
                return factory.UpdateConstructorTypeNode(node.AsConstructorTypeNode(), modifierArray, node.TypeParameterList(), node.ParameterList(), node.Type());
            }

            case Kind.PropertySignature:
            {
                return factory.UpdatePropertySignatureDeclaration(node.AsPropertySignatureDeclaration(), modifierArray, node.Name(), node.PostfixToken(), node.Type(), node.Initializer());
            }

            case Kind.PropertyDeclaration:
            {
                return factory.UpdatePropertyDeclaration(node.AsPropertyDeclaration(), modifierArray, node.Name(), node.PostfixToken(), node.Type(), node.Initializer());
            }

            case Kind.MethodSignature:
            {
                return factory.UpdateMethodSignatureDeclaration(node.AsMethodSignatureDeclaration(), modifierArray, node.Name(), node.PostfixToken(), node.TypeParameterList(), node.ParameterList(), node.Type());
            }

            case Kind.MethodDeclaration:
            {
                return factory.UpdateMethodDeclaration(node.AsMethodDeclaration(), modifierArray, node.AsMethodDeclaration().AsteriskToken, node.Name(), node.PostfixToken(), node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsMethodDeclaration().FullSignature, node.Body());
            }

            case Kind.Constructor:
            {
                return factory.UpdateConstructorDeclaration(node.AsConstructorDeclaration(), modifierArray, node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsConstructorDeclaration().FullSignature, node.Body());
            }

            case Kind.GetAccessor:
            {
                return factory.UpdateGetAccessorDeclaration(node.AsGetAccessorDeclaration(), modifierArray, node.Name(), node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsGetAccessorDeclaration().FullSignature, node.Body());
            }

            case Kind.SetAccessor:
            {
                return factory.UpdateSetAccessorDeclaration(node.AsSetAccessorDeclaration(), modifierArray, node.Name(), node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsSetAccessorDeclaration().FullSignature, node.Body());
            }

            case Kind.IndexSignature:
            {
                return factory.UpdateIndexSignatureDeclaration(node.AsIndexSignatureDeclaration(), modifierArray, node.ParameterList(), node.Type());
            }

            case Kind.FunctionExpression:
            {
                return factory.UpdateFunctionExpression(node.AsFunctionExpression(), modifierArray, node.AsFunctionExpression().AsteriskToken, node.Name(), node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsFunctionExpression().FullSignature, node.Body());
            }

            case Kind.ArrowFunction:
            {
                return factory.UpdateArrowFunction(node.AsArrowFunction(), modifierArray, node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsArrowFunction().FullSignature, node.AsArrowFunction().EqualsGreaterThanToken, node.Body());
            }

            case Kind.ClassExpression:
            {
                return factory.UpdateClassExpression(node.AsClassExpression(), modifierArray, node.Name(), node.TypeParameterList(), node.AsClassExpression().HeritageClauses, node.MemberList());
            }

            case Kind.VariableStatement:
            {
                return factory.UpdateVariableStatement(node.AsVariableStatement(), modifierArray, node.AsVariableStatement().DeclarationList);
            }

            case Kind.FunctionDeclaration:
            {
                return factory.UpdateFunctionDeclaration(node.AsFunctionDeclaration(), modifierArray, node.AsFunctionDeclaration().AsteriskToken, node.Name(), node.TypeParameterList(), node.ParameterList(), node.Type(), node.AsFunctionDeclaration().FullSignature, node.Body());
            }

            case Kind.ClassDeclaration:
            {
                return factory.UpdateClassDeclaration(node.AsClassDeclaration(), modifierArray, node.Name(), node.TypeParameterList(), node.AsClassDeclaration().HeritageClauses, node.MemberList());
            }

            case Kind.InterfaceDeclaration:
            {
                return factory.UpdateInterfaceDeclaration(node.AsInterfaceDeclaration(), modifierArray, node.Name(), node.TypeParameterList(), node.AsInterfaceDeclaration().HeritageClauses, node.MemberList());
            }

            case Kind.TypeAliasDeclaration:
            {
                return factory.UpdateTypeAliasDeclaration(node.AsTypeAliasDeclaration(), modifierArray, node.Name(), node.TypeParameterList(), node.Type());
            }

            case Kind.EnumDeclaration:
            {
                return factory.UpdateEnumDeclaration(node.AsEnumDeclaration(), modifierArray, node.Name(), node.MemberList());
            }

            case Kind.ModuleDeclaration:
            {
                return factory.UpdateModuleDeclaration(node.AsModuleDeclaration(), modifierArray, node.AsModuleDeclaration().Keyword, node.Name(), node.Attributes(), node.Body());
            }

            case Kind.ImportEqualsDeclaration:
            {
                return factory.UpdateImportEqualsDeclaration(node.AsImportEqualsDeclaration(), modifierArray, node.IsTypeOnly(), node.Name(), node.AsImportEqualsDeclaration().ModuleReference);
            }

            case Kind.ImportDeclaration:
            {
                return factory.UpdateImportDeclaration(node.AsImportDeclaration(), modifierArray, node.ImportClause(), node.ModuleSpecifier(), node.AsImportDeclaration().Attributes);
            }

            case Kind.ExportAssignment:
            {
                return factory.UpdateExportAssignment(node.AsExportAssignment(), modifierArray, node.AsExportAssignment().IsExportEquals, node.Type(), node.Expression());
            }

            case Kind.ExportDeclaration:
            {
                return factory.UpdateExportDeclaration(node.AsExportDeclaration(), modifierArray, node.IsTypeOnly(), node.AsExportDeclaration().ExportClause, node.ModuleSpecifier(), node.AsExportDeclaration().Attributes);
            }
        }

        throw new InvalidOperationException($"Node that does not have modifiers tried to have modifier replaced: {(int)node.Kind}");
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3795:1.
    public static Node GetInvokedExpression(Node node)
    {
        switch (node.Kind)
        {
            case Kind.TaggedTemplateExpression:
            {
                return node.AsTaggedTemplateExpression().Tag;
            }

            case Kind.JsxOpeningElement:
            case Kind.JsxSelfClosingElement:
            {
                return node.TagName();
            }

            case Kind.BinaryExpression:
            {
                return node.AsBinaryExpression().Right;
            }

            case Kind.JsxOpeningFragment:
            {
                return node;
            }

            default:
            {
                return node.Expression();
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3814:1.
    public static int IndexOfNode(Slice<Node> nodes, Node node)
    {
        var tuple3 = GoSlices.BinarySearchFunc(nodes, node, CompareNodePositions);
        var index = tuple3.Item1;
        var ok = tuple3.Item2;
        if (ok)
        {
            return index;
        }

        return -1;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3865:1.
    public static Slice<Node> GetSemanticJsxChildren(Slice<Node> children)
    {
        return Core.Filter(children, (i) =>
        {
            switch (i.Kind)
            {
                case Kind.JsxExpression:
                {
                    return (i.Expression() != null);
                }

                case Kind.JsxText:
                {
                    return !i.AsJsxText().ContainsOnlyTriviaWhiteSpaces;
                }

                default:
                {
                    return true;
                }
            }
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:3992:1.
    public static Node TryGetPropertyNameOfBindingOrAssignmentElement(Node bindingElement)
    {
        switch (bindingElement.Kind)
        {
            case Kind.BindingElement:
            {
                if ((bindingElement.PropertyName() != null))
                {
                    var propertyName = bindingElement.PropertyName();
                    if ((IsComputedPropertyName(propertyName) && IsStringOrNumericLiteralLike(propertyName.Expression())))
                    {
                        return propertyName.Expression();
                    }

                    return propertyName;
                }

                break;
            }

            case Kind.PropertyAssignment:
            {
                if ((bindingElement.Name() != null))
                {
                    var propertyName2 = bindingElement.Name();
                    if ((IsComputedPropertyName(propertyName2) && IsStringOrNumericLiteralLike(propertyName2.Expression())))
                    {
                        return propertyName2.Expression();
                    }

                    return propertyName2;
                }

                break;
            }

            case Kind.SpreadAssignment:
            {
                return bindingElement.Name();
            }
        }

        var target = GetTargetOfBindingOrAssignmentElement(bindingElement);
        if (((target != null) && IsPropertyName(target)))
        {
            return target;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4077:1.
    public static Node GetRestIndicatorOfBindingOrAssignmentElement(Node bindingElement)
    {
        switch (bindingElement.Kind)
        {
            case Kind.Parameter:
            {
                return bindingElement.AsParameterDeclaration().DotDotDotToken;
            }

            case Kind.BindingElement:
            {
                return bindingElement.AsBindingElement().DotDotDotToken;
            }

            case Kind.SpreadElement:
            case Kind.SpreadAssignment:
            {
                return bindingElement;
            }
        }

        return null;
    }

    // Finds the declaration that owns the JSDoc for a function-like node.
    // Keep these hosts aligned with JSDoc parameter reparsing so unmatched @param diagnostics use the same attachment rules.
    // Keep in sync with getNextJSDocCommentLocation in the API's src/ast/jsdoc.ts
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4131:1.
    public static Node GetNextJSDocCommentLocation(Node node)
    {
        {
            var parent = node.Parent;
            if ((parent != null))
            {
                switch (parent.Kind)
                {
                    case Kind.PropertyAssignment:
                    case Kind.ExportAssignment:
                    case Kind.PropertyDeclaration:
                    case Kind.VariableDeclaration:
                    case Kind.SatisfiesExpression:
                    case Kind.ReturnStatement:
                    case Kind.VariableStatement:
                    case Kind.ExpressionStatement:
                    {
                        return parent;
                    }

                    case Kind.VariableDeclarationList:
                    {
                        if ((parent.AsVariableDeclarationList().Declarations.Nodes[0] == node))
                        {
                            return parent;
                        }

                        break;
                    }
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4222:1.
    public static Node GetContainingFunction(Node node)
    {
        return FindAncestor(node.Parent, IsFunctionLike);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4226:1.
    public static Node ImportFromModuleSpecifier(Node node)
    {
        {
            var result = TryGetImportFromModuleSpecifier(node);
            if ((result != null))
            {
                return result;
            }
        }

        Debug.FailBadSyntaxKind(node.Parent);
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4234:1.
    public static Node TryGetImportFromModuleSpecifier(Node node)
    {
        switch (node.Parent.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportDeclaration:
            {
                return node.Parent;
            }

            case Kind.ExternalModuleReference:
            {
                return node.Parent.Parent;
            }

            case Kind.CallExpression:
            {
                if ((IsImportCall(node.Parent) || IsRequireCall(node.Parent, false)))
                {
                    return node.Parent;
                }

                return null;
            }

            case Kind.LiteralType:
            {
                if (!IsStringLiteral(node))
                {
                    return null;
                }

                if (IsImportTypeNode(node.Parent.Parent))
                {
                    return node.Parent.Parent;
                }

                return null;
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4292:1.
    public static Node GetFirstConstructorWithBody(Node node)
    {
        {
            foreach (var member in GoRuntime.RangeValues(node.Members()))
            {
                if ((IsConstructorDeclaration(member) && NodeIsPresent(member.Body())))
                {
                    return member;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4327:1.
    public static bool NodeCanBeDecorated(bool useLegacyDecorators, Node node, Node parent, Node grandparent)
    {
        if (((useLegacyDecorators && (node.Name() != null)) && IsPrivateIdentifier(node.Name())))
        {
            return false;
        }

        switch (node.Kind)
        {
            case Kind.ClassDeclaration:
            {
                return true;
            }

            case Kind.ClassExpression:
            {
                return !useLegacyDecorators;
            }

            case Kind.PropertyDeclaration:
            {
                return ((parent != null) && (((useLegacyDecorators && IsClassDeclaration(parent)) || (((!useLegacyDecorators && IsClassLike(parent)) && !HasAbstractModifier(node)) && !HasAmbientModifier(node)))));
            }

            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.MethodDeclaration:
            {
                return (((parent != null) && (node.Body() != null)) && (((useLegacyDecorators && IsClassDeclaration(parent)) || (!useLegacyDecorators && IsClassLike(parent)))));
            }

            case Kind.Parameter:
            {
                if (!useLegacyDecorators)
                {
                    return false;
                }

                return ((((((parent != null) && (parent.Body() != null)) && ((((parent.Kind == Kind.Constructor) || (parent.Kind == Kind.MethodDeclaration)) || (parent.Kind == Kind.SetAccessor)))) && (GetThisParameter(parent) != node)) && (grandparent != null)) && (grandparent.Kind == Kind.ClassDeclaration));
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4361:1.
    public static bool ClassOrConstructorParameterIsDecorated(bool useLegacyDecorators, Node node)
    {
        if (NodeIsDecorated(useLegacyDecorators, node, null, null))
        {
            return true;
        }

        var constructor = GetFirstConstructorWithBody(node);
        return ((constructor != null) && ChildIsDecorated(useLegacyDecorators, constructor, node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4369:1.
    public static bool ClassElementOrClassElementParameterIsDecorated(bool useLegacyDecorators, Node node, Node parent)
    {
        NodeList parameters = default;
        if (IsAccessor(node))
        {
            var decls = GetAllAccessorDeclarations(parent.Members(), node);
            Node firstAccessorWithDecorators = default;
            if (HasDecorators(decls.FirstAccessor))
            {
                firstAccessorWithDecorators = decls.FirstAccessor;
            }
            else
            {
                if (((decls.SecondAccessor != null) && HasDecorators(decls.SecondAccessor)))
                {
                    firstAccessorWithDecorators = decls.SecondAccessor;
                }
            }

            if (((firstAccessorWithDecorators == null) || (node != firstAccessorWithDecorators)))
            {
                return false;
            }

            if ((decls.SetAccessor != null))
            {
                parameters = decls.SetAccessor.Parameters;
            }
        }
        else
        {
            if (IsMethodDeclaration(node))
            {
                parameters = node.ParameterList();
            }
        }

        if (NodeIsDecorated(useLegacyDecorators, node, parent, null))
        {
            return true;
        }

        if (((parameters != null) && (GoRuntime.Len(parameters.Nodes) > 0)))
        {
            {
                foreach (var parameter in GoRuntime.RangeValues(parameters.Nodes))
                {
                    if (IsThisParameter(parameter))
                    {
                        continue;
                    }

                    if (NodeIsDecorated(useLegacyDecorators, parameter, node, parent))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4404:1.
    public static bool NodeIsDecorated(bool useLegacyDecorators, Node node, Node parent, Node grandparent)
    {
        return (HasDecorators(node) && NodeCanBeDecorated(useLegacyDecorators, node, parent, grandparent));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4408:1.
    public static bool NodeOrChildIsDecorated(bool useLegacyDecorators, Node node, Node parent, Node grandparent)
    {
        return (NodeIsDecorated(useLegacyDecorators, node, parent, grandparent) || ChildIsDecorated(useLegacyDecorators, node, parent));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4412:1.
    public static bool ChildIsDecorated(bool useLegacyDecorators, Node node, Node parent)
    {
        switch (node.Kind)
        {
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            {
                return Core.Some(node.Members(), (m) =>
                {
                    return NodeOrChildIsDecorated(useLegacyDecorators, m, node, parent);
                });
            }

            case Kind.MethodDeclaration:
            case Kind.SetAccessor:
            case Kind.Constructor:
            {
                return Core.Some(node.Parameters(), (p) =>
                {
                    return NodeIsDecorated(useLegacyDecorators, p, node, parent);
                });
            }

            default:
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4436:1.
    public static AllAccessorDeclarations GetAllAccessorDeclarationsForDeclaration(Node accessor, List<Node> declarationsOfSymbol)
    {
        Kind otherKind = default;
        if ((accessor.Kind == Kind.SetAccessor))
        {
            otherKind = Kind.GetAccessor;
        }
        else
        {
            if ((accessor.Kind == Kind.GetAccessor))
            {
                otherKind = Kind.SetAccessor;
            }
            else
            {
                throw new InvalidOperationException("Unexpected node kind \"" + accessor.Kind.String() + "\"");
            }
        }

        Node otherAccessor = default;
        {
            foreach (var d in GoRuntime.RangeValues(declarationsOfSymbol))
            {
                if ((d.Kind == otherKind))
                {
                    otherAccessor = d;
                    break;
                }
            }
        }

        Node firstAccessor = default;
        Node secondAccessor = default;
        if (((otherAccessor != null) && ((otherAccessor.Pos() < accessor.Pos()))))
        {
            firstAccessor = otherAccessor;
            secondAccessor = accessor;
        }
        else
        {
            firstAccessor = accessor;
            secondAccessor = otherAccessor;
        }

        SetAccessorDeclaration setAccessor = default;
        GetAccessorDeclaration getAccessor = default;
        if ((accessor.Kind == Kind.SetAccessor))
        {
            setAccessor = accessor.AsSetAccessorDeclaration();
            if ((otherAccessor != null))
            {
                getAccessor = otherAccessor.AsGetAccessorDeclaration();
            }
        }
        else
        {
            getAccessor = accessor.AsGetAccessorDeclaration();
            if ((otherAccessor != null))
            {
                setAccessor = otherAccessor.AsSetAccessorDeclaration();
            }
        }

        return new AllAccessorDeclarations
        {
            FirstAccessor = firstAccessor,
            SecondAccessor = secondAccessor,
            SetAccessor = setAccessor,
            GetAccessor = getAccessor
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4486:1.
    public static AllAccessorDeclarations GetAllAccessorDeclarations(Slice<Node> parentDeclarations, Node accessor)
    {
        if (HasDynamicName(accessor))
        {
            return GetAllAccessorDeclarationsForDeclaration(accessor, new List<Node> { accessor });
        }

        var accessorName = GetPropertyNameForPropertyNameNode(accessor.Name());
        var accessorStatic = IsStatic(accessor);
        List<Node> matches = default;
        {
            foreach (var member in GoRuntime.RangeValues(parentDeclarations))
            {
                if ((!IsAccessor(member) || (IsStatic(member) != accessorStatic)))
                {
                    continue;
                }

                var memberName = GetPropertyNameForPropertyNameNode(member.Name());
                if ((memberName == accessorName))
                {
                    matches = GoRuntime.Append(matches, member);
                }
            }
        }

        return GetAllAccessorDeclarationsForDeclaration(accessor, matches);
    }

    // *
    // * Gets the most likely element type for a TypeNode. This is not an exhaustive test
    // * as it assumes a rest argument can only be an array type (either T[], or Array<T>).
    // *
    // * @param node The type node.
    // *
    // * @internal
    //
    // Upstream upstream/TypeScript/tsc/internal/ast/utilities.go:4524:1.
    public static Node GetRestParameterElementType(Node node)
    {
        if ((node == null))
        {
            return node;
        }

        if ((node.Kind == Kind.ArrayType))
        {
            return node.AsArrayTypeNode().ElementType;
        }

        if (((node.Kind == Kind.TypeReference) && (node.AsTypeReferenceNode().TypeArguments != null)))
        {
            return Core.FirstOrNil(node.AsTypeReferenceNode().TypeArguments.Nodes);
        }

        return null;
    }
}

// END FOUNDATION SYNTAX ALGORITHMS
// utilities.go:950/2271/4429. Value models used by pure syntax inspection.
public enum FindAncestorResult
{
    False = 0,
    True = 1,
    Quit = 2
}

[Flags]
public enum SemanticMeaning
{
    None = 0,
    Value = 1,
    Type = 2,
    Namespace = 4,
    All = Value | Type | Namespace
}

public struct AllAccessorDeclarations
{
    public Node FirstAccessor;
    public Node SecondAccessor;
    public SetAccessorDeclaration SetAccessor;
    public GetAccessorDeclaration GetAccessor;
}

// utilities.go:3845. Path wrappers retain the supplied values; no path/project resolution.
public interface HasFileName
{
    string FileName();
    string PathKey();
}

public static partial class Ast
{
    private sealed class hasFileNameImpl(string fileName, string path) : HasFileName
    {
        public string FileName() => fileName;
        public string PathKey() => path;
    }

    public static HasFileName NewHasFileName(string fileName, string path) => new hasFileNameImpl(fileName, path);
    // utilities.go:877. Managed fresh closure replaces the upstream pool; state/control flow retained.
    private static Visitor newParentInChildrenSetter()
    {
        Node parent = null;
        Visitor visit = null;
        visit = node =>
        {
            if (parent != null)
                node.Parent = parent;
            var saveParent = parent;
            parent = node;
            node.ForEachChild(visit);
            parent = saveParent;
            return false;
        };
        return visit;
    }

    // utilities.go:899. No semantic parent/symbol inference is performed.
    public static void SetParentInChildren(Node node)
    {
        var fn = newParentInChildrenSetter();
        fn(node);
    }

    // utilities.go:921. Preserve ordered callbacks and at most one callback match per ancestor.
    public static List<Node> FindManyAncestors(Node node, params Func<Node, bool>[] callbacks)
    {
        var ancestors = new List<Node>(new Node[callbacks.Length]);
        var found = 0;
        while (node != null)
        {
            for (var i = 0; i < callbacks.Length; i++)
            {
                if (ancestors[i] == null && callbacks[i](node))
                {
                    ancestors[i] = node;
                    found++;
                    if (found == callbacks.Length)
                        return ancestors;
                    break;
                }
            }

            node = node.Parent;
        }

        return ancestors;
    }
}

public static partial class Ast
{
    // utilities.go:1699. Pure enum classification, independent of option derivation.
    private static bool isCommonJSContainingModuleKind(ModuleKind kind) => kind == ModuleKind.CommonJS || ModuleKind.Node16 <= kind && kind <= ModuleKind.NodeNext;
}

public static partial class Ast
{
    // utilities.go:3644. A module's real declaration is preserved across alias printing.
    public static SourceFile GetSourceFileOfModule(Symbol module)
    {
        var declaration = module.ValueDeclaration ?? GetNonAugmentationDeclaration(module);
        return GetSourceFileOfNode(declaration);
    }

    // utilities.go:3652.
    public static Node GetNonAugmentationDeclaration(Symbol symbol) => Core.Find(symbol.Declarations, declaration => !IsExternalModuleAugmentation(declaration) && !IsGlobalScopeAugmentation(declaration));
}
