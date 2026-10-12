// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/specifiers.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.ModuleSpecifiers;
using ModuleSpecifierFunctions = TypeScript.ModuleSpecifiers.ModuleSpecifiers;

namespace TypeScript.LanguageServices.AutoImport;
internal sealed partial class View
{
    // internal/ls/autoimport/specifiers.go:8
    internal (ModuleSpecifier Specifier, ResultKind Kind) GetModuleSpecifier(Export export, UserPreferences userPreferences)
    {
        if (export.UnresolvedModuleSpecifier.AsValue().Length != 0)
        {
            var specifier = export.UnresolvedModuleSpecifier;
            if (specifier.IsRelative())
            {
                var(relativePath, ok) = program.CaseSensitivity().RelativePathFromDirectory(RootedFilePath.FromValue(importingFile.FileNameGoValue()).Directory(), export.ModuleFileName);
                if (!ok)
                    return (default, ResultKind.None);
                specifier = relativePath.AsModuleSpecifier();
            }

            if (ModuleSpecifierFunctions.IsExcludedByRegex(specifier.AsValue(), userPreferences.AutoImportSpecifierExcludeRegexes))
                return (default, ResultKind.None);
            return (specifier, ResultKind.Relative);
        }

        var(ambientSpecifier, ambient) = export.ModuleID.AsModuleSpecifier();
        if (ambient)
        {
            if (ModuleSpecifierFunctions.IsExcludedByRegex(ambientSpecifier.AsValue(), userPreferences.AutoImportSpecifierExcludeRegexes))
                return (default, ResultKind.None);
            return (ambientSpecifier, ResultKind.Ambient);
        }

        if (export.PackageName.Length != 0 && registry.entrypoints != null && registry.entrypoints.TryGetValue(export.Path, out var entrypoints))
        {
            foreach (var entrypoint in entrypoints)
            {
                if ((entrypoint.IncludeConditions == null || entrypoint.IncludeConditions.IsSubsetOf(conditions)) && (entrypoint.ExcludeConditions == null || !conditions.Overlaps(entrypoint.ExcludeConditions)))
                {
                    var specifier = ModuleSpecifierFunctions.ProcessEntrypointEnding(entrypoint, userPreferences, program, program.Options(), importingFile, GetAllowedEndings());
                    if (!ModuleSpecifierFunctions.IsExcludedByRegex(specifier.AsValue(), userPreferences.AutoImportSpecifierExcludeRegexes))
                        return (specifier, ResultKind.NodeModules);
                }
            }

            return (default, ResultKind.None);
        }

        var cache = registry.specifierCache![importingFilePath]!;
        if (export.PackageName.Length == 0)
        {
            var(specifier, ok) = cache.Load(export.Path);
            if (ok)
                return specifier.AsValue().Length == 0 ? (default, ResultKind.None) : (specifier, ResultKind.Relative);
        }

        var(specifiers, kind) = ModuleSpecifierFunctions.GetModuleSpecifiersForFileWithInfo(importingFile, export.ModuleFileName, program.Options(), program, userPreferences, new ModuleSpecifierOptions(), true);
        // Original TODO: multiple specifiers may arise with rare local symlink paths.
        foreach (var specifier in specifiers)
        {
            if (specifier.AsValue().Contains("/node_modules/"u8))
                continue;
            cache.Store(export.Path, specifier);
            return (specifier, kind);
        }

        cache.Store(export.Path, default);
        return (default, ResultKind.None);
    }
}
