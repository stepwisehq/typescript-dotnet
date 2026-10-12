using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class LocaleMatcherTests
{
    public static IEnumerable<object[]> Cases => LocaleMatcherChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalWorkerFunction(string name) => LocaleMatcherChecks.Run(name);
}
