using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AstNavOriginalTests
{
    public static IEnumerable<object[]> Names => AstNavOriginalChecks.Names.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(Names))]
    public void WholeOriginal(string name) => AstNavOriginalChecks.RunOriginal(name);
}
