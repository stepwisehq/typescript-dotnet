// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored emit observation reusing the accepted original compiler input preparation.
// Pinned harnessutil.go:602–700 diagnostic ordering and 721–855 output ordering.
// Full original channel assertions remain separately tracked; raw files alone are not acceptance.
using System.Text.Json.Nodes;
using TypeScript.Compiler;
using TypeScript.Scanner;
using CP = TypeScript.Compiler.Program;

namespace Program.Checks;
public static partial class ProgramRunner
{
    public static JsonObject ObserveCompilerEmit(string name, string sourceName, string sourceHash, string configurationName, string content, Dictionary<string, string> configuration, string testDataDirectory, bool originalPreConfigurationSkip)
    {
        if (originalPreConfigurationSkip)
            return new()
            {
                ["name"] = name,
                ["status"] = "skipped-original-before-configuration",
                ["sourceSha256"] = sourceHash,
                ["configuration"] = configurationName
            };
        var compilation = CompilerNoEmitHarness.Prepare(sourceName, content, configuration, testDataDirectory);
        var preErrors = CollectEmitDiagnostics(compilation.PreProgram, compilation.Harness, pre: true);
        compilation = compilation.ConstructActualProgram();
        var program = compilation.Program;
        // The original cached source host shares the parsed AST between these generations.
        // Validate the original parent/union invariants before and after its real consumption.
        CompilerOriginalInvariantChecks.Validate(program);
        var recorder = new EmitOutputRecorder(compilation.Host.FS());
        var emitResult = program.Emit(default, new EmitOptions { WriteFile = recorder.Write }) ?? throw new InvalidOperationException("Uncancelled original emit returned nil.");
        var postErrors = CollectEmitDiagnostics(program, compilation.Harness, pre: false);
        var errors = DiagnosticCountMismatch(preErrors, postErrors);
        CompilerOriginalInvariantChecks.Validate(program);
        var observer = new NoEmitObserver();
        var diagnostics = observer.Diagnostics(errors);
        var emittedDiagnostics = observer.Diagnostics(emitResult.Diagnostics);
        JsonObject result = new()
        {
            ["name"] = name,
            ["status"] = "observed-managed-emit",
            ["sourceSha256"] = sourceHash,
            ["configuration"] = configurationName,
            ["diagnostics"] = diagnostics,
            ["emitResult"] = new JsonObject
            {
                ["emitSkipped"] = emitResult.EmitSkipped,
                ["diagnostics"] = emittedDiagnostics,
                ["emittedFiles"] = Value(emitResult.EmittedFiles),
                ["sourceMaps"] = ObserveSourceMaps(emitResult.SourceMaps)
            },
            ["owners"] = observer.Owners,
            ["typesAndSymbolsDisabled"] = compilation.Harness.NoTypesAndSymbols,
            ["originalOptionPolicy"] = OriginalNoEmitOptionPolicy(program.Options()),
            ["originalIncrementalWrapperRequired"] = program.Options().Incremental == Tristate.True,
            ["diagnosticCounts"] = new JsonObject
            {
                ["pre"] = preErrors.Count,
                ["post"] = postErrors.Count
            },
            ["originalAssertions"] = new JsonObject
            {
                ["unionOrderingBeforeEmit"] = true,
                ["parentPointersBeforeEmit"] = true,
                ["unionOrderingAfterEmit"] = true,
                ["parentPointersAfterEmit"] = true
            },
            ["pendingOriginalChannels"] = new JsonArray()
        };
        compilation = compilation with
        {
            Trace = compilation.Tracer.ToString()
        };
        var captured = recorder.Capture(compilation, emitResult, errors);
        // compiler_runner.go:318-326 mutates baseline units after content mapping.
        foreach (var units in new[]
        {
            compilation.OriginalInputFiles,
            compilation.OriginalOtherFiles
        }

        )
            for (int i = 0; i < units.Count; i++)
                if (program.GetSourceFile(TsPath.ToRootedFilePath(units[i].UnitName, compilation.Harness.CurrentDirectory))is { } mapped && mapped.ContentMapper() != "")
                    units[i] = units[i] with
                    {
                        Content = mapped.Text()
                    };
        var outputs = (Js: ObserveFiles(captured.JS), Dts: ObserveFiles(captured.DTS), Maps: ObserveFiles(captured.Maps));
        result["js"] = outputs.Js;
        result["dts"] = outputs.Dts;
        result["maps"] = outputs.Maps;
        if (result["originalOptionPolicy"]!["decision"]!.GetValue<string>() == "run")
        {
            var channels = new JsonObject();
            result["channels"] = channels;
            void Channel(string channel, Action action)
            {
                try
                {
                    action();
                    channels[channel] = new JsonObject
                    {
                        ["status"] = "passed-original-assertions"
                    };
                }
                catch (Exception error)
                {
                    channels[channel] = new JsonObject
                    {
                        ["status"] = "failed-original-assertions",
                        ["error"] = error.ToString()
                    };
                }
            }

            Channel("diagnostics", () =>
            {
                HashSet<string> mappedNames = new(StringComparer.Ordinal);
                foreach (var file in program.GetSourceFiles())
                    if (program.GetContentMapper(file)is not null)
                        mappedNames.Add(file.FileName());
                var errorFiles = compilation.OriginalConfigFiles.Concat(compilation.OriginalInputFiles).Concat(compilation.OriginalOtherFiles).Where(file => !mappedNames.Contains(file.UnitName)).ToList();
                Slice<Diagnostic> errorDiagnostics = errors;
                if (mappedNames.Count != 0)
                {
                    errorDiagnostics = default;
                    foreach (var diagnostic in errors)
                        if (diagnostic.File()is null || !mappedNames.Contains(diagnostic.File()!.FileName()))
                            errorDiagnostics = errorDiagnostics.Append(diagnostic);
                }

                result["errorBaselineBase64"] = Bytes(CompilerEmitBaselines.ObserveErrorBaseline(errorFiles, errorDiagnostics, program.Options().Pretty == Tristate.True));
            });
            Channel("contentMapper", () =>
            {
                var contentMapper = CompilerEmitBaselines.ContentMapperBaseline(captured);
                result["contentMapperBaselineBase64"] = contentMapper is null ? null : Bytes(contentMapper);
            });
            string? outputSkipReason = CompilerEmitBaselines.OriginalOutputSkipReason(TsPath.GetBaseFileName(sourceName));
            bool hasNonDts = compilation.OriginalInputFiles.Concat(compilation.OriginalOtherFiles).Any(file => !TsPath.FileExtensionIs(file.UnitName, TsPath.ExtensionDts));
            result["outputChannelDisposition"] = !hasNonDts ? "original-not-applicable" : outputSkipReason is null ? "run" : "original-harness-only-skip";
            if (outputSkipReason is not null)
                result["outputHarnessOnlySkipReasonBase64"] = Bytes(outputSkipReason);
            channels["output"] = new JsonObject
            {
                ["status"] = outputSkipReason is null || !hasNonDts ? "passed-original-assertions" : "skipped-original-channel"
            };
            if (hasNonDts && outputSkipReason is null)
                Channel("output", () =>
                {
                    var declaration = CompilerEmitBaselines.RecompileDeclarations(captured);
                    if (declaration is not null)
                    {
                        var declarationObserver = new NoEmitObserver();
                        result["declarationRecompile"] = new JsonObject
                        {
                            ["diagnostics"] = declarationObserver.Diagnostics(declaration.Compilation.Diagnostics),
                            ["owners"] = declarationObserver.Owners,
                            ["inputs"] = ObserveFiles(declaration.Inputs),
                            ["otherFiles"] = ObserveFiles(declaration.OtherFiles)
                        };
                    }

                    CompilerEmitCompilation? noCheckRepeat = null;
                    if (captured.Options.NoCheck != Tristate.True && captured.Options.NoEmit != Tristate.True)
                    {
                        var repeatOptions = captured.Options.Clone();
                        repeatOptions.NoCheck = Tristate.True;
                        var repeat = CompilePreparedEmit(CompilerNoEmitHarness.PrepareFiles(compilation.OriginalInputFiles.Select(file => new CompilerFixtureParser.Unit(file.UnitName, file.Content, [])).ToList(), compilation.OriginalOtherFiles.Select(file => new CompilerFixtureParser.Unit(file.UnitName, file.Content, [])).ToList(), compilation.Harness, repeatOptions, compilation.Harness.CurrentDirectory, compilation.ActualConfiguration, compilation.OriginalSymlinks, compilation.TestDataDirectory, compilation.Header));
                        noCheckRepeat = repeat;
                        var differences = new JsonArray();
                        // Original comparison visits noCheck output only; it does not assert the inverse set.
                        void Compare(List<CompilerEmitTestFile> actual, List<CompilerEmitTestFile> expected)
                        {
                            foreach (var file in actual)
                            {
                                var original = expected.FirstOrDefault(candidate => candidate.UnitName == file.UnitName);
                                if (original is null || original.Content != file.Content)
                                    differences.Add((JsonNode)new JsonObject { ["file"] = file.UnitName, ["status"] = original is null ? "missing-original" : "different", ["expectedBase64"] = original is null ? null : Bytes(original.Content), ["actualBase64"] = Bytes(file.Content) });
                            }
                        }

                        Compare(repeat.DTS, captured.DTS);
                        Compare(repeat.JS, captured.JS);
                        result["noCheckFileDifferences"] = differences;
                    }

                    result["javaScriptBaselineBase64"] = Bytes(CompilerEmitBaselines.JavaScriptBaseline(captured, declaration, noCheckRepeat));
                });
            Channel("sourceMap", () => result["sourceMapBaselineBase64"] = CompilerSourceMapBaselines.ObserveSourceMap(captured)is { } sourceMap ? Bytes(sourceMap) : null);
            Channel("sourceMapRecord", () => result["sourceMapRecordBaselineBase64"] = Bytes(CompilerSourceMapBaselines.ObserveSourceMapRecord(captured)));
            Channel("typesAndSymbols", () =>
            {
                var typeFiles = compilation.OriginalInputFiles.Concat(compilation.OriginalOtherFiles).Where(file => program.GetSourceFile(TsPath.ToRootedFilePath(file.UnitName, compilation.Harness.CurrentDirectory))is not null).Select(file => new CompilerBaselineWalker.File(file.UnitName, file.Content)).ToList();
                var types = compilation.Harness.NoTypesAndSymbols ? null : CompilerBaselineWalker.Observe(program, typeFiles, compilation.Header, errors.Count > 0);
                result["typesBase64"] = types is null ? null : Bytes(types.Types);
                result["symbolsBase64"] = types is null ? null : Bytes(types.Symbols);
                result["moduleResolutionBaselineBase64"] = program.Options().TraceResolution == Tristate.True ? Bytes(compilation.Trace.Length == 0 ? "<no content>" : compilation.Trace) : null;
            });
            Channel("moduleResolution", () => result["moduleResolutionBaselineBase64"] = program.Options().TraceResolution == Tristate.True ? Bytes(compilation.Trace.Length == 0 ? "<no content>" : compilation.Trace) : null);
            channels["unionOrdering"] = new JsonObject
            {
                ["status"] = "passed-original-assertions"
            };
            channels["parentPointers"] = new JsonObject
            {
                ["status"] = "passed-original-assertions"
            };
        }

        result["traceBase64"] = Bytes(compilation.Trace);
        return result;
    }

