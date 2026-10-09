// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/modulespecifiers/specifiers.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Genuine fixed relative diagnostic
// branch and original existing-import reuse/actual package-dedup target paths;
// broad generated package/path names, project references and symlink cache excluded.
using TypeScript.PackageJson;
using TypeScript.Modules;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace TypeScript.ModuleSpecifiers;
internal static partial class ModuleSpecifiers
{
    internal static ModuleSpecifiersResult GetModuleSpecifiers(Symbol moduleSymbol, Checker checker, CompilerOptions options, SourceFile importingFile, IModuleSpecifierGenerationHost host, UserPreferences preferences, ModuleSpecifierOptions specifierOptions, bool forAutoImports) => GetModuleSpecifiersWithInfo(moduleSymbol, checker, options, importingFile, host, preferences, specifierOptions, forAutoImports);
    private static ModuleSpecifiersResult GetModuleSpecifiersWithInfo(Symbol moduleSymbol, Checker checker, CompilerOptions options, SourceFile importingFile, IModuleSpecifierGenerationHost host, UserPreferences preferences, ModuleSpecifierOptions specifierOptions, bool forAutoImports)
    {
        if (forAutoImports || preferences.AutoImportSpecifierExcludeRegexes is { Count: > 0 })
            throw new NotSupportedException("Auto-import module specifier generation is outside the project checking contract.");
        tryGetModuleNameFromAmbientModule(moduleSymbol);
        var moduleFile = Ast.GetSourceFileOfModule(moduleSymbol);
        if (moduleFile == null)
            return new();
        string moduleName = host.GetSourceOfProjectReferenceIfOutputIncluded(moduleFile);
        var(specifiers, kind) = GetModuleSpecifiersForFileWithInfo(importingFile, moduleName, options, host, preferences, specifierOptions);
        return new()
        {
            Specifiers = specifiers,
            Kind = kind
        };
    }

    private static void tryGetModuleNameFromAmbientModule(Symbol moduleSymbol)
    {
        foreach (var declaration in moduleSymbol.Declarations ?? [])
            if (Ast.IsModuleWithStringLiteralName(declaration) || Ast.IsModuleDeclaration(declaration) && Ast.FindAncestor(declaration, Ast.IsModuleWithStringLiteralName) != null)
                throw new NotSupportedException("Ambient module specifier generation is outside the project checking contract.");
    // Both actual ambient-module producer branches are excluded. Ordinary
    // external source-file declarations have no ambient name result.
    }

    private sealed record Info(string ImportingSourceFileName, string SourceDirectory);
    private static Info getInfo(string name) => new(name, TsPath.GetDirectoryPath(name));
    private static RootedPath getPath(string fileName, IModuleSpecifierGenerationHost host) =>
        host.FileNamesAreGoBytes ? RootedPath.FromGoString(fileName) : RootedPath.FromUnicode(fileName);
    private static (List<string>? Specifiers, ResultKind Kind) GetModuleSpecifiersForFileWithInfo(SourceFile importingFile, string moduleName, CompilerOptions options, IModuleSpecifierGenerationHost host, UserPreferences preferences, ModuleSpecifierOptions specifierOptions)
    {
        var paths = getAllModulePathsWorker(getInfo(host.GetSourceOfProjectReferenceIfOutputIncluded(importingFile)), moduleName, host);
        return computeModuleSpecifiers(paths, options, importingFile, host, preferences, specifierOptions);
    }

    private static List<ModulePath> getAllModulePathsWorker(Info info, string importedFileName, IModuleSpecifierGenerationHost host)
    {
        var names = new Dictionary<string, ModulePath>(StringComparer.Ordinal);
        foreach (var path in GetEachFileNameOfModule(info.ImportingSourceFileName, importedFileName, host, true))
            names[path.FileName] = path;
        List<ModulePath> sorted = [];
        for (string directory = info.SourceDirectory; names.Count != 0;)
        {
            List<ModulePath> local = [];
            foreach (var pair in names.ToList())
                if (host.CaseSensitivity().StartsWithDirectory(
                    TsPath.RootedFilePathFromPath(getPath(pair.Key, host)),
                    TsPath.RootedDirectoryPathFromPath(getPath(directory, host))))
                {
                    local.Add(pair.Value);
                    names.Remove(pair.Key);
                }

            local.Sort((first, second) => comparePathsByRedirect(first, second, host));
            sorted.AddRange(local);
            string parent = TsPath.GetDirectoryPath(directory);
            if (parent == directory)
                break;
            directory = parent;
        }

        if (names.Count != 0)
        {
            var remaining = names.Values.ToList();
            remaining.Sort((first, second) => comparePathsByRedirect(first, second, host));
            sorted.AddRange(remaining);
        }

        return sorted;
    }

