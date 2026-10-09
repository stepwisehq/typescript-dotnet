using TypeScript.Projects;
using Xunit;

namespace Checker.Tests;
public sealed class ProjectControlsTests
{
    public static IEnumerable<object[]> Cases => ProjectControls.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SharedProjectControls(string name) => ProjectControls.Run(name);
}
