// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated closed model joins; tools/lsp/generate-codecs.py.
#nullable enable
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolCodec
{
    internal static bool TryWriteIntrinsicProtocolValue(ProtocolEncoder encoder, object value)
    {
        switch (value)
        {
            case SemanticTokenType item:
                WriteShape_ffe0cd0a8bc7038a(encoder, item);
                return true;
            case SemanticTokenModifier item:
                WriteShape_6b5b03f3fef5cc31(encoder, item);
                return true;
            case DocumentDiagnosticReportKind item:
                WriteShape_b8295814ec2d845f(encoder, item);
                return true;
            case ErrorCode item:
                WriteShape_b1b889e0a6a2ea91(encoder, item);
                return true;
            case FoldingRangeKind item:
                WriteShape_9bed5894ab8b426e(encoder, item);
                return true;
            case SymbolKind item:
                WriteShape_80525ce65b460521(encoder, item);
                return true;
            case SymbolTag item:
                WriteShape_977c9077c2130dba(encoder, item);
                return true;
            case UniquenessLevel item:
                WriteShape_52a1f306704bcdf2(encoder, item);
                return true;
            case MonikerKind item:
                WriteShape_7d2dfd2063c3273d(encoder, item);
                return true;
            case InlayHintKind item:
                WriteShape_53926c928508e1cb(encoder, item);
                return true;
            case MessageType item:
                WriteShape_6ff9dd7ca34940c3(encoder, item);
                return true;
            case TextDocumentSyncKind item:
                WriteShape_1259c5dfc44b321e(encoder, item);
                return true;
            case TextDocumentSaveReason item:
                WriteShape_9ba5b7e396b1b1ca(encoder, item);
                return true;
            case CompletionItemKind item:
                WriteShape_b8f2471eb8fc0cb3(encoder, item);
                return true;
            case CompletionItemTag item:
                WriteShape_5397ab5a342cc703(encoder, item);
                return true;
            case InsertTextFormat item:
                WriteShape_0125b1afb99b0757(encoder, item);
                return true;
            case InsertTextMode item:
                WriteShape_6cddb8f12d636f30(encoder, item);
                return true;
            case DocumentHighlightKind item:
                WriteShape_5f2a94526ab2d089(encoder, item);
                return true;
            case CodeActionKind item:
                WriteShape_ecff755ce44421bb(encoder, item);
                return true;
            case CodeActionTag item:
                WriteShape_22fd923137cdbcf0(encoder, item);
                return true;
            case TraceValue item:
                WriteShape_98b926ad367a3bcb(encoder, item);
                return true;
            case MarkupKind item:
                WriteShape_2ecdda95186829dd(encoder, item);
                return true;
            case LanguageKind item:
                WriteShape_74d0ea0a98141b4c(encoder, item);
                return true;
            case InlineCompletionTriggerKind item:
                WriteShape_163ef7bab0fae34d(encoder, item);
                return true;
            case PositionEncodingKind item:
                WriteShape_996a119923a77207(encoder, item);
                return true;
            case FileChangeType item:
                WriteShape_1d3058865d82fb8b(encoder, item);
                return true;
            case WatchKind item:
                WriteShape_df60ed0d4edd21e9(encoder, item);
                return true;
            case DiagnosticSeverity item:
                WriteShape_bab4fe1f8b21f9fa(encoder, item);
                return true;
            case DiagnosticTag item:
                WriteShape_1a56dbf398d99d2f(encoder, item);
                return true;
            case CompletionTriggerKind item:
                WriteShape_08b806e2da960947(encoder, item);
                return true;
            case ApplyKind item:
                WriteShape_14b702d0db1726aa(encoder, item);
                return true;
            case SignatureHelpTriggerKind item:
                WriteShape_a60e6e214e11d2ad(encoder, item);
                return true;
            case CodeActionTriggerKind item:
                WriteShape_01415bcc3e8debdf(encoder, item);
                return true;
            case FileOperationPatternKind item:
                WriteShape_c9ac6b08cd0fcaec(encoder, item);
                return true;
            case ResourceOperationKind item:
                WriteShape_3eec28594f7be075(encoder, item);
                return true;
            case FailureHandlingKind item:
                WriteShape_484e4404d3769d00(encoder, item);
                return true;
            case PrepareSupportDefaultBehavior item:
                WriteShape_491ada003a0c8630(encoder, item);
                return true;
            case TokenFormat item:
                WriteShape_61c52b50223120a3(encoder, item);
                return true;
            case VSContainerElementStyle item:
                WriteShape_474d572894555703(encoder, item);
                return true;
            case LogVerbosity item:
                WriteShape_061133d05f438d15(encoder, item);
                return true;
            case DiagnosticFlakeLogLevel item:
                WriteShape_1ca005e11eb3df3a(encoder, item);
                return true;
            case VSReferenceKind item:
                WriteShape_20bf14346b717f60(encoder, item);
                return true;
            case CodeLensKind item:
                WriteShape_4be58987e0e9adc8(encoder, item);
                return true;
            case AutoImportFixKind item:
                WriteShape_3cb7a6f693cd9a7a(encoder, item);
                return true;
            case ImportKind item:
                WriteShape_d39772dfff646e6d(encoder, item);
                return true;
            case AddAsTypeOnly item:
                WriteShape_5bfde1568463cec7(encoder, item);
                return true;
            case ClassificationTypeName item:
                WriteShape_2517ecc24dbf1859(encoder, item);
                return true;
            case DocumentUri item:
                WriteDocumentUri(encoder, item);
                return true;
            case URI item:
                WriteURI(encoder, item);
                return true;
            case Method item:
                WriteMethod(encoder, item);
                return true;
            case Null item:
                WriteNull(encoder, item);
                return true;
            case NoParams item:
                WriteNoParams(encoder, item);
                return true;
            case EmptyProtocolObject item:
                WriteEmptyProtocolObject(encoder, item);
                return true;
            default:
                return false;
        }
    }
}
