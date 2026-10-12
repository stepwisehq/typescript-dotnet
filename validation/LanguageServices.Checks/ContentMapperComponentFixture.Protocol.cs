// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected contentmappertest/{component,protocol,spawner,manifest,registry}.go
// component handler closure; actual IPC over the accepted net.Pipe adapter.
using TypeScript.Scanner;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.ContentMapper;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
internal static partial class ContentMapperComponentFixture
{
    internal const string PackageJSON = "{\n\t\"name\": \"mapper\",\n\t\"version\": \"1.0.0\",\n\t\"typescript\": { \"contentMapper\": { \"exec\": [\"component-mapper\"] } }\n}";
    internal static ISpawner NewSpawner() => new Spawner();
    private static object Payload<T>(ProtocolType<T> descriptor, T value) => new ProtocolPayload<T>(descriptor, value);
    private sealed class ComponentHandler : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
                return (Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = PositionEncoding.UTF8, DiagnosticSource = ByteStringValue.FromString("mapper") }), null);
            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(parsed, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                if (error is not null)
                    return (null, error);
                var(text, mappings, transformError) = TransformComponent(parsed!.Value.Content);
                if (transformError is not null)
                    return (null, transformError);
                return (Payload(ContentMapperCodecs.TransformResultValue, new TransformResult { Text = text, Extension = ByteStringValue.FromString(".ts"), Mappings = mappings }), null);
            }

            return (null, new Exception("contentmappertest: unexpected method " + GoQuote.Quote(method.Materialize()!)));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private sealed class StaticProjectHandler(IHandler handler) : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodOpenProject))
            {
                var(parsed, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.OpenProjectParamsValue);
                if (error is not null)
                    return (null, error);
                Slice<OptionDiagnosticResult> diagnostics = default;
                if (parsed!.Value.Options is { } options && options.Bytes.SequenceEqual("{\"plugins\":[{\"name\":1}]}"u8))
                    diagnostics = Slice<OptionDiagnosticResult>.FromArray([new() { Path = Slice<RawJsonValue?>.FromArray([RawJsonValue.FromRetainedBytes("\"plugins\""u8.ToArray()), RawJsonValue.FromRetainedBytes("0"u8.ToArray()), RawJsonValue.FromRetainedBytes("\"name\""u8.ToArray())]), MessageText = ByteStringValue.FromString("Option 'name' requires a string."), Code = 123 }]);
                return (Payload(ContentMapperCodecs.OpenProjectResultValue, new OpenProjectResult { OptionDiagnostics = diagnostics }), null);
            }

            if (method.EqualsValue(ProcessHosts.MethodCloseProject))
            {
                var(_, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.CloseProjectParamsValue);
                return (null, error);
            }

            return handler.HandleRequest(context, method, parameters);
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => handler.HandleNotification(context, method, parameters);
    }

    private static string FormatCommand(Slice<ByteStringValue> command)
    {
        var output = new GoBuilder();
        output.WriteByte('[');
        for (int i = 0; i < command.Length; i++)
        {
            if (i != 0)
                output.WriteByte(' ');
            output.WriteString(command[i]);
        }

        output.WriteByte(']');
        return output.String();
    }

    private sealed class Spawner : ISpawner
    {
        public (Stream? Duplex, Exception? Error) Spawn(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr)
        {
            if (command.Length == 0)
                return (null, new Exception("contentmappertest: empty mapper command"));
            if (!command[0].EqualsString("component-mapper"))
                return (null, new Exception("contentmappertest: unknown mapper command " + FormatCommand(command)));
            var(client, server) = IpcTestPipe.Pair();
            ThreadPool.UnsafeQueueUserWorkItem(static pair =>
            {
                _ = AsyncConn.NewAsyncConnWithProtocol(pair.Server, JSONRPCProtocol.NewJSONRPCProtocol(pair.Server), pair.Handler).Run(CancellationToken.None);
            }, (Server: server, Handler: (IHandler)new StaticProjectHandler(new ComponentHandler())), preferLocal: false);
            return (client, null);
        }
    }
}
