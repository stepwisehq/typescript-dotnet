// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Complete port of tsc/internal/core/pattern.go.
namespace TypeScript.Scanner;
public readonly struct Pattern
{
    private readonly ByteStringValue text;
    public string Text => text.Materialize() ?? "";
    internal ByteStringValue TextValue => text.IsNull ? default : text;
    public int StarIndex { get; }

    public Pattern(string text, int starIndex)
    {
        this.text = ByteStringValue.FromString(text);
        StarIndex = starIndex;
    }

    internal Pattern(ByteStringValue text, int starIndex) { this.text = text.Snapshot(); StarIndex = starIndex; }
    public bool Matches(string candidate) => Matches(ByteStringValue.FromString(candidate));
    internal bool Matches(ByteStringValue candidate)
    {
        if (StarIndex == -1) return TextValue.EqualsValue(candidate);
        if (candidate.IsNull) throw new NullReferenceException();
        return candidate.Length >= TextValue.Length - 1 && candidate.StartsWith(TextValue.Slice(0, StarIndex)) && candidate.EndsWith(TextValue.Slice(StarIndex + 1));
    }

    public string MatchedText(string candidate) => MatchedText(ByteStringValue.FromString(candidate)).Materialize()!;
    internal ByteStringValue MatchedText(ByteStringValue candidate)
    {
        if (!Matches(candidate)) throw new InvalidOperationException("candidate does not match pattern");
        return StarIndex == -1 ? default : candidate.Slice(StarIndex, candidate.Length - TextValue.Length + StarIndex + 1);
    }
    public bool IsValid() => StarIndex == -1 || StarIndex < text.Length;

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

    public static T FindBestPatternMatch<T>(Slice<T> values, Func<T, Pattern> getPattern, string candidate) => FindBestPatternMatch(values, getPattern, ByteStringValue.FromString(candidate));
    internal static T FindBestPatternMatch<T>(Slice<T> values, Func<T, Pattern> getPattern, ByteStringValue candidate)
    {
        T bestPattern = typeof(T) == typeof(string) ? (T)(object)"" : default!;
        int longestMatchPrefixLength = -1;
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

    internal static Pattern TryParsePattern(ByteStringValue pattern)
    {
        int starIndex = pattern.IndexOf('*');
        return starIndex == -1 || !pattern.Slice(starIndex + 1).Contains('*') ? new(pattern, starIndex) : default;
    }
    public static Pattern TryParsePattern(string pattern)
    {
        return TryParsePattern(ByteStringValue.FromString(pattern));
    }
}
