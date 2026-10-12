// Authored shared host adapter for genuine Program/resolver transform-output checks.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Transformers;
using TypeScript.Transformers.TsTransforms;
namespace Emit.Checks;
internal sealed class TsTransformFixture : IDisposable
{
    private readonly string directory;
    private readonly Action done;
    private readonly TypeScript.Compiler.Program program;
    private readonly TypeScript.Checking.Checker checker;
    internal SourceFile File { get; }
    internal SourceFile Other { get; }
    internal EmitContext Context { get; }
    internal TransformOptions Options { get; }
    internal TsTransformFixture(string source, string other, bool jsx, CompilerOptions compilerOptions)
    {
        directory = Path.Combine(Path.GetTempPath(), "emit-ts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string fileName = Path.Combine(directory, jsx ? "main.tsx" : "main.ts").Replace('\\', '/');
            System.IO.File.WriteAllText(fileName, source);
            string otherName = null;
            if (other.Length != 0)
            {
                string packageDirectory = Path.Combine(directory, "node_modules", "other");
                Directory.CreateDirectory(packageDirectory);
                otherName = Path.Combine(packageDirectory, "index.ts").Replace('\\', '/');
                System.IO.File.WriteAllText(otherName, other);
            }
            var fs = TypeScript.Bundled.Bundled.WrapFS(TypeScript.Vfs.OsVfs.OsVfs.FS());
            var config = TypeScript.TsOptions.TsOptions.NewParsedCommandLine(compilerOptions,
                [RootedFilePath.FromUnicode(fileName)], null,
                RootedDirectoryPath.FromUnicode(directory), fs.CaseSensitivity());
            program = TypeScript.Compiler.Program.NewProgram(new()
            {
                Config = config,
                Host = TypeScript.Compiler.CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath()),
                SingleThreaded = Tristate.True
            });
            File = program.GetSourceFile(RootedFilePath.FromUnicode(fileName));
            if (otherName != null) Other = program.GetSourceFile(RootedFilePath.FromUnicode(otherName));
            // Nonexclusive acquisition: genuine EmitResolver methods perform checker locking.
            var lease = program.GetTypeCheckerForFile(default, File);
            checker = lease.Item1;
            done = lease.Item2;
            Context = new EmitContext();
            var resolver = lease.Item1.NewEmitResolver(Context);
            Options = new TransformOptions
            {
                CompilerOptions = compilerOptions, Context = Context,
                EmitResolver = resolver, Resolver = resolver,
                GetEmitModuleFormatOfFile = GetEmitModuleFormatOfFile
            };
        }
        catch
        {
            done?.Invoke();
            Directory.Delete(directory, true);
            throw;
        }
    }
    private ModuleKind GetEmitModuleFormatOfFile(HasFileName file) => program.GetEmitModuleFormatOfFile((SourceFile)file);
    // compiler/emitter.go:139: complete TS portion, before separate JSX/ES/module families.
    internal SourceFile TransformSourceFileExactTsOrder()
    {
        SourceFile result = File;
        if (Options.CompilerOptions.EmitDecoratorMetadata == Tristate.True)
            result = MetadataTransformer.NewMetadataTransformer(Options).TransformSourceFile(result);
        result = TypeEraserTransformer.NewTypeEraserTransformer(Options).TransformSourceFile(result);
        if (Options.CompilerOptions.VerbatimModuleSyntax != Tristate.True && !Ast.IsInJSFile(File.AsNode()))
            result = ImportElisionTransformer.NewImportElisionTransformer(Options).TransformSourceFile(result);
        result = RuntimeSyntaxTransformer.NewRuntimeSyntaxTransformer(Options).TransformSourceFile(result);
        if (Options.CompilerOptions.ExperimentalDecorators == Tristate.True)
            result = LegacyDecoratorsTransformer.NewLegacyDecoratorsTransformer(Options).TransformSourceFile(result);
        return result;
    }
    // compiler/emitter.go:139-174: TS, then genuine ES selector, then UseStrict.
    // JSX and module transforms are separate later families; these fixtures use TS.
    internal SourceFile TransformSourceFileExactTsEsOrder()
    {
        SourceFile result = TransformSourceFileExactTsOrder();
        var downleveler = TypeScript.Transformers.EsTransforms.EsTransforms.GetESTransformer(Options);
        if (downleveler != null) result = downleveler.TransformSourceFile(result);
        return TypeScript.Transformers.EsTransforms.UseStrictTransformer.NewUseStrictTransformer(Options).TransformSourceFile(result);
    }
    internal string PrintEsOutput(SourceFile file) => Printer.NewPrinter(
        new PrinterOptions { NewLine = NewLineKind.LF, Target = Options.CompilerOptions.Target }, new PrintHandlers(), Context).EmitSourceFile(file);
    internal string PrintOutput(SourceFile file) => Printer.NewPrinter(
        new PrinterOptions { NewLine = NewLineKind.LF }, new PrintHandlers(), Context).EmitSourceFile(file);
    internal Slice<Diagnostic> Diagnostics() => checker.GetDiagnostics(File);
    internal TsDiagnosticChannels PublicDiagnostics()
    {
        // Check all applicable files first, then observe globals discovered by semantics.
        var semantic = program.GetSemanticDiagnostics();
        return new(
            TsDiagnosticProjection.Project(program.GetConfigFileParsingDiagnostics(), directory),
            TsDiagnosticProjection.Project(program.GetProgramDiagnostics(), directory),
            TsDiagnosticProjection.Project(program.GetSyntacticDiagnostics(), directory),
            TsDiagnosticProjection.Project(program.GetGlobalDiagnostics(), directory),
            TsDiagnosticProjection.Project(semantic, directory));
    }
    internal void CheckNoDiagnostics()
    {
        var diagnostics = checker.GetDiagnostics(File);
        if (diagnostics.Count != 0)
            throw new InvalidOperationException($"Genuine TS transform fixture has {diagnostics.Count} diagnostics");
    }
    public void Dispose()
    {
        try { done(); }
        finally { Directory.Delete(directory, true); }
    }
}
