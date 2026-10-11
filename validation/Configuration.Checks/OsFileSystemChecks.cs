// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Three original OSVFS read-side tests plus explicitly authored Linux ABI controls.
// Native .NET fixture writes create inputs; no mapped writable-VFS API is claimed.
using System.Diagnostics;
using System.Net.Sockets;
using TypeScript.Scanner;
using TypeScript.Vfs.OsVfs;
using Mode = TypeScript.Vfs.FileMode;

namespace Configuration.Checks;

public static class OsFileSystemChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["TestOS", "TestSymlinkRealpath", "TestGetAccessibleEntries", "Authored.ReadBOM", "Authored.LinuxStatAndPathBoundaries", "Authored.RawNativePathEncoding"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestOS": OriginalOS(); break;
            case "TestSymlinkRealpath": OriginalRealpath(); break;
            case "TestGetAccessibleEntries": OriginalEntries(); break;
            case "Authored.ReadBOM": ReadBOM(); break;
            case "Authored.LinuxStatAndPathBoundaries": LinuxBoundaries(); break;
            case "Authored.RawNativePathEncoding": OsRawPathChecks.Run(); break;
            default: throw new InvalidOperationException("Unknown OS read-side check: " + name);
        }
    }

    private sealed class Fixture : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "configuration-os-" + Guid.NewGuid().ToString("N"));
        public Fixture() => System.IO.Directory.CreateDirectory(Directory);
        public string PathFor(string relative) => Path.Combine(Directory, relative);
        public void Dispose() => System.IO.Directory.Delete(Directory, true);
    }

    private static RootedFilePath FilePath(string native) => RootedFilePath.FromUnicode(native.Replace('\\', '/'));
    private static RootedDirectoryPath DirectoryPath(string native) => RootedDirectoryPath.FromUnicode(native.Replace('\\', '/'));

    private static void OriginalOS()
    {
        var fs = OsVfs.FS();
        string? root = Environment.CurrentDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "upstream/TypeScript/tsc/go.mod"))) root = Path.GetDirectoryName(root);
        Check.True(root is not null, "Original fixture repo go.mod exists");
        string goMod = Path.Combine(root!, "upstream/TypeScript/tsc/go.mod");
        string expected = GoString.FromBytes(File.ReadAllBytes(goMod));
        var (contents, ok) = fs.ReadFile(FilePath(goMod));
        Check.True(ok, "Original OS ReadFile succeeds");
        Check.Equal(contents, expected, "Original OS ReadFile bytes");
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Check.True(home != "", "Original home directory fixture available");
        var path = DirectoryPath(home);
        string expectedHome = path.AsString();
        if (OperatingSystem.IsWindows()) expectedHome = expectedHome[..1].ToUpperInvariant() + expectedHome[1..];
        Check.Equal(fs.Realpath(path.AsPath()).AsString(), expectedHome, "Original home Realpath");
        _ = fs.CaseSensitivity();
        if (OperatingSystem.IsWindows()) Check.Equal(fs.CaseSensitivity(), CaseSensitivity.CaseInsensitive, "Original Windows case policy");
        if (OperatingSystem.IsLinux()) Check.Equal(fs.CaseSensitivity(), CaseSensitivity.CaseSensitive, "Original Linux case policy");
    }

    private static void OriginalRealpath()
    {
        using Fixture fixture = new();
        string target = fixture.PathFor("target"), link = fixture.PathFor("link");
        Directory.CreateDirectory(target);
        string targetFile = Path.Combine(target, "file");
        File.WriteAllText(targetFile, "hello");
        Directory.CreateSymbolicLink(link, target);
        string linkFile = Path.Combine(link, "file");
        Check.Equal(File.ReadAllText(linkFile), "hello", "Original native symlink input contents");
        var fs = OsVfs.FS();
        Check.Equal(fs.Realpath(FilePath(targetFile).AsPath()), fs.Realpath(FilePath(linkFile).AsPath()), "Original intermediate-directory symlink realpath identity");
    }

    private static void OriginalEntries()
    {
        using Fixture fixture = new();
        string target = fixture.PathFor("target"), link = fixture.PathFor("link");
        Directory.CreateDirectory(target);
        Directory.CreateDirectory(link);
        foreach (var (name, text) in new[] { ("file1", "hello"), ("file2", "world") })
        {
            string path = Path.Combine(target, name);
            File.WriteAllText(path, text);
            File.CreateSymbolicLink(Path.Combine(link, name), path);
        }
        foreach (string name in new[] { "dir1", "dir2" })
        {
            string path = Path.Combine(target, name);
            Directory.CreateDirectory(path);
            Directory.CreateSymbolicLink(Path.Combine(link, name), path);
        }
        var fs = OsVfs.FS();
        var entries = fs.GetAccessibleEntries(DirectoryPath(link));
        Check.Sequence(GoSlices.ToListOrNull(entries.Directories)?.Select(static name => name.Materialize()!).ToArray(), new[] { "dir1", "dir2" }, "Original symlink directories");
        Check.Sequence(GoSlices.ToListOrNull(entries.Files)?.Select(static name => name.Materialize()!).ToArray(), new[] { "file1", "file2" }, "Original symlink files");
        Check.True(entries.Symlinks is not null, "Original symlink metadata available");
        Check.Equal(entries.Symlinks!.Count, 4, "Original four symlink entries");
        foreach (string name in new[] { "file1", "file2", "dir1", "dir2" }) Check.True(entries.Symlinks!.Contains(ByteStringKey.FromString(name)), "Original symlink membership " + name);
        entries = fs.GetAccessibleEntries(DirectoryPath(target));
        Check.Sequence(GoSlices.ToListOrNull(entries.Directories)?.Select(static name => name.Materialize()!).ToArray(), new[] { "dir1", "dir2" }, "Original target directories");
        Check.Sequence(GoSlices.ToListOrNull(entries.Files)?.Select(static name => name.Materialize()!).ToArray(), new[] { "file1", "file2" }, "Original target files");
        Check.True(entries.Symlinks is not null, "Original target symlink metadata available");
        Check.Equal(entries.Symlinks!.Count, 0, "Original target no symlinks");
    }

    private static void ReadBOM()
    {
        using Fixture fixture = new();
        (byte[] Input, string Expected)[] rows =
        [
            ([], ""), ([0xEF, 0xBB, 0xBF, 0x61], "a"), ([0xEF, 0xBB], "\u00ef\u00bb"),
            ([0xFF, 0xFE, 0x61, 0], "a"), ([0xFE, 0xFF, 0, 0x61], "a"),
            ([0xFF, 0xFE, 0x61, 0, 0x62], "a"),
            ([0xFF, 0xFE, 0x3D, 0xD8, 0, 0xDE], GoString.ToUtf8("😀")),
            ([0xFE, 0xFF, 0xD8, 0x3D, 0xDE, 0], GoString.ToUtf8("😀")),
            ([0xFF, 0xFE, 0, 0xD8], GoString.ToUtf8("�")), ([0xFF, 0xFE, 0, 0xDC], GoString.ToUtf8("�")),
            ([0xFF, 0xFE], ""), ([0xFF], "\u00ff"), ([0xED, 0xA0, 0x80], "\u00ed\u00a0\u0080")
        ];
        var fs = OsVfs.FS();
        for (int i = 0; i < rows.Length; i++)
        {
            string native = fixture.PathFor(i + ".bin");
            File.WriteAllBytes(native, rows[i].Input);
            var (contents, ok) = fs.ReadFile(FilePath(native));
            Check.True(ok, "Authored BOM read succeeds " + i);
            Check.Equal(contents, rows[i].Expected, "Authored source-derived BOM bytes " + i);
        }
    }

    private static void LinuxBoundaries()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Authored statx ABI control requires the selected Linux runtime.");
        using Fixture fixture = new();
        var fs = OsVfs.FS();
        string regular = fixture.PathFor("é😀.txt");
        File.WriteAllBytes(regular, [1, 2, 3]);
        File.SetUnixFileMode(regular, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(123456789);
        File.SetLastWriteTimeUtc(regular, time.UtcDateTime);
        var info = fs.Stat(FilePath(regular).AsPath());
        Check.True(info is not null && !info.IsDir(), "Authored statx regular file");
        Check.Equal(info!.Name(), GoString.ToUtf8("é😀.txt"), "Authored statx raw name");
        Check.Equal(info.Size(), 3L, "Authored statx size offset");
        Check.Equal(info.Mode(), (Mode)0x1A0, "Authored statx permission bits");
        Check.Equal(info.ModTime(), time, "Authored statx mtime fields");
        var nul = RootedFilePath.FromGoString(FilePath(regular).AsString() + "\0ignored");
        Check.True(fs.Stat(nul.AsPath()) is null && !fs.FileExists(nul), "Authored NUL never truncates to existing file");
        Check.True(!fs.ReadFile(nul).Ok, "Authored NUL read fails");
        Check.Equal(fs.Realpath(nul.AsPath()), nul.AsPath(), "Authored NUL realpath unchanged");
        var nulDirectory = RootedDirectoryPath.FromGoString(DirectoryPath(fixture.Directory).AsString() + "\0ignored");
        var nulEntries = fs.GetAccessibleEntries(nulDirectory);
        Check.True(!fs.DirectoryExists(nulDirectory) && nulEntries.Files.IsNil && nulEntries.Directories.IsNil, "Authored NUL directory lookup fails");
        Check.True(nulEntries.Symlinks is { Count: 0 }, "Authored NUL directory known empty link metadata");
        string fifo = fixture.PathFor("fifo");
        ProcessStartInfo setup = new("mkfifo") { UseShellExecute = false, RedirectStandardError = true };
        setup.ArgumentList.Add("-m"); setup.ArgumentList.Add("640"); setup.ArgumentList.Add(fifo);
        using (var process = Process.Start(setup)!)
        {
            process.WaitForExit();
            Check.Equal(process.ExitCode, 0, "Authored FIFO native input creation " + process.StandardError.ReadToEnd());
        }
        string socketPath = fixture.PathFor("socket");
        using Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));
        Check.Equal(fs.Stat(FilePath(fifo).AsPath())!.Mode() & Mode.Type, Mode.NamedPipe, "Authored statx FIFO type");
        Check.Equal(fs.Stat(FilePath(socketPath).AsPath())!.Mode() & Mode.Type, Mode.Socket, "Authored statx Unix socket type");
        // Source FileExists accepts non-directory special nodes. Entry discovery
        // excludes them. Never open/read a FIFO or socket during this control.
        Check.True(fs.FileExists(FilePath(fifo)) && fs.FileExists(FilePath(socketPath)), "Authored source special-node file existence");
        string broken = fixture.PathFor("broken"), loop = fixture.PathFor("loop"), good = fixture.PathFor("good");
        File.CreateSymbolicLink(broken, fixture.PathFor("missing"));
        File.CreateSymbolicLink(loop, loop);
        File.CreateSymbolicLink(good, regular);
        Check.True(fs.Stat(FilePath(broken).AsPath()) is null && fs.Stat(FilePath(loop).AsPath()) is null, "Authored broken/loop stat absence");
        Check.Equal(fs.Realpath(FilePath(broken).AsPath()), FilePath(broken).AsPath(), "Authored broken realpath unchanged");
        Check.Equal(fs.Realpath(FilePath(loop).AsPath()), FilePath(loop).AsPath(), "Authored loop realpath unchanged");
        Check.Equal(fs.Stat(FilePath(good).AsPath())!.Mode() & Mode.Type, Mode.Regular, "Authored symlink stat follows regular leaf");
        var entries = fs.GetAccessibleEntries(DirectoryPath(fixture.Directory));
        Check.True(!entries.Files.IsNil && Core.Some(entries.Files, static name => name.EqualsValue(ByteStringValue.FromString("good"))) && Core.Some(entries.Files, static name => name.EqualsValue(ByteStringValue.FromString(GoString.ToUtf8("é😀.txt")))), "Authored regular entry discovery");
        Check.True(!Core.Some(entries.Files, static name => name.EqualsValue(ByteStringValue.FromString("fifo"))) && !Core.Some(entries.Files, static name => name.EqualsValue(ByteStringValue.FromString("socket"))) && !Core.Some(entries.Files, static name => name.EqualsValue(ByteStringValue.FromString("broken"))) && !Core.Some(entries.Files, static name => name.EqualsValue(ByteStringValue.FromString("loop"))), "Authored special/broken entry exclusion");
        Check.True(entries.Symlinks is { Count: 1 } && entries.Symlinks!.Contains(ByteStringKey.FromString("good")), "Authored only accessible symlinks retained");
    }
}
