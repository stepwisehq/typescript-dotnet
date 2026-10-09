// Authored finite Linux read-side fixture observations. Original VFS operations
// execute unchanged; native os writes only create inputs. No private Sys layout.
package main

import (
	"net"
	"os"
	"path/filepath"
	"runtime"
	"syscall"
	"time"

	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/osvfs"
)

func osReadProjection() any {
	if runtime.GOOS != "linux" {
		panic("Linux read-side fixture required")
	}
	root, err := os.MkdirTemp("", "configuration-os-original-")
	if err != nil {
		panic(err)
	}
	defer os.RemoveAll(root)
	must := func(e error) {
		if e != nil {
			panic(e)
		}
	}
	fs := osvfs.FS()
	native := filepath.Join(root, "é😀.txt")
	must(os.WriteFile(native, []byte{1, 2, 3}, 0640))
	must(os.Chmod(native, 0640))
	moment := time.Unix(123456789, 0)
	must(os.Chtimes(native, moment, moment))
	file := tspath.RootedFilePathFromAbsolute(native)
	stat := fs.Stat(file.AsPath())
	result := map[string]any{"regular": map[string]any{"nameBase64": b64(stat.Name()), "size": stat.Size(), "mode": uint32(stat.Mode()), "modifiedSeconds": stat.ModTime().Unix(), "isDirectory": stat.IsDir()}, "caseSensitivity": int(fs.CaseSensitivity())}
	nul := tspath.RootedFilePath(file.AsString() + "\x00ignored")
	_, readOK := fs.ReadFile(nul)
	nulDir := tspath.RootedDirectoryPath(root + "\x00ignored")
	entries := fs.GetAccessibleEntries(nulDir)
	result["nul"] = map[string]any{"statIsNil": fs.Stat(nul.AsPath()) == nil, "fileExists": fs.FileExists(nul), "readOK": readOK, "sameRealpath": fs.Realpath(nul.AsPath()) == nul.AsPath(), "directoryExists": fs.DirectoryExists(nulDir), "filesAreNil": entries.Files == nil, "directoriesAreNil": entries.Directories == nil, "symlinksAreNil": entries.Symlinks == nil, "symlinkCount": len(entries.Symlinks)}
	fifo := filepath.Join(root, "fifo")
	must(syscall.Mkfifo(fifo, 0640))
	must(os.Chmod(fifo, 0640))
	socketName := filepath.Join(root, "socket")
	socket, err := net.ListenUnix("unix", &net.UnixAddr{Name: socketName, Net: "unix"})
	must(err)
	defer socket.Close()
	result["special"] = map[string]any{"fifoType": uint32(fs.Stat(tspath.RootedPath(fifo)).Mode().Type()), "socketType": uint32(fs.Stat(tspath.RootedPath(socketName)).Mode().Type()), "fifoExists": fs.FileExists(tspath.RootedFilePath(fifo)), "socketExists": fs.FileExists(tspath.RootedFilePath(socketName))}
	broken := filepath.Join(root, "broken")
	loop := filepath.Join(root, "loop")
	good := filepath.Join(root, "good")
	must(os.Symlink(filepath.Join(root, "missing"), broken))
	must(os.Symlink(loop, loop))
	must(os.Symlink(native, good))
	result["links"] = map[string]any{"brokenStatIsNil": fs.Stat(tspath.RootedPath(broken)) == nil, "loopStatIsNil": fs.Stat(tspath.RootedPath(loop)) == nil, "sameBrokenRealpath": fs.Realpath(tspath.RootedPath(broken)) == tspath.RootedPath(broken), "sameLoopRealpath": fs.Realpath(tspath.RootedPath(loop)) == tspath.RootedPath(loop), "goodType": uint32(fs.Stat(tspath.RootedPath(good)).Mode().Type())}
	entries = fs.GetAccessibleEntries(tspath.RootedDirectoryPath(root))
	var names []string
	for _, name := range entries.Files {
		names = append(names, b64(name))
	}
	result["entries"] = map[string]any{"fileNamesBase64": names, "directoriesAreNil": entries.Directories == nil, "symlinkCount": len(entries.Symlinks), "goodIsSymlink": func() bool { _, ok := entries.Symlinks["good"]; return ok }()}
	inputs := [][]byte{{}, {0xef, 0xbb, 0xbf, 0x61}, {0xef, 0xbb}, {0xff, 0xfe, 0x61, 0}, {0xfe, 0xff, 0, 0x61}, {0xff, 0xfe, 0x61, 0, 0x62}, {0xff, 0xfe, 0x3d, 0xd8, 0, 0xde}, {0xfe, 0xff, 0xd8, 0x3d, 0xde, 0}, {0xff, 0xfe, 0, 0xd8}, {0xff, 0xfe, 0, 0xdc}, {0xff, 0xfe}, {0xff}, {0xed, 0xa0, 0x80}}
	reads := []any{}
	for i, input := range inputs {
		path := filepath.Join(root, "bom")
		must(os.WriteFile(path, input, 0600))
		contents, ok := fs.ReadFile(tspath.RootedFilePath(path))
		reads = append(reads, map[string]any{"index": i, "inputBase64": b64(string(input)), "contentsBase64": b64(contents), "ok": ok})
	}
	result["bom"] = reads
	return result
}
