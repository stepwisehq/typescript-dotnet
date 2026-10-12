// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/hover.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using TypeScript.LanguageServices.Utilities;
using Printing = TypeScript.DiagnosticPrinting;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private delegate (Location Location, Fidelity Fidelity) DocumentationLocationMapper(SourceFile file, TextRange range);
    // Actual trim call sites use ASCII cutsets only. Borrow the same raw byte bounds;
    // malformed UTF8 is not decoded or rewritten by Go's ASCII trim fast path either.
    private static ByteStringValue trimAsciiCutset(ByteStringValue text, string cutset, bool left, bool right)
    {
        int start = 0, end = text.Length;
        if (left)
            while (start < end && text[start] <= 127 && cutset.IndexOf((char)text[start]) >= 0)
                start++;
        if (right)
            while (end > start && text[end - 1] <= 127 && cutset.IndexOf((char)text[end - 1]) >= 0)
                end--;
        return text.Slice(start, end);
    }

    private const SymbolFormatFlags symbolFormatFlags = SymbolFormatFlags.WriteTypeParametersOrArguments | SymbolFormatFlags.UseOnlyExternalAliasing | SymbolFormatFlags.AllowAnyNodeKind | SymbolFormatFlags.UseAliasDefinedOutsideCurrentScope;
    private const TypeFormatFlags typeFormatFlags = TypeFormatFlags.UseAliasDefinedOutsideCurrentScope | TypeFormatFlags.UseInstantiationExpressions;
    private readonly record struct SymbolDisplayInfo(DisplayPartsWriter DisplayParts, Node? Declaration);
    // hover.go:27. Each admitted projection uses a genuine checker lease.
    internal (HoverOrNull Hover, Exception? Error) ProvideHover(ProtocolRequestContext context, HoverParams parameters)
    {
        ref readonly var capabilities = ref context.ClientCapabilities;
        var contentFormat = ProtocolUtilities.PreferredMarkupKind(capabilities.TextDocument.Hover.ContentFormat);
        int verbosityLevel = parameters.VerbosityLevel ?? 0;
        var (program, file) = getProgramAndFile(parameters.TextDocument.Uri);
        var positions = converters.FromLSPPositionForSourceFile(file, parameters.Position, Feature.Hover);
        Slice<Hover?> hovers = default;
        foreach (var projection in positions)
        {
            if (!projection.Fidelity.IsSingleSegment())
                continue;
            file = projection.Script;
            int position = projection.Position;
            var node = TypeScript.AstNavigation.AstNav.GetTouchingPropertyName(file, position);
            if (Ast.IsSourceFile(node) || Ast.IsPropertyAccessOrQualifiedName(node) && isInComment(file, position, node) == null)
                continue;
            var (checker, done) = program.GetTypeCheckerForFile(context, file);
            try
            {
                var rangeNode = getNodeForQuickInfo(node);
                var symbol = getSymbolAtLocationForQuickInfo(checker, rangeNode);
                long maximumLength = UserPreferences().MaximumHoverLength;
                if (maximumLength <= 0)
                    maximumLength = 500;
                var verbosity = new VerbosityContext
                {
                    Level = verbosityLevel,
                    MaxTruncationLength = maximumLength
                };
                bool vsCapability = capabilities.VSSupportsVisualStudioExtensions;
                var (quickInfo, documentation, vsDocumentation, runs) = getQuickInfoAndDocumentationForSymbol(checker, symbol, rangeNode, contentFormat, verbosity, vsCapability);
                if (quickInfo.Length == 0)
                    continue;
                var rangeFile = Ast.GetSourceFileOfNode(rangeNode);
                var textRange = getRangeOfNode(rangeNode, rangeFile, null);
                var (hoverRange, fidelity) = converters.ToLSPRangeForFeature(rangeFile, textRange, Feature.Hover);
                var content = ByteStringValue.Concat(contentFormat == MarkupKind.Markdown ? formatQuickInfo(quickInfo) : quickInfo, documentation);
                var hover = new Hover
                {
                    Contents = new MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings
                    {
                        MarkupContent = new MarkupContent
                        {
                            Kind = contentFormat,
                            Value = content
                        }
                    }
                };
                if (fidelity.IsSingleSegment())
                    hover.Range = hoverRange;
                if (capabilities.Experimental.HoverVerbosityLevel)
                    hover.CanIncreaseVerbosity = verbosity.CanIncreaseVerbosity && !verbosity.Truncated;
                if (vsCapability && runs.Length != 0)
                {
                    var kind = ScriptElementKind.Keyword;
                    ScriptElementKindModifier modifiers = default;
                    if (symbol != null)
                    {
                        var iconSymbol = symbol;
                        if ((symbol.Flags & SymbolFlags.Alias) != 0)
                        {
                            var resolved = checker.GetAliasedSymbol(symbol);
                            if (resolved != null && resolved != symbol)
                                iconSymbol = resolved;
                        }

                        kind = LsUtil.GetSymbolKind(checker, iconSymbol, rangeNode);
                        modifiers = LsUtil.GetSymbolModifiers(checker, symbol);
                    }

                    var image = getVSHoverImageId(kind, modifiers);
                    Slice<VSClassifiedTextRun?> documentationRuns = default;
                    var documentationText = trimAsciiCutset(vsDocumentation, "\n", true, false);
                    if (documentationText.Length != 0)
                        documentationRuns = Slice<VSClassifiedTextRun?>.FromArray([new VSClassifiedTextRun { ClassificationTypeName = ClassificationTypeName.Text.Value, Text = documentationText }]);
                    hover.VSRawContent = buildVSHoverRawContent(image, runs, documentationRuns);
                }

                hovers = hovers.Append(hover);
            }
            finally
            {
                done();
            }
        }

        if (hovers.Length == 0)
            return (new HoverOrNull(), null);
        if (hovers.Length == 1)
            return (new HoverOrNull { Hover = hovers[0] }, null);
        var combined = hovers[0]!;
        var contents = Slice<ByteStringValue>.Make(0, hovers.Length);
        HashSet<ByteStringKey>? seen = null;
        Slice<VSImageElementOrClassifiedTextElementOrContainerElement> rawContents = default;
        var commonRange = combined.Range;
        foreach (var hoverPointer in hovers)
        {
            var hover = hoverPointer!;
            var content = trimAsciiCutset(hover.Contents.MarkupContent!.Value, "\n", false, true);
            if ((seen ??= new()).Add(ByteStringKey.FromValue(content)))
            {
                contents = contents.Append(content);
                if (hover.VSRawContent != null)
                    rawContents = rawContents.Append(new VSImageElementOrClassifiedTextElementOrContainerElement { ContainerElement = hover.VSRawContent });
            }

            combined.CanIncreaseVerbosity |= hover.CanIncreaseVerbosity;
            if (commonRange == null || hover.Range == null || !commonRange.Value.Equals(hover.Range.Value))
                commonRange = null;
        }

        var separator = ByteStringValue.FromString(contentFormat == MarkupKind.Markdown ? "\n\n---\n\n" : "\n\n");
        combined.Contents.MarkupContent!.Value = GoStrings.Join(contents, separator);
        combined.Range = commonRange;
        combined.VSRawContent = rawContents.Length switch
        {
            0 => null,
            1 => rawContents[0].ContainerElement,
            _ => new VSContainerElement
            {
                Style = VSContainerElementStyle.Stacked,
                Elements = rawContents
            }
        };
        return (new HoverOrNull { Hover = combined }, null);
    }

    // hover.go:174. Keep rich-hover summary separate from Markdown tag sections.
    private (ByteStringValue QuickInfo, ByteStringValue Documentation, ByteStringValue VSDocumentation, Slice<VSClassifiedTextRun?> Runs) getQuickInfoAndDocumentationForSymbol(Checker checker, Symbol? symbol, Node node, MarkupKind format, VerbosityContext verbosity, bool vsCapability)
    {
        var info = getQuickInfoAndDeclarationAtLocation(checker, symbol, node, verbosity, vsCapability, getMeaningFromLocation(node));
        var quickInfo = info.DisplayParts.TextValue();
        if (quickInfo.Length == 0)
            return (default, default, default, default);
        var runs = info.DisplayParts.GetRuns();
        var documentation = getDocumentationForSymbol(documentationLocationMapper(Feature.Hover), checker, symbol, node, info.Declaration, format, false);
        ByteStringValue vsDocumentation = default;
        if (vsCapability)
            vsDocumentation = getDocumentationForSymbol(documentationLocationMapper(Feature.Hover), checker, symbol, node, info.Declaration, MarkupKind.PlainText, true);
        return (quickInfo, documentation, vsDocumentation, runs);
    }

    private DocumentationLocationMapper documentationLocationMapper(Feature feature) => (file, range) => sourceFileRangeToLSPLocationForFeature(file, range, feature);
    // hover.go:212. Preserve first nonempty source order, including aliases last.
    private static ByteStringValue getDocumentationForSymbol(DocumentationLocationMapper mapper, Checker checker, Symbol? symbol, Node node, Node? declaration, MarkupKind format, bool commentOnly)
    {
        var documentation = documentationFromSignature(mapper, checker, symbol, getCallOrNewExpression(node), node, format, commentOnly);
        if (documentation.Length != 0)
            return documentation;
        documentation = documentationFromRootSymbols(mapper, checker, symbol, node, format, commentOnly);
        if (documentation.Length != 0)
            return documentation;
        documentation = getDocumentationFromDeclaration(mapper, checker, symbol, declaration, node, format, commentOnly);
        return documentation.Length != 0 ? documentation : documentationFromAlias(mapper, checker, symbol, node, format, commentOnly);
    }

    private static ByteStringValue documentationFromSignature(DocumentationLocationMapper mapper, Checker checker, Symbol? symbol, Node? node, Node? location, MarkupKind format, bool commentOnly)
    {
        if (node == null)
            return default;
        var signature = checker.GetResolvedSignature(node);
        if (signature == null)
            return default;
        var declaration = signature.Declaration();
        if (declaration == null)
            return default;
        return Ast.IsCallSignatureDeclaration(declaration) || Ast.IsConstructSignatureDeclaration(declaration) ? getDocumentationFromDeclaration(mapper, checker, symbol, declaration, location, format, commentOnly) : default;
    }

    private static ByteStringValue documentationFromAlias(DocumentationLocationMapper mapper, Checker checker, Symbol? symbol, Node node, MarkupKind format, bool commentOnly)
    {
        if (symbol == null || (symbol.Flags & SymbolFlags.Alias) == 0)
            return default;
        var aliased = checker.GetAliasedSymbol(symbol);
        if (aliased == null || aliased == checker.GetUnknownSymbol())
            return default;
        var candidates = Slice<Symbol>.FromArray([aliased]);
        if (aliased.ExportSymbol != null)
            candidates = candidates.Append(aliased.ExportSymbol);
        foreach (var candidate in candidates)
        {
            var declaration = candidate.ValueDeclaration ?? (candidate.Declarations.Length != 0 ? candidate.Declarations[0] : null);
            if (declaration == null)
                continue;
            var documentation = getDocumentationFromDeclaration(mapper, checker, candidate, declaration, node, format, commentOnly);
            if (documentation.Length != 0)
                return documentation;
        }

        return default;
    }

    private static ByteStringValue documentationFromRootSymbols(DocumentationLocationMapper mapper, Checker checker, Symbol? symbol, Node node, MarkupKind format, bool commentOnly)
    {
        if (symbol == null)
            return default;
        var roots = checker.GetRootSymbols(symbol);
        if (roots.Length <= 1)
            return default;
        Slice<ByteStringValue> docs = default;
        foreach (var root in roots)
        {
            if (root == null)
                continue;
            var declarations = root.Declarations;
            if (declarations.Length == 0 && root.ValueDeclaration != null)
                declarations = Slice<Node>.FromArray([root.ValueDeclaration]);
            foreach (var declaration in declarations)
            {
                var documentation = getDocumentationFromDeclaration(mapper, checker, root, declaration, node, format, commentOnly);
                if (documentation.Length == 0)
                    continue;
                bool exists = false;
                foreach (var existing in docs)
                    if (existing.EqualsValue(documentation))
                    {
                        exists = true;
                        break;
                    }

                if (!exists)
                    docs = docs.Append(documentation);
            }
        }

        return GoStrings.Join(docs, ByteStringValue.FromString("\n"));
    }

    // hover.go:306. Full summary/tag formatting, not a flattened documentation substitute.
    private static ByteStringValue getDocumentationFromDeclaration(DocumentationLocationMapper mapper, Checker checker, Symbol? symbol, Node? declaration, Node? location, MarkupKind format, bool commentOnly)
    {
        if (declaration == null)
            return default;
        bool markdown = format == MarkupKind.Markdown;
        var builder = new GoBuilder();
        HashSet<Symbol>? seen = null;
        var jsdoc = getJSDocOrTag(checker, declaration, ref seen);
        if (jsdoc == null || (declaration.Flags & NodeFlags.Reparsed) == 0 && containsTypedefTag(jsdoc))
            return builder.Value();
        writeComments(mapper, builder, checker, jsdoc.Comments(), markdown);
        if (jsdoc.Kind != Kind.JSDoc || commentOnly || jsdoc.AsJSDoc().Tags is not { } tags)
            return builder.Value();
        foreach (var tag in tags.Nodes)
        {
            if (tag.Kind is Kind.JSDocTypeTag or Kind.JSDocTypedefTag or Kind.JSDocCallbackTag)
                continue;
            builder.WriteString("\n\n");
            builder.WriteString(markdown ? "*@" : "@");
            builder.WriteString(tag.TagName().TextValue());
            if (markdown)
                builder.WriteString("*");
            switch (tag.Kind)
            {
                case Kind.JSDocParameterTag:
                case Kind.JSDocPropertyTag:
                    writeOptionalEntityName(builder, tag.Name());
                    break;
                case Kind.JSDocAugmentsTag:
                    writeOptionalEntityName(builder, tag.ClassName());
                    break;
                case Kind.JSDocTemplateTag:
                    int index = 0;
                    foreach (var parameter in tag.TypeParameters())
                    {
                        if (index++ != 0)
                            builder.WriteString(",");
                        writeOptionalEntityName(builder, parameter.Name());
                    }

                    break;
            }

            var comments = tag.Comments();
            if (tag.Kind == Kind.JSDocUnknownTag && tag.TagName().TextValue().EqualsValue(ByteStringValue.FromString("example")))
            {
                var text = ScannerApi.GetTextOfJSDocCommentValue(tag.CommentList());
                if (text.StartsWith(ByteStringValue.FromString("<caption>")))
                {
                    int captionEnd = text.IndexOf(ByteStringValue.FromString("</caption>"));
                    if (captionEnd > 0)
                    {
                        builder.WriteString(ByteStringValue.FromString(" \xE2\x80\x94 "));
                        builder.WriteString(text.Slice(9, captionEnd));
                        text = text.Slice(captionEnd + 10);
                        while (true)
                        {
                            var first = trimAsciiCutset(text, " \t", true, false);
                            var second = trimAsciiCutset(first, "\r\n", true, false);
                            if (first.Length == second.Length)
                                break;
                            text = second;
                        }
                    }
                }

                builder.WriteString("\n");
                var fence = ByteStringValue.FromString("```");
                if (text.Length > 6 && text.StartsWith(fence) && text.EndsWith(fence) && text.IndexOf('\n') >= 0)
                {
                    builder.WriteString(text);
                    builder.WriteString("\n");
                }
                else
                    writeCode(builder, ByteStringValue.FromString("tsx"), text);
            }
            else if (tag.Kind == Kind.JSDocSeeTag && tag.AsJSDocSeeTag().NameExpression != null)
            {
                builder.WriteString(ByteStringValue.FromString(" \xE2\x80\x94 "));
                writeNameLink(mapper, builder, checker, tag.AsJSDocSeeTag().NameExpression.Name(), default, false, markdown);
                if (comments.Length != 0)
                {
                    builder.WriteString(" ");
                    writeComments(mapper, builder, checker, comments, markdown);
                }
            }
            else if (tag.Kind == Kind.JSDocThrowsTag && tag.AsJSDocThrowsTag().TypeExpression != null)
            {
                builder.WriteString(ByteStringValue.FromString(" \xE2\x80\x94 "));
                builder.WriteString(ScannerApi.GetTextOfNodeValue(tag.AsJSDocThrowsTag().TypeExpression));
                if (comments.Length != 0)
                {
                    builder.WriteString(" ");
                    writeComments(mapper, builder, checker, comments, markdown);
                }
            }
            else if (comments.Length != 0)
            {
                builder.WriteString(" ");
                if (comments[0].Kind != Kind.JSDocText || !comments[0].TextValue().StartsWith(ByteStringValue.FromString("-")))
                    builder.WriteString(ByteStringValue.FromString("\xE2\x80\x94 "));
                writeComments(mapper, builder, checker, comments, markdown);
            }
        }

        return builder.Value();
    }

    private static ByteStringValue formatQuickInfo(ByteStringValue quickInfo)
    {
        var builder = new GoBuilder();
        builder.Grow(32);
        writeCode(builder, ByteStringValue.FromString("typescript"), quickInfo);
        return builder.Value();
    }

    private static bool shouldGetType(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                return !((node.Flags & NodeFlags.JSDoc) != 0 && Ast.IsDeclarationName(node)) && !Ast.IsLabelName(node) && !Ast.IsTagName(node) && !Ast.IsConstTypeReference(node.Parent);
            case Kind.ThisKeyword:
            case Kind.ThisType:
            case Kind.SuperKeyword:
            case Kind.NamedTupleMember:
                return true;
            case Kind.MetaProperty:
                return Ast.IsImportMeta(node);
            default:
                return false;
        }
    }

    // hover.go:426. All local workers retain this query's explicit owners and state.
    private static SymbolDisplayInfo getQuickInfoAndDeclarationAtLocation(Checker checker, Symbol? symbol, Node node, VerbosityContext? verbosity, bool vsCapability, SemanticMeaning meaning)
    {
        var container = getContainerNode(node);
        verbosity ??= new VerbosityContext();
        var writer = DisplayPartsWriter.NewDisplayPartsWriter(vsCapability);
        var sourceFile = node != null ? Ast.GetSourceFileOfNode(node) : null;
        const NodeBuilderFlags classifiedFlags = NodeBuilderFlags.IgnoreErrors | NodeBuilderFlags.UseAliasDefinedOutsideCurrentScope | NodeBuilderFlags.WriteTypeParametersInQualifiedName;
        Printing.EmitContext? displayContext = null;
        Printing.EmitContext getEmitContext() => displayContext ??= new Printing.EmitContext();
        void writeTypeClassified(TypeScript.Checking.Type type, Node? enclosing, TypeFormatFlags flags)
        {
            flags |= TypeFormatFlags.MultilineObjectLiterals;
            if (!vsCapability)
            {
                writer.Write(checker.TypeToStringExValue(type, enclosing, flags, verbosity));
                return;
            }

            var context = getEmitContext();
            try
            {
                var symbols = new Dictionary<Node, Symbol>();
                var builder = Checker.NewNodeBuilderEx(checker, context, symbols);
                var combined = (NodeBuilderFlags)(flags & TypeFormatFlags.NodeBuilderFlagsMask) | classifiedFlags;
                var typeNode = builder.TypeToTypeNode(type, enclosing, combined, InternalFlags.None, null);
                if (typeNode == null)
                {
                    writer.Write(checker.TypeToStringExValue(type, enclosing, flags, verbosity));
                    return;
                }

                var printer = Printing.Printer.NewPrinter(new Printing.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
                printer.IdToSymbol = symbols;
                var temporary = DisplayPartsWriter.NewDisplayPartsWriter(true);
                printer.Write(typeNode, sourceFile, temporary, null);
                writer.WriteFrom(temporary);
            }
            finally
            {
                context.Factory.ReleaseArenas();
            }
        }

        void writeSignatureClassified(Signature signature, Node? enclosing, TypeFormatFlags flags)
        {
            flags |= TypeFormatFlags.MultilineObjectLiterals;
            if (!vsCapability)
            {
                writer.Write(checker.SignatureToStringExValue(signature, enclosing, flags, verbosity));
                return;
            }

            bool constructor = (signature.Flags() & SignatureFlags.Construct) != 0 && (flags & TypeFormatFlags.WriteCallStyleSignature) == 0;
            var kind = (flags & TypeFormatFlags.WriteArrowStyleSignature) != 0 ? (constructor ? Kind.ConstructorType : Kind.FunctionType) : (constructor ? Kind.ConstructSignature : Kind.CallSignature);
            var context = getEmitContext();
            try
            {
                var symbols = new Dictionary<Node, Symbol>();
                var builder = Checker.NewNodeBuilderEx(checker, context, symbols);
                var combined = (NodeBuilderFlags)(flags & TypeFormatFlags.NodeBuilderFlagsMask) | classifiedFlags;
                var signatureNode = builder.SignatureToSignatureDeclaration(signature, kind, enclosing, combined, InternalFlags.None, null);
                if (signatureNode == null)
                {
                    writer.Write(checker.SignatureToStringExValue(signature, enclosing, flags, verbosity));
                    return;
                }

                var printer = Printing.Printer.NewPrinter(new Printing.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
                printer.IdToSymbol = symbols;
                var temporary = DisplayPartsWriter.NewDisplayPartsWriter(true);
                printer.Write(signatureNode, sourceFile, temporary, null);
                writer.WriteFrom(temporary);
            }
            finally
            {
                context.Factory.ReleaseArenas();
            }
        }

        void writeSymbolClassified(Symbol current, Node? enclosing, SymbolFlags symbolMeaning, SymbolFormatFlags flags)
        {
            var text = checker.SymbolToStringExValue(current, enclosing, symbolMeaning, flags);
            if (vsCapability)
                writer.WriteSymbol(text, current);
            else
                writer.Write(text);
        }

        void writeModuleImportAttributes(Symbol current)
        {
            var declaration = Core.Find(current.Declarations, static candidate => Ast.IsModuleDeclaration(candidate) && candidate.AsModuleDeclaration().Attributes != null);
            if (declaration == null)
                return;
            var attributes = declaration.AsModuleDeclaration().Attributes;
            var context = getEmitContext();
            context.SetEmitFlags(attributes, Printing.EmitFlags.SingleLine);
            var printer = Printing.Printer.NewPrinter(new Printing.PrinterOptions { NewLine = NewLineKind.LF }, default, context);
            var temporary = DisplayPartsWriter.NewDisplayPartsWriter(vsCapability);
            printer.Write(attributes, Ast.GetSourceFileOfNode(declaration), temporary, null);
            writer.WriteKeyword(" with ");
            writer.WriteFrom(temporary);
        }

        if (node!.Kind == Kind.ThisKeyword && Ast.IsInExpressionContext(node) || Ast.IsThisInTypeQuery(node))
        {
            writer.WriteKeyword("this");
            writer.WritePunctuation(": ");
            writeTypeClassified(checker.GetTypeAtLocation(node), container, typeFormatFlags);
            return new(writer, null);
        }

        if (symbol == null)
        {
            if (shouldGetType(node))
                writeTypeClassified(checker.GetTypeAtLocation(node), container, typeFormatFlags);
            return new(writer, null);
        }

        HashSet<Symbol>? visitedAliases = null;
        int aliasLevel = 0;
        Node? firstDeclaration = null;
        void setDeclaration(Node? declaration)
        {
            if (firstDeclaration == null)
                firstDeclaration = declaration;
        }

        void writeNewLine()
        {
            if (writer.TextValue().Length != 0)
                writer.Write("\n");
            if (aliasLevel != 0)
            {
                writer.WritePunctuation("(");
                writer.Write("alias");
                writer.WritePunctuation(") ");
            }
        }

        void writeSignatures(Slice<Signature> signatures, string prefix, bool parenthesized, Symbol current)
        {
            for (int i = 0; i < signatures.Length; i++)
            {
                writeNewLine();
                if (i == 3 && signatures.Length >= 5)
                {
                    writer.WriteComment(FormattableString.Invariant($"// +{signatures.Length - 3} more overloads"));
                    break;
                }

                if (parenthesized)
                {
                    writer.WritePunctuation("(");
                    writer.Write(prefix);
                    writer.WritePunctuation(") ");
                }
                else
                    writer.WriteKeyword(prefix);
                writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                if ((current.Flags & SymbolFlags.Optional) != 0)
                    writer.WritePunctuation("?");
                writeSignatureClassified(signatures[i], container, typeFormatFlags | TypeFormatFlags.WriteCallStyleSignature | TypeFormatFlags.WriteTypeArgumentsOfSignature);
            }
        }

        void writeTypeParams(Slice<TypeScript.Checking.Type> parameters)
        {
            if (parameters.Length == 0)
                return;
            writer.WritePunctuation("<");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                    writer.WritePunctuation(", ");
                var parameter = parameters[i];
                writeSymbolClassified(parameter.Symbol(), null, SymbolFlags.None, symbolFormatFlags);
                var constraint = checker.GetConstraintOfTypeParameter(parameter);
                if (constraint != null)
                {
                    writer.WriteKeyword(" extends ");
                    writeTypeClassified(constraint, null, typeFormatFlags);
                }

                var defaultType = checker.GetDefaultFromTypeParameter(parameter);
                if (defaultType != null)
                {
                    writer.WriteOperator(" = ");
                    writeTypeClassified(defaultType, null, typeFormatFlags);
                }
            }

            writer.WritePunctuation(">");
        }

        bool symbolWasExpanded = false;
        bool canExpandSymbol(Symbol current)
        {
            if ((current.Flags & (SymbolFlags.Class | SymbolFlags.Interface | SymbolFlags.Namespace)) == 0)
                return false;
            var type = (current.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0 ? checker.GetDeclaredTypeOfSymbol(current) : checker.GetTypeOfSymbolAtLocation(current, node);
            if (type == null || checker.IsLibTypeForHoverVerbosity(type))
                return false;
            if (verbosity.Level > 0)
                return true;
            verbosity.CanIncreaseVerbosity = true;
            return false;
        }

        bool tryExpandSymbol(Symbol current, SymbolFlags symbolMeaning)
        {
            if (symbolWasExpanded)
                return true;
            if (canExpandSymbol(current))
            {
                var expandedContext = new VerbosityContext
                {
                    Level = verbosity.Level - 1,
                    MaxTruncationLength = verbosity.MaxTruncationLength
                };
                var expanded = checker.ExpandSymbolForHoverValue(current, symbolMeaning, expandedContext);
                if (expanded.Length != 0)
                {
                    verbosity.CanIncreaseVerbosity |= expandedContext.CanIncreaseVerbosity;
                    verbosity.Truncated |= expandedContext.Truncated;
                    writer.Write(expanded);
                    symbolWasExpanded = true;
                    return true;
                }
            }

            return false;
        }

        void writeSymbol(Symbol current)
        {
            if ((current.Flags & SymbolFlags.Alias) != 0 && (visitedAliases ??= new()).Add(current))
            {
                var aliased = checker.GetAliasedSymbol(current);
                if (aliased != checker.GetUnknownSymbol())
                {
                    aliasLevel++;
                    writeSymbol(aliased);
                    aliasLevel--;
                }
            }

            SymbolFlags flags = meaning switch
            {
                SemanticMeaning.Value => current.Flags & (SymbolFlags.Value | SymbolFlags.Signature),
                SemanticMeaning.Type => current.Flags & SymbolFlags.Type,
                SemanticMeaning.Namespace => current.Flags & SymbolFlags.Namespace,
                _ => current.Flags & (SymbolFlags.Value | SymbolFlags.Signature | SymbolFlags.Type | SymbolFlags.Namespace)};
            if (flags == 0)
            {
                if (aliasLevel != 0 || writer.TextValue().Length != 0)
                    return;
                flags = current.Flags & (SymbolFlags.Value | SymbolFlags.Signature | SymbolFlags.Type | SymbolFlags.Namespace);
                if (flags == 0)
                    return;
            }

            if ((flags & SymbolFlags.Property) != 0 && current.ValueDeclaration != null && Ast.IsMethodDeclaration(current.ValueDeclaration))
                flags = SymbolFlags.Method;
            if ((flags & (SymbolFlags.Variable | SymbolFlags.Property | SymbolFlags.Accessor)) != 0)
            {
                writeNewLine();
                if ((current.CheckFlags & CheckFlags.IndexSymbol) == 0)
                {
                    if ((flags & SymbolFlags.Property) != 0)
                    {
                        writer.WritePunctuation("(");
                        writer.Write("property");
                        writer.WritePunctuation(") ");
                    }
                    else if ((flags & SymbolFlags.Accessor) != 0)
                    {
                        writer.WritePunctuation("(");
                        writer.Write("accessor");
                        writer.WritePunctuation(") ");
                    }
                    else
                    {
                        var declaration = current.ValueDeclaration;
                        if (declaration != null)
                        {
                            declaration = Ast.GetRootDeclaration(declaration);
                            if (Ast.IsParameterDeclaration(declaration))
                            {
                                writer.WritePunctuation("(");
                                writer.Write("parameter");
                                writer.WritePunctuation(") ");
                            }
                            else if (Ast.IsVarLet(declaration))
                                writer.WriteKeyword("let ");
                            else if (Ast.IsVarConst(declaration))
                                writer.WriteKeyword("const ");
                            else if (Ast.IsVarUsing(declaration))
                                writer.WriteKeyword("using ");
                            else if (Ast.IsVarAwaitUsing(declaration))
                            {
                                writer.WriteKeyword("await ");
                                writer.WriteKeyword("using ");
                            }
                            else
                                writer.WriteKeyword("var ");
                        }
                    }

                    if (current.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameExportEquals)) && current.Parent != null && (current.Parent.Flags & SymbolFlags.Module) != 0)
                        writer.Write("exports");
                    else
                        writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                    if ((current.Flags & SymbolFlags.Optional) != 0)
                        writer.WritePunctuation("?");
                    writer.WritePunctuation(": ");
                }

                var call = getCallOrNewExpression(node);
                if (call != null)
                {
                    var signatureFlags = typeFormatFlags | TypeFormatFlags.WriteTypeArgumentsOfSignature | TypeFormatFlags.WriteArrowStyleSignature;
                    if (Ast.IsCallExpression(call))
                        signatureFlags |= TypeFormatFlags.WriteCallStyleSignature;
                    writeSignatureClassified(checker.GetResolvedSignature(call), container, signatureFlags);
                }
                else
                {
                    var type = checker.GetTypeOfSymbolAtLocation(current, node);
                    if (type.Symbol() != null && (type.Symbol().Flags & SymbolFlags.TypeParameter) != 0 && checker.GetConstraintOfTypeParameter(type) != null)
                    {
                        if (verbosity.Level > 0)
                        {
                            var expandedContext = new VerbosityContext
                            {
                                Level = verbosity.Level - 1,
                                MaxTruncationLength = verbosity.MaxTruncationLength
                            };
                            writer.Write(typeParameterToString(checker, type, container, expandedContext));
                            verbosity.CanIncreaseVerbosity |= expandedContext.CanIncreaseVerbosity;
                            verbosity.Truncated |= expandedContext.Truncated;
                        }
                        else
                        {
                            writeTypeClassified(type, container, typeFormatFlags);
                            verbosity.CanIncreaseVerbosity = true;
                        }
                    }
                    else
                        writeTypeClassified(type, container, typeFormatFlags);
                }

                setDeclaration(current.ValueDeclaration ?? (current.Declarations.Length != 0 ? current.Declarations[0] : null));
            }

            if ((flags & SymbolFlags.EnumMember) != 0)
            {
                writeNewLine();
                writer.WritePunctuation("(");
                writer.Write("enum member");
                writer.WritePunctuation(") ");
                var type = checker.GetTypeOfSymbol(current);
                writeTypeClassified(type, container, typeFormatFlags);
                if ((type.Flags() & TypeFlags.Literal) != 0)
                {
                    writer.WriteOperator(" = ");
                    writer.WriteLiteral(type.AsLiteralType().StringRepresentationValue());
                }

                setDeclaration(current.ValueDeclaration);
            }

            if ((flags & (SymbolFlags.Function | SymbolFlags.Method)) != 0)
            {
                bool method = (flags & SymbolFlags.Method) != 0;
                string prefix = method ? "method" : "function ";
                bool declaredHere = false;
                foreach (var declaration in current.Declarations)
                    if (declaration == node.Parent)
                    {
                        declaredHere = true;
                        break;
                    }

                if (Ast.IsIdentifier(node) && (Ast.IsFunctionLikeDeclaration(node.Parent) || Ast.IsMethodSignatureDeclaration(node.Parent)) && node.Parent.Name() == node && declaredHere)
                {
                    setDeclaration(node.Parent);
                    writeSignatures(Slice<Signature>.FromArray([checker.GetSignatureFromDeclaration(node.Parent)]), prefix, method, current);
                }
                else
                {
                    var signatures = getSignaturesAtLocation(checker, current, SignatureKind.Call, node);
                    if (signatures.Length == 1)
                    {
                        var declaration = signatures[0].Declaration();
                        if (declaration != null && (declaration.Flags & NodeFlags.JSDoc) == 0)
                            setDeclaration(declaration);
                    }

                    writeSignatures(signatures, prefix, method, current);
                }

                setDeclaration(current.ValueDeclaration);
            }

            if ((flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0)
            {
                if (node!.Kind == Kind.ThisKeyword || Ast.IsThisInTypeQuery(node))
                {
                    writeNewLine();
                    writer.WriteKeyword("this");
                }
                else if (node.Kind == Kind.ConstructorKeyword && (Ast.IsConstructorDeclaration(node.Parent) || Ast.IsConstructSignatureDeclaration(node.Parent)))
                {
                    setDeclaration(node.Parent);
                    writeSignatures(Slice<Signature>.FromArray([checker.GetSignatureFromDeclaration(node.Parent)]), "constructor ", false, current);
                }
                else
                {
                    Slice<Signature> signatures = default;
                    if ((flags & SymbolFlags.Class) != 0 && getCallOrNewExpression(node) != null)
                        signatures = getSignaturesAtLocation(checker, current, SignatureKind.Construct, node);
                    if (signatures.Length == 1)
                    {
                        var declaration = signatures[0].Declaration();
                        if (declaration != null && (declaration.Flags & NodeFlags.JSDoc) == 0)
                            setDeclaration(declaration);
                        writeSignatures(signatures, "constructor ", false, current);
                    }
                    else
                    {
                        writeNewLine();
                        if ((flags & SymbolFlags.Class) != 0)
                        {
                            var classExpression = Ast.GetDeclarationOfKind(current, Kind.ClassExpression);
                            if (classExpression != null)
                            {
                                writer.WritePunctuation("(");
                                writer.Write("local class");
                                writer.WritePunctuation(") ");
                            }

                            if (!tryExpandSymbol(current, flags))
                            {
                                if (classExpression == null)
                                {
                                    if (Core.Some(current.Declarations, static declaration => Ast.IsClassDeclaration(declaration) && Ast.HasAbstractModifier(declaration)))
                                        writer.WriteKeyword("abstract ");
                                    writer.WriteKeyword("class ");
                                }

                                writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                                writeTypeParams(checker.GetDeclaredTypeOfSymbol(current).AsInterfaceType().LocalTypeParameters());
                            }
                        }
                        else if (!tryExpandSymbol(current, flags))
                        {
                            writer.WriteKeyword("interface ");
                            writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                            writeTypeParams(checker.GetDeclaredTypeOfSymbol(current).AsInterfaceType().LocalTypeParameters());
                        }
                    }
                }

                setDeclaration((flags & SymbolFlags.Class) != 0 ? current.ValueDeclaration : Core.Find(current.Declarations, Ast.IsInterfaceDeclaration));
            }

            if ((flags & SymbolFlags.Enum) != 0)
            {
                writeNewLine();
                if (!tryExpandSymbol(current, flags))
                {
                    if (Core.Some(current.Declarations, static declaration => Ast.IsEnumDeclaration(declaration) && Ast.IsEnumConst(declaration)))
                        writer.WriteKeyword("const ");
                    writer.WriteKeyword("enum ");
                    writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                }

                setDeclaration(Core.Find(current.Declarations, Ast.IsEnumDeclaration));
            }

            if ((flags & SymbolFlags.Module) != 0)
            {
                writeNewLine();
                if (!tryExpandSymbol(current, flags))
                {
                    bool module = current.ValueDeclaration != null && (Ast.IsSourceFile(current.ValueDeclaration) || Ast.IsAmbientModule(current.ValueDeclaration));
                    writer.WriteKeyword(module ? "module " : "namespace ");
                    writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                    writeModuleImportAttributes(current);
                }

                setDeclaration(Core.Find(current.Declarations, Ast.IsModuleDeclaration));
            }

            if ((flags & SymbolFlags.TypeParameter) != 0)
            {
                writeNewLine();
                writer.WritePunctuation("(");
                writer.Write("type parameter");
                writer.WritePunctuation(") ");
                if (Ast.IsIdentifier(node) && Ast.IsTypeReferenceNode(node.Parent) && Checker.IsDistributedTypeParameter(checker.GetTypeAtLocation(node.Parent)))
                {
                    writer.WritePunctuation("(");
                    writer.Write("distributed");
                    writer.WritePunctuation(") ");
                }

                var type = checker.GetDeclaredTypeOfSymbol(current);
                writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                var constraint = checker.GetConstraintOfTypeParameter(type);
                if (constraint != null)
                {
                    writer.WriteKeyword(" extends ");
                    writeTypeClassified(constraint, container, typeFormatFlags);
                }

                if (current.Parent != null)
                {
                    writer.WriteKeyword(" in ");
                    writeSymbolClassified(current.Parent, container, SymbolFlags.None, symbolFormatFlags);
                    var parentType = checker.GetDeclaredTypeOfSymbol(current.Parent);
                    if (parentType.AsInterfaceType() != null)
                        writeTypeParams(parentType.AsInterfaceType().LocalTypeParameters());
                }
                else
                {
                    var parameterDeclaration = Ast.GetDeclarationOfKind(current, Kind.TypeParameter);
                    if (parameterDeclaration != null && parameterDeclaration.Parent != null)
                    {
                        var declaration = parameterDeclaration.Parent;
                        if (Ast.IsFunctionLike(declaration))
                        {
                            writer.WriteKeyword(" in ");
                            if (declaration.Kind == Kind.ConstructSignature)
                                writer.WriteKeyword("new ");
                            else if (declaration.Kind != Kind.CallSignature && declaration.Name() != null)
                                writeSymbolClassified(declaration.Symbol(), container, SymbolFlags.None, symbolFormatFlags);
                            var signature = checker.GetSignatureFromDeclaration(declaration);
                            if (signature != null)
                                writeSignatureClassified(signature, container, typeFormatFlags | TypeFormatFlags.WriteTypeArgumentsOfSignature);
                        }
                        else if (Ast.IsTypeAliasDeclaration(declaration))
                        {
                            writer.WriteKeyword(" in ");
                            writer.WriteKeyword("type ");
                            writeSymbolClassified(declaration.Symbol(), container, SymbolFlags.None, symbolFormatFlags);
                            var declarationSymbol = declaration.Symbol();
                            if (declarationSymbol != null)
                                writeTypeParams(checker.GetTypeAliasTypeParameters(declarationSymbol));
                        }
                    }
                }

                setDeclaration(Core.Find(current.Declarations, Ast.IsTypeParameterDeclaration));
            }

            if ((flags & SymbolFlags.TypeAlias) != 0)
            {
                writeNewLine();
                writer.WriteKeyword("type ");
                writeSymbolClassified(current, container, SymbolFlags.None, symbolFormatFlags);
                writeTypeParams(checker.GetTypeAliasTypeParameters(current));
                writer.WriteOperator(" = ");
                var aliasType = node.Parent != null && Ast.IsConstTypeReference(node.Parent) ? checker.GetTypeAtLocation(node.Parent) : checker.GetDeclaredTypeOfSymbol(current);
                writeTypeClassified(aliasType, container, typeFormatFlags | TypeFormatFlags.InTypeAlias);
                setDeclaration(Core.Find(current.Declarations, Ast.IsTypeOrJSTypeAliasDeclaration));
            }

            if ((flags & SymbolFlags.Signature) != 0)
            {
                writeNewLine();
                writeTypeClassified(checker.GetTypeOfSymbol(current), container, typeFormatFlags);
            }
        }

        writeSymbol(symbol);
        return new(writer, firstDeclaration);
    }

    // hover.go:960. Preserve the package worker boundary around configured printing.
    private static ByteStringValue typeParameterToString(Checker checker, TypeScript.Checking.Type type, Node? enclosing, VerbosityContext verbosity) => checker.TypeParameterToStringExValue(type, enclosing, verbosity);
    // hover.go:964.
    private static Node getNodeForQuickInfo(Node node)
    {
        if (node.Parent == null)
            return node;
        if (Ast.IsNewExpression(node.Parent) && node.Pos() == node.Parent.Pos())
            return node.Parent.Expression();
        if (Ast.IsNamedTupleMember(node.Parent) && node.Pos() == node.Parent.Pos())
            return node.Parent;
        if (Ast.IsImportMeta(node.Parent) && node.Parent.Name() == node)
            return node.Parent;
        if (Ast.IsJsxNamespacedName(node.Parent))
            return node.Parent;
        return node;
    }

    private static Symbol? getSymbolAtLocationForQuickInfo(Checker checker, Node node)
    {
        var element = getContainingObjectLiteralElement(node);
        if (element != null)
        {
            var contextual = checker.GetContextualType(element.Parent, ContextFlags.None);
            if (contextual != null)
            {
                var properties = checker.GetPropertySymbolsFromContextualType(element, contextual, false);
                if (properties.Length == 1)
                    return properties[0];
            }
        }

        return checker.GetSymbolAtLocation(node);
    }

    private static Slice<Signature> getSignaturesAtLocation(Checker checker, Symbol symbol, SignatureKind kind, Node node)
    {
        var signatures = checker.GetSignaturesOfTypeSlice(checker.RemoveMissingOrUndefinedType(checker.GetTypeOfSymbol(symbol)), kind);
        if (signatures.Length > 1 || signatures.Length == 1 && signatures[0].TypeParameters().Length != 0)
        {
            var call = getCallOrNewExpression(node);
            if (call != null)
                return Slice<Signature>.FromArray([checker.GetResolvedSignature(call)]);
        }

        return signatures;
    }

    private static Node? getCallOrNewExpression(Node node)
    {
        if (Ast.IsSourceFile(node))
            return null;
        if (Ast.IsPropertyAccessExpression(node.Parent) && node.Parent.Name() == node)
            node = node.Parent;
        return (Ast.IsCallExpression(node.Parent) || Ast.IsNewExpression(node.Parent)) && node.Parent.Expression() == node ? node.Parent : null;
    }

    private static bool containsTypedefTag(Node jsdoc)
    {
        if (jsdoc.Kind == Kind.JSDoc && jsdoc.AsJSDoc().Tags is { } tags)
            foreach (var tag in tags.Nodes)
                if (tag.Kind is Kind.JSDocTypedefTag or Kind.JSDocCallbackTag)
                    return true;
        return false;
    }

    // hover.go:1031. Code fences grow until the source contains no closing fence.
    private static void writeCode(GoBuilder builder, ByteStringValue language, ByteStringValue code)
    {
        if (code.Length == 0)
            return;
        int ticks = 3;
        while (code.IndexOf(ByteStringValue.FromString(new string ('`', ticks))) >= 0)
            ticks++;
        for (int i = 0; i < ticks; i++)
            builder.WriteByte((byte)'`');
        builder.WriteString(language);
        builder.WriteByte((byte)'\n');
        builder.WriteString(code);
        builder.WriteByte((byte)'\n');
        for (int i = 0; i < ticks; i++)
            builder.WriteByte((byte)'`');
        builder.WriteByte((byte)'\n');
    }

    // hover.go:1052. Documentation node order and quote policy remain unchanged.
    private static void writeComments(DocumentationLocationMapper mapper, GoBuilder builder, Checker checker, Slice<Node> comments, bool markdown)
    {
        foreach (var comment in comments)
            switch (comment.Kind)
            {
                case Kind.JSDocText:
                    builder.WriteString(comment.TextValue());
                    break;
                case Kind.JSDocLink:
                case Kind.JSDocLinkPlain:
                    writeJSDocLink(mapper, builder, checker, comment, false, markdown);
                    break;
                case Kind.JSDocLinkCode:
                    writeJSDocLink(mapper, builder, checker, comment, true, markdown);
                    break;
            }
    }

    private static void writeJSDocLink(DocumentationLocationMapper mapper, GoBuilder builder, Checker checker, Node link, bool quote, bool markdown)
    {
        var name = link.Name();
        var text = trimAsciiCutset(link.TextValue(), " ", true, true);
        if (name == null)
        {
            writeQuotedString(builder, text, quote && markdown);
            return;
        }

        if (Ast.IsIdentifier(name) && (name.TextValue().EqualsValue(ByteStringValue.FromString("http")) || name.TextValue().EqualsValue(ByteStringValue.FromString("https"))) && text.StartsWith(ByteStringValue.FromString("://")))
        {
            var combined = new GoBuilder();
            combined.WriteString(name.TextValue());
            combined.WriteString(text);
            var linkText = combined.Value();
            var uri = linkText;
            int commentPosition = -1;
            // The exact predicate is ASCII space or '|'; its byte match equals rune matching.
            for (int i = 0; i < linkText.Length; i++)
                if (linkText[i] is (int)' ' or (int)'|')
                {
                    commentPosition = i;
                    break;
                }

            if (commentPosition >= 0)
            {
                uri = linkText.Slice(0, commentPosition);
                linkText = trimCommentPrefix(linkText.Slice(commentPosition));
                if (linkText.Length == 0)
                    linkText = uri;
            }

            if (markdown)
                writeMarkdownLink(builder, linkText, uri, quote);
            else
            {
                writeQuotedString(builder, linkText, false);
                if (!linkText.EqualsValue(uri))
                {
                    builder.WriteString(" (");
                    builder.WriteString(uri);
                    builder.WriteString(")");
                }
            }

            return;
        }

        writeNameLink(mapper, builder, checker, name, text, quote, markdown);
    }

    private static void writeNameLink(DocumentationLocationMapper mapper, GoBuilder builder, Checker checker, Node name, ByteStringValue text, bool quote, bool markdown)
    {
        var declarations = getDeclarationsFromLocation(checker, name);
        if (declarations.Length != 0)
        {
            var declaration = declarations[0];
            var file = Ast.GetSourceFileOfNode(declaration);
            var node = Ast.GetNameOfDeclaration(declaration) ?? declaration;
            var (location, fidelity) = mapper(file, createRangeFromNode(node, file));
            int prefixLength = text.StartsWith(ByteStringValue.FromString("()")) ? 2 : 0;
            var linkText = trimCommentPrefix(text.Slice(prefixLength));
            if (linkText.Length == 0)
            {
                var combined = new GoBuilder();
                combined.WriteString(getEntityNameString(name));
                combined.WriteString(text.Slice(0, prefixLength));
                linkText = combined.Value();
            }

            if (markdown && fidelity.IsSingleSegment())
            {
                var uri = new GoBuilder();
                uri.WriteString(location.Uri.Value);
                uri.WriteString(FormattableString.Invariant($"#{location.Range.Start.Line + 1},{location.Range.Start.Character + 1}-{location.Range.End.Line + 1},{location.Range.End.Character + 1}"));
                writeMarkdownLink(builder, linkText, uri.Value(), quote);
            }
            else
                writeQuotedString(builder, linkText, false);
            return;
        }

        var fallback = new GoBuilder();
        fallback.WriteString(getEntityNameString(name));
        if (text.Length != 0)
            fallback.WriteString(" ");
        fallback.WriteString(text);
        writeQuotedString(builder, fallback.Value(), quote && markdown);
    }

    private static ByteStringValue trimCommentPrefix(ByteStringValue text)
    {
        text = trimAsciiCutset(text, " ", true, false);
        if (text.StartsWith(ByteStringValue.FromString("|")))
            text = text.Slice(1);
        return trimAsciiCutset(text, " ", true, false);
    }

    private static void writeMarkdownLink(GoBuilder builder, ByteStringValue text, ByteStringValue uri, bool quote)
    {
        builder.WriteString("[");
        writeQuotedString(builder, text, quote);
        builder.WriteString("](");
        builder.WriteString(uri);
        builder.WriteString(")");
    }

    private static void writeOptionalEntityName(GoBuilder builder, Node? name)
    {
        if (name == null)
            return;
        builder.WriteString(" ");
        writeQuotedString(builder, getEntityNameString(name), true);
    }

    private static void writeQuotedString(GoBuilder builder, ByteStringValue text, bool quote)
    {
        if (quote && text.IndexOf('`') < 0)
        {
            builder.WriteString("`");
            builder.WriteString(text);
            builder.WriteString("`");
        }
        else
            builder.WriteString(text);
    }

    private static ByteStringValue getEntityNameString(Node name)
    {
        var builder = new GoBuilder();
        writeEntityNameParts(builder, name);
        return builder.Value();
    }

    private static void writeEntityNameParts(GoBuilder builder, Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                builder.WriteString(node.TextValue());
                break;
            case Kind.QualifiedName:
                writeEntityNameParts(builder, node.AsQualifiedName().Left);
                builder.WriteByte((byte)'.');
                writeEntityNameParts(builder, node.AsQualifiedName().Right);
                break;
            case Kind.PropertyAccessExpression:
                writeEntityNameParts(builder, node.Expression());
                builder.WriteByte((byte)'.');
                writeEntityNameParts(builder, node.Name());
                break;
            case Kind.ParenthesizedExpression:
            case Kind.ExpressionWithTypeArguments:
                writeEntityNameParts(builder, node.Expression());
                break;
            case Kind.JSDocNameReference:
                writeEntityNameParts(builder, node.Name());
                break;
        }
    }
}
