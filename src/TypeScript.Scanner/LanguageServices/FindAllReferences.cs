// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/findallreferences.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.DiagnosticPrinting;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using ScannerApi = TypeScript.Scanner.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using Debug = TypeScript.Scanner.Debug;

using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal enum ReferenceUse
{
    None = 0,
    Other = 1,
    References = 2,
    Rename = 3
}

internal struct RefOptions
{
    internal bool findInStrings, findInComments;
    internal ReferenceUse use;
    internal bool implementations, useAliasesForRename;
}

// RefInfo is rehomed from the accepted shared-leaf counterpart at coherent promotion.
internal sealed class RefInfo
{
    internal SourceFile? file;
    internal RootedFilePath fileName;
    internal FileReference? reference;
    internal bool unverified;
}

internal enum DefinitionKind
{
    Symbol = 0,
    Label = 1,
    Keyword = 2,
    This = 3,
    String = 4,
    TripleSlashReference = 5
}

internal sealed class ReferenceDefinition
{
    internal DefinitionKind Kind;
    internal Symbol? symbol;
    internal Node? node;
    internal TripleSlashDefinition? tripleSlashFileRef;
}

internal sealed class TripleSlashDefinition
{
    internal FileReference reference = null!;
    internal SourceFile file = null!;
}

internal enum EntryKind
{
    None = 0,
    Range = 1,
    Node = 2,
    StringLiteral = 3,
    SearchedLocalFoundProperty = 4,
    SearchedPropertyFoundLocal = 5
}

internal sealed class ReferenceEntry
{
    internal EntryKind kind;
    internal Node? node;
    internal Node? context; // Original !!! ContextWithStartAndEndNode, optional.
    internal SourceFile? sourceFile;
    // Node-derived ranges own their cached value; module references retain the
    // actual FileReference owner of Go's &reference.ref.TextRange field pointer.
    private TextRange? cachedTextRange;
    internal FileReference? fileReferenceRange;
    internal TextRange? textRange { get => fileReferenceRange?.TextRange ?? cachedTextRange; set => cachedTextRange = value; }

    internal Location? lspRange;
    internal bool unmappable;
    // findallreferences.go:126/131.
    internal Node? Node() => node;
    internal bool IsNodeEntry() => node != null;
}

internal sealed class SymbolAndEntries
{
    internal ReferenceDefinition? definition;
    internal Slice<ReferenceEntry> references;
    // findallreferences.go:65. Keep the pointer definition and retained result header.
    internal static SymbolAndEntries NewSymbolAndEntries(DefinitionKind kind, Node? node, Symbol? symbol, Slice<ReferenceEntry> references) => new()
    {
        definition = new ReferenceDefinition
        {
            Kind = kind,
            node = node,
            symbol = symbol
        },
        references = references
    };
    internal Slice<ReferenceEntry> References() => references;
    internal Node? DefinitionNode()
    {
        if (definition == null)
            return null;
        if (definition.node != null)
            return definition.node;
        if (definition.symbol != null && definition.symbol.Declarations.Length > 0)
            return definition.symbol.Declarations[0];
        return null;
    }

    internal Symbol? DefinitionSymbol() => definition?.symbol;
    internal bool canUseDefinitionSymbol()
    {
        if (definition == null)
            return false;
        switch (definition.Kind)
        {
            case DefinitionKind.Symbol:
            case DefinitionKind.This:
                return definition.symbol != null;
            case DefinitionKind.TripleSlashReference:
                // Original TODO: file-reference search is not implemented here.
                return false;
            default:
                return false;
        }
    }
}

// findallreferences.go:480. Retained interface position owner, including lazy nil results.
internal class ReferencePosition : IHasTextDocumentPosition
{
    internal DocumentUri uri;
    internal TypeScript.Lsp.Protocol.Position pos;
    public DocumentUri TextDocumentURI() => uri;
    public TypeScript.Lsp.Protocol.Position TextDocumentPosition() => pos;
}

internal sealed class NonLocalDefinition : ReferencePosition
{
    internal Lazy<IHasTextDocumentPosition?> sourcePosition = null!;
    internal Lazy<IHasTextDocumentPosition?> generatedPosition = null!;
    internal IHasTextDocumentPosition? GetSourcePosition() => sourcePosition.Value;
    internal IHasTextDocumentPosition? GetGeneratedPosition() => generatedPosition.Value;
}

internal struct SignatureUsage
{
    internal Node Name;
    internal Node? Call;
}

internal struct SymbolEntryTransformOptions
{
    internal bool requireLocationsResult, dropOriginNodes;
}

internal struct SymbolAndEntriesData
{
    internal Node? OriginalNode;
    internal Slice<SymbolAndEntries> SymbolsAndEntries;
    internal int Position;
}

internal sealed partial class LanguageService
{
    // findallreferences.go:168. Cache the actual mutable entry location once.
    private TypeScript.Lsp.Protocol.Range getRangeOfEntry(ReferenceEntry entry) => resolveEntry(entry).lspRange!.Range;
    private (TypeScript.Lsp.Protocol.Range Range, bool Ok) getRangeOfEntryForFeature(ReferenceEntry entry, Feature feature)
    {
        var (location, ok) = getLocationOfEntryForFeature(entry, feature);
        return (location.Range, ok);
    }

    private DocumentUri getFileNameOfEntry(ReferenceEntry entry) => resolveEntry(entry).lspRange!.Uri;
    private (Location Location, bool Ok) getLocationOfEntryForFeature(ReferenceEntry entry, Feature feature)
    {
        resolveEntrySource(entry);
        var (location, fidelity) = sourceFileRangeToLSPLocationForFeature(entry.sourceFile!, entry.textRange!.Value, feature);
        return (location, fidelity.IsSingleSegment());
    }

    private static void resolveEntrySource(ReferenceEntry entry)
    {
        if (entry.sourceFile == null)
        {
            Debug.Assert(entry.node != null, "reference entry must have a node or source file");
            entry.sourceFile = Ast.GetSourceFileOfNode(entry.node!);
        }

        if (entry.textRange == null)
            entry.textRange = getRangeOfNode(entry.node!, entry.sourceFile, null);
    }

    private ReferenceEntry resolveEntry(ReferenceEntry entry)
    {
        resolveEntrySource(entry);
        if (entry.lspRange == null)
        {
            var (location, fidelity) = sourceFileRangeToLSPLocation(entry.sourceFile!, entry.textRange!.Value);
            entry.lspRange = location;
            entry.unmappable = !fidelity.IsSingleSegment();
        }

        return entry;
    }

    private static ReferenceEntry newNodeEntryWithKind(Node node, EntryKind kind)
    {
        var entry = newNodeEntry(node);
        entry.kind = kind;
        return entry;
    }

    private static ReferenceEntry newNodeEntry(Node node) => new()
    {
        kind = EntryKind.Node,
        node = node.Name() ?? node,
        context = getContextNodeForNodeEntry(node)
    };
    // findallreferences.go:221. The import ancestor predicate deliberately reads the
    // original node, as pinned upstream does; explicit state avoids a closure.
    private static Node? getContextNodeForNodeEntry(Node node)
    {
        if (Ast.IsDeclaration(node))
            return getContextNode(node);
        if (node.Parent == null)
            return null;
        if (!Ast.IsDeclaration(node.Parent) && !Ast.IsExportAssignment(node.Parent))
        {
            if (Ast.IsInJSFile(node))
            {
                // Original !!! jsdoc: check if branch still needed.
                Node? binaryExpression = null;
                if (Ast.IsBinaryExpression(node.Parent))
                    binaryExpression = node.Parent;
                else if (Ast.IsAccessExpression(node.Parent) && Ast.IsBinaryExpression(node.Parent.Parent) && node.Parent.Parent.AsBinaryExpression().Left == node.Parent)
                    binaryExpression = node.Parent.Parent;
                if (binaryExpression != null && Ast.GetAssignmentDeclarationKind(binaryExpression) != JSDeclarationKind.None)
                    return getContextNode(binaryExpression);
            }

            switch (node.Parent.Kind)
            {
                case Kind.JsxOpeningElement:
                case Kind.JsxClosingElement:
                    return node.Parent.Parent;
                case Kind.JsxSelfClosingElement:
                case Kind.LabeledStatement:
                case Kind.BreakStatement:
                case Kind.ContinueStatement:
                    return node.Parent;
                case Kind.StringLiteral:
                case Kind.NoSubstitutionTemplateLiteral:
                    var validImport = Ast.TryGetImportFromModuleSpecifier(node);
                    if (validImport != null)
                    {
                        var declarationOrStatement = Ast.FindAncestor(validImport, node, static (_, original) => Ast.IsDeclaration(original) || Ast.IsStatement(original) || Ast.IsJSDocTag(original));
                        return Ast.IsDeclaration(declarationOrStatement) ? getContextNode(declarationOrStatement) : declarationOrStatement;
                    }

                    break;
            }

            var propertyName = Ast.FindAncestor(node, Ast.IsComputedPropertyName);
            return propertyName != null ? getContextNode(propertyName.Parent) : null;
        }

        if (node.Parent.Name() == node || node.Parent.Kind == Kind.Constructor || node.Parent.Kind == Kind.ExportAssignment || ((Ast.IsImportOrExportSpecifier(node.Parent) || node.Parent.Kind == Kind.BindingElement) && node.Parent.PropertyName() == node) || (node.Kind == Kind.DefaultKeyword && Ast.HasSyntacticModifier(node.Parent, ModifierFlags.ExportDefault)))
            return getContextNode(node.Parent);
        return null;
    }

