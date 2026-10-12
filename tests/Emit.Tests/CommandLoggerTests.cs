using Emit.Checks;
using Xunit;

namespace Emit.Tests;
public sealed class CommandLoggerTests
{
    [Fact]
    public void AuthoredRawUtf8Boundary() => Assert.Equal(1, CommandLoggerChecks.RunAuthoredControls());
    [Fact]
    public void OriginalContentMapperLoggerEnvironmentVariable() => Assert.Equal(1, CommandLoggerChecks.RunAll());
}
