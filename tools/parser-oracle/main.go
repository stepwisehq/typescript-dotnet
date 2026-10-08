// Independent read-only structural oracle for the pinned TypeScript Go parser.
package main

import (
	"bufio"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"os"
	"reflect"
	"slices"
	"strings"
	"unsafe"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"github.com/microsoft/TypeScript/tsc/internal/scanner"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type Request struct {
	Name                   string          `json:"name"`
	Text                   string          `json:"text"`
	TextBytesBase64        *string         `json:"textBytesBase64"`
	ScriptKind             core.ScriptKind `json:"scriptKind"`
	FileName               string          `json:"fileName"`
	ExternalModuleJSX      bool            `json:"externalModuleJSX"`
	ExternalModuleForce    bool            `json:"externalModuleForce"`
	ResolveJSDoc           bool            `json:"resolveJSDoc"`
	TextProbeIDs           []int           `json:"textProbeIds"`
	TextProbeIncludeJSDoc  bool            `json:"textProbeIncludeJSDoc"`
	FoundationObservations bool            `json:"foundationObservations"`
}
type List struct {
	Identity      *int    `json:"identity,omitempty"`
	Pos           int     `json:"pos"`
	End           int     `json:"end"`
	NodesNull     bool    `json:"nodesNull"`
	Missing       bool    `json:"missing"`
	TrailingComma bool    `json:"trailingComma"`
	ModifierFlags *uint32 `json:"modifierFlags,omitempty"`
	Items         []int   `json:"items"`
}
type Field struct {
	Name    string    `json:"name"`
	Type    string    `json:"type"`
	IsNull  bool      `json:"isNull"`
	NodeID  *int      `json:"nodeId,omitempty"`
	List    *List     `json:"list,omitempty"`
	Number  *int64    `json:"number,omitempty"`
	Boolean *bool     `json:"boolean,omitempty"`
	Bytes   *string   `json:"bytesBase64,omitempty"`
	Nodes   *[]int    `json:"nodeIds,omitempty"`
	Strings *[]string `json:"stringsBase64,omitempty"`
}
type Parent struct {
	ID   *int     `json:"id,omitempty"`
	Kind ast.Kind `json:"kind"`
	Pos  int      `json:"pos"`
	End  int      `json:"end"`
}
type Node struct {
	ID         int           `json:"id"`
	Kind       ast.Kind      `json:"kind"`
	Pos        int           `json:"pos"`
	End        int           `json:"end"`
	Flags      ast.NodeFlags `json:"flags"`
	DataType   string        `json:"dataType"`
	Fields     []Field       `json:"fields"`
	ChildOrder []int         `json:"childOrder"`
	Parent     *Parent       `json:"parent"`
	JSDoc      []int         `json:"jsDoc"`
}
type Diagnostic struct {
	Code     int32        `json:"code"`
	Category int32        `json:"category"`
	Pos      int          `json:"pos"`
	End      int          `json:"end"`
	Key      string       `json:"key"`
	Args     []string     `json:"argsBase64"`
	Text     string       `json:"text"`
	Related  []Diagnostic `json:"related"`
	Chain    []Diagnostic `json:"chain"`
}
type Response struct {
	Nodes            []*Node        `json:"nodes"`
	ParseDiagnostics []Diagnostic   `json:"parseDiagnostics"`
	JSDiagnostics    []Diagnostic   `json:"jsDiagnostics"`
	JSDocDiagnostics []Diagnostic   `json:"jsDocDiagnostics"`
	Metadata         map[string]any `json:"metadata"`
	TextProbes       []TextProbe    `json:"textProbes"`
	Foundation       *Foundation    `json:"foundation,omitempty"`
}
type TextProbe struct {
	NodeID     int    `json:"nodeId"`
	TextBase64 string `json:"textBase64"`
	TokenPos   int    `json:"tokenPos"`
}
type encoder struct {
	file         *ast.SourceFile
	resolveJSDoc bool
	ids          map[*ast.Node]int
	raw          []*ast.Node
	output       []*Node
	resolvedDocs map[*ast.Node][]*ast.Node
	foundation   bool
	listIDs      map[*ast.NodeList]int
}

func bytes(s string) string { return base64.StdEncoding.EncodeToString([]byte(s)) }
func ptr[T any](value T) *T { return &value }
func readable(v reflect.Value) reflect.Value {
	if v.CanInterface() {
		return v
	}
	if !v.CanAddr() {
		panic("Unaddressable field " + v.Type().String())
	}
	return reflect.NewAt(v.Type(), unsafe.Pointer(v.UnsafeAddr())).Elem()
}

var nodeType = reflect.TypeFor[*ast.Node]()
var listType = reflect.TypeFor[*ast.NodeList]()
var modifiersType = reflect.TypeFor[*ast.ModifierList]()

// A known malformed parameter list exposes the pinned parser's shared missing
// backing array. Compare its identity, matching isMissingNodeList exactly;
// capacity alone could misclassify an ordinary empty list.
var missingBacking = func() **ast.Node {
	file := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: "/sentinel.ts", PathKey: "/sentinel.ts"}, "function f {}", core.ScriptKindTS)
	list := file.Statements.Nodes[0].AsFunctionDeclaration().Parameters
	if len(list.Nodes) != 0 || cap(list.Nodes) != 1 {
		panic("Pinned missing-list calibration no longer matches")
	}
	return unsafe.SliceData(list.Nodes)
}()
var excludedBases = map[string]bool{"NodeBase": true, "NodeDefault": true, "Node": true, "DeclarationBase": true, "ExportableBase": true, "LocalsContainerBase": true, "FlowNodeBase": true, "CompositeBase": true}

