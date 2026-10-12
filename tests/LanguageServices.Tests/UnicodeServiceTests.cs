using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class UnicodeServiceTests
{
    public static IEnumerable<object[]> Names() => UnicodeServiceChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Names))]
    public void OriginalSelectedAssertions(string name) => UnicodeServiceChecks.Run(name);
}
