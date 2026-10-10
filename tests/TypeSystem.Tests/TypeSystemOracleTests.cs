using TypeSystem.Checks;
using Xunit;

namespace TypeSystem.Tests;

public sealed class TypeSystemOracleTests
{
    public static IEnumerable<object[]> Cases => TypeSystemCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSemanticProgramOutput(string name) => TypeSystemCorpus.Run(name);
}
