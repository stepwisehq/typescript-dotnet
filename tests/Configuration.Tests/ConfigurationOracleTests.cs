using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class ConfigurationOracleTests
{
    public static IEnumerable<object[]> Cases => ConfigurationCorpus.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalConfigurationOutput(string name) => ConfigurationCorpus.Run(name);
}
