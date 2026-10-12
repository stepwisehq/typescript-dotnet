using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectConfigChangeTests
{
    [Fact]
    public void Original() => ProjectConfigChangeChecks.TestConfigFileChanges();
}
