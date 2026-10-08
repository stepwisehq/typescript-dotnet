// Authored read-only probes of original pinned syntax services.
package main

import (
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"os"
	"reflect"
	"sort"
	"strings"
)

func parse(s string, k core.ScriptKind) *ast.SourceFile {
	return parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: "/index.ts", PathKey: "/index.ts"}, s, k)
}
func caught(fn func()) (v bool) {
	defer func() {
		if recover() != nil {
			v = true
		}
	}()
	fn()
	return
}
func b(s string) string { return base64.StdEncoding.EncodeToString([]byte(s)) }
func snap(n *ast.Node) any {
	if n == nil {
		return nil
	}
	return []any{int(n.Kind), n.Pos(), n.End(), uint32(n.Flags)}
}
func nodes(a []*ast.Node) any {
	if a == nil {
		return nil
	}
	r := []any{}
	for _, n := range a {
		r = append(r, snap(n))
	}
	return r
}
func names() any {
	s := "/** @typedef {string} DocName */\nconst café='unused'; const caf\\u00e9=1; class C { #p=1; 'str-key'=2; 3=4; }"
	f := parse(s, core.ScriptKindJS)
	queries := []string{"café", "unused", "DocName", "#p", "str-key", "3", "absent"}
	has := []any{}
	for _, q := range queries {
		has = append(has, []any{b(q), f.HasIdentifier(q)})
	}
	t := f.GetNameTable()
	keys := []string{}
	for k := range t {
		keys = append(keys, k)
	}
	sort.Strings(keys)
	entries := []any{}
	for _, k := range keys {
		entries = append(entries, []any{b(k), t[k]})
	}
	return map[string]any{"source": s, "has": has, "table": entries, "repeatIdentity": reflect.ValueOf(t).UnsafePointer() == reflect.ValueOf(f.GetNameTable()).UnsafePointer()}
}
func tokens() any {
	s := "const x = 1_0; const y = 2;"
	f := parse(s, core.ScriptKindTS)
	p := f.Statements.Nodes[0]
	p2 := f.Statements.Nodes[1]
	pos := strings.Index(s, "1_0")
	a := f.GetOrCreateToken(ast.KindNumericLiteral, pos, pos+3, p, ast.TokenFlagsContainsSeparator)
	same := f.GetOrCreateToken(ast.KindNumericLiteral, pos, pos+3, p, 0)
	otherParent := f.GetOrCreateToken(ast.KindNumericLiteral, pos, pos+3, p2, 0)
	otherRange := f.GetOrCreateToken(ast.KindNumericLiteral, pos, pos+1, p, 0)
	p.Flags |= ast.NodeFlagsReparsed
	hit := f.GetOrCreateToken(ast.KindNumericLiteral, pos, pos+3, p, 0)
	return map[string]any{"same": a == same, "reparsedHit": a == hit, "otherParentDistinct": a != otherParent, "otherRangeDistinct": a != otherRange, "parentMatches": a.Parent == p, "rawText": b(a.Text()), "flags": int(a.AsNumericLiteral().TokenFlags), "kindMismatchThrows": caught(func() { f.GetOrCreateToken(ast.KindIdentifier, pos, pos+3, p, 0) }), "reparsedMissThrows": caught(func() { f.GetOrCreateToken(ast.KindNumericLiteral, pos, pos+2, p, 0) })}
}
func declarations() any {
	s := "import d, {a,b as B} from 'mod'; export {x,y as Y}; function f(a:number):void; function f(a:number){}; const named=function inner(){}; const {x, p:q}=obj; class C {constructor(public z:number){} p=1;}"
	f := parse(s, core.ScriptKindTS)
	t := f.GetDeclarationMap()
	keys := []string{}
	for k := range t {
		keys = append(keys, k)
	}
	sort.Strings(keys)
	r := []any{}
	for _, k := range keys {
		r = append(r, []any{b(k), nodes(t[k])})
	}
	return map[string]any{"source": s, "entries": r, "repeatIdentity": reflect.ValueOf(t).UnsafePointer() == reflect.ValueOf(f.GetDeclarationMap()).UnsafePointer(), "bound": f.IsBound()}
}
func decorators() any {
	r := []any{}
	for _, s := range []string{"class C{}", "export class C{}", "@d class C{}", "@d export class C{}"} {
		n := parse(s, core.ScriptKindTS).Statements.Nodes[0]
		d := n.Decorators()
		same := false
		if n.Modifiers() != nil && len(d) > 0 && len(n.Modifiers().Nodes) > 0 {
			same = &d[0] == &n.Modifiers().Nodes[0]
		}
		r = append(r, []any{s, nodes(d), same})
	}
	return r
}
func data() any {
	f := parse("", core.ScriptKindTS)
	g := parse("", core.ScriptKindTS)
	k := ast.NewSourceFileDataKey[*int]()
	k2 := ast.NewSourceFileDataKey[*int]()
	calls := 0
	v := 7
	first := f.GetOrComputeData(k, func(file *ast.SourceFile) *int {
		calls++
		if file != f {
			panic("wrong file")
		}
		return &v
	})
	repeat := f.GetOrComputeData(k, func(*ast.SourceFile) *int { calls++; return nil })
	second := f.GetOrComputeData(k2, func(*ast.SourceFile) *int { return nil })
	other := g.GetOrComputeData(k, func(*ast.SourceFile) *int { return nil })
	failed := ast.NewSourceFileDataKey[int]()
	failCalls := 0
	threw := caught(func() { f.GetOrComputeData(failed, func(*ast.SourceFile) int { failCalls++; panic("first compute") }) })
	after := f.GetOrComputeData(failed, func(*ast.SourceFile) int { failCalls++; return 9 })
	return map[string]any{"same": first == repeat, "calls": calls, "nilDifferentKey": second == nil, "nilDifferentFile": other == nil, "firstThrows": threw, "failureNoRetry": failCalls == 1, "failureDefault": after, "invalidKeyThrows": caught(func() { f.GetOrComputeData(&ast.SourceFileDataKey[int]{}, func(*ast.SourceFile) int { return 0 }) })}
}
func iterator() any {
	f := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	a := f.NewIdentifier("a")
	c := f.NewIdentifier("c")
	d := f.NewIdentifier("d")
	replacement := f.NewIdentifier("replacement")
	n := f.NewVariableDeclaration(a, nil, nil, c)
	seq := n.IterChildren()
	r := []string{}
	for child := range seq {
		r = append(r, child.Text())
		if len(r) == 1 {
			n.AsVariableDeclaration().Initializer = replacement
		}
	}
	list := f.NewNodeList([]*ast.Node{a, c})
	block := f.NewBlock(list, true)
	seen := []string{}
	for child := range block.IterChildren() {
		seen = append(seen, child.Text())
		if len(seen) == 1 {
			list.Nodes = append(list.Nodes, d)
		}
	}
	early := 0
	for range block.IterChildren() {
		early++
		break
	}
	return map[string]any{"laterChildReplacement": r, "appendCapturedLength": seen, "earlyStop": early}
}
func main() {
	r := map[string]any{"Sort": sortControls(), "Modes": modeControls(), "EntityNames": entityControls(), "Mutations": mutationControls(), "Diagnostics": diagnosticData(), "Factories": factories(), "Visitors": visitors(), "Accessors": accessors(), "Names": names(), "Tokens": tokens(), "Declarations": declarations(), "Decorators": decorators(), "Data": data(), "Iterator": iterator()}
	if err := json.NewEncoder(os.Stdout).Encode(r); err != nil {
		panic(fmt.Sprint(err))
	}
}
