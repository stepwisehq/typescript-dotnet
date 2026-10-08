// Bounded benchmark adapter for the pinned TypeScript Go parser.
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"runtime"
	"strconv"
	"time"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type inputFile struct {
	Path                string          `json:"path"`
	FileName            string          `json:"fileName"`
	ScriptKind          core.ScriptKind `json:"scriptKind"`
	ExternalModuleJSX   bool            `json:"externalModuleJSX"`
	ExternalModuleForce bool            `json:"externalModuleForce"`
}
type source struct {
	opts ast.SourceFileParseOptions
	text string
	kind core.ScriptKind
}
type signature struct {
	checksum                                                                     uint64
	nodes, identifiers, texts, parseDiagnostics, jsDiagnostics, jsdocDiagnostics int64
}
type result struct {
	Implementation          string  `json:"implementation"`
	Mode                    string  `json:"mode"`
	InputBytes              int64   `json:"inputBytes"`
	InputFiles              int     `json:"inputFiles"`
	Iterations              int64   `json:"iterations"`
	NodesPerPass            int64   `json:"nodesPerPass"`
	IdentifiersPerPass      int64   `json:"identifiersPerPass"`
	TextsPerPass            int64   `json:"textsPerPass"`
	ParseDiagnosticsPerPass int64   `json:"parseDiagnosticsPerPass"`
	JSDiagnosticsPerPass    int64   `json:"jsDiagnosticsPerPass"`
	JSDocDiagnosticsPerPass int64   `json:"jsDocDiagnosticsPerPass"`
	Checksum                string  `json:"checksum"`
	ElapsedSeconds          float64 `json:"elapsedSeconds"`
	AllocatedBytes          uint64  `json:"allocatedBytes"`
	GCCollections           uint32  `json:"gcCollections"`
	RuntimeVersion          string  `json:"runtimeVersion"`
}

func mix(checksum *uint64, value int64) { *checksum = (*checksum ^ uint64(value)) * 1099511628211 }
func observeDiagnostics(checksum *uint64, diags []*ast.Diagnostic, channel int64) int64 {
	mix(checksum, channel)
	mix(checksum, int64(len(diags)))
	for _, diag := range diags {
		mix(checksum, int64(diag.Code()))
		mix(checksum, int64(diag.Pos()))
		mix(checksum, int64(diag.End()))
		mix(checksum, int64(diag.Category()))
	}
	return int64(len(diags))
}
func main() {
	if len(os.Args) != 4 {
		panic("Usage: parser-bench WORKLOAD_JSON SAMPLE_MS WARMUP_MS")
	}
	bytes, err := os.ReadFile(os.Args[1])
	if err != nil {
		panic(err)
	}
	var workload struct {
		Files []inputFile `json:"files"`
	}
	if err = json.Unmarshal(bytes, &workload); err != nil {
		panic(err)
	}
	sources := make([]source, 0, len(workload.Files))
	var inputBytes int64
	for _, item := range workload.Files {
		data, err := os.ReadFile(item.Path)
		if err != nil {
			panic(err)
		}
		sources = append(sources, source{ast.SourceFileParseOptions{FileName: tspath.RootedFilePathFromNormalized(item.FileName), PathKey: tspath.PathKeyFromCanonical(item.FileName), ExternalModuleIndicatorOptions: ast.ExternalModuleIndicatorOptions{JSX: item.ExternalModuleJSX, Force: item.ExternalModuleForce}}, string(data), item.ScriptKind})
		inputBytes += int64(len(data))
	}
	if inputBytes == 0 || len(sources) == 0 {
		panic("Empty benchmark workload")
	}
	sampleMs, err := strconv.Atoi(os.Args[2])
	if err != nil {
		panic(err)
	}
	warmupMs, err := strconv.Atoi(os.Args[3])
	if err != nil {
		panic(err)
	}
	if sampleMs <= 0 || warmupMs < 0 {
		panic("Invalid duration")
	}
	pass := func() signature {
		s := signature{checksum: 14695981039346656037}
		for _, source := range sources {
			// Public string API; file bytes were loaded before timing. TS lazy JSDoc is not forced.
			file := parser.ParseSourceFile(source.opts, source.text, source.kind)
			mix(&s.checksum, int64(file.Kind))
			mix(&s.checksum, int64(file.Pos()))
			mix(&s.checksum, int64(file.End()))
			mix(&s.checksum, int64(file.Flags))
			mix(&s.checksum, int64(file.NodeCount))
			mix(&s.checksum, int64(file.IdentifierCount))
			mix(&s.checksum, int64(file.TextCount))
			s.nodes += int64(file.NodeCount)
			s.identifiers += int64(file.IdentifierCount)
			s.texts += int64(file.TextCount)
			s.parseDiagnostics += observeDiagnostics(&s.checksum, file.Diagnostics(), 0)
			s.jsDiagnostics += observeDiagnostics(&s.checksum, file.JSDiagnostics(), 1)
			s.jsdocDiagnostics += observeDiagnostics(&s.checksum, file.JSDocDiagnostics(), 2)
			runtime.KeepAlive(file)
		}
		return s
	}
	expected := pass()
	warmupStart := time.Now()
	for time.Since(warmupStart) < time.Duration(warmupMs)*time.Millisecond {
		if pass() != expected {
			panic("Warmup observer changed")
		}
	}
	runtime.GC()
	var before, after runtime.MemStats
	runtime.ReadMemStats(&before)
	started := time.Now()
	var iterations int64
	var elapsed time.Duration
	for {
		if pass() != expected {
			panic("Measured observer changed")
		}
		iterations++
		elapsed = time.Since(started)
		if elapsed >= time.Duration(sampleMs)*time.Millisecond {
			break
		}
	}
	runtime.ReadMemStats(&after)
	output := result{"go", "parse-source-file-plus-counters-and-diagnostics", inputBytes, len(sources), iterations, expected.nodes, expected.identifiers, expected.texts, expected.parseDiagnostics, expected.jsDiagnostics, expected.jsdocDiagnostics, fmt.Sprintf("%016x", expected.checksum), elapsed.Seconds(), after.TotalAlloc - before.TotalAlloc, after.NumGC - before.NumGC, runtime.Version()}
	if err = json.NewEncoder(os.Stdout).Encode(output); err != nil {
		panic(err)
	}
}