    // The original CompileFilesEx generation ordering shared by declaration/noCheck repeats.
    internal static CompilerEmitCompilation CompilePreparedEmit(CompilerNoEmitHarness.Compilation compilation)
    {
        var pre = CollectEmitDiagnostics(compilation.PreProgram, compilation.Harness, pre: true);
        compilation = compilation.ConstructActualProgram();
        CompilerOriginalInvariantChecks.Validate(compilation.Program);
        var recorder = new EmitOutputRecorder(compilation.Host.FS());
        var emitted = compilation.Program.Emit(default, new EmitOptions { WriteFile = recorder.Write }) ?? throw new InvalidOperationException("Uncancelled repeat emit returned nil.");
        var post = CollectEmitDiagnostics(compilation.Program, compilation.Harness, pre: false);
        CompilerOriginalInvariantChecks.Validate(compilation.Program);
        return recorder.Capture(compilation, emitted, DiagnosticCountMismatch(pre, post));
    }

    private static JsonArray ObserveFiles(IEnumerable<CompilerEmitTestFile> files)
    {
        JsonArray result = [];
        foreach (var file in files)
            result.Add((JsonNode)new JsonObject { ["nameBase64"] = Bytes(file.UnitName), ["contentBase64"] = Bytes(file.Content) });
        return result;
    }

