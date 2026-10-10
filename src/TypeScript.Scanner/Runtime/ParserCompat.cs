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
    public Slice<T> Clone(Slice<T> slice) => slice.Length == 0 ? default : TypeScript.Scanner.Slice<T>.FromArray(slice.AsSpan().ToArray());
    public List<T> Clone(List<T> list) => list is null || list.Count == 0 ? null : new(list);

    // Go core/arena.go:Clone: retain the final detached clone at the mutable List boundary.
    public List<T> CloneAsList(Slice<T> slice)
    {
        if (slice.Length == 0) return null;
        var result = new List<T>(slice.Length);
        result.AddRange(slice.AsSpan());
        return result;
    }
}

// Managed adaptation of translated Go list-range loops. Preserve the existing
// List adapter's live Count and retained reference, rather than silently adding
// Go's captured slice-header length semantics in this allocation-only change.
internal readonly struct GoListRange<T>(List<T> source) : IEnumerable<KeyValuePair<int, T>>
{
    public Enumerator GetEnumerator() => new(source);
    IEnumerator<KeyValuePair<int, T>> IEnumerable<KeyValuePair<int, T>>.GetEnumerator() => GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<int, T>>
    {
        private readonly List<T> source;
        private int index;
        private bool finished;
        internal Enumerator(List<T> source)
        {
            this.source = source;
            index = -1;
            finished = false;
            Current = default;
        }
        public KeyValuePair<int, T> Current { get; private set; }
        object System.Collections.IEnumerator.Current => Current;
        public bool MoveNext()
        {
            if (finished) return false;
            int next = index + 1;
            if (next < (source?.Count ?? 0))
            {
                index = next;
                // Capture at MoveNext, just as the previous yield expression did.
                Current = new(index, source[index]);
                return true;
            }
            finished = true;
            return false;
        }
        public void Dispose() => finished = true;
        public void Reset() => throw new NotSupportedException();
    }
}

// Values have a different preexisting List contract from GoListRange: use the
// actual List enumerator's version checks, cached Current and Dispose/Reset.
// Explicit IEnumerable routes retain the original interface enumerator; only
// concrete foreach binds the allocation-free value enumerator below.
internal readonly struct GoListValues<T>(List<T> source) : IEnumerable<T>
{
    public Enumerator GetEnumerator() => new(source);
    private IEnumerator<T> InterfaceEnumerator() => source is null
        ? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator()
        : ((IEnumerable<T>)source).GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => InterfaceEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => source is null
        ? ((System.Collections.IEnumerable)Array.Empty<T>()).GetEnumerator()
        : ((System.Collections.IEnumerable)source).GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly bool hasList;
        private List<T>.Enumerator listEnumerator;
        private readonly IEnumerator<T> emptyEnumerator;
        internal Enumerator(List<T> source)
        {
            // The original generic List interface uses the empty-array
            // enumerator when Count is zero at GetEnumerator, without later
            // List version checks. Preserve that distinct empty-list policy.
            hasList = source != null && source.Count != 0;
            listEnumerator = hasList ? source.GetEnumerator() : default;
            emptyEnumerator = hasList ? null : source is null
                ? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator()
                : ((IEnumerable<T>)source).GetEnumerator();
        }
        private IEnumerator<T> EmptyEnumerator => emptyEnumerator ?? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
        public T Current => hasList ? listEnumerator.Current : EmptyEnumerator.Current;
        object System.Collections.IEnumerator.Current => hasList
            ? ((System.Collections.IEnumerator)listEnumerator).Current
            : ((System.Collections.IEnumerator)EmptyEnumerator).Current;
        public bool MoveNext() => hasList ? listEnumerator.MoveNext() : EmptyEnumerator.MoveNext();
        public void Dispose()
        {
            if (hasList) listEnumerator.Dispose();
            else EmptyEnumerator.Dispose();
        }
        public void Reset()
        {
            if (!hasList) { EmptyEnumerator.Reset(); return; }
            // List exposes Reset only through IEnumerator. Boxing is confined
            // to an explicit Reset request, never ordinary value iteration.
            System.Collections.IEnumerator boxed = listEnumerator;
            boxed.Reset();
            listEnumerator = (List<T>.Enumerator)boxed;
        }
    }
}

// Integer Go range captures its signed bound and yields 0..count-1. Retain
// the old managed iterator's negative/zero empty and completion behavior.
internal readonly struct GoIntegerRange(int count) : IEnumerable<int>
{
    public Enumerator GetEnumerator() => new(count);
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    public struct Enumerator(int count) : IEnumerator<int>
    {
        private int next;
        private bool finished;
        public int Current { get; private set; }
        object System.Collections.IEnumerator.Current => Current;
        public bool MoveNext()
        {
            if (finished) return false;
            if (next < count) { Current = next++; return true; }
            finished = true;
            return false;
        }
        // The prior simple yield loop has no finally/resource cleanup.
        public void Dispose() { }
        public void Reset() => throw new NotSupportedException();
    }
}

