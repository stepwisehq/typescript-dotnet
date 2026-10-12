using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectSessionTests
{
    [Fact]
    public void OriginalSession() => ProjectSessionChecks.TestSession();
    [Fact]
    public void OriginalClose() => ProjectSessionChecks.TestSessionCloseDoesNotBlockOnIdleCacheCleanTimer();
}
