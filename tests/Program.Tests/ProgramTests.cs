using Program.Checks;
using Xunit;

namespace Program.Tests;
public sealed class ProgramTests
{
    public static IEnumerable<object[]> Cases => ProgramChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProgramChecks.Run(name);
}
