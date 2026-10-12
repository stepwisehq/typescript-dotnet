using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class SessionDiagnosticsEmitTests
{
    public static IEnumerable<object[]> Cases => SessionDiagnosticsEmitChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void AuthoredSessionDiagnosticsEmitContext(string name) => SessionDiagnosticsEmitChecks.Run(name);
}
