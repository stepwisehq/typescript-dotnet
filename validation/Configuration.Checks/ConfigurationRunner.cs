// Authored standalone metadata projection. Static serialization supports Native
// AOT; unknown stored shapes fail. No Program or Checker is instantiated.
using System.Text.Json.Nodes;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using T = TypeScript.TsOptions.TsOptions;
using S = TypeScript.Semver.Semver;

namespace Configuration.Checks;
public static class ConfigurationRunner
{
    private static string Bytes(string raw) => Convert.ToBase64String(GoString.Bytes(raw));
    private static string Input(JsonObject request, string scalar, string bytes)
    {
        if (request[bytes] is JsonValue encoded)
            return GoString.FromBytes(Convert.FromBase64String(encoded.GetValue<string>()));
        return request[scalar] is JsonValue value ? GoString.ToUtf8(value.GetValue<string>()) : "";
    }

    public static string Run(string line)
    {
        var request = JsonNode.Parse(line) as JsonObject ?? throw new InvalidOperationException("Request must be an object.");
        string mode = request["mode"]!.GetValue<string>();
        JsonObject result = new()
        {
            ["name"] = request["name"]!.GetValue<string>(),
            ["mode"] = mode
        };
        if (mode is "versions" or "ranges" or "locales")
        {
            JsonArray records = [];
            bool encoded = request["argumentsBase64"] is not null;
            var arguments = (JsonArray? )(request[encoded ? "argumentsBase64" : "arguments"]) ?? [];
            foreach (JsonNode? item in arguments)
            {
                string arg = encoded ? GoString.FromBytes(Convert.FromBase64String(item!.GetValue<string>())) : GoString.ToUtf8(item!.GetValue<string>());
                if (mode == "locales")
                {
                    var(locale, ok) = TypeScript.Locale.Locales.Parse(arg);
                    records.Add((JsonNode)new JsonObject { ["inputBase64"] = Bytes(arg), ["parsed"] = ok, ["stringBase64"] = Bytes(locale.String()) });
                }
                else if (mode == "versions")
                {
                    var(version, error) = S.TryParseVersion(arg);
                    records.Add((JsonNode)new JsonObject { ["inputBase64"] = Bytes(arg), ["stringBase64"] = Bytes(version.String()), ["errorBase64"] = error is null ? null : Bytes(error.Message) });
                }
                else
                {
                    var(range, ok) = S.TryParseVersionRange(arg);
                    records.Add((JsonNode)new JsonObject { ["inputBase64"] = Bytes(arg), ["parsed"] = ok, ["stringBase64"] = Bytes(range.String()) });
                }
            }

            result["records"] = records;
        }
        else if (mode == "text")
        {
            string config = Input(request, "configPath", "configPathBase64");
            if (config == "")
                config = "/project/tsconfig.json";
            int sensitivity = request["caseSensitivity"]?.GetValue<int>() ?? 1;
            if (sensitivity is not (0 or 1))
                throw new InvalidOperationException("Unknown case policy.");
            var file = RootedFilePath.FromGoString(config);
            var(value, errors) = T.ParseConfigFileTextToJson(file, ((CaseSensitivity)sensitivity).PathKey(file.AsPath()), Input(request, "text", "textBase64"));
            Observer observer = new();
            result["raw"] = Value(value);
            result["errors"] = observer.Diagnostics(errors);
            result["diagnosticFiles"] = observer.Files;
            if (request["formatNewLine"] is JsonValue newline)
            {
                string directory = Input(request, "baseDirectory", "baseDirectoryBase64");
                if (directory == "")
                    directory = TsPath.GetDirectoryPath(config);
                var options = new TypeScript.DiagnosticWriter.FormattingOptions
                {
                    NewLine = GoString.ToUtf8(newline.GetValue<string>()),
                    CurrentDirectory = RootedDirectoryPath.FromGoString(directory),
                    CaseSensitivity = (CaseSensitivity)sensitivity
                };
                var diagnostics = TypeScript.DiagnosticWriter.DiagnosticWriter.FromASTDiagnostics(errors, configFileNames: true);
                using StringWriter plain = new(), pretty = new(), summary = new();
                TypeScript.DiagnosticWriter.DiagnosticWriter.WriteFormatDiagnostics(plain, diagnostics, options);
                TypeScript.DiagnosticWriter.DiagnosticWriter.FormatDiagnosticsWithColorAndContext(pretty, diagnostics, options);
                TypeScript.DiagnosticWriter.DiagnosticWriter.WriteErrorSummaryText(summary, diagnostics, options);
                result["formatted"] = new JsonObject
                {
                    ["plainBase64"] = Bytes(plain.ToString()),
                    ["prettyBase64"] = Bytes(pretty.ToString()),
                    ["summaryBase64"] = Bytes(summary.ToString())
                };
            }
        }
        else if (mode == "raw-options")
        {
            RawCompilerOptions raw = new();
            Observer observer = new();
            string baseDirectory = Input(request, "baseDirectory", "baseDirectoryBase64");
            if (baseDirectory == "")
                baseDirectory = "/project";
            JsonArray records = [];
            bool encoded = request["argumentsBase64"] is not null;
            foreach (JsonNode? item in request[encoded ? "argumentsBase64" : "arguments"] as JsonArray ?? [])
            {
                string arg = encoded ? GoString.FromBytes(Convert.FromBase64String(item!.GetValue<string>())) : GoString.ToUtf8(item!.GetValue<string>());
                Exception? error = raw.UnmarshalJSON(arg);
                byte[] serialized;
                Exception? marshalError = null;
                try
                {
                    serialized = raw.MarshalJSON();
                }
                catch (Exception failure)
                {
                    serialized = [];
                    marshalError = failure;
                }

                var(options, diagnostics) = raw.Finalize(RootedDirectoryPath.FromGoString(baseDirectory));
                records.Add((JsonNode)new JsonObject { ["inputBase64"] = Bytes(arg), ["errorBase64"] = error is null ? null : Bytes(GoString.ToUtf8(error.Message)), ["marshaledBase64"] = Convert.ToBase64String(serialized), ["marshalErrorBase64"] = marshalError is null ? null : Bytes(GoString.ToUtf8(marshalError.Message)), ["options"] = Value(options), ["diagnostics"] = observer.Diagnostics(diagnostics) });
            }

            result["records"] = records;
            result["diagnosticFiles"] = observer.Files;
        }
        else if (mode == "marshal")
        {
            object? value = ConstructMarshalValue(request);
            var(bytes, error) = TypeScript.Json.JsonCodec.TryMarshal(value);
            result["marshaledBase64"] = Convert.ToBase64String(bytes ?? []);
            result["errorBase64"] = error is null ? null : Bytes(GoString.ToUtf8(error.Message));
        }
        else if (mode == "mapper-equality")
        {
            var terms = Decode(request["value"]) as List<object?> ?? throw new InvalidOperationException("Mapper equality requires a list.");
            if (terms.Count != 2)
                throw new InvalidOperationException("Mapper equality requires two terms.");
            var left = new TypeScript.ContentMapper.Mapper
            {
                Options = (TypeScript.Json.JsonValue? )terms[0]
            };
            var right = new TypeScript.ContentMapper.Mapper
            {
                Options = (TypeScript.Json.JsonValue? )terms[1]
            };
            result["equal"] = left.Equals(right);
            result["leftOptionsNil"] = left.Options?.Bytes is null;
            result["rightOptionsNil"] = right.Options?.Bytes is null;
            result["leftLength"] = left.Options?.Bytes?.Length ?? 0;
            result["rightLength"] = right.Options?.Bytes?.Length ?? 0;
        }
        else if (mode == "raw-json")
        {
            var(value, error) = TypeScript.Json.JsonCodec.Parse(Input(request, "text", "textBase64"));
            var(bytes, marshalError) = TypeScript.Json.JsonCodec.TryMarshal(value);
            result["errorBase64"] = error is null ? null : Bytes(GoString.ToUtf8(error.Message));
            result["marshaledBase64"] = Convert.ToBase64String(bytes ?? []);
            result["marshalErrorBase64"] = marshalError is null ? null : Bytes(GoString.ToUtf8(marshalError.Message));
        }
        else if (mode == "name-map")
        {
            string constructor = request["constructor"]!.GetValue<string>();
            if (constructor is not ("nil" or "empty"))
                throw new InvalidOperationException("Unknown empty-name-map constructor.");
            var names = T.GetNameMapFromList(constructor == "nil" ? null : []);
            JsonArray records = [];
            foreach (var arg in request["arguments"] as JsonArray ?? [])
            {
                string name = GoString.ToUtf8(arg!.GetValue<string>());
                records.Add((JsonNode)new JsonObject { ["nameBase64"] = Bytes(name), ["getIsNil"] = names.Get(name)is null, ["shortIsNil"] = names.GetFromShort(name)is null, ["longIsNil"] = names.GetOptionDeclarationFromName(name, false)is null, ["withShortIsNil"] = names.GetOptionDeclarationFromName(name, true)is null });
            }

            result["records"] = records;
        }
        else if (mode == "package-json")
        {
            JsonArray records = [];
            bool encoded = request["argumentsBase64"] is not null;
            foreach (JsonNode? item in request[encoded ? "argumentsBase64" : "arguments"] as JsonArray ?? [])
            {
                string arg = encoded ? GoString.FromBytes(Convert.FromBase64String(item!.GetValue<string>())) : GoString.ToUtf8(item!.GetValue<string>());
                var(fields, error) = TypeScript.PackageJson.PackageJsonParser.ParseFields(GoString.Bytes(arg));
                records.Add((JsonNode)new JsonObject { ["inputBase64"] = Bytes(arg), ["errorBase64"] = error is null ? null : Bytes(GoString.ToUtf8(error.Message)), ["fields"] = PackageFields(fields) });
            }

            result["records"] = records;
        }
        else if (mode is "source" or "json" or "file" or "command" or "build")
            ParseMetadata(request, result, mode);
        else
            throw new InvalidOperationException("Unknown configuration operation: " + mode);
        result["extendedConfigCache"] ??= new JsonArray();
        result["diagnosticFiles"] ??= new JsonArray();
        return result.ToJsonString();
    }

