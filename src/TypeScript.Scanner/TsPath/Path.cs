// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/path.go syntax-reference helpers.
// Pinned upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class TsPath
{
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
}
