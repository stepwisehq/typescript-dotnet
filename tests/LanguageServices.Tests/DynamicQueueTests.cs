using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class DynamicQueueTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => DynamicQueueChecks.Run(name);
    public static IEnumerable<object[]> Cases => DynamicQueueChecks.Names.Select(name => new object[] { name });
}
