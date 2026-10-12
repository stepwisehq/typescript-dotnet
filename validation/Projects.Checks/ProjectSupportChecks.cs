// Authored shared xUnit/Native AOT assertions, including explicitly adapted originals.
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;

namespace TypeScript.Projects;
public static partial class ProjectSupportChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestGetDirectoryPath", "TestGetPathComponents", "TestReducePathComponents", "TestCombinePaths", "TestResolvePath", "TestResolvePathWithoutTrailingDirectorySeparator", "TestNormalizePathDriveRoot", "TestGetNormalizedAbsolutePath", "Adapted.NormalizePathForCJSResolution", "Config.StrictSubset", "Resolution.PrecedenceAndFlags", "Resolution.UnsupportedWinner", "Program.CycleAndPathIdentity", "Program.NoEmitFilter", "Program.ParseFailureAndFreshIdentity", "TestIncludeReasonDiagnosticsAreProgramLocal", "Core.FindCapturedRange", "ModuleSpecifiers.SourceApi", "TestToFileNameLowerCase", "TestProcessEndingChecksRootedFilePath", "Adapted.TestIncludeProcessorDiagnosticsWithMissingFileCasing"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetDirectoryPath":
                TestGetDirectoryPath();
                break;
            case "TestGetPathComponents":
                TestGetPathComponents();
                break;
            case "TestReducePathComponents":
                TestReducePathComponents();
                break;
            case "TestCombinePaths":
                TestCombinePaths();
                break;
            case "TestResolvePath":
                TestResolvePath();
                break;
            case "TestResolvePathWithoutTrailingDirectorySeparator":
                TestResolvePathWithoutTrailingDirectorySeparator();
                break;
            case "TestNormalizePathDriveRoot":
                TestNormalizePathDriveRoot();
                break;
            case "TestGetNormalizedAbsolutePath":
                TestGetNormalizedAbsolutePath();
                break;
            case "Adapted.NormalizePathForCJSResolution":
                DirectoryIntent();
                break;
            case "Config.StrictSubset":
                StrictConfiguration();
                break;
            case "Resolution.PrecedenceAndFlags":
                ResolutionPrecedence();
                break;
            case "Resolution.UnsupportedWinner":
                UnsupportedWinner();
                break;
            case "Program.CycleAndPathIdentity":
                CycleAndIdentity();
                break;
            case "Program.NoEmitFilter":
                NoEmitFilter();
                break;
            case "Program.ParseFailureAndFreshIdentity":
                ParseFailureAndFreshIdentity();
                break;
            case "TestIncludeReasonDiagnosticsAreProgramLocal":
                IncludeReasonDiagnosticsAreProgramLocal();
                break;
            case "Core.FindCapturedRange":
                FindCapturedRange();
                break;
            case "ModuleSpecifiers.SourceApi":
                ModuleSpecifierSourceApi();
                break;
            case "TestToFileNameLowerCase":
                FileNameLowerCase();
                break;
            case "TestProcessEndingChecksRootedFilePath":
                ProcessEndingChecksRootedFilePath();
                break;
            case "Adapted.TestIncludeProcessorDiagnosticsWithMissingFileCasing":
                IncludeProcessorDiagnosticsWithMissingFileCasing();
                break;
            default:
                throw new ArgumentException("Unknown project support assertion: " + name);
        }
    }

    private static void Equal<T>(T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException($"Expected {expected}; actual {actual}.");
    }

    private static void Sequence<T>(IEnumerable<T> actual, IEnumerable<T> expected)
    {
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException("Ordered assertion differs: " + string.Join(",", actual));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject(Action action, string feature)
    {
        try
        {
            action();
        }
        catch (ProjectException ex)when (ex.Feature == feature)
        {
            return;
        }

        throw new InvalidOperationException("Expected explicit project rejection: " + feature);
    }

    // resolver_internal_test.go:10. All 12 non-dynamic rows retain four original
    // assertions; four URI encoding rows excluded => adapted subset, not full test.
    private static void DirectoryIntent()
    {
        (string Directory, string Module, string Path, string Text, bool DirectoryOnly)[] rows = [("/project", "file", "/project/file", "/project/file", false), ("/project", "directory/", "/project/directory", "/project/directory/", true), ("/project", ".", "/project", "/project/", true), ("/project", ".\\", "/project", "/project/", true), ("/project/src", "../lib/", "/project/lib", "/project/lib/", true), ("/project/src", "..\\lib\\", "/project/lib", "/project/lib/", true), ("/project", "..", "/", "/", true), ("c:/project", "..", "c:/", "c:/", true), ("/project", "c:", "c:/", "c:/", true), ("/project", "//server", "//server/", "//server/", true), ("file:///project", "..", "file:///", "file:///", true), ("/project", "file://server", "file://server/", "file://server/", true)];
        foreach (var row in rows)
        {
            var candidate = DefaultResolver.VerificationNormalizePathForCJSResolution(RootedDirectoryPath.FromUnicode(row.Directory), GoString.ToUtf8(row.Module));
            Equal(candidate.Path.ToUnicode(), row.Path);
            Equal(candidate.DirectoryOnly, row.DirectoryOnly);
            Equal(GoString.ToString(candidate.Text), row.Text);
            Equal(candidate.HasTrailingDirectorySeparator, row.DirectoryOnly);
        }
    }

    private static void StrictConfiguration()
    {
        using var disk = new Fixture();
        disk.Write("main.ts", "export const x=1;");
        string[] invalid = ["{}", "{\"files\":[]}", "{\"files\":false}", "{\"files\":[1]}", "{\"files\":[\"main.ts\"],\"files\":[\"main.ts\"]}", "{\"files\":[\"main.ts\"],\"include\":[\"*.ts\"]}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"noEmit\":false}}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"downlevelIteration\":false}}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"types\":[\"node\"]}}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"module\":\"CommonJS\"}}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"noEmit\":true,\"noEmit\":true}}"];
        foreach (string json in invalid)
        {
            disk.Config(json);
            Reject(() => Project.Load(disk.ConfigPath), "configuration");
        }

        // Authored malformed decoding controls, not original compiler assertions.
        string[] malformedStrings = ["{\"files\":[\"\\ud800.ts\"]}", "{\"files\":[\"\\udc00.ts\"]}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"module\":\"\\ud800\"}}", "{\"files\":[\"main.ts\"],\"\\ud800\":true}", "{\"files\":[\"main.ts\"],\"compilerOptions\":{\"\\ud800\":true}}"];
        foreach (string json in malformedStrings)
        {
            disk.Config(json);
            Reject(() => Project.Load(disk.ConfigPath), "configuration");
        }

        disk.Config("{\"files\":[\"x\\u0000.ts\"]}");
        Reject(() => Project.Load(disk.ConfigPath), "path");
        foreach (string path in new[]
        {
            "",
            " ",
            "bad\0.json"
        }

        )
            Reject(() => Project.Load(path), "configurationPath");
        disk.Config("{ /*comment*/ \"files\":[\"main.ts\"] }");
        Reject(() => Project.Load(disk.ConfigPath), "configurationJson");
        disk.Config("{\"files\":[\"main.ts\"],\"compilerOptions\":{\"target\":\"ESNext\",\"module\":\"ESNext\",\"moduleResolution\":\"Bundler\",\"noEmit\":true,\"types\":[]}}");
        Project valid = Project.Load(disk.ConfigPath);
        Require(valid.Options.NoEmit == Tristate.True && valid.Options.DownlevelIteration == Tristate.Unknown && valid.Options.Types?.Count == 0, "Fixed options must preserve omitted removed-option state.");
        Require(!GoSlices.Contains(valid.SourceFiles, valid.ConfigSourceFile), "Config AST is not a Program source root.");
        disk.Config("{\"files\":[\"missing.ts\"]}");
        Equal(Project.Load(disk.ConfigPath).Check().Single().Code(), 6053);
        disk.Config("{\"files\":[\"../escape.ts\"]}");
        Reject(() => Project.Load(disk.ConfigPath), "outsideRoot");
    }

    private static void ResolutionPrecedence()
    {
        using var disk = new Fixture();
        disk.Write("entry.ts", "");
        disk.Write("dep.ts", "export const n=1;");
        disk.Write("dep.js", "");
        disk.Write("models.types.ts", "export const n=3;");
        disk.Write("dir/index.ts", "export const n=2;");
        disk.Write("package.json", "{\"type\":\"module\"}");
        var host = new LegacyResolutionHost(new ProjectDiskHost(disk.Root));
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = host, CompilerOptions = ProjectConfiguration.CreateOptions() });
        ResolvedModule? Resolve(string name) => host.ResolveModuleName(resolver, GoString.ToUtf8(name), RootedFilePath.FromUnicode(disk.Path("entry.ts")), ResolutionMode.ESNext);
        File.CreateSymbolicLink(disk.Path("shadow.js"), disk.Path("entry.ts"));
        disk.Write("shadow.ts", "export const x=1;");
        Equal(Resolve("./shadow.js")!.ResolvedFileName.ToUnicode(), disk.Path("shadow.ts"));
        ResolvedModule js = Resolve("./dep.js")!;
        Equal(js.ResolvedFileName.ToUnicode(), disk.Path("dep.ts"));
        Equal(js.ResolvedUsingTsExtension, false);
        ResolvedModule ts = Resolve("./dep.ts")!;
        Equal(ts.ResolvedFileName, js.ResolvedFileName);
        Equal(ts.ResolvedUsingTsExtension, true);
        Equal(Resolve("./models.types")!.ResolvedFileName.ToUnicode(), disk.Path("models.types.ts"));
        Equal(Resolve("./dir")!.ResolvedFileName.ToUnicode(), disk.Path("dir/index.ts"));
        Require(ReferenceEquals(js, Resolve("./dep.js")), "Positive resolution cache identity.");
        var missing = Resolve("./missing");
        Require(!missing.IsResolved() && ReferenceEquals(missing, Resolve("./missing")), "Negative resolution cache identity.");
        // Authored legacy-admission controls: only reached directory metadata
        // is rejected; a source winner and ancestor format metadata stay legal.
        disk.Write("folder.ts", "export const value=1;");
        disk.Write("folder/package.json", "{}");
        Equal(Resolve("./folder")!.ResolvedFileName.ToUnicode(), disk.Path("folder.ts"));
        var scope = resolver.GetResolutionData().NewResolver(host).GetPackageScopeForPath(RootedDirectoryPath.FromUnicode(disk.Root));
        Require(scope.Exists(), "Ancestor package format metadata remains readable.");
        Equal(scope!.Contents!.Fields.Type.GetValue(), ("module", true));
        ScalarModuleEncoding();
    }

    // Authored host-boundary assertions: original scanner preserves lone
    // surrogates, while this filesystem adapter explicitly rejects their paths.
    private static void ScalarModuleEncoding()
    {
        // Authored scalar/raw-byte boundary controls, shared by the existing
        // managed and native Resolution.PrecedenceAndFlags case.
        foreach (string ascii in new[] { "", "./a.ts", "\0\t\n\u007F" })
        {
            Equal(PathByteText.FromUnicode(ascii), ascii);
            Equal(PathByteText.ToUnicode(ascii), ascii);
            Equal(ProjectDiskHost.DecodeModuleReference(ascii), ascii);
        }
        const string scalar = "é/\U0001F600.ts";
        const string raw = "\u00C3\u00A9/\u00F0\u009F\u0098\u0080.ts";
        Equal(PathByteText.FromUnicode(scalar), raw);
        Equal(PathByteText.ToUnicode(raw), scalar);
        Equal(ProjectDiskHost.DecodeModuleReference(raw), scalar);
        ExpectBoundaryException<System.Text.EncoderFallbackException>(() => PathByteText.FromUnicode("\uD800"));
        foreach (string malformed in new[] { "\u00FF", "\u00C0\u0080", "\u00ED\u00A0\u0080" })
        {
            ExpectBoundaryException<System.Text.DecoderFallbackException>(() => PathByteText.ToUnicode(malformed));
            Reject(() => ProjectDiskHost.DecodeModuleReference(malformed), "pathEncoding");
        }
        ExpectBoundaryException<ArgumentNullException>(() => PathByteText.FromUnicode(null!));
        ExpectBoundaryException<ArgumentNullException>(() => PathByteText.ToUnicode(null!));
        ExpectBoundaryException<ArgumentNullException>(() => ProjectDiskHost.DecodeModuleReference(null!));

        static void ExpectBoundaryException<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + " at the scalar path boundary.");
        }

        using var disk = new Fixture();
        disk.Config("{\"files\":[\"main.ts\"]}");
        disk.Write("\uFFFD.ts", "export const x=1;");
        disk.Write("\uFFFD\uFFFD\uFFFD.ts", "export const x=1;");
        disk.Write("\U0001F600.ts", "export const x=1;");
        disk.Write("main.ts", "const lone=\"\\uD800\"; export const x=1;");
        Equal(Project.Load(disk.ConfigPath).Check().Count, 0);
        foreach (string escaped in new[]
        {
            "\\uD800",
            "\\uDC00"
        }

        )
        {
            disk.Write("main.ts", "import {x} from \"./" + escaped + "\"; const y:number=x;");
            Reject(() => Project.Load(disk.ConfigPath), "pathEncoding");
        }

        foreach (var(escaped, name)in new[]
        {
            ("\\uD83D\\uDE00", "\U0001F600"),
            ("\\uFFFD", "\uFFFD"),
            ("\uFFFD", "\uFFFD")
        }

        )
        {
            disk.Write("main.ts", "import {x} from \"./" + escaped + "\"; const y:number=x;");
            Project project = Project.Load(disk.ConfigPath);
            Equal(project.Check().Count, 0);
            var edge = project.ResolutionEdges.Single();
            Equal(edge.ModuleReference, "./" + name);
            Equal(edge.Resolution.ResolvedFileName.ToUnicode(), disk.Path(name + ".ts"));
            var host = (ICheckerProgramHost)project;
            var formatter = (TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost)project;
            string validBytes = GoString.ToUtf8("./" + name);
            Require(ReferenceEquals(host.GetResolvedModule(edge.ImportingFile, validBytes, edge.Mode), edge.Resolution), "Scalar lookup retains actual cached target.");
            Require(ReferenceEquals(formatter.GetResolvedModuleFromModuleSpecifier(edge.ImportingFile, edge.ModuleSpecifier), edge.Resolution), "Scalar formatter lookup retains actual cached target.");
            // Controlled raw AST values exercise both lookup boundaries directly.
            foreach (byte middle in new byte[]
            {
                0xA0,
                0xB0
            }

            )
            {
                string invalidBytes = "./" + GoString.FromBytes(new byte[] { 0xED, middle, 0x80 });
                Reject(() => host.GetResolvedModule(edge.ImportingFile, invalidBytes, edge.Mode), "pathEncoding");
                Node invalidSpecifier = new NodeFactory().NewStringLiteral(invalidBytes, 0);
                Reject(() => formatter.GetResolvedModuleFromModuleSpecifier(edge.ImportingFile, invalidSpecifier), "pathEncoding");
            }
        }
    }

    private static void UnsupportedWinner()
    {
        using var disk = new Fixture();
        disk.Write("entry.ts", "");
        disk.Write("dep.d.ts", "");
        disk.Write("dep/index.ts", "");
        var host = new LegacyResolutionHost(new ProjectDiskHost(disk.Root));
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = host, CompilerOptions = ProjectConfiguration.CreateOptions() });
        ResolvedModule? Resolve(string name) => host.ResolveModuleName(resolver, GoString.ToUtf8(name), RootedFilePath.FromUnicode(disk.Path("entry.ts")), ResolutionMode.ESNext);
        Reject(() => Resolve("./dep"), "resolvedFileForm");
        disk.Write("dotted.types.ts", "");
        disk.Write("dotted.d.types.ts", "");
        Reject(() => Resolve("./dotted.types"), "resolvedFileForm");
        disk.Write("folder/package.json", "{}");
        disk.Write("folder/index.ts", "");
        Reject(() => Resolve("./folder"), "packageJsonResolution");
        Reject(() => Resolve("package"), "moduleSpecifier");
        File.CreateSymbolicLink(disk.Path("link.ts"), disk.Path("entry.ts"));
        Reject(() => Resolve("./link"), "symlink");
        Reject(() => Resolve("./node_modules/pkg"), "nodeModules");
    }

    // Adapted from TestProgram import/cycle order invariants: bounded POSIX paths,
    // fixed three explicit libs, no full ESNext/console/outside-root setup.
    private static void CycleAndIdentity()
    {
        using var disk = new Fixture();
        disk.Write("main.ts", "import {a} from './a';import {a as again} from './sub/../a.ts';export const result=a;");
        disk.Write("a.ts", "import {b} from './b';export const a=1;");
        disk.Write("b.ts", "import {a} from './a';export const b=2;");
        disk.Write("sub/placeholder.ts", "");
        disk.Config("{\"files\":[\"main.ts\",\"./main.ts\"]}");
        Project project = Project.Load(disk.ConfigPath);
        Sequence(project.UserFiles.Select(f => System.IO.Path.GetFileName(f.FileName())), new[] { "b.ts", "a.ts", "main.ts" });
        Equal(project.RootFiles.Count, 1);
        var a = project.UserFiles.Single(f => f.FileName().EndsWith("/a.ts", StringComparison.Ordinal));
        Require(project.ResolutionEdges.Where(e => e.Resolution.ResolvedFileName.ToUnicode() == a.FileName()).All(e => ReferenceEquals(e.TargetFile, a)), "Normalized/cyclic paths must share actual SourceFile identity.");
    }

    private static void NoEmitFilter()
    {
        Diagnostic skipped = Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, "skipped.ts");
        skipped.SetSkippedOnNoEmit();
        Diagnostic retained = Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, "retained.ts");
        var input = Slice<Diagnostic>.FromArray([skipped, retained]);
        var options = new CompilerOptions
        {
            NoEmit = Tristate.True
        };
        Sequence(GoSlices.Values(TypeScript.Compiler.Program.FilterNoEmitSemanticDiagnostics(input, options)), new[] { retained });
        Equal(input.Count, 2);
        var allRetained = Slice<Diagnostic>.FromArray([retained]);
        Require(Core.Same(TypeScript.Compiler.Program.FilterNoEmitSemanticDiagnostics(allRetained, options), allRetained), "Original all-match noEmit filter preserves header length and shared first-slot identity.");
        options.NoEmit = Tristate.False;
        Require(Core.Same(TypeScript.Compiler.Program.FilterNoEmitSemanticDiagnostics(input, options), input), "NoEmit=false retains original diagnostic header length and shared first-slot identity.");
    }

    private static void ParseFailureAndFreshIdentity()
    {
        using var disk = new Fixture();
        disk.Write("main.ts", "const x=;");
        disk.Config("{\"files\":[\"main.ts\"]}");
        Project malformed = Project.Load(disk.ConfigPath);
        Require(malformed.ParseDiagnostics.Count > 0 && malformed.Check().Count > 0, "Parse failure cannot become a successful check.");
        Reject(() => _ = malformed.Checker, "semanticEntry");
        disk.Write("main.ts", "export const x=1;");
        Project first = Project.Load(disk.ConfigPath), second = Project.Load(disk.ConfigPath);
        Require(!ReferenceEquals(first.RootFiles[0], second.RootFiles[0]) && !ReferenceEquals(first.LibraryFiles[0], second.LibraryFiles[0]), "Fresh project AST environments must be independent.");
    }

    // program_test.go:80. Original three identity assertions repeated for both
    // relative modes, translated to the actual mapped Program include processor.
    private static void IncludeReasonDiagnosticsAreProgramLocal()
    {
        using var disk = new Fixture();
        disk.Config("{\"files\":[\"main.ts\"]}");
        var configuration = new ProjectConfiguration(disk.ConfigPath);
        var oldProgram = new IncludeProcessor(configuration);
        var newProgram = new IncludeProcessor(configuration);
        var reason = new FileIncludeReason();
        foreach (bool relative in new[]
        {
            false,
            true
        }

        )
        {
            Diagnostic oldDiagnostic = reason.ToDiagnostic(oldProgram, relative);
            Diagnostic newDiagnostic = reason.ToDiagnostic(newProgram, relative);
            Require(ReferenceEquals(reason.ToDiagnostic(oldProgram, relative), oldDiagnostic), "Original old-program reason diagnostic cache identity.");
            Require(ReferenceEquals(reason.ToDiagnostic(newProgram, relative), newDiagnostic), "Original new-program reason diagnostic cache identity.");
            Require(!ReferenceEquals(oldDiagnostic, newDiagnostic), "Original reason diagnostics belong to distinct Programs.");
        }
    }

    private static void FindCapturedRange()
    {
        Equal(Core.Find<string>(null!, _ => true), "");
        Equal(Core.Find(new List<string> { "a" }, _ => false), "");
        var values = new List<string>
        {
            "a",
            "b"
        };
        var visited = new List<string>();
        Equal(Core.Find(values, item =>
        {
            visited.Add(item);
            if (item == "a")
                values.Add("c");
            return false;
        }), "");
        Sequence(visited, new[] { "a", "b" });
        Sequence(values, new[] { "a", "b", "c" });
        Equal(Core.Find(values, item => item == "b"), "b");
    }

    // Supplemental original source-API observation, not an upstream named test.
    // Original no/type/imports variants all return Relative ['./a']. The former
    // authored imports-map rejection is retired by the same-input pinned-Go
    // qualification in checker-noemit-validation/ending-host-repair.
    private static void ModuleSpecifierSourceApi()
    {
        RootIndexShadowProbe();
        foreach (string? package in new string? []
        {
            null,
            "{\"type\":\"module\",\"name\":\"example\",\"version\":\"1.0.0\"}",
            "{\"imports\":{\"#detail\":\"./a.ts\"}}"
        }

        )
        {
            using var disk = new Fixture();
            disk.Write("a.ts", "export const x=1;");
            disk.Write("b.ts", "export {x} from './a';");
            disk.Write("main.ts", "import {x} from './b';export const out=x;");
            disk.Config("{\"files\":[\"main.ts\"]}");
            if (package != null)
                disk.Write("package.json", package);
            var project = Project.Load(disk.ConfigPath);
            var target = project.UserFiles.Single(file => file.FileName().EndsWith("/a.ts", StringComparison.Ordinal));
            var context = project.UserFiles.Single(file => file.FileName().EndsWith("/main.ts", StringComparison.Ordinal));
            var checker = project.Checker;
            TypeScript.ModuleSpecifiers.ModuleSpecifiersResult Run() => TypeScript.ModuleSpecifiers.ModuleSpecifiers.GetModuleSpecifiers(target.AsNode().Symbol(), checker, project.Options, context, project, new TypeScript.ModuleSpecifiers.UserPreferences { ImportModuleSpecifierPreference = TypeScript.ModuleSpecifiers.ImportModuleSpecifierPreference.ProjectRelative }, new TypeScript.ModuleSpecifiers.ModuleSpecifierOptions(), false);
            var result = Run();
            Equal(result.Kind, TypeScript.ModuleSpecifiers.ResultKind.Relative);
            Sequence(GoSlices.Values(result.Specifiers).Select(specifier => specifier.AsString()), new[] { "./a" });
            Require(result.AmbientModuleSymbol == null, "Original source API returns no ambient symbol.");
        }
    }

    // program_test.go:539. Preserve the original no-panic diagnostic query;
    // disk/config/fixed explicit library setup replaces its default-lib host.
    private static void IncludeProcessorDiagnosticsWithMissingFileCasing()
    {
        using var disk = new Fixture();
        disk.Write("myFile.ts", "export const y = 2;");
        disk.Config("{\"files\":[\"MyFile.ts\",\"myFile.ts\"]}");
        _ = Project.Load(disk.ConfigPath).ProgramDiagnostics;
    }

    // path_test.go:557. All four original literal assertions are unchanged.
    private static void FileNameLowerCase()
    {
        Equal(TsPath.ToFileNameLowerCase("/user/UserName/projects/Project/file.ts"), "/user/username/projects/project/file.ts");
        Equal(TsPath.ToFileNameLowerCase("/user/UserName/projects/projectß/file.ts"), "/user/username/projects/projectß/file.ts");
        Equal(TsPath.ToFileNameLowerCase("/user/UserName/projects/İproject/file.ts"), "/user/username/projects/İproject/file.ts");
        Equal(TsPath.ToFileNameLowerCase("/user/UserName/projects/ı/file.ts"), "/user/username/projects/ı/file.ts");
    }

    // specifiers_test.go:407. Preserve both original rooted-probe assertions.
    // The complete same-package helper is internal and available through the
    // existing Projects.Checks friend assembly; use its retained Slice input.
    private static void ProcessEndingChecksRootedFilePath()
    {
        var host = new EndingProbeHost();
        var result = TypeScript.ModuleSpecifiers.ModuleSpecifiers.processEnding(
            ModuleSpecifier.FromGoString("./lib/index.ts"),
            RootedFilePath.FromGoString("/project/src/lib/index.ts"),
            Slice<TypeScript.ModuleSpecifiers.ModuleSpecifierEnding>.FromArray(
                [TypeScript.ModuleSpecifiers.ModuleSpecifierEnding.Minimal]),
            new CompilerOptions(), host);
        Equal(result.AsString(), "./lib/index");
        Require(host.FileExistsCalls.Contains("/project/src/lib.ts"), "Original FileExists rooted candidate was not probed.");
    }

    // The original mock implements the host interface; only FileExists is used
    // by this exact helper. Unused semantic services fail if unexpectedly called.
    private sealed class EndingProbeHost : TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost
    {
        internal readonly List<string> FileExistsCalls = [];
        public bool FileExists(RootedFilePath path)
        {
            FileExistsCalls.Add(path.AsString());
            return path.AsString() == "/project/src/lib.ts";
        }

        // specifiers_test.go:98–172. Preserve this fixture's rooted directory
        // and case policy; services outside the rooted FileExists probe fail.
        public RootedDirectoryPath BaseDirectory() => RootedDirectoryPath.FromGoString("/wrong");
        public RootedDirectoryPath CommonSourceDirectory() => BaseDirectory();
        public CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseSensitive;
        public List<string>? ContentMapperExtensions() => throw new InvalidOperationException("Unused original mock service");
        public TypeScript.Symlinks.KnownSymlinks GetSymlinkCache() => throw new InvalidOperationException("Unused original mock service");
        public RootedDirectoryPath GetGlobalTypingsCacheLocation() => throw new InvalidOperationException("Unused original mock service");
        public TypeScript.TsOptions.SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path) => throw new InvalidOperationException("Unused original mock service");
        public Slice<RootedFilePath> GetRedirectTargets(PathKey path) => throw new InvalidOperationException("Unused original mock service");
        public RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(HasFileName file) => throw new InvalidOperationException("Unused original mock service");
        public RootedDirectoryPath GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory) => throw new InvalidOperationException("Unused original mock service");
        public TypeScript.PackageJson.InfoCacheEntry? GetPackageJsonInfo(RootedFilePath path) => throw new InvalidOperationException("Unused original mock service");
        public ResolutionMode GetDefaultResolutionModeForFile(HasFileName file) => throw new InvalidOperationException("Unused original mock service");
        public ResolvedModule? GetResolvedModuleFromModuleSpecifier(HasFileName file, Node specifier) => throw new InvalidOperationException("Unused original mock service");
        public ResolutionMode GetModeForUsageLocation(HasFileName file, Node specifier) => throw new InvalidOperationException("Unused original mock service");
    }

    // Independent original Program + GetModuleSpecifiers observation: the only
    // difference is a read-only sibling /project.ts, never a Program source.
    private static void RootIndexShadowProbe()
    {
        using var disk = new Fixture();
        disk.Write("index.ts", "export const x=1;");
        disk.Write("b.ts", "export {x} from './index';");
        disk.Write("sub/main.ts", "import {x} from '../b';export const out=x;");
        disk.Config("{\"files\":[\"sub/main.ts\"]}");
        var project = Project.Load(disk.ConfigPath);
        var checker = project.Checker;
        var target = project.UserFiles.Single(file => file.FileName() == disk.Path("index.ts"));
        var context = project.UserFiles.Single(file => file.FileName() == disk.Path("sub/main.ts"));
        TypeScript.ModuleSpecifiers.ModuleSpecifiersResult Run() => TypeScript.ModuleSpecifiers.ModuleSpecifiers.GetModuleSpecifiers(target.AsNode().Symbol(), checker, project.Options, context, project, new TypeScript.ModuleSpecifiers.UserPreferences { ImportModuleSpecifierPreference = TypeScript.ModuleSpecifiers.ImportModuleSpecifierPreference.ProjectRelative }, new TypeScript.ModuleSpecifiers.ModuleSpecifierOptions(), false);
        string shadow = disk.Root + ".ts";
        Require(!File.Exists(shadow), "Fresh shadow probe must not overwrite another file.");
        var absent = Run();
        Equal(absent.Kind, TypeScript.ModuleSpecifiers.ResultKind.Relative);
        Sequence(GoSlices.Values(absent.Specifiers).Select(specifier => specifier.AsString()), new[] { ".." });
        Require(absent.AmbientModuleSymbol == null, "Original absent-shadow ambient symbol is nil.");
        try
        {
            File.WriteAllText(shadow, "class OutsideSourceMustNotBeParsed {};");
            var present = Run();
            Equal(present.Kind, TypeScript.ModuleSpecifiers.ResultKind.Relative);
            Sequence(GoSlices.Values(present.Specifiers).Select(specifier => specifier.AsString()), new[] { "../index" });
            Require(present.AmbientModuleSymbol == null, "Original present-shadow ambient symbol is nil.");
            Require(project.UserFiles.All(file => file.FileName() != shadow), "Shadow metadata must not enter source set.");
            Reject(() => new ProjectDiskHost(disk.Root).FileExists(shadow), "outsideRoot");
            File.Delete(shadow);
            File.CreateSymbolicLink(shadow, disk.Path("index.ts"));
            Reject(() => Run(), "symlink");
        }
        finally
        {
            File.Delete(shadow);
        }
    }

    private sealed class Fixture : IDisposable
    {
        internal string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tscheck-support-" + Guid.NewGuid().ToString("N"));
        internal string ConfigPath => Path("tscheck.json");

        internal Fixture() => Directory.CreateDirectory(Root);
        internal string Path(string relative) => System.IO.Path.Combine(Root, relative);
        internal void Write(string relative, string text)
        {
            string path = Path(relative);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        internal void Config(string json) => Write("tscheck.json", json);
        public void Dispose() => Directory.Delete(Root, true);
    }
}
