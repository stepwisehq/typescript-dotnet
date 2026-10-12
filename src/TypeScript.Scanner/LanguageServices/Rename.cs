// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/rename.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using TypeScript.LanguageServices.Utilities;
using AstNav = TypeScript.AstNavigation.AstNav;

using Kind = TypeScript.Scanner.Kind;
using Message = TypeScript.Scanner.Message;

namespace TypeScript.LanguageServices;
internal struct RenameInfo
{
    internal bool CanRename;
    internal ByteStringValue LocalizedErrorMessage, DisplayName;
    internal TypeScript.Lsp.Protocol.Range TriggerSpan;
    internal RootedPath FileToRename, NewFileName;
}

internal struct MappedRenameEdit
{
    internal DocumentUri uri;
    internal TextEdit edit;
}

internal readonly record struct RenameEditKey(DocumentUri Uri, TypeScript.Lsp.Protocol.Range Range);
internal sealed partial class LanguageService
{
    // rename.go:47. Conflicting edit text invalidates the whole edit result.
    private static (Dictionary<DocumentUri, Slice<TextEdit?>>? Changes, bool Ok) deduplicateRenameEdits(Slice<MappedRenameEdit> mappedEdits)
    {
        var editTexts = new Dictionary<RenameEditKey, ByteStringValue>();
        var uniqueEdits = Slice<MappedRenameEdit>.Make(0, mappedEdits.Length);
        foreach (var mappedEdit in mappedEdits)
        {
            var key = new RenameEditKey(mappedEdit.uri, mappedEdit.edit.Range);
            if (editTexts.TryGetValue(key, out var existingText))
            {
                if (!existingText.EqualsValue(mappedEdit.edit.NewText))
                    return (null, false);
                continue;
            }

            editTexts[key] = mappedEdit.edit.NewText;
            uniqueEdits = uniqueEdits.Append(mappedEdit);
        }

        var changes = new Dictionary<DocumentUri, Slice<TextEdit?>>();
        foreach (var mappedEdit in uniqueEdits)
            changes[mappedEdit.uri] = changes.GetValueOrDefault(mappedEdit.uri).Append(mappedEdit.edit);
        return (changes, true);
    }

    // rename.go:70. Cross-project orchestration is the shared genuine owner.
    internal (WorkspaceEditOrNull Result, Exception? Error) ProvideRename(ProtocolRequestContext context, RenameParams parameters, ICrossProjectOrchestrator? orchestrator) => handleCrossProject(context, parameters, orchestrator, static (service, requestContext, request, data, options) => service.symbolAndEntriesToRename(requestContext, request, data, options), combineRenameResponse, true, false, default, null);
    // rename.go:84. A projected rename position must have exact fidelity.
    internal RenameInfo GetRenameInfo(ProtocolRequestContext context, ByteStringValue newName, DocumentUri documentUri, TypeScript.Lsp.Protocol.Position position)
    {
        var (program, sourceFile) = getProgramAndFile(documentUri);
        var positions = converters.FromLSPPositionForSourceFile(sourceFile, position, Feature.Rename);
        foreach (var mapped in positions)
        {
            if (!mapped.Fidelity.IsExact())
                continue;
            var file = mapped.Script;
            var node = AstNav.GetTouchingPropertyName(file, mapped.Position);
            node = getAdjustedLocation(node, true, file);
            if (nodeIsEligibleForRename(node))
            {
                var (info, ok) = getRenameInfoForNode(context, newName, node, file, program);
                if (ok)
                    return info;
            }
        }

        return getRenameInfoError(context, Diagnostics.You_cannot_rename_this_element);
    }

