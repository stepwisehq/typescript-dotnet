// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned language-service counterpart; body assembly awaiting centralized compilation.
using AST = TypeScript.Scanner.Ast;
using AstNav = TypeScript.AstNavigation.AstNav;
using Kind = TypeScript.Scanner.Kind;
using ModuleSpecifierApi = TypeScript.ModuleSpecifiers.ModuleSpecifiers;
using NodeFactory = TypeScript.Scanner.NodeFactory;
using TsOptionFunctions = TypeScript.TsOptions.TsOptions;
using Protocol = TypeScript.Lsp.Protocol;
using ScannerApi = TypeScript.Scanner.Scanner;
using TypeScript.Checking;
using TypeScript.Collections;
using TypeScript.Compiler;
using TypeScript.DiagnosticPrinting;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.ModuleSpecifiers;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.SpanMap;

namespace TypeScript.LanguageServices;
// Source assembly component: StringCompletionPatterns.cs
internal sealed partial class LanguageService
{
    private bool addCompletionEntriesFromRelativePathPatterns(ModuleCompletionNameAndKindSet result, Program program, RelativePath fragment, RootedDirectoryPath directory, ExtensionOptions options, OrderedMap<string, List<string>?> paths) => addCompletionEntriesFromPaths(result, program, fragment.AsValue(), directory, options, paths);
    private bool addCompletionEntriesFromPaths(ModuleCompletionNameAndKindSet result, Program program, ByteStringValue fragment, RootedDirectoryPath directory, ExtensionOptions options, OrderedMap<string, List<string>?> paths)
    {
        Slice<ByteStringValue> Patterns(string key) => Core.MapSlice(paths.GetOrZero(key), ByteStringValue.FromString);
        int Compare(ByteStringValue a, ByteStringValue b)
        {
            var left = Core.TryParsePattern(a);
            var right = Core.TryParsePattern(b);
            int leftLength = left.StarIndex == -1 ? a.Length : left.StarIndex;
            int rightLength = right.StarIndex == -1 ? b.Length : right.StarIndex;
            return rightLength.CompareTo(leftLength);
        }

        return addCompletionEntriesFromPathsOrExportsOrImports(result, program, false, false, fragment, directory, options, paths.Keys(), Patterns, Compare);
    }

    private readonly record struct PathCompletionResult(Slice<ModuleCompletionNameAndKind> Results, bool Matched);
    private bool addCompletionEntriesFromPathsOrExportsOrImports(ModuleCompletionNameAndKindSet result, Program program, bool exports, bool imports, ByteStringValue fragment, RootedDirectoryPath directory, ExtensionOptions options, IEnumerable<string> keys, Func<string, Slice<ByteStringValue>> patternsForKey, Func<ByteStringValue, ByteStringValue, int> compare)
    {
        Slice<PathCompletionResult> pathResults = default;
        ByteStringValue? matchedPath = null;
        foreach (var key in keys)
        {
            if (key == ".")
                continue;
            var normalized = ByteStringValue.FromString(key);
            if (normalized.StartsWith(ByteStringValue.FromString("./")))
                normalized = normalized.Slice(2);
            if ((exports || imports) && ByteStringValue.FromString(key).EndsWith(ByteStringValue.FromString("/")))
                normalized = completionConcat(normalized, ByteStringValue.FromString("*"));
            var patterns = patternsForKey(key);
            if (patterns.Length == 0)
                continue;
            var pattern = Core.TryParsePattern(normalized);
            if (!pattern.IsValid())
                continue;
            bool match = pattern.Matches(fragment);
            bool longest = match && (matchedPath == null || compare(normalized, matchedPath.Value) < 0);
            if (longest)
            {
                matchedPath = normalized;
                pathResults = Core.Filter(pathResults, item => !item.Matched);
            }

            if (pattern.StarIndex == -1 || matchedPath == null || compare(normalized, matchedPath.Value) <= 0)
                pathResults = pathResults.Append(new PathCompletionResult(getCompletionsForPathMapping(normalized, patterns, fragment, directory, exports, imports, options, program), match));
        }

        foreach (var path in pathResults)
            foreach (var entry in path.Results)
                result.add(entry);
        return matchedPath != null;
    }
}

// Source assembly component: StringCompletionContexts.cs
internal sealed partial class LanguageService
{
    private StringLiteralCompletions? getStringLiteralCompletionEntries(ProtocolRequestContext context, SourceFile file, Node node, int position, Checker checker)
    {
        var parent = walkUpParentheses(node.Parent!);
        switch (parent.Kind)
        {
            case Kind.LiteralType:
                var grandparent = walkUpParentheses(parent.Parent!);
                return grandparent.Kind == Kind.ImportType ? getStringLiteralCompletionsFromModuleNames(file, node, GetProgram(), checker) : fromUnionableLiteralType(grandparent, parent, position, checker);
            case Kind.PropertyAssignment:
                if (AST.IsObjectLiteralExpression(parent.Parent) && parent.Name() == node)
                    return new StringLiteralCompletions
                    {
                        fromProperties = stringLiteralCompletionsForObjectLiteral(checker, parent.Parent!)
                    };
                if (AST.FindAncestor(parent.Parent, AST.IsCallLikeExpression) != null)
                {
                    HashSet<ByteStringKey>? unique = null;
                    var types = getStringLiteralTypes(checker.GetContextualType(node, ContextFlags.None), ref unique, checker);
                    types = types.Append(getStringLiteralTypes(checker.GetContextualType(node, ContextFlags.IgnoreNodeInferences), ref unique, checker));
                    return toStringLiteralCompletionsFromTypes(types);
                }

                return new StringLiteralCompletions
                {
                    fromTypes = fromContextualType(ContextFlags.None, node, checker)
                };
            case Kind.ElementAccessExpression:
                if (node == AST.SkipParentheses(parent.AsElementAccessExpression().ArgumentExpression))
                    return new StringLiteralCompletions
                    {
                        fromProperties = stringLiteralCompletionsFromProperties(checker.GetTypeAtLocation(parent.Expression()), checker)
                    };
                return null;
            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.JsxAttribute:
                if (!isRequireCallArgument(node) && !AST.IsImportCall(parent))
                {
                    var argument = parent.Kind == Kind.JsxAttribute ? parent.Parent! : node;
                    var info = getArgumentInfoForCompletions(argument, position, file, checker);
                    if (info == null)
                        return null;
                    var result = getStringLiteralCompletionsFromSignature(info.invocation, node, info, checker);
                    return new StringLiteralCompletions
                    {
                        fromTypes = result ?? fromContextualType(ContextFlags.None, node, checker)
                    };
                }

                goto case Kind.ImportDeclaration;
            case Kind.ImportDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ExternalModuleReference:
            case Kind.JSDocImportTag:
                return getStringLiteralCompletionsFromModuleNames(file, node, GetProgram(), checker);
            case Kind.CaseClause:
                var tracker = newCaseClauseTracker(checker, parent.Parent!.AsCaseBlock().Clauses.Nodes);
                var contextual = fromContextualType(ContextFlags.IgnoreNodeInferences, node, checker);
                if (contextual == null)
                    return null;
                return new StringLiteralCompletions
                {
                    fromTypes = new CompletionsFromTypes
                    {
                        types = Core.Filter(contextual.types, type => !tracker.hasValue(type.value)),
                        isNewIdentifier = false
                    }
                };
            case Kind.ImportSpecifier:
            case Kind.ExportSpecifier:
                if (parent.PropertyName()is { } property && node != property)
                    return null;
                var named = parent.Parent!;
                var module = named.Kind == Kind.NamedImports ? named.Parent!.Parent : named.Parent;
                if (module == null)
                    return null;
                var symbol = checker.GetSymbolAtLocation(module);
                if (symbol == null)
                    return null;
                var exports = checker.GetExportsAndPropertiesOfModule(symbol);
                HashSet<ByteStringKey>? existing = null;
                foreach (var item in named.Elements())
                    (existing ??= new()).Add(ByteStringKey.FromValue(item.PropertyNameOrName()!.TextValue()));
                return new StringLiteralCompletions
                {
                    fromProperties = new CompletionsFromProperties
                    {
                        symbols = Core.Filter(exports, item => !item.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameDefault)) && !(existing?.Contains(ByteStringKey.FromValue(item.NameValue)) ?? false)),
                        hasIndexSignature = false
                    }
                };
            case Kind.BinaryExpression:
                if (parent.AsBinaryExpression().OperatorToken.Kind == Kind.InKeyword)
                {
                    var properties = getPropertiesForCompletion(checker.GetTypeAtLocation(parent.AsBinaryExpression().Right), checker);
                    return new StringLiteralCompletions
                    {
                        fromProperties = new CompletionsFromProperties
                        {
                            symbols = Core.Filter(properties, symbol => symbol.ValueDeclaration == null || !AST.IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration)),
                            hasIndexSignature = false
                        }
                    };
                }

                return new StringLiteralCompletions
                {
                    fromTypes = fromContextualType(ContextFlags.None, node, checker)
                };
            default:
                return new StringLiteralCompletions
                {
                    fromTypes = fromContextualType(ContextFlags.IgnoreNodeInferences, node, checker) ?? fromContextualType(ContextFlags.None, node, checker)
                };
        }
    }

    private static CompletionsFromTypes? fromContextualType(ContextFlags flags, Node node, Checker checker) => toCompletionsFromTypes(getStringLiteralTypes(getContextualTypeFromParent(node, checker, flags), checker));
    private static CompletionsFromTypes? toCompletionsFromTypes(Slice<LiteralType> types) => types.Length == 0 ? null : new CompletionsFromTypes
    {
        types = types,
        isNewIdentifier = false
    };
    private static StringLiteralCompletions? toStringLiteralCompletionsFromTypes(Slice<LiteralType> types)
    {
        var result = toCompletionsFromTypes(types);
        return result == null ? null : new StringLiteralCompletions
        {
            fromTypes = result
        };
    }
}

