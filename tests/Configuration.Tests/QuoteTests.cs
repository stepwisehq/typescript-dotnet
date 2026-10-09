using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class QuoteTests
{
    public static IEnumerable<object[]> Cases => QuoteChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedOriginalAssertions(string name) => QuoteChecks.Run(name);
}
