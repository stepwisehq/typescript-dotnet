using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class SemverTests
{
    public static IEnumerable<object[]> Cases => SemverChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => SemverChecks.Run(name);
}
