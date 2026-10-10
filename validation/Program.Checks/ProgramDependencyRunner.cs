// Finite direct dependency API observer. Go-only typed error internals are
// retained by the reference observer as diagnostic metadata; exact rendering,
// actual partial values, ordered declared options and hashes are compared.
using System.Globalization;
using System.Text.Json.Nodes;
using TypeScript.Collections;
using TypeScript.ContentMapper;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using RawValue = TypeScript.Json.JsonValue;

namespace Program.Checks;

public static partial class ProgramRunner
{
    private static JsonNode? DependencyError(Exception? error) => error is null ? null : new JsonObject { ["textBase64"] = Bytes(error.Message) };

    private static JsonObject ObserveDependency(string name, JsonObject input)
    {
        Keys(input, "Kind", "JSONBase64", "MapperName", "MapperVersion", "ContributionID", "MapperOptionsBase64", "DeclaredOptions", "CompilerOptionsJSONBase64");
        string kind = input["Kind"]!.GetValue<string>();
        var result = new JsonObject { ["name"] = name, ["dependencyKind"] = kind };
        if (kind == "int32-matrix")
        {
            var (matrix, error) = JsonCodec.UnmarshalInt32Matrix(Convert.FromBase64String(input["JSONBase64"]!.GetValue<string>()));
            result["value"] = Value(matrix);
            result["error"] = DependencyError(error);
        }
        else if (kind == "spanmap-unmarshal")
        {
            var (map, error) = SpanMaps.Unmarshal(Convert.FromBase64String(input["JSONBase64"]!.GetValue<string>()));
            result["error"] = DependencyError(error);
            result["mapPresent"] = map is not null;
            result["segments"] = map.Segments() is { } segments ? new JsonArray(segments.Select(segment => (JsonNode)new JsonObject
            {
                ["VirtualStart"] = segment.VirtualStart, ["VirtualEnd"] = segment.VirtualEnd, ["OriginalStart"] = segment.OriginalStart,
                ["OriginalEnd"] = segment.OriginalEnd, ["Kind"] = (int)segment.Kind, ["Features"] = (int)segment.Features
            }).ToArray()) : null;
            result["marshalBase64"] = null;
            result["marshalError"] = null;
            if (map is not null)
            {
                var (bytes, marshalError) = map.Marshal();
                result["marshalBase64"] = Convert.ToBase64String(bytes ?? []);
                result["marshalError"] = DependencyError(marshalError);
            }
        }
        else if (kind == "mapper-identity")
        {
            CompilerOptions? options = null;
            if (input["CompilerOptionsJSONBase64"] is JsonNode optionBytes)
            {
                options = new();
                var (raw, error) = JsonCodec.Parse(GoString.FromBytes(Convert.FromBase64String(optionBytes.GetValue<string>())));
                if (error is not null) throw error;
                // Finite source-struct fixture adaptation: the actual Go JSON
                // alias decoder stores a path string without config Finalize.
                // Remaining supplied fields use their genuine option parsers.
                foreach (var (key, item) in ((OrderedMap<string, object?>)raw!).Entries())
                {
                    if (key == "baseUrl") options.BaseUrl = RootedDirectoryPath.FromGoString((string)item!);
                    else TypeScript.TsOptions.TsOptions.ParseCompilerOptions(key, item, options);
                }
            }
            var mapper = new Mapper { Name = Input(input, "MapperName"), Version = Input(input, "MapperVersion"), ContributionID = Input(input, "ContributionID") };
            mapper.CompilerOptions = ((JsonArray?)input["DeclaredOptions"])?.Select(value => GoString.ToUtf8(value!.GetValue<string>())).ToList();
            if (input["MapperOptionsBase64"] is JsonNode rawMapper) mapper.Options = new RawValue(Convert.FromBase64String(rawMapper.GetValue<string>()));
            var (declared, declaredError) = mapper.MarshalDeclaredOptions(options);
            JsonArray? entries = declared is null ? null : [];
            if (declared is not null)
                foreach (var (key, raw) in declared.Entries())
                    entries!.Add((JsonNode)new JsonObject { ["nameBase64"] = Bytes(key), ["rawBase64"] = Convert.ToBase64String(raw.Bytes!) });
            var (encoded, marshalError) = JsonCodec.TryMarshal(declared);
            UInt128 hash = mapper.TransformIdentity(options);
            result["optionsError"] = null;
            result["declaredError"] = DependencyError(declaredError);
            result["declared"] = entries;
            result["declaredJSONBase64"] = Convert.ToBase64String(encoded ?? []);
            result["marshalError"] = DependencyError(marshalError);
            result["hashHi"] = ((ulong)(hash >> 64)).ToString(CultureInfo.InvariantCulture);
            result["hashLo"] = ((ulong)hash).ToString(CultureInfo.InvariantCulture);
            result["identityBase64"] = Bytes(mapper.Identity());
        }
        else throw new ArgumentException("Unknown Program dependency probe: " + kind);
        return result;
    }
}
