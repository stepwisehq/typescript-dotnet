// Authored bounded disk adapter. Canonical path reduction uses source-mapped TsPath.
using System.Text;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class ProjectDiskHost
{
    private static readonly UTF8Encoding scalarPathEncoding = new(false, true);
    internal string Root { get; }

    // Source JS strings can contain WTF-8 lone surrogates. The bounded .NET
    // filesystem host only represents Unicode scalar paths; never replace bytes
    // and resolve a different filename. General scanner/string values stay intact.
    internal static string DecodeModuleReference(string bytes)
    {
        // This authored scalar-disk boundary needs no conversion for ASCII.
        // Invalid/WTF-8 and all non-ASCII retain the original strict decoder.
        if (bytes is not null && Ascii.IsValid(bytes.AsSpan()))
            return bytes;
        try
        {
            return scalarPathEncoding.GetString(GoString.Bytes(bytes!));
        }
        catch (DecoderFallbackException)
        {
            throw new ProjectException("pathEncoding", "Module specifier cannot be represented as a Unicode scalar disk path.");
        }
    }

    private readonly Dictionary<string, byte[]> texts = new(StringComparer.Ordinal);
    internal ProjectDiskHost(string root)
    {
        Root = TsPath.GetNormalizedAbsolutePath(root, "");
        CheckSymlinks(Root);
    }

    internal string Resolve(string directory, string relative)
    {
        if (relative.IndexOf('\0') >= 0 || TsPath.GetRootLength(relative) != 0 || relative.Contains("://", StringComparison.Ordinal) || relative.StartsWith("^/", StringComparison.Ordinal))
            throw new ProjectException("path", "Only relative disk paths are supported: " + relative, relative);
        string path = TsPath.GetNormalizedAbsolutePath(relative, directory);
        EnsureBounded(path);
        return path;
    }

    internal void EnsureBounded(string path, bool inspectSymlinks = true)
    {
        if (path != Root && !path.StartsWith(TsPath.EnsureTrailingDirectorySeparator(Root), StringComparison.Ordinal))
            throw new ProjectException("outsideRoot", "Project dependency escapes the configuration directory: " + path, path);
        if (path.Split('/').Contains("node_modules", StringComparer.Ordinal))
            throw new ProjectException("nodeModules", "node_modules paths are unsupported: " + path, path);
        if (inspectSymlinks)
            CheckSymlinks(path);
    }

    internal bool FileExists(string path)
    {
        EnsureBounded(path);
        return File.Exists(path);
    }

    // Original modulespecifiers.processEnding probes directory + each supported
    // extension before dropping /index. For the root directory these exact
    // existence-only candidates lie beside the root, never become dependencies.
    internal bool ModuleSpecifierFileExists(string path)
    {
        if (path.StartsWith(Root, StringComparison.Ordinal) && new[]
        {
            ".ts",
            ".tsx",
            ".d.ts",
            ".js",
            ".jsx",
            ".cts",
            ".d.cts",
            ".cjs",
            ".mts",
            ".d.mts",
            ".mjs",
            ".node",
            ".json"
        }.Contains(path[Root.Length..], StringComparer.Ordinal))
        {
            CheckSymlinks(path);
            return File.Exists(path);
        }

        return FileExists(path);
    }

    internal bool DirectoryExists(string path)
    {
        EnsureBounded(path);
        return Directory.Exists(path);
    }

    internal byte[] Read(string path)
    {
        EnsureBounded(path);
        if (texts.TryGetValue(path, out byte[]? bytes))
            return bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (IOException ex)
        {
            throw new ProjectException("read", ex.Message, path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ProjectException("read", ex.Message, path);
        }

        texts.Add(path, bytes);
        return bytes;
    }

    // Ancestor package metadata is consulted by genuine module-specifier generation.
    // It can lie above the dependency root, but this adapter never permits arbitrary
    // outside-root files: only package.json in actual root ancestors is admitted.
    private void EnsureMetadataDirectory(string directory)
    {
        if (directory != Root && !directory.StartsWith(TsPath.EnsureTrailingDirectorySeparator(Root), StringComparison.Ordinal) && !Root.StartsWith(TsPath.EnsureTrailingDirectorySeparator(directory), StringComparison.Ordinal))
            throw new ProjectException("outsideRoot", "Package metadata is not in a project ancestor: " + directory, directory);
        CheckSymlinks(directory);
    }

    internal bool MetadataDirectoryExists(string directory)
    {
        EnsureMetadataDirectory(directory);
        return Directory.Exists(directory);
    }

    internal bool MetadataFileExists(string path)
    {
        EnsureMetadataDirectory(TsPath.GetDirectoryPath(path));
        CheckSymlinks(path);
        return File.Exists(path);
    }

    internal byte[] ReadMetadata(string path)
    {
        EnsureMetadataDirectory(TsPath.GetDirectoryPath(path));
        CheckSymlinks(path);
        // resolver.go ignores a read error and parses the empty contents.
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            return[];
        }
        catch (UnauthorizedAccessException)
        {
            return[];
        }
    }

    internal static void CheckSymlinks(string path)
    {
        string root = Path.GetPathRoot(path) ?? "";
        string current = root;
        foreach (string segment in path[root.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            // LinkTarget includes dangling links; reject before following either file/directory.
            if (new FileInfo(current).LinkTarget is not null || new DirectoryInfo(current).LinkTarget is not null)
                throw new ProjectException("symlink", "Symlink-dependent project paths are unsupported: " + current, current);
        }
    }
}

// The legacy entry keeps its named admission policy; the Module algorithm is
// shared with full resolution. Metadata reads retain their ancestor exception.
internal interface ILegacyResolutionAdmissionHost
{
    void ValidateDirectoryPackageJson(RootedDirectoryPath directory);
}

internal sealed class LegacyResolutionHost(ProjectDiskHost disk) : TypeScript.Modules.IResolutionHost, TypeScript.Vfs.IFS, ILegacyResolutionAdmissionHost
{
    private bool resolvingModule;

    // Authored named legacy admission surrounds the actual shared resolver.
    // Candidate search and publication remain exclusively Module responsibilities.
    internal TypeScript.Modules.ResolvedModule? ResolveModuleName(TypeScript.Modules.DefaultResolver resolver,
        string moduleName, RootedFilePath containingFile, ResolutionMode mode)
    {
        string name = ProjectDiskHost.DecodeModuleReference(moduleName);
        if (!TsPath.PathIsRelative(name))
            throw new ProjectException("moduleSpecifier", "Only relative module specifiers are supported: " + name, containingFile.ToUnicode());
        if (mode is not (ResolutionMode.None or ResolutionMode.ESNext))
            throw new ProjectException("resolutionMode", "Only Bundler ES-module resolution is supported.", containingFile.ToUnicode());
        string candidate = TsPath.ResolvePath(containingFile.Directory().ToUnicode(), name);
        disk.EnsureBounded(candidate, inspectSymlinks: false);
        bool previous = resolvingModule;
        resolvingModule = true;
        try
        {
            var(result, _, error) = resolver.ResolveModuleName(moduleName, containingFile, mode);
            if (error is not null) throw error;
            return result;
        }
        finally
        {
            resolvingModule = previous;
        }
    }

    public void ValidateDirectoryPackageJson(RootedDirectoryPath directory)
    {
        if (!resolvingModule) return;
        string packageJson = TsPath.CombinePaths(directory.ToUnicode(), "package.json");
        if (disk.FileExists(packageJson))
            throw new ProjectException("packageJsonResolution", "Resolution requires unsupported package metadata: " + packageJson, packageJson);
    }

    public TypeScript.Vfs.IFS FS() => this;
    public RootedDirectoryPath GetCurrentDirectory() => RootedDirectoryPath.FromUnicode(disk.Root);
    public CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseSensitive;
    public bool FileExists(RootedFilePath path)
    {
        string text = path.ToUnicode();
        if (path.BaseName() == "package.json") return disk.MetadataFileExists(text);
        bool exists = disk.FileExists(text);
        if (exists && resolvingModule && (!text.EndsWith(".ts", StringComparison.Ordinal) || TsPath.IsDeclarationFileName(text)))
            throw new ProjectException("resolvedFileForm", "Upstream resolution selects an unsupported user file: " + text, text);
        return exists;
    }
    public (string Contents, bool Ok) ReadFile(RootedFilePath path)
    {
        string text = path.ToUnicode();
        if (path.BaseName() == "package.json")
        {
            byte[] bytes = disk.ReadMetadata(text);
            return (GoString.FromBytes(bytes), bytes.Length != 0);
        }
        if (!disk.FileExists(text)) return ("", false);
        return (GoString.FromBytes(disk.Read(text)), true);
    }
    public bool DirectoryExists(RootedDirectoryPath path) => disk.MetadataDirectoryExists(path.ToUnicode());
    public TypeScript.Vfs.Entries GetAccessibleEntries(RootedDirectoryPath path)
    {
        disk.EnsureBounded(path.ToUnicode());
        return TypeScript.Vfs.OsVfs.OsVfs.FS().GetAccessibleEntries(path);
    }
    public TypeScript.Vfs.FileInfo? Stat(RootedPath path)
    {
        disk.EnsureBounded(path.ToUnicode());
        return TypeScript.Vfs.OsVfs.OsVfs.FS().Stat(path);
    }
    public RootedPath Realpath(RootedPath path)
    {
        disk.EnsureBounded(path.ToUnicode());
        return TypeScript.Vfs.OsVfs.OsVfs.FS().Realpath(path);
    }
}
