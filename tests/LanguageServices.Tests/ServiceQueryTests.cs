using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ServiceQueryTests
{
    public static IEnumerable<object[]> Names => ServiceQueryChecks.Names.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(Names))]
    public void QueryOrderAndSerialization(string name) => ServiceQueryChecks.Run(name);
}
