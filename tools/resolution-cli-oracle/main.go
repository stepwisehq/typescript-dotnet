// Authored ordinary CLI diagnostic observer over the pinned genuine Program.
// No options are overridden. The three exact checking libraries are explicit roots.
package main

import (
	"bufio"
	"context"
	"crypto/sha256"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
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
	File                *string
	Code                int32
	Category, Message   string
	ByteStart, ByteEnd  int
	Line, Column        int
	Chain, Related      []*diagnostic
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
	libraries := []tspath.RootedFilePath{}
	for _, lib := range []struct{ name, hash string }{
		{"lib.es5.d.ts", "45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215"},
		{"lib.decorators.d.ts", "1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4"},
		{"lib.decorators.legacy.d.ts", "b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da"},
	} {
		data, err := os.ReadFile(filepath.Join("upstream/TypeScript/tsc/internal/bundled/libs", lib.name))
		if err != nil || fmt.Sprintf("%x", sha256.Sum256(data)) != lib.hash {
			panic("Pinned checking library changed: " + lib.name)
		}
		path := tspath.RootedFilePath("/lib/" + lib.name)
		if _, exists := files[path.AsString()]; exists {
			panic("Fixture collides with a checking library")
		}
		files[path.AsString()] = string(data)
		libraries = append(libraries, path)
	}
	fs := vfstest.FromMap(files, tspath.CaseSensitive)
	parsed, readErrors := tsoptions.GetParsedCommandLineOfConfigFile(tspath.RootedFilePath(input.ConfigPath), nil, nil, fs, nil)
	if parsed == nil {
		panic(readErrors)
	}
	// Match the named checking environment, not a fabricated fixed option set.
	parsed = parsed.WithFileNames(append(libraries, parsed.FileNames()...))
	p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue}, ProgramHosts: compiler.ProgramHosts{Host: compiler.NewCompilerHost(fs, "/lib", nil, nil, nil)}})
	ctx := context.Background()
	all := append([]*ast.Diagnostic{}, readErrors...)
	all = append(all, p.GetConfigFileParsingDiagnostics()...)
	all = append(all, p.GetProgramDiagnostics()...)
	all = append(all, p.GetSyntacticDiagnostics(ctx, nil)...)
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
