// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/syntax/parse.go complete parser/Compile admission closure.
// ErrorCode uses its original raw string role; limit panics use a selective exception transport.
using TypeScript.Scanner;
using System.Runtime.InteropServices;

namespace TypeScript.RegularExpressions.Syntax;
internal sealed class RegexParseError(string code, ByteStringValue expression) : Exception
{
    internal string Code = code;
    internal ByteStringValue Expr = expression;
    public override string Message => "error parsing regexp: " + Code + ": `" + Expr.Materialize() + "`";
}

// Actual limit panics are caught only by the outer Parse worker; other failures still propagate.
internal sealed class RegexLimitException(string code) : Exception
{
    internal readonly string Code = code;
}

[Flags]
internal enum RegexFlags : ushort
{
    None = 0,
    FoldCase = 1 << 0,
    Literal = 1 << 1,
    ClassNL = 1 << 2,
    DotNL = 1 << 3,
    OneLine = 1 << 4,
    NonGreedy = 1 << 5,
    PerlX = 1 << 6,
    UnicodeGroups = 1 << 7,
    WasDollar = 1 << 8,
    Simple = 1 << 9,
    MatchNL = ClassNL | DotNL,
    Perl = ClassNL | OneLine | PerlX | UnicodeGroups,
    POSIX = 0
}

internal sealed partial class RegexParser
{
    private const string ErrInternalError = "regexp/syntax: internal error";
    private const string ErrInvalidCharClass = "invalid character class";
    private const string ErrInvalidCharRange = "invalid character class range";
    private const string ErrInvalidEscape = "invalid escape sequence";
    private const string ErrInvalidNamedCapture = "invalid named capture";
    private const string ErrInvalidPerlOp = "invalid or unsupported Perl syntax";
    private const string ErrInvalidRepeatOp = "invalid nested repetition operator";
    private const string ErrInvalidRepeatSize = "invalid repeat count";
    private const string ErrInvalidUTF8 = "invalid UTF-8";
    private const string ErrMissingBracket = "missing closing ]";
    private const string ErrMissingParen = "missing closing )";
    private const string ErrMissingRepeatArgument = "missing argument to repetition operator";
    private const string ErrTrailingBackslash = "trailing backslash at end of expression";
    private const string ErrUnexpectedParen = "unexpected )";
    private const string ErrNestingDepth = "expression nests too deeply";
    private const string ErrLarge = "expression too large";
    internal const int minFold = 0x0041, maxFold = 0x1e943;
    private const int maxHeight = 1000, maxSize = (128 << 20) / (5 * 8), maxRunes = (128 << 20) / 4;
    private RegexFlags flags;
    private RegexSlice<RegexNode?> stack;
    private RegexNode? free;
    private int numCap, numRegexp, numRunes;
    private ByteStringValue wholeRegexp;
    private RegexSlice<int> tmpClass;
    private long repeats;
    private Dictionary<RegexNode, int>? height;
    private Dictionary<RegexNode, long>? size;
    // Go1.27.1 src/regexp/syntax/parse.go:141.
    private RegexNode newRegexp(RegexOp op)
    {
        var re = this.free;
        if ((re != null))
        {
            this.free = re.Sub0[0];
            re.Clear();
        }
        else
        {
            re = new RegexNode();
            this.numRegexp++;
        }

        re.Op = op;
        return re;
    }

    // Go1.27.1 src/regexp/syntax/parse.go:154.
    private void reuse(RegexNode re)
    {
        if ((this.height != null))
        {
            this.height.Remove(re);
        }

        re.Sub0[0] = this.free;
        this.free = re;
    }

    // Go1.27.1 src/regexp/syntax/parse.go:162.
    private void checkLimits(RegexNode re)
    {
        if ((this.numRunes > maxRunes))
        {
            throw new RegexLimitException(ErrLarge);
        }

        this.checkSize(re);
        this.checkHeight(re);
    }

    // Go1.27.1 src/regexp/syntax/parse.go:170.
    private void checkSize(RegexNode re)
    {
        if ((this.size == null))
        {
            if ((this.repeats == 0))
            {
                this.repeats = 1;
            }

            if ((re.Op == RegexOp.Repeat))
            {
                var n = re.Max;
                if ((n == -1))
                {
                    n = re.Min;
                }

                if ((n <= 0))
                {
                    n = 1;
                }

                if (((long)(n) > (maxSize / this.repeats)))
                {
                    this.repeats = maxSize;
                }
                else
                {
                    this.repeats *= (long)(n);
                }
            }

            if (((long)(this.numRegexp) < (maxSize / this.repeats)))
            {
                return;
            }

            this.size = new Dictionary<RegexNode, long>();
            {
                foreach (var re2 in this.stack)
                {
                    this.checkSize(re2!);
                }
            }
        }

        if ((this.calcSize(re, true) > maxSize))
        {
            throw new RegexLimitException(ErrLarge);
        }
    }

    // Go1.27.1 src/regexp/syntax/parse.go:212.
    private long calcSize(RegexNode re, bool force)
    {
        if (!force)
        {
            {
                var ok = this.size!.TryGetValue(re, out var size);
                if (ok)
                {
                    return size;
                }
            }
        }

        long size2 = default;
        switch (re.Op)
        {
            case RegexOp.Literal:
            {
                size2 = (long)(re.Rune.Length);
                break;
            }

            case RegexOp.Capture:
            case RegexOp.Star:
            {
                size2 = (2 + this.calcSize(re.Sub[0]!, false));
                break;
            }

            case RegexOp.Plus:
            case RegexOp.Quest:
            {
                size2 = (1 + this.calcSize(re.Sub[0]!, false));
                break;
            }

            case RegexOp.Concat:
            {
                {
                    foreach (var sub in re.Sub)
                    {
                        size2 += this.calcSize(sub!, false);
                    }
                }

                break;
            }

            case RegexOp.Alternate:
            {
                {
                    foreach (var sub2 in re.Sub)
                    {
                        size2 += this.calcSize(sub2!, false);
                    }
                }

                if ((re.Sub.Length > 1))
                {
                    size2 += ((long)(re.Sub.Length) - 1);
                }

                break;
            }

            case RegexOp.Repeat:
            {
                var sub3 = this.calcSize(re.Sub[0]!, false);
                if ((re.Max == -1))
                {
                    if ((re.Min == 0))
                    {
                        size2 = (2 + sub3);
                    }
                    else
                    {
                        size2 = (1 + ((long)(re.Min) * sub3));
                    }

                    break;
                }

                size2 = (((long)(re.Max) * sub3) + (long)((re.Max - re.Min)));
                break;
            }
        }

        size2 = Math.Max(1, size2);
        this.size![re] = size2;
        return size2;
    }

    // Go1.27.1 src/regexp/syntax/parse.go:258.
    private void checkHeight(RegexNode re)
    {
        if ((this.numRegexp < maxHeight))
        {
            return;
        }

        if ((this.height == null))
        {
            this.height = new Dictionary<RegexNode, int>();
            {
                foreach (var re2 in this.stack)
                {
                    this.checkHeight(re2!);
                }
            }
        }

        if ((this.calcHeight(re, true) > maxHeight))
        {
            throw new RegexLimitException(ErrNestingDepth);
        }
    }

    // Go1.27.1 src/regexp/syntax/parse.go:273.
    private int calcHeight(RegexNode re, bool force)
    {
        if (!force)
        {
            {
                var ok = this.height!.TryGetValue(re, out var h);
                if (ok)
                {
                    return h;
                }
            }
        }

        var h2 = 1;
        {
            foreach (var sub in re.Sub)
            {
                var hsub = this.calcHeight(sub!, false);
                if ((h2 < (1 + hsub)))
                {
                    h2 = (1 + hsub);
                }
            }
        }

        this.height![re] = h2;
        return h2;
    }

    // push pushes the regexp re onto the parse stack and returns the regexp.
    // Go1.27.1 src/regexp/syntax/parse.go:293.
    private RegexNode? push(RegexNode re)
    {
        this.numRunes += re.Rune.Length;
        if ((((re.Op == RegexOp.CharClass) && (re.Rune.Length == 2)) && (re.Rune[0] == re.Rune[1])))
        {
            if (this.maybeConcat(re.Rune[0], (this.flags & ~RegexFlags.FoldCase)))
            {
                return null;
            }

            re.Op = RegexOp.Literal;
            re.Rune = re.Rune.Slice2(0, 1);
            re.Flags = (this.flags & ~RegexFlags.FoldCase);
        }
        else
        {
            if (re.Op == RegexOp.CharClass && re.Rune.Length == 4 && re.Rune[0] == re.Rune[1] && re.Rune[2] == re.Rune[3] && UnicodeCase.SimpleFold(re.Rune[0]) == re.Rune[2] && UnicodeCase.SimpleFold(re.Rune[2]) == re.Rune[0] || re.Op == RegexOp.CharClass && re.Rune.Length == 2 && re.Rune[0] + 1 == re.Rune[1] && UnicodeCase.SimpleFold(re.Rune[0]) == re.Rune[1] && UnicodeCase.SimpleFold(re.Rune[1]) == re.Rune[0])
            {
                if (this.maybeConcat(re.Rune[0], (this.flags | RegexFlags.FoldCase)))
                {
                    return null;
                }

                re.Op = RegexOp.Literal;
                re.Rune = re.Rune.Slice2(0, 1);
                re.Flags = (this.flags | RegexFlags.FoldCase);
            }
            else
            {
                this.maybeConcat(-1, 0);
            }
        }

        this.stack = this.stack.Append(re);
        this.checkLimits(re);
        return re;
    }

