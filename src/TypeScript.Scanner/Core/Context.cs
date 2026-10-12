// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/core/context.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Lsp.Protocol;

namespace TypeScript.Scanner;

// context.go:28. The default request lifetime remains temporary.
internal enum CheckerLifetime
{
    Temporary = 0,
    Diagnostics = 1,
    API = 2,
}

internal static partial class Core
{
    // context.go:14. Explicit immutable derivation replaces context.WithValue.
    internal static ProtocolRequestContext WithRequestID(ProtocolRequestContext context, ByteStringValue id) => context.WithRequestID(id);

    // context.go:18. The default header is the upstream empty Go string.
    internal static ByteStringValue GetRequestID(ProtocolRequestContext context) => context.RequestID;

    // context.go:36.
    internal static ProtocolRequestContext WithCheckerLifetime(ProtocolRequestContext context, CheckerLifetime lifetime) => context.WithCheckerLifetime(lifetime);

    // context.go:40.
    internal static CheckerLifetime GetCheckerLifetime(ProtocolRequestContext context) => context.CheckerLifetime;
}
