using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class ProjectExtendedConfigOwnershipTests
{
    [Fact]
    public void Original() => ProjectExtendedConfigOwnershipChecks.TestExtendedConfigCacheOwnership();
}
