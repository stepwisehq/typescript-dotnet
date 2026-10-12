using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AtaInstallTests
{
    [Fact]
    public void TestInstallNpmPackages() => AtaInstallChecks.Run("TestInstallNpmPackages");
}
