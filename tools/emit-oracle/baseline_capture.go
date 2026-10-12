// Authored additive observation of original baseline records; no assertion is replaced.
package baseline

import (
	"encoding/base64"
	"os"
	"path/filepath"
	"slices"
)

// AuthoredRecordedNames requires the original tracking facility to be enabled.
func AuthoredRecordedNames() []string {
	if trackingDir == "" || !trackingInitialized {
		panic("Original baseline tracking must be enabled")
	}
	names := []string{}
	for name := range recordedBaselines.Keys() {
		names = append(names, name)
	}
	slices.Sort(names)
	return names
}

func AuthoredObserveRecorded(name string) any {
	// Run removes its previous local file before comparing. Matching content is
	// therefore the unchanged reference; differing content is the local artifact.
	local := filepath.Join(localRoot, name)
	content, err := os.ReadFile(local)
	origin := "original-local-difference"
	if os.IsNotExist(err) {
		if _, deleted := os.Stat(local + ".delete"); deleted == nil {
			content, err, origin = []byte(NoContent), nil, "original-no-content"
		} else {
			content, err = os.ReadFile(filepath.Join(referenceRoot, name))
			origin = "original-reference-match"
			if os.IsNotExist(err) {
				content, err, origin = []byte(NoContent), nil, "original-no-content"
			}
		}
	}
	if err != nil {
		panic(err)
	}
	return map[string]any{"path": filepath.ToSlash(name), "contentBase64": base64.StdEncoding.EncodeToString(content), "origin": origin}
}
