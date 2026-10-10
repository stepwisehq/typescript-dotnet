// Authored public callback fixtures. Supplied diagnostic values are explicit
// factory inputs; lookup/results still come from the genuine default resolver.
using System.Text.Json.Nodes;
using TypeScript.Compiler;
using TypeScript.ContentMapper;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;

namespace Program.Checks;

public static partial class ProgramRunner
{
    private sealed class CaptureSourceHost(ICompilerHost inner) : ICompilerHost
    {
        internal readonly Dictionary<RootedFilePath, SourceFile> Files = [];
        public IFS FS() => inner.FS();
        public RootedDirectoryPath DefaultLibraryPath() => inner.DefaultLibraryPath();
        public void Trace(Message message, params object[] args) => inner.Trace(message, args);
        public SourceFile? GetSourceFile(SourceFileParseOptions options)
        {
            var file = inner.GetSourceFile(options);
            if (file is not null) Files[RootedFilePath.FromGoString(file.FileNameGoBytes())] = file;
            return file;
        }
        public (SourceFiles Files, Exception? Error) GetContentMappedSourceFiles(SourceFileParseOptions options, Mapper mapper) => inner.GetContentMappedSourceFiles(options, mapper);
        public IProject? ContentMapperProject() => inner.ContentMapperProject();
        public ParsedCommandLine? GetResolvedProjectReference(RootedFilePath file, PathKey path) => inner.GetResolvedProjectReference(file, path);
    }

    private sealed class DecoratedResolver(IResolver inner, CaptureSourceHost host, JsonObject input) : IResolver
    {
        public RootedDirectoryPath BaseDirectory() => inner.BaseDirectory();
        public ResolutionData GetResolutionData() => inner.GetResolutionData();
        private Diagnostic SuppliedDiagnostic(RootedFilePath path, string kind)
        {
            var file = host.Files.GetValueOrDefault(path);
            return file is null
                ? Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, "authored-factory-" + kind)
                : Ast.NewDiagnostic(file, new(0, 1), Diagnostics.File_0_not_found, "authored-factory-" + kind);
        }
        public (ResolvedModule? Result, List<DiagAndArgs>? Traces, Exception? Error) ResolveModuleName(string name, RootedFilePath file, ResolutionMode mode, IResolvedProjectReference? reference = null)
        {
            var result = inner.ResolveModuleName(name, file, mode, reference);
            if (input["Error"]?.GetValue<bool>() == true)
                return (result.Result, result.Traces, new InvalidOperationException("authored resolver factory failure"));
            if (result.Result is not null && input["ModuleDiagnostics"]?.GetValue<bool>() == true)
                result.Result.ResolutionDiagnostics = [SuppliedDiagnostic(file, "module")];
            return result;
        }
        public (ResolvedModule? Result, List<DiagAndArgs>? Traces, Exception? Error) ResolveModuleNameFromDirectory(string name, RootedDirectoryPath directory, ResolutionMode mode) => inner.ResolveModuleNameFromDirectory(name, directory, mode);
        public (ResolvedTypeReferenceDirective? Result, List<DiagAndArgs>? Traces) ResolveTypeReferenceDirective(string name, RootedFilePath file, ResolutionMode mode, IResolvedProjectReference? reference = null)
        {
            var result = inner.ResolveTypeReferenceDirective(name, file, mode, reference);
            if (result.Result is not null && input["TypeDiagnostics"]?.GetValue<bool>() == true)
                result.Result.ResolutionDiagnostics = [SuppliedDiagnostic(file, "type")];
            return result;
        }
    }
}
