// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestUnmarshalAcceptsOmittedOptionalFields, including every field check.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestUnmarshalAcceptsOmittedOptionalFields()
    {
        var hint = new InlayHint();
        ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), Bytes("{\"position\":{\"line\":1,\"character\":5},\"label\":\"test\"}"), ref hint);
        Require(hint.Kind is null, "InlayHint Kind");
        Require(hint.TextEdits is null, "InlayHint TextEdits");
        Require(hint.Tooltip is null, "InlayHint Tooltip");
        Require(hint.PaddingLeft is null, "InlayHint PaddingLeft");
        Require(hint.PaddingRight is null, "InlayHint PaddingRight");
        Require(hint.Data is null, "InlayHint Data");
        Require(hint.Position.Line == 1u, "InlayHint Position.Line");
        Require(hint.Position.Character == 5u, "InlayHint Position.Character");
        var range = new FoldingRange();
        ProtocolCodec.Unmarshal(ProtocolCodec.GetType_66fbdf0fd1929f3b(), Bytes("{\"startLine\":5,\"endLine\":10}"), ref range);
        Require(range.Kind is null, "FoldingRange Kind");
        Require(range.StartCharacter is null, "FoldingRange StartCharacter");
        Require(range.EndCharacter is null, "FoldingRange EndCharacter");
        Require(range.CollapsedText is null, "FoldingRange CollapsedText");
        Require(range.StartLine == 5u, "FoldingRange StartLine");
        Require(range.EndLine == 10u, "FoldingRange EndLine");
    }
}
