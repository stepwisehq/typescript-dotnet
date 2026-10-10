using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class SourceFileNameTests
{
    public static IEnumerable<object[]> Cases => SourceFileNameChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesFilenameRolesAndLifecycle(string name) => SourceFileNameChecks.Run(name);
}
