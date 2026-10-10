using Declarations.Checks;
using Xunit;

namespace TypeSystem.Tests;

public sealed class MapperSemanticTests
{
    public static IEnumerable<object[]> Cases() => MapperSemanticChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSemanticChannel(string name) => MapperSemanticChecks.Run(name);
}
