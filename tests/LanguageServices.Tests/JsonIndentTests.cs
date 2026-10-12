using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class JsonIndentTests
{
    [Fact]
    public void AuthoredMarshalIndentBoundaries() => Assert.Equal(10, JsonIndentChecks.RunAll());
}
