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
