using Emit.Checks;
using Xunit;
namespace Emit.Tests;
public sealed class SourceMapTests
{
    public static IEnumerable<object[]> OriginalCases => SourceMapChecks.Names.Select(name => new object[] { name });
    [Theory, MemberData(nameof(OriginalCases))]
    public void OriginalSourceMap(string name) => SourceMapChecks.Run(name);
    [Fact]
    public void AuthoredSourceMapControls() => Assert.Equal(SourceMapChecks.AuthoredCaseCount, SourceMapChecks.RunAuthoredControls());
}
