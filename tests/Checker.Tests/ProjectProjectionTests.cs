using TypeScript.Checking.ProjectsValidation;
using Xunit;

namespace TypeScript.Checking.Tests;
public sealed class ProjectProjectionTests
{
    [Fact]
    public void PreservesLiteralAndIdentifierLookalikesWhileRelocatingOwnedImportTypes() => Runner.RunProjectionControl();
}
