using Emit.Checks;
using Xunit;
namespace Emit.Tests;
public sealed class ResolverTests
{
    public static IEnumerable<object[]> AuthoredCases => ResolverChecks.Names.Select(name => new object[] { name });
    [Theory, MemberData(nameof(AuthoredCases))]
    public void AuthoredResolverControls(string name) => ResolverChecks.Run(name);
}
