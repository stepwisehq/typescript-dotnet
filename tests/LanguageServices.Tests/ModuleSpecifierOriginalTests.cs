using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ModuleSpecifierOriginalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void Original(int index) => ModuleSpecifierOriginalChecks.RunOriginal(index);
}
