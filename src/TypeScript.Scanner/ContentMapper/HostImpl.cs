// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/contentmapper/hostimpl.go counterpart at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using LocaleValue = TypeScript.Locale.Locale;
using TypeScript.Ipc;
using System.Runtime.CompilerServices;
using TypeScript.SpanMap;
using NormalizedDirective = TypeScript.Scanner.MappedDiagnosticDirective;

namespace TypeScript.ContentMapper;
// hostimpl.go:43–218. Closed wire DTO owners keep raw Go-byte names/content.
internal sealed class InitializeParams
{
    internal ByteStringValue Locale;
    internal Slice<PositionEncoding> PositionEncodings;
}

internal sealed class InitializeResult
{
    internal PositionEncoding PositionEncoding;
    internal ByteStringValue DiagnosticSource;
}

internal sealed class OpenProjectParams
{
    internal ByteStringValue ConfigFileName;
    internal ByteStringValue ProjectHandle;
    internal RawJsonValue? Options;
    internal RawJsonValue? CompilerOptions;
}

internal sealed class OpenProjectResult
{
    internal ByteStringValue ConfigIdentity;
    internal Slice<ByteStringValue> WatchedFiles;
    internal Slice<OptionDiagnosticResult> OptionDiagnostics;
}

internal sealed class OptionDiagnosticResult
{
    internal Slice<RawJsonValue?> Path;
    internal ByteStringValue MessageText;
    internal int Code;
}

internal sealed class CloseProjectParams
{
    internal ByteStringValue ProjectHandle;
}

internal sealed class TransformParams
{
    internal ByteStringValue FileName;
    internal ByteStringValue Content;
    internal ByteStringValue ProjectHandle;
}

internal class MappedOutput
{
    internal ByteStringValue Text;
    internal ByteStringValue Extension;
    internal RawJsonValue? Mappings;
    internal DiagnosticDirectives? DiagnosticDirectives;
}

internal struct UnusedExpectDirectiveDiagnostic
{
    internal int Code;
    internal ByteStringValue MessageText;
}

internal sealed class DiagnosticDirectives
{
    internal Slice<UnusedExpectDirectiveDiagnostic> UnusedExpectDirectiveDiagnostics;
    internal Slice<WireMappedDiagnosticDirective> Directives;
}

internal struct WireMappedDiagnosticDirective
{
    internal long OriginalStart;
    internal long OriginalLength;
    internal long VirtualStart;
    internal long VirtualEnd;
    internal DiagnosticDirectivePolicy Policy;
    internal long? UnusedExpectDirectiveIndex;
}

internal sealed class SupplementalOutput : MappedOutput;
internal sealed class TransformResult : MappedOutput
{
    internal Slice<WireDiagnostic> Diagnostics;
    internal Slice<SupplementalOutput> Supplemental;
}

internal struct WireDiagnostic
{
    internal ByteStringValue MessageText;
    internal long Start, Length;
    internal int Code;
}

// hostimpl.go:349. The duplex read/write lifetime is distinct from stderr.
internal interface ISpawner
{
    (Stream? Duplex, Exception? Error) Spawn(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr);
}

internal interface IProcessExitState
{
    (int Code, bool Exited) ExitCode();
}

internal sealed class SpawnerFunc(Func<Slice<ByteStringValue>, ByteStringValue, Stream, (Stream? , Exception? )> spawn) : ISpawner
{
    public (Stream? Duplex, Exception? Error) Spawn(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr) => spawn(command, directory, stderr);
}

internal readonly record struct HostOptions(Action<ByteStringValue>? Logger);
// hostimpl.go:265/270. Independent atomic observations, matching source snapshot reads.
internal struct OperationTimingCollector
{
    private long count;
    private long duration;
    internal void Record(long start)
    {
        Interlocked.Increment(ref count);
        Interlocked.Add(ref duration, ElapsedNanoseconds(start));
    }

    internal OperationTiming Snapshot() => new()
    {
        Count = unchecked((ulong)Volatile.Read(ref count)),
        Duration = Volatile.Read(ref duration),
    };
    internal static long ElapsedNanoseconds(long start) => unchecked((long)((Int128)(Stopwatch.GetTimestamp() - start) * 1_000_000_000 / Stopwatch.Frequency));
}

internal sealed class TimingCollector
{
    internal readonly object gate = new();
    internal readonly Dictionary<string, MapperTimingCollector> mappers = new(StringComparer.Ordinal);
    internal ulong activeRequests;
    internal long requestWaitStart;
    internal long requestWaitElapsed;
    // hostimpl.go:291.
    internal MapperTimingCollector Mapper(string identity)
    {
        lock (gate)
        {
            if (mappers.TryGetValue(identity, out var existing))
                return existing;
            var timing = new MapperTimingCollector(this);
            mappers[identity] = timing;
            return timing;
        }
    }

    // hostimpl.go:302. Required detached map copy limits mutex scope, retaining worker pointers.
    internal Timings Snapshot()
    {
        long requestWait;
        Dictionary<string, MapperTimingCollector> snapshot;
        lock (gate)
        {
            requestWait = requestWaitElapsed;
            if (activeRequests != 0)
                requestWait = unchecked(requestWait + OperationTimingCollector.ElapsedNanoseconds(requestWaitStart));
            snapshot = new Dictionary<string, MapperTimingCollector>(mappers, StringComparer.Ordinal);
        }

        var result = new Timings
        {
            Mappers = new(snapshot.Count, StringComparer.Ordinal),
            RequestWait = requestWait
        };
        foreach (var(identity, timing)in snapshot)
            result.Mappers[identity] = new MapperTimings
            {
                Spawn = timing.spawn.Snapshot(),
                Initialize = timing.initialize.Snapshot(),
                OpenProject = timing.openProject.Snapshot(),
                CloseProject = timing.closeProject.Snapshot(),
                Transform = timing.transform.Snapshot(),
            };
        return result;
    }
}

internal sealed class MapperTimingCollector(TimingCollector owner)
{
    internal OperationTimingCollector spawn, initialize, openProject, closeProject, transform;
    // hostimpl.go:324.
    internal long StartRequest()
    {
        lock (owner.gate)
        {
            if (owner.activeRequests == 0)
                owner.requestWaitStart = Stopwatch.GetTimestamp();
            owner.activeRequests = unchecked(owner.activeRequests + 1);
        }

        return Stopwatch.GetTimestamp();
    }

    // hostimpl.go:334.
    internal void FinishRequest(ref OperationTimingCollector operation, long start)
    {
        operation.Record(start);
        lock (owner.gate)
        {
            owner.activeRequests = unchecked(owner.activeRequests - 1);
            if (owner.activeRequests == 0)
                owner.requestWaitElapsed = unchecked(owner.requestWaitElapsed + OperationTimingCollector.ElapsedNanoseconds(owner.requestWaitStart));
        }
    }
}

// hostimpl.go:419–450. Stream.Write owns the same required []byte→string copy;
// pending concatenation and retained suffixes remain raw Go-byte strings.
internal sealed class StderrLogger(ByteStringValue mapperName, Action<ByteStringValue> logger) : Stream
{
    private readonly object gate = new();
    private string pending = "";
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override void Write(ReadOnlySpan<byte> data)
    {
        lock (gate)
        {
            pending += GoString.FromBytes(data);
            while (true)
            {
                int index = pending.IndexOf('\n');
                if (index < 0)
                    break;
                Log(TrimCarriageReturn(pending[..index]));
                pending = pending[(index + 1)..];
            }
        }
    }

    // hostimpl.go:437. Called after closing the process, even when close fails.
    internal void FlushPending()
    {
        lock (gate)
        {
            if (pending.Length != 0)
            {
                Log(TrimCarriageReturn(pending));
                pending = "";
            }
        }
    }

