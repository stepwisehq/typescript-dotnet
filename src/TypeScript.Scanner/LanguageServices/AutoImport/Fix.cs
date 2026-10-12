// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/fix.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.ModuleSpecifiers;
using TypeScript.LanguageServices.Change;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.Lsconv;
using LocaleValue = TypeScript.Locale.Locale;
using TypeScript.Collections;
using CompilerProgram = TypeScript.Compiler.Program;
using TypeScript.Checking;
using UserPreferences = TypeScript.LanguageServices.Utilities.UserPreferences;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.AutoImport;
internal sealed class NewImportBinding
{
    internal ImportKind kind;
    internal ByteStringValue propertyName;
    internal ByteStringValue name;
    internal AddAsTypeOnly addAsTypeOnly;
}

internal sealed partial class Fix
{
    // Go embeds *lsproto.AutoImportFix: retain that exact stable DTO owner, not a copied payload.
    // Constructors/callers set the owner explicitly; no discarded default allocation.
    internal AutoImportFix AutoImportFix = null!;
    internal AutoImportFixKind Kind { get => AutoImportFix.Kind; set => AutoImportFix.Kind = value; }
    internal ByteStringValue Name { get => AutoImportFix.Name; set => AutoImportFix.Name = value; }
    internal ImportKind ImportKind { get => AutoImportFix.ImportKind; set => AutoImportFix.ImportKind = value; }
    internal bool UseRequire { get => AutoImportFix.UseRequire; set => AutoImportFix.UseRequire = value; }
    internal AddAsTypeOnly AddAsTypeOnly { get => AutoImportFix.AddAsTypeOnly; set => AutoImportFix.AddAsTypeOnly = value; }
    internal ByteStringValue ModuleSpecifier { get => AutoImportFix.ModuleSpecifier; set => AutoImportFix.ModuleSpecifier = value; }
    internal int ImportIndex { get => AutoImportFix.ImportIndex; set => AutoImportFix.ImportIndex = value; }
    internal Position? UsagePosition { get => AutoImportFix.UsagePosition; set => AutoImportFix.UsagePosition = value; }
    internal ByteStringValue NamespacePrefix { get => AutoImportFix.NamespacePrefix; set => AutoImportFix.NamespacePrefix = value; }

    internal ResultKind ModuleSpecifierKind;
    internal bool IsReExport;
    internal RootedFilePath ModuleFileName;
    internal Node? TypeOnlyAliasDeclaration;
}

internal sealed class AddToExistingImportFix
{
    internal Node importClauseOrBindingPattern = null!;
    internal NewImportBinding? defaultImport;
    internal NewImportBinding? namedImport;
}

internal struct ExistingImport
{
    internal Node node;
    internal ByteStringValue moduleSpecifier;
    internal int index;
}

internal sealed partial class Fix
{
    // fix.go:56. Description stays a raw Go byte header until the protocol consumer.
    internal (Slice<TextEdit> Edits, ByteStringValue Description, bool Safe) Edits(ProtocolRequestContext context, SourceFile file, CompilerOptions options, FormatCodeSettings formatOptions, Converters converters, UserPreferences preferences)
    {
        var locale = context.Locale;
        var tracker = Tracker.NewTracker(context, options, formatOptions, converters);
        switch (Kind)
        {
            case AutoImportFixKind.UseNamespace:
            {
                var description = AutoImport.AddNamespaceQualifier(this, tracker, file, locale);
                var(edits, safe) = AutoImport.FileEdits(tracker, file);
                return (edits, description, safe);
            }

            case AutoImportFixKind.AddToExisting:
            {
                if (file.Imports().Count <= ImportIndex)
                    throw new InvalidOperationException("import index out of range");
                var fix = AutoImport.GetAddToExistingImportFix(file, this);
                // SingleElementSlice(nil) is nil in the pinned worker.
                var namedImports = fix.namedImport == null ? default : Slice<NewImportBinding>.FromArray([fix.namedImport]);
                AutoImport.AddToExistingImport(tracker, file, fix.importClauseOrBindingPattern, fix.defaultImport, namedImports, preferences);
                var(edits, safe) = AutoImport.FileEdits(tracker, file);
                return (edits, ByteStringValue.FromString(Diagnostics.Update_import_from_0.Localize(locale, ModuleSpecifier)), safe);
            }

            case AutoImportFixKind.AddNew:
            {
                var defaultImport = ImportKind == ImportKind.Default ? new NewImportBinding
                {
                    name = Name,
                    addAsTypeOnly = AddAsTypeOnly
                }

                : null;
                var namedImports = ImportKind == ImportKind.Named ? Slice<NewImportBinding>.FromArray([new NewImportBinding { name = Name, addAsTypeOnly = AddAsTypeOnly }]) : default;
                NewImportBinding? namespaceLikeImport = ImportKind is ImportKind.Namespace or ImportKind.CommonJS ? new()
                {
                    kind = ImportKind,
                    name = Name
                }

                : null;
                var quotePreference = LsUtil.GetQuotePreference(file, preferences);
                var declarations = UseRequire ? AutoImport.GetNewRequires(tracker, ModuleSpecifier, quotePreference, defaultImport, namedImports, namespaceLikeImport, options) : AutoImport.GetNewImports(tracker, ModuleSpecifier, quotePreference, defaultImport, namedImports, namespaceLikeImport, options, preferences);
                AutoImport.InsertImports(tracker, file, declarations, true, preferences);
                var(edits, safe) = AutoImport.FileEdits(tracker, file);
                return (edits, ByteStringValue.FromString(Diagnostics.Add_import_from_0.Localize(locale, ModuleSpecifier)), safe);
            }

            case AutoImportFixKind.PromoteTypeOnly:
            {
                var declaration = AutoImport.PromoteFromTypeOnly(tracker, TypeOnlyAliasDeclaration!, options, file, preferences);
                if (declaration.Kind == TypeScript.Scanner.Kind.ImportSpecifier)
                {
                    var specifier = AutoImport.GetModuleSpecifierText(declaration.Parent.Parent);
                    var(edits, safe) = AutoImport.FileEdits(tracker, file);
                    return (edits, ByteStringValue.FromString(Diagnostics.Remove_type_from_import_of_0_from_1.Localize(locale, Name, specifier)), safe);
                }
                else
                {
                    var specifier = AutoImport.GetModuleSpecifierText(declaration);
                    var(edits, safe) = AutoImport.FileEdits(tracker, file);
                    return (edits, ByteStringValue.FromString(Diagnostics.Remove_type_from_import_declaration_from_0.Localize(locale, specifier)), safe);
                }
            }

            case AutoImportFixKind.JsdocTypeImport:
            {
                var description = AutoImport.AddImportType(this, file, preferences, tracker, locale);
                var(edits, safe) = AutoImport.FileEdits(tracker, file);
                return (edits, description, safe);
            }

            default:
                throw new InvalidOperationException("unimplemented fix edit");
        }
    }
}

