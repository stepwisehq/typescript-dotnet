using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class ProtocolFramingTests
{
    public static IEnumerable<object[]> Cases => ProtocolFramingChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProtocolFramingChecks.Run(name);
}
