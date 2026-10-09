using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class LocaleTests
{
    public static IEnumerable<object[]> Cases => LocaleChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedOriginalAssertions(string name) => LocaleChecks.Run(name);
}
