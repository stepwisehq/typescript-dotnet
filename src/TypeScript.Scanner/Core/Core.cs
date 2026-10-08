// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/core.go, scanner spelling and parser-required syntax helpers.
namespace TypeScript.Scanner;
internal static partial class Core
{
    public static T IfElse<T>(bool condition, T a, T b) => condition ? a : b;
    public static string GetSpellingSuggestionForStrings(string name, IEnumerable<string> candidates)
    {
        int[] runeName = GoString.Runes(name);
        int maximumLengthDifference = Math.Max(2, (int)(runeName.Length * 0.34));
        double bestDistance = Math.Floor(runeName.Length * 0.4) + 0.9;
        string bestCandidate = "";
        bool hasBest = false;
        foreach (string candidateName in candidates)
        {
            int maxLen = Math.Max(candidateName.Length, runeName.Length);
            int minLen = Math.Min(candidateName.Length, runeName.Length);
            if (candidateName != "" && maxLen - minLen <= maximumLengthDifference)
            {
                if (candidateName == name)
                    continue;
                if (candidateName.Length < 3 && !UnicodeCase.EqualFold(candidateName, name))
                    continue;
                double distance = levenshteinWithMax(runeName, GoString.Runes(candidateName), bestDistance);
                if (distance < 0)
                    continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidateName;
                    hasBest = true;
                }
                else if (!hasBest || StringComparer.Ordinal.Compare(candidateName, bestCandidate) < 0)
                {
                    bestCandidate = candidateName;
                    hasBest = true;
                }
            }
        }

        return bestCandidate;
    }

    private static double levenshteinWithMax(int[] s1, int[] s2, double maxValue)
    {
        double[] previous = new double[s2.Length + 1], current = new double[s2.Length + 1];
        double big = maxValue + 0.01;
        for (int i = 0; i < previous.Length; i++)
            previous[i] = i;
        for (int i = 1; i <= s1.Length; i++)
        {
            int c1 = s1[i - 1];
            int minJ = Math.Max((int)Math.Ceiling(i - maxValue), 1);
            int maxJ = Math.Min((int)Math.Floor(maxValue + i), s2.Length);
            double colMin = i;
            current[0] = colMin;
            for (int j = 1; j < minJ; j++)
                current[j] = big;
            for (int j = minJ; j <= maxJ; j++)
            {
                double substitutionDistance = UnicodeCase.ToLower(s1[i - 1]) == UnicodeCase.ToLower(s2[j - 1]) ? previous[j - 1] + 0.1 : previous[j - 1] + 2;
                double dist = c1 == s2[j - 1] ? previous[j - 1] : Math.Min(previous[j] + 1, Math.Min(current[j - 1] + 1, substitutionDistance));
                current[j] = dist;
                colMin = Math.Min(colMin, dist);
            }

            for (int j = maxJ + 1; j <= s2.Length; j++)
                current[j] = big;
            if (colMin > maxValue)
                return -1;
            (previous, current) = (current, previous);
        }

        double res = previous[s2.Length];
        return res > maxValue ? -1 : res;
    }
}

internal static partial class Core
{
    public static T? FirstOrNil<T>(List<T>? values) => values is null || values.Count == 0 ? default : values[0];
    public static int FindIndex<T>(List<T>? values, Func<T, bool> predicate) => values?.FindIndex(x => predicate(x)) ?? -1;
    public static T? Find<T>(List<T>? values, Func<T, bool> predicate) => values is null ? default : values.FirstOrDefault(predicate);
    public static bool Some<T>(List<T>? values, Func<T, bool> predicate) => values?.Any(predicate) ?? false;
    public static bool Every<T>(List<T>? values, Func<T, bool> predicate) => values?.All(predicate) ?? true;
    public static T? LastOrNil<T>(List<T>? values) => values is null || values.Count == 0 ? default : values[^1];
    public static List<U>? Map<T, U>(List<T>? values, Func<T, U> map) => values?.Select(map).ToList();
    public static ScriptKind GetScriptKindFromFileName(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".js" or ".mjs" or ".cjs" => ScriptKind.JS,
        ".jsx" => ScriptKind.JSX,
        ".ts" or ".mts" or ".cts" => ScriptKind.TS,
        ".tsx" => ScriptKind.TSX,
        ".json" => ScriptKind.JSON,
        _ => ScriptKind.Unknown
    };
}

internal static partial class Core
{
    public static List<int> ComputeECMALineStarts(string text) => ComputeECMALineStartsSeq(text).ToList();
    public static IEnumerable<int> ComputeECMALineStartsSeq(string text)
    {
        int textLen = text.Length, pos = 0, lineStart = 0;
        while (pos < textLen)
        {
            int b = text[pos];
            if (b < Utf8.RuneSelf)
            {
                pos++;
                switch (b)
                {
                    case '\r':
                        if (pos < textLen && text[pos] == '\n')
                            pos++;
                        goto case '\n';
                    case '\n':
                        yield return lineStart;
                        lineStart = pos;
                        break;
                }
            }
            else
            {
                var(ch, size) = Utf8.DecodeRuneInString(text.AsSpan(pos));
                pos += size;
                if (StringUtil.IsLineBreak(ch))
                {
                    yield return lineStart;
                    lineStart = pos;
                }
            }
        }

        yield return lineStart;
    }
}

