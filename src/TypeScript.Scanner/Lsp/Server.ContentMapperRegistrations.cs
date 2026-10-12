// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:458–740. Ignored complete dynamic mapper registration owner.
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal Exception? RegisterContentMapperExtensions(ProtocolRequestContext context, Slice<ByteStringValue> extensions)
    {
        if (!clientCapabilities.TextDocument.Synchronization.DynamicRegistration)
            return null;
        lock (contentMapperRegistrationMu)
        {
            if (contentMapperExtensionsRegistered)
            {
                var unregistrations = Slice<Unregistration?>.FromArray([new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDidOpenRegistrationID),
                 Method = Method.TextDocumentDidOpen.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDidChangeRegistrationID),
                 Method = Method.TextDocumentDidChange.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDidCloseRegistrationID),
                 Method = Method.TextDocumentDidClose.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDiagnosticRegistrationID),
                 Method = Method.TextDocumentDiagnostic.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperHoverRegistrationID),
                 Method = Method.TextDocumentHover.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperSignatureHelpRegistrationID),
                 Method = Method.TextDocumentSignatureHelp.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDefinitionRegistrationID),
                 Method = Method.TextDocumentDefinition.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperTypeDefinitionRegistrationID),
                 Method = Method.TextDocumentTypeDefinition.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperImplementationRegistrationID),
                 Method = Method.TextDocumentImplementation.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperReferencesRegistrationID),
                 Method = Method.TextDocumentReferences.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDocumentHighlightRegistrationID),
                 Method = Method.TextDocumentDocumentHighlight.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperCompletionRegistrationID),
                 Method = Method.TextDocumentCompletion.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperRenameRegistrationID),
                 Method = Method.TextDocumentRename.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperSemanticTokensRegistrationID),
                 Method = Method.TextDocumentSemanticTokens.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperDocumentSymbolRegistrationID),
                 Method = Method.TextDocumentDocumentSymbol.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperFoldingRangeRegistrationID),
                 Method = Method.TextDocumentFoldingRange.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperSelectionRangeRegistrationID),
                 Method = Method.TextDocumentSelectionRange.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperInlayHintRegistrationID),
                 Method = Method.TextDocumentInlayHint.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperCodeLensRegistrationID),
                 Method = Method.TextDocumentCodeLens.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperCodeActionRegistrationID),
                 Method = Method.TextDocumentCodeAction.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperFormattingRegistrationID),
                 Method = Method.TextDocumentFormatting.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperRangeFormattingRegistrationID),
                 Method = Method.TextDocumentRangeFormatting.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperOnTypeFormattingRegistrationID),
                 Method = Method.TextDocumentOnTypeFormatting.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperLinkedEditingRegistrationID),
                 Method = Method.TextDocumentLinkedEditingRange.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperCallHierarchyRegistrationID),
                 Method = Method.TextDocumentPrepareCallHierarchy.Value }
                ,
                 new Unregistration {
                 Id = ByteStringValue.FromString(ContentMapperWillRenameFilesRegistrationID),
                 Method = Method.WorkspaceWillRenameFiles.Value }
                ]);

                unregistrations = GoSlices.DeleteFunc(unregistrations, item => !SupportsContentMapperRegistration(item!.Id));
                var(_, error) = SendClientRequest(context, Method.ClientUnregisterCapabilityInfo, new UnregistrationParams { Unregisterations = unregistrations });
                if (error is not null)
                    return new InvalidOperationException("failed to unregister content mapper text document sync: " + error.Message, error);
                contentMapperExtensionsRegistered = false;
            }

            if (extensions.Length == 0)
                return null;
            var filters = Slice<TextDocumentFilterLanguageOrSchemeOrPattern>.Make(0, extensions.Length);
            foreach (var extension in extensions)
                filters = filters.Append(new TextDocumentFilterLanguageOrSchemeOrPattern {
                 Pattern = new TextDocumentFilterPattern {
                 Pattern = new PatternOrRelativePattern {
                 Pattern = ByteStringValue.Concat(ByteStringValue.FromString("**/*"),
                 extension) }
                 }
                 }
                );

            var selector = new DocumentSelectorOrNull
            {
                DocumentSelector = filters
            };
            var contentMapperFileRenameFilters = Slice<FileOperationFilter?>.Make(0, extensions.Length);
            foreach (var extension in extensions)
                contentMapperFileRenameFilters = contentMapperFileRenameFilters.Append(new FileOperationFilter {
                 Scheme = ByteStringValue.FromString("file"),
                 Pattern = new FileOperationPattern {
                 Glob = ByteStringValue.Concat(ByteStringValue.FromString("**/*"),
                 extension) }
                 }
                );

            var registrations = Slice<Registration?>.FromArray([new Registration {
             Id = ByteStringValue.FromString(ContentMapperDidOpenRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDidOpen = new TextDocumentRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperDidChangeRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDidChange = new TextDocumentChangeRegistrationOptions {
             DocumentSelector = selector,
             SyncKind = TextDocumentSyncKind.Incremental,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperDidCloseRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDidClose = new TextDocumentRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperDiagnosticRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDiagnostic = new DiagnosticRegistrationOptions {
             DocumentSelector = selector,
             Identifier = ByteStringValue.FromString("typescript"),
             InterFileDependencies = true,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperHoverRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentHover = new HoverRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperSignatureHelpRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentSignatureHelp = new SignatureHelpRegistrationOptions {
             DocumentSelector = selector,
             TriggerCharacters = LanguageService.SignatureHelpTriggerCharacters,
             RetriggerCharacters = LanguageService.SignatureHelpRetriggerCharacters,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperDefinitionRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDefinition = new DefinitionRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperTypeDefinitionRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentTypeDefinition = new TypeDefinitionRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperImplementationRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentImplementation = new ImplementationRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperReferencesRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentReferences = new ReferenceRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperDocumentHighlightRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDocumentHighlight = new DocumentHighlightRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperCompletionRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentCompletion = new CompletionRegistrationOptions {
             DocumentSelector = selector,
             TriggerCharacters = LanguageService.CompletionTriggerCharacters,
             ResolveProvider = true,
             CompletionItem = new ServerCompletionItemOptions {
             LabelDetailsSupport = true,
             }
            ,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperRenameRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentRename = new RenameRegistrationOptions {
             DocumentSelector = selector,
             PrepareProvider = true,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperSemanticTokensRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentSemanticTokens = new SemanticTokensRegistrationOptions {
             DocumentSelector = selector,
             Legend = LanguageService.SemanticTokensLegend(clientCapabilities.TextDocument.SemanticTokens),
             Full = new BooleanOrSemanticTokensFullDelta {
             Boolean = true,
             }
            ,
             Range = new BooleanOrEmptyObject {
             Boolean = true,
             }
            ,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperDocumentSymbolRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentDocumentSymbol = new DocumentSymbolRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperFoldingRangeRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentFoldingRange = new FoldingRangeRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperSelectionRangeRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentSelectionRange = new SelectionRangeRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperInlayHintRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentInlayHint = new InlayHintRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperCodeLensRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentCodeLens = new CodeLensRegistrationOptions {
             DocumentSelector = selector,
             ResolveProvider = true,
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperCodeActionRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentCodeAction = new CodeActionRegistrationOptions {
             DocumentSelector = selector,
             CodeActionKinds = SupportedCodeActionKinds(),
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperFormattingRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentFormatting = new DocumentFormattingRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperRangeFormattingRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentRangeFormatting = new DocumentRangeFormattingRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperOnTypeFormattingRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentOnTypeFormatting = new DocumentOnTypeFormattingRegistrationOptions {
             DocumentSelector = selector,
             FirstTriggerCharacter = ByteStringValue.FromString("{"),
             MoreTriggerCharacter = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("}"),
             ByteStringValue.FromString(";"),
             ByteStringValue.FromString("\n")]),
             }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperLinkedEditingRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentLinkedEditingRange = new LinkedEditingRangeRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperCallHierarchyRegistrationID),
             RegisterOptions = new RegisterOptions {
             TextDocumentPrepareCallHierarchy = new CallHierarchyRegistrationOptions {
             DocumentSelector = selector }
            ,
             }
            ,
             }
            ,
             new Registration {
             Id = ByteStringValue.FromString(ContentMapperWillRenameFilesRegistrationID),
             RegisterOptions = new RegisterOptions {
             WorkspaceWillRenameFiles = new FileOperationRegistrationOptions {
             Filters = contentMapperFileRenameFilters }
            ,
             }
            ,
             }
            ]);

            registrations = GoSlices.DeleteFunc(registrations, item => !SupportsContentMapperRegistration(item!.Id));
            var(_, registrationError) = SendClientRequest(context, Method.ClientRegisterCapabilityInfo, new RegistrationParams { Registrations = registrations });
            if (registrationError is not null)
                return new InvalidOperationException("failed to register content mapper text document sync: " + registrationError.Message, registrationError);
            contentMapperExtensionsRegistered = true;
            return null;
        }
    }
}
