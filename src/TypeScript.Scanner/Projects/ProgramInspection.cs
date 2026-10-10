// Authored ordinary disk inspection over the genuine Program constructor.
// This adapter never acquires a checker or replaces the configured environment.
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Vfs.OsVfs;

namespace TypeScript.Compiler;
public sealed class ProgramInspection
{
    public ConfigurationInspection Configuration { get; }
    public Program Program { get; }
    public IReadOnlyList<Diagnostic> ConfigurationDiagnostics { get; }
    public IReadOnlyList<Diagnostic> SyntacticDiagnostics { get; }
    public IReadOnlyList<Diagnostic> BindDiagnostics { get; }
    public IReadOnlyList<Diagnostic> ProgramDiagnostics { get; }
    public IReadOnlyList<Diagnostic> InclusionDiagnostics { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<DiagAndArgs> Traces => traces;
    private readonly List<DiagAndArgs> traces = [];

    public ProgramInspection(string configPath, bool useSourceOfProjectReference = false)
    {
        Configuration = Project.InspectConfiguration(configPath);
        if (Configuration.Parsed is null)
            throw new ConfigurationDiagnosticsException(Configuration.Diagnostics);
        var fs = TypeScript.Bundled.Bundled.WrapFS(OsVfs.FS());
        var host = CompilerHosts.NewCachedFSCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath(), trace: (message, args) =>
        {
            lock (traces) traces.Add(new() { Message = message, Args = args.Select(value => (object?)value).ToList() });
        });
        Program = TypeScript.Compiler.Program.NewProgram(new() { Config = Configuration.Parsed, Host = host, UseSourceOfProjectReference = useSourceOfProjectReference });
        ConfigurationDiagnostics = Configuration.Diagnostics;
        SyntacticDiagnostics = Program.GetSyntacticDiagnostics() ?? [];
        BindDiagnostics = Program.GetBindDiagnostics() ?? [];
        ProgramDiagnostics = Program.GetProgramDiagnostics() ?? [];
        // Preserve the source public inclusion-diagnostic policy without acquiring
        // a checker. Fileless inclusion diagnostics remain in ProgramDiagnostics;
        // all sources and resolution records remain present regardless of policy.
        var inclusion = new List<Diagnostic>();
        foreach (var source in Program.SourceFiles())
            inclusion.AddRange(Program.GetIncludeProcessorDiagnostics(source) ?? []);
        InclusionDiagnostics = TypeScript.Compiler.Program.SortAndDeduplicateDiagnostics(inclusion) ?? [];
        Diagnostics = TypeScript.Compiler.Program.SortAndDeduplicateDiagnostics([
            .. ConfigurationDiagnostics, .. SyntacticDiagnostics, .. BindDiagnostics, .. ProgramDiagnostics, .. InclusionDiagnostics
        ]) ?? [];
    }
}
