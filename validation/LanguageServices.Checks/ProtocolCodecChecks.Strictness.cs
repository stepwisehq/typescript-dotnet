// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original lsp_json_test.go enum and strictness assertions.
using TypeScript.Lsp.Protocol;
using Range = TypeScript.Lsp.Protocol.Range;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestEnumStringValues()
    {
        Require(InlayHintKind.Type.String().EqualsBytes("Type"u8), "InlayHintKind Type");
        Require(InlayHintKind.Parameter.String().EqualsBytes("Parameter"u8), "InlayHintKind Parameter");
        Require(SymbolKind.File.String().EqualsBytes("File"u8), "SymbolKind File");
        Require(SymbolKind.Function.String().EqualsBytes("Function"u8), "SymbolKind Function");
        Require(SymbolKind.Variable.String().EqualsBytes("Variable"u8), "SymbolKind Variable");
        Require(((InlayHintKind)999).String().Materialize()!.Contains("999", StringComparison.Ordinal), "unknown enum numeric value");
    }

    private static Exception? DecodeError<T>(ProtocolType<T> descriptor, string input, ref T value)
    {
        try
        {
            ProtocolCodec.Unmarshal(descriptor, Bytes(input), ref value);
            return null;
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            return error;
        }
    }

    private static void TestStrictnessMissingRequired()
    {
        var textEdit = new TextEdit();
        var error = DecodeError(ProtocolCodec.GetType_907ab9937a53fb02(), "{\"range\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":1}}}", ref textEdit);
        Require(error is not null && error.Message.Contains("missing required properties", StringComparison.Ordinal), "TextEdit missing newText");
        var range = new Range();
        error = DecodeError(ProtocolCodec.GetType_5de74a81b192304c(), "{\"start\":{\"line\":0,\"character\":0}}", ref range);
        Require(error is not null && error.Message.Contains("missing required properties", StringComparison.Ordinal), "Range missing end");
        var position = new Position();
        error = DecodeError(ProtocolCodec.GetType_6d031af10da7a25e(), "{\"line\":0}", ref position);
        Require(error is not null && error.Message.Contains("missing required properties", StringComparison.Ordinal), "Position missing character");
    }

    private static void TestStrictnessNotObject()
    {
        var value = new TextEdit();
        var error = DecodeError(ProtocolCodec.GetType_907ab9937a53fb02(), "\"oops\"", ref value);
        Require(error is not null, "TextEdit nonobject must fail");
        Require(error!.Message.Contains("object", StringComparison.Ordinal) || error.Message.Contains("cannot unmarshal", StringComparison.Ordinal), "TextEdit nonobject error");
    }
}
