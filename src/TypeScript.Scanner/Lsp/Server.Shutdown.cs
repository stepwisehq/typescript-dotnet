// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1827–1838,1894–1905; stdio-only contracted owner draft.
using System.Globalization;
using TypeScript.Lsp.Protocol;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (Null Value, Exception? Error) HandleShutdown(ProtocolRequestContext context, NoParams parameters, RequestMessage request)
    {
        builtinWatcher?.Close();
        // initializeAPISession is outside the stdio contract and never advertised;
        // no external embedding sessions can exist in this contracted owner.
        session!.Close();
        return (default, null);
    }

    private static Exception? HandleExit(ProtocolRequestContext context, NoParams parameters) => new EndOfStreamException();
    private Exception? HandleSetLogVerbosity(ProtocolRequestContext context, SetLogVerbosityParams parameters)
    {
        if (!ServerLogger.IsValidLogVerbosity(parameters.Verbosity))
            return new ProtocolErrorException(ErrorCode.InvalidParams, "invalid log verbosity " + ((int)parameters.Verbosity).ToString(CultureInfo.InvariantCulture));
        logger.SetVerbosity(parameters.Verbosity);
        return null;
    }

    internal bool IsActive()
    {
        long last = Interlocked.Read(ref lastRequestTimeMs);
        return last == 0 || DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(last) <= TimeSpan.FromMinutes(1);
    }
}
