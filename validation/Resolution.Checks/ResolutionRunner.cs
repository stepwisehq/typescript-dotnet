// Authored actual API observer. Stored values only; unknown shapes fail closed.
// Raw Go-byte roles use base64, without display/path normalization or reflection.
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using TypeScript.Collections;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.Vfs;
using T = TypeScript.TsOptions.TsOptions;
using VFileInfo = TypeScript.Vfs.FileInfo;

namespace Resolution.Checks;
public static class ResolutionRunner
{
    private static void Keys(JsonObject value, params string[] allowed)
    {
        foreach (var field in value)
            if (!allowed.Contains(field.Key, StringComparer.Ordinal))
                throw new InvalidOperationException("Unknown resolution request field: " + field.Key);
    }

    private static void Validate(JsonObject request)
    {
        Keys(request, "Name", "Cwd", "ConfigPath", "ConfigText", "CaseInsensitive", "Graph", "ExtraExtensions", "TypingsLocation", "RawCompilerOptions", "Redirects", "Static", "Files", "Queries");
        foreach (JsonObject file in ((JsonArray? )request["Files"] ?? []).Cast<JsonObject>())
            Keys(file, "Path", "Text", "PathBase64", "TextBase64", "Symlink");
        foreach (JsonObject query in ((JsonArray? )request["Queries"] ?? []).Cast<JsonObject>())
        {
            Keys(query, "Kind", "Name", "Containing", "NameBase64", "ContainingBase64", "Mode", "Other", "Extension", "ExtraExtension", "EnableDirectorySearch", "RedirectId");
            int mode = query["Mode"]?.GetValue<int>() ?? 0;
            if (mode is not (0 or 1 or 99))
                throw new InvalidOperationException("Unknown resolution mode");
        }

        foreach (JsonObject redirect in ((JsonArray? )request["Redirects"] ?? []).Cast<JsonObject>())
            Keys(redirect, "Id", "ConfigPath", "ConfigText");
        if (request["Static"] is JsonObject overrides)
        {
            Keys(overrides, "FallbackToResolver", "Entries");
            foreach (JsonObject entry in ((JsonArray? )overrides["Entries"] ?? []).Cast<JsonObject>())
            {
                Keys(entry, "ModuleName", "ContainingDirectory", "Mode", "Result");
                int mode = entry["Mode"]?.GetValue<int>() ?? 0;
                if (mode is not (0 or 1 or 99))
                    throw new InvalidOperationException("Unknown static resolution mode");
                if (entry["Result"] is JsonObject result)
                {
                    Keys(result, "ResolutionDiagnostics", "ResolvedFileName", "ResolvedPath", "OriginalPath", "Extension", "ResolvedUsingTsExtension", "ResolvedUsingExtraExtensions", "IsExternalLibraryImport", "IsCustomResolution", "PackageId", "AlternateResult");
                    if (result["PackageId"] is JsonObject id)
                        Keys(id, "Name", "SubModuleName", "Version", "PeerDependencies");
                }
            }
        }

        if (request["Graph"]?.GetValue<bool>() == true && (((JsonArray? )request["Queries"])?.Count > 0 || request["Static"] is not null || request["Redirects"] is not null || request["ExtraExtensions"] is not null || request["TypingsLocation"] is not null))
            throw new InvalidOperationException("Resolver-only inputs supplied for graph observation");
    }

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
        if (value is ByteStringValue bytes) return Value(bytes.Materialize());
        if (value is Slice<ByteStringValue> byteValues) return Value(GoSlices.ToListOrNull(byteValues));
        if (value is Slice<FileReference> references) return Value(GoSlices.ToListOrNull(references));
        if (value is Slice<object?> arguments) return Value(GoSlices.ToListOrNull(arguments));
        if (value is Slice<RootedFilePath> filePaths) return Value(GoSlices.ToListOrNull(filePaths));
        if (value is Slice<string> texts) return Value(GoSlices.ToListOrNull(texts));
        if (value is Slice<ResolvedEntrypoint> entrypoints) return Value(GoSlices.ToListOrNull(entrypoints));
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
        if (value is FileReference reference)
            return Object(("FileName", reference.FileName), ("ResolutionMode", reference.ResolutionMode), ("Preserve", reference.Preserve), ("pos", reference.Pos()), ("end", reference.End()));
        if (value is PluginImport plugin)
            return Object(("Name", plugin.Name));
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
                entries.Add((entry.Key is ByteStringKey entryKey ? entryKey.ToString() : entry.Key as string ?? throw new InvalidOperationException("Unobserved stored map key"), entry.Value));
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
                Owners.Add((JsonNode)new JsonObject { ["id"] = id, ["nameBase64"] = Bytes(file.FileName()), ["pathBase64"] = Bytes(file.PathKey()), ["textBase64"] = Bytes(file.Text()) });
            }

            return id;
        }

        internal JsonNode? Diagnostics(Slice<Diagnostic> diagnostics) => Diagnostics(GoSlices.ToListOrNull(diagnostics));
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

    private static JsonNode? Traces(Slice<DiagAndArgs> traces) => Traces(GoSlices.ToListOrNull(traces));
    private static JsonNode? Traces(List<DiagAndArgs>? traces)
    {
        if (traces is null)
            return null;
        JsonArray result = [];
        foreach (var trace in traces)
            result.Add((JsonNode)new JsonObject { ["code"] = trace.Message.Code(), ["category"] = (int)trace.Message.Category(), ["key"] = trace.Message.Key().ToString(), ["templateBase64"] = Bytes(trace.Message.String()), ["args"] = Value(trace.Args) });
        return result;
    }

    private sealed class Host : IFS, IResolutionHost
    {
        private readonly IFS fileSystem;
        private readonly RootedDirectoryPath cwd;
        private readonly Dictionary<PathKey, RootedPath> realPaths = [];
        private static readonly System.Text.UTF8Encoding StrictUTF8 = new(false, true);
        internal Host(IFS fileSystem, RootedDirectoryPath cwd, IEnumerable<Configuration.Checks.ConfigurationFileSystem.Input> inputs)
        {
            this.fileSystem = fileSystem;
            this.cwd = cwd;
            // Original vfstest creates entries by path components and retains
            // the first stored parent spelling for case-insensitive Realpath.
            foreach (var input in inputs.OrderBy(file => file.Path, Comparer<string>.Create(CompareParts)))
            {
                int rootLength = TsPath.GetRootLength(input.Path);
                string stored = input.Path[..rootLength];
                var root = RootedPath.FromGoString(stored);
                realPaths.TryAdd(CaseSensitivity().PathKey(root), root);
                foreach (string part in input.Path[rootLength..].Split('/', StringSplitOptions.RemoveEmptyEntries))
                {
                    stored = stored.EndsWith('/') ? stored + part : stored + "/" + part;
                    var path = RootedPath.FromGoString(stored);
                    var key = CaseSensitivity().PathKey(path);
                    realPaths.TryAdd(key, path);
                }

                var leaf = RootedPath.FromGoString(input.Path);
                realPaths[CaseSensitivity().PathKey(leaf)] = leaf;
            }
        }

        private static int CompareParts(string left, string right)
        {
            for (;;)
            {
                int a = left.IndexOf('/'), b = right.IndexOf('/');
                if (a < 0 || b < 0)
                    return StringComparer.Ordinal.Compare(left, right);
                int order = StringComparer.Ordinal.Compare(left[..a], right[..b]);
                if (order != 0)
                    return order;
                left = left[(a + 1)..];
                right = right[(b + 1)..];
            }
        }

        private static bool ValidPath(string path)
        {
            // Go io/fs.ValidPath rejects invalid UTF-8 in this backing host.
            // Pure typed-path APIs and the inherited authored host remain raw.
            try
            {
                StrictUTF8.GetCharCount(GoString.Bytes(path));
                return true;
            }
            catch (System.Text.DecoderFallbackException)
            {
                return false;
            }
        }

        internal readonly JsonArray Operations = [];
        private U Record<U>(string kind, string path, U result, JsonNode? projection)
        {
            Operations.Add((JsonNode)new JsonObject { ["kind"] = kind, ["pathBase64"] = Bytes(path), ["result"] = projection });
            return result;
        }

        public IFS FS() => this;
        public RootedDirectoryPath GetCurrentDirectory() => cwd;
        public CaseSensitivity CaseSensitivity() => fileSystem.CaseSensitivity();
        public bool FileExists(RootedFilePath path)
        {
            bool result = ValidPath(path.AsString()) && fileSystem.FileExists(path);
            return Record("fileExists", path.AsString(), result, Value(result));
        }

        public bool DirectoryExists(RootedDirectoryPath path)
        {
            bool result = ValidPath(path.AsString()) && fileSystem.DirectoryExists(path);
            return Record("directoryExists", path.AsString(), result, Value(result));
        }

        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            var result = ValidPath(path.AsString()) ? fileSystem.ReadFile(path) : (Contents: "", Ok: false);
            return Record("readFile", path.AsString(), result, new JsonObject { ["ok"] = result.Ok, ["textBase64"] = Bytes(result.Contents) });
        }

        public RootedPath Realpath(RootedPath path)
        {
            var result = ValidPath(path.AsString()) ? fileSystem.Realpath(path) : path;
            if (realPaths.TryGetValue(CaseSensitivity().PathKey(result), out var stored))
                result = stored;
            return Record("realpath", path.AsString(), result, Value(result));
        }

        public VFileInfo? Stat(RootedPath path)
        {
            var result = ValidPath(path.AsString()) ? fileSystem.Stat(path) : null;
            return Record("stat", path.AsString(), result, result is null ? null : Object(("name", result.Name()), ("size", result.Size()), ("mode", (uint)result.Mode()), ("isDir", result.IsDir())));
        }

        public Entries GetAccessibleEntries(RootedDirectoryPath path)
        {
            var result = ValidPath(path.AsString()) ? fileSystem.GetAccessibleEntries(path) : new Entries();
            if (result.Files.Count == 0)
                result.Files = default;
            if (result.Directories.Count == 0)
                result.Directories = default;
            return Record("accessibleEntries", path.AsString(), result, Object(("files", GoSlices.ToListOrNull(result.Files)?.Select(static name => name.Materialize()!).ToList()), ("directories", GoSlices.ToListOrNull(result.Directories)?.Select(static name => name.Materialize()!).ToList()), ("symlinks", result.Symlinks?.Select(static name => name.ToString()).OrderBy(name => name, StringComparer.Ordinal).ToList())));
        }
    }

    private static JsonNode? Utility(string name, string containing, JsonObject query, CompilerOptions options, Host host, Observer observer)
    {
        switch (name)
        {
            case "js-extension":
                return Value(ModuleUtilities.TryGetJSExtensionForFile(containing, options));
            case "js-extension-typed":
                return Value(ModuleUtilities.TryGetJSExtensionForFileName(RootedFilePath.FromGoString(containing), options));
            case "versioned-types-key":
                return Value(ModuleUtilities.IsApplicableVersionedTypesKey(containing));
            case "parse-package-name":
                var package = ModuleUtilities.ParsePackageName(containing);
                return new JsonObject
                {
                    ["packageName"] = Value(package.PackageName),
                    ["remaining"] = Value(package.Remaining)
                };
            case "mangle-package-name":
                return Value(ModuleUtilities.MangleScopedPackageName(containing));
            case "unmangle-package-name":
                return Value(ModuleUtilities.UnmangleScopedPackageName(containing));
            case "types-package-name":
                return Value(ModuleUtilities.GetTypesPackageName(containing));
            case "package-from-types":
                return Value(ModuleUtilities.GetPackageNameFromTypesPackageName(containing));
            case "compare-patterns":
                return Value(ModuleUtilities.ComparePatternKeys(containing, Input(query, "Other")));
            case "automatic-type-names":
                return Value(DefaultResolver.GetAutomaticTypeDirectiveNames(options, host.GetCurrentDirectory(), host));
            case "conditions":
                return Value(DefaultResolver.GetConditions(options, (ResolutionMode)(query["Mode"]?.GetValue<int>() ?? 0)));
            case "resolution-diagnostic":
                SourceFile file = Parser.ParseSourceFile(new SourceFileParseOptions(containing, host.CaseSensitivity().PathKey(RootedPath.FromGoString(containing)).AsString(), FileNameIsGoBytes: true), Array.Empty<byte>(), ScriptKind.TS);
                Message? message = ModuleUtilities.GetResolutionDiagnostic(options, new ResolvedModule { Extension = Input(query, "Extension"), ResolvedUsingExtraExtensions = query["ExtraExtension"]?.GetValue<bool>() ?? false }, file);
                return message is null ? null : new JsonObject
                {
                    ["code"] = message.Code(),
                    ["category"] = (int)message.Category(),
                    ["key"] = message.Key(),
                    ["templateBase64"] = Bytes(message.String())
                };
            case "binder-source-file":
                SourceFile source = Parser.ParseSourceFile(new SourceFileParseOptions(containing, host.CaseSensitivity().PathKey(RootedPath.FromGoString(containing)).AsString(), FileNameIsGoBytes: true), GoString.Bytes(Input(query, "Other")), ScriptKind.TS);
                Binder.BindSourceFile(source);
                return new JsonObject
                {
                    ["fileName"] = Value(source.FileName()),
                    ["path"] = Value(source.PathKey()),
                    ["moduleSymbolName"] = source.Symbol is null ? null : Value(source.Symbol.Name),
                    ["parseDiagnostics"] = observer.Diagnostics(source.Diagnostics())
                };
            default:
                throw new InvalidOperationException("Unknown actual Module utility API: " + name);
        }
    }

    private static JsonNode Graph(TypeScript.TsOptions.ParsedCommandLine parsed, Host host, Observer observer)
    {
        var graph = ResolvedProjectGraph.FromParsedConfiguration(parsed, host);
        JsonArray files = [], edges = [], paths = [], types = [], externalLibs = [];
        Dictionary<SourceFile, int> ids = new(ReferenceEqualityComparer.Instance);
        foreach (SourceFile file in graph.SourceFiles)
        {
            int id = observer.File(file)!.Value;
            ids.Add(file, id);
            files.Add((JsonNode)new JsonObject { ["identity"] = id, ["fileName"] = Value(file.FileName()), ["path"] = Value(file.PathKey()), ["text"] = Value(file.Text()), ["scriptKind"] = (int)file.ScriptKind, ["isDeclarationFile"] = file.IsDeclarationFile, ["externalModule"] = file.ExternalModuleIndicator is not null, ["parseDiagnostics"] = observer.Diagnostics(file.Diagnostics()), ["metadata"] = Value(graph.GetSourceFileMetaData(file)) });
        }

        Dictionary<ResolvedModule, int> resolutions = new(ReferenceEqualityComparer.Instance);
        foreach (var edge in graph.ResolutionEdges.OrderBy(edge => ids[edge.ImportingFile]).ThenBy(edge => edge.ImportIndex))
        {
            if (!resolutions.TryGetValue(edge.Resolution, out int id))
            {
                id = resolutions.Count;
                resolutions.Add(edge.Resolution, id);
            }

            edges.Add((JsonNode)new JsonObject { ["file"] = observer.File(edge.ImportingFile), ["specifier"] = new JsonObject { ["kind"] = (int)edge.ModuleSpecifier.Kind, ["pos"] = edge.ModuleSpecifier.Pos(), ["end"] = edge.ModuleSpecifier.End() }, ["reference"] = Value(edge.ModuleReference), ["mode"] = (int)edge.Mode, ["importIndex"] = edge.ImportIndex, ["syntheticKind"] = edge.SyntheticKind == "jsxRuntime" ? "jsx-runtime" : edge.SyntheticKind, ["identity"] = id, ["resolution"] = observer.Module(edge.Resolution), ["trace"] = Traces(edge.Traces), ["errorBase64"] = edge.Error is null ? null : Bytes(GoString.ToUtf8(edge.Error.Message)), ["scheduling"] = edge.Scheduling.ToString(), ["target"] = observer.File(edge.TargetFile) });
        }

        foreach (var edge in graph.PathReferences.OrderBy(edge => ids[edge.ImportingFile]).ThenBy(edge => edge.Reference.Pos()))
            paths.Add((JsonNode)new JsonObject { ["file"] = observer.File(edge.ImportingFile), ["reference"] = Value(edge.Reference), ["resolvedFile"] = Value(edge.ResolvedFileName), ["resolvedPath"] = Value(edge.ResolvedPath), ["diagnostic"] = observer.Diagnostics(edge.Diagnostic is null ? null : new[] { edge.Diagnostic }), ["scheduling"] = edge.Scheduling.ToString(), ["target"] = observer.File(edge.TargetFile) });
        foreach (var edge in graph.TypeDirectives.OrderBy(edge => ids[edge.ImportingFile]).ThenBy(edge => edge.Reference.Pos()))
            types.Add((JsonNode)new JsonObject { ["file"] = observer.File(edge.ImportingFile), ["reference"] = Value(edge.Reference), ["mode"] = (int)edge.Mode, ["resolution"] = observer.Directive(edge.Resolution), ["trace"] = Traces(edge.Traces), ["scheduling"] = edge.Scheduling.ToString(), ["target"] = observer.File(edge.TargetFile) });
        foreach (SourceFile file in graph.SourceFiles)
            foreach (var reference in file.LibReferenceDirectives)
                externalLibs.Add((JsonNode)new JsonObject { ["file"] = observer.File(file), ["reference"] = Value(reference), ["scheduling"] = "ExternalLibraryEnvironment" });
        return new JsonObject
        {
            ["roots"] = Value(graph.RootFileNames),
            ["files"] = files,
            ["edges"] = edges,
            ["pathReferences"] = paths,
            ["typeDirectives"] = types,
            ["externalLibReferences"] = externalLibs,
            ["missingFiles"] = Value(graph.MissingFiles.OrderBy(file => file.AsString(), StringComparer.Ordinal).ToList()),
            ["inclusionDiagnostics"] = observer.Diagnostics(graph.InclusionDiagnostics.Count == 0 ? null : graph.InclusionDiagnostics)
        };
    }

    private sealed record Redirect(RootedFilePath Name, CompilerOptions Options) : IResolvedProjectReference
    {
        public RootedFilePath ConfigName() => Name;
        public CompilerOptions CompilerOptions() => Options;
    }

    private static ResolvedModule? StaticResult(JsonNode? input)
    {
        if (input is null)
            return null;
        JsonObject row = (JsonObject)input;
        if (row["ResolutionDiagnostics"] is not null)
            throw new InvalidOperationException("Unobserved static diagnostic input constructor");
        JsonObject id = row["PackageId"] as JsonObject ?? [];
        return new ResolvedModule
        {
            ResolvedFileName = RootedFilePath.FromGoString(Input(row, "ResolvedFileName")),
            ResolvedPath = PathKey.FromGoString(Input(row, "ResolvedPath")),
            OriginalPath = RootedFilePath.FromGoString(Input(row, "OriginalPath")),
            Extension = Input(row, "Extension"),
            AlternateResult = RootedFilePath.FromGoString(Input(row, "AlternateResult")),
            ResolvedUsingTsExtension = row["ResolvedUsingTsExtension"]?.GetValue<bool>() ?? false,
            ResolvedUsingExtraExtensions = row["ResolvedUsingExtraExtensions"]?.GetValue<bool>() ?? false,
            IsExternalLibraryImport = row["IsExternalLibraryImport"]?.GetValue<bool>() ?? false,
            IsCustomResolution = row["IsCustomResolution"]?.GetValue<bool>() ?? false,
            PackageId = new PackageId
            {
                Name = Input(id, "Name"),
                SubModuleName = Input(id, "SubModuleName"),
                Version = Input(id, "Version"),
                PeerDependencies = Input(id, "PeerDependencies")
            }
        };
    }

    public static string Run(string line)
    {
        JsonObject request = JsonNode.Parse(line) as JsonObject ?? throw new InvalidOperationException("Expected request object");
        Validate(request);
        var inputs = ((JsonArray? )request["Files"] ?? []).Select(item =>
        {
            var file = (JsonObject)item!;
            return new Configuration.Checks.ConfigurationFileSystem.Input(Input(file, "Path"), Input(file, "Text"), file["Symlink"] is null ? null : Input(file, "Symlink"));
        }).ToList();
        var sensitivity = request["CaseInsensitive"]?.GetValue<bool>() == true ? CaseSensitivity.CaseInsensitive : CaseSensitivity.CaseSensitive;
        Host host = new(new Configuration.Checks.ConfigurationFileSystem(inputs, sensitivity), RootedDirectoryPath.FromGoString(Input(request, "Cwd")), inputs);
        RootedFilePath config = RootedFilePath.FromGoString(Input(request, "ConfigPath"));
        TypeScript.TsOptions.ParsedCommandLine? parsed = null;
        Slice<Diagnostic> errors = default, configErrors = default;
        CompilerOptions options;
        Exception? rawError = null;
        bool graphRequest = request["Graph"]?.GetValue<bool>() == true;
        if (graphRequest)
        {
            var graphConfig = T.GetParsedCommandLineOfConfigFile(config, null, null, host);
            parsed = graphConfig.Item1 ?? throw new InvalidOperationException("Graph configuration did not parse");
            errors = graphConfig.Item2;
        }

        if (request["RawCompilerOptions"] is not null)
        {
            TypeScript.TsOptions.RawCompilerOptions rawOptions = new();
            rawError = rawOptions.UnmarshalJSON(Input(request, "RawCompilerOptions"));
            var finalized = rawOptions.Finalize(host.GetCurrentDirectory());
            options = finalized.Item1;
            configErrors = finalized.Item2;
            if (graphRequest)
            {
                if (rawError is not null)
                    throw rawError;
                parsed!.SetCompilerOptions(options);
            }
        }
        else if (graphRequest)
        {
            options = parsed!.CompilerOptions();
            configErrors = parsed.Errors;
        }
        else
        {
            var rawConfig = T.ParseConfigFileTextToJson(config, sensitivity.PathKey(config.AsPath()), Input(request, "ConfigText"));
            errors = rawConfig.Item2;
            parsed = T.ParseJsonConfigFileContent(rawConfig.Item1, host, config.Directory(), null, config, null, null);
            options = parsed.CompilerOptions();
            configErrors = parsed.Errors;
        }

        if (graphRequest)
        {
            Observer graphObserver = new();
            JsonNode graph = Graph(parsed!, host, graphObserver);
            return new JsonObject
            {
                ["name"] = request["Name"]!.GetValue<string>(),
                ["options"] = Value(options),
                ["parseDiagnostics"] = graphObserver.Diagnostics(errors),
                ["configDiagnostics"] = graphObserver.Diagnostics(configErrors),
                ["graph"] = graph,
                ["hostOperations"] = host.Operations.Count == 0 ? null : host.Operations,
                ["diagnosticOwners"] = graphObserver.Owners
            }.ToJsonString();
        }

        List<string>? extras = request["ExtraExtensions"] is JsonArray extensions ? extensions.Select(extension => GoString.ToUtf8(extension!.GetValue<string>())).ToList() : null;
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = host, CompilerOptions = options, ExtraExtensions = extras, TypingsLocation = RootedDirectoryPath.FromGoString(Input(request, "TypingsLocation")) });
        IResolver active = resolver;
        Exception? staticError = null;
        if (request["Static"] is JsonObject overrides)
        {
            List<StaticResolutionEntry> staticEntries = [];
            foreach (JsonObject entry in ((JsonArray? )overrides["Entries"] ?? []).Cast<JsonObject>())
                staticEntries.Add(new StaticResolutionEntry { ModuleName = Input(entry, "ModuleName"), ContainingDirectory = RootedDirectoryPath.FromGoString(Input(entry, "ContainingDirectory")), ResolutionMode = entry["Mode"] is null ? null : new StrongBox<ResolutionMode>((ResolutionMode)entry["Mode"]!.GetValue<int>()), Result = StaticResult(entry["Result"]) });
            var result = StaticResolutions.NewStaticResolutions(staticEntries, overrides["FallbackToResolver"]?.GetValue<bool>() ?? false, host.GetCurrentDirectory(), sensitivity);
            staticError = result.Error;
            if (result.Result is not null)
                active = StaticResolver.NewStaticResolver(resolver, result.Result);
            else if (((JsonArray? )request["Queries"])?.Count > 0)
                throw new InvalidOperationException("Queries supplied for invalid static factory");
        }

        Observer observer = new();
        JsonArray results = [], redirectRows = [];
        Dictionary<object, int> identities = new(ReferenceEqualityComparer.Instance);
        Dictionary<string, Redirect> redirects = [];
        foreach (JsonObject entry in ((JsonArray? )request["Redirects"] ?? []).Cast<JsonObject>())
        {
            RootedFilePath path = RootedFilePath.FromGoString(Input(entry, "ConfigPath"));
            var raw = T.ParseConfigFileTextToJson(path, sensitivity.PathKey(path.AsPath()), Input(entry, "ConfigText"));
            var target = T.ParseJsonConfigFileContent(raw.Item1, host, path.Directory(), null, path, null, null);
            string id = entry["Id"]!.GetValue<string>();
            redirects.Add(id, new Redirect(target.ConfigName(), target.CompilerOptions()));
            redirectRows.Add((JsonNode)new JsonObject { ["id"] = id, ["configName"] = Value(target.ConfigName()), ["options"] = Value(target.CompilerOptions()), ["parseDiagnostics"] = observer.Diagnostics(raw.Item2), ["configDiagnostics"] = observer.Diagnostics(target.Errors) });
        }

        foreach (JsonObject query in ((JsonArray? )request["Queries"] ?? []).Cast<JsonObject>())
        {
            string kind = query["Kind"]!.GetValue<string>(), name = Input(query, "Name"), containing = Input(query, "Containing");
            var mode = (ResolutionMode)(query["Mode"]?.GetValue<int>() ?? 0);
            JsonNode? result;
            object? identity;
            Slice<DiagAndArgs> traces;
            Exception? error = null;
            IResolvedProjectReference? redirect = query["RedirectId"] is JsonValue redirectId ? redirects[redirectId.GetValue<string>()] : null;
            switch (kind)
            {
                case "config":
                    var configResult = DefaultResolver.ResolveConfig(name, RootedFilePath.FromGoString(containing), host);
                    result = observer.Module(configResult);
                    identity = configResult;
                    traces = default;
                    break;
                case "package-directory":
                    var packageDirectory = resolver.ResolvePackageDirectory(name, RootedFilePath.FromGoString(containing), mode, redirect);
                    result = observer.Module(packageDirectory);
                    identity = packageDirectory;
                    traces = default;
                    break;
                case "entrypoints":
                    InfoCacheEntry? packageEntry = null;
                    resolver.PackageJsonCacheEntries((key, item) =>
                    {
                        if (item.PackageDirectory.AsDirectoryPath() == RootedDirectoryPath.FromGoString(containing))
                        {
                            if (packageEntry is not null)
                                throw new InvalidOperationException("Ambiguous actual package entry");
                            packageEntry = item;
                        }

                        return true;
                    });
                    if (packageEntry is null)
                        throw new InvalidOperationException("Unobserved actual package entry");
                    result = Value(resolver.GetEntrypointsFromPackageJsonInfo(packageEntry, name, query["EnableDirectorySearch"]?.GetValue<bool>() ?? false));
                    identity = null;
                    traces = default;
                    break;
                case "utility":
                    result = Utility(name, containing, query, options, host, observer);
                    identity = null;
                    traces = default;
                    break;
                case "module":
                    var module = active.ResolveModuleName(name, RootedFilePath.FromGoString(containing), mode, redirect);
                    result = observer.Module(module.Result);
                    identity = module.Result;
                    traces = module.Traces;
                    error = module.Error;
                    break;
                case "directory":
                    var directory = active.ResolveModuleNameFromDirectory(name, RootedDirectoryPath.FromGoString(containing), mode);
                    result = observer.Module(directory.Result);
                    identity = directory.Result;
                    traces = directory.Traces;
                    error = directory.Error;
                    break;
                case "type":
                    var directive = active.ResolveTypeReferenceDirective(name, RootedFilePath.FromGoString(containing), mode, redirect);
                    result = observer.Directive(directive.Result);
                    identity = directive.Result;
                    traces = directive.Traces;
                    break;
                default:
                    throw new InvalidOperationException("Unobserved actual resolver operation: " + kind);
            }

            int? id = null;
            if (identity is not null)
            {
                if (!identities.TryGetValue(identity, out int existing))
                {
                    existing = identities.Count;
                    identities.Add(identity, existing);
                }

                id = existing;
            }

            results.Add((JsonNode)new JsonObject { ["kind"] = kind, ["name"] = Value(name), ["containing"] = Value(containing), ["mode"] = (int)mode, ["identity"] = id, ["result"] = result, ["trace"] = Traces(traces), ["errorBase64"] = error is null ? null : Bytes(GoString.ToUtf8(error.Message)) });
        }

        Dictionary<string, InfoCacheEntry> cache = [];
        resolver.PackageJsonCacheEntries((key, entry) =>
        {
            cache.Add(key.AsString(), entry);
            return true;
        });
        JsonArray cacheRows = [];
        Dictionary<InfoCacheEntry, int> entries = new(ReferenceEqualityComparer.Instance);
        Dictionary<PackageJson, int> contents = new(ReferenceEqualityComparer.Instance);
        foreach (var(key, entry)in cache.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!entries.TryGetValue(entry, out int id))
            {
                id = entries.Count;
                entries.Add(entry, id);
            }

            int? contentId = null;
            JsonNode? content = null;
            if (entry.Contents is not null)
            {
                if (!contents.TryGetValue(entry.Contents, out int existing))
                {
                    existing = contents.Count;
                    contents.Add(entry.Contents, existing);
                }

                contentId = existing;
                content = new JsonObject
                {
                    ["parseable"] = entry.Contents.Parseable,
                    ["fields"] = Value(entry.Contents.Fields)
                };
            }

            cacheRows.Add((JsonNode)new JsonObject { ["key"] = Value(key), ["identity"] = id, ["packageDirectory"] = Value(entry.PackageDirectory.AsDirectoryPath()), ["directoryExists"] = entry.DirectoryExists, ["contentsIdentity"] = contentId, ["contents"] = content });
        }

        JsonObject response = new()
        {
            ["name"] = request["Name"]!.GetValue<string>(),
            ["options"] = Value(options),
            ["parseDiagnostics"] = observer.Diagnostics(errors),
            ["configDiagnostics"] = observer.Diagnostics(configErrors),
            ["queries"] = results,
            ["hostOperations"] = host.Operations.Count == 0 ? null : host.Operations,
            ["packageCache"] = cacheRows,
            ["diagnosticOwners"] = observer.Owners
        };
        if (request["Static"] is not null)
            response["staticErrorBase64"] = staticError is null ? null : Bytes(GoString.ToUtf8(staticError.Message));
        if (request["RawCompilerOptions"] is not null)
            response["rawOptionsErrorBase64"] = rawError is null ? null : Bytes(GoString.ToUtf8(rawError.Message));
        if (request["Redirects"] is not null)
            response["redirects"] = redirectRows;
        return response.ToJsonString();
    }
}
