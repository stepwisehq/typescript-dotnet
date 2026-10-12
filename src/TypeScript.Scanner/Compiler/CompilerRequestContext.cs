// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Go context.Context retains request values through compiler/program.go emission.
using ProtocolRequestContext = TypeScript.Lsp.Protocol.ProtocolRequestContext;

namespace TypeScript.Compiler;
internal readonly struct CompilerRequestContext
{
    internal readonly CancellationToken Cancellation;
    internal readonly ProtocolRequestContext? Request;
    internal CompilerRequestContext(CancellationToken cancellation) => Cancellation = cancellation;
    internal CompilerRequestContext(ProtocolRequestContext request)
    {
        Request = request;
        Cancellation = request.Cancellation;
    }

    internal bool IsCancellationRequested => Cancellation.IsCancellationRequested;
}
