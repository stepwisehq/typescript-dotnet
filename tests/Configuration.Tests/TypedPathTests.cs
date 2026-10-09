using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class TypedPathTests
{
    public static IEnumerable<object[]> Cases => TypedPathChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedTypedPathAssertions(string name) => TypedPathChecks.Run(name);
}
