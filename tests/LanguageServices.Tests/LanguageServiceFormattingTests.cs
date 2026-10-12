using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class LanguageServiceFormattingTests
{
    public static IEnumerable<object[]> Names() => LanguageServiceFormattingChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Names))]
    public void OriginalFormattingServiceAssertions(string name) => LanguageServiceFormattingChecks.Run(name);
}
