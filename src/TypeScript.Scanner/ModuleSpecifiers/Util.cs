// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/modulespecifiers/util.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
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
using TsOptionFunctions = TypeScript.TsOptions.TsOptions;
using OutputPathFunctions = TypeScript.OutputPaths.OutputPaths;

namespace TypeScript.ModuleSpecifiers;
internal static partial class ModuleSpecifiers
{
    internal sealed class NodeModulePathParts
    {
        internal RootedDirectoryPath TopLevelNodeModulesSearchRoot, TopLevelNodeModulesDirectory, PackageRootDirectory;
        internal string PackageName = "";
        internal RelativePath PackageRelativePath;
        internal bool HasNestedNodeModules, IsDirectNodeModulesFile;
    }

    private enum nodeModulesPathParseState : byte
    {
        BeforeNodeModules,
        NodeModules,
        Scope,
        PackageContent
    }

    // Empty exclusions retain source false; nonempty regex evaluation is an external auto-import service consumer.
    private static bool IsExcludedByRegex(string name, List<string> excludes)
    {
        if (GoRuntime.Len(excludes) == 0)
        {
            return false;
        }

        throw new NotSupportedException("Auto-import exclusion regex evaluation is outside no-emit checker naming.");
    }

    // Upstream tsc/internal/modulespecifiers/util.go:30:1.
    private static int comparePathsByRedirect(ModulePath a, ModulePath b, CaseSensitivity caseSensitivity)
    {
        {
            var c = Core.CompareBooleans(b.IsRedirect, a.IsRedirect);
            if ((c != 0))
            {
                return c;
            }
        }

        {
            var c2 = a.FileName.DirectorySeparatorCount().CompareTo(b.FileName.DirectorySeparatorCount());
            if ((c2 != 0))
            {
                return c2;
            }
        }

        return caseSensitivity.CompareFilePaths(a.FileName, b.FileName);
    }

    // Upstream tsc/internal/modulespecifiers/util.go:43:1.
    public static bool PathIsBareSpecifier(ModuleSpecifier path)
    {
        return (!path.IsAbsolute() && !path.IsRelative());
    }

    // Upstream tsc/internal/modulespecifiers/util.go:125:1.
    public static string GetJSExtensionForDeclarationFileExtension(string ext)
    {
        switch (ext)
        {
            case TsPath.ExtensionDts:
            {
                return TsPath.ExtensionJs;
            }

            case TsPath.ExtensionDmts:
            {
                return TsPath.ExtensionMjs;
            }

            case TsPath.ExtensionDcts:
            {
                return TsPath.ExtensionCjs;
            }

            default:
            {
                return GoRuntime.Slice(ext, GoRuntime.Len(".d"), (GoRuntime.Len(ext) - GoRuntime.Len(TsPath.ExtensionTs)));
            }
        }
    }

    // TryGetRealFileNameForNonJSDeclarationFileName remaps files like `foo.d.json.ts` or
    // `foo.ModuleFunctions.d.css.ts` back to their real non-JS names.
    // Upstream tsc/internal/modulespecifiers/util.go:141:1.
    public static string TryGetRealFileNameForNonJSDeclarationFileName(string fileName)
    {
        var baseName = TsPath.GetBaseFileName(fileName);
        if (((!GoStrings.HasSuffix(fileName, TsPath.ExtensionTs) || !GoStrings.Contains(baseName, ".d.")) || GoStrings.HasSuffix(baseName, TsPath.ExtensionDts)))
        {
            return "";
        }

        var noExtension = TsPath.RemoveExtension(fileName, TsPath.ExtensionTs);
        var lastDotIndex = GoStrings.LastIndex(noExtension, ".");
        var ext = GoRuntime.Slice(noExtension, lastDotIndex);
        var tuple1 = GoStrings.Cut(noExtension, ".d.");
        var before = tuple1.Item1;
        return (before + ext);
    }

