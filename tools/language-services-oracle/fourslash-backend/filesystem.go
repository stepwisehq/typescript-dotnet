// Authored forwarding adapter over the original authoritative vfstest/bundled FS.
package lsptestutil

import (
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs"
	"time"
)

func candidateEncode(raw string) string { return base64.StdEncoding.EncodeToString([]byte(raw)) }
func candidateDecode(raw string) (string, error) {
	b, err := base64.StdEncoding.DecodeString(raw)
	return string(b), err
}
func candidateJSON(value any) (json.RawMessage, *string) {
	raw, err := json.Marshal(value)
	if err != nil {
		s := candidateEncode(err.Error())
		return nil, &s
	}
	return raw, nil
}
func candidateFailure(err error) (json.RawMessage, *string) {
	s := candidateEncode(err.Error())
	return nil, &s
}

func candidateFSDispatch(fs vfs.FS, operation string, raw json.RawMessage) (json.RawMessage, *string) {
	var args struct {
		Path              string `json:"path"`
		Contents          string `json:"contents"`
		AccessTicks       int64  `json:"accessTicks"`
		ModificationTicks int64  `json:"modificationTicks"`
	}
	if err := json.Unmarshal(raw, &args); err != nil {
		return candidateFailure(err)
	}
	path, err := candidateDecode(args.Path)
	if err != nil {
		return candidateFailure(err)
	}
	// Typed paths admit the same normalized rooted names used by the original host.
	switch operation {
	case "fs.fileExists":
		return candidateJSON(fs.FileExists(tspath.RootedFilePath(path)))
	case "fs.directoryExists":
		return candidateJSON(fs.DirectoryExists(tspath.RootedDirectoryPath(path)))
	case "fs.readFile":
		contents, ok := fs.ReadFile(tspath.RootedFilePath(path))
		return candidateJSON(map[string]any{"contents": candidateEncode(contents), "ok": ok})
	case "fs.writeFile":
		data, err := candidateDecode(args.Contents)
		if err != nil {
			return candidateFailure(err)
		}
		if err = fs.WriteFile(tspath.RootedFilePath(path), data); err != nil {
			return candidateJSON(candidateEncode(err.Error()))
		}
		return candidateJSON(nil)
	case "fs.appendFile":
		data, err := candidateDecode(args.Contents)
		if err != nil {
			return candidateFailure(err)
		}
		if err = fs.AppendFile(tspath.RootedFilePath(path), data); err != nil {
			return candidateJSON(candidateEncode(err.Error()))
		}
		return candidateJSON(nil)
	case "fs.remove":
		if err = fs.Remove(tspath.RootedPath(path)); err != nil {
			return candidateJSON(candidateEncode(err.Error()))
		}
		return candidateJSON(nil)
	case "fs.chtimes":
		fromTicks := func(ticks int64) time.Time { return time.Unix(ticks/10000000, (ticks%10000000)*100) }
		if err = fs.Chtimes(tspath.RootedPath(path), fromTicks(args.AccessTicks), fromTicks(args.ModificationTicks)); err != nil {
			return candidateJSON(candidateEncode(err.Error()))
		}
		return candidateJSON(nil)
	case "fs.realpath":
		return candidateJSON(candidateEncode(string(fs.Realpath(tspath.RootedPath(path)))))
	case "fs.entries":
		entries := fs.GetAccessibleEntries(tspath.RootedDirectoryPath(path))
		encodeNames := func(input []string) []string {
			if input == nil {
				return nil
			}
			output := make([]string, len(input))
			for i, s := range input {
				output[i] = candidateEncode(s)
			}
			return output
		}
		var symlinks []string
		if entries.Symlinks != nil {
			symlinks = make([]string, 0, len(entries.Symlinks))
			for name := range entries.Symlinks {
				symlinks = append(symlinks, candidateEncode(name))
			}
		}
		return candidateJSON(map[string]any{"files": encodeNames(entries.Files), "directories": encodeNames(entries.Directories), "symlinks": symlinks})
	case "fs.stat":
		info := fs.Stat(tspath.RootedPath(path))
		if info == nil {
			return candidateJSON(nil)
		}
		return candidateJSON(map[string]any{"name": candidateEncode(info.Name()), "size": info.Size(), "mode": uint32(info.Mode()), "seconds": info.ModTime().Unix(), "nanoseconds": info.ModTime().Nanosecond(), "isDir": info.IsDir()})
	default:
		return candidateFailure(fmt.Errorf("unknown candidate filesystem operation %q", operation))
	}
}
