// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/modulespecifiers/preferences.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Source naming paths for no-emit display; auto-import service filtering remains external.
#nullable disable
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Checking;
using TypeScript.TsOptions;
using ModuleFunctions = TypeScript.Modules.ModuleUtilities;
using OutputPathFunctions = TypeScript.OutputPaths.OutputPaths;

namespace TypeScript.ModuleSpecifiers;
internal static partial class ModuleSpecifiers
{
    private sealed class ModuleSpecifierPreferences
    {
        internal RelativePreferenceKind relativePreference;
        internal Func<ResolutionMode, List<ModuleSpecifierEnding>> getAllowedEndingsInPreferredOrder;
        internal Slice<string> excludeRegexes;
    }

    // Program errors validate that `noEmit` or `emitDeclarationOnly` is also set,
    // so this function doesn't check them to avoid propagating errors.
    // Upstream tsc/internal/modulespecifiers/preferences.go:14:1.
    private static bool shouldAllowImportingTsExtension(CompilerOptions compilerOptions, RootedFilePath fromFileName)
    {
        return (compilerOptions.GetAllowImportingTsExtensions() || fromFileName.IsDeclarationFile());
    }

    // Upstream tsc/internal/modulespecifiers/preferences.go:18:1.
    private static bool usesExtensionsOnImports(SourceFile file)
    {
        {
            foreach (var @ref in GoRuntime.RangeValues(file.Imports()))
            {
                var text = @ref.TextValue();
                if ((TsPath.PathIsRelative(text) && !TsPath.FileExtensionIsOneOf(text, TsPath.ExtensionsNotSupportingExtensionlessResolution)))
                {
                    return (TsPath.HasTSFileExtension(text) || TsPath.HasJSFileExtension(text));
                }
            }
        }

        return false;
    }

    // Upstream tsc/internal/modulespecifiers/preferences.go:28:1.
    private static ModuleSpecifierEnding inferPreference(ResolutionMode resolutionMode, SourceFile sourceFile, bool moduleResolutionIsNodeNext)
    {
        var usesJsExtensions = false;
        Slice<Node> specifiers = default;
        if (((sourceFile != null) && (GoRuntime.Len(sourceFile.Imports()) > 0)))
        {
            specifiers = sourceFile.Imports();
        }
        else
        {
            if (((sourceFile != null) && sourceFile.IsJS()))
            {
            }
        }

        {
            foreach (var specifier in GoRuntime.RangeValues(specifiers))
            {
                var path = specifier.TextValue();
                if (TsPath.PathIsRelative(path))
                {
                    if ((moduleResolutionIsNodeNext && (resolutionMode == ResolutionMode.CommonJS)))
                    {
                        continue;
                    }

                    if (TsPath.FileExtensionIsOneOf(path, TsPath.ExtensionsNotSupportingExtensionlessResolution))
                    {
                        continue;
                    }

                    if (TsPath.HasTSFileExtension(path))
                    {
                        return ModuleSpecifierEnding.TsExtension;
                    }

                    if (TsPath.HasJSFileExtension(path))
                    {
                        usesJsExtensions = true;
                    }
                }
            }
        }

        if (usesJsExtensions)
        {
            return ModuleSpecifierEnding.JsExtension;
        }

        return ModuleSpecifierEnding.Minimal;
    }

