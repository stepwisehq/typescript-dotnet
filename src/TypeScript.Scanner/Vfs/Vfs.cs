// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified read-side C# port of tsc/internal/vfs/vfs.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Mutating operations are separate.
using TypeScript.Scanner;

namespace TypeScript.Vfs;
/// <summary>Filesystem over original Go-byte-string rooted paths; reads return original byte strings.</summary>
public interface IFS
{
    CaseSensitivity CaseSensitivity();
    bool FileExists(RootedFilePath path);
    (string Contents, bool Ok) ReadFile(RootedFilePath path);
    bool DirectoryExists(RootedDirectoryPath path);
    Entries GetAccessibleEntries(RootedDirectoryPath path);
    FileInfo? Stat(RootedPath path);
    RootedPath Realpath(RootedPath path);
}

public sealed class Entries
{
    public List<string>? Files;
    public List<string>? Directories;
    // Null means unavailable metadata; allocated-empty means known no links.
    public HashSet<string>? Symlinks;
}

[Flags]
public enum FileMode : uint
{
    Regular = 0,
    Dir = 1u << 31,
    Append = 1u << 30,
    Exclusive = 1u << 29,
    Temporary = 1u << 28,
    Symlink = 1u << 27,
    Device = 1u << 26,
    NamedPipe = 1u << 25,
    Socket = 1u << 24,
    Setuid = 1u << 23,
    Setgid = 1u << 22,
    CharDevice = 1u << 21,
    Sticky = 1u << 20,
    Irregular = 1u << 19,
    Type = Dir | Symlink | NamedPipe | Socket | Device | CharDevice | Irregular,
    Perm = 0x1FF
}

public interface FileInfo
{
    string Name();
    long Size();
    FileMode Mode();
    DateTimeOffset ModTime();
    bool IsDir();
    object? Sys();
}

public interface DirEntry
{
    string Name();
    bool IsDir();
    FileMode Type();
    (FileInfo? Info, Exception? Error) Info();
}

public delegate Exception? WalkDirFunc(RootedPath path, DirEntry? entry, Exception? error);
public static partial class Vfs
{
    public static readonly Exception ErrNotExist = new IOException("file does not exist");
    public static readonly Exception ErrPermission = new IOException("permission denied");
    public static readonly Exception ErrInvalid = new IOException("invalid argument");
    public static readonly Exception ErrExist = new IOException("file already exists");
    public static readonly Exception ErrClosed = new IOException("file already closed");
    public static readonly Exception SkipDir = new IOException("skip this directory");
    public static readonly Exception SkipAll = new IOException("skip everything and stop the walk");
}
