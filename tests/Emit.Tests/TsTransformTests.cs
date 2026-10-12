using Emit.Checks;
using Xunit;
namespace Emit.Tests;
public sealed class TsTransformTests
{
    public static IEnumerable<object[]> MetadataCases => MetadataChecks.Names.Select(name => new object[] { name });
    public static IEnumerable<object[]> RuntimeCases => TsRuntimeChecks.Names.Select(name => new object[] { name });
    [Theory, MemberData(nameof(MetadataCases))]
    public void AuthoredMetadata(string name) => MetadataChecks.Run(name);
    [Theory, MemberData(nameof(RuntimeCases))]
    public void AuthoredRuntimeAndLegacy(string name) => TsRuntimeChecks.Run(name);
    [Fact]
    public void AuthoredMultiMap() => Assert.Equal(7, MultiMapControls.RunAll());
}
