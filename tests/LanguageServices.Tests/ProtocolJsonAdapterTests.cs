using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProtocolJsonAdapterTests
{
    public static IEnumerable<object[]> Cases => ProtocolJsonAdapterChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void AuthoredJsonAdaptation(string name) => ProtocolJsonAdapterChecks.Run(name);
}
