// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// compiler/contentmapper_test.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Five complete environment/error assertion domains. The three historical
// diagnostic-channel adaptations now use the genuine upstream semantic API.
// The generator body now retains the complete original bulk/incremental
// assertions; historical construction-only provenance remains immutable.
// Current restoration: validation/protocols-corpus/mapper-generator-restoration-provenance.json.
using Configuration.Checks;
using TypeScript.Compiler;
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.TsOptions;
using CompilerProgram = TypeScript.Compiler.Program;
using Bundled = TypeScript.Bundled.Bundled;
using ConfigParser = TypeScript.TsOptions.TsOptions;
using SpanKind = TypeScript.SpanMap.Kind;

namespace Program.Checks;
public static class CompilerMapperChecks
{
    public static IReadOnlyList<string> Cases { get; } = [
        nameof(TestContentMapperVirtualExtensionSetsImpliedNodeFormat),
        nameof(TestContentMapperDirectivesPreserveIncrementalGlobals),
        nameof(TestCompositeProjectContentMapperSupplementalRoots),
        nameof(TestContentMapperInvalidMappings),
        nameof(TestContentMapperSourceFileState),
        nameof(TestContentMapperProjectErrorDiagnostics)];

    public static void Run(string name)
    {
        switch (name)
        {
            case nameof(TestContentMapperVirtualExtensionSetsImpliedNodeFormat): TestContentMapperVirtualExtensionSetsImpliedNodeFormat(); break;
            case nameof(TestContentMapperDirectivesPreserveIncrementalGlobals): TestContentMapperDirectivesPreserveIncrementalGlobals(); break;
            case nameof(TestCompositeProjectContentMapperSupplementalRoots): TestCompositeProjectContentMapperSupplementalRoots(); break;
            case nameof(TestContentMapperInvalidMappings): TestContentMapperInvalidMappings(); break;
            case nameof(TestContentMapperSourceFileState): TestContentMapperSourceFileState(); break;
            case nameof(TestContentMapperProjectErrorDiagnostics): TestContentMapperProjectErrorDiagnostics(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private sealed class FakeProject(Func<string, string, (Result, Exception?)> transform) : IProject
    {
        public Exception? Refresh() => null;
        public (List<string>?, Exception?) Identities() => (null, null);
        public (string, Exception?) Identity(Mapper mapper) => ("test", null);
        public (List<RootedFilePath>?, Exception?) WatchedFiles() => (null, null);
        public List<OptionDiagnostic>? Diagnostics() => null;
        public Exception? Close() => null;
        public (Result, Exception?) Transform(Mapper mapper, Request request) => transform(request.FileName.AsString(), request.Content);
    }

    private static CompilerOptions DefaultOptions() => new()
    {
        SkipLibCheck = Tristate.True, Module = ModuleKind.ESNext, ModuleResolution = ModuleResolutionKind.Bundler
    };

    private static CompilerProgram NewProgram(FakeProject project, Dictionary<string, string> files, string[] roots, CompilerOptions? options = null)
    {
        // Original seed writes are replaced by construction of the same read-only
        // map. Original bundled-embedding skip is unnecessary: these are actual
        // embedded pinned library bytes, not application substitutes.
        var fs = Bundled.WrapFS(new ConfigurationFileSystem(files.Select(pair => new ConfigurationFileSystem.Input(pair.Key, pair.Value)), CaseSensitivity.CaseInsensitive));
        var config = ConfigParser.NewParsedCommandLine(options ?? DefaultOptions(), roots.Select(RootedFilePath.FromGoString).ToList(), null, RootedDirectoryPath.FromGoString("/"), fs.CaseSensitivity());
        config.ParsedConfig.ContentMappers = [new() { Package = "vue", Extensions = [".vue"], Name = "vue-mapper", Version = "1.0.0" }];
        return CompilerProgram.NewProgram(new()
        {
            Config = config, Host = CompilerHosts.NewCompilerHost(fs, Bundled.LibPath(), contentMapperProject: project), SingleThreaded = Tristate.True
        });
    }

    private static List<Diagnostic> CollectContentMapperDiagnostics(CompilerProgram program)
    {
        // Original collectContentMapperDiagnostics: preserve all three actual
        // producers and their order. No additional raw inclusion channel.
        List<Diagnostic> diagnostics = [];
        diagnostics.AddRange(program.GetSyntacticDiagnostics() ?? []);
        diagnostics.AddRange(program.GetSemanticDiagnostics() ?? []);
        diagnostics.AddRange(program.GetProgramDiagnostics() ?? []);
        return diagnostics;
    }

    private static void TestContentMapperVirtualExtensionSetsImpliedNodeFormat()
    {
        var program = NewProgram(new((_, _) => (new() { Text = "export {};", VirtualExtension = ".mts", Mappings = SpanMaps.New(null) }, null)),
            new() { ["/src/Component.vue"] = "<template />" }, ["/src/Component.vue"],
            new() { SkipLibCheck = Tristate.True, Module = ModuleKind.NodeNext, ModuleResolution = ModuleResolutionKind.NodeNext });
        var file = program.GetSourceFile(RootedFilePath.FromGoString("/src/Component.vue"));
        Check.True(file is not null, nameof(TestContentMapperVirtualExtensionSetsImpliedNodeFormat));
        Check.Equal(program.GetSourceFileMetaData(PathKey.FromGoString(file!.PathKey())).ImpliedNodeFormat, ResolutionMode.ESNext, nameof(TestContentMapperVirtualExtensionSetsImpliedNodeFormat));
    }

    private static void TestContentMapperDirectivesPreserveIncrementalGlobals()
    {
        const string text = "export function values() { function* generator() { yield 1; } }";
        foreach (var policy in new[] { MappedDiagnosticDirectivePolicy.Ignore, MappedDiagnosticDirectivePolicy.Expect })
        {
            var program = NewProgram(new((_, content) => (new()
            {
                Text = content, VirtualExtension = ".ts",
                Mappings = SpanMaps.New([new(0, content.Length, 0, content.Length, SpanKind.Verbatim, Feature.All)]),
                DiagnosticDirectives = [new()
                {
                    VirtualRange = new(0, content.Length), OriginalRange = new(0, content.Length), Policy = policy,
                    Source = "vue", UnusedCode = 2578, UnusedMessageText = "Unused mapped expect directive."
                }]
            }, null)), new() { ["/src/Component.vue"] = text }, ["/src/Component.vue"],
                new() { Lib = ["lib.es5.d.ts"], SkipLibCheck = Tristate.True, Module = ModuleKind.ESNext, ModuleResolution = ModuleResolutionKind.Bundler });
            Check.Equal(program.GetGlobalDiagnostics()?.Count ?? 0, 0, "original pre-incremental global count");
            var file = program.GetSourceFile(RootedFilePath.FromGoString("/src/Component.vue"));
            Check.True(file is not null, "original mapped file exists");
            var diagnostics = program.GetSemanticDiagnosticsForIncremental(default, [file!])[file!];
            int globals = 0, unused = 0;
            foreach (var diagnostic in diagnostics ?? [])
            {
                if (diagnostic.File() is null)
                {
                    Check.Equal(diagnostic.Code(), Diagnostics.Cannot_find_global_type_0.Code(), "original fileless diagnostic code");
                    Check.Equal(diagnostic.MessageArgs()[0], "IterableIterator", "original missing global name");
                    globals++;
                }
                else
                {
                    Check.True(ReferenceEquals(diagnostic.File(), file), "original mapped diagnostic file identity");
                    Check.Equal(diagnostic.Source(), "vue", "original mapped diagnostic source");
                    Check.Equal(diagnostic.Code(), 2578, "original unused expect diagnostic code");
                    unused++;
                }
            }
            Check.Equal(globals, 1, "original incremental missing global count");
            Check.Equal(unused, policy == MappedDiagnosticDirectivePolicy.Expect ? 1 : 0, "original policy-dependent unused directive count");
        }
    }

    private static void TestCompositeProjectContentMapperSupplementalRoots()
    {
        var project = new FakeProject((_, _) => (new()
        {
            Text = "export {};", VirtualExtension = ".ts", Mappings = SpanMaps.New(null),
            Supplemental = [new() { Text = "export {};", VirtualExtension = ".mts", Mappings = SpanMaps.New(null) }]
        }, null));
        CompilerOptions Options() => new() { Composite = Tristate.True, SkipLibCheck = Tristate.True, Module = ModuleKind.ESNext, ModuleResolution = ModuleResolutionKind.Bundler };
        int code = Diagnostics.File_0_is_not_listed_within_the_file_list_of_project_1_Projects_must_list_all_files_or_use_an_include_pattern.Code();
        var listed = NewProgram(project, new() { ["/src/Component.vue"] = "<template />" }, ["/src/Component.vue"], Options());
        Check.True(!CollectContentMapperDiagnostics(listed).Any(diagnostic => diagnostic.Code() == code), "supplemental covered by listed canonical root");
        var imported = NewProgram(project, new() { ["/src/index.ts"] = "import \"./Component.vue\";", ["/src/Component.vue"] = "<template />" }, ["/src/index.ts"], Options());
        Check.Equal(CollectContentMapperDiagnostics(imported).Count(diagnostic => diagnostic.Code() == code), 2, "imported canonical and supplemental unlisted count");
    }

    private static void TestContentMapperInvalidMappings()
    {
        const string transformed = "export const x = 1;\n", original = "<template>x</template>\n";
        var mappings = SpanMaps.New([new(0, 10, 0, 0, SpanKind.Atom, Feature.None), new(5, transformed.Length, 0, 0, SpanKind.Atom, Feature.None)]);
        var program = NewProgram(new((_, _) => (new() { Text = transformed, VirtualExtension = ".ts", Mappings = mappings }, null)),
            new() { ["/src/app.ts"] = "import \"./Component.vue\";", ["/src/Component.vue"] = original }, ["/src/app.ts"]);
        Check.True(CollectContentMapperDiagnostics(program).Any(diagnostic => diagnostic.Code() == Diagnostics.The_content_mapper_0_produced_overlapping_or_out_of_order_position_mappings_near_virtual_offset_1.Code()), "invalid mapping diagnostic");
    }

    private static void TestContentMapperSourceFileState()
    {
        var successful = NewProgram(new((_, _) => (new() { Text = "export {};", VirtualExtension = ".ts", Mappings = SpanMaps.New(null) }, null)), new() { ["/src/empty.vue"] = "" }, ["/src/empty.vue"]);
        var file = successful.GetSourceFile(RootedFilePath.FromGoString("/src/empty.vue"));
        Check.True(file is not null, "synthesized empty file exists");
        Check.Equal(file!.OriginalText(), "", "synthesized original text");
        Check.Equal(file.ContentMapper(), "vue-mapper@1.0.0", "synthesized mapper");
        Check.True(!file.IsContentMapperFailureStub(), "successful transform is not failure stub");
        var failed = NewProgram(new((_, _) => (default, new Exception("failed"))), new() { ["/src/fail.vue"] = "original" }, ["/src/fail.vue"]);
        file = failed.GetSourceFile(RootedFilePath.FromGoString("/src/fail.vue"));
        Check.True(file is not null, "failed transform file exists");
        Check.Equal(file!.OriginalText(), "original", "failed original text");
        Check.Equal(file.ContentMapper(), "vue-mapper@1.0.0", "failed mapper");
        Check.True(file.IsContentMapperFailureStub(), "failed transform retains stub");
        var localized = NewProgram(new((_, _) => (default, TransformError.NewTransformError(TransformErrorKind.Project, new ProjectError { Kind = ProjectErrorKind.MalformedResponse }))), new() { ["/src/fail.vue"] = "original" }, ["/src/fail.vue"]);
        Check.True(CollectContentMapperDiagnostics(localized).Any(diagnostic => diagnostic.MessageChain()?.Any(message => message.Code() == Diagnostics.The_content_mapper_returned_a_project_response_that_could_not_be_decoded.Code()) == true), "localized project response diagnostic");
    }

    private static void TestContentMapperProjectErrorDiagnostics()
    {
        (ProjectErrorKind Kind, Message Diagnostic, string Text)[] rows = [
            (ProjectErrorKind.MissingConfigIdentity, Diagnostics.The_content_mapper_did_not_return_configIdentity_which_is_required_when_the_content_mapper_has_dynamicConfig_Colon_true_in_its_package_json, "The content mapper did not return 'configIdentity', which is required when the content mapper has '\"dynamicConfig\": true' in its package.json."),
            (ProjectErrorKind.UnexpectedConfigIdentity, Diagnostics.The_content_mapper_returned_configIdentity_which_is_only_allowed_when_it_declares_dynamicConfig_Colon_true_in_its_package_json, "The content mapper returned 'configIdentity', which is only allowed when it declares '\"dynamicConfig\": true' in its package.json."),
            (ProjectErrorKind.UnexpectedWatchedFiles, Diagnostics.The_content_mapper_returned_watchedFiles_which_is_only_allowed_when_it_declares_dynamicConfig_Colon_true_in_its_package_json, "The content mapper returned 'watchedFiles', which is only allowed when it declares '\"dynamicConfig\": true' in its package.json.")];
        foreach (var row in rows)
        {
            var message = FileLoader.ContentMapperProjectErrorDiagnostic(new ProjectError { Kind = row.Kind });
            Check.Equal(message.Code(), row.Diagnostic.Code(), "project error code");
            Check.Equal(message.Localize(), row.Text, "project error default locale text");
        }
    }
}
