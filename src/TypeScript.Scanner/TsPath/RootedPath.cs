// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/rooted_path.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct RootedPath
{
    private readonly ByteStringValue value;
    internal RootedPath(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }

    internal RootedPath(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static RootedPath FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }

    public bool Equals(RootedPath other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static RootedPath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }

        return new(bytes);
    }

    public static RootedPath FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
    public int Compare(RootedPath other) => Math.Sign(ByteStringKey.FromValue(value).CompareTo(ByteStringKey.FromValue(other.value)));
    public string BaseName() => BaseNameValue().Materialize()!;
    public ByteStringValue BaseNameValue() => TsPath.GetBaseFileName(value);
    public RootedDirectoryPath Directory()
    {
        var text = value;
        int root = TsPath.GetRootLength(text);
        if (root == text.Length)
        {
            return new(text);
        }

        text = TsPath.RemoveTrailingDirectorySeparator(text);
        return new(text.Slice(0, Math.Max(root, text.LastIndexOf('/'))));
    }

    public (RootedDirectoryPath Root, string Relative) RootAndRelativePath()
    {
        var(root, relative) = RootAndRelativePathValue();
        return (root, relative.Materialize()!);
    }

    public (RootedDirectoryPath Root, ByteStringValue Relative) RootAndRelativePathValue()
    {
        int root = TsPath.GetRootLength(value);
        return (new(value.Slice(0, root)), value.Slice(root));
    }

    public bool IsDynamic() => TsPath.IsDynamicFileName(value);
    public (RelativePath Path, bool Ok) RelativeTo(RootedDirectoryPath directory) => CaseSensitivity.CaseSensitive.RelativePathWithinDirectory(directory, this);
}