internal static partial class AutoImport
{
    // fix.go:133. Missing file key yields a nil slice, not a dictionary exception.
    internal static (Slice<TextEdit> Edits, bool Safe) FileEdits(Tracker tracker, SourceFile file)
    {
        var(changes, unmappable) = tracker.GetChanges();
        changes.TryGetValue(file.OriginalFileName(), out var edits);
        return (edits, unmappable.Count == 0);
    }

    // fix.go:138
    internal static ByteStringValue AddImportType(Fix fix, SourceFile file, UserPreferences preferences, Tracker tracker, LocaleValue locale)
    {
        if (fix.UsagePosition == null)
            throw new InvalidOperationException("UsagePosition must be set for JSDoc type import fix");
        string quote = LsUtil.GetQuotePreference(file, preferences) == QuotePreference.Single ? "'" : "\"";
        var prefix = ByteStringValue.Concat(ByteStringValue.FromString("import(" + quote), fix.ModuleSpecifier, ByteStringValue.FromString(quote + ")."));
        tracker.InsertText(file, fix.UsagePosition.Value, prefix);
        return ByteStringValue.FromString(Diagnostics.Change_0_to_1.Localize(locale, fix.Name, ByteStringValue.Concat(prefix, fix.Name)));
    }

    // fix.go:152
    internal static ByteStringValue AddNamespaceQualifier(Fix fix, Tracker tracker, SourceFile file, LocaleValue locale)
    {
        if (fix.UsagePosition == null || fix.NamespacePrefix.Length == 0)
            throw new InvalidOperationException("namespace fix requires usage position and prefix");
        var prefix = ByteStringValue.Concat(fix.NamespacePrefix, ByteStringValue.FromString("."));
        var qualified = ByteStringValue.Concat(prefix, fix.Name);
        tracker.InsertText(file, fix.UsagePosition.Value, prefix);
        return ByteStringValue.FromString(Diagnostics.Change_0_to_1.Localize(locale, fix.Name, qualified));
    }

    // fix.go:161
    internal static AddToExistingImportFix GetAddToExistingImportFix(SourceFile file, Fix fix)
    {
        if (fix.Kind != AutoImportFixKind.AddToExisting)
            throw new InvalidOperationException("expected add to existing import fix");
        var specifier = file.Imports()[fix.ImportIndex];
        var importNode = Ast.TryGetImportFromModuleSpecifier(specifier);
        if (importNode == null)
            throw new InvalidOperationException("expected import declaration");
        Node clauseOrPattern;
        switch (importNode.Kind)
        {
            case Kind.ImportDeclaration:
                clauseOrPattern = importNode.ImportClause();
                if (clauseOrPattern == null)
                    throw new InvalidOperationException("expected import clause");
                break;
            case Kind.CallExpression:
                if (!Ast.IsVariableDeclarationInitializedToRequire(importNode.Parent))
                    throw new InvalidOperationException("expected require call expression to be in variable declaration");
                clauseOrPattern = importNode.Parent.Name();
                if (clauseOrPattern == null || !Ast.IsObjectBindingPattern(clauseOrPattern))
                    throw new InvalidOperationException("expected object binding pattern in variable declaration");
                break;
            default:
                throw new InvalidOperationException("expected import declaration or require call expression");
        }

        return new()
        {
            importClauseOrBindingPattern = clauseOrPattern,
            defaultImport = fix.ImportKind == ImportKind.Default ? new()
            {
                kind = ImportKind.Default,
                name = fix.Name,
                addAsTypeOnly = fix.AddAsTypeOnly
            }

            : null,
            namedImport = fix.ImportKind == ImportKind.Named ? new()
            {
                kind = ImportKind.Named,
                name = fix.Name,
                addAsTypeOnly = fix.AddAsTypeOnly
            }

            : null
        };
    }

    // fix.go:198
    internal static void AddToExistingImport(Tracker tracker, SourceFile file, Node clauseOrPattern, NewImportBinding? defaultImport, Slice<NewImportBinding> namedImports, UserPreferences preferences)
    {
        switch (clauseOrPattern.Kind)
        {
            case Kind.ObjectBindingPattern:
                var pattern = clauseOrPattern.AsBindingPattern();
                if (defaultImport != null)
                    AddElementToBindingPattern(tracker, file, pattern, defaultImport.name, ByteStringValue.FromString("default"));
                foreach (var namedImport in namedImports)
                    AddElementToBindingPattern(tracker, file, pattern, namedImport.name, default);
                return;
            case Kind.ImportClause:
            {
                var clause = clauseOrPattern.AsImportClause();
                // Preserve the actual append owner/capacity, including its nil final item.
                bool promote = clause.IsTypeOnly() && Core.Some(namedImports.Append(defaultImport!), static import => import != null && import.addAsTypeOnly == AddAsTypeOnly.NotAllowed);
                Slice<Node> existingSpecifiers = default;
                if (clause.NamedBindings is { Kind: Kind.NamedImports } bindings)
                    existingSpecifiers = bindings.Elements();
                if (defaultImport != null)
                {
                    Debug.Assert(clause.Name() == null, "Cannot add a default import to an import clause that already has one");
                    tracker.InsertNodeAt(file, TypeScript.AstNavigation.AstNav.GetStartOfNode(clause.AsNode(), file, false), tracker.NodeFactory.NewIdentifierWithTextValue(defaultImport.name), new() { Suffix = ByteStringValue.FromString(", ") });
                }

                if (namedImports.Count > 0)
                {
                    var(comparer, isSorted) = LsUtil.GetNamedImportSpecifierComparerWithDetection(clause.Parent, file, preferences);
                    var newSpecifiers = Core.MapSlice(namedImports, import => tracker.NodeFactory.NewImportSpecifier((!clause.IsTypeOnly() || promote) && ShouldUseTypeOnly(import.addAsTypeOnly, preferences), import.propertyName.Length == 0 ? null : tracker.NodeFactory.NewIdentifierWithTextValue(import.propertyName), tracker.NodeFactory.NewIdentifierWithTextValue(import.name)));
                    GoSlices.SortFunc(newSpecifiers, (a, b) => comparer(a, b));
                    if (existingSpecifiers.Count > 0 && isSorted != Tristate.False)
                    {
                        var comparisonSpecifiers = existingSpecifiers;
                        if (promote)
                            comparisonSpecifiers = Core.MapSlice(existingSpecifiers, specifier => tracker.NodeFactory.NewImportSpecifier(true, specifier.AsImportSpecifier().PropertyName, specifier.AsImportSpecifier().Name()));
                        foreach (var specifier in newSpecifiers)
                            tracker.InsertImportSpecifierAtIndex(file, specifier, clause.NamedBindings, LsUtil.GetImportSpecifierInsertionIndex(comparisonSpecifiers, specifier, comparer));
                    }
                    else if (existingSpecifiers.Count > 0)
                        foreach (var specifier in newSpecifiers)
                            tracker.InsertNodeInListAfter(file, existingSpecifiers[existingSpecifiers.Count - 1], specifier, null);
                    else if (newSpecifiers.Count > 0)
                    {
                        var newBindings = tracker.NodeFactory.NewNamedImports(tracker.NodeFactory.NewNodeList(newSpecifiers));
                        if (clause.NamedBindings != null)
                            tracker.ReplaceNode(file, clause.NamedBindings, newBindings, null);
                        else
                        {
                            if (clause.Name() == null)
                                throw new InvalidOperationException("Import clause must have either named imports or a default import");
                            tracker.InsertNodeAfter(file, clause.Name(), newBindings);
                        }
                    }
                }

                if (promote)
                {
                    tracker.Delete(file, GetTypeKeywordOfTypeOnlyImport(clause, file));
                    foreach (var specifier in existingSpecifiers)
                        if (!specifier.AsImportSpecifier().IsTypeOnly)
                            tracker.InsertModifierBefore(file, Kind.TypeKeyword, specifier);
                }

                return;
            }

            default:
                throw new InvalidOperationException("Unsupported clause kind: " + clauseOrPattern.Kind.String() + " for addToExistingImport");
        }
    }

