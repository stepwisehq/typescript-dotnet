using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class DiagnosticLocaleTests
{
    public static IEnumerable<object[]> Cases => DiagnosticLocaleChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalRuntimeFunction(string name) => DiagnosticLocaleChecks.Run(name);
}