public readonly partial record struct RootedFilePath
{
    private readonly ByteStringValue value;
    internal RootedFilePath(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }

    internal RootedFilePath(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static RootedFilePath FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }

    public bool Equals(RootedFilePath other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static RootedFilePath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }

        return new(bytes);
    }

    public static RootedFilePath FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
    public RootedPath AsPath() => new(value);
    public RootedDirectoryPath Directory() => AsPath().Directory();
    public string BaseName() => BaseNameValue().Materialize()!;
    public ByteStringValue BaseNameValue() => AsPath().BaseNameValue();
    public int Compare(RootedFilePath other) => AsPath().Compare(other.AsPath());
    public (RootedDirectoryPath Root, string Relative) RootAndRelativePath() => AsPath().RootAndRelativePath();
    public (RootedDirectoryPath Root, ByteStringValue Relative) RootAndRelativePathValue() => AsPath().RootAndRelativePathValue();
    public RootedFilePath AppendSuffix(string suffix) => AppendSuffix(ByteStringValue.FromString(suffix));
    public RootedFilePath AppendSuffix(ByteStringValue suffix)
    {
        if (value.Length == 0 && (suffix.IsNull || suffix.Length != 0) || suffix.Contains('/') || suffix.Contains('\\'))
        {
            throw new ArgumentException("invalid file name suffix");
        }

        var result = TsPath.pathConcat(value, suffix);
        return result.Length == 0 ? default : TsPath.RootedFilePathFromNormalized(result);
    }

    public bool ExtensionIs(string extension) => TsPath.FileExtensionIs(value, extension);
    public bool IsDeclarationFile() => TsPath.IsDeclarationFileName(value);
    public ModuleSpecifier AsModuleSpecifier() => new(value);
    public string WithoutRoot() => WithoutRootValue().Materialize()!;
    public ByteStringValue WithoutRootValue() => value.Slice(TsPath.GetRootLength(value));
    public RootedDirectoryPath DirectoryBefore(int index)
    {
        if (index < RootLength() || index > value.Length || index < value.Length && value[index] is not ('/' or '\\'))
        {
            throw new ArgumentException("directory boundary must be at a path separator");
        }

        return new(value.Slice(0, index));
    }

    public string SuffixAfterSeparator(int index) => SuffixAfterSeparatorValue(index).Materialize()!;
    public ByteStringValue SuffixAfterSeparatorValue(int index)
    {
        if (index < 0 || index >= value.Length || value[index] is not ('/' or '\\'))
        {
            throw new ArgumentException("suffix boundary must be at a path separator");
        }

        return value.Slice(index + 1);
    }

    public FileNameStem AsStem() => new(value);
    public FileNameStem RemoveFileExtension() => new(TsPath.RemoveFileExtension(value));
    public FileNameStem RemoveExtension(string extension) => RemoveExtension(ByteStringValue.FromString(extension));
    public FileNameStem RemoveExtension(ByteStringValue extension)
    {
        TsPath.validateFileExtension(extension);
        if (!value.EndsWith(extension))
        {
            throw new ArgumentException("file name does not have extension: " + extension.Materialize());
        }

        return new(TsPath.RemoveExtension(value, extension));
    }

    public RootedFilePath ChangeExtension(string extension) => ChangeExtension(ByteStringValue.FromString(extension));
    public RootedFilePath ChangeExtension(ByteStringValue extension)
    {
        TsPath.validateFileExtension(extension);
        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeExtension(value, extension));
    }

    public RootedFilePath ChangeFullExtension(string extension) => ChangeFullExtension(ByteStringValue.FromString(extension));
    public RootedFilePath ChangeFullExtension(ByteStringValue extension)
    {
        TsPath.validateFileExtension(extension);
        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeFullExtension(value, extension));
    }

    public RootedFilePath ChangeAnyExtension(string extension, IEnumerable<string> extensions, CaseSensitivity sensitivity) => ChangeAnyExtension(ByteStringValue.FromString(extension), extensions, sensitivity);
    public RootedFilePath ChangeAnyExtension(ByteStringValue extension, IEnumerable<string> extensions, CaseSensitivity sensitivity)
    {
        TsPath.validateFileExtension(extension);
        foreach (string candidate in extensions)
        {
            TsPath.validateFileExtension(candidate);
        }

        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeAnyExtension(value, extension, extensions, sensitivity));
    }

    public RootedFilePath ChangeAnyExtension(ByteStringValue extension, Slice<ByteStringValue> extensions, CaseSensitivity sensitivity)
    {
        TsPath.validateFileExtension(extension);
        foreach (var candidate in extensions)
        {
            TsPath.validateFileExtension(candidate);
        }

        return TsPath.rootedFilePathFromExtensionMutation(TsPath.ChangeAnyExtension(value, extension, extensions, sensitivity));
    }

    public bool ExtensionIsOneOf(IEnumerable<string> extensions) => TsPath.FileExtensionIsOneOf(value, extensions);
    public string Extension() => ExtensionValue().Materialize()!;
    public ByteStringValue ExtensionValue() => TsPath.TryGetExtensionFromPath(value);
    public string AnyExtension(IEnumerable<string>? extensions, CaseSensitivity sensitivity) => AnyExtensionValue(extensions, sensitivity).Materialize()!;
    public ByteStringValue AnyExtensionValue(IEnumerable<string>? extensions, CaseSensitivity sensitivity) => TsPath.GetAnyExtensionFromPath(value, extensions, sensitivity);
    public string LongestExtension(IEnumerable<string>? extensions, CaseSensitivity sensitivity) => LongestExtensionValue(extensions, sensitivity).Materialize()!;
    public ByteStringValue LongestExtensionValue(IEnumerable<string>? extensions, CaseSensitivity sensitivity) => TsPath.GetLongestExtensionFromPath(value, extensions, sensitivity);
    public bool HasExtension() => BaseNameValue().Contains('.');
    public bool HasImplementationTSFileExtension() => TsPath.HasImplementationTSFileExtension(value);
    public bool HasTSFileExtension() => TsPath.HasTSFileExtension(value);
    public string TryExtractTSExtension() => TsPath.TryExtractTSExtension(value);
    public bool HasJSONFileExtension() => TsPath.HasJSONFileExtension(value);
    public string DeclarationFileExtension() => DeclarationFileExtensionValue().Materialize()!;
    public ByteStringValue DeclarationFileExtensionValue() => TsPath.GetDeclarationFileExtension(value);
    public string DeclarationEmitExtension() => DeclarationEmitExtensionValue().Materialize()!;
    public ByteStringValue DeclarationEmitExtensionValue() => TsPath.GetDeclarationEmitExtensionForPath(value);
    public List<string> PossibleOriginalInputExtensions() => TsPath.projectPathComponents(PossibleOriginalInputExtensionsValues());
    public Slice<ByteStringValue> PossibleOriginalInputExtensionsValues() => TsPath.GetPossibleOriginalInputExtensionForExtension(value);
    public bool HasJSFileExtension() => TsPath.HasJSFileExtension(value);
    public bool IsDynamic() => TsPath.IsDynamicFileName(value);
    public List<string> Components() => TsPath.projectPathComponents(ComponentsValues());
    public Slice<ByteStringValue> ComponentsValues() => TsPath.GetPathComponents(value);
    public int RootLength() => TsPath.GetRootLength(value);
    public (RelativePath Path, bool Ok) RelativeTo(RootedDirectoryPath directory) => AsPath().RelativeTo(directory);
    public bool ContainsLowercaseDirectorySequence(string sequence) => ContainsLowercaseDirectorySequence(ByteStringValue.FromString(sequence));
    public bool ContainsLowercaseDirectorySequence(ByteStringValue sequence) => value.IndexOf(sequence) >= 0;
    public int DirectorySeparatorCount() => value.IsBytes ? value.ByteSpan.Count((byte)'/') : value.CharSpan.Count('/');
    public (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitAtComponent(string component) => SplitAtComponent(ByteStringValue.FromString(component));
    public (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitAtComponent(ByteStringValue component)
    {
        TsPath.validatePathComponent(component);
        var text = value;
        var needle = TsPath.pathConcat(ByteStringValue.FromString("/"), component);
        int root = RootLength();
        if (root == 0)
        {
            return (default, default, false);
        }

        for (int offset = root - 1;;)
        {
            int index = text.IndexOf(needle, offset);
            if (index < 0)
            {
                return (default, default, false);
            }

            int end = index + needle.Length;
            if (end == text.Length || text[end] == '/')
            {
                return (new(text.Slice(0, Math.Max(index, root))), new(text.Slice(0, end)), true);
            }

            offset = end;
        }
    }

    public (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitAtLastComponent(string component) => SplitAtLastComponent(ByteStringValue.FromString(component));
    public (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitAtLastComponent(ByteStringValue component)
    {
        TsPath.validatePathComponent(component);
        var text = value;
        var needle = TsPath.pathConcat(ByteStringValue.FromString("/"), component);
        for (int end = text.Length; end > 0;)
        {
            int index = text.Slice(0, end).LastIndexOf(needle);
            if (index < 0)
            {
                return (default, default, false);
            }

            int componentEnd = index + needle.Length;
            if (index >= RootLength() - 1 && (componentEnd == text.Length || text[componentEnd] == '/'))
            {
                return (new(text.Slice(0, Math.Max(index, RootLength()))), new(text.Slice(0, componentEnd)), true);
            }

            end = index;
        }

        return (default, default, false);
    }
}

public readonly partial record struct RootedDirectoryPath
{
    private readonly ByteStringValue value;
    internal RootedDirectoryPath(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }

    internal RootedDirectoryPath(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static RootedDirectoryPath FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }

    public bool Equals(RootedDirectoryPath other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static RootedDirectoryPath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }

        return new(bytes);
    }

    public static RootedDirectoryPath FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
    public RootedPath AsPath() => new(value);
    public string BaseName() => BaseNameValue().Materialize()!;
    public ByteStringValue BaseNameValue() => AsPath().BaseNameValue();
    public RootedDirectoryPath Directory() => AsPath().Directory();
    public int Compare(RootedDirectoryPath other) => AsPath().Compare(other.AsPath());
    public RootedFilePath ResolveFile(string path) => ResolveFile(ByteStringValue.FromString(path));
    public RootedFilePath ResolveFile(ByteStringValue path)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("cannot resolve from an empty directory name");
        }

        if (path.IsNull)
        {
            throw new NullReferenceException();
        }

        return path.Length == 0 ? new(value) : TsPath.ToRootedFilePath(path, this);
    }

    public RootedDirectoryPath ResolveDirectory(string path) => ResolveDirectory(ByteStringValue.FromString(path));
    public RootedDirectoryPath ResolveDirectory(ByteStringValue path)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("cannot resolve from an empty directory name");
        }

        if (path.IsNull)
        {
            throw new NullReferenceException();
        }

        return path.Length == 0 ? this : TsPath.ToRootedDirectoryPath(path, this);
    }

    public (T? Result, bool Ok) ForEachAncestorDirectory<T>(Func<RootedDirectoryPath, (T Result, bool Stop)> callback)
    {
        RootedDirectoryPath current = this;
        while (true)
        {
            var(result, stop) = callback(current);
            if (stop)
            {
                return (result, true);
            }

            var parent = current.Directory();
            if (parent == current)
            {
                return (typeof(T) == typeof(string) ? (T)(object)"" : default, false);
            }

            current = parent;
        }
    }

    public List<string> Components() => TsPath.projectPathComponents(ComponentsValues());
    public Slice<ByteStringValue> ComponentsValues() => TsPath.GetPathComponents(value);
    public bool ContainsLowercaseDirectorySequence(string sequence) => ContainsLowercaseDirectorySequence(ByteStringValue.FromString(sequence));
    public bool ContainsLowercaseDirectorySequence(ByteStringValue sequence) => value.IndexOf(sequence) >= 0;
    public RootedFilePath ResolveRelativeFile(RelativePath path)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("cannot resolve from an empty directory name");
        }

        var relative = path.AsValue();
        if (relative.Length == 0)
        {
            return new(value);
        }

        if (TsPath.hasURLRoot(value) && relative.IndexOfAny('?', '#') >= 0)
        {
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        }

        return path.requiresResolution() ? TsPath.ToRootedFilePath(relative, this) : TsPath.rootedFilePathFromResolved(TsPath.appendPathToDirectory(this, relative));
    }

    public RootedDirectoryPath ResolveRelativeDirectory(RelativePath path) => new(ResolveRelativeFile(path).AsValue());
    public RootedFilePath ResolveFileFromNormalizedRelative(string path) => ResolveFileFromNormalizedRelative(ByteStringValue.FromString(path));
    public RootedFilePath ResolveFileFromNormalizedRelative(ByteStringValue path)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("cannot resolve from an empty directory path");
        }

        if (path.IsNull)
        {
            throw new NullReferenceException();
        }

        if (path.Length == 0)
        {
            throw new ArgumentException("path must not be empty");
        }

        if (TsPath.hasURLRoot(value) && TsPath.GetEncodedRootLength(path) == 0 && path.IndexOfAny('?', '#') >= 0)
        {
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        }

        bool relativeSegment = false;
        foreach (var part in TsPath.GetPathComponents(path))
            if (part.EqualsBytes("."u8) || part.EqualsBytes(".."u8))
            {
                relativeSegment = true;
                break;
            }

        if (path[0] is '/' or '\\' || !TsPath.NormalizeSlashes(path).EqualsValue(path) || relativeSegment || TsPath.HasTrailingDirectorySeparator(path))
        {
            throw new ArgumentException("path must be relative and normalized: " + path.Materialize());
        }

        return TsPath.rootedFilePathFromResolved(TsPath.appendPathToDirectory(this, path));
    }
}