    // Authored finite public-codec controls construct supported raw values only.
    // Cyclic values are never passed to the recursive observation serializer.
    private static object? ConstructMarshalValue(JsonObject request)
    {
        string constructor = request["constructor"]?.GetValue<string>() ?? "";
        if (constructor == "")
            return Decode(request["value"]);
        if (constructor is "static-tsconfig-nil" or "static-tsconfig-empty" or "static-tsconfig-false")
        {
            TSConfig config = new();
            if (constructor == "static-tsconfig-empty")
            {
                config.References = [];
                config.Files = [];
                config.Include = [];
                config.Exclude = [];
            }

            if (constructor == "static-tsconfig-false")
                config.CompileOnSave = new StrongBox<bool>(false);
            return config;
        }

        if (constructor is "static-acquisition-nil" or "static-acquisition-empty")
        {
            TypeAcquisition options = new();
            if (constructor == "static-acquisition-empty")
            {
                options.Include = [];
                options.Exclude = [];
            }

            return options;
        }

        if (constructor is "static-build-nil" or "static-build-zero")
            return new BuildOptions
            {
                Builders = constructor == "static-build-zero" ? new StrongBox<long>(0) : null
            };
        if (constructor is "static-compiler-nil" or "static-compiler-empty" or "static-compiler-zero")
        {
            CompilerOptions options = new();
            if (constructor == "static-compiler-empty")
            {
                options.Types = [];
                options.Plugins = [];
                options.Paths = new();
            }

            if (constructor == "static-compiler-zero")
            {
                options.MaxNodeModuleJsDepth = new StrongBox<long>(0);
                options.Checkers = new StrongBox<long>(0);
            }

            return options;
        }

