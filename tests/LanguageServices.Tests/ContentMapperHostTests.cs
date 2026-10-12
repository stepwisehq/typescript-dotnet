using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ContentMapperHostTests
{
    public static IEnumerable<object[]> Cases => ContentMapperHostChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ContentMapperHostChecks.Run(name);
}
