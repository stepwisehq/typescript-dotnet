using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class CachedVfsTests
{
    public static IEnumerable<object[]> Cases => CachedVfsChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => CachedVfsChecks.Run(name);
}
