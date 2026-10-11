// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/module_specifier.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct ModuleSpecifier
{
    private readonly ByteStringValue value;
    internal ModuleSpecifier(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }
    internal ModuleSpecifier(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static ModuleSpecifier FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }
    public bool Equals(ModuleSpecifier other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static ModuleSpecifier FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.AsSpan().IndexOfAnyExceptInRange('\0', '\u00ff') >= 0)
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }
        return new(bytes);
    }

    public static ModuleSpecifier FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
}

public readonly partial record struct ModuleSpecifier
{
    public bool IsAbsolute() => TsPath.GetRootLength(value) > 0;
    public bool IsRelative() => TsPath.PathIsRelative(value);
    public bool Contains(string substring) => Contains(ByteStringValue.FromString(substring));
    public bool Contains(ByteStringValue substring) => value.IndexOf(substring) >= 0;
    public ModuleSpecifier Resolve(params string[] parts)
    {
        var headers = Slice<ByteStringValue>.Make(parts.Length);
        for (int i = 0; i < parts.Length; i++)
        {
            headers[i] = ByteStringValue.FromString(parts[i]);
        }
        return Resolve(headers);
    }
    public ModuleSpecifier Resolve(Slice<ByteStringValue> parts) => new(TsPath.ResolvePathWithoutTrailingDirectorySeparator(value, parts));
    public ModuleSpecifier ResolveRelative(RelativePath path) => new(TsPath.ResolvePathWithoutTrailingDirectorySeparator(TsPath.CombinePaths(value, path.AsValue())));
    public ModuleSpecifier CombineRelative(RelativePath path) => new(TsPath.CombinePaths(value, path.AsValue()));
    public ModuleSpecifier RemoveFileExtension() => new(TsPath.RemoveFileExtension(value));
}
public static partial class TsPath
{
    public static ModuleSpecifier ToModuleSpecifier(string value) => ModuleSpecifier.FromGoString(value);
    public static ModuleSpecifier ToModuleSpecifier(ByteStringValue value) => ModuleSpecifier.FromValue(value);
}
