using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AtaPackageNameTests
{
    [Fact]
    public void Original() => AtaPackageNameChecks.RunAll();
}
