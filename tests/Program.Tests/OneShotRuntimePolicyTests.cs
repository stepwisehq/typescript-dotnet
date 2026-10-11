using System.Text.Json;
using Xunit;

public sealed class OneShotRuntimePolicyTests
{
    [Fact]
    public void OrdinaryConfigKeepsPolicyOff()
    {
        Assert.True(CliArguments.TryParse(["--config", "project.json", "--json", "--noEmit"], out var options));
        Assert.False(options.NoGcRegion);
        Assert.True(options.Json);
        Assert.True(options.NoEmit);
    }

    [Fact]
    public void ConfigExplicitlyEnablesRegion()
    {
        Assert.True(CliArguments.TryParse(["--config", "project.json", "--no-gc-region", "--json"], out var options));
        Assert.True(options.NoGcRegion);
    }

    [Theory]
    [InlineData("--project")]
    [InlineData("--inspect-config")]
    [InlineData("--inspect-project")]
    [InlineData("--inspect-program")]
    public void RegionIsRejectedOutsideOneShotConfig(string mode) =>
        Assert.False(CliArguments.TryParse([mode, "project.json", "--no-gc-region"], out _));

    [Fact]
    public void DuplicateRegionOptionIsRejected() =>
        Assert.False(CliArguments.TryParse(["--config", "project.json", "--no-gc-region", "--no-gc-region"], out _));

    [Fact]
    public void InsufficientPreflightFallsBackAndReportsWithoutRequestingLargeRegion()
    {
        string folder = Path.Combine(Path.GetTempPath(), "tscheck-policy-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            string memory = Path.Combine(folder, "meminfo"), report = Path.Combine(folder, "report.json");
            File.WriteAllText(memory, "MemAvailable: 0 kB\n");
            using (var policy = new NoGcRegionPolicy(memory, report)) { }
            using var document = JsonDocument.Parse(File.ReadAllText(report));
            var value = document.RootElement;
            Assert.False(value.GetProperty("noGcStarted").GetBoolean());
            Assert.True(value.GetProperty("fallbackAtStart").GetBoolean());
            Assert.Equal("preflightFallback", value.GetProperty("noGcStartOutcome").GetString());
            Assert.Equal("notStarted", value.GetProperty("noGcEndOutcome").GetString());
            Assert.True(value.GetProperty("restoredExact").GetBoolean());
            Assert.Equal(14L * 1024 * 1024 * 1024, value.GetProperty("totalBudgetBytes").GetInt64());
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void InvalidOptionalReportPathDoesNotOverrideCompilerOutcome()
    {
        string memory = Path.GetTempFileName();
        try
        {
            File.WriteAllText(memory, "MemAvailable: 0 kB\n");
            using var policy = new NoGcRegionPolicy(memory, "invalid\0report");
        }
        finally { File.Delete(memory); }
    }

}
