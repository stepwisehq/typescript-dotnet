// Authored behavioral observer of actual Program APIs; no checker acquisition.
// Static value projection reuses accepted Resolution roles, without reflection.
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using TypeScript.Collections;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.Vfs;
using Configuration.Checks;
using CompilerProgram = TypeScript.Compiler.Program;
using T = TypeScript.TsOptions.TsOptions;

namespace Program.Checks;

public static partial class ProgramRunner
{
    private static string Hash(string bytes) => Convert.ToHexStringLower(SHA256.HashData(GoString.Bytes(bytes)));
    private static string Bytes(string raw) => Convert.ToBase64String(GoString.Bytes(raw));
    private static string Input(JsonObject item, string name, string? encoded = null) => item[encoded ?? name + "Base64"] is JsonValue raw ? GoString.FromBytes(Convert.FromBase64String(raw.GetValue<string>())) : GoString.ToUtf8(item[name]?.GetValue<string>() ?? "");
    private static JsonObject Object(params (string Key, object? Value)[] fields)
    {
        JsonObject result = [];
        foreach (var(key, value)in fields)
            result[key] = Value(value);
        return result;
    }

    private static JsonNode? Value(object? value)
    {
        if (value is null)
            return null;
        if (value is JsonNode node)
            return node;
        if (value is string text)
            return new JsonObject
            {
                ["bytesBase64"] = Bytes(text)
            };
        if (value is bool boolean)
            return JsonValue.Create(boolean);
        if (value is int integer)
            return JsonValue.Create(integer);
        if (value is long number)
            return JsonValue.Create(number);
        if (value is uint unsigned)
            return JsonValue.Create(unsigned);
        if (value is ulong largeUnsigned)
            return JsonValue.Create(largeUnsigned);
        if (value is UInt128 hash)
            return Object(("Hi", (ulong)(hash >> 64)), ("Lo", (ulong)hash));
        if (value is double floating)
            return JsonValue.Create(floating);
        if (value is Enum enumeration)
            return JsonValue.Create(Convert.ToInt64(enumeration));
        if (value is StrongBox<long> box)
            return Value(box.Value);
        if (value is StrongBox<bool> booleanBox)
            return Value(booleanBox.Value);
        if (value is ModuleSpecifier specifier)
            return Value(specifier.AsString());
        if (value is PackageDirectory packageDirectory)
            return Value(packageDirectory.AsDirectoryPath());
        if (value is DefaultResolver.resolutionCandidate candidate)
            return Value(candidate.AsString());
        if (value is ResolvedEntrypoint entrypoint)
            return Object(("OriginalFileName", entrypoint.OriginalFileName), ("ResolvedFileName", entrypoint.ResolvedFileName), ("ResolvedPath", entrypoint.ResolvedPath), ("ModuleSpecifier", entrypoint.ModuleSpecifier), ("Ending", entrypoint.Ending), ("IncludeConditions", entrypoint.IncludeConditions?.OrderBy(item => item, StringComparer.Ordinal).ToList()), ("ExcludeConditions", entrypoint.ExcludeConditions?.OrderBy(item => item, StringComparer.Ordinal).ToList()));
        if (value is RootedPath path)
            return Value(path.AsString());
        if (value is RootedFilePath file)
            return Value(file.AsString());
        if (value is RootedDirectoryPath directory)
            return Value(directory.AsString());
        if (value is PathKey key)
            return Value(key.AsString());
        if (value is CompilerOptions options)
        {
            JsonObject result = [];
            foreach (var field in OptionFields.CompilerOptions)
                result[field.Name] = Value(field.Read(options));
            return result;
        }

        if (value is SourceFileMetaData metadata)
            return Object(("PackageJsonType", metadata.PackageJsonType), ("PackageJsonDirectory", metadata.PackageJsonDirectory), ("ImpliedNodeFormat", metadata.ImpliedNodeFormat));
        if (value is SourceFileParseOptions parseOptions)
            return Object(("FileName", parseOptions.FileName ?? ""), ("PathKey", parseOptions.PathKey ?? ""), ("ExternalModuleIndicatorOptions", parseOptions.ExternalModuleIndicatorOptions));
        if (value is ExternalModuleIndicatorOptions indicatorOptions)
            return Object(("JSX", indicatorOptions.JSX), ("Force", indicatorOptions.Force));
        if (value is DuplicateSourceFile duplicate)
            return Object(("ParseOptions", duplicate.ParseOptions), ("ContentMapperParseOptions", duplicate.ContentMapperParseOptions), ("Hash", duplicate.Hash), ("ScriptKind", duplicate.ScriptKind), ("ContentMapper", duplicate.ContentMapper), ("IsContentMapperFailureStub", duplicate.IsContentMapperFailureStub));
        if (value is FileReference reference)
            return Object(("FileName", reference.FileName), ("ResolutionMode", reference.ResolutionMode), ("Preserve", reference.Preserve), ("pos", reference.Pos()), ("end", reference.End()));
        if (value is PluginImport plugin)
            return Object(("Name", plugin.Name));
        if (value is ProjectReference projectReference)
            return Object(("Path", projectReference.Path), ("OriginalPath", projectReference.OriginalPath), ("Circular", projectReference.Circular));
        if (value is SourceMapLocation location)
            return Value(location.AsString());
        if (value is PackageId id)
            return Object(("Name", id.Name), ("SubModuleName", id.SubModuleName), ("Version", id.Version), ("PeerDependencies", id.PeerDependencies));
        if (value is Fields fields)
            return Object(("HeaderFields", fields.HeaderFields), ("PathFields", fields.PathFields), ("DependencyFields", fields.DependencyFields), ("ContentMapper", fields.ContentMapper));
        if (value is HeaderFields header)
            return Object(("Name", header.Name), ("Version", header.Version), ("Type", header.Type));
        if (value is PathFields paths)
            return Object(("TSConfig", paths.TSConfig), ("Main", paths.Main), ("Types", paths.Types), ("Typings", paths.Typings), ("TypesVersions", paths.TypesVersions), ("Imports", paths.Imports), ("Exports", paths.Exports));
        if (value is DependencyFields dependencies)
            return Object(("Dependencies", dependencies.Dependencies), ("DevDependencies", dependencies.DevDependencies), ("PeerDependencies", dependencies.PeerDependencies), ("OptionalDependencies", dependencies.OptionalDependencies));
        if (value is ContentMapperFields mapper)
            return Object(("Exec", mapper.Exec), ("CompilerOptions", mapper.CompilerOptions), ("DynamicConfig", mapper.DynamicConfig));
        if (value is Expected<string> expectedText)
            return Expected(expectedText);
        if (value is Expected<bool> expectedBool)
            return Expected(expectedBool);
        if (value is Expected<List<string>?> expectedList)
            return Expected(expectedList);
        if (value is Expected<Dictionary<string, string>?> expectedMap)
            return Expected(expectedMap);
        if (value is Expected<ContentMapperFields> expectedMapper)
            return Expected(expectedMapper);
        if (value is JSONValue json)
            return Object(("Type", json.Type), ("Value", json.Value));
        if (value is ExportsOrImports exports)
            return new JsonObject
            {
                ["JSONValue"] = Object(("Type", exports.Type), ("Value", exports.Value))
            };
        if (value is OrderedMap<string, List<string>?> orderedPaths)
            return Ordered(orderedPaths.Entries().Select(item => (item.Key, (object? )item.Value)));
        if (value is OrderedMap<string, JSONValue> orderedJSON)
            return Ordered(orderedJSON.Entries().Select(item => (item.Key, (object? )item.Value)));
        if (value is OrderedMap<string, ExportsOrImports> orderedExports)
            return Ordered(orderedExports.Entries().Select(item => (item.Key, (object? )item.Value)));
        if (value is IDictionary dictionary)
        {
            var entries = new List<(string, object? )>();
            foreach (DictionaryEntry entry in dictionary)
                entries.Add((entry.Key as string ?? throw new InvalidOperationException("Unobserved stored map key"), entry.Value));
            return Ordered(entries.OrderBy(entry => entry.Item1, StringComparer.Ordinal));
        }

        if (value is IEnumerable sequence)
        {
            JsonArray array = [];
            foreach (object? item in sequence)
                array.Add((JsonNode? )Value(item));
            return array;
        }

        throw new InvalidOperationException("Unobserved stored value family: " + value.GetType());
    }