// Source assembly component: StringCompletionTypes.cs
internal sealed partial class LanguageService
{
    private static StringLiteralCompletions? fromUnionableLiteralType(Node grandparent, Node parent, int position, Checker checker)
    {
        switch (grandparent.Kind)
        {
            case Kind.CallExpression:
            case Kind.ExpressionWithTypeArguments:
            case Kind.JsxOpeningElement:
            case Kind.JsxSelfClosingElement:
            case Kind.NewExpression:
            case Kind.TaggedTemplateExpression:
            case Kind.TypeReference:
                var argument = AST.FindAncestor(parent, node => node.Parent == grandparent);
                return argument == null ? null : new StringLiteralCompletions
                {
                    fromTypes = new CompletionsFromTypes
                    {
                        types = getStringLiteralTypes(checker.GetTypeArgumentConstraint(argument), checker),
                        isNewIdentifier = false
                    }
                };
            case Kind.IndexedAccessType:
                if (!grandparent.AsIndexedAccessTypeNode().IndexType.Loc.ContainsInclusive(position))
                    return null;
                return new StringLiteralCompletions
                {
                    fromProperties = stringLiteralCompletionsFromProperties(checker.GetTypeFromTypeNode(grandparent.AsIndexedAccessTypeNode().ObjectType), checker)
                };
            case Kind.UnionType:
                var result = fromUnionableLiteralType(walkUpParentheses(grandparent.Parent!), parent, position, checker);
                if (result == null)
                    return null;
                var used = getAlreadyUsedTypesInStringLiteralUnion(grandparent, parent);
                if (result.fromProperties is { } properties)
                    return new StringLiteralCompletions
                    {
                        fromProperties = new CompletionsFromProperties
                        {
                            symbols = Core.Filter(properties.symbols, symbol => !Core.Some(used, value => value.EqualsValue(symbol.NameValue))),
                            hasIndexSignature = properties.hasIndexSignature
                        }
                    };
                if (result.fromTypes is { } types)
                    return new StringLiteralCompletions
                    {
                        fromTypes = new CompletionsFromTypes
                        {
                            types = Core.Filter(types.types, type => !Core.Some(used, value => value.EqualsValue(type.StringValue()))),
                            isNewIdentifier = false
                        }
                    };
                return null;
            case Kind.PropertySignature:
                return new StringLiteralCompletions
                {
                    fromTypes = new CompletionsFromTypes
                    {
                        types = getStringLiteralTypes(getConstraintOfTypeArgumentProperty(grandparent, checker), checker),
                        isNewIdentifier = false
                    }
                };
            default:
                return null;
        }
    }

    private static CompletionsFromProperties? stringLiteralCompletionsForObjectLiteral(Checker checker, Node expression)
    {
        var contextual = checker.GetContextualType(expression, ContextFlags.None);
        if (contextual == null)
            return null;
        var completions = checker.GetContextualType(expression, ContextFlags.IgnoreNodeInferences);
        return new CompletionsFromProperties
        {
            symbols = getPropertiesForObjectExpression(contextual, completions, expression, checker),
            hasIndexSignature = hasIndexSignature(contextual, checker)
        };
    }

    private static CompletionsFromProperties stringLiteralCompletionsFromProperties(TypeScript.Checking.Type type, Checker checker) => new()
    {
        symbols = Core.Filter(checker.GetApparentProperties(type), symbol => !(symbol.ValueDeclaration != null && AST.IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration))),
        hasIndexSignature = hasIndexSignature(type, checker),
    };
}

// Source assembly component: StringCompletionModuleModels.cs
internal sealed partial class LanguageService
{
    private enum ModuleCompletionKind
    {
        Directory,
        File,
        ExternalModuleName
    }

    private readonly record struct ModuleCompletionNameAndKind
    {
        internal ByteStringValue name { get; init; }
        internal ModuleCompletionKind kind { get; init; }
        internal ByteStringValue extension { get; init; }
    }

    private sealed class ModuleCompletionNameAndKindSet
    {
        internal required Dictionary<ByteStringKey, ModuleCompletionNameAndKind> names;
        internal void add(ModuleCompletionNameAndKind entry)
        {
            var key = ByteStringKey.FromValue(entry.name);
            if (!names.TryGetValue(key, out var existing) || existing.kind < entry.kind)
                names[key] = entry;
        }
    }

    private sealed class ExtensionOptions
    {
        internal Slice<ByteStringValue> extensionsToSearch;
        internal ReferenceKind referenceKind;
        internal SourceFile importingSourceFile = null!;
        internal ImportModuleSpecifierEndingPreference endingPreference;
        internal ResolutionMode resolutionMode;
    }

    private enum ReferenceKind
    {
        FileName,
        ModuleSpecifier
    }
}

