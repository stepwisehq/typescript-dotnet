// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/organizeimports.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.Change;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.DiagnosticPrinting;
using CompilerProgram = TypeScript.Compiler.Program;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using NodeFactory = TypeScript.Scanner.NodeFactory;
using UserPreferences = TypeScript.LanguageServices.Utilities.UserPreferences;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal Dictionary<RootedFilePath, Slice<TextEdit>> OrganizeImports(ProtocolRequestContext context, SourceFile file, CompilerProgram program, CodeActionKind kind)
    {
        var tracker = Tracker.NewTracker(context, program.Options(), FormatOptions(), converters);
        bool sort = kind == CodeActionKind.SourceSortImportsTs || kind == CodeActionKind.SourceOrganizeImportsTs;
        bool combine = sort;
        bool remove = kind == CodeActionKind.SourceRemoveUnusedImportsTs || kind == CodeActionKind.SourceOrganizeImportsTs;
        var imports = LsUtil.FilterImportDeclarations(file.Statements.Nodes);
        var groups = groupByNewlineContiguous(file, imports);
        var preferences = UserPreferences();
        var(comparers, typeOrders) = LsUtil.GetDetectionLists(preferences);
        var defaultComparer = comparers[0];
        var sortKind = LsUtil.ResolveOrganizeImportsSort(preferences);
        Func<string, string, int>? moduleComparer = null, nameComparer = null;
        if (sortKind != OrganizeImportsSort.Auto)
            moduleComparer = nameComparer = defaultComparer;
        var typeOrder = preferences.OrganizeImportsTypeOrder;
        if (sortKind == OrganizeImportsSort.Auto)
        {
            var(detected, _) = LsUtil.DetectModuleSpecifierCaseBySort(groups, comparers);
            moduleComparer = detected;
        }

        if (typeOrder == OrganizeImportsTypeOrder.Auto || sortKind == OrganizeImportsSort.Auto)
        {
            var(detected, detectedOrder, found) = LsUtil.DetectNamedImportOrganizationBySort(imports, comparers, typeOrders);
            if (found)
            {
                if (nameComparer is null || sortKind == OrganizeImportsSort.Auto)
                    nameComparer = detected;
                if (typeOrder == OrganizeImportsTypeOrder.Auto)
                    typeOrder = detectedOrder;
            }
        }

        var settings = new OrganizeImportsComparerSettings(moduleComparer, nameComparer, typeOrder);
        foreach (var group in groups)
            organizeImportsWorker(group, settings, sort, combine, remove, file, program, tracker, context);
        if (kind != CodeActionKind.SourceRemoveUnusedImportsTs)
            foreach (var group in getTopLevelExportGroups(file))
                organizeExportsWorker(group, settings, file, tracker);
        foreach (var statement in file.Statements.Nodes)
        {
            if (!Ast.IsAmbientModule(statement))
                continue;
            var module = statement.AsModuleDeclaration();
            if (module.Body is null)
                continue;
            var body = module.Body.AsModuleBlock();
            var moduleImports = LsUtil.FilterImportDeclarations(body.Statements.Nodes);
            foreach (var group in groupByNewlineContiguous(file, moduleImports))
                organizeImportsWorker(group, settings, sort, combine, remove, file, program, tracker, context);
            if (kind != CodeActionKind.SourceRemoveUnusedImportsTs)
            {
                Slice<Node> exports = default;
                foreach (var item in body.Statements.Nodes)
                    if (item.Kind == Kind.ExportDeclaration)
                        exports = exports.Append(item);
                organizeExportsWorker(exports, settings, file, tracker);
            }
        }

        // Unmappable files are dropped by the actual change tracker.
        var(changes, _) = tracker.GetChanges();
        return changes;
    }

    private readonly record struct OrganizeImportsComparerSettings(Func<string, string, int>? ModuleComparer, Func<string, string, int>? NameComparer, OrganizeImportsTypeOrder TypeOrder);
    private static void organizeImportsWorker(Slice<Node> oldImports, OrganizeImportsComparerSettings comparer, bool shouldSort, bool shouldCombine, bool shouldRemove, SourceFile file, CompilerProgram program, Tracker tracker, ProtocolRequestContext context)
    {
        if (oldImports.Length == 0)
            return;
        var processed = GoSlices.Clone(oldImports);
        Action? done = null;
        try
        {
            if (shouldRemove)
            {
                var(checker, release) = program.GetTypeCheckerForFile(context, file);
                done = release;
                processed = removeUnusedImports(processed, file, checker, program, tracker);
            }

            Slice<Node> newImports = default;
            if (shouldCombine)
            {
                var grouped = groupByModuleSpecifier(processed);
                if (shouldSort)
                    GoSliceSort.SortFunc(grouped.AsSpan(), (a, b) => a.Length == 0 || b.Length == 0 ? 0 : LsUtil.CompareModuleSpecifiers(a[0].ModuleSpecifier(), b[0].ModuleSpecifier(), comparer.ModuleComparer));
                var specifierComparer = LsUtil.GetNamedImportSpecifierComparer(new UserPreferences { OrganizeImportsTypeOrder = comparer.TypeOrder }, comparer.NameComparer);
                foreach (var group in grouped)
                {
                    var coalesced = coalesceImportsWorker(group, comparer.ModuleComparer!, specifierComparer, file, tracker);
                    if (shouldSort)
                        GoSliceSort.SortStableFunc(coalesced.AsSpan(), (a, b) => LsUtil.CompareImportsOrRequireStatements(a, b, comparer.ModuleComparer));
                    newImports = newImports.Append(coalesced);
                }
            }
            else
                newImports = processed;
            if (shouldSort && !shouldCombine)
                GoSliceSort.SortFunc(newImports.AsSpan(), (a, b) => LsUtil.CompareImportsOrRequireStatements(a, b, comparer.ModuleComparer));
            if (newImports.Length == 0)
                tracker.DeleteNodeRange(file, oldImports[0], oldImports[oldImports.Length - 1], LeadingTriviaOption.Exclude, TrailingTriviaOption.Include);
            else
            {
                foreach (var import in newImports)
                    tracker.EmitContext.SetEmitFlags(import, EmitFlags.NoLeadingComments);
                var options = new NodeOptions
                {
                    LeadingTriviaOption = LeadingTriviaOption.Exclude,
                    TrailingTriviaOption = TrailingTriviaOption.Include,
                    Suffix = ByteStringValue.FromString("\n")
                };
                // Go core.Map creates this required new node-header result, even though Node is the shared C# AST owner.
                var nodes = Core.MapSlice(newImports, static node => node);
                tracker.ReplaceNodeWithNodes(file, oldImports[0], nodes, options);
                for (int i = 1; i < oldImports.Length; i++)
                    tracker.Delete(file, oldImports[i]);
            }
        }
        finally
        {
            done?.Invoke();
        }
    }

    private static Slice<Slice<Node>> groupByModuleSpecifier(Slice<Node> imports)
    {
        var groups = new Dictionary<ByteStringKey, Slice<Node>>();
        Slice<ByteStringKey> order = default;
        foreach (var import in imports)
        {
            var key = ByteStringKey.FromValue(LsUtil.GetExternalModuleNameValue(import.ModuleSpecifier()));
            if (!groups.ContainsKey(key))
                order = order.Append(key);
            groups[key] = groups.GetValueOrDefault(key).Append(import);
        }

        var result = Slice<Slice<Node>>.Make(0, order.Length);
        foreach (var key in order)
            result = result.Append(groups[key]);
        return result;
    }

    private static Slice<Node> removeUnusedImports(Slice<Node> imports, SourceFile file, Checker checker, CompilerProgram program, Tracker tracker)
    {
        var options = program.Options();
        bool jsx = (file.SubtreeFacts() & SubtreeFacts.ContainsJsx) != 0;
        bool explicitImport = options.Jsx is JsxEmit.React or JsxEmit.ReactNative;
        var factory = new NodeFactory(default);
        var used = Slice<Node>.Make(0, imports.Length);
        foreach (var import in imports)
        {
            var declaration = import.AsImportDeclaration();
            if (declaration.ImportClause is null)
            {
                used = used.Append(import);
                continue;
            }

            var clause = declaration.ImportClause.AsImportClause();
            var name = clause.Name();
            var bindings = clause.NamedBindings;
            if (name is not null && !checker.IsDeclarationUsed(file, name.AsIdentifier(), jsx, explicitImport))
                name = null;
            if (bindings is not null)
            {
                switch (bindings.Kind)
                {
                    case Kind.NamespaceImport:
                        if (!checker.IsDeclarationUsed(file, bindings.AsNamespaceImport().Name().AsIdentifier(), jsx, explicitImport))
                            bindings = null;
                        break;
                    case Kind.NamedImports:
                        var named = bindings.AsNamedImports();
                        var original = bindings;
                        var elements = filterUsedImportSpecifiers(named.Elements.Nodes, checker, file, jsx, explicitImport);
                        if (elements.Length == 0)
                            bindings = null;
                        else if (elements.Length < named.Elements.Nodes.Length)
                            bindings = factory.UpdateNamedImports(named, factory.NewNodeList(elements));
                        if (bindings is not null && !Ast.NodeIsSynthesized(original) && !PrinterUtilities.RangeIsOnSingleLine(original.Loc, file))
                            tracker.EmitContext.SetEmitFlags(bindings, EmitFlags.MultiLine);
                        break;
                }
            }

            if (name is not null || bindings is not null)
            {
                var newClause = factory.UpdateImportClause(clause, clause.PhaseModifier, name, bindings);
                used = used.Append(factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), newClause, declaration.ModuleSpecifier, declaration.Attributes));
            }
            else if (hasModuleDeclarationMatchingSpecifier(file, import.ModuleSpecifier()))
            {
                if (file.IsDeclarationFile)
                    used = used.Append(factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), null, declaration.ModuleSpecifier, declaration.Attributes));
                else
                    used = used.Append(import);
            }
        }

        return used;
    }

    private static Slice<Node> filterUsedImportSpecifiers(Slice<Node> elements, Checker checker, SourceFile file, bool jsx, bool explicitImport)
    {
        Slice<Node> result = default;
        foreach (var element in elements)
            if (checker.IsDeclarationUsed(file, element.AsImportSpecifier().Name().AsIdentifier(), jsx, explicitImport))
                result = result.Append(element);
        return result;
    }

    private static bool hasModuleDeclarationMatchingSpecifier(SourceFile file, Node? specifier)
    {
        if (specifier is null || !Ast.IsStringLiteral(specifier))
            return false;
        var text = specifier.TextValue();
        foreach (var name in file.ModuleAugmentations)
            if (Ast.IsStringLiteral(name) && name.TextValue().EqualsValue(text))
                return true;
        return false;
    }

    private static ByteStringValue getImportAttributesKey(Node? attributes)
    {
        if (attributes is null)
            return default;
        var imports = attributes.AsImportAttributes();
        var key = new GoBuilder();
        key.WriteString(imports.Token.String());
        key.WriteString(" ");
        var nodes = Slice<Node>.Make(imports.Attributes.Nodes.Length);
        imports.Attributes.Nodes.AsSpan().CopyTo(nodes.AsSpan());
        GoSliceSort.SortFunc(nodes.AsSpan(), static (a, b) => StringUtil.CompareStringsCaseSensitive(a.AsImportAttribute().Name().TextValue(), b.AsImportAttribute().Name().TextValue()));
        foreach (var node in nodes)
        {
            var attribute = node.AsImportAttribute();
            key.WriteString(attribute.Name().TextValue());
            key.WriteString(":");
            if (Ast.IsStringLiteralLike(attribute.Value))
            {
                key.WriteString("\"");
                key.WriteString(attribute.Value.TextValue());
                key.WriteString("\"");
            }
            else
                key.WriteString(attribute.Value.TextValue());
            key.WriteString(" ");
        }

        return key.Value();
    }

    private static Slice<Slice<Node>> groupByNewlineContiguous(SourceFile file, Slice<Node> declarations)
    {
        var scanner = ScannerApi.NewScanner();
        scanner.SetSkipTrivia(false);
        Slice<Slice<Node>> groups = default;
        Slice<Node> current = default;
        foreach (var declaration in declarations)
        {
            if (current.Length > 0 && isNewGroup(file, declaration, scanner))
            {
                groups = groups.Append(current);
                current = default;
            }

            current = current.Append(declaration);
        }

        if (current.Length > 0)
            groups = groups.Append(current);
        return groups;
    }

    private static bool isNewGroup(SourceFile file, Node declaration, ScannerApi scanner)
    {
        int start = declaration.Pos();
        if (start < 0)
            return false;
        var text = file.TextView();
        if (start >= text.Length)
            return false;
        int tokenStart = ScannerApi.SkipTrivia(text, start);
        if (tokenStart <= start)
            return false;
        int length = tokenStart - start;
        scanner.SetByteTextInternal(text.Slice(start, tokenStart));
        int lines = 0;
        while (scanner.TokenStart() < length)
            if (scanner.Scan() == Kind.NewLineTrivia && ++lines >= 2)
                return true;
        return false;
    }

    private static Slice<Node> coalesceImportsWorker(Slice<Node> imports, Func<string, string, int> comparer, Func<Node, Node, int> specifierComparer, SourceFile? file, Tracker tracker)
    {
        if (imports.Length == 0)
            return imports;
        var groups = new Dictionary<ByteStringKey, Slice<Node>>();
        Slice<ByteStringKey> keys = default;
        foreach (var import in imports)
        {
            var key = ByteStringKey.FromValue(getImportAttributesKey(import.AsImportDeclaration().Attributes));
            if (!groups.ContainsKey(key))
                keys = keys.Append(key);
            groups[key] = groups.GetValueOrDefault(key).Append(import);
        }

        var result = Slice<Node>.Make(0);
        foreach (var key in keys)
        {
            var categorized = getCategorizedImports(groups[key]);
            if (categorized.ImportWithoutClause is not null)
                result = result.Append(categorized.ImportWithoutClause);
            GoSliceSort.SortStableFunc(categorized.SourcePhaseImports.AsSpan(), (a, b) =>
            {
                a = a.AsImportDeclaration().ImportClause!;
                b = b.AsImportDeclaration().ImportClause!;
                if (a.Name()is null && b.Name()is null)
                    return 0;
                if (a.Name()is null)
                    return 1;
                if (b.Name()is null)
                    return -1;
                return specifierComparer(a, b);
            });
            result = result.Append(categorized.SourcePhaseImports);
            var factory = new NodeFactory(default);
            for (int i = 0; i < 2; i++)
            {
                // Immediate Go array elements copy these value headers.
                var group = i == 0 ? categorized.RegularImports : categorized.TypeOnlyImports;
                if (group.IsEmpty())
                    continue;
                bool typeOnly = i == 1;
                if (!typeOnly && group.DefaultImports.Length == 1 && group.NamespaceImports.Length == 1 && group.NamedImports.Length == 0)
                {
                    var defaultImport = group.DefaultImports[0];
                    var namespaceImport = group.NamespaceImports[0];
                    var clause = defaultImport.AsImportDeclaration().ImportClause!.AsImportClause();
                    var bindings = namespaceImport.AsImportDeclaration().ImportClause!.AsImportClause().NamedBindings;
                    var newClause = factory.UpdateImportClause(clause, clause.PhaseModifier, clause.Name(), bindings);
                    var declaration = defaultImport.AsImportDeclaration();
                    result = result.Append(factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), newClause, declaration.ModuleSpecifier, declaration.Attributes));
                    continue;
                }

                GoSliceSort.SortFunc(group.NamespaceImports.AsSpan(), (a, b) =>
                {
                    var first = a.AsImportDeclaration().ImportClause!.AsImportClause().NamedBindings!.AsNamespaceImport().Name();
                    var second = b.AsImportDeclaration().ImportClause!.AsImportClause().NamedBindings!.AsNamespaceImport().Name();
                    return comparer(first.Text(), second.Text());
                });
                foreach (var import in group.NamespaceImports)
                {
                    var declaration = import.AsImportDeclaration();
                    var clause = declaration.ImportClause!.AsImportClause();
                    var newClause = factory.UpdateImportClause(clause, clause.PhaseModifier, null, clause.NamedBindings);
                    result = result.Append(factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), newClause, declaration.ModuleSpecifier, declaration.Attributes));
                }

                Node? firstDefault = group.DefaultImports.Length > 0 ? group.DefaultImports[0] : null;
                Node? firstNamed = group.NamedImports.Length > 0 ? group.NamedImports[0] : null;
                var firstImport = firstDefault ?? firstNamed;
                if (firstImport is null)
                    continue;
                Node? defaultName = null;
                Slice<Node> specifiers = default;
                if (group.DefaultImports.Length == 1)
                    defaultName = group.DefaultImports[0].AsImportDeclaration().ImportClause!.AsImportClause().Name();
                else
                    foreach (var import in group.DefaultImports)
                    {
                        var clause = import.AsImportDeclaration().ImportClause!.AsImportClause();
                        specifiers = specifiers.Append(factory.NewImportSpecifier(false, factory.NewIdentifier("default"), clause.Name()));
                    }

                specifiers = specifiers.Append(getNewImportSpecifiers(group.NamedImports, factory));
                GoSliceSort.SortStableFunc(specifiers.AsSpan(), (left, right) => specifierComparer(left, right));
                Node? namedImports;
                if (specifiers.Length == 0)
                    namedImports = defaultName is not null ? null : factory.NewNamedImports(factory.NewNodeList(default(Slice<Node>)));
                else
                {
                    var sorted = factory.NewNodeList(specifiers);
                    if (firstNamed is not null)
                    {
                        var original = firstNamed.AsImportDeclaration().ImportClause!.AsImportClause().NamedBindings!.AsNamedImports();
                        if (original.Elements.HasTrailingComma())
                            sorted.Loc = original.Elements.Loc;
                        namedImports = factory.UpdateNamedImports(original, sorted);
                    }
                    else
                        namedImports = factory.NewNamedImports(sorted);
                }

                if (file is not null && namedImports is not null && firstNamed is not null)
                {
                    var original = firstNamed.AsImportDeclaration().ImportClause!.AsImportClause().NamedBindings!;
                    if (!Ast.NodeIsSynthesized(original) && !PrinterUtilities.RangeIsOnSingleLine(original.Loc, file))
                        tracker.EmitContext.SetEmitFlags(namedImports, EmitFlags.MultiLine);
                }

                if (typeOnly && defaultName is not null && namedImports is not null)
                {
                    var declaration = firstImport.AsImportDeclaration();
                    var defaultClause = factory.NewImportClause(declaration.ImportClause!.AsImportClause().PhaseModifier, defaultName, null);
                    result = result.Append(factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), defaultClause, declaration.ModuleSpecifier, declaration.Attributes));
                    var namedDeclaration = (firstNamed ?? firstImport).AsImportDeclaration();
                    var namedClause = factory.NewImportClause(namedDeclaration.ImportClause!.AsImportClause().PhaseModifier, null, namedImports);
                    result = result.Append(factory.UpdateImportDeclaration(namedDeclaration, namedDeclaration.Modifiers(), namedClause, namedDeclaration.ModuleSpecifier, namedDeclaration.Attributes));
                }
                else
                {
                    var declaration = firstImport.AsImportDeclaration();
                    var clause = declaration.ImportClause!.AsImportClause();
                    var newClause = factory.UpdateImportClause(clause, clause.PhaseModifier, defaultName, namedImports);
                    result = result.Append(factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), newClause, declaration.ModuleSpecifier, declaration.Attributes));
                }
            }
        }

        return result;
    }

    private struct CategorizedImports
    {
        internal Node? ImportWithoutClause;
        internal Slice<Node> SourcePhaseImports;
        internal ImportGroup TypeOnlyImports, RegularImports;
    }

    private struct ImportGroup
    {
        internal Slice<Node> DefaultImports, NamespaceImports, NamedImports;
        internal readonly bool IsEmpty() => DefaultImports.Length == 0 && NamespaceImports.Length == 0 && NamedImports.Length == 0;
    }

    private static CategorizedImports getCategorizedImports(Slice<Node> imports)
    {
        CategorizedImports result = default;
        foreach (var import in imports)
        {
            var clauseNode = import.AsImportDeclaration().ImportClause;
            if (clauseNode is null)
            {
                result.ImportWithoutClause ??= import;
                continue;
            }

            var clause = clauseNode.AsImportClause();
            if (clause.PhaseModifier == Kind.SourceKeyword)
            {
                result.SourcePhaseImports = result.SourcePhaseImports.Append(import);
                continue;
            }

            ref var group = ref (clause.IsTypeOnly() ? ref result.TypeOnlyImports : ref result.RegularImports);
            if (clause.Name()is not null)
                group.DefaultImports = group.DefaultImports.Append(import);
            if (clause.NamedBindings is not null)
                switch (clause.NamedBindings.Kind)
                {
                    case Kind.NamespaceImport:
                        group.NamespaceImports = group.NamespaceImports.Append(import);
                        break;
                    case Kind.NamedImports:
                        group.NamedImports = group.NamedImports.Append(import);
                        break;
                }
        }

        return result;
    }

    private static Slice<Node> getNewImportSpecifiers(Slice<Node> imports, NodeFactory factory)
    {
        Slice<Node> result = default;
        foreach (var import in imports)
        {
            var elements = tryGetNamedBindingElements(import);
            if (elements.IsNil)
                continue;
            foreach (var element in elements)
            {
                var specifier = element.AsImportSpecifier();
                if (specifier.PropertyName is not null && specifier.Name()is not null && specifier.PropertyName.TextValue().EqualsValue(specifier.Name().TextValue()))
                {
                    result = result.Append(factory.UpdateImportSpecifier(specifier, specifier.IsTypeOnly, null, specifier.Name()));
                    continue;
                }

                result = result.Append(element);
            }
        }

        return result;
    }

    private static Slice<Node> tryGetNamedBindingElements(Node import)
    {
        if (import.Kind != Kind.ImportDeclaration)
            return default;
        var clauseNode = import.AsImportDeclaration().ImportClause;
        if (clauseNode is null)
            return default;
        var bindings = clauseNode.AsImportClause().NamedBindings;
        return bindings is not null && bindings.Kind == Kind.NamedImports ? bindings.AsNamedImports().Elements.Nodes : default;
    }

    private static Slice<Slice<Node>> getTopLevelExportGroups(SourceFile file)
    {
        Slice<Slice<Node>> groups = default;
        var statements = file.Statements.Nodes;
        int index = 0, groupIndex = 0;
        while (index < statements.Length)
        {
            if (statements[index].Kind == Kind.ExportDeclaration)
            {
                if (groupIndex >= groups.Length)
                    groups = groups.Append(Slice<Node>.Make(0));
                var declaration = statements[index].AsExportDeclaration();
                if (declaration.ModuleSpecifier is not null)
                {
                    groups[groupIndex] = groups[groupIndex].Append(statements[index]);
                    index++;
                }
                else
                {
                    while (index < statements.Length && statements[index].Kind == Kind.ExportDeclaration)
                    {
                        groups[groupIndex] = groups[groupIndex].Append(statements[index]);
                        index++;
                    }

                    groupIndex++;
                }
            }
            else
            {
                index++;
                if (groupIndex < groups.Length && groups[groupIndex].Length > 0)
                    groupIndex++;
            }
        }

        Slice<Slice<Node>> result = default;
        foreach (var group in groups)
            result = result.Append(groupByNewlineContiguous(file, group));
        return result;
    }

    private static void organizeExportsWorker(Slice<Node> oldExports, OrganizeImportsComparerSettings comparer, SourceFile file, Tracker tracker)
    {
        if (oldExports.Length == 0)
            return;
        var specifierComparer = LsUtil.GetNamedImportSpecifierComparer(new UserPreferences { OrganizeImportsTypeOrder = comparer.TypeOrder }, comparer.NameComparer);
        var exports = coalesceExportsWorker(oldExports, specifierComparer, comparer.ModuleComparer!, file, tracker);
        if (oldExports.Length > 0)
        {
            if (exports.Length == 0)
                tracker.DeleteNodeRange(file, oldExports[0], oldExports[oldExports.Length - 1], LeadingTriviaOption.Exclude, TrailingTriviaOption.Include);
            else
            {
                foreach (var export in exports)
                    tracker.EmitContext.AddEmitFlags(export, EmitFlags.NoLeadingComments);
                var options = new NodeOptions
                {
                    LeadingTriviaOption = LeadingTriviaOption.Exclude,
                    TrailingTriviaOption = TrailingTriviaOption.Include,
                    Suffix = ByteStringValue.FromString("\n")
                };
                var nodes = Core.MapSlice(exports, static node => node);
                tracker.ReplaceNodeWithNodes(file, oldExports[0], nodes, options);
                for (int i = 1; i < oldExports.Length; i++)
                    tracker.Delete(file, oldExports[i]);
            }
        }
    }

    private static Slice<Node> coalesceExportsWorker(Slice<Node> exports, Func<Node, Node, int> specifierComparer, Func<string, string, int> moduleComparer, SourceFile? file, Tracker tracker)
    {
        if (exports.Length == 0)
            return exports;
        var groups = new Dictionary<ByteStringKey, Slice<Node>>();
        Slice<ByteStringKey> order = default;
        foreach (var export in exports)
        {
            var declaration = export.AsExportDeclaration();
            var name = declaration.ModuleSpecifier?.TextValue() ?? default;
            var key = ByteStringKey.FromValue(name);
            if (!groups.ContainsKey(key))
                order = order.Append(key);
            groups[key] = groups.GetValueOrDefault(key).Append(export);
        }

        GoSliceSort.SortStableFunc(order.AsSpan(), (a, b) =>
        {
            if (a.Value.Length == 0 && b.Value.Length != 0)
                return 1;
            if (a.Value.Length != 0 && b.Value.Length == 0)
                return -1;
            return moduleComparer(a.Value.Materialize()!, b.Value.Materialize()!);
        });
        Slice<Node> result = default;
        var factory = new NodeFactory(default);
        foreach (var key in order)
        {
            var categorized = getCategorizedExports(groups[key]);
            if (categorized.ExportWithoutClause is not null)
                result = result.Append(categorized.ExportWithoutClause);
            for (int i = 0; i < 2; i++)
            {
                var subgroup = i == 0 ? categorized.NamedExports : categorized.TypeOnlyExports;
                if (subgroup.Length == 0)
                    continue;
                Slice<Node> specifiers = default;
                foreach (var export in subgroup)
                {
                    var clause = export.AsExportDeclaration().ExportClause;
                    if (clause is not null && clause.Kind == Kind.NamedExports)
                        specifiers = specifiers.Append(clause.AsNamedExports().Elements.Nodes);
                }

                GoSliceSort.SortStableFunc(specifiers.AsSpan(), (left, right) => specifierComparer(left, right));
                var declaration = subgroup[0].AsExportDeclaration();
                Node? updatedClause = null;
                if (declaration.ExportClause is not null)
                {
                    if (declaration.ExportClause.Kind == Kind.NamedExports)
                    {
                        var named = declaration.ExportClause.AsNamedExports();
                        updatedClause = factory.UpdateNamedExports(named, factory.NewNodeList(specifiers));
                        if (file is not null && !Ast.NodeIsSynthesized(named) && !PrinterUtilities.RangeIsOnSingleLine(named.Loc, file))
                            tracker.EmitContext.SetEmitFlags(updatedClause, EmitFlags.MultiLine);
                    }
                    else
                        updatedClause = declaration.ExportClause;
                }

                result = result.Append(factory.UpdateExportDeclaration(declaration, declaration.Modifiers(), declaration.IsTypeOnly, updatedClause, declaration.ModuleSpecifier, declaration.Attributes));
            }
        }

        return result;
    }

    private struct CategorizedExports
    {
        internal Node? ExportWithoutClause;
        internal Slice<Node> NamedExports, TypeOnlyExports;
    }

    private static CategorizedExports getCategorizedExports(Slice<Node> exports)
    {
        CategorizedExports result = default;
        foreach (var export in exports)
        {
            var declaration = export.AsExportDeclaration();
            if (declaration.ExportClause is null)
                result.ExportWithoutClause ??= export;
            else if (declaration.IsTypeOnly)
                result.TypeOnlyExports = result.TypeOnlyExports.Append(export);
            else
                result.NamedExports = result.NamedExports.Append(export);
        }

        return result;
    }
}
