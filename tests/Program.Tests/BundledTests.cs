using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class BundledTests
{
    public static IEnumerable<object[]> Cases => BundledChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => BundledChecks.Run(name);
}
