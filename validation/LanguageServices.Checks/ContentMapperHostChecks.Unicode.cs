// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using LocaleValue = TypeScript.Locale.Locale;
using WireDiagnostic = TypeScript.ContentMapper.WireDiagnostic;
using WireDirective = TypeScript.ContentMapper.WireMappedDiagnosticDirective;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    // host_test.go:102. Optional source pointer distinguishes absent from empty.
    private sealed class UnicodeMapper(PositionEncoding encoding, string? source = null) : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.InitializeParamsValue);
                if (error is not null)
                    return (null, error);
                bool offered = false;
                foreach (var candidate in value!.Value.PositionEncodings)
                    offered |= candidate == encoding;
                if (!offered && (encoding == PositionEncoding.UTF8 || encoding == PositionEncoding.UTF16))
                    return (null, new InvalidOperationException("position encoding " + GoQuote.Quote(encoding.AsString()) + " was not offered"));
                return (Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = encoding, DiagnosticSource = Text(source ?? "mapper") }), null);
            }

            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                if (error is not null)
                    return (null, error);
                int characterLength, textLength;
                if (encoding == PositionEncoding.UTF8)
                {
                    characterLength = 2;
                    textLength = 3;
                }
                else if (encoding == PositionEncoding.UTF16)
                {
                    characterLength = 1;
                    textLength = 2;
                }
                else
                    return (Payload(ContentMapperCodecs.TransformResultValue, new TransformResult { Text = value!.Value.Content, Extension = Text(".ts") }), null);
                // Same original [5]int tuples, in the negotiated position units.
                string tuples = "[[0," + characterLength + ",0," + characterLength + ",0],[" + characterLength + "," + (textLength - characterLength) + "," + characterLength + "," + (textLength - characterLength) + ",0]]";
                return (Payload(ContentMapperCodecs.TransformResultValue, new TransformResult { Text = value!.Value.Content, Extension = Text(".ts"), Mappings = new RawJsonValue(GoString.Bytes(tuples)), DiagnosticDirectives = new DiagnosticDirectives { Directives = Slice<WireDirective>.FromArray([new() { OriginalStart = characterLength, OriginalLength = textLength - characterLength, VirtualStart = characterLength, VirtualEnd = textLength, Policy = DiagnosticDirectivePolicy.Ignore }]) }, Diagnostics = Slice<WireDiagnostic>.FromArray([new() { MessageText = Text("after non-ASCII character"), Start = characterLength, Length = textLength - characterLength, Code = 1001 }]) }), null);
            }

            return (null, new InvalidOperationException("unexpected method " + method.Materialize()));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    // host_test.go:162.
    private sealed class InvalidDiagnosticMapper(PositionEncoding encoding) : IHandler
    {
        public (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
                return (Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = encoding, DiagnosticSource = Text("mapper") }), null);
            if (method.EqualsValue(ProcessHosts.MethodTransform))
                return (Payload(ContentMapperCodecs.TransformResultValue, new TransformResult { Extension = Text(".ts"), Diagnostics = Slice<WireDiagnostic>.FromArray([new() { MessageText = Text("invalid boundary"), Start = 1, Code = 1002 }]) }), null);
            return (null, new InvalidOperationException("unexpected method " + method.Materialize()));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private static (Result Result, Exception? Error) TransformWith(IHandler handler, Request request, Mapper? mapper = null)
    {
        var host = ProcessHosts.NewHost(CancellationToken.None, new FakeSpawner(handler), TypeScript.Locale.Locales.Default);
        try
        {
            return host.Transform(mapper ?? Mapper(), request);
        }
        finally
        {
            _ = host.Close();
        }
    }

    // host_test.go:731. Both encoding rows and every original mapping/directive observation.
    private static void RunnerPositionEncodings()
    {
        foreach (var encoding in new[]
        {
            PositionEncoding.UTF8,
            PositionEncoding.UTF16
        }

        )
        {
            string content = GoString.ToUtf8("éx");
            var(result, error) = TransformWith(new UnicodeMapper(encoding), new Request { FileName = new RootedFilePath("/a.vue"), Content = content }, Mapper(encoding.AsString()));
            Assert(error is null, error?.Message ?? "position transform failed");
            var segments = result.Mappings!.Segments();
            Assert(segments!.Count == 2, "two mapping segments");
            Assert(segments[0].VirtualEnd == 2 && segments[0].OriginalEnd == 2 && segments[1].VirtualStart == 2 && segments[1].OriginalStart == 2, "normalized UTF8 mapping positions");
            Assert(result.Text == content, "unchanged Unicode content");
            Assert(result.Mappings.Validate(result.Text, content)is null, "mapping validation");
            var(mapped, fidelity) = result.Mappings.VirtualToOriginalPosition(2);
            Assert(mapped == 2 && fidelity == TypeScript.SpanMap.Fidelity.Exact, "exact mapped position");
            Assert(result.Diagnostics![0].Pos() == 2 && result.Diagnostics[0].End() == 3, "diagnostic UTF8 positions");
            var directive = result.DiagnosticDirectives![0];
            Assert(directive.OriginalRange.Pos() == 2 && directive.OriginalRange.End() == 3 && directive.VirtualRange.Pos() == 2 && directive.VirtualRange.End() == 3, "directive UTF8 positions");
        }
    }

    private static void RunnerRejectsUnsupportedPositionEncoding()
    {
        var(_, error) = TransformWith(new UnicodeMapper(new PositionEncoding("utf-32")), new Request { FileName = new RootedFilePath("/a.vue"), Content = "x" }, Mapper("invalid"));
        Assert(error is not null && error.Message.Contains("unsupported position encoding", StringComparison.Ordinal), "unsupported encoding error");
    }

    private static void RunnerRejectsInvalidDiagnosticSource()
    {
        foreach (string source in new[]
        {
            "",
            " ",
            "ts",
            "TS",
            "d.ts",
            "json",
            "typescript",
            "TypeScript",
            "tsc",
            "TSC"
        }

        )
        {
            var(_, error) = TransformWith(new UnicodeMapper(PositionEncoding.UTF8, source), new Request { FileName = new RootedFilePath("/a.vue"), Content = "x" }, Mapper("invalid"));
            string expected = source.Trim().Length == 0 ? "diagnostic source must not be empty" : "is reserved by TypeScript";
            Assert(error is not null && error.Message.Contains(expected, StringComparison.Ordinal), "original source rejection " + source);
        }
    }

    private static void RunnerRejectsPositionsInsideUnicodeCharacters()
    {
        foreach (var(encoding, displayContent)in new[]
        {
            (PositionEncoding.UTF8, "é"),
            (PositionEncoding.UTF16, "😀")
        }

        )
        {
            var(_, error) = TransformWith(new InvalidDiagnosticMapper(encoding), new Request { FileName = new RootedFilePath("/a.vue"), Content = GoString.ToUtf8(displayContent) }, Mapper(encoding.AsString()));
            Assert(error is not null && error.Message.Contains("splits a Unicode code point", StringComparison.Ordinal), "inside-code-point position rejection");
        }
    }
}
