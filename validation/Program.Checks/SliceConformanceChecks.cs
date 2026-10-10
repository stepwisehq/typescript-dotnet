using TypeScript.Scanner;

namespace Program.Checks;

// Go language-spec slice/append/copy/range behavior. Growth capacity is a managed
// policy: these assertions constrain aliasing and visible values, not Go's allocator.
public static class SliceConformanceChecks
{
    public static void NilAndAllocatedEmptyRemainDistinct()
    {
        Slice<int> nil = default;
        var empty = Slice<int>.Make(0);
        Check.True(nil.IsNil);
        Check.False(empty.IsNil);
        Check.True(Slice<int>.FromArray(null).IsNil);
        Check.False(Slice<int>.FromArray(Array.Empty<int>()).IsNil);
        Check.Equal(0, nil.Length);
        Check.Equal(0, nil.Capacity);
        Check.Equal(0, empty.Length);
        Check.Equal(0, empty.Capacity);
        Check.True(nil.Slice2(0).IsNil);
        Check.True(nil.Slice3(0, 0, 0).IsNil);
        Check.False(empty.Slice2(0).IsNil);
        Check.True(nil.Append(ReadOnlySpan<int>.Empty).IsNil);
        Check.True(nil.Append(default(Slice<int>)).IsNil);
        Check.True(nil.Append(empty).IsNil);
        Check.False(empty.Append(ReadOnlySpan<int>.Empty).IsNil);
        Check.Empty(nil.AsSpan().ToArray());
    }

    public static void DescriptorCopySharesWritesButNotLength()
    {
        var original = Slice<int>.Make(2, 4);
        original[0] = 11;
        original[1] = 22;
        var copy = original;
        copy[0] = 33;
        var appended = copy.Append(44);
        Check.Equal(2, original.Length);
        Check.Equal(2, copy.Length);
        Check.Equal(3, appended.Length);
        Check.Sequence(new[] { 33, 22 }, original.AsSpan().ToArray());
        Check.Sequence(new[] { 33, 22, 44 }, original.Slice2(0, 3).AsSpan().ToArray());
    }

    public static void ArrayAndSpanViewsShareTheDeclaredBacking()
    {
        int[] array = [1, 2, 3];
        var slice = Slice<int>.FromArray(array);
        slice[1] = 8;
        array[2] = 9;
        slice.AsSpan()[0] = 5;
        Check.Sequence(new[] { 5, 8, 9 }, array);
        Check.Sequence(array, slice.AsSpan().ToArray());
    }

    public static void TwoIndexReslicingUsesCapacityAndPreservesOffset()
    {
        var original = Slice<int>.FromArray([1, 2, 3, 4]);
        var middle = original.Slice2(1, 2);
        Check.Equal(1, middle.Length);
        Check.Equal(3, middle.Capacity);
        Check.Sequence(new[] { 2, 3, 4 }, middle.Slice2(0, 3).AsSpan().ToArray());
        Check.Sequence(new[] { 4 }, middle.Slice2(2, 3).AsSpan().ToArray());
        middle[0] = 20;
        Check.Equal(20, original[1]);
        var tail = middle.Slice2(1);
        Check.Equal(0, tail.Length);
        Check.Equal(2, tail.Capacity);
        Check.Throws<ArgumentOutOfRangeException>(() => middle.Slice2(2));
    }

    public static void ThreeIndexSliceLimitsAppendSharing()
    {
        var original = Slice<int>.FromArray([1, 2, 3, 4]);
        var limited = original.Slice3(1, 2, 2);
        Check.Equal(1, limited.Length);
        Check.Equal(1, limited.Capacity);
        var grown = limited.Append(99);
        Check.Sequence(new[] { 1, 2, 3, 4 }, original.AsSpan().ToArray());
        limited[0] = 8;
        Check.Equal(8, original[1]);
        Check.Sequence(new[] { 2, 99 }, grown.AsSpan().ToArray());
        var spare = original.Slice3(1, 2, 3).Append(77);
        Check.Sequence(new[] { 8, 77 }, spare.AsSpan().ToArray());
        Check.Equal(77, original[2]);
    }

