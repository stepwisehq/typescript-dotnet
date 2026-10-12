// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/compiler/emitter.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Transformers;
using TypeScript.Transformers.Declarations;
using TypeScript.Transformers.TsTransforms;
using TypeScript.Transformers.EsTransforms;
using TypeScript.Transformers.ModuleTransforms;
using TypeScript.Transformers.JsxTransforms;
using TypeScript.Transformers.Inliners;
using TypeScript.SourceMap;
using TypeScript.SpanMap;
using TextWriter = TypeScript.DiagnosticPrinting.TextWriter;

namespace TypeScript.Compiler;
internal sealed class Emitter
{
    internal CompilerEmitHost host;
    internal EmitOnly emitOnly;
    private readonly DiagnosticsCollection emitterDiagnostics = new();
    internal EmitTextWriter writer;
    internal TypeScript.OutputPaths.OutputFilePaths paths;
    internal SourceFile sourceFile;
    internal readonly EmitResult emitResult = new();
    internal bool forceEmit;
    internal WriteFile writeFile;
    internal TypeScript.Tracing.Tracing tr;
    // emitter.go:46.
    internal void emit()
    {
        Action releaseTrace = tr?.Push(TypeScript.Tracing.Phase.Emit, "emit", new() { ["path"] = sourceFile.PathKey() }, true);
        try
        {
            var emitContext = new EmitContext();
            var emitResolver = this.host.NewEmitResolver(emitContext);
            this.emitJSFile(emitResolver, this.sourceFile, this.paths.JsFilePath(), this.paths.SourceMapFilePath());
            this.emitDeclarationFile(emitResolver, this.sourceFile, this.paths.DeclarationFilePath(), this.paths.DeclarationMapPath());
            this.emitResult.Diagnostics = this.emitterDiagnostics.GetDiagnostics();
        }
        finally
        {
            releaseTrace?.Invoke();
        }
    }

    // emitter.go:62.
    private Slice<IDeclarationTransformer> getDeclarationTransformers(IEmitResolver emitResolver, SourceFile sourceFile, RootedFilePath declarationFilePath)
    {
        var forceDtsEmit = ((this.emitOnly == EmitOnly.EmitOnlyBuilderSignature) || (this.forceEmit && (this.emitOnly == EmitOnly.EmitOnlyDts)));
        return Slice<IDeclarationTransformer>.FromArray(new IDeclarationTransformer[] { DeclarationTransformer.NewDeclarationTransformer(this.host, emitResolver, this.host.Options(), declarationFilePath), SupplementalReferencesTransformer.NewSupplementalReferencesTransformer(this.host, sourceFile, declarationFilePath, forceDtsEmit) });
    }

    // emitter.go:70.
    private SourceFile runScriptTransformers(IEmitResolver emitResolver, SourceFile sourceFile)
    {
        Action releaseTrace = tr?.Push(TypeScript.Tracing.Phase.Emit, "transformNodes", new() { ["path"] = sourceFile.PathKey() }, false);
        try
        {
            {
                foreach (var transformer in (getScriptTransformers(emitResolver, this.host, sourceFile)))
                {
                    sourceFile = transformer.TransformSourceFile(sourceFile);
                }
            }

            return sourceFile;
        }
        finally
        {
            releaseTrace?.Invoke();
        }
    }

    // emitter.go:80.
    private (SourceFile, Slice<Diagnostic>) runDeclarationTransformers(IEmitResolver emitResolver, SourceFile sourceFile, RootedFilePath declarationFilePath, RootedFilePath declarationMapPath)
    {
        Action releaseTrace = tr?.Push(TypeScript.Tracing.Phase.Emit, "transformNodes", new() { ["path"] = sourceFile.PathKey() }, false);
        try
        {
            Slice<Diagnostic> diags = default;
            {
                foreach (var transformer in (this.getDeclarationTransformers(emitResolver, sourceFile, declarationFilePath)))
                {
                    sourceFile = transformer.TransformSourceFile(sourceFile);
                    diags = diags.Append(transformer.GetDiagnostics());
                }
            }

            return (sourceFile, diags);
        }
        finally
        {
            releaseTrace?.Invoke();
        }
    }

