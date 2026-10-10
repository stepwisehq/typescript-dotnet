using Program.Checks;
using Xunit;

namespace Program.Tests;
public sealed class CompilerMapperTests
{
    public static IEnumerable<object[]> Cases => CompilerMapperChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalOrExplicitConstructionSubset(string name) => CompilerMapperChecks.Run(name);
}
