// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using ScannerApi = TypeScript.Scanner.Scanner;
using TypeScript.LanguageServices.Utilities;

namespace TypeScript.LanguageServices.Formatting;
internal static partial class Formatting
{
    // rulecontext.go:22.
    private static SemicolonPreference SemicolonOption(FormatCodeSettings options) => options.Semicolons;
    // rulecontext.go:26.
    private static Tristate InsertSpaceAfterCommaDelimiterOption(FormatCodeSettings options) => options.InsertSpaceAfterCommaDelimiter;
    // rulecontext.go:30.
    private static Tristate InsertSpaceAfterSemicolonInForStatementsOption(FormatCodeSettings options) => options.InsertSpaceAfterSemicolonInForStatements;
    // rulecontext.go:34.
    private static Tristate InsertSpaceBeforeAndAfterBinaryOperatorsOption(FormatCodeSettings options) => options.InsertSpaceBeforeAndAfterBinaryOperators;
    // rulecontext.go:38.
    private static Tristate InsertSpaceAfterConstructorOption(FormatCodeSettings options) => options.InsertSpaceAfterConstructor;
    // rulecontext.go:42.
    private static Tristate InsertSpaceAfterKeywordsInControlFlowStatementsOption(FormatCodeSettings options) => options.InsertSpaceAfterKeywordsInControlFlowStatements;
    // rulecontext.go:46.
    private static Tristate InsertSpaceAfterFunctionKeywordForAnonymousFunctionsOption(FormatCodeSettings options) => options.InsertSpaceAfterFunctionKeywordForAnonymousFunctions;
    // rulecontext.go:50.
    private static Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesisOption(FormatCodeSettings options) => options.InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis;
    // rulecontext.go:54.
    private static Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracketsOption(FormatCodeSettings options) => options.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets;
    // rulecontext.go:58.
    private static Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracesOption(FormatCodeSettings options) => options.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces;
    // rulecontext.go:62.
    private static Tristate InsertSpaceAfterOpeningAndBeforeClosingEmptyBracesOption(FormatCodeSettings options) => options.InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces;
    // rulecontext.go:66.
    private static Tristate InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBracesOption(FormatCodeSettings options) => options.InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces;
    // rulecontext.go:70.
    private static Tristate InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBracesOption(FormatCodeSettings options) => options.InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces;
    // rulecontext.go:74.
    private static Tristate InsertSpaceAfterTypeAssertionOption(FormatCodeSettings options) => options.InsertSpaceAfterTypeAssertion;
    // rulecontext.go:78.
    private static Tristate InsertSpaceBeforeFunctionParenthesisOption(FormatCodeSettings options) => options.InsertSpaceBeforeFunctionParenthesis;
    // rulecontext.go:82.
    private static Tristate PlaceOpenBraceOnNewLineForFunctionsOption(FormatCodeSettings options) => options.PlaceOpenBraceOnNewLineForFunctions;
    // rulecontext.go:86.
    private static Tristate PlaceOpenBraceOnNewLineForControlBlocksOption(FormatCodeSettings options) => options.PlaceOpenBraceOnNewLineForControlBlocks;
    // rulecontext.go:90.
    private static Tristate InsertSpaceBeforeTypeAnnotationOption(FormatCodeSettings options) => options.InsertSpaceBeforeTypeAnnotation;
    // rulecontext.go:94.
    private static Tristate IndentMultiLineObjectLiteralBeginningOnBlankLineOption(FormatCodeSettings options) => options.IndentMultiLineObjectLiteralBeginningOnBlankLine;
    // rulecontext.go:98.
    private static Tristate IndentSwitchCaseOption(FormatCodeSettings options) => options.IndentSwitchCase;
    // rulecontext.go:104. Predicates retain each original selected-option/value closure owner.
    private static ContextPredicate OptionEquals<T>(Func<FormatCodeSettings, T> selector, T value) => context => EqualityComparer<T>.Default.Equals(selector(context.Options), value);
    // rulecontext.go:110.
    private static ContextPredicate IsOptionEnabled(Func<FormatCodeSettings, Tristate> selector) => context => selector(context.Options) == Tristate.True;
    // rulecontext.go:116.
    private static ContextPredicate IsOptionDisabled(Func<FormatCodeSettings, Tristate> selector) => context => selector(context.Options) == Tristate.False;
    // rulecontext.go:122.
    private static ContextPredicate IsOptionDisabledOrUndefined(Func<FormatCodeSettings, Tristate> selector) => context => selector(context.Options) != Tristate.True;
    // rulecontext.go:128.
    private static ContextPredicate IsOptionDisabledOrUndefinedOrTokensOnSameLine(Func<FormatCodeSettings, Tristate> selector) => context => selector(context.Options) != Tristate.True || context.TokensAreOnSameLine();
    // rulecontext.go:134.
    private static ContextPredicate IsOptionEnabledOrUndefined(Func<FormatCodeSettings, Tristate> selector) => context => selector(context.Options) != Tristate.False;
    // rulecontext.go:138.
    private static bool IsForContext(FormattingContext context) => context.contextNode.Kind == Kind.ForStatement;
    // rulecontext.go:142.
    private static bool IsNotForContext(FormattingContext context) => !IsForContext(context);
    // rulecontext.go:195.
    private static bool IsNotBinaryOpContext(FormattingContext context) => !IsBinaryOpContext(context);
    // rulecontext.go:199.
    private static bool IsNotTypeAnnotationContext(FormattingContext context) => !IsTypeAnnotationContext(context);
    // rulecontext.go:212.
    private static bool IsOptionalPropertyContext(FormattingContext context) => Ast.IsPropertyDeclaration(context.contextNode) && Ast.HasQuestionToken(context.contextNode);
    // rulecontext.go:216.
    private static bool IsNonOptionalPropertyContext(FormattingContext context) => !IsOptionalPropertyContext(context);
    // rulecontext.go:220.
    private static bool IsConditionalOperatorContext(FormattingContext context) => context.contextNode.Kind == Kind.ConditionalExpression || context.contextNode.Kind == Kind.ConditionalType;
    // rulecontext.go:225.
    private static bool IsSameLineTokenOrBeforeBlockContext(FormattingContext context) => context.TokensAreOnSameLine() || IsBeforeBlockContext(context);
    // rulecontext.go:229.
    private static bool IsBraceWrappedContext(FormattingContext context) => context.contextNode.Kind == Kind.ObjectBindingPattern || context.contextNode.Kind == Kind.MappedType || IsSingleLineBlockContext(context);
    // rulecontext.go:236.
    private static bool IsBeforeMultilineBlockContext(FormattingContext context) => IsBeforeBlockContext(context) && !(context.NextNodeAllOnSameLine() || context.NextNodeBlockIsOnOneLine());
    // rulecontext.go:240.
    private static bool IsMultilineBlockContext(FormattingContext context) => IsBlockContext(context) && !(context.ContextNodeAllOnSameLine() || context.ContextNodeBlockIsOnOneLine());
    // rulecontext.go:244.
    private static bool IsSingleLineBlockContext(FormattingContext context) => IsBlockContext(context) && (context.ContextNodeAllOnSameLine() || context.ContextNodeBlockIsOnOneLine());
    // rulecontext.go:248.
    private static bool IsBlockContext(FormattingContext context) => NodeIsBlockContext(context.contextNode);
    // rulecontext.go:252.
    private static bool IsBeforeBlockContext(FormattingContext context) => NodeIsBlockContext(context.nextTokenParent);
    // rulecontext.go:300.
    private static bool IsNotFunctionDeclContext(FormattingContext context) => !IsFunctionDeclContext(context);
    // rulecontext.go:304.
    private static bool IsFunctionDeclarationOrFunctionExpressionContext(FormattingContext context) => context.contextNode.Kind == Kind.FunctionDeclaration || context.contextNode.Kind == Kind.FunctionExpression;
    // rulecontext.go:308.
    private static bool IsTypeScriptDeclWithBlockContext(FormattingContext context) => NodeIsTypeScriptDeclWithBlockContext(context.contextNode);
    // rulecontext.go:371.
    private static bool IsObjectContext(FormattingContext context) => context.contextNode.Kind == Kind.ObjectLiteralExpression;
    // rulecontext.go:375.
    private static bool IsFunctionCallContext(FormattingContext context) => context.contextNode.Kind == Kind.CallExpression;
    // rulecontext.go:379.
    private static bool IsNewContext(FormattingContext context) => context.contextNode.Kind == Kind.NewExpression;
    // rulecontext.go:383.
    private static bool IsFunctionCallOrNewContext(FormattingContext context) => IsFunctionCallContext(context) || IsNewContext(context);
    // rulecontext.go:387.
    private static bool IsPreviousTokenNotComma(FormattingContext context) => context.currentTokenSpan.Kind != Kind.CommaToken;
    // rulecontext.go:391.
    private static bool IsNextTokenNotCloseBracket(FormattingContext context) => context.nextTokenSpan.Kind != Kind.CloseBracketToken;
    // rulecontext.go:395.
    private static bool IsNextTokenNotCloseParen(FormattingContext context) => context.nextTokenSpan.Kind != Kind.CloseParenToken;
    // rulecontext.go:399.
    private static bool IsArrowFunctionContext(FormattingContext context) => context.contextNode.Kind == Kind.ArrowFunction;
    // rulecontext.go:403.
    private static bool IsImportTypeContext(FormattingContext context) => context.contextNode.Kind == Kind.ImportType;
    // rulecontext.go:407.
    private static bool IsNonJsxSameLineTokenContext(FormattingContext context) => context.TokensAreOnSameLine() && context.contextNode.Kind != Kind.JsxText;
    // rulecontext.go:411.
    private static bool IsNonJsxTextContext(FormattingContext context) => context.contextNode.Kind != Kind.JsxText;
    // rulecontext.go:415.
    private static bool IsNonJsxElementOrFragmentContext(FormattingContext context) => context.contextNode.Kind != Kind.JsxElement && context.contextNode.Kind != Kind.JsxFragment;
    // rulecontext.go:419.
    private static bool IsJsxExpressionContext(FormattingContext context) => context.contextNode.Kind == Kind.JsxExpression || context.contextNode.Kind == Kind.JsxSpreadAttribute;
    // rulecontext.go:423.
    private static bool IsNextTokenParentJsxAttribute(FormattingContext context) => context.nextTokenParent.Kind == Kind.JsxAttribute || (context.nextTokenParent.Kind == Kind.JsxNamespacedName && context.nextTokenParent.Parent.Kind == Kind.JsxAttribute);
    // rulecontext.go:427.
    private static bool IsJsxAttributeContext(FormattingContext context) => context.contextNode.Kind == Kind.JsxAttribute;
    // rulecontext.go:431.
    private static bool IsNextTokenParentNotJsxNamespacedName(FormattingContext context) => context.nextTokenParent.Kind != Kind.JsxNamespacedName;
    // rulecontext.go:435.
    private static bool IsNextTokenParentJsxNamespacedName(FormattingContext context) => context.nextTokenParent.Kind == Kind.JsxNamespacedName;
    // rulecontext.go:439.
    private static bool IsJsxSelfClosingElementContext(FormattingContext context) => context.contextNode.Kind == Kind.JsxSelfClosingElement;
    // rulecontext.go:443.
    private static bool IsNotBeforeBlockInFunctionDeclarationContext(FormattingContext context) => !IsFunctionDeclContext(context) && !IsBeforeBlockContext(context);
    // rulecontext.go:447.
    private static bool IsEndOfDecoratorContextOnSameLine(FormattingContext context) => context.TokensAreOnSameLine() && Ast.HasDecorators(context.contextNode) && NodeIsInDecoratorContext(context.currentTokenParent) && !NodeIsInDecoratorContext(context.nextTokenParent);
    // rulecontext.go:461.
    private static bool IsStartOfVariableDeclarationList(FormattingContext context) => context.currentTokenParent.Kind == Kind.VariableDeclarationList && ScannerApi.GetTokenPosOfNode(context.currentTokenParent, context.SourceFile, false) == context.currentTokenSpan.Loc.Pos();
    // rulecontext.go:466.
    private static bool IsNotFormatOnEnter(FormattingContext context) => context.FormattingRequestKind != FormatRequestKind.FormatOnEnter;
    // rulecontext.go:470.
    private static bool IsModuleDeclContext(FormattingContext context) => context.contextNode.Kind == Kind.ModuleDeclaration;
    // rulecontext.go:474.
    private static bool IsObjectTypeContext(FormattingContext context) => context.contextNode.Kind == Kind.TypeLiteral;
    // rulecontext.go:478.
    private static bool IsConstructorSignatureContext(FormattingContext context) => context.contextNode.Kind == Kind.ConstructSignature;
    // rulecontext.go:509.
    private static bool IsTypeArgumentOrParameterOrAssertionContext(FormattingContext context) => IsTypeArgumentOrParameterOrAssertion(context.currentTokenSpan, context.currentTokenParent) || IsTypeArgumentOrParameterOrAssertion(context.nextTokenSpan, context.nextTokenParent);
    // rulecontext.go:514.
    private static bool IsTypeAssertionContext(FormattingContext context) => context.contextNode.Kind == Kind.TypeAssertionExpression;
    // rulecontext.go:518.
    private static bool IsNonTypeAssertionContext(FormattingContext context) => !IsTypeAssertionContext(context);
    // rulecontext.go:522.
    private static bool IsVoidOpContext(FormattingContext context) => context.currentTokenSpan.Kind == Kind.VoidKeyword && context.currentTokenParent.Kind == Kind.VoidExpression;
    // rulecontext.go:526.
    private static bool IsYieldOrYieldStarWithOperand(FormattingContext context) => context.contextNode.Kind == Kind.YieldExpression && context.contextNode.Expression() != null;
    // rulecontext.go:530.
    private static bool IsNonNullAssertionContext(FormattingContext context) => context.contextNode.Kind == Kind.NonNullExpression;
    // rulecontext.go:534.
    private static bool IsNotStatementConditionContext(FormattingContext context) => !IsStatementConditionContext(context);
    // rulecontext.go:621.
    private static bool IsSemicolonInsertionContext(FormattingContext context) => LsUtil.PositionIsASICandidate(context.currentTokenSpan.Loc.End(), context.currentTokenParent, context.SourceFile);
    // rulecontext.go:625.
    private static bool IsNotPropertyAccessOnIntegerLiteral(FormattingContext context) => !Ast.IsPropertyAccessExpression(context.contextNode) || !Ast.IsNumericLiteral(context.contextNode.Expression()) || context.contextNode.Expression().TextValue().IndexOf(ByteStringValue.FromString(".")) >= 0;
    // rulecontext.go:146.
    private static bool IsBinaryOpContext(FormattingContext c)
    {
        switch (c.contextNode.Kind)
        {
            case Kind.BinaryExpression:
                return c.contextNode.AsBinaryExpression().OperatorToken.Kind != Kind.CommaToken;
            case Kind.ConditionalExpression:
            case Kind.ConditionalType:
            case Kind.AsExpression:
            case Kind.ExportSpecifier:
            case Kind.ImportSpecifier:
            case Kind.TypePredicate:
            case Kind.UnionType:
            case Kind.IntersectionType:
            case Kind.SatisfiesExpression:
                return true;
            case Kind.BindingElement:
            case Kind.TypeAliasDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.ExportAssignment:
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.EnumMember:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
                return c.currentTokenSpan.Kind == Kind.EqualsToken || c.nextTokenSpan.Kind == Kind.EqualsToken;
            case Kind.ForInStatement:
            case Kind.TypeParameter:
                return c.currentTokenSpan.Kind == Kind.InKeyword || c.nextTokenSpan.Kind == Kind.InKeyword || c.currentTokenSpan.Kind == Kind.EqualsToken || c.nextTokenSpan.Kind == Kind.EqualsToken;
            case Kind.ForOfStatement:
                return c.currentTokenSpan.Kind == Kind.OfKeyword || c.nextTokenSpan.Kind == Kind.OfKeyword;
        }

        return false;
    }