    private static string TrimCarriageReturn(string value) => value.EndsWith('\r') ? value[..^1] : value;
    // hostimpl.go:447. This owned log boundary preserves arbitrary raw bytes.
    private void Log(string message) => logger(ByteStringValue.Concat(ByteStringValue.FromString("[content mapper: "), mapperName, ByteStringValue.FromString("] stderr: "), ByteStringValue.FromString(message)));
}

// Standard Stream adapter for the actual io.ReadWriteCloser owner. Close returns
// the captured I/O error; Dispose exposes it at the managed Stream boundary.
internal abstract class ProcessStream(Stream inner) : Stream, IProcessExitState
{
    protected readonly Stream Inner = inner;
    public override bool CanRead => Inner.CanRead;
    public override bool CanSeek => Inner.CanSeek;
    public override bool CanWrite => Inner.CanWrite;
    public override long Length => Inner.Length;
    public override long Position { get => Inner.Position; set => Inner.Position = value; }

    public override void Flush() => Inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => Inner.Read(buffer);
    public override void Write(byte[] buffer, int offset, int count) => Inner.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => Inner.Write(buffer);
    public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);
    public override void SetLength(long value) => Inner.SetLength(value);
    public virtual (int Code, bool Exited) ExitCode() => Inner is IProcessExitState state ? state.ExitCode() : (0, false);
    internal abstract Exception? CloseProcess();
    protected override void Dispose(bool disposing)
    {
        if (disposing && CloseProcess()is { } error)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        base.Dispose(disposing);
    }

    protected static Exception? CloseInner(Stream stream)
    {
        if (stream is ProcessStream process)
            return process.CloseProcess();
        try
        {
            stream.Dispose();
            return null;
        }
        catch (Exception error)when (error is IOException or ObjectDisposedException)
        {
            return error;
        }
    }
}

// hostimpl.go:451–460. Stderr is flushed after the underlying close regardless
// of its returned error; logger exceptions still propagate like an upstream panic.
internal sealed class LoggedProcess(Stream inner, StderrLogger stderr) : ProcessStream(inner)
{
    // Go embeds only io.ReadWriteCloser here; its optional ExitCode method is
    // not promoted through that interface when logging wraps the child.
    public override (int Code, bool Exited) ExitCode() => (0, false);
    internal override Exception? CloseProcess()
    {
        var error = CloseInner(Inner);
        stderr.FlushPending();
        return error;
    }
}

// hostimpl.go:462–484. The mutex preserves sync.Once completion visibility and
// retains the first close error; transferable process-state access is forwarded.
internal sealed class CloseOnceProcess(Stream inner) : ProcessStream(inner)
{
    private readonly object gate = new();
    private bool closed;
    private Exception? error;
    internal override Exception? CloseProcess()
    {
        lock (gate)
        {
            if (!closed)
            {
                // Once is done even if the original close panics.
                closed = true;
                error = CloseInner(Inner);
            }

            return error;
        }
    }
}

// hostimpl.go:1319–1360. Go int inputs remain signed 64-bit until the bounded
// source buffer admits them; core.TextPos retains its actual signed 32-bit role.
internal sealed class PositionNormalizer
{
    private readonly ByteStringValue text;
    private readonly PositionEncoding encoding;
    private readonly PositionMap? positionMap;
    private readonly int length;
    private PositionNormalizer(ByteStringValue text, PositionEncoding encoding, PositionMap? map, int length)
    {
        this.text = text;
        this.encoding = encoding;
        positionMap = map;
        this.length = length;
    }

    internal static (PositionNormalizer? Value, Exception? Error) New(ByteStringValue text, PositionEncoding encoding)
    {
        if (encoding == PositionEncoding.UTF8)
            return (new(text, encoding, null, text.Length), null);
        if (encoding == PositionEncoding.UTF16)
        {
            var map = text.IsBytes ? Ast.ComputePositionMapByteString(text.ByteView) : Ast.ComputePositionMapByteString(text.Materialize() ?? "");
            return (new(text, encoding, map, map.UTF8ToUTF16(text.Length)), null);
        }

        return (null, new InvalidOperationException("unsupported position encoding " + GoQuote.Quote(encoding.AsString())));
    }

    internal (int Position, Exception? Error) NormalizeTextPos(int position)
    {
        var(normalized, error) = Normalize(position);
        return (unchecked((int)normalized), error);
    }

