// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Required editor mutations from internal/vfs/osvfs/os.go:180–188.
using TypeScript.Scanner;

namespace TypeScript.Vfs.OsVfs;
public static partial class OsVfs
{
    private sealed partial class OsFS
    {
        // os.go:180. The runtime filesystem owns recursive removal; symlinks
        // are removed themselves, rather than recursing into their targets.
        public Exception? Remove(RootedPath path)
        {
            blockingWriteDirectorySema.Wait();
            try
            {
                var native = Native(path.AsValue());
                if (OperatingSystem.IsLinux() && IntPtr.Size == 8)
                    return LinuxWritePaths.RemoveAll(native);
                try
                {
                    var attributes = File.GetAttributes(native);
                    if ((attributes & FileAttributes.Directory) != 0)
                        Directory.Delete(native, recursive: true);
                    else
                        File.Delete(native);
                    return null;
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    return null;
                }
            }
            catch (Exception error)when (readFailure(error) || error is ArgumentException or NotSupportedException)
            {
                return error;
            }
            finally
            {
                blockingWriteDirectorySema.Release();
            }
        }

        // os.go:186. Linux applies both timestamps in the same native operation.
        public Exception? Chtimes(RootedPath path, DateTimeOffset accessTime, DateTimeOffset modificationTime)
        {
            blockingWriteDirectorySema.Wait();
            try
            {
                var native = Native(path.AsValue());
                if (OperatingSystem.IsLinux() && IntPtr.Size == 8)
                    return LinuxWritePaths.Chtimes(native, accessTime, modificationTime);
                File.SetLastAccessTimeUtc(native, accessTime.UtcDateTime);
                File.SetLastWriteTimeUtc(native, modificationTime.UtcDateTime);
                return null;
            }
            catch (Exception error)when (readFailure(error) || error is ArgumentException or NotSupportedException)
            {
                return error;
            }
            finally
            {
                blockingWriteDirectorySema.Release();
            }
        }
    }
}
