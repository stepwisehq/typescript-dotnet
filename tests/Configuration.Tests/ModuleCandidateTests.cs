using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class ModuleCandidateTests
{
    public static IEnumerable<object[]> Cases => ModuleCandidateChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalCandidateAssertions(string name) => ModuleCandidateChecks.Run(name);
}
