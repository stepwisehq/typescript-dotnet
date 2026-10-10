using System.Text;
using Program.Checks;
using TypeScript.Scanner;
using Xunit;

namespace Checker.NoEmit.Tests;

// Authored controls for preparation defects found during actual original replay.
public sealed class CompilerHarnessTests
{
    [Fact]
    public void DiskBomDecodingPrecedesDirectiveExpansion()
    {
        const string text = "// @strict: true\n// @target: es5,es2015\nconst value = 1;";
        byte[] utf8 = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
        byte[] little = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray();
        byte[] big = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(text)).ToArray();
        foreach (byte[] bytes in new[] { utf8, little, big })
        {
            string decoded = CompilerNoEmitCases.DecodeSource(bytes);
            Assert.Equal(text, decoded);
            var variants = CompilerFixtureConfigurations.Expand(CompilerFixtureParser.ExtractCompilerSettings(decoded));
            Assert.Equal(new[] { "target=es2015", "target=es5" }, variants.Select(variant => variant.Name));
            Assert.All(variants, variant => Assert.Equal("true", variant.Config!["strict"]));
        }
    }

    [Fact]
    public void StoredCaseAndExplicitCanonicalCollisionFollowOriginalHost()
    {
        var fs = new CompilerBaselineFileSystem(new[] { new CompilerBaselineFileSystem.Input("/src/Foo.ts", "text") }, CaseSensitivity.CaseInsensitive);
        Assert.Equal("/src/Foo.ts", fs.Realpath(RootedPath.FromGoString("/src/foo.ts")).AsString());
        Assert.Throws<InvalidOperationException>(() => new CompilerBaselineFileSystem(new[]
        {
            new CompilerBaselineFileSystem.Input("/src/Foo.ts"), new CompilerBaselineFileSystem.Input("/src/foo.ts")
        }, CaseSensitivity.CaseInsensitive));
    }

    [Fact]
    public void RemovedModuleValueIsOriginalPreparationFailure()
    {
        var error = Assert.Throws<InvalidOperationException>(() => CompilerFixtureConfigurations.Expand(
            new Dictionary<string, string> { ["module"] = "none" }));
        Assert.Equal("Unknown value 'none' for option 'module'", error.Message);
    }
}
