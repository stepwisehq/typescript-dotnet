// Authored full script-chain outputs and complete public diagnostics.
// Original compiler-corpus/module/Program emission acceptance is separate.
package compiler_test

import (
	"context"
	"encoding/json"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/binder"
	"github.com/microsoft/TypeScript/tsc/internal/bundled"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/printer"
	"github.com/microsoft/TypeScript/tsc/internal/transformers"
	"github.com/microsoft/TypeScript/tsc/internal/transformers/estransforms"
	"github.com/microsoft/TypeScript/tsc/internal/transformers/inliners"
	"github.com/microsoft/TypeScript/tsc/internal/transformers/jsxtransforms"
	"github.com/microsoft/TypeScript/tsc/internal/transformers/moduletransforms"
	"github.com/microsoft/TypeScript/tsc/internal/transformers/tstransforms"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
	"os"
	"strings"
	"testing"
)

type scriptObservedCase struct {
	Name, Input, Other, Target, Module, FileName, Jsx, JsxFactory, JsxFragmentFactory, JsxImportSource, ModuleResolution                                                                                         string
	Files                                                                                                                                                                                                        map[string]string
	AllowJs, CheckJs, ResolveJsonModule, RewriteRelativeImportExtensions, ImportHelpers, NoEmitHelpers, EsModuleInterop, VerbatimModuleSyntax, IsolatedModules, AlwaysStrict, PreserveConstEnums, RemoveComments *bool
	JSX                                                                                                                                                                                                          bool
	UseDefineForClassFields, ExperimentalDecorators, EmitDecoratorMetadata                                                                                                                                       *bool
	StrictNullChecks                                                                                                                                                                                             *bool
	Output                                                                                                                                                                                                       *string
	FullDiagnostics                                                                                                                                                                                              *tsDiagnosticChannels
}

