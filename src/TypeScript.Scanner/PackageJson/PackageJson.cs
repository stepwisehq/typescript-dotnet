// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/packagejson/packagejson.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Parse projects only the demanded
// imports field; unselected header/dependency/version/content-mapper APIs omitted.
using System.Text.Json;

namespace TypeScript.PackageJson;
internal static class PackageJsonParser
{
    internal static PackageJson Parse(byte[] bytes)
    {
        var result = new PackageJson();
        try
        {
            using var json = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = int.MaxValue });
            // Original struct unmarshalling accepts null and ignores unknown fields,
            // accepts duplicate names, and rejects other top-level value kinds.
            if (json.RootElement.ValueKind == JsonValueKind.Null)
            {
                result.Parseable = true;
                return result;
            }

            if (json.RootElement.ValueKind != JsonValueKind.Object)
                return result;
            foreach (var field in json.RootElement.EnumerateObject())
                if (field.Name == "imports")
                    result.Imports = field.Value.Clone();
            result.Parseable = true;
        }
        catch (JsonException)
        {
        }

        return result;
    }
}
