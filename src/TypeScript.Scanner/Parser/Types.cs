// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/parser/types.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
[Flags]
public enum ParseFlags : uint
{
    None = 0,
    Yield = 1 << 0,
    Await = 1 << 1,
    Type = 1 << 2,
    IgnoreMissingOpenBrace = 1 << 4,
    JSDoc = 1 << 5
}
