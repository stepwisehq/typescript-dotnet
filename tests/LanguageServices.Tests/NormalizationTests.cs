using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class NormalizationTests
{
    [Fact]
    public void OriginalSegmentAssertions() => NormalizationChecks.Run("TestDecomposeSegment");
    public static IEnumerable<object[]> ConformanceChannels => NormalizationConformanceChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(ConformanceChannels))]
    public void OriginalNfdConformanceChannel(string name) => NormalizationConformanceChecks.Run(name);
    public static IEnumerable<object[]> StringChannels => NormalizationStringChannels.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(StringChannels))]
    public void OriginalStringSetupChannel(string name) => NormalizationStringChannels.Run(name);
    [Fact]
    public void OriginalNfdBootstrapChannel() => Assert.Equal(1, NormalizationBootstrapChecks.RunAll());
    [Fact]
    public void OriginalNfdSpanChannel() => Assert.Equal(1, NormalizationSpanChecks.RunAll());
    [Fact]
    public void OriginalCanonicalPropertyChannel() => Assert.Equal(1, NormalizationPropertyChecks.RunAll());
    [Fact]
    public void AuthoredNfdStringObservations() => Assert.Equal(8, NormalizationChecks.RunAuthoredControls());
}
