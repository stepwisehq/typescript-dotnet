// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/lsp/logger.go. Actual Server fields are reconciled at whole-owner promotion.
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed class ServerLogger : ILogger
{
    private readonly Server server;
    private readonly object mutex = new();
    private LogVerbosity verbosity = LogVerbosity.Info;
    private ServerLogger(Server server) => this.server = server;
    internal static ServerLogger NewLogger(Server server) => new(server);
    internal static LogVerbosity MaxVerbosityForMessageType(MessageType type) => type switch
    {
        MessageType.Error => LogVerbosity.Error,
        MessageType.Warning => LogVerbosity.Warning,
        MessageType.Info => LogVerbosity.Info,
        MessageType.Debug => LogVerbosity.Debug,
        _ => LogVerbosity.Info,
    };
    internal static bool IsValidLogVerbosity(LogVerbosity value) => value >= LogVerbosity.Off && value <= LogVerbosity.Error;
    private void SendLogMessage(MessageType type, ByteStringValue message)
    {
        if (Volatile.Read(ref server.initStarted) == 0)
        {
            WriteLine(message);
            return;
        }

        LogVerbosity current;
        lock (mutex)
            current = verbosity;
        if (current == LogVerbosity.Off || current > MaxVerbosityForMessageType(type))
            return;
        var notification = Method.WindowLogMessageInfo.NewNotificationMessage(new LogMessageParams { Type = type, Message = message });
        if (server.outgoingQueue.Put(server.backgroundCtx, notification.Message())is not null && server.backgroundCtx.IsCancellationRequested)
            WriteLine(message);
    }

    private void WriteLine(ByteStringValue message)
    {
        // fmt.Fprintln ignores its actual I/O error; retain raw UTF8 bytes and LF.
        try
        {
            var line = new GoBuilder();
            line.Grow(message.Length + 1);
            line.WriteString(message);
            line.WriteByte('\n');
            server.stderr.Write(line.Value().ByteSpan);
        }
        catch (Exception error)when (error is IOException or ObjectDisposedException)
        {
        }
    }

    public void Log(params object? [] message) => SendLogMessage(MessageType.Info, ByteStringValue.FromString(LogFormat.Sprint(message)));
    public void Logf(string format, params object? [] args) => SendLogMessage(MessageType.Info, ByteStringValue.FromString(LogFormat.Sprintf(format, args)));
    public ILogger? Verbose()
    {
        lock (mutex)
            return verbosity == LogVerbosity.Off || verbosity > LogVerbosity.Debug ? null : this;
    }

    public bool IsVerbose()
    {
        lock (mutex)
            return verbosity >= LogVerbosity.Trace && verbosity <= LogVerbosity.Debug;
    }

    public void SetVerbose(bool value)
    {
        lock (mutex)
            verbosity = value ? LogVerbosity.Debug : LogVerbosity.Info;
    }

    internal bool IsTracing()
    {
        lock (mutex)
            return verbosity == LogVerbosity.Trace;
    }

    internal void SetVerbosity(LogVerbosity value)
    {
        lock (mutex)
            verbosity = value;
    }

    public void Error(params object? [] message) => SendLogMessage(MessageType.Error, ByteStringValue.FromString(LogFormat.Sprint(message)));
    public void Errorf(string format, params object? [] args) => SendLogMessage(MessageType.Error, ByteStringValue.FromString(LogFormat.Sprintf(format, args)));
    public void Warn(params object? [] message) => SendLogMessage(MessageType.Warning, ByteStringValue.FromString(LogFormat.Sprint(message)));
    public void Warnf(string format, params object? [] args) => SendLogMessage(MessageType.Warning, ByteStringValue.FromString(LogFormat.Sprintf(format, args)));
    public void Info(params object? [] message) => SendLogMessage(MessageType.Info, ByteStringValue.FromString(LogFormat.Sprint(message)));
    public void Infof(string format, params object? [] args) => SendLogMessage(MessageType.Info, ByteStringValue.FromString(LogFormat.Sprintf(format, args)));
}
