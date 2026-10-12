// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Modules;
using TypeScript.Lsp.Protocol;

namespace TypeScript.ProjectSystem;
// internal/project/snapshot.go:327. Request derivations reach the actual resolver host.
internal interface IModuleResolverFactory
{
    (IResolver Resolver, Action Done) NewResolver(ProtocolRequestContext context, ResolverOptions options);
}
