using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchLinuxTests
{
    public static IEnumerable<object[]> Cases() => FsWatchBackendChecks.LinuxNames.Select(static name => new object[] { name });
    [Theory, MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchBackendChecks.RunLinux(name);
}
