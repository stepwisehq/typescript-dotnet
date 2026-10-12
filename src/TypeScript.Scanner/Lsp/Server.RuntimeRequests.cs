// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/server.go:2469; internal/pprof/pprof.go:167.
using TypeScript.Lsp.Protocol;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (Null Value, Exception? Error) HandleRunGC(ProtocolRequestContext context, NoParams parameters, RequestMessage request)
    {
        // The supported public request triggers this runtime's full collection;
        // it does not expose the excluded Go-specific profiling endpoints.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        logger.Info("GC triggered");
        return (new Null(), null);
    }
}
