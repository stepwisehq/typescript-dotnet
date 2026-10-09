// Copyright 2023 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Selected source assertions, not a complete jsonwire/jsontext/v2 test suite.
using TypeScript.Json;
using TypeScript.Json.Internal;
using TypeScript.Scanner;
namespace Configuration.Checks;
public static partial class JsonOriginalChecks
{
    private sealed record Row(string Input, int Count, string? Error, string? Want, string? Literal, string? Bits, bool Ok);
    public static IReadOnlyList<string> Cases { get; } = ["TestConsumeWhitespace", "TestConsumeLiteral", "TestConsumeString", "TestConsumeNumber", "TestParseHexUint16", "TestAppendQuote", "TestAppendNumber", "TestQuoteRune", "TestTruncatePointer", "TestDecoder", "TestDecoderErrors", "TestEncoderErrors", "TestCoderMaxDepth", "TestPointer", "TestObjectNamespace", "TestValueMethods", "TestMarshal", "TestUnmarshal", "TestEncoder", "TestSemanticError"];
    private static string Raw(string value) => GoString.FromBytes(Convert.FromBase64String(value));
    public static void Run(string name)
    {
        if (name == "TestSemanticError")
        {
            // errors_test.go TestSemanticError: existing helper terms only.
            Check.Equal(JsonTextErrors.SemanticError("marshal", "", "", "int", "", 0, null),
                "json: cannot marshal from Go int", name + " marshal int");
            Check.Equal(JsonTextErrors.SemanticError("unmarshal", "", "", "uint", "", 0, null),
                "json: cannot unmarshal into Go uint", name + " unmarshal uint");
            Check.Equal(JsonTextErrors.SemanticError("unmarshal", "object", "", "float64", "", 123, null),
                "json: cannot unmarshal JSON object into Go float64 after offset 123", name + " float64 offset");
            return;
        }
        if (name is "TestMarshal" or "TestUnmarshal") { RunArshal(name); return; }
        if (name is "TestEncoder" or "TestDecoder" or "TestDecoderErrors" or "TestEncoderErrors" or "TestCoderMaxDepth" or "TestPointer" or "TestObjectNamespace" or "TestValueMethods") { RunText(name); return; }
        var rows = name switch
        {
            "TestConsumeWhitespace" => TestConsumeWhitespaceRows,
            "TestConsumeLiteral" => TestConsumeLiteralRows,
            "TestConsumeString" => TestConsumeStringRows,
            "TestConsumeNumber" => TestConsumeNumberRows,
            "TestParseHexUint16" => TestParseHexUint16Rows,
            "TestAppendQuote" => TestAppendQuoteRows,
            "TestAppendNumber" => TestAppendNumberRows,
            "TestQuoteRune" => TestQuoteRuneRows,
            "TestTruncatePointer" => TestTruncatePointerRows,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            string input = Raw(row.Input), context = name + " row " + i;
            switch (name)
            {
                case "TestConsumeWhitespace": Check.Equal(JsonWire.ConsumeWhitespace(input), row.Count, context); break;
                case "TestConsumeLiteral": AssertWire(JsonWire.ConsumeLiteral(input, row.Literal!), row, context); break;
                case "TestConsumeString": AssertWire(JsonWire.ConsumeString(input), row, context); break;
                case "TestConsumeNumber": AssertWire(JsonWire.ConsumeNumber(input), row, context); break;
                case "TestParseHexUint16":
                    var hex = JsonWire.parseHexUint16(input);
                    Check.Equal(hex.Value, row.Count, context + " value");
                    Check.Equal(hex.Ok, row.Ok, context + " validity");
                    break;
                case "TestAppendQuote":
                    var quoted = JsonCodec.TryMarshal(input);
                    Check.Equal(quoted.Error?.Message, row.Error, context + " error");
                    Check.Equal(GoString.FromBytes(quoted.Bytes!), Raw(row.Want!), context + " bytes");
                    break;
                case "TestAppendNumber":
                    double number = BitConverter.Int64BitsToDouble(unchecked((long)Convert.ToUInt64(row.Bits!, 16)));
                    var encoded = JsonCodec.TryMarshal(number);
                    Check.True(encoded.Error is null, context + " error");
                    Check.Equal(GoString.FromBytes(encoded.Bytes!), row.Want, context + " float64 bytes");
                    break;
                case "TestQuoteRune": Check.Equal(JsonWire.QuoteRune(input), Raw(row.Want!), context); break;
                case "TestTruncatePointer": Check.Equal(JsonWire.TruncatePointer(input, 10), Raw(row.Want!), context); break;
            }
        }
    }
    private static void AssertWire(WireResult actual, Row expected, string context)
    {
        Check.Equal(actual.Consumed, expected.Count, context + " consumed prefix");
        Check.Equal(actual.UnexpectedEof ? "unexpected EOF" : actual.Error, expected.Error, context + " exact error");
    }
}
