// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/pathcompare.go,pathkey.go,canonicalize_other.go.
// Linux path folding is source-defined false; native Darwin implementation is external.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed class ComparisonCache
{
    internal Dictionary<ByteStringKey, ByteStringValue>? Values;
}

internal struct ComparisonPath
{
    internal ByteStringValue Path, Folded;
    internal bool Ready;
    internal ComparisonCache? Cache;
    internal ByteStringValue Fold()
    {
        if (!Ready)
        {
            var key = ByteStringKey.FromValue(Path);
            if (Cache?.Values is { } values && values.TryGetValue(key, out var folded))
            {
                Folded = folded;
                Ready = true;
                return Folded;
            }

            Folded = FsWatch.FoldNativePath(Path);
            Ready = true;
            if (Cache is not null)
            {
                Cache.Values ??= new();
                Cache.Values[key] = Folded;
            }
        }

        return Folded;
    }
}

internal readonly struct PathComparer
{
    internal readonly bool IgnoreCase;
    internal PathComparer(bool ignoreCase) => IgnoreCase = ignoreCase;
    internal ComparisonPath Prepare(ByteStringValue path)
    {
        var value = new ComparisonPath
        {
            Path = path
        };
        if (IgnoreCase && FsWatch.NativePathFolding)
            value.Fold();
        return value;
    }

    internal (ByteStringValue Suffix, bool Ok) Suffix(ByteStringValue root, ByteStringValue path)
    {
        var value = new ComparisonPath
        {
            Path = path
        };
        return SuffixPrepared(new ComparisonPath { Path = root }, ref value);
    }

    internal (ByteStringValue Suffix, bool Ok) SuffixPrepared(ComparisonPath root, ref ComparisonPath path)
    {
        if (FsWatch.IsInDirectoryOrSelf(root.Path, path.Path))
            return (path.Path.Slice(root.Path.Length), true);
        if (!IgnoreCase || root.Path.Length == 0)
            return (FsWatch.Empty, false);
        var(suffix, ok, unicode) = FsWatch.PathSuffixASCII(root.Path, path.Path);
        return !unicode ? (suffix, ok) : SuffixUnicode(root, ref path);
    }

    private (ByteStringValue Suffix, bool Ok) SuffixUnicode(ComparisonPath root, ref ComparisonPath path)
    {
        if (!FsWatch.NativePathFolding)
            return FsWatch.PathSuffixFoldUnicode(root.Path, path.Path);
        var a = root.Fold();
        var b = path.Fold();
        if (a.Length == 0 || b.Length == 0)
            return FsWatch.PathSuffixFoldUnicode(root.Path, path.Path);
        if (!FsWatch.IsInDirectoryOrSelf(a, b))
            return (FsWatch.Empty, false);
        if (a.EqualsValue(b))
            return (FsWatch.Empty, true);
        var offset = 0;
        var separators = 0;
        for (var i = 0; i < root.Path.Length; i++)
            if (root.Path[i] == '/')
                separators++;
        var trailingSeparator = root.Path[root.Path.Length - 1] == '/';
        if (!trailingSeparator)
            separators++;
        for (var i = 0; i < separators; i++)
        {
            var slash = path.Path.Slice(offset).IndexOf('/');
            if (slash < 0)
                throw new InvalidOperationException("fswatch: folded path lost a directory boundary");
            offset += slash + 1;
        }

        return (path.Path.Slice(trailingSeparator ? offset : offset - 1), true);
    }

    internal bool Contains(ByteStringValue root, ByteStringValue path) => Suffix(root, path).Ok;
    internal (ByteStringValue Path, bool Ok) Rebase(ByteStringValue path, ByteStringValue from, ByteStringValue to)
    {
        var value = new ComparisonPath
        {
            Path = path
        };
        return RebasePrepared(ref value, new ComparisonPath { Path = from }, to);
    }

    internal (ByteStringValue Path, bool Ok) RebasePrepared(ref ComparisonPath path, ComparisonPath from, ByteStringValue to)
    {
        if (FsWatch.IsInDirectoryOrSelf(from.Path, path.Path))
            return (FsWatch.RebasePath(path.Path, from.Path, to), true);
        if (!IgnoreCase || from.Path.Length == 0)
            return (FsWatch.Empty, false);
        var(suffix, ok, unicode) = FsWatch.PathSuffixASCII(from.Path, path.Path);
        if (unicode)
            (suffix, ok) = SuffixUnicode(from, ref path);
        return ok ? (FsWatch.JoinPathSuffix(to, suffix), true) : (FsWatch.Empty, false);
    }

    internal ByteStringValue Key(ByteStringValue path)
    {
        if (!IgnoreCase || !FsWatch.NativePathFolding || path.IndexOf('\0') >= 0)
            return path;
        var folded = FsWatch.FoldNativePath(path);
        return folded.Length > 0 ? folded : path;
    }
}

