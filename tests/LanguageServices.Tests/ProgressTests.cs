using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProgressTests
{
    [Fact]
    public void Original() => ProgressChecks.RunAll();
}