    internal static List<ModulePath> GetEachFileNameOfModule(string importingFileName, string importedFileName, IModuleSpecifierGenerationHost host, bool preferSymlinks)
    {
        // specifiers.go:262–354. Graph construction does not build reference
        // projects or a Program symlink cache. Actual package-dedup redirects
        // remain required targets even with those external stages absent.
        var importedPath = host.CaseSensitivity().PathKey(getPath(importedFileName, host));
        List<string> targets = [importedFileName];
        targets.AddRange(host.GetRedirectTargets(importedPath));
        bool shouldFilterIgnoredPaths = !targets.All(containsIgnoredPath);
        List<ModulePath> results = [];
        foreach (string target in targets)
        {
            if (shouldFilterIgnoredPaths && containsIgnoredPath(target))
                continue;
            results.Add(new(target, target.Contains("/node_modules/", StringComparison.Ordinal), false));
        }
        // Without the external symlink cache the original preferSymlinks branches
        // produce this same target order; no alternative spelling is fabricated.
        return results;
    }

    private static bool containsIgnoredPath(string fileName) =>
        fileName.Contains("/node_modules/.", StringComparison.Ordinal) ||
        fileName.Contains("/.git", StringComparison.Ordinal) || fileName.Contains(".#", StringComparison.Ordinal);

    private static (List<string>? Specifiers, ResultKind Kind) computeModuleSpecifiers(List<ModulePath> paths, CompilerOptions options, SourceFile importingFile, IModuleSpecifierGenerationHost host, UserPreferences userPreferences, ModuleSpecifierOptions specifierOptions)
    {
        var info = getInfo(host.FileNamesAreGoBytes ? importingFile.FileNameGoBytes() : importingFile.FileName());
        var preferences = getModuleSpecifierPreferences(userPreferences, host, options, importingFile, "");
        foreach (var path in paths)
        {
            var targetPath = host.CaseSensitivity().PathKey(getPath(path.FileName, host));
            Node? existingImport = null;
            foreach (var reference in importingFile.Imports() ?? [])
            {
                var resolved = host.GetResolvedModuleFromModuleSpecifier(importingFile, reference);
                if (resolved.IsResolved() && resolved!.ResolvedPath == targetPath)
                {
                    existingImport = reference;
                    break;
                }
            }

            if (existingImport == null)
                continue;
            if (preferences.relativePreference == RelativePreferenceKind.NonRelative && TsPath.PathIsRelative(existingImport.Text()))
                continue;
            var existingMode = host.GetModeForUsageLocation(importingFile, existingImport);
            var targetMode = specifierOptions.OverrideImportMode;
            if (targetMode == ResolutionMode.None)
                targetMode = host.GetDefaultResolutionModeForFile(importingFile);
            if (existingMode != targetMode && existingMode != ResolutionMode.None && targetMode != ResolutionMode.None)
                continue;
            return ([existingImport.Text()], ResultKind.None); // Actual original byte spelling.
        }

        List<string>? relativeSpecifiers = null;
        foreach (var path in paths)
        {
            if (path.IsInNodeModules || path.IsRedirect)
                throw new ProjectException("semanticUnsupported", "Diagnostic formatting requires package/redirect specifier generation outside the current checker. Use --inspect-project to inspect the resolved graph.");
            var importMode = specifierOptions.OverrideImportMode;
            if (importMode == ResolutionMode.None)
                importMode = host.GetDefaultResolutionModeForFile(importingFile);
            string local = getLocalModuleSpecifier(path.FileName, info, options, host, importMode, preferences, false);
            if (local.Length == 0)
                continue;
            if (PathIsBareSpecifier(local))
                throw new ProjectException("semanticUnsupported", "Diagnostic formatting requires non-relative generated specifiers outside the current checker. Use --inspect-project to inspect the resolved graph.");
            (relativeSpecifiers ??= new()).Add(host.FileNamesAreGoBytes ? local : GoString.ToUtf8(local));
        }

        return (relativeSpecifiers, ResultKind.Relative);
    }

