// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using WireDirective = TypeScript.ContentMapper.WireMappedDiagnosticDirective;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    // host_test.go:576. Original complete tuple admission and round-trip test.
    private static void MappedDiagnosticDirectiveJSON()
    {
        foreach (var policy in new[]
        {
            DiagnosticDirectivePolicy.Ignore,
            DiagnosticDirectivePolicy.Expect
        }

        )
        {
            var original = new WireDirective
            {
                VirtualStart = 8,
                VirtualEnd = 14,
                OriginalStart = 0,
                OriginalLength = 9,
                Policy = policy
            };
            var data = ProtocolCodec.Marshal(ContentMapperCodecs.MappedDiagnosticDirectiveValue, original);
            var expected = policy == DiagnosticDirectivePolicy.Ignore ? "[0,9,8,14,0]" : "[0,9,8,14,1]";
            Assert(data.EqualsValue(Text(expected)), "original directive JSON bytes");
            WireDirective decoded = default;
            ProtocolCodec.Unmarshal(ContentMapperCodecs.MappedDiagnosticDirectiveValue, data, ref decoded);
            AssertDirective(decoded, original);
        }

        foreach (var invalid in new[]
        {
            "[0,0,0,0]",
            "[0,0,0,0,0,1,2]"
        }

        )
        {
            WireDirective decoded = default;
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ContentMapperCodecs.MappedDiagnosticDirectiveValue, Text(invalid), ref decoded);
            }
            catch (System.Text.Json.JsonException caught)
            {
                error = caught;
            }

            Assert(error is not null && error.Message.Contains("diagnostic directive tuple", StringComparison.Ordinal), "tuple cardinality error");
        }

        var originalDirectives = new DiagnosticDirectives
        {
            UnusedExpectDirectiveDiagnostics = Slice<UnusedExpectDirectiveDiagnostic>.FromArray([new() { Code = 1, MessageText = Text("first") }, new() { Code = 2, MessageText = Text("second") }]),
            Directives = Slice<WireDirective>.FromArray([new() { OriginalStart = 2, OriginalLength = 3, VirtualStart = 5, VirtualEnd = 9, Policy = DiagnosticDirectivePolicy.Expect, UnusedExpectDirectiveIndex = 1 }])
        };
        var bytes = ProtocolCodec.Marshal(ContentMapperCodecs.DiagnosticDirectivesValue, originalDirectives);
        Assert(bytes.EqualsValue(Text("{\"unusedExpectDirectiveDiagnostics\":[{\"code\":1,\"messageText\":\"first\"},{\"code\":2,\"messageText\":\"second\"}],\"directives\":[[2,3,5,9,1,1]]}")), "original directive container JSON bytes");
        DiagnosticDirectives decodedDirectives = new();
        ProtocolCodec.Unmarshal(ContentMapperCodecs.DiagnosticDirectivesValue, bytes, ref decodedDirectives);
        Assert(!decodedDirectives.UnusedExpectDirectiveDiagnostics.IsNil && decodedDirectives.UnusedExpectDirectiveDiagnostics.Length == 2, "unused diagnostics header/count");
        for (int i = 0; i < 2; i++)
        {
            Assert(decodedDirectives.UnusedExpectDirectiveDiagnostics[i].Code == originalDirectives.UnusedExpectDirectiveDiagnostics[i].Code, "unused diagnostic code");
            Assert(decodedDirectives.UnusedExpectDirectiveDiagnostics[i].MessageText.EqualsValue(originalDirectives.UnusedExpectDirectiveDiagnostics[i].MessageText), "unused diagnostic message");
        }

        Assert(!decodedDirectives.Directives.IsNil && decodedDirectives.Directives.Length == 1, "directives header/count");
        AssertDirective(decodedDirectives.Directives[0], originalDirectives.Directives[0]);
    }

    private static void AssertDirective(WireDirective actual, WireDirective expected)
    {
        Assert(actual.OriginalStart == expected.OriginalStart && actual.OriginalLength == expected.OriginalLength && actual.VirtualStart == expected.VirtualStart && actual.VirtualEnd == expected.VirtualEnd && actual.Policy == expected.Policy && actual.UnusedExpectDirectiveIndex == expected.UnusedExpectDirectiveIndex, "complete directive value equality");
    }
}
