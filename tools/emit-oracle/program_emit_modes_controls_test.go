// Authored public ordinary Program.Emit controls, not incremental-owner assertions.
package compiler_test

import (
	"context"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/bundled"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
	"testing"
)

type emitModeWrite struct {
	Name, Text, SourceFile                        string
	SourceMapUrlPos                               int
	BuildInfoNil, DiagnosticsNil, SkippedDtsWrite bool
}
type emitModeObservation struct {
	Name          string
	Writes        []emitModeWrite
	EmitSkipped   bool
	Diagnostics   []tsDiagnosticValue
	EmittedFiles  []tspath.RootedFilePath
	SourceMapsNil bool
}

func TestAuthoredOrdinaryProgramEmitModes(t *testing.T) {
	for _, name := range []string{"TargetSourceFile", "ForceJsDespiteNoEmit", "BuilderSignatureDespiteNoEmit", "ForceDtsSkippedWrite"} {
		t.Run(name, func(t *testing.T) {
			options := &core.CompilerOptions{Target: core.ScriptTargetESNext, Module: core.ModuleKindESNext, SkipLibCheck: core.TSTrue, ListEmittedFiles: core.TSTrue, NewLine: core.NewLineKindLF}
			if name != "TargetSourceFile" {
				options.NoEmit = core.TSTrue
			}
			fs := bundled.WrapFS(vfstest.FromMap(map[string]string{"/fixture/main.ts": "export const café: number = 1;\n", "/fixture/other.ts": "export const other: string = 'value';\n"}, tspath.CaseSensitive))
			p := compiler.NewProgram(compiler.ProgramOptions{Config: tsoptions.NewParsedCommandLine(options, []tspath.RootedFilePath{"/fixture/main.ts", "/fixture/other.ts"}, nil, "/fixture", fs.CaseSensitivity()), Host: compiler.NewCompilerHost(fs, bundled.LibPath(), nil, nil, nil), SingleThreaded: core.TSTrue})
			file := p.GetSourceFile("/fixture/main.ts")
			observation := emitModeObservation{Name: name, Writes: []emitModeWrite{}}
			emitOptions := compiler.EmitOptions{WriteFile: func(path tspath.RootedFilePath, text string, data *compiler.WriteFileData) error {
				if name == "ForceDtsSkippedWrite" {
					data.SkippedDtsWrite = true
				}
				source := ""
				if data.SourceFile != nil {
					source = string(data.SourceFile.FileName())
				}
				observation.Writes = append(observation.Writes, emitModeWrite{string(path), text, source, data.SourceMapUrlPos, data.BuildInfo == nil, data.Diagnostics == nil, data.SkippedDtsWrite})
				return nil
			}}
			emitOptions.TargetSourceFiles = []*ast.SourceFile{file}
			if name == "ForceJsDespiteNoEmit" {
				emitOptions.EmitOnly = compiler.EmitOnlyJs
				emitOptions.ForceEmit = true
			}
			if name == "ForceDtsSkippedWrite" {
				emitOptions.EmitOnly = compiler.EmitOnlyDts
				emitOptions.ForceEmit = true
			}
			if name == "BuilderSignatureDespiteNoEmit" {
				emitOptions.EmitOnly = compiler.EmitOnlyBuilderSignature
			}
			if name != "TargetSourceFile" {
				initial := p.Emit(context.Background(), compiler.EmitOptions{})
				if initial == nil || initial.EmitSkipped {
					t.Fatal("NoEmit nil-target default returns empty non-skipped result")
				}
			}
			if name != "TargetSourceFile" {
				selected := p.Emit(context.Background(), compiler.EmitOptions{TargetSourceFiles: []*ast.SourceFile{file}})
				if selected == nil || !selected.EmitSkipped {
					t.Fatal("NoEmit selected target must skip")
				}
			}
			result := p.Emit(context.Background(), emitOptions)
			if result == nil {
				t.Fatal("noncancelled emit result missing")
			}
			observation.EmitSkipped = result.EmitSkipped
			observation.Diagnostics = tsProjectDiagnostics(result.Diagnostics)
			observation.EmittedFiles = result.EmittedFiles
			observation.SourceMapsNil = result.SourceMaps == nil
			if result.EmitSkipped || len(result.Diagnostics) != 0 || len(observation.Writes) != 1 {
				t.Fatal("unexpected public emit mode result")
			}
			raw, err := json.Marshal(observation)
			if err != nil {
				t.Fatal(err)
			}
			fmt.Printf("EMIT_MODE_OUTPUT=%s\n", raw)
		})
	}
}
