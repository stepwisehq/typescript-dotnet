// Complete demanded original contentmappertest/transforming.go worker closure, raw byte offsets.
using TypeScript.Scanner;
using TypeScript.ContentMapper;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using SpanKind = TypeScript.SpanMap.Kind;

namespace LanguageServices.Checks;
internal static partial class ContentMapperProjectFixture
{
    private sealed class TransformingHandler : IHandler, ILifecycleHandler
    {
        private readonly object gate = new();
        private Dictionary<ByteStringKey, Dictionary<ByteStringKey, RawJsonValue>?>? compilerOptions;
        public Exception? OpenProject(OpenProjectParams p)
        {
            Dictionary<ByteStringKey, RawJsonValue>? options = null;
            try
            {
                var decoder = new ProtocolDecoder(p.CompilerOptions?.TextValue ?? default);
                if (decoder.PeekKind() == 'n')
                    decoder.ReadToken((byte)'n');
                else
                {
                    options = new();
                    decoder.ReadObjectStart("collections.OrderedMap[string,json.Value]");
                    while (decoder.PeekKind() != '}')
                    {
                        var name = decoder.ReadDecodedPropertyName();
                        options[ByteStringKey.FromValue(name)] = decoder.ReadValue().Clone();
                    }

                    decoder.ReadToken((byte)'}');
                }

                decoder.Finish();
            }
            catch (Exception error)
            {
                return error;
            }

            lock (gate)
                (compilerOptions ??= new())[ByteStringKey.FromValue(p.ProjectHandle)] = options;
            return null;
        }