internal static class GoRuntime
{
    // Narrow source descriptor overloads; existing List consumers are unchanged.
    public static int Len<T>(Slice<T> source) => source.Length;
    public static Slice<T> Append<T>(Slice<T> source, T value) => source.Append(value);
    public static Slice<T> Slice<T>(Slice<T> source, int start) => source.Slice2(start);
    public static Slice<T> Slice<T>(Slice<T> source, int start, int end) => source.Slice2(start, end);
    public static IEnumerable<int> RangeKeys<T>(Slice<T> source)
    {
        for (int i = 0; i < source.Length; i++) yield return i;
    }
    public static List<T> MakeSlice<T>(int length) => Enumerable.Repeat(default(T), length).ToList();
    public static int Len(string s) => s?.Length ?? 0;
    public static int Len(ByteStringView s) => s.Length;
    public static int Len<T>(List<T> s) => s?.Count ?? 0;
    public static int Len<T>(T[] s) => s?.Length ?? 0;
    public static int Len<T>(HashSet<T> s) => s?.Count ?? 0;
    public static int Len<K, V>(Dictionary<K, V> s) => s?.Count ?? 0;
    // Managed append adapter: preserve the existing detached List result policy.
    // The array bridge retains null/default argument binding; scalar/multi-item
    // calls use C# params spans instead of allocating a temporary params array.
    public static List<T> Append<T>(List<T> source, T[] values) => Append(source, (ReadOnlySpan<T>)values);
    public static List<T> Append<T>(List<T> source, params ReadOnlySpan<T> values)
    {
        if (values.Length == 0)
            return source;
        var result = new List<T>(checked((source?.Count ?? 0) + values.Length));
        if (source != null)
            result.AddRange(source);
        result.AddRange(values);
        return result;
    }

    public static List<T> Append<T>(List<T> source, IEnumerable<T> values)
    {
        var additions = values?.ToList();
        if (additions is null || additions.Count == 0)
            return source;
        // Keep eager materialization (including its side effects) before copying source.
        return Append(source, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(additions));
    }

    public static string Slice(string source, int start) => source.Substring(start);
    public static string Slice(string source, int start, int end) => source.Substring(start, end - start);
    public static ByteStringView Slice(ByteStringView source, int start) => source.Slice(start);
    public static ByteStringView Slice(ByteStringView source, int start, int end) => source.Slice(start, end);
    public static List<T> Slice<T>(List<T> source, int start) => source is null && start == 0 ? null : source.GetRange(start, source.Count - start);
    public static List<T> Slice<T>(List<T> source, int start, int end) => source is null && start == 0 && end == 0 ? null : source.GetRange(start, end - start);
    public static GoListValues<T> RangeValues<T>(List<T> source) => new(source);
    public static IEnumerable<T> RangeValues<T>(IEnumerable<T> source) => source ?? [];
    public static IEnumerable<V> RangeValues<K, V>(Dictionary<K, V> source) => source?.Values ?? Enumerable.Empty<V>();
    public static IEnumerable<T> RangeKeys<T>(IEnumerable<T> source) => source ?? [];
    public static IEnumerable<int> RangeKeys<T>(List<T> source) => Enumerable.Range(0, source?.Count ?? 0);
    public static IEnumerable<int> RangeKeys(string source)
    {
        for (int i = 0; i < source.Length;)
        {
            yield return i;
            var(_, size) = Utf8.DecodeRuneInString(source.AsSpan(i));
            i += size;
        }
    }

    public static IEnumerable<ParsingContext> RangeKeys(ParsingContext count) => RangeKeys((int)count).Select(x => (ParsingContext)x);
    // Go integer range produces no values for zero or negative signed bounds.
    public static GoIntegerRange RangeKeys(int count) => new(count);
    public static IEnumerable<K> RangeKeys<K, V>(Dictionary<K, V> source) => source?.Keys ?? Enumerable.Empty<K>();
    public static GoListRange<T> Range<T>(List<T> source) => new(source);

    public static IEnumerable<KeyValuePair<int, int>> Range(string source)
    {
        for (int i = 0; i < source.Length;)
        {
            var(r, size) = Utf8.DecodeRuneInString(source.AsSpan(i));
            yield return new(i, r);
            i += size;
        }
    }

    public static IEnumerable<KeyValuePair<K, V>> Range<K, V>(Dictionary<K, V> source) => source ?? new();
}

internal static class GoSlices
{
    public static int Index<T>(List<T> values, T target) => values?.IndexOf(target) ?? -1;
    public static bool Contains<T>(List<T> values, T target) => Index(values, target) >= 0;
    public static bool Equal<T>(List<T> left, List<T> right)
    {
        int length = left?.Count ?? 0;
        if (length != (right?.Count ?? 0))
            return false;
        for (int i = 0; i < length; i++)
            if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                return false;
        return true;
    }

    // slices.Delete keeps the caller's captured header length, shifts the
    // backing values and clears the abandoned tail. The returned List owns
    // the shorter header, as with the existing managed Slice adapter.
    public static List<T> Delete<T>(List<T> values, int start, int end)
    {
        int length = values?.Count ?? 0;
        if (start < 0 || start > end || end > length)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (start == end)
            return values;
        int count = end - start;
        for (int i = start; i < length - count; i++)
            values[i] = values[i + count];
        for (int i = length - count; i < length; i++)
            values[i] = default;
        return values.GetRange(0, length - count);
    }

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
    public static bool IsDeclarationFile(this string value) => TsPath.getDeclarationFileExtensionFromNormalized(value ?? "") != "";
}
