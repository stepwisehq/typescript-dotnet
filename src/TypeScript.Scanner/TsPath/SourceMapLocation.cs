// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/source_map_location.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct SourceMapLocation
{
    private readonly string? value;
    internal SourceMapLocation(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static SourceMapLocation FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static SourceMapLocation FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
}

public readonly partial record struct SourceMapLocation
{
    public bool IsRelative() => TsPath.GetRootLength(AsString()) == 0;
    public RootedDirectoryPath ResolveDirectory(RootedDirectoryPath relativeBase, RootedDirectoryPath currentDirectory)
    {
        if (AsString() == "")
            throw new ArgumentException("cannot resolve an empty source map root");
        return IsRelative() ? relativeBase.ResolveDirectory(AsString()) : TsPath.ToRootedDirectoryPath(AsString(), currentDirectory);
    }
}

public static partial class TsPath
{
    public static SourceMapLocation ToSourceMapLocation(string path) => new(NormalizeSlashes(path));
}
