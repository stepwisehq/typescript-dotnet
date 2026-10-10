// New managed compatibility support for Go slice value semantics.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// NextCapacity adapts Go 1.27.1 runtime/slice.go:nextslicecap.
// Copyright 2009 The Go Authors. All rights reserved.
// Go-derived growth policy is licensed under the BSD license in licenses/Go-LICENSE.txt.
#nullable enable
namespace TypeScript.Scanner;

// A copied header shares array storage, while its offset/length/capacity remain
// independent. No Span is stored, so slices can be fields of ordinary objects.
public readonly struct Slice<T>
{
    private readonly T[]? storage;
    private readonly int offset;
    public int Length { get; }
    // Managed read-only spelling compatibility; the stored Go header length is authoritative.
    public int Count => Length;
    public int Capacity { get; }
    public bool IsNil => storage is null;

    private Slice(T[]? storage, int offset, int length, int capacity)
    {
        this.storage = storage;
        this.offset = offset;
        Length = length;
        Capacity = capacity;
    }

    public static Slice<T> Make(int length) => Make(length, length);

    public static Slice<T> Make(int length, int capacity)
    {
        if (length < 0 || capacity < length || capacity > Array.MaxLength)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Slice requires 0 <= length <= capacity <= Array.MaxLength.");
        // Array.Empty preserves allocated-empty versus nil without allocating.
        return new(capacity == 0 ? Array.Empty<T>() : new T[capacity], 0, length, capacity);
    }

    public static Slice<T> FromArray(T[]? array) => array is null
        ? default : new(array, 0, array.Length, array.Length);

    public T this[int index]
    {
        get
        {
            CheckIndex(index);
            return storage![offset + index];
        }
        set
        {
            CheckIndex(index);
            storage![offset + index] = value;
        }
    }

    private void CheckIndex(int index)
    {
        if ((uint)index >= (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    public Span<T> AsSpan() => storage.AsSpan(offset, Length);

    // Go's two-index slice permits high beyond len up to cap. An omitted high
    // uses the current length; slicing adjusts capacity relative to the new low.
    public Slice<T> Slice2(int low) => Slice2(low, Length);
    public Slice<T> Slice2(int low, int high) => Slice3(low, high, Capacity);

    public Slice<T> Slice3(int low, int high, int max)
    {
        if (low < 0 || high < low || max < high || max > Capacity)
            throw new ArgumentOutOfRangeException(nameof(high), "Slice requires 0 <= low <= high <= max <= capacity.");
        return new(storage, offset + low, high - low, max - low);
    }

    public Slice<T> Append(T value)
    {
        int length = AppendedLength(1);
        var result = WithCapacity(length);
        result.storage![result.offset + Length] = value;
        return new(result.storage, result.offset, length, result.Capacity);
    }

    public Slice<T> Append(Slice<T> values) => Append(values.AsSpan());

    public Slice<T> Append(ReadOnlySpan<T> values)
    {
        if (values.IsEmpty)
            return this;
        int length = AppendedLength(values.Length);
        var result = WithCapacity(length);
        // CopyTo has memmove semantics for overlapping spans. On growth, values
        // may still point into the old array; its contents remain available.
        values.CopyTo(result.storage.AsSpan(result.offset + Length, values.Length));
        return new(result.storage, result.offset, length, result.Capacity);
    }

    public int CopyFrom(Slice<T> source) => CopyFrom(source.AsSpan());

    // core.go:183 Same compares length and first storage address, ignoring cap.
    public bool SameStorageRange(Slice<T> other) => Length == other.Length &&
        (Length == 0 || ReferenceEquals(storage, other.storage) && offset == other.offset);

    // slices.Insert: grow the owned header if necessary, then move the tail.
    public Slice<T> Insert(int index, T value)
    {
        if ((uint)index > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        var result = Append(default(T)!);
        result.AsSpan().Slice(index, Length - index).CopyTo(result.AsSpan().Slice(index + 1));
        result[index] = value;
        return result;
    }
    public int CopyFrom(ReadOnlySpan<T> source)
    {
        int count = Math.Min(Length, source.Length);
        source[..count].CopyTo(AsSpan()[..count]);
        return count;
    }

    private int AppendedLength(int count)
    {
        if (count > Array.MaxLength - Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Slice exceeds Array.MaxLength.");
        return Length + count;
    }

    private Slice<T> WithCapacity(int required)
    {
        if (required <= Capacity)
            return this;
        int capacity = NextCapacity(required, Capacity);
        var array = new T[capacity];
        AsSpan().CopyTo(array);
        return new(array, 0, Length, capacity);
    }

    // Go runtime/slice.go:nextslicecap nominal policy: start at the requested
    // length, double below 256, then smoothly approach 1.25x growth. CLR array
    // allocation omits Go's element-size/allocator size-class rounding, so
    // automatically grown capacities do NOT promise exact Go runtime parity.
    // Explicit Make/Slice3 capacity and resulting storage aliases are exact.
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
        } while (capacity < required);
        return (int)Math.Min(capacity, Array.MaxLength);
    }

    public Enumerator GetEnumerator() => new(this);

    // Range captures the descriptor but reads elements from shared storage.
    public struct Enumerator
    {
        private readonly Slice<T> slice;
        private int index;
        internal Enumerator(Slice<T> slice) => (this.slice, index) = (slice, -1);
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
