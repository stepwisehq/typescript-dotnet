// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem;
// internal/project/checkerpool.go:22. Value copied into each pool.
internal readonly record struct CheckerPoolOptions(long MaxCheckers, TimeSpan IdleTimeout);
// internal/project/session.go:68. Immutable initialization owner shared by snapshots.
internal sealed record SessionOptions
{
    internal RootedDirectoryPath CurrentDirectory { get; init; }
    internal RootedDirectoryPath DefaultLibraryPath { get; init; }
    internal RootedDirectoryPath TypingsLocation { get; init; }
    internal PositionEncodingKind PositionEncoding { get; init; }
    internal bool WatchEnabled { get; init; }
    internal bool LoggingEnabled { get; init; }
    internal bool TelemetryEnabled { get; init; }
    internal bool PushDiagnosticsEnabled { get; init; }
    internal bool RunExternalCode { get; init; }
    internal TimeSpan DebounceDelay { get; init; }
    internal CheckerPoolOptions CheckerPoolOptions { get; init; }
}
