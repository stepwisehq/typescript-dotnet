// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under Apache-2.0; original jsnum_test.go assertions ported with Node reference checks.
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Checking.Validation;
public static class CallableNumericChecks
{
    private sealed record Row(string Function, int Index, ulong X, ulong Y, ulong Want);
    private static readonly Lazy<List<Row>> rows = new(ReadRows);
    private static readonly Lazy<Dictionary<(string, int), ulong>> nodeResults = new(EvaluateNode);
    public static IEnumerable<string> Cases => rows.Value.SelectMany(row => new[] { "Original.Test" + row.Function + "[" + row.Index + "]", "Node.Test" + row.Function + "[" + row.Index + "]" });

    private static string FixtureText()
    {
        using var stream = typeof(CallableNumericChecks).Assembly.GetManifestResourceStream("TypeScript.Checking.CallableNumericFixtures.json") ?? throw new InvalidDataException("Missing original numeric fixture resource");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<Row> ReadRows()
    {
        using var json = JsonDocument.Parse(FixtureText());
        return json.RootElement.EnumerateArray().Select(row => new Row(row.GetProperty("function").GetString()!, row.GetProperty("index").GetInt32(), ParseBits(row.GetProperty("x").GetString()!), ParseBits(row.GetProperty("y").GetString()!), ParseBits(row.GetProperty("want").GetString()!))).ToList();
    }

    private static ulong ParseBits(string text) => ulong.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    private static Dictionary<(string, int), ulong> EvaluateNode()
    {
        const string script = """
            import fs from 'node:fs';
            function fromBits(text) { const buffer=new ArrayBuffer(8); const view=new DataView(buffer); view.setBigUint64(0,BigInt('0x'+text),true);return view.getFloat64(0,true); }
            function toBits(value) { const buffer=new ArrayBuffer(8);const view=new DataView(buffer);view.setFloat64(0,value,true);return view.getBigUint64(0,true).toString(16).padStart(16,'0'); }
            const output=JSON.parse(fs.readFileSync(0,'utf8')).map(row=>{
              const a=fromBits(row.x), b=fromBits(row.y); let result;
              switch(row.function) {
                case 'ToInt32': result=a|0;break;
                case 'BitwiseNOT': result=~a;break;
                case 'BitwiseAND': result=a&b;break;
                case 'BitwiseOR': result=a|b;break;
                case 'BitwiseXOR': result=a^b;break;
                case 'SignedRightShift': result=a>>b;break;
                case 'UnsignedRightShift': result=a>>>b;break;
                case 'LeftShift': result=a<<b;break;
                case 'Remainder': result=a%b;break;
                case 'Exponentiate': result=a**b;break;
                default:throw new Error('Unknown original operation');
              }
              return {function:row.function,index:row.index,bits:toBits(result)};
            }); console.log(JSON.stringify(output));
            """;
        using var process = new Process
        {
            StartInfo = new("node")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.StartInfo.ArgumentList.Add("--input-type=module");
        process.StartInfo.ArgumentList.Add("-e");
        process.StartInfo.ArgumentList.Add(script);
        if (!process.Start())
            throw new InvalidOperationException("Node is required for original numeric assertions");
        process.StandardInput.Write(FixtureText());
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Node numeric reference did not finish");
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException("Node original numeric reference failed: " + error);
        using var json = JsonDocument.Parse(output);
        var values = json.RootElement.EnumerateArray().ToDictionary(row => (row.GetProperty("function").GetString()!, row.GetProperty("index").GetInt32()), row => ParseBits(row.GetProperty("bits").GetString()!));
        if (values.Count != rows.Value.Count)
            throw new InvalidDataException("Incomplete original Node reference results");
        return values;
    }

    public static void Run(string name)
    {
        bool node = name.StartsWith("Node.", StringComparison.Ordinal);
        var row = rows.Value.Single(row => name == (node ? "Node.Test" : "Original.Test") + row.Function + "[" + row.Index + "]");
        var x = new JsNum.Number(BitConverter.UInt64BitsToDouble(row.X));
        var y = new JsNum.Number(BitConverter.UInt64BitsToDouble(row.Y));
        double actual = row.Function switch
        {
            "ToInt32" => x.ToInt32(),
            "BitwiseNOT" => x.BitwiseNOT().Value,
            "BitwiseAND" => x.BitwiseAND(y).Value,
            "BitwiseOR" => x.BitwiseOR(y).Value,
            "BitwiseXOR" => x.BitwiseXOR(y).Value,
            "SignedRightShift" => x.SignedRightShift(y).Value,
            "UnsignedRightShift" => x.UnsignedRightShift(y).Value,
            "LeftShift" => x.LeftShift(y).Value,
            "Remainder" => x.Remainder(y).Value,
            "Exponentiate" => x.Exponentiate(y).Value,
            _ => throw new InvalidDataException("Unknown original numeric assertion")};
        ulong wantedBits = node ? nodeResults.Value[(row.Function, row.Index)] : row.Want;
        double expected = BitConverter.UInt64BitsToDouble(wantedBits);
        bool equal = double.IsNaN(actual) || double.IsNaN(expected) ? double.IsNaN(actual) && double.IsNaN(expected) : actual == expected;
        if (!equal && node && row.Function == "Exponentiate" && !double.IsNaN(actual) && !double.IsNaN(expected))
        {
            ulong actualBits = BitConverter.DoubleToUInt64Bits(actual);
            equal = actualBits > wantedBits ? actualBits - wantedBits <= 1 : wantedBits - actualBits <= 1;
        }

        if (!equal)
            throw new InvalidOperationException(name + ": actual IEEE " + BitConverter.DoubleToUInt64Bits(actual).ToString("x16") + ", expected " + wantedBits.ToString("x16"));
    }

    public static List<UtilityObservation> RunAll()
    {
        List<UtilityObservation> observations = [];
        foreach (string name in Cases)
        {
            Run(name);
            observations.Add(new() { Name = name, BoolValue = true });
        }

        return observations;
    }
}
