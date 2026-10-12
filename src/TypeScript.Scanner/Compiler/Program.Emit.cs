// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/compiler/program.go:1860–2090, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Collections.Concurrent;
using TypeScript.Scanner;
using TypeScript.SourceMap;
using TextWriter = TypeScript.DiagnosticPrinting.TextWriter;

namespace TypeScript.Compiler;
// emitter.go:24–31.
public enum EmitOnly : byte
{
    EmitAll,
    EmitOnlyJs,
    EmitOnlyDts,
    EmitOnlyBuilderSignature
}

// program.go:1860. A reference owner allows the callback to mark a skipped declaration write.
public sealed class WriteFileData
{
    public int SourceMapUrlPos;
    public object? BuildInfo;
    public Slice<Diagnostic> Diagnostics;
    public bool SkippedDtsWrite;
    public SourceFile? SourceFile;
}

// program.go:1868. Text is an actual raw Go byte string at the callback boundary.
public delegate Exception? WriteFile(RootedFilePath fileName, string text, WriteFileData data);
public struct EmitOptions
{
    public Slice<SourceFile> TargetSourceFiles;
    public EmitOnly EmitOnly;
    public bool ForceEmit;
    public WriteFile? WriteFile;
}

public sealed class EmitResult
{
    public bool EmitSkipped;
    public Slice<Diagnostic> Diagnostics;
    public Slice<RootedFilePath> EmittedFiles;
    public Slice<SourceMapEmitResult> SourceMaps;
}

public sealed class SourceMapEmitResult
{
    public Slice<ByteStringValue> InputSourceFileNames;
    public RawSourceMap SourceMap = null!;
    public string GeneratedFile = "";
}

public sealed partial class Program
{
    // program.go:1647. LoadOrStore preserves the first published diagnostics owner.
    private TypeScript.Collections.SyncMap<SourceFile, Slice<Diagnostic>>? declarationDiagnosticCache;
    private TypeScript.Collections.SyncMap<SourceFile, Slice<Diagnostic>> DeclarationDiagnosticCache => LazyInitializer.EnsureInitialized(ref declarationDiagnosticCache, static () => new());

    // program.go:1890. Work starts while queued and results are reduced in source input order.
    public EmitResult? Emit(CancellationToken context = default, EmitOptions options = default)
    {
        Action? releaseTrace = hosts.Tracing?.Push(TypeScript.Tracing.Phase.Emit, "emit", null, true);
        try
        {
            if (!options.ForceEmit && options.EmitOnly != EmitOnly.EmitOnlyBuilderSignature)
            {
                var result = HandleNoEmitOptions(context, this, options.TargetSourceFiles, null);
                if (result is not null || context.IsCancellationRequested)
                    return result;
            }

            string newLine = Options().NewLine.GetNewLineCharacter();
            var writerPool = new ConcurrentBag<TextWriter>();
            var wg = Core.NewWorkGroup(SingleThreaded());
            Slice<Emitter> emitters = default;
            bool forceDtsEmit = options.EmitOnly == EmitOnly.EmitOnlyBuilderSignature || options.ForceEmit && options.EmitOnly == EmitOnly.EmitOnlyDts;
            bool forceJsEmit = options.ForceEmit && options.EmitOnly == EmitOnly.EmitOnlyJs;
            foreach (var sourceFile in getSourceFilesToEmit(options.TargetSourceFiles, forceDtsEmit, forceJsEmit))
            {
                var emitter = new Emitter
                {
                    sourceFile = sourceFile,
                    emitOnly = options.EmitOnly,
                    forceEmit = options.ForceEmit,
                    writeFile = options.WriteFile!,
                    tr = hosts.Tracing!
                };
                emitters = emitters.Append(emitter);
                var work = new EmitWork(this, context, emitter, writerPool, newLine, forceDtsEmit, forceJsEmit, options.ForceEmit && options.EmitOnly == EmitOnly.EmitOnlyDts);
                wg.Queue(work.Run);
            }

            wg.RunAndWait();
            var results = emitters.IsNil ? default : Slice<EmitResult?>.Make(emitters.Count);
            for (int i = 0; i < emitters.Count; i++)
                results[i] = emitters[i].emitResult;
            return CombineEmitResults(results);
        }
        finally
        {
            releaseTrace?.Invoke();
        }
    }

