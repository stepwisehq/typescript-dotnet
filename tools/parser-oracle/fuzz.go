package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"

	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

func extractFuzz(directory string) []Request {
	paths, err := filepath.Glob(filepath.Join(directory, "*"))
	if err != nil {
		panic(err)
	}
	result := []Request{}
	for _, path := range paths {
		content, err := os.ReadFile(path)
		if err != nil {
			panic(err)
		}
		lines := strings.Split(strings.TrimSpace(string(content)), "\n")
		if len(lines) != 5 || lines[0] != "go test fuzz v1" {
			panic("Unknown fuzz corpus format: " + path)
		}
		values := []string{}
		for _, line := range lines[1:3] {
			if !strings.HasPrefix(line, "string(") || !strings.HasSuffix(line, ")") {
				panic("Expected Go string: " + line)
			}
			s, err := strconv.Unquote(line[7 : len(line)-1])
			if err != nil {
				panic(err)
			}
			values = append(values, s)
		}
		options := []bool{}
		for _, line := range lines[3:] {
			if !strings.HasPrefix(line, "bool(") || !strings.HasSuffix(line, ")") {
				panic("Expected Go bool: " + line)
			}
			v, err := strconv.ParseBool(line[5 : len(line)-1])
			if err != nil {
				panic(err)
			}
			options = append(options, v)
		}
		name := "/index" + values[0]
		encoded := bytes(values[1])
		kind := core.GetScriptKindFromFileName(tspath.RootedFilePathFromNormalized(name))
		if kind == core.ScriptKindUnknown {
			panic(fmt.Sprintf("Unsupported fuzz extension %q", values[0]))
		}
		result = append(result, Request{Name: "upstream-fuzz-" + filepath.Base(path), TextBytesBase64: &encoded, ScriptKind: kind, FileName: name, ExternalModuleJSX: options[0], ExternalModuleForce: options[1]})
	}
	return result
}
