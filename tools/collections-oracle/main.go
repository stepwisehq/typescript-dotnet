// Authored independent fixed-library Program oracle. Bootstrap observations are
// deliberately distinguished from the forthcoming demanded semantic model graph.
package main

import (
	"bufio"
	"context"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
	"os"
	"path/filepath"
	"reflect"
)

type CollectionRequest struct {
	Request
	Profile            string `json:"profile"`
	StoredObservations bool   `json:"storedObservations"`
	QueryNodeIDs       []int  `json:"queryNodeIds"`
}
type FileRecord struct {
	Index          int               `json:"index"`
	Name           string            `json:"fileName"`
	Hash           string            `json:"sha256"`
	Root           int               `json:"rootNodeId"`
	DefaultLibrary bool              `json:"defaultLibrary"`
	Declaration    bool              `json:"declaration"`
	JSX            bool              `json:"externalModuleJSX"`
	Force          bool              `json:"externalModuleForce"`
	Skip           bool              `json:"skipTypeChecking"`
	Bound          bool              `json:"isBound"`
	SymbolCount    int               `json:"symbolCount"`
	Parse          []*FullDiagnostic `json:"parseDiagnostics"`
	Bind           []*FullDiagnostic `json:"bindDiagnostics"`
	Program        []*FullDiagnostic `json:"programSemanticDiagnostics"`
}
type FullDiagnostic struct {
	Code        int32             `json:"code"`
	Category    int32             `json:"category"`
	Pos         int               `json:"pos"`
	End         int               `json:"end"`
	Key         string            `json:"key"`
	Args        []string          `json:"argsBase64"`
	Text        string            `json:"text"`
	File        *int              `json:"fileIndex"`
	Source      string            `json:"sourceBase64"`
	Unnecessary bool              `json:"reportsUnnecessary"`
	Deprecated  bool              `json:"reportsDeprecated"`
	Skip        bool              `json:"skippedOnNoEmit"`
	Chain       []*FullDiagnostic `json:"chain"`
	Related     []*FullDiagnostic `json:"related"`
}
type NodeOwner struct {
	Node  int  `json:"nodeId"`
	File  *int `json:"fileIndex"`
	Local *int `json:"fileNodeId"`
}
type CollectionQuery struct {
	Node         int    `json:"nodeId"`
	Text         string `json:"text"`
	Flags        uint32 `json:"flags"`
	ObjectFlags  uint32 `json:"objectFlags"`
	Payload      string `json:"dataType"`
	Symbol       *int   `json:"symbol"`
	RepeatType   bool   `json:"repeatType"`
	RepeatSymbol bool   `json:"repeatSymbol"`
}
type CollectionResult struct {
	Stage                    string            `json:"stage"`
	Stored                   *StoredGraph      `json:"storedGraph,omitempty"`
	Options                  map[string]any    `json:"options"`
	Files                    []FileRecord      `json:"files"`
	Nodes                    []*Node           `json:"nodes"`
	Owners                   []NodeOwner       `json:"nodeOwners"`
	Binding                  Binding           `json:"binding"`
	Globals                  *int              `json:"globals"`
	GlobalThis               *int              `json:"globalThisSymbol"`
	Queries                  []CollectionQuery `json:"queries"`
	GlobalDiagnostics        []*FullDiagnostic `json:"globalDiagnostics"`
	ConfigurationDiagnostics []*FullDiagnostic `json:"configurationDiagnostics"`
}

var libraryNames = []string{"lib.es5.d.ts", "lib.decorators.d.ts", "lib.decorators.legacy.d.ts"}
var libraryHashes = []string{"45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215", "1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4", "b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da"}

