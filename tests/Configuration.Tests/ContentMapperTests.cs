using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class ContentMapperTests
{
    public static IEnumerable<object[]> Cases => ContentMapperChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ContentMapperChecks.Run(name);
}
