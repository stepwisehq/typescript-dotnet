// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:2148–2180,2209–2235,2269–2291,2301–2323.
using TypeScript.LanguageServices;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using CompilerProgram = TypeScript.Compiler.Program;
using CompletionItem = TypeScript.Lsp.Protocol.CompletionItem;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal (CompletionItem? Value, Exception? Error) HandleCompletionItemResolve(ProtocolRequestContext context, CompletionItem parameters, RequestMessage request)
    {
        var data = parameters.Data;
        if (data is null)
            return (null, new Exception("completion item data is nil"));
        var(fileName, absolute) = TsPath.TryRootedFilePathFromAbsolute(data.FileName);
        if (!absolute)
            return (null, new Exception("completion item data fileName must be absolute"));
        DocumentUri uri;
        if (fileName.IsDynamic())
        {
            var(dynamicUri, valid) = ProtocolUris.TryDynamicFileNameToDocumentUri(fileName.AsPath());
            if (!valid)
                return (null, new Exception("completion item data fileName must be a valid dynamic path"));
            uri = dynamicUri;
        }
        else
            uri = Lsconv.FileNameToDocumentURI(fileName);
        var(service, error) = session!.GetLanguageService(context, uri);
        if (error is not null)
            return (null, error);
        try
        {
            return service!.ResolveCompletionItem(context, parameters, data, fileName);
        }
        catch (Exception panic)
        {
            Recover(request, panic);
            return (null, null);
        }
    }

    private (SymbolInformationsOrWorkspaceSymbolsOrNull? Value, Exception? Error) HandleWorkspaceSymbol(ProtocolRequestContext context, WorkspaceSymbolParams parameters, RequestMessage request)
    {
        SymbolInformationsOrWorkspaceSymbolsOrNull? response = default;
        Exception? error = null;
        void ProvideSymbols(Snapshot snapshot, Slice<CompilerProgram> programs)
        {
            try
            {
                (response, error) = LanguageService.ProvideWorkspaceSymbols(context, programs, snapshot.Converters(), snapshot.UserPreferences(), parameters.Query);
            }
            catch (Exception panic)
            {
                Recover(request, panic);
            }
        }

        if (parameters.TextDocument is not null && session!.Config().WorkspaceSymbolsScope == WorkspaceSymbolsScope.CurrentProject)
        {
            var uri = parameters.TextDocument.Uri;
            session.WithSnapshotForDocument(context, uri, snapshot => ProvideSymbols(snapshot, Core.MapSlice(snapshot.GetLanguageServiceProjectsContainingFile(uri), static project => project.GetProgram()!)));
        }
        else
            session!.WithSnapshotLoadingProjectTree(context, null, snapshot => ProvideSymbols(snapshot, Core.MapSlice(snapshot.ProjectCollection.LanguageServiceProjects(), static project => project.GetProgram()!)));
        return (response, error);
    }

    private (CodeLens? Value, Exception? Error) HandleCodeLensResolve(ProtocolRequestContext context, CodeLens lens, RequestMessage request)
    {
        var(service, orchestrator, error) = GetLanguageServiceAndCrossProjectOrchestrator(context, lens.Data!.Uri, request);
        if (CancellationError(context)is { } canceled)
            return (null, canceled);
        if (error is not null)
            return (lens, new ProtocolErrorException(ErrorCode.ContentModified));
        try
        {
            return service!.ResolveCodeLens(context, lens, initializationOptions!.CodeLensShowLocationsCommandName, orchestrator);
        }
        catch (Exception panic)
        {
            Recover(request, panic);
            return (null, null);
        }
    }

    private (CallHierarchyIncomingCallsOrNull? Value, Exception? Error) HandleCallHierarchyIncomingCalls(ProtocolRequestContext context, CallHierarchyIncomingCallsParams parameters, RequestMessage request)
    {
        var(service, orchestrator, error) = GetLanguageServiceAndCrossProjectOrchestrator(context, parameters.Item!.Uri, request);
        return error is not null ? (default, error) : service!.ProvideCallHierarchyIncomingCalls(context, parameters.Item, orchestrator);
    }

    private (CallHierarchyOutgoingCallsOrNull? Value, Exception? Error) HandleCallHierarchyOutgoingCalls(ProtocolRequestContext context, CallHierarchyOutgoingCallsParams parameters, RequestMessage request)
    {
        var(service, error) = session!.GetLanguageService(context, parameters.Item!.Uri);
        return error is not null ? (default, error) : service!.ProvideCallHierarchyOutgoingCalls(context, parameters.Item);
    }
}
