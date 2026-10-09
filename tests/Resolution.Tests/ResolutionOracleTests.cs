using Resolution.Checks;
using Xunit;

namespace Resolution.Tests;
public sealed class ResolutionOracleTests
{
    public static IEnumerable<object[]> Cases => ResolutionCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalResolutionOutput(string name) => ResolutionCorpus.Run(name);
}
