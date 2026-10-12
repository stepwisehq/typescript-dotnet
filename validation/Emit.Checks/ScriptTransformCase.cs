// Authored full script transform observations; actual Program emission is a later gate.
#nullable disable
using System.Text.Json;
using TypeScript.Scanner;

namespace Emit.Checks;
internal sealed record ScriptTransformCase(string Name, string Input, string Other, bool JSX, ScriptTarget Target, ModuleKind Module, Tristate UseDefineForClassFields, Tristate ExperimentalDecorators, Tristate EmitDecoratorMetadata, bool StrictNullChecks, string Output, string FullDiagnostics, string FileName, string FilesJson, string ExtraOptionsJson)
{
    internal void Run()
    {
        var options = new CompilerOptions
        {
            Target = Target,
            Module = Module,
            SkipLibCheck = Tristate.True,
            UseDefineForClassFields = UseDefineForClassFields,
            ExperimentalDecorators = ExperimentalDecorators,
            EmitDecoratorMetadata = EmitDecoratorMetadata,
            StrictNullChecks = StrictNullChecks ? Tristate.True : Tristate.False
        };
        using var extras = JsonDocument.Parse(ExtraOptionsJson);
        var values = extras.RootElement;
        Tristate Tri(string name) => values.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetBoolean() ? Tristate.True : Tristate.False : Tristate.Unknown;
        string Text(string name) => values.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
        if (values.TryGetProperty("Jsx", out var jsx) && !string.IsNullOrEmpty(jsx.GetString()))
            options.Jsx = Enum.Parse<JsxEmit>(jsx.GetString());
        if (values.TryGetProperty("ModuleResolution", out var mode) && !string.IsNullOrEmpty(mode.GetString()))
            options.ModuleResolution = Enum.Parse<ModuleResolutionKind>(mode.GetString());
        options.JsxFactory = GoString.ToUtf8(Text("JsxFactory"));
        options.JsxFragmentFactory = GoString.ToUtf8(Text("JsxFragmentFactory"));
        options.JsxImportSource = GoString.ToUtf8(Text("JsxImportSource"));
        options.AllowJs = Tri("AllowJs");
        options.CheckJs = Tri("CheckJs");
        options.ResolveJsonModule = Tri("ResolveJsonModule");
        options.RewriteRelativeImportExtensions = Tri("RewriteRelativeImportExtensions");
        options.ImportHelpers = Tri("ImportHelpers");
        options.NoEmitHelpers = Tri("NoEmitHelpers");
        options.ESModuleInterop = Tri("EsModuleInterop");
        options.VerbatimModuleSyntax = Tri("VerbatimModuleSyntax");
        options.IsolatedModules = Tri("IsolatedModules");
        options.AlwaysStrict = Tri("AlwaysStrict");
        options.PreserveConstEnums = Tri("PreserveConstEnums");
        options.RemoveComments = Tri("RemoveComments");
        using var filesDocument = JsonDocument.Parse(FilesJson);
        var files = new Dictionary<string, string>();
        if (filesDocument.RootElement.ValueKind != JsonValueKind.Null)
            foreach (var item in filesDocument.RootElement.EnumerateObject())
                files.Add(item.Name, item.Value.GetString());
        using var fixture = new TsTransformFixture(Input, Other, JSX, options, FileName, files);
        TsDiagnosticProjection.Check(Name, fixture.PublicDiagnostics(), FullDiagnostics);
        string actual = fixture.PrintEsOutput(fixture.TransformSourceFileExactScriptOrder());
        if (actual != GoString.ToUtf8(Output))
            throw new InvalidOperationException($"{Name}: full script output differs from pinned Go\nExpected: {Output}\nActual: {ScannerString.ToJsString(actual)}");
    }
}
