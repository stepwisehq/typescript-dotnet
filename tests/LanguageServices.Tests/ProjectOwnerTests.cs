using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectOwnerTests
{
    public static IEnumerable<object[]> CacheNames => ProjectCacheChecks.Names.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(CacheNames))]
    public void OriginalCaches(string name) => ProjectCacheChecks.Run(name);
    [Fact]
    public void OriginalDirtyProxy() => ProjectDirtyChecks.RunAll();
    [Fact]
    public void ComparerCopyOwnership() => ProjectDirtyChecks.RunAuthoredControls();
    [Fact]
    public void OriginalBackgroundQueue() => ProjectBackgroundChecks.RunAll();
}
