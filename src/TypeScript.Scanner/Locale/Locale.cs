// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/locale/locale.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Value representation stores canonical bytes; x/text Parse/canonicalization is separately mapped.
namespace TypeScript.Locale;
public readonly record struct Locale
{
    private readonly string? tag;
    internal Locale(string canonicalTag) => tag = canonicalTag is "" or "und" ? null : canonicalTag;
    public string String() => tag ?? "";
    public override string ToString() => String();
}

public static partial class Locales
{
    public static readonly Locale Default;
    public static (Locale Locale, bool Ok) Parse(string bytes)
    {
        var(tag, error) = XText.PublicLanguage.Parse(bytes);
        return (new Locale(tag), error == null);
    }
}
