// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
// internal/lsp/lsproto/lsp.go:397–407. Preserve raw byte kind boundaries.
internal readonly partial struct CodeActionKind
{
    internal bool Contains(CodeActionKind other) => Equals(other) || Equals(Empty) || other.Value.StartsWith(ByteStringValue.Concat(Value, ByteStringValue.FromString(".")));
    internal static readonly CodeActionKind SourceFixAllTs = new(ByteStringValue.Concat(SourceFixAll.Value, ByteStringValue.FromString(".ts")));
    internal static readonly CodeActionKind SourceOrganizeImportsTs = new(ByteStringValue.Concat(SourceOrganizeImports.Value, ByteStringValue.FromString(".ts")));
    internal static readonly CodeActionKind SourceRemoveUnusedImportsTs = new(ByteStringValue.Concat(Source.Value, ByteStringValue.FromString(".removeUnusedImports.ts")));
    internal static readonly CodeActionKind SourceSortImportsTs = new(ByteStringValue.Concat(Source.Value, ByteStringValue.FromString(".sortImports.ts")));
}
