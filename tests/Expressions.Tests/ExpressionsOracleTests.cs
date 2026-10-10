using Expressions.Checks;
using Xunit;

namespace Expressions.Tests;

public sealed class ExpressionsOracleTests
{
    public static IEnumerable<object[]> Cases => ExpressionsCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSemanticProgramOutput(string name) => ExpressionsCorpus.Run(name);
}
