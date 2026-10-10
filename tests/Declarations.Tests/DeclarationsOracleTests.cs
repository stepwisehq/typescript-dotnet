using Declarations.Checks;
using Xunit;

namespace Declarations.Tests;

public sealed class DeclarationsOracleTests
{
    public static IEnumerable<object[]> Cases => DeclarationsCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSemanticProgramOutput(string name) => DeclarationsCorpus.Run(name);
}
