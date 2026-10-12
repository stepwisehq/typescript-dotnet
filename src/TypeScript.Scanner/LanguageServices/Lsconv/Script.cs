// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/lsconv/converters.go:29-48 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.Lsconv;

// Go string headers are borrowed ByteStringValue headers here. Original text and
// virtual text retain their distinct owners; mapping never substitutes one for the other.
internal interface IScript
{
    RootedFilePath FileName();
    RootedFilePath OriginalFileName();
    ByteStringValue TextValue();
    TypeScript.SpanMap.SpanMap? SpanMap();
    ByteStringValue OriginalTextValue();
}

internal readonly record struct MappedSpan<T>(T Script, TypeScript.SpanMap.MappedSpan Mapping) where T : IScript
{
    internal TextRange Span => Mapping.Span;
    internal TypeScript.SpanMap.Fidelity Fidelity => Mapping.Fidelity;
}

internal readonly record struct MappedPosition<T>(T Script, TypeScript.SpanMap.MappedPosition Mapping) where T : IScript
{
    internal int Position => Mapping.Position;
    internal TypeScript.SpanMap.Fidelity Fidelity => Mapping.Fidelity;
}
