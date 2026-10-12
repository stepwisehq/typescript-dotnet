// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestRoundTrip and TestMarshalUnmarshalRoundTrip value factories/assertions.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Range = TypeScript.Lsp.Protocol.Range;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static ByteStringValue Text(string value) => ByteStringValue.FromString(GoString.ToUtf8(value));
    private static void Remarshal<T>(ProtocolType<T> descriptor, T expected, string name)
    {
        var data = ProtocolCodec.Marshal(descriptor, expected);
        T result = default!;
        ProtocolCodec.Unmarshal(descriptor, data, ref result);
        var again = ProtocolCodec.Marshal(descriptor, result);
        Require(data.EqualsValue(again), name + ": re-marshal differs");
    }

    private static void StructuralRoundTrip<T>(ProtocolType<T> descriptor, T expected, Func<T, T, bool> equal, string name)
    {
        var data = ProtocolCodec.Marshal(descriptor, expected);
        T result = default!;
        ProtocolCodec.Unmarshal(descriptor, data, ref result);
        Require(equal(expected, result), name + ": structural round trip differs");
    }

    private static Range OriginalRoundTripRange() => new()
    {
        Start = new Position
        {
            Line = 1,
            Character = 2
        },
        End = new Position
        {
            Line = 3,
            Character = 4
        },
    };
    private static void TestRoundTrip()
    {
        Remarshal(ProtocolCodec.GetType_5de74a81b192304c(), OriginalRoundTripRange(), "Range");
        Remarshal(ProtocolCodec.GetType_907ab9937a53fb02(), new TextEdit { @Range = OriginalRoundTripRange(), NewText = Text("hello") }, "TextEdit");
        Remarshal(ProtocolCodec.GetType_ec8d99ae989825b7(), new MarkupContent { Kind = MarkupKind.Markdown, Value = Text("**x**") }, "MarkupContent");
        Remarshal(ProtocolCodec.GetType_7f22677bab49bf26(), new DidChangeConfigurationParams { Settings = new Dictionary<string, object?> { ["js/ts"] = new Dictionary<string, object?> { ["x"] = 1d } }, }, "DidChangeConfigurationParams object");
        Remarshal(ProtocolCodec.GetType_7f22677bab49bf26(), new DidChangeConfigurationParams { Settings = null }, "DidChangeConfigurationParams null");
        Remarshal(ProtocolCodec.GetType_d2a93ce39bb367de(), new CompletionItem { Label = Text("pageXOffset"), Kind = CompletionItemKind.Field, SortText = Text("15"), InsertTextFormat = InsertTextFormat.PlainText, }, "CompletionItem");
        Remarshal(ProtocolCodec.GetType_6e5531cdb6cb8530(), new ParameterInformation { Label = new StringOrTuple { @String = Text("p: number") } }, "ParameterInformation string label");
        Remarshal(ProtocolCodec.GetType_6e5531cdb6cb8530(), new ParameterInformation { Label = new StringOrTuple { Tuple = Slice<uint>.FromArray([0u, 4u]) } }, "ParameterInformation tuple label");
    }

    private static void TestMarshalUnmarshalRoundTrip()
    {
        StructuralRoundTrip(ProtocolCodec.GetType_551be06dac20fdd0(), new InlayHint { Position = new Position { Line = 1, Character = 5 }, Label = new StringOrInlayHintLabelParts { @String = Text("param") }, Kind = InlayHintKind.Parameter, }, EqualShape_551be06dac20fdd0, "InlayHint with kind");
        StructuralRoundTrip(ProtocolCodec.GetType_551be06dac20fdd0(), new InlayHint { Position = new Position { Line = 0, Character = 0 }, Label = new StringOrInlayHintLabelParts { @String = Text("x") }, }, EqualShape_551be06dac20fdd0, "InlayHint minimal");
        StructuralRoundTrip(ProtocolCodec.GetType_66fbdf0fd1929f3b(), new FoldingRange { StartLine = 1, StartCharacter = 0u, EndLine = 10, EndCharacter = 5u, Kind = FoldingRangeKind.Region, CollapsedText = Text("..."), }, EqualShape_66fbdf0fd1929f3b, "FoldingRange with all fields");
        StructuralRoundTrip(ProtocolCodec.GetType_15b61974b2707a7b(), new Location { Uri = new DocumentUri(Text("file:///test.ts")), @Range = OriginalRoundTripRange() }, EqualShape_15b61974b2707a7b, "Location");
        StructuralRoundTrip(ProtocolCodec.GetType_6cabbf134c6b5195(), new InitializeParams { ProcessId = new IntegerOrNull(), RootUri = new DocumentUriOrNull { DocumentUri = new DocumentUri(Text("file:///workspace")) }, Capabilities = new ClientCapabilities(), }, EqualShape_6cabbf134c6b5195, "InitializeParams with null processId");
    }
}
