using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class NativePathTests
{
    public static IEnumerable<object[]> Cases => NativePathChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedNativePathAssertions(string name) => NativePathChecks.Run(name);
}
