// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original host_test.go in-process mapper fixtures; actual IPC over the shared
// rendezvous duplex pipe replaces net.Pipe, without replacing the process host.
using TypeScript.ContentMapper;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using SpanMaps = TypeScript.SpanMap.SpanMaps;
using MappingSegment = TypeScript.SpanMap.Segment;
using MappingKind = TypeScript.SpanMap.Kind;
using WireDiagnostic = TypeScript.ContentMapper.WireDiagnostic;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    private static ByteStringValue Text(string value) => ByteStringValue.FromString(value);
    private static object Payload<T>(ProtocolType<T> descriptor, T value) => new ProtocolPayload<T>(descriptor, value);
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static Mapper Mapper(string name = "mapper") => new()
    {
        Extensions = [".vue"],
        Name = name,
        Exec = ["mapper"]
    };
    private interface IHandlesProjects
    {
    }

    private sealed class ResponseMapper(Func<TransformParams, object?> response) : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
                return (Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = PositionEncoding.UTF8, DiagnosticSource = Text("mapper") }), null);
            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                return error is null ? (response(value!.Value), null) : (null, error);
            }

            return (null, new InvalidOperationException("unexpected method " + method.Materialize()));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private sealed class FakeMapper : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
                return (Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = PositionEncoding.UTF8, DiagnosticSource = Text("vue") }), null);
            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                if (error is not null)
                    return (null, error);
                var content = value!.Value.Content;
                var(mapping, mappingError) = SpanMaps.New([new MappingSegment(0, content.Length, 0, content.Length, MappingKind.Verbatim, default)]).Marshal();
                if (mappingError is not null)
                    return (null, mappingError);
                return (Payload(ContentMapperCodecs.TransformResultValue, new TransformResult { Text = content, Extension = Text(".ts"), Mappings = RawJsonValue.FromRetainedBytes(mapping!), Diagnostics = Slice<WireDiagnostic>.FromArray([new WireDiagnostic { MessageText = Text("boom"), Start = 0, Length = Math.Min(3, content.Length), Code = 9999 }]) }), null);
            }

            return (null, new InvalidOperationException("unexpected method " + method.Materialize()));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private sealed class NoOpProjectMapper(IHandler handler) : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodOpenProject))
                return (Payload(ContentMapperCodecs.OpenProjectResultValue, new OpenProjectResult()), null);
            if (method.EqualsValue(ProcessHosts.MethodCloseProject))
                return (null, null);
            return handler.HandleRequest(context, method, parameters);
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => handler.HandleNotification(context, method, parameters);
    }

    private sealed class FakeSpawner(IHandler? handler = null) : ISpawner
    {
        internal int Spawns, Closes;
        public (Stream? Duplex, Exception? Error) Spawn(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr)
        {
            Interlocked.Increment(ref Spawns);
            IHandler actual = handler ?? new FakeMapper();
            if (actual is not IHandlesProjects)
                actual = new NoOpProjectMapper(actual);
            var(client, server) = IpcTestPipe.Pair();
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                _ = AsyncConn.NewAsyncConnWithProtocol(state.Server, JSONRPCProtocol.NewJSONRPCProtocol(state.Server), state.Handler).Run(CancellationToken.None);
            }, (Server: server, Handler: actual), preferLocal: false);
            return (new CountingStream(client, this), null);
        }
    }

    private sealed class CountingStream(Stream inner, FakeSpawner owner) : Stream
    {
        private int closed;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref closed, 1) == 0)
                    Interlocked.Increment(ref owner.Closes);
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);
        public override void Flush() => inner.Flush();
        public override bool CanRead => inner.CanRead;
        public override bool CanWrite => inner.CanWrite;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
