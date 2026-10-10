using Protocols.Checks;
using Xunit;

namespace Protocols.Tests;

public sealed class MapperSemanticTests
{
    public static IEnumerable<object[]> Cases() => ProtocolMapperChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSemanticChannel(string name) => ProtocolMapperChecks.Run(name);
}
