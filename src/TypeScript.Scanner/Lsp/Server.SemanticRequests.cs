// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1947,2110–2117,2131–2146,2182–2207.
// Real promoted service owners; no manufactured empty responses.
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (HoverOrNull Value, Exception? Error) HandleHover(ProtocolRequestContext context, LanguageService service, HoverParams? parameters) => service.ProvideHover(context, parameters!);
    private (SignatureHelpOrNull Value, Exception? Error) HandleSignatureHelp(ProtocolRequestContext context, LanguageService service, SignatureHelpParams? parameters) => service.ProvideSignatureHelp(context, parameters!.TextDocument.Uri, parameters.Position, parameters.Context);
    private (LocationOrLocationsOrDefinitionLinksOrNull Value, Exception? Error) HandleDefinition(ProtocolRequestContext context, LanguageService service, DefinitionParams? parameters) => service.ProvideDefinition(context, parameters!.TextDocument.Uri, parameters.Position);
    private (LocationOrLocationsOrDefinitionLinksOrNull? Value, Exception? Error) HandleSourceDefinition(ProtocolRequestContext context, LanguageService service, TextDocumentPositionParams? parameters)
    {
        var(response, error) = service.ProvideSourceDefinition(context, parameters!.TextDocument.Uri, parameters.Position);
        return error is not null ? (null, error) : (response, null);
    }

    private (LocationOrLocationsOrDefinitionLinksOrNull Value, Exception? Error) HandleTypeDefinition(ProtocolRequestContext context, LanguageService service, TypeDefinitionParams? parameters) => service.ProvideTypeDefinition(context, parameters!.TextDocument.Uri, parameters.Position);
    private (TextEditsOrNull Value, Exception? Error) HandleDocumentFormat(ProtocolRequestContext context, LanguageService service, DocumentFormattingParams? parameters) => service.ProvideFormatDocument(context, parameters!.TextDocument.Uri, parameters.Options!);
    private (TextEditsOrNull Value, Exception? Error) HandleDocumentRangeFormat(ProtocolRequestContext context, LanguageService service, DocumentRangeFormattingParams? parameters) => service.ProvideFormatDocumentRange(context, parameters!.TextDocument.Uri, parameters.Options!, parameters.Range);
    private (TextEditsOrNull Value, Exception? Error) HandleDocumentOnTypeFormat(ProtocolRequestContext context, LanguageService service, DocumentOnTypeFormattingParams? parameters) => service.ProvideFormatDocumentOnType(context, parameters!.TextDocument.Uri, parameters.Options!, parameters.Position, parameters.Ch);
    // Pinned server.go:2119–2146,2237–2265,2293–2300,2325–2332.
    private (FoldingRangesOrNull Value, Exception? Error) HandleFoldingRange(ProtocolRequestContext context, LanguageService service, FoldingRangeParams? parameters) => service.ProvideFoldingRange(context, parameters!.TextDocument.Uri);
    private (VSOnAutoInsertResponseItemOrNull Value, Exception? Error) HandleVSOnAutoInsert(ProtocolRequestContext context, LanguageService service, VSOnAutoInsertParams? parameters) => service.ProvideOnAutoInsert(context, parameters!);
    private (LinkedEditingRangesOrNull Value, Exception? Error) HandleLinkedEditingRange(ProtocolRequestContext context, LanguageService service, LinkedEditingRangeParams? parameters) => service.ProvideLinkedEditingRange(context, parameters!);
    private (CompletionItemsOrListOrNull Value, Exception? Error) HandleCompletion(ProtocolRequestContext context, LanguageService service, CompletionParams? parameters) => service.ProvideCompletion(context, parameters!.TextDocument.Uri, parameters.Position, parameters.Context);
    private (SymbolInformationsOrDocumentSymbolsOrNull Value, Exception? Error) HandleDocumentSymbol(ProtocolRequestContext context, LanguageService service, DocumentSymbolParams? parameters) => service.ProvideDocumentSymbols(context, parameters!.TextDocument.Uri);
    private (SelectionRangesOrNull Value, Exception? Error) HandleSelectionRange(ProtocolRequestContext context, LanguageService service, SelectionRangeParams? parameters) => service.ProvideSelectionRanges(context, parameters!);
    private (CommandOrCodeActionArrayOrNull Value, Exception? Error) HandleCodeAction(ProtocolRequestContext context, LanguageService service, CodeActionParams? parameters) => service.ProvideCodeActions(context, parameters!);
    private (InlayHintsOrNull Value, Exception? Error) HandleInlayHint(ProtocolRequestContext context, LanguageService service, InlayHintParams? parameters) => service.ProvideInlayHint(context, parameters!);
    private (CodeLensesOrNull Value, Exception? Error) HandleCodeLens(ProtocolRequestContext context, LanguageService service, CodeLensParams? parameters) => service.ProvideCodeLenses(context, parameters!.TextDocument.Uri);
    private (CallHierarchyItemsOrNull Value, Exception? Error) HandlePrepareCallHierarchy(ProtocolRequestContext context, LanguageService service, CallHierarchyPrepareParams? parameters) => service.ProvidePrepareCallHierarchy(context, parameters!.TextDocument.Uri, parameters.Position);
    private (SemanticTokensOrNull Value, Exception? Error) HandleSemanticTokensFull(ProtocolRequestContext context, LanguageService service, SemanticTokensParams? parameters) => service.ProvideSemanticTokens(context, parameters!.TextDocument.Uri);
    private (SemanticTokensOrNull Value, Exception? Error) HandleSemanticTokensRange(ProtocolRequestContext context, LanguageService service, SemanticTokensRangeParams? parameters) => service.ProvideSemanticTokensRange(context, parameters!.TextDocument.Uri, parameters.Range);
}
