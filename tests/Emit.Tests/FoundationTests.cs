using Emit.Checks;
using Xunit;
namespace Emit.Tests;
public sealed class FoundationTests
{
    public static IEnumerable<object[]> AuthoredCases => FoundationChecks.Names.Select(name => new object[] { name });
    [Theory, MemberData(nameof(AuthoredCases))]
    public void AuthoredEmitFoundation(string name) => FoundationChecks.Run(name);
}