// Source assembly component: StringCompletionNonRelative.cs
internal sealed partial class LanguageService
{
    private Slice<ModuleCompletionNameAndKind> getCompletionEntriesForNonRelativeModules(ByteStringValue fragment, RootedDirectoryPath directory, ResolutionMode mode, Program program, Checker checker, ExtensionOptions extensionOptions)
    {
        var options = program.Options();
        var paths = options.Paths;
        var result = new ModuleCompletionNameAndKindSet
        {
            names = new()
        };
        var resolution = options.GetModuleResolutionKind();
        if (paths is not null && paths.Size() > 0)
            addCompletionEntriesFromPaths(result, program, fragment, options.GetPathsBasePath(program.BaseDirectory()), extensionOptions, paths);
        var fragmentDirectory = getFragmentDirectory(fragment);
        foreach (var ambient in getAmbientModuleCompletions(fragment, fragmentDirectory, checker))
            result.add(new ModuleCompletionNameAndKind { name = ambient, kind = ModuleCompletionKind.ExternalModuleName });
        getCompletionEntriesFromTypings(program, directory, fragmentDirectory, extensionOptions, result);
        if (moduleResolutionUsesNodeModules(resolution))
        {
            bool foundGlobal = false;
            if (fragmentDirectory.Length == 0)
                foreach (var module in enumerateNodeModulesVisibleToScript(directory))
                {
                    var entry = new ModuleCompletionNameAndKind
                    {
                        name = module,
                        kind = ModuleCompletionKind.ExternalModuleName
                    };
                    if (!result.names.ContainsKey(ByteStringKey.FromValue(entry.name)))
                    {
                        foundGlobal = true;
                        result.add(entry);
                    }
                }

            if (!foundGlobal)
            {
                bool exports = options.GetResolvePackageJsonExports(), imports = options.GetResolvePackageJsonImports(), seenScope = false;
                var conditions = DefaultResolver.GetConditions(options, mode);
                bool Lookup(ExportsOrImports? table, ByteStringValue text, RootedDirectoryPath baseDirectory, bool isExports, bool isImports)
                {
                    if (table == null || table.Value.Type != JSONValueType.Object)
                        return table != null && table.Value.Type != JSONValueType.NotPresent;
                    var objectTable = table.Value.AsObject();
                    addCompletionEntriesFromPathsOrExportsOrImports(result, program, isExports, isImports, text, baseDirectory, extensionOptions, objectTable.Keys(), key =>
                    {
                        var(value, ok) = objectTable.Get(key);
                        if (!ok)
                            return default;
                        var pattern = getPatternFromFirstMatchingCondition(value, conditions);
                        if (pattern.Length == 0)
                            return default;
                        if (ByteStringValue.FromString(key).EndsWith(ByteStringValue.FromString("/")) && pattern.EndsWith(ByteStringValue.FromString("/")))
                            pattern = completionConcat(pattern, ByteStringValue.FromString("*"));
                        return Slice<ByteStringValue>.FromArray(new[] { pattern });
                    }, static (a, b) => ModuleUtilities.ComparePatternKeys(a.Materialize()!, b.Materialize()!));
                    return true;
                }

                void ImportsLookup(RootedDirectoryPath ancestor)
                {
                    if (!imports || seenScope)
                        return;
                    var info = program.GetPackageJsonInfo(ancestor.ResolveFile("package.json"));
                    if (info != null && info.Exists())
                    {
                        seenScope = true;
                        Lookup(info.Contents!.Imports, fragment, ancestor, false, true);
                    }
                }

                Func<RootedDirectoryPath, (object? Result, bool Stop)> ancestorLookup = ancestor =>
                {
                    var modules = ancestor.ResolveDirectory("node_modules");
                    if (host.DirectoryExists(modules))
                        getCompletionEntriesForDirectoryFragment(fragment, modules, extensionOptions, program, false, default, result);
                    ImportsLookup(ancestor);
                    return (null, false);
                };
                if (fragmentDirectory.Length != 0 && exports)
                {
                    var directoryOrImports = ancestorLookup;
                    ancestorLookup = ancestor =>
                    {
                        var components = TsPath.GetPathComponents(fragment).Slice2(1);
                        if (components.Length == 0)
                            return directoryOrImports(ancestor);
                        var package = components[0];
                        components = components.Slice2(1);
                        if (package.StartsWith(ByteStringValue.FromString("@")))
                        {
                            if (components.Length == 0)
                                return directoryOrImports(ancestor);
                            package = TsPath.CombinePaths(package, components[0]);
                            components = components.Slice2(1);
                        }

                        if (imports && package.StartsWith(ByteStringValue.FromString("#")))
                        {
                            ImportsLookup(ancestor);
                            return (null, false);
                        }

                        var packageDirectory = ancestor.ResolveDirectory(TsPath.CombinePaths(ByteStringValue.FromString("node_modules"), package));
                        var info = program.GetPackageJsonInfo(packageDirectory.ResolveFile("package.json"));
                        if (info != null && info.Exists())
                        {
                            var subpath = joinCompletionText(components, ByteStringValue.FromString("/"));
                            if (components.Length > 0 && TsPath.HasTrailingDirectorySeparator(fragment))
                                subpath = completionConcat(subpath, ByteStringValue.FromString("/"));
                            if (Lookup(info.Contents!.Exports, subpath, packageDirectory, true, false))
                                return (null, false);
                        }

                        return directoryOrImports(ancestor);
                    };
                }

                TsPath.ForEachAncestorDirectoryPathStoppingAtGlobalCache<object?>(program.GetGlobalTypingsCacheLocation(), directory, ancestorLookup);
            }
        }

        return Slice<ModuleCompletionNameAndKind>.FromArray(result.names.Values.ToArray());
    }

    private static ByteStringValue getFragmentDirectory(ByteStringValue fragment)
    {
        if (!containsSlash(fragment))
            return default;
        return TsPath.HasTrailingDirectorySeparator(fragment) ? fragment : TsPath.GetDirectoryPath(fragment);
    }

    private static ByteStringValue getPatternFromFirstMatchingCondition(ExportsOrImports target, Slice<string> conditions)
    {
        if (target.Type == JSONValueType.String)
            return ByteStringValue.FromString((string)target.Value!);
        if (target.Type == JSONValueType.Object)
            foreach (var condition in target.AsObject().Keys())
                if (condition == "default" || Core.Some(conditions, value => value == condition) || Core.Some(conditions, value => value == "types") && ModuleUtilities.IsApplicableVersionedTypesKey(condition))
                {
                    var(pattern, ok) = target.AsObject().Get(condition);
                    if (ok)
                        return getPatternFromFirstMatchingCondition(pattern, conditions);
                }

        return default;
    }
}

// Source assembly component: StringCompletionTypings.cs
internal sealed partial class LanguageService
{
    private static Slice<ByteStringValue> getAmbientModuleCompletions(ByteStringValue fragment, ByteStringValue fragmentDirectory, Checker checker)
    {
        Slice<ByteStringValue> names = default;
        foreach (var symbol in checker.GetAmbientModules())
        {
            var name = getAmbientModuleName(symbol);
            if (name.StartsWith(fragment) && !name.Contains((int)'*'))
                names = names.Append(name);
        }

        if (fragmentDirectory.Length != 0)
        {
            var separator = TsPath.EnsureTrailingDirectorySeparator(fragmentDirectory);
            var span = names.AsSpan();
            for (int index = 0; index < span.Length; index++)
                if (span[index].StartsWith(separator))
                    span[index] = span[index].Slice(separator.Length);
        }

        return names;
    }

    private static ByteStringValue getAmbientModuleName(Symbol symbol)
    {
        var declaration = AST.GetNonAugmentationDeclaration(symbol);
        return declaration != null && AST.IsModuleWithStringLiteralName(declaration) ? declaration.Name()!.TextValue() : StringUtil.StripQuotesValue(symbol.NameValue);
    }

    private void getCompletionEntriesFromTypings(Program program, RootedDirectoryPath directory, ByteStringValue fragmentDirectory, ExtensionOptions extensionOptions, ModuleCompletionNameAndKindSet result)
    {
        var options = program.Options();
        var seen = new Dictionary<ByteStringKey, bool>();
        var(roots, _) = options.GetEffectiveTypeRoots(program.BaseDirectory());
        foreach (var root in roots ?? [])
            getCompletionEntriesFromTypingsDirectories(root, options, fragmentDirectory, extensionOptions, program, seen, result);
        TsPath.ForEachAncestorDirectoryPathStoppingAtGlobalCache<object?>(program.GetGlobalTypingsCacheLocation(), directory, ancestor =>
        {
            getCompletionEntriesFromTypingsDirectories(ancestor.ResolveDirectory("node_modules/@types"), options, fragmentDirectory, extensionOptions, program, seen, result);
            return (null, false);
        });
    }

    private void getCompletionEntriesFromTypingsDirectories(RootedDirectoryPath directory, CompilerOptions options, ByteStringValue fragmentDirectory, ExtensionOptions extensionOptions, Program program, Dictionary<ByteStringKey, bool> seen, ModuleCompletionNameAndKindSet result)
    {
        if (!host.DirectoryExists(directory))
            return;
        foreach (var typeDirectory in GetDirectories(directory))
        {
            var package = ModuleUtilities.UnmangleScopedPackageNameValue(typeDirectory);
            if ((options.Types?.Count ?? 0) > 0 && !Core.Some(options.Types, value => ByteStringValue.FromString(value).EqualsValue(package)))
                continue;
            if (fragmentDirectory.Length == 0)
            {
                var key = ByteStringKey.FromValue(package);
                if (!seen.TryGetValue(key, out bool present) || !present)
                {
                    result.add(new ModuleCompletionNameAndKind { name = package, kind = ModuleCompletionKind.ExternalModuleName });
                    seen[key] = true;
                }
            }
            else
            {
                var remaining = tryRemoveDirectoryPrefix(fragmentDirectory, package, program.CaseSensitivity());
                if (remaining != null)
                    getCompletionEntriesForDirectoryFragment(remaining.Value, directory.ResolveDirectory(typeDirectory), extensionOptions, program, false, default, result);
            }
        }
    }

    internal static ByteStringValue? tryRemoveDirectoryPrefix(ByteStringValue path, ByteStringValue prefix, CaseSensitivity sensitivity)
    {
        var(remaining, ok) = sensitivity.TrimPrefix(path, prefix);
        if (!ok)
            return null;
        if (remaining.Length > 0 && remaining[0] is '/' or '\\')
            remaining = remaining.Slice(1);
        return remaining;
    }

