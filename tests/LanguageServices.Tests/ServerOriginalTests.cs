using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class ServerOriginalTests
{
    [Theory]
    [MemberData(nameof(ServerOriginalChecks.Cases), MemberType = typeof(ServerOriginalChecks))]
    public void Original(string name) => ServerOriginalChecks.RunOriginal(name);
    [Fact(Skip = "Original TestReplay requires an explicitly supplied replay file")]
    public void Replay() => ServerReplayChecks.TestReplay();
}
