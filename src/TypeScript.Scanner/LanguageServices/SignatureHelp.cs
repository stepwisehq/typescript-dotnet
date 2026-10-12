// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/signaturehelp.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal static readonly Slice<ByteStringValue> SignatureHelpTriggerCharacters = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("("), ByteStringValue.FromString(","), ByteStringValue.FromString("<")]);
    internal static readonly Slice<ByteStringValue> SignatureHelpRetriggerCharacters = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(")")]);
    private sealed class CallInvocation
    {
        internal Node node = null!;
    }

    private sealed class TypeArgsInvocation
    {
        internal Node called = null!;
    }

    private sealed class ContextualInvocation
    {
        internal Signature signature = null!;
        internal Node node = null!;
        internal Symbol symbol = null!;
    }

    private sealed class Invocation
    {
        internal CallInvocation? callInvocation;
        internal TypeArgsInvocation? typeArgsInvocation;
        internal ContextualInvocation? contextualInvocation;
    }

    private struct SignatureInformation
    {
        internal ByteStringValue Label;
        internal ByteStringValue? Documentation;
        internal Slice<SignatureHelpParameter> Parameters;
        internal bool IsVariadic;
        internal Slice<VSClassifiedTextRun?> ColorizedRuns;
    }

    private sealed class SignatureHelpItemInfo
    {
        internal bool isVariadic;
        internal Slice<SignatureHelpParameter> parameters;
        internal DisplayPartsWriter writer = null!;
    }

    private struct SignatureHelpParameter
    {
        internal ParameterInformation? parameterInfo;
        internal bool isRest;
        internal bool isOptional;
    }

    private sealed class CandidateInfo
    {
        internal Slice<Signature> candidates;
        internal Signature? resolvedSignature;
    }

    private sealed class CandidateOrTypeInfo
    {
        internal CandidateInfo? candidateInfo;
        internal Symbol? typeInfo;
    }

    private sealed class ArgumentListInfo
    {
        internal bool isTypeParameterList;
        internal Invocation invocation = null!;
        internal TextRange argumentsSpan;
        internal int argumentIndex;
        internal int argumentCount;
    }

    private sealed class ContextualSignatureLocationInfo
    {
        internal TypeScript.Checking.Type contextualType = null!;
        internal int argumentIndex, argumentCount;
        internal TextRange argumentsSpan;
    }

    private sealed class ArgumentOrParameterListInfo
    {
        internal NodeList? list;
        internal int argumentIndex, argumentCount;
        internal TextRange argumentsSpan;
    }

    private sealed class ArgumentOrParameterListAndIndex
    {
        internal NodeList? list;
        internal int argumentIndex;
    }

    private enum SignatureHelpTriggerReasonKind
    {
        None,
        Invoked,
        CharacterTyped,
        Retriggered
    }

    private const TypeScript.NodeBuilding.Flags signatureHelpNodeBuilderFlags = TypeScript.NodeBuilding.Flags.OmitParameterModifiers | TypeScript.NodeBuilding.Flags.IgnoreErrors | TypeScript.NodeBuilding.Flags.UseAliasDefinedOutsideCurrentScope;
    // signaturehelp.go:49. Only single-segment projections admit a semantic query.
    internal (SignatureHelpOrNull Result, Exception? Error) ProvideSignatureHelp(ProtocolRequestContext context, DocumentUri documentUri, Position position, SignatureHelpContext? trigger)
    {
        var (program, file) = getProgramAndFile(documentUri);
        var positions = converters.FromLSPPositionForSourceFile(file, position, TypeScript.SpanMap.Feature.SignatureHelp);
        foreach (var projection in positions)
        {
            if (!projection.Fidelity.IsSingleSegment())
                continue;
            var items = GetSignatureHelpItems(context, projection.Position, program, projection.Script, trigger);
            if (items != null)
                return (new SignatureHelpOrNull { SignatureHelp = items }, null);
        }

        return (new SignatureHelpOrNull(), null);
    }

    // signaturehelp.go:75. Hold the actual checker lease across all candidate queries.
    internal SignatureHelp? GetSignatureHelpItems(ProtocolRequestContext context, int position, TypeScript.Compiler.Program program, SourceFile file, SignatureHelpContext? trigger)
    {
        var (checker, done) = program.GetTypeCheckerForFile(context, file);
        try
        {
            var startingToken = AstNav.FindPrecedingToken(file, position);
            if (startingToken == null)
                return null;
            var reason = SignatureHelpTriggerReasonKind.None;
            if (trigger != null)
            {
                switch (trigger.TriggerKind)
                {
                    case SignatureHelpTriggerKind.TriggerCharacter:
                        reason = trigger.TriggerCharacter.HasValue ? trigger.IsRetrigger ? SignatureHelpTriggerReasonKind.Retriggered : SignatureHelpTriggerReasonKind.CharacterTyped : SignatureHelpTriggerReasonKind.Invoked;
                        break;
                    case SignatureHelpTriggerKind.ContentChange:
                        reason = trigger.IsRetrigger ? SignatureHelpTriggerReasonKind.Retriggered : SignatureHelpTriggerReasonKind.CharacterTyped;
                        break;
                    default:
                        reason = SignatureHelpTriggerReasonKind.Invoked;
                        break;
                }
            }

            bool onlyUseSyntacticOwners = reason == SignatureHelpTriggerReasonKind.CharacterTyped;
            if (onlyUseSyntacticOwners && (IsInString(file, position, startingToken) || isInComment(file, position, startingToken) != null))
                return null;
            var arguments = getContainingArgumentInfo(startingToken, file, checker, reason == SignatureHelpTriggerReasonKind.Invoked, position);
            if (arguments == null || context.Cancellation.IsCancellationRequested)
                return null;
            var candidates = getCandidateOrTypeInfo(arguments, checker, file, startingToken, onlyUseSyntacticOwners);
            if (context.Cancellation.IsCancellationRequested)
                return null;
            if (candidates == null)
                return Ast.IsSourceFileJS(file) ? createJSSignatureHelpItems(context, arguments, program, checker) : null;
            if (candidates.candidateInfo is { } candidate)
                return createSignatureHelpItems(context, candidate.candidates, candidate.resolvedSignature, arguments, file, checker, onlyUseSyntacticOwners);
            return createTypeHelpItems(context, candidates.typeInfo!, arguments, file, checker);
        }
        finally
        {
            done();
        }
    }

    // signaturehelp.go:169. Preserve the client-specific active-parameter placement.
    private static SignatureHelp? createTypeHelpItems(ProtocolRequestContext context, Symbol symbol, ArgumentListInfo arguments, SourceFile file, Checker checker)
    {
        var typeParameters = checker.GetLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol);
        if (typeParameters.IsNil)
            return null;
        var item = getTypeHelpItem(symbol, typeParameters, getEnclosingDeclarationFromInvocation(arguments.invocation), file, checker);
        bool perSignature = context.ClientCapabilities.TextDocument.SignatureHelp.SignatureInformation.ActiveParameterSupport;
        var parameters = Slice<ParameterInformation?>.Make(item.Parameters.Length);
        for (int i = 0; i < item.Parameters.Length; i++)
            parameters[i] = item.Parameters[i].parameterInfo;
        var signature = new TypeScript.Lsp.Protocol.SignatureInformation
        {
            Label = item.Label,
            Parameters = parameters
        };
        if (perSignature && item.Parameters.Length > 0)
            signature.ActiveParameter = new UintegerOrNull
            {
                Uinteger = unchecked((uint)arguments.argumentIndex)
            };
        var help = new SignatureHelp
        {
            Signatures = Slice<TypeScript.Lsp.Protocol.SignatureInformation?>.FromArray([signature]),
            ActiveSignature = 0
        };
        if (!perSignature && item.Parameters.Length > 0)
            help.ActiveParameter = new UintegerOrNull
            {
                Uinteger = unchecked((uint)arguments.argumentIndex)
            };
        return help;
    }

    // signaturehelp.go:211. Parameter printing shares the per-query factory owner.
    private static SignatureInformation getTypeHelpItem(Symbol symbol, Slice<TypeScript.Checking.Type> typeParameters, Node enclosing, SourceFile file, Checker checker)
    {
        var context = new TypeScript.DiagnosticPrinting.EmitContext();
        var builder = Checker.NewNodeBuilder(checker, context);
        var printer = TypeScript.DiagnosticPrinting.Printer.NewPrinter(new TypeScript.DiagnosticPrinting.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
        var parameters = Slice<SignatureHelpParameter>.Make(typeParameters.Length);
        for (int i = 0; i < typeParameters.Length; i++)
            parameters[i] = createSignatureHelpParameterForTypeParameter(typeParameters[i], file, enclosing, builder, printer);
        var display = new GoBuilder();
        display.WriteString(checker.SymbolToStringExValue(symbol, null!, SymbolFlags.All));
        if (parameters.Length != 0)
        {
            display.WriteString(ScannerApi.TokenToString(Kind.LessThanToken));
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    display.WriteString(", ");
                display.WriteString(parameters[i].parameterInfo!.Label.String!.Value);
            }

            display.WriteString(ScannerApi.TokenToString(Kind.GreaterThanToken));
        }

        return new SignatureInformation
        {
            Label = display.Value(),
            Parameters = parameters
        };
    }

    // signaturehelp.go:246. Preserve the original JS-only name heuristic.
    private SignatureHelp? createJSSignatureHelpItems(ProtocolRequestContext context, ArgumentListInfo arguments, TypeScript.Compiler.Program program, Checker checker)
    {
        if (arguments.invocation.contextualInvocation != null)
            return null;
        var expression = getExpressionFromInvocation(arguments);
        if (!Ast.IsPropertyAccessExpression(expression))
            return null;
        var name = expression.Name().TextValue();
        if (name.Length == 0)
            return null;
        foreach (var file in program.GetSourceFiles())
        {
            var result = findSignatureHelpFromNamedDeclarations(context, file, name, arguments, checker);
            if (result != null)
                return result;
        }

        return null;
    }

    // signaturehelp.go:269. A single query-local recursive delegate is reused throughout.
    private SignatureHelp? findSignatureHelpFromNamedDeclarations(ProtocolRequestContext context, SourceFile file, ByteStringValue name, ArgumentListInfo arguments, Checker checker)
    {
        SignatureHelp? result = null;
        Visitor visit = null!;
        visit = node =>
        {
            if (result != null)
                return true;
            if (Ast.GetDeclarationNameValue(node).EqualsValue(name) && node.Symbol() is { } symbol)
            {
                var type = checker.GetTypeOfSymbolAtLocation(symbol, node);
                if (type != null)
                {
                    var signatures = checker.GetCallSignatures(type);
                    if (signatures.Length > 0)
                    {
                        result = createSignatureHelpItems(context, signatures, signatures[0], arguments, file, checker, true);
                        if (result != null)
                            return true;
                    }
                }
            }

            node.ForEachChild(visit);
            return result != null;
        };
        visit(file);
        return result;
    }

    // signaturehelp.go:297. Preserve expanded-candidate selection and protocol field placement.
    private SignatureHelp? createSignatureHelpItems(ProtocolRequestContext context, Slice<Signature> candidates, Signature? resolved, ArgumentListInfo arguments, SourceFile file, Checker checker, bool useFullPrefix)
    {
        ref readonly var caps = ref context.ClientCapabilities;
        var docFormat = ProtocolUtilities.PreferredMarkupKind(caps.TextDocument.SignatureHelp.SignatureInformation.DocumentationFormat);
        bool visualStudio = caps.VSSupportsVisualStudioExtensions;
        var enclosing = getEnclosingDeclarationFromInvocation(arguments.invocation);
        if (enclosing == null)
            return null;
        Symbol? target;
        if (arguments.invocation.contextualInvocation is { } contextual)
            target = contextual.symbol;
        else
        {
            target = checker.GetSymbolAtLocation(getExpressionFromInvocation(arguments));
            if (target == null && useFullPrefix && resolved!.Declaration() is { } declaration)
                target = declaration.Symbol();
        }

        var targetDisplay = new GoBuilder();
        if (target != null && !target.NameValue.StartsWith(ByteStringValue.FromString(Ast.InternalSymbolNamePrefix)))
            targetDisplay.WriteString(useFullPrefix ? checker.SymbolToStringExValue(target, file, SymbolFlags.None, SymbolFormatFlags.UseAliasDefinedOutsideCurrentScope) : checker.SymbolToStringExValue(target, null!, SymbolFlags.All));
        var items = Slice<Slice<SignatureInformation>>.Make(candidates.Length);
        for (int i = 0; i < candidates.Length; i++)
            items[i] = getSignatureHelpItem(candidates[i], arguments.isTypeParameterList, targetDisplay.Value(), target, enclosing, file, checker, docFormat, visualStudio);
        int selected = 0, seen = 0;
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (candidates[i] == resolved)
            {
                selected = seen;
                if (item.Length > 1)
                    for (int j = 0; j < item.Length; j++)
                        if (item[j].IsVariadic || item[j].Parameters.Length >= arguments.argumentCount)
                        {
                            selected = seen + j;
                            break;
                        }
            }

            seen += item.Length;
        }

        Debug.Assert(selected != -1);
        var flattened = Slice<SignatureInformation>.Make(0);
        foreach (var item in items)
            flattened = flattened.Append(item);
        if (flattened.Length == 0)
            return null;
        bool perSignature = caps.TextDocument.SignatureHelp.SignatureInformation.ActiveParameterSupport;
        bool supportsNull = caps.TextDocument.SignatureHelp.SignatureInformation.NoActiveParameterSupport;
        var signatures = Slice<TypeScript.Lsp.Protocol.SignatureInformation?>.Make(flattened.Length);
        for (int i = 0; i < flattened.Length; i++)
        {
            var item = flattened[i];
            var parameters = Slice<ParameterInformation?>.Make(item.Parameters.Length);
            for (int j = 0; j < item.Parameters.Length; j++)
                parameters[j] = item.Parameters[j].parameterInfo;
            var signature = new TypeScript.Lsp.Protocol.SignatureInformation
            {
                Label = item.Label,
                Documentation = item.Documentation is { } documentation ? new StringOrMarkupContent
                {
                    MarkupContent = new MarkupContent
                    {
                        Kind = docFormat,
                        Value = documentation
                    }
                }

                : null,
                Parameters = parameters
            };
            if (item.ColorizedRuns.Length > 0)
                signature.VSColorizedLabel = new VSClassifiedTextElement
                {
                    Runs = item.ColorizedRuns
                };
            if (perSignature)
                signature.ActiveParameter = computeActiveParameter(item, arguments.argumentIndex, supportsNull);
            signatures[i] = signature;
        }

        var help = new SignatureHelp
        {
            Signatures = signatures,
            ActiveSignature = unchecked((uint)selected)
        };
        if (!perSignature)
            help.ActiveParameter = computeActiveParameter(flattened[selected], arguments.argumentIndex, supportsNull);
        return help;
    }

    private UintegerOrNull? computeActiveParameter(SignatureInformation signature, int argumentIndex, bool supportsNull)
    {
        int count = signature.Parameters.Length;
        if (count == 0)
            return null;
        uint active = unchecked((uint)argumentIndex);
        if (signature.IsVariadic)
        {
            int firstRest = -1;
            for (int i = 0; i < count; i++)
                if (signature.Parameters[i].isRest)
                {
                    firstRest = i;
                    break;
                }

            if (firstRest > -1 && firstRest < count - 1)
                return supportsNull ? new UintegerOrNull() : new UintegerOrNull
                {
                    Uinteger = unchecked((uint)count)
                };
            if (active > (uint)(count - 1))
                active = (uint)(count - 1);
        }

        return new UintegerOrNull
        {
            Uinteger = active
        };
    }

    private Slice<SignatureInformation> getSignatureHelpItem(Signature candidate, bool isTypeParameterList, ByteStringValue targetDisplay, Symbol? targetSymbol, Node enclosing, SourceFile file, Checker checker, MarkupKind docFormat, bool visualStudio)
    {
        var context = new TypeScript.DiagnosticPrinting.EmitContext();
        var infos = isTypeParameterList ? itemInfoForTypeParameters(candidate, checker, enclosing, file, docFormat, visualStudio, context) : itemInfoForParameters(candidate, checker, enclosing, file, docFormat, visualStudio, context);
        var suffix = returnTypeToDisplayParts(candidate, checker, enclosing, file, visualStudio, context);
        ByteStringValue? documentation = null;
        if (candidate.Declaration() is { } declaration)
        {
            var doc = getDocumentationFromDeclaration(documentationLocationMapper(TypeScript.SpanMap.Feature.SignatureHelp), checker, null, declaration, null, docFormat, true);
            if (doc.Length != 0)
                documentation = doc;
        }

        var result = Slice<SignatureInformation>.Make(infos.Length);
        for (int i = 0; i < infos.Length; i++)
        {
            var info = infos[i];
            var writer = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
            if (targetDisplay.Length != 0)
                writer.WriteSymbol(targetDisplay, targetSymbol!);
            writer.WriteFrom(info.writer);
            writer.WriteFrom(suffix);
            result[i] = new SignatureInformation
            {
                Label = writer.TextValue(),
                Documentation = documentation,
                Parameters = info.parameters,
                IsVariadic = info.isVariadic,
                ColorizedRuns = writer.GetRuns()
            };
        }

        return result;
    }

    private static DisplayPartsWriter returnTypeToDisplayParts(Signature candidate, Checker checker, Node enclosing, SourceFile file, bool visualStudio, TypeScript.DiagnosticPrinting.EmitContext context)
    {
        var writer = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
        writer.WritePunctuation(": ");
        var predicate = checker.GetTypePredicateOfSignature(candidate);
        if (predicate != null)
            writer.Write(checker.TypePredicateToStringValue(predicate));
        else
        {
            var type = checker.GetReturnTypeOfSignature(candidate);
            var node = checker.TypeToTypeNode(type, enclosing, signatureHelpNodeBuilderFlags, null);
            if (node != null)
            {
                var printer = TypeScript.DiagnosticPrinting.Printer.NewPrinter(new TypeScript.DiagnosticPrinting.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
                var temporary = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
                printer.Write(node, file, temporary, null);
                writer.WriteFrom(temporary);
            }
            else
                writer.Write(checker.TypeToStringValue(type));
        }

        return writer;
    }

    private Slice<SignatureHelpItemInfo> itemInfoForTypeParameters(Signature candidate, Checker checker, Node enclosing, SourceFile file, MarkupKind docFormat, bool visualStudio, TypeScript.DiagnosticPrinting.EmitContext context)
    {
        var builder = Checker.NewNodeBuilder(checker, context);
        var printer = TypeScript.DiagnosticPrinting.Printer.NewPrinter(new TypeScript.DiagnosticPrinting.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
        var typeParameters = candidate.Target() is { } target ? target.TypeParameters() : candidate.TypeParameters();
        var signatureParameters = Slice<SignatureHelpParameter>.Make(typeParameters.Length);
        for (int i = 0; i < typeParameters.Length; i++)
            signatureParameters[i] = createSignatureHelpParameterForTypeParameter(typeParameters[i], file, enclosing, builder, printer);
        var thisParameter = Slice<SignatureHelpParameter>.Make(0);
        if (candidate.ThisParameter() is { } @this)
            thisParameter = Slice<SignatureHelpParameter>.FromArray([createSignatureHelpParameterForParameter(@this, enclosing, builder, printer, file, checker, docFormat)]);
        var prefix = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
        prefix.WritePunctuation(ScannerApi.TokenToString(Kind.LessThanToken));
        for (int i = 0; i < signatureParameters.Length; i++)
        {
            if (i > 0)
                prefix.WritePunctuation(", ");
            prefix.WriteClassified(signatureParameters[i].parameterInfo!.Label.String!.Value, ClassificationTypeName.TypeParameterName);
        }

        prefix.WritePunctuation(ScannerApi.TokenToString(Kind.GreaterThanToken));
        var lists = checker.GetExpandedParameters(candidate, false);
        if (lists.Length != 0)
            prefix.WritePunctuation(ScannerApi.TokenToString(Kind.OpenParenToken));
        var result = Slice<SignatureHelpItemInfo>.Make(lists.Length);
        for (int i = 0; i < lists.Length; i++)
        {
            var writer = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
            writer.WriteFrom(prefix);
            var parameters = thisParameter;
            for (int j = 0; j < lists[i].Length; j++)
            {
                var parameter = lists[i][j];
                var node = builder.SymbolToParameterDeclaration(parameter, enclosing, signatureHelpNodeBuilderFlags, TypeScript.NodeBuilding.InternalFlags.None, null);
                if (j > 0)
                    writer.WritePunctuation(", ");
                var temporary = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
                printer.Write(node, file, temporary, null);
                context.Factory.ReleaseArenas();
                var label = temporary.TextValue();
                writer.WriteFrom(temporary);
                parameters = parameters.Append(createSignatureHelpParameterFromLabel(parameter, label, checker, docFormat));
            }

            writer.WritePunctuation(ScannerApi.TokenToString(Kind.CloseParenToken));
            // The source renders ordinary parameters but returns the type-parameter header here.
            result[i] = new SignatureHelpItemInfo
            {
                parameters = signatureParameters,
                writer = writer
            };
        }

        return result;
    }

    private Slice<SignatureHelpItemInfo> itemInfoForParameters(Signature candidate, Checker checker, Node enclosing, SourceFile file, MarkupKind docFormat, bool visualStudio, TypeScript.DiagnosticPrinting.EmitContext context)
    {
        var builder = Checker.NewNodeBuilder(checker, context);
        var printer = TypeScript.DiagnosticPrinting.Printer.NewPrinter(new TypeScript.DiagnosticPrinting.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
        var signatureParameters = Slice<SignatureHelpParameter>.Make(candidate.TypeParameters().Length);
        for (int i = 0; i < signatureParameters.Length; i++)
            signatureParameters[i] = createSignatureHelpParameterForTypeParameter(candidate.TypeParameters()[i], file, enclosing, builder, printer);
        var prefix = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
        if (signatureParameters.Length != 0)
        {
            prefix.WritePunctuation(ScannerApi.TokenToString(Kind.LessThanToken));
            for (int i = 0; i < signatureParameters.Length; i++)
            {
                if (i > 0)
                    prefix.WritePunctuation(", ");
                prefix.WriteClassified(signatureParameters[i].parameterInfo!.Label.String!.Value, ClassificationTypeName.TypeParameterName);
            }

            prefix.WritePunctuation(ScannerApi.TokenToString(Kind.GreaterThanToken));
        }

        var lists = checker.GetExpandedParameters(candidate, false);
        if (lists.Length != 0)
            prefix.WritePunctuation(ScannerApi.TokenToString(Kind.OpenParenToken));
        bool isVariadic(Slice<Symbol> parameters)
        {
            if (!checker.HasEffectiveRestParameter(candidate))
                return false;
            if (lists.Length == 1)
                return true;
            return parameters.Length != 0 && parameters[parameters.Length - 1] != null && (parameters[parameters.Length - 1].CheckFlags & CheckFlags.RestParameter) != 0;
        }

        var result = Slice<SignatureHelpItemInfo>.Make(lists.Length);
        for (int i = 0; i < lists.Length; i++)
        {
            var parameters = Slice<SignatureHelpParameter>.Make(lists[i].Length);
            var writer = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
            writer.WriteFrom(prefix);
            for (int j = 0; j < lists[i].Length; j++)
            {
                var parameter = lists[i][j];
                var node = builder.SymbolToParameterDeclaration(parameter, enclosing, signatureHelpNodeBuilderFlags, TypeScript.NodeBuilding.InternalFlags.None, null);
                if (j > 0)
                    writer.WritePunctuation(", ");
                var temporary = DisplayPartsWriter.NewDisplayPartsWriter(visualStudio);
                printer.Write(node, file, temporary, null);
                context.Factory.ReleaseArenas();
                var label = temporary.TextValue();
                writer.WriteFrom(temporary);
                parameters[j] = createSignatureHelpParameterFromLabel(parameter, label, checker, docFormat);
            }

            writer.WritePunctuation(ScannerApi.TokenToString(Kind.CloseParenToken));
            result[i] = new SignatureHelpItemInfo
            {
                isVariadic = isVariadic(lists[i]),
                parameters = parameters,
                writer = writer
            };
        }

        return result;
    }

    private SignatureHelpParameter createSignatureHelpParameterFromLabel(Symbol parameter, ByteStringValue label, Checker checker, MarkupKind docFormat)
    {
        StringOrMarkupContent? documentation = null;
        if (parameter.ValueDeclaration is { } declaration)
        {
            var doc = getDocumentationFromDeclaration(documentationLocationMapper(TypeScript.SpanMap.Feature.SignatureHelp), checker, null, declaration, null, docFormat, true);
            if (doc.Length != 0)
                documentation = new StringOrMarkupContent
                {
                    MarkupContent = new MarkupContent
                    {
                        Kind = docFormat,
                        Value = doc
                    }
                };
        }

        return new SignatureHelpParameter
        {
            parameterInfo = new ParameterInformation
            {
                Label = new StringOrTuple
                {
                    String = label
                },
                Documentation = documentation
            },
            isRest = (parameter.CheckFlags & CheckFlags.RestParameter) != 0,
            isOptional = (parameter.CheckFlags & CheckFlags.OptionalParameter) != 0
        };
    }

    private SignatureHelpParameter createSignatureHelpParameterForParameter(Symbol parameter, Node enclosing, NodeBuilder builder, TypeScript.DiagnosticPrinting.Printer printer, SourceFile file, Checker checker, MarkupKind docFormat)
    {
        try
        {
            var node = builder.SymbolToParameterDeclaration(parameter, enclosing, signatureHelpNodeBuilderFlags, TypeScript.NodeBuilding.InternalFlags.None, null);
            return createSignatureHelpParameterFromLabel(parameter, printer.EmitValue(node, file), checker, docFormat);
        }
        finally
        {
            builder.EmitContext().Factory.ReleaseArenas();
        }
    }

    // signaturehelp.go:705. Arena release follows every original parameter print.
    private static SignatureHelpParameter createSignatureHelpParameterForTypeParameter(TypeScript.Checking.Type type, SourceFile file, Node enclosing, NodeBuilder builder, TypeScript.DiagnosticPrinting.Printer printer)
    {
        try
        {
            var declaration = builder.TypeParameterToDeclaration(type, enclosing, signatureHelpNodeBuilderFlags, TypeScript.NodeBuilding.InternalFlags.None, null);
            var display = printer.EmitValue(declaration, file);
            return new SignatureHelpParameter
            {
                parameterInfo = new ParameterInformation
                {
                    Label = new StringOrTuple
                    {
                        String = display
                    }
                }
            };
        }
        finally
        {
            builder.EmitContext().Factory.ReleaseArenas();
        }
    }

    // signaturehelp.go:747/757. Preserve the invocation discriminator and true nil boundaries.
    private static Node getEnclosingDeclarationFromInvocation(Invocation invocation) => invocation.callInvocation != null
        ? invocation.callInvocation.node : invocation.typeArgsInvocation != null
        ? invocation.typeArgsInvocation.called : invocation.contextualInvocation!.node;
    private static Node getExpressionFromInvocation(ArgumentListInfo info) => info.invocation.callInvocation != null ? Ast.GetInvokedExpression(info.invocation.callInvocation.node) : info.invocation.typeArgsInvocation!.called;
    // signaturehelp.go:774. The variants preserve nil/empty candidate discrimination.
    private static CandidateOrTypeInfo? getCandidateOrTypeInfo(ArgumentListInfo info, Checker checker, SourceFile file, Node startingToken, bool onlyUseSyntacticOwners)
    {
        if (info.invocation.callInvocation is { } call)
        {
            if (onlyUseSyntacticOwners && !isSyntacticOwner(startingToken, call.node, file))
                return null;
            var (resolved, candidates) = Checker.GetResolvedSignatureForSignatureHelp(call.node, info.argumentCount, checker);
            if (candidates.Length == 0)
                return null;
            return new CandidateOrTypeInfo
            {
                candidateInfo = new CandidateInfo
                {
                    candidates = candidates,
                    resolvedSignature = resolved
                }
            };
        }

        if (info.invocation.typeArgsInvocation is { } types)
        {
            var called = types.called;
            var container = Ast.IsIdentifier(called) ? called.Parent : called;
            if (onlyUseSyntacticOwners && !containsPrecedingToken(startingToken, file, container))
                return null;
            var candidates = getPossibleGenericSignatures(called, info.argumentCount, checker);
            if (candidates.Length != 0)
                return new CandidateOrTypeInfo
                {
                    candidateInfo = new CandidateInfo
                    {
                        candidates = candidates,
                        resolvedSignature = candidates[0]
                    }
                };
            var symbol = checker.GetSymbolAtLocation(called);
            return symbol != null ? new CandidateOrTypeInfo
            {
                typeInfo = symbol
            }

            : null;
        }

        if (info.invocation.contextualInvocation is { } contextual)
            return new CandidateOrTypeInfo
            {
                candidateInfo = new CandidateInfo
                {
                    candidates = Slice<Signature>.FromArray([contextual.signature]),
                    resolvedSignature = contextual.signature
                }
            };
        // All three pointer variants returned above; Go fmt prints the empty struct here.
        Debug.Fail("Illegal value: {<nil> <nil> <nil>}");
        return null;
    }

    private static bool isSyntacticOwner(Node startingToken, Node node, SourceFile file)
    {
        if (!Ast.IsCallOrNewExpression(node))
            return false;
        var children = getChildrenFromNonJSDocNode(node, file);
        switch (startingToken.Kind)
        {
            case Kind.OpenParenToken:
            case Kind.CommaToken:
                foreach (var child in children)
                    if (child == startingToken)
                        return true;
                return false;
            case Kind.LessThanToken:
                return containsPrecedingToken(startingToken, file, node.Expression());
            default:
                return false;
        }
    }

    private static bool containsPrecedingToken(Node startingToken, SourceFile file, Node container)
    {
        int position = startingToken.Pos();
        var parent = startingToken.Parent;
        while (parent != null)
        {
            var preceding = AstNav.FindPrecedingTokenEx(file, position, parent, true);
            if (preceding != null)
                return RangeContainsRange(container.Loc, preceding.Loc);
            parent = parent.Parent;
        }

        return false;
    }

    // signaturehelp.go:868. The innermost boundary candidate is retained while walking outwards.
    private static ArgumentListInfo? getContainingArgumentInfo(Node node, SourceFile file, Checker checker, bool isManuallyInvoked, int position)
    {
        ArgumentListInfo? first = null;
        for (var current = node; !Ast.IsSourceFile(current) && (isManuallyInvoked || !Ast.IsBlock(current)); current = current.Parent)
        {
            Debug.Assert(RangeContainsRange(current.Parent.Loc, current.Loc), "Not a subspan. Child: " + current.Kind.String() + ", parent: " + current.Parent.Kind.String());
            var info = getImmediatelyContainingArgumentOrContextualParameterInfo(current, position, file, checker);
            if (info == null)
                continue;
            if (info.invocation.contextualInvocation != null)
                return info;
            first ??= info;
            if (info.argumentsSpan.End() == position || info.argumentsSpan.Contains(position))
                return info;
        }

        return first;
    }

    private static ArgumentListInfo? getImmediatelyContainingArgumentOrContextualParameterInfo(Node node, int position, SourceFile file, Checker checker) => tryGetParameterInfo(node, file, checker) ?? getImmediatelyContainingArgumentInfo(node, position, file, checker);
    // signaturehelp.go:930. Preserve invocation, tagged-template, JSX and generic fallback order.
    private static ArgumentListInfo? getImmediatelyContainingArgumentInfo(Node node, int position, SourceFile file, Checker checker)
    {
        var parent = node.Parent;
        if (Ast.IsCallOrNewExpression(parent))
        {
            var info = getArgumentOrParameterListInfo(node, file, checker);
            if (info == null)
                return null;
            bool isTypeParameters = false;
            var typeArguments = parent.TypeArgumentList();
            if (typeArguments != null && typeArguments.Pos() == info.list!.Pos())
                isTypeParameters = true;
            return new ArgumentListInfo
            {
                isTypeParameterList = isTypeParameters,
                invocation = new Invocation
                {
                    callInvocation = new CallInvocation
                    {
                        node = parent
                    }
                },
                argumentsSpan = info.argumentsSpan,
                argumentIndex = info.argumentIndex,
                argumentCount = info.argumentCount
            };
        }

        if (isNoSubstitutionTemplateLiteral(node) && isTaggedTemplateExpression(parent))
            return isInsideTemplateLiteral(node, position, file) ? getArgumentListInfoForTemplate(parent.AsTaggedTemplateExpression(), 0, file) : null;
        if (isTemplateHead(node) && parent.Parent.Kind == Kind.TaggedTemplateExpression)
        {
            var expression = parent.AsTemplateExpression();
            return getArgumentListInfoForTemplate(expression.Parent.AsTaggedTemplateExpression(), isInsideTemplateLiteral(node, position, file) ? 0 : 1, file);
        }

        if (Ast.IsTemplateSpan(parent) && isTaggedTemplateExpression(parent.Parent.Parent))
        {
            if (isTemplateTail(node) && !isInsideTemplateLiteral(node, position, file))
                return null;
            int spanIndex = Ast.IndexOfNode(parent.Parent.AsTemplateExpression().TemplateSpans.Nodes, parent);
            return getArgumentListInfoForTemplate(parent.Parent.Parent.AsTaggedTemplateExpression(), getArgumentIndexForTemplatePiece(spanIndex, node, position, file), file);
        }

        if (Ast.IsJsxOpeningLikeElement(parent))
        {
            int start = parent.Attributes().Loc.Pos();
            int end = ScannerApi.SkipTrivia(file.TextView(), parent.Attributes().End());
            // The pinned owner supplies end-start as NewTextRange's second argument.
            return new ArgumentListInfo
            {
                invocation = new Invocation
                {
                    callInvocation = new CallInvocation
                    {
                        node = parent
                    }
                },
                argumentsSpan = new TextRange(start, end - start),
                argumentCount = 1
            };
        }

        var typeInfo = getPossibleTypeArgumentsInfo(node, file);
        if (typeInfo == null)
            return null;
        return new ArgumentListInfo
        {
            isTypeParameterList = true,
            invocation = new Invocation
            {
                typeArgsInvocation = new TypeArgsInvocation
                {
                    called = typeInfo.called
                }
            },
            argumentsSpan = new TextRange(typeInfo.called.Loc.Pos(), node.End()),
            argumentIndex = typeInfo.nTypeArguments,
            argumentCount = typeInfo.nTypeArguments + 1
        };
    }

    private static int getArgumentIndexForTemplatePiece(int spanIndex, Node node, int position, SourceFile file)
    {
        Debug.Assert(position >= node.Loc.Pos(), "Assumed 'position' could not occur before node.");
        if (Ast.IsTemplateLiteralToken(node))
            return isInsideTemplateLiteral(node, position, file) ? 0 : spanIndex + 2;
        return spanIndex + 1;
    }

    private static Node? getAdjustedNode(Node node)
    {
        if (node.Kind is Kind.OpenParenToken or Kind.CommaToken)
            return node;
        return Ast.FindAncestor(node.Parent, static parent =>
        {
            if (Ast.IsParameterDeclaration(parent))
                return true;
            if (Ast.IsBindingElement(parent) || Ast.IsObjectBindingPattern(parent) || Ast.IsArrayBindingPattern(parent))
                return false;
            return false;
        });
    }

    // signaturehelp.go:1081. A tuple spread contributes its required fixed prefix.
    private static int getSpreadElementCount(SpreadElement node, Checker checker)
    {
        var spread = checker.GetTypeAtLocation(node.Expression);
        if (Checker.IsTupleType(spread))
        {
            var tuple = spread.Target().AsTupleType();
            if (tuple == null)
                return 0;
            var flags = tuple.ElementFlags();
            int fixedLength = tuple.FixedLength();
            if (fixedLength == 0)
                return 0;
            int firstOptional = Core.FindIndex(flags, static flag => (flag & ElementFlags.Required) == 0);
            return firstOptional < 0 ? fixedLength : firstOptional;
        }

        return 0;
    }

    private static int getArgumentIndex(Node node, NodeList? arguments, SourceFile file, Checker checker) => getArgumentIndexOrCount(getTokenFromNodeList(arguments, node.Parent, file), node, checker);
    private static int getArgumentCount(Node node, NodeList? arguments, SourceFile file, Checker checker) => getArgumentIndexOrCount(getTokenFromNodeList(arguments, node.Parent, file), null, checker);
    private static int getArgumentIndexOrCount(Slice<Node> arguments, Node? node, Checker checker)
    {
        int index = 0;
        bool skipComma = false;
        foreach (var argument in arguments)
        {
            if (node != null && argument == node)
            {
                if (!skipComma && argument.Kind == Kind.CommaToken)
                    index++;
                return index;
            }

            if (Ast.IsSpreadElement(argument))
            {
                index += getSpreadElementCount(argument.AsSpreadElement(), checker);
                skipComma = true;
                continue;
            }

            if (argument.Kind != Kind.CommaToken)
            {
                index++;
                skipComma = true;
                continue;
            }

            if (skipComma)
            {
                skipComma = false;
                continue;
            }

            index++;
        }

        if (node != null)
            return index;
        int count = index;
        if (arguments.Length > 0 && arguments[arguments.Length - 1].Kind == Kind.CommaToken)
            count = index + 1;
        return count;
    }

    private static ArgumentOrParameterListInfo? getArgumentOrParameterListInfo(Node node, SourceFile file, Checker checker)
    {
        var info = getArgumentOrParameterListAndIndex(node, file, checker);
        if (info == null)
            return null;
        return new ArgumentOrParameterListInfo
        {
            list = info.list,
            argumentIndex = info.argumentIndex,
            argumentCount = getArgumentCount(node, info.list, file, checker),
            argumentsSpan = getApplicableSpanForArguments(info.list, node, file)
        };
    }

    private static TextRange getApplicableSpanForArguments(NodeList? list, Node? node, SourceFile file)
    {
        if (list == null && node != null)
        {
            int start = node.End();
            int end = ScannerApi.SkipTrivia(file.TextView(), node.End());
            return new TextRange(start, ensureMinimumSpanSize(start, end));
        }

        int spanStart = list!.Pos();
        int spanEnd = ScannerApi.SkipTrivia(file.TextView(), list.End());
        return new TextRange(spanStart, ensureMinimumSpanSize(spanStart, spanEnd));
    }

    private static int ensureMinimumSpanSize(int start, int end) => end <= start ? start + 1 : end;
    private static ArgumentOrParameterListAndIndex? getArgumentOrParameterListAndIndex(Node node, SourceFile file, Checker checker)
    {
        if (node.Kind is Kind.LessThanToken or Kind.OpenParenToken)
            return new ArgumentOrParameterListAndIndex
            {
                list = getChildListThatStartsWithOpenerToken(node.Parent, node),
                argumentIndex = 0
            };
        var list = findContainingList(node, file);
        return list == null ? null : new ArgumentOrParameterListAndIndex
        {
            list = list,
            argumentIndex = getArgumentIndex(node, list, file, checker)
        };
    }

    private static NodeList? getChildListThatStartsWithOpenerToken(Node parent, Node opener)
    {
        if (Ast.IsCallExpression(parent))
            return opener.Kind == Kind.LessThanToken ? parent.AsCallExpression().TypeArgumentList() : parent.AsCallExpression().Arguments;
        if (Ast.IsNewExpression(parent))
            return opener.Kind == Kind.LessThanToken ? parent.AsNewExpression().TypeArgumentList() : parent.AsNewExpression().Arguments;
        return null;
    }

    private static ArgumentListInfo? tryGetParameterInfo(Node startingToken, SourceFile file, Checker checker)
    {
        var node = getAdjustedNode(startingToken);
        if (node == null)
            return null;
        var info = getContextualSignatureLocationInfo(node, file, checker);
        if (info == null)
            return null;
        var type = checker.GetNonNullableType(info.contextualType);
        if (type == null)
            return null;
        var symbol = type.Symbol();
        if (symbol == null)
            return null;
        var signatures = checker.GetSignaturesOfTypeSlice(type, SignatureKind.Call);
        if (signatures.Length == 0)
            return null;
        var contextual = new ContextualInvocation
        {
            signature = signatures[signatures.Length - 1],
            node = startingToken,
            symbol = chooseBetterSymbol(symbol)
        };
        return new ArgumentListInfo
        {
            isTypeParameterList = false,
            invocation = new Invocation
            {
                contextualInvocation = contextual
            },
            argumentsSpan = info.argumentsSpan,
            argumentIndex = info.argumentIndex,
            argumentCount = info.argumentCount
        };
    }

    private static Symbol chooseBetterSymbol(Symbol symbol)
    {
        if (symbol.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameType)))
            foreach (var declaration in symbol.Declarations)
                if (Ast.IsFunctionTypeNode(declaration) && Ast.CanHaveSymbol(declaration.Parent))
                    return declaration.Parent.Symbol();
        return symbol;
    }

    private static ContextualSignatureLocationInfo? getContextualSignatureLocationInfo(Node node, SourceFile file, Checker checker)
    {
        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.ParenthesizedExpression:
            case Kind.MethodDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
                var info = getArgumentOrParameterListInfo(node, file, checker);
                if (info == null)
                    return null;
                var contextual = Ast.IsMethodDeclaration(parent) ? checker.GetContextualTypeForObjectLiteralElement(parent, ContextFlags.None) : checker.GetContextualType(parent, ContextFlags.None);
                return contextual != null ? new ContextualSignatureLocationInfo
                {
                    contextualType = contextual,
                    argumentIndex = info.argumentIndex,
                    argumentCount = info.argumentCount,
                    argumentsSpan = info.argumentsSpan
                }

                : null;
            case Kind.BinaryExpression:
                var highest = getHighestBinary(parent.AsBinaryExpression());
                var type = checker.GetContextualType(highest.AsNode(), ContextFlags.None);
                int index = 0;
                if (node.Kind != Kind.OpenParenToken)
                {
                    index = countBinaryExpressionParameters(parent.AsBinaryExpression()) - 1;
                    int count = countBinaryExpressionParameters(highest);
                    if (type != null)
                        return new ContextualSignatureLocationInfo
                        {
                            contextualType = type,
                            argumentIndex = index,
                            argumentCount = count,
                            argumentsSpan = new TextRange(parent.Pos(), parent.End())
                        };
                    return null;
                }

                break;
        }

        return null;
    }

    private static BinaryExpression getHighestBinary(BinaryExpression binary) => Ast.IsBinaryExpression(binary.Parent) ? getHighestBinary(binary.Parent.AsBinaryExpression()) : binary;
    private static int countBinaryExpressionParameters(BinaryExpression binary) => Ast.IsBinaryExpression(binary.Left) ? countBinaryExpressionParameters(binary.Left.AsBinaryExpression()) + 1 : 2;
    private static Slice<Node> getTokenFromNodeList(NodeList? list, Node? parent, SourceFile file)
    {
        if (list == null || parent == null)
            return default;
        int left = list.Pos(), index = 0;
        Slice<Node> tokens = default;
        while (left < list.End())
        {
            if (list.Nodes.Length > index && left == list.Nodes[index].Pos())
            {
                tokens = tokens.Append(list.Nodes[index]);
                left = list.Nodes[index].End();
                index++;
            }
            else
            {
                var scanner = ScannerApi.GetScannerForSourceFile(file, left);
                int end = scanner.TokenEnd();
                tokens = tokens.Append(file.GetOrCreateToken(scanner.Token(), scanner.TokenFullStart(), end, parent, scanner.TokenFlags()));
                left = end;
            }
        }

        return tokens;
    }

    private static ArgumentListInfo getArgumentListInfoForTemplate(TaggedTemplateExpression template, int argumentIndex, SourceFile file)
    {
        int count = 1;
        if (!isNoSubstitutionTemplateLiteral(template.Template))
            count = template.Template.AsTemplateExpression().TemplateSpans.Nodes.Length + 1;
        if (argumentIndex != 0)
            Debug.Assert(argumentIndex < count);
        return new ArgumentListInfo
        {
            isTypeParameterList = false,
            invocation = new Invocation
            {
                callInvocation = new CallInvocation
                {
                    node = template.AsNode()
                }
            },
            argumentIndex = argumentIndex,
            argumentCount = count,
            argumentsSpan = getApplicableRangeForTaggedTemplate(template, file)
        };
    }

    private static TextRange getApplicableRangeForTaggedTemplate(TaggedTemplateExpression taggedTemplate, SourceFile file)
    {
        var template = taggedTemplate.Template;
        int start = ScannerApi.GetTokenPosOfNode(template, file, false), end = template.End();
        if (template.Kind == Kind.TemplateExpression)
        {
            var spans = template.AsTemplateExpression().TemplateSpans;
            var last = spans.Nodes[spans.Nodes.Length - 1];
            if (last.AsTemplateSpan().Literal.End() - last.AsTemplateSpan().Literal.Pos() == 0)
                end = ScannerApi.SkipTrivia(file.TextView(), end);
        }

        // Preserve the pinned source's NewTextRange(start, end-start) expression.
        return new TextRange(start, end - start);
    }
}
