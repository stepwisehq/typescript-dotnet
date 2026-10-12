// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// cmd/tsc/lsp.go, sys.go:74–134 and isprocessalive_unix.go at the pinned revision.
// Standard .NET process/stdio adapters; Linux signal-zero probe retains EPERM admission.
using System.Diagnostics;
using System.Runtime.InteropServices;
using TypeScript.ContentMapper;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.Vfs.OsVfs;

namespace TypeScript.Lsp;
public static partial class LspCommand
{
    public static int Run(string[] args)
    {
        bool stdio = false;
        long clientProcessId = 0;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg is "--stdio" or "-stdio")
            {
                stdio = true;
                continue;
            }

            if (arg == "--" || !arg.StartsWith('-'))
                break;
            int equals = arg.IndexOf('=');
            if (equals >= 0 && arg[..equals] is "--stdio" or "-stdio")
            {
                string boolean = arg[(equals + 1)..];
                if (boolean is "1" or "t" or "T" or "TRUE" or "true" or "True")
                    stdio = true;
                else if (boolean is "0" or "f" or "F" or "FALSE" or "false" or "False")
                    stdio = false;
                else
                {
                    Console.Error.WriteLine("invalid value for stdio");
                    return 2;
                }

                continue;
            }

            string name = equals < 0 ? arg : arg[..equals];
            string? value = equals < 0 ? i + 1 < args.Length ? args[++i] : null : arg[(equals + 1)..];
            if (name is "--clientProcessId" or "-clientProcessId")
            {
                if (value is null || !TryParseProcessID(value.AsSpan(), out clientProcessId))
                {
                    Console.Error.WriteLine("invalid clientProcessId");
                    return 2;
                }
            }
            else if (name is "--pipe" or "-pipe" or "--socket" or "-socket")
            {
                if (value is null)
                {
                    Console.Error.WriteLine("flag needs an argument: " + name);
                    return 2;
                }
            // Upstream admits these flags but only stdio actually launches a transport.
            }
            else
            {
                Console.Error.WriteLine("unsupported LSP flag: " + name);
                return 2;
            }
        }

        if (!stdio)
        {
            Console.Error.WriteLine("only stdio is supported");
            return 1;
        }

        using var cancel = new CancellationTokenSource();
        using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, signal =>
        {
            signal.Cancel = true;
            cancel.Cancel();
        });
        using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, signal =>
        {
            signal.Cancel = true;
            cancel.Cancel();
        });
        var cwd = TsPath.RootedDirectoryPathFromAbsolute(OsTextValue(Environment.CurrentDirectory));
        string? cache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (string.IsNullOrEmpty(cache))
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            cache = string.IsNullOrEmpty(home) ? Path.GetTempPath() : Path.Combine(home, ".cache");
        }

        var typings = TsPath.ToRootedDirectoryPath(OsTextValue(Path.Combine(cache, "typescript", Core.VersionMajorMinor())), cwd);
        var watchdogs = new List<Thread>();
        var watchdogMu = new object ();
        bool stopping = false;
        void WatchParent(long pid)
        {
            lock (watchdogMu)
                if (!stopping && StartParentProcessWatchdog(cancel, pid)is { } worker)
                    watchdogs.Add(worker);
        }

        Action<long>? watchParent;
        if (clientProcessId > 0)
        {
            WatchParent(clientProcessId);
            watchParent = null;
        }
        else
            watchParent = WatchParent;
        var server = Server.NewServer(new ServerOptions { In = LspTransport.ToReader(Console.OpenStandardInput()), Out = LspTransport.ToWriter(Console.OpenStandardOutput()), Err = Console.OpenStandardError(), Cwd = cwd, FS = TypeScript.Bundled.Bundled.WrapFS(OsVfs.FS()), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = typings, NpmInstall = NpmInstall, Spawn = SpawnProcess, ProgressDelay = TimeSpan.FromMilliseconds(250), SetParentProcessID = watchParent });
        Exception? error;
        try
        {
            error = server.Run(cancel.Token);
        }
        finally
        {
            lock (watchdogMu)
            {
                stopping = true;
                cancel.Cancel();
                foreach (var watchdog in watchdogs)
                    watchdog.Join();
            }
        }

        if (error is null)
            return 0;
        Console.Error.WriteLine(error.Message);
        return 1;
    }

    // OS/BCL strings are Unicode display strings; encode once at this ingress.
    private static ByteStringValue OsTextValue(string text) => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(System.Text.Encoding.UTF8.GetBytes(text)));
    private static ProcessStartInfo CreateProcess(ByteStringValue executable, ByteStringValue directory, Slice<ByteStringValue> args)
    {
        var info = new ProcessStartInfo(GoString.ToString(executable.Materialize()!))
        {
            WorkingDirectory = GoString.ToString(directory.Materialize()!),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in args)
            info.ArgumentList.Add(GoString.ToString(arg.Materialize()!));
        return info;
    }

    internal static (byte[]? Output, Exception? Error) NpmInstall(ProtocolRequestContext context, ByteStringValue directory, Slice<ByteStringValue> args)
    {
        if (context.Cancellation.IsCancellationRequested)
            return (null, new OperationCanceledException("context canceled", context.Cancellation));
        try
        {
            using var process = new Process
            {
                StartInfo = CreateProcess(ByteStringValue.FromString("npm"), directory, args)
            };
            process.Start();
            using var output = new MemoryStream();
            var stdout = process.StandardOutput.BaseStream.CopyToAsync(output);
            // exec.Cmd.Output retains stderr for the exit error and does not send it to the server wire.
            using var stderr = new PrefixSuffixSaver();
            var stderrRead = process.StandardError.BaseStream.CopyToAsync(stderr);
            Exception? cancellationError = null;
            using var registration = context.Cancellation.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        cancellationError = new OperationCanceledException("context canceled", context.Cancellation);
                    }
                }
                catch (InvalidOperationException)
                {
                // The BCL already-exited admission corresponds to os.ErrProcessDone.
                }
                catch (System.ComponentModel.Win32Exception error)
                {
                    // ESRCH likewise means the process completed; other native errors
                    // return through Wait, never escape CancellationTokenSource.Cancel.
                    if (error.NativeErrorCode != 3)
                        cancellationError = new IOException("exec: canceling Cmd: " + error.Message, error);
                }
            });
            process.WaitForExit();
            Exception? copyError = null;
            try
            {
                Task.WhenAll(stdout, stderrRead).GetAwaiter().GetResult();
            }
            catch (Exception pipeError)when (pipeError is IOException or ObjectDisposedException)
            {
                copyError = pipeError;
            }

            registration.Dispose(); // Wait consumes the completed cancellation result before choosing its error.
            // exec.Cmd.Wait prefers process failure, then watchCtx, then pipe-copy failure.
            Exception? error = process.ExitCode != 0 ? new NpmExitError(process.ExitCode, stderr.Bytes()) : cancellationError ?? copyError;
            return (output.ToArray(), error);
        }
        catch (Exception error)when (error is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (null, error);
        }
    }

    // Retains ExitError's stderr at the actual process-output/error owner.
    private sealed class NpmExitError(int code, Slice<byte> stderr) : IOException("exit status " + code.ToString(System.Globalization.CultureInfo.InvariantCulture))
    {
        internal readonly int ExitCode = code;
        internal readonly Slice<byte> Stderr = stderr;
    }

    private static (Stream? Process, Exception? Error) SpawnProcess(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr)
    {
        // The original command[0] admission is retained (empty command is a caller panic).
        var process = new Process
        {
            StartInfo = CreateProcess(command[0], directory, command.Slice2(1))
        };
        process.StartInfo.RedirectStandardInput = true;
        try
        {
            process.Start();
            return (new ChildProcess(process, stderr), null);
        }
        catch (Exception error)when (error is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            return (null, error);
        }
    }

    private sealed class ChildProcess : Stream, IProcessExitState
    {
        private readonly Process process;
        private readonly Task stderrCopy;
        private bool reaped;
        private int finalExitCode;
        internal ChildProcess(Process process, Stream stderr)
        {
            this.process = process;
            stderrCopy = process.StandardError.BaseStream.CopyToAsync(stderr);
        }

        public (int Code, bool Exited) ExitCode() => reaped ? (finalExitCode, true) : (0, false);
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => process.StandardOutput.BaseStream.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => process.StandardOutput.BaseStream.Read(buffer);
        public override void Write(byte[] buffer, int offset, int count) => process.StandardInput.BaseStream.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => process.StandardInput.BaseStream.Write(buffer);
        public override void Flush() => process.StandardInput.BaseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (!disposing || reaped)
                return;
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
            }

            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
            }

            process.WaitForExit();
            finalExitCode = process.ExitCode;
            reaped = true;
            // Go WaitDelay closes inherited pipe handles after one second; it does not wait forever for a descendant's stderr.
            try
            {
                stderrCopy.Wait(TimeSpan.FromSeconds(1));
            }
            finally
            {
                process.StandardError.Close();
                process.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private static Thread? StartParentProcessWatchdog(CancellationTokenSource cancel, long pid)
    {
        if (pid <= 0)
            return null;
        var token = cancel.Token;
        var worker = new Thread(() =>
        {
            while (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
                if (!IsProcessAlive(pid))
                {
                    Console.Error.WriteLine($"Parent process {pid} has exited, shutting down.");
                    cancel.Cancel();
                    return;
                }
        })
        {
            IsBackground = true
        };
        worker.Start();
        return worker;
    }

    private static bool IsProcessAlive(long pid) => Kill(unchecked((int)pid), 0) == 0 || Marshal.GetLastPInvokeError() == 1;
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int Kill(int pid, int signal);
}