func fullDiagnostics(values []*ast.Diagnostic, files map[*ast.SourceFile]int) []*FullDiagnostic {
	if values == nil {
		return nil
	}
	out := []*FullDiagnostic{}
	for _, d := range values {
		var args []string
		if d.MessageArgs() != nil {
			args = []string{}
			for _, a := range d.MessageArgs() {
				args = append(args, bytes(a))
			}
		}
		v := &FullDiagnostic{Code: d.Code(), Category: int32(d.Category()), Pos: d.Pos(), End: d.End(), Key: string(d.MessageKey()), Args: args, Text: d.String(), Source: bytes(d.Source()), Unnecessary: d.ReportsUnnecessary(), Deprecated: d.ReportsDeprecated(), Skip: d.SkippedOnNoEmit(), Chain: fullDiagnostics(d.MessageChain(), files), Related: fullDiagnostics(d.RelatedInformation(), files)}
		if d.File() != nil {
			index, ok := files[d.File()]
			if !ok {
				panic("Diagnostic file outside fixed environment")
			}
			v.File = ptr(index)
		}
		if d.RepopulateInfo() != nil {
			panic("Unexpected incremental diagnostic state")
		}
		out = append(out, v)
	}
	return out
}
func runCollection(r CollectionRequest) CollectionResult {
	if r.Profile != "Collections" {
		panic("Expected Collections profile")
	}
	if r.FileName == "" {
		r.FileName = "/main.ts"
	}
	if r.FileName != "/main.ts" {
		panic("Fixed user source must be /main.ts")
	}
	text := r.Text
	if r.TextBytesBase64 != nil {
		b, err := base64.StdEncoding.DecodeString(*r.TextBytesBase64)
		if err != nil {
			panic(err)
		}
		text = string(b)
	}
	fsFiles := map[string]string{r.FileName: text}
	roots := []string{}
	for i, name := range libraryNames {
		b, err := os.ReadFile(filepath.Join("upstream/TypeScript/tsc/internal/bundled/libs", name))
		if err != nil {
			panic(err)
		}
		if fmt.Sprintf("%x", sha256.Sum256(b)) != libraryHashes[i] {
			panic("Library bytes differ from pin")
		}
		path := "/lib/" + name
		roots = append(roots, path)
		fsFiles[path] = string(b)
	}
	roots = append(roots, r.FileName)
	rootsJSON, _ := json.Marshal(roots)
	fsFiles["/tsconfig.json"] = fmt.Sprintf(`{"files":%s,"compilerOptions":{"target":"esnext","noLib":true,"noResolve":true,"skipLibCheck":true,"strictNullChecks":true,"strictFunctionTypes":true,"noImplicitAny":true,"noImplicitThis":true,"strictBindCallApply":false,"noImplicitReturns":false,"exactOptionalPropertyTypes":false,"noUncheckedIndexedAccess":false,"downlevelIteration":false}}`, rootsJSON)
	fs := vfstest.FromMap(fsFiles, tspath.CaseSensitive)
	parsed, errors := tsoptions.GetParsedCommandLineOfConfigFile("/tsconfig.json", &core.CompilerOptions{}, nil, fs, nil)
	if len(errors) != 0 {
		panic(errors)
	}
	host := compiler.NewCompilerHost(fs, tspath.RootedDirectoryPathFromNormalized("/lib"), nil, nil, nil)
	program := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue, SkipModuleResolution: true}, ProgramHosts: compiler.ProgramHosts{Host: host}})
	files := program.SourceFiles()
	if len(files) != len(roots) {
		panic("Unexpected fixed environment file count")
	}
	fileIDs := map[*ast.SourceFile]int{}
	for i, f := range files {
		if f.FileName().AsString() != roots[i] {
			panic("Program file order differs")
		}
		fileIDs[f] = i
		// Compare the actual Program parse mode with the authored loader's direct mode,
		// before any lazy documentation getter or checking can populate its state.
		if i < 3 {
			direct := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: f.FileName(), PathKey: f.PathKey()}, f.Text(), core.ScriptKindTS)
			a := encoder{file: f, ids: map[*ast.Node]int{}}
			b := encoder{file: direct, ids: map[*ast.Node]int{}}
			a.node(f.AsNode())
			b.node(direct.AsNode())
			aa, _ := json.Marshal(a.output)
			bb, _ := json.Marshal(b.output)
			if string(aa) != string(bb) || f.ParseOptions() != direct.ParseOptions() {
				panic("Program and fixed loader library parse mode differ")
			}
		}
	}
	c, _ := checker.NewChecker(program, nil)
	defer checker.OracleCollectionsRelease(c)
	file := files[3]
	c.GetDiagnostics(context.Background(), file)
	e := encoder{file: file, ids: map[*ast.Node]int{}}
	result := CollectionResult{Stage: "environment-bootstrap", Files: []FileRecord{}, Owners: []NodeOwner{}, Queries: []CollectionQuery{}, Options: map[string]any{"profile": "Collections", "target": "ESNext", "noLib": true, "noResolve": true, "skipModuleResolution": true, "skipLibCheck": true, "strictNullChecks": true, "strictFunctionTypes": true, "noImplicitAny": true, "noImplicitThis": true, "strictBindCallApply": false, "noImplicitReturns": false, "exactOptionalPropertyTypes": false, "noUncheckedIndexedAccess": false, "downlevelIteration": false}}
	for i, f := range files {
		root := e.node(f.AsNode())
		opts := f.ParseOptions()
		var ds []*ast.Diagnostic
		ds = compiler.OracleCollectionsSemanticDiagnostics(program, c, f)
		result.Files = append(result.Files, FileRecord{Index: i, Name: f.FileName().AsString(), Hash: fmt.Sprintf("%x", sha256.Sum256([]byte(f.Text()))), Root: root, DefaultLibrary: program.IsSourceFileDefaultLibrary(f.PathKey()), Declaration: f.IsDeclarationFile, JSX: opts.ExternalModuleIndicatorOptions.JSX, Force: opts.ExternalModuleIndicatorOptions.Force, Skip: program.SkipTypeChecking(f, false), Bound: f.IsBound(), SymbolCount: int(f.SymbolCount), Parse: fullDiagnostics(f.Diagnostics(), fileIDs), Bind: fullDiagnostics(f.BindDiagnostics(), fileIDs), Program: fullDiagnostics(ds, fileIDs)})
	}
	parsedNodeCount := len(e.raw)
	g := newGraph(&e)
	ids := r.QueryNodeIDs
	if ids == nil {
		ids = []int{}
		for id, n := range e.raw {
			if ast.IsVariableDeclaration(n) && n.Name() != nil && ast.IsIdentifier(n.Name()) && ast.GetSourceFileOfNode(n) == file {
				ids = append(ids, e.ids[n.Name()])
			}
			_ = id
		}
	}
	queriedTypes := []*checker.Type{}
	for _, id := range ids {
		if id < 0 || id >= len(e.raw) || ast.GetSourceFileOfNode(e.raw[id]) != file {
			panic("Query outside user syntax")
		}
		n := e.raw[id]
		t := c.GetTypeAtLocation(n)
		queriedTypes = append(queriedTypes, t)
		s := c.GetSymbolAtLocation(n)
		result.Queries = append(result.Queries, CollectionQuery{id, c.TypeToString(t), uint32(t.Flags()), uint32(t.ObjectFlags()), readable(reflect.ValueOf(t).Elem().FieldByName("data")).Elem().Elem().Type().Name(), g.symbol(s), t == c.GetTypeAtLocation(n), s == c.GetSymbolAtLocation(n)})
	}
	globals := readable(reflect.ValueOf(c).Elem().FieldByName("globals")).Interface().(ast.SymbolTable)
	result.Globals = g.table(globals)
	result.GlobalThis = g.symbol(globals["globalThis"])
	result.Binding = g.encode(file)
	if r.StoredObservations {
		stored := newStoredEncoder(c, g).encode(queriedTypes)
		result.Stored = &stored
		for len(g.output.Nodes) < len(e.raw) {
			i := len(g.output.Nodes)
			out := BoundNode{NodeID: i, Fields: []BoundField{}}
			g.fields(readable(reflect.ValueOf(e.raw[i]).Elem().FieldByName("data")).Elem().Elem(), &out)
			g.output.Nodes = append(g.output.Nodes, out)
		}
		result.Binding = g.output
		for i, n := range e.raw {
			if n.Parent != nil {
				p := &Parent{Kind: n.Parent.Kind, Pos: n.Parent.Pos(), End: n.Parent.End()}
				if id, ok := e.ids[n.Parent]; ok {
					p.ID = ptr(id)
				}
				e.output[i].Parent = p
			}
		}
	}
	result.Nodes = e.output
	localCounts := make([]int, len(files))
	for id, n := range e.raw {
		root := n
		seen := map[*ast.Node]bool{}
		for root.Parent != nil {
			if seen[root] {
				panic("Cyclic stored node parent chain")
			}
			seen[root] = true
			root = root.Parent
		}
		var ownerIndex, localIndex *int
		if root.Kind == ast.KindSourceFile {
			index, ok := fileIDs[root.AsSourceFile()]
			if !ok {
				panic("Stored node outside fixed files")
			}
			ownerIndex = ptr(index)
			if id < parsedNodeCount {
				localIndex = ptr(localCounts[index])
				localCounts[index]++
			}
		} else if id < parsedNodeCount {
			panic("Parsed node lost file ownership")
		}
		result.Owners = append(result.Owners, NodeOwner{id, ownerIndex, localIndex})
	}
	result.GlobalDiagnostics = fullDiagnostics(c.GetGlobalDiagnostics(), fileIDs)
	result.ConfigurationDiagnostics = fullDiagnostics(errors, fileIDs)
	return result
}
func main() {
	if len(os.Args) == 2 && os.Args[1] == "--support-projections" {
		if err := json.NewEncoder(os.Stdout).Encode(supportProjections()); err != nil {
			panic(err)
		}
		return
	}
	in := bufio.NewScanner(os.Stdin)
	in.Buffer(make([]byte, 65536), 32*1024*1024)
	out := json.NewEncoder(os.Stdout)
	for in.Scan() {
		var r CollectionRequest
		if err := json.Unmarshal(in.Bytes(), &r); err != nil {
			panic(err)
		}
		if err := out.Encode(runCollection(r)); err != nil {
			panic(err)
		}
	}
	if err := in.Err(); err != nil {
		panic(err)
	}
}
