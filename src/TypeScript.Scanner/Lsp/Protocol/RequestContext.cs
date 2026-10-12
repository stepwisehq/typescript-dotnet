// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Explicit request-scoped counterpart of Go context values in lsproto/lsp.go
// and locale/locale.go. Cancellation remains the standard .NET token.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;
using LocaleValue = TypeScript.Locale.Locale;

namespace TypeScript.Lsp.Protocol;
internal sealed class ProtocolRequestContext
{
    private sealed class CapabilityOwner(ResolvedClientCapabilities capabilities)
    {
        internal readonly ResolvedClientCapabilities Value = capabilities;
    }

    private readonly CapabilityOwner capabilities;
    internal readonly CancellationToken Cancellation;
    internal readonly LocaleValue Locale;
    internal readonly ByteStringValue RequestID;
    internal readonly CheckerLifetime CheckerLifetime;
    internal readonly FormatCodeSettings? FormatSettings;
    internal readonly ByteStringValue? FormatHostNewLine;
    internal ref readonly ResolvedClientCapabilities ClientCapabilities => ref capabilities.Value;

    internal ProtocolRequestContext(CancellationToken cancellation, LocaleValue locale, ResolvedClientCapabilities capabilities) : this(cancellation, locale, new CapabilityOwner(capabilities), default, TypeScript.Scanner.CheckerLifetime.Temporary, null, null)
    {
    }

    private ProtocolRequestContext(CancellationToken cancellation, LocaleValue locale, CapabilityOwner capabilities, ByteStringValue requestID, CheckerLifetime checkerLifetime, FormatCodeSettings? formatSettings, ByteStringValue? formatHostNewLine)
    {
        Cancellation = cancellation;
        Locale = locale;
        this.capabilities = capabilities;
        RequestID = requestID;
        CheckerLifetime = checkerLifetime;
        FormatSettings = formatSettings;
        FormatHostNewLine = formatHostNewLine;
    }

    internal static readonly ProtocolRequestContext Default = new(default, default, default(ResolvedClientCapabilities));
    internal ProtocolRequestContext WithCancellationToken(CancellationToken cancellation) => new(cancellation, Locale, capabilities, RequestID, CheckerLifetime, FormatSettings, FormatHostNewLine);
    internal ProtocolRequestContext WithLocale(LocaleValue locale) => new(Cancellation, locale, capabilities, RequestID, CheckerLifetime, FormatSettings, FormatHostNewLine);
    internal ProtocolRequestContext WithClientCapabilities(ResolvedClientCapabilities value) => new(Cancellation, Locale, new CapabilityOwner(value), RequestID, CheckerLifetime, FormatSettings, FormatHostNewLine);
    internal ProtocolRequestContext WithRequestID(ByteStringValue requestID) => new(Cancellation, Locale, capabilities, requestID, CheckerLifetime, FormatSettings, FormatHostNewLine);
    internal ProtocolRequestContext WithCheckerLifetime(CheckerLifetime lifetime) => new(Cancellation, Locale, capabilities, RequestID, lifetime, FormatSettings, FormatHostNewLine);
    internal ProtocolRequestContext WithFormatCodeSettings(FormatCodeSettings settings, ByteStringValue newLine) => new(Cancellation, Locale, capabilities, RequestID, CheckerLifetime, settings, newLine);
}

internal static class ProtocolContexts
{
    // lsp.go:381: deriving a context preserves all unrelated request values.
    internal static ProtocolRequestContext WithClientCapabilities(ProtocolRequestContext context, ResolvedClientCapabilities capabilities) => context.WithClientCapabilities(capabilities);
    internal static ref readonly ResolvedClientCapabilities GetClientCapabilities(ProtocolRequestContext context) => ref context.ClientCapabilities;
}