    // maybeConcat implements incremental concatenation
    // of literal runes into ByteStringValue nodes. The parser calls this
    // before each push, so only the top fragment of the stack
    // might need processing. Since this is called before a push,
    // the topmost literal is no longer subject to operators like *
    // (Otherwise ab* would turn into (ab)*.)
    // If r >= 0 and there's a node left over, maybeConcat uses it
    // to push r with the given flags.
    // maybeConcat reports whether r was pushed.
    // Go1.27.1 src/regexp/syntax/parse.go:339.
    private bool maybeConcat(int r, RegexFlags flags)
    {
        var n = this.stack.Length;
        if ((n < 2))
        {
            return false;
        }

        var re1 = this.stack[(n - 1)]!;
        var re2 = this.stack[(n - 2)]!;
        if ((((re1.Op != RegexOp.Literal) || (re2.Op != RegexOp.Literal)) || ((re1.Flags & RegexFlags.FoldCase) != (re2.Flags & RegexFlags.FoldCase))))
        {
            return false;
        }

        re2.Rune = re2.Rune.Append(re1.Rune);
        if ((r >= 0))
        {
            re1.Rune = re1.Rune0.Slice2(0, 1);
            re1.Rune[0] = r;
            re1.Flags = flags;
            return true;
        }

        this.stack = this.stack.Slice2(0, (n - 1));
        this.reuse(re1);
        return false;
    }

    // literal pushes a literal regexp for the rune r on the stack.
    // Go1.27.1 src/regexp/syntax/parse.go:368.
    private void literal(int r)
    {
        var re = this.newRegexp(RegexOp.Literal);
        re.Flags = this.flags;
        if (((this.flags & RegexFlags.FoldCase) != 0))
        {
            r = minFoldRune(r);
        }

        re.Rune0[0] = r;
        re.Rune = re.Rune0.Slice2(0, 1);
        this.push(re);
    }

    // minFoldRune returns the minimum rune fold-equivalent to r.
    // Go1.27.1 src/regexp/syntax/parse.go:380.
    private static int minFoldRune(int r)
    {
        if (((r < minFold) || (r > maxFold)))
        {
            return r;
        }

        var m = r;
        var r0 = r;
        {
            r = UnicodeCase.SimpleFold(r);
            for (; (r != r0); r = UnicodeCase.SimpleFold(r))
            {
                m = Math.Min(m, r);
            }
        }

        return m;
    }

    // op pushes a regexp with the given op onto the stack
    // and returns that regexp.
    // Go1.27.1 src/regexp/syntax/parse.go:394.
    private RegexNode op(RegexOp op)
    {
        var re = this.newRegexp(op);
        re.Flags = this.flags;
        return this.push(re)!;
    }

    // repeat replaces the top stack element with itself repeated according to op, min, max.
    // before is the regexp suffix starting at the repetition operator.
    // after is the regexp suffix following after the repetition operator.
    // repeat returns an updated 'after' and an RegexParseError?, if any.
    // Go1.27.1 src/regexp/syntax/parse.go:404.
    private (ByteStringValue, RegexParseError? ) repeat(RegexOp op, int min, int max, ByteStringValue before, ByteStringValue after, ByteStringValue lastRepeat)
    {
        var flags = this.flags;
        if (((this.flags & RegexFlags.PerlX) != 0))
        {
            if (((after.Length > 0) && (after[0] == '?')))
            {
                after = after.Slice(1, after.Length);
                flags ^= RegexFlags.NonGreedy;
            }

            if ((lastRepeat.Length != 0))
            {
                return (emptyText, new RegexParseError(ErrInvalidRepeatOp, lastRepeat.Slice(0, (lastRepeat.Length - after.Length))));
            }
        }

        var n = this.stack.Length;
        if ((n == 0))
        {
            return (emptyText, new RegexParseError(ErrMissingRepeatArgument, before.Slice(0, (before.Length - after.Length))));
        }

        var sub = this.stack[(n - 1)]!;
        if ((sub.Op >= RegexOp.LeftParen))
        {
            return (emptyText, new RegexParseError(ErrMissingRepeatArgument, before.Slice(0, (before.Length - after.Length))));
        }

        var re = this.newRegexp(op);
        re.Min = min;
        re.Max = max;
        re.Flags = flags;
        re.Sub = re.Sub0.Slice2(0, 1);
        re.Sub[0] = sub;
        this.stack[(n - 1)] = re;
        this.checkLimits(re);
        if ((((op == RegexOp.Repeat) && (((min >= 2) || (max >= 2)))) && !repeatIsValid(re, 1000)))
        {
            return (emptyText, new RegexParseError(ErrInvalidRepeatSize, before.Slice(0, (before.Length - after.Length))));
        }

        return (after, null);
    }