    // emitter.go:92.
    private static Transformer getModuleTransformer(TransformOptions opts)
    {
        switch (opts.CompilerOptions.GetEmitModuleKind())
        {
            case ModuleKind.Preserve:
            {
                return ESModuleTransformer.NewESModuleTransformer(opts);
            }

            case ModuleKind.ESNext:
            case ModuleKind.ES2022:
            case ModuleKind.ES2020:
            case ModuleKind.ES2015:
            case ModuleKind.Node20:
            case ModuleKind.Node18:
            case ModuleKind.Node16:
            case ModuleKind.NodeNext:
            case ModuleKind.CommonJS:
            {
                return ImpliedModuleTransformer.NewImpliedModuleTransformer(opts);
            }

            default:
            {
                return CommonJSModuleTransformer.NewCommonJSModuleTransformer(opts);
            }
        }
    }

    // emitter.go:114.
    internal static Slice<Transformer> getScriptTransformers(IEmitResolver emitResolver, ICompilerEmitHost host, SourceFile sourceFile)
    {
        if (((emitResolver == null) || (emitResolver.EmitContext() == null)))
        {
            throw new InvalidOperationException("Script transformers require an EmitResolver with an EmitContext");
        }

        var emitContext = emitResolver.EmitContext();
        Slice<Transformer> tx = default;
        var options = host.Options();
        var importElisionEnabled = (!(options.VerbatimModuleSyntax == Tristate.True) && !Ast.IsInJSFile(sourceFile.AsNode()));
        var jsxTransformEnabled = (options.GetJSXTransformEnabled() && (sourceFile.LanguageVariant == LanguageVariant.JSX));
        IReferenceResolver referenceResolver = default;
        if ((((importElisionEnabled || jsxTransformEnabled) || !options.GetIsolatedModules()) || (options.EmitDecoratorMetadata == Tristate.True)))
        {
            referenceResolver = emitResolver;
        }
        else
        {
            referenceResolver = new ReferenceResolver(options, new ReferenceResolverHooks());
        }

        var opts = new TransformOptions
        {
            Context = emitContext,
            CompilerOptions = options,
            Resolver = referenceResolver,
            EmitResolver = emitResolver,
            GetEmitModuleFormatOfFile = host.GetEmitModuleFormatOfFile
        };
        {
            if ((options.EmitDecoratorMetadata == Tristate.True))
            {
                tx = tx.Append(MetadataTransformer.NewMetadataTransformer(opts));
            }

            tx = tx.Append(TypeEraserTransformer.NewTypeEraserTransformer(opts));
            if (importElisionEnabled)
            {
                tx = tx.Append(ImportElisionTransformer.NewImportElisionTransformer(opts));
            }

            tx = tx.Append(RuntimeSyntaxTransformer.NewRuntimeSyntaxTransformer(opts));
            if ((options.ExperimentalDecorators == Tristate.True))
            {
                tx = tx.Append(LegacyDecoratorsTransformer.NewLegacyDecoratorsTransformer(opts));
            }
        }

        if (jsxTransformEnabled)
        {
            tx = tx.Append(JSXTransformer.NewJSXTransformer(opts));
        }

        var downleveler = EsTransforms.GetESTransformer(opts);
        if ((downleveler != null))
        {
            tx = tx.Append(downleveler);
        }

        tx = tx.Append(UseStrictTransformer.NewUseStrictTransformer(opts));
        tx = tx.Append(getModuleTransformer(opts));
        if (!options.GetIsolatedModules())
        {
            tx = tx.Append(ConstEnumInliningTransformer.NewConstEnumInliningTransformer(opts));
        }

        return tx;
    }

    // emitter.go:185.
    private void emitJSFile(IEmitResolver emitResolver, SourceFile sourceFile, RootedFilePath jsFilePath, RootedFilePath sourceMapFilePath)
    {
        var emitContext = emitResolver.EmitContext();
        var options = this.host.Options();
        if ((((sourceFile == null) || ((this.emitOnly != EmitOnly.EmitAll) && (this.emitOnly != EmitOnly.EmitOnlyJs))) || (jsFilePath == default)))
        {
            return;
        }

        if ((!this.forceEmit && (((options.NoEmit == Tristate.True) || this.host.IsEmitBlocked(jsFilePath)))))
        {
            this.emitResult.EmitSkipped = true;
            return;
        }

        Action releaseTrace = tr?.Push(TypeScript.Tracing.Phase.Emit, "emitJsFileOrBundle", new() { ["jsFilePath"] = jsFilePath }, true);
        try
        {
            sourceFile = this.runScriptTransformers(emitResolver, sourceFile);
            var printerOptions = new PrinterOptions
            {
                RemoveComments = (options.RemoveComments == Tristate.True),
                NewLine = options.NewLine,
                NoEmitHelpers = (options.NoEmitHelpers == Tristate.True),
                SourceMap = (options.SourceMap == Tristate.True),
                InlineSourceMap = (options.InlineSourceMap == Tristate.True),
                InlineSources = (options.InlineSources == Tristate.True),
                Target = options.Target
            };
            var outputPrinter = Printer.NewPrinter(printerOptions, new PrintHandlers { }, emitContext);
            this.printSourceFile(emitContext, jsFilePath, sourceMapFilePath, sourceFile, outputPrinter, options, shouldEmitSourceMaps(options, sourceFile));
        }
        finally
        {
            releaseTrace?.Invoke();
        }
    }

