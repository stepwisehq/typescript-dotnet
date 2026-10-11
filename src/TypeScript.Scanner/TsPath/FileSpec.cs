// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/file_spec.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct FileSpec
{
    private readonly ByteStringValue value;
    internal FileSpec(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }
    internal FileSpec(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static FileSpec FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }
    public bool Equals(FileSpec other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static FileSpec FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }
        return new(bytes);
    }

    public static FileSpec FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
}

/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct PathPattern
{
    private readonly ByteStringValue value;
    internal PathPattern(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }
    internal PathPattern(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static PathPattern FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }
    public bool Equals(PathPattern other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static PathPattern FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }
        return new(bytes);
    }

    public static PathPattern FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
}

public static partial class TsPath
{
    public static FileSpec ToFileSpec(ByteStringValue value) => FileSpec.FromValue(value);
    public static FileSpec ToFileSpec(string value) => FileSpec.FromGoString(value);
    public static PathPattern ToPathPattern(ByteStringValue value) => PathPattern.FromValue(value);
    public static PathPattern ToPathPattern(string value) => PathPattern.FromGoString(value);
}
