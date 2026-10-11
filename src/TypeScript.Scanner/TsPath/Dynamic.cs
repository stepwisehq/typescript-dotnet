// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/dynamic.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    private const string dynamicURIPathSegmentEscapePrefix = "~ts-uri-escape~";
    private const string dynamicURIModuleSpecifierEscapePrefix = "~ts-uri-spec~";
    private const string dynamicURINoPathEscapePrefix = "~ts-uri-no-path~";
    public static bool IsEncodedDynamicFileName(string path) => IsEncodedDynamicFileName(ByteStringValue.FromString(path));
    public static bool IsEncodedDynamicFileName(ByteStringValue path) => path.StartsWith("^/~ts-uri~/"u8);
    internal static string canonicalDynamicURIPath(string path) => canonicalDynamicURIPath(ByteStringValue.FromString(path)).Materialize()!;
    internal static ByteStringValue canonicalDynamicURIPath(ByteStringValue path) => IsEncodedDynamicFileName(path) && GetRootLength(path) == path.Length && !HasTrailingDirectorySeparator(path) ? pathConcat(path, ByteStringValue.FromString("/")) : path;
    public static string EncodeDynamicURIPath(string path) => EncodeDynamicURIPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue EncodeDynamicURIPath(ByteStringValue path) => encodeDynamicURIPathRootAware(path, true);
    public static string EncodeDynamicURIDirectoryPath(string path) => EncodeDynamicURIDirectoryPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue EncodeDynamicURIDirectoryPath(ByteStringValue path) => encodeDynamicURIPathRootAware(path, false);
    public static string EncodeDynamicRelativeURIPath(string path) => EncodeDynamicRelativeURIPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue EncodeDynamicRelativeURIPath(ByteStringValue path) => encodeDynamicRelativeURIPath(path, true);
    public static string EncodeDynamicRelativeURIDirectoryPath(string path) => EncodeDynamicRelativeURIDirectoryPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue EncodeDynamicRelativeURIDirectoryPath(ByteStringValue path) => encodeDynamicRelativeURIPath(path, false);
    private static ByteStringValue encodeDynamicRelativeURIPath(ByteStringValue path, bool preserveExtension) => encodeDynamicURIPathRootAware(path, preserveExtension);
    public static string EncodeDynamicURIPathWithSuffix(string path, string suffix) => EncodeDynamicURIPathWithSuffix(ByteStringValue.FromString(path), ByteStringValue.FromString(suffix)).Materialize()!;
    public static ByteStringValue EncodeDynamicURIPathWithSuffix(ByteStringValue path, ByteStringValue suffix)
    {
        if (!suffix.IsNull && suffix.Length == 0)
        {
            return EncodeDynamicURIPath(path);
        }
        int slash = path.LastIndexOf('/');
        var before = slash != -1 ? pathConcat(EncodeDynamicURIDirectoryPath(path.Slice(0, slash)), ByteStringValue.FromString("/")) : default;
        return pathConcat(before, forceEncodeDynamicURIPathSegmentWithSuffix(path.Slice(slash + 1), suffix));
    }
    private static ByteStringValue encodeDynamicURIPathRootAware(ByteStringValue path, bool preserveExtension)
    {
        var encoded = encodeDynamicURIPath(path, preserveExtension);
        if (GetEncodedRootLength(encoded) == 0)
        {
            return encoded;
        }
        int slash = encoded.IndexOf('/');
        return slash < 0 ? forceEncodeDynamicURIPathSegment(encoded, preserveExtension) : pathConcat(forceEncodeDynamicURIPathSegment(encoded.Slice(0, slash), false), ByteStringValue.FromString("/"), encoded.Slice(slash + 1));
    }
    private static ByteStringValue encodeDynamicURIPath(ByteStringValue path, bool preserveExtension)
    {
        if (!dynamicURIPathNeedsEncoding(path))
        {
            return path;
        }
        var builder = new GoBuilder();
        builder.Grow(path.Length + dynamicURIPathSegmentEscapePrefix.Length);
        for (;;)
        {
            int slash = path.IndexOf('/');
            bool found = slash >= 0;
            var segment = found ? path.Slice(0, slash) : path;
            builder.WriteString(encodeDynamicURIPathSegment(segment, preserveExtension && !found));
            if (!found)
            {
                return builder.Value();
            }
            builder.WriteByte('/');
            path = path.Slice(slash + 1);
        }
    }
    public static string ForceEncodeDynamicURIPathSegment(string segment, bool preserveExtension) => ForceEncodeDynamicURIPathSegment(ByteStringValue.FromString(segment), preserveExtension).Materialize()!;
    public static ByteStringValue ForceEncodeDynamicURIPathSegment(ByteStringValue segment, bool preserveExtension) => forceEncodeDynamicURIPathSegment(segment, preserveExtension);
    public static string EncodeDynamicModuleSpecifier(string specifier) => EncodeDynamicModuleSpecifier(ByteStringValue.FromString(specifier)).Materialize()!;
    public static ByteStringValue EncodeDynamicModuleSpecifier(ByteStringValue specifier) => encodeDynamicModuleSpecifier(specifier, true);
    public static string EncodeDynamicDirectorySpecifier(string specifier) => EncodeDynamicDirectorySpecifier(ByteStringValue.FromString(specifier)).Materialize()!;
    public static ByteStringValue EncodeDynamicDirectorySpecifier(ByteStringValue specifier) => encodeDynamicModuleSpecifier(specifier, false);
    private static ByteStringValue encodeDynamicModuleSpecifier(ByteStringValue specifier, bool preserveExtension)
    {
        if (!specifier.Contains("~ts-uri-escape~"u8) && !specifier.Contains("~ts-uri-spec~"u8))
        {
            return specifier;
        }
        var builder = new GoBuilder();
        builder.Grow(specifier.Length + dynamicURIPathSegmentEscapePrefix.Length);
        for (;;)
        {
            int separator = specifier.IndexOfAny('/', '\\');
            if (separator < 0)
            {
                builder.WriteString(encodeDynamicModuleSpecifierSegment(specifier, preserveExtension));
                return builder.Value();
            }
            builder.WriteString(encodeDynamicModuleSpecifierSegment(specifier.Slice(0, separator), false));
            builder.WriteByte(specifier[separator]);
            specifier = specifier.Slice(separator + 1);
        }
    }
    private static ByteStringValue encodeDynamicModuleSpecifierSegment(ByteStringValue segment, bool preserveExtension)
    {
        if (segment.StartsWith("~ts-uri-spec~"u8))
        {
            var physical = pathConcat(ByteStringValue.FromString(dynamicURIPathSegmentEscapePrefix), segment.Slice(dynamicURIModuleSpecifierEscapePrefix.Length));
            if (!DecodeDynamicURIPathSegment(physical).EqualsValue(physical))
            {
                return physical;
            }
        }
        return segment.StartsWith("~ts-uri-escape~"u8) ? forceEncodeDynamicURIPathSegment(segment, preserveExtension) : segment;
    }
    public static string DynamicURIPathToModuleSpecifier(string path) => DynamicURIPathToModuleSpecifier(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue DynamicURIPathToModuleSpecifier(ByteStringValue path)
    {
        if (!path.Contains("~ts-uri-escape~"u8))
        {
            return path;
        }
        var segments = splitDynamicPath(path);
        for (int i = 0; i < segments.Length; i++)
        if (segments[i].StartsWith("~ts-uri-escape~"u8) && !DecodeDynamicURIPathSegment(segments[i]).EqualsValue(segments[i]))
        {
            segments[i] = pathConcat(ByteStringValue.FromString(dynamicURIModuleSpecifierEscapePrefix), segments[i].Slice(dynamicURIPathSegmentEscapePrefix.Length));
        }
        return joinPathSegments(segments);
    }
    public static string EncodeDynamicLogicalModuleSpecifier(string specifier) => EncodeDynamicLogicalModuleSpecifier(ByteStringValue.FromString(specifier)).Materialize()!;
    public static ByteStringValue EncodeDynamicLogicalModuleSpecifier(ByteStringValue specifier)
    {
        if (specifier.IsNull)
        {
            throw new NullReferenceException();
        }
        if (specifier.Length == 0)
        {
            return default;
        }
        bool trailing = HasTrailingDirectorySeparator(specifier);
        if (trailing)
        {
            specifier = RemoveTrailingDirectorySeparator(specifier);
        }
        var encoded = DynamicURIPathToModuleSpecifier(EncodeDynamicRelativeURIPath(specifier));
        return trailing ? pathConcat(encoded, ByteStringValue.FromString("/")) : encoded;
    }
    private static bool dynamicURIPathNeedsEncoding(ByteStringValue path)
    {
        for (;;)
        {
            int slash = path.IndexOf('/');
            if (dynamicURIPathSegmentNeedsEncoding(slash < 0 ? path : path.Slice(0, slash)))
            {
                return true;
            }
            if (slash < 0)
            {
                return false;
            }
            path = path.Slice(slash + 1);
        }
    }
    private static ByteStringValue encodeDynamicURIPathSegment(ByteStringValue segment, bool preserveExtension) => dynamicURIPathSegmentNeedsEncoding(segment) ? forceEncodeDynamicURIPathSegment(segment, preserveExtension) : segment;
    private static bool dynamicURIPathSegmentNeedsEncoding(ByteStringValue segment) => segment.Length == 0 || segment.EqualsBytes("."u8) || segment.EqualsBytes(".."u8) || segment.StartsWith("~ts-uri-escape~"u8) || segment.StartsWith("~ts-uri-spec~"u8) || segment.StartsWith("~ts-uri-no-path~"u8) || segment.Contains('\\');
    public static string EncodeDynamicURINoPath(string suffix) => EncodeDynamicURINoPath(ByteStringValue.FromString(suffix)).Materialize()!;
    public static ByteStringValue EncodeDynamicURINoPath(ByteStringValue suffix) => pathConcat(ByteStringValue.FromString(dynamicURINoPathEscapePrefix), encodePathHex(suffix), ByteStringValue.FromString("~"));
    private static ByteStringValue encodePathHex(ByteStringValue value)
    {
        if (value.IsNull)
        {
            throw new ArgumentNullException(nameof(value));
        }
        if (value.IsBytes)
        {
            return encodePathHex(value.ByteSpan);
        }
        byte[]? rented = null;
        Span<byte> bytes = value.Length <= 256 ? stackalloc byte[value.Length] : (rented = System.Buffers.ArrayPool<byte>.Shared.Rent(value.Length)).AsSpan(0, value.Length);
        try
        {
            System.Text.Encoding.Latin1.GetBytes(value.CharSpan, bytes);
            return encodePathHex(bytes);
        }
        finally
        {
            if (rented is not null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
    private static ByteStringValue encodePathHex(ReadOnlySpan<byte> bytes)
    {
        var encoded = new byte[checked(bytes.Length * 2)];
        Convert.TryToHexStringLower(bytes, encoded, out _);
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(encoded));
    }
    private static (ByteStringValue Bytes, bool Ok) decodeValidHex(ByteStringValue encoded)
    {
        byte[] bytes;
        try
        {
            bytes = encoded.IsBytes ? Convert.FromHexString(encoded.ByteSpan) : Convert.FromHexString(encoded.CharSpan);
        }
        catch (FormatException)
        {
            return (default, false);
        }
        for (int offset = 0; offset < bytes.Length;)
        {
            var (rune, size) = Utf8.DecodeRuneInString(bytes.AsSpan(offset));
            if (rune == Utf8.RuneError && size == 1)
            {
                return (default, false);
            }
            offset += size;
        }
        return (ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes)), true);
    }
    public static (string Path, bool Ok) DecodeDynamicURINoPath(string path)
    {
        var (decoded, ok) = DecodeDynamicURINoPath(ByteStringValue.FromString(path));
        return (decoded.Materialize()!, ok);
    }
    public static (ByteStringValue Path, bool Ok) DecodeDynamicURINoPath(ByteStringValue path)
    {
        if (!path.StartsWith("~ts-uri-no-path~"u8))
        {
            return (default, false);
        }
        var encoded = path.Slice(dynamicURINoPathEscapePrefix.Length);
        int marker = encoded.IndexOf('~');
        return marker < 0 || marker != encoded.Length - 1 ? (default, false) : decodeValidHex(encoded.Slice(0, marker));
    }
    private static ByteStringValue forceEncodeDynamicURIPathSegment(ByteStringValue segment, bool preserveExtension)
    {
        ByteStringValue extension = default;
        if (preserveExtension && !segment.EqualsBytes("."u8) && !segment.EqualsBytes(".."u8))
        {
            (segment, extension) = splitDynamicURIFileExtension(segment);
        }
        return pathConcat(ByteStringValue.FromString(dynamicURIPathSegmentEscapePrefix), encodePathHex(segment), ByteStringValue.FromString("~"), extension);
    }
    private static ByteStringValue forceEncodeDynamicURIPathSegmentWithSuffix(ByteStringValue segment, ByteStringValue suffix)
    {
        var (baseName, extension) = splitDynamicURIFileExtension(segment);
        var payload = pathConcat(baseName, ByteStringValue.FromString("\0"), suffix);
        return pathConcat(ByteStringValue.FromString(dynamicURIPathSegmentEscapePrefix), encodePathHex(payload), ByteStringValue.FromString("~"), extension);
    }
    private static (ByteStringValue Base, ByteStringValue Extension) splitDynamicURIFileExtension(ByteStringValue segment)
    {
        var name = segment.Slice(segment.LastIndexOf('\\') + 1);
        var extension = GetDeclarationFileExtension(name);
        if (extension.Length == 0)
        {
            extension = GetAnyExtensionFromPath(name, null, CaseSensitivity.CaseSensitive);
        }
        return (extension.Length == 0 ? segment : segment.Slice(0, segment.Length - extension.Length), extension);
    }
    public static string DecodeDynamicURIPath(string path) => DecodeDynamicURIPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue DecodeDynamicURIPath(ByteStringValue path)
    {
        if (!path.Contains("~ts-uri-escape~"u8))
        {
            return path;
        }
        var segments = splitDynamicPath(path);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = DecodeDynamicURIPathSegment(segments[i]);
        }
        return joinPathSegments(segments);
    }
    public static (string Path, bool Ok) TryDecodeDynamicURIPath(string path)
    {
        var (decoded, ok) = TryDecodeDynamicURIPath(ByteStringValue.FromString(path));
        return (decoded.Materialize()!, ok);
    }
    public static (ByteStringValue Path, bool Ok) TryDecodeDynamicURIPath(ByteStringValue path)
    {
        var segments = splitDynamicPath(path);
        for (int i = 0; i < segments.Length; i++)
        {
            var (decoded, ok) = TryDecodeDynamicURIPathSegment(segments[i]);
            if (!ok)
            {
                return (default, false);
            }
            segments[i] = decoded;
        }
        return (joinPathSegments(segments), true);
    }
    public static (string Path, bool Ok) DecodeDynamicURIPathForDisk(string path)
    {
        var (decoded, ok) = DecodeDynamicURIPathForDisk(ByteStringValue.FromString(path));
        return (decoded.Materialize()!, ok);
    }
    public static (ByteStringValue Path, bool Ok) DecodeDynamicURIPathForDisk(ByteStringValue path)
    {
        var decoded = DecodeDynamicURIPath(path);
        if (GetEncodedRootLength(decoded) != 0)
        {
            return (default, false);
        }
        var remaining = decoded;
        for (;;)
        {
            int slash = remaining.IndexOf('/');
            var segment = slash < 0 ? remaining : remaining.Slice(0, slash);
            if (segment.Length == 0 && slash >= 0 || segment.EqualsBytes("."u8) || segment.EqualsBytes(".."u8) || segment.Contains('\\'))
            {
                return (default, false);
            }
            if (slash < 0)
            {
                return (decoded, true);
            }
            remaining = remaining.Slice(slash + 1);
        }
    }
    public static string DecodeDynamicURIPathSegment(string segment) => DecodeDynamicURIPathSegment(ByteStringValue.FromString(segment)).Materialize()!;
    public static ByteStringValue DecodeDynamicURIPathSegment(ByteStringValue segment)
    {
        var (decoded, ok) = TryDecodeDynamicURIPathSegment(segment);
        return ok ? decoded : segment;
    }
    public static (string Segment, bool Ok) TryDecodeDynamicURIPathSegment(string segment)
    {
        var (decoded, ok) = TryDecodeDynamicURIPathSegment(ByteStringValue.FromString(segment));
        return (decoded.Materialize()!, ok);
    }
    public static (ByteStringValue Segment, bool Ok) TryDecodeDynamicURIPathSegment(ByteStringValue segment)
    {
        if (!segment.StartsWith("~ts-uri-escape~"u8))
        {
            return dynamicURIPathSegmentNeedsEncoding(segment) ? (default, false) : (segment, true);
        }
        var encoded = segment.Slice(dynamicURIPathSegmentEscapePrefix.Length);
        int marker = encoded.IndexOf('~');
        if (marker < 0)
        {
            return (default, false);
        }
        var extension = encoded.Slice(marker + 1);
        var (decoded, ok) = decodeValidHex(encoded.Slice(0, marker));
        if (!ok)
        {
            return (default, false);
        }
        int zero = decoded.IndexOf(0);
        return (zero < 0 ? pathConcat(decoded, extension) : pathConcat(decoded.Slice(0, zero), extension, decoded.Slice(zero + 1)), true);
    }
    internal static ByteStringValue trimDynamicRoot(ByteStringValue root) => root.EndsWith("/"u8) ? root.Slice(0, root.Length - 1) : root;
    private static Slice<ByteStringValue> splitDynamicPath(ByteStringValue path)
    {
        int count = 1 + (path.IsBytes ? path.ByteSpan.Count((byte)'/') : path.CharSpan.Count('/'));
        var result = Slice<ByteStringValue>.Make(count);
        int index = 0;
        for (;;)
        {
            int slash = path.IndexOf('/');
            if (slash < 0)
            {
                result[index] = path;
                return result;
            }
            result[index++] = path.Slice(0, slash);
            path = path.Slice(slash + 1);
        }
    }
    private static ByteStringValue joinPathSegments(Slice<ByteStringValue> segments)
    {
        if (segments.Length == 0)
        {
            return default;
        }
        if (segments.Length == 1)
        {
            return segments[0];
        }
        int size = segments.Length - 1;
        foreach (var segment in segments)
        {
            size = checked(size + segment.Length);
        }
        var builder = new GoBuilder();
        builder.Grow(size);
        for (int i = 0; i < segments.Length; i++)
        {
            if (i > 0)
            {
                builder.WriteByte('/');
            }
            builder.WriteString(segments[i]);
        }
        return builder.Value();
    }
}
