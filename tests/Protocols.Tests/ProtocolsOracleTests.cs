using Protocols.Checks;
using Xunit;

namespace Protocols.Tests;

public sealed class ProtocolsOracleTests
{
    public static IEnumerable<object[]> Cases => ProtocolsCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSemanticProgramOutput(string name) => ProtocolsCorpus.Run(name);
}
