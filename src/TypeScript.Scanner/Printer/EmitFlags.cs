// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.DiagnosticPrinting;
[Flags]
internal enum EmitFlags : uint
{
    None = 0,
    SingleLine = 1u << 0,
    MultiLine = 1u << 1,
    NoLeadingSourceMap = 1u << 2,
    NoTrailingSourceMap = 1u << 3,
    NoNestedSourceMaps = 1u << 4,
    NoTokenLeadingSourceMaps = 1u << 5,
    NoTokenTrailingSourceMaps = 1u << 6,
    NoLeadingComments = 1u << 7,
    NoTrailingComments = 1u << 8,
    NoNestedComments = 1u << 9,
    HelperName = 1u << 10,
    ExportName = 1u << 11,
    LocalName = 1u << 12,
    Indented = 1u << 13,
    NoIndentation = 1u << 14,
    ReuseTempVariableScope = 1u << 15,
    CustomPrologue = 1u << 16,
    NoAsciiEscaping = 1u << 17,
    ExternalHelpers = 1u << 18,
    StartOnNewLine = 1u << 19,
    IndirectCall = 1u << 20,
    AsyncFunctionBody = 1u << 21,
    NoLexicalArguments = 1u << 22,
    TransformPrivateStaticElements = 1u << 23,
    NoLexicalThis = 1u << 24,
    NoSourceMap = NoLeadingSourceMap | NoTrailingSourceMap,
    NoTokenSourceMaps = NoTokenLeadingSourceMaps | NoTokenTrailingSourceMaps,
    NoComments = NoLeadingComments | NoTrailingComments
}
