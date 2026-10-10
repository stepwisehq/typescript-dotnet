using TypeSystem.Checks;
using Xunit;

namespace TypeSystem.Tests;

public sealed class JsCasingTests
{
    [Fact]
    public void TestJSCasing() => JsCasingChecks.Run("TestJSCasing");
}