    private static JsonNode Expected<U>(Expected<U> expected) => Object(("Null", expected.Null), ("Valid", expected.Valid), ("Value", expected.Value));
    private static JsonArray Ordered(IEnumerable<(string Key, object? Value)> entries)
    {
        JsonArray result = [];
        foreach (var(key, value)in entries)
            result.Add((JsonNode)new JsonObject { ["keyBase64"] = Bytes(key), ["value"] = Value(value) });
        return result;
    }

    private sealed class Observer
    {
        private readonly Dictionary<SourceFile, int> files = new(ReferenceEqualityComparer.Instance);
        internal readonly JsonArray Owners = [];
        internal int? File(SourceFile? file)
        {
            if (file is null)
                return null;
            if (!files.TryGetValue(file, out int id))
            {
                id = files.Count;
                files.Add(file, id);
                Owners.Add((JsonNode)new JsonObject { ["id"] = id, ["nameBase64"] = Bytes(file.FileNameGoBytes()), ["pathBase64"] = Bytes(file.PathKey()), ["textSha256"] = Hash(file.Text()), ["textBytes"] = file.Text().Length });
            }

            return id;
        }

        internal JsonNode? Diagnostics(IEnumerable<Diagnostic>? diagnostics)
        {
            if (diagnostics is null)
                return null;
            JsonArray result = [];
            foreach (Diagnostic d in diagnostics)
                result.Add((JsonNode)new JsonObject { ["code"] = d.Code(), ["category"] = (int)d.Category(), ["key"] = d.MessageKey().ToString(), ["textBase64"] = Bytes(GoString.ToUtf8(d.String())), ["args"] = Value(d.MessageArgs()), ["pos"] = d.Pos(), ["end"] = d.End(), ["file"] = File(d.File()), ["sourceBase64"] = Bytes(d.Source()), ["reportsUnnecessary"] = d.ReportsUnnecessary(), ["reportsDeprecated"] = d.ReportsDeprecated(), ["skippedOnNoEmit"] = d.SkippedOnNoEmit(), ["chain"] = Diagnostics(d.MessageChain()), ["related"] = Diagnostics(d.RelatedInformation()), ["repopulateInfo"] = null });
            return result;
        }

