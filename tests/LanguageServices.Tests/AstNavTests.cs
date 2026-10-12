using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AstNavTests
{
    [Fact]
    public void OriginalTokenSubtests() => AstNavChecks.RunOriginalTokenSubtests();
    [Fact]
    public void OriginalUnitFindPrecedingToken() => AstNavChecks.RunAll();
}