    // fix.go:321
    internal static Node GetTypeKeywordOfTypeOnlyImport(ImportClause clause, SourceFile file)
    {
        Debug.Assert(clause.IsTypeOnly(), "import clause must be type-only");
        var keyword = TypeScript.AstNavigation.AstNav.FindChildOfKind(clause.AsNode(), Kind.TypeKeyword, file);
        Debug.Assert(keyword != null, "type-only import clause should have a type keyword");
        return keyword!;
    }

    // fix.go:331. Retain original initializer position for the optional propertyName.
    internal static void AddElementToBindingPattern(Tracker tracker, SourceFile file, BindingPattern pattern, ByteStringValue name, ByteStringValue propertyName)
    {
        var element = tracker.NodeFactory.NewBindingElement(null, null, tracker.NodeFactory.NewIdentifierWithTextValue(name), propertyName.Length == 0 ? null : tracker.NodeFactory.NewIdentifierWithTextValue(propertyName));
        if (pattern.Elements.Nodes.Count > 0)
            tracker.InsertNodeInListAfter(file, pattern.Elements.Nodes[pattern.Elements.Nodes.Count - 1], element, pattern.Elements);
        else
            tracker.ReplaceNode(file, pattern.AsNode(), tracker.NodeFactory.NewBindingPattern(Kind.ObjectBindingPattern, tracker.AsNodeFactory().NewNodeList(Slice<Node>.FromArray([element]))), null);
    }
}

internal static partial class AutoImport
{
    // fix.go:346
    internal static Slice<Node> GetNewImports(Tracker tracker, ByteStringValue moduleSpecifier, QuotePreference quotePreference, NewImportBinding? defaultImport, Slice<NewImportBinding> namedImports, NewImportBinding? namespaceLikeImport, CompilerOptions options, UserPreferences preferences)
    {
        var flags = quotePreference == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None;
        var literal = tracker.NodeFactory.NewStringLiteralWithTextValue(moduleSpecifier, flags);
        Slice<Node> statements = default;
        if (defaultImport != null || namedImports.Count > 0)
        {
            bool topLevelTypeOnly = (defaultImport == null || NeedsTypeOnly(defaultImport.addAsTypeOnly)) && Core.Every(namedImports, static import => NeedsTypeOnly(import.addAsTypeOnly)) || (options.VerbatimModuleSyntax == Tristate.True || preferences.PreferTypeOnlyAutoImports == Tristate.True) && (defaultImport == null || defaultImport.addAsTypeOnly != AddAsTypeOnly.NotAllowed) && !Core.Some(namedImports, static import => import.addAsTypeOnly == AddAsTypeOnly.NotAllowed);
            var defaultNode = defaultImport == null ? null : tracker.NodeFactory.NewIdentifierWithTextValue(defaultImport.name);
            var specifiers = Core.MapSlice(namedImports, import => tracker.NodeFactory.NewImportSpecifier(!topLevelTypeOnly && ShouldUseTypeOnly(import.addAsTypeOnly, preferences), import.propertyName.Length == 0 ? null : tracker.NodeFactory.NewIdentifierWithTextValue(import.propertyName), tracker.NodeFactory.NewIdentifierWithTextValue(import.name)));
            statements = statements.Append(MakeImport(tracker, defaultNode, specifiers, literal, topLevelTypeOnly));
        }

        if (namespaceLikeImport != null)
        {
            Node declaration;
            if (namespaceLikeImport.kind == ImportKind.CommonJS)
                declaration = tracker.NodeFactory.NewImportEqualsDeclaration(null, ShouldUseTypeOnly(namespaceLikeImport.addAsTypeOnly, preferences), tracker.NodeFactory.NewIdentifierWithTextValue(namespaceLikeImport.name), tracker.NodeFactory.NewExternalModuleReference(literal));
            else
                declaration = tracker.NodeFactory.NewImportDeclaration(null, tracker.NodeFactory.NewImportClause(ShouldUseTypeOnly(namespaceLikeImport.addAsTypeOnly, preferences) ? Kind.TypeKeyword : Kind.Unknown, null, tracker.NodeFactory.NewNamespaceImport(tracker.NodeFactory.NewIdentifierWithTextValue(namespaceLikeImport.name))), literal, null);
            statements = statements.Append(declaration);
        }

        if (statements.Count == 0)
            throw new InvalidOperationException("No statements to insert for new imports");
        return statements;
    }