    private Slice<ByteStringValue> enumerateNodeModulesVisibleToScript(RootedDirectoryPath directory)
    {
        Slice<ByteStringValue> result = default;
        TsPath.ForEachAncestorDirectoryPathStoppingAtGlobalCache<object?>(program.GetGlobalTypingsCacheLocation(), directory, ancestor =>
        {
            var info = program.GetPackageJsonInfo(ancestor.ResolveFile("package.json"));
            if (info != null && info.Exists() && info.Contents != null)
                info.Contents.Fields.RangeDependencies((name, version, field) =>
                {
                    var value = ByteStringValue.FromString(name);
                    if (!value.StartsWith(ByteStringValue.FromString("@types/")))
                        result = result.Append(value);
                    return true;
                });
            return (null, false);
        });
        return result;
    }

    private ExtensionOptions getExtensionOptions(CompilerOptions options, ReferenceKind referenceKind, SourceFile file, ResolutionMode mode, Checker? checker) => new()
    {
        extensionsToSearch = getSupportedExtensionsForModuleResolution(options, GetProgram().CommandLine().ContentMapperExtensions(), checker),
        referenceKind = referenceKind,
        importingSourceFile = file,
        endingPreference = UserPreferences().ImportModuleSpecifierEnding,
        resolutionMode = mode,
    };
    private static Slice<ByteStringValue> getSupportedExtensionsForModuleResolution(CompilerOptions options, List<string>? extraExtensions, Checker? checker)
    {
        Slice<ByteStringValue> extensions = default;
        if (checker != null)
            foreach (var symbol in checker.GetAmbientModules())
            {
                var name = getAmbientModuleName(symbol);
                if (name.StartsWith(ByteStringValue.FromString("*.")) && !name.Contains((int)'/'))
                    extensions = extensions.Append(name.Slice(1));
            }

        foreach (var group in TsOptionFunctions.GetSupportedExtensions(options, extraExtensions))
            foreach (var extension in group)
                extensions = extensions.Append(ByteStringValue.FromString(extension));
        if (moduleResolutionUsesNodeModules(options.GetModuleResolutionKind()))
        {
            // Original Flatten owns its output even when JSON is disabled.
            Slice<ByteStringValue> flattened = default;
            flattened = flattened.Append(extensions);
            if (options.GetResolveJsonModule())
                flattened = flattened.Append(ByteStringValue.FromString(TsPath.ExtensionJson));
            return flattened;
        }

        return extensions;
    }

    private static bool moduleResolutionUsesNodeModules(ModuleResolutionKind resolution) => resolution >= ModuleResolutionKind.Node16 && resolution <= ModuleResolutionKind.NodeNext || resolution == ModuleResolutionKind.Bundler;
    private static bool isPathRelativeToScript(ByteStringValue path) => path.StartsWith(ByteStringValue.FromString("./")) || path.StartsWith(ByteStringValue.FromString("../"));
}

// Source assembly component: StringCompletionDirectory.cs
internal sealed partial class LanguageService
{
    private ModuleCompletionNameAndKindSet getCompletionEntriesForDirectoryFragment(ByteStringValue fragment, RootedDirectoryPath directory, ExtensionOptions options, Program program, bool relative, PathKey exclude, ModuleCompletionNameAndKindSet result)
    {
        fragment = TsPath.NormalizeSlashes(fragment);
        if (!TsPath.HasTrailingDirectorySeparator(fragment))
            fragment = TsPath.GetDirectoryPath(fragment);
        if (fragment.Length == 0)
            fragment = ByteStringValue.FromString(".");
        fragment = TsPath.EnsureTrailingDirectorySeparator(fragment);
        var baseDirectory = directory.ResolveDirectory(fragment);
        if (!relative)
        {
            var packageDirectory = program.GetNearestAncestorDirectoryWithPackageJson(baseDirectory);
            if (packageDirectory.AsValue().Length != 0)
            {
                var info = program.GetPackageJsonInfo(packageDirectory.ResolveFile("package.json"));
                if (info?.Contents != null && info.Contents.Fields.TypesVersions.Type == JSONValueType.Object)
                {
                    var versionPaths = info.Contents.GetVersionPaths(null);
                    var paths = versionPaths.GetPaths();
                    if (paths is not null && paths.Size() > 0)
                    {
                        var(path, ok) = baseDirectory.AsPath().RelativeTo(packageDirectory);
                        if (!ok)
                            throw new InvalidOperationException("package json directory must be an ancestor of the completion directory");
                        if (path.AsValue().Length != 0)
                            path = RelativePath.FromValue(TsPath.EnsureTrailingDirectorySeparator(path.AsValue()));
                        if (addCompletionEntriesFromRelativePathPatterns(result, program, path, packageDirectory, options, paths))
                            return result;
                    }
                }
            }
        }

        if (!host.DirectoryExists(baseDirectory))
            return result;
        var files = ReadDirectory(baseDirectory, options.extensionsToSearch, Slice<ByteStringValue>.FromArray(new[] { ByteStringValue.FromString("./*") }));
        foreach (var file in files)
        {
            if (exclude == program.PathKeyForFileName(file))
                continue;
            var(name, extension) = getFilenameWithExtensionOption(ByteStringValue.FromString(file.BaseName()), program, options, false);
            result.add(new ModuleCompletionNameAndKind { name = name, kind = ModuleCompletionKind.File, extension = extension });
        }

        foreach (var child in GetDirectories(baseDirectory))
        {
            var name = TsPath.GetBaseFileName(child);
            if (!name.EqualsValue(ByteStringValue.FromString("@types")))
                result.add(new ModuleCompletionNameAndKind { name = name, kind = ModuleCompletionKind.Directory });
        }

        return result;
    }
}

// Source assembly component: StringCompletionPaths.cs
internal sealed partial class LanguageService
{
    private StringLiteralCompletions? getStringLiteralCompletionsFromModuleNames(SourceFile file, Node node, Program program, Checker checker)
    {
        int start = AstNav.GetStartOfNode(node, file, false) + 1;
        var(span, ok) = pathCompletionReplacementSpan(file, getDirectoryFragmentRange(node.TextValue(), start));
        if (!ok)
            return null;
        var entries = getStringLiteralCompletionsFromModuleNamesWorker(file, node, program, checker);
        return new StringLiteralCompletions
        {
            fromPaths = new PathCompletions
            {
                entries = toPathCompletions(entries),
                replacementSpan = span
            }
        };
    }

    private static Slice<PathCompletion> toPathCompletions(Slice<ModuleCompletionNameAndKind> names) => Core.MapSlice(names, entry => new PathCompletion { name = entry.name, kind = moduletToScriptElementKind(entry.kind), extension = entry.extension });
    private (Protocol.Range? Range, bool Ok) pathCompletionReplacementSpan(SourceFile file, TextRange? range)
    {
        if (range == null)
            return (null, true);
        var(result, fidelity) = createLspRangeFromBounds(range.Value.Pos(), range.Value.End(), file);
        return fidelity.IsExact() ? (result, true) : (null, false);
    }

    private static ScriptElementKind moduletToScriptElementKind(ModuleCompletionKind kind) => kind switch
    {
        ModuleCompletionKind.Directory => ScriptElementKind.Directory,
        ModuleCompletionKind.File => ScriptElementKind.ScriptElement,
        ModuleCompletionKind.ExternalModuleName => ScriptElementKind.ExternalModuleName,
        _ => throw new InvalidOperationException("Unknown moduleCompletionKind: " + ((int)kind).ToString(System.Globalization.CultureInfo.InvariantCulture)),
    };
    private static bool isAnyDirectorySeparator(int rune) => rune is '/' or '\\';
    private static TextRange? getDirectoryFragmentRange(ByteStringValue text, int start)
    {
        int offset = 0;
        for (int index = text.Length - 1; index >= 0; index--)
            if (isAnyDirectorySeparator(text[index]))
            {
                offset = index + 1;
                break;
            }

        int length = text.Length - offset;
        return length == 0 ? null : new TextRange(start + offset, start + offset + length);
    }

