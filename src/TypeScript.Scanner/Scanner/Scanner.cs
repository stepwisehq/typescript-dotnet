// Copyright (c) Microsoft Corporation. Licensed under the Apache License 2.0.
// See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/scanner/scanner.go; modified C# translation, upstream fed0bf24149f; see UPSTREAM.md.
using System;
using System.Collections.Generic;

namespace TypeScript.Scanner;
public partial class Scanner
{
    // Existing one-char-per-byte text; slices share storage and bound scanning.
    private ByteStringView text = "";
    private int end, pos, fullStartPos, tokenStart, skipJSDocLeadingAsterisks;
    private LanguageVariant languageVariant;
    private ScriptTarget scriptTarget;
    private ErrorCallback? onError;
    private bool skipTrivia = true;
    private Kind token;
    private global::TypeScript.Scanner.TokenFlags tokenFlags;
    // scanner.go tokenValue is a string descriptor. Unchanged token text borrows
    // source bytes; decoded output owns a string. Legacy raw string projection is
    // cached on first access, without replacing the borrowed descriptor.
    private string? tokenValueString = "";
    private ByteStringView tokenValueView;
    private bool tokenValueIsView;
    private string tokenValue
    {
        get => tokenValueString ??= tokenValueView.Materialize();
        set { tokenValueString = value; tokenValueView = default; tokenValueIsView = false; }
    }
    private void setTokenValueView(ByteStringView value)
    {
        tokenValueString = null;
        tokenValueView = value;
        tokenValueIsView = true;
    }
    private Slice<CommentDirective> commentDirectives;
    private Dictionary<string, string>? numberCache, hexNumberCache, hexDigitCache;
    private const int mergeConflictMarkerLength = 7, maxAsciiCharacter = 127;
    private static readonly Dictionary<string, Kind> textToKeyword = new(StringComparer.Ordinal)
    {
        ["abstract"] = Kind.AbstractKeyword,
        ["accessor"] = Kind.AccessorKeyword,
        ["any"] = Kind.AnyKeyword,
        ["as"] = Kind.AsKeyword,
        ["asserts"] = Kind.AssertsKeyword,
        ["assert"] = Kind.AssertKeyword,
        ["bigint"] = Kind.BigIntKeyword,
        ["boolean"] = Kind.BooleanKeyword,
        ["break"] = Kind.BreakKeyword,
        ["case"] = Kind.CaseKeyword,
        ["catch"] = Kind.CatchKeyword,
        ["class"] = Kind.ClassKeyword,
        ["continue"] = Kind.ContinueKeyword,
        ["const"] = Kind.ConstKeyword,
        ["constructor"] = Kind.ConstructorKeyword,
        ["debugger"] = Kind.DebuggerKeyword,
        ["declare"] = Kind.DeclareKeyword,
        ["default"] = Kind.DefaultKeyword,
        ["defer"] = Kind.DeferKeyword,
        ["delete"] = Kind.DeleteKeyword,
        ["do"] = Kind.DoKeyword,
        ["else"] = Kind.ElseKeyword,
        ["enum"] = Kind.EnumKeyword,
        ["export"] = Kind.ExportKeyword,
        ["extends"] = Kind.ExtendsKeyword,
        ["false"] = Kind.FalseKeyword,
        ["finally"] = Kind.FinallyKeyword,
        ["for"] = Kind.ForKeyword,
        ["from"] = Kind.FromKeyword,
        ["function"] = Kind.FunctionKeyword,
        ["get"] = Kind.GetKeyword,
        ["if"] = Kind.IfKeyword,
        ["immediate"] = Kind.ImmediateKeyword,
        ["implements"] = Kind.ImplementsKeyword,
        ["import"] = Kind.ImportKeyword,
        ["in"] = Kind.InKeyword,
        ["infer"] = Kind.InferKeyword,
        ["instanceof"] = Kind.InstanceOfKeyword,
        ["interface"] = Kind.InterfaceKeyword,
        ["intrinsic"] = Kind.IntrinsicKeyword,
        ["is"] = Kind.IsKeyword,
        ["keyof"] = Kind.KeyOfKeyword,
        ["let"] = Kind.LetKeyword,
        ["module"] = Kind.ModuleKeyword,
        ["namespace"] = Kind.NamespaceKeyword,
        ["never"] = Kind.NeverKeyword,
        ["new"] = Kind.NewKeyword,
        ["null"] = Kind.NullKeyword,
        ["number"] = Kind.NumberKeyword,
        ["object"] = Kind.ObjectKeyword,
        ["package"] = Kind.PackageKeyword,
        ["private"] = Kind.PrivateKeyword,
        ["protected"] = Kind.ProtectedKeyword,
        ["public"] = Kind.PublicKeyword,
        ["override"] = Kind.OverrideKeyword,
        ["out"] = Kind.OutKeyword,
        ["readonly"] = Kind.ReadonlyKeyword,
        ["require"] = Kind.RequireKeyword,
        ["global"] = Kind.GlobalKeyword,
        ["return"] = Kind.ReturnKeyword,
        ["satisfies"] = Kind.SatisfiesKeyword,
        ["set"] = Kind.SetKeyword,
        ["source"] = Kind.SourceKeyword,
        ["static"] = Kind.StaticKeyword,
        ["string"] = Kind.StringKeyword,
        ["super"] = Kind.SuperKeyword,
        ["switch"] = Kind.SwitchKeyword,
        ["symbol"] = Kind.SymbolKeyword,
        ["this"] = Kind.ThisKeyword,
        ["throw"] = Kind.ThrowKeyword,
        ["true"] = Kind.TrueKeyword,
        ["try"] = Kind.TryKeyword,
        ["type"] = Kind.TypeKeyword,
        ["typeof"] = Kind.TypeOfKeyword,
        ["undefined"] = Kind.UndefinedKeyword,
        ["unique"] = Kind.UniqueKeyword,
        ["unknown"] = Kind.UnknownKeyword,
        ["using"] = Kind.UsingKeyword,
        ["var"] = Kind.VarKeyword,
        ["void"] = Kind.VoidKeyword,
        ["while"] = Kind.WhileKeyword,
        ["with"] = Kind.WithKeyword,
        ["yield"] = Kind.YieldKeyword,
        ["async"] = Kind.AsyncKeyword,
        ["await"] = Kind.AwaitKeyword,
        ["of"] = Kind.OfKeyword,
    };
    private static readonly Dictionary<string, Kind> textToToken = CreateTextToToken();
    private static readonly string[] tokenToText = CreateTokenToText();
    public Kind Token()
    {
        return this.token;
    }

    public TokenFlags TokenFlags()
    {
        return this.tokenFlags;
    }

    public int TokenFullStart()
    {
        return this.fullStartPos;
    }

    public int TokenStart()
    {
        return this.tokenStart;
    }

    public int TokenEnd()
    {
        return this.pos;
    }

    public TextRange TokenRange()
    {
        return new TextRange(this.tokenStart, this.pos);
    }

    public CommentDirective[]? CommentDirectives()
    {
        return this.commentDirectives.IsNil ? null : this.commentDirectives.AsSpan().ToArray();
    }

    public void ResetPos(int pos)
    {
        if (pos < 0)
        {
            throw new InvalidOperationException("Cannot reset token state to negative position");
        }

        this.pos = pos;
        this.fullStartPos = pos;
        this.tokenStart = pos;
    }

    public void ResetTokenState(int pos)
    {
        this.ResetPos(pos);
        this.token = Kind.Unknown;
        this.tokenValue = "";
        this.tokenFlags = global::TypeScript.Scanner.TokenFlags.None;
    }

    public void SetSkipJSDocLeadingAsterisks(bool skip)
    {
        if (skip)
        {
            this.skipJSDocLeadingAsterisks += 1;
        }
        else
        {
            this.skipJSDocLeadingAsterisks += -1;
        }
    }

    public void SetSkipTrivia(bool skip)
    {
        this.skipTrivia = skip;
    }

