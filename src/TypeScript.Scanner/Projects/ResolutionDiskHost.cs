// Authored read-host seam over the accepted scalar-path OS filesystem.
// Resolution itself belongs to the complete source-mapped Module subsystem.
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.Vfs.OsVfs;

namespace TypeScript.Compiler;
internal sealed class ResolutionDiskHost(IFS fileSystem, RootedDirectoryPath directory) : IResolutionHost
{
    public IFS FS() => fileSystem;
    public RootedDirectoryPath GetCurrentDirectory() => directory;
    internal static ResolutionDiskHost ForDirectory(RootedDirectoryPath directory) => new(OsVfs.FS(), directory);
}
