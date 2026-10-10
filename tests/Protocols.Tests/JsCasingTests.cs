using TypeSystem.Checks;
using Xunit;

namespace Protocols.Tests;

public sealed class JsCasingTests
{
    [Fact]
    public void TestJSCasing() => JsCasingChecks.Run("TestJSCasing");
}
