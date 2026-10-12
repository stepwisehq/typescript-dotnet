// Authored batch observation. Calls original full compiler preparation and assertions.
// The only execution adaptation is sequential batch channels instead of t.Parallel.
package testrunner

import (
	"bufio"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"slices"
	"testing"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/baseline"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/harnessutil"
)

type authoredEmitRequest struct{ Name, Source, SourceSha256, Configuration string }

func authoredEmitChannel(t *testing.T, name string, run func(*testing.T)) any {
	before := baseline.AuthoredRecordedNames()
	status := "passed-original-assertions"
	t.Run(name, func(t *testing.T) {
		defer func() {
			if failure := recover(); failure != nil {
				t.Errorf("Original channel panic: %v", failure)
			}
			if t.Failed() {
				status = "failed-original-assertions"
			} else if t.Skipped() {
				status = "skipped-original-channel"
			}
		}()
		run(t)
	})
	records := []any{}
	for _, file := range baseline.AuthoredRecordedNames() {
		if !slices.Contains(before, file) {
			records = append(records, baseline.AuthoredObserveRecorded(file))
		}
	}
	return map[string]any{"status": status, "baselines": records}
}

func TestAuthoredWholeEmit(t *testing.T) {
	input, output := os.Getenv("EMIT_REQUESTS"), os.Getenv("EMIT_OUTPUT")
	if input == "" || output == "" {
		t.Fatal("Explicit request/output paths required")
	}
	// Validate original tracking before doing any compiler work.
	baseline.AuthoredRecordedNames()
	in, err := os.Open(input)
	if err != nil {
		t.Fatal(err)
	}
	defer in.Close()
	out, err := os.Create(output)
	if err != nil {
		t.Fatal(err)
	}
	defer out.Close()
	decoder := json.NewDecoder(bufio.NewReader(in))
	decoder.DisallowUnknownFields()
	encoder := json.NewEncoder(out)
	allowed := map[string]*CompilerBaselineRunner{}
	for _, kind := range []CompilerTestType{TestTypeRegression, TestTypeConformance} {
		runner := NewCompilerBaselineRunner(kind)
		// Original RunTests removes previous local artifacts once per suite.
		// This also prevents stale .delete markers from affecting observation.
		runner.cleanUpLocal(t)
		for _, file := range runner.EnumerateTestFiles() {
			allowed[filepath.Clean(file)] = runner
		}
	}
	seen := map[string]bool{}
	for {
		var request authoredEmitRequest
		err := decoder.Decode(&request)
		if err == io.EOF {
			break
		}
		if err != nil {
			t.Fatal(err)
		}
		if request.Name == "" || seen[request.Name] {
			t.Fatal("Empty or duplicate request name")
		}
		seen[request.Name] = true
		runner := allowed[filepath.Clean(request.Source)]
		if runner == nil {
			t.Fatalf("Source outside original inventory: %s", request.Source)
		}
		bytes, err := os.ReadFile(request.Source)
		if err != nil {
			t.Fatal(err)
		}
		digest := sha256.Sum256(bytes)
		if hex.EncodeToString(digest[:]) != request.SourceSha256 {
			t.Fatal("Original source bytes changed")
		}
		t.Run(request.Name, func(t *testing.T) {
			row := map[string]any{"name": request.Name, "sourceSha256": request.SourceSha256, "configuration": request.Configuration, "status": "failed-original-preparation"}
			defer func() {
				if failure := recover(); failure != nil {
					row["errorBase64"] = authoredNoEmitB64(fmt.Sprint(failure))
					t.Errorf("Original emit panic: %v", failure)
				}
				if row["status"] == "failed-original-preparation" && t.Skipped() {
					row["status"] = "skipped-original-preparation"
				}
				if err := encoder.Encode(row); err != nil {
					t.Error(err)
				}
			}()
			if slices.Contains(skippedTests, filepath.Base(request.Source)) {
				row["status"] = "skipped-original-before-configuration"
				return
			}
			original := getCompilerFileBasedTest(t, request.Source)
			configs := original.configurations
			if len(configs) == 0 {
				configs = []*harnessutil.NamedTestConfiguration{{Name: "", Config: nil}}
			}
			var config *harnessutil.NamedTestConfiguration
			for _, candidate := range configs {
				if candidate.Name == request.Configuration {
					config = candidate
					break
				}
			}
			if config == nil {
				t.Fatal("Configuration absent from original expansion")
			}
			payload := makeUnitsFromTest(original.content, original.filename)
			compiled := newCompilerTest(t, request.Name, request.Source, &payload, config)
			observer := &authoredNoEmitObserver{files: map[*ast.SourceFile]int{}, owners: []any{}}
			row["diagnostics"] = observer.diagnostics(compiled.result.Diagnostics)
			row["emitResult"] = map[string]any{"emitSkipped": compiled.result.Result.EmitSkipped, "diagnostics": observer.diagnostics(compiled.result.Result.Diagnostics), "emittedFiles": authoredNoEmitValue(compiled.result.Result.EmittedFiles), "sourceMaps": authoredNoEmitValue(compiled.result.Result.SourceMaps)}
			row["owners"] = observer.owners
			files := func(values func(func(*harnessutil.TestFile) bool)) []any {
				result := []any{}
				values(func(file *harnessutil.TestFile) bool {
					result = append(result, map[string]any{"nameBase64": authoredNoEmitB64(file.UnitName), "contentBase64": authoredNoEmitB64(file.Content)})
					return true
				})
				return result
			}
			row["js"] = files(compiled.result.JS.Values())
			row["dts"] = files(compiled.result.DTS.Values())
			row["maps"] = files(compiled.result.Maps.Values())
			row["status"] = "observed-original-compilation"
			policySkipped := false
			row["optionPolicy"] = authoredEmitChannel(t, "original-option-policy", func(t *testing.T) {
				defer func() { policySkipped = t.Skipped() || t.Failed() }()
				harnessutil.SkipUnsupportedCompilerOptions(t, compiled.options)
			})
			if policySkipped {
				row["status"] = "original-option-policy-disposition"
				return
			}
			channels := map[string]any{}
			for _, channel := range []struct {
				name string
				run  func(*testing.T)
			}{
				{"diagnostics", func(t *testing.T) { compiled.verifyDiagnostics(t, runner.testSuitName) }},
				{"contentMapper", func(t *testing.T) { compiled.verifyContentMapper(t, runner.testSuitName) }},
				{"output", func(t *testing.T) { compiled.verifyJavaScriptOutput(t, runner.testSuitName) }},
				{"sourceMap", func(t *testing.T) { compiled.verifySourceMapOutput(t, runner.testSuitName) }},
				{"sourceMapRecord", func(t *testing.T) { compiled.verifySourceMapRecord(t, runner.testSuitName) }},
				{"typesAndSymbols", func(t *testing.T) { compiled.verifyTypesAndSymbols(t, runner.testSuitName) }},
				{"moduleResolution", func(t *testing.T) { compiled.verifyModuleResolution(t, runner.testSuitName) }},
				{"unionOrdering", compiled.verifyUnionOrdering},
				{"parentPointers", compiled.verifyParentPointers},
			} {
				channels[channel.name] = authoredEmitChannel(t, channel.name, channel.run)
			}
			row["channels"] = channels
			if reason, skipped := skippedEmitTests[compiled.basename]; skipped {
				row["outputHarnessOnlySkipReasonBase64"] = authoredNoEmitB64(reason)
				channels["output"].(map[string]any)["status"] = "skipped-original-channel"
			}
			if !compiled.hasNonDtsFiles {
				row["outputNotApplicable"] = "original-no-non-declaration-input"
			}
			row["typesAndSymbolsDisabled"] = compiled.harnessOptions.NoTypesAndSymbols
			if t.Failed() {
				row["status"] = "observed-with-original-assertion-failures"
			} else {
				row["status"] = "passed-original-full-harness"
			}
		})
	}
}
