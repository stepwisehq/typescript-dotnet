// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/view.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Collections;
using TypeScript.PackageJson;
using TypeScript.Lsp.Protocol;
using CompilerProgram = TypeScript.Compiler.Program;
using ModulePreferences = TypeScript.ModuleSpecifiers.UserPreferences;
using ModuleSpecifierEnding = TypeScript.ModuleSpecifiers.ModuleSpecifierEnding;
using ModuleSpecifierFunctions = TypeScript.ModuleSpecifiers.ModuleSpecifiers;

namespace TypeScript.LanguageServices.AutoImport;
internal enum QueryKind
{
    WordPrefix,
    ExactMatch,
    CaseInsensitiveMatch
}

internal sealed class FixAndExport
{
    internal Fix Fix = null!;
    internal Export Export = null!;
}

internal sealed partial class View
{
    private Registry registry = null!;
    internal SourceFile importingFile = null!;
    private PathKey importingFilePath;
    internal CompilerProgram program = null!;
    private Checker checker = null!;
    private ModulePreferences preferences;
    private IProjectID projectID = null!;
    private Slice<ModuleSpecifierEnding> allowedEndings;
    private HashSet<string> conditions = null!;
    private Tristate shouldUseUriStyleNodeCoreModules;
    private MultiMap<ModuleID, ExistingImport>? existingImports;
    private bool? shouldUseRequireForFixes;
    // internal/ls/autoimport/view.go:35
    internal static View NewView(Registry registry, SourceFile importingFile, IProjectID projectID, CompilerProgram program, Checker checker, ModulePreferences preferences)
    {
        var importingFilePath = PathKey.FromValue(importingFile.PathKeyValue());
        if (importingFile.CanonicalSourceFile()is { } canonical)
            importingFilePath = PathKey.FromValue(canonical.PathKeyValue());
        var conditionItems = TypeScript.Modules.DefaultResolver.GetConditions(program.Options(), program.GetDefaultResolutionModeForFile(importingFile));
        var conditions = new HashSet<string>(conditionItems.Count, StringComparer.Ordinal);
        foreach (var condition in conditionItems)
            conditions.Add(condition);
        return new View
        {
            registry = registry,
            importingFile = importingFile,
            importingFilePath = importingFilePath,
            program = program,
            checker = checker,
            projectID = projectID,
            preferences = preferences,
            conditions = conditions,
            shouldUseUriStyleNodeCoreModules = LsUtil.ShouldUseUriStyleNodeCoreModules(importingFile, program),
        };
    }

    // internal/ls/autoimport/view.go:61. nil alone denotes an uncomputed header.
    private Slice<ModuleSpecifierEnding> GetAllowedEndings()
    {
        if (allowedEndings.IsNil)
            allowedEndings = ModuleSpecifierFunctions.GetAllowedEndingsInPreferredOrderValue(preferences, program, program.Options(), importingFile, default, program.GetDefaultResolutionModeForFile(importingFile));
        return allowedEndings;
    }

    // internal/ls/autoimport/view.go:85. Explicit immutable state replaces this one query's Go closure.
    internal Slice<Export> Search(ByteStringValue query, QueryKind kind) => Search((query, kind), static (state, bucket) => state.kind switch
    {
        QueryKind.WordPrefix => bucket.Index!.SearchWordPrefix(state.query),
        QueryKind.ExactMatch => bucket.Index!.Find(state.query, true),
        QueryKind.CaseInsensitiveMatch => bucket.Index!.Find(state.query, false),
        _ => throw new InvalidOperationException("unreachable"),
    });
    // internal/ls/autoimport/view.go:102
    internal Slice<Export> SearchByExportID(ExportID id) => Search(id, static (target, bucket) => Core.Filter(bucket.Index!.Entries, target, static (export, state) => export.ExportID == state));
    // internal/ls/autoimport/view.go:112
    private Slice<Export> Search<TState>(TState state, Func<TState, RegistryBucket, Slice<Export>> search)
    {
        Slice<Export> results = default;
        if (registry.projects != null && registry.projects.TryGetValue(projectID, out var projectBucket))
        {
            var exports = search(state, projectBucket);
            results = results.Grow(exports.Count);
            foreach (var export in exports)
            {
                var(modulePath, path) = export.ModuleID.AsPathKey();
                if (path && modulePath == PathKey.FromValue(importingFile.PathKeyValue()))
                    continue;
                results = results.Append(export);
            }
        }

        HashSet<ByteStringKey>? allowedPackages = null;
        PathKey.FromValue(importingFile.PathKeyValue()).Parent().ForEachAncestorDirectory<object?>(directoryPath =>
        {
            if (registry.directories.TryGetValue(directoryPath, out var directory))
            {
                var packageJson = directory.packageJson;
                if (packageJson?.Contents is { Parseable: true } contents)
                {
                    allowedPackages ??= new();
                    AutoImport.AddPackageJsonDependencies(contents, allowedPackages);
                }
            }

            return (null, false);
        });
        if (allowedPackages != null && registry.projects != null && registry.projects.TryGetValue(projectID, out var bucket))
        {
            // Original UnionedWith returns an independently-owned set, not an in-place mutation.
            var union = new HashSet<ByteStringKey>(allowedPackages, allowedPackages.Comparer);
            if (bucket.ResolvedPackageNames != null)
                union.UnionWith(bucket.ResolvedPackageNames);
            allowedPackages = union;
        }

        var excludedPackages = new HashSet<ByteStringKey>();
        PathKey.FromValue(importingFile.PathKeyValue()).Parent().ForEachAncestorDirectory<object?>(directoryPath =>
        {
            if (registry.nodeModules != null && registry.nodeModules.TryGetValue(directoryPath, out var bucket))
            {
                var exports = search(state, bucket);
                results = results.Grow(exports.Count);
                foreach (var export in exports)
                {
                    var packageName = ByteStringKey.FromValue(export.PackageName);
                    if (excludedPackages.Contains(packageName))
                        continue;
                    if (allowedPackages != null && !allowedPackages.Contains(packageName))
                        continue;
                    results = results.Append(export);
                }

                if (bucket.PackageFiles != null)
                    foreach (var packageName in bucket.PackageFiles.Keys)
                        excludedPackages.Add(packageName);
            }

            return (null, false);
        });
        return results;
    }

