// Copyright (c) Microsoft Corporation. All rights reserved.
// Pinned internal/lsp/lspwatcher/lspwatcher.go:535–621.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace TypeScript.Lsp.Watching;
internal sealed partial class Watcher
{
    internal static (RootedDirectoryPath Root, bool Ok) WatchRoot(FileSystemWatcher watcher)
    {
        if (watcher.GlobPattern.Pattern is { } pattern)
            return ToWatchRoot(RootFromGlob(pattern));
        if (watcher.GlobPattern.RelativePattern is { } relative)
        {
            if (relative.BaseUri.URI is { } uri)
            {
                var path = new DocumentUri(uri.Value).FileName();
                if (path.AsValue().Length != 0)
                    return (TsPath.RootedDirectoryPathFromPath(path.AsPath()).ResolveDirectory(RootFromGlob(relative.Pattern)), true);
            }

            return (default, false);
        }

        return (default, false);
    }

    private static (RootedDirectoryPath Root, bool Ok) ToWatchRoot(ByteStringValue root) => root.Length == 0 || !TsPath.PathIsAbsolute(root) ? (default, false) : (TsPath.RootedDirectoryPathFromNormalized(root), true);
    internal static ByteStringValue RootFromGlob(ByteStringValue pattern)
    {
        pattern = TsPath.NormalizeSlashes(pattern);
        int metaIndex = -1;
        for (int i = 0; i < pattern.Length; i++)
        {
            switch (pattern[i])
            {
                case '*':
                case '?':
                case '[':
                case '{':
                    metaIndex = i;
                    break;
            }

            if (metaIndex != -1)
                break;
        }

        if (metaIndex == -1)
            return TsPath.NormalizePath(TrimRightSlash(pattern));
        var directory = TrimRightSlash(pattern.Slice(0, metaIndex));
        return directory.Length == 0 ? default : TsPath.NormalizePath(directory);
    }

    private static ByteStringValue TrimRightSlash(ByteStringValue value)
    {
        int end = value.Length;
        while (end > 0 && value[end - 1] == '/')
            end--;
        return value.Slice(0, end);
    }

    internal static ByteStringValue WatchPatternString(FileSystemWatcher watcher)
    {
        if (watcher.GlobPattern.Pattern is { } pattern)
            return pattern;
        if (watcher.GlobPattern.RelativePattern is { } relative)
            return ByteStringValue.Concat(relative.BaseUri.URI?.Value ?? default, ByteStringValue.FromString("/"), relative.Pattern);
        return default;
    }

    internal static bool IsRecursiveGlob(FileSystemWatcher watcher) => WatchPatternString(watcher).IndexOf(ByteStringValue.FromString("**")) >= 0;
    internal static WatchKind EffectiveKind(FileSystemWatcher watcher) => watcher.Kind ?? (WatchKind.Create | WatchKind.Change | WatchKind.Delete);
}