func (e *encoder) node(n *ast.Node) int {
	if id, ok := e.ids[n]; ok {
		return id
	}
	id := len(e.output)
	e.ids[n] = id
	e.raw = append(e.raw, n)
	value := readable(reflect.ValueOf(n).Elem().FieldByName("data")).Elem().Elem()
	result := &Node{ID: id, Kind: n.Kind, Pos: n.Pos(), End: n.End(), Flags: n.Flags, DataType: value.Type().Name(), Fields: []Field{}, ChildOrder: []int{}, JSDoc: []int{}}
	e.output = append(e.output, result)
	e.fields(value, result)
	n.ForEachChild(func(child *ast.Node) bool { result.ChildOrder = append(result.ChildOrder, e.node(child)); return false })
	if e.resolveJSDoc {
		docs := n.JSDoc(e.file)
		if e.resolvedDocs == nil {
			e.resolvedDocs = map[*ast.Node][]*ast.Node{}
		}
		e.resolvedDocs[n] = slices.Clone(docs)
		for _, doc := range docs {
			result.JSDoc = append(result.JSDoc, e.node(doc))
		}
	}
	return id
}
func (e *encoder) fields(v reflect.Value, n *Node) {
	t := v.Type()
	for i := 0; i < v.NumField(); i++ {
		sf := t.Field(i)
		value := v.Field(i)
		if value.Type().String() == "*ast.FlowNode" {
			continue
		}
		if sf.Anonymous {
			if excludedBases[sf.Type.Name()] {
				continue
			}
			e.fields(value, n)
			continue
		}
		if t.Name() == "SourceFile" && sf.Name != "Statements" && sf.Name != "EndOfFileToken" {
			continue
		}
		name := strings.ToUpper(sf.Name[:1]) + sf.Name[1:]
		f := Field{Name: name}
		switch {
		case value.Type() == nodeType:
			f.Type = "node"
			f.IsNull = value.IsNil()
			if !f.IsNull {
				f.NodeID = ptr(e.node(readable(value).Interface().(*ast.Node)))
			}
		case value.Type() == listType:
			f.Type = "list"
			f.IsNull = value.IsNil()
			if !f.IsNull {
				f.List = e.list(readable(value).Interface().(*ast.NodeList), nil)
			}
		case value.Type() == modifiersType:
			f.Type = "list"
			f.IsNull = value.IsNil()
			if !f.IsNull {
				list := readable(value).Interface().(*ast.ModifierList)
				f.List = e.list(&list.NodeList, ptr(uint32(list.ModifierFlags)))
			}
		case value.Kind() == reflect.String:
			f.Type = "bytes"
			f.Bytes = ptr(bytes(value.String()))
		case value.Kind() == reflect.Bool:
			f.Type = "bool"
			f.Boolean = ptr(value.Bool())
		case value.Kind() >= reflect.Int && value.Kind() <= reflect.Int64:
			f.Type = "number"
			f.Number = ptr(value.Int())
		case value.Kind() >= reflect.Uint && value.Kind() <= reflect.Uint64:
			f.Type = "number"
			f.Number = ptr(int64(value.Uint()))
		case value.Kind() == reflect.Slice && value.Type().Elem() == nodeType:
			f.Type = "nodes"
			f.IsNull = value.IsNil()
			if !f.IsNull {
				ids := []int{}
				for j := 0; j < value.Len(); j++ {
					ids = append(ids, e.node(readable(value.Index(j)).Interface().(*ast.Node)))
				}
				f.Nodes = &ids
			}
		case value.Kind() == reflect.Slice && value.Type().Elem().Kind() == reflect.String:
			f.Type = "strings"
			f.IsNull = value.IsNil()
			if !f.IsNull {
				parts := []string{}
				for j := 0; j < value.Len(); j++ {
					parts = append(parts, bytes(value.Index(j).String()))
				}
				f.Strings = &parts
			}
		default:
			panic(fmt.Sprintf("Unclassified payload field %s.%s (%s)", t.Name(), sf.Name, value.Type()))
		}
		n.Fields = append(n.Fields, f)
	}
}
func (e *encoder) list(l *ast.NodeList, modifiers *uint32) *List {
	result := &List{Pos: l.Loc.Pos(), End: l.Loc.End(), NodesNull: l.Nodes == nil, Missing: cap(l.Nodes) == 1 && unsafe.SliceData(l.Nodes) == missingBacking, TrailingComma: l.HasTrailingComma(), ModifierFlags: modifiers, Items: []int{}}
	if e.foundation {
		id, ok := e.listIDs[l]
		if !ok {
			id = len(e.listIDs)
			e.listIDs[l] = id
		}
		result.Identity = ptr(id)
	}
	for _, item := range l.Nodes {
		result.Items = append(result.Items, e.node(item))
	}
	return result
}
func diagnostics(diags []*ast.Diagnostic) []Diagnostic {
	result := []Diagnostic{}
	for _, d := range diags {
		args := []string{}
		for _, s := range d.MessageArgs() {
			args = append(args, bytes(s))
		}
		result = append(result, Diagnostic{d.Code(), int32(d.Category()), d.Pos(), d.End(), string(d.MessageKey()), args, d.String(), diagnostics(d.RelatedInformation()), diagnostics(d.MessageChain())})
	}
	return result
}
func references(refs []*ast.FileReference) []map[string]any {
	r := []map[string]any{}
	for _, f := range refs {
		r = append(r, map[string]any{"pos": f.Pos(), "end": f.End(), "fileNameBase64": bytes(f.FileName), "resolutionMode": f.ResolutionMode, "preserve": f.Preserve})
	}
	return r
}
func run(r Request) Response {
	text := r.Text
	if r.TextBytesBase64 != nil {
		b, err := base64.StdEncoding.DecodeString(*r.TextBytesBase64)
		if err != nil {
			panic(err)
		}
		text = string(b)
	}
	if r.ScriptKind == 0 {
		r.ScriptKind = core.ScriptKindTS
	}
	switch r.ScriptKind {
	case core.ScriptKindJS, core.ScriptKindJSX, core.ScriptKindTS, core.ScriptKindTSX, core.ScriptKindJSON:
	default:
		panic("Unsupported ScriptKind")
	}
	if r.FileName == "" {
		extensions := map[core.ScriptKind]string{1: ".js", 2: ".jsx", 3: ".ts", 4: ".tsx", 6: ".json"}
		r.FileName = "/index" + extensions[r.ScriptKind]
	}
	file := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: tspath.RootedFilePathFromNormalized(r.FileName), PathKey: tspath.PathKeyFromCanonical(r.FileName), ExternalModuleIndicatorOptions: ast.ExternalModuleIndicatorOptions{JSX: r.ExternalModuleJSX, Force: r.ExternalModuleForce}}, text, r.ScriptKind)
	e := encoder{file: file, resolveJSDoc: r.ResolveJSDoc, ids: map[*ast.Node]int{}, foundation: r.FoundationObservations, listIDs: map[*ast.NodeList]int{}}
	e.node(file.AsNode())
	indicator := func(n *ast.Node) any {
		if n == nil {
			return nil
		}
		return e.node(n)
	}
	metadata := map[string]any{"scriptKind": file.ScriptKind, "languageVariant": file.LanguageVariant, "isDeclarationFile": file.IsDeclarationFile, "nodeCount": file.NodeCount, "identifierCount": file.IdentifierCount, "textCount": file.TextCount, "externalModuleIndicator": indicator(file.ExternalModuleIndicator), "commonJSModuleIndicator": indicator(file.CommonJSModuleIndicator), "commentDirectives": []map[string]any{}, "referencedFiles": references(file.ReferencedFiles), "typeReferenceDirectives": references(file.TypeReferenceDirectives), "libReferenceDirectives": references(file.LibReferenceDirectives), "reparsedClones": []int{}}
	directives := []map[string]any{}
	for _, d := range file.CommentDirectives {
		directives = append(directives, map[string]any{"pos": d.Loc.Pos(), "end": d.Loc.End(), "kind": d.Kind})
	}
	metadata["commentDirectives"] = directives
	reparsed := []int{}
	for _, n := range file.ReparsedClones {
		reparsed = append(reparsed, e.node(n))
	}
	metadata["reparsedClones"] = reparsed
	imports := []int{}
	for _, n := range file.Imports() {
		imports = append(imports, e.node(n))
	}
	metadata["imports"] = imports
	augmentations := []int{}
	for _, n := range file.ModuleAugmentations {
		augmentations = append(augmentations, e.node(n))
	}
	metadata["moduleAugmentations"] = augmentations
	ambient := []string{}
	for _, s := range file.AmbientModuleNames {
		ambient = append(ambient, bytes(s))
	}
	metadata["ambientModuleNamesBase64"] = ambient
	metadata["usesUriStyleNodeCoreModules"] = int(file.UsesUriStyleNodeCoreModules)
	metadata["checkJsDirective"] = nil
	if d := file.CheckJsDirective; d != nil {
		metadata["checkJsDirective"] = map[string]any{"enabled": d.Enabled, "pos": d.Range.Pos(), "end": d.Range.End(), "kind": d.Range.Kind, "hasTrailingNewLine": d.Range.HasTrailingNewLine}
	}
	pragmas := []map[string]any{}
	for _, p := range file.Pragmas {
		args := map[string]any{}
		for name, a := range p.Args {
			args[name] = map[string]any{"pos": a.Pos(), "end": a.End(), "valueBase64": bytes(a.Value)}
		}
		pragmas = append(pragmas, map[string]any{"pos": p.Pos(), "end": p.End(), "kind": p.Kind, "hasTrailingNewLine": p.HasTrailingNewLine, "name": p.Name, "args": args})
	}
	metadata["pragmas"] = pragmas
	for i, n := range e.raw {
		if r.ResolveJSDoc {
			first, second := n.JSDoc(file), n.JSDoc(file)
			cold := e.resolvedDocs[n]
			if (cold == nil) != (first == nil) || !slices.Equal(cold, first) || (first == nil) != (second == nil) || !slices.Equal(first, second) {
				panic(fmt.Sprintf("Unstable warm JSDoc at canonical node %d", i))
			}
		}
		seen := map[*ast.Node]bool{}
		for ancestor := n; ancestor != nil; ancestor = ancestor.Parent {
			if seen[ancestor] {
				panic(fmt.Sprintf("Parent cycle at canonical node %d", i))
			}
			seen[ancestor] = true
		}
		if n.Parent != nil {
			parent := &Parent{Kind: n.Parent.Kind, Pos: n.Parent.Pos(), End: n.Parent.End()}
			if id, ok := e.ids[n.Parent]; ok {
				parent.ID = ptr(id)
			}
			e.output[i].Parent = parent
		}
	}
	// encoding/json sorts metadata map keys; all syntax/list/diagnostic ordering stays intact.
	probes := []TextProbe{}
	for _, id := range r.TextProbeIDs {
		if id < 0 || id >= len(e.raw) {
			panic("Invalid text probe canonical node ID")
		}
		n := e.raw[id]
		probes = append(probes, TextProbe{id, bytes(scanner.GetTextOfNode(n)), scanner.GetTokenPosOfNode(n, file, r.TextProbeIncludeJSDoc)})
	}
	var foundation *Foundation
	if r.FoundationObservations {
		foundation = e.foundationObservation()
	}
	return Response{e.output, diagnostics(file.Diagnostics()), diagnostics(file.JSDiagnostics()), diagnostics(file.JSDocDiagnostics()), metadata, probes, foundation}
}
func main() {
	if len(os.Args) == 2 && os.Args[1] == "--foundation-clones" {
		foundationCloneFixtures()
		return
	}
	if len(os.Args) == 3 && os.Args[1] == "--extract-foundation-files" {
		extractFoundationSources(os.Args[2])
		return
	}
	if len(os.Args) == 3 && os.Args[1] == "--extract-test-files" {
		out := json.NewEncoder(os.Stdout)
		for _, request := range extractTestFiles(os.Args[2]) {
			if err := out.Encode(request); err != nil {
				panic(err)
			}
		}
		return
	}
	if len(os.Args) == 3 && os.Args[1] == "--extract-fuzz" {
		out := json.NewEncoder(os.Stdout)
		for _, r := range extractFuzz(os.Args[2]) {
			if err := out.Encode(r); err != nil {
				panic(err)
			}
		}
		return
	}
	in := bufio.NewScanner(os.Stdin)
	in.Buffer(make([]byte, 65536), 32*1024*1024)
	out := json.NewEncoder(os.Stdout)
	for in.Scan() {
		var r Request
		if err := json.Unmarshal(in.Bytes(), &r); err != nil {
			panic(err)
		}
		if err := out.Encode(run(r)); err != nil {
			panic(err)
		}
	}
	if err := in.Err(); err != nil {
		panic(err)
	}
}
