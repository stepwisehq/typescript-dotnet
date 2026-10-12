using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class StackSanitizerTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => StackSanitizerChecks.Run(name);
    public static IEnumerable<object[]> Cases => StackSanitizerChecks.Names.Select(name => new object[] { name });
}
