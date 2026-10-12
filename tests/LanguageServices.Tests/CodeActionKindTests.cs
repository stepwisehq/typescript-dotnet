using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class CodeActionKindTests
{
    [Fact]
    public void AuthoredSourceActionConstants() => Assert.Equal(1, CodeActionKindChecks.RunAll());
}
