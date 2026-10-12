using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ConsumedLogFormatTests
{
    [Fact]
    public void AuthoredConsumedBoundary() => ConsumedLogFormatChecks.RunAll();
}