    // fix.go:415
    internal static Slice<Node> GetNewRequires(Tracker tracker, ByteStringValue moduleSpecifier, QuotePreference quotePreference, NewImportBinding? defaultImport, Slice<NewImportBinding> namedImports, NewImportBinding? namespaceLikeImport, CompilerOptions options)
    {
        var literal = tracker.NodeFactory.NewStringLiteralWithTextValue(moduleSpecifier, quotePreference == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None);
        Slice<Node> statements = default;
        if (defaultImport != null || namedImports.Count > 0)
        {
            // Original []*Node{} owns an explicit empty slice before appending.
            var elements = Slice<Node>.Make(0);
            foreach (var import in namedImports)
                elements = elements.Append(tracker.NodeFactory.NewBindingElement(null, import.propertyName.Length == 0 ? null : tracker.NodeFactory.NewIdentifierWithTextValue(import.propertyName), tracker.NodeFactory.NewIdentifierWithTextValue(import.name), null));
            if (defaultImport != null)
                elements = Slice<Node>.FromArray([tracker.NodeFactory.NewBindingElement(null, tracker.NodeFactory.NewIdentifier("default"), tracker.NodeFactory.NewIdentifierWithTextValue(defaultImport.name), null)]).Append(elements);
            statements = statements.Append(CreateConstEqualsRequireDeclaration(tracker, tracker.NodeFactory.NewBindingPattern(Kind.ObjectBindingPattern, tracker.NodeFactory.NewNodeList(elements)), literal));
        }

        if (namespaceLikeImport != null)
            statements = statements.Append(CreateConstEqualsRequireDeclaration(tracker, tracker.NodeFactory.NewIdentifierWithTextValue(namespaceLikeImport.name), literal));
        Debug.Assert(!statements.IsNil);
        return statements;
    }

    // fix.go:480
    internal static Node CreateConstEqualsRequireDeclaration(Tracker tracker, Node name, Node literal) => tracker.NodeFactory.NewVariableStatement(null, tracker.NodeFactory.NewVariableDeclarationList(tracker.NodeFactory.NewNodeList(Slice<Node>.FromArray([tracker.NodeFactory.NewVariableDeclaration(name, null, null, tracker.NodeFactory.NewCallExpression(tracker.NodeFactory.NewIdentifier("require"), null, null, tracker.NodeFactory.NewNodeList(Slice<Node>.FromArray([literal])), NodeFlags.None))])), NodeFlags.Const));
    // fix.go:503. New declarations are cloned before sorting; existing source AST is never reordered.
    internal static void InsertImports(Tracker tracker, SourceFile file, Slice<Node> imports, bool blankLineBetween, UserPreferences preferences)
    {
        var existingStatements = imports[0].Kind == Kind.VariableStatement ? Core.Filter(file.Statements.Nodes, Ast.IsRequireVariableStatement) : Core.Filter(file.Statements.Nodes, Ast.IsAnyImportSyntax);
        var(comparer, isSorted) = LsUtil.GetOrganizeImportsStringComparerWithDetection(existingStatements, preferences);
        var sorted = GoSlices.Clone(imports);
        GoSlices.SortFunc(sorted, (a, b) => LsUtil.CompareImportsOrRequireStatements(a, b, comparer));
        if (existingStatements.Count > 0 && isSorted)
            foreach (var import in sorted)
            {
                int index = LsUtil.GetImportDeclarationInsertIndex(existingStatements, import, (a, b) => LsUtil.CompareImportsOrRequireStatements(a, b, comparer));
                if (index == 0)
                {
                    var trivia = existingStatements[0] == file.Statements.Nodes[0] ? LeadingTriviaOption.Exclude : LeadingTriviaOption.None;
                    tracker.InsertNodeBefore(file, existingStatements[0], import, false, trivia);
                }
                else
                    tracker.InsertNodeAfter(file, existingStatements[index - 1], import);
            }
        else if (existingStatements.Count > 0)
            tracker.InsertNodesAfter(file, existingStatements[existingStatements.Count - 1], sorted);
        else
            tracker.InsertAtTopOfFile(file, sorted, blankLineBetween);
    }

    // fix.go:542
    internal static Node MakeImport(Tracker tracker, Node? defaultImport, Slice<Node> namedImports, Node moduleSpecifier, bool isTypeOnly)
    {
        var newNamedImports = namedImports.Count > 0 ? tracker.NodeFactory.NewNamedImports(tracker.NodeFactory.NewNodeList(namedImports)) : null;
        var clause = defaultImport != null || newNamedImports != null ? tracker.NodeFactory.NewImportClause(isTypeOnly ? Kind.TypeKeyword : Kind.Unknown, defaultImport, newNamedImports) : null;
        return tracker.NodeFactory.NewImportDeclaration(null, clause, moduleSpecifier, null);
    }
}

internal sealed partial class View
{
    // fix.go:554
    internal Slice<Fix> GetFixes(Export export, bool forJSX, bool isValidTypeOnlyUseSite, Position? usagePosition)
    {
        Slice<Fix> fixes = default;
        var namespaceFix = TryUseExistingNamespaceImport(export, usagePosition);
        if (namespaceFix != null)
            fixes = fixes.Append(namespaceFix);
        var existingFix = TryAddToExistingImport(export, isValidTypeOnlyUseSite);
        if (existingFix != null)
            return fixes.Append(existingFix);
        // !!! getNewImportFromExistingSpecifier - even worth it?
        var(specifier, specifierKind) = GetModuleSpecifier(export, preferences);
        if (specifier == default)
            return fixes.Count > 0 ? fixes : default;
        bool isJS = RootedFilePath.FromValue(importingFile.FileNameGoValue()).HasJSFileExtension();
        bool importedSymbolHasValueMeaning = (export.Flags & SymbolFlags.Value) != 0 || export.IsUnresolvedAlias();
        if (!importedSymbolHasValueMeaning && isJS && usagePosition != null)
            return Slice<Fix>.FromArray([new Fix { AutoImportFix = new() { Kind = AutoImportFixKind.JsdocTypeImport, ModuleSpecifier = specifier.AsValue(), Name = export.Name(), UsagePosition = usagePosition }, ModuleSpecifierKind = specifierKind, IsReExport = export.Target.ModuleID != export.ModuleID, ModuleFileName = export.ModuleFileName }]);
        var importKind = AutoImport.GetImportKind(importingFile, export, program, false);
        var addAsTypeOnly = AutoImport.GetAddAsTypeOnly(isValidTypeOnlyUseSite, export, program.Options());
        var name = export.Name();
        // Original code tests the first byte, rather than decoding the first UTF8 rune.
        if (forJSX && !GoUnicode.IsUpper(name[0]))
        {
            if (!export.IsRenameable())
                return default;
            var builder = new GoBuilder();
            builder.WriteRune(UnicodeCase.ToUpper(name[0]));
            builder.WriteString(name.Slice(1));
            name = builder.Value();
        }

        return fixes.Append(new Fix { AutoImportFix = new() { Kind = AutoImportFixKind.AddNew, ImportKind = importKind, ModuleSpecifier = specifier.AsValue(), Name = name, UseRequire = ShouldUseRequire(), AddAsTypeOnly = addAsTypeOnly }, ModuleSpecifierKind = specifierKind, IsReExport = export.Target.ModuleID != export.ModuleID, ModuleFileName = export.ModuleFileName });
    }

