// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/relative_path.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Path text is an original Go byte string (one managed char per byte).
namespace TypeScript.Scanner;
/// <summary>Original Go byte-string identity. Use FromUnicode for a scalar Unicode boundary.</summary>
public readonly partial record struct RelativePath
{
    private readonly string? value;
    internal RelativePath(string bytes) => value = bytes.Length == 0 ? null : bytes;
    public string AsString() => value ?? "";
    public static RelativePath FromGoString(string bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Any(ch => ch > 255))
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(bytes));
        return new(bytes);
    }

    public static RelativePath FromUnicode(string text) => FromGoString(PathByteText.FromUnicode(text));
    public string ToUnicode() => PathByteText.ToUnicode(AsString());
    public override string ToString() => AsString();
}

public readonly partial record struct RelativePath
{
    public ModuleSpecifier AsModuleSpecifier() => new(TsPath.EnsurePathIsNonModuleName(AsString()));
    public RelativePath ChangeExtension(string extension)
    {
        TsPath.validateFileExtension(extension);
        return new(TsPath.ChangeExtension(AsString(), extension));
    }

    public string BaseName() => TsPath.GetBaseFileName(AsString());
    public bool IsParentRelative() => AsString() == ".." || AsString().StartsWith("../", StringComparison.Ordinal);
    public bool HasTrailingDirectorySeparator() => TsPath.HasTrailingDirectorySeparator(AsString());
    public RelativePath WithoutTrailingDirectorySeparator() => HasTrailingDirectorySeparator() ? new(TsPath.RemoveTrailingDirectorySeparator(AsString())) : this;
    public RelativePath WithTrailingDirectorySeparator() => AsString() == "" || HasTrailingDirectorySeparator() ? this : new(AsString() + "/");
    internal bool requiresResolution() => IsParentRelative() || HasTrailingDirectorySeparator();
}

public static partial class TsPath
{
    public static RelativePath ToRelativePath(string path)
    {
        if (GetEncodedRootLength(path) != 0)
            throw new ArgumentException("relative path must not be rooted");
        return new(NormalizePath(path));
    }

    public static RelativePath RelativePathFromNormalized(string path)
    {
        if (GetEncodedRootLength(path) != 0 || NormalizePath(path) != path)
            throw new ArgumentException("relative path must be relative and normalized: " + path);
        return new(path);
    }

    internal static void validateFileExtension(string extension)
    {
        if (extension.Contains('/') || extension.Contains('\\'))
            throw new ArgumentException("file extension must not contain a directory separator");
    }

    internal static string relativePathFromNormalizedPaths(string from, string to, CaseSensitivity sensitivity)
    {
        var first = GetPathComponents(from);
        var second = GetPathComponents(to);
        if (IsEncodedDynamicFileName(from) || IsEncodedDynamicFileName(to))
        {
            first[0] = first[0].TrimEnd('/');
            second[0] = second[0].TrimEnd('/');
            if (first[0] != second[0])
                return to;
            sensitivity = CaseSensitivity.CaseSensitive;
        }

        int start = 0, common = Math.Min(first.Count, second.Count);
        for (; start < common; start++)
        {
            bool equal = start == 0 || sensitivity.IsCaseInsensitive() ? UnicodeCase.EqualFold(first[start], second[start]) : first[start] == second[start];
            if (!equal)
                break;
        }

        if (start == 0)
            return GetPathFromPathComponents(second);
        List<string> result = [""];
        for (int i = start; i < first.Count; i++)
            result.Add("..");
        for (int i = start; i < second.Count; i++)
            result.Add(second[i]);
        return GetPathFromPathComponents(result);
    }
}

public static class RelativePathOperations
{
    public static RelativePath CanonicalRelativePath(this CaseSensitivity sensitivity, RelativePath path) => new(sensitivity.Canonicalize(path.AsString()));
    public static (RelativePath Path, bool Ok) RelativePathFromDirectory(this CaseSensitivity sensitivity, RootedDirectoryPath directory, RootedFilePath fileName) => sensitivity.RelativePathFromPath(directory, fileName.AsPath());
    public static (RelativePath Path, bool Ok) RelativePathFromPath(this CaseSensitivity sensitivity, RootedDirectoryPath directory, RootedPath path)
    {
        string result = TsPath.relativePathFromNormalizedPaths(directory.AsString(), path.AsString(), sensitivity);
        return TsPath.GetEncodedRootLength(result) != 0 ? (default, false) : (new(result), true);
    }

    public static (RelativePath Path, bool Ok) RelativePathFromFile(this CaseSensitivity sensitivity, RootedFilePath from, RootedFilePath to) => sensitivity.RelativePathFromFileToPath(from, to.AsPath());
    public static (RelativePath Path, bool Ok) RelativePathFromFileToPath(this CaseSensitivity sensitivity, RootedFilePath from, RootedPath to) => sensitivity.RelativePathFromPath(from.Directory(), to);
    public static RelativePath RelativePathFromRelativeDirectory(this CaseSensitivity sensitivity, RelativePath directory, RelativePath fileName) => new(TsPath.relativePathFromNormalizedPaths(directory.AsString(), fileName.AsString(), sensitivity));
}