    // rename.go:104. Eligibility is checked even when prepareRename was skipped;
    // edits keep source ordering and exact content-mapped write admission.
    private (WorkspaceEditOrNull Result, Exception? Error) symbolAndEntriesToRename(ProtocolRequestContext context, RenameParams parameters, SymbolAndEntriesData data, SymbolEntryTransformOptions options)
    {
        if (!nodeIsEligibleForRename(data.OriginalNode))
            return (new(), null);
        var program = GetProgram();
        var sourceFile = Ast.GetSourceFileOfNode(data.OriginalNode!);
        var (info, ok) = getRenameInfoForNode(context, parameters.NewName, data.OriginalNode!, sourceFile, program);
        if (!ok || !info.CanRename)
            return (new(), null);
        var entries = Core.FlatMap(data.SymbolsAndEntries, static symbolAndEntries => symbolAndEntries.references);
        Slice<MappedRenameEdit> mappedEdits = default;
        var (checker, done) = program.GetTypeChecker(context);
        try
        {
            var quotePreference = LsUtil.GetQuotePreference(sourceFile, UserPreferences());
            bool useAliasesForRename = (UserPreferences().ProvidePrefixAndSuffixTextForRename != Tristate.False);
            foreach (var entry in entries)
            {
                var uri = getFileNameOfEntry(entry);
                if (UserPreferences().AllowRenameOfImportPath != Tristate.True && entry.node != null && Ast.IsStringLiteralLike(entry.node) && Ast.TryGetImportFromModuleSpecifier(entry.node) != null)
                    continue;
                var (range, mapped) = renameEditRange(entry);
                if (!mapped)
                    continue;
                var textEdit = new TextEdit
                {
                    Range = range,
                    NewText = getTextForRename(data.OriginalNode!, entry, parameters.NewName, checker, quotePreference, useAliasesForRename)
                };
                mappedEdits = mappedEdits.Append(new MappedRenameEdit { uri = uri, edit = textEdit });
            }

            var (changes, distinct) = deduplicateRenameEdits(mappedEdits);
            if (!distinct)
                return (new(), null);
            return (new WorkspaceEditOrNull { WorkspaceEdit = new WorkspaceEdit { Changes = new ProtocolMap<DocumentUri, Slice<TextEdit?>>(changes) } }, null);
        }
        finally
        {
            done();
        }
    }

    // rename.go:152. Rename writes require exact, not merely single-segment,
    // content projection. Preserve non-mapped cached entry ranges unchanged.
    private (TypeScript.Lsp.Protocol.Range Range, bool Ok) renameEditRange(ReferenceEntry entry)
    {
        resolveEntry(entry);
        if (entry.node == null)
        {
            var (location, fidelity) = sourceFileRangeToLSPLocation(entry.sourceFile!, entry.textRange!.Value);
            return (location.Range, fidelity.IsExact());
        }

        var sourceFile = Ast.GetSourceFileOfNode(entry.node);
        if (sourceFile == null || sourceFile.SpanMap() == null)
            return (getRangeOfEntry(entry), true);
        var (range, mappedFidelity) = converters.ToLSPRange(sourceFile, entry.textRange!.Value);
        return (range, mappedFidelity.IsExact());
    }

    // rename.go:167. Actual checker query owners and default display flags survive
    // this raw-byte producer/consumer path.
    private (RenameInfo Info, bool Ok) getRenameInfoForNode(ProtocolRequestContext context, ByteStringValue newName, Node node, SourceFile sourceFile, Program program)
    {
        var (checker, done) = program.GetTypeChecker(context);
        try
        {
            var symbol = checker.GetSymbolAtLocation(node);
            if (symbol == null)
            {
                if (Ast.IsStringLiteralLike(node))
                {
                    var type = getContextualTypeFromParentOrAncestorTypeNode(node, checker);
                    if (type != null && (type.IsStringLiteral() || (type.IsUnion() && Core.Every(type.Types(), static member => member.IsStringLiteral()))))
                        return (getRenameInfoSuccess(node, sourceFile, node.TextValue(), converters), true);
                }
                else if (Ast.IsLabelName(node))
                    return (getRenameInfoSuccess(node, sourceFile, node.TextValue(), converters), true);
                return (default, false);
            }

            if (symbol.Declarations.Length == 0)
                return (default, false);
            var message = renameBlockedReason(sourceFile, node, symbol, checker, program);
            if (message != null)
                return (getRenameInfoError(context, message), true);
            if (Ast.IsStringLiteralLike(node) && Ast.TryGetImportFromModuleSpecifier(node) != null)
                return (UserPreferences().AllowRenameOfImportPath == Tristate.True) ? getRenameInfoForModule(context, newName, node, sourceFile, symbol) : (default, false);
            return (getRenameInfoSuccess(node, sourceFile, checker.SymbolToStringExValue(symbol, null, SymbolFlags.All, SymbolFormatFlags.AllowAnyNodeKind), converters), true);
        }
        finally
        {
            done();
        }
    }

