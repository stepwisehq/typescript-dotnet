// Authored ordinary CLI observations through genuine pinned configuration and
// Program APIs. Checking uses unchanged parsed options and authentic bundled
// library selection; no host-injected library roots or checker admission.
package main

import (
	"context"
	"encoding/json"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/bundled"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
	"github.com/microsoft/TypeScript/tsc/internal/scanner"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

func cliDiagnostics(input []*ast.Diagnostic) []any {
	if input == nil {
		return nil
	}
	result := []any{}
	for _, d := range input {
		var file any
		line, column := 0, 0
		if d.File() != nil {
			file = d.File().FileName()
			if d.Pos() >= 0 {
				l, c := scanner.GetECMALineAndUTF16CharacterOfPosition(d.File(), d.Pos())
				line = l + 1
				column = int(c) + 1
			}
		}
		category := []string{"Warning", "Error", "Suggestion", "Message"}[int(d.Category())]
		result = append(result, map[string]any{"File": file, "Code": d.Code(), "Category": category, "Message": d.String(), "ByteStart": d.Pos(), "ByteEnd": d.End(), "Line": line, "Column": column, "Chain": cliDiagnostics(d.MessageChain()), "Related": cliDiagnostics(d.RelatedInformation())})
	}
	return result
}
func runCLI(r Request) any {
	cs := tspath.CaseSensitive
	if r.CaseSensitivity != nil {
		cs = tspath.CaseSensitivity(*r.CaseSensitivity)
	}
	path := tspath.RootedFilePath(input(r.ConfigPath, r.ConfigPathBase64))
	files := append([]FileInput(nil), r.Files...)
	fs := filesystem(files, cs)
	if r.CliAction == "check" {
		fs = bundled.WrapFS(fs)
	}
	parsed, readErrors := tsoptions.GetParsedCommandLineOfConfigFile(path, nil, nil, fs, nil)
	diagnostics := readErrors
	if parsed != nil {
		diagnostics = parsed.GetConfigFileParsingDiagnostics()
	}
	output := map[string]any{"ExitCode": 0, "Error": nil, "Diagnostics": []any{}}
	if r.CliAction == "inspect" {
		var config any
		if parsed != nil {
			encoded, err := tsjson.Marshal(tsoptions.ConvertToTSConfig(parsed, path))
			if err != nil {
				panic(err)
			}
			if err := json.Unmarshal(encoded, &config); err != nil {
				panic(err)
			}
			if parsed.FileNames() != nil {
				names := []string{}
				encodedNames := []string{}
				for _, name := range parsed.FileNames() {
					names = append(names, name.AsString())
					encodedNames = append(encodedNames, b64(name))
				}
				output["Files"] = names
				output["FileNamesBase64"] = encodedNames
			}
		}
		output["Configuration"] = config
	} else if r.CliAction == "check" {
		if len(diagnostics) == 0 && parsed != nil {
			p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue}, ProgramHosts: compiler.ProgramHosts{Host: compiler.NewCompilerHost(fs, bundled.LibPath(), nil, nil, nil)}})
			ctx := context.Background()
			diagnostics = append(diagnostics, p.GetProgramDiagnostics()...)
			diagnostics = append(diagnostics, p.GetSyntacticDiagnostics(ctx, nil)...)
			diagnostics = append(diagnostics, p.GetBindDiagnostics(ctx, nil)...)
			diagnostics = append(diagnostics, p.GetSemanticDiagnostics(ctx, nil)...)
			diagnostics = append(diagnostics, p.GetGlobalDiagnostics(ctx)...)
			diagnostics = compiler.SortAndDeduplicateDiagnostics(diagnostics)
		}
	} else {
		panic("Unknown original CLI observation action")
	}
	if len(diagnostics) != 0 {
		output["ExitCode"] = 1
		output["Diagnostics"] = cliDiagnostics(diagnostics)
	}
	return map[string]any{"name": r.Name, "result": output}
}
