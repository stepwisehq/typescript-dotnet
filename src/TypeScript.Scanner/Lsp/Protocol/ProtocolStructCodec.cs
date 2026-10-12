// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed counterpart of lsproto/structcodec.go, preserving required-field and deferred-arm ownership.
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.JsonRpc;

namespace TypeScript.Lsp.Protocol;
internal readonly record struct ProtocolStructField(string Name, int Index, int RequiredID, bool RejectNull);
internal sealed class ProtocolStructSpec
{
    internal readonly Dictionary<ByteStringKey, ProtocolStructField> ByName;
    private readonly Slice<ByteStringValue> requiredNames;
    private readonly ulong requiredMask;
    internal ProtocolStructSpec(int fieldCount, ReadOnlySpan<ProtocolStructField> fields)
    {
        ByName = new(fieldCount);
        var names = Slice<ByteStringValue>.Make(0);
        ulong mask = 0;
        foreach (var field in fields)
        {
            var name = ByteStringValue.FromString(field.Name);
            ByName[ByteStringKey.FromValue(name)] = field;
            if (field.RequiredID >= 0)
            {
                mask |= 1UL << field.RequiredID;
                names = names.Append(name);
            }
        }

        requiredNames = names;
        requiredMask = mask;
    }

    internal void Finish(ulong seen)
    {
        ulong missing = requiredMask & ~seen;
        if (missing == 0)
            return;
        var names = Slice<ByteStringValue>.Make(0);
        for (int id = 0; id < requiredNames.Length; id++)
            if ((missing & (1UL << id)) != 0)
                names = names.Append(requiredNames[id]);
        throw ProtocolErrors.Missing(names);
    }
}

internal readonly record struct ProtocolDeferredField(ByteStringValue Name, RawJsonValue Value);
internal struct ProtocolDiscriminatedState(string typeName, ByteStringValue discriminator)
{
    internal readonly string TypeName = typeName;
    internal readonly ByteStringValue Discriminator = discriminator;
    internal ProtocolRawValue? DiscriminatorValue;
    internal bool HasDiscriminator;
    internal Slice<ProtocolDeferredField> Deferred;
    internal bool Closed;
    internal JsonException InvalidDiscriminator()
    {
        var quoted = new GoBuilder();
        TypeScript.Json.JsonCodec.QuoteToBuilder(quoted, Discriminator);
        return ProtocolErrors.Text(HasDiscriminator ? ByteStringValue.Concat(ByteStringValue.FromString("invalid " + TypeName + " discriminator "), quoted.Value(), ByteStringValue.FromString(": "), DiscriminatorValue!.Value.TextValue) : ByteStringValue.Concat(ByteStringValue.FromString("invalid " + TypeName + ": missing discriminator "), quoted.Value()));
    }
}

internal delegate bool ProtocolFieldRead<T>(ref ProtocolDecoder decoder, ref T target, ByteStringValue name, ref ulong seen, bool skipUnknown);
internal static partial class ProtocolCodec
{
    // structcodec.go:184. State carries the decoder's position by explicit ref
    // call, rather than storing a pointer to a ref-struct in an escaping owner.
    internal static ProtocolDiscriminatedState ScanDiscriminatedStruct(ref ProtocolDecoder decoder, string typeName, ByteStringValue discriminator)
    {
        if (decoder.PeekKind() != '{')
            throw ProtocolErrors.NotObject(decoder.PeekKind());
        decoder.ReadToken((byte)'{');
        var state = new ProtocolDiscriminatedState(typeName, discriminator);
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsValue(discriminator))
            {
                state.DiscriminatorValue = decoder.ReadValue();
                state.HasDiscriminator = true;
                return state;
            }

            var value = decoder.ReadValue();
            state.Deferred = state.Deferred.Append(new ProtocolDeferredField(name, value.Clone()));
        }

        decoder.ReadToken((byte)'}');
        state.Closed = true;
        return state;
    }

    // structcodec.go unmarshalDiscriminatedArm: a closed field function replaces
    // the Go reflect.Type cache, while preserving the original transaction and
    // deferred-field order. The decoder is an explicit ref because it is stack-owned.
    internal static void UnmarshalDiscriminatedArm<T>(ref ProtocolDecoder decoder, ProtocolDiscriminatedState state, ref T? value, ProtocolStructSpec spec, ProtocolFieldRead<T> readField, Func<T> create)
        where T : class
    {
        T target = create();
        ulong seen = 0;
        if (state.HasDiscriminator)
        {
            var nested = new ProtocolDecoder(state.DiscriminatorValue!.Value.TextValue);
            if (readField(ref nested, ref target, state.Discriminator, ref seen, false))
                nested.Finish();
        }

        foreach (var field in state.Deferred)
        {
            var nested = new ProtocolDecoder(field.Value.TextValue);
            if (readField(ref nested, ref target, field.Name, ref seen, false))
                nested.Finish();
        }

        if (!state.Closed)
        {
            while (decoder.PeekKind() != '}')
            {
                var name = decoder.ReadPropertyName();
                readField(ref decoder, ref target, name, ref seen, true);
            }

            decoder.ReadToken((byte)'}');
        }

        spec.Finish(seen);
        value = target;
    }
}
