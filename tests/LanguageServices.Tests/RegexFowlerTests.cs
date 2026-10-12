using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class RegexFowlerTests
{
    [Fact]
    public void OriginalCompileAdmissionAndMatchStringChannels() => Assert.Equal(1, RegexFowlerChecks.RunAll());
}
