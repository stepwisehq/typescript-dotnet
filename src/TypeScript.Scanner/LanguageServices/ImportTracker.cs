// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/importTracker.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Lsp.Protocol;
using Debug = TypeScript.Scanner.Debug;

using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal enum ImpExpKind
{
    Unknown = 0,
    Import = 1,
    Export = 2
}

internal sealed class ImportExportSymbol
{
    internal ImpExpKind kind;
    internal Symbol? symbol;
    internal ExportInfo? exportInfo;
}

internal enum ExportKind
{
    Named = 0,
    Default = 1,
    ExportEquals = 2,
    UMD = 3,
    Module = 4
}

internal sealed class ExportInfo
{
    internal Symbol exportingModuleSymbol = null!;
    internal ExportKind exportKind;
}

internal struct LocationAndSymbol
{
    internal Node importLocation;
    internal Symbol? importSymbol;
}

internal sealed class ImportsResult
{
    internal Slice<LocationAndSymbol> importSearches;
    internal Slice<Node> singleReferences;
    internal Slice<SourceFile> indirectUsers;
}

internal delegate ImportsResult ImportTracker(Symbol exportSymbol, ExportInfo exportInfo, bool isForRename);
internal enum ModuleReferenceKind
{
    Import = 0,
    Reference = 1,
    Implicit = 2
}

internal struct ModuleReference
{
    internal ModuleReferenceKind kind;
    internal Node? literal;
    internal SourceFile? referencingFile;
    internal FileReference? reference;
}

internal sealed partial class LanguageService
{
    // importTracker.go:77. One actual retained imports map per lazily created tracker.
    private static ImportTracker createImportTracker(ProtocolRequestContext context, Program program, Slice<SourceFile> sourceFiles, HashSet<RootedFilePath> sourceFilesSet, Checker checker)
    {
        var allDirectImports = getDirectImportsMap(context, program, sourceFiles, checker);
        return (exportSymbol, exportInfo, isForRename) =>
        {
            var (directImports, indirectUsers) = getImportersForExport(sourceFiles, sourceFilesSet, allDirectImports, exportInfo, checker);
            var (importSearches, singleReferences) = getSearchesFromDirectImports(directImports, exportSymbol, exportInfo.exportKind, checker, isForRename);
            return new ImportsResult
            {
                importSearches = importSearches,
                singleReferences = singleReferences,
                indirectUsers = indirectUsers
            };
        };
    }

    // importTracker.go:87. The map is an actual initialized Go make owner.
    private static Dictionary<Symbol, Slice<Node>> getDirectImportsMap(ProtocolRequestContext context, Program program, Slice<SourceFile> sourceFiles, Checker checker)
    {
        var result = new Dictionary<Symbol, Slice<Node>>();
        foreach (var sourceFile in sourceFiles)
        {
            if (context.Cancellation.IsCancellationRequested)
                return result;
            forEachImport(program, sourceFile, (importDeclaration, moduleSpecifier) =>
            {
                var moduleSymbol = checker.GetSymbolAtLocation(moduleSpecifier);
                if (moduleSymbol != null)
                    result[moduleSymbol] = result.GetValueOrDefault(moduleSymbol).Append(importDeclaration);
            });
        }

        return result;
    }

    // importTracker.go:102. Explicit/implicit import headers follow original order.
    private static void forEachImport(Program program, SourceFile sourceFile, Action<Node, Node> action)
    {
        Slice<Node> implicitImports = default;
        var (_, jsxSpecifier) = program.GetJSXRuntimeImportSpecifierValue(PathKey.FromValue(sourceFile.PathKeyValue()));
        if (jsxSpecifier != null)
            implicitImports = implicitImports.Append(jsxSpecifier);
        var importHelpersSpecifier = program.GetImportHelpersImportSpecifier(PathKey.FromValue(sourceFile.PathKeyValue()));
        if (importHelpersSpecifier != null)
            implicitImports = implicitImports.Append(importHelpersSpecifier);
        if (sourceFile.ExternalModuleIndicator != null || sourceFile.Imports().Length + implicitImports.Length != 0)
        {
            foreach (var import in sourceFile.Imports())
                action(Ast.ImportFromModuleSpecifier(import), import);
            foreach (var import in implicitImports)
                action(Ast.ImportFromModuleSpecifier(import), import);
        }
        else
        {
            forEachPossibleImportOrExportStatement(sourceFile.AsNode(), node =>
            {
                switch (node.Kind)
                {
                    case Kind.ExportDeclaration:
                    case Kind.ImportDeclaration:
                    case Kind.JSImportDeclaration:
                        var specifier = node.ModuleSpecifier();
                        if (specifier != null && Ast.IsStringLiteral(specifier))
                            action(node, specifier);
                        break;
                    case Kind.ImportEqualsDeclaration:
                        if (isExternalModuleImportEquals(node))
                            action(node, node.AsImportEqualsDeclaration().ModuleReference.Expression());
                        break;
                }

                return false;
            });
        }
    }

