using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectSnapshotLifetimeTests
{
    [Fact]
    public void OriginalSnapshot() => ProjectSnapshotLifetimeChecks.TestSnapshot();
    [Fact]
    public void OriginalProjectIDNarrowing() => ProjectSnapshotLifetimeChecks.TestProjectIDNarrowing();
    [Fact]
    public void OriginalRefCountingCaches() => ProjectSnapshotLifetimeChecks.TestRefCountingCaches();
}
