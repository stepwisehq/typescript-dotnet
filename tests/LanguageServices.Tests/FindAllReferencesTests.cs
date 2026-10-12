using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FindAllReferencesTests
{
    public static IEnumerable<object[]> Cases => FindAllReferencesChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => FindAllReferencesChecks.Run(name);
}
