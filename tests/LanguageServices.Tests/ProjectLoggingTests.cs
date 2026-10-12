using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectLoggingTests
{
    public static IEnumerable<object[]> OriginalNames => ProjectLoggingChecks.Names.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(OriginalNames))]
    public void OriginalLogTreeEntries(string name) => ProjectLoggingChecks.Run(name);
    public static IEnumerable<object[]> AuthoredNames => ProjectLoggingChecks.AuthoredNames.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(AuthoredNames))]
    public void AuthoredBehavior(string name) => ProjectLoggingChecks.RunAuthored(name);
}
