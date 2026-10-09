// Authored bounded host contract for the original checker Program dependency.
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal interface ICheckerProgramHost : TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost
{
    IReadOnlyList<SourceFile> SourceFiles { get; }

    CompilerOptions Options { get; }

    // Authored semantic admission: full graph resolution versus named legacy policy.
    bool AllowsNonRelativeModuleSpecifiers { get; }

    // Authored byte/scalar adapter boundary; not a resolver operation.
    string GetSourceFileNameGoBytes(SourceFile sourceFile);

    ResolvedModule? GetResolvedModule(SourceFile importingFile, string moduleReference, ResolutionMode mode);
    SourceFile? GetSourceFileForResolvedModule(ResolvedModule module);
    ModuleKind GetEmitModuleFormatOfFile(SourceFile sourceFile);
    ResolutionMode GetEmitSyntaxForUsageLocation(SourceFile sourceFile, Node location);
    ResolutionMode GetImpliedNodeFormatForEmit(SourceFile sourceFile);
    bool IsSourceFileDefaultLibrary(SourceFile sourceFile);
}