    // rulecontext.go:207.
    private static bool IsTypeAnnotationContext(FormattingContext c)
    {
        var kind = c.contextNode.Kind;
        return kind is Kind.PropertyDeclaration or Kind.PropertySignature or Kind.Parameter or Kind.VariableDeclaration || Ast.IsFunctionLikeKind(kind);
    }

    // rulecontext.go:265. Only nodes with immediate brace children are admitted.
    private static bool NodeIsBlockContext(Node node) => NodeIsTypeScriptDeclWithBlockContext(node) || node.Kind is Kind.Block or Kind.CaseBlock or Kind.ObjectLiteralExpression or Kind.ModuleBlock;
    // rulecontext.go:282.
    private static bool IsFunctionDeclContext(FormattingContext c) => c.contextNode.Kind is Kind.FunctionDeclaration or Kind.MethodDeclaration or Kind.MethodSignature or Kind.GetAccessor or Kind.SetAccessor or Kind.CallSignature or Kind.FunctionExpression or Kind.Constructor or Kind.ArrowFunction or Kind.InterfaceDeclaration;
    // rulecontext.go:321.
    private static bool NodeIsTypeScriptDeclWithBlockContext(Node node) => node.Kind is Kind.ClassDeclaration or Kind.ClassExpression or Kind.InterfaceDeclaration or Kind.EnumDeclaration or Kind.TypeLiteral or Kind.ModuleDeclaration or Kind.ExportDeclaration or Kind.NamedExports or Kind.ImportDeclaration or Kind.NamedImports;
    // rulecontext.go:340.
    private static bool IsAfterCodeBlockContext(FormattingContext c)
    {
        switch (c.currentTokenParent.Kind)
        {
            case Kind.ClassDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.EnumDeclaration:
            case Kind.CatchClause:
            case Kind.ModuleBlock:
            case Kind.SwitchStatement:
                return true;
            case Kind.Block:
                var parent = c.currentTokenParent.Parent;
                if (parent == null || parent.Kind != Kind.ArrowFunction && parent.Kind != Kind.FunctionExpression)
                    return true;
                break;
        }

        return false;
    }

