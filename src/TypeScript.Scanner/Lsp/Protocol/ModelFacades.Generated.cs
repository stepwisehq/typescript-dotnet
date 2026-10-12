// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated closed model joins; tools/lsp/generate-codecs.py.
#nullable enable
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal sealed partial class ImplementationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ImplementationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteImplementationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadImplementationParams(ref decoder, ref value);
    }
}

internal sealed partial class Location : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Location";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocation(ref decoder, ref value);
    }
}

internal sealed partial class ImplementationRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ImplementationRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteImplementationRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadImplementationRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class TypeDefinitionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeDefinitionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeDefinitionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeDefinitionParams(ref decoder, ref value);
    }
}

internal sealed partial class TypeDefinitionRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeDefinitionRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeDefinitionRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeDefinitionRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFolder : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFolder";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFolder(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFolder(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeWorkspaceFoldersParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeWorkspaceFoldersParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeWorkspaceFoldersParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeWorkspaceFoldersParams(ref decoder, ref value);
    }
}

internal sealed partial class ConfigurationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ConfigurationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteConfigurationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadConfigurationParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentColorParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentColorParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentColorParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentColorParams(ref decoder, ref value);
    }
}

internal sealed partial class ColorInformation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ColorInformation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteColorInformation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadColorInformation(ref decoder, ref value);
    }
}

internal sealed partial class DocumentColorRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentColorRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentColorRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentColorRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class ColorPresentationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ColorPresentationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteColorPresentationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadColorPresentationParams(ref decoder, ref value);
    }
}

internal sealed partial class ColorPresentation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ColorPresentation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteColorPresentation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadColorPresentation(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRangeParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRangeParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRangeParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRangeParams(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRange : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRange";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRange(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRange(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DeclarationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeclarationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeclarationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeclarationParams(ref decoder, ref value);
    }
}

internal sealed partial class DeclarationRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeclarationRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeclarationRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeclarationRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class SelectionRangeParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectionRangeParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectionRangeParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectionRangeParams(ref decoder, ref value);
    }
}

internal sealed partial class SelectionRange : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectionRange";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectionRange(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectionRange(ref decoder, ref value);
    }
}

internal sealed partial class SelectionRangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectionRangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectionRangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectionRangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressCreateParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressCreateParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressCreateParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressCreateParams(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressCancelParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressCancelParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressCancelParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressCancelParams(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyPrepareParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyPrepareParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyPrepareParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyPrepareParams(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyItem(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyIncomingCallsParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyIncomingCallsParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyIncomingCallsParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyIncomingCallsParams(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyIncomingCall : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyIncomingCall";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyIncomingCall(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyIncomingCall(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyOutgoingCallsParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyOutgoingCallsParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyOutgoingCallsParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyOutgoingCallsParams(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyOutgoingCall : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyOutgoingCall";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyOutgoingCall(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyOutgoingCall(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensParams(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokens : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokens";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokens(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokens(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensPartialResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensPartialResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensPartialResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensPartialResult(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensDeltaParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensDeltaParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensDeltaParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensDeltaParams(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensDelta : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensDelta";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensDelta(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensDelta(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensDeltaPartialResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensDeltaPartialResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensDeltaPartialResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensDeltaPartialResult(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensRangeParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensRangeParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensRangeParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensRangeParams(ref decoder, ref value);
    }
}

internal sealed partial class ShowDocumentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ShowDocumentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteShowDocumentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadShowDocumentParams(ref decoder, ref value);
    }
}

internal sealed partial class ShowDocumentResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ShowDocumentResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteShowDocumentResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadShowDocumentResult(ref decoder, ref value);
    }
}

internal sealed partial class LinkedEditingRangeParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LinkedEditingRangeParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLinkedEditingRangeParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLinkedEditingRangeParams(ref decoder, ref value);
    }
}

internal sealed partial class LinkedEditingRanges : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LinkedEditingRanges";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLinkedEditingRanges(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLinkedEditingRanges(ref decoder, ref value);
    }
}

internal sealed partial class LinkedEditingRangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LinkedEditingRangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLinkedEditingRangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLinkedEditingRangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class CreateFilesParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CreateFilesParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCreateFilesParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCreateFilesParams(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceEdit(ref decoder, ref value);
    }
}

internal sealed partial class FileOperationRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileOperationRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileOperationRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileOperationRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class RenameFilesParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameFilesParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameFilesParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameFilesParams(ref decoder, ref value);
    }
}

internal sealed partial class DeleteFilesParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeleteFilesParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeleteFilesParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeleteFilesParams(ref decoder, ref value);
    }
}

internal sealed partial class MonikerParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MonikerParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMonikerParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMonikerParams(ref decoder, ref value);
    }
}

internal sealed partial class Moniker : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Moniker";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMoniker(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMoniker(ref decoder, ref value);
    }
}

internal sealed partial class MonikerRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MonikerRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMonikerRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMonikerRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchyPrepareParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyPrepareParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyPrepareParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchyPrepareParams(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchyItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchyItem(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchyRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchyRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchySupertypesParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchySupertypesParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchySupertypesParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchySupertypesParams(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchySubtypesParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchySubtypesParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchySubtypesParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchySubtypesParams(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueParams(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintParams(ref decoder, ref value);
    }
}

internal sealed partial class InlayHint : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHint";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHint(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHint(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentDiagnosticParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentDiagnosticParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentDiagnosticParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentDiagnosticParams(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticServerCancellationData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticServerCancellationData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticServerCancellationData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticServerCancellationData(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceDiagnosticParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceDiagnosticParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceDiagnosticParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceDiagnosticParams(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceDiagnosticReportPartialResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceDiagnosticReportPartialResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceDiagnosticReportPartialResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceDiagnosticReportPartialResult(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionParams(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionList : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionList";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionList(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionList(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionItem(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentParams(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentResult(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentRefreshParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentRefreshParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentRefreshParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentRefreshParams(ref decoder, ref value);
    }
}

internal sealed partial class RegistrationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RegistrationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRegistrationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRegistrationParams(ref decoder, ref value);
    }
}

internal sealed partial class UnregistrationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.UnregistrationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteUnregistrationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadUnregistrationParams(ref decoder, ref value);
    }
}

internal sealed partial class InitializeParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializeParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializeParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializeParams(ref decoder, ref value);
    }
}

internal sealed partial class InitializeResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializeResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializeResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializeResult(ref decoder, ref value);
    }
}

internal sealed partial class InitializeError : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializeError";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializeError(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializeError(ref decoder, ref value);
    }
}

internal partial struct InitializedParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializedParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializedParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadInitializedParams(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal sealed partial class DidChangeConfigurationParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeConfigurationParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeConfigurationParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeConfigurationParams(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeConfigurationRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeConfigurationRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeConfigurationRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeConfigurationRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class ShowMessageParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ShowMessageParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteShowMessageParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadShowMessageParams(ref decoder, ref value);
    }
}

internal sealed partial class ShowMessageRequestParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ShowMessageRequestParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteShowMessageRequestParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadShowMessageRequestParams(ref decoder, ref value);
    }
}

internal sealed partial class MessageActionItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MessageActionItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMessageActionItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMessageActionItem(ref decoder, ref value);
    }
}

internal sealed partial class LogMessageParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LogMessageParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLogMessageParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLogMessageParams(ref decoder, ref value);
    }
}

internal sealed partial class DidOpenTextDocumentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidOpenTextDocumentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidOpenTextDocumentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidOpenTextDocumentParams(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeTextDocumentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeTextDocumentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeTextDocumentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeTextDocumentParams(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentChangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentChangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentChangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentChangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DidCloseTextDocumentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidCloseTextDocumentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidCloseTextDocumentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidCloseTextDocumentParams(ref decoder, ref value);
    }
}