    // rename.go:209. Null node is a genuine non-eligible query outcome.
    private static bool nodeIsEligibleForRename(Node? node)
    {
        if (node == null)
            return false;
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.ThisKeyword:
                return true;
            case Kind.NumericLiteral:
                return isLiteralNameOfPropertyDeclarationOrIndexAccess(node);
            default:
                return false;
        }
    }

    // rename.go:231. Library/default/package admission precedes edit production.
    private Message? renameBlockedReason(SourceFile sourceFile, Node node, Symbol symbol, Checker checker, Program program)
    {
        foreach (var declaration in symbol.Declarations)
            if (isDefinedInLibraryFile(program, declaration))
                return Diagnostics.You_cannot_rename_elements_that_are_defined_in_the_standard_TypeScript_library;
        if (Ast.IsIdentifier(node) && node.TextValue().EqualsValue(ByteStringValue.FromString("default")) && symbol.Parent != null && (symbol.Parent.Flags & SymbolFlags.Module) != 0)
            return Diagnostics.You_cannot_rename_this_element;
        var message = wouldRenameInOtherNodeModules(sourceFile, symbol, checker, UserPreferences());
        return message;
    }

    // rename.go:249.
    private static bool isDefinedInLibraryFile(Program program, Node declaration)
    {
        var file = Ast.GetSourceFileOfNode(declaration);
        return program.IsSourceFileDefaultLibrary(PathKey.FromValue(file.PathKeyValue())) && RootedFilePath.FromValue(file.FileNameGoValue()).IsDeclarationFile();
    }

    // rename.go:255. Package path owners compare their original raw content.
    private static Message? wouldRenameInOtherNodeModules(SourceFile originalFile, Symbol symbol, Checker checker, TypeScript.LanguageServices.Utilities.UserPreferences preferences)
    {
        var candidateSymbol = symbol;
        if ((preferences.ProvidePrefixAndSuffixTextForRename == Tristate.False) && (candidateSymbol.Flags & SymbolFlags.Alias) != 0)
        {
            var importSpecifier = Core.Find(candidateSymbol.Declarations, Ast.IsImportSpecifier);
            if (importSpecifier != null && importSpecifier.AsImportSpecifier().PropertyName == null)
                candidateSymbol = checker.GetAliasedSymbol(candidateSymbol);
        }

        var declarations = candidateSymbol.Declarations;
        if (declarations.Length == 0)
            return null;
        var originalPackage = TypeScript.Modules.ModuleUtilities.NodeModulePackageRootForFile(RootedFilePath.FromValue(originalFile.FileNameGoValue()));
        if (originalPackage.AsValue().Length == 0)
        {
            foreach (var declaration in declarations)
                if (isInsideNodeModules(RootedFilePath.FromValue(Ast.GetSourceFileOfNode(declaration).FileNameGoValue())))
                    return Diagnostics.You_cannot_rename_elements_that_are_defined_in_a_node_modules_folder;
            return null;
        }

        foreach (var declaration in declarations)
        {
            var declarationPackage = TypeScript.Modules.ModuleUtilities.NodeModulePackageRootForFile(RootedFilePath.FromValue(Ast.GetSourceFileOfNode(declaration).FileNameGoValue()));
            if (declarationPackage.AsValue().Length != 0 && declarationPackage != originalPackage)
                return Diagnostics.You_cannot_rename_elements_that_are_defined_in_another_node_modules_folder;
        }

        return null;
    }

    // rename.go:290/294/298. Unknown resource operation strings do not equal rename.
    internal static bool ClientSupportsWillRenameFiles(ProtocolRequestContext context) => context.ClientCapabilities.Workspace.FileOperations.WillRename;
    internal static bool ClientSupportsDocumentChanges(ProtocolRequestContext context) => context.ClientCapabilities.Workspace.WorkspaceEdit.DocumentChanges;
    internal static bool ClientSupportsRenameResourceOperations(ProtocolRequestContext context) => Core.Some(context.ClientCapabilities.Workspace.WorkspaceEdit.ResourceOperations, static operation => operation == ResourceOperationKind.Rename);
    // rename.go:304. Relative module/file validation keeps the actual editor
    // capability checks and exact content-mapped trigger range.
    private (RenameInfo Info, bool Ok) getRenameInfoForModule(ProtocolRequestContext context, ByteStringValue newName, Node specifier, SourceFile sourceFile, Symbol moduleSymbol)
    {
        var specifierText = specifier.TextValue();
        if (!TsPath.IsExternalModuleNameRelative(specifierText))
            return (getRenameInfoError(context, Diagnostics.You_cannot_rename_a_module_via_a_global_import), true);
        if (!ClientSupportsDocumentChanges(context) || !ClientSupportsRenameResourceOperations(context))
            return (getRenameInfoError(context, Diagnostics.File_rename_is_not_supported_by_the_editor), true);
        var moduleSourceFile = Core.Find(moduleSymbol.Declarations, Ast.IsSourceFile);
        if (moduleSourceFile == null)
            return (default, false);
        var fileName = RootedFilePath.FromValue(moduleSourceFile.AsSourceFile().FileNameGoValue());
        RootedPath withoutIndex = default;
        if (!specifierText.EndsWith(ByteStringValue.FromString("/index")) && !specifierText.EndsWith(ByteStringValue.FromString("/index.js")))
            withoutIndex = tryRemoveIndexFileName(fileName);
        var displayName = withoutIndex.AsValue().Length != 0 ? withoutIndex : fileName.AsPath();
        var newFileName = getNewFileNameForModuleRename(displayName, specifierText, newName);
        int indexAfterLastSlash = specifierText.LastIndexOf('/') + 1;
        int start = AstNav.GetStartOfNode(specifier, sourceFile, false) + 1 + indexAfterLastSlash;
        int length = specifierText.Length - indexAfterLastSlash;
        var (triggerSpan, fidelity) = converters.ToLSPRange(sourceFile, new TextRange(start, start + length));
        if (!fidelity.IsExact())
            return (default, false);
        return (new RenameInfo { CanRename = true, DisplayName = specifierText.Slice(indexAfterLastSlash), TriggerSpan = triggerSpan, FileToRename = displayName, NewFileName = newFileName }, true);
    }

    // rename.go:348. A root-level index does not name a removable directory.
    internal static RootedPath tryRemoveIndexFileName(RootedFilePath fileName)
    {
        if (TsPath.RemoveFileExtension(fileName.BaseNameValue()).EqualsValue(ByteStringValue.FromString("index")))
        {
            var (root, relative) = fileName.RootAndRelativePathValue();
            if ((root.AsValue().EqualsValue(ByteStringValue.FromString("/")) || root.AsPath().IsDynamic()) && TsPath.RemoveFileExtension(relative).EqualsValue(ByteStringValue.FromString("index")))
                return default;
            return fileName.Directory().AsPath();
        }

        return default;
    }

    // rename.go:361. Preserve the resolved file's extension when a specifier's
    // extension differs, and the source filesystem's actual case-sensitivity.
    private RootedPath getNewFileNameForModuleRename(RootedPath oldPath, ByteStringValue specifierText, ByteStringValue newName)
    {
        var oldFileName = TsPath.RootedFilePathFromPath(oldPath);
        var newPath = oldPath.Directory().ResolveFile(newName);
        var caseSensitivity = host.CaseSensitivity();
        var oldExtension = oldFileName.IsDeclarationFile() ? oldFileName.DeclarationFileExtensionValue() : oldFileName.AnyExtensionValue(null, caseSensitivity);
        if (!newPath.HasExtension())
            newPath = newPath.AppendSuffix(oldExtension);
        else if (newPath.AnyExtensionValue(null, caseSensitivity).EqualsValue(TsPath.GetAnyExtensionFromPath(specifierText, null, caseSensitivity)))
            newPath = newPath.ChangeAnyExtension(oldExtension, default(Slice<ByteStringValue>), caseSensitivity);
        return newPath.AsPath();
    }

    // rename.go:376. Fixed concatenation owns only the required final output;
    // unchanged rename text retains its caller's immutable header.
    private ByteStringValue getTextForRename(Node originalNode, ReferenceEntry entry, ByteStringValue newText, Checker checker, QuotePreference quotePreference, bool useAliasesForRename)
    {
        if (useAliasesForRename && entry.kind != EntryKind.Range && (Ast.IsIdentifier(originalNode) || Ast.IsStringLiteralLike(originalNode)))
        {
            var node = Ast.GetReparsedNodeForNode(entry.node!);
            var kind = entry.kind;
            var parent = node.Parent;
            var name = originalNode.TextValue();
            bool isShorthandAssignment = Ast.IsShorthandPropertyAssignment(parent);
            if (isShorthandAssignment || (isObjectBindingElementWithoutPropertyName(parent) && parent.Name() == node && parent.AsBindingElement().DotDotDotToken == null))
            {
                if (kind == EntryKind.SearchedLocalFoundProperty)
                    return ByteStringValue.Concat(name, ByteStringValue.FromString(": "), newText);
                if (kind == EntryKind.SearchedPropertyFoundLocal)
                    return ByteStringValue.Concat(newText, ByteStringValue.FromString(": "), name);
                if (isShorthandAssignment)
                {
                    var grandparent = parent.Parent;
                    if (Ast.IsObjectLiteralExpression(grandparent) && Ast.IsBinaryExpression(grandparent.Parent) && Ast.IsModuleExportsAccessExpression(grandparent.Parent.AsBinaryExpression().Left))
                        return ByteStringValue.Concat(name, ByteStringValue.FromString(": "), newText);
                    return ByteStringValue.Concat(newText, ByteStringValue.FromString(": "), name);
                }

                return ByteStringValue.Concat(name, ByteStringValue.FromString(": "), newText);
            }

            if (Ast.IsImportSpecifier(parent) && parent.PropertyName() == null)
            {
                var originalSymbol = Ast.IsExportSpecifier(originalNode.Parent) ? checker.GetExportSpecifierLocalTargetSymbol(originalNode.Parent) : checker.GetSymbolAtLocation(originalNode);
                if (originalSymbol != null && Core.Some(originalSymbol.Declarations, parent, static (declaration, owner) => declaration == owner))
                    return ByteStringValue.Concat(name, ByteStringValue.FromString(" as "), newText);
                return newText;
            }

            if (Ast.IsExportSpecifier(parent) && parent.PropertyName() == null)
                return originalNode == entry.node || checker.GetSymbolAtLocation(originalNode) == checker.GetSymbolAtLocation(entry.node!) ? ByteStringValue.Concat(name, ByteStringValue.FromString(" as "), newText) : ByteStringValue.Concat(newText, ByteStringValue.FromString(" as "), name);
        }

        if (entry.kind != EntryKind.Range && Ast.IsNumericLiteral(entry.node) && Ast.IsAccessExpression(entry.node!.Parent))
        {
            var quote = getQuoteFromPreference(quotePreference);
            return ByteStringValue.Concat(quote, newText, quote);
        }

        return newText;
    } // rename.go:436. Quote spelling stays borrowed through rename text production.

    private static ByteStringValue getQuoteFromPreference(QuotePreference preference) => ByteStringValue.FromString(preference == QuotePreference.Single ? "'" : "\"");
    // rename.go:439. Localization returns the real locale-aware raw text owner.
    private static RenameInfo getRenameInfoError(ProtocolRequestContext context, Message message) => new()
    {
        CanRename = false,
        LocalizedErrorMessage = ByteStringValue.FromString(message.Localize(context.Locale))
    };
    // rename.go:446.
    private static RenameInfo getRenameInfoSuccess(Node node, SourceFile sourceFile, ByteStringValue displayName, TypeScript.LanguageServices.Lsconv.Converters converters)
    {
        int start = AstNav.GetStartOfNode(node, sourceFile, false);
        int end = node.End();
        if (Ast.IsStringLiteralLike(node))
        {
            start++;
            end--;
        }

        var (triggerSpan, fidelity) = converters.ToLSPRange(sourceFile, new TextRange(start, end));
        return !fidelity.IsExact() ? new RenameInfo
        {
            CanRename = false
        }

        : new RenameInfo
        {
            CanRename = true,
            DisplayName = displayName,
            TriggerSpan = triggerSpan
        };
    }
}
