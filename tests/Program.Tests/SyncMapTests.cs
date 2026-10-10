using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class SyncMapTests
{
    [Fact]
    public void Original() => SyncMapChecks.Run("TestSyncMapWithNil");
}
