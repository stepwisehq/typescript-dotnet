// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/source_map_location.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct SourceMapLocation
{
    private readonly ByteStringValue value;
    internal SourceMapLocation(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }
    internal SourceMapLocation(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static SourceMapLocation FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }
    public bool Equals(SourceMapLocation other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static SourceMapLocation FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }
        return new(bytes);
    }

    public static SourceMapLocation FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
}

public readonly partial record struct SourceMapLocation
{
    public bool IsRelative() => TsPath.GetRootLength(value) == 0;
    public RootedDirectoryPath ResolveDirectory(RootedDirectoryPath relativeBase, RootedDirectoryPath currentDirectory)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("cannot resolve an empty source map root");
        }
        return IsRelative() ? relativeBase.ResolveDirectory(value) : TsPath.ToRootedDirectoryPath(value, currentDirectory);
    }
}

public static partial class TsPath
{
    public static SourceMapLocation ToSourceMapLocation(string path) => ToSourceMapLocation(ByteStringValue.FromString(path));
    public static SourceMapLocation ToSourceMapLocation(ByteStringValue path) => new(NormalizeSlashes(path));
}