    // rulecontext.go:360.
    private static bool IsControlDeclContext(FormattingContext c) => c.contextNode.Kind is Kind.IfStatement or Kind.SwitchStatement or Kind.ForStatement or Kind.ForInStatement or Kind.ForOfStatement or Kind.WhileStatement or Kind.TryStatement or Kind.DoStatement or Kind.WithStatement or Kind.CatchClause;
    // rulecontext.go:454.
    private static bool NodeIsInDecoratorContext(Node? node)
    {
        while (node != null && Ast.IsExpression(node))
            node = node.Parent;
        return node != null && node.Kind == Kind.Decorator;
    }

    // rulecontext.go:482.
    private static bool IsTypeArgumentOrParameterOrAssertion(TextRangeWithKind token, Node parent)
    {
        if (token.Kind != Kind.LessThanToken && token.Kind != Kind.GreaterThanToken)
            return false;
        return parent.Kind is Kind.TypeReference or Kind.TypeAssertionExpression or Kind.TypeAliasDeclaration or Kind.ClassDeclaration or Kind.ClassExpression or Kind.InterfaceDeclaration or Kind.FunctionDeclaration or Kind.FunctionExpression or Kind.ArrowFunction or Kind.MethodDeclaration or Kind.MethodSignature or Kind.CallSignature or Kind.ConstructSignature or Kind.CallExpression or Kind.NewExpression or Kind.ExpressionWithTypeArguments;
    }

