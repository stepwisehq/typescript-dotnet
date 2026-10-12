// Copyright 2014 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/onepass.go complete selected one-pass analysis/copy/dispatch closure.
using TypeScript.Scanner;
using TypeScript.RegularExpressions.Syntax;

namespace TypeScript.RegularExpressions;
internal sealed class OnePassProgram
{
    internal Slice<OnePassInstruction> Inst;
    internal int Start, NumCap;
}

// Original embedding is a value field; the copied instruction retains its rune header.
internal struct OnePassInstruction
{
    internal Instruction Inst;
    internal Slice<uint> Next;
}

// onepass.go:95-140. These queues are actual heap owners returned by newQueue.
internal sealed class OnePassQueue
{
    private readonly Slice<uint> sparse, dense;
    private uint size, nextIndex;
    internal OnePassQueue(int length)
    {
        sparse = Slice<uint>.Make(length);
        dense = Slice<uint>.Make(length);
    }

    internal bool Empty() => nextIndex >= size;
    internal uint Next()
    {
        var result = dense[(int)nextIndex];
        nextIndex++;
        return result;
    }

    internal void Clear()
    {
        size = 0;
        nextIndex = 0;
    }

    internal bool Contains(uint value) => value < (uint)sparse.Length && sparse[(int)value] < size && dense[(int)sparse[(int)value]] == value;
    internal void Insert(uint value)
    {
        if (!Contains(value))
            InsertNew(value);
    }

    private void InsertNew(uint value)
    {
        if (value >= (uint)sparse.Length)
            return;
        sparse[(int)value] = size;
        dense[(int)size] = value;
        size++;
    }
}

internal sealed partial class GoRegexp
{
    // onepass.go:41. References remain in the original compiled instruction owner.
    private static (ByteStringValue Prefix, bool Complete, uint Pc) OnePassPrefix(RegexProgram program)
    {
        ref var instruction = ref program.Inst.AsSpan()[program.Start];
        if (instruction.Op != InstructionOp.EmptyWidth || ((EmptyOp)instruction.Arg & EmptyOp.BeginText) == 0)
            return (ByteStringValue.FromString(""), instruction.Op == InstructionOp.Match, (uint)program.Start);
        var pc = instruction.Out;
        instruction = ref program.Inst.AsSpan()[(int)pc];
        while (instruction.Op == InstructionOp.Nop)
        {
            pc = instruction.Out;
            instruction = ref program.Inst.AsSpan()[(int)pc];
        }

        if (instruction.RuneOp() != InstructionOp.Rune || instruction.Rune.Length != 1)
            return (ByteStringValue.FromString(""), instruction.Op == InstructionOp.Match, (uint)program.Start);
        var buffer = new GoBuilder();
        while (instruction.RuneOp() == InstructionOp.Rune && instruction.Rune.Length == 1 && ((RegexFlags)instruction.Arg & RegexFlags.FoldCase) == 0 && instruction.Rune[0] != Utf8.RuneError)
        {
            buffer.WriteRune(instruction.Rune[0]);
            pc = instruction.Out;
            instruction = ref program.Inst.AsSpan()[(int)instruction.Out];
        }

        var complete = instruction.Op == InstructionOp.EmptyWidth && ((EmptyOp)instruction.Arg & EmptyOp.EndText) != 0 && program.Inst[(int)instruction.Out].Op == InstructionOp.Match;
        return (buffer.Value(), complete, pc);
    }

    // onepass.go:75.
    private static uint OnePassNext(in OnePassInstruction instruction, int rune)
    {
        var next = instruction.Inst.MatchRunePos(rune);
        if (next >= 0)
            return instruction.Next[next];
        return instruction.Inst.Op == InstructionOp.AltMatch ? instruction.Inst.Out : 0;
    }

    // onepass.go:215. Drop only the original working owners/shortcut replacements.
    private static void CleanupOnePass(OnePassProgram program, RegexProgram original)
    {
        for (var index = 0; index < original.Inst.Length; index++)
        {
            var instruction = original.Inst[index];
            ref var target = ref program.Inst.AsSpan()[index];
            switch (instruction.Op)
            {
                case InstructionOp.Alt:
                case InstructionOp.AltMatch:
                case InstructionOp.Rune:
                    break;
                case InstructionOp.Capture:
                case InstructionOp.EmptyWidth:
                case InstructionOp.Nop:
                case InstructionOp.Match:
                case InstructionOp.Fail:
                    target.Next = default;
                    break;
                case InstructionOp.Rune1:
                case InstructionOp.RuneAny:
                case InstructionOp.RuneAnyNotNL:
                    target.Next = default;
                    target = new OnePassInstruction
                    {
                        Inst = instruction
                    };
                    break;
            }
        }
    }

