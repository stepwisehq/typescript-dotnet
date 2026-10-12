// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original union wrong-kind and boolean-union assertions.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestUnmarshalUnionTypeWrongKind()
    {
        {
            var v = new IntegerOrString();
            Require(DecodeError(ProtocolCodec.GetType_95e25e81cd99bf99(), "true", ref v)is not null, "IntegerOrString rejects true");
        }

        {
            var v = new IntegerOrString();
            Require(DecodeError(ProtocolCodec.GetType_95e25e81cd99bf99(), "null", ref v)is not null, "IntegerOrString rejects null");
        }

        {
            var v = new IntegerOrString();
            Require(DecodeError(ProtocolCodec.GetType_95e25e81cd99bf99(), "{}", ref v)is not null, "IntegerOrString rejects {}");
        }

        {
            var v = new IntegerOrString();
            Require(DecodeError(ProtocolCodec.GetType_95e25e81cd99bf99(), "[]", ref v)is not null, "IntegerOrString rejects []");
        }

        {
            var v = new StringOrInlayHintLabelParts();
            Require(DecodeError(ProtocolCodec.GetType_be643dd009a1ea80(), "42", ref v)is not null, "StringOrInlayHintLabelParts rejects 42");
        }

        {
            var v = new StringOrInlayHintLabelParts();
            Require(DecodeError(ProtocolCodec.GetType_be643dd009a1ea80(), "true", ref v)is not null, "StringOrInlayHintLabelParts rejects true");
        }
    }

    private static void TestUnmarshalBooleanUnionTypes()
    {
        {
            var value = Decode(ProtocolCodec.GetType_baa50df5ae26800a(), "true");
            Require(value.Boolean is not null, "BooleanOrHoverOptions bool presence");
            Require(value.Boolean.Value == true, "BooleanOrHoverOptions bool value");
            Require(value.HoverOptions is null, "BooleanOrHoverOptions object absence");
        }

        {
            var value = Decode(ProtocolCodec.GetType_baa50df5ae26800a(), "false");
            Require(value.Boolean is not null, "BooleanOrHoverOptions bool presence");
            Require(value.Boolean.Value == false, "BooleanOrHoverOptions bool value");
            Require(value.HoverOptions is null, "BooleanOrHoverOptions object absence");
        }

        {
            var value = Decode(ProtocolCodec.GetType_baa50df5ae26800a(), "{}");
            Require(value.Boolean is null, "BooleanOrHoverOptions bool absence");
            Require(value.HoverOptions is not null, "BooleanOrHoverOptions object presence");
        }

        {
            var value = new BooleanOrHoverOptions();
            Require(DecodeError(ProtocolCodec.GetType_baa50df5ae26800a(), "\"nope\"", ref value)is not null, "BooleanOrHoverOptions rejects string");
        }
    }
}
