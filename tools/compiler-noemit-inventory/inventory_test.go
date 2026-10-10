// Authored inventory only: use the unchanged pinned compiler harness producers.
// This extra test file is supplied by Go overlay; no upstream source is replaced.
package testrunner

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"os"
	"path/filepath"
	"slices"
	"strings"
	"testing"

	"github.com/microsoft/TypeScript/tsc/internal/testutil/harnessutil"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type inventoryUnit struct {
	Name        string            `json:"name"`
	TextSha256  string            `json:"textSha256"`
	TextBytes   int               `json:"textBytes"`
	FileOptions map[string]string `json:"fileOptions"`
}

func inventoryHash(bytes []byte) string {
	hash := sha256.Sum256(bytes)
	return hex.EncodeToString(hash[:])
}

func TestAuthoredCompilerNoEmitInventory(t *testing.T) {
	output := os.Getenv("COMPILER_NOEMIT_INVENTORY_OUTPUT")
	if output == "" {
		t.Fatal("Explicit inventory output path required")
	}
	f, err := os.Create(output)
	if err != nil {
		t.Fatal(err)
	}
	defer f.Close()
	encoder := json.NewEncoder(f)
	seen := make(map[string]bool)
	for _, suite := range []CompilerTestType{TestTypeRegression, TestTypeConformance} {
		runner := NewCompilerBaselineRunner(suite)
		files := slices.Clone(runner.EnumerateTestFiles())
		slices.Sort(files)
		for _, filename := range files {
			base := tspath.GetBaseFileName(filename)
			if seen[base] {
				t.Fatalf("Original harness duplicate basename: %s", base)
			}
			seen[base] = true
			content, err := os.ReadFile(filename)
			if err != nil {
				t.Fatal(err)
			}
			settings := extractCompilerSettings(string(content))
			units, links, cwd, globalOptions, parseErrors := ParseTestFilesAndSymlinks(string(content), filename,
				func(name, text string, options map[string]string) (inventoryUnit, error) {
					return inventoryUnit{Name: name, TextSha256: inventoryHash([]byte(text)), TextBytes: len(text), FileOptions: options}, nil
				})
			var extractionError any
			if parseErrors != nil {
				extractionError = parseErrors.Error()
			}
			row := map[string]any{"suite": runner.testSuitName, "source": filepath.ToSlash(filename),
				"sourceSha256": inventoryHash(content), "sourceBytes": len(content), "rawSettings": settings,
				"units": units, "symlinks": links, "currentDirectory": cwd, "globalOptions": globalOptions,
				"extractionError": extractionError, "classification": "inventoried-not-executed"}
			if slices.Contains(skippedTests, base) {
				row["upstreamDisposition"] = "skipped-before-configurations"
				if strings.HasPrefix(base, "APISample_") || base == "APILibCheck.ts" {
					row["upstreamReason"] = "requires original built typescript.d.ts API environment"
				} else {
					row["upstreamReason"] = "original explicit removed-option skip list"
				}
				row["configurations"] = nil
			} else {
				row["upstreamDisposition"] = "configuration-enumerated"
				configs := harnessutil.GetFileBasedTestConfigurations(t, settings, compilerVaryBy)
				if len(configs) == 0 {
					configs = []*harnessutil.NamedTestConfiguration{{Name: "", Config: nil}}
				}
				slices.SortFunc(configs, func(a, b *harnessutil.NamedTestConfiguration) int { return strings.Compare(a.Name, b.Name) })
				row["configurations"] = configs
			}
			if err := encoder.Encode(row); err != nil {
				t.Fatal(err)
			}
		}
	}
	if len(seen) == 0 {
		t.Fatal("Empty compiler inventory")
	}
}