    // fix.go:636
    private Fix? TryUseExistingNamespaceImport(Export export, Position? usagePosition)
    {
        if (usagePosition == null || AutoImport.GetImportKind(importingFile, export, program, false) != ImportKind.Named)
            return null;
        foreach (var import in GetExistingImports().Get(export.ModuleID))
        {
            var namespacePrefix = AutoImport.GetNamespaceLikeImportText(import.node);
            if (namespacePrefix.Length == 0 || import.moduleSpecifier.Length == 0)
                continue;
            return new Fix
            {
                AutoImportFix = new()
                {
                    Kind = AutoImportFixKind.UseNamespace,
                    Name = export.Name(),
                    ModuleSpecifier = import.moduleSpecifier,
                    ImportKind = ImportKind.Namespace,
                    AddAsTypeOnly = AddAsTypeOnly.Allowed,
                    ImportIndex = import.index,
                    UsagePosition = usagePosition,
                    NamespacePrefix = namespacePrefix
                }
            };
        }

        return null;
    }

    // fix.go:690
    private Fix? TryAddToExistingImport(Export export, bool isValidTypeOnlyUseSite)
    {
        var matchingDeclarations = GetExistingImports().Get(export.ModuleID);
        if (matchingDeclarations.Count == 0)
            return null;
        if (Ast.IsSourceFileJS(importingFile) && (export.Flags & SymbolFlags.Value) == 0 && !Core.Every(matchingDeclarations, static import => Ast.IsJSDocImportTag(import.node)))
            return null;
        var importKind = AutoImport.GetImportKind(importingFile, export, program, false);
        if (importKind is ImportKind.CommonJS or ImportKind.Namespace)
            return null;
        var addAsTypeOnly = AutoImport.GetAddAsTypeOnly(isValidTypeOnlyUseSite, export, program.Options());
        Fix? best = null;
        foreach (var import in matchingDeclarations)
        {
            if (import.node.Kind == Kind.ImportEqualsDeclaration)
                continue;
            if (import.node.Kind == Kind.VariableDeclaration)
            {
                if ((importKind is ImportKind.Named or ImportKind.Default) && import.node.Name().Kind == Kind.ObjectBindingPattern)
                {
                    var fix = new Fix
                    {
                        AutoImportFix = new()
                        {
                            Kind = AutoImportFixKind.AddToExisting,
                            Name = export.Name(),
                            ImportKind = importKind,
                            ImportIndex = import.index,
                            ModuleSpecifier = import.moduleSpecifier,
                            AddAsTypeOnly = addAsTypeOnly
                        }
                    };
                    if (addAsTypeOnly == AddAsTypeOnly.NotAllowed)
                        return fix;
                    best ??= fix;
                }

                continue;
            }

            var clauseNode = import.node.ImportClause();
            if (clauseNode == null || !Ast.IsStringLiteralLike(import.node.ModuleSpecifier()))
                continue;
            var clause = clauseNode.AsImportClause();
            if (clause.PhaseModifier == Kind.SourceKeyword)
                continue;
            var bindings = clause.NamedBindings;
            if (clause.IsTypeOnly() && !(importKind == ImportKind.Named && bindings != null))
                continue;
            if (importKind == ImportKind.Default && (clause.Name() != null || addAsTypeOnly == AddAsTypeOnly.Required && bindings != null))
                continue;
            if (importKind == ImportKind.Named && bindings != null && bindings.Kind == Kind.NamespaceImport)
                continue;
            var existingFix = new Fix
            {
                AutoImportFix = new()
                {
                    Kind = AutoImportFixKind.AddToExisting,
                    Name = export.Name(),
                    ImportKind = importKind,
                    ImportIndex = import.index,
                    ModuleSpecifier = import.moduleSpecifier,
                    AddAsTypeOnly = addAsTypeOnly
                }
            };
            bool isTypeOnly = clause.IsTypeOnly();
            if (addAsTypeOnly != AddAsTypeOnly.NotAllowed && isTypeOnly || addAsTypeOnly == AddAsTypeOnly.NotAllowed && !isTypeOnly)
                return existingFix;
            best ??= existingFix;
        }

        return best;
    }

    // fix.go:849
    private MultiMap<ModuleID, ExistingImport> GetExistingImports()
    {
        if (existingImports != null)
            return existingImports;
        var result = TypeScript.Collections.Collections.NewMultiMapWithSizeHint<ModuleID, ExistingImport>(importingFile.Imports().Count);
        for (int i = 0; i < importingFile.Imports().Count; i++)
        {
            var specifier = importingFile.Imports()[i];
            var node = Ast.TryGetImportFromModuleSpecifier(specifier);
            if (node == null)
                throw new InvalidOperationException("error: did not expect node kind " + specifier.Kind.String());
            if (Ast.IsVariableDeclarationInitializedToRequire(node.Parent))
            {
                var symbol = checker.ResolveExternalModuleName(specifier, null);
                if (symbol != null)
                {
                    var(moduleID, _, ok) = AutoImport.TryGetModuleIDAndFileNameOfModuleSymbol(symbol);
                    if (ok)
                        result.Add(moduleID, new() { node = node.Parent, moduleSpecifier = specifier.TextValue(), index = i });
                }
            }
            else if (node.Kind is Kind.ImportDeclaration or Kind.ImportEqualsDeclaration or Kind.JSDocImportTag)
            {
                var symbol = checker.GetSymbolAtLocation(specifier);
                if (symbol != null)
                {
                    var(moduleID, _, ok) = AutoImport.TryGetModuleIDAndFileNameOfModuleSymbol(symbol);
                    if (ok)
                        result.Add(moduleID, new() { node = node, moduleSpecifier = specifier.TextValue(), index = i });
                }
            }
        }

        existingImports = result;
        return result;
    }