    // harnessutil.go:638–667. Suggestions/declarations have different pre/post order.
    private static Slice<Diagnostic> CollectEmitDiagnostics(CP program, CompilerNoEmitHarness.HarnessOptions harness, bool pre)
    {
        Slice<Diagnostic> errors = default;
        errors = errors.Append(program.GetConfigFileParsingDiagnostics());
        errors = errors.Append(program.GetProgramDiagnostics());
        errors = errors.Append(program.GetSyntacticDiagnostics());
        errors = errors.Append(program.GetSemanticDiagnostics());
        errors = errors.Append(program.GetGlobalDiagnostics());
        if (pre && harness.CaptureSuggestions)
            errors = errors.Append(program.GetSuggestionDiagnostics());
        if (program.Options().GetEmitDeclarations())
            errors = errors.Append(program.GetDeclarationDiagnostics());
        if (!pre && harness.CaptureSuggestions)
            errors = errors.Append(program.GetSuggestionDiagnostics());
        return CP.SortAndDeduplicateDiagnostics(errors);
    }

    // harnessutil.go:670–697. Preserve the original warning and excess related diagnostics.
    private static Slice<Diagnostic> DiagnosticCountMismatch(Slice<Diagnostic> pre, Slice<Diagnostic> post)
    {
        if (pre.Count == post.Count)
            return post;
        var longer = post;
        var shorter = pre;
        if (pre.Count > post.Count)
        {
            longer = pre;
            shorter = post;
        }

        var diagnostic = Ast.NewCompilerDiagnostic(Diagnostics.NewAdHocMessage($"Pre-emit ({pre.Count}) and post-emit ({post.Count}) diagnostic counts do not match! This can indicate that a semantic _error_ was added by the emit resolver - such an error may not be reflected on the command line or in the editor, but may be captured in a baseline here!"));
        diagnostic = diagnostic.AddRelatedInfo(Ast.NewCompilerDiagnostic(Diagnostics.NewAdHocMessage("The excess diagnostics are:")));
        foreach (var item in longer)
        {
            bool matched = false;
            foreach (var candidate in shorter)
                if (Ast.CompareDiagnostics(item, candidate) == 0)
                {
                    matched = true;
                    break;
                }

            if (!matched)
                diagnostic = diagnostic.AddRelatedInfo(item);
        }

        return shorter.Append(diagnostic);
    }

