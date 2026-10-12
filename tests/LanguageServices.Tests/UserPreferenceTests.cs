using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class UserPreferenceTests
{
    public static IEnumerable<object[]> Cases => UserPreferenceChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => UserPreferenceChecks.Run(name);
    [Fact]
    public void Go64PreferenceWidths() => Assert.Equal(1, UserPreferenceChecks.RunAuthoredControls());
}