public readonly partial record struct FileNameStem
{
    private readonly ByteStringValue value;
    internal FileNameStem(string bytes) : this(ByteStringValue.FromString(bytes is null ? throw new NullReferenceException() : bytes))
    {
    }

    internal FileNameStem(ByteStringValue bytes) => value = bytes.IsNull ? throw new NullReferenceException() : bytes.Length == 0 ? default : bytes.Snapshot();
    public ByteStringValue AsValue() => value;
    public string AsString() => value.Materialize()!;
    public static FileNameStem FromValue(ByteStringValue bytes)
    {
        TsPath.validateGoPathValue(bytes);
        return new(bytes);
    }

    public bool Equals(FileNameStem other) => value.EqualsValue(other.value);
    public override int GetHashCode() => ByteStringKey.FromValue(value).GetHashCode();
    public static FileNameStem FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!GoString.IsByteText(bytes.AsSpan()))
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        }

        return new(bytes);
    }

    public static FileNameStem FromUnicode(string text) => FromValue(PathByteText.FromUnicodeValue(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsValue());
    public override string ToString() => AsString();
    public RootedFilePath AppendSuffix(string suffix) => AppendSuffix(ByteStringValue.FromString(suffix));
    public RootedFilePath AppendSuffix(ByteStringValue suffix)
    {
        if (value.Length == 0 && (suffix.IsNull || suffix.Length != 0) || suffix.Contains('/') || suffix.Contains('\\'))
        {
            throw new ArgumentException("invalid file name suffix");
        }

        var result = TsPath.pathConcat(value, suffix);
        return result.Length == 0 ? default : TsPath.RootedFilePathFromNormalized(result);
    }
}

