using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class CompilerOptionTests
{
    public static IEnumerable<object[]> Cases => CompilerOptionChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => CompilerOptionChecks.Run(name);
}