        if (constructor is "static-reference-false" or "static-reference-original")
            return new ProjectReference
            {
                Path = TsPath.RootedPathFromAbsolute("/project/lib"),
                Circular = false,
                OriginalPath = constructor == "static-reference-original" ? "./lib" : ""
            };
        if (constructor == "definition-raw")
            return new TypeScript.ContentMapper.Definition
            {
                Package = "pkg",
                Extensions = [".x"],
                Options = (TypeScript.Json.JsonValue? )Decode(request["value"])
            };
        if (constructor is "json-value-depth" or "list-json-value-depth")
        {
            int depth = request["depth"]?.GetValue<int>() ?? 0;
            if (depth < 0 || depth > 10001)
                throw new InvalidOperationException("Invalid raw JSON depth.");
            object raw = new TypeScript.Json.JsonValue(GoString.Bytes(new string ('[', depth) + "0" + new string (']', depth)));
            return constructor == "list-json-value-depth" ? new List<object?>
            {
                1d,
                raw
            }

            : raw;
        }

        if (constructor is "list-depth" or "map-depth")
        {
            int depth = request["depth"]?.GetValue<int>() ?? 0;
            if (depth < 0 || depth > 10001)
                throw new InvalidOperationException("Invalid bounded marshal depth.");
            object value = 0d;
            for (int i = 0; i < depth; i++)
                value = constructor == "list-depth" ? new List<object?>
                {
                    value
                }

                : new Dictionary<string, object?>
                {
                    ["x"] = value
                };
            return value;
        }

        if (constructor == "list-cycle")
        {
            List<object?> list = [null ];
            list[0] = list;
            return list;
        }

        if (constructor == "map-cycle")
        {
            Dictionary<string, object?> map = [];
            map["x"] = map;
            return map;
        }

        string container = "";
        if (constructor.StartsWith("list-", StringComparison.Ordinal))
        {
            container = "list";
            constructor = constructor[5..];
        }

        if (constructor.StartsWith("map-", StringComparison.Ordinal))
        {
            container = "map";
            constructor = constructor[4..];
        }

        double number = constructor switch
        {
            "nan" => double.NaN,
            "positive-infinity" => double.PositiveInfinity,
            "negative-infinity" => double.NegativeInfinity,
            _ => throw new InvalidOperationException("Unknown bounded marshal constructor.")};
        return container == "list" ? new List<object?>
        {
            number
        }

        : container == "map" ? new Dictionary<string, object?>
        {
            ["x"] = number
        }

        : number;
    }

    private static JsonNode ExpectedValue<TValue>(TypeScript.PackageJson.Expected<TValue> field, JsonNode value) => new JsonObject
    {
        ["present"] = field.IsPresent(),
        ["valid"] = field.Valid,
        ["null"] = field.Null,
        ["expectedType"] = field.ExpectedJSONType(),
        ["actualType"] = field.ActualJSONType(),
        ["value"] = value
    };
    private static JsonNode PackageValue(TypeScript.PackageJson.JSONValue field)
    {
        JsonNode value;
        if (field.Type == TypeScript.PackageJson.JSONValueType.Array)
        {
            JsonArray? items = field.AsArray()is null ? null : new JsonArray();
            if (items is not null)
                foreach (var item in field.AsArray()!)
                    items.Add(PackageValue(item));
            value = new JsonObject
            {
                ["kind"] = "list",
                ["items"] = items
            };
        }
        else if (field.Type == TypeScript.PackageJson.JSONValueType.Object)
        {
            JsonArray entries = [];
            foreach (var(key, item)in field.AsObject().Entries())
                entries.Add((JsonNode)new JsonObject { ["keyBase64"] = Bytes(key), ["value"] = PackageValue(item) });
            value = new JsonObject
            {
                ["kind"] = "ordered-map",
                ["entries"] = entries
            };
        }
        else
            value = Value(field.Value);
        return new JsonObject
        {
            ["type"] = (int)field.Type,
            ["present"] = field.IsPresent(),
            ["falsy"] = field.IsFalsy(),
            ["value"] = value
        };
    }

