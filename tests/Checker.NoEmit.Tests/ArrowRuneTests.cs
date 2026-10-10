using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class ArrowRuneTests
{
    [Theory]
    [InlineData("arrow-u2028")]
    [InlineData("arrow-u2029")]
    [InlineData("arrow-lf")]
    [InlineData("arrow-space")]
    public void CompleteArrowDiagnosticsFollowRuneLineBreaks(string name)
    {
        using var resource = typeof(ArrowRuneTests).Assembly.GetManifestResourceStream("Checker.NoEmit.Tests.ArrowRuneCases")
            ?? throw new InvalidOperationException("Pinned-Go arrow references are missing.");
        RegexByteArgumentChecks.Check(ArrowRuneChecks.Read(resource)[name]);
    }
}
