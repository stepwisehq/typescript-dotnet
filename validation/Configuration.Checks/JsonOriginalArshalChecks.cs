// Copyright 2023 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Selected default static raw-value domains from v2/arshal_test.go.
using TypeScript.Collections;
using TypeScript.Json;
using TypeScript.Scanner;
namespace Configuration.Checks;
public static partial class JsonOriginalChecks
{
    private sealed record ArshalRow(string Name, object? Value, string Input, string Want, string Error);
    private static void RunArshal(string name)
    {
        var rows = name == "TestMarshal" ? TestMarshalRows : TestUnmarshalRows;
        foreach (var row in rows)
        {
            string context = name + " " + row.Name;
            if (name == "TestMarshal")
            {
                var result = JsonCodec.TryMarshal(row.Value);
                Check.Equal(result.Error?.Message ?? "", GoString.ToString(Raw(row.Error)), context + " error");
                Check.Equal(GoString.FromBytes(result.Bytes!), Raw(row.Want), context + " partial/full bytes");
            }
            else
            {
                var result = JsonCodec.Parse(Raw(row.Input));
                Check.Equal(result.Error?.Message ?? "", GoString.ToString(Raw(row.Error)), context + " exact error");
                EqualRaw(result.Value, row.Value, context + " partial/full value");
            }
        }
    }
    private static void EqualRaw(object? actual, object? expected, string context)
    {
        if (expected is List<object?> list)
        {
            Check.True(actual is List<object?>, context + " list shape");
            var value = (List<object?>)actual!;
            Check.Equal(value.Count, list.Count, context + " length");
            for (int i = 0; i < list.Count; i++) EqualRaw(value[i], list[i], context + "[" + i + "]");
        }
        else if (expected is Dictionary<string, object?> fields)
        {
            Check.True(actual is OrderedMap<string, object?>, context + " map shape");
            var value = (OrderedMap<string, object?>)actual!;
            Check.Equal(value.Size(), fields.Count, context + " count");
            foreach (var field in fields)
            {
                var (item, present) = value.Get(field.Key);
                Check.True(present, context + " key membership");
                EqualRaw(item, field.Value, context + "[" + field.Key + "]");
            }
        }
        else Check.Equal(actual, expected, context);
    }
}