        internal JsonNode? Module(ResolvedModule? result) => result is null ? null : Object(("ResolutionDiagnostics", Diagnostics(result.ResolutionDiagnostics)), ("ResolvedFileName", result.ResolvedFileName), ("ResolvedPath", result.ResolvedPath), ("OriginalPath", result.OriginalPath), ("Extension", result.Extension), ("ResolvedUsingTsExtension", result.ResolvedUsingTsExtension), ("ResolvedUsingExtraExtensions", result.ResolvedUsingExtraExtensions), ("IsExternalLibraryImport", result.IsExternalLibraryImport), ("IsCustomResolution", result.IsCustomResolution), ("PackageId", result.PackageId), ("AlternateResult", result.AlternateResult));
        internal JsonNode? Directive(ResolvedTypeReferenceDirective? result) => result is null ? null : Object(("ResolutionDiagnostics", Diagnostics(result.ResolutionDiagnostics)), ("Primary", result.Primary), ("ResolvedFileName", result.ResolvedFileName), ("ResolvedPath", result.ResolvedPath), ("OriginalPath", result.OriginalPath), ("PackageId", result.PackageId), ("IsExternalLibraryImport", result.IsExternalLibraryImport));
    }

    private static JsonObject Snapshot(Observer observer, CompilerProgram program, bool bind)
    {
        JsonArray files = [];
        foreach (var file in program.SourceFiles())
            files.Add((JsonNode)new JsonObject
            {
                ["id"] = observer.File(file), ["nameBase64"] = Bytes(file.FileNameGoBytes()), ["pathBase64"] = Bytes(file.PathKey()),
                ["textSha256"] = Hash(file.Text()), ["textBytes"] = file.Text().Length, ["scriptKind"] = (int)file.ScriptKind,
                ["declaration"] = file.IsDeclarationFile, ["externalModule"] = file.ExternalModuleIndicator is not null,
                ["metadata"] = Value(program.GetSourceFileMetaData(PathKey.FromGoString(file.PathKey()))),
                ["defaultLibrary"] = program.IsSourceFileDefaultLibrary(PathKey.FromGoString(file.PathKey())),
                ["library"] = program.IsLibFile(file), ["externalLibrary"] = program.IsSourceFileFromExternalLibrary(file),
                ["globalTypings"] = program.IsGlobalTypingsFile(RootedFilePath.FromGoString(file.FileNameGoBytes())),
                ["parseDiagnostics"] = observer.Diagnostics(file.Diagnostics()), ["includeDiagnostics"] = observer.Diagnostics(program.GetIncludeProcessorDiagnostics(file)),
                ["defaultMode"] = (int)program.GetDefaultResolutionModeForFile(file), ["emitFormat"] = (int)program.GetEmitModuleFormatOfFile(file),
                ["references"] = Value(file.ReferencedFiles), ["typeReferences"] = Value(file.TypeReferenceDirectives), ["libReferences"] = Value(file.LibReferenceDirectives),
                ["mappedSource"] = MappedSource(observer, file)
            });
        JsonArray aliases = [];
        foreach (var pair in program.FilesByPath().OrderBy(pair => pair.Key, StringComparer.Ordinal))
            aliases.Add((JsonNode)new JsonObject { ["pathBase64"] = Bytes(pair.Key), ["file"] = observer.File(pair.Value) });

        var modules = new List<(string Path, string Name, ResolutionMode Mode, JsonObject Row)>();
        program.ForEachResolvedModule((resolution, name, mode, path) => modules.Add((path.AsString(), name, mode,
            new JsonObject { ["pathBase64"] = Bytes(path.AsString()), ["nameBase64"] = Bytes(name), ["mode"] = (int)mode,
                ["result"] = observer.Module(resolution), ["target"] = resolution.IsResolved() ? observer.File(program.GetSourceFileForResolvedModule(resolution)) : null })));
        var directives = new List<(string Path, string Name, ResolutionMode Mode, JsonObject Row)>();
        program.ForEachResolvedTypeReferenceDirective((resolution, name, mode, path) => directives.Add((path.AsString(), name, mode,
            new JsonObject { ["pathBase64"] = Bytes(path.AsString()), ["nameBase64"] = Bytes(name), ["mode"] = (int)mode,
                ["result"] = observer.Directive(resolution), ["target"] = resolution is not null && resolution.IsResolved() ? observer.File(program.GetSourceFileByPath(resolution.ResolvedPath)) : null })));
        static JsonArray Associations(List<(string Path, string Name, ResolutionMode Mode, JsonObject Row)> rows)
        {
            JsonArray result = [];
            foreach (var row in rows.OrderBy(row => row.Path, StringComparer.Ordinal).ThenBy(row => row.Name, StringComparer.Ordinal).ThenBy(row => row.Mode))
                result.Add((JsonNode)row.Row);
            return result;
        }

        JsonArray references = [];
        foreach (var config in program.GetResolvedProjectReferences() ?? [])
            references.Add((JsonNode?)(config is null ? null : new JsonObject { ["configBase64"] = Bytes(config.ConfigFileName().AsString()),
                ["roots"] = Value(config.FileNames()), ["options"] = Value(config.CompilerOptions()), ["references"] = Value(config.ProjectReferences()) }));
        JsonArray referenceTraversal = [];
        program.RangeResolvedProjectReference((path, config, parent, index) =>
        {
            referenceTraversal.Add((JsonNode)new JsonObject { ["pathBase64"] = Bytes(path.AsString()),
                ["configBase64"] = config is null ? null : Bytes(config.ConfigFileName().AsString()),
                ["parentBase64"] = Bytes(parent.ConfigFileName().AsString()), ["index"] = index });
            return true;
        });
        using var explain = new MemoryStream();
        program.ExplainFiles(explain, TypeScript.Locale.Locales.Default, RootedDirectoryPath.FromGoString("/"));
        return new JsonObject
        {
            ["files"] = files, ["filesByPath"] = aliases, ["duplicateSources"] = Value(program.DuplicateSourceFiles()), ["options"] = Value(program.Options()), ["roots"] = Value(program.CommandLine().FileNames()),
            ["configDiagnostics"] = observer.Diagnostics(program.GetConfigFileParsingDiagnostics()), ["programDiagnostics"] = observer.Diagnostics(program.GetProgramDiagnostics()),
            ["syntacticDiagnostics"] = observer.Diagnostics(program.GetSyntacticDiagnostics()), ["bindDiagnostics"] = bind ? observer.Diagnostics(program.GetBindDiagnostics()) : null,
            ["modules"] = Associations(modules), ["typeDirectives"] = Associations(directives), ["references"] = references, ["referenceTraversal"] = referenceTraversal,
            ["commonSourceDirectory"] = Value(program.CommonSourceDirectory()),
            ["unresolvedImports"] = Value(program.GetUnresolvedImports().OrderBy(value => value, StringComparer.Ordinal).ToList()),
            ["resolvedPackages"] = Value(program.ResolvedPackageNames().OrderBy(value => value, StringComparer.Ordinal).ToList()),
            ["unresolvedPackages"] = Value(program.UnresolvedPackageNames().OrderBy(value => value, StringComparer.Ordinal).ToList()),
            ["deepImports"] = Value(program.DeepImportPackageNames().OrderBy(value => value, StringComparer.Ordinal).ToList()),
            ["explainFilesBase64"] = Convert.ToBase64String(explain.ToArray()), ["moduleResolutionErrorBase64"] = program.ModuleResolutionError() is Exception error ? Bytes(error.Message) : null
        };
    }

