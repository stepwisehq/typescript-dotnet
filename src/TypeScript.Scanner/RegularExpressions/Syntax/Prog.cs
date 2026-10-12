// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go 1.27.1 regexp/syntax/prog.go matching workers. Diagnostic dumps are outside the selected Compile/MatchString closure.
using TypeScript.Scanner;

namespace TypeScript.RegularExpressions.Syntax;
// prog.go:17/25/61/115. Instruction records stay values in the actual program arena.
internal enum InstructionOp : byte
{
    Alt,
    AltMatch,
    Capture,
    EmptyWidth,
    Match,
    Fail,
    Nop,
    Rune,
    Rune1,
    RuneAny,
    RuneAnyNotNL
}

[Flags]
internal enum EmptyOp : byte
{
    None = 0,
    BeginLine = 1 << 0,
    EndLine = 1 << 1,
    BeginText = 1 << 2,
    EndText = 1 << 3,
    WordBoundary = 1 << 4,
    NoWordBoundary = 1 << 5
}

internal partial struct Instruction
{
    internal InstructionOp Op;
    internal uint Out, Arg;
    internal RegexSlice<int> Rune;
}

internal sealed partial class RegexProgram
{
    internal Slice<Instruction> Inst;
    internal int Start;
    internal int NumCap;
}

internal static class RegexAssertions
{
    // prog.go:79. Boundary assertions use the original ASCII-only word definition.
    internal static EmptyOp EmptyOpContext(int before, int after)
    {
        var op = EmptyOp.NoWordBoundary;
        byte boundary = 0;
        if (IsWordChar(before))
            boundary = 1;
        else if (before == '\n')
            op |= EmptyOp.BeginLine;
        else if (before < 0)
            op |= EmptyOp.BeginText | EmptyOp.BeginLine;
        if (IsWordChar(after))
            boundary ^= 1;
        else if (after == '\n')
            op |= EmptyOp.EndLine;
        else if (after < 0)
            op |= EmptyOp.EndText | EmptyOp.EndLine;
        if (boundary != 0)
            op ^= EmptyOp.WordBoundary | EmptyOp.NoWordBoundary;
        return op;
    }

    // prog.go:110.
    internal static bool IsWordChar(int rune) => rune is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';
}

internal sealed partial class RegexProgram
{
    // prog.go:131. References point into the retained instruction arena.
    internal ref Instruction SkipNop(uint pc)
    {
        ref var instruction = ref Inst.AsSpan()[(int)pc];
        while (instruction.Op is InstructionOp.Nop or InstructionOp.Capture)
            instruction = ref Inst.AsSpan()[(int)instruction.Out];
        return ref instruction;
    }

    // prog.go:151. Prefix keeps the original empty-prefix allocation fast path.
    internal (ByteStringValue Prefix, bool Complete) Prefix()
    {
        ref var instruction = ref SkipNop((uint)Start);
        if (instruction.RuneOp() != InstructionOp.Rune || instruction.Rune.Length != 1)
            return (ByteStringValue.FromString(""), instruction.Op == InstructionOp.Match);
        var buffer = new GoBuilder();
        while (instruction.RuneOp() == InstructionOp.Rune && instruction.Rune.Length == 1 && ((RegexFlags)instruction.Arg & RegexFlags.FoldCase) == 0 && instruction.Rune[0] != Utf8.RuneError)
        {
            buffer.WriteRune(instruction.Rune[0]);
            instruction = ref SkipNop(instruction.Out);
        }

        return (buffer.Value(), instruction.Op == InstructionOp.Match);
    }

    // prog.go:169.
    internal EmptyOp StartCond()
    {
        EmptyOp flags = 0;
        var pc = (uint)Start;
        while (true)
        {
            ref var instruction = ref Inst.AsSpan()[(int)pc];
            switch (instruction.Op)
            {
                case InstructionOp.EmptyWidth:
                    flags |= (EmptyOp)instruction.Arg;
                    break;
                case InstructionOp.Fail:
                    return unchecked((EmptyOp)byte.MaxValue);
                case InstructionOp.Capture:
                case InstructionOp.Nop:
                    break;
                default:
                    return flags;
            }

            pc = instruction.Out;
        }
    }
}

internal partial struct Instruction
{
    // prog.go:140.
    internal readonly InstructionOp RuneOp() => Op is InstructionOp.Rune1 or InstructionOp.RuneAny or InstructionOp.RuneAnyNotNL ? InstructionOp.Rune : Op;
    // prog.go:195/204. Capture and rune ranges retain their original arena/header owners.
    internal readonly bool MatchRune(int rune) => MatchRunePos(rune) != -1;
    internal readonly int MatchRunePos(int value)
    {
        var rune = Rune;
        switch (rune.Length)
        {
            case 0:
                return -1;
            case 1:
                var first = rune[0];
                if (value == first)
                    return 0;
                if (((RegexFlags)Arg & RegexFlags.FoldCase) != 0)
                    for (var folded = UnicodeCase.SimpleFold(first); folded != first; folded = UnicodeCase.SimpleFold(folded))
                        if (value == folded)
                            return 0;
                return -1;
            case 2:
                return value >= rune[0] && value <= rune[1] ? 0 : -1;
            case 4:
            case 6:
            case 8:
                for (var index = 0; index < rune.Length; index += 2)
                {
                    if (value < rune[index])
                        return -1;
                    if (value <= rune[index + 1])
                        return index / 2;
                }

                return -1;
        }

        var low = 0;
        var high = rune.Length / 2;
        while (low < high)
        {
            var middle = (int)((uint)(low + high) >> 1);
            if (rune[2 * middle] <= value)
            {
                if (value <= rune[2 * middle + 1])
                    return middle;
                low = middle + 1;
            }
            else
                high = middle;
        }

        return -1;
    }

    // prog.go:268.
    internal readonly bool MatchEmptyWidth(int before, int after) => (EmptyOp)Arg switch
    {
        EmptyOp.BeginLine => before == '\n' || before == -1,
        EmptyOp.EndLine => after == '\n' || after == -1,
        EmptyOp.BeginText => before == -1,
        EmptyOp.EndText => after == -1,
        EmptyOp.WordBoundary => RegexAssertions.IsWordChar(before) != RegexAssertions.IsWordChar(after),
        EmptyOp.NoWordBoundary => RegexAssertions.IsWordChar(before) == RegexAssertions.IsWordChar(after),
        _ => throw new InvalidOperationException("unknown empty width arg")};
}
