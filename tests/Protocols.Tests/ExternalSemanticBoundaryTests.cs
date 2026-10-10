using Declarations.Checks;
using Xunit;

namespace Protocols.Tests;

public sealed class ExternalSemanticBoundaryTests
{
    [Fact]
    public void ActualExternalConsumers() => ExternalSemanticBoundaryChecks.Run(ExternalSemanticBoundaryChecks.Cases[0]);
}
