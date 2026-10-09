// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/module_specifier.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct ModuleSpecifier
{
    private readonly string? value;
    internal ModuleSpecifier(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static ModuleSpecifier FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static ModuleSpecifier FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
}

public readonly partial record struct ModuleSpecifier
{
    public bool IsAbsolute() => TsPath.GetRootLength(AsString()) > 0;
    public bool IsRelative() => TsPath.PathIsRelative(AsString());
    public bool Contains(string substring) => AsString().Contains(substring, StringComparison.Ordinal);
    public ModuleSpecifier Resolve(params string[] parts) => new(TsPath.ResolvePathWithoutTrailingDirectorySeparator(AsString(), parts));
    public ModuleSpecifier ResolveRelative(RelativePath path) => Resolve(path.AsString());
    public ModuleSpecifier CombineRelative(RelativePath path) => new(TsPath.CombinePaths(AsString(), path.AsString()));
    public ModuleSpecifier RemoveFileExtension() => new(TsPath.RemoveFileExtension(AsString()));
}

public static partial class TsPath
{
    public static ModuleSpecifier ToModuleSpecifier(string value) => ModuleSpecifier.FromGoString(value);
}
