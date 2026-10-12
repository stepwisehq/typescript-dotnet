// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/exec.go selected NFA workers; queue and instruction pointers
// are ref headers/retained Memory arena locations, preserving original mutable owners.
using System.Collections.Concurrent;
using TypeScript.Scanner;
using TypeScript.RegularExpressions.Syntax;

namespace TypeScript.RegularExpressions;
internal sealed partial class RegexMachine
{
    // exec.go:104.
    internal void Init(int captureCount)
    {
        foreach (var thread in pool)
            thread.cap = thread.cap.Slice2(0, captureCount);
        matchcap = matchcap.Slice2(0, captureCount);
    }

    // exec.go:113.
    private NfaThread Allocate(Memory<Instruction> instruction)
    {
        NfaThread thread;
        var count = pool.Length;
        if (count > 0)
        {
            thread = pool[count - 1];
            pool = pool.Slice2(0, count - 1);
        }
        else
            thread = new NfaThread
            {
                cap = Slice<int>.Make(matchcap.Length, matchcap.Capacity)
            };
        thread.inst = instruction;
        return thread;
    }

    // exec.go:175.
    internal bool Match(ref RegexInputString input, int position)
    {
        var regex = re!;
        var program = p!;
        var startCondition = regex.cond;
        if (startCondition == unchecked((EmptyOp)byte.MaxValue))
            return false;
        matched = false;
        for (var index = 0; index < matchcap.Length; index++)
            matchcap[index] = -1;
        ref var run = ref q0;
        ref var next = ref q1;
        var(rune, width) = input.Step(position);
        var nextRune = -1;
        var nextWidth = 0;
        if (rune != -1)
            (nextRune, nextWidth) = input.Step(position + width);
        var flag = position == 0 ? new LazyFlag(-1, rune) : input.Context(position);
        while (true)
        {
            if (run.dense.Length == 0)
            {
                if ((startCondition & EmptyOp.BeginText) != 0 && position != 0)
                    break;
                if (matched)
                    break;
                if (regex.prefix.Length > 0 && nextRune != regex.prefixRune && input.CanCheckPrefix())
                {
                    var advance = input.Index(regex, position);
                    if (advance < 0)
                        break;
                    position += advance;
                    (rune, width) = input.Step(position);
                    (nextRune, nextWidth) = input.Step(position + width);
                }
            }

            if (!matched)
            {
                if (matchcap.Length > 0)
                    matchcap[0] = position;
                Add(ref run, (uint)program.Start, position, matchcap, in flag, null);
            }

            flag = new LazyFlag(rune, nextRune);
            Step(ref run, ref next, position, position + width, rune, in flag);
            if (width == 0)
                break;
            if (matchcap.Length == 0 && matched)
                break;
            position += width;
            (rune, width) = (nextRune, nextWidth);
            if (rune != -1)
                (nextRune, nextWidth) = input.Step(position + width);
            ref var previousRun = ref run;
            run = ref next;
            next = ref previousRun;
        }

        Clear(ref next);
        return matched;
    }

    // exec.go:244.
    private void Clear(ref NfaQueue queue)
    {
        foreach (var entry in queue.dense)
            if (entry.t != null)
                pool = pool.Append(entry.t);
        queue.dense = queue.dense.Slice2(0, 0);
    }

