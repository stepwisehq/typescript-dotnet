// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1591–1708; ignored complete advertised-capability construction.
// Required real LS trigger/legend APIs remain explicit until their complete owners arrive.
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    // Original package slice owner; each response retains the same header.
    private static readonly Slice<FileOperationFilter?> FileRenameFilters = Slice<FileOperationFilter?>.FromArray([new FileOperationFilter { Scheme = ByteStringValue.FromString("file"), Pattern = new FileOperationPattern { Glob = ByteStringValue.FromString("**/*.{ts,tsx,js,jsx,cts,cjs,mts,mjs,json}") } }]);
    private ServerCapabilities CreateServerCapabilities() => new ServerCapabilities
    {
        PositionEncoding = positionEncoding,
        TextDocumentSync = new TextDocumentSyncOptionsOrKind
        {
            Options = new TextDocumentSyncOptions
            {
                OpenClose = true,
                Change = TextDocumentSyncKind.Incremental,
                Save = new BooleanOrSaveOptions
                {
                    Boolean = true,
                },
            },
        },
        HoverProvider = new BooleanOrHoverOptions
        {
            Boolean = true,
        },
        DefinitionProvider = new BooleanOrDefinitionOptions
        {
            Boolean = true,
        },
        TypeDefinitionProvider = new BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions
        {
            Boolean = true,
        },
        ReferencesProvider = new BooleanOrReferenceOptions
        {
            Boolean = true,
        },
        ImplementationProvider = new BooleanOrImplementationOptionsOrImplementationRegistrationOptions
        {
            Boolean = true,
        },
        DiagnosticProvider = new DiagnosticOptionsOrRegistrationOptions
        {
            Options = new DiagnosticOptions
            {
                Identifier = ByteStringValue.FromString("typescript"),
                InterFileDependencies = true,
            },
        },
        CompletionProvider = new CompletionOptions
        {
            TriggerCharacters = LanguageService.CompletionTriggerCharacters,
            ResolveProvider = true,
            CompletionItem = new ServerCompletionItemOptions
            {
                LabelDetailsSupport = true,
            },
        },
        SignatureHelpProvider = new SignatureHelpOptions
        {
            TriggerCharacters = LanguageService.SignatureHelpTriggerCharacters,
            RetriggerCharacters = LanguageService.SignatureHelpRetriggerCharacters,
        },
        DocumentFormattingProvider = new BooleanOrDocumentFormattingOptions
        {
            Boolean = true,
        },
        DocumentRangeFormattingProvider = new BooleanOrDocumentRangeFormattingOptions
        {
            Boolean = true,
        },
        DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions
        {
            FirstTriggerCharacter = ByteStringValue.FromString("{"),
            MoreTriggerCharacter = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("}"), ByteStringValue.FromString(";"), ByteStringValue.FromString("\n")]),
        },
        WorkspaceSymbolProvider = new BooleanOrWorkspaceSymbolOptions
        {
            Boolean = true,
        },
        DocumentSymbolProvider = new BooleanOrDocumentSymbolOptions
        {
            Boolean = true,
        },
        FoldingRangeProvider = new BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions
        {
            Boolean = true,
        },
        RenameProvider = new BooleanOrRenameOptions
        {
            RenameOptions = new RenameOptions
            {
                PrepareProvider = true,
            },
        },
        DocumentHighlightProvider = new BooleanOrDocumentHighlightOptions
        {
            Boolean = true,
        },
        SelectionRangeProvider = new BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions
        {
            Boolean = true,
        },
        LinkedEditingRangeProvider = new BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions
        {
            Boolean = true,
        },
        InlayHintProvider = new BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions
        {
            Boolean = true,
        },
        CodeLensProvider = new CodeLensOptions
        {
            ResolveProvider = true,
        },
        CodeActionProvider = new BooleanOrCodeActionOptions
        {
            CodeActionOptions = new CodeActionOptions
            {
                CodeActionKinds = SupportedCodeActionKinds(),
            },
        },
        CallHierarchyProvider = new BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions
        {
            Boolean = true,
        },
        Experimental = new ExperimentalServerCapabilities
        {
            CustomSourceDefinitionProvider = true,
            CustomMultiDocumentHighlightProvider = true,
        },
        VSReferencesProvider = true,
        VSOnAutoInsertProvider = new VSOnAutoInsertOptions
        {
            VSTriggerCharacters = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(">")]),
        },
        Workspace = new WorkspaceOptions
        {
            FileOperations = new FileOperationOptions
            {
                WillRename = new FileOperationRegistrationOptions
                {
                    Filters = FileRenameFilters,
                },
            },
        },
        SemanticTokensProvider = new SemanticTokensOptionsOrRegistrationOptions
        {
            Options = new SemanticTokensOptions
            {
                Legend = LanguageService.SemanticTokensLegend(clientCapabilities.TextDocument.SemanticTokens),
                Full = new BooleanOrSemanticTokensFullDelta
                {
                    Boolean = true,
                },
                Range = new BooleanOrEmptyObject
                {
                    Boolean = true,
                },
            },
        },
    };
}
