// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/inlay_hints.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Complete owner; original editor-response acceptance remains pending.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.SpanMap;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal (InlayHintsOrNull Result, Exception? Error) ProvideInlayHint(ProtocolRequestContext context, InlayHintParams parameters)
    {
        var userPreferences = UserPreferences();
        var preferences = userPreferences.InlayHintsPreferences;
        if (!isAnyInlayHintEnabled(preferences))
            return (new(), null);
        var(program, file) = getProgramAndFile(parameters.TextDocument.Uri);
        var quote = LsUtil.GetQuotePreference(file, userPreferences);
        var mappedRanges = converters.FromLSPRangeIntersectingForSourceFile(file, parameters.Range, Feature.InlayHints);
        var result = Slice<InlayHint?>.Make(0, mappedRanges.Length);
        foreach (var mapped in mappedRanges)
        {
            var projection = mapped.Script;
            var(checker, done) = program.GetTypeCheckerForFile(context, projection);
            try
            {
                var state = new InlayHintState(context, mapped.Span, preferences, quote, projection, checker, converters);
                state.Visit(projection);
                result = result.Append(state.Result);
            }
            finally
            {
                done();
            }
        }

        return (new() { InlayHints = result }, null);
    }

    private sealed partial class InlayHintState(ProtocolRequestContext context, TextRange span, InlayHintsPreferences preferences, QuotePreference quotePreference, SourceFile file, Checker checker, Converters converters)
    {
        internal Slice<InlayHint?> Result;
        // CLR bound method values are objects. Retain one for this request-owned
        // state instead of creating a delegate at every ForEachChild call.
        private Visitor? childVisitor;
        internal bool Visit(Node node)
        {
            if (node is null || node.End() - node.Pos() == 0 || (node.Flags & NodeFlags.Reparsed) != 0)
                return false;
            switch (node.Kind)
            {
                case Kind.ModuleDeclaration:
                case Kind.ClassDeclaration:
                case Kind.InterfaceDeclaration:
                case Kind.FunctionDeclaration:
                case Kind.ClassExpression:
                case Kind.FunctionExpression:
                case Kind.MethodDeclaration:
                case Kind.ArrowFunction:
                    if (context.Cancellation.IsCancellationRequested)
                        return true;
                    break;
            }

            if (!span.Intersects(node.Loc))
                return false;
            if (Ast.IsTypeNode(node) && !Ast.IsExpressionWithTypeArguments(node))
                return false;
            if ((preferences.IncludeInlayVariableTypeHints == Tristate.True) && Ast.IsVariableDeclaration(node))
                VisitVariableLikeDeclaration(node);
            else if ((preferences.IncludeInlayPropertyDeclarationTypeHints == Tristate.True) && Ast.IsPropertyDeclaration(node))
                VisitVariableLikeDeclaration(node);
            else if ((preferences.IncludeInlayEnumMemberValueHints == Tristate.True) && Ast.IsEnumMember(node))
                VisitEnumMember(node);
            else if (shouldShowParameterNameHints(preferences) && (Ast.IsCallExpression(node) || Ast.IsNewExpression(node)))
                VisitCallOrNewExpression(node);
            else
            {
                if ((preferences.IncludeInlayFunctionParameterTypeHints == Tristate.True) && Ast.IsFunctionLikeDeclaration(node) && Ast.HasContextSensitiveParameters(node))
                    VisitFunctionLikeForParameterType(node);
                if ((preferences.IncludeInlayFunctionLikeReturnTypeHints == Tristate.True) && isSignatureSupportingReturnAnnotation(node))
                    VisitFunctionDeclarationLikeForReturnType(node);
            }

            return node.ForEachChild(childVisitor ??= Visit);
        }

        private void VisitFunctionDeclarationLikeForReturnType(Node declaration)
        {
            if (Ast.IsArrowFunction(declaration) && AstNav.FindChildOfKind(declaration, Kind.OpenParenToken, file)is null)
                return;
            if (declaration.Type()is not null || declaration.Body()is null)
                return;
            var signature = checker.GetSignatureFromDeclaration(declaration);
            if (signature is null)
                return;
            var predicate = checker.GetTypePredicateOfSignature(signature);
            if (predicate is not null && predicate.Type()is not null)
            {
                AddTypeHints(TypePredicateToInlayHintParts(predicate), GetTypeAnnotationPosition(declaration));
                return;
            }

            var returnType = checker.GetReturnTypeOfSignature(signature);
            if (isModuleReferenceType(returnType))
                return;
            AddTypeHints(TypeToInlayHintParts(returnType), GetTypeAnnotationPosition(declaration));
        }
    }

    private static bool shouldShowParameterNameHints(InlayHintsPreferences preferences) => preferences.IncludeInlayParameterNameHints == IncludeInlayParameterNameHints.Literals || preferences.IncludeInlayParameterNameHints == IncludeInlayParameterNameHints.All;
    private static bool shouldShowLiteralParameterNameHintsOnly(InlayHintsPreferences preferences) => preferences.IncludeInlayParameterNameHints == IncludeInlayParameterNameHints.Literals;
    private static bool isSignatureSupportingReturnAnnotation(Node node) => Ast.IsArrowFunction(node) || Ast.IsFunctionExpression(node) || Ast.IsFunctionDeclaration(node) || Ast.IsMethodDeclaration(node) || Ast.IsGetAccessorDeclaration(node);
    private static bool isHintableDeclaration(Node node)
    {
        if ((Ast.IsPartOfParameterDeclaration(node) || Ast.IsVariableDeclaration(node) && Ast.IsVarConst(node)) && node.Initializer()is { } initial)
        {
            var initializer = Ast.SkipParentheses(initial);
            return !(isHintableLiteral(initializer) || Ast.IsNewExpression(initializer) || Ast.IsObjectLiteralExpression(initializer) || Ast.IsAssertionExpression(initializer));
        }

        return true;
    }

    private static bool isHintableLiteral(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PrefixUnaryExpression:
                var operand = node.AsPrefixUnaryExpression().Operand;
                return Ast.IsLiteralExpression(operand) || Ast.IsIdentifier(operand) && Ast.IsInfinityOrNaNString(operand.TextValue());
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateExpression:
                return true;
            case Kind.Identifier:
                var name = node.TextValue();
                return name.EqualsBytes("undefined"u8) || Ast.IsInfinityOrNaNString(name);
        }

        return Ast.IsLiteralExpression(node);
    }

    private static bool isModuleReferenceType(TypeScript.Checking.Type type) => type.Symbol()is { } symbol && (symbol.Flags & SymbolFlags.Module) != 0;
    private static Node? getParameterDeclarationIdentifier(Symbol symbol) => symbol.ValueDeclaration is { } declaration && Ast.IsParameterDeclaration(declaration) && Ast.IsIdentifier(declaration.Name()) ? declaration.Name() : null;
    private static bool identifierOrAccessExpressionPostfixMatchesParameterName(Node expression, ByteStringValue parameterName)
    {
        if (Ast.IsIdentifier(expression))
            return expression.TextValue().EqualsValue(parameterName);
        if (Ast.IsPropertyAccessExpression(expression))
            return expression.Name().TextValue().EqualsValue(parameterName);
        return false;
    }

    private static bool isAnyInlayHintEnabled(InlayHintsPreferences preferences) => preferences.IncludeInlayParameterNameHints != IncludeInlayParameterNameHints.None || (preferences.IncludeInlayFunctionParameterTypeHints == Tristate.True) || (preferences.IncludeInlayVariableTypeHints == Tristate.True) || (preferences.IncludeInlayPropertyDeclarationTypeHints == Tristate.True) || (preferences.IncludeInlayFunctionLikeReturnTypeHints == Tristate.True) || (preferences.IncludeInlayEnumMemberValueHints == Tristate.True);
    private sealed partial class InlayHintState
    {
        private int GetTypeAnnotationPosition(Node declaration)
        {
            var token = AstNav.FindChildOfKind(declaration, Kind.CloseParenToken, file);
            return token is not null ? token.End() : declaration.ParameterList().End();
        }

        private bool LeadingCommentsContainsParameterName(Node node, ByteStringValue name)
        {
            if (!ScannerApi.IsIdentifierTextByteString(name, file.LanguageVariant))
                return false;
            var text = file.TextView();
            foreach (var range in getLeadingCommentRangesOfNode(node, file))
            {
                var comment = GoStrings.TrimFunc(ByteStringValue.FromView(text.Slice(range.Pos, range.End)), static rune => GoUnicode.IsSpace(rune) || rune == '/' || rune == '*');
                if (comment.EqualsValue(name))
                    return true;
            }

            return false;
        }

        private InlayHintLabelPart GetNodeDisplayPart(ByteStringValue text, Node node)
        {
            var sourceFile = Ast.GetSourceFileOfNode(node);
            int start = AstNav.GetStartOfNode(node, sourceFile, false);
            var result = new InlayHintLabelPart
            {
                Value = text
            };
            var(range, fidelity) = converters.ToLSPRangeForFeature(sourceFile, new TextRange(start, node.End()), Feature.InlayHints);
            if (fidelity.IsSingleSegment())
                result.Location = new()
                {
                    Uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(sourceFile.OriginalFileName()),
                    Range = range
                };
            return result;
        }

        private ByteStringValue GetLiteralText(Node node)
        {
            switch (node.Kind)
            {
                case Kind.StringLiteral:
                    if (quotePreference == QuotePreference.Single)
                        return ByteStringValue.Concat(ByteStringValue.FromString("'"), TypeScript.DiagnosticPrinting.PrinterUtilities.EscapeStringValue(node.TextValue(), TypeScript.DiagnosticPrinting.QuoteChar.SingleQuote), ByteStringValue.FromString("'"));
                    return ByteStringValue.Concat(ByteStringValue.FromString("\""), TypeScript.DiagnosticPrinting.PrinterUtilities.EscapeStringValue(node.TextValue(), TypeScript.DiagnosticPrinting.QuoteChar.DoubleQuote), ByteStringValue.FromString("\""));
                case Kind.TemplateHead:
                case Kind.TemplateMiddle:
                case Kind.TemplateTail:
                    var raw = TypeScript.DiagnosticPrinting.PrinterUtilities.templateRawTextValue(node);
                    if (raw.Length == 0)
                        raw = TypeScript.DiagnosticPrinting.PrinterUtilities.EscapeStringValue(node.TextValue(), TypeScript.DiagnosticPrinting.QuoteChar.Backtick);
                    return node.Kind == Kind.TemplateHead ? ByteStringValue.Concat(ByteStringValue.FromString("`"), raw, ByteStringValue.FromString("${")) : node.Kind == Kind.TemplateMiddle ? ByteStringValue.Concat(ByteStringValue.FromString("}"), raw, ByteStringValue.FromString("${")) : ByteStringValue.Concat(ByteStringValue.FromString("}"), raw, ByteStringValue.FromString("`"));
            }

            return node.TextValue();
        }

        private readonly record struct ParameterInfo(Node Parameter, ByteStringValue Name, bool IsRestParameter);
        private ParameterInfo? GetParameterIdentifierInfoAtPosition(Signature signature, int position)
        {
            var parameters = signature.Parameters();
            int count = parameters.Length - (signature.HasRestParameter() ? 1 : 0);
            if (position < count)
            {
                var identifier = getParameterDeclarationIdentifier(parameters[position]);
                return identifier is null ? null : new ParameterInfo(identifier, identifier.TextValue(), false);
            }

            Symbol? restParameter = null;
            Node? restIdentifier = null;
            if (count < parameters.Length)
            {
                restParameter = parameters[count];
                restIdentifier = getParameterDeclarationIdentifier(restParameter);
            }

            if (restIdentifier is null)
                return null;
            var restType = checker.GetTypeOfSymbol(restParameter!);
            if (restType.IsTupleType())
            {
                var elements = restType.Target().AsTupleType().ElementInfos();
                var associatedNames = Slice<Node?>.Make(0, elements.Length);
                foreach (var element in elements)
                    associatedNames = associatedNames.Append(element.LabeledDeclaration());
                int index = position - count;
                if (index < associatedNames.Length && associatedNames[index] is { } associated)
                {
                    Debug.Assert(Ast.IsIdentifier(associated.Name()));
                    bool rest = Ast.IsNamedTupleMember(associated) ? associated.AsNamedTupleMember().DotDotDotToken is not null : associated.AsParameterDeclaration().DotDotDotToken is not null;
                    return new ParameterInfo(associated.Name(), associated.Name().TextValue(), rest);
                }

                return null;
            }

            if (position == count)
                return new ParameterInfo(restIdentifier, ByteStringValue.FromString(restParameter!.Name), true);
            return null;
        }
    }

    private sealed partial class InlayHintState
    {
        private void VisitEnumMember(Node member)
        {
            if (member.Initializer()is not null)
                return;
            var value = checker.GetConstantValue(member);
            if (value is not null)
                AddEnumMemberValueHints(Evaluator.AnyToStringValue(value), member.End());
        }

        private void VisitVariableLikeDeclaration(Node declaration)
        {
            if (declaration.Initializer()is null && !(Ast.IsPropertyDeclaration(declaration) && (checker.GetTypeAtLocation(declaration).Flags() & TypeFlags.Any) == 0) || Ast.IsBindingPattern(declaration.Name()) || Ast.IsVariableDeclaration(declaration) && !isHintableDeclaration(declaration))
                return;
            if (declaration.Type()is not null)
                return;
            var type = checker.GetTypeAtLocation(declaration);
            if (isModuleReferenceType(type))
                return;
            var parts = TypeToInlayHintParts(type);
            ByteStringValue text = default;
            if (parts.String is { } value)
                text = value;
            else if (parts.InlayHintLabelParts is { } labels)
            {
                var builder = new GoBuilder();
                foreach (var label in labels)
                    builder.WriteString(label!.Value);
                text = builder.Value();
            }

            if (!(preferences.IncludeInlayVariableTypeHintsWhenTypeMatchesName == Tristate.True) && !Ast.IsComputedPropertyName(declaration.Name()) && UnicodeCase.EqualFold(declaration.Name().TextValue(), text))
                return;
            AddTypeHints(parts, declaration.Name().End());
        }

        private void VisitFunctionLikeForParameterType(Node node)
        {
            var signature = checker.GetSignatureFromDeclaration(node);
            if (signature is null)
                return;
            int position = 0;
            foreach (var parameter in node.Parameters())
            {
                if (isHintableDeclaration(parameter))
                {
                    var symbol = Ast.IsThisParameter(parameter) ? signature.ThisParameter() : signature.Parameters()[position];
                    AddParameterTypeHint(parameter, symbol);
                }

                if (Ast.IsThisParameter(parameter))
                    continue;
                position++;
            }
        }

        private void AddParameterTypeHint(Node node, Symbol? symbol)
        {
            if (node.Type()is not null || symbol is null)
                return;
            var hint = GetParameterDeclarationTypeHints(symbol);
            if (hint is null)
                return;
            int position = node.QuestionToken()is { } question ? question.End() : node.Name().End();
            AddTypeHints(hint, position);
        }

        private StringOrInlayHintLabelParts? GetParameterDeclarationTypeHints(Symbol symbol)
        {
            var declaration = symbol.ValueDeclaration;
            if (declaration is null || !Ast.IsParameterDeclaration(declaration))
                return null;
            var type = checker.GetTypeOfSymbolAtLocation(symbol, declaration);
            return isModuleReferenceType(type) ? null : TypeToInlayHintParts(type);
        }

        private StringOrInlayHintLabelParts TypeToInlayHintParts(TypeScript.Checking.Type type)
        {
            var flags = NodeBuilderFlags.IgnoreErrors | NodeBuilderFlags.AllowUniqueESSymbolType | NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope;
            var symbols = new Dictionary<Node, Symbol>(ReferenceEqualityComparer.Instance);
            // Original forces a fresh builder to collect identifier-symbol associations.
            var node = checker.TypeToTypeNode(type, null!, flags, symbols);
            Debug.Assert(node is not null, "should always get typenode");
            return new()
            {
                InlayHintLabelParts = GetInlayHintLabelParts(node!, symbols)
            };
        }

        private StringOrInlayHintLabelParts TypePredicateToInlayHintParts(TypePredicate predicate)
        {
            var flags = NodeBuilderFlags.IgnoreErrors | NodeBuilderFlags.AllowUniqueESSymbolType | NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope;
            var symbols = new Dictionary<Node, Symbol>(ReferenceEqualityComparer.Instance);
            var node = checker.TypePredicateToTypePredicateNode(predicate, null!, flags, symbols);
            Debug.Assert(node is not null, "should always get typePredicateNode");
            return new()
            {
                InlayHintLabelParts = GetInlayHintLabelParts(node!, symbols)
            };
        }

        private void AddTypeHints(StringOrInlayHintLabelParts hint, int position)
        {
            var(lsp, fidelity) = converters.ToLSPPositionForFeature(file, position, Feature.InlayHints);
            if (fidelity.IsNone())
                return;
            // The original union argument is a Go value; retain its borrowed headers
            // while giving this result its own mutable union owner.
            hint = new()
            {
                String = hint.String,
                InlayHintLabelParts = hint.InlayHintLabelParts
            };
            if (hint.String is { } text)
                hint.String = ByteStringValue.Concat(ByteStringValue.FromString(": "), text);
            else
                hint.InlayHintLabelParts = Slice<InlayHintLabelPart?>.FromArray([new() { Value = ByteStringValue.FromString(": ") }]).Append(hint.InlayHintLabelParts!.Value);
            Result = Result.Append(new InlayHint { Label = hint, Position = lsp, Kind = InlayHintKind.Type, PaddingLeft = true });
        }

        private void AddEnumMemberValueHints(ByteStringValue text, int position)
        {
            var(lsp, fidelity) = converters.ToLSPPositionForFeature(file, position, Feature.InlayHints);
            if (fidelity.IsNone())
                return;
            Result = Result.Append(new InlayHint { Label = new() { String = ByteStringValue.Concat(ByteStringValue.FromString("= "), text) }, Position = lsp, PaddingLeft = true });
        }

        private void AddParameterHints(ByteStringValue text, Node parameter, int position, bool firstVariadic)
        {
            var(lsp, fidelity) = converters.ToLSPPositionForFeature(file, position, Feature.InlayHints);
            if (fidelity.IsNone())
                return;
            var hintText = ByteStringValue.Concat(firstVariadic ? ByteStringValue.FromString("...") : ByteStringValue.FromString(""), text);
            var parts = Slice<InlayHintLabelPart?>.FromArray([GetNodeDisplayPart(hintText, parameter), new() { Value = ByteStringValue.FromString(":") }]);
            Result = Result.Append(new InlayHint { Label = new() { InlayHintLabelParts = parts }, Position = lsp, Kind = InlayHintKind.Parameter, PaddingRight = true });
        }

        private void VisitCallOrNewExpression(Node expression)
        {
            var arguments = expression.Arguments();
            if (arguments.Length == 0)
                return;
            var signature = checker.GetResolvedSignature(expression);
            if (signature is null)
                return;
            int signaturePosition = 0;
            foreach (var original in arguments)
            {
                var argument = Ast.SkipParentheses(original);
                if (shouldShowLiteralParameterNameHintsOnly(preferences) && !isHintableLiteral(argument))
                {
                    signaturePosition++;
                    continue;
                }

                int spreadArguments = 0;
                if (Ast.IsSpreadElement(argument))
                {
                    var spreadType = checker.GetTypeAtLocation(argument.Expression());
                    if (spreadType.IsTupleType())
                    {
                        var tuple = spreadType.Target().AsTupleType();
                        int length = tuple.FixedLength();
                        if (length == 0)
                            continue;
                        int firstOptional = GoSlices.IndexFunc(tuple.ElementFlags(), static flags => (flags & ElementFlags.Required) == 0);
                        int required = firstOptional < 0 ? length : firstOptional;
                        if (required > 0)
                            spreadArguments = required;
                    }
                }

                var identifier = GetParameterIdentifierInfoAtPosition(signature, signaturePosition);
                signaturePosition += spreadArguments > 0 ? spreadArguments : 1;
                if (identifier is null)
                    return;
                var info = identifier.Value;
                bool differs = (preferences.IncludeInlayParameterNameHintsWhenArgumentMatchesName == Tristate.True) || !identifierOrAccessExpressionPostfixMatchesParameterName(argument, info.Name);
                if (!differs && !info.IsRestParameter)
                    continue;
                if (LeadingCommentsContainsParameterName(argument, info.Name))
                    continue;
                AddParameterHints(info.Name, info.Parameter, AstNav.GetStartOfNode(original, file, false), info.IsRestParameter);
            }
        }
    }
}