internal sealed partial class DidSaveTextDocumentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidSaveTextDocumentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidSaveTextDocumentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidSaveTextDocumentParams(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentSaveRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentSaveRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentSaveRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentSaveRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class WillSaveTextDocumentParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WillSaveTextDocumentParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWillSaveTextDocumentParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWillSaveTextDocumentParams(ref decoder, ref value);
    }
}

internal sealed partial class TextEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextEdit(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeWatchedFilesParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeWatchedFilesParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeWatchedFilesParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeWatchedFilesParams(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeWatchedFilesRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeWatchedFilesRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeWatchedFilesRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeWatchedFilesRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class PublishDiagnosticsParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PublishDiagnosticsParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePublishDiagnosticsParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPublishDiagnosticsParams(ref decoder, ref value);
    }
}

internal sealed partial class CompletionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionParams(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItem(ref decoder, ref value);
    }
}

internal sealed partial class CompletionList : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionList";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionList(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionList(ref decoder, ref value);
    }
}

internal sealed partial class CompletionRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class HoverParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.HoverParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteHoverParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadHoverParams(ref decoder, ref value);
    }
}

internal sealed partial class Hover : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Hover";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteHover(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadHover(ref decoder, ref value);
    }
}

internal sealed partial class HoverRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.HoverRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteHoverRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadHoverRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelpParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelpParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelpParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelpParams(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelp : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelp";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelp(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelp(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelpRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelpRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelpRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelpRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DefinitionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DefinitionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDefinitionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDefinitionParams(ref decoder, ref value);
    }
}

internal sealed partial class DefinitionRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DefinitionRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDefinitionRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDefinitionRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class ReferenceParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ReferenceParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteReferenceParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadReferenceParams(ref decoder, ref value);
    }
}

internal sealed partial class ReferenceRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ReferenceRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteReferenceRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadReferenceRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentHighlightParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentHighlightParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentHighlightParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentHighlightParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentHighlight : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentHighlight";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentHighlight(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentHighlight(ref decoder, ref value);
    }
}

internal sealed partial class DocumentHighlightRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentHighlightRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentHighlightRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentHighlightRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentSymbolParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentSymbolParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentSymbolParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentSymbolParams(ref decoder, ref value);
    }
}

internal sealed partial class SymbolInformation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SymbolInformation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSymbolInformation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSymbolInformation(ref decoder, ref value);
    }
}

internal sealed partial class DocumentSymbol : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentSymbol";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentSymbol(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentSymbol(ref decoder, ref value);
    }
}

internal sealed partial class DocumentSymbolRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentSymbolRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentSymbolRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentSymbolRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionParams(ref decoder, ref value);
    }
}

internal sealed partial class Command : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Command";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCommand(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCommand(ref decoder, ref value);
    }
}

internal sealed partial class CodeAction : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeAction";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeAction(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeAction(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceSymbolParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceSymbolParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceSymbolParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceSymbolParams(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceSymbol : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceSymbol";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceSymbol(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceSymbol(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceSymbolRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceSymbolRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceSymbolRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceSymbolRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensParams(ref decoder, ref value);
    }
}

internal sealed partial class CodeLens : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLens";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLens(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLens(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentLinkParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLinkParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLinkParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentLinkParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentLink : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLink";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLink(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentLink(ref decoder, ref value);
    }
}

internal sealed partial class DocumentLinkRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLinkRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLinkRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentLinkRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentFormattingParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentFormattingParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentFormattingParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentFormattingParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentFormattingRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentFormattingRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentFormattingRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentFormattingRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentRangeFormattingParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentRangeFormattingParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentRangeFormattingParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentRangeFormattingParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentRangeFormattingRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentRangeFormattingRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentRangeFormattingRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentRangeFormattingRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentRangesFormattingParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentRangesFormattingParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentRangesFormattingParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentRangesFormattingParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentOnTypeFormattingParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentOnTypeFormattingParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentOnTypeFormattingParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentOnTypeFormattingParams(ref decoder, ref value);
    }
}

internal sealed partial class DocumentOnTypeFormattingRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentOnTypeFormattingRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentOnTypeFormattingRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentOnTypeFormattingRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class RenameParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameParams(ref decoder, ref value);
    }
}

internal sealed partial class RenameRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class PrepareRenameParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PrepareRenameParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePrepareRenameParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPrepareRenameParams(ref decoder, ref value);
    }
}

internal sealed partial class ExecuteCommandParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ExecuteCommandParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteExecuteCommandParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadExecuteCommandParams(ref decoder, ref value);
    }
}

internal sealed partial class ExecuteCommandRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ExecuteCommandRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteExecuteCommandRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadExecuteCommandRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class ApplyWorkspaceEditParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ApplyWorkspaceEditParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteApplyWorkspaceEditParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadApplyWorkspaceEditParams(ref decoder, ref value);
    }
}

internal sealed partial class ApplyWorkspaceEditResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ApplyWorkspaceEditResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteApplyWorkspaceEditResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadApplyWorkspaceEditResult(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressBegin : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressBegin";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressBegin(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressBegin(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressReport(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressEnd : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressEnd";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressEnd(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressEnd(ref decoder, ref value);
    }
}

internal sealed partial class SetTraceParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SetTraceParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSetTraceParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSetTraceParams(ref decoder, ref value);
    }
}

internal sealed partial class LogTraceParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LogTraceParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLogTraceParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLogTraceParams(ref decoder, ref value);
    }
}

internal sealed partial class CancelParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CancelParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCancelParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCancelParams(ref decoder, ref value);
    }
}

internal sealed partial class ProgressParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProgressParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProgressParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProgressParams(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentPositionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentPositionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentPositionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentPositionParams(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressParams(ref decoder, ref value);
    }
}

internal sealed partial class PartialResultParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PartialResultParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePartialResultParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPartialResultParams(ref decoder, ref value);
    }
}

internal sealed partial class LocationLink : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LocationLink";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocationLink(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocationLink(ref decoder, ref value);
    }
}

internal partial struct Range : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Range";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRange(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadRange(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal sealed partial class ImplementationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ImplementationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteImplementationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadImplementationOptions(ref decoder, ref value);
    }
}

internal sealed partial class StaticRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StaticRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStaticRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStaticRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class TypeDefinitionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeDefinitionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeDefinitionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeDefinitionOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFoldersChangeEvent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFoldersChangeEvent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFoldersChangeEvent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFoldersChangeEvent(ref decoder, ref value);
    }
}

internal sealed partial class ConfigurationItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ConfigurationItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteConfigurationItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadConfigurationItem(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentIdentifier : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentIdentifier";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentIdentifier(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentIdentifier(ref decoder, ref value);
    }
}

internal sealed partial class Color : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Color";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteColor(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadColor(ref decoder, ref value);
    }
}

internal sealed partial class DocumentColorOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentColorOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentColorOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentColorOptions(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRangeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRangeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRangeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRangeOptions(ref decoder, ref value);
    }
}

internal sealed partial class DeclarationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeclarationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeclarationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeclarationOptions(ref decoder, ref value);
    }
}

internal partial struct Position : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Position";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePosition(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadPosition(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal sealed partial class SelectionRangeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectionRangeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectionRangeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectionRangeOptions(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyOptions(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensOptions(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensEdit(ref decoder, ref value);
    }
}

internal sealed partial class LinkedEditingRangeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LinkedEditingRangeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLinkedEditingRangeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLinkedEditingRangeOptions(ref decoder, ref value);
    }
}

internal sealed partial class FileCreate : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileCreate";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileCreate(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileCreate(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentEdit(ref decoder, ref value);
    }
}

