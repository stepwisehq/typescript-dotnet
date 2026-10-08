package main

import (
	"encoding/json"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/testrunner"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"os"
	"path/filepath"
	"strings"
)

// Use the upstream test-unit splitter, preserving @filename boundaries and
// removing harness-only option lines exactly as the upstream parser fuzz seeds.
func extractTestFiles(manifestPath string) []Request {
	content, err := os.ReadFile(manifestPath)
	if err != nil {
		panic(err)
	}
	var paths []string
	if err = json.Unmarshal(content, &paths); err != nil {
		panic(err)
	}
	result := []Request{}
	for _, path := range paths {
		source, err := os.ReadFile(path)
		if err != nil {
			panic(err)
		}
		units, _, _, _, err := testrunner.ParseTestFilesAndSymlinks(string(source), path, func(filename, content string, _ map[string]string) (Request, error) {
			filename = "/" + strings.TrimPrefix(filepath.ToSlash(filename), "/")
			kind := core.GetScriptKindFromFileName(tspath.RootedFilePathFromNormalized(filename))
			switch kind {
			case core.ScriptKindJS, core.ScriptKindJSX, core.ScriptKindTS, core.ScriptKindTSX, core.ScriptKindJSON:
			default:
				panic("Unsupported test unit: " + filename)
			}
			encoded := bytes(content)
			return Request{Name: "upstream-" + filepath.ToSlash(path) + "::" + filename, TextBytesBase64: &encoded, ScriptKind: kind, FileName: filename, ResolveJSDoc: kind == core.ScriptKindJS || kind == core.ScriptKindJSX}, nil
		})
		if err != nil {
			panic(err)
		}
		result = append(result, units...)
	}
	return result
}
