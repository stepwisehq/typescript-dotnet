// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Managed owner/header adaptation for regexp/syntax Regexp.Sub0[1] and Rune0[2].
// The original syntax-node owner retains its inline arrays; growth owns a detached array.
using System.Runtime.InteropServices;

namespace TypeScript.RegularExpressions.Syntax;
internal interface IRegexInlineStorage<T>
{
    ref T Element(int index);
}

internal readonly struct RegexSlice<T>
{
    private readonly T[]? array;
    private readonly IRegexInlineStorage<T>? inline;
    private readonly int offset;
    internal int Length { get; }
    internal int Count => Length;
    internal int Capacity { get; }
    internal bool IsNil => array is null && inline is null;

    private RegexSlice(T[]? array, IRegexInlineStorage<T>? inline, int offset, int length, int capacity)
    {
        this.array = array;
        this.inline = inline;
        this.offset = offset;
        Length = length;
        Capacity = capacity;
    }

    internal static RegexSlice<T> Inline(IRegexInlineStorage<T> owner, int capacity) => new(null, owner, 0, capacity, capacity);
    internal static RegexSlice<T> FromArray(T[]? values) => values is null ? default : new(values, null, 0, values.Length, values.Length);
    internal T this[int index]
    {
        get
        {
            CheckIndex(index);
            return array is not null ? array[offset + index] : inline!.Element(offset + index);
        }

        set
        {
            CheckIndex(index);
            if (array is not null)
                array[offset + index] = value;
            else
                inline!.Element(offset + index) = value;
        }
    }

    private void CheckIndex(int index)
    {
        if ((uint)index >= (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal Span<T> AsSpan() => Length == 0 ? default : array is not null ? array.AsSpan(offset, Length) : inline is not null ? MemoryMarshal.CreateSpan(ref inline.Element(offset), Length) : default;
    internal RegexSlice<T> Slice2(int start) => Slice2(start, Length);
    internal RegexSlice<T> Slice2(int start, int end)
    {
        if (start < 0 || end < start || end > Capacity)
            throw new ArgumentOutOfRangeException(nameof(end));
        return new(array, inline, offset + start, end - start, Capacity - start);
    }

    internal RegexSlice<T> Append(T value)
    {
        var result = WithLength(checked(Length + 1));
        result[Length] = value;
        return result;
    }

    // Go append(slice, first, second): one growth decision, no temporary managed array
    // or accumulating stack allocation in rune-fold/class loops.
    internal RegexSlice<T> Append(T first, T second)
    {
        var result = WithLength(checked(Length + 2));
        result[Length] = first;
        result[Length + 1] = second;
        return result;
    }

    internal RegexSlice<T> Append(RegexSlice<T> values) => Append(values.AsSpan());
    internal RegexSlice<T> Append(ReadOnlySpan<T> values)
    {
        var result = WithLength(checked(Length + values.Length));
        values.CopyTo(result.AsSpan()[Length..]);
        return result;
    }

    private RegexSlice<T> WithLength(int length)
    {
        if (length <= Capacity)
            return new(array, inline, offset, length, Capacity);
        // Accepted shared runtime/slice.go nominal growth policy, with the same required prefix copy.
        int capacity = NextCapacity(length, Capacity);
        var storage = new T[capacity];
        AsSpan().CopyTo(storage);
        return new(storage, null, 0, length, capacity);
    }

    // Go1.27.1 runtime/slice.go nextslicecap, the same nominal policy as the accepted shared slice.
    private static int NextCapacity(int required, int oldCapacity)
    {
        long capacity = oldCapacity;
        long doubled = capacity * 2;
        if (required > doubled)
            return required;
        if (oldCapacity < 256)
            return (int)Math.Min(doubled, Array.MaxLength);
        do
        {
            capacity += (capacity + 3 * 256) >> 2;
        }
        while (capacity < required);
        return (int)Math.Min(capacity, Array.MaxLength);
    }

    public Enumerator GetEnumerator() => new(this);
    public struct Enumerator
    {
        private readonly RegexSlice<T> slice;
        private int index;
        internal Enumerator(RegexSlice<T> slice)
        {
            this.slice = slice;
            index = -1;
        }

        public T Current => slice[index];

        public bool MoveNext()
        {
            if (index + 1 >= slice.Length)
                return false;
            index++;
            return true;
        }
    }
}
