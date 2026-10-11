// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/core.go, scanner spelling and parser-required syntax helpers.
namespace TypeScript.Scanner;
internal static partial class Core
{
    // core.go:714. The source caller supplies a valid byte offset.
    public static int IndexAfter(string s, string pattern, int startIndex)
    {
        int matched = s.AsSpan(startIndex).IndexOf(pattern.AsSpan(), StringComparison.Ordinal);
        return matched == -1 ? -1 : matched + startIndex;
    }

    // core.go:866. True sorts after false.
    public static int CompareBooleans(bool a, bool b) => a == b ? 0 : a ? 1 : -1;

    // core.go:723.
    public static bool ShouldRewriteModuleSpecifier(string specifier, CompilerOptions compilerOptions) =>
        compilerOptions.RewriteRelativeImportExtensions == Tristate.True &&
        TsPath.PathIsRelative(specifier) && !TsPath.IsDeclarationFileName(specifier) && TsPath.HasTSFileExtension(specifier);

    public static bool ShouldRewriteModuleSpecifier(ByteStringValue specifier, CompilerOptions compilerOptions) =>
        compilerOptions.RewriteRelativeImportExtensions == Tristate.True &&
        TsPath.PathIsRelative(specifier) && !TsPath.IsDeclarationFileName(specifier) && TsPath.HasTSFileExtension(specifier);

    // core.go:828. Preserve the source list unless a duplicate requires a copy.
    public static List<T>? Deduplicate<T>(List<T>? values)
    {
        if (values is { Count: > 1 })
            for (int i = 0; i < values.Count; i++)
                if (containsPrefix(values, i, values[i]))
                {
                    var result = values.GetRange(0, i);
                    for (i++; i < values.Count; i++)
                        if (!result.Contains(values[i]))
                            result.Add(values[i]);
                    return result;
                }

        return values;
    }

