using TypeScript.Checking.Validation;
using Xunit;

namespace TypeScript.Checking.Tests;
public sealed class CallableNumericTests
{
    public static IEnumerable<object[]> OriginalRows() => CallableNumericChecks.Cases.Select(name => new object[] { name });
    [Theory, MemberData(nameof(OriginalRows))]
    public void OriginalNumericLiteralAndNodeAssertionsAreSharedWithNative(string name) => CallableNumericChecks.Run(name);
}