    // emitter.go:223.
    private void emitDeclarationFile(IEmitResolver emitResolver, SourceFile sourceFile, RootedFilePath declarationFilePath, RootedFilePath declarationMapPath)
    {
        var emitContext = emitResolver.EmitContext();
        var options = this.host.Options();
        if ((((sourceFile == null) || (this.emitOnly == EmitOnly.EmitOnlyJs)) || (declarationFilePath == default)))
        {
            return;
        }

        var emitDeclarationMap = ((this.emitOnly != EmitOnly.EmitOnlyBuilderSignature) && (options.DeclarationMap == Tristate.True));
        var contentMappedSource = sourceFile;
        Action releaseTrace = tr?.Push(TypeScript.Tracing.Phase.Emit, "emitDeclarationFileOrBundle", new() { ["declarationFilePath"] = declarationFilePath }, true);
        try
        {
            var tuple3 = this.runDeclarationTransformers(emitResolver, sourceFile, declarationFilePath, declarationMapPath);
            sourceFile = tuple3.Item1;
            var diags = tuple3.Item2;
            {
                foreach (var elem in (diags))
                {
                    this.emitterDiagnostics.Add(elem);
                }
            }

            if (((!this.forceEmit && (this.emitOnly != EmitOnly.EmitOnlyBuilderSignature)) && (((options.NoEmit == Tristate.True) || this.host.IsEmitBlocked(declarationFilePath)))))
            {
                this.emitResult.EmitSkipped = true;
                return;
            }

            var declBlocked = (((diags.Count > 0) && !this.forceEmit) && (this.emitOnly != EmitOnly.EmitOnlyBuilderSignature));
            if (declBlocked)
            {
                this.emitResult.EmitSkipped = true;
                return;
            }

            var printerOptions = new PrinterOptions
            {
                RemoveComments = (options.RemoveComments == Tristate.True),
                NewLine = options.NewLine,
                NoEmitHelpers = true,
                Target = options.GetEmitScriptTarget(),
                SourceMap = emitDeclarationMap,
                InlineSourceMap = (options.InlineSourceMap == Tristate.True),
                OnlyPrintJSDocStyle = true,
                OmitBraceSourceMapPositions = true
            };
            var printHandlers = new PrintHandlers
            {
            };
            {
                var spanMap = contentMappedSource.SpanMap();
                if ((emitDeclarationMap && (spanMap != null)))
                {
                    var originalSource = new DeclarationMapSource(contentMappedSource);
                    printHandlers.MapSourcePosition = (source, pos) =>
                    {
                        if ((source.FileName() != RootedFilePath.FromValue(contentMappedSource.FileNameGoValue())))
                        {
                            return (source, pos, true);
                        }

                        var tuple5 = spanMap.VirtualToOriginalPositionExact(pos);
                        var mapped = tuple5.Item1;
                        var ok = tuple5.Item2;
                        if (!ok)
                        {
                            return (null, 0, false);
                        }

                        return (originalSource, (int)(mapped), true);
                    };
                }
            }

            var outputPrinter = Printer.NewPrinter(printerOptions, printHandlers, emitContext);
            var declarationMapOptions = new CompilerOptions
            {
                SourceMap = Core.IfElse(emitDeclarationMap, Tristate.True, Tristate.False),
                SourceRoot = options.SourceRoot,
                MapRoot = options.MapRoot
            };
            this.printSourceFile(emitContext, declarationFilePath, declarationMapPath, sourceFile, outputPrinter, declarationMapOptions, shouldEmitSourceMaps(declarationMapOptions, sourceFile));
        }
        finally
        {
            releaseTrace?.Invoke();
        }
    }

