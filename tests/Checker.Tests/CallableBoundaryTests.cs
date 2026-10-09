using System.Text.Json;
using TypeScript.Checking.Validation;
using TypeScript.Scanner;
using Xunit;

namespace TypeScript.Checking.Tests;
public sealed class CallableBoundaryTests
{
    public static IEnumerable<object? []> Controls()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "callable-corpus/admission-controls.json")));
        foreach (var value in json.RootElement.EnumerateArray())
            yield return[value.GetProperty("name").GetString(), value.GetRawText(), value.GetProperty("expectedFeature").GetString()];
    }

    [Theory, MemberData(nameof(Controls))]
    public void AuthoredCallableBoundaryIsSeparateFromGoSemanticAgreement(string name, string input, string? feature)
    {
        var request = JsonSerializer.Deserialize(input, CheckerJson.Default.Request)!;
        var actual = Runner.Admission(request);
        Assert.True(actual.Admitted == (feature is null), name + ": " + actual.Feature);
        Assert.Equal(feature, actual.Feature);
        if (feature is not null)
            Assert.InRange(actual.End!.Value, actual.Pos!.Value, System.Text.Encoding.UTF8.GetByteCount(request.Text));
    }

    [Fact]
    public void DefaultLegacyAdmissionStaysSeparateFromCallableCapability()
    {
        var file = Parser.ParseSourceFile(new SourceFileParseOptions { FileName = "/main.ts" }, "function f(x:number){return x;}const y=f(1);", ScriptKind.TS);
        Assert.Equal("statement", Assert.Throws<UnsupportedSemanticFeatureException>(() => Checker.Create(file)).Feature);
        Assert.Null(Checker.Create(file, CheckerProfile.Callable).GetDiagnostics());
    }

    [Fact]
    public void CallableNativeAndManagedSharePublicApiAssertions()
    {
        Assert.Equal(10, CallableSelfTests.Run().Count);
    }

    [Fact]
    public void InvalidProfileHasAnExplicitPublicBoundary()
    {
        var file = Parser.ParseSourceFile(new SourceFileParseOptions { FileName = "/main.ts" }, "", ScriptKind.TS);
        Assert.Throws<ArgumentOutOfRangeException>(() => Checker.Create(file, (CheckerProfile)int.MaxValue));
    }
}
