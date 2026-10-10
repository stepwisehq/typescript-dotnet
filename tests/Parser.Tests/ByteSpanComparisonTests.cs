using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ByteSpanComparisonTests
{
    public static IEnumerable<object[]> Cases() => ByteSpanComparisonChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreserveExactByteComparisonSemantics(string name) => ByteSpanComparisonChecks.Run(name);
}