    // emitter.go:315.
    private void printSourceFile(EmitContext emitContext, RootedFilePath jsFilePath, RootedFilePath sourceMapFilePath, SourceFile sourceFile, Printer printer_, CompilerOptions mapOptions, bool shouldEmitSourceMaps)
    {
        var options = this.host.Options();
        Generator sourceMapGenerator = default;
        if (shouldEmitSourceMaps)
        {
            sourceMapGenerator = Generator.NewGenerator(jsFilePath.BaseNameValue(), getSourceRoot(mapOptions), this.getSourceMapDirectory(mapOptions, jsFilePath, sourceFile), this.host.CaseSensitivity());
        }

        printer_.Write(sourceFile.AsNode(), sourceFile, this.writer, sourceMapGenerator);
        emitContext.Factory.ReleaseArenas();
        var sourceMapUrlPos = -1;
        if ((sourceMapGenerator != null))
        {
            if (((mapOptions.SourceMap == Tristate.True) || (mapOptions.InlineSourceMap == Tristate.True)))
            {
                this.emitResult.SourceMaps = this.emitResult.SourceMaps.Append(new SourceMapEmitResult { InputSourceFileNames = sourceMapGenerator.Sources(), SourceMap = sourceMapGenerator.RawSourceMap(), GeneratedFile = jsFilePath.AsString() });
            }

            var sourceMappingURL = this.getSourceMappingURL(mapOptions, sourceMapGenerator, jsFilePath, sourceMapFilePath, sourceFile);
            if ((sourceMappingURL.Length > 0))
            {
                if (!this.writer.IsAtStartOfLine())
                {
                    this.writer.RawWrite(Core.IfElse((options.NewLine == NewLineKind.CRLF), "\r\n", "\n"));
                }

                sourceMapUrlPos = this.writer.GetTextPos();
                this.writer.WriteComment("//# sourceMappingURL=");
                this.writer.WriteComment(sourceMappingURL);
            }

            if ((sourceMapFilePath != default))
            {
                var sourceMap = sourceMapGenerator.Value();
                var err = this.writeText(sourceMapFilePath, sourceMap, new WriteFileData { SourceFile = this.sourceFile });
                if ((err != null))
                {
                    this.emitterDiagnostics.Add(Ast.NewCompilerDiagnostic(Diagnostics.Could_not_write_file_0_Colon_1, jsFilePath, GoString.ToUtf8(err.Message)));
                }
                else
                {
                    this.emitResult.EmittedFiles = this.emitResult.EmittedFiles.Append(sourceMapFilePath);
                }
            }
        }
        else
        {
            this.writer.WriteLine();
        }

        var text = this.writer.TextValue();
        if ((options.EmitBOM == Tristate.True))
        {
            text = StringUtil.AddUTF8ByteOrderMark(text);
        }

        var data = new WriteFileData
        {
            SourceMapUrlPos = sourceMapUrlPos,
            Diagnostics = this.emitterDiagnostics.GetDiagnostics(),
            SourceFile = this.sourceFile
        };
        var err2 = this.writeText(jsFilePath, text, data);
        var skippedDtsWrite = data.SkippedDtsWrite;
        if ((err2 != null))
        {
            this.emitterDiagnostics.Add(Ast.NewCompilerDiagnostic(Diagnostics.Could_not_write_file_0_Colon_1, jsFilePath, GoString.ToUtf8(err2.Message)));
        }
        else
        {
            if (!skippedDtsWrite)
            {
                this.emitResult.EmittedFiles = this.emitResult.EmittedFiles.Append(jsFilePath);
            }
        }

        this.writer.Clear();
    }

    // emitter.go:394.
    private Exception writeText(RootedFilePath fileName, ByteStringValue text, WriteFileData data)
    {
        if ((this.writeFile != null))
        {
            return this.writeFile(fileName, text.Materialize(), data);
        }

        return this.host.WriteOutputText(fileName, text);
    }

    // emitter.go:401.
    private static bool shouldEmitSourceMaps(CompilerOptions mapOptions, SourceFile sourceFile)
    {
        return ((((mapOptions.SourceMap == Tristate.True) || (mapOptions.InlineSourceMap == Tristate.True))) && !RootedFilePath.FromValue(sourceFile.FileNameGoValue()).ExtensionIs(TsPath.ExtensionJson));
    }

    // emitter.go:406.
    private static ByteStringValue getSourceRoot(CompilerOptions mapOptions)
    {
        var sourceRoot = mapOptions.SourceRoot.AsValue();
        if ((sourceRoot.Length > 0))
        {
            sourceRoot = TsPath.EnsureTrailingDirectorySeparator(sourceRoot);
        }

        return sourceRoot;
    }

