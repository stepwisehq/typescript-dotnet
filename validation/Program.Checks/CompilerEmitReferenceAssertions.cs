// Shared original-reference assertions for xUnit and the actual corpus consumers.
using System.Text.Json.Nodes;

namespace Program.Checks;
public static class CompilerEmitReferenceAssertions
{
    public static void Check(JsonObject expected, JsonObject actual, JsonObject? supplemental = null)
    {
        string name = expected["name"]!.GetValue<string>();
        void Equal(JsonNode? left, JsonNode? right, string field)
        {
            if (!JsonNode.DeepEquals(left, right))
                throw new InvalidOperationException("Original emit reference differs: " + name + "/" + field);
        }

        foreach (string field in new[]
        {
            "sourceSha256",
            "configuration",
            "diagnostics",
            "owners",
            "emitResult",
            "js",
            "dts",
            "maps"
        })
            Equal(expected[field], actual[field], field);
        if (expected["status"]!.GetValue<string>() != "passed-original-full-harness")
            throw new InvalidOperationException("Original full-harness pass required for shared original assertions: " + name);
        if (actual["pendingOriginalChannels"]!.AsArray().Count != 0)
            throw new InvalidOperationException("Original assertion implementation remains pending: " + name);
        foreach (var channel in expected["channels"]!.AsObject())
        {
            var original = channel.Value!.AsObject();
            var managed = actual["channels"]![channel.Key]!.AsObject();
            Equal(original["status"], managed["status"], channel.Key + "/status");
            if (managed["status"]!.GetValue<string>() != "passed-original-assertions")
                continue; // Exact original harness-only skip is retained above.
            var baselines = original["baselines"]!.AsArray();
            if (baselines.Count == 0 && channel.Key == "sourceMapRecord")
            {
                if (supplemental is null || !JsonNode.DeepEquals(expected["sourceSha256"], supplemental["sourceSha256"]) || !JsonNode.DeepEquals(expected["configuration"], supplemental["configuration"]))
                    throw new InvalidOperationException("Exact supplemental original record identity is required: " + name);
                baselines = supplemental["channels"]![channel.Key]!["baselines"]!.AsArray();
            }

            HashSet<string> observedFields = new(StringComparer.Ordinal);
            foreach (var record in baselines)
            {
                string path = record!["path"]!.GetValue<string>();
                string field = channel.Key switch
                {
                    "diagnostics" => "errorBaselineBase64",
                    "contentMapper" => "contentMapperBaselineBase64",
                    "output" => "javaScriptBaselineBase64",
                    "sourceMap" => "sourceMapBaselineBase64",
                    "sourceMapRecord" => "sourceMapRecordBaselineBase64",
                    "typesAndSymbols" => path.EndsWith(".types", StringComparison.Ordinal) ? "typesBase64" : path.EndsWith(".symbols", StringComparison.Ordinal) ? "symbolsBase64" : throw new InvalidOperationException("Unexpected original type/symbol baseline path: " + path),
                    "moduleResolution" => "moduleResolutionBaselineBase64",
                    _ => throw new InvalidOperationException("Unexpected original baseline channel: " + channel.Key)};
                observedFields.Add(field);
                Equal(record["contentBase64"], actual[field], channel.Key + "/" + path);
            }
            string[] possibleFields = channel.Key switch
            {
                "diagnostics" => ["errorBaselineBase64"],
                "contentMapper" => ["contentMapperBaselineBase64"],
                "output" => ["javaScriptBaselineBase64"],
                "sourceMap" => ["sourceMapBaselineBase64"],
                "sourceMapRecord" => ["sourceMapRecordBaselineBase64"],
                "typesAndSymbols" => ["typesBase64", "symbolsBase64"],
                "moduleResolution" => ["moduleResolutionBaselineBase64"],
                "unionOrdering" or "parentPointers" => [],
                _ => throw new InvalidOperationException("Unexpected original assertion channel: " + channel.Key)
            };
            foreach (string field in possibleFields)
                if (!observedFields.Contains(field) && actual[field] is not null)
                    throw new InvalidOperationException("Managed wrote a baseline absent in original channel: " + name + "/" + field);
        }

        foreach (string invariant in new[]
        {
            "parentPointersBeforeEmit",
            "parentPointersAfterEmit",
            "unionOrderingBeforeEmit",
            "unionOrderingAfterEmit"
        })
            if (actual["originalAssertions"]![invariant]!.GetValue<bool>() != true)
                throw new InvalidOperationException("Original emit invariant failed: " + name + "/" + invariant);
    }
}
