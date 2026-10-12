// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1841–1892. Ignored complete notification-owner draft.
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private Exception? HandleDidChangeWorkspaceConfiguration(ProtocolRequestContext context, DidChangeConfigurationParams parameters)
    {
        // Only the original map[string]any arm configures the retained Session.
        if (parameters.Settings is Dictionary<string, object> settings)
            session!.Configure(LsUtil.ParseUserPreferences(settings));
        return null;
    }

    private Exception? HandleDidOpen(ProtocolRequestContext context, DidOpenTextDocumentParams parameters)
    {
        var document = parameters.TextDocument!;
        session!.DidOpenFile(context, document.Uri, document.Version, document.Text, document.LanguageId);
        return null;
    }

    private Exception? HandleDidChange(ProtocolRequestContext context, DidChangeTextDocumentParams parameters)
    {
        session!.DidChangeFile(context, parameters.TextDocument.Uri, parameters.TextDocument.Version, parameters.ContentChanges);
        return null;
    }

    private Exception? HandleDidSave(ProtocolRequestContext context, DidSaveTextDocumentParams parameters)
    {
        session!.DidSaveFile(context, parameters.TextDocument.Uri);
        return null;
    }

    private Exception? HandleDidClose(ProtocolRequestContext context, DidCloseTextDocumentParams parameters)
    {
        session!.DidCloseFile(context, parameters.TextDocument.Uri);
        return null;
    }

    private Exception? HandleDidChangeWatchedFiles(ProtocolRequestContext context, DidChangeWatchedFilesParams parameters)
    {
        session!.DidChangeWatchedFiles(context, parameters.Changes!);
        return null;
    }

    private static Exception? HandleSetTrace(ProtocolRequestContext context, SetTraceParams parameters)
    {
        // Exact upstream intentional no-op: custom/setLogVerbosity owns server logging.
        return null;
    }
}