internal sealed partial class CreateFile : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CreateFile";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCreateFile(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCreateFile(ref decoder, ref value);
    }
}

internal sealed partial class RenameFile : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameFile";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameFile(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameFile(ref decoder, ref value);
    }
}

internal sealed partial class DeleteFile : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeleteFile";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeleteFile(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeleteFile(ref decoder, ref value);
    }
}

internal sealed partial class ChangeAnnotation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ChangeAnnotation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteChangeAnnotation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadChangeAnnotation(ref decoder, ref value);
    }
}

internal sealed partial class FileOperationFilter : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileOperationFilter";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileOperationFilter(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileOperationFilter(ref decoder, ref value);
    }
}

internal sealed partial class FileRename : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileRename";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileRename(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileRename(ref decoder, ref value);
    }
}

internal sealed partial class FileDelete : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileDelete";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileDelete(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileDelete(ref decoder, ref value);
    }
}

internal sealed partial class MonikerOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MonikerOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMonikerOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMonikerOptions(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchyOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchyOptions(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueContext : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueContext";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueContext(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueContext(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueText : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueText";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueText(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueText(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueVariableLookup : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueVariableLookup";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueVariableLookup(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueVariableLookup(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueEvaluatableExpression : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueEvaluatableExpression";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueEvaluatableExpression(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueEvaluatableExpression(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueOptions(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintLabelPart : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintLabelPart";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintLabelPart(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintLabelPart(ref decoder, ref value);
    }
}

internal sealed partial class MarkupContent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MarkupContent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMarkupContent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMarkupContent(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintOptions(ref decoder, ref value);
    }
}

internal sealed partial class RelatedFullDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RelatedFullDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRelatedFullDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRelatedFullDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class RelatedUnchangedDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RelatedUnchangedDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRelatedUnchangedDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRelatedUnchangedDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class DocumentDiagnosticReportPartialResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentDiagnosticReportPartialResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentDiagnosticReportPartialResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentDiagnosticReportPartialResult(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticOptions(ref decoder, ref value);
    }
}

internal sealed partial class PreviousResultId : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PreviousResultId";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePreviousResultId(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPreviousResultId(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentItem(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionContext : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionContext";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionContext(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionContext(ref decoder, ref value);
    }
}

internal sealed partial class StringValue : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringValue";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringValue(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringValue(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionOptions(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentOptions(ref decoder, ref value);
    }
}

internal sealed partial class Registration : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Registration";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRegistration(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRegistration(ref decoder, ref value);
    }
}

internal sealed partial class RegisterOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RegisterOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRegisterOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRegisterOptions(ref decoder, ref value);
    }
}

internal sealed partial class Unregistration : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Unregistration";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteUnregistration(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadUnregistration(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFoldersInitializeParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFoldersInitializeParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFoldersInitializeParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFoldersInitializeParams(ref decoder, ref value);
    }
}

internal sealed partial class ServerCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ServerCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteServerCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadServerCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ServerInfo : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ServerInfo";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteServerInfo(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadServerInfo(ref decoder, ref value);
    }
}

internal sealed partial class VersionedTextDocumentIdentifier : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VersionedTextDocumentIdentifier";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVersionedTextDocumentIdentifier(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVersionedTextDocumentIdentifier(ref decoder, ref value);
    }
}

internal sealed partial class SaveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SaveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSaveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSaveOptions(ref decoder, ref value);
    }
}

internal sealed partial class FileEvent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileEvent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileEvent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileEvent(ref decoder, ref value);
    }
}

internal sealed partial class FileSystemWatcher : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileSystemWatcher";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileSystemWatcher(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileSystemWatcher(ref decoder, ref value);
    }
}

internal sealed partial class Diagnostic : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.Diagnostic";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnostic(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnostic(ref decoder, ref value);
    }
}

internal sealed partial class CompletionContext : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionContext";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionContext(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionContext(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItemLabelDetails : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemLabelDetails";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemLabelDetails(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItemLabelDetails(ref decoder, ref value);
    }
}

internal sealed partial class InsertReplaceEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InsertReplaceEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInsertReplaceEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInsertReplaceEdit(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItemDefaults : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemDefaults";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemDefaults(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItemDefaults(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItemApplyKinds : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemApplyKinds";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemApplyKinds(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItemApplyKinds(ref decoder, ref value);
    }
}

internal sealed partial class CompletionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionOptions(ref decoder, ref value);
    }
}

internal sealed partial class HoverOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.HoverOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteHoverOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadHoverOptions(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelpContext : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelpContext";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelpContext(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelpContext(ref decoder, ref value);
    }
}

internal sealed partial class SignatureInformation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureInformation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureInformation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureInformation(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelpOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelpOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelpOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelpOptions(ref decoder, ref value);
    }
}

internal sealed partial class DefinitionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DefinitionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDefinitionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDefinitionOptions(ref decoder, ref value);
    }
}

internal sealed partial class ReferenceContext : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ReferenceContext";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteReferenceContext(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadReferenceContext(ref decoder, ref value);
    }
}

internal sealed partial class ReferenceOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ReferenceOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteReferenceOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadReferenceOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentHighlightOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentHighlightOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentHighlightOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentHighlightOptions(ref decoder, ref value);
    }
}

internal sealed partial class BaseSymbolInformation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BaseSymbolInformation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBaseSymbolInformation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBaseSymbolInformation(ref decoder, ref value);
    }
}

internal sealed partial class DocumentSymbolOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentSymbolOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentSymbolOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentSymbolOptions(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionContext : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionContext";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionContext(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionContext(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionDisabled : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionDisabled";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionDisabled(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionDisabled(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionOptions(ref decoder, ref value);
    }
}

internal sealed partial class LocationUriOnly : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LocationUriOnly";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocationUriOnly(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocationUriOnly(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceSymbolOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceSymbolOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceSymbolOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceSymbolOptions(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentLinkOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLinkOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLinkOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentLinkOptions(ref decoder, ref value);
    }
}

internal sealed partial class FormattingOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FormattingOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFormattingOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFormattingOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentFormattingOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentFormattingOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentFormattingOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentFormattingOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentRangeFormattingOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentRangeFormattingOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentRangeFormattingOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentRangeFormattingOptions(ref decoder, ref value);
    }
}

internal sealed partial class DocumentOnTypeFormattingOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentOnTypeFormattingOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentOnTypeFormattingOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentOnTypeFormattingOptions(ref decoder, ref value);
    }
}

internal sealed partial class RenameOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameOptions(ref decoder, ref value);
    }
}

internal sealed partial class PrepareRenamePlaceholder : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PrepareRenamePlaceholder";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePrepareRenamePlaceholder(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPrepareRenamePlaceholder(ref decoder, ref value);
    }
}

internal sealed partial class PrepareRenameDefaultBehavior : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PrepareRenameDefaultBehavior";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePrepareRenameDefaultBehavior(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPrepareRenameDefaultBehavior(ref decoder, ref value);
    }
}

internal sealed partial class ExecuteCommandOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ExecuteCommandOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteExecuteCommandOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadExecuteCommandOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceEditMetadata : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceEditMetadata";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceEditMetadata(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceEditMetadata(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressOptions(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensLegend : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensLegend";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensLegend(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensLegend(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensFullDelta : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensFullDelta";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensFullDelta(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensFullDelta(ref decoder, ref value);
    }
}

internal sealed partial class OptionalVersionedTextDocumentIdentifier : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.OptionalVersionedTextDocumentIdentifier";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteOptionalVersionedTextDocumentIdentifier(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadOptionalVersionedTextDocumentIdentifier(ref decoder, ref value);
    }
}

