// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:359–453. Ignored direct capability admission owner.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private const string ContentMapperDidOpenRegistrationID = "content-mapper-did-open";
    private const string ContentMapperDidChangeRegistrationID = "content-mapper-did-change";
    private const string ContentMapperDidCloseRegistrationID = "content-mapper-did-close";
    private const string ContentMapperDiagnosticRegistrationID = "content-mapper-diagnostic";
    private const string ContentMapperHoverRegistrationID = "content-mapper-hover";
    private const string ContentMapperSignatureHelpRegistrationID = "content-mapper-signature-help";
    private const string ContentMapperDefinitionRegistrationID = "content-mapper-definition";
    private const string ContentMapperTypeDefinitionRegistrationID = "content-mapper-type-definition";
    private const string ContentMapperImplementationRegistrationID = "content-mapper-implementation";
    private const string ContentMapperReferencesRegistrationID = "content-mapper-references";
    private const string ContentMapperDocumentHighlightRegistrationID = "content-mapper-document-highlight";
    private const string ContentMapperCompletionRegistrationID = "content-mapper-completion";
    private const string ContentMapperRenameRegistrationID = "content-mapper-rename";
    private const string ContentMapperSemanticTokensRegistrationID = "content-mapper-semantic-tokens";
    private const string ContentMapperDocumentSymbolRegistrationID = "content-mapper-document-symbol";
    private const string ContentMapperFoldingRangeRegistrationID = "content-mapper-folding-range";
    private const string ContentMapperSelectionRangeRegistrationID = "content-mapper-selection-range";
    private const string ContentMapperInlayHintRegistrationID = "content-mapper-inlay-hint";
    private const string ContentMapperCodeLensRegistrationID = "content-mapper-code-lens";
    private const string ContentMapperCodeActionRegistrationID = "content-mapper-code-action";
    private const string ContentMapperFormattingRegistrationID = "content-mapper-formatting";
    private const string ContentMapperRangeFormattingRegistrationID = "content-mapper-range-formatting";
    private const string ContentMapperOnTypeFormattingRegistrationID = "content-mapper-on-type-formatting";
    private const string ContentMapperLinkedEditingRegistrationID = "content-mapper-linked-editing";
    private const string ContentMapperCallHierarchyRegistrationID = "content-mapper-call-hierarchy";
    private const string ContentMapperWillRenameFilesRegistrationID = "content-mapper-will-rename-files";
    private bool SupportsContentMapperRegistration(ByteStringValue id) => id switch
    {
        _ when id.EqualsString(ContentMapperDidOpenRegistrationID) || id.EqualsString(ContentMapperDidChangeRegistrationID) || id.EqualsString(ContentMapperDidCloseRegistrationID) => clientCapabilities.TextDocument.Synchronization.DynamicRegistration,
        _ when id.EqualsString(ContentMapperDiagnosticRegistrationID) => clientCapabilities.TextDocument.Diagnostic.DynamicRegistration,
        _ when id.EqualsString(ContentMapperHoverRegistrationID) => clientCapabilities.TextDocument.Hover.DynamicRegistration,
        _ when id.EqualsString(ContentMapperSignatureHelpRegistrationID) => clientCapabilities.TextDocument.SignatureHelp.DynamicRegistration,
        _ when id.EqualsString(ContentMapperDefinitionRegistrationID) => clientCapabilities.TextDocument.Definition.DynamicRegistration,
        _ when id.EqualsString(ContentMapperTypeDefinitionRegistrationID) => clientCapabilities.TextDocument.TypeDefinition.DynamicRegistration,
        _ when id.EqualsString(ContentMapperImplementationRegistrationID) => clientCapabilities.TextDocument.Implementation.DynamicRegistration,
        _ when id.EqualsString(ContentMapperReferencesRegistrationID) => clientCapabilities.TextDocument.References.DynamicRegistration,
        _ when id.EqualsString(ContentMapperDocumentHighlightRegistrationID) => clientCapabilities.TextDocument.DocumentHighlight.DynamicRegistration,
        _ when id.EqualsString(ContentMapperCompletionRegistrationID) => clientCapabilities.TextDocument.Completion.DynamicRegistration,
        _ when id.EqualsString(ContentMapperRenameRegistrationID) => clientCapabilities.TextDocument.Rename.DynamicRegistration,
        _ when id.EqualsString(ContentMapperSemanticTokensRegistrationID) => clientCapabilities.TextDocument.SemanticTokens.DynamicRegistration,
        _ when id.EqualsString(ContentMapperDocumentSymbolRegistrationID) => clientCapabilities.TextDocument.DocumentSymbol.DynamicRegistration,
        _ when id.EqualsString(ContentMapperFoldingRangeRegistrationID) => clientCapabilities.TextDocument.FoldingRange.DynamicRegistration,
        _ when id.EqualsString(ContentMapperSelectionRangeRegistrationID) => clientCapabilities.TextDocument.SelectionRange.DynamicRegistration,
        _ when id.EqualsString(ContentMapperInlayHintRegistrationID) => clientCapabilities.TextDocument.InlayHint.DynamicRegistration,
        _ when id.EqualsString(ContentMapperCodeLensRegistrationID) => clientCapabilities.TextDocument.CodeLens.DynamicRegistration,
        _ when id.EqualsString(ContentMapperCodeActionRegistrationID) => clientCapabilities.TextDocument.CodeAction.DynamicRegistration,
        _ when id.EqualsString(ContentMapperFormattingRegistrationID) => clientCapabilities.TextDocument.Formatting.DynamicRegistration,
        _ when id.EqualsString(ContentMapperRangeFormattingRegistrationID) => clientCapabilities.TextDocument.RangeFormatting.DynamicRegistration,
        _ when id.EqualsString(ContentMapperOnTypeFormattingRegistrationID) => clientCapabilities.TextDocument.OnTypeFormatting.DynamicRegistration,
        _ when id.EqualsString(ContentMapperLinkedEditingRegistrationID) => clientCapabilities.TextDocument.LinkedEditingRange.DynamicRegistration,
        _ when id.EqualsString(ContentMapperCallHierarchyRegistrationID) => clientCapabilities.TextDocument.CallHierarchy.DynamicRegistration,
        _ when id.EqualsString(ContentMapperWillRenameFilesRegistrationID) => clientCapabilities.Workspace.FileOperations.DynamicRegistration && clientCapabilities.Workspace.FileOperations.WillRename,
        _ => false
    };
    private static Slice<CodeActionKind> SupportedCodeActionKinds() => Slice<CodeActionKind>.FromArray([CodeActionKind.QuickFix, CodeActionKind.SourceOrganizeImportsTs, CodeActionKind.SourceRemoveUnusedImportsTs, CodeActionKind.SourceSortImportsTs, CodeActionKind.SourceFixAllTs]);
}
