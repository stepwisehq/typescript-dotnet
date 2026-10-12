// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Model owners from internal/project/ata/ata.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.ProjectSystem.Logging;

namespace TypeScript.ProjectSystem.Ata;
// ata.go:21. Pointer fields retain their original owners; set entries are raw names.
internal sealed class TypingsInfo
{
    internal TypeAcquisition? TypeAcquisition;
    internal CompilerOptions CompilerOptions = null!;
    internal HashSet<ByteStringKey>? UnresolvedImports;
    // ata.go:27. Go sets compare nil and allocated-empty as equal.
    internal bool Equals(TypingsInfo other) => (TypeAcquisition == null ? other.TypeAcquisition == null : TypeAcquisition.Equals(other.TypeAcquisition)) && CompilerOptions.GetAllowJS() == other.CompilerOptions.GetAllowJS() && (UnresolvedImports == null ? other.UnresolvedImports == null || other.UnresolvedImports.Count == 0 : other.UnresolvedImports == null ? UnresolvedImports.Count == 0 : UnresolvedImports.SetEquals(other.UnresolvedImports));
}

// ata.go:33.
internal sealed class CachedTyping
{
    internal RootedFilePath TypingsLocation;
    internal TypeScript.Semver.Version? Version;
}

// ata.go:38. Go int is Linux64; narrowing only occurs at the actual CLR semaphore owner.
internal sealed class TypingsInstallerOptions
{
    internal RootedDirectoryPath TypingsLocation;
    internal long ThrottleLimit;
}

// ata.go:43. Process execution is injected; output remains raw bytes.
internal interface INpmExecutor
{
    (Slice<byte> Output, Exception? Error) NpmInstall(CancellationToken context, RootedDirectoryPath cwd, Slice<ByteStringValue> args);
}

// ata.go:85. Original fmt.Stringer role is shared with project ID logging.
internal interface IAtaProjectID : IGoLogStringer
{
}

// ata.go:104.
internal sealed class TypingsInstallRequest
{
    internal IAtaProjectID ProjectID = null!;
    internal TypingsInfo TypingsInfo = null!;
    internal Slice<RootedFilePath> FileNames;
    internal RootedDirectoryPath ProjectRootPath;
    internal IFS FS = null!;
    internal ILogger Logger = null!;
}

// ata.go:113.
internal sealed class TypingsInstallResult
{
    internal Slice<RootedFilePath> TypingsFiles;
    internal Slice<RootedPath> FilesToWatch;
}