internal sealed partial class AnnotatedTextEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.AnnotatedTextEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteAnnotatedTextEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadAnnotatedTextEdit(ref decoder, ref value);
    }
}

internal sealed partial class SnippetTextEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SnippetTextEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSnippetTextEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSnippetTextEdit(ref decoder, ref value);
    }
}

internal sealed partial class ResourceOperation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResourceOperation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResourceOperation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadResourceOperation(ref decoder, ref value);
    }
}

internal sealed partial class CreateFileOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CreateFileOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCreateFileOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCreateFileOptions(ref decoder, ref value);
    }
}

internal sealed partial class RenameFileOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameFileOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameFileOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameFileOptions(ref decoder, ref value);
    }
}

internal sealed partial class DeleteFileOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeleteFileOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeleteFileOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeleteFileOptions(ref decoder, ref value);
    }
}

internal sealed partial class FileOperationPattern : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileOperationPattern";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileOperationPattern(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileOperationPattern(ref decoder, ref value);
    }
}

internal sealed partial class FullDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FullDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFullDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFullDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class UnchangedDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.UnchangedDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteUnchangedDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadUnchangedDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFullDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFullDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFullDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFullDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceUnchangedDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceUnchangedDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceUnchangedDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceUnchangedDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class SelectedCompletionInfo : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectedCompletionInfo";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectedCompletionInfo(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectedCompletionInfo(ref decoder, ref value);
    }
}

internal sealed partial class ClientInfo : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientInfo";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientInfo(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientInfo(ref decoder, ref value);
    }
}

internal sealed partial class ClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentSyncOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentSyncOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentSyncOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentSyncOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceOptions(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentChangePartial : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentChangePartial";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentChangePartial(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentChangePartial(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentChangeWholeDocument : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentChangeWholeDocument";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentChangeWholeDocument(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentChangeWholeDocument(ref decoder, ref value);
    }
}

internal sealed partial class CodeDescription : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeDescription";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeDescription(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeDescription(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticRelatedInformation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticRelatedInformation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticRelatedInformation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticRelatedInformation(ref decoder, ref value);
    }
}

internal sealed partial class EditRangeWithInsertReplace : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.EditRangeWithInsertReplace";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteEditRangeWithInsertReplace(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadEditRangeWithInsertReplace(ref decoder, ref value);
    }
}

internal sealed partial class ServerCompletionItemOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ServerCompletionItemOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteServerCompletionItemOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadServerCompletionItemOptions(ref decoder, ref value);
    }
}

internal sealed partial class MarkedStringWithLanguage : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MarkedStringWithLanguage";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMarkedStringWithLanguage(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMarkedStringWithLanguage(ref decoder, ref value);
    }
}

internal sealed partial class ParameterInformation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ParameterInformation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteParameterInformation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadParameterInformation(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionKindDocumentation : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionKindDocumentation";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionKindDocumentation(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionKindDocumentation(ref decoder, ref value);
    }
}

internal sealed partial class FileOperationPatternOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileOperationPatternOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileOperationPatternOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileOperationPatternOptions(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class WindowClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WindowClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWindowClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWindowClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class GeneralClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.GeneralClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteGeneralClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadGeneralClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFoldersServerCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFoldersServerCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFoldersServerCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFoldersServerCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class FileOperationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileOperationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileOperationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileOperationOptions(ref decoder, ref value);
    }
}

internal sealed partial class RelativePattern : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RelativePattern";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRelativePattern(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRelativePattern(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceEditClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceEditClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceEditClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceEditClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeConfigurationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeConfigurationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeConfigurationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeConfigurationClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DidChangeWatchedFilesClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DidChangeWatchedFilesClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDidChangeWatchedFilesClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDidChangeWatchedFilesClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceSymbolClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceSymbolClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceSymbolClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceSymbolClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ExecuteCommandClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ExecuteCommandClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteExecuteCommandClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadExecuteCommandClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class FileOperationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FileOperationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFileOperationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFileOperationClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRangeWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRangeWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRangeWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRangeWorkspaceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentSyncClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentSyncClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentSyncClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentSyncClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentFilterClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentFilterClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentFilterClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentFilterClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class CompletionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class HoverClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.HoverClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteHoverClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadHoverClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelpClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelpClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelpClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelpClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DeclarationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DeclarationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDeclarationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDeclarationClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DefinitionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DefinitionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDefinitionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDefinitionClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TypeDefinitionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeDefinitionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeDefinitionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeDefinitionClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ImplementationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ImplementationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteImplementationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadImplementationClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ReferenceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ReferenceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteReferenceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadReferenceClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentHighlightClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentHighlightClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentHighlightClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentHighlightClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentSymbolClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentSymbolClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentSymbolClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentSymbolClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentLinkClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLinkClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLinkClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentLinkClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentColorClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentColorClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentColorClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentColorClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentFormattingClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentFormattingClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentFormattingClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentFormattingClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentRangeFormattingClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentRangeFormattingClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentRangeFormattingClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentRangeFormattingClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DocumentOnTypeFormattingClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentOnTypeFormattingClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentOnTypeFormattingClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentOnTypeFormattingClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class RenameClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RenameClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRenameClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRenameClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRangeClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRangeClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRangeClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRangeClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class SelectionRangeClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectionRangeClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectionRangeClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectionRangeClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class PublishDiagnosticsClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PublishDiagnosticsClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePublishDiagnosticsClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPublishDiagnosticsClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class LinkedEditingRangeClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LinkedEditingRangeClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLinkedEditingRangeClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLinkedEditingRangeClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class MonikerClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MonikerClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMonikerClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMonikerClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchyClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchyClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ShowMessageRequestClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ShowMessageRequestClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteShowMessageRequestClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadShowMessageRequestClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ShowDocumentClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ShowDocumentClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteShowDocumentClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadShowDocumentClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class StaleRequestSupportOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StaleRequestSupportOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStaleRequestSupportOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStaleRequestSupportOptions(ref decoder, ref value);
    }
}

internal sealed partial class RegularExpressionsClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RegularExpressionsClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRegularExpressionsClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRegularExpressionsClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class MarkdownClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MarkdownClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMarkdownClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMarkdownClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentFilterLanguage : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentFilterLanguage";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentFilterLanguage(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentFilterLanguage(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentFilterScheme : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentFilterScheme";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentFilterScheme(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentFilterScheme(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentFilterPattern : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentFilterPattern";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentFilterPattern(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentFilterPattern(ref decoder, ref value);
    }
}

internal sealed partial class ChangeAnnotationsSupportOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ChangeAnnotationsSupportOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteChangeAnnotationsSupportOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadChangeAnnotationsSupportOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientSymbolKindOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSymbolKindOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSymbolKindOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSymbolKindOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientSymbolTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSymbolTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSymbolTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSymbolTagOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientSymbolResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSymbolResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSymbolResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSymbolResolveOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCompletionItemOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCompletionItemOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCompletionItemOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCompletionItemOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCompletionItemOptionsKind : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCompletionItemOptionsKind";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCompletionItemOptionsKind(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCompletionItemOptionsKind(ref decoder, ref value);
    }
}

internal sealed partial class CompletionListCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionListCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionListCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionListCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ClientSignatureInformationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSignatureInformationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSignatureInformationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSignatureInformationOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCodeActionLiteralOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCodeActionLiteralOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCodeActionLiteralOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCodeActionLiteralOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCodeActionResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCodeActionResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCodeActionResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCodeActionResolveOptions(ref decoder, ref value);
    }
}

