// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/client.go interface at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;
using TelemetryEvent = TypeScript.Lsp.Protocol.RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull;

namespace TypeScript.ProjectSystem;
// Go context values become the actual request-scoped cancellation/locale/capability owner.
internal interface IProjectClient
{
    Exception? WatchFiles(ProtocolRequestContext context, WatcherID id, Slice<FileSystemWatcher> watchers);
    Exception? UnwatchFiles(ProtocolRequestContext context, WatcherID id);
    Exception? RegisterContentMapperExtensions(ProtocolRequestContext context, Slice<ByteStringValue> extensions);
    Exception? RefreshDiagnostics(ProtocolRequestContext context);
    Exception? PublishDiagnostics(ProtocolRequestContext context, PublishDiagnosticsParams parameters);
    Exception? RefreshInlayHints(ProtocolRequestContext context);
    Exception? RefreshCodeLens(ProtocolRequestContext context);
    void ProgressStart(TypeScript.Scanner.Message message, params object? [] arguments);
    void ProgressFinish(TypeScript.Scanner.Message message, params object? [] arguments);
    Exception? SendTelemetry(ProtocolRequestContext context, TelemetryEvent telemetry);
    bool IsActive();
    void SetLocale(ByteStringValue locale);
    TypeScript.Locale.Locale GetLocale();
}
