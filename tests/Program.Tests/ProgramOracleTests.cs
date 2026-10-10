using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class ProgramOracleTests
{
    public static IEnumerable<object[]> Cases => ProgramCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalProgramOutput(string name) => ProgramCorpus.Run(name);
}