internal sealed partial class CodeActionTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeActionTagOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCodeLensResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCodeLensResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCodeLensResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCodeLensResolveOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientFoldingRangeKindOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientFoldingRangeKindOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientFoldingRangeKindOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientFoldingRangeKindOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientFoldingRangeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientFoldingRangeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientFoldingRangeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientFoldingRangeOptions(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticsCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticsCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticsCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticsCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ClientSemanticTokensRequestOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSemanticTokensRequestOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSemanticTokensRequestOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSemanticTokensRequestOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientInlayHintResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientInlayHintResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientInlayHintResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientInlayHintResolveOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientShowMessageActionItemOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientShowMessageActionItemOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientShowMessageActionItemOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientShowMessageActionItemOptions(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItemTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItemTagOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCompletionItemResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCompletionItemResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCompletionItemResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCompletionItemResolveOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCompletionItemInsertTextModeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCompletionItemInsertTextModeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCompletionItemInsertTextModeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCompletionItemInsertTextModeOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientSignatureParameterInformationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSignatureParameterInformationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSignatureParameterInformationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSignatureParameterInformationOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientCodeActionKindOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientCodeActionKindOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientCodeActionKindOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientCodeActionKindOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientDiagnosticsTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientDiagnosticsTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientDiagnosticsTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientDiagnosticsTagOptions(ref decoder, ref value);
    }
}

internal sealed partial class ClientSemanticTokensRequestFullDelta : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ClientSemanticTokensRequestFullDelta";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteClientSemanticTokensRequestFullDelta(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadClientSemanticTokensRequestFullDelta(ref decoder, ref value);
    }
}

internal sealed partial class InitializationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializationOptions(ref decoder, ref value);
    }
}

internal sealed partial class AutoImportFix : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.AutoImportFix";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteAutoImportFix(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadAutoImportFix(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItemData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItemData(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensData(ref decoder, ref value);
    }
}

internal sealed partial class ExperimentalServerCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ExperimentalServerCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteExperimentalServerCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadExperimentalServerCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class ExperimentalClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ExperimentalClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteExperimentalClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadExperimentalClientCapabilities(ref decoder, ref value);
    }
}

internal sealed partial class VSOnAutoInsertOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSOnAutoInsertOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSOnAutoInsertOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSOnAutoInsertOptions(ref decoder, ref value);
    }
}

internal sealed partial class VSReferenceItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSReferenceItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSReferenceItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSReferenceItem(ref decoder, ref value);
    }
}

internal sealed partial class VSOnAutoInsertParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSOnAutoInsertParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSOnAutoInsertParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSOnAutoInsertParams(ref decoder, ref value);
    }
}

internal sealed partial class VSOnAutoInsertResponseItem : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSOnAutoInsertResponseItem";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSOnAutoInsertResponseItem(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSOnAutoInsertResponseItem(ref decoder, ref value);
    }
}

internal sealed partial class RequestFailureTelemetryEvent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RequestFailureTelemetryEvent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRequestFailureTelemetryEvent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRequestFailureTelemetryEvent(ref decoder, ref value);
    }
}

internal sealed partial class RequestFailureTelemetryProperties : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RequestFailureTelemetryProperties";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRequestFailureTelemetryProperties(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRequestFailureTelemetryProperties(ref decoder, ref value);
    }
}

internal sealed partial class ProfileParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProfileParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProfileParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProfileParams(ref decoder, ref value);
    }
}

internal sealed partial class ProfileResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProfileResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProfileResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProfileResult(ref decoder, ref value);
    }
}

internal sealed partial class InitializeAPISessionParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializeAPISessionParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializeAPISessionParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializeAPISessionParams(ref decoder, ref value);
    }
}

internal sealed partial class InitializeAPISessionResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializeAPISessionResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializeAPISessionResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializeAPISessionResult(ref decoder, ref value);
    }
}

internal sealed partial class ProjectInfoParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProjectInfoParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProjectInfoParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProjectInfoParams(ref decoder, ref value);
    }
}

internal sealed partial class ProjectInfoResult : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProjectInfoResult";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProjectInfoResult(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProjectInfoResult(ref decoder, ref value);
    }
}

internal sealed partial class ContentMapperManifest : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ContentMapperManifest";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteContentMapperManifest(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadContentMapperManifest(ref decoder, ref value);
    }
}

internal sealed partial class InferredProjectContentMapperContribution : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InferredProjectContentMapperContribution";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInferredProjectContentMapperContribution(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInferredProjectContentMapperContribution(ref decoder, ref value);
    }
}

internal sealed partial class ContentMapperContribution : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ContentMapperContribution";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteContentMapperContribution(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadContentMapperContribution(ref decoder, ref value);
    }
}

internal sealed partial class SetContentMapperContributionsParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SetContentMapperContributionsParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSetContentMapperContributionsParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSetContentMapperContributionsParams(ref decoder, ref value);
    }
}

internal sealed partial class SetLogVerbosityParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SetLogVerbosityParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSetLogVerbosityParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSetLogVerbosityParams(ref decoder, ref value);
    }
}

internal sealed partial class PerformanceStatsTelemetryEvent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PerformanceStatsTelemetryEvent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePerformanceStatsTelemetryEvent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPerformanceStatsTelemetryEvent(ref decoder, ref value);
    }
}

internal sealed partial class PerformanceStatsTelemetryMeasurements : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PerformanceStatsTelemetryMeasurements";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePerformanceStatsTelemetryMeasurements(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPerformanceStatsTelemetryMeasurements(ref decoder, ref value);
    }
}

internal sealed partial class ProjectInfoTelemetryEvent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProjectInfoTelemetryEvent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProjectInfoTelemetryEvent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProjectInfoTelemetryEvent(ref decoder, ref value);
    }
}

internal sealed partial class ProjectInfoTelemetryMeasurements : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ProjectInfoTelemetryMeasurements";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteProjectInfoTelemetryMeasurements(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadProjectInfoTelemetryMeasurements(ref decoder, ref value);
    }
}

internal sealed partial class MultiDocumentHighlight : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MultiDocumentHighlight";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMultiDocumentHighlight(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMultiDocumentHighlight(ref decoder, ref value);
    }
}

internal sealed partial class MultiDocumentHighlightParams : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MultiDocumentHighlightParams";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMultiDocumentHighlightParams(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMultiDocumentHighlightParams(ref decoder, ref value);
    }
}

internal sealed partial class VSClassifiedTextRun : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSClassifiedTextRun";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSClassifiedTextRun(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSClassifiedTextRun(ref decoder, ref value);
    }
}

internal sealed partial class VSClassifiedTextElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSClassifiedTextElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSClassifiedTextElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSClassifiedTextElement(ref decoder, ref value);
    }
}

internal sealed partial class VSImageId : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSImageId";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSImageId(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSImageId(ref decoder, ref value);
    }
}

internal sealed partial class VSImageElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSImageElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSImageElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSImageElement(ref decoder, ref value);
    }
}

internal sealed partial class VSContainerElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSContainerElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSContainerElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSContainerElement(ref decoder, ref value);
    }
}

internal partial struct CallHierarchyItemData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyItemData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyItemData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadCallHierarchyItemData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct TypeHierarchyItemData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyItemData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyItemData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadTypeHierarchyItemData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct InlayHintData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadInlayHintData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct CodeActionData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeActionData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeActionData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadCodeActionData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct WorkspaceSymbolData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceSymbolData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceSymbolData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadWorkspaceSymbolData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct DocumentLinkData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLinkData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLinkData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadDocumentLinkData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct DiagnosticData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadDiagnosticData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct CompletionItemDefaultsData : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemDefaultsData";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemDefaultsData(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadCompletionItemDefaultsData(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal sealed partial class IntegerOrString : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.IntegerOrString";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteIntegerOrString(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadIntegerOrString(ref decoder, ref value);
    }
}

