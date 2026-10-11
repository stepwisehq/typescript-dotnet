// Modified C# port of the borrowed string path operations in tspath/path.go.
// The final rooted path owns its existing raw string representation; candidate
// names and their components remain borrowed headers until that publication.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    internal static ByteStringValue GetBaseFileName(ByteStringValue path) => getBaseFileNameFromNormalized(NormalizeSlashes(path));
    private static ByteStringValue getBaseFileNameFromNormalized(ByteStringValue path)
    {
        if (GetRootLength(path) == path.Length) return default;
        if (path.Length > 0 && path[path.Length - 1] == '/') path = path.Slice(0, path.Length - 1);
        int slash = path.IsBytes ? path.ByteSpan.LastIndexOf((byte)'/') : path.CharSpan.LastIndexOf('/');
        return path.Slice(Math.Max(GetRootLength(path), slash + 1));
    }
    internal static bool HasExtension(ByteStringValue path) => GetBaseFileName(path).Contains('.');
    internal static bool IsDeclarationFileName(ByteStringValue path) => GetDeclarationFileExtension(path).Length != 0;
    internal static ByteStringValue GetDeclarationFileExtension(ByteStringValue path)
    {
        var name = GetBaseFileName(path);
        foreach (var extension in supportedDeclarationExtensions)
            if (name.EndsWith(ByteStringValue.FromString(extension))) return ByteStringValue.FromString(extension);
        if (name.EndsWith(".ts"u8))
        {
            int index = name.IndexOf(".d."u8);
            if (index >= 0) return name.Slice(index);
        }
        return default;
    }
    internal static bool FileExtensionIs(ByteStringValue path, string extension)
    {
        if (path.IsNull) throw new NullReferenceException();
        return path.Length > extension.Length && path.EndsWith(ByteStringValue.FromString(extension));
    }
    internal static string TryExtractTSExtension(ByteStringValue path)
    {
        foreach (var extension in supportedTSExtensionsForExtractExtension)
            if (FileExtensionIs(path, extension)) return extension;
        return "";
    }
    internal static bool HasTSFileExtension(ByteStringValue path)
    {
        foreach (var extension in SupportedTSExtensionsFlat)
            if (FileExtensionIs(path, extension)) return true;
        return false;
    }
    internal static ByteStringValue GetAnyExtensionFromPath(ByteStringValue path, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        if (extensions != null && extensions.Any())
        {
            if (HasTrailingDirectorySeparator(path)) path = path.Slice(0, path.Length - 1);
            foreach (var extension in extensions)
            {
                string expected = extension.StartsWith('.') ? extension : "." + extension;
                if (path.Length < expected.Length || path[path.Length - expected.Length] != '.') continue;
                var actual = path.Slice(path.Length - expected.Length);
                if (sensitivity.IsCaseSensitive() ? actual.EqualsString(expected) : equalFoldPathPart(actual, expected)) return actual;
            }
            return default;
        }
        var name = GetBaseFileName(path);
        int dot = name.IsBytes ? name.ByteSpan.LastIndexOf((byte)'.') : name.CharSpan.LastIndexOf('.');
        return dot >= 0 ? name.Slice(dot) : default;
    }
    internal static int GetEncodedRootLength(ByteStringValue path)
    {
        if (path.IsNull) throw new NullReferenceException();
        int length = path.Length;
        if (length == 0) return 0;
        int first = path[0];
        if (first is '/' or '\\')
        {
            if (length == 1 || path[1] != first) return 1;
            int next = path.Slice(2).IndexOf(first);
            return next < 0 ? length : next + 3;
        }
        if (IsVolumeCharacter(first) && length > 1 && path[1] == ':')
        {
            if (length == 2) return 2;
            if (path[2] is '/' or '\\') return 3;
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
        if (scheme < 0) return 0;
        int start = scheme + 3;
        int authorityLength = path.Slice(start).IndexOf('/');
        if (authorityLength < 0) return ~length;
        int end = start + authorityLength;
        if (equalFoldPathPart(path.Slice(0, scheme), "file") && (end == start || equalFoldPathPart(path.Slice(start, end), "localhost"))
            && length > end + 2 && IsVolumeCharacter(path[end + 1]))
        {
            int volumeEnd = -1, offset = end + 2;
            if (path[offset] == ':') volumeEnd = offset + 1;
            else if (path[offset] == '%' && length > offset + 2 && path[offset + 1] == '3' && path[offset + 2] is 'a' or 'A') volumeEnd = offset + 3;
            if (volumeEnd >= 0)
            {
                if (volumeEnd == length) return ~volumeEnd;
                if (path[volumeEnd] == '/') return ~(volumeEnd + 1);
            }
        }
        return ~(end + 1);
    }
    private static bool equalFoldPathPart(ByteStringValue value, string expected)
    {
        if (!value.IsBytes) return UnicodeCase.EqualFold(value.CharSpan, expected.AsSpan());
        char[]? rented = null;
        Span<char> chars = value.Length <= 256 ? stackalloc char[value.Length]
            : (rented = System.Buffers.ArrayPool<char>.Shared.Rent(value.Length)).AsSpan(0, value.Length);
        try { System.Text.Encoding.Latin1.GetChars(value.ByteSpan, chars); return UnicodeCase.EqualFold(chars, expected.AsSpan()); }
        finally { if (rented is not null) System.Buffers.ArrayPool<char>.Shared.Return(rented); }
    }
    internal static int GetRootLength(ByteStringValue path) { int root = GetEncodedRootLength(path); return root < 0 ? ~root : root; }
    internal static bool HasTrailingDirectorySeparator(ByteStringValue path)
    {
        if (path.IsNull) throw new NullReferenceException();
        return path.Length > 0 && path[path.Length - 1] is '/' or '\\';
    }
    internal static ByteStringValue NormalizeSlashes(ByteStringValue path)
    {
        if (!path.Contains('\\')) return path;
        var builder = new GoBuilder();
        int start = 0;
        while (start < path.Length)
        {
            int next = path.Slice(start).IndexOf('\\');
            if (next < 0) { builder.WriteString(path.Slice(start)); break; }
            builder.WriteString(path.Slice(start, start + next)); builder.WriteByte('/'); start += next + 1;
        }
        return builder.Value();
    }
    internal static Slice<ByteStringValue> GetPathComponents(ByteStringValue path)
    {
        path = NormalizeSlashes(path);
        int root = GetRootLength(path), start = root;
        var parts = Slice<ByteStringValue>.Make(0).Append(path.Slice(0, root));
        while (true)
        {
            int next = path.Slice(start).IndexOf('/');
            if (next < 0)
            {
                if (start < path.Length) parts = parts.Append(path.Slice(start));
                return parts;
            }
            parts = parts.Append(path.Slice(start, start + next)); start += next + 1;
        }
    }
    internal static ByteStringValue CombinePaths(ByteStringValue first, ByteStringValue trailing)
    {
        first = NormalizeSlashes(first);
        if (trailing.Length == 0) return first;
        trailing = NormalizeSlashes(trailing);
        if (first.Length == 0 || GetRootLength(trailing) != 0) return trailing;
        var builder = new GoBuilder(); builder.WriteString(first);
        if (!HasTrailingDirectorySeparator(first)) builder.WriteByte('/');
        builder.WriteString(trailing); return builder.Value();
    }
    internal static ByteStringValue CombinePaths(ByteStringValue first, Slice<ByteStringValue> paths)
    {
        var result = NormalizeSlashes(first);
        foreach (var path in paths) result = CombinePaths(result, path);
        return result;
    }
    // ToRootedPath's validation stays before the complete path is constructed.
    // Normalize once after joining, instead of materializing a source name merely
    // to pass it into a string-taking path convenience overload.
    internal static RootedPath ToRootedPath(ByteStringValue path, RootedDirectoryPath directory)
    {
        if (path.IsNull) throw new NullReferenceException();
        if (path.Length == 0) throw new ArgumentException("path must not be empty");
        int root = GetEncodedRootLength(path);
        int scheme = path.IsBytes ? path.ByteSpan.IndexOf("://"u8) : path.CharSpan.IndexOf("://".AsSpan());
        if (root < 0 && scheme >= 0 && (path.Slice(scheme + 3).Contains('?') || path.Slice(scheme + 3).Contains('#')))
            throw new ArgumentException("path must not contain a URL query or fragment");
        if (root == 0 && hasURLRoot(directory.AsString()) && (path.Contains('?') || path.Contains('#')))
            throw new ArgumentException("relative URL path must not contain a query or fragment");
        var joined = GetRootLength(path) == 0 && directory != default ? CombinePaths(ByteStringValue.FromString(directory.AsString()), path) : NormalizeSlashes(path);
        // This is the final owned normalized path, not a retained module-name key.
        string normalized = getNormalizedAbsolutePathFromNormalizedSlashes(joined.Materialize()!);
        if (GetEncodedRootLength(normalized) == 0 || hasRootedURLSuffix(normalized)) throw new ArgumentException("path must be rooted");
        return new(ensureRootedPathRootSeparator(normalized));
    }
    internal static ByteStringValue EncodeDynamicModuleSpecifier(ByteStringValue specifier) => encodeDynamicModuleSpecifier(specifier, true);
    internal static ByteStringValue EncodeDynamicDirectorySpecifier(ByteStringValue specifier) => encodeDynamicModuleSpecifier(specifier, false);
    private static ByteStringValue encodeDynamicModuleSpecifier(ByteStringValue specifier, bool preserveExtension)
    {
        bool needsEncoding = specifier.IsBytes
            ? specifier.ByteSpan.IndexOf("~ts-uri-escape~"u8) >= 0 || specifier.ByteSpan.IndexOf("~ts-uri-spec~"u8) >= 0
            : specifier.CharSpan.IndexOf(dynamicURIPathSegmentEscapePrefix.AsSpan()) >= 0 || specifier.CharSpan.IndexOf(dynamicURIModuleSpecifierEscapePrefix.AsSpan()) >= 0;
        if (!needsEncoding) return specifier;
        var builder = new GoBuilder();
        while (true)
        {
            int separator = specifier.IsBytes ? specifier.ByteSpan.IndexOfAny((byte)'/', (byte)'\\') : specifier.CharSpan.IndexOfAny('/', '\\');
            if (separator < 0) { builder.WriteString(encodeDynamicModuleSpecifierSegment(specifier, preserveExtension)); return builder.Value(); }
            builder.WriteString(encodeDynamicModuleSpecifierSegment(specifier.Slice(0, separator), false));
            builder.WriteByte(specifier[separator]); specifier = specifier.Slice(separator + 1);
        }
    }
    private static ByteStringValue encodeDynamicModuleSpecifierSegment(ByteStringValue segment, bool preserveExtension)
    {
        if (segment.StartsWith("~ts-uri-spec~"u8))
        {
            var builder = new GoBuilder(); builder.WriteString(dynamicURIPathSegmentEscapePrefix); builder.WriteString(segment.Slice(dynamicURIModuleSpecifierEscapePrefix.Length));
            string physical = builder.String();
            if (DecodeDynamicURIPathSegment(physical) != physical) return ByteStringValue.FromString(physical);
        }
        return segment.StartsWith("~ts-uri-escape~"u8) ? forceEncodeDynamicURIPathSegment(segment, preserveExtension) : segment;
    }
    private static ByteStringValue forceEncodeDynamicURIPathSegment(ByteStringValue segment, bool preserveExtension)
    {
        ByteStringValue extension = default;
        if (preserveExtension && !segment.EqualsBytes("."u8) && !segment.EqualsBytes(".."u8))
        {
            int slash = segment.IsBytes ? segment.ByteSpan.LastIndexOf((byte)'\\') : segment.CharSpan.LastIndexOf('\\');
            var baseName = segment.Slice(slash + 1);
            foreach (string declaration in supportedDeclarationExtensions)
                if (baseName.EndsWith(ByteStringValue.FromString(declaration))) { extension = baseName.Slice(baseName.Length - declaration.Length); break; }
            if (extension.Length == 0 && baseName.EndsWith(".ts"u8))
            {
                int index = baseName.IsBytes ? baseName.ByteSpan.IndexOf(".d."u8) : baseName.CharSpan.IndexOf(".d.".AsSpan());
                if (index >= 0) extension = baseName.Slice(index);
            }
            if (extension.Length == 0)
            {
                int dot = baseName.IsBytes ? baseName.ByteSpan.LastIndexOf((byte)'.') : baseName.CharSpan.LastIndexOf('.');
                if (dot >= 0) extension = baseName.Slice(dot);
            }
            if (extension.Length != 0) segment = segment.Slice(0, segment.Length - extension.Length);
        }
        string hex;
        if (segment.IsBytes) hex = Convert.ToHexStringLower(segment.ByteSpan);
        else
        {
            byte[]? rented = null;
            int size = System.Text.Encoding.Latin1.GetByteCount(segment.CharSpan);
            Span<byte> bytes = size <= 256 ? stackalloc byte[size] : (rented = System.Buffers.ArrayPool<byte>.Shared.Rent(size)).AsSpan(0, size);
            try { int written = System.Text.Encoding.Latin1.GetBytes(segment.CharSpan, bytes); hex = Convert.ToHexStringLower(bytes[..written]); }
            finally { if (rented is not null) System.Buffers.ArrayPool<byte>.Shared.Return(rented); }
        }
        var output = new GoBuilder(); output.WriteString(dynamicURIPathSegmentEscapePrefix); output.WriteString(hex); output.WriteByte('~'); output.WriteString(extension); return output.Value();
    }

}
