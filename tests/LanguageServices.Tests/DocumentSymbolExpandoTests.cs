using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class DocumentSymbolExpandoTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompleteExpandoResponses(bool hierarchical) => DocumentSymbolExpandoChecks.RunCase(hierarchical);
}