    internal (long Position, Exception? Error) Normalize(long position)
    {
        if (position < 0)
            return (0, new InvalidOperationException("position " + position.ToString(System.Globalization.CultureInfo.InvariantCulture) + " is negative"));
        if (position > length)
            return (0, new InvalidOperationException("position " + position.ToString(System.Globalization.CultureInfo.InvariantCulture) + " exceeds " + encoding.AsString() + " length " + length.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        int bytePosition = encoding == PositionEncoding.UTF8 ? (int)position : positionMap!.UTF16ToUTF8((int)position);
        if (bytePosition < text.Length && (text[bytePosition] & 0xC0) == 0x80)
            return (0, new InvalidOperationException("position " + position.ToString(System.Globalization.CultureInfo.InvariantCulture) + " splits a Unicode code point"));
        return (bytePosition, null);
    }
}

// hostimpl.go:372–417. Logging observes the same complete typed message before
// send or after successful receive, without replacing the protocol transport.
internal sealed class LoggingProtocol(TypeScript.Ipc.IProtocol protocol, ByteStringValue mapperName, Action<ByteStringValue> logger) : TypeScript.Ipc.IProtocol
{
    private void Log<T>(ByteStringValue direction, ProtocolType<T> descriptor, T message)
    {
        ByteStringValue data;
        try
        {
            data = ProtocolCodec.Marshal(descriptor, message);
        }
        catch (System.Text.Json.JsonException error)
        {
            logger(ByteStringValue.Concat(ByteStringValue.FromString("[content mapper: "), mapperName, ByteStringValue.FromString("] "), direction, ByteStringValue.FromString(": <failed to serialize: "), ByteStringValue.FromString(GoString.ToUtf8(error.Message)), ByteStringValue.FromString(">")));
            return;
        }

        logger(ByteStringValue.Concat(ByteStringValue.FromString("[content mapper: "), mapperName, ByteStringValue.FromString("] "), direction, ByteStringValue.FromString(": "), data));
    }

    public (GenericMessage? Message, Exception? Error) ReadMessage()
    {
        var result = protocol.ReadMessage();
        if (result.Error is null)
            Log(ByteStringValue.FromString("receive"), GenericMessageCodecs.MessagePointer, result.Message);
        return result;
    }

    public Exception? WriteRequest(ID? id, ByteStringValue method, object? parameters)
    {
        Log(ByteStringValue.FromString("send"), GenericMessageCodecs.RequestValue, new GenericRequestMessage { ID = id, Method = method, Params = parameters });
        return protocol.WriteRequest(id, method, parameters);
    }

    public Exception? WriteNotification(ByteStringValue method, object? parameters)
    {
        Log(ByteStringValue.FromString("send"), GenericMessageCodecs.RequestValue, new GenericRequestMessage { Method = method, Params = parameters });
        return protocol.WriteNotification(method, parameters);
    }

    public Exception? WriteResponse(ID? id, object? result)
    {
        Log(ByteStringValue.FromString("send"), GenericMessageCodecs.ResponseValue, new GenericResponseMessage { ID = id, Result = result });
        return protocol.WriteResponse(id, result);
    }

    public Exception? WriteError(ID? id, ResponseError? error)
    {
        Log(ByteStringValue.FromString("send"), GenericMessageCodecs.ResponseValue, new GenericResponseMessage { ID = id, Error = error });
        return protocol.WriteError(id, error);
    }
}

// hostimpl.go:1364. Child-initiated requests violate this parent-driven protocol;
// notification errors are deliberately ignored by the actual asynchronous conn.
internal sealed class RejectHandler : TypeScript.Ipc.IHandler
{
    public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => (null, new InvalidOperationException("content mapper sent an unexpected request: " + GoString.ToString(method.Materialize() ?? "")));
    public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
}

internal delegate (IConn? Connection, ProcessStream? Closer, PositionEncoding Encoding, ByteStringValue Source, Exception? Error) DialFunc(CancellationToken context, Mapper mapper, LocaleValue locale);
internal sealed class ProjectEntry
{
    internal Mapper mapper = null!;
    internal ProjectSpec spec;
    internal ByteStringValue projectHandle;
    internal bool opened;
    internal ByteStringValue configIdentity;
    internal List<RootedFilePath>? watchedFiles;
    internal List<OptionDiagnostic>? optionDiagnostics;
}

internal sealed class MapperConnection
{
    internal IConn? conn;
    internal ProcessStream? closer;
    internal Exception? error;
    internal PositionEncoding positionEncoding;
    internal ByteStringValue diagnosticSource;
    internal long refs;
}

internal sealed partial class ProcessHost : IHost
{
    internal readonly CancellationTokenSource cancellation;
    private readonly CancellationToken contextToken;
    private bool cancellationDisposed;
    internal CancellationToken Context => contextToken;

    private CancellationTokenRegistration stop;
    internal readonly DialFunc dial;
    internal readonly TimingCollector timing;
    internal readonly ReaderWriterLockSlim lifecycle = new(LockRecursionPolicy.NoRecursion);
    internal LocaleValue diagnosticLocale;
    internal readonly object gate = new();
    internal Dictionary<string, MapperConnection>? conns = new(StringComparer.Ordinal);
    internal Dictionary<string, ProjectEntry>? projects = new(StringComparer.Ordinal);
    internal Dictionary<ProjectSpecIdentity, ProjectLease>? projectLeases = new();
    internal ulong nextProjectID;
    // hostimpl.go:546. AfterFunc callbacks run asynchronously and stop is
    // nonblocking; Unregister avoids waiting under the lifecycle write lock.
    internal ProcessHost(CancellationToken context, DialFunc dial, TimingCollector timing, LocaleValue locale)
    {
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(context);
        contextToken = cancellation.Token;
        this.dial = dial;
        this.timing = timing;
        diagnosticLocale = locale;
        stop = context.Register(static value => ThreadPool.UnsafeQueueUserWorkItem(static host =>
        {
            _ = host.Close();
        }, (ProcessHost)value!, preferLocal: false), this);
    }

    public Timings Timings() => timing.Snapshot();
    // hostimpl.go:567. Project handles survive locale changes, but processes and
    // the cached initialization result are reset before closers run outside gate.
    public void SetLocale(LocaleValue locale)
    {
        lifecycle.EnterWriteLock();
        try
        {
            if (diagnosticLocale.String() == locale.String())
                return;
            diagnosticLocale = locale;
            Slice<ProcessStream> closers = default;
            lock (gate)
            {
                if (conns is not null)
                    foreach (var entry in conns.Values)
                    {
                        if (entry.closer is not null)
                            closers = closers.Append(entry.closer);
                        entry.conn = null;
                        entry.closer = null;
                        entry.error = null;
                        entry.positionEncoding = default;
                        entry.diagnosticSource = default;
                    }

                if (projects is not null)
                    foreach (var entry in projects.Values)
                        entry.opened = false;
            }

            foreach (var closer in closers)
                _ = closer.CloseProcess();
        }
        finally
        {
            lifecycle.ExitWriteLock();
        }
    }

    // hostimpl.go:833. Failed startup is retained until release/locale reset.
    internal (IConn? Connection, PositionEncoding Encoding, ByteStringValue Source, Exception? Error) ConnectionForLocked(Mapper mapper)
    {
        if (conns is null)
            return (null, default, default, new InvalidOperationException("content mapper host is closed"));
        string identity = mapper.Identity();
        if (!conns.TryGetValue(identity, out var entry))
            conns[identity] = entry = new();
        if (entry.conn is not null || entry.error is not null)
            return (entry.conn, entry.positionEncoding, entry.diagnosticSource, entry.error);
        var(connection, closer, encoding, source, error) = dial(Context, mapper, diagnosticLocale);
        entry.conn = connection;
        entry.closer = closer;
        entry.error = error;
        entry.positionEncoding = encoding;
        entry.diagnosticSource = source;
        return (connection, encoding, source, error);
    }

    // hostimpl.go:824.
    internal (IConn? Connection, PositionEncoding Encoding, ByteStringValue Source, Exception? Error) ConnectionFor(Mapper mapper)
    {
        lock (gate)
            return ConnectionForLocked(mapper);
    }

    // hostimpl.go:742. One required capacity-sized identity owner plus a seen map;
    // release is once-only and retains the real host until its transferable call.
    public Action Acquire(List<Mapper>? mappers)
    {
        var seen = new HashSet<string>(mappers?.Count ?? 0, StringComparer.Ordinal);
        var identities = Slice<string>.Make(0, mappers?.Count ?? 0);
        lock (gate)
        {
            if (conns is not null && mappers is not null)
                foreach (var mapper in mappers)
                {
                    string identity = mapper.Identity();
                    if (!seen.Add(identity))
                        continue;
                    identities = identities.Append(identity);
                    if (!conns.TryGetValue(identity, out var entry))
                        conns[identity] = entry = new();
                    entry.refs = unchecked(entry.refs + 1);
                }
        }

        object releaseGate = new();
        bool released = false;
        System.Runtime.ExceptionServices.ExceptionDispatchInfo? panic = null;
        return () =>
        {
            lock (releaseGate)
            {
                if (!released)
                {
                    released = true;
                    try
                    {
                        Release(identities);
                    }
                    catch (Exception error)
                    {
                        panic = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error);
                        throw;
                    }
                }

                // sync.OnceFunc rethrows the same panic on every later call.
                panic?.Throw();
            }
        };
    }

    // hostimpl.go:1067. Removing the last reference closes outside the map mutex.
    internal void Release(Slice<string> identities)
    {
        Slice<ProcessStream> closers = default;
        lock (gate)
        {
            if (conns is not null)
                foreach (var identity in identities)
                {
                    if (!conns.TryGetValue(identity, out var entry))
                        continue;
                    entry.refs = unchecked(entry.refs - 1);
                    if (entry.refs == 0)
                    {
                        conns.Remove(identity);
                        if (entry.closer is not null)
                            closers = closers.Append(entry.closer);
                    }
                }
        }

        foreach (var closer in closers)
            _ = closer.CloseProcess();
    }

    // hostimpl.go:795. Context cancellation and map detachment precede physical
    // child close; all close errors retain their order through the joined result.
    public Exception? Close()
    {
        lifecycle.EnterWriteLock();
        try
        {
            stop.Unregister();
            if (!cancellationDisposed)
            {
                try
                {
                    cancellation.Cancel();
                }
                finally
                {
                    cancellation.Dispose();
                    cancellationDisposed = true;
                }
            }

            Slice<ProcessStream> closers = default;
            lock (gate)
            {
                if (conns is not null)
                    foreach (var entry in conns.Values)
                        if (entry.closer is not null)
                            closers = closers.Append(entry.closer);
                conns = null;
                projects = null;
                projectLeases = null;
            }

            Exception? error = null;
            foreach (var closer in closers)
                error = Join(error, closer.CloseProcess());
            return error;
        }
        finally
        {
            lifecycle.ExitWriteLock();
        }
    }

    internal static Exception? Join(Exception? first, Exception? second) => first is null ? second : second is null ? first : new HostJoinedError(first, second);
    internal sealed class HostJoinedError(Exception first, Exception second) : Exception(first.Message + "\n" + second.Message, first)
    {
        internal readonly Exception Second = second;
    }
}

internal static class ProcessHosts
{
    internal static readonly ByteStringValue MethodInitialize = ByteStringValue.FromString("initialize");
    internal static readonly ByteStringValue MethodOpenProject = ByteStringValue.FromString("openProject");
    internal static readonly ByteStringValue MethodCloseProject = ByteStringValue.FromString("closeProject");
    internal static readonly ByteStringValue MethodTransform = ByteStringValue.FromString("transform");
    // hostimpl.go:486/492. The production dial uses the injected real spawner,
    // actual duplex framing and linked lifetime, never a successful null host.
    internal static IHost NewHost(CancellationToken context, ISpawner spawner, LocaleValue locale) => NewHostWithOptions(context, spawner, locale, default);
    internal static ProcessHost NewHostWithOptions(CancellationToken context, ISpawner spawner, LocaleValue locale, HostOptions options)
    {
        var timing = new TimingCollector();
        return new ProcessHost(context, (token, mapper, diagnosticLocale) =>
        {
            if ((mapper.Exec?.Count ?? 0) == 0)
                return (null, null, default, default, new InvalidOperationException("content mapper " + GoQuote.Quote(mapper.Package) + " declares no command to run"));
            var collector = timing.Mapper(mapper.Identity());
            string diagnosticName = mapper.DiagnosticName();
            long spawnStart = System.Diagnostics.Stopwatch.GetTimestamp();
            StderrLogger? stderr = options.Logger is null ? null : new(ByteStringValue.FromString(diagnosticName), options.Logger);
            // Existing metadata stores Exec in a managed List. This exact-size
            // header projection is a required existing-API adaptation, not an
            // upstream clone of command byte buffers.
            var command = Slice<ByteStringValue>.Make(mapper.Exec!.Count);
            for (int i = 0; i < command.Count; i++)
                command[i] = ByteStringValue.FromString(mapper.Exec[i]);
            var(stream, spawnError) = spawner.Spawn(command, mapper.PackageDirectory.AsValue(), stderr ?? (Stream)Stream.Null);
            collector.spawn.Record(spawnStart);
            if (spawnError is not null)
                return (null, null, default, default, new InitializeError { Kind = InitializeErrorKind.ProcessStart, MapperName = diagnosticName, Command = mapper.Exec[0], Detail = spawnError.Message });
            if (stderr is not null)
                stream = new LoggedProcess(stream!, stderr);
            var process = new CloseOnceProcess(stream!);
            IProtocol protocol = JSONRPCProtocol.NewJSONRPCProtocol(process);
            if (options.Logger is not null)
                protocol = new LoggingProtocol(protocol, ByteStringValue.FromString(diagnosticName), options.Logger);
            var connection = AsyncConn.NewAsyncConnWithProtocol(process, protocol, new RejectHandler());
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                _ = state.Connection.Run(state.Token);
                _ = state.Process.CloseProcess();
            }, (Connection: connection, Process: process, Token: token), preferLocal: false);
            using var initialize = CancellationTokenSource.CreateLinkedTokenSource(token);
            initialize.CancelAfter(TimeSpan.FromSeconds(5));
            long initializeStart = collector.StartRequest();
            var(encoding, source, error) = Handshake(initialize.Token, connection, diagnosticLocale);
            collector.FinishRequest(ref collector.initialize, initializeStart);
            bool initializeCanceled = initialize.IsCancellationRequested;
            initialize.Cancel();
            if (error is not null)
            {
                // Exit state must be observed before close mutates the process.
                var(code, exited) = process.ExitCode();
                _ = process.CloseProcess();
                if (FindInitializeError(error)is { } initializeError)
                {
                    initializeError.MapperName = diagnosticName;
                    return (null, null, default, default, initializeError);
                }

                if (exited)
                    return (null, null, default, default, new InitializeError { Kind = InitializeErrorKind.ProcessExit, MapperName = diagnosticName, ExitCode = code });
                if (initializeCanceled || IsCancellation(error))
                    return (null, null, default, default, new InitializeError { Kind = InitializeErrorKind.NoResponse, MapperName = diagnosticName, TimeoutSeconds = 5 });
                return (null, null, default, default, new InitializeError { Kind = InitializeErrorKind.Request, MapperName = diagnosticName, Detail = error.Message });
            }

            return (connection, process, encoding, source, null);
        }, timing, locale);
    }

    internal static InitializeError? FindInitializeError(Exception error)
    {
        if (error is InitializeError initialize)
            return initialize;
        if (error.InnerException is { } inner && FindInitializeError(inner)is { } found)
            return found;
        if (error is AsyncConn.JoinedError joined)
            return FindInitializeError(joined.Second);
        if (error is ProcessHost.HostJoinedError hostJoined)
            return FindInitializeError(hostJoined.Second);
        return null;
    }

    private static bool IsCancellation(Exception error) => error is OperationCanceledException || error.InnerException is { } inner && IsCancellation(inner) || error is AsyncConn.JoinedError joined && IsCancellation(joined.Second) || error is ProcessHost.HostJoinedError hostJoined && IsCancellation(hostJoined.Second);
    // hostimpl.go:1093. Actual value-root payload/decode; null response yields the
    // source zero struct and then the original encoding validation error.
    internal static (PositionEncoding Encoding, ByteStringValue Source, Exception? Error) Handshake(CancellationToken context, IConn connection, LocaleValue locale)
    {
        var(raw, error) = connection.Call(context, MethodInitialize, new TypeScript.Lsp.Protocol.ProtocolPayload<InitializeParams>(ContentMapperCodecs.InitializeParamsValue, new() { Locale = ByteStringValue.FromString(locale.String()), PositionEncodings = Slice<PositionEncoding>.FromArray([PositionEncoding.UTF8, PositionEncoding.UTF16]) }));
        if (error is not null)
            return (default, default, error);
        InitializeResult result = new();
        try
        {
            TypeScript.Lsp.Protocol.ProtocolCodec.Unmarshal(ContentMapperCodecs.InitializeResultValue, raw?.TextValue ?? default, ref result);
        }
        catch (System.Text.Json.JsonException decodeError)
        {
            return (default, default, new InitializeError { Kind = InitializeErrorKind.InvalidResponse, Detail = decodeError.Message });
        }

        if (result.PositionEncoding != PositionEncoding.UTF8 && result.PositionEncoding != PositionEncoding.UTF16)
            return (default, default, new InitializeError { Kind = InitializeErrorKind.PositionEncoding, PositionEncoding = result.PositionEncoding });
        bool allSpace = true;
        var text = result.DiagnosticSource;
        for (int i = 0; i < text.Length;)
        {
            var(rune, size) = text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[i..]) : Utf8.DecodeRuneInString(text.CharSpan[i..]);
            if (!GoUnicode.IsSpace(rune))
            {
                allSpace = false;
                break;
            }

            i += size;
        }

        if (allSpace)
            return (default, default, new InitializeError { Kind = InitializeErrorKind.EmptyDiagnosticSource });
        bool reserved = UnicodeCase.EqualFold(text, ByteStringValue.FromString("typescript")) || UnicodeCase.EqualFold(text, ByteStringValue.FromString("tsc"));
        if (!reserved)
            foreach (var extensions in TsPath.AllSupportedExtensionsWithJson)
                foreach (var extension in extensions)
                {
                    var value = ByteStringValue.FromString(extension);
                    if (value.StartsWith("."u8))
                        value = value.Slice(1);
                    if (UnicodeCase.EqualFold(text, value))
                    {
                        reserved = true;
                        break;
                    }
                }

        if (reserved)
            return (default, default, new InitializeError { Kind = InitializeErrorKind.ReservedDiagnosticSource, DiagnosticSource = text.Materialize() ?? "" });
        return (result.PositionEncoding, result.DiagnosticSource, null);
    }
}

