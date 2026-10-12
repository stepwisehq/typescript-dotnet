// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/modulespecifiers/specifiers.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
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
    private struct ambientModuleInfo
    {
        internal ModuleSpecifier name;
        internal Symbol symbol;
    }

    private sealed class Info
    {
        internal CaseSensitivity CaseSensitivity;
        internal RootedFilePath ImportingSourceFileName;
        internal RootedDirectoryPath SourceDirectory;
    }

    private struct pkgJsonDirAttemptResult
    {
        internal RootedFilePath moduleFileToTry;
        internal RootedDirectoryPath packageRootDirectory;
        internal bool blockedByExports;
        internal ModuleSpecifier verbatimFromExports;
    }

    private struct specPair
    {
        internal ModuleSpecifierEnding ending;
        internal ModuleSpecifier value;
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:19:1.
    public static ModuleSpecifiersResult GetModuleSpecifiers(Symbol moduleSymbol, Checker checker, CompilerOptions compilerOptions, SourceFile importingSourceFile, IModuleSpecifierGenerationHost host, UserPreferences userPreferences, ModuleSpecifierOptions options, bool forAutoImports)
    {
        return GetModuleSpecifiersWithInfo(moduleSymbol, checker, compilerOptions, importingSourceFile, host, userPreferences, options, forAutoImports);
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:41:1.
    public static ModuleSpecifiersResult GetModuleSpecifiersWithInfo(Symbol moduleSymbol, Checker checker, CompilerOptions compilerOptions, SourceFile importingSourceFile, IModuleSpecifierGenerationHost host, UserPreferences userPreferences, ModuleSpecifierOptions options, bool forAutoImports)
    {
        var ambient = tryGetModuleNameFromAmbientModule(moduleSymbol, checker);
        if ((ambient.name.AsValue().Length > 0))
        {
            if ((forAutoImports && IsExcludedByRegex(ambient.name.AsValue(), userPreferences.AutoImportSpecifierExcludeRegexes)))
            {
                return new ModuleSpecifiersResult
                {
                    Kind = ResultKind.Ambient,
                    AmbientModuleSymbol = ambient.symbol
                };
            }

            return new ModuleSpecifiersResult
            {
                Specifiers = Slice<ModuleSpecifier>.FromArray([ambient.name]),
                Kind = ResultKind.Ambient,
                AmbientModuleSymbol = ambient.symbol
            };
        }

        var moduleSourceFile = Ast.GetSourceFileOfModule(moduleSymbol);
        if ((moduleSourceFile == null))
        {
            return new ModuleSpecifiersResult
            {
            };
        }

        var moduleFileName = host.GetSourceOfProjectReferenceIfOutputIncluded(moduleSourceFile);
        var tuple1 = GetModuleSpecifiersForFileWithInfo(importingSourceFile, moduleFileName, compilerOptions, host, userPreferences, options, forAutoImports);
        var specifiers = tuple1.Item1;
        var kind = tuple1.Item2;
        return new ModuleSpecifiersResult
        {
            Specifiers = specifiers,
            Kind = kind
        };
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:79:1.
    public static (Slice<ModuleSpecifier>, ResultKind) GetModuleSpecifiersForFileWithInfo(SourceFile importingSourceFile, RootedFilePath moduleFileName, CompilerOptions compilerOptions, IModuleSpecifierGenerationHost host, UserPreferences userPreferences, ModuleSpecifierOptions options, bool forAutoImports)
    {
        var modulePaths = getAllModulePathsWorker(getInfo(host.GetSourceOfProjectReferenceIfOutputIncluded(importingSourceFile), host), moduleFileName, host, compilerOptions, options);
        return computeModuleSpecifiers(modulePaths, compilerOptions, importingSourceFile, host, userPreferences, options, forAutoImports);
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:112:1.
    private static ambientModuleInfo tryGetModuleNameFromAmbientModule(Symbol moduleSymbol, Checker checker)
    {
        {
            foreach (var decl in GoRuntime.RangeValues(moduleSymbol.Declarations))
            {
                if ((Ast.IsModuleWithStringLiteralName(decl) && ((!Ast.IsModuleAugmentationExternal(decl) || !TsPath.IsExternalModuleNameRelative(decl.Name().TextValue())))))
                {
                    return new ambientModuleInfo
                    {
                        name = TsPath.ToModuleSpecifier(decl.Name().TextValue()),
                        symbol = moduleSymbol
                    };
                }
            }
        }

        {
            foreach (var range2 in GoRuntime.Range(moduleSymbol.Declarations))
            {
                var _2 = range2.Key;
                var d = range2.Value;
                if (!Ast.IsModuleDeclaration(d))
                {
                    continue;
                }

                var possibleContainer = Ast.FindAncestor(d, Ast.IsModuleWithStringLiteralName);
                if ((((possibleContainer == null) || (possibleContainer.Parent == null)) || !Ast.IsSourceFile(possibleContainer.Parent)))
                {
                    continue;
                }

                var ok = possibleContainer.Symbol().Exports.TryGetValue(Ast.InternalSymbolNameExportEquals, out var sym);
                if ((!ok || (sym == null)))
                {
                    continue;
                }

                var exportAssignmentDecl = sym.ValueDeclaration;
                if (((exportAssignmentDecl == null) || (exportAssignmentDecl.Kind != Kind.ExportAssignment)))
                {
                    continue;
                }

                var exportSymbol = checker.GetSymbolAtLocation(exportAssignmentDecl.Expression());
                if ((exportSymbol == null))
                {
                    continue;
                }

                if (((exportSymbol.Flags & SymbolFlags.Alias) != 0))
                {
                    exportSymbol = checker.GetAliasedSymbol(exportSymbol);
                }

                if ((exportSymbol == d.Symbol()))
                {
                    return new ambientModuleInfo
                    {
                        name = TsPath.ToModuleSpecifier(possibleContainer.Name().TextValue()),
                        symbol = possibleContainer.Symbol()
                    };
                }
            }
        }

        return new ambientModuleInfo
        {
        };
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:168:1.
    private static Info getInfo(RootedFilePath importingSourceFileName, IModuleSpecifierGenerationHost host)
    {
        return new Info
        {
            ImportingSourceFileName = importingSourceFileName,
            SourceDirectory = importingSourceFileName.Directory(),
            CaseSensitivity = host.CaseSensitivity()
        };
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:179:1.
    private static Slice<ModulePath> getAllModulePaths(Info info, RootedFilePath importedFileName, IModuleSpecifierGenerationHost host, CompilerOptions compilerOptions, UserPreferences preferences, ModuleSpecifierOptions options)
    {
        var modulePaths = getAllModulePathsWorker(info, importedFileName, host, compilerOptions, options);
        return modulePaths;
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:202:1.
    private static Slice<ModulePath> getAllModulePathsWorker(Info info, RootedFilePath importedFileName, IModuleSpecifierGenerationHost host, CompilerOptions compilerOptions, ModuleSpecifierOptions options)
    {
        var allFileNames = new Dictionary<RootedFilePath, ModulePath>();
        var paths = GetEachFileNameOfModule(info.ImportingSourceFileName, importedFileName, host, true);
        {
            foreach (var p in GoRuntime.RangeValues(paths))
            {
                allFileNames[p.FileName] = p;
            }
        }

        var caseSensitivity = info.CaseSensitivity;
        Comparison<ModulePath> comparePaths = (a, b) =>
        {
            return comparePathsByRedirect(a, b, caseSensitivity);
        };
        var sortedPaths = Slice<ModulePath>.Make(0, paths.Length);
        {
            var directory = info.SourceDirectory;
            while ((GoRuntime.Len(allFileNames) != 0))
            {
                Slice<ModulePath> pathsInDirectory = default;
                {
                    foreach (var range2 in GoRuntime.Range(allFileNames))
                    {
                        var fileName = range2.Key;
                        var p2 = range2.Value;
                        if (caseSensitivity.StartsWithDirectory(fileName, directory))
                        {
                            pathsInDirectory = pathsInDirectory.Append(p2);
                            allFileNames.Remove(fileName);
                        }
                    }
                }

                if ((GoRuntime.Len(pathsInDirectory) > 0))
                {
                    GoSlices.SortFunc(pathsInDirectory, comparePaths);
                    sortedPaths = sortedPaths.Append(pathsInDirectory);
                }

                var newDirectory = directory.AsPath().Directory();
                if ((newDirectory == directory))
                {
                    break;
                }

                directory = newDirectory;
            }
        }

        if ((GoRuntime.Len(allFileNames) > 0))
        {
            var remainingPaths = Slice<ModulePath>.Make(0, allFileNames.Count);
            foreach (var remaining in allFileNames.Values) remainingPaths = remainingPaths.Append(remaining);
            GoSlices.SortFunc(remainingPaths, comparePaths);
            sortedPaths = sortedPaths.Append(remainingPaths);
        }

        return sortedPaths;
    }

    // containsIgnoredPath checks if a path contains patterns that should be ignored.
    // This is a local helper that duplicates tspath.ContainsIgnoredPath for performance.
    // Upstream tsc/internal/modulespecifiers/specifiers.go:250:1.
    private static bool containsIgnoredPath(RootedFilePath fileName)
    {
        return ((fileName.AsValue().Contains("/node_modules/."u8) || fileName.AsValue().Contains("/.git"u8)) || fileName.AsValue().Contains(".#"u8));
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:256:1.
    private static bool moduleSpecifierContainsNodeModules(ModuleSpecifier specifier)
    {
        return specifier.Contains("/node_modules/");
    }

    // GetEachFileNameOfModule returns all possible file paths for a module, including symlink alternatives.
    // This function handles symlink resolution and provides multiple path options for module resolution.
    // Upstream tsc/internal/modulespecifiers/specifiers.go:262:1.
    public static Slice<ModulePath> GetEachFileNameOfModule(RootedFilePath importingFileName, RootedFilePath importedFileName, IModuleSpecifierGenerationHost host, bool preferSymlinks)
    {
        var caseSensitivity = host.CaseSensitivity();
        var importedPath = caseSensitivity.PathKey(importedFileName.AsPath());
        RootedFilePath referenceRedirect = default;
        var outputAndReference = host.GetProjectReferenceFromSource(importedPath);
        if (((outputAndReference != null) && (outputAndReference.OutputDts != default)))
        {
            referenceRedirect = outputAndReference.OutputDts;
        }

        var redirects = host.GetRedirectTargets(importedPath);
        var importedFileNames = Slice<RootedFilePath>.Make(0);
        if ((referenceRedirect != default))
        {
            importedFileNames = importedFileNames.Append(referenceRedirect);
        }

        importedFileNames = importedFileNames.Append(importedFileName);
        importedFileNames = importedFileNames.Append(redirects);
        var targets = importedFileNames;
        var shouldFilterIgnoredPaths = !Core.Every(targets, containsIgnoredPath);
        var results = Slice<ModulePath>.Make(0, 2);
        if (!preferSymlinks)
        {
            {
                foreach (var p in GoRuntime.RangeValues(targets))
                {
                    if (!((shouldFilterIgnoredPaths && containsIgnoredPath(p))))
                    {
                        results = results.Append(new ModulePath { FileName = p, IsInNodeModules = p.ContainsLowercaseDirectorySequence("/node_modules/"), IsRedirect = (referenceRedirect == p) });
                    }
                }
            }
        }

        var symlinkCache = host.GetSymlinkCache();
        if ((symlinkCache != null))
        {
            TsPath.ForEachAncestorDirectoryPathStoppingAtGlobalCache(host.GetGlobalTypingsCacheLocation(), importedFileName.Directory(), (realPathDirectory) =>
            {
                var tuple2 = symlinkCache.DirectoriesByRealpath().Load(caseSensitivity.PathKey(realPathDirectory.AsPath()));
                var symlinkSet = tuple2.Item1;
                var ok = tuple2.Item2;
                if (!ok)
                {
                    return (false, false);
                }

                if (caseSensitivity.ContainsFilePath(realPathDirectory, importingFileName))
                {
                    return (false, true);
                }

                {
                    foreach (var range3 in GoRuntime.Range(targets))
                    {
                        var _2 = range3.Key;
                        var target = range3.Value;
                        var tuple4 = caseSensitivity.RelativeFilePathFromDirectory(realPathDirectory, target);
                        var relative = tuple4.Item1;
                        var ok2 = tuple4.Item2;
                        if (!ok2)
                        {
                            continue;
                        }

                        symlinkSet.Range((symlinkDirectory) =>
                        {
                            var option = symlinkDirectory.ResolveRelativeFile(relative);
                            results = results.Append(new ModulePath { FileName = option, IsInNodeModules = option.ContainsLowercaseDirectorySequence("/node_modules/"), IsRedirect = (target == referenceRedirect) });
                            shouldFilterIgnoredPaths = true;
                            return true;
                        });
                    }
                }

                return (false, false);
            });
        }

        if (preferSymlinks)
        {
            {
                foreach (var range5 in GoRuntime.Range(targets))
                {
                    var _3 = range5.Key;
                    var p2 = range5.Value;
                    if (!((shouldFilterIgnoredPaths && containsIgnoredPath(p2))))
                    {
                        results = results.Append(new ModulePath { FileName = p2, IsInNodeModules = p2.ContainsLowercaseDirectorySequence("/node_modules/"), IsRedirect = (referenceRedirect == p2) });
                    }
                }
            }
        }

        return results;
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:352:1.
    private static (Slice<ModuleSpecifier>, ResultKind) computeModuleSpecifiers(Slice<ModulePath> modulePaths, CompilerOptions compilerOptions, SourceFile importingSourceFile, IModuleSpecifierGenerationHost host, UserPreferences userPreferences, ModuleSpecifierOptions options, bool forAutoImport)
    {
        var info = getInfo(RootedFilePath.FromValue(importingSourceFile.FileNameGoValue()), host);
        var preferences = getModuleSpecifierPreferences(userPreferences, host, compilerOptions, importingSourceFile, "");
        var caseSensitivity = info.CaseSensitivity;
        ModuleSpecifier existingSpecifier = default;
        {
            foreach (var modulePath in GoRuntime.RangeValues(modulePaths))
            {
                var targetPath = caseSensitivity.PathKey(modulePath.FileName.AsPath());
                Node existingImport = default;
                {
                    foreach (var range2 in GoRuntime.Range(importingSourceFile.Imports()))
                    {
                        var _2 = range2.Key;
                        var importSpecifier = range2.Value;
                        var resolvedModule = host.GetResolvedModuleFromModuleSpecifier(importingSourceFile, importSpecifier);
                        if ((resolvedModule.IsResolved() && (resolvedModule.ResolvedPath == targetPath)))
                        {
                            existingImport = importSpecifier;
                            break;
                        }
                    }
                }

                if ((existingImport != null))
                {
                    if (((preferences.relativePreference == RelativePreferenceKind.NonRelative) && TsPath.PathIsRelative(existingImport.TextValue())))
                    {
                        continue;
                    }

                    var existingMode = host.GetModeForUsageLocation(importingSourceFile, existingImport);
                    var targetMode = options.OverrideImportMode;
                    if ((targetMode == ResolutionMode.None))
                    {
                        targetMode = host.GetDefaultResolutionModeForFile(importingSourceFile);
                    }

                    if ((((existingMode != targetMode) && (existingMode != ResolutionMode.None)) && (targetMode != ResolutionMode.None)))
                    {
                        continue;
                    }

                    existingSpecifier = TsPath.ToModuleSpecifier(existingImport.TextValue());
                    break;
                }
            }
        }

        if ((existingSpecifier != default))
        {
            return (Slice<ModuleSpecifier>.FromArray([existingSpecifier]), ResultKind.None);
        }

        var importedFileIsInNodeModules = Core.Some(modulePaths, (p) =>
        {
            return p.IsInNodeModules;
        });
        Slice<ModuleSpecifier> pathsSpecifiers = default;
        Slice<ModuleSpecifier> redirectPathsSpecifiers = default;
        Slice<ModuleSpecifier> nodeModulesSpecifiers = default;
        Slice<ModuleSpecifier> relativeSpecifiers = default;
        {
            foreach (var range3 in GoRuntime.Range(modulePaths))
            {
                var _3 = range3.Key;
                var modulePath2 = range3.Value;
                ModuleSpecifier specifier = default;
                if (modulePath2.IsInNodeModules)
                {
                    specifier = tryGetModuleNameAsNodeModule(modulePath2, info, importingSourceFile, host, compilerOptions, userPreferences, false, options.OverrideImportMode);
                }

                if (((specifier.AsValue().Length > 0) && !((forAutoImport && IsExcludedByRegex(specifier.AsValue(), preferences.excludeRegexes)))))
                {
                    nodeModulesSpecifiers = nodeModulesSpecifiers.Append(specifier);
                    if (modulePath2.IsRedirect)
                    {
                        return (nodeModulesSpecifiers, ResultKind.NodeModules);
                    }
                }

                var importMode = options.OverrideImportMode;
                if ((importMode == ResolutionMode.None))
                {
                    importMode = host.GetDefaultResolutionModeForFile(importingSourceFile);
                }

                var local = getLocalModuleSpecifier(modulePath2.FileName, info, compilerOptions, host, importMode, preferences, (modulePath2.IsRedirect || (specifier.AsValue().Length > 0)));
                if (((local.AsValue().Length == 0) || (forAutoImport && IsExcludedByRegex(local.AsValue(), preferences.excludeRegexes))))
                {
                    continue;
                }

                if (modulePath2.IsRedirect)
                {
                    redirectPathsSpecifiers = redirectPathsSpecifiers.Append(local);
                }
                else
                {
                    if (PathIsBareSpecifier(local))
                    {
                        if (moduleSpecifierContainsNodeModules(local))
                        {
                            relativeSpecifiers = relativeSpecifiers.Append(local);
                        }
                        else
                        {
                            pathsSpecifiers = pathsSpecifiers.Append(local);
                        }
                    }
                    else
                    {
                        if (((forAutoImport || !importedFileIsInNodeModules) || modulePath2.IsInNodeModules))
                        {
                            relativeSpecifiers = relativeSpecifiers.Append(local);
                        }
                    }
                }
            }
        }

        if ((GoRuntime.Len(pathsSpecifiers) > 0))
        {
            return (pathsSpecifiers, ResultKind.Paths);
        }

        if ((GoRuntime.Len(redirectPathsSpecifiers) > 0))
        {
            return (redirectPathsSpecifiers, ResultKind.Redirect);
        }

        if ((GoRuntime.Len(nodeModulesSpecifiers) > 0))
        {
            return (nodeModulesSpecifiers, ResultKind.NodeModules);
        }

        return (relativeSpecifiers, ResultKind.Relative);
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:479:1.
    private static ModuleSpecifier getLocalModuleSpecifier(RootedFilePath moduleFileName, Info info, CompilerOptions compilerOptions, IModuleSpecifierGenerationHost host, ResolutionMode importMode, ModuleSpecifierPreferences preferences, bool pathsOnly)
    {
        var paths = compilerOptions.Paths;
        var rootDirs = compilerOptions.GetEffectiveRootDirs();
        if ((pathsOnly && (paths == null)))
        {
            return default;
        }

        var sourceDirectory = info.SourceDirectory;
        var allowedEndings = preferences.getAllowedEndingsInPreferredOrder(importMode);
        ModuleSpecifier relativePath = default;
        if ((GoRuntime.Len(rootDirs) > 0))
        {
            relativePath = tryGetModuleNameFromRootDirs(rootDirs, moduleFileName, sourceDirectory, allowedEndings, compilerOptions, host);
        }

        if ((relativePath.AsValue().Length == 0))
        {
            var tuple1 = host.CaseSensitivity().RelativePathFromDirectory(sourceDirectory, moduleFileName);
            var relativePathFromSource = tuple1.Item1;
            var ok = tuple1.Item2;
            var path = moduleFileName.AsValue();
            if (ok)
            {
                path = relativePathFromSource.AsModuleSpecifier().AsValue();
            }

            relativePath = processEnding(TsPath.ToModuleSpecifier(path), moduleFileName, allowedEndings, compilerOptions, host);
        }

        if (((((paths == null) && !compilerOptions.GetResolvePackageJsonImports())) || (preferences.relativePreference == RelativePreferenceKind.Relative)))
        {
            if (pathsOnly)
            {
                return default;
            }

            return relativePath;
        }

        var baseDirectory = host.BaseDirectory();
        {
            var pathsBasePath = compilerOptions.GetPathsBasePath(baseDirectory);
            if ((pathsBasePath != default))
            {
                baseDirectory = pathsBasePath;
            }
        }

        var relativeToBaseUrl = getRelativePathIfInSameVolume(moduleFileName, baseDirectory, host.CaseSensitivity());
        if ((relativeToBaseUrl.AsValue().Length == 0))
        {
            if (pathsOnly)
            {
                return default;
            }

            return relativePath;
        }

        ModuleSpecifier fromPackageJsonImports = default;
        if (!pathsOnly)
        {
            fromPackageJsonImports = tryGetModuleNameFromPackageJsonImports(moduleFileName, sourceDirectory, compilerOptions, host, importMode, prefersTsExtension(allowedEndings));
        }

        ByteStringValue fromPaths = default;
        if ((((pathsOnly || (fromPackageJsonImports.AsValue().Length == 0))) && (paths != null)))
        {
            fromPaths = tryGetModuleNameFromPaths(relativeToBaseUrl.AsValue(), moduleFileName, paths, allowedEndings, baseDirectory, host, compilerOptions);
        }

        if (pathsOnly)
        {
            return TsPath.ToModuleSpecifier(fromPaths);
        }

        ByteStringValue maybeNonRelative = default;
        if ((fromPackageJsonImports.AsValue().Length > 0))
        {
            maybeNonRelative = fromPackageJsonImports.AsValue();
        }
        else
        {
            maybeNonRelative = fromPaths;
        }

        if ((GoRuntime.Len(maybeNonRelative) == 0))
        {
            return relativePath;
        }

        var relativeIsExcluded = IsExcludedByRegex(relativePath.AsValue(), preferences.excludeRegexes);
        var nonRelativeIsExcluded = IsExcludedByRegex(maybeNonRelative, preferences.excludeRegexes);
        if ((!relativeIsExcluded && nonRelativeIsExcluded))
        {
            return relativePath;
        }

        if ((relativeIsExcluded && !nonRelativeIsExcluded))
        {
            return TsPath.ToModuleSpecifier(maybeNonRelative);
        }

        if (((preferences.relativePreference == RelativePreferenceKind.NonRelative) && !TsPath.PathIsRelative(maybeNonRelative)))
        {
            return TsPath.ToModuleSpecifier(maybeNonRelative);
        }

        if (((preferences.relativePreference == RelativePreferenceKind.ExternalNonRelative) && !TsPath.PathIsRelative(maybeNonRelative)))
        {
            PathKey projectDirectory = default;
            {
                var configFileName = compilerOptions.ConfigFilePath;
                if ((configFileName != default))
                {
                    projectDirectory = host.CaseSensitivity().PathKey(configFileName.Directory().AsPath());
                }
                else
                {
                    projectDirectory = host.CaseSensitivity().PathKey(host.BaseDirectory().AsPath());
                }
            }

            var caseSensitivity = host.CaseSensitivity();
            var canonicalSourceDirectory = caseSensitivity.PathKey(sourceDirectory.AsPath());
            var modulePath = caseSensitivity.PathKey(moduleFileName.AsPath());
            var sourceIsInternal = projectDirectory.ContainsPath(canonicalSourceDirectory);
            var targetIsInternal = projectDirectory.ContainsPath(modulePath);
            if (((sourceIsInternal && !targetIsInternal) || (!sourceIsInternal && targetIsInternal)))
            {
                return TsPath.ToModuleSpecifier(maybeNonRelative);
            }

            var nearestTargetPackageJson = host.GetNearestAncestorDirectoryWithPackageJson(moduleFileName.Directory());
            var nearestSourcePackageJson = host.GetNearestAncestorDirectoryWithPackageJson(sourceDirectory);
            if (!packageJsonPathsAreEqual(nearestTargetPackageJson, nearestSourcePackageJson, host.CaseSensitivity()))
            {
                return TsPath.ToModuleSpecifier(maybeNonRelative);
            }

            return relativePath;
        }

        if ((maybeNonRelative.StartsWith(".."u8) || (CountPathComponents(relativePath.AsValue()) < CountPathComponents(maybeNonRelative))))
        {
            return relativePath;
        }

        return TsPath.ToModuleSpecifier(maybeNonRelative);
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:639:1.
    private static ModuleSpecifier processEnding(ModuleSpecifier specifier, RootedFilePath sourceFileName, Slice<ModuleSpecifierEnding> allowedEndings, CompilerOptions options, IModuleSpecifierGenerationHost host)
    {
        var fileName = specifier.AsValue();
        if (TsPath.FileExtensionIsOneOf(fileName, new List<string> { TsPath.ExtensionJson, TsPath.ExtensionMjs, TsPath.ExtensionCjs }))
        {
            return specifier;
        }

        var noExtension = TsPath.RemoveFileExtension(fileName);
        if ((fileName.EqualsValue(noExtension)))
        {
            return specifier;
        }

        var jsPriority = GoSlices.Index(allowedEndings, ModuleSpecifierEnding.JsExtension);
        var tsPriority = GoSlices.Index(allowedEndings, ModuleSpecifierEnding.TsExtension);
        if (((TsPath.FileExtensionIsOneOf(fileName, new List<string> { TsPath.ExtensionMts, TsPath.ExtensionCts }) && (tsPriority != -1)) && (tsPriority < jsPriority)))
        {
            return specifier;
        }

        if (TsPath.FileExtensionIsOneOf(fileName, new List<string> { TsPath.ExtensionDmts, TsPath.ExtensionDcts }))
        {
            var inputExt = TsPath.GetDeclarationFileExtension(fileName);
            var ext = GetJSExtensionValueForDeclarationFileExtension(inputExt);
            return TsPath.ToModuleSpecifier(TsPath.pathConcat(TsPath.RemoveExtension(fileName, inputExt), ext));
        }

        if (TsPath.FileExtensionIsOneOf(fileName, new List<string> { TsPath.ExtensionMts, TsPath.ExtensionCts }))
        {
            return TsPath.ToModuleSpecifier(TsPath.pathConcat(noExtension, ByteStringValue.FromString(getJSExtensionForFile(fileName, options))));
        }

        if (((!TsPath.FileExtensionIsOneOf(fileName, new List<string> { TsPath.ExtensionDts }) && TsPath.FileExtensionIsOneOf(fileName, new List<string> { TsPath.ExtensionTs })) && fileName.Contains(".d."u8)))
        {
            {
                var result = TryGetRealFileNameValueForNonJSDeclarationFileName(fileName);
                if ((result.Length != 0))
                {
                    return TsPath.ToModuleSpecifier(result);
                }
            }
        }

        switch (allowedEndings[0])
        {
            case ModuleSpecifierEnding.Minimal:
            {
                var withoutIndex = GoStrings.TrimSuffix(noExtension, ByteStringValue.FromString("/index"));
                if ((((host != null) && (!withoutIndex.EqualsValue(noExtension))) && tryGetAnyFileFromPath(host, TsPath.RootedFilePathFromPath(sourceFileName.Directory().AsPath()))))
                {
                    return TsPath.ToModuleSpecifier(noExtension);
                }

                return TsPath.ToModuleSpecifier(withoutIndex);
            }

            case ModuleSpecifierEnding.Index:
            {
                return TsPath.ToModuleSpecifier(noExtension);
            }

            case ModuleSpecifierEnding.JsExtension:
            {
                return TsPath.ToModuleSpecifier(TsPath.pathConcat(noExtension, ByteStringValue.FromString(getJSExtensionForFile(fileName, options))));
            }

            case ModuleSpecifierEnding.TsExtension:
            {
                if (TsPath.IsDeclarationFileName(fileName))
                {
                    var extensionlessPriority = -1;
                    {
                        foreach (var range1 in GoRuntime.Range(allowedEndings))
                        {
                            var i = range1.Key;
                            var e = range1.Value;
                            if (((e == ModuleSpecifierEnding.Minimal) || (e == ModuleSpecifierEnding.Index)))
                            {
                                extensionlessPriority = i;
                                break;
                            }
                        }
                    }

                    if (((extensionlessPriority != -1) && (extensionlessPriority < jsPriority)))
                    {
                        return TsPath.ToModuleSpecifier(noExtension);
                    }

                    return TsPath.ToModuleSpecifier(TsPath.pathConcat(noExtension, ByteStringValue.FromString(getJSExtensionForFile(fileName, options))));
                }

                return specifier;
            }

            default:
            {
                throw new InvalidOperationException("Unexpected module specifier ending: " + allowedEndings[0]);
            }
        }
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:712:1.
    private static ModuleSpecifier tryGetModuleNameFromRootDirs(List<RootedDirectoryPath> rootDirs, RootedFilePath moduleFileName, RootedDirectoryPath sourceDirectory, Slice<ModuleSpecifierEnding> allowedEndings, CompilerOptions compilerOptions, IModuleSpecifierGenerationHost host)
    {
        var normalizedTargetPaths = getPathsRelativeToRootDirs(moduleFileName, rootDirs, host.CaseSensitivity());
        if ((GoRuntime.Len(normalizedTargetPaths) == 0))
        {
            return default;
        }

        var normalizedSourcePaths = getPathsRelativeToRootDirs(TsPath.RootedFilePathFromPath(sourceDirectory.AsPath()), rootDirs, host.CaseSensitivity());
        ModuleSpecifier shortest = default;
        int shortestSepCount = default;
        {
            foreach (var sourcePath in GoRuntime.RangeValues(normalizedSourcePaths))
            {
                {
                    foreach (var range2 in GoRuntime.Range(normalizedTargetPaths))
                    {
                        var _2 = range2.Key;
                        var targetPath = range2.Value;
                        var candidate = host.CaseSensitivity().RelativePathFromRelativeDirectory(sourcePath, targetPath).AsModuleSpecifier();
                        var candidateSepCount = GoStrings.Count(candidate.AsValue(), "/");
                        if (((shortest.AsValue().Length == 0) || (candidateSepCount < shortestSepCount)))
                        {
                            shortest = candidate;
                            shortestSepCount = candidateSepCount;
                        }
                    }
                }
            }
        }

        if ((shortest.AsValue().Length == 0))
        {
            return default;
        }

        return processEnding(shortest, moduleFileName, allowedEndings, compilerOptions, host);
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:745:1.
    private static ModuleSpecifier tryGetModuleNameAsNodeModule(ModulePath pathObj, Info info, SourceFile importingSourceFile, IModuleSpecifierGenerationHost host, CompilerOptions options, UserPreferences userPreferences, bool packageNameOnly, ResolutionMode overrideMode)
    {
        var parts = GetNodeModulePathParts(pathObj.FileName);
        if ((parts == null))
        {
            return default;
        }

        var preferences = getModuleSpecifierPreferences(userPreferences, host, options, importingSourceFile, "");
        var allowedEndings = preferences.getAllowedEndingsInPreferredOrder(ResolutionMode.None);
        var caseSensitivity = host.CaseSensitivity();
        var moduleSpecifier = pathObj.FileName.AsValue();
        var isPackageRootPath = false;
        if (!packageNameOnly)
        {
            if (parts.IsDirectNodeModulesFile)
            {
                moduleSpecifier = processEnding(pathObj.FileName.AsModuleSpecifier(), pathObj.FileName, allowedEndings, options, host).AsValue();
            }
            else
            {
                RootedFilePath moduleFileName = default;
                var packageRootDirectory = parts.PackageRootDirectory;
                var relativeComponents = GoStrings.Split(parts.PackageRelativePath.AsValue(), "/");
                {
                    var i = 0;
                    for (; true; i++)
                    {
                        var tuple1 = packageRootDirectory.AsPath().RelativeTo(parts.TopLevelNodeModulesDirectory);
                        var packagePath = tuple1.Item1;
                        var ok = tuple1.Item2;
                        if (!ok)
                        {
                            return default;
                        }

                        var pkgJsonResults = tryDirectoryWithPackageJson(packageRootDirectory, parts.PackageRootDirectory, packagePath.AsValue(), pathObj, importingSourceFile, host, overrideMode, options, allowedEndings);
                        var moduleFileToTry = pkgJsonResults.moduleFileToTry;
                        var resolvedPackageRoot = pkgJsonResults.packageRootDirectory;
                        var blockedByExports = pkgJsonResults.blockedByExports;
                        if (blockedByExports)
                        {
                            return default;
                        }

                        if ((pkgJsonResults.verbatimFromExports != default))
                        {
                            return pkgJsonResults.verbatimFromExports;
                        }

                        if ((resolvedPackageRoot != default))
                        {
                            moduleSpecifier = resolvedPackageRoot.AsValue();
                            isPackageRootPath = true;
                            break;
                        }

                        if ((moduleFileName.AsValue().Length == 0))
                        {
                            moduleFileName = moduleFileToTry;
                        }

                        if ((i >= (GoRuntime.Len(relativeComponents) - 1)))
                        {
                            moduleSpecifier = processEnding(moduleFileName.AsModuleSpecifier(), moduleFileName, allowedEndings, options, host).AsValue();
                            break;
                        }

                        packageRootDirectory = packageRootDirectory.ResolveDirectory(relativeComponents[i]);
                    }
                }
            }
        }

        if ((pathObj.IsRedirect && !isPackageRootPath))
        {
            return default;
        }

        var globalTypingsCacheLocation = host.GetGlobalTypingsCacheLocation();
        if ((!caseSensitivity.ContainsPath(parts.TopLevelNodeModulesSearchRoot, info.SourceDirectory.AsPath()) || ((globalTypingsCacheLocation != default) && caseSensitivity.ContainsPath(parts.TopLevelNodeModulesSearchRoot, globalTypingsCacheLocation.AsPath()))))
        {
            return default;
        }

        var nodeModulesPrefix = TsPath.EnsureTrailingDirectorySeparator(parts.TopLevelNodeModulesDirectory.AsValue());
        if (!moduleSpecifier.StartsWith(nodeModulesPrefix))
        {
            return default;
        }

        var nodeModulesDirectoryName = GoRuntime.Slice(moduleSpecifier, GoRuntime.Len(nodeModulesPrefix));
        return TsPath.ToModuleSpecifier(ModuleFunctions.GetPackageNameFromTypesPackageNameValue(nodeModulesDirectoryName));
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:846:1.
    private static pkgJsonDirAttemptResult tryDirectoryWithPackageJson(RootedDirectoryPath packageRootDirectory, RootedDirectoryPath packageBaseDirectory, ByteStringValue packageName, ModulePath pathObj, SourceFile importingSourceFile, IModuleSpecifierGenerationHost host, ResolutionMode overrideMode, CompilerOptions options, Slice<ModuleSpecifierEnding> allowedEndings)
    {
        var packageJsonPath = packageRootDirectory.ResolveFile("package.json");
        var moduleFileToTry = pathObj.FileName;
        var maybeBlockedByTypesVersions = false;
        var packageJson = host.GetPackageJsonInfo(packageJsonPath);
        if ((packageJson == null))
        {
            var tuple1 = moduleFileToTry.RelativeTo(packageBaseDirectory);
            var relative = tuple1.Item1;
            var ok = tuple1.Item2;
            if (!ok)
            {
                throw new InvalidOperationException("module file was not contained by package root");
            }

            var fileName = relative.AsValue();
            if ((((fileName.EqualsBytes("index.d.ts"u8) || fileName.EqualsBytes("index.js"u8)) || fileName.EqualsBytes("index.ts"u8)) || fileName.EqualsBytes("index.tsx"u8)))
            {
                return new pkgJsonDirAttemptResult
                {
                    moduleFileToTry = moduleFileToTry,
                    packageRootDirectory = packageRootDirectory
                };
            }
            else
            {
                return new pkgJsonDirAttemptResult
                {
                    moduleFileToTry = moduleFileToTry
                };
            }
        }

        var importMode = overrideMode;
        if ((importMode == ResolutionMode.None))
        {
            importMode = host.GetDefaultResolutionModeForFile(importingSourceFile);
        }

        var packageJsonContent = packageJson.GetContents();
        if (options.GetResolvePackageJsonExports())
        {
            var packageName2 = ModuleFunctions.GetPackageNameFromTypesPackageNameValue(packageName);
            if (pathObj.FileName.ExtensionIsOneOf(new List<string> { TsPath.ExtensionCjs, TsPath.ExtensionCts, TsPath.ExtensionDcts }))
            {
                importMode = ResolutionMode.CommonJS;
            }
            else
            {
                if (pathObj.FileName.ExtensionIsOneOf(new List<string> { TsPath.ExtensionMjs, TsPath.ExtensionMts, TsPath.ExtensionDmts }))
                {
                    importMode = ResolutionMode.ESNext;
                }
            }

            var conditions = DefaultResolver.GetConditions(options, importMode);
            ModuleSpecifier fromExports = default;
            if (((packageJsonContent != null) && (packageJsonContent.Fields.Exports.Type != JSONValueType.NotPresent)))
            {
                fromExports = tryGetModuleNameFromExports(options, host, pathObj.FileName, packageRootDirectory, packageName2, packageJsonContent.Fields.Exports, conditions);
            }

            if ((fromExports.AsValue().Length > 0))
            {
                return new pkgJsonDirAttemptResult
                {
                    verbatimFromExports = fromExports
                };
            }

            if (((packageJsonContent != null) && (packageJsonContent.Fields.Exports.Type != JSONValueType.NotPresent)))
            {
                return new pkgJsonDirAttemptResult
                {
                    moduleFileToTry = moduleFileToTry,
                    blockedByExports = true
                };
            }
        }

        VersionPaths versionPaths = default;
        if (((packageJsonContent != null) && (packageJsonContent.Fields.TypesVersions.Type == JSONValueType.Object)))
        {
            versionPaths = packageJsonContent.GetVersionPaths(null);
        }

        if ((versionPaths.GetPaths() != null))
        {
            var tuple2 = moduleFileToTry.RelativeTo(packageRootDirectory);
            var subModuleName = tuple2.Item1;
            var ok2 = tuple2.Item2;
            if (!ok2)
            {
                throw new InvalidOperationException("module file was not contained by package root");
            }

            var fromPaths = tryGetModuleNameFromPaths(subModuleName.AsValue(), moduleFileToTry, versionPaths.GetPaths(), allowedEndings, packageRootDirectory, host, options);
            if ((GoRuntime.Len(fromPaths) == 0))
            {
                maybeBlockedByTypesVersions = true;
            }
            else
            {
                moduleFileToTry = packageRootDirectory.ResolveFile(fromPaths);
            }
        }

        var mainFileRelative = "index.js";
        if ((packageJsonContent != null))
        {
            if (packageJsonContent.Fields.Typings.Valid)
            {
                mainFileRelative = packageJsonContent.Fields.Typings.Value;
            }
            else
            {
                if (packageJsonContent.Fields.Types.Valid)
                {
                    mainFileRelative = packageJsonContent.Fields.Types.Value;
                }
                else
                {
                    if (packageJsonContent.Fields.Main.Valid)
                    {
                        mainFileRelative = packageJsonContent.Fields.Main.Value;
                    }
                }
            }
        }

        if (((GoRuntime.Len(mainFileRelative) > 0) && !((maybeBlockedByTypesVersions && (DefaultResolver.MatchPatternOrExact(DefaultResolver.TryParsePatterns(versionPaths.GetPaths()), mainFileRelative).IsValid())))))
        {
            var caseSensitivity = host.CaseSensitivity();
            var packageType = "";
            if ((packageJsonContent != null))
            {
                packageType = packageJsonContent.Fields.Type.Value;
            }

            if (isPackageMainFile(moduleFileToTry, packageRootDirectory, mainFileRelative, packageType, caseSensitivity))
            {
                return new pkgJsonDirAttemptResult
                {
                    packageRootDirectory = packageRootDirectory,
                    moduleFileToTry = moduleFileToTry
                };
            }
        }

        return new pkgJsonDirAttemptResult
        {
            moduleFileToTry = moduleFileToTry
        };
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:982:1.
    private static bool isPackageMainFile(RootedFilePath moduleFileName, RootedDirectoryPath packageRootDirectory, string mainFileRelative, string packageType, CaseSensitivity caseSensitivity)
    {
        var mainIsDirectory = TsPath.HasTrailingDirectorySeparator(mainFileRelative);
        var mainExportFile = packageRootDirectory.ResolveFile(mainFileRelative);
        if ((!mainIsDirectory && (caseSensitivity.CompareFileNameStems(mainExportFile.RemoveFileExtension(), moduleFileName.RemoveFileExtension()) == 0)))
        {
            return true;
        }

        var mainExportDirectory = TsPath.RootedDirectoryPathFromPath(mainExportFile.AsPath());
        return ((((packageType != "module") && !moduleFileName.ExtensionIsOneOf(TsPath.ExtensionsNotSupportingExtensionlessResolution)) && (caseSensitivity.ComparePaths(moduleFileName.Directory().AsPath(), mainExportDirectory.AsPath()) == 0)) && (TsPath.RemoveFileExtension(moduleFileName.BaseNameValue()).EqualsBytes("index"u8)));
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:1003:1.
    private static ModuleSpecifier tryGetModuleNameFromExports(CompilerOptions options, IModuleSpecifierGenerationHost host, RootedFilePath targetFileName, RootedDirectoryPath packageDirectory, ByteStringValue packageName, ExportsOrImports exports, Slice<string> conditions)
    {
        if (exports.IsSubpaths())
        {
            {
                foreach (var range1 in exports.AsObject().Entries())
                {
                    var k = range1.Key;
                    var subk = range1.Value;
                    var subPackageName = TsPath.ResolvePathWithoutTrailingDirectorySeparator(packageName, Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(k)]));
                    var mode = MatchingMode.Exact;
                    if (GoStrings.HasSuffix(k, "/"))
                    {
                        mode = MatchingMode.Directory;
                    }
                    else
                    {
                        if (GoStrings.Contains(k, "*"))
                        {
                            mode = MatchingMode.Pattern;
                        }
                    }

                    var result = tryGetModuleNameFromExportsOrImports(options, host, targetFileName, packageDirectory, subPackageName, subk, conditions, mode, false, false);
                    if ((GoRuntime.Len(result) > 0))
                    {
                        return TsPath.ToModuleSpecifier(result);
                    }
                }
            }
        }

        return TsPath.ToModuleSpecifier(tryGetModuleNameFromExportsOrImports(options, host, targetFileName, packageDirectory, packageName, exports, conditions, MatchingMode.Exact, false, false));
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:1046:1.
    private static ModuleSpecifier tryGetModuleNameFromPackageJsonImports(RootedFilePath moduleFileName, RootedDirectoryPath sourceDirectory, CompilerOptions options, IModuleSpecifierGenerationHost host, ResolutionMode importMode, bool preferTsExtension)
    {
        if (!options.GetResolvePackageJsonImports())
        {
            return default;
        }

        var ancestorDirectoryWithPackageJson = host.GetNearestAncestorDirectoryWithPackageJson(sourceDirectory);
        if ((ancestorDirectoryWithPackageJson.AsValue().Length == 0))
        {
            return default;
        }

        var packageJsonPath = ancestorDirectoryWithPackageJson.ResolveFile("package.json");
        var info = host.GetPackageJsonInfo(packageJsonPath);
        if ((info == null))
        {
            return default;
        }

        var imports = info.GetContents().Fields.Imports;
        switch (imports.Type)
        {
            case JSONValueType.NotPresent:
            case JSONValueType.Array:
            case JSONValueType.String:
            {
                return default;
            }

            case JSONValueType.Object:
            {
                var conditions = DefaultResolver.GetConditions(options, importMode);
                var top = imports.AsObject();
                var entries = top.Entries();
                {
                    foreach (var range1 in entries)
                    {
                        var k = range1.Key;
                        var value = range1.Value;
                        if ((((k == "#") || (k == "#/")) || !GoStrings.HasPrefix(k, "#")))
                        {
                            continue;
                        }

                        if (((GoStrings.HasPrefix(k, "#/") && (options.GetModuleResolutionKind() != ModuleResolutionKind.NodeNext)) && (options.GetModuleResolutionKind() != ModuleResolutionKind.Bundler)))
                        {
                            continue;
                        }

                        var mode = MatchingMode.Exact;
                        if (GoStrings.HasSuffix(k, "/"))
                        {
                            mode = MatchingMode.Directory;
                        }
                        else
                        {
                            if (GoStrings.Contains(k, "*"))
                            {
                                mode = MatchingMode.Pattern;
                            }
                        }

                        var result = tryGetModuleNameFromExportsOrImports(options, host, moduleFileName, ancestorDirectoryWithPackageJson, ByteStringValue.FromString(k), value, conditions, mode, true, preferTsExtension);
                        if ((GoRuntime.Len(result) > 0))
                        {
                            return TsPath.ToModuleSpecifier(result);
                        }
                    }
                }

                break;
            }
        }

        return default;
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:1116:1.
    private static ByteStringValue tryGetModuleNameFromPaths(ByteStringValue relativeToBaseUrl, RootedFilePath fileName, OrderedMap<string, List<string>> paths, Slice<ModuleSpecifierEnding> allowedEndings, RootedDirectoryPath baseDirectory, IModuleSpecifierGenerationHost host, CompilerOptions compilerOptions)
    {
        var caseSensitivity = host.CaseSensitivity();
        {
            foreach (var range1 in paths.Entries())
            {
                var key = range1.Key;
                var values = range1.Value;
                {
                    foreach (var patternText in GoRuntime.RangeValues(values))
                    {
                        var normalized = TsPath.NormalizePath(ByteStringValue.FromString(patternText));
                        var pattern = resolvePathPatternIfInSameVolume(normalized, baseDirectory, caseSensitivity);
                        if ((GoRuntime.Len(pattern) == 0))
                        {
                            pattern = normalized;
                        }

                        var tuple3 = GoStrings.Cut(pattern, ByteStringValue.FromString("*"));
                        var prefix = tuple3.Item1;
                        var suffix = tuple3.Item2;
                        var ok = tuple3.Item3;
                        List<specPair> candidates = default;
                        {
                            foreach (var range4 in GoRuntime.Range(allowedEndings))
                            {
                                var _2 = range4.Key;
                                var ending = range4.Value;
                                var result = processEnding(TsPath.ToModuleSpecifier(relativeToBaseUrl), fileName, Slice<ModuleSpecifierEnding>.FromArray([ending]), compilerOptions, host);
                                candidates = GoRuntime.Append(candidates, new specPair { ending = ending, value = result });
                            }
                        }

                        if ((GoRuntime.Len(TsPath.TryGetExtensionFromPath(pattern)) > 0))
                        {
                            candidates = GoRuntime.Append(candidates, new specPair { ending = ModuleSpecifierEnding.JsExtension, value = TsPath.ToModuleSpecifier(relativeToBaseUrl) });
                        }

                        if (ok)
                        {
                            {
                                foreach (var range5 in GoRuntime.Range(candidates))
                                {
                                    var _3 = range5.Key;
                                    var c = range5.Value;
                                    var value = c.value.AsValue();
                                    if (((((GoRuntime.Len(value) >= (GoRuntime.Len(prefix) + GoRuntime.Len(suffix))) && StringUtil.HasPrefix(value, prefix, caseSensitivity.IsCaseSensitive())) && StringUtil.HasSuffix(value, suffix, caseSensitivity.IsCaseSensitive())) && validateEnding(c, relativeToBaseUrl, fileName, compilerOptions, host)))
                                    {
                                        var matchedStar = GoRuntime.Slice(value, GoRuntime.Len(prefix), (GoRuntime.Len(value) - GoRuntime.Len(suffix)));
                                        if (!TsPath.PathIsRelative(matchedStar))
                                        {
                                            return replaceFirstStar(ByteStringValue.FromString(key), matchedStar);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if ((Core.Some(candidates, (c2) =>
                            {
                                return ((c2.ending != ModuleSpecifierEnding.Minimal) && (pattern.EqualsValue(c2.value.AsValue())));
                            }) || Core.Some(candidates, (c3) =>
                            {
                                return (((c3.ending == ModuleSpecifierEnding.Minimal) && (pattern.EqualsValue(c3.value.AsValue()))) && validateEnding(c3, relativeToBaseUrl, fileName, compilerOptions, host));
                            })))
                            {
                                return ByteStringValue.FromString(key);
                            }
                        }
                    }
                }
            }
        }

        return default;
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:1219:1.
    private static bool validateEnding(specPair c, ByteStringValue relativeToBaseUrl, RootedFilePath fileName, CompilerOptions compilerOptions, IModuleSpecifierGenerationHost host)
    {
        return ((c.ending != ModuleSpecifierEnding.Minimal) || (c.value == processEnding(TsPath.ToModuleSpecifier(relativeToBaseUrl), fileName, Slice<ModuleSpecifierEnding>.FromArray([c.ending]), compilerOptions, host)));
    }

    // Upstream tsc/internal/modulespecifiers/specifiers.go:1230:1.
    private static ByteStringValue tryGetModuleNameFromExportsOrImports(CompilerOptions options, IModuleSpecifierGenerationHost host, RootedFilePath targetFileName, RootedDirectoryPath packageDirectory, ByteStringValue packageName, ExportsOrImports exports, Slice<string> conditions, MatchingMode mode, bool isImports, bool preferTsExtension)
    {
        var packageSpecifier = TsPath.ToModuleSpecifier(packageName);
        switch (exports.Type)
        {
            case JSONValueType.NotPresent:
            {
                return default;
            }

            case JSONValueType.String:
            {
                var strValue = ByteStringValue.FromString((string)exports.Value);
                RootedFilePath outputFile = default;
                RootedFilePath declarationFile = default;
                if (isImports)
                {
                    outputFile = OutputPathFunctions.GetOutputJSFileNameWorker(targetFileName, options, host);
                    declarationFile = OutputPathFunctions.GetOutputDeclarationFileNameWorker(targetFileName, options, host);
                }

                RootedFilePath extensionSwappedTarget = default;
                if (targetFileName.HasTSFileExtension())
                {
                    extensionSwappedTarget = targetFileName.RemoveFileExtension().AppendSuffix(ModuleFunctions.TryGetJSExtensionForFileName(targetFileName, options));
                }

                var canTryTsExtension = (preferTsExtension && targetFileName.HasImplementationTSFileExtension());
                var caseSensitivity = host.CaseSensitivity();
                switch (mode)
                {
                    case MatchingMode.Exact:
                    {
                        if (TsPath.HasTrailingDirectorySeparator(strValue))
                        {
                            return default;
                        }

                        var resolvedTarget = packageDirectory.ResolveFile(strValue);
                        if ((((((extensionSwappedTarget.AsValue().Length > 0) && (caseSensitivity.CompareFilePaths(extensionSwappedTarget, resolvedTarget) == 0)) || (caseSensitivity.CompareFilePaths(targetFileName, resolvedTarget) == 0)) || ((outputFile.AsValue().Length > 0) && (caseSensitivity.CompareFilePaths(outputFile, resolvedTarget) == 0))) || ((declarationFile.AsValue().Length > 0) && (caseSensitivity.CompareFilePaths(declarationFile, resolvedTarget) == 0))))
                        {
                            return packageName;
                        }

                        break;
                    }

                    case MatchingMode.Directory:
                    {
                        var resolvedTarget2 = packageDirectory.ResolveDirectory(TsPath.RemoveTrailingDirectorySeparator(strValue));
                        if ((canTryTsExtension && caseSensitivity.ContainsPath(TsPath.RootedDirectoryPathFromPath(targetFileName.AsPath()), resolvedTarget2.AsPath())))
                        {
                            var tuple1 = caseSensitivity.RelativePathFromDirectory(resolvedTarget2, targetFileName);
                            var fragment = tuple1.Item1;
                            return packageSpecifier.Resolve(Slice<ByteStringValue>.FromArray([strValue, fragment.AsValue()])).AsValue();
                        }

                        if (((extensionSwappedTarget.AsValue().Length > 0) && caseSensitivity.ContainsFilePath(resolvedTarget2, extensionSwappedTarget)))
                        {
                            var tuple2 = caseSensitivity.RelativePathFromDirectory(resolvedTarget2, extensionSwappedTarget);
                            var fragment2 = tuple2.Item1;
                            return packageSpecifier.Resolve(Slice<ByteStringValue>.FromArray([strValue, fragment2.AsValue()])).AsValue();
                        }

                        if ((!canTryTsExtension && caseSensitivity.ContainsFilePath(resolvedTarget2, targetFileName)))
                        {
                            var tuple3 = caseSensitivity.RelativePathFromDirectory(resolvedTarget2, targetFileName);
                            var fragment3 = tuple3.Item1;
                            return packageSpecifier.Resolve(Slice<ByteStringValue>.FromArray([strValue, fragment3.AsValue()])).AsValue();
                        }

                        if (((outputFile.AsValue().Length > 0) && caseSensitivity.ContainsFilePath(resolvedTarget2, outputFile)))
                        {
                            var tuple4 = caseSensitivity.RelativePathFromDirectory(resolvedTarget2, outputFile);
                            var fragment4 = tuple4.Item1;
                            return packageSpecifier.CombineRelative(fragment4).AsValue();
                        }

                        if (((declarationFile.AsValue().Length > 0) && caseSensitivity.ContainsFilePath(resolvedTarget2, declarationFile)))
                        {
                            var tuple5 = caseSensitivity.RelativePathFromDirectory(resolvedTarget2, declarationFile);
                            var fragment5 = tuple5.Item1;
                            var jsExtension = getJSExtensionForFileName(declarationFile, options);
                            var fragmentWithJsExtension = fragment5.ChangeExtension(jsExtension);
                            return packageSpecifier.CombineRelative(fragmentWithJsExtension).AsValue();
                        }

                        break;
                    }

                    case MatchingMode.Pattern:
                    {
                        var pathOrPattern = TsPath.ResolvePath(packageDirectory.AsValue(), Slice<ByteStringValue>.FromArray([strValue]));
                        var tuple6 = GoStrings.Cut(pathOrPattern, ByteStringValue.FromString("*"));
                        var leadingSlice = tuple6.Item1;
                        var trailingSlice = tuple6.Item2;
                        var caseSensitivity2 = host.CaseSensitivity();
                        var targetFilePath = targetFileName.AsValue();
                        if ((canTryTsExtension && StringUtil.HasPrefixAndSuffixWithoutOverlap(targetFilePath, leadingSlice, trailingSlice, caseSensitivity2.IsCaseSensitive())))
                        {
                            var starReplacement = GoRuntime.Slice(targetFilePath, GoRuntime.Len(leadingSlice), (GoRuntime.Len(targetFilePath) - GoRuntime.Len(trailingSlice)));
                            return replaceFirstStar(packageName, starReplacement);
                        }

                        {
                            var extensionSwappedTargetString = extensionSwappedTarget.AsValue();
                            if (((GoRuntime.Len(extensionSwappedTargetString) > 0) && StringUtil.HasPrefixAndSuffixWithoutOverlap(extensionSwappedTargetString, leadingSlice, trailingSlice, caseSensitivity2.IsCaseSensitive())))
                            {
                                var starReplacement2 = GoRuntime.Slice(extensionSwappedTargetString, GoRuntime.Len(leadingSlice), (GoRuntime.Len(extensionSwappedTargetString) - GoRuntime.Len(trailingSlice)));
                                return replaceFirstStar(packageName, starReplacement2);
                            }
                        }

                        if ((!canTryTsExtension && StringUtil.HasPrefixAndSuffixWithoutOverlap(targetFilePath, leadingSlice, trailingSlice, caseSensitivity2.IsCaseSensitive())))
                        {
                            var starReplacement3 = GoRuntime.Slice(targetFilePath, GoRuntime.Len(leadingSlice), (GoRuntime.Len(targetFilePath) - GoRuntime.Len(trailingSlice)));
                            return replaceFirstStar(packageName, starReplacement3);
                        }

                        {
                            var outputFileString = outputFile.AsValue();
                            if (((GoRuntime.Len(outputFileString) > 0) && StringUtil.HasPrefixAndSuffixWithoutOverlap(outputFileString, leadingSlice, trailingSlice, caseSensitivity2.IsCaseSensitive())))
                            {
                                var starReplacement4 = GoRuntime.Slice(outputFileString, GoRuntime.Len(leadingSlice), (GoRuntime.Len(outputFileString) - GoRuntime.Len(trailingSlice)));
                                return replaceFirstStar(packageName, starReplacement4);
                            }
                        }

                        {
                            var declarationFileString = declarationFile.AsValue();
                            if (((GoRuntime.Len(declarationFileString) > 0) && StringUtil.HasPrefixAndSuffixWithoutOverlap(declarationFileString, leadingSlice, trailingSlice, caseSensitivity2.IsCaseSensitive())))
                            {
                                var starReplacement5 = GoRuntime.Slice(declarationFileString, GoRuntime.Len(leadingSlice), (GoRuntime.Len(declarationFileString) - GoRuntime.Len(trailingSlice)));
                                var substituted = replaceFirstStar(packageName, starReplacement5);
                                var jsExtension2 = ModuleFunctions.TryGetJSExtensionForFileName(declarationFile, options);
                                if ((GoRuntime.Len(jsExtension2) > 0))
                                {
                                    return TsPath.ChangeFullExtension(substituted, jsExtension2);
                                }
                            }
                        }

                        break;
                    }
                }

                return default;
            }

            case JSONValueType.Array:
            {
                var arr = exports.AsArray();
                {
                    foreach (var range7 in GoRuntime.Range(arr))
                    {
                        var _7 = range7.Key;
                        var e = range7.Value;
                        var result = tryGetModuleNameFromExportsOrImports(options, host, targetFileName, packageDirectory, packageName, e, conditions, mode, isImports, preferTsExtension);
                        if ((GoRuntime.Len(result) > 0))
                        {
                            return result;
                        }
                    }
                }

                break;
            }

            case JSONValueType.Object:
            {
                var obj = exports.AsObject();
                {
                    foreach (var range8 in obj.Entries())
                    {
                        var key = range8.Key;
                        var value = range8.Value;
                        if ((((key == "default") || GoSlices.Contains(conditions, key)) || (GoSlices.Contains(conditions, "types") && ModuleFunctions.IsApplicableVersionedTypesKey(key))))
                        {
                            var result2 = tryGetModuleNameFromExportsOrImports(options, host, targetFileName, packageDirectory, packageName, value, conditions, mode, isImports, preferTsExtension);
                            if ((GoRuntime.Len(result2) > 0))
                            {
                                return result2;
                            }
                        }
                    }
                }

                break;
            }

            case JSONValueType.Null:
            {
                return default;
            }
        }

        return default;
    }
    // specifiers.go:1388. Preserve the old import spelling preference for moved files.
    internal static ModuleSpecifier UpdateModuleSpecifier(CompilerOptions compilerOptions, IModuleSpecifierGenerationHost host,
        SourceFile importingSourceFile, RootedFilePath importingSourceFileName, ModuleSpecifier oldImportSpecifier,
        RootedFilePath toFileName, UserPreferences userPreferences, ModuleSpecifierOptions options)
        => getModuleSpecifierWithPreferences(compilerOptions, host, importingSourceFile, importingSourceFileName,
            oldImportSpecifier, toFileName, userPreferences, options);

    // specifiers.go:1411. Node-module candidates precede the original local-path fallback.
    private static ModuleSpecifier getModuleSpecifierWithPreferences(CompilerOptions compilerOptions, IModuleSpecifierGenerationHost host,
        SourceFile importingSourceFile, RootedFilePath importingSourceFileName, ModuleSpecifier oldImportSpecifier,
        RootedFilePath toFileName, UserPreferences userPreferences, ModuleSpecifierOptions options)
    {
        var info = getInfo(importingSourceFileName, host);
        var modulePaths = getAllModulePaths(info, toFileName, host, compilerOptions, userPreferences, options);
        var preferences = getModuleSpecifierPreferences(userPreferences, host, compilerOptions, importingSourceFile, oldImportSpecifier.AsValue());
        var resolutionMode = options.OverrideImportMode;
        if (resolutionMode == ResolutionMode.None) resolutionMode = host.GetDefaultResolutionModeForFile(importingSourceFile);
        foreach (var modulePath in modulePaths)
        {
            var firstDefined = tryGetModuleNameAsNodeModule(modulePath, info, importingSourceFile, host, compilerOptions, userPreferences, false, options.OverrideImportMode);
            if (firstDefined.AsValue().Length > 0) return firstDefined;
        }
        return getLocalModuleSpecifier(toFileName, info, compilerOptions, host, resolutionMode, preferences, false);
    }

}
