// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/path.go syntax-reference helpers.
// Pinned upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    // path.go:1180. Structural operation over original Go bytes.
    public static bool HasExtension(string fileName) => GetBaseFileName(fileName).Contains('.');

    private const string DynamicURIFileNamePrefix = "^/~ts-uri~/";
    private const string urlSchemeSeparator = "://";
    public static bool IsVolumeCharacter(int ch) => ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z';
    private static int getFileUrlVolumeSeparatorEnd(string url, int start)
    {
        if (url.Length <= start)
            return -1;
        int ch0 = url[start];
        if (ch0 == ':')
            return start + 1;
        if (ch0 == '%' && url.Length > start + 2 && url[start + 1] == '3')
        {
            int ch2 = url[start + 2];
            if (ch2 == 'a' || ch2 == 'A')
                return start + 3;
        }

        return -1;
    }

    public static int GetEncodedRootLength(string path)
    {
        int ln = path.Length;
        if (ln == 0)
            return 0;
        int ch0 = path[0];
        if (ch0 == '/' || ch0 == '\\')
        {
            if (ln == 1 || path[1] != ch0)
                return 1;
            int offset = 2;
            int p1 = path[offset..].IndexOf((char)ch0);
            if (p1 < 0)
                return ln;
            return p1 + offset + 1;
        }

        if (IsVolumeCharacter(ch0) && ln > 1 && path[1] == ':')
        {
            if (ln == 2)
                return 2;
            int ch2 = path[2];
            if (ch2 == '/' || ch2 == '\\')
                return 3;
        }

        if (ch0 == '^' && ln > 1 && path[1] == '/')
        {
            if (GoStrings.HasPrefix(path, DynamicURIFileNamePrefix))
            {
                int dynamicSchemeEnd = path[DynamicURIFileNamePrefix.Length..].IndexOf('/');
                if (dynamicSchemeEnd != -1)
                {
                    int dynamicAuthorityStart = DynamicURIFileNamePrefix.Length + dynamicSchemeEnd + 1;
                    int dynamicAuthorityEnd = path[dynamicAuthorityStart..].IndexOf('/');
                    if (dynamicAuthorityEnd != -1)
                        return dynamicAuthorityStart + dynamicAuthorityEnd + 1;
                    return ~ln;
                }
            }

            return 2;
        }

        int schemeEnd = GoStrings.Index(path, urlSchemeSeparator);
        if (schemeEnd != -1)
        {
            int authorityStart = schemeEnd + urlSchemeSeparator.Length;
            int authorityLength = GoStrings.Index(path[authorityStart..], "/");
            if (authorityLength != -1)
            {
                int authorityEnd = authorityStart + authorityLength;
                string scheme = path[..schemeEnd];
                string authority = path[authorityStart..authorityEnd];
                if (UnicodeCase.EqualFold(scheme, "file") && (authority == "" || UnicodeCase.EqualFold(authority, "localhost")) && path.Length > authorityEnd + 2 && IsVolumeCharacter(path[authorityEnd + 1]))
                {
                    int volumeSeparatorEnd = getFileUrlVolumeSeparatorEnd(path, authorityEnd + 2);
                    if (volumeSeparatorEnd != -1)
                    {
                        if (volumeSeparatorEnd == path.Length)
                            return ~volumeSeparatorEnd;
                        if (path[volumeSeparatorEnd] == '/')
                            return ~(volumeSeparatorEnd + 1);
                    }
                }

                return ~(authorityEnd + 1);
            }

            return ~ln;
        }

        return 0;
    }

    public static bool IsDynamicFileName(string path) => path.StartsWith("^/", StringComparison.Ordinal);
    public static bool PathIsAbsolute(string path) => GetEncodedRootLength(path) != 0;
    public static bool IsRootedDiskPath(string path) => GetEncodedRootLength(path) > 0;
    public static int GetRootLength(string path)
    {
        var rootLength = GetEncodedRootLength(path);
        return rootLength < 0 ? ~rootLength : rootLength;
    }

    public static string NormalizeSlashes(string path) => path.Replace('\\', '/');
    public static string GetBaseFileName(string path) => getBaseFileNameFromNormalized(NormalizeSlashes(path));
    private static string getBaseFileNameFromNormalized(string path)
    {
        var rootLength = GetRootLength(path);
        if (rootLength == path.Length)
            return "";
        if (path.Length > 0 && path[^1] == '/')
            path = path[..^1];
        return path[Math.Max(GetRootLength(path), path.LastIndexOf('/') + 1)..];
    }

    public static bool PathIsRelative(string path)
    {
        if (path == "." || path == "..")
            return true;
        if (path.Length >= 2 && path[0] == '.' && (path[1] == '/' || path[1] == '\\'))
            return true;
        if (path.Length >= 3 && path[0] == '.' && path[1] == '.' && (path[2] == '/' || path[2] == '\\'))
            return true;
        return false;
    }

    public static bool IsExternalModuleNameRelative(string moduleName) => PathIsRelative(moduleName) || IsRootedDiskPath(moduleName);
    public static bool HasTrailingDirectorySeparator(string path) => path.Length > 0 && path[^1] is '/' or '\\';
    public static string RemoveTrailingDirectorySeparator(string path) => HasTrailingDirectorySeparator(path) ? path[..^1] : path;
    public static string EnsureTrailingDirectorySeparator(string path) => HasTrailingDirectorySeparator(path) ? path : path + "/";
    public static string GetDirectoryPath(string path)
    {
        path = NormalizeSlashes(path);
        int root = GetRootLength(path);
        if (root == path.Length)
            return path;
        path = RemoveTrailingDirectorySeparator(path);
        return path[..Math.Max(root, path.LastIndexOf('/'))];
    }

    public static string CombinePaths(string firstPath, params string[] paths)
    {
        string result = NormalizeSlashes(firstPath);
        foreach (string value in paths)
        {
            if (value == "")
                continue;
            string trailing = NormalizeSlashes(value);
            if (result == "" || GetRootLength(trailing) != 0)
                result = trailing;
            else
                result = EnsureTrailingDirectorySeparator(result) + trailing;
        }

        return result;
    }

    public static List<string> GetPathComponents(string path)
    {
        path = NormalizeSlashes(path);
        int root = GetRootLength(path);
        var rest = path[root..].Split('/').ToList();
        if (rest.Count > 0 && rest[^1] == "")
            rest.RemoveAt(rest.Count - 1);
        rest.Insert(0, path[..root]);
        return rest;
    }

    internal static List<string> ReducePathComponents(List<string> components)
    {
        if (components.Count == 0)
            return[];
        var reduced = new List<string>
        {
            components[0]
        };
        for (int i = 1; i < components.Count; i++)
        {
            string component = components[i];
            if (component is "" or ".")
                continue;
            if (component == "..")
            {
                if (reduced.Count > 1 && reduced[^1] != "..")
                {
                    reduced.RemoveAt(reduced.Count - 1);
                    continue;
                }

                if (reduced.Count == 1 && reduced[0] != "")
                    continue;
            }

            reduced.Add(component);
        }

        return reduced;
    }

    public static string GetPathFromPathComponents(List<string> components) => components.Count == 0 ? "" : (components[0] == "" ? "" : EnsureTrailingDirectorySeparator(components[0])) + string.Join('/', components.Skip(1));
    public static string GetNormalizedAbsolutePath(string fileName, string currentDirectory)
    {
        fileName = GetRootLength(fileName) == 0 && currentDirectory != "" ? CombinePaths(currentDirectory, fileName) : NormalizeSlashes(fileName);
        return getNormalizedAbsolutePathFromNormalizedSlashes(fileName);
    }

    // path.go:424. The upstream simpleNormalizePath optimization is omitted;
    // the original general normalization algorithm is retained for every path.
    private static string getNormalizedAbsolutePathFromNormalizedSlashes(string fileName)
    {
        if (fileName == "")
            return "";
        int rootLength = GetRootLength(fileName), length = fileName.Length;
        string root = fileName[..rootLength], normalized = "";
        bool changed = false, seenNonDotDotSegment = rootLength != 0;
        int index = rootLength, normalizedUpTo = index;
        while (index < length)
        {
            int segmentStart = index;
            char ch = fileName[index];
            while (ch == '/')
            {
                index++;
                if (index < length)
                    ch = fileName[index];
                else
                    break;
            }

            if (index > segmentStart)
            {
                if (!changed)
                {
                    normalized = fileName[..Math.Max(rootLength, segmentStart - 1)];
                    changed = true;
                }

                if (index == length)
                    break;
                segmentStart = index;
            }

            int segmentEnd = fileName.IndexOf('/', index + 1);
            if (segmentEnd == -1)
                segmentEnd = length;
            int segmentLength = segmentEnd - segmentStart;
            if (segmentLength == 1 && fileName[index] == '.')
            {
                if (!changed)
                {
                    normalized = fileName[..normalizedUpTo];
                    changed = true;
                }
            }
            else if (segmentLength == 2 && fileName[index] == '.' && fileName[index + 1] == '.')
            {
                if (!seenNonDotDotSegment)
                {
                    if (changed)
                        normalized += normalized.Length == rootLength ? ".." : "/..";
                    else
                        normalizedUpTo = index + 2;
                }
                else
                {
                    if (!changed)
                    {
                        normalized = normalizedUpTo - 1 >= 0 ? fileName[..Math.Max(rootLength, fileName[..(normalizedUpTo - 1)].LastIndexOf('/'))] : fileName[..normalizedUpTo];
                        changed = true;
                    }
                    else
                    {
                        int lastSlash = normalized.LastIndexOf('/');
                        normalized = lastSlash != -1 ? normalized[..Math.Max(rootLength, lastSlash)] : root;
                    }

                    seenNonDotDotSegment = (normalized.Length != rootLength || rootLength != 0) && normalized != ".." && !normalized.EndsWith("/..", StringComparison.Ordinal);
                }
            }
            else if (changed)
            {
                if (normalized.Length != rootLength)
                    normalized += "/";
                seenNonDotDotSegment = true;
                normalized += fileName[segmentStart..segmentEnd];
            }
            else
            {
                seenNonDotDotSegment = true;
                normalizedUpTo = segmentEnd;
            }

            index = segmentEnd + 1;
        }

        if (changed)
            return normalized;
        if (length > rootLength)
        {
            while (HasTrailingDirectorySeparator(fileName))
                fileName = RemoveTrailingDirectorySeparator(fileName);
            return fileName;
        }

        return length == rootLength ? EnsureTrailingDirectorySeparator(fileName) : fileName;
    }

    public static string NormalizePath(string path)
    {
        path = NormalizeSlashes(path);
        string result = getNormalizedAbsolutePathFromNormalizedSlashes(path);
        return result != "" && HasTrailingDirectorySeparator(path) ? EnsureTrailingDirectorySeparator(result) : result;
    }

    public static string ResolvePath(string path, params string[] paths) => NormalizePath(paths.Length > 0 ? CombinePaths(path, paths) : NormalizeSlashes(path));
    public static string ResolvePathWithoutTrailingDirectorySeparator(string path, params string[] paths)
    {
        string resolved = ResolvePath(path, paths);
        return resolved.Length > GetRootLength(resolved) ? RemoveTrailingDirectorySeparator(resolved) : resolved;
    }

    // path.go:665. Unicode lowering uses the pinned Go tables; dotted capital I
    // retains its special filename identity. Input/output here are .NET disk paths.
    public static string ToFileNameLowerCase(string fileName)
    {
        var builder = new System.Text.StringBuilder(fileName.Length);
        foreach (var rune in fileName.EnumerateRunes())
            builder.Append(new System.Text.Rune(rune.Value == 0x0130 ? rune.Value : UnicodeCase.ToLower(rune.Value)).ToString());
        return builder.ToString();
    }

    // path.go:665, original-byte counterpart for pure configuration/VFS paths.
    public static string ToFileNameLowerCaseGoBytes(string bytes)
    {
        var builder = new GoBuilder();
        foreach (int rune in GoString.Runes(bytes))
            builder.WriteRune(rune == 0x130 ? rune : UnicodeCase.ToLower(rune));
        return builder.String();
    }

    // path.go:737/753/795. Case-sensitive project paths share a POSIX root;
    // retain component reduction and the original relative component assembly.
    public static List<string> GetPathComponentsRelativeTo(string from, string to)
    {
        var first = ReducePathComponents(GetPathComponents(from));
        var second = ReducePathComponents(GetPathComponents(to));
        int start = 0, common = Math.Min(first.Count, second.Count);
        for (; start < common; start++)
        {
            bool equal = start == 0 ? UnicodeCase.EqualFold(GoString.ToUtf8(first[start]), GoString.ToUtf8(second[start])) : first[start] == second[start];
            if (!equal)
                break;
        }

        if (start == 0)
            return second;
        List<string> result = [""];
        for (int i = start; i < first.Count; i++)
            result.Add("..");
        for (int i = start; i < second.Count; i++)
            result.Add(second[i]);
        return result;
    }

    public static string GetRelativePathFromDirectory(string fromDirectory, string to)
    {
        if ((GetRootLength(fromDirectory) > 0) != (GetRootLength(to) > 0))
            throw new ArgumentException("Paths must either both be absolute or both be relative.");
        return GetPathFromPathComponents(GetPathComponentsRelativeTo(fromDirectory, to));
    }

    // Source byte-oriented overloads; the legacy two-argument scalar API above
    // remains the established Projects adapter.
    public static string ConvertToRelativePath(string absoluteOrRelativePath, RootedDirectoryPath currentDirectory, CaseSensitivity sensitivity)
    {
        if (!IsRootedDiskPath(absoluteOrRelativePath))
            return absoluteOrRelativePath;
        return ResolveRelativePathToDirectoryOrUrl(currentDirectory.AsString(), absoluteOrRelativePath, false, currentDirectory, sensitivity);
    }

    public static List<string> GetPathComponentsRelativeTo(string from, string to, CaseSensitivity sensitivity) => getPathComponentsRelativeTo(ReducePathComponents(GetPathComponents(from)), ReducePathComponents(GetPathComponents(to)), sensitivity);
    private static List<string> resolvePathComponentsRelativeTo(string from, string to, RootedDirectoryPath currentDirectory, CaseSensitivity sensitivity) => getPathComponentsRelativeTo(ReducePathComponents(GetPathComponents(CombinePaths(currentDirectory.AsString(), from))), ReducePathComponents(GetPathComponents(CombinePaths(currentDirectory.AsString(), to))), sensitivity);
    private static List<string> getPathComponentsRelativeTo(List<string> from, List<string> to, CaseSensitivity sensitivity)
    {
        int start = 0, maxCommonComponents = Math.Min(from.Count, to.Count);
        for (; start < maxCommonComponents; start++)
            if (!(start == 0 || !sensitivity.IsCaseSensitive() ? UnicodeCase.EqualFold(from[start], to[start]) : from[start] == to[start]))
                break;
        if (start == 0)
            return to;
        List<string> result = [""];
        for (int i = start; i < from.Count; i++)
            result.Add("..");
        for (int i = start; i < to.Count; i++)
            result.Add(to[i]);
        return result;
    }

    public static string GetRelativePathToDirectoryOrUrl(string directoryPathOrUrl, string relativeOrAbsolutePath, bool isAbsolutePathAnUrl, CaseSensitivity sensitivity) => getRelativePathToDirectoryOrUrl(GetPathComponentsRelativeTo(directoryPathOrUrl, relativeOrAbsolutePath, sensitivity), isAbsolutePathAnUrl);
    public static string ResolveRelativePathToDirectoryOrUrl(string directoryPathOrUrl, string relativeOrAbsolutePath, bool isAbsolutePathAnUrl, RootedDirectoryPath currentDirectory, CaseSensitivity sensitivity) => getRelativePathToDirectoryOrUrl(resolvePathComponentsRelativeTo(directoryPathOrUrl, relativeOrAbsolutePath, currentDirectory, sensitivity), isAbsolutePathAnUrl);
    private static string getRelativePathToDirectoryOrUrl(List<string> components, bool isAbsolutePathAnUrl)
    {
        string first = components[0];
        if (isAbsolutePathAnUrl && IsRootedDiskPath(first))
            components[0] = (first[0] == '/' ? "file://" : "file:///") + first;
        return GetPathFromPathComponents(components);
    }

    public static string EnsurePathIsNonModuleName(string path) => GetRootLength(path) == 0 && !PathIsRelative(path) ? "./" + path : path;
    public static string GetAnyExtensionFromPath(string path, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        if (extensions != null && extensions.Any())
        {
            path = RemoveTrailingDirectorySeparator(path);
            foreach (string extension in extensions)
            {
                string result = tryGetExtensionFromPath(path, extension, sensitivity);
                if (result != "")
                    return result;
            }

            return "";
        }

        string name = getBaseFileNameFromNormalized(NormalizeSlashes(path));
        int dot = name.LastIndexOf('.');
        return dot >= 0 ? name[dot..] : "";
    }

    public static string GetLongestExtensionFromPath(string path, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        path = RemoveTrailingDirectorySeparator(path);
        string longest = "";
        foreach (string extension in extensions ?? [])
            if (extension.Length > longest.Length)
            {
                string matched = tryGetExtensionFromPath(path, extension, sensitivity);
                if (matched != "")
                    longest = matched;
            }

        return longest;
    }

    private static string tryGetExtensionFromPath(string path, string extension, CaseSensitivity sensitivity)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        if (path.Length < extension.Length || path[path.Length - extension.Length] != '.')
            return "";
        string actual = path[(path.Length - extension.Length)..];
        return (sensitivity.IsCaseSensitive() ? actual == extension : UnicodeCase.EqualFold(actual, extension)) ? actual : "";
    }

    // path.go:554. Needed by normalized typed-path admission, including //.
    internal static bool hasRelativePathSegment(string p)
    {
        int n = p.Length;
        if (n == 0)
            return false;
        if (p is "." or "..")
            return true;
        if (p[0] == '.')
        {
            if (n >= 2 && p[1] == '/')
                return true;
            if (n >= 3 && p[1] == '.' && p[2] == '/')
                return true;
        }

        if (p[n - 1] == '.')
        {
            if (n >= 2 && p[n - 2] == '/')
                return true;
            if (n >= 3 && p[n - 2] == '.' && p[n - 3] == '/')
                return true;
        }

        bool prevSlash = false;
        int segLen = 0, dotCount = 0;
        for (int i = 0; i < n; i++)
        {
            char c = p[i];
            if (c == '/')
            {
                if (prevSlash)
                    return true;
                if ((segLen == 1 && dotCount == 1) || (segLen == 2 && dotCount == 2))
                    return true;
                prevSlash = true;
                segLen = dotCount = 0;
                continue;
            }

            if (c == '.')
            {
                if (dotCount >= 0)
                    dotCount++;
            }
            else
                dotCount = -1;
            segLen++;
            prevSlash = false;
        }

        return (segLen == 1 && dotCount == 1) || (segLen == 2 && dotCount == 2);
    }
}

