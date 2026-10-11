// Authored Linux disk representability controls. Python creates raw-byte
// fixture names/links only; all tested reads use the actual mapped OS VFS.
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.Vfs.OsVfs;

namespace Configuration.Checks;
internal static class OsRawPathChecks
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Raw native path controls require Linux.");
        string root = Path.Combine(Path.GetTempPath(), "configuration-raw-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Fixture(root, """
                import os,sys
                root=os.fsencode(sys.argv[1])
                os.mkdir(root+b'/collision')
                for name,content in [(b'\xff.ts',b'raw'),('\ufffd.ts'.encode(),b'replacement')]:
                    with open(root+b'/collision/'+name,'wb') as file:file.write(content)
                os.mkdir(root+b'/valid')
                for name in ['\ufffd.ts','\u00e9\U0001f600.ts']:
                    with open(root+b'/valid/'+name.encode(),'wb') as file:file.write(b'valid')
                os.mkdir(root+b'/links')
                os.symlink(b'../collision/\xff.ts',root+b'/links/raw')
                os.symlink('../valid/\ufffd.ts'.encode(),root+b'/links/replacement')
                os.symlink(b'\xff-missing',root+b'/links/raw-missing')
                os.symlink(b'second',root+b'/links/first')
                os.symlink(b'\xff-missing',root+b'/links/second')
                """);
            var fs = OsVfs.FS();
            Reject(() => _ = fs.GetAccessibleEntries(Dir(Path.Combine(root, "collision"))), "Raw FF and U+FFFD filename collision");
            var valid = fs.GetAccessibleEntries(Dir(Path.Combine(root, "valid")));
            Check.Sequence(GoSlices.ToListOrNull(valid.Files)?.Select(static name => name.Materialize()!).ToArray(), new[] { GoString.ToUtf8("é😀.ts"), GoString.ToUtf8("�.ts") }, "Valid scalar filenames retain original byte order");
            foreach (string name in new[]
            {
                "�.ts",
                "é😀.ts"
            }

            )
            {
                var(contents, ok) = fs.ReadFile(File(Path.Combine(root, "valid", name)));
                Check.True(ok, "Valid scalar filename read " + name);
                Check.Equal(contents, "valid", "Valid scalar filename exact contents");
            }

            Reject(() => _ = fs.Realpath(RootedPath.FromUnicode(Path.Combine(root, "links", "raw"))), "Existing invalid link target");
            foreach (string missing in new[] { "raw-missing", "first" })
            {
                var path = RootedPath.FromUnicode(Path.Combine(root, "links", missing));
                Check.Equal(fs.Realpath(path), path, "Original failed O_PATH retains input without decoding a target: " + missing);
            }
            Check.Equal(fs.Realpath(RootedPath.FromUnicode(Path.Combine(root, "links", "replacement"))), RootedPath.FromUnicode(Path.Combine(root, "valid", "�.ts")), "Actual U+FFFD link target remains valid");
        }
        finally
        {
            Fixture(root, "import shutil,sys;shutil.rmtree(sys.argv[1])");
        }
    }

    private static RootedDirectoryPath Dir(string path) => RootedDirectoryPath.FromUnicode(path);
    private static RootedFilePath File(string path) => RootedFilePath.FromUnicode(path);
    private static void Reject(Action operation, string context)
    {
        FileSystemPathEncodingException? captured = null;
        try
        {
            operation();
        }
        catch (FileSystemPathEncodingException error)
        {
            captured = error;
        }

        Check.True(captured is not null, context + " explicitly rejects before lossy decoding");
        Check.True(captured!.PathBytes.Contains((char)255), context + " retains the original invalid byte");
    }

    private static void Fixture(string root, string script)
    {
        ProcessStartInfo start = new("python3")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add(root);
        using Process process = Process.Start(start) ?? throw new IOException("Could not start raw path fixture setup.");
        string output = process.StandardOutput.ReadToEnd(), error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check.Equal(process.ExitCode, 0, "Raw path fixture setup " + output + error);
    }
}
