// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/modulespecifiers/specifiers.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Genuine fixed relative diagnostic
// branch; packages/project redirects/symlinks/paths/rootDirs/auto-imports excluded.
using System.Text.Json;
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
                if (pair.Key.StartsWith(TsPath.EnsureTrailingDirectorySeparator(directory), StringComparison.Ordinal))
                {
                    local.Add(pair.Value);
                    names.Remove(pair.Key);
                }

            local.Sort(comparePathsByRedirect);
            sorted.AddRange(local);
            string parent = TsPath.GetDirectoryPath(directory);
            if (parent == directory)
                break;
            directory = parent;
        }

        if (names.Count != 0)
        {
            var remaining = names.Values.ToList();
            remaining.Sort(comparePathsByRedirect);
            sorted.AddRange(remaining);
        }

        return sorted;
    }

    internal static List<ModulePath> GetEachFileNameOfModule(string importingFileName, string importedFileName, IModuleSpecifierGenerationHost host, bool preferSymlinks)
    {
        // specifiers.go:262. The fixed host has neither project redirects nor
        // symlink cache; the sole original target survives ignored-path filtering
        // because all targets are then ignored together.
        bool inNodeModules = importedFileName.Contains("/node_modules/", StringComparison.Ordinal);
        if (inNodeModules)
            throw new NotSupportedException("node_modules specifier generation is unsupported.");
        return[new(importedFileName, false, false)];
    }

    private static (List<string>? Specifiers, ResultKind Kind) computeModuleSpecifiers(List<ModulePath> paths, CompilerOptions options, SourceFile importingFile, IModuleSpecifierGenerationHost host, UserPreferences userPreferences, ModuleSpecifierOptions specifierOptions)
    {
        var info = getInfo(importingFile.FileName());
        var preferences = getModuleSpecifierPreferences(userPreferences, host, options, importingFile, "");
        foreach (var path in paths)
        {
            Node? existingImport = null;
            foreach (var reference in importingFile.Imports() ?? [])
            {
                var resolved = host.GetResolvedModuleFromModuleSpecifier(importingFile, reference);
                if (resolved?.IsResolved() == true && resolved.ResolvedPath == path.FileName)
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
                throw new NotSupportedException("Package/redirect specifier generation is unsupported.");
            var importMode = specifierOptions.OverrideImportMode;
            if (importMode == ResolutionMode.None)
                importMode = host.GetDefaultResolutionModeForFile(importingFile);
            string local = getLocalModuleSpecifier(path.FileName, info, options, host, importMode, preferences, false);
            if (local.Length == 0)
                continue;
            if (PathIsBareSpecifier(local))
                throw new NotSupportedException("Non-relative generated specifiers require unsupported package/path mappings.");
            (relativeSpecifiers ??= new()).Add(GoString.ToUtf8(local));
        }

        return (relativeSpecifiers, ResultKind.Relative);
    }

    private static string getLocalModuleSpecifier(string moduleFileName, Info info, CompilerOptions options, IModuleSpecifierGenerationHost host, ResolutionMode importMode, ModuleSpecifierPreferences preferences, bool pathsOnly)
    {
        // specifiers.go:479. The fixed compiler configuration has no paths/rootDirs.
        if (pathsOnly)
            return "";
        var endings = preferences.getAllowedEndingsInPreferredOrder(importMode);
        string path = TsPath.GetRelativePathFromDirectory(info.SourceDirectory, moduleFileName);
        if (TsPath.GetRootLength(path) == 0)
            path = TsPath.EnsurePathIsNonModuleName(path);
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
        throw new NotSupportedException("Generated package imports specifiers are unsupported.");
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
        if (imports.ValueKind != JsonValueKind.Object)
            return "";
        // Original ordered-map JSON decoding updates duplicate names in their
        // first insertion position; retain the final value before lookup.
        var entries = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var pair in imports.EnumerateObject())
            entries[pair.Name] = pair.Value;
        foreach (var pair in entries)
        {
            string key = pair.Key;
            if (key is "#" or "#/" || !key.StartsWith('#'))
                continue;
            if (key.StartsWith("#/", StringComparison.Ordinal) && options.GetModuleResolutionKind()is not (ModuleResolutionKind.NodeNext or ModuleResolutionKind.Bundler))
                continue;
            // The next original branch reverse-matches conditional/package path
            // mappings. Reaching a valid mapping producer is explicitly unsupported;
            // never pretend that a selected mapping was absent.
            if (pair.Value.ValueKind is JsonValueKind.String or JsonValueKind.Array or JsonValueKind.Object)
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
