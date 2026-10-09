// Authored host adapter for original resolver test fixtures. It reuses the
// accepted read-side virtual host; it is not a port of the writable vfstest API.
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace Resolution.Checks;
internal sealed class ResolutionHost(IFS fileSystem, RootedDirectoryPath directory) : IResolutionHost
{
    public IFS FS() => fileSystem;
    public RootedDirectoryPath GetCurrentDirectory() => directory;
}

internal static class ResolutionFixtures
{
    public static ConfigurationFileSystem Fs(params (string Path, string Contents)[] files) => new(files.Select(file => new ConfigurationFileSystem.Input(GoString.ToUtf8(file.Path), GoString.ToUtf8(file.Contents))));
    public static ResolutionHost Host(IFS fs, string directory = "/repo") => new(fs, RootedDirectoryPath.FromUnicode(directory));
    public static CompilerOptions Bundler(bool trace = false) => new()
    {
        ModuleResolution = ModuleResolutionKind.Bundler,
        Module = ModuleKind.ESNext,
        Target = ScriptTarget.ESNext,
        TraceResolution = trace ? Tristate.True : Tristate.Unknown
    };
}
