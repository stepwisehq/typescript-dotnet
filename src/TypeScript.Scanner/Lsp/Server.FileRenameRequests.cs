// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/lsp/server.go:2008–2108; retained whole-project service closure.
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ProtocolRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private readonly record struct RenameEditKey(DocumentUri Uri, ProtocolRange Range);
    private (WorkspaceEditOrNull Value, Exception? Error) HandleWillRenameFilesWorker(ProtocolRequestContext context, RenameFilesParams parameters, RequestMessage request, bool sendRenameFile)
    {
        if (parameters.Files.Length == 0)
            return (new(), null);
        var uris = Slice<DocumentUri>.Make(0, parameters.Files.Length);
        foreach (var file in parameters.Files)
            uris = uris.Append(file!.OldUri);
        if (uris.Length == 0)
            return (new(), null);
        var services = session!.GetLanguageServicesForDocumentsLoadingProjectTree(context, uris);
        var seenEdits = new Dictionary<RenameEditKey, ByteStringValue>();
        var seenRenames = new HashSet<DocumentUri>();
        Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile> documentChanges = default;
        foreach (var service in services)
            foreach (var file in parameters.Files)
                foreach (var change in service.GetEditsForFileRename(context, file!.OldUri, file.NewUri))
                {
                    if (change.RenameFile is { } rename)
                    {
                        if (seenRenames.Add(rename.OldUri))
                            documentChanges = documentChanges.Append(change);
                    }
                    else if (change.TextDocumentEdit is { } document)
                    {
                        var uri = document.TextDocument.Uri;
                        Slice<TextEditOrAnnotatedTextEditOrSnippetTextEdit> deduped = default;
                        foreach (var edit in document.Edits)
                        {
                            if (edit.TextEdit is { } text)
                            {
                                var key = new RenameEditKey(uri, text.Range);
                                if (seenEdits.TryGetValue(key, out var previous) && previous.EqualsValue(text.NewText))
                                    continue;
                                seenEdits[key] = text.NewText;
                            }

                            deduped = deduped.Append(edit);
                        }

                        if (deduped.Length != 0)
                            documentChanges = documentChanges.Append(new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile { TextDocumentEdit = new TextDocumentEdit { TextDocument = document.TextDocument, Edits = deduped } });
                    }
                }

        if (sendRenameFile)
            foreach (var file in parameters.Files)
                documentChanges = documentChanges.Append(new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile { RenameFile = new RenameFile { OldUri = file!.OldUri, NewUri = file.NewUri } });
        if (documentChanges.Length == 0)
            return (new(), null);
        if (LanguageService.ClientSupportsDocumentChanges(context))
            return (new WorkspaceEditOrNull { WorkspaceEdit = new WorkspaceEdit { DocumentChanges = documentChanges } }, null);
        var changes = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make();
        foreach (var change in documentChanges)
            if (change.TextDocumentEdit is { } document)
                foreach (var edit in document.Edits)
                    if (edit.TextEdit is { } text)
                        changes[document.TextDocument.Uri] = changes[document.TextDocument.Uri].Append(text);
        return (new WorkspaceEditOrNull { WorkspaceEdit = new WorkspaceEdit { Changes = changes } }, null);
    }
}