    // repeatIsValid reports whether the repetition re is valid.
    // Valid means that the combination of the top-level repetition
    // and any inner repetitions does not exceed n copies of the
    // innermost thing.
    // This function rewalks the regexp tree and is called for every repetition,
    // so we have to worry about inducing quadratic behavior in the parser.
    // We avoid this by only calling repeatIsValid when min or max >= 2.
    // In that case the depth of any >= 2 nesting can only get to 9 without
    // triggering a parse RegexParseError?, so each subtree can only be rewalked 9 times.
    // Go1.27.1 src/regexp/syntax/parse.go:452.
    private static bool repeatIsValid(RegexNode re, int n)
    {
        if ((re.Op == RegexOp.Repeat))
        {
            var m = re.Max;
            if ((m == 0))
            {
                return true;
            }

            if ((m < 0))
            {
                m = re.Min;
            }

            if ((m > n))
            {
                return false;
            }

            if ((m > 0))
            {
                n /= m;
            }
        }

        {
            foreach (var sub in re.Sub)
            {
                if (!repeatIsValid(sub!, n))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // concat replaces the top of the stack (above the topmost '|' or '(') with its concatenation.
    // Go1.27.1 src/regexp/syntax/parse.go:477.
    private RegexNode concat()
    {
        this.maybeConcat(-1, 0);
        var i = this.stack.Length;
        while (((i > 0) && (this.stack[(i - 1)]!.Op < RegexOp.LeftParen)))
        {
            i--;
        }

        var subs = this.stack.Slice2(i, this.stack.Length);
        this.stack = this.stack.Slice2(0, i);
        if ((subs.Length == 0))
        {
            return this.push(this.newRegexp(RegexOp.EmptyMatch))!;
        }

        return this.push(this.collapse(subs, RegexOp.Concat))!;
    }

    // alternate replaces the top of the stack (above the topmost '(') with its alternation.
    // Go1.27.1 src/regexp/syntax/parse.go:497.
    private RegexNode alternate()
    {
        var i = this.stack.Length;
        while (((i > 0) && (this.stack[(i - 1)]!.Op < RegexOp.LeftParen)))
        {
            i--;
        }

        var subs = this.stack.Slice2(i, this.stack.Length);
        this.stack = this.stack.Slice2(0, i);
        if ((subs.Length > 0))
        {
            cleanAlt(subs[(subs.Length - 1)]!);
        }

        if ((subs.Length == 0))
        {
            return this.push(this.newRegexp(RegexOp.NoMatch))!;
        }

        return this.push(this.collapse(subs, RegexOp.Alternate))!;
    }

    // cleanAlt cleans re for eventual inclusion in an alternation.
    // Go1.27.1 src/regexp/syntax/parse.go:523.
    private static void cleanAlt(RegexNode re)
    {
        switch (re.Op)
        {
            case RegexOp.CharClass:
            {
                re.Rune = cleanClass(re.Rune);
                if ((((re.Rune.Length == 2) && (re.Rune[0] == 0)) && (re.Rune[1] == 0x10ffff)))
                {
                    re.Rune = default;
                    re.Op = RegexOp.AnyChar;
                    return;
                }

                if ((((((re.Rune.Length == 4) && (re.Rune[0] == 0)) && (re.Rune[1] == ('\n' - 1))) && (re.Rune[2] == ('\n' + 1))) && (re.Rune[3] == 0x10ffff)))
                {
                    re.Rune = default;
                    re.Op = RegexOp.AnyCharNotNL;
                    return;
                }

                if (((re.Rune.Capacity - re.Rune.Length) > 100))
                {
                    re.Rune = re.Rune0.Slice2(0, 0).Append(re.Rune);
                }

                break;
            }
        }
    }

    // collapse returns the result of applying op to sub.
    // If sub contains op nodes, they all get hoisted up
    // so that there is never a concat of a concat or an
    // alternate of an alternate.
    // Go1.27.1 src/regexp/syntax/parse.go:549.
    private RegexNode collapse(RegexSlice<RegexNode?> subs, RegexOp op)
    {
        if ((subs.Length == 1))
        {
            return subs[0]!;
        }

        var re = this.newRegexp(op);
        re.Sub = re.Sub0.Slice2(0, 0);
        {
            foreach (var sub in subs)
            {
                if ((sub!.Op == op))
                {
                    re.Sub = re.Sub.Append(sub.Sub);
                    this.reuse(sub);
                }
                else
                {
                    re.Sub = re.Sub.Append(sub);
                }
            }
        }

        if ((op == RegexOp.Alternate))
        {
            re.Sub = this.factor(re.Sub);
            if ((re.Sub.Length == 1))
            {
                var old = re;
                re = re.Sub[0]!;
                this.reuse(old);
            }
        }

        return re;
    }

    // factor factors common prefixes from the alternation list sub.
    // It returns a replacement list that reuses the same storage and
    // frees (passes to p.reuse) any removed *Regexps.
    //
    // For example,
    //
    //	ABC|ABD|AEF|BCX|BCY
    //
    // simplifies by literal prefix extraction to
    //
    //	A(B(C|D)|EF)|BC(X|Y)
    //
    // which simplifies by character class introduction to
    //
    //	A(B[CD]|EF)|BC[XY]
    // Go1.27.1 src/regexp/syntax/parse.go:589.
    private RegexSlice<RegexNode?> factor(RegexSlice<RegexNode?> sub)
    {
        if ((sub.Length < 2))
        {
            return sub;
        }

        RegexSlice<int> str = default;
        RegexFlags strflags = default;
        var start = 0;
        var @out = sub.Slice2(0, 0);
        {
            var i = 0;
            for (; (i <= sub.Length); i++)
            {
                RegexSlice<int> istr = default;
                RegexFlags iflags = default;
                if ((i < sub.Length))
                {
                    (istr, iflags) = this.leadingString(sub[i]!);
                    if ((iflags == strflags))
                    {
                        var same = 0;
                        while ((((same < str.Length) && (same < istr.Length)) && (str[same] == istr[same])))
                        {
                            same++;
                        }

                        if ((same > 0))
                        {
                            str = str.Slice2(0, same);
                            continue;
                        }
                    }
                }

                if ((i == start))
                {
                }
                else
                {
                    if ((i == (start + 1)))
                    {
                        @out = @out.Append(sub[start]!);
                    }
                    else
                    {
                        var prefix = this.newRegexp(RegexOp.Literal);
                        prefix.Flags = strflags;
                        prefix.Rune = prefix.Rune.Slice2(0, 0).Append(str);
                        {
                            var j = start;
                            for (; (j < i); j++)
                            {
                                sub[j] = this.removeLeadingString(sub[j]!, str.Length);
                                this.checkLimits(sub[j]!);
                            }
                        }

                        var suffix = this.collapse(sub.Slice2(start, i), RegexOp.Alternate);
                        var re = this.newRegexp(RegexOp.Concat);
                        re.Sub = re.Sub.Slice2(0, 0).Append(prefix, suffix);
                        @out = @out.Append(re);
                    }
                }

                start = i;
                str = istr;
                strflags = iflags;
            }
        }

        sub = @out;
        start = 0;
        @out = sub.Slice2(0, 0);
        RegexNode? first = null;
        {
            var i2 = 0;
            for (; (i2 <= sub.Length); i2++)
            {
                RegexNode? ifirst = null;
                if ((i2 < sub.Length))
                {
                    ifirst = this.leadingRegexp(sub[i2]!);
                    if ((((first != null) && first.Equal(ifirst)) && ((isCharClass(first) || ((((first.Op == RegexOp.Repeat) && (first.Min == first.Max)) && isCharClass(first.Sub[0]!)))))))
                    {
                        continue;
                    }
                }

                if ((i2 == start))
                {
                }
                else
                {
                    if ((i2 == (start + 1)))
                    {
                        @out = @out.Append(sub[start]!);
                    }
                    else
                    {
                        var prefix2 = first;
                        {
                            var j2 = start;
                            for (; (j2 < i2); j2++)
                            {
                                var reuse = (j2 != start);
                                sub[j2] = this.removeLeadingRegexp(sub[j2]!, reuse);
                                this.checkLimits(sub[j2]!);
                            }
                        }

                        var suffix2 = this.collapse(sub.Slice2(start, i2), RegexOp.Alternate);
                        var re2 = this.newRegexp(RegexOp.Concat);
                        re2.Sub = re2.Sub.Slice2(0, 0).Append(prefix2, suffix2);
                        @out = @out.Append(re2);
                    }
                }

                start = i2;
                first = ifirst;
            }
        }

        sub = @out;
        start = 0;
        @out = sub.Slice2(0, 0);
        {
            var i3 = 0;
            for (; (i3 <= sub.Length); i3++)
            {
                if (((i3 < sub.Length) && isCharClass(sub[i3]!)))
                {
                    continue;
                }

                if ((i3 == start))
                {
                }
                else
                {
                    if ((i3 == (start + 1)))
                    {
                        @out = @out.Append(sub[start]!);
                    }
                    else
                    {
                        var max = start;
                        {
                            var j3 = (start + 1);
                            for (; (j3 < i3); j3++)
                            {
                                if (((sub[max]!.Op < sub[j3]!.Op) || ((sub[max]!.Op == sub[j3]!.Op) && (sub[max]!.Rune.Length < sub[j3]!.Rune.Length))))
                                {
                                    max = j3;
                                }
                            }
                        }

                        (sub[start]!, sub[max]!) = (sub[max]!, sub[start]!);
                        {
                            var j4 = (start + 1);
                            for (; (j4 < i3); j4++)
                            {
                                mergeCharClass(sub[start]!, sub[j4]!);
                                this.reuse(sub[j4]!);
                            }
                        }

                        cleanAlt(sub[start]!);
                        @out = @out.Append(sub[start]!);
                    }
                }

                if ((i3 < sub.Length))
                {
                    @out = @out.Append(sub[i3]!);
                }

                start = (i3 + 1);
            }
        }

        sub = @out;
        start = 0;
        @out = sub.Slice2(0, 0);
        {
            for (var i4 = 0; i4 < sub.Length; i4++)
            {
                if (((((i4 + 1) < sub.Length) && (sub[i4]!.Op == RegexOp.EmptyMatch)) && (sub[(i4 + 1)]!.Op == RegexOp.EmptyMatch)))
                {
                    continue;
                }

                @out = @out.Append(sub[i4]!);
            }
        }

        sub = @out;
        return sub;
    }

    // leadingString returns the leading literal ByteStringValue that re begins with.
    // The ByteStringValue refers to storage in re or its children.
    // Go1.27.1 src/regexp/syntax/parse.go:778.
    private (RegexSlice<int>, RegexFlags) leadingString(RegexNode re)
    {
        if (((re.Op == RegexOp.Concat) && (re.Sub.Length > 0)))
        {
            re = re.Sub[0]!;
        }

        if ((re.Op != RegexOp.Literal))
        {
            return (default, 0);
        }

        return (re.Rune, (re.Flags & RegexFlags.FoldCase));
    }

    // removeLeadingString removes the first n leading runes
    // from the beginning of re. It returns the replacement for re.
    // Go1.27.1 src/regexp/syntax/parse.go:790.
    private RegexNode removeLeadingString(RegexNode re, int n)
    {
        if (((re.Op == RegexOp.Concat) && (re.Sub.Length > 0)))
        {
            var sub = re.Sub[0]!;
            sub = this.removeLeadingString(sub, n);
            re.Sub[0] = sub;
            if ((sub.Op == RegexOp.EmptyMatch))
            {
                this.reuse(sub);
                switch (re.Sub.Length)
                {
                    case 0:
                    case 1:
                    {
                        re.Op = RegexOp.EmptyMatch;
                        re.Sub = default;
                        break;
                    }

                    case 2:
                    {
                        var old = re;
                        re = re.Sub[1]!;
                        this.reuse(old);
                        break;
                    }

                    default:
                    {
                        re.Sub.Slice2(1).AsSpan().CopyTo(re.Sub.AsSpan());
                        re.Sub = re.Sub.Slice2(0, (re.Sub.Length - 1));
                        break;
                    }
                }
            }

            return re;
        }

        if ((re.Op == RegexOp.Literal))
        {
            re.Rune = re.Rune.Slice2(0, CopyPrefix(re.Rune, re.Rune.Slice2(n, re.Rune.Length)));
            if ((re.Rune.Length == 0))
            {
                re.Op = RegexOp.EmptyMatch;
            }
        }

        return re;
    }

    // leadingRegexp returns the leading regexp that re begins with.
    // The regexp refers to storage in re or its children.
    // Go1.27.1 src/regexp/syntax/parse.go:827.
    private RegexNode? leadingRegexp(RegexNode re)
    {
        if ((re.Op == RegexOp.EmptyMatch))
        {
            return null;
        }

        if (((re.Op == RegexOp.Concat) && (re.Sub.Length > 0)))
        {
            var sub = re.Sub[0]!;
            if ((sub.Op == RegexOp.EmptyMatch))
            {
                return null;
            }

            return sub;
        }

        return re;
    }

    // removeLeadingRegexp removes the leading regexp in re.
    // It returns the replacement for re.
    // If reuse is true, it passes the removed regexp (if no longer needed) to p.reuse.
    // Go1.27.1 src/regexp/syntax/parse.go:844.
    private RegexNode removeLeadingRegexp(RegexNode re, bool reuse)
    {
        if (((re.Op == RegexOp.Concat) && (re.Sub.Length > 0)))
        {
            if (reuse)
            {
                this.reuse(re.Sub[0]!);
            }

            re.Sub = re.Sub.Slice2(0, CopyPrefix(re.Sub, re.Sub.Slice2(1, re.Sub.Length)));
            switch (re.Sub.Length)
            {
                case 0:
                {
                    re.Op = RegexOp.EmptyMatch;
                    re.Sub = default;
                    break;
                }

                case 1:
                {
                    var old = re;
                    re = re.Sub[0]!;
                    this.reuse(old);
                    break;
                }
            }

            return re;
        }

        if (reuse)
        {
            this.reuse(re);
        }

        return this.newRegexp(RegexOp.EmptyMatch);
    }

    // Go1.27.1 src/regexp/syntax/parse.go:867.
    private static RegexNode literalRegexp(ByteStringValue s, RegexFlags flags)
    {
        var re = new RegexNode
        {
            Op = RegexOp.Literal
        };
        re.Flags = flags;
        re.Rune = re.Rune0.Slice2(0, 0);
        {
            var remainder = s;
            while (remainder.Length != 0)
            {
                var(rune, width) = DecodeRune(remainder);
                if (re.Rune.Length >= re.Rune.Capacity)
                {
                    re.Rune = makeRunes(s);
                    break;
                }

                re.Rune = re.Rune.Append(rune);
                remainder = remainder.Slice(width);
            }
        }

        return re;
    }

    // Parse parses a regular expression ByteStringValue s, controlled by the specified
    // RegexFlags, and returns a regular expression parse tree. The syntax is
    // described in the top-level comment.
    // Go1.27.1 src/regexp/syntax/parse.go:887.
    public static (RegexNode? , RegexParseError? ) Parse(ByteStringValue s, RegexFlags flags)
    {
        try
        {
            return parse(s, flags);
        }
        catch (RegexLimitException limit)when (limit.Code is ErrLarge or ErrNestingDepth)
        {
            return (null, new RegexParseError(limit.Code, s));
        }
    }

    // Go1.27.1 src/regexp/syntax/parse.go:891.
    private static (RegexNode? , RegexParseError? ) parse(ByteStringValue s, RegexFlags flags)
    {
        RegexParseError? err = default;
        if (((flags & RegexFlags.Literal) != 0))
        {
            {
                var err2 = CheckUTF8(s);
                if ((err2 != null))
                {
                    return (null, err2);
                }
            }

            return (literalRegexp(s, flags), null);
        }

        var p = new RegexParser();
        int c = default;
        RegexOp op = default;
        ByteStringValue lastRepeat = emptyText;
        p.flags = flags;
        p.wholeRegexp = s;
        var t = s;
        while ((t.Length != 0))
        {
            ByteStringValue repeat = ByteStringValue.FromString("");
            switch (t[0])
            {
                default:
                {
                    {
                        (c, t, err) = NextRune(t);
                        if ((err != null))
                        {
                            return (null, err);
                        }
                    }

                    p.literal(c);
                    break;
                }

                case '(':
                {
                    if (((((p.flags & RegexFlags.PerlX) != 0) && (t.Length >= 2)) && (t[1] == '?')))
                    {
                        {
                            (t, err) = p.parsePerlFlags(t);
                            if ((err != null))
                            {
                                return (null, err);
                            }
                        }

                        break;
                    }

                    p.numCap++;
                    p.op(RegexOp.LeftParen).Cap = p.numCap;
                    t = t.Slice(1, t.Length);
                    break;
                }

                case '|':
                {
                    p.parseVerticalBar();
                    t = t.Slice(1, t.Length);
                    break;
                }

                case ')':
                {
                    {
                        err = p.parseRightParen();
                        if ((err != null))
                        {
                            return (null, err);
                        }
                    }

                    t = t.Slice(1, t.Length);
                    break;
                }

                case '^':
                {
                    if (((p.flags & RegexFlags.OneLine) != 0))
                    {
                        p.op(RegexOp.BeginText);
                    }
                    else
                    {
                        p.op(RegexOp.BeginLine);
                    }

                    t = t.Slice(1, t.Length);
                    break;
                }

                case '$':
                {
                    if (((p.flags & RegexFlags.OneLine) != 0))
                    {
                        p.op(RegexOp.EndText).Flags |= RegexFlags.WasDollar;
                    }
                    else
                    {
                        p.op(RegexOp.EndLine);
                    }

                    t = t.Slice(1, t.Length);
                    break;
                }

                case '.':
                {
                    if (((p.flags & RegexFlags.DotNL) != 0))
                    {
                        p.op(RegexOp.AnyChar);
                    }
                    else
                    {
                        p.op(RegexOp.AnyCharNotNL);
                    }

                    t = t.Slice(1, t.Length);
                    break;
                }

                case '[':
                {
                    {
                        (t, err) = p.parseClass(t);
                        if ((err != null))
                        {
                            return (null, err);
                        }
                    }

                    break;
                }

                case '*':
                case '+':
                case '?':
                {
                    var before = t;
                    switch (t[0])
                    {
                        case '*':
                        {
                            op = RegexOp.Star;
                            break;
                        }

                        case '+':
                        {
                            op = RegexOp.Plus;
                            break;
                        }

                        case '?':
                        {
                            op = RegexOp.Quest;
                            break;
                        }
                    }

                    var after = t.Slice(1, t.Length);
                    {
                        (after, err) = p.repeat(op, 0, 0, before, after, lastRepeat);
                        if ((err != null))
                        {
                            return (null, err);
                        }
                    }

                    repeat = before;
                    t = after;
                    break;
                }

                case '{':
                {
                    op = RegexOp.Repeat;
                    var before2 = t;
                    var tuple10 = p.parseRepeat(t);
                    var min = tuple10.Item1;
                    var max = tuple10.Item2;
                    var after2 = tuple10.Item3;
                    var ok = tuple10.Item4;
                    if (!ok)
                    {
                        p.literal('{');
                        t = t.Slice(1, t.Length);
                        break;
                    }

                    if (((((min < 0) || (min > 1000)) || (max > 1000)) || ((max >= 0) && (min > max))))
                    {
                        return (null, new RegexParseError(ErrInvalidRepeatSize, before2.Slice(0, (before2.Length - after2.Length))));
                    }

                    {
                        (after2, err) = p.repeat(op, min, max, before2, after2, lastRepeat);
                        if ((err != null))
                        {
                            return (null, err);
                        }
                    }

                    repeat = before2;
                    t = after2;
                    break;
                }

                case '\\':
                {
                    if ((((p.flags & RegexFlags.PerlX) != 0) && (t.Length >= 2)))
                    {
                        switch (t[1])
                        {
                            case 'A':
                            {
                                p.op(RegexOp.BeginText);
                                t = t.Slice(2, t.Length);
                                goto BigSwitchEnd;
                            }

                            case 'b':
                            {
                                p.op(RegexOp.WordBoundary);
                                t = t.Slice(2, t.Length);
                                goto BigSwitchEnd;
                            }

                            case 'B':
                            {
                                p.op(RegexOp.NoWordBoundary);
                                t = t.Slice(2, t.Length);
                                goto BigSwitchEnd;
                            }

                            case 'C':
                            {
                                return (null, new RegexParseError(ErrInvalidEscape, t.Slice(0, 2)));
                            }

                            case 'Q':
                            {
                                ByteStringValue lit = emptyText;
                                (lit, t, _) = CutInput(t.Slice(2, t.Length), ByteStringValue.FromString("\\E"));
                                while ((lit.Length != 0))
                                {
                                    var tuple11 = NextRune(lit);
                                    var c2 = tuple11.Item1;
                                    var rest = tuple11.Item2;
                                    var err3 = tuple11.Item3;
                                    if ((err3 != null))
                                    {
                                        return (null, err3);
                                    }

                                    p.literal(c2);
                                    lit = rest;
                                }

                                goto BigSwitchEnd;
                            }

                            case 'z':
                            {
                                p.op(RegexOp.EndText);
                                t = t.Slice(2, t.Length);
                                goto BigSwitchEnd;
                            }
                        }
                    }

                    var re = p.newRegexp(RegexOp.CharClass);
                    re.Flags = p.flags;
                    if (((t.Length >= 2) && (((t[1] == 'p') || (t[1] == 'P')))))
                    {
                        var tuple12 = p.parseUnicodeClass(t, re.Rune0.Slice2(0, 0));
                        var r = tuple12.Item1;
                        var rest2 = tuple12.Item2;
                        var err4 = tuple12.Item3;
                        if ((err4 != null))
                        {
                            return (null, err4);
                        }

                        if ((!r.IsNil))
                        {
                            re.Rune = r;
                            t = rest2;
                            p.push(re);
                            goto BigSwitchEnd;
                        }
                    }

                    {
                        var tuple13 = p.parsePerlClassEscape(t, re.Rune0.Slice2(0, 0));
                        var r2 = tuple13.Item1;
                        var rest3 = tuple13.Item2;
                        if ((!r2.IsNil))
                        {
                            re.Rune = r2;
                            t = rest3;
                            p.push(re);
                            goto BigSwitchEnd;
                        }
                    }

                    p.reuse(re);
                    {
                        (c, t, err) = p.ParseEscape(t);
                        if ((err != null))
                        {
                            return (null, err);
                        }
                    }

                    p.literal(c);
                    break;
                }
            }

            BigSwitchEnd:
                ;
            lastRepeat = repeat;
        }

        p.concat();
        if (p.swapVerticalBar())
        {
            p.stack = p.stack.Slice2(0, (p.stack.Length - 1));
        }

        p.alternate();
        var n = p.stack.Length;
        if ((n != 1))
        {
            return (null, new RegexParseError(ErrMissingParen, s));
        }

        return (p.stack[0]!, null);
    }

    // parseRepeat parses {min} (max=min) or {min,} (max=-1) or {min,max}.
    // If s is not of that form, it returns ok == false.
    // If s has the right form but the values are too big, it returns min == -1, ok == true.
    // Go1.27.1 src/regexp/syntax/parse.go:1102.
    private (int Min, int Max, ByteStringValue Remainder, bool Ok) parseRepeat(ByteStringValue s)
    {
        int min = default;
        int max = default;
        ByteStringValue rest = emptyText;
        bool ok = default;
        if (((s.Length == 0) || (s[0] != '{')))
        {
            return (min, max, rest, ok);
        }

        s = s.Slice(1, s.Length);
        bool ok1 = default;
        {
            (min, s, ok1) = ParseInt(s);
            if (!ok1)
            {
                return (min, max, rest, ok);
            }
        }

        if ((s.Length == 0))
        {
            return (min, max, rest, ok);
        }

        if ((s[0] != ','))
        {
            max = min;
        }
        else
        {
            s = s.Slice(1, s.Length);
            if ((s.Length == 0))
            {
                return (min, max, rest, ok);
            }

            if ((s[0] == '}'))
            {
                max = -1;
            }
            else
            {
                {
                    (max, s, ok1) = ParseInt(s);
                    if (!ok1)
                    {
                        return (min, max, rest, ok);
                    }
                    else
                    {
                        if ((max < 0))
                        {
                            min = -1;
                        }
                    }
                }
            }
        }

        if (((s.Length == 0) || (s[0] != '}')))
        {
            return (min, max, rest, ok);
        }

        rest = s.Slice(1, s.Length);
        ok = true;
        return (min, max, rest, ok);
    }

    // parsePerlFlags parses a RegexFlags.Perl flag setting or non-capturing group or both,
    // like (?i) or (?: or (?i:.  It removes the prefix from s and updates the parse state.
    // The caller must have ensured that s begins with "(?".
    // Go1.27.1 src/regexp/syntax/parse.go:1141.
    private (ByteStringValue, RegexParseError? ) parsePerlFlags(ByteStringValue s)
    {
        ByteStringValue rest = emptyText;
        RegexParseError? err = default;
        var t = s;
        var startsWithP = (((t.Length > 4) && (t[2] == 'P')) && (t[3] == '<'));
        var startsWithName = ((t.Length > 3) && (t[2] == '<'));
        if ((startsWithP || startsWithName))
        {
            var exprStartPos = 4;
            if (startsWithName)
            {
                exprStartPos = 3;
            }

            var end = t.IndexOf('>');
            if ((end < 0))
            {
                {
                    err = CheckUTF8(t);
                    if ((err != null))
                    {
                        return (emptyText, err);
                    }
                }

                return (emptyText, new RegexParseError(ErrInvalidNamedCapture, s));
            }

            var capture = t.Slice(0, (end + 1));
            var name = t.Slice(exprStartPos, end);
            {
                err = CheckUTF8(name);
                if ((err != null))
                {
                    return (emptyText, err);
                }
            }

            if (!isValidCaptureName(name))
            {
                return (emptyText, new RegexParseError(ErrInvalidNamedCapture, capture));
            }

            this.numCap++;
            var re = this.op(RegexOp.LeftParen);
            re.Cap = this.numCap;
            re.Name = name;
            return (t.Slice((end + 1), t.Length), null);
        }

        int c = default;
        t = t.Slice(2, t.Length);
        var flags = this.flags;
        var sign = +1;
        var sawFlag = false;
        while ((t.Length != 0))
        {
            {
                (c, t, err) = NextRune(t);
                if ((err != null))
                {
                    return (emptyText, err);
                }
            }

            switch (c)
            {
                default:
                {
                    goto LoopEnd;
                }

                case 'i':
                {
                    flags |= RegexFlags.FoldCase;
                    sawFlag = true;
                    break;
                }

                case 'm':
                {
                    flags &= ~(RegexFlags.OneLine);
                    sawFlag = true;
                    break;
                }

                case 's':
                {
                    flags |= RegexFlags.DotNL;
                    sawFlag = true;
                    break;
                }

                case 'U':
                {
                    flags |= RegexFlags.NonGreedy;
                    sawFlag = true;
                    break;
                }

                case '-':
                {
                    if ((sign < 0))
                    {
                        goto LoopEnd;
                    }

                    sign = -1;
                    flags = ~flags;
                    sawFlag = false;
                    break;
                }

                case ':':
                case ')':
                {
                    if ((sign < 0))
                    {
                        if (!sawFlag)
                        {
                            goto LoopEnd;
                        }

                        flags = ~flags;
                    }

                    if ((c == ':'))
                    {
                        this.op(RegexOp.LeftParen);
                    }

                    this.flags = flags;
                    return (t, null);
                }
            }
        }

        LoopEnd:
            ;
        return (emptyText, new RegexParseError(ErrInvalidPerlOp, s.Slice(0, (s.Length - t.Length))));
    }

    // isValidCaptureName reports whether name
    // is a valid capture name: [A-Za-z0-9_]+.
    // PCRE limits names to 32 bytes.
    // Python rejects names starting with digits.
    // We don't enforce either of those.
    // Go1.27.1 src/regexp/syntax/parse.go:1260.
    private static bool isValidCaptureName(ByteStringValue name)
    {
        if ((name.Length == 0))
        {
            return false;
        }

        {
            for (var index = 0; index < name.Length; index++)
            {
                var c = name[index];
                if (((c != '_') && !IsAlnum(c)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // parseInt parses a decimal integer.
    // Go1.27.1 src/regexp/syntax/parse.go:1273.
    private static bool isCharClass(RegexNode re)
    {
        return (((((re.Op == RegexOp.Literal) && (re.Rune.Length == 1)) || (re.Op == RegexOp.CharClass)) || (re.Op == RegexOp.AnyCharNotNL)) || (re.Op == RegexOp.AnyChar));
    }

    // does re match r?
    // Go1.27.1 src/regexp/syntax/parse.go:1310.
    private static bool matchRune(RegexNode re, int r)
    {
        switch (re.Op)
        {
            case RegexOp.Literal:
            {
                return ((re.Rune.Length == 1) && (re.Rune[0] == r));
            }

            case RegexOp.CharClass:
            {
                {
                    var i = 0;
                    for (; (i < re.Rune.Length); i += 2)
                    {
                        if (((re.Rune[i] <= r) && (r <= re.Rune[(i + 1)])))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            case RegexOp.AnyCharNotNL:
            {
                return (r != '\n');
            }

            case RegexOp.AnyChar:
            {
                return true;
            }
        }

        return false;
    }

    // parseVerticalBar handles a | in the input.
    // Go1.27.1 src/regexp/syntax/parse.go:1330.
    private void parseVerticalBar()
    {
        this.concat();
        if (!this.swapVerticalBar())
        {
            this.op(RegexOp.VerticalBar);
        }
    }

    // mergeCharClass makes dst = dst|src.
    // The caller must ensure that dst.Op >= src.Op,
    // to reduce the amount of copying.
    // Go1.27.1 src/regexp/syntax/parse.go:1345.
    private static void mergeCharClass(RegexNode dst, RegexNode src)
    {
        switch (dst.Op)
        {
            case RegexOp.AnyChar:
            {
                break;
            }

            case RegexOp.AnyCharNotNL:
            {
                if (matchRune(src, '\n'))
                {
                    dst.Op = RegexOp.AnyChar;
                }

                break;
            }

            case RegexOp.CharClass:
            {
                if ((src.Op == RegexOp.Literal))
                {
                    dst.Rune = appendLiteral(dst.Rune, src.Rune[0], src.Flags);
                }
                else
                {
                    dst.Rune = appendClass(dst.Rune, src.Rune);
                }

                break;
            }

            case RegexOp.Literal:
            {
                if (((src.Rune[0] == dst.Rune[0]) && (src.Flags == dst.Flags)))
                {
                    break;
                }

                dst.Op = RegexOp.CharClass;
                dst.Rune = appendLiteral(dst.Rune.Slice2(0, 0), dst.Rune[0], dst.Flags);
                dst.Rune = appendLiteral(dst.Rune, src.Rune[0], src.Flags);
                break;
            }
        }
    }

    // If the top of the stack is an element followed by an RegexOp.VerticalBar
    // swapVerticalBar swaps the two and returns true.
    // Otherwise it returns false.
    // Go1.27.1 src/regexp/syntax/parse.go:1375.
    private bool swapVerticalBar()
    {
        var n = this.stack.Length;
        if (((((n >= 3) && (this.stack[(n - 2)]!.Op == RegexOp.VerticalBar)) && isCharClass(this.stack[(n - 1)]!)) && isCharClass(this.stack[(n - 3)]!)))
        {
            var re1 = this.stack[(n - 1)]!;
            var re3 = this.stack[(n - 3)]!;
            if ((re1.Op > re3.Op))
            {
                (re1, re3) = (re3, re1);
                this.stack[(n - 3)] = re3;
            }

            mergeCharClass(re3, re1);
            this.reuse(re1);
            this.stack = this.stack.Slice2(0, (n - 1));
            return true;
        }

        if ((n >= 2))
        {
            var re12 = this.stack[(n - 1)]!;
            var re2 = this.stack[(n - 2)]!;
            if ((re2.Op == RegexOp.VerticalBar))
            {
                if ((n >= 3))
                {
                    cleanAlt(this.stack[(n - 3)]!);
                }

                this.stack[(n - 2)] = re12;
                this.stack[(n - 1)] = re2;
                return true;
            }
        }

        return false;
    }

    // parseRightParen handles a ) in the input.
    // Go1.27.1 src/regexp/syntax/parse.go:1411.
    private RegexParseError? parseRightParen()
    {
        this.concat();
        if (this.swapVerticalBar())
        {
            this.stack = this.stack.Slice2(0, (this.stack.Length - 1));
        }

        this.alternate();
        var n = this.stack.Length;
        if ((n < 2))
        {
            return new RegexParseError(ErrUnexpectedParen, this.wholeRegexp);
        }

        var re1 = this.stack[(n - 1)]!;
        var re2 = this.stack[(n - 2)]!;
        this.stack = this.stack.Slice2(0, (n - 2));
        if ((re2.Op != RegexOp.LeftParen))
        {
            return new RegexParseError(ErrUnexpectedParen, this.wholeRegexp);
        }

        this.flags = re2.Flags;
        if ((re2.Cap == 0))
        {
            this.push(re1);
        }
        else
        {
            re2.Op = RegexOp.Capture;
            re2.Sub = re2.Sub0.Slice2(0, 1);
            re2.Sub[0] = re1;
            this.push(re2);
        }

        return null;
    }

    // parseEscape parses an escape sequence at the beginning of s
    // and returns the rune.
    // Go1.27.1 src/regexp/syntax/parse.go:1445.
    // parse.go:1445.
    private (int Rune, ByteStringValue Remainder, RegexParseError? Error) ParseEscape(ByteStringValue source)
    {
        var rest = source.Slice(1);
        if (rest.Length == 0)
            return (0, emptyText, new(ErrTrailingBackslash, emptyText));
        var(character, next, error) = NextRune(rest);
        rest = next;
        if (error != null)
            return (0, emptyText, error);
        switch (character)
        {
            default:
                // Escaped ASCII punctuation is itself. Nonzero single octal digits are unsupported backreferences.
                if (character < Utf8.RuneSelf && !IsAlnum(character))
                    return (character, rest, null);
                break;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
                if (rest.Length == 0 || rest[0] < '0' || rest[0] > '7')
                    break;
                goto case '0';
            case '0':
                var octal = character - '0';
                for (var index = 1; index < 3; index++)
                {
                    if (rest.Length == 0 || rest[0] < '0' || rest[0] > '7')
                        break;
                    octal = octal * 8 + rest[0] - '0';
                    rest = rest.Slice(1);
                }

                return (octal, rest, null);
            case 'x':
                if (rest.Length == 0)
                    break;
                (character, rest, error) = NextRune(rest);
                if (error != null)
                    return (0, emptyText, error);
                if (character == '{')
                {
                    var digitCount = 0;
                    var hexadecimal = 0;
                    while (true)
                    {
                        if (rest.Length == 0)
                            goto InvalidEscape;
                        (character, rest, error) = NextRune(rest);
                        if (error != null)
                            return (0, emptyText, error);
                        if (character == '}')
                            break;
                        var value = Unhex(character);
                        if (value < 0)
                            goto InvalidEscape;
                        hexadecimal = hexadecimal * 16 + value;
                        if (hexadecimal > 0x10FFFF)
                            goto InvalidEscape;
                        digitCount++;
                    }

                    if (digitCount == 0)
                        goto InvalidEscape;
                    return (hexadecimal, rest, null);
                }

                var first = Unhex(character);
                (character, rest, error) = NextRune(rest);
                if (error != null)
                    return (0, emptyText, error);
                var second = Unhex(character);
                if (first < 0 || second < 0)
                    break;
                return (first * 16 + second, rest, null);
            // No backspace \b here: the original distinguishes assertion and class escape syntax.
            case 'a':
                return ('\a', rest, error);
            case 'f':
                return ('\f', rest, error);
            case 'n':
                return ('\n', rest, error);
            case 'r':
                return ('\r', rest, error);
            case 't':
                return ('\t', rest, error);
            case 'v':
                return ('\v', rest, error);
        }

        InvalidEscape:
            return (0, emptyText, new(ErrInvalidEscape, source.Slice(0, source.Length - rest.Length)));
    }

    // parse.go:1550. Preserve the original whole-class error owner when the closing bracket is absent.
    private (int Rune, ByteStringValue Remainder, RegexParseError? Error) ParseClassChar(ByteStringValue text, ByteStringValue wholeClass)
    {
        if (text.Length == 0)
            return (0, emptyText, new(ErrMissingBracket, wholeClass));
        return text[0] == '\\' ? ParseEscape(text) : NextRune(text);
    }

    private (RegexSlice<int>, ByteStringValue) parsePerlClassEscape(ByteStringValue s, RegexSlice<int> r)
    {
        RegexSlice<int> @out = default;
        ByteStringValue rest = emptyText;
        if (((((this.flags & RegexFlags.PerlX) == 0) || (s.Length < 2)) || (s[0] != '\\')))
        {
            return (@out, rest);
        }

        var g = RegexCharGroups.perlGroup.GetValueOrDefault(ByteStringKey.FromValue(s.Slice(0, 2)));
        if ((g.Sign == 0))
        {
            return (@out, rest);
        }

        return (this.appendGroup(r, g), s.Slice(2, s.Length));
    }

    // parseNamedClass parses a leading RegexFlags.POSIX named character class like [:alnum:]
    // from the beginning of s. If one is present, it appends the characters to r
    // and returns the new slice r and the remainder of the ByteStringValue.
    // Go1.27.1 src/regexp/syntax/parse.go:1599.
    private (RegexSlice<int>, ByteStringValue, RegexParseError? ) parseNamedClass(ByteStringValue s, RegexSlice<int> r)
    {
        RegexSlice<int> @out = default;
        ByteStringValue rest = emptyText;
        RegexParseError? err = default;
        if ((((s.Length < 2) || (s[0] != '[')) || (s[1] != ':')))
        {
            return (@out, rest, err);
        }

        var i = s.Slice(2).IndexOf(ByteStringValue.FromString(":]"));
        if ((i < 0))
        {
            return (@out, rest, err);
        }

        i += 2;
        var tuple16 = (s.Slice(0, (i + 2)), s.Slice((i + 2), s.Length));
        var name = tuple16.Item1;
        s = tuple16.Item2;
        var g = RegexCharGroups.posixGroup.GetValueOrDefault(ByteStringKey.FromValue(name));
        if ((g.Sign == 0))
        {
            return (default, emptyText, new RegexParseError(ErrInvalidCharRange, name));
        }

        return (this.appendGroup(r, g), s, null);
    }

    // Go1.27.1 src/regexp/syntax/parse.go:1617.
    private RegexSlice<int> appendGroup(RegexSlice<int> r, CharGroup g)
    {
        if (((this.flags & RegexFlags.FoldCase) == 0))
        {
            if ((g.Sign < 0))
            {
                r = appendNegatedClass(r, g.Class);
            }
            else
            {
                r = appendClass(r, g.Class);
            }
        }
        else
        {
            var tmp = this.tmpClass.Slice2(0, 0);
            tmp = appendFoldedClass(tmp, g.Class);
            this.tmpClass = tmp;
            tmp = cleanClass(this.tmpClass);
            if ((g.Sign < 0))
            {
                r = appendNegatedClass(r, tmp);
            }
            else
            {
                r = appendClass(r, tmp);
            }
        }

        return r;
    }

    // initAliases initializes categoryAliases by canonicalizing unicode.CategoryAliases.
    // Go1.27.1 src/regexp/syntax/parse.go:1664.
    // parse.go:1640. The original sync.Once retains these two required alias map owners.
    private static readonly Lazy<(Dictionary<ByteStringKey, ByteStringValue> Categories, Dictionary<ByteStringKey, ByteStringValue> Scripts)> aliases = new(InitAliases);
    private static readonly GoUnicode.RangeTable anyTable = new([new(0, (1 << 16) - 1, 1)], [new(1 << 16, 0x10FFFF, 1)], 0);
    private static readonly GoUnicode.RangeTable asciiTable = new([new(0, 0x7F, 1)], [], 0);
    private static readonly GoUnicode.RangeTable asciiFoldTable = new([new(0, 0x7F, 1), new(0x017F, 0x017F, 1), new(0x212A, 0x212A, 1)], [], 0);
    // parse.go:1654.
    private static (Dictionary<ByteStringKey, ByteStringValue>, Dictionary<ByteStringKey, ByteStringValue>) InitAliases()
    {
        var categories = new Dictionary<ByteStringKey, ByteStringValue>();
        var scripts = new Dictionary<ByteStringKey, ByteStringValue>();
        foreach (var entry in RegexUnicodeTables.CategoryAliases)
            categories[ByteStringKey.FromValue(CanonicalName(entry.Key.Value))] = entry.Value;
        foreach (var entry in RegexUnicodeTables.Scripts)
            scripts[ByteStringKey.FromValue(CanonicalName(entry.Key.Value))] = entry.Key.Value;
        return (categories, scripts);
    }

    // parse.go:1669. Unchanged names retain their original string header.
    private static ByteStringValue CanonicalName(ByteStringValue name)
    {
        Slice<byte> bytes = default;
        var first = true;
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (character is '_' or '-' or ' ')
                character = ' ';
            else if (first)
            {
                if (character is >= 'a' and <= 'z')
                    character -= 'a' - 'A';
                first = false;
            }
            else if (character is >= 'A' and <= 'Z')
                character += 'a' - 'A';
            if (bytes.IsNil)
            {
                if (character == name[index] && character != ' ')
                    continue;
                bytes = Slice<byte>.Make(index, name.Length);
                for (var prefix = 0; prefix < index; prefix++)
                    bytes[prefix] = (byte)name[prefix];
            }

            if (character == ' ')
                continue;
            bytes = bytes.Append((byte)character);
        }

        if (bytes.IsNil)
            return name;
        // Required Go string(b) copy: the immutable result does not alias the mutable slice.
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes.AsSpan().ToArray()));
    }

    // parse.go:1720. Missing map entries remain nil; Assigned inverts the actual Cn owner.
    private static (GoUnicode.RangeTable? Table, GoUnicode.RangeTable? Fold, int Sign) UnicodeTable(ByteStringValue name)
    {
        name = CanonicalName(name);
        var key = ByteStringKey.FromValue(name);
        if (name.EqualsValue(ByteStringValue.FromString("Any")))
            return (anyTable, anyTable, 1);
        if (name.EqualsValue(ByteStringValue.FromString("Assigned")))
            return (RegexUnicodeTables._Cn, RegexUnicodeTables._Cn, -1);
        if (name.EqualsValue(ByteStringValue.FromString("Ascii")))
            return (asciiTable, asciiFoldTable, 1);
        if (name.EqualsValue(ByteStringValue.FromString("Lc")))
        {
            var lc = ByteStringKey.FromString("LC");
            return (RegexUnicodeTables.Categories[lc], RegexUnicodeTables.FoldCategory.GetValueOrDefault(lc), 1);
        }

        if (RegexUnicodeTables.Categories.TryGetValue(key, out var category))
            return (category, RegexUnicodeTables.FoldCategory.GetValueOrDefault(key), 1);
        if (RegexUnicodeTables.Scripts.TryGetValue(key, out var script))
            return (script, RegexUnicodeTables.FoldScript.GetValueOrDefault(key), 1);
        var initialized = aliases.Value;
        if (initialized.Categories.TryGetValue(key, out var actualCategory) && actualCategory.Length != 0)
        {
            var actual = ByteStringKey.FromValue(actualCategory);
            return (RegexUnicodeTables.Categories.GetValueOrDefault(actual), RegexUnicodeTables.FoldCategory.GetValueOrDefault(actual), 1);
        }

        if (initialized.Scripts.TryGetValue(key, out var actualScript) && actualScript.Length != 0)
        {
            var actual = ByteStringKey.FromValue(actualScript);
            return (RegexUnicodeTables.Scripts.GetValueOrDefault(actual), RegexUnicodeTables.FoldScript.GetValueOrDefault(actual), 1);
        }

        return (null, null, 0);
    }

    private (RegexSlice<int>, ByteStringValue, RegexParseError? ) parseUnicodeClass(ByteStringValue s, RegexSlice<int> r)
    {
        RegexSlice<int> @out = default;
        ByteStringValue rest = emptyText;
        RegexParseError? err = default;
        if ((((((this.flags & RegexFlags.UnicodeGroups) == 0) || (s.Length < 2)) || (s[0] != '\\')) || ((s[1] != 'p') && (s[1] != 'P'))))
        {
            return (@out, rest, err);
        }

        var sign = +1;
        if ((s[1] == 'P'))
        {
            sign = -1;
        }

        var t = s.Slice(2, s.Length);
        var tuple20 = NextRune(t);
        var c = tuple20.Item1;
        t = tuple20.Item2;
        err = tuple20.Item3;
        if ((err != null))
        {
            return (@out, rest, err);
        }

        ByteStringValue seq = emptyText;
        ByteStringValue name = emptyText;
        if ((c != '{'))
        {
            seq = s.Slice(0, (s.Length - t.Length));
            name = seq.Slice(2, seq.Length);
        }
        else
        {
            var end = s.IndexOf('}');
            if ((end < 0))
            {
                {
                    err = CheckUTF8(s);
                    if ((err != null))
                    {
                        return (@out, rest, err);
                    }
                }

                return (default, emptyText, new RegexParseError(ErrInvalidCharRange, s));
            }

            (seq, t) = (s.Slice(0, (end + 1)), s.Slice((end + 1), s.Length));
            name = s.Slice(3, end);
            {
                err = CheckUTF8(name);
                if ((err != null))
                {
                    return (@out, rest, err);
                }
            }
        }

        if (((name.Length != 0) && (name[0] == '^')))
        {
            sign = -sign;
            name = name.Slice(1, name.Length);
        }

        var tuple21 = UnicodeTable(name);
        var tab = tuple21.Item1;
        var fold = tuple21.Item2;
        var tsign = tuple21.Item3;
        if ((tab == null))
        {
            return (default, emptyText, new RegexParseError(ErrInvalidCharRange, seq));
        }

        if ((tsign < 0))
        {
            sign = -sign;
        }

        if ((((this.flags & RegexFlags.FoldCase) == 0) || (fold == null)))
        {
            if ((sign > 0))
            {
                r = appendTable(r, tab);
            }
            else
            {
                r = appendNegatedTable(r, tab);
            }
        }
        else
        {
            var tmp = this.tmpClass.Slice2(0, 0);
            tmp = appendTable(tmp, tab);
            tmp = appendTable(tmp, fold);
            this.tmpClass = tmp;
            tmp = cleanClass(this.tmpClass);
            if ((sign > 0))
            {
                r = appendClass(r, tmp);
            }
            else
            {
                r = appendNegatedClass(r, tmp);
            }
        }

        return (r, t, null);
    }

    // parseClass parses a character class at the beginning of s
    // and pushes it onto the parse stack.
    // Go1.27.1 src/regexp/syntax/parse.go:1836.
    private (ByteStringValue, RegexParseError? ) parseClass(ByteStringValue s)
    {
        ByteStringValue rest = emptyText;
        var t = s.Slice(1, s.Length);
        var re = this.newRegexp(RegexOp.CharClass);
        re.Flags = this.flags;
        re.Rune = re.Rune0.Slice2(0, 0);
        var sign = +1;
        if (((t.Length != 0) && (t[0] == '^')))
        {
            sign = -1;
            t = t.Slice(1, t.Length);
            if (((this.flags & RegexFlags.ClassNL) == 0))
            {
                re.Rune = re.Rune.Append('\n', '\n');
            }
        }

        var @class = re.Rune;
        var first = true;
        while ((((t.Length == 0) || (t[0] != ']')) || first))
        {
            if ((((((t.Length != 0) && (t[0] == '-')) && ((this.flags & RegexFlags.PerlX) == 0)) && !first) && (((t.Length == 1) || (t[1] != ']')))))
            {
                var tuple22 = DecodeRune(t.Slice(1, t.Length));
                var size = tuple22.Item2;
                return (emptyText, new RegexParseError(ErrInvalidCharRange, t.Slice(0, (1 + size))));
            }

            first = false;
            if ((((t.Length > 2) && (t[0] == '[')) && (t[1] == ':')))
            {
                var tuple23 = this.parseNamedClass(t, @class);
                var nclass = tuple23.Item1;
                var nt = tuple23.Item2;
                var err2 = tuple23.Item3;
                if ((err2 != null))
                {
                    return (emptyText, err2);
                }

                if ((!nclass.IsNil))
                {
                    (@class, t) = (nclass, nt);
                    continue;
                }
            }

            var tuple24 = this.parseUnicodeClass(t, @class);
            var nclass2 = tuple24.Item1;
            var nt2 = tuple24.Item2;
            var err3 = tuple24.Item3;
            if ((err3 != null))
            {
                return (emptyText, err3);
            }

            if ((!nclass2.IsNil))
            {
                (@class, t) = (nclass2, nt2);
                continue;
            }

            {
                var tuple25 = this.parsePerlClassEscape(t, @class);
                var nclass3 = tuple25.Item1;
                var nt3 = tuple25.Item2;
                if ((!nclass3.IsNil))
                {
                    (@class, t) = (nclass3, nt3);
                    continue;
                }
            }

            var rng = t;
            int lo = default;
            int hi = default;
            {
                (lo, t, err3) = this.ParseClassChar(t, s);
                if ((err3 != null))
                {
                    return (emptyText, err3);
                }
            }

            hi = lo;
            if ((((t.Length >= 2) && (t[0] == '-')) && (t[1] != ']')))
            {
                t = t.Slice(1, t.Length);
                {
                    (hi, t, err3) = this.ParseClassChar(t, s);
                    if ((err3 != null))
                    {
                        return (emptyText, err3);
                    }
                }

                if ((hi < lo))
                {
                    rng = rng.Slice(0, (rng.Length - t.Length));
                    return (emptyText, new RegexParseError(ErrInvalidCharRange, rng));
                }
            }

            if (((this.flags & RegexFlags.FoldCase) == 0))
            {
                @class = appendRange(@class, lo, hi);
            }
            else
            {
                @class = appendFoldedRange(@class, lo, hi);
            }
        }

        t = t.Slice(1, t.Length);
        re.Rune = @class;
        @class = cleanClass(re.Rune);
        if ((sign < 0))
        {
            @class = negateClass(@class);
        }

        re.Rune = @class;
        this.push(re);
        return (t, null);
    }

    // cleanClass sorts the ranges (pairs of elements of r),
    // merges them, and eliminates duplicates.
    // Go1.27.1 src/regexp/syntax/parse.go:1932.
    private static RegexSlice<int> cleanClass(RegexSlice<int> rp)
    {
        // The original ranges adapter swaps adjacent rune pairs in the same backing owner.
        // A borrowed pair view feeds the existing pinned Go pdqsort implementation.
        GoSliceSort.SortFunc(MemoryMarshal.Cast<int, RuneRangePair>(rp.AsSpan()), CompareRuneRanges);
        var r = rp;
        if ((r.Length < 2))
        {
            return r;
        }

        var w = 2;
        {
            var i = 2;
            for (; (i < r.Length); i += 2)
            {
                var tuple26 = (r[i], r[(i + 1)]);
                var lo = tuple26.Item1;
                var hi = tuple26.Item2;
                if ((lo <= (r[(w - 1)] + 1)))
                {
                    if ((hi > r[(w - 1)]))
                    {
                        r[(w - 1)] = hi;
                    }

                    continue;
                }

                r[w] = lo;
                r[(w + 1)] = hi;
                w += 2;
            }
        }

        return r.Slice2(0, w);
    }

    // inCharClass reports whether r is in the class.
    // It assumes the class has been cleaned by cleanClass.
    // Go1.27.1 src/regexp/syntax/parse.go:1964.
    private static bool inCharClass(int r, RegexSlice<int> @class)
    {
        // Go1.27.1 sort/search.go:99: Find, with the original comparison inlined.
        var n = @class.Length / 2;
        var i = 0;
        var j = n;
        while (i < j)
        {
            var h = (int)((uint)(i + j) >> 1);
            if (r > @class[2 * h + 1])
                i = h + 1;
            else
                j = h;
        }

        return i < n && r >= @class[2 * i] && r <= @class[2 * i + 1];
    }

    // appendLiteral returns the result of appending the literal x to the class r.
    // Go1.27.1 src/regexp/syntax/parse.go:1979.
    private static RegexSlice<int> appendLiteral(RegexSlice<int> r, int x, RegexFlags flags)
    {
        if (((flags & RegexFlags.FoldCase) != 0))
        {
            return appendFoldedRange(r, x, x);
        }

        return appendRange(r, x, x);
    }

    // appendRange returns the result of appending the range lo-hi to the class r.
    // Go1.27.1 src/regexp/syntax/parse.go:1987.
    internal static RegexSlice<int> appendRange(RegexSlice<int> r, int lo, int hi)
    {
        var n = r.Length;
        {
            var i = 2;
            for (; (i <= 4); i += 2)
            {
                if ((n >= i))
                {
                    var tuple29 = (r[(n - i)], r[((n - i) + 1)]);
                    var rlo = tuple29.Item1;
                    var rhi = tuple29.Item2;
                    if (((lo <= (rhi + 1)) && (rlo <= (hi + 1))))
                    {
                        if ((lo < rlo))
                        {
                            r[(n - i)] = lo;
                        }

                        if ((hi > rhi))
                        {
                            r[((n - i) + 1)] = hi;
                        }

                        return r;
                    }
                }
            }
        }

        return r.Append(lo, hi);
    }

    // appendFoldedRange returns the result of appending the range lo-hi
    // and its case folding-equivalent runes to the class r.
    // Go1.27.1 src/regexp/syntax/parse.go:2020.
    private static RegexSlice<int> appendFoldedRange(RegexSlice<int> r, int lo, int hi)
    {
        if (((lo <= minFold) && (hi >= maxFold)))
        {
            return appendRange(r, lo, hi);
        }

        if (((hi < minFold) || (lo > maxFold)))
        {
            return appendRange(r, lo, hi);
        }

        if ((lo < minFold))
        {
            r = appendRange(r, lo, (minFold - 1));
            lo = minFold;
        }

        if ((hi > maxFold))
        {
            r = appendRange(r, (maxFold + 1), hi);
            hi = maxFold;
        }

        {
            var c = lo;
            for (; (c <= hi); c++)
            {
                r = appendRange(r, c, c);
                var f = UnicodeCase.SimpleFold(c);
                while ((f != c))
                {
                    r = appendRange(r, f, f);
                    f = UnicodeCase.SimpleFold(f);
                }
            }
        }

        return r;
    }

    // appendClass returns the result of appending the class x to the class r.
    // It assume x is clean.
    // Go1.27.1 src/regexp/syntax/parse.go:2055.
    private static RegexSlice<int> appendClass(RegexSlice<int> r, RegexSlice<int> x)
    {
        {
            var i = 0;
            for (; (i < x.Length); i += 2)
            {
                r = appendRange(r, x[i], x[(i + 1)]);
            }
        }

        return r;
    }

    // appendFoldedClass returns the result of appending the case folding of the class x to the class r.
    // Go1.27.1 src/regexp/syntax/parse.go:2063.
    private static RegexSlice<int> appendFoldedClass(RegexSlice<int> r, RegexSlice<int> x)
    {
        {
            var i = 0;
            for (; (i < x.Length); i += 2)
            {
                r = appendFoldedRange(r, x[i], x[(i + 1)]);
            }
        }

        return r;
    }

    // appendNegatedClass returns the result of appending the negation of the class x to the class r.
    // It assumes x is clean.
    // Go1.27.1 src/regexp/syntax/parse.go:2072.
    private static RegexSlice<int> appendNegatedClass(RegexSlice<int> r, RegexSlice<int> x)
    {
        var nextLo = 0;
        {
            var i = 0;
            for (; (i < x.Length); i += 2)
            {
                var tuple30 = (x[i], x[(i + 1)]);
                var lo = tuple30.Item1;
                var hi = tuple30.Item2;
                if ((nextLo <= (lo - 1)))
                {
                    r = appendRange(r, nextLo, (lo - 1));
                }

                nextLo = (hi + 1);
            }
        }

        if ((nextLo <= 0x10ffff))
        {
            r = appendRange(r, nextLo, 0x10ffff);
        }

        return r;
    }

    // appendTable returns the result of appending x to the class r.
    // Go1.27.1 src/regexp/syntax/parse.go:2088.
    private static RegexSlice<int> appendTable(RegexSlice<int> r, GoUnicode.RangeTable x)
    {
        {
            foreach (var xr in x.R16)
            {
                var tuple32 = ((int)(xr.Lo), (int)(xr.Hi), (int)(xr.Stride));
                var lo = tuple32.Item1;
                var hi = tuple32.Item2;
                var stride = tuple32.Item3;
                if ((stride == 1))
                {
                    r = appendRange(r, lo, hi);
                    continue;
                }

                {
                    var c = lo;
                    for (; (c <= hi); c += stride)
                    {
                        r = appendRange(r, c, c);
                    }
                }
            }
        }

        {
            foreach (var xr2 in x.R32)
            {
                var tuple34 = ((int)(xr2.Lo), (int)(xr2.Hi), (int)(xr2.Stride));
                var lo2 = tuple34.Item1;
                var hi2 = tuple34.Item2;
                var stride2 = tuple34.Item3;
                if ((stride2 == 1))
                {
                    r = appendRange(r, lo2, hi2);
                    continue;
                }

                {
                    var c2 = lo2;
                    for (; (c2 <= hi2); c2 += stride2)
                    {
                        r = appendRange(r, c2, c2);
                    }
                }
            }
        }

        return r;
    }

    // appendNegatedTable returns the result of appending the negation of x to the class r.
    // Go1.27.1 src/regexp/syntax/parse.go:2113.
    private static RegexSlice<int> appendNegatedTable(RegexSlice<int> r, GoUnicode.RangeTable x)
    {
        var nextLo = 0;
        {
            foreach (var xr in x.R16)
            {
                var tuple36 = ((int)(xr.Lo), (int)(xr.Hi), (int)(xr.Stride));
                var lo = tuple36.Item1;
                var hi = tuple36.Item2;
                var stride = tuple36.Item3;
                if ((stride == 1))
                {
                    if ((nextLo <= (lo - 1)))
                    {
                        r = appendRange(r, nextLo, (lo - 1));
                    }

                    nextLo = (hi + 1);
                    continue;
                }

                {
                    var c = lo;
                    for (; (c <= hi); c += stride)
                    {
                        if ((nextLo <= (c - 1)))
                        {
                            r = appendRange(r, nextLo, (c - 1));
                        }

                        nextLo = (c + 1);
                    }
                }
            }
        }

        {
            foreach (var xr2 in x.R32)
            {
                var tuple38 = ((int)(xr2.Lo), (int)(xr2.Hi), (int)(xr2.Stride));
                var lo2 = tuple38.Item1;
                var hi2 = tuple38.Item2;
                var stride2 = tuple38.Item3;
                if ((stride2 == 1))
                {
                    if ((nextLo <= (lo2 - 1)))
                    {
                        r = appendRange(r, nextLo, (lo2 - 1));
                    }

                    nextLo = (hi2 + 1);
                    continue;
                }

                {
                    var c2 = lo2;
                    for (; (c2 <= hi2); c2 += stride2)
                    {
                        if ((nextLo <= (c2 - 1)))
                        {
                            r = appendRange(r, nextLo, (c2 - 1));
                        }

                        nextLo = (c2 + 1);
                    }
                }
            }
        }

        if ((nextLo <= 0x10ffff))
        {
            r = appendRange(r, nextLo, 0x10ffff);
        }

        return r;
    }

    // negateClass overwrites r and returns r's negation.
    // It assumes the class r is already clean.
    // Go1.27.1 src/regexp/syntax/parse.go:2155.
    private static RegexSlice<int> negateClass(RegexSlice<int> r)
    {
        var nextLo = 0;
        var w = 0;
        {
            var i = 0;
            for (; (i < r.Length); i += 2)
            {
                var tuple39 = (r[i], r[(i + 1)]);
                var lo = tuple39.Item1;
                var hi = tuple39.Item2;
                if ((nextLo <= (lo - 1)))
                {
                    r[w] = nextLo;
                    r[(w + 1)] = (lo - 1);
                    w += 2;
                }

                nextLo = (hi + 1);
            }
        }

        r = r.Slice2(0, w);
        if ((nextLo <= 0x10ffff))
        {
            r = r.Append(nextLo, 0x10ffff);
        }

        return r;
    }

    // Go1.27.1 src/regexp/syntax/parse.go:2184–2199: ranges view/comparison/swaps.
    [StructLayout(LayoutKind.Sequential)]
    private struct RuneRangePair
    {
        internal int Lo;
        internal int Hi;
    }

    private static readonly Comparison<RuneRangePair> CompareRuneRanges = static (a, b) => a.Lo < b.Lo ? -1 : a.Lo > b.Lo ? 1 : a.Hi > b.Hi ? -1 : a.Hi < b.Hi ? 1 : 0;
    // Go1.27.1 src/regexp/syntax/parse.go:2202.
    private static readonly ByteStringValue emptyText = ByteStringValue.FromString("");
    private static (int Rune, int Size) DecodeRune(ByteStringValue text) => text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan) : Utf8.DecodeRuneInString(text.CharSpan);
    // parse.go:2198. Error Expr retains the exact offending remainder, including malformed bytes.
    private static RegexParseError? CheckUTF8(ByteStringValue text)
    {
        while (text.Length != 0)
        {
            var(rune, size) = DecodeRune(text);
            if (rune == Utf8.RuneError && size == 1)
                return new(ErrInvalidUTF8, text);
            text = text.Slice(size);
        }

        return null;
    }

    // parse.go:2209.
    private static (int Rune, ByteStringValue Remainder, RegexParseError? Error) NextRune(ByteStringValue text)
    {
        var(rune, size) = DecodeRune(text);
        if (rune == Utf8.RuneError && size == 1)
            return (0, emptyText, new(ErrInvalidUTF8, text));
        return (rune, text.Slice(size), null);
    }

    // parse.go:2217.
    private static bool IsAlnum(int rune) => rune is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    // parse.go:2221.
    private static int Unhex(int rune)
    {
        if (rune is >= '0' and <= '9')
            return rune - '0';
        if (rune is >= 'a' and <= 'f')
            return rune - 'a' + 10;
        if (rune is >= 'A' and <= 'F')
            return rune - 'A' + 10;
        return -1;
    }

    // parse.go:1273. Repeat admission bounds the parsed value before it becomes an array/count index.
    private static (int Number, ByteStringValue Remainder, bool Ok) ParseInt(ByteStringValue text)
    {
        if (text.Length == 0 || text[0] < '0' || text[0] > '9')
            return (0, emptyText, false);
        if (text.Length >= 2 && text[0] == '0' && text[1] is >= '0' and <= '9')
            return (0, emptyText, false);
        var digits = text;
        while (text.Length != 0 && text[0] is >= '0' and <= '9')
            text = text.Slice(1);
        digits = digits.Slice(0, digits.Length - text.Length);
        var number = 0;
        for (var index = 0; index < digits.Length; index++)
        {
            if (number >= 100000000)
            {
                number = -1;
                break;
            }

            number = number * 10 + digits[index] - '0';
        }

        return (number, text, true);
    }

    // Go1.27.1 strings/strings.go Cut: borrowed prefix/suffix headers and empty suffix on absence.
    private static (ByteStringValue Before, ByteStringValue After, bool Found) CutInput(ByteStringValue input, ByteStringValue separator)
    {
        var index = input.IndexOf(separator);
        return index >= 0 ? (input.Slice(0, index), input.Slice(index + separator.Length), true) : (input, emptyText, false);
    }

    private static RegexSlice<int> makeRunes(ByteStringValue input)
    {
        var remainder = input;
        var count = 0;
        while (remainder.Length != 0)
        {
            var(_, width) = DecodeRune(remainder);
            remainder = remainder.Slice(width);
            count++;
        }

        var result = RegexSlice<int>.FromArray(new int[count]);
        remainder = input;
        for (var index = 0; index < count; index++)
        {
            var(rune, width) = DecodeRune(remainder);
            result[index] = rune;
            remainder = remainder.Slice(width);
        }

        return result;
    }

    // Go built-in copy preserves overlap and returns the copied prefix length.
    private static int CopyPrefix<T>(RegexSlice<T> destination, RegexSlice<T> source)
    {
        var count = Math.Min(destination.Length, source.Length);
        source.AsSpan()[..count].CopyTo(destination.AsSpan());
        return count;
    }
}
