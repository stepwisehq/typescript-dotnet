// Copyright 2015 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/backtrack.go complete selected string-matching closure.
using System.Collections.Concurrent;
using TypeScript.Scanner;
using TypeScript.RegularExpressions.Syntax;

namespace TypeScript.RegularExpressions;
internal struct BacktrackJob
{
    internal uint pc;
    internal bool arg;
    internal int pos;
}

internal sealed class BitState
{
    internal int end;
    internal Slice<int> cap, matchcap;
    internal Slice<BacktrackJob> jobs;
    internal Slice<uint> visited;
    internal RegexInputString input;
    // backtrack.go:80. All original capacities and required clearing survive reuse.
    internal void Reset(RegexProgram program, int end, int captureCount)
    {
        this.end = end;
        jobs = jobs.Capacity == 0 ? Slice<BacktrackJob>.Make(0, 256) : jobs.Slice2(0, 0);
        var visitedSize = (program.Inst.Length * (end + 1) + 31) / 32;
        if (visited.Capacity < visitedSize)
            visited = Slice<uint>.Make(visitedSize, 256 * 1024 / 32);
        else
        {
            visited = visited.Slice2(0, visitedSize);
            visited.AsSpan().Clear();
        }

        cap = cap.Capacity < captureCount ? Slice<int>.Make(captureCount) : cap.Slice2(0, captureCount);
        cap.AsSpan().Fill(-1);
        matchcap = matchcap.Capacity < captureCount ? Slice<int>.Make(captureCount) : matchcap.Slice2(0, captureCount);
        matchcap.AsSpan().Fill(-1);
    }

    // backtrack.go:118.
    internal bool ShouldVisit(uint pc, int position)
    {
        var bit = (uint)((int)pc * (end + 1) + position);
        var mask = 1u << (int)(bit & 31);
        if ((visited[(int)(bit / 32)] & mask) != 0)
            return false;
        visited[(int)(bit / 32)] |= mask;
        return true;
    }

    // backtrack.go:130.
    internal void Push(GoRegexp regex, uint pc, int position, bool argument)
    {
        if (regex.prog.Inst[(int)pc].Op != InstructionOp.Fail && (argument || ShouldVisit(pc, position)))
            jobs = jobs.Append(new BacktrackJob { pc = pc, arg = argument, pos = position });
    }
}

internal sealed partial class GoRegexp
{
    private const int maxBacktrackProgram = 500, maxBacktrackVector = 256 * 1024;
    private static readonly ConcurrentBag<BitState> bitStatePool = new();
    private static BitState NewBitState() => bitStatePool.TryTake(out var state) ? state : new BitState();
    private static void FreeBitState(BitState state)
    {
        state.input.Str = ByteStringValue.FromString("");
        bitStatePool.Add(state);
    }

