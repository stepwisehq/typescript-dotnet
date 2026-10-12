// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed test-only structural equality preserving Go nil, pointer and slice distinctions.
#nullable enable
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using Range = TypeScript.Lsp.Protocol.Range;
using Diagnostic = TypeScript.Lsp.Protocol.Diagnostic;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static bool EqualShape_5fdd995f16212fef(AnnotatedTextEdit? a, AnnotatedTextEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dc76475f4b1ed640(a, b);
    }

    private static bool EqualShape_539cd689badc5bf0(ApplyKind? a, ApplyKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_14b702d0db1726aa(a.Value, b.Value);
    }

    private static bool EqualShape_0eb713b09b092b3b(ApplyWorkspaceEditParams? a, ApplyWorkspaceEditParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_54526a811be46335(a, b);
    }

    private static bool EqualShape_77dd28538be52d1a(ApplyWorkspaceEditResult? a, ApplyWorkspaceEditResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6d7f4c2b0309a43e(a, b);
    }

    private static bool EqualShape_5a6a4da133030761(AutoImportFix? a, AutoImportFix? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2fa6cdfb6ada2628(a, b);
    }

    private static bool EqualShape_ea9906a90cd63255(BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions? a, BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_446299ae2bd49e81(a, b);
    }

    private static bool EqualShape_b4c3dc66bb9d16b0(BooleanOrClientSemanticTokensRequestFullDelta? a, BooleanOrClientSemanticTokensRequestFullDelta? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ceb7a942d3e190ba(a.Value, b.Value);
    }

    private static bool EqualShape_13036b4475c995d5(BooleanOrCodeActionOptions? a, BooleanOrCodeActionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e9690919b3d8115d(a, b);
    }

    private static bool EqualShape_77c7b71b2461b29c(BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions? a, BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_145e615b6579c981(a, b);
    }

    private static bool EqualShape_259dbc4245dbc446(BooleanOrDefinitionOptions? a, BooleanOrDefinitionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cf52b6cb9db4fe19(a, b);
    }

    private static bool EqualShape_0c5bef69fdd57a46(BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions? a, BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1474d27e4d0c6fa5(a, b);
    }

    private static bool EqualShape_785ae5941ed13886(BooleanOrDocumentFormattingOptions? a, BooleanOrDocumentFormattingOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a917cb45ee3cef42(a, b);
    }

    private static bool EqualShape_f1cff6dd6c13f7f1(BooleanOrDocumentHighlightOptions? a, BooleanOrDocumentHighlightOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_54fc7b1b34343b4a(a, b);
    }

    private static bool EqualShape_ae4ad4f64fbd3d83(BooleanOrDocumentRangeFormattingOptions? a, BooleanOrDocumentRangeFormattingOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c5052a5c26268d1f(a, b);
    }

    private static bool EqualShape_b2a4ac939659fc9f(BooleanOrDocumentSymbolOptions? a, BooleanOrDocumentSymbolOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c021a4c2316929b7(a, b);
    }

    private static bool EqualShape_5b602f5e58c34242(BooleanOrEmptyObject? a, BooleanOrEmptyObject? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0464b31e8eb7a40e(a.Value, b.Value);
    }

    private static bool EqualShape_6240b47f5020cfc1(BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions? a, BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0a0a219b5ffede8b(a, b);
    }

    private static bool EqualShape_61c783782174fcc1(BooleanOrHoverOptions? a, BooleanOrHoverOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_baa50df5ae26800a(a, b);
    }

    private static bool EqualShape_da11e3aa093439aa(BooleanOrImplementationOptionsOrImplementationRegistrationOptions? a, BooleanOrImplementationOptionsOrImplementationRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_15d10a5acdcde7ec(a, b);
    }

    private static bool EqualShape_a4ab8b7c4476ab01(BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions? a, BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_feb69b4dbb92199a(a, b);
    }

    private static bool EqualShape_10f353960870ef44(BooleanOrInlineCompletionOptions? a, BooleanOrInlineCompletionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_959a1d992a272787(a, b);
    }

    private static bool EqualShape_31859f7808a72778(BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions? a, BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1d2b7a4459f0e39b(a, b);
    }

    private static bool EqualShape_d3a0d0010acc34e0(BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions? a, BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0d2d61395bd0e247(a, b);
    }

    private static bool EqualShape_1f6a23b003d18878(BooleanOrMonikerOptionsOrMonikerRegistrationOptions? a, BooleanOrMonikerOptionsOrMonikerRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_afd51367d6d2382c(a, b);
    }

    private static bool EqualShape_0c5a48b6e102e5c9(BooleanOrReferenceOptions? a, BooleanOrReferenceOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3b3fec573e871e5d(a, b);
    }

    private static bool EqualShape_2b687e3cf5129ec6(BooleanOrRenameOptions? a, BooleanOrRenameOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_504bddb6f7a15921(a, b);
    }

    private static bool EqualShape_f7ce9c5ebf99ce16(BooleanOrSaveOptions? a, BooleanOrSaveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_41952583e79cd693(a, b);
    }

    private static bool EqualShape_82434037fc61936c(BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions? a, BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5d2769c3484cdd3e(a, b);
    }

    private static bool EqualShape_c2aad4b3c18b521c(BooleanOrSemanticTokensFullDelta? a, BooleanOrSemanticTokensFullDelta? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7df65eef7163c6f4(a, b);
    }

    private static bool EqualShape_ff8bcc4e001f966d(BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions? a, BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ed71ad313eefc7ad(a, b);
    }

    private static bool EqualShape_d450ab36f6254939(BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions? a, BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_87827fc2be5c0773(a, b);
    }

    private static bool EqualShape_b0181fd8a1566118(BooleanOrWorkspaceSymbolOptions? a, BooleanOrWorkspaceSymbolOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e5bfff85c753a8b1(a, b);
    }

    private static bool EqualShape_a097ceb150983cc8(CallHierarchyClientCapabilities? a, CallHierarchyClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_383095cfb255a14d(a, b);
    }

    private static bool EqualShape_41e3a0bbc0502807(CallHierarchyIncomingCall? a, CallHierarchyIncomingCall? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e184a9b2f92e1a06(a, b);
    }

    private static bool EqualShape_ed03e962779d614b(CallHierarchyIncomingCallsParams? a, CallHierarchyIncomingCallsParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3a8cdb686bf71f77(a, b);
    }

    private static bool EqualShape_52678145e6f3309d(CallHierarchyItem? a, CallHierarchyItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8c7fd0716410365c(a, b);
    }

    private static bool EqualShape_6e332018a1c206ec(CallHierarchyItemData? a, CallHierarchyItemData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_65359ea06e6d25e6(a.Value, b.Value);
    }

    private static bool EqualShape_7d3a2c5f8846331f(CallHierarchyOptions? a, CallHierarchyOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bf40a5eb811d3651(a, b);
    }

    private static bool EqualShape_1e0ec82d8ba4bc44(CallHierarchyOutgoingCall? a, CallHierarchyOutgoingCall? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4589918246a73c8d(a, b);
    }

    private static bool EqualShape_025e7254fe5230b5(CallHierarchyOutgoingCallsParams? a, CallHierarchyOutgoingCallsParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_683273adad7a85de(a, b);
    }

    private static bool EqualShape_bc44762ff14269d3(CallHierarchyPrepareParams? a, CallHierarchyPrepareParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0c095bc834e4d3c8(a, b);
    }

    private static bool EqualShape_f3fc39c6644381af(CallHierarchyRegistrationOptions? a, CallHierarchyRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1f08ef49b14db62c(a, b);
    }

    private static bool EqualShape_41887763d421c10d(CancelParams? a, CancelParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_68e14858b5be82dc(a, b);
    }

    private static bool EqualShape_6a13e7db61712509(ChangeAnnotation? a, ChangeAnnotation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95a49b2bfa6a44cd(a, b);
    }

    private static bool EqualShape_4e27d72802a002a5(ChangeAnnotationsSupportOptions? a, ChangeAnnotationsSupportOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_767b65d5f0d728e6(a, b);
    }

    private static bool EqualShape_29c18c60ff8bc658(ClientCapabilities? a, ClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ae5beebabf5633ee(a, b);
    }

    private static bool EqualShape_29d190fdf01733b6(ClientCodeActionKindOptions? a, ClientCodeActionKindOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c1f39ce851ecc9a0(a, b);
    }

    private static bool EqualShape_56d6bf9bb39696ab(ClientCodeActionLiteralOptions? a, ClientCodeActionLiteralOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_97349393a7e84c9a(a, b);
    }

    private static bool EqualShape_45d71b781cb3ee1d(ClientCodeActionResolveOptions? a, ClientCodeActionResolveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_56c69147319a7d0b(a, b);
    }

    private static bool EqualShape_50f962c22f1d0a22(ClientCodeLensResolveOptions? a, ClientCodeLensResolveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d931abba359e22bd(a, b);
    }

    private static bool EqualShape_54a61a2bbc545d95(ClientCompletionItemInsertTextModeOptions? a, ClientCompletionItemInsertTextModeOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_28bf4fd56d374e67(a, b);
    }

    private static bool EqualShape_9fe8908a524bc20a(ClientCompletionItemOptions? a, ClientCompletionItemOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b37b4bbb7ef32304(a, b);
    }

    private static bool EqualShape_3e40fa5e66a3fd31(ClientCompletionItemOptionsKind? a, ClientCompletionItemOptionsKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ea7a2cb221fe3126(a, b);
    }

    private static bool EqualShape_0b540d7879247121(ClientCompletionItemResolveOptions? a, ClientCompletionItemResolveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_08243bf3c503d83a(a, b);
    }

    private static bool EqualShape_01178517db2f6abb(ClientDiagnosticsTagOptions? a, ClientDiagnosticsTagOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e354d3fe1df50a36(a, b);
    }

    private static bool EqualShape_b2a2fec66de9fc74(ClientFoldingRangeKindOptions? a, ClientFoldingRangeKindOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_51037275a4d10b5f(a, b);
    }

    private static bool EqualShape_c7202684a50e9e09(ClientFoldingRangeOptions? a, ClientFoldingRangeOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a627df9eb6b0b5da(a, b);
    }

    private static bool EqualShape_98be1a1b6cdb3d2e(ClientInfo? a, ClientInfo? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e58e814bcb67ebf4(a, b);
    }

    private static bool EqualShape_b51791d12c689d69(ClientInlayHintResolveOptions? a, ClientInlayHintResolveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_43b1b75d4aa5fd41(a, b);
    }

    private static bool EqualShape_58d29bcb4ddfa875(ClientSemanticTokensRequestFullDelta? a, ClientSemanticTokensRequestFullDelta? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e1f502acb9f727c5(a, b);
    }

    private static bool EqualShape_60a32c284332509f(ClientSemanticTokensRequestOptions? a, ClientSemanticTokensRequestOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1e6c97fe656a3276(a, b);
    }

    private static bool EqualShape_beac3eba343e80bd(ClientShowMessageActionItemOptions? a, ClientShowMessageActionItemOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0d5e37ca0a60abe2(a, b);
    }

    private static bool EqualShape_ba85fc2bf9b60b3e(ClientSignatureInformationOptions? a, ClientSignatureInformationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6a3fd3a20dce8c99(a, b);
    }

    private static bool EqualShape_c7579c3877cec4b4(ClientSignatureParameterInformationOptions? a, ClientSignatureParameterInformationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_133fc1cbd2a995c6(a, b);
    }

    private static bool EqualShape_ef83c3195ef91327(ClientSymbolKindOptions? a, ClientSymbolKindOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bfc7ea6558336881(a, b);
    }

    private static bool EqualShape_9a1297d4a6494114(ClientSymbolResolveOptions? a, ClientSymbolResolveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8e0f26b4017e860f(a, b);
    }

    private static bool EqualShape_56b6c5f1c4a81226(ClientSymbolTagOptions? a, ClientSymbolTagOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_51e4a8fb8975920a(a, b);
    }

    private static bool EqualShape_a85aecd48f6d75ee(CodeAction? a, CodeAction? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_865c9c8fafe5c729(a, b);
    }

    private static bool EqualShape_6796dc2dfcb4b687(CodeActionClientCapabilities? a, CodeActionClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e7f7570213c73b8e(a, b);
    }

    private static bool EqualShape_428ffbee266930e2(CodeActionContext? a, CodeActionContext? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5e9db653ca06c3c9(a, b);
    }

    private static bool EqualShape_ab7c5b654dbfe20d(CodeActionData? a, CodeActionData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_64c59af8f44adf5e(a.Value, b.Value);
    }

    private static bool EqualShape_e83e8f04df156ab0(CodeActionDisabled? a, CodeActionDisabled? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_081648c1b4ef6f5e(a, b);
    }

    private static bool EqualShape_34290cef18c63dd2(CodeActionKind? a, CodeActionKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ecff755ce44421bb(a.Value, b.Value);
    }

    private static bool EqualShape_56c96d7eb180a250(CodeActionKindDocumentation? a, CodeActionKindDocumentation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4271ba828c998e5f(a, b);
    }

    private static bool EqualShape_06a20c8c465f1bd8(CodeActionOptions? a, CodeActionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c349820ded303c22(a, b);
    }

    private static bool EqualShape_63a9f0c4b9da7caf(CodeActionParams? a, CodeActionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5fd24f9135249493(a, b);
    }

    private static bool EqualShape_735a891d6156b340(CodeActionRegistrationOptions? a, CodeActionRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9b2193d2ea04428b(a, b);
    }

    private static bool EqualShape_a161c2bc3bcc3cc8(CodeActionTagOptions? a, CodeActionTagOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_412c1650085309d1(a, b);
    }

    private static bool EqualShape_b5d5feda1821575b(CodeActionTriggerKind? a, CodeActionTriggerKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_01415bcc3e8debdf(a.Value, b.Value);
    }

    private static bool EqualShape_4b00f8336e551717(CodeDescription? a, CodeDescription? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d5d05065bc890cc5(a, b);
    }

    private static bool EqualShape_49a1f25275c46426(CodeLens? a, CodeLens? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9e0c959c15c790a3(a, b);
    }

    private static bool EqualShape_5485c6a4027eb67a(CodeLensClientCapabilities? a, CodeLensClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f0921b0a90d3de37(a, b);
    }

    private static bool EqualShape_5b5c06a0e17dacd3(CodeLensData? a, CodeLensData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f549b1b2ff50cdf8(a, b);
    }

    private static bool EqualShape_53c738698791fca8(CodeLensOptions? a, CodeLensOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b5c29b63aaa2a6f8(a, b);
    }

    private static bool EqualShape_a9fef69240f29dea(CodeLensParams? a, CodeLensParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4a62670ff2e4f6dc(a, b);
    }

    private static bool EqualShape_5b4d0277cba25f93(CodeLensRegistrationOptions? a, CodeLensRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d5d1b6fc9d1f6ede(a, b);
    }

    private static bool EqualShape_09bdab53659d84ac(CodeLensWorkspaceClientCapabilities? a, CodeLensWorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_27b4581cda4b0d84(a, b);
    }

    private static bool EqualShape_a16099c806419f32(ColorInformation? a, ColorInformation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d5d2d1066d39a9f1(a, b);
    }

    private static bool EqualShape_0d923cd5be75d1dd(ColorPresentation? a, ColorPresentation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1f1f6c1ccc446b5b(a, b);
    }

    private static bool EqualShape_2c666a557a1efac5(ColorPresentationParams? a, ColorPresentationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ef2f7018da26a917(a, b);
    }

    private static bool EqualShape_a6c992bdda521682(Command? a, Command? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_713166971d730f81(a, b);
    }

    private static bool EqualShape_e7da12038bd3e115(CompletionClientCapabilities? a, CompletionClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_16f91af89436100d(a, b);
    }

    private static bool EqualShape_f06cca5d2d4cbbca(CompletionContext? a, CompletionContext? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e236ca93686fcfa1(a, b);
    }

    private static bool EqualShape_cf50404157ead35b(CompletionItem? a, CompletionItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d2a93ce39bb367de(a, b);
    }

    private static bool EqualShape_9e95dd258053a4f7(CompletionItemApplyKinds? a, CompletionItemApplyKinds? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_946202155aedc75c(a, b);
    }

    private static bool EqualShape_30f6a0420ca353f5(CompletionItemData? a, CompletionItemData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ec574f9f7f49a56a(a, b);
    }

    private static bool EqualShape_a0a77280d06f4e70(CompletionItemDefaults? a, CompletionItemDefaults? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_467395eb042a5b79(a, b);
    }

    private static bool EqualShape_a27705e55c8812a8(CompletionItemDefaultsData? a, CompletionItemDefaultsData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_04b1eeb817033ba6(a.Value, b.Value);
    }

    private static bool EqualShape_7b038bf5ef819f1b(CompletionItemKind? a, CompletionItemKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b8f2471eb8fc0cb3(a.Value, b.Value);
    }

    private static bool EqualShape_8f3b8df56b881212(CompletionItemLabelDetails? a, CompletionItemLabelDetails? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4918d4bf2593f8af(a, b);
    }

    private static bool EqualShape_7730fd624a35ac9d(CompletionItemTagOptions? a, CompletionItemTagOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9d1d9d8587fbcefd(a, b);
    }

    private static bool EqualShape_4fcf690eadce272f(CompletionList? a, CompletionList? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f8ad52b4cd7e25cc(a, b);
    }

    private static bool EqualShape_64f00aa06587937a(CompletionListCapabilities? a, CompletionListCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c850bc8b5898a45e(a, b);
    }

    private static bool EqualShape_9e007aa38ffe63d0(CompletionOptions? a, CompletionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1d476dc84cbde137(a, b);
    }

    private static bool EqualShape_b0765757b0232b19(CompletionParams? a, CompletionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a52f8dcb7c004673(a, b);
    }

    private static bool EqualShape_94d985c5c25d2f44(CompletionRegistrationOptions? a, CompletionRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_78ef87019ed5c3c1(a, b);
    }

    private static bool EqualShape_7cc978bb2dea8225(ConfigurationItem? a, ConfigurationItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6fb58034d4f05554(a, b);
    }

    private static bool EqualShape_bc8e4faf0404a553(ConfigurationParams? a, ConfigurationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_99d81d86d0860ed1(a, b);
    }

    private static bool EqualShape_7f829fb51f96000f(ContentMapperContribution? a, ContentMapperContribution? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dfd2d49e4be6f7a1(a, b);
    }

    private static bool EqualShape_bcc8effb9176a1cf(ContentMapperManifest? a, ContentMapperManifest? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ac126e252e4ca42c(a, b);
    }

    private static bool EqualShape_20b1e7f66489189e(CreateFile? a, CreateFile? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9d3a24d3495132d7(a, b);
    }

    private static bool EqualShape_0d4f750a1e2fe6dc(CreateFileOptions? a, CreateFileOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ad7b628c8a318f7f(a, b);
    }

    private static bool EqualShape_738bf8dff23f3605(CreateFilesParams? a, CreateFilesParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c1b6632f7bfe8d76(a, b);
    }

    private static bool EqualShape_bffa125f221ca999(DeclarationClientCapabilities? a, DeclarationClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d092d2b3e5d67c33(a, b);
    }

    private static bool EqualShape_13687efc1ec01c3a(DeclarationOptions? a, DeclarationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_55db17bfd8074c50(a, b);
    }

    private static bool EqualShape_4a300e84bcca2752(DeclarationParams? a, DeclarationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_67e2762ff01d380a(a, b);
    }

    private static bool EqualShape_4686cbddb0e0dea7(DeclarationRegistrationOptions? a, DeclarationRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_64b3f4521813f6e3(a, b);
    }

    private static bool EqualShape_b68f902d67c631dd(DefinitionClientCapabilities? a, DefinitionClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d782be7c64b733ad(a, b);
    }

    private static bool EqualShape_c28e132512d210cf(DefinitionOptions? a, DefinitionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9faf6afba7757762(a, b);
    }

    private static bool EqualShape_d0d15be04a4f4f53(DefinitionParams? a, DefinitionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2c8b11f5c8545467(a, b);
    }

    private static bool EqualShape_e78a189dd9ae441b(DefinitionRegistrationOptions? a, DefinitionRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_477146b0718707a3(a, b);
    }

    private static bool EqualShape_3bcc5a96460b21ba(DeleteFile? a, DeleteFile? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bbfcf281d1c9c237(a, b);
    }

    private static bool EqualShape_7593b2981911d93d(DeleteFileOptions? a, DeleteFileOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1f8e9018d646804f(a, b);
    }

    private static bool EqualShape_a0071b6bdc2fa622(DeleteFilesParams? a, DeleteFilesParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_54b8495600617e2e(a, b);
    }

    private static bool EqualShape_cfb0101e50941834(Diagnostic? a, Diagnostic? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b1fa878a98a15b74(a, b);
    }

    private static bool EqualShape_2f065e8091229dc4(DiagnosticClientCapabilities? a, DiagnosticClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_acc80cae1a8d44b9(a, b);
    }

    private static bool EqualShape_4cd06a52127e4782(DiagnosticData? a, DiagnosticData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_29ddb33ca2a0e609(a.Value, b.Value);
    }

    private static bool EqualShape_068ed04f8cfbd578(DiagnosticFlakeLogLevel? a, DiagnosticFlakeLogLevel? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1ca005e11eb3df3a(a.Value, b.Value);
    }

    private static bool EqualShape_55e07a8cec82c655(DiagnosticOptions? a, DiagnosticOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7978f199a414fbd(a, b);
    }

    private static bool EqualShape_97e699b77b112a1d(DiagnosticOptionsOrRegistrationOptions? a, DiagnosticOptionsOrRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_703869dd83e88e30(a, b);
    }

    private static bool EqualShape_40ecbb6a23243047(DiagnosticRegistrationOptions? a, DiagnosticRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a14be8cd84f4acb8(a, b);
    }

    private static bool EqualShape_fd0503f33542b10c(DiagnosticRelatedInformation? a, DiagnosticRelatedInformation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_730df2a39dbfb7da(a, b);
    }

    private static bool EqualShape_cd9c63e32586179a(DiagnosticSeverity? a, DiagnosticSeverity? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bab4fe1f8b21f9fa(a.Value, b.Value);
    }

    private static bool EqualShape_10ef1d906f707e84(DiagnosticWorkspaceClientCapabilities? a, DiagnosticWorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_22994f812002fe3c(a, b);
    }

    private static bool EqualShape_db2a6c56987ef6af(DidChangeConfigurationClientCapabilities? a, DidChangeConfigurationClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7b4c2bd9272ffb37(a, b);
    }

    private static bool EqualShape_0826f1723afffabc(DidChangeConfigurationParams? a, DidChangeConfigurationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7f22677bab49bf26(a, b);
    }

    private static bool EqualShape_d9c278c49c9c5441(DidChangeConfigurationRegistrationOptions? a, DidChangeConfigurationRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_15c1f1ed9bdde479(a, b);
    }

    private static bool EqualShape_994a2745c3313728(DidChangeTextDocumentParams? a, DidChangeTextDocumentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0c6ffd2fc1fd5265(a, b);
    }

    private static bool EqualShape_42258ccaf2d2888f(DidChangeWatchedFilesClientCapabilities? a, DidChangeWatchedFilesClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5b8e42fb55c703ff(a, b);
    }

    private static bool EqualShape_3b6f1bec206a0f98(DidChangeWatchedFilesParams? a, DidChangeWatchedFilesParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7a687231fe680441(a, b);
    }

    private static bool EqualShape_e85dcfbccb9fd36f(DidChangeWatchedFilesRegistrationOptions? a, DidChangeWatchedFilesRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9a8637149e2b0c93(a, b);
    }

    private static bool EqualShape_254c9d197c2aeb11(DidChangeWorkspaceFoldersParams? a, DidChangeWorkspaceFoldersParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3814f51204c3cfe2(a, b);
    }

    private static bool EqualShape_e0e1198d80557e7a(DidCloseTextDocumentParams? a, DidCloseTextDocumentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a93e90edfeeab584(a, b);
    }

    private static bool EqualShape_2c6d4e0f9bb39d8f(DidOpenTextDocumentParams? a, DidOpenTextDocumentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e938ee27d893dd4c(a, b);
    }

    private static bool EqualShape_e9e03a8597015240(DidSaveTextDocumentParams? a, DidSaveTextDocumentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_30e8b3b731a2add1(a, b);
    }

    private static bool EqualShape_a1b5026abac1822b(DocumentColorClientCapabilities? a, DocumentColorClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_eb60b030a70a7451(a, b);
    }

    private static bool EqualShape_ddcb3de072c3c453(DocumentColorOptions? a, DocumentColorOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_522a26e4dbe6345c(a, b);
    }

    private static bool EqualShape_5ddc46f5e5594518(DocumentColorParams? a, DocumentColorParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cbb3073d36672b63(a, b);
    }

    private static bool EqualShape_e69c3a42ec51132a(DocumentColorRegistrationOptions? a, DocumentColorRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2588d25e0144d45(a, b);
    }

    private static bool EqualShape_cf50e9dd099d3850(DocumentDiagnosticParams? a, DocumentDiagnosticParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7548490d28d56df8(a, b);
    }

    private static bool EqualShape_b549a20e1eaf9467(DocumentFormattingClientCapabilities? a, DocumentFormattingClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cce2bbd91d77be98(a, b);
    }

    private static bool EqualShape_41b8cb3798a0eaa3(DocumentFormattingOptions? a, DocumentFormattingOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e984db9d3215a45f(a, b);
    }

    private static bool EqualShape_90229c68dfa4c029(DocumentFormattingParams? a, DocumentFormattingParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d3c0b448bcf8e371(a, b);
    }

    private static bool EqualShape_d75a7439bcbd1f29(DocumentFormattingRegistrationOptions? a, DocumentFormattingRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_62ee47a157b066a5(a, b);
    }

    private static bool EqualShape_23150a029e73ef27(DocumentHighlight? a, DocumentHighlight? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_186bd9e070f0cc76(a, b);
    }

    private static bool EqualShape_981464bac8d2e488(DocumentHighlightClientCapabilities? a, DocumentHighlightClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3ea7538c7c7b4ca6(a, b);
    }

    private static bool EqualShape_891c53554db94d15(DocumentHighlightKind? a, DocumentHighlightKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5f2a94526ab2d089(a.Value, b.Value);
    }

    private static bool EqualShape_2e7180cdcd7328e7(DocumentHighlightOptions? a, DocumentHighlightOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5fa893e750b616b3(a, b);
    }

    private static bool EqualShape_5629080604890233(DocumentHighlightParams? a, DocumentHighlightParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0123e4aa1b008823(a, b);
    }

    private static bool EqualShape_2d1576d4995f0019(DocumentHighlightRegistrationOptions? a, DocumentHighlightRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0c38eeec102fae46(a, b);
    }

    private static bool EqualShape_1340a620b0fd7d81(DocumentLink? a, DocumentLink? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_25f2aaad3d4056b4(a, b);
    }

    private static bool EqualShape_5c0964568f60471c(DocumentLinkClientCapabilities? a, DocumentLinkClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e06be71a80ee1ec3(a, b);
    }

    private static bool EqualShape_c8bba042661f66cd(DocumentLinkData? a, DocumentLinkData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4c9581f87cdeb817(a.Value, b.Value);
    }

    private static bool EqualShape_17440916585efd87(DocumentLinkOptions? a, DocumentLinkOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_529f3bd836f0b9d8(a, b);
    }

    private static bool EqualShape_aef4df72ade4f590(DocumentLinkParams? a, DocumentLinkParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b94d0f031a384478(a, b);
    }

    private static bool EqualShape_552d6ebc223a0b59(DocumentLinkRegistrationOptions? a, DocumentLinkRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_92723a302d3136eb(a, b);
    }

    private static bool EqualShape_f2551d19a058aa31(DocumentOnTypeFormattingClientCapabilities? a, DocumentOnTypeFormattingClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9ba4d6b0690b3f7f(a, b);
    }

    private static bool EqualShape_d1f8e0711d3ee76b(DocumentOnTypeFormattingOptions? a, DocumentOnTypeFormattingOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0fa03db0935ecc4d(a, b);
    }

    private static bool EqualShape_309c04822b884703(DocumentOnTypeFormattingParams? a, DocumentOnTypeFormattingParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_973929f55766dcd7(a, b);
    }

    private static bool EqualShape_1273c6a7488dec0a(DocumentOnTypeFormattingRegistrationOptions? a, DocumentOnTypeFormattingRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e4d7072185bf69d(a, b);
    }

    private static bool EqualShape_675b278ef4023caa(DocumentRangeFormattingClientCapabilities? a, DocumentRangeFormattingClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_da985f3aad64b092(a, b);
    }

    private static bool EqualShape_50b39195f03a0636(DocumentRangeFormattingOptions? a, DocumentRangeFormattingOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ff263dfb0755ad33(a, b);
    }

    private static bool EqualShape_525ad2c6bdd43e11(DocumentRangeFormattingParams? a, DocumentRangeFormattingParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d6bb17f42468c458(a, b);
    }

    private static bool EqualShape_c4b8e34a248dcb66(DocumentRangeFormattingRegistrationOptions? a, DocumentRangeFormattingRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e4397d5196cfce64(a, b);
    }

    private static bool EqualShape_9a1f9355b8a6b347(DocumentRangesFormattingParams? a, DocumentRangesFormattingParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ebefe18aa77c282b(a, b);
    }

    private static bool EqualShape_6146aa6b9c506765(DocumentSymbol? a, DocumentSymbol? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7a5e3c27bb3a5284(a, b);
    }

    private static bool EqualShape_a8886634269f294b(DocumentSymbolClientCapabilities? a, DocumentSymbolClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7ca8061eaafe80fa(a, b);
    }

    private static bool EqualShape_41bb397a975775f8(DocumentSymbolOptions? a, DocumentSymbolOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a1b6a2290a294048(a, b);
    }

    private static bool EqualShape_b0185682eefde6f5(DocumentSymbolParams? a, DocumentSymbolParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2be3ae8722c9c7c4(a, b);
    }

    private static bool EqualShape_3be177f910d82eb3(DocumentSymbolRegistrationOptions? a, DocumentSymbolRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_40bf243208068748(a, b);
    }

    private static bool EqualShape_ab86d3ea50f3859e(DocumentUri? a, DocumentUri? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Value, b.Value);
    }

    private static bool EqualShape_0f34605711837034(EditRangeWithInsertReplace? a, EditRangeWithInsertReplace? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_909237c23c611f07(a, b);
    }

    private static bool EqualShape_731174f183c7f076(ExecuteCommandClientCapabilities? a, ExecuteCommandClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6d21070f992c17fb(a, b);
    }

    private static bool EqualShape_6c0ea6c32425b0e0(ExecuteCommandOptions? a, ExecuteCommandOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_03d1076067933682(a, b);
    }

    private static bool EqualShape_05e58a3099cb42a6(ExecuteCommandParams? a, ExecuteCommandParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_167e0d23d2d2a94b(a, b);
    }

    private static bool EqualShape_5dc7279ef53555bc(ExecuteCommandRegistrationOptions? a, ExecuteCommandRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e5b0616c18b5df9a(a, b);
    }

    private static bool EqualShape_fcda8a980d4c687d(ExperimentalClientCapabilities? a, ExperimentalClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_450a644bbefb758d(a, b);
    }

    private static bool EqualShape_2f77452b23ae1df3(ExperimentalServerCapabilities? a, ExperimentalServerCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8328a3a42170a833(a, b);
    }

    private static bool EqualShape_3599cdd072d7151a(FailureHandlingKind? a, FailureHandlingKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_484e4404d3769d00(a.Value, b.Value);
    }

    private static bool EqualShape_f99255a19aeb440c(FileCreate? a, FileCreate? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dd7287de677959f4(a, b);
    }

    private static bool EqualShape_6d7945c3a568dcd5(FileDelete? a, FileDelete? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4cfd293b1972bff8(a, b);
    }

    private static bool EqualShape_47f24aa2d05bbb16(FileEvent? a, FileEvent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a125c27075ddc783(a, b);
    }

    private static bool EqualShape_97571a077e94445f(FileOperationClientCapabilities? a, FileOperationClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9de47d600efc33f1(a, b);
    }

    private static bool EqualShape_4f9a8694ec35690b(FileOperationFilter? a, FileOperationFilter? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_01085faf2ca32a63(a, b);
    }

    private static bool EqualShape_05252cee26410791(FileOperationOptions? a, FileOperationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_518c29b60657172f(a, b);
    }

    private static bool EqualShape_d914c67f0dffd8de(FileOperationPattern? a, FileOperationPattern? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_876111df355462c4(a, b);
    }

    private static bool EqualShape_6b2ece5c46641318(FileOperationPatternKind? a, FileOperationPatternKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c9ac6b08cd0fcaec(a.Value, b.Value);
    }

    private static bool EqualShape_bfdb692544efd922(FileOperationPatternOptions? a, FileOperationPatternOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_77897374dc003098(a, b);
    }

    private static bool EqualShape_95c7ba3a24076324(FileOperationRegistrationOptions? a, FileOperationRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1cbbf99920573dca(a, b);
    }

    private static bool EqualShape_0a97aeac6dfe4cd0(FileRename? a, FileRename? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_132f8fe961259b8b(a, b);
    }

    private static bool EqualShape_243be7733f7be22a(FileSystemWatcher? a, FileSystemWatcher? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8546807fc7830176(a, b);
    }

    private static bool EqualShape_c1f115ff19a883a0(FoldingRange? a, FoldingRange? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_66fbdf0fd1929f3b(a, b);
    }

    private static bool EqualShape_ab939e84b0c28daf(FoldingRangeClientCapabilities? a, FoldingRangeClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_016dd67866f93e94(a, b);
    }

    private static bool EqualShape_5b82af5067892e83(FoldingRangeKind? a, FoldingRangeKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9bed5894ab8b426e(a.Value, b.Value);
    }

    private static bool EqualShape_4bb278676873c924(FoldingRangeOptions? a, FoldingRangeOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_563033aa92b4afe3(a, b);
    }

    private static bool EqualShape_c5de05b6d55fce56(FoldingRangeParams? a, FoldingRangeParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e4340b98efb17abe(a, b);
    }

    private static bool EqualShape_2248c420e67b79a4(FoldingRangeRegistrationOptions? a, FoldingRangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2465fd48699a0885(a, b);
    }

    private static bool EqualShape_4d913eb07665bbff(FoldingRangeWorkspaceClientCapabilities? a, FoldingRangeWorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_fb7ea6a0b64b120e(a, b);
    }

    private static bool EqualShape_3755655439c8ef4b(FormattingOptions? a, FormattingOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4441aa46c6968d1e(a, b);
    }

    private static bool EqualShape_51d09426b5861162(FullDocumentDiagnosticReport? a, FullDocumentDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7a31e4ad68de1c48(a, b);
    }

    private static bool EqualShape_b0c64514ae89504b(GeneralClientCapabilities? a, GeneralClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_337bdf3fdd9371a3(a, b);
    }

    private static bool EqualShape_d74932d04207d5c2(Hover? a, Hover? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c76ae86ad3014c85(a, b);
    }

    private static bool EqualShape_5d1960c178ace368(HoverClientCapabilities? a, HoverClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3adf798fbc806c6(a, b);
    }

    private static bool EqualShape_bd657437c0767aea(HoverOptions? a, HoverOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5e5dc1cc6e1c3523(a, b);
    }

    private static bool EqualShape_a2005eb48646fc88(HoverParams? a, HoverParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c1b679afaeb1b5ec(a, b);
    }

    private static bool EqualShape_844a68e7d6788338(HoverRegistrationOptions? a, HoverRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c42e31a1d8d0c570(a, b);
    }

    private static bool EqualShape_b4cf53ddc6d51528(ImplementationClientCapabilities? a, ImplementationClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_505ca50db8c7eb85(a, b);
    }

    private static bool EqualShape_ecb8c575e0dac043(ImplementationOptions? a, ImplementationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6a59e8e8c3547e2d(a, b);
    }

    private static bool EqualShape_5e7964c0382565a1(ImplementationParams? a, ImplementationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3126943fdda20947(a, b);
    }

    private static bool EqualShape_a1158e8b376f8c3c(ImplementationRegistrationOptions? a, ImplementationRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_999302bf98018b4a(a, b);
    }

    private static bool EqualShape_c90d452ef07656ad(InferredProjectContentMapperContribution? a, InferredProjectContentMapperContribution? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_44f48a6fd5281753(a, b);
    }

    private static bool EqualShape_a5466bf851beb93d(InitializationOptions? a, InitializationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d6291485da709fea(a, b);
    }

    private static bool EqualShape_6ebb34a7d02f2ddb(InitializationOptionsOrNull? a, InitializationOptionsOrNull? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_36ae92715ecba9c7(a, b);
    }

    private static bool EqualShape_10995e0a07c05c14(InitializeAPISessionParams? a, InitializeAPISessionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0f19f7201ba374f8(a, b);
    }

    private static bool EqualShape_956a57c39237f60e(InitializeAPISessionResult? a, InitializeAPISessionResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d5842f43f86d28d8(a, b);
    }

    private static bool EqualShape_8925ca3b9b3925a3(InitializeParams? a, InitializeParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6cabbf134c6b5195(a, b);
    }

    private static bool EqualShape_72b54be9523832a8(InitializeResult? a, InitializeResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ff2c54bb0124e1e5(a, b);
    }

    private static bool EqualShape_ec8eef9b1eea20a2(InitializedParams? a, InitializedParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_77f425e16948cb7b(a.Value, b.Value);
    }

    private static bool EqualShape_07b46c42ab1a924b(InlayHint? a, InlayHint? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_551be06dac20fdd0(a, b);
    }

    private static bool EqualShape_951aeb500d7eef88(InlayHintClientCapabilities? a, InlayHintClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_860bcfccc3d275ce(a, b);
    }

    private static bool EqualShape_e1dfa6c327dd6c3c(InlayHintData? a, InlayHintData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ec25b7a7c027131f(a.Value, b.Value);
    }

    private static bool EqualShape_07d32d25cf0ad06f(InlayHintKind? a, InlayHintKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_53926c928508e1cb(a.Value, b.Value);
    }

    private static bool EqualShape_e114c64cb251e202(InlayHintLabelPart? a, InlayHintLabelPart? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_83ad5d75a7336f5c(a, b);
    }

    private static bool EqualShape_248b47c8dc0cacef(InlayHintOptions? a, InlayHintOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e7413e6883917923(a, b);
    }

    private static bool EqualShape_6b56554844cacafb(InlayHintParams? a, InlayHintParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e20fc5d07945082(a, b);
    }

    private static bool EqualShape_85717bf7a3915adc(InlayHintRegistrationOptions? a, InlayHintRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5736a71dddfa55b8(a, b);
    }

    private static bool EqualShape_764eade21b3490a5(InlayHintWorkspaceClientCapabilities? a, InlayHintWorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_48084566f2cdcaae(a, b);
    }

    private static bool EqualShape_a9b342577c06553e(InlineCompletionClientCapabilities? a, InlineCompletionClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e7de3ce110b1d167(a, b);
    }

    private static bool EqualShape_12114617eb6b57ab(InlineCompletionContext? a, InlineCompletionContext? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_22637a4522270110(a, b);
    }

    private static bool EqualShape_3c57f532ec15eb7a(InlineCompletionItem? a, InlineCompletionItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5f63db4527f32b9c(a, b);
    }

    private static bool EqualShape_e7db5a8938276dbb(InlineCompletionList? a, InlineCompletionList? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_433b2e6a3a3c27cc(a, b);
    }

    private static bool EqualShape_e28e3677bae697ca(InlineCompletionOptions? a, InlineCompletionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_571267fe163915c1(a, b);
    }

    private static bool EqualShape_e0b034e192ebb19b(InlineCompletionParams? a, InlineCompletionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d9c2c29e86a56320(a, b);
    }

    private static bool EqualShape_4d7c5054db5abf5f(InlineCompletionRegistrationOptions? a, InlineCompletionRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e1b6c72c88dbb66d(a, b);
    }

    private static bool EqualShape_047caebfe4129ed3(InlineValueClientCapabilities? a, InlineValueClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9422bd44de4450a3(a, b);
    }

    private static bool EqualShape_cf67b383aa7395a7(InlineValueContext? a, InlineValueContext? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0db2a1b74408177b(a, b);
    }

    private static bool EqualShape_a4ea07c6da53809d(InlineValueEvaluatableExpression? a, InlineValueEvaluatableExpression? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5ba15e00f3989e84(a, b);
    }

    private static bool EqualShape_f59f02a865f30930(InlineValueOptions? a, InlineValueOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4227afe828e4c487(a, b);
    }

    private static bool EqualShape_401f634648fb9bcf(InlineValueParams? a, InlineValueParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c9023b2dc10087cf(a, b);
    }

    private static bool EqualShape_6740dd541d21b3a4(InlineValueRegistrationOptions? a, InlineValueRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5852b2dfb2cf7003(a, b);
    }

    private static bool EqualShape_9d9ba902d757f771(InlineValueText? a, InlineValueText? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e1fd84adea44b647(a, b);
    }

    private static bool EqualShape_7a0d9411f2d63a63(InlineValueVariableLookup? a, InlineValueVariableLookup? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2dc5fda12fce3a9d(a, b);
    }

    private static bool EqualShape_63f3d68cbd8bfa6d(InlineValueWorkspaceClientCapabilities? a, InlineValueWorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_caa283e237c108c2(a, b);
    }

    private static bool EqualShape_9795643b269bbb7c(InsertReplaceEdit? a, InsertReplaceEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_79b7a3fa81c6612e(a, b);
    }

    private static bool EqualShape_7c5ad57d1d134af0(InsertTextFormat? a, InsertTextFormat? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0125b1afb99b0757(a.Value, b.Value);
    }

    private static bool EqualShape_7edf63c19953acbf(InsertTextMode? a, InsertTextMode? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6cddb8f12d636f30(a.Value, b.Value);
    }

    private static bool EqualShape_c3d8a0302fb53f84(IntegerOrString? a, IntegerOrString? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95e25e81cd99bf99(a, b);
    }

    private static bool EqualShape_b10082da90551716(LinkedEditingRangeClientCapabilities? a, LinkedEditingRangeClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1d4bf231f870ced7(a, b);
    }

    private static bool EqualShape_f059f04438094981(LinkedEditingRangeOptions? a, LinkedEditingRangeOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bf2919c7ffa7243d(a, b);
    }

    private static bool EqualShape_e7708efe661b82c1(LinkedEditingRangeParams? a, LinkedEditingRangeParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7aa30078fed4b566(a, b);
    }

    private static bool EqualShape_bc219c8cf4519a73(LinkedEditingRangeRegistrationOptions? a, LinkedEditingRangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_89439e1c9f8204ac(a, b);
    }

    private static bool EqualShape_d089671828ebe059(LinkedEditingRanges? a, LinkedEditingRanges? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_42b657f1a5deeca0(a, b);
    }

    private static bool EqualShape_f9ef6e0f3fa4927b(Location? a, Location? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_15b61974b2707a7b(a, b);
    }

    private static bool EqualShape_1e631912e1fd4f05(LocationLink? a, LocationLink? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bdb924567ef98797(a, b);
    }

    private static bool EqualShape_ed98b7171408f86c(LocationOrLocationsOrDefinitionLinksOrNull? a, LocationOrLocationsOrDefinitionLinksOrNull? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e65bade6fa339317(a, b);
    }

    private static bool EqualShape_6fd44c23af14c27f(LocationUriOnly? a, LocationUriOnly? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3bee336672c6633c(a, b);
    }

    private static bool EqualShape_d0b0036fc9f1e834(LogMessageParams? a, LogMessageParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8e5a686c4d7af637(a, b);
    }

    private static bool EqualShape_6f989b5d35150c92(LogTraceParams? a, LogTraceParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_561978800a186b54(a, b);
    }

    private static bool EqualShape_1f5a1b49e624f6ed(LogVerbosity? a, LogVerbosity? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_061133d05f438d15(a.Value, b.Value);
    }

    private static bool EqualShape_cda55cf0c5a75507(MarkdownClientCapabilities? a, MarkdownClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5754eb1e7d7a8a87(a, b);
    }

    private static bool EqualShape_c3b0d76093e2f53f(MarkedStringWithLanguage? a, MarkedStringWithLanguage? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_02b572e84de66f2a(a, b);
    }

    private static bool EqualShape_a09c50c289cdf175(MarkupContent? a, MarkupContent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ec8d99ae989825b7(a, b);
    }

    private static bool EqualShape_505239fc3470cbd5(MessageActionItem? a, MessageActionItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0627c9b9dd315342(a, b);
    }

    private static bool EqualShape_16a1534e4208ae3b(Moniker? a, Moniker? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c59fb88a060d1bc0(a, b);
    }

    private static bool EqualShape_e1cd5e0f225c96c6(MonikerClientCapabilities? a, MonikerClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6294802e598ca5a1(a, b);
    }

    private static bool EqualShape_01469ed2e83bad7e(MonikerKind? a, MonikerKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7d2dfd2063c3273d(a.Value, b.Value);
    }

    private static bool EqualShape_da81b08167dd97fa(MonikerOptions? a, MonikerOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6a54ddd49258de2c(a, b);
    }

    private static bool EqualShape_8a8053fac823678a(MonikerParams? a, MonikerParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dce655840bd2b665(a, b);
    }

    private static bool EqualShape_f75de590883ed072(MonikerRegistrationOptions? a, MonikerRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_01d385a3539e6586(a, b);
    }

    private static bool EqualShape_d778570f7ada035b(MultiDocumentHighlight? a, MultiDocumentHighlight? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_01d7598b5a7fa2ba(a, b);
    }

    private static bool EqualShape_07b5af1a25270b84(MultiDocumentHighlightParams? a, MultiDocumentHighlightParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d34dfd9b81abfe93(a, b);
    }

    private static bool EqualShape_445e57e999b361cf(ParameterInformation? a, ParameterInformation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6e5531cdb6cb8530(a, b);
    }

    private static bool EqualShape_f683870be53950f3(PatternOrRelativePattern? a, PatternOrRelativePattern? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_555c0b23fa28c7a4(a, b);
    }

    private static bool EqualShape_0af74c1c29f6331d(PerformanceStatsTelemetryEvent? a, PerformanceStatsTelemetryEvent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4ba91157fee437e2(a, b);
    }

    private static bool EqualShape_6ccd8518896c2c38(PerformanceStatsTelemetryMeasurements? a, PerformanceStatsTelemetryMeasurements? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_eca5393ae77d3617(a, b);
    }

    private static bool EqualShape_020329ac5748a864(Position? a, Position? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6d031af10da7a25e(a.Value, b.Value);
    }

    private static bool EqualShape_d99a5f4bdbe98370(PositionEncodingKind? a, PositionEncodingKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_996a119923a77207(a.Value, b.Value);
    }

    private static bool EqualShape_1afe8f9cb73e43ee(PrepareRenameDefaultBehavior? a, PrepareRenameDefaultBehavior? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0939d43961a41cae(a, b);
    }

    private static bool EqualShape_0b32d999dee51635(PrepareRenameParams? a, PrepareRenameParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_21a0c71ff605bf6f(a, b);
    }

    private static bool EqualShape_45ce497aa8b53194(PrepareRenamePlaceholder? a, PrepareRenamePlaceholder? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a423b1bf4090c576(a, b);
    }

    private static bool EqualShape_4f120ecccd865abb(PrepareSupportDefaultBehavior? a, PrepareSupportDefaultBehavior? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_491ada003a0c8630(a.Value, b.Value);
    }

    private static bool EqualShape_7913ef99f1d77dae(ProfileParams? a, ProfileParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_643784fd09a0a166(a, b);
    }

    private static bool EqualShape_baa03deba3a4bfe8(ProfileResult? a, ProfileResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ea1fbfb94ccf57aa(a, b);
    }

    private static bool EqualShape_3561a401fccf8947(ProgressParams? a, ProgressParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_26b35649303908fb(a, b);
    }

    private static bool EqualShape_eb844afb71dd21c8(ProjectInfoParams? a, ProjectInfoParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_82dae73cd3c77e28(a, b);
    }

    private static bool EqualShape_d25ba9061423cf4d(ProjectInfoResult? a, ProjectInfoResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1a1c57207754153f(a, b);
    }

    private static bool EqualShape_f64057052b72427f(ProjectInfoTelemetryEvent? a, ProjectInfoTelemetryEvent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f3af5d7035530534(a, b);
    }

    private static bool EqualShape_9107abb7a369fc01(ProjectInfoTelemetryMeasurements? a, ProjectInfoTelemetryMeasurements? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d4d842b2a58207a2(a, b);
    }

    private static bool EqualShape_8269ed450df87f35(PublishDiagnosticsClientCapabilities? a, PublishDiagnosticsClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6fa38bb9a3563ce0(a, b);
    }

    private static bool EqualShape_37c198e5300a71f8(PublishDiagnosticsParams? a, PublishDiagnosticsParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a870657479cb5aa7(a, b);
    }

    private static bool EqualShape_c15fa81b51c6dda0(Range? a, Range? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.Value, b.Value);
    }

    private static bool EqualShape_3c5dcfa086349e56(RangeOrEditRangeWithInsertReplace? a, RangeOrEditRangeWithInsertReplace? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_88865600aa610d9f(a, b);
    }

    private static bool EqualShape_dddb35c4f04b6359(ReferenceClientCapabilities? a, ReferenceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c08c1b89cb199dea(a, b);
    }

    private static bool EqualShape_736e0638b6dc86f2(ReferenceContext? a, ReferenceContext? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_49c578f22284c849(a, b);
    }

    private static bool EqualShape_01630610b86f2479(ReferenceOptions? a, ReferenceOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7972bdd81c162d72(a, b);
    }

    private static bool EqualShape_f26b7615545b5628(ReferenceParams? a, ReferenceParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d77f6d8f9dfa25d9(a, b);
    }

    private static bool EqualShape_455b65df496a7e4e(ReferenceRegistrationOptions? a, ReferenceRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_944d8fdf608f0ed6(a, b);
    }

    private static bool EqualShape_4779ed0bd2fc3188(RegisterOptions? a, RegisterOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_525c0e333a92bf78(a, b);
    }

    private static bool EqualShape_c48c035a6051381e(Registration? a, Registration? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c793e0d9a1cc6421(a, b);
    }

    private static bool EqualShape_8e6ae1c687bb81d3(RegistrationParams? a, RegistrationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1b5a140ba3379372(a, b);
    }

    private static bool EqualShape_c573dc5e5f151c1e(RegularExpressionsClientCapabilities? a, RegularExpressionsClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_81491243f7e99a45(a, b);
    }

    private static bool EqualShape_b44212a73089a572(RelatedFullDocumentDiagnosticReport? a, RelatedFullDocumentDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_821440dfb3fa7e3f(a, b);
    }

    private static bool EqualShape_6c7d4458419f8392(RelatedUnchangedDocumentDiagnosticReport? a, RelatedUnchangedDocumentDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_77534693e60adab3(a, b);
    }

    private static bool EqualShape_741900dcc54c0f9e(RelativePattern? a, RelativePattern? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_417e71cd37016ebf(a, b);
    }

    private static bool EqualShape_a91454fb98aa7a0a(RenameClientCapabilities? a, RenameClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0c6819e76b439377(a, b);
    }

    private static bool EqualShape_5e2ceb6a00e2a777(RenameFile? a, RenameFile? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3f987788dab9f8e0(a, b);
    }

    private static bool EqualShape_4c0950628e22bacd(RenameFileOptions? a, RenameFileOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4e2827e566b1dc06(a, b);
    }

    private static bool EqualShape_d469332edf23a98a(RenameFilesParams? a, RenameFilesParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2b49debd9d4868a5(a, b);
    }

    private static bool EqualShape_9118f2a9dd376545(RenameOptions? a, RenameOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dc010ea0160c6a6a(a, b);
    }

    private static bool EqualShape_af10a232c5a437d5(RenameParams? a, RenameParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7c73bd9e0a480ec1(a, b);
    }

    private static bool EqualShape_9a106112ad8519a4(RenameRegistrationOptions? a, RenameRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2a0823905e235731(a, b);
    }

    private static bool EqualShape_929549d024332472(RequestFailureTelemetryEvent? a, RequestFailureTelemetryEvent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_aa1fe6bec4869c52(a, b);
    }

    private static bool EqualShape_6fae2aa56eb29bf7(RequestFailureTelemetryProperties? a, RequestFailureTelemetryProperties? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6cfd8d46d0edce53(a, b);
    }

    private static bool EqualShape_b5e23cfa61bf82e5(SaveOptions? a, SaveOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0c860e4c1955b16b(a, b);
    }

    private static bool EqualShape_c896f039f28c0b09(SelectedCompletionInfo? a, SelectedCompletionInfo? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_422af1cce96213bc(a, b);
    }

    private static bool EqualShape_09de083f42750e75(SelectionRange? a, SelectionRange? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7d7b70e7ccd8dfec(a, b);
    }

    private static bool EqualShape_6ab85e723dea516d(SelectionRangeClientCapabilities? a, SelectionRangeClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a4e17fae82626379(a, b);
    }

    private static bool EqualShape_91343bb11414d053(SelectionRangeOptions? a, SelectionRangeOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_de0943b8402d64fb(a, b);
    }

    private static bool EqualShape_1fbac1913a5833ad(SelectionRangeParams? a, SelectionRangeParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0b27b0baa03194cf(a, b);
    }

    private static bool EqualShape_00c568d61d6e4506(SelectionRangeRegistrationOptions? a, SelectionRangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1299af8f490cf41a(a, b);
    }

    private static bool EqualShape_ebdc16536169d222(SemanticTokens? a, SemanticTokens? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_448a954da33d9419(a, b);
    }

    private static bool EqualShape_e25e34bcf60ac848(SemanticTokensClientCapabilities? a, SemanticTokensClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_22677e2a6998efa4(a, b);
    }

    private static bool EqualShape_3d6953018d2b3b79(SemanticTokensDelta? a, SemanticTokensDelta? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1c8ca776704f95d7(a, b);
    }

    private static bool EqualShape_6d6a60a56b13f4dd(SemanticTokensDeltaParams? a, SemanticTokensDeltaParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9101edacad945287(a, b);
    }

    private static bool EqualShape_6b0fc3a769e2d57c(SemanticTokensEdit? a, SemanticTokensEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5d1443274aea049d(a, b);
    }

    private static bool EqualShape_3e6981e05f891299(SemanticTokensFullDelta? a, SemanticTokensFullDelta? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_47409fff8234f1d6(a, b);
    }

    private static bool EqualShape_48c95bf06e1b64d4(SemanticTokensLegend? a, SemanticTokensLegend? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_508b7ba4b756581e(a, b);
    }

    private static bool EqualShape_811cdaf9e395c82f(SemanticTokensOptions? a, SemanticTokensOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e73c6a23ccc9fff4(a, b);
    }

    private static bool EqualShape_bab5a33b2118066f(SemanticTokensOptionsOrRegistrationOptions? a, SemanticTokensOptionsOrRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_45b9b7d638819fbf(a, b);
    }

    private static bool EqualShape_cc94aa36d2c0e092(SemanticTokensParams? a, SemanticTokensParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8bf9ad4f5cef6cfa(a, b);
    }

    private static bool EqualShape_c223343f04947da6(SemanticTokensRangeParams? a, SemanticTokensRangeParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0dc62352c8c25887(a, b);
    }

    private static bool EqualShape_fe696d3ff7732e89(SemanticTokensRegistrationOptions? a, SemanticTokensRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4b3cdaefc55e634e(a, b);
    }

    private static bool EqualShape_5a87b105e4bdfba4(SemanticTokensWorkspaceClientCapabilities? a, SemanticTokensWorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_fe40ff6064ff54f2(a, b);
    }

    private static bool EqualShape_a8f57d2338f06506(ServerCapabilities? a, ServerCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_628c62fa6a34c245(a, b);
    }

    private static bool EqualShape_66abf0144b9c6652(ServerCompletionItemOptions? a, ServerCompletionItemOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3b0c84c52fea24a8(a, b);
    }

    private static bool EqualShape_389fa1d2ea1e79ff(ServerInfo? a, ServerInfo? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4f0db94bc9f4a530(a, b);
    }

    private static bool EqualShape_1f528ac400b85442(SetContentMapperContributionsParams? a, SetContentMapperContributionsParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1171d758ea6161e3(a, b);
    }

    private static bool EqualShape_75e0d334d1b10a70(SetLogVerbosityParams? a, SetLogVerbosityParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b4447bd7705f87ab(a, b);
    }

    private static bool EqualShape_d8d9df472eb6dd2a(SetTraceParams? a, SetTraceParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_41823968658e5dfc(a, b);
    }

    private static bool EqualShape_fa630b9d5505b4e0(ShowDocumentClientCapabilities? a, ShowDocumentClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2303d8fc7f8ad8a3(a, b);
    }

    private static bool EqualShape_9362615b87821265(ShowDocumentParams? a, ShowDocumentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7e2bf70a83c07533(a, b);
    }

    private static bool EqualShape_2df516fbfab36711(ShowDocumentResult? a, ShowDocumentResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c44a39c887303450(a, b);
    }

    private static bool EqualShape_864b8799c6d733fb(ShowMessageParams? a, ShowMessageParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_663434b32ed8ff88(a, b);
    }

    private static bool EqualShape_84aaa7c2cc391af1(ShowMessageRequestClientCapabilities? a, ShowMessageRequestClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d3e4643133128cfd(a, b);
    }

    private static bool EqualShape_afd2f07de6bf412f(ShowMessageRequestParams? a, ShowMessageRequestParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_11bb59cd26639030(a, b);
    }

    private static bool EqualShape_db195c87aa8dbd39(SignatureHelp? a, SignatureHelp? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5a6c866a2e0d1dee(a, b);
    }

    private static bool EqualShape_8b8480807e012f80(SignatureHelpClientCapabilities? a, SignatureHelpClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ee58083fc9757534(a, b);
    }

    private static bool EqualShape_d1c7f85c1a40ed24(SignatureHelpContext? a, SignatureHelpContext? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1331ccfab2d32049(a, b);
    }

    private static bool EqualShape_cc58706809ce50fc(SignatureHelpOptions? a, SignatureHelpOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f179f1d094fa3f6c(a, b);
    }

    private static bool EqualShape_b91c410f715c83d5(SignatureHelpParams? a, SignatureHelpParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_775120ff277a7df5(a, b);
    }

    private static bool EqualShape_ba6554e7a6e6220a(SignatureHelpRegistrationOptions? a, SignatureHelpRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cd22913bfb024c92(a, b);
    }

    private static bool EqualShape_090b50bb170c7d95(SignatureInformation? a, SignatureInformation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_281bb89ea78ef9ef(a, b);
    }

    private static bool EqualShape_56e45e4f5ddf42a4(SnippetTextEdit? a, SnippetTextEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_108d1bfa63a85c92(a, b);
    }

    private static bool EqualShape_d7072dfcd6925eea(StaleRequestSupportOptions? a, StaleRequestSupportOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_16b0a700ab1e5e92(a, b);
    }

    private static bool EqualShape_958f7a38d91645d4(StringOrBoolean? a, StringOrBoolean? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4da8fc01584b07f2(a, b);
    }

    private static bool EqualShape_818def5a213356d9(StringOrMarkupContent? a, StringOrMarkupContent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31e9efe8f25ec13e(a, b);
    }

    private static bool EqualShape_e5ac9928546536d2(StringOrNull? a, StringOrNull? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5ada2186dac127d6(a, b);
    }

    private static bool EqualShape_da890d6dcb4d2c8c(StringOrStrings? a, StringOrStrings? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_23dbff1cf97c4515(a, b);
    }

    private static bool EqualShape_2160d538d577aae5(StringValue? a, StringValue? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_24b828daee6b73d3(a, b);
    }

    private static bool EqualShape_7de106dad6f98cbc(SymbolInformation? a, SymbolInformation? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7a1b84d8b2015bb3(a, b);
    }

    private static bool EqualShape_73d416e55ae7bb8e(TextDocumentChangeRegistrationOptions? a, TextDocumentChangeRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_60522a76f247ede4(a, b);
    }

    private static bool EqualShape_9d8ecb04ded0e67b(TextDocumentClientCapabilities? a, TextDocumentClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ccc9d5a581c31fa6(a, b);
    }

    private static bool EqualShape_8d61f6c1466e1d79(TextDocumentContentChangePartial? a, TextDocumentContentChangePartial? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d1c2c4faf51c15cd(a, b);
    }

    private static bool EqualShape_2d082a6bd3807513(TextDocumentContentChangeWholeDocument? a, TextDocumentContentChangeWholeDocument? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a785b2bcdef39e09(a, b);
    }

    private static bool EqualShape_6adc23222769dd63(TextDocumentContentClientCapabilities? a, TextDocumentContentClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8525cd90fd0e2c39(a, b);
    }

    private static bool EqualShape_1cae8ae1a7c570ea(TextDocumentContentOptions? a, TextDocumentContentOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_13118cef8ccea9e5(a, b);
    }

    private static bool EqualShape_4fed6769aec47ce0(TextDocumentContentOptionsOrRegistrationOptions? a, TextDocumentContentOptionsOrRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_12d4c2faae1da2c4(a, b);
    }

    private static bool EqualShape_c92bb2f96cdf04f7(TextDocumentContentParams? a, TextDocumentContentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c01437ea23dbe5fe(a, b);
    }

    private static bool EqualShape_09a0b49c233d321e(TextDocumentContentRefreshParams? a, TextDocumentContentRefreshParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_fb320f99142648f1(a, b);
    }

    private static bool EqualShape_599369b639c1cd8a(TextDocumentContentRegistrationOptions? a, TextDocumentContentRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7f1347f4bff47aed(a, b);
    }

    private static bool EqualShape_129c385f03cff56d(TextDocumentContentResult? a, TextDocumentContentResult? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6aee4fb4cbde55f1(a, b);
    }

    private static bool EqualShape_7f5935c52d83c56e(TextDocumentEdit? a, TextDocumentEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e35cf3d963f0d31d(a, b);
    }

    private static bool EqualShape_c0722562aa9bb9ff(TextDocumentFilterClientCapabilities? a, TextDocumentFilterClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9988519c9dffc485(a, b);
    }

    private static bool EqualShape_5e448967b43d5c68(TextDocumentFilterLanguage? a, TextDocumentFilterLanguage? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e4dfacba18e99d73(a, b);
    }

    private static bool EqualShape_579d16403267872b(TextDocumentFilterPattern? a, TextDocumentFilterPattern? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_29eec48c8b924f26(a, b);
    }

    private static bool EqualShape_4ad3fb09cee37081(TextDocumentFilterScheme? a, TextDocumentFilterScheme? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ed1e0538451840ef(a, b);
    }

    private static bool EqualShape_580176b81ce49f09(TextDocumentIdentifier? a, TextDocumentIdentifier? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a, b);
    }

    private static bool EqualShape_0f9333e138040d4c(TextDocumentItem? a, TextDocumentItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bfe80aa9873109a2(a, b);
    }

    private static bool EqualShape_3fd07e3bff08abd4(TextDocumentPositionParams? a, TextDocumentPositionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3166bd6fc1b8b11c(a, b);
    }

    private static bool EqualShape_fe6155021c1ea02b(TextDocumentRegistrationOptions? a, TextDocumentRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dde1e86fd065f07c(a, b);
    }

    private static bool EqualShape_114d5ab6eeb4591b(TextDocumentSaveRegistrationOptions? a, TextDocumentSaveRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_87e5fd44f56f3ab9(a, b);
    }

    private static bool EqualShape_099f782a33e1ec37(TextDocumentSyncClientCapabilities? a, TextDocumentSyncClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_54b3525db15e3a53(a, b);
    }

    private static bool EqualShape_b058cd7c54da6335(TextDocumentSyncKind? a, TextDocumentSyncKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1259c5dfc44b321e(a.Value, b.Value);
    }

    private static bool EqualShape_d1d1ccd1566ac47d(TextDocumentSyncOptions? a, TextDocumentSyncOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f2c5e9259b0b9679(a, b);
    }

    private static bool EqualShape_01c0f78b125f5830(TextDocumentSyncOptionsOrKind? a, TextDocumentSyncOptionsOrKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c80d765f99706699(a, b);
    }

    private static bool EqualShape_56642992931a7c98(TextEdit? a, TextEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_907ab9937a53fb02(a, b);
    }

    private static bool EqualShape_d401c45b8c71281c(TextEditOrInsertReplaceEdit? a, TextEditOrInsertReplaceEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b3a71121264d6d6a(a, b);
    }

    private static bool EqualShape_1be8e25859d6d163(TraceValue? a, TraceValue? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_98b926ad367a3bcb(a.Value, b.Value);
    }

    private static bool EqualShape_d9566f6cdf1ce51e(TypeDefinitionClientCapabilities? a, TypeDefinitionClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d90d2c9d9d18720a(a, b);
    }

    private static bool EqualShape_13996fbd8ef62326(TypeDefinitionOptions? a, TypeDefinitionOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_edef749fe5e8e612(a, b);
    }

    private static bool EqualShape_af34cd95c9556cfa(TypeDefinitionParams? a, TypeDefinitionParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_60ed4f843d0b5556(a, b);
    }

    private static bool EqualShape_dc874b303baa6dbb(TypeDefinitionRegistrationOptions? a, TypeDefinitionRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_56741666965b99ed(a, b);
    }

    private static bool EqualShape_e10d80c319e7db76(TypeHierarchyClientCapabilities? a, TypeHierarchyClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2fbaa79cbc8e5da5(a, b);
    }

    private static bool EqualShape_daf7e9c8b5b7a1fe(TypeHierarchyItem? a, TypeHierarchyItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f1de4bc0a7efe042(a, b);
    }

    private static bool EqualShape_93dd60b3c4ff5c3f(TypeHierarchyItemData? a, TypeHierarchyItemData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f33a5bc24ebcc136(a.Value, b.Value);
    }

    private static bool EqualShape_5a2b4afa8c993da1(TypeHierarchyOptions? a, TypeHierarchyOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_af73cf291a3bbe3e(a, b);
    }

    private static bool EqualShape_c30ad85359eea5cf(TypeHierarchyPrepareParams? a, TypeHierarchyPrepareParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8ed2c1f6365885f3(a, b);
    }

    private static bool EqualShape_fd11d6b574dafc11(TypeHierarchyRegistrationOptions? a, TypeHierarchyRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ead4f86af75af483(a, b);
    }

    private static bool EqualShape_ff44bd988171cc0c(TypeHierarchySubtypesParams? a, TypeHierarchySubtypesParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f8c90245f3414581(a, b);
    }

    private static bool EqualShape_2cebac8fd236d553(TypeHierarchySupertypesParams? a, TypeHierarchySupertypesParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_178bc5d7aa446df7(a, b);
    }

    private static bool EqualShape_5b3003eb17f67429(URI? a, URI? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d70fbad88d0f9eaf(a.Value, b.Value);
    }

    private static bool EqualShape_7bb64244ba2ae5a2(UintegerOrNull? a, UintegerOrNull? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_905a1da3c49bbf1b(a, b);
    }

    private static bool EqualShape_3af2e2ea9ab7472d(UnchangedDocumentDiagnosticReport? a, UnchangedDocumentDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f1f7a7b99ec47d3d(a, b);
    }

    private static bool EqualShape_45b251721cf796c5(Unregistration? a, Unregistration? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_21a4e4496bc03a0b(a, b);
    }

    private static bool EqualShape_89d2825e130c081f(UnregistrationParams? a, UnregistrationParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5241e16167d32e95(a, b);
    }

    private static bool EqualShape_fe1808ad593e0e19(VSClassifiedTextElement? a, VSClassifiedTextElement? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_71a22c52ef2b5944(a, b);
    }

    private static bool EqualShape_f1616ff6438f019f(VSClassifiedTextRun? a, VSClassifiedTextRun? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_29e032165608952d(a, b);
    }

    private static bool EqualShape_2147172615c487bd(VSContainerElement? a, VSContainerElement? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ef2ac6c5999dfb93(a, b);
    }

    private static bool EqualShape_f8e77e12e4148327(VSImageElement? a, VSImageElement? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_87ce8a8264759003(a, b);
    }

    private static bool EqualShape_6e353e7a91e3eabb(VSImageId? a, VSImageId? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b601db4f6bc4b2de(a, b);
    }

    private static bool EqualShape_20a5f76192074c7b(VSOnAutoInsertOptions? a, VSOnAutoInsertOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cbb6e344e6f178f9(a, b);
    }

    private static bool EqualShape_b0684943ff20b457(VSOnAutoInsertParams? a, VSOnAutoInsertParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_90625166f43f5eca(a, b);
    }

    private static bool EqualShape_7e93cf4fd6cf39d8(VSOnAutoInsertResponseItem? a, VSOnAutoInsertResponseItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d38b6e9912bc25e1(a, b);
    }

    private static bool EqualShape_7d06f0b6619a5404(VSReferenceItem? a, VSReferenceItem? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_65d84b8c94226d82(a, b);
    }

    private static bool EqualShape_86af5e542d0d8a54(WatchKind? a, WatchKind? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_df60ed0d4edd21e9(a.Value, b.Value);
    }

    private static bool EqualShape_f734253d8a313081(WillSaveTextDocumentParams? a, WillSaveTextDocumentParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f923e9ff4f55ce6e(a, b);
    }

    private static bool EqualShape_b2dad6074f987d5f(WindowClientCapabilities? a, WindowClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9fd8a5a7e3a477bc(a, b);
    }

    private static bool EqualShape_731482de7fb6a532(WorkDoneProgressBegin? a, WorkDoneProgressBegin? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_77eefa47f6f89dd3(a, b);
    }

    private static bool EqualShape_cf9e752213deb611(WorkDoneProgressCancelParams? a, WorkDoneProgressCancelParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_fa2ceac6aff2f951(a, b);
    }

    private static bool EqualShape_b9145445c90c5757(WorkDoneProgressCreateParams? a, WorkDoneProgressCreateParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_876a6750f55a3e46(a, b);
    }

    private static bool EqualShape_0b7ae274031367c1(WorkDoneProgressEnd? a, WorkDoneProgressEnd? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8e9ae8bd94490e20(a, b);
    }

    private static bool EqualShape_fcb01c2b2c033678(WorkDoneProgressReport? a, WorkDoneProgressReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c0b20ac9d5d09a16(a, b);
    }

    private static bool EqualShape_a0bb3a1dab62b716(WorkspaceClientCapabilities? a, WorkspaceClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f80d356a037da136(a, b);
    }

    private static bool EqualShape_d2c90464a9b0b7dc(WorkspaceDiagnosticParams? a, WorkspaceDiagnosticParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ee5fd540df668407(a, b);
    }

    private static bool EqualShape_598bbeb1fb6b59db(WorkspaceDiagnosticReport? a, WorkspaceDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_68403f0f0129590d(a, b);
    }

    private static bool EqualShape_df1798df00f72a79(WorkspaceEdit? a, WorkspaceEdit? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f594be26f67ebc84(a, b);
    }

    private static bool EqualShape_547589d2e00acce8(WorkspaceEditClientCapabilities? a, WorkspaceEditClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f8870bd1e193be94(a, b);
    }

    private static bool EqualShape_67d56ad1036c4e75(WorkspaceEditMetadata? a, WorkspaceEditMetadata? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_683af1a028c8c949(a, b);
    }

    private static bool EqualShape_415708d422f69fe5(WorkspaceFolder? a, WorkspaceFolder? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ffec6af89eff0832(a, b);
    }

    private static bool EqualShape_88e5a1337985c61a(WorkspaceFoldersChangeEvent? a, WorkspaceFoldersChangeEvent? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cac484af091ededa(a, b);
    }

    private static bool EqualShape_f62df71aba721e59(WorkspaceFoldersOrNull? a, WorkspaceFoldersOrNull? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a529bfd28675e22d(a, b);
    }

    private static bool EqualShape_8e9369d40b64e986(WorkspaceFoldersServerCapabilities? a, WorkspaceFoldersServerCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76eae964c482175c(a, b);
    }

    private static bool EqualShape_a2796bb989312de1(WorkspaceFullDocumentDiagnosticReport? a, WorkspaceFullDocumentDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_27b2946fe8aba24d(a, b);
    }

    private static bool EqualShape_a858cc09b0d6150b(WorkspaceOptions? a, WorkspaceOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_891102cd69b5f6f3(a, b);
    }

    private static bool EqualShape_a563895c23d4de71(WorkspaceSymbol? a, WorkspaceSymbol? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e3f9d3600b02eeb1(a, b);
    }

    private static bool EqualShape_8f41f0be4b699b48(WorkspaceSymbolClientCapabilities? a, WorkspaceSymbolClientCapabilities? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a5728d8363acabeb(a, b);
    }

    private static bool EqualShape_ddca5fcfafe66f28(WorkspaceSymbolData? a, WorkspaceSymbolData? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d6eb674f2d558af4(a.Value, b.Value);
    }

    private static bool EqualShape_7710655c7ee6b505(WorkspaceSymbolOptions? a, WorkspaceSymbolOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_64a1fc35c89fbeab(a, b);
    }

    private static bool EqualShape_87f4d7099eb72b5f(WorkspaceSymbolParams? a, WorkspaceSymbolParams? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f45d058e1512eabf(a, b);
    }

    private static bool EqualShape_7f8c0262c5fa45b6(WorkspaceSymbolRegistrationOptions? a, WorkspaceSymbolRegistrationOptions? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dbb4c03cff5a9b2c(a, b);
    }

    private static bool EqualShape_16143ebe25c1037b(WorkspaceUnchangedDocumentDiagnosticReport? a, WorkspaceUnchangedDocumentDiagnosticReport? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c1a1987ea31ba882(a, b);
    }

    private static bool EqualShape_66ae27f385454775(Slice<uint>? a, Slice<uint>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2d5dae07671f98b7(a.Value, b.Value);
    }

    private static bool EqualShape_d1c51c51376d5e5c(Slice<CallHierarchyIncomingCall?>? a, Slice<CallHierarchyIncomingCall?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c60a33add77739d5(a.Value, b.Value);
    }

    private static bool EqualShape_6cc8830bf2904d2b(Slice<CallHierarchyItem?>? a, Slice<CallHierarchyItem?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9b09d17f086528f6(a.Value, b.Value);
    }

    private static bool EqualShape_5c439b35dcbd1fbc(Slice<CallHierarchyOutgoingCall?>? a, Slice<CallHierarchyOutgoingCall?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2c31c997f33109f1(a.Value, b.Value);
    }

    private static bool EqualShape_2ca73625c2b826f0(Slice<CodeActionKindDocumentation?>? a, Slice<CodeActionKindDocumentation?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b7ce3c701164809b(a.Value, b.Value);
    }

    private static bool EqualShape_3ce89d926ef007a2(Slice<CodeLens?>? a, Slice<CodeLens?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_70ab3b05fe66ce10(a.Value, b.Value);
    }

    private static bool EqualShape_419c6066a7befedc(Slice<CompletionItem?>? a, Slice<CompletionItem?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_72b625abb0de2281(a.Value, b.Value);
    }

    private static bool EqualShape_fec9371736dac724(Slice<Diagnostic?>? a, Slice<Diagnostic?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ce18911ddb414acc(a.Value, b.Value);
    }

    private static bool EqualShape_2b0b407063db97b3(Slice<DiagnosticRelatedInformation?>? a, Slice<DiagnosticRelatedInformation?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1559cac8798c0fa0(a.Value, b.Value);
    }

    private static bool EqualShape_e51c5a913937c7ce(Slice<DocumentHighlight?>? a, Slice<DocumentHighlight?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_671d1014e749357c(a.Value, b.Value);
    }

    private static bool EqualShape_1f19d74e1e78ea50(Slice<DocumentLink?>? a, Slice<DocumentLink?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2b3264d438c5cb67(a.Value, b.Value);
    }

    private static bool EqualShape_135e6f0abfdfb63b(Slice<DocumentSymbol?>? a, Slice<DocumentSymbol?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bf7087ec6c4d58fb(a.Value, b.Value);
    }

    private static bool EqualShape_c6718f7e5bb64a46(Slice<FoldingRange?>? a, Slice<FoldingRange?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4a8a3bbd5078844b(a.Value, b.Value);
    }

    private static bool EqualShape_910e533db5076de4(Slice<InlayHint?>? a, Slice<InlayHint?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0d3017096d66f3b1(a.Value, b.Value);
    }

    private static bool EqualShape_58bbbfbc30e2a502(Slice<InlayHintLabelPart?>? a, Slice<InlayHintLabelPart?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ba427d4ac914714f(a.Value, b.Value);
    }

    private static bool EqualShape_08d276aa8b28bb2f(Slice<InlineCompletionItem?>? a, Slice<InlineCompletionItem?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2ac7ed000ec4e5b2(a.Value, b.Value);
    }

    private static bool EqualShape_778f55dd7079fa7d(Slice<LocationLink?>? a, Slice<LocationLink?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4ff9339b88523bfc(a.Value, b.Value);
    }

    private static bool EqualShape_f5832b80cb969020(Slice<MessageActionItem?>? a, Slice<MessageActionItem?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d053cdadfa7a7223(a.Value, b.Value);
    }

    private static bool EqualShape_a84ffd8b4dd738c6(Slice<Moniker?>? a, Slice<Moniker?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cff0bac0044f0266(a.Value, b.Value);
    }

    private static bool EqualShape_b009ce335e5a312f(Slice<MultiDocumentHighlight?>? a, Slice<MultiDocumentHighlight?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_865bb029c4ea44b0(a.Value, b.Value);
    }

    private static bool EqualShape_487dd612ddbe2ce0(Slice<ParameterInformation?>? a, Slice<ParameterInformation?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_324bfc5f74dc2198(a.Value, b.Value);
    }

    private static bool EqualShape_f0f6f210736d68bc(Slice<SelectionRange?>? a, Slice<SelectionRange?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a5afe4cc8f9d19b3(a.Value, b.Value);
    }

    private static bool EqualShape_d8f673282f8de161(Slice<SymbolInformation?>? a, Slice<SymbolInformation?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_be4b95d73d017aee(a.Value, b.Value);
    }

    private static bool EqualShape_965c47d3497332b4(Slice<TextEdit?>? a, Slice<TextEdit?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7326b970e1272421(a.Value, b.Value);
    }

    private static bool EqualShape_88ee8d795f1da4c8(Slice<TypeHierarchyItem?>? a, Slice<TypeHierarchyItem?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3be68171a26cc119(a.Value, b.Value);
    }

    private static bool EqualShape_e9b47abbd83278a9(Slice<VSReferenceItem?>? a, Slice<VSReferenceItem?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_55cf654fd8b1bd53(a.Value, b.Value);
    }

    private static bool EqualShape_d7efea5eb04decde(Slice<WorkspaceFolder?>? a, Slice<WorkspaceFolder?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e365b467c8b502ca(a.Value, b.Value);
    }

    private static bool EqualShape_22017d3457b6fb5e(Slice<WorkspaceSymbol?>? a, Slice<WorkspaceSymbol?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f23180af26841dd8(a.Value, b.Value);
    }

    private static bool EqualShape_9e77fe2456b8adca(Slice<CodeActionKind>? a, Slice<CodeActionKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1dc84ac3dcf6749a(a.Value, b.Value);
    }

    private static bool EqualShape_daba325661334541(Slice<CodeActionTag>? a, Slice<CodeActionTag>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f7e208d55963916d(a.Value, b.Value);
    }

    private static bool EqualShape_9eb5bb2746e91c91(Slice<CommandOrCodeAction>? a, Slice<CommandOrCodeAction>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4109991f4006b883(a.Value, b.Value);
    }

    private static bool EqualShape_5dc4a752c0fa39c1(Slice<CompletionItemKind>? a, Slice<CompletionItemKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_78ac88b4cb4bbbef(a.Value, b.Value);
    }

    private static bool EqualShape_2150bf5242acd3a2(Slice<CompletionItemTag>? a, Slice<CompletionItemTag>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ea1bedafe91c8e42(a.Value, b.Value);
    }

    private static bool EqualShape_71d19dccff435d03(Slice<DiagnosticTag>? a, Slice<DiagnosticTag>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d1a5fcd9177b1e80(a.Value, b.Value);
    }

    private static bool EqualShape_c1deb99c4ec4592d(Slice<FoldingRangeKind>? a, Slice<FoldingRangeKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7ecdd9acf300e742(a.Value, b.Value);
    }

    private static bool EqualShape_e9c2bcf9dee60523(Slice<InlineValueTextOrVariableLookupOrEvaluatableExpression>? a, Slice<InlineValueTextOrVariableLookupOrEvaluatableExpression>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_00bafde87caef9b1(a.Value, b.Value);
    }

    private static bool EqualShape_b65d4719a95ca244(Slice<Location>? a, Slice<Location>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5610d1960004ba0d(a.Value, b.Value);
    }

    private static bool EqualShape_95df085df9457b35(Slice<MarkupKind>? a, Slice<MarkupKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1599a20ab951c327(a.Value, b.Value);
    }

    private static bool EqualShape_325bd644d4160e55(Slice<PositionEncodingKind>? a, Slice<PositionEncodingKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c901c7a8c1b9d477(a.Value, b.Value);
    }

    private static bool EqualShape_ba948791eff6cd24(Slice<ResourceOperationKind>? a, Slice<ResourceOperationKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dac512c95e2386c0(a.Value, b.Value);
    }

    private static bool EqualShape_e555e7503571f3c2(Slice<StringOrMarkedStringWithLanguage>? a, Slice<StringOrMarkedStringWithLanguage>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_87d8fc0cbda71f19(a.Value, b.Value);
    }

    private static bool EqualShape_18c695c9e7e44f01(Slice<SymbolKind>? a, Slice<SymbolKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_07349dd2472a64ed(a.Value, b.Value);
    }

    private static bool EqualShape_2a7c50a738423fd9(Slice<SymbolTag>? a, Slice<SymbolTag>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0a862cf9a5f1866e(a.Value, b.Value);
    }

    private static bool EqualShape_fdc9b4b090c79264(Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile>? a, Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4d5a6be4cf57fad6(a.Value, b.Value);
    }

    private static bool EqualShape_521fd5c1777fdd96(Slice<TextDocumentFilterLanguageOrSchemeOrPattern>? a, Slice<TextDocumentFilterLanguageOrSchemeOrPattern>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_afceee5ac36382a4(a.Value, b.Value);
    }

    private static bool EqualShape_e9a7ae9a4c39a84e(Slice<VSReferenceKind>? a, Slice<VSReferenceKind>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_638cf2a0f3f3a811(a.Value, b.Value);
    }

    private static bool EqualShape_576f0a462b844c7d(Slice<object?>? a, Slice<object?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6b4e60b92db020cb(a.Value, b.Value);
    }

    private static bool EqualShape_8941972aede71346(Slice<ByteStringValue>? a, Slice<ByteStringValue>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Value, b.Value);
    }

    private static bool EqualShape_1c48ab226b18fa3a(Slice<uint>? a, Slice<uint>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4c1cc0279aa62acf(a.Value, b.Value);
    }

    private static bool EqualShape_7f4d449efe399ec7(ProtocolPointer<object?>? a, ProtocolPointer<object?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d6a7cd2a7371b1a1(a.Value, b.Value);
    }

    private static bool EqualShape_31f02903a9b7a4aa(bool? a, bool? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.Value, b.Value);
    }

    private static bool EqualShape_c2c7219dbdd4eab1(int? a, int? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_05c312945775207c(a.Value, b.Value);
    }

    private static bool EqualShape_15bd2111816ca216(ProtocolMap<DocumentUri, FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport>? a, ProtocolMap<DocumentUri, FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_65e0d82b4a75d616(a.Value, b.Value);
    }

    private static bool EqualShape_1c52b3755ac5b562(ProtocolMap<DocumentUri, Slice<TextEdit?>>? a, ProtocolMap<DocumentUri, Slice<TextEdit?>>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_eadc07a4a900af66(a.Value, b.Value);
    }

    private static bool EqualShape_bab6c35c85f554b6(ProtocolMap<ByteStringKey, ChangeAnnotation?>? a, ProtocolMap<ByteStringKey, ChangeAnnotation?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b30c5bf781ea434f(a.Value, b.Value);
    }

    private static bool EqualShape_94061b27224c72c8(ProtocolMap<ByteStringKey, object?>? a, ProtocolMap<ByteStringKey, object?>? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_26c0c2adc8ab309a(a.Value, b.Value);
    }

    private static bool EqualShape_3e89801b52bf7f98(ByteStringValue? a, ByteStringValue? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Value, b.Value);
    }

    private static bool EqualShape_e0148ef8e4bd7459(EmptyProtocolObject? a, EmptyProtocolObject? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0d0ad1bdf1affd9c(a.Value, b.Value);
    }

    private static bool EqualShape_3f8175cc2ff18f25(uint? a, uint? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1c06d81634ed9a2e(a.Value, b.Value);
    }

    private static bool EqualShape_5bfde1568463cec7(AddAsTypeOnly a, AddAsTypeOnly b)
    {
        return EqualityComparer<AddAsTypeOnly>.Default.Equals(a, b);
    }

    private static bool EqualShape_dc76475f4b1ed640(AnnotatedTextEdit a, AnnotatedTextEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_473287f8298dba71(a.NewText, b.NewText) && EqualShape_473287f8298dba71(a.AnnotationId, b.AnnotationId);
    }

    private static bool EqualShape_14b702d0db1726aa(ApplyKind a, ApplyKind b)
    {
        return EqualityComparer<ApplyKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_54526a811be46335(ApplyWorkspaceEditParams a, ApplyWorkspaceEditParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Label, b.Label) && EqualShape_df1798df00f72a79(a.Edit, b.Edit) && EqualShape_67d56ad1036c4e75(a.Metadata, b.Metadata);
    }

    private static bool EqualShape_6d7f4c2b0309a43e(ApplyWorkspaceEditResult a, ApplyWorkspaceEditResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.Applied, b.Applied) && EqualShape_3e89801b52bf7f98(a.FailureReason, b.FailureReason) && EqualShape_3f8175cc2ff18f25(a.FailedChange, b.FailedChange);
    }

    private static bool EqualShape_2fa6cdfb6ada2628(AutoImportFix a, AutoImportFix b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3cb7a6f693cd9a7a(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_d39772dfff646e6d(a.ImportKind, b.ImportKind) && EqualShape_b760f44fa5965c24(a.UseRequire, b.UseRequire) && EqualShape_5bfde1568463cec7(a.AddAsTypeOnly, b.AddAsTypeOnly) && EqualShape_473287f8298dba71(a.ModuleSpecifier, b.ModuleSpecifier) && EqualShape_05c312945775207c(a.ImportIndex, b.ImportIndex) && EqualShape_020329ac5748a864(a.UsagePosition, b.UsagePosition) && EqualShape_473287f8298dba71(a.NamespacePrefix, b.NamespacePrefix);
    }

    private static bool EqualShape_3cb7a6f693cd9a7a(AutoImportFixKind a, AutoImportFixKind b)
    {
        return EqualityComparer<AutoImportFixKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_67f5c69ca6dc88bb(BaseSymbolInformation a, BaseSymbolInformation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_80525ce65b460521(a.Kind, b.Kind) && EqualShape_2a7c50a738423fd9(a.Tags, b.Tags) && EqualShape_3e89801b52bf7f98(a.ContainerName, b.ContainerName);
    }

    private static bool EqualShape_446299ae2bd49e81(BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions a, BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_7d3a2c5f8846331f(a.CallHierarchyOptions, b.CallHierarchyOptions) && EqualShape_f3fc39c6644381af(a.CallHierarchyRegistrationOptions, b.CallHierarchyRegistrationOptions);
    }

    private static bool EqualShape_ceb7a942d3e190ba(BooleanOrClientSemanticTokensRequestFullDelta a, BooleanOrClientSemanticTokensRequestFullDelta b)
    {
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_58d29bcb4ddfa875(a.ClientSemanticTokensRequestFullDelta, b.ClientSemanticTokensRequestFullDelta);
    }

    private static bool EqualShape_e9690919b3d8115d(BooleanOrCodeActionOptions a, BooleanOrCodeActionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_06a20c8c465f1bd8(a.CodeActionOptions, b.CodeActionOptions);
    }

    private static bool EqualShape_145e615b6579c981(BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions a, BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_13687efc1ec01c3a(a.DeclarationOptions, b.DeclarationOptions) && EqualShape_4686cbddb0e0dea7(a.DeclarationRegistrationOptions, b.DeclarationRegistrationOptions);
    }

    private static bool EqualShape_cf52b6cb9db4fe19(BooleanOrDefinitionOptions a, BooleanOrDefinitionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_c28e132512d210cf(a.DefinitionOptions, b.DefinitionOptions);
    }

    private static bool EqualShape_1474d27e4d0c6fa5(BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions a, BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_ddcb3de072c3c453(a.DocumentColorOptions, b.DocumentColorOptions) && EqualShape_e69c3a42ec51132a(a.DocumentColorRegistrationOptions, b.DocumentColorRegistrationOptions);
    }

    private static bool EqualShape_a917cb45ee3cef42(BooleanOrDocumentFormattingOptions a, BooleanOrDocumentFormattingOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_41b8cb3798a0eaa3(a.DocumentFormattingOptions, b.DocumentFormattingOptions);
    }

    private static bool EqualShape_54fc7b1b34343b4a(BooleanOrDocumentHighlightOptions a, BooleanOrDocumentHighlightOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_2e7180cdcd7328e7(a.DocumentHighlightOptions, b.DocumentHighlightOptions);
    }

    private static bool EqualShape_c5052a5c26268d1f(BooleanOrDocumentRangeFormattingOptions a, BooleanOrDocumentRangeFormattingOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_50b39195f03a0636(a.DocumentRangeFormattingOptions, b.DocumentRangeFormattingOptions);
    }

    private static bool EqualShape_c021a4c2316929b7(BooleanOrDocumentSymbolOptions a, BooleanOrDocumentSymbolOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_41bb397a975775f8(a.DocumentSymbolOptions, b.DocumentSymbolOptions);
    }

    private static bool EqualShape_0464b31e8eb7a40e(BooleanOrEmptyObject a, BooleanOrEmptyObject b)
    {
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_e0148ef8e4bd7459(a.EmptyObject, b.EmptyObject);
    }

    private static bool EqualShape_0a0a219b5ffede8b(BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions a, BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_4bb278676873c924(a.FoldingRangeOptions, b.FoldingRangeOptions) && EqualShape_2248c420e67b79a4(a.FoldingRangeRegistrationOptions, b.FoldingRangeRegistrationOptions);
    }

    private static bool EqualShape_baa50df5ae26800a(BooleanOrHoverOptions a, BooleanOrHoverOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_bd657437c0767aea(a.HoverOptions, b.HoverOptions);
    }

    private static bool EqualShape_15d10a5acdcde7ec(BooleanOrImplementationOptionsOrImplementationRegistrationOptions a, BooleanOrImplementationOptionsOrImplementationRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_ecb8c575e0dac043(a.ImplementationOptions, b.ImplementationOptions) && EqualShape_a1158e8b376f8c3c(a.ImplementationRegistrationOptions, b.ImplementationRegistrationOptions);
    }

    private static bool EqualShape_feb69b4dbb92199a(BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions a, BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_248b47c8dc0cacef(a.InlayHintOptions, b.InlayHintOptions) && EqualShape_85717bf7a3915adc(a.InlayHintRegistrationOptions, b.InlayHintRegistrationOptions);
    }

    private static bool EqualShape_959a1d992a272787(BooleanOrInlineCompletionOptions a, BooleanOrInlineCompletionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_e28e3677bae697ca(a.InlineCompletionOptions, b.InlineCompletionOptions);
    }

    private static bool EqualShape_1d2b7a4459f0e39b(BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions a, BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_f59f02a865f30930(a.InlineValueOptions, b.InlineValueOptions) && EqualShape_6740dd541d21b3a4(a.InlineValueRegistrationOptions, b.InlineValueRegistrationOptions);
    }

    private static bool EqualShape_0d2d61395bd0e247(BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions a, BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_f059f04438094981(a.LinkedEditingRangeOptions, b.LinkedEditingRangeOptions) && EqualShape_bc219c8cf4519a73(a.LinkedEditingRangeRegistrationOptions, b.LinkedEditingRangeRegistrationOptions);
    }

    private static bool EqualShape_afd51367d6d2382c(BooleanOrMonikerOptionsOrMonikerRegistrationOptions a, BooleanOrMonikerOptionsOrMonikerRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_da81b08167dd97fa(a.MonikerOptions, b.MonikerOptions) && EqualShape_f75de590883ed072(a.MonikerRegistrationOptions, b.MonikerRegistrationOptions);
    }

    private static bool EqualShape_3b3fec573e871e5d(BooleanOrReferenceOptions a, BooleanOrReferenceOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_01630610b86f2479(a.ReferenceOptions, b.ReferenceOptions);
    }

    private static bool EqualShape_504bddb6f7a15921(BooleanOrRenameOptions a, BooleanOrRenameOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_9118f2a9dd376545(a.RenameOptions, b.RenameOptions);
    }

    private static bool EqualShape_41952583e79cd693(BooleanOrSaveOptions a, BooleanOrSaveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_b5e23cfa61bf82e5(a.SaveOptions, b.SaveOptions);
    }

    private static bool EqualShape_5d2769c3484cdd3e(BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions a, BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_91343bb11414d053(a.SelectionRangeOptions, b.SelectionRangeOptions) && EqualShape_00c568d61d6e4506(a.SelectionRangeRegistrationOptions, b.SelectionRangeRegistrationOptions);
    }

    private static bool EqualShape_7df65eef7163c6f4(BooleanOrSemanticTokensFullDelta a, BooleanOrSemanticTokensFullDelta b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_3e6981e05f891299(a.SemanticTokensFullDelta, b.SemanticTokensFullDelta);
    }

    private static bool EqualShape_ed71ad313eefc7ad(BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions a, BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_13996fbd8ef62326(a.TypeDefinitionOptions, b.TypeDefinitionOptions) && EqualShape_dc874b303baa6dbb(a.TypeDefinitionRegistrationOptions, b.TypeDefinitionRegistrationOptions);
    }

    private static bool EqualShape_87827fc2be5c0773(BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions a, BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_5a2b4afa8c993da1(a.TypeHierarchyOptions, b.TypeHierarchyOptions) && EqualShape_fd11d6b574dafc11(a.TypeHierarchyRegistrationOptions, b.TypeHierarchyRegistrationOptions);
    }

    private static bool EqualShape_e5bfff85c753a8b1(BooleanOrWorkspaceSymbolOptions a, BooleanOrWorkspaceSymbolOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean) && EqualShape_7710655c7ee6b505(a.WorkspaceSymbolOptions, b.WorkspaceSymbolOptions);
    }

    private static bool EqualShape_383095cfb255a14d(CallHierarchyClientCapabilities a, CallHierarchyClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_e184a9b2f92e1a06(CallHierarchyIncomingCall a, CallHierarchyIncomingCall b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_52678145e6f3309d(a.From, b.From) && EqualShape_395c34a167937420(a.FromRanges, b.FromRanges);
    }

    private static bool EqualShape_d97c2ffc45ab575f(CallHierarchyIncomingCallsOrNull a, CallHierarchyIncomingCallsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d1c51c51376d5e5c(a.CallHierarchyIncomingCalls, b.CallHierarchyIncomingCalls);
    }

    private static bool EqualShape_3a8cdb686bf71f77(CallHierarchyIncomingCallsParams a, CallHierarchyIncomingCallsParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_52678145e6f3309d(a.Item, b.Item);
    }

    private static bool EqualShape_8c7fd0716410365c(CallHierarchyItem a, CallHierarchyItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_80525ce65b460521(a.Kind, b.Kind) && EqualShape_2a7c50a738423fd9(a.Tags, b.Tags) && EqualShape_3e89801b52bf7f98(a.Detail, b.Detail) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_5de74a81b192304c(a.SelectionRange, b.SelectionRange) && EqualShape_6e332018a1c206ec(a.Data, b.Data);
    }

    private static bool EqualShape_65359ea06e6d25e6(CallHierarchyItemData a, CallHierarchyItemData b)
    {
        return true;
    }

    private static bool EqualShape_90a14ad0a9a8b850(CallHierarchyItemsOrNull a, CallHierarchyItemsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6cc8830bf2904d2b(a.CallHierarchyItems, b.CallHierarchyItems);
    }

    private static bool EqualShape_bf40a5eb811d3651(CallHierarchyOptions a, CallHierarchyOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_4589918246a73c8d(CallHierarchyOutgoingCall a, CallHierarchyOutgoingCall b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_52678145e6f3309d(a.To, b.To) && EqualShape_395c34a167937420(a.FromRanges, b.FromRanges);
    }

    private static bool EqualShape_7db6fb25db830142(CallHierarchyOutgoingCallsOrNull a, CallHierarchyOutgoingCallsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5c439b35dcbd1fbc(a.CallHierarchyOutgoingCalls, b.CallHierarchyOutgoingCalls);
    }

    private static bool EqualShape_683273adad7a85de(CallHierarchyOutgoingCallsParams a, CallHierarchyOutgoingCallsParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_52678145e6f3309d(a.Item, b.Item);
    }

    private static bool EqualShape_0c095bc834e4d3c8(CallHierarchyPrepareParams a, CallHierarchyPrepareParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken);
    }

    private static bool EqualShape_1f08ef49b14db62c(CallHierarchyRegistrationOptions a, CallHierarchyRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_68e14858b5be82dc(CancelParams a, CancelParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95e25e81cd99bf99(a.Id, b.Id);
    }

    private static bool EqualShape_95a49b2bfa6a44cd(ChangeAnnotation a, ChangeAnnotation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Label, b.Label) && EqualShape_31f02903a9b7a4aa(a.NeedsConfirmation, b.NeedsConfirmation) && EqualShape_3e89801b52bf7f98(a.Description, b.Description);
    }

    private static bool EqualShape_767b65d5f0d728e6(ChangeAnnotationsSupportOptions a, ChangeAnnotationsSupportOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.GroupsOnLabel, b.GroupsOnLabel);
    }

    private static bool EqualShape_2517ecc24dbf1859(ClassificationTypeName a, ClassificationTypeName b)
    {
        return EqualityComparer<ClassificationTypeName>.Default.Equals(a, b);
    }

    private static bool EqualShape_ae5beebabf5633ee(ClientCapabilities a, ClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a0bb3a1dab62b716(a.Workspace, b.Workspace) && EqualShape_9d8ecb04ded0e67b(a.TextDocument, b.TextDocument) && EqualShape_b2dad6074f987d5f(a.Window, b.Window) && EqualShape_b0c64514ae89504b(a.General, b.General) && EqualShape_fcda8a980d4c687d(a.Experimental, b.Experimental) && EqualShape_31f02903a9b7a4aa(a.VSSupportsVisualStudioExtensions, b.VSSupportsVisualStudioExtensions) && EqualShape_c2c7219dbdd4eab1(a.VSSupportedSnippetVersion, b.VSSupportedSnippetVersion) && EqualShape_31f02903a9b7a4aa(a.VSSupportsNotIncludingTextInTextDocumentDidOpen, b.VSSupportsNotIncludingTextInTextDocumentDidOpen) && EqualShape_31f02903a9b7a4aa(a.VSSupportsIconExtensions, b.VSSupportsIconExtensions) && EqualShape_31f02903a9b7a4aa(a.VSSupportsDiagnosticRequests, b.VSSupportsDiagnosticRequests);
    }

    private static bool EqualShape_c1f39ce851ecc9a0(ClientCodeActionKindOptions a, ClientCodeActionKindOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1dc84ac3dcf6749a(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_97349393a7e84c9a(ClientCodeActionLiteralOptions a, ClientCodeActionLiteralOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_29d190fdf01733b6(a.CodeActionKind, b.CodeActionKind);
    }

    private static bool EqualShape_56c69147319a7d0b(ClientCodeActionResolveOptions a, ClientCodeActionResolveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_d931abba359e22bd(ClientCodeLensResolveOptions a, ClientCodeLensResolveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_28bf4fd56d374e67(ClientCompletionItemInsertTextModeOptions a, ClientCompletionItemInsertTextModeOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_39d79f06277c11aa(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_b37b4bbb7ef32304(ClientCompletionItemOptions a, ClientCompletionItemOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.SnippetSupport, b.SnippetSupport) && EqualShape_31f02903a9b7a4aa(a.CommitCharactersSupport, b.CommitCharactersSupport) && EqualShape_95df085df9457b35(a.DocumentationFormat, b.DocumentationFormat) && EqualShape_31f02903a9b7a4aa(a.DeprecatedSupport, b.DeprecatedSupport) && EqualShape_31f02903a9b7a4aa(a.PreselectSupport, b.PreselectSupport) && EqualShape_7730fd624a35ac9d(a.TagSupport, b.TagSupport) && EqualShape_31f02903a9b7a4aa(a.InsertReplaceSupport, b.InsertReplaceSupport) && EqualShape_0b540d7879247121(a.ResolveSupport, b.ResolveSupport) && EqualShape_54a61a2bbc545d95(a.InsertTextModeSupport, b.InsertTextModeSupport) && EqualShape_31f02903a9b7a4aa(a.LabelDetailsSupport, b.LabelDetailsSupport);
    }

    private static bool EqualShape_ea7a2cb221fe3126(ClientCompletionItemOptionsKind a, ClientCompletionItemOptionsKind b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5dc4a752c0fa39c1(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_08243bf3c503d83a(ClientCompletionItemResolveOptions a, ClientCompletionItemResolveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_e354d3fe1df50a36(ClientDiagnosticsTagOptions a, ClientDiagnosticsTagOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d1a5fcd9177b1e80(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_51037275a4d10b5f(ClientFoldingRangeKindOptions a, ClientFoldingRangeKindOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c1deb99c4ec4592d(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_a627df9eb6b0b5da(ClientFoldingRangeOptions a, ClientFoldingRangeOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.CollapsedText, b.CollapsedText);
    }

    private static bool EqualShape_e58e814bcb67ebf4(ClientInfo a, ClientInfo b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_3e89801b52bf7f98(a.Version, b.Version);
    }

    private static bool EqualShape_43b1b75d4aa5fd41(ClientInlayHintResolveOptions a, ClientInlayHintResolveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_e1f502acb9f727c5(ClientSemanticTokensRequestFullDelta a, ClientSemanticTokensRequestFullDelta b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Delta, b.Delta);
    }

    private static bool EqualShape_1e6c97fe656a3276(ClientSemanticTokensRequestOptions a, ClientSemanticTokensRequestOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5b602f5e58c34242(a.@Range, b.@Range) && EqualShape_b4c3dc66bb9d16b0(a.Full, b.Full);
    }

    private static bool EqualShape_0d5e37ca0a60abe2(ClientShowMessageActionItemOptions a, ClientShowMessageActionItemOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.AdditionalPropertiesSupport, b.AdditionalPropertiesSupport);
    }

    private static bool EqualShape_6a3fd3a20dce8c99(ClientSignatureInformationOptions a, ClientSignatureInformationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95df085df9457b35(a.DocumentationFormat, b.DocumentationFormat) && EqualShape_c7579c3877cec4b4(a.ParameterInformation, b.ParameterInformation) && EqualShape_31f02903a9b7a4aa(a.ActiveParameterSupport, b.ActiveParameterSupport) && EqualShape_31f02903a9b7a4aa(a.NoActiveParameterSupport, b.NoActiveParameterSupport);
    }

    private static bool EqualShape_133fc1cbd2a995c6(ClientSignatureParameterInformationOptions a, ClientSignatureParameterInformationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.LabelOffsetSupport, b.LabelOffsetSupport);
    }

    private static bool EqualShape_bfc7ea6558336881(ClientSymbolKindOptions a, ClientSymbolKindOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_18c695c9e7e44f01(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_8e0f26b4017e860f(ClientSymbolResolveOptions a, ClientSymbolResolveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_51e4a8fb8975920a(ClientSymbolTagOptions a, ClientSymbolTagOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0a862cf9a5f1866e(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_865c9c8fafe5c729(CodeAction a, CodeAction b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Title, b.Title) && EqualShape_34290cef18c63dd2(a.Kind, b.Kind) && EqualShape_fec9371736dac724(a.Diagnostics, b.Diagnostics) && EqualShape_31f02903a9b7a4aa(a.IsPreferred, b.IsPreferred) && EqualShape_e83e8f04df156ab0(a.Disabled, b.Disabled) && EqualShape_df1798df00f72a79(a.Edit, b.Edit) && EqualShape_a6c992bdda521682(a.Command, b.Command) && EqualShape_ab7c5b654dbfe20d(a.Data, b.Data) && EqualShape_daba325661334541(a.Tags, b.Tags);
    }

    private static bool EqualShape_e7f7570213c73b8e(CodeActionClientCapabilities a, CodeActionClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_56d6bf9bb39696ab(a.CodeActionLiteralSupport, b.CodeActionLiteralSupport) && EqualShape_31f02903a9b7a4aa(a.IsPreferredSupport, b.IsPreferredSupport) && EqualShape_31f02903a9b7a4aa(a.DisabledSupport, b.DisabledSupport) && EqualShape_31f02903a9b7a4aa(a.DataSupport, b.DataSupport) && EqualShape_45d71b781cb3ee1d(a.ResolveSupport, b.ResolveSupport) && EqualShape_31f02903a9b7a4aa(a.HonorsChangeAnnotations, b.HonorsChangeAnnotations) && EqualShape_31f02903a9b7a4aa(a.DocumentationSupport, b.DocumentationSupport) && EqualShape_a161c2bc3bcc3cc8(a.TagSupport, b.TagSupport);
    }

    private static bool EqualShape_5e9db653ca06c3c9(CodeActionContext a, CodeActionContext b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ce18911ddb414acc(a.Diagnostics, b.Diagnostics) && EqualShape_9e77fe2456b8adca(a.Only, b.Only) && EqualShape_b5d5feda1821575b(a.TriggerKind, b.TriggerKind);
    }

    private static bool EqualShape_64c59af8f44adf5e(CodeActionData a, CodeActionData b)
    {
        return true;
    }

    private static bool EqualShape_081648c1b4ef6f5e(CodeActionDisabled a, CodeActionDisabled b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Reason, b.Reason);
    }

    private static bool EqualShape_ecff755ce44421bb(CodeActionKind a, CodeActionKind b)
    {
        return EqualityComparer<CodeActionKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_4271ba828c998e5f(CodeActionKindDocumentation a, CodeActionKindDocumentation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ecff755ce44421bb(a.Kind, b.Kind) && EqualShape_a6c992bdda521682(a.Command, b.Command);
    }

    private static bool EqualShape_c349820ded303c22(CodeActionOptions a, CodeActionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_9e77fe2456b8adca(a.CodeActionKinds, b.CodeActionKinds) && EqualShape_2ca73625c2b826f0(a.Documentation, b.Documentation) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_5fd24f9135249493(CodeActionParams a, CodeActionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_428ffbee266930e2(a.Context, b.Context);
    }

    private static bool EqualShape_9b2193d2ea04428b(CodeActionRegistrationOptions a, CodeActionRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_9e77fe2456b8adca(a.CodeActionKinds, b.CodeActionKinds) && EqualShape_2ca73625c2b826f0(a.Documentation, b.Documentation) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_22fd923137cdbcf0(CodeActionTag a, CodeActionTag b)
    {
        return EqualityComparer<CodeActionTag>.Default.Equals(a, b);
    }

    private static bool EqualShape_412c1650085309d1(CodeActionTagOptions a, CodeActionTagOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f7e208d55963916d(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_01415bcc3e8debdf(CodeActionTriggerKind a, CodeActionTriggerKind b)
    {
        return EqualityComparer<CodeActionTriggerKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_d5d05065bc890cc5(CodeDescription a, CodeDescription b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d70fbad88d0f9eaf(a.Href, b.Href);
    }

    private static bool EqualShape_9e0c959c15c790a3(CodeLens a, CodeLens b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_a6c992bdda521682(a.Command, b.Command) && EqualShape_5b5c06a0e17dacd3(a.Data, b.Data);
    }

    private static bool EqualShape_f0921b0a90d3de37(CodeLensClientCapabilities a, CodeLensClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_50f962c22f1d0a22(a.ResolveSupport, b.ResolveSupport);
    }

    private static bool EqualShape_f549b1b2ff50cdf8(CodeLensData a, CodeLensData b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4be58987e0e9adc8(a.Kind, b.Kind) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_05c312945775207c(a.Position, b.Position) && EqualShape_c2c7219dbdd4eab1(a.SupplementalFileIndex, b.SupplementalFileIndex);
    }

    private static bool EqualShape_4be58987e0e9adc8(CodeLensKind a, CodeLensKind b)
    {
        return EqualityComparer<CodeLensKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_b5c29b63aaa2a6f8(CodeLensOptions a, CodeLensOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_4a62670ff2e4f6dc(CodeLensParams a, CodeLensParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_d5d1b6fc9d1f6ede(CodeLensRegistrationOptions a, CodeLensRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_27b4581cda4b0d84(CodeLensWorkspaceClientCapabilities a, CodeLensWorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_a0de8ff39d760531(CodeLensesOrNull a, CodeLensesOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3ce89d926ef007a2(a.CodeLenses, b.CodeLenses);
    }

    private static bool EqualShape_6b73191a0a4b6742(Color a, Color b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6bd2a66c4467bc37(a.Red, b.Red) && EqualShape_6bd2a66c4467bc37(a.Green, b.Green) && EqualShape_6bd2a66c4467bc37(a.Blue, b.Blue) && EqualShape_6bd2a66c4467bc37(a.Alpha, b.Alpha);
    }

    private static bool EqualShape_d5d2d1066d39a9f1(ColorInformation a, ColorInformation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_6b73191a0a4b6742(a.Color, b.Color);
    }

    private static bool EqualShape_1f1f6c1ccc446b5b(ColorPresentation a, ColorPresentation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Label, b.Label) && EqualShape_56642992931a7c98(a.TextEdit, b.TextEdit) && EqualShape_965c47d3497332b4(a.AdditionalTextEdits, b.AdditionalTextEdits);
    }

    private static bool EqualShape_ef2f7018da26a917(ColorPresentationParams a, ColorPresentationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6b73191a0a4b6742(a.Color, b.Color) && EqualShape_5de74a81b192304c(a.@Range, b.@Range);
    }

    private static bool EqualShape_713166971d730f81(Command a, Command b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Title, b.Title) && EqualShape_3e89801b52bf7f98(a.Tooltip, b.Tooltip) && EqualShape_473287f8298dba71(a.CommandValue, b.CommandValue) && EqualShape_576f0a462b844c7d(a.Arguments, b.Arguments);
    }

    private static bool EqualShape_58a4b3825288a73b(CommandOrCodeAction a, CommandOrCodeAction b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a6c992bdda521682(a.Command, b.Command) && EqualShape_a85aecd48f6d75ee(a.CodeAction, b.CodeAction);
    }

    private static bool EqualShape_cada04bb4fe9d5c5(CommandOrCodeActionArrayOrNull a, CommandOrCodeActionArrayOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9eb5bb2746e91c91(a.CommandOrCodeActionArray, b.CommandOrCodeActionArray);
    }

    private static bool EqualShape_16f91af89436100d(CompletionClientCapabilities a, CompletionClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_9fe8908a524bc20a(a.CompletionItem, b.CompletionItem) && EqualShape_3e40fa5e66a3fd31(a.CompletionItemKind, b.CompletionItemKind) && EqualShape_7edf63c19953acbf(a.InsertTextMode, b.InsertTextMode) && EqualShape_31f02903a9b7a4aa(a.ContextSupport, b.ContextSupport) && EqualShape_64f00aa06587937a(a.CompletionList, b.CompletionList);
    }

    private static bool EqualShape_e236ca93686fcfa1(CompletionContext a, CompletionContext b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_08b806e2da960947(a.TriggerKind, b.TriggerKind) && EqualShape_3e89801b52bf7f98(a.TriggerCharacter, b.TriggerCharacter);
    }

    private static bool EqualShape_d2a93ce39bb367de(CompletionItem a, CompletionItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Label, b.Label) && EqualShape_8f3b8df56b881212(a.LabelDetails, b.LabelDetails) && EqualShape_7b038bf5ef819f1b(a.Kind, b.Kind) && EqualShape_2150bf5242acd3a2(a.Tags, b.Tags) && EqualShape_3e89801b52bf7f98(a.Detail, b.Detail) && EqualShape_818def5a213356d9(a.Documentation, b.Documentation) && EqualShape_31f02903a9b7a4aa(a.Deprecated, b.Deprecated) && EqualShape_31f02903a9b7a4aa(a.Preselect, b.Preselect) && EqualShape_3e89801b52bf7f98(a.SortText, b.SortText) && EqualShape_3e89801b52bf7f98(a.FilterText, b.FilterText) && EqualShape_3e89801b52bf7f98(a.InsertText, b.InsertText) && EqualShape_7c5ad57d1d134af0(a.InsertTextFormat, b.InsertTextFormat) && EqualShape_7edf63c19953acbf(a.InsertTextMode, b.InsertTextMode) && EqualShape_d401c45b8c71281c(a.TextEdit, b.TextEdit) && EqualShape_3e89801b52bf7f98(a.TextEditText, b.TextEditText) && EqualShape_965c47d3497332b4(a.AdditionalTextEdits, b.AdditionalTextEdits) && EqualShape_8941972aede71346(a.CommitCharacters, b.CommitCharacters) && EqualShape_a6c992bdda521682(a.Command, b.Command) && EqualShape_30f6a0420ca353f5(a.Data, b.Data);
    }

    private static bool EqualShape_946202155aedc75c(CompletionItemApplyKinds a, CompletionItemApplyKinds b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_539cd689badc5bf0(a.CommitCharacters, b.CommitCharacters) && EqualShape_539cd689badc5bf0(a.Data, b.Data);
    }

    private static bool EqualShape_ec574f9f7f49a56a(CompletionItemData a, CompletionItemData b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.FileName, b.FileName) && EqualShape_05c312945775207c(a.Position, b.Position) && EqualShape_c2c7219dbdd4eab1(a.SupplementalFileIndex, b.SupplementalFileIndex) && EqualShape_473287f8298dba71(a.Source, b.Source) && EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_5a6a4da133030761(a.AutoImport, b.AutoImport) && EqualShape_b760f44fa5965c24(a.IsImportStatementCompletion, b.IsImportStatementCompletion);
    }

    private static bool EqualShape_467395eb042a5b79(CompletionItemDefaults a, CompletionItemDefaults b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8941972aede71346(a.CommitCharacters, b.CommitCharacters) && EqualShape_3c5dcfa086349e56(a.EditRange, b.EditRange) && EqualShape_7c5ad57d1d134af0(a.InsertTextFormat, b.InsertTextFormat) && EqualShape_7edf63c19953acbf(a.InsertTextMode, b.InsertTextMode) && EqualShape_a27705e55c8812a8(a.Data, b.Data);
    }

    private static bool EqualShape_04b1eeb817033ba6(CompletionItemDefaultsData a, CompletionItemDefaultsData b)
    {
        return true;
    }

    private static bool EqualShape_b8f2471eb8fc0cb3(CompletionItemKind a, CompletionItemKind b)
    {
        return EqualityComparer<CompletionItemKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_4918d4bf2593f8af(CompletionItemLabelDetails a, CompletionItemLabelDetails b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Detail, b.Detail) && EqualShape_3e89801b52bf7f98(a.Description, b.Description);
    }

    private static bool EqualShape_5397ab5a342cc703(CompletionItemTag a, CompletionItemTag b)
    {
        return EqualityComparer<CompletionItemTag>.Default.Equals(a, b);
    }

    private static bool EqualShape_9d1d9d8587fbcefd(CompletionItemTagOptions a, CompletionItemTagOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ea1bedafe91c8e42(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_96d4df6d20dad568(CompletionItemsOrListOrNull a, CompletionItemsOrListOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_419c6066a7befedc(a.Items, b.Items) && EqualShape_4fcf690eadce272f(a.List, b.List);
    }

    private static bool EqualShape_f8ad52b4cd7e25cc(CompletionList a, CompletionList b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.IsIncomplete, b.IsIncomplete) && EqualShape_a0a77280d06f4e70(a.ItemDefaults, b.ItemDefaults) && EqualShape_9e95dd258053a4f7(a.ApplyKind, b.ApplyKind) && EqualShape_72b625abb0de2281(a.Items, b.Items);
    }

    private static bool EqualShape_c850bc8b5898a45e(CompletionListCapabilities a, CompletionListCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8941972aede71346(a.ItemDefaults, b.ItemDefaults) && EqualShape_31f02903a9b7a4aa(a.ApplyKindSupport, b.ApplyKindSupport);
    }

    private static bool EqualShape_1d476dc84cbde137(CompletionOptions a, CompletionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_8941972aede71346(a.TriggerCharacters, b.TriggerCharacters) && EqualShape_8941972aede71346(a.AllCommitCharacters, b.AllCommitCharacters) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider) && EqualShape_66abf0144b9c6652(a.CompletionItem, b.CompletionItem);
    }

    private static bool EqualShape_a52f8dcb7c004673(CompletionParams a, CompletionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_f06cca5d2d4cbbca(a.Context, b.Context);
    }

    private static bool EqualShape_78ef87019ed5c3c1(CompletionRegistrationOptions a, CompletionRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_8941972aede71346(a.TriggerCharacters, b.TriggerCharacters) && EqualShape_8941972aede71346(a.AllCommitCharacters, b.AllCommitCharacters) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider) && EqualShape_66abf0144b9c6652(a.CompletionItem, b.CompletionItem);
    }

    private static bool EqualShape_08b806e2da960947(CompletionTriggerKind a, CompletionTriggerKind b)
    {
        return EqualityComparer<CompletionTriggerKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_6fb58034d4f05554(ConfigurationItem a, ConfigurationItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5b3003eb17f67429(a.ScopeUri, b.ScopeUri) && EqualShape_3e89801b52bf7f98(a.Section, b.Section);
    }

    private static bool EqualShape_99d81d86d0860ed1(ConfigurationParams a, ConfigurationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3f0803b0c56b9ff5(a.Items, b.Items);
    }

    private static bool EqualShape_dfd2d49e4be6f7a1(ContentMapperContribution a, ContentMapperContribution b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.ContributorId, b.ContributorId) && EqualShape_4af101177fe720bf(a.Extensions, b.Extensions) && EqualShape_c90d452ef07656ad(a.InferredProjectContribution, b.InferredProjectContribution);
    }

    private static bool EqualShape_ac126e252e4ca42c(ContentMapperManifest a, ContentMapperManifest b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_3e89801b52bf7f98(a.Version, b.Version) && EqualShape_4af101177fe720bf(a.Exec, b.Exec) && EqualShape_3e89801b52bf7f98(a.Cwd, b.Cwd) && EqualShape_8941972aede71346(a.CompilerOptions, b.CompilerOptions) && EqualShape_31f02903a9b7a4aa(a.DynamicConfig, b.DynamicConfig);
    }

    private static bool EqualShape_9d3a24d3495132d7(CreateFile a, CreateFile b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_15379156e683e93e(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.AnnotationId, b.AnnotationId) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_0d4f750a1e2fe6dc(a.Options, b.Options);
    }

    private static bool EqualShape_ad7b628c8a318f7f(CreateFileOptions a, CreateFileOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Overwrite, b.Overwrite) && EqualShape_31f02903a9b7a4aa(a.IgnoreIfExists, b.IgnoreIfExists);
    }

    private static bool EqualShape_c1b6632f7bfe8d76(CreateFilesParams a, CreateFilesParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_dc44ad1109493e27(a.Files, b.Files);
    }

    private static bool EqualShape_d092d2b3e5d67c33(DeclarationClientCapabilities a, DeclarationClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_55db17bfd8074c50(DeclarationOptions a, DeclarationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_67e2762ff01d380a(DeclarationParams a, DeclarationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_64b3f4521813f6e3(DeclarationRegistrationOptions a, DeclarationRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_d782be7c64b733ad(DefinitionClientCapabilities a, DefinitionClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_9faf6afba7757762(DefinitionOptions a, DefinitionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_2c8b11f5c8545467(DefinitionParams a, DefinitionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_477146b0718707a3(DefinitionRegistrationOptions a, DefinitionRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_bbfcf281d1c9c237(DeleteFile a, DeleteFile b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6681ac8dac15fa23(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.AnnotationId, b.AnnotationId) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_7593b2981911d93d(a.Options, b.Options);
    }

    private static bool EqualShape_1f8e9018d646804f(DeleteFileOptions a, DeleteFileOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Recursive, b.Recursive) && EqualShape_31f02903a9b7a4aa(a.IgnoreIfNotExists, b.IgnoreIfNotExists);
    }

    private static bool EqualShape_54b8495600617e2e(DeleteFilesParams a, DeleteFilesParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0dec4654e36c7064(a.Files, b.Files);
    }

    private static bool EqualShape_b1fa878a98a15b74(Diagnostic a, Diagnostic b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_cd9c63e32586179a(a.Severity, b.Severity) && EqualShape_c3d8a0302fb53f84(a.Code, b.Code) && EqualShape_4b00f8336e551717(a.CodeDescription, b.CodeDescription) && EqualShape_3e89801b52bf7f98(a.Source, b.Source) && EqualShape_31e9efe8f25ec13e(a.Message, b.Message) && EqualShape_71d19dccff435d03(a.Tags, b.Tags) && EqualShape_2b0b407063db97b3(a.RelatedInformation, b.RelatedInformation) && EqualShape_4cd06a52127e4782(a.Data, b.Data);
    }

    private static bool EqualShape_acc80cae1a8d44b9(DiagnosticClientCapabilities a, DiagnosticClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RelatedInformation, b.RelatedInformation) && EqualShape_01178517db2f6abb(a.TagSupport, b.TagSupport) && EqualShape_31f02903a9b7a4aa(a.CodeDescriptionSupport, b.CodeDescriptionSupport) && EqualShape_31f02903a9b7a4aa(a.DataSupport, b.DataSupport) && EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.RelatedDocumentSupport, b.RelatedDocumentSupport) && EqualShape_31f02903a9b7a4aa(a.MarkupMessageSupport, b.MarkupMessageSupport);
    }

    private static bool EqualShape_29ddb33ca2a0e609(DiagnosticData a, DiagnosticData b)
    {
        return true;
    }

    private static bool EqualShape_1ca005e11eb3df3a(DiagnosticFlakeLogLevel a, DiagnosticFlakeLogLevel b)
    {
        return EqualityComparer<DiagnosticFlakeLogLevel>.Default.Equals(a, b);
    }

    private static bool EqualShape_c7978f199a414fbd(DiagnosticOptions a, DiagnosticOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Identifier, b.Identifier) && EqualShape_b760f44fa5965c24(a.InterFileDependencies, b.InterFileDependencies) && EqualShape_b760f44fa5965c24(a.WorkspaceDiagnostics, b.WorkspaceDiagnostics);
    }

    private static bool EqualShape_703869dd83e88e30(DiagnosticOptionsOrRegistrationOptions a, DiagnosticOptionsOrRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_55e07a8cec82c655(a.Options, b.Options) && EqualShape_40ecbb6a23243047(a.RegistrationOptions, b.RegistrationOptions);
    }

    private static bool EqualShape_a14be8cd84f4acb8(DiagnosticRegistrationOptions a, DiagnosticRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Identifier, b.Identifier) && EqualShape_b760f44fa5965c24(a.InterFileDependencies, b.InterFileDependencies) && EqualShape_b760f44fa5965c24(a.WorkspaceDiagnostics, b.WorkspaceDiagnostics) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_730df2a39dbfb7da(DiagnosticRelatedInformation a, DiagnosticRelatedInformation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_15b61974b2707a7b(a.Location, b.Location) && EqualShape_473287f8298dba71(a.Message, b.Message);
    }

    private static bool EqualShape_aefa91d53756e143(DiagnosticServerCancellationData a, DiagnosticServerCancellationData b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.RetriggerRequest, b.RetriggerRequest);
    }

    private static bool EqualShape_bab4fe1f8b21f9fa(DiagnosticSeverity a, DiagnosticSeverity b)
    {
        return EqualityComparer<DiagnosticSeverity>.Default.Equals(a, b);
    }

    private static bool EqualShape_1a56dbf398d99d2f(DiagnosticTag a, DiagnosticTag b)
    {
        return EqualityComparer<DiagnosticTag>.Default.Equals(a, b);
    }

    private static bool EqualShape_22994f812002fe3c(DiagnosticWorkspaceClientCapabilities a, DiagnosticWorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_1ace992726f49b52(DiagnosticsCapabilities a, DiagnosticsCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RelatedInformation, b.RelatedInformation) && EqualShape_01178517db2f6abb(a.TagSupport, b.TagSupport) && EqualShape_31f02903a9b7a4aa(a.CodeDescriptionSupport, b.CodeDescriptionSupport) && EqualShape_31f02903a9b7a4aa(a.DataSupport, b.DataSupport);
    }

    private static bool EqualShape_7b4c2bd9272ffb37(DidChangeConfigurationClientCapabilities a, DidChangeConfigurationClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_7f22677bab49bf26(DidChangeConfigurationParams a, DidChangeConfigurationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d6a7cd2a7371b1a1(a.Settings, b.Settings);
    }

    private static bool EqualShape_15c1f1ed9bdde479(DidChangeConfigurationRegistrationOptions a, DidChangeConfigurationRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_da890d6dcb4d2c8c(a.Section, b.Section);
    }

    private static bool EqualShape_0c6ffd2fc1fd5265(DidChangeTextDocumentParams a, DidChangeTextDocumentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_bfe1d574dff86508(a.TextDocument, b.TextDocument) && EqualShape_f4ba3b7d663ee20d(a.ContentChanges, b.ContentChanges);
    }

    private static bool EqualShape_5b8e42fb55c703ff(DidChangeWatchedFilesClientCapabilities a, DidChangeWatchedFilesClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.RelativePatternSupport, b.RelativePatternSupport);
    }

    private static bool EqualShape_7a687231fe680441(DidChangeWatchedFilesParams a, DidChangeWatchedFilesParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76b4ca7f821c8fc5(a.Changes, b.Changes);
    }

    private static bool EqualShape_9a8637149e2b0c93(DidChangeWatchedFilesRegistrationOptions a, DidChangeWatchedFilesRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_34e5d5df1af6102f(a.Watchers, b.Watchers);
    }

    private static bool EqualShape_3814f51204c3cfe2(DidChangeWorkspaceFoldersParams a, DidChangeWorkspaceFoldersParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_88e5a1337985c61a(a.Event, b.Event);
    }

    private static bool EqualShape_a93e90edfeeab584(DidCloseTextDocumentParams a, DidCloseTextDocumentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_e938ee27d893dd4c(DidOpenTextDocumentParams a, DidOpenTextDocumentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0f9333e138040d4c(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_30e8b3b731a2add1(DidSaveTextDocumentParams a, DidSaveTextDocumentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_3e89801b52bf7f98(a.Text, b.Text);
    }

    private static bool EqualShape_eb60b030a70a7451(DocumentColorClientCapabilities a, DocumentColorClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_522a26e4dbe6345c(DocumentColorOptions a, DocumentColorOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_cbb3073d36672b63(DocumentColorParams a, DocumentColorParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_b2588d25e0144d45(DocumentColorRegistrationOptions a, DocumentColorRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_7548490d28d56df8(DocumentDiagnosticParams a, DocumentDiagnosticParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_3e89801b52bf7f98(a.Identifier, b.Identifier) && EqualShape_3e89801b52bf7f98(a.PreviousResultId, b.PreviousResultId);
    }

    private static bool EqualShape_b8295814ec2d845f(DocumentDiagnosticReportKind a, DocumentDiagnosticReportKind b)
    {
        return EqualityComparer<DocumentDiagnosticReportKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_9e742979ea06ee1e(DocumentDiagnosticReportPartialResult a, DocumentDiagnosticReportPartialResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_65e0d82b4a75d616(a.RelatedDocuments, b.RelatedDocuments);
    }

    private static bool EqualShape_cce2bbd91d77be98(DocumentFormattingClientCapabilities a, DocumentFormattingClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_e984db9d3215a45f(DocumentFormattingOptions a, DocumentFormattingOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_d3c0b448bcf8e371(DocumentFormattingParams a, DocumentFormattingParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_3755655439c8ef4b(a.Options, b.Options);
    }

    private static bool EqualShape_62ee47a157b066a5(DocumentFormattingRegistrationOptions a, DocumentFormattingRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_186bd9e070f0cc76(DocumentHighlight a, DocumentHighlight b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_891c53554db94d15(a.Kind, b.Kind);
    }

    private static bool EqualShape_3ea7538c7c7b4ca6(DocumentHighlightClientCapabilities a, DocumentHighlightClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_5f2a94526ab2d089(DocumentHighlightKind a, DocumentHighlightKind b)
    {
        return EqualityComparer<DocumentHighlightKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_5fa893e750b616b3(DocumentHighlightOptions a, DocumentHighlightOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_0123e4aa1b008823(DocumentHighlightParams a, DocumentHighlightParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_0c38eeec102fae46(DocumentHighlightRegistrationOptions a, DocumentHighlightRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_de9824a43cc27a46(DocumentHighlightsOrNull a, DocumentHighlightsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e51c5a913937c7ce(a.DocumentHighlights, b.DocumentHighlights);
    }

    private static bool EqualShape_25f2aaad3d4056b4(DocumentLink a, DocumentLink b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_5b3003eb17f67429(a.Target, b.Target) && EqualShape_3e89801b52bf7f98(a.Tooltip, b.Tooltip) && EqualShape_c8bba042661f66cd(a.Data, b.Data);
    }

    private static bool EqualShape_e06be71a80ee1ec3(DocumentLinkClientCapabilities a, DocumentLinkClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.TooltipSupport, b.TooltipSupport);
    }

    private static bool EqualShape_4c9581f87cdeb817(DocumentLinkData a, DocumentLinkData b)
    {
        return true;
    }

    private static bool EqualShape_529f3bd836f0b9d8(DocumentLinkOptions a, DocumentLinkOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_b94d0f031a384478(DocumentLinkParams a, DocumentLinkParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_92723a302d3136eb(DocumentLinkRegistrationOptions a, DocumentLinkRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_1589962d250e4fa8(DocumentLinksOrNull a, DocumentLinksOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1f19d74e1e78ea50(a.DocumentLinks, b.DocumentLinks);
    }

    private static bool EqualShape_9ba4d6b0690b3f7f(DocumentOnTypeFormattingClientCapabilities a, DocumentOnTypeFormattingClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_0fa03db0935ecc4d(DocumentOnTypeFormattingOptions a, DocumentOnTypeFormattingOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.FirstTriggerCharacter, b.FirstTriggerCharacter) && EqualShape_8941972aede71346(a.MoreTriggerCharacter, b.MoreTriggerCharacter);
    }

    private static bool EqualShape_973929f55766dcd7(DocumentOnTypeFormattingParams a, DocumentOnTypeFormattingParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_473287f8298dba71(a.Ch, b.Ch) && EqualShape_3755655439c8ef4b(a.Options, b.Options);
    }

    private static bool EqualShape_3e4d7072185bf69d(DocumentOnTypeFormattingRegistrationOptions a, DocumentOnTypeFormattingRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_473287f8298dba71(a.FirstTriggerCharacter, b.FirstTriggerCharacter) && EqualShape_8941972aede71346(a.MoreTriggerCharacter, b.MoreTriggerCharacter);
    }

    private static bool EqualShape_da985f3aad64b092(DocumentRangeFormattingClientCapabilities a, DocumentRangeFormattingClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.RangesSupport, b.RangesSupport);
    }

    private static bool EqualShape_ff263dfb0755ad33(DocumentRangeFormattingOptions a, DocumentRangeFormattingOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.RangesSupport, b.RangesSupport);
    }

    private static bool EqualShape_d6bb17f42468c458(DocumentRangeFormattingParams a, DocumentRangeFormattingParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_3755655439c8ef4b(a.Options, b.Options);
    }

    private static bool EqualShape_e4397d5196cfce64(DocumentRangeFormattingRegistrationOptions a, DocumentRangeFormattingRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.RangesSupport, b.RangesSupport);
    }

    private static bool EqualShape_ebefe18aa77c282b(DocumentRangesFormattingParams a, DocumentRangesFormattingParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_395c34a167937420(a.Ranges, b.Ranges) && EqualShape_3755655439c8ef4b(a.Options, b.Options);
    }

    private static bool EqualShape_c7819f785729d94c(DocumentSelectorOrNull a, DocumentSelectorOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_521fd5c1777fdd96(a.DocumentSelector, b.DocumentSelector);
    }

    private static bool EqualShape_7a5e3c27bb3a5284(DocumentSymbol a, DocumentSymbol b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_3e89801b52bf7f98(a.Detail, b.Detail) && EqualShape_80525ce65b460521(a.Kind, b.Kind) && EqualShape_2a7c50a738423fd9(a.Tags, b.Tags) && EqualShape_31f02903a9b7a4aa(a.Deprecated, b.Deprecated) && EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_5de74a81b192304c(a.SelectionRange, b.SelectionRange) && EqualShape_135e6f0abfdfb63b(a.Children, b.Children);
    }

    private static bool EqualShape_7ca8061eaafe80fa(DocumentSymbolClientCapabilities a, DocumentSymbolClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_ef83c3195ef91327(a.SymbolKind, b.SymbolKind) && EqualShape_31f02903a9b7a4aa(a.HierarchicalDocumentSymbolSupport, b.HierarchicalDocumentSymbolSupport) && EqualShape_56b6c5f1c4a81226(a.TagSupport, b.TagSupport) && EqualShape_31f02903a9b7a4aa(a.LabelSupport, b.LabelSupport);
    }

    private static bool EqualShape_a1b6a2290a294048(DocumentSymbolOptions a, DocumentSymbolOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Label, b.Label);
    }

    private static bool EqualShape_2be3ae8722c9c7c4(DocumentSymbolParams a, DocumentSymbolParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_40bf243208068748(DocumentSymbolRegistrationOptions a, DocumentSymbolRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Label, b.Label);
    }

    private static bool EqualShape_b2094f714cc94ba3(DocumentUri a, DocumentUri b)
    {
        return EqualityComparer<DocumentUri>.Default.Equals(a, b);
    }

    private static bool EqualShape_b23e05e41f22b21f(DocumentUriOrNull a, DocumentUriOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ab86d3ea50f3859e(a.DocumentUri, b.DocumentUri);
    }

    private static bool EqualShape_909237c23c611f07(EditRangeWithInsertReplace a, EditRangeWithInsertReplace b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.Insert, b.Insert) && EqualShape_5de74a81b192304c(a.Replace, b.Replace);
    }

    private static bool EqualShape_b1b889e0a6a2ea91(ErrorCode a, ErrorCode b)
    {
        return EqualityComparer<ErrorCode>.Default.Equals(a, b);
    }

    private static bool EqualShape_6d21070f992c17fb(ExecuteCommandClientCapabilities a, ExecuteCommandClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_03d1076067933682(ExecuteCommandOptions a, ExecuteCommandOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_4af101177fe720bf(a.Commands, b.Commands);
    }

    private static bool EqualShape_167e0d23d2d2a94b(ExecuteCommandParams a, ExecuteCommandParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_473287f8298dba71(a.Command, b.Command) && EqualShape_576f0a462b844c7d(a.Arguments, b.Arguments);
    }

    private static bool EqualShape_e5b0616c18b5df9a(ExecuteCommandRegistrationOptions a, ExecuteCommandRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_4af101177fe720bf(a.Commands, b.Commands);
    }

    private static bool EqualShape_450a644bbefb758d(ExperimentalClientCapabilities a, ExperimentalClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.HoverVerbosityLevel, b.HoverVerbosityLevel);
    }

    private static bool EqualShape_8328a3a42170a833(ExperimentalServerCapabilities a, ExperimentalServerCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.CustomSourceDefinitionProvider, b.CustomSourceDefinitionProvider) && EqualShape_31f02903a9b7a4aa(a.CustomMultiDocumentHighlightProvider, b.CustomMultiDocumentHighlightProvider);
    }

    private static bool EqualShape_484e4404d3769d00(FailureHandlingKind a, FailureHandlingKind b)
    {
        return EqualityComparer<FailureHandlingKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_1d3058865d82fb8b(FileChangeType a, FileChangeType b)
    {
        return EqualityComparer<FileChangeType>.Default.Equals(a, b);
    }

    private static bool EqualShape_dd7287de677959f4(FileCreate a, FileCreate b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri);
    }

    private static bool EqualShape_4cfd293b1972bff8(FileDelete a, FileDelete b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri);
    }

    private static bool EqualShape_a125c27075ddc783(FileEvent a, FileEvent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_1d3058865d82fb8b(a.Type, b.Type);
    }

    private static bool EqualShape_9de47d600efc33f1(FileOperationClientCapabilities a, FileOperationClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.DidCreate, b.DidCreate) && EqualShape_31f02903a9b7a4aa(a.WillCreate, b.WillCreate) && EqualShape_31f02903a9b7a4aa(a.DidRename, b.DidRename) && EqualShape_31f02903a9b7a4aa(a.WillRename, b.WillRename) && EqualShape_31f02903a9b7a4aa(a.DidDelete, b.DidDelete) && EqualShape_31f02903a9b7a4aa(a.WillDelete, b.WillDelete);
    }

    private static bool EqualShape_01085faf2ca32a63(FileOperationFilter a, FileOperationFilter b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Scheme, b.Scheme) && EqualShape_d914c67f0dffd8de(a.Pattern, b.Pattern);
    }

    private static bool EqualShape_518c29b60657172f(FileOperationOptions a, FileOperationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95c7ba3a24076324(a.DidCreate, b.DidCreate) && EqualShape_95c7ba3a24076324(a.WillCreate, b.WillCreate) && EqualShape_95c7ba3a24076324(a.DidRename, b.DidRename) && EqualShape_95c7ba3a24076324(a.WillRename, b.WillRename) && EqualShape_95c7ba3a24076324(a.DidDelete, b.DidDelete) && EqualShape_95c7ba3a24076324(a.WillDelete, b.WillDelete);
    }

    private static bool EqualShape_876111df355462c4(FileOperationPattern a, FileOperationPattern b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Glob, b.Glob) && EqualShape_6b2ece5c46641318(a.Matches, b.Matches) && EqualShape_bfdb692544efd922(a.Options, b.Options);
    }

    private static bool EqualShape_c9ac6b08cd0fcaec(FileOperationPatternKind a, FileOperationPatternKind b)
    {
        return EqualityComparer<FileOperationPatternKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_77897374dc003098(FileOperationPatternOptions a, FileOperationPatternOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.IgnoreCase, b.IgnoreCase);
    }

    private static bool EqualShape_1cbbf99920573dca(FileOperationRegistrationOptions a, FileOperationRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_836ac2ca4c2fab5f(a.Filters, b.Filters);
    }

    private static bool EqualShape_132f8fe961259b8b(FileRename a, FileRename b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.OldUri, b.OldUri) && EqualShape_b2094f714cc94ba3(a.NewUri, b.NewUri);
    }

    private static bool EqualShape_8546807fc7830176(FileSystemWatcher a, FileSystemWatcher b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_555c0b23fa28c7a4(a.GlobPattern, b.GlobPattern) && EqualShape_86af5e542d0d8a54(a.Kind, b.Kind);
    }

    private static bool EqualShape_66fbdf0fd1929f3b(FoldingRange a, FoldingRange b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1c06d81634ed9a2e(a.StartLine, b.StartLine) && EqualShape_3f8175cc2ff18f25(a.StartCharacter, b.StartCharacter) && EqualShape_1c06d81634ed9a2e(a.EndLine, b.EndLine) && EqualShape_3f8175cc2ff18f25(a.EndCharacter, b.EndCharacter) && EqualShape_5b82af5067892e83(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.CollapsedText, b.CollapsedText);
    }

    private static bool EqualShape_016dd67866f93e94(FoldingRangeClientCapabilities a, FoldingRangeClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_3f8175cc2ff18f25(a.RangeLimit, b.RangeLimit) && EqualShape_31f02903a9b7a4aa(a.LineFoldingOnly, b.LineFoldingOnly) && EqualShape_b2a2fec66de9fc74(a.FoldingRangeKind, b.FoldingRangeKind) && EqualShape_c7202684a50e9e09(a.FoldingRange, b.FoldingRange);
    }

    private static bool EqualShape_9bed5894ab8b426e(FoldingRangeKind a, FoldingRangeKind b)
    {
        return EqualityComparer<FoldingRangeKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_563033aa92b4afe3(FoldingRangeOptions a, FoldingRangeOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_e4340b98efb17abe(FoldingRangeParams a, FoldingRangeParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_2465fd48699a0885(FoldingRangeRegistrationOptions a, FoldingRangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_fb7ea6a0b64b120e(FoldingRangeWorkspaceClientCapabilities a, FoldingRangeWorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_c0e2536c958d686b(FoldingRangesOrNull a, FoldingRangesOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c6718f7e5bb64a46(a.FoldingRanges, b.FoldingRanges);
    }

    private static bool EqualShape_4441aa46c6968d1e(FormattingOptions a, FormattingOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1c06d81634ed9a2e(a.TabSize, b.TabSize) && EqualShape_b760f44fa5965c24(a.InsertSpaces, b.InsertSpaces) && EqualShape_31f02903a9b7a4aa(a.TrimTrailingWhitespace, b.TrimTrailingWhitespace) && EqualShape_31f02903a9b7a4aa(a.InsertFinalNewline, b.InsertFinalNewline) && EqualShape_31f02903a9b7a4aa(a.TrimFinalNewlines, b.TrimFinalNewlines);
    }

    private static bool EqualShape_7a31e4ad68de1c48(FullDocumentDiagnosticReport a, FullDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4a4f64afaada24e4(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.ResultId, b.ResultId) && EqualShape_ce18911ddb414acc(a.Items, b.Items);
    }

    private static bool EqualShape_122188e5e315b8c0(FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport a, FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_51d09426b5861162(a.FullDocumentDiagnosticReport, b.FullDocumentDiagnosticReport) && EqualShape_3af2e2ea9ab7472d(a.UnchangedDocumentDiagnosticReport, b.UnchangedDocumentDiagnosticReport);
    }

    private static bool EqualShape_337bdf3fdd9371a3(GeneralClientCapabilities a, GeneralClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d7072dfcd6925eea(a.StaleRequestSupport, b.StaleRequestSupport) && EqualShape_c573dc5e5f151c1e(a.RegularExpressions, b.RegularExpressions) && EqualShape_cda55cf0c5a75507(a.Markdown, b.Markdown) && EqualShape_325bd644d4160e55(a.PositionEncodings, b.PositionEncodings);
    }

    private static bool EqualShape_c76ae86ad3014c85(Hover a, Hover b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_20ab4652e2622e44(a.Contents, b.Contents) && EqualShape_c15fa81b51c6dda0(a.@Range, b.@Range) && EqualShape_b760f44fa5965c24(a.CanIncreaseVerbosity, b.CanIncreaseVerbosity) && EqualShape_2147172615c487bd(a.VSRawContent, b.VSRawContent);
    }

    private static bool EqualShape_c3adf798fbc806c6(HoverClientCapabilities a, HoverClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_95df085df9457b35(a.ContentFormat, b.ContentFormat);
    }

    private static bool EqualShape_5e5dc1cc6e1c3523(HoverOptions a, HoverOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_eed3cf3342b20f8c(HoverOrNull a, HoverOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d74932d04207d5c2(a.Hover, b.Hover);
    }

    private static bool EqualShape_c1b679afaeb1b5ec(HoverParams a, HoverParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c2c7219dbdd4eab1(a.VerbosityLevel, b.VerbosityLevel);
    }

    private static bool EqualShape_c42e31a1d8d0c570(HoverRegistrationOptions a, HoverRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_505ca50db8c7eb85(ImplementationClientCapabilities a, ImplementationClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_6a59e8e8c3547e2d(ImplementationOptions a, ImplementationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_3126943fdda20947(ImplementationParams a, ImplementationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_999302bf98018b4a(ImplementationRegistrationOptions a, ImplementationRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_d39772dfff646e6d(ImportKind a, ImportKind b)
    {
        return EqualityComparer<ImportKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_44f48a6fd5281753(InferredProjectContentMapperContribution a, InferredProjectContentMapperContribution b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_94061b27224c72c8(a.Options, b.Options) && EqualShape_bcc8effb9176a1cf(a.Manifest, b.Manifest);
    }

    private static bool EqualShape_d6291485da709fea(InitializationOptions a, InitializationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DisablePushDiagnostics, b.DisablePushDiagnostics) && EqualShape_3e89801b52bf7f98(a.CodeLensShowLocationsCommandName, b.CodeLensShowLocationsCommandName) && EqualShape_7f4d449efe399ec7(a.UserPreferences, b.UserPreferences) && EqualShape_31f02903a9b7a4aa(a.EnableTelemetry, b.EnableTelemetry) && EqualShape_1f5a1b49e624f6ed(a.LogVerbosity, b.LogVerbosity) && EqualShape_31f02903a9b7a4aa(a.RunExternalCode, b.RunExternalCode) && EqualShape_068ed04f8cfbd578(a.TrackFlakyDiagnostics, b.TrackFlakyDiagnostics);
    }

    private static bool EqualShape_36ae92715ecba9c7(InitializationOptionsOrNull a, InitializationOptionsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a5466bf851beb93d(a.InitializationOptions, b.InitializationOptions);
    }

    private static bool EqualShape_0f19f7201ba374f8(InitializeAPISessionParams a, InitializeAPISessionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Pipe, b.Pipe);
    }

    private static bool EqualShape_d5842f43f86d28d8(InitializeAPISessionResult a, InitializeAPISessionResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.SessionId, b.SessionId) && EqualShape_473287f8298dba71(a.Pipe, b.Pipe);
    }

    private static bool EqualShape_d9e9313e42a0d6c0(InitializeError a, InitializeError b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.Retry, b.Retry);
    }

    private static bool EqualShape_6cabbf134c6b5195(InitializeParams a, InitializeParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_17d3cfba51c6c525(a.ProcessId, b.ProcessId) && EqualShape_98be1a1b6cdb3d2e(a.ClientInfo, b.ClientInfo) && EqualShape_3e89801b52bf7f98(a.Locale, b.Locale) && EqualShape_e5ac9928546536d2(a.RootPath, b.RootPath) && EqualShape_b23e05e41f22b21f(a.RootUri, b.RootUri) && EqualShape_29c18c60ff8bc658(a.Capabilities, b.Capabilities) && EqualShape_6ebb34a7d02f2ddb(a.InitializationOptions, b.InitializationOptions) && EqualShape_1be8e25859d6d163(a.Trace, b.Trace) && EqualShape_f62df71aba721e59(a.WorkspaceFolders, b.WorkspaceFolders);
    }

    private static bool EqualShape_ff2c54bb0124e1e5(InitializeResult a, InitializeResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a8f57d2338f06506(a.Capabilities, b.Capabilities) && EqualShape_389fa1d2ea1e79ff(a.ServerInfo, b.ServerInfo);
    }

    private static bool EqualShape_77f425e16948cb7b(InitializedParams a, InitializedParams b)
    {
        return true;
    }

    private static bool EqualShape_551be06dac20fdd0(InlayHint a, InlayHint b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_be643dd009a1ea80(a.Label, b.Label) && EqualShape_07d32d25cf0ad06f(a.Kind, b.Kind) && EqualShape_965c47d3497332b4(a.TextEdits, b.TextEdits) && EqualShape_818def5a213356d9(a.Tooltip, b.Tooltip) && EqualShape_31f02903a9b7a4aa(a.PaddingLeft, b.PaddingLeft) && EqualShape_31f02903a9b7a4aa(a.PaddingRight, b.PaddingRight) && EqualShape_e1dfa6c327dd6c3c(a.Data, b.Data);
    }

    private static bool EqualShape_860bcfccc3d275ce(InlayHintClientCapabilities a, InlayHintClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b51791d12c689d69(a.ResolveSupport, b.ResolveSupport);
    }

    private static bool EqualShape_ec25b7a7c027131f(InlayHintData a, InlayHintData b)
    {
        return true;
    }

    private static bool EqualShape_53926c928508e1cb(InlayHintKind a, InlayHintKind b)
    {
        return EqualityComparer<InlayHintKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_83ad5d75a7336f5c(InlayHintLabelPart a, InlayHintLabelPart b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Value, b.Value) && EqualShape_818def5a213356d9(a.Tooltip, b.Tooltip) && EqualShape_f9ef6e0f3fa4927b(a.Location, b.Location) && EqualShape_a6c992bdda521682(a.Command, b.Command);
    }

    private static bool EqualShape_e7413e6883917923(InlayHintOptions a, InlayHintOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_3e20fc5d07945082(InlayHintParams a, InlayHintParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_5de74a81b192304c(a.@Range, b.@Range);
    }

    private static bool EqualShape_5736a71dddfa55b8(InlayHintRegistrationOptions a, InlayHintRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider) && EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_48084566f2cdcaae(InlayHintWorkspaceClientCapabilities a, InlayHintWorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_e59fdbb1e7ba4144(InlayHintsOrNull a, InlayHintsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_910e533db5076de4(a.InlayHints, b.InlayHints);
    }

    private static bool EqualShape_e7de3ce110b1d167(InlineCompletionClientCapabilities a, InlineCompletionClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_22637a4522270110(InlineCompletionContext a, InlineCompletionContext b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_163ef7bab0fae34d(a.TriggerKind, b.TriggerKind) && EqualShape_c896f039f28c0b09(a.SelectedCompletionInfo, b.SelectedCompletionInfo);
    }

    private static bool EqualShape_5f63db4527f32b9c(InlineCompletionItem a, InlineCompletionItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7485e0c10f1b758e(a.InsertText, b.InsertText) && EqualShape_3e89801b52bf7f98(a.FilterText, b.FilterText) && EqualShape_c15fa81b51c6dda0(a.@Range, b.@Range) && EqualShape_a6c992bdda521682(a.Command, b.Command);
    }

    private static bool EqualShape_433b2e6a3a3c27cc(InlineCompletionList a, InlineCompletionList b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2ac7ed000ec4e5b2(a.Items, b.Items);
    }

    private static bool EqualShape_1970c1b9314d0672(InlineCompletionListOrItemsOrNull a, InlineCompletionListOrItemsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e7db5a8938276dbb(a.List, b.List) && EqualShape_08d276aa8b28bb2f(a.Items, b.Items);
    }

    private static bool EqualShape_571267fe163915c1(InlineCompletionOptions a, InlineCompletionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_d9c2c29e86a56320(InlineCompletionParams a, InlineCompletionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_12114617eb6b57ab(a.Context, b.Context);
    }

    private static bool EqualShape_e1b6c72c88dbb66d(InlineCompletionRegistrationOptions a, InlineCompletionRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_163ef7bab0fae34d(InlineCompletionTriggerKind a, InlineCompletionTriggerKind b)
    {
        return EqualityComparer<InlineCompletionTriggerKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_9422bd44de4450a3(InlineValueClientCapabilities a, InlineValueClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_0db2a1b74408177b(InlineValueContext a, InlineValueContext b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_05c312945775207c(a.FrameId, b.FrameId) && EqualShape_5de74a81b192304c(a.StoppedLocation, b.StoppedLocation);
    }

    private static bool EqualShape_5ba15e00f3989e84(InlineValueEvaluatableExpression a, InlineValueEvaluatableExpression b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_3e89801b52bf7f98(a.Expression, b.Expression);
    }

    private static bool EqualShape_4227afe828e4c487(InlineValueOptions a, InlineValueOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_c9023b2dc10087cf(InlineValueParams a, InlineValueParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_cf67b383aa7395a7(a.Context, b.Context);
    }

    private static bool EqualShape_5852b2dfb2cf7003(InlineValueRegistrationOptions a, InlineValueRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_e1fd84adea44b647(InlineValueText a, InlineValueText b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_473287f8298dba71(a.Text, b.Text);
    }

    private static bool EqualShape_9b1858affa016849(InlineValueTextOrVariableLookupOrEvaluatableExpression a, InlineValueTextOrVariableLookupOrEvaluatableExpression b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9d9ba902d757f771(a.Text, b.Text) && EqualShape_7a0d9411f2d63a63(a.VariableLookup, b.VariableLookup) && EqualShape_a4ea07c6da53809d(a.EvaluatableExpression, b.EvaluatableExpression);
    }

    private static bool EqualShape_2dc5fda12fce3a9d(InlineValueVariableLookup a, InlineValueVariableLookup b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_3e89801b52bf7f98(a.VariableName, b.VariableName) && EqualShape_b760f44fa5965c24(a.CaseSensitiveLookup, b.CaseSensitiveLookup);
    }

    private static bool EqualShape_caa283e237c108c2(InlineValueWorkspaceClientCapabilities a, InlineValueWorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_8992dae46a973c42(InlineValuesOrNull a, InlineValuesOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e9c2bcf9dee60523(a.InlineValues, b.InlineValues);
    }

    private static bool EqualShape_79b7a3fa81c6612e(InsertReplaceEdit a, InsertReplaceEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.NewText, b.NewText) && EqualShape_5de74a81b192304c(a.Insert, b.Insert) && EqualShape_5de74a81b192304c(a.Replace, b.Replace);
    }

    private static bool EqualShape_0125b1afb99b0757(InsertTextFormat a, InsertTextFormat b)
    {
        return EqualityComparer<InsertTextFormat>.Default.Equals(a, b);
    }

    private static bool EqualShape_6cddb8f12d636f30(InsertTextMode a, InsertTextMode b)
    {
        return EqualityComparer<InsertTextMode>.Default.Equals(a, b);
    }

    private static bool EqualShape_17d3cfba51c6c525(IntegerOrNull a, IntegerOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c2c7219dbdd4eab1(a.@Integer, b.@Integer);
    }

    private static bool EqualShape_95e25e81cd99bf99(IntegerOrString a, IntegerOrString b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c2c7219dbdd4eab1(a.@Integer, b.@Integer) && EqualShape_3e89801b52bf7f98(a.@String, b.@String);
    }

    private static bool EqualShape_a0f960cf093cfaa2(LSPAnyOrNull a, LSPAnyOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7f4d449efe399ec7(a.LSPAny, b.LSPAny);
    }

    private static bool EqualShape_74d0ea0a98141b4c(LanguageKind a, LanguageKind b)
    {
        return EqualityComparer<LanguageKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_1d4bf231f870ced7(LinkedEditingRangeClientCapabilities a, LinkedEditingRangeClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_bf2919c7ffa7243d(LinkedEditingRangeOptions a, LinkedEditingRangeOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_7aa30078fed4b566(LinkedEditingRangeParams a, LinkedEditingRangeParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken);
    }

    private static bool EqualShape_89439e1c9f8204ac(LinkedEditingRangeRegistrationOptions a, LinkedEditingRangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_42b657f1a5deeca0(LinkedEditingRanges a, LinkedEditingRanges b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_395c34a167937420(a.Ranges, b.Ranges) && EqualShape_3e89801b52bf7f98(a.WordPattern, b.WordPattern);
    }

    private static bool EqualShape_f076e01264070196(LinkedEditingRangesOrNull a, LinkedEditingRangesOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d089671828ebe059(a.LinkedEditingRanges, b.LinkedEditingRanges);
    }

    private static bool EqualShape_15b61974b2707a7b(Location a, Location b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_5de74a81b192304c(a.@Range, b.@Range);
    }

    private static bool EqualShape_bdb924567ef98797(LocationLink a, LocationLink b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c15fa81b51c6dda0(a.OriginSelectionRange, b.OriginSelectionRange) && EqualShape_b2094f714cc94ba3(a.TargetUri, b.TargetUri) && EqualShape_5de74a81b192304c(a.TargetRange, b.TargetRange) && EqualShape_5de74a81b192304c(a.TargetSelectionRange, b.TargetSelectionRange);
    }

    private static bool EqualShape_fb622e0b8d2bf184(LocationOrLocationUriOnly a, LocationOrLocationUriOnly b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f9ef6e0f3fa4927b(a.Location, b.Location) && EqualShape_6fd44c23af14c27f(a.LocationUriOnly, b.LocationUriOnly);
    }

    private static bool EqualShape_224e82bb96ae8cfe(LocationOrLocationsOrDeclarationLinksOrNull a, LocationOrLocationsOrDeclarationLinksOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f9ef6e0f3fa4927b(a.Location, b.Location) && EqualShape_b65d4719a95ca244(a.Locations, b.Locations) && EqualShape_778f55dd7079fa7d(a.DeclarationLinks, b.DeclarationLinks);
    }

    private static bool EqualShape_e65bade6fa339317(LocationOrLocationsOrDefinitionLinksOrNull a, LocationOrLocationsOrDefinitionLinksOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f9ef6e0f3fa4927b(a.Location, b.Location) && EqualShape_b65d4719a95ca244(a.Locations, b.Locations) && EqualShape_778f55dd7079fa7d(a.DefinitionLinks, b.DefinitionLinks);
    }

    private static bool EqualShape_3bee336672c6633c(LocationUriOnly a, LocationUriOnly b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri);
    }

    private static bool EqualShape_e255434a1eda226b(LocationsOrNull a, LocationsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b65d4719a95ca244(a.Locations, b.Locations);
    }

    private static bool EqualShape_8e5a686c4d7af637(LogMessageParams a, LogMessageParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6ff9dd7ca34940c3(a.Type, b.Type) && EqualShape_473287f8298dba71(a.Message, b.Message);
    }

    private static bool EqualShape_561978800a186b54(LogTraceParams a, LogTraceParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Message, b.Message) && EqualShape_3e89801b52bf7f98(a.Verbose, b.Verbose);
    }

    private static bool EqualShape_061133d05f438d15(LogVerbosity a, LogVerbosity b)
    {
        return EqualityComparer<LogVerbosity>.Default.Equals(a, b);
    }

    private static bool EqualShape_5754eb1e7d7a8a87(MarkdownClientCapabilities a, MarkdownClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Parser, b.Parser) && EqualShape_3e89801b52bf7f98(a.Version, b.Version) && EqualShape_8941972aede71346(a.AllowedTags, b.AllowedTags);
    }

    private static bool EqualShape_02b572e84de66f2a(MarkedStringWithLanguage a, MarkedStringWithLanguage b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Language, b.Language) && EqualShape_473287f8298dba71(a.Value, b.Value);
    }

    private static bool EqualShape_ec8d99ae989825b7(MarkupContent a, MarkupContent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2ecdda95186829dd(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.Value, b.Value);
    }

    private static bool EqualShape_20ab4652e2622e44(MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings a, MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a09c50c289cdf175(a.MarkupContent, b.MarkupContent) && EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_c3b0d76093e2f53f(a.MarkedStringWithLanguage, b.MarkedStringWithLanguage) && EqualShape_e555e7503571f3c2(a.MarkedStrings, b.MarkedStrings);
    }

    private static bool EqualShape_2ecdda95186829dd(MarkupKind a, MarkupKind b)
    {
        return EqualityComparer<MarkupKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_0627c9b9dd315342(MessageActionItem a, MessageActionItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Title, b.Title);
    }

    private static bool EqualShape_5853b8e3ac90f099(MessageActionItemOrNull a, MessageActionItemOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_505239fc3470cbd5(a.MessageActionItem, b.MessageActionItem);
    }

    private static bool EqualShape_6ff9dd7ca34940c3(MessageType a, MessageType b)
    {
        return EqualityComparer<MessageType>.Default.Equals(a, b);
    }

    private static bool EqualShape_52a0f9b65b278850(Method a, Method b)
    {
        return EqualityComparer<Method>.Default.Equals(a, b);
    }

    private static bool EqualShape_c59fb88a060d1bc0(Moniker a, Moniker b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Scheme, b.Scheme) && EqualShape_473287f8298dba71(a.Identifier, b.Identifier) && EqualShape_52a1f306704bcdf2(a.Unique, b.Unique) && EqualShape_01469ed2e83bad7e(a.Kind, b.Kind);
    }

    private static bool EqualShape_6294802e598ca5a1(MonikerClientCapabilities a, MonikerClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_7d2dfd2063c3273d(MonikerKind a, MonikerKind b)
    {
        return EqualityComparer<MonikerKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_6a54ddd49258de2c(MonikerOptions a, MonikerOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_dce655840bd2b665(MonikerParams a, MonikerParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_01d385a3539e6586(MonikerRegistrationOptions a, MonikerRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_0d916739fbbbc286(MonikersOrNull a, MonikersOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a84ffd8b4dd738c6(a.Monikers, b.Monikers);
    }

    private static bool EqualShape_01d7598b5a7fa2ba(MultiDocumentHighlight a, MultiDocumentHighlight b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_671d1014e749357c(a.Highlights, b.Highlights);
    }

    private static bool EqualShape_d34dfd9b81abfe93(MultiDocumentHighlightParams a, MultiDocumentHighlightParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_7789c42f95c3f703(a.FilesToSearch, b.FilesToSearch);
    }

    private static bool EqualShape_3a79319ef19a1228(MultiDocumentHighlightsOrNull a, MultiDocumentHighlightsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b009ce335e5a312f(a.MultiDocumentHighlights, b.MultiDocumentHighlights);
    }

    private static bool EqualShape_937bacb7ab352525(NoParams a, NoParams b)
    {
        return true;
    }

    private static bool EqualShape_fa1d2db62d4d952e(Null a, Null b)
    {
        return true;
    }

    private static bool EqualShape_a348a1e23de5742a(OptionalVersionedTextDocumentIdentifier a, OptionalVersionedTextDocumentIdentifier b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_17d3cfba51c6c525(a.Version, b.Version);
    }

    private static bool EqualShape_6e5531cdb6cb8530(ParameterInformation a, ParameterInformation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_9f68e715f063af80(a.Label, b.Label) && EqualShape_818def5a213356d9(a.Documentation, b.Documentation);
    }

    private static bool EqualShape_413dff5f49001606(PartialResultParams a, PartialResultParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_555c0b23fa28c7a4(PatternOrRelativePattern a, PatternOrRelativePattern b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Pattern, b.Pattern) && EqualShape_741900dcc54c0f9e(a.RelativePattern, b.RelativePattern);
    }

    private static bool EqualShape_4ba91157fee437e2(PerformanceStatsTelemetryEvent a, PerformanceStatsTelemetryEvent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_80a5c2c59f770ffa(a.EventName, b.EventName) && EqualShape_45902ec7f4da96d7(a.TelemetryPurpose, b.TelemetryPurpose) && EqualShape_6ccd8518896c2c38(a.Measurements, b.Measurements);
    }

    private static bool EqualShape_eca5393ae77d3617(PerformanceStatsTelemetryMeasurements a, PerformanceStatsTelemetryMeasurements b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6bd2a66c4467bc37(a.OpenFileCount, b.OpenFileCount) && EqualShape_6bd2a66c4467bc37(a.UptimeSeconds, b.UptimeSeconds) && EqualShape_6bd2a66c4467bc37(a.ProjectCount, b.ProjectCount) && EqualShape_6bd2a66c4467bc37(a.ConfigCount, b.ConfigCount) && EqualShape_6bd2a66c4467bc37(a.CachedDiskFileCount, b.CachedDiskFileCount) && EqualShape_6bd2a66c4467bc37(a.MemoryUsedBytes, b.MemoryUsedBytes) && EqualShape_6bd2a66c4467bc37(a.GoMemLimit, b.GoMemLimit) && EqualShape_6bd2a66c4467bc37(a.GoGCPercent, b.GoGCPercent) && EqualShape_6bd2a66c4467bc37(a.HeapGoalBytes, b.HeapGoalBytes) && EqualShape_6bd2a66c4467bc37(a.HeapLiveBytes, b.HeapLiveBytes) && EqualShape_6bd2a66c4467bc37(a.HeapObjectCount, b.HeapObjectCount) && EqualShape_6bd2a66c4467bc37(a.HeapStackBytes, b.HeapStackBytes) && EqualShape_6bd2a66c4467bc37(a.HeapReleasedBytes, b.HeapReleasedBytes) && EqualShape_6bd2a66c4467bc37(a.HeapFreeBytes, b.HeapFreeBytes) && EqualShape_6bd2a66c4467bc37(a.GcScanHeapBytes, b.GcScanHeapBytes) && EqualShape_6bd2a66c4467bc37(a.GoMaxProcs, b.GoMaxProcs) && EqualShape_6bd2a66c4467bc37(a.GoroutineCount, b.GoroutineCount) && EqualShape_6bd2a66c4467bc37(a.GcCyclesTotal, b.GcCyclesTotal) && EqualShape_6bd2a66c4467bc37(a.GcCPUSeconds, b.GcCPUSeconds) && EqualShape_6bd2a66c4467bc37(a.UserCPUSeconds, b.UserCPUSeconds) && EqualShape_6bd2a66c4467bc37(a.SystemMemTotal, b.SystemMemTotal) && EqualShape_6bd2a66c4467bc37(a.SystemMemUsed, b.SystemMemUsed) && EqualShape_6bd2a66c4467bc37(a.AutoImportProjectBucketCount, b.AutoImportProjectBucketCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportNodeModulesBucketCount, b.AutoImportNodeModulesBucketCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportUniquePackageCount, b.AutoImportUniquePackageCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportProjectExportCount, b.AutoImportProjectExportCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportNodeModulesExportCount, b.AutoImportNodeModulesExportCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportProjectFileCount, b.AutoImportProjectFileCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportNodeModulesFileCount, b.AutoImportNodeModulesFileCount) && EqualShape_6bd2a66c4467bc37(a.AutoImportNodeModulesUnfilteredBucketCount, b.AutoImportNodeModulesUnfilteredBucketCount);
    }

    private static bool EqualShape_6d031af10da7a25e(Position a, Position b)
    {
        return EqualShape_1c06d81634ed9a2e(a.Line, b.Line) && EqualShape_1c06d81634ed9a2e(a.Character, b.Character);
    }

    private static bool EqualShape_996a119923a77207(PositionEncodingKind a, PositionEncodingKind b)
    {
        return EqualityComparer<PositionEncodingKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_0939d43961a41cae(PrepareRenameDefaultBehavior a, PrepareRenameDefaultBehavior b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.DefaultBehavior, b.DefaultBehavior);
    }

    private static bool EqualShape_21a0c71ff605bf6f(PrepareRenameParams a, PrepareRenameParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken);
    }

    private static bool EqualShape_a423b1bf4090c576(PrepareRenamePlaceholder a, PrepareRenamePlaceholder b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_473287f8298dba71(a.Placeholder, b.Placeholder);
    }

    private static bool EqualShape_491ada003a0c8630(PrepareSupportDefaultBehavior a, PrepareSupportDefaultBehavior b)
    {
        return EqualityComparer<PrepareSupportDefaultBehavior>.Default.Equals(a, b);
    }

    private static bool EqualShape_2d2e58655dd72e85(PreviousResultId a, PreviousResultId b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_473287f8298dba71(a.Value, b.Value);
    }

    private static bool EqualShape_643784fd09a0a166(ProfileParams a, ProfileParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Dir, b.Dir);
    }

    private static bool EqualShape_ea1fbfb94ccf57aa(ProfileResult a, ProfileResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.File, b.File);
    }

    private static bool EqualShape_26b35649303908fb(ProgressParams a, ProgressParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95e25e81cd99bf99(a.Token, b.Token) && EqualShape_fd6812dd893b247d(a.Value, b.Value);
    }

    private static bool EqualShape_82dae73cd3c77e28(ProjectInfoParams a, ProjectInfoParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_1a1c57207754153f(ProjectInfoResult a, ProjectInfoResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.ConfigFilePath, b.ConfigFilePath);
    }

    private static bool EqualShape_f3af5d7035530534(ProjectInfoTelemetryEvent a, ProjectInfoTelemetryEvent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_33119583302dabc8(a.EventName, b.EventName) && EqualShape_45902ec7f4da96d7(a.TelemetryPurpose, b.TelemetryPurpose) && EqualShape_219dd46d9c2a4a1d(a.Properties, b.Properties) && EqualShape_9107abb7a369fc01(a.Measurements, b.Measurements);
    }

    private static bool EqualShape_d4d842b2a58207a2(ProjectInfoTelemetryMeasurements a, ProjectInfoTelemetryMeasurements b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6bd2a66c4467bc37(a.JsFileCount, b.JsFileCount) && EqualShape_6bd2a66c4467bc37(a.JsFileSize, b.JsFileSize) && EqualShape_6bd2a66c4467bc37(a.JsxFileCount, b.JsxFileCount) && EqualShape_6bd2a66c4467bc37(a.JsxFileSize, b.JsxFileSize) && EqualShape_6bd2a66c4467bc37(a.TsFileCount, b.TsFileCount) && EqualShape_6bd2a66c4467bc37(a.TsFileSize, b.TsFileSize) && EqualShape_6bd2a66c4467bc37(a.TsxFileCount, b.TsxFileCount) && EqualShape_6bd2a66c4467bc37(a.TsxFileSize, b.TsxFileSize) && EqualShape_6bd2a66c4467bc37(a.DtsFileCount, b.DtsFileCount) && EqualShape_6bd2a66c4467bc37(a.DtsFileSize, b.DtsFileSize);
    }

    private static bool EqualShape_6fa38bb9a3563ce0(PublishDiagnosticsClientCapabilities a, PublishDiagnosticsClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RelatedInformation, b.RelatedInformation) && EqualShape_01178517db2f6abb(a.TagSupport, b.TagSupport) && EqualShape_31f02903a9b7a4aa(a.CodeDescriptionSupport, b.CodeDescriptionSupport) && EqualShape_31f02903a9b7a4aa(a.DataSupport, b.DataSupport) && EqualShape_31f02903a9b7a4aa(a.VersionSupport, b.VersionSupport);
    }

    private static bool EqualShape_a870657479cb5aa7(PublishDiagnosticsParams a, PublishDiagnosticsParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_c2c7219dbdd4eab1(a.Version, b.Version) && EqualShape_ce18911ddb414acc(a.Diagnostics, b.Diagnostics);
    }

    private static bool EqualShape_5de74a81b192304c(Range a, Range b)
    {
        return EqualShape_6d031af10da7a25e(a.Start, b.Start) && EqualShape_6d031af10da7a25e(a.End, b.End);
    }

    private static bool EqualShape_88865600aa610d9f(RangeOrEditRangeWithInsertReplace a, RangeOrEditRangeWithInsertReplace b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c15fa81b51c6dda0(a.@Range, b.@Range) && EqualShape_0f34605711837034(a.EditRangeWithInsertReplace, b.EditRangeWithInsertReplace);
    }

    private static bool EqualShape_d42fdd8e2304fd1f(RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull a, RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c15fa81b51c6dda0(a.@Range, b.@Range) && EqualShape_45ce497aa8b53194(a.PrepareRenamePlaceholder, b.PrepareRenamePlaceholder) && EqualShape_1afe8f9cb73e43ee(a.PrepareRenameDefaultBehavior, b.PrepareRenameDefaultBehavior);
    }

    private static bool EqualShape_c08c1b89cb199dea(ReferenceClientCapabilities a, ReferenceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_49c578f22284c849(ReferenceContext a, ReferenceContext b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.IncludeDeclaration, b.IncludeDeclaration);
    }

    private static bool EqualShape_7972bdd81c162d72(ReferenceOptions a, ReferenceOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_d77f6d8f9dfa25d9(ReferenceParams a, ReferenceParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_736e0638b6dc86f2(a.Context, b.Context);
    }

    private static bool EqualShape_944d8fdf608f0ed6(ReferenceRegistrationOptions a, ReferenceRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_525c0e333a92bf78(RegisterOptions a, RegisterOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a1158e8b376f8c3c(a.TextDocumentImplementation, b.TextDocumentImplementation) && EqualShape_dc874b303baa6dbb(a.TextDocumentTypeDefinition, b.TextDocumentTypeDefinition) && EqualShape_e69c3a42ec51132a(a.TextDocumentDocumentColor, b.TextDocumentDocumentColor) && EqualShape_e69c3a42ec51132a(a.TextDocumentColorPresentation, b.TextDocumentColorPresentation) && EqualShape_2248c420e67b79a4(a.TextDocumentFoldingRange, b.TextDocumentFoldingRange) && EqualShape_4686cbddb0e0dea7(a.TextDocumentDeclaration, b.TextDocumentDeclaration) && EqualShape_00c568d61d6e4506(a.TextDocumentSelectionRange, b.TextDocumentSelectionRange) && EqualShape_f3fc39c6644381af(a.TextDocumentPrepareCallHierarchy, b.TextDocumentPrepareCallHierarchy) && EqualShape_fe696d3ff7732e89(a.TextDocumentSemanticTokens, b.TextDocumentSemanticTokens) && EqualShape_bc219c8cf4519a73(a.TextDocumentLinkedEditingRange, b.TextDocumentLinkedEditingRange) && EqualShape_95c7ba3a24076324(a.WorkspaceWillCreateFiles, b.WorkspaceWillCreateFiles) && EqualShape_95c7ba3a24076324(a.WorkspaceWillRenameFiles, b.WorkspaceWillRenameFiles) && EqualShape_95c7ba3a24076324(a.WorkspaceWillDeleteFiles, b.WorkspaceWillDeleteFiles) && EqualShape_f75de590883ed072(a.TextDocumentMoniker, b.TextDocumentMoniker) && EqualShape_fd11d6b574dafc11(a.TextDocumentPrepareTypeHierarchy, b.TextDocumentPrepareTypeHierarchy) && EqualShape_6740dd541d21b3a4(a.TextDocumentInlineValue, b.TextDocumentInlineValue) && EqualShape_85717bf7a3915adc(a.TextDocumentInlayHint, b.TextDocumentInlayHint) && EqualShape_40ecbb6a23243047(a.TextDocumentDiagnostic, b.TextDocumentDiagnostic) && EqualShape_4d7c5054db5abf5f(a.TextDocumentInlineCompletion, b.TextDocumentInlineCompletion) && EqualShape_599369b639c1cd8a(a.WorkspaceTextDocumentContent, b.WorkspaceTextDocumentContent) && EqualShape_fe6155021c1ea02b(a.TextDocumentWillSaveWaitUntil, b.TextDocumentWillSaveWaitUntil) && EqualShape_94d985c5c25d2f44(a.TextDocumentCompletion, b.TextDocumentCompletion) && EqualShape_844a68e7d6788338(a.TextDocumentHover, b.TextDocumentHover) && EqualShape_ba6554e7a6e6220a(a.TextDocumentSignatureHelp, b.TextDocumentSignatureHelp) && EqualShape_e78a189dd9ae441b(a.TextDocumentDefinition, b.TextDocumentDefinition) && EqualShape_455b65df496a7e4e(a.TextDocumentReferences, b.TextDocumentReferences) && EqualShape_2d1576d4995f0019(a.TextDocumentDocumentHighlight, b.TextDocumentDocumentHighlight) && EqualShape_3be177f910d82eb3(a.TextDocumentDocumentSymbol, b.TextDocumentDocumentSymbol) && EqualShape_735a891d6156b340(a.TextDocumentCodeAction, b.TextDocumentCodeAction) && EqualShape_7f8c0262c5fa45b6(a.WorkspaceSymbol, b.WorkspaceSymbol) && EqualShape_5b4d0277cba25f93(a.TextDocumentCodeLens, b.TextDocumentCodeLens) && EqualShape_552d6ebc223a0b59(a.TextDocumentDocumentLink, b.TextDocumentDocumentLink) && EqualShape_d75a7439bcbd1f29(a.TextDocumentFormatting, b.TextDocumentFormatting) && EqualShape_c4b8e34a248dcb66(a.TextDocumentRangeFormatting, b.TextDocumentRangeFormatting) && EqualShape_c4b8e34a248dcb66(a.TextDocumentRangesFormatting, b.TextDocumentRangesFormatting) && EqualShape_1273c6a7488dec0a(a.TextDocumentOnTypeFormatting, b.TextDocumentOnTypeFormatting) && EqualShape_9a106112ad8519a4(a.TextDocumentRename, b.TextDocumentRename) && EqualShape_5dc7279ef53555bc(a.WorkspaceExecuteCommand, b.WorkspaceExecuteCommand) && EqualShape_95c7ba3a24076324(a.WorkspaceDidCreateFiles, b.WorkspaceDidCreateFiles) && EqualShape_95c7ba3a24076324(a.WorkspaceDidRenameFiles, b.WorkspaceDidRenameFiles) && EqualShape_95c7ba3a24076324(a.WorkspaceDidDeleteFiles, b.WorkspaceDidDeleteFiles) && EqualShape_d9c278c49c9c5441(a.WorkspaceDidChangeConfiguration, b.WorkspaceDidChangeConfiguration) && EqualShape_fe6155021c1ea02b(a.TextDocumentDidOpen, b.TextDocumentDidOpen) && EqualShape_73d416e55ae7bb8e(a.TextDocumentDidChange, b.TextDocumentDidChange) && EqualShape_fe6155021c1ea02b(a.TextDocumentDidClose, b.TextDocumentDidClose) && EqualShape_114d5ab6eeb4591b(a.TextDocumentDidSave, b.TextDocumentDidSave) && EqualShape_fe6155021c1ea02b(a.TextDocumentWillSave, b.TextDocumentWillSave) && EqualShape_e85dcfbccb9fd36f(a.WorkspaceDidChangeWatchedFiles, b.WorkspaceDidChangeWatchedFiles);
    }

    private static bool EqualShape_c793e0d9a1cc6421(Registration a, Registration b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Id, b.Id) && EqualShape_4779ed0bd2fc3188(a.RegisterOptions, b.RegisterOptions);
    }

    private static bool EqualShape_1b5a140ba3379372(RegistrationParams a, RegistrationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6d415e5613568a1b(a.Registrations, b.Registrations);
    }

    private static bool EqualShape_81491243f7e99a45(RegularExpressionsClientCapabilities a, RegularExpressionsClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Engine, b.Engine) && EqualShape_3e89801b52bf7f98(a.Version, b.Version);
    }

    private static bool EqualShape_821440dfb3fa7e3f(RelatedFullDocumentDiagnosticReport a, RelatedFullDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4a4f64afaada24e4(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.ResultId, b.ResultId) && EqualShape_ce18911ddb414acc(a.Items, b.Items) && EqualShape_15bd2111816ca216(a.RelatedDocuments, b.RelatedDocuments);
    }

    private static bool EqualShape_c4ca009a9b03016c(RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport a, RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b44212a73089a572(a.FullDocumentDiagnosticReport, b.FullDocumentDiagnosticReport) && EqualShape_6c7d4458419f8392(a.UnchangedDocumentDiagnosticReport, b.UnchangedDocumentDiagnosticReport);
    }

    private static bool EqualShape_77534693e60adab3(RelatedUnchangedDocumentDiagnosticReport a, RelatedUnchangedDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_830ba33890174a2d(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.ResultId, b.ResultId) && EqualShape_15bd2111816ca216(a.RelatedDocuments, b.RelatedDocuments);
    }

    private static bool EqualShape_417e71cd37016ebf(RelativePattern a, RelativePattern b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f9f7734231f531e1(a.BaseUri, b.BaseUri) && EqualShape_473287f8298dba71(a.Pattern, b.Pattern);
    }

    private static bool EqualShape_0c6819e76b439377(RenameClientCapabilities a, RenameClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.PrepareSupport, b.PrepareSupport) && EqualShape_4f120ecccd865abb(a.PrepareSupportDefaultBehavior, b.PrepareSupportDefaultBehavior) && EqualShape_31f02903a9b7a4aa(a.HonorsChangeAnnotations, b.HonorsChangeAnnotations);
    }

    private static bool EqualShape_3f987788dab9f8e0(RenameFile a, RenameFile b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_40fcf4d785d811c9(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.AnnotationId, b.AnnotationId) && EqualShape_b2094f714cc94ba3(a.OldUri, b.OldUri) && EqualShape_b2094f714cc94ba3(a.NewUri, b.NewUri) && EqualShape_4c0950628e22bacd(a.Options, b.Options);
    }

    private static bool EqualShape_4e2827e566b1dc06(RenameFileOptions a, RenameFileOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Overwrite, b.Overwrite) && EqualShape_31f02903a9b7a4aa(a.IgnoreIfExists, b.IgnoreIfExists);
    }

    private static bool EqualShape_2b49debd9d4868a5(RenameFilesParams a, RenameFilesParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2f05961eae7e619c(a.Files, b.Files);
    }

    private static bool EqualShape_dc010ea0160c6a6a(RenameOptions a, RenameOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.PrepareProvider, b.PrepareProvider);
    }

    private static bool EqualShape_7c73bd9e0a480ec1(RenameParams a, RenameParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_473287f8298dba71(a.NewName, b.NewName);
    }

    private static bool EqualShape_2a0823905e235731(RenameRegistrationOptions a, RenameRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.PrepareProvider, b.PrepareProvider);
    }

    private static bool EqualShape_aa1fe6bec4869c52(RequestFailureTelemetryEvent a, RequestFailureTelemetryEvent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5c63c9ad8dcf6ff2(a.EventName, b.EventName) && EqualShape_96e1158afa77c90c(a.TelemetryPurpose, b.TelemetryPurpose) && EqualShape_6fae2aa56eb29bf7(a.Properties, b.Properties);
    }

    private static bool EqualShape_9d57f0706e39340b(RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull a, RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_929549d024332472(a.RequestFailureTelemetryEvent, b.RequestFailureTelemetryEvent) && EqualShape_0af74c1c29f6331d(a.PerformanceStatsTelemetryEvent, b.PerformanceStatsTelemetryEvent) && EqualShape_f64057052b72427f(a.ProjectInfoTelemetryEvent, b.ProjectInfoTelemetryEvent);
    }

    private static bool EqualShape_6cfd8d46d0edce53(RequestFailureTelemetryProperties a, RequestFailureTelemetryProperties b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.ErrorCode, b.ErrorCode) && EqualShape_473287f8298dba71(a.RequestMethod, b.RequestMethod) && EqualShape_473287f8298dba71(a.Stack, b.Stack);
    }

    private static bool EqualShape_a1431a3c77c75148(ResolvedCallHierarchyClientCapabilities a, ResolvedCallHierarchyClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_4d7a594566ef6368(ResolvedChangeAnnotationsSupportOptions a, ResolvedChangeAnnotationsSupportOptions b)
    {
        return EqualShape_b760f44fa5965c24(a.GroupsOnLabel, b.GroupsOnLabel);
    }

    private static bool EqualShape_15a6ffab147ff54f(ResolvedClientCapabilities a, ResolvedClientCapabilities b)
    {
        return EqualShape_0df6a724a7aef924(a.Workspace, b.Workspace) && EqualShape_cf72f937b83f57e6(a.TextDocument, b.TextDocument) && EqualShape_7f5a0984ac008725(a.Window, b.Window) && EqualShape_bc1f8c144e61e107(a.General, b.General) && EqualShape_fc211bcfd2b0a289(a.Experimental, b.Experimental) && EqualShape_b760f44fa5965c24(a.VSSupportsVisualStudioExtensions, b.VSSupportsVisualStudioExtensions) && EqualShape_05c312945775207c(a.VSSupportedSnippetVersion, b.VSSupportedSnippetVersion) && EqualShape_b760f44fa5965c24(a.VSSupportsNotIncludingTextInTextDocumentDidOpen, b.VSSupportsNotIncludingTextInTextDocumentDidOpen) && EqualShape_b760f44fa5965c24(a.VSSupportsIconExtensions, b.VSSupportsIconExtensions) && EqualShape_b760f44fa5965c24(a.VSSupportsDiagnosticRequests, b.VSSupportsDiagnosticRequests);
    }

    private static bool EqualShape_44f7cbe5ecd0606b(ResolvedClientCodeActionKindOptions a, ResolvedClientCodeActionKindOptions b)
    {
        return EqualShape_1dc84ac3dcf6749a(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_0bc2cb70a12f4597(ResolvedClientCodeActionLiteralOptions a, ResolvedClientCodeActionLiteralOptions b)
    {
        return EqualShape_44f7cbe5ecd0606b(a.CodeActionKind, b.CodeActionKind);
    }

    private static bool EqualShape_ec1d3ab2566b74ca(ResolvedClientCodeActionResolveOptions a, ResolvedClientCodeActionResolveOptions b)
    {
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_47455c659890b087(ResolvedClientCodeLensResolveOptions a, ResolvedClientCodeLensResolveOptions b)
    {
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_d11f730ae1cedb58(ResolvedClientCompletionItemInsertTextModeOptions a, ResolvedClientCompletionItemInsertTextModeOptions b)
    {
        return EqualShape_39d79f06277c11aa(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_e258bbb0418c80db(ResolvedClientCompletionItemOptions a, ResolvedClientCompletionItemOptions b)
    {
        return EqualShape_b760f44fa5965c24(a.SnippetSupport, b.SnippetSupport) && EqualShape_b760f44fa5965c24(a.CommitCharactersSupport, b.CommitCharactersSupport) && EqualShape_1599a20ab951c327(a.DocumentationFormat, b.DocumentationFormat) && EqualShape_b760f44fa5965c24(a.DeprecatedSupport, b.DeprecatedSupport) && EqualShape_b760f44fa5965c24(a.PreselectSupport, b.PreselectSupport) && EqualShape_dda3a89f1c5e5940(a.TagSupport, b.TagSupport) && EqualShape_b760f44fa5965c24(a.InsertReplaceSupport, b.InsertReplaceSupport) && EqualShape_de8480b40157ab7f(a.ResolveSupport, b.ResolveSupport) && EqualShape_d11f730ae1cedb58(a.InsertTextModeSupport, b.InsertTextModeSupport) && EqualShape_b760f44fa5965c24(a.LabelDetailsSupport, b.LabelDetailsSupport);
    }

    private static bool EqualShape_3c6c4303d29123cf(ResolvedClientCompletionItemOptionsKind a, ResolvedClientCompletionItemOptionsKind b)
    {
        return EqualShape_78ac88b4cb4bbbef(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_de8480b40157ab7f(ResolvedClientCompletionItemResolveOptions a, ResolvedClientCompletionItemResolveOptions b)
    {
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_0d2b729e1b5a42a7(ResolvedClientDiagnosticsTagOptions a, ResolvedClientDiagnosticsTagOptions b)
    {
        return EqualShape_d1a5fcd9177b1e80(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_b0d5a44f2167b0a2(ResolvedClientFoldingRangeKindOptions a, ResolvedClientFoldingRangeKindOptions b)
    {
        return EqualShape_7ecdd9acf300e742(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_3c1088b29f2d583e(ResolvedClientFoldingRangeOptions a, ResolvedClientFoldingRangeOptions b)
    {
        return EqualShape_b760f44fa5965c24(a.CollapsedText, b.CollapsedText);
    }

    private static bool EqualShape_adad191c3b598df5(ResolvedClientInlayHintResolveOptions a, ResolvedClientInlayHintResolveOptions b)
    {
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_c636f60fd26466f1(ResolvedClientSemanticTokensRequestOptions a, ResolvedClientSemanticTokensRequestOptions b)
    {
        return EqualShape_0464b31e8eb7a40e(a.@Range, b.@Range) && EqualShape_ceb7a942d3e190ba(a.Full, b.Full);
    }

    private static bool EqualShape_6376761663dda21c(ResolvedClientShowMessageActionItemOptions a, ResolvedClientShowMessageActionItemOptions b)
    {
        return EqualShape_b760f44fa5965c24(a.AdditionalPropertiesSupport, b.AdditionalPropertiesSupport);
    }

    private static bool EqualShape_18234a841967b063(ResolvedClientSignatureInformationOptions a, ResolvedClientSignatureInformationOptions b)
    {
        return EqualShape_1599a20ab951c327(a.DocumentationFormat, b.DocumentationFormat) && EqualShape_7ef36a2db7d0846c(a.ParameterInformation, b.ParameterInformation) && EqualShape_b760f44fa5965c24(a.ActiveParameterSupport, b.ActiveParameterSupport) && EqualShape_b760f44fa5965c24(a.NoActiveParameterSupport, b.NoActiveParameterSupport);
    }

    private static bool EqualShape_7ef36a2db7d0846c(ResolvedClientSignatureParameterInformationOptions a, ResolvedClientSignatureParameterInformationOptions b)
    {
        return EqualShape_b760f44fa5965c24(a.LabelOffsetSupport, b.LabelOffsetSupport);
    }

    private static bool EqualShape_14836655cb506488(ResolvedClientSymbolKindOptions a, ResolvedClientSymbolKindOptions b)
    {
        return EqualShape_07349dd2472a64ed(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_d25c9406f3bdb053(ResolvedClientSymbolResolveOptions a, ResolvedClientSymbolResolveOptions b)
    {
        return EqualShape_4af101177fe720bf(a.Properties, b.Properties);
    }

    private static bool EqualShape_20b4b5ac914261e2(ResolvedClientSymbolTagOptions a, ResolvedClientSymbolTagOptions b)
    {
        return EqualShape_0a862cf9a5f1866e(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_641a1024311e36fe(ResolvedCodeActionClientCapabilities a, ResolvedCodeActionClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_0bc2cb70a12f4597(a.CodeActionLiteralSupport, b.CodeActionLiteralSupport) && EqualShape_b760f44fa5965c24(a.IsPreferredSupport, b.IsPreferredSupport) && EqualShape_b760f44fa5965c24(a.DisabledSupport, b.DisabledSupport) && EqualShape_b760f44fa5965c24(a.DataSupport, b.DataSupport) && EqualShape_ec1d3ab2566b74ca(a.ResolveSupport, b.ResolveSupport) && EqualShape_b760f44fa5965c24(a.HonorsChangeAnnotations, b.HonorsChangeAnnotations) && EqualShape_b760f44fa5965c24(a.DocumentationSupport, b.DocumentationSupport) && EqualShape_e814eab5db3b8cf5(a.TagSupport, b.TagSupport);
    }

    private static bool EqualShape_e814eab5db3b8cf5(ResolvedCodeActionTagOptions a, ResolvedCodeActionTagOptions b)
    {
        return EqualShape_f7e208d55963916d(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_0ff44a61bdf71057(ResolvedCodeLensClientCapabilities a, ResolvedCodeLensClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_47455c659890b087(a.ResolveSupport, b.ResolveSupport);
    }

    private static bool EqualShape_0e5fa0acd9a1a453(ResolvedCodeLensWorkspaceClientCapabilities a, ResolvedCodeLensWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_ad0f5cb601bc08b8(ResolvedCompletionClientCapabilities a, ResolvedCompletionClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_e258bbb0418c80db(a.CompletionItem, b.CompletionItem) && EqualShape_3c6c4303d29123cf(a.CompletionItemKind, b.CompletionItemKind) && EqualShape_6cddb8f12d636f30(a.InsertTextMode, b.InsertTextMode) && EqualShape_b760f44fa5965c24(a.ContextSupport, b.ContextSupport) && EqualShape_00713122eb953ff0(a.CompletionList, b.CompletionList);
    }

    private static bool EqualShape_dda3a89f1c5e5940(ResolvedCompletionItemTagOptions a, ResolvedCompletionItemTagOptions b)
    {
        return EqualShape_ea1bedafe91c8e42(a.ValueSet, b.ValueSet);
    }

    private static bool EqualShape_00713122eb953ff0(ResolvedCompletionListCapabilities a, ResolvedCompletionListCapabilities b)
    {
        return EqualShape_4af101177fe720bf(a.ItemDefaults, b.ItemDefaults) && EqualShape_b760f44fa5965c24(a.ApplyKindSupport, b.ApplyKindSupport);
    }

    private static bool EqualShape_a42722ce7a0bd593(ResolvedDeclarationClientCapabilities a, ResolvedDeclarationClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_33dd478114d2d134(ResolvedDefinitionClientCapabilities a, ResolvedDefinitionClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_03a4c5c9ebafb0c9(ResolvedDiagnosticClientCapabilities a, ResolvedDiagnosticClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RelatedInformation, b.RelatedInformation) && EqualShape_0d2b729e1b5a42a7(a.TagSupport, b.TagSupport) && EqualShape_b760f44fa5965c24(a.CodeDescriptionSupport, b.CodeDescriptionSupport) && EqualShape_b760f44fa5965c24(a.DataSupport, b.DataSupport) && EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.RelatedDocumentSupport, b.RelatedDocumentSupport) && EqualShape_b760f44fa5965c24(a.MarkupMessageSupport, b.MarkupMessageSupport);
    }

    private static bool EqualShape_eb7c1ded116c115a(ResolvedDiagnosticWorkspaceClientCapabilities a, ResolvedDiagnosticWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_12a2abfb3665ab1e(ResolvedDidChangeConfigurationClientCapabilities a, ResolvedDidChangeConfigurationClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_e8f2fbc0da1f4a99(ResolvedDidChangeWatchedFilesClientCapabilities a, ResolvedDidChangeWatchedFilesClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.RelativePatternSupport, b.RelativePatternSupport);
    }

    private static bool EqualShape_3c4a740ec0e4680c(ResolvedDocumentColorClientCapabilities a, ResolvedDocumentColorClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_3e07826012df67db(ResolvedDocumentFormattingClientCapabilities a, ResolvedDocumentFormattingClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_225fc6c37e90f6f2(ResolvedDocumentHighlightClientCapabilities a, ResolvedDocumentHighlightClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_09d19c1cdd74e35b(ResolvedDocumentLinkClientCapabilities a, ResolvedDocumentLinkClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.TooltipSupport, b.TooltipSupport);
    }

    private static bool EqualShape_5a21ad75a1fe240f(ResolvedDocumentOnTypeFormattingClientCapabilities a, ResolvedDocumentOnTypeFormattingClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_4d325fddfb853b0a(ResolvedDocumentRangeFormattingClientCapabilities a, ResolvedDocumentRangeFormattingClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.RangesSupport, b.RangesSupport);
    }

    private static bool EqualShape_53f5bf3e3f4d7e6e(ResolvedDocumentSymbolClientCapabilities a, ResolvedDocumentSymbolClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_14836655cb506488(a.SymbolKind, b.SymbolKind) && EqualShape_b760f44fa5965c24(a.HierarchicalDocumentSymbolSupport, b.HierarchicalDocumentSymbolSupport) && EqualShape_20b4b5ac914261e2(a.TagSupport, b.TagSupport) && EqualShape_b760f44fa5965c24(a.LabelSupport, b.LabelSupport);
    }

    private static bool EqualShape_13f53d28d95c16fe(ResolvedExecuteCommandClientCapabilities a, ResolvedExecuteCommandClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_fc211bcfd2b0a289(ResolvedExperimentalClientCapabilities a, ResolvedExperimentalClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.HoverVerbosityLevel, b.HoverVerbosityLevel);
    }

    private static bool EqualShape_07e1391212a2fdb5(ResolvedFileOperationClientCapabilities a, ResolvedFileOperationClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.DidCreate, b.DidCreate) && EqualShape_b760f44fa5965c24(a.WillCreate, b.WillCreate) && EqualShape_b760f44fa5965c24(a.DidRename, b.DidRename) && EqualShape_b760f44fa5965c24(a.WillRename, b.WillRename) && EqualShape_b760f44fa5965c24(a.DidDelete, b.DidDelete) && EqualShape_b760f44fa5965c24(a.WillDelete, b.WillDelete);
    }

    private static bool EqualShape_eb30950ba5d34e43(ResolvedFoldingRangeClientCapabilities a, ResolvedFoldingRangeClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_1c06d81634ed9a2e(a.RangeLimit, b.RangeLimit) && EqualShape_b760f44fa5965c24(a.LineFoldingOnly, b.LineFoldingOnly) && EqualShape_b0d5a44f2167b0a2(a.FoldingRangeKind, b.FoldingRangeKind) && EqualShape_3c1088b29f2d583e(a.FoldingRange, b.FoldingRange);
    }

    private static bool EqualShape_692f253b9522679f(ResolvedFoldingRangeWorkspaceClientCapabilities a, ResolvedFoldingRangeWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_bc1f8c144e61e107(ResolvedGeneralClientCapabilities a, ResolvedGeneralClientCapabilities b)
    {
        return EqualShape_ae4cf1722d9b1b8c(a.StaleRequestSupport, b.StaleRequestSupport) && EqualShape_beda1189ff75fe3c(a.RegularExpressions, b.RegularExpressions) && EqualShape_48c437b8486b4b13(a.Markdown, b.Markdown) && EqualShape_c901c7a8c1b9d477(a.PositionEncodings, b.PositionEncodings);
    }

    private static bool EqualShape_74ee6076b161a5e2(ResolvedHoverClientCapabilities a, ResolvedHoverClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_1599a20ab951c327(a.ContentFormat, b.ContentFormat);
    }

    private static bool EqualShape_6f72f8fa55d75ae2(ResolvedImplementationClientCapabilities a, ResolvedImplementationClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_27a6a96bab603564(ResolvedInlayHintClientCapabilities a, ResolvedInlayHintClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_adad191c3b598df5(a.ResolveSupport, b.ResolveSupport);
    }

    private static bool EqualShape_6f24450c9e850af2(ResolvedInlayHintWorkspaceClientCapabilities a, ResolvedInlayHintWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_e6379ab3c5a02d71(ResolvedInlineCompletionClientCapabilities a, ResolvedInlineCompletionClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_6d4e69b83ef46f6c(ResolvedInlineValueClientCapabilities a, ResolvedInlineValueClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_0cf7bd74ae5beeb5(ResolvedInlineValueWorkspaceClientCapabilities a, ResolvedInlineValueWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_7b1ab6225cd7e83c(ResolvedLinkedEditingRangeClientCapabilities a, ResolvedLinkedEditingRangeClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_48c437b8486b4b13(ResolvedMarkdownClientCapabilities a, ResolvedMarkdownClientCapabilities b)
    {
        return EqualShape_473287f8298dba71(a.Parser, b.Parser) && EqualShape_473287f8298dba71(a.Version, b.Version) && EqualShape_4af101177fe720bf(a.AllowedTags, b.AllowedTags);
    }

    private static bool EqualShape_6a11cf7a261f6596(ResolvedMonikerClientCapabilities a, ResolvedMonikerClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_aa17796b111066d2(ResolvedPublishDiagnosticsClientCapabilities a, ResolvedPublishDiagnosticsClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RelatedInformation, b.RelatedInformation) && EqualShape_0d2b729e1b5a42a7(a.TagSupport, b.TagSupport) && EqualShape_b760f44fa5965c24(a.CodeDescriptionSupport, b.CodeDescriptionSupport) && EqualShape_b760f44fa5965c24(a.DataSupport, b.DataSupport) && EqualShape_b760f44fa5965c24(a.VersionSupport, b.VersionSupport);
    }

    private static bool EqualShape_3b8690524f16adbd(ResolvedReferenceClientCapabilities a, ResolvedReferenceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_beda1189ff75fe3c(ResolvedRegularExpressionsClientCapabilities a, ResolvedRegularExpressionsClientCapabilities b)
    {
        return EqualShape_473287f8298dba71(a.Engine, b.Engine) && EqualShape_473287f8298dba71(a.Version, b.Version);
    }

    private static bool EqualShape_11239c0149bed461(ResolvedRenameClientCapabilities a, ResolvedRenameClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.PrepareSupport, b.PrepareSupport) && EqualShape_491ada003a0c8630(a.PrepareSupportDefaultBehavior, b.PrepareSupportDefaultBehavior) && EqualShape_b760f44fa5965c24(a.HonorsChangeAnnotations, b.HonorsChangeAnnotations);
    }

    private static bool EqualShape_337aad13e30e29ee(ResolvedSelectionRangeClientCapabilities a, ResolvedSelectionRangeClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_139fcea77a02d93c(ResolvedSemanticTokensClientCapabilities a, ResolvedSemanticTokensClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_c636f60fd26466f1(a.Requests, b.Requests) && EqualShape_4af101177fe720bf(a.TokenTypes, b.TokenTypes) && EqualShape_4af101177fe720bf(a.TokenModifiers, b.TokenModifiers) && EqualShape_20603491c5ccd12d(a.Formats, b.Formats) && EqualShape_b760f44fa5965c24(a.OverlappingTokenSupport, b.OverlappingTokenSupport) && EqualShape_b760f44fa5965c24(a.MultilineTokenSupport, b.MultilineTokenSupport) && EqualShape_b760f44fa5965c24(a.ServerCancelSupport, b.ServerCancelSupport) && EqualShape_b760f44fa5965c24(a.AugmentsSyntaxTokens, b.AugmentsSyntaxTokens);
    }

    private static bool EqualShape_d1f533b25382465d(ResolvedSemanticTokensWorkspaceClientCapabilities a, ResolvedSemanticTokensWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_0b6ae5afd248382d(ResolvedShowDocumentClientCapabilities a, ResolvedShowDocumentClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.Support, b.Support);
    }

    private static bool EqualShape_b7a258d345a9c8c3(ResolvedShowMessageRequestClientCapabilities a, ResolvedShowMessageRequestClientCapabilities b)
    {
        return EqualShape_6376761663dda21c(a.MessageActionItem, b.MessageActionItem);
    }

    private static bool EqualShape_a25ed1013e366188(ResolvedSignatureHelpClientCapabilities a, ResolvedSignatureHelpClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_18234a841967b063(a.SignatureInformation, b.SignatureInformation) && EqualShape_b760f44fa5965c24(a.ContextSupport, b.ContextSupport);
    }

    private static bool EqualShape_ae4cf1722d9b1b8c(ResolvedStaleRequestSupportOptions a, ResolvedStaleRequestSupportOptions b)
    {
        return EqualShape_b760f44fa5965c24(a.Cancel, b.Cancel) && EqualShape_4af101177fe720bf(a.RetryOnContentModified, b.RetryOnContentModified);
    }

    private static bool EqualShape_cf72f937b83f57e6(ResolvedTextDocumentClientCapabilities a, ResolvedTextDocumentClientCapabilities b)
    {
        return EqualShape_f55898ff0d7d0b57(a.Synchronization, b.Synchronization) && EqualShape_6d2551c03ed73195(a.Filters, b.Filters) && EqualShape_ad0f5cb601bc08b8(a.Completion, b.Completion) && EqualShape_74ee6076b161a5e2(a.Hover, b.Hover) && EqualShape_a25ed1013e366188(a.SignatureHelp, b.SignatureHelp) && EqualShape_a42722ce7a0bd593(a.Declaration, b.Declaration) && EqualShape_33dd478114d2d134(a.Definition, b.Definition) && EqualShape_b6a3c7fa1b7024cd(a.TypeDefinition, b.TypeDefinition) && EqualShape_6f72f8fa55d75ae2(a.Implementation, b.Implementation) && EqualShape_3b8690524f16adbd(a.References, b.References) && EqualShape_225fc6c37e90f6f2(a.DocumentHighlight, b.DocumentHighlight) && EqualShape_53f5bf3e3f4d7e6e(a.DocumentSymbol, b.DocumentSymbol) && EqualShape_641a1024311e36fe(a.CodeAction, b.CodeAction) && EqualShape_0ff44a61bdf71057(a.CodeLens, b.CodeLens) && EqualShape_09d19c1cdd74e35b(a.DocumentLink, b.DocumentLink) && EqualShape_3c4a740ec0e4680c(a.ColorProvider, b.ColorProvider) && EqualShape_3e07826012df67db(a.Formatting, b.Formatting) && EqualShape_4d325fddfb853b0a(a.RangeFormatting, b.RangeFormatting) && EqualShape_5a21ad75a1fe240f(a.OnTypeFormatting, b.OnTypeFormatting) && EqualShape_11239c0149bed461(a.Rename, b.Rename) && EqualShape_eb30950ba5d34e43(a.FoldingRange, b.FoldingRange) && EqualShape_337aad13e30e29ee(a.SelectionRange, b.SelectionRange) && EqualShape_aa17796b111066d2(a.PublishDiagnostics, b.PublishDiagnostics) && EqualShape_a1431a3c77c75148(a.CallHierarchy, b.CallHierarchy) && EqualShape_139fcea77a02d93c(a.SemanticTokens, b.SemanticTokens) && EqualShape_7b1ab6225cd7e83c(a.LinkedEditingRange, b.LinkedEditingRange) && EqualShape_6a11cf7a261f6596(a.Moniker, b.Moniker) && EqualShape_f5214ddae878858f(a.TypeHierarchy, b.TypeHierarchy) && EqualShape_6d4e69b83ef46f6c(a.InlineValue, b.InlineValue) && EqualShape_27a6a96bab603564(a.InlayHint, b.InlayHint) && EqualShape_03a4c5c9ebafb0c9(a.Diagnostic, b.Diagnostic) && EqualShape_e6379ab3c5a02d71(a.InlineCompletion, b.InlineCompletion);
    }

    private static bool EqualShape_a3299508a5069228(ResolvedTextDocumentContentClientCapabilities a, ResolvedTextDocumentContentClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_6d2551c03ed73195(ResolvedTextDocumentFilterClientCapabilities a, ResolvedTextDocumentFilterClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.RelativePatternSupport, b.RelativePatternSupport);
    }

    private static bool EqualShape_f55898ff0d7d0b57(ResolvedTextDocumentSyncClientCapabilities a, ResolvedTextDocumentSyncClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.WillSave, b.WillSave) && EqualShape_b760f44fa5965c24(a.WillSaveWaitUntil, b.WillSaveWaitUntil) && EqualShape_b760f44fa5965c24(a.DidSave, b.DidSave);
    }

    private static bool EqualShape_b6a3c7fa1b7024cd(ResolvedTypeDefinitionClientCapabilities a, ResolvedTypeDefinitionClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_b760f44fa5965c24(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_f5214ddae878858f(ResolvedTypeHierarchyClientCapabilities a, ResolvedTypeHierarchyClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_7f5a0984ac008725(ResolvedWindowClientCapabilities a, ResolvedWindowClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_b7a258d345a9c8c3(a.ShowMessage, b.ShowMessage) && EqualShape_0b6ae5afd248382d(a.ShowDocument, b.ShowDocument);
    }

    private static bool EqualShape_0df6a724a7aef924(ResolvedWorkspaceClientCapabilities a, ResolvedWorkspaceClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.ApplyEdit, b.ApplyEdit) && EqualShape_617cc3531ccde6a8(a.WorkspaceEdit, b.WorkspaceEdit) && EqualShape_12a2abfb3665ab1e(a.DidChangeConfiguration, b.DidChangeConfiguration) && EqualShape_e8f2fbc0da1f4a99(a.DidChangeWatchedFiles, b.DidChangeWatchedFiles) && EqualShape_d66f8c5651bd8423(a.Symbol, b.Symbol) && EqualShape_13f53d28d95c16fe(a.ExecuteCommand, b.ExecuteCommand) && EqualShape_b760f44fa5965c24(a.WorkspaceFolders, b.WorkspaceFolders) && EqualShape_b760f44fa5965c24(a.Configuration, b.Configuration) && EqualShape_d1f533b25382465d(a.SemanticTokens, b.SemanticTokens) && EqualShape_0e5fa0acd9a1a453(a.CodeLens, b.CodeLens) && EqualShape_07e1391212a2fdb5(a.FileOperations, b.FileOperations) && EqualShape_0cf7bd74ae5beeb5(a.InlineValue, b.InlineValue) && EqualShape_6f24450c9e850af2(a.InlayHint, b.InlayHint) && EqualShape_eb7c1ded116c115a(a.Diagnostics, b.Diagnostics) && EqualShape_692f253b9522679f(a.FoldingRange, b.FoldingRange) && EqualShape_a3299508a5069228(a.TextDocumentContent, b.TextDocumentContent);
    }

    private static bool EqualShape_617cc3531ccde6a8(ResolvedWorkspaceEditClientCapabilities a, ResolvedWorkspaceEditClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DocumentChanges, b.DocumentChanges) && EqualShape_dac512c95e2386c0(a.ResourceOperations, b.ResourceOperations) && EqualShape_484e4404d3769d00(a.FailureHandling, b.FailureHandling) && EqualShape_b760f44fa5965c24(a.NormalizesLineEndings, b.NormalizesLineEndings) && EqualShape_4d7a594566ef6368(a.ChangeAnnotationSupport, b.ChangeAnnotationSupport) && EqualShape_b760f44fa5965c24(a.MetadataSupport, b.MetadataSupport) && EqualShape_b760f44fa5965c24(a.SnippetEditSupport, b.SnippetEditSupport);
    }

    private static bool EqualShape_d66f8c5651bd8423(ResolvedWorkspaceSymbolClientCapabilities a, ResolvedWorkspaceSymbolClientCapabilities b)
    {
        return EqualShape_b760f44fa5965c24(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_14836655cb506488(a.SymbolKind, b.SymbolKind) && EqualShape_20b4b5ac914261e2(a.TagSupport, b.TagSupport) && EqualShape_d25c9406f3bdb053(a.ResolveSupport, b.ResolveSupport);
    }

    private static bool EqualShape_bb0929e8a5eb2809(ResourceOperation a, ResourceOperation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.AnnotationId, b.AnnotationId);
    }

    private static bool EqualShape_3eec28594f7be075(ResourceOperationKind a, ResourceOperationKind b)
    {
        return EqualityComparer<ResourceOperationKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_0c860e4c1955b16b(SaveOptions a, SaveOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.IncludeText, b.IncludeText);
    }

    private static bool EqualShape_422af1cce96213bc(SelectedCompletionInfo a, SelectedCompletionInfo b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_473287f8298dba71(a.Text, b.Text);
    }

    private static bool EqualShape_7d7b70e7ccd8dfec(SelectionRange a, SelectionRange b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_09de083f42750e75(a.Parent, b.Parent);
    }

    private static bool EqualShape_a4e17fae82626379(SelectionRangeClientCapabilities a, SelectionRangeClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_de0943b8402d64fb(SelectionRangeOptions a, SelectionRangeOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_0b27b0baa03194cf(SelectionRangeParams a, SelectionRangeParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_45419e3f2e1b1343(a.Positions, b.Positions);
    }

    private static bool EqualShape_1299af8f490cf41a(SelectionRangeRegistrationOptions a, SelectionRangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_130b4f72041fba63(SelectionRangesOrNull a, SelectionRangesOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f0f6f210736d68bc(a.SelectionRanges, b.SelectionRanges);
    }

    private static bool EqualShape_6b5b03f3fef5cc31(SemanticTokenModifier a, SemanticTokenModifier b)
    {
        return EqualityComparer<SemanticTokenModifier>.Default.Equals(a, b);
    }

    private static bool EqualShape_ffe0cd0a8bc7038a(SemanticTokenType a, SemanticTokenType b)
    {
        return EqualityComparer<SemanticTokenType>.Default.Equals(a, b);
    }

    private static bool EqualShape_448a954da33d9419(SemanticTokens a, SemanticTokens b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.ResultId, b.ResultId) && EqualShape_4c1cc0279aa62acf(a.Data, b.Data);
    }

    private static bool EqualShape_22677e2a6998efa4(SemanticTokensClientCapabilities a, SemanticTokensClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_60a32c284332509f(a.Requests, b.Requests) && EqualShape_4af101177fe720bf(a.TokenTypes, b.TokenTypes) && EqualShape_4af101177fe720bf(a.TokenModifiers, b.TokenModifiers) && EqualShape_20603491c5ccd12d(a.Formats, b.Formats) && EqualShape_31f02903a9b7a4aa(a.OverlappingTokenSupport, b.OverlappingTokenSupport) && EqualShape_31f02903a9b7a4aa(a.MultilineTokenSupport, b.MultilineTokenSupport) && EqualShape_31f02903a9b7a4aa(a.ServerCancelSupport, b.ServerCancelSupport) && EqualShape_31f02903a9b7a4aa(a.AugmentsSyntaxTokens, b.AugmentsSyntaxTokens);
    }

    private static bool EqualShape_1c8ca776704f95d7(SemanticTokensDelta a, SemanticTokensDelta b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.ResultId, b.ResultId) && EqualShape_2a6767a930bd40f7(a.Edits, b.Edits);
    }

    private static bool EqualShape_9101edacad945287(SemanticTokensDeltaParams a, SemanticTokensDeltaParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_473287f8298dba71(a.PreviousResultId, b.PreviousResultId);
    }

    private static bool EqualShape_236ed7986274e517(SemanticTokensDeltaPartialResult a, SemanticTokensDeltaPartialResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2a6767a930bd40f7(a.Edits, b.Edits);
    }

    private static bool EqualShape_5d1443274aea049d(SemanticTokensEdit a, SemanticTokensEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1c06d81634ed9a2e(a.Start, b.Start) && EqualShape_1c06d81634ed9a2e(a.DeleteCount, b.DeleteCount) && EqualShape_1c48ab226b18fa3a(a.Data, b.Data);
    }

    private static bool EqualShape_47409fff8234f1d6(SemanticTokensFullDelta a, SemanticTokensFullDelta b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Delta, b.Delta);
    }

    private static bool EqualShape_508b7ba4b756581e(SemanticTokensLegend a, SemanticTokensLegend b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.TokenTypes, b.TokenTypes) && EqualShape_4af101177fe720bf(a.TokenModifiers, b.TokenModifiers);
    }

    private static bool EqualShape_e73c6a23ccc9fff4(SemanticTokensOptions a, SemanticTokensOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_48c95bf06e1b64d4(a.Legend, b.Legend) && EqualShape_5b602f5e58c34242(a.@Range, b.@Range) && EqualShape_c2aad4b3c18b521c(a.Full, b.Full);
    }

    private static bool EqualShape_45b9b7d638819fbf(SemanticTokensOptionsOrRegistrationOptions a, SemanticTokensOptionsOrRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_811cdaf9e395c82f(a.Options, b.Options) && EqualShape_fe696d3ff7732e89(a.RegistrationOptions, b.RegistrationOptions);
    }

    private static bool EqualShape_ea15ccbea3b9ff1e(SemanticTokensOrNull a, SemanticTokensOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ebdc16536169d222(a.SemanticTokens, b.SemanticTokens);
    }

    private static bool EqualShape_0e9da642d9c60622(SemanticTokensOrSemanticTokensDeltaOrNull a, SemanticTokensOrSemanticTokensDeltaOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_ebdc16536169d222(a.SemanticTokens, b.SemanticTokens) && EqualShape_3d6953018d2b3b79(a.SemanticTokensDelta, b.SemanticTokensDelta);
    }

    private static bool EqualShape_8bf9ad4f5cef6cfa(SemanticTokensParams a, SemanticTokensParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_92f2d270bac051fa(SemanticTokensPartialResult a, SemanticTokensPartialResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4c1cc0279aa62acf(a.Data, b.Data);
    }

    private static bool EqualShape_0dc62352c8c25887(SemanticTokensRangeParams a, SemanticTokensRangeParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_5de74a81b192304c(a.@Range, b.@Range);
    }

    private static bool EqualShape_4b3cdaefc55e634e(SemanticTokensRegistrationOptions a, SemanticTokensRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_48c95bf06e1b64d4(a.Legend, b.Legend) && EqualShape_5b602f5e58c34242(a.@Range, b.@Range) && EqualShape_c2aad4b3c18b521c(a.Full, b.Full) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_fe40ff6064ff54f2(SemanticTokensWorkspaceClientCapabilities a, SemanticTokensWorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RefreshSupport, b.RefreshSupport);
    }

    private static bool EqualShape_628c62fa6a34c245(ServerCapabilities a, ServerCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d99a5f4bdbe98370(a.PositionEncoding, b.PositionEncoding) && EqualShape_01c0f78b125f5830(a.TextDocumentSync, b.TextDocumentSync) && EqualShape_9e007aa38ffe63d0(a.CompletionProvider, b.CompletionProvider) && EqualShape_61c783782174fcc1(a.HoverProvider, b.HoverProvider) && EqualShape_cc58706809ce50fc(a.SignatureHelpProvider, b.SignatureHelpProvider) && EqualShape_77c7b71b2461b29c(a.DeclarationProvider, b.DeclarationProvider) && EqualShape_259dbc4245dbc446(a.DefinitionProvider, b.DefinitionProvider) && EqualShape_ff8bcc4e001f966d(a.TypeDefinitionProvider, b.TypeDefinitionProvider) && EqualShape_da11e3aa093439aa(a.ImplementationProvider, b.ImplementationProvider) && EqualShape_0c5a48b6e102e5c9(a.ReferencesProvider, b.ReferencesProvider) && EqualShape_f1cff6dd6c13f7f1(a.DocumentHighlightProvider, b.DocumentHighlightProvider) && EqualShape_b2a4ac939659fc9f(a.DocumentSymbolProvider, b.DocumentSymbolProvider) && EqualShape_13036b4475c995d5(a.CodeActionProvider, b.CodeActionProvider) && EqualShape_53c738698791fca8(a.CodeLensProvider, b.CodeLensProvider) && EqualShape_17440916585efd87(a.DocumentLinkProvider, b.DocumentLinkProvider) && EqualShape_0c5bef69fdd57a46(a.ColorProvider, b.ColorProvider) && EqualShape_b0181fd8a1566118(a.WorkspaceSymbolProvider, b.WorkspaceSymbolProvider) && EqualShape_785ae5941ed13886(a.DocumentFormattingProvider, b.DocumentFormattingProvider) && EqualShape_ae4ad4f64fbd3d83(a.DocumentRangeFormattingProvider, b.DocumentRangeFormattingProvider) && EqualShape_d1f8e0711d3ee76b(a.DocumentOnTypeFormattingProvider, b.DocumentOnTypeFormattingProvider) && EqualShape_2b687e3cf5129ec6(a.RenameProvider, b.RenameProvider) && EqualShape_6240b47f5020cfc1(a.FoldingRangeProvider, b.FoldingRangeProvider) && EqualShape_82434037fc61936c(a.SelectionRangeProvider, b.SelectionRangeProvider) && EqualShape_6c0ea6c32425b0e0(a.ExecuteCommandProvider, b.ExecuteCommandProvider) && EqualShape_ea9906a90cd63255(a.CallHierarchyProvider, b.CallHierarchyProvider) && EqualShape_d3a0d0010acc34e0(a.LinkedEditingRangeProvider, b.LinkedEditingRangeProvider) && EqualShape_bab5a33b2118066f(a.SemanticTokensProvider, b.SemanticTokensProvider) && EqualShape_1f6a23b003d18878(a.MonikerProvider, b.MonikerProvider) && EqualShape_d450ab36f6254939(a.TypeHierarchyProvider, b.TypeHierarchyProvider) && EqualShape_31859f7808a72778(a.InlineValueProvider, b.InlineValueProvider) && EqualShape_a4ab8b7c4476ab01(a.InlayHintProvider, b.InlayHintProvider) && EqualShape_97e699b77b112a1d(a.DiagnosticProvider, b.DiagnosticProvider) && EqualShape_10f353960870ef44(a.InlineCompletionProvider, b.InlineCompletionProvider) && EqualShape_a858cc09b0d6150b(a.Workspace, b.Workspace) && EqualShape_2f77452b23ae1df3(a.Experimental, b.Experimental) && EqualShape_20a5f76192074c7b(a.VSOnAutoInsertProvider, b.VSOnAutoInsertProvider) && EqualShape_31f02903a9b7a4aa(a.VSReferencesProvider, b.VSReferencesProvider);
    }

    private static bool EqualShape_3b0c84c52fea24a8(ServerCompletionItemOptions a, ServerCompletionItemOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.LabelDetailsSupport, b.LabelDetailsSupport);
    }

    private static bool EqualShape_4f0db94bc9f4a530(ServerInfo a, ServerInfo b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_3e89801b52bf7f98(a.Version, b.Version);
    }

    private static bool EqualShape_1171d758ea6161e3(SetContentMapperContributionsParams a, SetContentMapperContributionsParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c2b56fcb6b7cc23e(a.Contributions, b.Contributions) && EqualShape_91ce3736d1dc6bcf(a.OpenDocuments, b.OpenDocuments);
    }

    private static bool EqualShape_b4447bd7705f87ab(SetLogVerbosityParams a, SetLogVerbosityParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_061133d05f438d15(a.Verbosity, b.Verbosity);
    }

    private static bool EqualShape_41823968658e5dfc(SetTraceParams a, SetTraceParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_98b926ad367a3bcb(a.Value, b.Value);
    }

    private static bool EqualShape_2303d8fc7f8ad8a3(ShowDocumentClientCapabilities a, ShowDocumentClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.Support, b.Support);
    }

    private static bool EqualShape_7e2bf70a83c07533(ShowDocumentParams a, ShowDocumentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d70fbad88d0f9eaf(a.Uri, b.Uri) && EqualShape_31f02903a9b7a4aa(a.External, b.External) && EqualShape_31f02903a9b7a4aa(a.TakeFocus, b.TakeFocus) && EqualShape_c15fa81b51c6dda0(a.Selection, b.Selection);
    }

    private static bool EqualShape_c44a39c887303450(ShowDocumentResult a, ShowDocumentResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.Success, b.Success);
    }

    private static bool EqualShape_663434b32ed8ff88(ShowMessageParams a, ShowMessageParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6ff9dd7ca34940c3(a.Type, b.Type) && EqualShape_473287f8298dba71(a.Message, b.Message);
    }

    private static bool EqualShape_d3e4643133128cfd(ShowMessageRequestClientCapabilities a, ShowMessageRequestClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_beac3eba343e80bd(a.MessageActionItem, b.MessageActionItem);
    }

    private static bool EqualShape_11bb59cd26639030(ShowMessageRequestParams a, ShowMessageRequestParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6ff9dd7ca34940c3(a.Type, b.Type) && EqualShape_473287f8298dba71(a.Message, b.Message) && EqualShape_f5832b80cb969020(a.Actions, b.Actions);
    }

    private static bool EqualShape_5a6c866a2e0d1dee(SignatureHelp a, SignatureHelp b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_2df1476a21b7b317(a.Signatures, b.Signatures) && EqualShape_3f8175cc2ff18f25(a.ActiveSignature, b.ActiveSignature) && EqualShape_7bb64244ba2ae5a2(a.ActiveParameter, b.ActiveParameter);
    }

    private static bool EqualShape_ee58083fc9757534(SignatureHelpClientCapabilities a, SignatureHelpClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_ba85fc2bf9b60b3e(a.SignatureInformation, b.SignatureInformation) && EqualShape_31f02903a9b7a4aa(a.ContextSupport, b.ContextSupport);
    }

    private static bool EqualShape_1331ccfab2d32049(SignatureHelpContext a, SignatureHelpContext b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a60e6e214e11d2ad(a.TriggerKind, b.TriggerKind) && EqualShape_3e89801b52bf7f98(a.TriggerCharacter, b.TriggerCharacter) && EqualShape_b760f44fa5965c24(a.IsRetrigger, b.IsRetrigger) && EqualShape_db195c87aa8dbd39(a.ActiveSignatureHelp, b.ActiveSignatureHelp);
    }

    private static bool EqualShape_f179f1d094fa3f6c(SignatureHelpOptions a, SignatureHelpOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_8941972aede71346(a.TriggerCharacters, b.TriggerCharacters) && EqualShape_8941972aede71346(a.RetriggerCharacters, b.RetriggerCharacters);
    }

    private static bool EqualShape_888fc079fe4e5694(SignatureHelpOrNull a, SignatureHelpOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_db195c87aa8dbd39(a.SignatureHelp, b.SignatureHelp);
    }

    private static bool EqualShape_775120ff277a7df5(SignatureHelpParams a, SignatureHelpParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_d1c7f85c1a40ed24(a.Context, b.Context);
    }

    private static bool EqualShape_cd22913bfb024c92(SignatureHelpRegistrationOptions a, SignatureHelpRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_8941972aede71346(a.TriggerCharacters, b.TriggerCharacters) && EqualShape_8941972aede71346(a.RetriggerCharacters, b.RetriggerCharacters);
    }

    private static bool EqualShape_a60e6e214e11d2ad(SignatureHelpTriggerKind a, SignatureHelpTriggerKind b)
    {
        return EqualityComparer<SignatureHelpTriggerKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_281bb89ea78ef9ef(SignatureInformation a, SignatureInformation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Label, b.Label) && EqualShape_818def5a213356d9(a.Documentation, b.Documentation) && EqualShape_487dd612ddbe2ce0(a.Parameters, b.Parameters) && EqualShape_7bb64244ba2ae5a2(a.ActiveParameter, b.ActiveParameter) && EqualShape_fe1808ad593e0e19(a.VSColorizedLabel, b.VSColorizedLabel);
    }

    private static bool EqualShape_108d1bfa63a85c92(SnippetTextEdit a, SnippetTextEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_2160d538d577aae5(a.Snippet, b.Snippet) && EqualShape_3e89801b52bf7f98(a.AnnotationId, b.AnnotationId);
    }

    private static bool EqualShape_16b0a700ab1e5e92(StaleRequestSupportOptions a, StaleRequestSupportOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b760f44fa5965c24(a.Cancel, b.Cancel) && EqualShape_4af101177fe720bf(a.RetryOnContentModified, b.RetryOnContentModified);
    }

    private static bool EqualShape_689fd1f5d9125759(StaticRegistrationOptions a, StaticRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_88a5baf5906d4eb9(StringLiteralBegin a, StringLiteralBegin b)
    {
        return true;
    }

    private static bool EqualShape_44e043791719404a(StringLiteralClassifiedTextElement a, StringLiteralClassifiedTextElement b)
    {
        return true;
    }

    private static bool EqualShape_bb0b4343e63f3503(StringLiteralClassifiedTextRun a, StringLiteralClassifiedTextRun b)
    {
        return true;
    }

    private static bool EqualShape_4bde02dc25c7843b(StringLiteralContainerElement a, StringLiteralContainerElement b)
    {
        return true;
    }

    private static bool EqualShape_15379156e683e93e(StringLiteralCreate a, StringLiteralCreate b)
    {
        return true;
    }

    private static bool EqualShape_6681ac8dac15fa23(StringLiteralDelete a, StringLiteralDelete b)
    {
        return true;
    }

    private static bool EqualShape_46e624b49f981eb0(StringLiteralEnd a, StringLiteralEnd b)
    {
        return true;
    }

    private static bool EqualShape_96e1158afa77c90c(StringLiteralError a, StringLiteralError b)
    {
        return true;
    }

    private static bool EqualShape_4a4f64afaada24e4(StringLiteralFull a, StringLiteralFull b)
    {
        return true;
    }

    private static bool EqualShape_291de7541a6e8a51(StringLiteralImageElement a, StringLiteralImageElement b)
    {
        return true;
    }

    private static bool EqualShape_f92aabc242322d92(StringLiteralImageId a, StringLiteralImageId b)
    {
        return true;
    }

    private static bool EqualShape_5c63c9ad8dcf6ff2(StringLiteralLanguageServerErrorResponse a, StringLiteralLanguageServerErrorResponse b)
    {
        return true;
    }

    private static bool EqualShape_80a5c2c59f770ffa(StringLiteralLanguageServerPerformanceStats a, StringLiteralLanguageServerPerformanceStats b)
    {
        return true;
    }

    private static bool EqualShape_33119583302dabc8(StringLiteralLanguageServerProjectInfo a, StringLiteralLanguageServerProjectInfo b)
    {
        return true;
    }

    private static bool EqualShape_40fcf4d785d811c9(StringLiteralRename a, StringLiteralRename b)
    {
        return true;
    }

    private static bool EqualShape_0e789aef680d8005(StringLiteralReport a, StringLiteralReport b)
    {
        return true;
    }

    private static bool EqualShape_f79bae81a80b15e3(StringLiteralSnippet a, StringLiteralSnippet b)
    {
        return true;
    }

    private static bool EqualShape_830ba33890174a2d(StringLiteralUnchanged a, StringLiteralUnchanged b)
    {
        return true;
    }

    private static bool EqualShape_45902ec7f4da96d7(StringLiteralUsage a, StringLiteralUsage b)
    {
        return true;
    }

    private static bool EqualShape_4da8fc01584b07f2(StringOrBoolean a, StringOrBoolean b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_31f02903a9b7a4aa(a.Boolean, b.Boolean);
    }

    private static bool EqualShape_be643dd009a1ea80(StringOrInlayHintLabelParts a, StringOrInlayHintLabelParts b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_58bbbfbc30e2a502(a.InlayHintLabelParts, b.InlayHintLabelParts);
    }

    private static bool EqualShape_ae8e2688e3b7bd25(StringOrMarkedStringWithLanguage a, StringOrMarkedStringWithLanguage b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_c3b0d76093e2f53f(a.MarkedStringWithLanguage, b.MarkedStringWithLanguage);
    }

    private static bool EqualShape_31e9efe8f25ec13e(StringOrMarkupContent a, StringOrMarkupContent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_a09c50c289cdf175(a.MarkupContent, b.MarkupContent);
    }

    private static bool EqualShape_5ada2186dac127d6(StringOrNull a, StringOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String);
    }

    private static bool EqualShape_7485e0c10f1b758e(StringOrStringValue a, StringOrStringValue b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_2160d538d577aae5(a.StringValue, b.StringValue);
    }

    private static bool EqualShape_23dbff1cf97c4515(StringOrStrings a, StringOrStrings b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_8941972aede71346(a.Strings, b.Strings);
    }

    private static bool EqualShape_9f68e715f063af80(StringOrTuple a, StringOrTuple b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.@String, b.@String) && EqualShape_66ae27f385454775(a.Tuple, b.Tuple);
    }

    private static bool EqualShape_24b828daee6b73d3(StringValue a, StringValue b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f79bae81a80b15e3(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.Value, b.Value);
    }

    private static bool EqualShape_7a1b84d8b2015bb3(SymbolInformation a, SymbolInformation b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_80525ce65b460521(a.Kind, b.Kind) && EqualShape_2a7c50a738423fd9(a.Tags, b.Tags) && EqualShape_3e89801b52bf7f98(a.ContainerName, b.ContainerName) && EqualShape_31f02903a9b7a4aa(a.Deprecated, b.Deprecated) && EqualShape_15b61974b2707a7b(a.Location, b.Location);
    }

    private static bool EqualShape_abef6860edeb78c0(SymbolInformationsOrDocumentSymbolsOrNull a, SymbolInformationsOrDocumentSymbolsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d8f673282f8de161(a.SymbolInformations, b.SymbolInformations) && EqualShape_135e6f0abfdfb63b(a.DocumentSymbols, b.DocumentSymbols);
    }

    private static bool EqualShape_e6d72008fd49de7e(SymbolInformationsOrWorkspaceSymbolsOrNull a, SymbolInformationsOrWorkspaceSymbolsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d8f673282f8de161(a.SymbolInformations, b.SymbolInformations) && EqualShape_22017d3457b6fb5e(a.WorkspaceSymbols, b.WorkspaceSymbols);
    }

    private static bool EqualShape_80525ce65b460521(SymbolKind a, SymbolKind b)
    {
        return EqualityComparer<SymbolKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_977c9077c2130dba(SymbolTag a, SymbolTag b)
    {
        return EqualityComparer<SymbolTag>.Default.Equals(a, b);
    }

    private static bool EqualShape_60522a76f247ede4(TextDocumentChangeRegistrationOptions a, TextDocumentChangeRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_1259c5dfc44b321e(a.SyncKind, b.SyncKind);
    }

    private static bool EqualShape_ccc9d5a581c31fa6(TextDocumentClientCapabilities a, TextDocumentClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_099f782a33e1ec37(a.Synchronization, b.Synchronization) && EqualShape_c0722562aa9bb9ff(a.Filters, b.Filters) && EqualShape_e7da12038bd3e115(a.Completion, b.Completion) && EqualShape_5d1960c178ace368(a.Hover, b.Hover) && EqualShape_8b8480807e012f80(a.SignatureHelp, b.SignatureHelp) && EqualShape_bffa125f221ca999(a.Declaration, b.Declaration) && EqualShape_b68f902d67c631dd(a.Definition, b.Definition) && EqualShape_d9566f6cdf1ce51e(a.TypeDefinition, b.TypeDefinition) && EqualShape_b4cf53ddc6d51528(a.Implementation, b.Implementation) && EqualShape_dddb35c4f04b6359(a.References, b.References) && EqualShape_981464bac8d2e488(a.DocumentHighlight, b.DocumentHighlight) && EqualShape_a8886634269f294b(a.DocumentSymbol, b.DocumentSymbol) && EqualShape_6796dc2dfcb4b687(a.CodeAction, b.CodeAction) && EqualShape_5485c6a4027eb67a(a.CodeLens, b.CodeLens) && EqualShape_5c0964568f60471c(a.DocumentLink, b.DocumentLink) && EqualShape_a1b5026abac1822b(a.ColorProvider, b.ColorProvider) && EqualShape_b549a20e1eaf9467(a.Formatting, b.Formatting) && EqualShape_675b278ef4023caa(a.RangeFormatting, b.RangeFormatting) && EqualShape_f2551d19a058aa31(a.OnTypeFormatting, b.OnTypeFormatting) && EqualShape_a91454fb98aa7a0a(a.Rename, b.Rename) && EqualShape_ab939e84b0c28daf(a.FoldingRange, b.FoldingRange) && EqualShape_6ab85e723dea516d(a.SelectionRange, b.SelectionRange) && EqualShape_8269ed450df87f35(a.PublishDiagnostics, b.PublishDiagnostics) && EqualShape_a097ceb150983cc8(a.CallHierarchy, b.CallHierarchy) && EqualShape_e25e34bcf60ac848(a.SemanticTokens, b.SemanticTokens) && EqualShape_b10082da90551716(a.LinkedEditingRange, b.LinkedEditingRange) && EqualShape_e1cd5e0f225c96c6(a.Moniker, b.Moniker) && EqualShape_e10d80c319e7db76(a.TypeHierarchy, b.TypeHierarchy) && EqualShape_047caebfe4129ed3(a.InlineValue, b.InlineValue) && EqualShape_951aeb500d7eef88(a.InlayHint, b.InlayHint) && EqualShape_2f065e8091229dc4(a.Diagnostic, b.Diagnostic) && EqualShape_a9b342577c06553e(a.InlineCompletion, b.InlineCompletion);
    }

    private static bool EqualShape_d1c2c4faf51c15cd(TextDocumentContentChangePartial a, TextDocumentContentChangePartial b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_3f8175cc2ff18f25(a.RangeLength, b.RangeLength) && EqualShape_473287f8298dba71(a.Text, b.Text);
    }

    private static bool EqualShape_67ba06b602f43ed5(TextDocumentContentChangePartialOrWholeDocument a, TextDocumentContentChangePartialOrWholeDocument b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8d61f6c1466e1d79(a.Partial, b.Partial) && EqualShape_2d082a6bd3807513(a.WholeDocument, b.WholeDocument);
    }

    private static bool EqualShape_a785b2bcdef39e09(TextDocumentContentChangeWholeDocument a, TextDocumentContentChangeWholeDocument b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Text, b.Text);
    }

    private static bool EqualShape_8525cd90fd0e2c39(TextDocumentContentClientCapabilities a, TextDocumentContentClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_13118cef8ccea9e5(TextDocumentContentOptions a, TextDocumentContentOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Schemes, b.Schemes);
    }

    private static bool EqualShape_12d4c2faae1da2c4(TextDocumentContentOptionsOrRegistrationOptions a, TextDocumentContentOptionsOrRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1cae8ae1a7c570ea(a.Options, b.Options) && EqualShape_599369b639c1cd8a(a.RegistrationOptions, b.RegistrationOptions);
    }

    private static bool EqualShape_c01437ea23dbe5fe(TextDocumentContentParams a, TextDocumentContentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri);
    }

    private static bool EqualShape_fb320f99142648f1(TextDocumentContentRefreshParams a, TextDocumentContentRefreshParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri);
    }

    private static bool EqualShape_7f1347f4bff47aed(TextDocumentContentRegistrationOptions a, TextDocumentContentRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.Schemes, b.Schemes) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_6aee4fb4cbde55f1(TextDocumentContentResult a, TextDocumentContentResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Text, b.Text);
    }

    private static bool EqualShape_e35cf3d963f0d31d(TextDocumentEdit a, TextDocumentEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a348a1e23de5742a(a.TextDocument, b.TextDocument) && EqualShape_21a4e03bf043eb13(a.Edits, b.Edits);
    }

    private static bool EqualShape_d30cdf3f6150538e(TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile a, TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7f5935c52d83c56e(a.TextDocumentEdit, b.TextDocumentEdit) && EqualShape_20b1e7f66489189e(a.CreateFile, b.CreateFile) && EqualShape_5e2ceb6a00e2a777(a.RenameFile, b.RenameFile) && EqualShape_3bcc5a96460b21ba(a.DeleteFile, b.DeleteFile);
    }

    private static bool EqualShape_9988519c9dffc485(TextDocumentFilterClientCapabilities a, TextDocumentFilterClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.RelativePatternSupport, b.RelativePatternSupport);
    }

    private static bool EqualShape_e4dfacba18e99d73(TextDocumentFilterLanguage a, TextDocumentFilterLanguage b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Language, b.Language) && EqualShape_3e89801b52bf7f98(a.Scheme, b.Scheme) && EqualShape_f683870be53950f3(a.Pattern, b.Pattern);
    }

    private static bool EqualShape_f05eaa57d84a7b55(TextDocumentFilterLanguageOrSchemeOrPattern a, TextDocumentFilterLanguageOrSchemeOrPattern b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5e448967b43d5c68(a.Language, b.Language) && EqualShape_4ad3fb09cee37081(a.Scheme, b.Scheme) && EqualShape_579d16403267872b(a.Pattern, b.Pattern);
    }

    private static bool EqualShape_29eec48c8b924f26(TextDocumentFilterPattern a, TextDocumentFilterPattern b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Language, b.Language) && EqualShape_3e89801b52bf7f98(a.Scheme, b.Scheme) && EqualShape_555c0b23fa28c7a4(a.Pattern, b.Pattern);
    }

    private static bool EqualShape_ed1e0538451840ef(TextDocumentFilterScheme a, TextDocumentFilterScheme b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3e89801b52bf7f98(a.Language, b.Language) && EqualShape_473287f8298dba71(a.Scheme, b.Scheme) && EqualShape_f683870be53950f3(a.Pattern, b.Pattern);
    }

    private static bool EqualShape_76f80cdd6acc5d72(TextDocumentIdentifier a, TextDocumentIdentifier b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri);
    }

    private static bool EqualShape_bfe80aa9873109a2(TextDocumentItem a, TextDocumentItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_74d0ea0a98141b4c(a.LanguageId, b.LanguageId) && EqualShape_05c312945775207c(a.Version, b.Version) && EqualShape_473287f8298dba71(a.Text, b.Text);
    }

    private static bool EqualShape_3166bd6fc1b8b11c(TextDocumentPositionParams a, TextDocumentPositionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position);
    }

    private static bool EqualShape_dde1e86fd065f07c(TextDocumentRegistrationOptions a, TextDocumentRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector);
    }

    private static bool EqualShape_9ba5b7e396b1b1ca(TextDocumentSaveReason a, TextDocumentSaveReason b)
    {
        return EqualityComparer<TextDocumentSaveReason>.Default.Equals(a, b);
    }

    private static bool EqualShape_87e5fd44f56f3ab9(TextDocumentSaveRegistrationOptions a, TextDocumentSaveRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.IncludeText, b.IncludeText);
    }

    private static bool EqualShape_54b3525db15e3a53(TextDocumentSyncClientCapabilities a, TextDocumentSyncClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.WillSave, b.WillSave) && EqualShape_31f02903a9b7a4aa(a.WillSaveWaitUntil, b.WillSaveWaitUntil) && EqualShape_31f02903a9b7a4aa(a.DidSave, b.DidSave);
    }

    private static bool EqualShape_1259c5dfc44b321e(TextDocumentSyncKind a, TextDocumentSyncKind b)
    {
        return EqualityComparer<TextDocumentSyncKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_f2c5e9259b0b9679(TextDocumentSyncOptions a, TextDocumentSyncOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.OpenClose, b.OpenClose) && EqualShape_b058cd7c54da6335(a.Change, b.Change) && EqualShape_31f02903a9b7a4aa(a.WillSave, b.WillSave) && EqualShape_31f02903a9b7a4aa(a.WillSaveWaitUntil, b.WillSaveWaitUntil) && EqualShape_f7ce9c5ebf99ce16(a.Save, b.Save);
    }

    private static bool EqualShape_c80d765f99706699(TextDocumentSyncOptionsOrKind a, TextDocumentSyncOptionsOrKind b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d1d1ccd1566ac47d(a.Options, b.Options) && EqualShape_b058cd7c54da6335(a.Kind, b.Kind);
    }

    private static bool EqualShape_907ab9937a53fb02(TextEdit a, TextEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_473287f8298dba71(a.NewText, b.NewText);
    }

    private static bool EqualShape_2696e31555a9351f(TextEditOrAnnotatedTextEditOrSnippetTextEdit a, TextEditOrAnnotatedTextEditOrSnippetTextEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_56642992931a7c98(a.TextEdit, b.TextEdit) && EqualShape_5fdd995f16212fef(a.AnnotatedTextEdit, b.AnnotatedTextEdit) && EqualShape_56e45e4f5ddf42a4(a.SnippetTextEdit, b.SnippetTextEdit);
    }

    private static bool EqualShape_b3a71121264d6d6a(TextEditOrInsertReplaceEdit a, TextEditOrInsertReplaceEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_56642992931a7c98(a.TextEdit, b.TextEdit) && EqualShape_9795643b269bbb7c(a.InsertReplaceEdit, b.InsertReplaceEdit);
    }

    private static bool EqualShape_5571522b69357c5d(TextEditsOrNull a, TextEditsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_965c47d3497332b4(a.TextEdits, b.TextEdits);
    }

    private static bool EqualShape_61c52b50223120a3(TokenFormat a, TokenFormat b)
    {
        return EqualityComparer<TokenFormat>.Default.Equals(a, b);
    }

    private static bool EqualShape_98b926ad367a3bcb(TraceValue a, TraceValue b)
    {
        return EqualityComparer<TraceValue>.Default.Equals(a, b);
    }

    private static bool EqualShape_d90d2c9d9d18720a(TypeDefinitionClientCapabilities a, TypeDefinitionClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_31f02903a9b7a4aa(a.LinkSupport, b.LinkSupport);
    }

    private static bool EqualShape_edef749fe5e8e612(TypeDefinitionOptions a, TypeDefinitionOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_60ed4f843d0b5556(TypeDefinitionParams a, TypeDefinitionParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken);
    }

    private static bool EqualShape_56741666965b99ed(TypeDefinitionRegistrationOptions a, TypeDefinitionRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_2fbaa79cbc8e5da5(TypeHierarchyClientCapabilities a, TypeHierarchyClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration);
    }

    private static bool EqualShape_f1de4bc0a7efe042(TypeHierarchyItem a, TypeHierarchyItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_80525ce65b460521(a.Kind, b.Kind) && EqualShape_2a7c50a738423fd9(a.Tags, b.Tags) && EqualShape_3e89801b52bf7f98(a.Detail, b.Detail) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_5de74a81b192304c(a.@Range, b.@Range) && EqualShape_5de74a81b192304c(a.SelectionRange, b.SelectionRange) && EqualShape_93dd60b3c4ff5c3f(a.Data, b.Data);
    }

    private static bool EqualShape_f33a5bc24ebcc136(TypeHierarchyItemData a, TypeHierarchyItemData b)
    {
        return true;
    }

    private static bool EqualShape_52355c9e633f440a(TypeHierarchyItemsOrNull a, TypeHierarchyItemsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_88ee8d795f1da4c8(a.TypeHierarchyItems, b.TypeHierarchyItems);
    }

    private static bool EqualShape_af73cf291a3bbe3e(TypeHierarchyOptions a, TypeHierarchyOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_8ed2c1f6365885f3(TypeHierarchyPrepareParams a, TypeHierarchyPrepareParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_6d031af10da7a25e(a.Position, b.Position) && EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken);
    }

    private static bool EqualShape_ead4f86af75af483(TypeHierarchyRegistrationOptions a, TypeHierarchyRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c7819f785729d94c(a.DocumentSelector, b.DocumentSelector) && EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_3e89801b52bf7f98(a.Id, b.Id);
    }

    private static bool EqualShape_f8c90245f3414581(TypeHierarchySubtypesParams a, TypeHierarchySubtypesParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_daf7e9c8b5b7a1fe(a.Item, b.Item);
    }

    private static bool EqualShape_178bc5d7aa446df7(TypeHierarchySupertypesParams a, TypeHierarchySupertypesParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_daf7e9c8b5b7a1fe(a.Item, b.Item);
    }

    private static bool EqualShape_d70fbad88d0f9eaf(URI a, URI b)
    {
        return EqualityComparer<URI>.Default.Equals(a, b);
    }

    private static bool EqualShape_905a1da3c49bbf1b(UintegerOrNull a, UintegerOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_3f8175cc2ff18f25(a.Uinteger, b.Uinteger);
    }

    private static bool EqualShape_f1f7a7b99ec47d3d(UnchangedDocumentDiagnosticReport a, UnchangedDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_830ba33890174a2d(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.ResultId, b.ResultId);
    }

    private static bool EqualShape_52a1f306704bcdf2(UniquenessLevel a, UniquenessLevel b)
    {
        return EqualityComparer<UniquenessLevel>.Default.Equals(a, b);
    }

    private static bool EqualShape_21a4e4496bc03a0b(Unregistration a, Unregistration b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Id, b.Id) && EqualShape_473287f8298dba71(a.Method, b.Method);
    }

    private static bool EqualShape_5241e16167d32e95(UnregistrationParams a, UnregistrationParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_45b2da20bd40a37b(a.Unregisterations, b.Unregisterations);
    }

    private static bool EqualShape_71a22c52ef2b5944(VSClassifiedTextElement a, VSClassifiedTextElement b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_cfe6fde79ae997b2(a.Runs, b.Runs) && EqualShape_44e043791719404a(a.VSType, b.VSType);
    }

    private static bool EqualShape_29e032165608952d(VSClassifiedTextRun a, VSClassifiedTextRun b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.ClassificationTypeName, b.ClassificationTypeName) && EqualShape_473287f8298dba71(a.Text, b.Text) && EqualShape_3e89801b52bf7f98(a.MarkerTagType, b.MarkerTagType) && EqualShape_05c312945775207c(a.Style, b.Style) && EqualShape_bb0b4343e63f3503(a.VSType, b.VSType);
    }

    private static bool EqualShape_ef2ac6c5999dfb93(VSContainerElement a, VSContainerElement b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_474d572894555703(a.Style, b.Style) && EqualShape_75e4dbe21d4f69e4(a.Elements, b.Elements) && EqualShape_4bde02dc25c7843b(a.VSType, b.VSType);
    }

    private static bool EqualShape_474d572894555703(VSContainerElementStyle a, VSContainerElementStyle b)
    {
        return EqualityComparer<VSContainerElementStyle>.Default.Equals(a, b);
    }

    private static bool EqualShape_87ce8a8264759003(VSImageElement a, VSImageElement b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_6e353e7a91e3eabb(a.ImageId, b.ImageId) && EqualShape_291de7541a6e8a51(a.VSType, b.VSType);
    }

    private static bool EqualShape_e43f96073fda3c06(VSImageElementOrClassifiedTextElementOrContainerElement a, VSImageElementOrClassifiedTextElementOrContainerElement b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f8e77e12e4148327(a.ImageElement, b.ImageElement) && EqualShape_fe1808ad593e0e19(a.ClassifiedTextElement, b.ClassifiedTextElement) && EqualShape_2147172615c487bd(a.ContainerElement, b.ContainerElement);
    }

    private static bool EqualShape_b601db4f6bc4b2de(VSImageId a, VSImageId b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Guid, b.Guid) && EqualShape_05c312945775207c(a.Id, b.Id) && EqualShape_f92aabc242322d92(a.VSType, b.VSType);
    }

    private static bool EqualShape_cbb6e344e6f178f9(VSOnAutoInsertOptions a, VSOnAutoInsertOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4af101177fe720bf(a.VSTriggerCharacters, b.VSTriggerCharacters);
    }

    private static bool EqualShape_90625166f43f5eca(VSOnAutoInsertParams a, VSOnAutoInsertParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.VSTextDocument, b.VSTextDocument) && EqualShape_6d031af10da7a25e(a.VSPosition, b.VSPosition) && EqualShape_473287f8298dba71(a.VSCh, b.VSCh);
    }

    private static bool EqualShape_d38b6e9912bc25e1(VSOnAutoInsertResponseItem a, VSOnAutoInsertResponseItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0125b1afb99b0757(a.VSTextEditFormat, b.VSTextEditFormat) && EqualShape_56642992931a7c98(a.VSTextEdit, b.VSTextEdit);
    }

    private static bool EqualShape_eea9e0fe8e3bc71f(VSOnAutoInsertResponseItemOrNull a, VSOnAutoInsertResponseItemOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_7e93cf4fd6cf39d8(a.VSOnAutoInsertResponseItem, b.VSOnAutoInsertResponseItem);
    }

    private static bool EqualShape_65d84b8c94226d82(VSReferenceItem a, VSReferenceItem b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_05c312945775207c(a.VSId, b.VSId) && EqualShape_c2c7219dbdd4eab1(a.VSDefinitionId, b.VSDefinitionId) && EqualShape_e9a7ae9a4c39a84e(a.VSKind, b.VSKind) && EqualShape_15b61974b2707a7b(a.VSLocation, b.VSLocation) && EqualShape_fe1808ad593e0e19(a.VSDefinitionText, b.VSDefinitionText) && EqualShape_3e89801b52bf7f98(a.VSProjectName, b.VSProjectName) && EqualShape_3e89801b52bf7f98(a.VSContainingType, b.VSContainingType);
    }

    private static bool EqualShape_43cf43781798e7ed(VSReferenceItemsOrNull a, VSReferenceItemsOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e9b47abbd83278a9(a.VSReferenceItems, b.VSReferenceItems);
    }

    private static bool EqualShape_20bf14346b717f60(VSReferenceKind a, VSReferenceKind b)
    {
        return EqualityComparer<VSReferenceKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_bfe1d574dff86508(VersionedTextDocumentIdentifier a, VersionedTextDocumentIdentifier b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_05c312945775207c(a.Version, b.Version);
    }

    private static bool EqualShape_df60ed0d4edd21e9(WatchKind a, WatchKind b)
    {
        return EqualityComparer<WatchKind>.Default.Equals(a, b);
    }

    private static bool EqualShape_f923e9ff4f55ce6e(WillSaveTextDocumentParams a, WillSaveTextDocumentParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_76f80cdd6acc5d72(a.TextDocument, b.TextDocument) && EqualShape_9ba5b7e396b1b1ca(a.Reason, b.Reason);
    }

    private static bool EqualShape_9fd8a5a7e3a477bc(WindowClientCapabilities a, WindowClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_84aaa7c2cc391af1(a.ShowMessage, b.ShowMessage) && EqualShape_fa630b9d5505b4e0(a.ShowDocument, b.ShowDocument);
    }

    private static bool EqualShape_77eefa47f6f89dd3(WorkDoneProgressBegin a, WorkDoneProgressBegin b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_88a5baf5906d4eb9(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.Title, b.Title) && EqualShape_31f02903a9b7a4aa(a.Cancellable, b.Cancellable) && EqualShape_3e89801b52bf7f98(a.Message, b.Message) && EqualShape_3f8175cc2ff18f25(a.Percentage, b.Percentage);
    }

    private static bool EqualShape_fd6812dd893b247d(WorkDoneProgressBeginOrReportOrEnd a, WorkDoneProgressBeginOrReportOrEnd b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_731482de7fb6a532(a.Begin, b.Begin) && EqualShape_fcb01c2b2c033678(a.Report, b.Report) && EqualShape_0b7ae274031367c1(a.End, b.End);
    }

    private static bool EqualShape_fa2ceac6aff2f951(WorkDoneProgressCancelParams a, WorkDoneProgressCancelParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95e25e81cd99bf99(a.Token, b.Token);
    }

    private static bool EqualShape_876a6750f55a3e46(WorkDoneProgressCreateParams a, WorkDoneProgressCreateParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_95e25e81cd99bf99(a.Token, b.Token);
    }

    private static bool EqualShape_8e9ae8bd94490e20(WorkDoneProgressEnd a, WorkDoneProgressEnd b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_46e624b49f981eb0(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.Message, b.Message);
    }

    private static bool EqualShape_4c899c625b152feb(WorkDoneProgressOptions a, WorkDoneProgressOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress);
    }

    private static bool EqualShape_5d933d6055d6e87f(WorkDoneProgressParams a, WorkDoneProgressParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken);
    }

    private static bool EqualShape_c0b20ac9d5d09a16(WorkDoneProgressReport a, WorkDoneProgressReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_0e789aef680d8005(a.Kind, b.Kind) && EqualShape_31f02903a9b7a4aa(a.Cancellable, b.Cancellable) && EqualShape_3e89801b52bf7f98(a.Message, b.Message) && EqualShape_3f8175cc2ff18f25(a.Percentage, b.Percentage);
    }

    private static bool EqualShape_f80d356a037da136(WorkspaceClientCapabilities a, WorkspaceClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.ApplyEdit, b.ApplyEdit) && EqualShape_547589d2e00acce8(a.WorkspaceEdit, b.WorkspaceEdit) && EqualShape_db2a6c56987ef6af(a.DidChangeConfiguration, b.DidChangeConfiguration) && EqualShape_42258ccaf2d2888f(a.DidChangeWatchedFiles, b.DidChangeWatchedFiles) && EqualShape_8f41f0be4b699b48(a.Symbol, b.Symbol) && EqualShape_731174f183c7f076(a.ExecuteCommand, b.ExecuteCommand) && EqualShape_31f02903a9b7a4aa(a.WorkspaceFolders, b.WorkspaceFolders) && EqualShape_31f02903a9b7a4aa(a.Configuration, b.Configuration) && EqualShape_5a87b105e4bdfba4(a.SemanticTokens, b.SemanticTokens) && EqualShape_09bdab53659d84ac(a.CodeLens, b.CodeLens) && EqualShape_97571a077e94445f(a.FileOperations, b.FileOperations) && EqualShape_63f3d68cbd8bfa6d(a.InlineValue, b.InlineValue) && EqualShape_764eade21b3490a5(a.InlayHint, b.InlayHint) && EqualShape_10ef1d906f707e84(a.Diagnostics, b.Diagnostics) && EqualShape_4d913eb07665bbff(a.FoldingRange, b.FoldingRange) && EqualShape_6adc23222769dd63(a.TextDocumentContent, b.TextDocumentContent);
    }

    private static bool EqualShape_ee5fd540df668407(WorkspaceDiagnosticParams a, WorkspaceDiagnosticParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_3e89801b52bf7f98(a.Identifier, b.Identifier) && EqualShape_5c053116a76b9706(a.PreviousResultIds, b.PreviousResultIds);
    }

    private static bool EqualShape_68403f0f0129590d(WorkspaceDiagnosticReport a, WorkspaceDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4d8f4aff94e0369a(a.Items, b.Items);
    }

    private static bool EqualShape_8daf1506d9a2f00c(WorkspaceDiagnosticReportPartialResult a, WorkspaceDiagnosticReportPartialResult b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4d8f4aff94e0369a(a.Items, b.Items);
    }

    private static bool EqualShape_f594be26f67ebc84(WorkspaceEdit a, WorkspaceEdit b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_1c52b3755ac5b562(a.Changes, b.Changes) && EqualShape_fdc9b4b090c79264(a.DocumentChanges, b.DocumentChanges) && EqualShape_bab6c35c85f554b6(a.ChangeAnnotations, b.ChangeAnnotations);
    }

    private static bool EqualShape_f8870bd1e193be94(WorkspaceEditClientCapabilities a, WorkspaceEditClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DocumentChanges, b.DocumentChanges) && EqualShape_ba948791eff6cd24(a.ResourceOperations, b.ResourceOperations) && EqualShape_3599cdd072d7151a(a.FailureHandling, b.FailureHandling) && EqualShape_31f02903a9b7a4aa(a.NormalizesLineEndings, b.NormalizesLineEndings) && EqualShape_4e27d72802a002a5(a.ChangeAnnotationSupport, b.ChangeAnnotationSupport) && EqualShape_31f02903a9b7a4aa(a.MetadataSupport, b.MetadataSupport) && EqualShape_31f02903a9b7a4aa(a.SnippetEditSupport, b.SnippetEditSupport);
    }

    private static bool EqualShape_683af1a028c8c949(WorkspaceEditMetadata a, WorkspaceEditMetadata b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.IsRefactoring, b.IsRefactoring);
    }

    private static bool EqualShape_8ef40582cdaf80c7(WorkspaceEditOrNull a, WorkspaceEditOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_df1798df00f72a79(a.WorkspaceEdit, b.WorkspaceEdit);
    }

    private static bool EqualShape_ffec6af89eff0832(WorkspaceFolder a, WorkspaceFolder b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d70fbad88d0f9eaf(a.Uri, b.Uri) && EqualShape_473287f8298dba71(a.Name, b.Name);
    }

    private static bool EqualShape_f9f7734231f531e1(WorkspaceFolderOrURI a, WorkspaceFolderOrURI b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_415708d422f69fe5(a.WorkspaceFolder, b.WorkspaceFolder) && EqualShape_5b3003eb17f67429(a.URI, b.URI);
    }

    private static bool EqualShape_cac484af091ededa(WorkspaceFoldersChangeEvent a, WorkspaceFoldersChangeEvent b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_e365b467c8b502ca(a.Added, b.Added) && EqualShape_e365b467c8b502ca(a.Removed, b.Removed);
    }

    private static bool EqualShape_fdd23c4151a01701(WorkspaceFoldersInitializeParams a, WorkspaceFoldersInitializeParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_f62df71aba721e59(a.WorkspaceFolders, b.WorkspaceFolders);
    }

    private static bool EqualShape_a529bfd28675e22d(WorkspaceFoldersOrNull a, WorkspaceFoldersOrNull b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_d7efea5eb04decde(a.WorkspaceFolders, b.WorkspaceFolders);
    }

    private static bool EqualShape_76eae964c482175c(WorkspaceFoldersServerCapabilities a, WorkspaceFoldersServerCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.Supported, b.Supported) && EqualShape_958f7a38d91645d4(a.ChangeNotifications, b.ChangeNotifications);
    }

    private static bool EqualShape_27b2946fe8aba24d(WorkspaceFullDocumentDiagnosticReport a, WorkspaceFullDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_4a4f64afaada24e4(a.Kind, b.Kind) && EqualShape_3e89801b52bf7f98(a.ResultId, b.ResultId) && EqualShape_ce18911ddb414acc(a.Items, b.Items) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_17d3cfba51c6c525(a.Version, b.Version);
    }

    private static bool EqualShape_04b29b87f373e8dd(WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport a, WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_a2796bb989312de1(a.FullDocumentDiagnosticReport, b.FullDocumentDiagnosticReport) && EqualShape_16143ebe25c1037b(a.UnchangedDocumentDiagnosticReport, b.UnchangedDocumentDiagnosticReport);
    }

    private static bool EqualShape_891102cd69b5f6f3(WorkspaceOptions a, WorkspaceOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_8e9369d40b64e986(a.WorkspaceFolders, b.WorkspaceFolders) && EqualShape_05252cee26410791(a.FileOperations, b.FileOperations) && EqualShape_4fed6769aec47ce0(a.TextDocumentContent, b.TextDocumentContent);
    }

    private static bool EqualShape_e3f9d3600b02eeb1(WorkspaceSymbol a, WorkspaceSymbol b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_473287f8298dba71(a.Name, b.Name) && EqualShape_80525ce65b460521(a.Kind, b.Kind) && EqualShape_2a7c50a738423fd9(a.Tags, b.Tags) && EqualShape_3e89801b52bf7f98(a.ContainerName, b.ContainerName) && EqualShape_fb622e0b8d2bf184(a.Location, b.Location) && EqualShape_ddca5fcfafe66f28(a.Data, b.Data);
    }

    private static bool EqualShape_a5728d8363acabeb(WorkspaceSymbolClientCapabilities a, WorkspaceSymbolClientCapabilities b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.DynamicRegistration, b.DynamicRegistration) && EqualShape_ef83c3195ef91327(a.SymbolKind, b.SymbolKind) && EqualShape_56b6c5f1c4a81226(a.TagSupport, b.TagSupport) && EqualShape_9a1297d4a6494114(a.ResolveSupport, b.ResolveSupport);
    }

    private static bool EqualShape_d6eb674f2d558af4(WorkspaceSymbolData a, WorkspaceSymbolData b)
    {
        return true;
    }

    private static bool EqualShape_64a1fc35c89fbeab(WorkspaceSymbolOptions a, WorkspaceSymbolOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_f45d058e1512eabf(WorkspaceSymbolParams a, WorkspaceSymbolParams b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_c3d8a0302fb53f84(a.WorkDoneToken, b.WorkDoneToken) && EqualShape_c3d8a0302fb53f84(a.PartialResultToken, b.PartialResultToken) && EqualShape_473287f8298dba71(a.Query, b.Query) && EqualShape_580176b81ce49f09(a.TextDocument, b.TextDocument);
    }

    private static bool EqualShape_dbb4c03cff5a9b2c(WorkspaceSymbolRegistrationOptions a, WorkspaceSymbolRegistrationOptions b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_31f02903a9b7a4aa(a.WorkDoneProgress, b.WorkDoneProgress) && EqualShape_31f02903a9b7a4aa(a.ResolveProvider, b.ResolveProvider);
    }

    private static bool EqualShape_c1a1987ea31ba882(WorkspaceUnchangedDocumentDiagnosticReport a, WorkspaceUnchangedDocumentDiagnosticReport b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return EqualShape_830ba33890174a2d(a.Kind, b.Kind) && EqualShape_473287f8298dba71(a.ResultId, b.ResultId) && EqualShape_b2094f714cc94ba3(a.Uri, b.Uri) && EqualShape_17d3cfba51c6c525(a.Version, b.Version);
    }

    private static bool EqualShape_2d5dae07671f98b7(Slice<uint> a, Slice<uint> b)
    {
        for (int index = 0; index < 2; index++)
            if (!EqualShape_1c06d81634ed9a2e(a.IsNil ? default : a[index], b.IsNil ? default : b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_c60a33add77739d5(Slice<CallHierarchyIncomingCall?> a, Slice<CallHierarchyIncomingCall?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_41e3a0bbc0502807(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_9b09d17f086528f6(Slice<CallHierarchyItem?> a, Slice<CallHierarchyItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_52678145e6f3309d(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_2c31c997f33109f1(Slice<CallHierarchyOutgoingCall?> a, Slice<CallHierarchyOutgoingCall?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_1e0ec82d8ba4bc44(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_b7ce3c701164809b(Slice<CodeActionKindDocumentation?> a, Slice<CodeActionKindDocumentation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_56c96d7eb180a250(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_70ab3b05fe66ce10(Slice<CodeLens?> a, Slice<CodeLens?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_49a1f25275c46426(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_5c253ffb73d0bac8(Slice<ColorInformation?> a, Slice<ColorInformation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_a16099c806419f32(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_89f8227ba96a2f1b(Slice<ColorPresentation?> a, Slice<ColorPresentation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_0d923cd5be75d1dd(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_72b625abb0de2281(Slice<CompletionItem?> a, Slice<CompletionItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_cf50404157ead35b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_3f0803b0c56b9ff5(Slice<ConfigurationItem?> a, Slice<ConfigurationItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_7cc978bb2dea8225(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_c2b56fcb6b7cc23e(Slice<ContentMapperContribution?> a, Slice<ContentMapperContribution?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_7f829fb51f96000f(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_ce18911ddb414acc(Slice<Diagnostic?> a, Slice<Diagnostic?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_cfb0101e50941834(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_1559cac8798c0fa0(Slice<DiagnosticRelatedInformation?> a, Slice<DiagnosticRelatedInformation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_fd0503f33542b10c(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_671d1014e749357c(Slice<DocumentHighlight?> a, Slice<DocumentHighlight?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_23150a029e73ef27(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_2b3264d438c5cb67(Slice<DocumentLink?> a, Slice<DocumentLink?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_1340a620b0fd7d81(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_bf7087ec6c4d58fb(Slice<DocumentSymbol?> a, Slice<DocumentSymbol?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_6146aa6b9c506765(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_dc44ad1109493e27(Slice<FileCreate?> a, Slice<FileCreate?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_f99255a19aeb440c(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_0dec4654e36c7064(Slice<FileDelete?> a, Slice<FileDelete?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_6d7945c3a568dcd5(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_76b4ca7f821c8fc5(Slice<FileEvent?> a, Slice<FileEvent?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_47f24aa2d05bbb16(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_836ac2ca4c2fab5f(Slice<FileOperationFilter?> a, Slice<FileOperationFilter?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_4f9a8694ec35690b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_2f05961eae7e619c(Slice<FileRename?> a, Slice<FileRename?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_0a97aeac6dfe4cd0(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_34e5d5df1af6102f(Slice<FileSystemWatcher?> a, Slice<FileSystemWatcher?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_243be7733f7be22a(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4a8a3bbd5078844b(Slice<FoldingRange?> a, Slice<FoldingRange?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_c1f115ff19a883a0(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_0d3017096d66f3b1(Slice<InlayHint?> a, Slice<InlayHint?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_07b46c42ab1a924b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_ba427d4ac914714f(Slice<InlayHintLabelPart?> a, Slice<InlayHintLabelPart?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_e114c64cb251e202(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_2ac7ed000ec4e5b2(Slice<InlineCompletionItem?> a, Slice<InlineCompletionItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_3c57f532ec15eb7a(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4ff9339b88523bfc(Slice<LocationLink?> a, Slice<LocationLink?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_1e631912e1fd4f05(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_d053cdadfa7a7223(Slice<MessageActionItem?> a, Slice<MessageActionItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_505239fc3470cbd5(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_cff0bac0044f0266(Slice<Moniker?> a, Slice<Moniker?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_16a1534e4208ae3b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_865bb029c4ea44b0(Slice<MultiDocumentHighlight?> a, Slice<MultiDocumentHighlight?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_d778570f7ada035b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_324bfc5f74dc2198(Slice<ParameterInformation?> a, Slice<ParameterInformation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_445e57e999b361cf(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_6d415e5613568a1b(Slice<Registration?> a, Slice<Registration?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_c48c035a6051381e(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_a5afe4cc8f9d19b3(Slice<SelectionRange?> a, Slice<SelectionRange?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_09de083f42750e75(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_2a6767a930bd40f7(Slice<SemanticTokensEdit?> a, Slice<SemanticTokensEdit?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_6b0fc3a769e2d57c(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_2df1476a21b7b317(Slice<SignatureInformation?> a, Slice<SignatureInformation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_090b50bb170c7d95(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_be4b95d73d017aee(Slice<SymbolInformation?> a, Slice<SymbolInformation?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_7de106dad6f98cbc(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_7326b970e1272421(Slice<TextEdit?> a, Slice<TextEdit?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_56642992931a7c98(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_3be68171a26cc119(Slice<TypeHierarchyItem?> a, Slice<TypeHierarchyItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_daf7e9c8b5b7a1fe(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_45b2da20bd40a37b(Slice<Unregistration?> a, Slice<Unregistration?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_45b251721cf796c5(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_cfe6fde79ae997b2(Slice<VSClassifiedTextRun?> a, Slice<VSClassifiedTextRun?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_f1616ff6438f019f(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_55cf654fd8b1bd53(Slice<VSReferenceItem?> a, Slice<VSReferenceItem?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_7d06f0b6619a5404(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_e365b467c8b502ca(Slice<WorkspaceFolder?> a, Slice<WorkspaceFolder?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_415708d422f69fe5(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_f23180af26841dd8(Slice<WorkspaceSymbol?> a, Slice<WorkspaceSymbol?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_a563895c23d4de71(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_1dc84ac3dcf6749a(Slice<CodeActionKind> a, Slice<CodeActionKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_ecff755ce44421bb(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_f7e208d55963916d(Slice<CodeActionTag> a, Slice<CodeActionTag> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_22fd923137cdbcf0(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4109991f4006b883(Slice<CommandOrCodeAction> a, Slice<CommandOrCodeAction> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_58a4b3825288a73b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_78ac88b4cb4bbbef(Slice<CompletionItemKind> a, Slice<CompletionItemKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_b8f2471eb8fc0cb3(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_ea1bedafe91c8e42(Slice<CompletionItemTag> a, Slice<CompletionItemTag> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_5397ab5a342cc703(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_d1a5fcd9177b1e80(Slice<DiagnosticTag> a, Slice<DiagnosticTag> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_1a56dbf398d99d2f(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_7789c42f95c3f703(Slice<DocumentUri> a, Slice<DocumentUri> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_b2094f714cc94ba3(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_7ecdd9acf300e742(Slice<FoldingRangeKind> a, Slice<FoldingRangeKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_9bed5894ab8b426e(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_00bafde87caef9b1(Slice<InlineValueTextOrVariableLookupOrEvaluatableExpression> a, Slice<InlineValueTextOrVariableLookupOrEvaluatableExpression> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_9b1858affa016849(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_39d79f06277c11aa(Slice<InsertTextMode> a, Slice<InsertTextMode> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_6cddb8f12d636f30(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_5610d1960004ba0d(Slice<Location> a, Slice<Location> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_15b61974b2707a7b(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_1599a20ab951c327(Slice<MarkupKind> a, Slice<MarkupKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_2ecdda95186829dd(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_45419e3f2e1b1343(Slice<Position> a, Slice<Position> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_6d031af10da7a25e(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_c901c7a8c1b9d477(Slice<PositionEncodingKind> a, Slice<PositionEncodingKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_996a119923a77207(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_5c053116a76b9706(Slice<PreviousResultId> a, Slice<PreviousResultId> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_2d2e58655dd72e85(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_395c34a167937420(Slice<Range> a, Slice<Range> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_5de74a81b192304c(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_dac512c95e2386c0(Slice<ResourceOperationKind> a, Slice<ResourceOperationKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_3eec28594f7be075(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_87d8fc0cbda71f19(Slice<StringOrMarkedStringWithLanguage> a, Slice<StringOrMarkedStringWithLanguage> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_ae8e2688e3b7bd25(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_07349dd2472a64ed(Slice<SymbolKind> a, Slice<SymbolKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_80525ce65b460521(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_0a862cf9a5f1866e(Slice<SymbolTag> a, Slice<SymbolTag> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_977c9077c2130dba(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_f4ba3b7d663ee20d(Slice<TextDocumentContentChangePartialOrWholeDocument> a, Slice<TextDocumentContentChangePartialOrWholeDocument> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_67ba06b602f43ed5(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4d5a6be4cf57fad6(Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile> a, Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_d30cdf3f6150538e(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_afceee5ac36382a4(Slice<TextDocumentFilterLanguageOrSchemeOrPattern> a, Slice<TextDocumentFilterLanguageOrSchemeOrPattern> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_f05eaa57d84a7b55(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_91ce3736d1dc6bcf(Slice<TextDocumentIdentifier> a, Slice<TextDocumentIdentifier> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_76f80cdd6acc5d72(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_21a4e03bf043eb13(Slice<TextEditOrAnnotatedTextEditOrSnippetTextEdit> a, Slice<TextEditOrAnnotatedTextEditOrSnippetTextEdit> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_2696e31555a9351f(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_20603491c5ccd12d(Slice<TokenFormat> a, Slice<TokenFormat> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_61c52b50223120a3(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_75e4dbe21d4f69e4(Slice<VSImageElementOrClassifiedTextElementOrContainerElement> a, Slice<VSImageElementOrClassifiedTextElementOrContainerElement> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_e43f96073fda3c06(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_638cf2a0f3f3a811(Slice<VSReferenceKind> a, Slice<VSReferenceKind> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_20bf14346b717f60(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4d8f4aff94e0369a(Slice<WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport> a, Slice<WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_04b29b87f373e8dd(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_6b4e60b92db020cb(Slice<object?> a, Slice<object?> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_d6a7cd2a7371b1a1(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4af101177fe720bf(Slice<ByteStringValue> a, Slice<ByteStringValue> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_473287f8298dba71(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_4c1cc0279aa62acf(Slice<uint> a, Slice<uint> b)
    {
        if (a.IsNil != b.IsNil || a.Length != b.Length)
            return false;
        for (int index = 0; index < a.Length; index++)
            if (!EqualShape_1c06d81634ed9a2e(a[index], b[index]))
                return false;
        return true;
    }

    private static bool EqualShape_d6a7cd2a7371b1a1(object? a, object? b)
    {
        return EqualAny(a, b);
    }

    private static bool EqualShape_b760f44fa5965c24(bool a, bool b)
    {
        return EqualityComparer<bool>.Default.Equals(a, b);
    }

    private static bool EqualShape_6bd2a66c4467bc37(double a, double b)
    {
        return a == b;
    }

    private static bool EqualShape_05c312945775207c(int a, int b)
    {
        return EqualityComparer<int>.Default.Equals(a, b);
    }

    private static bool EqualShape_65e0d82b4a75d616(ProtocolMap<DocumentUri, FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport> a, ProtocolMap<DocumentUri, FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport> b)
    {
        if (a.IsNil != b.IsNil || a.Count != b.Count)
            return false;
        foreach (var entry in a)
            if (!b.TryGetValue(entry.Key, out var value) || !EqualShape_122188e5e315b8c0(entry.Value, value))
                return false;
        return true;
    }

    private static bool EqualShape_eadc07a4a900af66(ProtocolMap<DocumentUri, Slice<TextEdit?>> a, ProtocolMap<DocumentUri, Slice<TextEdit?>> b)
    {
        if (a.IsNil != b.IsNil || a.Count != b.Count)
            return false;
        foreach (var entry in a)
            if (!b.TryGetValue(entry.Key, out var value) || !EqualShape_7326b970e1272421(entry.Value, value))
                return false;
        return true;
    }

    private static bool EqualShape_b30c5bf781ea434f(ProtocolMap<ByteStringKey, ChangeAnnotation?> a, ProtocolMap<ByteStringKey, ChangeAnnotation?> b)
    {
        if (a.IsNil != b.IsNil || a.Count != b.Count)
            return false;
        foreach (var entry in a)
            if (!b.TryGetValue(entry.Key, out var value) || !EqualShape_6a13e7db61712509(entry.Value, value))
                return false;
        return true;
    }

    private static bool EqualShape_26c0c2adc8ab309a(ProtocolMap<ByteStringKey, object?> a, ProtocolMap<ByteStringKey, object?> b)
    {
        if (a.IsNil != b.IsNil || a.Count != b.Count)
            return false;
        foreach (var entry in a)
            if (!b.TryGetValue(entry.Key, out var value) || !EqualShape_d6a7cd2a7371b1a1(entry.Value, value))
                return false;
        return true;
    }

    private static bool EqualShape_219dd46d9c2a4a1d(ProtocolMap<ByteStringKey, ByteStringValue> a, ProtocolMap<ByteStringKey, ByteStringValue> b)
    {
        if (a.IsNil != b.IsNil || a.Count != b.Count)
            return false;
        foreach (var entry in a)
            if (!b.TryGetValue(entry.Key, out var value) || !EqualShape_473287f8298dba71(entry.Value, value))
                return false;
        return true;
    }

    private static bool EqualShape_473287f8298dba71(ByteStringValue a, ByteStringValue b)
    {
        return a.EqualsValue(b);
    }

    private static bool EqualShape_0d0ad1bdf1affd9c(EmptyProtocolObject a, EmptyProtocolObject b)
    {
        return true;
    }

    private static bool EqualShape_1c06d81634ed9a2e(uint a, uint b)
    {
        return EqualityComparer<uint>.Default.Equals(a, b);
    }

    private static bool EqualAny(object? a, object? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        switch (a)
        {
            case string text:
                return b is string otherText && text == otherText;
            case bool value:
                return b is bool otherBoolean && value == otherBoolean;
            case double value:
                return b is double otherNumber && value == otherNumber;
            case Dictionary<string, object?> map:
                if (b is not Dictionary<string, object?> otherMap || map.Count != otherMap.Count)
                    return false;
                foreach (var entry in map)
                    if (!otherMap.TryGetValue(entry.Key, out var value) || !EqualAny(entry.Value, value))
                        return false;
                return true;
            case List<object?> array:
                if (b is not List<object?> otherArray || array.Count != otherArray.Count)
                    return false;
                for (int index = 0; index < array.Count; index++)
                    if (!EqualAny(array[index], otherArray[index]))
                        return false;
                return true;
            case ImplementationParams value:
                return b is ImplementationParams otherImplementationParams && EqualShape_3126943fdda20947(value, otherImplementationParams);
            case Location value:
                return b is Location otherLocation && EqualShape_15b61974b2707a7b(value, otherLocation);
            case ImplementationRegistrationOptions value:
                return b is ImplementationRegistrationOptions otherImplementationRegistrationOptions && EqualShape_999302bf98018b4a(value, otherImplementationRegistrationOptions);
            case TypeDefinitionParams value:
                return b is TypeDefinitionParams otherTypeDefinitionParams && EqualShape_60ed4f843d0b5556(value, otherTypeDefinitionParams);
            case TypeDefinitionRegistrationOptions value:
                return b is TypeDefinitionRegistrationOptions otherTypeDefinitionRegistrationOptions && EqualShape_56741666965b99ed(value, otherTypeDefinitionRegistrationOptions);
            case WorkspaceFolder value:
                return b is WorkspaceFolder otherWorkspaceFolder && EqualShape_ffec6af89eff0832(value, otherWorkspaceFolder);
            case DidChangeWorkspaceFoldersParams value:
                return b is DidChangeWorkspaceFoldersParams otherDidChangeWorkspaceFoldersParams && EqualShape_3814f51204c3cfe2(value, otherDidChangeWorkspaceFoldersParams);
            case ConfigurationParams value:
                return b is ConfigurationParams otherConfigurationParams && EqualShape_99d81d86d0860ed1(value, otherConfigurationParams);
            case DocumentColorParams value:
                return b is DocumentColorParams otherDocumentColorParams && EqualShape_cbb3073d36672b63(value, otherDocumentColorParams);
            case ColorInformation value:
                return b is ColorInformation otherColorInformation && EqualShape_d5d2d1066d39a9f1(value, otherColorInformation);
            case DocumentColorRegistrationOptions value:
                return b is DocumentColorRegistrationOptions otherDocumentColorRegistrationOptions && EqualShape_b2588d25e0144d45(value, otherDocumentColorRegistrationOptions);
            case ColorPresentationParams value:
                return b is ColorPresentationParams otherColorPresentationParams && EqualShape_ef2f7018da26a917(value, otherColorPresentationParams);
            case ColorPresentation value:
                return b is ColorPresentation otherColorPresentation && EqualShape_1f1f6c1ccc446b5b(value, otherColorPresentation);
            case FoldingRangeParams value:
                return b is FoldingRangeParams otherFoldingRangeParams && EqualShape_e4340b98efb17abe(value, otherFoldingRangeParams);
            case FoldingRange value:
                return b is FoldingRange otherFoldingRange && EqualShape_66fbdf0fd1929f3b(value, otherFoldingRange);
            case FoldingRangeRegistrationOptions value:
                return b is FoldingRangeRegistrationOptions otherFoldingRangeRegistrationOptions && EqualShape_2465fd48699a0885(value, otherFoldingRangeRegistrationOptions);
            case DeclarationParams value:
                return b is DeclarationParams otherDeclarationParams && EqualShape_67e2762ff01d380a(value, otherDeclarationParams);
            case DeclarationRegistrationOptions value:
                return b is DeclarationRegistrationOptions otherDeclarationRegistrationOptions && EqualShape_64b3f4521813f6e3(value, otherDeclarationRegistrationOptions);
            case SelectionRangeParams value:
                return b is SelectionRangeParams otherSelectionRangeParams && EqualShape_0b27b0baa03194cf(value, otherSelectionRangeParams);
            case SelectionRange value:
                return b is SelectionRange otherSelectionRange && EqualShape_7d7b70e7ccd8dfec(value, otherSelectionRange);
            case SelectionRangeRegistrationOptions value:
                return b is SelectionRangeRegistrationOptions otherSelectionRangeRegistrationOptions && EqualShape_1299af8f490cf41a(value, otherSelectionRangeRegistrationOptions);
            case WorkDoneProgressCreateParams value:
                return b is WorkDoneProgressCreateParams otherWorkDoneProgressCreateParams && EqualShape_876a6750f55a3e46(value, otherWorkDoneProgressCreateParams);
            case WorkDoneProgressCancelParams value:
                return b is WorkDoneProgressCancelParams otherWorkDoneProgressCancelParams && EqualShape_fa2ceac6aff2f951(value, otherWorkDoneProgressCancelParams);
            case CallHierarchyPrepareParams value:
                return b is CallHierarchyPrepareParams otherCallHierarchyPrepareParams && EqualShape_0c095bc834e4d3c8(value, otherCallHierarchyPrepareParams);
            case CallHierarchyItem value:
                return b is CallHierarchyItem otherCallHierarchyItem && EqualShape_8c7fd0716410365c(value, otherCallHierarchyItem);
            case CallHierarchyRegistrationOptions value:
                return b is CallHierarchyRegistrationOptions otherCallHierarchyRegistrationOptions && EqualShape_1f08ef49b14db62c(value, otherCallHierarchyRegistrationOptions);
            case CallHierarchyIncomingCallsParams value:
                return b is CallHierarchyIncomingCallsParams otherCallHierarchyIncomingCallsParams && EqualShape_3a8cdb686bf71f77(value, otherCallHierarchyIncomingCallsParams);
            case CallHierarchyIncomingCall value:
                return b is CallHierarchyIncomingCall otherCallHierarchyIncomingCall && EqualShape_e184a9b2f92e1a06(value, otherCallHierarchyIncomingCall);
            case CallHierarchyOutgoingCallsParams value:
                return b is CallHierarchyOutgoingCallsParams otherCallHierarchyOutgoingCallsParams && EqualShape_683273adad7a85de(value, otherCallHierarchyOutgoingCallsParams);
            case CallHierarchyOutgoingCall value:
                return b is CallHierarchyOutgoingCall otherCallHierarchyOutgoingCall && EqualShape_4589918246a73c8d(value, otherCallHierarchyOutgoingCall);
            case SemanticTokensParams value:
                return b is SemanticTokensParams otherSemanticTokensParams && EqualShape_8bf9ad4f5cef6cfa(value, otherSemanticTokensParams);
            case SemanticTokens value:
                return b is SemanticTokens otherSemanticTokens && EqualShape_448a954da33d9419(value, otherSemanticTokens);
            case SemanticTokensPartialResult value:
                return b is SemanticTokensPartialResult otherSemanticTokensPartialResult && EqualShape_92f2d270bac051fa(value, otherSemanticTokensPartialResult);
            case SemanticTokensRegistrationOptions value:
                return b is SemanticTokensRegistrationOptions otherSemanticTokensRegistrationOptions && EqualShape_4b3cdaefc55e634e(value, otherSemanticTokensRegistrationOptions);
            case SemanticTokensDeltaParams value:
                return b is SemanticTokensDeltaParams otherSemanticTokensDeltaParams && EqualShape_9101edacad945287(value, otherSemanticTokensDeltaParams);
            case SemanticTokensDelta value:
                return b is SemanticTokensDelta otherSemanticTokensDelta && EqualShape_1c8ca776704f95d7(value, otherSemanticTokensDelta);
            case SemanticTokensDeltaPartialResult value:
                return b is SemanticTokensDeltaPartialResult otherSemanticTokensDeltaPartialResult && EqualShape_236ed7986274e517(value, otherSemanticTokensDeltaPartialResult);
            case SemanticTokensRangeParams value:
                return b is SemanticTokensRangeParams otherSemanticTokensRangeParams && EqualShape_0dc62352c8c25887(value, otherSemanticTokensRangeParams);
            case ShowDocumentParams value:
                return b is ShowDocumentParams otherShowDocumentParams && EqualShape_7e2bf70a83c07533(value, otherShowDocumentParams);
            case ShowDocumentResult value:
                return b is ShowDocumentResult otherShowDocumentResult && EqualShape_c44a39c887303450(value, otherShowDocumentResult);
            case LinkedEditingRangeParams value:
                return b is LinkedEditingRangeParams otherLinkedEditingRangeParams && EqualShape_7aa30078fed4b566(value, otherLinkedEditingRangeParams);
            case LinkedEditingRanges value:
                return b is LinkedEditingRanges otherLinkedEditingRanges && EqualShape_42b657f1a5deeca0(value, otherLinkedEditingRanges);
            case LinkedEditingRangeRegistrationOptions value:
                return b is LinkedEditingRangeRegistrationOptions otherLinkedEditingRangeRegistrationOptions && EqualShape_89439e1c9f8204ac(value, otherLinkedEditingRangeRegistrationOptions);
            case CreateFilesParams value:
                return b is CreateFilesParams otherCreateFilesParams && EqualShape_c1b6632f7bfe8d76(value, otherCreateFilesParams);
            case WorkspaceEdit value:
                return b is WorkspaceEdit otherWorkspaceEdit && EqualShape_f594be26f67ebc84(value, otherWorkspaceEdit);
            case FileOperationRegistrationOptions value:
                return b is FileOperationRegistrationOptions otherFileOperationRegistrationOptions && EqualShape_1cbbf99920573dca(value, otherFileOperationRegistrationOptions);
            case RenameFilesParams value:
                return b is RenameFilesParams otherRenameFilesParams && EqualShape_2b49debd9d4868a5(value, otherRenameFilesParams);
            case DeleteFilesParams value:
                return b is DeleteFilesParams otherDeleteFilesParams && EqualShape_54b8495600617e2e(value, otherDeleteFilesParams);
            case MonikerParams value:
                return b is MonikerParams otherMonikerParams && EqualShape_dce655840bd2b665(value, otherMonikerParams);
            case Moniker value:
                return b is Moniker otherMoniker && EqualShape_c59fb88a060d1bc0(value, otherMoniker);
            case MonikerRegistrationOptions value:
                return b is MonikerRegistrationOptions otherMonikerRegistrationOptions && EqualShape_01d385a3539e6586(value, otherMonikerRegistrationOptions);
            case TypeHierarchyPrepareParams value:
                return b is TypeHierarchyPrepareParams otherTypeHierarchyPrepareParams && EqualShape_8ed2c1f6365885f3(value, otherTypeHierarchyPrepareParams);
            case TypeHierarchyItem value:
                return b is TypeHierarchyItem otherTypeHierarchyItem && EqualShape_f1de4bc0a7efe042(value, otherTypeHierarchyItem);
            case TypeHierarchyRegistrationOptions value:
                return b is TypeHierarchyRegistrationOptions otherTypeHierarchyRegistrationOptions && EqualShape_ead4f86af75af483(value, otherTypeHierarchyRegistrationOptions);
            case TypeHierarchySupertypesParams value:
                return b is TypeHierarchySupertypesParams otherTypeHierarchySupertypesParams && EqualShape_178bc5d7aa446df7(value, otherTypeHierarchySupertypesParams);
            case TypeHierarchySubtypesParams value:
                return b is TypeHierarchySubtypesParams otherTypeHierarchySubtypesParams && EqualShape_f8c90245f3414581(value, otherTypeHierarchySubtypesParams);
            case InlineValueParams value:
                return b is InlineValueParams otherInlineValueParams && EqualShape_c9023b2dc10087cf(value, otherInlineValueParams);
            case InlineValueRegistrationOptions value:
                return b is InlineValueRegistrationOptions otherInlineValueRegistrationOptions && EqualShape_5852b2dfb2cf7003(value, otherInlineValueRegistrationOptions);
            case InlayHintParams value:
                return b is InlayHintParams otherInlayHintParams && EqualShape_3e20fc5d07945082(value, otherInlayHintParams);
            case InlayHint value:
                return b is InlayHint otherInlayHint && EqualShape_551be06dac20fdd0(value, otherInlayHint);
            case InlayHintRegistrationOptions value:
                return b is InlayHintRegistrationOptions otherInlayHintRegistrationOptions && EqualShape_5736a71dddfa55b8(value, otherInlayHintRegistrationOptions);
            case DocumentDiagnosticParams value:
                return b is DocumentDiagnosticParams otherDocumentDiagnosticParams && EqualShape_7548490d28d56df8(value, otherDocumentDiagnosticParams);
            case DiagnosticServerCancellationData value:
                return b is DiagnosticServerCancellationData otherDiagnosticServerCancellationData && EqualShape_aefa91d53756e143(value, otherDiagnosticServerCancellationData);
            case DiagnosticRegistrationOptions value:
                return b is DiagnosticRegistrationOptions otherDiagnosticRegistrationOptions && EqualShape_a14be8cd84f4acb8(value, otherDiagnosticRegistrationOptions);
            case WorkspaceDiagnosticParams value:
                return b is WorkspaceDiagnosticParams otherWorkspaceDiagnosticParams && EqualShape_ee5fd540df668407(value, otherWorkspaceDiagnosticParams);
            case WorkspaceDiagnosticReport value:
                return b is WorkspaceDiagnosticReport otherWorkspaceDiagnosticReport && EqualShape_68403f0f0129590d(value, otherWorkspaceDiagnosticReport);
            case WorkspaceDiagnosticReportPartialResult value:
                return b is WorkspaceDiagnosticReportPartialResult otherWorkspaceDiagnosticReportPartialResult && EqualShape_8daf1506d9a2f00c(value, otherWorkspaceDiagnosticReportPartialResult);
            case InlineCompletionParams value:
                return b is InlineCompletionParams otherInlineCompletionParams && EqualShape_d9c2c29e86a56320(value, otherInlineCompletionParams);
            case InlineCompletionList value:
                return b is InlineCompletionList otherInlineCompletionList && EqualShape_433b2e6a3a3c27cc(value, otherInlineCompletionList);
            case InlineCompletionItem value:
                return b is InlineCompletionItem otherInlineCompletionItem && EqualShape_5f63db4527f32b9c(value, otherInlineCompletionItem);
            case InlineCompletionRegistrationOptions value:
                return b is InlineCompletionRegistrationOptions otherInlineCompletionRegistrationOptions && EqualShape_e1b6c72c88dbb66d(value, otherInlineCompletionRegistrationOptions);
            case TextDocumentContentParams value:
                return b is TextDocumentContentParams otherTextDocumentContentParams && EqualShape_c01437ea23dbe5fe(value, otherTextDocumentContentParams);
            case TextDocumentContentResult value:
                return b is TextDocumentContentResult otherTextDocumentContentResult && EqualShape_6aee4fb4cbde55f1(value, otherTextDocumentContentResult);
            case TextDocumentContentRegistrationOptions value:
                return b is TextDocumentContentRegistrationOptions otherTextDocumentContentRegistrationOptions && EqualShape_7f1347f4bff47aed(value, otherTextDocumentContentRegistrationOptions);
            case TextDocumentContentRefreshParams value:
                return b is TextDocumentContentRefreshParams otherTextDocumentContentRefreshParams && EqualShape_fb320f99142648f1(value, otherTextDocumentContentRefreshParams);
            case RegistrationParams value:
                return b is RegistrationParams otherRegistrationParams && EqualShape_1b5a140ba3379372(value, otherRegistrationParams);
            case UnregistrationParams value:
                return b is UnregistrationParams otherUnregistrationParams && EqualShape_5241e16167d32e95(value, otherUnregistrationParams);
            case InitializeParams value:
                return b is InitializeParams otherInitializeParams && EqualShape_6cabbf134c6b5195(value, otherInitializeParams);
            case InitializeResult value:
                return b is InitializeResult otherInitializeResult && EqualShape_ff2c54bb0124e1e5(value, otherInitializeResult);
            case InitializeError value:
                return b is InitializeError otherInitializeError && EqualShape_d9e9313e42a0d6c0(value, otherInitializeError);
            case InitializedParams value:
                return b is InitializedParams otherInitializedParams && EqualShape_77f425e16948cb7b(value, otherInitializedParams);
            case DidChangeConfigurationParams value:
                return b is DidChangeConfigurationParams otherDidChangeConfigurationParams && EqualShape_7f22677bab49bf26(value, otherDidChangeConfigurationParams);
            case DidChangeConfigurationRegistrationOptions value:
                return b is DidChangeConfigurationRegistrationOptions otherDidChangeConfigurationRegistrationOptions && EqualShape_15c1f1ed9bdde479(value, otherDidChangeConfigurationRegistrationOptions);
            case ShowMessageParams value:
                return b is ShowMessageParams otherShowMessageParams && EqualShape_663434b32ed8ff88(value, otherShowMessageParams);
            case ShowMessageRequestParams value:
                return b is ShowMessageRequestParams otherShowMessageRequestParams && EqualShape_11bb59cd26639030(value, otherShowMessageRequestParams);
            case MessageActionItem value:
                return b is MessageActionItem otherMessageActionItem && EqualShape_0627c9b9dd315342(value, otherMessageActionItem);
            case LogMessageParams value:
                return b is LogMessageParams otherLogMessageParams && EqualShape_8e5a686c4d7af637(value, otherLogMessageParams);
            case DidOpenTextDocumentParams value:
                return b is DidOpenTextDocumentParams otherDidOpenTextDocumentParams && EqualShape_e938ee27d893dd4c(value, otherDidOpenTextDocumentParams);
            case TextDocumentRegistrationOptions value:
                return b is TextDocumentRegistrationOptions otherTextDocumentRegistrationOptions && EqualShape_dde1e86fd065f07c(value, otherTextDocumentRegistrationOptions);
            case DidChangeTextDocumentParams value:
                return b is DidChangeTextDocumentParams otherDidChangeTextDocumentParams && EqualShape_0c6ffd2fc1fd5265(value, otherDidChangeTextDocumentParams);
            case TextDocumentChangeRegistrationOptions value:
                return b is TextDocumentChangeRegistrationOptions otherTextDocumentChangeRegistrationOptions && EqualShape_60522a76f247ede4(value, otherTextDocumentChangeRegistrationOptions);
            case DidCloseTextDocumentParams value:
                return b is DidCloseTextDocumentParams otherDidCloseTextDocumentParams && EqualShape_a93e90edfeeab584(value, otherDidCloseTextDocumentParams);
            case DidSaveTextDocumentParams value:
                return b is DidSaveTextDocumentParams otherDidSaveTextDocumentParams && EqualShape_30e8b3b731a2add1(value, otherDidSaveTextDocumentParams);
            case TextDocumentSaveRegistrationOptions value:
                return b is TextDocumentSaveRegistrationOptions otherTextDocumentSaveRegistrationOptions && EqualShape_87e5fd44f56f3ab9(value, otherTextDocumentSaveRegistrationOptions);
            case WillSaveTextDocumentParams value:
                return b is WillSaveTextDocumentParams otherWillSaveTextDocumentParams && EqualShape_f923e9ff4f55ce6e(value, otherWillSaveTextDocumentParams);
            case TextEdit value:
                return b is TextEdit otherTextEdit && EqualShape_907ab9937a53fb02(value, otherTextEdit);
            case DidChangeWatchedFilesParams value:
                return b is DidChangeWatchedFilesParams otherDidChangeWatchedFilesParams && EqualShape_7a687231fe680441(value, otherDidChangeWatchedFilesParams);
            case DidChangeWatchedFilesRegistrationOptions value:
                return b is DidChangeWatchedFilesRegistrationOptions otherDidChangeWatchedFilesRegistrationOptions && EqualShape_9a8637149e2b0c93(value, otherDidChangeWatchedFilesRegistrationOptions);
            case PublishDiagnosticsParams value:
                return b is PublishDiagnosticsParams otherPublishDiagnosticsParams && EqualShape_a870657479cb5aa7(value, otherPublishDiagnosticsParams);
            case CompletionParams value:
                return b is CompletionParams otherCompletionParams && EqualShape_a52f8dcb7c004673(value, otherCompletionParams);
            case CompletionItem value:
                return b is CompletionItem otherCompletionItem && EqualShape_d2a93ce39bb367de(value, otherCompletionItem);
            case CompletionList value:
                return b is CompletionList otherCompletionList && EqualShape_f8ad52b4cd7e25cc(value, otherCompletionList);
            case CompletionRegistrationOptions value:
                return b is CompletionRegistrationOptions otherCompletionRegistrationOptions && EqualShape_78ef87019ed5c3c1(value, otherCompletionRegistrationOptions);
            case HoverParams value:
                return b is HoverParams otherHoverParams && EqualShape_c1b679afaeb1b5ec(value, otherHoverParams);
            case Hover value:
                return b is Hover otherHover && EqualShape_c76ae86ad3014c85(value, otherHover);
            case HoverRegistrationOptions value:
                return b is HoverRegistrationOptions otherHoverRegistrationOptions && EqualShape_c42e31a1d8d0c570(value, otherHoverRegistrationOptions);
            case SignatureHelpParams value:
                return b is SignatureHelpParams otherSignatureHelpParams && EqualShape_775120ff277a7df5(value, otherSignatureHelpParams);
            case SignatureHelp value:
                return b is SignatureHelp otherSignatureHelp && EqualShape_5a6c866a2e0d1dee(value, otherSignatureHelp);
            case SignatureHelpRegistrationOptions value:
                return b is SignatureHelpRegistrationOptions otherSignatureHelpRegistrationOptions && EqualShape_cd22913bfb024c92(value, otherSignatureHelpRegistrationOptions);
            case DefinitionParams value:
                return b is DefinitionParams otherDefinitionParams && EqualShape_2c8b11f5c8545467(value, otherDefinitionParams);
            case DefinitionRegistrationOptions value:
                return b is DefinitionRegistrationOptions otherDefinitionRegistrationOptions && EqualShape_477146b0718707a3(value, otherDefinitionRegistrationOptions);
            case ReferenceParams value:
                return b is ReferenceParams otherReferenceParams && EqualShape_d77f6d8f9dfa25d9(value, otherReferenceParams);
            case ReferenceRegistrationOptions value:
                return b is ReferenceRegistrationOptions otherReferenceRegistrationOptions && EqualShape_944d8fdf608f0ed6(value, otherReferenceRegistrationOptions);
            case DocumentHighlightParams value:
                return b is DocumentHighlightParams otherDocumentHighlightParams && EqualShape_0123e4aa1b008823(value, otherDocumentHighlightParams);
            case DocumentHighlight value:
                return b is DocumentHighlight otherDocumentHighlight && EqualShape_186bd9e070f0cc76(value, otherDocumentHighlight);
            case DocumentHighlightRegistrationOptions value:
                return b is DocumentHighlightRegistrationOptions otherDocumentHighlightRegistrationOptions && EqualShape_0c38eeec102fae46(value, otherDocumentHighlightRegistrationOptions);
            case DocumentSymbolParams value:
                return b is DocumentSymbolParams otherDocumentSymbolParams && EqualShape_2be3ae8722c9c7c4(value, otherDocumentSymbolParams);
            case SymbolInformation value:
                return b is SymbolInformation otherSymbolInformation && EqualShape_7a1b84d8b2015bb3(value, otherSymbolInformation);
            case DocumentSymbol value:
                return b is DocumentSymbol otherDocumentSymbol && EqualShape_7a5e3c27bb3a5284(value, otherDocumentSymbol);
            case DocumentSymbolRegistrationOptions value:
                return b is DocumentSymbolRegistrationOptions otherDocumentSymbolRegistrationOptions && EqualShape_40bf243208068748(value, otherDocumentSymbolRegistrationOptions);
            case CodeActionParams value:
                return b is CodeActionParams otherCodeActionParams && EqualShape_5fd24f9135249493(value, otherCodeActionParams);
            case Command value:
                return b is Command otherCommand && EqualShape_713166971d730f81(value, otherCommand);
            case CodeAction value:
                return b is CodeAction otherCodeAction && EqualShape_865c9c8fafe5c729(value, otherCodeAction);
            case CodeActionRegistrationOptions value:
                return b is CodeActionRegistrationOptions otherCodeActionRegistrationOptions && EqualShape_9b2193d2ea04428b(value, otherCodeActionRegistrationOptions);
            case WorkspaceSymbolParams value:
                return b is WorkspaceSymbolParams otherWorkspaceSymbolParams && EqualShape_f45d058e1512eabf(value, otherWorkspaceSymbolParams);
            case WorkspaceSymbol value:
                return b is WorkspaceSymbol otherWorkspaceSymbol && EqualShape_e3f9d3600b02eeb1(value, otherWorkspaceSymbol);
            case WorkspaceSymbolRegistrationOptions value:
                return b is WorkspaceSymbolRegistrationOptions otherWorkspaceSymbolRegistrationOptions && EqualShape_dbb4c03cff5a9b2c(value, otherWorkspaceSymbolRegistrationOptions);
            case CodeLensParams value:
                return b is CodeLensParams otherCodeLensParams && EqualShape_4a62670ff2e4f6dc(value, otherCodeLensParams);
            case CodeLens value:
                return b is CodeLens otherCodeLens && EqualShape_9e0c959c15c790a3(value, otherCodeLens);
            case CodeLensRegistrationOptions value:
                return b is CodeLensRegistrationOptions otherCodeLensRegistrationOptions && EqualShape_d5d1b6fc9d1f6ede(value, otherCodeLensRegistrationOptions);
            case DocumentLinkParams value:
                return b is DocumentLinkParams otherDocumentLinkParams && EqualShape_b94d0f031a384478(value, otherDocumentLinkParams);
            case DocumentLink value:
                return b is DocumentLink otherDocumentLink && EqualShape_25f2aaad3d4056b4(value, otherDocumentLink);
            case DocumentLinkRegistrationOptions value:
                return b is DocumentLinkRegistrationOptions otherDocumentLinkRegistrationOptions && EqualShape_92723a302d3136eb(value, otherDocumentLinkRegistrationOptions);
            case DocumentFormattingParams value:
                return b is DocumentFormattingParams otherDocumentFormattingParams && EqualShape_d3c0b448bcf8e371(value, otherDocumentFormattingParams);
            case DocumentFormattingRegistrationOptions value:
                return b is DocumentFormattingRegistrationOptions otherDocumentFormattingRegistrationOptions && EqualShape_62ee47a157b066a5(value, otherDocumentFormattingRegistrationOptions);
            case DocumentRangeFormattingParams value:
                return b is DocumentRangeFormattingParams otherDocumentRangeFormattingParams && EqualShape_d6bb17f42468c458(value, otherDocumentRangeFormattingParams);
            case DocumentRangeFormattingRegistrationOptions value:
                return b is DocumentRangeFormattingRegistrationOptions otherDocumentRangeFormattingRegistrationOptions && EqualShape_e4397d5196cfce64(value, otherDocumentRangeFormattingRegistrationOptions);
            case DocumentRangesFormattingParams value:
                return b is DocumentRangesFormattingParams otherDocumentRangesFormattingParams && EqualShape_ebefe18aa77c282b(value, otherDocumentRangesFormattingParams);
            case DocumentOnTypeFormattingParams value:
                return b is DocumentOnTypeFormattingParams otherDocumentOnTypeFormattingParams && EqualShape_973929f55766dcd7(value, otherDocumentOnTypeFormattingParams);
            case DocumentOnTypeFormattingRegistrationOptions value:
                return b is DocumentOnTypeFormattingRegistrationOptions otherDocumentOnTypeFormattingRegistrationOptions && EqualShape_3e4d7072185bf69d(value, otherDocumentOnTypeFormattingRegistrationOptions);
            case RenameParams value:
                return b is RenameParams otherRenameParams && EqualShape_7c73bd9e0a480ec1(value, otherRenameParams);
            case RenameRegistrationOptions value:
                return b is RenameRegistrationOptions otherRenameRegistrationOptions && EqualShape_2a0823905e235731(value, otherRenameRegistrationOptions);
            case PrepareRenameParams value:
                return b is PrepareRenameParams otherPrepareRenameParams && EqualShape_21a0c71ff605bf6f(value, otherPrepareRenameParams);
            case ExecuteCommandParams value:
                return b is ExecuteCommandParams otherExecuteCommandParams && EqualShape_167e0d23d2d2a94b(value, otherExecuteCommandParams);
            case ExecuteCommandRegistrationOptions value:
                return b is ExecuteCommandRegistrationOptions otherExecuteCommandRegistrationOptions && EqualShape_e5b0616c18b5df9a(value, otherExecuteCommandRegistrationOptions);
            case ApplyWorkspaceEditParams value:
                return b is ApplyWorkspaceEditParams otherApplyWorkspaceEditParams && EqualShape_54526a811be46335(value, otherApplyWorkspaceEditParams);
            case ApplyWorkspaceEditResult value:
                return b is ApplyWorkspaceEditResult otherApplyWorkspaceEditResult && EqualShape_6d7f4c2b0309a43e(value, otherApplyWorkspaceEditResult);
            case WorkDoneProgressBegin value:
                return b is WorkDoneProgressBegin otherWorkDoneProgressBegin && EqualShape_77eefa47f6f89dd3(value, otherWorkDoneProgressBegin);
            case WorkDoneProgressReport value:
                return b is WorkDoneProgressReport otherWorkDoneProgressReport && EqualShape_c0b20ac9d5d09a16(value, otherWorkDoneProgressReport);
            case WorkDoneProgressEnd value:
                return b is WorkDoneProgressEnd otherWorkDoneProgressEnd && EqualShape_8e9ae8bd94490e20(value, otherWorkDoneProgressEnd);
            case SetTraceParams value:
                return b is SetTraceParams otherSetTraceParams && EqualShape_41823968658e5dfc(value, otherSetTraceParams);
            case LogTraceParams value:
                return b is LogTraceParams otherLogTraceParams && EqualShape_561978800a186b54(value, otherLogTraceParams);
            case CancelParams value:
                return b is CancelParams otherCancelParams && EqualShape_68e14858b5be82dc(value, otherCancelParams);
            case ProgressParams value:
                return b is ProgressParams otherProgressParams && EqualShape_26b35649303908fb(value, otherProgressParams);
            case TextDocumentPositionParams value:
                return b is TextDocumentPositionParams otherTextDocumentPositionParams && EqualShape_3166bd6fc1b8b11c(value, otherTextDocumentPositionParams);
            case WorkDoneProgressParams value:
                return b is WorkDoneProgressParams otherWorkDoneProgressParams && EqualShape_5d933d6055d6e87f(value, otherWorkDoneProgressParams);
            case PartialResultParams value:
                return b is PartialResultParams otherPartialResultParams && EqualShape_413dff5f49001606(value, otherPartialResultParams);
            case LocationLink value:
                return b is LocationLink otherLocationLink && EqualShape_bdb924567ef98797(value, otherLocationLink);
            case Range value:
                return b is Range otherRange && EqualShape_5de74a81b192304c(value, otherRange);
            case ImplementationOptions value:
                return b is ImplementationOptions otherImplementationOptions && EqualShape_6a59e8e8c3547e2d(value, otherImplementationOptions);
            case StaticRegistrationOptions value:
                return b is StaticRegistrationOptions otherStaticRegistrationOptions && EqualShape_689fd1f5d9125759(value, otherStaticRegistrationOptions);
            case TypeDefinitionOptions value:
                return b is TypeDefinitionOptions otherTypeDefinitionOptions && EqualShape_edef749fe5e8e612(value, otherTypeDefinitionOptions);
            case WorkspaceFoldersChangeEvent value:
                return b is WorkspaceFoldersChangeEvent otherWorkspaceFoldersChangeEvent && EqualShape_cac484af091ededa(value, otherWorkspaceFoldersChangeEvent);
            case ConfigurationItem value:
                return b is ConfigurationItem otherConfigurationItem && EqualShape_6fb58034d4f05554(value, otherConfigurationItem);
            case TextDocumentIdentifier value:
                return b is TextDocumentIdentifier otherTextDocumentIdentifier && EqualShape_76f80cdd6acc5d72(value, otherTextDocumentIdentifier);
            case Color value:
                return b is Color otherColor && EqualShape_6b73191a0a4b6742(value, otherColor);
            case DocumentColorOptions value:
                return b is DocumentColorOptions otherDocumentColorOptions && EqualShape_522a26e4dbe6345c(value, otherDocumentColorOptions);
            case FoldingRangeOptions value:
                return b is FoldingRangeOptions otherFoldingRangeOptions && EqualShape_563033aa92b4afe3(value, otherFoldingRangeOptions);
            case DeclarationOptions value:
                return b is DeclarationOptions otherDeclarationOptions && EqualShape_55db17bfd8074c50(value, otherDeclarationOptions);
            case Position value:
                return b is Position otherPosition && EqualShape_6d031af10da7a25e(value, otherPosition);
            case SelectionRangeOptions value:
                return b is SelectionRangeOptions otherSelectionRangeOptions && EqualShape_de0943b8402d64fb(value, otherSelectionRangeOptions);
            case CallHierarchyOptions value:
                return b is CallHierarchyOptions otherCallHierarchyOptions && EqualShape_bf40a5eb811d3651(value, otherCallHierarchyOptions);
            case SemanticTokensOptions value:
                return b is SemanticTokensOptions otherSemanticTokensOptions && EqualShape_e73c6a23ccc9fff4(value, otherSemanticTokensOptions);
            case SemanticTokensEdit value:
                return b is SemanticTokensEdit otherSemanticTokensEdit && EqualShape_5d1443274aea049d(value, otherSemanticTokensEdit);
            case LinkedEditingRangeOptions value:
                return b is LinkedEditingRangeOptions otherLinkedEditingRangeOptions && EqualShape_bf2919c7ffa7243d(value, otherLinkedEditingRangeOptions);
            case FileCreate value:
                return b is FileCreate otherFileCreate && EqualShape_dd7287de677959f4(value, otherFileCreate);
            case TextDocumentEdit value:
                return b is TextDocumentEdit otherTextDocumentEdit && EqualShape_e35cf3d963f0d31d(value, otherTextDocumentEdit);
            case CreateFile value:
                return b is CreateFile otherCreateFile && EqualShape_9d3a24d3495132d7(value, otherCreateFile);
            case RenameFile value:
                return b is RenameFile otherRenameFile && EqualShape_3f987788dab9f8e0(value, otherRenameFile);
            case DeleteFile value:
                return b is DeleteFile otherDeleteFile && EqualShape_bbfcf281d1c9c237(value, otherDeleteFile);
            case ChangeAnnotation value:
                return b is ChangeAnnotation otherChangeAnnotation && EqualShape_95a49b2bfa6a44cd(value, otherChangeAnnotation);
            case FileOperationFilter value:
                return b is FileOperationFilter otherFileOperationFilter && EqualShape_01085faf2ca32a63(value, otherFileOperationFilter);
            case FileRename value:
                return b is FileRename otherFileRename && EqualShape_132f8fe961259b8b(value, otherFileRename);
            case FileDelete value:
                return b is FileDelete otherFileDelete && EqualShape_4cfd293b1972bff8(value, otherFileDelete);
            case MonikerOptions value:
                return b is MonikerOptions otherMonikerOptions && EqualShape_6a54ddd49258de2c(value, otherMonikerOptions);
            case TypeHierarchyOptions value:
                return b is TypeHierarchyOptions otherTypeHierarchyOptions && EqualShape_af73cf291a3bbe3e(value, otherTypeHierarchyOptions);
            case InlineValueContext value:
                return b is InlineValueContext otherInlineValueContext && EqualShape_0db2a1b74408177b(value, otherInlineValueContext);
            case InlineValueText value:
                return b is InlineValueText otherInlineValueText && EqualShape_e1fd84adea44b647(value, otherInlineValueText);
            case InlineValueVariableLookup value:
                return b is InlineValueVariableLookup otherInlineValueVariableLookup && EqualShape_2dc5fda12fce3a9d(value, otherInlineValueVariableLookup);
            case InlineValueEvaluatableExpression value:
                return b is InlineValueEvaluatableExpression otherInlineValueEvaluatableExpression && EqualShape_5ba15e00f3989e84(value, otherInlineValueEvaluatableExpression);
            case InlineValueOptions value:
                return b is InlineValueOptions otherInlineValueOptions && EqualShape_4227afe828e4c487(value, otherInlineValueOptions);
            case InlayHintLabelPart value:
                return b is InlayHintLabelPart otherInlayHintLabelPart && EqualShape_83ad5d75a7336f5c(value, otherInlayHintLabelPart);
            case MarkupContent value:
                return b is MarkupContent otherMarkupContent && EqualShape_ec8d99ae989825b7(value, otherMarkupContent);
            case InlayHintOptions value:
                return b is InlayHintOptions otherInlayHintOptions && EqualShape_e7413e6883917923(value, otherInlayHintOptions);
            case RelatedFullDocumentDiagnosticReport value:
                return b is RelatedFullDocumentDiagnosticReport otherRelatedFullDocumentDiagnosticReport && EqualShape_821440dfb3fa7e3f(value, otherRelatedFullDocumentDiagnosticReport);
            case RelatedUnchangedDocumentDiagnosticReport value:
                return b is RelatedUnchangedDocumentDiagnosticReport otherRelatedUnchangedDocumentDiagnosticReport && EqualShape_77534693e60adab3(value, otherRelatedUnchangedDocumentDiagnosticReport);
            case DocumentDiagnosticReportPartialResult value:
                return b is DocumentDiagnosticReportPartialResult otherDocumentDiagnosticReportPartialResult && EqualShape_9e742979ea06ee1e(value, otherDocumentDiagnosticReportPartialResult);
            case DiagnosticOptions value:
                return b is DiagnosticOptions otherDiagnosticOptions && EqualShape_c7978f199a414fbd(value, otherDiagnosticOptions);
            case PreviousResultId value:
                return b is PreviousResultId otherPreviousResultId && EqualShape_2d2e58655dd72e85(value, otherPreviousResultId);
            case TextDocumentItem value:
                return b is TextDocumentItem otherTextDocumentItem && EqualShape_bfe80aa9873109a2(value, otherTextDocumentItem);
            case InlineCompletionContext value:
                return b is InlineCompletionContext otherInlineCompletionContext && EqualShape_22637a4522270110(value, otherInlineCompletionContext);
            case StringValue value:
                return b is StringValue otherStringValue && EqualShape_24b828daee6b73d3(value, otherStringValue);
            case InlineCompletionOptions value:
                return b is InlineCompletionOptions otherInlineCompletionOptions && EqualShape_571267fe163915c1(value, otherInlineCompletionOptions);
            case TextDocumentContentOptions value:
                return b is TextDocumentContentOptions otherTextDocumentContentOptions && EqualShape_13118cef8ccea9e5(value, otherTextDocumentContentOptions);
            case Registration value:
                return b is Registration otherRegistration && EqualShape_c793e0d9a1cc6421(value, otherRegistration);
            case RegisterOptions value:
                return b is RegisterOptions otherRegisterOptions && EqualShape_525c0e333a92bf78(value, otherRegisterOptions);
            case Unregistration value:
                return b is Unregistration otherUnregistration && EqualShape_21a4e4496bc03a0b(value, otherUnregistration);
            case WorkspaceFoldersInitializeParams value:
                return b is WorkspaceFoldersInitializeParams otherWorkspaceFoldersInitializeParams && EqualShape_fdd23c4151a01701(value, otherWorkspaceFoldersInitializeParams);
            case ServerCapabilities value:
                return b is ServerCapabilities otherServerCapabilities && EqualShape_628c62fa6a34c245(value, otherServerCapabilities);
            case ServerInfo value:
                return b is ServerInfo otherServerInfo && EqualShape_4f0db94bc9f4a530(value, otherServerInfo);
            case VersionedTextDocumentIdentifier value:
                return b is VersionedTextDocumentIdentifier otherVersionedTextDocumentIdentifier && EqualShape_bfe1d574dff86508(value, otherVersionedTextDocumentIdentifier);
            case SaveOptions value:
                return b is SaveOptions otherSaveOptions && EqualShape_0c860e4c1955b16b(value, otherSaveOptions);
            case FileEvent value:
                return b is FileEvent otherFileEvent && EqualShape_a125c27075ddc783(value, otherFileEvent);
            case FileSystemWatcher value:
                return b is FileSystemWatcher otherFileSystemWatcher && EqualShape_8546807fc7830176(value, otherFileSystemWatcher);
            case Diagnostic value:
                return b is Diagnostic otherDiagnostic && EqualShape_b1fa878a98a15b74(value, otherDiagnostic);
            case CompletionContext value:
                return b is CompletionContext otherCompletionContext && EqualShape_e236ca93686fcfa1(value, otherCompletionContext);
            case CompletionItemLabelDetails value:
                return b is CompletionItemLabelDetails otherCompletionItemLabelDetails && EqualShape_4918d4bf2593f8af(value, otherCompletionItemLabelDetails);
            case InsertReplaceEdit value:
                return b is InsertReplaceEdit otherInsertReplaceEdit && EqualShape_79b7a3fa81c6612e(value, otherInsertReplaceEdit);
            case CompletionItemDefaults value:
                return b is CompletionItemDefaults otherCompletionItemDefaults && EqualShape_467395eb042a5b79(value, otherCompletionItemDefaults);
            case CompletionItemApplyKinds value:
                return b is CompletionItemApplyKinds otherCompletionItemApplyKinds && EqualShape_946202155aedc75c(value, otherCompletionItemApplyKinds);
            case CompletionOptions value:
                return b is CompletionOptions otherCompletionOptions && EqualShape_1d476dc84cbde137(value, otherCompletionOptions);
            case HoverOptions value:
                return b is HoverOptions otherHoverOptions && EqualShape_5e5dc1cc6e1c3523(value, otherHoverOptions);
            case SignatureHelpContext value:
                return b is SignatureHelpContext otherSignatureHelpContext && EqualShape_1331ccfab2d32049(value, otherSignatureHelpContext);
            case SignatureInformation value:
                return b is SignatureInformation otherSignatureInformation && EqualShape_281bb89ea78ef9ef(value, otherSignatureInformation);
            case SignatureHelpOptions value:
                return b is SignatureHelpOptions otherSignatureHelpOptions && EqualShape_f179f1d094fa3f6c(value, otherSignatureHelpOptions);
            case DefinitionOptions value:
                return b is DefinitionOptions otherDefinitionOptions && EqualShape_9faf6afba7757762(value, otherDefinitionOptions);
            case ReferenceContext value:
                return b is ReferenceContext otherReferenceContext && EqualShape_49c578f22284c849(value, otherReferenceContext);
            case ReferenceOptions value:
                return b is ReferenceOptions otherReferenceOptions && EqualShape_7972bdd81c162d72(value, otherReferenceOptions);
            case DocumentHighlightOptions value:
                return b is DocumentHighlightOptions otherDocumentHighlightOptions && EqualShape_5fa893e750b616b3(value, otherDocumentHighlightOptions);
            case BaseSymbolInformation value:
                return b is BaseSymbolInformation otherBaseSymbolInformation && EqualShape_67f5c69ca6dc88bb(value, otherBaseSymbolInformation);
            case DocumentSymbolOptions value:
                return b is DocumentSymbolOptions otherDocumentSymbolOptions && EqualShape_a1b6a2290a294048(value, otherDocumentSymbolOptions);
            case CodeActionContext value:
                return b is CodeActionContext otherCodeActionContext && EqualShape_5e9db653ca06c3c9(value, otherCodeActionContext);
            case CodeActionDisabled value:
                return b is CodeActionDisabled otherCodeActionDisabled && EqualShape_081648c1b4ef6f5e(value, otherCodeActionDisabled);
            case CodeActionOptions value:
                return b is CodeActionOptions otherCodeActionOptions && EqualShape_c349820ded303c22(value, otherCodeActionOptions);
            case LocationUriOnly value:
                return b is LocationUriOnly otherLocationUriOnly && EqualShape_3bee336672c6633c(value, otherLocationUriOnly);
            case WorkspaceSymbolOptions value:
                return b is WorkspaceSymbolOptions otherWorkspaceSymbolOptions && EqualShape_64a1fc35c89fbeab(value, otherWorkspaceSymbolOptions);
            case CodeLensOptions value:
                return b is CodeLensOptions otherCodeLensOptions && EqualShape_b5c29b63aaa2a6f8(value, otherCodeLensOptions);
            case DocumentLinkOptions value:
                return b is DocumentLinkOptions otherDocumentLinkOptions && EqualShape_529f3bd836f0b9d8(value, otherDocumentLinkOptions);
            case FormattingOptions value:
                return b is FormattingOptions otherFormattingOptions && EqualShape_4441aa46c6968d1e(value, otherFormattingOptions);
            case DocumentFormattingOptions value:
                return b is DocumentFormattingOptions otherDocumentFormattingOptions && EqualShape_e984db9d3215a45f(value, otherDocumentFormattingOptions);
            case DocumentRangeFormattingOptions value:
                return b is DocumentRangeFormattingOptions otherDocumentRangeFormattingOptions && EqualShape_ff263dfb0755ad33(value, otherDocumentRangeFormattingOptions);
            case DocumentOnTypeFormattingOptions value:
                return b is DocumentOnTypeFormattingOptions otherDocumentOnTypeFormattingOptions && EqualShape_0fa03db0935ecc4d(value, otherDocumentOnTypeFormattingOptions);
            case RenameOptions value:
                return b is RenameOptions otherRenameOptions && EqualShape_dc010ea0160c6a6a(value, otherRenameOptions);
            case PrepareRenamePlaceholder value:
                return b is PrepareRenamePlaceholder otherPrepareRenamePlaceholder && EqualShape_a423b1bf4090c576(value, otherPrepareRenamePlaceholder);
            case PrepareRenameDefaultBehavior value:
                return b is PrepareRenameDefaultBehavior otherPrepareRenameDefaultBehavior && EqualShape_0939d43961a41cae(value, otherPrepareRenameDefaultBehavior);
            case ExecuteCommandOptions value:
                return b is ExecuteCommandOptions otherExecuteCommandOptions && EqualShape_03d1076067933682(value, otherExecuteCommandOptions);
            case WorkspaceEditMetadata value:
                return b is WorkspaceEditMetadata otherWorkspaceEditMetadata && EqualShape_683af1a028c8c949(value, otherWorkspaceEditMetadata);
            case WorkDoneProgressOptions value:
                return b is WorkDoneProgressOptions otherWorkDoneProgressOptions && EqualShape_4c899c625b152feb(value, otherWorkDoneProgressOptions);
            case SemanticTokensLegend value:
                return b is SemanticTokensLegend otherSemanticTokensLegend && EqualShape_508b7ba4b756581e(value, otherSemanticTokensLegend);
            case SemanticTokensFullDelta value:
                return b is SemanticTokensFullDelta otherSemanticTokensFullDelta && EqualShape_47409fff8234f1d6(value, otherSemanticTokensFullDelta);
            case OptionalVersionedTextDocumentIdentifier value:
                return b is OptionalVersionedTextDocumentIdentifier otherOptionalVersionedTextDocumentIdentifier && EqualShape_a348a1e23de5742a(value, otherOptionalVersionedTextDocumentIdentifier);
            case AnnotatedTextEdit value:
                return b is AnnotatedTextEdit otherAnnotatedTextEdit && EqualShape_dc76475f4b1ed640(value, otherAnnotatedTextEdit);
            case SnippetTextEdit value:
                return b is SnippetTextEdit otherSnippetTextEdit && EqualShape_108d1bfa63a85c92(value, otherSnippetTextEdit);
            case ResourceOperation value:
                return b is ResourceOperation otherResourceOperation && EqualShape_bb0929e8a5eb2809(value, otherResourceOperation);
            case CreateFileOptions value:
                return b is CreateFileOptions otherCreateFileOptions && EqualShape_ad7b628c8a318f7f(value, otherCreateFileOptions);
            case RenameFileOptions value:
                return b is RenameFileOptions otherRenameFileOptions && EqualShape_4e2827e566b1dc06(value, otherRenameFileOptions);
            case DeleteFileOptions value:
                return b is DeleteFileOptions otherDeleteFileOptions && EqualShape_1f8e9018d646804f(value, otherDeleteFileOptions);
            case FileOperationPattern value:
                return b is FileOperationPattern otherFileOperationPattern && EqualShape_876111df355462c4(value, otherFileOperationPattern);
            case FullDocumentDiagnosticReport value:
                return b is FullDocumentDiagnosticReport otherFullDocumentDiagnosticReport && EqualShape_7a31e4ad68de1c48(value, otherFullDocumentDiagnosticReport);
            case UnchangedDocumentDiagnosticReport value:
                return b is UnchangedDocumentDiagnosticReport otherUnchangedDocumentDiagnosticReport && EqualShape_f1f7a7b99ec47d3d(value, otherUnchangedDocumentDiagnosticReport);
            case WorkspaceFullDocumentDiagnosticReport value:
                return b is WorkspaceFullDocumentDiagnosticReport otherWorkspaceFullDocumentDiagnosticReport && EqualShape_27b2946fe8aba24d(value, otherWorkspaceFullDocumentDiagnosticReport);
            case WorkspaceUnchangedDocumentDiagnosticReport value:
                return b is WorkspaceUnchangedDocumentDiagnosticReport otherWorkspaceUnchangedDocumentDiagnosticReport && EqualShape_c1a1987ea31ba882(value, otherWorkspaceUnchangedDocumentDiagnosticReport);
            case SelectedCompletionInfo value:
                return b is SelectedCompletionInfo otherSelectedCompletionInfo && EqualShape_422af1cce96213bc(value, otherSelectedCompletionInfo);
            case ClientInfo value:
                return b is ClientInfo otherClientInfo && EqualShape_e58e814bcb67ebf4(value, otherClientInfo);
            case ClientCapabilities value:
                return b is ClientCapabilities otherClientCapabilities && EqualShape_ae5beebabf5633ee(value, otherClientCapabilities);
            case TextDocumentSyncOptions value:
                return b is TextDocumentSyncOptions otherTextDocumentSyncOptions && EqualShape_f2c5e9259b0b9679(value, otherTextDocumentSyncOptions);
            case WorkspaceOptions value:
                return b is WorkspaceOptions otherWorkspaceOptions && EqualShape_891102cd69b5f6f3(value, otherWorkspaceOptions);
            case TextDocumentContentChangePartial value:
                return b is TextDocumentContentChangePartial otherTextDocumentContentChangePartial && EqualShape_d1c2c4faf51c15cd(value, otherTextDocumentContentChangePartial);
            case TextDocumentContentChangeWholeDocument value:
                return b is TextDocumentContentChangeWholeDocument otherTextDocumentContentChangeWholeDocument && EqualShape_a785b2bcdef39e09(value, otherTextDocumentContentChangeWholeDocument);
            case CodeDescription value:
                return b is CodeDescription otherCodeDescription && EqualShape_d5d05065bc890cc5(value, otherCodeDescription);
            case DiagnosticRelatedInformation value:
                return b is DiagnosticRelatedInformation otherDiagnosticRelatedInformation && EqualShape_730df2a39dbfb7da(value, otherDiagnosticRelatedInformation);
            case EditRangeWithInsertReplace value:
                return b is EditRangeWithInsertReplace otherEditRangeWithInsertReplace && EqualShape_909237c23c611f07(value, otherEditRangeWithInsertReplace);
            case ServerCompletionItemOptions value:
                return b is ServerCompletionItemOptions otherServerCompletionItemOptions && EqualShape_3b0c84c52fea24a8(value, otherServerCompletionItemOptions);
            case MarkedStringWithLanguage value:
                return b is MarkedStringWithLanguage otherMarkedStringWithLanguage && EqualShape_02b572e84de66f2a(value, otherMarkedStringWithLanguage);
            case ParameterInformation value:
                return b is ParameterInformation otherParameterInformation && EqualShape_6e5531cdb6cb8530(value, otherParameterInformation);
            case CodeActionKindDocumentation value:
                return b is CodeActionKindDocumentation otherCodeActionKindDocumentation && EqualShape_4271ba828c998e5f(value, otherCodeActionKindDocumentation);
            case FileOperationPatternOptions value:
                return b is FileOperationPatternOptions otherFileOperationPatternOptions && EqualShape_77897374dc003098(value, otherFileOperationPatternOptions);
            case WorkspaceClientCapabilities value:
                return b is WorkspaceClientCapabilities otherWorkspaceClientCapabilities && EqualShape_f80d356a037da136(value, otherWorkspaceClientCapabilities);
            case TextDocumentClientCapabilities value:
                return b is TextDocumentClientCapabilities otherTextDocumentClientCapabilities && EqualShape_ccc9d5a581c31fa6(value, otherTextDocumentClientCapabilities);
            case WindowClientCapabilities value:
                return b is WindowClientCapabilities otherWindowClientCapabilities && EqualShape_9fd8a5a7e3a477bc(value, otherWindowClientCapabilities);
            case GeneralClientCapabilities value:
                return b is GeneralClientCapabilities otherGeneralClientCapabilities && EqualShape_337bdf3fdd9371a3(value, otherGeneralClientCapabilities);
            case WorkspaceFoldersServerCapabilities value:
                return b is WorkspaceFoldersServerCapabilities otherWorkspaceFoldersServerCapabilities && EqualShape_76eae964c482175c(value, otherWorkspaceFoldersServerCapabilities);
            case FileOperationOptions value:
                return b is FileOperationOptions otherFileOperationOptions && EqualShape_518c29b60657172f(value, otherFileOperationOptions);
            case RelativePattern value:
                return b is RelativePattern otherRelativePattern && EqualShape_417e71cd37016ebf(value, otherRelativePattern);
            case WorkspaceEditClientCapabilities value:
                return b is WorkspaceEditClientCapabilities otherWorkspaceEditClientCapabilities && EqualShape_f8870bd1e193be94(value, otherWorkspaceEditClientCapabilities);
            case DidChangeConfigurationClientCapabilities value:
                return b is DidChangeConfigurationClientCapabilities otherDidChangeConfigurationClientCapabilities && EqualShape_7b4c2bd9272ffb37(value, otherDidChangeConfigurationClientCapabilities);
            case DidChangeWatchedFilesClientCapabilities value:
                return b is DidChangeWatchedFilesClientCapabilities otherDidChangeWatchedFilesClientCapabilities && EqualShape_5b8e42fb55c703ff(value, otherDidChangeWatchedFilesClientCapabilities);
            case WorkspaceSymbolClientCapabilities value:
                return b is WorkspaceSymbolClientCapabilities otherWorkspaceSymbolClientCapabilities && EqualShape_a5728d8363acabeb(value, otherWorkspaceSymbolClientCapabilities);
            case ExecuteCommandClientCapabilities value:
                return b is ExecuteCommandClientCapabilities otherExecuteCommandClientCapabilities && EqualShape_6d21070f992c17fb(value, otherExecuteCommandClientCapabilities);
            case SemanticTokensWorkspaceClientCapabilities value:
                return b is SemanticTokensWorkspaceClientCapabilities otherSemanticTokensWorkspaceClientCapabilities && EqualShape_fe40ff6064ff54f2(value, otherSemanticTokensWorkspaceClientCapabilities);
            case CodeLensWorkspaceClientCapabilities value:
                return b is CodeLensWorkspaceClientCapabilities otherCodeLensWorkspaceClientCapabilities && EqualShape_27b4581cda4b0d84(value, otherCodeLensWorkspaceClientCapabilities);
            case FileOperationClientCapabilities value:
                return b is FileOperationClientCapabilities otherFileOperationClientCapabilities && EqualShape_9de47d600efc33f1(value, otherFileOperationClientCapabilities);
            case InlineValueWorkspaceClientCapabilities value:
                return b is InlineValueWorkspaceClientCapabilities otherInlineValueWorkspaceClientCapabilities && EqualShape_caa283e237c108c2(value, otherInlineValueWorkspaceClientCapabilities);
            case InlayHintWorkspaceClientCapabilities value:
                return b is InlayHintWorkspaceClientCapabilities otherInlayHintWorkspaceClientCapabilities && EqualShape_48084566f2cdcaae(value, otherInlayHintWorkspaceClientCapabilities);
            case DiagnosticWorkspaceClientCapabilities value:
                return b is DiagnosticWorkspaceClientCapabilities otherDiagnosticWorkspaceClientCapabilities && EqualShape_22994f812002fe3c(value, otherDiagnosticWorkspaceClientCapabilities);
            case FoldingRangeWorkspaceClientCapabilities value:
                return b is FoldingRangeWorkspaceClientCapabilities otherFoldingRangeWorkspaceClientCapabilities && EqualShape_fb7ea6a0b64b120e(value, otherFoldingRangeWorkspaceClientCapabilities);
            case TextDocumentContentClientCapabilities value:
                return b is TextDocumentContentClientCapabilities otherTextDocumentContentClientCapabilities && EqualShape_8525cd90fd0e2c39(value, otherTextDocumentContentClientCapabilities);
            case TextDocumentSyncClientCapabilities value:
                return b is TextDocumentSyncClientCapabilities otherTextDocumentSyncClientCapabilities && EqualShape_54b3525db15e3a53(value, otherTextDocumentSyncClientCapabilities);
            case TextDocumentFilterClientCapabilities value:
                return b is TextDocumentFilterClientCapabilities otherTextDocumentFilterClientCapabilities && EqualShape_9988519c9dffc485(value, otherTextDocumentFilterClientCapabilities);
            case CompletionClientCapabilities value:
                return b is CompletionClientCapabilities otherCompletionClientCapabilities && EqualShape_16f91af89436100d(value, otherCompletionClientCapabilities);
            case HoverClientCapabilities value:
                return b is HoverClientCapabilities otherHoverClientCapabilities && EqualShape_c3adf798fbc806c6(value, otherHoverClientCapabilities);
            case SignatureHelpClientCapabilities value:
                return b is SignatureHelpClientCapabilities otherSignatureHelpClientCapabilities && EqualShape_ee58083fc9757534(value, otherSignatureHelpClientCapabilities);
            case DeclarationClientCapabilities value:
                return b is DeclarationClientCapabilities otherDeclarationClientCapabilities && EqualShape_d092d2b3e5d67c33(value, otherDeclarationClientCapabilities);
            case DefinitionClientCapabilities value:
                return b is DefinitionClientCapabilities otherDefinitionClientCapabilities && EqualShape_d782be7c64b733ad(value, otherDefinitionClientCapabilities);
            case TypeDefinitionClientCapabilities value:
                return b is TypeDefinitionClientCapabilities otherTypeDefinitionClientCapabilities && EqualShape_d90d2c9d9d18720a(value, otherTypeDefinitionClientCapabilities);
            case ImplementationClientCapabilities value:
                return b is ImplementationClientCapabilities otherImplementationClientCapabilities && EqualShape_505ca50db8c7eb85(value, otherImplementationClientCapabilities);
            case ReferenceClientCapabilities value:
                return b is ReferenceClientCapabilities otherReferenceClientCapabilities && EqualShape_c08c1b89cb199dea(value, otherReferenceClientCapabilities);
            case DocumentHighlightClientCapabilities value:
                return b is DocumentHighlightClientCapabilities otherDocumentHighlightClientCapabilities && EqualShape_3ea7538c7c7b4ca6(value, otherDocumentHighlightClientCapabilities);
            case DocumentSymbolClientCapabilities value:
                return b is DocumentSymbolClientCapabilities otherDocumentSymbolClientCapabilities && EqualShape_7ca8061eaafe80fa(value, otherDocumentSymbolClientCapabilities);
            case CodeActionClientCapabilities value:
                return b is CodeActionClientCapabilities otherCodeActionClientCapabilities && EqualShape_e7f7570213c73b8e(value, otherCodeActionClientCapabilities);
            case CodeLensClientCapabilities value:
                return b is CodeLensClientCapabilities otherCodeLensClientCapabilities && EqualShape_f0921b0a90d3de37(value, otherCodeLensClientCapabilities);
            case DocumentLinkClientCapabilities value:
                return b is DocumentLinkClientCapabilities otherDocumentLinkClientCapabilities && EqualShape_e06be71a80ee1ec3(value, otherDocumentLinkClientCapabilities);
            case DocumentColorClientCapabilities value:
                return b is DocumentColorClientCapabilities otherDocumentColorClientCapabilities && EqualShape_eb60b030a70a7451(value, otherDocumentColorClientCapabilities);
            case DocumentFormattingClientCapabilities value:
                return b is DocumentFormattingClientCapabilities otherDocumentFormattingClientCapabilities && EqualShape_cce2bbd91d77be98(value, otherDocumentFormattingClientCapabilities);
            case DocumentRangeFormattingClientCapabilities value:
                return b is DocumentRangeFormattingClientCapabilities otherDocumentRangeFormattingClientCapabilities && EqualShape_da985f3aad64b092(value, otherDocumentRangeFormattingClientCapabilities);
            case DocumentOnTypeFormattingClientCapabilities value:
                return b is DocumentOnTypeFormattingClientCapabilities otherDocumentOnTypeFormattingClientCapabilities && EqualShape_9ba4d6b0690b3f7f(value, otherDocumentOnTypeFormattingClientCapabilities);
            case RenameClientCapabilities value:
                return b is RenameClientCapabilities otherRenameClientCapabilities && EqualShape_0c6819e76b439377(value, otherRenameClientCapabilities);
            case FoldingRangeClientCapabilities value:
                return b is FoldingRangeClientCapabilities otherFoldingRangeClientCapabilities && EqualShape_016dd67866f93e94(value, otherFoldingRangeClientCapabilities);
            case SelectionRangeClientCapabilities value:
                return b is SelectionRangeClientCapabilities otherSelectionRangeClientCapabilities && EqualShape_a4e17fae82626379(value, otherSelectionRangeClientCapabilities);
            case PublishDiagnosticsClientCapabilities value:
                return b is PublishDiagnosticsClientCapabilities otherPublishDiagnosticsClientCapabilities && EqualShape_6fa38bb9a3563ce0(value, otherPublishDiagnosticsClientCapabilities);
            case CallHierarchyClientCapabilities value:
                return b is CallHierarchyClientCapabilities otherCallHierarchyClientCapabilities && EqualShape_383095cfb255a14d(value, otherCallHierarchyClientCapabilities);
            case SemanticTokensClientCapabilities value:
                return b is SemanticTokensClientCapabilities otherSemanticTokensClientCapabilities && EqualShape_22677e2a6998efa4(value, otherSemanticTokensClientCapabilities);
            case LinkedEditingRangeClientCapabilities value:
                return b is LinkedEditingRangeClientCapabilities otherLinkedEditingRangeClientCapabilities && EqualShape_1d4bf231f870ced7(value, otherLinkedEditingRangeClientCapabilities);
            case MonikerClientCapabilities value:
                return b is MonikerClientCapabilities otherMonikerClientCapabilities && EqualShape_6294802e598ca5a1(value, otherMonikerClientCapabilities);
            case TypeHierarchyClientCapabilities value:
                return b is TypeHierarchyClientCapabilities otherTypeHierarchyClientCapabilities && EqualShape_2fbaa79cbc8e5da5(value, otherTypeHierarchyClientCapabilities);
            case InlineValueClientCapabilities value:
                return b is InlineValueClientCapabilities otherInlineValueClientCapabilities && EqualShape_9422bd44de4450a3(value, otherInlineValueClientCapabilities);
            case InlayHintClientCapabilities value:
                return b is InlayHintClientCapabilities otherInlayHintClientCapabilities && EqualShape_860bcfccc3d275ce(value, otherInlayHintClientCapabilities);
            case DiagnosticClientCapabilities value:
                return b is DiagnosticClientCapabilities otherDiagnosticClientCapabilities && EqualShape_acc80cae1a8d44b9(value, otherDiagnosticClientCapabilities);
            case InlineCompletionClientCapabilities value:
                return b is InlineCompletionClientCapabilities otherInlineCompletionClientCapabilities && EqualShape_e7de3ce110b1d167(value, otherInlineCompletionClientCapabilities);
            case ShowMessageRequestClientCapabilities value:
                return b is ShowMessageRequestClientCapabilities otherShowMessageRequestClientCapabilities && EqualShape_d3e4643133128cfd(value, otherShowMessageRequestClientCapabilities);
            case ShowDocumentClientCapabilities value:
                return b is ShowDocumentClientCapabilities otherShowDocumentClientCapabilities && EqualShape_2303d8fc7f8ad8a3(value, otherShowDocumentClientCapabilities);
            case StaleRequestSupportOptions value:
                return b is StaleRequestSupportOptions otherStaleRequestSupportOptions && EqualShape_16b0a700ab1e5e92(value, otherStaleRequestSupportOptions);
            case RegularExpressionsClientCapabilities value:
                return b is RegularExpressionsClientCapabilities otherRegularExpressionsClientCapabilities && EqualShape_81491243f7e99a45(value, otherRegularExpressionsClientCapabilities);
            case MarkdownClientCapabilities value:
                return b is MarkdownClientCapabilities otherMarkdownClientCapabilities && EqualShape_5754eb1e7d7a8a87(value, otherMarkdownClientCapabilities);
            case TextDocumentFilterLanguage value:
                return b is TextDocumentFilterLanguage otherTextDocumentFilterLanguage && EqualShape_e4dfacba18e99d73(value, otherTextDocumentFilterLanguage);
            case TextDocumentFilterScheme value:
                return b is TextDocumentFilterScheme otherTextDocumentFilterScheme && EqualShape_ed1e0538451840ef(value, otherTextDocumentFilterScheme);
            case TextDocumentFilterPattern value:
                return b is TextDocumentFilterPattern otherTextDocumentFilterPattern && EqualShape_29eec48c8b924f26(value, otherTextDocumentFilterPattern);
            case ChangeAnnotationsSupportOptions value:
                return b is ChangeAnnotationsSupportOptions otherChangeAnnotationsSupportOptions && EqualShape_767b65d5f0d728e6(value, otherChangeAnnotationsSupportOptions);
            case ClientSymbolKindOptions value:
                return b is ClientSymbolKindOptions otherClientSymbolKindOptions && EqualShape_bfc7ea6558336881(value, otherClientSymbolKindOptions);
            case ClientSymbolTagOptions value:
                return b is ClientSymbolTagOptions otherClientSymbolTagOptions && EqualShape_51e4a8fb8975920a(value, otherClientSymbolTagOptions);
            case ClientSymbolResolveOptions value:
                return b is ClientSymbolResolveOptions otherClientSymbolResolveOptions && EqualShape_8e0f26b4017e860f(value, otherClientSymbolResolveOptions);
            case ClientCompletionItemOptions value:
                return b is ClientCompletionItemOptions otherClientCompletionItemOptions && EqualShape_b37b4bbb7ef32304(value, otherClientCompletionItemOptions);
            case ClientCompletionItemOptionsKind value:
                return b is ClientCompletionItemOptionsKind otherClientCompletionItemOptionsKind && EqualShape_ea7a2cb221fe3126(value, otherClientCompletionItemOptionsKind);
            case CompletionListCapabilities value:
                return b is CompletionListCapabilities otherCompletionListCapabilities && EqualShape_c850bc8b5898a45e(value, otherCompletionListCapabilities);
            case ClientSignatureInformationOptions value:
                return b is ClientSignatureInformationOptions otherClientSignatureInformationOptions && EqualShape_6a3fd3a20dce8c99(value, otherClientSignatureInformationOptions);
            case ClientCodeActionLiteralOptions value:
                return b is ClientCodeActionLiteralOptions otherClientCodeActionLiteralOptions && EqualShape_97349393a7e84c9a(value, otherClientCodeActionLiteralOptions);
            case ClientCodeActionResolveOptions value:
                return b is ClientCodeActionResolveOptions otherClientCodeActionResolveOptions && EqualShape_56c69147319a7d0b(value, otherClientCodeActionResolveOptions);
            case CodeActionTagOptions value:
                return b is CodeActionTagOptions otherCodeActionTagOptions && EqualShape_412c1650085309d1(value, otherCodeActionTagOptions);
            case ClientCodeLensResolveOptions value:
                return b is ClientCodeLensResolveOptions otherClientCodeLensResolveOptions && EqualShape_d931abba359e22bd(value, otherClientCodeLensResolveOptions);
            case ClientFoldingRangeKindOptions value:
                return b is ClientFoldingRangeKindOptions otherClientFoldingRangeKindOptions && EqualShape_51037275a4d10b5f(value, otherClientFoldingRangeKindOptions);
            case ClientFoldingRangeOptions value:
                return b is ClientFoldingRangeOptions otherClientFoldingRangeOptions && EqualShape_a627df9eb6b0b5da(value, otherClientFoldingRangeOptions);
            case DiagnosticsCapabilities value:
                return b is DiagnosticsCapabilities otherDiagnosticsCapabilities && EqualShape_1ace992726f49b52(value, otherDiagnosticsCapabilities);
            case ClientSemanticTokensRequestOptions value:
                return b is ClientSemanticTokensRequestOptions otherClientSemanticTokensRequestOptions && EqualShape_1e6c97fe656a3276(value, otherClientSemanticTokensRequestOptions);
            case ClientInlayHintResolveOptions value:
                return b is ClientInlayHintResolveOptions otherClientInlayHintResolveOptions && EqualShape_43b1b75d4aa5fd41(value, otherClientInlayHintResolveOptions);
            case ClientShowMessageActionItemOptions value:
                return b is ClientShowMessageActionItemOptions otherClientShowMessageActionItemOptions && EqualShape_0d5e37ca0a60abe2(value, otherClientShowMessageActionItemOptions);
            case CompletionItemTagOptions value:
                return b is CompletionItemTagOptions otherCompletionItemTagOptions && EqualShape_9d1d9d8587fbcefd(value, otherCompletionItemTagOptions);
            case ClientCompletionItemResolveOptions value:
                return b is ClientCompletionItemResolveOptions otherClientCompletionItemResolveOptions && EqualShape_08243bf3c503d83a(value, otherClientCompletionItemResolveOptions);
            case ClientCompletionItemInsertTextModeOptions value:
                return b is ClientCompletionItemInsertTextModeOptions otherClientCompletionItemInsertTextModeOptions && EqualShape_28bf4fd56d374e67(value, otherClientCompletionItemInsertTextModeOptions);
            case ClientSignatureParameterInformationOptions value:
                return b is ClientSignatureParameterInformationOptions otherClientSignatureParameterInformationOptions && EqualShape_133fc1cbd2a995c6(value, otherClientSignatureParameterInformationOptions);
            case ClientCodeActionKindOptions value:
                return b is ClientCodeActionKindOptions otherClientCodeActionKindOptions && EqualShape_c1f39ce851ecc9a0(value, otherClientCodeActionKindOptions);
            case ClientDiagnosticsTagOptions value:
                return b is ClientDiagnosticsTagOptions otherClientDiagnosticsTagOptions && EqualShape_e354d3fe1df50a36(value, otherClientDiagnosticsTagOptions);
            case ClientSemanticTokensRequestFullDelta value:
                return b is ClientSemanticTokensRequestFullDelta otherClientSemanticTokensRequestFullDelta && EqualShape_e1f502acb9f727c5(value, otherClientSemanticTokensRequestFullDelta);
            case InitializationOptions value:
                return b is InitializationOptions otherInitializationOptions && EqualShape_d6291485da709fea(value, otherInitializationOptions);
            case AutoImportFix value:
                return b is AutoImportFix otherAutoImportFix && EqualShape_2fa6cdfb6ada2628(value, otherAutoImportFix);
            case CompletionItemData value:
                return b is CompletionItemData otherCompletionItemData && EqualShape_ec574f9f7f49a56a(value, otherCompletionItemData);
            case CodeLensData value:
                return b is CodeLensData otherCodeLensData && EqualShape_f549b1b2ff50cdf8(value, otherCodeLensData);
            case ExperimentalServerCapabilities value:
                return b is ExperimentalServerCapabilities otherExperimentalServerCapabilities && EqualShape_8328a3a42170a833(value, otherExperimentalServerCapabilities);
            case ExperimentalClientCapabilities value:
                return b is ExperimentalClientCapabilities otherExperimentalClientCapabilities && EqualShape_450a644bbefb758d(value, otherExperimentalClientCapabilities);
            case VSOnAutoInsertOptions value:
                return b is VSOnAutoInsertOptions otherVSOnAutoInsertOptions && EqualShape_cbb6e344e6f178f9(value, otherVSOnAutoInsertOptions);
            case VSReferenceItem value:
                return b is VSReferenceItem otherVSReferenceItem && EqualShape_65d84b8c94226d82(value, otherVSReferenceItem);
            case VSOnAutoInsertParams value:
                return b is VSOnAutoInsertParams otherVSOnAutoInsertParams && EqualShape_90625166f43f5eca(value, otherVSOnAutoInsertParams);
            case VSOnAutoInsertResponseItem value:
                return b is VSOnAutoInsertResponseItem otherVSOnAutoInsertResponseItem && EqualShape_d38b6e9912bc25e1(value, otherVSOnAutoInsertResponseItem);
            case RequestFailureTelemetryEvent value:
                return b is RequestFailureTelemetryEvent otherRequestFailureTelemetryEvent && EqualShape_aa1fe6bec4869c52(value, otherRequestFailureTelemetryEvent);
            case RequestFailureTelemetryProperties value:
                return b is RequestFailureTelemetryProperties otherRequestFailureTelemetryProperties && EqualShape_6cfd8d46d0edce53(value, otherRequestFailureTelemetryProperties);
            case ProfileParams value:
                return b is ProfileParams otherProfileParams && EqualShape_643784fd09a0a166(value, otherProfileParams);
            case ProfileResult value:
                return b is ProfileResult otherProfileResult && EqualShape_ea1fbfb94ccf57aa(value, otherProfileResult);
            case InitializeAPISessionParams value:
                return b is InitializeAPISessionParams otherInitializeAPISessionParams && EqualShape_0f19f7201ba374f8(value, otherInitializeAPISessionParams);
            case InitializeAPISessionResult value:
                return b is InitializeAPISessionResult otherInitializeAPISessionResult && EqualShape_d5842f43f86d28d8(value, otherInitializeAPISessionResult);
            case ProjectInfoParams value:
                return b is ProjectInfoParams otherProjectInfoParams && EqualShape_82dae73cd3c77e28(value, otherProjectInfoParams);
            case ProjectInfoResult value:
                return b is ProjectInfoResult otherProjectInfoResult && EqualShape_1a1c57207754153f(value, otherProjectInfoResult);
            case ContentMapperManifest value:
                return b is ContentMapperManifest otherContentMapperManifest && EqualShape_ac126e252e4ca42c(value, otherContentMapperManifest);
            case InferredProjectContentMapperContribution value:
                return b is InferredProjectContentMapperContribution otherInferredProjectContentMapperContribution && EqualShape_44f48a6fd5281753(value, otherInferredProjectContentMapperContribution);
            case ContentMapperContribution value:
                return b is ContentMapperContribution otherContentMapperContribution && EqualShape_dfd2d49e4be6f7a1(value, otherContentMapperContribution);
            case SetContentMapperContributionsParams value:
                return b is SetContentMapperContributionsParams otherSetContentMapperContributionsParams && EqualShape_1171d758ea6161e3(value, otherSetContentMapperContributionsParams);
            case SetLogVerbosityParams value:
                return b is SetLogVerbosityParams otherSetLogVerbosityParams && EqualShape_b4447bd7705f87ab(value, otherSetLogVerbosityParams);
            case PerformanceStatsTelemetryEvent value:
                return b is PerformanceStatsTelemetryEvent otherPerformanceStatsTelemetryEvent && EqualShape_4ba91157fee437e2(value, otherPerformanceStatsTelemetryEvent);
            case PerformanceStatsTelemetryMeasurements value:
                return b is PerformanceStatsTelemetryMeasurements otherPerformanceStatsTelemetryMeasurements && EqualShape_eca5393ae77d3617(value, otherPerformanceStatsTelemetryMeasurements);
            case ProjectInfoTelemetryEvent value:
                return b is ProjectInfoTelemetryEvent otherProjectInfoTelemetryEvent && EqualShape_f3af5d7035530534(value, otherProjectInfoTelemetryEvent);
            case ProjectInfoTelemetryMeasurements value:
                return b is ProjectInfoTelemetryMeasurements otherProjectInfoTelemetryMeasurements && EqualShape_d4d842b2a58207a2(value, otherProjectInfoTelemetryMeasurements);
            case MultiDocumentHighlight value:
                return b is MultiDocumentHighlight otherMultiDocumentHighlight && EqualShape_01d7598b5a7fa2ba(value, otherMultiDocumentHighlight);
            case MultiDocumentHighlightParams value:
                return b is MultiDocumentHighlightParams otherMultiDocumentHighlightParams && EqualShape_d34dfd9b81abfe93(value, otherMultiDocumentHighlightParams);
            case VSClassifiedTextRun value:
                return b is VSClassifiedTextRun otherVSClassifiedTextRun && EqualShape_29e032165608952d(value, otherVSClassifiedTextRun);
            case VSClassifiedTextElement value:
                return b is VSClassifiedTextElement otherVSClassifiedTextElement && EqualShape_71a22c52ef2b5944(value, otherVSClassifiedTextElement);
            case VSImageId value:
                return b is VSImageId otherVSImageId && EqualShape_b601db4f6bc4b2de(value, otherVSImageId);
            case VSImageElement value:
                return b is VSImageElement otherVSImageElement && EqualShape_87ce8a8264759003(value, otherVSImageElement);
            case VSContainerElement value:
                return b is VSContainerElement otherVSContainerElement && EqualShape_ef2ac6c5999dfb93(value, otherVSContainerElement);
            case CallHierarchyItemData value:
                return b is CallHierarchyItemData otherCallHierarchyItemData && EqualShape_65359ea06e6d25e6(value, otherCallHierarchyItemData);
            case TypeHierarchyItemData value:
                return b is TypeHierarchyItemData otherTypeHierarchyItemData && EqualShape_f33a5bc24ebcc136(value, otherTypeHierarchyItemData);
            case InlayHintData value:
                return b is InlayHintData otherInlayHintData && EqualShape_ec25b7a7c027131f(value, otherInlayHintData);
            case CodeActionData value:
                return b is CodeActionData otherCodeActionData && EqualShape_64c59af8f44adf5e(value, otherCodeActionData);
            case WorkspaceSymbolData value:
                return b is WorkspaceSymbolData otherWorkspaceSymbolData && EqualShape_d6eb674f2d558af4(value, otherWorkspaceSymbolData);
            case DocumentLinkData value:
                return b is DocumentLinkData otherDocumentLinkData && EqualShape_4c9581f87cdeb817(value, otherDocumentLinkData);
            case DiagnosticData value:
                return b is DiagnosticData otherDiagnosticData && EqualShape_29ddb33ca2a0e609(value, otherDiagnosticData);
            case CompletionItemDefaultsData value:
                return b is CompletionItemDefaultsData otherCompletionItemDefaultsData && EqualShape_04b1eeb817033ba6(value, otherCompletionItemDefaultsData);
            case SemanticTokenType value:
                return b is SemanticTokenType otherSemanticTokenType && EqualShape_ffe0cd0a8bc7038a(value, otherSemanticTokenType);
            case SemanticTokenModifier value:
                return b is SemanticTokenModifier otherSemanticTokenModifier && EqualShape_6b5b03f3fef5cc31(value, otherSemanticTokenModifier);
            case DocumentDiagnosticReportKind value:
                return b is DocumentDiagnosticReportKind otherDocumentDiagnosticReportKind && EqualShape_b8295814ec2d845f(value, otherDocumentDiagnosticReportKind);
            case ErrorCode value:
                return b is ErrorCode otherErrorCode && EqualShape_b1b889e0a6a2ea91(value, otherErrorCode);
            case FoldingRangeKind value:
                return b is FoldingRangeKind otherFoldingRangeKind && EqualShape_9bed5894ab8b426e(value, otherFoldingRangeKind);
            case SymbolKind value:
                return b is SymbolKind otherSymbolKind && EqualShape_80525ce65b460521(value, otherSymbolKind);
            case SymbolTag value:
                return b is SymbolTag otherSymbolTag && EqualShape_977c9077c2130dba(value, otherSymbolTag);
            case UniquenessLevel value:
                return b is UniquenessLevel otherUniquenessLevel && EqualShape_52a1f306704bcdf2(value, otherUniquenessLevel);
            case MonikerKind value:
                return b is MonikerKind otherMonikerKind && EqualShape_7d2dfd2063c3273d(value, otherMonikerKind);
            case InlayHintKind value:
                return b is InlayHintKind otherInlayHintKind && EqualShape_53926c928508e1cb(value, otherInlayHintKind);
            case MessageType value:
                return b is MessageType otherMessageType && EqualShape_6ff9dd7ca34940c3(value, otherMessageType);
            case TextDocumentSyncKind value:
                return b is TextDocumentSyncKind otherTextDocumentSyncKind && EqualShape_1259c5dfc44b321e(value, otherTextDocumentSyncKind);
            case TextDocumentSaveReason value:
                return b is TextDocumentSaveReason otherTextDocumentSaveReason && EqualShape_9ba5b7e396b1b1ca(value, otherTextDocumentSaveReason);
            case CompletionItemKind value:
                return b is CompletionItemKind otherCompletionItemKind && EqualShape_b8f2471eb8fc0cb3(value, otherCompletionItemKind);
            case CompletionItemTag value:
                return b is CompletionItemTag otherCompletionItemTag && EqualShape_5397ab5a342cc703(value, otherCompletionItemTag);
            case InsertTextFormat value:
                return b is InsertTextFormat otherInsertTextFormat && EqualShape_0125b1afb99b0757(value, otherInsertTextFormat);
            case InsertTextMode value:
                return b is InsertTextMode otherInsertTextMode && EqualShape_6cddb8f12d636f30(value, otherInsertTextMode);
            case DocumentHighlightKind value:
                return b is DocumentHighlightKind otherDocumentHighlightKind && EqualShape_5f2a94526ab2d089(value, otherDocumentHighlightKind);
            case CodeActionKind value:
                return b is CodeActionKind otherCodeActionKind && EqualShape_ecff755ce44421bb(value, otherCodeActionKind);
            case CodeActionTag value:
                return b is CodeActionTag otherCodeActionTag && EqualShape_22fd923137cdbcf0(value, otherCodeActionTag);
            case TraceValue value:
                return b is TraceValue otherTraceValue && EqualShape_98b926ad367a3bcb(value, otherTraceValue);
            case MarkupKind value:
                return b is MarkupKind otherMarkupKind && EqualShape_2ecdda95186829dd(value, otherMarkupKind);
            case LanguageKind value:
                return b is LanguageKind otherLanguageKind && EqualShape_74d0ea0a98141b4c(value, otherLanguageKind);
            case InlineCompletionTriggerKind value:
                return b is InlineCompletionTriggerKind otherInlineCompletionTriggerKind && EqualShape_163ef7bab0fae34d(value, otherInlineCompletionTriggerKind);
            case PositionEncodingKind value:
                return b is PositionEncodingKind otherPositionEncodingKind && EqualShape_996a119923a77207(value, otherPositionEncodingKind);
            case FileChangeType value:
                return b is FileChangeType otherFileChangeType && EqualShape_1d3058865d82fb8b(value, otherFileChangeType);
            case WatchKind value:
                return b is WatchKind otherWatchKind && EqualShape_df60ed0d4edd21e9(value, otherWatchKind);
            case DiagnosticSeverity value:
                return b is DiagnosticSeverity otherDiagnosticSeverity && EqualShape_bab4fe1f8b21f9fa(value, otherDiagnosticSeverity);
            case DiagnosticTag value:
                return b is DiagnosticTag otherDiagnosticTag && EqualShape_1a56dbf398d99d2f(value, otherDiagnosticTag);
            case CompletionTriggerKind value:
                return b is CompletionTriggerKind otherCompletionTriggerKind && EqualShape_08b806e2da960947(value, otherCompletionTriggerKind);
            case ApplyKind value:
                return b is ApplyKind otherApplyKind && EqualShape_14b702d0db1726aa(value, otherApplyKind);
            case SignatureHelpTriggerKind value:
                return b is SignatureHelpTriggerKind otherSignatureHelpTriggerKind && EqualShape_a60e6e214e11d2ad(value, otherSignatureHelpTriggerKind);
            case CodeActionTriggerKind value:
                return b is CodeActionTriggerKind otherCodeActionTriggerKind && EqualShape_01415bcc3e8debdf(value, otherCodeActionTriggerKind);
            case FileOperationPatternKind value:
                return b is FileOperationPatternKind otherFileOperationPatternKind && EqualShape_c9ac6b08cd0fcaec(value, otherFileOperationPatternKind);
            case ResourceOperationKind value:
                return b is ResourceOperationKind otherResourceOperationKind && EqualShape_3eec28594f7be075(value, otherResourceOperationKind);
            case FailureHandlingKind value:
                return b is FailureHandlingKind otherFailureHandlingKind && EqualShape_484e4404d3769d00(value, otherFailureHandlingKind);
            case PrepareSupportDefaultBehavior value:
                return b is PrepareSupportDefaultBehavior otherPrepareSupportDefaultBehavior && EqualShape_491ada003a0c8630(value, otherPrepareSupportDefaultBehavior);
            case TokenFormat value:
                return b is TokenFormat otherTokenFormat && EqualShape_61c52b50223120a3(value, otherTokenFormat);
            case VSContainerElementStyle value:
                return b is VSContainerElementStyle otherVSContainerElementStyle && EqualShape_474d572894555703(value, otherVSContainerElementStyle);
            case LogVerbosity value:
                return b is LogVerbosity otherLogVerbosity && EqualShape_061133d05f438d15(value, otherLogVerbosity);
            case DiagnosticFlakeLogLevel value:
                return b is DiagnosticFlakeLogLevel otherDiagnosticFlakeLogLevel && EqualShape_1ca005e11eb3df3a(value, otherDiagnosticFlakeLogLevel);
            case VSReferenceKind value:
                return b is VSReferenceKind otherVSReferenceKind && EqualShape_20bf14346b717f60(value, otherVSReferenceKind);
            case CodeLensKind value:
                return b is CodeLensKind otherCodeLensKind && EqualShape_4be58987e0e9adc8(value, otherCodeLensKind);
            case AutoImportFixKind value:
                return b is AutoImportFixKind otherAutoImportFixKind && EqualShape_3cb7a6f693cd9a7a(value, otherAutoImportFixKind);
            case ImportKind value:
                return b is ImportKind otherImportKind && EqualShape_d39772dfff646e6d(value, otherImportKind);
            case AddAsTypeOnly value:
                return b is AddAsTypeOnly otherAddAsTypeOnly && EqualShape_5bfde1568463cec7(value, otherAddAsTypeOnly);
            case ClassificationTypeName value:
                return b is ClassificationTypeName otherClassificationTypeName && EqualShape_2517ecc24dbf1859(value, otherClassificationTypeName);
            case IntegerOrString value:
                return b is IntegerOrString otherIntegerOrString && EqualShape_95e25e81cd99bf99(value, otherIntegerOrString);
            case DocumentSelectorOrNull value:
                return b is DocumentSelectorOrNull otherDocumentSelectorOrNull && EqualShape_c7819f785729d94c(value, otherDocumentSelectorOrNull);
            case BooleanOrEmptyObject value:
                return b is BooleanOrEmptyObject otherBooleanOrEmptyObject && EqualShape_0464b31e8eb7a40e(value, otherBooleanOrEmptyObject);
            case BooleanOrSemanticTokensFullDelta value:
                return b is BooleanOrSemanticTokensFullDelta otherBooleanOrSemanticTokensFullDelta && EqualShape_7df65eef7163c6f4(value, otherBooleanOrSemanticTokensFullDelta);
            case TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile value:
                return b is TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile otherTextDocumentEditOrCreateFileOrRenameFileOrDeleteFile && EqualShape_d30cdf3f6150538e(value, otherTextDocumentEditOrCreateFileOrRenameFileOrDeleteFile);
            case StringOrInlayHintLabelParts value:
                return b is StringOrInlayHintLabelParts otherStringOrInlayHintLabelParts && EqualShape_be643dd009a1ea80(value, otherStringOrInlayHintLabelParts);
            case StringOrMarkupContent value:
                return b is StringOrMarkupContent otherStringOrMarkupContent && EqualShape_31e9efe8f25ec13e(value, otherStringOrMarkupContent);
            case WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport value:
                return b is WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport otherWorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport && EqualShape_04b29b87f373e8dd(value, otherWorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport);
            case StringOrStringValue value:
                return b is StringOrStringValue otherStringOrStringValue && EqualShape_7485e0c10f1b758e(value, otherStringOrStringValue);
            case IntegerOrNull value:
                return b is IntegerOrNull otherIntegerOrNull && EqualShape_17d3cfba51c6c525(value, otherIntegerOrNull);
            case StringOrNull value:
                return b is StringOrNull otherStringOrNull && EqualShape_5ada2186dac127d6(value, otherStringOrNull);
            case DocumentUriOrNull value:
                return b is DocumentUriOrNull otherDocumentUriOrNull && EqualShape_b23e05e41f22b21f(value, otherDocumentUriOrNull);
            case InitializationOptionsOrNull value:
                return b is InitializationOptionsOrNull otherInitializationOptionsOrNull && EqualShape_36ae92715ecba9c7(value, otherInitializationOptionsOrNull);
            case WorkspaceFoldersOrNull value:
                return b is WorkspaceFoldersOrNull otherWorkspaceFoldersOrNull && EqualShape_a529bfd28675e22d(value, otherWorkspaceFoldersOrNull);
            case StringOrStrings value:
                return b is StringOrStrings otherStringOrStrings && EqualShape_23dbff1cf97c4515(value, otherStringOrStrings);
            case TextDocumentContentChangePartialOrWholeDocument value:
                return b is TextDocumentContentChangePartialOrWholeDocument otherTextDocumentContentChangePartialOrWholeDocument && EqualShape_67ba06b602f43ed5(value, otherTextDocumentContentChangePartialOrWholeDocument);
            case TextEditOrInsertReplaceEdit value:
                return b is TextEditOrInsertReplaceEdit otherTextEditOrInsertReplaceEdit && EqualShape_b3a71121264d6d6a(value, otherTextEditOrInsertReplaceEdit);
            case MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings value:
                return b is MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings otherMarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings && EqualShape_20ab4652e2622e44(value, otherMarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings);
            case UintegerOrNull value:
                return b is UintegerOrNull otherUintegerOrNull && EqualShape_905a1da3c49bbf1b(value, otherUintegerOrNull);
            case LocationOrLocationUriOnly value:
                return b is LocationOrLocationUriOnly otherLocationOrLocationUriOnly && EqualShape_fb622e0b8d2bf184(value, otherLocationOrLocationUriOnly);
            case WorkDoneProgressBeginOrReportOrEnd value:
                return b is WorkDoneProgressBeginOrReportOrEnd otherWorkDoneProgressBeginOrReportOrEnd && EqualShape_fd6812dd893b247d(value, otherWorkDoneProgressBeginOrReportOrEnd);
            case TextEditOrAnnotatedTextEditOrSnippetTextEdit value:
                return b is TextEditOrAnnotatedTextEditOrSnippetTextEdit otherTextEditOrAnnotatedTextEditOrSnippetTextEdit && EqualShape_2696e31555a9351f(value, otherTextEditOrAnnotatedTextEditOrSnippetTextEdit);
            case FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport value:
                return b is FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport otherFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport && EqualShape_122188e5e315b8c0(value, otherFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport);
            case TextDocumentSyncOptionsOrKind value:
                return b is TextDocumentSyncOptionsOrKind otherTextDocumentSyncOptionsOrKind && EqualShape_c80d765f99706699(value, otherTextDocumentSyncOptionsOrKind);
            case BooleanOrHoverOptions value:
                return b is BooleanOrHoverOptions otherBooleanOrHoverOptions && EqualShape_baa50df5ae26800a(value, otherBooleanOrHoverOptions);
            case BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions value:
                return b is BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions otherBooleanOrDeclarationOptionsOrDeclarationRegistrationOptions && EqualShape_145e615b6579c981(value, otherBooleanOrDeclarationOptionsOrDeclarationRegistrationOptions);
            case BooleanOrDefinitionOptions value:
                return b is BooleanOrDefinitionOptions otherBooleanOrDefinitionOptions && EqualShape_cf52b6cb9db4fe19(value, otherBooleanOrDefinitionOptions);
            case BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions value:
                return b is BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions otherBooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions && EqualShape_ed71ad313eefc7ad(value, otherBooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions);
            case BooleanOrImplementationOptionsOrImplementationRegistrationOptions value:
                return b is BooleanOrImplementationOptionsOrImplementationRegistrationOptions otherBooleanOrImplementationOptionsOrImplementationRegistrationOptions && EqualShape_15d10a5acdcde7ec(value, otherBooleanOrImplementationOptionsOrImplementationRegistrationOptions);
            case BooleanOrReferenceOptions value:
                return b is BooleanOrReferenceOptions otherBooleanOrReferenceOptions && EqualShape_3b3fec573e871e5d(value, otherBooleanOrReferenceOptions);
            case BooleanOrDocumentHighlightOptions value:
                return b is BooleanOrDocumentHighlightOptions otherBooleanOrDocumentHighlightOptions && EqualShape_54fc7b1b34343b4a(value, otherBooleanOrDocumentHighlightOptions);
            case BooleanOrDocumentSymbolOptions value:
                return b is BooleanOrDocumentSymbolOptions otherBooleanOrDocumentSymbolOptions && EqualShape_c021a4c2316929b7(value, otherBooleanOrDocumentSymbolOptions);
            case BooleanOrCodeActionOptions value:
                return b is BooleanOrCodeActionOptions otherBooleanOrCodeActionOptions && EqualShape_e9690919b3d8115d(value, otherBooleanOrCodeActionOptions);
            case BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions value:
                return b is BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions otherBooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions && EqualShape_1474d27e4d0c6fa5(value, otherBooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions);
            case BooleanOrWorkspaceSymbolOptions value:
                return b is BooleanOrWorkspaceSymbolOptions otherBooleanOrWorkspaceSymbolOptions && EqualShape_e5bfff85c753a8b1(value, otherBooleanOrWorkspaceSymbolOptions);
            case BooleanOrDocumentFormattingOptions value:
                return b is BooleanOrDocumentFormattingOptions otherBooleanOrDocumentFormattingOptions && EqualShape_a917cb45ee3cef42(value, otherBooleanOrDocumentFormattingOptions);
            case BooleanOrDocumentRangeFormattingOptions value:
                return b is BooleanOrDocumentRangeFormattingOptions otherBooleanOrDocumentRangeFormattingOptions && EqualShape_c5052a5c26268d1f(value, otherBooleanOrDocumentRangeFormattingOptions);
            case BooleanOrRenameOptions value:
                return b is BooleanOrRenameOptions otherBooleanOrRenameOptions && EqualShape_504bddb6f7a15921(value, otherBooleanOrRenameOptions);
            case BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions value:
                return b is BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions otherBooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions && EqualShape_0a0a219b5ffede8b(value, otherBooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions);
            case BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions value:
                return b is BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions otherBooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions && EqualShape_5d2769c3484cdd3e(value, otherBooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions);
            case BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions value:
                return b is BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions otherBooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions && EqualShape_446299ae2bd49e81(value, otherBooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions);
            case BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions value:
                return b is BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions otherBooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions && EqualShape_0d2d61395bd0e247(value, otherBooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions);
            case SemanticTokensOptionsOrRegistrationOptions value:
                return b is SemanticTokensOptionsOrRegistrationOptions otherSemanticTokensOptionsOrRegistrationOptions && EqualShape_45b9b7d638819fbf(value, otherSemanticTokensOptionsOrRegistrationOptions);
            case BooleanOrMonikerOptionsOrMonikerRegistrationOptions value:
                return b is BooleanOrMonikerOptionsOrMonikerRegistrationOptions otherBooleanOrMonikerOptionsOrMonikerRegistrationOptions && EqualShape_afd51367d6d2382c(value, otherBooleanOrMonikerOptionsOrMonikerRegistrationOptions);
            case BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions value:
                return b is BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions otherBooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions && EqualShape_87827fc2be5c0773(value, otherBooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions);
            case BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions value:
                return b is BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions otherBooleanOrInlineValueOptionsOrInlineValueRegistrationOptions && EqualShape_1d2b7a4459f0e39b(value, otherBooleanOrInlineValueOptionsOrInlineValueRegistrationOptions);
            case BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions value:
                return b is BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions otherBooleanOrInlayHintOptionsOrInlayHintRegistrationOptions && EqualShape_feb69b4dbb92199a(value, otherBooleanOrInlayHintOptionsOrInlayHintRegistrationOptions);
            case DiagnosticOptionsOrRegistrationOptions value:
                return b is DiagnosticOptionsOrRegistrationOptions otherDiagnosticOptionsOrRegistrationOptions && EqualShape_703869dd83e88e30(value, otherDiagnosticOptionsOrRegistrationOptions);
            case BooleanOrInlineCompletionOptions value:
                return b is BooleanOrInlineCompletionOptions otherBooleanOrInlineCompletionOptions && EqualShape_959a1d992a272787(value, otherBooleanOrInlineCompletionOptions);
            case PatternOrRelativePattern value:
                return b is PatternOrRelativePattern otherPatternOrRelativePattern && EqualShape_555c0b23fa28c7a4(value, otherPatternOrRelativePattern);
            case RangeOrEditRangeWithInsertReplace value:
                return b is RangeOrEditRangeWithInsertReplace otherRangeOrEditRangeWithInsertReplace && EqualShape_88865600aa610d9f(value, otherRangeOrEditRangeWithInsertReplace);
            case BooleanOrSaveOptions value:
                return b is BooleanOrSaveOptions otherBooleanOrSaveOptions && EqualShape_41952583e79cd693(value, otherBooleanOrSaveOptions);
            case TextDocumentContentOptionsOrRegistrationOptions value:
                return b is TextDocumentContentOptionsOrRegistrationOptions otherTextDocumentContentOptionsOrRegistrationOptions && EqualShape_12d4c2faae1da2c4(value, otherTextDocumentContentOptionsOrRegistrationOptions);
            case StringOrTuple value:
                return b is StringOrTuple otherStringOrTuple && EqualShape_9f68e715f063af80(value, otherStringOrTuple);
            case StringOrBoolean value:
                return b is StringOrBoolean otherStringOrBoolean && EqualShape_4da8fc01584b07f2(value, otherStringOrBoolean);
            case WorkspaceFolderOrURI value:
                return b is WorkspaceFolderOrURI otherWorkspaceFolderOrURI && EqualShape_f9f7734231f531e1(value, otherWorkspaceFolderOrURI);
            case BooleanOrClientSemanticTokensRequestFullDelta value:
                return b is BooleanOrClientSemanticTokensRequestFullDelta otherBooleanOrClientSemanticTokensRequestFullDelta && EqualShape_ceb7a942d3e190ba(value, otherBooleanOrClientSemanticTokensRequestFullDelta);
            case VSImageElementOrClassifiedTextElementOrContainerElement value:
                return b is VSImageElementOrClassifiedTextElementOrContainerElement otherVSImageElementOrClassifiedTextElementOrContainerElement && EqualShape_e43f96073fda3c06(value, otherVSImageElementOrClassifiedTextElementOrContainerElement);
            case LocationOrLocationsOrDefinitionLinksOrNull value:
                return b is LocationOrLocationsOrDefinitionLinksOrNull otherLocationOrLocationsOrDefinitionLinksOrNull && EqualShape_e65bade6fa339317(value, otherLocationOrLocationsOrDefinitionLinksOrNull);
            case FoldingRangesOrNull value:
                return b is FoldingRangesOrNull otherFoldingRangesOrNull && EqualShape_c0e2536c958d686b(value, otherFoldingRangesOrNull);
            case LocationOrLocationsOrDeclarationLinksOrNull value:
                return b is LocationOrLocationsOrDeclarationLinksOrNull otherLocationOrLocationsOrDeclarationLinksOrNull && EqualShape_224e82bb96ae8cfe(value, otherLocationOrLocationsOrDeclarationLinksOrNull);
            case SelectionRangesOrNull value:
                return b is SelectionRangesOrNull otherSelectionRangesOrNull && EqualShape_130b4f72041fba63(value, otherSelectionRangesOrNull);
            case CallHierarchyItemsOrNull value:
                return b is CallHierarchyItemsOrNull otherCallHierarchyItemsOrNull && EqualShape_90a14ad0a9a8b850(value, otherCallHierarchyItemsOrNull);
            case CallHierarchyIncomingCallsOrNull value:
                return b is CallHierarchyIncomingCallsOrNull otherCallHierarchyIncomingCallsOrNull && EqualShape_d97c2ffc45ab575f(value, otherCallHierarchyIncomingCallsOrNull);
            case CallHierarchyOutgoingCallsOrNull value:
                return b is CallHierarchyOutgoingCallsOrNull otherCallHierarchyOutgoingCallsOrNull && EqualShape_7db6fb25db830142(value, otherCallHierarchyOutgoingCallsOrNull);
            case SemanticTokensOrNull value:
                return b is SemanticTokensOrNull otherSemanticTokensOrNull && EqualShape_ea15ccbea3b9ff1e(value, otherSemanticTokensOrNull);
            case SemanticTokensOrSemanticTokensDeltaOrNull value:
                return b is SemanticTokensOrSemanticTokensDeltaOrNull otherSemanticTokensOrSemanticTokensDeltaOrNull && EqualShape_0e9da642d9c60622(value, otherSemanticTokensOrSemanticTokensDeltaOrNull);
            case LinkedEditingRangesOrNull value:
                return b is LinkedEditingRangesOrNull otherLinkedEditingRangesOrNull && EqualShape_f076e01264070196(value, otherLinkedEditingRangesOrNull);
            case WorkspaceEditOrNull value:
                return b is WorkspaceEditOrNull otherWorkspaceEditOrNull && EqualShape_8ef40582cdaf80c7(value, otherWorkspaceEditOrNull);
            case MonikersOrNull value:
                return b is MonikersOrNull otherMonikersOrNull && EqualShape_0d916739fbbbc286(value, otherMonikersOrNull);
            case TypeHierarchyItemsOrNull value:
                return b is TypeHierarchyItemsOrNull otherTypeHierarchyItemsOrNull && EqualShape_52355c9e633f440a(value, otherTypeHierarchyItemsOrNull);
            case InlineValuesOrNull value:
                return b is InlineValuesOrNull otherInlineValuesOrNull && EqualShape_8992dae46a973c42(value, otherInlineValuesOrNull);
            case InlayHintsOrNull value:
                return b is InlayHintsOrNull otherInlayHintsOrNull && EqualShape_e59fdbb1e7ba4144(value, otherInlayHintsOrNull);
            case RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport value:
                return b is RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport otherRelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport && EqualShape_c4ca009a9b03016c(value, otherRelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport);
            case InlineCompletionListOrItemsOrNull value:
                return b is InlineCompletionListOrItemsOrNull otherInlineCompletionListOrItemsOrNull && EqualShape_1970c1b9314d0672(value, otherInlineCompletionListOrItemsOrNull);
            case MessageActionItemOrNull value:
                return b is MessageActionItemOrNull otherMessageActionItemOrNull && EqualShape_5853b8e3ac90f099(value, otherMessageActionItemOrNull);
            case TextEditsOrNull value:
                return b is TextEditsOrNull otherTextEditsOrNull && EqualShape_5571522b69357c5d(value, otherTextEditsOrNull);
            case CompletionItemsOrListOrNull value:
                return b is CompletionItemsOrListOrNull otherCompletionItemsOrListOrNull && EqualShape_96d4df6d20dad568(value, otherCompletionItemsOrListOrNull);
            case HoverOrNull value:
                return b is HoverOrNull otherHoverOrNull && EqualShape_eed3cf3342b20f8c(value, otherHoverOrNull);
            case SignatureHelpOrNull value:
                return b is SignatureHelpOrNull otherSignatureHelpOrNull && EqualShape_888fc079fe4e5694(value, otherSignatureHelpOrNull);
            case LocationsOrNull value:
                return b is LocationsOrNull otherLocationsOrNull && EqualShape_e255434a1eda226b(value, otherLocationsOrNull);
            case DocumentHighlightsOrNull value:
                return b is DocumentHighlightsOrNull otherDocumentHighlightsOrNull && EqualShape_de9824a43cc27a46(value, otherDocumentHighlightsOrNull);
            case SymbolInformationsOrDocumentSymbolsOrNull value:
                return b is SymbolInformationsOrDocumentSymbolsOrNull otherSymbolInformationsOrDocumentSymbolsOrNull && EqualShape_abef6860edeb78c0(value, otherSymbolInformationsOrDocumentSymbolsOrNull);
            case CommandOrCodeAction value:
                return b is CommandOrCodeAction otherCommandOrCodeAction && EqualShape_58a4b3825288a73b(value, otherCommandOrCodeAction);
            case CommandOrCodeActionArrayOrNull value:
                return b is CommandOrCodeActionArrayOrNull otherCommandOrCodeActionArrayOrNull && EqualShape_cada04bb4fe9d5c5(value, otherCommandOrCodeActionArrayOrNull);
            case SymbolInformationsOrWorkspaceSymbolsOrNull value:
                return b is SymbolInformationsOrWorkspaceSymbolsOrNull otherSymbolInformationsOrWorkspaceSymbolsOrNull && EqualShape_e6d72008fd49de7e(value, otherSymbolInformationsOrWorkspaceSymbolsOrNull);
            case CodeLensesOrNull value:
                return b is CodeLensesOrNull otherCodeLensesOrNull && EqualShape_a0de8ff39d760531(value, otherCodeLensesOrNull);
            case DocumentLinksOrNull value:
                return b is DocumentLinksOrNull otherDocumentLinksOrNull && EqualShape_1589962d250e4fa8(value, otherDocumentLinksOrNull);
            case RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull value:
                return b is RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull otherRangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull && EqualShape_d42fdd8e2304fd1f(value, otherRangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull);
            case LSPAnyOrNull value:
                return b is LSPAnyOrNull otherLSPAnyOrNull && EqualShape_a0f960cf093cfaa2(value, otherLSPAnyOrNull);
            case MultiDocumentHighlightsOrNull value:
                return b is MultiDocumentHighlightsOrNull otherMultiDocumentHighlightsOrNull && EqualShape_3a79319ef19a1228(value, otherMultiDocumentHighlightsOrNull);
            case VSOnAutoInsertResponseItemOrNull value:
                return b is VSOnAutoInsertResponseItemOrNull otherVSOnAutoInsertResponseItemOrNull && EqualShape_eea9e0fe8e3bc71f(value, otherVSOnAutoInsertResponseItemOrNull);
            case VSReferenceItemsOrNull value:
                return b is VSReferenceItemsOrNull otherVSReferenceItemsOrNull && EqualShape_43cf43781798e7ed(value, otherVSReferenceItemsOrNull);
            case RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull value:
                return b is RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull otherRequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull && EqualShape_9d57f0706e39340b(value, otherRequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull);
            case TextDocumentFilterLanguageOrSchemeOrPattern value:
                return b is TextDocumentFilterLanguageOrSchemeOrPattern otherTextDocumentFilterLanguageOrSchemeOrPattern && EqualShape_f05eaa57d84a7b55(value, otherTextDocumentFilterLanguageOrSchemeOrPattern);
            case StringOrMarkedStringWithLanguage value:
                return b is StringOrMarkedStringWithLanguage otherStringOrMarkedStringWithLanguage && EqualShape_ae8e2688e3b7bd25(value, otherStringOrMarkedStringWithLanguage);
            case InlineValueTextOrVariableLookupOrEvaluatableExpression value:
                return b is InlineValueTextOrVariableLookupOrEvaluatableExpression otherInlineValueTextOrVariableLookupOrEvaluatableExpression && EqualShape_9b1858affa016849(value, otherInlineValueTextOrVariableLookupOrEvaluatableExpression);
            case StringLiteralBegin value:
                return b is StringLiteralBegin otherStringLiteralBegin && EqualShape_88a5baf5906d4eb9(value, otherStringLiteralBegin);
            case StringLiteralReport value:
                return b is StringLiteralReport otherStringLiteralReport && EqualShape_0e789aef680d8005(value, otherStringLiteralReport);
            case StringLiteralEnd value:
                return b is StringLiteralEnd otherStringLiteralEnd && EqualShape_46e624b49f981eb0(value, otherStringLiteralEnd);
            case StringLiteralCreate value:
                return b is StringLiteralCreate otherStringLiteralCreate && EqualShape_15379156e683e93e(value, otherStringLiteralCreate);
            case StringLiteralRename value:
                return b is StringLiteralRename otherStringLiteralRename && EqualShape_40fcf4d785d811c9(value, otherStringLiteralRename);
            case StringLiteralDelete value:
                return b is StringLiteralDelete otherStringLiteralDelete && EqualShape_6681ac8dac15fa23(value, otherStringLiteralDelete);
            case StringLiteralFull value:
                return b is StringLiteralFull otherStringLiteralFull && EqualShape_4a4f64afaada24e4(value, otherStringLiteralFull);
            case StringLiteralUnchanged value:
                return b is StringLiteralUnchanged otherStringLiteralUnchanged && EqualShape_830ba33890174a2d(value, otherStringLiteralUnchanged);
            case StringLiteralSnippet value:
                return b is StringLiteralSnippet otherStringLiteralSnippet && EqualShape_f79bae81a80b15e3(value, otherStringLiteralSnippet);
            case StringLiteralLanguageServerErrorResponse value:
                return b is StringLiteralLanguageServerErrorResponse otherStringLiteralLanguageServerErrorResponse && EqualShape_5c63c9ad8dcf6ff2(value, otherStringLiteralLanguageServerErrorResponse);
            case StringLiteralError value:
                return b is StringLiteralError otherStringLiteralError && EqualShape_96e1158afa77c90c(value, otherStringLiteralError);
            case StringLiteralLanguageServerPerformanceStats value:
                return b is StringLiteralLanguageServerPerformanceStats otherStringLiteralLanguageServerPerformanceStats && EqualShape_80a5c2c59f770ffa(value, otherStringLiteralLanguageServerPerformanceStats);
            case StringLiteralUsage value:
                return b is StringLiteralUsage otherStringLiteralUsage && EqualShape_45902ec7f4da96d7(value, otherStringLiteralUsage);
            case StringLiteralLanguageServerProjectInfo value:
                return b is StringLiteralLanguageServerProjectInfo otherStringLiteralLanguageServerProjectInfo && EqualShape_33119583302dabc8(value, otherStringLiteralLanguageServerProjectInfo);
            case StringLiteralClassifiedTextRun value:
                return b is StringLiteralClassifiedTextRun otherStringLiteralClassifiedTextRun && EqualShape_bb0b4343e63f3503(value, otherStringLiteralClassifiedTextRun);
            case StringLiteralClassifiedTextElement value:
                return b is StringLiteralClassifiedTextElement otherStringLiteralClassifiedTextElement && EqualShape_44e043791719404a(value, otherStringLiteralClassifiedTextElement);
            case StringLiteralImageId value:
                return b is StringLiteralImageId otherStringLiteralImageId && EqualShape_f92aabc242322d92(value, otherStringLiteralImageId);
            case StringLiteralImageElement value:
                return b is StringLiteralImageElement otherStringLiteralImageElement && EqualShape_291de7541a6e8a51(value, otherStringLiteralImageElement);
            case StringLiteralContainerElement value:
                return b is StringLiteralContainerElement otherStringLiteralContainerElement && EqualShape_4bde02dc25c7843b(value, otherStringLiteralContainerElement);
            case ResolvedChangeAnnotationsSupportOptions value:
                return b is ResolvedChangeAnnotationsSupportOptions otherResolvedChangeAnnotationsSupportOptions && EqualShape_4d7a594566ef6368(value, otherResolvedChangeAnnotationsSupportOptions);
            case ResolvedWorkspaceEditClientCapabilities value:
                return b is ResolvedWorkspaceEditClientCapabilities otherResolvedWorkspaceEditClientCapabilities && EqualShape_617cc3531ccde6a8(value, otherResolvedWorkspaceEditClientCapabilities);
            case ResolvedDidChangeConfigurationClientCapabilities value:
                return b is ResolvedDidChangeConfigurationClientCapabilities otherResolvedDidChangeConfigurationClientCapabilities && EqualShape_12a2abfb3665ab1e(value, otherResolvedDidChangeConfigurationClientCapabilities);
            case ResolvedDidChangeWatchedFilesClientCapabilities value:
                return b is ResolvedDidChangeWatchedFilesClientCapabilities otherResolvedDidChangeWatchedFilesClientCapabilities && EqualShape_e8f2fbc0da1f4a99(value, otherResolvedDidChangeWatchedFilesClientCapabilities);
            case ResolvedClientSymbolKindOptions value:
                return b is ResolvedClientSymbolKindOptions otherResolvedClientSymbolKindOptions && EqualShape_14836655cb506488(value, otherResolvedClientSymbolKindOptions);
            case ResolvedClientSymbolTagOptions value:
                return b is ResolvedClientSymbolTagOptions otherResolvedClientSymbolTagOptions && EqualShape_20b4b5ac914261e2(value, otherResolvedClientSymbolTagOptions);
            case ResolvedClientSymbolResolveOptions value:
                return b is ResolvedClientSymbolResolveOptions otherResolvedClientSymbolResolveOptions && EqualShape_d25c9406f3bdb053(value, otherResolvedClientSymbolResolveOptions);
            case ResolvedWorkspaceSymbolClientCapabilities value:
                return b is ResolvedWorkspaceSymbolClientCapabilities otherResolvedWorkspaceSymbolClientCapabilities && EqualShape_d66f8c5651bd8423(value, otherResolvedWorkspaceSymbolClientCapabilities);
            case ResolvedExecuteCommandClientCapabilities value:
                return b is ResolvedExecuteCommandClientCapabilities otherResolvedExecuteCommandClientCapabilities && EqualShape_13f53d28d95c16fe(value, otherResolvedExecuteCommandClientCapabilities);
            case ResolvedSemanticTokensWorkspaceClientCapabilities value:
                return b is ResolvedSemanticTokensWorkspaceClientCapabilities otherResolvedSemanticTokensWorkspaceClientCapabilities && EqualShape_d1f533b25382465d(value, otherResolvedSemanticTokensWorkspaceClientCapabilities);
            case ResolvedCodeLensWorkspaceClientCapabilities value:
                return b is ResolvedCodeLensWorkspaceClientCapabilities otherResolvedCodeLensWorkspaceClientCapabilities && EqualShape_0e5fa0acd9a1a453(value, otherResolvedCodeLensWorkspaceClientCapabilities);
            case ResolvedFileOperationClientCapabilities value:
                return b is ResolvedFileOperationClientCapabilities otherResolvedFileOperationClientCapabilities && EqualShape_07e1391212a2fdb5(value, otherResolvedFileOperationClientCapabilities);
            case ResolvedInlineValueWorkspaceClientCapabilities value:
                return b is ResolvedInlineValueWorkspaceClientCapabilities otherResolvedInlineValueWorkspaceClientCapabilities && EqualShape_0cf7bd74ae5beeb5(value, otherResolvedInlineValueWorkspaceClientCapabilities);
            case ResolvedInlayHintWorkspaceClientCapabilities value:
                return b is ResolvedInlayHintWorkspaceClientCapabilities otherResolvedInlayHintWorkspaceClientCapabilities && EqualShape_6f24450c9e850af2(value, otherResolvedInlayHintWorkspaceClientCapabilities);
            case ResolvedDiagnosticWorkspaceClientCapabilities value:
                return b is ResolvedDiagnosticWorkspaceClientCapabilities otherResolvedDiagnosticWorkspaceClientCapabilities && EqualShape_eb7c1ded116c115a(value, otherResolvedDiagnosticWorkspaceClientCapabilities);
            case ResolvedFoldingRangeWorkspaceClientCapabilities value:
                return b is ResolvedFoldingRangeWorkspaceClientCapabilities otherResolvedFoldingRangeWorkspaceClientCapabilities && EqualShape_692f253b9522679f(value, otherResolvedFoldingRangeWorkspaceClientCapabilities);
            case ResolvedTextDocumentContentClientCapabilities value:
                return b is ResolvedTextDocumentContentClientCapabilities otherResolvedTextDocumentContentClientCapabilities && EqualShape_a3299508a5069228(value, otherResolvedTextDocumentContentClientCapabilities);
            case ResolvedWorkspaceClientCapabilities value:
                return b is ResolvedWorkspaceClientCapabilities otherResolvedWorkspaceClientCapabilities && EqualShape_0df6a724a7aef924(value, otherResolvedWorkspaceClientCapabilities);
            case ResolvedTextDocumentSyncClientCapabilities value:
                return b is ResolvedTextDocumentSyncClientCapabilities otherResolvedTextDocumentSyncClientCapabilities && EqualShape_f55898ff0d7d0b57(value, otherResolvedTextDocumentSyncClientCapabilities);
            case ResolvedTextDocumentFilterClientCapabilities value:
                return b is ResolvedTextDocumentFilterClientCapabilities otherResolvedTextDocumentFilterClientCapabilities && EqualShape_6d2551c03ed73195(value, otherResolvedTextDocumentFilterClientCapabilities);
            case ResolvedCompletionItemTagOptions value:
                return b is ResolvedCompletionItemTagOptions otherResolvedCompletionItemTagOptions && EqualShape_dda3a89f1c5e5940(value, otherResolvedCompletionItemTagOptions);
            case ResolvedClientCompletionItemResolveOptions value:
                return b is ResolvedClientCompletionItemResolveOptions otherResolvedClientCompletionItemResolveOptions && EqualShape_de8480b40157ab7f(value, otherResolvedClientCompletionItemResolveOptions);
            case ResolvedClientCompletionItemInsertTextModeOptions value:
                return b is ResolvedClientCompletionItemInsertTextModeOptions otherResolvedClientCompletionItemInsertTextModeOptions && EqualShape_d11f730ae1cedb58(value, otherResolvedClientCompletionItemInsertTextModeOptions);
            case ResolvedClientCompletionItemOptions value:
                return b is ResolvedClientCompletionItemOptions otherResolvedClientCompletionItemOptions && EqualShape_e258bbb0418c80db(value, otherResolvedClientCompletionItemOptions);
            case ResolvedClientCompletionItemOptionsKind value:
                return b is ResolvedClientCompletionItemOptionsKind otherResolvedClientCompletionItemOptionsKind && EqualShape_3c6c4303d29123cf(value, otherResolvedClientCompletionItemOptionsKind);
            case ResolvedCompletionListCapabilities value:
                return b is ResolvedCompletionListCapabilities otherResolvedCompletionListCapabilities && EqualShape_00713122eb953ff0(value, otherResolvedCompletionListCapabilities);
            case ResolvedCompletionClientCapabilities value:
                return b is ResolvedCompletionClientCapabilities otherResolvedCompletionClientCapabilities && EqualShape_ad0f5cb601bc08b8(value, otherResolvedCompletionClientCapabilities);
            case ResolvedHoverClientCapabilities value:
                return b is ResolvedHoverClientCapabilities otherResolvedHoverClientCapabilities && EqualShape_74ee6076b161a5e2(value, otherResolvedHoverClientCapabilities);
            case ResolvedClientSignatureParameterInformationOptions value:
                return b is ResolvedClientSignatureParameterInformationOptions otherResolvedClientSignatureParameterInformationOptions && EqualShape_7ef36a2db7d0846c(value, otherResolvedClientSignatureParameterInformationOptions);
            case ResolvedClientSignatureInformationOptions value:
                return b is ResolvedClientSignatureInformationOptions otherResolvedClientSignatureInformationOptions && EqualShape_18234a841967b063(value, otherResolvedClientSignatureInformationOptions);
            case ResolvedSignatureHelpClientCapabilities value:
                return b is ResolvedSignatureHelpClientCapabilities otherResolvedSignatureHelpClientCapabilities && EqualShape_a25ed1013e366188(value, otherResolvedSignatureHelpClientCapabilities);
            case ResolvedDeclarationClientCapabilities value:
                return b is ResolvedDeclarationClientCapabilities otherResolvedDeclarationClientCapabilities && EqualShape_a42722ce7a0bd593(value, otherResolvedDeclarationClientCapabilities);
            case ResolvedDefinitionClientCapabilities value:
                return b is ResolvedDefinitionClientCapabilities otherResolvedDefinitionClientCapabilities && EqualShape_33dd478114d2d134(value, otherResolvedDefinitionClientCapabilities);
            case ResolvedTypeDefinitionClientCapabilities value:
                return b is ResolvedTypeDefinitionClientCapabilities otherResolvedTypeDefinitionClientCapabilities && EqualShape_b6a3c7fa1b7024cd(value, otherResolvedTypeDefinitionClientCapabilities);
            case ResolvedImplementationClientCapabilities value:
                return b is ResolvedImplementationClientCapabilities otherResolvedImplementationClientCapabilities && EqualShape_6f72f8fa55d75ae2(value, otherResolvedImplementationClientCapabilities);
            case ResolvedReferenceClientCapabilities value:
                return b is ResolvedReferenceClientCapabilities otherResolvedReferenceClientCapabilities && EqualShape_3b8690524f16adbd(value, otherResolvedReferenceClientCapabilities);
            case ResolvedDocumentHighlightClientCapabilities value:
                return b is ResolvedDocumentHighlightClientCapabilities otherResolvedDocumentHighlightClientCapabilities && EqualShape_225fc6c37e90f6f2(value, otherResolvedDocumentHighlightClientCapabilities);
            case ResolvedDocumentSymbolClientCapabilities value:
                return b is ResolvedDocumentSymbolClientCapabilities otherResolvedDocumentSymbolClientCapabilities && EqualShape_53f5bf3e3f4d7e6e(value, otherResolvedDocumentSymbolClientCapabilities);
            case ResolvedClientCodeActionKindOptions value:
                return b is ResolvedClientCodeActionKindOptions otherResolvedClientCodeActionKindOptions && EqualShape_44f7cbe5ecd0606b(value, otherResolvedClientCodeActionKindOptions);
            case ResolvedClientCodeActionLiteralOptions value:
                return b is ResolvedClientCodeActionLiteralOptions otherResolvedClientCodeActionLiteralOptions && EqualShape_0bc2cb70a12f4597(value, otherResolvedClientCodeActionLiteralOptions);
            case ResolvedClientCodeActionResolveOptions value:
                return b is ResolvedClientCodeActionResolveOptions otherResolvedClientCodeActionResolveOptions && EqualShape_ec1d3ab2566b74ca(value, otherResolvedClientCodeActionResolveOptions);
            case ResolvedCodeActionTagOptions value:
                return b is ResolvedCodeActionTagOptions otherResolvedCodeActionTagOptions && EqualShape_e814eab5db3b8cf5(value, otherResolvedCodeActionTagOptions);
            case ResolvedCodeActionClientCapabilities value:
                return b is ResolvedCodeActionClientCapabilities otherResolvedCodeActionClientCapabilities && EqualShape_641a1024311e36fe(value, otherResolvedCodeActionClientCapabilities);
            case ResolvedClientCodeLensResolveOptions value:
                return b is ResolvedClientCodeLensResolveOptions otherResolvedClientCodeLensResolveOptions && EqualShape_47455c659890b087(value, otherResolvedClientCodeLensResolveOptions);
            case ResolvedCodeLensClientCapabilities value:
                return b is ResolvedCodeLensClientCapabilities otherResolvedCodeLensClientCapabilities && EqualShape_0ff44a61bdf71057(value, otherResolvedCodeLensClientCapabilities);
            case ResolvedDocumentLinkClientCapabilities value:
                return b is ResolvedDocumentLinkClientCapabilities otherResolvedDocumentLinkClientCapabilities && EqualShape_09d19c1cdd74e35b(value, otherResolvedDocumentLinkClientCapabilities);
            case ResolvedDocumentColorClientCapabilities value:
                return b is ResolvedDocumentColorClientCapabilities otherResolvedDocumentColorClientCapabilities && EqualShape_3c4a740ec0e4680c(value, otherResolvedDocumentColorClientCapabilities);
            case ResolvedDocumentFormattingClientCapabilities value:
                return b is ResolvedDocumentFormattingClientCapabilities otherResolvedDocumentFormattingClientCapabilities && EqualShape_3e07826012df67db(value, otherResolvedDocumentFormattingClientCapabilities);
            case ResolvedDocumentRangeFormattingClientCapabilities value:
                return b is ResolvedDocumentRangeFormattingClientCapabilities otherResolvedDocumentRangeFormattingClientCapabilities && EqualShape_4d325fddfb853b0a(value, otherResolvedDocumentRangeFormattingClientCapabilities);
            case ResolvedDocumentOnTypeFormattingClientCapabilities value:
                return b is ResolvedDocumentOnTypeFormattingClientCapabilities otherResolvedDocumentOnTypeFormattingClientCapabilities && EqualShape_5a21ad75a1fe240f(value, otherResolvedDocumentOnTypeFormattingClientCapabilities);
            case ResolvedRenameClientCapabilities value:
                return b is ResolvedRenameClientCapabilities otherResolvedRenameClientCapabilities && EqualShape_11239c0149bed461(value, otherResolvedRenameClientCapabilities);
            case ResolvedClientFoldingRangeKindOptions value:
                return b is ResolvedClientFoldingRangeKindOptions otherResolvedClientFoldingRangeKindOptions && EqualShape_b0d5a44f2167b0a2(value, otherResolvedClientFoldingRangeKindOptions);
            case ResolvedClientFoldingRangeOptions value:
                return b is ResolvedClientFoldingRangeOptions otherResolvedClientFoldingRangeOptions && EqualShape_3c1088b29f2d583e(value, otherResolvedClientFoldingRangeOptions);
            case ResolvedFoldingRangeClientCapabilities value:
                return b is ResolvedFoldingRangeClientCapabilities otherResolvedFoldingRangeClientCapabilities && EqualShape_eb30950ba5d34e43(value, otherResolvedFoldingRangeClientCapabilities);
            case ResolvedSelectionRangeClientCapabilities value:
                return b is ResolvedSelectionRangeClientCapabilities otherResolvedSelectionRangeClientCapabilities && EqualShape_337aad13e30e29ee(value, otherResolvedSelectionRangeClientCapabilities);
            case ResolvedClientDiagnosticsTagOptions value:
                return b is ResolvedClientDiagnosticsTagOptions otherResolvedClientDiagnosticsTagOptions && EqualShape_0d2b729e1b5a42a7(value, otherResolvedClientDiagnosticsTagOptions);
            case ResolvedPublishDiagnosticsClientCapabilities value:
                return b is ResolvedPublishDiagnosticsClientCapabilities otherResolvedPublishDiagnosticsClientCapabilities && EqualShape_aa17796b111066d2(value, otherResolvedPublishDiagnosticsClientCapabilities);
            case ResolvedCallHierarchyClientCapabilities value:
                return b is ResolvedCallHierarchyClientCapabilities otherResolvedCallHierarchyClientCapabilities && EqualShape_a1431a3c77c75148(value, otherResolvedCallHierarchyClientCapabilities);
            case ResolvedClientSemanticTokensRequestOptions value:
                return b is ResolvedClientSemanticTokensRequestOptions otherResolvedClientSemanticTokensRequestOptions && EqualShape_c636f60fd26466f1(value, otherResolvedClientSemanticTokensRequestOptions);
            case ResolvedSemanticTokensClientCapabilities value:
                return b is ResolvedSemanticTokensClientCapabilities otherResolvedSemanticTokensClientCapabilities && EqualShape_139fcea77a02d93c(value, otherResolvedSemanticTokensClientCapabilities);
            case ResolvedLinkedEditingRangeClientCapabilities value:
                return b is ResolvedLinkedEditingRangeClientCapabilities otherResolvedLinkedEditingRangeClientCapabilities && EqualShape_7b1ab6225cd7e83c(value, otherResolvedLinkedEditingRangeClientCapabilities);
            case ResolvedMonikerClientCapabilities value:
                return b is ResolvedMonikerClientCapabilities otherResolvedMonikerClientCapabilities && EqualShape_6a11cf7a261f6596(value, otherResolvedMonikerClientCapabilities);
            case ResolvedTypeHierarchyClientCapabilities value:
                return b is ResolvedTypeHierarchyClientCapabilities otherResolvedTypeHierarchyClientCapabilities && EqualShape_f5214ddae878858f(value, otherResolvedTypeHierarchyClientCapabilities);
            case ResolvedInlineValueClientCapabilities value:
                return b is ResolvedInlineValueClientCapabilities otherResolvedInlineValueClientCapabilities && EqualShape_6d4e69b83ef46f6c(value, otherResolvedInlineValueClientCapabilities);
            case ResolvedClientInlayHintResolveOptions value:
                return b is ResolvedClientInlayHintResolveOptions otherResolvedClientInlayHintResolveOptions && EqualShape_adad191c3b598df5(value, otherResolvedClientInlayHintResolveOptions);
            case ResolvedInlayHintClientCapabilities value:
                return b is ResolvedInlayHintClientCapabilities otherResolvedInlayHintClientCapabilities && EqualShape_27a6a96bab603564(value, otherResolvedInlayHintClientCapabilities);
            case ResolvedDiagnosticClientCapabilities value:
                return b is ResolvedDiagnosticClientCapabilities otherResolvedDiagnosticClientCapabilities && EqualShape_03a4c5c9ebafb0c9(value, otherResolvedDiagnosticClientCapabilities);
            case ResolvedInlineCompletionClientCapabilities value:
                return b is ResolvedInlineCompletionClientCapabilities otherResolvedInlineCompletionClientCapabilities && EqualShape_e6379ab3c5a02d71(value, otherResolvedInlineCompletionClientCapabilities);
            case ResolvedTextDocumentClientCapabilities value:
                return b is ResolvedTextDocumentClientCapabilities otherResolvedTextDocumentClientCapabilities && EqualShape_cf72f937b83f57e6(value, otherResolvedTextDocumentClientCapabilities);
            case ResolvedClientShowMessageActionItemOptions value:
                return b is ResolvedClientShowMessageActionItemOptions otherResolvedClientShowMessageActionItemOptions && EqualShape_6376761663dda21c(value, otherResolvedClientShowMessageActionItemOptions);
            case ResolvedShowMessageRequestClientCapabilities value:
                return b is ResolvedShowMessageRequestClientCapabilities otherResolvedShowMessageRequestClientCapabilities && EqualShape_b7a258d345a9c8c3(value, otherResolvedShowMessageRequestClientCapabilities);
            case ResolvedShowDocumentClientCapabilities value:
                return b is ResolvedShowDocumentClientCapabilities otherResolvedShowDocumentClientCapabilities && EqualShape_0b6ae5afd248382d(value, otherResolvedShowDocumentClientCapabilities);
            case ResolvedWindowClientCapabilities value:
                return b is ResolvedWindowClientCapabilities otherResolvedWindowClientCapabilities && EqualShape_7f5a0984ac008725(value, otherResolvedWindowClientCapabilities);
            case ResolvedStaleRequestSupportOptions value:
                return b is ResolvedStaleRequestSupportOptions otherResolvedStaleRequestSupportOptions && EqualShape_ae4cf1722d9b1b8c(value, otherResolvedStaleRequestSupportOptions);
            case ResolvedRegularExpressionsClientCapabilities value:
                return b is ResolvedRegularExpressionsClientCapabilities otherResolvedRegularExpressionsClientCapabilities && EqualShape_beda1189ff75fe3c(value, otherResolvedRegularExpressionsClientCapabilities);
            case ResolvedMarkdownClientCapabilities value:
                return b is ResolvedMarkdownClientCapabilities otherResolvedMarkdownClientCapabilities && EqualShape_48c437b8486b4b13(value, otherResolvedMarkdownClientCapabilities);
            case ResolvedGeneralClientCapabilities value:
                return b is ResolvedGeneralClientCapabilities otherResolvedGeneralClientCapabilities && EqualShape_bc1f8c144e61e107(value, otherResolvedGeneralClientCapabilities);
            case ResolvedExperimentalClientCapabilities value:
                return b is ResolvedExperimentalClientCapabilities otherResolvedExperimentalClientCapabilities && EqualShape_fc211bcfd2b0a289(value, otherResolvedExperimentalClientCapabilities);
            case ResolvedClientCapabilities value:
                return b is ResolvedClientCapabilities otherResolvedClientCapabilities && EqualShape_15a6ffab147ff54f(value, otherResolvedClientCapabilities);
            default:
                throw new InvalidOperationException("unregistered original protocol equality owner");
        }
    }
}
