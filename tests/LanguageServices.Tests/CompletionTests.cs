using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class CompletionTests
{
    [Fact]
    public void OriginalDirectoryPrefixCaseFolding() => CompletionChecks.TestTryRemoveDirectoryPrefixCaseFoldingShrinksPrefix();
}
