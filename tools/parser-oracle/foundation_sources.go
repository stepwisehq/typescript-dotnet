// Authored extraction adapter. The source-unit/directive splitter is original Go.
package main

import (
	"bufio"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/testrunner"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"os"
	"strings"
)

type SourceInput struct {
	Path  string `json:"path"`
	Suite string `json:"suite"`
}
type SourceUnit struct {
	SourcePath           string               `json:"sourcePath"`
	Suite                string               `json:"suite"`
	SourceSHA256         string               `json:"sourceSha256"`
	UnitIndex            int                  `json:"unitIndex"`
	Name                 string               `json:"name"`
	FileName             string               `json:"fileName"`
	Extension            string               `json:"extension"`
	ScriptKind           core.ScriptKind      `json:"scriptKind"`
	OriginalFuzzEligible bool                 `json:"originalFuzzEligible"`
	TextBytesBase64      string               `json:"textBytesBase64"`
	FileOptions          map[string]string    `json:"fileOptions"`
	GlobalOptions        map[string]string    `json:"globalOptions"`
	CurrentDirectory     string               `json:"currentDirectory"`
	Symlinks             map[string]string    `json:"symlinks"`
	Extraction           string               `json:"extraction"`
	Error                string               `json:"error,omitempty"`
	ParseOptionVariants  []ParseOptionVariant `json:"parseOptionVariants"`
}
type ParseOptionVariant struct {
	JSX                 bool     `json:"jsx"`
	Force               bool     `json:"force"`
	Module              string   `json:"module"`
	ModuleDetection     string   `json:"moduleDetection"`
	JsxEmit             string   `json:"jsxEmit"`
	InvalidOptionTokens []string `json:"invalidOptionTokens"`
}

func extractFoundationSource(input SourceInput) (units []SourceUnit) {
	source, err := os.ReadFile("upstream/TypeScript/" + input.Path)
	if err != nil {
		return []SourceUnit{{SourcePath: input.Path, Suite: input.Suite, Extraction: "failed", Error: err.Error()}}
	}
	hash := sha256.Sum256(source)
	sourceHash := hex.EncodeToString(hash[:])
	defer func() {
		if value := recover(); value != nil {
			units = []SourceUnit{{SourcePath: input.Path, Suite: input.Suite, SourceSHA256: sourceHash, Extraction: "failed", Error: fmt.Sprint(value)}}
		}
	}()
	if input.Suite == "fuzz-fixture" {
		name := "/index" + tspath.TryGetExtensionFromPath(input.Path)
		return []SourceUnit{{SourcePath: input.Path, Suite: input.Suite, SourceSHA256: sourceHash, Name: name, FileName: name,
			Extension: tspath.TryGetExtensionFromPath(input.Path), OriginalFuzzEligible: originalFuzzExtension(tspath.TryGetExtensionFromPath(input.Path)), ScriptKind: core.GetScriptKindFromFileName(tspath.RootedFilePathFromNormalized(name)),
			TextBytesBase64: bytes(string(source)), Extraction: "raw-fixture", FileOptions: map[string]string{}, GlobalOptions: map[string]string{}, Symlinks: map[string]string{}}}
	}
	var symlinks map[string]string
	var currentDir string
	var options map[string]string
	units, symlinks, currentDir, options, err = testrunner.ParseTestFilesAndSymlinks(string(source), input.Path, func(filename, content string, fileOptions map[string]string) (SourceUnit, error) {
		// Normalize only the logical caller filename, using original path helpers;
		// contents stay exactly as returned by the original test-unit splitter.
		name := tspath.ToRootedFilePath(filename, "/").AsString()
		return SourceUnit{SourcePath: input.Path, Suite: input.Suite, SourceSHA256: sourceHash, Name: filename, FileName: name,
			Extension: tspath.TryGetExtensionFromPath(filename), ScriptKind: core.GetScriptKindFromFileName(tspath.RootedFilePathFromNormalized(name)),
			TextBytesBase64: bytes(content), FileOptions: fileOptions, Extraction: "original-splitter"}, nil
	})
	if err != nil {
		return []SourceUnit{{SourcePath: input.Path, Suite: input.Suite, SourceSHA256: sourceHash, Extraction: "failed", Error: err.Error()}}
	}
	for i := range units {
		units[i].UnitIndex = i
		units[i].OriginalFuzzEligible = originalFuzzExtension(units[i].Extension)
		units[i].GlobalOptions = options
		units[i].CurrentDirectory = currentDir
		units[i].Symlinks = symlinks
		units[i].ParseOptionVariants = foundationOptionVariants(units[i])
	}
	return units
}
func extractFoundationSources(path string) {
	file, err := os.Open(path)
	if err != nil {
		panic(err)
	}
	defer file.Close()
	scan := bufio.NewScanner(file)
	scan.Buffer(make([]byte, 65536), 32*1024*1024)
	out := json.NewEncoder(os.Stdout)
	for scan.Scan() {
		var input SourceInput
		if err := json.Unmarshal(scan.Bytes(), &input); err != nil {
			panic(err)
		}
		for _, unit := range extractFoundationSource(input) {
			if err := out.Encode(unit); err != nil {
				panic(err)
			}
		}
	}
	if err := scan.Err(); err != nil {
		panic(err)
	}
}

