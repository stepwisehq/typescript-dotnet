using TypeScript.Binding;
using Xunit;

namespace TypeScript.Scanner.BinderTests;
public class BindingSupportTests
{
    public static IEnumerable<object[]> Cases() => BindingSupportChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void BindingCompletionSupport(string name) => BindingSupportChecks.Run(name);
}
