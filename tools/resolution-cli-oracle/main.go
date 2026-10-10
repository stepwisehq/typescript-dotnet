// Authored ordinary CLI diagnostic observer over the pinned genuine Program.
// Parsed options and roots are unchanged; authentic bundled libraries are selected by Program.
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
	parsed, readErrors := tsoptions.GetParsedCommandLineOfConfigFile(tspath.RootedFilePath(input.ConfigPath), nil, nil, fs, nil)
	if parsed == nil {
		panic(readErrors)
	}
	p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue}, ProgramHosts: compiler.ProgramHosts{Host: compiler.NewCompilerHost(fs, bundled.LibPath(), nil, nil, nil)}})
	ctx := context.Background()
	all := append([]*ast.Diagnostic{}, readErrors...)
	all = append(all, p.GetConfigFileParsingDiagnostics()...)
	all = append(all, p.GetProgramDiagnostics()...)
	all = append(all, p.GetSyntacticDiagnostics(ctx, nil)...)
	all = append(all, p.GetBindDiagnostics(ctx, nil)...)
	all = append(all, p.GetSemanticDiagnostics(ctx, nil)...)
	all = append(all, p.GetGlobalDiagnostics(ctx)...)
	all = compiler.SortAndDeduplicateDiagnostics(all)
	status := 0
	if len(all) != 0 {
		status = 1
	}
	return map[string]any{"Name": input.Name, "Result": map[string]any{"ExitCode": status, "Error": nil, "Diagnostics": describe(all)}}
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