    private static int MaxBitStateLength(RegexProgram program) => ShouldBacktrack(program) ? maxBacktrackVector / program.Inst.Length : 0;
    private static bool ShouldBacktrack(RegexProgram program) => program.Inst.Length <= maxBacktrackProgram;
    // backtrack.go:139. The original tail labels avoid unnecessary job pushes.
    private bool TryBacktrack(BitState state, ref RegexInputString input, uint startPc, int startPosition)
    {
        var longest = this.longest;
        state.Push(this, startPc, startPosition, false);
        while (state.jobs.Length > 0)
        {
            var last = state.jobs.Length - 1;
            var pc = state.jobs[last].pc;
            var position = state.jobs[last].pos;
            var argument = state.jobs[last].arg;
            state.jobs = state.jobs.Slice2(0, last);
            goto Skip;
            CheckAndLoop:
                if (!state.ShouldVisit(pc, position))
                    continue;
            Skip:
                ref var instruction = ref prog.Inst.AsSpan()[(int)pc];
            switch (instruction.Op)
            {
                default:
                    throw new InvalidOperationException("bad inst");
                case InstructionOp.Fail:
                    throw new InvalidOperationException("unexpected InstFail");
                case InstructionOp.Alt:
                    if (argument)
                    {
                        argument = false;
                        pc = instruction.Arg;
                        goto CheckAndLoop;
                    }
                    else
                    {
                        state.Push(this, pc, position, true);
                        pc = instruction.Out;
                        goto CheckAndLoop;
                    }

                case InstructionOp.AltMatch:
                    switch (prog.Inst[(int)instruction.Out].Op)
                    {
                        case InstructionOp.Rune:
                        case InstructionOp.Rune1:
                        case InstructionOp.RuneAny:
                        case InstructionOp.RuneAnyNotNL:
                            state.Push(this, instruction.Arg, position, false);
                            pc = instruction.Arg;
                            position = state.end;
                            goto CheckAndLoop;
                    }

                    state.Push(this, instruction.Out, state.end, false);
                    pc = instruction.Out;
                    goto CheckAndLoop;
                case InstructionOp.Rune:
                {
                    var(rune, width) = input.Step(position);
                    if (!instruction.MatchRune(rune))
                        continue;
                    position += width;
                    pc = instruction.Out;
                    goto CheckAndLoop;
                }

                case InstructionOp.Rune1:
                {
                    var(rune, width) = input.Step(position);
                    if (rune != instruction.Rune[0])
                        continue;
                    position += width;
                    pc = instruction.Out;
                    goto CheckAndLoop;
                }

                case InstructionOp.RuneAnyNotNL:
                {
                    var(rune, width) = input.Step(position);
                    if (rune == '\n' || rune == -1)
                        continue;
                    position += width;
                    pc = instruction.Out;
                    goto CheckAndLoop;
                }

                case InstructionOp.RuneAny:
                {
                    var(rune, width) = input.Step(position);
                    if (rune == -1)
                        continue;
                    position += width;
                    pc = instruction.Out;
                    goto CheckAndLoop;
                }

                case InstructionOp.Capture:
                    if (argument)
                    {
                        state.cap[(int)instruction.Arg] = position;
                        continue;
                    }
                    else
                    {
                        if (instruction.Arg < (uint)state.cap.Length)
                        {
                            state.Push(this, pc, state.cap[(int)instruction.Arg], true);
                            state.cap[(int)instruction.Arg] = position;
                        }

                        pc = instruction.Out;
                        goto CheckAndLoop;
                    }

                case InstructionOp.EmptyWidth:
                    var flag = input.Context(position);
                    if (!flag.Match((EmptyOp)instruction.Arg))
                        continue;
                    pc = instruction.Out;
                    goto CheckAndLoop;
                case InstructionOp.Nop:
                    pc = instruction.Out;
                    goto CheckAndLoop;
                case InstructionOp.Match:
                    if (state.cap.Length == 0)
                        return true;
                    if (state.cap.Length > 1)
                        state.cap[1] = position;
                    var old = state.matchcap[1];
                    if (old == -1 || longest && position > 0 && position > old)
                        state.matchcap.CopyFrom(state.cap);
                    if (!longest)
                        return true;
                    if (position == state.end)
                        return true;
                    continue;
            }
        }

        return longest && state.matchcap.Length > 1 && state.matchcap[1] >= 0;
    }

    // backtrack.go:305. Visited state is intentionally shared across unanchored starts.
    private Slice<int> Backtrack(ByteStringValue text, int position, int captureCount, Slice<int> destination)
    {
        var startCondition = cond;
        if (startCondition == unchecked((EmptyOp)byte.MaxValue))
            return default;
        if ((startCondition & EmptyOp.BeginText) != 0 && position != 0)
            return default;
        var state = NewBitState();
        state.input.Str = text;
        ref var input = ref state.input;
        var end = text.Length;
        state.Reset(prog, end, captureCount);
        if ((startCondition & EmptyOp.BeginText) != 0)
        {
            if (state.cap.Length > 0)
                state.cap[0] = position;
            if (!TryBacktrack(state, ref input, (uint)prog.Start, position))
            {
                FreeBitState(state);
                return default;
            }
        }
        else
        {
            var width = -1;
            for (; position <= end && width != 0; position += width)
            {
                if (prefix.Length > 0)
                {
                    var advance = input.Index(this, position);
                    if (advance < 0)
                    {
                        FreeBitState(state);
                        return default;
                    }

                    position += advance;
                }

                if (state.cap.Length > 0)
                    state.cap[0] = position;
                if (TryBacktrack(state, ref input, (uint)prog.Start, position))
                    goto Match;
                (_, width) = input.Step(position);
            }

            FreeBitState(state);
            return default;
        }

        Match:
            destination = destination.Append(state.matchcap);
        FreeBitState(state);
        return destination;
    }
}