internal sealed partial class ProcessHost
{
    // hostimpl.go:640. Open admission records watched/options outputs in the same
    // order as the source and marks opened only after every field is admitted.
    internal Exception? OpenProjectLocked(CancellationToken context, ProjectEntry entry)
    {
        if (entry.opened)
            return null;
        var(connection, _, source, error) = ConnectionForLocked(entry.mapper);
        if (error is not null)
            return error;
        var(compilerOptions, marshalError) = TypeScript.Json.JsonCodec.TryMarshal(entry.spec.CompilerOptions);
        if (marshalError is not null)
            return marshalError;
        var collector = timing.Mapper(entry.mapper.Identity());
        long start = collector.StartRequest();
        var(raw, requestError) = connection!.Call(context, ProcessHosts.MethodOpenProject, new TypeScript.Lsp.Protocol.ProtocolPayload<OpenProjectParams>(ContentMapperCodecs.OpenProjectParamsValue, new() { ConfigFileName = entry.spec.ConfigFileName.AsValue(), ProjectHandle = entry.projectHandle, Options = entry.mapper.Options?.Bytes is { } optionBytes ? TypeScript.JsonRpc.RawJsonValue.FromRetainedBytes(optionBytes) : null, CompilerOptions = TypeScript.JsonRpc.RawJsonValue.FromRetainedBytes(compilerOptions!) }));
        collector.FinishRequest(ref collector.openProject, start);
        if (requestError is not null)
            return requestError;
        OpenProjectResult result = new();
        try
        {
            TypeScript.Lsp.Protocol.ProtocolCodec.Unmarshal(ContentMapperCodecs.OpenProjectResultValue, raw?.TextValue ?? default, ref result);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ProjectError
            {
                Kind = ProjectErrorKind.MalformedResponse
            };
        }

        if (entry.mapper.DynamicConfig && result.ConfigIdentity.Length == 0)
            return new ProjectError
            {
                Kind = ProjectErrorKind.MissingConfigIdentity
            };
        if (!entry.mapper.DynamicConfig && result.ConfigIdentity.Length != 0)
            return new ProjectError
            {
                Kind = ProjectErrorKind.UnexpectedConfigIdentity
            };
        if (!entry.mapper.DynamicConfig && result.WatchedFiles.Count != 0)
            return new ProjectError
            {
                Kind = ProjectErrorKind.UnexpectedWatchedFiles
            };
        entry.configIdentity = result.ConfigIdentity;
        entry.watchedFiles = new(result.WatchedFiles.Count);
        foreach (var fileName in result.WatchedFiles)
        {
            var(path, rooted) = TsPath.TryRootedFilePathFromAbsolute(fileName);
            if (!rooted)
                return new ProjectError
                {
                    Kind = ProjectErrorKind.NonAbsoluteWatchedFile
                };
            entry.watchedFiles.Add(path);
        }

        entry.optionDiagnostics = new(result.OptionDiagnostics.Count);
        foreach (var diagnostic in result.OptionDiagnostics)
        {
            var path = new List<OptionPathSegment>(diagnostic.Path.Count);
            foreach (var segment in diagnostic.Path)
            {
                switch (segment!.Kind)
                {
                    case (byte)'"':
                        var propertyDecoder = new TypeScript.Lsp.Protocol.ProtocolDecoder(segment.TextValue);
                        ByteStringValue property;
                        try
                        {
                            property = propertyDecoder.ReadString();
                            propertyDecoder.Finish();
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            return new ProjectError
                            {
                                Kind = ProjectErrorKind.MalformedResponse
                            };
                        }

                        path.Add(new(property.Materialize() ?? "", 0, false));
                        break;
                    case (byte)'0':
                        long index;
                        try
                        {
                            index = ContentMapperCodecs.DecodeInt(segment.TextValue);
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            return new ProjectError
                            {
                                Kind = ProjectErrorKind.MalformedResponse
                            };
                        }

                        if (index < 0)
                            return new ProjectError
                            {
                                Kind = ProjectErrorKind.MalformedResponse
                            };
                        // Actual source OptionPathSegment.Index is Go int; the
                        // promotion packet includes its focused signed64 join.
                        path.Add(new("", index, true));
                        break;
                    default:
                        return new ProjectError
                        {
                            Kind = ProjectErrorKind.MalformedResponse
                        };
                }
            }

            entry.optionDiagnostics.Add(new() { Mapper = entry.mapper, Path = path, Source = source.Materialize() ?? "", Code = diagnostic.Code, MessageText = diagnostic.MessageText.Materialize() ?? "" });
        }

        entry.opened = true;
        return null;
    }