// path.go:1003 et seq. Canonical identity is distinct from EqualFold matching.
public enum CaseSensitivity : byte
{
    CaseInsensitive,
    CaseSensitive
}

public static class CaseSensitivityOperations
{
    public static bool IsCaseSensitive(this CaseSensitivity value) => value == CaseSensitivity.CaseSensitive;
    public static bool IsCaseInsensitive(this CaseSensitivity value) => value == CaseSensitivity.CaseInsensitive;
    public static string String(this CaseSensitivity value) => value switch
    {
        CaseSensitivity.CaseInsensitive => "false",
        CaseSensitivity.CaseSensitive => "true",
        _ => "unknown"
    };
    public static Func<string, string, int> GetComparer(this CaseSensitivity value) => (left, right) => value.CompareStrings(left, right);
    public static string Canonicalize(this CaseSensitivity value, string bytes)
    {
        if (value.IsCaseSensitive())
            return bytes;
        return TsPath.ToFileNameLowerCaseGoBytes(bytes);
    }

    public static (string Remaining, bool Ok) TrimPrefix(this CaseSensitivity value, string text, string prefix)
    {
        if (value.IsCaseSensitive())
            return text.StartsWith(prefix, StringComparison.Ordinal) ? (text[prefix.Length..], true) : (text, false);
        string canonical = value.Canonicalize(prefix);
        if (!value.Canonicalize(text).StartsWith(canonical, StringComparison.Ordinal))
            return (text, false);
        int count = GoString.Runes(canonical).Length, offset = 0;
        for (int i = 0; i < count && offset < text.Length; i++)
            offset += Utf8.DecodeRuneInString(text.AsSpan(offset)).Size;
        return (text[offset..], true);
    }

    public static int CompareStrings(this CaseSensitivity value, string a, string b)
    {
        if (value.IsCaseSensitive())
            return Math.Sign(string.CompareOrdinal(a, b));
        int ai = 0, bi = 0;
        while (ai < a.Length && bi < b.Length)
        {
            var(ar, sa) = Utf8.DecodeRuneInString(a.AsSpan(ai));
            var(br, sb) = Utf8.DecodeRuneInString(b.AsSpan(bi));
            int result = UnicodeCase.ToLower(ar).CompareTo(UnicodeCase.ToLower(br));
            if (result != 0)
                return result;
            ai += sa;
            bi += sb;
        }

        return (a.Length - ai).CompareTo(b.Length - bi);
    }


}
