// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/relative_path.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct RelativePath
{
    private readonly ByteStringValue value;
    internal RelativePath(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }
    internal RelativePath(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static RelativePath FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }
    public bool Equals(RelativePath other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static RelativePath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.AsSpan().IndexOfAnyExceptInRange('\0', '\u00ff') >= 0)
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }
        return new(bytes);
    }

    public static RelativePath FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
}

public readonly partial record struct RelativePath
{
    public ModuleSpecifier AsModuleSpecifier() => new(TsPath.EnsurePathIsNonModuleName(value));
    public RelativePath ChangeExtension(string extension) => ChangeExtension(ByteStringValue.FromString(extension));
    public RelativePath ChangeExtension(ByteStringValue extension)
    {
        TsPath.validateFileExtension(extension);
        return new(TsPath.ChangeExtension(value, extension));
    }
    public string BaseName() => BaseNameValue().Materialize()!;
    public ByteStringValue BaseNameValue() => TsPath.GetBaseFileName(value);
    public bool IsParentRelative() => value.EqualsBytes(".."u8) || value.StartsWith("../"u8);
    public bool HasTrailingDirectorySeparator() => TsPath.HasTrailingDirectorySeparator(value);
    public RelativePath WithoutTrailingDirectorySeparator() => HasTrailingDirectorySeparator() ? new(TsPath.RemoveTrailingDirectorySeparator(value)) : this;
    public RelativePath WithTrailingDirectorySeparator() => value.Length == 0 || HasTrailingDirectorySeparator() ? this : new(TsPath.pathConcat(value, ByteStringValue.FromString("/")));
    internal bool requiresResolution() => IsParentRelative() || HasTrailingDirectorySeparator();
}
public static partial class TsPath
{
    public static RelativePath ToRelativePath(string path) => ToRelativePath(ByteStringValue.FromString(path));
    public static RelativePath ToRelativePath(ByteStringValue path)
    {
        if (GetEncodedRootLength(path) != 0)
        {
            throw new ArgumentException("relative path must not be rooted");
        }
        return new(NormalizePath(path));
    }
    public static RelativePath RelativePathFromNormalized(string path) => RelativePathFromNormalized(ByteStringValue.FromString(path));
    public static RelativePath RelativePathFromNormalized(ByteStringValue path)
    {
        if (GetEncodedRootLength(path) != 0 || !NormalizePath(path).EqualsValue(path))
        {
            throw new ArgumentException("relative path must be relative and normalized: " + path.Materialize());
        }
        return new(path);
    }
    internal static void validateFileExtension(string extension) => validateFileExtension(ByteStringValue.FromString(extension));
    internal static void validateFileExtension(ByteStringValue extension)
    {
        if (extension.Contains('/') || extension.Contains('\\'))
        {
            throw new ArgumentException("file extension must not contain a directory separator");
        }
    }
    internal static ByteStringValue relativePathFromNormalizedPaths(ByteStringValue from, ByteStringValue to, CaseSensitivity sensitivity)
    {
        var first = pathComponents(from, GetRootLength(from));
        var second = pathComponents(to, GetRootLength(to));
        if (IsEncodedDynamicFileName(from) || IsEncodedDynamicFileName(to))
        {
            first[0] = trimDynamicRoot(first[0]);
            second[0] = trimDynamicRoot(second[0]);
            if (!first[0].EqualsValue(second[0]))
            {
                return to;
            }
            sensitivity = CaseSensitivity.CaseSensitive;
        }
        return GetPathFromPathComponents(getPathComponentsRelativeTo(first, second, sensitivity));
    }
}
public static class RelativePathOperations
{
    public static RelativePath CanonicalRelativePath(this CaseSensitivity sensitivity, RelativePath path) => new(sensitivity.Canonicalize(path.AsValue()));
    public static (RelativePath Path, bool Ok) RelativePathFromDirectory(this CaseSensitivity sensitivity, RootedDirectoryPath directory, RootedFilePath file) => sensitivity.RelativePathFromPath(directory, file.AsPath());
    public static (RelativePath Path, bool Ok) RelativePathFromPath(this CaseSensitivity sensitivity, RootedDirectoryPath directory, RootedPath path)
    {
        var result = TsPath.relativePathFromNormalizedPaths(directory.AsValue(), path.AsValue(), sensitivity);
        return TsPath.GetEncodedRootLength(result) != 0 ? (default, false) : (new(result), true);
    }
    public static (RelativePath Path, bool Ok) RelativePathFromFile(this CaseSensitivity sensitivity, RootedFilePath from, RootedFilePath to) => sensitivity.RelativePathFromFileToPath(from, to.AsPath());
    public static (RelativePath Path, bool Ok) RelativePathFromFileToPath(this CaseSensitivity sensitivity, RootedFilePath from, RootedPath to) => sensitivity.RelativePathFromPath(from.Directory(), to);
    public static RelativePath RelativePathFromRelativeDirectory(this CaseSensitivity sensitivity, RelativePath directory, RelativePath file) => new(TsPath.relativePathFromNormalizedPaths(directory.AsValue(), file.AsValue(), sensitivity));
}