    public bool HasUnicodeEscape()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.UnicodeEscape) != 0;
    }

    public bool HasExtendedUnicodeEscape()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.ExtendedUnicodeEscape) != 0;
    }

    public bool HasPrecedingLineBreak()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingLineBreak) != 0;
    }

    public bool HasPrecedingJSDocComment()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocComment) != 0;
    }

    public bool HasPrecedingJSDocLeadingAsterisks()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocLeadingAsterisks) != 0;
    }

    public bool HasPrecedingJSDocWithDeprecatedTag()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithDeprecated) != 0;
    }

    public bool HasPrecedingJSDocWithSeeOrLink()
    {
        return (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithSeeOrLink) != 0;
    }

    // scanJSDocCommentForTags scans a JSDoc comment for @deprecated, @see, and @link tags,
    // setting the appropriate token flags. Called during scanning when a JSDoc comment is detected.
    private void scanJSDocCommentForTags(ByteStringView commentText)
    {
        while (true)
        {
            var i = commentText.AsSpan().IndexOf((byte)'@');
            if (i < 0)
            {
                return;
            }

            commentText = commentText.Slice(i + 1);
            if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithDeprecated) == 0 && hasJSDocTag(commentText, "deprecated"u8))
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithDeprecated;
            }

            if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithSeeOrLink) == 0 &&
                (hasJSDocTag(commentText, "see"u8) || hasJSDocTag(commentText, "link"u8) ||
                 hasJSDocTag(commentText, "linkcode"u8) || hasJSDocTag(commentText, "linkplain"u8)))
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithSeeOrLink;
            }

            if ((this.tokenFlags & (global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithDeprecated | global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithSeeOrLink)) == (global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithDeprecated | global::TypeScript.Scanner.TokenFlags.PrecedingJSDocWithSeeOrLink))
            {
                return;
            }
        }
    }

    // scanner.go:381. The source's ordered variadic tag tests use borrowed static
    // patterns at the caller; no temporary params array or text encoding is needed.
    // hasJSDocTag reports whether text starts with the given tag name followed
    // by a valid JSDoc tag terminator (whitespace, '}', '*', or end-of-string).
    private static bool hasJSDocTag(ByteStringView text, ReadOnlySpan<byte> tag)
    {
        if (!GoStrings.HasPrefix(text.AsSpan(), tag))
            return false;
        if (text.Length == tag.Length)
            return true;
        char ch = text[tag.Length];
        return ch is ' ' or '\t' or '\n' or '\r' or '}' or '*';
    }

    public void SetOnError(ErrorCallback? errorCallback)
    {
        this.onError = errorCallback;
        onErrorUsesByteStrings = false;
    }

    public void SetLanguageVariant(LanguageVariant languageVariant)
    {
        this.languageVariant = languageVariant;
    }

    public void SetScriptTarget(ScriptTarget scriptTarget)
    {
        this.scriptTarget = scriptTarget;
    }

    private ScriptTarget languageVersion()
    {
        if (this.scriptTarget == ScriptTarget.None)
        {
            return ScriptTarget.Latest;
        }

        return this.scriptTarget;
    }

    private void error(Message diagnostic)
    {
        this.errorAt(diagnostic, this.pos, 0);
    }

    private void errorAt(Message diagnostic, int pos, int length, params object[] args)
    {
        if (this.onError != null)
        {
            if (onErrorUsesByteStrings)
                this.onError(diagnostic, pos, length, args);
            else
                this.onError(diagnostic, pos, length, args.Select(arg => arg is string value ? (object)ScannerString.ToUtf8String(value) : arg).ToArray());
        }
    }

    // NOTE: even though this returns a rune, it only decodes the current byte.
    // It must be checked against utf8.RuneSelf to verify that a call to charAndSize
    // is not needed.
    private int charCode()
    {
        if (this.pos < this.end)
        {
            return (int)this.text[this.pos];
        }

        return -1;
    }

    // NOTE: this returns a rune, but only decodes the byte at the offset.
    private int charAt(int offset)
    {
        if (this.pos + offset < this.end)
        {
            return (int)this.text[this.pos + offset];
        }

        return -1;
    }

    private (int, int) charAndSize()
    {
        if (this.pos < this.end)
        {
            {
                int b = this.text[this.pos];
                if (b < Utf8.RuneSelf)
                {
                    return ((int)b, 1);
                }
            }
        }

        return Utf8.DecodeRuneInString(this.text.AsSpan(this.pos));
    }

    // scanASCIIWhile advances s.pos over the longest run of ASCII bytes for which
    // pred returns true. It stops at end-of-text, the first non-ASCII byte, or the
    // first byte where pred is false.
    private void scanASCIIWhile(Func<int, bool> pred)
    {
        var text = this.text.AsSpan(this.pos, this.end - this.pos);
        var i = 0;
        while (i < text.Length)
        {
            int b = text[i];
            if (b >= Utf8.RuneSelf || !pred(b))
            {
                break;
            }

            i++;
        }

        this.pos += i;
    }

    public Kind Scan()
    {
        this.fullStartPos = this.pos;
        this.tokenFlags = global::TypeScript.Scanner.TokenFlags.None;
        while (true)
        {
            var ch = this.charCode();
            this.tokenStart = this.pos;
            switch (ch)
            {
                case '\t':
                case '\v':
                case '\f':
                case ' ':
                {
                    this.pos++;
                    if (this.skipTrivia)
                    {
                        continue;
                    }

                    while (true)
                    {
                        var(whiteSpaceCh, size) = this.charAndSize();
                        if (!StringUtil.IsWhiteSpaceSingleLine(whiteSpaceCh))
                        {
                            break;
                        }

                        this.pos += size;
                    }

                    this.token = Kind.WhitespaceTrivia;
                    break;
                }

                case '\n':
                case '\r':
                {
                    this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingLineBreak;
                    if (this.skipTrivia)
                    {
                        this.pos++;
                        this.scanASCIIWhile((b) =>
                        {
                            return b == ' ' || b >= '\t' && b <= '\r';
                        });
                        continue;
                    }

                    if (ch == '\r' && this.charAt(1) == '\n')
                    {
                        this.pos += 2;
                    }
                    else
                    {
                        this.pos++;
                    }

                    this.token = Kind.NewLineTrivia;
                    break;
                }

                case '!':
                {
                    if (this.charAt(1) == '=')
                    {
                        if (this.charAt(2) == '=')
                        {
                            this.pos += 3;
                            this.token = Kind.ExclamationEqualsEqualsToken;
                        }
                        else
                        {
                            this.pos += 2;
                            this.token = Kind.ExclamationEqualsToken;
                        }
                    }
                    else
                    {
                        this.pos++;
                        this.token = Kind.ExclamationToken;
                    }

                    break;
                }

                case '"':
                case '\'':
                {
                    this.scanString(false);
                    this.token = Kind.StringLiteral;
                    break;
                }

                case '`':
                {
                    this.token = this.scanTemplateAndSetTokenValue(false);
                    break;
                }

                case '%':
                {
                    if (this.charAt(1) == '=')
                    {
                        this.pos += 2;
                        this.token = Kind.PercentEqualsToken;
                    }
                    else
                    {
                        this.pos++;
                        this.token = Kind.PercentToken;
                    }

                    break;
                }

                case '&':
                {
                    var next = this.charAt(1);
                    if (next == '&')
                    {
                        if (this.charAt(2) == '=')
                        {
                            this.pos += 3;
                            this.token = Kind.AmpersandAmpersandEqualsToken;
                        }
                        else
                        {
                            this.pos += 2;
                            this.token = Kind.AmpersandAmpersandToken;
                        }
                    }
                    else
                    {
                        if (next == '=')
                        {
                            this.pos += 2;
                            this.token = Kind.AmpersandEqualsToken;
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.AmpersandToken;
                        }
                    }

                    break;
                }

                case '(':
                {
                    this.pos++;
                    this.token = Kind.OpenParenToken;
                    break;
                }

                case ')':
                {
                    this.pos++;
                    this.token = Kind.CloseParenToken;
                    break;
                }

                case '*':
                {
                    var next = this.charAt(1);
                    if (next == '=')
                    {
                        this.pos += 2;
                        this.token = Kind.AsteriskEqualsToken;
                    }
                    else
                    {
                        if (next == '*')
                        {
                            if (this.charAt(2) == '=')
                            {
                                this.pos += 3;
                                this.token = Kind.AsteriskAsteriskEqualsToken;
                            }
                            else
                            {
                                this.pos += 2;
                                this.token = Kind.AsteriskAsteriskToken;
                            }
                        }
                        else
                        {
                            this.pos++;
                            if (this.skipJSDocLeadingAsterisks != 0 && (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingJSDocLeadingAsterisks) == 0 && (this.tokenFlags & global::TypeScript.Scanner.TokenFlags.PrecedingLineBreak) != 0)
                            {
                                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingJSDocLeadingAsterisks;
                                continue;
                            }

                            this.token = Kind.AsteriskToken;
                        }
                    }

                    break;
                }

                case '+':
                {
                    var next = this.charAt(1);
                    if (next == '=')
                    {
                        this.pos += 2;
                        this.token = Kind.PlusEqualsToken;
                    }
                    else
                    {
                        if (next == '+')
                        {
                            this.pos += 2;
                            this.token = Kind.PlusPlusToken;
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.PlusToken;
                        }
                    }

                    break;
                }

                case ',':
                {
                    this.pos++;
                    this.token = Kind.CommaToken;
                    break;
                }

                case '-':
                {
                    var next = this.charAt(1);
                    if (next == '=')
                    {
                        this.pos += 2;
                        this.token = Kind.MinusEqualsToken;
                    }
                    else
                    {
                        if (next == '-')
                        {
                            this.pos += 2;
                            this.token = Kind.MinusMinusToken;
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.MinusToken;
                        }
                    }

                    break;
                }

                case '.':
                {
                    var next = this.charAt(1);
                    if (StringUtil.IsDigit(next))
                    {
                        this.token = this.scanNumber();
                    }
                    else
                    {
                        if (next == '.' && this.charAt(2) == '.')
                        {
                            this.pos += 3;
                            this.token = Kind.DotDotDotToken;
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.DotToken;
                        }
                    }

                    break;
                }

                case '/':
                {
                    if (this.charAt(1) == '/')
                    {
                        this.pos += 2;
                        while (true)
                        {
                            this.scanASCIIWhile((b) =>
                            {
                                return b != '\n' && b != '\r';
                            });
                            var(ch1, size) = this.charAndSize();
                            if (size == 0 || StringUtil.IsLineBreak(ch1))
                            {
                                break;
                            }

                            this.pos += size;
                        }

                        this.processCommentDirective(this.tokenStart, this.pos, false);
                        if (this.skipTrivia)
                        {
                            continue;
                        }

                        this.token = Kind.SingleLineCommentTrivia;
                        return this.token;
                    }

                    if (this.charAt(1) == '*')
                    {
                        this.pos += 2;
                        var isJSDoc = this.charCode() == '*' && this.charAt(1) != '/';
                        var commentClosed = false;
                        var lastLineStart = this.tokenStart;
                        while (true)
                        {
                            this.scanASCIIWhile((b) =>
                            {
                                return b != '*' && b != '\n' && b != '\r';
                            });
                            var(ch1, size) = this.charAndSize();
                            if (size == 0)
                            {
                                break;
                            }

                            if (ch1 == '*' && this.charAt(1) == '/')
                            {
                                this.pos += 2;
                                commentClosed = true;
                                break;
                            }

                            this.pos += size;
                            if (StringUtil.IsLineBreak(ch1))
                            {
                                lastLineStart = this.pos;
                                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingLineBreak;
                            }
                        }

                        if (isJSDoc)
                        {
                            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingJSDocComment;
                            this.scanJSDocCommentForTags(this.text.Slice(this.tokenStart, this.pos));
                        }

                        this.processCommentDirective(lastLineStart, this.pos, true);
                        if (!commentClosed)
                        {
                            this.error(Diagnostics.Asterisk_Slash_expected);
                        }

                        if (this.skipTrivia)
                        {
                            continue;
                        }

                        if (!commentClosed)
                        {
                            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Unterminated;
                        }

                        this.token = Kind.MultiLineCommentTrivia;
                        return this.token;
                    }

                    if (this.charAt(1) == '=')
                    {
                        this.pos += 2;
                        this.token = Kind.SlashEqualsToken;
                    }
                    else
                    {
                        this.pos++;
                        this.token = Kind.SlashToken;
                    }

                    break;
                }

                case '0':
                {
                    if (this.charAt(1) == 'X' || this.charAt(1) == 'x')
                    {
                        var start = this.pos;
                        this.pos += 2;
                        var digits = this.scanHexDigits(1, true, true);
                        if (digits == "")
                        {
                            this.error(Diagnostics.Hexadecimal_digit_expected);
                            digits = "0";
                        }

                        if (this.hexNumberCache == null)
                        {
                            this.hexNumberCache = new Dictionary<string, string>();
                        }

                        {
                            var ok = this.hexNumberCache.TryGetValue(digits, out var cachedValue);
                            if (ok)
                            {
                                this.tokenValue = cachedValue!;
                            }
                            else
                            {
                                // Go scanner.go:721–727: borrow the comparison slices; retain only the selected token value.
                                var rawText = this.text.Slice(start, this.pos);
                                if (GoStrings.HasPrefix(rawText.AsSpan(), "0x"u8) && rawText.Length - 2 == digits.Length && GoStrings.HasPrefix(rawText.AsSpan(2), digits))
                                {
                                    this.tokenValue = rawText.Materialize();
                                }
                                else
                                {
                                    this.tokenValue = "0x" + digits;
                                }

                                this.hexNumberCache[digits] = this.tokenValue;
                            }
                        }

                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.HexSpecifier;
                        this.token = this.scanBigIntSuffix();
                        break;
                    }

                    if (this.charAt(1) == 'B' || this.charAt(1) == 'b')
                    {
                        this.pos += 2;
                        var digits = this.scanBinaryOrOctalDigits(2);
                        if (digits == "")
                        {
                            this.error(Diagnostics.Binary_digit_expected);
                            digits = "0";
                        }

                        this.tokenValue = "0b" + digits;
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.BinarySpecifier;
                        this.token = this.scanBigIntSuffix();
                        break;
                    }

                    if (this.charAt(1) == 'O' || this.charAt(1) == 'o')
                    {
                        this.pos += 2;
                        var digits = this.scanBinaryOrOctalDigits(8);
                        if (digits == "")
                        {
                            this.error(Diagnostics.Octal_digit_expected);
                            digits = "0";
                        }

                        this.tokenValue = "0o" + digits;
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.OctalSpecifier;
                        this.token = this.scanBigIntSuffix();
                        break;
                    }

                    goto case '1';
                }

                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                {
                    this.token = this.scanNumber();
                    break;
                }

                case ':':
                {
                    this.pos++;
                    this.token = Kind.ColonToken;
                    break;
                }

                case ';':
                {
                    this.pos++;
                    this.token = Kind.SemicolonToken;
                    break;
                }

                case '<':
                {
                    if (this.charAt(1) == '<' && isConflictMarkerTrivia(this.text, this.pos))
                    {
                        this.pos = scanConflictMarkerTrivia(this.text, this.pos, this.errorAt);
                        if (this.skipTrivia)
                        {
                            continue;
                        }
                        else
                        {
                            this.token = Kind.ConflictMarkerTrivia;
                            return this.token;
                        }
                    }

                    if (this.charAt(1) == '<')
                    {
                        if (this.charAt(2) == '=')
                        {
                            this.pos += 3;
                            this.token = Kind.LessThanLessThanEqualsToken;
                        }
                        else
                        {
                            this.pos += 2;
                            this.token = Kind.LessThanLessThanToken;
                        }
                    }
                    else
                    {
                        if (this.charAt(1) == '=')
                        {
                            this.pos += 2;
                            this.token = Kind.LessThanEqualsToken;
                        }
                        else
                        {
                            if (this.languageVariant == LanguageVariant.JSX && this.charAt(1) == '/' && this.charAt(2) != '*')
                            {
                                this.pos += 2;
                                this.token = Kind.LessThanSlashToken;
                            }
                            else
                            {
                                this.pos++;
                                this.token = Kind.LessThanToken;
                            }
                        }
                    }

                    break;
                }

                case '=':
                {
                    if (this.charAt(1) == '=' && isConflictMarkerTrivia(this.text, this.pos))
                    {
                        this.pos = scanConflictMarkerTrivia(this.text, this.pos, this.errorAt);
                        if (this.skipTrivia)
                        {
                            continue;
                        }
                        else
                        {
                            this.token = Kind.ConflictMarkerTrivia;
                            return this.token;
                        }
                    }

                    if (this.charAt(1) == '=')
                    {
                        if (this.charAt(2) == '=')
                        {
                            this.pos += 3;
                            this.token = Kind.EqualsEqualsEqualsToken;
                        }
                        else
                        {
                            this.pos += 2;
                            this.token = Kind.EqualsEqualsToken;
                        }
                    }
                    else
                    {
                        if (this.charAt(1) == '>')
                        {
                            this.pos += 2;
                            this.token = Kind.EqualsGreaterThanToken;
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.EqualsToken;
                        }
                    }

                    break;
                }

                case '>':
                {
                    if (this.charAt(1) == '>' && isConflictMarkerTrivia(this.text, this.pos))
                    {
                        this.pos = scanConflictMarkerTrivia(this.text, this.pos, this.errorAt);
                        if (this.skipTrivia)
                        {
                            continue;
                        }
                        else
                        {
                            this.token = Kind.ConflictMarkerTrivia;
                            return this.token;
                        }
                    }

                    this.pos++;
                    this.token = Kind.GreaterThanToken;
                    break;
                }

                case '?':
                {
                    if (this.charAt(1) == '.' && !StringUtil.IsDigit(this.charAt(2)))
                    {
                        this.pos += 2;
                        this.token = Kind.QuestionDotToken;
                    }
                    else
                    {
                        if (this.charAt(1) == '?')
                        {
                            if (this.charAt(2) == '=')
                            {
                                this.pos += 3;
                                this.token = Kind.QuestionQuestionEqualsToken;
                            }
                            else
                            {
                                this.pos += 2;
                                this.token = Kind.QuestionQuestionToken;
                            }
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.QuestionToken;
                        }
                    }

                    break;
                }

                case '[':
                {
                    this.pos++;
                    this.token = Kind.OpenBracketToken;
                    break;
                }

                case ']':
                {
                    this.pos++;
                    this.token = Kind.CloseBracketToken;
                    break;
                }

                case '^':
                {
                    if (this.charAt(1) == '=')
                    {
                        this.pos += 2;
                        this.token = Kind.CaretEqualsToken;
                    }
                    else
                    {
                        this.pos++;
                        this.token = Kind.CaretToken;
                    }

                    break;
                }

                case '{':
                {
                    this.pos++;
                    this.token = Kind.OpenBraceToken;
                    break;
                }

                case '|':
                {
                    if (this.charAt(1) == '|' && isConflictMarkerTrivia(this.text, this.pos))
                    {
                        this.pos = scanConflictMarkerTrivia(this.text, this.pos, this.errorAt);
                        if (this.skipTrivia)
                        {
                            continue;
                        }
                        else
                        {
                            this.token = Kind.ConflictMarkerTrivia;
                            return this.token;
                        }
                    }

                    if (this.charAt(1) == '|')
                    {
                        if (this.charAt(2) == '=')
                        {
                            this.pos += 3;
                            this.token = Kind.BarBarEqualsToken;
                        }
                        else
                        {
                            this.pos += 2;
                            this.token = Kind.BarBarToken;
                        }
                    }
                    else
                    {
                        if (this.charAt(1) == '=')
                        {
                            this.pos += 2;
                            this.token = Kind.BarEqualsToken;
                        }
                        else
                        {
                            this.pos++;
                            this.token = Kind.BarToken;
                        }
                    }

                    break;
                }

                case '}':
                {
                    this.pos++;
                    this.token = Kind.CloseBraceToken;
                    break;
                }

                case '~':
                {
                    this.pos++;
                    this.token = Kind.TildeToken;
                    break;
                }

                case '@':
                {
                    this.pos++;
                    this.token = Kind.AtToken;
                    break;
                }

                case '\\':
                {
                    if (this.scanIdentifier(0, identifierVariant.Standard))
                    {
                        this.token = this.getIdentifierToken();
                    }
                    else
                    {
                        this.scanInvalidCharacter();
                    }

                    break;
                }

                case '#':
                {
                    if (this.charAt(1) == '!')
                    {
                        if (this.pos == 0)
                        {
                            this.pos += 2;
                            {
                                var(shebangCh, size) = this.charAndSize();
                                for (; size > 0 && !StringUtil.IsLineBreak(shebangCh); (shebangCh, size) = this.charAndSize())
                                {
                                    this.pos += size;
                                }
                            }

                            continue;
                        }

                        this.errorAt(Diagnostics.X_can_only_be_used_at_the_start_of_a_file, this.pos, 2);
                        this.pos += 2;
                        this.token = Kind.Unknown;
                        break;
                    }

                    if (!this.scanIdentifier(1, identifierVariant.Standard))
                    {
                        this.errorAt(Diagnostics.Invalid_character, this.pos - 1, 1);
                        this.tokenValue = "#";
                    }

                    this.token = Kind.PrivateIdentifier;
                    break;
                }

                default:
                {
                    if (ch < 0)
                    {
                        this.token = Kind.EndOfFile;
                        break;
                    }

                    if (this.scanIdentifier(0, identifierVariant.Standard))
                    {
                        this.token = this.getIdentifierToken();
                        break;
                    }

                    var(decodedCh, size) = this.charAndSize();
                    if (decodedCh == Utf8.RuneError)
                    {
                        this.errorAt(Diagnostics.File_appears_to_be_binary, 0, 0);
                        this.pos = this.text.Length;
                        this.token = Kind.NonTextFileMarkerTrivia;
                        break;
                    }

                    if (StringUtil.IsWhiteSpaceSingleLine(decodedCh))
                    {
                        this.pos += size;
                        if (decodedCh == 0x0085 || this.skipTrivia)
                        {
                            continue;
                        }

                        while (true)
                        {
                            (decodedCh, size) = this.charAndSize();
                            if (!StringUtil.IsWhiteSpaceSingleLine(decodedCh))
                            {
                                break;
                            }

                            this.pos += size;
                        }

                        this.token = Kind.WhitespaceTrivia;
                        return this.token;
                    }

                    if (StringUtil.IsLineBreak(decodedCh))
                    {
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingLineBreak;
                        this.pos += size;
                        continue;
                    }

                    this.scanInvalidCharacter();
                    break;
                }
            }

            return this.token;
        }
    }

    private void processCommentDirective(int start, int end, bool multiline)
    {
        var pos = start;
        if (multiline)
        {
            while (pos < end && (this.text[pos] == ' ' || this.text[pos] == '\t'))
            {
                pos++;
            }

            while (pos < end && (this.text[pos] == '/' || this.text[pos] == '*'))
            {
                pos++;
            }
        }
        else
        {
            pos += 2;
            while (pos < end && this.text[pos] == '/')
            {
                pos++;
            }
        }

        while (pos < end && (this.text[pos] == ' ' || this.text[pos] == '\t'))
        {
            pos++;
        }

        if (!(pos < end && this.text[pos] == '@'))
        {
            return;
        }

        pos++;
        CommentDirectiveKind kind = default;
        switch (true)
        {
            case var case2 when GoStrings.HasPrefix(this.text.AsSpan(pos), "ts-expect-error"u8):
            {
                kind = global::TypeScript.Scanner.CommentDirectiveKind.ExpectError;
                break;
            }

            case var case1002 when GoStrings.HasPrefix(this.text.AsSpan(pos), "ts-ignore"u8):
            {
                kind = global::TypeScript.Scanner.CommentDirectiveKind.Ignore;
                break;
            }

            default:
            {
                return;
            }
        }

        this.commentDirectives = this.commentDirectives.Append(new CommentDirective(new TextRange(start, end), kind));
    }

    public Kind ReScanLessThanToken()
    {
        if (this.token == Kind.LessThanLessThanToken)
        {
            this.pos = this.tokenStart + 1;
            this.token = Kind.LessThanToken;
        }

        return this.token;
    }

    public Kind ReScanGreaterThanToken()
    {
        if (this.token == Kind.GreaterThanToken)
        {
            this.reScanGreaterThanTokenInner();
        }

        return this.token;
    }

    private void reScanGreaterThanTokenInner()
    {
        this.pos = this.tokenStart + 1;
        if (this.charCode() == '>')
        {
            if (this.charAt(1) == '>')
            {
                if (this.charAt(2) == '=')
                {
                    this.pos += 3;
                    this.token = Kind.GreaterThanGreaterThanGreaterThanEqualsToken;
                }
                else
                {
                    this.pos += 2;
                    this.token = Kind.GreaterThanGreaterThanGreaterThanToken;
                }
            }
            else
            {
                if (this.charAt(1) == '=')
                {
                    this.pos += 2;
                    this.token = Kind.GreaterThanGreaterThanEqualsToken;
                }
                else
                {
                    this.pos++;
                    this.token = Kind.GreaterThanGreaterThanToken;
                }
            }
        }
        else
        {
            if (this.charCode() == '=')
            {
                this.pos++;
                this.token = Kind.GreaterThanEqualsToken;
            }
        }
    }

    public Kind ReScanTemplateToken(bool isTaggedTemplate)
    {
        this.pos = this.tokenStart;
        this.token = this.scanTemplateAndSetTokenValue(!isTaggedTemplate);
        return this.token;
    }

    public Kind ReScanAsteriskEqualsToken()
    {
        if (this.token != Kind.AsteriskEqualsToken)
        {
            throw new InvalidOperationException("'ReScanAsteriskEqualsToken' should only be called on a '*='");
        }

        this.pos = this.tokenStart + 1;
        this.token = Kind.EqualsToken;
        return this.token;
    }

    public Kind ReScanSlashToken(params bool[] reportErrors)
    {
        var shouldReportErrors = reportErrors.Length > 0 && reportErrors[0];
        if (this.token == Kind.SlashToken || this.token == Kind.SlashEqualsToken)
        {
            var startOfRegExpBody = this.tokenStart + 1;
            var p = startOfRegExpBody;
            var inEscape = false;
            var namedCaptureGroups = false;
            var inCharacterClass = false;
            while (true)
            {
                if (p >= this.end)
                {
                    this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Unterminated;
                    goto loopEnd;
                }

                int ch = this.text[p];
                switch (true)
                {
                    case var case3 when StringUtil.IsLineBreak(ch):
                    {
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Unterminated;
                        goto loopEnd;
                    }

                    case var case1003 when inEscape:
                    {
                        inEscape = false;
                        break;
                    }

                    case var case2003 when (ch == '/' && !inCharacterClass):
                    {
                        goto loopEnd;
                    }

                    case var case3003 when (ch == '['):
                    {
                        inCharacterClass = true;
                        break;
                    }

                    case var case4003 when (ch == '\\'):
                    {
                        inEscape = true;
                        break;
                    }

                    case var case5003 when (ch == ']'):
                    {
                        inCharacterClass = false;
                        break;
                    }

                    case var case6003 when (!inCharacterClass && ch == '(' && p + 1 < this.end && this.text[p + 1] == '?' && p + 2 < this.end && this.text[p + 2] == '<' && (p + 3 >= this.end || this.text[p + 3] != '=' && this.text[p + 3] != '!')):
                    {
                        namedCaptureGroups = true;
                        break;
                    }
                }

                p++;
            }

            loopEnd:
                ;
            var endOfRegExpBody = p;
            if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.Unterminated) != 0)
            {
                p = startOfRegExpBody;
                inEscape = false;
                var characterClassDepth = 0;
                var inDecimalQuantifier = false;
                var groupDepth = 0;
                while (p < endOfRegExpBody)
                {
                    int ch = this.text[p];
                    if (inEscape)
                    {
                        inEscape = false;
                    }
                    else
                    {
                        if (ch == '\\')
                        {
                            inEscape = true;
                        }
                        else
                        {
                            if (ch == '[')
                            {
                                characterClassDepth++;
                            }
                            else
                            {
                                if (ch == ']' && characterClassDepth != 0)
                                {
                                    characterClassDepth--;
                                }
                                else
                                {
                                    if (characterClassDepth == 0)
                                    {
                                        if (ch == '{')
                                        {
                                            inDecimalQuantifier = true;
                                        }
                                        else
                                        {
                                            if (ch == '}' && inDecimalQuantifier)
                                            {
                                                inDecimalQuantifier = false;
                                            }
                                            else
                                            {
                                                if (!inDecimalQuantifier)
                                                {
                                                    if (ch == '(')
                                                    {
                                                        groupDepth++;
                                                    }
                                                    else
                                                    {
                                                        if (ch == ')' && groupDepth != 0)
                                                        {
                                                            groupDepth--;
                                                        }
                                                        else
                                                        {
                                                            if (ch == ')' || ch == ']' || ch == '}')
                                                            {
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    p++;
                }

                while (p > startOfRegExpBody)
                {
                    var(ch, size) = Utf8.DecodeLastRuneInString(this.text.AsSpan(0, p - 0));
                    if (StringUtil.IsWhiteSpaceLike(ch) || ch == ';')
                    {
                        p -= size;
                    }
                    else
                    {
                        break;
                    }
                }

                this.errorAt(Diagnostics.Unterminated_regular_expression_literal, this.tokenStart, p - this.tokenStart);
            }
            else
            {
                p++;
                regularExpressionFlags regExpFlags = default;
                while (p < this.end)
                {
                    var(ch, size) = Utf8.DecodeRuneInString(this.text.AsSpan(p));
                    if (ch == Utf8.RuneError || !IsIdentifierPart(ch))
                    {
                        break;
                    }

                    if (shouldReportErrors)
                    {
                        var ok = charCodeToRegExpFlag.TryGetValue(ch, out var flag);
                        if (!ok)
                        {
                            this.errorAt(Diagnostics.Unknown_regular_expression_flag, p, size);
                        }
                        else
                        {
                            if ((regExpFlags & flag) != 0)
                            {
                                this.errorAt(Diagnostics.Duplicate_regular_expression_flag, p, size);
                            }
                            else
                            {
                                if (((regExpFlags | flag) & regularExpressionFlags.AnyUnicodeMode) == regularExpressionFlags.AnyUnicodeMode)
                                {
                                    this.errorAt(Diagnostics.The_Unicode_u_flag_and_the_Unicode_Sets_v_flag_cannot_be_set_simultaneously, p, size);
                                }
                                else
                                {
                                    regExpFlags |= flag;
                                    this.checkRegularExpressionFlagAvailability(flag, p, size);
                                }
                            }
                        }
                    }

                    p += size;
                }

                if (shouldReportErrors)
                {
                    this.pos = startOfRegExpBody;
                    var saveEnd = this.end;
                    var saveTokenPos = this.tokenStart;
                    var saveTokenFlags = this.tokenFlags;
                    this.end = endOfRegExpBody;
                    var parser = new regExpParser
                    {
                        scanner = this,
                        end = endOfRegExpBody,
                        regExpFlags = regExpFlags,
                        anyUnicodeMode = (regExpFlags & regularExpressionFlags.AnyUnicodeMode) != 0,
                        unicodeSetsMode = (regExpFlags & regularExpressionFlags.UnicodeSets) != 0,
                        annexB = true,
                        namedCaptureGroups = namedCaptureGroups,
                        groupSpecifiers = new Dictionary<string, bool>()
                    };
                    parser.run();
                    this.end = saveEnd;
                    this.pos = p;
                    this.tokenStart = saveTokenPos;
                    this.tokenFlags = saveTokenFlags;
                }
                else
                {
                    this.pos = p;
                }
            }

            this.pos = p;
            this.setTokenValueView(this.text.Slice(this.tokenStart, this.pos));
            this.token = Kind.RegularExpressionLiteral;
        }

        return this.token;
    }

    public Kind ReScanJsxToken(bool allowMultilineJsxText)
    {
        this.pos = this.fullStartPos;
        this.tokenStart = this.fullStartPos;
        this.token = this.ScanJsxTokenEx(allowMultilineJsxText);
        return this.token;
    }

    public Kind ReScanHashToken()
    {
        if (this.token == Kind.PrivateIdentifier)
        {
            this.pos = this.tokenStart + 1;
            this.token = Kind.HashToken;
        }

        return this.token;
    }

    public Kind ReScanQuestionToken()
    {
        if (this.token != Kind.QuestionQuestionToken)
        {
            throw new InvalidOperationException("'reScanQuestionToken' should only be called on a '??'");
        }

        this.pos = this.tokenStart + 1;
        this.token = Kind.QuestionToken;
        return this.token;
    }

    public Kind ScanJsxToken()
    {
        return this.ScanJsxTokenEx(true);
    }

    public Kind ScanJsxTokenEx(bool allowMultilineJsxText)
    {
        this.fullStartPos = this.pos;
        this.tokenStart = this.pos;
        var ch = this.charCode();
        switch (true)
        {
            case var case4 when (ch < 0):
            {
                this.token = Kind.EndOfFile;
                break;
            }

            case var case1004 when (ch == '<'):
            {
                if (this.charAt(1) == '/')
                {
                    this.pos += 2;
                    this.token = Kind.LessThanSlashToken;
                }
                else
                {
                    this.pos++;
                    this.token = Kind.LessThanToken;
                }

                break;
            }

            case var case2004 when (ch == '{'):
            {
                this.pos++;
                this.token = Kind.OpenBraceToken;
                break;
            }

            default:
            {
                var firstNonWhitespace = 0;
                while (true)
                {
                    var(jsxCh, size) = this.charAndSize();
                    if (size == 0 || jsxCh == '{')
                    {
                        break;
                    }

                    if (jsxCh == '<')
                    {
                        if (isConflictMarkerTrivia(this.text, this.pos))
                        {
                            this.pos = scanConflictMarkerTrivia(this.text, this.pos, this.errorAt);
                            this.token = Kind.ConflictMarkerTrivia;
                            return this.token;
                        }

                        break;
                    }

                    if (jsxCh == '>')
                    {
                        this.errorAt(Diagnostics.Unexpected_token_Did_you_mean_or_gt, this.pos, 1);
                    }
                    else
                    {
                        if (jsxCh == '}')
                        {
                            this.errorAt(Diagnostics.Unexpected_token_Did_you_mean_or_rbrace, this.pos, 1);
                        }
                    }

                    if (StringUtil.IsLineBreak(jsxCh) && firstNonWhitespace == 0)
                    {
                        firstNonWhitespace = -1;
                    }
                    else
                    {
                        if (!allowMultilineJsxText && StringUtil.IsLineBreak(jsxCh) && firstNonWhitespace > 0)
                        {
                            break;
                        }
                        else
                        {
                            if (!StringUtil.IsWhiteSpaceLike(jsxCh))
                            {
                                firstNonWhitespace = this.pos;
                            }
                        }
                    }

                    this.pos += size;
                }

                this.setTokenValueView(this.text.Slice(this.fullStartPos, this.pos));
                this.token = Kind.JsxText;
                if (firstNonWhitespace == -1)
                {
                    this.token = Kind.JsxTextAllWhiteSpaces;
                }

                break;
            }
        }

        return this.token;
    }

    // Scans a JSX identifier; these differ from normal identifiers in that they allow dashes
    public Kind ScanJsxIdentifier()
    {
        if (tokenIsIdentifierOrKeyword(this.token))
        {
            this.tokenValue += this.scanIdentifierParts(identifierVariant.JSX);
            this.token = this.getIdentifierToken();
        }

        return this.token;
    }

    public Kind ScanJsxAttributeValue()
    {
        this.fullStartPos = this.pos;
        {
            var(ch, size) = this.charAndSize();
            for (; size > 0 && StringUtil.IsWhiteSpaceLike(ch); (ch, size) = this.charAndSize())
            {
                this.pos += size;
            }
        }

        this.tokenStart = this.pos;
        switch (this.charCode())
        {
            case '"':
            case '\'':
            {
                this.scanString(true);
                this.token = Kind.StringLiteral;
                return this.token;
            }

            default:
            {
                return this.Scan();
            }
        }
    }

    public Kind ReScanJsxAttributeValue()
    {
        this.pos = this.fullStartPos;
        this.tokenStart = this.fullStartPos;
        return this.ScanJsxAttributeValue();
    }

    // Also returns Kind.JSDocCommentTextToken alongside the usual JSDoc kinds.
    public Kind ScanJSDocCommentTextToken(bool inBackticks)
    {
        this.fullStartPos = this.pos;
        this.tokenFlags = global::TypeScript.Scanner.TokenFlags.None;
        if (this.pos >= this.text.Length)
        {
            this.token = Kind.EndOfFile;
            return this.token;
        }

        this.tokenStart = this.pos;
        {
            var(ch, size) = this.charAndSize();
            for (; this.pos < this.text.Length && !StringUtil.IsLineBreak(ch) && ch != '`'; (ch, size) = this.charAndSize())
            {
                if (!inBackticks)
                {
                    if (ch == '{')
                    {
                        break;
                    }
                    else
                    {
                        if (ch == '@' && this.pos >= 0)
                        {
                            var(previous, _) = Utf8.DecodeLastRuneInString(this.text.AsSpan(0, this.pos - 0));
                            if (StringUtil.IsWhiteSpaceSingleLine(previous))
                            {
                                var(next, _) = Utf8.DecodeRuneInString(this.text.AsSpan(this.pos + size));
                                if (IsIdentifierStart(next))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                this.pos += size;
            }
        }

        if (this.pos == this.tokenStart)
        {
            return this.ScanJSDocToken();
        }

        this.setTokenValueView(this.text.Slice(this.tokenStart, this.pos));
        this.token = Kind.JSDocCommentTextToken;
        return this.token;
    }

    // Peek at the character at the current scanner position (expected to be right after '@')
    // and return true if a JSDoc tag can follow. Identifier starts indicate a tag name.
    // Whitespace, newlines, and EOF are also accepted to support incomplete tags for code completion.
    public bool CanFollowJSDocAt()
    {
        if (this.pos >= this.text.Length)
        {
            return true;
        }

        var(ch, _) = Utf8.DecodeRuneInString(this.text.AsSpan(this.pos));
        return IsIdentifierStart(ch) || StringUtil.IsWhiteSpaceSingleLine(ch) || StringUtil.IsLineBreak(ch);
    }

    public Kind ScanJSDocToken()
    {
        this.fullStartPos = this.pos;
        this.tokenFlags = global::TypeScript.Scanner.TokenFlags.None;
        if (this.pos >= this.text.Length)
        {
            this.token = Kind.EndOfFile;
            return this.token;
        }

        this.tokenStart = this.pos;
        var(ch, size) = this.charAndSize();
        this.pos += size;
        switch (ch)
        {
            case '\t':
            case '\v':
            case '\f':
            case ' ':
            {
                {
                    var(ch2, size2) = this.charAndSize();
                    for (; size2 > 0 && StringUtil.IsWhiteSpaceSingleLine(ch2); (ch2, size2) = this.charAndSize())
                    {
                        this.pos += size2;
                    }
                }

                this.token = Kind.WhitespaceTrivia;
                return this.token;
            }

            case '@':
            {
                this.token = Kind.AtToken;
                return this.token;
            }

            case '\r':
            {
                if (this.charCode() == '\n')
                {
                    this.pos++;
                }

                goto case '\n';
            }

            case '\n':
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.PrecedingLineBreak;
                this.token = Kind.NewLineTrivia;
                return this.token;
            }

            case '*':
            {
                this.token = Kind.AsteriskToken;
                return this.token;
            }

            case '{':
            {
                this.token = Kind.OpenBraceToken;
                return this.token;
            }

            case '}':
            {
                this.token = Kind.CloseBraceToken;
                return this.token;
            }

            case '[':
            {
                this.token = Kind.OpenBracketToken;
                return this.token;
            }

            case ']':
            {
                this.token = Kind.CloseBracketToken;
                return this.token;
            }

            case '(':
            {
                this.token = Kind.OpenParenToken;
                return this.token;
            }

            case ')':
            {
                this.token = Kind.CloseParenToken;
                return this.token;
            }

            case '<':
            {
                this.token = Kind.LessThanToken;
                return this.token;
            }

            case '>':
            {
                this.token = Kind.GreaterThanToken;
                return this.token;
            }

            case '=':
            {
                this.token = Kind.EqualsToken;
                return this.token;
            }

            case ',':
            {
                this.token = Kind.CommaToken;
                return this.token;
            }

            case '.':
            {
                this.token = Kind.DotToken;
                return this.token;
            }

            case '`':
            {
                this.token = Kind.BacktickToken;
                return this.token;
            }

            case '#':
            {
                this.token = Kind.HashToken;
                return this.token;
            }
        }

        this.pos = this.tokenStart;
        if (this.scanIdentifier(0, identifierVariant.JSX))
        {
            this.token = this.getIdentifierToken();
            return this.token;
        }

        this.pos = this.tokenStart + size;
        this.token = Kind.Unknown;
        return this.token;
    }

    private bool scanIdentifier(int prefixLength, identifierVariant variant)
    {
        var start = this.pos;
        this.pos += prefixLength;
        var identifierStart = this.pos;
        var ch = this.charCode();
        if (variant != identifierVariant.JSX && (StringUtil.IsASCIILetter(ch) || ch == '_' || ch == '$'))
        {
            this.pos++;
            this.scanASCIIWhile((b) =>
            {
                return b >= 'a' && b <= 'z' || b >= 'A' && b <= 'Z' || b >= '0' && b <= '9' || b == '_' || b == '$';
            });
            ch = this.charCode();
            if (ch < Utf8.RuneSelf && ch != '\\')
            {
                this.setTokenValueView(this.text.Slice(start, this.pos));
                return true;
            }

            this.pos = identifierStart;
        }

        int size;
        (ch, size) = this.charAndSize();
        if (IsIdentifierStart(ch))
        {
            var languageVariant = LanguageVariant.Standard;
            if (variant == identifierVariant.JSX)
            {
                languageVariant = LanguageVariant.JSX;
            }

            while (true)
            {
                this.pos += size;
                (ch, size) = this.charAndSize();
                if (!IsIdentifierPartEx(ch, languageVariant))
                {
                    break;
                }
            }

            this.setTokenValueView(this.text.Slice(start, this.pos));
            if (ch == '\\')
            {
                var builder = new GoBuilder();
                builder.WriteString(this.text.AsSpan(start, this.pos - start));
                this.scanIdentifierParts(variant, builder);
                this.tokenValue = builder.String();
            }

            return true;
        }

        if (ch == '\\')
        {
            {
                var(escaped, ok) = this.scanIdentifierEscape(IsIdentifierStart, variant == identifierVariant.RegExpGroupName);
                if (ok)
                {
                    var builder = new GoBuilder();
                    builder.WriteString(this.text.AsSpan(start, identifierStart - start));
                    builder.WriteRune(escaped);
                    this.scanIdentifierParts(variant, builder);
                    this.tokenValue = builder.String();
                    return true;
                }
            }
        }

        return false;
    }

    private string scanIdentifierParts(identifierVariant variant)
    {
        var sb = new GoBuilder();
        this.scanIdentifierParts(variant, sb);
        return sb.String();
    }

    // scanner.go:1529: reuse the same traversal when an identifier prefix and
    // escape already belong to the final builder, avoiding a temporary suffix.
    private void scanIdentifierParts(identifierVariant variant, GoBuilder sb)
    {
        var start = this.pos;
        var languageVariant = LanguageVariant.Standard;
        if (variant == identifierVariant.JSX)
        {
            languageVariant = LanguageVariant.JSX;
        }

        while (true)
        {
            var(ch, size) = this.charAndSize();
            if (IsIdentifierPartEx(ch, languageVariant))
            {
                this.pos += size;
                continue;
            }

            if (ch == '\\')
            {
                var escapeStart = this.pos;
                {
                    var(escaped, ok) = this.scanIdentifierEscape((ch) =>
                    {
                        return IsIdentifierPartEx(ch, languageVariant);
                    }, variant == identifierVariant.RegExpGroupName);
                    if (ok)
                    {
                        sb.WriteString(this.text.AsSpan(start, escapeStart - start));
                        sb.WriteRune(escaped);
                        start = this.pos;
                        continue;
                    }
                }
            }

            break;
        }

        sb.WriteString(this.text.AsSpan(start, this.pos - start));
    }

    private (int, bool) scanIdentifierEscape(Func<int, bool> isValid, bool allowSurrogatePairEscape)
    {
        var escaped = this.peekUnicodeEscape();
        if (escaped >= 0 && isValid(escaped))
        {
            return (this.scanUnicodeEscape(true), true);
        }

        if (allowSurrogatePairEscape && this.charAt(2) != '{' && StringUtil.IsHighSurrogate(escaped))
        {
            var savedPos = this.pos;
            var savedTokenFlags = this.tokenFlags;
            this.scanUnicodeEscape(false);
            if (this.charAt(2) != '{')
            {
                {
                    var(codePoint, ok) = this.scanLowSurrogateEscape(escaped);
                    if (ok && isValid(codePoint))
                    {
                        return (codePoint, true);
                    }
                }
            }

            this.pos = savedPos;
            this.tokenFlags = savedTokenFlags;
        }

        return (0, false);
    }

    // scanner.go:1586: publish a borrowed token on the unchanged fast path;
    // only escape/newline processing constructs an owned decoded value.
    private void scanString(bool jsxAttributeString)
    {
        var quote = this.charCode();
        if (quote == '\'')
        {
            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.SingleQuote;
        }

        this.pos++;
        var strLen = GoStrings.IndexByte(this.text.AsSpan(this.pos), (int)quote);
        if (strLen == 0)
        {
            this.pos++;
            this.setTokenValueView(this.text.Slice(this.pos - 1, this.pos - 1));
            return;
        }

        if (strLen > 0)
        {
            // scanner.go:1601. Inspect the borrowed substring before the final token projection.
            var str = this.text.AsSpan(this.pos, strLen);
            if (jsxAttributeString || GoStrings.IndexByte(str, '\\') < 0 && GoStrings.IndexByte(str, '\r') < 0 && GoStrings.IndexByte(str, '\n') < 0)
            {
                this.setTokenValueView(this.text.Slice(this.pos, this.pos + strLen));
                this.pos += strLen + 1;
                return;
            }
        }

        GoBuilder? sb = null;
        var valueStart = this.pos;
        var valueEnd = this.pos;
        var start = this.pos;
        while (true)
        {
            var ch = this.charCode();
            if (ch < 0)
            {
                valueEnd = this.pos;
                sb?.WriteString(this.text.AsSpan(start, this.pos - start));
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Unterminated;
                this.error(Diagnostics.Unterminated_string_literal);
                break;
            }

            if (ch == quote)
            {
                valueEnd = this.pos;
                sb?.WriteString(this.text.AsSpan(start, this.pos - start));
                this.pos++;
                break;
            }

            if (ch == '\\' && !jsxAttributeString)
            {
                sb ??= new GoBuilder();
                sb.WriteString(this.text.AsSpan(start, this.pos - start));
                sb.WriteString(this.scanEscapeSequence(EscapeSequenceScanningFlags.String | EscapeSequenceScanningFlags.ReportErrors));
                start = this.pos;
                continue;
            }

            if ((ch == '\n' || ch == '\r') && !jsxAttributeString)
            {
                valueEnd = this.pos;
                sb?.WriteString(this.text.AsSpan(start, this.pos - start));
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Unterminated;
                this.error(Diagnostics.Unterminated_string_literal);
                break;
            }

            this.pos++;
        }

        if (sb is null)
            this.setTokenValueView(this.text.Slice(valueStart, valueEnd));
        else
            this.tokenValue = sb.String();
    }

    private Kind scanTemplateAndSetTokenValue(bool shouldEmitInvalidEscapeError)
    {
        var startedWithBacktick = this.charCode() == '`';
        this.pos++;
        var start = this.pos;
        // scanner.go:1638: source fragments borrow; transformed output is joined
        // directly into its final owner instead of copied temporary strings.
        GoBuilder? builder = null;
        var valueStart = start;
        var valueEnd = start;
        Kind token = default;
        while (true)
        {
            this.scanASCIIWhile((b) =>
            {
                return b != '`' && b != '$' && b != '\\' && b != '\r';
            });
            var ch = this.charCode();
            if (ch < 0 || ch == '`')
            {
                valueEnd = this.pos;
                builder?.WriteString(this.text.AsSpan(start, this.pos - start));
                if (ch == '`')
                {
                    this.pos++;
                }
                else
                {
                    this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Unterminated;
                    this.error(Diagnostics.Unterminated_template_literal);
                }

                token = Core.IfElse(startedWithBacktick, Kind.NoSubstitutionTemplateLiteral, Kind.TemplateTail);
                break;
            }

            if (ch == '$' && this.charAt(1) == '{')
            {
                valueEnd = this.pos;
                builder?.WriteString(this.text.AsSpan(start, this.pos - start));
                this.pos += 2;
                token = Core.IfElse(startedWithBacktick, Kind.TemplateHead, Kind.TemplateMiddle);
                break;
            }

            if (ch == '\\')
            {
                builder ??= new GoBuilder();
                builder.WriteString(this.text.AsSpan(start, this.pos - start));
                builder.WriteString(this.scanEscapeSequence(EscapeSequenceScanningFlags.String | Core.IfElse(shouldEmitInvalidEscapeError, EscapeSequenceScanningFlags.ReportErrors, (EscapeSequenceScanningFlags)0)));
                start = this.pos;
                continue;
            }

            if (ch == '\r')
            {
                builder ??= new GoBuilder();
                builder.WriteString(this.text.AsSpan(start, this.pos - start));
                this.pos++;
                if (this.charCode() == '\n')
                {
                    this.pos++;
                }

                builder.WriteByte((byte)'\n');
                start = this.pos;
                continue;
            }

            this.pos++;
        }

        if (builder is null)
            this.setTokenValueView(this.text.Slice(valueStart, valueEnd));
        else
            this.tokenValue = builder.String();
        return token;
    }

    private string scanEscapeSequence(EscapeSequenceScanningFlags flags)
    {
        var start = this.pos;
        this.pos++;
        var ch = this.charCode();
        if (ch < 0)
        {
            this.error(Diagnostics.Unexpected_end_of_text);
            return "";
        }

        this.pos++;
        switch (ch)
        {
            case '0':
            {
                if (!StringUtil.IsDigit(this.charCode()))
                {
                    return "\x00";
                }

                goto case '1';
            }

            case '1':
            case '2':
            case '3':
            {
                if (StringUtil.IsOctalDigit(this.charCode()))
                {
                    this.pos++;
                }

                goto case '4';
            }

            case '4':
            case '5':
            case '6':
            case '7':
            {
                if (StringUtil.IsOctalDigit(this.charCode()))
                {
                    this.pos++;
                }

                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidEscape;
                if ((flags & EscapeSequenceScanningFlags.ReportInvalidEscapeErrors) != 0)
                {
                    var(code, _) = Strconv.ParseInt(this.text.Substring(start + 1, this.pos - (start + 1)), 8, 32);
                    if ((flags & EscapeSequenceScanningFlags.RegularExpression) != 0 && (flags & EscapeSequenceScanningFlags.AtomEscape) == 0 && ch != '0')
                    {
                        this.errorAt(Diagnostics.Octal_escape_sequences_and_backreferences_are_not_allowed_in_a_character_class_If_this_was_intended_as_an_escape_sequence_use_the_syntax_0_instead, start, this.pos - start, Fmt.Sprintf("\\x%02x", code));
                    }
                    else
                    {
                        this.errorAt(Diagnostics.Octal_escape_sequences_are_not_allowed_Use_the_syntax_0, start, this.pos - start, Fmt.Sprintf("\\x%02x", code));
                    }

                    return GoString.FromRune((int)code);
                }

                return this.text.Substring(start, this.pos - start);
            }

            case '8':
            case '9':
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidEscape;
                if ((flags & EscapeSequenceScanningFlags.ReportInvalidEscapeErrors) != 0)
                {
                    if ((flags & EscapeSequenceScanningFlags.RegularExpression) != 0 && (flags & EscapeSequenceScanningFlags.AtomEscape) == 0)
                    {
                        this.errorAt(Diagnostics.Decimal_escape_sequences_and_backreferences_are_not_allowed_in_a_character_class, start, this.pos - start);
                    }
                    else
                    {
                        this.errorAt(Diagnostics.Escape_sequence_0_is_not_allowed, start, this.pos - start, this.text.Substring(start, this.pos - start));
                    }

                    return GoString.FromRune(ch);
                }

                return this.text.Substring(start, this.pos - start);
            }

            case 'b':
            {
                return "\b";
            }

            case 't':
            {
                return "\t";
            }

            case 'n':
            {
                return "\n";
            }

            case 'v':
            {
                return "\v";
            }

            case 'f':
            {
                return "\f";
            }

            case 'r':
            {
                return "\r";
            }

            case '\'':
            {
                return "'";
            }

            case '"':
            {
                return "\"";
            }

            case 'u':
            {
                var extended = this.charCode() == '{';
                this.pos -= 2;
                var codePoint = this.scanUnicodeEscape((flags & EscapeSequenceScanningFlags.ReportInvalidEscapeErrors) != 0);
                if (extended)
                {
                    if ((flags & EscapeSequenceScanningFlags.AllowExtendedUnicodeEscape) == 0)
                    {
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidEscape;
                        if ((flags & EscapeSequenceScanningFlags.ReportInvalidEscapeErrors) != 0)
                        {
                            this.errorAt(Diagnostics.Unicode_escape_sequences_are_only_available_when_the_Unicode_u_flag_or_the_Unicode_Sets_v_flag_is_set, start, this.pos - start);
                        }
                    }

                    if (codePoint < 0)
                    {
                        return this.text.Substring(start, this.pos - start);
                    }

                    if ((flags & EscapeSequenceScanningFlags.RegularExpression) == 0 && StringUtil.IsHighSurrogate(codePoint))
                    {
                        {
                            var(combined, ok) = this.scanLowSurrogateEscape(codePoint);
                            if (ok)
                            {
                                return GoString.FromRune(combined);
                            }
                        }
                    }

                    return StringUtil.EncodeJSStringRune(codePoint);
                }

                if (codePoint < 0)
                {
                    return this.text.Substring(start, this.pos - start);
                }
                else
                {
                    if (StringUtil.IsHighSurrogate(codePoint))
                    {
                        if ((flags & EscapeSequenceScanningFlags.RegularExpression) == 0)
                        {
                            {
                                var(combined, ok) = this.scanLowSurrogateEscape(codePoint);
                                if (ok)
                                {
                                    return GoString.FromRune(combined);
                                }
                            }
                        }
                        else
                        {
                            if ((flags & EscapeSequenceScanningFlags.AnyUnicodeMode) != 0 && this.charCode() == '\\' && this.charAt(1) == 'u' && this.charAt(2) != '{')
                            {
                                var savedPos = this.pos;
                                var nextCodePoint = this.scanUnicodeEscape((flags & EscapeSequenceScanningFlags.ReportInvalidEscapeErrors) != 0);
                                if (StringUtil.IsLowSurrogate(nextCodePoint))
                                {
                                    return GoString.FromRune(StringUtil.SurrogatePairToCodePoint(codePoint, nextCodePoint));
                                }

                                this.pos = savedPos;
                            }
                        }
                    }
                }

                return StringUtil.EncodeJSStringRune(codePoint);
            }

            case 'x':
            {
                for (; this.pos < start + 4; this.pos++)
                {
                    if (!StringUtil.IsHexDigit(this.charCode()))
                    {
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidEscape;
                        if ((flags & EscapeSequenceScanningFlags.ReportInvalidEscapeErrors) != 0)
                        {
                            this.error(Diagnostics.Hexadecimal_digit_expected);
                        }

                        return this.text.Substring(start, this.pos - start);
                    }
                }

                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.HexEscape;
                var(escapedValue, _) = Strconv.ParseInt(this.text.Substring(start + 2, this.pos - (start + 2)), 16, 32);
                return GoString.FromRune((int)escapedValue);
            }

            case '\r':
            {
                if (this.charCode() == '\n')
                {
                    this.pos++;
                }

                goto case '\n';
            }

            case '\n':
            {
                return "";
            }

            default:
            {
                if (ch >= Utf8.RuneSelf)
                {
                    this.pos--;
                    int size = default;
                    (ch, size) = Utf8.DecodeRuneInString(this.text.AsSpan(this.pos));
                    this.pos += size;
                }

                if (ch == '\u2028' || ch == '\u2029')
                {
                    return "";
                }

                if ((flags & EscapeSequenceScanningFlags.AnyUnicodeMode) != 0 || (flags & EscapeSequenceScanningFlags.RegularExpression) != 0 && (flags & EscapeSequenceScanningFlags.AnnexB) == 0 && IsIdentifierPart(ch))
                {
                    this.errorAt(Diagnostics.This_character_cannot_be_escaped_in_a_regular_expression, start, this.pos - start);
                }

                return GoString.FromRune(ch);
            }
        }
    }

    // Known to be at \u
    private int scanUnicodeEscape(bool shouldEmitInvalidEscapeError)
    {
        this.pos += 2;
        var start = this.pos;
        var extended = this.charCode() == '{';
        string hexDigits = "";
        if (extended)
        {
            this.pos++;
            hexDigits = this.scanHexDigits(1, true, false);
        }
        else
        {
            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.UnicodeEscape;
            hexDigits = this.scanHexDigits(4, false, false);
        }

        if (hexDigits == "")
        {
            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidEscape;
            if (shouldEmitInvalidEscapeError)
            {
                this.error(Diagnostics.Hexadecimal_digit_expected);
            }

            return -1;
        }

        var(hexValue, _) = Strconv.ParseInt(hexDigits, 16, 32);
        if (extended)
        {
            var isInvalidExtendedEscape = false;
            if (hexValue > 0x10FFFF)
            {
                if (shouldEmitInvalidEscapeError)
                {
                    this.errorAt(Diagnostics.An_extended_Unicode_escape_value_must_be_between_0x0_and_0x10FFFF_inclusive, start + 1, this.pos - start - 1);
                }

                isInvalidExtendedEscape = true;
            }

            if (this.pos >= this.end)
            {
                if (shouldEmitInvalidEscapeError)
                {
                    this.error(Diagnostics.Unexpected_end_of_text);
                }

                isInvalidExtendedEscape = true;
            }
            else
            {
                if (this.charCode() == '}')
                {
                    this.pos++;
                }
                else
                {
                    if (shouldEmitInvalidEscapeError)
                    {
                        this.error(Diagnostics.Unterminated_Unicode_escape_sequence);
                    }

                    isInvalidExtendedEscape = true;
                }
            }

            if (isInvalidExtendedEscape)
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidEscape;
                return -1;
            }

            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ExtendedUnicodeEscape;
        }

        return (int)hexValue;
    }

    // scanLowSurrogateEscape attempts to consume a low-surrogate Unicode escape
    // (either '\uLow' or '\u{Low}') immediately following an already-scanned high
    // surrogate and combine them into a single supplementary code point. This
    // mirrors how adjacent UTF-16 code units form a surrogate pair in a JavaScript
    // string, regardless of which escape syntax produced each half. On success it
    // returns the combined code point and true; otherwise it restores the scanner
    // position and returns false.
    private (int, bool) scanLowSurrogateEscape(int high)
    {
        if (this.charCode() != '\\' || this.charAt(1) != 'u')
        {
            return (0, false);
        }

        var savedPos = this.pos;
        var savedTokenFlags = this.tokenFlags;
        var low = this.scanUnicodeEscape(false);
        if (StringUtil.IsLowSurrogate(low))
        {
            return (StringUtil.SurrogatePairToCodePoint(high, low), true);
        }

        this.pos = savedPos;
        this.tokenFlags = savedTokenFlags;
        return (0, false);
    }

    // Current character is known to be a backslash. Check for Unicode escape of the form '\uXXXX'
    // or '\u{XXXXXX}' and return code point value if valid Unicode escape is found. Otherwise return -1.
    private int peekUnicodeEscape()
    {
        if (this.charAt(1) == 'u')
        {
            var savePos = this.pos;
            var saveTokenFlags = this.tokenFlags;
            var codePoint = this.scanUnicodeEscape(false);
            this.pos = savePos;
            this.tokenFlags = saveTokenFlags;
            return codePoint;
        }

        return -1;
    }

    private Kind scanNumber()
    {
        var start = this.pos;
        string fixedPart = "";
        if (this.charCode() == '0')
        {
            this.pos++;
            if (this.charCode() == '_')
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsSeparator | global::TypeScript.Scanner.TokenFlags.ContainsInvalidSeparator;
                this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos, 1);
                this.pos = start;
                fixedPart = this.scanNumberFragment();
            }
            else
            {
                var(digits, isOctal) = this.scanDigits();
                if (digits == "")
                {
                    fixedPart = "0";
                }
                else
                {
                    if (!isOctal)
                    {
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsLeadingZero;
                        fixedPart = digits;
                    }
                    else
                    {
                        var(val, _) = Strconv.ParseInt(digits, 8, 64);
                        this.tokenValue = Strconv.FormatInt(val, 10);
                        this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Octal;
                        var withMinus = this.token == Kind.MinusToken;
                        var literal = Core.IfElse(withMinus, "-", "") + "0o" + Strconv.FormatInt(val, 8);
                        if (withMinus)
                        {
                            start--;
                        }

                        this.errorAt(Diagnostics.Octal_literals_are_not_allowed_Use_the_syntax_0, start, this.pos - start, literal);
                        return Kind.NumericLiteral;
                    }
                }
            }
        }
        else
        {
            fixedPart = this.scanNumberFragment();
        }

        var fixedPartEnd = this.pos;
        var fractionalPart = "";
        var exponentPreamble = "";
        var exponentPart = "";
        if (this.charCode() == '.')
        {
            this.pos++;
            fractionalPart = this.scanNumberFragment();
        }

        var end = this.pos;
        if (this.charCode() == 'E' || this.charCode() == 'e')
        {
            this.pos++;
            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.Scientific;
            if (this.charCode() == '+' || this.charCode() == '-')
            {
                this.pos++;
            }

            var startNumericPart = this.pos;
            exponentPart = this.scanNumberFragment();
            if (exponentPart == "")
            {
                this.error(Diagnostics.Digit_expected);
            }
            else
            {
                exponentPreamble = this.text.Substring(end, startNumericPart - end);
                end = this.pos;
            }
        }

        if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.ContainsSeparator) != 0)
        {
            this.tokenValue = fixedPart;
            if (fractionalPart != "")
            {
                this.tokenValue += "." + fractionalPart;
            }

            if (exponentPart != "")
            {
                this.tokenValue += exponentPreamble + exponentPart;
            }
        }
        else
        {
            this.setTokenValueView(this.text.Slice(start, end));
        }

        if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.ContainsLeadingZero) != 0)
        {
            this.errorAt(Diagnostics.Decimals_with_leading_zeros_are_not_allowed, start, this.pos - start);
            this.tokenValue = JsNum.FromString(this.tokenValue).String();
            return Kind.NumericLiteral;
        }

        Kind result = default;
        if (fixedPartEnd == this.pos)
        {
            result = this.scanBigIntSuffix();
        }
        else
        {
            this.tokenValue = JsNum.FromString(this.tokenValue).String();
            result = Kind.NumericLiteral;
        }

        var(ch, _) = this.charAndSize();
        if (IsIdentifierStart(ch))
        {
            var idStart = this.pos;
            var id = this.scanIdentifierParts(identifierVariant.Standard);
            if (result != Kind.BigIntLiteral && id.Length == 1 && this.text[idStart] == 'n')
            {
                if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.Scientific) != 0)
                {
                    this.errorAt(Diagnostics.A_bigint_literal_cannot_use_exponential_notation, start, this.pos - start);
                    return result;
                }

                if (fixedPartEnd < idStart)
                {
                    this.errorAt(Diagnostics.A_bigint_literal_must_be_an_integer, start, this.pos - start);
                    return result;
                }
            }

            this.errorAt(Diagnostics.An_identifier_or_keyword_cannot_immediately_follow_a_numeric_literal, idStart, this.pos - idStart);
            this.pos = idStart;
        }

        return result;
    }

    private string scanNumberFragment()
    {
        var start = this.pos;
        var allowSeparator = false;
        var isPreviousTokenSeparator = false;
        GoBuilder result = new GoBuilder();
        while (true)
        {
            var before = this.pos;
            this.scanASCIIWhile((b) =>
            {
                return b >= '0' && b <= '9';
            });
            if (this.pos > before)
            {
                allowSeparator = true;
                isPreviousTokenSeparator = false;
            }

            var ch = this.charCode();
            if (ch == '_')
            {
                this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsSeparator;
                if (allowSeparator)
                {
                    allowSeparator = false;
                    isPreviousTokenSeparator = true;
                    // scanner.go:2044. Builder consumes the source slice without an intermediate string.
                    result.WriteString(this.text.AsSpan(start, this.pos - start));
                }
                else
                {
                    this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidSeparator;
                    if (isPreviousTokenSeparator)
                    {
                        this.errorAt(Diagnostics.Multiple_consecutive_numeric_separators_are_not_permitted, this.pos, 1);
                    }
                    else
                    {
                        this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos, 1);
                    }
                }

                this.pos++;
                start = this.pos;
                continue;
            }

            break;
        }

        if (isPreviousTokenSeparator)
        {
            this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsInvalidSeparator;
            this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos - 1, 1);
        }

        if (result.Len() == 0)
        {
            return this.text.Substring(start, this.pos - start);
        }

        result.WriteString(this.text.AsSpan(start, this.pos - start));
        return result.String();
    }

    private (string, bool) scanDigits()
    {
        var start = this.pos;
        var isOctal = true;
        while (StringUtil.IsDigit(this.charCode()))
        {
            if (!StringUtil.IsOctalDigit(this.charCode()))
            {
                isOctal = false;
            }

            this.pos++;
        }

        return (this.text.Substring(start, this.pos - start), isOctal);
    }

    private string scanHexDigits(int minCount, bool scanAsManyAsPossible, bool canHaveSeparators)
    {
        var digitCount = 0;
        var start = this.pos;
        var allowSeparator = false;
        var isPreviousTokenSeparator = false;
        while (digitCount < minCount || scanAsManyAsPossible)
        {
            var ch = this.charCode();
            if (StringUtil.IsHexDigit(ch))
            {
                allowSeparator = canHaveSeparators;
                isPreviousTokenSeparator = false;
                digitCount++;
            }
            else
            {
                if (canHaveSeparators && ch == '_')
                {
                    this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsSeparator;
                    if (allowSeparator)
                    {
                        allowSeparator = false;
                        isPreviousTokenSeparator = true;
                    }
                    else
                    {
                        if (isPreviousTokenSeparator)
                        {
                            this.errorAt(Diagnostics.Multiple_consecutive_numeric_separators_are_not_permitted, this.pos, 1);
                        }
                        else
                        {
                            this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos, 1);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            this.pos++;
        }

        if (isPreviousTokenSeparator)
        {
            this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos - 1, 1);
        }

        if (digitCount < minCount)
        {
            return "";
        }

        var digits = this.text.Substring(start, this.pos - start);
        if (this.hexDigitCache == null)
        {
            this.hexDigitCache = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        {
            var ok = this.hexDigitCache.TryGetValue(digits, out var cached);
            if (ok)
            {
                return cached!;
            }
            else
            {
                var original = digits;
                if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.ContainsSeparator) != 0)
                {
                    digits = GoStrings.ReplaceAll(digits, "_", "");
                }

                digits = GoStrings.ToLower(digits);
                this.hexDigitCache[original] = digits;
                return digits;
            }
        }
    }

    private string scanBinaryOrOctalDigits(int radix)
    {
        GoBuilder sb = new GoBuilder();
        var allowSeparator = false;
        var isPreviousTokenSeparator = false;
        while (true)
        {
            var ch = this.charCode();
            if (StringUtil.IsDigit(ch) && ch - '0' < radix)
            {
                sb.WriteByte((int)ch);
                allowSeparator = true;
                isPreviousTokenSeparator = false;
            }
            else
            {
                if (ch == '_')
                {
                    this.tokenFlags |= global::TypeScript.Scanner.TokenFlags.ContainsSeparator;
                    if (allowSeparator)
                    {
                        allowSeparator = false;
                        isPreviousTokenSeparator = true;
                    }
                    else
                    {
                        if (isPreviousTokenSeparator)
                        {
                            this.errorAt(Diagnostics.Multiple_consecutive_numeric_separators_are_not_permitted, this.pos, 1);
                        }
                        else
                        {
                            this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos, 1);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            this.pos++;
        }

        if (isPreviousTokenSeparator)
        {
            this.errorAt(Diagnostics.Numeric_separators_are_not_allowed_here, this.pos - 1, 1);
        }

        return sb.String();
    }

    private Kind scanBigIntSuffix()
    {
        if (this.charCode() == 'n')
        {
            this.tokenValue += "n";
            if ((this.tokenFlags & global::TypeScript.Scanner.TokenFlags.BinaryOrOctalSpecifier) != 0)
            {
                this.tokenValue = JsNum.ParsePseudoBigInt(this.tokenValue) + "n";
            }

            this.pos++;
            return Kind.BigIntLiteral;
        }

        if (this.numberCache == null)
        {
            this.numberCache = new Dictionary<string, string>();
        }

        {
            var ok = this.numberCache.TryGetValue(this.tokenValue, out var cached);
            if (ok)
            {
                this.tokenValue = cached!;
            }
            else
            {
                var tokenValue = JsNum.FromString(this.tokenValue).String();
                if (tokenValue == this.tokenValue)
                {
                    tokenValue = this.tokenValue;
                }

                this.numberCache[this.tokenValue] = tokenValue;
                this.tokenValue = tokenValue;
            }
        }

        return Kind.NumericLiteral;
    }

    private void scanInvalidCharacter()
    {
        var(_, size) = this.charAndSize();
        this.errorAt(Diagnostics.Invalid_character, this.pos, size);
        this.pos += size;
        this.token = Kind.Unknown;
    }

    // Section 6.1.4
    private static bool isWordCharacter(int ch)
    {
        return StringUtil.IsASCIILetter(ch) || StringUtil.IsDigit(ch) || ch == '_';
    }

    public static bool IsIdentifierStart(int ch)
    {
        return StringUtil.IsASCIILetter(ch) || ch == '_' || ch == '$' || ch >= Utf8.RuneSelf && StringUtil.IsUnicodeIdentifierStart(ch);
    }

    public static bool IsIdentifierPart(int ch)
    {
        return IsIdentifierPartEx(ch, LanguageVariant.Standard);
    }

    public static bool IsIdentifierPartEx(int ch, LanguageVariant languageVariant)
    {
        return isWordCharacter(ch) || ch == '$' || ch >= Utf8.RuneSelf && StringUtil.IsUnicodeIdentifierPart(ch) || languageVariant == LanguageVariant.JSX && ch == '-';
    }

    private static bool couldStartTrivia(string text, int pos)
    {
        {
            int ch = text[pos];
            switch (ch)
            {
                case '\r':
                case '\n':
                case '\t':
                case '\v':
                case '\f':
                case ' ':
                case '/':
                case '<':
                case '|':
                case '=':
                case '>':
                {
                    return true;
                }

                case '#':
                {
                    return pos == 0;
                }

                default:
                {
                    return ch > maxAsciiCharacter;
                }
            }
        }
    }

    public static int SkipTrivia(string text, int pos)
    {
        return skipTriviaEx(GoString.ToUtf8(text), pos, null);
    }

    private static int skipTriviaEx(ByteStringView text, int pos, SkipTriviaOptions? options)
    {
        if (pos < 0)
        {
            return pos;
        }

        if (options == null)
        {
            options = new SkipTriviaOptions
            {
            };
        }

        var textLen = text.Length;
        var canConsumeStar = false;
        while (true)
        {
            if (pos >= textLen)
            {
                return pos;
            }

            var(ch, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
            switch (ch)
            {
                case '\r':
                {
                    if (pos + 1 < textLen && text[pos + 1] == '\n')
                    {
                        pos++;
                    }

                    goto case '\n';
                }

                case '\n':
                {
                    pos++;
                    if (options.StopAfterLineBreak)
                    {
                        return pos;
                    }

                    canConsumeStar = options.InJSDoc;
                    continue;
                }

                case '\t':
                case '\v':
                case '\f':
                case ' ':
                {
                    pos++;
                    continue;
                }

                case '/':
                {
                    if (options.StopAtComments)
                    {
                        break;
                    }

                    if (pos + 1 < textLen)
                    {
                        if (text[pos + 1] == '/')
                        {
                            pos += 2;
                            while (pos < textLen)
                            {
                                var(commentCh, commentSize) = Utf8.DecodeRuneInString(text.AsSpan(pos));
                                if (StringUtil.IsLineBreak(commentCh))
                                {
                                    break;
                                }

                                pos += commentSize;
                            }

                            canConsumeStar = false;
                            continue;
                        }

                        if (text[pos + 1] == '*')
                        {
                            pos += 2;
                            while (pos < textLen)
                            {
                                if (text[pos] == '*' && pos + 1 < textLen && text[pos + 1] == '/')
                                {
                                    pos += 2;
                                    break;
                                }

                                var(_, commentSize) = Utf8.DecodeRuneInString(text.AsSpan(pos));
                                pos += commentSize;
                            }

                            canConsumeStar = false;
                            continue;
                        }
                    }

                    break;
                }

                case '<':
                case '|':
                case '=':
                case '>':
                {
                    if (isConflictMarkerTrivia(text, pos))
                    {
                        pos = scanConflictMarkerTrivia(text, pos, null);
                        canConsumeStar = false;
                        continue;
                    }

                    break;
                }

                case '#':
                {
                    if (pos == 0 && isShebangTrivia(text, pos))
                    {
                        pos = scanShebangTrivia(text, pos);
                        canConsumeStar = false;
                        continue;
                    }

                    break;
                }

                case '*':
                {
                    if (canConsumeStar)
                    {
                        pos++;
                        canConsumeStar = false;
                        continue;
                    }

                    break;
                }

                default:
                {
                    if (ch > (int)maxAsciiCharacter && StringUtil.IsWhiteSpaceLike(ch))
                    {
                        pos += size;
                        continue;
                    }

                    break;
                }
            }

            return pos;
        }
    }

    private static bool isConflictMarkerTrivia(ByteStringView text, int pos)
    {
        if (pos < 0)
        {
            throw new InvalidOperationException("pos < 0");
        }

        if (pos + 1 >= text.Length || text[pos + 1] != text[pos])
        {
            return false;
        }

        var atLineStart = pos == 0 || StringUtil.IsLineBreak((int)text[pos - 1]);
        if (!atLineStart && pos >= 2)
        {
            var(prev, _) = Utf8.DecodeLastRuneInString(text.AsSpan(0, pos - 2 - 0));
            atLineStart = StringUtil.IsLineBreak(prev);
        }

        if (atLineStart)
        {
            int ch = text[pos];
            if (pos + mergeConflictMarkerLength < text.Length)
            {
                for (int i = 0; i < mergeConflictMarkerLength; i++)
                {
                    if (text[pos + i] != ch)
                        return false;
                }

                return ch == '=' || text[pos + mergeConflictMarkerLength] == ' ';
            }
        }

        return false;
    }

    private static int scanConflictMarkerTrivia(ByteStringView text, int pos, ErrorCallback? reportError)
    {
        if (reportError != null)
        {
            reportError(Diagnostics.Merge_conflict_marker_encountered, pos, mergeConflictMarkerLength);
        }

        var(ch, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
        var length = text.Length;
        if (ch == '<' || ch == '>')
        {
            while (pos < length && !StringUtil.IsLineBreak(ch))
            {
                pos += size;
                (ch, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
            }
        }
        else
        {
            if (ch != '|' && ch != '=')
            {
                throw new InvalidOperationException("Assertion failed: ch must be either '|' or '='");
            }

            while (pos < length)
            {
                var currentChar = text[pos];
                if ((currentChar == '=' || currentChar == '>') && (int)currentChar != ch && isConflictMarkerTrivia(text, pos))
                {
                    break;
                }

                pos++;
            }
        }

        return pos;
    }

    private static bool isShebangTrivia(ByteStringView text, int pos)
    {
        if (text.Length < 2)
        {
            return false;
        }

        if (pos != 0)
        {
            throw new InvalidOperationException("Shebangs check must only be done at the start of the file");
        }

        return text[0] == '#' && text[1] == '!';
    }

    private static int scanShebangTrivia(ByteStringView text, int pos)
    {
        pos += 2;
        while (pos < text.Length)
        {
            var(ch, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
            if (StringUtil.IsLineBreak(ch))
            {
                break;
            }

            pos += size;
        }

        return pos;
    }

    private static string getShebang(string text)
    {
        if (!isShebangTrivia(text, 0))
        {
            return "";
        }

        var end = scanShebangTrivia(text, 0);
        return text.Substring(0, end - 0);
    }

    public static int ComputeLineOfPosition(int[] lineStarts, int pos)
    {
        var low = 0;
        var high = lineStarts.Length - 1;
        while (low <= high)
        {
            var middle = low + (high - low >> 1);
            var value = (int)lineStarts[middle];
            if (value < pos)
            {
                low = middle + 1;
            }
            else
            {
                if (value > pos)
                {
                    high = middle - 1;
                }
                else
                {
                    return middle;
                }
            }
        }

        return low - 1;
    }

    // ComputePositionOfLineAndByteOffset computes a byte position from a line and
    // raw byte offset from the line start. This is a simple addition with validation.
    public static int ComputePositionOfLineAndByteOffset(int[] lineStarts, int line, int byteOffset)
    {
        if (line < 0 || line >= lineStarts.Length)
        {
            throw new InvalidOperationException(Fmt.Sprintf("Bad line number. Line: %d, lineStarts.length: %d.", line, lineStarts.Length));
        }

        return (int)lineStarts[line] + byteOffset;
    }

    // ComputePositionOfLineAndUTF16Character converts a line and UTF-16 character offset
    // back to a byte position. The character parameter is measured in UTF-16 code units.
    // It scans from the line start to correctly handle multi-byte characters.
    // When allowEdits is true, out-of-range values are clamped instead of panicking.
    private static int computePositionOfLineAndUTF16Character(int[] lineStarts, int line, int character, ByteStringView text, bool allowEdits)
    {
        if (line < 0 || line >= lineStarts.Length)
        {
            if (allowEdits)
            {
                if (line < 0)
                {
                    line = 0;
                }
                else
                {
                    if (line >= lineStarts.Length)
                    {
                        line = lineStarts.Length - 1;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(Fmt.Sprintf("Bad line number. Line: %d, lineStarts.length: %d.", line, lineStarts.Length));
            }
        }

        var lineStart = (int)lineStarts[line];
        if (character > 0)
        {
            var lineEnd = text.Length;
            if (line + 1 < lineStarts.Length)
            {
                lineEnd = (int)lineStarts[line + 1];
            }

            var utf16Count = (int)0;
            var pos = lineStart;
            while (pos < lineEnd)
            {
                if (utf16Count >= character)
                {
                    break;
                }

                var(r, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
                utf16Count += (int)Utf16.RuneLen(r);
                pos += size;
            }

            if (!allowEdits)
            {
                if (pos == lineEnd && utf16Count < character)
                {
                    throw new InvalidOperationException(Fmt.Sprintf("Bad UTF-16 character offset. Line: %d, character: %d.", line, character));
                }

                Debug.Assert(pos <= text.Length);
                return pos;
            }

            if (pos > text.Length)
            {
                return text.Length;
            }

            return pos;
        }

        var res = lineStart;
        if (allowEdits)
        {
            if (res > text.Length)
            {
                return text.Length;
            }

            return res;
        }

        Debug.Assert(res <= text.Length);
        return res;
    }

    public static IEnumerable<CommentRange> GetLeadingCommentRanges(string text, int pos) => iterateCommentRanges(null, GoString.ToUtf8(text), pos, false);
    public static IEnumerable<CommentRange> GetLeadingCommentRanges(byte[] text, int pos) => iterateCommentRanges(null, GoString.FromBytes(text), pos, false);
    public static IEnumerable<CommentRange> GetTrailingCommentRanges(string text, int pos) => iterateCommentRanges(null, GoString.ToUtf8(text), pos, true);
    public static IEnumerable<CommentRange> GetTrailingCommentRanges(byte[] text, int pos) => iterateCommentRanges(null, GoString.FromBytes(text), pos, true);
    public static IEnumerable<CommentRange> GetLeadingCommentRanges(NodeFactory? factory, string text, int pos) => iterateCommentRanges(factory, GoString.ToUtf8(text), pos, false);
    public static IEnumerable<CommentRange> GetLeadingCommentRanges(NodeFactory? factory, byte[] text, int pos) => iterateCommentRanges(factory, GoString.FromBytes(text), pos, false);
    public static IEnumerable<CommentRange> GetTrailingCommentRanges(NodeFactory? factory, string text, int pos) => iterateCommentRanges(factory, GoString.ToUtf8(text), pos, true);
    public static IEnumerable<CommentRange> GetTrailingCommentRanges(NodeFactory? factory, byte[] text, int pos) => iterateCommentRanges(factory, GoString.FromBytes(text), pos, true);
    // ast.CommentRange is a value struct; NewCommentRange has no node hooks/identity.
    private static IEnumerable<CommentRange> iterateCommentRanges(NodeFactory? factory, ByteStringView text, int pos, bool trailing)
    {
        int pendingPos = 0, pendingEnd = 0;
        Kind pendingKind = default;
        bool pendingHasTrailingNewLine = false, hasPendingCommentRange = false;
        bool collecting = trailing;
        if (pos == 0)
        {
            collecting = true;
            if (isShebangTrivia(text, pos))
                pos = scanShebangTrivia(text, pos);
        }

        while (pos >= 0 && pos < text.Length)
        {
            var(ch, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
            switch (ch)
            {
                case '\r':
                    if (pos + 1 < text.Length && text[pos + 1] == '\n')
                        pos++;
                    goto case '\n';
                case '\n':
                    pos++;
                    if (trailing)
                        goto scanEnd;
                    collecting = true;
                    if (hasPendingCommentRange)
                        pendingHasTrailingNewLine = true;
                    continue;
                case '\t':
                case '\v':
                case '\f':
                case ' ':
                    pos++;
                    continue;
                case '/':
                {
                    int nextChar = pos + 1 < text.Length ? text[pos + 1] : 0;
                    bool hasTrailingNewLine = false;
                    if (nextChar == '/' || nextChar == '*')
                    {
                        Kind kind = nextChar == '/' ? Kind.SingleLineCommentTrivia : Kind.MultiLineCommentTrivia;
                        int startPos = pos;
                        pos += 2;
                        if (nextChar == '/')
                        {
                            while (pos < text.Length)
                            {
                                var(c, s) = Utf8.DecodeRuneInString(text.AsSpan(pos));
                                if (StringUtil.IsLineBreak(c))
                                {
                                    hasTrailingNewLine = true;
                                    break;
                                }

                                pos += s;
                            }
                        }
                        else
                        {
                            // Go searches the shared suffix, not an allocated copy.
                            int i = GoStrings.Index(text.AsSpan(pos), "*/"u8);
                            pos = i >= 0 ? pos + i + 2 : text.Length;
                        }

                        if (collecting)
                        {
                            if (hasPendingCommentRange)
                                yield return factory?.NewCommentRange(pendingKind, pendingPos, pendingEnd, pendingHasTrailingNewLine) ?? new CommentRange(pendingKind, pendingPos, pendingEnd, pendingHasTrailingNewLine);
                            pendingPos = startPos;
                            pendingEnd = pos;
                            pendingKind = kind;
                            pendingHasTrailingNewLine = hasTrailingNewLine;
                            hasPendingCommentRange = true;
                        }

                        continue;
                    }

                    goto scanEnd;
                }

                default:
                    if (ch > 127 && StringUtil.IsWhiteSpaceLike(ch))
                    {
                        if (hasPendingCommentRange && StringUtil.IsLineBreak(ch))
                            pendingHasTrailingNewLine = true;
                        pos += size;
                        continue;
                    }

                    goto scanEnd;
            }
        }

        scanEnd:
            if (hasPendingCommentRange)
                yield return factory?.NewCommentRange(pendingKind, pendingPos, pendingEnd, pendingHasTrailingNewLine) ?? new CommentRange(pendingKind, pendingPos, pendingEnd, pendingHasTrailingNewLine);
    }

    public Scanner()
    {
    }

    public static Scanner NewScanner() => new();
    public void Reset()
    {
        numberCache?.Clear();
        hexNumberCache?.Clear();
        hexDigitCache?.Clear();
        text = "";
        end = pos = fullStartPos = tokenStart = skipJSDocLeadingAsterisks = 0;
        languageVariant = default;
        scriptTarget = default;
        onError = null;
        onErrorUsesByteStrings = false;
        skipTrivia = true;
        token = default;
        tokenValue = "";
        tokenFlags = default;
        commentDirectives = default;
    }

    public string Text() => text.DecodeUnicode();
    public byte[] TextBytes() => text.ToArray();
    public string TokenText() => text.Slice(tokenStart, pos).DecodeUnicode();
    public byte[] TokenTextBytes() => text.Slice(tokenStart, pos).ToArray();
    public string TokenValue() => ScannerString.ToJsString(tokenValue);
    public byte[] TokenValueBytes() => tokenValueIsView ? tokenValueView.ToArray() : GoString.Bytes(tokenValue);
    public void SetText(string value) => SetByteText(ByteStringView.FromUnicode(value));
    public void SetText(byte[] value) => SetByteText(ByteStringView.CopyFrom(value));
    public void SetText(ReadOnlySpan<byte> value) => SetByteText(ByteStringView.CopyFrom(value));
    private void SetByteText(ByteStringView value)
    {
        text = value;
        end = value.Length;
        pos = fullStartPos = tokenStart = skipJSDocLeadingAsterisks = 0;
        token = default;
        tokenFlags = default;
        tokenValue = "";
        commentDirectives = default;
    }

    public ScannerState Mark() => new()
    {
        pos = pos,
        fullStartPos = fullStartPos,
        tokenStart = tokenStart,
        token = token,
        tokenValueString = tokenValueString,
        tokenValueView = tokenValueView,
        tokenValueIsView = tokenValueIsView,
        tokenFlags = tokenFlags,
        commentDirectives = commentDirectives,
        skipJSDocLeadingAsterisks = skipJSDocLeadingAsterisks
    };
    public void Rewind(ScannerState state)
    {
        pos = state.pos;
        fullStartPos = state.fullStartPos;
        tokenStart = state.tokenStart;
        token = state.token;
        tokenValueString = state.tokenValueIsView ? state.tokenValueString : state.tokenValueString ?? "";
        tokenValueView = state.tokenValueView;
        tokenValueIsView = state.tokenValueIsView;
        tokenFlags = state.tokenFlags;
        commentDirectives = state.commentDirectives;
        skipJSDocLeadingAsterisks = state.skipJSDocLeadingAsterisks;
    }

    public static int SkipTriviaEx(string text, int pos, SkipTriviaOptions? options) => skipTriviaEx(GoString.ToUtf8(text), pos, options!);
    public static int SkipTriviaEx(byte[] text, int pos, SkipTriviaOptions? options) => skipTriviaEx(GoString.FromBytes(text), pos, options!);
    public static int SkipTrivia(byte[] text, int pos) => skipTriviaEx(GoString.FromBytes(text), pos, null!);
    public static string GetShebang(string text) => GoString.ToString(getShebang(GoString.ToUtf8(text)));
    public static byte[] GetShebang(byte[] text) => GoString.Bytes(getShebang(GoString.FromBytes(text)));
    public static int ComputePositionOfLineAndUTF16Character(int[] lineStarts, int line, int character, string text, bool allowEdits) => computePositionOfLineAndUTF16Character(lineStarts, line, character, GoString.ToUtf8(text), allowEdits);
    public static int ComputePositionOfLineAndUTF16Character(int[] lineStarts, int line, int character, byte[] text, bool allowEdits) => computePositionOfLineAndUTF16Character(lineStarts, line, character, ByteStringView.CopyFrom(text), allowEdits);
    // scanner.go getIdentifierToken: the keyword table uses raw byte strings.
    // Its bounded keys can use .NET's ordinal alternate lookup without owning
    // a temporary token string or encoding non-ASCII source bytes again.
    private Kind getIdentifierToken() => GetIdentifierToken(TokenValueTextValue());
    internal static Kind GetIdentifierToken(ByteStringValue text)
    {
        if (!text.IsBytes)
            return GetIdentifierToken(text.Materialize()!);
        var bytes = text.ByteSpan;
        if (bytes.Length < 2 || bytes.Length > 12 || bytes[0] < 'a' || bytes[0] > 'z')
            return Kind.Identifier;
        Span<char> chars = stackalloc char[12];
        for (int i = 0; i < bytes.Length; i++)
            chars[i] = (char)bytes[i];
        return textToKeyword.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(chars[..bytes.Length], out var keyword)
            ? keyword : Kind.Identifier;
    }

    public static Kind GetIdentifierToken(string str)
    {
        if (str.Length >= 2 && str.Length <= 12 && str[0] >= 'a' && str[0] <= 'z' && textToKeyword.TryGetValue(str, out var keyword))
            return keyword;
        return Kind.Identifier;
    }

    public static bool IsValidIdentifier(string value) => isValidIdentifier(GoString.ToUtf8(value));
    public static bool IsValidIdentifier(byte[] value) => isValidIdentifier(GoString.FromBytes(value));
    // scanner.go:2224 range decodes the shared byte-string suffix without copying it.
    private static bool isValidIdentifier(string value)
    {
        if (value.Length == 0)
            return false;
        for (int i = 0; i < value.Length;)
        {
            var(ch, size) = Utf8.DecodeRuneInString(value.AsSpan(i));
            if (i == 0 && !IsIdentifierStart(ch) || i != 0 && !IsIdentifierPart(ch))
                return false;
            i += size;
        }

        return true;
    }

    public static string TokenToString(Kind token) => tokenToText[(int)token];
    public static Kind StringToToken(string s) => textToToken.GetValueOrDefault(s);
    public static string[] GetViableKeywordSuggestions() => textToKeyword.Keys.Where(x => x.Length > 2).ToArray();
    // scanner.go:2255. The Go fixed-size array preserves bounds failures for invalid kinds.
    private static string[] CreateTokenToText()
    {
        var result = new string[(int)Kind.Count];
        Array.Fill(result, "");
        foreach (var entry in textToToken)
            result[(int)entry.Value] = entry.Key;
        return result;
    }

    private static Dictionary<string, Kind> CreateTextToToken()
    {
        var result = new Dictionary<string, Kind>(textToKeyword, StringComparer.Ordinal)
        {
            ["{"] = Kind.OpenBraceToken,
            ["}"] = Kind.CloseBraceToken,
            ["("] = Kind.OpenParenToken,
            [")"] = Kind.CloseParenToken,
            ["["] = Kind.OpenBracketToken,
            ["]"] = Kind.CloseBracketToken,
            ["."] = Kind.DotToken,
            ["..."] = Kind.DotDotDotToken,
            [";"] = Kind.SemicolonToken,
            [","] = Kind.CommaToken,
            ["<"] = Kind.LessThanToken,
            [">"] = Kind.GreaterThanToken,
            ["<="] = Kind.LessThanEqualsToken,
            [">="] = Kind.GreaterThanEqualsToken,
            ["=="] = Kind.EqualsEqualsToken,
            ["!="] = Kind.ExclamationEqualsToken,
            ["==="] = Kind.EqualsEqualsEqualsToken,
            ["!=="] = Kind.ExclamationEqualsEqualsToken,
            ["=>"] = Kind.EqualsGreaterThanToken,
            ["+"] = Kind.PlusToken,
            ["-"] = Kind.MinusToken,
            ["**"] = Kind.AsteriskAsteriskToken,
            ["*"] = Kind.AsteriskToken,
            ["/"] = Kind.SlashToken,
            ["%"] = Kind.PercentToken,
            ["++"] = Kind.PlusPlusToken,
            ["--"] = Kind.MinusMinusToken,
            ["<<"] = Kind.LessThanLessThanToken,
            ["</"] = Kind.LessThanSlashToken,
            [">>"] = Kind.GreaterThanGreaterThanToken,
            [">>>"] = Kind.GreaterThanGreaterThanGreaterThanToken,
            ["&"] = Kind.AmpersandToken,
            ["|"] = Kind.BarToken,
            ["^"] = Kind.CaretToken,
            ["!"] = Kind.ExclamationToken,
            ["~"] = Kind.TildeToken,
            ["&&"] = Kind.AmpersandAmpersandToken,
            ["||"] = Kind.BarBarToken,
            ["?"] = Kind.QuestionToken,
            ["??"] = Kind.QuestionQuestionToken,
            ["?."] = Kind.QuestionDotToken,
            [":"] = Kind.ColonToken,
            ["="] = Kind.EqualsToken,
            ["+="] = Kind.PlusEqualsToken,
            ["-="] = Kind.MinusEqualsToken,
            ["*="] = Kind.AsteriskEqualsToken,
            ["**="] = Kind.AsteriskAsteriskEqualsToken,
            ["/="] = Kind.SlashEqualsToken,
            ["%="] = Kind.PercentEqualsToken,
            ["<<="] = Kind.LessThanLessThanEqualsToken,
            [">>="] = Kind.GreaterThanGreaterThanEqualsToken,
            [">>>="] = Kind.GreaterThanGreaterThanGreaterThanEqualsToken,
            ["&="] = Kind.AmpersandEqualsToken,
            ["|="] = Kind.BarEqualsToken,
            ["^="] = Kind.CaretEqualsToken,
            ["||="] = Kind.BarBarEqualsToken,
            ["&&="] = Kind.AmpersandAmpersandEqualsToken,
            ["??="] = Kind.QuestionQuestionEqualsToken,
            ["@"] = Kind.AtToken,
            ["#"] = Kind.HashToken,
            ["`"] = Kind.BacktickToken,
        };
        return result;
    }

    // Upstream scanner.go source-file scanning and diagnostic span helpers.
    public static Scanner GetScannerForSourceFile(SourceFile sourceFile, int pos)
    {
        var scanner = NewScanner();
        scanner.text = sourceFile.TextView();
        scanner.pos = pos;
        scanner.end = scanner.text.Length;
        scanner.languageVariant = sourceFile.LanguageVariant;
        scanner.Scan();
        return scanner;
    }

    public static Kind ScanTokenAtPosition(SourceFile sourceFile, int pos) => GetScannerForSourceFile(sourceFile, pos).token;
    public static TextRange GetRangeOfTokenAtPosition(SourceFile sourceFile, int pos)
    {
        var scanner = GetScannerForSourceFile(sourceFile, pos);
        return Core.NewTextRange(scanner.tokenStart, scanner.pos);
    }

    public static List<int> GetECMALineStarts(SourceFileLike sourceFile) => sourceFile.ECMALineMap();
    public static int GetECMALineOfPosition(SourceFileLike sourceFile, int pos) => ComputeLineOfPosition(GetECMALineStarts(sourceFile).ToArray(), pos);
    // scanner.go:2685-2701. Positions are UTF8 byte offsets; characters are UTF16 code units.
    public static (int Line, int Character) GetECMALineAndUTF16CharacterOfPosition(SourceFileLike sourceFile, int pos)
    {
        var lineMap = GetECMALineStarts(sourceFile);
        int line = ComputeLineOfPosition(lineMap.ToArray(), pos);
        int character = Core.UTF16Len(sourceFile.TextView().Slice(lineMap[line], pos));
        return (line, character);
    }

    public static (int Line, int ByteOffset) GetECMALineAndByteOffsetOfPosition(SourceFileLike sourceFile, int pos)
    {
        var lineMap = GetECMALineStarts(sourceFile);
        int line = ComputeLineOfPosition(lineMap.ToArray(), pos);
        return (line, pos - lineMap[line]);
    }

    // scanner.go:2717-2728. Use the internal byte-string directly (no second UTF8 encoding).
    public static int GetECMAPositionOfLineAndUTF16Character(SourceFileLike sourceFile, int line, int character) => computePositionOfLineAndUTF16Character(GetECMALineStarts(sourceFile).ToArray(), line, character, sourceFile.TextView(), false);
    public static int GetECMAPositionOfLineAndByteOffset(SourceFileLike sourceFile, int line, int byteOffset) => ComputePositionOfLineAndByteOffset(GetECMALineStarts(sourceFile).ToArray(), line, byteOffset);
    public static int GetECMAEndLinePosition(SourceFile sourceFile, int line)
    {
        int pos = GetECMALineStarts(sourceFile)[line];
        while (true)
        {
            var(ch, size) = Utf8.DecodeRuneInString(sourceFile.TextView().AsSpan(pos));
            if (size == 0 || StringUtil.IsLineBreak(ch))
                return pos - 1;
            pos += size;
        }
    }

    private static TextRange getErrorRangeForArrowFunction(SourceFile sourceFile, Node node)
    {
        int pos = SkipTriviaByteString(sourceFile.TextView(), node.Pos());
        var body = node.Body();
        if (body != null && body.Kind == Kind.Block)
        {
            int startLine = GetECMALineOfPosition(sourceFile, body.Pos());
            int endLine = GetECMALineOfPosition(sourceFile, body.End());
            if (startLine < endLine)
                return Core.NewTextRange(pos, GetECMAEndLinePosition(sourceFile, startLine) + 1);
        }

        return Core.NewTextRange(pos, node.End());
    }

    private static Node? findOriginatingJSDocSatisfiesTag(SourceFile sourceFile, Node node)
    {
        var targetType = node.AsSatisfiesExpression().Type;
        if ((targetType.Flags & NodeFlags.Reparsed) == 0)
            return null;
        for (Node? current = node.Parent; current != null; current = current.Parent)
        {
            if ((current.Flags & NodeFlags.HasJSDoc) == 0)
                continue;
            Node? firstSatisfiesTag = null;
            foreach (var jsDoc in GoRuntime.RangeValues(current.EagerJSDoc(sourceFile)))
            {
                var tags = jsDoc.AsJSDoc().Tags;
                if (tags == null)
                    continue;
                foreach (var tag in GoRuntime.RangeValues(tags.Nodes))
                {
                    if (!Ast.IsJSDocSatisfiesTag(tag))
                        continue;
                    if (firstSatisfiesTag == null)
                        firstSatisfiesTag = tag;
                    var typeExpr = tag.AsJSDocSatisfiesTag().TypeExpression;
                    if (typeExpr != null)
                    {
                        var type = typeExpr.Type();
                        if (type != null && type.Loc == targetType.Loc)
                            return tag;
                    }
                }
            }

            return firstSatisfiesTag;
        }

        return null;
    }

    public static TextRange GetErrorRangeForNode(SourceFile sourceFile, Node node)
    {
        Node? errorNode = node;
        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                int pos = SkipTriviaByteString(sourceFile.TextView(), 0);
                if (pos == sourceFile.TextView().Length)
                    return Core.NewTextRange(0, 0);
                return GetRangeOfTokenAtPosition(sourceFile, pos);
            }

            case Kind.FunctionDeclaration:
            case Kind.MethodDeclaration:
                if ((node.Flags & NodeFlags.Reparsed) != 0)
                {
                    errorNode = node;
                    break;
                }

                goto case Kind.VariableDeclaration;
            case Kind.VariableDeclaration:
            case Kind.BindingElement:
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.EnumDeclaration:
            case Kind.EnumMember:
            case Kind.FunctionExpression:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.NamespaceImport:
                errorNode = Ast.GetNameOfDeclaration(node);
                break;
            case Kind.ClassExpression:
                errorNode = node.Name();
                break;
            case Kind.ArrowFunction:
                return getErrorRangeForArrowFunction(sourceFile, node);
            case Kind.CaseClause:
            case Kind.DefaultClause:
            {
                int start = SkipTriviaByteString(sourceFile.TextView(), node.Pos());
                int end = node.End();
                var statements = node.Statements();
                if (GoRuntime.Len(statements) != 0)
                    end = statements[0].Pos();
                return Core.NewTextRange(start, end);
            }

            case Kind.ReturnStatement:
            case Kind.YieldExpression:
                return GetRangeOfTokenAtPosition(sourceFile, SkipTriviaByteString(sourceFile.TextView(), node.Pos()));
            case Kind.SatisfiesExpression:
            {
                var jsDocSatisfiesTag = findOriginatingJSDocSatisfiesTag(sourceFile, node);
                if (jsDocSatisfiesTag != null)
                    return GetRangeOfTokenAtPosition(sourceFile, SkipTriviaByteString(sourceFile.TextView(), jsDocSatisfiesTag.TagName().Pos()));
                return GetRangeOfTokenAtPosition(sourceFile, SkipTriviaByteString(sourceFile.TextView(), node.AsSatisfiesExpression().Expression.End()));
            }

            case Kind.Constructor:
            {
                if ((node.Flags & NodeFlags.Reparsed) != 0)
                {
                    errorNode = node;
                    break;
                }

                var scanner = GetScannerForSourceFile(sourceFile, node.Pos());
                int start = scanner.TokenStart();
                while (scanner.Token() != Kind.ConstructorKeyword && scanner.Token() != Kind.StringLiteral && scanner.Token() != Kind.EndOfFile)
                    scanner.Scan();
                return Core.NewTextRange(start, scanner.TokenEnd());
            }
        }

        if (errorNode == null)
            return GetRangeOfTokenAtPosition(sourceFile, node.Pos());
        int errorPos = errorNode.Pos();
        if (!Ast.NodeIsMissing(errorNode) && !Ast.IsJsxText(errorNode))
            errorPos = SkipTriviaByteString(sourceFile.TextView(), errorPos);
        return Core.NewTextRange(errorPos, errorNode.End());
    }

    // Port of tsc/internal/scanner/scanner.go GetTokenPosOfNode.
    public static int GetTokenPosOfNode(Node node, SourceFile sourceFile, bool includeJSDoc)
    {
        if (Ast.NodeIsMissing(node))
            return node.Pos();
        if (Ast.IsJSDocNodeKind(node.Kind) || node.Kind == Kind.JsxText)
            return skipTriviaEx(sourceFile.TextView(), node.Pos(), new SkipTriviaOptions { StopAtComments = true });
        if (includeJSDoc && GoRuntime.Len(node.JSDoc(sourceFile)) > 0)
            return GetTokenPosOfNode(node.JSDoc(sourceFile)[0], sourceFile, false);
        return skipTriviaEx(sourceFile.TextView(), node.Pos(), new SkipTriviaOptions { InJSDoc = (node.Flags & NodeFlags.JSDoc) != 0 });
    }
}

public delegate void ErrorCallback(Message diagnostic, int start, int length, params object[] args);
// scanner.go:201,293,297: value snapshots share directive backing storage;
// restoring a copied header does not clone its mutable backing array.
public record struct ScannerState
{
    internal int pos, fullStartPos, tokenStart, skipJSDocLeadingAsterisks;
    internal Kind token;
    internal TokenFlags tokenFlags;
    internal string? tokenValueString;
    internal ByteStringView tokenValueView;
    internal bool tokenValueIsView;
    internal Slice<CommentDirective> commentDirectives;
}

public sealed class SkipTriviaOptions
{
    public bool StopAfterLineBreak { get; set; }
    public bool StopAtComments { get; set; }
    public bool InJSDoc { get; set; }
}

internal enum identifierVariant
{
    Standard,
    JSX,
    RegExpGroupName
}

[Flags]
internal enum EscapeSequenceScanningFlags
{
    String = 1 << 0,
    ReportErrors = 1 << 1,
    RegularExpression = 1 << 2,
    AnnexB = 1 << 3,
    AnyUnicodeMode = 1 << 4,
    AtomEscape = 1 << 5,
    ReportInvalidEscapeErrors = RegularExpression | ReportErrors,
    AllowExtendedUnicodeEscape = String | AnyUnicodeMode
}

// ast.go:3111. TextRange embedding is exposed through Loc and forwarding methods;
// Pos/End remain C# properties for compatibility with the existing public adapter.
public readonly record struct CommentRange(Kind Kind, int Pos, int End, bool HasTrailingNewLine)
{
    public TextRange Loc => new(Pos, End);

    public int Len() => Loc.Len();
    public bool IsValid() => Loc.IsValid();
    public bool Contains(int pos) => Loc.Contains(pos);
    public bool ContainsInclusive(int pos) => Loc.ContainsInclusive(pos);
    public bool ContainsExclusive(int pos) => Loc.ContainsExclusive(pos);
    public TextRange WithPos(int pos) => Loc.WithPos(pos);
    public TextRange WithEnd(int end) => Loc.WithEnd(end);
    public bool ContainedBy(TextRange range) => Loc.ContainedBy(range);
    public bool Overlaps(TextRange range) => Loc.Overlaps(range);
    public bool Intersects(TextRange range) => Loc.Intersects(range);
}