    public static void MakeAndIndexBoundsFailWithoutUsingSpareCapacity()
    {
        Check.Throws<ArgumentOutOfRangeException>(() => Slice<int>.Make(-1));
        Check.Throws<ArgumentOutOfRangeException>(() => Slice<int>.Make(1, 0));
        Check.Throws<ArgumentOutOfRangeException>(() => Slice<int>.Make(0, -1));
        var slice = Slice<int>.Make(1, 4);
        slice[0] = 7;
        Check.Throws<ArgumentOutOfRangeException>(() => { _ = slice[-1]; });
        Check.Throws<ArgumentOutOfRangeException>(() => { _ = slice[1]; });
        Check.Throws<ArgumentOutOfRangeException>(() => slice[1] = 9);
        Check.Sequence(new[] { 7, 0, 0, 0 }, slice.Slice2(0, 4).AsSpan().ToArray());
    }

    public static void InvalidTwoIndexBoundsFail(int low, int high)
    {
        var slice = Slice<int>.Make(2, 4);
        Check.Throws<ArgumentOutOfRangeException>(() => slice.Slice2(low, high));
    }

    public static void InvalidThreeIndexBoundsFail(int low, int high, int max)
    {
        var slice = Slice<int>.Make(2, 4);
        Check.Throws<ArgumentOutOfRangeException>(() => slice.Slice3(low, high, max));
    }

    public static void AppendReusesCapacityThenDetachesOnGrowth()
    {
        var backing = Slice<int>.Make(1, 3);
        backing[0] = 10;
        var appended = backing.Append(20).Append(30);
        Check.Sequence(new[] { 10, 20, 30 }, backing.Slice2(0, 3).AsSpan().ToArray());
        var grown = appended.Append(40);
        Check.True(grown.Capacity >= grown.Length);
        Check.Sequence(new[] { 10, 20, 30, 40 }, grown.AsSpan().ToArray());
        grown[0] = 99;
        Check.Equal(10, backing[0]);
        appended[1] = 88;
        Check.Equal(20, grown[1]);
    }

    public static void AppendFromOverlappingSliceUsesOriginalSourceValues()
    {
        var backing = Slice<int>.Make(4, 6);
        ReadOnlySpan<int> values = [1, 2, 3, 4];
        values.CopyTo(backing.AsSpan());
        var result = backing.Slice2(0, 1).Append(backing.Slice2(0, 3));
        Check.Sequence(new[] { 1, 1, 2, 3 }, result.AsSpan().ToArray());
        Check.Equal(4, backing.Length);
        Check.Sequence(new[] { 1, 1, 2, 3 }, backing.AsSpan().ToArray());
        var full = Slice<int>.FromArray([1, 2, 3]);
        var detached = full.Slice2(1).Append(full);
        Check.Sequence(new[] { 2, 3, 1, 2, 3 }, detached.AsSpan().ToArray());
        Check.Sequence(new[] { 1, 2, 3 }, full.AsSpan().ToArray());
        detached[0] = 99;
        Check.Equal(2, full[1]);
    }

    public static void AppendFromSpanWorksForAliasedAndEmptySources()
    {
        var backing = Slice<int>.Make(4, 6);
        ReadOnlySpan<int> values = [1, 2, 3, 4];
        values.CopyTo(backing.AsSpan());
        var prefix = backing.Slice2(0, 1);
        var result = prefix.Append(backing.AsSpan()[..3]);
        Check.Sequence(new[] { 1, 1, 2, 3 }, result.AsSpan().ToArray());
        var unchanged = result.Append(ReadOnlySpan<int>.Empty);
        Check.Equal(result.Length, unchanged.Length);
        Check.Equal(result.Capacity, unchanged.Capacity);
        unchanged[0] = 9;
        Check.Equal(9, result[0]);
    }

    public static void CopyHandlesOverlapInBothDirectionsAndReturnsMinimumLength()
    {
        var right = Slice<int>.FromArray([1, 2, 3, 4]);
        Check.Equal(3, right.Slice2(1).CopyFrom(right.Slice2(0, 3)));
        Check.Sequence(new[] { 1, 1, 2, 3 }, right.AsSpan().ToArray());
        var left = Slice<int>.FromArray([1, 2, 3, 4]);
        Check.Equal(3, left.Slice2(0, 3).CopyFrom(left.AsSpan()[1..]));
        Check.Sequence(new[] { 2, 3, 4, 4 }, left.AsSpan().ToArray());
        var shortDestination = Slice<int>.Make(2, 5);
        Check.Equal(2, shortDestination.CopyFrom(left));
        Check.Sequence(new[] { 2, 3 }, shortDestination.AsSpan().ToArray());
        Check.Equal(2, shortDestination.Length);
        Check.Equal(0, default(Slice<int>).CopyFrom(left));
    }

