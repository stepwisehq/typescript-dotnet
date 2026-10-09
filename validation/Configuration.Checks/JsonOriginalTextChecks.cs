// Copyright 2023 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Selected whole-value assertions, with stream/storage omissions in provenance.
using TypeScript.Json;
using TypeScript.Json.Internal;
using TypeScript.Scanner;

namespace Configuration.Checks;
public static partial class JsonOriginalChecks
{
    private sealed record ExtraRow(string Input, string Want, bool Ok);
    private static void RunText(string name)
    {
        var rows = name switch
        {
            "TestEncoder" => TestEncoderRows,
            "TestDecoder" => TestDecoderRows,
            "TestDecoderErrors" => TestDecoderErrorsRows,
            "TestEncoderErrors" => TestEncoderErrorsRows,
            "TestCoderMaxDepth" => TestCoderMaxDepthRows,
            "TestPointer" => TestPointerRows,
            "TestObjectNamespace" => TestObjectNamespaceRows,
            "TestValueMethods" => TestValueMethodsRows,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
        if (name == "TestObjectNamespace")
        {
            // The source tests both fresh and reset state. Each whole-value
            // validation starts with an actual fresh decoder namespace.
            for (int pass = 0; pass < 2; pass++)
            {
                var names = new List<string>();
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    string quoted = Raw(row.Input);
                    if (quoted == "")
                    {
                        names.RemoveAt(names.Count - 1);
                        continue;
                    }

                    string value = "{" + string.Join(",", names.Append(quoted).Select(n => n + ":null")) + "}";
                    var error = JsonCodec.ValidateValue(new JsonValue(GoString.Bytes(value)));
                    Check.Equal(error is null, row.Ok, name + " insertion " + i);
                    if (row.Ok)
                        names.Add(quoted);
                }
            }

            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            string input = Raw(row.Input), context = name + " row " + i;
            if (name == "TestPointer")
            {
                int delimiter = input.IndexOf('\0');
                Check.Equal(JsonTextErrors.AppendPointer(input[..delimiter], input[(delimiter + 1)..]), Raw(row.Want), context + " append roundtrip");
                continue;
            }

            if (name == "TestEncoder")
            {
                var parsed = JsonCodec.Parse(input);
                Check.True(parsed.Error is null, context + " source-valid primitive");
                var encoded = JsonCodec.TryMarshal(parsed.Value);
                Check.True(encoded.Error is null, context + " encode error");
                Check.Equal(GoString.FromBytes(encoded.Bytes!), Raw(row.Want), context + " source compact bytes");
                continue;
            }

            var raw = new JsonValue(GoString.Bytes(input));
            var error = name == "TestEncoderErrors" ? JsonCodec.FormatRawValue(raw).Error : JsonCodec.ValidateValue(raw);
            Check.Equal(error is null, row.Ok, context + " validity");
            if (row.Want != "")
                Check.Equal(error?.Message, GoString.ToString(Raw(row.Want)), context + " original exact error");
        }
    }
}