    private Slice<ModuleCompletionNameAndKind> getStringLiteralCompletionsFromModuleNamesWorker(SourceFile file, Node node, Program program, Checker checker)
    {
        var literal = TsPath.NormalizeSlashes(node.TextValue());
        ResolutionMode mode = default;
        if (AST.IsStringLiteralLike(node))
            mode = program.GetModeForUsageLocation(file, node);
        var path = PathKey.FromValue(file.PathKeyValue());
        var directory = RootedFilePath.FromValue(file.FileNameGoValue()).Directory();
        var options = program.Options();
        var extensions = getExtensionOptions(options, ReferenceKind.ModuleSpecifier, file, mode, checker);
        if (isPathRelativeToScript(literal) || (options.Paths?.Size() ?? 0) == 0 && (TsPath.IsRootedDiskPath(literal) || TsPath.GetEncodedRootLength(literal) < 0))
            return getCompletionEntriesForRelativeModules(literal, directory, program, path, extensions);
        return getCompletionEntriesForNonRelativeModules(literal, directory, mode, program, checker, extensions);
    }
}

// Source assembly component: StringCompletionConversion.cs
internal sealed partial class LanguageService
{
    private CompletionList? getStringLiteralCompletions(ProtocolRequestContext context, SourceFile file, int position, Node? token, Checker checker, CompilerOptions options, bool includeSymbols)
    {
        if (isInReferenceComment(file, position))
            return convertPathCompletions(context, getTripleSlashReferenceCompletions(file, position, GetProgram(), checker), file, position);
        if (IsInString(file, position, token))
        {
            if (token == null || !AST.IsStringLiteralLike(token))
                return null;
            var entries = getStringLiteralCompletionEntries(context, file, token, position, checker);
            return convertStringLiteralCompletions(context, entries, token, file, position, checker, options, includeSymbols);
        }

        return null;
    }

    private CompletionList? convertStringLiteralCompletions(ProtocolRequestContext context, StringLiteralCompletions? completion, Node token, SourceFile file, int position, Checker checker, CompilerOptions options, bool includeSymbols)
    {
        if (completion == null)
            return null;
        var replacement = createRangeFromStringLiteralLikeContent(file, token, position);
        if (completion.fromPaths != null)
            return convertPathCompletions(context, completion.fromPaths, file, position);
        if (completion.fromProperties is { } properties)
        {
            var data = new CompletionDataData
            {
                symbols = properties.symbols,
                completionKind = CompletionKind.String,
                isNewIdentifierLocation = properties.hasIndexSignature,
                location = file.AsNode(),
                contextToken = token
            };
            var(_, items, error) = getCompletionEntriesFromSymbols(context, checker, data, token, position, file, options, includeSymbols);
            if (error != null)
                throw error;
            var commits = getDefaultCommitCharacters(properties.hasIndexSignature);
            return new CompletionList
            {
                IsIncomplete = false,
                ItemDefaults = setItemDefaults(context, position, file, items, ref commits, replacement),
                Items = items
            };
        }

        if (completion.fromTypes is { } types)
        {
            var quote = token.Kind == Kind.NoSubstitutionTemplateLiteral ? QuoteChar.Backtick : token.TextValue().StartsWith(ByteStringValue.FromString("'")) ? QuoteChar.SingleQuote : QuoteChar.DoubleQuote;
            var items = Core.MapSlice(types.types, type =>
            {
                var name = PrinterUtilities.EscapeStringValue(type.StringValue(), quote);
                var item = createLSPCompletionItem(context, name, default, default, CompletionSortText.LocationPriority, ScriptElementKind.String, ScriptElementKindModifier.None, getReplacementRangeForContextToken(file, token, position), null, null, file, position, false, false, false, false, default, null, null, null);
                return (CompletionItem? )new CompletionItem
                {
                    Item = item
                };
            });
            var commits = getDefaultCommitCharacters(types.isNewIdentifier);
            return new CompletionList
            {
                IsIncomplete = false,
                ItemDefaults = setItemDefaults(context, position, file, items, ref commits, null),
                Items = items
            };
        }

        return null;
    }

    private CompletionList? convertPathCompletions(ProtocolRequestContext context, PathCompletions? completion, SourceFile file, int position)
    {
        if (completion == null)
            return null;
        var commits = getDefaultCommitCharacters(true);
        var items = Core.MapSlice(completion.entries, path =>
        {
            var detail = path.name;
            if (!path.name.EndsWith(path.extension))
                detail = completionConcat(detail, path.extension);
            var item = createLSPCompletionItem(context, path.name, default, default, CompletionSortText.LocationPriority, path.kind, kindModifiersFromExtension(path.extension), completion.replacementSpan, null, null, file, position, false, false, false, false, default, null, null, detail);
            return (CompletionItem? )new CompletionItem
            {
                Item = item
            };
        });
        return new CompletionList
        {
            IsIncomplete = false,
            ItemDefaults = setItemDefaults(context, position, file, items, ref commits, null),
            Items = items
        };
    }
}

// Source assembly component: StringCompletionExtensions.cs
internal sealed partial class LanguageService
{
    private static (ByteStringValue Name, ByteStringValue Extension) getFilenameWithExtensionOption(ByteStringValue name, Program program, ExtensionOptions options, bool wildcard)
    {
        var nonJS = ModuleSpecifierApi.TryGetRealFileNameValueForNonJSDeclarationFileName(name);
        if (nonJS.Length != 0)
            return (nonJS, TsPath.TryGetExtensionFromPath(nonJS));
        if (options.referenceKind == ReferenceKind.FileName)
            return (name, TsPath.TryGetExtensionFromPath(name));
        var endings = ModuleSpecifierApi.GetAllowedEndingsInPreferredOrderValue(new TypeScript.ModuleSpecifiers.UserPreferences { ImportModuleSpecifierEnding = options.endingPreference }, program, program.Options(), options.importingSourceFile, default, options.resolutionMode);
        if (wildcard)
            endings = Core.Filter(endings, ending => ending is not (ModuleSpecifierEnding.Minimal or ModuleSpecifierEnding.Index));
        if (endings.Length > 0 && endings[0] == ModuleSpecifierEnding.TsExtension)
        {
            if (TsPath.FileExtensionIsOneOf(name, TsPath.SupportedTSImplementationExtensions))
                return (name, TsPath.TryGetExtensionFromPath(name));
            var output = ByteStringValue.FromString(ModuleUtilities.TryGetJSExtensionForFile(name, program.Options()));
            return output.Length != 0 ? (TsPath.ChangeExtension(name, output), output) : (name, TsPath.TryGetExtensionFromPath(name));
        }

        if (!wildcard && endings.Length > 0 && endings[0] is ModuleSpecifierEnding.Minimal or ModuleSpecifierEnding.Index && TsPath.FileExtensionIsOneOf(name, new[] { TsPath.ExtensionJs, TsPath.ExtensionJsx, TsPath.ExtensionTs, TsPath.ExtensionTsx, TsPath.ExtensionDts }))
            return (TsPath.RemoveFileExtension(name), TsPath.TryGetExtensionFromPath(name));
        var extension = ByteStringValue.FromString(ModuleUtilities.TryGetJSExtensionForFile(name, program.Options()));
        return extension.Length != 0 ? (TsPath.ChangeExtension(name, extension), extension) : (name, TsPath.TryGetExtensionFromPath(name));
    }
}

// Source assembly component: StringCompletionReferences.cs
internal sealed partial class LanguageService
{
    private static bool isInReferenceComment(SourceFile file, int position)
    {
        var comment = isInComment(file, position, AstNav.GetTokenAtPosition(file, position));
        return comment != null && hasTripleSlashPrefix(ByteStringValue.FromView(file.TextView().Slice(comment.Value.Pos, comment.Value.End)));
    }