    private static JsonNode? ObserveSourceMaps(Slice<SourceMapEmitResult> maps)
    {
        if (maps.IsNil)
            return null;
        JsonArray result = [];
        foreach (var item in maps)
        {
            var map = item.SourceMap;
            result.Add((JsonNode)new JsonObject { ["GeneratedFile"] = Value(item.GeneratedFile), ["InputSourceFileNames"] = Value(item.InputSourceFileNames), ["SourceMap"] = new JsonObject { ["Version"] = map.Version, ["File"] = Value(map.File), ["SourceRoot"] = Value(map.SourceRoot), ["Sources"] = Value(map.Sources), ["Names"] = Value(map.Names), ["Mappings"] = Value(map.Mappings), ["SourcesContent"] = map.SourcesContent is null ? null : Value(map.SourcesContent) } });
        }

        return result;
    }

    // recorderfs.go:27. Record only successful real filesystem writes, with canonical path identity.
    private sealed class EmitOutputRecorder(TypeScript.Vfs.IFS fs)
    {
        private readonly object gate = new();
        private readonly Dictionary<RootedFilePath, Output> outputs = new();
        private sealed record Output(RootedFilePath Path, string Text);
        internal Exception? Write(RootedFilePath path, string text, WriteFileData data)
        {
            var error = fs.WriteFile(path, text);
            if (error is not null)
                return error;
            var realPath = TsPath.RootedFilePathFromPath(fs.Realpath(path.AsPath()));
            lock (gate)
                outputs[realPath] = new(realPath, text);
            return null;
        }