    // rulecontext.go:538.
    private static bool IsStatementConditionContext(FormattingContext c) => c.contextNode.Kind is Kind.IfStatement or Kind.ForStatement or Kind.ForInStatement or Kind.ForOfStatement or Kind.DoStatement or Kind.WhileStatement;
    // rulecontext.go:554.
    private static bool IsSemicolonDeletionContext(FormattingContext context)
    {
        var nextTokenKind = context.nextTokenSpan.Kind;
        var nextTokenStart = context.nextTokenSpan.Loc.Pos();
        if (Ast.IsTrivia(nextTokenKind))
        {
            // Original TODO: codefix parent ownership differs from the previous formatter.
            var next = context.nextTokenParent == context.currentTokenParent ? AstNav.FindNextToken(context.nextTokenParent, context.SourceFile.AsNode(), context.SourceFile) : LsUtil.GetFirstToken(context.nextTokenParent, context.SourceFile);
            if (next == null)
                return true;
            nextTokenKind = next.Kind;
            nextTokenStart = ScannerApi.GetTokenPosOfNode(next, context.SourceFile, false);
        }

        var startLine = ScannerApi.GetECMALineOfPosition(context.SourceFile, context.currentTokenSpan.Loc.Pos());
        var endLine = ScannerApi.GetECMALineOfPosition(context.SourceFile, nextTokenStart);
        if (startLine == endLine)
            return nextTokenKind == Kind.CloseBraceToken || nextTokenKind == Kind.EndOfFile;
        if (nextTokenKind == Kind.SemicolonToken && context.currentTokenSpan.Kind == Kind.SemicolonToken)
            return true;
        if (nextTokenKind is Kind.SemicolonClassElement or Kind.SemicolonToken)
            return false;
        if (context.contextNode.Kind is Kind.InterfaceDeclaration or Kind.TypeAliasDeclaration)
            return context.currentTokenParent.Kind != Kind.PropertySignature || context.currentTokenParent.Type() != null || nextTokenKind != Kind.OpenParenToken;
        if (Ast.IsPropertyDeclaration(context.currentTokenParent))
            return context.currentTokenParent.Initializer() == null;
        return context.currentTokenParent.Kind != Kind.ForStatement && context.currentTokenParent.Kind != Kind.EmptyStatement && context.currentTokenParent.Kind != Kind.SemicolonClassElement && nextTokenKind != Kind.OpenBracketToken && nextTokenKind != Kind.OpenParenToken && nextTokenKind != Kind.PlusToken && nextTokenKind != Kind.MinusToken && nextTokenKind != Kind.SlashToken && nextTokenKind != Kind.RegularExpressionLiteral && nextTokenKind != Kind.CommaToken && nextTokenKind != Kind.TemplateExpression && nextTokenKind != Kind.TemplateHead && nextTokenKind != Kind.NoSubstitutionTemplateLiteral && nextTokenKind != Kind.DotToken;
    }
}
