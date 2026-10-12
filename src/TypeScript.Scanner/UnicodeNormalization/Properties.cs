// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned golang.org/x/text v0.42.0 unicode/norm/forminfo.go canonical decomposition closure.
using TypeScript.Scanner;

namespace TypeScript.UnicodeNormalization;
internal struct NormalizationProperties
{
    internal byte Position, Size, CombiningClass, TrailingCombiningClass, LeadingNonStarters, Flags;
    internal ushort Index;
    private const int QuickCheckMask = 0x3F;
    private const int HeaderLengthMask = 0x1F;
    private const int HeaderFlagsMask = 0xE0;
    // forminfo.go:98–190. Packed classes remain compressed until public class observation.
    internal readonly bool BoundaryBefore() => CombiningClass == 0 && !CombinesBackward();
    internal readonly bool BoundaryAfter() => IsInert();
    internal readonly bool IsInvalid() => Flags == 0x80;
    internal readonly bool IsYesD() => (Flags & 0x4) == 0;
    internal readonly bool CombinesBackward() => (Flags & 0x8) != 0;
    internal readonly bool HasDecomposition() => (Flags & 0x4) != 0;
    internal readonly bool IsInert() => (Flags & QuickCheckMask) == 0 && CombiningClass == 0;
    internal readonly bool MultiSegment() => Index >= NormTables.firstMulti && Index < NormTables.endMulti;
    internal readonly byte NLeadingNonStarters() => LeadingNonStarters;
    internal readonly byte NTrailingNonStarters() => (byte)(Flags & 0x03);
    internal readonly ByteStringValue Decomposition()
    {
        if (Index == 0)
            return default;
        int length = NormTables.decomps[Index] & HeaderLengthMask;
        if (length == 31)
            length = 33;
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(NormTables.decomps)).Slice(Index + 1, Index + 1 + length);
    }

    internal readonly byte CCC() => Index >= NormTables.firstCCCZeroExcept ? (byte)0 : NormTables.ccc[CombiningClass];
    internal readonly byte LeadCCC() => NormTables.ccc[CombiningClass];
    internal readonly byte TrailCCC() => NormTables.ccc[TrailingCombiningClass];
    // forminfo.go:224; the original NFD form shares canonical NFC trie lookup.
    internal static NormalizationProperties LookupInfoNFC(ByteStringValue source, int position)
    {
        var(value, size) = NfcTrie.LookupString(source.Slice(position));
        return CompInfo(value, size);
    }

    // forminfo.go:253. Required packed decomposition flags and first-starter exception unchanged.
    internal static NormalizationProperties CompInfo(ushort value, int size)
    {
        if (size == 0)
            return new()
            {
                Flags = 0x80,
                Size = 1
            };
        if (value == 0)
            return new()
            {
                Size = (byte)size
            };
        if (value >= 0x8000)
        {
            var properties = new NormalizationProperties
            {
                Size = (byte)size,
                CombiningClass = unchecked((byte)value),
                TrailingCombiningClass = unchecked((byte)value),
                Flags = (byte)((value >> 8) & 0x3F)
            };
            if (properties.CombiningClass > 0 || properties.CombinesBackward())
                properties.LeadingNonStarters = (byte)(properties.Flags & 0x3);
            return properties;
        }

        int header = NormTables.decomps[value];
        var result = new NormalizationProperties
        {
            Size = (byte)size,
            Flags = (byte)(((header & HeaderFlagsMask) >> 2) | 0x4),
            Index = value
        };
        if (value >= NormTables.firstCCC)
        {
            int length = header & HeaderLengthMask;
            if (length == 31)
                length = 33;
            int offset = value + length + 1;
            byte combining = NormTables.decomps[offset];
            result.TrailingCombiningClass = (byte)(combining >> 2);
            result.Flags |= (byte)(combining & 0x3);
            if (offset >= NormTables.firstLeadingCCC)
            {
                result.LeadingNonStarters = (byte)(combining & 0x3);
                if (offset >= NormTables.firstStarterWithNLead)
                {
                    result.Flags &= 0x03;
                    result.Index = 0;
                    return result;
                }

                result.CombiningClass = NormTables.decomps[offset + 1];
            }
        }

        return result;
    }
}