    private static JsonNode PackageExports(TypeScript.PackageJson.ExportsOrImports field)
    {
        JsonNode value;
        if (field.Type == TypeScript.PackageJson.JSONValueType.Array)
        {
            JsonArray? items = field.AsArray()is null ? null : new JsonArray();
            if (items is not null)
                foreach (var item in field.AsArray()!)
                    items.Add(PackageExports(item));
            value = new JsonObject
            {
                ["kind"] = "list",
                ["items"] = items
            };
        }
        else if (field.Type == TypeScript.PackageJson.JSONValueType.Object)
        {
            JsonArray entries = [];
            foreach (var(key, item)in field.AsObject().Entries())
                entries.Add((JsonNode)new JsonObject { ["keyBase64"] = Bytes(key), ["value"] = PackageExports(item) });
            value = new JsonObject
            {
                ["kind"] = "ordered-map",
                ["entries"] = entries
            };
        }
        else
            value = Value(field.Value);
        return new JsonObject
        {
            ["type"] = (int)field.Type,
            ["present"] = field.IsPresent(),
            ["falsy"] = field.IsFalsy(),
            ["subpaths"] = field.IsSubpaths(),
            ["imports"] = field.IsImports(),
            ["conditions"] = field.IsConditions(),
            ["value"] = value
        };
    }

    private static JsonNode PackageFields(TypeScript.PackageJson.Fields fields)
    {
        var mapper = fields.ContentMapper.Value;
        return new JsonObject
        {
            ["name"] = ExpectedValue(fields.Name, Value(fields.Name.Value)),
            ["version"] = ExpectedValue(fields.Version, Value(fields.Version.Value)),
            ["type"] = ExpectedValue(fields.Type, Value(fields.Type.Value)),
            ["tsconfig"] = ExpectedValue(fields.TSConfig, Value(fields.TSConfig.Value)),
            ["main"] = ExpectedValue(fields.Main, Value(fields.Main.Value)),
            ["types"] = ExpectedValue(fields.Types, Value(fields.Types.Value)),
            ["typings"] = ExpectedValue(fields.Typings, Value(fields.Typings.Value)),
            ["typesVersions"] = PackageValue(fields.TypesVersions),
            ["imports"] = PackageExports(fields.Imports),
            ["exports"] = PackageExports(fields.Exports),
            ["dependencies"] = ExpectedValue(fields.Dependencies, Value(fields.Dependencies.Value, "map[string]string")),
            ["devDependencies"] = ExpectedValue(fields.DevDependencies, Value(fields.DevDependencies.Value, "map[string]string")),
            ["peerDependencies"] = ExpectedValue(fields.PeerDependencies, Value(fields.PeerDependencies.Value, "map[string]string")),
            ["optionalDependencies"] = ExpectedValue(fields.OptionalDependencies, Value(fields.OptionalDependencies.Value, "map[string]string")),
            ["contentMapper"] = ExpectedValue(fields.ContentMapper, new JsonObject { ["exec"] = ExpectedValue(mapper.Exec, Value(mapper.Exec.Value, "[]string")), ["compilerOptions"] = ExpectedValue(mapper.CompilerOptions, Value(mapper.CompilerOptions.Value, "[]string")), ["dynamicConfig"] = ExpectedValue(mapper.DynamicConfig, Value(mapper.DynamicConfig.Value)) })
        };
    }

    private static object? Decode(JsonNode? tagged)
    {
        if (tagged is null)
            return null;
        var node = (JsonObject)tagged;
        string kind = node["kind"]!.GetValue<string>();
        switch (kind)
        {
            case "null":
                return null;
            case "json-value":
                return node["null"]?.GetValue<bool>() == true ? new TypeScript.Json.JsonValue(null) : new TypeScript.Json.JsonValue(Convert.FromBase64String(node["bytesBase64"]!.GetValue<string>()));
            case "bytes":
                return GoString.FromBytes(Convert.FromBase64String(node["bytesBase64"]!.GetValue<string>()));
            case "boolean":
                return node["boolean"]?.GetValue<bool>() ?? false;
            case "number":
                return double.Parse(node["number"]!.GetValue<string>(), CultureInfo.InvariantCulture);
            case "list":
                if (node["null"]?.GetValue<bool>() == true)
                    return null;
                return (node["items"] as JsonArray ?? []).Select(Decode).ToList();
            case "ordered-map":
                if (node["null"]?.GetValue<bool>() == true)
                    throw new InvalidOperationException("Nil ordered-map pointer is an invalid original API input.");
                OrderedMap<string, object?> ordered = new();
                foreach (JsonNode? entry in node["entries"] as JsonArray ?? [])
                    ordered.Set(GoString.FromBytes(Convert.FromBase64String(entry!["keyBase64"]!.GetValue<string>())), Decode(entry["value"]));
                return ordered;
            case "map":
                if (node["null"]?.GetValue<bool>() == true)
                    return null;
                Dictionary<string, object?> map = [];
                foreach (JsonNode? entry in node["entries"] as JsonArray ?? [])
                    map[GoString.FromBytes(Convert.FromBase64String(entry!["keyBase64"]!.GetValue<string>()))] = Decode(entry["value"]);
                return map;
            default:
                throw new InvalidOperationException("Unknown tagged input kind: " + kind);
        }
    }

    private static CompilerOptions? Options(JsonNode? raw)
    {
        if (raw is null)
            return null;
        var map = Decode(raw) as OrderedMap<string, object?> ?? throw new InvalidOperationException("Existing options require ordered-map.");
        CompilerOptions options = new();
        foreach (var(key, value)in map.Entries())
            T.ParseCompilerOptions(key, value, options);
        return options;
    }

