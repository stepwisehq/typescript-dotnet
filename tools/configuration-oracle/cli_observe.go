// Authored ordinary CLI observations through genuine pinned configuration and
// Program APIs. Fixed complete libraries are a checking-host input, not defaults
// overwritten in the parsed config. No checker admission is applied in Go.
package main

import (
	"context"
	"crypto/sha256"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
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
	if r.CliAction == "check" {
		hashes := map[string]string{"lib.es5.d.ts": "45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215", "lib.decorators.d.ts": "1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4", "lib.decorators.legacy.d.ts": "b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da"}
		for _, name := range []string{"lib.es5.d.ts", "lib.decorators.d.ts", "lib.decorators.legacy.d.ts"} {
			bytes, err := os.ReadFile(filepath.Join("upstream/TypeScript/tsc/internal/bundled/libs", name))
			if err != nil {
				panic(err)
			}
			if fmt.Sprintf("%x", sha256.Sum256(bytes)) != hashes[name] {
				panic("Complete pinned library bytes changed")
			}
			files = append(files, FileInput{Path: "/lib/" + name, Text: string(bytes)})
		}
	}
	fs := filesystem(files, cs)
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
			roots := []tspath.RootedFilePath{"/lib/lib.es5.d.ts", "/lib/lib.decorators.d.ts", "/lib/lib.decorators.legacy.d.ts"}
			roots = append(roots, parsed.FileNames()...)
			p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed.WithFileNames(roots), SingleThreaded: core.TSTrue}, ProgramHosts: compiler.ProgramHosts{Host: compiler.NewCompilerHost(fs, "/lib", nil, nil, nil)}})
			ctx := context.Background()
			diagnostics = append(diagnostics, p.GetProgramDiagnostics()...)
			diagnostics = append(diagnostics, p.GetSyntacticDiagnostics(ctx, nil)...)
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
