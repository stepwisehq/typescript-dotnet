// Authored downstream capability boundary. The TsOptions result is unchanged;
// configurations outside current checking support remain fully inspectable.
using TypeScript.Scanner;
using TypeScript.TsOptions;

namespace TypeScript.Compiler;
internal static class ProjectCheckingAdmission
{
    internal static void Validate(ParsedCommandLine parsed, string path)
    {
        void Require(bool condition, string reason)
        {
            if (!condition)
                throw new ProjectException("checkingConfiguration", reason + " Use --inspect-config to inspect this configuration without checking.", path);
        }

        Require((parsed.ProjectReferences()?.Count ?? 0) == 0, "Project-reference execution is not supported.");
        Require((parsed.ContentMappers()?.Count ?? 0) == 0, "Content-mapper execution is not supported.");
        CompilerOptions options = parsed.CompilerOptions();
        Require(options.GetEmitScriptTarget() == ScriptTarget.ESNext && options.GetEmitModuleKind() == ModuleKind.ESNext && options.GetModuleResolutionKind() == ModuleResolutionKind.Bundler, "Checking currently requires effective ESNext target/module and Bundler resolution.");
        Require(options.NoEmit == Tristate.True && options.NoLib == Tristate.True && options.SkipLibCheck == Tristate.True, "Checking requires noEmit, noLib and skipLibCheck; its three genuine libraries are loaded explicitly.");
        Require(options.Types is { Count: 0 } && options.TypeRoots is null, "Checking requires types: [] and no custom typeRoots.");
        Require(!options.GetAllowJS() && !options.GetResolveJsonModule() && options.NoResolve != Tristate.True, "JavaScript/JSON resolution and noResolve are not supported for checking.");
        Require(options.GetAllowImportingTsExtensions() && options.NoUncheckedSideEffectImports == Tristate.True && options.ForceConsistentCasingInFileNames == Tristate.True, "Checking requires TypeScript import extensions, checked side-effect imports and consistent filename casing.");
        Require(options.StrictNullChecks == Tristate.True && options.StrictFunctionTypes == Tristate.True && options.NoImplicitAny == Tristate.True && options.NoImplicitThis == Tristate.True, "Checking requires explicit strictNullChecks, strictFunctionTypes, noImplicitAny and noImplicitThis values.");
        Require(!options.GetStrictOptionValue(options.StrictBindCallApply) && !options.GetStrictOptionValue(options.StrictPropertyInitialization) && !options.GetStrictOptionValue(options.StrictBuiltinIteratorReturn) && !options.GetStrictOptionValue(options.UseUnknownInCatchVariables), "Additional strict-mode checking is outside the current semantic profile.");
        Require(options.NoImplicitReturns != Tristate.True && options.ExactOptionalPropertyTypes != Tristate.True && options.NoUncheckedIndexedAccess != Tristate.True, "Return-completeness, exact optional properties and unchecked indexed access are outside the current semantic profile.");
        // Compare every remaining source-declared option field against its real
        // zero value. This is only a local admission copy; parsed options are
        // never rewritten to the previously fixed checking profile.
        var remaining = options.Clone();
        remaining.Target = ScriptTarget.None;
        remaining.Module = ModuleKind.None;
        remaining.ModuleResolution = ModuleResolutionKind.Unknown;
        remaining.NoEmit = remaining.NoLib = remaining.SkipLibCheck = Tristate.Unknown;
        remaining.Types = null;
        remaining.AllowJs = remaining.ResolveJsonModule = remaining.NoResolve = Tristate.Unknown;
        remaining.AllowImportingTsExtensions = remaining.NoUncheckedSideEffectImports = remaining.ForceConsistentCasingInFileNames = Tristate.Unknown;
        remaining.Strict = remaining.StrictNullChecks = remaining.StrictFunctionTypes = remaining.NoImplicitAny = remaining.NoImplicitThis = Tristate.Unknown;
        remaining.StrictBindCallApply = remaining.StrictPropertyInitialization = remaining.StrictBuiltinIteratorReturn = remaining.UseUnknownInCatchVariables = Tristate.Unknown;
        remaining.NoImplicitReturns = remaining.ExactOptionalPropertyTypes = remaining.NoUncheckedIndexedAccess = Tristate.Unknown;
        // Resolution-only settings already acted on the genuine graph. Removing
        // them from this admission copy neither drops files nor changes options.
        remaining.BaseUrl = default;
        remaining.Paths = null;
        remaining.RootDirs = null;
        remaining.ModuleSuffixes = null;
        remaining.CustomConditions = null;
        remaining.ResolvePackageJsonExports = remaining.ResolvePackageJsonImports = remaining.PreserveSymlinks = Tristate.Unknown;
        remaining.TraceResolution = remaining.AllowArbitraryExtensions = Tristate.Unknown;
        remaining.DeduplicatePackages = Tristate.Unknown;
        remaining.MaxNodeModuleJsDepth = null;
        remaining.ConfigFilePath = default;
        Require(remaining.Equals(new CompilerOptions()), "Other explicitly supplied compiler settings are not supported by the current checker.");
    }
}
