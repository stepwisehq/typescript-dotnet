// Authored original compiler-harness backing host. Source comparison fixes the
// retained host's path-role restrictions locally. Matcher queries
// operate on actual raw Go-byte paths. This host does not implement writes,
// clocks or watch/cache invalidation, and its metadata is not an OS ABI claim.
using TypeScript.Scanner;
using TypeScript.Vfs;
using VfsInfo = TypeScript.Vfs.FileInfo;
using VfsMode = TypeScript.Vfs.FileMode;

namespace Program.Checks;

public sealed class CompilerBaselineFileSystem : IFS
{
    public sealed record Input(string Path, string Contents = "", string? Symlink = null, bool Directory = false, bool ReadFailure = false);
    private sealed record Entry(string Path, string Contents, string? Symlink, bool Directory, bool ReadFailure);
    private readonly Dictionary<PathKey, Entry> entries = [];
    private readonly CaseSensitivity sensitivity;
    private static readonly System.Text.UTF8Encoding StrictUTF8 = new(false, true);

    public CompilerBaselineFileSystem(IEnumerable<Input> files, CaseSensitivity sensitivity = TypeScript.Scanner.CaseSensitivity.CaseSensitive)
    {
        this.sensitivity = sensitivity;
        HashSet<PathKey> supplied = [];
        bool posix = false, windows = false;
        // Original component-based construction order determines stored-case paths.
        foreach (Input file in files.OrderBy(file => file.Path, Comparer<string>.Create(CompareParts)))
        {
            RootedPath path = RootedPath.FromGoString(file.Path);
            ValidateInputPath(file.Path);
            if (file.Symlink != null)
                ValidateInputPath(file.Symlink);
            PathKey key = sensitivity.PathKey(path);
            // vfstest.convertMapFS rejects duplicate explicit canonical paths;
            // an explicit leaf may replace its previously synthesized directory.
            if (!supplied.Add(key))
                throw new InvalidOperationException("Duplicate canonical virtual file path.");
            string parent = TsPath.GetDirectoryPath(file.Path);
            if (parent != "") EnsureDirectory(parent);
            entries[key] = new Entry(file.Path, file.Contents, file.Symlink, file.Directory, file.ReadFailure);
        }
        if (posix && windows)
            throw new InvalidOperationException("Mixed POSIX and Windows virtual paths.");

        void ValidateInputPath(string value)
        {
            if (!TsPath.IsRootedDiskPath(value) || TsPath.RemoveTrailingDirectorySeparator(TsPath.NormalizePath(value)) != value)
                throw new InvalidOperationException("Virtual fixture paths must be rooted and normalized.");
            if (value.StartsWith('/')) posix = true;
            else windows = true;
        }
    }

    // vfstest.mkdirAll: follow existing directory links before creating
    // intermediates, and retain the stored spelling of each created component.
    // Rooted paths adapt the source MapFS's relative io/fs names.
    private void EnsureDirectory(string path)
    {
        string? resolved = Resolve(path);
        if (resolved != null)
        {
            if (!entries[sensitivity.PathKey(RootedPath.FromGoString(resolved))].Directory)
                throw new InvalidOperationException("Virtual fixture parent is a regular file.");
            return;
        }
        List<string> toCreate = [];
        int offset = TsPath.GetRootLength(path);
        int restarts = 0;
        // The source MapFS has an implicit io/fs root. Its rooted counterpart
        // is stored explicitly here so DirectoryExists/Realpath use one role.
        string root = path[..offset];
        entries.TryAdd(sensitivity.PathKey(RootedPath.FromGoString(root)), new Entry(root, "", null, true, false));
        for (;;)
        {
            int slash = path.IndexOf('/', offset);
            string dir = slash < 0 ? path : path[..slash];
            string rest = slash < 0 ? "" : path[(slash + 1)..];
            var canonical = sensitivity.PathKey(RootedPath.FromGoString(dir));
            resolved = Resolve(dir);
            if (resolved == null)
            {
                // A broken existing link is an error, not a missing directory.
                for (string prefix = dir; ; prefix = TsPath.GetDirectoryPath(prefix))
                {
                    if (entries.TryGetValue(sensitivity.PathKey(RootedPath.FromGoString(prefix)), out Entry? entry) && entry.Symlink != null)
                        throw new InvalidOperationException("Virtual fixture parent is a broken symlink.");
                    string parent = TsPath.GetDirectoryPath(prefix);
                    if (parent == prefix || parent == "") break;
                }
                toCreate.Add(dir);
            }
            else
            {
                Entry entry = entries[sensitivity.PathKey(RootedPath.FromGoString(resolved))];
                if (!entry.Directory)
                    throw new InvalidOperationException("Virtual fixture parent is a regular file.");
                if (canonical != sensitivity.PathKey(RootedPath.FromGoString(resolved)))
                {
                    if (++restarts > 256)
                        throw new InvalidOperationException("Virtual fixture parent symlink cycle.");
                    path = rest == "" ? entry.Path : TsPath.CombinePaths(entry.Path, rest);
                    toCreate.Clear();
                    offset = TsPath.GetRootLength(path);
                    continue;
                }
            }
            if (rest == "") break;
            offset = dir.Length + 1;
        }
        foreach (string dir in toCreate)
            entries[sensitivity.PathKey(RootedPath.FromGoString(dir))] = new Entry(dir, "", null, true, false);
    }

