// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/rooted_path.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct RootedPath
{
    private readonly string? value;
    internal RootedPath(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static RootedPath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static RootedPath FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
    public int Compare(RootedPath other) => Math.Sign(string.CompareOrdinal(AsString(), other.AsString()));
    public string BaseName() => TsPath.GetBaseFileName(AsString());
    public RootedDirectoryPath Directory()
    {
        string text = AsString();
        int root = TsPath.GetRootLength(text);
        if (root == text.Length)
            return new(text);
        text = TsPath.RemoveTrailingDirectorySeparator(text);
        return new(text[..Math.Max(root, text.LastIndexOf('/'))]);
    }

    public (RootedDirectoryPath Root, string Relative) RootAndRelativePath()
    {
        string text = AsString();
        int root = TsPath.GetRootLength(text);
        return (new(text[..root]), text[root..]);
    }
}

/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct RootedFilePath
{
    private readonly string? value;
    internal RootedFilePath(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static RootedFilePath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static RootedFilePath FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
    public RootedPath AsPath() => new(AsString());
    public RootedDirectoryPath Directory() => AsPath().Directory();
    public string BaseName() => AsPath().BaseName();
    public int Compare(RootedFilePath other) => AsPath().Compare(other.AsPath());
    public (RootedDirectoryPath Root, string Relative) RootAndRelativePath() => AsPath().RootAndRelativePath();
    public RootedFilePath AppendSuffix(string suffix)
    {
        if (AsString() == "" && suffix != "" || suffix.Contains('/') || suffix.Contains('\\'))
            throw new ArgumentException("invalid file name suffix");
        string result = AsString() + suffix;
        return result == "" ? default : TsPath.RootedFilePathFromNormalized(result);
    }

    public bool ExtensionIs(string extension) => AsString().Length > extension.Length && AsString().EndsWith(extension, StringComparison.Ordinal);
    public bool IsDeclarationFile() => TsPath.IsDeclarationFileName(AsString());
}

/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct RootedDirectoryPath
{
    private readonly string? value;
    internal RootedDirectoryPath(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static RootedDirectoryPath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static RootedDirectoryPath FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
    public RootedPath AsPath() => new(AsString());
    public string BaseName() => AsPath().BaseName();
    public RootedDirectoryPath Directory() => AsPath().Directory();
    public int Compare(RootedDirectoryPath other) => AsPath().Compare(other.AsPath());
    public RootedFilePath ResolveFile(string path)
    {
        if (AsString() == "")
            throw new ArgumentException("cannot resolve from an empty directory name");
        return path == "" ? new(AsString()) : TsPath.ToRootedFilePath(path, this);
    }

    public RootedDirectoryPath ResolveDirectory(string path)
    {
        if (AsString() == "")
            throw new ArgumentException("cannot resolve from an empty directory name");
        return path == "" ? this : TsPath.ToRootedDirectoryPath(path, this);
    }

    public (T? Result, bool Ok) ForEachAncestorDirectory<T>(Func<RootedDirectoryPath, (T Result, bool Stop)> callback)
    {
        RootedDirectoryPath current = this;
        while (true)
        {
            var(result, stop) = callback(current);
            if (stop)
                return (result, true);
            var parent = current.Directory();
            if (parent == current)
                return (typeof(T) == typeof(string) ? (T)(object)"" : default, false);
            current = parent;
        }
    }
}

public static partial class TsPath
{
    public static RootedPath ToRootedPath(string bytes, RootedDirectoryPath currentDirectory)
    {
        if (bytes == "")
            throw new ArgumentException("path must not be empty");
        if (hasRootedURLSuffix(bytes))
            throw new ArgumentException("path must not contain a URL query or fragment");
        if (GetEncodedRootLength(bytes) == 0 && GetEncodedRootLength(currentDirectory.AsString()) < 0 && currentDirectory.AsString().Contains("://", StringComparison.Ordinal) && bytes.IndexOfAny(['?', '#']) >= 0)
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        string normalized = GetNormalizedAbsolutePath(bytes, currentDirectory.AsString());
        if (GetEncodedRootLength(normalized) == 0 || hasRootedURLSuffix(normalized))
            throw new ArgumentException("path must be rooted");
        return new(ensureRootedPathRootSeparator(normalized));
    }

    private static bool hasRootedURLSuffix(string path)
    {
        if (GetEncodedRootLength(path) >= 0)
            return false;
        int scheme = path.IndexOf("://", StringComparison.Ordinal);
        return scheme >= 0 && path[(scheme + 3)..].IndexOfAny(['?', '#']) >= 0;
    }

    private static string ensureRootedPathRootSeparator(string path) => GetRootLength(path) == path.Length && !HasTrailingDirectorySeparator(path) ? path + "/" : path;
    public static (RootedPath Path, bool Ok) TryRootedPathFromAbsolute(string path)
    {
        if (hasRootedURLSuffix(path) || GetEncodedRootLength(path) == 0)
            return (default, false);
        return (new(ensureRootedPathRootSeparator(GetNormalizedAbsolutePath(path, ""))), true);
    }

    public static RootedPath RootedPathFromAbsolute(string path)
    {
        var(result, ok) = TryRootedPathFromAbsolute(path);
        return ok ? result : throw new ArgumentException("path must be absolute");
    }

    public static (RootedPath Path, bool Ok) TryRootedPathFromNormalized(string path)
    {
        if (hasRootedURLSuffix(path))
            return (default, false);
        int root = GetEncodedRootLength(path);
        if (root < 0)
            root = ~root;
        if (path == "" || root == 0 || path.Contains('\\') || root < path.Length && path[root] == '/' || hasRelativePathSegment(path[root..]) || path.Length == root && !HasTrailingDirectorySeparator(path) || path.Length > root && HasTrailingDirectorySeparator(path))
            return (default, false);
        return (new(path), true);
    }

    public static RootedPath RootedPathFromNormalized(string path)
    {
        var(result, ok) = TryRootedPathFromNormalized(path);
        return ok ? result : throw new ArgumentException("path must be rooted and normalized: " + path);
    }

    public static RootedFilePath ToRootedFilePath(string path, RootedDirectoryPath directory) => new(ToRootedPath(path, directory).AsString());
    public static RootedDirectoryPath ToRootedDirectoryPath(string path, RootedDirectoryPath directory) => new(ToRootedPath(path, directory).AsString());
    public static RootedFilePath RootedFilePathFromAbsolute(string path) => new(RootedPathFromAbsolute(path).AsString());
    public static RootedDirectoryPath RootedDirectoryPathFromAbsolute(string path) => new(RootedPathFromAbsolute(path).AsString());
    public static RootedFilePath RootedFilePathFromNormalized(string path) => new(RootedPathFromNormalized(path).AsString());
    public static RootedDirectoryPath RootedDirectoryPathFromNormalized(string path) => new(RootedPathFromNormalized(path).AsString());
    public static RootedFilePath RootedFilePathFromPath(RootedPath path) => new(path.AsString());
    public static RootedDirectoryPath RootedDirectoryPathFromPath(RootedPath path) => new(path.AsString());
}

/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct FileNameStem
{
    private readonly string? value;
    internal FileNameStem(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static FileNameStem FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static FileNameStem FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
}

public readonly partial record struct RootedPath
{
    public bool IsDynamic() => TsPath.IsDynamicFileName(AsString());
    public (RelativePath Path, bool Ok) RelativeTo(RootedDirectoryPath directory) => CaseSensitivity.CaseSensitive.RelativePathWithinDirectory(directory, this);
}

public readonly partial record struct RootedFilePath
{
    public ModuleSpecifier AsModuleSpecifier() => new(AsString());
    public string WithoutRoot() => AsString()[TsPath.GetRootLength(AsString())..];
    public RootedDirectoryPath DirectoryBefore(int index)
    {
        string text = AsString();
        if (index < RootLength() || index > text.Length || index < text.Length && text[index] is not ('/' or '\\'))
            throw new ArgumentException("directory boundary must be at a path separator");
        return new(text[..index]);
    }

    public string SuffixAfterSeparator(int index)
    {
        string text = AsString();
        if (index < 0 || index >= text.Length || text[index] is not ('/' or '\\'))
            throw new ArgumentException("suffix boundary must be at a path separator");
        return text[(index + 1)..];
    }

    public FileNameStem AsStem() => new(AsString());
    public FileNameStem RemoveFileExtension() => new(TsPath.RemoveFileExtension(AsString()));
    public FileNameStem RemoveExtension(string extension)
    {
        TsPath.validateFileExtension(extension);
        if (!AsString().EndsWith(extension, StringComparison.Ordinal))
            throw new ArgumentException("file name does not have extension: " + extension);
        return new(TsPath.RemoveExtension(AsString(), extension));
    }

    public RootedFilePath ChangeExtension(string extension)
    {
        TsPath.validateFileExtension(extension);
        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeExtension(AsString(), extension));
    }

    public RootedFilePath ChangeFullExtension(string extension)
    {
        TsPath.validateFileExtension(extension);
        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeFullExtension(AsString(), extension));
    }

    public RootedFilePath ChangeAnyExtension(string extension, IEnumerable<string> extensions, CaseSensitivity sensitivity)
    {
        TsPath.validateFileExtension(extension);
        foreach (string candidate in extensions)
            TsPath.validateFileExtension(candidate);
        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeAnyExtension(AsString(), extension, extensions, sensitivity));
    }

    public bool ExtensionIsOneOf(IEnumerable<string> extensions) => TsPath.FileExtensionIsOneOf(AsString(), extensions);
    public string Extension() => TsPath.TryGetExtensionFromPath(AsString());
    public string AnyExtension(IEnumerable<string>? extensions, CaseSensitivity sensitivity) => TsPath.GetAnyExtensionFromPath(AsString(), extensions, sensitivity);
    public string LongestExtension(IEnumerable<string>? extensions, CaseSensitivity sensitivity) => TsPath.GetLongestExtensionFromPath(AsString(), extensions, sensitivity);
    public bool HasExtension() => BaseName().Contains('.');
    public bool HasImplementationTSFileExtension() => TsPath.HasImplementationTSFileExtension(AsString());
    public bool HasTSFileExtension() => TsPath.HasTSFileExtension(AsString());
    public string TryExtractTSExtension() => TsPath.TryExtractTSExtension(AsString());
    public bool HasJSONFileExtension() => TsPath.HasJSONFileExtension(AsString());
    public string DeclarationFileExtension() => TsPath.GetDeclarationFileExtension(AsString());
    public string DeclarationEmitExtension() => TsPath.GetDeclarationEmitExtensionForPath(AsString());
    public List<string> PossibleOriginalInputExtensions() => TsPath.GetPossibleOriginalInputExtensionForExtension(AsString());
    public bool HasJSFileExtension() => TsPath.HasJSFileExtension(AsString());
    public bool IsDynamic() => TsPath.IsDynamicFileName(AsString());
    public List<string> Components() => TsPath.GetPathComponents(AsString());
    public int RootLength() => TsPath.GetRootLength(AsString());
    public (RelativePath Path, bool Ok) RelativeTo(RootedDirectoryPath directory) => AsPath().RelativeTo(directory);
    public bool ContainsLowercaseDirectorySequence(string sequence) => AsString().Contains(sequence, StringComparison.Ordinal);
    public int DirectorySeparatorCount() => AsString().Count(ch => ch == '/');
    public (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitAtComponent(string component)
    {
        TsPath.validatePathComponent(component);
        string text = AsString(), needle = "/" + component;
        int root = RootLength();
        if (root == 0)
            return (default, default, false);
        for (int offset = root - 1;;)
        {
            int index = text.IndexOf(needle, offset, StringComparison.Ordinal);
            if (index < 0)
                return (default, default, false);
            int end = index + needle.Length;
            if (end == text.Length || text[end] == '/')
                return (new(text[..Math.Max(index, root)]), new(text[..end]), true);
            offset = end;
        }
    }

    public (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitAtLastComponent(string component)
    {
        TsPath.validatePathComponent(component);
        string text = AsString(), needle = "/" + component;
        for (int end = text.Length; end > 0;)
        {
            int index = text[..end].LastIndexOf(needle, StringComparison.Ordinal);
            if (index < 0)
                return (default, default, false);
            int componentEnd = index + needle.Length;
            if (index >= RootLength() - 1 && (componentEnd == text.Length || text[componentEnd] == '/'))
                return (new(text[..Math.Max(index, RootLength())]), new(text[..componentEnd]), true);
            end = index;
        }

        return (default, default, false);
    }
}

public readonly partial record struct FileNameStem
{
    public RootedFilePath AppendSuffix(string suffix)
    {
        if (AsString() == "" && suffix != "" || suffix.Contains('/') || suffix.Contains('\\'))
            throw new ArgumentException("invalid file name suffix");
        string result = AsString() + suffix;
        return result == "" ? default : TsPath.RootedFilePathFromNormalized(result);
    }
}

public readonly partial record struct RootedDirectoryPath
{
    public List<string> Components() => TsPath.GetPathComponents(AsString());
    public bool ContainsLowercaseDirectorySequence(string sequence) => AsString().Contains(sequence, StringComparison.Ordinal);
    public RootedFilePath ResolveRelativeFile(RelativePath path)
    {
        if (AsString() == "")
            throw new ArgumentException("cannot resolve from an empty directory name");
        if (path.AsString() == "")
            return new(AsString());
        if (TsPath.hasURLRoot(AsString()) && path.AsString().IndexOfAny(['?', '#']) >= 0)
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        return path.requiresResolution() ? TsPath.ToRootedFilePath(path.AsString(), this) : TsPath.rootedFilePathFromResolved(TsPath.appendPathToDirectory(this, path.AsString()));
    }

    public RootedDirectoryPath ResolveRelativeDirectory(RelativePath path) => new(ResolveRelativeFile(path).AsString());
    public RootedFilePath ResolveFileFromNormalizedRelative(string path)
    {
        if (AsString() == "")
            throw new ArgumentException("cannot resolve from an empty directory path");
        if (path == "")
            throw new ArgumentException("path must not be empty");
        if (TsPath.hasURLRoot(AsString()) && TsPath.GetEncodedRootLength(path) == 0 && path.IndexOfAny(['?', '#']) >= 0)
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        if (path[0] is '/' or '\\' || TsPath.NormalizeSlashes(path) != path || path.Split('/').Any(part => part is "." or "..") || TsPath.HasTrailingDirectorySeparator(path))
            throw new ArgumentException("path must be relative and normalized: " + path);
        return TsPath.rootedFilePathFromResolved(TsPath.appendPathToDirectory(this, path));
    }
}

public static partial class TsPath
{
    internal static bool hasURLRoot(string path) => GetEncodedRootLength(path) < 0 && path.Contains("://", StringComparison.Ordinal);
    internal static RootedFilePath rootedFilePathFromResolved(string path)
    {
        if (hasRootedURLSuffix(path))
            throw new ArgumentException("path must not contain a URL query or fragment");
        return new(path);
    }

    internal static RootedFilePath rootedFilePathFromExtensionMutation(string path)
    {
        string name = GetBaseFileName(path);
        if (name is "." or ".." || HasTrailingDirectorySeparator(path) && path.Length > GetRootLength(path))
            throw new ArgumentException("file extension change must preserve path normalization");
        return rootedFilePathFromResolved(path);
    }

    internal static void validatePathComponent(string component)
    {
        if (component == "" || component.Contains('/') || component.Contains('\\') || component is "." or "..")
            throw new ArgumentException("invalid path component");
    }

    internal static string appendPathToDirectory(RootedDirectoryPath directory, string path) => directory.AsString() + (HasTrailingDirectorySeparator(directory.AsString()) ? "" : "/") + path;
    public static (RootedFilePath Path, bool Ok) TryRootedFilePathFromAbsolute(string path)
    {
        var(result, ok) = TryRootedPathFromAbsolute(path);
        return (new(result.AsString()), ok);
    }

    public static (RootedFilePath Path, bool Ok) TryRootedFilePathFromNormalized(string path)
    {
        var(result, ok) = TryRootedPathFromNormalized(path);
        return (new(result.AsString()), ok);
    }
}

public static class RootedPathOperations
{
    public static bool StartsWithDirectory(this CaseSensitivity sensitivity, RootedFilePath fileName, RootedDirectoryPath directory)
    {
        var(relative, ok) = sensitivity.RelativePathWithinDirectory(directory, fileName.AsPath());
        return ok && relative.AsString() != "";
    }

    public static (RelativePath Path, bool Ok) RelativeFilePathFromDirectory(this CaseSensitivity sensitivity, RootedDirectoryPath directory, RootedFilePath fileName) => sensitivity.RelativePathWithinDirectory(directory, fileName.AsPath());
    public static int CompareFileNameStems(this CaseSensitivity sensitivity, FileNameStem first, FileNameStem second) => sensitivity.ComparePaths(new(first.AsString()), new(second.AsString()));
    public static (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitFilePathAtComponent(this CaseSensitivity sensitivity, RootedFilePath fileName, string component)
    {
        TsPath.validatePathComponent(component);
        var components = fileName.Components();
        if (TsPath.IsEncodedDynamicFileName(fileName.AsString()))
            sensitivity = CaseSensitivity.CaseSensitive;
        for (int i = 1; i < components.Count; i++)
            if (sensitivity.CompareStrings(components[i], component) == 0)
                return (new(TsPath.GetPathFromPathComponents(components.Take(i).ToList())), new(TsPath.GetPathFromPathComponents(components.Take(i + 1).ToList())), true);
        return (default, default, false);
    }

    public static RootedDirectoryPath CommonDirectoryOfFiles(this CaseSensitivity sensitivity, IEnumerable<RootedFilePath> files)
    {
        List<string>? common = null;
        foreach (var file in files)
        {
            var components = file.Components();
            components.RemoveAt(components.Count - 1);
            if (common == null)
            {
                common = components;
                continue;
            }

            int n = Math.Min(common.Count, components.Count);
            var effective = sensitivity;
            if (TsPath.IsEncodedDynamicFileName(file.AsString()) || TsPath.IsEncodedDynamicFileName(TsPath.GetPathFromPathComponents(common)))
            {
                effective = CaseSensitivity.CaseSensitive;
                common[0] = common[0].TrimEnd('/');
                components[0] = components[0].TrimEnd('/');
            }

            for (int i = 0; i < n; i++)
                if (effective.Canonicalize(common[i]) != effective.Canonicalize(components[i]))
                {
                    if (i == 0)
                        return default;
                    common = common.Take(i).ToList();
                    break;
                }

            if (components.Count < common.Count)
                common = common.Take(components.Count).ToList();
        }

        return common == null || common.Count == 0 ? default : new(TsPath.GetPathFromPathComponents(common));
    }
}
