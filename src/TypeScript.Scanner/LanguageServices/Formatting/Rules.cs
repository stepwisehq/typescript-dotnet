// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;

namespace TypeScript.LanguageServices.Formatting;
internal static partial class Formatting
{
    // rules.go:10. All rule lists and their priority order are retained.
    private static Slice<RuleSpec> GetAllRules()
    {
        var allTokens = Slice<Kind>.Make(0, (int)Kind.LastToken - (int)Kind.FirstToken + 1);
        for (var token = Kind.FirstToken; token <= Kind.LastToken; token++)
            if (token != Kind.EndOfFile)
                allTokens = allTokens.Append(token);
        TokenRange AnyTokenExcept(params Kind[] tokens)
        {
            var selected = Slice<Kind>.Make(0, allTokens.Length);
            foreach (var token in allTokens)
                if (Array.IndexOf(tokens, token) < 0)
                    selected = selected.Append(token);
            return new(selected, false);
        }

        var anyToken = new TokenRange(allTokens, false);
        var anyTokenIncludingMultilineComments = TokenRangeFromEx(allTokens, Kind.MultiLineCommentTrivia);
        var anyTokenIncludingEOF = TokenRangeFromEx(allTokens, Kind.EndOfFile);
        var keywords = TokenRangeFromRange(Kind.FirstKeyword, Kind.LastKeyword);
        var binaryOperators = TokenRangeFromRange(Kind.FirstBinaryOperator, Kind.LastBinaryOperator);
        var binaryKeywordOperators = Slice<Kind>.FromArray([Kind.InKeyword, Kind.InstanceOfKeyword, Kind.OfKeyword, Kind.AsKeyword, Kind.IsKeyword, Kind.SatisfiesKeyword, ]);
        var unaryPrefixOperators = Slice<Kind>.FromArray([Kind.PlusPlusToken, Kind.MinusToken, Kind.TildeToken, Kind.ExclamationToken]);
        var unaryPrefixExpressions = Slice<Kind>.FromArray([Kind.NumericLiteral, Kind.BigIntLiteral, Kind.Identifier, Kind.OpenParenToken, Kind.OpenBracketToken, Kind.OpenBraceToken, Kind.ThisKeyword, Kind.NewKeyword, ]);
        var unaryPreincrementExpressions = Slice<Kind>.FromArray([Kind.Identifier, Kind.OpenParenToken, Kind.ThisKeyword, Kind.NewKeyword]);
        var unaryPostincrementExpressions = Slice<Kind>.FromArray([Kind.Identifier, Kind.CloseParenToken, Kind.CloseBracketToken, Kind.NewKeyword]);
        var unaryPredecrementExpressions = Slice<Kind>.FromArray([Kind.Identifier, Kind.OpenParenToken, Kind.ThisKeyword, Kind.NewKeyword]);
        var unaryPostdecrementExpressions = Slice<Kind>.FromArray([Kind.Identifier, Kind.CloseParenToken, Kind.CloseBracketToken, Kind.NewKeyword]);
        var comments = Slice<Kind>.FromArray([Kind.SingleLineCommentTrivia, Kind.MultiLineCommentTrivia]);
        var typeKeywords = Slice<Kind>.FromArray([Kind.AnyKeyword, Kind.AssertsKeyword, Kind.BigIntKeyword, Kind.BooleanKeyword, Kind.FalseKeyword, Kind.InferKeyword, Kind.KeyOfKeyword, Kind.NeverKeyword, Kind.NullKeyword, Kind.NumberKeyword, Kind.ObjectKeyword, Kind.ReadonlyKeyword, Kind.StringKeyword, Kind.SymbolKeyword, Kind.TypeOfKeyword, Kind.TrueKeyword, Kind.VoidKeyword, Kind.UndefinedKeyword, Kind.UniqueKeyword, Kind.UnknownKeyword, ]);
        var typeNames = Slice<Kind>.FromArray([Kind.Identifier]).Append(typeKeywords);
        // Place a space before open brace in a function declaration
        // TypeScript: Function can have return types, which can be made of tons of different token kinds
        var functionOpenBraceLeftTokenRange = anyTokenIncludingMultilineComments;
        // Place a space before open brace in a TypeScript declaration that has braces as children (class, module, enum, etc)
        var typeScriptOpenBraceLeftTokenRange = TokenRangeFrom(Kind.Identifier, Kind.GreaterThanToken, Kind.MultiLineCommentTrivia, Kind.ClassKeyword, Kind.ExportKeyword, Kind.ImportKeyword);
        // Place a space before open brace in a control flow construct
        var controlOpenBraceLeftTokenRange = TokenRangeFrom(Kind.CloseParenToken, Kind.MultiLineCommentTrivia, Kind.DoKeyword, Kind.TryKeyword, Kind.FinallyKeyword, Kind.ElseKeyword, Kind.CatchKeyword);
        // These rules are higher in priority than user-configurable
        var highPriorityCommonRules = Slice<RuleSpec>.FromArray([ // Leave comments alone
            Rule("IgnoreBeforeComment", anyToken, comments, AnyContext, RuleAction.StopProcessingSpaceActions),
            Rule("IgnoreAfterLineComment", Kind.SingleLineCommentTrivia, anyToken, AnyContext, RuleAction.StopProcessingSpaceActions),
            Rule(
                "NotSpaceBeforeColon",
                anyToken,
                Kind.ColonToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotBinaryOpContext, IsNotTypeAnnotationContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceAfterColon",
                Kind.ColonToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotBinaryOpContext, IsNextTokenParentNotJsxNamespacedName]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeQuestionMark",
                anyToken,
                Kind.QuestionToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotBinaryOpContext, IsNotTypeAnnotationContext]),
                RuleAction.DeleteSpace
            ), // insert space after '?' only when it is used in conditional operator
            Rule(
                "SpaceAfterQuestionMarkInConditionalOperator",
                Kind.QuestionToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsConditionalOperatorContext]),
                RuleAction.InsertSpace
            ), // in other cases there should be no space between '?' and next token
            Rule(
                "NoSpaceAfterQuestionMark",
                Kind.QuestionToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNonOptionalPropertyContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeDot",
                anyToken,
                Slice<Kind>.FromArray([Kind.DotToken, Kind.QuestionDotToken]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotPropertyAccessOnIntegerLiteral]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterDot",
                Slice<Kind>.FromArray([Kind.DotToken, Kind.QuestionDotToken]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBetweenImportParenInImportType",
                Kind.ImportKeyword,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsImportTypeContext]),
                RuleAction.DeleteSpace
            ), // Special handling of unary operators.
 // Prefix operators generally shouldn't have a space between
        // them and their target unary expression.
            Rule(
                "NoSpaceAfterUnaryPrefixOperator",
                unaryPrefixOperators,
                unaryPrefixExpressions,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotBinaryOpContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterUnaryPreincrementOperator",
                Kind.PlusPlusToken,
                unaryPreincrementExpressions,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterUnaryPredecrementOperator",
                Kind.MinusMinusToken,
                unaryPredecrementExpressions,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeUnaryPostincrementOperator",
                unaryPostincrementExpressions,
                Kind.PlusPlusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotStatementConditionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeUnaryPostdecrementOperator",
                unaryPostdecrementExpressions,
                Kind.MinusMinusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotStatementConditionContext]),
                RuleAction.DeleteSpace
            ), // More unary operator special-casing.
 // DevDiv 181814: Be careful when removing leading whitespace
        // around unary operators.  Examples:
        //      1 - -2  --X--> 1--2
        //      a + ++b --X--> a+++b
            Rule(
                "SpaceAfterPostincrementWhenFollowedByAdd",
                Kind.PlusPlusToken,
                Kind.PlusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterAddWhenFollowedByUnaryPlus",
                Kind.PlusToken,
                Kind.PlusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterAddWhenFollowedByPreincrement",
                Kind.PlusToken,
                Kind.PlusPlusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterPostdecrementWhenFollowedBySubtract",
                Kind.MinusMinusToken,
                Kind.MinusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterSubtractWhenFollowedByUnaryMinus",
                Kind.MinusToken,
                Kind.MinusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterSubtractWhenFollowedByPredecrement",
                Kind.MinusToken,
                Kind.MinusMinusToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterCloseBrace",
                Kind.CloseBraceToken,
                Slice<Kind>.FromArray([Kind.CommaToken, Kind.SemicolonToken]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // For functions and control block place } on a new line []ast.Kind{multi-line rule}
            Rule(
                "NewLineBeforeCloseBraceInBlockContext",
                anyTokenIncludingMultilineComments,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsMultilineBlockContext]),
                RuleAction.InsertNewLine
            ), // Space/new line after }.
            Rule(
                "SpaceAfterCloseBrace",
                Kind.CloseBraceToken,
                AnyTokenExcept(Kind.CloseParenToken),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsAfterCodeBlockContext]),
                RuleAction.InsertSpace
            ), // Special case for (}, else) and (}, while) since else & while tokens are not part of the tree which makes SpaceAfterCloseBrace rule not applied
 // Also should not apply to })
            Rule(
                "SpaceBetweenCloseBraceAndElse",
                Kind.CloseBraceToken,
                Kind.ElseKeyword,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBetweenCloseBraceAndWhile",
                Kind.CloseBraceToken,
                Kind.WhileKeyword,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenEmptyBraceBrackets",
                Kind.OpenBraceToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsObjectContext]),
                RuleAction.DeleteSpace
            ), // Add a space after control dec context if the next character is an open bracket ex: 'if (false)[]ast.Kind{a, b} = []ast.Kind{1, 2};' -> 'if (false) []ast.Kind{a, b} = []ast.Kind{1, 2};'
            Rule(
                "SpaceAfterConditionalClosingParen",
                Kind.CloseParenToken,
                Kind.OpenBracketToken,
                Slice<ContextPredicate>.FromArray([IsControlDeclContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenFunctionKeywordAndStar",
                Kind.FunctionKeyword,
                Kind.AsteriskToken,
                Slice<ContextPredicate>.FromArray([IsFunctionDeclarationOrFunctionExpressionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceAfterStarInGeneratorDeclaration",
                Kind.AsteriskToken,
                Kind.Identifier,
                Slice<ContextPredicate>.FromArray([IsFunctionDeclarationOrFunctionExpressionContext]),
                RuleAction.InsertSpace
            ),
            Rule("SpaceAfterFunctionInFuncDecl", Kind.FunctionKeyword, anyToken, Slice<ContextPredicate>.FromArray([IsFunctionDeclContext]), RuleAction.InsertSpace), // Insert new line after { and before } in multi-line contexts.
            Rule(
                "NewLineAfterOpenBraceInBlockContext",
                Kind.OpenBraceToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsMultilineBlockContext]),
                RuleAction.InsertNewLine
            ), // For get/set members, we check for (identifier,identifier) since get/set don't have tokens and they are represented as just an identifier token.
 // Though, we do extra check on the context to make sure we are dealing with get/set node. Example:
        //      get x() {}
        //      set x(val) {}
            Rule(
                "SpaceAfterGetSetInMember",
                Slice<Kind>.FromArray([Kind.GetKeyword, Kind.SetKeyword]),
                Kind.Identifier,
                Slice<ContextPredicate>.FromArray([IsFunctionDeclContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenYieldKeywordAndStar",
                Kind.YieldKeyword,
                Kind.AsteriskToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsYieldOrYieldStarWithOperand]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceBetweenYieldOrYieldStarAndOperand",
                Slice<Kind>.FromArray([Kind.YieldKeyword, Kind.AsteriskToken]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsYieldOrYieldStarWithOperand]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenReturnAndSemicolon",
                Kind.ReturnKeyword,
                Kind.SemicolonToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceAfterCertainKeywords",
                Slice<Kind>.FromArray([Kind.VarKeyword, Kind.ThrowKeyword, Kind.NewKeyword, Kind.DeleteKeyword, Kind.ReturnKeyword, Kind.TypeOfKeyword, Kind.AwaitKeyword]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterLetConstInVariableDeclaration",
                Slice<Kind>.FromArray([Kind.LetKeyword, Kind.ConstKeyword]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsStartOfVariableDeclarationList]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeOpenParenInFuncCall",
                anyToken,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsFunctionCallOrNewContext, IsPreviousTokenNotComma]),
                RuleAction.DeleteSpace
            ), // Special case for binary operators (that are keywords). For these we have to add a space and shouldn't follow any user options.
            Rule(
                "SpaceBeforeBinaryKeywordOperator",
                anyToken,
                binaryKeywordOperators,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterBinaryKeywordOperator",
                binaryKeywordOperators,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterVoidOperator",
                Kind.VoidKeyword,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsVoidOpContext]),
                RuleAction.InsertSpace
            ), // Async-await
            Rule(
                "SpaceBetweenAsyncAndOpenParen",
                Kind.AsyncKeyword,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsArrowFunctionContext, IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBetweenAsyncAndFunctionKeyword",
                Kind.AsyncKeyword,
                Slice<Kind>.FromArray([Kind.FunctionKeyword, Kind.Identifier]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ), // Template string
            Rule(
                "NoSpaceBetweenTagAndTemplateString",
                Slice<Kind>.FromArray([Kind.Identifier, Kind.CloseParenToken]),
                Slice<Kind>.FromArray([Kind.NoSubstitutionTemplateLiteral, Kind.TemplateHead]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // JSX opening elements
            Rule(
                "SpaceBeforeJsxAttribute",
                anyToken,
                Kind.Identifier,
                Slice<ContextPredicate>.FromArray([IsNextTokenParentJsxAttribute, IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBeforeSlashInJsxOpeningElement",
                anyToken,
                Kind.SlashToken,
                Slice<ContextPredicate>.FromArray([IsJsxSelfClosingElementContext, IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeGreaterThanTokenInJsxOpeningElement",
                Kind.SlashToken,
                Kind.GreaterThanToken,
                Slice<ContextPredicate>.FromArray([IsJsxSelfClosingElementContext, IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeEqualInJsxAttribute",
                anyToken,
                Kind.EqualsToken,
                Slice<ContextPredicate>.FromArray([IsJsxAttributeContext, IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterEqualInJsxAttribute",
                Kind.EqualsToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsJsxAttributeContext, IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeJsxNamespaceColon",
                Kind.Identifier,
                Kind.ColonToken,
                Slice<ContextPredicate>.FromArray([IsNextTokenParentJsxNamespacedName]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterJsxNamespaceColon",
                Kind.ColonToken,
                Kind.Identifier,
                Slice<ContextPredicate>.FromArray([IsNextTokenParentJsxNamespacedName]),
                RuleAction.DeleteSpace
            ), // TypeScript-specific rules
 // Use of module as a function call. e.g.: import m2 = module("m2");
            Rule(
                "NoSpaceAfterModuleImport",
                Slice<Kind>.FromArray([Kind.ModuleKeyword, Kind.RequireKeyword]),
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // Add a space around certain TypeScript keywords
            Rule(
                "SpaceAfterCertainTypeScriptKeywords",
                Slice<Kind>.FromArray([Kind.AbstractKeyword, Kind.AccessorKeyword, Kind.ClassKeyword, Kind.DeclareKeyword, Kind.DefaultKeyword, Kind.EnumKeyword, Kind.ExportKeyword, Kind.ExtendsKeyword, Kind.GetKeyword, Kind.ImplementsKeyword, Kind.ImportKeyword, Kind.InterfaceKeyword, Kind.ModuleKeyword, Kind.NamespaceKeyword, Kind.OverrideKeyword, Kind.PrivateKeyword, Kind.PublicKeyword, Kind.ProtectedKeyword, Kind.ReadonlyKeyword, Kind.SetKeyword, Kind.StaticKeyword, Kind.TypeKeyword, Kind.FromKeyword, Kind.KeyOfKeyword, Kind.InferKeyword, ]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBeforeCertainTypeScriptKeywords",
                anyToken,
                Slice<Kind>.FromArray([Kind.ExtendsKeyword, Kind.ImplementsKeyword, Kind.FromKeyword]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ), // Treat string literals in module names as identifiers, and add a space between the literal and the opening Brace braces, e.g.: module "m2" {
            Rule("SpaceAfterModuleName", Kind.StringLiteral, Kind.OpenBraceToken, Slice<ContextPredicate>.FromArray([IsModuleDeclContext]), RuleAction.InsertSpace), // Lambda expressions
            Rule("SpaceBeforeArrow", anyToken, Kind.EqualsGreaterThanToken, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.InsertSpace),
            Rule("SpaceAfterArrow", Kind.EqualsGreaterThanToken, anyToken, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.InsertSpace), // Optional parameters and let args
            Rule("NoSpaceAfterEllipsis", Kind.DotDotDotToken, Kind.Identifier, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.DeleteSpace),
            Rule(
                "NoSpaceAfterOptionalParameters",
                Kind.QuestionToken,
                Slice<Kind>.FromArray([Kind.CloseParenToken, Kind.CommaToken]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotBinaryOpContext]),
                RuleAction.DeleteSpace
            ), // Remove spaces in empty interface literals. e.g.: x: {}
            Rule(
                "NoSpaceBetweenEmptyInterfaceBraceBrackets",
                Kind.OpenBraceToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsObjectTypeContext]),
                RuleAction.DeleteSpace
            ), // generics and type assertions
            Rule(
                "NoSpaceBeforeOpenAngularBracket",
                typeNames,
                Kind.LessThanToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsTypeArgumentOrParameterOrAssertionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBetweenCloseParenAndAngularBracket",
                Kind.CloseParenToken,
                Kind.LessThanToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsTypeArgumentOrParameterOrAssertionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterOpenAngularBracket",
                Kind.LessThanToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsTypeArgumentOrParameterOrAssertionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeCloseAngularBracket",
                anyToken,
                Kind.GreaterThanToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsTypeArgumentOrParameterOrAssertionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterCloseAngularBracket",
                Kind.GreaterThanToken,
                Slice<Kind>.FromArray([Kind.OpenParenToken, Kind.OpenBracketToken, Kind.GreaterThanToken, Kind.CommaToken]),
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsTypeArgumentOrParameterOrAssertionContext, IsNotFunctionDeclContext, /*To prevent an interference with the SpaceBeforeOpenParenInFuncDecl Rule*/ IsNonTypeAssertionContext, ]),
                RuleAction.DeleteSpace
            ), // decorators
            Rule(
                "SpaceBeforeAt",
                Slice<Kind>.FromArray([Kind.CloseParenToken, Kind.Identifier]),
                Kind.AtToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule("NoSpaceAfterAt", Kind.AtToken, anyToken, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.DeleteSpace), // Insert space after @ in decorator
            Rule(
                "SpaceAfterDecorator",
                anyToken,
                Slice<Kind>.FromArray([Kind.AbstractKeyword, Kind.Identifier, Kind.ExportKeyword, Kind.DefaultKeyword, Kind.ClassKeyword, Kind.StaticKeyword, Kind.PublicKeyword, Kind.PrivateKeyword, Kind.ProtectedKeyword, Kind.GetKeyword, Kind.SetKeyword, Kind.OpenBracketToken, Kind.AsteriskToken, ]),
                Slice<ContextPredicate>.FromArray([IsEndOfDecoratorContextOnSameLine]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeNonNullAssertionOperator",
                anyToken,
                Kind.ExclamationToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNonNullAssertionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterNewKeywordOnConstructorSignature",
                Kind.NewKeyword,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsConstructorSignatureContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceLessThanAndNonJSXTypeAnnotation",
                Kind.LessThanToken,
                Kind.LessThanToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ), ]);
        // These rules are applied after high priority
        var userConfigurableRules = Slice<RuleSpec>.FromArray([ // Treat constructor as an identifier in a function declaration, and remove spaces between constructor and following left parentheses
            Rule(
                "SpaceAfterConstructor",
                Kind.ConstructorKeyword,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterConstructorOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterConstructor",
                Kind.ConstructorKeyword,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterConstructorOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceAfterComma",
                Kind.CommaToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterCommaDelimiterOption), IsNonJsxSameLineTokenContext, IsNonJsxElementOrFragmentContext, IsNextTokenNotCloseBracket, IsNextTokenNotCloseParen]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterComma",
                Kind.CommaToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterCommaDelimiterOption), IsNonJsxSameLineTokenContext, IsNonJsxElementOrFragmentContext]),
                RuleAction.DeleteSpace
            ), // Insert space after function keyword for anonymous functions
            Rule(
                "SpaceAfterAnonymousFunctionKeyword",
                Slice<Kind>.FromArray([Kind.FunctionKeyword, Kind.AsteriskToken]),
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterFunctionKeywordForAnonymousFunctionsOption), IsFunctionDeclContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterAnonymousFunctionKeyword",
                Slice<Kind>.FromArray([Kind.FunctionKeyword, Kind.AsteriskToken]),
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterFunctionKeywordForAnonymousFunctionsOption), IsFunctionDeclContext]),
                RuleAction.DeleteSpace
            ), // Insert space after keywords in control flow statements
            Rule(
                "SpaceAfterKeywordInControl",
                keywords,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterKeywordsInControlFlowStatementsOption), IsControlDeclContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterKeywordInControl",
                keywords,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterKeywordsInControlFlowStatementsOption), IsControlDeclContext]),
                RuleAction.DeleteSpace
            ), // Insert space after opening and before closing nonempty parenthesis
            Rule(
                "SpaceAfterOpenParen",
                Kind.OpenParenToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesisOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBeforeCloseParen",
                anyToken,
                Kind.CloseParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesisOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBetweenOpenParens",
                Kind.OpenParenToken,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesisOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenParens",
                Kind.OpenParenToken,
                Kind.CloseParenToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterOpenParen",
                Kind.OpenParenToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesisOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeCloseParen",
                anyToken,
                Kind.CloseParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesisOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // Insert space after opening and before closing nonempty brackets
            Rule(
                "SpaceAfterOpenBracket",
                Kind.OpenBracketToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracketsOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBeforeCloseBracket",
                anyToken,
                Kind.CloseBracketToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracketsOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenBrackets",
                Kind.OpenBracketToken,
                Kind.CloseBracketToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterOpenBracket",
                Kind.OpenBracketToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracketsOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeCloseBracket",
                anyToken,
                Kind.CloseBracketToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracketsOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // Insert a space after { and before } in single-line contexts, but remove space from empty object literals {}.
            Rule(
                "SpaceAfterOpenBrace",
                Kind.OpenBraceToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracesOption), IsBraceWrappedContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBeforeCloseBrace",
                anyToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracesOption), IsBraceWrappedContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenEmptyBraceBrackets",
                Kind.OpenBraceToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsObjectContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterOpenBrace",
                Kind.OpenBraceToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracesOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeCloseBrace",
                anyToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabled(InsertSpaceAfterOpeningAndBeforeClosingNonemptyBracesOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // Insert a space after opening and before closing empty brace brackets
            Rule(
                "SpaceBetweenEmptyBraceBrackets",
                Kind.OpenBraceToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingEmptyBracesOption)]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBetweenEmptyBraceBrackets",
                Kind.OpenBraceToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabled(InsertSpaceAfterOpeningAndBeforeClosingEmptyBracesOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // Insert space after opening and before closing template string braces
            Rule(
                "SpaceAfterTemplateHeadAndMiddle",
                Slice<Kind>.FromArray([Kind.TemplateHead, Kind.TemplateMiddle]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBracesOption), IsNonJsxTextContext]),
                RuleAction.InsertSpace,
                RuleFlags.CanDeleteNewLines
            ),
            Rule(
                "SpaceBeforeTemplateMiddleAndTail",
                anyToken,
                Slice<Kind>.FromArray([Kind.TemplateMiddle, Kind.TemplateTail]),
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBracesOption), IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterTemplateHeadAndMiddle",
                Slice<Kind>.FromArray([Kind.TemplateHead, Kind.TemplateMiddle]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBracesOption), IsNonJsxTextContext]),
                RuleAction.DeleteSpace,
                RuleFlags.CanDeleteNewLines
            ),
            Rule(
                "NoSpaceBeforeTemplateMiddleAndTail",
                anyToken,
                Slice<Kind>.FromArray([Kind.TemplateMiddle, Kind.TemplateTail]),
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBracesOption), IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // No space after { and before } in JSX expression
            Rule(
                "SpaceAfterOpenBraceInJsxExpression",
                Kind.OpenBraceToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBracesOption), IsNonJsxSameLineTokenContext, IsJsxExpressionContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceBeforeCloseBraceInJsxExpression",
                anyToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBracesOption), IsNonJsxSameLineTokenContext, IsJsxExpressionContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterOpenBraceInJsxExpression",
                Kind.OpenBraceToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBracesOption), IsNonJsxSameLineTokenContext, IsJsxExpressionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceBeforeCloseBraceInJsxExpression",
                anyToken,
                Kind.CloseBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBracesOption), IsNonJsxSameLineTokenContext, IsJsxExpressionContext]),
                RuleAction.DeleteSpace
            ), // Insert space after semicolon in for statement
            Rule(
                "SpaceAfterSemicolonInFor",
                Kind.SemicolonToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterSemicolonInForStatementsOption), IsNonJsxSameLineTokenContext, IsForContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterSemicolonInFor",
                Kind.SemicolonToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterSemicolonInForStatementsOption), IsNonJsxSameLineTokenContext, IsForContext]),
                RuleAction.DeleteSpace
            ), // Insert space before and after binary operators
            Rule(
                "SpaceBeforeBinaryOperator",
                anyToken,
                binaryOperators,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceBeforeAndAfterBinaryOperatorsOption), IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "SpaceAfterBinaryOperator",
                binaryOperators,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceBeforeAndAfterBinaryOperatorsOption), IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeBinaryOperator",
                anyToken,
                binaryOperators,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceBeforeAndAfterBinaryOperatorsOption), IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterBinaryOperator",
                binaryOperators,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceBeforeAndAfterBinaryOperatorsOption), IsNonJsxSameLineTokenContext, IsBinaryOpContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceBeforeOpenParenInFuncDecl",
                anyToken,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceBeforeFunctionParenthesisOption), IsNonJsxSameLineTokenContext, IsFunctionDeclContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeOpenParenInFuncDecl",
                anyToken,
                Kind.OpenParenToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceBeforeFunctionParenthesisOption), IsNonJsxSameLineTokenContext, IsFunctionDeclContext]),
                RuleAction.DeleteSpace
            ), // Open Brace braces after control block
            Rule(
                "NewLineBeforeOpenBraceInControl",
                controlOpenBraceLeftTokenRange,
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(PlaceOpenBraceOnNewLineForControlBlocksOption), IsControlDeclContext, IsBeforeMultilineBlockContext]),
                RuleAction.InsertNewLine,
                RuleFlags.CanDeleteNewLines
            ), // Open Brace braces after function
 // TypeScript: Function can have return types, which can be made of tons of different token kinds
            Rule(
                "NewLineBeforeOpenBraceInFunction",
                functionOpenBraceLeftTokenRange,
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(PlaceOpenBraceOnNewLineForFunctionsOption), IsFunctionDeclContext, IsBeforeMultilineBlockContext]),
                RuleAction.InsertNewLine,
                RuleFlags.CanDeleteNewLines
            ), // Open Brace braces after TypeScript module/class/interface
            Rule(
                "NewLineBeforeOpenBraceInTypeScriptDeclWithBlock",
                typeScriptOpenBraceLeftTokenRange,
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(PlaceOpenBraceOnNewLineForFunctionsOption), IsTypeScriptDeclWithBlockContext, IsBeforeMultilineBlockContext]),
                RuleAction.InsertNewLine,
                RuleFlags.CanDeleteNewLines
            ),
            Rule(
                "SpaceAfterTypeAssertion",
                Kind.GreaterThanToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceAfterTypeAssertionOption), IsNonJsxSameLineTokenContext, IsTypeAssertionContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceAfterTypeAssertion",
                Kind.GreaterThanToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceAfterTypeAssertionOption), IsNonJsxSameLineTokenContext, IsTypeAssertionContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "SpaceBeforeTypeAnnotation",
                anyToken,
                Slice<Kind>.FromArray([Kind.QuestionToken, Kind.ColonToken]),
                Slice<ContextPredicate>.FromArray([IsOptionEnabled(InsertSpaceBeforeTypeAnnotationOption), IsNonJsxSameLineTokenContext, IsTypeAnnotationContext]),
                RuleAction.InsertSpace
            ),
            Rule(
                "NoSpaceBeforeTypeAnnotation",
                anyToken,
                Slice<Kind>.FromArray([Kind.QuestionToken, Kind.ColonToken]),
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefined(InsertSpaceBeforeTypeAnnotationOption), IsNonJsxSameLineTokenContext, IsTypeAnnotationContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoOptionalSemicolon",
                Kind.SemicolonToken,
                anyTokenIncludingEOF,
                Slice<ContextPredicate>.FromArray([OptionEquals(SemicolonOption, SemicolonPreference.Remove), IsSemicolonDeletionContext]),
                RuleAction.DeleteToken
            ),
            Rule(
                "OptionalSemicolon",
                anyToken,
                anyTokenIncludingEOF,
                Slice<ContextPredicate>.FromArray([OptionEquals(SemicolonOption, SemicolonPreference.Insert), IsSemicolonInsertionContext]),
                RuleAction.InsertTrailingSemicolon
            ), ]);
        // These rules are lower in priority than user-configurable. Rules earlier in this list have priority over rules later in the list.
        var lowPriorityCommonRules = Slice<RuleSpec>.FromArray([ // Space after keyword but not before ; or : or ?
            Rule("NoSpaceBeforeSemicolon", anyToken, Kind.SemicolonToken, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.DeleteSpace),
            Rule(
                "SpaceBeforeOpenBraceInControl",
                controlOpenBraceLeftTokenRange,
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefinedOrTokensOnSameLine(PlaceOpenBraceOnNewLineForControlBlocksOption), IsControlDeclContext, IsNotFormatOnEnter, IsSameLineTokenOrBeforeBlockContext]),
                RuleAction.InsertSpace,
                RuleFlags.CanDeleteNewLines
            ),
            Rule(
                "SpaceBeforeOpenBraceInFunction",
                functionOpenBraceLeftTokenRange,
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefinedOrTokensOnSameLine(PlaceOpenBraceOnNewLineForFunctionsOption), IsFunctionDeclContext, IsBeforeBlockContext, IsNotFormatOnEnter, IsSameLineTokenOrBeforeBlockContext]),
                RuleAction.InsertSpace,
                RuleFlags.CanDeleteNewLines
            ),
            Rule(
                "SpaceBeforeOpenBraceInTypeScriptDeclWithBlock",
                typeScriptOpenBraceLeftTokenRange,
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsOptionDisabledOrUndefinedOrTokensOnSameLine(PlaceOpenBraceOnNewLineForFunctionsOption), IsTypeScriptDeclWithBlockContext, IsNotFormatOnEnter, IsSameLineTokenOrBeforeBlockContext]),
                RuleAction.InsertSpace,
                RuleFlags.CanDeleteNewLines
            ),
            Rule("NoSpaceBeforeComma", anyToken, Kind.CommaToken, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.DeleteSpace), // No space before and after indexer `x[]ast.Kind{}`
            Rule(
                "NoSpaceBeforeOpenBracket",
                AnyTokenExcept(Kind.AsyncKeyword, Kind.CaseKeyword),
                Kind.OpenBracketToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ),
            Rule(
                "NoSpaceAfterCloseBracket",
                Kind.CloseBracketToken,
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNotBeforeBlockInFunctionDeclarationContext]),
                RuleAction.DeleteSpace
            ),
            Rule("SpaceAfterSemicolon", Kind.SemicolonToken, anyToken, Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]), RuleAction.InsertSpace), // Remove extra space between for and await
            Rule(
                "SpaceBetweenForAndAwaitKeyword",
                Kind.ForKeyword,
                Kind.AwaitKeyword,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ), // Remove extra spaces between ... and type name in tuple spread
            Rule(
                "SpaceBetweenDotDotDotAndTypeName",
                Kind.DotDotDotToken,
                typeNames,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.DeleteSpace
            ), // Add a space between statements. All keywords except (do,else,case) has open/close parens after them.
 // So, we have a rule to add a space for []ast.Kind{),Any}, []ast.Kind{do,Any}, []ast.Kind{else,Any}, and []ast.Kind{case,Any}
            Rule(
                "SpaceBetweenStatements",
                Slice<Kind>.FromArray([Kind.CloseParenToken, Kind.DoKeyword, Kind.ElseKeyword, Kind.CaseKeyword]),
                anyToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext, IsNonJsxElementOrFragmentContext, IsNotForContext]),
                RuleAction.InsertSpace
            ), // This low-pri rule takes care of "try {", "catch {" and "finally {" in case the rule SpaceBeforeOpenBraceInControl didn't execute on FormatOnEnter.
            Rule(
                "SpaceAfterTryCatchFinally",
                Slice<Kind>.FromArray([Kind.TryKeyword, Kind.CatchKeyword, Kind.FinallyKeyword]),
                Kind.OpenBraceToken,
                Slice<ContextPredicate>.FromArray([IsNonJsxSameLineTokenContext]),
                RuleAction.InsertSpace
            ), ]);
        var result = Slice<RuleSpec>.Make(0, highPriorityCommonRules.Length + userConfigurableRules.Length + lowPriorityCommonRules.Length);
        result = result.Append(highPriorityCommonRules);
        result = result.Append(userConfigurableRules);
        result = result.Append(lowPriorityCommonRules);
        return result;
    }

    // rules.go:428. The variadic caller owns its argument slice.
    private static TokenRange TokenRangeFrom(params Kind[] tokens) => new(Slice<Kind>.FromArray(tokens), true);
    // rules.go:435. Append retains or grows the original prefix header exactly as the Go operation.
    private static TokenRange TokenRangeFromEx(Slice<Kind> prefix, params Kind[] tokens) => new(prefix.Append(Slice<Kind>.FromArray(tokens)), true);
    // rules.go:443.
    private static TokenRange TokenRangeFromRange(Kind start, Kind end)
    {
        var tokens = Slice<Kind>.Make(0, (int)end - (int)start + 1);
        for (var token = start; token <= end; token++)
            tokens = tokens.Append(token);
        return new(tokens, true);
    }
}
