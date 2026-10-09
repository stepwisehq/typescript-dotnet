// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified complete C# port of tsc/internal/tspath/dynamic.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Strings are original Go bytes.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    private const string dynamicURIPathSegmentEscapePrefix = "~ts-uri-escape~";
    private const string dynamicURIModuleSpecifierEscapePrefix = "~ts-uri-spec~";
    private const string dynamicURINoPathEscapePrefix = "~ts-uri-no-path~";
    public static bool IsEncodedDynamicFileName(string path) => path.StartsWith(DynamicURIFileNamePrefix, StringComparison.Ordinal);
    internal static string canonicalDynamicURIPath(string path) => IsEncodedDynamicFileName(path) && GetRootLength(path) == path.Length && !HasTrailingDirectorySeparator(path) ? path + "/" : path;
    public static string EncodeDynamicURIPath(string path) => encodeDynamicURIPathRootAware(path, true);
    public static string EncodeDynamicURIDirectoryPath(string path) => encodeDynamicURIPathRootAware(path, false);
    public static string EncodeDynamicRelativeURIPath(string path) => encodeDynamicRelativeURIPath(path, true);
    public static string EncodeDynamicRelativeURIDirectoryPath(string path) => encodeDynamicRelativeURIPath(path, false);
    private static string encodeDynamicRelativeURIPath(string path, bool preserveExtension) => encodeDynamicURIPathRootAware(path, preserveExtension);
    public static string EncodeDynamicURIPathWithSuffix(string path, string suffix)
    {
        if (suffix == "")
            return EncodeDynamicURIPath(path);
        int slash = path.LastIndexOf('/');
        string before = slash != -1 ? EncodeDynamicURIDirectoryPath(path[..slash]) + "/" : "";
        return before + forceEncodeDynamicURIPathSegmentWithSuffix(path[(slash + 1)..], suffix);
    }

    private static string encodeDynamicURIPathRootAware(string path, bool preserveExtension)
    {
        string encoded = encodeDynamicURIPath(path, preserveExtension);
        if (GetEncodedRootLength(encoded) == 0)
            return encoded;
        int slash = encoded.IndexOf('/');
        return slash < 0 ? forceEncodeDynamicURIPathSegment(encoded, preserveExtension) : forceEncodeDynamicURIPathSegment(encoded[..slash], false) + "/" + encoded[(slash + 1)..];
    }

    private static string encodeDynamicURIPath(string path, bool preserveExtension)
    {
        if (!dynamicURIPathNeedsEncoding(path))
            return path;
        string[] segments = path.Split('/');
        for (int i = 0; i < segments.Length; i++)
            segments[i] = encodeDynamicURIPathSegment(segments[i], preserveExtension && i == segments.Length - 1);
        return string.Join('/', segments);
    }

    public static string ForceEncodeDynamicURIPathSegment(string segment, bool preserveExtension) => forceEncodeDynamicURIPathSegment(segment, preserveExtension);
    public static string EncodeDynamicModuleSpecifier(string specifier) => encodeDynamicModuleSpecifier(specifier, true);
    public static string EncodeDynamicDirectorySpecifier(string specifier) => encodeDynamicModuleSpecifier(specifier, false);
    private static string encodeDynamicModuleSpecifier(string specifier, bool preserveExtension)
    {
        if (!specifier.Contains(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal) && !specifier.Contains(dynamicURIModuleSpecifierEscapePrefix, StringComparison.Ordinal))
            return specifier;
        var builder = new GoBuilder();
        while (true)
        {
            int separator = specifier.IndexOfAny(['/', '\\']);
            if (separator < 0)
            {
                builder.WriteString(encodeDynamicModuleSpecifierSegment(specifier, preserveExtension));
                return builder.String();
            }

            builder.WriteString(encodeDynamicModuleSpecifierSegment(specifier[..separator], false));
            builder.WriteByte(specifier[separator]);
            specifier = specifier[(separator + 1)..];
        }
    }

    private static string encodeDynamicModuleSpecifierSegment(string segment, bool preserveExtension)
    {
        if (segment.StartsWith(dynamicURIModuleSpecifierEscapePrefix, StringComparison.Ordinal))
        {
            string physical = dynamicURIPathSegmentEscapePrefix + segment[dynamicURIModuleSpecifierEscapePrefix.Length..];
            if (DecodeDynamicURIPathSegment(physical) != physical)
                return physical;
        }

        return segment.StartsWith(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal) ? forceEncodeDynamicURIPathSegment(segment, preserveExtension) : segment;
    }

    public static string DynamicURIPathToModuleSpecifier(string path)
    {
        if (!path.Contains(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal))
            return path;
        string[] segments = path.Split('/');
        for (int i = 0; i < segments.Length; i++)
            if (segments[i].StartsWith(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal) && DecodeDynamicURIPathSegment(segments[i]) != segments[i])
                segments[i] = dynamicURIModuleSpecifierEscapePrefix + segments[i][dynamicURIPathSegmentEscapePrefix.Length..];
        return string.Join('/', segments);
    }

    public static string EncodeDynamicLogicalModuleSpecifier(string specifier)
    {
        if (specifier == "")
            return "";
        bool trailing = HasTrailingDirectorySeparator(specifier);
        if (trailing)
            specifier = RemoveTrailingDirectorySeparator(specifier);
        string encoded = DynamicURIPathToModuleSpecifier(EncodeDynamicRelativeURIPath(specifier));
        return trailing ? encoded + "/" : encoded;
    }

    private static bool dynamicURIPathNeedsEncoding(string path) => path.Split('/').Any(dynamicURIPathSegmentNeedsEncoding);
    private static string encodeDynamicURIPathSegment(string segment, bool preserveExtension) => dynamicURIPathSegmentNeedsEncoding(segment) ? forceEncodeDynamicURIPathSegment(segment, preserveExtension) : segment;
    private static bool dynamicURIPathSegmentNeedsEncoding(string segment) => segment is "" or "." or ".." || segment.StartsWith(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal) || segment.StartsWith(dynamicURIModuleSpecifierEscapePrefix, StringComparison.Ordinal) || segment.StartsWith(dynamicURINoPathEscapePrefix, StringComparison.Ordinal) || segment.Contains('\\');
    public static string EncodeDynamicURINoPath(string suffix) => dynamicURINoPathEscapePrefix + Convert.ToHexStringLower(GoString.Bytes(suffix)) + "~";
    private static (string Bytes, bool Ok) decodeValidHex(string encoded)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromHexString(encoded);
        }
        catch (FormatException)
        {
            return ("", false);
        }

        string value = GoString.FromBytes(bytes);
        for (int offset = 0; offset < value.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(value.AsSpan(offset));
            if (rune == Utf8.RuneError && size == 1)
                return ("", false);
            offset += size;
        }

        return (value, true);
    }

    public static (string Path, bool Ok) DecodeDynamicURINoPath(string path)
    {
        if (!path.StartsWith(dynamicURINoPathEscapePrefix, StringComparison.Ordinal))
            return ("", false);
        string encoded = path[dynamicURINoPathEscapePrefix.Length..];
        int marker = encoded.IndexOf('~');
        return marker < 0 || marker != encoded.Length - 1 ? ("", false) : decodeValidHex(encoded[..marker]);
    }

    private static string forceEncodeDynamicURIPathSegment(string segment, bool preserveExtension)
    {
        string extension = "";
        if (preserveExtension && segment is not ("." or ".."))
            (segment, extension) = splitDynamicURIFileExtension(segment);
        return dynamicURIPathSegmentEscapePrefix + Convert.ToHexStringLower(GoString.Bytes(segment)) + "~" + extension;
    }

    private static string forceEncodeDynamicURIPathSegmentWithSuffix(string segment, string suffix)
    {
        var(baseName, extension) = splitDynamicURIFileExtension(segment);
        return dynamicURIPathSegmentEscapePrefix + Convert.ToHexStringLower(GoString.Bytes(baseName + "\0" + suffix)) + "~" + extension;
    }

    private static (string Base, string Extension) splitDynamicURIFileExtension(string segment)
    {
        string baseName = segment[(segment.LastIndexOf('\\') + 1)..];
        string extension = GetDeclarationFileExtension(baseName);
        if (extension == "")
        {
            int dot = baseName.LastIndexOf('.');
            if (dot >= 0)
                extension = baseName[dot..];
        }

        return (extension == "" ? segment : segment[..^extension.Length], extension);
    }

    public static string DecodeDynamicURIPath(string path)
    {
        if (!path.Contains(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal))
            return path;
        return string.Join('/', path.Split('/').Select(DecodeDynamicURIPathSegment));
    }

    public static (string Path, bool Ok) TryDecodeDynamicURIPath(string path)
    {
        string[] segments = path.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            var(decoded, ok) = TryDecodeDynamicURIPathSegment(segments[i]);
            if (!ok)
                return ("", false);
            segments[i] = decoded;
        }

        return (string.Join('/', segments), true);
    }

    public static (string Path, bool Ok) DecodeDynamicURIPathForDisk(string path)
    {
        string decoded = DecodeDynamicURIPath(path);
        if (GetEncodedRootLength(decoded) != 0)
            return ("", false);
        string[] segments = decoded.Split('/');
        for (int i = 0; i < segments.Length; i++)
            if (segments[i] == "" && i < segments.Length - 1 || segments[i] is "." or ".." || segments[i].Contains('\\'))
                return ("", false);
        return (decoded, true);
    }

    public static string DecodeDynamicURIPathSegment(string segment)
    {
        var(decoded, ok) = TryDecodeDynamicURIPathSegment(segment);
        return ok ? decoded : segment;
    }

    public static (string Segment, bool Ok) TryDecodeDynamicURIPathSegment(string segment)
    {
        if (!segment.StartsWith(dynamicURIPathSegmentEscapePrefix, StringComparison.Ordinal))
            return dynamicURIPathSegmentNeedsEncoding(segment) ? ("", false) : (segment, true);
        string encoded = segment[dynamicURIPathSegmentEscapePrefix.Length..];
        int marker = encoded.IndexOf('~');
        if (marker < 0)
            return ("", false);
        string extension = encoded[(marker + 1)..];
        var(decoded, ok) = decodeValidHex(encoded[..marker]);
        if (!ok)
            return ("", false);
        int zero = decoded.IndexOf('\0');
        return (zero < 0 ? decoded + extension : decoded[..zero] + extension + decoded[(zero + 1)..], true);
    }
}
