// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/file_spec.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct FileSpec
{
    private readonly string? value;
    internal FileSpec(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static FileSpec FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static FileSpec FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
}

/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct PathPattern
{
    private readonly string? value;
    internal PathPattern(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static PathPattern FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static PathPattern FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
}

public static partial class TsPath
{
    public static FileSpec ToFileSpec(string value) => FileSpec.FromGoString(value);
    public static PathPattern ToPathPattern(string value) => PathPattern.FromGoString(value);
}