    private static bool hasTripleSlashPrefix(ByteStringValue text) => text.StartsWith(ByteStringValue.FromString("///")) && GoStrings.TrimFunc(text.Slice(3), GoUnicode.IsSpace).StartsWith(ByteStringValue.FromString("<"));
    private static ByteStringValue trimDirectiveWhitespace(ByteStringValue text)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            var(rune, size) = completionDecodeRune(text.Slice(offset));
            if (!StringUtil.IsWhiteSpaceLike(rune))
                break;
            offset += size;
        }

        return text.Slice(offset);
    }

    private static (ByteStringValue Prefix, ByteStringValue Kind, ByteStringValue ToComplete, bool Ok) parseTripleSlashDirectiveFragment(ByteStringValue text)
    {
        var rest = text;
        if (!rest.StartsWith(ByteStringValue.FromString("///")))
            return (default, default, default, false);
        rest = trimDirectiveWhitespace(rest.Slice(3));
        if (!rest.StartsWith(ByteStringValue.FromString("<reference")))
            return (default, default, default, false);
        rest = rest.Slice("<reference".Length);
        if (rest.Length == 0 || !StringUtil.IsWhiteSpaceLike(rest[0]))
            return (default, default, default, false);
        rest = trimDirectiveWhitespace(rest);
        ByteStringValue kind;
        if (rest.StartsWith(ByteStringValue.FromString("path")))
        {
            kind = ByteStringValue.FromString("path");
            rest = rest.Slice(4);
        }
        else if (rest.StartsWith(ByteStringValue.FromString("types")))
        {
            kind = ByteStringValue.FromString("types");
            rest = rest.Slice(5);
        }
        else
            return (default, default, default, false);
        rest = trimDirectiveWhitespace(rest);
        if (!rest.StartsWith(ByteStringValue.FromString("=")))
            return (default, default, default, false);
        rest = trimDirectiveWhitespace(rest.Slice(1));
        if (rest.Length == 0 || rest[0] is not ('\'' or '"'))
            return (default, default, default, false);
        rest = rest.Slice(1);
        for (int index = 0; index < rest.Length; index++)
            if (rest[index] is '\'' or '"')
                return (default, default, default, false);
        return (text.Slice(0, text.Length - rest.Length), kind, rest, true);
    }

    private PathCompletions? getTripleSlashReferenceCompletions(SourceFile file, int position, Program program, Checker checker)
    {
        var token = AstNav.GetTokenAtPosition(file, position);
        Slice<CommentRange> comments = default;
        foreach (var comment in ScannerApi.GetLeadingCommentRanges(new NodeFactory(default), file.TextView(), token.Pos()))
            comments = comments.Append(comment);
        CommentRange? found = null;
        foreach (var comment in comments)
            if (position >= comment.Pos && position <= comment.End)
            {
                found = comment;
                break;
            }

        if (found == null)
            return null;
        var text = ByteStringValue.FromView(file.TextView().Slice(found.Value.Pos, position));
        var(prefix, kind, fragment, ok) = parseTripleSlashDirectiveFragment(text);
        if (!ok)
            return null;
        var(span, exact) = pathCompletionReplacementSpan(file, getDirectoryFragmentRange(fragment, found.Value.Pos + prefix.Length));
        if (!exact)
            return null;
        var directory = RootedFilePath.FromValue(file.FileNameGoValue()).Directory();
        Slice<ModuleCompletionNameAndKind> names = default;
        if (kind.EqualsString("path"))
        {
            var extensions = getExtensionOptions(program.Options(), ReferenceKind.FileName, file, ResolutionMode.None, null);
            var result = getCompletionEntriesForDirectoryFragment(fragment, directory, extensions, program, true, PathKey.FromValue(file.PathKeyValue()), new ModuleCompletionNameAndKindSet { names = new() });
            names = Slice<ModuleCompletionNameAndKind>.FromArray(result.names.Values.ToArray());
        }
        else if (kind.EqualsString("types"))
        {
            var extensions = getExtensionOptions(program.Options(), ReferenceKind.ModuleSpecifier, file, ResolutionMode.None, null);
            var result = new ModuleCompletionNameAndKindSet
            {
                names = new()
            };
            getCompletionEntriesFromTypings(program, directory, getFragmentDirectory(fragment), extensions, result);
            names = Slice<ModuleCompletionNameAndKind>.FromArray(result.names.Values.ToArray());
        }

        return new PathCompletions
        {
            entries = toPathCompletions(names),
            replacementSpan = span
        };
    }
}

// Source assembly component: StringCompletionWildcard.cs
internal sealed partial class LanguageService
{
    private Slice<ModuleCompletionNameAndKind> getModulesForPathsPattern(ByteStringValue fragment, RootedDirectoryPath directory, ByteStringValue pattern, bool exports, bool imports, ExtensionOptions options, Program program)
    {
        var parsed = Core.TryParsePattern(pattern);
        if (!parsed.IsValid() || parsed.StarIndex == -1)
            return default;
        var prefix = parsed.TextValue.Slice(0, parsed.StarIndex);
        var suffix = parsed.TextValue.Slice(parsed.StarIndex + 1);
        var normalizedPrefix = TsPath.ResolvePath(prefix);
        ByteStringValue prefixDirectory, prefixBase;
        if (TsPath.HasTrailingDirectorySeparator(prefix))
        {
            prefixDirectory = normalizedPrefix;
            prefixBase = default;
        }
        else
        {
            prefixDirectory = TsPath.GetDirectoryPath(normalizedPrefix);
            prefixBase = TsPath.GetBaseFileName(normalizedPrefix);
        }

        bool fragmentHasPath = containsSlash(fragment);
        ByteStringValue fragmentDirectory = default;
        if (fragmentHasPath)
            fragmentDirectory = TsPath.HasTrailingDirectorySeparator(fragment) ? fragment : TsPath.GetDirectoryPath(fragment);
        var compilerOptions = program.Options();
        var sensitivity = program.CaseSensitivity();
        RootedDirectoryPath outDir = compilerOptions.OutDir, declarationDir = compilerOptions.DeclarationDir;
        var expandedPrefix = fragmentHasPath ? TsPath.CombinePaths(prefixDirectory, completionConcat(prefixBase, fragmentDirectory)) : prefixDirectory;
        var baseDirectory = directory.ResolveDirectory(expandedPrefix);
        RootedDirectoryPath originalOut = default, originalDeclaration = default;
        if (imports)
        {
            if (outDir.AsValue().Length != 0)
                originalOut = getPossibleOriginalInputPathWithoutChangingExt(baseDirectory, sensitivity, outDir, program.CommonSourceDirectory);
            if (declarationDir.AsValue().Length != 0)
                originalDeclaration = getPossibleOriginalInputPathWithoutChangingExt(baseDirectory, sensitivity, declarationDir, program.CommonSourceDirectory);
        }

        var normalizedSuffix = TsPath.NormalizePath(suffix);
        ByteStringValue declarationExtension = default;
        Slice<ByteStringValue> inputExtensions = default;
        if (normalizedSuffix.Length != 0)
        {
            declarationExtension = TsPath.GetDeclarationEmitExtensionForPath(completionConcat(ByteStringValue.FromString("_"), normalizedSuffix));
            inputExtensions = TsPath.GetPossibleOriginalInputExtensionForExtension(completionConcat(ByteStringValue.FromString("_"), normalizedSuffix));
        }

        Slice<ByteStringValue> matchingSuffixes = default;
        if (declarationExtension.Length != 0)
            matchingSuffixes = matchingSuffixes.Append(TsPath.ChangeExtension(normalizedSuffix, declarationExtension));
        foreach (var extension in inputExtensions)
            matchingSuffixes = matchingSuffixes.Append(TsPath.ChangeExtension(normalizedSuffix, extension));
        matchingSuffixes = matchingSuffixes.Append(normalizedSuffix);
        Slice<ByteStringValue> includes = default;
        if (normalizedSuffix.Length != 0)
            foreach (var matchSuffix in matchingSuffixes)
                includes = includes.Append(completionConcat(ByteStringValue.FromString("**/*"), matchSuffix));
        else
            includes = Slice<ByteStringValue>.FromArray(new[] { ByteStringValue.FromString("./*") });
        bool wildcard = (exports || imports) && pattern.EndsWith(ByteStringValue.FromString("/*"));
        ByteStringValue Trim(ByteStringValue path, ByteStringValue start)
        {
            foreach (var end in matchingSuffixes)
            {
                var inner = withoutStartAndEnd(TsPath.NormalizePath(path), start, end);
                if (inner != null)
                    return removeLeadingDirectorySeparator(inner.Value);
            }

            return default;
        }

        Slice<ModuleCompletionNameAndKind> Matches(RootedDirectoryPath basePath)
        {
            var completePrefix = fragmentHasPath ? basePath.AsValue() : completionConcat(TsPath.EnsureTrailingDirectorySeparator(basePath.AsValue()), prefixBase);
            var matches = ReadDirectory(basePath, options.extensionsToSearch, includes);
            Slice<ModuleCompletionNameAndKind> result = default;
            foreach (var match in matches)
            {
                var trimmed = Trim(match.AsValue(), completePrefix);
                if (trimmed.Length == 0)
                    continue;
                if (containsSlash(trimmed))
                {
                    var components = TsPath.GetPathComponents(removeLeadingDirectorySeparator(trimmed));
                    if (components.Length > 1)
                        result = result.Append(new ModuleCompletionNameAndKind { name = components[1], kind = ModuleCompletionKind.Directory });
                }
                else
                {
                    var(name, extension) = getFilenameWithExtensionOption(trimmed, program, options, wildcard);
                    if (extension.Length == 0)
                        extension = getFileExtension(match.AsValue());
                    result = result.Append(new ModuleCompletionNameAndKind { name = name, kind = ModuleCompletionKind.File, extension = extension });
                }
            }

            return result;
        }

        Slice<ModuleCompletionNameAndKind> Directories(RootedDirectoryPath basePath)
        {
            Slice<ModuleCompletionNameAndKind> result = default;
            foreach (var child in GetDirectories(basePath))
                if (!child.EqualsValue(ByteStringValue.FromString("node_modules")))
                    result = result.Append(new ModuleCompletionNameAndKind { name = child, kind = ModuleCompletionKind.Directory });
            return result;
        }

        Slice<ModuleCompletionNameAndKind> all = default;
        all = all.Append(Matches(baseDirectory));
        if (originalOut.AsValue().Length != 0)
            all = all.Append(Matches(originalOut));
        if (originalDeclaration.AsValue().Length != 0)
            all = all.Append(Matches(originalDeclaration));
        if (normalizedSuffix.Length == 0)
        {
            all = all.Append(Directories(baseDirectory));
            if (originalOut.AsValue().Length != 0)
                all = all.Append(Directories(originalOut));
            if (originalDeclaration.AsValue().Length != 0)
                all = all.Append(Directories(originalDeclaration));
        }

        return all;
    }

