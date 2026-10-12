// Authored genuine Program observations for complete TypeScript→ES target chains.
#nullable disable
using TypeScript.Scanner;

namespace Emit.Checks;
internal sealed record EsTransformCase(string Name, string Input, string Other, bool JSX, ScriptTarget Target, ModuleKind Module, Tristate UseDefineForClassFields, Tristate ExperimentalDecorators, Tristate EmitDecoratorMetadata, bool StrictNullChecks, string Output, string FullDiagnostics)
{
    internal void Run()
    {
        var options = new CompilerOptions
        {
            Target = Target,
            Module = Module,
            UseDefineForClassFields = UseDefineForClassFields,
            ExperimentalDecorators = ExperimentalDecorators,
            EmitDecoratorMetadata = EmitDecoratorMetadata,
            StrictNullChecks = StrictNullChecks ? Tristate.True : Tristate.False,
            SkipLibCheck = Tristate.True
        };
        using var fixture = new TsTransformFixture(Input, Other, JSX, options);
        // Invalid programs retain their complete public diagnostics and transform output.
        TsDiagnosticProjection.Check(Name, fixture.PublicDiagnostics(), FullDiagnostics);
        string actual = fixture.PrintEsOutput(fixture.TransformSourceFileExactTsEsOrder());
        if (actual != GoString.ToUtf8(Output))
            throw new InvalidOperationException($"{Name}: complete TS→ES output differs from pinned Go\nExpected: {Output}\nActual: {ScannerString.ToJsString(actual)}");
    }
}