    // importTracker.go:143. The same action owner is passed through nested modules.
    private static bool forEachPossibleImportOrExportStatement(Node sourceFileLike, Func<Node, bool> action)
    {
        foreach (var statement in getStatementsOfSourceFileLike(sourceFileLike))
            if (action(statement) || (isAmbientModuleDeclaration(statement) && forEachPossibleImportOrExportStatement(statement, action)))
                return true;
        return false;
    }

    // importTracker.go:152.
    private static Node getSourceFileLikeForImportDeclaration(Node node)
    {
        if (Ast.IsCallExpression(node) || Ast.IsJSDocImportTag(node))
            return Ast.GetSourceFileOfNode(node).AsNode();
        var parent = node.Parent;
        if (Ast.IsSourceFile(parent))
            return parent;
        Debug.Assert(Ast.IsModuleBlock(parent) && isAmbientModuleDeclaration(parent.Parent));
        return parent.Parent;
    }

    // importTracker.go:164.
    private static bool isAmbientModuleDeclaration(Node node) => Ast.IsModuleDeclaration(node) && Ast.IsStringLiteral(node.Name());
    // importTracker.go:168. Preserve nil when the source-like owner has no body.
    private static Slice<Node> getStatementsOfSourceFileLike(Node node)
    {
        if (Ast.IsSourceFile(node))
            return node.Statements();
        var body = node.Body();
        return body != null ? body.Statements() : default;
    }