    private static bool containsSlash(ByteStringValue fragment) => fragment.Contains((int)'/');
    private static ByteStringValue? withoutStartAndEnd(ByteStringValue text, ByteStringValue start, ByteStringValue end) => text.StartsWith(start) && text.EndsWith(end) && text.Length >= start.Length + end.Length ? text.Slice(start.Length, text.Length - end.Length) : null;
    private static ByteStringValue removeLeadingDirectorySeparator(ByteStringValue path) => path.StartsWith(ByteStringValue.FromString("/")) ? path.Slice(1) : path;
    private static RootedDirectoryPath getPossibleOriginalInputPathWithoutChangingExt(RootedDirectoryPath file, CaseSensitivity sensitivity, RootedDirectoryPath output, Func<RootedDirectoryPath> commonDirectory)
    {
        if (output.AsValue().Length == 0)
            return file;
        var(relative, ok) = sensitivity.RelativePathFromPath(output, file.AsPath());
        return ok ? commonDirectory().ResolveRelativeDirectory(relative) : file;
    }
}

// Source assembly component: StringCompletionDetails.cs
internal sealed partial class LanguageService
{
    private Protocol.CompletionItem getStringLiteralCompletionDetails(ProtocolRequestContext context, Checker checker, Protocol.CompletionItem item, ByteStringValue name, SourceFile file, int position, Node? token, MarkupKind format)
    {
        if (token == null || !AST.IsStringLiteralLike(token))
            return item;
        var completion = getStringLiteralCompletionEntries(context, file, token, position, checker);
        return completion == null ? item : stringLiteralCompletionDetails(item, name, token, position, completion, file, checker, format);
    }

    private Protocol.CompletionItem stringLiteralCompletionDetails(Protocol.CompletionItem item, ByteStringValue name, Node location, int position, StringLiteralCompletions completion, SourceFile file, Checker checker, MarkupKind format)
    {
        if (completion.fromPaths != null)
            return item;
        if (completion.fromProperties is { } properties)
            foreach (var symbol in properties.symbols)
                if (symbol.NameValue.EqualsValue(name))
                    return createCompletionDetailsForSymbol(item, symbol, checker, location, position, format);
        if (completion.fromTypes is { } types)
            foreach (var type in types.types)
                if (type.StringValue().EqualsValue(name))
                    return createCompletionDetails(item, name, default, format);
        return item;
    }
}

// Source assembly component: StringCompletionRelative.cs
internal sealed partial class LanguageService
{
    private Slice<ModuleCompletionNameAndKind> getCompletionEntriesForRelativeModules(ByteStringValue literal, RootedDirectoryPath directory, Program program, PathKey path, ExtensionOptions options)
    {
        if ((program.Options().RootDirs?.Count ?? 0) > 0)
            return getCompletionEntriesForDirectoryFragmentWithRootDirs(literal, directory, program, path, options);
        var result = getCompletionEntriesForDirectoryFragment(literal, directory, options, program, true, path, new ModuleCompletionNameAndKindSet { names = new() });
        return Slice<ModuleCompletionNameAndKind>.FromArray(result.names.Values.ToArray());
    }

    private Slice<ModuleCompletionNameAndKind> getCompletionEntriesForDirectoryFragmentWithRootDirs(ByteStringValue fragment, RootedDirectoryPath directory, Program program, PathKey exclude, ExtensionOptions options)
    {
        var bases = getBaseDirectoriesFromRootDirs(program.Options().GetEffectiveRootDirs(), directory, program.CaseSensitivity());
        Slice<ModuleCompletionNameAndKind> all = default;
        foreach (var baseDirectory in bases)
        {
            var result = getCompletionEntriesForDirectoryFragment(fragment, baseDirectory, options, program, true, exclude, new ModuleCompletionNameAndKindSet { names = new() });
            foreach (var entry in result.names.Values)
                all = all.Append(entry);
        }

        return deduplicateModuleCompletions(all);
    }

    private static Slice<RootedDirectoryPath> getBaseDirectoriesFromRootDirs(List<RootedDirectoryPath>? roots, RootedDirectoryPath directory, CaseSensitivity sensitivity)
    {
        RelativePath relative = default;
        foreach (var root in roots ?? [])
        {
            var(value, ok) = sensitivity.RelativePathWithinDirectory(root, directory.AsPath());
            if (ok)
            {
                relative = value;
                break;
            }
        }

        Slice<RootedDirectoryPath> directories = default;
        foreach (var root in roots ?? [])
            directories = directories.Append(relative.AsValue().Length == 0 ? root : root.ResolveRelativeDirectory(relative));
        directories = directories.Append(directory);
        return deduplicateDirectoryNames(directories);
    }

    private static Slice<RootedDirectoryPath> deduplicateDirectoryNames(Slice<RootedDirectoryPath> items)
    {
        if (items.Length <= 1)
            return items;
        var seen = new Dictionary<RootedDirectoryPath, bool>();
        Slice<RootedDirectoryPath> result = default;
        foreach (var directory in items)
            if (!seen.TryGetValue(directory, out bool value) || !value)
            {
                seen[directory] = true;
                result = result.Append(directory);
            }

        return result;
    }

    private static Slice<ModuleCompletionNameAndKind> deduplicateModuleCompletions(Slice<ModuleCompletionNameAndKind> items)
    {
        if (items.Length <= 1)
            return items;
        var seen = new Dictionary<(ByteStringKey Name, ModuleCompletionKind Kind, ByteStringKey Extension), bool>();
        Slice<ModuleCompletionNameAndKind> result = default;
        foreach (var item in items)
        {
            var key = (ByteStringKey.FromValue(item.name), item.kind, ByteStringKey.FromValue(item.extension));
            if (!seen.TryGetValue(key, out bool value) || !value)
            {
                seen[key] = true;
                result = result.Append(item);
            }
        }

        return result;
    }
}

// Source assembly component: StringCompletionTypeHelpers.cs
internal sealed partial class LanguageService
{
    private static Node walkUpParentheses(Node node) => node.Kind switch
    {
        Kind.ParenthesizedType => AST.WalkUpParenthesizedTypes(node),
        Kind.ParenthesizedExpression => AST.WalkUpParenthesizedExpressions(node),
        _ => node
    };
    private static Slice<LiteralType> getStringLiteralTypes(TypeScript.Checking.Type? type, Checker checker)
    {
        HashSet<ByteStringKey>? unique = null;
        return getStringLiteralTypes(type, ref unique, checker);
    }

    private static Slice<LiteralType> getStringLiteralTypes(TypeScript.Checking.Type? type, ref HashSet<ByteStringKey>? unique, Checker checker)
    {
        if (type == null)
            return default;
        type = skipConstraint(type, checker);
        if (type.IsUnion())
        {
            Slice<LiteralType> result = default;
            foreach (var element in type.Types())
                result = result.Append(getStringLiteralTypes(element, ref unique, checker));
            return result;
        }

        if (type.IsStringLiteral() && !type.IsEnumLiteral() && (unique ??= new()).Add(ByteStringKey.FromValue(type.AsLiteralType().StringValue())))
            return Slice<LiteralType>.FromArray(new[] { type.AsLiteralType() });
        return default;
    }