    // Upstream tsc/internal/modulespecifiers/preferences.go:69:1.
    private static ModuleSpecifierEnding getModuleSpecifierEndingPreference(ImportModuleSpecifierEndingPreference pref, ResolutionMode resolutionMode, CompilerOptions compilerOptions, SourceFile sourceFile)
    {
        var moduleResolution = compilerOptions.GetModuleResolutionKind();
        var moduleResolutionIsNodeNext = ((ModuleResolutionKind.Node16 <= moduleResolution) && (moduleResolution <= ModuleResolutionKind.NodeNext));
        if (((pref == ImportModuleSpecifierEndingPreference.Js) || ((resolutionMode == ResolutionMode.ESNext) && moduleResolutionIsNodeNext)))
        {
            if (!shouldAllowImportingTsExtension(compilerOptions, default))
            {
                return ModuleSpecifierEnding.JsExtension;
            }

            if ((inferPreference(resolutionMode, sourceFile, moduleResolutionIsNodeNext) != ModuleSpecifierEnding.JsExtension))
            {
                return ModuleSpecifierEnding.TsExtension;
            }

            return ModuleSpecifierEnding.JsExtension;
        }

        if ((pref == ImportModuleSpecifierEndingPreference.Minimal))
        {
            return ModuleSpecifierEnding.Minimal;
        }

        if ((pref == ImportModuleSpecifierEndingPreference.Index))
        {
            return ModuleSpecifierEnding.Index;
        }

        if (!shouldAllowImportingTsExtension(compilerOptions, default))
        {
            if (((sourceFile != null) && usesExtensionsOnImports(sourceFile)))
            {
                return ModuleSpecifierEnding.JsExtension;
            }

            return ModuleSpecifierEnding.Minimal;
        }

        return inferPreference(resolutionMode, sourceFile, moduleResolutionIsNodeNext);
    }

    // Upstream tsc/internal/modulespecifiers/preferences.go:114:1.
    private static ModuleSpecifierEnding getPreferredEnding(UserPreferences prefs, IModuleSpecifierGenerationHost host, CompilerOptions compilerOptions, SourceFile importingSourceFile, string oldImportSpecifier, ResolutionMode resolutionMode)
    {
        if ((GoRuntime.Len(oldImportSpecifier) > 0))
        {
            if (TsPath.HasJSFileExtension(oldImportSpecifier))
            {
                return ModuleSpecifierEnding.JsExtension;
            }

            if (GoStrings.HasSuffix(oldImportSpecifier, "/index"))
            {
                return ModuleSpecifierEnding.Index;
            }
        }

        if ((resolutionMode == ResolutionMode.None))
        {
            resolutionMode = host.GetDefaultResolutionModeForFile(importingSourceFile);
        }

        return getModuleSpecifierEndingPreference(prefs.ImportModuleSpecifierEnding, resolutionMode, compilerOptions, importingSourceFile);
    }

