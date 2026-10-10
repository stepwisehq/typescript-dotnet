using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class CheckerPoolTests
{
    public static IEnumerable<object[]> Cases => CheckerPoolChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => CheckerPoolChecks.Run(name);
}
