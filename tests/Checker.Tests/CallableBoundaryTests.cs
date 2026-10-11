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
        Assert.True(Checker.Create(file, CheckerProfile.Callable).GetDiagnostics().IsNil);
    }

    [Fact]
    public void CallableNativeAndManagedSharePublicApiAssertions()
    {
        Assert.Equal(10, CallableSelfTests.Run().Count);
    }

    // Authored managed admission control; does not change original semantic records.
    [Fact]
    public void SignatureQueriesPreserveOwnForeignDefaultAndNullAdmission()
    {
        static (Checker Checker, Signature Signature) CallSignature()
        {
            var file = Parser.ParseSourceFile(new("/signature-api.ts", "/signature-api.ts"),
                "function f(value:number):number{return value;}const result=f(1);", ScriptKind.TS);
            var checker = Checker.Create(file, CheckerProfile.Callable);
            Assert.True(checker.GetDiagnostics().IsNil);
            var call = file.Statements.Nodes[1].AsVariableStatement().DeclarationList
                .AsVariableDeclarationList().Declarations.Nodes[0].Initializer();
            return (checker, checker.GetResolvedSignature(call));
        }

        var own = CallSignature();
        Assert.True((own.Checker.GetReturnTypeOfSignature(own.Signature).Flags() & TypeFlags.Number) != 0);
        Assert.Null(own.Checker.GetTypePredicateOfSignature(own.Signature));
        var foreign = CallSignature();
        foreach (var signature in new[] { foreign.Signature, new Signature() })
        {
            var returnError = Assert.Throws<ArgumentException>(() => own.Checker.GetReturnTypeOfSignature(signature));
            var predicateError = Assert.Throws<ArgumentException>(() => own.Checker.GetTypePredicateOfSignature(signature));
            Assert.Equal("signature", returnError.ParamName);
            Assert.Equal("signature", predicateError.ParamName);
            Assert.StartsWith("Signature belongs to a different checker", returnError.Message);
            Assert.StartsWith("Signature belongs to a different checker", predicateError.Message);
        }
        Assert.Equal("signature", Assert.Throws<ArgumentNullException>(
            () => own.Checker.GetReturnTypeOfSignature(null!)).ParamName);
        Assert.Equal("signature", Assert.Throws<ArgumentNullException>(
            () => own.Checker.GetTypePredicateOfSignature(null!)).ParamName);
    }

    [Fact]
    public void InvalidProfileHasAnExplicitPublicBoundary()
    {
        var file = Parser.ParseSourceFile(new SourceFileParseOptions { FileName = "/main.ts" }, "", ScriptKind.TS);
        Assert.Throws<ArgumentOutOfRangeException>(() => Checker.Create(file, (CheckerProfile)int.MaxValue));
    }
}
