// Managed interface for the selected original checker Program/Host dependency;
// actual Program services and named legacy adapter ownership are explicit.
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.TsOptions;

namespace TypeScript.Compiler;
internal interface ICheckerProgramHost : TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost
{
    Slice<SourceFile> SourceFiles { get; }

    CompilerOptions Options { get; }
    void BindSourceFiles();
    Dictionary<ByteStringKey, ModeAwareCache<ResolvedModule?>> GetResolvedModules();
    Dictionary<string, bool> GetPackagesMap();
    SourceFileMetaData GetSourceFileMetaData(PathKey path);
    (string ModuleReference, Node? Specifier) GetJSXRuntimeImportSpecifier(PathKey path);
    (ByteStringValue ModuleReference, Node? Specifier) GetJSXRuntimeImportSpecifierValue(PathKey path)
    {
        var (name, specifier) = GetJSXRuntimeImportSpecifier(path);
        return (ByteStringValue.FromString(name), specifier);
    }
    Node? GetImportHelpersImportSpecifier(PathKey path);
    bool SourceFileMayBeEmitted(SourceFile sourceFile, bool forceDtsEmit);
    SourceOutputAndProjectReference? GetProjectReferenceFromOutputDts(PathKey path);
    ParsedCommandLine? GetRedirectForResolution(SourceFile sourceFile);

    // Authored semantic admission: full graph resolution versus named legacy policy.
    bool AllowsNonRelativeModuleSpecifiers { get; }

    // Authored byte/scalar adapter boundary; not a resolver operation.
    string GetSourceFileNameGoBytes(SourceFile sourceFile);

    ResolvedModule? GetResolvedModule(SourceFile importingFile, string moduleReference, ResolutionMode mode);
    ResolvedModule? GetResolvedModule(SourceFile importingFile, ByteStringValue moduleReference, ResolutionMode mode) =>
        GetResolvedModule(importingFile, moduleReference.Materialize()!, mode);
    SourceFile? GetSourceFileForResolvedModule(ResolvedModule module);
    ModuleKind GetEmitModuleFormatOfFile(SourceFile sourceFile);
    ResolutionMode GetEmitSyntaxForUsageLocation(SourceFile sourceFile, Node location);
    ModuleKind GetImpliedNodeFormatForEmit(SourceFile sourceFile);
    bool IsSourceFileDefaultLibrary(SourceFile sourceFile);
}
