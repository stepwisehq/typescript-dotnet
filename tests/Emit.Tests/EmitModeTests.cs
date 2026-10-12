using Emit.Checks;
using Xunit;

namespace Emit.Tests;
public sealed class EmitModeTests
{
    public static IEnumerable<object[]> Cases => EmitModeChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void GenuinePublicEmitMode(string name) => EmitModeChecks.Run(name);
}
