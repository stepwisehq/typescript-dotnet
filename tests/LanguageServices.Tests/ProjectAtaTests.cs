using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectAtaTests
{
    [Fact]
    public void OriginalATA() => ProjectAtaChecks.TestATA();
}