    // hostimpl.go:734.
    internal Exception? CloseProject(Mapper mapper, IConn connection, ByteStringValue handle)
    {
        var collector = timing.Mapper(mapper.Identity());
        long start = collector.StartRequest();
        var(_, error) = connection.Call(Context, ProcessHosts.MethodCloseProject, new TypeScript.Lsp.Protocol.ProtocolPayload<CloseProjectParams>(ContentMapperCodecs.CloseProjectParamsValue, new() { ProjectHandle = handle }));
        collector.FinishRequest(ref collector.closeProject, start);
        return error;
    }

    // hostimpl.go:763. The temporary project uses a fresh options pointer, and its
    // deferred close always executes even when Transform returns an error.
    public (Result Result, Exception? Error) Transform(Mapper mapper, Request request)
    {
        var project = Project(new() { Mappers = [mapper], CompilerOptions = new() });
        try
        {
            return project.Transform(mapper, request);
        }
        finally
        {
            _ = project.Close();
        }
    }

    // hostimpl.go:772. Decoder/initialization/request failures retain source kinds.
    internal (Result Result, Exception? Error) TransformLocked(Mapper mapper, Request request, ByteStringValue handle)
    {
        if (handle.Length == 0)
            return (default, new InvalidOperationException("content mapper project handle is required"));
        var(connection, encoding, source, initializeError) = ConnectionFor(mapper);
        if (initializeError is not null)
            return (default, TransformError.NewTransformError(TransformErrorKind.Initialize, initializeError));
        var collector = timing.Mapper(mapper.Identity());
        long start = collector.StartRequest();
        var(raw, requestError) = connection!.Call(Context, ProcessHosts.MethodTransform, new TypeScript.Lsp.Protocol.ProtocolPayload<TransformParams>(ContentMapperCodecs.TransformParamsValue, new() { FileName = request.FileName.AsValue(), Content = ByteStringValue.FromString(request.Content), ProjectHandle = handle }));
        collector.FinishRequest(ref collector.transform, start);
        if (requestError is not null)
            return (default, TransformError.NewTransformError(TransformErrorKind.Request, requestError));
        var(result, error) = HostDecode.DecodeTransformResult(raw, ByteStringValue.FromString(request.Content), encoding, source);
        return error is null ? (result, null) : (default, TransformError.NewTransformError(TransformErrorKind.Response, error));
    }

