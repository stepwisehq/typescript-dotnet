using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectGraphOwnershipTests
{
    [Fact]
    public void DiagnosticChangePredicate() => Assert.Equal(1, ProjectGraphOwnershipChecks.RunAuthoredControls());
}
