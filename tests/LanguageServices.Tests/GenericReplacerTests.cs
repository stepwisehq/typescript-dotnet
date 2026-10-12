using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class GenericReplacerTests
{
    [Fact]
    public void OriginalGenericReplaceChannel() => Assert.Equal(1, GenericReplacerChecks.RunAll());
}