internal sealed partial class DocumentSelectorOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentSelectorOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentSelectorOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentSelectorOrNull(ref decoder, ref value);
    }
}

internal partial struct BooleanOrEmptyObject : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrEmptyObject";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrEmptyObject(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadBooleanOrEmptyObject(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal sealed partial class BooleanOrSemanticTokensFullDelta : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrSemanticTokensFullDelta";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrSemanticTokensFullDelta(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrSemanticTokensFullDelta(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentEditOrCreateFileOrRenameFileOrDeleteFile(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentEditOrCreateFileOrRenameFileOrDeleteFile(ref decoder, ref value);
    }
}

internal sealed partial class StringOrInlayHintLabelParts : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrInlayHintLabelParts";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrInlayHintLabelParts(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrInlayHintLabelParts(ref decoder, ref value);
    }
}

internal sealed partial class StringOrMarkupContent : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrMarkupContent";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrMarkupContent(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrMarkupContent(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class StringOrStringValue : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrStringValue";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrStringValue(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrStringValue(ref decoder, ref value);
    }
}

internal sealed partial class IntegerOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.IntegerOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteIntegerOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadIntegerOrNull(ref decoder, ref value);
    }
}

internal sealed partial class StringOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrNull(ref decoder, ref value);
    }
}

internal sealed partial class DocumentUriOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentUriOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentUriOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentUriOrNull(ref decoder, ref value);
    }
}

internal sealed partial class InitializationOptionsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InitializationOptionsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInitializationOptionsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInitializationOptionsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFoldersOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFoldersOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFoldersOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFoldersOrNull(ref decoder, ref value);
    }
}

internal sealed partial class StringOrStrings : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrStrings";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrStrings(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrStrings(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentChangePartialOrWholeDocument : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentChangePartialOrWholeDocument";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentChangePartialOrWholeDocument(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentChangePartialOrWholeDocument(ref decoder, ref value);
    }
}

internal sealed partial class TextEditOrInsertReplaceEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextEditOrInsertReplaceEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextEditOrInsertReplaceEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextEditOrInsertReplaceEdit(ref decoder, ref value);
    }
}

internal sealed partial class MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMarkupContentOrStringOrMarkedStringWithLanguageOrMarkedStrings(ref decoder, ref value);
    }
}

internal sealed partial class UintegerOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.UintegerOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteUintegerOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadUintegerOrNull(ref decoder, ref value);
    }
}

internal sealed partial class LocationOrLocationUriOnly : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LocationOrLocationUriOnly";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocationOrLocationUriOnly(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocationOrLocationUriOnly(ref decoder, ref value);
    }
}

internal sealed partial class WorkDoneProgressBeginOrReportOrEnd : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkDoneProgressBeginOrReportOrEnd";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkDoneProgressBeginOrReportOrEnd(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkDoneProgressBeginOrReportOrEnd(ref decoder, ref value);
    }
}

internal sealed partial class TextEditOrAnnotatedTextEditOrSnippetTextEdit : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextEditOrAnnotatedTextEditOrSnippetTextEdit";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextEditOrAnnotatedTextEditOrSnippetTextEdit(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextEditOrAnnotatedTextEditOrSnippetTextEdit(ref decoder, ref value);
    }
}

internal sealed partial class FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentSyncOptionsOrKind : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentSyncOptionsOrKind";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentSyncOptionsOrKind(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentSyncOptionsOrKind(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrHoverOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrHoverOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrHoverOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrHoverOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDeclarationOptionsOrDeclarationRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDeclarationOptionsOrDeclarationRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDeclarationOptionsOrDeclarationRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDefinitionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDefinitionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDefinitionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDefinitionOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrTypeDefinitionOptionsOrTypeDefinitionRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrImplementationOptionsOrImplementationRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrImplementationOptionsOrImplementationRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrImplementationOptionsOrImplementationRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrImplementationOptionsOrImplementationRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrReferenceOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrReferenceOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrReferenceOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrReferenceOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDocumentHighlightOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDocumentHighlightOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDocumentHighlightOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDocumentHighlightOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDocumentSymbolOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDocumentSymbolOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDocumentSymbolOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDocumentSymbolOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrCodeActionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrCodeActionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrCodeActionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrCodeActionOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDocumentColorOptionsOrDocumentColorRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrWorkspaceSymbolOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrWorkspaceSymbolOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrWorkspaceSymbolOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrWorkspaceSymbolOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDocumentFormattingOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDocumentFormattingOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDocumentFormattingOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDocumentFormattingOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrDocumentRangeFormattingOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrDocumentRangeFormattingOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrDocumentRangeFormattingOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrDocumentRangeFormattingOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrRenameOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrRenameOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrRenameOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrRenameOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrFoldingRangeOptionsOrFoldingRangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrSelectionRangeOptionsOrSelectionRangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrCallHierarchyOptionsOrCallHierarchyRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrLinkedEditingRangeOptionsOrLinkedEditingRangeRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensOptionsOrRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensOptionsOrRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensOptionsOrRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensOptionsOrRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrMonikerOptionsOrMonikerRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrMonikerOptionsOrMonikerRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrMonikerOptionsOrMonikerRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrMonikerOptionsOrMonikerRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrTypeHierarchyOptionsOrTypeHierarchyRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrInlineValueOptionsOrInlineValueRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrInlineValueOptionsOrInlineValueRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrInlineValueOptionsOrInlineValueRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrInlayHintOptionsOrInlayHintRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrInlayHintOptionsOrInlayHintRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrInlayHintOptionsOrInlayHintRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class DiagnosticOptionsOrRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DiagnosticOptionsOrRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDiagnosticOptionsOrRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDiagnosticOptionsOrRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrInlineCompletionOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrInlineCompletionOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrInlineCompletionOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrInlineCompletionOptions(ref decoder, ref value);
    }
}

internal sealed partial class PatternOrRelativePattern : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.PatternOrRelativePattern";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WritePatternOrRelativePattern(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadPatternOrRelativePattern(ref decoder, ref value);
    }
}

internal sealed partial class RangeOrEditRangeWithInsertReplace : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RangeOrEditRangeWithInsertReplace";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRangeOrEditRangeWithInsertReplace(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRangeOrEditRangeWithInsertReplace(ref decoder, ref value);
    }
}

internal sealed partial class BooleanOrSaveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrSaveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrSaveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadBooleanOrSaveOptions(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentContentOptionsOrRegistrationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentContentOptionsOrRegistrationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentContentOptionsOrRegistrationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentContentOptionsOrRegistrationOptions(ref decoder, ref value);
    }
}

internal sealed partial class StringOrTuple : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrTuple";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrTuple(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrTuple(ref decoder, ref value);
    }
}

internal sealed partial class StringOrBoolean : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrBoolean";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrBoolean(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrBoolean(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceFolderOrURI : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceFolderOrURI";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceFolderOrURI(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceFolderOrURI(ref decoder, ref value);
    }
}

internal partial struct BooleanOrClientSemanticTokensRequestFullDelta : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.BooleanOrClientSemanticTokensRequestFullDelta";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteBooleanOrClientSemanticTokensRequestFullDelta(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadBooleanOrClientSemanticTokensRequestFullDelta(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal sealed partial class VSImageElementOrClassifiedTextElementOrContainerElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSImageElementOrClassifiedTextElementOrContainerElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSImageElementOrClassifiedTextElementOrContainerElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSImageElementOrClassifiedTextElementOrContainerElement(ref decoder, ref value);
    }
}

internal sealed partial class LocationOrLocationsOrDefinitionLinksOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LocationOrLocationsOrDefinitionLinksOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocationOrLocationsOrDefinitionLinksOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocationOrLocationsOrDefinitionLinksOrNull(ref decoder, ref value);
    }
}

internal sealed partial class FoldingRangesOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.FoldingRangesOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteFoldingRangesOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadFoldingRangesOrNull(ref decoder, ref value);
    }
}

