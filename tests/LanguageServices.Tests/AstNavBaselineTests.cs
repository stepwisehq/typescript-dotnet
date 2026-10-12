using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AstNavBaselineTests
{
    public static IEnumerable<object[]> Names => AstNavBaselineChecks.Names.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(Names))]
    public void OriginalGoJsonBaseline(string name) => AstNavBaselineChecks.Run(name);
}