internal static partial class FsWatch
{
    internal static readonly ByteStringValue Empty = ByteStringValue.FromString("");
    internal static readonly bool NativePathFolding = false;
    internal const bool NativePathComparisonAvailable = false;
    internal static ByteStringValue FoldNativePath(ByteStringValue path) => throw new InvalidOperationException("fswatch: native path folding is only available on Darwin");
    internal static ByteStringValue CanonicalizePath(ByteStringValue path) => path;
    internal static (WatchPathComparer Comparer, Exception? Error) PathComparerForPath(ByteStringValue path) => (default, null);
    internal static (ByteStringValue Suffix, bool Ok, bool Unicode) PathSuffixASCII(ByteStringValue root, ByteStringValue path)
    {
        var i = 0;
        while (i + 8 <= root.Length && i + 8 <= path.Length && root.Slice(i, i + 8).EqualsValue(path.Slice(i, i + 8)))
            i += 8;
        for (; i < root.Length && i < path.Length; i++)
        {
            var a = root[i];
            var b = path[i];
            if (a >= Utf8.RuneSelf || b >= Utf8.RuneSelf)
                return (Empty, false, true);
            if (a == b)
                continue;
            a |= 0x20;
            b |= 0x20;
            if (a != b || a < 'a' || a > 'z')
                return (Empty, false, false);
        }

        if (i == root.Length && (i == path.Length || path[i] == '/'))
            return (path.Slice(i), true, false);
        return (Empty, false, i < root.Length && root[i] >= Utf8.RuneSelf || i < path.Length && path[i] >= Utf8.RuneSelf);
    }

    internal static (ByteStringValue Suffix, bool Ok) PathSuffixFoldUnicode(ByteStringValue root, ByteStringValue path)
    {
        while (true)
        {
            var rootSlash = root.IndexOf('/');
            var pathSlash = path.IndexOf('/');
            var rootPart = rootSlash < 0 ? root : root.Slice(0, rootSlash);
            var pathPart = pathSlash < 0 ? path : path.Slice(0, pathSlash);
            if (!UnicodeCase.EqualFold(rootPart, pathPart))
                return (Empty, false);
            if (rootSlash < 0)
                return (pathSlash >= 0 ? path.Slice(pathPart.Length) : Empty, true);
            if (pathSlash < 0)
                return (Empty, false);
            root = root.Slice(rootSlash + 1);
            path = path.Slice(pathSlash + 1);
        }
    }
}

// Public Go PathComparer value owner from pathkey.go. Keep the raw-path
// admission guard separate from internal event-routing comparison workers.
internal readonly struct WatchPathComparer(PathComparer comparer)
{
    internal ByteStringValue Key(ByteStringValue path) => comparer.Key(path);
    internal (ByteStringValue Path, bool Ok) Rebase(ByteStringValue path, ByteStringValue from, ByteStringValue to)
    {
        if (!ValidUtf8(path) || !ValidUtf8(from) || path.IndexOf('\0') >= 0 || from.IndexOf('\0') >= 0)
            return default(PathComparer).Rebase(path, from, to);
        return comparer.Rebase(path, from, to);
    }

    private static bool ValidUtf8(ByteStringValue value)
    {
        while (value.Length > 0)
        {
            var(rune, width) = value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan) : Utf8.DecodeRuneInString(value.CharSpan);
            if (rune == Utf8.RuneError && width == 1)
                return false;
            value = value.Slice(width);
        }

        return true;
    }
}
