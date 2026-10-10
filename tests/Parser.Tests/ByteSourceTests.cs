using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ByteSourceTests
{
    public static IEnumerable<object[]> Cases() => ByteSourceChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesByteSourceContracts(string name) => ByteSourceChecks.Run(name);
}
