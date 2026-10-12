// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/import_adder.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.Change;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using CompilerProgram = TypeScript.Compiler.Program;
using Locales = TypeScript.Locale.Locales;
using BuilderFlags = TypeScript.NodeBuilding.Flags;

namespace TypeScript.LanguageServices.AutoImport;
internal interface IImportAdder
{
    bool HasFixes();
    void AddImportFromExportedSymbol(Symbol symbol, bool isValidTypeOnlyUseSite);
    void AddImportFix(Fix fix);
    Slice<TextEdit> Edits();
}

internal sealed class AddToExistingState
{
    internal Node importClauseOrBindingPattern = null!;
    internal NewImportBinding? defaultImport;
    internal Dictionary<ByteStringKey, NewImportBinding> namedImports = null!;
}

internal sealed class ImportsCollection
{
    internal NewImportBinding? defaultImport;
    internal Dictionary<ByteStringKey, NewImportBinding>? namedImports;
    internal NewImportBinding? namespaceLikeImport;
    internal bool useRequire;
}

internal sealed partial class ImportAdder : IImportAdder
{
    private ProtocolRequestContext ctx = null!;
    private Checker checker = null!;
    private View view = null!;
    private FormatCodeSettings formatOptions;
    private Converters converters = null!;
    private UserPreferences preferences;
    private Slice<Fix> addToNamespace;
    private Slice<Fix> importType;
    private Dictionary<Node, AddToExistingState> addToExisting = null!;
    private Dictionary<ByteStringKey, ImportsCollection> newImports = null!;
    // import_adder.go:66. Program/file are retained parameters although the Go owner does not store them.
    internal static IImportAdder NewImportAdder(ProtocolRequestContext context, CompilerProgram program, Checker checker, SourceFile file, View view, FormatCodeSettings formatOptions, Converters converters, UserPreferences preferences) => new ImportAdder
    {
        ctx = context,
        checker = checker,
        view = view,
        formatOptions = formatOptions,
        converters = converters,
        preferences = preferences,
        addToExisting = new(),
        newImports = new()
    };
    public bool HasFixes() => addToNamespace.Count > 0 || importType.Count > 0 || addToExisting.Count > 0 || newImports.Count > 0;
    public void AddImportFromExportedSymbol(Symbol exportedSymbol, bool isValidTypeOnlyUseSite)
    {
        var symbol = checker.GetMergedSymbol(checker.SkipAlias(exportedSymbol));
        var exports = GetAllExportsForSymbol(symbol);
        if (exports.Count == 0)
            return;
        var fix = GetImportFixForSymbol(view, exports, isValidTypeOnlyUseSite);
        if (fix != null)
            AddImportFix(fix);
    }

    // import_adder.go:115. The actual tracker drops unmappable files rather than corrupting edits.
    public Slice<TextEdit> Edits()
    {
        var tracker = Tracker.NewTracker(ctx, view.program.Options(), formatOptions, converters);
        var quotePreference = LsUtil.GetQuotePreference(view.importingFile, preferences);
        foreach (var fix in addToNamespace)
            AutoImport.AddNamespaceQualifier(fix, tracker, view.importingFile, Locales.Default);
        foreach (var fix in importType)
            AutoImport.AddImportType(fix, view.importingFile, preferences, tracker, Locales.Default);
        foreach (var(clause, entry)in addToExisting)
            AutoImport.AddToExistingImport(tracker, view.importingFile, clause, entry.defaultImport, SortedNamedImports(entry.namedImports), preferences);
        Slice<Node> declarations = default;
        foreach (var(key, entry)in newImports)
        {
            var moduleSpecifier = key.Value.Slice(2);
            var next = entry.useRequire ? AutoImport.GetNewRequires(tracker, moduleSpecifier, quotePreference, entry.defaultImport, SortedNamedImports(entry.namedImports), entry.namespaceLikeImport, view.program.Options()) : AutoImport.GetNewImports(tracker, moduleSpecifier, quotePreference, entry.defaultImport, SortedNamedImports(entry.namedImports), entry.namespaceLikeImport, view.program.Options(), preferences);
            declarations = declarations.Append(next);
        }

        if (declarations.Count > 0)
            AutoImport.InsertImports(tracker, view.importingFile, declarations, true, preferences);
        var(changes, _) = tracker.GetChanges();
        return changes.TryGetValue(view.importingFile.OriginalFileName(), out var edits) ? edits : default;
    }

