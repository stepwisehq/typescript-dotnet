// Independent read-only graph oracle for the pinned source-file binder.
package main

import (
	"bufio"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/binder"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"os"
	"reflect"
	"slices"
	"strconv"
	"strings"
	"sync"
	"sync/atomic"
	"unsafe"
)

var lastSyntaxEncoder *encoder

type BoundField struct {
	Name string `json:"name"`
	Type string `json:"type"`
	Ref  *int   `json:"ref"`
}
type BoundNode struct {
	NodeID int          `json:"nodeId"`
	Fields []BoundField `json:"fields"`
}
type Name struct {
	Kind        string  `json:"kind"`
	Bytes       *string `json:"bytesBase64,omitempty"`
	Prefix      *string `json:"prefixBase64,omitempty"`
	Suffix      *string `json:"suffixBase64,omitempty"`
	OwnerSymbol *int    `json:"ownerSymbolId,omitempty"`
	OwnerNode   *int    `json:"ownerNodeId,omitempty"`
}
type Symbol struct {
	ID               int    `json:"id"`
	Flags            uint32 `json:"flags"`
	Name             Name   `json:"name"`
	DeclarationsNull bool   `json:"declarationsNull"`
	Declarations     []int  `json:"declarations"`
	ValueDeclaration *int   `json:"valueDeclaration"`
	Parent           *int   `json:"parent"`
	ExportSymbol     *int   `json:"exportSymbol"`
	Members          *int   `json:"members"`
	Exports          *int   `json:"exports"`
}
type Entry struct {
	Key      Name `json:"key"`
	SymbolID int  `json:"symbolId"`
}
type Table struct {
	ID      int     `json:"id"`
	Entries []Entry `json:"entries"`
}
type Flow struct {
	ID          int    `json:"id"`
	Flags       uint32 `json:"flags"`
	Node        *int   `json:"node"`
	Antecedent  *int   `json:"antecedent"`
	Antecedents *int   `json:"antecedents"`
}
type FlowList struct {
	ID   int  `json:"id"`
	Flow *int `json:"flow"`
	Next *int `json:"next"`
}
type Pattern struct {
	Text      string `json:"textBase64"`
	StarIndex int    `json:"starIndex"`
	Symbol    int    `json:"symbolId"`
}
type Binding struct {
	Nodes         []BoundNode  `json:"nodes"`
	Symbols       []*Symbol    `json:"symbols"`
	Tables        []*Table     `json:"tables"`
	Flows         []*Flow      `json:"flows"`
	FlowLists     []*FlowList  `json:"flowLists"`
	Diagnostics   []Diagnostic `json:"diagnostics"`
	IsBound       bool         `json:"isBound"`
	SymbolCount   int          `json:"symbolCount"`
	GlobalExports *int         `json:"globalExports"`
	PatternsNull  bool         `json:"patternsNull"`
	Patterns      []Pattern    `json:"patterns"`
}
type Result struct {
	Before     Response `json:"before"`
	After      Response `json:"after"`
	Binding    Binding  `json:"binding"`
	Idempotent bool     `json:"idempotent"`
}
type graph struct {
	syntax    *encoder
	output    Binding
	symbolIDs map[*ast.Symbol]int
	tableIDs  map[unsafe.Pointer]int
	flowIDs   map[*ast.FlowNode]int
	listIDs   map[*ast.FlowList]int
}