    private static ConfigurationFileSystem FileSystem(JsonArray? files, CaseSensitivity sensitivity)
    {
        List<ConfigurationFileSystem.Input> inputs = [];
        foreach (JsonNode? item in files ?? [])
        {
            var file = (JsonObject)item!;
            string? link = file["symlink"] is not null || file["symlinkBase64"] is not null ? Input(file, "symlink", "symlinkBase64") : null;
            inputs.Add(new(Input(file, "path", "pathBase64"), Input(file, "text", "textBase64"), link, file["directory"]?.GetValue<bool>() ?? false, file["readFailure"]?.GetValue<bool>() ?? false));
        }

        return new ConfigurationFileSystem(inputs, sensitivity);
    }

    private static void ParseMetadata(JsonObject request, JsonObject result, string mode)
    {
        int casePolicy = request["caseSensitivity"]?.GetValue<int>() ?? 1;
        if (casePolicy is not (0 or 1))
            throw new InvalidOperationException("Unknown case policy.");
        CaseSensitivity sensitivity = (CaseSensitivity)casePolicy;
        string config = Input(request, "configPath", "configPathBase64");
        if (config == "")
            config = "/project/tsconfig.json";
        string directory = Input(request, "baseDirectory", "baseDirectoryBase64");
        if (directory == "")
            directory = TsPath.GetDirectoryPath(config);
        RootedFilePath configPath = RootedFilePath.FromGoString(config);
        RootedDirectoryPath basePath = RootedDirectoryPath.FromGoString(directory);
        JsonArray files = (JsonArray? )request["files"]?.DeepClone() ?? [];
        if (request["text"] is not null || request["textBase64"] is not null)
        {
            JsonObject? existing = files.OfType<JsonObject>().FirstOrDefault(file => Input(file, "path", "pathBase64") == config);
            if (existing is null)
            {
                existing = new JsonObject
                {
                    ["pathBase64"] = Bytes(config)
                };
                files.Add((JsonNode)existing);
            }

            existing["textBase64"] = Bytes(Input(request, "text", "textBase64"));
        }

        var fs = FileSystem(files, sensitivity);
        Observer observer = new();
        ExtendedCache cache = new();
        ParsedCommandLine? parsed = null;
        List<Diagnostic>? readErrors = null;
        switch (mode)
        {
            case "source":
                var source = T.NewTsconfigSourceFileFromFilePath(configPath, sensitivity.PathKey(configPath.AsPath()), Input(request, "text", "textBase64"));
                observer.File(source.SourceFile);
                parsed = T.ParseJsonSourceFileConfigFileContent(source, fs, basePath, Options(request["existingOptions"]), request["existingOptionsRaw"] is null ? null : (OrderedMap<string, object?>? )Decode(request["existingOptionsRaw"]), null, cache);
                break;
            case "json":
                object? raw = Decode(request["value"]);
                parsed = T.ParseJsonConfigFileContent(raw, fs, basePath, Options(request["existingOptions"]), configPath, null, cache);
                // Go interface typed-nil list/map input headers are erased by managed object null.
                // Preserve only the explicitly supplied declaration, never invent a value.
                string declaredInput = request["value"]?["null"]?.GetValue<bool>() == true ? request["value"]?["kind"]?.GetValue<string>() switch
                {
                    "list" => "[]interface {}",
                    "map" => "map[string]interface {}",
                    _ => ""
                } : "";
                result["inputValueAfterParsing"] = Value(raw, declaredInput);
                break;
            case "file":
                (parsed, readErrors) = T.GetParsedCommandLineOfConfigFile(configPath, Options(request["existingOptions"]), request["existingOptionsRaw"] is null ? null : (OrderedMap<string, object?>? )Decode(request["existingOptionsRaw"]), fs, cache);
                break;
            case "command":
            case "build":
                List<string>? args = request["argumentsBase64"] is JsonArray encoded ? encoded.Select(arg => GoString.FromBytes(Convert.FromBase64String(arg!.GetValue<string>()))).ToList() : (request["arguments"] as JsonArray)?.Select(arg => GoString.ToUtf8(arg!.GetValue<string>())).ToList();
                if (mode == "command")
                    parsed = T.ParseCommandLine(args, fs, basePath);
                else
                {
                    var build = T.ParseBuildCommandLine(args, fs, basePath);
                    result["buildOptions"] = Value(build.BuildOptions);
                    result["compilerOptions"] = Value(build.CompilerOptions);
                    result["projects"] = Value(build.Projects, "[]string");
                    result["resolvedProjectPaths"] = Value(build.ResolvedProjectPaths(), "[]tspath.RootedFilePath");
                    result["raw"] = Value(build.Raw);
                    result["errors"] = observer.Diagnostics(build.Errors);
                }

                break;
        }

        if (mode != "build")
        {
            result["parsed"] = observer.Parsed(parsed, request, configPath);
            result["readErrors"] = observer.Diagnostics(readErrors);
        }

        if (parsed is not null && request["reloadFiles"] is JsonArray reload)
        {
            var reloaded = parsed.ReloadFileNamesOfParsedCommandLine(FileSystem(reload, sensitivity));
            result["reloaded"] = observer.Parsed(reloaded, request, configPath);
            result["reloadIdentity"] = new JsonObject
            {
                ["distinctParsed"] = !ReferenceEquals(parsed, reloaded),
                ["sameCompilerOptions"] = ReferenceEquals(parsed.CompilerOptions(), reloaded.CompilerOptions()),
                ["sameConfigSource"] = ReferenceEquals(parsed.ConfigFile, reloaded.ConfigFile)
            };
        }

        if (request["configurationSequence"] is JsonArray sequence)
        {
            JsonArray states = [];
            foreach (var name in sequence)
            {
                RootedFilePath path = RootedFilePath.FromUnicode(name!.GetValue<string>());
                var(next, errors) = T.GetParsedCommandLineOfConfigFile(path, Options(request["existingOptions"]), request["existingOptionsRaw"] is null ? null : (OrderedMap<string, object?>? )Decode(request["existingOptionsRaw"]), fs, cache);
                states.Add((JsonNode)new JsonObject { ["parsed"] = observer.Parsed(next, request, path), ["readErrors"] = observer.Diagnostics(errors) });
            }

            result["sequence"] = states;
        }

        result["extendedConfigCache"] = observer.Cache(cache);
        result["diagnosticFiles"] = observer.Files;
    }