        public void CloseProject(CloseProjectParams p)
        {
            lock (gate)
                compilerOptions?.Remove(ByteStringKey.FromValue(p.ProjectHandle));
        }

        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
                return (Payload(ContentMapperCodecs.InitializeResultValue, Initialize("box")), null);
            if (!method.EqualsValue(ProcessHosts.MethodTransform))
                return (null, Unexpected(method));
            var(p, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
            if (error is not null)
                return (null, error);
            Dictionary<ByteStringKey, RawJsonValue>? options = null;
            lock (gate)
                compilerOptions?.TryGetValue(ByteStringKey.FromValue(p!.Value.ProjectHandle), out options);
            if (options is null)
                return (null, new InvalidOperationException("contentmappertest: project " + GoQuote.Quote(p!.Value.ProjectHandle.Materialize()!) + " is not open"));
            var(result, transformError) = Transform(p!.Value.Content, options);
            return transformError is null ? (Payload(ContentMapperCodecs.TransformResultValue, result), null) : (null, transformError);
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private static (TransformResult Result, Exception? Error) Transform(ByteStringValue content, Dictionary<ByteStringKey, RawJsonValue> options)
    {
        var text = new GoBuilder();
        List<Segment>? segments = null;
        Slice<WireDiagnostic> diagnostics = default;
        text.WriteString("const __VERSION = \"1.0.0\";\n");
        void Verbatim(int from, int to)
        {
            if (to <= from)
                return;
            int start = text.Len();
            text.WriteString(content.Slice(from, to));
            (segments ??= new()).Add(new(start, text.Len(), from, to, SpanKind.Verbatim, Feature.All));
        }

        void Atom(ByteStringValue value, int from, int to)
        {
            int start = text.Len();
            text.WriteString(value);
            (segments ??= new()).Add(new(start, text.Len(), from, to, SpanKind.Atom, Feature.All));
        }

        int pos = 0;
        while (pos < content.Length)
        {
            int relative = content.Slice(pos).IndexOf("#{"u8);
            if (relative < 0)
            {
                Verbatim(pos, content.Length);
                break;
            }

            int start = pos + relative;
            int lineEnd = start + content.Slice(start).IndexOf((byte)'\n');
            if (lineEnd < start)
                lineEnd = content.Length;
            int close = content.Slice(start, lineEnd).IndexOf((byte)'}');
            if (close < 0)
            {
                Verbatim(pos, start);
                Atom(ByteStringValue.FromString("undefined"), start, lineEnd);
                diagnostics = diagnostics.Append(new WireDiagnostic { MessageText = ByteStringValue.FromString("Unclosed interpolation."), Start = start, Length = lineEnd - start, Code = 1000 });
                pos = lineEnd;
                continue;
            }

            int end = start + close + 1;
            var name = content.Slice(start + 2, end - 1);
            Verbatim(pos, start);
            var value = options.TryGetValue(ByteStringKey.FromValue(name), out var raw) && raw.TextValue.Length > 0 ? raw.TextValue : ByteStringValue.FromString("undefined");
            Atom(value, start, end);
            pos = end;
        }

        var map = SpanMaps.New(segments);
        var(bytes, error) = map.Marshal();
        if (error is not null)
            return (new(), error);
        var extension = ByteStringValue.FromString(".ts");
        int newline = content.IndexOf((byte)'\n');
        var prefix = ByteStringValue.FromString("// @box-extension:");
        if (newline >= 0 && content.Slice(0, newline).StartsWith(prefix))
            extension = Trim(content.Slice(prefix.Length, newline));
        return (new() { Text = text.Value(), Extension = extension, Mappings = RawJsonValue.FromRetainedBytes(bytes!), Diagnostics = diagnostics, DiagnosticDirectives = Directives(content, map) }, null);
    }

    private static ByteStringValue Trim(ByteStringValue text) => GoStrings.TrimFunc(text, GoUnicode.IsSpace);
    private static DiagnosticDirectives? Directives(ByteStringValue content, TypeScript.SpanMap.SpanMap map)
    {
        static DiagnosticDirectives Wrap(Slice<WireMappedDiagnosticDirective> directives, params UnusedExpectDirectiveDiagnostic[] unused) => new()
        {
            Directives = directives,
            UnusedExpectDirectiveDiagnostics = unused.Length == 0 ? default : Slice<UnusedExpectDirectiveDiagnostic>.FromArray(unused)
        };
        var invalid = ByteStringValue.FromString("// @box-invalid-directive:");
        if (content.StartsWith(invalid))
        {
            int end = content.IndexOf((byte)'\n');
            if (end < 0)
                end = content.Length;
            switch (Trim(content.Slice(invalid.Length, end)).Materialize())
            {
                case "invalid-range":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { VirtualStart = -1, Policy = DiagnosticDirectivePolicy.Ignore }]));
                case "original-range-out-of-bounds":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { OriginalStart = content.Length + 1, Policy = DiagnosticDirectivePolicy.Ignore }]));
                case "virtual-range-out-of-bounds":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { VirtualStart = 1 << 20, VirtualEnd = 1 << 20, Policy = DiagnosticDirectivePolicy.Ignore }]));
                case "invalid-policy":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { Policy = (DiagnosticDirectivePolicy)2 }]));
                case "ignore-with-unused-diagnostic":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { Policy = DiagnosticDirectivePolicy.Ignore }]), new UnusedExpectDirectiveDiagnostic());
                case "expect-without-unused-diagnostic":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { Policy = DiagnosticDirectivePolicy.Expect }]));
                case "invalid-unused-diagnostic-index":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { Policy = DiagnosticDirectivePolicy.Expect, UnusedExpectDirectiveIndex = 1 }]), new UnusedExpectDirectiveDiagnostic());
                case "overlap":
                    return Wrap(Slice<WireMappedDiagnosticDirective>.FromArray([new() { VirtualEnd = 2, Policy = DiagnosticDirectivePolicy.Ignore }, new() { VirtualStart = 1, VirtualEnd = 3, Policy = DiagnosticDirectivePolicy.Ignore }]));
            }
        }

        var ignore = ByteStringValue.FromString("// @box-ignore");
        var expect = ByteStringValue.FromString("// @box-expect-error");
        Slice<WireMappedDiagnosticDirective> directives = default;
        Slice<UnusedExpectDirectiveDiagnostic> unused = default;
        for (int start = 0; start < content.Length;)
        {
            int end = content.Slice(start).IndexOf((byte)'\n');
            end = end < 0 ? content.Length : end + start;
            var line = Trim(content.Slice(start, end));
            DiagnosticDirectivePolicy policy = default;
            bool hasPolicy = false;
            int unusedIndex = -1;
            if (line.EqualsValue(ignore))
            {
                policy = DiagnosticDirectivePolicy.Ignore;
                hasPolicy = true;
            }
            else if (line.StartsWith(ByteStringValue.Concat(expect, ByteStringValue.FromString(":"))))
            {
                policy = DiagnosticDirectivePolicy.Expect;
                hasPolicy = true;
                unusedIndex = unused.Length;
                unused = unused.Append(new UnusedExpectDirectiveDiagnostic { Code = 2578, MessageText = Trim(line.Slice(expect.Length + 1)) });
            }

            if (hasPolicy && end < content.Length)
            {
                int affectedStart = end + 1, length = content.Slice(affectedStart).IndexOf((byte)'\n');
                if (length < 0)
                    length = content.Length - affectedStart;
                var spans = map.OriginalToVirtualSpans(new TextRange(affectedStart, affectedStart + length), Feature.All);
                if (spans?.Count == 1)
                    directives = directives.Append(new WireMappedDiagnosticDirective { OriginalStart = start, OriginalLength = end - start, VirtualStart = spans[0].Span.Pos(), VirtualEnd = spans[0].Span.End(), Policy = policy, UnusedExpectDirectiveIndex = unusedIndex >= 0 ? unusedIndex : null });
            }

            if (end == content.Length)
                break;
            start = end + 1;
        }

        if (unused.Length == 1)
            for (int i = 0; i < directives.Length; i++)
                directives.AsSpan()[i].UnusedExpectDirectiveIndex = null;
        return directives.Length == 0 && unused.Length == 0 ? null : new()
        {
            Directives = directives,
            UnusedExpectDirectiveDiagnostics = unused
        };
    }
}