// Pointer-comparable combinators required by source-file binding.
internal static partial class Core
{
    public static T? OrElse<T>(T? value, T? defaultValue)
        where T : class => value ?? defaultValue;
    public static List<T> AppendIfUnique<T>(List<T>? values, T element)
        where T : class
    {
        if (values?.Contains(element) == true)
            return values;
        return GoRuntime.Append(values, element);
    }
}

// Exact generic spelling-selection control flow from core/core.go:581-632.
internal static partial class Core
{
    public static T? GetSpellingSuggestion<T>(string name, IEnumerable<T> candidates, Func<T, string> getName, Func<T, T, int> compare) => getSpellingSuggestion(name, candidates, getName, compare, 0);
    public static T? GetSpellingSuggestionWithMaxCandidateCount<T>(string name, IEnumerable<T> candidates, Func<T, string> getName, Func<T, T, int> compare, int maxCandidates) => getSpellingSuggestion(name, candidates, getName, compare, maxCandidates);
    private static T? getSpellingSuggestion<T>(string name, IEnumerable<T> candidates, Func<T, string> getName, Func<T, T, int> compare, int maxCandidates)
    {
        int[] runeName = GoString.Runes(name);
        int maximumLengthDifference = Math.Max(2, (int)(runeName.Length * 0.34));
        double bestDistance = Math.Floor(runeName.Length * 0.4) + 0.9;
        // Go string's zero value is empty, unlike a C# reference's zero value.
        T? zero = typeof(T) == typeof(string) ? (T)(object)"" : default;
        T? bestCandidate = zero;
        bool hasBest = false;
        int checkedCandidates = 0;
        foreach (T candidate in candidates)
        {
            checkedCandidates++;
            if (maxCandidates > 0 && checkedCandidates > maxCandidates)
                return zero;
            string candidateName = getName(candidate);
            int maxLen = Math.Max(candidateName.Length, runeName.Length);
            int minLen = Math.Min(candidateName.Length, runeName.Length);
            if (candidateName != "" && maxLen - minLen <= maximumLengthDifference)
            {
                if (candidateName == name)
                    continue;
                if (candidateName.Length < 3 && !UnicodeCase.EqualFold(candidateName, name))
                    continue;
                double distance = levenshteinWithMax(runeName, GoString.Runes(candidateName), bestDistance);
                if (distance < 0)
                    continue;
                Debug.Assert(distance <= bestDistance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidate;
                    hasBest = true;
                }
                else if (!hasBest || compare(candidate, bestCandidate!) < 0)
                {
                    bestCandidate = candidate;
                    hasBest = true;
                }
            }
        }

        return bestCandidate;
    }
}

// Upstream core/core.go:285; selected mapped outputs are pointer-like references.
internal static partial class Core
{
    public static TOut? FirstNonNil<TIn, TOut>(List<TIn>? slice, Func<TIn, TOut?> map)
        where TOut : class
    {
        if (slice is not null)
        {
            foreach (var value in slice)
            {
                var mapped = map(value);
                if (mapped is not null)
                    return mapped;
            }
        }

        return null;
    }
}

// core.go:466-500. ECMAScript byte/UTF16 coordinate support.
internal static partial class Core
{
    public static (int Line, int ByteOffset) PositionToLineAndByteOffset(int position, IReadOnlyList<int> lineStarts)
    {
        int low = 0, high = lineStarts.Count;
        // sort.Search: find the first line start greater than the position.
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (lineStarts[middle] > position)
                high = middle;
            else
                low = middle + 1;
        }

        int line = Math.Max(low - 1, 0);
        return (line, position - lineStarts[line]);
    }

    // Input is a Go UTF8 byte-string. Invalid sequences consume one byte and
    // contribute U+FFFD, including each byte of a JS surrogate sentinel.
    public static int UTF16Len(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] >= Utf8.RuneSelf)
            {
                int length = i;
                for (int position = i; position < text.Length;)
                {
                    var(rune, size) = Utf8.DecodeRuneInString(text.AsSpan(position));
                    length += rune >= 0x10000 ? 2 : 1;
                    position += size;
                }

                return length;
            }
        }

        return text.Length;
    }
}

// core.go:36. Preserve Go nil and the original slice when every item matches.
internal static partial class Core
{
    public static List<T>? Filter<T>(List<T>? values, Func<T, bool> predicate)
    {
        if (values == null)
            return null;
        int count = values.Count;
        for (int i = 0; i < count; i++)
        {
            if (!predicate(values[i]))
            {
                var result = values.GetRange(0, i);
                for (i++; i < count; i++)
                {
                    T value = values[i];
                    if (predicate(value))
                        result.Add(value);
                }

                return result;
            }
        }

        // Go returns its captured slice header even if the caller appended through a callback.
        return values.Count == count ? values : values.GetRange(0, count);
    }
}
