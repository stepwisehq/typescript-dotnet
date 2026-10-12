// Copyright 2010 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/regexp.go: selected Compile/MatchString closure and real engine selection.
using System.Collections.Concurrent;
using TypeScript.Scanner;
using TypeScript.RegularExpressions.Syntax;

namespace TypeScript.RegularExpressions;
internal sealed partial class GoRegexp
{
    internal ByteStringValue expr;
    internal RegexProgram prog = null!;
    internal OnePassProgram? onepass;
    internal int numSubexp, maxBitStateLen;
    internal Slice<ByteStringValue> subexpNames;
    internal ByteStringValue prefix;
    internal Slice<byte> prefixBytes;
    internal int prefixRune;
    internal uint prefixEnd;
    internal int mpool, matchcap;
    internal bool prefixComplete;
    internal EmptyOp cond;
    internal int minInputLen;
    internal bool longest;
    // regexp.go:130.
    internal static (GoRegexp? Regex, RegexParseError? Error) Compile(ByteStringValue expression) => Compile(expression, RegexFlags.Perl, false);
    // regexp.go:167.
    internal static (GoRegexp? Regex, RegexParseError? Error) Compile(ByteStringValue expression, RegexFlags mode, bool longest)
    {
        var(tree, error) = RegexParser.Parse(expression, mode);
        if (error != null)
            return (null, error);
        var maxCapture = tree!.MaxCap();
        var captureNames = tree.CapNames();
        tree = RegexNode.Simplify(tree)!;
        var program = RegexCompiler.Compile(tree);
        var captureCount = Math.Max(program.NumCap, 2);
        var regex = new GoRegexp
        {
            expr = expression,
            prog = program,
            onepass = CompileOnePass(program),
            numSubexp = maxCapture,
            subexpNames = captureNames,
            cond = program.StartCond(),
            longest = longest,
            matchcap = captureCount,
            minInputLen = MinInputLength(tree)
        };
        if (regex.onepass == null)
        {
            (regex.prefix, regex.prefixComplete) = program.Prefix();
            regex.maxBitStateLen = MaxBitStateLength(program);
        }
        else
            (regex.prefix, regex.prefixComplete, regex.prefixEnd) = OnePassPrefix(program);
        if (regex.prefix.Length != 0)
        {
            // Original regexp.go:206 []byte(prefix) owns an independent copy.
            regex.prefixBytes = Slice<byte>.Make(regex.prefix.Length);
            if (regex.prefix.IsBytes)
                regex.prefix.ByteSpan.CopyTo(regex.prefixBytes.AsSpan());
            else
                for (var index = 0; index < regex.prefix.Length; index++)
                    regex.prefixBytes[index] = (byte)regex.prefix[index];
            regex.prefixRune = RegexInputString.DecodeRune(regex.prefix).Rune;
        }

        var instructionCount = program.Inst.Length;
        var poolIndex = 0;
        while (matchSize[poolIndex] != 0 && matchSize[poolIndex] < instructionCount)
            poolIndex++;
        regex.mpool = poolIndex;
        return (regex, null);
    }

    // regexp.go:226. Standard concurrent pools retain the original ownership classes;
    // runtime-specific GC eviction behavior is not an observable regexp guarantee.
    private static readonly int[] matchSize = [128, 512, 2048, 16384, 0];
    private static readonly ConcurrentBag<RegexMachine>[] matchPool = [new(), new(), new(), new(), new()];
    // regexp.go:230.
    private RegexMachine GetMachine()
    {
        if (!matchPool[mpool].TryTake(out var machine))
            machine = new RegexMachine();
        machine.re = this;
        machine.p = prog;
        if (machine.matchcap.Capacity < matchcap)
        {
            machine.matchcap = Slice<int>.Make(matchcap);
            foreach (var thread in machine.pool)
                thread.cap = Slice<int>.Make(matchcap);
        }

        var length = matchSize[mpool];
        if (length == 0)
            length = prog.Inst.Length;
        if (machine.q0.sparse.Length < length)
        {
            machine.q0 = new NfaQueue
            {
                sparse = Slice<uint>.Make(length),
                dense = Slice<NfaEntry>.Make(0, length)
            };
            machine.q1 = new NfaQueue
            {
                sparse = Slice<uint>.Make(length),
                dense = Slice<NfaEntry>.Make(0, length)
            };
        }

        return machine;
    }

