using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class OsFileSystemTests
{
    public static IEnumerable<object[]> Cases => OsFileSystemChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedOriginalAndNativeBoundaryAssertions(string name) => OsFileSystemChecks.Run(name);
}
