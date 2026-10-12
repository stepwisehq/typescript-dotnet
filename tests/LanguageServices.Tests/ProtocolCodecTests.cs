using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProtocolCodecTests
{
    public static IEnumerable<object[]> Cases => ProtocolCodecChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalProtocolFunction(string name) => ProtocolCodecChecks.Run(name);
    [Fact]
    public void AuthoredJSONBoundaryControls() => Assert.Equal(7, ProtocolCodecChecks.RunJSONBoundaryControls());
    [Fact]
    public void AuthoredMapNameBoundaryControls() => Assert.Equal(3, ProtocolCodecChecks.RunMapNameBoundaryControls());
    [Fact]
    public void AuthoredZeroValueBoundaryControls() => Assert.Equal(5, ProtocolCodecChecks.RunZeroValueBoundaryControls());
    [Theory]
    [InlineData(16)]
    [InlineData(256)]
    [InlineData(2048)]
    [InlineData(4998)]
    public void AuthoredRecursiveModelDepth(int depth) => ProtocolCodecChecks.RunModelDepthBoundary(depth);
    [Theory]
    [InlineData(16)]
    [InlineData(256)]
    [InlineData(2048)]
    [InlineData(10000)]
    public void AuthoredRecursiveAnyDepth(int depth) => ProtocolCodecChecks.RunAnyDepthBoundary(depth);
}