    // hostimpl.go:623. Raw binary transform hash bytes precede a fresh xxh3 hash;
    // this is not textual hash concatenation and options/config bytes are borrowed.
    internal static string CombinedIdentity(Mapper mapper, ByteStringValue configIdentity, CompilerOptions? options)
    {
        var identity = ByteStringValue.FromString(mapper.Identity());
        var optionBytes = mapper.Options?.Bytes;
        Span<byte> transformHash = stackalloc byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt128BigEndian(transformHash, mapper.TransformIdentity(options));
        var buffer = new GoBuilder();
        buffer.Grow(checked(identity.Length + (optionBytes?.Length ?? 0) + configIdentity.Length + transformHash.Length + 3));
        buffer.WriteString(identity);
        buffer.WriteByte(0);
        if (optionBytes is not null)
            buffer.WriteString(optionBytes);
        buffer.WriteByte(0);
        buffer.WriteString(configIdentity);
        buffer.WriteByte(0);
        buffer.WriteString(transformHash);
        var hash = System.IO.Hashing.XxHash128.HashToUInt128(buffer.Value().ByteSpan);
        return mapper.Identity() + ":" + hash.ToString("x32", System.Globalization.CultureInfo.InvariantCulture);
    }
}

// The source's string key stores ordered pointer identities. A closed CLR value
// key stores the same ordered reference identities, without exposing fabricated
// addresses. The detached ordered array is the source owned key's replacement.
internal readonly struct ProjectSpecIdentity : IEquatable<ProjectSpecIdentity>
{
    private readonly RootedFilePath fileName;
    private readonly CompilerOptions? options;
    private readonly Mapper[] mappers;
    internal ProjectSpecIdentity(ProjectSpec spec)
    {
        fileName = spec.ConfigFileName;
        options = spec.CompilerOptions;
        mappers = spec.Mappers?.ToArray() ?? Array.Empty<Mapper>();
    }

    public bool Equals(ProjectSpecIdentity other)
    {
        if (fileName != other.fileName || !ReferenceEquals(options, other.options) || mappers.Length != other.mappers.Length)
            return false;
        for (int i = 0; i < mappers.Length; i++)
            if (!ReferenceEquals(mappers[i], other.mappers[i]))
                return false;
        return true;
    }

    public override bool Equals(object? other) => other is ProjectSpecIdentity identity && Equals(identity);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(fileName);
        hash.Add(options is null ? 0 : RuntimeHelpers.GetHashCode(options));
        foreach (var mapper in mappers)
            hash.Add(RuntimeHelpers.GetHashCode(mapper));
        return hash.ToHashCode();
    }
}

internal sealed partial class ProcessHost
{
    // hostimpl.go:595. Lifecycle reader lock precedes the host map mutex; each
    // lease retains exact mapper pointer identity independently of Mapper.Equals.
    public IProject Project(ProjectSpec spec)
    {
        lifecycle.EnterReadLock();
        try
        {
            var key = new ProjectSpecIdentity(spec);
            lock (gate)
            {
                if (projects is null)
                    return null!;
                if (projectLeases!.TryGetValue(key, out var existing))
                    return existing.RetainLocked();
                var lease = new ProjectLease(this, key, spec.Mappers is null ? null : new List<Mapper>(spec.Mappers));
                lease.refs = 1;
                if (spec.Mappers is not null)
                    foreach (var mapper in spec.Mappers)
                    {
                        string entryKey = mapper.Identity() + ":" + nextProjectID.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        nextProjectID = unchecked(nextProjectID + 1);
                        projects[entryKey] = new ProjectEntry
                        {
                            mapper = mapper,
                            spec = spec,
                            projectHandle = ByteStringValue.FromString(entryKey)
                        };
                        string identity = mapper.Identity();
                        if (!conns!.TryGetValue(identity, out var connection))
                            conns[identity] = connection = new();
                        connection.refs = unchecked(connection.refs + 1);
                        lease.entries[mapper] = entryKey;
                    }

                projectLeases[key] = lease;
                return lease;
            }
        }
        finally
        {
            lifecycle.ExitReadLock();
        }
    }
}

internal sealed partial class ProjectLease : IProject
{
    internal readonly ProcessHost host;
    internal readonly ProjectSpecIdentity key;
    internal readonly List<Mapper>? mappers;
    internal readonly Dictionary<Mapper, string> entries;
    internal long refs;
    private readonly object closeGate = new();
    private bool closed;
    internal ProjectLease(ProcessHost host, ProjectSpecIdentity key, List<Mapper>? mappers)
    {
        this.host = host;
        this.key = key;
        this.mappers = mappers;
        entries = new(mappers?.Count ?? 0, ReferenceEqualityComparer.Instance);
    }

    // hostimpl.go:867. Retained aliases have independent once-close state.
    internal IProject RetainLocked()
    {
        refs = unchecked(refs + 1);
        return new RetainedProject(this);
    }

    // hostimpl.go:1024. Original lease and each retained alias consume one ref.
    public Exception? Close()
    {
        lock (closeGate)
        {
            if (closed)
                return null;
            closed = true;
            return Release();
        }
    }

    internal Exception? Release()
    {
        Exception? result = null;
        host.lifecycle.EnterReadLock();
        try
        {
            Slice<string> released = default;
            lock (host.gate)
            {
                refs = unchecked(refs - 1);
                if (refs < 0)
                    throw new InvalidOperationException("content mapper project reference count below zero");
                if (refs != 0)
                    return null;
                if (host.projectLeases?.GetValueOrDefault(key) == this)
                    host.projectLeases.Remove(key);
                foreach (var entryKey in entries.Values)
                {
                    var entry = host.projects?.GetValueOrDefault(entryKey);
                    if (entry is null)
                        continue;
                    if (entry.opened)
                    {
                        var connection = host.conns?.GetValueOrDefault(entry.mapper.Identity());
                        if (connection?.conn is not null)
                            result = ProcessHost.Join(result, host.CloseProject(entry.mapper, connection.conn, entry.projectHandle));
                    }

                    host.projects!.Remove(entryKey);
                    released = released.Append(entry.mapper.Identity());
                }
            }

            host.Release(released);
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }

        return result;
    }
}

internal sealed partial class ProjectLease
{
    // hostimpl.go:878. Opened handles are reset even when closeProject returns an error.
    public Exception? Refresh()
    {
        host.lifecycle.EnterReadLock();
        try
        {
            lock (host.gate)
            {
                if (host.projects is null)
                    return null;
                Exception? result = null;
                foreach (var key in entries.Values)
                {
                    var entry = host.projects.GetValueOrDefault(key);
                    if (entry is null || !entry.opened)
                        continue;
                    var connection = host.conns?.GetValueOrDefault(entry.mapper.Identity());
                    if (connection?.conn is not null)
                        result = ProcessHost.Join(result, host.CloseProject(entry.mapper, connection.conn, entry.projectHandle));
                    entry.opened = false;
                }

                return result;
            }
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }
    }

    // hostimpl.go:901. Ordered mapper clone, not entry map order, defines identities.
    public (List<string>? Identities, Exception? Error) Identities()
    {
        host.lifecycle.EnterReadLock();
        try
        {
            lock (host.gate)
            {
                if (host.projects is null)
                    return (null, null);
                var result = new List<string>(entries.Count);
                if (mappers is not null)
                    foreach (var mapper in mappers)
                    {
                        var entry = host.projects.GetValueOrDefault(entries[mapper]);
                        if (entry is null)
                            continue;
                        if (mapper.DynamicConfig)
                        {
                            var error = host.OpenProjectLocked(host.Context, entry);
                            if (error is not null)
                                return (null, error);
                            result.Add(ProcessHost.CombinedIdentity(mapper, entry.configIdentity, entry.spec.CompilerOptions));
                        }
                        else
                            result.Add(mapper.Identity() + ":" + mapper.TransformIdentityHex(entry.spec.CompilerOptions));
                    }

                return (result, null);
            }
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }
    }