    // Go closure captures this per-job state; its bound callback retains one explicit owner.
    private sealed class EmitWork(Program program, CancellationToken context, Emitter emitter, ConcurrentBag<TextWriter> pool, string newLine, bool forceDts, bool forceJs, bool forceDeclarationMap)
    {
        internal void Run()
        {
            var(host, done) = CompilerEmitHost.New(context, program, emitter.sourceFile);
            try
            {
                emitter.host = host;
                if (!pool.TryTake(out var writer))
                    writer = TextWriter.NewTextWriter(newLine, 0);
                writer.Clear();
                emitter.writer = writer;
                emitter.paths = TypeScript.OutputPaths.OutputPaths.GetOutputPathsFor(emitter.sourceFile, host.Options(), host, new() { Dts = forceDts, Js = forceJs, DeclarationMap = forceDeclarationMap });
                emitter.emit();
                emitter.writer = null!;
                pool.Add(writer);
            }
            finally
            {
                done();
            }
        }
    }

    // program.go:1974. Nil source-map results stay nil; no sort changes the input order.
    public static EmitResult CombineEmitResults(Slice<EmitResult?> results)
    {
        var result = new EmitResult();
        foreach (var item in results)
        {
            if (item is null)
                continue;
            result.EmitSkipped |= item.EmitSkipped;
            result.Diagnostics = result.Diagnostics.Append(item.Diagnostics);
            result.EmittedFiles = result.EmittedFiles.Append(item.EmittedFiles);
            if (!item.SourceMaps.IsNil)
                result.SourceMaps = result.SourceMaps.Append(item.SourceMaps);
        }

        return result;
    }

    // program.go:1999. Explicit empty targets differ from nil targets under noEmit.
    public static EmitResult? HandleNoEmitOptions(CancellationToken context, Program program, Slice<SourceFile> files, Func<EmitResult?>? emitBuildInfo)
    {
        if (program.Options().NoEmit != Tristate.True)
        {
            if (program.Options().NoEmitOnError != Tristate.True)
                return null;
            var diagnostics = GetDiagnosticsOfAnyProgram(context, program, files, true, program.GetBindDiagnostics, program.GetSemanticDiagnostics);
            return diagnostics.Count == 0 ? null : new EmitResult
            {
                Diagnostics = diagnostics,
                EmitSkipped = true
            };
        }

        if (!files.IsNil)
            return new EmitResult
            {
                EmitSkipped = true
            };
        if (emitBuildInfo is not null)
        {
            var result = emitBuildInfo();
            if (result is not null)
                return result;
        }

        return new EmitResult();
    }

    // program.go:2033. Syntax failure gates semantic/declaration work exactly as upstream.
    public static Slice<Diagnostic> GetDiagnosticsOfAnyProgram(CancellationToken context, Program program, Slice<SourceFile> files, bool skipNoEmitCheckForDtsDiagnostics, Func<CancellationToken, SourceFile?, Slice<Diagnostic>> getBindDiagnostics, Func<CancellationToken, SourceFile?, Slice<Diagnostic>> getSemanticDiagnostics)
    {
        var allDiagnostics = GoSlices.Clip(program.GetConfigFileParsingDiagnostics());
        int configLength = allDiagnostics.Count;
        var syntax = AppendDiagnosticsForFiles(default, files, context, program.GetSyntacticDiagnostics);
        if (syntax.Count > 0)
            allDiagnostics = allDiagnostics.Append(program.processedFiles.contentMapperDiagnostics);
        allDiagnostics = allDiagnostics.Append(syntax);
        if (allDiagnostics.Count == configLength)
        {
            allDiagnostics = allDiagnostics.Append(program.GetProgramDiagnostics());
            _ = AppendDiagnosticsForFiles(default, files, context, getBindDiagnostics);
            if (program.Options().ListFilesOnly != Tristate.True)
            {
                allDiagnostics = allDiagnostics.Append(program.GetGlobalDiagnostics(context));
                if (allDiagnostics.Count == configLength)
                {
                    allDiagnostics = AppendDiagnosticsForFiles(allDiagnostics, files, context, getSemanticDiagnostics);
                    allDiagnostics = allDiagnostics.Append(program.GetGlobalDiagnostics(context));
                }

                if ((skipNoEmitCheckForDtsDiagnostics || program.Options().NoEmit == Tristate.True) && program.Options().GetEmitDeclarations() && allDiagnostics.Count == configLength)
                    allDiagnostics = AppendDiagnosticsForFiles(allDiagnostics, files, context, program.GetDeclarationDiagnostics);
            }
        }

        return allDiagnostics;
    }

    private static Slice<Diagnostic> AppendDiagnosticsForFiles(Slice<Diagnostic> diagnostics, Slice<SourceFile> files, CancellationToken context, Func<CancellationToken, SourceFile?, Slice<Diagnostic>> getDiagnostics)
    {
        if (files.IsNil)
            return diagnostics.Append(getDiagnostics(context, null));
        foreach (var file in files)
            diagnostics = diagnostics.Append(getDiagnostics(context, file));
        return diagnostics;
    }
}
