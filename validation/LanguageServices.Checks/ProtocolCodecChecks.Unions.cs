// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestUnmarshalUnionTypes and TestMarshalUnionTypes.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static T Decode<T>(ProtocolType<T> descriptor, string input)
    {
        T value = default!;
        ProtocolCodec.Unmarshal(descriptor, Bytes(input), ref value);
        return value;
    }

    private static void TestUnmarshalUnionTypes()
    {
        var integer = Decode(ProtocolCodec.GetType_95e25e81cd99bf99(), "42");
        Require(integer.Integer is not null, "IntegerOrString integer presence");
        Require(integer.Integer.Value == 42, "IntegerOrString integer value");
        Require(integer.String is null, "IntegerOrString string absence");
        var text = Decode(ProtocolCodec.GetType_95e25e81cd99bf99(), "\"hello\"");
        Require(text.String is not null, "IntegerOrString string presence");
        Require(text.String.Value.EqualsBytes("hello"u8), "IntegerOrString string value");
        Require(text.Integer is null, "IntegerOrString integer absence");
        var number = Decode(ProtocolCodec.GetType_17d3cfba51c6c525(), "42");
        Require(number.Integer is not null, "IntegerOrNull integer presence");
        Require(number.Integer.Value == 42, "IntegerOrNull integer value");
        var empty = Decode(ProtocolCodec.GetType_17d3cfba51c6c525(), "null");
        Require(empty.Integer is null, "IntegerOrNull integer absence");
        var uri = Decode(ProtocolCodec.GetType_b23e05e41f22b21f(), "\"file:///test.ts\"");
        Require(uri.DocumentUri is not null, "DocumentUriOrNull URI presence");
        Require(uri.DocumentUri.Value.Value.EqualsBytes("file:///test.ts"u8), "DocumentUriOrNull URI value");
        var noUri = Decode(ProtocolCodec.GetType_b23e05e41f22b21f(), "null");
        Require(noUri.DocumentUri is null, "DocumentUriOrNull URI absence");
    }

    private static void TestMarshalUnionTypes()
    {
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_17d3cfba51c6c525(), new IntegerOrNull { Integer = 42 }).EqualsBytes("42"u8), "IntegerOrNull value");
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_17d3cfba51c6c525(), new IntegerOrNull()).EqualsBytes("null"u8), "IntegerOrNull null");
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_95e25e81cd99bf99(), new IntegerOrString { Integer = 7 }).EqualsBytes("7"u8), "IntegerOrString integer");
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_95e25e81cd99bf99(), new IntegerOrString { @String = Text("tok") }).EqualsBytes("\"tok\""u8), "IntegerOrString string");
    }
}