internal sealed partial class LocationOrLocationsOrDeclarationLinksOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LocationOrLocationsOrDeclarationLinksOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocationOrLocationsOrDeclarationLinksOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocationOrLocationsOrDeclarationLinksOrNull(ref decoder, ref value);
    }
}

internal sealed partial class SelectionRangesOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SelectionRangesOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSelectionRangesOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSelectionRangesOrNull(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyItemsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyItemsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyItemsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyItemsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyIncomingCallsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyIncomingCallsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyIncomingCallsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyIncomingCallsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class CallHierarchyOutgoingCallsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CallHierarchyOutgoingCallsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCallHierarchyOutgoingCallsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCallHierarchyOutgoingCallsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensOrNull(ref decoder, ref value);
    }
}

internal sealed partial class SemanticTokensOrSemanticTokensDeltaOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SemanticTokensOrSemanticTokensDeltaOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSemanticTokensOrSemanticTokensDeltaOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSemanticTokensOrSemanticTokensDeltaOrNull(ref decoder, ref value);
    }
}

internal sealed partial class LinkedEditingRangesOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LinkedEditingRangesOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLinkedEditingRangesOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLinkedEditingRangesOrNull(ref decoder, ref value);
    }
}

internal sealed partial class WorkspaceEditOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.WorkspaceEditOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteWorkspaceEditOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadWorkspaceEditOrNull(ref decoder, ref value);
    }
}

internal sealed partial class MonikersOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MonikersOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMonikersOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMonikersOrNull(ref decoder, ref value);
    }
}

internal sealed partial class TypeHierarchyItemsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TypeHierarchyItemsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTypeHierarchyItemsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTypeHierarchyItemsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class InlineValuesOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValuesOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValuesOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValuesOrNull(ref decoder, ref value);
    }
}

internal sealed partial class InlayHintsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlayHintsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlayHintsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlayHintsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport(ref decoder, ref value);
    }
}

internal sealed partial class InlineCompletionListOrItemsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineCompletionListOrItemsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineCompletionListOrItemsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineCompletionListOrItemsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class MessageActionItemOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MessageActionItemOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMessageActionItemOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMessageActionItemOrNull(ref decoder, ref value);
    }
}

internal sealed partial class TextEditsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextEditsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextEditsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextEditsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class CompletionItemsOrListOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CompletionItemsOrListOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCompletionItemsOrListOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCompletionItemsOrListOrNull(ref decoder, ref value);
    }
}

internal sealed partial class HoverOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.HoverOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteHoverOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadHoverOrNull(ref decoder, ref value);
    }
}

internal sealed partial class SignatureHelpOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SignatureHelpOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSignatureHelpOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSignatureHelpOrNull(ref decoder, ref value);
    }
}

internal sealed partial class LocationsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LocationsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLocationsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLocationsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class DocumentHighlightsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentHighlightsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentHighlightsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentHighlightsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class SymbolInformationsOrDocumentSymbolsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SymbolInformationsOrDocumentSymbolsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSymbolInformationsOrDocumentSymbolsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSymbolInformationsOrDocumentSymbolsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class CommandOrCodeAction : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CommandOrCodeAction";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCommandOrCodeAction(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCommandOrCodeAction(ref decoder, ref value);
    }
}

internal sealed partial class CommandOrCodeActionArrayOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CommandOrCodeActionArrayOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCommandOrCodeActionArrayOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCommandOrCodeActionArrayOrNull(ref decoder, ref value);
    }
}

internal sealed partial class SymbolInformationsOrWorkspaceSymbolsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.SymbolInformationsOrWorkspaceSymbolsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteSymbolInformationsOrWorkspaceSymbolsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadSymbolInformationsOrWorkspaceSymbolsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class CodeLensesOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.CodeLensesOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteCodeLensesOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadCodeLensesOrNull(ref decoder, ref value);
    }
}

internal sealed partial class DocumentLinksOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.DocumentLinksOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteDocumentLinksOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadDocumentLinksOrNull(ref decoder, ref value);
    }
}

internal sealed partial class RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRangeOrPrepareRenamePlaceholderOrPrepareRenameDefaultBehaviorOrNull(ref decoder, ref value);
    }
}

internal sealed partial class LSPAnyOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.LSPAnyOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteLSPAnyOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadLSPAnyOrNull(ref decoder, ref value);
    }
}

internal sealed partial class MultiDocumentHighlightsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.MultiDocumentHighlightsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteMultiDocumentHighlightsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadMultiDocumentHighlightsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class VSOnAutoInsertResponseItemOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSOnAutoInsertResponseItemOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSOnAutoInsertResponseItemOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSOnAutoInsertResponseItemOrNull(ref decoder, ref value);
    }
}

internal sealed partial class VSReferenceItemsOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.VSReferenceItemsOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteVSReferenceItemsOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadVSReferenceItemsOrNull(ref decoder, ref value);
    }
}

internal sealed partial class RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteRequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadRequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull(ref decoder, ref value);
    }
}

internal sealed partial class TextDocumentFilterLanguageOrSchemeOrPattern : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.TextDocumentFilterLanguageOrSchemeOrPattern";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteTextDocumentFilterLanguageOrSchemeOrPattern(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadTextDocumentFilterLanguageOrSchemeOrPattern(ref decoder, ref value);
    }
}

internal sealed partial class StringOrMarkedStringWithLanguage : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringOrMarkedStringWithLanguage";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringOrMarkedStringWithLanguage(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadStringOrMarkedStringWithLanguage(ref decoder, ref value);
    }
}

internal sealed partial class InlineValueTextOrVariableLookupOrEvaluatableExpression : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.InlineValueTextOrVariableLookupOrEvaluatableExpression";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteInlineValueTextOrVariableLookupOrEvaluatableExpression(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        ProtocolCodec.ReadInlineValueTextOrVariableLookupOrEvaluatableExpression(ref decoder, ref value);
    }
}

