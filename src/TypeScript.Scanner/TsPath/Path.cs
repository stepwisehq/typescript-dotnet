// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/path.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Retained Go strings use immutable byte/raw-character headers. String overloads
// project only at their explicit public compatibility boundary.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    private const string DynamicURIFileNamePrefix = "^/~ts-uri~/";
    private const string urlSchemeSeparator = "://";
    internal static void validateGoPathValue(ByteStringValue value)
    {
        if (value.IsNull)
        {
            throw new ArgumentNullException(nameof(value));
        }
        if (!value.IsBytes && value.CharSpan.IndexOfAnyExceptInRange('\0', '\u00ff') >= 0)
        {
            throw new ArgumentException("Go byte strings require one managed character per byte.", nameof(value));
        }
    }
    // Go string concatenation can return a nonempty operand when every other
    // operand is empty. Changed content owns one exact byte buffer, never a CLR
    // projection between internal path operations.
    internal static ByteStringValue pathConcat(params ReadOnlySpan<ByteStringValue> parts) => ByteStringValue.Concat(parts);
    public static bool IsVolumeCharacter(int ch) => ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z';
    public static int GetEncodedRootLength(string path) => GetEncodedRootLength(ByteStringValue.FromString(path));
    public static int GetEncodedRootLength(ByteStringValue path)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }
        int length = path.Length;
        if (length == 0)
        {
            return 0;
        }
        int first = path[0];
        if (first is '/' or '\\')
        {
            if (length == 1 || path[1] != first)
            {
                return 1;
            }
            int next = path.Slice(2).IndexOf(first);
            return next < 0 ? length : next + 3;
        }
        if (IsVolumeCharacter(first) && length > 1 && path[1] == ':')
        {
            if (length == 2)
            {
                return 2;
            }
            if (path[2] is '/' or '\\')
            {
                return 3;
            }
        }
        if (first == '^' && length > 1 && path[1] == '/')
        {
            if (path.StartsWith("^/~ts-uri~/"u8))
            {
                int schemeEnd = path.Slice(DynamicURIFileNamePrefix.Length).IndexOf('/');
                if (schemeEnd >= 0)
                {
                    int authorityStart = DynamicURIFileNamePrefix.Length + schemeEnd + 1;
                    int authorityEnd = path.Slice(authorityStart).IndexOf('/');
                    return authorityEnd >= 0 ? authorityStart + authorityEnd + 1 : ~length;
                }
            }
            return 2;
        }
        int scheme = path.IsBytes ? path.ByteSpan.IndexOf("://"u8) : path.CharSpan.IndexOf("://".AsSpan());
        if (scheme < 0)
        {
            return 0;
        }
        int start = scheme + 3;
        int authorityLength = path.Slice(start).IndexOf('/');
        if (authorityLength < 0)
        {
            return ~length;
        }
        int end = start + authorityLength;
        if (equalFoldPathPart(path.Slice(0, scheme), "file") && (end == start || equalFoldPathPart(path.Slice(start, end), "localhost"))
        && length > end + 2 && IsVolumeCharacter(path[end + 1]))
        {
            int volumeEnd = -1, offset = end + 2;
            if (path[offset] == ':')
            {
                volumeEnd = offset + 1;
            }
            else
            {
                if (path[offset] == '%' && length > offset + 2 && path[offset + 1] == '3' && path[offset + 2] is 'a' or 'A')
                {
                    volumeEnd = offset + 3;
                }
            }
            if (volumeEnd >= 0)
            {
                if (volumeEnd == length)
                {
                    return ~volumeEnd;
                }
                if (path[volumeEnd] == '/')
                {
                    return ~(volumeEnd + 1);
                }
            }
        }
        return ~(end + 1);
    }
    private static bool equalFoldPathPart(ByteStringValue value, string expected) =>
    UnicodeCase.EqualFold(value, ByteStringValue.FromString(expected));
    public static int GetRootLength(string path) => GetRootLength(ByteStringValue.FromString(path));
    public static int GetRootLength(ByteStringValue path)
    {
        int root = GetEncodedRootLength(path);
        return root < 0 ? ~root : root;
    }
    public static bool IsDynamicFileName(string path) => IsDynamicFileName(ByteStringValue.FromString(path));
    public static bool IsDynamicFileName(ByteStringValue path) => path.StartsWith("^/"u8);
    public static bool PathIsAbsolute(string path) => PathIsAbsolute(ByteStringValue.FromString(path));
    public static bool PathIsAbsolute(ByteStringValue path) => GetEncodedRootLength(path) != 0;
    public static bool IsRootedDiskPath(string path) => IsRootedDiskPath(ByteStringValue.FromString(path));
    public static bool IsRootedDiskPath(ByteStringValue path) => GetEncodedRootLength(path) > 0;
    public static bool PathIsRelative(string path) => PathIsRelative(ByteStringValue.FromString(path));
    public static bool PathIsRelative(ByteStringValue path)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }
        return path.EqualsBytes("."u8) || path.EqualsBytes(".."u8) ||
        path.Length >= 2 && path[0] == '.' && path[1] is '/' or '\\' ||
        path.Length >= 3 && path[0] == '.' && path[1] == '.' && path[2] is '/' or '\\';
    }
    public static bool IsExternalModuleNameRelative(string name) => IsExternalModuleNameRelative(ByteStringValue.FromString(name));
    public static bool IsExternalModuleNameRelative(ByteStringValue name) => PathIsRelative(name) || IsRootedDiskPath(name);
    public static string NormalizeSlashes(string path) => NormalizeSlashes(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue NormalizeSlashes(ByteStringValue path)
    {
        if (!path.Contains('\\'))
        {
            return path;
        }
        var builder = new GoBuilder();
        int start = 0;
        while (start < path.Length)
        {
            int next = path.Slice(start).IndexOf('\\');
            if (next < 0)
            {
                builder.WriteString(path.Slice(start));
                break;
            }
            builder.WriteString(path.Slice(start, start + next));
            builder.WriteByte('/');
            start += next + 1;
        }
        return builder.Value();
    }
    public static string GetBaseFileName(string path) => GetBaseFileName(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue GetBaseFileName(ByteStringValue path) => getBaseFileNameFromNormalized(NormalizeSlashes(path));
    private static ByteStringValue getBaseFileNameFromNormalized(ByteStringValue path)
    {
        int root = GetRootLength(path);
        if (root == path.Length)
        {
            return default;
        }
        if (path.Length > 0 && path[path.Length - 1] == '/')
        {
            path = path.Slice(0, path.Length - 1);
        }
        return path.Slice(Math.Max(GetRootLength(path), path.LastIndexOf('/') + 1));
    }
    public static bool HasExtension(string path) => HasExtension(ByteStringValue.FromString(path));
    public static bool HasExtension(ByteStringValue path) => GetBaseFileName(path).Contains('.');
    public static bool HasTrailingDirectorySeparator(string path) => HasTrailingDirectorySeparator(ByteStringValue.FromString(path));
    public static bool HasTrailingDirectorySeparator(ByteStringValue path)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }
        return path.Length > 0 && path[path.Length - 1] is '/' or '\\';
    }
    public static string RemoveTrailingDirectorySeparator(string path) => RemoveTrailingDirectorySeparator(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue RemoveTrailingDirectorySeparator(ByteStringValue path) => HasTrailingDirectorySeparator(path) ? path.Slice(0, path.Length - 1) : path;
    public static string EnsureTrailingDirectorySeparator(string path) => EnsureTrailingDirectorySeparator(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue EnsureTrailingDirectorySeparator(ByteStringValue path) => HasTrailingDirectorySeparator(path) ? path : pathConcat(path, ByteStringValue.FromString("/"));
    public static string GetDirectoryPath(string path) => GetDirectoryPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue GetDirectoryPath(ByteStringValue path)
    {
        path = NormalizeSlashes(path);
        int root = GetRootLength(path);
        if (root == path.Length)
        {
            return path;
        }
        path = RemoveTrailingDirectorySeparator(path);
        return path.Slice(0, Math.Max(root, path.LastIndexOf('/')));
    }
    public static string CombinePaths(string path, params string[] paths) =>
    combinePaths(ByteStringValue.FromString(path), paths.AsSpan(), static value => ByteStringValue.FromString(value)).Materialize()!;
    private static ByteStringValue combinePaths(ByteStringValue path, ReadOnlySpan<string> paths) =>
    combinePaths(path, paths, static value => ByteStringValue.FromString(value));
    public static ByteStringValue CombinePaths(ByteStringValue path, ByteStringValue trailing) =>
    combinePaths(path, (ReadOnlySpan<ByteStringValue>)[trailing], static value => value);
    public static ByteStringValue CombinePaths(ByteStringValue path, Slice<ByteStringValue> paths) =>
    combinePaths(path, paths.AsSpan(), static value => value);
    // path.go:87. One append-only builder keeps the original logical start
    // when a later rooted path replaces earlier components; no join per part.
    private static ByteStringValue combinePaths<T>(ByteStringValue path, ReadOnlySpan<T> paths, Func<T, ByteStringValue> getValue)
    {
        path = NormalizeSlashes(path);
        var builder = new GoBuilder();
        int size = checked(path.Length + paths.Length);
        foreach (var item in paths)
        {
            size = checked(size + getValue(item).Length);
        }
        builder.Grow(size);
        builder.WriteString(path);
        int start = 0;
        foreach (var item in paths)
        {
            var trailingPath = getValue(item);
            if (!trailingPath.IsNull && trailingPath.Length == 0)
            {
                continue;
            }
            trailingPath = NormalizeSlashes(trailingPath);
            var result = builder.Value().Slice(start);
            bool resultIsEmpty = result.Length == 0;
            bool trailingPathIsRooted = !resultIsEmpty && GetRootLength(trailingPath) != 0;
            if (resultIsEmpty || trailingPathIsRooted)
            {
                start = builder.Len();
                builder.WriteString(trailingPath);
            }
            else
            {
                if (!HasTrailingDirectorySeparator(result))
                {
                    builder.WriteByte('/');
                }
                builder.WriteString(trailingPath);
            }
        }
        return builder.Value().Slice(start);
    }
    public static List<string> GetPathComponents(string path)
    {
        var parts = GetPathComponents(ByteStringValue.FromString(path));
        var result = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            result.Add(part.Materialize()!);
        }
        return result;
    }
    public static Slice<ByteStringValue> GetPathComponents(ByteStringValue path)
    {
        path = NormalizeSlashes(path);
        return pathComponents(path, GetRootLength(path));
    }
    // path.go:138. Split borrows strings; trimming changes only its header.
    // The root literal and append retain Go's independently owned result slots.
    private static Slice<ByteStringValue> pathComponents(ByteStringValue path, int rootLength)
    {
        var root = path.Slice(0, rootLength);
        var rest = splitDynamicPath(path.Slice(rootLength));
        if (rest.Length > 0 && rest[rest.Length - 1].Length == 0)
        {
            rest = rest.Slice2(0, rest.Length - 1);
        }
        var components = Slice<ByteStringValue>.Make(1);
        components[0] = root;
        return components.Append(rest);
    }
    internal static List<string> ReducePathComponents(List<string> parts)
    {
        var input = Slice<ByteStringValue>.Make(parts.Count);
        for (int i = 0; i < parts.Count; i++)
        {
            input[i] = ByteStringValue.FromString(parts[i]);
        }
        var reduced = ReducePathComponents(input);
        var output = new List<string>(reduced.Length);
        foreach (var part in reduced)
        {
            output.Add(part.Materialize()!);
        }
        return output;
    }
    public static Slice<ByteStringValue> ReducePathComponents(Slice<ByteStringValue> components)
    {
        if (components.Length == 0)
        {
            return Slice<ByteStringValue>.Make(0);
        }
        var reduced = Slice<ByteStringValue>.Make(0).Append(components[0]);
        for (int i = 1; i < components.Length; i++)
        {
            var component = components[i];
            if (component.Length == 0 || component.EqualsBytes("."u8))
            {
                continue;
            }
            if (component.EqualsBytes(".."u8))
            {
                if (reduced.Length > 1 && !reduced[reduced.Length - 1].EqualsBytes(".."u8))
                {
                    reduced = reduced.Slice2(0, reduced.Length - 1);
                    continue;
                }
                if (reduced.Length == 1 && reduced[0].Length != 0)
                {
                    continue;
                }
            }
            reduced = reduced.Append(component);
        }
        return reduced;
    }
    public static string GetPathFromPathComponents(List<string> components)
    {
        var headers = Slice<ByteStringValue>.Make(components.Count);
        for (int i = 0; i < components.Count; i++)
        {
            headers[i] = ByteStringValue.FromString(components[i]);
        }
        return GetPathFromPathComponents(headers).Materialize()!;
    }
    public static ByteStringValue GetPathFromPathComponents(Slice<ByteStringValue> components)
    {
        if (components.Length == 0)
        {
            return default;
        }
        var root = components[0];
        if (root.Length != 0)
        {
            root = EnsureTrailingDirectorySeparator(root);
        }
        return pathConcat(root, joinPathSegments(components.Slice2(1, components.Length)));
    }
    public static string GetNormalizedAbsolutePath(string path, string directory) => GetNormalizedAbsolutePath(ByteStringValue.FromString(path), ByteStringValue.FromString(directory)).Materialize()!;
    public static ByteStringValue GetNormalizedAbsolutePath(ByteStringValue path, RootedDirectoryPath directory) => GetNormalizedAbsolutePath(path, directory.AsValue());
    public static ByteStringValue GetNormalizedAbsolutePath(ByteStringValue path, ByteStringValue directory)
    {
        path = GetRootLength(path) == 0 && directory.Length != 0 ? CombinePaths(directory, path) : NormalizeSlashes(path);
        return getNormalizedAbsolutePathFromNormalizedSlashes(path);
    }
    private static ByteStringValue getNormalizedAbsolutePathFromNormalizedSlashes(ByteStringValue fileName)
    {
        int rootLength = GetRootLength(fileName);
        var (simpleNormalized, ok) = simpleNormalizePath(fileName);
        if (ok)
        {
            int simpleLength = simpleNormalized.Length;
            if (simpleLength > rootLength)
            {
                return RemoveTrailingDirectorySeparator(simpleNormalized);
            }
            if (simpleLength == rootLength && rootLength != 0)
            {
                return EnsureTrailingDirectorySeparator(simpleNormalized);
            }
            return simpleNormalized;
        }

        int length = fileName.Length;
        ByteStringValue root = fileName.Slice(0, rootLength), normalized = default;
        bool changed = false, seenNonDotDotSegment = rootLength != 0;
        int index = rootLength, normalizedUpTo = index;
        while (index < length)
        {
            int segmentStart = index;
            int ch = fileName[index];
            while (ch == '/')
            {
                index++;
                if (index < length)
                {
                    ch = fileName[index];
                }
                else
                break;
            }

            if (index > segmentStart)
            {
                if (!changed)
                {
                    normalized = fileName.Slice(0, Math.Max(rootLength, segmentStart - 1));
                    changed = true;
                }

                if (index == length)
                {
                    break;
                }
                segmentStart = index;
            }

            int segmentEnd = fileName.Slice(index + 1).IndexOf('/');
            if (segmentEnd == -1)
            {
                segmentEnd = length;
            }
            else
            segmentEnd += index + 1;
            int segmentLength = segmentEnd - segmentStart;
            if (segmentLength == 1 && fileName[index] == '.')
            {
                if (!changed)
                {
                    normalized = fileName.Slice(0, normalizedUpTo);
                    changed = true;
                }
            }
            else if (segmentLength == 2 && fileName[index] == '.' && fileName[index + 1] == '.')
            {
                if (!seenNonDotDotSegment)
                {
                    if (changed)
                    {
                        normalized = pathConcat(normalized, ByteStringValue.FromString(normalized.Length == rootLength ? ".." : "/.."));
                    }
                    else
                    normalizedUpTo = index + 2;
                }
                else
                {
                    if (!changed)
                    {
                        // path.go:499. Borrow the search prefix; the retained result remains a borrowed header.
                        normalized = normalizedUpTo - 1 >= 0 ? fileName.Slice(0, Math.Max(rootLength, fileName.Slice(0, normalizedUpTo - 1).LastIndexOf('/'))) : fileName.Slice(0, normalizedUpTo);
                        changed = true;
                    }
                    else
                    {
                        int lastSlash = normalized.LastIndexOf('/');
                        normalized = lastSlash != -1 ? normalized.Slice(0, Math.Max(rootLength, lastSlash)) : root;
                    }

                    seenNonDotDotSegment = (normalized.Length != rootLength || rootLength != 0) && !normalized.EqualsBytes(".."u8) && !normalized.EndsWith("/.."u8);
                }
            }
            else if (changed)
            {
                if (normalized.Length != rootLength)
                {
                    normalized = pathConcat(normalized, ByteStringValue.FromString("/"));
                }
                seenNonDotDotSegment = true;
                normalized = pathConcat(normalized, fileName.Slice(segmentStart, segmentEnd));
            }
            else
            {
                seenNonDotDotSegment = true;
                normalizedUpTo = segmentEnd;
            }

            index = segmentEnd + 1;
        }

        if (changed)
        {
            return normalized;
        }
        if (length > rootLength)
        {
            while (HasTrailingDirectorySeparator(fileName))
            {
                fileName = RemoveTrailingDirectorySeparator(fileName);
            }
            return fileName;
        }

        return length == rootLength ? EnsureTrailingDirectorySeparator(fileName) : fileName;
    }
    private static (ByteStringValue Path, bool Ok) simpleNormalizePath(ByteStringValue path)
    {
        if (!hasRelativePathSegment(path))
        {
            return (path, true);
        }
        var simplified = replacePathText(path, ByteStringValue.FromString("/./"), ByteStringValue.FromString("/"));
        var trimmed = simplified.StartsWith("./"u8) ? simplified.Slice(2) : simplified;
        if (!trimmed.EqualsValue(path) && !hasRelativePathSegment(trimmed) &&
        !(!trimmed.EqualsValue(simplified) && trimmed.StartsWith("/"u8))) return (trimmed, true);
        return (default, false);
    }
    private static ByteStringValue replacePathText(ByteStringValue path, ByteStringValue oldText, ByteStringValue newText)
    {
        int found = path.IndexOf(oldText);
        if (found < 0)
        {
            return path;
        }
        var builder = new GoBuilder();
        int start = 0;
        while (found >= 0)
        {
            builder.WriteString(path.Slice(start, found));
            builder.WriteString(newText);
            start = found + oldText.Length;
            found = path.IndexOf(oldText, start);
        }
        builder.WriteString(path.Slice(start));
        return builder.Value();
    }
    public static string NormalizePath(string path) => NormalizePath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue NormalizePath(ByteStringValue path)
    {
        path = NormalizeSlashes(path);
        var result = getNormalizedAbsolutePathFromNormalizedSlashes(path);
        return result.Length != 0 && HasTrailingDirectorySeparator(path) ? EnsureTrailingDirectorySeparator(result) : result;
    }
    public static string ResolvePath(string path, params string[] paths) => NormalizePath(combinePaths(ByteStringValue.FromString(path), paths)).Materialize()!;
    public static ByteStringValue ResolvePath(ByteStringValue path, Slice<ByteStringValue> paths) => NormalizePath(CombinePaths(path, paths));
    public static ByteStringValue ResolvePath(ByteStringValue path, ByteStringValue other) => NormalizePath(CombinePaths(path, other));
    public static ByteStringValue ResolvePath(ByteStringValue path) => NormalizePath(path);
    public static string ResolvePathWithoutTrailingDirectorySeparator(string path, params string[] paths) => resolvePathWithoutTrailingDirectorySeparator(NormalizePath(combinePaths(ByteStringValue.FromString(path), paths))).Materialize()!;
    public static ByteStringValue ResolvePathWithoutTrailingDirectorySeparator(ByteStringValue path, Slice<ByteStringValue> paths) => resolvePathWithoutTrailingDirectorySeparator(ResolvePath(path, paths));
    public static ByteStringValue ResolvePathWithoutTrailingDirectorySeparator(ByteStringValue path) => resolvePathWithoutTrailingDirectorySeparator(ResolvePath(path));
    private static ByteStringValue resolvePathWithoutTrailingDirectorySeparator(ByteStringValue path) => path.Length > GetRootLength(path) ? RemoveTrailingDirectorySeparator(path) : path;
    // Scalar Unicode is an explicit disk adapter, not an internal byte algorithm.
    public static string ToFileNameLowerCase(string fileName)
    {
        var builder = new System.Text.StringBuilder(fileName.Length);
        foreach (var rune in fileName.EnumerateRunes())
        {
            builder.Append(new System.Text.Rune(rune.Value == 0x0130 ? rune.Value : UnicodeCase.ToLower(rune.Value)).ToString());
        }
        return builder.ToString();
    }
    public static string ToFileNameLowerCaseGoBytes(string bytes) => ToFileNameLowerCaseGoBytes(ByteStringValue.FromString(bytes)).Materialize()!;
    public static ByteStringValue ToFileNameLowerCaseGoBytes(ByteStringValue bytes) => GoStrings.ToFileNameLowerCase(bytes);
    public static List<string> GetPathComponentsRelativeTo(string from, string to) => projectPathComponents(getPathComponentsRelativeTo(
        ReducePathComponents(GetPathComponents(ByteStringValue.FromString(from))),
        ReducePathComponents(GetPathComponents(ByteStringValue.FromString(to))), CaseSensitivity.CaseSensitive, true));
    public static List<string> GetPathComponentsRelativeTo(string from, string to, CaseSensitivity sensitivity) => projectPathComponents(GetPathComponentsRelativeTo(ByteStringValue.FromString(from), ByteStringValue.FromString(to), sensitivity));
    internal static List<string> projectPathComponents(Slice<ByteStringValue> components)
    {
        var result = new List<string>(components.Length);
        foreach (var part in components)
        {
            result.Add(part.Materialize()!);
        }
        return result;
    }
    public static Slice<ByteStringValue> GetPathComponentsRelativeTo(ByteStringValue from, ByteStringValue to, CaseSensitivity sensitivity) => getPathComponentsRelativeTo(ReducePathComponents(GetPathComponents(from)), ReducePathComponents(GetPathComponents(to)), sensitivity);
    private static Slice<ByteStringValue> resolvePathComponentsRelativeTo(ByteStringValue from, ByteStringValue to, RootedDirectoryPath directory, CaseSensitivity sensitivity) => getPathComponentsRelativeTo(ReducePathComponents(GetPathComponents(CombinePaths(directory.AsValue(), from))), ReducePathComponents(GetPathComponents(CombinePaths(directory.AsValue(), to))), sensitivity);
    private static Slice<ByteStringValue> getPathComponentsRelativeTo(Slice<ByteStringValue> from, Slice<ByteStringValue> to, CaseSensitivity sensitivity, bool scalarRoots = false)
    {
        int start = 0, common = Math.Min(from.Length, to.Length);
        for (; start < common; start++)
        {
            bool equal;
            if (start == 0 && scalarRoots)
            {
                // Genuine legacy scalar ingress. Internal descriptor overloads
                // already contain Go bytes and never enter this adapter.
                equal = UnicodeCase.EqualFold(
                    ByteStringValue.FromView(ByteStringView.FromUnicode(from[start].Materialize()!)),
                    ByteStringValue.FromView(ByteStringView.FromUnicode(to[start].Materialize()!)));
            }
            else
            {
                equal = start == 0 || !sensitivity.IsCaseSensitive()
                    ? UnicodeCase.EqualFold(from[start], to[start])
                    : from[start].EqualsValue(to[start]);
            }
            if (!equal)
            {
                break;
            }
        }
        if (start == 0)
        {
            return to;
        }
        int dotDotCount = from.Length - start;
        var result = Slice<ByteStringValue>.Make(1 + dotDotCount + to.Length - start);
        int index = 1;
        for (int i = 0; i < dotDotCount; i++)
        {
            result[index++] = ByteStringValue.FromString("..");
        }
        to.AsSpan().Slice(start).CopyTo(result.AsSpan().Slice(index));
        return result;
    }
    public static string GetRelativePathFromDirectory(string from, string to)
    {
        if ((GetRootLength(from) > 0) != (GetRootLength(to) > 0))
        {
            throw new ArgumentException("Paths must either both be absolute or both be relative.");
        }
        return GetPathFromPathComponents(GetPathComponentsRelativeTo(from, to));
    }
    public static ByteStringValue GetRelativePathFromDirectory(ByteStringValue from, ByteStringValue to)
    {
        if ((GetRootLength(from) > 0) != (GetRootLength(to) > 0))
        {
            throw new ArgumentException("Paths must either both be absolute or both be relative.");
        }
        return GetPathFromPathComponents(GetPathComponentsRelativeTo(from, to, CaseSensitivity.CaseSensitive));
    }
    public static string ConvertToRelativePath(string path, RootedDirectoryPath directory, CaseSensitivity sensitivity) => ConvertToRelativePath(ByteStringValue.FromString(path), directory, sensitivity).Materialize()!;
    public static ByteStringValue ConvertToRelativePath(ByteStringValue path, RootedDirectoryPath directory, CaseSensitivity sensitivity) => !IsRootedDiskPath(path) ? path : ResolveRelativePathToDirectoryOrUrl(directory.AsValue(), path, false, directory, sensitivity);
    public static string GetRelativePathToDirectoryOrUrl(string from, string to, bool url, CaseSensitivity sensitivity) => GetRelativePathToDirectoryOrUrl(ByteStringValue.FromString(from), ByteStringValue.FromString(to), url, sensitivity).Materialize()!;
    public static ByteStringValue GetRelativePathToDirectoryOrUrl(ByteStringValue from, ByteStringValue to, bool url, CaseSensitivity sensitivity) => getRelativePathToDirectoryOrUrl(GetPathComponentsRelativeTo(from, to, sensitivity), url);
    public static string ResolveRelativePathToDirectoryOrUrl(string from, string to, bool url, RootedDirectoryPath directory, CaseSensitivity sensitivity) => ResolveRelativePathToDirectoryOrUrl(ByteStringValue.FromString(from), ByteStringValue.FromString(to), url, directory, sensitivity).Materialize()!;
    public static ByteStringValue ResolveRelativePathToDirectoryOrUrl(ByteStringValue from, ByteStringValue to, bool url, RootedDirectoryPath directory, CaseSensitivity sensitivity) => getRelativePathToDirectoryOrUrl(resolvePathComponentsRelativeTo(from, to, directory, sensitivity), url);
    private static ByteStringValue getRelativePathToDirectoryOrUrl(Slice<ByteStringValue> components, bool url)
    {
        var first = components[0];
        if (url && IsRootedDiskPath(first))
        {
            components[0] = pathConcat(ByteStringValue.FromString(first[0] == '/' ? "file://" : "file:///"), first);
        }
        return GetPathFromPathComponents(components);
    }
    public static string EnsurePathIsNonModuleName(string path) => EnsurePathIsNonModuleName(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue EnsurePathIsNonModuleName(ByteStringValue path) => GetRootLength(path) == 0 && !PathIsRelative(path) ? pathConcat(ByteStringValue.FromString("./"), path) : path;
    public static string GetAnyExtensionFromPath(string path, IEnumerable<string>? extensions, CaseSensitivity sensitivity) => GetAnyExtensionFromPath(ByteStringValue.FromString(path), extensions, sensitivity).Materialize()!;
    public static ByteStringValue GetAnyExtensionFromPath(ByteStringValue path, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        if (extensions != null && extensions.Any())
        {
            path = RemoveTrailingDirectorySeparator(path);
            foreach (string extension in extensions)
            {
                var result = tryGetExtensionFromPath(path, extension, sensitivity);
                if (result.Length != 0)
                {
                    return result;
                }
            }
            return default;
        }
        var name = getBaseFileNameFromNormalized(NormalizeSlashes(path));
        int dot = name.LastIndexOf('.');
        return dot >= 0 ? name.Slice(dot) : default;
    }
    public static ByteStringValue GetAnyExtensionFromPath(ByteStringValue path, Slice<ByteStringValue> extensions, CaseSensitivity sensitivity)
    {
        if (extensions.Length != 0)
        {
            path = RemoveTrailingDirectorySeparator(path);
            foreach (var extension in extensions)
            {
                var result = tryGetExtensionFromPath(path, extension, sensitivity);
                if (result.Length != 0)
                {
                    return result;
                }
            }
            return default;
        }
        return GetAnyExtensionFromPath(path, (IEnumerable<string>?)null, sensitivity);
    }
    public static string GetLongestExtensionFromPath(string path, IEnumerable<string>? extensions, CaseSensitivity sensitivity) => GetLongestExtensionFromPath(ByteStringValue.FromString(path), extensions, sensitivity).Materialize()!;
    public static ByteStringValue GetLongestExtensionFromPath(ByteStringValue path, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        path = RemoveTrailingDirectorySeparator(path);
        ByteStringValue longest = default;
        foreach (string extension in extensions ?? []) if (extension.Length > longest.Length)
        {
            var result = tryGetExtensionFromPath(path, extension, sensitivity);
            if (result.Length != 0)
            {
                longest = result;
            }
        }
        return longest;
    }
    private static ByteStringValue tryGetExtensionFromPath(ByteStringValue path, string extension, CaseSensitivity sensitivity)
        => tryGetExtensionFromPath(path, ByteStringValue.FromString(extension), sensitivity);
    private static ByteStringValue tryGetExtensionFromPath(ByteStringValue path, ByteStringValue extension, CaseSensitivity sensitivity)
    {
        if (!extension.StartsWith("."u8))
        {
            extension = pathConcat(ByteStringValue.FromString("."), extension);
        }
        if (path.Length < extension.Length || path[path.Length - extension.Length] != '.')
        {
            return default;
        }
        var actual = path.Slice(path.Length - extension.Length);
        var expected = extension;
        return (sensitivity.IsCaseSensitive() ? actual.EqualsValue(expected) : UnicodeCase.EqualFold(actual, expected)) ? actual : default;
    }
    internal static bool hasRelativePathSegment(string path) => hasRelativePathSegment(ByteStringValue.FromString(path));
    internal static bool hasRelativePathSegment(ByteStringValue p)
    {
        int n = p.Length;
        if (n == 0)
        {
            return false;
        }
        if (p.EqualsBytes("."u8) || p.EqualsBytes(".."u8))
        {
            return true;
        }
        if (p[0] == '.')
        {
            if (n >= 2 && p[1] == '/')
            {
                return true;
            }
            if (n >= 3 && p[1] == '.' && p[2] == '/')
            {
                return true;
            }
        }

        if (p[n - 1] == '.')
        {
            if (n >= 2 && p[n - 2] == '/')
            {
                return true;
            }
            if (n >= 3 && p[n - 2] == '.' && p[n - 3] == '/')
            {
                return true;
            }
        }

        bool prevSlash = false;
        int segLen = 0, dotCount = 0;
        for (int i = 0; i < n; i++)
        {
            int c = p[i];
            if (c == '/')
            {
                if (prevSlash)
                {
                    return true;
                }
                if ((segLen == 1 && dotCount == 1) || (segLen == 2 && dotCount == 2))
                {
                    return true;
                }
                prevSlash = true;
                segLen = dotCount = 0;
                continue;
            }

            if (c == '.')
            {
                if (dotCount >= 0)
                {
                    dotCount++;
                }
            }
            else
            dotCount = -1;
            segLen++;
            prevSlash = false;
        }

        return (segLen == 1 && dotCount == 1) || (segLen == 2 && dotCount == 2);
    }
}
public enum CaseSensitivity : byte
{
    CaseInsensitive, CaseSensitive
}
public static class CaseSensitivityOperations
{
    public static bool IsCaseSensitive(this CaseSensitivity value) => value == CaseSensitivity.CaseSensitive;
    public static bool IsCaseInsensitive(this CaseSensitivity value) => value == CaseSensitivity.CaseInsensitive;
    public static string String(this CaseSensitivity value) => value switch
    {
        CaseSensitivity.CaseInsensitive => "false", CaseSensitivity.CaseSensitive => "true", _ => "unknown"
    }
    ;
    public static Func<string, string, int> GetComparer(this CaseSensitivity value) => (left, right) => value.CompareStrings(left, right);
    public static string Canonicalize(this CaseSensitivity value, string bytes) => value.Canonicalize(ByteStringValue.FromString(bytes)).Materialize()!;
    public static ByteStringValue Canonicalize(this CaseSensitivity value, ByteStringValue bytes) => value.IsCaseSensitive() ? bytes : TsPath.ToFileNameLowerCaseGoBytes(bytes);
    public static (string Remaining, bool Ok) TrimPrefix(this CaseSensitivity value, string text, string prefix)
    {
        var (remaining, ok) = value.TrimPrefix(ByteStringValue.FromString(text), ByteStringValue.FromString(prefix));
        return (remaining.Materialize()!, ok);
    }
    public static (ByteStringValue Remaining, bool Ok) TrimPrefix(this CaseSensitivity value, ByteStringValue text, ByteStringValue prefix)
    {
        if (value.IsCaseSensitive())
        {
            return text.StartsWith(prefix) ? (text.Slice(prefix.Length), true) : (text, false);
        }
        var canonical = value.Canonicalize(prefix);
        if (!value.Canonicalize(text).StartsWith(canonical))
        {
            return (text, false);
        }
        int count = 0;
        foreach (var rune in GoString.EnumerateRunes(canonical))
        {
            count++;
        }
        int offset = 0;
        for (int i = 0; i < count && offset < text.Length; i++)
        {
            offset += (text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[offset..]) : Utf8.DecodeRuneInString(text.CharSpan[offset..])).Size;
        }
        return (text.Slice(offset), true);
    }
    public static int CompareStrings(this CaseSensitivity value, string a, string b) => value.CompareValues(ByteStringValue.FromString(a), ByteStringValue.FromString(b));
    public static int CompareValues(this CaseSensitivity value, ByteStringValue a, ByteStringValue b)
    {
        if (a.IsNull || b.IsNull)
        {
            throw new NullReferenceException();
        }
        if (value.IsCaseSensitive())
        {
            return Math.Sign(ByteStringKey.FromValue(a).CompareTo(ByteStringKey.FromValue(b)));
        }
        int ai = 0, bi = 0;
        while (ai < a.Length && bi < b.Length)
        {
            var (ar, sa) = a.IsBytes ? Utf8.DecodeRuneInString(a.ByteSpan[ai..]) : Utf8.DecodeRuneInString(a.CharSpan[ai..]);
            var (br, sb) = b.IsBytes ? Utf8.DecodeRuneInString(b.ByteSpan[bi..]) : Utf8.DecodeRuneInString(b.CharSpan[bi..]);
            int result = UnicodeCase.ToLower(ar).CompareTo(UnicodeCase.ToLower(br));
            if (result != 0)
            {
                return result;
            }
            ai += sa;
            bi += sb;
        }
        return (a.Length - ai).CompareTo(b.Length - bi);
    }
}
