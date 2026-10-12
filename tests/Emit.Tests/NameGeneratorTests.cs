using Emit.Checks;
using Xunit;
namespace Emit.Tests;
public sealed class NameGeneratorTests
{
    public static IEnumerable<object[]> OriginalCases => NameGeneratorChecks.Names.Select(name => new object[] { name });
    [Theory, MemberData(nameof(OriginalCases))]
    public void OriginalNameGeneration(string name) => NameGeneratorChecks.Run(name);
}
