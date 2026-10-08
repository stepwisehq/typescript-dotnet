// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Managed compatibility adapters for the parser's Go slice and pointer semantics.
#nullable disable
namespace TypeScript.Scanner;
internal sealed class GoRef<T>(Func<T> get, Action<T> set)
{
    public T Value { get => get(); set => set(value); }
}

internal sealed class GoArena<T>
{
    public List<T> NewSlice1(T value) => new()
    {
        value
    };
    public List<T> NewSlice(int length) => length == 0 ? null : Enumerable.Repeat(default(T), length).ToList();
    public List<T> Clone(List<T> list) => list is null || list.Count == 0 ? null : new(list);
}

internal static class GoRuntime
{
    public static List<T> MakeSlice<T>(int length) => Enumerable.Repeat(default(T), length).ToList();
    public static int Len(string s) => s?.Length ?? 0;
    public static int Len<T>(List<T> s) => s?.Count ?? 0;
    public static int Len<T>(T[] s) => s?.Length ?? 0;
    public static int Len<T>(HashSet<T> s) => s?.Count ?? 0;
    public static int Len<K, V>(Dictionary<K, V> s) => s?.Count ?? 0;
    public static List<T> Append<T>(List<T> source, params T[] values)
    {
        if (values is null || values.Length == 0)
            return source;
        var result = source is null ? new List<T>() : new(source);
        result.AddRange(values);
        return result;
    }

    public static List<T> Append<T>(List<T> source, IEnumerable<T> values)
    {
        var additions = values?.ToList();
        if (additions is null || additions.Count == 0)
            return source;
        var result = source is null ? new List<T>() : new(source);
        result.AddRange(additions);
        return result;
    }

    public static string Slice(string source, int start) => source.Substring(start);
    public static string Slice(string source, int start, int end) => source.Substring(start, end - start);
    public static List<T> Slice<T>(List<T> source, int start) => source is null && start == 0 ? null : source.GetRange(start, source.Count - start);
    public static List<T> Slice<T>(List<T> source, int start, int end) => source is null && start == 0 && end == 0 ? null : source.GetRange(start, end - start);
    public static IEnumerable<T> RangeValues<T>(IEnumerable<T> source) => source ?? [];
    public static IEnumerable<V> RangeValues<K, V>(Dictionary<K, V> source) => source?.Values ?? Enumerable.Empty<V>();
    public static IEnumerable<T> RangeKeys<T>(IEnumerable<T> source) => source ?? [];
    public static IEnumerable<int> RangeKeys<T>(List<T> source) => Enumerable.Range(0, source?.Count ?? 0);
    public static IEnumerable<int> RangeKeys(string source)
    {
        for (int i = 0; i < source.Length;)
        {
            yield return i;
            var(_, size) = Utf8.DecodeRuneInString(source.Substring(i));
            i += size;
        }
    }

    public static IEnumerable<ParsingContext> RangeKeys(ParsingContext count) => Enumerable.Range(0, (int)count).Select(x => (ParsingContext)x);
    public static IEnumerable<int> RangeKeys(int count) => Enumerable.Range(0, count);
    public static IEnumerable<K> RangeKeys<K, V>(Dictionary<K, V> source) => source?.Keys ?? Enumerable.Empty<K>();
    public static IEnumerable<KeyValuePair<int, T>> Range<T>(List<T> source)
    {
        for (int i = 0; i < (source?.Count ?? 0); i++)
            yield return new(i, source[i]);
    }

    public static IEnumerable<KeyValuePair<int, int>> Range(string source)
    {
        for (int i = 0; i < source.Length;)
        {
            var(r, size) = Utf8.DecodeRuneInString(source.Substring(i));
            yield return new(i, r);
            i += size;
        }
    }

    public static IEnumerable<KeyValuePair<K, V>> Range<K, V>(Dictionary<K, V> source) => source ?? new();
}

internal static class GoSlices
{
    public static int Index<T>(List<T> values, T target) => values?.IndexOf(target) ?? -1;
    public static (int Index, bool Found) BinarySearchFunc<T>(List<T> values, T target, Comparison<T> compare)
    {
        int low = 0, high = values?.Count ?? 0;
        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (compare(values[mid], target) < 0)
                low = mid + 1;
            else
                high = mid;
        }

        return (low, low < (values?.Count ?? 0) && compare(values[low], target) == 0);
    }

    public static IEnumerable<T> Values<T>(IEnumerable<T> values) => values ?? [];
    public static List<T> Clone<T>(List<T> values) => values is null ? null : new(values);
    public static void SortFunc<T>(List<T> values, Comparison<T> compare) => GoSliceSort.SortFunc(values, compare);
    public static List<T> DeleteFunc<T>(List<T> values, Func<T, bool> pred) => values?.Where(x => !pred(x)).ToList();
}

internal static class ParserPaths
{
    public static string AsString(this string value) => value ?? "";
    // RootedFilePath.IsDeclarationFile forwards to the normalized-path extension algorithm.
    public static bool IsDeclarationFile(this string value) =>
        TsPath.getDeclarationFileExtensionFromNormalized(value ?? "") != "";
}