    // fix.go:878
    private bool ShouldUseRequire()
    {
        if (shouldUseRequireForFixes.HasValue)
            return shouldUseRequireForFixes.Value;
        bool result = ComputeShouldUseRequire();
        shouldUseRequireForFixes = result;
        return result;
    }

    // fix.go:950
    private bool ComputeShouldUseRequire()
    {
        if (!RootedFilePath.FromValue(importingFile.FileNameGoValue()).HasJSFileExtension())
            return false;
        switch (AutoImport.DetectSyntax(importingFile, program.Options()))
        {
            case FileSyntaxKind.CJS:
                return true;
            case FileSyntaxKind.ESM:
                return false;
        }

        switch (program.GetImpliedNodeFormatForEmit(importingFile))
        {
            case ResolutionMode.CommonJS:
                return true;
            case ResolutionMode.ESNext:
                return false;
        }

        if (program.Options().ConfigFilePath != default)
            return program.Options().GetEmitModuleKind() < ModuleKind.ES2015;
        foreach (var file in program.GetSourceFiles())
        {
            if (file == importingFile || !Ast.IsSourceFileJS(file) || program.IsSourceFileFromExternalLibrary(file))
                continue;
            switch (AutoImport.DetectSyntax(file, program.Options()))
            {
                case FileSyntaxKind.CJS:
                    return true;
                case FileSyntaxKind.ESM:
                    return false;
            }
        }

        return true;
    }

    // fix.go:1008
    internal int CompareFixesForSorting(Fix a, Fix b)
    {
        int result = CompareFixesForRanking(a, b);
        return result != 0 ? result : CompareModuleSpecifiersForSorting(a, b);
    }

    // fix.go:1018
    internal int CompareFixesForRanking(Fix a, Fix b)
    {
        int result = (int)a.Kind - (int)b.Kind;
        return result != 0 ? result : CompareModuleSpecifiersForRanking(a, b);
    }

    // fix.go:1029
    private int CompareModuleSpecifiersForRanking(Fix a, Fix b)
    {
        int comparison = AutoImport.CompareModuleSpecifierRelativity(a, b, preferences);
        if (comparison != 0)
            return comparison;
        if (a.ModuleSpecifierKind == ResultKind.Ambient && b.ModuleSpecifierKind == ResultKind.Ambient)
        {
            comparison = CompareNodeCoreModuleSpecifiers(a.ModuleSpecifier, b.ModuleSpecifier, importingFile, program);
            if (comparison != 0)
                return comparison;
        }

        if (a.ModuleSpecifierKind == ResultKind.Relative && b.ModuleSpecifierKind == ResultKind.Relative)
        {
            var fileName = RootedFilePath.FromValue(importingFile.FileNameGoValue());
            comparison = Core.CompareBooleans(AutoImport.IsFixPossiblyReExportingImportingFile(a, fileName, program.CaseSensitivity()), AutoImport.IsFixPossiblyReExportingImportingFile(b, fileName, program.CaseSensitivity()));
            if (comparison != 0)
                return comparison;
        }

        return TsPath.CompareNumberOfDirectorySeparators(a.ModuleSpecifier, b.ModuleSpecifier);
    }

    // fix.go:1052
    private int CompareModuleSpecifiersForSorting(Fix a, Fix b)
    {
        int result = CompareModuleSpecifiersForRanking(a, b);
        if (result != 0)
            return result;
        if (a.ModuleSpecifier.StartsWith("./"u8) && !b.ModuleSpecifier.StartsWith("./"u8))
            return -1;
        if (b.ModuleSpecifier.StartsWith("./"u8) && !a.ModuleSpecifier.StartsWith("./"u8))
            return 1;
        result = ByteStringKey.FromValue(a.ModuleSpecifier).CompareTo(ByteStringKey.FromValue(b.ModuleSpecifier));
        if (result != 0)
            return result;
        return ((int)a.ImportKind).CompareTo((int)b.ImportKind);
    }

    // fix.go:1073. Preserve the original first-prefix preference and fallback order.
    private int CompareNodeCoreModuleSpecifiers(ByteStringValue a, ByteStringValue b, SourceFile file, CompilerProgram compilerProgram)
    {
        if (a.StartsWith("node:"u8) && !b.StartsWith("node:"u8))
            return shouldUseUriStyleNodeCoreModules == Tristate.True ? -1 : shouldUseUriStyleNodeCoreModules == Tristate.False ? 1 : 0;
        if (b.StartsWith("node:"u8) && !a.StartsWith("node:"u8))
            return shouldUseUriStyleNodeCoreModules == Tristate.True ? 1 : shouldUseUriStyleNodeCoreModules == Tristate.False ? -1 : 0;
        return 0;
    }
}

internal enum FileSyntaxKind
{
    Ambiguous,
    ESM,
    CJS
}

internal static partial class AutoImport
{
    // fix.go:623
    internal static AddAsTypeOnly GetAddAsTypeOnly(bool isValidTypeOnlyUseSite, Export export, CompilerOptions options)
    {
        if (!isValidTypeOnlyUseSite)
            return AddAsTypeOnly.NotAllowed;
        if (options.VerbatimModuleSyntax == Tristate.True && (export.IsTypeOnly || (export.Flags & SymbolFlags.Value) == 0) || export.IsTypeOnly && (export.Flags & SymbolFlags.Value) != 0)
            return AddAsTypeOnly.Required;
        return AddAsTypeOnly.Allowed;
    }

    // fix.go:669
    internal static ByteStringValue GetNamespaceLikeImportText(Node declaration)
    {
        switch (declaration.Kind)
        {
            case Kind.VariableDeclaration:
                var name = declaration.Name();
                return name != null && name.Kind == Kind.Identifier ? name.TextValue() : default;
            case Kind.ImportEqualsDeclaration:
                return declaration.Name().TextValue();
            case Kind.JSDocImportTag:
            case Kind.ImportDeclaration:
                var clause = declaration.ImportClause();
                return clause?.AsImportClause().NamedBindings is { Kind: Kind.NamespaceImport } bindings ? bindings.Name().TextValue() : default;
            default:
                return default;
        }
    }