    // import_adder.go:179. Sorted keys and result are two required, separately owned Go slices.
    private static Slice<NewImportBinding> SortedNamedImports(Dictionary<ByteStringKey, NewImportBinding>? map)
    {
        Slice<ByteStringKey> keys = default;
        if (map != null)
            foreach (var key in map.Keys)
                keys = keys.Append(key);
        GoSlices.SortFunc(keys, static (a, b) => a.CompareTo(b));
        var result = Slice<NewImportBinding>.Make(0, keys.Count);
        foreach (var key in keys)
            result = result.Append(map![key]);
        return result;
    }

    // import_adder.go:323
    private static AddAsTypeOnly ReduceAddAsTypeOnlyValues(AddAsTypeOnly previous, AddAsTypeOnly next) => next > previous ? next : previous;
    // import_adder.go:336. Keep the original unconditional new-entry allocation before cache selection.
    private ImportsCollection GetNewImportEntry(ByteStringValue moduleSpecifier, ImportKind kind, bool useRequire, AddAsTypeOnly typeOnly)
    {
        var typeKey = NewImportsKey(moduleSpecifier, true);
        var valueKey = NewImportsKey(moduleSpecifier, false);
        newImports.TryGetValue(typeKey, out var typeEntry);
        newImports.TryGetValue(valueKey, out var valueEntry);
        var newEntry = new ImportsCollection
        {
            useRequire = useRequire
        };
        if (kind == ImportKind.Default && typeOnly == AddAsTypeOnly.Required)
        {
            if (typeEntry != null)
                return typeEntry;
            newImports[typeKey] = newEntry;
            return newEntry;
        }

        if (typeOnly == AddAsTypeOnly.Allowed && (typeEntry != null || valueEntry != null))
            return typeEntry ?? valueEntry!;
        if (valueEntry != null)
            return valueEntry;
        newImports[valueKey] = newEntry;
        return newEntry;
    }

    private static ByteStringKey NewImportsKey(ByteStringValue specifier, bool topLevelTypeOnly) => ByteStringKey.FromValue(ByteStringValue.Concat(ByteStringValue.FromString(topLevelTypeOnly ? "1|" : "0|"), specifier));
    private Slice<Export> GetAllExportsForSymbol(Symbol symbol)
    {
        var export = AutoImport.SymbolToExport(symbol, checker);
        return export != null ? view.SearchByExportID(export.ExportID) : default;
    }

    // import_adder.go:491
    private Fix? GetImportFixForSymbol(View owner, Slice<Export> exports, bool validTypeOnlyUseSite)
    {
        var fixes = Core.FlatMap(exports, (owner, validTypeOnlyUseSite), static (export, state) => state.owner.GetFixes(export, false, state.validTypeOnlyUseSite, null));
        GoSlices.SortFunc(fixes, owner.CompareFixesForRanking);
        return fixes.Count > 0 ? fixes[0] : null;
    }
}

// Go package-level factory; the private owner initializer above retains its complete field initialization.
internal static partial class AutoImport
{
    internal static IImportAdder NewImportAdder(ProtocolRequestContext context, CompilerProgram program, Checker checker, SourceFile file, View view, FormatCodeSettings formatOptions, Converters converters, UserPreferences preferences) => ImportAdder.NewImportAdder(context, program, checker, file, view, formatOptions, converters, preferences);
}

