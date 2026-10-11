// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/parser/parser.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
public partial class Parser
{
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:103:1.
    private static Parser newParser()
    {
        var res = new Parser
        {
        };
        res.initializeClosures();
        return res;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:134:1.
    internal static SourceFile ParseSourceFileOwned(SourceFileParseOptions opts, ByteStringView sourceText, ScriptKind scriptKind)
    {
        var p = getParser();
        // Go parser-pool return omitted: this port creates an isolated managed parser.
        p.initializeState(opts, sourceText, scriptKind);
        p.nextToken();
        if ((p.scriptKind == ScriptKind.JSON))
        {
            return p.parseJSONText();
        }

        return p.parseSourceFileWorker();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:145:1.
    private void initializeClosures()
    {
        this.setParentFromContext = (n) =>
        {
            n.Parent = this.currentParent;
            return false;
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:152:1.
    private bool isJavaScript()
    {
        return ((this.scriptKind == ScriptKind.JS) || (this.scriptKind == ScriptKind.JSX));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:156:1.
    private SourceFile parseJSONText()
    {
        var pos = this.nodePos();
        NodeList statements = default;
        Node eof = default;
        if ((this.token == Kind.EndOfFile))
        {
            statements = this.newNodeList(Core.NewTextRange(pos, this.nodePos()), default);
            eof = this.parseTokenNode();
        }
        else
        {
            object expressions = default;
            while ((this.token != Kind.EndOfFile))
            {
                Node expression = default;
                switch (this.token)
                {
                    case Kind.OpenBracketToken:
                    {
                        expression = this.parseArrayLiteralExpression();
                        break;
                    }

                    case Kind.TrueKeyword:
                    case Kind.FalseKeyword:
                    case Kind.NullKeyword:
                    {
                        expression = this.parseTokenNode();
                        break;
                    }

                    case Kind.MinusToken:
                    {
                        if (this.lookAhead((p) =>
                        {
                            return ((p.nextToken() == Kind.NumericLiteral) && (p.nextToken() != Kind.ColonToken));
                        }))
                        {
                            expression = this.parsePrefixUnaryExpression();
                        }
                        else
                        {
                            expression = this.parseObjectLiteralExpression();
                        }

                        break;
                    }

                    case Kind.NumericLiteral:
                    case Kind.StringLiteral:
                    {
                        if (this.lookAhead((p2) =>
                        {
                            return (p2.nextToken() != Kind.ColonToken);
                        }))
                        {
                            expression = this.parseLiteralExpression();
                            break;
                        }

                        goto default;
                    }

                    default:
                    {
                        expression = this.parseObjectLiteralExpression();
                        break;
                    }
                }

                if ((expressions != null))
                {
                    {
                        var ok = expressions is Slice<Node>;
                        var es = ok ? (Slice<Node>)expressions : default;
                        if (ok)
                        {
                            expressions = GoRuntime.Append(es, expression);
                        }
                        else
                        {
                            expressions = Slice<Node>.Make(0, 2).Append(((Node)expressions)).Append(expression);
                        }
                    }
                }
                else
                {
                    expressions = expression;
                    if ((this.token != Kind.EndOfFile))
                    {
                        this.parseErrorAtCurrentToken(Diagnostics.Unexpected_token);
                    }
                }
            }

            Node expression2 = default;
            {
                var ok2 = expressions is Slice<Node>;
                var es2 = ok2 ? (Slice<Node>)expressions : default;
                if (ok2)
                {
                    expression2 = this.finishNode(this.factory.NewArrayLiteralExpression(this.newNodeList(Core.NewTextRange(pos, this.nodePos()), es2), false), pos);
                }
                else
                {
                    expression2 = ((Node)expressions);
                }
            }

            var statement = this.finishNode(this.factory.NewExpressionStatement(expression2), pos);
            statements = this.newNodeList(Core.NewTextRange(pos, this.nodePos()), this.nodeSliceArena.NewSlice1Header(statement));
            eof = this.parseExpectedToken(Kind.EndOfFile);
        }

        var node = this.finishNode(this.factory.NewSourceFileWithTextView(this.opts, this.sourceText, statements, eof), pos);
        var result = node.AsSourceFile();
        if ((GoRuntime.Len(result.Statements.Nodes) > 0))
        {
            this.validateJsonValue(result, result.Statements.Nodes[0].Expression());
        }

        this.finishSourceFile(result, false);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:226:1.
    private static TextRange getErrorSpanForNode(ByteStringView sourceText, Node node)
    {
        var pos = node.Pos();
        if (!Ast.NodeIsMissing(node))
        {
            pos = Scanner.SkipTriviaByteString(sourceText, pos);
        }

        return Core.NewTextRange(pos, node.End());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:234:1.
    private void validateJsonValue(SourceFile sourceFile, Node valueExpression)
    {
        if ((valueExpression == null))
        {
            return;
        }

        switch (valueExpression.Kind)
        {
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
            case Kind.NumericLiteral:
            {
                return;
            }

            case Kind.StringLiteral:
            {
                if (!isDoubleQuotedString(valueExpression))
                {
                    this.diagnostics = GoRuntime.Append(this.diagnostics, Ast.NewDiagnostic(sourceFile, getErrorSpanForNode(this.sourceText, valueExpression), Diagnostics.String_literal_with_double_quotes_expected));
                }

                return;
            }

            case Kind.PrefixUnaryExpression:
            {
                if (((valueExpression.AsPrefixUnaryExpression().Operator != Kind.MinusToken) || (valueExpression.AsPrefixUnaryExpression().Operand.Kind != Kind.NumericLiteral)))
                {
                    break;
                }

                return;
            }

            case Kind.ObjectLiteralExpression:
            {
                this.validateJsonObjectLiteral(sourceFile, valueExpression.AsObjectLiteralExpression());
                return;
            }

            case Kind.ArrayLiteralExpression:
            {
                {
                    foreach (var element in GoRuntime.RangeValues(valueExpression.Elements()))
                    {
                        this.validateJsonValue(sourceFile, element);
                    }
                }

                return;
            }
        }

        this.diagnostics = GoRuntime.Append(this.diagnostics, Ast.NewDiagnostic(sourceFile, getErrorSpanForNode(this.sourceText, valueExpression), Diagnostics.Property_value_can_only_be_string_literal_numeric_literal_true_false_null_object_literal_or_array_literal));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:263:1.
    private static bool isDoubleQuotedString(Node node)
    {
        return (Ast.IsStringLiteral(node) && ((node.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) == 0));
    }

    // validateJsonObjectLiteral validates properties of a JSON object literal.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:268:1.
    private void validateJsonObjectLiteral(SourceFile sourceFile, ObjectLiteralExpression node)
    {
        {
            foreach (var element in GoRuntime.RangeValues(node.Properties.Nodes))
            {
                if ((element.Kind != Kind.PropertyAssignment))
                {
                    this.diagnostics = GoRuntime.Append(this.diagnostics, Ast.NewDiagnostic(sourceFile, getErrorSpanForNode(this.sourceText, element), Diagnostics.Property_assignment_expected));
                    continue;
                }

                if (((element.Name() != null) && !isDoubleQuotedString(element.Name())))
                {
                    this.diagnostics = GoRuntime.Append(this.diagnostics, Ast.NewDiagnostic(sourceFile, getErrorSpanForNode(this.sourceText, element.Name()), Diagnostics.String_literal_with_double_quotes_expected));
                }

                this.validateJsonValue(sourceFile, element.AsPropertyAssignment().Initializer);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:281:1.
    private static Node ParseIsolatedEntityNameRaw(ByteStringView text)
    {
        var p = getParser();
        // Go parser-pool return omitted: this port creates an isolated managed parser.
        p.initializeState(new SourceFileParseOptions { }, text, ScriptKind.JS);
        p.nextToken();
        var entityName = p.parseEntityName(true, false, null);
        return Core.IfElse(((p.token == Kind.EndOfFile) && (GoRuntime.Len(p.diagnostics) == 0)), entityName, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:290:1.
    private void initializeState(SourceFileParseOptions opts, ByteStringView sourceText, ScriptKind scriptKind)
    {
        if ((scriptKind == ScriptKind.Unknown))
        {
            throw new InvalidOperationException(("ScriptKind must be specified when parsing source file: " + opts.FileName.AsString()));
        }

        if ((this.scanner == null))
        {
            this.scanner = Scanner.NewScanner();
        }
        else
        {
            this.scanner.Reset();
        }

        this.opts = opts;
        this.sourceText = sourceText;
        this.scriptKind = scriptKind;
        this.languageVariant = getLanguageVariant(this.scriptKind);
        switch (this.scriptKind)
        {
            case ScriptKind.JS:
            case ScriptKind.JSX:
            {
                this.contextFlags = NodeFlags.JavaScriptFile;
                break;
            }

            case ScriptKind.JSON:
            {
                this.contextFlags = (NodeFlags.JavaScriptFile | NodeFlags.JsonFile);
                break;
            }

            default:
            {
                this.contextFlags = NodeFlags.None;
                break;
            }
        }

        this.scanner.SetByteTextInternal(this.sourceText);
        this.scanner.SetOnErrorByteStrings(this.scanError);
        this.scanner.SetLanguageVariant(this.languageVariant);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:317:1.
    private void scanError(Message message, int pos, int length, params object[] args)
    {
        this.parseErrorAtRange(Core.NewTextRange(pos, (pos + length)), message, args);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:321:1.
    private Diagnostic parseErrorAt(int pos, int end, Message message, params object[] args)
    {
        return this.parseErrorAtRange(Core.NewTextRange(pos, end), message, args);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:325:1.
    private Diagnostic parseErrorAtCurrentToken(Message message, params object[] args)
    {
        return this.parseErrorAtRange(this.scanner.TokenRange(), message, args);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:329:1.
    private Diagnostic parseErrorAtRange(TextRange loc, Message message, params object[] args)
    {
        Diagnostic result = default;
        if (((GoRuntime.Len(this.diagnostics) == 0) || (this.diagnostics[(GoRuntime.Len(this.diagnostics) - 1)].Pos() != loc.Pos())))
        {
            result = Ast.NewDiagnostic(null, loc, message, args);
            this.diagnostics = GoRuntime.Append(this.diagnostics, result);
        }

        this.hasParseError = true;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:351:1.
    private ParserState mark()
    {
        return new ParserState
        {
            scannerState = this.scanner.Mark(),
            contextFlags = this.contextFlags,
            diagnosticsLen = GoRuntime.Len(this.diagnostics),
            jsDiagnosticsLen = GoRuntime.Len(this.jsDiagnostics),
            jsdocInfosLen = GoRuntime.Len(this.jsdocInfos),
            reparsedClonesLen = GoRuntime.Len(this.reparsedClones),
            statementHasAwaitIdentifier = this.statementHasAwaitIdentifier,
            hasParseError = this.hasParseError
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:364:1.
    private void rewind(ParserState state)
    {
        this.scanner.Rewind(state.scannerState);
        this.token = this.scanner.Token();
        this.contextFlags = state.contextFlags;
        this.diagnostics = GoRuntime.Slice(this.diagnostics, 0, state.diagnosticsLen);
        this.jsDiagnostics = GoRuntime.Slice(this.jsDiagnostics, 0, state.jsDiagnosticsLen);
        // parser.go:370: rewind the private slice header, retaining its backing storage.
        this.jsdocInfos = this.jsdocInfos.Slice2(0, state.jsdocInfosLen);
        this.reparsedClones = GoRuntime.Slice(this.reparsedClones, 0, state.reparsedClonesLen);
        this.statementHasAwaitIdentifier = state.statementHasAwaitIdentifier;
        this.hasParseError = state.hasParseError;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:376:1.
    private bool lookAhead(Func<Parser, bool> callback)
    {
        var state = this.mark();
        var result = callback(this);
        this.rewind(state);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:383:1.
    private Kind nextToken()
    {
        if ((Ast.IsKeyword(this.token) && ((this.scanner.HasUnicodeEscape() || this.scanner.HasExtendedUnicodeEscape()))))
        {
            this.parseErrorAtCurrentToken(Diagnostics.Keywords_cannot_contain_escape_characters);
        }

        this.token = this.scanner.Scan();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:393:1.
    private Kind nextTokenWithoutCheck()
    {
        this.token = this.scanner.Scan();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:398:1.
    private Kind nextTokenJSDoc()
    {
        this.token = this.scanner.ScanJSDocToken();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:403:1.
    private Kind nextJSDocCommentTextToken(bool inBackticks)
    {
        this.token = this.scanner.ScanJSDocCommentTextToken(inBackticks);
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:408:1.
    private int nodePos()
    {
        return this.scanner.TokenFullStart();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:412:1.
    private bool hasPrecedingLineBreak()
    {
        return this.scanner.HasPrecedingLineBreak();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:416:1.
    private jsdocScannerInfo jsdocScannerInfo()
    {
        if (!this.scanner.HasPrecedingJSDocComment())
        {
            return 0;
        }

        var info = global::TypeScript.Scanner.jsdocScannerInfo.HasJSDoc;
        if (this.scanner.HasPrecedingJSDocWithDeprecatedTag())
        {
            info |= global::TypeScript.Scanner.jsdocScannerInfo.HasDeprecated;
        }

        if (this.scanner.HasPrecedingJSDocWithSeeOrLink())
        {
            info |= global::TypeScript.Scanner.jsdocScannerInfo.HasSeeOrLink;
        }

        return info;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:430:1.
    private SourceFile parseSourceFileWorker()
    {
        var isDeclarationFile = this.opts.FileName.IsDeclarationFile();
        if (isDeclarationFile)
        {
            this.contextFlags |= NodeFlags.Ambient;
        }

        var pos = this.nodePos();
        var statements = this.parseListIndex(ParsingContext.SourceElements, 0, static (parserCallback, argi, _) => parserCallback.parseToplevelStatement(argi));
        var end = this.nodePos();
        var endJSDoc = this.jsdocScannerInfo();
        var eof = this.parseTokenNode();
        this.withJSDoc(eof, endJSDoc);
        if ((eof.Kind != Kind.EndOfFile))
        {
            throw new InvalidOperationException("Expected end of file token from scanner.");
        }

        if ((GoRuntime.Len(this.reparseList) != 0))
        {
            statements = GoRuntime.Append(statements, this.reparseList);
            this.reparseList = default;
        }

        var node = this.finishNode(this.factory.NewSourceFileWithTextView(this.opts, this.sourceText, this.newNodeList(Core.NewTextRange(pos, end), statements), eof), pos);
        var result = node.AsSourceFile();
        this.finishSourceFile(result, isDeclarationFile);
        if (((!result.IsDeclarationFile && (result.ExternalModuleIndicator != null)) && (GoRuntime.Len(this.possibleAwaitSpans) > 0)))
        {
            var reparse = this.finishNode(this.reparseTopLevelAwait(result), pos);
            if ((node != reparse))
            {
                result = reparse.AsSourceFile();
                this.finishSourceFile(result, isDeclarationFile);
            }
        }

        collectExternalModuleReferences(result);
        if (Ast.IsInJSFile(node))
        {
            result.SetJSDiagnostics(attachFileToDiagnostics(this.jsDiagnostics, result));
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:465:1.
    private void finishSourceFile(SourceFile result, bool isDeclarationFile)
    {
        result.CommentDirectives = this.scanner.CommentDirectivesHeader();
        result.Pragmas = getCommentPragmas(this.factory, this.sourceText);
        this.processPragmasIntoFields(result);
        result.SetDiagnostics(attachFileToDiagnostics(this.diagnostics, result));
        result.SetJSDocDiagnostics(attachFileToDiagnostics(this.jsdocDiagnostics, result));
        result.IsDeclarationFile = isDeclarationFile;
        result.LanguageVariant = this.languageVariant;
        result.ScriptKind = this.scriptKind;
        result.Flags |= this.sourceFlags;
        result.NodeCount = this.factory.NodeCount();
        result.TextCount = this.factory.TextCount();
        result.IdentifierCount = this.identifierCount;
        result.SetJSDocCache(this.createJSDocCache());
        if (!this.isJavaScript())
        {
            result.SetHasLazyJSDoc(true);
        }

        GoSlices.SortFunc(this.reparsedClones, Ast.CompareNodePositions);
        result.ReparsedClones = GoSlices.Clone(this.reparsedClones);
        Ast.SetExternalModuleIndicator(result, this.opts.ExternalModuleIndicatorOptions);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:488:1.
    private Dictionary<Node, Slice<Node>> createJSDocCache()
    {
        if ((GoRuntime.Len(this.jsdocInfos) == 0))
        {
            return null;
        }

        var result = new Dictionary<Node, Slice<Node>>();
        {
            foreach (var info in GoRuntime.RangeValues(this.jsdocInfos))
            {
                result[info.parent] = info.jsDocs;
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:499:1.
    private Node parseToplevelStatement(int i)
    {
        this.statementHasAwaitIdentifier = false;
        var statement = this.parseStatement();
        i += GoRuntime.Len(this.reparseList);
        if ((this.statementHasAwaitIdentifier && ((statement.Flags & NodeFlags.AwaitContext) == 0)))
        {
            if (((GoRuntime.Len(this.possibleAwaitSpans) == 0) || (this.possibleAwaitSpans[(GoRuntime.Len(this.possibleAwaitSpans) - 1)] != i)))
            {
                this.possibleAwaitSpans = this.possibleAwaitSpans.Append((ReadOnlySpan<int>)[i, (i + 1)]);
            }
            else
            {
                this.possibleAwaitSpans[(GoRuntime.Len(this.possibleAwaitSpans) - 1)] = (i + 1);
            }
        }

        return statement;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:516:1.
    private Node reparseTopLevelAwait(SourceFile sourceFile)
    {
        if (((GoRuntime.Len(this.possibleAwaitSpans) % 2) == 1))
        {
            throw new InvalidOperationException("possibleAwaitSpans malformed: odd number of indices, not paired into spans.");
        }

        var statements = Slice<Node>.Make(0);
        var savedParseDiagnostics = this.diagnostics;
        this.diagnostics = Slice<Diagnostic>.Make(0);
        var afterAwaitStatement = 0;
        {
            var i = 0;
            for (; (i < GoRuntime.Len(this.possibleAwaitSpans)); i += 2)
            {
                var nextAwaitStatement = this.possibleAwaitSpans[i];
                var prevStatement = sourceFile.Statements.Nodes[afterAwaitStatement];
                var nextStatement = sourceFile.Statements.Nodes[nextAwaitStatement];
                statements = GoRuntime.Append(statements, GoRuntime.Slice(sourceFile.Statements.Nodes, afterAwaitStatement, nextAwaitStatement));
                var diagnosticStart = Core.FindIndex(savedParseDiagnostics, prevStatement, static (diagnostic, statement) =>
                {
                    return (diagnostic.Pos() >= statement.Pos());
                });
                int diagnosticEnd = default;
                if ((diagnosticStart >= 0))
                {
                    diagnosticEnd = Core.FindIndex(GoRuntime.Slice(savedParseDiagnostics, diagnosticStart), nextStatement, static (diagnostic2, statement) =>
                    {
                        return (diagnostic2.Pos() >= statement.Pos());
                    });
                }
                else
                {
                    diagnosticEnd = -1;
                }

                if ((diagnosticStart >= 0))
                {
                    Slice<Diagnostic> slice = default;
                    if ((diagnosticEnd >= 0))
                    {
                        slice = GoRuntime.Slice(savedParseDiagnostics, diagnosticStart, (diagnosticStart + diagnosticEnd));
                    }
                    else
                    {
                        slice = GoRuntime.Slice(savedParseDiagnostics, diagnosticStart);
                    }

                    this.diagnostics = GoRuntime.Append(this.diagnostics, slice);
                }

                var state = this.mark();
                this.contextFlags |= NodeFlags.AwaitContext;
                this.scanner.ResetPos(nextStatement.Pos());
                this.nextToken();
                afterAwaitStatement = this.possibleAwaitSpans[(i + 1)];
                while ((this.token != Kind.EndOfFile))
                {
                    var startPos = this.scanner.TokenFullStart();
                    var statement = this.parseStatement();
                    statements = GoRuntime.Append(statements, statement);
                    if ((startPos == this.scanner.TokenFullStart()))
                    {
                        this.nextToken();
                    }

                    if ((afterAwaitStatement < GoRuntime.Len(sourceFile.Statements.Nodes)))
                    {
                        var lastAwaitStatement = sourceFile.Statements.Nodes[(afterAwaitStatement - 1)];
                        if ((statement.End() == lastAwaitStatement.End()))
                        {
                            break;
                        }

                        if ((statement.End() > lastAwaitStatement.End()))
                        {
                            i += 2;
                            if ((i < GoRuntime.Len(this.possibleAwaitSpans)))
                            {
                                afterAwaitStatement = this.possibleAwaitSpans[(i + 1)];
                            }
                            else
                            {
                                afterAwaitStatement = GoRuntime.Len(sourceFile.Statements.Nodes);
                            }
                        }
                    }
                }

                state.diagnosticsLen = GoRuntime.Len(this.diagnostics);
                this.rewind(state);
            }
        }

        if ((afterAwaitStatement < GoRuntime.Len(sourceFile.Statements.Nodes)))
        {
            var prevStatement2 = sourceFile.Statements.Nodes[afterAwaitStatement];
            statements = GoRuntime.Append(statements, GoRuntime.Slice(sourceFile.Statements.Nodes, afterAwaitStatement));
            var diagnosticStart2 = Core.FindIndex(savedParseDiagnostics, prevStatement2, static (diagnostic3, statement) =>
            {
                return (diagnostic3.Pos() >= statement.Pos());
            });
            if ((diagnosticStart2 >= 0))
            {
                this.diagnostics = GoRuntime.Append(this.diagnostics, GoRuntime.Slice(savedParseDiagnostics, diagnosticStart2));
            }
        }

        var result = this.factory.NewSourceFileWithTextView(sourceFile.ParseOptions(), this.sourceText, this.newNodeList(sourceFile.Statements.Loc, statements), sourceFile.EndOfFileToken);
        {
            foreach (var s in GoRuntime.RangeValues(statements))
            {
                s.Parent = result.AsNode();
            }
        }

        return result;
    }

    // parser.go:617/655. Inline references preserve each invocation's local cap16 scratch.
    [System.Runtime.CompilerServices.InlineArray(16)]
    private struct ParseNodeBuffer { private Node first; }

    // Shared managed append adapter for the two private parser list buffers; no header escapes.
    private static void appendListNode(ref ParseNodeBuffer inlineNodes, ref Slice<Node> spilledNodes, ref int count, Node node)
    {
        if (count < 16)
            inlineNodes[count] = node;
        else
        {
            if (spilledNodes.IsNil)
            {
                spilledNodes = Slice<Node>.Make(16, 32);
                ((ReadOnlySpan<Node>)inlineNodes).CopyTo(spilledNodes.AsSpan());
            }
            spilledNodes = spilledNodes.Append(node);
        }
        count++;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:612:1.
    // The source's synchronous callback receives its captured value explicitly,
    // so parseList forwards without a managed display class or bound delegate.
    private Slice<Node> parseListIndex<TState>(ParsingContext kind, TState state, Func<Parser, int, TState, Node> parseElement)
    {
        var saveParsingContexts = this.parsingContexts;
        this.parsingContexts |= (1 << (int)(kind));
        var outerReparseList = this.reparseList;
        this.reparseList = default;
        ParseNodeBuffer inlineNodes = default;
        Slice<Node> spilledNodes = default;
        int listLength = 0;
        {
            var i = 0;
            for (; !this.isListTerminator(kind); i++)
            {
                if (this.isListElement(kind, false))
                {
                    var elt = parseElement(this, listLength, state);
                    if ((GoRuntime.Len(this.reparseList) != 0))
                    {
                        {
                            foreach (var e in GoRuntime.RangeValues(this.reparseList))
                            {
                                if (((((Ast.IsJSTypeAliasDeclaration(e) || Ast.IsJSImportDeclaration(e))) && (kind != ParsingContext.SourceElements)) && (kind != ParsingContext.BlockStatements)))
                                {
                                    outerReparseList = GoRuntime.Append(outerReparseList, e);
                                }
                                else
                                {
                                    appendListNode(ref inlineNodes, ref spilledNodes, ref listLength, e);
                                }
                            }
                        }

                        this.reparseList = default;
                    }

                    appendListNode(ref inlineNodes, ref spilledNodes, ref listLength, elt);
                    continue;
                }

                if (this.abortParsingListOrMoveToNextToken(kind))
                {
                    break;
                }
            }
        }

        this.reparseList = outerReparseList;
        this.parsingContexts = saveParsingContexts;
        return this.nodeSliceArena.CloneHeader(spilledNodes.IsNil ? ((ReadOnlySpan<Node>)inlineNodes).Slice(0, listLength) : spilledNodes.AsSpan());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:644:1.
    private NodeList parseList(ParsingContext kind, Func<Parser, Node> parseElement)
    {
        var pos = this.nodePos();
        var nodes = this.parseListIndex(kind, parseElement, static (p2, _, callback) => callback(p2));
        return this.newNodeList(Core.NewTextRange(pos, this.nodePos()), nodes);
    }

    // Return a non-nil (but possibly empty) slice if parsing was successful, or nil if parseElement returned nil
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:651:1.
    private NodeList parseDelimitedList(ParsingContext kind, Func<Parser, Node> parseElement)
        => parseDelimitedList(kind, parseElement, static (parser, callback) => callback(parser));

    // parser.go:651. Explicit synchronous state keeps one original list algorithm.
    private NodeList parseDelimitedList<TState>(ParsingContext kind, TState state, Func<Parser, TState, Node> parseElement)
    {
        var pos = this.nodePos();
        var saveParsingContexts = this.parsingContexts;
        this.parsingContexts |= (1 << (int)(kind));
        ParseNodeBuffer inlineNodes = default;
        Slice<Node> spilledNodes = default;
        int listLength = 0;
        while (true)
        {
            if (this.isListElement(kind, false))
            {
                var startPos = this.nodePos();
                var element = parseElement(this, state);
                if ((element == null))
                {
                    this.parsingContexts = saveParsingContexts;
                    return null;
                }

                appendListNode(ref inlineNodes, ref spilledNodes, ref listLength, element);
                if (this.parseOptional(Kind.CommaToken))
                {
                    continue;
                }

                if (this.isListTerminator(kind))
                {
                    break;
                }

                if (((this.token != Kind.CommaToken) && (kind == ParsingContext.EnumMembers)))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.An_enum_member_name_must_be_followed_by_a_or);
                }
                else
                {
                    this.parseExpected(Kind.CommaToken);
                }

                if ((((((kind == ParsingContext.ObjectLiteralMembers) || (kind == ParsingContext.ImportAttributes))) && (this.token == Kind.SemicolonToken)) && !this.hasPrecedingLineBreak()))
                {
                    this.nextToken();
                }

                if ((startPos == this.nodePos()))
                {
                    this.nextToken();
                }

                continue;
            }

            if (this.isListTerminator(kind))
            {
                break;
            }

            if (this.abortParsingListOrMoveToNextToken(kind))
            {
                break;
            }
        }

        this.parsingContexts = saveParsingContexts;
        return this.newNodeList(Core.NewTextRange(pos, this.nodePos()), this.nodeSliceArena.CloneHeader(spilledNodes.IsNil ? ((ReadOnlySpan<Node>)inlineNodes).Slice(0, listLength) : spilledNodes.AsSpan()));
    }

    // Return a non-nil (but possibly empty) NodeList if parsing was successful, a missing NodeList if the opening
    // token wasn't found, or nil if parseElement returned nil.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:710:1.
    private NodeList parseBracketedList(ParsingContext kind, Func<Parser, Node> parseElement, Kind opening, Kind closing)
    {
        if (this.parseExpected(opening))
        {
            var result = this.parseDelimitedList(kind, parseElement);
            this.parseExpected(closing);
            return result;
        }

        return this.createMissingList();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:719:1.
    private NodeList parseEmptyNodeList()
    {
        return this.newNodeList(Core.NewTextRange(this.nodePos(), this.nodePos()), default);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:723:1.
    private NodeList createMissingList()
    {
        var result = this.parseEmptyNodeList();
        result.Nodes = missingListNodes;
        result.IsMissing = true;
        return result;
    }

    // Returns true if we should abort parsing.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:730:1.
    private bool abortParsingListOrMoveToNextToken(ParsingContext kind)
    {
        this.parsingContextErrors(kind);
        if (this.isInSomeParsingContext())
        {
            return true;
        }

        this.nextToken();
        return false;
    }

    // True if positioned at element or terminator of the current list or any enclosing list
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:740:1.
    private bool isInSomeParsingContext()
    {
        Debug.Assert((this.parsingContexts != 0), "Missing parsing context");
        {
            foreach (var kind in GoRuntime.RangeKeys(ParsingContext.Count))
            {
                if (((this.parsingContexts & ((1 << (int)(kind)))) != 0))
                {
                    if ((this.isListElement(kind, true) || this.isListTerminator(kind)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:754:1.
    private void parsingContextErrors(ParsingContext context)
    {
        switch (context)
        {
            case ParsingContext.SourceElements:
            {
                if ((this.token == Kind.DefaultKeyword))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, "export");
                }
                else
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Declaration_or_statement_expected);
                }

                break;
            }

            case ParsingContext.BlockStatements:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Declaration_or_statement_expected);
                break;
            }

            case ParsingContext.SwitchClauses:
            {
                this.parseErrorAtCurrentToken(Diagnostics.X_case_or_default_expected);
                break;
            }

            case ParsingContext.SwitchClauseStatements:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Statement_expected);
                break;
            }

            case ParsingContext.RestProperties:
            case ParsingContext.TypeMembers:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Property_or_signature_expected);
                break;
            }

            case ParsingContext.ClassMembers:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Unexpected_token_A_constructor_method_accessor_or_property_was_expected);
                break;
            }

            case ParsingContext.EnumMembers:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Enum_member_expected);
                break;
            }

            case ParsingContext.HeritageClauseElement:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Expression_expected);
                break;
            }

            case ParsingContext.VariableDeclarations:
            {
                if (Ast.IsKeyword(this.token))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.X_0_is_not_allowed_as_a_variable_declaration_name, Scanner.TokenToString(this.token));
                }
                else
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Variable_declaration_expected);
                }

                break;
            }

            case ParsingContext.ObjectBindingElements:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Property_destructuring_pattern_expected);
                break;
            }

            case ParsingContext.ArrayBindingElements:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Array_element_destructuring_pattern_expected);
                break;
            }

            case ParsingContext.ArgumentExpressions:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Argument_expression_expected);
                break;
            }

            case ParsingContext.ObjectLiteralMembers:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Property_assignment_expected);
                break;
            }

            case ParsingContext.ArrayLiteralMembers:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Expression_or_comma_expected);
                break;
            }

            case ParsingContext.JSDocParameters:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Parameter_declaration_expected);
                break;
            }

            case ParsingContext.Parameters:
            {
                if (Ast.IsKeyword(this.token))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.X_0_is_not_allowed_as_a_parameter_name, Scanner.TokenToString(this.token));
                }
                else
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Parameter_declaration_expected);
                }

                break;
            }

            case ParsingContext.TypeParameters:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Type_parameter_declaration_expected);
                break;
            }

            case ParsingContext.TypeArguments:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Type_argument_expected);
                break;
            }

            case ParsingContext.TupleElementTypes:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Type_expected);
                break;
            }

            case ParsingContext.HeritageClauses:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Unexpected_token_expected);
                break;
            }

            case ParsingContext.ImportOrExportSpecifiers:
            {
                if ((this.token == Kind.FromKeyword))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, "}");
                }
                else
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Identifier_expected);
                }

                break;
            }

            case ParsingContext.JsxAttributes:
            case ParsingContext.JsxChildren:
            case ParsingContext.JSDocComment:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Identifier_expected);
                break;
            }

            case ParsingContext.ImportAttributes:
            {
                this.parseErrorAtCurrentToken(Diagnostics.Identifier_or_string_literal_expected);
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in parsingContextErrors");
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:823:1.
    private bool isListElement(ParsingContext parsingContext, bool inErrorRecovery)
    {
        switch (parsingContext)
        {
            case ParsingContext.SourceElements:
            case ParsingContext.BlockStatements:
            case ParsingContext.SwitchClauseStatements:
            {
                return (!(((this.token == Kind.SemicolonToken) && inErrorRecovery)) && this.isStartOfStatement());
            }

            case ParsingContext.SwitchClauses:
            {
                return ((this.token == Kind.CaseKeyword) || (this.token == Kind.DefaultKeyword));
            }

            case ParsingContext.TypeMembers:
            {
                return this.lookAhead((parserCallback) => parserCallback.scanTypeMemberStart());
            }

            case ParsingContext.ClassMembers:
            {
                return (this.lookAhead((parserCallback) => parserCallback.scanClassMemberStart()) || ((this.token == Kind.SemicolonToken) && !inErrorRecovery));
            }

            case ParsingContext.EnumMembers:
            {
                return ((this.token == Kind.OpenBracketToken) || this.isLiteralPropertyName());
            }

            case ParsingContext.ObjectLiteralMembers:
            {
                switch (this.token)
                {
                    case Kind.OpenBracketToken:
                    case Kind.AsteriskToken:
                    case Kind.DotDotDotToken:
                    case Kind.DotToken:
                    {
                        return true;
                    }

                    default:
                    {
                        return this.isLiteralPropertyName();
                    }
                }
            }

            case ParsingContext.RestProperties:
            {
                return this.isLiteralPropertyName();
            }

            case ParsingContext.ObjectBindingElements:
            {
                return (((this.token == Kind.OpenBracketToken) || (this.token == Kind.DotDotDotToken)) || this.isLiteralPropertyName());
            }

            case ParsingContext.ImportAttributes:
            {
                return this.isImportAttributeName();
            }

            case ParsingContext.HeritageClauseElement:
            {
                if ((this.token == Kind.OpenBraceToken))
                {
                    return this.isValidHeritageClauseObjectLiteral();
                }

                if (!inErrorRecovery)
                {
                    return (this.isStartOfLeftHandSideExpression() && !this.isHeritageClauseExtendsOrImplementsKeyword());
                }

                return (this.isIdentifier() && !this.isHeritageClauseExtendsOrImplementsKeyword());
            }

            case ParsingContext.VariableDeclarations:
            {
                return this.isBindingIdentifierOrPrivateIdentifierOrPattern();
            }

            case ParsingContext.ArrayBindingElements:
            {
                return (((this.token == Kind.CommaToken) || (this.token == Kind.DotDotDotToken)) || this.isBindingIdentifierOrPrivateIdentifierOrPattern());
            }

            case ParsingContext.TypeParameters:
            {
                return (((this.token == Kind.InKeyword) || (this.token == Kind.ConstKeyword)) || this.isIdentifier());
            }

            case ParsingContext.ArrayLiteralMembers:
            {
                if (((this.token == Kind.CommaToken) || (this.token == Kind.DotToken)))
                {
                    return true;
                }

                goto case ParsingContext.ArgumentExpressions;
            }

            case ParsingContext.ArgumentExpressions:
            {
                return ((this.token == Kind.DotDotDotToken) || this.isStartOfExpression());
            }

            case ParsingContext.Parameters:
            {
                return this.isStartOfParameter(false);
            }

            case ParsingContext.JSDocParameters:
            {
                return this.isStartOfParameter(true);
            }

            case ParsingContext.TypeArguments:
            case ParsingContext.TupleElementTypes:
            {
                return ((this.token == Kind.CommaToken) || this.isStartOfType(false));
            }

            case ParsingContext.HeritageClauses:
            {
                return this.isHeritageClause();
            }

            case ParsingContext.ImportOrExportSpecifiers:
            {
                if (((this.token == Kind.FromKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsTokenStringLiteral())))
                {
                    return false;
                }

                if ((this.token == Kind.StringLiteral))
                {
                    return true;
                }

                return tokenIsIdentifierOrKeyword(this.token);
            }

            case ParsingContext.JsxAttributes:
            {
                return (tokenIsIdentifierOrKeyword(this.token) || (this.token == Kind.OpenBraceToken));
            }

            case ParsingContext.JsxChildren:
            {
                return true;
            }

            case ParsingContext.JSDocComment:
            {
                return true;
            }
        }

        throw new InvalidOperationException("Unhandled case in isListElement");
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:915:1.
    private bool isListTerminator(ParsingContext kind)
    {
        if ((this.token == Kind.EndOfFile))
        {
            return true;
        }

        switch (kind)
        {
            case ParsingContext.BlockStatements:
            case ParsingContext.SwitchClauses:
            case ParsingContext.TypeMembers:
            case ParsingContext.ClassMembers:
            case ParsingContext.EnumMembers:
            case ParsingContext.ObjectLiteralMembers:
            case ParsingContext.ObjectBindingElements:
            case ParsingContext.ImportOrExportSpecifiers:
            case ParsingContext.ImportAttributes:
            {
                return (this.token == Kind.CloseBraceToken);
            }

            case ParsingContext.SwitchClauseStatements:
            {
                return (((this.token == Kind.CloseBraceToken) || (this.token == Kind.CaseKeyword)) || (this.token == Kind.DefaultKeyword));
            }

            case ParsingContext.HeritageClauseElement:
            {
                return (((this.token == Kind.OpenBraceToken) || (this.token == Kind.ExtendsKeyword)) || (this.token == Kind.ImplementsKeyword));
            }

            case ParsingContext.VariableDeclarations:
            {
                return (((this.canParseSemicolon() || (this.token == Kind.InKeyword)) || (this.token == Kind.OfKeyword)) || (this.token == Kind.EqualsGreaterThanToken));
            }

            case ParsingContext.TypeParameters:
            {
                return (((((this.token == Kind.GreaterThanToken) || (this.token == Kind.OpenParenToken)) || (this.token == Kind.OpenBraceToken)) || (this.token == Kind.ExtendsKeyword)) || (this.token == Kind.ImplementsKeyword));
            }

            case ParsingContext.ArgumentExpressions:
            {
                return ((this.token == Kind.CloseParenToken) || (this.token == Kind.SemicolonToken));
            }

            case ParsingContext.ArrayLiteralMembers:
            case ParsingContext.TupleElementTypes:
            case ParsingContext.ArrayBindingElements:
            {
                return (this.token == Kind.CloseBracketToken);
            }

            case ParsingContext.JSDocParameters:
            case ParsingContext.Parameters:
            case ParsingContext.RestProperties:
            {
                return ((this.token == Kind.CloseParenToken) || (this.token == Kind.CloseBracketToken));
            }

            case ParsingContext.TypeArguments:
            {
                return (this.token != Kind.CommaToken);
            }

            case ParsingContext.HeritageClauses:
            {
                return ((this.token == Kind.OpenBraceToken) || (this.token == Kind.CloseBraceToken));
            }

            case ParsingContext.JsxAttributes:
            {
                return ((this.token == Kind.GreaterThanToken) || (this.token == Kind.SlashToken));
            }

            case ParsingContext.JsxChildren:
            {
                return ((this.token == Kind.LessThanToken) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsSlash()));
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:961:1.
    private bool parseExpectedJSDoc(Kind kind)
    {
        if ((this.token == kind))
        {
            this.nextTokenJSDoc();
            return true;
        }

        if (!isKeywordOrPunctuation(kind))
        {
            throw new InvalidOperationException("Invalid JSDoc kind: expected keyword or punctuation");
        }

        this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(kind));
        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:973:1.
    private void parseExpectedMatchingBrackets(Kind openKind, Kind closeKind, bool openParsed, int openPosition)
    {
        if ((this.token == closeKind))
        {
            this.nextToken();
            return;
        }

        var lastError = this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(closeKind));
        if (!openParsed)
        {
            return;
        }

        if ((lastError != null))
        {
            var related = Ast.NewDiagnostic(null, Core.NewTextRange(openPosition, openPosition), Diagnostics.The_parser_expected_to_find_a_1_to_match_the_0_token_here, Scanner.TokenToString(openKind), Scanner.TokenToString(closeKind));
            lastError.AddRelatedInfo(related);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:988:1.
    private bool parseOptional(Kind token)
    {
        if ((this.token == token))
        {
            this.nextToken();
            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:996:1.
    private bool parseExpected(Kind kind)
    {
        return this.parseExpectedWithDiagnostic(kind, null, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1000:1.
    private bool parseExpectedWithoutAdvancing(Kind kind)
    {
        return this.parseExpectedWithDiagnostic(kind, null, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1004:1.
    private bool parseExpectedWithDiagnostic(Kind kind, Message message, bool shouldAdvance)
    {
        if ((this.token == kind))
        {
            if (shouldAdvance)
            {
                this.nextToken();
            }

            return true;
        }

        if ((message != null))
        {
            this.parseErrorAtCurrentToken(message);
        }
        else
        {
            this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(kind));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1020:1.
    private Node parseTokenNode()
    {
        var pos = this.nodePos();
        var kind = this.token;
        this.nextToken();
        return this.finishNode(this.factory.NewToken(kind), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1027:1.
    private Node parseExpectedToken(Kind kind)
    {
        var token = this.parseOptionalToken(kind);
        if ((token == null))
        {
            this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(kind));
            token = this.finishNode(this.factory.NewToken(kind), this.nodePos());
        }

        return token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1036:1.
    private Node parseOptionalToken(Kind kind)
    {
        if ((this.token == kind))
        {
            return this.parseTokenNode();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1043:1.
    private Node parseExpectedTokenJSDoc(Kind kind)
    {
        var optional = this.parseOptionalTokenJSDoc(kind);
        if ((optional == null))
        {
            if (!isKeywordOrPunctuation(kind))
            {
                throw new InvalidOperationException("expected keyword or punctuation");
            }

            this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(kind));
            optional = this.finishNode(this.factory.NewToken(kind), this.nodePos());
        }

        return optional;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1055:1.
    private Node parseOptionalTokenJSDoc(Kind kind)
    {
        if ((this.token == kind))
        {
            return this.parseTokenNode();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1062:1.
    private Node parseStatement()
    {
        switch (this.token)
        {
            case Kind.SemicolonToken:
            {
                return this.parseEmptyStatement();
            }

            case Kind.OpenBraceToken:
            {
                return this.parseBlock(false, null);
            }

            case Kind.VarKeyword:
            {
                return this.parseVariableStatement(this.nodePos(), this.jsdocScannerInfo(), null);
            }

            case Kind.LetKeyword:
            {
                if (this.isLetDeclaration())
                {
                    return this.parseVariableStatement(this.nodePos(), this.jsdocScannerInfo(), null);
                }

                break;
            }

            case Kind.AwaitKeyword:
            {
                if (this.isAwaitUsingDeclaration())
                {
                    return this.parseVariableStatement(this.nodePos(), this.jsdocScannerInfo(), null);
                }

                break;
            }

            case Kind.UsingKeyword:
            {
                if (this.isUsingDeclaration())
                {
                    return this.parseVariableStatement(this.nodePos(), this.jsdocScannerInfo(), null);
                }

                break;
            }

            case Kind.FunctionKeyword:
            {
                return this.parseFunctionDeclaration(this.nodePos(), this.jsdocScannerInfo(), null);
            }

            case Kind.ClassKeyword:
            {
                return this.parseClassDeclaration(this.nodePos(), this.jsdocScannerInfo(), null);
            }

            case Kind.IfKeyword:
            {
                return this.parseIfStatement();
            }

            case Kind.DoKeyword:
            {
                return this.parseDoStatement();
            }

            case Kind.WhileKeyword:
            {
                return this.parseWhileStatement();
            }

            case Kind.ForKeyword:
            {
                return this.parseForOrForInOrForOfStatement();
            }

            case Kind.ContinueKeyword:
            {
                return this.parseContinueStatement();
            }

            case Kind.BreakKeyword:
            {
                return this.parseBreakStatement();
            }

            case Kind.ReturnKeyword:
            {
                return this.parseReturnStatement();
            }

            case Kind.WithKeyword:
            {
                return this.parseWithStatement();
            }

            case Kind.SwitchKeyword:
            {
                return this.parseSwitchStatement();
            }

            case Kind.ThrowKeyword:
            {
                return this.parseThrowStatement();
            }

            case Kind.TryKeyword:
            case Kind.CatchKeyword:
            case Kind.FinallyKeyword:
            {
                return this.parseTryStatement();
            }

            case Kind.DebuggerKeyword:
            {
                return this.parseDebuggerStatement();
            }

            case Kind.AtToken:
            {
                return this.parseDeclaration();
            }

            case Kind.AsyncKeyword:
            case Kind.InterfaceKeyword:
            case Kind.TypeKeyword:
            case Kind.ModuleKeyword:
            case Kind.NamespaceKeyword:
            case Kind.DeclareKeyword:
            case Kind.ConstKeyword:
            case Kind.EnumKeyword:
            case Kind.ExportKeyword:
            case Kind.ImportKeyword:
            case Kind.PrivateKeyword:
            case Kind.ProtectedKeyword:
            case Kind.PublicKeyword:
            case Kind.AbstractKeyword:
            case Kind.AccessorKeyword:
            case Kind.StaticKeyword:
            case Kind.ReadonlyKeyword:
            case Kind.GlobalKeyword:
            {
                if (this.isStartOfDeclaration())
                {
                    return this.parseDeclaration();
                }

                break;
            }
        }

        return this.parseExpressionOrLabeledStatement();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1123:1.
    private Node parseDeclaration()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var modifiers = this.parseModifiersEx(true, false, false);
        var isAmbient = ((modifiers != null) && Core.Some(modifiers.Nodes, isDeclareModifier));
        if (isAmbient)
        {
            {
                foreach (var m in GoRuntime.RangeValues(modifiers.Nodes))
                {
                    m.Flags |= NodeFlags.Ambient;
                }
            }

            var saveContextFlags = this.contextFlags;
            this.setContextFlags(NodeFlags.Ambient, true);
            var result = this.parseDeclarationWorker(pos, jsdoc, modifiers);
            this.contextFlags = saveContextFlags;
            return result;
        }
        else
        {
            return this.parseDeclarationWorker(pos, jsdoc, modifiers);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1150:1.
    private Node parseDeclarationWorker(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        switch (this.token)
        {
            case Kind.VarKeyword:
            case Kind.LetKeyword:
            case Kind.ConstKeyword:
            case Kind.UsingKeyword:
            {
                return this.parseVariableStatement(pos, jsdoc, modifiers);
            }

            case Kind.AwaitKeyword:
            {
                if (this.isAwaitUsingDeclaration())
                {
                    return this.parseVariableStatement(pos, jsdoc, modifiers);
                }

                break;
            }

            case Kind.FunctionKeyword:
            {
                return this.parseFunctionDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.ClassKeyword:
            {
                return this.parseClassDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.InterfaceKeyword:
            {
                return this.parseInterfaceDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.TypeKeyword:
            {
                return this.parseTypeAliasDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.EnumKeyword:
            {
                return this.parseEnumDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.GlobalKeyword:
            case Kind.ModuleKeyword:
            case Kind.NamespaceKeyword:
            {
                return this.parseModuleDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.ImportKeyword:
            {
                return this.parseImportDeclarationOrImportEqualsDeclaration(pos, jsdoc, modifiers);
            }

            case Kind.ExportKeyword:
            {
                this.nextToken();
                switch (this.token)
                {
                    case Kind.DefaultKeyword:
                    case Kind.EqualsToken:
                    {
                        return this.parseExportAssignment(pos, jsdoc, modifiers);
                    }

                    case Kind.AsKeyword:
                    {
                        return this.parseNamespaceExportDeclaration(pos, jsdoc, modifiers);
                    }

                    default:
                    {
                        return this.parseExportDeclaration(pos, jsdoc, modifiers);
                    }
                }
            }
        }

        if ((modifiers != null))
        {
            this.parseErrorAt(this.nodePos(), this.nodePos(), Diagnostics.Declaration_expected);
            return this.finishNode(this.factory.NewMissingDeclaration(modifiers), pos);
        }

        throw new InvalidOperationException("Unhandled case in parseDeclarationWorker");
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1192:1.
    private static bool isDeclareModifier(Node modifier)
    {
        return (modifier.Kind == Kind.DeclareKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1196:1.
    private bool isLetDeclaration()
    {
        return this.lookAhead((parserCallback) => parserCallback.nextTokenIsBindingIdentifierOrStartOfDestructuring());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1202:1.
    private bool nextTokenIsBindingIdentifierOrStartOfDestructuring()
    {
        this.nextToken();
        return ((this.isBindingIdentifier() || (this.token == Kind.OpenBraceToken)) || (this.token == Kind.OpenBracketToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1207:1.
    private Node parseBlock(bool ignoreMissingOpenBrace, Message diagnosticMessage)
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var openBracePosition = this.scanner.TokenStart();
        var openBraceParsed = this.parseExpectedWithDiagnostic(Kind.OpenBraceToken, diagnosticMessage, true);
        var multiline = false;
        if ((openBraceParsed || ignoreMissingOpenBrace))
        {
            multiline = this.hasPrecedingLineBreak();
            var statements = this.parseList(ParsingContext.BlockStatements, (parserCallback) => parserCallback.parseStatement());
            this.parseExpectedMatchingBrackets(Kind.OpenBraceToken, Kind.CloseBraceToken, openBraceParsed, openBracePosition);
            var result = this.finishNode(this.factory.NewBlock(statements, multiline), pos);
            this.withJSDoc(result, jsdoc);
            if ((this.token == Kind.EqualsToken))
            {
                this.parseErrorAtCurrentToken(Diagnostics.Declaration_or_statement_expected_This_follows_a_block_of_statements_so_if_you_intended_to_write_a_destructuring_assignment_you_might_need_to_wrap_the_whole_assignment_in_parentheses);
                this.nextToken();
            }

            return result;
        }

        var result2 = this.finishNode(this.factory.NewBlock(this.createMissingList(), multiline), pos);
        this.withJSDoc(result2, jsdoc);
        return result2;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1230:1.
    private Node parseEmptyStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.SemicolonToken);
        var result = this.finishNode(this.factory.NewEmptyStatement(), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1239:1.
    private Node parseIfStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.IfKeyword);
        var openParenPosition = this.scanner.TokenStart();
        var openParenParsed = this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpectedMatchingBrackets(Kind.OpenParenToken, Kind.CloseParenToken, openParenParsed, openParenPosition);
        var thenStatement = this.parseStatement();
        Node elseStatement = default;
        if (this.parseOptional(Kind.ElseKeyword))
        {
            elseStatement = this.parseStatement();
        }

        var result = this.finishNode(this.factory.NewIfStatement(expression, thenStatement, elseStatement), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1257:1.
    private Node parseDoStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.DoKeyword);
        var statement = this.parseStatement();
        this.parseExpected(Kind.WhileKeyword);
        var openParenPosition = this.scanner.TokenStart();
        var openParenParsed = this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpectedMatchingBrackets(Kind.OpenParenToken, Kind.CloseParenToken, openParenParsed, openParenPosition);
        this.parseOptional(Kind.SemicolonToken);
        var result = this.finishNode(this.factory.NewDoStatement(statement, expression), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1277:1.
    private Node parseWhileStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.WhileKeyword);
        var openParenPosition = this.scanner.TokenStart();
        var openParenParsed = this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpectedMatchingBrackets(Kind.OpenParenToken, Kind.CloseParenToken, openParenParsed, openParenPosition);
        var statement = this.parseStatement();
        var result = this.finishNode(this.factory.NewWhileStatement(expression, statement), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1291:1.
    private Node parseForOrForInOrForOfStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.ForKeyword);
        var awaitToken = this.parseOptionalToken(Kind.AwaitKeyword);
        this.parseExpected(Kind.OpenParenToken);
        Node initializer = default;
        if ((this.token != Kind.SemicolonToken))
        {
            if ((((((this.token == Kind.VarKeyword) || (this.token == Kind.LetKeyword)) || (this.token == Kind.ConstKeyword)) || ((this.token == Kind.UsingKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsBindingIdentifierOrStartOfDestructuringOnSameLineDisallowOf()))) || ((this.token == Kind.AwaitKeyword) && this.lookAhead((parserCallback) => parserCallback.nextIsUsingKeywordThenBindingIdentifierOrStartOfObjectDestructuringOnSameLine()))))
            {
                initializer = this.parseVariableDeclarationList(true);
            }
            else
            {
                initializer = this.doInContext(NodeFlags.DisallowInContext, true, (parserCallback) => parserCallback.parseExpression());
            }
        }

        Node result = default;
        switch (true)
        {
            case true when (((awaitToken != null) && this.parseExpected(Kind.OfKeyword)) || ((awaitToken == null) && this.parseOptional(Kind.OfKeyword))):
            {
                var expression = this.doInContext(NodeFlags.DisallowInContext, false, (parserCallback) => parserCallback.parseAssignmentExpressionOrHigher());
                this.parseExpected(Kind.CloseParenToken);
                result = this.factory.NewForInOrOfStatement(Kind.ForOfStatement, awaitToken, initializer, expression, this.parseStatement());
                break;
            }

            case true when this.parseOptional(Kind.InKeyword):
            {
                var expression2 = this.parseExpressionAllowIn();
                this.parseExpected(Kind.CloseParenToken);
                result = this.factory.NewForInOrOfStatement(Kind.ForInStatement, null, initializer, expression2, this.parseStatement());
                break;
            }

            default:
            {
                this.parseExpected(Kind.SemicolonToken);
                Node condition = default;
                if (((this.token != Kind.SemicolonToken) && (this.token != Kind.CloseParenToken)))
                {
                    condition = this.parseExpressionAllowIn();
                }

                this.parseExpected(Kind.SemicolonToken);
                Node incrementor = default;
                if ((this.token != Kind.CloseParenToken))
                {
                    incrementor = this.parseExpressionAllowIn();
                }

                this.parseExpected(Kind.CloseParenToken);
                result = this.factory.NewForStatement(initializer, condition, incrementor, this.parseStatement());
                break;
            }
        }

        this.finishNode(result, pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1337:1.
    private Node parseBreakStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.BreakKeyword);
        var label = this.parseIdentifierUnlessAtSemicolon();
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewBreakStatement(label), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1348:1.
    private Node parseContinueStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.ContinueKeyword);
        var label = this.parseIdentifierUnlessAtSemicolon();
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewContinueStatement(label), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1359:1.
    private Node parseIdentifierUnlessAtSemicolon()
    {
        if (!this.canParseSemicolon())
        {
            return this.parseIdentifier();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1366:1.
    private Node parseReturnStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.ReturnKeyword);
        Node expression = default;
        if (!this.canParseSemicolon())
        {
            expression = this.parseExpressionAllowIn();
        }

        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewReturnStatement(expression), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1380:1.
    private Node parseWithStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.WithKeyword);
        var openParenPosition = this.scanner.TokenStart();
        var openParenParsed = this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpectedMatchingBrackets(Kind.OpenParenToken, Kind.CloseParenToken, openParenParsed, openParenPosition);
        var statement = this.doInContext(NodeFlags.InWithStatement, true, (parserCallback) => parserCallback.parseStatement());
        var result = this.finishNode(this.factory.NewWithStatement(expression, statement), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1394:1.
    private Node parseCaseClause()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.CaseKeyword);
        var expression = this.parseExpressionAllowIn();
        this.parseExpected(Kind.ColonToken);
        var statements = this.parseList(ParsingContext.SwitchClauseStatements, (parserCallback) => parserCallback.parseStatement());
        var result = this.finishNode(this.factory.NewCaseOrDefaultClause(Kind.CaseClause, expression, statements), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1406:1.
    private Node parseDefaultClause()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.DefaultKeyword);
        this.parseExpected(Kind.ColonToken);
        var statements = this.parseList(ParsingContext.SwitchClauseStatements, (parserCallback) => parserCallback.parseStatement());
        var result = this.finishNode(this.factory.NewCaseOrDefaultClause(Kind.DefaultClause, null, statements), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1417:1.
    private Node parseCaseOrDefaultClause()
    {
        if ((this.token == Kind.CaseKeyword))
        {
            return this.parseCaseClause();
        }

        return this.parseDefaultClause();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1424:1.
    private Node parseCaseBlock()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.OpenBraceToken);
        var clauses = this.parseList(ParsingContext.SwitchClauses, (parserCallback) => parserCallback.parseCaseOrDefaultClause());
        this.parseExpected(Kind.CloseBraceToken);
        var result = this.finishNode(this.factory.NewCaseBlock(clauses), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1435:1.
    private Node parseSwitchStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.SwitchKeyword);
        this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpected(Kind.CloseParenToken);
        var caseBlock = this.parseCaseBlock();
        var result = this.finishNode(this.factory.NewSwitchStatement(expression, caseBlock), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1448:1.
    private Node parseThrowStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.ThrowKeyword);
        Node expression = default;
        if (!this.hasPrecedingLineBreak())
        {
            expression = this.parseExpressionAllowIn();
        }
        else
        {
            expression = this.createMissingIdentifier();
        }

        if (!this.tryParseSemicolon())
        {
            this.parseErrorForMissingSemicolonAfter(expression);
        }

        var result = this.finishNode(this.factory.NewThrowStatement(expression), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // TODO: Review for error recovery
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1474:1.
    private Node parseTryStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.TryKeyword);
        var tryBlock = this.parseBlock(false, null);
        Node catchClause = default;
        if ((this.token == Kind.CatchKeyword))
        {
            catchClause = this.parseCatchClause();
        }

        Node finallyBlock = default;
        if (((catchClause == null) || (this.token == Kind.FinallyKeyword)))
        {
            this.parseExpectedWithDiagnostic(Kind.FinallyKeyword, Diagnostics.X_catch_or_finally_expected, true);
            finallyBlock = this.parseBlock(false, null);
        }

        var result = this.finishNode(this.factory.NewTryStatement(tryBlock, catchClause, finallyBlock), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1495:1.
    private Node parseCatchClause()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.CatchKeyword);
        Node variableDeclaration = default;
        if (this.parseOptional(Kind.OpenParenToken))
        {
            variableDeclaration = this.parseVariableDeclaration();
            this.parseExpected(Kind.CloseParenToken);
        }

        var block = this.parseBlock(false, null);
        var result = this.finishNode(this.factory.NewCatchClause(variableDeclaration, block), pos);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1508:1.
    private Node parseDebuggerStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.DebuggerKeyword);
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewDebuggerStatement(), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1518:1.
    private Node parseExpressionOrLabeledStatement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var hasParen = (this.token == Kind.OpenParenToken);
        var expression = this.parseExpression();
        if (((expression.Kind == Kind.Identifier) && this.parseOptional(Kind.ColonToken)))
        {
            var result = this.finishNode(this.factory.NewLabeledStatement(expression, this.parseStatement()), pos);
            this.withJSDoc(result, jsdoc);
            return result;
        }

        if (!this.tryParseSemicolon())
        {
            this.parseErrorForMissingSemicolonAfter(expression);
        }

        var result2 = this.finishNode(this.factory.NewExpressionStatement(expression), pos);
        if (hasParen)
        {
            jsdoc &= ~(global::TypeScript.Scanner.jsdocScannerInfo.HasJSDoc);
        }

        this.withJSDoc(result2, jsdoc);
        return result2;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1544:1.
    private Node parseVariableStatement(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var declarationList = this.parseVariableDeclarationList(false);
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewVariableStatement(modifiers, declarationList), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1553:1.
    private Node parseVariableDeclarationList(bool inForStatementInitializer)
    {
        var pos = this.nodePos();
        NodeFlags flags = default;
        switch (this.token)
        {
            case Kind.VarKeyword:
            {
                flags = NodeFlags.None;
                break;
            }

            case Kind.LetKeyword:
            {
                flags = NodeFlags.Let;
                break;
            }

            case Kind.ConstKeyword:
            {
                flags = NodeFlags.Const;
                break;
            }

            case Kind.UsingKeyword:
            {
                flags = NodeFlags.Using;
                break;
            }

            case Kind.AwaitKeyword:
            {
                if (!this.isAwaitUsingDeclaration())
                {
                    break;
                }

                flags = NodeFlags.AwaitUsing;
                this.nextToken();
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in parseVariableDeclarationList");
            }
        }

        this.nextToken();
        NodeList declarations = default;
        if (((this.token == Kind.OfKeyword) && this.lookAhead((parserCallback) => parserCallback.nextIsIdentifierAndCloseParen())))
        {
            declarations = this.createMissingList();
        }
        else
        {
            var saveContextFlags = this.contextFlags;
            this.setContextFlags(NodeFlags.DisallowInContext, inForStatementInitializer);
            declarations = this.parseDelimitedList(ParsingContext.VariableDeclarations, Core.IfElse<Func<Parser, Node>>(inForStatementInitializer, (parserCallback) => parserCallback.parseVariableDeclaration(), (parserCallback) => parserCallback.parseVariableDeclarationAllowExclamation()));
            this.contextFlags = saveContextFlags;
        }

        var result = this.finishNode(this.factory.NewVariableDeclarationList(declarations, flags), pos);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1597:1.
    private bool nextIsIdentifierAndCloseParen()
    {
        return (this.nextTokenIsIdentifier() && (this.nextToken() == Kind.CloseParenToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1601:1.
    private bool nextTokenIsIdentifier()
    {
        this.nextToken();
        return this.isIdentifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1606:1.
    private Node parseVariableDeclaration()
    {
        return this.parseVariableDeclarationWorker(false);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1610:1.
    private Node parseVariableDeclarationAllowExclamation()
    {
        return this.parseVariableDeclarationWorker(true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1614:1.
    private Node parseVariableDeclarationWorker(bool allowExclamation)
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var name = this.parseIdentifierOrPatternWithDiagnostic(Diagnostics.Private_identifiers_are_not_allowed_in_variable_declarations);
        Node exclamationToken = default;
        if ((((allowExclamation && (name.Kind == Kind.Identifier)) && (this.token == Kind.ExclamationToken)) && !this.hasPrecedingLineBreak()))
        {
            exclamationToken = this.parseTokenNode();
        }

        var typeNode = this.parseTypeAnnotation();
        Node initializer = default;
        if (((this.token != Kind.InKeyword) && (this.token != Kind.OfKeyword)))
        {
            initializer = this.parseInitializer();
        }

        var result = this.finishNode(this.factory.NewVariableDeclaration(name, exclamationToken, typeNode, initializer), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1633:1.
    private Node parseIdentifierOrPattern()
    {
        return this.parseIdentifierOrPatternWithDiagnostic(null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1637:1.
    private Node parseIdentifierOrPatternWithDiagnostic(Message privateIdentifierDiagnosticMessage)
    {
        if ((this.token == Kind.OpenBracketToken))
        {
            return this.parseArrayBindingPattern();
        }

        if ((this.token == Kind.OpenBraceToken))
        {
            return this.parseObjectBindingPattern();
        }

        return this.parseBindingIdentifierWithDiagnostic(privateIdentifierDiagnosticMessage);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1647:1.
    private Node parseArrayBindingPattern()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.OpenBracketToken);
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.DisallowInContext, false);
        var elements = this.parseDelimitedList(ParsingContext.ArrayBindingElements, (parserCallback) => parserCallback.parseArrayBindingElement());
        this.contextFlags = saveContextFlags;
        this.parseExpected(Kind.CloseBracketToken);
        return this.finishNode(this.factory.NewBindingPattern(Kind.ArrayBindingPattern, elements), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1658:1.
    private Node parseArrayBindingElement()
    {
        var pos = this.nodePos();
        Node dotDotDotToken = default;
        Node name = default;
        Node initializer = default;
        if ((this.token != Kind.CommaToken))
        {
            dotDotDotToken = this.parseOptionalToken(Kind.DotDotDotToken);
            name = this.parseIdentifierOrPattern();
            initializer = this.parseInitializer();
        }

        return this.finishNode(this.factory.NewBindingElement(dotDotDotToken, null, name, initializer), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1672:1.
    private Node parseObjectBindingPattern()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.OpenBraceToken);
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.DisallowInContext, false);
        var elements = this.parseDelimitedList(ParsingContext.ObjectBindingElements, (parserCallback) => parserCallback.parseObjectBindingElement());
        this.contextFlags = saveContextFlags;
        this.parseExpected(Kind.CloseBraceToken);
        return this.finishNode(this.factory.NewBindingPattern(Kind.ObjectBindingPattern, elements), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1683:1.
    private Node parseObjectBindingElement()
    {
        var pos = this.nodePos();
        var dotDotDotToken = this.parseOptionalToken(Kind.DotDotDotToken);
        var tokenIsIdentifier = this.isBindingIdentifier();
        var propertyName = this.parsePropertyName();
        Node name = default;
        if ((tokenIsIdentifier && (this.token != Kind.ColonToken)))
        {
            name = propertyName;
            propertyName = null;
        }
        else
        {
            this.parseExpected(Kind.ColonToken);
            name = this.parseIdentifierOrPattern();
        }

        var initializer = this.parseInitializer();
        return this.finishNode(this.factory.NewBindingElement(dotDotDotToken, propertyName, name, initializer), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1700:1.
    private Node parseInitializer()
    {
        if (this.parseOptional(Kind.EqualsToken))
        {
            return this.parseAssignmentExpressionOrHigher();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1707:1.
    private Node parseTypeAnnotation()
    {
        if (this.parseOptional(Kind.ColonToken))
        {
            return this.parseType();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1714:1.
    private Node parseFunctionDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        this.parseExpected(Kind.FunctionKeyword);
        var asteriskToken = this.parseOptionalToken(Kind.AsteriskToken);
        Node name = default;
        if ((((modifiers == null) || ((modifiers.ModifierFlags & ModifierFlags.Default) == 0)) || this.isBindingIdentifier()))
        {
            name = this.parseBindingIdentifier();
        }

        var signatureFlags = (Core.IfElse((asteriskToken != null), ParseFlags.Yield, ParseFlags.None) | Core.IfElse(((modifiers != null) && ((modifiers.ModifierFlags & ModifierFlags.Async) != 0)), ParseFlags.Await, ParseFlags.None));
        var typeParameters = this.parseTypeParameters();
        var saveContextFlags = this.contextFlags;
        if (((modifiers != null) && ((modifiers.ModifierFlags & ModifierFlags.Export) != 0)))
        {
            this.setContextFlags(NodeFlags.AwaitContext, true);
        }

        var parameters = this.parseParameters(signatureFlags);
        var returnType = this.parseReturnType(Kind.ColonToken, false);
        var body = this.parseFunctionBlockOrSemicolon(signatureFlags, Diagnostics.X_or_expected);
        this.contextFlags = saveContextFlags;
        var result = this.finishNode(this.factory.NewFunctionDeclaration(modifiers, asteriskToken, name, typeParameters, parameters, returnType, null, body), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1738:1.
    private Node parseClassDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        return this.parseClassDeclarationOrExpression(pos, jsdoc, modifiers, Kind.ClassDeclaration);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1742:1.
    private Node parseClassExpression()
    {
        return this.parseClassDeclarationOrExpression(this.nodePos(), this.jsdocScannerInfo(), null, Kind.ClassExpression);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1746:1.
    private Node parseClassDeclarationOrExpression(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers, Kind kind)
    {
        var saveContextFlags = this.contextFlags;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        this.parseExpected(Kind.ClassKeyword);
        var name = this.parseNameOfClassDeclarationOrExpression();
        var typeParameters = this.parseTypeParameters();
        if (((((modifiers != null) && ((this.parsingContexts & ((1 << (int)(ParsingContext.SourceElements)))) != 0)) && ((this.parsingContexts & ((((1 << (int)(ParsingContext.BlockStatements))) | ((1 << (int)(ParsingContext.SwitchClauseStatements)))))) == 0)) && Core.Some(modifiers.Nodes, isExportModifier)))
        {
            this.setContextFlags(NodeFlags.AwaitContext, true);
        }

        var heritageClauses = this.parseHeritageClauses(false);
        NodeList members = default;
        if (this.parseExpected(Kind.OpenBraceToken))
        {
            members = this.parseList(ParsingContext.ClassMembers, (parserCallback) => parserCallback.parseClassElement());
            this.parseExpected(Kind.CloseBraceToken);
        }
        else
        {
            members = this.createMissingList();
        }

        this.contextFlags = saveContextFlags;
        Node result = default;
        if (((modifiers != null) && ((Ast.ModifiersToFlags(modifiers.Nodes) & ModifierFlags.Ambient) != 0)))
        {
            this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        }

        if ((kind == Kind.ClassDeclaration))
        {
            result = this.factory.NewClassDeclaration(modifiers, name, typeParameters, heritageClauses, members);
        }
        else
        {
            result = this.factory.NewClassExpression(modifiers, name, typeParameters, heritageClauses, members);
        }

        this.finishNode(result, pos);
        this.withJSDoc(result, jsdoc);
        if (((result.Flags & NodeFlags.JavaScriptFile) != 0))
        {
            this.checkJSSyntax(result);
            if ((heritageClauses != null))
            {
                {
                    foreach (var clause in GoRuntime.RangeValues(heritageClauses.Nodes))
                    {
                        if ((clause.AsHeritageClause().Token == Kind.ExtendsKeyword))
                        {
                            {
                                foreach (var range9 in GoRuntime.Range(clause.AsHeritageClause().Types.Nodes))
                                {
                                    var _2 = range9.Key;
                                    var expr = range9.Value;
                                    this.checkJSSyntax(expr);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1796:1.
    private Node parseNameOfClassDeclarationOrExpression()
    {
        if ((this.isBindingIdentifier() && !this.isImplementsClause()))
        {
            var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
            var id = this.createIdentifier(this.isBindingIdentifier());
            this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
            return id;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1811:1.
    private bool isImplementsClause()
    {
        return ((this.token == Kind.ImplementsKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeyword()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1815:1.
    private static bool isExportModifier(Node modifier)
    {
        return (modifier.Kind == Kind.ExportKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1819:1.
    private static bool isAsyncModifier(Node modifier)
    {
        return (modifier.Kind == Kind.AsyncKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1823:1.
    private NodeList parseHeritageClauses(bool isInterface)
    {
        if (this.isHeritageClause())
        {
            return this.parseList(ParsingContext.HeritageClauses, (p) =>
            {
                return p.parseHeritageClause(isInterface);
            });
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1834:1.
    private Node parseHeritageClause(bool isInterface)
    {
        var pos = this.nodePos();
        var kind = this.token;
        this.nextToken();
        Func<Parser, Node> parseElement = (parserCallback) => parserCallback.parseExpressionWithTypeArguments();
        if (isTypeHeritageClause(isInterface, kind))
        {
            parseElement = (parserCallback) => parserCallback.parseTypeHeritageClauseElement();
        }

        var types = this.parseDelimitedList(ParsingContext.HeritageClauseElement, parseElement);
        return this.checkJSSyntax(this.finishNode(this.factory.NewHeritageClause(kind, types), pos));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1846:1.
    private static bool isTypeHeritageClause(bool isInterface, Kind token)
    {
        return ((isInterface && (token == Kind.ExtendsKeyword)) || (!isInterface && (token == Kind.ImplementsKeyword)));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1851:1.
    private Node parseTypeHeritageClauseElement()
    {
        var pos = this.nodePos();
        var expressionWithTypeArguments = this.parseExpressionWithTypeArguments().AsExpressionWithTypeArguments();
        if (!isValidHeritageTypeReferenceExpression(expressionWithTypeArguments.Expression))
        {
            return expressionWithTypeArguments.AsNode();
        }

        var typeName = this.convertEntityNameExpressionToEntityName(expressionWithTypeArguments.Expression);
        return this.finishNode(this.factory.NewTypeReferenceNode(typeName, expressionWithTypeArguments.TypeArguments), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1861:1.
    private static bool isValidHeritageTypeReferenceExpression(Node node)
    {
        if (Ast.IsIdentifier(node))
        {
            return Ast.NodeIsPresent(node);
        }

        return (((Ast.IsPropertyAccessExpression(node) && !Ast.IsOptionalChain(node)) && Ast.NodeIsPresent(node.Name())) && isValidHeritageTypeReferenceExpression(node.Expression()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1871:1.
    private Node convertEntityNameExpressionToEntityName(Node node)
    {
        if (Ast.IsIdentifier(node))
        {
            return node;
        }

        var propertyAccess = node.AsPropertyAccessExpression();
        var result = this.factory.NewQualifiedName(this.convertEntityNameExpressionToEntityName(propertyAccess.Expression), propertyAccess.Name());
        return this.finishNodeWithEnd(result, node.Pos(), node.End());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1883:1.
    private Node parseExpressionWithTypeArguments()
    {
        var pos = this.nodePos();
        var expression = this.parseLeftHandSideExpressionOrHigher();
        if (Ast.IsExpressionWithTypeArguments(expression))
        {
            return expression;
        }

        var typeArguments = this.parseTypeArguments();
        return this.finishNode(this.factory.NewExpressionWithTypeArguments(expression, typeArguments), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1893:1.
    private Node parseClassElement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        if ((this.token == Kind.SemicolonToken))
        {
            this.nextToken();
            var result = this.finishNode(this.factory.NewSemicolonClassElement(), pos);
            this.withJSDoc(result, jsdoc);
            return result;
        }

        var modifiers = this.parseModifiersEx(true, true, true);
        if (((this.token == Kind.StaticKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenBrace())))
        {
            return this.parseClassStaticBlockDeclaration(pos, jsdoc, modifiers);
        }

        if (this.parseContextualModifier(Kind.GetKeyword))
        {
            return this.parseAccessorDeclaration(pos, jsdoc, modifiers, Kind.GetAccessor, ParseFlags.None);
        }

        if (this.parseContextualModifier(Kind.SetKeyword))
        {
            return this.parseAccessorDeclaration(pos, jsdoc, modifiers, Kind.SetAccessor, ParseFlags.None);
        }

        if (((this.token == Kind.ConstructorKeyword) || (this.token == Kind.StringLiteral)))
        {
            var constructorDeclaration = this.tryParseConstructorDeclaration(pos, jsdoc, modifiers);
            if ((constructorDeclaration != null))
            {
                return constructorDeclaration;
            }
        }

        if (this.isIndexSignature())
        {
            return this.checkJSSyntax(this.parseIndexSignatureDeclaration(pos, jsdoc, modifiers));
        }

        if ((((((tokenIsIdentifierOrKeyword(this.token) || (this.token == Kind.StringLiteral)) || (this.token == Kind.NumericLiteral)) || (this.token == Kind.BigIntLiteral)) || (this.token == Kind.AsteriskToken)) || (this.token == Kind.OpenBracketToken)))
        {
            var isAmbient = ((modifiers != null) && Core.Some(modifiers.Nodes, isDeclareModifier));
            if (isAmbient)
            {
                {
                    foreach (var m in GoRuntime.RangeValues(modifiers.Nodes))
                    {
                        m.Flags |= NodeFlags.Ambient;
                    }
                }

                var saveContextFlags = this.contextFlags;
                this.setContextFlags(NodeFlags.Ambient, true);
                var result2 = this.parsePropertyOrMethodDeclaration(pos, jsdoc, modifiers);
                this.contextFlags = saveContextFlags;
                return result2;
            }
            else
            {
                return this.parsePropertyOrMethodDeclaration(pos, jsdoc, modifiers);
            }
        }

        if ((modifiers != null))
        {
            this.parseErrorAt(this.nodePos(), this.nodePos(), Diagnostics.Declaration_expected);
            var name = this.createMissingIdentifier();
            return this.parsePropertyDeclaration(pos, jsdoc, modifiers, name, null);
        }

        throw new InvalidOperationException("Should not have attempted to parse class member declaration.");
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1948:1.
    private Node parseClassStaticBlockDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        this.parseExpectedToken(Kind.StaticKeyword);
        var body = this.parseClassStaticBlockBody();
        var result = this.finishNode(this.factory.NewClassStaticBlockDeclaration(modifiers, body), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1956:1.
    private Node parseClassStaticBlockBody()
    {
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.YieldContext, false);
        this.setContextFlags(NodeFlags.AwaitContext, true);
        var body = this.parseBlock(false, null);
        this.contextFlags = saveContextFlags;
        return body;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1965:1.
    private Node tryParseConstructorDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var state = this.mark();
        if (((this.token == Kind.ConstructorKeyword) || (((this.token == Kind.StringLiteral) && this.scanner.TokenValueEquals("constructor"u8)) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenParen()))))
        {
            this.nextToken();
            var typeParameters = this.parseTypeParameters();
            var parameters = this.parseParameters(ParseFlags.None);
            var returnType = this.parseReturnType(Kind.ColonToken, false);
            var body = this.parseFunctionBlockOrSemicolon(ParseFlags.None, Diagnostics.X_or_expected);
            var result = this.finishNode(this.factory.NewConstructorDeclaration(modifiers, typeParameters, parameters, returnType, null, body), pos);
            this.withJSDoc(result, jsdoc);
            this.checkJSSyntax(result);
            return result;
        }

        this.rewind(state);
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1982:1.
    private bool nextTokenIsOpenParen()
    {
        return (this.nextToken() == Kind.OpenParenToken);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1986:1.
    private Node parsePropertyOrMethodDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var asteriskToken = this.parseOptionalToken(Kind.AsteriskToken);
        var name = this.parsePropertyName();
        var questionToken = this.parseOptionalToken(Kind.QuestionToken);
        if ((((asteriskToken != null) || (this.token == Kind.OpenParenToken)) || (this.token == Kind.LessThanToken)))
        {
            return this.parseMethodDeclaration(pos, jsdoc, modifiers, asteriskToken, name, questionToken, Diagnostics.X_or_expected);
        }

        return this.parsePropertyDeclaration(pos, jsdoc, modifiers, name, questionToken);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:1998:1.
    private Node parseMethodDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers, Node asteriskToken, Node name, Node questionToken, Message diagnosticMessage)
    {
        var signatureFlags = (Core.IfElse((asteriskToken != null), ParseFlags.Yield, ParseFlags.None) | Core.IfElse(modifierListHasAsync(modifiers), ParseFlags.Await, ParseFlags.None));
        var typeParameters = this.parseTypeParameters();
        var parameters = this.parseParameters(signatureFlags);
        var typeNode = this.parseReturnType(Kind.ColonToken, false);
        var body = this.parseFunctionBlockOrSemicolon(signatureFlags, diagnosticMessage);
        var result = this.finishNode(this.factory.NewMethodDeclaration(modifiers, asteriskToken, name, questionToken, typeParameters, parameters, typeNode, null, body), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2010:1.
    private static bool modifierListHasAsync(ModifierList modifiers)
    {
        return ((modifiers != null) && Core.Some(modifiers.Nodes, isAsyncModifier));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2014:1.
    private Node parsePropertyDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers, Node name, Node questionToken)
    {
        var postfixToken = questionToken;
        if (((postfixToken == null) && !this.hasPrecedingLineBreak()))
        {
            postfixToken = this.parseOptionalToken(Kind.ExclamationToken);
        }

        var typeNode = this.parseTypeAnnotation();
        var initializer = this.doInContext(((NodeFlags.YieldContext | NodeFlags.AwaitContext) | NodeFlags.DisallowInContext), false, (parserCallback) => parserCallback.parseInitializer());
        this.parseSemicolonAfterPropertyName(name, typeNode, initializer);
        var result = this.finishNode(this.factory.NewPropertyDeclaration(modifiers, name, postfixToken, typeNode, initializer), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2028:1.
    private void parseSemicolonAfterPropertyName(Node name, Node typeNode, Node initializer)
    {
        if (((this.token == Kind.AtToken) && !this.hasPrecedingLineBreak()))
        {
            this.parseErrorAtCurrentToken(Diagnostics.Decorators_must_precede_the_name_and_all_keywords_of_property_declarations);
            return;
        }

        if ((this.token == Kind.OpenParenToken))
        {
            this.parseErrorAtCurrentToken(Diagnostics.Cannot_start_a_function_call_in_a_type_annotation);
            this.nextToken();
            return;
        }

        if (((typeNode != null) && !this.canParseSemicolon()))
        {
            if ((initializer != null))
            {
                this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(Kind.SemicolonToken));
            }
            else
            {
                this.parseErrorAtCurrentToken(Diagnostics.Expected_for_property_initializer);
            }

            return;
        }

        if (this.tryParseSemicolon())
        {
            return;
        }

        if ((initializer != null))
        {
            this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(Kind.SemicolonToken));
            return;
        }

        this.parseErrorForMissingSemicolonAfter(name);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2056:1.
    private void parseErrorForMissingSemicolonAfter(Node node)
    {
        if ((node.Kind == Kind.TaggedTemplateExpression))
        {
            this.parseErrorAtRange(this.skipRangeTrivia(node.AsTaggedTemplateExpression().Template.Loc), Diagnostics.Module_declaration_names_may_only_use_or_quoted_strings);
            return;
        }

        ByteStringValue expressionText = default;
        if ((node.Kind == Kind.Identifier))
        {
            expressionText = node.TextValue();
        }

        if ((expressionText.EqualsBytes(""u8)))
        {
            this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(Kind.SemicolonToken));
            return;
        }

        var pos = Scanner.SkipTriviaByteString(this.sourceText, node.Pos());
        switch (true)
        {
            case true when expressionText.EqualsBytes("const"u8):
            case true when expressionText.EqualsBytes("let"u8):
            case true when expressionText.EqualsBytes("var"u8):
            {
                this.parseErrorAt(pos, node.End(), Diagnostics.Variable_declaration_not_allowed_at_this_location);
                return;
            }

            case true when expressionText.EqualsBytes("declare"u8):
            {
                return;
            }

            case true when expressionText.EqualsBytes("interface"u8):
            {
                this.parseErrorForInvalidName(Diagnostics.Interface_name_cannot_be_0, Diagnostics.Interface_must_be_given_a_name, Kind.OpenBraceToken);
                return;
            }

            case true when expressionText.EqualsBytes("is"u8):
            {
                this.parseErrorAt(pos, this.scanner.TokenStart(), Diagnostics.A_type_predicate_is_only_allowed_in_return_type_position_for_functions_and_methods);
                return;
            }

            case true when expressionText.EqualsBytes("module"u8):
            case true when expressionText.EqualsBytes("namespace"u8):
            {
                this.parseErrorForInvalidName(Diagnostics.Namespace_name_cannot_be_0, Diagnostics.Namespace_must_be_given_a_name, Kind.OpenBraceToken);
                return;
            }

            case true when expressionText.EqualsBytes("type"u8):
            {
                this.parseErrorForInvalidName(Diagnostics.Type_alias_name_cannot_be_0, Diagnostics.Type_alias_must_be_given_a_name, Kind.EqualsToken);
                return;
            }
        }

        var suggestion = Core.GetSpellingSuggestionForStrings(expressionText.Materialize(), GoSlices.Values(viableKeywordSuggestions));
        if ((suggestion == ""))
        {
            suggestion = getSpaceSuggestion(expressionText.Materialize());
        }

        if ((suggestion != ""))
        {
            this.parseErrorAt(pos, node.End(), Diagnostics.Unknown_keyword_or_identifier_Did_you_mean_0, suggestion);
            return;
        }

        if ((this.token == Kind.Unknown))
        {
            return;
        }

        this.parseErrorAt(pos, node.End(), Diagnostics.Unexpected_keyword_or_identifier);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2112:1.
    private static string getSpaceSuggestion(string expressionText)
    {
        {
            foreach (var keyword in GoRuntime.RangeValues(viableKeywordSuggestions))
            {
                if (((GoRuntime.Len(expressionText) > (GoRuntime.Len(keyword) + 2)) && GoStrings.HasPrefix(expressionText, keyword)))
                {
                    return ((keyword + " ") + GoRuntime.Slice(expressionText, GoRuntime.Len(keyword)));
                }
            }
        }

        return "";
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2121:1.
    private void parseErrorForInvalidName(Message nameDiagnostic, Message blankDiagnostic, Kind tokenIfBlankName)
    {
        if ((this.token == tokenIfBlankName))
        {
            this.parseErrorAtCurrentToken(blankDiagnostic);
        }
        else
        {
            this.parseErrorAtCurrentToken(nameDiagnostic, this.scanner.TokenValueByteString());
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2129:1.
    private Node parseInterfaceDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        this.parseExpected(Kind.InterfaceKeyword);
        var name = this.parseIdentifier();
        var typeParameters = this.parseTypeParameters();
        var heritageClauses = this.parseHeritageClauses(true);
        var members = this.parseObjectTypeMembers();
        var result = this.finishNode(this.factory.NewInterfaceDeclaration(modifiers, name, typeParameters, heritageClauses, members), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2141:1.
    private Node parseTypeAliasDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        this.parseExpected(Kind.TypeKeyword);
        if (this.hasPrecedingLineBreak())
        {
            this.parseErrorAtCurrentToken(Diagnostics.Line_break_not_permitted_here);
        }

        var name = this.parseIdentifier();
        var typeParameters = this.parseTypeParameters();
        this.parseExpected(Kind.EqualsToken);
        Node typeNode = default;
        if (((this.token == Kind.IntrinsicKeyword) && this.lookAhead((parserCallback) => parserCallback.nextIsNotDot())))
        {
            typeNode = this.parseKeywordTypeNode();
        }
        else
        {
            typeNode = this.parseType();
        }

        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewTypeAliasDeclaration(modifiers, name, typeParameters, typeNode), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2162:1.
    private bool nextIsNotDot()
    {
        return (this.nextToken() != Kind.DotToken);
    }

    // In an ambient declaration, the grammar only allows integer literals as initializers.
    // In a non-ambient declaration, the grammar allows uninitialized members only in a
    // ConstantEnumMemberSection, which starts at the beginning of an enum declaration
    // or any time an integer literal initializer is encountered.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2170:1.
    private Node parseEnumMember()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var name = this.parsePropertyName();
        var initializer = this.doInContext(NodeFlags.DisallowInContext, false, (parserCallback) => parserCallback.parseInitializer());
        var result = this.finishNode(this.factory.NewEnumMember(name, initializer), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2180:1.
    private Node parseEnumDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        this.parseExpected(Kind.EnumKeyword);
        var name = this.parseIdentifier();
        NodeList members = default;
        if (this.parseExpected(Kind.OpenBraceToken))
        {
            var saveContextFlags = this.contextFlags;
            this.setContextFlags((NodeFlags.YieldContext | NodeFlags.AwaitContext), false);
            members = this.parseDelimitedList(ParsingContext.EnumMembers, (parserCallback) => parserCallback.parseEnumMember());
            this.contextFlags = saveContextFlags;
            this.parseExpected(Kind.CloseBraceToken);
        }
        else
        {
            members = this.createMissingList();
        }

        var result = this.finishNode(this.factory.NewEnumDeclaration(modifiers, name, members), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2201:1.
    private Node parseModuleDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var keyword = Kind.ModuleKeyword;
        if ((this.token == Kind.GlobalKeyword))
        {
            return this.parseAmbientExternalModuleDeclaration(pos, jsdoc, modifiers);
        }
        else
        {
            if (this.parseOptional(Kind.NamespaceKeyword))
            {
                keyword = Kind.NamespaceKeyword;
            }
            else
            {
                this.parseExpected(Kind.ModuleKeyword);
                if ((this.token == Kind.StringLiteral))
                {
                    return this.parseAmbientExternalModuleDeclaration(pos, jsdoc, modifiers);
                }
            }
        }

        return this.parseModuleOrNamespaceDeclaration(pos, jsdoc, modifiers, false, keyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2217:1.
    private Node parseAmbientExternalModuleDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        Node name = default;
        var keyword = Kind.ModuleKeyword;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        if ((this.token == Kind.GlobalKeyword))
        {
            name = this.parseIdentifier();
            keyword = Kind.GlobalKeyword;
        }
        else
        {
            name = this.parseLiteralExpression();
        }

        Node attributes = default;
        if (((keyword == Kind.ModuleKeyword) && this.parseOptional(Kind.WithKeyword)))
        {
            attributes = this.parseTypeLiteral();
        }

        Node body = default;
        if ((this.token == Kind.OpenBraceToken))
        {
            body = this.parseModuleBlock();
        }
        else
        {
            this.parseSemicolon();
        }

        var result = this.finishNode(this.factory.NewModuleDeclaration(modifiers, keyword, name, attributes, body), pos);
        this.withJSDoc(result, jsdoc);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2245:1.
    private Node parseModuleBlock()
    {
        var pos = this.nodePos();
        NodeList statements = default;
        if (this.parseExpected(Kind.OpenBraceToken))
        {
            statements = this.parseList(ParsingContext.BlockStatements, (parserCallback) => parserCallback.parseStatement());
            this.parseExpected(Kind.CloseBraceToken);
        }
        else
        {
            statements = this.createMissingList();
        }

        return this.finishNode(this.factory.NewModuleBlock(statements), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2257:1.
    private Node parseModuleOrNamespaceDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers, bool nested, Kind keyword)
    {
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        Node name = default;
        if (nested)
        {
            name = this.parseIdentifierName();
        }
        else
        {
            name = this.parseIdentifier();
        }

        Node body = default;
        if (this.parseOptional(Kind.DotToken))
        {
            var implicitExport = this.factory.NewModifier(Kind.ExportKeyword);
            implicitExport.Loc = Core.NewTextRange(this.nodePos(), this.nodePos());
            implicitExport.Flags = NodeFlags.Reparsed;
            var implicitModifiers = this.newModifierList(implicitExport.Loc, this.nodeSliceArena.NewSlice1Header(implicitExport));
            body = this.parseModuleOrNamespaceDeclaration(this.nodePos(), 0, implicitModifiers, true, keyword);
        }
        else
        {
            body = this.parseModuleBlock();
        }

        var result = this.finishNode(this.factory.NewModuleDeclaration(modifiers, keyword, name, null, body), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2282:1.
    private Node parseImportDeclarationOrImportEqualsDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        this.parseExpected(Kind.ImportKeyword);
        var afterImportPos = this.nodePos();
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var phaseModifierCandidate = this.currentImportPhaseModifier();
        Node identifier = default;
        if (this.isIdentifier())
        {
            identifier = this.parseIdentifier();
        }

        var phaseModifier = Kind.Unknown;
        if (((((identifier != null) && (identifier.TextEquals("type"u8))) && (((this.token != Kind.FromKeyword) || (this.isIdentifier() && this.lookAhead((parserCallback) => parserCallback.nextTokenIsFromKeywordOrEqualsToken()))))) && ((this.isIdentifier() || this.tokenAfterImportDefinitelyProducesImportDeclaration()))))
        {
            phaseModifier = Kind.TypeKeyword;
            identifier = null;
            if (this.isIdentifier())
            {
                identifier = this.parseIdentifier();
            }
        }
        else
        {
            if ((((identifier != null) && (phaseModifierCandidate != Kind.Unknown)) && this.shouldParseImportPhaseModifier()))
            {
                phaseModifier = phaseModifierCandidate;
                identifier = null;
                if (this.isIdentifier())
                {
                    identifier = this.parseIdentifier();
                }
            }
        }

        if (((((identifier != null) && this.tokenAfterImportedIdentifierAllowsImportEqualsDeclaration()) && (phaseModifier != Kind.DeferKeyword)) && (phaseModifier != Kind.SourceKeyword)))
        {
            var importEquals = this.checkJSSyntax(this.parseImportEqualsDeclaration(pos, jsdoc, modifiers, identifier, (phaseModifier == Kind.TypeKeyword)));
            this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
            return importEquals;
        }

        var importClause = this.tryParseImportClause(identifier, afterImportPos, phaseModifier, false);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        var moduleSpecifier = this.parseModuleSpecifier();
        var attributes = this.tryParseImportAttributes();
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewImportDeclaration(modifiers, importClause, moduleSpecifier, attributes), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2324:1.
    private bool nextTokenIsFromKeywordOrEqualsToken()
    {
        this.nextToken();
        return ((this.token == Kind.FromKeyword) || (this.token == Kind.EqualsToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2329:1.
    private bool shouldParseImportPhaseModifier()
    {
        switch (this.token)
        {
            case Kind.CommaToken:
            case Kind.EqualsToken:
            {
                return false;
            }

            case Kind.FromKeyword:
            {
                if (this.lookAhead((parserCallback) => parserCallback.nextTokenIsTokenStringLiteral()))
                {
                    return false;
                }

                break;
            }
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2341:1.
    private Kind currentImportPhaseModifier()
    {
        var text = this.scanner.TokenTextView().AsSpan();
        if (text.SequenceEqual("defer"u8)) return Kind.DeferKeyword;
        if (text.SequenceEqual("source"u8)) return Kind.SourceKeyword;
        return Kind.Unknown;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2352:1.
    private bool tokenAfterImportDefinitelyProducesImportDeclaration()
    {
        return ((this.token == Kind.AsteriskToken) || (this.token == Kind.OpenBraceToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2356:1.
    private bool tokenAfterImportedIdentifierAllowsImportEqualsDeclaration()
    {
        switch (this.token)
        {
            case Kind.CommaToken:
            case Kind.FromKeyword:
            {
                return false;
            }

            default:
            {
                return true;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2365:1.
    private Node parseImportEqualsDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers, Node identifier, bool isTypeOnly)
    {
        this.parseExpected(Kind.EqualsToken);
        var moduleReference = this.parseModuleReference();
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewImportEqualsDeclaration(modifiers, isTypeOnly, identifier, moduleReference), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2374:1.
    private Node parseModuleReference()
    {
        if (((this.token == Kind.RequireKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenParen())))
        {
            return this.parseExternalModuleReference();
        }

        return this.parseEntityName(false, false, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2381:1.
    private Node parseExternalModuleReference()
    {
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var pos = this.nodePos();
        this.parseExpected(Kind.RequireKeyword);
        this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseModuleSpecifier();
        this.parseExpected(Kind.CloseParenToken);
        var result = this.finishNode(this.factory.NewExternalModuleReference(expression), pos);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2393:1.
    private Node parseModuleSpecifier()
    {
        if ((this.token == Kind.StringLiteral))
        {
            return this.parseLiteralExpression();
        }

        return this.parseExpression();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2403:1.
    private Node tryParseImportClause(Node identifier, int pos, Kind phaseModifier, bool skipJSDocLeadingAsterisks)
    {
        if ((((identifier != null) || (this.token == Kind.AsteriskToken)) || (this.token == Kind.OpenBraceToken)))
        {
            var importClause = this.parseImportClause(identifier, pos, phaseModifier, skipJSDocLeadingAsterisks);
            this.parseExpected(Kind.FromKeyword);
            return importClause;
        }

        if (((phaseModifier == Kind.DeferKeyword) || (phaseModifier == Kind.SourceKeyword)))
        {
            return this.finishNode(this.factory.NewImportClause(phaseModifier, null, null), pos);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2418:1.
    private Node parseImportClause(Node identifier, int pos, Kind phaseModifier, bool skipJSDocLeadingAsterisks)
    {
        Node namedBindings = default;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        if (((identifier == null) || this.parseOptional(Kind.CommaToken)))
        {
            if (skipJSDocLeadingAsterisks)
            {
                this.scanner.SetSkipJSDocLeadingAsterisks(true);
            }

            if ((this.token == Kind.AsteriskToken))
            {
                namedBindings = this.parseNamespaceImport();
            }
            else
            {
                namedBindings = this.parseNamedImports();
            }

            if (skipJSDocLeadingAsterisks)
            {
                this.scanner.SetSkipJSDocLeadingAsterisks(false);
            }
        }

        var result = this.finishNode(this.factory.NewImportClause(phaseModifier, identifier, namedBindings), pos);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2447:1.
    private Node parseNamespaceImport()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.AsteriskToken);
        this.parseExpected(Kind.AsKeyword);
        var name = this.parseIdentifier();
        return this.finishNode(this.factory.NewNamespaceImport(name), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2457:1.
    private Node parseNamedImports()
    {
        var pos = this.nodePos();
        var imports = this.parseBracketedList(ParsingContext.ImportOrExportSpecifiers, (parserCallback) => parserCallback.parseImportSpecifier(), Kind.OpenBraceToken, Kind.CloseBraceToken);
        return this.finishNode(this.factory.NewNamedImports(imports), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2467:1.
    private Node parseImportSpecifier()
    {
        var pos = this.nodePos();
        var tuple12 = this.parseImportOrExportSpecifier(Kind.ImportSpecifier);
        var isTypeOnly = tuple12.Item1;
        var propertyName = tuple12.Item2;
        var name = tuple12.Item3;
        Node identifierName = default;
        if ((name.Kind == Kind.Identifier))
        {
            identifierName = name;
        }
        else
        {
            this.parseErrorAtRange(this.skipRangeTrivia(name.Loc), Diagnostics.Identifier_expected);
            identifierName = this.newIdentifier("");
            this.finishNode(identifierName, name.Pos());
        }

        var result = this.checkJSSyntax(this.finishNode(this.factory.NewImportSpecifier(isTypeOnly, propertyName, identifierName), pos));
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2482:1.
    private (bool, Node, Node) parseImportOrExportSpecifier(Kind kind)
    {
        bool isTypeOnly = default;
        Node propertyName = default;
        Node name = default;
        var canParseAsKeyword = true;
        var disallowKeywords = (kind == Kind.ImportSpecifier);
        bool nameOk = default;
        (name, nameOk) = this.parseModuleExportName(disallowKeywords);
        if (((name.Kind == Kind.Identifier) && (name.TextEquals("type"u8))))
        {
            if ((this.token == Kind.AsKeyword))
            {
                var firstAs = this.parseIdentifierName();
                if ((this.token == Kind.AsKeyword))
                {
                    var secondAs = this.parseIdentifierName();
                    if (this.canParseModuleExportName())
                    {
                        isTypeOnly = true;
                        propertyName = firstAs;
                        (name, nameOk) = this.parseModuleExportName(disallowKeywords);
                        canParseAsKeyword = false;
                    }
                    else
                    {
                        propertyName = name;
                        name = secondAs;
                        canParseAsKeyword = false;
                    }
                }
                else
                {
                    if (this.canParseModuleExportName())
                    {
                        propertyName = name;
                        canParseAsKeyword = false;
                        (name, nameOk) = this.parseModuleExportName(disallowKeywords);
                    }
                    else
                    {
                        isTypeOnly = true;
                        name = firstAs;
                    }
                }
            }
            else
            {
                if (this.canParseModuleExportName())
                {
                    isTypeOnly = true;
                    (name, nameOk) = this.parseModuleExportName(disallowKeywords);
                }
            }
        }

        if ((canParseAsKeyword && (this.token == Kind.AsKeyword)))
        {
            propertyName = name;
            this.parseExpected(Kind.AsKeyword);
            (name, nameOk) = this.parseModuleExportName(disallowKeywords);
        }

        if (!nameOk)
        {
            this.parseErrorAtRange(this.skipRangeTrivia(name.Loc), Diagnostics.Identifier_expected);
        }

        return (isTypeOnly, propertyName, name);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2554:1.
    private bool canParseModuleExportName()
    {
        return (tokenIsIdentifierOrKeyword(this.token) || (this.token == Kind.StringLiteral));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2558:1.
    private (Node, bool) parseModuleExportName(bool disallowKeywords)
    {
        bool nameOk = default;
        nameOk = true;
        if ((this.token == Kind.StringLiteral))
        {
            return (this.parseLiteralExpression(), nameOk);
        }

        if (((disallowKeywords && Ast.IsKeyword(this.token)) && !this.isIdentifier()))
        {
            nameOk = false;
        }

        return (this.parseIdentifierName(), nameOk);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2570:1.
    private Node tryParseImportAttributes()
    {
        if (((this.token == Kind.WithKeyword) || (((this.token == Kind.AssertKeyword) && !this.hasPrecedingLineBreak()))))
        {
            if ((this.token == Kind.AssertKeyword))
            {
                this.parseErrorAtCurrentToken(Diagnostics.Import_assertions_have_been_replaced_by_import_attributes_Use_with_instead_of_assert);
            }

            return this.parseImportAttributes(this.token, false);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2580:1.
    private Node parseExportAssignment(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var saveContextFlags = this.contextFlags;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        this.setContextFlags(NodeFlags.AwaitContext, true);
        var isExportEquals = false;
        if (this.parseOptional(Kind.EqualsToken))
        {
            isExportEquals = true;
        }
        else
        {
            this.parseExpected(Kind.DefaultKeyword);
        }

        var expression = this.parseAssignmentExpressionOrHigher();
        this.parseSemicolon();
        this.contextFlags = saveContextFlags;
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        var result = this.finishNode(this.factory.NewExportAssignment(modifiers, isExportEquals, null, expression), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2600:1.
    private Node parseNamespaceExportDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        this.parseExpected(Kind.AsKeyword);
        this.parseExpected(Kind.NamespaceKeyword);
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var name = this.parseIdentifier();
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        this.parseSemicolon();
        var result = this.finishNode(this.factory.NewNamespaceExportDeclaration(modifiers, name), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2613:1.
    private Node parseExportDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var saveContextFlags = this.contextFlags;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        this.setContextFlags(NodeFlags.AwaitContext, true);
        Node exportClause = default;
        Node moduleSpecifier = default;
        Node attributes = default;
        var isTypeOnly = this.parseOptional(Kind.TypeKeyword);
        var namespaceExportPos = this.nodePos();
        if (this.parseOptional(Kind.AsteriskToken))
        {
            if (this.parseOptional(Kind.AsKeyword))
            {
                exportClause = this.parseNamespaceExport(namespaceExportPos);
            }

            this.parseExpected(Kind.FromKeyword);
            moduleSpecifier = this.parseModuleSpecifier();
        }
        else
        {
            exportClause = this.parseNamedExports();
            if (((this.token == Kind.FromKeyword) || (((this.token == Kind.StringLiteral) && !this.hasPrecedingLineBreak()))))
            {
                this.parseExpected(Kind.FromKeyword);
                moduleSpecifier = this.parseModuleSpecifier();
            }
        }

        if ((((moduleSpecifier != null) && (((this.token == Kind.WithKeyword) || (this.token == Kind.AssertKeyword)))) && !this.hasPrecedingLineBreak()))
        {
            if ((this.token == Kind.AssertKeyword))
            {
                this.parseErrorAtCurrentToken(Diagnostics.Import_assertions_have_been_replaced_by_import_attributes_Use_with_instead_of_assert);
            }

            attributes = this.parseImportAttributes(this.token, false);
        }

        this.parseSemicolon();
        this.contextFlags = saveContextFlags;
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        var result = this.finishNode(this.factory.NewExportDeclaration(modifiers, isTypeOnly, exportClause, moduleSpecifier, attributes), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2653:1.
    private Node parseNamespaceExport(int pos)
    {
        var tuple13 = this.parseModuleExportName(false);
        var exportName = tuple13.Item1;
        return this.finishNode(this.factory.NewNamespaceExport(exportName), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2658:1.
    private Node parseNamedExports()
    {
        var pos = this.nodePos();
        var exports = this.parseBracketedList(ParsingContext.ImportOrExportSpecifiers, (parserCallback) => parserCallback.parseExportSpecifier(), Kind.OpenBraceToken, Kind.CloseBraceToken);
        return this.finishNode(this.factory.NewNamedExports(exports), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2668:1.
    private Node parseExportSpecifier()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var tuple14 = this.parseImportOrExportSpecifier(Kind.ExportSpecifier);
        var isTypeOnly = tuple14.Item1;
        var propertyName = tuple14.Item2;
        var name = tuple14.Item3;
        var result = this.finishNode(this.factory.NewExportSpecifier(isTypeOnly, propertyName, name), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2680:1.
    private Node parseType()
    {
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.TypeExcludesFlags, false);
        Node typeNode = default;
        if (this.isStartOfFunctionTypeOrConstructorType())
        {
            typeNode = this.parseFunctionOrConstructorType();
        }
        else
        {
            var pos = this.nodePos();
            typeNode = this.parseUnionTypeOrHigher();
            if (((!this.inDisallowConditionalTypesContext() && !this.hasPrecedingLineBreak()) && this.parseOptional(Kind.ExtendsKeyword)))
            {
                var extendsType = this.doInContext(NodeFlags.DisallowConditionalTypesContext, true, (parserCallback) => parserCallback.parseType());
                this.parseExpected(Kind.QuestionToken);
                var trueType = this.doInContext(NodeFlags.DisallowConditionalTypesContext, false, (parserCallback) => parserCallback.parseType());
                this.parseExpected(Kind.ColonToken);
                var falseType = this.doInContext(NodeFlags.DisallowConditionalTypesContext, false, (parserCallback) => parserCallback.parseType());
                var conditionalType = this.factory.NewConditionalTypeNode(typeNode, extendsType, trueType, falseType);
                this.finishNode(conditionalType, pos);
                typeNode = conditionalType;
            }
        }

        this.contextFlags = saveContextFlags;
        return typeNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2705:1.
    private Node parseUnionTypeOrHigher()
    {
        return this.parseUnionOrIntersectionType(Kind.BarToken, (parserCallback) => parserCallback.parseIntersectionTypeOrHigher());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2709:1.
    private Node parseIntersectionTypeOrHigher()
    {
        return this.parseUnionOrIntersectionType(Kind.AmpersandToken, (parserCallback) => parserCallback.parseTypeOperatorOrHigher());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2713:1.
    private Node parseUnionOrIntersectionType(Kind @operator, Func<Parser, Node> parseConstituentType)
    {
        var pos = this.nodePos();
        var isUnionType = (@operator == Kind.BarToken);
        var hasLeadingOperator = this.parseOptional(@operator);
        Node typeNode = default;
        if (hasLeadingOperator)
        {
            typeNode = this.parseFunctionOrConstructorTypeToError(isUnionType, parseConstituentType);
        }
        else
        {
            typeNode = parseConstituentType(this);
        }

        if (((this.token == @operator) || hasLeadingOperator))
        {
            var types = Slice<Node>.Make(1);
            types[0] = typeNode;
            while (this.parseOptional(@operator))
            {
                types = GoRuntime.Append(types, this.parseFunctionOrConstructorTypeToError(isUnionType, parseConstituentType));
            }

            typeNode = this.createUnionOrIntersectionTypeNode(@operator, this.newNodeList(Core.NewTextRange(pos, this.nodePos()), this.nodeSliceArena.CloneHeader(types)));
            this.finishNode(typeNode, pos);
        }

        return typeNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2735:1.
    private Node createUnionOrIntersectionTypeNode(Kind @operator, NodeList types)
    {
        switch (@operator)
        {
            case Kind.BarToken:
            {
                return this.factory.NewUnionTypeNode(types);
            }

            case Kind.AmpersandToken:
            {
                return this.factory.NewIntersectionTypeNode(types);
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in createUnionOrIntersectionType");
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2746:1.
    private Node parseTypeOperatorOrHigher()
    {
        var @operator = this.token;
        switch (@operator)
        {
            case Kind.KeyOfKeyword:
            case Kind.UniqueKeyword:
            case Kind.ReadonlyKeyword:
            {
                return this.parseTypeOperator(@operator);
            }

            case Kind.InferKeyword:
            {
                return this.parseInferType();
            }
        }

        return this.doInContext(NodeFlags.DisallowConditionalTypesContext, false, (parserCallback) => parserCallback.parsePostfixTypeOrHigher());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2757:1.
    private Node parseTypeOperator(Kind @operator)
    {
        var pos = this.nodePos();
        this.parseExpected(@operator);
        return this.finishNode(this.factory.NewTypeOperatorNode(@operator, this.parseTypeOperatorOrHigher()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2763:1.
    private Node parseInferType()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.InferKeyword);
        return this.finishNode(this.factory.NewInferTypeNode(this.parseTypeParameterOfInferType()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2769:1.
    private Node parseTypeParameterOfInferType()
    {
        var pos = this.nodePos();
        var name = this.parseIdentifier();
        var constraint = this.tryParseConstraintOfInferType();
        return this.finishNode(this.factory.NewTypeParameterDeclaration(null, name, constraint, null, null), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2776:1.
    private Node tryParseConstraintOfInferType()
    {
        var state = this.mark();
        if (this.parseOptional(Kind.ExtendsKeyword))
        {
            var constraint = this.doInContext(NodeFlags.DisallowConditionalTypesContext, true, (parserCallback) => parserCallback.parseType());
            if ((this.inDisallowConditionalTypesContext() || (this.token != Kind.QuestionToken)))
            {
                return constraint;
            }
        }

        this.rewind(state);
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2788:1.
    private Node parsePostfixTypeOrHigher()
    {
        var pos = this.nodePos();
        var typeNode = this.parseNonArrayType();
        while (!this.hasPrecedingLineBreak())
        {
            switch (this.token)
            {
                case Kind.ExclamationToken:
                {
                    this.nextToken();
                    typeNode = this.finishNode(this.factory.NewJSDocNonNullableType(typeNode), pos);
                    break;
                }

                case Kind.QuestionToken:
                {
                    if (this.lookAhead((parserCallback) => parserCallback.nextIsStartOfType()))
                    {
                        return typeNode;
                    }

                    this.nextToken();
                    typeNode = this.finishNode(this.factory.NewJSDocNullableType(typeNode), pos);
                    break;
                }

                case Kind.OpenBracketToken:
                {
                    this.parseExpected(Kind.OpenBracketToken);
                    if (this.isStartOfType(false))
                    {
                        var indexType = this.parseType();
                        this.parseExpected(Kind.CloseBracketToken);
                        typeNode = this.finishNode(this.factory.NewIndexedAccessTypeNode(typeNode, indexType), pos);
                    }
                    else
                    {
                        this.parseExpected(Kind.CloseBracketToken);
                        typeNode = this.finishNode(this.factory.NewArrayTypeNode(typeNode), pos);
                    }

                    break;
                }

                default:
                {
                    return typeNode;
                }
            }
        }

        return typeNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2820:1.
    private bool nextIsStartOfType()
    {
        this.nextToken();
        return this.isStartOfType(false);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2825:1.
    private Node parseNonArrayType()
    {
        switch (this.token)
        {
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.StringKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.SymbolKeyword:
            case Kind.BooleanKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NeverKeyword:
            case Kind.ObjectKeyword:
            {
                var state = this.mark();
                var keywordTypeNode = this.parseKeywordTypeNode();
                if ((this.token != Kind.DotToken))
                {
                    return keywordTypeNode;
                }

                this.rewind(state);
                return this.parseTypeReference();
            }

            case Kind.AsteriskEqualsToken:
            {
                this.scanner.ReScanAsteriskEqualsToken();
                goto case Kind.AsteriskToken;
            }

            case Kind.AsteriskToken:
            {
                return this.parseJSDocAllType();
            }

            case Kind.QuestionQuestionToken:
            {
                this.scanner.ReScanQuestionToken();
                goto case Kind.QuestionToken;
            }

            case Kind.QuestionToken:
            {
                return this.parseJSDocNullableType();
            }

            case Kind.ExclamationToken:
            {
                return this.parseJSDocNonNullableType();
            }

            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
            {
                return this.parseLiteralTypeNode(false);
            }

            case Kind.MinusToken:
            {
                if (this.lookAhead((parserCallback) => parserCallback.nextTokenIsNumericOrBigIntLiteral()))
                {
                    return this.parseLiteralTypeNode(true);
                }

                return this.parseTypeReference();
            }

            case Kind.VoidKeyword:
            {
                return this.parseKeywordTypeNode();
            }

            case Kind.ThisKeyword:
            {
                var thisKeyword = this.parseThisTypeNode();
                if (((this.token == Kind.IsKeyword) && !this.hasPrecedingLineBreak()))
                {
                    return this.parseThisTypePredicate(thisKeyword);
                }

                return thisKeyword;
            }

            case Kind.TypeOfKeyword:
            {
                if (this.lookAhead((parserCallback) => parserCallback.nextIsStartOfTypeOfImportType()))
                {
                    return this.parseImportType();
                }

                return this.parseTypeQuery();
            }

            case Kind.OpenBraceToken:
            {
                if (this.lookAhead((parserCallback) => parserCallback.nextIsStartOfMappedType()))
                {
                    return this.parseMappedType();
                }

                return this.parseTypeLiteral();
            }

            case Kind.OpenBracketToken:
            {
                return this.parseTupleType();
            }

            case Kind.OpenParenToken:
            {
                return this.parseParenthesizedType();
            }

            case Kind.ImportKeyword:
            {
                return this.parseImportType();
            }

            case Kind.AssertsKeyword:
            {
                if (this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOnSameLine()))
                {
                    return this.parseAssertsTypePredicate();
                }

                return this.parseTypeReference();
            }

            case Kind.TemplateHead:
            {
                return this.parseTemplateType();
            }

            default:
            {
                return this.parseTypeReference();
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2895:1.
    private Node parseKeywordTypeNode()
    {
        var pos = this.nodePos();
        var result = this.factory.NewKeywordTypeNode(this.token);
        this.nextToken();
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2902:1.
    private Node parseThisTypeNode()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewThisTypeNode(), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2908:1.
    private Node parseThisTypePredicate(Node lhs)
    {
        this.nextToken();
        return this.finishNode(this.factory.NewTypePredicateNode(null, lhs, this.parseType()), lhs.Pos());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2913:1.
    private Node parseJSDocAllType()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewJSDocAllType(), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2919:1.
    private Node parseJSDocNonNullableType()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewJSDocNonNullableType(this.parseTypeOperatorOrHigher()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2925:1.
    private Node parseJSDocNullableType()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewJSDocNullableType(this.parseTypeOperatorOrHigher()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2932:1.
    private Node parseJSDocType()
    {
        this.scanner.SetSkipJSDocLeadingAsterisks(true);
        var pos = this.nodePos();
        var hasDotDotDot = this.parseOptional(Kind.DotDotDotToken);
        var t = this.parseTypeOrTypePredicate();
        this.scanner.SetSkipJSDocLeadingAsterisks(false);
        if (hasDotDotDot)
        {
            t = this.finishNode(this.factory.NewJSDocVariadicType(t), pos);
        }

        if ((this.token == Kind.EqualsToken))
        {
            this.nextToken();
            return this.finishNode(this.factory.NewJSDocOptionalType(t), pos);
        }

        return t;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2949:1.
    private Node parseLiteralTypeNode(bool negative)
    {
        var pos = this.nodePos();
        if (negative)
        {
            this.nextToken();
        }

        Node expression = default;
        if ((((this.token == Kind.TrueKeyword) || (this.token == Kind.FalseKeyword)) || (this.token == Kind.NullKeyword)))
        {
            expression = this.parseKeywordExpression();
        }
        else
        {
            expression = this.parseLiteralExpression();
        }

        if (negative)
        {
            expression = this.finishNode(this.factory.NewPrefixUnaryExpression(Kind.MinusToken, expression), pos);
        }

        return this.finishNode(this.factory.NewLiteralTypeNode(expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2966:1.
    private Node parseTypeReference()
    {
        var pos = this.nodePos();
        return this.finishNode(this.factory.NewTypeReferenceNode(this.parseEntityNameOfTypeReference(), this.parseTypeArgumentsOfTypeReference()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2971:1.
    private Node parseEntityNameOfTypeReference()
    {
        return this.parseEntityName(true, false, Diagnostics.Type_expected);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2975:1.
    private Node parseEntityName(bool allowReservedWords, bool allowPrivateName, Message diagnosticMessage)
    {
        var pos = this.nodePos();
        Node entity = default;
        if (allowReservedWords)
        {
            entity = this.parseIdentifierNameWithDiagnostic(diagnosticMessage);
        }
        else
        {
            entity = this.parseIdentifierWithDiagnostic(diagnosticMessage, null);
        }

        while (this.parseOptional(Kind.DotToken))
        {
            if ((this.token == Kind.LessThanToken))
            {
                break;
            }

            entity = this.finishNode(this.factory.NewQualifiedName(entity, this.parseRightSideOfDot(allowReservedWords, allowPrivateName, true)), pos);
        }

        return entity;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:2994:1.
    private Node parseRightSideOfDot(bool allowIdentifierNames, bool allowPrivateIdentifiers, bool allowUnicodeEscapeSequenceInIdentifierName)
    {
        if (((this.hasPrecedingLineBreak() && tokenIsIdentifierOrKeyword(this.token)) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOnSameLine())))
        {
            this.parseErrorAt(this.nodePos(), this.nodePos(), Diagnostics.Identifier_expected);
            return this.createMissingIdentifier();
        }

        if ((this.token == Kind.PrivateIdentifier))
        {
            var node = this.parsePrivateIdentifier();
            if (allowPrivateIdentifiers)
            {
                return node;
            }

            this.parseErrorAt(this.nodePos(), this.nodePos(), Diagnostics.Identifier_expected);
            return this.createMissingIdentifier();
        }

        if (allowIdentifierNames)
        {
            if (allowUnicodeEscapeSequenceInIdentifierName)
            {
                return this.parseIdentifierName();
            }

            return this.parseIdentifierNameErrorOnUnicodeEscapeSequence();
        }

        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var id = this.parseIdentifier();
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return id;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3041:1.
    private Node newIdentifier(string text) => newIdentifier(ByteStringValue.FromString(text));

    // parser.go:3041: keep a Go text header through AST publication.
    private Node newIdentifier(ByteStringValue text)
    {
        this.identifierCount++;
        var id = this.factory.NewIdentifierWithTextValue(text);
        if (text.EqualsBytes("await"u8))
            this.statementHasAwaitIdentifier = true;
        return id;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3050:1.
    private Node createMissingIdentifier()
    {
        return this.finishNode(this.newIdentifier(""), this.nodePos());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3054:1.
    private Node parsePrivateIdentifier()
    {
        var pos = this.nodePos();
        var text = this.scanner.TokenValueTextValue();
        this.nextToken();
        return this.finishNode(this.factory.NewPrivateIdentifierWithTextValue(text), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3061:1.
    private Kind reScanLessThanToken()
    {
        this.token = this.scanner.ReScanLessThanToken();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3066:1.
    private Kind reScanGreaterThanToken()
    {
        this.token = this.scanner.ReScanGreaterThanToken();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3071:1.
    private Kind reScanSlashToken()
    {
        this.token = this.scanner.ReScanSlashToken();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3076:1.
    private Kind reScanTemplateToken(bool isTaggedTemplate)
    {
        this.token = this.scanner.ReScanTemplateToken(isTaggedTemplate);
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3081:1.
    private NodeList parseTypeArgumentsOfTypeReference()
    {
        if ((!this.hasPrecedingLineBreak() && (this.reScanLessThanToken() == Kind.LessThanToken)))
        {
            return this.parseTypeArguments();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3088:1.
    private NodeList parseTypeArguments()
    {
        if ((this.token == Kind.LessThanToken))
        {
            return this.parseBracketedList(ParsingContext.TypeArguments, (parserCallback) => parserCallback.parseType(), Kind.LessThanToken, Kind.GreaterThanToken);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3095:1.
    private bool nextIsStartOfTypeOfImportType()
    {
        this.nextToken();
        return (this.token == Kind.ImportKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3100:1.
    private Node parseImportType()
    {
        this.sourceFlags |= NodeFlags.PossiblyContainsDynamicImport;
        var pos = this.nodePos();
        var isTypeOf = this.parseOptional(Kind.TypeOfKeyword);
        this.parseExpected(Kind.ImportKeyword);
        this.parseExpected(Kind.OpenParenToken);
        var typeNode = this.parseType();
        Node attributes = default;
        if (this.parseOptional(Kind.CommaToken))
        {
            var openBracePosition = this.scanner.TokenStart();
            this.parseExpected(Kind.OpenBraceToken);
            var currentToken = this.token;
            if (((currentToken == Kind.WithKeyword) || (currentToken == Kind.AssertKeyword)))
            {
                if ((currentToken == Kind.AssertKeyword))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Import_assertions_have_been_replaced_by_import_attributes_Use_with_instead_of_assert);
                }

                this.nextToken();
            }
            else
            {
                this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(Kind.WithKeyword));
            }

            this.parseExpected(Kind.ColonToken);
            attributes = this.parseImportAttributes(currentToken, true);
            this.parseOptional(Kind.CommaToken);
            if (!this.parseExpected(Kind.CloseBraceToken))
            {
                if ((GoRuntime.Len(this.diagnostics) != 0))
                {
                    var lastDiagnostic = this.diagnostics[(GoRuntime.Len(this.diagnostics) - 1)];
                    if ((lastDiagnostic.Code() == Diagnostics.X_0_expected.Code()))
                    {
                        var related = Ast.NewDiagnostic(null, Core.NewTextRange(openBracePosition, openBracePosition), Diagnostics.The_parser_expected_to_find_a_1_to_match_the_0_token_here, "{", "}");
                        lastDiagnostic.AddRelatedInfo(related);
                    }
                }
            }
        }

        this.parseExpected(Kind.CloseParenToken);
        Node qualifier = default;
        if (this.parseOptional(Kind.DotToken))
        {
            qualifier = this.parseEntityNameOfTypeReference();
        }

        var typeArguments = this.parseTypeArgumentsOfTypeReference();
        return this.finishNode(this.factory.NewImportTypeNode(isTypeOf, typeNode, attributes, qualifier, typeArguments), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3142:1.
    private Node parseImportAttribute()
    {
        var pos = this.nodePos();
        Node name = default;
        if (tokenIsIdentifierOrKeyword(this.token))
        {
            name = this.parseIdentifierName();
        }
        else
        {
            if ((this.token == Kind.StringLiteral))
            {
                name = this.parseLiteralExpression();
            }
        }

        if ((name != null))
        {
            this.parseExpected(Kind.ColonToken);
        }
        else
        {
            this.parseErrorAtCurrentToken(Diagnostics.Identifier_or_string_literal_expected);
        }

        var value = this.parseAssignmentExpressionOrHigher();
        return this.finishNode(this.factory.NewImportAttribute(name, value), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3159:1.
    private Node parseImportAttributes(Kind token, bool skipKeyword)
    {
        var pos = this.nodePos();
        if (!skipKeyword)
        {
            this.parseExpected(token);
        }

        NodeList elements = default;
        bool multiLine = default;
        var openBracePosition = this.scanner.TokenStart();
        if (this.parseExpected(Kind.OpenBraceToken))
        {
            multiLine = this.hasPrecedingLineBreak();
            elements = this.parseDelimitedList(ParsingContext.ImportAttributes, (parserCallback) => parserCallback.parseImportAttribute());
            if (!this.parseExpected(Kind.CloseBraceToken))
            {
                if ((GoRuntime.Len(this.diagnostics) != 0))
                {
                    var lastDiagnostic = this.diagnostics[(GoRuntime.Len(this.diagnostics) - 1)];
                    if ((lastDiagnostic.Code() == Diagnostics.X_0_expected.Code()))
                    {
                        var related = Ast.NewDiagnostic(null, Core.NewTextRange(openBracePosition, openBracePosition), Diagnostics.The_parser_expected_to_find_a_1_to_match_the_0_token_here, "{", "}");
                        lastDiagnostic.AddRelatedInfo(related);
                    }
                }
            }
        }
        else
        {
            elements = this.parseEmptyNodeList();
        }

        return this.finishNode(this.factory.NewImportAttributes(token, elements, multiLine), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3185:1.
    private Node parseTypeQuery()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.TypeOfKeyword);
        var entityName = this.parseEntityName(true, true, null);
        NodeList typeArguments = default;
        if (!this.hasPrecedingLineBreak())
        {
            typeArguments = this.parseTypeArguments();
        }

        return this.finishNode(this.factory.NewTypeQueryNode(entityName, typeArguments), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3197:1.
    private bool nextIsStartOfMappedType()
    {
        this.nextToken();
        if (((this.token == Kind.PlusToken) || (this.token == Kind.MinusToken)))
        {
            return (this.nextToken() == Kind.ReadonlyKeyword);
        }

        if ((this.token == Kind.ReadonlyKeyword))
        {
            this.nextToken();
        }

        return (((this.token == Kind.OpenBracketToken) && this.nextTokenIsIdentifier()) && (this.nextToken() == Kind.InKeyword));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3208:1.
    private Node parseMappedType()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.OpenBraceToken);
        Node readonlyToken = default;
        if ((((this.token == Kind.ReadonlyKeyword) || (this.token == Kind.PlusToken)) || (this.token == Kind.MinusToken)))
        {
            readonlyToken = this.parseTokenNode();
            if ((readonlyToken.Kind != Kind.ReadonlyKeyword))
            {
                this.parseExpected(Kind.ReadonlyKeyword);
            }
        }

        this.parseExpected(Kind.OpenBracketToken);
        var typeParameter = this.parseMappedTypeParameter();
        Node nameType = default;
        if (this.parseOptional(Kind.AsKeyword))
        {
            nameType = this.parseType();
        }

        this.parseExpected(Kind.CloseBracketToken);
        Node questionToken = default;
        if ((((this.token == Kind.QuestionToken) || (this.token == Kind.PlusToken)) || (this.token == Kind.MinusToken)))
        {
            questionToken = this.parseTokenNode();
            if ((questionToken.Kind != Kind.QuestionToken))
            {
                this.parseExpected(Kind.QuestionToken);
            }
        }

        var typeNode = this.parseTypeAnnotation();
        this.parseSemicolon();
        var members = this.parseList(ParsingContext.TypeMembers, (parserCallback) => parserCallback.parseTypeMember());
        this.parseExpected(Kind.CloseBraceToken);
        return this.finishNode(this.factory.NewMappedTypeNode(readonlyToken, typeParameter, nameType, questionToken, typeNode, members), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3239:1.
    private Node parseMappedTypeParameter()
    {
        var pos = this.nodePos();
        var name = this.parseIdentifierName();
        this.parseExpected(Kind.InKeyword);
        var typeNode = this.parseType();
        return this.finishNode(this.factory.NewTypeParameterDeclaration(null, name, typeNode, null, null), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3247:1.
    private Node parseTypeMember()
    {
        if (((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken)))
        {
            return this.parseSignatureMember(Kind.CallSignature);
        }

        if (((this.token == Kind.NewKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenParenOrLessThan())))
        {
            return this.parseSignatureMember(Kind.ConstructSignature);
        }

        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var modifiers = this.parseModifiers();
        if (this.parseContextualModifier(Kind.GetKeyword))
        {
            return this.parseAccessorDeclaration(pos, jsdoc, modifiers, Kind.GetAccessor, ParseFlags.Type);
        }

        if (this.parseContextualModifier(Kind.SetKeyword))
        {
            return this.parseAccessorDeclaration(pos, jsdoc, modifiers, Kind.SetAccessor, ParseFlags.Type);
        }

        if (this.isIndexSignature())
        {
            return this.parseIndexSignatureDeclaration(pos, jsdoc, modifiers);
        }

        return this.parsePropertyOrMethodSignature(pos, jsdoc, modifiers);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3269:1.
    private bool nextTokenIsOpenParenOrLessThan()
    {
        this.nextToken();
        return ((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3274:1.
    private Node parseSignatureMember(Kind kind)
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        if ((kind == Kind.ConstructSignature))
        {
            this.parseExpected(Kind.NewKeyword);
        }

        var typeParameters = this.parseTypeParameters();
        var parameters = this.parseParameters(ParseFlags.Type);
        var typeNode = this.parseReturnType(Kind.ColonToken, true);
        this.parseTypeMemberSemicolon();
        Node result = default;
        if ((kind == Kind.CallSignature))
        {
            result = this.factory.NewCallSignatureDeclaration(typeParameters, parameters, typeNode);
        }
        else
        {
            result = this.factory.NewConstructSignatureDeclaration(typeParameters, parameters, typeNode);
        }

        this.finishNode(result, pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3295:1.
    private NodeList parseTypeParameters()
    {
        if ((this.token == Kind.LessThanToken))
        {
            return this.parseBracketedList(ParsingContext.TypeParameters, (parserCallback) => parserCallback.parseTypeParameter(), Kind.LessThanToken, Kind.GreaterThanToken);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3302:1.
    private Node parseTypeParameter()
    {
        var pos = this.nodePos();
        var modifiers = this.parseModifiersEx(false, true, false);
        var name = this.parseIdentifier();
        Node constraint = default;
        Node expression = default;
        if (this.parseOptional(Kind.ExtendsKeyword))
        {
            if ((this.isStartOfType(false) || !this.isStartOfExpression()))
            {
                constraint = this.parseType();
            }
            else
            {
                expression = this.parseUnaryExpressionOrHigher();
            }
        }

        Node defaultType = default;
        if (this.parseOptional(Kind.EqualsToken))
        {
            defaultType = this.parseType();
        }

        var result = this.factory.NewTypeParameterDeclaration(modifiers, name, constraint, expression, defaultType);
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3334:1.
    private NodeList parseParameters(ParseFlags flags)
    {
        if (this.parseExpected(Kind.OpenParenToken))
        {
            var parameters = this.parseParametersWorker(flags, true);
            this.parseExpected(Kind.CloseParenToken);
            return parameters;
        }

        return this.createMissingList();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3356:1.
    private NodeList parseParametersWorker(ParseFlags flags, bool allowAmbiguity)
    {
        var inAwaitContext = ((this.contextFlags & NodeFlags.AwaitContext) != 0);
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.YieldContext, ((flags & ParseFlags.Yield) != 0));
        this.setContextFlags(NodeFlags.AwaitContext, ((flags & ParseFlags.Await) != 0));
        var parameters = this.parseDelimitedList(ParsingContext.Parameters, (inAwaitContext, allowAmbiguity, flags), static (p, state) =>
        {
            var parameter = p.parseParameterEx(state.inAwaitContext, state.allowAmbiguity);
            if (((parameter != null) && ((state.flags & ParseFlags.Type) == 0)))
            {
                p.checkJSSyntax(parameter);
            }

            return parameter;
        });
        this.contextFlags = saveContextFlags;
        return parameters;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3385:1.
    private Node parseParameter()
    {
        return this.parseParameterEx(false, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3389:1.
    private Node parseParameterEx(bool inOuterAwaitContext, bool allowAmbiguity)
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.AwaitContext, inOuterAwaitContext);
        var modifiers = this.parseModifiersEx(true, false, false);
        this.contextFlags = saveContextFlags;
        if ((this.token == Kind.ThisKeyword))
        {
            var result = this.factory.NewParameterDeclaration(modifiers, null, this.createIdentifier(true), null, this.parseTypeAnnotation(), null);
            if ((modifiers != null))
            {
                this.parseErrorAtRange(modifiers.Nodes[0].Loc, Diagnostics.Neither_decorators_nor_modifiers_may_be_applied_to_this_parameters);
            }

            this.withJSDoc(this.finishNode(result, pos), jsdoc);
            return result;
        }

        var dotDotDotToken = this.parseOptionalToken(Kind.DotDotDotToken);
        if ((!allowAmbiguity && !this.isParameterNameStart()))
        {
            return null;
        }

        var result2 = this.factory.NewParameterDeclaration(modifiers, dotDotDotToken, this.parseNameOfParameter(modifiers), this.parseOptionalToken(Kind.QuestionToken), this.parseTypeAnnotation(), this.parseInitializer());
        this.withJSDoc(this.finishNode(result2, pos), jsdoc);
        return result2;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3430:1.
    private bool isParameterNameStart()
    {
        return ((this.isBindingIdentifier() || (this.token == Kind.OpenBracketToken)) || (this.token == Kind.OpenBraceToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3437:1.
    private Node parseNameOfParameter(ModifierList modifiers)
    {
        var name = this.parseIdentifierOrPatternWithDiagnostic(Diagnostics.Private_identifiers_cannot_be_used_as_parameters);
        if ((((name.Loc.Len() == 0) && (modifiers == null)) && Ast.IsModifierKind(this.token)))
        {
            this.nextToken();
        }

        return name;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3455:1.
    private Node parseReturnType(Kind returnToken, bool isType)
    {
        if (this.shouldParseReturnType(returnToken, isType))
        {
            return this.doInContext(NodeFlags.DisallowConditionalTypesContext, false, (parserCallback) => parserCallback.parseTypeOrTypePredicate());
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3462:1.
    private bool shouldParseReturnType(Kind returnToken, bool isType)
    {
        if ((returnToken == Kind.EqualsGreaterThanToken))
        {
            this.parseExpected(returnToken);
            return true;
        }
        else
        {
            if (this.parseOptional(Kind.ColonToken))
            {
                return true;
            }
            else
            {
                if ((isType && (this.token == Kind.EqualsGreaterThanToken)))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(Kind.ColonToken));
                    this.nextToken();
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3477:1.
    private Node parseTypeOrTypePredicate()
    {
        if (this.isIdentifier())
        {
            var state = this.mark();
            var pos = this.nodePos();
            var id = this.parseIdentifier();
            if (((this.token == Kind.IsKeyword) && !this.hasPrecedingLineBreak()))
            {
                this.nextToken();
                return this.finishNode(this.factory.NewTypePredicateNode(null, id, this.parseType()), pos);
            }

            this.rewind(state);
        }

        return this.parseType();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3491:1.
    private void parseTypeMemberSemicolon()
    {
        if (this.parseOptional(Kind.CommaToken))
        {
            return;
        }

        this.parseSemicolon();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3501:1.
    private Node parseAccessorDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers, Kind kind, ParseFlags flags)
    {
        var name = this.parsePropertyName();
        var typeParameters = this.parseTypeParameters();
        var parameters = this.parseParameters(ParseFlags.None);
        var returnType = this.parseReturnType(Kind.ColonToken, false);
        var body = this.parseFunctionBlockOrSemicolon(flags, null);
        Node result = default;
        if ((kind == Kind.GetAccessor))
        {
            result = this.factory.NewGetAccessorDeclaration(modifiers, name, typeParameters, parameters, returnType, null, body);
        }
        else
        {
            result = this.factory.NewSetAccessorDeclaration(modifiers, name, typeParameters, parameters, returnType, null, body);
        }

        this.withJSDoc(this.finishNode(result, pos), jsdoc);
        if (((flags & ParseFlags.Type) == 0))
        {
            this.checkJSSyntax(result);
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3521:1.
    private Node parsePropertyName()
    {
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var prop = this.parsePropertyNameWorker(true);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return prop;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3528:1.
    private Node parsePropertyNameWorker(bool allowComputedPropertyNames)
    {
        if ((((this.token == Kind.StringLiteral) || (this.token == Kind.NumericLiteral)) || (this.token == Kind.BigIntLiteral)))
        {
            return this.parseLiteralExpression();
        }

        if ((allowComputedPropertyNames && (this.token == Kind.OpenBracketToken)))
        {
            return this.parseComputedPropertyName();
        }

        if ((this.token == Kind.PrivateIdentifier))
        {
            return this.parsePrivateIdentifier();
        }

        return this.parseIdentifierName();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3541:1.
    private Node parseComputedPropertyName()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.OpenBracketToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpected(Kind.CloseBracketToken);
        return this.finishNode(this.factory.NewComputedPropertyName(expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3555:1.
    private Node parseFunctionBlockOrSemicolon(ParseFlags flags, Message diagnosticMessage)
    {
        if ((this.token != Kind.OpenBraceToken))
        {
            if (((flags & ParseFlags.Type) != 0))
            {
                this.parseTypeMemberSemicolon();
                return null;
            }

            if (this.canParseSemicolon())
            {
                this.parseSemicolon();
                return null;
            }
        }

        return this.parseFunctionBlock(flags, diagnosticMessage);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3569:1.
    private Node parseFunctionBlock(ParseFlags flags, Message diagnosticMessage)
    {
        var saveContextFlags = this.contextFlags;
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        this.setContextFlags(NodeFlags.YieldContext, ((flags & ParseFlags.Yield) != 0));
        this.setContextFlags(NodeFlags.AwaitContext, ((flags & ParseFlags.Await) != 0));
        this.setContextFlags(NodeFlags.DecoratorContext, false);
        var block = this.parseBlock(((flags & ParseFlags.IgnoreMissingOpenBrace) != 0), diagnosticMessage);
        this.contextFlags = saveContextFlags;
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return block;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3583:1.
    private bool isIndexSignature()
    {
        return ((this.token == Kind.OpenBracketToken) && this.lookAhead((parserCallback) => parserCallback.nextIsUnambiguouslyIndexSignature()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3587:1.
    private bool nextIsUnambiguouslyIndexSignature()
    {
        this.nextToken();
        if (((this.token == Kind.DotDotDotToken) || (this.token == Kind.CloseBracketToken)))
        {
            return true;
        }

        if (Ast.IsModifierKind(this.token))
        {
            this.nextToken();
            if (this.isIdentifier())
            {
                return true;
            }
        }
        else
        {
            if (!this.isIdentifier())
            {
                return false;
            }
            else
            {
                this.nextToken();
            }
        }

        if (((this.token == Kind.ColonToken) || (this.token == Kind.CommaToken)))
        {
            return true;
        }

        if ((this.token != Kind.QuestionToken))
        {
            return false;
        }

        this.nextToken();
        return (((this.token == Kind.ColonToken) || (this.token == Kind.CommaToken)) || (this.token == Kind.CloseBracketToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3636:1.
    private Node parseIndexSignatureDeclaration(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var parameters = this.parseBracketedList(ParsingContext.Parameters, (parserCallback) => parserCallback.parseParameter(), Kind.OpenBracketToken, Kind.CloseBracketToken);
        var typeNode = this.parseTypeAnnotation();
        this.parseTypeMemberSemicolon();
        var result = this.finishNode(this.factory.NewIndexSignatureDeclaration(modifiers, parameters, typeNode), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3645:1.
    private Node parsePropertyOrMethodSignature(int pos, jsdocScannerInfo jsdoc, ModifierList modifiers)
    {
        var name = this.parsePropertyName();
        var questionToken = this.parseOptionalToken(Kind.QuestionToken);
        Node result = default;
        if (((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken)))
        {
            var typeParameters = this.parseTypeParameters();
            var parameters = this.parseParameters(ParseFlags.Type);
            var returnType = this.parseReturnType(Kind.ColonToken, true);
            result = this.factory.NewMethodSignatureDeclaration(modifiers, name, questionToken, typeParameters, parameters, returnType);
        }
        else
        {
            var typeNode = this.parseTypeAnnotation();
            Node initializer = default;
            if ((this.token == Kind.EqualsToken))
            {
                initializer = this.parseInitializer();
            }

            result = this.factory.NewPropertySignatureDeclaration(modifiers, name, questionToken, typeNode, initializer);
        }

        this.parseTypeMemberSemicolon();
        this.withJSDoc(this.finishNode(result, pos), jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3672:1.
    private Node parseTypeLiteral()
    {
        var pos = this.nodePos();
        var result = this.finishNode(this.factory.NewTypeLiteralNode(this.parseObjectTypeMembers()), pos);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3678:1.
    private NodeList parseObjectTypeMembers()
    {
        if (this.parseExpected(Kind.OpenBraceToken))
        {
            var members = this.parseList(ParsingContext.TypeMembers, (parserCallback) => parserCallback.parseTypeMember());
            this.parseExpected(Kind.CloseBraceToken);
            return members;
        }

        return this.createMissingList();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3687:1.
    private Node parseTupleType()
    {
        var pos = this.nodePos();
        return this.finishNode(this.factory.NewTupleTypeNode(this.parseBracketedList(ParsingContext.TupleElementTypes, (parserCallback) => parserCallback.parseTupleElementNameOrTupleElementType(), Kind.OpenBracketToken, Kind.CloseBracketToken)), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3692:1.
    private Node parseTupleElementNameOrTupleElementType()
    {
        if (this.lookAhead((parserCallback) => parserCallback.scanStartOfNamedTupleElement()))
        {
            var pos = this.nodePos();
            var jsdoc = this.jsdocScannerInfo();
            var dotDotDotToken = this.parseOptionalToken(Kind.DotDotDotToken);
            var name = this.parseIdentifierName();
            var questionToken = this.parseOptionalToken(Kind.QuestionToken);
            this.parseExpected(Kind.ColonToken);
            var typeNode = this.parseTupleElementType();
            var result = this.finishNode(this.factory.NewNamedTupleMember(dotDotDotToken, name, questionToken, typeNode), pos);
            this.withJSDoc(result, jsdoc);
            return result;
        }

        return this.parseTupleElementType();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3708:1.
    private bool scanStartOfNamedTupleElement()
    {
        if ((this.token == Kind.DotDotDotToken))
        {
            return (tokenIsIdentifierOrKeyword(this.nextToken()) && this.nextTokenIsColonOrQuestionColon());
        }

        return (tokenIsIdentifierOrKeyword(this.token) && this.nextTokenIsColonOrQuestionColon());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3715:1.
    private bool nextTokenIsColonOrQuestionColon()
    {
        return ((this.nextToken() == Kind.ColonToken) || ((this.token == Kind.QuestionToken) && (this.nextToken() == Kind.ColonToken)));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3719:1.
    private Node parseTupleElementType()
    {
        var pos = this.nodePos();
        if (this.parseOptional(Kind.DotDotDotToken))
        {
            return this.finishNode(this.factory.NewRestTypeNode(this.parseType()), pos);
        }

        var typeNode = this.parseType();
        if ((Ast.IsJSDocNullableType(typeNode) && (typeNode.Pos() == typeNode.Type().Pos())))
        {
            var node = this.factory.NewOptionalTypeNode(typeNode.Type());
            node.Flags = typeNode.Flags;
            node.Loc = typeNode.Loc;
            typeNode.Type().Parent = node;
            return node;
        }

        return typeNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3735:1.
    private Node parseParenthesizedType()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.OpenParenToken);
        var typeNode = this.parseType();
        this.parseExpected(Kind.CloseParenToken);
        return this.finishNode(this.factory.NewParenthesizedTypeNode(typeNode), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3743:1.
    private Node parseAssertsTypePredicate()
    {
        var pos = this.nodePos();
        var assertsModifier = this.parseExpectedToken(Kind.AssertsKeyword);
        Node parameterName = default;
        if ((this.token == Kind.ThisKeyword))
        {
            parameterName = this.parseThisTypeNode();
        }
        else
        {
            parameterName = this.parseIdentifier();
        }

        Node typeNode = default;
        if (this.parseOptional(Kind.IsKeyword))
        {
            typeNode = this.parseType();
        }

        return this.finishNode(this.factory.NewTypePredicateNode(assertsModifier, parameterName, typeNode), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3759:1.
    private Node parseTemplateType()
    {
        var pos = this.nodePos();
        return this.finishNode(this.factory.NewTemplateLiteralTypeNode(this.parseTemplateHead(false), this.parseTemplateTypeSpans()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3764:1.
    private Node parseTemplateHead(bool isTaggedTemplate)
    {
        if ((!isTaggedTemplate && ((this.scanner.TokenFlags() & TokenFlags.IsInvalid) != 0)))
        {
            this.reScanTemplateToken(false);
        }

        var pos = this.nodePos();
        var result = this.factory.NewTemplateHeadWithTextValue(this.scanner.TokenValueTextValue(), this.getTemplateLiteralRawText(2), this.scanner.TokenFlags());
        this.nextToken();
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3774:1.
    private ByteStringValue getTemplateLiteralRawText(int endLength)
    {
        // parser.go:3774. Retain the borrowed raw template header in its AST payload.
        var tokenText = this.scanner.TokenTextView();
        if (((this.scanner.TokenFlags() & TokenFlags.Unterminated) != 0))
        {
            endLength = 0;
        }

        return ByteStringValue.FromView(GoRuntime.Slice(tokenText, 1, (GoRuntime.Len(tokenText) - endLength)));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3782:1.
    private NodeList parseTemplateTypeSpans()
    {
        var pos = this.nodePos();
        Slice<Node> list = default;
        while (true)
        {
            var span = this.parseTemplateTypeSpan();
            list = GoRuntime.Append(list, span);
            if ((span.AsTemplateLiteralTypeSpan().Literal.Kind != Kind.TemplateMiddle))
            {
                break;
            }
        }

        return this.newNodeList(Core.NewTextRange(pos, this.nodePos()), list);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3795:1.
    private Node parseTemplateTypeSpan()
    {
        var pos = this.nodePos();
        return this.finishNode(this.factory.NewTemplateLiteralTypeSpan(this.parseType(), this.parseLiteralOfTemplateSpan(false)), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3800:1.
    private Node parseLiteralOfTemplateSpan(bool isTaggedTemplate)
    {
        if ((this.token == Kind.CloseBraceToken))
        {
            this.reScanTemplateToken(isTaggedTemplate);
            return this.parseTemplateMiddleOrTail();
        }

        this.parseErrorAtCurrentToken(Diagnostics.X_0_expected, Scanner.TokenToString(Kind.CloseBraceToken));
        return this.finishNode(this.factory.NewTemplateTail("", "", TokenFlags.None), this.nodePos());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3809:1.
    private Node parseTemplateMiddleOrTail()
    {
        var pos = this.nodePos();
        Node result = default;
        if ((this.token == Kind.TemplateMiddle))
        {
            result = this.factory.NewTemplateMiddleWithTextValue(this.scanner.TokenValueTextValue(), this.getTemplateLiteralRawText(2), this.scanner.TokenFlags());
        }
        else
        {
            result = this.factory.NewTemplateTailWithTextValue(this.scanner.TokenValueTextValue(), this.getTemplateLiteralRawText(1), this.scanner.TokenFlags());
        }

        this.nextToken();
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3821:1.
    private Node parseFunctionOrConstructorTypeToError(bool isInUnionType, Func<Parser, Node> parseConstituentType)
    {
        if (this.isStartOfFunctionTypeOrConstructorType())
        {
            var typeNode = this.parseFunctionOrConstructorType();
            Message diagnostic = default;
            if ((typeNode.Kind == Kind.FunctionType))
            {
                diagnostic = Core.IfElse(isInUnionType, Diagnostics.Function_type_notation_must_be_parenthesized_when_used_in_a_union_type, Diagnostics.Function_type_notation_must_be_parenthesized_when_used_in_an_intersection_type);
            }
            else
            {
                diagnostic = Core.IfElse(isInUnionType, Diagnostics.Constructor_type_notation_must_be_parenthesized_when_used_in_a_union_type, Diagnostics.Constructor_type_notation_must_be_parenthesized_when_used_in_an_intersection_type);
            }

            this.parseErrorAtRange(typeNode.Loc, diagnostic);
            return typeNode;
        }

        return parseConstituentType(this);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3843:1.
    private bool isStartOfFunctionTypeOrConstructorType()
    {
        return ((((this.token == Kind.LessThanToken) || ((this.token == Kind.OpenParenToken) && this.lookAhead((parserCallback) => parserCallback.nextIsUnambiguouslyStartOfFunctionType()))) || (this.token == Kind.NewKeyword)) || ((this.token == Kind.AbstractKeyword) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsNewKeyword())));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3850:1.
    private Node parseFunctionOrConstructorType()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var modifiers = this.parseModifiersForConstructorType();
        var isConstructorType = this.parseOptional(Kind.NewKeyword);
        Debug.Assert(((modifiers == null) || isConstructorType), "Per isStartOfFunctionOrConstructorType, a function type cannot have modifiers.");
        var typeParameters = this.parseTypeParameters();
        var parameters = this.parseParameters(ParseFlags.Type);
        var returnType = this.parseReturnType(Kind.EqualsGreaterThanToken, false);
        Node result = default;
        if (isConstructorType)
        {
            result = this.factory.NewConstructorTypeNode(modifiers, typeParameters, parameters, returnType);
        }
        else
        {
            result = this.factory.NewFunctionTypeNode(typeParameters, parameters, returnType);
        }

        this.finishNode(result, pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3870:1.
    private ModifierList parseModifiersForConstructorType()
    {
        if ((this.token == Kind.AbstractKeyword))
        {
            var pos = this.nodePos();
            var modifier = this.factory.NewModifier(this.token);
            this.nextToken();
            this.finishNode(modifier, pos);
            return this.newModifierList(modifier.Loc, this.nodeSliceArena.NewSlice1Header(modifier));
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3881:1.
    private bool nextTokenIsNewKeyword()
    {
        return (this.nextToken() == Kind.NewKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3885:1.
    private bool nextIsUnambiguouslyStartOfFunctionType()
    {
        this.nextToken();
        if (((this.token == Kind.CloseParenToken) || (this.token == Kind.DotDotDotToken)))
        {
            return true;
        }

        if (this.skipParameterStart())
        {
            if (((((this.token == Kind.ColonToken) || (this.token == Kind.CommaToken)) || (this.token == Kind.QuestionToken)) || (this.token == Kind.EqualsToken)))
            {
                return true;
            }

            if (((this.token == Kind.CloseParenToken) && (this.nextToken() == Kind.EqualsGreaterThanToken)))
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3910:1.
    private bool skipParameterStart()
    {
        if (Ast.IsModifierKind(this.token))
        {
            this.parseModifiers();
        }

        this.parseOptional(Kind.DotDotDotToken);
        if ((this.isIdentifier() || (this.token == Kind.ThisKeyword)))
        {
            this.nextToken();
            return true;
        }

        if (((this.token == Kind.OpenBracketToken) || (this.token == Kind.OpenBraceToken)))
        {
            var previousErrorCount = GoRuntime.Len(this.diagnostics);
            this.parseIdentifierOrPattern();
            return (previousErrorCount == GoRuntime.Len(this.diagnostics));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3929:1.
    private ModifierList parseModifiers()
    {
        return this.parseModifiersEx(false, false, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3933:1.
    private ModifierList parseModifiersEx(bool allowDecorators, bool permitConstAsModifier, bool stopOnStartOfClassStaticBlock)
    {
        bool hasLeadingModifier = default;
        bool hasTrailingDecorator = default;
        bool hasTrailingModifier = default;
        bool hasStaticModifier = default;
        var pos = this.nodePos();
        var list = Slice<Node>.Make(0);
        while (true)
        {
            if (((allowDecorators && (this.token == Kind.AtToken)) && !hasTrailingModifier))
            {
                var decorator = this.parseDecorator();
                list = GoRuntime.Append(list, decorator);
                if (hasLeadingModifier)
                {
                    hasTrailingDecorator = true;
                }
            }
            else
            {
                var modifier = this.tryParseModifier(hasStaticModifier, permitConstAsModifier, stopOnStartOfClassStaticBlock);
                if ((modifier == null))
                {
                    break;
                }

                if ((modifier.Kind == Kind.StaticKeyword))
                {
                    hasStaticModifier = true;
                }

                list = GoRuntime.Append(list, modifier);
                if (hasTrailingDecorator)
                {
                    hasTrailingModifier = true;
                }
                else
                {
                    hasLeadingModifier = true;
                }
            }
        }

        if ((GoRuntime.Len(list) != 0))
        {
            return this.newModifierList(Core.NewTextRange(pos, this.nodePos()), this.nodeSliceArena.CloneHeader(list));
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3973:1.
    private Node parseDecorator()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.AtToken);
        var expression = this.doInContext(NodeFlags.DecoratorContext, true, (parserCallback) => parserCallback.parseDecoratorExpression());
        return this.finishNode(this.factory.NewDecorator(expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3980:1.
    private Node parseDecoratorExpression()
    {
        if ((this.inAwaitContext() && (this.token == Kind.AwaitKeyword)))
        {
            var pos = this.nodePos();
            var awaitExpression = this.parseIdentifierWithDiagnostic(Diagnostics.Expression_expected, null);
            this.nextToken();
            var memberExpression = this.parseMemberExpressionRest(pos, awaitExpression, true);
            return this.parseCallExpressionRest(pos, memberExpression);
        }

        return this.parseLeftHandSideExpressionOrHigher();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:3993:1.
    private Node tryParseModifier(bool hasSeenStaticModifier, bool permitConstAsModifier, bool stopOnStartOfClassStaticBlock)
    {
        var pos = this.nodePos();
        var kind = this.token;
        if (((this.token == Kind.ConstKeyword) && permitConstAsModifier))
        {
            if (!this.lookAhead((parserCallback) => parserCallback.nextTokenIsOnSameLineAndCanFollowModifier()))
            {
                return null;
            }
            else
            {
                this.nextToken();
            }
        }
        else
        {
            if (((stopOnStartOfClassStaticBlock && (this.token == Kind.StaticKeyword)) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenBrace())))
            {
                return null;
            }
            else
            {
                if ((hasSeenStaticModifier && (this.token == Kind.StaticKeyword)))
                {
                    return null;
                }
                else
                {
                    if (!this.parseAnyContextualModifier())
                    {
                        return null;
                    }
                }
            }
        }

        return this.finishNode(this.factory.NewModifier(kind), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4016:1.
    private bool parseContextualModifier(Kind t)
    {
        var state = this.mark();
        if (((this.token == t) && this.nextTokenCanFollowModifier()))
        {
            return true;
        }

        this.rewind(state);
        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4025:1.
    private bool parseAnyContextualModifier()
    {
        var state = this.mark();
        if ((Ast.IsModifierKind(this.token) && this.nextTokenCanFollowModifier()))
        {
            return true;
        }

        this.rewind(state);
        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4034:1.
    private bool nextTokenCanFollowModifier()
    {
        switch (this.token)
        {
            case Kind.ConstKeyword:
            {
                return (this.nextToken() == Kind.EnumKeyword);
            }

            case Kind.ExportKeyword:
            {
                this.nextToken();
                if ((this.token == Kind.DefaultKeyword))
                {
                    return this.lookAhead((parserCallback) => parserCallback.nextTokenCanFollowDefaultKeyword());
                }

                if ((this.token == Kind.TypeKeyword))
                {
                    return this.lookAhead((parserCallback) => parserCallback.nextTokenCanFollowExportModifier());
                }

                return this.canFollowExportModifier();
            }

            case Kind.DefaultKeyword:
            {
                return this.nextTokenCanFollowDefaultKeyword();
            }

            case Kind.StaticKeyword:
            {
                this.nextToken();
                return this.canFollowModifier();
            }

            case Kind.GetKeyword:
            case Kind.SetKeyword:
            {
                this.nextToken();
                return this.canFollowGetOrSetKeyword();
            }

            default:
            {
                return this.nextTokenIsOnSameLineAndCanFollowModifier();
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4061:1.
    private bool nextTokenCanFollowDefaultKeyword()
    {
        switch (this.nextToken())
        {
            case Kind.ClassKeyword:
            case Kind.FunctionKeyword:
            case Kind.InterfaceKeyword:
            case Kind.AtToken:
            {
                return true;
            }

            case Kind.AbstractKeyword:
            {
                return this.lookAhead((parserCallback) => parserCallback.nextTokenIsClassKeywordOnSameLine());
            }

            case Kind.AsyncKeyword:
            {
                return this.lookAhead((parserCallback) => parserCallback.nextTokenIsFunctionKeywordOnSameLine());
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4073:1.
    private bool nextTokenIsIdentifierOrKeyword()
    {
        return tokenIsIdentifierOrKeyword(this.nextToken());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4077:1.
    private bool nextTokenIsIdentifierOrKeywordOrGreaterThan()
    {
        return tokenIsIdentifierOrKeywordOrGreaterThan(this.nextToken());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4081:1.
    private bool nextTokenIsIdentifierOrKeywordOnSameLine()
    {
        return (this.nextTokenIsIdentifierOrKeyword() && !this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4085:1.
    private bool nextTokenIsIdentifierOrKeywordOrLiteralOnSameLine()
    {
        return (((((this.nextTokenIsIdentifierOrKeyword() || (this.token == Kind.NumericLiteral)) || (this.token == Kind.BigIntLiteral)) || (this.token == Kind.StringLiteral))) && !this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4089:1.
    private bool nextTokenIsClassKeywordOnSameLine()
    {
        return ((this.nextToken() == Kind.ClassKeyword) && !this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4093:1.
    private bool nextTokenIsFunctionKeywordOnSameLine()
    {
        return ((this.nextToken() == Kind.FunctionKeyword) && !this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4097:1.
    private bool nextTokenCanFollowExportModifier()
    {
        this.nextToken();
        return this.canFollowExportModifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4102:1.
    private bool canFollowExportModifier()
    {
        return ((this.token == Kind.AtToken) || ((((this.token != Kind.AsteriskToken) && (this.token != Kind.AsKeyword)) && (this.token != Kind.OpenBraceToken)) && this.canFollowModifier()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4106:1.
    private bool canFollowModifier()
    {
        return (((((this.token == Kind.OpenBracketToken) || (this.token == Kind.OpenBraceToken)) || (this.token == Kind.AsteriskToken)) || (this.token == Kind.DotDotDotToken)) || this.isLiteralPropertyName());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4110:1.
    private bool canFollowGetOrSetKeyword()
    {
        return ((this.token == Kind.OpenBracketToken) || this.isLiteralPropertyName());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4114:1.
    private bool nextTokenIsOnSameLineAndCanFollowModifier()
    {
        this.nextToken();
        if (this.hasPrecedingLineBreak())
        {
            return false;
        }

        return this.canFollowModifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4122:1.
    private bool nextTokenIsOpenBrace()
    {
        return (this.nextToken() == Kind.OpenBraceToken);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4126:1.
    private Node parseExpression()
    {
        var saveContextFlags = this.contextFlags;
        this.contextFlags &= ~(NodeFlags.DecoratorContext);
        var pos = this.nodePos();
        var expr = this.parseAssignmentExpressionOrHigher();
        while (true)
        {
            var operatorToken = this.parseOptionalToken(Kind.CommaToken);
            if ((operatorToken == null))
            {
                break;
            }

            expr = this.makeBinaryExpression(expr, operatorToken, this.parseAssignmentExpressionOrHigher(), pos);
        }

        this.contextFlags = saveContextFlags;
        return expr;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4147:1.
    private Node parseExpressionAllowIn()
    {
        return this.doInContext(NodeFlags.DisallowInContext, false, (parserCallback) => parserCallback.parseExpression());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4151:1.
    private Node parseAssignmentExpressionOrHigher()
    {
        return this.parseAssignmentExpressionOrHigherWorker(true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4155:1.
    private Node parseAssignmentExpressionOrHigherWorker(bool allowReturnTypeInArrowFunction)
    {
        if (this.isYieldExpression())
        {
            return this.parseYieldExpression();
        }

        var arrowExpression = this.tryParseParenthesizedArrowFunctionExpression(allowReturnTypeInArrowFunction);
        if ((arrowExpression != null))
        {
            return arrowExpression;
        }

        arrowExpression = this.tryParseAsyncSimpleArrowFunctionExpression(allowReturnTypeInArrowFunction);
        if ((arrowExpression != null))
        {
            return arrowExpression;
        }

        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var expr = this.parseBinaryExpressionOrHigher(OperatorPrecedence.Lowest);
        if (((expr.Kind == Kind.Identifier) && (this.token == Kind.EqualsGreaterThanToken)))
        {
            return this.parseSimpleArrowFunctionExpression(pos, expr, allowReturnTypeInArrowFunction, jsdoc, null);
        }

        if ((Ast.IsLeftHandSideExpression(expr) && Ast.IsAssignmentOperator(this.reScanGreaterThanToken())))
        {
            return this.makeBinaryExpression(expr, this.parseTokenNode(), this.parseAssignmentExpressionOrHigherWorker(allowReturnTypeInArrowFunction), pos);
        }

        return this.parseConditionalExpressionRest(expr, pos, allowReturnTypeInArrowFunction);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4224:1.
    private bool isYieldExpression()
    {
        if ((this.token == Kind.YieldKeyword))
        {
            if (this.inYieldContext())
            {
                return true;
            }

            return this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOrLiteralOnSameLine());
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4251:1.
    private Node parseYieldExpression()
    {
        var pos = this.nodePos();
        this.nextToken();
        Node result = default;
        if ((!this.hasPrecedingLineBreak() && (((this.token == Kind.AsteriskToken) || this.isStartOfExpression()))))
        {
            result = this.factory.NewYieldExpression(this.parseOptionalToken(Kind.AsteriskToken), this.parseAssignmentExpressionOrHigher());
        }
        else
        {
            result = this.factory.NewYieldExpression(null, null);
        }

        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4269:1.
    private Tristate isParenthesizedArrowFunctionExpression()
    {
        if ((((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken)) || (this.token == Kind.AsyncKeyword)))
        {
            var state = this.mark();
            var result = this.nextIsParenthesizedArrowFunctionExpression();
            this.rewind(state);
            return result;
        }

        if ((this.token == Kind.EqualsGreaterThanToken))
        {
            return Tristate.True;
        }

        return Tristate.False;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4286:1.
    private Tristate nextIsParenthesizedArrowFunctionExpression()
    {
        if ((this.token == Kind.AsyncKeyword))
        {
            this.nextToken();
            if (this.hasPrecedingLineBreak())
            {
                return Tristate.False;
            }

            if (((this.token != Kind.OpenParenToken) && (this.token != Kind.LessThanToken)))
            {
                return Tristate.False;
            }
        }

        var first = this.token;
        var second = this.nextToken();
        if ((first == Kind.OpenParenToken))
        {
            if ((second == Kind.CloseParenToken))
            {
                var third = this.nextToken();
                switch (third)
                {
                    case Kind.EqualsGreaterThanToken:
                    case Kind.ColonToken:
                    case Kind.OpenBraceToken:
                    {
                        return Tristate.True;
                    }
                }

                return Tristate.False;
            }

            if (((second == Kind.OpenBracketToken) || (second == Kind.OpenBraceToken)))
            {
                return Tristate.Unknown;
            }

            if ((second == Kind.DotDotDotToken))
            {
                return Tristate.True;
            }

            if (((Ast.IsModifierKind(second) && (second != Kind.AsyncKeyword)) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifier())))
            {
                if ((this.nextToken() == Kind.AsKeyword))
                {
                    return Tristate.False;
                }

                return Tristate.True;
            }

            if ((!this.isIdentifier() && (second != Kind.ThisKeyword)))
            {
                return Tristate.False;
            }

            switch (this.nextToken())
            {
                case Kind.ColonToken:
                {
                    return Tristate.True;
                }

                case Kind.QuestionToken:
                {
                    this.nextToken();
                    if (((((this.token == Kind.ColonToken) || (this.token == Kind.CommaToken)) || (this.token == Kind.EqualsToken)) || (this.token == Kind.CloseParenToken)))
                    {
                        return Tristate.True;
                    }

                    return Tristate.False;
                }

                case Kind.CommaToken:
                case Kind.EqualsToken:
                case Kind.CloseParenToken:
                {
                    return Tristate.Unknown;
                }
            }

            return Tristate.False;
        }
        else
        {
            Debug.Assert((first == Kind.LessThanToken));
            if ((!this.isIdentifier() && (this.token != Kind.ConstKeyword)))
            {
                return Tristate.False;
            }

            if ((this.languageVariant == LanguageVariant.JSX))
            {
                var isArrowFunctionInJsx = this.lookAhead((p) =>
                {
                    p.parseOptional(Kind.ConstKeyword);
                    var third2 = p.nextToken();
                    if ((third2 == Kind.ExtendsKeyword))
                    {
                        var fourth = p.nextToken();
                        switch (fourth)
                        {
                            case Kind.EqualsToken:
                            case Kind.GreaterThanToken:
                            case Kind.SlashToken:
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        if (((third2 == Kind.CommaToken) || (third2 == Kind.EqualsToken)))
                        {
                            return true;
                        }
                    }

                    return false;
                });
                if (isArrowFunctionInJsx)
                {
                    return Tristate.True;
                }

                return Tristate.False;
            }

            return Tristate.Unknown;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4394:1.
    private Node tryParseParenthesizedArrowFunctionExpression(bool allowReturnTypeInArrowFunction)
    {
        var tristate = this.isParenthesizedArrowFunctionExpression();
        if ((tristate == Tristate.False))
        {
            return null;
        }

        if ((tristate == Tristate.True))
        {
            return this.parseParenthesizedArrowFunctionExpression(true, true);
        }

        var state = this.mark();
        var result = this.parsePossibleParenthesizedArrowFunctionExpression(allowReturnTypeInArrowFunction);
        if ((result == null))
        {
            this.rewind(state);
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4415:1.
    private Node parseParenthesizedArrowFunctionExpression(bool allowAmbiguity, bool allowReturnTypeInArrowFunction)
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var modifiers = this.parseModifiersForArrowFunction();
        var isAsync = modifierListHasAsync(modifiers);
        var signatureFlags = Core.IfElse(isAsync, ParseFlags.Await, ParseFlags.None);
        var typeParameters = this.parseTypeParameters();
        NodeList parameters = default;
        if (!this.parseExpected(Kind.OpenParenToken))
        {
            if (!allowAmbiguity)
            {
                return null;
            }

            parameters = this.createMissingList();
        }
        else
        {
            if (!allowAmbiguity)
            {
                var maybeParameters = this.parseParametersWorker(signatureFlags, allowAmbiguity);
                if ((maybeParameters == null))
                {
                    return null;
                }

                parameters = maybeParameters;
            }
            else
            {
                parameters = this.parseParametersWorker(signatureFlags, allowAmbiguity);
            }

            if ((!this.parseExpected(Kind.CloseParenToken) && !allowAmbiguity))
            {
                return null;
            }
        }

        var hasReturnColon = (this.token == Kind.ColonToken);
        var returnType = this.parseReturnType(Kind.ColonToken, false);
        if ((((returnType != null) && !allowAmbiguity) && typeHasArrowFunctionBlockingParseError(returnType)))
        {
            return null;
        }

        var unwrappedType = returnType;
        while (((unwrappedType != null) && (unwrappedType.Kind == Kind.ParenthesizedType)))
        {
            unwrappedType = unwrappedType.Type();
        }

        if (((!allowAmbiguity && (this.token != Kind.EqualsGreaterThanToken)) && (this.token != Kind.OpenBraceToken)))
        {
            return null;
        }

        var lastToken = this.token;
        var equalsGreaterThanToken = this.parseExpectedToken(Kind.EqualsGreaterThanToken);
        Node body = default;
        if (((lastToken == Kind.EqualsGreaterThanToken) || (lastToken == Kind.OpenBraceToken)))
        {
            body = this.parseArrowFunctionExpressionBody(isAsync, allowReturnTypeInArrowFunction);
        }
        else
        {
            body = this.parseIdentifier();
        }

        if ((!allowReturnTypeInArrowFunction && hasReturnColon))
        {
            if ((this.token != Kind.ColonToken))
            {
                return null;
            }
        }

        var result = this.finishNode(this.factory.NewArrowFunction(modifiers, typeParameters, parameters, returnType, null, equalsGreaterThanToken, body), pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4513:1.
    private ModifierList parseModifiersForArrowFunction()
    {
        if ((this.token == Kind.AsyncKeyword))
        {
            var pos = this.nodePos();
            this.nextToken();
            var modifier = this.finishNode(this.factory.NewModifier(Kind.AsyncKeyword), pos);
            return this.newModifierList(modifier.Loc, this.nodeSliceArena.NewSlice1Header(modifier));
        }

        return null;
    }

    // If true, we should abort parsing an error function.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4524:1.
    private static bool typeHasArrowFunctionBlockingParseError(Node node)
    {
        switch (node.Kind)
        {
            case Kind.TypeReference:
            {
                return Ast.NodeIsMissing(node.AsTypeReferenceNode().TypeName);
            }

            case Kind.FunctionType:
            case Kind.ConstructorType:
            {
                return (isMissingNodeList(node.FunctionLikeData().Parameters) || typeHasArrowFunctionBlockingParseError(node.Type()));
            }

            case Kind.ParenthesizedType:
            {
                return typeHasArrowFunctionBlockingParseError(node.Type());
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4536:1.
    private Node parseArrowFunctionExpressionBody(bool isAsync, bool allowReturnTypeInArrowFunction)
    {
        if ((this.token == Kind.OpenBraceToken))
        {
            return this.parseFunctionBlock(Core.IfElse(isAsync, ParseFlags.Await, ParseFlags.None), null);
        }

        if ((((((this.token != Kind.SemicolonToken) && (this.token != Kind.FunctionKeyword)) && (this.token != Kind.ClassKeyword)) && this.isStartOfStatement()) && !this.isStartOfExpressionStatement()))
        {
            return this.parseFunctionBlock((ParseFlags.IgnoreMissingOpenBrace | Core.IfElse(isAsync, ParseFlags.Await, ParseFlags.None)), null);
        }

        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.AwaitContext, isAsync);
        this.setContextFlags(NodeFlags.YieldContext, false);
        var node = this.parseAssignmentExpressionOrHigherWorker(allowReturnTypeInArrowFunction);
        this.contextFlags = saveContextFlags;
        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4565:1.
    private bool isStartOfExpressionStatement()
    {
        return (((((this.token != Kind.OpenBraceToken) && (this.token != Kind.FunctionKeyword)) && (this.token != Kind.ClassKeyword)) && (this.token != Kind.AtToken)) && this.isStartOfExpression());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4570:1.
    private Node parsePossibleParenthesizedArrowFunctionExpression(bool allowReturnTypeInArrowFunction)
    {
        var tokenPos = this.scanner.TokenStart();
        if (this.notParenthesizedArrow?.Contains(tokenPos) == true)
        {
            return null;
        }

        var result = this.parseParenthesizedArrowFunctionExpression(false, allowReturnTypeInArrowFunction);
        if ((result == null))
        {
            (this.notParenthesizedArrow ??= new()).Add(tokenPos);
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4582:1.
    private Node tryParseAsyncSimpleArrowFunctionExpression(bool allowReturnTypeInArrowFunction)
    {
        if (((this.token == Kind.AsyncKeyword) && this.lookAhead((parserCallback) => parserCallback.nextIsUnParenthesizedAsyncArrowFunction())))
        {
            var pos = this.nodePos();
            var jsdoc = this.jsdocScannerInfo();
            var asyncModifier = this.parseModifiersForArrowFunction();
            var expr = this.parseBinaryExpressionOrHigher(OperatorPrecedence.Lowest);
            return this.parseSimpleArrowFunctionExpression(pos, expr, allowReturnTypeInArrowFunction, jsdoc, asyncModifier);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4594:1.
    private bool nextIsUnParenthesizedAsyncArrowFunction()
    {
        if ((this.token == Kind.AsyncKeyword))
        {
            this.nextToken();
            if ((this.hasPrecedingLineBreak() || (this.token == Kind.EqualsGreaterThanToken)))
            {
                return false;
            }

            if (!this.isIdentifier())
            {
                return false;
            }

            this.nextTokenWithoutCheck();
            return (!this.hasPrecedingLineBreak() && (this.token == Kind.EqualsGreaterThanToken));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4615:1.
    private Node parseSimpleArrowFunctionExpression(int pos, Node identifier, bool allowReturnTypeInArrowFunction, jsdocScannerInfo jsdoc, ModifierList asyncModifier)
    {
        Debug.Assert((this.token == Kind.EqualsGreaterThanToken), "parseSimpleArrowFunctionExpression should only have been called if we had a =>");
        var parameter = this.finishNode(this.factory.NewParameterDeclaration(null, null, identifier, null, null, null), identifier.Pos());
        var parameters = this.newNodeList(parameter.Loc, this.nodeSliceArena.NewSlice1Header(parameter));
        var equalsGreaterThanToken = this.parseExpectedToken(Kind.EqualsGreaterThanToken);
        var body = this.parseArrowFunctionExpressionBody((asyncModifier != null), allowReturnTypeInArrowFunction);
        var result = this.finishNode(this.factory.NewArrowFunction(asyncModifier, null, parameters, null, null, equalsGreaterThanToken, body), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4626:1.
    private Node parseConditionalExpressionRest(Node leftOperand, int pos, bool allowReturnTypeInArrowFunction)
    {
        var questionToken = this.parseOptionalToken(Kind.QuestionToken);
        if ((questionToken == null))
        {
            return leftOperand;
        }

        var saveContextFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.DisallowInContext, false);
        var trueExpression = this.parseAssignmentExpressionOrHigherWorker(false);
        this.contextFlags = saveContextFlags;
        var colonToken = this.parseExpectedToken(Kind.ColonToken);
        Node falseExpression = default;
        if (Ast.NodeIsPresent(colonToken))
        {
            falseExpression = this.parseAssignmentExpressionOrHigherWorker(allowReturnTypeInArrowFunction);
        }
        else
        {
            falseExpression = this.createMissingIdentifier();
        }

        return this.finishNode(this.factory.NewConditionalExpression(leftOperand, questionToken, trueExpression, colonToken, falseExpression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4648:1.
    private Node parseBinaryExpressionOrHigher(OperatorPrecedence precedence)
    {
        var pos = this.nodePos();
        var leftOperand = this.parseUnaryExpressionOrHigher();
        return this.parseBinaryExpressionRest(precedence, leftOperand, pos);
    }

    // shouldConsumeBinaryOperator reports whether an operator binds before the operator represented by currentPrecedence.
    // At equal precedence, only the right-associative exponentiation operator binds first.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4656:1.
    private static bool shouldConsumeBinaryOperator(Kind @operator, OperatorPrecedence operatorPrecedence, OperatorPrecedence currentPrecedence)
    {
        if ((operatorPrecedence > currentPrecedence))
        {
            return true;
        }

        return ((operatorPrecedence == currentPrecedence) && (@operator == Kind.AsteriskAsteriskToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4663:1.
    private Node parseBinaryExpressionRest(OperatorPrecedence precedence, Node leftOperand, int pos)
    {
        var lastOperand = leftOperand;
        while (true)
        {
            var @operator = this.reScanGreaterThanToken();
            var newPrecedence = Ast.GetBinaryOperatorPrecedence(@operator);
            if (!shouldConsumeBinaryOperator(@operator, newPrecedence, precedence))
            {
                break;
            }

            if (((@operator == Kind.InKeyword) && this.inDisallowInContext()))
            {
                break;
            }

            if (((@operator == Kind.AsKeyword) || (@operator == Kind.SatisfiesKeyword)))
            {
                if (this.hasPrecedingLineBreak())
                {
                    break;
                }
                else
                {
                    this.nextToken();
                    var lastPrecedence = OperatorPrecedence.Highest;
                    if (Ast.IsBinaryExpression(lastOperand))
                    {
                        lastPrecedence = Ast.GetBinaryOperatorPrecedence(lastOperand.AsBinaryExpression().OperatorToken.Kind);
                    }

                    if ((@operator == Kind.SatisfiesKeyword))
                    {
                        leftOperand = this.makeSatisfiesExpression(leftOperand, this.parseType());
                    }
                    else
                    {
                        leftOperand = this.makeAsExpression(leftOperand, this.parseType());
                    }

                    var nextOperator = this.reScanGreaterThanToken();
                    var nextPrecedence = Ast.GetBinaryOperatorPrecedence(nextOperator);
                    if (shouldConsumeBinaryOperator(nextOperator, nextPrecedence, lastPrecedence))
                    {
                        break;
                    }
                }
            }
            else
            {
                leftOperand = this.makeBinaryExpression(leftOperand, this.parseTokenNode(), this.parseBinaryExpressionOrHigher(newPrecedence), pos);
                lastOperand = leftOperand;
            }
        }

        return leftOperand;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4734:1.
    private Node makeSatisfiesExpression(Node expression, Node typeNode)
    {
        return this.checkJSSyntax(this.finishNode(this.factory.NewSatisfiesExpression(expression, typeNode), expression.Pos()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4738:1.
    private Node makeAsExpression(Node left, Node right)
    {
        return this.checkJSSyntax(this.finishNode(this.factory.NewAsExpression(left, right), left.Pos()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4742:1.
    private Node makeBinaryExpression(Node left, Node operatorToken, Node right, int pos)
    {
        return this.finishNode(this.factory.NewBinaryExpression(null, left, null, operatorToken, right), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4746:1.
    private Node parseUnaryExpressionOrHigher()
    {
        if (this.isUpdateExpression())
        {
            var pos = this.nodePos();
            var updateExpression = this.parseUpdateExpression();
            if ((this.token == Kind.AsteriskAsteriskToken))
            {
                return this.parseBinaryExpressionRest(Ast.GetBinaryOperatorPrecedence(this.token), updateExpression, pos);
            }

            return updateExpression;
        }

        var unaryOperator = this.token;
        var simpleUnaryExpression = this.parseSimpleUnaryExpression();
        if ((this.token == Kind.AsteriskAsteriskToken))
        {
            var pos2 = Scanner.SkipTriviaByteString(this.sourceText, simpleUnaryExpression.Pos());
            var end = simpleUnaryExpression.End();
            if ((simpleUnaryExpression.Kind == Kind.TypeAssertionExpression))
            {
                this.parseErrorAt(pos2, end, Diagnostics.A_type_assertion_expression_is_not_allowed_in_the_left_hand_side_of_an_exponentiation_expression_Consider_enclosing_the_expression_in_parentheses);
            }
            else
            {
                Debug.Assert(isKeywordOrPunctuation(unaryOperator));
                this.parseErrorAt(pos2, end, Diagnostics.An_unary_expression_with_the_0_operator_is_not_allowed_in_the_left_hand_side_of_an_exponentiation_expression_Consider_enclosing_the_expression_in_parentheses, Scanner.TokenToString(unaryOperator));
            }
        }

        return simpleUnaryExpression;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4785:1.
    private bool isUpdateExpression()
    {
        switch (this.token)
        {
            case Kind.PlusToken:
            case Kind.MinusToken:
            case Kind.TildeToken:
            case Kind.ExclamationToken:
            case Kind.DeleteKeyword:
            case Kind.TypeOfKeyword:
            case Kind.VoidKeyword:
            case Kind.AwaitKeyword:
            {
                return false;
            }

            case Kind.LessThanToken:
            {
                return (this.languageVariant == LanguageVariant.JSX);
            }
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4795:1.
    private Node parseUpdateExpression()
    {
        var pos = this.nodePos();
        if (((this.token == Kind.PlusPlusToken) || (this.token == Kind.MinusMinusToken)))
        {
            var @operator = this.token;
            this.nextToken();
            return this.finishNode(this.factory.NewPrefixUnaryExpression(@operator, this.parseLeftHandSideExpressionOrHigher()), pos);
        }
        else
        {
            if ((((this.languageVariant == LanguageVariant.JSX) && (this.token == Kind.LessThanToken)) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOrGreaterThan())))
            {
                return this.parseJsxElementOrSelfClosingElementOrFragment(true, -1, null, false);
            }
        }

        var expression = this.parseLeftHandSideExpressionOrHigher();
        if (((((this.token == Kind.PlusPlusToken) || (this.token == Kind.MinusMinusToken))) && !this.hasPrecedingLineBreak()))
        {
            var operator2 = this.token;
            this.nextToken();
            return this.finishNode(this.factory.NewPostfixUnaryExpression(expression, operator2), pos);
        }

        return expression;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4814:1.
    private Node parseJsxElementOrSelfClosingElementOrFragment(bool inExpressionContext, int topInvalidNodePosition, Node openingTag, bool mustBeUnary)
    {
        var pos = this.nodePos();
        var opening = this.parseJsxOpeningOrSelfClosingElementOrOpeningFragment(inExpressionContext);
        Node result = default;
        switch (opening.Kind)
        {
            case Kind.JsxOpeningElement:
            {
                var children = this.parseJsxChildren(opening);
                Node closingElement = default;
                var lastChild = Core.LastOrNil(children.Nodes);
                if (((((lastChild != null) && (lastChild.Kind == Kind.JsxElement)) && !Ast.TagNamesAreEquivalent(lastChild.AsJsxElement().OpeningElement.TagName(), lastChild.AsJsxElement().ClosingElement.TagName())) && Ast.TagNamesAreEquivalent(opening.TagName(), lastChild.AsJsxElement().ClosingElement.TagName())))
                {
                    var end = lastChild.Children().End();
                    var missingIdentifier = this.finishNodeWithEnd(this.newIdentifier(""), end, end);
                    var newClosingElement = this.finishNodeWithEnd(this.factory.NewJsxClosingElement(missingIdentifier), end, end);
                    var newLast = this.finishNodeWithEnd(this.factory.NewJsxElement(lastChild.AsJsxElement().OpeningElement, lastChild.Children(), newClosingElement), lastChild.AsJsxElement().OpeningElement.Pos(), end);
                    if ((lastChild.AsJsxElement().OpeningElement != null))
                    {
                        lastChild.AsJsxElement().OpeningElement.Parent = newLast;
                    }

                    if ((lastChild.Children() != null))
                    {
                        {
                            foreach (var c in GoRuntime.RangeValues(lastChild.Children().Nodes))
                            {
                                c.Parent = newLast;
                            }
                        }
                    }

                    newClosingElement.Parent = newLast;
                    children = this.newNodeList(Core.NewTextRange(children.Pos(), newLast.End()), GoRuntime.Append(GoRuntime.Slice(children.Nodes, 0, (GoRuntime.Len(children.Nodes) - 1)), newLast));
                    closingElement = lastChild.AsJsxElement().ClosingElement;
                }
                else
                {
                    closingElement = this.parseJsxClosingElement(opening, inExpressionContext);
                    if (!Ast.TagNamesAreEquivalent(opening.TagName(), closingElement.TagName()))
                    {
                        if ((((openingTag != null) && Ast.IsJsxOpeningElement(openingTag)) && Ast.TagNamesAreEquivalent(closingElement.TagName(), openingTag.TagName())))
                        {
                            this.parseErrorAtRange(opening.TagName().Loc, Diagnostics.JSX_element_0_has_no_corresponding_closing_tag, Scanner.GetTextOfNodeFromSourceTextByteString(this.sourceText, opening.TagName(), false));
                        }
                        else
                        {
                            this.parseErrorAtRange(closingElement.TagName().Loc, Diagnostics.Expected_corresponding_JSX_closing_tag_for_0, Scanner.GetTextOfNodeFromSourceTextByteString(this.sourceText, opening.TagName(), false));
                        }
                    }
                }

                result = this.finishNode(this.factory.NewJsxElement(opening, children, closingElement), pos);
                closingElement.Parent = result;
                break;
            }

            case Kind.JsxOpeningFragment:
            {
                result = this.finishNode(this.factory.NewJsxFragment(opening, this.parseJsxChildren(opening), this.parseJsxClosingFragment(inExpressionContext)), pos);
                break;
            }

            case Kind.JsxSelfClosingElement:
            {
                result = opening;
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in parseJsxElementOrSelfClosingElementOrFragment");
            }
        }

        if (((!mustBeUnary && inExpressionContext) && (this.token == Kind.LessThanToken)))
        {
            var topBadPos = topInvalidNodePosition;
            if ((topBadPos < 0))
            {
                topBadPos = result.Pos();
            }

            var invalidElement = this.parseJsxElementOrSelfClosingElementOrFragment(true, topBadPos, null, false);
            var operatorToken = this.factory.NewToken(Kind.CommaToken);
            operatorToken.Loc = Core.NewTextRange(invalidElement.Pos(), invalidElement.Pos());
            this.parseErrorAt(Scanner.SkipTriviaByteString(this.sourceText, topBadPos), invalidElement.End(), Diagnostics.JSX_expressions_must_have_one_parent_element);
            result = this.finishNode(this.factory.NewBinaryExpression(null, result, null, operatorToken, invalidElement), pos);
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4894:1.
    private NodeList parseJsxChildren(Node openingTag)
    {
        var pos = this.nodePos();
        var saveParsingContexts = this.parsingContexts;
        this.parsingContexts |= (1 << (int)(ParsingContext.JsxChildren));
        Slice<Node> list = default;
        while (true)
        {
            var currentToken = this.scanner.ReScanJsxToken(true);
            var child = this.parseJsxChild(openingTag, currentToken);
            if ((child == null))
            {
                break;
            }

            list = GoRuntime.Append(list, child);
            if ((((Ast.IsJsxOpeningElement(openingTag) && (child.Kind == Kind.JsxElement)) && !Ast.TagNamesAreEquivalent(child.AsJsxElement().OpeningElement.TagName(), child.AsJsxElement().ClosingElement.TagName())) && Ast.TagNamesAreEquivalent(openingTag.TagName(), child.AsJsxElement().ClosingElement.TagName())))
            {
                break;
            }
        }

        this.parsingContexts = saveParsingContexts;
        return this.newNodeList(Core.NewTextRange(pos, this.nodePos()), list);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4917:1.
    private Node parseJsxChild(Node openingTag, Kind token)
    {
        switch (token)
        {
            case Kind.EndOfFile:
            {
                if (Ast.IsJsxOpeningFragment(openingTag))
                {
                    this.parseErrorAtRange(openingTag.Loc, Diagnostics.JSX_fragment_has_no_corresponding_closing_tag);
                }
                else
                {
                    var tag = openingTag.TagName();
                    var start = Math.Min(Scanner.SkipTriviaByteString(this.sourceText, tag.Pos()), tag.End());
                    this.parseErrorAt(start, tag.End(), Diagnostics.JSX_element_0_has_no_corresponding_closing_tag, Scanner.GetTextOfNodeFromSourceTextByteString(this.sourceText, openingTag.TagName(), false));
                }

                return null;
            }

            case Kind.LessThanSlashToken:
            case Kind.ConflictMarkerTrivia:
            {
                return null;
            }

            case Kind.JsxText:
            case Kind.JsxTextAllWhiteSpaces:
            {
                return this.parseJsxText();
            }

            case Kind.OpenBraceToken:
            {
                return this.parseJsxExpression(false);
            }

            case Kind.LessThanToken:
            {
                return this.parseJsxElementOrSelfClosingElementOrFragment(false, -1, openingTag, false);
            }
        }

        throw new InvalidOperationException("Unhandled case in parseJsxChild");
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4945:1.
    private Node parseJsxText()
    {
        var pos = this.nodePos();
        var result = this.factory.NewJsxTextWithTextValue(this.scanner.TokenValueTextValue(), (this.token == Kind.JsxTextAllWhiteSpaces));
        this.scanJsxText();
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4952:1.
    private Node parseJsxExpression(bool inExpressionContext)
    {
        var pos = this.nodePos();
        if (!this.parseExpected(Kind.OpenBraceToken))
        {
            return null;
        }

        Node dotDotDotToken = default;
        Node expression = default;
        if ((this.token != Kind.CloseBraceToken))
        {
            if (!inExpressionContext)
            {
                dotDotDotToken = this.parseOptionalToken(Kind.DotDotDotToken);
            }

            expression = this.parseExpression();
        }

        if (inExpressionContext)
        {
            this.parseExpected(Kind.CloseBraceToken);
        }
        else
        {
            if (this.parseExpectedWithoutAdvancing(Kind.CloseBraceToken))
            {
                this.scanJsxText();
            }
        }

        return this.finishNode(this.factory.NewJsxExpression(dotDotDotToken, expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4976:1.
    private Kind scanJsxText()
    {
        this.token = this.scanner.ScanJsxToken();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4981:1.
    private Kind scanJsxIdentifier()
    {
        this.token = this.scanner.ScanJsxIdentifier();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4986:1.
    private Kind scanJsxAttributeValue()
    {
        this.token = this.scanner.ScanJsxAttributeValue();
        return this.token;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:4991:1.
    private Node parseJsxClosingElement(Node open, bool inExpressionContext)
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.LessThanSlashToken);
        var tagName = this.parseJsxElementName();
        if (this.parseExpectedWithDiagnostic(Kind.GreaterThanToken, null, false))
        {
            if ((inExpressionContext || !Ast.TagNamesAreEquivalent(open.TagName(), tagName)))
            {
                this.nextToken();
            }
            else
            {
                this.scanJsxText();
            }
        }

        return this.finishNode(this.factory.NewJsxClosingElement(tagName), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5006:1.
    private Node parseJsxOpeningOrSelfClosingElementOrOpeningFragment(bool inExpressionContext)
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.LessThanToken);
        if ((this.token == Kind.GreaterThanToken))
        {
            this.scanJsxText();
            return this.finishNode(this.factory.NewJsxOpeningFragment(), pos);
        }

        var tagName = this.parseJsxElementName();
        NodeList typeArguments = default;
        if (((this.contextFlags & NodeFlags.JavaScriptFile) == 0))
        {
            typeArguments = this.parseTypeArguments();
        }

        var attributes = this.parseJsxAttributes();
        Node result = default;
        if ((this.token == Kind.GreaterThanToken))
        {
            this.scanJsxText();
            result = this.factory.NewJsxOpeningElement(tagName, typeArguments, attributes);
        }
        else
        {
            this.parseExpected(Kind.SlashToken);
            if (this.parseExpectedWithoutAdvancing(Kind.GreaterThanToken))
            {
                if (inExpressionContext)
                {
                    this.nextToken();
                }
                else
                {
                    this.scanJsxText();
                }
            }

            result = this.factory.NewJsxSelfClosingElement(tagName, typeArguments, attributes);
        }

        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5041:1.
    private Node parseJsxElementName()
    {
        var pos = this.nodePos();
        var initialExpression = this.parseJsxTagName();
        if (Ast.IsJsxNamespacedName(initialExpression))
        {
            return initialExpression;
        }

        var expression = initialExpression;
        while (this.parseOptional(Kind.DotToken))
        {
            expression = this.finishNode(this.factory.NewPropertyAccessExpression(expression, null, this.parseRightSideOfDot(true, false, false), NodeFlags.None), pos);
        }

        return expression;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5059:1.
    private Node parseJsxTagName()
    {
        var pos = this.nodePos();
        this.scanJsxIdentifier();
        var isThis = (this.token == Kind.ThisKeyword);
        var tagName = this.parseIdentifierNameErrorOnUnicodeEscapeSequence();
        if (this.parseOptional(Kind.ColonToken))
        {
            this.scanJsxIdentifier();
            return this.finishNode(this.factory.NewJsxNamespacedName(tagName, this.parseIdentifierNameErrorOnUnicodeEscapeSequence()), pos);
        }

        if (isThis)
        {
            var result = this.factory.NewKeywordExpression(Kind.ThisKeyword);
            return this.finishNode(result, pos);
        }

        return tagName;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5075:1.
    private Node parseJsxAttributes()
    {
        var pos = this.nodePos();
        return this.finishNode(this.factory.NewJsxAttributes(this.parseList(ParsingContext.JsxAttributes, (parserCallback) => parserCallback.parseJsxAttribute())), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5080:1.
    private Node parseJsxAttribute()
    {
        if ((this.token == Kind.OpenBraceToken))
        {
            return this.parseJsxSpreadAttribute();
        }

        var pos = this.nodePos();
        return this.finishNode(this.factory.NewJsxAttribute(this.parseJsxAttributeName(), this.parseJsxAttributeValue()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5088:1.
    private Node parseJsxSpreadAttribute()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.OpenBraceToken);
        this.parseExpected(Kind.DotDotDotToken);
        var expression = this.parseExpression();
        this.parseExpected(Kind.CloseBraceToken);
        return this.finishNode(this.factory.NewJsxSpreadAttribute(expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5097:1.
    private Node parseJsxAttributeName()
    {
        var pos = this.nodePos();
        this.scanJsxIdentifier();
        var attrName = this.parseIdentifierNameErrorOnUnicodeEscapeSequence();
        if (this.parseOptional(Kind.ColonToken))
        {
            this.scanJsxIdentifier();
            return this.finishNode(this.factory.NewJsxNamespacedName(attrName, this.parseIdentifierNameErrorOnUnicodeEscapeSequence()), pos);
        }

        return attrName;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5108:1.
    private Node parseJsxAttributeValue()
    {
        if ((this.token == Kind.EqualsToken))
        {
            if ((this.scanJsxAttributeValue() == Kind.StringLiteral))
            {
                return this.parseLiteralExpression();
            }

            if ((this.token == Kind.OpenBraceToken))
            {
                return this.parseJsxExpression(true);
            }

            if ((this.token == Kind.LessThanToken))
            {
                return this.parseJsxElementOrSelfClosingElementOrFragment(true, -1, null, true);
            }

            this.parseErrorAtCurrentToken(Diagnostics.X_or_JSX_element_expected);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5126:1.
    private Node parseJsxClosingFragment(bool inExpressionContext)
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.LessThanSlashToken);
        if (this.parseExpectedWithDiagnostic(Kind.GreaterThanToken, Diagnostics.Expected_corresponding_closing_tag_for_JSX_fragment, false))
        {
            if (inExpressionContext)
            {
                this.nextToken();
            }
            else
            {
                this.scanJsxText();
            }
        }

        return this.finishNode(this.factory.NewJsxClosingFragment(), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5140:1.
    private Node parseSimpleUnaryExpression()
    {
        switch (this.token)
        {
            case Kind.PlusToken:
            case Kind.MinusToken:
            case Kind.TildeToken:
            case Kind.ExclamationToken:
            {
                return this.parsePrefixUnaryExpression();
            }

            case Kind.DeleteKeyword:
            {
                return this.parseDeleteExpression();
            }

            case Kind.TypeOfKeyword:
            {
                return this.parseTypeOfExpression();
            }

            case Kind.VoidKeyword:
            {
                return this.parseVoidExpression();
            }

            case Kind.LessThanToken:
            {
                if ((this.languageVariant == LanguageVariant.JSX))
                {
                    return this.parseJsxElementOrSelfClosingElementOrFragment(true, -1, null, true);
                }

                return this.parseTypeAssertion();
            }

            case Kind.AwaitKeyword:
            {
                if (this.isAwaitExpression())
                {
                    return this.parseAwaitExpression();
                }

                goto default;
            }

            default:
            {
                return this.parseUpdateExpression();
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5170:1.
    private Node parsePrefixUnaryExpression()
    {
        var pos = this.nodePos();
        var @operator = this.token;
        this.nextToken();
        return this.finishNode(this.factory.NewPrefixUnaryExpression(@operator, this.parseSimpleUnaryExpression()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5177:1.
    private Node parseDeleteExpression()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewDeleteExpression(this.parseSimpleUnaryExpression()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5183:1.
    private Node parseTypeOfExpression()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewTypeOfExpression(this.parseSimpleUnaryExpression()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5189:1.
    private Node parseVoidExpression()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewVoidExpression(this.parseSimpleUnaryExpression()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5195:1.
    private bool isAwaitExpression()
    {
        if ((this.token == Kind.AwaitKeyword))
        {
            if (this.inAwaitContext())
            {
                return true;
            }

            return this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOrLiteralOnSameLine());
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5206:1.
    private Node parseAwaitExpression()
    {
        var pos = this.nodePos();
        this.nextToken();
        return this.finishNode(this.factory.NewAwaitExpression(this.parseSimpleUnaryExpression()), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5212:1.
    private Node parseTypeAssertion()
    {
        Debug.Assert((this.languageVariant != LanguageVariant.JSX), "Type assertions should never be parsed in JSX; they should be parsed as comparisons or JSX elements/fragments.");
        var pos = this.nodePos();
        this.parseExpected(Kind.LessThanToken);
        var typeNode = this.parseType();
        this.parseExpected(Kind.GreaterThanToken);
        var expression = this.parseSimpleUnaryExpression();
        return this.finishNode(this.factory.NewTypeAssertion(typeNode, expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5222:1.
    private Node parseLeftHandSideExpressionOrHigher()
    {
        var pos = this.nodePos();
        Node expression = default;
        if ((this.token == Kind.ImportKeyword))
        {
            if (this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenParenOrLessThan()))
            {
                this.sourceFlags |= NodeFlags.PossiblyContainsDynamicImport;
                expression = this.parseKeywordExpression();
            }
            else
            {
                if (this.lookAhead((parserCallback) => parserCallback.nextTokenIsDot()))
                {
                    this.nextToken();
                    this.nextToken();
                    expression = this.finishNode(this.factory.NewMetaProperty(Kind.ImportKeyword, this.parseImportMetaPropertyName()), pos);
                    if (Ast.IsImportPhaseMetaProperty(expression))
                    {
                        if (((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken)))
                        {
                            this.sourceFlags |= NodeFlags.PossiblyContainsDynamicImport;
                        }
                    }
                    else
                    {
                        this.sourceFlags |= NodeFlags.PossiblyContainsImportMeta;
                    }
                }
                else
                {
                    expression = this.parseMemberExpressionOrHigher();
                }
            }
        }
        else
        {
            if ((this.token == Kind.SuperKeyword))
            {
                expression = this.parseSuperExpression();
            }
            else
            {
                expression = this.parseMemberExpressionOrHigher();
            }
        }

        return this.parseCallExpressionRest(pos, expression);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5291:1.
    private bool nextTokenIsDot()
    {
        return (this.nextToken() == Kind.DotToken);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5295:1.
    private Node parseImportMetaPropertyName()
    {
        switch (this.token)
        {
            case Kind.DeferKeyword:
            case Kind.SourceKeyword:
            {
                if ((this.currentImportPhaseModifier() == Kind.Unknown))
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Keywords_cannot_contain_escape_characters);
                }

                break;
            }
        }

        return this.parseIdentifierName();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5305:1.
    private Node parseSuperExpression()
    {
        var pos = this.nodePos();
        var expression = this.parseKeywordExpression();
        if ((this.token == Kind.LessThanToken))
        {
            var startPos = this.nodePos();
            var typeArguments = this.tryParseTypeArgumentsInExpression();
            if ((typeArguments != null))
            {
                this.parseErrorAt(startPos, this.nodePos(), Diagnostics.X_super_may_not_use_type_arguments);
                if (!this.isTemplateStartOfTaggedTemplate())
                {
                    expression = this.finishNode(this.factory.NewExpressionWithTypeArguments(expression, typeArguments), pos);
                }
            }
        }

        if ((((this.token == Kind.OpenParenToken) || (this.token == Kind.DotToken)) || (this.token == Kind.OpenBracketToken)))
        {
            return expression;
        }

        this.parseErrorAtCurrentToken(Diagnostics.X_super_must_be_followed_by_an_argument_list_or_member_access);
        return this.finishNode(this.factory.NewPropertyAccessExpression(expression, null, this.parseRightSideOfDot(true, true, true), NodeFlags.None), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5328:1.
    private bool isTemplateStartOfTaggedTemplate()
    {
        return ((this.token == Kind.NoSubstitutionTemplateLiteral) || (this.token == Kind.TemplateHead));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5332:1.
    private NodeList tryParseTypeArgumentsInExpression()
    {
        if ((((this.contextFlags & NodeFlags.JavaScriptFile) != 0) || (((this.token != Kind.LessThanToken) && (this.token != Kind.LessThanLessThanToken)))))
        {
            return null;
        }

        var state = this.mark();
        if ((this.reScanLessThanToken() == Kind.LessThanToken))
        {
            this.nextToken();
            var typeArguments = this.parseDelimitedList(ParsingContext.TypeArguments, (parserCallback) => parserCallback.parseType());
            if ((this.reScanGreaterThanToken() == Kind.GreaterThanToken))
            {
                this.nextToken();
                if (this.canFollowTypeArgumentsInExpression())
                {
                    return typeArguments;
                }
            }
        }

        this.rewind(state);
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5360:1.
    private bool canFollowTypeArgumentsInExpression()
    {
        switch (this.token)
        {
            case Kind.OpenParenToken:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateHead:
            {
                return true;
            }

            case Kind.LessThanToken:
            case Kind.GreaterThanToken:
            case Kind.PlusToken:
            case Kind.MinusToken:
            {
                return false;
            }
        }

        return ((this.hasPrecedingLineBreak() || this.isBinaryOperator()) || !this.isStartOfExpression());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5379:1.
    private Node parseMemberExpressionOrHigher()
    {
        var pos = this.nodePos();
        var expression = this.parsePrimaryExpression();
        return this.parseMemberExpressionRest(pos, expression, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5432:1.
    private Node parseMemberExpressionRest(int pos, Node expression, bool allowOptionalChain)
    {
        while (true)
        {
            Node questionDotToken = default;
            var isPropertyAccess = false;
            if ((allowOptionalChain && this.isStartOfOptionalPropertyOrElementAccessChain()))
            {
                questionDotToken = this.parseExpectedToken(Kind.QuestionDotToken);
                isPropertyAccess = tokenIsIdentifierOrKeyword(this.token);
            }
            else
            {
                isPropertyAccess = this.parseOptional(Kind.DotToken);
            }

            if (isPropertyAccess)
            {
                expression = this.parsePropertyAccessExpressionRest(pos, expression, questionDotToken);
                continue;
            }

            if (((((questionDotToken != null) || !this.inDecoratorContext())) && this.parseOptional(Kind.OpenBracketToken)))
            {
                expression = this.parseElementAccessExpressionRest(pos, expression, questionDotToken);
                continue;
            }

            if (this.isTemplateStartOfTaggedTemplate())
            {
                if (((questionDotToken == null) && Ast.IsExpressionWithTypeArguments(expression)))
                {
                    var original = expression.AsExpressionWithTypeArguments();
                    expression = this.parseTaggedTemplateRest(pos, original.Expression, questionDotToken, original.TypeArguments);
                    this.unparseExpressionWithTypeArguments(original.Expression, original.TypeArguments, expression);
                }
                else
                {
                    expression = this.parseTaggedTemplateRest(pos, expression, questionDotToken, null);
                }

                continue;
            }

            if ((questionDotToken == null))
            {
                if (((this.token == Kind.ExclamationToken) && !this.hasPrecedingLineBreak()))
                {
                    this.nextToken();
                    expression = this.checkJSSyntax(this.finishNode(this.factory.NewNonNullExpression(expression, NodeFlags.None), pos));
                    continue;
                }

                var typeArguments = this.tryParseTypeArgumentsInExpression();
                if ((typeArguments != null))
                {
                    expression = this.finishNode(this.factory.NewExpressionWithTypeArguments(expression, typeArguments), pos);
                    continue;
                }
            }

            return expression;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5478:1.
    private bool isStartOfOptionalPropertyOrElementAccessChain()
    {
        return ((this.token == Kind.QuestionDotToken) && this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOrOpenBracketOrTemplate()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5482:1.
    private bool nextTokenIsIdentifierOrKeywordOrOpenBracketOrTemplate()
    {
        this.nextToken();
        return ((tokenIsIdentifierOrKeyword(this.token) || (this.token == Kind.OpenBracketToken)) || this.isTemplateStartOfTaggedTemplate());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5487:1.
    private Node parsePropertyAccessExpressionRest(int pos, Node expression, Node questionDotToken)
    {
        var name = this.parseRightSideOfDot(true, true, true);
        var isOptionalChain = ((questionDotToken != null) || this.tryReparseOptionalChain(expression));
        var propertyAccess = this.factory.NewPropertyAccessExpression(expression, questionDotToken, name, Core.IfElse(isOptionalChain, NodeFlags.OptionalChain, NodeFlags.None));
        if ((isOptionalChain && Ast.IsPrivateIdentifier(name)))
        {
            this.parseErrorAtRange(this.skipRangeTrivia(name.Loc), Diagnostics.An_optional_chain_cannot_contain_private_identifiers);
        }

        if (Ast.IsExpressionWithTypeArguments(expression))
        {
            var typeArguments = expression.TypeArgumentList();
            if ((typeArguments != null))
            {
                var loc = Core.NewTextRange((typeArguments.Pos() - 1), (Scanner.SkipTriviaByteString(this.sourceText, typeArguments.End()) + 1));
                this.parseErrorAtRange(loc, Diagnostics.An_instantiation_expression_cannot_be_followed_by_a_property_access);
            }
        }

        return this.finishNode(propertyAccess, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5504:1.
    private bool tryReparseOptionalChain(Node node)
    {
        if (((node.Flags & NodeFlags.OptionalChain) != 0))
        {
            return true;
        }

        if (Ast.IsNonNullExpression(node))
        {
            var expr = node.Expression();
            while ((Ast.IsNonNullExpression(expr) && ((expr.Flags & NodeFlags.OptionalChain) == 0)))
            {
                expr = expr.Expression();
            }

            if (((expr.Flags & NodeFlags.OptionalChain) != 0))
            {
                while (Ast.IsNonNullExpression(node))
                {
                    node.Flags |= NodeFlags.OptionalChain;
                    node = node.Expression();
                }

                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5526:1.
    private Node parseElementAccessExpressionRest(int pos, Node expression, Node questionDotToken)
    {
        var argumentExpression = this.createMissingIdentifier();
        if ((this.token == Kind.CloseBracketToken))
        {
            this.parseErrorAt(this.nodePos(), this.nodePos(), Diagnostics.An_element_access_expression_should_take_an_argument);
        }
        else
        {
            argumentExpression = this.parseExpressionAllowIn();
        }

        this.parseExpected(Kind.CloseBracketToken);
        var isOptionalChain = ((questionDotToken != null) || this.tryReparseOptionalChain(expression));
        return this.finishNode(this.factory.NewElementAccessExpression(expression, questionDotToken, argumentExpression, Core.IfElse(isOptionalChain, NodeFlags.OptionalChain, NodeFlags.None)), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5538:1.
    private Node parseCallExpressionRest(int pos, Node expression)
    {
        while (true)
        {
            expression = this.parseMemberExpressionRest(pos, expression, true);
            NodeList typeArguments = default;
            var questionDotToken = this.parseOptionalToken(Kind.QuestionDotToken);
            if ((questionDotToken != null))
            {
                typeArguments = this.tryParseTypeArgumentsInExpression();
                if (this.isTemplateStartOfTaggedTemplate())
                {
                    expression = this.parseTaggedTemplateRest(pos, expression, questionDotToken, typeArguments);
                    continue;
                }
            }

            if (((typeArguments != null) || (this.token == Kind.OpenParenToken)))
            {
                if (((questionDotToken == null) && (expression.Kind == Kind.ExpressionWithTypeArguments)))
                {
                    typeArguments = expression.TypeArgumentList();
                    expression = expression.AsExpressionWithTypeArguments().Expression;
                }

                var inner = expression;
                var argumentList = this.parseArgumentList();
                var isOptionalChain = ((questionDotToken != null) || this.tryReparseOptionalChain(expression));
                expression = this.checkJSSyntax(this.finishNode(this.factory.NewCallExpression(expression, questionDotToken, typeArguments, argumentList, Core.IfElse(isOptionalChain, NodeFlags.OptionalChain, NodeFlags.None)), pos));
                this.unparseExpressionWithTypeArguments(inner, typeArguments, expression);
                continue;
            }

            if ((questionDotToken != null))
            {
                this.parseErrorAtCurrentToken(Diagnostics.Identifier_expected);
                var name = this.createMissingIdentifier();
                expression = this.finishNode(this.factory.NewPropertyAccessExpression(expression, questionDotToken, name, NodeFlags.OptionalChain), pos);
            }

            break;
        }

        return expression;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5574:1.
    private NodeList parseArgumentList()
    {
        this.parseExpected(Kind.OpenParenToken);
        var result = this.parseDelimitedList(ParsingContext.ArgumentExpressions, (parserCallback) => parserCallback.parseArgumentExpression());
        this.parseExpected(Kind.CloseParenToken);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5581:1.
    private Node parseArgumentExpression()
    {
        return this.doInContext((NodeFlags.DisallowInContext | NodeFlags.DecoratorContext), false, (parserCallback) => parserCallback.parseArgumentOrArrayLiteralElement());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5585:1.
    private Node parseArgumentOrArrayLiteralElement()
    {
        switch (this.token)
        {
            case Kind.DotDotDotToken:
            {
                return this.parseSpreadElement();
            }

            case Kind.CommaToken:
            {
                return this.finishNode(this.factory.NewOmittedExpression(), this.nodePos());
            }
        }

        return this.parseAssignmentExpressionOrHigher();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5595:1.
    private Node parseSpreadElement()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.DotDotDotToken);
        var expression = this.parseAssignmentExpressionOrHigher();
        return this.finishNode(this.factory.NewSpreadElement(expression), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5602:1.
    private Node parseTaggedTemplateRest(int pos, Node tag, Node questionDotToken, NodeList typeArguments)
    {
        Node template = default;
        if ((this.token == Kind.NoSubstitutionTemplateLiteral))
        {
            this.reScanTemplateToken(true);
            template = this.parseLiteralExpression();
        }
        else
        {
            template = this.parseTemplateExpression(true);
        }

        var isOptionalChain = ((questionDotToken != null) || ((tag.Flags & NodeFlags.OptionalChain) != 0));
        return this.checkJSSyntax(this.finishNode(this.factory.NewTaggedTemplateExpression(tag, questionDotToken, typeArguments, template, Core.IfElse(isOptionalChain, NodeFlags.OptionalChain, NodeFlags.None)), pos));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5614:1.
    private Node parseTemplateExpression(bool isTaggedTemplate)
    {
        var pos = this.nodePos();
        return this.finishNode(this.factory.NewTemplateExpression(this.parseTemplateHead(isTaggedTemplate), this.parseTemplateSpans(isTaggedTemplate)), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5619:1.
    private NodeList parseTemplateSpans(bool isTaggedTemplate)
    {
        var pos = this.nodePos();
        Slice<Node> list = default;
        while (true)
        {
            var span = this.parseTemplateSpan(isTaggedTemplate);
            list = GoRuntime.Append(list, span);
            if ((span.AsTemplateSpan().Literal.Kind != Kind.TemplateMiddle))
            {
                break;
            }
        }

        return this.newNodeList(Core.NewTextRange(pos, this.nodePos()), list);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5632:1.
    private Node parseTemplateSpan(bool isTaggedTemplate)
    {
        var pos = this.nodePos();
        var expression = this.parseExpressionAllowIn();
        var literal = this.parseLiteralOfTemplateSpan(isTaggedTemplate);
        return this.finishNode(this.factory.NewTemplateSpan(expression, literal), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5639:1.
    private Node parsePrimaryExpression()
    {
        switch (this.token)
        {
            case Kind.NoSubstitutionTemplateLiteral:
            {
                if (((this.scanner.TokenFlags() & TokenFlags.IsInvalid) != 0))
                {
                    this.reScanTemplateToken(false);
                }

                goto case Kind.NumericLiteral;
            }

            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.StringLiteral:
            {
                return this.parseLiteralExpression();
            }

            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.NullKeyword:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            {
                return this.parseKeywordExpression();
            }

            case Kind.OpenParenToken:
            {
                return this.parseParenthesizedExpression();
            }

            case Kind.OpenBracketToken:
            {
                return this.parseArrayLiteralExpression();
            }

            case Kind.OpenBraceToken:
            {
                return this.parseObjectLiteralExpression();
            }

            case Kind.AsyncKeyword:
            {
                if (!this.lookAhead((parserCallback) => parserCallback.nextTokenIsFunctionKeywordOnSameLine()))
                {
                    break;
                }

                return this.parseFunctionExpression();
            }

            case Kind.AtToken:
            {
                return this.parseDecoratedExpression();
            }

            case Kind.ClassKeyword:
            {
                return this.parseClassExpression();
            }

            case Kind.FunctionKeyword:
            {
                return this.parseFunctionExpression();
            }

            case Kind.NewKeyword:
            {
                return this.parseNewExpressionOrNewDotTarget();
            }

            case Kind.SlashToken:
            case Kind.SlashEqualsToken:
            {
                if ((this.reScanSlashToken() == Kind.RegularExpressionLiteral))
                {
                    return this.parseLiteralExpression();
                }

                break;
            }

            case Kind.TemplateHead:
            {
                return this.parseTemplateExpression(false);
            }

            case Kind.PrivateIdentifier:
            {
                return this.parsePrivateIdentifier();
            }
        }

        return this.parseIdentifierWithDiagnostic(Diagnostics.Expression_expected, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5684:1.
    private Node parseParenthesizedExpression()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        this.parseExpected(Kind.OpenParenToken);
        var expression = this.parseExpressionAllowIn();
        this.parseExpected(Kind.CloseParenToken);
        var result = this.finishNode(this.factory.NewParenthesizedExpression(expression), pos);
        this.withJSDoc(result, jsdoc);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5695:1.
    private Node parseArrayLiteralExpression()
    {
        var pos = this.nodePos();
        var openBracketPosition = this.scanner.TokenStart();
        var openBracketParsed = this.parseExpected(Kind.OpenBracketToken);
        var multiLine = this.hasPrecedingLineBreak();
        var elements = this.parseDelimitedList(ParsingContext.ArrayLiteralMembers, (parserCallback) => parserCallback.parseArgumentOrArrayLiteralElement());
        this.parseExpectedMatchingBrackets(Kind.OpenBracketToken, Kind.CloseBracketToken, openBracketParsed, openBracketPosition);
        return this.finishNode(this.factory.NewArrayLiteralExpression(elements, multiLine), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5705:1.
    private Node parseObjectLiteralExpression()
    {
        var pos = this.nodePos();
        var openBracePosition = this.scanner.TokenStart();
        var openBraceParsed = this.parseExpected(Kind.OpenBraceToken);
        var multiLine = this.hasPrecedingLineBreak();
        var properties = this.parseDelimitedList(ParsingContext.ObjectLiteralMembers, (parserCallback) => parserCallback.parseObjectLiteralElement());
        this.parseExpectedMatchingBrackets(Kind.OpenBraceToken, Kind.CloseBraceToken, openBraceParsed, openBracePosition);
        return this.finishNode(this.factory.NewObjectLiteralExpression(properties, multiLine), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5715:1.
    private Node parseObjectLiteralElement()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        if (this.parseOptional(Kind.DotDotDotToken))
        {
            var expression = this.parseAssignmentExpressionOrHigher();
            var result = this.finishNode(this.factory.NewSpreadAssignment(expression), pos);
            this.withJSDoc(result, jsdoc);
            return result;
        }

        var modifiers = this.parseModifiersEx(true, false, false);
        if (this.parseContextualModifier(Kind.GetKeyword))
        {
            return this.parseAccessorDeclaration(pos, jsdoc, modifiers, Kind.GetAccessor, ParseFlags.None);
        }

        if (this.parseContextualModifier(Kind.SetKeyword))
        {
            return this.parseAccessorDeclaration(pos, jsdoc, modifiers, Kind.SetAccessor, ParseFlags.None);
        }

        var asteriskToken = this.parseOptionalToken(Kind.AsteriskToken);
        var tokenIsIdentifier = this.isIdentifier();
        var name = this.parsePropertyName();
        var postfixToken = this.parseOptionalToken(Kind.QuestionToken);
        if ((postfixToken == null))
        {
            postfixToken = this.parseOptionalToken(Kind.ExclamationToken);
        }

        if ((((asteriskToken != null) || (this.token == Kind.OpenParenToken)) || (this.token == Kind.LessThanToken)))
        {
            return this.parseMethodDeclaration(pos, jsdoc, modifiers, asteriskToken, name, postfixToken, null);
        }

        Node node = default;
        var isShorthandPropertyAssignment = (tokenIsIdentifier && (this.token != Kind.ColonToken));
        if (isShorthandPropertyAssignment)
        {
            var equalsToken = this.parseOptionalToken(Kind.EqualsToken);
            Node initializer = default;
            if ((equalsToken != null))
            {
                initializer = this.doInContext(NodeFlags.DisallowInContext, false, (parserCallback) => parserCallback.parseAssignmentExpressionOrHigher());
            }

            node = this.factory.NewShorthandPropertyAssignment(modifiers, name, postfixToken, null, equalsToken, initializer);
        }
        else
        {
            this.parseExpected(Kind.ColonToken);
            var initializer2 = this.doInContext(NodeFlags.DisallowInContext, false, (parserCallback) => parserCallback.parseAssignmentExpressionOrHigher());
            node = this.factory.NewPropertyAssignment(modifiers, name, postfixToken, null, initializer2);
        }

        this.finishNode(node, pos);
        this.withJSDoc(node, jsdoc);
        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5767:1.
    private Node parseFunctionExpression()
    {
        var saveContexFlags = this.contextFlags;
        this.setContextFlags(NodeFlags.DecoratorContext, false);
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var modifiers = this.parseModifiers();
        this.parseExpected(Kind.FunctionKeyword);
        var asteriskToken = this.parseOptionalToken(Kind.AsteriskToken);
        var isGenerator = (asteriskToken != null);
        var isAsync = modifierListHasAsync(modifiers);
        var signatureFlags = (Core.IfElse(isGenerator, ParseFlags.Yield, ParseFlags.None) | Core.IfElse(isAsync, ParseFlags.Await, ParseFlags.None));
        Node name = default;
        switch (true)
        {
            case true when (isGenerator && isAsync):
            {
                name = this.doInContext((NodeFlags.YieldContext | NodeFlags.AwaitContext), true, (parserCallback) => parserCallback.parseOptionalBindingIdentifier());
                break;
            }

            case true when isGenerator:
            {
                name = this.doInContext(NodeFlags.YieldContext, true, (parserCallback) => parserCallback.parseOptionalBindingIdentifier());
                break;
            }

            case true when isAsync:
            {
                name = this.doInContext(NodeFlags.AwaitContext, true, (parserCallback) => parserCallback.parseOptionalBindingIdentifier());
                break;
            }

            default:
            {
                name = this.parseOptionalBindingIdentifier();
                break;
            }
        }

        var typeParameters = this.parseTypeParameters();
        var parameters = this.parseParameters(signatureFlags);
        var returnType = this.parseReturnType(Kind.ColonToken, false);
        var body = this.parseFunctionBlock(signatureFlags, null);
        this.contextFlags = saveContexFlags;
        var result = this.factory.NewFunctionExpression(modifiers, asteriskToken, name, typeParameters, parameters, returnType, null, body);
        this.finishNode(result, pos);
        this.withJSDoc(result, jsdoc);
        this.checkJSSyntax(result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5806:1.
    private Node parseOptionalBindingIdentifier()
    {
        if (this.isBindingIdentifier())
        {
            return this.parseBindingIdentifier();
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5813:1.
    private Node parseDecoratedExpression()
    {
        var pos = this.nodePos();
        var jsdoc = this.jsdocScannerInfo();
        var modifiers = this.parseModifiersEx(true, false, false);
        if ((this.token == Kind.ClassKeyword))
        {
            return this.parseClassDeclarationOrExpression(pos, jsdoc, modifiers, Kind.ClassExpression);
        }

        this.parseErrorAt(this.nodePos(), this.nodePos(), Diagnostics.Expression_expected);
        return this.finishNode(this.factory.NewMissingDeclaration(modifiers), pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5824:1.
    private void unparseExpressionWithTypeArguments(Node expression, NodeList typeArguments, Node result)
    {
        if ((expression != null))
        {
            expression.Parent = result;
        }

        if ((typeArguments != null))
        {
            {
                foreach (var a in GoRuntime.RangeValues(typeArguments.Nodes))
                {
                    a.Parent = result;
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5836:1.
    private Node parseNewExpressionOrNewDotTarget()
    {
        var pos = this.nodePos();
        this.parseExpected(Kind.NewKeyword);
        if (this.parseOptional(Kind.DotToken))
        {
            var name = this.parseIdentifierName();
            return this.finishNode(this.factory.NewMetaProperty(Kind.NewKeyword, name), pos);
        }

        var expressionPos = this.nodePos();
        var expression = this.parseMemberExpressionRest(expressionPos, this.parsePrimaryExpression(), false);
        NodeList typeArguments = default;
        if ((expression.Kind == Kind.ExpressionWithTypeArguments))
        {
            typeArguments = expression.TypeArgumentList();
            expression = expression.AsExpressionWithTypeArguments().Expression;
        }

        if ((this.token == Kind.QuestionDotToken))
        {
            this.parseErrorAtCurrentToken(Diagnostics.Invalid_optional_chain_from_new_expression_Did_you_mean_to_call_0, Scanner.GetTextOfNodeFromSourceTextByteString(this.sourceText, expression, false));
        }

        NodeList argumentList = default;
        if ((this.token == Kind.OpenParenToken))
        {
            argumentList = this.parseArgumentList();
        }

        var result = this.checkJSSyntax(this.finishNode(this.factory.NewNewExpression(expression, typeArguments, argumentList), pos));
        this.unparseExpressionWithTypeArguments(expression, typeArguments, result);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5863:1.
    private Node parseKeywordExpression()
    {
        var pos = this.nodePos();
        var result = this.factory.NewKeywordExpression(this.token);
        this.nextToken();
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5870:1.
    private Node parseLiteralExpression()
    {
        var pos = this.nodePos();
        var text = this.scanner.TokenValueTextValue();
        var tokenFlags = this.scanner.TokenFlags();
        Node result = default;
        switch (this.token)
        {
            case Kind.StringLiteral:
            {
                result = this.factory.NewStringLiteralWithTextValue(text, tokenFlags);
                break;
            }

            case Kind.NumericLiteral:
            {
                result = this.factory.NewNumericLiteralWithTextValue(text, tokenFlags);
                break;
            }

            case Kind.BigIntLiteral:
            {
                result = this.factory.NewBigIntLiteralWithTextValue(text, tokenFlags);
                break;
            }

            case Kind.RegularExpressionLiteral:
            {
                result = this.factory.NewRegularExpressionLiteralWithTextValue(text, tokenFlags);
                break;
            }

            case Kind.NoSubstitutionTemplateLiteral:
            {
                result = this.factory.NewNoSubstitutionTemplateLiteralWithTextValue(text, tokenFlags);
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in parseLiteralExpression");
            }
        }

        this.nextToken();
        return this.finishNode(result, pos);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5893:1.
    private Node parseIdentifierNameErrorOnUnicodeEscapeSequence()
    {
        if ((this.scanner.HasUnicodeEscape() || this.scanner.HasExtendedUnicodeEscape()))
        {
            this.parseErrorAtCurrentToken(Diagnostics.Unicode_escape_sequence_cannot_appear_here);
        }

        return this.createIdentifier(tokenIsIdentifierOrKeyword(this.token));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5900:1.
    private Node parseBindingIdentifier()
    {
        return this.parseBindingIdentifierWithDiagnostic(null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5904:1.
    private Node parseBindingIdentifierWithDiagnostic(Message privateIdentifierDiagnosticMessage)
    {
        var saveHasAwaitIdentifier = this.statementHasAwaitIdentifier;
        var id = this.createIdentifierWithDiagnostic(this.isBindingIdentifier(), null, privateIdentifierDiagnosticMessage);
        this.statementHasAwaitIdentifier = saveHasAwaitIdentifier;
        return id;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5911:1.
    private Node parseIdentifierName()
    {
        return this.parseIdentifierNameWithDiagnostic(null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5915:1.
    private Node parseIdentifierNameWithDiagnostic(Message diagnosticMessage)
    {
        return this.createIdentifierWithDiagnostic(tokenIsIdentifierOrKeyword(this.token), diagnosticMessage, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5919:1.
    private Node parseIdentifier()
    {
        return this.parseIdentifierWithDiagnostic(null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5923:1.
    private Node parseIdentifierWithDiagnostic(Message diagnosticMessage, Message privateIdentifierDiagnosticMessage)
    {
        return this.createIdentifierWithDiagnostic(this.isIdentifier(), diagnosticMessage, privateIdentifierDiagnosticMessage);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5927:1.
    private Node createIdentifier(bool isIdentifier)
    {
        return this.createIdentifierWithDiagnostic(isIdentifier, null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5931:1.
    private Node createIdentifierWithDiagnostic(bool isIdentifier, Message diagnosticMessage, Message privateIdentifierDiagnosticMessage)
    {
        if (isIdentifier)
        {
            int pos = default;
            if (this.scanner.HasPrecedingJSDocLeadingAsterisks())
            {
                pos = this.scanner.TokenStart();
            }
            else
            {
                pos = this.nodePos();
            }

            var text = this.scanner.TokenValueTextValue();
            this.nextTokenWithoutCheck();
            return this.finishNode(this.newIdentifier(text), pos);
        }

        if ((this.token == Kind.PrivateIdentifier))
        {
            if ((privateIdentifierDiagnosticMessage != null))
            {
                this.parseErrorAtCurrentToken(privateIdentifierDiagnosticMessage);
            }
            else
            {
                this.parseErrorAtCurrentToken(Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies);
            }

            return this.createIdentifier(true);
        }

        var reportAtCurrentPosition = (this.token == Kind.EndOfFile);
        if ((diagnosticMessage != null))
        {
            if (reportAtCurrentPosition)
            {
                var pos2 = this.scanner.TokenFullStart();
                this.parseErrorAt(pos2, pos2, diagnosticMessage);
            }
            else
            {
                this.parseErrorAtCurrentToken(diagnosticMessage);
            }
        }
        else
        {
            if (isReservedWord(this.token))
            {
                if (reportAtCurrentPosition)
                {
                    var pos3 = this.scanner.TokenFullStart();
                    this.parseErrorAt(pos3, pos3, Diagnostics.Identifier_expected_0_is_a_reserved_word_that_cannot_be_used_here, this.scanner.TokenTextByteString());
                }
                else
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Identifier_expected_0_is_a_reserved_word_that_cannot_be_used_here, this.scanner.TokenTextByteString());
                }
            }
            else
            {
                if (reportAtCurrentPosition)
                {
                    var pos4 = this.scanner.TokenFullStart();
                    this.parseErrorAt(pos4, pos4, Diagnostics.Identifier_expected);
                }
                else
                {
                    this.parseErrorAtCurrentToken(Diagnostics.Identifier_expected);
                }
            }
        }

        return this.createMissingIdentifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5978:1.
    private NodeList newNodeList(TextRange loc, Slice<Node> nodes)
    {
        var list = this.factory.NewNodeList(nodes);
        list.Loc = loc;
        return list;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5984:1.
    private ModifierList newModifierList(TextRange loc, Slice<Node> nodes)
    {
        var list = this.factory.NewModifierList(nodes);
        list.Loc = loc;
        return list;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5990:1.
    private Node finishNode(Node node, int pos)
    {
        return this.finishNodeWithEnd(node, pos, this.nodePos());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:5994:1.
    private Node finishNodeWithEnd(Node node, int pos, int end)
    {
        node.Loc = Core.NewTextRange(pos, end);
        node.Flags |= this.contextFlags;
        if (this.hasParseError)
        {
            node.Flags |= NodeFlags.ThisNodeHasError;
            this.hasParseError = false;
        }

        this.overrideParentInImmediateChildren(node);
        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6005:1.
    private void overrideParentInImmediateChildren(Node node)
    {
        this.currentParent = node;
        node.ForEachChild(this.setParentFromContext);
        this.currentParent = null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6011:1.
    private bool nextTokenIsSlash()
    {
        return (this.nextToken() == Kind.SlashToken);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6015:1.
    private bool scanTypeMemberStart()
    {
        if (((((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken)) || (this.token == Kind.GetKeyword)) || (this.token == Kind.SetKeyword)))
        {
            return true;
        }

        var idToken = false;
        while (Ast.IsModifierKind(this.token))
        {
            idToken = true;
            this.nextToken();
        }

        if ((this.token == Kind.OpenBracketToken))
        {
            return true;
        }

        if (this.isLiteralPropertyName())
        {
            idToken = true;
            this.nextToken();
        }

        if (idToken)
        {
            return ((((((this.token == Kind.OpenParenToken) || (this.token == Kind.LessThanToken)) || (this.token == Kind.QuestionToken)) || (this.token == Kind.ColonToken)) || (this.token == Kind.CommaToken)) || this.canParseSemicolon());
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6043:1.
    private bool scanClassMemberStart()
    {
        var idToken = Kind.Unknown;
        if ((this.token == Kind.AtToken))
        {
            return true;
        }

        while (Ast.IsModifierKind(this.token))
        {
            idToken = this.token;
            if (Ast.IsClassMemberModifier(idToken))
            {
                return true;
            }

            this.nextToken();
        }

        if ((this.token == Kind.AsteriskToken))
        {
            return true;
        }

        if (this.isLiteralPropertyName())
        {
            idToken = this.token;
            this.nextToken();
        }

        if ((this.token == Kind.OpenBracketToken))
        {
            return true;
        }

        if ((idToken != Kind.Unknown))
        {
            if (((!Ast.IsKeyword(idToken) || (idToken == Kind.SetKeyword)) || (idToken == Kind.GetKeyword)))
            {
                return true;
            }

            switch (this.token)
            {
                case Kind.OpenParenToken:
                case Kind.LessThanToken:
                case Kind.ExclamationToken:
                case Kind.ColonToken:
                case Kind.EqualsToken:
                case Kind.QuestionToken:
                {
                    return true;
                }
            }

            return this.canParseSemicolon();
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6102:1.
    private bool canParseSemicolon()
    {
        return ((((this.token == Kind.SemicolonToken) || (this.token == Kind.CloseBraceToken)) || (this.token == Kind.EndOfFile)) || this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6108:1.
    private bool tryParseSemicolon()
    {
        if (!this.canParseSemicolon())
        {
            return false;
        }

        if ((this.token == Kind.SemicolonToken))
        {
            this.nextToken();
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6119:1.
    private bool parseSemicolon()
    {
        return (this.tryParseSemicolon() || this.parseExpected(Kind.SemicolonToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6123:1.
    private bool isLiteralPropertyName()
    {
        return (((tokenIsIdentifierOrKeyword(this.token) || (this.token == Kind.StringLiteral)) || (this.token == Kind.NumericLiteral)) || (this.token == Kind.BigIntLiteral));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6127:1.
    private bool isStartOfStatement()
    {
        switch (this.token)
        {
            case Kind.AtToken:
            case Kind.SemicolonToken:
            case Kind.OpenBraceToken:
            case Kind.VarKeyword:
            case Kind.LetKeyword:
            case Kind.UsingKeyword:
            case Kind.FunctionKeyword:
            case Kind.ClassKeyword:
            case Kind.EnumKeyword:
            case Kind.IfKeyword:
            case Kind.DoKeyword:
            case Kind.WhileKeyword:
            case Kind.ForKeyword:
            case Kind.ContinueKeyword:
            case Kind.BreakKeyword:
            case Kind.ReturnKeyword:
            case Kind.WithKeyword:
            case Kind.SwitchKeyword:
            case Kind.ThrowKeyword:
            case Kind.TryKeyword:
            case Kind.DebuggerKeyword:
            case Kind.CatchKeyword:
            case Kind.FinallyKeyword:
            {
                return true;
            }

            case Kind.ImportKeyword:
            {
                return (this.isStartOfDeclaration() || this.isNextTokenOpenParenOrLessThanOrDot());
            }

            case Kind.ConstKeyword:
            case Kind.ExportKeyword:
            {
                return this.isStartOfDeclaration();
            }

            case Kind.AsyncKeyword:
            case Kind.DeclareKeyword:
            case Kind.InterfaceKeyword:
            case Kind.ModuleKeyword:
            case Kind.NamespaceKeyword:
            case Kind.TypeKeyword:
            case Kind.GlobalKeyword:
            case Kind.DeferKeyword:
            case Kind.SourceKeyword:
            {
                return true;
            }

            case Kind.AccessorKeyword:
            case Kind.PublicKeyword:
            case Kind.PrivateKeyword:
            case Kind.ProtectedKeyword:
            case Kind.StaticKeyword:
            case Kind.ReadonlyKeyword:
            {
                return (this.isStartOfDeclaration() || !this.lookAhead((parserCallback) => parserCallback.nextTokenIsIdentifierOrKeywordOnSameLine()));
            }

            default:
            {
                return this.isStartOfExpression();
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6156:1.
    private bool isStartOfDeclaration()
    {
        return this.lookAhead((parserCallback) => parserCallback.scanStartOfDeclaration());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6160:1.
    private bool scanStartOfDeclaration()
    {
        while (true)
        {
            switch (this.token)
            {
                case Kind.VarKeyword:
                case Kind.LetKeyword:
                case Kind.ConstKeyword:
                case Kind.FunctionKeyword:
                case Kind.ClassKeyword:
                case Kind.EnumKeyword:
                {
                    return true;
                }

                case Kind.UsingKeyword:
                {
                    return this.isUsingDeclaration();
                }

                case Kind.AwaitKeyword:
                {
                    return this.isAwaitUsingDeclaration();
                }

                case Kind.InterfaceKeyword:
                case Kind.TypeKeyword:
                case Kind.DeferKeyword:
                case Kind.SourceKeyword:
                {
                    return this.nextTokenIsIdentifierOnSameLine();
                }

                case Kind.ModuleKeyword:
                case Kind.NamespaceKeyword:
                {
                    return this.nextTokenIsIdentifierOrStringLiteralOnSameLine();
                }

                case Kind.AbstractKeyword:
                case Kind.AccessorKeyword:
                case Kind.AsyncKeyword:
                case Kind.DeclareKeyword:
                case Kind.PrivateKeyword:
                case Kind.ProtectedKeyword:
                case Kind.PublicKeyword:
                case Kind.ReadonlyKeyword:
                {
                    var previousToken = this.token;
                    this.nextToken();
                    if (this.hasPrecedingLineBreak())
                    {
                        return false;
                    }

                    if (((previousToken == Kind.DeclareKeyword) && (this.token == Kind.TypeKeyword)))
                    {
                        return true;
                    }

                    continue;
                }

                case Kind.GlobalKeyword:
                {
                    this.nextToken();
                    return (((this.token == Kind.OpenBraceToken) || (this.token == Kind.Identifier)) || (this.token == Kind.ExportKeyword));
                }

                case Kind.ImportKeyword:
                {
                    this.nextToken();
                    return ((((((this.token == Kind.DeferKeyword) || (this.token == Kind.SourceKeyword)) || (this.token == Kind.StringLiteral)) || (this.token == Kind.AsteriskToken)) || (this.token == Kind.OpenBraceToken)) || tokenIsIdentifierOrKeyword(this.token));
                }

                case Kind.ExportKeyword:
                {
                    this.nextToken();
                    if (((((((this.token == Kind.EqualsToken) || (this.token == Kind.AsteriskToken)) || (this.token == Kind.OpenBraceToken)) || (this.token == Kind.DefaultKeyword)) || (this.token == Kind.AsKeyword)) || (this.token == Kind.AtToken)))
                    {
                        return true;
                    }

                    if ((this.token == Kind.TypeKeyword))
                    {
                        this.nextToken();
                        return (((this.token == Kind.AsteriskToken) || (this.token == Kind.OpenBraceToken)) || (this.isIdentifier() && !this.hasPrecedingLineBreak()));
                    }

                    continue;
                }

                case Kind.StaticKeyword:
                {
                    this.nextToken();
                    continue;
                }
            }

            return false;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6234:1.
    private bool isStartOfExpression()
    {
        if (this.isStartOfLeftHandSideExpression())
        {
            return true;
        }

        switch (this.token)
        {
            case Kind.PlusToken:
            case Kind.MinusToken:
            case Kind.TildeToken:
            case Kind.ExclamationToken:
            case Kind.DeleteKeyword:
            case Kind.TypeOfKeyword:
            case Kind.VoidKeyword:
            case Kind.PlusPlusToken:
            case Kind.MinusMinusToken:
            case Kind.LessThanToken:
            case Kind.AwaitKeyword:
            case Kind.YieldKeyword:
            case Kind.PrivateIdentifier:
            case Kind.AtToken:
            {
                return true;
            }
        }

        if (this.isBinaryOperator())
        {
            return true;
        }

        return this.isIdentifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6257:1.
    private bool isStartOfLeftHandSideExpression()
    {
        switch (this.token)
        {
            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.NullKeyword:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateHead:
            case Kind.OpenParenToken:
            case Kind.OpenBracketToken:
            case Kind.OpenBraceToken:
            case Kind.FunctionKeyword:
            case Kind.ClassKeyword:
            case Kind.NewKeyword:
            case Kind.SlashToken:
            case Kind.SlashEqualsToken:
            case Kind.Identifier:
            {
                return true;
            }

            case Kind.ImportKeyword:
            {
                return this.isNextTokenOpenParenOrLessThanOrDot();
            }
        }

        return this.isIdentifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6270:1.
    private bool isStartOfType(bool inStartOfParameter)
    {
        switch (this.token)
        {
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.StringKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.BooleanKeyword:
            case Kind.ReadonlyKeyword:
            case Kind.SymbolKeyword:
            case Kind.UniqueKeyword:
            case Kind.VoidKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NullKeyword:
            case Kind.ThisKeyword:
            case Kind.TypeOfKeyword:
            case Kind.NeverKeyword:
            case Kind.OpenBraceToken:
            case Kind.OpenBracketToken:
            case Kind.LessThanToken:
            case Kind.BarToken:
            case Kind.AmpersandToken:
            case Kind.NewKeyword:
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.ObjectKeyword:
            case Kind.AsteriskToken:
            case Kind.QuestionToken:
            case Kind.ExclamationToken:
            case Kind.DotDotDotToken:
            case Kind.InferKeyword:
            case Kind.ImportKeyword:
            case Kind.AssertsKeyword:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateHead:
            {
                return true;
            }

            case Kind.FunctionKeyword:
            {
                return !inStartOfParameter;
            }

            case Kind.MinusToken:
            {
                return (!inStartOfParameter && this.lookAhead((parserCallback) => parserCallback.nextTokenIsNumericOrBigIntLiteral()));
            }

            case Kind.OpenParenToken:
            {
                return (!inStartOfParameter && this.lookAhead((parserCallback) => parserCallback.nextIsParenthesizedOrFunctionType()));
            }
        }

        return this.isIdentifier();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6293:1.
    private bool nextTokenIsNumericOrBigIntLiteral()
    {
        this.nextToken();
        return ((this.token == Kind.NumericLiteral) || (this.token == Kind.BigIntLiteral));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6298:1.
    private bool nextIsParenthesizedOrFunctionType()
    {
        this.nextToken();
        return (((this.token == Kind.CloseParenToken) || this.isStartOfParameter(false)) || this.isStartOfType(false));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6303:1.
    private bool isStartOfParameter(bool isJSDocParameter)
    {
        return (((((this.token == Kind.DotDotDotToken) || this.isBindingIdentifierOrPrivateIdentifierOrPattern()) || Ast.IsModifierKind(this.token)) || (this.token == Kind.AtToken)) || this.isStartOfType(!isJSDocParameter));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6311:1.
    private bool isBindingIdentifierOrPrivateIdentifierOrPattern()
    {
        return ((((this.token == Kind.OpenBraceToken) || (this.token == Kind.OpenBracketToken)) || (this.token == Kind.PrivateIdentifier)) || this.isBindingIdentifier());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6315:1.
    private bool isNextTokenOpenParenOrLessThanOrDot()
    {
        return this.lookAhead((parserCallback) => parserCallback.nextTokenIsOpenParenOrLessThanOrDot());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6319:1.
    private bool nextTokenIsOpenParenOrLessThanOrDot()
    {
        switch (this.nextToken())
        {
            case Kind.OpenParenToken:
            case Kind.LessThanToken:
            case Kind.DotToken:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6327:1.
    private bool nextTokenIsIdentifierOnSameLine()
    {
        this.nextToken();
        return (this.isIdentifier() && !this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6332:1.
    private bool nextTokenIsIdentifierOrStringLiteralOnSameLine()
    {
        this.nextToken();
        return (((this.isIdentifier() || (this.token == Kind.StringLiteral))) && !this.hasPrecedingLineBreak());
    }

    // Ignore strict mode flag because we will report an error in type checker instead.
    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6338:1.
    private bool isIdentifier()
    {
        if ((this.token == Kind.Identifier))
        {
            return true;
        }

        if ((((this.token == Kind.YieldKeyword) && this.inYieldContext()) || ((this.token == Kind.AwaitKeyword) && this.inAwaitContext())))
        {
            return false;
        }

        return (this.token > Kind.LastReservedWord);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6352:1.
    private bool isBindingIdentifier()
    {
        return ((this.token == Kind.Identifier) || (this.token > Kind.LastReservedWord));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6357:1.
    private bool isImportAttributeName()
    {
        return (tokenIsIdentifierOrKeyword(this.token) || (this.token == Kind.StringLiteral));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6361:1.
    private bool isBinaryOperator()
    {
        if ((this.inDisallowInContext() && (this.token == Kind.InKeyword)))
        {
            return false;
        }

        return (Ast.GetBinaryOperatorPrecedence(this.token) != OperatorPrecedence.Invalid);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6368:1.
    private bool isValidHeritageClauseObjectLiteral()
    {
        return this.lookAhead((parserCallback) => parserCallback.nextIsValidHeritageClauseObjectLiteral());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6372:1.
    private bool nextIsValidHeritageClauseObjectLiteral()
    {
        if ((this.nextToken() == Kind.CloseBraceToken))
        {
            var next = this.nextToken();
            return ((((next == Kind.CommaToken) || (next == Kind.OpenBraceToken)) || (next == Kind.ExtendsKeyword)) || (next == Kind.ImplementsKeyword));
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6387:1.
    private bool isHeritageClause()
    {
        return ((this.token == Kind.ExtendsKeyword) || (this.token == Kind.ImplementsKeyword));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6391:1.
    private bool isHeritageClauseExtendsOrImplementsKeyword()
    {
        return (this.isHeritageClause() && this.lookAhead((parserCallback) => parserCallback.nextIsStartOfExpression()));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6395:1.
    private bool nextIsStartOfExpression()
    {
        this.nextToken();
        return this.isStartOfExpression();
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6400:1.
    private bool isUsingDeclaration()
    {
        return this.lookAhead((p) =>
        {
            return p.nextTokenIsBindingIdentifierOrStartOfDestructuringOnSameLine(false);
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6409:1.
    private bool nextTokenIsEqualsOrSemicolonOrColonToken()
    {
        this.nextToken();
        return (((this.token == Kind.EqualsToken) || (this.token == Kind.SemicolonToken)) || (this.token == Kind.ColonToken));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6414:1.
    private bool nextTokenIsBindingIdentifierOrStartOfDestructuringOnSameLine(bool disallowOf)
    {
        this.nextToken();
        if ((disallowOf && (this.token == Kind.OfKeyword)))
        {
            return this.lookAhead((parserCallback) => parserCallback.nextTokenIsEqualsOrSemicolonOrColonToken());
        }

        return (((this.isBindingIdentifier() || (this.token == Kind.OpenBraceToken))) && !this.hasPrecedingLineBreak());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6422:1.
    private bool nextTokenIsBindingIdentifierOrStartOfDestructuringOnSameLineDisallowOf()
    {
        return this.nextTokenIsBindingIdentifierOrStartOfDestructuringOnSameLine(true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6426:1.
    private bool isAwaitUsingDeclaration()
    {
        return this.lookAhead((parserCallback) => parserCallback.nextIsUsingKeywordThenBindingIdentifierOrStartOfObjectDestructuringOnSameLine());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6430:1.
    private bool nextIsUsingKeywordThenBindingIdentifierOrStartOfObjectDestructuringOnSameLine()
    {
        return ((this.nextToken() == Kind.UsingKeyword) && this.nextTokenIsBindingIdentifierOrStartOfDestructuringOnSameLine(false));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6434:1.
    private bool nextTokenIsTokenStringLiteral()
    {
        return (this.nextToken() == Kind.StringLiteral);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6438:1.
    private void setContextFlags(NodeFlags flags, bool value)
    {
        if (value)
        {
            this.contextFlags |= flags;
        }
        else
        {
            this.contextFlags &= ~(flags);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6446:1.
    private T doInContext<T>(NodeFlags flags, bool value, Func<Parser, T> f)
    {
        var saveContextFlags = this.contextFlags;
        this.setContextFlags(flags, value);
        var result = f(this);
        this.contextFlags = saveContextFlags;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6454:1.
    private bool inYieldContext()
    {
        return ((this.contextFlags & NodeFlags.YieldContext) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6458:1.
    private bool inDisallowInContext()
    {
        return ((this.contextFlags & NodeFlags.DisallowInContext) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6462:1.
    private bool inDisallowConditionalTypesContext()
    {
        return ((this.contextFlags & NodeFlags.DisallowConditionalTypesContext) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6466:1.
    private bool inDecoratorContext()
    {
        return ((this.contextFlags & NodeFlags.DecoratorContext) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6470:1.
    private bool inAwaitContext()
    {
        return ((this.contextFlags & NodeFlags.AwaitContext) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6474:1.
    private TextRange skipRangeTrivia(TextRange textRange)
    {
        return Core.NewTextRange(Scanner.SkipTriviaByteString(this.sourceText, textRange.Pos()), textRange.End());
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6478:1.
    private static bool isReservedWord(Kind token)
    {
        return ((Kind.FirstReservedWord <= token) && (token <= Kind.LastReservedWord));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6482:1.
    private static Slice<Diagnostic> attachFileToDiagnostics(Slice<Diagnostic> diagnostics, SourceFile file)
    {
        {
            foreach (var d in GoRuntime.RangeValues(diagnostics))
            {
                d.SetFile(file);
                {
                    foreach (var range18 in GoRuntime.Range(d.RelatedInformation()))
                    {
                        var _2 = range18.Key;
                        var r = range18.Value;
                        r.SetFile(file);
                    }
                }
            }
        }

        return diagnostics;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6492:1.
    private static Slice<Pragma> getCommentPragmas(NodeFactory f, ByteStringView sourceText)
    {
        Slice<Pragma> pragmas = default;
        {
            foreach (var commentRange in GoRuntime.RangeKeys(Scanner.GetLeadingCommentRangesByteString(sourceText, 0)))
            {
                var comment = GoRuntime.Slice(sourceText, commentRange.Pos, commentRange.End);
                pragmas = GoRuntime.Append(pragmas, extractPragmas(commentRange, comment));
            }
        }

        return pragmas;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6500:1.
    private static Slice<Pragma> extractPragmas(CommentRange commentRange, ByteStringView text)
    {
        if ((commentRange.Kind == Kind.SingleLineCommentTrivia))
        {
            var pos = 2;
            var tripleSlash = match(text, pos, "/"u8);
            if (tripleSlash)
            {
                pos++;
            }

            pos = skipBlanks(text, pos);
            if ((tripleSlash && match(text, pos, "<"u8)))
            {
                var tagName = extractName(text, (pos + 1));
                if ((!tagName.EqualsBytes("reference"u8)))
                {
                    return default;
                }

                pos += 10;
                var args = new Dictionary<ByteStringKey, PragmaArgument>(ByteStringKey.Comparer);
                while (true)
                {
                    pos = skipBlanks(text, pos);
                    if (match(text, pos, "/>"u8))
                    {
                        break;
                    }

                    var argName = extractName(text, pos);
                    if ((argName.Length == 0))
                    {
                        break;
                    }

                    pos = skipBlanks(text, (pos + argName.Length));
                    if (!match(text, pos, "="u8))
                    {
                        break;
                    }

                    pos = skipBlanks(text, (pos + 1));
                    var tuple20 = extractQuotedString(text, pos);
                    var value = tuple20.Item1;
                    var ok = tuple20.Item2;
                    if (!ok)
                    {
                        break;
                    }

                    args[ByteStringKey.FromValue(argName)] = new PragmaArgument
                    {
                        NameValue = argName,
                        ValueValue = value,
                        TextRange = Core.NewTextRange(((commentRange.Pos + pos) + 1), (((commentRange.Pos + pos) + 1) + value.Length))
                    };
                    pos += (value.Length + 2);
                }

                return Slice<Pragma>.FromArray([
                    new Pragma
                    {
                        CommentRange = commentRange,
                        NameValue = ByteStringValue.FromString("reference"),
                        Args = args
                    }
                ]);
            }

            if (match(text, pos, "@"u8))
            {
                pos++;
                var pragmaName = extractName(text, pos);
                if (!(((pragmaName.EqualsBytes("ts-check"u8)) || (pragmaName.EqualsBytes("ts-nocheck"u8)))))
                {
                    return default;
                }

                return Slice<Pragma>.FromArray([
                    new Pragma
                    {
                        CommentRange = commentRange,
                        NameValue = pragmaName
                    }
                ]);
            }
        }

        if ((commentRange.Kind == Kind.MultiLineCommentTrivia))
        {
            text = GoStrings.TrimSuffix(text, "*/"u8);
            var pos2 = 2;
            Slice<Pragma> pragmas = default;
            while (true)
            {
                {
                    pos2 = skipTo(text, pos2, "@"u8);
                    if ((pos2 < 0))
                    {
                        break;
                    }
                }

                var namePos = (pos2 + 1);
                var nameEnd = skipNonBlanks(text, namePos);
                if ((nameEnd == namePos))
                {
                    pos2++;
                    continue;
                }

                var lineEnd = lineEndPos(text, pos2);
                var pragmaName2 = GoStrings.ToLower(ByteStringValue.FromView(text.Slice(namePos, nameEnd)));
                if (((((pragmaName2.EqualsBytes("jsx"u8)) || (pragmaName2.EqualsBytes("jsxfrag"u8))) || (pragmaName2.EqualsBytes("jsximportsource"u8))) || (pragmaName2.EqualsBytes("jsxruntime"u8))))
                {
                    var start = skipBlanks(text, nameEnd);
                    var argEnd = skipNonBlanks(text, start);
                    if ((argEnd != start))
                    {
                        var args2 = new Dictionary<ByteStringKey, PragmaArgument>(ByteStringKey.Comparer);
                        args2[ByteStringKey.FromString("factory")] = new PragmaArgument
                        {
                            NameValue = ByteStringValue.FromString("factory"),
                            ValueValue = ByteStringValue.FromView(text.Slice(start, argEnd)),
                            TextRange = Core.NewTextRange((commentRange.Pos + start), (commentRange.Pos + argEnd))
                        };
                        pragmas = GoRuntime.Append(pragmas, new Pragma { CommentRange = commentRange, NameValue = pragmaName2, Args = args2 });
                    }
                }

                pos2 = lineEnd;
            }

            return pragmas;
        }

        return default;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6604:1.
    private static bool match(ByteStringView text, int pos, ReadOnlySpan<byte> s)
    {
        return GoStrings.HasPrefix(text.AsSpan(pos), s);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6608:1.
    private static int skipBlanks(ByteStringView text, int pos)
    {
        while (((pos < GoRuntime.Len(text)) && (((text[pos] == ' ') || (text[pos] == '\t')))))
        {
            pos++;
        }

        return pos;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6615:1.
    private static int skipNonBlanks(ByteStringView text, int pos)
    {
        while (((pos < GoRuntime.Len(text)) && (((((text[pos] != ' ') && (text[pos] != '\t')) && (text[pos] != '\r')) && (text[pos] != '\n')))))
        {
            pos++;
        }

        return pos;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6622:1.
    private static int skipTo(ByteStringView text, int pos, ReadOnlySpan<byte> s)
    {
        if ((pos >= GoRuntime.Len(text)))
        {
            return -1;
        }

        var i = GoStrings.Index(text.AsSpan(pos), s);
        if ((i < 0))
        {
            return -1;
        }

        return (pos + i);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6633:1.
    private static int lineEndPos(ByteStringView text, int pos)
    {
        while ((pos < GoRuntime.Len(text)))
        {
            var tuple21 = Utf8.DecodeRuneInString(text.AsSpan(pos));
            var ch = tuple21.Item1;
            var size = tuple21.Item2;
            if (StringUtil.IsLineBreak(ch))
            {
                return pos;
            }

            pos += size;
        }

        return GoRuntime.Len(text);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6644:1.
    private static ByteStringValue extractName(ByteStringView text, int pos)
    {
        var start = pos;
        while (((pos < GoRuntime.Len(text)) && (((((text[pos] >= 'A') && (text[pos] <= 'Z')) || ((text[pos] >= 'a') && (text[pos] <= 'z'))) || (text[pos] == '-')))))
        {
            pos++;
        }

        return GoStrings.ToLower(ByteStringValue.FromView(text.Slice(start, pos)));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6652:1.
    private static (ByteStringValue, bool) extractQuotedString(ByteStringView text, int pos)
    {
        if ((pos == GoRuntime.Len(text)))
        {
            return (default, false);
        }

        var quote = text[pos];
        if (((quote != '\'') && (quote != '"')))
        {
            return (default, false);
        }

        pos++;
        var start = pos;
        while (((pos < GoRuntime.Len(text)) && (text[pos] != quote)))
        {
            pos++;
        }

        if ((pos == GoRuntime.Len(text)))
        {
            return (default, false);
        }

        return (ByteStringValue.FromView(text.Slice(start, pos)), true);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6671:1.
    private void processPragmasIntoFields(SourceFile context)
    {
        context.CheckJsDirective = null;
        context.ReferencedFiles = default;
        context.TypeReferenceDirectives = default;
        context.LibReferenceDirectives = default;
        {
            foreach (var pragma in GoRuntime.RangeValues(context.Pragmas))
            {
                switch (true)
                {
                    case true when pragma.NameValue.EqualsBytes("reference"u8):
                    {
                        var typesOk = ByteStringValue.FromString("types").TryGetValue(pragma.Args, out var types);
                        var libOk = ByteStringValue.FromString("lib").TryGetValue(pragma.Args, out var lib);
                        var pathOk = ByteStringValue.FromString("path").TryGetValue(pragma.Args, out var path);
                        var resolutionModeOk = ByteStringValue.FromString("resolution-mode").TryGetValue(pragma.Args, out var resolutionMode);
                        var preserveOk = ByteStringValue.FromString("preserve").TryGetValue(pragma.Args, out var preserve);
                        var noDefaultLibOk = ByteStringValue.FromString("no-default-lib").TryGetValue(pragma.Args, out var noDefaultLib);
                        switch (true)
                        {
                            case true when (noDefaultLibOk && (noDefaultLib.ValueValue.EqualsBytes("true"u8))):
                            {
                                break;
                            }

                            case true when typesOk:
                            {
                                ResolutionMode parsed = default;
                                if (resolutionModeOk)
                                {
                                    parsed = this.parseResolutionMode(resolutionMode.ValueValue, resolutionMode.Pos(), resolutionMode.End());
                                }

                                context.TypeReferenceDirectives = GoRuntime.Append(context.TypeReferenceDirectives, new FileReference { TextRange = types.TextRange, FileNameValue = types.ValueValue, ResolutionMode = parsed, Preserve = (preserveOk && (preserve.ValueValue.EqualsBytes("true"u8))) });
                                break;
                            }

                            case true when libOk:
                            {
                                context.LibReferenceDirectives = GoRuntime.Append(context.LibReferenceDirectives, new FileReference { TextRange = lib.TextRange, FileNameValue = lib.ValueValue, Preserve = (preserveOk && (preserve.ValueValue.EqualsBytes("true"u8))) });
                                break;
                            }

                            case true when pathOk:
                            {
                                context.ReferencedFiles = GoRuntime.Append(context.ReferencedFiles, new FileReference { TextRange = path.TextRange, FileNameValue = path.ValueValue, Preserve = (preserveOk && (preserve.ValueValue.EqualsBytes("true"u8))) });
                                break;
                            }

                            default:
                            {
                                this.parseErrorAtRange(pragma.TextRange, Diagnostics.Invalid_reference_directive_syntax);
                                break;
                            }
                        }

                        break;
                    }

                    case true when pragma.NameValue.EqualsBytes("ts-check"u8):
                    case true when pragma.NameValue.EqualsBytes("ts-nocheck"u8):
                    {
                        if (((context.CheckJsDirective == null) || (pragma.TextRange.Pos() > context.CheckJsDirective.Range.Pos)))
                        {
                            context.CheckJsDirective = new CheckJsDirective
                            {
                                Enabled = (pragma.NameValue.EqualsBytes("ts-check"u8)),
                                Range = pragma.CommentRange
                            };
                        }

                        break;
                    }

                    case true when pragma.NameValue.EqualsBytes("jsx"u8):
                    case true when pragma.NameValue.EqualsBytes("jsxfrag"u8):
                    case true when pragma.NameValue.EqualsBytes("jsximportsource"u8):
                    case true when pragma.NameValue.EqualsBytes("jsxruntime"u8):
                    {
                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException(("Unhandled pragma kind: " + pragma.Name));
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6731:1.
    private ResolutionMode parseResolutionMode(ByteStringValue mode, int pos, int end)
    {
        ResolutionMode resolutionKind = default;
        if ((mode.EqualsBytes("import"u8)))
        {
            resolutionKind = ResolutionMode.ESNext;
            return resolutionKind;
        }

        if ((mode.EqualsBytes("require"u8)))
        {
            resolutionKind = ResolutionMode.CommonJS;
            return resolutionKind;
        }

        this.parseErrorAt(pos, end, Diagnostics.X_resolution_mode_should_be_either_require_or_import);
        return resolutionKind;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6744:1.
    private void jsErrorAtRange(TextRange loc, Message message, params object[] args)
    {
        this.jsDiagnostics = GoRuntime.Append(this.jsDiagnostics, Ast.NewDiagnostic(null, Core.NewTextRange(Scanner.SkipTriviaByteString(this.sourceText, loc.Pos()), loc.End()), message, args));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6748:1.
    private void checkJSDecoratorSyntax(Node node)
    {
        var modifiers = node.ModifierNodes();
        if ((GoRuntime.Len(modifiers) == 0))
        {
            return;
        }

        if (Ast.CanHaveIllegalDecorators(node))
        {
            {
                foreach (var modifier in GoRuntime.RangeValues(modifiers))
                {
                    if (Ast.IsDecorator(modifier))
                    {
                        this.jsErrorAtRange(modifier.Loc, Diagnostics.Decorators_are_not_valid_here);
                        break;
                    }
                }
            }
        }
        else
        {
            if (Ast.CanHaveDecorators(node))
            {
                var decoratorIndex = Core.FindIndex(modifiers, Ast.IsDecorator);
                if ((decoratorIndex >= 0))
                {
                    if (Ast.IsClassDeclaration(node))
                    {
                        var exportIndex = Core.FindIndex(modifiers, isExportModifier);
                        if ((exportIndex >= 0))
                        {
                            var defaultIndex = Core.FindIndex(modifiers, (m) =>
                            {
                                return (m.Kind == Kind.DefaultKeyword);
                            });
                            if ((((decoratorIndex > exportIndex) && (defaultIndex >= 0)) && (decoratorIndex < defaultIndex)))
                            {
                                this.jsErrorAtRange(modifiers[decoratorIndex].Loc, Diagnostics.Decorators_are_not_valid_here);
                            }
                            else
                            {
                                if ((decoratorIndex < exportIndex))
                                {
                                    var trailingDecoratorIndex = -1;
                                    {
                                        var i = exportIndex;
                                        for (; (i < GoRuntime.Len(modifiers)); i++)
                                        {
                                            if (Ast.IsDecorator(modifiers[i]))
                                            {
                                                trailingDecoratorIndex = i;
                                                break;
                                            }
                                        }
                                    }

                                    if ((trailingDecoratorIndex >= 0))
                                    {
                                        var diag = Ast.NewDiagnostic(null, Core.NewTextRange(Scanner.SkipTriviaByteString(this.sourceText, modifiers[trailingDecoratorIndex].Loc.Pos()), modifiers[trailingDecoratorIndex].Loc.End()), Diagnostics.Decorators_may_not_appear_after_export_or_export_default_if_they_also_appear_before_export);
                                        diag.AddRelatedInfo(Ast.NewDiagnostic(null, Core.NewTextRange(Scanner.SkipTriviaByteString(this.sourceText, modifiers[decoratorIndex].Loc.Pos()), modifiers[decoratorIndex].Loc.End()), Diagnostics.Decorator_used_before_export_here));
                                        this.jsDiagnostics = GoRuntime.Append(this.jsDiagnostics, diag);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/parser.go:6802:1.
    private Node checkJSSyntax(Node node)
    {
        if ((((node.Flags & NodeFlags.JavaScriptFile) == 0) || ((node.Flags & ((NodeFlags.JSDoc | NodeFlags.Reparsed))) != 0)))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.MethodDeclaration:
            {
                {
                    var token = node.QuestionToken();
                    if ((((token != null) && ((token.Flags & NodeFlags.Reparsed) == 0)) && Ast.IsQuestionToken(token)))
                    {
                        this.jsErrorAtRange(token.Loc, Diagnostics.The_0_modifier_can_only_be_used_in_TypeScript_files, "?");
                    }
                }

                goto case Kind.MethodSignature;
            }

            case Kind.MethodSignature:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionExpression:
            case Kind.FunctionDeclaration:
            case Kind.ArrowFunction:
            case Kind.VariableDeclaration:
            case Kind.IndexSignature:
            {
                if ((Ast.IsFunctionLike(node) && (node.Body() == null)))
                {
                    this.jsErrorAtRange(node.Loc, Diagnostics.Signature_declarations_can_only_be_used_in_TypeScript_files);
                }
                else
                {
                    {
                        var t = node.Type();
                        if (((t != null) && ((t.Flags & NodeFlags.Reparsed) == 0)))
                        {
                            this.jsErrorAtRange(t.Loc, Diagnostics.Type_annotations_can_only_be_used_in_TypeScript_files);
                        }
                    }
                }

                break;
            }

            case Kind.ImportDeclaration:
            {
                {
                    var clause = node.ImportClause();
                    if (((clause != null) && clause.IsTypeOnly()))
                    {
                        this.jsErrorAtRange(node.Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, "import type");
                    }
                }

                break;
            }

            case Kind.ExportDeclaration:
            {
                if (node.IsTypeOnly())
                {
                    this.jsErrorAtRange(node.Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, "export type");
                }

                break;
            }

            case Kind.ImportSpecifier:
            {
                if (node.IsTypeOnly())
                {
                    this.jsErrorAtRange(node.Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, "import...type");
                }

                break;
            }

            case Kind.ExportSpecifier:
            {
                if (node.IsTypeOnly())
                {
                    this.jsErrorAtRange(node.Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, "export...type");
                }

                break;
            }

            case Kind.ImportEqualsDeclaration:
            {
                this.jsErrorAtRange(node.Loc, Diagnostics.X_import_can_only_be_used_in_TypeScript_files);
                break;
            }

            case Kind.ExportAssignment:
            {
                if (node.AsExportAssignment().IsExportEquals)
                {
                    this.jsErrorAtRange(node.Loc, Diagnostics.X_export_can_only_be_used_in_TypeScript_files);
                }

                break;
            }

            case Kind.HeritageClause:
            {
                if ((node.AsHeritageClause().Token == Kind.ImplementsKeyword))
                {
                    this.jsErrorAtRange(node.Loc, Diagnostics.X_implements_clauses_can_only_be_used_in_TypeScript_files);
                }

                break;
            }

            case Kind.InterfaceDeclaration:
            {
                this.jsErrorAtRange(node.Name().Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, "interface");
                break;
            }

            case Kind.ModuleDeclaration:
            {
                this.jsErrorAtRange(node.Name().Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, Scanner.TokenToString(node.AsModuleDeclaration().Keyword));
                break;
            }

            case Kind.TypeAliasDeclaration:
            {
                this.jsErrorAtRange(node.Name().Loc, Diagnostics.Type_aliases_can_only_be_used_in_TypeScript_files);
                break;
            }

            case Kind.EnumDeclaration:
            {
                this.jsErrorAtRange(node.Name().Loc, Diagnostics.X_0_declarations_can_only_be_used_in_TypeScript_files, "enum");
                break;
            }

            case Kind.NonNullExpression:
            {
                this.jsErrorAtRange(node.Loc, Diagnostics.Non_null_assertions_can_only_be_used_in_TypeScript_files);
                break;
            }

            case Kind.AsExpression:
            {
                this.jsErrorAtRange(node.Type().Loc, Diagnostics.Type_assertion_expressions_can_only_be_used_in_TypeScript_files);
                break;
            }

            case Kind.SatisfiesExpression:
            {
                this.jsErrorAtRange(node.Type().Loc, Diagnostics.Type_satisfaction_expressions_can_only_be_used_in_TypeScript_files);
                break;
            }
        }

        this.checkJSDecoratorSyntax(node);
        switch (node.Kind)
        {
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.MethodDeclaration:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionExpression:
            case Kind.FunctionDeclaration:
            case Kind.ArrowFunction:
            {
                {
                    var list = node.TypeParameterList();
                    if (((list != null) && Core.Some(list.Nodes, (n) =>
                    {
                        return ((n.Flags & NodeFlags.Reparsed) == 0);
                    })))
                    {
                        this.jsErrorAtRange(list.Loc, Diagnostics.Type_parameter_declarations_can_only_be_used_in_TypeScript_files);
                    }
                }

                goto case Kind.VariableStatement;
            }

            case Kind.VariableStatement:
            case Kind.PropertyDeclaration:
            {
                {
                    foreach (var modifier in GoRuntime.RangeValues(node.ModifierNodes()))
                    {
                        if (((((modifier.Flags & NodeFlags.Reparsed) == 0) && (modifier.Kind != Kind.Decorator)) && ((Ast.ModifierToFlag(modifier.Kind) & ModifierFlags.JavaScript) == 0)))
                        {
                            this.jsErrorAtRange(modifier.Loc, Diagnostics.The_0_modifier_can_only_be_used_in_TypeScript_files, Scanner.TokenToString(modifier.Kind));
                        }
                    }
                }

                break;
            }

            case Kind.Parameter:
            {
                if (Core.Some(node.ModifierNodes(), Ast.IsModifier))
                {
                    this.jsErrorAtRange(node.Modifiers().Loc, Diagnostics.Parameter_modifiers_can_only_be_used_in_TypeScript_files);
                }

                break;
            }

            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.ExpressionWithTypeArguments:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxOpeningElement:
            case Kind.TaggedTemplateExpression:
            {
                {
                    var list2 = node.TypeArgumentList();
                    if (((list2 != null) && Core.Some(list2.Nodes, (n2) =>
                    {
                        return ((n2.Flags & NodeFlags.Reparsed) == 0);
                    })))
                    {
                        this.jsErrorAtRange(list2.Loc, Diagnostics.Type_arguments_can_only_be_used_in_TypeScript_files);
                    }
                }

                break;
            }
        }

        return node;
    }
}

// Hand-adapted declarations from parser.go; bounded runtime/API bridges.
internal enum ParsingContext
{
    SourceElements,
    BlockStatements,
    SwitchClauses,
    SwitchClauseStatements,
    TypeMembers,
    ClassMembers,
    EnumMembers,
    HeritageClauseElement,
    VariableDeclarations,
    ObjectBindingElements,
    ArrayBindingElements,
    ArgumentExpressions,
    ObjectLiteralMembers,
    JsxAttributes,
    JsxChildren,
    ArrayLiteralMembers,
    Parameters,
    JSDocParameters,
    RestProperties,
    TypeParameters,
    TypeArguments,
    TupleElementTypes,
    HeritageClauses,
    ImportOrExportSpecifiers,
    ImportAttributes,
    JSDocComment,
    Count
}

[Flags]
internal enum jsdocScannerInfo
{
    HasJSDoc = 1,
    HasDeprecated = 2,
    HasSeeOrLink = 4
}

internal sealed class JSDocInfo
{
    public Node parent;
    public Slice<Node> jsDocs;
}

internal struct ParserState
{
    public ScannerState scannerState;
    public NodeFlags contextFlags;
    public int diagnosticsLen, jsDiagnosticsLen, jsdocInfosLen, reparsedClonesLen;
    public bool statementHasAwaitIdentifier, hasParseError;
}

public partial class Parser
{
    private Scanner scanner;
    private NodeFactory factory = new();
    private SourceFileParseOptions opts;
    private ByteStringView sourceText = "";
    private ScriptKind scriptKind;
    private LanguageVariant languageVariant;
    private Slice<Diagnostic> diagnostics, jsDiagnostics, jsdocDiagnostics;
    private Kind token;
    private NodeFlags sourceFlags, contextFlags;
    private int parsingContexts;
    private bool statementHasAwaitIdentifier, hasDeprecatedTag, hasParseError;
    private int identifierCount;
    // collections.Set[int] begins nil and owns storage only on the first Add.
    private HashSet<int> notParenthesizedArrow;
    private GoArena<Node> nodeSliceArena = new();
    private GoArena<ByteStringView> stringSliceArena = new();
    // parser.go:90: private append/reslice storage; only the final JSDoc cache escapes.
    private Slice<JSDocInfo> jsdocInfos;
    private Slice<int> possibleAwaitSpans;
    private Slice<ByteStringView> jsdocCommentsSpace, jsdocTagCommentsSpace;
    private Slice<CommentRange> jsdocCommentRangesSpace;
    private Slice<Node> jsdocTagCommentsPartsSpace, reparseList;
    private Slice<Node> reparsedClones;
    private Node currentParent;
    private Visitor setParentFromContext;
    private static readonly string[] viableKeywordSuggestions = Scanner.GetViableKeywordSuggestions();
    // parser.go:111/6490: missing lists have a distinct zero-length cap-one backing slot.
    private static readonly Slice<Node> missingListNodes = Slice<Node>.Make(0, 1);
    // Go parser-pool adapter: each request obtains an isolated managed parser.
    private static Parser getParser() => newParser();
    private static bool isMissingNodeList(NodeList list) => list != null && list.Nodes.Capacity == 1 && list.Nodes.Slice2(0, 1).SameStorageRange(missingListNodes.Slice2(0, 1));
    // Supported contract: JS1, JSX2, TS3, TSX4, JSON6. Unknown follows upstream failure; reserved numeric casts are outside the tested contract.
    public static SourceFile ParseSourceFile(SourceFileParseOptions opts, string sourceText, ScriptKind scriptKind) => ParseSourceFileOwned(opts, ByteStringView.FromUnicode(sourceText), scriptKind);
    public static SourceFile ParseSourceFile(SourceFileParseOptions opts, byte[] sourceText, ScriptKind scriptKind) => ParseSourceFileOwned(opts, ByteStringView.CopyFrom(sourceText), scriptKind);
    public static Node ParseIsolatedEntityName(string text) => ParseIsolatedEntityNameRaw(ByteStringView.FromUnicode(text));
    public static Node ParseIsolatedEntityName(byte[] text) => ParseIsolatedEntityNameRaw(ByteStringView.CopyFrom(text));
    internal static Node ParseIsolatedEntityName(ByteStringValue text)
    {
        if (text.IsNull) throw new ArgumentNullException(nameof(text));
        return ParseIsolatedEntityNameRaw(text.IsBytes ? text.ByteView : ByteStringView.FromGoBytes(text.Materialize()));
    }
}
