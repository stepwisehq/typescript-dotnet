// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/exec.go:121-169 lazy zero-width assertion state.
using TypeScript.RegularExpressions.Syntax;

namespace TypeScript.RegularExpressions;
internal readonly struct LazyFlag
{
    private readonly ulong value;
    // exec.go:124. Signed runes occupy the same two 32-bit halves as the original uint64 owner.
    internal LazyFlag(int before, int after) => value = unchecked(((ulong)(long)before << 32) | (uint)after);
    // exec.go:128. Only evaluate the rune-dependent assertions actually requested.
    internal bool Match(EmptyOp op)
    {
        if (op == 0)
            return true;
        var before = unchecked((int)(uint)(value >> 32));
        if ((op & EmptyOp.BeginLine) != 0)
        {
            if (before != '\n' && before >= 0)
                return false;
            op &= ~EmptyOp.BeginLine;
        }

        if ((op & EmptyOp.BeginText) != 0)
        {
            if (before >= 0)
                return false;
            op &= ~EmptyOp.BeginText;
        }

        if (op == 0)
            return true;
        var after = unchecked((int)(uint)value);
        if ((op & EmptyOp.EndLine) != 0)
        {
            if (after != '\n' && after >= 0)
                return false;
            op &= ~EmptyOp.EndLine;
        }

        if ((op & EmptyOp.EndText) != 0)
        {
            if (after >= 0)
                return false;
            op &= ~EmptyOp.EndText;
        }

        if (op == 0)
            return true;
        if (RegexAssertions.IsWordChar(before) != RegexAssertions.IsWordChar(after))
            op &= ~EmptyOp.WordBoundary;
        else
            op &= ~EmptyOp.NoWordBoundary;
        return op == 0;
    }
}
