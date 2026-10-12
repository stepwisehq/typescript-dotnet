// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using WireDirective = TypeScript.ContentMapper.WireMappedDiagnosticDirective;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    // Test-only closed map response projection; the actual raw response is unchanged.
    private static readonly ProtocolType<RawJsonValue> RawResponseDescriptor = new("map[string]interface {}", ReadRawResponse, static (encoder, value) => encoder.WriteRaw(value.Bytes));
    private static void ReadRawResponse(ref ProtocolDecoder decoder, ref RawJsonValue value) => value = decoder.ReadValue().Clone();
    private static TError? ErrorAs<TError>(Exception? error)
        where TError : Exception
    {
        if (error is TError found)
            return found;
        if (error?.InnerException is { } inner && ErrorAs<TError>(inner)is { } nested)
            return nested;
        if (error is TypeScript.Ipc.AsyncConn.JoinedError joined)
            return ErrorAs<TError>(joined.Second);
        if (error is ProcessHost.HostJoinedError hostJoined)
            return ErrorAs<TError>(hostJoined.Second);
        return null;
    }

    private static DiagnosticDirectives Directives(WireDirective[] directives, params UnusedExpectDirectiveDiagnostic[] unused) => new()
    {
        Directives = Slice<WireDirective>.FromArray(directives),
        // The variadic empty slice is nil in the original helper call.
        UnusedExpectDirectiveDiagnostics = unused.Length == 0 ? default : Slice<UnusedExpectDirectiveDiagnostic>.FromArray(unused)
    };
    private static (Result Result, Exception? Error) TransformOutput(TransformResult output, string content = "component") => TransformWith(new ResponseMapper(_ => Payload(ContentMapperCodecs.TransformResultValue, output)), new Request { FileName = new RootedFilePath("/component.vue"), Content = content });
    private static void RunnerTransformResponseValidation()
    {
        // Exact malformed wire response {"text":1}, transported by the actual raw descriptor.
        var(_, error) = TransformWith(new ResponseMapper(_ => Payload(RawResponseDescriptor, new RawJsonValue("{\"text\":1}"u8))), new Request { FileName = new RootedFilePath("/a.vue"), Content = "a" });
        Assert(error is not null, "malformed result fails request");
    }

    private static void RunnerTransformSupplementalOutputs()
    {
        var(result, error) = TransformOutput(new() { Text = Text("export default 1;"), Extension = Text(".ts"), Supplemental = Slice<SupplementalOutput>.FromArray([new() { Text = Text("declare const first: string;"), Extension = Text(".ts"), DiagnosticDirectives = Directives([new() { VirtualEnd = 7, Policy = DiagnosticDirectivePolicy.Ignore }]) }, new() { Text = Text("declare const second: number;"), Extension = Text(".mjs") }]) });
        Assert(error is null, error?.Message ?? "supplemental transform failed");
        Assert(result.Supplemental?.Count == 2, "two supplemental outputs");
        Assert(result.Supplemental![0].Text == "declare const first: string;", "first supplemental text");
        Assert(result.Supplemental[0].VirtualExtension == ".ts" && result.Supplemental[0].Mappings is not null, "first supplemental extension/mapping");
        Assert(result.Supplemental[0].DiagnosticDirectives?.Count == 1 && result.Supplemental[0].DiagnosticDirectives![0].VirtualRange.End() == 7, "first supplemental directive");
        Assert(result.Supplemental[1].VirtualExtension == ".mjs" && result.Supplemental[1].Mappings is not null, "second supplemental extension/mapping");
    }

    private static void RunnerTransformInvalidSupplementalDiagnosticDirective()
    {
        var(_, error) = TransformOutput(new() { Text = Text("export {};"), Extension = Text(".ts"), Supplemental = Slice<SupplementalOutput>.FromArray([new() { Text = Text("export {};"), Extension = Text(".ts") }, new() { Text = Text("export {};"), Extension = Text(".ts"), DiagnosticDirectives = Directives([new() { Policy = DiagnosticDirectivePolicy.Expect }]) }]) });
        var directiveError = ErrorAs<DiagnosticDirectiveError>(error);
        Assert(directiveError is not null && directiveError.Kind == DiagnosticDirectiveErrorKind.ExpectMissingUnusedDiagnostic && directiveError.Index == 0 && directiveError.SupplementalIndex == 1, "original supplemental error fields");
    }

    private static void RunnerRejectsInvalidVirtualExtension()
    {
        foreach (bool supplemental in new[]
        {
            false,
            true
        }

        )
            foreach (string extension in new[]
            {
                "",
                ".coffee"
            }

            )
            {
                var(_, error) = TransformOutput(new() { Text = Text("export {};"), Extension = Text(supplemental ? ".ts" : extension), Supplemental = supplemental ? Slice<SupplementalOutput>.FromArray([new() { Text = Text("export {};"), Extension = Text(extension) }]) : default });
                Assert(error is not null && error.Message.Contains("invalid virtual extension", StringComparison.Ordinal), "original virtual extension rejection");
            }
    }

    private static void RunnerTransformDiagnosticDirectives()
    {
        var output = new TransformResult
        {
            Text = Text("virtual source"),
            Extension = Text(".ts"),
            DiagnosticDirectives = Directives([new() { OriginalLength = 9, VirtualStart = 8, VirtualEnd = 14, Policy = DiagnosticDirectivePolicy.Expect }], new UnusedExpectDirectiveDiagnostic { Code = 2578, MessageText = Text("Unused framework directive.") })
        };
        var(result, error) = TransformOutput(output, "directive\nsource");
        Assert(error is null, error?.Message ?? "directive transform failed");
        Assert(result.DiagnosticDirectives?.Count == 1, "one normalized directive");
        var directive = result.DiagnosticDirectives![0];
        Assert(directive.OriginalRange.Pos() == 0 && directive.OriginalRange.End() == 9 && directive.VirtualRange.Pos() == 8 && directive.VirtualRange.End() == 14, "original/virtual directive ranges");
        Assert(directive.Policy == TypeScript.Scanner.MappedDiagnosticDirectivePolicy.Expect && directive.UnusedCode == 2578 && directive.UnusedMessageText == "Unused framework directive." && directive.Source == "mapper", "expect diagnostic/source fields");
        output.DiagnosticDirectives = Directives([new() { OriginalLength = 9, VirtualStart = 8, VirtualEnd = 14, Policy = DiagnosticDirectivePolicy.Expect, UnusedExpectDirectiveIndex = 1 }], new UnusedExpectDirectiveDiagnostic { Code = 1, MessageText = Text("first") }, new UnusedExpectDirectiveDiagnostic { Code = 2, MessageText = Text("second") });
        (result, error) = TransformOutput(output, "directive\nsource");
        Assert(error is null, error?.Message ?? "indexed directive transform failed");
        Assert(result.DiagnosticDirectives![0].UnusedCode == 2 && result.DiagnosticDirectives[0].UnusedMessageText == "second", "selected unused diagnostic");
        var(_, ignoredError) = TransformOutput(new() { Text = Text("x"), Extension = Text(".ts"), DiagnosticDirectives = Directives([new() { OriginalStart = -1, Policy = DiagnosticDirectivePolicy.Ignore }], new UnusedExpectDirectiveDiagnostic()) }, "directive\nsource");
        Assert(ignoredError is null, "ignore tolerates invalid original range");
        foreach (var(name, text, directives, expectedKind, unusedCount)in new (string, string, WireDirective[], DiagnosticDirectiveErrorKind, int)[]
        {
            ("invalid range", "x", [new() { VirtualStart = -1, Policy = DiagnosticDirectivePolicy.Ignore }], DiagnosticDirectiveErrorKind.InvalidRange, 0),
            ("unknown policy", "x", [new() { Policy = (DiagnosticDirectivePolicy)2 }], DiagnosticDirectiveErrorKind.InvalidPolicy, 0),
            ("expect requires unused diagnostic", "x", [new() { Policy = DiagnosticDirectivePolicy.Expect }], DiagnosticDirectiveErrorKind.ExpectMissingUnusedDiagnostic, 0),
            ("multiple unused diagnostics require index", "x", [new() { Policy = DiagnosticDirectivePolicy.Expect }], DiagnosticDirectiveErrorKind.ExpectMissingUnusedDiagnostic, 2),
            ("original range out of bounds", "x", [new() { OriginalStart = 99, Policy = DiagnosticDirectivePolicy.Expect }], DiagnosticDirectiveErrorKind.InvalidRange, 1),
            ("virtual range out of bounds", "x", [new() { VirtualStart = 99, Policy = DiagnosticDirectivePolicy.Ignore }], DiagnosticDirectiveErrorKind.InvalidRange, 0),
            ("overlap", "abc", [new() { VirtualEnd = 2, Policy = DiagnosticDirectivePolicy.Ignore }, new() { VirtualStart = 1, VirtualEnd = 3, Policy = DiagnosticDirectivePolicy.Ignore }], DiagnosticDirectiveErrorKind.Overlap, 0)
        }

        )
        {
            var wire = Directives(directives);
            wire.UnusedExpectDirectiveDiagnostics = unusedCount == 0 ? default : Slice<UnusedExpectDirectiveDiagnostic>.Make(unusedCount);
            var(_, invalidError) = TransformOutput(new() { Text = Text(text), Extension = Text(".ts"), DiagnosticDirectives = wire }, "directive\nsource");
            Assert(ErrorAs<DiagnosticDirectiveError>(invalidError)?.Kind == expectedKind, name);
        }
    }
}