func newGraph(e *encoder) *graph {
	return &graph{syntax: e, output: Binding{Nodes: []BoundNode{}, Symbols: []*Symbol{}, Tables: []*Table{}, Flows: []*Flow{}, FlowLists: []*FlowList{}, Patterns: []Pattern{}}, symbolIDs: map[*ast.Symbol]int{}, tableIDs: map[unsafe.Pointer]int{}, flowIDs: map[*ast.FlowNode]int{}, listIDs: map[*ast.FlowList]int{}}
}
func (g *graph) node(n *ast.Node) *int {
	if n == nil {
		return nil
	}
	return ptr(g.syntax.node(n))
}
func (g *graph) name(name string, s *ast.Symbol) Name {
	for _, decl := range s.Declarations {
		if n := decl.Name(); n != nil && n.Kind == ast.KindPrivateIdentifier && strings.HasPrefix(name, "\xfe#") {
			owner := s.Parent
			if owner == nil {
				panic("Private symbol has no parent")
			}
			prefix := "\xfe#"
			suffix := "@" + n.Text()
			want := prefix + strconv.FormatUint(uint64(ast.GetSymbolId(owner)), 10) + suffix
			if name != want {
				panic(fmt.Sprintf("Private name owner mismatch: %q != %q", name, want))
			}
			return Name{Kind: "private", Prefix: ptr(bytes(prefix)), Suffix: ptr(bytes(suffix)), OwnerSymbol: g.symbol(owner)}
		}
		if ast.IsAmbientModule(decl) {
			module := decl.AsModuleDeclaration()
			if module.Attributes == nil {
				continue
			}
			pattern := core.TryParsePattern(module.Name().Text())
			if !pattern.IsValid() || pattern.StarIndex < 0 {
				continue
			}
			prefix := "\xfe\"" + pattern.Text + "\"pattern@"
			want := prefix + strconv.FormatUint(uint64(ast.GetNodeId(module.Attributes)), 10)
			if name != want {
				panic("Pattern name owner mismatch")
			}
			return Name{Kind: "pattern", Prefix: ptr(bytes(prefix)), OwnerNode: g.node(module.Attributes)}
		}
	}
	return Name{Kind: "raw", Bytes: ptr(bytes(name))}
}
func (g *graph) symbol(s *ast.Symbol) *int {
	if s == nil {
		return nil
	}
	if id, ok := g.symbolIDs[s]; ok {
		return ptr(id)
	}
	if s.CheckFlags != 0 {
		panic("Unexpected checker-owned symbol flags")
	}
	id := len(g.output.Symbols)
	g.symbolIDs[s] = id
	out := &Symbol{ID: id, Flags: uint32(s.Flags), DeclarationsNull: s.Declarations == nil, Declarations: []int{}}
	g.output.Symbols = append(g.output.Symbols, out)
	out.Name = g.name(s.Name, s)
	for _, decl := range s.Declarations {
		out.Declarations = append(out.Declarations, *g.node(decl))
	}
	out.ValueDeclaration = g.node(s.ValueDeclaration)
	out.Parent = g.symbol(s.Parent)
	out.ExportSymbol = g.symbol(s.ExportSymbol)
	out.Members = g.table(s.Members)
	out.Exports = g.table(s.Exports)
	return ptr(id)
}

