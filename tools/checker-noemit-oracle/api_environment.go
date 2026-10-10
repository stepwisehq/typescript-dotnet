// Authored no-emit harness environment adapter. Adds only the original locked
// public API declaration roles; compiler options and roots remain untouched.
package harnessutil

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"slices"
	"strings"
)

const authoredAPIEnvironmentRelative = "validation/checker-noemit-environment/typescript-6.0.3"

func authoredAddAPIEnvironment(files map[string]string) error {
	roles := []struct{ virtual, member string }{
		{"/.ts/typescript.d.ts", "lib/typescript.d.ts"},
		{"/.ts/tsserverlibrary.d.ts", "lib/tsserverlibrary.d.ts"},
	}
	active := false
	for _, text := range files {
		for _, role := range roles {
			active = active || strings.Contains(text, role.virtual)
		}
	}
	if !active {
		return nil
	}

	directory := os.Getenv("CHECKER_NOEMIT_API_ENVIRONMENT")
	if directory == "" {
		cwd, err := os.Getwd()
		if err != nil {
			return err
		}
		for root := cwd; ; root = filepath.Dir(root) {
			candidate := filepath.Join(root, authoredAPIEnvironmentRelative)
			if _, err := os.Stat(filepath.Join(candidate, "provenance.json")); err == nil {
				directory = candidate
				break
			}
			if filepath.Dir(root) == root {
				return fmt.Errorf("authentic locked API environment is unavailable")
			}
		}
	}
	directory, err := filepath.Abs(directory)
	if err != nil {
		return err
	}
	root := filepath.Dir(filepath.Dir(filepath.Dir(directory)))
	if directory != filepath.Join(root, authoredAPIEnvironmentRelative) {
		return fmt.Errorf("API environment must use the fixed repository package location")
	}
	data, err := os.ReadFile(filepath.Join(directory, "provenance.json"))
	if err != nil {
		return err
	}
	var provenance struct {
		Classification, Upstream, Version, LockSource, LockSourceSha256, Generator, GeneratorSha256 string
		Files                                                                                       map[string]struct {
			Sha256         string
			Bytes          int
			OriginalMember string
		}
		VirtualRoles             map[string]string
		UnavailableOriginalRoles []string
	}
	if err := json.Unmarshal(data, &provenance); err != nil {
		return err
	}
	if provenance.Classification != "authentic-upstream-locked-public-api-environment-adaptation" ||
		provenance.Upstream != "fed0bf24149fb1ed36039212648bafdafc1ea10e" || provenance.Version != "6.0.3" {
		return fmt.Errorf("unrecognized API environment provenance")
	}
	checkFile := func(path, expected string) ([]byte, error) {
		bytes, err := os.ReadFile(path)
		if err != nil {
			return nil, err
		}
		digest := sha256.Sum256(bytes)
		if hex.EncodeToString(digest[:]) != expected {
			return nil, fmt.Errorf("API environment file differs: %s", path)
		}
		return bytes, nil
	}
	if provenance.LockSource != "upstream/TypeScript/package-lock.json" || provenance.Generator != "tools/prepare-checker-api-environment.py" {
		return fmt.Errorf("API lock or preparation source changed")
	}
	if _, err := checkFile(filepath.Join(root, provenance.LockSource), provenance.LockSourceSha256); err != nil {
		return err
	}
	if _, err := checkFile(filepath.Join(root, provenance.Generator), provenance.GeneratorSha256); err != nil {
		return err
	}
	members := []string{"lib/typescript.d.ts", "lib/tsserverlibrary.d.ts", "LICENSE.txt", "ThirdPartyNoticeText.txt", "package.json"}
	if len(provenance.Files) != len(members) || len(provenance.VirtualRoles) != len(roles) ||
		!slices.Equal(provenance.UnavailableOriginalRoles, []string{"/.ts/typescript.internal.d.ts", "/.ts/tsserverlibrary.internal.d.ts"}) {
		return fmt.Errorf("API environment member, role or internal availability set changed")
	}
	payloads := map[string]string{}
	for _, member := range members {
		metadata, ok := provenance.Files[member]
		if !ok || metadata.OriginalMember != "package/"+member {
			return fmt.Errorf("API member role changed: %s", member)
		}
		bytes, err := checkFile(filepath.Join(directory, member), metadata.Sha256)
		if err != nil {
			return err
		}
		if len(bytes) != metadata.Bytes {
			return fmt.Errorf("API member length differs: %s", member)
		}
		payloads[member] = string(bytes)
	}
	for _, role := range roles {
		if provenance.VirtualRoles[role.virtual] != role.member {
			return fmt.Errorf("API virtual role changed")
		}
		if existing, ok := files[role.virtual]; ok && existing != payloads[role.member] {
			return fmt.Errorf("conflicting explicit API fixture role")
		}
	}
	for _, role := range roles {
		files[role.virtual] = payloads[role.member]
	}
	return nil
}
