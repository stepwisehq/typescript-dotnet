using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class ModuleRegexTests
{
    [Fact]
    public void AuthoredExclusionSyntaxAndInvalidCache() => Assert.Equal(1, ModuleRegexChecks.RunAll());
}
