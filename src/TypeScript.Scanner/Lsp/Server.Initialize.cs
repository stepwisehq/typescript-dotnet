// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1537–1590,1709–1711; ignored genuine initialize owner.
using TypeScript.Json;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (InitializeResult? Value, Exception? Error) HandleInitialize(ProtocolRequestContext context, InitializeParams parameters, RequestMessage request)
    {
        if (initializeParams is not null)
            return (null, new ProtocolErrorException(ErrorCode.InvalidRequest));
        Interlocked.Exchange(ref initStarted, 1);
        initializeParams = parameters;
        initializationOptions = parameters.InitializationOptions?.InitializationOptions ?? new InitializationOptions();
        if (initializationOptions.LogVerbosity is { } verbosity && ServerLogger.IsValidLogVerbosity(verbosity))
            logger.SetVerbosity(verbosity);
        if (initializationOptions.TrackFlakyDiagnostics is { } tracking)
            flakeLogging = tracking;
        clientCapabilities = parameters.Capabilities.Resolve();
        if (clientCapabilities.Window.WorkDoneProgress)
            projectProgress = ProjectLoadingProgress.NewProjectLoadingProgress(this, progressDelay);
        // The source marshals *ResolvedClientCapabilities, not its value.
        // The lazily constructed explicit pointer-root descriptor preserves that admission.
        var(capabilitiesJson, error) = JsonCodec.MarshalIndentValue(new ProtocolPayload<ProtocolPointer<ResolvedClientCapabilities>?>(ProtocolServerDescriptors.ResolvedClientCapabilitiesPointer, new ProtocolPointer<ResolvedClientCapabilities>(clientCapabilities)), ByteStringValue.FromString(""), ByteStringValue.FromString("\t"));
        if (error is not null)
            return (null, error);
        logger.Info(ByteStringValue.Concat(ByteStringValue.FromString("Resolved client capabilities: "), capabilitiesJson));
        positionEncoding = PositionEncodingKind.UTF16;
        foreach (var encoding in clientCapabilities.General.PositionEncodings)
            if (encoding == PositionEncodingKind.UTF8)
            {
                positionEncoding = PositionEncodingKind.UTF8;
                break;
            }

        if (parameters.Locale is { } localeText)
            (locale, _) = TypeScript.Locale.Locales.Parse(localeText.Materialize()!);
        initLocale = locale;
        if (startWatchdog is not null && parameters.ProcessId.Integer is { } processID)
            startWatchdog(processID);
        return (new InitializeResult { ServerInfo = new ServerInfo { Name = ByteStringValue.FromString("typescript"), Version = ByteStringValue.FromString(Core.Version()) }, Capabilities = CreateServerCapabilities() }, null);
    }
}