    private static Slice<ByteStringValue> getAlreadyUsedTypesInStringLiteralUnion(Node union, Node current)
    {
        var list = union.AsUnionTypeNode().Types;
        if (list == null)
            return default;
        Slice<ByteStringValue> values = default;
        foreach (var node in list.Nodes)
            if (node != current && AST.IsLiteralTypeNode(node) && AST.IsStringLiteral(node.AsLiteralTypeNode().Literal))
                values = values.Append(node.AsLiteralTypeNode().Literal.TextValue());
        return values;
    }

    private static bool hasIndexSignature(TypeScript.Checking.Type type, Checker checker) => checker.GetStringIndexType(type) != null || checker.GetNumberIndexType(type) != null;
    private static bool isRequireCallArgument(Node node) => AST.IsCallExpression(node.Parent) && node.Parent!.Arguments().Length > 0 && node.Parent.Arguments()[0] == node && AST.IsIdentifier(node.Parent.Expression()) && node.Parent.Expression().TextValue().EqualsValue(ByteStringValue.FromString("require"));
    private static ScriptElementKindModifier kindModifiersFromExtension(ByteStringValue extension)
    {
        if (extension.EqualsString(TsPath.ExtensionTsBuildInfo))
            throw new InvalidOperationException("Extension " + TsPath.ExtensionTsBuildInfo + " is unsupported.");
        return extension.Materialize() switch
        {
            TsPath.ExtensionDts => ScriptElementKindModifier.Dts,
            TsPath.ExtensionJs => ScriptElementKindModifier.Js,
            TsPath.ExtensionJson => ScriptElementKindModifier.Json,
            TsPath.ExtensionJsx => ScriptElementKindModifier.Jsx,
            TsPath.ExtensionTs => ScriptElementKindModifier.Ts,
            TsPath.ExtensionTsx => ScriptElementKindModifier.Tsx,
            TsPath.ExtensionDmts => ScriptElementKindModifier.Dmts,
            TsPath.ExtensionMjs => ScriptElementKindModifier.Mjs,
            TsPath.ExtensionMts => ScriptElementKindModifier.Mts,
            TsPath.ExtensionDcts => ScriptElementKindModifier.Dcts,
            TsPath.ExtensionCjs => ScriptElementKindModifier.Cjs,
            TsPath.ExtensionCts => ScriptElementKindModifier.Cts,
            _ => ScriptElementKindModifier.None,
        };
    }

    private static CompletionsFromTypes? getStringLiteralCompletionsFromSignature(Node call, Node argument, ArgumentInfoForCompletions info, Checker checker)
    {
        bool newIdentifier = false;
        HashSet<ByteStringKey>? unique = null;
        var editing = AST.IsJsxOpeningLikeElement(call) ? AST.FindAncestor(argument.Parent, AST.IsJsxAttribute) : argument;
        if (editing == null)
            throw new InvalidOperationException("Expected jsx opening-like element to have a jsx attribute as ancestor.");
        var candidates = checker.GetCandidateSignaturesForStringLiteralCompletions(call, editing);
        Slice<LiteralType> types = default;
        foreach (var candidate in candidates)
        {
            if (!candidate.HasRestParameter() && info.argumentCount > candidate.Parameters().Length)
                continue;
            var type = checker.GetTypeParameterAtPosition(candidate, info.argumentIndex);
            if (AST.IsJsxOpeningLikeElement(call))
                type = checker.GetTypeOfPropertyOfType(type, editing.AsJsxAttribute().Name().TextValue()) ?? type;
            newIdentifier |= type.IsString();
            types = types.Append(getStringLiteralTypes(type, ref unique, checker));
        }

        return types.Length > 0 ? new CompletionsFromTypes
        {
            types = types,
            isNewIdentifier = newIdentifier
        }

        : null;
    }
}

// Source assembly component: StringCompletionPathMapping.cs
internal sealed partial class LanguageService
{
    private Slice<ModuleCompletionNameAndKind> getCompletionsForPathMapping(ByteStringValue path, Slice<ByteStringValue> patterns, ByteStringValue fragment, RootedDirectoryPath directory, bool exports, bool imports, ExtensionOptions options, Program program)
    {
        var fragmentDirectory = getFragmentDirectory(fragment);
        if (fragmentDirectory.Length != 0)
            fragmentDirectory = TsPath.EnsureTrailingDirectorySeparator(fragmentDirectory);
        Slice<ModuleCompletionNameAndKind> JustName(ByteStringValue name, ModuleCompletionKind kind, ByteStringValue extension)
        {
            if (!name.StartsWith(fragment))
                return default;
            name = TsPath.RemoveTrailingDirectorySeparator(name);
            if (fragmentDirectory.Length != 0 && name.StartsWith(fragmentDirectory))
                name = name.Slice(fragmentDirectory.Length);
            return Slice<ModuleCompletionNameAndKind>.FromArray(new[] { new ModuleCompletionNameAndKind { name = name, kind = kind, extension = extension } });
        }

        var parsed = Core.TryParsePattern(path);
        if (!parsed.IsValid())
            return default;
        if (parsed.StarIndex == -1)
            return JustName(path, ModuleCompletionKind.File, getFileExtension(Core.FirstOrNil(patterns)));
        var prefix = parsed.TextValue.Slice(0, parsed.StarIndex);
        var suffix = parsed.TextValue.Slice(parsed.StarIndex + 1);
        if (!fragment.StartsWith(prefix))
        {
            if (!prefix.StartsWith(fragment))
                return default;
            if (path.EndsWith(ByteStringValue.FromString("/*")))
                return JustName(prefix, ModuleCompletionKind.Directory, default);
            var remainingPrefix = prefix.Slice(fragmentDirectory.Length);
            Slice<ModuleCompletionNameAndKind> completions = default;
            foreach (var pattern in patterns)
            {
                var modules = getModulesForPathsPattern(default, directory, pattern, exports, imports, options, program);
                var span = modules.AsSpan();
                for (int index = 0; index < span.Length; index++)
                    span[index] = span[index] with
                    {
                        name = completionConcat(remainingPrefix, span[index].name, span[index].kind == ModuleCompletionKind.File ? suffix : default)
                    };
                completions = completions.Append(modules);
            }

            return completions;
        }

        var remaining = fragment.Slice(prefix.Length);
        ByteStringValue remainingDirectory = default;
        if (!fragmentDirectory.StartsWith(prefix))
            remainingDirectory = prefix.Slice(fragmentDirectory.Length);
        return Core.FlatMap(patterns, pattern =>
        {
            var modules = getModulesForPathsPattern(remaining, directory, pattern, exports, imports, options, program);
            var span = modules.AsSpan();
            for (int index = 0; index < span.Length; index++)
                span[index] = span[index] with
                {
                    name = completionConcat(remainingDirectory, span[index].name, span[index].kind == ModuleCompletionKind.File ? suffix : default)
                };
            return modules;
        });
    }

    private static ByteStringValue getFileExtension(ByteStringValue name)
    {
        var extension = TsPath.TryGetExtensionFromPath(name);
        return extension.Length == 0 ? TsPath.GetAnyExtensionFromPath(name, null, TypeScript.Scanner.CaseSensitivity.CaseSensitive) : extension;
    }
}

// Source assembly component: StringCompletionModels.cs
internal sealed partial class LanguageService
{
    private sealed class CompletionsFromTypes
    {
        internal Slice<LiteralType> types;
        internal bool isNewIdentifier;
    }

    private sealed class CompletionsFromProperties
    {
        internal Slice<Symbol> symbols;
        internal bool hasIndexSignature;
    }

    private sealed class PathCompletion
    {
        internal ByteStringValue name;
        internal ScriptElementKind kind;
        internal ByteStringValue extension;
    }

    private sealed class PathCompletions
    {
        internal Slice<PathCompletion> entries;
        internal Protocol.Range? replacementSpan;
    }

    private sealed class StringLiteralCompletions
    {
        internal CompletionsFromTypes? fromTypes;
        internal CompletionsFromProperties? fromProperties;
        internal PathCompletions? fromPaths;
    }
}
