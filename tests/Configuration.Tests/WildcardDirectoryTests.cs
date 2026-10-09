using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class WildcardDirectoryTests
{
    public static IEnumerable<object[]> Cases => WildcardDirectoryChecks.Cases.Select(name => new object[] { name });

    [Theory, MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => WildcardDirectoryChecks.Run(name);
}