    private static void Keys(JsonObject value, params string[] allowed)
    {
        foreach (var pair in value)
            if (!allowed.Contains(pair.Key, StringComparer.Ordinal)) throw new ArgumentException("Unknown Program request field: " + pair.Key);
    }

    public static string Run(string line)
    {
        var request = (JsonObject)JsonNode.Parse(line)!;
        Keys(request, "Name", "ConfigPath", "Files", "CaseInsensitive", "UseSourceOfProjectReference", "SkipModuleResolution", "SingleThreaded", "TypingsLocation", "Bind", "Changes", "Dependency", "ResolverDecorator", "MapperProject");
        if (request["ResolverDecorator"] is not null &&
            (request["SingleThreaded"]?.GetValue<bool>() != true || request["Changes"] is JsonArray { Count: > 0 }))
            throw new ArgumentException("Authored resolver callback host requires single-threaded fresh construction");
        if (request["MapperProject"] is not null && request["Changes"] is JsonArray { Count: > 0 })
            throw new ArgumentException("Authored mapper callback fixture requires fresh construction");
        if (request["Dependency"] is JsonObject dependency) return ObserveDependency(request["Name"]!.GetValue<string>(), dependency).ToJsonString();
        var sensitivity = request["CaseInsensitive"]?.GetValue<bool>() == true ? CaseSensitivity.CaseInsensitive : CaseSensitivity.CaseSensitive;
        var inputs = new Dictionary<string, ConfigurationFileSystem.Input>(StringComparer.Ordinal);
        void AddFiles(JsonArray? files, bool replace)
        {
            foreach (JsonObject file in (files ?? []).Cast<JsonObject>())
            {
                Keys(file, "Path", "Text", "PathBase64", "TextBase64", "Symlink");
                string path = Input(file, "Path");
                var input = new ConfigurationFileSystem.Input(path, Input(file, "Text"), file["Symlink"] is null ? null : Input(file, "Symlink"));
                if (!replace && inputs.ContainsKey(path)) throw new ArgumentException("Duplicate fixture file");
                inputs[path] = input;
            }
        }
        AddFiles((JsonArray?)request["Files"], false);
        IFS FS() => TypeScript.Bundled.Bundled.WrapFS(new ConfigurationFileSystem(inputs.Values, sensitivity));
        var fs = FS();
        var (parsed, errors) = T.GetParsedCommandLineOfConfigFile(RootedFilePath.FromGoString(Input(request, "ConfigPath")), null, null, fs, null);
        var mapperProject = ConfigureMapperProject(parsed, request["MapperProject"] as JsonObject);
        var observer = new Observer();
        JsonArray traces = [], snapshots = [], changes = [];
        ICompilerHost host = CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath(), null, (message, arguments) =>
            traces.Add((JsonNode)new JsonObject { ["code"] = message.Code(), ["key"] = message.Key().ToString(), ["templateBase64"] = Bytes(message.String()), ["args"] = Value(arguments) }), mapperProject);
        Func<ResolverOptions, IResolver>? resolverFactory = null;
        if (request["ResolverDecorator"] is JsonObject decorator)
        {
            Keys(decorator, "ModuleDiagnostics", "TypeDiagnostics", "Error");
            var captured = new CaptureSourceHost(host);
            host = captured;
            resolverFactory = options => new DecoratedResolver(DefaultResolver.NewResolver(options), captured, decorator);
        }
        var result = new JsonObject { ["name"] = request["Name"]?.GetValue<string>(), ["readDiagnostics"] = observer.Diagnostics(errors), ["snapshots"] = snapshots, ["changes"] = changes };
        if (parsed is not null)
        {
            bool bind = request["Bind"]?.GetValue<bool>() ?? false;
            var program = CompilerProgram.NewProgram(new ProgramOptions
            {
                Config = parsed, Host = host, UseSourceOfProjectReference = request["UseSourceOfProjectReference"]?.GetValue<bool>() ?? false,
                SkipModuleResolution = request["SkipModuleResolution"]?.GetValue<bool>() ?? false,
                SingleThreaded = request["SingleThreaded"] is null ? Tristate.Unknown : request["SingleThreaded"]!.GetValue<bool>() ? Tristate.True : Tristate.False,
                TypingsLocation = RootedDirectoryPath.FromGoString(Input(request, "TypingsLocation")),
                CreateModuleResolver = resolverFactory
            });
            snapshots.Add((JsonNode)Snapshot(observer, program, bind));
            foreach (JsonObject change in ((JsonArray?)request["Changes"] ?? []).Cast<JsonObject>())
            {
                Keys(change, "Kind", "File", "Files");
                AddFiles((JsonArray?)change["Files"], true);
                var newHost = CompilerHosts.NewCompilerHost(FS(), TypeScript.Bundled.Bundled.LibPath());
                var old = program;
                var path = sensitivity.PathKey(RootedPath.FromGoString(Input(change, "File")));
                string kind = change["Kind"]!.GetValue<string>();
                var changed = kind switch
                {
                    "reuse" => program.ReuseProgram(path, newHost),
                    "update" => program.UpdateProgram(path, newHost),
                    _ => throw new ArgumentException("Unknown lifecycle operation")
                };
                changes.Add((JsonNode)new JsonObject { ["kind"] = kind, ["reused"] = changed.Reused, ["changedFile"] = observer.File(changed.NewFile),
                    ["programReturned"] = changed.Program is not null, ["sameFileNames"] = changed.Program is not null && changed.Program.HasSameFileNames(old) });
                if (changed.Program is not null) { program = changed.Program; snapshots.Add((JsonNode)Snapshot(observer, program, bind)); }
            }
        }
        result["owners"] = observer.Owners.Count == 0 ? null : observer.Owners;
        result["traces"] = traces;
        return result.ToJsonString();
    }
}
