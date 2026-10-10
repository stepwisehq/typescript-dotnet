using Xunit;
using Program.Checks;
namespace Program.Tests;
public sealed class ParseResultTests
{
    [Theory]
    [MemberData(nameof(ParseResultChecks.Cases), MemberType = typeof(ParseResultChecks))]
    public void OriginalAssertions(string name) => ParseResultChecks.Run(name);
}
