// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Explicit byte-string adapters for the parser; public scanner Unicode APIs remain unchanged.
#nullable disable
namespace TypeScript.Scanner;
public partial class Scanner
{
    private bool onErrorUsesByteStrings;
    internal void SetByteTextInternal(string value) => SetByteText(value);
    internal string TokenValueByteString() => tokenValue;
    internal string TokenTextByteString() => text.Substring(tokenStart, pos - tokenStart);
    internal string TextByteString() => text;
    internal void SetOnErrorByteStrings(ErrorCallback value)
    {
        onError = value;
        onErrorUsesByteStrings = true;
    }

    internal static int SkipTriviaByteString(string source, int pos) => skipTriviaEx(source, pos, null);
    internal static bool IsValidIdentifierByteString(string source) => isValidIdentifier(source);
    internal static IEnumerable<CommentRange> GetLeadingCommentRangesByteString(string source, int pos) => iterateCommentRanges(source, pos, false);
    internal static IEnumerable<CommentRange> GetTrailingCommentRangesByteString(string source, int pos) => iterateCommentRanges(source, pos, true);
}
