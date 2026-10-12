// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestMarshalOmitsZeroOptionalFields and TestLiteralTypes.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestMarshalOmitsZeroOptionalFields()
    {
        var hint = new InlayHint
        {
            Position = new Position
            {
                Line = 0,
                Character = 0
            },
            Label = new StringOrInlayHintLabelParts
            {
                @String = Text("x")
            },
        };
        var text = ProtocolCodec.Marshal(ProtocolCodec.GetType_551be06dac20fdd0(), hint).Materialize()!;
        Require(!text.Contains("kind", StringComparison.Ordinal), "InlayHint omits kind");
        Require(!text.Contains("textEdits", StringComparison.Ordinal), "InlayHint omits textEdits");
        Require(!text.Contains("paddingLeft", StringComparison.Ordinal), "InlayHint omits paddingLeft");
        Require(text.Contains("position", StringComparison.Ordinal), "InlayHint contains position");
        Require(text.Contains("label", StringComparison.Ordinal), "InlayHint contains label");
        var range = new FoldingRange
        {
            StartLine = 1,
            EndLine = 10
        };
        text = ProtocolCodec.Marshal(ProtocolCodec.GetType_66fbdf0fd1929f3b(), range).Materialize()!;
        Require(!text.Contains("kind", StringComparison.Ordinal), "FoldingRange omits kind");
        Require(!text.Contains("startCharacter", StringComparison.Ordinal), "FoldingRange omits startCharacter");
        Require(text.Contains("startLine", StringComparison.Ordinal), "FoldingRange contains startLine");
        Require(text.Contains("endLine", StringComparison.Ordinal), "FoldingRange contains endLine");
    }

    private static void TestLiteralTypes()
    {
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_15379156e683e93e(), new StringLiteralCreate()).EqualsBytes("\"create\""u8), "StringLiteralCreate marshal");
        var value = new StringLiteralCreate();
        ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15379156e683e93e(), Bytes("\"create\""), ref value);
        value = new StringLiteralCreate();
        Require(DecodeError(ProtocolCodec.GetType_15379156e683e93e(), "\"delete\"", ref value)is not null, "StringLiteralCreate rejects wrong value");
        value = new StringLiteralCreate();
        Require(DecodeError(ProtocolCodec.GetType_15379156e683e93e(), "42", ref value)is not null, "StringLiteralCreate rejects wrong type");
    }
}