    // emitter.go:416.
    private RootedDirectoryPath getMapRootDirectory(SourceMapLocation mapRoot, SourceFile sourceFile)
    {
        var sourceMapDirectory = mapRoot.ResolveDirectory(this.host.CommonSourceDirectory(), this.host.BaseDirectory());
        if ((sourceFile != null))
        {
            sourceMapDirectory = TypeScript.OutputPaths.OutputPaths.GetSourceFileNameInNewDir(RootedFilePath.FromValue(sourceFile.FileNameGoValue()), sourceMapDirectory, this.host.CommonSourceDirectory(), this.host.CaseSensitivity()).Directory();
        }

        return sourceMapDirectory;
    }

    // emitter.go:432.
    private RootedDirectoryPath getSourceMapDirectory(CompilerOptions mapOptions, RootedFilePath filePath, SourceFile sourceFile)
    {
        if ((mapOptions.SourceRoot.AsValue().Length > 0))
        {
            return this.host.CommonSourceDirectory();
        }

        if ((mapOptions.MapRoot.AsValue().Length > 0))
        {
            return this.getMapRootDirectory(mapOptions.MapRoot, sourceFile);
        }

        return filePath.Directory();
    }

    // emitter.go:442.
    private ByteStringValue getSourceMappingURL(CompilerOptions mapOptions, Generator sourceMapGenerator, RootedFilePath filePath, RootedFilePath sourceMapFilePath, SourceFile sourceFile)
    {
        if ((mapOptions.InlineSourceMap == Tristate.True))
        {
            return ByteStringValue.FromString(sourceMapGenerator.Base64DataURL());
        }

        var sourceMapFile = sourceMapFilePath.BaseNameValue();
        if ((mapOptions.MapRoot.AsValue().Length > 0))
        {
            var sourceMapDirectory = this.getMapRootDirectory(mapOptions.MapRoot, sourceFile);
            var sourceMapFilePath2 = sourceMapDirectory.ResolveFile(sourceMapFile);
            if (mapOptions.MapRoot.IsRelative())
            {
                return StringUtil.EncodeURI(TsPath.GetRelativePathToDirectoryOrUrl(filePath.Directory().AsValue(), sourceMapFilePath2.AsValue(), true, this.host.CaseSensitivity()));
            }

            return StringUtil.EncodeURI(sourceMapFilePath2.AsValue());
        }

        return StringUtil.EncodeURI(sourceMapFile);
    }

    // emitter.go:538.
    private static Slice<SourceFile> getSourceFilesToEmit(ISourceFileMayBeEmittedHost host, Slice<SourceFile> target, bool forceDtsEmit, bool forceJsEmit) => Core.Filter(target.IsNil ? host.SourceFiles() : target, (Host: host, Dts: forceDtsEmit, Js: forceJsEmit), static (file, state) => Program.sourceFileMayBeEmitted(file, state.Host, state.Dts, state.Js));
    // emitter.go:547.
    private static bool isSourceFileNotJson(SourceFile file) => !Ast.IsJsonSourceFile(file);
    // emitter.go:551.
    internal static Slice<Diagnostic> getDeclarationDiagnostics(ICompilerEmitHost host, SourceFile file)
    {
        var fullFiles = Core.Filter(getSourceFilesToEmit(host, Slice<SourceFile>.FromArray([file]), false, false), isSourceFileNotJson);
        bool contains = false;
        foreach (var candidate in fullFiles)
            if (candidate == file)
            {
                contains = true;
                break;
            }

        if (!contains)
            return Slice<Diagnostic>.Make(0);
        var resolver = host.NewEmitResolver(new EmitContext());
        var transform = DeclarationTransformer.NewDeclarationTransformer(host, resolver, host.Options(), default);
        transform.TransformSourceFile(file);
        return transform.GetDiagnostics();
    }
}

// emitter.go:296–313. Map original content-mapper coordinates, retaining its immutable source owner.
internal sealed class DeclarationMapSource : Source
{
    private readonly RootedFilePath fileName;
    private readonly ByteStringView text;
    private readonly Slice<int> lineMap;
    internal DeclarationMapSource(SourceFile sourceFile)
    {
        fileName = sourceFile.OriginalFileName();
        text = sourceFile.ContentMapper() == "" ? sourceFile.TextView() : ByteStringView.FromGoBytes(sourceFile.OriginalText());
        lineMap = Core.ComputeECMALineStarts(text);
    }

    public RootedFilePath FileName() => fileName;
    public ByteStringView Text() => text;
    public Slice<int> ECMALineMap() => lineMap;
}
