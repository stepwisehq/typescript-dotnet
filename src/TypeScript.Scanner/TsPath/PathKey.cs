// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/pathkey.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct PathKey
{
    private readonly string? value;
    internal PathKey(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static PathKey FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static PathKey FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
    public PathKey Parent() => new(TsPath.GetDirectoryPath(AsString()));
    public string BaseName() => TsPath.GetBaseFileName(AsString());
    public PathKey RemoveTrailingDirectorySeparator() => new(TsPath.RemoveTrailingDirectorySeparator(AsString()));
}

public static partial class TsPath
{
    public static (PathKey Key, bool Ok) TryPathKeyFromCanonical(string path)
    {
        if (path == "")
            return (default, true);
        return TryRootedPathFromNormalized(path).Ok ? (new(path), true) : (default, false);
    }

    public static PathKey PathKeyFromCanonical(string path)
    {
        var(key, ok) = TryPathKeyFromCanonical(path);
        return ok ? key : throw new ArgumentException("path must be normalized");
    }
}

public static class PathKeyOperations
{
    public static PathKey PathKey(this CaseSensitivity sensitivity, RootedPath path)
    {
        string bytes = path.AsString();
        if (bytes.StartsWith("^/~ts-uri~/", StringComparison.Ordinal))
        {
            if (TsPath.GetRootLength(bytes) == bytes.Length && !TsPath.HasTrailingDirectorySeparator(bytes))
                bytes += "/";
            return new(bytes);
        }

        return new(sensitivity.Canonicalize(bytes));
    }

    public static int ComparePaths(this CaseSensitivity sensitivity, RootedPath left, RootedPath right)
    {
        string a = left.AsString(), b = right.AsString();
        if (a == b)
            return 0;
        if (a == "")
            return -1;
        if (b == "")
            return 1;
        if (a.StartsWith("^/~ts-uri~/", StringComparison.Ordinal) || b.StartsWith("^/~ts-uri~/", StringComparison.Ordinal))
            return Math.Sign(string.CompareOrdinal(CaseSensitivity.CaseSensitive.PathKey(left).AsString(), CaseSensitivity.CaseSensitive.PathKey(right).AsString()));
        int aRoot = TsPath.GetRootLength(a), bRoot = TsPath.GetRootLength(b);
        int result = CaseSensitivity.CaseInsensitive.CompareStrings(a[..aRoot], b[..bRoot]);
        return result != 0 ? result : sensitivity.CompareStrings(a[aRoot..], b[bRoot..]);
    }

    public static int CompareFilePaths(this CaseSensitivity sensitivity, RootedFilePath left, RootedFilePath right) => sensitivity.ComparePaths(left.AsPath(), right.AsPath());
    public static bool ContainsPath(this CaseSensitivity sensitivity, RootedDirectoryPath parent, RootedPath child) => sensitivity.RelativePathWithinDirectory(parent, child).Ok;
    public static bool ContainsFilePath(this CaseSensitivity sensitivity, RootedDirectoryPath parent, RootedFilePath child) => sensitivity.ContainsPath(parent, child.AsPath());
    public static (RelativePath Path, bool Ok) RelativePathWithinDirectory(this CaseSensitivity sensitivity, RootedDirectoryPath parent, RootedPath child)
    {
        string a = parent.AsString(), b = child.AsString();
        if (a == "" || b == "")
            return (default, false);
        int aRoot = TsPath.GetRootLength(a), bRoot = TsPath.GetRootLength(b);
        string leftRoot = a[..aRoot], rightRoot = b[..bRoot];
        bool dynamic = a.StartsWith("^/~ts-uri~/", StringComparison.Ordinal) || b.StartsWith("^/~ts-uri~/", StringComparison.Ordinal);
        if (dynamic)
        {
            leftRoot = leftRoot.TrimEnd('/');
            rightRoot = rightRoot.TrimEnd('/');
            sensitivity = CaseSensitivity.CaseSensitive;
        }

        if (!(dynamic ? leftRoot == rightRoot : UnicodeCase.EqualFold(leftRoot, rightRoot)))
            return (default, false);
        var(relative, ok) = sensitivity.TrimPrefix(b[bRoot..], a[aRoot..]);
        if (!ok || relative != "" && !TsPath.HasTrailingDirectorySeparator(a) && a.Length != aRoot && relative[0] != '/')
            return (default, false);
        return (new(relative.StartsWith('/') ? relative[1..] : relative), true);
    }
}

public readonly partial record struct PathKey
{
    public string Extension() => TsPath.TryGetExtensionFromPath(AsString());
    public bool ExtensionIsOneOf(IEnumerable<string> extensions) => TsPath.FileExtensionIsOneOf(AsString(), extensions);
    public bool HasJSFileExtension() => TsPath.HasJSFileExtension(AsString());
    public bool IsDynamic() => TsPath.IsDynamicFileName(AsString());
    public PathKey CaseInsensitiveKey() => TsPath.IsEncodedDynamicFileName(AsString()) ? this : new(TsPath.ToFileNameLowerCaseGoBytes(AsString()));
    public PathKey AppendCanonicalComponent(string component)
    {
        if (AsString() == "")
            throw new ArgumentException("cannot append a component to an empty path key");
        TsPath.validatePathComponent(component);
        return TsPath.PathKeyFromCanonical(AsString() + (TsPath.HasTrailingDirectorySeparator(AsString()) ? "" : "/") + component);
    }

    public PathKey AppendCanonicalSuffix(string suffix)
    {
        if (AsString() == "" && suffix != "" || suffix.Contains('/') || suffix.Contains('\\'))
            throw new ArgumentException("invalid path suffix");
        return TsPath.PathKeyFromCanonical(AsString() + suffix);
    }

    public (PathKey Before, PathKey Through, bool Ok) SplitAtCanonicalComponent(string component)
    {
        var(before, through, ok) = new RootedFilePath(AsString()).SplitAtComponent(component);
        return (new(before.AsString()), new(through.AsString()), ok);
    }

    public bool ContainsLowercaseDirectorySequence(string sequence) => AsString().Contains(sequence, StringComparison.Ordinal);
    public bool ContainsPath(PathKey child)
    {
        string parent = AsString(), text = child.AsString();
        if (parent == "")
            return false;
        int parentRoot = TsPath.GetRootLength(parent), childRoot = TsPath.GetRootLength(text);
        string a = parent[..parentRoot], b = text[..childRoot];
        bool dynamic = TsPath.IsEncodedDynamicFileName(parent) || TsPath.IsEncodedDynamicFileName(text);
        if (dynamic)
        {
            a = a.TrimEnd('/');
            b = b.TrimEnd('/');
        }

        if (!(dynamic ? a == b : UnicodeCase.EqualFold(a, b)))
            return false;
        string rest = parent[parentRoot..], childRest = text[childRoot..];
        return rest == childRest || childRest.Length > rest.Length && childRest.StartsWith(rest, StringComparison.Ordinal) && (rest == "" || rest[^1] == '/' || childRest[rest.Length] == '/');
    }

    public (T Result, bool Ok) ForEachAncestorDirectory<T>(Func<PathKey, (T Result, bool Stop)> callback)
    {
        var current = this;
        for (;;)
        {
            var(result, stop) = callback(current);
            if (stop)
                return (result, true);
            var parent = current.Parent();
            if (parent == current)
                return (typeof(T) == typeof(string) ? (T)(object)"" : default!, false);
            current = parent;
        }
    }
}
