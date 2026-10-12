using Emit.Checks;
using Xunit;

namespace Emit.Tests;
public sealed class CheckerMutexTests
{
    public static IEnumerable<object[]> Cases => CheckerMutexChecks.Names.Select(name => new object[] { name });

    [Theory, MemberData(nameof(Cases))]
    public void AuthoredCheckerMutex(string name) => CheckerMutexChecks.Run(name);
}