    // Exact original vfstest.comparePathsByParts: compare the complete remaining
    // suffix as soon as either side has no slash. Explicit leaves overwrite with
    // their complete original spelling; parent creation retains its first name.
    private static int CompareParts(string left, string right)
    {
        for (;;)
        {
            int a = left.IndexOf('/'), b = right.IndexOf('/');
            if (a < 0 || b < 0) return StringComparer.Ordinal.Compare(left, right);
            int order = StringComparer.Ordinal.Compare(left[..a], right[..b]);
            if (order != 0) return order;
            left = left[(a + 1)..]; right = right[(b + 1)..];
        }
    }
    public CaseSensitivity CaseSensitivity() => sensitivity;

    private string? Resolve(string path)
    {
        HashSet<PathKey> visited = [];
        for (int iteration = 0; iteration < 256; iteration++)
        {
            if (!ValidLookupPath(path)) return null;
            var key = sensitivity.PathKey(RootedPath.FromGoString(path));
            if (!visited.Add(key)) return null;
            // vfstest.getFollowingSymlinksWorker checks the exact ordinary entry
            // before considering a directory symlink prefix.
            if (entries.TryGetValue(key, out var direct) && direct.Symlink == null)
                return path;
            string prefix = path;
            Entry? link = null;
            for (;;)
            {
                if (entries.TryGetValue(sensitivity.PathKey(RootedPath.FromGoString(prefix)), out Entry? entry) && entry.Symlink is not null)
                {
                    link = entry;
                    break;
                }
                string parent = TsPath.GetDirectoryPath(prefix);
                if (parent == prefix || parent == "") break;
                prefix = parent;
            }
            if (link is null) return entries.ContainsKey(key) ? path : null;
            string remainder = path[prefix.Length..].TrimStart('/');
            path = remainder == "" ? link.Symlink! : TsPath.CombinePaths(link.Symlink!, remainder);
        }
        return null;
    }

    private Entry? Lookup(string path)
    {
        if (!ValidLookupPath(path)) return null;
        string? resolved = Resolve(path);
        return resolved is null ? null : entries.GetValueOrDefault(sensitivity.PathKey(RootedPath.FromGoString(resolved)));
    }

    // Go io/fs.ValidPath rejects invalid UTF-8 names; content bytes remain raw.
    private static bool ValidLookupPath(string path)
    {
        try { StrictUTF8.GetCharCount(GoString.Bytes(path)); return true; }
        catch (System.Text.DecoderFallbackException) { return false; }
    }

    public bool FileExists(RootedFilePath path) => Lookup(path.AsString()) is { Directory: false };
    public bool DirectoryExists(RootedDirectoryPath path) => Lookup(path.AsString()) is { Directory: true };
    public (string Contents, bool Ok) ReadFile(RootedFilePath path)
    {
        Entry? entry = Lookup(path.AsString());
        if (entry is null || entry.Directory || entry.ReadFailure) return ("", false);
        return VfsInternal.DecodeBytes(entry.Contents);
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath path)
    {
        string? resolved = ValidLookupPath(path.AsString()) ? Resolve(path.AsString()) : null;
        if (resolved is null || Lookup(resolved) is not { Directory: true }) return new Entries { Symlinks = [] };
        PathKey parentKey = sensitivity.PathKey(RootedPath.FromGoString(resolved));
        Entries result = new() { Files = [], Directories = [], Symlinks = [] };
        foreach (Entry entry in entries.Values)
        {
            if (sensitivity.PathKey(RootedPath.FromGoString(entry.Path)) == parentKey) continue;
            if (sensitivity.PathKey(RootedPath.FromGoString(TsPath.GetDirectoryPath(entry.Path))) != parentKey) continue;
            Entry? target = Lookup(entry.Path);
            if (target is null) continue;
            string name = TsPath.GetBaseFileName(entry.Path);
            (target.Directory ? result.Directories : result.Files).Add(name);
            if (entry.Symlink is not null) result.Symlinks.Add(name);
        }
        result.Files.Sort(StringComparer.Ordinal);
        result.Directories.Sort(StringComparer.Ordinal);
        // vfs.Common appends into nil file/directory slices, while allocating
        // the symlink set even for empty or inaccessible directories.
        if (result.Files.Count == 0) result.Files = null;
        if (result.Directories.Count == 0) result.Directories = null;
        return result;
    }

    public VfsInfo? Stat(RootedPath path) => Lookup(path.AsString()) is { } entry ? new Info(entry) : null;
    public RootedPath Realpath(RootedPath path) => RootedPath.FromGoString(Lookup(path.AsString())?.Path ?? path.AsString());

    private sealed class Info(Entry entry) : VfsInfo
    {
        public string Name() => TsPath.GetBaseFileName(entry.Path);
        public long Size() => entry.Directory ? 0 : entry.Contents.Length;
        public VfsMode Mode() => entry.Directory ? VfsMode.Dir : VfsMode.Regular;
        public DateTimeOffset ModTime() => DateTimeOffset.UnixEpoch;
        public bool IsDir() => entry.Directory;
        public object? Sys() => null;
    }
}
