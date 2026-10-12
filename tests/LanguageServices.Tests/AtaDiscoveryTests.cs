using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AtaDiscoveryTests
{
    [Fact]
    public void TestDiscoverTypings() => AtaDiscoveryChecks.Run("TestDiscoverTypings");
}
