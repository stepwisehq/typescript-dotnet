// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/modulespecifiers/util.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Source naming and actual cached auto-import exclusion matching.
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
        internal ByteStringValue PackageName;
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

    private readonly record struct RegexPatternCacheKey(ByteStringKey Pattern, bool CaseInsensitive);
    private static readonly System.Threading.ReaderWriterLockSlim regexPatternCacheMu = new();
    private static readonly Dictionary<RegexPatternCacheKey, TypeScript.RegularExpressions.GoRegexp> regexPatternCache = new();
    // Upstream tsc/internal/modulespecifiers/util.go:47.
    internal static bool IsExcludedByRegex(string name, Slice<string> excludes) => IsExcludedByRegex(ByteStringValue.FromString(name), excludes);
    internal static bool IsExcludedByRegex(ByteStringValue name, Slice<string> excludes)
    {
        foreach (var pattern in excludes)
        {
            var regex = stringToRegex(ByteStringValue.FromString(pattern));
            if (regex != null && regex.MatchString(name))
                return true;
        }

        return false;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:60. Scoped RW locks retain
    // double-checked admission; cached pattern ownership matches strings.Clone.
    private static TypeScript.RegularExpressions.GoRegexp stringToRegex(ByteStringValue pattern)
    {
        bool caseInsensitive = false;
        if (pattern.Length > 2 && pattern[0] == '/')
        {
            int lastSlash = pattern.LastIndexOf('/');
            if (lastSlash > 0)
            {
                bool hasUnescapedMiddleSlash = false;
                for (int i = 1; i < lastSlash; i++)
                {
                    if (pattern[i] == '/' && pattern[i - 1] != '\\')
                    {
                        hasUnescapedMiddleSlash = true;
                        break;
                    }
                }

                if (!hasUnescapedMiddleSlash)
                {
                    var flags = pattern.Slice(lastSlash + 1);
                    pattern = pattern.Slice(1, lastSlash);
                    for (int i = 0; i < flags.Length; i++)
                        if (flags[i] == 'i')
                            caseInsensitive = true;
                }
            }
        }

        var key = new RegexPatternCacheKey(ByteStringKey.FromValue(pattern), caseInsensitive);
        regexPatternCacheMu.EnterReadLock();
        try
        {
            if (regexPatternCache.TryGetValue(key, out var found))
                return found;
        }
        finally
        {
            regexPatternCacheMu.ExitReadLock();
        }

        regexPatternCacheMu.EnterWriteLock();
        try
        {
            if (regexPatternCache.TryGetValue(key, out var found))
                return found;
            if (regexPatternCache.Count > 1000)
                regexPatternCache.Clear();
            pattern = pattern.Length == 0 ? ByteStringValue.FromString("") : pattern.IsBytes ? ByteStringValue.FromView(ByteStringView.FromOwnedBytes(pattern.ByteSpan.ToArray())) : ByteStringValue.FromString(new string (pattern.CharSpan));
            key = new RegexPatternCacheKey(ByteStringKey.FromValue(pattern), caseInsensitive);
            var compilePattern = caseInsensitive ? ByteStringValue.Concat(ByteStringValue.FromString("(?i:"), pattern, ByteStringValue.FromString(")")) : pattern;
            var(compiled, error) = TypeScript.RegularExpressions.GoRegexp.Compile(compilePattern);
            regexPatternCache[key] = error == null ? compiled : null;
            return error == null ? compiled : null;
        }
        finally
        {
            regexPatternCacheMu.ExitWriteLock();
        }
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
    public static string GetJSExtensionForDeclarationFileExtension(string ext) => GetJSExtensionValueForDeclarationFileExtension(ByteStringValue.FromString(ext)).Materialize()!;
    private static ByteStringValue GetJSExtensionValueForDeclarationFileExtension(ByteStringValue ext)
    {
        switch (ext)
        {
            case var _ when ext.EqualsString(TsPath.ExtensionDts):
            {
                return ByteStringValue.FromString(TsPath.ExtensionJs);
            }

            case var _ when ext.EqualsString(TsPath.ExtensionDmts):
            {
                return ByteStringValue.FromString(TsPath.ExtensionMjs);
            }

            case var _ when ext.EqualsString(TsPath.ExtensionDcts):
            {
                return ByteStringValue.FromString(TsPath.ExtensionCjs);
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
    public static string TryGetRealFileNameForNonJSDeclarationFileName(string fileName) => TryGetRealFileNameValueForNonJSDeclarationFileName(ByteStringValue.FromString(fileName)).Materialize()!;
    internal static ByteStringValue TryGetRealFileNameValueForNonJSDeclarationFileName(ByteStringValue fileName)
    {
        var baseName = TsPath.GetBaseFileName(fileName);
        if (((!fileName.EndsWith(".ts"u8) || !baseName.Contains(".d."u8)) || baseName.EndsWith(".d.ts"u8)))
        {
            return default;
        }

        var noExtension = TsPath.RemoveExtension(fileName, TsPath.ExtensionTs);
        var lastDotIndex = noExtension.LastIndexOf('.');
        var ext = GoRuntime.Slice(noExtension, lastDotIndex);
        var tuple1 = GoStrings.Cut(noExtension, ByteStringValue.FromString(".d."));
        var before = tuple1.Item1;
        return TsPath.pathConcat(before, ext);
    }

    // Upstream tsc/internal/modulespecifiers/util.go:156:1.
    private static string getJSExtensionForFile(ByteStringValue fileName, CompilerOptions options)
    {
        var result = ModuleFunctions.TryGetJSExtensionForFile(fileName, options);
        if ((GoRuntime.Len(result) == 0))
        {
            throw new InvalidOperationException(($"Extension {extensionFromPath(fileName).Materialize()} is unsupported:: FileName:: {fileName.Materialize()}"));
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
    private static ByteStringValue extensionFromPath(ByteStringValue path)
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
    private static Slice<RelativePath> getPathsRelativeToRootDirs(RootedFilePath path, List<RootedDirectoryPath> rootDirs, CaseSensitivity caseSensitivity)
    {
        Slice<RelativePath> results = default;
        {
            foreach (var rootDir in GoRuntime.RangeValues(rootDirs))
            {
                var relativePath = getRelativePathIfInSameVolume(path, rootDir, caseSensitivity);
                if (!isPathRelativeToParent(relativePath))
                {
                    results = results.Append(relativePath);
                }
            }
        }

        return results;
    }

    // Upstream tsc/internal/modulespecifiers/util.go:214:1.
    internal static bool isPathRelativeToParent(RelativePath path)
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
    private static ByteStringValue resolvePathPatternIfInSameVolume(ByteStringValue path, RootedDirectoryPath directoryPath, CaseSensitivity caseSensitivity)
    {
        var relativePath = TsPath.ResolveRelativePathToDirectoryOrUrl(directoryPath.AsValue(), path, false, directoryPath, caseSensitivity);
        if (TsPath.IsRootedDiskPath(relativePath))
        {
            return default;
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
    private static bool prefersTsExtension(Slice<ModuleSpecifierEnding> allowedEndings)
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
    private static ByteStringValue replaceFirstStar(ByteStringValue s, ByteStringValue replacement)
    {
        return GoStrings.Replace(s, "*", replacement, 1);
    }

    // Upstream tsc/internal/modulespecifiers/util.go:283:1.
    public static NodeModulePathParts GetNodeModulePathParts(RootedFilePath fileName)
    {
        var fullPath = fileName.AsValue();
        var topLevelNodeModulesIndex = -1;
        var packageNameStart = -1;
        var packageRootIndex = -1;
        var hasNestedNodeModules = false;
        var partEnd = 0;
        var state = nodeModulesPathParseState.BeforeNodeModules;
        while ((partEnd >= 0))
        {
            var partStart = partEnd;
            partEnd = Core.IndexAfter(fullPath, ByteStringValue.FromString("/"), (partStart + 1));
            switch (state)
            {
                case nodeModulesPathParseState.BeforeNodeModules:
                {
                    if (fullPath.Slice(partStart).StartsWith("/node_modules/"u8))
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
                    if (fullPath.Slice(partStart).StartsWith("/node_modules/"u8))
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
        var info = getInfo(RootedFilePath.FromValue(importingSourceFile.FileNameGoValue()), host);
        var modulePaths = getAllModulePaths(info, nodeModulesFileName, host, compilerOptions, preferences, options);
        {
            foreach (var modulePath in GoRuntime.RangeValues(modulePaths))
            {
                {
                    var result = tryGetModuleNameAsNodeModule(modulePath, info, importingSourceFile, host, compilerOptions, preferences, true, options.OverrideImportMode);
                    if ((result.AsValue().Length > 0))
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
    public static string GetPackageNameFromDirectory(RootedPath path) => GetPackageNameValueFromDirectory(path).Materialize()!;
    internal static ByteStringValue GetPackageNameValueFromDirectory(RootedPath path)
    {
        var fileOrDirectoryPath = path.AsValue();
        var tuple1 = GoStrings.CutLast(fileOrDirectoryPath, ByteStringValue.FromString("/node_modules/"));
        var basename = tuple1.Item2;
        var found = tuple1.Item3;
        if (!found)
        {
            return default;
        }

        if ((basename[0] == '.'))
        {
            return default;
        }

        var nextSlash = basename.IndexOf('/');
        if ((nextSlash == -1))
        {
            return basename;
        }

        if (((basename[0] != '@') || (nextSlash == (GoRuntime.Len(basename) - 1))))
        {
            return GoRuntime.Slice(basename, 0, nextSlash);
        }

        var secondSlash = basename.Slice(nextSlash + 1).IndexOf('/');
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
        var specifier = entrypoint.ModuleSpecifier.AsValue();
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
        if ((dtsExtension.Length != 0))
        {
            switch (preferredEnding)
            {
                case ModuleSpecifierEnding.TsExtension:
                case ModuleSpecifierEnding.JsExtension:
                {
                    var jsExtension = GetJSExtensionValueForDeclarationFileExtension(dtsExtension);
                    return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension, Slice<ByteStringValue>.FromArray([dtsExtension]), CaseSensitivity.CaseSensitive));
                }

                case ModuleSpecifierEnding.Minimal:
                case ModuleSpecifierEnding.Index:
                {
                    if ((entrypoint.Ending == Ending.Changeable))
                    {
                        if ((dtsExtension.EqualsString(TsPath.ExtensionDts)))
                        {
                            specifier = TsPath.RemoveExtension(specifier, dtsExtension);
                            if ((preferredEnding == ModuleSpecifierEnding.Minimal))
                            {
                                specifier = GoStrings.TrimSuffix(specifier, ByteStringValue.FromString("/index"));
                            }

                            return TsPath.ToModuleSpecifier(specifier);
                        }

                        var jsExtension2 = GetJSExtensionValueForDeclarationFileExtension(dtsExtension);
                        return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension2, Slice<ByteStringValue>.FromArray([dtsExtension]), CaseSensitivity.CaseSensitive));
                    }

                    var jsExtension3 = GetJSExtensionValueForDeclarationFileExtension(dtsExtension);
                    return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension3, Slice<ByteStringValue>.FromArray([dtsExtension]), CaseSensitivity.CaseSensitive));
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
                            return TsPath.ToModuleSpecifier(TsPath.pathConcat(TsPath.RemoveFileExtension(specifier), ByteStringValue.FromString(jsExtension4)));
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
                            specifier = GoStrings.TrimSuffix(specifier, ByteStringValue.FromString("/index"));
                        }

                        return TsPath.ToModuleSpecifier(specifier);
                    }

                    {
                        var jsExtension5 = ModuleFunctions.TryGetJSExtensionForFile(specifier, options);
                        if ((jsExtension5 != ""))
                        {
                            return TsPath.ToModuleSpecifier(TsPath.pathConcat(TsPath.RemoveFileExtension(specifier), ByteStringValue.FromString(jsExtension5)));
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
                            specifier = GoStrings.TrimSuffix(specifier, ByteStringValue.FromString("/index"));
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

    internal static ModuleSpecifier ProcessEntrypointEnding(ResolvedEntrypoint entrypoint, UserPreferences prefs, IModuleSpecifierGenerationHost host, CompilerOptions options, SourceFile importingSourceFile, Slice<ModuleSpecifierEnding> allowedEndings)
    {
        var specifier = entrypoint.ModuleSpecifier.AsValue();
        if ((entrypoint.Ending == Ending.Fixed))
        {
            return entrypoint.ModuleSpecifier;
        }

        if ((GoRuntime.Len(allowedEndings) == 0))
        {
            allowedEndings = GetAllowedEndingsInPreferredOrderValue(prefs, host, options, importingSourceFile, default, host.GetDefaultResolutionModeForFile(importingSourceFile));
        }

        var preferredEnding = allowedEndings[0];
        var dtsExtension = TsPath.GetDeclarationFileExtension(specifier);
        if ((dtsExtension.Length != 0))
        {
            switch (preferredEnding)
            {
                case ModuleSpecifierEnding.TsExtension:
                case ModuleSpecifierEnding.JsExtension:
                {
                    var jsExtension = GetJSExtensionValueForDeclarationFileExtension(dtsExtension);
                    return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension, Slice<ByteStringValue>.FromArray([dtsExtension]), CaseSensitivity.CaseSensitive));
                }

                case ModuleSpecifierEnding.Minimal:
                case ModuleSpecifierEnding.Index:
                {
                    if ((entrypoint.Ending == Ending.Changeable))
                    {
                        if ((dtsExtension.EqualsString(TsPath.ExtensionDts)))
                        {
                            specifier = TsPath.RemoveExtension(specifier, dtsExtension);
                            if ((preferredEnding == ModuleSpecifierEnding.Minimal))
                            {
                                specifier = GoStrings.TrimSuffix(specifier, ByteStringValue.FromString("/index"));
                            }

                            return TsPath.ToModuleSpecifier(specifier);
                        }

                        var jsExtension2 = GetJSExtensionValueForDeclarationFileExtension(dtsExtension);
                        return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension2, Slice<ByteStringValue>.FromArray([dtsExtension]), CaseSensitivity.CaseSensitive));
                    }

                    var jsExtension3 = GetJSExtensionValueForDeclarationFileExtension(dtsExtension);
                    return TsPath.ToModuleSpecifier(TsPath.ChangeAnyExtension(specifier, jsExtension3, Slice<ByteStringValue>.FromArray([dtsExtension]), CaseSensitivity.CaseSensitive));
                }
            }

            return TsPath.ToModuleSpecifier(specifier);
        }

        if (TsPath.FileExtensionIsOneOf(specifier, TsPath.SupportedTSImplementationExtensions))
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
                            return TsPath.ToModuleSpecifier(TsPath.pathConcat(TsPath.RemoveFileExtension(specifier), ByteStringValue.FromString(jsExtension4)));
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
                            specifier = GoStrings.TrimSuffix(specifier, ByteStringValue.FromString("/index"));
                        }

                        return TsPath.ToModuleSpecifier(specifier);
                    }

                    {
                        var jsExtension5 = ModuleFunctions.TryGetJSExtensionForFile(specifier, options);
                        if ((jsExtension5 != ""))
                        {
                            return TsPath.ToModuleSpecifier(TsPath.pathConcat(TsPath.RemoveFileExtension(specifier), ByteStringValue.FromString(jsExtension5)));
                        }
                    }

                    return TsPath.ToModuleSpecifier(specifier);
                }
            }

            return TsPath.ToModuleSpecifier(specifier);
        }

        if (TsPath.FileExtensionIsOneOf(specifier, TsPath.SupportedJSExtensionsFlat))
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
                            specifier = GoStrings.TrimSuffix(specifier, ByteStringValue.FromString("/index"));
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
