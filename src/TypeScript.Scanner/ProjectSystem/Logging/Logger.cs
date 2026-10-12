// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/logging/logger.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem.Logging;
internal interface ILogger
{
    void Error(params object? [] message);
    void Errorf(string format, params object? [] args);
    void Warn(params object? [] message);
    void Warnf(string format, params object? [] args);
    void Info(params object? [] message);
    void Infof(string format, params object? [] args);
    void Log(params object? [] message);
    void Logf(string format, params object? [] args);
    ILogger? Verbose();
    bool IsVerbose();
    void SetVerbose(bool verbose);
}

internal class Logger : ILogger
{
    private readonly object gate = new();
    private bool verbose;
    private readonly Stream writer;
    private readonly Func<string> prefix;
    protected Logger(Stream output, Func<string> prefix)
    {
        writer = output;
        this.prefix = prefix;
    }

    // logger.go:48 Log; raw Go bytes are written at the external stream boundary.
    public void Log(params object? [] message)
    {
        lock (gate)
            Write(prefix() + " " + LogFormat.Sprint(message) + "\n");
    }

    // logger.go:58 Logf.
    public void Logf(string format, params object? [] args)
    {
        lock (gate)
            Write(prefix() + " " + LogFormat.Sprintf(format, args) + "\n");
    }

    private void Write(string bytes)
    {
        // Upstream ignores fmt.Fprint's returned write error. CLR IO/disposed
        // failures represent that error channel; other exceptional failures escape.
        try
        {
            writer.Write(GoString.Bytes(bytes));
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    // logger.go:67–94 Verbose/IsVerbose/SetVerbose.
    public ILogger? Verbose()
    {
        lock (gate)
            return verbose ? this : null;
    }

    public bool IsVerbose()
    {
        lock (gate)
            return verbose;
    }

    public void SetVerbose(bool value)
    {
        lock (gate)
            verbose = value;
    }

    public void Error(params object? [] message) => Log(message);
    public void Errorf(string format, params object? [] args) => Logf(format, args);
    public void Warn(params object? [] message) => Log(message);
    public void Warnf(string format, params object? [] args) => Logf(format, args);
    public void Info(params object? [] message) => Log(message);
    public void Infof(string format, params object? [] args) => Logf(format, args);
    // logger.go:120 NewLogger.
    public static ILogger NewLogger(Stream output) => new Logger(output, static () => FormatTime(DateTimeOffset.Now));
    // logger.go:130 NewNopLogger. Go returns an interface containing a nil pointer;
    // a stateless managed singleton preserves callable no-op behavior.
    public static ILogger NewNopLogger() => NopLogger.Instance;
    internal static string FormatTime(DateTimeOffset time) => "[" + time.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "]";
    private sealed class NopLogger : ILogger
    {
        internal static readonly NopLogger Instance = new();
        public void Error(params object? [] message)
        {
        }

        public void Errorf(string format, params object? [] args)
        {
        }

        public void Warn(params object? [] message)
        {
        }

        public void Warnf(string format, params object? [] args)
        {
        }

        public void Info(params object? [] message)
        {
        }

        public void Infof(string format, params object? [] args)
        {
        }

        public void Log(params object? [] message)
        {
        }

        public void Logf(string format, params object? [] args)
        {
        }

        public ILogger? Verbose() => null;
        public bool IsVerbose() => false;
        public void SetVerbose(bool verbose)
        {
        }
    }
}
