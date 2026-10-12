// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/lsproto/lsp.go:20-148 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;

internal readonly partial struct DocumentUri
{
    internal RootedPath Path()
    {
        if (TypeScript.Bundled.Bundled.IsBundled(Value))
            return TsPath.RootedPathFromAbsolute(Value);
        if (Value.StartsWith("file://"u8))
        {
            var (host, path) = FileUri.Parse(Value);
            return TsPath.RootedPathFromAbsolute(host.Length != 0
                ? ByteStringValue.Concat(ByteStringValue.FromString("//"), host, path)
                : ProtocolUris.FixWindowsURIPath(path));
        }

        int colon = Value.IndexOf(':');
        if (colon < 0)
            throw new InvalidOperationException("invalid URI: " + Value.Materialize());
        var scheme = Value.Slice(0, colon);
        var uriPath = Value.Slice(colon + 1);
        ByteStringValue suffix = default;
        int suffixStart = uriPath.IndexOfAny('?', '#');
        if (suffixStart >= 0)
        {
            suffix = uriPath.Slice(suffixStart);
            uriPath = uriPath.Slice(0, suffixStart);
        }

        var authority = ByteStringValue.FromString("ts-nul-authority");
        bool hasAuthority = false;
        bool hasPath = true;
        if (uriPath.StartsWith("//"u8))
        {
            hasAuthority = true;
            var rest = uriPath.Slice(2);
            int slash = rest.IndexOf('/');
            if (slash < 0)
            {
                authority = rest;
                uriPath = default;
                hasPath = false;
            }
            else
            {
                authority = rest.Slice(0, slash);
                uriPath = rest.Slice(slash + 1);
            }
        }
        var encodedAuthority = authority;
        if (hasAuthority)
            encodedAuthority = authority.EqualsBytes("ts-nul-authority"u8)
                ? TsPath.ForceEncodeDynamicURIPathSegment(authority, false)
                : TsPath.EncodeDynamicURIPath(authority);
        var encodedPath = hasPath
            ? TsPath.EncodeDynamicURIPathWithSuffix(uriPath, suffix)
            : TsPath.EncodeDynamicURINoPath(suffix);
        return TsPath.RootedPathFromNormalized(ByteStringValue.Concat(
            ByteStringValue.FromString(ProtocolUris.DynamicURIFileNamePrefix), scheme,
            ByteStringValue.FromString("/"), encodedAuthority, ByteStringValue.FromString("/"), encodedPath));
    }

    internal RootedFilePath FileName() => TsPath.RootedFilePathFromPath(Path());
    internal PathKey PathKey(CaseSensitivity caseSensitivity) => caseSensitivity.PathKey(Path());
}

internal static class ProtocolUris
{
    // Same package constant as tspath.DynamicURIFileNamePrefix; the accepted
    // path owner keeps it private, so this consumer retains the literal.
    internal const string DynamicURIFileNamePrefix = "^/~ts-uri~/";

    internal static DocumentUri DynamicFileNameToDocumentUri(RootedPath fileName)
    {
        var (uri, ok) = dynamicFileNameToDocumentUri(fileName, false);
        return ok ? uri : throw new InvalidOperationException("invalid file name: " + fileName.AsString());
    }

    internal static (DocumentUri Uri, bool Ok) TryDynamicFileNameToDocumentUri(RootedPath fileName) => dynamicFileNameToDocumentUri(fileName, true);

    // lsp.go:98. Encoded and legacy dynamic names preserve distinct authority
    // and no-path sentinels; strict decoding rejects malformed escape payloads.
    private static (DocumentUri Uri, bool Ok) dynamicFileNameToDocumentUri(RootedPath fileName, bool strict)
    {
        var path = fileName.AsValue();
        bool encoded = TsPath.IsEncodedDynamicFileName(path);
        int start = encoded ? DynamicURIFileNamePrefix.Length : 2;
        var remaining = path.Slice(start);
        int slash = remaining.IndexOf('/');
        if (slash < 0 || strict && slash == 0)
            return (default, false);
        var scheme = remaining.Slice(0, slash);
        var rest = remaining.Slice(slash + 1);
        slash = rest.IndexOf('/');
        if (slash < 0)
            return (default, false);
        var authority = rest.Slice(0, slash);
        var uriPath = rest.Slice(slash + 1);
        bool hasAuthority = !authority.EqualsBytes("ts-nul-authority"u8);
        if (encoded)
        {
            if (strict)
            {
                var (decoded, ok) = TsPath.TryDecodeDynamicURIPathSegment(authority);
                if (!ok)
                    return (default, false);
                authority = decoded;
            }
            else
                authority = TsPath.DecodeDynamicURIPathSegment(authority);
        }
        if (encoded && hasAuthority)
        {
            var (suffix, decodedNoPath) = TsPath.DecodeDynamicURINoPath(uriPath);
            if (decodedNoPath)
                return (new DocumentUri(ByteStringValue.Concat(scheme, ByteStringValue.FromString("://"), authority, suffix)), true);
        }
        if (encoded)
        {
            if (strict)
            {
                var (decoded, ok) = TsPath.TryDecodeDynamicURIPath(uriPath);
                if (!ok)
                    return (default, false);
                uriPath = decoded;
            }
            else
                uriPath = TsPath.DecodeDynamicURIPath(uriPath);
        }
        return (!hasAuthority
            ? new DocumentUri(ByteStringValue.Concat(scheme, ByteStringValue.FromString(":"), uriPath))
            : new DocumentUri(ByteStringValue.Concat(scheme, ByteStringValue.FromString("://"), authority, ByteStringValue.FromString("/"), uriPath)), true);
    }

    internal static ByteStringValue FixWindowsURIPath(ByteStringValue path)
    {
        if (path.StartsWith("/"u8))
        {
            var rest = path.Slice(1);
            if (rest.Length >= 2 && TsPath.IsVolumeCharacter(rest[0]) && rest[1] == ':')
                return rest;
        }
        return path;
    }
}
