// Original project tests use vfstest.FromMap. Reuse the accepted rooted-path
// compiler fixture for its read/symlink algorithms, with explicit test mutations.
// This finite fixture is not a production filesystem or session implementation.
using TypeScript.Scanner;
using TypeScript.Vfs;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
internal sealed class ProjectTestFileSystem : IFS, IMutableFileSystem
{
    private readonly object gate = new();
    private readonly CaseSensitivity sensitivity;
    private readonly Dictionary<PathKey, BaselineFS.Input> inputs = new();
    private BaselineFS current;
    internal ProjectTestFileSystem(CaseSensitivity sensitivity, params BaselineFS.Input[] files)
    {
        this.sensitivity = sensitivity;
        foreach (var file in files)
            inputs.Add(sensitivity.PathKey(RootedPath.FromGoString(file.Path)), file);
        current = new(inputs.Values, sensitivity);
    }

    public CaseSensitivity CaseSensitivity() => sensitivity;
    public bool FileExists(RootedFilePath path)
    {
        lock (gate)
            return current.FileExists(path);
    }

    public bool DirectoryExists(RootedDirectoryPath path)
    {
        lock (gate)
            return current.DirectoryExists(path);
    }

    public (string Contents, bool Ok) ReadFile(RootedFilePath path)
    {
        lock (gate)
            return current.ReadFile(path);
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath path)
    {
        lock (gate)
            return current.GetAccessibleEntries(path);
    }

    public TypeScript.Vfs.FileInfo? Stat(RootedPath path)
    {
        lock (gate)
            return current.Stat(path);
    }

    public RootedPath Realpath(RootedPath path)
    {
        lock (gate)
            return current.Realpath(path);
    }

    public Exception? WriteFile(RootedFilePath path, string content)
    {
        lock (gate)
        {
            var actual = current.Realpath(path.AsPath());
            inputs[sensitivity.PathKey(actual)] = new(actual.AsString(), content);
            current = new(inputs.Values, sensitivity);
            return null;
        }
    }

    public Exception? Remove(RootedPath path)
    {
        lock (gate)
        {
            var key = sensitivity.PathKey(path);
            foreach (var candidate in inputs.Keys.ToArray())
                if (key.ContainsPath(candidate))
                    inputs.Remove(candidate);
            current = new(inputs.Values, sensitivity);
            return null;
        }
    }

    public Exception? AppendFile(RootedFilePath path, ByteStringValue content)
    {
        lock (gate)
        {
            var(old, _) = current.ReadFile(path);
            return WriteFile(path, old + content.Materialize());
        }
    }

    public Exception? Chtimes(RootedPath path, DateTimeOffset access, DateTimeOffset modification) => throw new NotSupportedException("Original project fixture does not request clocks");
}