        // harnessutil.go:743–817. Callback order is concurrent; original channel order follows inputs.
        internal CompilerEmitCompilation Capture(CompilerNoEmitHarness.Compilation prepared, EmitResult result, Slice<Diagnostic> diagnostics)
        {
            var compilation = new CompilerEmitCompilation
            {
                Prepared = prepared,
                Result = result,
                Diagnostics = diagnostics
            };
            Observe(prepared.Program, compilation);
            return compilation;
        }

        private void Observe(CP program, CompilerEmitCompilation compilation)
        {
            Dictionary<RootedFilePath, Output> snapshot;
            lock (gate)
                snapshot = new(outputs);
            Dictionary<RootedFilePath, Output> js = [], dts = [], maps = [];
            foreach (var(path, file)in snapshot)
            {
                if (path.HasJSFileExtension() || path.HasJSONFileExtension())
                    js[path] = file;
                else if (path.IsDeclarationFile())
                    dts[path] = file;
                else if (path.ExtensionIs(".map"))
                    maps[path] = file;
            }

            JsonArray jsResult = [], dtsResult = [], mapResult = [];
            foreach (var source in program.GetSourceFiles())
            {
                var path = RootedFilePath.FromValue(source.FileNameGoValue());
                compilation.Inputs.Add(new(path.AsString(), source.Text()));
                if (path.IsDeclarationFile())
                    continue;
                string extension = TypeScript.OutputPaths.OutputPaths.GetOutputExtensionForFileName(path, program.Options().Jsx);
                AppendOutput(js, GetOutputPath(program, path, extension), jsResult, compilation.JS, compilation.Outputs);
                AppendOutput(dts, GetOutputPath(program, path, path.DeclarationEmitExtension()), dtsResult, compilation.DTS, compilation.Outputs);
                AppendOutput(maps, GetOutputPath(program, path, extension + ".map"), mapResult, compilation.Maps, compilation.Outputs);
            }

            AppendRemaining(js, jsResult, compilation.JS);
            AppendRemaining(dts, dtsResult, compilation.DTS);
            AppendRemaining(maps, mapResult, compilation.Maps);
        }

        private static void AppendOutput(Dictionary<RootedFilePath, Output> remaining, RootedFilePath path, JsonArray target, List<CompilerEmitTestFile> files, List<CompilerEmitTestFile> outputs)
        {
            if (remaining.Remove(path, out var output))
            {
                target.Add((JsonNode)ObserveOutput(output));
                var file = new CompilerEmitTestFile(output.Path.AsString(), output.Text);
                files.Add(file);
                outputs.Add(file);
            }
        }

        private static void AppendRemaining(Dictionary<RootedFilePath, Output> remaining, JsonArray target, List<CompilerEmitTestFile> files)
        {
            foreach (var item in remaining.Values.OrderBy(item => item.Path.AsString(), StringComparer.Ordinal))
            {
                target.Add((JsonNode)ObserveOutput(item));
                files.Add(new(item.Path.AsString(), item.Text));
            }
        }

        private static JsonObject ObserveOutput(Output output) => new()
        {
            ["nameBase64"] = Bytes(output.Path.AsString()),
            ["contentBase64"] = Bytes(output.Text)
        };
        // harnessutil.go:823. Preserve original declarationDir/outDir selection, including its fallback.
        private static RootedFilePath GetOutputPath(CP program, RootedFilePath file, string extension)
        {
            var options = program.Options();
            var path = file;
            RootedDirectoryPath outDir;
            if (extension is ".d.ts" or ".d.mts" or ".d.cts" || extension.EndsWith(".ts", StringComparison.Ordinal) && extension.Contains(".d.", StringComparison.Ordinal))
            {
                outDir = options.DeclarationDir;
                if (outDir == default)
                    outDir = options.OutDir;
            }
            else
                outDir = options.OutDir;
            if (outDir != default)
            {
                var common = program.CommonSourceDirectory();
                if (common != default)
                {
                    var(relative, ok) = program.CaseSensitivity().RelativePathFromDirectory(common, file);
                    if (ok)
                        path = (options.OutDir == default ? outDir : options.OutDir).ResolveRelativeFile(relative);
                }
            }

            return extension == path.DeclarationEmitExtension() ? TypeScript.OutputPaths.OutputPaths.ChangeToDeclarationExtension(path, program) : path.ChangeExtension(extension);
        }
    }
}
