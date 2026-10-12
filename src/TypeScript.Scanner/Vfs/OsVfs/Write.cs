// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete demanded write path from osvfs/os.go:140-173, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Buffers;
using TypeScript.Scanner;

namespace TypeScript.Vfs.OsVfs;
public static partial class OsVfs
{
    // os.go:24/28. Limits apply across all owners, matching package-level Go semaphores.
    private static readonly SemaphoreSlim blockingWriteDirectorySema = new(128, 128);
    private static readonly SemaphoreSlim writeSema = new(32, 32);
    private sealed partial class OsFS : IOutputTextFileSystem
    {
        // os.go:140. The synchronous byte writer has no Unicode content conversion.
        private static Exception? writeFileWithFlag(string path, ByteStringValue content)
        {
            writeSema.Wait();
            FileStream? file = null;
            try
            {
                if (OperatingSystem.IsLinux())
                {
                    if (content.IsBytes)
                        return LinuxWritePaths.WriteFile(path, content.ByteSpan);
                    byte[] rawBytes = ArrayPool<byte>.Shared.Rent(content.Length);
                    try
                    {
                        int length = System.Text.Encoding.Latin1.GetBytes(content.CharSpan, rawBytes);
                        return LinuxWritePaths.WriteFile(path, rawBytes.AsSpan(0, length));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rawBytes);
                    }
                }

                file = new FileStream(path, System.IO.FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);
                if (content.IsBytes)
                    file.Write(content.ByteSpan);
                else
                {
                    // Public raw-Go string callbacks require a CLR char-to-byte boundary.
                    // Internal emitted bytes take the direct span path above.
                    byte[] bytes = ArrayPool<byte>.Shared.Rent(content.Length);
                    try
                    {
                        int count = System.Text.Encoding.Latin1.GetBytes(content.CharSpan, bytes);
                        file.Write(bytes.AsSpan(0, count));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(bytes);
                    }
                }

                return null;
            }
            catch (Exception error)when (readFailure(error) || error is ArgumentException or NotSupportedException)
            {
                return error;
            }
            finally
            {
                // Original deferred Close does not replace the Write result with a close error.
                try
                {
                    try
                    {
                        file?.Dispose();
                    }
                    catch (Exception error)when (readFailure(error))
                    {
                    }
                }
                finally
                {
                    writeSema.Release();
                }
            }
        }

        // os.go:157. Parent creation uses original 0777 subject to the OS umask.
        private static Exception? ensureDirectoryExists(string path)
        {
            blockingWriteDirectorySema.Wait();
            try
            {
                if (OperatingSystem.IsLinux())
                    return LinuxWritePaths.MkdirAll(path);
                Directory.CreateDirectory(path);
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

        // os.go:162. Every first write failure attempts parent creation, then retries exactly once.
        public Exception? WriteOutputText(RootedFilePath path, ByteStringValue content)
        {
            string native;
            string directory;
            try
            {
                native = Native(path.AsValue());
                directory = Native(path.Directory().AsValue());
            }
            catch (FileSystemPathEncodingException encodingError)
            {
                return encodingError;
            }

            if (writeFileWithFlag(native, content)is null)
                return null;
            var error = ensureDirectoryExists(directory);
            return error ?? writeFileWithFlag(native, content);
        }

        // os.go:172. O_WRONLY|O_CREATE|O_TRUNC corresponds to FileMode.Create above.
        public Exception? WriteFile(RootedFilePath path, string content) => WriteOutputText(path, ByteStringValue.FromString(content));
    }
}
