// Copyright 2013 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/syntax/perl_groups.go exact generated data.
// Source SHA256: d060a0645b60b210eaec7c3dba6d554dc297c14fe71cdbcc9c25b4fc7562c3a4
using TypeScript.Scanner;

namespace TypeScript.RegularExpressions.Syntax;
internal readonly record struct CharGroup(int Sign, RegexSlice<int> Class);
internal static class RegexCharGroups
{
    private static readonly RegexSlice<int> code1 = RegexSlice<int>.FromArray([0x30, 0x39]);
    private static readonly RegexSlice<int> code2 = RegexSlice<int>.FromArray([0x9, 0xa, 0xc, 0xd, 0x20, 0x20]);
    private static readonly RegexSlice<int> code3 = RegexSlice<int>.FromArray([0x30, 0x39, 0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a]);
    private static readonly RegexSlice<int> code4 = RegexSlice<int>.FromArray([0x30, 0x39, 0x41, 0x5a, 0x61, 0x7a]);
    private static readonly RegexSlice<int> code5 = RegexSlice<int>.FromArray([0x41, 0x5a, 0x61, 0x7a]);
    private static readonly RegexSlice<int> code6 = RegexSlice<int>.FromArray([0x0, 0x7f]);
    private static readonly RegexSlice<int> code7 = RegexSlice<int>.FromArray([0x9, 0x9, 0x20, 0x20]);
    private static readonly RegexSlice<int> code8 = RegexSlice<int>.FromArray([0x0, 0x1f, 0x7f, 0x7f]);
    private static readonly RegexSlice<int> code9 = RegexSlice<int>.FromArray([0x30, 0x39]);
    private static readonly RegexSlice<int> code10 = RegexSlice<int>.FromArray([0x21, 0x7e]);
    private static readonly RegexSlice<int> code11 = RegexSlice<int>.FromArray([0x61, 0x7a]);
    private static readonly RegexSlice<int> code12 = RegexSlice<int>.FromArray([0x20, 0x7e]);
    private static readonly RegexSlice<int> code13 = RegexSlice<int>.FromArray([0x21, 0x2f, 0x3a, 0x40, 0x5b, 0x60, 0x7b, 0x7e]);
    private static readonly RegexSlice<int> code14 = RegexSlice<int>.FromArray([0x9, 0xd, 0x20, 0x20]);
    private static readonly RegexSlice<int> code15 = RegexSlice<int>.FromArray([0x41, 0x5a]);
    private static readonly RegexSlice<int> code16 = RegexSlice<int>.FromArray([0x30, 0x39, 0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a]);
    private static readonly RegexSlice<int> code17 = RegexSlice<int>.FromArray([0x30, 0x39, 0x41, 0x46, 0x61, 0x66]);
    internal static readonly Dictionary<ByteStringKey, CharGroup> perlGroup = new()
    {
        [ByteStringKey.FromString("\\d")] = new(+1, code1),
        [ByteStringKey.FromString("\\D")] = new(-1, code1),
        [ByteStringKey.FromString("\\s")] = new(+1, code2),
        [ByteStringKey.FromString("\\S")] = new(-1, code2),
        [ByteStringKey.FromString("\\w")] = new(+1, code3),
        [ByteStringKey.FromString("\\W")] = new(-1, code3),
    };
    internal static readonly Dictionary<ByteStringKey, CharGroup> posixGroup = new()
    {
        [ByteStringKey.FromString("[:alnum:]")] = new(+1, code4),
        [ByteStringKey.FromString("[:^alnum:]")] = new(-1, code4),
        [ByteStringKey.FromString("[:alpha:]")] = new(+1, code5),
        [ByteStringKey.FromString("[:^alpha:]")] = new(-1, code5),
        [ByteStringKey.FromString("[:ascii:]")] = new(+1, code6),
        [ByteStringKey.FromString("[:^ascii:]")] = new(-1, code6),
        [ByteStringKey.FromString("[:blank:]")] = new(+1, code7),
        [ByteStringKey.FromString("[:^blank:]")] = new(-1, code7),
        [ByteStringKey.FromString("[:cntrl:]")] = new(+1, code8),
        [ByteStringKey.FromString("[:^cntrl:]")] = new(-1, code8),
        [ByteStringKey.FromString("[:digit:]")] = new(+1, code9),
        [ByteStringKey.FromString("[:^digit:]")] = new(-1, code9),
        [ByteStringKey.FromString("[:graph:]")] = new(+1, code10),
        [ByteStringKey.FromString("[:^graph:]")] = new(-1, code10),
        [ByteStringKey.FromString("[:lower:]")] = new(+1, code11),
        [ByteStringKey.FromString("[:^lower:]")] = new(-1, code11),
        [ByteStringKey.FromString("[:print:]")] = new(+1, code12),
        [ByteStringKey.FromString("[:^print:]")] = new(-1, code12),
        [ByteStringKey.FromString("[:punct:]")] = new(+1, code13),
        [ByteStringKey.FromString("[:^punct:]")] = new(-1, code13),
        [ByteStringKey.FromString("[:space:]")] = new(+1, code14),
        [ByteStringKey.FromString("[:^space:]")] = new(-1, code14),
        [ByteStringKey.FromString("[:upper:]")] = new(+1, code15),
        [ByteStringKey.FromString("[:^upper:]")] = new(-1, code15),
        [ByteStringKey.FromString("[:word:]")] = new(+1, code16),
        [ByteStringKey.FromString("[:^word:]")] = new(-1, code16),
        [ByteStringKey.FromString("[:xdigit:]")] = new(+1, code17),
        [ByteStringKey.FromString("[:^xdigit:]")] = new(-1, code17),
    };
}