    // hostimpl.go:927. Unknown pointer identity is an empty successful result.
    public (string Identity, Exception? Error) Identity(Mapper mapper)
    {
        host.lifecycle.EnterReadLock();
        try
        {
            lock (host.gate)
            {
                if (host.projects is null || !entries.TryGetValue(mapper, out var key))
                    return ("", null);
                var entry = host.projects.GetValueOrDefault(key);
                if (entry is null)
                    return ("", null);
                if (mapper.DynamicConfig)
                {
                    var error = host.OpenProjectLocked(host.Context, entry);
                    if (error is not null)
                        return ("", error);
                    return (ProcessHost.CombinedIdentity(mapper, entry.configIdentity, entry.spec.CompilerOptions), null);
                }

                return (mapper.Identity() + ":" + mapper.TransformIdentityHex(entry.spec.CompilerOptions), null);
            }
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }
    }

    // hostimpl.go:955. Append remains lazy, followed by ordered in-place compact.
    public (List<RootedFilePath>? Files, Exception? Error) WatchedFiles()
    {
        host.lifecycle.EnterReadLock();
        try
        {
            lock (host.gate)
            {
                if (host.projects is null)
                    return (null, null);
                List<RootedFilePath>? files = null;
                foreach (var key in entries.Values)
                {
                    var entry = host.projects.GetValueOrDefault(key);
                    if (entry is null)
                        continue;
                    if (entry.mapper.DynamicConfig)
                    {
                        var error = host.OpenProjectLocked(host.Context, entry);
                        if (error is not null)
                            return (null, error);
                    }

                    if (entry.watchedFiles is { Count: > 0 })
                        (files ??= new()).AddRange(entry.watchedFiles);
                }

                if (files is not null)
                {
                    files.Sort(static (a, b) => a.Compare(b));
                    int count = 0;
                    for (int i = 0; i < files.Count; i++)
                        if (count == 0 || files[count - 1] != files[i])
                            files[count++] = files[i];
                    files.RemoveRange(count, files.Count - count);
                }

                return (files, null);
            }
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }
    }

    // hostimpl.go:980. Only already-opened entries contribute option diagnostics.
    public List<OptionDiagnostic>? Diagnostics()
    {
        host.lifecycle.EnterReadLock();
        try
        {
            lock (host.gate)
            {
                if (host.projects is null)
                    return null;
                List<OptionDiagnostic>? result = null;
                if (mappers is not null)
                    foreach (var mapper in mappers)
                    {
                        var entry = host.projects.GetValueOrDefault(entries[mapper]);
                        if (entry is null || !entry.opened)
                            continue;
                        if (entry.optionDiagnostics is { Count: > 0 })
                            (result ??= new()).AddRange(entry.optionDiagnostics);
                    }

                return result;
            }
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }
    }

    // hostimpl.go:1002. The map lock protects open/handle admission, but is
    // released before the transform request; the lifecycle lease spans both.
    public (Result Result, Exception? Error) Transform(Mapper mapper, Request request)
    {
        host.lifecycle.EnterReadLock();
        try
        {
            ByteStringValue handle;
            lock (host.gate)
            {
                string key = entries.GetValueOrDefault(mapper) ?? "";
                var entry = host.projects?.GetValueOrDefault(key);
                if (entry is null)
                    return (default, new InvalidOperationException("content mapper project is closed"));
                var error = host.OpenProjectLocked(host.Context, entry);
                if (error is not null)
                    return (default, TransformError.NewTransformError(ProcessHosts.FindInitializeError(error)is not null ? TransformErrorKind.Initialize : TransformErrorKind.Project, error));
                handle = entry.projectHandle;
            }

            return host.TransformLocked(mapper, request, handle);
        }
        finally
        {
            host.lifecycle.ExitReadLock();
        }
    }
}

internal sealed class RetainedProject(ProjectLease lease) : IProject
{
    private readonly object closeGate = new();
    private bool closed;
    public Exception? Close()
    {
        lock (closeGate)
        {
            if (closed)
                return null;
            closed = true;
            return lease.Release();
        }
    }

    public Exception? Refresh() => lease.Refresh();
    public (List<string>? Identities, Exception? Error) Identities() => lease.Identities();
    public (string Identity, Exception? Error) Identity(Mapper mapper) => lease.Identity(mapper);
    public (List<RootedFilePath>? Files, Exception? Error) WatchedFiles() => lease.WatchedFiles();
    public List<OptionDiagnostic>? Diagnostics() => lease.Diagnostics();
    public (Result Result, Exception? Error) Transform(Mapper mapper, Request request) => lease.Transform(mapper, request);
}

internal static class HostDecode
{
    // hostimpl.go:1208. Required exact-length normalized result and independent
    // sortable indexed copy preserve original error index and caller order.
    internal static (List<NormalizedDirective>? Value, Exception? Error) NormalizeDiagnosticDirectives(DiagnosticDirectives? directives, PositionNormalizer virtualPositions, PositionNormalizer originalPositions, ByteStringValue diagnosticSource)
    {
        if (directives is null)
            return (null, null);
        var result = new List<NormalizedDirective>(directives.Directives.Count);
        for (int i = 0; i < directives.Directives.Count; i++)
        {
            var directive = directives.Directives[i];
            var normalized = new NormalizedDirective
            {
                Source = diagnosticSource.Materialize() ?? ""
            };
            switch (directive.Policy)
            {
                case DiagnosticDirectivePolicy.Ignore:
                    normalized.Policy = MappedDiagnosticDirectivePolicy.Ignore;
                    break;
                case DiagnosticDirectivePolicy.Expect:
                    long index = directive.UnusedExpectDirectiveIndex ?? 0;
                    if (directive.UnusedExpectDirectiveIndex is null && directives.UnusedExpectDirectiveDiagnostics.Count != 1)
                        return (null, Error(DiagnosticDirectiveErrorKind.ExpectMissingUnusedDiagnostic, i));
                    if (index < 0 || index >= directives.UnusedExpectDirectiveDiagnostics.Count)
                        return (null, Error(DiagnosticDirectiveErrorKind.InvalidUnusedDiagnosticIndex, i));
                    var unused = directives.UnusedExpectDirectiveDiagnostics[(int)index];
                    normalized.Policy = MappedDiagnosticDirectivePolicy.Expect;
                    normalized.UnusedCode = unused.Code;
                    normalized.UnusedMessageText = unused.MessageText.Materialize() ?? "";
                    break;
                default:
                    var invalidPolicy = Error(DiagnosticDirectiveErrorKind.InvalidPolicy, i);
                    invalidPolicy.Policy = directive.Policy;
                    return (null, invalidPolicy);
            }

            if (directive.VirtualStart < 0 || directive.VirtualEnd < directive.VirtualStart)
                return (null, Error(DiagnosticDirectiveErrorKind.InvalidRange, i));
            var(virtualStart, startError) = virtualPositions.Normalize(directive.VirtualStart);
            if (startError is not null)
                return (null, Error(DiagnosticDirectiveErrorKind.InvalidRange, i));
            var(virtualEnd, endError) = virtualPositions.Normalize(directive.VirtualEnd);
            if (endError is not null)
                return (null, Error(DiagnosticDirectiveErrorKind.InvalidRange, i));
            normalized.VirtualRange = new((int)virtualStart, (int)virtualEnd);
            bool validOriginal = directive.OriginalStart >= 0 && directive.OriginalLength >= 0 && directive.OriginalStart <= long.MaxValue - directive.OriginalLength;
            if (validOriginal)
            {
                var(originalStart, originalStartError) = originalPositions.Normalize(directive.OriginalStart);
                var(originalEnd, originalEndError) = originalPositions.Normalize(directive.OriginalStart + directive.OriginalLength);
                if (originalStartError is null && originalEndError is null)
                    normalized.OriginalRange = new((int)originalStart, (int)originalEnd);
                else
                    validOriginal = false;
            }

            if (normalized.Policy == MappedDiagnosticDirectivePolicy.Expect && !validOriginal)
                return (null, Error(DiagnosticDirectiveErrorKind.InvalidRange, i));
            result.Add(normalized);
        }

        var sorted = new (NormalizedDirective Directive, int Index)[result.Count];
        for (int i = 0; i < result.Count; i++)
            sorted[i] = (result[i], i);
        GoSliceSort.SortFunc(sorted.AsSpan(), static (a, b) => a.Directive.VirtualRange.Pos().CompareTo(b.Directive.VirtualRange.Pos()));
        for (int i = 1; i < sorted.Length; i++)
            if (sorted[i].Directive.VirtualRange.Pos() < sorted[i - 1].Directive.VirtualRange.End())
                return (null, Error(DiagnosticDirectiveErrorKind.Overlap, sorted[i].Index));
        return (result, null);
    }

