// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/pattern.go; binder-required parsing/validity slice.
namespace TypeScript.Scanner;
public readonly struct Pattern
{
    private readonly string? text;
    public string Text => text ?? "";
    public int StarIndex { get; }

    public Pattern(string text, int starIndex)
    {
        this.text = text;
        StarIndex = starIndex;
    }

    public bool IsValid() => StarIndex == -1 || StarIndex < (Text?.Length ?? 0);
}

internal static partial class Core
{
    public static Pattern TryParsePattern(string pattern)
    {
        int starIndex = pattern.IndexOf('*');
        return starIndex == -1 || !pattern.AsSpan(starIndex + 1).Contains('*') ? new(pattern, starIndex) : default;
    }
}
