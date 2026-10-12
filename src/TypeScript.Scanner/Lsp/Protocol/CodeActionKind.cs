// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
// internal/lsp/lsproto/lsp.go:397–407. Preserve raw byte kind boundaries.
internal readonly partial struct CodeActionKind
{
    internal bool Contains(CodeActionKind other) => Equals(other) || Equals(Empty) || other.Value.StartsWith(ByteStringValue.Concat(Value, ByteStringValue.FromString(".")));
    // Go constants are initialized independently; partial C# field ordering must not
    // read uninitialized generated base-kind headers.
    internal static readonly CodeActionKind SourceFixAllTs = new(ByteStringValue.FromString("source.fixAll.ts"));
    internal static readonly CodeActionKind SourceOrganizeImportsTs = new(ByteStringValue.FromString("source.organizeImports.ts"));
    internal static readonly CodeActionKind SourceRemoveUnusedImportsTs = new(ByteStringValue.FromString("source.removeUnusedImports.ts"));
    internal static readonly CodeActionKind SourceSortImportsTs = new(ByteStringValue.FromString("source.sortImports.ts"));
}
