// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/ast.go.
namespace TypeScript.Scanner;

// Scanner-only comment directive records.
public enum CommentDirectiveKind { Unknown, ExpectError, Ignore }
public readonly record struct CommentDirective(TextRange Loc, CommentDirectiveKind Kind);
