using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class LsUtilityTests
{
    public static IEnumerable<object[]> Cases => LsUtilityChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => LsUtilityChecks.Run(name);
}
