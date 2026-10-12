// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1951–1964 and actual cross-project providers.
// Ignored genuine consumer draft; remaining file-rename edit owner is required.
using TypeScript.LanguageServices;
using TypeScript.Scanner;
using DiagnosticConverters = TypeScript.LanguageServices.Lsconv.Lsconv;
using TypeScript.Lsp.Protocol;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull Value, Exception? Error) HandlePrepareRename(ProtocolRequestContext context, LanguageService service, PrepareRenameParams? parameters)
    {
        var info = service.GetRenameInfo(context, default, parameters!.TextDocument.Uri, parameters.Position);
        if (!info.CanRename)
            return (new(), new UserFacingRequestFailedError(info.LocalizedErrorMessage));
        return (new() { PrepareRenamePlaceholder = new() { Range = info.TriggerSpan, Placeholder = info.DisplayName } }, null);
    }

    // server.go:1966–2003. The real file-rename edit service remains a required
    // later dependency of HandleWillRenameFilesWorker, not an empty fallback.
    private (WorkspaceEditOrNull Value, Exception? Error) HandleRename(ProtocolRequestContext context, RenameParams? parameters, RequestMessage request)
    {
        var(service, orchestrator, error) = GetLanguageServiceAndCrossProjectOrchestrator(context, parameters!.TextDocument.Uri, request);
        if (error is not null)
            return (new(), error);
        var info = service!.GetRenameInfo(context, parameters.NewName, parameters.TextDocument.Uri, parameters.Position);
        if (info.CanRename && info.FileToRename.AsValue().Length != 0)
        {
            if (LanguageService.ClientSupportsWillRenameFiles(context))
            {
                var documentChanges = Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile>.FromArray([new() { RenameFile = new() { OldUri = DiagnosticConverters.PathToDocumentURI(info.FileToRename), NewUri = DiagnosticConverters.PathToDocumentURI(info.NewFileName) } }]);
                return (new() { WorkspaceEdit = new() { DocumentChanges = documentChanges } }, null);
            }

            var renameFiles = new RenameFilesParams
            {
                Files = Slice<FileRename?>.FromArray([new FileRename { OldUri = DiagnosticConverters.PathToDocumentURI(info.FileToRename), NewUri = DiagnosticConverters.PathToDocumentURI(info.NewFileName) }])
            };
            return HandleWillRenameFilesWorker(context, renameFiles, request, true);
        }

        return service.ProvideRename(context, parameters, orchestrator);
    }

    // server.go:2005. Full worker admission/iteration is retained in the pending
    // file-rename closure once its genuine GetEditsForFileRename service arrives.
    private (WorkspaceEditOrNull Value, Exception? Error) HandleWillRenameFiles(ProtocolRequestContext context, RenameFilesParams? parameters, RequestMessage request) => HandleWillRenameFilesWorker(context, parameters!, request, false);
    // server.go:2241–2247. Preserve the real multi-document search input header.
    private (DocumentHighlightsOrNull Value, Exception? Error) HandleDocumentHighlight(ProtocolRequestContext context, LanguageService service, DocumentHighlightParams? parameters) => service.ProvideDocumentHighlights(context, parameters!.TextDocument.Uri, parameters.Position);
    private (MultiDocumentHighlightsOrNull Value, Exception? Error) HandleMultiDocumentHighlight(ProtocolRequestContext context, LanguageService service, MultiDocumentHighlightParams? parameters) => service.ProvideMultiDocumentHighlights(context, parameters!.TextDocument.Uri, parameters.Position, parameters.FilesToSearch);
}