internal sealed partial class ImportAdder
{
    // import_adder.go:191
    public void AddImportFix(Fix fix)
    {
        var name = fix.Name;
        var key = ByteStringKey.FromValue(name);
        var options = view.program.Options();
        switch (fix.Kind)
        {
            case AutoImportFixKind.UseNamespace:
                addToNamespace = addToNamespace.Append(fix);
                break;
            case AutoImportFixKind.JsdocTypeImport:
                importType = importType.Append(fix);
                break;
            case AutoImportFixKind.AddToExisting:
            {
                var existing = AutoImport.GetAddToExistingImportFix(view.importingFile, fix);
                if (!addToExisting.TryGetValue(existing.importClauseOrBindingPattern, out var entry))
                {
                    entry = new AddToExistingState
                    {
                        importClauseOrBindingPattern = existing.importClauseOrBindingPattern,
                        namedImports = new()
                    };
                    addToExisting[existing.importClauseOrBindingPattern] = entry;
                }

                if (fix.ImportKind == ImportKind.Named)
                {
                    entry.namedImports.TryGetValue(key, out var previous);
                    entry.namedImports[key] = new NewImportBinding
                    {
                        kind = ImportKind.Named,
                        name = name,
                        addAsTypeOnly = ReduceAddAsTypeOnlyValues(previous?.addAsTypeOnly ?? default, fix.AddAsTypeOnly),
                        propertyName = existing.namedImport!.propertyName
                    };
                }
                else
                {
                    Debug.Assert(entry.defaultImport == null || entry.defaultImport.name.EqualsValue(name), "(Add to Existing) Default import should be missing or match symbolName");
                    entry.defaultImport = new NewImportBinding
                    {
                        kind = ImportKind.Default,
                        name = name,
                        addAsTypeOnly = ReduceAddAsTypeOnlyValues(entry.defaultImport?.addAsTypeOnly ?? default, fix.AddAsTypeOnly)
                    };
                }

                break;
            }

            case AutoImportFixKind.AddNew:
            {
                var entry = GetNewImportEntry(fix.ModuleSpecifier, fix.ImportKind, fix.UseRequire, fix.AddAsTypeOnly);
                Debug.Assert(entry.useRequire == fix.UseRequire, "(Add new) Tried to add an `import` and a `require` for the same module");
                switch (fix.ImportKind)
                {
                    case ImportKind.Default:
                        Debug.Assert(entry.defaultImport == null || entry.defaultImport.name.EqualsValue(name), "(Add new) Default import should be missing or match symbolName");
                        entry.defaultImport = new NewImportBinding
                        {
                            kind = ImportKind.Default,
                            name = name,
                            addAsTypeOnly = ReduceAddAsTypeOnlyValues(entry.defaultImport?.addAsTypeOnly ?? default, fix.AddAsTypeOnly)
                        };
                        break;
                    case ImportKind.Named:
                        entry.namedImports ??= new();
                        entry.namedImports.TryGetValue(key, out var previousNamed);
                        entry.namedImports[key] = new NewImportBinding
                        {
                            kind = ImportKind.Named,
                            name = name,
                            addAsTypeOnly = ReduceAddAsTypeOnlyValues(previousNamed?.addAsTypeOnly ?? default, fix.AddAsTypeOnly)
                        // Upstream propertyName remains unspecified here.
                        };
                        break;
                    case ImportKind.CommonJS:
                        if (options.VerbatimModuleSyntax == Tristate.True)
                        {
                            entry.namedImports ??= new();
                            entry.namedImports.TryGetValue(key, out var previousCommonJS);
                            entry.namedImports[key] = new NewImportBinding
                            {
                                kind = ImportKind.CommonJS,
                                name = name,
                                addAsTypeOnly = ReduceAddAsTypeOnlyValues(previousCommonJS?.addAsTypeOnly ?? default, fix.AddAsTypeOnly)
                            };
                        }
                        else
                        {
                            Debug.Assert(entry.namespaceLikeImport == null || entry.namespaceLikeImport.name.EqualsValue(name), "Namespacelike import should be missing or match symbolName");
                            entry.namespaceLikeImport = new NewImportBinding
                            {
                                kind = ImportKind.CommonJS,
                                name = name,
                                addAsTypeOnly = fix.AddAsTypeOnly
                            };
                        }

                        break;
                    case ImportKind.Namespace:
                        Debug.Assert(entry.namespaceLikeImport == null || entry.namespaceLikeImport.name.EqualsValue(name), "Namespacelike import should be missing or match symbolName");
                        entry.namespaceLikeImport = new NewImportBinding
                        {
                            kind = ImportKind.Namespace,
                            name = name,
                            addAsTypeOnly = fix.AddAsTypeOnly
                        };
                        break;
                }

                break;
            }

            case AutoImportFixKind.PromoteTypeOnly:
                break; // Upstream excludes this case from fix-all.
            default:
                Debug.Fail($"Unexpected fix kind: {fix.Kind}");
                break;
        }
    }
}

