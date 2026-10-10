// Authored bounded batch driver over the original configuration/harness/full walker.
// No upstream file is replaced and no compiler option is forced for no-emit observation.
package testrunner

import (
	"bufio"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/harnessutil"
	"os"
	"path/filepath"
	"slices"
	"testing"
)

type authoredNoEmitRequest struct{ Name, Source, SourceSha256, Configuration string }

func authoredNoEmitChannels(o *authoredNoEmitObserver, c harnessutil.AuthoredNoEmitChannels) any {
	return map[string]any{"config": o.diagnostics(c.Config), "program": o.diagnostics(c.Program), "syntactic": o.diagnostics(c.Syntactic), "semantic": o.diagnostics(c.Semantic), "global": o.diagnostics(c.Global), "suggestions": o.diagnostics(c.Suggestions), "sortedAndDeduplicated": o.diagnostics(c.SortedAndDeduplicated)}
}
func authoredOriginalOptionPolicy(options *core.CompilerOptions) any {
	// Exact first decision of failOnUnsupportedCompilerOptions + SkipUnsupportedCompilerOptions.
	// Policy metadata only; these output-harness decisions do not erase genuine checking.
	action, reason := "run", ""
	switch {
	case options.Module == core.ModuleKindAMD:
		action, reason = "fatal", fmt.Sprintf("unsupported module kind %s", options.Module)
	case options.OutFile != "":
		action, reason = "fatal", fmt.Sprintf("unsupported outFile %s", options.OutFile)
	case options.Module == core.ModuleKindUMD || options.Module == core.ModuleKindSystem:
		action, reason = "skip", fmt.Sprintf("unsupported module kind %s", options.Module)
	case options.ModuleResolution == core.ModuleResolutionKindNode10 || options.ModuleResolution == core.ModuleResolutionKindClassic:
		action, reason = "skip", fmt.Sprintf("unsupported module resolution kind %d", options.ModuleResolution)
	case options.ESModuleInterop.IsFalse():
		action, reason = "skip", "esModuleInterop=false is unsupported"
	case options.AllowSyntheticDefaultImports.IsFalse():
		action, reason = "skip", "allowSyntheticDefaultImports=false is unsupported"
	case options.BaseUrl != "":
		action, reason = "skip", fmt.Sprintf("unsupported baseUrl %s", options.BaseUrl)
	case options.Target == core.ScriptTargetES5:
		action, reason = "skip", fmt.Sprintf("unsupported target %s", options.Target)
	case options.AlwaysStrict.IsFalse():
		action, reason = "skip", "alwaysStrict=false is unsupported"
	}
	return map[string]any{"decision": action, "reasonBase64": authoredNoEmitB64(reason)}
}
func TestAuthoredWholeNoEmit(t *testing.T) {
	input, output := os.Getenv("CHECKER_NOEMIT_REQUESTS"), os.Getenv("CHECKER_NOEMIT_OUTPUT")
	if input == "" || output == "" {
		t.Fatal("Explicit request/output paths required")
	}
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
	seen := map[string]bool{}
	allowed := map[string]bool{}
	for _, kind := range []CompilerTestType{TestTypeRegression, TestTypeConformance} {
		for _, file := range NewCompilerBaselineRunner(kind).EnumerateTestFiles() {
			allowed[filepath.Clean(file)] = true
		}
	}
	for decoder.More() {
		var request authoredNoEmitRequest
		if err := decoder.Decode(&request); err != nil {
			t.Fatal(err)
		}
		if request.Name == "" || seen[request.Name] {
			t.Fatal("Empty or duplicate request name")
		}
		seen[request.Name] = true
		if !allowed[filepath.Clean(request.Source)] {
			t.Fatalf("Source is outside original compiler/conformance enumeration: %s", request.Source)
		}
		source, err := os.ReadFile(request.Source)
		if err != nil {
			t.Fatal(err)
		}
		digest := sha256.Sum256(source)
		if hex.EncodeToString(digest[:]) != request.SourceSha256 {
			t.Fatal("Original source hash changed")
		}
		t.Run(request.Name, func(t *testing.T) {
			row := map[string]any{"name": request.Name, "status": "failed-original-runtime", "sourceSha256": request.SourceSha256, "configuration": request.Configuration}
			defer func() {
				if failure := recover(); failure != nil {
					row["errorBase64"] = authoredNoEmitB64(fmt.Sprint(failure))
					t.Errorf("Original no-emit observation panic: %v", failure)
				}
				if t.Failed() {
					row["status"] = "failed-original-runtime"
				}
				if err := encoder.Encode(row); err != nil {
					t.Error(err)
				}
			}()
			original := getCompilerFileBasedTest(t, request.Source)
			configs := original.configurations
			if len(configs) == 0 {
				configs = []*harnessutil.NamedTestConfiguration{{Name: "", Config: nil}}
			}
			var configuration *harnessutil.NamedTestConfiguration
			for _, candidate := range configs {
				if candidate.Name == request.Configuration {
					configuration = candidate
					break
				}
			}
			if configuration == nil {
				t.Fatal("Requested configuration is absent from original expansion")
			}
			payload := makeUnitsFromTest(original.content, original.filename)
			observed := authoredNewNoEmitCompilerTest(t, request.Name, request.Source, &payload, configuration)
			originalSkip := slices.Contains(skippedTests, filepath.Base(request.Source))
			row["originalPreConfigurationSkip"] = originalSkip
			row["originalOptionPolicy"] = authoredOriginalOptionPolicy(observed.options)
			o := &authoredNoEmitObserver{files: map[*ast.SourceFile]int{}, owners: []any{}}
			row["preChecking"] = authoredNoEmitChannels(o, observed.Channels.PreChecking)
			row["checking"] = authoredNoEmitChannels(o, observed.Channels.Checking)
			row["owners"] = o.owners
			row["typesAndSymbolsDisabled"] = observed.harnessOptions.NoTypesAndSymbols
			baselines := observed.authoredFullBaselines()
			if baselines == nil {
				row["typesBase64"] = nil
				row["symbolsBase64"] = nil
			} else {
				row["typesBase64"] = authoredNoEmitB64(baselines.Types)
				row["symbolsBase64"] = authoredNoEmitB64(baselines.Symbols)
			}
			row["traceBase64"] = authoredNoEmitB64(observed.result.Trace)
			observed.verifyUnionOrdering(t) // Exact original assertion body, after full walker/trace.
			unionPassed := !t.Failed()
			observed.verifyParentPointers(t)
			row["originalAssertions"] = map[string]bool{"unionOrdering": unionPassed, "parentPointers": !t.Failed()}
			row["status"] = "observed"
		})
	}
	if len(seen) == 0 {
		t.Fatal("Empty no-emit batch")
	}
}
