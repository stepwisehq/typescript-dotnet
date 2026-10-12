// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned Go1.27.1 regexp/syntax/compile.go complete instruction compiler.
using TypeScript.Scanner;

namespace TypeScript.RegularExpressions.Syntax;
internal readonly struct PatchList
{
    internal readonly uint Head, Tail;
    internal PatchList(uint head, uint tail)
    {
        Head = head;
        Tail = tail;
    }

    // compile.go:23/27. Patch pointers remain locations in the actual instruction value arena.
    internal static PatchList Make(uint location) => new(location, location);
    internal void Patch(RegexProgram program, uint value)
    {
        uint head = Head;
        while (head != 0)
        {
            ref var instruction = ref program.Inst.AsSpan()[(int)(head >> 1)];
            if ((head & 1) == 0)
            {
                head = instruction.Out;
                instruction.Out = value;
            }
            else
            {
                head = instruction.Arg;
                instruction.Arg = value;
            }
        }
    }

    // compile.go:41.
    internal PatchList Append(RegexProgram program, PatchList second)
    {
        if (Head == 0)
            return second;
        if (second.Head == 0)
            return this;
        ref var instruction = ref program.Inst.AsSpan()[(int)(Tail >> 1)];
        if ((Tail & 1) == 0)
            instruction.Out = second.Head;
        else
            instruction.Arg = second.Head;
        return new(Head, second.Tail);
    }
}

internal struct Fragment
{
    internal uint Index;
    internal PatchList Out;
    internal bool Nullable;
    internal Fragment(uint index, PatchList output, bool nullable)
    {
        Index = index;
        Out = output;
        Nullable = nullable;
    }
}

internal struct RegexCompiler
{
    private RegexProgram program;
    private static readonly RegexSlice<int> AnyRuneNotNL = RegexSlice<int>.FromArray([0, '\n' - 1, '\n' + 1, 0x10FFFF]);
    private static readonly RegexSlice<int> AnyRune = RegexSlice<int>.FromArray([0, 0x10FFFF]);
    // compile.go:71. Compile has no source error path; parser/simplifier admission precedes it.
    internal static RegexProgram Compile(RegexNode expression)
    {
        RegexCompiler compiler = default;
        compiler.Init();
        var fragment = compiler.CompileNode(expression);
        var program = compiler.program!; // Init establishes the actual program owner.
        fragment.Out.Patch(program, compiler.Inst(InstructionOp.Match).Index);
        program.Start = (int)fragment.Index;
        return program;
    }

    // compile.go:80.
    private void Init()
    {
        program = new()
        {
            NumCap = 2
        };
        Inst(InstructionOp.Fail);
    }

    // compile.go:89. Preserve precedence, nullable flags and exact alternative patch order.
    private Fragment CompileNode(RegexNode expression)
    {
        switch (expression.Op)
        {
            case RegexOp.NoMatch:
                return Fail();
            case RegexOp.EmptyMatch:
                return Nop();
            case RegexOp.Literal:
                if (expression.Rune.Length == 0)
                    return Nop();
                Fragment literal = default;
                for (int index = 0; index < expression.Rune.Length; index++)
                {
                    var fragment = Rune(expression.Rune.Slice2(index, index + 1), expression.Flags);
                    literal = index == 0 ? fragment : Cat(literal, fragment);
                }

                return literal;
            case RegexOp.CharClass:
                return Rune(expression.Rune, expression.Flags);
            case RegexOp.AnyCharNotNL:
                return Rune(AnyRuneNotNL, 0);
            case RegexOp.AnyChar:
                return Rune(AnyRune, 0);
            case RegexOp.BeginLine:
                return Empty(EmptyOp.BeginLine);
            case RegexOp.EndLine:
                return Empty(EmptyOp.EndLine);
            case RegexOp.BeginText:
                return Empty(EmptyOp.BeginText);
            case RegexOp.EndText:
                return Empty(EmptyOp.EndText);
            case RegexOp.WordBoundary:
                return Empty(EmptyOp.WordBoundary);
            case RegexOp.NoWordBoundary:
                return Empty(EmptyOp.NoWordBoundary);
            case RegexOp.Capture:
                var opening = Capture((uint)(expression.Cap << 1));
                var subexpression = CompileNode(expression.Sub[0]!);
                var closing = Capture((uint)((expression.Cap << 1) | 1));
                return Cat(Cat(opening, subexpression), closing);
            case RegexOp.Star:
                return Star(CompileNode(expression.Sub[0]!), (expression.Flags & RegexFlags.NonGreedy) != 0);
            case RegexOp.Plus:
                return Plus(CompileNode(expression.Sub[0]!), (expression.Flags & RegexFlags.NonGreedy) != 0);
            case RegexOp.Quest:
                return Quest(CompileNode(expression.Sub[0]!), (expression.Flags & RegexFlags.NonGreedy) != 0);
            case RegexOp.Concat:
                if (expression.Sub.Length == 0)
                    return Nop();
                Fragment concat = default;
                for (int index = 0; index < expression.Sub.Length; index++)
                    concat = index == 0 ? CompileNode(expression.Sub[index]!) : Cat(concat, CompileNode(expression.Sub[index]!));
                return concat;
            case RegexOp.Alternate:
                Fragment alternate = default;
                foreach (var child in expression.Sub)
                    alternate = Alt(alternate, CompileNode(child!));
                return alternate;
        }

        throw new InvalidOperationException("regexp: unhandled case in compile");
    }

