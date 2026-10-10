// Authored reconciliation against the actual original decoded disk-read producer.
// Additive overlay only. The earlier raw-byte inventory and its proof remain intact.
package testrunner

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"slices"
	"strings"
	"testing"

	"github.com/microsoft/TypeScript/tsc/internal/testutil/harnessutil"
)

func authoredDecodedHash(bytes []byte) string {
	h := sha256.Sum256(bytes)
	return hex.EncodeToString(h[:])
}
func TestAuthoredDecodedCompilerInventory(t *testing.T) {
	output := os.Getenv("CHECKER_DECODED_INVENTORY_OUTPUT")
	if output == "" {
		t.Fatal("Explicit decoded inventory output required")
	}
	f, err := os.Create(output)
	if err != nil {
		t.Fatal(err)
	}
	defer f.Close()
	encoder := json.NewEncoder(f)
	for _, suite := range []CompilerTestType{TestTypeRegression, TestTypeConformance} {
		runner := NewCompilerBaselineRunner(suite)
		files := slices.Clone(runner.EnumerateTestFiles())
		slices.Sort(files)
		for _, filename := range files {
			raw, err := os.ReadFile(filename)
			if err != nil {
				t.Fatal(err)
			}
			t.Run(runner.testSuitName+"/"+filepath.Base(filename), func(t *testing.T) {
				row := map[string]any{"suite": runner.testSuitName, "source": filepath.ToSlash(filename), "sourceSha256": authoredDecodedHash(raw), "sourceBytes": len(raw), "status": "failed-original-configuration-expansion", "originalPreConfigurationSkip": slices.Contains(skippedTests, filepath.Base(filename))}
				defer func() {
					if failure := recover(); failure != nil {
						row["error"] = strings.TrimSpace(strings.ReplaceAll(strings.TrimSpace(fmt.Sprint(failure)), "\r\n", "\n"))
						t.Errorf("Original configuration expansion panic: %v", failure)
					}
					if t.Failed() {
						row["status"] = "failed-original-configuration-expansion"
					}
					if err := encoder.Encode(row); err != nil {
						t.Error(err)
					}
				}()
				original := getCompilerFileBasedTest(t, filename) // osvfs.ReadFile decoding, not os.ReadFile text.
				configs := original.configurations
				if len(configs) == 0 {
					configs = []*harnessutil.NamedTestConfiguration{{Name: "", Config: nil}}
				}
				slices.SortFunc(configs, func(a, b *harnessutil.NamedTestConfiguration) int { return strings.Compare(a.Name, b.Name) })
				row["decodedTextSha256"] = authoredDecodedHash([]byte(original.content))
				row["decodedTextBytes"] = len(original.content)
				row["rawSettings"] = extractCompilerSettings(original.content)
				row["configurations"] = configs
				units, links, cwd, globals, extractionError := ParseTestFilesAndSymlinks(original.content, original.filename, func(name, text string, options map[string]string) (map[string]any, error) {
					return map[string]any{"name": name, "textSha256": authoredDecodedHash([]byte(text)), "textBytes": len(text), "fileOptions": options}, nil
				})
				row["symlinks"] = links
				row["currentDirectory"] = cwd
				row["globalOptions"] = globals
				if extractionError != nil {
					row["extractionError"] = extractionError.Error()
				} else {
					row["extractionError"] = nil
				}
				row["units"] = units
				row["status"] = "decoded-configuration-enumerated"
			})
		}
	}
}
