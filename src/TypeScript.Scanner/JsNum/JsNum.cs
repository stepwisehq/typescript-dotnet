// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/jsnum/jsnum.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// CLR binary64 operations replace Go's float64 operators. Integer exponentiation
// retains the original exact-integer / 256-bit-precision rounding algorithm.
using System.Numerics;

namespace TypeScript.Scanner;
internal static partial class JsNum
{
    internal static readonly Number MaxSafeInteger = new(9007199254740991);
    internal static readonly Number MinSafeInteger = new(-9007199254740991);
    internal static Number NaN() => new(double.NaN);
    internal static Number Inf(int sign) => new(sign >= 0 ? double.PositiveInfinity : double.NegativeInfinity);
    private static bool isNonFinite(double x) => ((ulong)BitConverter.DoubleToInt64Bits(x) & 0x7FF0000000000000UL) == 0x7FF0000000000000UL;
    internal static readonly Number negativeZero = new(Math.CopySign(0, -1));
    public readonly partial record struct Number
    {
        public bool IsNaN() => double.IsNaN(Value);
        public bool IsInf() => double.IsInfinity(Value);
        private uint toUint32() => unchecked((uint)ToInt32());
        // jsnum.go:53. The cast is used only as an equality-tested fast path;
        // explicit modulo and unchecked narrowing preserve Go's low 32 bits.
        public int ToInt32()
        {
            double x = Value;
            int smi = unchecked((int)x);
            if ((double)smi == x)
                return smi;
            if (isNonFinite(x))
                return 0;
            x = Math.Truncate(x);
            x %= 4294967296d;
            return unchecked((int)(long)x);
        }

        private uint toShiftCount() => toUint32() & 31;
        public Number SignedRightShift(Number y) => new(ToInt32() >> (int)y.toShiftCount());
        public Number UnsignedRightShift(Number y) => new(toUint32() >> (int)y.toShiftCount());
        public Number LeftShift(Number y) => new(unchecked(ToInt32() << (int)y.toShiftCount()));
        public Number BitwiseNOT() => new(~ToInt32());
        public Number BitwiseOR(Number y) => new(ToInt32() | y.ToInt32());
        public Number BitwiseAND(Number y) => new(ToInt32() & y.ToInt32());
        public Number BitwiseXOR(Number y) => new(ToInt32() ^ y.ToInt32());
        private Number trunc() => new(Math.Truncate(Value));
        public Number Floor() => new(Math.Floor(Value));
        public Number Abs() => new(Math.Abs(Value));
        public Number Remainder(Number d)
        {
            if (IsNaN() || d.IsNaN())
                return NaN();
            if (IsInf())
                return NaN();
            if (d.IsInf())
                return this;
            if (d.Value == 0)
                return NaN();
            if (Value == 0)
                return this;
            return new(Value % d.Value);
        }

        public Number Exponentiate(Number exponent)
        {
            if ((Value == 1 || Value == -1) && exponent.IsInf())
                return NaN();
            if (Value == 1 && exponent.IsNaN())
                return NaN();
            double b = Value, e = exponent.Value;
            if (b >= long.MinValue && b <= (double)long.MaxValue && b == Math.Truncate(b) && e >= 0 && e <= (double)long.MaxValue && e == Math.Truncate(e) && !double.IsInfinity(e))
            {
                double magnitude = e * Math.Log2(Math.Abs(b));
                if (magnitude > 53 && magnitude <= Math.Log2(double.MaxValue))
                {
                    // This arm has |b| >= 2 and e <= 1024, so the original big.Int
                    // exponent is also representable by BigInteger.Pow's int API.
                    BigInteger ri = BigInteger.Pow(new BigInteger(unchecked((long)b)), (int)e);
                    bool negative = ri.Sign < 0;
                    BigInteger absolute = BigInteger.Abs(ri);
                    // big.Float.SetPrec(256).SetInt rounds nearest-even before
                    // Float64 rounds again. Keep that two-step boundary exactly.
                    long bits = absolute.GetBitLength();
                    if (bits > 256)
                    {
                        int shift = (int)(bits - 256);
                        BigInteger significant = absolute >> shift;
                        BigInteger discarded = absolute - (significant << shift);
                        BigInteger half = BigInteger.One << (shift - 1);
                        if (discarded > half || (discarded == half && !significant.IsEven))
                            significant++;
                        absolute = significant << shift;
                    }

                    return new(Math.CopySign(ToDouble(absolute), negative ? -1 : 1));
                }
            }

            return new(Math.Pow(b, e));
        }

        public static Number operator +(Number x) => x;
        public static Number operator -(Number x) => new(-x.Value);
        public static Number operator +(Number x, Number y) => new(x.Value + y.Value);
        public static Number operator -(Number x, Number y) => new(x.Value - y.Value);
        public static Number operator *(Number x, Number y) => new(x.Value * y.Value);
        public static Number operator /(Number x, Number y) => new(x.Value / y.Value);
        public static bool operator <(Number x, Number y) => x.Value < y.Value;
        public static bool operator>(Number x, Number y) => x.Value > y.Value;
        public static bool operator <=(Number x, Number y) => x.Value <= y.Value;
        public static bool operator >=(Number x, Number y) => x.Value >= y.Value;
        public static implicit operator Number(double value) => new(value);
        public static explicit operator double (Number value) => value.Value;
        // Go Number equality is float64 equality, including NaN != NaN.
        public bool Equals(Number other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => String();
    }
}