    private static string getLocalModuleSpecifier(string moduleFileName, Info info, CompilerOptions options, IModuleSpecifierGenerationHost host, ResolutionMode importMode, ModuleSpecifierPreferences preferences, bool pathsOnly)
    {
        // specifiers.go:479. Existing imports were reused before reaching this
        // selected relative generator. Never silently ignore a newly admitted
        // resolver setting whose reverse-formatting producer is still external.
        if (options.GetEffectiveRootDirs() is { Count: > 0 })
            throw new ProjectException("semanticUnsupported", "Diagnostic formatting requires rootDirs reverse mapping outside the current checker. Use --inspect-project to inspect the resolved graph.");
        if (options.Paths is not null && preferences.relativePreference != RelativePreferenceKind.Relative)
            throw new ProjectException("semanticUnsupported", "Diagnostic formatting requires paths reverse mapping outside the current checker. Use --inspect-project to inspect the resolved graph.");
        if (pathsOnly)
            return "";
        var endings = preferences.getAllowedEndingsInPreferredOrder(importMode);
        var (relativePath, inSameRoot) = host.CaseSensitivity().RelativePathFromDirectory(
            TsPath.RootedDirectoryPathFromPath(getPath(info.SourceDirectory, host)),
            TsPath.RootedFilePathFromPath(getPath(moduleFileName, host)));
        string path = moduleFileName;
        if (inSameRoot)
        {
            string bytes = relativePath.AsModuleSpecifier().AsString();
            path = host.FileNamesAreGoBytes ? bytes : GoString.ToString(bytes);
        }
        string relative = processEnding(path, moduleFileName, endings, options, host);
        if (!options.GetResolvePackageJsonImports() || preferences.relativePreference == RelativePreferenceKind.Relative)
            return relative;
        // No configured paths base exists. Genuine package-import reverse mapping
        // can still be consulted before returning the relative path.
        string relativeToBase = TsPath.GetRelativePathFromDirectory(host.BaseDirectory(), moduleFileName);
        if (TsPath.GetRootLength(relativeToBase) != 0 || relativeToBase.Length == 0)
            return relative;
        string fromImports = tryGetModuleNameFromPackageJsonImports(moduleFileName, info.SourceDirectory, options, host, importMode, endings.Contains(ModuleSpecifierEnding.TsExtension));
        if (fromImports.Length == 0)
            return relative;
        throw new ProjectException("semanticUnsupported", "Diagnostic formatting requires generated package imports specifiers outside the current checker. Use --inspect-project to inspect the resolved graph.");
    }

    private static string tryGetModuleNameFromPackageJsonImports(string moduleFileName, string directory, CompilerOptions options, IModuleSpecifierGenerationHost host, ResolutionMode mode, bool preferTs)
    {
        if (!options.GetResolvePackageJsonImports())
            return "";
        string ancestor = host.GetNearestAncestorDirectoryWithPackageJson(directory);
        if (ancestor.Length == 0)
            return "";
        var info = host.GetPackageJsonInfo(TsPath.CombinePaths(ancestor, "package.json"));
        if (info?.GetContents()is not { } contents)
            return "";
        var imports = contents.Imports;
        if (imports.Type != JSONValueType.Object)
            return "";
        foreach (var pair in imports.AsObject().Entries())
        {
            string key = pair.Key;
            if (key is "#" or "#/" || !key.StartsWith('#'))
                continue;
            if (key.StartsWith("#/", StringComparison.Ordinal) && options.GetModuleResolutionKind()is not (ModuleResolutionKind.NodeNext or ModuleResolutionKind.Bundler))
                continue;
            // The next original branch reverse-matches conditional/package path
            // mappings. Reaching a valid mapping producer is explicitly unsupported;
            // never pretend that a selected mapping was absent.
            if (pair.Value.Type is JSONValueType.String or JSONValueType.Array or JSONValueType.Object)
                throw new ProjectException("packageJsonSpecifierMapping", "Diagnostic module names require unsupported ancestor imports mapping: " + TsPath.CombinePaths(ancestor, "package.json"));
        }

        return "";
    }

    internal static string processEnding(string specifier, string sourceFileName, List<ModuleSpecifierEnding> endings, CompilerOptions options, IModuleSpecifierGenerationHost host)
    {
        if (specifier.EndsWith(".json", StringComparison.Ordinal) || specifier.EndsWith(".mjs", StringComparison.Ordinal) || specifier.EndsWith(".cjs", StringComparison.Ordinal))
            return specifier;
        string noExtension = TsPath.RemoveFileExtension(specifier);
        if (noExtension == specifier)
            return specifier;
        if (!specifier.EndsWith(".ts", StringComparison.Ordinal) || TsPath.IsDeclarationFileName(specifier))
            throw new NotSupportedException("Module specifier ending for an unsupported source-file form.");
        switch (endings[0])
        {
            case ModuleSpecifierEnding.Minimal:
                string withoutIndex = noExtension.EndsWith("/index", StringComparison.Ordinal) ? noExtension[..^6] : noExtension;
                if (withoutIndex != noExtension && tryGetAnyFileFromPath(host, TsPath.GetDirectoryPath(sourceFileName)))
                    return noExtension;
                return withoutIndex;
            case ModuleSpecifierEnding.Index:
                return noExtension;
            case ModuleSpecifierEnding.JsExtension:
                return noExtension + getJSExtensionForFile(specifier, options);
            case ModuleSpecifierEnding.TsExtension:
                return specifier;
            default:
                throw new InvalidOperationException("Unknown module specifier ending.");
        }
    }
}
