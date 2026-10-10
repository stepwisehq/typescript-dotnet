using Xunit;
using Program.Checks;
namespace Program.Tests;
public sealed class SpanMapTests
{
    [Theory]
    [MemberData(nameof(SpanMapChecks.Cases), MemberType = typeof(SpanMapChecks))]
    public void OriginalAssertions(string name) => SpanMapChecks.Run(name);
}
