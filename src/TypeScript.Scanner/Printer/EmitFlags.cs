// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/emitflags.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic single-line, synthesized leading-comment and ASCII-escaping flags; general emit excluded.
namespace TypeScript.DiagnosticPrinting;
[Flags]
internal enum EmitFlags : uint
{
    None = 0,
    SingleLine = 1,
    NoLeadingComments = 1u << 7,
    NoAsciiEscaping = 1u << 17
}
