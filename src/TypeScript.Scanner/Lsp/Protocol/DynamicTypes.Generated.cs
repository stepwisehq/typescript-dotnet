// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated closed model joins; tools/lsp/generate-codecs.py.
#nullable enable
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolDynamicTypes
{
    private static string IntrinsicName(object value) => value switch
    {
        SemanticTokenType => "lsproto.SemanticTokenType",
        SemanticTokenModifier => "lsproto.SemanticTokenModifier",
        DocumentDiagnosticReportKind => "lsproto.DocumentDiagnosticReportKind",
        ErrorCode => "lsproto.ErrorCode",
        FoldingRangeKind => "lsproto.FoldingRangeKind",
        SymbolKind => "lsproto.SymbolKind",
        SymbolTag => "lsproto.SymbolTag",
        UniquenessLevel => "lsproto.UniquenessLevel",
        MonikerKind => "lsproto.MonikerKind",
        InlayHintKind => "lsproto.InlayHintKind",
        MessageType => "lsproto.MessageType",
        TextDocumentSyncKind => "lsproto.TextDocumentSyncKind",
        TextDocumentSaveReason => "lsproto.TextDocumentSaveReason",
        CompletionItemKind => "lsproto.CompletionItemKind",
        CompletionItemTag => "lsproto.CompletionItemTag",
        InsertTextFormat => "lsproto.InsertTextFormat",
        InsertTextMode => "lsproto.InsertTextMode",
        DocumentHighlightKind => "lsproto.DocumentHighlightKind",
        CodeActionKind => "lsproto.CodeActionKind",
        CodeActionTag => "lsproto.CodeActionTag",
        TraceValue => "lsproto.TraceValue",
        MarkupKind => "lsproto.MarkupKind",
        LanguageKind => "lsproto.LanguageKind",
        InlineCompletionTriggerKind => "lsproto.InlineCompletionTriggerKind",
        PositionEncodingKind => "lsproto.PositionEncodingKind",
        FileChangeType => "lsproto.FileChangeType",
        WatchKind => "lsproto.WatchKind",
        DiagnosticSeverity => "lsproto.DiagnosticSeverity",
        DiagnosticTag => "lsproto.DiagnosticTag",
        CompletionTriggerKind => "lsproto.CompletionTriggerKind",
        ApplyKind => "lsproto.ApplyKind",
        SignatureHelpTriggerKind => "lsproto.SignatureHelpTriggerKind",
        CodeActionTriggerKind => "lsproto.CodeActionTriggerKind",
        FileOperationPatternKind => "lsproto.FileOperationPatternKind",
        ResourceOperationKind => "lsproto.ResourceOperationKind",
        FailureHandlingKind => "lsproto.FailureHandlingKind",
        PrepareSupportDefaultBehavior => "lsproto.PrepareSupportDefaultBehavior",
        TokenFormat => "lsproto.TokenFormat",
        VSContainerElementStyle => "lsproto.VSContainerElementStyle",
        LogVerbosity => "lsproto.LogVerbosity",
        DiagnosticFlakeLogLevel => "lsproto.DiagnosticFlakeLogLevel",
        VSReferenceKind => "lsproto.VSReferenceKind",
        CodeLensKind => "lsproto.CodeLensKind",
        AutoImportFixKind => "lsproto.AutoImportFixKind",
        ImportKind => "lsproto.ImportKind",
        AddAsTypeOnly => "lsproto.AddAsTypeOnly",
        ClassificationTypeName => "lsproto.ClassificationTypeName",
        DocumentUri => "lsproto.DocumentUri",
        URI => "lsproto.URI",
        Method => "lsproto.Method",
        Null => "lsproto.Null",
        NoParams => "lsproto.NoParams",
        EmptyProtocolObject => "struct {}",
        _ => throw new InvalidOperationException("unregistered dynamic protocol JSON type"),
    };
}