// Tables are unordered. Sort generated opaque keys by the existing syntax
// owner's identity, so process-global counter digit boundaries cannot reorder
// entries. Ordinary raw byte keys keep their exact lexical order.
func (g *graph) sortKey(name string, s *ast.Symbol) string {
	for _, decl := range s.Declarations {
		if n := decl.Name(); n != nil && n.Kind == ast.KindPrivateIdentifier && strings.HasPrefix(name, "\xfe#") {
			if s.Parent == nil || len(s.Parent.Declarations) == 0 {
				panic("Private sorting owner missing")
			}
			id, ok := g.syntax.ids[s.Parent.Declarations[0]]
			if !ok {
				panic("Private sorting owner outside syntax")
			}
			return "\xfe#" + fmt.Sprintf("%020d", id) + "@" + n.Text()
		}
		if ast.IsAmbientModule(decl) {
			m := decl.AsModuleDeclaration()
			if m.Attributes == nil {
				continue
			}
			pattern := core.TryParsePattern(m.Name().Text())
			if !pattern.IsValid() || pattern.StarIndex < 0 {
				continue
			}
			id, ok := g.syntax.ids[m.Attributes]
			if !ok {
				panic("Pattern sorting owner outside syntax")
			}
			return "\xfe\"" + pattern.Text + "\"pattern@" + fmt.Sprintf("%020d", id)
		}
	}
	return name
}
func (g *graph) table(t ast.SymbolTable) *int {
	if t == nil {
		return nil
	}
	pointer := reflect.ValueOf(t).UnsafePointer()
	if id, ok := g.tableIDs[pointer]; ok {
		return ptr(id)
	}
	id := len(g.output.Tables)
	g.tableIDs[pointer] = id
	out := &Table{ID: id, Entries: []Entry{}}
	g.output.Tables = append(g.output.Tables, out)
	keys := []string{}
	for key := range t {
		keys = append(keys, key)
	}
	slices.SortFunc(keys, func(a, b string) int { return strings.Compare(g.sortKey(a, t[a]), g.sortKey(b, t[b])) })
	for _, key := range keys {
		symbol := t[key]
		if symbol == nil {
			panic("Nil symbol table entry")
		}
		out.Entries = append(out.Entries, Entry{g.name(key, symbol), *g.symbol(symbol)})
	}
	return ptr(id)
}
func (g *graph) flow(n *ast.FlowNode) *int {
	if n == nil {
		return nil
	}
	if id, ok := g.flowIDs[n]; ok {
		return ptr(id)
	}
	id := len(g.output.Flows)
	g.flowIDs[n] = id
	out := &Flow{ID: id, Flags: uint32(n.Flags)}
	g.output.Flows = append(g.output.Flows, out)
	out.Node = g.node(n.Node)
	out.Antecedent = g.flow(n.Antecedent)
	out.Antecedents = g.list(n.Antecedents)
	return ptr(id)
}
func (g *graph) list(l *ast.FlowList) *int {
	if l == nil {
		return nil
	}
	if id, ok := g.listIDs[l]; ok {
		return ptr(id)
	}
	id := len(g.output.FlowLists)
	g.listIDs[l] = id
	out := &FlowList{ID: id}
	g.output.FlowLists = append(g.output.FlowLists, out)
	out.Flow = g.flow(l.Flow)
	out.Next = g.list(l.Next)
	return ptr(id)
}
func (g *graph) fields(v reflect.Value, out *BoundNode) {
	t := v.Type()
	for i := 0; i < v.NumField(); i++ {
		sf := t.Field(i)
		value := v.Field(i)
		if sf.Anonymous {
			if sf.Type.Name() == "NodeBase" || sf.Type.Name() == "NodeDefault" || sf.Type.Name() == "CompositeBase" {
				continue
			}
			g.fields(value, out)
			continue
		}
		if t.Name() == "SourceFile" {
			continue
		}
		field := BoundField{Name: strings.ToUpper(sf.Name[:1]) + sf.Name[1:]}
		switch value.Type() {
		case reflect.TypeFor[*ast.Symbol]():
			field.Type = "symbol"
			field.Ref = g.symbol(readable(value).Interface().(*ast.Symbol))
		case reflect.TypeFor[ast.SymbolTable]():
			field.Type = "table"
			field.Ref = g.table(readable(value).Interface().(ast.SymbolTable))
		case reflect.TypeFor[*ast.FlowNode]():
			field.Type = "flow"
			field.Ref = g.flow(readable(value).Interface().(*ast.FlowNode))
		case reflect.TypeFor[*ast.FlowList]():
			field.Type = "flowList"
			field.Ref = g.list(readable(value).Interface().(*ast.FlowList))
		default:
			if sf.Name != "NextContainer" {
				continue
			}
			if value.Type() != nodeType {
				panic("Unknown NextContainer type")
			}
			field.Type = "node"
			field.Ref = g.node(readable(value).Interface().(*ast.Node))
		}
		out.Fields = append(out.Fields, field)
	}
}
func validateDiagnostics(items []*ast.Diagnostic, file *ast.SourceFile) {
	for _, d := range items {
		if d.ReportsUnnecessary() || d.ReportsDeprecated() || d.Source() != "" || d.SkippedOnNoEmit() || d.RepopulateInfo() != nil || (d.File() != nil && d.File() != file) {
			panic("Unexpected binder diagnostic metadata outside canonical schema")
		}
		validateDiagnostics(d.RelatedInformation(), file)
		validateDiagnostics(d.MessageChain(), file)
	}
}
func (g *graph) encode(file *ast.SourceFile) Binding {
	for i := 0; i < len(g.syntax.raw); i++ {
		n := g.syntax.raw[i]
		out := BoundNode{NodeID: i, Fields: []BoundField{}}
		g.fields(readable(reflect.ValueOf(n).Elem().FieldByName("data")).Elem().Elem(), &out)
		g.output.Nodes = append(g.output.Nodes, out)
	}
	g.output.GlobalExports = g.table(file.GlobalExports)
	g.output.PatternsNull = file.PatternAmbientModules == nil
	for _, p := range file.PatternAmbientModules {
		g.output.Patterns = append(g.output.Patterns, Pattern{bytes(p.Pattern.Text), p.Pattern.StarIndex, *g.symbol(p.Symbol)})
	}
	// Metadata may discover nodes not reached through syntax (pattern attributes).
	for len(g.output.Nodes) < len(g.syntax.raw) {
		i := len(g.output.Nodes)
		out := BoundNode{NodeID: i, Fields: []BoundField{}}
		g.fields(readable(reflect.ValueOf(g.syntax.raw[i]).Elem().FieldByName("data")).Elem().Elem(), &out)
		g.output.Nodes = append(g.output.Nodes, out)
	}
	validateDiagnostics(file.BindDiagnostics(), file)
	g.output.Diagnostics = diagnostics(file.BindDiagnostics())
	g.output.IsBound = file.IsBound()
	g.output.SymbolCount = file.SymbolCount
	for i, n := range g.syntax.raw {
		if n.Parent != nil {
			p := &Parent{Kind: n.Parent.Kind, Pos: n.Parent.Pos(), End: n.Parent.End()}
			if id, ok := g.syntax.ids[n.Parent]; ok {
				p.ID = ptr(id)
			}
			g.syntax.output[i].Parent = p
		}
	}
	return g.output
}
func parseFile(r Request) *ast.SourceFile {
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
	extensions := map[core.ScriptKind]string{1: ".js", 2: ".jsx", 3: ".ts", 4: ".tsx", 6: ".json"}
	ext, ok := extensions[r.ScriptKind]
	if !ok {
		panic("Unsupported ScriptKind")
	}
	if r.FileName == "" {
		r.FileName = "/index" + ext
	}
	return parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: tspath.RootedFilePathFromNormalized(r.FileName), PathKey: tspath.PathKeyFromCanonical(r.FileName), ExternalModuleIndicatorOptions: ast.ExternalModuleIndicatorOptions{JSX: r.ExternalModuleJSX, Force: r.ExternalModuleForce}}, text, r.ScriptKind)
}
func assertSameIDs[K comparable](first, second map[K]int) {
	if len(first) != len(second) {
		panic("Repeated binding replaced graph identities")
	}
	for key, id := range first {
		if next, ok := second[key]; !ok || id != next {
			panic("Repeated binding replaced graph identities")
		}
	}
}
func runBinder(r Request) Result {
	file := parseFile(r)
	before := runParser(r, file)
	binder.BindSourceFile(file)
	after := runParser(r, file)
	graph := newGraph(lastSyntaxEncoder)
	binding := graph.encode(file)
	after.Nodes = graph.syntax.output
	first, err := json.Marshal(struct {
		After   Response
		Binding Binding
	}{after, binding})
	if err != nil {
		panic(err)
	}
	binder.BindSourceFile(file)
	secondAfter := runParser(r, file)
	secondGraph := newGraph(lastSyntaxEncoder)
	secondBinding := secondGraph.encode(file)
	secondAfter.Nodes = secondGraph.syntax.output
	assertSameIDs(graph.syntax.ids, secondGraph.syntax.ids)
	assertSameIDs(graph.symbolIDs, secondGraph.symbolIDs)
	assertSameIDs(graph.tableIDs, secondGraph.tableIDs)
	assertSameIDs(graph.flowIDs, secondGraph.flowIDs)
	assertSameIDs(graph.listIDs, secondGraph.listIDs)
	second, err := json.Marshal(struct {
		After   Response
		Binding Binding
	}{secondAfter, secondBinding})
	if err != nil {
		panic(err)
	}
	if string(first) != string(second) {
		panic("Repeated binding changed published graph")
	}
	return Result{before, after, binding, true}
}
func selfTests() {
	file := parser.ParseSourceFile(ast.SourceFileParseOptions{}, "", core.ScriptKindTS)
	entered, release, finished := make(chan struct{}), make(chan struct{}), make(chan struct{})
	var attempts atomic.Int32
	go func() {
		file.BindOnce(func() { attempts.Add(1); close(entered); <-release; file.SymbolCount = 123 })
		close(finished)
	}()
	<-entered
	if file.IsBound() {
		panic("Early publication")
	}
	var wait sync.WaitGroup
	for range 6 {
		wait.Go(func() { file.BindOnce(func() { attempts.Add(1) }) })
	}
	close(release)
	wait.Wait()
	<-finished
	if !file.IsBound() || attempts.Load() != 1 || file.SymbolCount != 123 {
		panic("Concurrent BindOnce publication differs")
	}
	failed := parser.ParseSourceFile(ast.SourceFileParseOptions{}, "", core.ScriptKindTS)
	failures := 0
	func() {
		defer func() {
			if recover() != "deliberate failure" {
				panic("Unexpected callback failure")
			}
		}()
		failed.BindOnce(func() { failures++; panic("deliberate failure") })
	}()
	failed.BindOnce(func() { failures++ })
	if failed.IsBound() || failures != 1 {
		panic("Failed BindOnce retried or published")
	}
	module := parser.ParseSourceFile(ast.SourceFileParseOptions{}, "export const x=1;", core.ScriptKindTS)
	for range 8 {
		wait.Go(func() { binder.BindSourceFile(module) })
	}
	wait.Wait()
	if !module.IsBound() || module.Symbol.Name != "\"\"" || module.Symbol.Exports["x"] == nil {
		panic("Default options or concurrent Binder differs")
	}
	symbol, exports, declaration := module.Symbol, reflect.ValueOf(module.Symbol.Exports).UnsafePointer(), module.Symbol.Exports["x"]
	binder.BindSourceFile(module)
	if module.Symbol != symbol || reflect.ValueOf(module.Symbol.Exports).UnsafePointer() != exports || module.Symbol.Exports["x"] != declaration {
		panic("Repeated Binder replaced identities")
	}
	for _, name := range []string{"concurrent-bind-once-publication", "failed-bind-once-no-retry", "default-options-module-name", "concurrent-binder-idempotent-identities"} {
		fmt.Println(name)
	}
}
func main() {
	if len(os.Args) == 2 && os.Args[1] == "--self-test" {
		selfTests()
		return
	}
	if len(os.Args) != 1 {
		panic("Expected no arguments or --self-test")
	}
	in := bufio.NewScanner(os.Stdin)
	in.Buffer(make([]byte, 65536), 32*1024*1024)
	out := json.NewEncoder(os.Stdout)
	for in.Scan() {
		var r Request
		if err := json.Unmarshal(in.Bytes(), &r); err != nil {
			panic(err)
		}
		if err := out.Encode(runBinder(r)); err != nil {
			panic(err)
		}
	}
	if err := in.Err(); err != nil {
		panic(err)
	}
}
