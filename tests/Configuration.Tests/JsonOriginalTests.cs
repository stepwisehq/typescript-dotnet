using Configuration.Checks;
using Xunit;
namespace Configuration.Tests;
public sealed class JsonOriginalTests
{
    public static IEnumerable<object[]> Cases => JsonOriginalChecks.Cases.Select(name => new object[] { name });
    [Theory, MemberData(nameof(Cases))]
    public void SelectedOriginalAssertions(string name) => JsonOriginalChecks.Run(name);
}
