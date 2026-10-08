// Copyright (c) Microsoft Corporation. All rights reserved.
// Apache-2.0: licenses/TypeScript-LICENSE.txt.
// Assertion-preserving port of pinned jsnum/string_test.go and pseudobigint_test.go.
// Upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static partial class ScannerChecks
{
    private readonly record struct JsResult(double Number, string Text);
    private static readonly Lazy<JsResult[]> originalJsResults = new(() => EvaluateWithNode(NumberFixtures.Format.Concat(NumberFixtures.Parse).ToArray()));
    private static readonly Lazy<JsResult[]> formatSeedJsResults = new(() => EvaluateWithNode(NumberFixtures.Format.Concat(NumberFixtures.Parse)
        .Select(row => new NumberFixtures.Row(row.Bits, new JsNum.Number(FromBits(row.Bits)).String())).ToArray()));

    private static void AddNumberChecks(Dictionary<string, Action> cases)
    {
        for (int i = 0; i < NumberFixtures.Format.Length; i++)
        {
            var row = NumberFixtures.Format[i];
            int index = i;
            double number = FromBits(row.Bits);
            string id = i.ToString("D3", CultureInfo.InvariantCulture) + "/" + row.Text;
            cases.Add("TestString/" + id, () => Equal(row.Text, new JsNum.Number(number).String(), "Number.String IEEE " + row.Bits.ToString("x16")));
            cases.Add("TestFromString/stringTests/" + id, () =>
            {
                EqualNumber(number, ParseNumber(row.Text), "FromString plain");
                EqualNumber(number, ParseNumber(row.Text + " "), "FromString trailing space");
                EqualNumber(number, ParseNumber(" " + row.Text), "FromString leading space");
            });
            cases.Add("TestStringRoundtrip/" + id, () => Equal(row.Text, JsNum.FromString(GoString.ToUtf8(row.Text)).String(), "String roundtrip"));
            cases.Add("TestStringJS/stringTests/" + id, () =>
            {
                var result = originalJsResults.Value[index];
                EqualNumber(number, result.Number, "Node parses expected string");
                Equal(row.Text, result.Text, "Node formats original number");
            });
        }
        for (int i = 0; i < NumberFixtures.Parse.Length; i++)
        {
            var row = NumberFixtures.Parse[i];
            int index = NumberFixtures.Format.Length + i;
            double number = FromBits(row.Bits);
            string id = i.ToString("D3", CultureInfo.InvariantCulture) + "/" + row.Text;
            cases.Add("TestFromString/fromStringTests/" + id, () => EqualNumber(number, ParseNumber(row.Text), "FromString extra input"));
            cases.Add("TestStringJS/fromStringTests/" + id, () => EqualNumber(number, originalJsResults.Value[index].Number, "Node parses extra input"));
        }
        var seeds = NumberFixtures.Format.Concat(NumberFixtures.Parse).ToArray();
        for (int i = 0; i < seeds.Length; i++)
        {
            var row = seeds[i];
            int index = i;
            string id = i.ToString("D3", CultureInfo.InvariantCulture);
            cases.Add("FuzzStringJS/seed/" + id, () =>
            {
                double number = FromBits(row.Bits);
                var results = formatSeedJsResults.Value;
                Equal(seeds.Length, results.Length, "Node batch preserves one output per original singleton seed");
                Equal(new JsNum.Number(number).String(), results[index].Text, "Seed Number.String matches Node");
                EqualNumber(number, results[index].Number, "Seed formatted string roundtrips through Node");
            });
            cases.Add("FuzzFromStringJS/seed/" + id, () =>
            {
                if (Encoding.UTF8.GetByteCount(row.Text) > 350)
                    throw new InvalidOperationException("Original fuzz seed unexpectedly exceeds its 350-byte skip boundary");
                var results = originalJsResults.Value;
                Equal(seeds.Length, results.Length, "Node batch preserves one output per original singleton seed");
                EqualNumber(ParseNumber(row.Text), results[index].Number, "Seed FromString matches Node");
            });
        }
        AddBigIntChecks(cases);
    }

    private static void AddBigIntChecks(Dictionary<string, Action> cases)
    {
        var numbers = new List<long>();
        for (long i = 0; i < 1000; i++) numbers.Add(i);
        for (int bits = 0; bits < 53; bits++)
        {
            numbers.Add(1L << bits);
            numbers.Add((1L << bits) - 1);
        }
        for (int i = 0; i < numbers.Count; i++)
        {
            long number = numbers[i];
            cases.Add("TestParsePseudoBigInt/strip base10/" + i.ToString("D4", CultureInfo.InvariantCulture), () =>
            {
                string expected = new JsNum.Number(number).String();
                for (int leadingZeros = 0; leadingZeros < 10; leadingZeros++)
                    Equal(expected, JsNum.ParsePseudoBigInt(new string('0', leadingZeros) + expected + "n"), "BigInt decimal zero stripping");
            });
        }
        (string Literal, string Output)[] small =
        [
            ("0b0n", "0"), ("0b1n", "1"), ("0b1010n", "10"), ("0b1010_0101n", "165"), ("0B1101n", "13"),
            ("0o0n", "0"), ("0o7n", "7"), ("0o755n", "493"), ("0o7_5_5n", "493"), ("0O12n", "10"),
            ("0x0n", "0"), ("0xFn", "15"), ("0xFFn", "255"), ("0xF_Fn", "255"), ("0X1Fn", "31")
        ];
        foreach (var row in small)
            cases.Add("TestParsePseudoBigInt/small/" + row.Literal, () => Equal(row.Output, JsNum.ParsePseudoBigInt(row.Literal), row.Literal));
        string[] large =
        [
            "123456789012345678901234567890n",
            "0b1100011101110100100001111111101101100001101110011111000001110111001001110001111110000101011010010n",
            "0o143564417755415637016711617605322n",
            "0x18ee90ff6c373e0ee4e3f0ad2n"
        ];
        for (int i = 0; i < large.Length; i++)
        {
            string literal = large[i];
            cases.Add("TestParsePseudoBigInt/large/" + i, () => Equal("123456789012345678901234567890", JsNum.ParsePseudoBigInt(literal), literal));
        }
    }

    private static double FromBits(ulong bits) => BitConverter.UInt64BitsToDouble(bits);
    private static double ParseNumber(string text) => JsNum.FromString(GoString.ToUtf8(text)).Value;
    // Upstream assertEqualNumber treats NaN explicitly; ordinary equality intentionally treats +/-0 equally.
    private static void EqualNumber(double expected, double actual, string context)
    {
        if (double.IsNaN(expected) ? !double.IsNaN(actual) : actual != expected)
            throw new InvalidOperationException($"{context}: expected IEEE {BitConverter.DoubleToUInt64Bits(expected):x16}, actual {BitConverter.DoubleToUInt64Bits(actual):x16}");
    }

    private static JsResult[] EvaluateWithNode(NumberFixtures.Row[] rows)
    {
        const string script = "let s='';process.stdin.setEncoding('utf8');process.stdin.on('data',x=>s+=x);process.stdin.on('end',()=>{const a=JSON.parse(s);const b=new ArrayBuffer(8),v=new DataView(b);function number(h){v.setBigUint64(0,BigInt('0x'+h),true);return v.getFloat64(0,true);}function bits(n){v.setFloat64(0,n,true);return v.getBigUint64(0,true).toString(16).padStart(16,'0');}process.stdout.write(JSON.stringify(a.map(x=>[bits(+x[1]),''+number(x[0])])));});";
        var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-e");
        start.ArgumentList.Add(script);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Original JS assertions require Node.js");
        using var input = new MemoryStream();
        using (var json = new Utf8JsonWriter(input))
        {
            json.WriteStartArray();
            foreach (var row in rows)
            {
                json.WriteStartArray();
                json.WriteStringValue(row.Bits.ToString("x16", CultureInfo.InvariantCulture));
                json.WriteStringValue(row.Text);
                json.WriteEndArray();
            }
            json.WriteEndArray();
        }
        process.StandardInput.Write(Encoding.UTF8.GetString(input.ToArray()));
        process.StandardInput.Close();
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Node.js foundation assertions timed out");
        }
        string output = outputTask.GetAwaiter().GetResult();
        string error = errorTask.GetAwaiter().GetResult();
        Equal(0, process.ExitCode, "Node.js exit code: " + error);
        using var document = JsonDocument.Parse(output);
        Equal(rows.Length, document.RootElement.GetArrayLength(), "Node.js result count");
        return document.RootElement.EnumerateArray().Select(row => new JsResult(
            FromBits(ulong.Parse(row[0].GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture)), row[1].GetString()!)).ToArray();
    }
}
