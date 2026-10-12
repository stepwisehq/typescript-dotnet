using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AutoImportTests
{
    public static IEnumerable<object[]> OriginalNames() => AutoImportOwnerChecks.Names.Concat(AutoImportRealpathChecks.Names).Concat(AutoImportAliasResolverChecks.Names).Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(OriginalNames))]
    public void OriginalProducerAssertions(string name)
    {
        if (AutoImportOwnerChecks.Names.Contains(name))
            AutoImportOwnerChecks.Run(name);
        else if (AutoImportRealpathChecks.Names.Contains(name))
            AutoImportRealpathChecks.Run(name);
        else
            AutoImportAliasResolverChecks.Run(name);
    }

    [Fact]
    public void ReusableRegistryWorkPhases() => Assert.Equal(1, AutoImportWorkGroupChecks.RunAll());
}
