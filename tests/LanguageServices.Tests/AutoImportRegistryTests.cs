using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class AutoImportRegistryTests
{
    public static IEnumerable<object[]> Cases => AutoImportRegistryChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => AutoImportRegistryChecks.Run(name);
}
