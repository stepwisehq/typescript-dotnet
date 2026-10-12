// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/lsp/stack_sanitizer.go pure string algorithm.
// Real accepted GoRegexp replacement workers; CLR stack projection remains separate.
using TypeScript.Scanner;
using TypeScript.RegularExpressions;

namespace TypeScript.Lsp;
internal static class StackSanitizer
{
    private static readonly GoRegexp GenericSecretKeywordRegex = GoRegexp.MustCompile(ByteStringValue.FromString(@"(?i)(key|token|signature|sig|pwd)([(\[.|])"));
    internal static ByteStringValue DefeatGenericSecretRegex(ByteStringValue value) => GenericSecretKeywordRegex.ReplaceAllString(value, ByteStringValue.FromString("${1}X_X${2}"));
    internal static ByteStringValue SanitizeStackTrace(ByteStringValue stack)
    {
        var start = stack.IndexOf(ByteStringValue.FromString("runtime/debug.Stack()"));
        if (start < 0)
            return ByteStringValue.FromString("");
        stack = stack.Slice(start);
        var result = new GoBuilder();
        var lineNumber = 0;
        var position = 0;
        // strings.Lines includes each trailing newline in its yielded header and
        // yields no extra empty item after a final newline. Preserve both roles.
        while (position < stack.Length)
        {
            var newline = stack.Slice(position).IndexOf('\n');
            var end = newline < 0 ? stack.Length : position + newline + 1;
            var line = stack.Slice(position, end);
            if (lineNumber++ > 0)
                result.WriteByte((byte)'\n');
            var index = 0;
            while (index < line.Length && line[index] is ' ' or '\t')
                index++;
            result.WriteString(line.Slice(0, index));
            line = line.Slice(index);
            var module = line.IndexOf(ByteStringValue.FromString("TypeScript/tsc/"));
            if (module >= 0)
                WriteSanitizedModuleOrPath(line.Slice(module), result);
            else
                result.WriteString("(REDACTED FRAME)");
            position = end;
        }

        return DefeatGenericSecretRegex(result.Value());
    }

    private static void WriteSanitizedModuleOrPath(ByteStringValue line, GoBuilder result)
    {
        line = TrimSpace(line);
        var plusHex = line.IndexOf(ByteStringValue.FromString(" +0x"));
        if (plusHex >= 0)
            line = line.Slice(0, plusHex);
        else
        {
            var goroutine = line.LastIndexOf(ByteStringValue.FromString(" in goroutine "));
            if (goroutine >= 0)
                line = line.Slice(0, goroutine);
        }

        var start = 0;
        var segmentIndex = 0;
        while (true)
        {
            var slash = line.Slice(start).IndexOf('/');
            var end = slash < 0 ? line.Length : start + slash;
            var segment = line.Slice(start, end);
            if (segmentIndex++ > 0)
                result.WriteString("|>");
            if (segment.Length > 0 && segment[segment.Length - 1] == ')')
            {
                var open = segment.LastIndexOf('(');
                if (open < 0)
                    result.WriteString("???");
                else
                {
                    result.WriteString(segment.Slice(0, open));
                    result.WriteString("()");
                }
            }
            else
                result.WriteString(segment);
            if (slash < 0)
                break;
            start = end + 1;
        }
    }

    // Narrow borrowed counterpart of strings.TrimSpace for this actual consumer.
    private static ByteStringValue TrimSpace(ByteStringValue value)
    {
        var start = 0;
        while (start < value.Length)
        {
            var(rune, size) = value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan[start..]) : Utf8.DecodeRuneInString(value.CharSpan[start..]);
            if (!GoUnicode.IsSpace(rune))
                break;
            start += size;
        }

        value = value.Slice(start);
        return GoStrings.TrimRightFunc(value, GoUnicode.IsSpace);
    }
}