func esTri(v *bool) core.Tristate {
	if v == nil {
		return core.TSUnknown
	}
	if *v {
		return core.TSTrue
	}
	return core.TSFalse
}
func esTarget(name string) core.ScriptTarget {
	switch name {
	case "ES2015":
		return core.ScriptTargetES2015
	case "ES2016":
		return core.ScriptTargetES2016
	case "ES2017":
		return core.ScriptTargetES2017
	case "ES2018":
		return core.ScriptTargetES2018
	case "ES2019":
		return core.ScriptTargetES2019
	case "ES2020":
		return core.ScriptTargetES2020
	case "ES2021":
		return core.ScriptTargetES2021
	case "ES2022":
		return core.ScriptTargetES2022
	case "ESNext":
		return core.ScriptTargetESNext
	default:
		panic("unknown target: " + name)
	}
}
func TestAuthoredScriptFullChains(t *testing.T) {
	capture := os.Getenv("EMIT_SCRIPT_CAPTURE") != ""
	for _, path := range strings.Split(os.Getenv("EMIT_SCRIPT_CASES"), ",") {
		raw, err := os.ReadFile(path)
		if err != nil {
			t.Fatal(err)
		}
		var cases []scriptObservedCase
		if err = json.Unmarshal(raw, &cases); err != nil {
			t.Fatal(err)
		}
		for i := range cases {
			tc := &cases[i]
			t.Run(tc.Name, func(t *testing.T) {
				module := core.ModuleKindESNext
				switch tc.Module {
				case "", "ESNext":
				case "None":
					module = core.ModuleKindNone
				case "CommonJS":
					module = core.ModuleKindCommonJS
				case "Preserve":
					module = core.ModuleKindPreserve
				case "ES2015":
					module = core.ModuleKindES2015
				case "ES2020":
					module = core.ModuleKindES2020
				case "ES2022":
					module = core.ModuleKindES2022
				case "Node16":
					module = core.ModuleKindNode16
				case "Node18":
					module = core.ModuleKindNode18
				case "Node20":
					module = core.ModuleKindNode20
				case "NodeNext":
					module = core.ModuleKindNodeNext
				default:
					t.Fatalf("unknown module: %s", tc.Module)
				}
				strict := core.TSTrue
				if tc.StrictNullChecks != nil {
					strict = esTri(tc.StrictNullChecks)
				}
				options := &core.CompilerOptions{Target: esTarget(tc.Target), Module: module, SkipLibCheck: core.TSTrue, StrictNullChecks: strict, UseDefineForClassFields: esTri(tc.UseDefineForClassFields), ExperimentalDecorators: esTri(tc.ExperimentalDecorators), EmitDecoratorMetadata: esTri(tc.EmitDecoratorMetadata)}
				options.AllowJs = esTri(tc.AllowJs)
				options.CheckJs = esTri(tc.CheckJs)
				options.ResolveJsonModule = esTri(tc.ResolveJsonModule)
				options.RewriteRelativeImportExtensions = esTri(tc.RewriteRelativeImportExtensions)
				options.ImportHelpers = esTri(tc.ImportHelpers)
				options.NoEmitHelpers = esTri(tc.NoEmitHelpers)
				options.ESModuleInterop = esTri(tc.EsModuleInterop)
				options.VerbatimModuleSyntax = esTri(tc.VerbatimModuleSyntax)
				options.IsolatedModules = esTri(tc.IsolatedModules)
				options.AlwaysStrict = esTri(tc.AlwaysStrict)
				options.PreserveConstEnums = esTri(tc.PreserveConstEnums)
				options.RemoveComments = esTri(tc.RemoveComments)
				options.JsxFactory = tc.JsxFactory
				options.JsxFragmentFactory = tc.JsxFragmentFactory
				options.JsxImportSource = tc.JsxImportSource
				switch tc.Jsx {
				case "", "None":
				case "Preserve":
					options.Jsx = core.JsxEmitPreserve
				case "React":
					options.Jsx = core.JsxEmitReact
				case "ReactNative":
					options.Jsx = core.JsxEmitReactNative
				case "ReactJSX":
					options.Jsx = core.JsxEmitReactJSX
				case "ReactJSXDev":
					options.Jsx = core.JsxEmitReactJSXDev
				default:
					t.Fatalf("unknown JSX: %s", tc.Jsx)
				}
				switch tc.ModuleResolution {
				case "", "Unknown":
				case "Classic":
					options.ModuleResolution = core.ModuleResolutionKindClassic
				case "Node10":
					options.ModuleResolution = core.ModuleResolutionKindNode10
				case "Node16":
					options.ModuleResolution = core.ModuleResolutionKindNode16
				case "NodeNext":
					options.ModuleResolution = core.ModuleResolutionKindNodeNext
				case "Bundler":
					options.ModuleResolution = core.ModuleResolutionKindBundler
				default:
					t.Fatalf("unknown resolution: %s", tc.ModuleResolution)
				}
				fileName := tc.FileName
				if fileName == "" {
					fileName = "main.ts"
					if tc.JSX {
						fileName = "main.tsx"
					}
				}
				name := tspath.RootedFilePath("/fixture/" + fileName)
				files := map[string]string{name.AsString(): tc.Input}
				if tc.Other != "" {
					files["/fixture/node_modules/other/index.ts"] = tc.Other
				}
				for key, value := range tc.Files {
					files["/fixture/"+key] = value
				}
				fs := bundled.WrapFS(vfstest.FromMap(files, tspath.CaseSensitive))
				p := compiler.NewProgram(compiler.ProgramOptions{Config: tsoptions.NewParsedCommandLine(options, []tspath.RootedFilePath{name}, nil, "/fixture", fs.CaseSensitivity()), Host: compiler.NewCompilerHost(fs, bundled.LibPath(), nil, nil, nil), SingleThreaded: core.TSTrue})
				original := p.GetSourceFile(name)
				c, done := p.GetTypeCheckerForFile(context.Background(), original)
				defer done()
				fullDiagnostics := tsPublicDiagnostics(p)
				// Same explicit fixture-root relocation as the accepted managed public DTO.
				diagnosticJSON, _ := json.Marshal(fullDiagnostics)
				diagnosticJSON = []byte(strings.ReplaceAll(string(diagnosticJSON), "/fixture/", "/"))
				if err := json.Unmarshal(diagnosticJSON, &fullDiagnostics); err != nil {
					t.Fatal(err)
				}
				ec := printer.NewEmitContext()
				r := c.NewEmitResolver(ec)
				opts := &transformers.TransformOptions{Context: ec, CompilerOptions: options, EmitResolver: r, Resolver: r, GetEmitModuleFormatOfFile: p.GetEmitModuleFormatOfFile}
				jsxEnabled := options.GetJSXTransformEnabled() && original.LanguageVariant == core.LanguageVariantJSX
				importElision := !options.VerbatimModuleSyntax.IsTrue() && !ast.IsInJSFile(original.AsNode())
				if !(importElision || jsxEnabled || !options.GetIsolatedModules() || options.EmitDecoratorMetadata.IsTrue()) {
					opts.Resolver = binder.NewReferenceResolver(options, binder.ReferenceResolverHooks{})
				}
				file := original
				if options.EmitDecoratorMetadata.IsTrue() {
					file = tstransforms.NewMetadataTransformer(opts).TransformSourceFile(file)
				}
				file = tstransforms.NewTypeEraserTransformer(opts).TransformSourceFile(file)
				if !options.VerbatimModuleSyntax.IsTrue() && !ast.IsInJSFile(original.AsNode()) {
					file = tstransforms.NewImportElisionTransformer(opts).TransformSourceFile(file)
				}
				file = tstransforms.NewRuntimeSyntaxTransformer(opts).TransformSourceFile(file)
				if options.ExperimentalDecorators.IsTrue() {
					file = tstransforms.NewLegacyDecoratorsTransformer(opts).TransformSourceFile(file)
				}
				if jsxEnabled {
					file = jsxtransforms.NewJSXTransformer(opts).TransformSourceFile(file)
				}
				if tx := estransforms.GetESTransformer(opts); tx != nil {
					file = tx.TransformSourceFile(file)
				}
				file = estransforms.NewUseStrictTransformer(opts).TransformSourceFile(file)
				var moduleTransformer *transformers.Transformer
				switch options.GetEmitModuleKind() {
				case core.ModuleKindPreserve:
					moduleTransformer = moduletransforms.NewESModuleTransformer(opts)
				case core.ModuleKindESNext, core.ModuleKindES2022, core.ModuleKindES2020, core.ModuleKindES2015, core.ModuleKindNode20, core.ModuleKindNode18, core.ModuleKindNode16, core.ModuleKindNodeNext, core.ModuleKindCommonJS:
					moduleTransformer = moduletransforms.NewImpliedModuleTransformer(opts)
				default:
					moduleTransformer = moduletransforms.NewCommonJSModuleTransformer(opts)
				}
				file = moduleTransformer.TransformSourceFile(file)
				if !options.GetIsolatedModules() {
					file = inliners.NewConstEnumInliningTransformer(opts).TransformSourceFile(file)
				}
				output := printer.NewPrinter(printer.PrinterOptions{NewLine: core.NewLineKindLF, Target: options.Target, RemoveComments: options.RemoveComments.IsTrue(), NoEmitHelpers: options.NoEmitHelpers.IsTrue()}, printer.PrintHandlers{}, ec).EmitSourceFile(file)
				if capture {
					tc.Output = &output
					tc.FullDiagnostics = &fullDiagnostics
				} else {
					if tc.Output == nil || *tc.Output != output {
						t.Fatalf("full script output differs\n%s", output)
					}
					a, _ := json.Marshal(tc.FullDiagnostics)
					b, _ := json.Marshal(fullDiagnostics)
					if string(a) != string(b) {
						t.Fatalf("full public diagnostics differ: %s != %s", a, b)
					}
				}
				t.Logf("target=%s outputBytes=%d semanticDiagnostics=%d", tc.Target, len(output), len(fullDiagnostics.Semantic))
			})
		}
		if capture {
			raw, err = json.MarshalIndent(cases, "", "  ")
			if err != nil {
				t.Fatal(err)
			}
			if err = os.WriteFile(path, append(raw, '\n'), 0644); err != nil {
				t.Fatal(err)
			}
		}
	}
}