    // regexp.go:260.
    private void PutMachine(RegexMachine machine)
    {
        machine.re = null;
        machine.p = null;
        machine.input.Str = ByteStringValue.FromString("");
        matchPool[mpool].Add(machine);
    }

    // regexp.go:270.
    internal static int MinInputLength(RegexNode tree)
    {
        switch (tree.Op)
        {
            default:
                return 0;
            case RegexOp.AnyChar:
            case RegexOp.AnyCharNotNL:
            case RegexOp.CharClass:
                return 1;
            case RegexOp.Literal:
                var length = 0;
                foreach (var rune in tree.Rune)
                    length += rune == Utf8.RuneError ? 1 : Utf8.RuneLen(rune);
                return length;
            case RegexOp.Capture:
            case RegexOp.Plus:
                return MinInputLength(tree.Sub[0]!);
            case RegexOp.Repeat:
                return tree.Min * MinInputLength(tree.Sub[0]!);
            case RegexOp.Concat:
                var concatenatedLength = 0;
                foreach (var child in tree.Sub)
                    concatenatedLength += MinInputLength(child!);
                return concatenatedLength;
            case RegexOp.Alternate:
                var alternateLength = MinInputLength(tree.Sub[0]!);
                foreach (var child in tree.Sub.Slice2(1))
                    alternateLength = Math.Min(alternateLength, MinInputLength(child!));
                return alternateLength;
        }
    }

    // regexp.go:527/exec.go:514. Only the public string match operation is selected.
    internal bool MatchString(ByteStringValue input) => !Find(input, 0, 0, default).IsNil;
    // exec.go:524. Empty-but-nonnil destination is a successful zero-capture sentinel.
    private Slice<int> Find(ByteStringValue input, int position, int captures, Slice<int> destination)
    {
        if (destination.IsNil)
            destination = Slice<int>.FromArray(Array.Empty<int>()).Slice3(0, 0, 0);
        if (input.Length < minInputLen)
            return default;
        if (onepass != null)
            return DoOnePass(input, position, captures, destination);
        if (input.Length < maxBitStateLen)
            return Backtrack(input, position, captures, destination);
        var machine = GetMachine();
        machine.input.Str = input;
        machine.Init(captures);
        if (!machine.Match(ref machine.input, position))
        {
            PutMachine(machine);
            return default;
        }

        destination = destination.Append(machine.matchcap);
        PutMachine(machine);
        return destination;
    }
}

// The selected MatchString closure keeps this value embedded in the machine/bit-state
// owner, as Go inputs.string. No interface boxing or standalone input owner is required.
internal struct RegexInputString
{
    internal ByteStringValue Str;
    internal readonly (int Rune, int Width) Step(int position)
    {
        if (position < Str.Length)
            return DecodeRune(Str.Slice(position));
        return (-1, 0);
    }

    internal readonly bool CanCheckPrefix() => true;
    internal readonly bool HasPrefix(GoRegexp regex) => Str.StartsWith(regex.prefix);
    internal readonly int Index(GoRegexp regex, int position) => Str.Slice(position).IndexOf(regex.prefix);
    internal readonly LazyFlag Context(int position)
    {
        var before = -1;
        var after = -1;
        if ((uint)(position - 1) < (uint)Str.Length)
        {
            var prefix = Str.Slice(0, position);
            before = prefix.IsBytes ? Utf8.DecodeLastRuneInString(prefix.ByteSpan).Rune : Utf8.DecodeLastRuneInString(prefix.CharSpan).Rune;
        }

        if ((uint)position < (uint)Str.Length)
            after = DecodeRune(Str.Slice(position)).Rune;
        return new LazyFlag(before, after);
    }

    internal static (int Rune, int Width) DecodeRune(ByteStringValue input) => input.IsBytes ? Utf8.DecodeRuneInString(input.ByteSpan) : Utf8.DecodeRuneInString(input.CharSpan);
}
