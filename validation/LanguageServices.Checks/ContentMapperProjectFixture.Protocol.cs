// Selected complete original contentmappertest protocol/manifest/spawner and demanded mapper handlers.
using TypeScript.Scanner;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.ContentMapper;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using SpanKind = TypeScript.SpanMap.Kind;
using MapperInitializeResult = TypeScript.ContentMapper.InitializeResult;

namespace LanguageServices.Checks;
internal static partial class ContentMapperProjectFixture
{
    internal const string TransformingMapper = "compiler-test-mapper", VerbatimMapper = "verbatim-mapper", ModuleVerbatimMapper = "module-verbatim-mapper", DynamicVerbatimMapper = "dynamic-verbatim-mapper", SupplementalMapper = "supplemental-mapper";
    internal static readonly Slice<ByteStringValue> DeclaredOptions = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("target"), ByteStringValue.FromString("jsx")]);
    internal sealed class ProjectLifecycle
    {
        internal int Opens, Closes;
    }

    internal static string PackageJSON(string mapper)
    {
        string options = mapper == TransformingMapper ? ", \"compilerOptions\": [\"target\", \"jsx\"]" : "";
        string dynamic = mapper == DynamicVerbatimMapper ? ", \"dynamicConfig\": true" : "";
        return "{\n\t\"name\": \"mapper\",\n\t\"version\": \"1.0.0\",\n\t\"typescript\": { \"contentMapper\": { \"exec\": [" + GoQuote.Quote(mapper) + "]" + options + dynamic + " } }\n}";
    }

    internal static ISpawner NewSpawner(ProjectLifecycle? lifecycle = null) => new Spawner(lifecycle);
    private static object Payload<T>(ProtocolType<T> type, T value) => new ProtocolPayload<T>(type, value);
    private static MapperInitializeResult Initialize(string source) => new()
    {
        PositionEncoding = PositionEncoding.UTF8,
        DiagnosticSource = ByteStringValue.FromString(source)
    };
    private static Exception Unexpected(ByteStringValue method) => new InvalidOperationException("contentmappertest: unexpected method " + GoQuote.Quote(method.Materialize()!));
    private static Slice<OptionDiagnosticResult> OptionDiagnostics(RawJsonValue? options) => options is not null && options.TextValue.EqualsString("{\"plugins\":[{\"name\":1}]}") ? Slice<OptionDiagnosticResult>.FromArray([new() { Path = Slice<RawJsonValue?>.FromArray([RawJsonValue.FromRetainedBytes("\"plugins\""u8.ToArray()), RawJsonValue.FromRetainedBytes("0"u8.ToArray()), RawJsonValue.FromRetainedBytes("\"name\""u8.ToArray())]), MessageText = ByteStringValue.FromString("Option 'name' requires a string."), Code = 123 }]) : default;
    private static (MappedOutput Output, Exception? Error) IdentityOutput(ByteStringValue content)
    {
        var(bytes, error) = SpanMaps.New([new Segment(0, content.Length, 0, content.Length, SpanKind.Verbatim, Feature.All)]).Marshal();
        return error is not null ? (new(), error) : (new() { Text = content, Extension = ByteStringValue.FromString(".ts"), Mappings = RawJsonValue.FromRetainedBytes(bytes!) }, null);
    }

    private interface ILifecycleHandler
    {
        Exception? OpenProject(OpenProjectParams p);
        void CloseProject(CloseProjectParams p);
    }

    private sealed class StaticProjectHandler(IHandler inner) : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodOpenProject))
            {
                var(p, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.OpenProjectParamsValue);
                if (error is not null)
                    return (null, error);
                if (inner is ILifecycleHandler handler && handler.OpenProject(p!.Value)is { } failure)
                    return (null, failure);
                return (Payload(ContentMapperCodecs.OpenProjectResultValue, new OpenProjectResult { OptionDiagnostics = OptionDiagnostics(p!.Value.Options) }), null);
            }

            if (method.EqualsValue(ProcessHosts.MethodCloseProject))
            {
                var(p, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.CloseProjectParamsValue);
                if (error is not null)
                    return (null, error);
                if (inner is ILifecycleHandler handler)
                    handler.CloseProject(p!.Value);
                return (null, null);
            }

            return inner.HandleRequest(context, method, parameters);
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => inner.HandleNotification(context, method, parameters);
    }

    private class VerbatimHandler(bool module = false, bool supplemental = false) : IHandler
    {
        public virtual (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
                return (Payload(ContentMapperCodecs.InitializeResultValue, Initialize("mapper")), null);
            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(p, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                if (error is not null)
                    return (null, error);
                var(output, mappingError) = IdentityOutput(p!.Value.Content);
                if (mappingError is not null)
                    return (null, mappingError);
                TransformResult result = supplemental ? new()
                {
                    Text = ByteStringValue.FromString("export {};"),
                    Extension = ByteStringValue.FromString(".ts"),
                    Supplemental = Slice<SupplementalOutput>.FromArray([new() { Text = output.Text, Extension = output.Extension, Mappings = output.Mappings }])
                }

                : new()
                {
                    Text = output.Text,
                    Extension = module ? ByteStringValue.FromString(".mts") : output.Extension,
                    Mappings = output.Mappings
                };
                return (Payload(ContentMapperCodecs.TransformResultValue, result), null);
            }

            return (null, Unexpected(method));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private sealed class DynamicHandler(ProjectLifecycle? lifecycle) : VerbatimHandler
    {
        public override (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodOpenProject))
            {
                if (lifecycle is not null)
                    Interlocked.Increment(ref lifecycle.Opens);
                var(p, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.OpenProjectParamsValue);
                if (error is not null)
                    return (null, error);
                var value = p!.Value;
                var identity = ByteStringValue.Concat(value.ConfigFileName, ByteStringValue.FromString(":"), value.Options?.TextValue ?? default);
                var directory = TsPath.GetDirectoryPath(value.ConfigFileName);
                if (directory.Length == 0)
                    directory = ByteStringValue.FromString("/");
                return (Payload(ContentMapperCodecs.OpenProjectResultValue, new OpenProjectResult { ConfigIdentity = identity, WatchedFiles = Slice<ByteStringValue>.FromArray([TsPath.CombinePaths(directory, ByteStringValue.FromString("mapper.config.json"))]), OptionDiagnostics = OptionDiagnostics(value.Options) }), null);
            }

            if (method.EqualsValue(ProcessHosts.MethodCloseProject))
            {
                if (lifecycle is not null)
                    Interlocked.Increment(ref lifecycle.Closes);
                return (null, null);
            }

            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(p, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                if (error is not null)
                    return (null, error);
                if (p!.Value.ProjectHandle.Length == 0)
                    return (null, new InvalidOperationException("content mapper transform requires a project handle"));
            }

            return base.HandleRequest(context, method, parameters);
        }
    }

    private sealed class Spawner(ProjectLifecycle? lifecycle) : ISpawner
    {
        public (Stream? Duplex, Exception? Error) Spawn(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr)
        {
            if (command.Length == 0)
                return (null, new InvalidOperationException("contentmappertest: empty mapper command"));
            IHandler? handler = command[0].Materialize() switch
            {
                TransformingMapper => new TransformingHandler(),
                VerbatimMapper => new VerbatimHandler(),
                ModuleVerbatimMapper => new VerbatimHandler(module: true),
                DynamicVerbatimMapper => new DynamicHandler(lifecycle),
                SupplementalMapper => new VerbatimHandler(supplemental: true),
                _ => null
            };
            if (handler is null)
            {
                var b = new GoBuilder();
                b.WriteByte('[');
                for (int i = 0; i < command.Length; i++)
                {
                    if (i != 0)
                        b.WriteByte(' ');
                    b.WriteString(command[i]);
                }

                b.WriteByte(']');
                return (null, new InvalidOperationException("contentmappertest: unknown mapper command " + b.String()));
            }

            if (!command[0].EqualsString(DynamicVerbatimMapper))
                handler = new StaticProjectHandler(handler);
            var(client, server) = IpcTestPipe.Pair();
            // Original NewSpawner owns one retained mapper goroutine. The actual
            // synchronous pipe reader requires its own background thread on CLR,
            // rather than occupying a pool worker needed by request handlers.
            var reader = new Thread(static value =>
            {
                var state = ((Stream Server, IHandler Handler))value!;
                _ = AsyncConn.NewAsyncConnWithProtocol(state.Server, JSONRPCProtocol.NewJSONRPCProtocol(state.Server), state.Handler).Run(CancellationToken.None);
            })
            {
                IsBackground = true
            };
            var readerState = (Server: (Stream)server, Handler: handler);
            if (ExecutionContext.IsFlowSuppressed())
                reader.Start(readerState);
            else
            {
                using var flow = ExecutionContext.SuppressFlow();
                reader.Start(readerState);
            }

            return (client, null);
        }
    }
}
