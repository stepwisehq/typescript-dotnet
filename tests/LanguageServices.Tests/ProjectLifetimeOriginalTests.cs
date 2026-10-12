using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class ProjectLifetimeOriginalTests
{
    [Fact]
    public void Original() => ProjectLifetimeOriginalChecks.TestProjectLifetime();
}
