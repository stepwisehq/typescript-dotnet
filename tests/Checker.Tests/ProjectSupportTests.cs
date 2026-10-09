using TypeScript.Projects;
using Xunit;

namespace Checker.Tests;
public sealed class ProjectSupportTests
{
    public static IEnumerable<object[]> Cases => ProjectSupportChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SharedProjectSupportAssertions(string name) => ProjectSupportChecks.Run(name);
}
