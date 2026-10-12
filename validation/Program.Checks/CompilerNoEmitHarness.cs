// Authored no-emit test harness counterpart of the pinned compiler_runner,
// test_case_parser and harnessutil preparation. Options and source bytes remain
// original; Emit/declaration-output/builder cache operations are separate domains.
#nullable disable
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Configuration.Checks;
using TypeScript.Compiler;
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;
using T = TypeScript.TsOptions.TsOptions;
using CP = TypeScript.Compiler.Program;

namespace Program.Checks;
public static class CompilerNoEmitHarness
{
    public sealed record HarnessOptions(CaseSensitivity Sensitivity, RootedDirectoryPath CurrentDirectory, List<string> LibFiles, bool NoTypesAndSymbols, bool CaptureSuggestions, bool FullEmitPaths = false);
    public sealed partial record Compilation(CP PreProgram, CP Program, HarnessOptions Harness, List<CompilerBaselineWalker.File> BaselineFiles, string Header, string Trace);
    private static readonly RootedDirectoryPath Src = RootedDirectoryPath.FromGoString("/.src");
    private static readonly Regex Reference = new("reference[\\t\\n\\f\\r ]path", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> HarnessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "useCaseSensitiveFileNames",
        "baselineFile",
        "includeBuiltFile",
        "fileName",
        "libFiles",
        "noImplicitReferences",
        "currentDirectory",
        "symlink",
        "link",
        "noTypesAndSymbols",
        "fullEmitPaths",
        "reportDiagnostics",
        "captureSuggestions"
    };
    private static readonly HashSet<string> BooleanHarnessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "useCaseSensitiveFileNames",
        "noImplicitReferences",
        "noTypesAndSymbols",
        "fullEmitPaths",
        "reportDiagnostics",
        "captureSuggestions"
    };
    private static readonly Dictionary<string, CommandLineOption> ExtraOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["allowNonTsExtensions"] = new()
        {
            Name = "allowNonTsExtensions",
            Kind = CommandLineOptionKind.Boolean
        },
        ["noErrorTruncation"] = new()
        {
            Name = "noErrorTruncation",
            Kind = CommandLineOptionKind.Boolean
        },
        ["suppressOutputPathCheck"] = new()
        {
            Name = "suppressOutputPathCheck",
            Kind = CommandLineOptionKind.Boolean
        },
        ["noCheck"] = new()
        {
            Name = "noCheck",
            Kind = CommandLineOptionKind.Boolean
        }
    };
    // The configuration variants are the actual pinned enumerator's Config values,
    // correlated to fixture/configuration hashes by the bounded batch driver.
    public static Compilation Prepare(string sourceName, string content, Dictionary<string, string> configuration, string testDataDirectory)
    {
        var payload = CompilerFixtureParser.Parse(content, sourceName);
        var rawSettings = new Dictionary<string, string>(configuration, StringComparer.Ordinal);
        var initialDirectory = payload.CurrentDirectory == "" ? Src : TsPath.ToRootedDirectoryPath(payload.CurrentDirectory, Src);
        CompilerBaselineFileSystem.Input UnitInput(CompilerFixtureParser.Unit unit) => new(TsPath.ToRootedFilePath(unit.Name, initialDirectory).AsString(), unit.Content);
        // Original makeUnitsFromTest builds allFiles as a last-wins map before VFS creation.
        Dictionary<string, CompilerBaselineFileSystem.Input> configInputMap = new(StringComparer.Ordinal);
        foreach (var unit in payload.Units)
        {
            var entry = UnitInput(unit);
            configInputMap[entry.Path] = entry;
        }

        var configInputs = configInputMap.Values.ToList();
        foreach (var(path, target)in payload.Symlinks)
            configInputs.Add(new(TsPath.ToRootedPath(path, initialDirectory).AsString(), Symlink: TsPath.ToRootedPath(target, initialDirectory).AsString()));
        var configFS = new CompilerBaselineFileSystem(configInputs, CaseSensitivity.CaseSensitive);
        ParsedCommandLine tsconfig = null;
        var units = payload.Units.ToList();
        List<CompilerEmitTestFile> configFiles = [];
        for (int i = 0; i < units.Count; i++)
        {
            string basename = TsPath.GetBaseFileName(units[i].Name).ToLowerInvariant();
            if (basename is not ("tsconfig.json" or "jsconfig.json"))
                continue;
            var fileName = TsPath.ToRootedFilePath(units[i].Name, initialDirectory);
            var source = Parser.ParseSourceFile(new SourceFileParseOptions(fileName.AsString(), configFS.CaseSensitivity().PathKey(fileName.AsPath()).AsString(), FileNameIsGoBytes: true), GoString.Bytes(units[i].Content), ScriptKind.JSON);
            CompilerOptions existing = payload.GlobalOptions.GetValueOrDefault("runexternalcode") == "true" ? new()
            {
                RunExternalCode = Tristate.True
            }

            : null;
            tsconfig = T.ParseJsonSourceFileConfigFileContent(new() { SourceFile = source }, configFS, fileName.Directory(), existing);
            configFiles.Add(new(units[i].Name, units[i].Content));
            units.RemoveAt(i);
            break;
        }

        var cwd = rawSettings.GetValueOrDefault("currentdirectory", "")is { Length: > 0 } rawDirectory ? TsPath.ToRootedDirectoryPath(rawDirectory, Src) : Src;
        List<CompilerFixtureParser.Unit> input, other;
        if (tsconfig is not null)
        {
            var roots = tsconfig.FileNames() ?? [];
            input = units.Where(unit => roots.Contains(TsPath.ToRootedFilePath(unit.Name, cwd))).ToList();
            other = units.Where(unit => !roots.Contains(TsPath.ToRootedFilePath(unit.Name, cwd))).ToList();
        }
        else
        {
            if (rawSettings.TryGetValue("baseurl", out string baseUrl) && !TsPath.IsRootedDiskPath(baseUrl))
                rawSettings["baseurl"] = TsPath.ToRootedDirectoryPath(baseUrl, cwd).AsString();
            if (units.Count == 0)
                throw new InvalidOperationException("Original fixture has no compilation unit.");
            var last = units[^1];
            if (rawSettings.GetValueOrDefault("noimplicitreferences", "") != "" || last.Content.Contains("require(", StringComparison.Ordinal) || Reference.IsMatch(last.Content))
            {
                input = [last];
                other = units.Take(units.Count - 1).ToList();
            }
            else
            {
                input = units;
                other = [];
            }
        }

        var options = tsconfig?.CompilerOptions().Clone() ?? new CompilerOptions();
        if (options.NewLine == NewLineKind.None)
            options.NewLine = NewLineKind.CRLF;
        if (options.SkipDefaultLibCheck == Tristate.Unknown)
            options.SkipDefaultLibCheck = Tristate.True;
        options.NoErrorTruncation = Tristate.True;
        CaseSensitivity sensitivity = CaseSensitivity.CaseSensitive;
        RootedDirectoryPath harnessDirectory = cwd;
        List<string> libFiles = [];
        bool noTypes = false, suggestions = false, fullEmitPaths = false;
        foreach (var(name, value)in rawSettings)
        {
            if (name == "typescriptversion")
                continue;
            var option = T.OptionsDeclarations.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase));
            option ??= ExtraOptions.GetValueOrDefault(name);
            if (option is not null)
            {
                T.ParseCompilerOptions(option.Name, OptionValue(option, value, cwd), options);
                continue;
            }

            if (!HarnessNames.Contains(name))
                throw new InvalidOperationException("Unknown compiler option '" + name + "'.");
            bool boolean = BooleanHarnessNames.Contains(name) && ParseBoolean(name, value);
            switch (name.ToLowerInvariant())
            {
                case "usecasesensitivefilenames":
                    sensitivity = boolean ? CaseSensitivity.CaseSensitive : CaseSensitivity.CaseInsensitive;
                    break;
                case "currentdirectory":
                    harnessDirectory = TsPath.ToRootedDirectoryPath(value, harnessDirectory);
                    break;
                case "libfiles":
                    libFiles = ((List<object>)OptionValue(new CommandLineOption { Name = "libFiles", Kind = CommandLineOptionKind.List }, value, cwd))?.Cast<string>().ToList() ?? [];
                    break;
                case "notypesandsymbols":
                    noTypes = boolean;
                    break;
                case "capturesuggestions":
                    suggestions = boolean;
                    break;
                case "fullemitpaths":
                    fullEmitPaths = boolean;
                    break;
            }
        }

        var harness = new HarnessOptions(sensitivity, harnessDirectory, libFiles, noTypes, suggestions, fullEmitPaths);
        return PrepareFiles(input, other, harness, options, cwd, tsconfig, payload.Symlinks, testDataDirectory, Header(sourceName, testDataDirectory))with
        {
            OriginalConfigFiles = configFiles.Select(file => file with { UnitName = TsPath.ToRootedFilePath(file.UnitName, cwd).AsString() }).ToList()
        };
    }

    // Shared CompileFilesEx tail: declaration recompilation reuses the real host,
    // options/config/content mapper/environment preparation, not fixture serialization.
    internal static Compilation PrepareFiles(List<CompilerFixtureParser.Unit> input, List<CompilerFixtureParser.Unit> other, HarnessOptions harness, CompilerOptions options, RootedDirectoryPath cwd, ParsedCommandLine tsconfig, Dictionary<string, string> symlinks, string testDataDirectory, string header)
    {
        List<RootedFilePath> programRoots = input.Select(unit => TsPath.ToRootedFilePath(unit.Name, cwd)).Where(path => !path.ExtensionIs(".json") && !path.ExtensionIs(".tsbuildinfo")).ToList();
        bool includeLib = input.Any(unit => unit.Content.Contains("/.lib/", StringComparison.Ordinal));
        foreach (string file in harness.LibFiles)
        {
            if (file == "lib.d.ts" && options.NoLib != Tristate.True)
                continue;
            programRoots.Add(cwd.ResolveFile(TsPath.CombinePaths("/.lib", file)));
            includeLib = true;
        }

        Dictionary<string, CompilerBaselineFileSystem.Input> files = new(StringComparer.Ordinal);
        foreach (var unit in input.Concat(other))
        {
            string name = TsPath.GetNormalizedAbsolutePath(unit.Name, cwd.AsString());
            files[name] = new(name, unit.Content);
        }

        foreach (var(path, target)in symlinks)
        {
            string name = TsPath.GetNormalizedAbsolutePath(path, cwd.AsString());
            files[name] = new(name, Symlink: TsPath.GetNormalizedAbsolutePath(target, cwd.AsString()));
        }

        if (includeLib)
            foreach (string file in Directory.EnumerateFiles(Path.Combine(testDataDirectory, "tests/lib"), "*", SearchOption.AllDirectories))
            {
                string path = "/.lib/" + Path.GetRelativePath(Path.Combine(testDataDirectory, "tests/lib"), file).Replace('\\', '/');
                files[path] = new(path, GoString.FromBytes(System.IO.File.ReadAllBytes(file)));
            }

        CompilerApiEnvironment.AddToFiles(files, testDataDirectory);
        IFS fs = TypeScript.Bundled.Bundled.WrapFS(new CompilerBaselineFileSystem(files.Values, harness.Sensitivity));
        var config = T.NewParsedCommandLine(options, programRoots, null, tsconfig is not null && tsconfig.BaseDirectory().AsString() != "" ? tsconfig.BaseDirectory() : cwd, harness.Sensitivity);
        config.ParsedConfig.ContentMappers = tsconfig?.ContentMappers();
        config.ConfigFile = tsconfig?.ConfigFile;
        config.Errors = tsconfig?.Errors ?? default;
        IProject mapperProject = options.RunExternalCode == Tristate.True && (config.ContentMappers()?.Count ?? 0) > 0 ? new CompilerFixtureMapperProject(config.ContentMappers(), options) : null;
        var tracer = new BaselineTracer(cwd, harness.Sensitivity);
        var host = new CachedSourceHost(CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath(), trace: tracer.Trace, contentMapperProject: mapperProject));
        var preOptions = options.Clone();
        preOptions.TraceResolution = Tristate.False;
        var preConfig = T.NewParsedCommandLine(preOptions, config.FileNames(), config.ProjectReferences(), config.BaseDirectory(), harness.Sensitivity);
        preConfig.ParsedConfig.ContentMappers = config.ContentMappers();
        preConfig.ConfigFile = config.ConfigFile;
        preConfig.Errors = config.Errors;
        // Complete recomputation is an explicit harness scheduling adaptation for
        // original incremental-option inputs; their options/buildinfo remain real.
        var pre = CP.NewProgram(new() { Config = preConfig, Host = host });
        // The caller collects this generation before constructing the next one,
        // preserving the actual original diagnostics/acquisition ordering.
        return new(pre, null, harness, input.Concat(other).Select(unit => new CompilerBaselineWalker.File(TsPath.GetNormalizedAbsolutePath(unit.Name, cwd.AsString()), unit.Content)).ToList(), header, "")
        {
            ActualConfiguration = config,
            Host = host,
            Tracer = tracer,
            OriginalInputFiles = input.Select(unit => new CompilerEmitTestFile(TsPath.ToRootedFilePath(unit.Name, cwd).AsString(), unit.Content)).ToList(),
            OriginalOtherFiles = other.Select(unit => new CompilerEmitTestFile(TsPath.ToRootedFilePath(unit.Name, cwd).AsString(), unit.Content)).ToList(),
            OriginalSymlinks = symlinks,
            TestDataDirectory = testDataDirectory
        };
    }

    public sealed partial record Compilation
    {
        public List<CompilerEmitTestFile> OriginalConfigFiles { get; init; } = [];
        public List<CompilerEmitTestFile> OriginalInputFiles { get; init; } = [];
        public List<CompilerEmitTestFile> OriginalOtherFiles { get; init; } = [];
        internal Dictionary<string, string> OriginalSymlinks { get; init; } = [];
        internal string TestDataDirectory { get; init; } = "";
        internal ParsedCommandLine ActualConfiguration { get; init; }
        internal ICompilerHost Host { get; init; }
        internal BaselineTracer Tracer { get; init; }

        public Compilation ConstructActualProgram() => this with
        {
            Program = CP.NewProgram(new() { Config = ActualConfiguration, Host = Host })
        };
    }

    private static string Header(string source, string testData) => TsPath.GetPathFromPathComponents(TsPath.GetPathComponentsRelativeTo(GoString.ToUtf8(testData), source, CaseSensitivity.CaseInsensitive));
    private static bool ParseBoolean(string name, string value) => value.ToLowerInvariant() switch
    {
        "true" => true,
        "false" => false,
        _ => throw new InvalidOperationException("Value for option '" + name + "' must be a boolean, got: " + value)};
    private static object OptionValue(CommandLineOption option, string value, RootedDirectoryPath cwd)
    {
        switch (option.Kind)
        {
            case CommandLineOptionKind.String:
                return option.PathKind.IsRooted() ? TsPath.GetNormalizedAbsolutePath(value, cwd.AsString()) : value;
            case CommandLineOptionKind.Number:
                return long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            case CommandLineOptionKind.Boolean:
                return ParseBoolean(option.Name, value);
            case CommandLineOptionKind.Enum:
                var(enumeration, ok) = option.EnumMap().Get(value.ToLowerInvariant());
                return ok ? enumeration : throw new InvalidOperationException("Unknown enum value for " + option.Name + ": " + value);
            case CommandLineOptionKind.List:
            case CommandLineOptionKind.ListOrElement:
                var(list, errors) = T.ParseListTypeOption(option, value);
                if (option.Elements().PathKind.IsRooted())
                    return list?.Select(item => (object)TsPath.GetNormalizedAbsolutePath((string)item, cwd.AsString())).ToList();
                if (errors.Length > 0)
                    throw new InvalidOperationException("Unknown value '" + value + "' for compiler option '" + option.Name + "'");
                return list;
            case CommandLineOptionKind.Object:
                throw new InvalidOperationException("Object type options like '" + option.Name + "' are not supported");
            default:
                return null;
        }
    }

    private sealed record SourceKey(SourceFileParseOptions Options, string Text, ScriptKind Kind);
    private sealed class CachedSourceHost(ICompilerHost inner) : ICompilerHost
    {
        private static readonly ConcurrentDictionary<SourceKey, SourceFile> Cache = new();
        public IFS FS() => inner.FS();
        public RootedDirectoryPath DefaultLibraryPath() => inner.DefaultLibraryPath();
        public void Trace(Message message, params object[] args) => inner.Trace(message, args);
        public SourceFile GetSourceFile(SourceFileParseOptions options)
        {
            var file = RootedFilePath.FromGoString(options.FileName);
            var(text, ok) = FS().ReadFile(file);
            if (!ok)
                return null;
            var kind = Core.GetScriptKindFromFileName(file);
            if (kind == ScriptKind.Unknown)
                throw new InvalidOperationException("Unknown script kind for file  " + file.AsString());
            return Cache.GetOrAdd(new(options, text, kind), key => Parser.ParseSourceFile(key.Options, GoString.Bytes(key.Text), key.Kind));
        }

        public (SourceFiles Files, Exception Error) GetContentMappedSourceFiles(SourceFileParseOptions options, Mapper mapper) => inner.GetContentMappedSourceFiles(options, mapper);
        public IProject ContentMapperProject() => inner.ContentMapperProject();
        public ParsedCommandLine GetResolvedProjectReference(RootedFilePath file, PathKey path) => inner.GetResolvedProjectReference(file, path);
    }

    internal sealed class BaselineTracer(RootedDirectoryPath cwd, CaseSensitivity sensitivity)
    {
        private readonly StringBuilder builder = new();
        private readonly Dictionary<PathKey, bool> packageJsonCache = new();
        public void Trace(Message message, object[] args) => builder.Append(Sanitize(GoString.ToUtf8(Ast.NewCompilerDiagnostic(message, args).String()))).Append('\n');
        private string Sanitize(string message)
        {
            string version = "'" + Core.Version() + "'";
            int index = message.IndexOf(version, StringComparison.Ordinal);
            if (index >= 0)
                return message[..index] + "'FakeTSVersion'" + message[(index + version.Length)..];
            foreach (var(suffix, exists)in new[]
            {
                ("' does not exist according to earlier cached lookups.", false),
                ("' exists according to earlier cached lookups.", true)
            }

            )
                if (message.EndsWith(suffix, StringComparison.Ordinal))
                {
                    string prefix = message[..^suffix.Length];
                    string file = prefix.StartsWith("File '", StringComparison.Ordinal) ? prefix[6..] : prefix;
                    var key = sensitivity.PathKey(TsPath.ToRootedPath(file, cwd));
                    if (packageJsonCache.ContainsKey(key))
                        return message;
                    packageJsonCache[key] = exists;
                    return exists ? "Found 'package.json' at '" + file + "'." : "File '" + file + "' does not exist.";
                }

            if (message.EndsWith("' does not exist.", StringComparison.Ordinal))
            {
                string prefix = message[..^"' does not exist.".Length];
                string file = prefix.StartsWith("File '", StringComparison.Ordinal) ? prefix[6..] : prefix;
                var key = sensitivity.PathKey(TsPath.ToRootedPath(file, cwd));
                if (!packageJsonCache.ContainsKey(key))
                {
                    packageJsonCache[key] = false;
                    return message;
                }

                return "File '" + file + "' does not exist according to earlier cached lookups.";
            }

            if (message.StartsWith("Found 'package.json' at '", StringComparison.Ordinal))
            {
                string file = message[25..];
                if (file.EndsWith("'.", StringComparison.Ordinal))
                    file = file[..^2];
                var key = sensitivity.PathKey(TsPath.ToRootedPath(file, cwd));
                if (!packageJsonCache.ContainsKey(key))
                {
                    packageJsonCache[key] = true;
                    return message;
                }

                return "File '" + file + "' exists according to earlier cached lookups.";
            }

            return message;
        }

        public override string ToString() => builder.ToString();
    }
}
