// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# read-decoding port of tsc/internal/vfs/internal/internal.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.Vfs;
public static class VfsInternal
{
    public static (string Root, string Remaining) SplitPath(string path)
    {
        path = TsPath.NormalizePath(path);
        int rootLength = RootLength(path);
        return (path[..rootLength], TsPath.RemoveTrailingDirectorySeparator(path[rootLength..]));
    }

    public static int RootLength(string path)
    {
        int length = TsPath.GetEncodedRootLength(path);
        if (length == 0)
            throw new ArgumentException("vfs: path is not absolute: " + path);
        return length < 0 ? ~length : length;
    }

    public static (string Contents, bool Ok) DecodeBytes(string bytes)
    {
        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                return (decodeUtf16(bytes[2..], true), true);
            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                return (decodeUtf16(bytes[2..], false), true);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            bytes = bytes[3..];
        return (bytes, true);
    }

    // Representation adaptation of decodeBytes/decodeUtf16: transfer fresh disk
    // storage, slice the UTF8 BOM, and transcode UTF16 once into owned UTF8 bytes.
    // No UTF8 validation is performed; source malformed/WTF8 bytes pass through.
    internal static (ByteStringView Contents, bool Ok) DecodeSourceBytes(byte[] owned)
    {
        if (owned.Length >= 2)
        {
            if (owned[0] == 0xFF && owned[1] == 0xFE)
                return (decodeUtf16Bytes(owned.AsSpan(2), true), true);
            if (owned[0] == 0xFE && owned[1] == 0xFF)
                return (decodeUtf16Bytes(owned.AsSpan(2), false), true);
        }

        var contents = ByteStringView.FromOwnedBytes(owned);
        if (owned.Length >= 3 && owned[0] == 0xEF && owned[1] == 0xBB && owned[2] == 0xBF)
            contents = contents.Slice(3);
        return (contents, true);
    }

    private static ByteStringView decodeUtf16Bytes(ReadOnlySpan<byte> bytes, bool littleEndian)
    {
        // As in the source uint16 slice, ignore an odd trailing byte. Reading
        // each unit directly avoids an intermediate UTF16 string or array.
        bytes = bytes[..(bytes.Length & ~1)];
        int length = 0;
        for (int i = 0; i < bytes.Length;)
            length = checked(length + Utf8.RuneLen(nextUtf16Rune(bytes, ref i, littleEndian)));

        var decoded = new byte[length];
        int offset = 0;
        for (int i = 0; i < bytes.Length;)
        {
            var rune = new System.Text.Rune(nextUtf16Rune(bytes, ref i, littleEndian));
            offset += rune.EncodeToUtf8(decoded.AsSpan(offset));
        }
        return ByteStringView.FromOwnedBytes(decoded);
    }

    private static int nextUtf16Rune(ReadOnlySpan<byte> bytes, ref int offset, bool littleEndian)
    {
        int unit = littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..])
            : BinaryPrimitives.ReadUInt16BigEndian(bytes[offset..]);
        offset += 2;
        if (unit is >= 0xD800 and <= 0xDBFF && offset < bytes.Length)
        {
            int next = littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..])
                : BinaryPrimitives.ReadUInt16BigEndian(bytes[offset..]);
            if (next is >= 0xDC00 and <= 0xDFFF)
            {
                offset += 2;
                return 0x10000 + ((unit - 0xD800) << 10) + next - 0xDC00;
            }
        }
        return unit is >= 0xD800 and <= 0xDFFF ? Utf8.RuneError : unit;
    }

    private static string decodeUtf16(string bytes, bool littleEndian)
    {
        var units = new ushort[bytes.Length / 2]; // Source ignores an odd trailing byte.
        for (int i = 0; i < units.Length; i++)
        {
            var pair = GoString.Bytes(bytes.AsSpan(i * 2, 2).ToString());
            units[i] = littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(pair) : BinaryPrimitives.ReadUInt16BigEndian(pair);
        }

        var builder = new GoBuilder();
        for (int i = 0; i < units.Length; i++)
        {
            int unit = units[i];
            if (unit is >= 0xD800 and <= 0xDBFF && i + 1 < units.Length && units[i + 1] is >= 0xDC00 and <= 0xDFFF)
            {
                builder.WriteRune(0x10000 + ((unit - 0xD800) << 10) + units[++i] - 0xDC00);
            }
            else
                builder.WriteRune(unit is >= 0xD800 and <= 0xDFFF ? Utf8.RuneError : unit);
        }

        return builder.String();
    }
}
