using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class IpcTests
{
    public static IEnumerable<object[]> AsyncCases => IpcAsyncChecks.Names.Select(static name => new object[] { name });
    public static IEnumerable<object[]> TimingCases => IpcTimingChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(AsyncCases))]
    public void OriginalAsync(string name) => IpcAsyncChecks.Run(name);
    [Theory]
    [MemberData(nameof(TimingCases))]
    public void OriginalTiming(string name) => IpcTimingChecks.Run(name);
}
