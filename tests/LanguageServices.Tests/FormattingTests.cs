using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FormattingTests
{
    public static IEnumerable<object[]> Cases => FormattingChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalFormatting(string name) => FormattingChecks.Run(name);
}