    private sealed class ExtendedCache : IExtendedConfigCache
    {
        internal readonly Dictionary<PathKey, ExtendedConfigCacheEntry> Entries = [];
        public ExtendedConfigCacheEntry GetExtendedConfig(RootedFilePath fileName, PathKey path, List<PathKey>? stack, TypeScript.Vfs.IFS fs)
        {
            if (Entries.TryGetValue(path, out var entry))
                return entry;
            entry = T.ParseExtendedConfig(fileName, path, stack, fs, this);
            Entries.Add(path, entry);
            return entry;
        }
    }

    private static JsonNode Value(object? value) => Value(value, "");
    private static JsonNode Value(object? value, string declaredType)
    {
        if (value is null)
        {
            if (declaredType.StartsWith("[]", StringComparison.Ordinal) || declaredType == "jsontext.Value")
                return new JsonObject
                {
                    ["kind"] = "list",
                    ["items"] = null
                };
            if (declaredType.StartsWith("*collections.OrderedMap", StringComparison.Ordinal))
                return new JsonObject
                {
                    ["kind"] = "ordered-map",
                    ["entries"] = null
                };
            if (declaredType.StartsWith("map[", StringComparison.Ordinal))
                return new JsonObject
                {
                    ["kind"] = "map",
                    ["entries"] = null
                };
            return new JsonObject
            {
                ["kind"] = "null"
            };
        }

        switch (value)
        {
            case TypeScript.TsOptions.EmptyConfigurationObject:
                return new JsonObject
                {
                    ["kind"] = "empty-config-object"
                };
            case string bytes:
                return new JsonObject
                {
                    ["kind"] = "bytes",
                    ["bytesBase64"] = Bytes(bytes)
                };
            case bool boolean:
                return new JsonObject
                {
                    ["kind"] = "boolean",
                    ["boolean"] = boolean
                };
            case double number:
                return new JsonObject
                {
                    ["kind"] = "float64",
                    ["bits"] = unchecked((ulong)BitConverter.DoubleToInt64Bits(number)).ToString("x16")
                };
            case int or long or uint or ulong or sbyte or byte or short or ushort or Enum:
                return new JsonObject
                {
                    ["kind"] = "integer",
                    ["number"] = Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                };
            case StrongBox<long> pointer:
                return Value(pointer.Value);
            case StrongBox<bool> pointer:
                return Value(pointer.Value);
            case RootedPath path:
                return Value(path.AsString());
            case RootedFilePath path:
                return Value(path.AsString());
            case RootedDirectoryPath path:
                return Value(path.AsString());
            case PathKey path:
                return Value(path.AsString());
            case SourceMapLocation path:
                return Value(path.AsString());
            case FileSpec path:
                return Value(path.AsString());
            case PathPattern path:
                return Value(path.AsString());
            case RelativePath path:
                return Value(path.AsString());
            case TypeScript.Json.JsonValue json:
                return Value(json.Bytes);
            case IList list:
                JsonArray items = [];
                foreach (object? item in list)
                    items.Add(Value(item));
                return new JsonObject
                {
                    ["kind"] = "list",
                    ["items"] = items
                };
            case OrderedMap<string, object?> map:
                JsonArray entries = [];
                foreach (var(key, item)in map.Entries())
                    entries.Add((JsonNode)new JsonObject { ["keyBase64"] = Bytes(key), ["value"] = Value(item) });
                return new JsonObject
                {
                    ["kind"] = "ordered-map",
                    ["entries"] = entries
                };
            case OrderedMap<string, List<string>> map:
                JsonArray paths = [];
                foreach (var(key, item)in map.Entries())
                    paths.Add((JsonNode)new JsonObject { ["keyBase64"] = Bytes(key), ["value"] = Value(item, "[]string") });
                return new JsonObject
                {
                    ["kind"] = "ordered-map",
                    ["entries"] = paths
                };
            case IDictionary map:
                var fields = new List<(string Key, object? Value)>();
                foreach (DictionaryEntry entry in map)
                {
                    string key = entry.Key switch
                    {
                        string bytes => bytes,
                        PathKey path => path.AsString(),
                        RootedDirectoryPath path => path.AsString(),
                        _ => throw new InvalidOperationException("Unknown configuration map key.")};
                    fields.Add((key, entry.Value));
                }

                JsonArray unordered = [];
                foreach (var(key, item)in fields.OrderBy(field => field.Key, StringComparer.Ordinal))
                    unordered.Add((JsonNode)new JsonObject { ["keyBase64"] = Bytes(key), ["value"] = Value(item) });
                return new JsonObject
                {
                    ["kind"] = "map",
                    ["entries"] = unordered
                };
            case CompilerOptions model:
                return Record("CompilerOptions", model, OptionFields.CompilerOptions);
            case TypeAcquisition model:
                return Record("TypeAcquisition", model, OptionFields.TypeAcquisition);
            case BuildOptions model:
                return Record("BuildOptions", model, OptionFields.BuildOptions);
            case TSConfig model:
                return Record("TSConfig", model, ConfigurationObserve.TSConfigFields);
            case ProjectReference model:
                return Record("ProjectReference", model, ConfigurationObserve.ProjectReferenceFields);
            case PluginImport model:
                return Record("PluginImport", model, ConfigurationObserve.PluginImportFields);
            case TypeScript.ContentMapper.Mapper model:
                return Record("Mapper", model, ConfigurationObserve.MapperFields);
            case TypeScript.ContentMapper.Definition model:
                return Record("Definition", model, ConfigurationObserve.DefinitionFields);
            case TypeScript.ContentMapper.Manifest model:
                return Record("Manifest", model, ConfigurationObserve.ManifestFields);
            default:
                throw new InvalidOperationException("Unknown stored configuration value: " + value.GetType().Name);
        }
    }

