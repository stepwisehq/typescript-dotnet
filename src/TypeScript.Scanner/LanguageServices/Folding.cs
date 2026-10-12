// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/folding.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using TypeScript.DiagnosticPrinting;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using ProtocolRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal (FoldingRangesOrNull Result, Exception? Error) ProvideFoldingRange(ProtocolRequestContext context, DocumentUri documentUri)
    {
        var(_, sourceFile) = getProgramAndFile(documentUri);
        var projections = Slice<SourceFile>.FromArray([sourceFile]).Append(sourceFile.SupplementalSourceFiles());
        Slice<FoldingRange?> result = default;
        foreach (var projection in projections)
        {
            var ranges = addNodeOutliningSpans(context, projection).Append(addRegionOutliningSpans(context, projection));
            if (context.ClientCapabilities.TextDocument.FoldingRange.LineFoldingOnly)
                ranges = adjustFoldingEnd(ranges, projection);
            result = result.Append(ranges);
        }

        GoSlices.SortStableFunc(result, static (a, b) =>
        {
            int comparison = a!.StartLine.CompareTo(b!.StartLine);
            if (comparison != 0)
                return comparison;
            comparison = a.StartCharacter!.Value.CompareTo(b.StartCharacter!.Value);
            if (comparison != 0)
                return comparison;
            comparison = a.EndLine.CompareTo(b.EndLine);
            if (comparison != 0)
                return comparison;
            return a.EndCharacter!.Value.CompareTo(b.EndCharacter!.Value);
        });
        // Original zero collections.Set allocates only on its first AddIfAbsent.
        HashSet<FoldingRangeKey>? seen = null;
        result = GoSlices.DeleteFunc(result, range => !(seen ??= new()).Add(keyForFoldingRange(range!)));
        return (new() { FoldingRanges = result }, null);
    }

    // ByteStringKey preserves Go string value identity independent of CLR backing.
    private readonly record struct FoldingRangeKey(uint StartLine, uint StartCharacter, uint EndLine, uint EndCharacter, FoldingRangeKind Kind, ByteStringKey CollapsedText, bool HasStartCharacter, bool HasEndCharacter, bool HasKind, bool HasCollapsedText);
    private static FoldingRangeKey keyForFoldingRange(FoldingRange range) => new(range.StartLine, range.StartCharacter.GetValueOrDefault(), range.EndLine, range.EndCharacter.GetValueOrDefault(), range.Kind.GetValueOrDefault(), ByteStringKey.FromValue(range.CollapsedText.GetValueOrDefault()), range.StartCharacter.HasValue, range.EndCharacter.HasValue, range.Kind.HasValue, range.CollapsedText.HasValue);
    private Slice<FoldingRange?> adjustFoldingEnd(Slice<FoldingRange?> ranges, SourceFile sourceFile)
    {
        var text = sourceFile.TextView();
        var result = Slice<FoldingRange?>.Make(0, ranges.Length);
        foreach (var value in ranges)
        {
            var range = value!;
            if (range.EndCharacter is { } character && character > 0)
            {
                var positions = converters.FromLSPPositionForSourceFile(sourceFile, new Position { Line = range.EndLine, Character = character }, Feature.FoldingRanges);
                TypeScript.LanguageServices.Lsconv.MappedPosition<SourceFile>? position = null;
                for (int i = 0; i < positions.Length; i++)
                    if (positions[i].Script == sourceFile && !positions[i].Fidelity.IsNone())
                    {
                        position = positions[i];
                        break;
                    }

                if (position is { } mapped && mapped.Position > 0 && mapped.Position <= text.Length)
                {
                    int last = text[mapped.Position - 1];
                    if ((last == '}' || last == ']' || last == ')' || last == '`' || last == '>') && range.EndLine > range.StartLine)
                        range.EndLine--;
                }
            }

            result = result.Append(range);
        }

        return result;
    }

    private Slice<FoldingRange?> addNodeOutliningSpans(ProtocolRequestContext context, SourceFile file)
    {
        int depthRemaining = 40, current = 0;
        var statements = file.Statements;
        int count = statements.Nodes.Length;
        var ranges = Slice<FoldingRange?>.Make(0, 40);
        while (current < count)
        {
            while (current < count && !Ast.IsAnyImportSyntax(statements.Nodes[current]))
            {
                ranges = ranges.Append(visitFoldingNode(context, statements.Nodes[current], depthRemaining, file, this));
                current++;
            }

            if (current == count)
                break;
            int first = current;
            while (current < count && Ast.IsAnyImportSyntax(statements.Nodes[current]))
            {
                ranges = ranges.Append(visitFoldingNode(context, statements.Nodes[current], depthRemaining, file, this));
                current++;
            }

            int last = current - 1;
            if (last != first)
            {
                var imports = createFoldingRangeFromBounds(context, AstNav.GetStartOfNode(AstNav.FindChildOfKind(statements.Nodes[first], Kind.ImportKeyword, file), file, false), statements.Nodes[last].End(), FoldingRangeKind.Imports, file, this);
                if (imports is not null)
                    ranges = ranges.Append(imports);
            }
        }

        return ranges.Append(visitFoldingNode(context, file.EndOfFileToken, depthRemaining, file, this));
    }

    private readonly record struct RegionStart(int Position, ByteStringValue? CollapsedText);
    private Slice<FoldingRange?> addRegionOutliningSpans(ProtocolRequestContext context, SourceFile file)
    {
        var regions = Slice<RegionStart>.Make(0, 40);
        var output = Slice<FoldingRange?>.Make(0, 40);
        var text = file.TextView();
        foreach (int start in ScannerApi.GetECMALineStarts(file))
        {
            int end = getLineEndOfPosition(file, start);
            var line = text.Slice(start, end);
            var delimiter = parseRegionDelimiter(line);
            if (delimiter is null || isInComment(file, start, AstNav.GetTokenAtPosition(file, start))is not null)
                continue;
            if (delimiter.Value.IsStart)
            {
                int position = GoStrings.Index(line.AsSpan(), "//"u8) + start;
                ByteStringValue? collapsed = null;
                if (supportsCollapsedText(context))
                    collapsed = delimiter.Value.Name.Length == 0 ? ByteStringValue.FromString("#region") : delimiter.Value.Name;
                regions = regions.Append(new RegionStart(position, collapsed));
            }
            else if (regions.Length > 0)
            {
                var region = regions[regions.Length - 1];
                regions = regions.Slice2(0, regions.Length - 1);
                var(range, fidelity) = createFoldingRangeFromBounds(region.Position, end, file);
                if (fidelity.IsNone())
                    continue;
                var folding = createFoldingRange(context, range, FoldingRangeKind.Region, default);
                folding.CollapsedText = region.CollapsedText;
                output = output.Append(folding);
            }
        }

        return output;
    }

    private static Slice<FoldingRange?> visitFoldingNode(ProtocolRequestContext context, Node node, int depthRemaining, SourceFile file, LanguageService service)
    {
        if ((node.Flags & NodeFlags.Reparsed) != 0 || depthRemaining == 0 || context.Cancellation.IsCancellationRequested)
            return default;
        var ranges = Slice<FoldingRange?>.Make(0, 40);
        if ((!Ast.IsBinaryExpression(node) && Ast.IsDeclaration(node)) || Ast.IsVariableStatement(node) || Ast.IsReturnStatement(node) || Ast.IsCallOrNewExpression(node) || node.Kind == Kind.EndOfFile)
            ranges = ranges.Append(addOutliningForLeadingCommentsForNode(context, node, file, service));
        if (Ast.IsFunctionLike(node) && node.Parent is not null && Ast.IsBinaryExpression(node.Parent) && node.Parent.AsBinaryExpression().Left is { } left && Ast.IsPropertyAccessExpression(left))
            ranges = ranges.Append(addOutliningForLeadingCommentsForNode(context, left, file, service));
        if (Ast.IsBlock(node) && node.AsBlock().Statements is { } blockStatements)
            ranges = ranges.Append(addOutliningForLeadingCommentsForPos(context, blockStatements.End(), file, service));
        if (Ast.IsModuleBlock(node) && node.AsModuleBlock().Statements is { } moduleStatements)
            ranges = ranges.Append(addOutliningForLeadingCommentsForPos(context, moduleStatements.End(), file, service));
        if (Ast.IsClassLike(node) || Ast.IsInterfaceDeclaration(node))
        {
            var members = Ast.IsClassDeclaration(node) ? node.AsClassDeclaration().Members : Ast.IsClassExpression(node) ? node.AsClassExpression().Members : node.AsInterfaceDeclaration().Members;
            if (members is not null)
                ranges = ranges.Append(addOutliningForLeadingCommentsForPos(context, members.End(), file, service));
        }

        var span = getOutliningSpanForNode(context, node, file, service);
        if (span is not null)
            ranges = ranges.Append(span);
        depthRemaining--;
        if (Ast.IsCallExpression(node))
        {
            depthRemaining++;
            var expression = visitFoldingNode(context, node.Expression(), depthRemaining, file, service);
            if (!expression.IsNil)
                ranges = ranges.Append(expression);
            depthRemaining--;
            foreach (var argument in node.Arguments())
                if (argument is not null)
                    ranges = ranges.Append(visitFoldingNode(context, argument, depthRemaining, file, service));
            foreach (var argument in node.TypeArguments())
                if (argument is not null)
                    ranges = ranges.Append(visitFoldingNode(context, argument, depthRemaining, file, service));
        }
        else if (Ast.IsIfStatement(node) && node.AsIfStatement().ElseStatement is { } elseStatement && Ast.IsIfStatement(elseStatement))
        {
            var expression = visitFoldingNode(context, node.Expression(), depthRemaining, file, service);
            if (!expression.IsNil)
                ranges = ranges.Append(expression);
            var then = visitFoldingNode(context, node.AsIfStatement().ThenStatement, depthRemaining, file, service);
            if (!then.IsNil)
                ranges = ranges.Append(then);
            depthRemaining++;
            var otherwise = visitFoldingNode(context, elseStatement, depthRemaining, file, service);
            if (!otherwise.IsNil)
                ranges = ranges.Append(otherwise);
            depthRemaining--;
        }
        else
            node.ForEachChild(child =>
            {
                var children = visitFoldingNode(context, child, depthRemaining, file, service);
                if (!children.IsNil)
                    ranges = ranges.Append(children);
                return false;
            });
        depthRemaining++;
        return ranges;
    }

    private static Slice<FoldingRange?> addOutliningForLeadingCommentsForNode(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service) => Ast.IsJsxText(node) ? default : addOutliningForLeadingCommentsForPos(context, node.Pos(), file, service);
    private static Slice<FoldingRange?> addOutliningForLeadingCommentsForPos(ProtocolRequestContext context, int pos, SourceFile file, LanguageService service)
    {
        var emit = new EmitContext();
        var ranges = Slice<FoldingRange?>.Make(0, 40);
        int first = -1, last = -1, count = 0;
        FoldingRange? Combine() => count > 1 ? createFoldingRangeFromBounds(context, first, last, FoldingRangeKind.Comment, file, service) : null;
        var text = file.TextView();
        foreach (var comment in ScannerApi.GetLeadingCommentRangesValue(emit.Factory.AsNodeFactory(), text, pos))
        {
            int start = comment.Pos, end = comment.End;
            if (context.Cancellation.IsCancellationRequested)
                return default;
            switch (comment.Kind)
            {
                case Kind.SingleLineCommentTrivia:
                    if (parseRegionDelimiter(text.Slice(start, end))is not null)
                    {
                        var previous = Combine();
                        if (previous is not null)
                            ranges = ranges.Append(previous);
                        count = 0;
                        break;
                    }

                    if (count == 0)
                        first = start;
                    last = end;
                    count++;
                    break;
                case Kind.MultiLineCommentTrivia:
                    var combined = Combine();
                    if (combined is not null)
                        ranges = ranges.Append(combined);
                    var block = createFoldingRangeFromBounds(context, start, end, FoldingRangeKind.Comment, file, service);
                    if (block is not null)
                        ranges = ranges.Append(block);
                    count = 0;
                    break;
                default:
                    throw new InvalidOperationException("Debug failure. Illegal value: " + comment.Kind.String());
            }
        }

        var added = Combine();
        if (added is not null)
            ranges = ranges.Append(added);
        return ranges;
    }

    private readonly record struct RegionDelimiterResult(bool IsStart, ByteStringValue Name);
    private static RegionDelimiterResult? parseRegionDelimiter(ByteStringView text)
    {
        text = GoStrings.TrimLeftFunc(text, GoUnicode.IsSpace);
        if (!GoStrings.HasPrefix(text.AsSpan(), "//"u8))
            return null;
        text = GoStrings.TrimLeftFunc(text.Slice(2), GoUnicode.IsSpace);
        text = GoStrings.TrimRightFunc(text, GoUnicode.IsSpace);
        text = GoStrings.TrimSuffix(text, "\r");
        if (!GoStrings.HasPrefix(text.AsSpan(), "#"u8))
            return null;
        text = text.Slice(1);
        bool start = true;
        if (GoStrings.HasPrefix(text.AsSpan(), "end"u8))
        {
            start = false;
            text = text.Slice(3);
        }

        if (!GoStrings.HasPrefix(text.AsSpan(), "region"u8))
            return null;
        text = GoStrings.TrimLeftFunc(text.Slice(6), GoUnicode.IsSpace);
        text = GoStrings.TrimRightFunc(text, GoUnicode.IsSpace);
        return new RegionDelimiterResult(start, ByteStringValue.FromView(text));
    }

    private static FoldingRange? getOutliningSpanForNode(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        switch (node.Kind)
        {
            case Kind.Block:
                if (Ast.IsFunctionLike(node.Parent))
                    return functionSpan(context, node.Parent, node, file, service);
                switch (node.Parent.Kind)
                {
                    case Kind.DoStatement:
                    case Kind.ForInStatement:
                    case Kind.ForOfStatement:
                    case Kind.ForStatement:
                    case Kind.IfStatement:
                    case Kind.WhileStatement:
                    case Kind.WithStatement:
                    case Kind.CatchClause:
                        return spanForNode(context, node, Kind.OpenBraceToken, true, file, service);
                    case Kind.TryStatement:
                        var statement = node.Parent.AsTryStatement();
                        if (statement.TryBlock == node)
                            return spanForNode(context, node, Kind.OpenBraceToken, true, file, service);
                        if (statement.FinallyBlock == node)
                        {
                            var span = spanForNode(context, node, Kind.OpenBraceToken, true, file, service);
                            if (span is not null)
                                return span;
                        }

                        goto default;
                    default:
                        var(range, fidelity) = service.createLspRangeFromNodeForFeature(node, file, Feature.FoldingRanges);
                        return fidelity.IsNone() ? null : createFoldingRange(context, range, default, default);
                }

            case Kind.ModuleBlock:
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.InterfaceDeclaration:
            case Kind.EnumDeclaration:
            case Kind.CaseBlock:
            case Kind.TypeLiteral:
            case Kind.ObjectBindingPattern:
                return spanForNode(context, node, Kind.OpenBraceToken, true, file, service);
            case Kind.TupleType:
                return spanForNode(context, node, Kind.OpenBracketToken, !Ast.IsTupleTypeNode(node.Parent), file, service);
            case Kind.CaseClause:
            case Kind.DefaultClause:
                return spanForNodeArray(context, node.AsCaseOrDefaultClause().Statements, file, service);
            case Kind.ObjectLiteralExpression:
                return spanForNode(context, node, Kind.OpenBraceToken, !Ast.IsArrayLiteralExpression(node.Parent) && !Ast.IsCallExpression(node.Parent), file, service);
            case Kind.ArrayLiteralExpression:
                return spanForNode(context, node, Kind.OpenBracketToken, !Ast.IsArrayLiteralExpression(node.Parent) && !Ast.IsCallExpression(node.Parent), file, service);
            case Kind.JsxElement:
            case Kind.JsxFragment:
                return spanForJSXElement(context, node, file, service);
            case Kind.JsxSelfClosingElement:
            case Kind.JsxOpeningElement:
                return spanForJSXAttributes(context, node, file, service);
            case Kind.TemplateExpression:
            case Kind.NoSubstitutionTemplateLiteral:
                return spanForTemplateLiteral(context, node, file, service);
            case Kind.ArrayBindingPattern:
                return spanForNode(context, node, Kind.OpenBracketToken, !Ast.IsBindingElement(node.Parent), file, service);
            case Kind.ArrowFunction:
                return spanForArrowFunction(context, node, file, service);
            case Kind.CallExpression:
                return spanForCallExpression(context, node, file, service);
            case Kind.ParenthesizedExpression:
                return spanForParenthesizedExpression(context, node, file, service);
            case Kind.NamedImports:
            case Kind.NamedExports:
            case Kind.ImportAttributes:
                return spanForImportExportElements(context, node, file, service);
        }

        return null;
    }

    private static FoldingRange? spanForImportExportElements(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        NodeList? elements = node.Kind switch
        {
            Kind.NamedImports => node.AsNamedImports().Elements,
            Kind.NamedExports => node.AsNamedExports().Elements,
            Kind.ImportAttributes => node.AsImportAttributes().Attributes,
            _ => null
        };
        if (elements is null || elements.Nodes.Length == 0)
            return null;
        var open = AstNav.FindChildOfKind(node, Kind.OpenBraceToken, file);
        var close = AstNav.FindChildOfKind(node, Kind.CloseBraceToken, file);
        return open is null || close is null || PrinterUtilities.PositionsAreOnSameLine(open.Pos(), close.Pos(), file) ? null : rangeBetweenTokens(context, open, close, file, false, service);
    }

    private static FoldingRange? spanForParenthesizedExpression(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        int start = AstNav.GetStartOfNode(node, file, false);
        if (PrinterUtilities.PositionsAreOnSameLine(start, node.End(), file))
            return null;
        var(range, fidelity) = service.createFoldingRangeFromBounds(start, node.End(), file);
        return fidelity.IsNone() ? null : createFoldingRange(context, range, default, default);
    }

    private static FoldingRange? spanForCallExpression(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        if (node.AsCallExpression().Arguments is not { } arguments || arguments.Nodes.Length == 0)
            return null;
        var open = AstNav.FindChildOfKind(node, Kind.OpenParenToken, file);
        var close = AstNav.FindChildOfKind(node, Kind.CloseParenToken, file);
        return open is null || close is null || PrinterUtilities.PositionsAreOnSameLine(open.Pos(), close.Pos(), file) ? null : rangeBetweenTokens(context, open, close, file, true, service);
    }

    private static FoldingRange? spanForArrowFunction(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        var body = node.AsArrowFunction().Body;
        if (Ast.IsBlock(body) || Ast.IsParenthesizedExpression(body) || PrinterUtilities.PositionsAreOnSameLine(body.Pos(), body.End(), file))
            return null;
        var(range, fidelity) = service.createFoldingRangeFromBounds(body.Pos(), body.End(), file);
        return fidelity.IsNone() ? null : createFoldingRange(context, range, default, default);
    }

    private static FoldingRange? spanForTemplateLiteral(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service) => node.Kind == Kind.NoSubstitutionTemplateLiteral && node.TextValue().Length == 0 ? null : createFoldingRangeFromBounds(context, AstNav.GetStartOfNode(node, file, false), node.End(), default, file, service);
    private static FoldingRange? spanForJSXElement(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        if (node.Kind == Kind.JsxElement)
        {
            var element = node.AsJsxElement();
            var(range, fidelity) = service.createFoldingRangeFromBounds(AstNav.GetStartOfNode(element.OpeningElement, file, false), element.ClosingElement.End(), file);
            if (fidelity.IsNone())
                return null;
            var name = ScannerApi.GetTextOfNodeValue(element.OpeningElement.TagName());
            var builder = new GoBuilder();
            builder.Grow(8 + name.Length * 2);
            builder.WriteString("<");
            builder.WriteString(name);
            builder.WriteString(">...</");
            builder.WriteString(name);
            builder.WriteString(">");
            return createFoldingRange(context, range, default, builder.Value());
        }

        var fragment = node.AsJsxFragment();
        var(fragmentRange, fragmentFidelity) = service.createFoldingRangeFromBounds(AstNav.GetStartOfNode(fragment.OpeningFragment, file, false), fragment.ClosingFragment.End(), file);
        return fragmentFidelity.IsNone() ? null : createFoldingRange(context, fragmentRange, default, ByteStringValue.FromString("<>...</>"));
    }

    private static FoldingRange? spanForJSXAttributes(ProtocolRequestContext context, Node node, SourceFile file, LanguageService service)
    {
        var attributes = node.Kind == Kind.JsxSelfClosingElement ? node.AsJsxSelfClosingElement().Attributes : node.AsJsxOpeningElement().Attributes;
        return attributes.Properties().Length == 0 ? null : createFoldingRangeFromBounds(context, AstNav.GetStartOfNode(node, file, false), node.End(), default, file, service);
    }

    private static FoldingRange? spanForNodeArray(ProtocolRequestContext context, NodeList? statements, SourceFile file, LanguageService service)
    {
        if (statements is null || statements.Nodes.Length == 0)
            return null;
        var(range, fidelity) = service.createFoldingRangeFromBounds(statements.Pos(), statements.End(), file);
        return fidelity.IsNone() ? null : createFoldingRange(context, range, default, default);
    }

    private static FoldingRange? spanForNode(ProtocolRequestContext context, Node node, Kind openKind, bool useFullStart, SourceFile file, LanguageService service)
    {
        Kind closeKind = openKind == Kind.OpenBraceToken ? Kind.CloseBraceToken : Kind.CloseBracketToken;
        var open = AstNav.FindChildOfKind(node, openKind, file);
        var close = AstNav.FindChildOfKind(node, closeKind, file);
        return open is not null && close is not null ? rangeBetweenTokens(context, open, close, file, useFullStart, service) : null;
    }

    private static FoldingRange? rangeBetweenTokens(ProtocolRequestContext context, Node open, Node close, SourceFile file, bool useFullStart, LanguageService service)
    {
        var(range, fidelity) = service.createFoldingRangeFromBounds(useFullStart ? open.Pos() : AstNav.GetStartOfNode(open, file, false), close.End(), file);
        return fidelity.IsNone() ? null : createFoldingRange(context, range, default, default);
    }

    private static bool supportsCollapsedText(ProtocolRequestContext context) => context.ClientCapabilities.TextDocument.FoldingRange.FoldingRange.CollapsedText;
    private static FoldingRange createFoldingRange(ProtocolRequestContext context, ProtocolRange range, FoldingRangeKind kind, ByteStringValue collapsedText)
    {
        var result = new FoldingRange
        {
            StartLine = range.Start.Line,
            StartCharacter = range.Start.Character,
            EndLine = range.End.Line,
            EndCharacter = range.End.Character,
            Kind = kind.Value.Length == 0 ? null : kind
        };
        if (collapsedText.Length != 0 && supportsCollapsedText(context))
            result.CollapsedText = collapsedText;
        return result;
    }

    private static FoldingRange? createFoldingRangeFromBounds(ProtocolRequestContext context, int pos, int end, FoldingRangeKind kind, SourceFile file, LanguageService service)
    {
        var(range, fidelity) = service.createFoldingRangeFromBounds(pos, end, file);
        return fidelity.IsNone() ? null : createFoldingRange(context, range, kind, default);
    }

    private (ProtocolRange Range, Fidelity Fidelity) createFoldingRangeFromBounds(int start, int end, SourceFile file) => converters.ToLSPRangeForFeature(file, new TextRange(start, end), Feature.FoldingRanges);
    private static FoldingRange? functionSpan(ProtocolRequestContext context, Node node, Node body, SourceFile file, LanguageService service)
    {
        var open = tryGetFunctionOpenToken(node, body, file);
        var close = AstNav.FindChildOfKind(body, Kind.CloseBraceToken, file);
        return open is not null && close is not null ? rangeBetweenTokens(context, open, close, file, true, service) : null;
    }

    private static Node? tryGetFunctionOpenToken(Node node, Node body, SourceFile file)
    {
        if (isNodeArrayMultiLine(node.Parameters(), file))
        {
            var open = AstNav.FindChildOfKind(node, Kind.OpenParenToken, file);
            if (open is not null)
                return open;
        }

        return AstNav.FindChildOfKind(body, Kind.OpenBraceToken, file);
    }

    private static bool isNodeArrayMultiLine(Slice<Node> nodes, SourceFile file) => nodes.Length != 0 && !PrinterUtilities.PositionsAreOnSameLine(nodes[0].Pos(), nodes[nodes.Length - 1].End(), file);
}
