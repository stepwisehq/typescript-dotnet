using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class ShowConfigTests
{
    public static IEnumerable<object[]> Cases => ShowConfigChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ShowConfigChecks.Run(name);
}