public static partial class TsPath
{
    public static T? ForEachAncestorDirectoryPathStoppingAtGlobalCache<T>(RootedDirectoryPath globalCache, RootedDirectoryPath directory, Func<RootedDirectoryPath, (T Result, bool Stop)> callback)
    {
        var(result, _) = directory.ForEachAncestorDirectory<T>(ancestor =>
        {
            var(value, stop) = callback(ancestor);
            return (value, stop || ancestor == globalCache);
        });
        return result;
    }

    public static RootedPath ToRootedPath(string path, RootedDirectoryPath directory) => ToRootedPath(ByteStringValue.FromString(path), directory);
    public static RootedPath ToRootedPath(ByteStringValue path, RootedDirectoryPath directory)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }

        if (path.Length == 0)
        {
            throw new ArgumentException("path must not be empty");
        }

        if (hasRootedURLSuffix(path))
        {
            throw new ArgumentException("path must not contain a URL query or fragment");
        }

        if (GetEncodedRootLength(path) == 0 && hasURLRoot(directory.AsValue()) && path.IndexOfAny('?', '#') >= 0)
        {
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        }

        var normalized = GetNormalizedAbsolutePath(path, directory.AsValue());
        if (GetEncodedRootLength(normalized) == 0 || hasRootedURLSuffix(normalized))
        {
            throw new ArgumentException("path must be rooted");
        }

        return new(ensureRootedPathRootSeparator(normalized));
    }

    private static bool hasRootedURLSuffix(ByteStringValue path)
    {
        if (GetEncodedRootLength(path) >= 0)
        {
            return false;
        }

        int scheme = path.IndexOf("://"u8);
        return scheme >= 0 && path.Slice(scheme + 3).IndexOfAny('?', '#') >= 0;
    }

    private static ByteStringValue ensureRootedPathRootSeparator(ByteStringValue path) => GetRootLength(path) == path.Length && !HasTrailingDirectorySeparator(path) ? pathConcat(path, ByteStringValue.FromString("/")) : path;
    public static (RootedPath Path, bool Ok) TryRootedPathFromAbsolute(string path) => TryRootedPathFromAbsolute(ByteStringValue.FromString(path));
    public static (RootedPath Path, bool Ok) TryRootedPathFromAbsolute(ByteStringValue path)
    {
        if (hasRootedURLSuffix(path) || GetEncodedRootLength(path) == 0)
        {
            return (default, false);
        }

        return (new(ensureRootedPathRootSeparator(GetNormalizedAbsolutePath(path, default(ByteStringValue)))), true);
    }

    public static RootedPath RootedPathFromAbsolute(string path) => RootedPathFromAbsolute(ByteStringValue.FromString(path));
    public static RootedPath RootedPathFromAbsolute(ByteStringValue path)
    {
        var(result, ok) = TryRootedPathFromAbsolute(path);
        return ok ? result : throw new ArgumentException("path must be absolute");
    }

    public static (RootedPath Path, bool Ok) TryRootedPathFromNormalized(string path) => TryRootedPathFromNormalized(ByteStringValue.FromString(path));
    public static (RootedPath Path, bool Ok) TryRootedPathFromNormalized(ByteStringValue path)
    {
        if (hasRootedURLSuffix(path))
        {
            return (default, false);
        }

        int root = GetEncodedRootLength(path);
        if (root < 0)
        {
            root = ~root;
        }

        if (path.Length == 0 || root == 0 || path.Contains('\\') || root < path.Length && path[root] == '/' || hasRelativePathSegment(path.Slice(root)) || path.Length == root && !HasTrailingDirectorySeparator(path) || path.Length > root && HasTrailingDirectorySeparator(path))
        {
            return (default, false);
        }

        return (new(path), true);
    }

    public static RootedPath RootedPathFromNormalized(string path) => RootedPathFromNormalized(ByteStringValue.FromString(path));
    public static RootedPath RootedPathFromNormalized(ByteStringValue path)
    {
        var(result, ok) = TryRootedPathFromNormalized(path);
        return ok ? result : throw new ArgumentException("path must be rooted and normalized: " + path.Materialize());
    }

    public static RootedFilePath ToRootedFilePath(string path, RootedDirectoryPath directory) => ToRootedFilePath(ByteStringValue.FromString(path), directory);
    public static RootedFilePath ToRootedFilePath(ByteStringValue path, RootedDirectoryPath directory) => new(ToRootedPath(path, directory).AsValue());
    public static RootedFilePath RootedFilePathFromAbsolute(string path) => RootedFilePathFromAbsolute(ByteStringValue.FromString(path));
    public static RootedFilePath RootedFilePathFromAbsolute(ByteStringValue path) => new(RootedPathFromAbsolute(path).AsValue());
    public static RootedFilePath RootedFilePathFromNormalized(string path) => RootedFilePathFromNormalized(ByteStringValue.FromString(path));
    public static RootedFilePath RootedFilePathFromNormalized(ByteStringValue path) => new(RootedPathFromNormalized(path).AsValue());
    public static RootedFilePath RootedFilePathFromPath(RootedPath path) => new(path.AsValue());
    public static RootedDirectoryPath ToRootedDirectoryPath(string path, RootedDirectoryPath directory) => ToRootedDirectoryPath(ByteStringValue.FromString(path), directory);
    public static RootedDirectoryPath ToRootedDirectoryPath(ByteStringValue path, RootedDirectoryPath directory) => new(ToRootedPath(path, directory).AsValue());
    public static RootedDirectoryPath RootedDirectoryPathFromAbsolute(string path) => RootedDirectoryPathFromAbsolute(ByteStringValue.FromString(path));
    public static RootedDirectoryPath RootedDirectoryPathFromAbsolute(ByteStringValue path) => new(RootedPathFromAbsolute(path).AsValue());
    public static RootedDirectoryPath RootedDirectoryPathFromNormalized(string path) => RootedDirectoryPathFromNormalized(ByteStringValue.FromString(path));
    public static RootedDirectoryPath RootedDirectoryPathFromNormalized(ByteStringValue path) => new(RootedPathFromNormalized(path).AsValue());
    public static RootedDirectoryPath RootedDirectoryPathFromPath(RootedPath path) => new(path.AsValue());
    internal static bool hasURLRoot(string path) => hasURLRoot(ByteStringValue.FromString(path));
    internal static bool hasURLRoot(ByteStringValue path) => GetEncodedRootLength(path) < 0 && path.Contains("://"u8);
    internal static RootedFilePath rootedFilePathFromResolved(ByteStringValue path)
    {
        if (hasRootedURLSuffix(path))
        {
            throw new ArgumentException("path must not contain a URL query or fragment");
        }

        return new(path);
    }

    internal static RootedFilePath rootedFilePathFromExtensionMutation(ByteStringValue path)
    {
        var name = GetBaseFileName(path);
        if (name.EqualsBytes("."u8) || name.EqualsBytes(".."u8) || HasTrailingDirectorySeparator(path) && path.Length > GetRootLength(path))
        {
            throw new ArgumentException("file extension change must preserve path normalization");
        }

        return rootedFilePathFromResolved(path);
    }

    internal static void validatePathComponent(string component) => validatePathComponent(ByteStringValue.FromString(component));
    internal static void validatePathComponent(ByteStringValue component)
    {
        if (component.IsNull)
        {
            throw new NullReferenceException();
        }

        if (component.Length == 0 || component.Contains('/') || component.Contains('\\') || component.EqualsBytes("."u8) || component.EqualsBytes(".."u8))
        {
            throw new ArgumentException("invalid path component");
        }
    }

    internal static ByteStringValue appendPathToDirectory(RootedDirectoryPath directory, ByteStringValue path) => pathConcat(directory.AsValue(), HasTrailingDirectorySeparator(directory.AsValue()) ? default : ByteStringValue.FromString("/"), path);
    public static (RootedFilePath Path, bool Ok) TryRootedFilePathFromAbsolute(string path) => TryRootedFilePathFromAbsolute(ByteStringValue.FromString(path));
    public static (RootedFilePath Path, bool Ok) TryRootedFilePathFromAbsolute(ByteStringValue path)
    {
        var(result, ok) = TryRootedPathFromAbsolute(path);
        return (new(result.AsValue()), ok);
    }

    public static (RootedFilePath Path, bool Ok) TryRootedFilePathFromNormalized(string path) => TryRootedFilePathFromNormalized(ByteStringValue.FromString(path));
    public static (RootedFilePath Path, bool Ok) TryRootedFilePathFromNormalized(ByteStringValue path)
    {
        var(result, ok) = TryRootedPathFromNormalized(path);
        return (new(result.AsValue()), ok);
    }
}

