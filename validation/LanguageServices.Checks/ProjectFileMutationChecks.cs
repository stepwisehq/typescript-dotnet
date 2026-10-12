// Authored public-behavior controls for required editor text/VFS owners.
// No original upstream test-function claim (core/textchange has no direct tests).
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace LanguageServices.Checks;
public static class ProjectFileMutationChecks
{
    public static readonly string[] Names = ["OrderedByteEdits", "AppendRemoveAndTimestamp"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        if (name == Names[0])
        {
            var text = ByteStringValue.FromView(ByteStringView.FromOwnedBytes([65, 0xC3, 0xA9, 66, 0xFF, 67]));
            var change = new TextChange(new TextRange(1, 3), ByteStringValue.FromString("X"));
            Equal(change.ApplyTo(text), [65, 88, 66, 0xFF, 67]);
            var edits = Slice<TextChange>.FromArray([new(new TextRange(1, 3), ByteStringValue.FromString("X")), new(new TextRange(4, 5), ByteStringValue.FromString("Y")), ]);
            Equal(Core.ApplyBulkEdits(text, edits), [65, 88, 66, 89, 67]);
            Equal(Core.ApplyBulkEdits(text, default), [65, 0xC3, 0xA9, 66, 0xFF, 67]);
            Equal(text, [65, 0xC3, 0xA9, 66, 0xFF, 67]);
            // snapshotfs reload: old owners retain their computed line caches.
            var cached = new TypeScript.ProjectSystem.CachedFile(RootedFilePath.FromUnicode("/cached.ts"), ByteStringValue.FromString("old\nline"));
            var oldLsp = cached.LSPLineMap();
            var oldEcma = cached.ECMALineInfo();
            var changed = cached.Clone();
            changed.LSPLineMap();
            changed.ECMALineInfo();
            changed.Reload(ByteStringValue.FromString("new\nsecond\nthird"));
            if (cached.LSPLineMap().LineStarts.Length != 2 || cached.ECMALineInfo().LineCount() != 2 || changed.LSPLineMap().LineStarts.Length != 3 || changed.ECMALineInfo().LineCount() != 3 || !ReferenceEquals(cached.LSPLineMap(), oldLsp) || !ReferenceEquals(cached.ECMALineInfo(), oldEcma))
                throw new InvalidOperationException("Reload reused stale line information or changed the old snapshot");
            return;
        }

        if (name != Names[1])
            throw new ArgumentException(name);
        string directory = Path.Combine(Path.GetTempPath(), "ls-mutations-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            IFS fs = TypeScript.Bundled.Bundled.WrapFS(TypeScript.Vfs.OsVfs.OsVfs.FS());
            var file = RootedFilePath.FromUnicode(Path.Combine(directory, "nested", "file.ts"));
            Error(FileSystemMutations.AppendFile(fs, file, ByteStringValue.FromView(ByteStringView.FromOwnedBytes([0xFF, 65]))));
            Error(FileSystemMutations.AppendFile(fs, file, ByteStringValue.FromView(ByteStringView.FromOwnedBytes([0xC3, 0xA9, 10]))));
            if (!File.ReadAllBytes(PathByteText.ToUnicode(file.AsValue())).AsSpan().SequenceEqual(new byte[] { 0xFF, 65, 0xC3, 0xA9, 10 }))
                throw new InvalidOperationException("Append replaced or transcoded original bytes");
            var access = DateTimeOffset.FromUnixTimeSeconds(1700000000);
            var modification = DateTimeOffset.FromUnixTimeSeconds(1700000010);
            Error(FileSystemMutations.Chtimes(fs, file.AsPath(), access, modification));
            if (File.GetLastWriteTimeUtc(PathByteText.ToUnicode(file.AsValue())) != modification.UtcDateTime)
                throw new InvalidOperationException("Timestamp mutation lost modification value");
            string target = Path.Combine(directory, "retained");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "keep"), "retained");
            if (OperatingSystem.IsLinux())
                Directory.CreateSymbolicLink(Path.Combine(directory, "nested", "link"), target);
            Error(FileSystemMutations.Remove(fs, RootedDirectoryPath.FromUnicode(Path.Combine(directory, "nested")).AsPath()));
            if (Directory.Exists(Path.Combine(directory, "nested")) || !File.Exists(Path.Combine(target, "keep")))
                throw new InvalidOperationException("Recursive remove followed a symlink or retained its root");
            Error(FileSystemMutations.Remove(fs, file.AsPath()));
            if (FileSystemMutations.Chtimes(fs, file.AsPath(), access, modification)is null)
                throw new InvalidOperationException("Missing timestamp target returned successful mutation");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void Error(Exception? error)
    {
        if (error != null)
            throw new InvalidOperationException("Filesystem mutation failed", error);
    }

    private static void Equal(ByteStringValue value, ReadOnlySpan<byte> expected)
    {
        if (!FileContent(value).SequenceEqual(expected))
            throw new InvalidOperationException("Byte edit differs");
    }

    private static ReadOnlySpan<byte> FileContent(ByteStringValue value) => value.ByteSpan;
}
