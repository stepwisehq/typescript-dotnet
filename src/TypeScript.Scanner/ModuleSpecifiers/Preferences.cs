// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/modulespecifiers/preferences.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Fixed Bundler project diagnostics.
using TypeScript.Scanner;

namespace TypeScript.ModuleSpecifiers;
internal static partial class ModuleSpecifiers
{
    private static bool shouldAllowImportingTsExtension(CompilerOptions options, string fileName) => options.GetAllowImportingTsExtensions() || TsPath.IsDeclarationFileName(fileName);
    private static bool usesExtensionsOnImports(SourceFile file)
    {
        foreach (var reference in file.Imports() ?? [])
        {
            string path = reference.Text();
            if (TsPath.PathIsRelative(path) && !hasMandatoryExtension(path))
                return hasTsExtension(path) || hasJsExtension(path);
        }

        return false;
    }

    private static ModuleSpecifierEnding inferPreference(ResolutionMode mode, SourceFile? file, bool isNodeNext)
    {
        bool usesJsExtensions = false;
        foreach (var reference in file?.Imports() ?? [])
        {
            string path = reference.Text();
            if (!TsPath.PathIsRelative(path))
                continue;
            if (isNodeNext && mode == ResolutionMode.CommonJS)
                continue;
            if (hasMandatoryExtension(path))
                continue;
            if (hasTsExtension(path))
                return ModuleSpecifierEnding.TsExtension;
            if (hasJsExtension(path))
                usesJsExtensions = true;
        }

        return usesJsExtensions ? ModuleSpecifierEnding.JsExtension : ModuleSpecifierEnding.Minimal;
    }

    private static ModuleSpecifierEnding getModuleSpecifierEndingPreference(ImportModuleSpecifierEndingPreference preference, ResolutionMode mode, CompilerOptions options, SourceFile file)
    {
        var resolution = options.GetModuleResolutionKind();
        bool isNodeNext = resolution >= ModuleResolutionKind.Node16 && resolution <= ModuleResolutionKind.NodeNext;
        if (preference == ImportModuleSpecifierEndingPreference.Js || mode == ResolutionMode.ESNext && isNodeNext)
        {
            if (!shouldAllowImportingTsExtension(options, ""))
                return ModuleSpecifierEnding.JsExtension;
            return inferPreference(mode, file, isNodeNext) != ModuleSpecifierEnding.JsExtension ? ModuleSpecifierEnding.TsExtension : ModuleSpecifierEnding.JsExtension;
        }

        if (preference == ImportModuleSpecifierEndingPreference.Minimal)
            return ModuleSpecifierEnding.Minimal;
        if (preference == ImportModuleSpecifierEndingPreference.Index)
            return ModuleSpecifierEnding.Index;
        if (!shouldAllowImportingTsExtension(options, ""))
            return usesExtensionsOnImports(file) ? ModuleSpecifierEnding.JsExtension : ModuleSpecifierEnding.Minimal;
        return inferPreference(mode, file, isNodeNext);
    }

    private static ModuleSpecifierEnding getPreferredEnding(UserPreferences preferences, IModuleSpecifierGenerationHost host, CompilerOptions options, SourceFile file, string oldImportSpecifier, ResolutionMode mode)
    {
        if (oldImportSpecifier.Length > 0)
        {
            if (hasJsExtension(oldImportSpecifier))
                return ModuleSpecifierEnding.JsExtension;
            if (oldImportSpecifier.EndsWith("/index", StringComparison.Ordinal))
                return ModuleSpecifierEnding.Index;
        }

        if (mode == ResolutionMode.None)
            mode = host.GetDefaultResolutionModeForFile(file);
        return getModuleSpecifierEndingPreference(preferences.ImportModuleSpecifierEnding, mode, options, file);
    }

    private sealed record ModuleSpecifierPreferences(RelativePreferenceKind relativePreference, Func<ResolutionMode, List<ModuleSpecifierEnding>> getAllowedEndingsInPreferredOrder);
    internal static List<ModuleSpecifierEnding> GetAllowedEndingsInPreferredOrder(UserPreferences preferences, IModuleSpecifierGenerationHost host, CompilerOptions options, SourceFile file, string oldSpecifier, ResolutionMode syntaxMode)
    {
        var preferred = getPreferredEnding(preferences, host, options, file, oldSpecifier, ResolutionMode.None);
        var mode = host.GetDefaultResolutionModeForFile(file);
        if (mode != syntaxMode)
            preferred = getPreferredEnding(preferences, host, options, file, oldSpecifier, syntaxMode);
        bool allowTs = shouldAllowImportingTsExtension(options, file.FileName());
        var effective = syntaxMode != ResolutionMode.None ? syntaxMode : mode;
        var resolution = options.GetModuleResolutionKind();
        bool isNodeNext = resolution >= ModuleResolutionKind.Node16 && resolution <= ModuleResolutionKind.NodeNext;
        if (effective == ResolutionMode.ESNext && isNodeNext)
            return allowTs ? [ModuleSpecifierEnding.TsExtension, ModuleSpecifierEnding.JsExtension] : [ModuleSpecifierEnding.JsExtension];
        return preferred switch
        {
            ModuleSpecifierEnding.JsExtension => allowTs ? [ModuleSpecifierEnding.JsExtension, ModuleSpecifierEnding.TsExtension, ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.Index] : [ModuleSpecifierEnding.JsExtension, ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.Index],
            ModuleSpecifierEnding.TsExtension => [ModuleSpecifierEnding.TsExtension, ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.JsExtension, ModuleSpecifierEnding.Index],
            ModuleSpecifierEnding.Index => allowTs ? [ModuleSpecifierEnding.Index, ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.TsExtension, ModuleSpecifierEnding.JsExtension] : [ModuleSpecifierEnding.Index, ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.JsExtension],
            ModuleSpecifierEnding.Minimal => allowTs ? [ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.Index, ModuleSpecifierEnding.TsExtension, ModuleSpecifierEnding.JsExtension] : [ModuleSpecifierEnding.Minimal, ModuleSpecifierEnding.Index, ModuleSpecifierEnding.JsExtension],
            _ => throw new InvalidOperationException("Unknown module specifier ending.")};
    }

    private static ModuleSpecifierPreferences getModuleSpecifierPreferences(UserPreferences preferences, IModuleSpecifierGenerationHost host, CompilerOptions options, SourceFile file, string oldSpecifier)
    {
        var relative = RelativePreferenceKind.Shortest;
        if (oldSpecifier.Length > 0)
            relative = TsPath.PathIsRelative(oldSpecifier) ? RelativePreferenceKind.Relative : RelativePreferenceKind.NonRelative;
        else
            relative = preferences.ImportModuleSpecifierPreference switch
            {
                ImportModuleSpecifierPreference.Relative => RelativePreferenceKind.Relative,
                ImportModuleSpecifierPreference.NonRelative => RelativePreferenceKind.NonRelative,
                ImportModuleSpecifierPreference.ProjectRelative => RelativePreferenceKind.ExternalNonRelative,
                _ => RelativePreferenceKind.Shortest
            };
        return new(relative, mode => GetAllowedEndingsInPreferredOrder(preferences, host, options, file, oldSpecifier, mode));
    }
}