internal static partial class AutoImport
{
    // import_adder.go:382
    internal static Node? TypeToAutoImportableTypeNode(Checker checker, IImportAdder? adder, TypeScript.Checking.Type type, Node context)
    {
        var symbols = new Dictionary<Node, Symbol>();
        var node = checker.TypeToTypeNode(type, context, BuilderFlags.None, symbols);
        return node == null ? null : TypeNodeToAutoImportableTypeNode(node, adder, symbols);
    }

    internal static Node TypeNodeToAutoImportableTypeNode(Node node, IImportAdder? adder, Dictionary<Node, Symbol>? symbols)
    {
        var(reference, imports) = TryGetAutoImportableReferenceFromTypeNode(node, symbols);
        if (reference != null)
        {
            if (adder != null)
                ImportSymbols(adder, imports);
            node = reference;
        }

        // Original TODO: fresh node reuse must also preserve symbols.
        return node;
    }

    private static void ImportSymbols(IImportAdder adder, Slice<Symbol> symbols)
    {
        foreach (var symbol in symbols)
            adder.AddImportFromExportedSymbol(symbol, true);
    }

    // import_adder.go:427. One visitor/factory and recursive callback owner per query, as in Go.
    internal static (Node? Node, Slice<Symbol> Symbols) TryGetAutoImportableReferenceFromTypeNode(Node node, Dictionary<Node, Symbol>? idToSymbol)
    {
        Slice<Symbol> symbols = default;
        NodeVisitor visitor = null!;
        var factory = Ast.NewNodeFactory(default);
        Node Visit(Node current)
        {
            if (Ast.IsLiteralImportTypeNode(current) && current.AsImportTypeNode().Qualifier != null)
            {
                var import = current.AsImportTypeNode();
                var firstIdentifier = Ast.GetFirstIdentifier(import.Qualifier);
                Symbol? symbol = null;
                idToSymbol?.TryGetValue(firstIdentifier, out symbol);
                if (symbol == null)
                    return current.VisitEachChild(visitor);
                var name = GetNameForExportedSymbol(symbol, false);
                var qualifier = name.EqualsValue(firstIdentifier.TextValue()) ? import.Qualifier : ReplaceFirstIdentifierOfEntityName(factory, import.Qualifier, factory.NewIdentifierWithTextValue(name));
                symbols = symbols.Append(symbol);
                var arguments = visitor.VisitNodes(import.TypeArguments);
                return factory.NewTypeReferenceNode(qualifier, arguments);
            }

            return visitor.VisitEachChild(current);
        }

        visitor = Ast.NewNodeVisitor(Visit, factory, default);
        var result = visitor.VisitNode(node);
        Debug.Assert(result == null || Ast.IsTypeNode(result), "expected a type node");
        return (result, symbols);
    }

    // import_adder.go:466
    private static ByteStringValue GetNameForExportedSymbol(Symbol symbol, bool preferCapitalized)
    {
        if (symbol.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameExportEquals)) || symbol.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameDefault)))
        {
            var name = GetDefaultLikeExportNameFromDeclaration(symbol);
            if (name.Length != 0)
                return name;
            Debug.Assert(symbol.Parent != null, "Expected exported symbol to have module symbol as parent");
            return LsUtil.ModuleSymbolToValidIdentifierValue(symbol.Parent, preferCapitalized);
        }

        return symbol.NameValue;
    }

    private static Node ReplaceFirstIdentifierOfEntityName(NodeFactory factory, Node name, Node identifier)
    {
        if (name.Kind == Kind.Identifier)
            return identifier;
        return factory.NewQualifiedName(ReplaceFirstIdentifierOfEntityName(factory, name.AsQualifiedName().Left, identifier), name.AsQualifiedName().Right);
    }
}
