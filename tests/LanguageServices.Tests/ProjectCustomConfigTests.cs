using Xunit;
using LanguageServices.Checks;
namespace LanguageServices.Tests;
public sealed class ProjectCustomConfigTests
{
    [Fact]
    public void OriginalCustomConfigFileName() => ProjectCustomConfigChecks.Original();
}
