// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed root-shape counterpart for server.go:1554 json.MarshalIndent(&capabilities).
// Reuses generated fields; pointer admission is distinct from the value root.
namespace TypeScript.Lsp.Protocol;
internal static class ProtocolServerDescriptors
{
    private static class CapabilitiesRoot
    {
        static CapabilitiesRoot()
        {
        }

        internal static readonly ProtocolType<ProtocolPointer<ResolvedClientCapabilities>?> Value = new("*lsproto.ResolvedClientCapabilities", Read, Write);
        private static void Read(ref ProtocolDecoder decoder, ref ProtocolPointer<ResolvedClientCapabilities>? value)
        {
            if (decoder.PeekKind() == 'n')
            {
                decoder.ReadToken((byte)'n');
                value = null;
                return;
            }

            value ??= new(default);
            ProtocolCodec.ReadResolvedClientCapabilities(ref decoder, ref value.Value);
        }

        private static void Write(ProtocolEncoder encoder, ProtocolPointer<ResolvedClientCapabilities>? value)
        {
            if (value is null)
                encoder.WriteNull();
            else
                ProtocolCodec.WriteResolvedClientCapabilities(encoder, value.Value);
        }
    }

    internal static ProtocolType<ProtocolPointer<ResolvedClientCapabilities>?> ResolvedClientCapabilitiesPointer => CapabilitiesRoot.Value;
}

internal static partial class ProtocolCodec
{
    private static class ContributionOptionsRoot
    {
        static ContributionOptionsRoot()
        {
        }

        internal static readonly ProtocolType<ProtocolMap<TypeScript.Scanner.ByteStringKey, object?>> Value = new("map[string]interface {}", ReadShape_26c0c2adc8ab309a, WriteShape_26c0c2adc8ab309a);
    }

    internal static ProtocolType<ProtocolMap<TypeScript.Scanner.ByteStringKey, object?>> ContentMapperContributionOptions => ContributionOptionsRoot.Value;
}