public static class RootedPathOperations
{
    public static bool StartsWithDirectory(this CaseSensitivity sensitivity, RootedFilePath file, RootedDirectoryPath directory)
    {
        var(relative, ok) = sensitivity.RelativePathWithinDirectory(directory, file.AsPath());
        return ok && relative.AsValue().Length != 0;
    }

    public static (RelativePath Path, bool Ok) RelativeFilePathFromDirectory(this CaseSensitivity sensitivity, RootedDirectoryPath directory, RootedFilePath file) => sensitivity.RelativePathWithinDirectory(directory, file.AsPath());
    public static int CompareFileNameStems(this CaseSensitivity sensitivity, FileNameStem first, FileNameStem second) => sensitivity.ComparePaths(new(first.AsValue()), new(second.AsValue()));
    public static (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitFilePathAtComponent(this CaseSensitivity sensitivity, RootedFilePath file, string component) => sensitivity.SplitFilePathAtComponent(file, ByteStringValue.FromString(component));
    public static (RootedDirectoryPath Before, RootedDirectoryPath Through, bool Ok) SplitFilePathAtComponent(this CaseSensitivity sensitivity, RootedFilePath file, ByteStringValue component)
    {
        TsPath.validatePathComponent(component);
        var parts = file.ComponentsValues();
        if (TsPath.IsEncodedDynamicFileName(file.AsValue()))
        {
            sensitivity = CaseSensitivity.CaseSensitive;
        }

        for (int i = 1; i < parts.Length; i++)
            if (sensitivity.CompareValues(parts[i], component) == 0)
                return (new(TsPath.GetPathFromPathComponents(parts.Slice2(0, i))), new(TsPath.GetPathFromPathComponents(parts.Slice2(0, i + 1))), true);
        return (default, default, false);
    }

    public static RootedDirectoryPath CommonDirectoryOfFiles(this CaseSensitivity sensitivity, IEnumerable<RootedFilePath> files)
    {
        Slice<ByteStringValue>? common = null;
        foreach (var file in files)
        {
            var components = file.ComponentsValues();
            components = components.Slice2(0, components.Length - 1);
            if (common == null)
            {
                common = components;
                continue;
            }

            var prefix = common.Value;
            int n = Math.Min(prefix.Length, components.Length);
            var effective = sensitivity;
            if (TsPath.IsEncodedDynamicFileName(file.AsValue()) || TsPath.IsEncodedDynamicFileName(TsPath.GetPathFromPathComponents(prefix)))
            {
                effective = CaseSensitivity.CaseSensitive;
                prefix[0] = TsPath.trimDynamicRoot(prefix[0]);
                components[0] = TsPath.trimDynamicRoot(components[0]);
            }

            for (int i = 0; i < n; i++)
                if (!effective.Canonicalize(prefix[i]).EqualsValue(effective.Canonicalize(components[i])))
                {
                    if (i == 0)
                    {
                        return default;
                    }

                    prefix = prefix.Slice2(0, i);
                    break;
                }

            if (components.Length < prefix.Length)
            {
                prefix = prefix.Slice2(0, components.Length);
            }

            common = prefix;
        }

        return common is null || common.Value.Length == 0 ? default : new(TsPath.GetPathFromPathComponents(common.Value));
    }
}

public static partial class TsPath
{
    // rooted_path.go:664. Required editor watch grouping; callbacks retain byte headers.
    internal static (Slice<RootedDirectoryPath> Parents, HashSet<RootedDirectoryPath>? Ignored) GetCommonParentDirectories(Slice<RootedDirectoryPath> directories, int minComponents, Func<RootedDirectoryPath, Slice<ByteStringValue>> getComponents, CaseSensitivity sensitivity)
    {
        if (minComponents < 1)
            throw new InvalidOperationException("minComponents must be at least 1");
        if (directories.Length == 0)
            return (default, null);
        if (directories.Length == 1)
            return ReducePathComponents(getComponents(directories[0])).Length < minComponents ? (default, new() { directories[0] }) : (directories, null);
        var ignored = new HashSet<RootedDirectoryPath>();
        var groups = Slice<Slice<ByteStringValue>>.Make(0, directories.Length);
        foreach (var directory in directories)
        {
            var components = ReducePathComponents(getComponents(directory));
            if (components.Length < minComponents)
                ignored.Add(directory);
            else
                groups = groups.Append(components);
        }

        var results = GetCommonParentsWorker(groups, minComponents, sensitivity);
        var parents = Slice<RootedDirectoryPath>.Make(results.Length);
        for (int i = 0; i < results.Length; i++)
            parents[i] = new RootedDirectoryPath(GetPathFromPathComponents(results[i]));
        return (parents, ignored);
    }

