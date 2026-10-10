using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class KnownSymlinkTests
{
    public static IEnumerable<object[]> Cases => KnownSymlinkChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => KnownSymlinkChecks.Run(name);
}
