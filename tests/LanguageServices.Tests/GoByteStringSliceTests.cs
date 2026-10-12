using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class GoByteStringSliceTests
{
    [Fact]
    public void AuthoredBorrowedExtensionMembership() => Assert.Equal(1, GoByteStringSliceChecks.RunAll());
}
