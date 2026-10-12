// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated closed model joins; tools/lsp/generate-codecs.py.
#nullable enable
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolCodec
{
    internal static bool TryReadIntrinsicProtocolValue(ref ProtocolDecoder decoder, ref object? value)
    {
        switch (value)
        {
            case SemanticTokenType item:
                try
                {
                    ReadShape_ffe0cd0a8bc7038a(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case SemanticTokenModifier item:
                try
                {
                    ReadShape_6b5b03f3fef5cc31(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case DocumentDiagnosticReportKind item:
                try
                {
                    ReadShape_b8295814ec2d845f(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case ErrorCode item:
                try
                {
                    ReadShape_b1b889e0a6a2ea91(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case FoldingRangeKind item:
                try
                {
                    ReadShape_9bed5894ab8b426e(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case SymbolKind item:
                try
                {
                    ReadShape_80525ce65b460521(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case SymbolTag item:
                try
                {
                    ReadShape_977c9077c2130dba(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case UniquenessLevel item:
                try
                {
                    ReadShape_52a1f306704bcdf2(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case MonikerKind item:
                try
                {
                    ReadShape_7d2dfd2063c3273d(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case InlayHintKind item:
                try
                {
                    ReadShape_53926c928508e1cb(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case MessageType item:
                try
                {
                    ReadShape_6ff9dd7ca34940c3(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case TextDocumentSyncKind item:
                try
                {
                    ReadShape_1259c5dfc44b321e(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case TextDocumentSaveReason item:
                try
                {
                    ReadShape_9ba5b7e396b1b1ca(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CompletionItemKind item:
                try
                {
                    ReadShape_b8f2471eb8fc0cb3(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CompletionItemTag item:
                try
                {
                    ReadShape_5397ab5a342cc703(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case InsertTextFormat item:
                try
                {
                    ReadShape_0125b1afb99b0757(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case InsertTextMode item:
                try
                {
                    ReadShape_6cddb8f12d636f30(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case DocumentHighlightKind item:
                try
                {
                    ReadShape_5f2a94526ab2d089(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CodeActionKind item:
                try
                {
                    ReadShape_ecff755ce44421bb(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CodeActionTag item:
                try
                {
                    ReadShape_22fd923137cdbcf0(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case TraceValue item:
                try
                {
                    ReadShape_98b926ad367a3bcb(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case MarkupKind item:
                try
                {
                    ReadShape_2ecdda95186829dd(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case LanguageKind item:
                try
                {
                    ReadShape_74d0ea0a98141b4c(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case InlineCompletionTriggerKind item:
                try
                {
                    ReadShape_163ef7bab0fae34d(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case PositionEncodingKind item:
                try
                {
                    ReadShape_996a119923a77207(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case FileChangeType item:
                try
                {
                    ReadShape_1d3058865d82fb8b(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case WatchKind item:
                try
                {
                    ReadShape_df60ed0d4edd21e9(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case DiagnosticSeverity item:
                try
                {
                    ReadShape_bab4fe1f8b21f9fa(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case DiagnosticTag item:
                try
                {
                    ReadShape_1a56dbf398d99d2f(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CompletionTriggerKind item:
                try
                {
                    ReadShape_08b806e2da960947(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case ApplyKind item:
                try
                {
                    ReadShape_14b702d0db1726aa(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case SignatureHelpTriggerKind item:
                try
                {
                    ReadShape_a60e6e214e11d2ad(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CodeActionTriggerKind item:
                try
                {
                    ReadShape_01415bcc3e8debdf(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case FileOperationPatternKind item:
                try
                {
                    ReadShape_c9ac6b08cd0fcaec(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case ResourceOperationKind item:
                try
                {
                    ReadShape_3eec28594f7be075(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case FailureHandlingKind item:
                try
                {
                    ReadShape_484e4404d3769d00(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case PrepareSupportDefaultBehavior item:
                try
                {
                    ReadShape_491ada003a0c8630(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case TokenFormat item:
                try
                {
                    ReadShape_61c52b50223120a3(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case VSContainerElementStyle item:
                try
                {
                    ReadShape_474d572894555703(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case LogVerbosity item:
                try
                {
                    ReadShape_061133d05f438d15(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case DiagnosticFlakeLogLevel item:
                try
                {
                    ReadShape_1ca005e11eb3df3a(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case VSReferenceKind item:
                try
                {
                    ReadShape_20bf14346b717f60(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case CodeLensKind item:
                try
                {
                    ReadShape_4be58987e0e9adc8(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case AutoImportFixKind item:
                try
                {
                    ReadShape_3cb7a6f693cd9a7a(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case ImportKind item:
                try
                {
                    ReadShape_d39772dfff646e6d(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case AddAsTypeOnly item:
                try
                {
                    ReadShape_5bfde1568463cec7(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case ClassificationTypeName item:
                try
                {
                    ReadShape_2517ecc24dbf1859(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case DocumentUri item:
                try
                {
                    ReadDocumentUri(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case URI item:
                try
                {
                    ReadURI(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case Method item:
                try
                {
                    ReadMethod(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case Null item:
                try
                {
                    ReadNull(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case NoParams item:
                try
                {
                    ReadNoParams(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case EmptyProtocolObject item:
                try
                {
                    ReadEmptyProtocolObject(ref decoder, ref item);
                }
                finally
                {
                    value = item;
                }

                return true;
            case int item:
                try
                {
                    item = decoder.ReadInt32();
                }
                finally
                {
                    value = item;
                }

                return true;
            case uint item:
                try
                {
                    item = decoder.ReadUint32();
                }
                finally
                {
                    value = item;
                }

                return true;
            case ByteStringValue item:
                try
                {
                    item = decoder.ReadString();
                }
                finally
                {
                    value = item;
                }

                return true;
            default:
                return false;
        }
    }
}
