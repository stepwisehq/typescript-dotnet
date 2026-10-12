// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/documenthighlights.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using TypeScript.LanguageServices.Utilities;
using AstNav = TypeScript.AstNavigation.AstNav;

using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    // documenthighlights.go:22. Only the requested original document is extracted.
    internal (DocumentHighlightsOrNull Result, Exception? Error) ProvideDocumentHighlights(ProtocolRequestContext context, DocumentUri documentUri, TypeScript.Lsp.Protocol.Position documentPosition)
    {
        var (result, error) = provideDocumentHighlightsWorker(context, documentUri, documentPosition, default);
        if (error != null)
            return (new(), error);
        Slice<DocumentHighlight?> highlights = default;
        if (result.MultiDocumentHighlights != null)
            foreach (var document in result.MultiDocumentHighlights.Value)
                if (document!.Uri == documentUri)
                    highlights = highlights.Append(document.Highlights);
        return (new() { DocumentHighlights = highlights }, null);
    }

    // documenthighlights.go:40.
    internal (MultiDocumentHighlightsOrNull Result, Exception? Error) ProvideMultiDocumentHighlights(ProtocolRequestContext context, DocumentUri documentUri, TypeScript.Lsp.Protocol.Position documentPosition, Slice<DocumentUri> filesToSearch) => provideDocumentHighlightsWorker(context, documentUri, documentPosition, filesToSearch);
    // documenthighlights.go:44. Retain projected segment order and hinted capacity.
    private (MultiDocumentHighlightsOrNull Result, Exception? Error) provideDocumentHighlightsWorker(ProtocolRequestContext context, DocumentUri documentUri, TypeScript.Lsp.Protocol.Position documentPosition, Slice<DocumentUri> filesToSearch)
    {
        var (program, sourceFile) = getProgramAndFile(documentUri);
        var positions = converters.FromLSPPositionForSourceFile(sourceFile, documentPosition, Feature.DocumentHighlights);
        var results = Slice<MultiDocumentHighlightsOrNull>.Make(0, positions.Length);
        foreach (var mapped in positions)
            if (mapped.Fidelity.IsSingleSegment())
                results = results.Append(provideDocumentHighlightsAtPosition(context, documentUri, mapped.Position, program, mapped.Script, filesToSearch));
        return (combineMultiDocumentHighlights(results), null);
    }

    // documenthighlights.go:57. JSX matching precedes file resolution, and
    // semantic absence falls back to the actual syntactic highlight owner.
    private MultiDocumentHighlightsOrNull provideDocumentHighlightsAtPosition(ProtocolRequestContext context, DocumentUri documentUri, int position, Program program, SourceFile sourceFile, Slice<DocumentUri> filesToSearch)
    {
        var node = AstNav.GetTouchingPropertyName(sourceFile, position);
        if (node.Parent != null && (node.Parent.Kind == Kind.JsxClosingElement || (node.Parent.Kind == Kind.JsxOpeningElement && node.Parent.TagName() == node)))
        {
            Node? openingElement = null, closingElement = null;
            if (Ast.IsJsxElement(node.Parent.Parent))
            {
                openingElement = node.Parent.Parent.AsJsxElement().OpeningElement;
                closingElement = node.Parent.Parent.AsJsxElement().ClosingElement;
            }

            Slice<DocumentHighlight?> highlights = default;
            if (openingElement != null)
            {
                var (range, fidelity) = createLspRangeFromNodeForFeature(openingElement, sourceFile, Feature.DocumentHighlights);
                if (!fidelity.IsNone())
                    highlights = highlights.Append(new DocumentHighlight { Range = range, Kind = DocumentHighlightKind.Read });
            }

            if (closingElement != null)
            {
                var (range, fidelity) = createLspRangeFromNodeForFeature(closingElement, sourceFile, Feature.DocumentHighlights);
                if (!fidelity.IsNone())
                    highlights = highlights.Append(new DocumentHighlight { Range = range, Kind = DocumentHighlightKind.Read });
            }

            return new()
            {
                MultiDocumentHighlights = Slice<MultiDocumentHighlight?>.FromArray([new MultiDocumentHighlight { Uri = documentUri, Highlights = highlights }])
            };
        }

        Slice<SourceFile> sourceFiles = default;
        var seenFiles = new HashSet<RootedFilePath>(filesToSearch.Length);
        foreach (var uri in filesToSearch)
        {
            var fileName = uri.FileName();
            if (!seenFiles.Add(fileName))
                continue;
            var file = program.GetSourceFile(fileName);
            if (file != null)
                sourceFiles = sourceFiles.Append(file);
        }

        if (sourceFiles.Length == 0)
            sourceFiles = Slice<SourceFile>.FromArray([sourceFile]);
        var multiHighlights = getSemanticDocumentHighlights(context, position, node, program, sourceFiles);
        if (multiHighlights.Length == 0)
        {
            var syntacticHighlights = getSyntacticDocumentHighlights(node, sourceFile);
            if (syntacticHighlights.Length > 0)
                multiHighlights = Slice<MultiDocumentHighlight?>.FromArray([new MultiDocumentHighlight { Uri = documentUri, Highlights = syntacticHighlights }]);
        }

        return new()
        {
            MultiDocumentHighlights = multiHighlights
        };
    }

    // documenthighlights.go:119. Map value sets retain nil backing until Add;
    // assignment after each document preserves the original Set header update.
    private static MultiDocumentHighlightsOrNull combineMultiDocumentHighlights(Slice<MultiDocumentHighlightsOrNull> results)
    {
        var byUri = new Dictionary<DocumentUri, MultiDocumentHighlight>();
        var seen = new Dictionary<DocumentUri, HashSet<TypeScript.Lsp.Protocol.Range>?>();
        Slice<MultiDocumentHighlight?> combinedDocuments = default;
        foreach (var result in results)
        {
            if (result.MultiDocumentHighlights == null)
                continue;
            foreach (var optionalDocument in result.MultiDocumentHighlights.Value)
            {
                var document = optionalDocument!;
                if (!byUri.TryGetValue(document.Uri, out var combinedDocument))
                {
                    combinedDocument = new MultiDocumentHighlight
                    {
                        Uri = document.Uri
                    };
                    byUri[document.Uri] = combinedDocument;
                    combinedDocuments = combinedDocuments.Append(combinedDocument);
                }

                var ranges = seen.GetValueOrDefault(document.Uri);
                foreach (var optionalHighlight in document.Highlights)
                {
                    var highlight = optionalHighlight!;
                    if ((ranges ??= new()).Add(highlight.Range))
                        combinedDocument.Highlights = combinedDocument.Highlights.Append(highlight);
                }

                seen[document.Uri] = ranges;
            }
        }

        return new()
        {
            MultiDocumentHighlights = combinedDocuments
        };
    }

    // documenthighlights.go:145. Original filenames are byte-content map keys.
    private Slice<MultiDocumentHighlight?> getSemanticDocumentHighlights(ProtocolRequestContext context, int position, Node node, Program program, Slice<SourceFile> sourceFiles)
    {
        var referenceEntries = getReferencedSymbolsForNode(context, position, node, program, sourceFiles, new RefOptions { use = ReferenceUse.None });
        if (referenceEntries.IsNil)
            return default;
        var fileHighlights = new Dictionary<ByteStringKey, Slice<DocumentHighlight?>>();
        foreach (var entry in referenceEntries)
            foreach (var reference in entry.references)
            {
                var (fileName, highlight) = toDocumentHighlight(reference);
                if (highlight == null)
                    continue;
                var key = ByteStringKey.FromValue(fileName);
                fileHighlights[key] = fileHighlights.GetValueOrDefault(key).Append(highlight);
            }

        Slice<MultiDocumentHighlight?> result = default;
        foreach (var file in sourceFiles)
        {
            var fileName = file.OriginalFileName();
            if (fileHighlights.TryGetValue(ByteStringKey.FromValue(fileName.AsValue()), out var highlights))
                result = result.Append(new MultiDocumentHighlight { Uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(fileName), Highlights = highlights });
        }

        return result;
    }

    // documenthighlights.go:171. Range-only entries are always read occurrences.
    private (ByteStringValue FileName, DocumentHighlight? Highlight) toDocumentHighlight(ReferenceEntry entry)
    {
        entry = resolveEntry(entry);
        var fileName = entry.sourceFile!.OriginalFileName().AsValue();
        var (range, ok) = getRangeOfEntryForFeature(entry, Feature.DocumentHighlights);
        if (!ok)
            return (fileName, null);
        var kind = DocumentHighlightKind.Read;
        if (entry.kind != EntryKind.Range && Ast.IsWriteAccessForReference(entry.node!))
            kind = DocumentHighlightKind.Write;
        return (fileName, new DocumentHighlight { Range = range, Kind = kind });
    }

    // documenthighlights.go:200. All branches dispatch the original syntax owners.
    private Slice<DocumentHighlight?> getSyntacticDocumentHighlights(Node node, SourceFile sourceFile)
    {
        switch (node.Kind)
        {
            case Kind.IfKeyword:
            case Kind.ElseKeyword:
                return Ast.IsIfStatement(node.Parent) ? getIfElseOccurrences(node.Parent.AsIfStatement(), sourceFile) : default;
            case Kind.ReturnKeyword:
                return useParent(node.Parent, Ast.IsReturnStatement, getReturnOccurrences, sourceFile);
            case Kind.ThrowKeyword:
                return useParent(node.Parent, Ast.IsThrowStatement, getThrowOccurrences, sourceFile);
            case Kind.TryKeyword:
            case Kind.CatchKeyword:
            case Kind.FinallyKeyword:
                return useParent(node.Kind == Kind.CatchKeyword ? node.Parent.Parent : node.Parent, Ast.IsTryStatement, getTryCatchFinallyOccurrences, sourceFile);
            case Kind.SwitchKeyword:
                return useParent(node.Parent, Ast.IsSwitchStatement, getSwitchCaseDefaultOccurrences, sourceFile);
            case Kind.CaseKeyword:
            case Kind.DefaultKeyword:
                return Ast.IsDefaultClause(node.Parent) || Ast.IsCaseClause(node.Parent) ? useParent(node.Parent.Parent.Parent, Ast.IsSwitchStatement, getSwitchCaseDefaultOccurrences, sourceFile) : default;
            case Kind.BreakKeyword:
            case Kind.ContinueKeyword:
                return useParent(node.Parent, Ast.IsBreakOrContinueStatement, getBreakOrContinueStatementOccurrences, sourceFile);
            case Kind.ForKeyword:
            case Kind.WhileKeyword:
            case Kind.DoKeyword:
                return useParent(node.Parent, static candidate => Ast.IsIterationStatement(candidate, true), getLoopBreakContinueOccurrences, sourceFile);
            case Kind.ConstructorKeyword:
                return getFromAllDeclarations(Ast.IsConstructorDeclaration, Slice<Kind>.FromArray([Kind.ConstructorKeyword]), node, sourceFile);
            case Kind.GetKeyword:
            case Kind.SetKeyword:
                return getFromAllDeclarations(Ast.IsAccessor, Slice<Kind>.FromArray([Kind.GetKeyword, Kind.SetKeyword]), node, sourceFile);
            case Kind.AwaitKeyword:
                return useParent(node.Parent, Ast.IsAwaitExpression, getAsyncAndAwaitOccurrences, sourceFile);
            case Kind.AsyncKeyword:
                return highlightSpans(getAsyncAndAwaitOccurrences(node, sourceFile), sourceFile);
            case Kind.YieldKeyword:
                return highlightSpans(getYieldOccurrences(node, sourceFile), sourceFile);
            case Kind.InKeyword:
            case Kind.OutKeyword:
                return default;
            default:
                return Ast.IsModifierKind(node.Kind) && (Ast.IsDeclaration(node.Parent) || Ast.IsVariableStatement(node.Parent)) ? highlightSpans(getModifierOccurrences(node.Kind, node.Parent, sourceFile), sourceFile) : default;
        }
    }

    // documenthighlights.go:255.
    private Slice<DocumentHighlight?> useParent(Node node, Func<Node, bool> nodeTest, Func<Node, SourceFile, Slice<Node>> getNodes, SourceFile sourceFile) => nodeTest(node) ? highlightSpans(getNodes(node, sourceFile), sourceFile) : default;
    // documenthighlights.go:262. Nil child pointers are deliberately omitted.
    private Slice<DocumentHighlight?> highlightSpans(Slice<Node> nodes, SourceFile sourceFile)
    {
        if (nodes.Length == 0)
            return default;
        Slice<DocumentHighlight?> highlights = default;
        foreach (var node in nodes)
            if (node != null)
            {
                var (range, fidelity) = createLspRangeFromNodeForFeature(node, sourceFile, Feature.DocumentHighlights);
                if (!fidelity.IsNone())
                    highlights = highlights.Append(new DocumentHighlight { Range = range, Kind = DocumentHighlightKind.Read });
            }

        return highlights;
    }

    // documenthighlights.go:277. Exactly the first matching keyword child is kept
    // for each declaration; the original source-file argument remains unchanged.
    private Slice<DocumentHighlight?> getFromAllDeclarations(Func<Node, bool> nodeTest, Slice<Kind> keywords, Node node, SourceFile sourceFile)
    {
        return useParent(node.Parent, nodeTest, (declaration, file) =>
        {
            Slice<Node> symbolDeclarations = default;
            if (Ast.CanHaveSymbol(declaration) && declaration.Symbol() is { } symbol)
                foreach (var candidate in symbol.Declarations)
                    if (nodeTest(candidate))
                    {
                        foreach (var child in getChildrenFromNonJSDocNode(candidate, sourceFile))
                        {
                            bool found = false;
                            foreach (var keyword in keywords)
                                if (child.Kind == keyword)
                                {
                                    symbolDeclarations = symbolDeclarations.Append(child);
                                    found = true;
                                    break;
                                }

                            if (found)
                                break;
                        }
                    }

            return symbolDeclarations;
        }, sourceFile);
    }

    // documenthighlights.go:300. Adjacent else/if keywords share a range only
    // when their intervening original bytes are single-line whitespace.
    private Slice<DocumentHighlight?> getIfElseOccurrences(IfStatement statement, SourceFile sourceFile)
    {
        var keywords = getIfElseKeywords(statement, sourceFile);
        Slice<DocumentHighlight?> highlights = default;
        for (int i = 0; i < keywords.Count; i++)
        {
            if (keywords[i].Kind == Kind.ElseKeyword && i < keywords.Count - 1)
            {
                var elseKeyword = keywords[i];
                var ifKeyword = keywords[i + 1];
                bool shouldCombine = true;
                int start = TypeScript.Scanner.Scanner.GetTokenPosOfNode(ifKeyword, sourceFile, false);
                if (start < 0)
                    start = ifKeyword.Pos();
                for (int j = start - 1; j >= elseKeyword.End(); j--)
                    if (!StringUtil.IsWhiteSpaceSingleLine(sourceFile.TextView()[j]))
                    {
                        shouldCombine = false;
                        break;
                    }

                if (shouldCombine)
                {
                    var (range, fidelity) = createLspRangeFromBounds(TypeScript.Scanner.Scanner.SkipTrivia(sourceFile.TextView(), elseKeyword.Pos()), ifKeyword.End(), sourceFile);
                    if (!fidelity.IsNone())
                        highlights = highlights.Append(new DocumentHighlight { Range = range, Kind = DocumentHighlightKind.Read });
                    i++;
                    continue;
                }
            }

            var (ordinaryRange, ordinaryFidelity) = createLspRangeFromNodeForFeature(keywords[i], sourceFile, Feature.DocumentHighlights);
            if (!ordinaryFidelity.IsNone())
                highlights = highlights.Append(new DocumentHighlight { Range = ordinaryRange, Kind = DocumentHighlightKind.Read });
        }

        return highlights;
    }

    // documenthighlights.go:346. Climb only through actual else-branch identity,
    // then collect the same chain in original keyword order.
    private static Slice<Node> getIfElseKeywords(IfStatement statement, SourceFile sourceFile)
    {
        while (Ast.IsIfStatement(statement.AsNode().Parent))
        {
            var parent = statement.AsNode().Parent.AsIfStatement();
            if (parent.ElseStatement != statement.AsNode())
                break;
            statement = parent;
        }

        Slice<Node> keywords = default;
        while (true)
        {
            var children = getChildrenFromNonJSDocNode(statement.AsNode(), sourceFile);
            if (children.Count > 0 && children[0].Kind == Kind.IfKeyword)
                keywords = keywords.Append(children[0]);
            for (int i = children.Count - 1; i >= 0; i--)
                if (children[i].Kind == Kind.ElseKeyword)
                {
                    keywords = keywords.Append(children[i]);
                    break;
                }

            if (statement.ElseStatement == null || !Ast.IsIfStatement(statement.ElseStatement))
                break;
            statement = statement.ElseStatement.AsIfStatement();
        }

        return keywords;
    }

    // documenthighlights.go:384.
    private static Slice<Node> getReturnOccurrences(Node node, SourceFile sourceFile)
    {
        var function = Ast.FindAncestor(node.Parent, Ast.IsFunctionLike);
        if (function == null)
            return default;
        Slice<Node> keywords = default;
        var body = function.Body();
        if (body != null)
        {
            Ast.ForEachReturnStatement(body, statement =>
            {
                var keyword = AstNav.FindChildOfKind(statement, Kind.ReturnKeyword, sourceFile);
                if (keyword != null)
                    keywords = keywords.Append(keyword);
                return false;
            });
            foreach (var statement in aggregateOwnedThrowStatements(body, sourceFile))
            {
                var keyword = AstNav.FindChildOfKind(statement, Kind.ThrowKeyword, sourceFile);
                if (keyword != null)
                    keywords = keywords.Append(keyword);
            }
        }

        return keywords;
    }

    // documenthighlights.go:414. A catch changes ownership; finally remains in the
    // current context. Function boundaries stop the recursive collection.
    private static Slice<Node> aggregateOwnedThrowStatements(Node node, SourceFile sourceFile)
    {
        if (Ast.IsThrowStatement(node))
            return Slice<Node>.FromArray([node]);
        if (Ast.IsTryStatement(node))
        {
            var statement = node.AsTryStatement();
            Slice<Node> result = default;
            if (statement.CatchClause != null)
                result = aggregateOwnedThrowStatements(statement.CatchClause, sourceFile);
            else if (statement.TryBlock != null)
                result = aggregateOwnedThrowStatements(statement.TryBlock, sourceFile);
            if (statement.FinallyBlock != null)
                result = result.Append(aggregateOwnedThrowStatements(statement.FinallyBlock, sourceFile));
            return result;
        }

        if (Ast.IsFunctionLike(node))
            return default;
        return flatMapChildren(node, sourceFile, aggregateOwnedThrowStatements);
    }

    // documenthighlights.go:444. The callback and accumulation header belong to
    // this traversal frame, as in the original scoped child visitor.
    private static Slice<T> flatMapChildren<T>(Node node, SourceFile sourceFile, Func<Node, SourceFile, Slice<T>> callback)
    {
        Slice<T> result = default;
        node.ForEachChild(child =>
        {
            var value = callback(child, sourceFile);
            if (!value.IsNil)
                result = result.Append(value);
            return false;
        });
        return result;
    }

    // documenthighlights.go:459.
    private static Slice<Node> getThrowOccurrences(Node node, SourceFile sourceFile)
    {
        var owner = getThrowStatementOwner(node);
        if (owner == null)
            return default;
        Slice<Node> keywords = default;
        foreach (var statement in aggregateOwnedThrowStatements(owner, sourceFile))
        {
            var keyword = AstNav.FindChildOfKind(statement, Kind.ThrowKeyword, sourceFile);
            if (keyword != null)
                keywords = keywords.Append(keyword);
        }

        if (Ast.IsFunctionBlock(owner))
            Ast.ForEachReturnStatement(owner, statement =>
            {
                var keyword = AstNav.FindChildOfKind(statement, Kind.ReturnKeyword, sourceFile);
                if (keyword != null)
                    keywords = keywords.Append(keyword);
                return false;
            });
        return keywords;
    }

    // documenthighlights.go:493. Ownership requires actual try-block identity.
    private static Node? getThrowStatementOwner(Node statement)
    {
        var child = statement;
        while (child.Parent != null)
        {
            var parent = child.Parent;
            if (Ast.IsFunctionBlock(parent) || parent.Kind == Kind.SourceFile)
                return parent;
            if (Ast.IsTryStatement(parent))
            {
                var owner = parent.AsTryStatement();
                if (owner.TryBlock == child && owner.CatchClause != null)
                    return child;
            }

            child = parent;
        }

        return null;
    }

    // documenthighlights.go:517.
    private static Slice<Node> getTryCatchFinallyOccurrences(Node node, SourceFile sourceFile)
    {
        var statement = node.AsTryStatement();
        Slice<Node> keywords = default;
        var token = LsUtil.GetFirstToken(node, sourceFile);
        if (token != null && token.Kind == Kind.TryKeyword)
            keywords = keywords.Append(token);
        if (statement.CatchClause != null)
        {
            var keyword = AstNav.FindChildOfKind(node, Kind.CatchKeyword, sourceFile);
            if (keyword != null)
                keywords = keywords.Append(keyword);
        }

        if (statement.FinallyBlock != null)
        {
            var keyword = AstNav.FindChildOfKind(node, Kind.FinallyKeyword, sourceFile);
            if (keyword != null)
                keywords = keywords.Append(keyword);
        }

        return keywords;
    }

    // documenthighlights.go:543.
    private static Slice<Node> getSwitchCaseDefaultOccurrences(Node node, SourceFile sourceFile)
    {
        var statement = node.AsSwitchStatement();
        Slice<Node> keywords = default;
        var token = LsUtil.GetFirstToken(node, sourceFile);
        if (token.Kind == Kind.SwitchKeyword)
            keywords = keywords.Append(token);
        foreach (var clause in statement.CaseBlock.AsCaseBlock().Clauses.Nodes)
        {
            var clauseToken = LsUtil.GetFirstToken(clause, sourceFile);
            if (clauseToken.Kind is Kind.CaseKeyword or Kind.DefaultKeyword)
                keywords = keywords.Append(clauseToken);
            foreach (var jump in aggregateAllBreakAndContinueStatements(clause, sourceFile))
                if (jump.Kind == Kind.BreakStatement && ownsBreakOrContinueStatement(statement.AsNode(), jump))
                    keywords = keywords.Append(LsUtil.GetFirstToken(jump, sourceFile));
        }

        return keywords;
    }

    // documenthighlights.go:570.
    private static Slice<Node> aggregateAllBreakAndContinueStatements(Node node, SourceFile sourceFile)
    {
        if (Ast.IsBreakOrContinueStatement(node))
            return Slice<Node>.FromArray([node]);
        if (Ast.IsFunctionLike(node))
            return default;
        return flatMapChildren(node, sourceFile, aggregateAllBreakAndContinueStatements);
    }

    // documenthighlights.go:580.
    private static bool ownsBreakOrContinueStatement(Node owner, Node statement)
    {
        var actualOwner = getBreakOrContinueOwner(statement);
        return actualOwner != null && actualOwner == owner;
    }

    // documenthighlights.go:588. Explicit state retains the statement identity
    // through ancestor traversal without a freshly bound callback per ancestor.
    private static Node? getBreakOrContinueOwner(Node statement) => Ast.FindAncestorOrQuit(statement, statement, static (node, jump) =>
    {
        switch (node.Kind)
        {
            case Kind.SwitchStatement:
                if (jump.Kind == Kind.ContinueStatement)
                    return FindAncestorResult.False;
                goto case Kind.ForStatement;
            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.WhileStatement:
            case Kind.DoStatement:
                return jump.Label() == null || isLabeledBy(node, jump.Label().TextValue()) ? FindAncestorResult.True : FindAncestorResult.False;
            default:
                return Ast.IsFunctionLike(node) ? FindAncestorResult.Quit : FindAncestorResult.False;
        }
    });
    // documenthighlights.go:613.
    private static bool isLabeledBy(Node node, ByteStringValue labelName) => Ast.FindAncestorOrQuit(node.Parent, labelName, static (owner, name) =>
    {
        if (!Ast.IsLabeledStatement(owner))
            return FindAncestorResult.Quit;
        return owner.Label().TextValue().EqualsValue(name) ? FindAncestorResult.True : FindAncestorResult.False;
    }) != null;
    // documenthighlights.go:626.
    private static Slice<Node> getBreakOrContinueStatementOccurrences(Node node, SourceFile sourceFile)
    {
        var owner = getBreakOrContinueOwner(node);
        if (owner != null)
            switch (owner.Kind)
            {
                case Kind.ForStatement:
                case Kind.ForInStatement:
                case Kind.ForOfStatement:
                case Kind.DoStatement:
                case Kind.WhileStatement:
                    return getLoopBreakContinueOccurrences(owner, sourceFile);
                case Kind.SwitchStatement:
                    return getSwitchCaseDefaultOccurrences(owner, sourceFile);
            }

        return default;
    }

    // documenthighlights.go:640.
    private static Slice<Node> getLoopBreakContinueOccurrences(Node node, SourceFile sourceFile)
    {
        Slice<Node> keywords = default;
        var token = LsUtil.GetFirstToken(node, sourceFile);
        if (token.Kind is Kind.ForKeyword or Kind.DoKeyword or Kind.WhileKeyword)
        {
            keywords = keywords.Append(token);
            if (node.Kind == Kind.DoStatement)
            {
                var loopTokens = getChildrenFromNonJSDocNode(node, sourceFile);
                for (int i = loopTokens.Count - 1; i >= 0; i--)
                    if (loopTokens[i].Kind == Kind.WhileKeyword)
                    {
                        keywords = keywords.Append(loopTokens[i]);
                        break;
                    }
            }
        }

        foreach (var statement in aggregateAllBreakAndContinueStatements(node, sourceFile))
        {
            token = LsUtil.GetFirstToken(statement, sourceFile);
            if (ownsBreakOrContinueStatement(node, statement) && token.Kind is Kind.BreakKeyword or Kind.ContinueKeyword)
                keywords = keywords.Append(token);
        }

        return keywords;
    }

    // documenthighlights.go:666.
    private static Slice<Node> getAsyncAndAwaitOccurrences(Node node, SourceFile sourceFile)
    {
        var function = Ast.GetContainingFunction(node);
        if (function == null)
            return default;
        Slice<Node> keywords = default;
        foreach (var modifier in function.ModifierNodes())
            if (modifier.Kind == Kind.AsyncKeyword)
                keywords = keywords.Append(modifier);
        function.ForEachChild(child =>
        {
            traverseWithoutCrossingFunction(child, sourceFile, current =>
            {
                if (Ast.IsAwaitExpression(current))
                {
                    var token = LsUtil.GetFirstToken(current, sourceFile);
                    if (token.Kind == Kind.AwaitKeyword)
                        keywords = keywords.Append(token);
                }
            });
            return false;
        });
        return keywords;
    }

    // documenthighlights.go:695.
    private static Slice<Node> getYieldOccurrences(Node node, SourceFile sourceFile)
    {
        var function = Ast.FindAncestor(node.Parent, Ast.IsFunctionLike);
        if (function == null)
            return default;
        Slice<Node> keywords = default;
        function.ForEachChild(child =>
        {
            traverseWithoutCrossingFunction(child, sourceFile, current =>
            {
                if (Ast.IsYieldExpression(current))
                {
                    var token = LsUtil.GetFirstToken(current, sourceFile);
                    if (token.Kind == Kind.YieldKeyword)
                        keywords = keywords.Append(token);
                }
            });
            return false;
        });
        return keywords;
    }

    // documenthighlights.go:719. The callback sees each node before boundaries
    // stop recursion. One scoped owner retains the immutable file/callback state.
    private static void traverseWithoutCrossingFunction(Node node, SourceFile sourceFile, Action<Node> callback) => new FunctionBoundaryTraversal(callback).Visit(node);
    private sealed class FunctionBoundaryTraversal
    {
        private readonly Action<Node> callback;
        private readonly Visitor child;
        internal FunctionBoundaryTraversal(Action<Node> callback)
        {
            this.callback = callback;
            child = VisitChild;
        }

        internal void Visit(Node node)
        {
            callback(node);
            if (!Ast.IsFunctionLike(node) && !Ast.IsClassLike(node) && !Ast.IsInterfaceDeclaration(node) && !Ast.IsModuleDeclaration(node) && !Ast.IsTypeAliasDeclaration(node) && !Ast.IsTypeNode(node))
                node.ForEachChild(child);
        }

        private bool VisitChild(Node node)
        {
            Visit(node);
            return false;
        }
    }

    // documenthighlights.go:729.
    private static Slice<Node> getModifierOccurrences(Kind kind, Node node, SourceFile sourceFile)
    {
        Slice<Node> result = default;
        foreach (var candidate in getNodesToSearchForModifier(node, Ast.ModifierToFlag(kind)))
        {
            var modifier = findModifier(candidate, kind);
            if (modifier != null)
                result = result.Append(modifier);
        }

        return result;
    }

    // documenthighlights.go:743. Required append copies preserve the independent
    // result header; members and parameters remain borrowed until appended.
    private static Slice<Node> getNodesToSearchForModifier(Node declaration, ModifierFlags modifierFlag)
    {
        Slice<Node> result = default;
        var container = declaration.Parent;
        if (container == null)
            return default;
        switch (container.Kind)
        {
            case Kind.ModuleBlock:
            case Kind.SourceFile:
            case Kind.Block:
            case Kind.CaseClause:
            case Kind.DefaultClause:
                if ((modifierFlag & ModifierFlags.Abstract) != 0 && Ast.IsClassDeclaration(declaration))
                    return result.Append(declaration.Members()).Append(declaration);
                return result.Append(container.Statements());
            case Kind.Constructor:
            case Kind.MethodDeclaration:
            case Kind.FunctionDeclaration:
                result = result.Append(container.Parameters());
                if (Ast.IsClassLike(container.Parent))
                    result = result.Append(container.Parent.Members());
                return result;
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.InterfaceDeclaration:
            case Kind.TypeLiteral:
                var members = container.Members();
                result = result.Append(members);
                if ((modifierFlag & (ModifierFlags.AccessibilityModifier | ModifierFlags.Readonly)) != 0)
                {
                    Node? constructor = null;
                    foreach (var member in members)
                        if (Ast.IsConstructorDeclaration(member))
                        {
                            constructor = member;
                            break;
                        }

                    if (constructor != null)
                        result = result.Append(constructor.Parameters());
                }
                else if ((modifierFlag & ModifierFlags.Abstract) != 0)
                    result = result.Append(container);
                return result;
            default:
                return default;
        }
    }

    // documenthighlights.go:790.
    private static Node? findModifier(Node node, Kind kind)
    {
        foreach (var modifier in node.ModifierNodes())
            if (modifier.Kind == kind)
                return modifier;
        return null;
    }
}
