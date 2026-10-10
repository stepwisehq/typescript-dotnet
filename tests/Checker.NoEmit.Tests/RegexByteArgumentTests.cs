using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class RegexByteArgumentTests
{
    [Theory]
    [InlineData("regex-unicode-invalid")]
    [InlineData("regex-ascii-invalid")]
    [InlineData("regex-unicode-valid")]
    public void CompleteRegexDiagnosticsPreserveByteArguments(string name)
    {
        using var resource = typeof(RegexByteArgumentTests).Assembly
            .GetManifestResourceStream("Checker.NoEmit.Tests.RegexByteArgumentCases")
            ?? throw new InvalidOperationException("Pinned-Go regex references are missing.");
        RegexByteArgumentChecks.Check(RegexByteArgumentChecks.Read(resource)[name]);
    }
}
