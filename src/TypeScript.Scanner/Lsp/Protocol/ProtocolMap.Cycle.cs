// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// json/v2 map cycle keys retain the actual shared dictionary owner, not a boxed header.
namespace TypeScript.Lsp.Protocol;
internal readonly partial struct ProtocolMap<TKey, TValue>
    where TKey : notnull
{
    internal object? CycleOwner => entries;
}