    // importTracker.go:178. These recursive callbacks belong to one query owner;
    // source-file headers and import node identities are never detached.
    private static (Slice<Node> DirectImports, Slice<SourceFile> IndirectUsers) getImportersForExport(Slice<SourceFile> sourceFiles, HashSet<RootedFilePath> sourceFilesSet, Dictionary<Symbol, Slice<Node>> allDirectImports, ExportInfo exportInfo, Checker checker)
    {
        Slice<Node> directImports = default;
        Slice<Node> indirectUserDeclarations = default;
        var markSeenDirectImport = nodeSeenTracker();
        var markSeenIndirectUser = nodeSeenTracker();
        bool isAvailableThroughGlobal = isSourceFileWithGlobalExports(exportInfo.exportingModuleSymbol.ValueDeclaration);
        Slice<Node> getDirectImports(Symbol? moduleSymbol) => moduleSymbol != null ? allDirectImports.GetValueOrDefault(moduleSymbol) : default;
        void addIndirectUser(Node sourceFileLike, bool addTransitiveDependencies)
        {
            if (isAvailableThroughGlobal || !markSeenIndirectUser(sourceFileLike))
                return;
            indirectUserDeclarations = indirectUserDeclarations.Append(sourceFileLike);
            if (!addTransitiveDependencies)
                return;
            var moduleSymbol = checker.GetMergedSymbol(sourceFileLike.Symbol());
            if (moduleSymbol == null)
                return;
            Debug.Assert((moduleSymbol.Flags & SymbolFlags.Module) != 0);
            foreach (var directImport in getDirectImports(moduleSymbol))
                if (!Ast.IsImportTypeNode(directImport))
                    addIndirectUser(getSourceFileLikeForImportDeclaration(directImport), true);
        }

        static bool isExported(Node? node, bool stopAtAmbientModule)
        {
            while (node != null && !(stopAtAmbientModule && isAmbientModuleDeclaration(node)))
            {
                if (Ast.HasSyntacticModifier(node, ModifierFlags.Export))
                    return true;
                node = node.Parent;
            }

            return false;
        }

        void handleImportCall(Node importCall)
        {
            var top = Ast.FindAncestor(importCall, isAmbientModuleDeclaration) ?? Ast.GetSourceFileOfNode(importCall).AsNode();
            addIndirectUser(top, isExported(importCall, true));
        }

        void handleNamespaceImport(Node importDeclaration, Node name, bool isReExport, bool alreadyAddedDirect)
        {
            if (exportInfo.exportKind == ExportKind.ExportEquals)
            {
                if (!alreadyAddedDirect)
                    directImports = directImports.Append(importDeclaration);
            }
            else if (!isAvailableThroughGlobal)
            {
                var sourceFileLike = getSourceFileLikeForImportDeclaration(importDeclaration);
                Debug.Assert(Ast.IsSourceFile(sourceFileLike) || Ast.IsModuleDeclaration(sourceFileLike));
                addIndirectUser(sourceFileLike, isReExport || findNamespaceReExports(sourceFileLike, name, checker));
            }
        }

        void handleDirectImports(Symbol? exportingModuleSymbol)
        {
            foreach (var direct in getDirectImports(exportingModuleSymbol))
            {
                if (!markSeenDirectImport(direct))
                    continue;
                // Original !!! cancellation check remains unimplemented here.
                switch (direct.Kind)
                {
                    case Kind.CallExpression:
                        if (Ast.IsImportCall(direct))
                            handleImportCall(direct);
                        else if (!isAvailableThroughGlobal)
                        {
                            var parent = direct.Parent;
                            if (exportInfo.exportKind == ExportKind.ExportEquals && Ast.IsVariableDeclaration(parent))
                            {
                                var name = parent.Name();
                                if (Ast.IsIdentifier(name))
                                    directImports = directImports.Append(name);
                            }
                        }

                        break;
                    case Kind.Identifier:
                        break;
                    case Kind.ImportEqualsDeclaration:
                        handleNamespaceImport(direct, direct.Name(), Ast.HasSyntacticModifier(direct, ModifierFlags.Export), false);
                        break;
                    case Kind.ImportDeclaration:
                    case Kind.JSImportDeclaration:
                    case Kind.JSDocImportTag:
                        directImports = directImports.Append(direct);
                        var importClause = direct.ImportClause();
                        if (importClause != null)
                        {
                            var namedBindings = importClause.AsImportClause().NamedBindings;
                            if (namedBindings != null && Ast.IsNamespaceImport(namedBindings))
                            {
                                handleNamespaceImport(direct, namedBindings.Name(), false, true);
                                break;
                            }
                        }

                        if (!isAvailableThroughGlobal && Ast.IsDefaultImport(direct))
                            addIndirectUser(getSourceFileLikeForImportDeclaration(direct), false);
                        break;
                    case Kind.ExportDeclaration:
                        var exportClause = direct.AsExportDeclaration().ExportClause;
                        if (exportClause == null)
                            handleDirectImports(getContainingModuleSymbol(direct, checker));
                        else if (Ast.IsNamespaceExport(exportClause))
                            addIndirectUser(getSourceFileLikeForImportDeclaration(direct), true);
                        else
                            directImports = directImports.Append(direct);
                        break;
                    case Kind.ImportType:
                        if (!isAvailableThroughGlobal && direct.AsImportTypeNode().IsTypeOf && direct.AsImportTypeNode().Qualifier == null && isExported(direct, false))
                            addIndirectUser(Ast.GetSourceFileOfNode(direct).AsNode(), true);
                        directImports = directImports.Append(direct);
                        break;
                    default:
                        Debug.Fail("Unexpected import kind.\nNode " + direct.KindString() + " was unexpected.");
                        break;
                }
            }
        }

        Slice<SourceFile> getIndirectUsers()
        {
            if (isAvailableThroughGlobal)
                return sourceFiles;
            foreach (var declaration in exportInfo.exportingModuleSymbol.Declarations)
                if (Ast.IsExternalModuleAugmentation(declaration) && sourceFilesSet.Contains(RootedFilePath.FromValue(Ast.GetSourceFileOfNode(declaration).FileNameGoValue())))
                    addIndirectUser(declaration, false);
            // Original duplicates from multiple ambient declarations are intentional.
            return Core.MapSlice(indirectUserDeclarations, Ast.GetSourceFileOfNode);
        }

        handleDirectImports(exportInfo.exportingModuleSymbol);
        return (directImports, getIndirectUsers());
    }

