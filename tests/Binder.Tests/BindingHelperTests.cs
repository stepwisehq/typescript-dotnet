using TypeScript.Binding;
using Xunit;

namespace TypeScript.Scanner.BinderTests;
public sealed class BindingHelperTests
{
    public static IEnumerable<object[]> Cases() => BindingHelperChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndependentPinnedGoHelpers(string name) => BindingHelperChecks.Run(name);
}
