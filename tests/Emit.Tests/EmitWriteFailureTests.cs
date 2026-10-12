using Emit.Checks;
using Xunit;

namespace Emit.Tests;
public sealed class EmitWriteFailureTests
{
    [Fact]
    public void ActualOsWriteFailure() => EmitWriteFailureChecks.WriteFailure();
    [Fact]
    public void ExistingOutputDirectory() => EmitWriteFailureChecks.ExistingOutputDirectory();
    [Fact]
    public void RawOutputByteBoundary() => EmitWriteFailureChecks.RawOutputBytes();
}
