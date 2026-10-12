// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original optionalDiscriminatorArm test owner and closed field metadata adapter.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private sealed class OptionalDiscriminatorArm
    {
        internal StringLiteralBegin? Kind;
        internal ByteStringValue Title;
    }

    private static class OptionalArmSpec
    {
        static OptionalArmSpec()
        {
        }

        internal static readonly ProtocolStructSpec Value = new(2, [new("kind", 0, -1, true), new("title", 1, 0, false), ]);
    }

    private static bool ReadOptionalArmField(ref ProtocolDecoder decoder, ref OptionalDiscriminatorArm target, ByteStringValue name, ref ulong seen, bool skipUnknown)
    {
        if (!OptionalArmSpec.Value.ByName.TryGetValue(ByteStringKey.FromValue(name), out var field))
        {
            if (skipUnknown)
                decoder.SkipValue();
            return false;
        }

        if (field.RequiredID >= 0)
            seen |= 1UL << field.RequiredID;
        if (field.RejectNull && decoder.PeekKind() == 'n')
            throw ProtocolErrors.Null(name);
        if (field.Index == 0)
        {
            target.Kind = default(StringLiteralBegin);
            StringLiteralBegin value = default;
            try
            {
                ProtocolCodec.ReadStringLiteralBegin(ref decoder, ref value);
            }
            finally
            {
                target.Kind = value;
            }
        }
        else
            target.Title = decoder.ReadString();
        return true;
    }

    private static void CheckOriginalOptionalDiscriminatorArm()
    {
        var decoder = new ProtocolDecoder(Bytes("{\"kind\":\"begin\",\"title\":\"Indexing\"}"));
        var state = ProtocolCodec.ScanDiscriminatedStruct(ref decoder, "optionalDiscriminatorArm", Text("kind"));
        OptionalDiscriminatorArm? value = null;
        ProtocolCodec.UnmarshalDiscriminatedArm(ref decoder, state, ref value, OptionalArmSpec.Value, ReadOptionalArmField, static () => new OptionalDiscriminatorArm());
        Require(value is not null, "optional discriminator arm presence");
        Require(value.Kind is not null, "optional discriminator kind retained");
        Require(value.Title.EqualsBytes("Indexing"u8), "optional discriminator title");
    }
}
