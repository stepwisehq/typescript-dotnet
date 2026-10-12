using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectSnapshotTests
{
    public static IEnumerable<object[]> Cases => ProjectSnapshotChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProjectSnapshotChecks.Run(name);
}