    private readonly record struct CommonParentGroup(Slice<ByteStringValue> Head, Slice<Slice<ByteStringValue>> Tails);
    // path.go:1238. Preserve first-divergence fanout, latest presentation head and required concat copies.
    private static Slice<Slice<ByteStringValue>> GetCommonParentsWorker(Slice<Slice<ByteStringValue>> components, int minComponents, CaseSensitivity sensitivity)
    {
        if (components.Length == 0)
            return default;
        int maxDepth = components[0].Length;
        for (int i = 1; i < components.Length; i++)
            maxDepth = Math.Min(maxDepth, components[i].Length);
        for (int last = 0; last < maxDepth; last++)
        {
            var candidate = components[0][last];
            for (int j = 0; j < components.Length - 1; j++)
            {
                if (sensitivity.IsCaseSensitive() ? candidate.EqualsValue(components[j + 1][last]) : UnicodeCase.EqualFold(candidate, components[j + 1][last]))
                    continue;
                if (last >= minComponents)
                    return Slice<Slice<ByteStringValue>>.FromArray([components[0].Slice2(0, last)]);
                var ordered = Slice<ByteStringKey>.Make(0, components.Length - j);
                var groups = new Dictionary<ByteStringKey, CommonParentGroup>();
                foreach (var group in components)
                {
                    var key = ByteStringKey.FromValue(sensitivity.Canonicalize(group[last]));
                    if (!groups.TryGetValue(key, out var prior))
                        ordered = ordered.Append(key);
                    groups[key] = new(group.Slice2(0, last + 1), prior.Tails.Append(group.Slice2(last + 1, group.Length)));
                }

                ordered.AsSpan().Sort();
                var result = Slice<Slice<ByteStringValue>>.Make(0, groups.Count);
                foreach (var key in ordered)
                {
                    var group = groups[key];
                    var subResults = GetCommonParentsWorker(group.Tails, minComponents - last - 1, sensitivity);
                    foreach (var tail in subResults)
                    {
                        if (tail.Length == 0)
                            result = result.Append(group.Head.Slice3(0, group.Head.Length, group.Head.Length));
                        else
                        {
                            var combined = Slice<ByteStringValue>.Make(group.Head.Length + tail.Length);
                            group.Head.AsSpan().CopyTo(combined.AsSpan());
                            tail.AsSpan().CopyTo(combined.AsSpan()[group.Head.Length..]);
                            result = result.Append(combined);
                        }
                    }
                }

                return result;
            }
        }

        return Slice<Slice<ByteStringValue>>.FromArray([components[0].Slice2(0, maxDepth)]);
    }
}
