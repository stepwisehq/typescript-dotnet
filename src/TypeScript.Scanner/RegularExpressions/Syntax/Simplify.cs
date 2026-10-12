// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned Go1.27.1 regexp/syntax/simplify.go complete worker family.
namespace TypeScript.RegularExpressions.Syntax;
internal sealed partial class RegexNode
{
    // simplify.go:27 original *nre = *re: inline values copy, retained slice descriptors still share their original owners.
    private RegexNode CopyValue() => new()
    {
        Op = Op,
        Flags = Flags,
        Sub = Sub,
        Rune = Rune,
        subStorage = subStorage,
        runeStorage = runeStorage,
        Min = Min,
        Max = Max,
        Cap = Cap,
        Name = Name
    };
    // simplify.go:14. Nil receiver admission is retained by the static entry; identities of unchanged children stay stable.
    internal static RegexNode? Simplify(RegexNode? expression)
    {
        if (expression is null)
            return null;
        switch (expression.Op)
        {
            case RegexOp.Capture:
            case RegexOp.Concat:
            case RegexOp.Alternate:
                var result = expression;
                for (int index = 0; index < expression.Sub.Length; index++)
                {
                    var child = expression.Sub[index];
                    var simplified = Simplify(child);
                    if (ReferenceEquals(result, expression) && !ReferenceEquals(simplified, child))
                    {
                        result = expression.CopyValue();
                        result.Rune = default;
                        result.Sub = result.Sub0.Slice2(0, 0).Append(expression.Sub.Slice2(0, index));
                    }

                    if (!ReferenceEquals(result, expression))
                        result.Sub = result.Sub.Append(simplified);
                }

                return result;
            case RegexOp.Star:
            case RegexOp.Plus:
            case RegexOp.Quest:
                return SimplifyOne(expression.Op, expression.Flags, Simplify(expression.Sub[0])!, expression);
            case RegexOp.Repeat:
                if (expression.Min == 0 && expression.Max == 0)
                    return new()
                    {
                        Op = RegexOp.EmptyMatch
                    };
                var subexpression = Simplify(expression.Sub[0])!;
                if (expression.Max == -1)
                {
                    if (expression.Min == 0)
                        return SimplifyOne(RegexOp.Star, expression.Flags, subexpression, null);
                    if (expression.Min == 1)
                        return SimplifyOne(RegexOp.Plus, expression.Flags, subexpression, null);
                    var concat = new RegexNode
                    {
                        Op = RegexOp.Concat
                    };
                    concat.Sub = concat.Sub0.Slice2(0, 0);
                    for (int index = 0; index < expression.Min - 1; index++)
                        concat.Sub = concat.Sub.Append(subexpression);
                    concat.Sub = concat.Sub.Append(SimplifyOne(RegexOp.Plus, expression.Flags, subexpression, null));
                    return concat;
                }

                if (expression.Min == 1 && expression.Max == 1)
                    return subexpression;
                RegexNode? prefix = null;
                if (expression.Min > 0)
                {
                    prefix = new()
                    {
                        Op = RegexOp.Concat
                    };
                    prefix.Sub = prefix.Sub0.Slice2(0, 0);
                    for (int index = 0; index < expression.Min; index++)
                        prefix.Sub = prefix.Sub.Append(subexpression);
                }

                if (expression.Max > expression.Min)
                {
                    var suffix = SimplifyOne(RegexOp.Quest, expression.Flags, subexpression, null);
                    for (int index = expression.Min + 1; index < expression.Max; index++)
                    {
                        var concat = new RegexNode
                        {
                            Op = RegexOp.Concat
                        };
                        concat.Sub = concat.Sub0.Slice2(0, 0).Append(subexpression).Append(suffix);
                        suffix = SimplifyOne(RegexOp.Quest, expression.Flags, concat, null);
                    }

                    if (prefix is null)
                        return suffix;
                    prefix.Sub = prefix.Sub.Append(suffix);
                }

                if (prefix is not null)
                    return prefix;
                return new()
                {
                    Op = RegexOp.NoMatch
                };
        }

        return expression;
    }

    // simplify.go:134. Preserve empty/idempotent/identity fast paths before owning a replacement.
    private static RegexNode SimplifyOne(RegexOp op, RegexFlags flags, RegexNode subexpression, RegexNode? expression)
    {
        if (subexpression.Op == RegexOp.EmptyMatch)
            return subexpression;
        if (op == subexpression.Op && (flags & RegexFlags.NonGreedy) == (subexpression.Flags & RegexFlags.NonGreedy))
            return subexpression;
        if (expression is not null && expression.Op == op && (expression.Flags & RegexFlags.NonGreedy) == (flags & RegexFlags.NonGreedy) && ReferenceEquals(subexpression, expression.Sub[0]))
            return expression;
        var result = new RegexNode
        {
            Op = op,
            Flags = flags
        };
        result.Sub = result.Sub0.Slice2(0, 0).Append(subexpression);
        return result;
    }
}
