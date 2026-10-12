using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectSessionSchedulingTests
{
    [Fact]
    public void AuthoredSchedulingDurations() => Assert.Equal(1, ProjectSessionSchedulingChecks.RunAll());
}
