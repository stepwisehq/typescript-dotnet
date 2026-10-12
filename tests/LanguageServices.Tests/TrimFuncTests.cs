using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class TrimFuncTests
{
    [Fact]
    public void OriginalTestTrimFunc() => TrimFuncChecks.RunAll();
}
