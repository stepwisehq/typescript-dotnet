using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.Tests;

public sealed class FoundationScannerTests
{
    public static IEnumerable<object[]> OriginalCases() => ScannerChecks.Cases
        .Where(name => name is not "TestScanStringPreservesLoneSurrogates" and not "TestScanSourceKeyword")
        .Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(OriginalCases))]
    public void PreservesOriginalUpstreamAssertion(string name) => ScannerChecks.Run(name);
}