    // onepass.go:230. Required new instruction arena; embedded rune headers are copied.
    private static OnePassProgram OnePassCopy(RegexProgram original)
    {
        var program = new OnePassProgram
        {
            Start = original.Start,
            NumCap = original.NumCap,
            Inst = Slice<OnePassInstruction>.Make(original.Inst.Length)
        };
        for (var index = 0; index < original.Inst.Length; index++)
            program.Inst[index] = new OnePassInstruction
            {
                Inst = original.Inst[index]
            };
        for (var pc = 0; pc < program.Inst.Length; pc++)
        {
            if (program.Inst[pc].Inst.Op is not (InstructionOp.Alt or InstructionOp.AltMatch))
                continue;
            ref var otherA = ref program.Inst.AsSpan()[pc].Inst.Out;
            ref var altA = ref program.Inst.AsSpan()[pc].Inst.Arg;
            var alternate = program.Inst[(int)altA].Inst;
            if (alternate.Op is not (InstructionOp.Alt or InstructionOp.AltMatch))
            {
                ref var temporary = ref altA;
                altA = ref otherA;
                otherA = ref temporary;
                alternate = program.Inst[(int)altA].Inst;
                if (alternate.Op is not (InstructionOp.Alt or InstructionOp.AltMatch))
                    continue;
            }

            var other = program.Inst[(int)otherA].Inst;
            if (other.Op is InstructionOp.Alt or InstructionOp.AltMatch)
                continue;
            ref var altB = ref program.Inst.AsSpan()[(int)altA].Inst.Out;
            ref var otherB = ref program.Inst.AsSpan()[(int)altA].Inst.Arg;
            var patch = false;
            if (alternate.Out == (uint)pc)
                patch = true;
            else if (alternate.Arg == (uint)pc)
            {
                patch = true;
                ref var temporary = ref altB;
                altB = ref otherB;
                otherB = ref temporary;
            }

            if (patch)
                altB = otherA;
            if (otherA == altB)
                altA = otherB;
        }

        return program;
    }

    private const uint mergeFailed = uint.MaxValue;
    private static readonly RegexSlice<int> noRune = RegexSlice<int>.FromArray([]);
    private static readonly Slice<uint> noNext = Slice<uint>.FromArray([mergeFailed]);
    // onepass.go:158. Rune owners are read by borrowed header; only the merged result
    // and next-IP arrays are newly owned, with the original append detach behavior.
    internal static (RegexSlice<int> Runes, Slice<uint> Next) MergeRuneSets(RegexSlice<int> leftRunes, RegexSlice<int> rightRunes, uint leftPc, uint rightPc)
    {
        var leftLength = leftRunes.Length;
        var rightLength = rightRunes.Length;
        if ((leftLength & 1) != 0 || (rightLength & 1) != 0)
            throw new InvalidOperationException("mergeRuneSets odd length []rune");
        var left = 0;
        var right = 0;
        var merged = RegexSlice<int>.FromArray([]);
        var next = Slice<uint>.Make(0);
        var ok = true;
        var index = -1;
        // Direct local invocation preserves the original extend closure boundary;
        // no delegate conversion or per-range closure owner is introduced.
        bool Extend(ref int low, RegexSlice<int> array, uint pc)
        {
            if (index > 0 && array[low] <= merged[index])
                return false;
            merged = merged.Append(array[low], array[low + 1]);
            low += 2;
            index += 2;
            next = next.Append(pc);
            return true;
        }

        try
        {
            while (left < leftLength || right < rightLength)
            {
                if (right >= rightLength)
                    ok = Extend(ref left, leftRunes, leftPc);
                else if (left >= leftLength)
                    ok = Extend(ref right, rightRunes, rightPc);
                else if (rightRunes[right] < leftRunes[left])
                    ok = Extend(ref right, rightRunes, rightPc);
                else
                    ok = Extend(ref left, leftRunes, leftPc);
                if (!ok)
                    return (noRune, noNext);
            }

            return (merged, next);
        }
        finally
        {
            if (!ok)
            {
                merged = default;
                next = default;
            }
        }
    }