    // Upstream tsc/internal/modulespecifiers/util.go:156:1.
    private static string getJSExtensionForFile(string fileName, CompilerOptions options)
    {
        var result = ModuleFunctions.TryGetJSExtensionForFile(fileName, options);
        if ((GoRuntime.Len(result) == 0))
        {
            throw new InvalidOperationException(($"Extension {extensionFromPath(fileName)} is unsupported:: FileName:: {fileName}"));
        }

        return result;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:164:1.
    private static string getJSExtensionForFileName(RootedFilePath fileName, CompilerOptions options)
    {
        var result = ModuleFunctions.TryGetJSExtensionForFileName(fileName, options);
        if ((GoRuntime.Len(result) == 0))
        {
            throw new InvalidOperationException(($"Extension {fileName.Extension()} is unsupported:: FileName:: {fileName.AsString()}"));
        }

        return result;
    }

    // *
    // * Gets the extension from a path.
    // * Path must have a valid extension.
    //
    // Upstream tsc/internal/modulespecifiers/util.go:176:1.
    private static string extensionFromPath(string path)
    {
        var ext = TsPath.TryGetExtensionFromPath(path);
        if ((GoRuntime.Len(ext) == 0))
        {
            throw new InvalidOperationException(($"File {path} has unknown extension."));
        }

        return ext;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:184:1.
    private static bool tryGetAnyFileFromPath(IModuleSpecifierGenerationHost host, RootedFilePath path)
    {
        var extGroups = TsOptionFunctions.GetSupportedExtensions(new CompilerOptions { AllowJs = Tristate.True }, new List<string> { ".node", ".json" });
        {
            foreach (var exts in GoRuntime.RangeValues(extGroups))
            {
                {
                    foreach (var range2 in GoRuntime.Range(exts))
                    {
                        var _2 = range2.Key;
                        var e = range2.Value;
                        if (host.FileExists(path.AppendSuffix(e)))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:203:1.
    private static List<RelativePath> getPathsRelativeToRootDirs(RootedFilePath path, List<RootedDirectoryPath> rootDirs, CaseSensitivity caseSensitivity)
    {
        List<RelativePath> results = default;
        {
            foreach (var rootDir in GoRuntime.RangeValues(rootDirs))
            {
                var relativePath = getRelativePathIfInSameVolume(path, rootDir, caseSensitivity);
                if (!isPathRelativeToParent(relativePath))
                {
                    results = GoRuntime.Append(results, relativePath);
                }
            }
        }

        return results;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:214:1.
    private static bool isPathRelativeToParent(RelativePath path)
    {
        return path.IsParentRelative();
    }

    // Upstream tsc/internal/modulespecifiers/util.go:218:1.
    private static RelativePath getRelativePathIfInSameVolume(RootedFilePath path, RootedDirectoryPath directoryPath, CaseSensitivity caseSensitivity)
    {
        var tuple1 = caseSensitivity.RelativePathFromDirectory(directoryPath, path);
        var relativePath = tuple1.Item1;
        var ok = tuple1.Item2;
        if (!ok)
        {
            return default;
        }

        return relativePath;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:226:1.
    private static string resolvePathPatternIfInSameVolume(string path, RootedDirectoryPath directoryPath, CaseSensitivity caseSensitivity)
    {
        var relativePath = TsPath.ResolveRelativePathToDirectoryOrUrl(directoryPath.AsString(), path, false, directoryPath, caseSensitivity);
        if (TsPath.IsRootedDiskPath(relativePath))
        {
            return "";
        }

        return relativePath;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:241:1.
    private static bool packageJsonPathsAreEqual(RootedDirectoryPath a, RootedDirectoryPath b, CaseSensitivity caseSensitivity)
    {
        if ((a == b))
        {
            return true;
        }

        if (((a == default) || (b == default)))
        {
            return false;
        }

        return (caseSensitivity.ComparePaths(a.AsPath(), b.AsPath()) == 0);
    }

    // Upstream tsc/internal/modulespecifiers/util.go:251:1.
    private static bool prefersTsExtension(List<ModuleSpecifierEnding> allowedEndings)
    {
        var jsPriority = GoSlices.Index(allowedEndings, ModuleSpecifierEnding.JsExtension);
        var tsPriority = GoSlices.Index(allowedEndings, ModuleSpecifierEnding.TsExtension);
        if ((tsPriority > -1))
        {
            return (tsPriority < jsPriority);
        }

        return false;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:260:1.
    private static string replaceFirstStar(string s, string replacement)
    {
        return GoStrings.Replace(s, "*", replacement, 1);
    }

    // Upstream tsc/internal/modulespecifiers/util.go:283:1.
    public static NodeModulePathParts GetNodeModulePathParts(RootedFilePath fileName)
    {
        var fullPath = fileName.AsString();
        var topLevelNodeModulesIndex = -1;
        var packageNameStart = -1;
        var packageRootIndex = -1;
        var hasNestedNodeModules = false;
        var partEnd = 0;
        var state = nodeModulesPathParseState.BeforeNodeModules;
        while ((partEnd >= 0))
        {
            var partStart = partEnd;
            partEnd = Core.IndexAfter(fullPath, "/", (partStart + 1));
            switch (state)
            {
                case nodeModulesPathParseState.BeforeNodeModules:
                {
                    if (GoStrings.HasPrefix(GoRuntime.Slice(fullPath, partStart), "/node_modules/"))
                    {
                        topLevelNodeModulesIndex = partStart;
                        state = nodeModulesPathParseState.NodeModules;
                    }

                    break;
                }

                case nodeModulesPathParseState.NodeModules:
                {
                    packageNameStart = (partStart + 1);
                    if ((packageNameStart >= GoRuntime.Len(fullPath)))
                    {
                        return null;
                    }

                    if ((fullPath[packageNameStart] == '@'))
                    {
                        state = nodeModulesPathParseState.Scope;
                    }
                    else
                    {
                        packageRootIndex = partEnd;
                        state = nodeModulesPathParseState.PackageContent;
                    }

                    break;
                }

                case nodeModulesPathParseState.Scope:
                {
                    packageRootIndex = partEnd;
                    state = nodeModulesPathParseState.PackageContent;
                    break;
                }

                case nodeModulesPathParseState.PackageContent:
                {
                    if (GoStrings.HasPrefix(GoRuntime.Slice(fullPath, partStart), "/node_modules/"))
                    {
                        hasNestedNodeModules = true;
                        state = nodeModulesPathParseState.NodeModules;
                    }

                    break;
                }
            }
        }

        if (((topLevelNodeModulesIndex == -1) || (packageNameStart == -1)))
        {
            return null;
        }

        var rootLength = fileName.RootLength();
        var searchRootEnd = Math.Max(topLevelNodeModulesIndex, rootLength);
        var topLevelNodeModulesSearchRoot = TsPath.RootedDirectoryPathFromNormalized(GoRuntime.Slice(fullPath, 0, searchRootEnd));
        var topLevelNodeModulesDirectory = TsPath.RootedDirectoryPathFromNormalized(GoRuntime.Slice(fullPath, 0, (topLevelNodeModulesIndex + GoRuntime.Len("/node_modules"))));
        if ((packageRootIndex == -1))
        {
            return new NodeModulePathParts
            {
                TopLevelNodeModulesSearchRoot = topLevelNodeModulesSearchRoot,
                TopLevelNodeModulesDirectory = topLevelNodeModulesDirectory,
                PackageName = GoRuntime.Slice(fullPath, packageNameStart),
                HasNestedNodeModules = hasNestedNodeModules,
                IsDirectNodeModulesFile = true
            };
        }

        var packageRootDirectory = TsPath.RootedDirectoryPathFromNormalized(GoRuntime.Slice(fullPath, 0, packageRootIndex));
        return new NodeModulePathParts
        {
            TopLevelNodeModulesSearchRoot = topLevelNodeModulesSearchRoot,
            TopLevelNodeModulesDirectory = topLevelNodeModulesDirectory,
            PackageRootDirectory = packageRootDirectory,
            PackageName = GoRuntime.Slice(fullPath, packageNameStart, packageRootIndex),
            PackageRelativePath = TsPath.RelativePathFromNormalized(GoRuntime.Slice(fullPath, (packageRootIndex + 1))),
            HasNestedNodeModules = hasNestedNodeModules
        };
    }

    // Upstream tsc/internal/modulespecifiers/util.go:353:1.
    public static ModuleSpecifier GetNodeModulesPackageName(CompilerOptions compilerOptions, SourceFile importingSourceFile, RootedFilePath nodeModulesFileName, IModuleSpecifierGenerationHost host, UserPreferences preferences, ModuleSpecifierOptions options)
    {
        var info = getInfo(RootedFilePath.FromGoString(importingSourceFile.FileNameGoBytes()), host);
        var modulePaths = getAllModulePaths(info, nodeModulesFileName, host, compilerOptions, preferences, options);
        {
            foreach (var modulePath in GoRuntime.RangeValues(modulePaths))
            {
                {
                    var result = tryGetModuleNameAsNodeModule(modulePath, info, importingSourceFile, host, compilerOptions, preferences, true, options.OverrideImportMode);
                    if ((result.AsString().Length > 0))
                    {
                        return result;
                    }
                }
            }
        }

        return default;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:371:1.
    private static bool allKeysStartWithDot(OrderedMap<string, ExportsOrImports> obj)
    {
        {
            foreach (var k in obj.Keys())
            {
                if (!GoStrings.HasPrefix(k, "."))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:380:1.
    public static string GetPackageNameFromDirectory(RootedPath path)
    {
        var fileOrDirectoryPath = path.AsString();
        var tuple1 = GoStrings.CutLast(fileOrDirectoryPath, "/node_modules/");
        var basename = tuple1.Item2;
        var found = tuple1.Item3;
        if (!found)
        {
            return "";
        }

        if ((basename[0] == '.'))
        {
            return "";
        }

        var nextSlash = GoStrings.Index(basename, "/");
        if ((nextSlash == -1))
        {
            return basename;
        }

        if (((basename[0] != '@') || (nextSlash == (GoRuntime.Len(basename) - 1))))
        {
            return GoRuntime.Slice(basename, 0, nextSlash);
        }

        var secondSlash = GoStrings.Index(GoRuntime.Slice(basename, (nextSlash + 1)), "/");
        if ((secondSlash == -1))
        {
            return basename;
        }

        return GoRuntime.Slice(basename, 0, ((nextSlash + 1) + secondSlash));
    }

    // ProcessEntrypointEnding processes a pre-computed module specifier from a package.json exports
    // entrypoint according to the entrypoint's Ending type and the user's preferred endings.
    // Upstream tsc/internal/modulespecifiers/util.go:410:1.
    public static ModuleSpecifier ProcessEntrypointEnding(ResolvedEntrypoint entrypoint, UserPreferences prefs, IModuleSpecifierGenerationHost host, CompilerOptions options, SourceFile importingSourceFile, List<ModuleSpecifierEnding> allowedEndings)
    {
        var specifier = entrypoint.ModuleSpecifier.AsString();
        if ((entrypoint.Ending == Ending.Fixed))
        {
            return entrypoint.ModuleSpecifier;
        }

        if ((GoRuntime.Len(allowedEndings) == 0))
        {
            allowedEndings = GetAllowedEndingsInPreferredOrder(prefs, host, options, importingSourceFile, "", host.GetDefaultResolutionModeForFile(importingSourceFile));
        }

        var preferredEnding = allowedEndings[0];
        var dtsExtension = TsPath.GetDeclarationFileExtension(specifier);
        if ((dtsExtension != ""))
        {
            switch (preferredEnding)
            {
                case ModuleSpecifierEnding.TsExtension:
                case ModuleSpecifierEnding.JsExtension:
                {
                    var jsExtension = GetJSExtensionForDeclarationFileExtension(dtsExtension);
                    return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension, new List<string> { dtsExtension }, CaseSensitivity.CaseSensitive));
                }

                case ModuleSpecifierEnding.Minimal:
                case ModuleSpecifierEnding.Index:
                {
                    if ((entrypoint.Ending == Ending.Changeable))
                    {
                        if ((dtsExtension == TsPath.ExtensionDts))
                        {
                            specifier = TsPath.RemoveExtension(specifier, dtsExtension);
                            if ((preferredEnding == ModuleSpecifierEnding.Minimal))
                            {
                                specifier = GoStrings.TrimSuffix(specifier, "/index");
                            }

                            return TsPath.ToModuleSpecifier(specifier);
                        }

                        var jsExtension2 = GetJSExtensionForDeclarationFileExtension(dtsExtension);
                        return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension2, new List<string> { dtsExtension }, CaseSensitivity.CaseSensitive));
                    }

                    var jsExtension3 = GetJSExtensionForDeclarationFileExtension(dtsExtension);
                    return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension3, new List<string> { dtsExtension }, CaseSensitivity.CaseSensitive));
                }
            }

            return TsPath.ToModuleSpecifier(specifier);
        }

        if (TsPath.FileExtensionIsOneOf(specifier, new List<string> { TsPath.ExtensionTs, TsPath.ExtensionTsx, TsPath.ExtensionMts, TsPath.ExtensionCts }))
        {
            switch (preferredEnding)
            {
                case ModuleSpecifierEnding.TsExtension:
                {
                    return TsPath.ToModuleSpecifier(specifier);
                }

                case ModuleSpecifierEnding.JsExtension:
                {
                    {
                        var jsExtension4 = ModuleFunctions.TryGetJSExtensionForFile(specifier, options);
                        if ((jsExtension4 != ""))
                        {
                            return TsPath.ToModuleSpecifier((TsPath.RemoveFileExtension(specifier) + jsExtension4));
                        }
                    }

                    return TsPath.ToModuleSpecifier(specifier);
                }

                case ModuleSpecifierEnding.Minimal:
                case ModuleSpecifierEnding.Index:
                {
                    if ((entrypoint.Ending == Ending.Changeable))
                    {
                        specifier = TsPath.RemoveFileExtension(specifier);
                        if ((preferredEnding == ModuleSpecifierEnding.Minimal))
                        {
                            specifier = GoStrings.TrimSuffix(specifier, "/index");
                        }

                        return TsPath.ToModuleSpecifier(specifier);
                    }

                    {
                        var jsExtension5 = ModuleFunctions.TryGetJSExtensionForFile(specifier, options);
                        if ((jsExtension5 != ""))
                        {
                            return TsPath.ToModuleSpecifier((TsPath.RemoveFileExtension(specifier) + jsExtension5));
                        }
                    }

                    return TsPath.ToModuleSpecifier(specifier);
                }
            }

            return TsPath.ToModuleSpecifier(specifier);
        }

        if (TsPath.FileExtensionIsOneOf(specifier, new List<string> { TsPath.ExtensionJs, TsPath.ExtensionJsx, TsPath.ExtensionMjs, TsPath.ExtensionCjs }))
        {
            switch (preferredEnding)
            {
                case ModuleSpecifierEnding.TsExtension:
                case ModuleSpecifierEnding.JsExtension:
                {
                    return TsPath.ToModuleSpecifier(specifier);
                }

                case ModuleSpecifierEnding.Minimal:
                case ModuleSpecifierEnding.Index:
                {
                    if ((entrypoint.Ending == Ending.Changeable))
                    {
                        specifier = TsPath.RemoveFileExtension(specifier);
                        if ((preferredEnding == ModuleSpecifierEnding.Minimal))
                        {
                            specifier = GoStrings.TrimSuffix(specifier, "/index");
                        }

                        return TsPath.ToModuleSpecifier(specifier);
                    }

                    return TsPath.ToModuleSpecifier(specifier);
                }
            }

            return TsPath.ToModuleSpecifier(specifier);
        }

        return TsPath.ToModuleSpecifier(specifier);
    }
}
