// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license in licenses/Go-LICENSE.txt.
// Modified C# port of Go go1.27.1 src/slices/sort.go; required SortFunc and pivot helpers only.
// Managed List/Comparison and bit-length adaptation; all other slices APIs excluded.
#nullable disable
namespace TypeScript.Scanner;
internal static partial class GoSliceSort
{
    public static void SortFunc<E>(List<E> x, Comparison<E> cmp)
    {
        var n = x?.Count ?? 0;
        pdqsortCmpFunc(x, 0, n, bitLength((ulong)n), cmp);
    }

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
