using Emit.Checks;
using Xunit;
namespace Emit.Tests;
public sealed class PrinterTests
{
    public static IEnumerable<object[]> OriginalCases => PrinterChecks.Names.Select(name => new object[] { name });
    public static IEnumerable<object[]> OriginalUtilityCases => PrinterUtilityChecks.Names.Select(name => new object[] { name });
    [Theory, MemberData(nameof(OriginalCases))]
    public void OriginalPrinter(string name) => PrinterChecks.Run(name);
    [Theory, MemberData(nameof(OriginalUtilityCases))]
    public void OriginalPrinterUtility(string name) => PrinterUtilityChecks.Run(name);
    [Fact]
    public void OriginalTypeEraser() => TypeEraserChecks.TestTypeEraser();
    [Fact]
    public void OriginalImportElision() => ImportElisionChecks.TestImportElision();
    [Fact]
    public void AuthoredIntegerRadices() => Assert.Equal(9, FormatUintControls.RunAll());
    [Fact]
    public void AuthoredCommentWhitespace() => Assert.Equal(1, PrinterCommentControls.RunAll());
    [Fact]
    public void AuthoredUtilityControls() => Assert.Equal(1, PrinterUtilityChecks.RunAuthoredControls());
}