    // exec.go:260.
    private void Step(ref NfaQueue run, ref NfaQueue next, int position, int nextPosition, int rune, in LazyFlag nextCondition)
    {
        var longest = re!.longest;
        for (var index = 0; index < run.dense.Length; index++)
        {
            ref var entry = ref run.dense.AsSpan()[index];
            var thread = entry.t;
            if (thread == null)
                continue;
            if (longest && matched && thread.cap.Length > 0 && matchcap[0] < thread.cap[0])
            {
                pool = pool.Append(thread);
                continue;
            }

            ref var instruction = ref thread.Instruction;
            var add = false;
            switch (instruction.Op)
            {
                default:
                    throw new InvalidOperationException("bad inst");
                case InstructionOp.Match:
                    if (thread.cap.Length > 0 && (!longest || !matched || matchcap[1] < position))
                    {
                        thread.cap[1] = position;
                        matchcap.CopyFrom(thread.cap);
                    }

                    if (!longest)
                    {
                        foreach (var remaining in run.dense.Slice2(index + 1))
                            if (remaining.t != null)
                                pool = pool.Append(remaining.t);
                        run.dense = run.dense.Slice2(0, 0);
                    }

                    matched = true;
                    break;
                case InstructionOp.Rune:
                    add = instruction.MatchRune(rune);
                    break;
                case InstructionOp.Rune1:
                    add = rune == instruction.Rune[0];
                    break;
                case InstructionOp.RuneAny:
                    add = true;
                    break;
                case InstructionOp.RuneAnyNotNL:
                    add = rune != '\n';
                    break;
            }

            if (add)
                thread = Add(ref next, instruction.Out, nextPosition, thread.cap, in nextCondition, thread);
            if (thread != null)
                pool = pool.Append(thread);
        }

        run.dense = run.dense.Slice2(0, 0);
    }

    // exec.go:318. Again is the original tail-recursion label. Dense capacity was
    // reserved for the full program by get(), so recursive Add retains entry refs.
    private NfaThread? Add(ref NfaQueue queue, uint pc, int position, Slice<int> captures, in LazyFlag condition, NfaThread? thread)
    {
        Again:
            if (pc == 0)
                return thread;
        var sparseIndex = queue.sparse[(int)pc];
        if (sparseIndex < (uint)queue.dense.Length && queue.dense[(int)sparseIndex].pc == pc)
            return thread;
        var index = queue.dense.Length;
        queue.dense = queue.dense.Slice2(0, index + 1);
        ref var entry = ref queue.dense.AsSpan()[index];
        entry.t = null;
        entry.pc = pc;
        queue.sparse[(int)pc] = (uint)index;
        var instructionMemory = p!.Inst.AsWritableMemory().Slice((int)pc, 1);
        ref var instruction = ref instructionMemory.Span[0];
        switch (instruction.Op)
        {
            default:
                throw new InvalidOperationException("unhandled");
            case InstructionOp.Fail:
                break;
            case InstructionOp.Alt:
            case InstructionOp.AltMatch:
                thread = Add(ref queue, instruction.Out, position, captures, in condition, thread);
                pc = instruction.Arg;
                goto Again;
            case InstructionOp.EmptyWidth:
                if (condition.Match((EmptyOp)instruction.Arg))
                {
                    pc = instruction.Out;
                    goto Again;
                }

                break;
            case InstructionOp.Nop:
                pc = instruction.Out;
                goto Again;
            case InstructionOp.Capture:
                if ((int)instruction.Arg < captures.Length)
                {
                    var previousPosition = captures[(int)instruction.Arg];
                    captures[(int)instruction.Arg] = position;
                    Add(ref queue, instruction.Out, position, captures, in condition, null);
                    captures[(int)instruction.Arg] = previousPosition;
                }
                else
                {
                    pc = instruction.Out;
                    goto Again;
                }

                break;
            case InstructionOp.Match:
            case InstructionOp.Rune:
            case InstructionOp.Rune1:
            case InstructionOp.RuneAny:
            case InstructionOp.RuneAnyNotNL:
                if (thread == null)
                    thread = Allocate(instructionMemory);
                else
                    thread.inst = instructionMemory;
                if (captures.Length > 0 && !thread.cap.SameStorageRange(captures))
                    thread.cap.CopyFrom(captures);
                entry.t = thread;
                thread = null;
                break;
        }

        return thread;
    }
}

internal struct NfaQueue
{
    internal Slice<uint> sparse;
    internal Slice<NfaEntry> dense;
}

internal struct NfaEntry
{
    internal uint pc;
    internal NfaThread? t;
}

internal sealed class NfaThread
{
    // Go *syntax.Inst retains an arena location. A single-slot Memory header has
    // the same owner lifetime and observes the same instruction, without a box.
    internal Memory<Instruction> inst;
    internal ref Instruction Instruction => ref inst.Span[0];

    internal Slice<int> cap;
}

