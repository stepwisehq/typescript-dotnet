// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Complete port of tsc/internal/core/pattern.go.
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

    public bool Matches(string candidate) => StarIndex == -1 ? Text == candidate : candidate.Length >= Text.Length - 1 && candidate.StartsWith(Text[..StarIndex], StringComparison.Ordinal) && candidate.EndsWith(Text[(StarIndex + 1)..], StringComparison.Ordinal);
    public string MatchedText(string candidate)
    {
        if (!Matches(candidate))
            throw new InvalidOperationException("candidate does not match pattern");
        return StarIndex == -1 ? "" : candidate[StarIndex..(candidate.Length - Text.Length + StarIndex + 1)];
    }

    public bool IsValid() => StarIndex == -1 || StarIndex < (Text?.Length ?? 0);
}

internal static partial class Core
{
    public static T FindBestPatternMatch<T>(List<T>? values, Func<T, Pattern> getPattern, string candidate)
    {
        T bestPattern = typeof(T) == typeof(string) ? (T)(object)"" : default!;
        int longestMatchPrefixLength = -1;
        if (values != null)
            foreach (T value in values)
            {
                Pattern pattern = getPattern(value);
                if ((pattern.StarIndex == -1 || pattern.StarIndex > longestMatchPrefixLength) && pattern.Matches(candidate))
                {
                    bestPattern = value;
                    longestMatchPrefixLength = pattern.StarIndex;
                }
            }

        return bestPattern;
    }

    public static Pattern TryParsePattern(string pattern)
    {
        int starIndex = pattern.IndexOf('*');
        return starIndex == -1 || !pattern.AsSpan(starIndex + 1).Contains('*') ? new(pattern, starIndex) : default;
    }
}