    // Upstream tsc/internal/modulespecifiers/preferences.go:147:1.
    public static List<ModuleSpecifierEnding> GetAllowedEndingsInPreferredOrder(UserPreferences prefs, IModuleSpecifierGenerationHost host, CompilerOptions compilerOptions, SourceFile importingSourceFile, string oldImportSpecifier, ResolutionMode syntaxImpliedNodeFormat)
    {
        var preferredEnding = getPreferredEnding(prefs, host, compilerOptions, importingSourceFile, oldImportSpecifier, ResolutionMode.None);
        var resolutionMode = host.GetDefaultResolutionModeForFile(importingSourceFile);
        if ((resolutionMode != syntaxImpliedNodeFormat))
        {
            preferredEnding = getPreferredEnding(prefs, host, compilerOptions, importingSourceFile, oldImportSpecifier, syntaxImpliedNodeFormat);
        }

        var moduleResolution = compilerOptions.GetModuleResolutionKind();
        var moduleResolutionIsNodeNext = ((ModuleResolutionKind.Node16 <= moduleResolution) && (moduleResolution <= ModuleResolutionKind.NodeNext));
        var allowImportingTsExtension = shouldAllowImportingTsExtension(compilerOptions, RootedFilePath.FromValue(importingSourceFile.FileNameGoValue()));
        var effectiveSyntaxMode = syntaxImpliedNodeFormat;
        if ((effectiveSyntaxMode == ResolutionMode.None))
        {
            effectiveSyntaxMode = resolutionMode;
        }

        if (((effectiveSyntaxMode == ResolutionMode.ESNext) && moduleResolutionIsNodeNext))
        {
            if (allowImportingTsExtension)
            {
                return new List<ModuleSpecifierEnding>
                {
                    ModuleSpecifierEnding.TsExtension,
                    ModuleSpecifierEnding.JsExtension
                };
            }

            return new List<ModuleSpecifierEnding>
            {
                ModuleSpecifierEnding.JsExtension
            };
        }

        switch (preferredEnding)
        {
            case ModuleSpecifierEnding.JsExtension:
            {
                if (allowImportingTsExtension)
                {
                    return new List<ModuleSpecifierEnding>
                    {
                        ModuleSpecifierEnding.JsExtension,
                        ModuleSpecifierEnding.TsExtension,
                        ModuleSpecifierEnding.Minimal,
                        ModuleSpecifierEnding.Index
                    };
                }

                return new List<ModuleSpecifierEnding>
                {
                    ModuleSpecifierEnding.JsExtension,
                    ModuleSpecifierEnding.Minimal,
                    ModuleSpecifierEnding.Index
                };
            }

            case ModuleSpecifierEnding.TsExtension:
            {
                return new List<ModuleSpecifierEnding>
                {
                    ModuleSpecifierEnding.TsExtension,
                    ModuleSpecifierEnding.Minimal,
                    ModuleSpecifierEnding.JsExtension,
                    ModuleSpecifierEnding.Index
                };
            }

            case ModuleSpecifierEnding.Index:
            {
                if (allowImportingTsExtension)
                {
                    return new List<ModuleSpecifierEnding>
                    {
                        ModuleSpecifierEnding.Index,
                        ModuleSpecifierEnding.Minimal,
                        ModuleSpecifierEnding.TsExtension,
                        ModuleSpecifierEnding.JsExtension
                    };
                }

                return new List<ModuleSpecifierEnding>
                {
                    ModuleSpecifierEnding.Index,
                    ModuleSpecifierEnding.Minimal,
                    ModuleSpecifierEnding.JsExtension
                };
            }

            case ModuleSpecifierEnding.Minimal:
            {
                if (allowImportingTsExtension)
                {
                    return new List<ModuleSpecifierEnding>
                    {
                        ModuleSpecifierEnding.Minimal,
                        ModuleSpecifierEnding.Index,
                        ModuleSpecifierEnding.TsExtension,
                        ModuleSpecifierEnding.JsExtension
                    };
                }

                return new List<ModuleSpecifierEnding>
                {
                    ModuleSpecifierEnding.Minimal,
                    ModuleSpecifierEnding.Index,
                    ModuleSpecifierEnding.JsExtension
                };
            }

            default:
            {
                throw new InvalidOperationException("Unexpected module specifier ending: " + preferredEnding);
            }
        }

    }

    // Upstream tsc/internal/modulespecifiers/preferences.go:213:1.
    private static ModuleSpecifierPreferences getModuleSpecifierPreferences(UserPreferences prefs, IModuleSpecifierGenerationHost host, CompilerOptions compilerOptions, SourceFile importingSourceFile, string oldImportSpecifier)
    {
        var excludes = prefs.AutoImportSpecifierExcludeRegexes;
        var relativePreference = RelativePreferenceKind.Shortest;
        if ((GoRuntime.Len(oldImportSpecifier) > 0))
        {
            if (TsPath.IsExternalModuleNameRelative(oldImportSpecifier))
            {
                relativePreference = RelativePreferenceKind.Relative;
            }
            else
            {
                relativePreference = RelativePreferenceKind.NonRelative;
            }
        }
        else
        {
            switch (prefs.ImportModuleSpecifierPreference.Value)
            {
                case "relative":
                {
                    relativePreference = RelativePreferenceKind.Relative;
                    break;
                }

                case "non-relative":
                {
                    relativePreference = RelativePreferenceKind.NonRelative;
                    break;
                }

                case "project-relative":
                {
                    relativePreference = RelativePreferenceKind.ExternalNonRelative;
                    break;
                }
            }
        }

        Func<ResolutionMode, List<ModuleSpecifierEnding>> getAllowedEndingsInPreferredOrder = (syntaxImpliedNodeFormat) =>
        {
            return GetAllowedEndingsInPreferredOrder(prefs, host, compilerOptions, importingSourceFile, oldImportSpecifier, syntaxImpliedNodeFormat);
        };
        return new ModuleSpecifierPreferences
        {
            excludeRegexes = excludes,
            relativePreference = relativePreference,
            getAllowedEndingsInPreferredOrder = getAllowedEndingsInPreferredOrder
        };
    }
}
