// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/pathkey.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct PathKey
{
    private readonly ByteStringValue value;
    internal PathKey(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }
    internal PathKey(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static PathKey FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }
    public bool Equals(PathKey other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static PathKey FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        // pathkey.go's string key uses raw bytes; this managed ingress guard accepts all 256 byte values.
        if (bytes.AsSpan().IndexOfAnyExceptInRange('\0', '\u00ff') >= 0)
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }
        return new(bytes);
    }

    public static PathKey FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
    public PathKey Parent() => new(TsPath.GetDirectoryPath(value));
    public string BaseName() => BaseNameValue().Materialize()!;
    public ByteStringValue BaseNameValue() => TsPath.GetBaseFileName(value);
    public PathKey RemoveTrailingDirectorySeparator() => new(TsPath.RemoveTrailingDirectorySeparator(value));
}
public static partial class TsPath
{
    public static (PathKey Key, bool Ok) TryPathKeyFromCanonical(string path) => TryPathKeyFromCanonical(ByteStringValue.FromString(path));
    public static (PathKey Key, bool Ok) TryPathKeyFromCanonical(ByteStringValue path)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }
        if (path.Length == 0)
        {
            return (default, true);
        }
        return TryRootedPathFromNormalized(path).Ok ? (new(path), true) : (default, false);
    }
    public static PathKey PathKeyFromCanonical(string path) => PathKeyFromCanonical(ByteStringValue.FromString(path));
    public static PathKey PathKeyFromCanonical(ByteStringValue path)
    {
        var (key, ok) = TryPathKeyFromCanonical(path);
        return ok ? key : throw new ArgumentException("path must be normalized");
    }
}
public static class PathKeyOperations
{
    public static PathKey PathKey(this CaseSensitivity sensitivity, RootedPath path)
    {
        var bytes = path.AsValue();
        if (bytes.StartsWith("^/~ts-uri~/"u8))
        {
            if (TsPath.GetRootLength(bytes) == bytes.Length && !TsPath.HasTrailingDirectorySeparator(bytes))
            {
                bytes = TsPath.pathConcat(bytes, ByteStringValue.FromString("/"));
            }
            return new(bytes);
        }
        return new(sensitivity.Canonicalize(bytes));
    }
    public static int ComparePaths(this CaseSensitivity sensitivity, RootedPath left, RootedPath right)
    {
        var leftText = left.AsValue();
        var rightText = right.AsValue();
        if (leftText.EqualsValue(rightText))
        {
            return 0;
        }
        if (leftText.Length == 0)
        {
            return -1;
        }
        if (rightText.Length == 0)
        {
            return 1;
        }
        if (leftText.StartsWith("^/~ts-uri~/"u8) || rightText.StartsWith("^/~ts-uri~/"u8))
        {
            return CaseSensitivity.CaseSensitive.CompareValues(CaseSensitivity.CaseSensitive.PathKey(left).AsValue(), CaseSensitivity.CaseSensitive.PathKey(right).AsValue());
        }
        int leftRootLength = TsPath.GetRootLength(leftText), rightRootLength = TsPath.GetRootLength(rightText);
        int result = CaseSensitivity.CaseInsensitive.CompareValues(leftText.Slice(0, leftRootLength), rightText.Slice(0, rightRootLength));
        return result != 0 ? result : sensitivity.CompareValues(leftText.Slice(leftRootLength), rightText.Slice(rightRootLength));
    }
    public static int CompareFilePaths(this CaseSensitivity sensitivity, RootedFilePath left, RootedFilePath right) => sensitivity.ComparePaths(left.AsPath(), right.AsPath());
    public static bool ContainsPath(this CaseSensitivity sensitivity, RootedDirectoryPath parent, RootedPath child) => sensitivity.RelativePathWithinDirectory(parent, child).Ok;
    public static bool ContainsFilePath(this CaseSensitivity sensitivity, RootedDirectoryPath parent, RootedFilePath child) => sensitivity.ContainsPath(parent, child.AsPath());
    public static (RelativePath Path, bool Ok) RelativePathWithinDirectory(this CaseSensitivity sensitivity, RootedDirectoryPath parent, RootedPath child)
    {
        var parentText = parent.AsValue();
        var childText = child.AsValue();
        if (parentText.Length == 0 || childText.Length == 0)
        {
            return (default, false);
        }
        int parentRootLength = TsPath.GetRootLength(parentText), childRootLength = TsPath.GetRootLength(childText);
        var leftRoot = parentText.Slice(0, parentRootLength);
        var rightRoot = childText.Slice(0, childRootLength);
        bool dynamic = parentText.StartsWith("^/~ts-uri~/"u8) || childText.StartsWith("^/~ts-uri~/"u8);
        if (dynamic)
        {
            leftRoot = TsPath.trimDynamicRoot(leftRoot);
            rightRoot = TsPath.trimDynamicRoot(rightRoot);
            sensitivity = CaseSensitivity.CaseSensitive;
        }
        if (!(dynamic ? leftRoot.EqualsValue(rightRoot) : UnicodeCase.EqualFold(leftRoot, rightRoot)))
        {
            return (default, false);
        }
        var (relative, ok) = sensitivity.TrimPrefix(childText.Slice(childRootLength), parentText.Slice(parentRootLength));
        if (!ok || relative.Length != 0 && !TsPath.HasTrailingDirectorySeparator(parentText) && parentText.Length != parentRootLength && relative[0] != '/')
        {
            return (default, false);
        }
        return (new(relative.StartsWith("/"u8) ? relative.Slice(1) : relative), true);
    }
}
public readonly partial record struct PathKey
{
    public string Extension() => ExtensionValue().Materialize()!;
    public ByteStringValue ExtensionValue() => TsPath.TryGetExtensionFromPath(value);
    public bool ExtensionIsOneOf(IEnumerable<string> extensions) => TsPath.FileExtensionIsOneOf(value, extensions);
    public bool HasJSFileExtension() => TsPath.HasJSFileExtension(value);
    public bool IsDynamic() => TsPath.IsDynamicFileName(value);
    public PathKey CaseInsensitiveKey() => TsPath.IsEncodedDynamicFileName(value) ? this : new(TsPath.ToFileNameLowerCaseGoBytes(value));
    public PathKey AppendCanonicalComponent(string component) => AppendCanonicalComponent(ByteStringValue.FromString(component));
    public PathKey AppendCanonicalComponent(ByteStringValue component)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("cannot append a component to an empty path key");
        }
        TsPath.validatePathComponent(component);
        return TsPath.PathKeyFromCanonical(TsPath.pathConcat(value, TsPath.HasTrailingDirectorySeparator(value) ? default : ByteStringValue.FromString("/"), component));
    }
    public PathKey AppendCanonicalSuffix(string suffix) => AppendCanonicalSuffix(ByteStringValue.FromString(suffix));
    public PathKey AppendCanonicalSuffix(ByteStringValue suffix)
    {
        if (value.Length == 0 && (suffix.IsNull || suffix.Length != 0) || suffix.Contains('/') || suffix.Contains('\\'))
        {
            throw new ArgumentException("invalid path suffix");
        }
        return TsPath.PathKeyFromCanonical(TsPath.pathConcat(value, suffix));
    }
    public (PathKey Before, PathKey Through, bool Ok) SplitAtCanonicalComponent(string component) => SplitAtCanonicalComponent(ByteStringValue.FromString(component));
    public (PathKey Before, PathKey Through, bool Ok) SplitAtCanonicalComponent(ByteStringValue component)
    {
        var (before, through, ok) = new RootedFilePath(value).SplitAtComponent(component);
        return (new(before.AsValue()), new(through.AsValue()), ok);
    }
    public bool ContainsLowercaseDirectorySequence(string sequence) => ContainsLowercaseDirectorySequence(ByteStringValue.FromString(sequence));
    public bool ContainsLowercaseDirectorySequence(ByteStringValue sequence) => value.IndexOf(sequence) >= 0;
    public bool ContainsPath(PathKey child)
    {
        var parent = value;
        var text = child.value;
        if (parent.Length == 0)
        {
            return false;
        }
        int parentRootLength = TsPath.GetRootLength(parent), childRootLength = TsPath.GetRootLength(text);
        var parentRoot = parent.Slice(0, parentRootLength);
        var childRoot = text.Slice(0, childRootLength);
        bool dynamic = TsPath.IsEncodedDynamicFileName(parent) || TsPath.IsEncodedDynamicFileName(text);
        if (dynamic)
        {
            parentRoot = TsPath.trimDynamicRoot(parentRoot);
            childRoot = TsPath.trimDynamicRoot(childRoot);
        }
        if (!(dynamic ? parentRoot.EqualsValue(childRoot) : UnicodeCase.EqualFold(parentRoot, childRoot)))
        {
            return false;
        }
        var rest = parent.Slice(parentRootLength);
        var childRest = text.Slice(childRootLength);
        return rest.EqualsValue(childRest) || childRest.Length > rest.Length && childRest.StartsWith(rest) && (rest.Length == 0 || rest[rest.Length - 1] == '/' || childRest[rest.Length] == '/');
    }
    public (T Result, bool Ok) ForEachAncestorDirectory<T>(Func<PathKey, (T Result, bool Stop)> callback)
    {
        var current = this;
        for (;;)
        {
            var (result, stop) = callback(current);
            if (stop)
            {
                return (result, true);
            }
            var parent = current.Parent();
            if (parent == current)
            {
                return (typeof(T) == typeof(string) ? (T)(object)"" : default!, false);
            }
            current = parent;
        }
    }
}