    private static string OriginalType(string type) => type switch
    {
        "Tristate" or "JsxEmit" or "ModuleKind" or "ModuleResolutionKind" or "ModuleDetectionKind" or "NewLineKind" or "ScriptTarget" => "core." + type,
        "[]PluginImport" => "[]core.PluginImport",
        _ => type.Replace("string, ", "string,", StringComparison.Ordinal)};
    private static JsonNode Record<TModel>(string name, TModel model, IReadOnlyList<OptionFieldDescriptor<TModel>> descriptors)
    {
        JsonArray fields = [];
        HashSet<string> names = [];
        foreach (var field in descriptors)
        {
            if (!names.Add(field.Name))
                throw new InvalidOperationException("Duplicate static stored field descriptor.");
            object? stored = field.Read(model);
            // Original embedded Definition is a struct subrecord. Managed inheritance
            // retains the same object; use its declared field scope, not derived dispatch.
            JsonNode projected = field.GoType == "contentmapper.Definition" ? Record("Definition", (TypeScript.ContentMapper.Definition)stored!, ConfigurationObserve.DefinitionFields) : Value(stored, field.GoType);
            fields.Add((JsonNode)new JsonObject { ["name"] = field.Name, ["jsonName"] = field.JsonName, ["declaredType"] = OriginalType(field.GoType), ["value"] = projected });
        }

        return new JsonObject
        {
            ["kind"] = "record",
            ["type"] = name,
            ["fields"] = fields
        };
    }

    private sealed class Observer
    {
        private readonly Dictionary<SourceFile, int> ids = new(ReferenceEqualityComparer.Instance);
        public JsonArray Files { get; } = [];

        public int? File(SourceFile? file)
        {
            if (file is null)
                return null;
            if (!ids.TryGetValue(file, out int id))
            {
                id = ids.Count;
                ids.Add(file, id);
                Files.Add((JsonNode)new JsonObject { ["id"] = id, ["nameBase64"] = Bytes(file.FileName()), ["pathBase64"] = Bytes(file.PathKey()), ["textBase64"] = Bytes(file.Text()) });
            }

            return id;
        }

        private static JsonArray? Specs(List<ConfigurationStoredField>? fields)
        {
            if (fields is null)
                return null;
            JsonArray result = [];
            foreach (var field in fields)
            {
                string type = field.Name switch
                {
                    "validatedFilesSpec" or "validatedFilesSpecBeforeSubstitution" => "[]tspath.FileSpec",
                    "validatedFileNames" => "[]tspath.RootedFilePath",
                    "validatedIncludeSpecs" or "validatedExcludeSpecs" or "validatedIncludeSpecsBeforeSubstitution" => "[]tspath.PathPattern",
                    "fileSpecByPath" => "map[tspath.PathKey]tspath.FileSpec",
                    // Original publishes propOfRaw.sliceValue into each any
                    // slot at its sole constructor, retaining typed nil []any.
                    "filesSpecs" or "includeSpecs" or "excludeSpecs" => "[]interface {}",
                    "isDefaultIncludeSpec" => "",
                    _ => throw new InvalidOperationException("Unknown stored spec field: " + field.Name)};
                result.Add((JsonNode)new JsonObject { ["name"] = field.Name, ["value"] = Value(field.Value, type) });
            }

            return result;
        }

