using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class SlicePredicateTests
{
    [Fact]
    public void OriginalTestIndexFunc() => SlicePredicateChecks.TestIndexFunc();
    [Fact]
    public void OriginalTestContainsFunc() => SlicePredicateChecks.TestContainsFunc();
}