    // findallreferences.go:286. Upstream's for/switch TODO leaves remain true nil.
    private static Node? getContextNode(Node? node)
    {
        if (node == null)
            return null;
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
                if (!Ast.IsVariableDeclarationList(node.Parent) || node.Parent.AsVariableDeclarationList().Declarations.Nodes.Length != 1)
                    return node;
                if (Ast.IsVariableStatement(node.Parent.Parent))
                    return node.Parent.Parent;
                if (Ast.IsForInOrOfStatement(node.Parent.Parent))
                    return getContextNode(node.Parent.Parent);
                return node.Parent;
            case Kind.BindingElement:
                return getContextNode(node.Parent.Parent);
            case Kind.ImportSpecifier:
                return node.Parent.Parent.Parent;
            case Kind.ExportSpecifier:
            case Kind.NamespaceImport:
                return node.Parent.Parent;
            case Kind.ImportClause:
            case Kind.NamespaceExport:
                return node.Parent;
            case Kind.BinaryExpression:
                return node.Parent.Kind == Kind.ExpressionStatement ? node.Parent : node;
            case Kind.ForOfStatement:
            case Kind.ForInStatement:
            case Kind.SwitchStatement:
                // !!! not implemented in pinned upstream.
                return null;
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
                return Ast.IsArrayLiteralOrObjectLiteralDestructuringPattern(node.Parent) ? getContextNode(Ast.FindAncestor(node.Parent, static ancestor => ancestor.Kind == Kind.BinaryExpression || Ast.IsForInOrOfStatement(ancestor))) : node;
            default:
                return node;
        }
    }

    // findallreferences.go:335. Only string-literal ranges strip the original quote bounds.
    private static TextRange getRangeOfNode(Node node, SourceFile? sourceFile, Node? endNode)
    {
        sourceFile ??= Ast.GetSourceFileOfNode(node);
        int start = ScannerApi.GetTokenPosOfNode(node, sourceFile, false);
        int end = (endNode ?? node).End();
        if (Ast.IsStringLiteralLike(node) && end - start > 2)
        {
            if (endNode != null)
                throw new InvalidOperationException("endNode is not nil for stringLiteralLike");
            start++;
            end--;
        }

        if (endNode != null && endNode.Kind == Kind.CaseBlock)
            end = endNode.Pos();
        return new TextRange(start, end);
    }

    // findallreferences.go:354. Search spelling remains a borrowed byte header.
    private static bool isValidReferencePosition(Node node, ByteStringValue searchSymbolName)
    {
        switch (node.Kind)
        {
            case Kind.PrivateIdentifier:
                // Original !!! JSDocMemberName admission remains unimplemented.
                return node.TextValue().Length == searchSymbolName.Length;
            case Kind.Identifier:
                return node.TextValue().Length == searchSymbolName.Length;
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.StringLiteral:
                return node.TextValue().Length == searchSymbolName.Length && (isLiteralNameOfPropertyDeclarationOrIndexAccess(node) || isNameOfModuleDeclaration(node) || isExpressionOfExternalModuleImportEqualsDeclaration(node) || (Ast.IsCallExpression(node.Parent) && Ast.IsBindableObjectDefinePropertyCall(node.Parent) && node.Parent.Arguments()[1] == node) || Ast.IsImportOrExportSpecifier(node.Parent));
            case Kind.NumericLiteral:
                return isLiteralNameOfPropertyDeclarationOrIndexAccess(node) && node.TextValue().Length == searchSymbolName.Length;
            case Kind.DefaultKeyword:
                return 7 == searchSymbolName.Length;
            default:
                return false;
        }
    }

    // findallreferences.go:379.
    private static bool isForRenameWithPrefixAndSuffixText(RefOptions options) => options.use == ReferenceUse.Rename && options.useAliasesForRename;
    // findallreferences.go:383. The first non-nil result follows declaration order.
    private static Symbol? skipPastExportOrImportSpecifierOrUnion(Symbol symbol, Node? node, Checker checker, bool useLocalSymbolForExportSpecifier)
    {
        if (node == null)
            return null;
        var parent = node.Parent;
        if (parent.Kind == Kind.ExportSpecifier && useLocalSymbolForExportSpecifier)
            return getLocalSymbolForExportSpecifier(node, symbol, parent.AsExportSpecifier(), checker);
        foreach (var declaration in symbol.Declarations)
        {
            if (declaration.Parent == null)
            {
                if ((symbol.Flags & (SymbolFlags.Transient | SymbolFlags.ModuleExports)) != 0)
                    continue;
                throw new InvalidOperationException("Unexpected symbol at " + node.Kind.String() + ": " + symbol.NameValue.Materialize());
            }

            if (declaration.Parent.Kind == Kind.TypeLiteral && declaration.Parent.Parent.Kind == Kind.UnionType)
            {
                var property = checker.GetPropertyOfType(checker.GetTypeFromTypeNode(declaration.Parent.Parent), symbol.NameValue);
                if (property != null)
                    return property;
            }
        }

        return null;
    }

    // findallreferences.go:407. Scope identity is the actual container node owner.
    private static Node? getSymbolScope(Symbol symbol)
    {
        var valueDeclaration = symbol.ValueDeclaration;
        if (valueDeclaration != null && (valueDeclaration.Kind == Kind.FunctionExpression || valueDeclaration.Kind == Kind.ClassExpression))
            return valueDeclaration;
        if (symbol.Declarations.Length == 0)
            return null;
        var declarations = symbol.Declarations;
        if ((symbol.Flags & (SymbolFlags.Property | SymbolFlags.Method)) != 0)
        {
            foreach (var declaration in declarations)
            {
                if (Ast.HasModifier(declaration, ModifierFlags.Private) || Ast.IsPrivateIdentifierClassElementDeclaration(declaration))
                    return Ast.FindAncestorKind(declaration, Kind.ClassDeclaration);
            }

            return null;
        }

        foreach (var declaration in declarations)
            if (isObjectBindingElementWithoutPropertyName(declaration))
                return null;
        bool exposedByParent = symbol.Parent != null && (symbol.Flags & SymbolFlags.TypeParameter) == 0;
        if (exposedByParent && !(Checker.IsExternalModuleSymbol(symbol.Parent!) && !isSourceFileWithGlobalExports(symbol.Parent!.ValueDeclaration)))
            return null;
        Node? scope = null;
        foreach (var declaration in declarations)
        {
            var container = getContainerNode(declaration);
            if (scope != null && scope != container)
                return null;
            if (container == null || (container.Kind == Kind.SourceFile && !Ast.IsExternalOrCommonJSModule(container.AsSourceFile())))
                return null;
            scope = container;
        }

        if (exposedByParent)
            return Ast.GetSourceFileOfNode(scope!).AsNode();
        return scope; // Original TODO: GH#18217.
    }

    // findallreferences.go:496. Shared declaration-map query worker, not a references response.
    private static (SourceFile File, int Position) getFileAndStartPosFromDeclaration(Node declaration)
    {
        var file = Ast.GetSourceFileOfNode(declaration);
        var name = Ast.GetNameOfDeclaration(declaration) ?? declaration;
        return (file, getRangeOfNode(name, file, null).Pos());
    }

    // findallreferences.go:503. Both mapped positions retain sync.OnceValue's
    // nil caching and exception propagation through Lazy ExecutionAndPublication.
    private NonLocalDefinition? getNonLocalDefinition(ProtocolRequestContext context, SymbolAndEntries entry)
    {
        if (!entry.canUseDefinitionSymbol())
            return null;
        var program = GetProgram();
        var (checker, done) = program.GetTypeChecker(context);
        try
        {
            var emitResolver = checker.NewEmitResolver(new TypeScript.DiagnosticPrinting.EmitContext());
            foreach (var declaration in entry.definition!.symbol!.Declarations)
            {
                if (!isDefinitionVisible(emitResolver, declaration))
                    continue;
                var (file, startPosition) = getFileAndStartPosFromDeclaration(declaration);
                var fileName = RootedFilePath.FromValue(file.FileNameGoValue());
                var (lspPosition, fidelity) = converters.ToLSPPosition(file, startPosition);
                if (fidelity.IsNone())
                    continue;
                return new NonLocalDefinition
                {
                    uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(fileName),
                    pos = lspPosition,
                    sourcePosition = new Lazy<IHasTextDocumentPosition?>(() =>
                    {
                        var mapped = tryGetSourcePosition(fileName, startPosition);
                        if (mapped == null)
                            return null;
                        var (mappedPosition, mappedFidelity) = converters.ToLSPPosition(getScript(mapped.FileName)!, mapped.Pos);
                        if (mappedFidelity.IsNone())
                            return null;
                        return new ReferencePosition
                        {
                            uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(mapped.FileName),
                            pos = mappedPosition
                        };
                    }, LazyThreadSafetyMode.ExecutionAndPublication),
                    generatedPosition = new Lazy<IHasTextDocumentPosition?>(() =>
                    {
                        var mapped = tryGetGeneratedPosition(fileName, startPosition);
                        if (mapped == null)
                            return null;
                        var (mappedPosition, mappedFidelity) = converters.ToLSPPosition(getScript(mapped.FileName)!, mapped.Pos);
                        if (mappedFidelity.IsNone())
                            return null;
                        return new ReferencePosition
                        {
                            uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(mapped.FileName),
                            pos = mappedPosition
                        };
                    }, LazyThreadSafetyMode.ExecutionAndPublication)
                };
            }

            return null;
        }
        finally
        {
            done();
        }
    }

    // findallreferences.go:570. These service exceptions deliberately follow the
    // declaration visibility owner rather than treating expressions as exports.
    private static bool isDefinitionVisible(IEmitResolver emitResolver, Node declaration)
    {
        if (emitResolver.IsDeclarationVisible(declaration))
            return true;
        if (declaration.Parent == null)
            return false;
        if (Ast.HasInitializer(declaration.Parent) && declaration.Parent.Initializer() == declaration)
            return isDefinitionVisible(emitResolver, declaration.Parent);
        switch (declaration.Kind)
        {
            case Kind.PropertyDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.MethodDeclaration:
                if (Ast.HasModifier(declaration, ModifierFlags.Private) || Ast.IsPrivateIdentifier(declaration.Name()))
                    return false;
                goto case Kind.Constructor;
            case Kind.Constructor:
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.ObjectLiteralExpression:
            case Kind.ClassExpression:
            case Kind.ArrowFunction:
            case Kind.FunctionExpression:
                return isDefinitionVisible(emitResolver, declaration.Parent);
            default:
                return false;
        }
    }

    // findallreferences.go:608. Callback locations remain content-map-qualified.
    private void forEachOriginalDefinitionLocation(ProtocolRequestContext context, SymbolAndEntries entry, Action<DocumentUri, TypeScript.Lsp.Protocol.Position> callback)
    {
        if (!entry.canUseDefinitionSymbol())
            return;
        var program = GetProgram();
        foreach (var declaration in entry.definition!.symbol!.Declarations)
        {
            var (file, startPosition) = getFileAndStartPosFromDeclaration(declaration);
            var fileName = RootedFilePath.FromValue(file.FileNameGoValue());
            if (fileName.IsDeclarationFile())
            {
                var mapped = tryGetSourcePosition(fileName, startPosition);
                if (mapped != null)
                {
                    var (position, fidelity) = converters.ToLSPPosition(getScript(mapped.FileName)!, mapped.Pos);
                    if (!fidelity.IsNone())
                        callback(TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(mapped.FileName), position);
                }
            }
            else if (program.IsSourceFromProjectReference(PathKey.FromValue(file.PathKeyValue())))
            {
                var (position, fidelity) = converters.ToLSPPosition(file, startPosition);
                if (!fidelity.IsNone())
                    callback(TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(fileName), position);
            }
        }
    }

    // findallreferences.go:644. Preserve projected segment order and append owners.
    internal (SymbolAndEntriesData Data, bool Ok) provideSymbolsAndEntries(ProtocolRequestContext context, DocumentUri uri, TypeScript.Lsp.Protocol.Position documentPosition, bool isRename, bool implementations)
    {
        var (program, sourceFile) = getProgramAndFile(uri);
        var feature = implementations ? Feature.Implementation : isRename ? Feature.Rename : Feature.References;
        var positions = converters.FromLSPPositionForSourceFile(sourceFile, documentPosition, feature);
        if (positions.Length == 0)
            return (default, false);
        SymbolAndEntriesData combined = default;
        bool ok = false;
        foreach (var mapped in positions)
        {
            if (!mapped.Fidelity.IsSingleSegment())
                continue;
            var (data, found) = provideSymbolsAndEntriesAtPosition(context, program, mapped.Script, mapped.Position, isRename, implementations);
            if (!found)
                continue;
            if (!ok)
            {
                combined.OriginalNode = data.OriginalNode;
                combined.Position = data.Position;
                ok = true;
            }

            combined.SymbolsAndEntries = combined.SymbolsAndEntries.Append(data.SymbolsAndEntries);
        }

        return (combined, ok);
    }

    // findallreferences.go:679. Queue headers advance without retaining new copies;
    // definition/reference identities are shared with the original entries.
    private (SymbolAndEntriesData Data, bool Ok) provideSymbolsAndEntriesAtPosition(ProtocolRequestContext context, TypeScript.Compiler.Program program, SourceFile sourceFile, int position, bool isRename, bool implementations)
    {
        var node = AstNav.GetTouchingPropertyName(sourceFile, position);
        if (isRename)
            node = getAdjustedLocation(node, true, sourceFile);
        if ((isRename && !nodeIsEligibleForRename(node)) || (implementations && Ast.IsSourceFile(node)))
            return (new SymbolAndEntriesData { OriginalNode = node, Position = position }, false);
        var entries = getSymbolAndEntries(context, position, node, program, isRename, implementations);
        if (!implementations)
            return (new SymbolAndEntriesData { OriginalNode = node, SymbolsAndEntries = entries, Position = position }, true);
        Slice<SymbolAndEntries> implementationEntries = default;
        Slice<ReferenceEntry> queue = default;
        HashSet<Node?>? seenNodes = null;
        HashSet<Symbol?>? seenDefinitions = null;
        // One query-local captured owner, matching the original closure lifetime.
        void addToQueue(Slice<SymbolAndEntries> symbolsAndEntries)
        {
            foreach (var symbolAndEntries in symbolsAndEntries)
            {
                Slice<ReferenceEntry> newReferences = default;
                foreach (var reference in symbolAndEntries.references)
                {
                    if ((seenNodes ??= new()).Add(reference.node))
                    {
                        queue = queue.Append(reference);
                        newReferences = newReferences.Append(reference);
                    }
                }

                if (newReferences.Length > 0 || symbolAndEntries.definition == null || (seenDefinitions ??= new()).Add(symbolAndEntries.definition.symbol))
                    implementationEntries = implementationEntries.Append(new SymbolAndEntries { definition = symbolAndEntries.definition, references = newReferences });
            }
        }

        addToQueue(entries);
        while (queue.Length != 0)
        {
            if (context.Cancellation.IsCancellationRequested)
                return (default, false);
            var entry = queue[0];
            queue = queue.Slice2(1, queue.Length);
            if (entry.node != null)
                addToQueue(getSymbolAndEntries(context, entry.node.Pos(), entry.node, program, isRename, implementations));
        }

        return (new SymbolAndEntriesData { OriginalNode = node, SymbolsAndEntries = implementationEntries, Position = position }, true);
    }

    // findallreferences.go:728.
    private Slice<SymbolAndEntries> getSymbolAndEntries(ProtocolRequestContext context, int position, Node node, TypeScript.Compiler.Program program, bool isRename, bool implementations)
    {
        RefOptions options = default;
        if (!isRename)
        {
            options.use = ReferenceUse.References;
            if (implementations)
                options.implementations = true;
        }
        else
        {
            options.use = ReferenceUse.Rename;
            options.useAliasesForRename = (UserPreferences().ProvidePrefixAndSuffixTextForRename != Tristate.False);
        }

        return getReferencedSymbolsForNode(context, position, node, program, program.GetSourceFiles(), options);
    }

    // findallreferences.go:747.
    internal (LocationsOrNull Result, Exception? Error) ProvideReferences(ProtocolRequestContext context, ReferenceParams parameters, ICrossProjectOrchestrator? orchestrator) => handleCrossProject(context, parameters, orchestrator, static (service, ctx, request, data, options) => service.symbolAndEntriesToReferences(ctx, request, data, options), combineReferences, false, false, default, null);
    // findallreferences.go:761.
    private (LocationsOrNull Result, Exception? Error) provideReferencesFromData(ProtocolRequestContext context, ReferenceParams parameters, ICrossProjectOrchestrator? orchestrator, SymbolAndEntriesData data) => handleCrossProject(context, parameters, orchestrator, static (service, ctx, request, symbols, options) => service.symbolAndEntriesToReferences(ctx, request, symbols, options), combineReferences, false, false, default, new SymbolAndEntriesDataOwner(data));
    // findallreferences.go:775.
    internal (VSReferenceItemsOrNull Result, Exception? Error) ProvideVSReferences(ProtocolRequestContext context, ReferenceParams parameters, ICrossProjectOrchestrator? orchestrator) => handleCrossProject(context, parameters, orchestrator, static (service, ctx, request, data, options) => service.symbolAndEntriesToVSReferences(ctx, request, data, options), combineVSReferences, false, false, default, null);
    // findallreferences.go:789.
    private (LocationsOrNull Result, Exception? Error) symbolAndEntriesToReferences(ProtocolRequestContext context, ReferenceParams parameters, SymbolAndEntriesData data, SymbolEntryTransformOptions options)
    {
        Slice<Location> locations = default;
        HashSet<DefinitionLocationKey>? seen = null;
        foreach (var symbol in data.SymbolsAndEntries)
            locations = combineLocationArray(locations, convertSymbolAndEntriesToLocations(symbol, parameters.Context!.IncludeDeclaration, Feature.References), ref seen);
        return (new LocationsOrNull { Locations = locations }, null);
    }

    // findallreferences.go:800.
    private (VSReferenceItemsOrNull Result, Exception? Error) symbolAndEntriesToVSReferences(ProtocolRequestContext context, ReferenceParams parameters, SymbolAndEntriesData data, SymbolEntryTransformOptions options)
    {
        bool vs = context.ClientCapabilities.VSSupportsVisualStudioExtensions;
        Slice<VSReferenceItem?> items = default;
        int id = 0;
        var projectName = ByteStringValue.FromString(projectID.GoLogString());
        foreach (var symbol in data.SymbolsAndEntries)
        {
            if (symbol.definition == null)
                continue;
            var info = definitionToReferencedSymbolDefinitionInfo(context, symbol.definition, data.OriginalNode!, vs, Feature.References);
            if (info == null)
                continue;
            int definitionId = id;
            items = items.Append(new VSReferenceItem { VSId = definitionId, VSLocation = info.location, VSDefinitionText = info.displayText, VSKind = Slice<VSReferenceKind>.FromArray([VSReferenceKind.Unknown]), VSProjectName = projectName, VSContainingType = default(ByteStringValue) });
            id++;
            foreach (var reference in symbol.references)
            {
                if (symbol.definition.symbol != null && isDeclarationOfSymbol(reference.node!, symbol.definition.symbol))
                    continue;
                var (location, mapped) = getLocationOfEntryForFeature(reference, Feature.References);
                if (!mapped)
                    continue;
                var kind = VSReferenceKind.Read;
                if (reference.kind != EntryKind.Range && reference.node != null && Ast.IsWriteAccessForReference(reference.node))
                    kind = VSReferenceKind.Write;
                items = items.Append(new VSReferenceItem { VSId = id, VSDefinitionId = definitionId, VSLocation = location, VSKind = Slice<VSReferenceKind>.FromArray([kind]), VSProjectName = projectName });
                id++;
            }
        }

        return (new VSReferenceItemsOrNull { VSReferenceItems = items }, null);
    }

    private sealed class ReferencedSymbolDefinitionInfo
    {
        internal Node node = null!;
        internal Location location = null!;
        internal VSClassifiedTextElement displayText = null!;
    }

    // findallreferences.go:873. Each case retains its actual node/location source;
    // unsupported or unmappable definitions are absent, not synthetic locations.
    private ReferencedSymbolDefinitionInfo? definitionToReferencedSymbolDefinitionInfo(ProtocolRequestContext context, ReferenceDefinition definition, Node originalNode, bool vsCapability, Feature feature)
    {
        switch (definition.Kind)
        {
            case DefinitionKind.Symbol:
            {
                var symbol = definition.symbol;
                if (symbol == null)
                    return null;
                var element = getDefinitionKindAndDisplayParts(context, symbol, originalNode, vsCapability);
                var node = symbol.Declarations.Count > 0 ? symbol.Declarations[0].Name() ?? symbol.Declarations[0] : originalNode;
                var (location, mapped) = getLocationOfEntryForFeature(new ReferenceEntry { kind = EntryKind.Node, node = node }, feature);
                return mapped ? new ReferencedSymbolDefinitionInfo
                {
                    node = node,
                    location = location,
                    displayText = element
                }

                : null;
            }

            case DefinitionKind.Label:
            {
                var node = definition.node;
                if (node == null)
                    return null;
                var (location, mapped) = getLocationOfEntryForFeature(new ReferenceEntry { kind = EntryKind.Node, node = node }, feature);
                if (!mapped)
                    return null;
                return new ReferencedSymbolDefinitionInfo
                {
                    node = node,
                    location = location,
                    displayText = new VSClassifiedTextElement
                    {
                        Runs = Slice<VSClassifiedTextRun?>.FromArray([new VSClassifiedTextRun { Text = node.TextValue(), ClassificationTypeName = ClassificationTypeName.Text.Value }])
                    }
                };
            }

            case DefinitionKind.Keyword:
            {
                var node = definition.node;
                if (node == null)
                    return null;
                var (location, mapped) = getLocationOfEntryForFeature(new ReferenceEntry { kind = EntryKind.Node, node = node }, feature);
                if (!mapped)
                    return null;
                return new ReferencedSymbolDefinitionInfo
                {
                    node = node,
                    location = location,
                    displayText = new VSClassifiedTextElement
                    {
                        Runs = Slice<VSClassifiedTextRun?>.FromArray([new VSClassifiedTextRun { Text = ByteStringValue.FromString(TypeScript.Scanner.Scanner.TokenToString(node.Kind)), ClassificationTypeName = ClassificationTypeName.Keyword.Value }])
                    }
                };
            }

            case DefinitionKind.This:
            {
                var node = definition.node;
                var symbol = definition.symbol;
                if (node == null || symbol == null)
                    return null;
                var element = getDefinitionKindAndDisplayParts(context, symbol, node, vsCapability);
                var (location, mapped) = getLocationOfEntryForFeature(new ReferenceEntry { kind = EntryKind.Node, node = node }, feature);
                return mapped ? new ReferencedSymbolDefinitionInfo
                {
                    node = node,
                    location = location,
                    displayText = element
                }

                : null;
            }

            case DefinitionKind.String:
            {
                var node = definition.node;
                if (node == null)
                    return null;
                var (location, mapped) = getLocationOfEntryForFeature(new ReferenceEntry { kind = EntryKind.Node, node = node }, feature);
                if (!mapped)
                    return null;
                return new ReferencedSymbolDefinitionInfo
                {
                    node = node,
                    location = location,
                    displayText = new VSClassifiedTextElement
                    {
                        Runs = Slice<VSClassifiedTextRun?>.FromArray([new VSClassifiedTextRun { Text = node.TextValue(), ClassificationTypeName = ClassificationTypeName.String.Value }])
                    }
                };
            }

            case DefinitionKind.TripleSlashReference:
            {
                var reference = definition.tripleSlashFileRef;
                if (reference == null || reference.file == null)
                    return null;
                var node = reference.file.AsNode();
                var (location, mapped) = getLocationOfEntryForFeature(new ReferenceEntry { kind = EntryKind.Node, node = node }, feature);
                if (!mapped)
                    return null;
                return new ReferencedSymbolDefinitionInfo
                {
                    node = node,
                    location = location,
                    displayText = new VSClassifiedTextElement
                    {
                        Runs = Slice<VSClassifiedTextRun?>.FromArray([new VSClassifiedTextRun { Text = ByteStringValue.Concat(ByteStringValue.FromString("\""), reference.reference.FileNameValue, ByteStringValue.FromString("\"")), ClassificationTypeName = ClassificationTypeName.String.Value }])
                    }
                };
            }

            default:
                return null;
        }
    }

    // findallreferences.go:997. Raw display output stays a borrowed header up to
    // the protocol owner, and the checker request lease is always released.
    private VSClassifiedTextElement getDefinitionKindAndDisplayParts(ProtocolRequestContext context, Symbol symbol, Node originalNode, bool vsCapability)
    {
        var program = GetProgram();
        var (checker, done) = program.GetTypeChecker(context);
        try
        {
            var meaning = getIntersectingMeaningFromDeclarations(originalNode, symbol, SemanticMeaning.All);
            var info = getQuickInfoAndDeclarationAtLocation(checker, symbol, originalNode, null, vsCapability, meaning);
            if (vsCapability)
                return new VSClassifiedTextElement
                {
                    Runs = info.DisplayParts.GetRuns()
                };
            return new VSClassifiedTextElement
            {
                Runs = Slice<VSClassifiedTextRun?>.FromArray([new VSClassifiedTextRun { Text = info.DisplayParts.TextValue(), ClassificationTypeName = ClassificationTypeName.Text.Value }])
            };
        }
        finally
        {
            done();
        }
    }

    // findallreferences.go:1016.
    internal (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) ProvideImplementations(ProtocolRequestContext context, ImplementationParams parameters, ICrossProjectOrchestrator? orchestrator) => provideImplementationsEx(context, parameters, default, orchestrator);
    // findallreferences.go:1020.
    private (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) provideImplementationsEx(ProtocolRequestContext context, ImplementationParams parameters, SymbolEntryTransformOptions options, ICrossProjectOrchestrator? orchestrator) => handleCrossProject(context, parameters, orchestrator, static (service, ctx, request, data, transformOptions) => service.symbolAndEntriesToImplementations(ctx, request, data, transformOptions), combineImplementations, false, true, options, null);
    // findallreferences.go:1034.
    private (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) provideImplementationsFromData(ProtocolRequestContext context, ImplementationParams parameters, SymbolEntryTransformOptions options, ICrossProjectOrchestrator? orchestrator, SymbolAndEntriesData data) => handleCrossProject(context, parameters, orchestrator, static (service, ctx, request, symbols, transformOptions) => service.symbolAndEntriesToImplementations(ctx, request, symbols, transformOptions), combineImplementations, false, true, options, new SymbolAndEntriesDataOwner(data));
    // findallreferences.go:1048.
    private (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) symbolAndEntriesToImplementations(ProtocolRequestContext context, ImplementationParams parameters, SymbolAndEntriesData data, SymbolEntryTransformOptions options)
    {
        HashSet<Node?>? seen = null;
        Slice<ReferenceEntry> entries = default;
        foreach (var group in data.SymbolsAndEntries)
            foreach (var reference in group.references)
                if ((seen ??= new()).Add(reference.node) && (!options.dropOriginNodes || !reference.node!.Loc.ContainsInclusive(data.Position)))
                    entries = entries.Append(reference);
        if (!options.requireLocationsResult && context.ClientCapabilities.TextDocument.Implementation.LinkSupport)
            return (new LocationOrLocationsOrDefinitionLinksOrNull { DefinitionLinks = convertEntriesToLocationLinks(entries, Feature.Implementation) }, null);
        return (new LocationOrLocationsOrDefinitionLinksOrNull { Locations = convertEntriesToLocations(entries, Feature.Implementation) }, null);
    }

    // findallreferences.go:1068. Core.Filter retains its unchanged header fast path.
    private Slice<Location> convertSymbolAndEntriesToLocations(SymbolAndEntries symbolAndEntries, bool includeDeclarations, Feature feature)
    {
        var references = symbolAndEntries.references;
        // Original !!! includeDeclarations handling.
        if (!includeDeclarations && symbolAndEntries.definition != null)
            references = Core.Filter(references, symbolAndEntries.definition.symbol, static (entry, symbol) => !isDeclarationOfSymbol(entry.node, symbol));
        return convertEntriesToLocations(references, feature);
    }

    // findallreferences.go:1081.
    private static bool isDeclarationOfSymbol(Node? node, Symbol? target)
    {
        if (node == null || target == null)
            return false;
        Node? source = null;
        var declaration = Ast.GetDeclarationFromName(node);
        if (declaration != null)
            source = declaration;
        else if (node.Kind == Kind.DefaultKeyword)
            source = node.Parent;
        else if (Ast.IsLiteralComputedPropertyDeclarationName(node))
            source = node.Parent.Parent;
        else if (node.Kind == Kind.ConstructorKeyword && Ast.IsConstructorDeclaration(node.Parent))
            source = node.Parent.Parent;
        // Original !!! CommonJS binary declaration adjustment is not implemented.
        return source != null && Core.Some(target.Declarations, source, static (candidate, owner) => candidate == owner);
    }

    // findallreferences.go:1105. Keep the original explicit result capacity.
    private Slice<Location> convertEntriesToLocations(Slice<ReferenceEntry> entries, Feature feature)
    {
        var locations = Slice<Location>.Make(0, entries.Length);
        foreach (var entry in entries)
        {
            var (location, ok) = getLocationOfEntryForFeature(entry, feature);
            if (ok)
                locations = locations.Append(location);
        }

        return locations;
    }

    // findallreferences.go:1116. Context projection may widen only the same URI.
    private Slice<LocationLink?> convertEntriesToLocationLinks(Slice<ReferenceEntry> entries, Feature feature)
    {
        var links = Slice<LocationLink?>.Make(0, entries.Length);
        foreach (var entry in entries)
        {
            var (location, ok) = getLocationOfEntryForFeature(entry, feature);
            if (!ok)
                continue;
            var targetSelectionRange = location.Range;
            var targetRange = targetSelectionRange;
            if (entry.node != null)
            {
                var contextTextRange = toContextRange(entry.textRange, entry.sourceFile!, entry.context);
                if (contextTextRange != null)
                {
                    var (contextLocation, fidelity) = sourceFileRangeToLSPLocationForFeature(entry.sourceFile!, contextTextRange.Value, feature);
                    if (!fidelity.IsNone() && contextLocation.Uri == location.Uri)
                        targetRange = contextLocation.Range;
                }
            }

            links = links.Append(new LocationLink { TargetUri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(entry.sourceFile!.OriginalFileName()), TargetRange = targetRange, TargetSelectionRange = targetSelectionRange });
        }

        return links;
    }

    // findallreferences.go:1149. Stable sort mutates the appended header exactly
    // as upstream; the first nonempty group header is borrowed, not cloned.
    private Slice<SymbolAndEntries> mergeReferences(TypeScript.Compiler.Program program, params Slice<SymbolAndEntries>[] groups)
    {
        var result = Slice<SymbolAndEntries>.Make(0);
        int fileIndex(ReferenceEntry entry)
        {
            resolveEntrySource(entry);
            var files = program.SourceFiles();
            for (int i = 0; i < files.Count; i++)
                if (files[i] == entry.sourceFile)
                    return i;
            return -1;
        }

        foreach (var group in groups)
        {
            if (group.Count == 0)
                continue;
            if (result.Count == 0)
            {
                result = group;
                continue;
            }

            foreach (var entry in group)
            {
                if (entry.definition == null || entry.definition.Kind != DefinitionKind.Symbol)
                {
                    result = result.Append(entry);
                    continue;
                }

                int index = -1;
                for (int i = 0; i < result.Count; i++)
                    if (result[i].definition != null && result[i].definition!.Kind == DefinitionKind.Symbol && result[i].definition!.symbol == entry.definition.symbol)
                    {
                        index = i;
                        break;
                    }

                if (index == -1)
                {
                    result = result.Append(entry);
                    continue;
                }

                var reference = result[index];
                var sorted = reference.references.Append(entry.references);
                GoSlices.SortStableFunc(sorted, (first, second) =>
                {
                    int firstFile = fileIndex(first), secondFile = fileIndex(second);
                    return firstFile != secondFile ? firstFile.CompareTo(secondFile) : ProtocolUtilities.CompareRanges(getRangeOfEntry(first), getRangeOfEntry(second));
                });
                result[index] = new SymbolAndEntries
                {
                    definition = reference.definition,
                    references = sorted
                };
            }
        }

        return result;
    }

    // findallreferences.go:1202. Public query preserves the actual program owner.
    internal Slice<SymbolAndEntries> GetReferencedSymbolsForNode(ProtocolRequestContext context, int position, Node node, Slice<SourceFile> sourceFiles) => getReferencedSymbolsForNode(context, position, node, program, sourceFiles, new RefOptions { use = ReferenceUse.References });
    // findallreferences.go:1218. The explicitly made declaration-name map is
    // retained once for the query; result entries preserve original AST identity.
    internal Slice<SignatureUsage> GetSignatureUsages(ProtocolRequestContext context, Node signatureDeclaration)
    {
        var name = signatureDeclaration.Name();
        if (name == null || !Ast.IsIdentifier(name))
            return default;
        var sourceFiles = program.GetSourceFiles();
        var entries = GetReferencedSymbolsForNode(context, name.Pos(), name, sourceFiles);
        var declarationNames = new HashSet<Node>();
        foreach (var entry in entries)
            if (entry.definition?.symbol != null)
                foreach (var declaration in entry.definition.symbol.Declarations)
                    if (declaration.Name() is { } declarationName)
                        declarationNames.Add(declarationName);
        Slice<SignatureUsage> result = default;
        foreach (var entry in entries)
        {
            foreach (var reference in entry.References())
            {
                if (!reference.IsNodeEntry())
                    continue;
                var node = reference.Node();
                if (node == null || declarationNames.Contains(node))
                    continue;
                var called = Ast.ClimbPastPropertyAccess(node);
                Node? callExpression = null;
                if (called.Parent != null && Ast.IsCallExpression(called.Parent) && called.Parent.Expression() == called)
                    callExpression = called.Parent;
                result = result.Append(new SignatureUsage { Name = node, Call = callExpression });
            }
        }

        return result;
    }

    // findallreferences.go:1269. The source file admission set is a genuine
    // initialized hinted owner, distinct from lazily backed zero sets.
    private Slice<SymbolAndEntries> getReferencedSymbolsForNode(ProtocolRequestContext context, int position, Node node, TypeScript.Compiler.Program program, Slice<SourceFile> sourceFiles, RefOptions options)
    {
        var sourceFilesSet = new HashSet<RootedFilePath>(sourceFiles.Length);
        foreach (var file in sourceFiles)
            sourceFilesSet.Add(RootedFilePath.FromValue(file.FileNameGoValue()));
        if (options.use == ReferenceUse.References || options.use == ReferenceUse.Rename)
            node = getAdjustedLocation(node, options.use == ReferenceUse.Rename, Ast.GetSourceFileOfNode(node));
        var (checker, done) = program.GetTypeChecker(context);
        try
        {
            if (node.Kind == Kind.SourceFile)
            {
                var resolvedReference = getReferenceAtPosition(node.AsSourceFile(), position, program);
                if (resolvedReference?.file == null)
                    return default;
                var moduleSymbol = checker.GetMergedSymbol(resolvedReference.file.Symbol);
                if (moduleSymbol != null)
                    return getReferencedSymbolsForModule(checker, program, moduleSymbol, false, sourceFiles, sourceFilesSet);
                // Original !!! file-include reasons are not implemented.
                return Slice<SymbolAndEntries>.FromArray([new SymbolAndEntries { definition = new ReferenceDefinition { Kind = DefinitionKind.TripleSlashReference, tripleSlashFileRef = new TripleSlashDefinition { reference = resolvedReference.reference! } }, references = getReferencesForNonModule(resolvedReference.file, program) }]);
            }

            if (!options.implementations)
            {
                var special = getReferencedSymbolsSpecial(node, sourceFiles);
                if (!special.IsNil)
                    return special;
            }

            var symbol = checker.GetSymbolAtLocation(node.Kind == Kind.Constructor && node.Parent.Name() != null ? node.Parent.Name() : node);
            if (symbol == null)
            {
                if (!options.implementations && Ast.IsStringLiteralLike(node))
                {
                    if (isModuleSpecifierLike(node))
                    {
                    // Original !!! resolved-module file-include reason lookup
                    // is unimplemented; fall through to string-literal references.
                    }

                    return getReferencesForStringLiteral(context, node, sourceFiles, checker);
                }

                return default;
            }

            if (symbol.NameValue.EqualsValue(ByteStringValue.FromString(Ast.InternalSymbolNameExportEquals)))
            {
                if (symbol.Parent == null)
                    return default;
                return getReferencedSymbolsForModule(checker, program, symbol.Parent, false, sourceFiles, sourceFilesSet);
            }

            var moduleReferences = getReferencedSymbolsForModuleIfDeclaredBySourceFile(context, symbol, program, sourceFiles, checker, options, sourceFilesSet);
            if (!moduleReferences.IsNil && (symbol.Flags & SymbolFlags.Transient) == 0)
                return moduleReferences;
            var aliasedSymbol = getMergedAliasedSymbolOfNamespaceExportDeclaration(node, symbol, checker);
            var moduleReferencesOfExportTarget = getReferencedSymbolsForModuleIfDeclaredBySourceFile(context, aliasedSymbol, program, sourceFiles, checker, options, sourceFilesSet);
            var references = getReferencedSymbolsForSymbol(context, program, symbol, node, sourceFiles, sourceFilesSet, checker, options);
            return mergeReferences(program, moduleReferences, references, moduleReferencesOfExportTarget);
        }
        finally
        {
            done();
        }
    }

    // findallreferences.go:1394.
    private static bool isStringLiteralPropertyReference(Node node, Checker checker) => Ast.IsPropertySignatureDeclaration(node.Parent) && checker.GetPropertyOfType(checker.GetTypeAtLocation(node.Parent.Parent), node.TextValue()) != null;
    // findallreferences.go:1401.
    private Slice<SymbolAndEntries> getReferencedSymbolsForModuleIfDeclaredBySourceFile(ProtocolRequestContext context, Symbol? symbol, TypeScript.Compiler.Program program, Slice<SourceFile> sourceFiles, Checker checker, RefOptions options, HashSet<RootedFilePath> sourceFilesSet)
    {
        if (symbol == null || (symbol.Flags & SymbolFlags.Module) == 0 || symbol.Declarations.Count == 0)
            return default;
        SourceFile? moduleFile = null;
        foreach (var declaration in symbol.Declarations)
            if (Ast.IsSourceFile(declaration))
            {
                moduleFile = declaration.AsSourceFile();
                break;
            }

        if (moduleFile == null)
            return default;
        var exportEquals = symbol.Exports?[Ast.InternalSymbolNameExportEquals];
        var moduleReferences = getReferencedSymbolsForModule(checker, program, symbol, exportEquals != null, sourceFiles, sourceFilesSet);
        if (exportEquals == null || (exportEquals.Flags & SymbolFlags.Alias) == 0 || !sourceFilesSet.Contains(RootedFilePath.FromValue(moduleFile.FileNameGoValue())))
            return moduleReferences;
        var (resolved, _) = checker.ResolveAlias(exportEquals);
        return mergeReferences(program, moduleReferences, getReferencedSymbolsForSymbol(context, program, resolved!, null, sourceFiles, sourceFilesSet, checker, options));
    }

    // findallreferences.go:1421.
    private static Slice<SymbolAndEntries> getReferencedSymbolsSpecial(Node node, Slice<SourceFile> sourceFiles)
    {
        if (isTypeKeyword(node.Kind))
        {
            if (node.Kind == Kind.VoidKeyword && node.Parent.Kind == Kind.VoidExpression)
                return default;
            if (node.Kind == Kind.ReadonlyKeyword && !isReadonlyTypeOperator(node))
                return default;
            return getAllReferencesForKeyword(sourceFiles, node.Kind, node.Kind == Kind.ReadonlyKeyword);
        }

        if (Ast.IsImportMeta(node.Parent) && node.Parent.Name() == node)
            return getAllReferencesForImportMeta(sourceFiles);
        if (node.Kind == Kind.StaticKeyword && node.Parent.Kind == Kind.ClassStaticBlockDeclaration)
            return Slice<SymbolAndEntries>.FromArray([SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.Keyword, node, null, Slice<ReferenceEntry>.FromArray([newNodeEntry(node)]))]);
        if (Ast.IsJumpStatementTarget(node))
        {
            var label = getTargetLabel(node.Parent, node.TextValue());
            return label != null ? getLabelReferencesInNode(label.Parent, label) : default;
        }

        if (Ast.IsLabelOfLabeledStatement(node))
            return getLabelReferencesInNode(node.Parent, node);
        if (isThis(node))
            return getReferencesForThisKeyword(node, sourceFiles);
        if (node.Kind == Kind.SuperKeyword)
            return getReferencesForSuperKeyword(node);
        return default;
    }

    // findallreferences.go:1477.
    private static Slice<SymbolAndEntries> getLabelReferencesInNode(Node container, Node targetLabel)
    {
        var file = Ast.GetSourceFileOfNode(container);
        var labelName = targetLabel.TextValue();
        var references = Core.MapNonNilSlice(getPossibleSymbolReferenceNodes(file, labelName, container), (targetLabel, labelName), static (node, state) => node == state.targetLabel || (Ast.IsJumpStatementTarget(node) && getTargetLabel(node, state.labelName) == state.targetLabel) ? newNodeEntry(node) : null);
        return Slice<SymbolAndEntries>.FromArray([SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.Label, targetLabel, null, references)]);
    }

    // findallreferences.go:1490.
    private static Slice<SymbolAndEntries> getReferencesForThisKeyword(Node keyword, Slice<SourceFile> sourceFiles)
    {
        var searchSpace = Ast.GetThisContainer(keyword, false, false);
        var staticFlag = ModifierFlags.Static;
        static bool isParameterName(Node node) => node.Kind == Kind.Identifier && node.Parent.Kind == Kind.Parameter && node.Parent.Name() == node;
        switch (searchSpace.Kind)
        {
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
                if ((searchSpace.Kind is Kind.MethodDeclaration or Kind.MethodSignature) && Ast.IsObjectLiteralMethod(searchSpace))
                {
                    staticFlag &= searchSpace.ModifierFlags();
                    searchSpace = searchSpace.Parent;
                    break;
                }

                staticFlag &= searchSpace.ModifierFlags();
                searchSpace = searchSpace.Parent;
                break;
            case Kind.SourceFile:
                if (Ast.IsExternalModule(searchSpace.AsSourceFile()) || isParameterName(keyword))
                    return default;
                break;
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
                break;
            default:
                return default;
        }

        var files = sourceFiles;
        if (searchSpace.Kind != Kind.SourceFile)
            files = Slice<SourceFile>.FromArray([Ast.GetSourceFileOfNode(searchSpace)]);
        Slice<Node> nodes = default;
        foreach (var file in files)
        {
            var candidates = getPossibleSymbolReferenceNodes(file, ByteStringValue.FromString("this"), searchSpace.Kind == Kind.SourceFile ? file.AsNode() : searchSpace);
            var accepted = Core.Filter(candidates, (searchSpace, staticFlag), static (node, state) =>
            {
                if (!isThis(node))
                    return false;
                var container = Ast.GetThisContainer(node, false, false);
                if (!Ast.CanHaveSymbol(container))
                    return false;
                switch (state.searchSpace.Kind)
                {
                    case Kind.FunctionExpression:
                    case Kind.FunctionDeclaration:
                        return state.searchSpace.Symbol() == container.Symbol();
                    case Kind.MethodDeclaration:
                    case Kind.MethodSignature:
                        return Ast.IsObjectLiteralMethod(state.searchSpace) && state.searchSpace.Symbol() == container.Symbol();
                    case Kind.ClassExpression:
                    case Kind.ClassDeclaration:
                    case Kind.ObjectLiteralExpression:
                        return container.Parent != null && Ast.CanHaveSymbol(container.Parent) && state.searchSpace.Symbol() == container.Parent.Symbol() && Ast.IsStatic(container) == (state.staticFlag != ModifierFlags.None);
                    case Kind.SourceFile:
                        return container.Kind == Kind.SourceFile && !Ast.IsExternalModule(container.AsSourceFile()) && !isParameterName(node);
                    default:
                        return false;
                }
            });
            nodes = nodes.Append(accepted);
        }

        var references = Core.MapSlice(nodes, static node => newNodeEntry(node));
        Node? parameter = null;
        foreach (var reference in references)
            if (reference.node!.Parent.Kind == Kind.Parameter)
            {
                parameter = reference.node;
                break;
            }

        parameter ??= keyword;
        return Slice<SymbolAndEntries>.FromArray([SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.This, parameter, searchSpace.Symbol(), references)]);
    }

    // findallreferences.go:1568.
    private static Slice<SymbolAndEntries> getReferencesForSuperKeyword(Node keyword)
    {
        var searchSpace = Ast.GetSuperContainer(keyword, false);
        if (searchSpace == null)
            return default;
        var staticFlag = ModifierFlags.Static;
        switch (searchSpace.Kind)
        {
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
                staticFlag &= searchSpace.ModifierFlags();
                searchSpace = searchSpace.Parent;
                break;
            default:
                return default;
        }

        var file = Ast.GetSourceFileOfNode(searchSpace);
        var references = Core.MapNonNilSlice(getPossibleSymbolReferenceNodes(file, ByteStringValue.FromString("super"), searchSpace), (searchSpace, staticFlag), static (node, state) =>
        {
            if (node.Kind != Kind.SuperKeyword)
                return null;
            var container = Ast.GetSuperContainer(node, false);
            return container != null && Ast.IsStatic(container) == (state.staticFlag != ModifierFlags.None) && container.Parent.Symbol() == state.searchSpace.Symbol() ? newNodeEntry(node) : null;
        });
        return Slice<SymbolAndEntries>.FromArray([SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.Symbol, null, searchSpace.Symbol(), references)]);
    }

    // findallreferences.go:1604.
    private static Slice<SymbolAndEntries> getAllReferencesForImportMeta(Slice<SourceFile> sourceFiles)
    {
        Slice<ReferenceEntry> references = default;
        foreach (var file in sourceFiles)
            references = references.Append(Core.MapNonNilSlice(getPossibleSymbolReferenceNodes(file, ByteStringValue.FromString("meta"), file.AsNode()), static node => Ast.IsImportMeta(node.Parent) ? newNodeEntry(node.Parent) : null!));
        if (references.Count == 0)
            return default;
        return Slice<SymbolAndEntries>.FromArray([SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.Keyword, references[0].node, null, references)]);
    }

    // findallreferences.go:1620.
    private static Slice<SymbolAndEntries> getAllReferencesForKeyword(Slice<SourceFile> sourceFiles, Kind kind, bool filterReadonly)
    {
        Slice<ReferenceEntry> references = default;
        var text = ByteStringValue.FromString(TypeScript.Scanner.Scanner.TokenToString(kind));
        foreach (var file in sourceFiles)
            references = references.Append(Core.MapNonNilSlice(getPossibleSymbolReferenceNodes(file, text, file.AsNode()), (kind, filterReadonly), static (node, state) => node.Kind == state.kind && (!state.filterReadonly || isReadonlyTypeOperator(node)) ? newNodeEntry(node) : null));
        if (references.Count == 0)
            return default;
        return Slice<SymbolAndEntries>.FromArray([SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.Keyword, references[0].node, null, references)]);
    }

    // findallreferences.go:1637. Mapping retains the original required result copy.
    private static Slice<Node> getPossibleSymbolReferenceNodes(SourceFile sourceFile, ByteStringValue symbolName, Node? container)
    {
        return Core.MapNonNilSlice<int, Node>(getPossibleSymbolReferencePositions(sourceFile, symbolName, container), position =>
        {
            var referenceLocation = AstNav.GetTouchingPropertyName(sourceFile, position);
            return referenceLocation != sourceFile.AsNode() ? referenceLocation : null!;
        });
    }

    // findallreferences.go:1646. Pinned source compares adjacent bytes as runes and
    // retains its initial substring-relative index; do not alter either behavior.
    private static Slice<int> getPossibleSymbolReferencePositions(SourceFile sourceFile, ByteStringValue symbolName, Node? container)
    {
        var positions = Slice<int>.Make(0);
        // Original TODO: cache symbol existence and support Unicode escapes.
        if (symbolName.Length == 0)
            return positions;
        var text = ByteStringValue.FromView(sourceFile.TextView());
        int sourceLength = text.Length;
        int symbolNameLength = symbolName.Length;
        container ??= sourceFile.AsNode();
        int position = text.Slice(container.Pos()).IndexOf(symbolName);
        int endPositionOfContainer = container.End();
        while (position >= 0 && position < endPositionOfContainer)
        {
            int endPosition = position + symbolNameLength;
            if ((position == 0 || !TypeScript.Scanner.Scanner.IsIdentifierPart(text[position - 1])) && (endPosition == sourceLength || !TypeScript.Scanner.Scanner.IsIdentifierPart(text[endPosition])))
                positions = positions.Append(position);
            int startIndex = position + symbolNameLength + 1;
            if (startIndex > text.Length)
                break;
            int foundIndex = text.Slice(startIndex).IndexOf(symbolName);
            if (foundIndex == -1)
                break;
            position = startIndex + foundIndex;
        }

        return positions;
    }

    // findallreferences.go:1692. Cache only the recursive child callback; scoped
    // frame restoration retains the original per-call result local and early stop.
    private static Node? findFirstJsxNode(Node root) => new FirstJsxVisitor().Visit(root);
    private sealed class FirstJsxVisitor
    {
        private readonly Visitor child;
        private Node? result;
        internal FirstJsxVisitor() => child = VisitChild;
        internal Node? Visit(Node node)
        {
            if (node.Kind is Kind.JsxElement or Kind.JsxSelfClosingElement or Kind.JsxFragment)
                return node;
            if ((node.SubtreeFacts() & SubtreeFacts.ContainsJsx) == 0)
                return null;
            var saved = result;
            result = null;
            try
            {
                node.ForEachChild(child);
                return result;
            }
            finally
            {
                result = saved;
            }
        }

        private bool VisitChild(Node node)
        {
            result = Visit(node);
            return result != null;
        }
    }

    // findallreferences.go:1718. This nil-independent empty result is an existing
    // upstream TODO, not a substitute for the full module reference search.
    private static Slice<ReferenceEntry> getReferencesForNonModule(SourceFile referencedFile, TypeScript.Compiler.Program program) => Slice<ReferenceEntry>.Make(0);
    // findallreferences.go:1723.
    private static Symbol? getMergedAliasedSymbolOfNamespaceExportDeclaration(Node node, Symbol symbol, Checker checker)
    {
        if (node.Parent != null && node.Parent.Kind == Kind.NamespaceExportDeclaration)
        {
            var (aliasedSymbol, ok) = checker.ResolveAlias(symbol);
            if (ok)
            {
                var targetSymbol = checker.GetMergedSymbol(aliasedSymbol);
                if (aliasedSymbol != targetSymbol)
                    return targetSymbol;
            }
        }

        return null;
    } // findallreferences.go:1354.

    private Slice<SymbolAndEntries> getReferencesForStringLiteral(ProtocolRequestContext context, Node node, Slice<SourceFile> sourceFiles, Checker checker)
    {
        var type = getContextualTypeFromParentOrAncestorTypeNode(node, checker);
        Slice<ReferenceEntry> references = default;
        foreach (var sourceFile in sourceFiles)
        {
            if (context.Cancellation.IsCancellationRequested)
                continue;
            Slice<ReferenceEntry> entries = default;
            foreach (var reference in getPossibleSymbolReferenceNodes(sourceFile, node.TextValue(), null))
                if (Ast.IsStringLiteralLike(reference) && reference.TextValue().EqualsValue(node.TextValue()))
                {
                    if (type != null)
                    {
                        var referenceType = getContextualTypeFromParentOrAncestorTypeNode(reference, checker);
                        if (type != checker.GetStringType() && (type == referenceType || isStringLiteralPropertyReference(reference, checker)))
                            entries = entries.Append(newNodeEntryWithKind(reference, EntryKind.StringLiteral));
                    }
                    else
                    {
                        if (Ast.IsNoSubstitutionTemplateLiteral(reference) && !PrinterUtilities.RangeIsOnSingleLine(reference.Loc, sourceFile))
                            continue;
                        entries = entries.Append(newNodeEntryWithKind(reference, EntryKind.StringLiteral));
                    }
                }

            references = references.Append(entries);
        }

        return Slice<SymbolAndEntries>.FromArray([new SymbolAndEntries { definition = new ReferenceDefinition { Kind = DefinitionKind.String, node = node }, references = references }]);
    }

    // findallreferences.go:1735. Explicit, implicit runtime and triple-slash
    // references retain their distinct original location producers.
    private Slice<SymbolAndEntries> getReferencedSymbolsForModule(Checker checker, TypeScript.Compiler.Program program, Symbol symbol, bool excludeImportTypeOfExportEquals, Slice<SourceFile> sourceFiles, HashSet<RootedFilePath> sourceFilesSet)
    {
        Debug.Assert(symbol.ValueDeclaration != null);
        var references = Core.MapNonNilSlice(findModuleReferences(program, sourceFiles, symbol, checker), excludeImportTypeOfExportEquals, static (reference, exclude) =>
        {
            switch (reference.kind)
            {
                case ModuleReferenceKind.Import:
                    var parent = reference.literal!.Parent;
                    if (Ast.IsLiteralTypeNode(parent) && Ast.IsImportTypeNode(parent.Parent))
                        if (exclude && parent.Parent.AsImportTypeNode().Qualifier == null)
                            return null;
                    return newNodeEntry(reference.literal);
                case ModuleReferenceKind.Implicit:
                    Node? rangeNode = null;
                    if (!reference.literal!.TextValue().EqualsString("tslib"))
                        rangeNode = findFirstJsxNode(reference.referencingFile!.AsNode());
                    if (rangeNode == null)
                    {
                        var statements = reference.referencingFile!.Statements;
                        rangeNode = statements != null && statements.Nodes.Count > 0 ? statements.Nodes[0] : reference.referencingFile.AsNode();
                    }

                    return newNodeEntry(rangeNode);
                case ModuleReferenceKind.Reference:
                    return new ReferenceEntry
                    {
                        kind = EntryKind.Range,
                        sourceFile = reference.referencingFile,
                        fileReferenceRange = reference.reference
                    };
                default:
                    return null;
            }
        });
        if (symbol.Declarations.Count > 0)
            foreach (var declaration in symbol.Declarations)
                if (declaration.Kind == Kind.ModuleDeclaration && sourceFilesSet.Contains(RootedFilePath.FromValue(Ast.GetSourceFileOfNode(declaration).FileNameGoValue())))
                    references = references.Append(newNodeEntry(declaration.AsModuleDeclaration().Name()));
        var exported = symbol.Exports?[Ast.InternalSymbolNameExportEquals];
        if (exported != null && exported.Declarations.Count > 0)
            foreach (var declaration in exported.Declarations)
            {
                var file = Ast.GetSourceFileOfNode(declaration);
                if (!sourceFilesSet.Contains(RootedFilePath.FromValue(file.FileNameGoValue())))
                    continue;
                Node? node;
                if (Ast.IsBinaryExpression(declaration) && Ast.IsPropertyAccessExpression(declaration.AsBinaryExpression().Left))
                    node = declaration.AsBinaryExpression().Left.Expression();
                else if (Ast.IsExportAssignment(declaration))
                {
                    node = AstNav.FindChildOfKind(declaration, Kind.ExportKeyword, file);
                    Debug.Assert(node != null, "Expected to find export keyword");
                }
                else
                    node = Ast.GetNameOfDeclaration(declaration) ?? declaration;
                references = references.Append(newNodeEntry(node!));
            }

        return references.Count > 0 ? Slice<SymbolAndEntries>.FromArray([new SymbolAndEntries { definition = new ReferenceDefinition { Kind = DefinitionKind.Symbol, symbol = symbol }, references = references }]) : Slice<SymbolAndEntries>.Make(0);
    }

    // findallreferences.go:1835.
    private static string getSpecialSearchKind(Node? node)
    {
        if (node == null)
            return "none";
        switch (node.Kind)
        {
            case Kind.Constructor:
            case Kind.ConstructorKeyword:
                return "constructor";
            case Kind.Identifier:
                if (Ast.IsClassLike(node.Parent))
                {
                    Debug.Assert(node.Parent.Name() == node);
                    return "class";
                }

                break;
        }

        return "none";
    }

    // findallreferences.go:1853.
    private static Slice<SymbolAndEntries> getReferencedSymbolsForSymbol(ProtocolRequestContext context, TypeScript.Compiler.Program program, Symbol originalSymbol, Node? node, Slice<SourceFile> sourceFiles, HashSet<RootedFilePath> sourceFilesSet, Checker checker, RefOptions options)
    {
        var symbol = skipPastExportOrImportSpecifierOrUnion(originalSymbol, node, checker, !isForRenameWithPrefixAndSuffixText(options)) ?? originalSymbol;
        var meaning = SemanticMeaning.All;
        if (options.use != ReferenceUse.Rename)
            meaning = getIntersectingMeaningFromDeclarations(node, symbol, SemanticMeaning.All);
        var state = newState(context, program, sourceFiles, sourceFilesSet, node, checker, meaning, options);
        Node? exportSpecifier = null;
        if (isForRenameWithPrefixAndSuffixText(options) && symbol.Declarations.Count != 0)
            foreach (var declaration in symbol.Declarations)
                if (Ast.IsExportSpecifier(declaration))
                {
                    exportSpecifier = declaration;
                    break;
                }

        if (exportSpecifier != null)
            state.getReferencesAtExportSpecifier(exportSpecifier.Name(), symbol, exportSpecifier.AsExportSpecifier(), state.createSearch(node, originalSymbol, ImpExpKind.Unknown, default, default), true, true);
        else if (node != null && node.Kind == Kind.DefaultKeyword && symbol.NameValue.EqualsString(Ast.InternalSymbolNameDefault) && symbol.Parent != null)
        {
            state.addReference(node, symbol, EntryKind.Node);
            state.searchForImportsOfExport(node, symbol, new ExportInfo { exportingModuleSymbol = symbol.Parent, exportKind = ExportKind.Default });
        }
        else
        {
            var search = state.createSearch(node, symbol, ImpExpKind.Unknown, default, state.populateSearchSymbolSet(symbol, node, options.use == ReferenceUse.Rename, options.useAliasesForRename, options.implementations));
            state.getReferencesInContainerOrFiles(symbol, search);
        }

        return state.result;
    }

    // findallreferences.go:1890. Query-retained headers and includes callback.
    private sealed class RefSearch
    {
        internal ImpExpKind comingFrom;
        internal Symbol symbol = null!;
        internal ByteStringValue text, escapedText;
        internal Slice<Symbol> parents, allSearchSymbols;
        internal Func<Symbol, bool> includes = null!;
    }

    private readonly record struct InheritKey(Symbol symbol, Symbol parent);
    // Go *Set has a distinct owner even before its nullable backing map exists.
    private sealed class SeenSymbolSet
    {
        internal HashSet<Symbol>? values;
    }

    // findallreferences.go:1907. Maps explicitly initialized by newState remain
    // distinct from the zero-value sets, whose backing storage is lazy.
    private sealed class RefState
    {
        internal Slice<SourceFile> sourceFiles;
        internal HashSet<RootedFilePath> sourceFilesSet = null!;
        internal string specialSearchKind = null!;
        internal Checker checker = null!;
        internal ProtocolRequestContext context = null!;
        internal TypeScript.Compiler.Program program = null!;
        internal SemanticMeaning searchMeaning;
        internal RefOptions options;
        internal Slice<SymbolAndEntries> result;
        internal Dictionary<InheritKey, bool> inheritsFromCache = null!;
        internal HashSet<Node>? seenContainingTypeReferences, seenReExportRHS;
        internal ImportTracker? importTracker;
        internal Dictionary<Symbol, SymbolAndEntries> symbolToReferences = null!;
        internal Dictionary<SourceFile, SeenSymbolSet> sourceFileToSeenSymbols = null!;
        // findallreferences.go:1942.
        internal bool includesSourceFile(SourceFile file) => sourceFilesSet.Contains(RootedFilePath.FromValue(file.FileNameGoValue()));
        // findallreferences.go:1946.
        internal ImportsResult getImportSearches(Symbol symbol, ExportInfo info)
        {
            importTracker ??= createImportTracker(context, program, sourceFiles, sourceFilesSet, checker);
            return importTracker(symbol, info, options.use == ReferenceUse.Rename);
        }

        // findallreferences.go:1954. The query's callback captures the retained
        // allSearchSymbols header once, preserving identity membership semantics.
        internal RefSearch createSearch(Node? location, Symbol symbol, ImpExpKind comingFrom, ByteStringValue text, Slice<Symbol> allSearchSymbols)
        {
            if (text.Length == 0)
            {
                var local = NameResolver.GetLocalSymbolForExportDefault(symbol) ?? getNonModuleSymbolOfMergedModuleSymbol(symbol) ?? symbol;
                var name = Ast.SymbolNameValue(local);
                var (moduleName, isAmbient) = Ast.TryGetAmbientModuleNameValueFromSymbolName(name);
                text = isAmbient ? moduleName : name;
            }

            if (allSearchSymbols.Count == 0)
                allSearchSymbols = Slice<Symbol>.FromArray([symbol]);
            var search = new RefSearch
            {
                symbol = symbol,
                comingFrom = comingFrom,
                text = text,
                escapedText = text,
                allSearchSymbols = allSearchSymbols,
                includes = candidate =>
                {
                    foreach (var item in allSearchSymbols)
                        if (item == candidate)
                            return true;
                    return false;
                }
            };
            if (options.implementations && location != null)
                search.parents = getParentSymbolsOfPropertyAccess(location, symbol, checker);
            return search;
        }

        // findallreferences.go:1991.
        internal Action<Node, EntryKind> referenceAdder(Symbol symbol)
        {
            if (!symbolToReferences.TryGetValue(symbol, out var entries))
            {
                entries = SymbolAndEntries.NewSymbolAndEntries(DefinitionKind.Symbol, null, symbol, default);
                symbolToReferences[symbol] = entries;
                result = result.Append(entries);
            }

            return (node, kind) => entries.references = entries.references.Append(newNodeEntryWithKind(node, kind));
        }

        // findallreferences.go:2003.
        internal void addReference(Node location, Symbol symbol, EntryKind kind)
        {
            if (options.use == ReferenceUse.Rename && location.Kind == Kind.DefaultKeyword)
                return;
            var add = referenceAdder(symbol);
            if (options.implementations)
                addImplementationReferences(location, node => add(node, kind));
            else
                add(location, kind);
        }

        // findallreferences.go:2203.
        internal bool markSearchedSymbols(SourceFile file, Slice<Symbol> symbols)
        {
            if (!sourceFileToSeenSymbols.TryGetValue(file, out var seen))
            {
                seen = new SeenSymbolSet();
                sourceFileToSeenSymbols[file] = seen;
            }

            bool anyNew = false;
            foreach (var symbol in symbols)
                if ((seen.values ??= new()).Add(symbol))
                    anyNew = true;
            return anyNew;
        }

        // findallreferences.go:2110.
        internal void addImplementationReferences(Node node, Action<Node> add)
        {
            if (Ast.IsDeclarationName(node) && isImplementation(node.Parent))
            {
                add(node);
                return;
            }

            if (node.Kind != Kind.Identifier)
                return;
            if (node.Parent.Kind == Kind.ShorthandPropertyAssignment)
                getReferenceEntriesForShorthandPropertyAssignment(node, checker, add);
            var containing = getContainingNodeIfInHeritageClause(node);
            if (containing != null)
            {
                add(containing);
                return;
            }

            var typeNode = Ast.FindAncestor(node, static ancestor => !Ast.IsQualifiedName(ancestor.Parent) && !Ast.IsTypeNode(ancestor.Parent) && !Ast.IsTypeElement(ancestor.Parent));
            if (typeNode == null || typeNode.Parent.Type() == null)
                return;
            var owner = typeNode.Parent;
            if (owner.Type() == typeNode && (seenContainingTypeReferences ??= new()).Add(owner))
            {
                void addIfImplementation(Node expression)
                {
                    if (isImplementationExpression(expression))
                        add(expression);
                }

                if (Ast.HasInitializer(owner))
                    addIfImplementation(owner.Initializer());
                else if (Ast.IsFunctionLike(owner) && owner.Body() != null)
                {
                    var body = owner.Body();
                    if (body.Kind == Kind.Block)
                        Ast.ForEachReturnStatement(body, statement =>
                        {
                            var expression = statement.Expression();
                            if (expression != null)
                                addIfImplementation(expression);
                            return false;
                        });
                    else
                        addIfImplementation(body);
                }
                else if (Ast.IsAssertionExpression(owner) || Ast.IsSatisfiesExpression(owner))
                    addIfImplementation(owner.Expression());
            }
        }

        // findallreferences.go:2170.
        internal void getReferencesInContainerOrFiles(Symbol symbol, RefSearch search)
        {
            var scope = getSymbolScope(symbol);
            if (scope != null)
            {
                bool addHere = scope.Kind != Kind.SourceFile;
                if (!addHere)
                    foreach (var file in sourceFiles)
                        if (file == scope.AsSourceFile())
                        {
                            addHere = true;
                            break;
                        }

                getReferencesInContainer(scope, Ast.GetSourceFileOfNode(scope), search, addHere);
            }
            else
                foreach (var file in sourceFiles)
                    searchForName(file, search);
        }

        // findallreferences.go:2185.
        internal void getReferencesInSourceFile(SourceFile file, RefSearch search, bool addHere) => getReferencesInContainer(file.AsNode(), file, search, addHere);
        // findallreferences.go:2190.
        internal void getReferencesInContainer(Node container, SourceFile file, RefSearch search, bool addHere)
        {
            if (!markSearchedSymbols(file, search.allSearchSymbols))
                return;
            foreach (var position in getPossibleSymbolReferencePositions(file, search.text, container))
                getReferencesAtLocation(file, position, search, addHere);
        }

        // findallreferences.go:2218. Original comment/string search TODO is retained;
        // an invalid token position does not produce an invented textual reference.
        internal void getReferencesAtLocation(SourceFile file, int position, RefSearch search, bool addHere)
        {
            var location = AstNav.GetTouchingPropertyName(file, position);
            if (!isValidReferencePosition(location, search.text))
                return;
            if ((getMeaningFromLocation(location) & searchMeaning) == 0)
                return;
            var referenceSymbol = checker.GetSymbolAtLocation(location);
            if (referenceSymbol == null)
                return;
            var parent = location.Parent;
            if (parent.Kind == Kind.ImportSpecifier && parent.PropertyName() == location)
                return;
            if (parent.Kind == Kind.ExportSpecifier)
            {
                getReferencesAtExportSpecifier(location, referenceSymbol, parent.AsExportSpecifier(), search, addHere, false);
                return;
            }

            var (related, kind) = getRelatedSymbol(search, referenceSymbol, location);
            if (related == null)
            {
                getReferenceForShorthandProperty(referenceSymbol, search);
                return;
            }

            switch (specialSearchKind)
            {
                case "none":
                    if (addHere)
                        addReference(location, related, kind);
                    break;
                case "constructor":
                    addConstructorReferences(location, related, search, addHere);
                    break;
                case "class":
                    addClassStaticThisReferences(location, related, search, addHere);
                    break;
            }

            if (Ast.IsInJSFile(location) && location.Parent.Kind == Kind.BindingElement && Ast.IsVariableDeclarationInitializedToBareOrAccessedRequire(location.Parent.Parent.Parent))
            {
                referenceSymbol = location.Parent.Symbol();
                if (referenceSymbol == null)
                    return;
            }

            getImportOrExportReferences(location, referenceSymbol, search);
        }

        // findallreferences.go:2289. Keep referenceAdder invocation deferred until
        // each actual constructor occurrence, including inherited constructor search.
        internal void addConstructorReferences(Node location, Symbol symbol, RefSearch search, bool addHere)
        {
            if (Ast.IsNewExpressionTarget(location, false, false) && addHere)
                addReference(location, symbol, EntryKind.Node);
            Action<Node, EntryKind> pusher() => referenceAdder(search.symbol);
            if (Ast.IsClassLike(location.Parent))
                findOwnConstructorReferences(search.symbol, Ast.GetSourceFileOfNode(location), node => pusher()(node, EntryKind.Node));
            else
            {
                var extending = tryGetClassByExtendingIdentifier(location);
                if (extending != null)
                {
                    findSuperConstructorAccesses(extending, node => pusher()(node, EntryKind.Node));
                    findInheritedConstructorReferences(extending);
                }
            }
        }

        // findallreferences.go:2315.
        internal void addClassStaticThisReferences(Node location, Symbol symbol, RefSearch search, bool addHere)
        {
            if (addHere)
                addReference(location, symbol, EntryKind.Node);
            var classLike = location.Parent;
            if (options.use == ReferenceUse.Rename || !Ast.IsClassLike(classLike))
                return;
            var add = referenceAdder(search.symbol);
            var members = classLike.Members();
            if (members.IsNil)
                return;
            foreach (var member in members)
            {
                if (!(isMethodOrAccessor(member) && Ast.HasStaticModifier(member)))
                    continue;
                var body = member.Body();
                if (body == null)
                    continue;
                new StaticThisVisitor(add).Visit(body);
            }
        }

        // findallreferences.go:2352.
        internal void findInheritedConstructorReferences(Node declaration)
        {
            if (hasOwnConstructor(declaration))
                return;
            var symbol = declaration.Symbol();
            getReferencesInContainerOrFiles(symbol, createSearch(null, symbol, ImpExpKind.Unknown, default, default));
        }

        // findallreferences.go:2361.
        internal void getImportOrExportReferences(Node location, Symbol symbol, RefSearch search)
        {
            var importOrExport = getImportOrExportSymbol(location, symbol, checker, search.comingFrom == ImpExpKind.Export);
            if (importOrExport == null)
                return;
            if (importOrExport.kind == ImpExpKind.Import)
            {
                if (!isForRenameWithPrefixAndSuffixText(options))
                    searchForImportedSymbol(importOrExport.symbol!);
            }
            else
                searchForImportsOfExport(location, importOrExport.symbol!, importOrExport.exportInfo!);
        }

        // findallreferences.go:2375.
        internal bool markSeenReExportRHS(Node node) => (seenReExportRHS ??= new()).Add(node);
        // findallreferences.go:2379.
        internal void getReferencesAtExportSpecifier(Node location, Symbol symbol, ExportSpecifier specifier, RefSearch search, bool addHere, bool alwaysGetReferences)
        {
            Debug.Assert(!alwaysGetReferences || options.useAliasesForRename, "If alwaysGetReferences is true, then prefix/suffix text must be enabled");
            var declaration = specifier.AsNode().Parent.Parent.AsExportDeclaration();
            var propertyName = specifier.PropertyName;
            var name = specifier.Name();
            var local = getLocalSymbolForExportSpecifier(location, symbol, specifier, checker);
            if (!alwaysGetReferences && !search.includes(local))
                return;
            void addRef()
            {
                if (addHere)
                    addReference(location, local, EntryKind.Node);
            }

            if (propertyName == null)
            {
                if (!(options.use == ReferenceUse.Rename && Ast.ModuleExportNameIsDefault(name)))
                    addRef();
            }
            else if (location == propertyName)
            {
                if (declaration.ModuleSpecifier == null)
                    addRef();
                if (addHere && options.use != ReferenceUse.Rename && markSeenReExportRHS(name))
                {
                    var export = specifier.AsNode().Symbol();
                    Debug.Assert(export != null, "exportSpecifier.Symbol() should not be nil");
                    addReference(name, export!, EntryKind.Node);
                }
            }
            else if (markSeenReExportRHS(location))
                addRef();
            if (!isForRenameWithPrefixAndSuffixText(options) || alwaysGetReferences)
            {
                bool isDefault = Ast.ModuleExportNameIsDefault(location) || Ast.ModuleExportNameIsDefault(specifier.Name());
                var export = specifier.AsNode().Symbol();
                Debug.Assert(export != null, "exportSpecifier.Symbol() should not be nil");
                var info = getExportInfo(export!, isDefault ? ExportKind.Default : ExportKind.Named, checker);
                if (info != null)
                    searchForImportsOfExport(location, export!, info);
            }

            if (search.comingFrom != ImpExpKind.Export && declaration.ModuleSpecifier != null && propertyName == null && !isForRenameWithPrefixAndSuffixText(options))
            {
                var imported = checker.GetExportSpecifierLocalTargetSymbol(specifier.AsNode());
                if (imported != null)
                    searchForImportedSymbol(imported);
            }
        }

        // findallreferences.go:2452.
        internal void searchForImportedSymbol(Symbol symbol)
        {
            foreach (var declaration in symbol.Declarations)
            {
                var file = Ast.GetSourceFileOfNode(declaration);
                getReferencesInSourceFile(file, createSearch(declaration, symbol, ImpExpKind.Import, default, default), includesSourceFile(file));
            }
        }

        // findallreferences.go:2461.
        internal void searchForImportsOfExport(Node exportLocation, Symbol exportSymbol, ExportInfo info)
        {
            var imports = getImportSearches(exportSymbol, info);
            if (imports.singleReferences.Count != 0)
            {
                var add = referenceAdder(exportSymbol);
                foreach (var reference in imports.singleReferences)
                    if (shouldAddSingleReference(reference))
                        add(reference, EntryKind.Node);
            }

            foreach (var import in imports.importSearches)
                getReferencesInSourceFile(Ast.GetSourceFileOfNode(import.importLocation), createSearch(import.importLocation, import.importSymbol!, ImpExpKind.Export, default, default), true);
            if (imports.indirectUsers.Count != 0)
            {
                RefSearch? search = null;
                switch (info.exportKind)
                {
                    case ExportKind.Named:
                        search = createSearch(exportLocation, exportSymbol, ImpExpKind.Export, default, default);
                        break;
                    case ExportKind.Default:
                        if (options.use != ReferenceUse.Rename)
                            search = createSearch(exportLocation, exportSymbol, ImpExpKind.Export, ByteStringValue.FromString("default"), default);
                        break;
                }

                if (search != null)
                    foreach (var file in imports.indirectUsers)
                        searchForName(file, search);
            }
        }

        // findallreferences.go:2498.
        internal bool shouldAddSingleReference(Node node)
        {
            if (!hasMatchingMeaning(node))
                return false;
            if (options.use != ReferenceUse.Rename)
                return true;
            if (!Ast.IsIdentifier(node) && !Ast.IsImportOrExportSpecifier(node.Parent))
                return false;
            return !(Ast.IsImportOrExportSpecifier(node.Parent) && Ast.ModuleExportNameIsDefault(node));
        }

        // findallreferences.go:2513.
        internal bool hasMatchingMeaning(Node node) => (getMeaningFromLocation(node) & searchMeaning) != 0;
        // findallreferences.go:2517.
        internal void getReferenceForShorthandProperty(Symbol symbol, RefSearch search)
        {
            if ((symbol.Flags & SymbolFlags.Transient) != 0 || symbol.ValueDeclaration == null)
                return;
            var shorthand = checker.GetShorthandAssignmentValueSymbol(symbol.ValueDeclaration);
            var name = Ast.GetNameOfDeclaration(symbol.ValueDeclaration);
            if (name != null && search.includes(shorthand!))
                addReference(name, shorthand!, EntryKind.Node);
        }

        // findallreferences.go:2535.
        internal Slice<Symbol> populateSearchSymbolSet(Symbol symbol, Node? location, bool isRename, bool providePrefixAndSuffix, bool implementations)
        {
            if (location == null)
                return Slice<Symbol>.FromArray([symbol]);
            var result = Slice<Symbol>.Make(0);
            forEachRelatedSymbol(symbol, location, isRename, !(isRename && providePrefixAndSuffix), (candidate, root, @base) =>
            {
                if (@base != null && isStaticSymbol(symbol) != isStaticSymbol(@base))
                    @base = null;
                result = result.Append(@base ?? root ?? candidate);
                return null;
            }, _ => !implementations);
            return result;
        }

        // findallreferences.go:2560.
        internal (Symbol? Symbol, EntryKind Kind) getRelatedSymbol(RefSearch search, Symbol symbol, Node location) => forEachRelatedSymbol(symbol, location, false, options.use != ReferenceUse.Rename || options.useAliasesForRename, (candidate, root, @base) =>
        {
            if (@base != null && isStaticSymbol(symbol) != isStaticSymbol(@base))
                @base = null;
            var searched = @base ?? root ?? candidate;
            if (searched != null && search.includes(searched))
                return root != null && (candidate.CheckFlags & CheckFlags.Synthetic) == 0 ? root : candidate;
            return null;
        }, root =>
        {
            if (search.parents.Count == 0)
                return true;
            foreach (var parent in search.parents)
                if (explicitlyInheritsFrom(root.Parent, parent))
                    return true;
            return false;
        });
        // findallreferences.go:2592. The root callback is query-scoped and captures
        // the current symbol parameter for each real base-type worker invocation.
        internal (Symbol? Symbol, EntryKind Kind) forEachRelatedSymbol(Symbol symbol, Node location, bool isRenamePopulation, bool onlyBindingAtLocation, Func<Symbol, Symbol?, Symbol?, Symbol?> callback, Func<Symbol, bool> allowBaseTypes)
        {
            Symbol? fromRoot(Symbol candidate)
            {
                foreach (var root in checker.GetRootSymbols(candidate))
                {
                    var found = callback(candidate, root, null);
                    if (found != null)
                        return found;
                    if (root.Parent != null && (root.Parent.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0 && allowBaseTypes(root))
                    {
                        found = getPropertySymbolsFromBaseTypes(root.Parent, root.NameValue, checker, @base => callback(candidate, root, @base));
                        if (found != null)
                            return found;
                    }
                }

                return null;
            }

            var objectElement = getContainingObjectLiteralElement(location);
            if (objectElement != null)
            {
                var shorthand = checker.GetShorthandAssignmentValueSymbol(location.Parent);
                if (shorthand != null && isRenamePopulation)
                    return (callback(shorthand, null, null), EntryKind.SearchedLocalFoundProperty);
                var contextual = checker.GetContextualType(objectElement.Parent, ContextFlags.None);
                if (contextual != null)
                    foreach (var candidate in checker.GetPropertySymbolsFromContextualType(objectElement, contextual, true))
                    {
                        var found = fromRoot(candidate);
                        if (found != null)
                            return (found, EntryKind.SearchedPropertyFoundLocal);
                    }

                var property = checker.GetPropertySymbolOfDestructuringAssignment(location);
                if (property != null)
                {
                    var found = callback(property, null, null);
                    if (found != null)
                        return (found, EntryKind.SearchedPropertyFoundLocal);
                }

                if (shorthand != null)
                {
                    var found = callback(shorthand, null, null);
                    if (found != null)
                        return (found, EntryKind.SearchedLocalFoundProperty);
                }
            }

            var aliased = getMergedAliasedSymbolOfNamespaceExportDeclaration(location, symbol, checker);
            if (aliased != null)
            {
                var found = callback(aliased, null, null);
                if (found != null)
                    return (found, EntryKind.Node);
            }

            var rootResult = fromRoot(symbol);
            if (rootResult != null)
                return (rootResult, EntryKind.Node);
            if (symbol.ValueDeclaration != null && Ast.IsParameterPropertyDeclaration(symbol.ValueDeclaration, symbol.ValueDeclaration.Parent))
            {
                var (parameter, member) = checker.GetSymbolsOfParameterPropertyDeclaration(symbol.ValueDeclaration, symbol.NameValue);
                Debug.Assert((parameter.Flags & SymbolFlags.FunctionScopedVariable) != 0 && (member.Flags & SymbolFlags.ClassMember) != 0, "GetSymbolsOfParameterPropertyDeclaration must return (parameter, member) pair");
                return (fromRoot((symbol.Flags & SymbolFlags.FunctionScopedVariable) != 0 ? member : parameter), EntryKind.Node);
            }

            var export = Ast.GetDeclarationOfKind(symbol, Kind.ExportSpecifier);
            if (export != null && (!isRenamePopulation || export.PropertyName() == null))
            {
                var local = checker.GetExportSpecifierLocalTargetSymbol(export);
                if (local != null)
                {
                    var found = callback(local, null, null);
                    if (found != null)
                        return (found, EntryKind.Node);
                }
            }

            if (!isRenamePopulation)
            {
                Symbol? property;
                if (onlyBindingAtLocation)
                {
                    if (!isObjectBindingElementWithoutPropertyName(location.Parent))
                        return (null, EntryKind.None);
                    property = getPropertySymbolFromBindingElement(checker, location.Parent);
                }
                else
                    property = getPropertySymbolOfObjectBindingPatternWithoutPropertyName(symbol, checker);
                return property == null ? (null, EntryKind.None) : (fromRoot(property), EntryKind.SearchedPropertyFoundLocal);
            }

            Debug.Assert(isRenamePopulation);
            if (onlyBindingAtLocation)
            {
                var property = getPropertySymbolOfObjectBindingPatternWithoutPropertyName(symbol, checker);
                if (property != null)
                    return (fromRoot(property), EntryKind.SearchedPropertyFoundLocal);
            }

            return (null, EntryKind.None);
        }

        // findallreferences.go:2729.
        internal void searchForName(SourceFile file, RefSearch search)
        {
            if (file.GetNameTable().ContainsKey(ByteStringKey.FromValue(search.escapedText)))
                getReferencesInSourceFile(file, search, true);
        }

        // findallreferences.go:2735.
        internal bool explicitlyInheritsFrom(Symbol symbol, Symbol parent)
        {
            if (symbol == parent)
                return true;
            var key = new InheritKey(symbol, parent);
            if (inheritsFromCache.TryGetValue(key, out var cached))
                return cached;
            inheritsFromCache[key] = false;
            if (symbol.Declarations.IsNil)
                return false;
            bool inherits = false;
            foreach (var declaration in symbol.Declarations)
            {
                foreach (var reference in getAllSuperTypeNodes(declaration))
                {
                    var type = checker.GetTypeAtLocation(reference);
                    if (type != null && type.Symbol() != null && explicitlyInheritsFrom(type.Symbol(), parent))
                    {
                        inherits = true;
                        break;
                    }
                }

                if (inherits)
                    break;
            }

            inheritsFromCache[key] = inherits;
            return inherits;
        }
    }

    // findallreferences.go:1926.
    private static RefState newState(ProtocolRequestContext context, TypeScript.Compiler.Program program, Slice<SourceFile> sourceFiles, HashSet<RootedFilePath> sourceFilesSet, Node? node, Checker checker, SemanticMeaning meaning, RefOptions options) => new()
    {
        sourceFiles = sourceFiles,
        sourceFilesSet = sourceFilesSet,
        specialSearchKind = getSpecialSearchKind(node),
        checker = checker,
        context = context,
        program = program,
        searchMeaning = meaning,
        options = options,
        inheritsFromCache = new(),
        symbolToReferences = new(),
        sourceFileToSeenSymbols = new()
    };
    // findallreferences.go:2017.
    private static void getReferenceEntriesForShorthandPropertyAssignment(Node node, Checker checker, Action<Node> add)
    {
        var symbol = checker.GetSymbolAtLocation(node);
        if (symbol == null || symbol.ValueDeclaration == null)
            return;
        var shorthand = checker.GetShorthandAssignmentValueSymbol(symbol.ValueDeclaration);
        if (shorthand != null && shorthand.Declarations.Count > 0)
            foreach (var declaration in shorthand.Declarations)
                if ((Ast.GetMeaningFromDeclaration(declaration) & SemanticMeaning.Value) != 0)
                    add(declaration);
    }

    // findallreferences.go:2032.
    private static bool isMethodOrAccessor(Node node) => node.Kind is Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor;
    // findallreferences.go:2036.
    private static Node? tryGetClassByExtendingIdentifier(Node node) => Ast.TryGetClassExtendingExpressionWithTypeArguments(Ast.ClimbPastPropertyAccess(node).Parent);
    // findallreferences.go:2040.
    private static Symbol? getClassConstructorSymbol(Symbol symbol) => symbol.Members?[Ast.InternalSymbolNameConstructor];
    // findallreferences.go:2047.
    private static bool hasOwnConstructor(Node declaration) => getClassConstructorSymbol(declaration.Symbol()) != null;
    // findallreferences.go:2051.
    private static void findOwnConstructorReferences(Symbol symbol, SourceFile file, Action<Node> add)
    {
        var constructor = getClassConstructorSymbol(symbol);
        if (constructor != null && constructor.Declarations.Count > 0)
            foreach (var declaration in constructor.Declarations)
                if (declaration.Kind == Kind.Constructor)
                {
                    var keyword = AstNav.FindChildOfKind(declaration, Kind.ConstructorKeyword, file);
                    if (keyword != null)
                        add(keyword);
                }

        if (symbol.Exports != null)
            foreach (var member in symbol.Exports.Values)
            {
                var declaration = member.ValueDeclaration;
                if (declaration != null && declaration.Kind == Kind.MethodDeclaration)
                {
                    var body = declaration.Body();
                    if (body != null)
                        forEachDescendantOfKind(body, Kind.ThisKeyword, node =>
                        {
                            if (Ast.IsNewExpressionTarget(node, false, false))
                                add(node);
                        });
                }
            }
    }

    // findallreferences.go:2080.
    private static void findSuperConstructorAccesses(Node declaration, Action<Node> add)
    {
        var constructor = getClassConstructorSymbol(declaration.Symbol());
        if (constructor == null || constructor.Declarations.Count == 0)
            return;
        foreach (var candidate in constructor.Declarations)
            if (candidate.Kind == Kind.Constructor)
            {
                var body = candidate.Body();
                if (body != null)
                    forEachDescendantOfKind(body, Kind.SuperKeyword, node =>
                    {
                        if (Ast.IsCallExpressionTarget(node, false, false))
                            add(node);
                    });
            }
    }

    // findallreferences.go:2100. Explicit query owner caches the child callback;
    // recursion retains preorder and the same immutable kind/action state.
    private static void forEachDescendantOfKind(Node node, Kind kind, Action<Node> action) => new DescendantKindVisitor(kind, action).Visit(node);
    private sealed class DescendantKindVisitor
    {
        private readonly Kind kind;
        private readonly Action<Node> action;
        private readonly Visitor child;
        internal DescendantKindVisitor(Kind kind, Action<Node> action)
        {
            this.kind = kind;
            this.action = action;
            child = VisitChild;
        }

        internal void Visit(Node node) => node.ForEachChild(child);
        private bool VisitChild(Node node)
        {
            if (node.Kind == kind)
                action(node);
            Visit(node);
            return false;
        }
    }

    // Callback owner for findallreferences.go:2315; it preserves the same
    // this/function/class stopping rules without per-node child delegate binding.
    private sealed class StaticThisVisitor
    {
        private readonly Action<Node, EntryKind> add;
        private readonly Visitor child;
        internal StaticThisVisitor(Action<Node, EntryKind> add)
        {
            this.add = add;
            child = VisitChild;
        }

        internal void Visit(Node node)
        {
            if (node.Kind == Kind.ThisKeyword)
                add(node, EntryKind.Node);
            else if (!Ast.IsFunctionLike(node) && !Ast.IsClassLike(node))
                node.ForEachChild(child);
        }

        private bool VisitChild(Node node)
        {
            Visit(node);
            return false;
        }
    }
}
