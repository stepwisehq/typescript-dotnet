using Declarations.Checks;
using Xunit;

namespace Declarations.Tests;

public sealed class CheckerOriginalTests
{
    public static IEnumerable<object[]> Cases => CheckerOriginalChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => CheckerOriginalChecks.Run(name);
}
