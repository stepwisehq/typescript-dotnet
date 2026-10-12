using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchBackendTests
{
    public static IEnumerable<object[]> Cases() => FsWatchBackendChecks.BroadNames.Select(static name => new object[] { name });
    [Theory, MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchBackendChecks.RunBroad(name);
}

[CollectionDefinition("FsWatch runtime observer", DisableParallelization = true)]
public sealed class FsWatchRuntimeCollection;
[Collection("FsWatch runtime observer")]
public sealed class FsWatchRuntimeTests
{
    [Fact]
    public void OriginalNoGoroutineLeak() => FsWatchBackendChecks.TestSubscribeNoGoroutineLeak();
}