    private static readonly RegexSlice<int> anyRuneNotNl = RegexSlice<int>.FromArray([0, '\n' - 1, '\n' + 1, 0x10FFFF]);
    private static readonly RegexSlice<int> anyRune = RegexSlice<int>.FromArray([0, 0x10FFFF]);
    // onepass.go:293. The original recursive check keeps one set of queues/runelists
    // per analysis invocation. Direct local recursion does not allocate per-node callbacks.
    private static OnePassProgram? MakeOnePass(OnePassProgram program)
    {
        if (program.Inst.Length >= 1000)
            return null;
        var instructionQueue = new OnePassQueue(program.Inst.Length);
        var visitQueue = new OnePassQueue(program.Inst.Length);
        var onePassRunes = Slice<RegexSlice<int>>.Make(program.Inst.Length);
        bool Check(uint pc, Slice<bool> matches)
        {
            var ok = true;
            ref var target = ref program.Inst.AsSpan()[(int)pc];
            ref var instruction = ref target.Inst;
            if (visitQueue.Contains(pc))
                return ok;
            visitQueue.Insert(pc);
            switch (instruction.Op)
            {
                case InstructionOp.Alt:
                case InstructionOp.AltMatch:
                    ok = Check(instruction.Out, matches) && Check(instruction.Arg, matches);
                    var matchOut = matches[(int)instruction.Out];
                    var matchArg = matches[(int)instruction.Arg];
                    if (matchOut && matchArg)
                    {
                        ok = false;
                        break;
                    }

                    if (matchArg)
                    {
                        (instruction.Out, instruction.Arg) = (instruction.Arg, instruction.Out);
                        (matchOut, matchArg) = (matchArg, matchOut);
                    }

                    if (matchOut)
                    {
                        matches[(int)pc] = true;
                        instruction.Op = InstructionOp.AltMatch;
                    }

                    (onePassRunes[(int)pc], target.Next) = MergeRuneSets(onePassRunes[(int)instruction.Out], onePassRunes[(int)instruction.Arg], instruction.Out, instruction.Arg);
                    if (target.Next.Length > 0 && target.Next[0] == mergeFailed)
                    {
                        ok = false;
                        break;
                    }

                    break;
                case InstructionOp.Capture:
                case InstructionOp.Nop:
                    ok = Check(instruction.Out, matches);
                    matches[(int)pc] = matches[(int)instruction.Out];
                    onePassRunes[(int)pc] = RegexSlice<int>.FromArray([]).Append(onePassRunes[(int)instruction.Out]);
                    target.Next = Slice<uint>.Make(onePassRunes[(int)pc].Length / 2 + 1);
                    for (var index = 0; index < target.Next.Length; index++)
                        target.Next[index] = instruction.Out;
                    break;
                case InstructionOp.EmptyWidth:
                    ok = Check(instruction.Out, matches);
                    matches[(int)pc] = matches[(int)instruction.Out];
                    onePassRunes[(int)pc] = RegexSlice<int>.FromArray([]).Append(onePassRunes[(int)instruction.Out]);
                    target.Next = Slice<uint>.Make(onePassRunes[(int)pc].Length / 2 + 1);
                    for (var index = 0; index < target.Next.Length; index++)
                        target.Next[index] = instruction.Out;
                    break;
                case InstructionOp.Match:
                case InstructionOp.Fail:
                    matches[(int)pc] = instruction.Op == InstructionOp.Match;
                    break;
                case InstructionOp.Rune:
                {
                    matches[(int)pc] = false;
                    if (target.Next.Length > 0)
                        break;
                    instructionQueue.Insert(instruction.Out);
                    if (instruction.Rune.Length == 0)
                    {
                        onePassRunes[(int)pc] = RegexSlice<int>.FromArray([]);
                        target.Next = Slice<uint>.FromArray([instruction.Out]);
                        break;
                    }

                    var runes = RegexSlice<int>.FromArray([]);
                    if (instruction.Rune.Length == 1 && ((RegexFlags)instruction.Arg & RegexFlags.FoldCase) != 0)
                    {
                        var first = instruction.Rune[0];
                        runes = runes.Append(first, first);
                        for (var fold = UnicodeCase.SimpleFold(first); fold != first; fold = UnicodeCase.SimpleFold(fold))
                            runes = runes.Append(fold, fold);
                        GoSliceSort.SortFunc(runes.AsSpan(), static (left, right) => left.CompareTo(right));
                    }
                    else
                        runes = runes.Append(instruction.Rune);
                    onePassRunes[(int)pc] = runes;
                    target.Next = Slice<uint>.Make(onePassRunes[(int)pc].Length / 2 + 1);
                    for (var index = 0; index < target.Next.Length; index++)
                        target.Next[index] = instruction.Out;
                    instruction.Op = InstructionOp.Rune;
                    break;
                }

                case InstructionOp.Rune1:
                {
                    matches[(int)pc] = false;
                    if (target.Next.Length > 0)
                        break;
                    instructionQueue.Insert(instruction.Out);
                    var runes = RegexSlice<int>.FromArray([]);
                    if (((RegexFlags)instruction.Arg & RegexFlags.FoldCase) != 0)
                    {
                        var first = instruction.Rune[0];
                        runes = runes.Append(first, first);
                        for (var fold = UnicodeCase.SimpleFold(first); fold != first; fold = UnicodeCase.SimpleFold(fold))
                            runes = runes.Append(fold, fold);
                        GoSliceSort.SortFunc(runes.AsSpan(), static (left, right) => left.CompareTo(right));
                    }
                    else
                        runes = runes.Append(instruction.Rune[0], instruction.Rune[0]);
                    onePassRunes[(int)pc] = runes;
                    target.Next = Slice<uint>.Make(onePassRunes[(int)pc].Length / 2 + 1);
                    for (var index = 0; index < target.Next.Length; index++)
                        target.Next[index] = instruction.Out;
                    instruction.Op = InstructionOp.Rune;
                    break;
                }

                case InstructionOp.RuneAny:
                    matches[(int)pc] = false;
                    if (target.Next.Length > 0)
                        break;
                    instructionQueue.Insert(instruction.Out);
                    onePassRunes[(int)pc] = RegexSlice<int>.FromArray([]).Append(anyRune);
                    target.Next = Slice<uint>.FromArray([instruction.Out]);
                    break;
                case InstructionOp.RuneAnyNotNL:
                    matches[(int)pc] = false;
                    if (target.Next.Length > 0)
                        break;
                    instructionQueue.Insert(instruction.Out);
                    onePassRunes[(int)pc] = RegexSlice<int>.FromArray([]).Append(anyRuneNotNl);
                    target.Next = Slice<uint>.Make(onePassRunes[(int)pc].Length / 2 + 1);
                    for (var index = 0; index < target.Next.Length; index++)
                        target.Next[index] = instruction.Out;
                    break;
            }

            return ok;
        }

        instructionQueue.Clear();
        instructionQueue.Insert((uint)program.Start);
        var matches = Slice<bool>.Make(program.Inst.Length);
        OnePassProgram? result = program;
        while (!instructionQueue.Empty())
        {
            visitQueue.Clear();
            var pc = instructionQueue.Next();
            if (!Check(pc, matches))
            {
                result = null;
                break;
            }
        }

        if (result != null)
            for (var index = 0; index < result.Inst.Length; index++)
                result.Inst.AsSpan()[index].Inst.Rune = onePassRunes[index];
        return result;
    }

