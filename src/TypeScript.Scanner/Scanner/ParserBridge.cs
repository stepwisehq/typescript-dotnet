// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Explicit byte-string adapters for the parser; public scanner Unicode APIs remain unchanged.
#nullable disable
namespace TypeScript.Scanner;
public partial class Scanner
{
    private bool onErrorUsesByteStrings;
    internal void SetByteTextInternal(string value) => SetByteText(value);
    internal void SetByteTextInternal(ByteStringView value) => SetByteText(value);
    internal ByteStringView TokenTextView() => text.Slice(tokenStart, pos);
    internal ByteStringView TokenValueTextView() => tokenValueIsView ? tokenValueView : (ByteStringView)tokenValueString;
    internal string TokenValueByteString() => tokenValue;
    internal string TokenTextByteString() => text.Substring(tokenStart, pos - tokenStart);
    internal string TextByteString() => text.Materialize();
    internal void SetOnErrorByteStrings(ErrorCallback value)
    {
        onError = value;
        onErrorUsesByteStrings = true;
    }

    internal static int SkipTrivia(ByteStringView source, int pos) => skipTriviaEx(source, pos, null);
    internal static int SkipTriviaEx(ByteStringView source, int pos, SkipTriviaOptions options) => skipTriviaEx(source, pos, options);
    internal static int SkipTriviaByteString(string source, int pos) => skipTriviaEx(source, pos, null);
    internal static int SkipTriviaByteString(ByteStringView source, int pos) => skipTriviaEx(source, pos, null);
    internal static bool IsValidIdentifierByteString(string source) => isValidIdentifier(source);
    internal static IEnumerable<CommentRange> GetLeadingCommentRangesByteString(string source, int pos) => iterateCommentRanges(null, source, pos, false);
    internal static IEnumerable<CommentRange> GetLeadingCommentRangesByteString(ByteStringView source, int pos) => iterateCommentRanges(null, source, pos, false);
    internal static IEnumerable<CommentRange> GetTrailingCommentRangesByteString(string source, int pos) => iterateCommentRanges(null, source, pos, true);
    internal static IEnumerable<CommentRange> GetTrailingCommentRangesByteString(ByteStringView source, int pos) => iterateCommentRanges(null, source, pos, true);
}
