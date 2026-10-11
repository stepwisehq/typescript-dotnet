// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/parser/jsdoc.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
public partial class Parser
{
    // parseJSDocForNode lazily parses JSDoc for a node in a TS file.
    // Called on first access to Node.JSDoc() for non-JS source files.
    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:19:1.
    private static Slice<Node> parseJSDocForNode(SourceFile sourceFile, Node node)
    {
        var p = getParser();
        // Go parser-pool return omitted: this port creates an isolated managed parser.
        p.initializeState(sourceFile.ParseOptions(), sourceFile.TextView(), sourceFile.ScriptKind);
        var ranges = GetJSDocCommentRanges(p.factory, default, node, sourceFile.TextView());
        if ((GoRuntime.Len(ranges) == 0))
        {
            return default;
        }

        var jsdoc = Slice<Node>.Make(0);
        var pos = node.Pos();
        {
            foreach (var comment in GoRuntime.RangeValues(ranges))
            {
                {
                    var parsed = p.parseJSDocComment(node, comment.Pos, comment.End, pos);
                    if ((parsed != null))
                    {
                        parsed.Parent = node;
                        jsdoc = GoRuntime.Append(jsdoc, parsed);
                        pos = parsed.End();
                    }
                }
            }
        }

        return jsdoc;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:56:1.
    private Slice<Node> withJSDoc(Node node, jsdocScannerInfo info)
    {
        if (((info & global::TypeScript.Scanner.jsdocScannerInfo.HasJSDoc) == 0))
        {
            return default;
        }

        if (!this.isJavaScript())
        {
            node.Flags |= NodeFlags.HasJSDoc;
            if (((info & global::TypeScript.Scanner.jsdocScannerInfo.HasDeprecated) != 0))
            {
                node.Flags |= NodeFlags.PossiblyContainsDeprecatedTag;
            }

            if (((info & global::TypeScript.Scanner.jsdocScannerInfo.HasSeeOrLink) == 0))
            {
                return default;
            }
        }

        var ranges = GetJSDocCommentRanges(this.factory, this.jsdocCommentRangesSpace, node, this.sourceText);
        this.jsdocCommentRangesSpace = GoRuntime.Slice(ranges, 0, 0);
        this.hasDeprecatedTag = false;
        var jsdoc = GoRuntime.Slice(this.nodeSliceArena.NewSliceHeader(GoRuntime.Len(ranges)), 0, 0);
        var pos = node.Pos();
        {
            foreach (var comment in GoRuntime.RangeValues(ranges))
            {
                {
                    var parsed = this.parseJSDocComment(node, comment.Pos, comment.End, pos);
                    if ((parsed != null))
                    {
                        parsed.Parent = node;
                        jsdoc = GoRuntime.Append(jsdoc, parsed);
                        pos = parsed.End();
                    }
                }
            }
        }

        if ((GoRuntime.Len(jsdoc) != 0))
        {
            if (((node.Flags & NodeFlags.HasJSDoc) == 0))
            {
                node.Flags |= NodeFlags.HasJSDoc;
            }

            if (this.hasDeprecatedTag)
            {
                this.hasDeprecatedTag = false;
                node.Flags |= NodeFlags.PossiblyContainsDeprecatedTag;
            }

            if (this.isJavaScript())
            {
                this.reparseTags(node, jsdoc);
            }

            // jsdoc.go:101: append into the private parser slice.
            this.jsdocInfos = this.jsdocInfos.Append(new JSDocInfo { parent = node, jsDocs = jsdoc });
            return jsdoc;
        }

        return default;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:107:1.
    private Node parseJSDocTypeExpression(bool mayOmitBraces)
    {
        var pos = this.nodePos();
        bool hasBrace = default;
        if (mayOmitBraces)
        {
            hasBrace = this.parseOptional(Kind.OpenBraceToken);
        }
        else
        {
            hasBrace = this.parseExpected(Kind.OpenBraceToken);
        }

        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.JSDoc, true);
        var t = this.parseJSDocType();
        this.contextFlags = saveContextFlags;
        if (hasBrace)
        {
            this.parseExpectedJSDoc(Kind.CloseBraceToken);
        }

        return this.finishNode(this.factory.NewJSDocTypeExpression(t), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:126:1.
    private Node parseJSDocNameReference()
    {
        var pos = this.nodePos();
        var hasBrace = this.parseOptional(Kind.OpenBraceToken);
        var entityName = this.parseJSDocLinkName();
        if (hasBrace)
        {
            this.parseExpectedJSDoc(Kind.CloseBraceToken);
        }

        this.scanner.ResetPos(this.scanner.TokenFullStart());
        this.nextTokenJSDoc();
        return this.finishNode(this.factory.NewJSDocNameReference(entityName), pos);
    }

    // Pass end=-1 to parse the text to the end
    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:139:1.
    private Node parseJSDocComment(Node parent, int start, int end, int fullStart)
    {
        if ((end == -1))
        {
            end = GoRuntime.Len(this.sourceText);
        }

        if (!isJSDocLikeText(GoRuntime.Slice(this.sourceText, start)))
        {
            return null;
        }

        var saveSourceText = this.sourceText;
        var saveToken = this.token;
        var saveContextFlags = this.contextFlags;
        var saveParsingContexts = this.parsingContexts;
        var saveScannerState = this.scanner.Mark();
        var saveDiagnosticsLength = GoRuntime.Len(this.diagnostics);
        var saveHasParseError = this.hasParseError;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var initialIndent = ((start + 4) - ((this.sourceText.AsSpan(0, start).LastIndexOf((byte)'\n') + 1)));
        // jsdoc.go:162. Go's prefix slice shares source storage; keep a bounded
        // byte view so the scanner cannot consume the trailing */ or later text.
        this.sourceText = GoRuntime.Slice(this.sourceText, 0, (end - 2));
        this.scanner.SetByteTextInternal(this.sourceText);
        this.scanner.ResetPos((start + 3));
        this.setContextFlags(NodeFlags.JSDoc, true);
        this.parsingContexts |= (1 << (int)(ParsingContext.JSDocComment));
        var comment = this.parseJSDocCommentWorker(start, end, fullStart, initialIndent);
        if (((this.contextFlags & NodeFlags.JavaScriptFile) != 0))
        {
            this.jsdocDiagnostics = GoRuntime.Append(this.jsdocDiagnostics, GoRuntime.Slice(this.diagnostics, saveDiagnosticsLength));
        }

        this.diagnostics = GoRuntime.Slice(this.diagnostics, 0, saveDiagnosticsLength);
        this.sourceText = saveSourceText;
        this.scanner.SetByteTextInternal(this.sourceText);
        this.parsingContexts = saveParsingContexts;
        this.contextFlags = saveContextFlags;
        this.scanner.Rewind(saveScannerState);
        this.token = saveToken;
        this.hasParseError = saveHasParseError;
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return comment;
    }

    // *
    // * @param offset - the offset in the containing file
    // * @param indent - the number of spaces to consider as the margin (applies to non-first lines only)
    // 
    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:193:1.
    private Node parseJSDocCommentWorker(int start, int end, int fullStart, int indent)
    {
        var tags = GoRuntime.Slice(this.nodeSliceArena.NewSliceHeader(1), 0, 0);
        var tagsPos = -1;
        var tagsEnd = -1;
        var state = jsdocState.SawAsterisk;
        var backtickCount = 0;
        var inFencedCodeBlock = false;
        var commentParts = GoRuntime.Slice(this.nodeSliceArena.NewSliceHeader(1), 0, 0);
        var comments = this.jsdocCommentsSpace;
        var commentsPos = -1;
        var linkEnd = start;
        var margin = -1;
        Action<ByteStringView> pushComment = (text) =>
        {
            if ((margin == -1))
            {
                margin = indent;
            }

            comments = GoRuntime.Append(comments, text);
            indent += GoRuntime.Len(text);
        };
        this.nextTokenJSDoc();
        while (this.parseOptionalJsdoc(Kind.WhitespaceTrivia))
        {
        }

        if (this.parseOptionalJsdoc(Kind.NewLineTrivia))
        {
            state = jsdocState.BeginningOfLine;
            indent = 0;
        }

        while (true)
        {
            if (((this.token != Kind.BacktickToken) && (backtickCount > 0)))
            {
                if ((backtickCount >= 3))
                {
                    inFencedCodeBlock = !inFencedCodeBlock;
                }

                backtickCount = 0;
            }

            switch (this.token)
            {
                case Kind.AtToken:
                {
                    if ((inFencedCodeBlock || !this.scanner.CanFollowJSDocAt()))
                    {
                        if (inFencedCodeBlock)
                        {
                            state = jsdocState.SavingBackticks;
                        }
                        else
                        {
                            state = jsdocState.SavingComments;
                        }

                        pushComment(this.scanner.TokenTextView());
                        break;
                    }

                    comments = removeTrailingWhitespace(comments);
                    if ((commentsPos == -1))
                    {
                        commentsPos = this.nodePos();
                    }

                    var tag = this.parseTag(tags, indent);
                    if ((tagsPos == -1))
                    {
                        tagsPos = tag.Pos();
                    }

                    tags = GoRuntime.Append(tags, tag);
                    tagsEnd = tag.End();
                    state = jsdocState.BeginningOfLine;
                    margin = -1;
                    break;
                }

                case Kind.NewLineTrivia:
                {
                    comments = GoRuntime.Append(comments, this.scanner.TokenTextView());
                    state = jsdocState.BeginningOfLine;
                    indent = 0;
                    break;
                }

                case Kind.AsteriskToken:
                {
                    var asterisk = this.scanner.TokenTextView();
                    if ((state == jsdocState.SawAsterisk))
                    {
                        state = jsdocState.SavingComments;
                        pushComment(asterisk);
                    }
                    else
                    {
                        if ((state != jsdocState.BeginningOfLine))
                        {
                            throw new InvalidOperationException("state must be BeginningOfLine");
                        }

                        state = jsdocState.SawAsterisk;
                        indent += GoRuntime.Len(asterisk);
                    }

                    break;
                }

                case Kind.WhitespaceTrivia:
                {
                    if (((state == jsdocState.SavingComments) || (state == jsdocState.SavingBackticks)))
                    {
                        throw new InvalidOperationException("whitespace shouldn't come from the scanner while saving top-level comment text");
                    }

                    var whitespace = this.scanner.TokenTextView();
                    if (((margin > -1) && ((indent + GoRuntime.Len(whitespace)) > margin)))
                    {
                        var existingIndent = (margin - indent);
                        if ((existingIndent < 0))
                        {
                            existingIndent += GoRuntime.Len(whitespace);
                        }

                        if ((existingIndent < 0))
                        {
                            existingIndent = 0;
                        }

                        comments = GoRuntime.Append(comments, GoRuntime.Slice(whitespace, existingIndent));
                    }

                    indent += GoRuntime.Len(whitespace);
                    break;
                }

                case Kind.EndOfFile:
                {
                    goto loopEnd;
                }

                case Kind.JSDocCommentTextToken:
                {
                    if ((state != jsdocState.SavingBackticks))
                    {
                        if (inFencedCodeBlock)
                        {
                            state = jsdocState.SavingBackticks;
                        }
                        else
                        {
                            state = jsdocState.SavingComments;
                        }
                    }

                    pushComment(this.scanner.TokenValueTextView());
                    break;
                }

                case Kind.BacktickToken:
                {
                    backtickCount++;
                    if ((state == jsdocState.SavingBackticks))
                    {
                        state = jsdocState.SavingComments;
                    }
                    else
                    {
                        state = jsdocState.SavingBackticks;
                    }

                    pushComment(this.scanner.TokenTextView());
                    break;
                }

                case Kind.OpenBraceToken:
                {
                    if (inFencedCodeBlock)
                    {
                        state = jsdocState.SavingBackticks;
                        pushComment(this.scanner.TokenTextView());
                        break;
                    }

                    state = jsdocState.SavingComments;
                    var commentEnd = this.scanner.TokenFullStart();
                    var linkStart = (this.scanner.TokenEnd() - 1);
                    var link = this.parseJSDocLink(linkStart);
                    if ((link != null))
                    {
                        if ((linkEnd == start))
                        {
                            comments = removeLeadingNewlines(comments);
                        }

                        var jsdocText = this.finishNodeWithEnd(this.factory.NewJSDocTextWithFragments(JSDocTextFragments.FromViews(this.stringSliceArena.CloneHeader(comments))), linkEnd, commentEnd);
                        commentParts = commentParts.Append((ReadOnlySpan<Node>)[jsdocText, link]);
                        comments = GoRuntime.Slice(comments, 0, 0);
                        linkEnd = this.scanner.TokenEnd();
                        break;
                    }

                    goto default;
                }

                default:
                {
                    if ((state != jsdocState.SavingBackticks))
                    {
                        if (inFencedCodeBlock)
                        {
                            state = jsdocState.SavingBackticks;
                        }
                        else
                        {
                            state = jsdocState.SavingComments;
                        }
                    }

                    pushComment(this.scanner.TokenTextView());
                    break;
                }
            }

            if (((state == jsdocState.SavingComments) || (state == jsdocState.SavingBackticks)))
            {
                this.nextJSDocCommentTextToken((state == jsdocState.SavingBackticks));
            }
            else
            {
                this.nextTokenJSDoc();
            }
        }

        loopEnd:
            ;
        this.jsdocCommentsSpace = GoRuntime.Slice(comments, 0, 0);
        if ((commentsPos == -1))
        {
            commentsPos = this.scanner.TokenFullStart();
        }

        if ((GoRuntime.Len(comments) > 0))
        {
            comments[(GoRuntime.Len(comments) - 1)] = GoStrings.TrimRightFunc(comments[(GoRuntime.Len(comments) - 1)], GoUnicode.IsSpace);
            var jsdocText2 = this.finishNodeWithEnd(this.factory.NewJSDocTextWithFragments(JSDocTextFragments.FromViews(this.stringSliceArena.CloneHeader(comments))), linkEnd, commentsPos);
            commentParts = GoRuntime.Append(commentParts, jsdocText2);
        }

        if ((((GoRuntime.Len(commentParts) > 0) && (GoRuntime.Len(tags) > 0)) && (commentsPos == -1)))
        {
            throw new InvalidOperationException("having parsed tags implies that the end of the comment span should be set");
        }

        NodeList tagsNodeList = default;
        if ((tagsPos != -1))
        {
            tagsNodeList = this.newNodeList(Core.NewTextRange(tagsPos, tagsEnd), tags);
        }

        var jsdocComment = this.factory.NewJSDoc(this.newNodeList(Core.NewTextRange(start, commentsPos), commentParts), tagsNodeList);
        return this.finishNodeWithEnd(jsdocComment, fullStart, end);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:380:1.
    private static Slice<ByteStringView> removeLeadingNewlines(Slice<ByteStringView> comments)
    {
        var i = 0;
        while (((i < GoRuntime.Len(comments)) && (GoStrings.TrimLeftFunc(comments[i], rune => rune is '\r' or '\n').Length == 0)))
        {
            i++;
        }

        return GoRuntime.Slice(comments, i);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:388:1.
    private static ByteStringView trimEnd(ByteStringView s)
    {
        return GoStrings.TrimRightFunc(s, StringUtil.IsWhiteSpaceLike);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:392:1.
    private static Slice<ByteStringView> removeTrailingWhitespace(Slice<ByteStringView> comments)
    {
        var end = GoRuntime.Len(comments);
        {
            var i = (GoRuntime.Len(comments) - 1);
            for (; (i >= 0); i--)
            {
                var trimmed = trimEnd(comments[i]);
                if ((trimmed.Length == 0))
                {
                    end = i;
                }
                else
                {
                    comments[i] = trimmed;
                    break;
                }
            }
        }

        return GoRuntime.Slice(comments, 0, end);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:406:1.
    private bool isNextNonwhitespaceTokenEndOfFile()
    {
        while (true)
        {
            this.nextTokenJSDoc();
            if ((this.token == Kind.EndOfFile))
            {
                return true;
            }

            if (!(((this.token == Kind.WhitespaceTrivia) || (this.token == Kind.NewLineTrivia))))
            {
                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:419:1.
    private void skipWhitespace()
    {
        if (((this.token == Kind.WhitespaceTrivia) || (this.token == Kind.NewLineTrivia)))
        {
            if (this.lookAhead((parserCallback) => parserCallback.isNextNonwhitespaceTokenEndOfFile()))
            {
                return;
            }
        }

        while (((this.token == Kind.WhitespaceTrivia) || (this.token == Kind.NewLineTrivia)))
        {
            this.nextTokenJSDoc();
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:431:1.
    private ByteStringView skipWhitespaceOrAsterisk()
    {
        if (((this.token == Kind.WhitespaceTrivia) || (this.token == Kind.NewLineTrivia)))
        {
            if (this.lookAhead((parserCallback) => parserCallback.isNextNonwhitespaceTokenEndOfFile()))
            {
                return "";
            }
        }

        var precedingLineBreak = this.scanner.HasPrecedingLineBreak();
        var seenLineBreak = false;
        var indents = Slice<ByteStringView>.Make(0);
        while (((((precedingLineBreak && (this.token == Kind.AsteriskToken))) || (this.token == Kind.WhitespaceTrivia)) || (this.token == Kind.NewLineTrivia)))
        {
            indents = GoRuntime.Append(indents, this.scanner.TokenTextView());
            if ((this.token == Kind.NewLineTrivia))
            {
                precedingLineBreak = true;
                seenLineBreak = true;
                indents = GoRuntime.Slice(indents, 0, 0);
            }
            else
            {
                if ((this.token == Kind.AsteriskToken))
                {
                    precedingLineBreak = false;
                }
            }

            this.nextTokenJSDoc();
        }

        if (seenLineBreak)
        {
            return GoStrings.JoinViews(indents, "");
        }
        else
        {
            return "";
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:460:1.
    private Node parseTag(Slice<Node> tags, int margin)
    {
        if ((this.token != Kind.AtToken))
        {
            throw new InvalidOperationException("should be called only at the start of a tag");
        }

        var start = this.scanner.TokenStart();
        this.nextTokenJSDoc();
        var tagName = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
        var indentText = this.skipWhitespaceOrAsterisk();
        Node tag = default;
        switch (tagName.Text())
        {
            case "implements":
            {
                tag = this.parseImplementsTag(start, tagName, margin, indentText);
                break;
            }

            case "augments":
            case "extends":
            {
                tag = this.parseAugmentsTag(start, tagName, margin, indentText);
                break;
            }

            case "public":
            {
                tag = this.parseSimpleTag(start, (tagName2, comments) =>
                {
                    return this.factory.NewJSDocPublicTag(tagName2, comments);
                }, tagName, margin, indentText);
                break;
            }

            case "private":
            {
                tag = this.parseSimpleTag(start, (tagName3, comments2) =>
                {
                    return this.factory.NewJSDocPrivateTag(tagName3, comments2);
                }, tagName, margin, indentText);
                break;
            }

            case "protected":
            {
                tag = this.parseSimpleTag(start, (tagName4, comments3) =>
                {
                    return this.factory.NewJSDocProtectedTag(tagName4, comments3);
                }, tagName, margin, indentText);
                break;
            }

            case "readonly":
            {
                tag = this.parseSimpleTag(start, (tagName5, comments4) =>
                {
                    return this.factory.NewJSDocReadonlyTag(tagName5, comments4);
                }, tagName, margin, indentText);
                break;
            }

            case "override":
            {
                tag = this.parseSimpleTag(start, (tagName6, comments5) =>
                {
                    return this.factory.NewJSDocOverrideTag(tagName6, comments5);
                }, tagName, margin, indentText);
                break;
            }

            case "deprecated":
            {
                this.hasDeprecatedTag = true;
                tag = this.parseSimpleTag(start, (tagName7, comments6) =>
                {
                    return this.factory.NewJSDocDeprecatedTag(tagName7, comments6);
                }, tagName, margin, indentText);
                break;
            }

            case "this":
            {
                tag = this.parseThisTag(start, tagName, margin, indentText);
                break;
            }

            case "arg":
            case "argument":
            case "param":
            {
                tag = this.parseParameterOrPropertyTag(start, tagName, propertyLikeParse.Parameter, margin);
                break;
            }

            case "return":
            case "returns":
            {
                tag = this.parseReturnTag(tags, start, tagName, margin, indentText);
                break;
            }

            case "template":
            {
                tag = this.parseTemplateTag(start, tagName, margin, indentText);
                break;
            }

            case "type":
            {
                tag = this.parseTypeTag(tags, start, tagName, margin, indentText);
                break;
            }

            case "typedef":
            {
                tag = this.parseTypedefTag(start, tagName, margin, indentText);
                break;
            }

            case "callback":
            {
                tag = this.parseCallbackTag(start, tagName, margin, indentText);
                break;
            }

            case "overload":
            {
                tag = this.parseOverloadTag(start, tagName, margin, indentText);
                break;
            }

            case "satisfies":
            {
                tag = this.parseSatisfiesTag(start, tagName, margin, indentText);
                break;
            }

            case "see":
            {
                tag = this.parseSeeTag(start, tagName, margin, indentText);
                break;
            }

            case "exception":
            case "throws":
            {
                tag = this.parseThrowsTag(start, tagName, margin, indentText);
                break;
            }

            case "import":
            {
                tag = this.parseImportTag(start, tagName, margin, indentText);
                break;
            }

            default:
            {
                tag = this.parseUnknownTag(start, tagName, margin, indentText);
                break;
            }
        }

        if ((tag == null))
        {
            throw new InvalidOperationException("tag should not be nil");
        }

        return tag;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:534:1.
    private NodeList parseTrailingTagComments(int pos, int end, int margin, ByteStringView indentText)
    {
        if ((GoRuntime.Len(indentText) == 0))
        {
            margin += (end - pos);
        }

        ByteStringView initialMargin = "";
        if ((margin < GoRuntime.Len(indentText)))
        {
            initialMargin = GoRuntime.Slice(indentText, margin);
        }

        return this.parseTagComments(margin, new GoRef<ByteStringView>(() => initialMargin, value => initialMargin = value));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:546:1.
    private NodeList parseTagComments(int indent, GoRef<ByteStringView> initialMargin)
    {
        var commentsPos = this.nodePos();
        var comments = this.jsdocTagCommentsSpace;
        this.jsdocTagCommentsSpace = default;
        var parts = this.jsdocTagCommentsPartsSpace;
        this.jsdocTagCommentsPartsSpace = default;
        var linkEnd = -1;
        var state = jsdocState.BeginningOfLine;
        var backtickCount = 0;
        var inFencedCodeBlock = false;
        if ((indent < 0))
        {
            throw new InvalidOperationException("indent must be a natural number");
        }

        var margin = -1;
        Action<ByteStringView> pushComment = (text) =>
        {
            if ((margin == -1))
            {
                margin = indent;
            }

            comments = GoRuntime.Append(comments, text);
            indent += GoRuntime.Len(text);
        };
        if ((initialMargin != null))
        {
            if ((initialMargin.Value.Length != 0))
            {
                pushComment(initialMargin.Value);
            }

            state = jsdocState.SawAsterisk;
        }

        var tok = this.token;
        while (true)
        {
            if (((tok != Kind.BacktickToken) && (backtickCount > 0)))
            {
                if ((backtickCount >= 3))
                {
                    inFencedCodeBlock = !inFencedCodeBlock;
                }

                backtickCount = 0;
            }

            switch (tok)
            {
                case Kind.NewLineTrivia:
                {
                    state = jsdocState.BeginningOfLine;
                    comments = GoRuntime.Append(comments, this.scanner.TokenTextView());
                    indent = 0;
                    break;
                }

                case Kind.AtToken:
                {
                    if ((!inFencedCodeBlock && this.scanner.CanFollowJSDocAt()))
                    {
                        this.scanner.ResetPos((this.scanner.TokenEnd() - 1));
                        goto loopEnd;
                    }

                    if (inFencedCodeBlock)
                    {
                        state = jsdocState.SavingBackticks;
                    }
                    else
                    {
                        state = jsdocState.SavingComments;
                    }

                    pushComment(this.scanner.TokenTextView());
                    break;
                }

                case Kind.EndOfFile:
                {
                    goto loopEnd;
                }

                case Kind.WhitespaceTrivia:
                {
                    if (((state == jsdocState.SavingComments) || (state == jsdocState.SavingBackticks)))
                    {
                        throw new InvalidOperationException("whitespace shouldn't come from the scanner while saving comment text");
                    }

                    var whitespace = this.scanner.TokenTextView();
                    if (((margin > -1) && ((indent + GoRuntime.Len(whitespace)) > margin)))
                    {
                        comments = GoRuntime.Append(comments, GoRuntime.Slice(whitespace, Math.Max((margin - indent), 0)));
                        if (inFencedCodeBlock)
                        {
                            state = jsdocState.SavingBackticks;
                        }
                        else
                        {
                            state = jsdocState.SavingComments;
                        }
                    }

                    indent += GoRuntime.Len(whitespace);
                    break;
                }

                case Kind.OpenBraceToken:
                {
                    if (inFencedCodeBlock)
                    {
                        state = jsdocState.SavingBackticks;
                        pushComment(this.scanner.TokenTextView());
                        break;
                    }

                    state = jsdocState.SavingComments;
                    var commentEnd = this.scanner.TokenFullStart();
                    var linkStart = (this.scanner.TokenEnd() - 1);
                    var link = this.parseJSDocLink(linkStart);
                    if ((link != null))
                    {
                        int commentStart = default;
                        if ((linkEnd > -1))
                        {
                            commentStart = linkEnd;
                        }
                        else
                        {
                            commentStart = commentsPos;
                        }

                        var text2 = this.finishNodeWithEnd(this.factory.NewJSDocTextWithFragments(JSDocTextFragments.FromViews(this.stringSliceArena.CloneHeader(comments))), commentStart, commentEnd);
                        parts = GoRuntime.Append(parts, text2);
                        parts = GoRuntime.Append(parts, link);
                        comments = GoRuntime.Slice(comments, 0, 0);
                        linkEnd = this.scanner.TokenEnd();
                    }
                    else
                    {
                        pushComment(this.scanner.TokenTextView());
                    }

                    break;
                }

                case Kind.BacktickToken:
                {
                    backtickCount++;
                    if ((state == jsdocState.SavingBackticks))
                    {
                        state = jsdocState.SavingComments;
                    }
                    else
                    {
                        state = jsdocState.SavingBackticks;
                    }

                    pushComment(this.scanner.TokenTextView());
                    break;
                }

                case Kind.JSDocCommentTextToken:
                {
                    if ((state != jsdocState.SavingBackticks))
                    {
                        if (inFencedCodeBlock)
                        {
                            state = jsdocState.SavingBackticks;
                        }
                        else
                        {
                            state = jsdocState.SavingComments;
                        }
                    }

                    pushComment(this.scanner.TokenValueTextView());
                    break;
                }

                case Kind.AsteriskToken:
                {
                    if ((state == jsdocState.BeginningOfLine))
                    {
                        state = jsdocState.SawAsterisk;
                        indent += 1;
                        break;
                    }

                    goto default;
                }

                default:
                {
                    if ((state != jsdocState.SavingBackticks))
                    {
                        if (inFencedCodeBlock)
                        {
                            state = jsdocState.SavingBackticks;
                        }
                        else
                        {
                            state = jsdocState.SavingComments;
                        }
                    }

                    pushComment(this.scanner.TokenTextView());
                    break;
                }
            }

            if (((state == jsdocState.SavingComments) || (state == jsdocState.SavingBackticks)))
            {
                tok = this.nextJSDocCommentTextToken((state == jsdocState.SavingBackticks));
            }
            else
            {
                tok = this.nextTokenJSDoc();
            }
        }

        loopEnd:
            ;
        this.jsdocTagCommentsSpace = GoRuntime.Slice(comments, 0, 0);
        comments = removeLeadingNewlines(comments);
        comments = removeTrailingWhitespace(comments);
        if ((GoRuntime.Len(comments) > 0))
        {
            int commentStart2 = default;
            if ((linkEnd > -1))
            {
                commentStart2 = linkEnd;
            }
            else
            {
                commentStart2 = commentsPos;
            }

            var text3 = this.finishNode(this.factory.NewJSDocTextWithFragments(JSDocTextFragments.FromViews(this.stringSliceArena.CloneHeader(comments))), commentStart2);
            parts = GoRuntime.Append(parts, text3);
        }

        this.jsdocTagCommentsPartsSpace = GoRuntime.Slice(parts, 0, 0);
        if ((GoRuntime.Len(parts) > 0))
        {
            return this.newNodeList(Core.NewTextRange(commentsPos, this.scanner.TokenEnd()), this.nodeSliceArena.CloneHeader(parts));
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:714:1.
    private Node parseJSDocLink(int start)
    {
        var state = this.mark();
        var tuple3 = this.parseJSDocLinkPrefix();
        var linkType = tuple3.Item1;
        var ok = tuple3.Item2;
        if (!ok)
        {
            this.rewind(state);
            return null;
        }

        this.nextTokenJSDoc();
        this.skipWhitespace();
        var name = this.parseJSDocLinkName();
        Slice<ByteStringView> text = default;
        while ((((this.token != Kind.CloseBraceToken) && (this.token != Kind.NewLineTrivia)) && (this.token != Kind.EndOfFile)))
        {
            text = GoRuntime.Append(text, this.scanner.TokenTextView());
            this.nextTokenJSDoc();
        }

        Node create = default;
        switch (linkType)
        {
            case "link":
            {
                create = this.factory.NewJSDocLinkWithFragments(name, JSDocTextFragments.FromViews(text));
                break;
            }

            case "linkcode":
            {
                create = this.factory.NewJSDocLinkCodeWithFragments(name, JSDocTextFragments.FromViews(text));
                break;
            }

            default:
            {
                create = this.factory.NewJSDocLinkPlainWithFragments(name, JSDocTextFragments.FromViews(text));
                break;
            }
        }

        return this.finishNodeWithEnd(create, start, this.scanner.TokenEnd());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:742:1.
    private Node parseJSDocLinkName()
    {
        if (tokenIsIdentifierOrKeyword(this.token))
        {
            var pos = this.nodePos();
            var name = this.parseIdentifierName();
            while (this.parseOptional(Kind.DotToken))
            {
                Node right = default;
                if ((this.token == Kind.PrivateIdentifier))
                {
                    right = this.createMissingIdentifier();
                }
                else
                {
                    right = this.parseIdentifierName();
                }

                name = this.finishNode(this.factory.NewQualifiedName(name, right), pos);
            }

            while ((this.token == Kind.PrivateIdentifier))
            {
                this.scanner.ReScanHashToken();
                this.nextTokenJSDoc();
                name = this.finishNode(this.factory.NewQualifiedName(name, this.parseIdentifier()), pos);
            }

            return name;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:765:1.
    private (string, bool) parseJSDocLinkPrefix()
    {
        this.skipWhitespaceOrAsterisk();
        if ((((this.token == Kind.OpenBraceToken) && (this.nextTokenJSDoc() == Kind.AtToken)) && tokenIsIdentifierOrKeyword(this.nextTokenJSDoc())))
        {
            // jsdoc.go:765: link-kind selection needs only the borrowed bytes;
            // return the existing canonical tag strings for factory dispatch.
            if (this.scanner.TokenValueEquals("link"u8)) return ("link", true);
            if (this.scanner.TokenValueEquals("linkcode"u8)) return ("linkcode", true);
            if (this.scanner.TokenValueEquals("linkplain"u8)) return ("linkplain", true);
        }

        return ("NONE", false);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:776:1.
    private static bool isJSDocLinkTag(string kind)
    {
        return (((kind == "link") || (kind == "linkcode")) || (kind == "linkplain"));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:780:1.
    private Node parseUnknownTag(int start, Node tagName, int indent, ByteStringView indentText)
    {
        return this.finishNode(this.factory.NewJSDocUnknownTag(tagName, this.parseTrailingTagComments(start, this.nodePos(), indent, indentText)), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:784:1.
    private Node tryParseTypeExpression()
    {
        this.skipWhitespaceOrAsterisk();
        if ((this.token == Kind.OpenBraceToken))
        {
            return this.parseJSDocTypeExpression(false);
        }
        else
        {
            return null;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:793:1.
    private (Node, bool) parseBracketNameInPropertyAndParamTag(propertyLikeParse target)
    {
        Node name = default;
        bool isBracketed = default;
        isBracketed = this.parseOptionalJsdoc(Kind.OpenBracketToken);
        if (isBracketed)
        {
            this.skipWhitespace();
        }

        var isBackquoted = this.parseOptionalJsdoc(Kind.BacktickToken);
        name = this.parseJSDocEntityName(Core.IfElse((target == propertyLikeParse.Parameter), null, Diagnostics.Identifier_expected));
        if (isBackquoted)
        {
            this.parseExpectedTokenJSDoc(Kind.BacktickToken);
        }

        if (isBracketed)
        {
            this.skipWhitespace();
            if ((this.parseOptionalToken(Kind.EqualsToken) != null))
            {
                this.parseExpression();
            }

            this.parseExpected(Kind.CloseBracketToken);
        }

        return (name, isBracketed);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:818:1.
    private static bool isObjectOrObjectArrayTypeReference(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ObjectKeyword:
            {
                return true;
            }

            case Kind.ArrayType:
            {
                return isObjectOrObjectArrayTypeReference(node.AsArrayTypeNode().ElementType);
            }

            default:
            {
                if (Ast.IsTypeReferenceNode(node))
                {
                    var @ref = node.AsTypeReferenceNode();
                    return ((Ast.IsIdentifier(@ref.TypeName) && (@ref.TypeName.Text() == "Object")) && (@ref.TypeArguments == null));
                }

                return false;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:833:1.
    private Node parseParameterOrPropertyTag(int start, Node tagName, propertyLikeParse target, int indent)
    {
        var typeExpression = this.tryParseTypeExpression();
        var isNameFirst = (typeExpression == null);
        this.skipWhitespaceOrAsterisk();
        var tuple4 = this.parseBracketNameInPropertyAndParamTag(target);
        var name = tuple4.Item1;
        var isBracketed = tuple4.Item2;
        var indentText = this.skipWhitespaceOrAsterisk();
        if ((isNameFirst && this.lookAhead((p) =>
        {
            var tuple5 = p.parseJSDocLinkPrefix();
            var ok = tuple5.Item2;
            return !ok;
        })))
        {
            typeExpression = this.tryParseTypeExpression();
        }

        var comment = this.parseTrailingTagComments(start, this.nodePos(), indent, indentText);
        var nestedTypeLiteral = this.parseNestedTypeLiteral(typeExpression, name, target, indent);
        if ((nestedTypeLiteral != null))
        {
            typeExpression = nestedTypeLiteral;
            isNameFirst = true;
        }

        Node result = default;
        var kind = Core.IfElse((target == propertyLikeParse.Property), Kind.JSDocPropertyTag, Kind.JSDocParameterTag);
        result = this.factory.NewJSDocParameterOrPropertyTag(kind, tagName, name, isBracketed, typeExpression, isNameFirst, comment);
        return this.finishNode(result, start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:858:1.
    private Node parseNestedTypeLiteral(Node typeExpression, Node name, propertyLikeParse target, int indent)
    {
        if (((typeExpression != null) && isObjectOrObjectArrayTypeReference(typeExpression.Type())))
        {
            var pos = this.nodePos();
            Slice<Node> children = default;
            while (true)
            {
                var state = this.mark();
                var child = this.parseChildParameterOrPropertyTag(target, indent, name);
                if ((child == null))
                {
                    this.rewind(state);
                    break;
                }

                switch (child.Kind)
                {
                    case Kind.JSDocParameterTag:
                    case Kind.JSDocPropertyTag:
                    {
                        children = GoRuntime.Append(children, child);
                        break;
                    }

                    case Kind.JSDocTemplateTag:
                    {
                        this.parseErrorAtRange(child.TagName().Loc, Diagnostics.A_JSDoc_template_tag_may_not_follow_a_typedef_callback_or_overload_tag);
                        break;
                    }
                }
            }

            if ((!children.IsNil))
            {
                var literal = this.finishNode(this.factory.NewJSDocTypeLiteral(children, (typeExpression.Type().Kind == Kind.ArrayType)), pos);
                return this.finishNode(this.factory.NewJSDocTypeExpression(literal), pos);
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:884:1.
    private Node parseReturnTag(Slice<Node> previousTags, int start, Node tagName, int indent, ByteStringView indentText)
    {
        if (Core.Some(previousTags, Ast.IsJSDocReturnTag))
        {
            this.parseErrorAt(tagName.Pos(), this.scanner.TokenStart(), Diagnostics.X_0_tag_already_specified, tagName.Text());
        }

        var typeExpression = this.tryParseTypeExpression();
        return this.finishNode(this.factory.NewJSDocReturnTag(tagName, typeExpression, this.parseTrailingTagComments(start, this.nodePos(), indent, indentText)), start);
    }

    // pass indent=-1 to skip parsing trailing comments (as when a type tag is nested in a typedef)
    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:894:1.
    private Node parseTypeTag(Slice<Node> previousTags, int start, Node tagName, int indent, ByteStringView indentText)
    {
        if (Core.Some(previousTags, Ast.IsJSDocTypeTag))
        {
            this.parseErrorAt(tagName.Pos(), this.scanner.TokenStart(), Diagnostics.X_0_tag_already_specified, tagName.Text());
        }

        var typeExpression = this.parseJSDocTypeExpression(true);
        NodeList comments = default;
        if ((indent != -1))
        {
            comments = this.parseTrailingTagComments(start, this.nodePos(), indent, indentText);
        }

        return this.finishNode(this.factory.NewJSDocTypeTag(tagName, typeExpression, comments), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:907:1.
    private Node parseSeeTag(int start, Node tagName, int indent, ByteStringView indentText)
    {
        var hasNameReference = ((this.isIdentifier() && !GoStrings.HasPrefix(this.sourceText.AsSpan(this.scanner.TokenEnd()), "://"u8)) || ((this.token == Kind.OpenBraceToken) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeyword())));
        Node nameExpression = default;
        if (hasNameReference)
        {
            nameExpression = this.parseJSDocNameReference();
        }

        var comments = this.parseTrailingTagComments(start, this.nodePos(), indent, indentText);
        return this.finishNode(this.factory.NewJSDocSeeTag(tagName, nameExpression, comments), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:918:1.
    private Node parseImplementsTag(int start, Node tagName, int margin, ByteStringView indentText)
    {
        var className = this.parseExpressionWithTypeArgumentsForAugments();
        return this.finishNode(this.factory.NewJSDocImplementsTag(tagName, className, this.parseTrailingTagComments(start, this.nodePos(), margin, indentText)), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:923:1.
    private Node parseAugmentsTag(int start, Node tagName, int margin, ByteStringView indentText)
    {
        var className = this.parseExpressionWithTypeArgumentsForAugments();
        return this.finishNode(this.factory.NewJSDocAugmentsTag(tagName, className, this.parseTrailingTagComments(start, this.nodePos(), margin, indentText)), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:928:1.
    private Node parseSatisfiesTag(int start, Node tagName, int margin, ByteStringView indentText)
    {
        var typeExpression = this.parseJSDocTypeExpression(false);
        var comments = this.parseTrailingTagComments(start, this.nodePos(), margin, indentText);
        return this.finishNode(this.factory.NewJSDocSatisfiesTag(tagName, typeExpression, comments), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:934:1.
    private Node parseThrowsTag(int start, Node tagName, int margin, ByteStringView indentText)
    {
        var typeExpression = this.tryParseTypeExpression();
        var comment = this.parseTrailingTagComments(start, this.nodePos(), margin, indentText);
        return this.finishNode(this.factory.NewJSDocThrowsTag(tagName, typeExpression, comment), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:940:1.
    private Node parseImportTag(int start, Node tagName, int margin, ByteStringView indentText)
    {
        var afterImportTagPos = this.scanner.TokenFullStart();
        Node identifier = default;
        if (this.isIdentifier())
        {
            identifier = this.parseIdentifier();
        }

        var importClause = this.tryParseImportClause(identifier, afterImportTagPos, Kind.TypeKeyword, true);
        var moduleSpecifier = this.parseModuleSpecifier();
        var attributes = this.tryParseImportAttributes();
        var comments = this.parseTrailingTagComments(start, this.nodePos(), margin, indentText);
        return this.finishNode(this.factory.NewJSDocImportTag(tagName, importClause, moduleSpecifier, attributes, comments), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:956:1.
    private Node parseExpressionWithTypeArgumentsForAugments()
    {
        var usedBrace = this.parseOptional(Kind.OpenBraceToken);
        var pos = this.nodePos();
        var expression = this.parsePropertyAccessEntityNameExpression();
        this.scanner.SetSkipJSDocLeadingAsterisks(true);
        var typeArguments = this.parseTypeArguments();
        this.scanner.SetSkipJSDocLeadingAsterisks(false);
        var node = this.finishNode(this.factory.NewExpressionWithTypeArguments(expression, typeArguments), pos);
        if (usedBrace)
        {
            this.skipWhitespace();
            this.parseExpected(Kind.CloseBraceToken);
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:971:1.
    private Node parsePropertyAccessEntityNameExpression()
    {
        var pos = this.nodePos();
        var node = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
        while (this.parseOptional(Kind.DotToken))
        {
            var name = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
            node = this.finishNode(this.factory.NewPropertyAccessExpression(node, null, name, NodeFlags.None), pos);
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:981:1.
    private Node parseSimpleTag(int start, Func<Node, NodeList, Node> createTag, Node tagName2, int margin, ByteStringView indentText)
    {
        return this.finishNode(createTag(tagName2, this.parseTrailingTagComments(start, this.nodePos(), margin, indentText)), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:985:1.
    private Node parseThisTag(int start, Node tagName, int margin, ByteStringView indentText)
    {
        var typeExpression = this.parseJSDocTypeExpression(true);
        this.skipWhitespace();
        var result = this.factory.NewJSDocThisTag(tagName, typeExpression, this.parseTrailingTagComments(start, this.nodePos(), margin, indentText));
        return this.finishNode(result, start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:992:1.
    private Node parseJSDocTypeNameWithNamespace(bool nested)
    {
        var start = this.scanner.TokenStart();
        if (!tokenIsIdentifierOrKeyword(this.token))
        {
            return null;
        }

        var typeNameOrNamespaceName = this.parseJSDocIdentifierName(null);
        if (this.parseOptionalJsdoc(Kind.DotToken))
        {
            var body = this.parseJSDocTypeNameWithNamespace(true);
            var jsDocNamespaceNode = this.factory.NewModuleDeclaration(null, Kind.NamespaceKeyword, typeNameOrNamespaceName, null, body);
            if (nested)
            {
                jsDocNamespaceNode.Flags |= NodeFlags.NestedNamespace;
            }

            return this.finishNode(jsDocNamespaceNode, start);
        }

        if (nested)
        {
            typeNameOrNamespaceName.Flags |= NodeFlags.IdentifierIsInJSDocNamespace;
        }

        return typeNameOrNamespaceName;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1018:1.
    private Node parseTypedefTag(int start, Node tagName, int indent, ByteStringView indentText)
    {
        var typeExpression = this.tryParseTypeExpression();
        this.skipWhitespaceOrAsterisk();
        var fullName = this.parseJSDocTypeNameWithNamespace(false);
        if ((fullName == null))
        {
            fullName = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
        }

        this.skipWhitespace();
        var comment = this.parseTagComments(indent, null);
        var end = -1;
        var hasChildren = false;
        if (((typeExpression == null) || isObjectOrObjectArrayTypeReference(typeExpression.Type())))
        {
            Node child = default;
            JSDocTypeTag childTypeTag = default;
            Slice<Node> jsdocPropertyTags = default;
            while (true)
            {
                var state = this.mark();
                child = this.parseChildPropertyTag(indent);
                if ((child == null))
                {
                    this.rewind(state);
                    break;
                }

                hasChildren = true;
                switch (child.Kind)
                {
                    case Kind.JSDocTemplateTag:
                    {
                        this.parseErrorAtRange(child.TagName().Loc, Diagnostics.A_JSDoc_template_tag_may_not_follow_a_typedef_callback_or_overload_tag);
                        break;
                    }

                    case Kind.JSDocTypeTag:
                    {
                        if ((childTypeTag == null))
                        {
                            childTypeTag = child.AsJSDocTypeTag();
                        }
                        else
                        {
                            var lastError = this.parseErrorAtCurrentToken(Diagnostics.A_JSDoc_typedef_comment_may_not_contain_multiple_type_tags);
                            if ((lastError != null))
                            {
                                var related = Ast.NewDiagnostic(null, Core.NewTextRange(0, 0), Diagnostics.The_tag_was_first_specified_here);
                                lastError.AddRelatedInfo(related);
                            }
                        }

                        break;
                    }

                    default:
                    {
                        jsdocPropertyTags = GoRuntime.Append(jsdocPropertyTags, child);
                        break;
                    }
                }
            }

            if (hasChildren)
            {
                var isArrayType = ((typeExpression != null) && (typeExpression.Type().Kind == Kind.ArrayType));
                var jsdocTypeLiteral = this.factory.NewJSDocTypeLiteral(jsdocPropertyTags, isArrayType);
                if ((((childTypeTag != null) && (childTypeTag.TypeExpression != null)) && !isObjectOrObjectArrayTypeReference(childTypeTag.TypeExpression.Type())))
                {
                    typeExpression = childTypeTag.TypeExpression;
                }
                else
                {
                    var pos = start;
                    if ((GoRuntime.Len(jsdocPropertyTags) > 0))
                    {
                        pos = jsdocPropertyTags[0].Pos();
                    }

                    typeExpression = this.finishNode(jsdocTypeLiteral, pos);
                }

                end = typeExpression.End();
            }
        }

        if ((end == -1))
        {
            if ((hasChildren && (typeExpression != null)))
            {
                end = typeExpression.End();
            }
            else
            {
                if ((comment != null))
                {
                    end = this.nodePos();
                }
                else
                {
                    if ((fullName != null))
                    {
                        end = fullName.End();
                    }
                    else
                    {
                        if ((typeExpression != null))
                        {
                            end = typeExpression.End();
                        }
                        else
                        {
                            end = tagName.End();
                        }
                    }
                }
            }
        }

        if ((comment == null))
        {
            comment = this.parseTrailingTagComments(start, end, indent, indentText);
        }

        var typedefTag = this.finishNodeWithEnd(this.factory.NewJSDocTypedefTag(tagName, typeExpression, fullName, comment), start, end);
        if ((typeExpression != null))
        {
            typeExpression.Parent = typedefTag;
        }

        return typedefTag;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1102:1.
    private NodeList parseCallbackTagParameters(int indent)
    {
        Node child = default;
        Slice<Node> parameters = default;
        var pos = this.nodePos();
        while (true)
        {
            var state = this.mark();
            child = this.parseChildParameterOrPropertyTag(propertyLikeParse.CallbackParameter, indent, null);
            if ((child == null))
            {
                this.rewind(state);
                break;
            }

            if ((child.Kind == Kind.JSDocTemplateTag))
            {
                this.parseErrorAtRange(child.TagName().Loc, Diagnostics.A_JSDoc_template_tag_may_not_follow_a_typedef_callback_or_overload_tag);
            }
            else
            {
                parameters = GoRuntime.Append(parameters, child);
            }
        }

        return this.newNodeList(Core.NewTextRange(pos, this.nodePos()), parameters);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1122:1.
    private Node parseJSDocSignature(int start, int indent)
    {
        var parameters = this.parseCallbackTagParameters(indent);
        Node returnTag = default;
        var state = this.mark();
        if (this.parseOptionalJsdoc(Kind.AtToken))
        {
            var tag = this.parseTag(default, indent);
            if ((tag.Kind == Kind.JSDocReturnTag))
            {
                returnTag = tag;
            }
        }

        if ((returnTag == null))
        {
            this.rewind(state);
        }

        return this.finishNode(this.factory.NewJSDocSignature(null, parameters, returnTag), start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1138:1.
    private Node parseCallbackTag(int start, Node tagName, int indent, ByteStringView indentText)
    {
        var fullName = this.parseJSDocTypeNameWithNamespace(false);
        if ((fullName == null))
        {
            fullName = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
        }

        this.skipWhitespace();
        var comment = this.parseTagComments(indent, null);
        var typeExpression = this.parseJSDocSignature(this.nodePos(), indent);
        if ((comment == null))
        {
            comment = this.parseTrailingTagComments(start, this.nodePos(), indent, indentText);
        }

        int end = default;
        if ((comment != null))
        {
            end = this.nodePos();
        }
        else
        {
            end = typeExpression.End();
        }

        return this.finishNodeWithEnd(this.factory.NewJSDocCallbackTag(tagName, typeExpression, fullName, comment), start, end);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1158:1.
    private Node parseOverloadTag(int start, Node tagName, int indent, ByteStringView indentText)
    {
        this.skipWhitespace();
        var comment = this.parseTagComments(indent, null);
        var typeExpression = this.parseJSDocSignature(start, indent);
        if ((comment == null))
        {
            comment = this.parseTrailingTagComments(start, this.nodePos(), indent, indentText);
        }

        int end = default;
        if ((comment != null))
        {
            end = this.nodePos();
        }
        else
        {
            end = typeExpression.End();
        }

        return this.finishNodeWithEnd(this.factory.NewJSDocOverloadTag(tagName, typeExpression, comment), start, end);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1174:1.
    private static bool textsEqual(Node a, Node b)
    {
        while ((!Ast.IsIdentifier(a) || !Ast.IsIdentifier(b)))
        {
            if (((!Ast.IsIdentifier(a) && !Ast.IsIdentifier(b)) && (a.AsQualifiedName().Right.Text() == b.AsQualifiedName().Right.Text())))
            {
                a = a.AsQualifiedName().Left;
                b = b.AsQualifiedName().Left;
            }
            else
            {
                return false;
            }
        }

        return (a.Text() == b.Text());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1186:1.
    private Node parseChildPropertyTag(int indent)
    {
        return this.parseChildParameterOrPropertyTag(propertyLikeParse.Property, indent, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1190:1.
    private Node parseChildParameterOrPropertyTag(propertyLikeParse target, int indent, Node name)
    {
        var canParseTag = true;
        var seenAsterisk = false;
        while (true)
        {
            switch (this.nextTokenJSDoc())
            {
                case Kind.AtToken:
                {
                    if ((canParseTag && this.scanner.CanFollowJSDocAt()))
                    {
                        var child = this.tryParseChildTag(target, indent);
                        if (((((child != null) && (name != null)) && (((child.Kind == Kind.JSDocParameterTag) || (child.Kind == Kind.JSDocPropertyTag)))) && ((Ast.IsIdentifier(child.Name()) || !textsEqual(name, child.Name().AsQualifiedName().Left)))))
                        {
                            return null;
                        }

                        return child;
                    }

                    seenAsterisk = false;
                    break;
                }

                case Kind.NewLineTrivia:
                {
                    canParseTag = true;
                    seenAsterisk = false;
                    break;
                }

                case Kind.AsteriskToken:
                {
                    if (seenAsterisk)
                    {
                        canParseTag = false;
                    }

                    seenAsterisk = true;
                    break;
                }

                case Kind.Identifier:
                {
                    canParseTag = false;
                    break;
                }

                case Kind.EndOfFile:
                {
                    return null;
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1222:1.
    private Node tryParseChildTag(propertyLikeParse target, int indent)
    {
        if ((this.token != Kind.AtToken))
        {
            throw new InvalidOperationException("should only be called when at @");
        }

        var start = this.scanner.TokenFullStart();
        this.nextTokenJSDoc();
        var tagName = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
        var indentText = this.skipWhitespaceOrAsterisk();
        propertyLikeParse t = default;
        switch (tagName.Text())
        {
            case "type":
            {
                if ((target == propertyLikeParse.Property))
                {
                    return this.parseTypeTag(default, start, tagName, -1, "");
                }

                break;
            }

            case "prop":
            case "property":
            {
                t = propertyLikeParse.Property;
                break;
            }

            case "arg":
            case "argument":
            case "param":
            {
                t = (propertyLikeParse.Parameter | propertyLikeParse.CallbackParameter);
                break;
            }

            case "template":
            {
                return this.parseTemplateTag(start, tagName, indent, indentText);
            }

            case "this":
            {
                return this.parseThisTag(start, tagName, indent, indentText);
            }

            default:
            {
                return null;
            }
        }

        if ((((target & t)) == 0))
        {
            return null;
        }

        return this.parseParameterOrPropertyTag(start, tagName, target, indent);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1254:1.
    private Node parseTemplateTagTypeParameter()
    {
        var typeParameterPos = this.nodePos();
        var isBracketed = this.parseOptionalJsdoc(Kind.OpenBracketToken);
        if (isBracketed)
        {
            this.skipWhitespace();
        }

        var modifiers = this.parseModifiersEx(false, true, false);
        var name = this.parseJSDocIdentifierName(Diagnostics.Unexpected_token_A_type_parameter_name_was_expected_without_curly_braces);
        Node defaultType = default;
        if (isBracketed)
        {
            this.skipWhitespace();
            this.parseExpected(Kind.EqualsToken);
            var saveContextFlags = this.contextFlags;
            this.setContextFlags(NodeFlags.JSDoc, true);
            defaultType = this.parseJSDocType();
            this.contextFlags = saveContextFlags;
            this.parseExpected(Kind.CloseBracketToken);
        }

        if (Ast.NodeIsMissing(name))
        {
            return null;
        }

        return this.finishNode(this.factory.NewTypeParameterDeclaration(modifiers, name, null, null, defaultType), typeParameterPos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1280:1.
    private NodeList parseTemplateTagTypeParameters()
    {
        var typeParameters = new NodeList
        {
        };
        {
            var ok = true;
            for (; ok; ok = this.parseOptionalJsdoc(Kind.CommaToken))
            {
                this.skipWhitespace();
                var node = this.parseTemplateTagTypeParameter();
                if ((node != null))
                {
                    typeParameters.Nodes = GoRuntime.Append(typeParameters.Nodes, node);
                }

                this.skipWhitespaceOrAsterisk();
            }
        }

        return typeParameters;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1293:1.
    private Node parseTemplateTag(int start, Node tagName, int indent, ByteStringView indentText)
    {
        Node constraint = default;
        if ((this.token == Kind.OpenBraceToken))
        {
            constraint = this.parseJSDocTypeExpression(false);
        }

        var typeParameters = this.parseTemplateTagTypeParameters();
        var result = this.factory.NewJSDocTemplateTag(tagName, constraint, typeParameters, this.parseTrailingTagComments(start, this.nodePos(), indent, indentText));
        return this.finishNode(result, start);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1314:1.
    private bool parseOptionalJsdoc(Kind t)
    {
        if ((this.token == t))
        {
            this.nextTokenJSDoc();
            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1322:1.
    private Node parseJSDocEntityName(Message diagnosticMessage)
    {
        Node entity = this.parseJSDocIdentifierName(diagnosticMessage);
        if (this.parseOptional(Kind.OpenBracketToken))
        {
            this.parseExpected(Kind.CloseBracketToken);
        }

        while (this.parseOptional(Kind.DotToken))
        {
            var name = this.parseJSDocIdentifierName(Diagnostics.Identifier_expected);
            if (this.parseOptional(Kind.OpenBracketToken))
            {
                this.parseExpected(Kind.CloseBracketToken);
            }

            var pos = entity.Pos();
            entity = this.finishNode(this.factory.NewQualifiedName(entity, name), pos);
        }

        return entity;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/jsdoc.go:1341:1.
    private Node parseJSDocIdentifierName(Message diagnosticMessage)
    {
        if (!tokenIsIdentifierOrKeyword(this.token))
        {
            if ((diagnosticMessage != null))
            {
                this.parseErrorAtCurrentToken(diagnosticMessage);
            }
            else
            {
                if (isReservedWord(this.token))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Identifier_expected_0_is_a_reserved_word_that_cannot_be_used_here, this.scanner.TokenTextView().Materialize());
                }
            }

            return this.finishNode(this.newIdentifier(""), this.nodePos());
        }

        var pos = this.scanner.TokenStart();
        var end = this.scanner.TokenEnd();
        var text = this.scanner.TokenValueTextValue();
        this.nextTokenJSDoc();
        return this.finishNodeWithEnd(this.newIdentifier(text), pos, end);
    }
}

// Declarations from jsdoc.go; its init registration maps to a static class constructor.
internal enum jsdocState
{
    BeginningOfLine,
    SawAsterisk,
    SavingComments,
    SavingBackticks
}

[Flags]
internal enum propertyLikeParse
{
    Property = 1,
    Parameter = 2,
    CallbackParameter = 4
}

public partial class Parser
{
    static Parser() => Ast.SetParseJSDocForNode(parseJSDocForNode);
}