    private static bool containsPrefix<T>(List<T> values, int end, T value)
    {
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < end; i++)
            if (comparer.Equals(values[i], value)) return true;
        return false;
    }

    // core.go:828. Clone only after the first duplicate; unchanged input keeps
    // its nil/empty and shared backing header, exactly like slices.Clone.
    public static Slice<T> Deduplicate<T>(Slice<T> values)
    {
        if (values.Length > 1)
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (!GoSlices.Contains(values.Slice2(0, i), value)) continue;
                var result = GoSlices.Clone(values.Slice2(0, i));
                for (i++; i < values.Length; i++)
                {
                    value = values[i];
                    if (!GoSlices.Contains(result, value)) result = result.Append(value);
                }
                return result;
            }
        return values;
    }

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
    // core.go:183/192/199 and ordered slice search helpers. Descriptor copies
    // capture the length while element reads retain the shared backing store.
    public static bool Same<T>(Slice<T> left, Slice<T> right) => left.SameStorageRange(right);
    public static bool Some<T>(Slice<T> values, Func<T, bool> predicate) =>
        Some(values, predicate, static (value, callback) => callback(value));
    public static bool Some<T, TState>(Slice<T> values, TState state, Func<T, TState, bool> predicate)
    {
        foreach (var value in values) if (predicate(value, state)) return true;
        return false;
    }
    public static bool Every<T>(Slice<T> values, Func<T, bool> predicate) =>
        Every(values, predicate, static (value, callback) => callback(value));
    public static bool Every<T, TState>(Slice<T> values, TState state, Func<T, TState, bool> predicate)
    {
        foreach (var value in values) if (!predicate(value, state)) return false;
        return true;
    }
    public static T? FirstOrNil<T>(Slice<T> values) => values.Length == 0 ? default : values[0];
    public static T? LastOrNil<T>(Slice<T> values) => values.Length == 0 ? default : values[values.Length - 1];
    public static T? Find<T>(Slice<T> values, Func<T, bool> predicate) =>
        Find(values, predicate, static (value, callback) => callback(value));
    public static T? Find<T, TState>(Slice<T> values, TState state, Func<T, TState, bool> predicate)
    {
        foreach (var value in values) if (predicate(value, state)) return value;
        return typeof(T) == typeof(string) ? (T)(object)"" : default;
    }
    public static TOut? FirstNonNil<TIn, TOut>(Slice<TIn> values, Func<TIn, TOut?> map) where TOut : class
    {
        foreach (var value in values)
        {
            var mapped = map(value);
            if (mapped is not null) return mapped;
        }
        return null;
    }
    public static int FindIndex<T>(Slice<T> values, Func<T, bool> predicate) =>
        FindIndex(values, predicate, static (value, callback) => callback(value));
    public static int FindIndex<T, TState>(Slice<T> values, TState state, Func<T, TState, bool> predicate)
    {
        for (int i = 0; i < values.Length; i++) if (predicate(values[i], state)) return i;
        return -1;
    }
    public static int FindLastIndex<T>(Slice<T> values, Func<T, bool> predicate)
    {
        for (int i = values.Length - 1; i >= 0; i--) if (predicate(values[i])) return i;
        return -1;
    }
    public static int CountWhere<T>(Slice<T> values, Func<T, bool> predicate)
    {
        int count = 0;
        foreach (var value in values) if (predicate(value)) count++;
        return count;
    }
    // core.go:305: only two nonempty inputs need a fresh combined buffer.
    public static Slice<T> Concatenate<T>(Slice<T> left, Slice<T> right)
    {
        if (right.Length == 0) return left;
        if (left.Length == 0) return right;
        var result = Slice<T>.Make(checked(left.Length + right.Length));
        left.AsSpan().CopyTo(result.AsSpan());
        right.AsSpan().CopyTo(result.AsSpan().Slice(left.Length));
        return result;
    }
    public static Slice<T> AppendIfUnique<T>(Slice<T> values, T element) where T : class =>
        GoSlices.Contains(values, element) ? values : values.Append(element);

    // core.go:36: return the original header unless the first exclusion occurs.
    public static Slice<T> Filter<T>(Slice<T> values, Func<T, bool> predicate) =>
        Filter(values, predicate, static (value, callback) => callback(value));
    public static Slice<T> Filter<T, TState>(Slice<T> values, TState state, Func<T, TState, bool> predicate)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!predicate(values[i], state))
            {
                var result = GoSlices.Clone(values.Slice2(0, i));
                for (i++; i < values.Length; i++)
                {
                    var value = values[i];
                    if (predicate(value, state)) result = result.Append(value);
                }
                return result;
            }
        }
        return values;
    }
    // core.go:151: unchanged maps borrow; first change makes one detached result.
    public static Slice<T> SameMap<T>(Slice<T> values, Func<T, T> map) =>
        SameMap(values, map, static (value, callback) => callback(value));

    public static Slice<T> SameMap<T, TState>(Slice<T> values, TState state, Func<T, TState, T> map) =>
        sameMapIndex(values, (State: state, Map: map), static (value, _, context) => context.Map(value, context.State));
    public static Slice<T> SameMapIndex<T>(Slice<T> values, Func<T, int, T> map) =>
        sameMapIndex(values, map, static (value, index, callback) => callback(value, index));
    private static Slice<T> sameMapIndex<T, TState>(Slice<T> values, TState state, Func<T, int, TState, T> map)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var mapped = map(value, i, state);
            if (!EqualityComparer<T>.Default.Equals(mapped, value))
            {
                var result = Slice<T>.Make(values.Length);
                values.AsSpan().Slice(0, i).CopyTo(result.AsSpan());
                result[i] = mapped;
                for (int j = i + 1; j < values.Length; j++) result[j] = map(values[j], j, state);
                return result;
            }
        }
        return values;
    }
    // core.go:80. Signature outputs keep descriptors; other mapped outputs keep
    // their established owned List storage, without projecting the input slice.
    public static Slice<U> MapSlice<T, U>(Slice<T> values, Func<T, U> map) =>
        MapSlice(values, map, static (value, callback) => callback(value));

    public static Slice<U> MapSlice<T, U, TState>(Slice<T> values, TState state, Func<T, TState, U> map)
    {
        if (values.IsNil) return default;
        return MapSlice<T, U, TState>(values.AsSpan(), state, map);
    }

    // List inputs retain their established storage; mapped Node outputs are
    // published directly as descriptors instead of an intermediate List.
    public static Slice<U> MapSlice<T, U>(List<T>? values, Func<T, U> map) =>
        MapSlice(values, map, static (value, callback) => callback(value));

    public static Slice<U> MapSlice<T, U, TState>(List<T>? values, TState state, Func<T, TState, U> map)
    {
        if (values is null) return default;
        return MapSlice<T, U, TState>(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(values), state, map);
    }

    private static Slice<U> MapSlice<T, U, TState>(ReadOnlySpan<T> values, TState state, Func<T, TState, U> map)
    {
        var result = Slice<U>.Make(values.Length);
        for (int i = 0; i < values.Length; i++) result[i] = map(values[i], state);
        return result;
    }
    // core.go:106. Borrow the captured input header; only mapped output escapes.
    public static Slice<U> MapIndexSlice<T, U>(List<T>? values, Func<T, int, U> map) =>
        MapIndexSlice(values, map, static (value, index, callback) => callback(value, index));
    public static Slice<U> MapIndexSlice<T, U, TState>(List<T>? values, TState state, Func<T, int, TState, U> map)
    {
        if (values is null) return default;
        ReadOnlySpan<T> input = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(values);
        return mapIndexSlice(input, state, map);
    }
    public static Slice<U> MapIndexSlice<T, U>(Slice<T> values, Func<T, int, U> map) =>
        MapIndexSlice(values, map, static (value, index, callback) => callback(value, index));
    public static Slice<U> MapIndexSlice<T, U, TState>(Slice<T> values, TState state, Func<T, int, TState, U> map)
    {
        if (values.IsNil) return default;
        return mapIndexSlice(values.AsSpan(), state, map);
    }
    private static Slice<U> mapIndexSlice<T, U, TState>(ReadOnlySpan<T> input, TState state, Func<T, int, TState, U> map)
    {
        var result = Slice<U>.Make(input.Length);
        for (int i = 0; i < input.Length; i++) result[i] = map(input[i], i, state);
        return result;
    }
    public static List<U>? Map<T, U>(Slice<T> values, Func<T, U> map)
    {
        if (values.IsNil) return null;
        var result = new List<U>(values.Length);
        foreach (var value in values) result.Add(map(value));
        return result;
    }
    // core.go:117: no mapped nonzero value retains a nil output List boundary.
    public static List<U>? MapNonNil<T, U>(Slice<T> values, Func<T, U> map)
    {
        List<U>? result = null;
        U zero = typeof(U) == typeof(string) ? (U)(object)string.Empty : default!;
        foreach (var value in values)
        {
            var mapped = map(value);
            if (!EqualityComparer<U>.Default.Equals(mapped, zero)) (result ??= new()).Add(mapped);
        }
        return result;
    }

    // core.go:117. Map only nonzero results into the retained Go header.
    public static Slice<U> MapNonNilSlice<T, U>(Slice<T> values, Func<T, U> map)
    {
        Slice<U> result = default;
        U zero = typeof(U) == typeof(string) ? (U)(object)string.Empty : default!;
        foreach (var value in values)
        {
            var mapped = map(value);
            if (!EqualityComparer<U>.Default.Equals(mapped, zero)) result = result.Append(mapped);
        }
        return result;
    }

    public static T? FirstOrNil<T>(List<T>? values) => values is null || values.Count == 0 ? default : values[0];
    public static int FindIndex<T>(List<T>? values, Func<T, bool> predicate) => values?.FindIndex(x => predicate(x)) ?? -1;

    // core.go:246; nil slices have no matching index.
    public static int FindLastIndex<T>(List<T>? values, Func<T, bool> predicate) => values?.FindLastIndex(x => predicate(x)) ?? -1;
    // core.go:218. Original range captures slice-header length and returns Go zero.
    public static T? Find<T>(List<T>? values, Func<T, bool> predicate)
    {
        int count = values?.Count ?? 0;
        for (int i = 0; i < count; i++)
            if (predicate(values![i]))
                return values[i];
        return typeof(T) == typeof(string) ? (T)(object)"" : default;
    }

    public static bool Some<T>(List<T>? values, Func<T, bool> predicate) => values?.Any(predicate) ?? false;
    public static bool Every<T>(List<T>? values, Func<T, bool> predicate) => values?.All(predicate) ?? true;
    public static T? LastOrNil<T>(List<T>? values) => values is null || values.Count == 0 ? default : values[^1];
    public static List<U>? Map<T, U>(List<T>? values, Func<T, U> map) => values?.Select(map).ToList();
    public static List<U>? MapIndex<T, U>(List<T>? values, Func<T, int, U> map)
    {
        if (values is null)
            return null;
        int count = values.Count;
        var result = new List<U>(count);
        for (int i = 0; i < count; i++)
            result.Add(map(values[i], i));
        return result;
    }

    public static List<U>? MapIndex<T, U>(Slice<T> values, Func<T, int, U> map)
    {
        if (values.IsNil)
            return null;
        int count = values.Length;
        var result = new List<U>(count);
        for (int i = 0; i < count; i++)
            result.Add(map(values[i], i));
        return result;
    }

    public static List<T>? SameMapIndex<T>(List<T>? values, Func<T, int, T> map) =>
        sameMapIndex(values, map, static (value, index, callback) => callback(value, index));

    private static List<T>? sameMapIndex<T, TState>(List<T>? values, TState state, Func<T, int, TState, T> map)
    {
        int count = values?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            T value = values![i];
            T mapped = map(value, i, state);
            if (!EqualityComparer<T>.Default.Equals(mapped, value))
            {
                var result = values.GetRange(0, i);
                result.Add(mapped);
                for (int j = i + 1; j < count; j++)
                    result.Add(map(values[j], j, state));
                return result;
            }
        }

        return values is not null && values.Count != count ? values.GetRange(0, count) : values;
    }

    public static List<T>? SameMap<T>(List<T>? values, Func<T, T> map) =>
        SameMap(values, map, static (value, callback) => callback(value));
    public static List<T>? SameMap<T, TState>(List<T>? values, TState state, Func<T, TState, T> map) =>
        sameMapIndex(values, (State: state, Map: map), static (value, _, context) => context.Map(value, context.State));
    // Go compares slice length and the address of the first element; empty
    // slices compare alike. Managed slices retain their owning list identity.
    public static bool Same<T>(List<T>? left, List<T>? right) => (left?.Count ?? 0) == (right?.Count ?? 0) && ((left?.Count ?? 0) == 0 || ReferenceEquals(left, right));
    public static List<T>? Concatenate<T>(List<T>? left, List<T>? right)
    {
        if ((right?.Count ?? 0) == 0)
            return left;
        if ((left?.Count ?? 0) == 0)
            return right;
        var result = new List<T>(left!);
        result.AddRange(right!);
        return result;
    }

    public static int CountWhere<T>(List<T>? values, Func<T, bool> predicate)
    {
        int count = 0, length = values?.Count ?? 0;
        for (int i = 0; i < length; i++)
            if (predicate(values![i]))
                count++;
        return count;
    }

    public static List<T> ReplaceElement<T>(List<T> values, int index, T value)
    {
        var result = GoSlices.Clone(values);
        result[index] = value;
        return result;
    }

    public static Slice<T> ReplaceElement<T>(Slice<T> values, int index, T value)
    {
        var result = GoSlices.Clone(values);
        result[index] = value;
        return result;
    }

    // core.go:525/561. Typed Go-byte paths use the pinned path extension rule;
    // the legacy Unicode overload below retains its established public role.
    public static ScriptKind GetScriptKindFromFileName(RootedFilePath fileName)
    {
        var extension = ByteStringValue.FromString(fileName.AnyExtension(null, CaseSensitivity.CaseSensitive));
        if (extension.Length == 0) return ScriptKind.Unknown;
        extension = GoStrings.ToLower(extension);
        if (extension.EqualsBytes(".js"u8) || extension.EqualsBytes(".cjs"u8) || extension.EqualsBytes(".mjs"u8)) return ScriptKind.JS;
        if (extension.EqualsBytes(".jsx"u8)) return ScriptKind.JSX;
        if (extension.EqualsBytes(".ts"u8) || extension.EqualsBytes(".cts"u8) || extension.EqualsBytes(".mts"u8)) return ScriptKind.TS;
        if (extension.EqualsBytes(".tsx"u8)) return ScriptKind.TSX;
        if (extension.EqualsBytes(".json"u8)) return ScriptKind.JSON;
        return ScriptKind.Unknown;
    }
    public static ScriptKind EnsureScriptKindFromFileName(RootedFilePath fileName)
    {
        var kind = GetScriptKindFromFileName(fileName);
        return kind == ScriptKind.Unknown ? ScriptKind.TS : kind;
    }

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
    public static Slice<int> ComputeECMALineStarts(string text) => ComputeECMALineStarts(ByteStringValue.FromString(text));
    // Same core.go line-start algorithm; a borrowed byte-text descriptor avoids
    // materializing a JSDoc node's bounded source before normalization.
    public static Slice<int> ComputeECMALineStarts(ByteStringView text) => ComputeECMALineStarts(ByteStringValue.FromView(text));
    private static Slice<int> ComputeECMALineStarts(ByteStringValue text)
    {
        if (text.IsNull) throw new NullReferenceException();
        int textLen = text.Length, pos = 0, lineStart = 0;
        // core.go:429 specifies this initial capacity, rather than a guessed hint.
        int capacity = checked((text.IsBytes ? text.ByteSpan.Count((byte)'\n') : text.CharSpan.Count('\n')) + 1);
        var result = Slice<int>.Make(0, capacity);
        while (pos < textLen)
        {
            int b = text[pos];
            if (b < Utf8.RuneSelf)
            {
                pos++;
                switch (b)
                {
                    case '\r':
                        if (pos < textLen && text[pos] == '\n') pos++;
                        goto case '\n';
                    case '\n':
                        result = result.Append(lineStart); lineStart = pos; break;
                }
            }
            else
            {
                var (ch, size) = text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[pos..]) : Utf8.DecodeRuneInString(text.CharSpan[pos..]);
                pos += size;
                if (StringUtil.IsLineBreak(ch)) { result = result.Append(lineStart); lineStart = pos; }
            }
        }
        return result.Append(lineStart);
    }
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
    public static IEnumerable<int> ComputeECMALineStartsSeq(ByteStringView text)
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

    public static int UTF16Len(ByteStringView text)
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

    // core.go:117. Preserve nil output when no mapped value differs from its Go zero value.
    internal static List<U>? MapNonNil<T, U>(List<T>? source, Func<T, U> f)
    {
        List<U>? result = null;
        U zero = typeof(U) == typeof(string) ? (U)(object)string.Empty : default!;
        int count = source?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            var mapped = f(source![i]);
            if (!EqualityComparer<U>.Default.Equals(mapped, zero))
                (result ??= []).Add(mapped);
        }

        return result;
    }

    // core.go:269. A negative index retains the upstream invalid-index behavior.
    internal static T? ElementOrNil<T>(Slice<T> source, int index)
    {
        if (index < source.Length) return source[index];
        return typeof(T) == typeof(string) ? (T)(object)string.Empty : default;
    }
    internal static T? ElementOrNil<T>(List<T>? source, int index)
    {
        if (index < (source?.Count ?? 0))
            return source![index];
        return typeof(T) == typeof(string) ? (T)(object)string.Empty : default;
    }
}

internal static partial class Core
{
    // core.go:228. slices.Backward captures the original slice-header length.
    internal static T? FindLast<T>(Slice<T> values, Func<T, bool> predicate)
    {
        for (int i = values.Length - 1; i >= 0; i--)
        {
            var value = values[i];
            if (predicate(value)) return value;
        }
        return typeof(T) == typeof(string) ? (T)(object)"" : default;
    }

    internal static T? FindLast<T>(List<T>? values, Func<T, bool> predicate)
    {
        for (int i = (values?.Count ?? 0) - 1; i >= 0; i--)
            if (predicate(values![i]))
                return values[i];
        return typeof(T) == typeof(string) ? (T)(object)"" : default;
    }
}
