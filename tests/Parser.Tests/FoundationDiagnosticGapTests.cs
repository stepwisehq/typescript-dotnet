using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;
public sealed class FoundationDiagnosticGapTests
{
    public static IEnumerable<object[]> Cases() => DiagnosticGapChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesOriginalDiagnosticWitness(string name) => DiagnosticGapChecks.Run(name);
}