internal partial struct StringLiteralBegin : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralBegin";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralBegin(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralBegin(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralReport : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralReport";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralReport(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralReport(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralEnd : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralEnd";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralEnd(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralEnd(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralCreate : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralCreate";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralCreate(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralCreate(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralRename : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralRename";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralRename(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralRename(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralDelete : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralDelete";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralDelete(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralDelete(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralFull : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralFull";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralFull(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralFull(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralUnchanged : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralUnchanged";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralUnchanged(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralUnchanged(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralSnippet : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralSnippet";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralSnippet(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralSnippet(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralLanguageServerErrorResponse : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralLanguageServerErrorResponse";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralLanguageServerErrorResponse(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralLanguageServerErrorResponse(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralError : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralError";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralError(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralError(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralLanguageServerPerformanceStats : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralLanguageServerPerformanceStats";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralLanguageServerPerformanceStats(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralLanguageServerPerformanceStats(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralUsage : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralUsage";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralUsage(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralUsage(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralLanguageServerProjectInfo : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralLanguageServerProjectInfo";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralLanguageServerProjectInfo(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralLanguageServerProjectInfo(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralClassifiedTextRun : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralClassifiedTextRun";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralClassifiedTextRun(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralClassifiedTextRun(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralClassifiedTextElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralClassifiedTextElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralClassifiedTextElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralClassifiedTextElement(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralImageId : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralImageId";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralImageId(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralImageId(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralImageElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralImageElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralImageElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralImageElement(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct StringLiteralContainerElement : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.StringLiteralContainerElement";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteStringLiteralContainerElement(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadStringLiteralContainerElement(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedChangeAnnotationsSupportOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedChangeAnnotationsSupportOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedChangeAnnotationsSupportOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedChangeAnnotationsSupportOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedWorkspaceEditClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedWorkspaceEditClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedWorkspaceEditClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedWorkspaceEditClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDidChangeConfigurationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDidChangeConfigurationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDidChangeConfigurationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDidChangeConfigurationClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDidChangeWatchedFilesClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDidChangeWatchedFilesClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDidChangeWatchedFilesClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDidChangeWatchedFilesClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientSymbolKindOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientSymbolKindOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientSymbolKindOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientSymbolKindOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientSymbolTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientSymbolTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientSymbolTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientSymbolTagOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientSymbolResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientSymbolResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientSymbolResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientSymbolResolveOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedWorkspaceSymbolClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedWorkspaceSymbolClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedWorkspaceSymbolClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedWorkspaceSymbolClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedExecuteCommandClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedExecuteCommandClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedExecuteCommandClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedExecuteCommandClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedSemanticTokensWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedSemanticTokensWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedSemanticTokensWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedSemanticTokensWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCodeLensWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCodeLensWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCodeLensWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCodeLensWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedFileOperationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedFileOperationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedFileOperationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedFileOperationClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedInlineValueWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedInlineValueWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedInlineValueWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedInlineValueWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedInlayHintWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedInlayHintWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedInlayHintWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedInlayHintWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDiagnosticWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDiagnosticWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDiagnosticWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDiagnosticWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedFoldingRangeWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedFoldingRangeWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedFoldingRangeWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedFoldingRangeWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedTextDocumentContentClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedTextDocumentContentClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedTextDocumentContentClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedTextDocumentContentClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedWorkspaceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedWorkspaceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedWorkspaceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedWorkspaceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedTextDocumentSyncClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedTextDocumentSyncClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedTextDocumentSyncClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedTextDocumentSyncClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedTextDocumentFilterClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedTextDocumentFilterClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedTextDocumentFilterClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedTextDocumentFilterClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCompletionItemTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCompletionItemTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCompletionItemTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCompletionItemTagOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCompletionItemResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCompletionItemResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCompletionItemResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCompletionItemResolveOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCompletionItemInsertTextModeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCompletionItemInsertTextModeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCompletionItemInsertTextModeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCompletionItemInsertTextModeOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCompletionItemOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCompletionItemOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCompletionItemOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCompletionItemOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCompletionItemOptionsKind : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCompletionItemOptionsKind";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCompletionItemOptionsKind(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCompletionItemOptionsKind(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCompletionListCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCompletionListCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCompletionListCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCompletionListCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCompletionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCompletionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCompletionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCompletionClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedHoverClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedHoverClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedHoverClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedHoverClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientSignatureParameterInformationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientSignatureParameterInformationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientSignatureParameterInformationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientSignatureParameterInformationOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientSignatureInformationOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientSignatureInformationOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientSignatureInformationOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientSignatureInformationOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedSignatureHelpClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedSignatureHelpClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedSignatureHelpClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedSignatureHelpClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDeclarationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDeclarationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDeclarationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDeclarationClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDefinitionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDefinitionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDefinitionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDefinitionClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedTypeDefinitionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedTypeDefinitionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedTypeDefinitionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedTypeDefinitionClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedImplementationClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedImplementationClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedImplementationClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedImplementationClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedReferenceClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedReferenceClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedReferenceClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedReferenceClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentHighlightClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentHighlightClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentHighlightClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentHighlightClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentSymbolClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentSymbolClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentSymbolClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentSymbolClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCodeActionKindOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCodeActionKindOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCodeActionKindOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCodeActionKindOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCodeActionLiteralOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCodeActionLiteralOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCodeActionLiteralOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCodeActionLiteralOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCodeActionResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCodeActionResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCodeActionResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCodeActionResolveOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCodeActionTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCodeActionTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCodeActionTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCodeActionTagOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCodeActionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCodeActionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCodeActionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCodeActionClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCodeLensResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCodeLensResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCodeLensResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCodeLensResolveOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCodeLensClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCodeLensClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCodeLensClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCodeLensClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentLinkClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentLinkClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentLinkClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentLinkClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentColorClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentColorClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentColorClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentColorClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentFormattingClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentFormattingClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentFormattingClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentFormattingClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentRangeFormattingClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentRangeFormattingClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentRangeFormattingClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentRangeFormattingClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDocumentOnTypeFormattingClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDocumentOnTypeFormattingClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDocumentOnTypeFormattingClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDocumentOnTypeFormattingClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedRenameClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedRenameClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedRenameClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedRenameClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientFoldingRangeKindOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientFoldingRangeKindOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientFoldingRangeKindOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientFoldingRangeKindOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientFoldingRangeOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientFoldingRangeOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientFoldingRangeOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientFoldingRangeOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedFoldingRangeClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedFoldingRangeClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedFoldingRangeClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedFoldingRangeClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedSelectionRangeClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedSelectionRangeClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedSelectionRangeClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedSelectionRangeClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientDiagnosticsTagOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientDiagnosticsTagOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientDiagnosticsTagOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientDiagnosticsTagOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedPublishDiagnosticsClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedPublishDiagnosticsClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedPublishDiagnosticsClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedPublishDiagnosticsClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedCallHierarchyClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedCallHierarchyClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedCallHierarchyClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedCallHierarchyClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientSemanticTokensRequestOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientSemanticTokensRequestOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientSemanticTokensRequestOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientSemanticTokensRequestOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedSemanticTokensClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedSemanticTokensClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedSemanticTokensClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedSemanticTokensClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedLinkedEditingRangeClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedLinkedEditingRangeClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedLinkedEditingRangeClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedLinkedEditingRangeClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedMonikerClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedMonikerClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedMonikerClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedMonikerClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedTypeHierarchyClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedTypeHierarchyClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedTypeHierarchyClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedTypeHierarchyClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedInlineValueClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedInlineValueClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedInlineValueClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedInlineValueClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientInlayHintResolveOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientInlayHintResolveOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientInlayHintResolveOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientInlayHintResolveOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedInlayHintClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedInlayHintClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedInlayHintClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedInlayHintClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedDiagnosticClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedDiagnosticClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedDiagnosticClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedDiagnosticClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedInlineCompletionClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedInlineCompletionClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedInlineCompletionClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedInlineCompletionClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedTextDocumentClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedTextDocumentClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedTextDocumentClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedTextDocumentClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientShowMessageActionItemOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientShowMessageActionItemOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientShowMessageActionItemOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientShowMessageActionItemOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedShowMessageRequestClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedShowMessageRequestClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedShowMessageRequestClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedShowMessageRequestClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedShowDocumentClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedShowDocumentClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedShowDocumentClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedShowDocumentClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedWindowClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedWindowClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedWindowClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedWindowClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedStaleRequestSupportOptions : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedStaleRequestSupportOptions";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedStaleRequestSupportOptions(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedStaleRequestSupportOptions(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedRegularExpressionsClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedRegularExpressionsClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedRegularExpressionsClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedRegularExpressionsClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedMarkdownClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedMarkdownClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedMarkdownClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedMarkdownClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedGeneralClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedGeneralClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedGeneralClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedGeneralClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedExperimentalClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedExperimentalClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedExperimentalClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedExperimentalClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}

internal partial struct ResolvedClientCapabilities : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed
{
    string IProtocolTypeNamed.GoType => "lsproto.ResolvedClientCapabilities";

    void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.WriteResolvedClientCapabilities(encoder, this);
    void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)
    {
        var value = this;
        try
        {
            ProtocolCodec.ReadResolvedClientCapabilities(ref decoder, ref value);
        }
        finally
        {
            this = value;
        }
    }
}
