// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/syntax/regexp.go selected tree structure/capture workers.
using TypeScript.Scanner;
using System.Runtime.CompilerServices;

namespace TypeScript.RegularExpressions.Syntax;
internal enum RegexOp : byte
{
    NoMatch = 1,
    EmptyMatch,
    Literal,
    CharClass,
    AnyCharNotNL,
    AnyChar,
    BeginLine,
    EndLine,
    BeginText,
    EndText,
    WordBoundary,
    NoWordBoundary,
    Capture,
    Star,
    Plus,
    Quest,
    Repeat,
    Concat,
    Alternate,
    LeftParen = 128,
    VerticalBar
}

[InlineArray(1)]
internal struct RegexSubStorage
{
    private RegexNode? element;
}

[InlineArray(2)]
internal struct RegexRuneStorage
{
    private int element;
}

internal sealed partial class RegexNode : IRegexInlineStorage<RegexNode?>, IRegexInlineStorage<int>
{
    internal RegexOp Op;
    internal RegexFlags Flags;
    internal RegexSlice<RegexNode?> Sub;
    internal readonly RegexSlice<RegexNode?> Sub0;
    internal RegexSlice<int> Rune;
    internal readonly RegexSlice<int> Rune0;
    private RegexSubStorage subStorage;
    private RegexRuneStorage runeStorage;
    internal int Min, Max, Cap;
    internal ByteStringValue Name = ByteStringValue.FromString("");
    internal RegexNode()
    {
        Sub0 = RegexSlice<RegexNode?>.Inline(this, 1);
        Rune0 = RegexSlice<int>.Inline(this, 2);
    }

    ref RegexNode? IRegexInlineStorage<RegexNode?>.Element(int index) => ref subStorage[index];
    ref int IRegexInlineStorage<int>.Element(int index) => ref runeStorage[index];
    // parse.go:146. Reused nodes are cleared in place, retaining their embedded storage owner.
    internal void Clear()
    {
        Op = 0;
        Flags = 0;
        Sub = default;
        Rune = default;
        subStorage = default;
        runeStorage = default;
        Min = Max = Cap = 0;
        Name = ByteStringValue.FromString("");
    }
}

internal sealed partial class RegexNode
{
    // regexp.go:64. Null receiver comparison is kept in the actual static worker.
    internal bool Equal(RegexNode? other) => EqualNodes(this, other);
    private static bool EqualNodes(RegexNode? left, RegexNode? right)
    {
        if (left is null || right is null)
            return ReferenceEquals(left, right);
        if (left.Op != right.Op)
            return false;
        switch (left.Op)
        {
            case RegexOp.EndText:
                if ((left.Flags & RegexFlags.WasDollar) != (right.Flags & RegexFlags.WasDollar))
                    return false;
                break;
            case RegexOp.Literal:
            case RegexOp.CharClass:
                return (left.Flags & RegexFlags.FoldCase) == (right.Flags & RegexFlags.FoldCase) && left.Rune.AsSpan().SequenceEqual(right.Rune.AsSpan());
            case RegexOp.Alternate:
            case RegexOp.Concat:
                if (left.Sub.Length != right.Sub.Length)
                    return false;
                for (var index = 0; index < left.Sub.Length; index++)
                    if (!EqualNodes(left.Sub[index], right.Sub[index]))
                        return false;
                return true;
            case RegexOp.Star:
            case RegexOp.Plus:
            case RegexOp.Quest:
                if ((left.Flags & RegexFlags.NonGreedy) != (right.Flags & RegexFlags.NonGreedy) || !EqualNodes(left.Sub[0], right.Sub[0]))
                    return false;
                break;
            case RegexOp.Repeat:
                if ((left.Flags & RegexFlags.NonGreedy) != (right.Flags & RegexFlags.NonGreedy) || left.Min != right.Min || left.Max != right.Max || !EqualNodes(left.Sub[0], right.Sub[0]))
                    return false;
                break;
            case RegexOp.Capture:
                if (left.Cap != right.Cap || !left.Name.EqualsValue(right.Name) || !EqualNodes(left.Sub[0], right.Sub[0]))
                    return false;
                break;
        }

        return true;
    }

    // regexp.go:437.
    internal int MaxCap()
    {
        var maximum = Op == RegexOp.Capture ? Cap : 0;
        foreach (var child in Sub)
        {
            var count = child!.MaxCap();
            if (maximum < count)
                maximum = count;
        }

        return maximum;
    }

    // regexp.go:451. Strings remain immutable borrowed headers; the slice itself is a required new owner.
    internal Slice<ByteStringValue> CapNames()
    {
        var names = Slice<ByteStringValue>.Make(MaxCap() + 1);
        // CLR byte-value default denotes absence; Go string zero denotes actual empty text.
        names.AsSpan().Fill(ByteStringValue.FromString(""));
        FillCapNames(names);
        return names;
    }

    // regexp.go:457.
    private void FillCapNames(Slice<ByteStringValue> names)
    {
        if (Op == RegexOp.Capture)
            names[Cap] = Name;
        foreach (var child in Sub)
            child!.FillCapNames(names);
    }
}
