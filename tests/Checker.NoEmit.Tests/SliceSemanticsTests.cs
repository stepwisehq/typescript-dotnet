using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class SliceSemanticsTests
{
    [Fact]
    public void NilAndAllocatedEmptyRemainDistinct() => SliceConformanceChecks.NilAndAllocatedEmptyRemainDistinct();

    [Fact]
    public void DescriptorCopySharesWritesButNotLength() => SliceConformanceChecks.DescriptorCopySharesWritesButNotLength();

    [Fact]
    public void ArrayAndSpanViewsShareTheDeclaredBacking() => SliceConformanceChecks.ArrayAndSpanViewsShareTheDeclaredBacking();

    [Fact]
    public void TwoIndexReslicingUsesCapacityAndPreservesOffset() => SliceConformanceChecks.TwoIndexReslicingUsesCapacityAndPreservesOffset();

    [Fact]
    public void ThreeIndexSliceLimitsAppendSharing() => SliceConformanceChecks.ThreeIndexSliceLimitsAppendSharing();

    [Fact]
    public void MakeAndIndexBoundsFailWithoutUsingSpareCapacity() => SliceConformanceChecks.MakeAndIndexBoundsFailWithoutUsingSpareCapacity();

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 1)]
    [InlineData(0, 5)]
    [InlineData(5, 5)]
    public void InvalidTwoIndexBoundsFail(int low, int high) => SliceConformanceChecks.InvalidTwoIndexBoundsFail(low, high);

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(2, 1, 3)]
    [InlineData(0, 3, 2)]
    [InlineData(0, 1, 5)]
    public void InvalidThreeIndexBoundsFail(int low, int high, int max) => SliceConformanceChecks.InvalidThreeIndexBoundsFail(low, high, max);

    [Fact]
    public void AppendReusesCapacityThenDetachesOnGrowth() => SliceConformanceChecks.AppendReusesCapacityThenDetachesOnGrowth();

    [Fact]
    public void AppendFromOverlappingSliceUsesOriginalSourceValues() => SliceConformanceChecks.AppendFromOverlappingSliceUsesOriginalSourceValues();

    [Fact]
    public void AppendFromSpanWorksForAliasedAndEmptySources() => SliceConformanceChecks.AppendFromSpanWorksForAliasedAndEmptySources();

    [Fact]
    public void CopyHandlesOverlapInBothDirectionsAndReturnsMinimumLength() => SliceConformanceChecks.CopyHandlesOverlapInBothDirectionsAndReturnsMinimumLength();

    [Fact]
    public void IterationCapturesHeaderButObservesSharedElementWrites() => SliceConformanceChecks.IterationCapturesHeaderButObservesSharedElementWrites();

    [Fact]
    public void PushPopAndPoolResetRetainBackingAndRestoreLogicalContents() => SliceConformanceChecks.PushPopAndPoolResetRetainBackingAndRestoreLogicalContents();

}
