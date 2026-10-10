using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class IgnoredPathTests
{
    public static IEnumerable<object[]> Cases => IgnoredPathChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => IgnoredPathChecks.Run(name);
}