// Case-sensitive membership matches original FuzzParser, not generic mode
// inference. Unrecognized asset extensions are explicitly dispositioned later.
func originalFuzzExtension(extension string) bool {
	for _, group := range tspath.AllSupportedExtensionsWithJson {
		for _, value := range group {
			if extension == value {
				return true
			}
		}
	}
	return false
}
func relevantOptionValues(options map[string]string, key string, fallback string) []string {
	value := options[key]
	if value == "" {
		return []string{fallback}
	}
	values := strings.Split(strings.ToLower(value), ",")
	for i := range values {
		values[i] = strings.TrimSpace(values[i])
	}
	return values
}
func foundationOptionVariants(unit SourceUnit) []ParseOptionVariant {
	modules := map[string]core.ModuleKind{"": core.ModuleKindNone, "none": core.ModuleKindNone, "commonjs": core.ModuleKindCommonJS, "amd": core.ModuleKindAMD,
		"umd": core.ModuleKindUMD, "system": core.ModuleKindSystem, "es6": core.ModuleKindES2015, "es2015": core.ModuleKindES2015,
		"es2020": core.ModuleKindES2020, "es2022": core.ModuleKindES2022, "esnext": core.ModuleKindESNext, "node16": core.ModuleKindNode16,
		"node18": core.ModuleKindNode18, "node20": core.ModuleKindNode20, "nodenext": core.ModuleKindNodeNext, "preserve": core.ModuleKindPreserve}
	detections := map[string]core.ModuleDetectionKind{"": core.ModuleDetectionKindNone, "auto": core.ModuleDetectionKindAuto, "legacy": core.ModuleDetectionKindLegacy, "force": core.ModuleDetectionKindForce}
	jsxModes := map[string]core.JsxEmit{"": core.JsxEmitNone, "preserve": core.JsxEmitPreserve, "react": core.JsxEmitReact, "react-native": core.JsxEmitReactNative, "react-jsx": core.JsxEmitReactJSX, "react-jsxdev": core.JsxEmitReactJSXDev}
	options := unit.GlobalOptions
	result := []ParseOptionVariant{}
	seen := map[ast.ExternalModuleIndicatorOptions]bool{}
	for _, module := range relevantOptionValues(options, "module", "") {
		for _, detection := range relevantOptionValues(options, "moduledetection", "") {
			for _, jsx := range relevantOptionValues(options, "jsx", "") {
				mk, mo := modules[module]
				md, do := detections[detection]
				jx, jo := jsxModes[jsx]
				invalid := []string{}
				if !mo {
					invalid = append(invalid, "module="+module)
				}
				if !do {
					invalid = append(invalid, "moduleDetection="+detection)
				}
				if !jo {
					invalid = append(invalid, "jsx="+jsx)
				}
				// Project/package metadata is outside the foundation host. Record the
				// exact original helper result with zero metadata and explicit options.
				flags := ast.GetExternalModuleIndicatorOptions(tspath.RootedFilePathFromNormalized(unit.FileName), &core.CompilerOptions{Module: mk, ModuleDetection: md, Jsx: jx}, ast.SourceFileMetaData{})
				if !seen[flags] {
					seen[flags] = true
					result = append(result, ParseOptionVariant{flags.JSX, flags.Force, module, detection, jsx, invalid})
				}
			}
		}
	}
	return result
}
