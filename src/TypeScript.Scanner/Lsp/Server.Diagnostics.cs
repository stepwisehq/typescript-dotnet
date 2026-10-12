// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1888–1958; real service/context emit consumer draft.
using TypeScript.Compiler;
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ProtocolDiagnostic = TypeScript.Lsp.Protocol.Diagnostic;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport Value, Exception? Error) HandleDocumentDiagnostic(ProtocolRequestContext context, LanguageService service, DocumentDiagnosticParams parameters)
    {
        context = context.WithCheckerLifetime(CheckerLifetime.Diagnostics);
        if (flakeLogging == DiagnosticFlakeLogLevel.Off)
            return service.ProvideDiagnostics(context, parameters.TextDocument.Uri);
        var(direct, error) = service.ProvideDiagnostics(context, parameters.TextDocument.Uri);
        if (error is not null)
            return (direct, error);
        // Required complete Program.Emit context owner preserves request ID/lifetime.
        // The original observer deliberately discards emitted output and its result.
        service.GetProgram().Emit(context, new EmitOptions { WriteFile = static (fileName, text, data) => null });
        var(secondary, secondaryError) = service.ProvideDiagnostics(context, parameters.TextDocument.Uri);
        if (secondaryError is not null)
            return (direct, error);
        var(missingFromPre, missingFromPost) = ProtocolUtilities.CompareDiagnostics(direct.FullDocumentDiagnosticReport!.Items, secondary.FullDocumentDiagnosticReport!.Items);
        if (missingFromPre.Count == 0 && missingFromPost.Count == 0)
            return (direct, error);
        var diff = GenerateDiagnosticDiffString(missingFromPre, missingFromPost, static diagnostic => diagnostic.AsString());
        logger.Error(diff);
        if (telemetryEnabled)
        {
            var sanitized = GenerateDiagnosticDiffString(missingFromPre, missingFromPost, static diagnostic => diagnostic.CodeAsString());
            _ = SendNotification(Method.TelemetryEventInfo, new RequestFailureTelemetryEventOrPerformanceStatsTelemetryEventOrProjectInfoTelemetryEventOrNull { RequestFailureTelemetryEvent = new RequestFailureTelemetryEvent { Properties = new RequestFailureTelemetryProperties { ErrorCode = ErrorCode.InternalError.String(), RequestMethod = ByteStringValue.FromString("textDocument.diagnostic.flakeLog"), Stack = sanitized } } });
        }

        if (flakeLogging == DiagnosticFlakeLogLevel.Panic)
            throw new InvalidOperationException(GoString.ToString(ByteStringValue.Concat(ByteStringValue.FromString("flaky diagnostic(s) logged:\n"), diff).Materialize()!));
        return (direct, error);
    }

    private static ByteStringValue GenerateDiagnosticDiffString(Slice<ProtocolDiagnostic?> missingFromPre, Slice<ProtocolDiagnostic?> missingFromPost, Func<ProtocolDiagnostic, ByteStringValue> stringify)
    {
        var builder = new GoBuilder();
        foreach (var diagnostic in missingFromPre)
        {
            builder.WriteString("Diagnostic ");
            builder.WriteString(stringify(diagnostic!));
            builder.WriteString(" was present after emit but not before emit\n");
        }

        foreach (var diagnostic in missingFromPost)
        {
            builder.WriteString("Diagnostic ");
            builder.WriteString(stringify(diagnostic!));
            builder.WriteString(" was present before emit but not after emit\n");
        }

        return builder.Value();
    }
}