    // onepass.go:458. Exact anchoring and alternate ambiguity gates.
    internal static OnePassProgram? CompileOnePass(RegexProgram original)
    {
        if (original.Start == 0)
            return null;
        if (original.Inst[original.Start].Op != InstructionOp.EmptyWidth || ((EmptyOp)original.Inst[original.Start].Arg & EmptyOp.BeginText) != EmptyOp.BeginText)
            return null;
        var hasAlternate = false;
        foreach (var instruction in original.Inst)
            if (instruction.Op is InstructionOp.Alt or InstructionOp.AltMatch)
            {
                hasAlternate = true;
                break;
            }

        foreach (var instruction in original.Inst)
        {
            var outOp = original.Inst[(int)instruction.Out].Op;
            switch (instruction.Op)
            {
                default:
                    if (outOp == InstructionOp.Match && hasAlternate)
                        return null;
                    break;
                case InstructionOp.Alt:
                case InstructionOp.AltMatch:
                    if (outOp == InstructionOp.Match || original.Inst[(int)instruction.Arg].Op == InstructionOp.Match)
                        return null;
                    break;
                case InstructionOp.EmptyWidth:
                    if (outOp == InstructionOp.Match)
                    {
                        if (((EmptyOp)instruction.Arg & EmptyOp.EndText) == EmptyOp.EndText)
                            continue;
                        return null;
                    }

                    break;
            }
        }

        var program = MakeOnePass(OnePassCopy(original));
        if (program != null)
            CleanupOnePass(program, original);
        return program;
    }
}
