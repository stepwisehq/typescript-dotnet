// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license in licenses/Go-LICENSE.txt.
// Modified C# port of Go go1.27.1 src/slices/sort.go; required SortFunc, SortStableFunc and their helpers only.
// Borrowed Span/Comparison and bit-length adaptation; all other slices APIs excluded.
#nullable disable
namespace TypeScript.Scanner;
internal static partial class GoSliceSort
{
    public static void SortFunc<E>(List<E> x, Comparison<E> cmp)
        => SortFunc(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(x), cmp);

    public static void SortFunc<E>(Span<E> x, Comparison<E> cmp)
    {
        var n = x.Length;
        pdqsortCmpFunc(x, 0, n, bitLength((ulong)n), cmp);
    }

    // slices/sort.go:37. Stable ordering in the same borrowed storage.
    public static void SortStableFunc<E>(Span<E> x, Comparison<E> cmp)
        => stableCmpFunc(x, x.Length, cmp);

    private enum sortedHint
    {
        unknownHint,
        increasingHint,
        decreasingHint
    }

    private struct XorShift
    {
        private ulong value;
        internal XorShift(ulong value)
        {
            this.value = value;
        }

        public ulong Next()
        {
            unchecked
            {
                value ^= value << 13;
                value ^= value >> 7;
                value ^= value << 17;
            }

            return value;
        }
    }

    private static ulong nextPowerOfTwo(int length) => 1UL << bitLength((ulong)length);
    private static int bitLength(ulong value) => 64 - System.Numerics.BitOperations.LeadingZeroCount(value);
}
