// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/server.go ServerOptions. Draft; actual Session/LS consumers pending.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Vfs;
using ParseCache = TypeScript.ProjectSystem.RefCountCache<TypeScript.ProjectSystem.ParseCacheKey, TypeScript.Scanner.SourceFile, TypeScript.ProjectSystem.IFileHandle>;

namespace TypeScript.Lsp;
internal sealed class ServerOptions
{
    internal ILspReader In = null!;
    internal ILspWriter Out = null!;
    internal Stream Err = null!;
    internal RootedDirectoryPath Cwd;
    internal IFS FS = null!;
    internal RootedDirectoryPath DefaultLibraryPath;
    internal RootedDirectoryPath TypingsLocation;
    internal ParseCache? ParseCache;
    internal Func<Protocol.ProtocolRequestContext, ByteStringValue, Slice<ByteStringValue>, (byte[]? Output, Exception? Error)>? NpmInstall;
    // Duplex stream reads the child's stdout and writes to stdin; host owns the
    // actual process adapter and close lifetime, exactly as Go ReadWriteCloser.
    internal Func<Slice<ByteStringValue>, ByteStringValue, Stream, (Stream? Process, Exception? Error)>? Spawn;
    internal TimeSpan ProgressDelay;
    internal Action<long>? SetParentProcessID;
}