internal sealed partial class RegexMachine
{
    internal GoRegexp? re;
    internal RegexProgram? p;
    internal NfaQueue q0, q1;
    internal Slice<NfaThread> pool;
    internal bool matched;
    internal Slice<int> matchcap;
    internal RegexInputString input;
// Queue workers receive ref headers. Swapping run/next uses ref reassignment,
// preserving the original pointer swap rather than copying embedded queue state.
}

internal sealed class OnePassMachine
{
    internal RegexInputString input;
    internal Slice<int> matchcap;
}

internal sealed partial class GoRegexp
{
    private static readonly ConcurrentBag<OnePassMachine> onePassPool = new();
    private static OnePassMachine NewOnePassMachine() => onePassPool.TryTake(out var machine) ? machine : new OnePassMachine();
    private static void FreeOnePassMachine(OnePassMachine machine)
    {
        machine.input.Str = ByteStringValue.FromString("");
        onePassPool.Add(machine);
    }

    private Slice<int> DoOnePass(ByteStringValue text, int position, int captureCount, Slice<int> destination)
    {
        var startCondition = cond;
        if (startCondition == unchecked((EmptyOp)byte.MaxValue))
            return default;
        var machine = NewOnePassMachine();
        machine.matchcap = machine.matchcap.Capacity < captureCount ? Slice<int>.Make(captureCount) : machine.matchcap.Slice2(0, captureCount);
        var matched = false;
        for (var index = 0; index < machine.matchcap.Length; index++)
            machine.matchcap[index] = -1;
        machine.input.Str = text;
        ref var input = ref machine.input;
        var(rune, width) = input.Step(position);
        var nextRune = -1;
        var nextWidth = 0;
        if (rune != -1)
            (nextRune, nextWidth) = input.Step(position + width);
        var flag = position == 0 ? new LazyFlag(-1, rune) : input.Context(position);
        var program = onepass!;
        var pc = program.Start;
        ref var target = ref program.Inst.AsSpan()[pc];
        if (position == 0 && flag.Match((EmptyOp)target.Inst.Arg) && prefix.Length > 0 && input.CanCheckPrefix())
        {
            if (!input.HasPrefix(this))
                goto Return;
            position += prefix.Length;
            (rune, width) = input.Step(position);
            (nextRune, nextWidth) = input.Step(position + width);
            flag = input.Context(position);
            pc = (int)prefixEnd;
        }

        while (true)
        {
            target = ref program.Inst.AsSpan()[pc];
            ref var instruction = ref target.Inst;
            pc = (int)instruction.Out;
            switch (instruction.Op)
            {
                default:
                    throw new InvalidOperationException("bad inst");
                case InstructionOp.Match:
                    matched = true;
                    if (machine.matchcap.Length > 0)
                    {
                        machine.matchcap[0] = 0;
                        machine.matchcap[1] = position;
                    }

                    goto Return;
                case InstructionOp.Rune:
                    if (!instruction.MatchRune(rune))
                        goto Return;
                    break;
                case InstructionOp.Rune1:
                    if (rune != instruction.Rune[0])
                        goto Return;
                    break;
                case InstructionOp.RuneAny:
                    break;
                case InstructionOp.RuneAnyNotNL:
                    if (rune == '\n')
                        goto Return;
                    break;
                case InstructionOp.Alt:
                case InstructionOp.AltMatch:
                    pc = (int)OnePassNext(in target, rune);
                    continue;
                case InstructionOp.Fail:
                    goto Return;
                case InstructionOp.Nop:
                    continue;
                case InstructionOp.EmptyWidth:
                    if (!flag.Match((EmptyOp)instruction.Arg))
                        goto Return;
                    continue;
                case InstructionOp.Capture:
                    if ((int)instruction.Arg < machine.matchcap.Length)
                        machine.matchcap[(int)instruction.Arg] = position;
                    continue;
            }

            if (width == 0)
                break;
            flag = new LazyFlag(rune, nextRune);
            position += width;
            (rune, width) = (nextRune, nextWidth);
            if (rune != -1)
                (nextRune, nextWidth) = input.Step(position + width);
        }

        Return:
            if (!matched)
            {
                FreeOnePassMachine(machine);
                return default;
            }

        destination = destination.Append(machine.matchcap);
        FreeOnePassMachine(machine);
        return destination;
    }
}