    public static void IterationCapturesHeaderButObservesSharedElementWrites()
    {
        var slice = Slice<int>.Make(2, 4);
        slice[0] = 1;
        slice[1] = 2;
        List<int> visited = [];
        foreach (int value in slice)
        {
            visited.Add(value);
            if (visited.Count == 1)
            {
                slice = slice.Append(3);
                slice[1] = 20;
            }
        }
        Check.Sequence(new[] { 1, 20 }, visited);
        Check.Sequence(new[] { 1, 20, 3 }, slice.AsSpan().ToArray());
        Check.Equal(3, slice.Length);
        var detached = Slice<int>.FromArray([1, 2]);
        List<int> detachedVisited = [];
        foreach (int value in detached)
        {
            detachedVisited.Add(value);
            if (detachedVisited.Count == 1)
            {
                detached = detached.Append(3);
                detached[1] = 20;
            }
        }
        Check.Sequence(new[] { 1, 2 }, detachedVisited);
        Check.Sequence(new[] { 1, 20, 3 }, detached.AsSpan().ToArray());
        List<int> nilVisited = [];
        foreach (int value in default(Slice<int>))
            nilVisited.Add(value);
        Check.Empty(nilVisited);
    }

    public static void PushPopAndPoolResetRetainBackingAndRestoreLogicalContents()
    {
        var stack = Slice<int>.Make(0, 3);
        stack = stack.Append(10).Append(20);
        var captured = stack;
        stack = stack.Slice2(0, stack.Length - 1).Append(30);
        Check.Sequence(new[] { 10, 30 }, stack.AsSpan().ToArray());
        Check.Sequence(new[] { 10, 30 }, captured.AsSpan().ToArray());
        stack = stack.Slice2(0, 0);
        Check.Equal(0, stack.Length);
        Check.Equal(3, stack.Capacity);
        Check.Sequence(new[] { 10, 30 }, captured.AsSpan().ToArray());
        stack = stack.Append(40);
        Check.Sequence(new[] { 40 }, stack.AsSpan().ToArray());
        Check.Sequence(new[] { 40, 30 }, captured.AsSpan().ToArray());
    }

    // The conventional Native AOT consumer executes exactly the same assertions
    // as xUnit, without reflection discovery or a separate implementation.
    public static int RunAll()
    {
        NilAndAllocatedEmptyRemainDistinct();
        DescriptorCopySharesWritesButNotLength();
        ArrayAndSpanViewsShareTheDeclaredBacking();
        TwoIndexReslicingUsesCapacityAndPreservesOffset();
        ThreeIndexSliceLimitsAppendSharing();
        MakeAndIndexBoundsFailWithoutUsingSpareCapacity();
        AppendReusesCapacityThenDetachesOnGrowth();
        AppendFromOverlappingSliceUsesOriginalSourceValues();
        AppendFromSpanWorksForAliasedAndEmptySources();
        CopyHandlesOverlapInBothDirectionsAndReturnsMinimumLength();
        IterationCapturesHeaderButObservesSharedElementWrites();
        PushPopAndPoolResetRetainBackingAndRestoreLogicalContents();
        InvalidTwoIndexBoundsFail(-1, 0);
        InvalidTwoIndexBoundsFail(0, -1);
        InvalidTwoIndexBoundsFail(2, 1);
        InvalidTwoIndexBoundsFail(0, 5);
        InvalidTwoIndexBoundsFail(5, 5);
        InvalidThreeIndexBoundsFail(-1, 0, 0);
        InvalidThreeIndexBoundsFail(0, -1, 0);
        InvalidThreeIndexBoundsFail(0, 0, -1);
        InvalidThreeIndexBoundsFail(2, 1, 3);
        InvalidThreeIndexBoundsFail(0, 3, 2);
        InvalidThreeIndexBoundsFail(0, 1, 5);
        return 23;
    }

    private static class Check
    {
        public static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Slice condition must be true.");
        }
        public static void False(bool value) => True(!value);
        public static void Equal(int expected, int actual)
        {
            if (expected != actual)
                throw new InvalidOperationException($"Slice expected {expected}, actual {actual}.");
        }
        public static void Sequence(IEnumerable<int> expected, IEnumerable<int> actual)
        {
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException("Slice visible values differ.");
        }
        public static void Empty(IEnumerable<int> values) => True(!values.Any());
        public static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Slice bounds must fail with " + typeof(T).Name);
        }
    }
}