    // fix.go:799
    internal static ImportKind GetImportKindForImportStatement(SourceFile file, Export export, TypeScript.Compiler.Program program) => GetImportKind(file, export, program, true);
    // fix.go:803
    internal static ImportKind GetImportKind(SourceFile file, Export export, TypeScript.Compiler.Program program, bool forceImportKeyword)
    {
        if (program.Options().VerbatimModuleSyntax == Tristate.True && program.GetEmitModuleFormatOfFile(file) == ModuleKind.CommonJS)
            return ImportKind.CommonJS;
        switch (export.Syntax)
        {
            case ExportSyntax.DefaultModifier:
            case ExportSyntax.DefaultDeclaration:
                return ImportKind.Default;
            case ExportSyntax.Named:
                if (export.ExportName.EqualsString(Ast.InternalSymbolNameDefault))
                    return ImportKind.Default;
                goto case ExportSyntax.Modifier;
            case ExportSyntax.Modifier:
            case ExportSyntax.Star:
            case ExportSyntax.CommonJSExportsProperty:
                return ImportKind.Named;
            case ExportSyntax.Equals:
            case ExportSyntax.CommonJSModuleExports:
            case ExportSyntax.UMD:
                if (!export.ExportName.EqualsString(Ast.InternalSymbolNameExportEquals))
                    return ImportKind.Named;
                foreach (var statement in file.Statements.Nodes)
                    if (Ast.IsImportEqualsDeclaration(statement) && !Ast.NodeIsMissing(statement.AsImportEqualsDeclaration().ModuleReference))
                        return ImportKind.CommonJS;
                if (file.ExternalModuleIndicator != null || forceImportKeyword || !Ast.IsSourceFileJS(file))
                    return ImportKind.Default;
                return ImportKind.CommonJS;
            default:
                throw new InvalidOperationException("unhandled export syntax kind: " + export.Syntax.String().Materialize());
        }
    }

    // fix.go:900
    internal static FileSyntaxKind DetectSyntax(SourceFile file, CompilerOptions options)
    {
        var(hasESM, hasCJS) = DetectSyntaxIndicators(file, options);
        return hasCJS && !hasESM ? FileSyntaxKind.CJS : hasESM && !hasCJS ? FileSyntaxKind.ESM : FileSyntaxKind.Ambiguous;
    }

    // fix.go:916
    internal static (bool HasESM, bool HasCJS) DetectSyntaxIndicators(SourceFile file, CompilerOptions options)
    {
        bool hasCJS = file.CommonJSModuleIndicator != null;
        if (options.GetEmitModuleDetectionKind() != ModuleDetectionKind.Force)
            return (file.ExternalModuleIndicator != null, hasCJS);
        if (file.ExternalModuleIndicator != null && file.ExternalModuleIndicator != file.AsNode())
            return (true, hasCJS);
        foreach (var import in file.Imports())
        {
            if ((import.Flags & NodeFlags.Synthesized) != 0)
                continue;
            var parent = import.Parent;
            if (parent == null)
                continue;
            if (parent.Kind is Kind.ImportDeclaration or Kind.JSImportDeclaration or Kind.ExportDeclaration or Kind.ExternalModuleReference)
                return (true, hasCJS);
        }

        return (false, hasCJS);
    }

    // fix.go:996
    internal static bool NeedsTypeOnly(AddAsTypeOnly addAsTypeOnly) => addAsTypeOnly == AddAsTypeOnly.Required;
    // fix.go:1000
    internal static bool ShouldUseTypeOnly(AddAsTypeOnly addAsTypeOnly, UserPreferences preferences) => NeedsTypeOnly(addAsTypeOnly) || addAsTypeOnly != AddAsTypeOnly.NotAllowed && preferences.PreferTypeOnlyAutoImports == Tristate.True;
    // fix.go:1025
    internal static int CompareFixKinds(AutoImportFixKind a, AutoImportFixKind b) => (int)a - (int)b;
    // fix.go:1097
    internal static bool IsFixPossiblyReExportingImportingFile(Fix fix, RootedFilePath importingFileName, CaseSensitivity sensitivity) => fix.IsReExport && IsIndexFileName(fix.ModuleFileName) && sensitivity.StartsWithDirectory(importingFileName, fix.ModuleFileName.Directory());
    // fix.go:1104
    internal static bool IsIndexFileName(RootedFilePath fileName)
    {
        var name = fileName.BaseName();
        return name == "index.js" || name == "index.jsx" || name == "index.d.ts" || name == "index.ts" || name == "index.tsx";
    }

    // fix.go:1323
    internal static int CompareModuleSpecifierRelativity(Fix a, Fix b, TypeScript.ModuleSpecifiers.UserPreferences preferences) => (preferences.ImportModuleSpecifierPreference == ImportModuleSpecifierPreference.NonRelative || preferences.ImportModuleSpecifierPreference == ImportModuleSpecifierPreference.ProjectRelative) ? Core.CompareBooleans(a.ModuleSpecifierKind == ResultKind.Relative, b.ModuleSpecifierKind == ResultKind.Relative) : 0;
}