    private readonly record struct ExportGroupKey(ExportID Target, ByteStringKey Name, ByteStringKey AmbientModuleOrPackageName);
    // internal/ls/autoimport/view.go:179
    internal Slice<FixAndExport> GetCompletions(ByteStringValue prefix, Position position, bool forJSX, bool isTypeOnlyLocation)
    {
        var results = Search(prefix, QueryKind.WordPrefix);
        var grouped = new Dictionary<ExportGroupKey, Slice<Export>>(results.Count);
        foreach (var export in results)
        {
            var name = export.Name();
            if (!TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name, LanguageVariant.Standard))
                continue;
            // Original tests the first byte as a rune; do not reinterpret this as the whole UTF8 scalar.
            if (forJSX && !(GoUnicode.IsUpper(name[0]) || export.IsRenameable()))
                continue;
            var target = export.Target != default ? export.Target : export.ExportID;
            var ambient = export.AmbientModuleName();
            if (ambient.Length == 0)
                ambient = export.PackageName;
            if (export.PackageName.EqualsString("@types/node") || export.Path.ContainsLowercaseDirectorySequence("/node_modules/@types/node/"))
            {
                if (Core.ContainsUnprefixedNodeCoreModule(ambient))
                    ambient = ByteStringValue.Concat(ByteStringValue.FromString("node:"), ambient);
            }

            var key = new ExportGroupKey(target, ByteStringKey.FromValue(name), ByteStringKey.FromValue(ambient));
            grouped.TryGetValue(key, out var existing);
            var replaced = false;
            for (var index = 0; index < existing.Count; index++)
            {
                var old = existing[index];
                if (export.ExportID != old.ExportID)
                    continue;
                // Original Replace of one element updates the same slice backing owner.
                existing[index] = new Export
                {
                    ExportID = export.ExportID,
                    ModuleFileName = export.ModuleFileName,
                    UnresolvedModuleSpecifier = export.UnresolvedModuleSpecifier,
                    PackageName = export.PackageName,
                    IsTypeOnly = export.IsTypeOnly || old.IsTypeOnly,
                    Syntax = (long)export.Syntax < (long)old.Syntax ? export.Syntax : old.Syntax,
                    Flags = export.Flags | old.Flags,
                    ScriptElementKind = (int)export.ScriptElementKind < (int)old.ScriptElementKind ? export.ScriptElementKind : old.ScriptElementKind,
                    ScriptElementKindModifiers = export.ScriptElementKindModifiers | old.ScriptElementKindModifiers,
                    localName = export.localName,
                    Target = export.Target,
                    Path = export.Path,
                };
                grouped[key] = existing;
                replaced = true;
                break;
            }

            if (replaced)
                continue;
            grouped[key] = existing.Append(export);
        }

        var fixes = Slice<FixAndExport>.Make(0, results.Count);
        foreach (var exports in grouped.Values)
        {
            var groupFixes = Slice<FixAndExport>.Make(0, exports.Count);
            foreach (var export in exports)
                foreach (var fix in GetFixes(export, forJSX, isTypeOnlyLocation, position))
                    groupFixes = groupFixes.Append(new FixAndExport { Fix = fix, Export = export });
            fixes = fixes.Append(Core.MinAllFunc(groupFixes, this, static (left, right, view) => view.CompareFixesForRanking(left.Fix, right.Fix)));
        }

        // The original client sorts by SortText/Label; this comparer only establishes relative fix ranking.
        GoSlices.SortFunc(fixes, (left, right) => CompareFixesForSorting(left.Fix, right.Fix));
        return fixes;
    }
}