    // importTracker.go:334.
    private static Symbol? getContainingModuleSymbol(Node importer, Checker checker) => checker.GetMergedSymbol(getSourceFileLikeForImportDeclaration(importer).Symbol());
    // importTracker.go:339. Static-state predicates keep the query symbol owner.
    private static bool findNamespaceReExports(Node sourceFileLike, Node name, Checker checker)
    {
        var namespaceImportSymbol = checker.GetSymbolAtLocation(name);
        return forEachPossibleImportOrExportStatement(sourceFileLike, statement =>
        {
            if (!Ast.IsExportDeclaration(statement))
                return false;
            var exportClause = statement.AsExportDeclaration().ExportClause;
            var moduleSpecifier = statement.ModuleSpecifier();
            return moduleSpecifier == null && exportClause != null && Ast.IsNamedExports(exportClause) && Core.Some(exportClause.Elements(), (checker, namespaceImportSymbol), static (element, state) => state.checker.GetExportSpecifierLocalTargetSymbol(element) == state.namespaceImportSymbol);
        });
    }

    // importTracker.go:354. Query-local closures share the actual result headers.
    private static (Slice<LocationAndSymbol> ImportSearches, Slice<Node> SingleReferences) getSearchesFromDirectImports(Slice<Node> directImports, Symbol exportSymbol, ExportKind exportKind, Checker checker, bool isForRename)
    {
        Slice<LocationAndSymbol> importSearches = default;
        Slice<Node> singleReferences = default;
        void addSearch(Node location, Symbol? symbol) => importSearches = importSearches.Append(new LocationAndSymbol { importLocation = location, importSymbol = symbol });
        bool isNameMatch(ByteStringValue name) => name.EqualsValue(exportSymbol.NameValue) || (exportKind != ExportKind.Named && name.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameDefault)));
        void handleNamespaceImportLike(Node importName)
        {
            if (exportKind == ExportKind.ExportEquals && (!isForRename || isNameMatch(importName.TextValue())))
                addSearch(importName, checker.GetSymbolAtLocation(importName));
        }

        void searchForNamedImport(Node? namedBindings)
        {
            if (namedBindings == null)
                return;
            foreach (var element in namedBindings.Elements())
            {
                var name = element.Name();
                var propertyName = element.PropertyName();
                if (!isNameMatch((propertyName ?? name).TextValue()))
                    continue;
                if (propertyName != null)
                {
                    singleReferences = singleReferences.Append(propertyName);
                    if (!isForRename || name.TextValue().EqualsValue(exportSymbol.NameValue))
                        addSearch(name, checker.GetSymbolAtLocation(name));
                }
                else
                {
                    var localSymbol = Ast.IsExportSpecifier(element) && element.PropertyName() != null ? checker.GetExportSpecifierLocalTargetSymbol(element) : checker.GetSymbolAtLocation(name);
                    addSearch(name, localSymbol);
                }
            }
        }

        void handleImport(Node declaration)
        {
            if (Ast.IsImportEqualsDeclaration(declaration))
            {
                if (isExternalModuleImportEquals(declaration))
                    handleNamespaceImportLike(declaration.Name());
                return;
            }

            if (Ast.IsIdentifier(declaration))
            {
                handleNamespaceImportLike(declaration);
                return;
            }

            if (Ast.IsImportTypeNode(declaration))
            {
                var qualifier = declaration.AsImportTypeNode().Qualifier;
                if (qualifier != null)
                {
                    var firstIdentifier = Ast.GetFirstIdentifier(qualifier);
                    if (firstIdentifier.TextValue().EqualsValue(Ast.SymbolNameValue(exportSymbol)))
                        singleReferences = singleReferences.Append(firstIdentifier);
                }
                else if (exportKind == ExportKind.ExportEquals)
                    singleReferences = singleReferences.Append(declaration.AsImportTypeNode().Argument.AsLiteralTypeNode().Literal);
                return;
            }

            // Original grammar-error admission.
            if (!Ast.IsStringLiteral(declaration.ModuleSpecifier()))
                return;
            if (Ast.IsExportDeclaration(declaration))
            {
                var exportClause = declaration.AsExportDeclaration().ExportClause;
                if (exportClause != null && Ast.IsNamedExports(exportClause))
                    searchForNamedImport(exportClause);
                return;
            }

            var importClause = declaration.ImportClause();
            if (importClause != null)
            {
                var namedBindings = importClause.AsImportClause().NamedBindings;
                if (namedBindings != null)
                {
                    switch (namedBindings.Kind)
                    {
                        case Kind.NamespaceImport:
                            handleNamespaceImportLike(namedBindings.Name());
                            break;
                        case Kind.NamedImports:
                            if (exportKind == ExportKind.Named || exportKind == ExportKind.Default)
                                searchForNamedImport(namedBindings);
                            break;
                    }
                }

                var name = importClause.Name();
                if (name != null && (exportKind == ExportKind.Default || exportKind == ExportKind.ExportEquals) && (!isForRename || name.TextValue().EqualsValue(symbolNameNoDefault(exportSymbol))))
                    addSearch(name, checker.GetSymbolAtLocation(name));
            }
        }

        foreach (var declaration in directImports)
            handleImport(declaration);
        return (importSearches, singleReferences);
    }

    // importTracker.go:463. Local functions preserve source export-first ordering
    // and capture the one query's node/symbol/checker owners.
    private static ImportExportSymbol? getImportOrExportSymbol(Node node, Symbol symbol, Checker checker, bool comingFromExport)
    {
        ImportExportSymbol? exportInfo(Symbol exportedSymbol, ExportKind kind)
        {
            var info = getExportInfo(exportedSymbol, kind, checker);
            return info != null ? new ImportExportSymbol
            {
                kind = ImpExpKind.Export,
                symbol = exportedSymbol,
                exportInfo = info
            }

            : null;
        }

        ImportExportSymbol? getExport()
        {
            ImportExportSymbol? getExportAssignmentExport(Node exportAssignment)
            {
                if (exportAssignment.Symbol().Parent == null)
                    return null;
                return new ImportExportSymbol
                {
                    kind = ImpExpKind.Export,
                    symbol = symbol,
                    exportInfo = new ExportInfo
                    {
                        exportingModuleSymbol = exportAssignment.Symbol().Parent,
                        exportKind = exportAssignment.AsExportAssignment().IsExportEquals ? ExportKind.ExportEquals : ExportKind.Default
                    }
                };
            }

            static ExportKind getExportKindForDeclaration(Node declaration) => Ast.HasSyntacticModifier(declaration, ModifierFlags.Default) ? ExportKind.Default : ExportKind.Named;
            ImportExportSymbol? getSpecialPropertyExport(Node expression, bool useLhsSymbol)
            {
                ExportKind kind;
                switch (Ast.GetAssignmentDeclarationKind(expression))
                {
                    case JSDeclarationKind.ExportsProperty:
                        kind = ExportKind.Named;
                        break;
                    case JSDeclarationKind.ModuleExports:
                        kind = ExportKind.ExportEquals;
                        break;
                    default:
                        return null;
                }

                var exportedSymbol = useLhsSymbol ? expression.Symbol() : symbol;
                return exportedSymbol != null ? exportInfo(exportedSymbol, kind) : null;
            }

            var parent = node.Parent;
            var grandparent = parent.Parent;
            if (symbol.ExportSymbol != null)
            {
                if (Ast.IsPropertyAccessExpression(parent))
                {
                    if (Ast.IsBinaryExpression(grandparent) && Core.Some(symbol.Declarations, parent, static (declaration, owner) => declaration == owner))
                        return getSpecialPropertyExport(grandparent, false);
                    return null;
                }

                return exportInfo(symbol.ExportSymbol, getExportKindForDeclaration(parent));
            }
            else
            {
                var exportNode = getExportNode(parent, node);
                if (exportNode != null && (Ast.HasSyntacticModifier(exportNode, ModifierFlags.Export) || Ast.IsImplicitlyExportedJSDocDeclaration(exportNode)))
                {
                    if (Ast.IsImportEqualsDeclaration(exportNode) && exportNode.AsImportEqualsDeclaration().ModuleReference == node)
                    {
                        if (comingFromExport)
                            return null;
                        return new ImportExportSymbol
                        {
                            kind = ImpExpKind.Import,
                            symbol = checker.GetSymbolAtLocation(exportNode.Name())
                        };
                    }

                    return exportInfo(symbol, getExportKindForDeclaration(exportNode));
                }

                if (Ast.IsNamespaceExport(parent))
                    return exportInfo(symbol, ExportKind.Named);
                if (Ast.IsExportAssignment(parent))
                    return getExportAssignmentExport(parent);
                if (Ast.IsExportAssignment(grandparent))
                    return getExportAssignmentExport(grandparent);
                if (Ast.IsBinaryExpression(parent))
                    return getSpecialPropertyExport(parent, true);
                if (Ast.IsBinaryExpression(grandparent))
                    return getSpecialPropertyExport(grandparent, true);
                if (Ast.IsJSDocTypedefTag(parent) || Ast.IsJSDocCallbackTag(parent))
                    return exportInfo(symbol, ExportKind.Named);
            }

            return null;
        }

        ImportExportSymbol? getImport()
        {
            if (!isNodeImport(node))
                return null;
            var importedSymbol = (symbol.Flags & SymbolFlags.Alias) != 0 ? checker.GetImmediateAliasedSymbol(symbol) : getPropertySymbolOfObjectBindingPatternWithoutPropertyName(symbol, checker);
            if (importedSymbol == null)
                return null;
            importedSymbol = skipExportSpecifierSymbol(importedSymbol, checker);
            if (importedSymbol == null)
                return null;
            if (importedSymbol.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameExportEquals)))
            {
                importedSymbol = getExportEqualsLocalSymbol(importedSymbol, checker);
                if (importedSymbol == null)
                    return null;
            }

            var importedName = symbolNameNoDefault(importedSymbol);
            if (importedName.Length == 0 || importedName.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameDefault)) || importedName.EqualsValue(symbol.NameValue))
                return new ImportExportSymbol
                {
                    kind = ImpExpKind.Import,
                    symbol = importedSymbol
                };
            return null;
        }

        var result = getExport();
        if (result == null && !comingFromExport)
            result = getImport();
        return result;
    }

    // importTracker.go:613. Namespace exports remain global searches.
    private static ExportInfo? getExportInfo(Symbol exportSymbol, ExportKind exportKind, Checker checker)
    {
        if (exportSymbol.Parent != null)
        {
            var exportingModuleSymbol = checker.GetMergedSymbol(exportSymbol.Parent);
            if (Checker.IsExternalModuleSymbol(exportingModuleSymbol))
                return new ExportInfo
                {
                    exportingModuleSymbol = exportingModuleSymbol,
                    exportKind = exportKind
                };
        }

        return null;
    }

    // importTracker.go:631.
    private static Node? getExportNode(Node parent, Node node)
    {
        Node? declaration = null;
        if (Ast.IsVariableDeclaration(parent))
            declaration = parent;
        else if (Ast.IsBindingElement(parent))
            declaration = Ast.WalkUpBindingElementsAndPatterns(parent);
        if (declaration != null)
            return parent.Name() == node && !Ast.IsCatchClause(declaration.Parent) && Ast.IsVariableStatement(declaration.Parent.Parent) ? declaration.Parent.Parent : null;
        return parent;
    }

    // importTracker.go:648.
    private static bool isNodeImport(Node node)
    {
        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.ImportEqualsDeclaration:
                return parent.Name() == node && isExternalModuleImportEquals(parent);
            case Kind.ImportSpecifier:
                return parent.PropertyName() == null;
            case Kind.ImportClause:
            case Kind.NamespaceImport:
                Debug.Assert(parent.Name() == node);
                return true;
            case Kind.BindingElement:
                return Ast.IsInJSFile(node) && Ast.IsVariableDeclarationInitializedToBareOrAccessedRequire(parent.Parent.Parent);
            default:
                return false;
        }
    }

    // importTracker.go:663. Do not admit a non-string external module reference.
    private static bool isExternalModuleImportEquals(Node node)
    {
        var moduleReference = node.AsImportEqualsDeclaration().ModuleReference;
        return Ast.IsExternalModuleReference(moduleReference) && moduleReference.Expression().Kind == Kind.StringLiteral;
    }

    // importTracker.go:669. Alias skipping preserves declaration order and original
    // nil symbol outcomes for CommonJS property access/shorthand declarations.
    private static Symbol? skipExportSpecifierSymbol(Symbol symbol, Checker checker)
    {
        foreach (var declaration in symbol.Declarations)
        {
            if (Ast.IsExportSpecifier(declaration) && declaration.PropertyName() == null && declaration.Parent.Parent.ModuleSpecifier() == null)
                return checker.GetExportSpecifierLocalTargetSymbol(declaration) ?? symbol;
            if (Ast.IsPropertyAccessExpression(declaration) && Ast.IsModuleExportsAccessExpression(declaration.Expression()) && !Ast.IsPrivateIdentifier(declaration.Name()))
                return checker.GetSymbolAtLocation(declaration);
            if (Ast.IsShorthandPropertyAssignment(declaration) && Ast.IsBinaryExpression(declaration.Parent.Parent) && Ast.GetAssignmentDeclarationKind(declaration.Parent.Parent) == JSDeclarationKind.ModuleExports)
                return checker.GetExportSpecifierLocalTargetSymbol(declaration.Name());
        }

        return symbol;
    }

    // importTracker.go:684.
    private static Symbol? getExportEqualsLocalSymbol(Symbol importedSymbol, Checker checker)
    {
        if ((importedSymbol.Flags & SymbolFlags.Alias) != 0)
            return checker.GetImmediateAliasedSymbol(importedSymbol);
        var declaration = importedSymbol.ValueDeclaration;
        Debug.Assert(declaration != null);
        if (Ast.IsExportAssignment(declaration))
            return declaration!.Expression().Symbol();
        if (Ast.IsBinaryExpression(declaration))
            return declaration!.AsBinaryExpression().Right.Symbol();
        if (Ast.IsSourceFile(declaration))
            return declaration!.Symbol();
        return null;
    }

    // importTracker.go:702. The actual symbol-name header stays borrowed.
    private static ByteStringValue symbolNameNoDefault(Symbol symbol)
    {
        if (!symbol.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameDefault)))
            return symbol.NameValue;
        foreach (var declaration in symbol.Declarations)
        {
            var name = Ast.GetNameOfDeclaration(declaration);
            if (name != null && Ast.IsIdentifier(name))
                return name.TextValue();
        }

        return default;
    }

    // importTracker.go:718. The source slice stores value records, not pointer
    // wrappers; source reference and literal identities remain retained fields.
    private static Slice<ModuleReference> findModuleReferences(Program program, Slice<SourceFile> sourceFiles, Symbol searchModuleSymbol, Checker checker)
    {
        var references = Slice<ModuleReference>.Make(0);
        foreach (var referencingFile in sourceFiles)
        {
            var searchSourceFile = searchModuleSymbol.ValueDeclaration;
            if (searchSourceFile != null && searchSourceFile.Kind == Kind.SourceFile)
            {
                foreach (var reference in referencingFile.ReferencedFiles)
                    if (program.GetSourceFileFromReference(referencingFile, reference) == searchSourceFile.AsSourceFile())
                        references = references.Append(new ModuleReference { kind = ModuleReferenceKind.Reference, referencingFile = referencingFile, reference = reference });
                foreach (var reference in referencingFile.TypeReferenceDirectives)
                {
                    var referenced = program.GetResolvedTypeReferenceDirectiveFromTypeReferenceDirective(reference, referencingFile);
                    if (referenced != null && referenced.ResolvedFileName == RootedFilePath.FromValue(searchSourceFile.AsSourceFile().FileNameGoValue()))
                        references = references.Append(new ModuleReference { kind = ModuleReferenceKind.Reference, referencingFile = referencingFile, reference = reference });
                }
            }

            forEachImport(program, referencingFile, (importDeclaration, moduleSpecifier) =>
            {
                var moduleSymbol = checker.GetSymbolAtLocation(moduleSpecifier);
                if (moduleSymbol == searchModuleSymbol)
                    references = references.Append(Ast.NodeIsSynthesized(importDeclaration) ? new ModuleReference { kind = ModuleReferenceKind.Implicit, literal = moduleSpecifier, referencingFile = referencingFile } : new ModuleReference { kind = ModuleReferenceKind.Import, literal = moduleSpecifier });
            });
        }

        return references;
    }
}
