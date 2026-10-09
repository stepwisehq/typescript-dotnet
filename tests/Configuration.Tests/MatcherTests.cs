using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class MatcherTests
{
    public static IEnumerable<object[]> Cases => MatcherChecks.Cases.Select(name => new object[] { name });

    [Theory, MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => MatcherChecks.Run(name);
}
