// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// scanner.go:2800–2922 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
public partial class Scanner
{
    internal static CommentRangeSequence GetLeadingCommentRangesValue(NodeFactory? factory, ByteStringView text, int pos) => new(factory, text, pos, false);
    internal static CommentRangeSequence GetTrailingCommentRangesValue(NodeFactory? factory, ByteStringView text, int pos) => new(factory, text, pos, true);
    internal readonly struct CommentRangeSequence(NodeFactory? factory, ByteStringView text, int pos, bool trailing)
    {
        public CommentRangeEnumerator GetEnumerator() => new(factory, text, pos, trailing);
    }

    // Go iter.Seq's suspended closure becomes explicit value state. Text retains
    // its immutable owner; stopping enumeration does not scan subsequent input.
    internal struct CommentRangeEnumerator(NodeFactory? factory, ByteStringView text, int pos, bool trailing)
    {
        private readonly NodeFactory? factory = factory;
        private readonly ByteStringView text = text;
        private readonly bool trailing = trailing;
        private int pos = pos;
        private int pendingPos, pendingEnd;
        private Kind pendingKind;
        private bool pendingHasTrailingNewLine, hasPendingCommentRange;
        private bool collecting, initialized, finished, resumePending;
        private CommentRange nextPending;
        public CommentRange Current { get; private set; }

        private CommentRange createPendingRange() => factory?.NewCommentRange(pendingKind, pendingPos, pendingEnd, pendingHasTrailingNewLine) ?? new CommentRange(pendingKind, pendingPos, pendingEnd, pendingHasTrailingNewLine);
        public bool MoveNext()
        {
            if (finished)
                return false;
            if (!initialized)
            {
                initialized = true;
                collecting = trailing;
                if (pos == 0)
                {
                    collecting = true;
                    if (isShebangTrivia(text, pos))
                        pos = scanShebangTrivia(text, pos);
                }
            }

            // Resume immediately after Go's successful yield, before scanning.
            if (resumePending)
            {
                pendingPos = nextPending.Pos;
                pendingEnd = nextPending.End;
                pendingKind = nextPending.Kind;
                pendingHasTrailingNewLine = nextPending.HasTrailingNewLine;
                hasPendingCommentRange = true;
                resumePending = false;
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
                                    // Skip only ASCII bytes that cannot be line breaks.
                                    int candidate = Core.IndexOfECMALineScanCandidate(text.AsSpan(pos));
                                    if (candidate < 0)
                                    {
                                        pos = text.Length;
                                        break;
                                    }

                                    pos += candidate;
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
                                {
                                    Current = createPendingRange();
                                    nextPending = new(kind, startPos, pos, hasTrailingNewLine);
                                    resumePending = true;
                                    return true;
                                }

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
                finished = true;
            if (!hasPendingCommentRange)
                return false;
            Current = createPendingRange();
            return true;
        }
    }
}
