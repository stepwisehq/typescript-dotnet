using Xunit;
using LanguageServices.Checks;
namespace LanguageServices.Tests;
public sealed class ProjectUntitledTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OriginalUntitled(int index) => ProjectUntitledChecks.RunOriginal(index);
}