    // compile.go:161. Original fail instruction makes location zero unusable as an output link.
    private Fragment Inst(InstructionOp op)
    {
        var fragment = new Fragment
        {
            Index = (uint)program.Inst.Length,
            Nullable = true
        };
        program.Inst = program.Inst.Append(new Instruction { Op = op });
        return fragment;
    }

    // compile.go:168/174/178.
    private Fragment Nop()
    {
        var fragment = Inst(InstructionOp.Nop);
        fragment.Out = PatchList.Make(fragment.Index << 1);
        return fragment;
    }

    private static Fragment Fail() => default;
    private Fragment Capture(uint argument)
    {
        var fragment = Inst(InstructionOp.Capture);
        fragment.Out = PatchList.Make(fragment.Index << 1);
        program.Inst.AsSpan()[(int)fragment.Index].Arg = argument;
        if (program.NumCap < (int)argument + 1)
            program.NumCap = (int)argument + 1;
        return fragment;
    }

    // compile.go:189.
    private Fragment Cat(Fragment first, Fragment second)
    {
        if (first.Index == 0 || second.Index == 0)
            return default;
        first.Out.Patch(program, second.Index);
        return new(first.Index, second.Out, first.Nullable && second.Nullable);
    }

    // compile.go:201.
    private Fragment Alt(Fragment first, Fragment second)
    {
        if (first.Index == 0)
            return second;
        if (second.Index == 0)
            return first;
        var fragment = Inst(InstructionOp.Alt);
        ref var instruction = ref program.Inst.AsSpan()[(int)fragment.Index];
        instruction.Out = first.Index;
        instruction.Arg = second.Index;
        fragment.Out = first.Out.Append(program, second.Out);
        fragment.Nullable = first.Nullable || second.Nullable;
        return fragment;
    }

    // compile.go:219.
    private Fragment Quest(Fragment first, bool nongreedy)
    {
        var fragment = Inst(InstructionOp.Alt);
        ref var instruction = ref program.Inst.AsSpan()[(int)fragment.Index];
        if (nongreedy)
        {
            instruction.Arg = first.Index;
            fragment.Out = PatchList.Make(fragment.Index << 1);
        }
        else
        {
            instruction.Out = first.Index;
            fragment.Out = PatchList.Make((fragment.Index << 1) | 1);
        }

        fragment.Out = fragment.Out.Append(program, first.Out);
        return fragment;
    }

    // compile.go:238. Nullable star uses (first+)? to preserve source priority order.
    private Fragment Loop(Fragment first, bool nongreedy)
    {
        var fragment = Inst(InstructionOp.Alt);
        ref var instruction = ref program.Inst.AsSpan()[(int)fragment.Index];
        if (nongreedy)
        {
            instruction.Arg = first.Index;
            fragment.Out = PatchList.Make(fragment.Index << 1);
        }
        else
        {
            instruction.Out = first.Index;
            fragment.Out = PatchList.Make((fragment.Index << 1) | 1);
        }

        first.Out.Patch(program, fragment.Index);
        return fragment;
    }

    // compile.go:252/261/265.
    private Fragment Star(Fragment first, bool nongreedy) => first.Nullable ? Quest(Plus(first, nongreedy), nongreedy) : Loop(first, nongreedy);
    private Fragment Plus(Fragment first, bool nongreedy) => new(first.Index, Loop(first, nongreedy).Out, first.Nullable);
    private Fragment Empty(EmptyOp op)
    {
        var fragment = Inst(InstructionOp.EmptyWidth);
        program.Inst.AsSpan()[(int)fragment.Index].Arg = (uint)op;
        fragment.Out = PatchList.Make(fragment.Index << 1);
        return fragment;
    }

    // compile.go:272. Rune headers retain their original syntax/global owners, with original execution specializations.
    private Fragment Rune(RegexSlice<int> runes, RegexFlags flags)
    {
        var fragment = Inst(InstructionOp.Rune);
        fragment.Nullable = false;
        ref var instruction = ref program.Inst.AsSpan()[(int)fragment.Index];
        instruction.Rune = runes;
        flags &= RegexFlags.FoldCase;
        if (runes.Length != 1 || UnicodeCase.SimpleFold(runes[0]) == runes[0])
            flags &= ~RegexFlags.FoldCase;
        instruction.Arg = (uint)flags;
        fragment.Out = PatchList.Make(fragment.Index << 1);
        if ((flags & RegexFlags.FoldCase) == 0 && (runes.Length == 1 || runes.Length == 2 && runes[0] == runes[1]))
            instruction.Op = InstructionOp.Rune1;
        else if (runes.Length == 2 && runes[0] == 0 && runes[1] == 0x10FFFF)
            instruction.Op = InstructionOp.RuneAny;
        else if (runes.Length == 4 && runes[0] == 0 && runes[1] == '\n' - 1 && runes[2] == '\n' + 1 && runes[3] == 0x10FFFF)
            instruction.Op = InstructionOp.RuneAnyNotNL;
        return fragment;
    }
}
