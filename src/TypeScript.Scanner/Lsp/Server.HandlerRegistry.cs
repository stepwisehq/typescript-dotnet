// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1265–1333. Full stdio registry draft; genuine feature owners pending.
// The five explicit external embedding/Go-profile APIs are unregistered and unadvertised.
using TypeScript.Lsp.Protocol;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private static class HandlerRegistry
    {
        // Explicit static constructor keeps registry construction on the server path only.
        static HandlerRegistry()
        {
        }

        internal static readonly HandlerMap Value = CreateHandlers();
    }

    private static HandlerMap CreateHandlers()
    {
        var handlers = new HandlerMap();
        handlers.RegisterRequestHandler(Method.InitializeInfo, static (server, context, parameters, request) => server.HandleInitialize(context, parameters!, request));
        handlers.RegisterNotificationHandler(Method.InitializedInfo, static (server, context, parameters) => server.HandleInitialized(context, parameters));
        handlers.RegisterRequestHandler(Method.ShutdownInfo, static (server, context, parameters, request) => server.HandleShutdown(context, parameters!, request));
        handlers.RegisterNotificationHandler(Method.ExitInfo, static (server, context, parameters) => HandleExit(context, parameters!));
        handlers.RegisterNotificationHandler(Method.WorkspaceDidChangeConfigurationInfo, static (server, context, parameters) => server.HandleDidChangeWorkspaceConfiguration(context, parameters!));
        handlers.RegisterNotificationHandler(Method.TextDocumentDidOpenInfo, static (server, context, parameters) => server.HandleDidOpen(context, parameters!));
        handlers.RegisterNotificationHandler(Method.TextDocumentDidChangeInfo, static (server, context, parameters) => server.HandleDidChange(context, parameters!));
        handlers.RegisterNotificationHandler(Method.TextDocumentDidSaveInfo, static (server, context, parameters) => server.HandleDidSave(context, parameters!));
        handlers.RegisterNotificationHandler(Method.TextDocumentDidCloseInfo, static (server, context, parameters) => server.HandleDidClose(context, parameters!));
        handlers.RegisterNotificationHandler(Method.WorkspaceDidChangeWatchedFilesInfo, static (server, context, parameters) => server.HandleDidChangeWatchedFiles(context, parameters!));
        handlers.RegisterNotificationHandler(Method.SetTraceInfo, static (server, context, parameters) => HandleSetTrace(context, parameters!));
        handlers.RegisterNotificationHandler(Method.CustomSetLogVerbosityInfo, static (server, context, parameters) => server.HandleSetLogVerbosity(context, parameters!));
        handlers.RegisterRequestHandler(Method.WorkspaceWillRenameFilesInfo, static (server, context, parameters, request) => server.HandleWillRenameFiles(context, parameters!, request));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentDiagnosticInfo, static (server, context, service, parameters) => server.HandleDocumentDiagnostic(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentHoverInfo, static (server, context, service, parameters) => server.HandleHover(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentDefinitionInfo, static (server, context, service, parameters) => server.HandleDefinition(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.CustomTextDocumentSourceDefinitionInfo, static (server, context, service, parameters) => server.HandleSourceDefinition(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentTypeDefinitionInfo, static (server, context, service, parameters) => server.HandleTypeDefinition(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentSignatureHelpInfo, static (server, context, service, parameters) => server.HandleSignatureHelp(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentFormattingInfo, static (server, context, service, parameters) => server.HandleDocumentFormat(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentRangeFormattingInfo, static (server, context, service, parameters) => server.HandleDocumentRangeFormat(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentOnTypeFormattingInfo, static (server, context, service, parameters) => server.HandleDocumentOnTypeFormat(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentDocumentSymbolInfo, static (server, context, service, parameters) => server.HandleDocumentSymbol(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentDocumentHighlightInfo, static (server, context, service, parameters) => server.HandleDocumentHighlight(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.CustomTextDocumentMultiDocumentHighlightInfo, static (server, context, service, parameters) => server.HandleMultiDocumentHighlight(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentSelectionRangeInfo, static (server, context, service, parameters) => server.HandleSelectionRange(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentInlayHintInfo, static (server, context, service, parameters) => server.HandleInlayHint(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentCodeLensInfo, static (server, context, service, parameters) => server.HandleCodeLens(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentCodeActionInfo, static (server, context, service, parameters) => server.HandleCodeAction(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentPrepareCallHierarchyInfo, static (server, context, service, parameters) => server.HandlePrepareCallHierarchy(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentFoldingRangeInfo, static (server, context, service, parameters) => server.HandleFoldingRange(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentPrepareRenameInfo, static (server, context, service, parameters) => server.HandlePrepareRename(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentLinkedEditingRangeInfo, static (server, context, service, parameters) => server.HandleLinkedEditingRange(context, service, parameters!));
        handlers.RegisterLanguageServiceWithAutoImportsRequestHandler(Method.TextDocumentCompletionInfo, static (server, context, service, parameters) => server.HandleCompletion(context, service, parameters!));
        handlers.RegisterLanguageServiceWithAutoImportsRequestHandler(Method.TextDocumentCodeActionInfo, static (server, context, service, parameters) => server.HandleCodeAction(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentVSOnAutoInsertInfo, static (server, context, service, parameters) => server.HandleVSOnAutoInsert(context, service, parameters!));
        handlers.RegisterMultiProjectReferenceRequestHandler(Method.TextDocumentReferencesInfo, static (service, context, parameters, orchestrator) => service.ProvideReferences(context, parameters!, orchestrator));
        handlers.RegisterMultiProjectReferenceRequestHandler(Method.TextDocumentVSReferencesInfo, static (service, context, parameters, orchestrator) => service.ProvideVSReferences(context, parameters!, orchestrator));
        handlers.RegisterRequestHandler(Method.TextDocumentRenameInfo, static (server, context, parameters, request) => server.HandleRename(context, parameters!, request));
        handlers.RegisterMultiProjectReferenceRequestHandler(Method.TextDocumentImplementationInfo, static (service, context, parameters, orchestrator) => service.ProvideImplementations(context, parameters!, orchestrator));
        handlers.RegisterRequestHandler(Method.CallHierarchyIncomingCallsInfo, static (server, context, parameters, request) =>
        {
            var(response, error) = server.HandleCallHierarchyIncomingCalls(context, parameters!, request);
            // The wire descriptor owns a value-root union; null remains its null arm.
            return (response!, error);
        });
        handlers.RegisterRequestHandler(Method.CallHierarchyOutgoingCallsInfo, static (server, context, parameters, request) =>
        {
            var(response, error) = server.HandleCallHierarchyOutgoingCalls(context, parameters!, request);
            // The wire descriptor owns a value-root union; null remains its null arm.
            return (response!, error);
        });
        handlers.RegisterRequestHandler(Method.WorkspaceSymbolInfo, static (server, context, parameters, request) =>
        {
            var(response, error) = server.HandleWorkspaceSymbol(context, parameters!, request);
            // The wire descriptor owns a value-root union; null remains its null arm.
            return (response!, error);
        });
        handlers.RegisterRequestHandler(Method.CompletionItemResolveInfo, static (server, context, parameters, request) => server.HandleCompletionItemResolve(context, parameters!, request));
        handlers.RegisterRequestHandler(Method.CodeLensResolveInfo, static (server, context, parameters, request) => server.HandleCodeLensResolve(context, parameters!, request));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentSemanticTokensFullInfo, static (server, context, service, parameters) => server.HandleSemanticTokensFull(context, service, parameters!));
        handlers.RegisterLanguageServiceDocumentRequestHandler(Method.TextDocumentSemanticTokensRangeInfo, static (server, context, service, parameters) => server.HandleSemanticTokensRange(context, service, parameters!));
        handlers.RegisterRequestHandler(Method.CustomRunGCInfo, static (server, context, parameters, request) => server.HandleRunGC(context, parameters!, request));
        handlers.RegisterRequestHandler(Method.CustomProjectInfoInfo, static (server, context, parameters, request) => server.HandleProjectInfo(context, parameters!, request));
        handlers.RegisterRequestHandler(Method.CustomSetContentMapperContributionsInfo, static (server, context, parameters, request) => server.HandleSetContentMapperContributions(context, parameters!, request));
        return handlers;
    }
}