    private static DiagnosticDirectiveError Error(DiagnosticDirectiveErrorKind kind, int index) => new()
    {
        Kind = kind,
        Index = index,
        SupplementalIndex = -1
    };
    // hostimpl.go:1290. Segments() and New retain their actual required clones.
    internal static (TypeScript.SpanMap.SpanMap? Value, Exception? Error) NormalizeMappings(TypeScript.SpanMap.SpanMap mappings, PositionNormalizer virtualPositions, PositionNormalizer originalPositions)
    {
        var segments = mappings.Segments();
        if (segments is not null)
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var(virtualStart, error) = virtualPositions.NormalizeTextPos(segment.VirtualStart);
                if (error is not null)
                    return (null, MappingError(i, "virtual start", error));
                segment = segment with
                {
                    VirtualStart = virtualStart
                };
                var(virtualEnd, endError) = virtualPositions.NormalizeTextPos(segment.VirtualEnd);
                if (endError is not null)
                    return (null, MappingError(i, "virtual end", endError));
                segment = segment with
                {
                    VirtualEnd = virtualEnd
                };
                var(originalStart, originalError) = originalPositions.NormalizeTextPos(segment.OriginalStart);
                if (originalError is not null)
                    return (null, MappingError(i, "original start", originalError));
                segment = segment with
                {
                    OriginalStart = originalStart
                };
                var(originalEnd, originalEndError) = originalPositions.NormalizeTextPos(segment.OriginalEnd);
                if (originalEndError is not null)
                    return (null, MappingError(i, "original end", originalEndError));
                segments[i] = segment with
                {
                    OriginalEnd = originalEnd
                };
            }

        return (SpanMaps.New(segments), null);
    }

    private static Exception MappingError(int index, string coordinate, Exception error) => new InvalidOperationException("invalid content mapper mapping " + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + coordinate + ": " + error.Message, error);
    // hostimpl.go:1173. The nonnil empty span map means synthesized output;
    // it must remain distinct from nil (which means not content-mapped).
    internal static (MappedResult Value, PositionNormalizer? OriginalPositions, Exception? Error) DecodeMappedOutput(MappedOutput output, ByteStringValue originalText, PositionEncoding encoding, ByteStringValue diagnosticSource)
    {
        string extension = output.Extension.Materialize() ?? "";
        if (!ContentMappers.IsSupportedVirtualExtension(extension))
            return (default, null, new InvalidVirtualExtensionError { Extension = extension });
        var result = new MappedResult
        {
            Text = output.Text.Materialize() ?? "",
            VirtualExtension = extension
        };
        var(virtualPositions, virtualError) = PositionNormalizer.New(output.Text, encoding);
        if (virtualError is not null)
            return (default, null, virtualError);
        var(originalPositions, originalError) = PositionNormalizer.New(originalText, encoding);
        if (originalError is not null)
            return (default, null, originalError);
        if (output.Mappings is { } raw && raw.Bytes.Length > 0)
        {
            // Promotion requires the exact existing SpanMaps/JsonCodec borrowed
            // span overload; the original retained json.Value already owns bytes.
            var(map, decodeError) = SpanMaps.Unmarshal(raw.BorrowOwnedBytes());
            if (decodeError is not null)
                return (default, null, decodeError);
            var(normalized, normalizeError) = NormalizeMappings(map!, virtualPositions!, originalPositions!);
            if (normalizeError is not null)
                return (default, null, normalizeError);
            result.Mappings = normalized;
        }
        else
            result.Mappings = SpanMaps.New(null);
        var(directives, directiveError) = NormalizeDiagnosticDirectives(output.DiagnosticDirectives, virtualPositions!, originalPositions!, diagnosticSource);
        if (directiveError is not null)
            return (default, null, directiveError);
        result.DiagnosticDirectives = directives;
        return (result, originalPositions, null);
    }

    // hostimpl.go:1124. Full wire result decoding precedes normalized output;
    // supplemental errors retain their original supplemental index.
    internal static (Result Value, Exception? Error) DecodeTransformResult(RawJsonValue? raw, ByteStringValue originalText, PositionEncoding encoding, ByteStringValue diagnosticSource)
    {
        TransformResult output = new();
        try
        {
            ProtocolCodec.Unmarshal(ContentMapperCodecs.TransformResultValue, raw?.TextValue ?? default, ref output);
        }
        catch (System.Text.Json.JsonException error)
        {
            return (default, error);
        }

        var(mapped, originalPositions, mappedError) = DecodeMappedOutput(output, originalText, encoding, diagnosticSource);
        if (mappedError is not null)
            return (default, mappedError);
        var result = new Result
        {
            Text = mapped.Text,
            VirtualExtension = mapped.VirtualExtension,
            Mappings = mapped.Mappings,
            DiagnosticDirectives = mapped.DiagnosticDirectives
        };
        for (int i = 0; i < output.Supplemental.Count; i++)
        {
            var(supplemental, _, supplementalError) = DecodeMappedOutput(output.Supplemental[i], originalText, encoding, diagnosticSource);
            if (supplementalError is not null)
            {
                if (supplementalError is DiagnosticDirectiveError directiveError)
                    directiveError.SupplementalIndex = i;
                return (default, supplementalError);
            }

            (result.Supplemental ??= new()).Add(supplemental);
        }

        foreach (var diagnostic in output.Diagnostics)
        {
            if (diagnostic.Start < 0 || diagnostic.Length < 0 || diagnostic.Start > long.MaxValue - diagnostic.Length)
                return (default, new InvalidOperationException("invalid content mapper diagnostic range [" + Number(diagnostic.Start) + ", " + Number(unchecked(diagnostic.Start + diagnostic.Length)) + ")"));
            var(start, startError) = originalPositions!.Normalize(diagnostic.Start);
            if (startError is not null)
                return (default, new InvalidOperationException("invalid content mapper diagnostic start: " + startError.Message, startError));
            var(end, endError) = originalPositions.Normalize(diagnostic.Start + diagnostic.Length);
            if (endError is not null)
                return (default, new InvalidOperationException("invalid content mapper diagnostic end: " + endError.Message, endError));
            (result.Diagnostics ??= new()).Add(Ast.NewExternalDiagnostic(null!, new((int)start, (int)end), diagnosticSource.Materialize() ?? "", DiagnosticCategory.Error, diagnostic.Code, diagnostic.MessageText.Materialize() ?? ""));
        }

        return (result, null);
    }

    private static string Number(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
