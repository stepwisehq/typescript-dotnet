using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ConverterTests
{
    public static IEnumerable<object[]> Cases => ConverterChecks.Names.Select(name => new object[] { name });

    [Fact]
    public void AuthoredCoordinateBoundaries() => Assert.Equal(4, ConverterChecks.RunCoordinateBoundaryControls());

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalConverter(string name) => ConverterChecks.Run(name);
}
