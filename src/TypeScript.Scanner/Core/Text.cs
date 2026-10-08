// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/text.go.
namespace TypeScript.Scanner;
public readonly record struct TextRange(int Start, int Finish)
{
    public int Pos() => Start;
    public int End() => Finish;
    public int Len() => Finish - Start;
    public bool IsValid() => Start >= 0 || Finish >= 0;
    public bool Contains(int pos) => pos >= Start && pos < Finish;
    public bool ContainsInclusive(int pos) => pos >= Start && pos <= Finish;
    public bool ContainsExclusive(int pos) => Start < pos && pos < Finish;
    public TextRange WithPos(int pos) => new(pos, Finish);
    public TextRange WithEnd(int end) => new(Start, end);
    public bool ContainedBy(TextRange other) => other.Start <= Start && other.Finish >= Finish;
    public bool Overlaps(TextRange other) => Math.Max(Start, other.Start) < Math.Min(Finish, other.Finish);
    public bool Intersects(TextRange other) => Math.Max(Start, other.Start) <= Math.Min(Finish, other.Finish);
}

internal static partial class Core
{
    public static TextRange NewTextRange(int pos, int end) => new(pos, end);
    public static TextRange UndefinedTextRange() => new(-1, -1);
    public static int CompareTextRanges(TextRange first, TextRange second)
    {
        int comparison = first.Pos() - second.Pos();
        return comparison != 0 ? comparison : first.End() - second.End();
    }
}
