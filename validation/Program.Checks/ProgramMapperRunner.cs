// Explicit Project callback responses, using original public parsed mapper
// configuration and genuine host TransformAndParse. Subprocess runtime is absent.
using System.Text.Json.Nodes;
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.TsOptions;

namespace Program.Checks;

public static partial class ProgramRunner
{
    private static IProject? ConfigureMapperProject(ParsedCommandLine? parsed, JsonObject? input)
    {
        if (parsed is null || input is null) return null;
        Keys(input, "Text", "VirtualExtension", "MappingJSONBase64", "NilMappings", "Supplemental", "ErrorKind", "ProjectErrorKind", "ErrorText", "OptionDiagnostic");
        var mapper = new Mapper { Package = "vue", Extensions = [".vue"], Name = "authored-mapper", Version = "1.0.0" };
        parsed.ParsedConfig.ContentMappers = [mapper];
        return new FixtureMapperProject(input, mapper);
    }

    private sealed class FixtureMapperProject(JsonObject input, Mapper mapper) : IProject
    {
        public Exception? Refresh() => null;
        public (List<string>?, Exception?) Identities() => (null, null);
        public (string, Exception?) Identity(Mapper value) => ("fixture-identity", null);
        public (List<RootedFilePath>?, Exception?) WatchedFiles() => (null, null);
        public Exception? Close() => null;
        public List<OptionDiagnostic>? Diagnostics() => input["OptionDiagnostic"]?.GetValue<bool>() == true
            ? [new() { Mapper = mapper, Source = "fixture", Code = 9001, MessageText = "Authored mapper option diagnostic." }]
            : null;

        private static MappedResult ReadResult(JsonObject value)
        {
            TypeScript.SpanMap.SpanMap? map = null;
            if (value["NilMappings"]?.GetValue<bool>() != true)
            {
                map = SpanMaps.New(null);
                if (value["MappingJSONBase64"] is JsonValue raw)
                {
                    var (actual, error) = SpanMaps.Unmarshal(Convert.FromBase64String(raw.GetValue<string>()));
                    if (error is not null) throw error;
                    map = actual;
                }
            }
            return new() { Text = Input(value, "Text"), VirtualExtension = Input(value, "VirtualExtension"), Mappings = map };
        }

        public (Result, Exception?) Transform(Mapper value, Request request)
        {
            if (input["ProjectErrorKind"] is JsonValue projectKind)
                return (default, new ProjectError { Kind = (ProjectErrorKind)projectKind.GetValue<byte>() });
            if (input["ErrorKind"] is JsonValue kind)
                return (default, new TransformError((TransformErrorKind)kind.GetValue<byte>(), new InvalidOperationException(Input(input, "ErrorText"))));
            var main = ReadResult(input);
            List<MappedResult>? supplemental = null;
            if (input["Supplemental"] is JsonArray children)
            {
                supplemental = [];
                foreach (JsonObject child in children.Cast<JsonObject>())
                {
                    Keys(child, "Text", "VirtualExtension", "MappingJSONBase64", "NilMappings");
                    supplemental.Add(ReadResult(child));
                }
            }
            return (new() { Text = main.Text, VirtualExtension = main.VirtualExtension, Mappings = main.Mappings, Supplemental = supplemental }, null);
        }
    }

    private static JsonNode? MappedSource(Observer observer, SourceFile file)
    {
        if (file.ContentMapper() == "") return null;
        string? mapBytes = null;
        if (file.SpanMap() is { } map)
        {
            var (data, error) = map.Marshal();
            if (error is not null) throw error;
            mapBytes = Convert.ToBase64String(data!);
        }
        JsonArray? supplemental = null;
        if (file.SupplementalSourceFiles() is { } children)
        {
            supplemental = [];
            foreach (var child in children) supplemental.Add((JsonNode?)JsonValue.Create(observer.File(child)));
        }
        return new JsonObject { ["mapperBase64"] = Bytes(file.ContentMapper()), ["originalTextBase64"] = Bytes(file.OriginalText()),
            ["originalFileNameBase64"] = Bytes(file.OriginalFileName().AsString()), ["identityBase64"] = Bytes(file.ContentMapperTransformIdentity()),
            ["failureStub"] = file.IsContentMapperFailureStub(), ["mapBase64"] = mapBytes,
            ["canonical"] = JsonValue.Create(observer.File(file.CanonicalSourceFile())), ["supplemental"] = supplemental };
    }
}