        public JsonNode? Parsed(ParsedCommandLine? parsed, JsonObject request, RootedFilePath config)
        {
            if (parsed is null)
                return null;
            JsonObject result = new()
            {
                ["configFile"] = File(parsed.ConfigFile?.SourceFile),
                ["options"] = Value(parsed.CompilerOptions()),
                ["typeAcquisition"] = Value(parsed.ParsedConfig.TypeAcquisition),
                ["fileNames"] = Value(parsed.FileNames(), "[]tspath.RootedFilePath"),
                ["literalFileNames"] = Value(parsed.LiteralFileNames(), "[]tspath.RootedFilePath"),
                ["references"] = Value(parsed.ProjectReferences(), "[]*core.ProjectReference"),
                ["contentMappers"] = Value(parsed.ContentMappers(), "[]*contentmapper.Mapper"),
                ["raw"] = Value(parsed.Raw),
                ["compileOnSave"] = Value(parsed.CompileOnSave),
                ["errors"] = Diagnostics(parsed.Errors),
                ["baseDirectoryBase64"] = Bytes(parsed.BaseDirectory().AsString()),
                ["caseSensitivity"] = (int)parsed.CaseSensitivity(),
                ["extendedSourceFiles"] = Value(parsed.ExtendedSourceFiles(), "[]tspath.RootedFilePath"),
                ["configParsingDiagnostics"] = Diagnostics(parsed.GetConfigFileParsingDiagnostics()),
                ["commonSourceDirectoryBase64"] = Bytes(parsed.CommonSourceDirectory().AsString()),
                ["resolvedReferencePaths"] = Value(parsed.ResolvedProjectReferencePaths(), "[]tspath.RootedFilePath")
            };
            JsonArray names = [];
            for (int i = 0; i < (parsed.FileNames()?.Count ?? 0); i++)
                names.Add((JsonNode)JsonValue.Create(Bytes(parsed.RootFileNameForDiagnostic(i)))!);
            result["rootFileNamesForDiagnosticsBase64"] = names;
            result["outputFileNames"] = Value(parsed.GetOutputFileNames().ToList());
            result["buildInfoFileNameBase64"] = Bytes(parsed.GetBuildInfoFileName().AsString());
            result["showConfig"] = Value(T.ConvertToTSConfig(parsed, config));
            JsonArray queries = [];
            bool command = request["mode"]!.GetValue<string>() == "command";
            foreach (JsonNode? query in request["fileQueries"] as JsonArray ?? [])
            {
                var path = TsPath.ToRootedFilePath(GoString.ToUtf8(query!.GetValue<string>()), parsed.BaseDirectory());
                JsonObject row = new()
                {
                    ["pathBase64"] = Bytes(path.AsString()),
                    ["possiblyMatches"] = parsed.PossiblyMatchesFileName(path)
                };
                if (!command)
                {
                    var(spec, defaultInclude) = parsed.GetMatchedIncludeSpec(path);
                    row["fileSpecBase64"] = Bytes(parsed.GetMatchedFileSpec(path));
                    row["includeSpecBase64"] = Bytes(spec);
                    row["defaultInclude"] = defaultInclude;
                }

                queries.Add((JsonNode)row);
            }

            result["fileQueries"] = queries;
            JsonArray directories = [];
            foreach (JsonNode? query in request["directoryQueries"] as JsonArray ?? [])
            {
                var path = TsPath.ToRootedPath(GoString.ToUtf8(query!.GetValue<string>()), parsed.BaseDirectory());
                directories.Add((JsonNode)new JsonObject { ["pathBase64"] = Bytes(path.AsString()), ["possiblyMatches"] = parsed.PossiblyMatchesDirectoryName(parsed.CaseSensitivity().PathKey(path)) });
            }

            result["directoryQueries"] = directories;
            result["storedSpecs"] = Specs(ConfigurationObserve.Specs(parsed));
            if (!command)
                result["wildcardDirectories"] = Value(parsed.WildcardDirectories(), "map[tspath.RootedDirectoryPath]bool");
            return result;
        }

        public JsonArray Cache(ExtendedCache cache)
        {
            JsonArray result = [];
            foreach (var(path, entry)in cache.Entries.OrderBy(entry => entry.Key.AsString(), StringComparer.Ordinal))
            {
                var stored = ConfigurationObserve.ExtendedConfig(entry);
                result.Add((JsonNode)new JsonObject { ["pathBase64"] = Bytes(path.AsString()), ["source"] = File(stored.Source), ["storedSpecs"] = Specs(stored.Specs), ["extendedSourceFiles"] = Value(stored.ExtendedSourceFiles, "[]tspath.RootedFilePath"), ["hasParsedConfig"] = stored.HasParsedConfig, ["raw"] = Value(stored.Raw), ["options"] = Value(stored.Options), ["unresolvedPaths"] = Value(stored.UnresolvedPaths, "map[string]interface {}"), ["typeAcquisition"] = Value(stored.TypeAcquisition), ["extendedConfigPaths"] = Value(stored.ExtendedConfigPaths, "[]tspath.RootedFilePath"), ["errors"] = Diagnostics(stored.Errors) });
            }

            return result;
        }

        public JsonArray? Diagnostics(IReadOnlyList<Diagnostic>? diagnostics)
        {
            if (diagnostics is null)
                return null;
            JsonArray result = [];
            foreach (var diagnostic in diagnostics)
            {
                var arguments = diagnostic.MessageArgs();
                result.Add((JsonNode)new JsonObject { ["code"] = diagnostic.Code(), ["category"] = (int)diagnostic.Category(), ["key"] = diagnostic.MessageKey(), ["textBase64"] = Bytes(GoString.ToUtf8(diagnostic.String())), ["argsBase64"] = arguments is null ? null : new JsonArray(arguments.Select(arg => JsonValue.Create(Bytes(arg)) as JsonNode).ToArray()), ["pos"] = diagnostic.Pos(), ["end"] = diagnostic.End(), ["file"] = File(diagnostic.File()), ["sourceBase64"] = Bytes(diagnostic.Source()), ["reportsUnnecessary"] = diagnostic.ReportsUnnecessary(), ["reportsDeprecated"] = diagnostic.ReportsDeprecated(), ["skippedOnNoEmit"] = diagnostic.SkippedOnNoEmit(), ["chain"] = Diagnostics(diagnostic.MessageChain()), ["related"] = Diagnostics(diagnostic.RelatedInformation()) });
            }

            return result;
        }
    }
}