internal static partial class AutoImport
{
    // fix.go:1112
    internal static Node PromoteFromTypeOnly(Tracker changes, Node aliasDeclaration, CompilerOptions compilerOptions, SourceFile sourceFile, UserPreferences preferences)
    {
        var convertExistingToTypeOnly = compilerOptions.VerbatimModuleSyntax;
        switch (aliasDeclaration.Kind)
        {
            case Kind.ImportSpecifier:
            {
                var spec = aliasDeclaration.AsImportSpecifier();
                if (spec.IsTypeOnly)
                {
                    if (spec.Parent != null && spec.Parent.Kind == Kind.NamedImports)
                    {
                        var elements = spec.Parent.AsNamedImports().Elements.Nodes;
                        if (elements.Count > 1)
                        {
                            var propertyName = spec.PropertyName == null ? null : changes.NodeFactory.NewIdentifierWithTextValue(spec.PropertyName.TextValue());
                            var newSpecifier = changes.NodeFactory.NewImportSpecifier(false, propertyName, changes.NodeFactory.NewIdentifierWithTextValue(spec.Name().TextValue()));
                            var(comparer, _) = LsUtil.GetNamedImportSpecifierComparerWithDetection(spec.Parent.Parent.Parent, sourceFile, preferences);
                            int insertionIndex = LsUtil.GetImportSpecifierInsertionIndex(elements, newSpecifier, comparer);
                            int currentIndex = -1;
                            for (int i = 0; i < elements.Count; i++)
                                if (elements[i] == aliasDeclaration)
                                {
                                    currentIndex = i;
                                    break;
                                }

                            if (insertionIndex != currentIndex)
                            {
                                changes.Delete(sourceFile, aliasDeclaration);
                                changes.InsertImportSpecifierAtIndex(sourceFile, newSpecifier, spec.Parent, insertionIndex);
                                return aliasDeclaration;
                            }
                        }

                        var firstToken = LsUtil.GetFirstToken(aliasDeclaration, sourceFile);
                        int typePosition = ScannerApi.GetTokenPosOfNode(firstToken, sourceFile, false);
                        var target = spec.PropertyName ?? spec.Name();
                        int targetPosition = ScannerApi.GetTokenPosOfNode(target, sourceFile, false);
                        changes.DeleteRange(sourceFile, Core.NewTextRange(typePosition, targetPosition));
                    }

                    return aliasDeclaration;
                }

                if (spec.Parent == null || spec.Parent.Kind != Kind.NamedImports)
                    throw new InvalidOperationException("ImportSpecifier parent must be NamedImports");
                if (spec.Parent.Parent == null || spec.Parent.Parent.Kind != Kind.ImportClause)
                    throw new InvalidOperationException("NamedImports parent must be ImportClause");
                PromoteImportClause(changes, spec.Parent.Parent, compilerOptions, sourceFile, preferences, convertExistingToTypeOnly, aliasDeclaration);
                return spec.Parent.Parent;
            }

            case Kind.ImportClause:
                PromoteImportClause(changes, aliasDeclaration, compilerOptions, sourceFile, preferences, convertExistingToTypeOnly, aliasDeclaration);
                return aliasDeclaration;
            case Kind.NamespaceImport:
                if (aliasDeclaration.Parent == null || aliasDeclaration.Parent.Kind != Kind.ImportClause)
                    throw new InvalidOperationException("NamespaceImport parent must be ImportClause");
                PromoteImportClause(changes, aliasDeclaration.Parent, compilerOptions, sourceFile, preferences, convertExistingToTypeOnly, aliasDeclaration);
                return aliasDeclaration.Parent;
            case Kind.ImportEqualsDeclaration:
                var scan = ScannerApi.GetScannerForSourceFile(sourceFile, aliasDeclaration.Pos());
                scan.Scan(); // Skip import to reach type.
                DeleteTypeKeyword(changes, sourceFile, scan.TokenStart());
                return aliasDeclaration;
            default:
                throw new InvalidOperationException($"Unexpected alias declaration kind: {aliasDeclaration.Kind}");
        }
    }

    // fix.go:1214
    private static void PromoteImportClause(Tracker changes, Node importClauseNode, CompilerOptions compilerOptions, SourceFile sourceFile, UserPreferences preferences, Tristate convertExistingToTypeOnly, Node aliasDeclaration)
    {
        var importClause = importClauseNode.AsImportClause();
        if (importClause.PhaseModifier == Kind.TypeKeyword)
            DeleteTypeKeyword(changes, sourceFile, importClause.Pos());
        if (compilerOptions.AllowImportingTsExtensions == Tristate.False)
        {
            var moduleSpecifier = NodeBuilderImpl.TryGetModuleSpecifierFromDeclaration(importClause.Parent);
            if (moduleSpecifier != null)
            {
            // Upstream deliberately leaves extension conversion unimplemented here:
            // ResolvedUsingTsExtension cannot be checked without a program.
            }
        }

        if (convertExistingToTypeOnly == Tristate.True)
        {
            var namedImports = importClause.NamedBindings;
            if (namedImports != null && namedImports.Kind == Kind.NamedImports)
            {
                var elements = namedImports.AsNamedImports().Elements.Nodes;
                if (elements.Count > 1)
                {
                    var(_, isSorted) = LsUtil.GetNamedImportSpecifierComparerWithDetection(importClause.Parent, sourceFile, preferences);
                    if (isSorted != Tristate.False && aliasDeclaration != null && aliasDeclaration.Kind == Kind.ImportSpecifier)
                    {
                        int aliasIndex = -1;
                        for (int i = 0; i < elements.Count; i++)
                            if (elements[i] == aliasDeclaration)
                            {
                                aliasIndex = i;
                                break;
                            }

                        if (aliasIndex > 0)
                        {
                            changes.Delete(sourceFile, aliasDeclaration);
                            changes.InsertImportSpecifierAtIndex(sourceFile, aliasDeclaration, namedImports, 0);
                        }
                    }

                    foreach (var element in elements)
                    {
                        if (aliasDeclaration != null && aliasDeclaration.Kind == Kind.ImportSpecifier && element == aliasDeclaration)
                            continue;
                        if (!element.AsImportSpecifier().IsTypeOnly)
                            changes.InsertModifierBefore(sourceFile, Kind.TypeKeyword, element);
                    }
                }
            }
        }
    }

    // fix.go:1290. Byte offsets and only space/tab trailing removal match the scanner owner.
    private static void DeleteTypeKeyword(Tracker changes, SourceFile sourceFile, int startPosition)
    {
        var scan = ScannerApi.GetScannerForSourceFile(sourceFile, startPosition);
        if (scan.Token() != Kind.TypeKeyword)
            return;
        int start = scan.TokenStart(), end = scan.TokenEnd();
        var text = sourceFile.TextView();
        while (end < text.Length && (text[end] == (byte)' ' || text[end] == (byte)'\t'))
            end++;
        changes.DeleteRange(sourceFile, Core.NewTextRange(start, end));
    }

    // fix.go:1306
    internal static ByteStringValue GetModuleSpecifierText(Node promotedDeclaration)
    {
        if (promotedDeclaration.Kind == Kind.ImportEqualsDeclaration)
        {
            var reference = promotedDeclaration.AsImportEqualsDeclaration().ModuleReference;
            if (Ast.IsExternalModuleReference(reference))
            {
                var expression = reference.Expression();
                if (expression != null)
                    return Ast.IsStringLiteralLike(expression) ? expression.TextValue() : ScannerApi.GetTextOfNodeValue(expression);
            }

            return ScannerApi.GetTextOfNodeValue(reference);
        }

        var specifier = promotedDeclaration.Parent.ModuleSpecifier();
        return Ast.IsStringLiteralLike(specifier) ? specifier.TextValue() : ScannerApi.GetTextOfNodeValue(specifier);
    }
}
