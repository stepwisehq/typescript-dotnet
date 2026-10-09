// Authored read-only virtual test/oracle host. Configuration and matcher queries
// operate on actual raw Go-byte paths. This host does not implement writes,
// clocks or watch/cache invalidation, and its metadata is not an OS ABI claim.
using TypeScript.Scanner;
using TypeScript.Vfs;
using VfsInfo = TypeScript.Vfs.FileInfo;
using VfsMode = TypeScript.Vfs.FileMode;

namespace Configuration.Checks;

public sealed class ConfigurationFileSystem : IFS
{
    public sealed record Input(string Path, string Contents = "", string? Symlink = null, bool Directory = false, bool ReadFailure = false);
    private sealed record Entry(string Path, string Contents, string? Symlink, bool Directory, bool ReadFailure);
    private readonly Dictionary<PathKey, Entry> entries = [];
    private readonly CaseSensitivity sensitivity;

    public ConfigurationFileSystem(IEnumerable<Input> files, CaseSensitivity sensitivity = TypeScript.Scanner.CaseSensitivity.CaseSensitive)
    {
        this.sensitivity = sensitivity;
        HashSet<PathKey> supplied = [];
        // Canonical construction order makes directory presentation independent
        // of request/map insertion order, like original vfstest.FromMap.
        foreach (Input file in files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            RootedPath path = RootedPath.FromGoString(file.Path);
            if (TsPath.NormalizePath(file.Path) != file.Path || TsPath.GetRootLength(file.Path) == 0)
                throw new InvalidOperationException("Virtual fixture paths must be rooted and normalized.");
            PathKey key = sensitivity.PathKey(path);
            if (!supplied.Add(key)) throw new InvalidOperationException("Duplicate canonical virtual file path.");
            entries[key] = new Entry(file.Path, file.Contents, file.Symlink, file.Directory, file.ReadFailure);
            string parent = TsPath.GetDirectoryPath(file.Path);
            for (;;)
            {
                if (parent == "") break;
                var parentKey = sensitivity.PathKey(RootedPath.FromGoString(parent));
                if (!entries.TryGetValue(parentKey, out Entry? existing))
                    entries.Add(parentKey, new Entry(parent, "", null, true, false));
                else if (!existing.Directory && existing.Symlink is null)
                    throw new InvalidOperationException("Virtual fixture parent is a regular file.");
                string next = TsPath.GetDirectoryPath(parent);
                if (next == parent) break;
                parent = next;
            }
        }
    }

    public CaseSensitivity CaseSensitivity() => sensitivity;

    private string? Resolve(string path)
    {
        HashSet<PathKey> visited = [];
        for (int iteration = 0; iteration < 256; iteration++)
        {
            var key = sensitivity.PathKey(RootedPath.FromGoString(path));
            if (!visited.Add(key)) return null;
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
        string? resolved = Resolve(path);
        return resolved is null ? null : entries.GetValueOrDefault(sensitivity.PathKey(RootedPath.FromGoString(resolved)));
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
        string? resolved = Resolve(path.AsString());
        if (resolved is null || Lookup(resolved) is not { Directory: true }) return new Entries();
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
        return result;
    }

    public VfsInfo? Stat(RootedPath path) => Lookup(path.AsString()) is { } entry ? new Info(entry) : null;
    public RootedPath Realpath(RootedPath path) => RootedPath.FromGoString(Resolve(path.AsString()) ?? path.AsString());

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
