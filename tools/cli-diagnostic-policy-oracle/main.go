// Authored finite observer for the ordinary disk CLI aggregation policy.
// Calls pinned compiler.GetDiagnosticsOfAnyProgram with existing-options NoEmit
// (ordinary CLI) or ListFilesOnly (API-only phase control) via original parsing.
package main

import (
	"bufio"
	"context"
	"encoding/json"
	"os"
	"strings"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/bundled"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/scanner"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)

type request struct {
	Name, ConfigPath string
	Files            map[string]string
	NoEmit           bool
	ListFilesOnly    bool
}

type diagnostic struct {
	File               *string
	Code               int32
	Category, Message  string
	ByteStart, ByteEnd int
	Line, Column       int
	Chain, Related     []*diagnostic
}

func describe(rows []*ast.Diagnostic) []*diagnostic {
	if rows == nil {
		return nil
	}
	result := []*diagnostic{}
	for _, d := range rows {
		row := &diagnostic{Code: d.Code(), Category: strings.TrimPrefix(d.Category().String(), "Category"), Message: d.String(), ByteStart: d.Pos(), ByteEnd: d.End(), Chain: describe(d.MessageChain()), Related: describe(d.RelatedInformation())}
		if file := d.File(); file != nil {
			name := file.FileName().AsString()
			row.File = &name
			if d.Pos() >= 0 {
				line, column := scanner.GetECMALineAndUTF16CharacterOfPosition(file, d.Pos())
				row.Line, row.Column = line+1, int(column)+1
			}
		}
		result = append(result, row)
	}
	return result
}

func observe(input request) any {
	files := input.Files
	if _, exists := files[input.ConfigPath]; !exists {
		panic("Configuration must be an actual fixture file")
	}
	fs := bundled.WrapFS(vfstest.FromMap(files, tspath.CaseSensitive))
	var overrides *core.CompilerOptions
	if input.NoEmit || input.ListFilesOnly {
		overrides = &core.CompilerOptions{}
		if input.NoEmit {
			overrides.NoEmit = core.TSTrue
		}
		if input.ListFilesOnly {
			overrides.ListFilesOnly = core.TSTrue
		}
	}
	parsed, readErrors := tsoptions.GetParsedCommandLineOfConfigFile(tspath.RootedFilePath(input.ConfigPath), overrides, nil, fs, nil)
	if parsed == nil {
		panic(readErrors)
	}
	p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue}, ProgramHosts: compiler.ProgramHosts{Host: compiler.NewCompilerHost(fs, bundled.LibPath(), nil, nil, nil)}})
	ctx := context.Background()
	// Project.LoadConfiguration reports invalid config before checker entry.
	all := parsed.GetConfigFileParsingDiagnostics()
	if len(all) == 0 {
		all = compiler.GetDiagnosticsOfAnyProgram(ctx, p, nil, false, p.GetBindDiagnostics, p.GetSemanticDiagnostics)
	}
	all = compiler.SortAndDeduplicateDiagnostics(all)
	status := 0
	if len(all) != 0 {
		status = 1
	}
	return map[string]any{"Name": input.Name, "Result": map[string]any{"ExitCode": status, "Error": nil, "Diagnostics": append([]*diagnostic{}, describe(all)...)}}
}

func main() {
	input := bufio.NewScanner(os.Stdin)
	input.Buffer(make([]byte, 4096), 32*1024*1024)
	output := json.NewEncoder(os.Stdout)
	for input.Scan() {
		var row request
		if err := json.Unmarshal(input.Bytes(), &row); err != nil {
			panic(err)
		}
		if err := output.Encode(observe(row)); err != nil {
			panic(err)
		}
	}
	if err := input.Err(); err != nil {
		panic(err)
	}
}
