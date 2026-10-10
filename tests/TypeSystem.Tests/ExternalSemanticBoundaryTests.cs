using Declarations.Checks;
using Xunit;

namespace TypeSystem.Tests;

public sealed class ExternalSemanticBoundaryTests
{
    [Fact]
    public void ActualExternalConsumers() => ExternalSemanticBoundaryChecks.Run(ExternalSemanticBoundaryChecks.Cases[0]);
}
