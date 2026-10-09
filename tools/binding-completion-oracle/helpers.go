// Authored observations of the four exported pinned binder helper APIs.
// Only parsed legitimate AST nodes are used. High-ID writes are isolated API
// boundary controls, not naturally allocated process-counter claims.
package main

import (
	"encoding/base64"
	"reflect"
	"strconv"
	"sync/atomic"
	"unsafe"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/binder"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
)

func helperObservations() map[string]any {
	return map[string]any{
		"PrivateNames":      helperPrivateNames(),
		"StrictPrologues":   helperStrictPrologues(),
		"ValueDeclarations": helperValueDeclarations(),
		"ContainerFlags":    helperContainerFlags(),
	}
}

func helperPrivateNames() []any {
	rows := []any{}
	ids := []uint64{1, 1 << 31, 1<<63 - 1, 1 << 63, 1<<64 - 1}
	descriptions := []string{"#p", "#é", string([]byte{'#', 0xff})}
	for _, id := range ids {
		for _, description := range descriptions {
			file := supportParse("class C {}")
			binder.BindSourceFile(file)
			symbol := file.Statements.Nodes[0].Symbol()
			// ast.Symbol.id is intentionally private. Use its real atomic cell;
			// this changes no source algorithm, owner or synthetic AST payload.
			cell := reflect.ValueOf(symbol).Elem().FieldByName("id")
			(*atomic.Uint64)(unsafe.Pointer(cell.UnsafeAddr())).Store(id)
			name := binder.GetSymbolNameForPrivateIdentifier(symbol, description)
			rows = append(rows, map[string]any{
				"idBits": strconv.FormatUint(id, 10), "descriptionBase64": base64.StdEncoding.EncodeToString([]byte(description)),
				"nameBase64": base64.StdEncoding.EncodeToString([]byte(name)), "repeatSame": binder.GetSymbolNameForPrivateIdentifier(symbol, description) == name,
			})
		}
	}
	return rows
}

func helperStrictPrologues() []any {
	inputs := []struct {
		text         string
		functionBody bool
	}{
		{"", false},
		{"'use strict'; x;", false},
		{"\"other\"; /*before*/ 'use strict';", false},
		{"'use\\u0020strict';", false},
		{"'use\\x20strict';", false},
		{"'use strict' + 'x'; 'use strict';", false},
		{"0; 'use strict';", false},
		{"\"other\"; ' use strict '; 'use strict';", false},
		{"`use strict`; 'use strict';", false},
		{"function f(){\"other\"; 'use strict'; return 1;}", true},
		{"function f(){return 1; 'use strict';}", true},
		{"/*leading*/ \"use strict\"; 'use strict';", false},
	}
	rows := []any{}
	for _, input := range inputs {
		file := supportParse(input.text)
		statements := file.Statements.Nodes
		if input.functionBody {
			statements = statements[0].Body().Statements()
		}
		found := binder.FindUseStrictPrologue(file, statements)
		index := -1
		var pos, end *int
		if found != nil {
			for i, node := range statements {
				if found == node {
					index = i
					break
				}
			}
			start, finish := int(found.Pos()), int(found.End())
			pos, end = &start, &finish
		}
		rows = append(rows, map[string]any{"textBase64": supportBytes(input.text), "functionBody": input.functionBody, "index": index, "pos": pos, "end": end})
	}
	return rows
}

const helperValueSource = "namespace A {} namespace B {} function f(){} var x=1; x.p=2; Object.defineProperty(x,'p',{value:3}); x; x.p;"

func helperValueDeclarations() map[string]any {
	file := supportParse(helperValueSource)
	statements := file.Statements.Nodes
	nodes := map[string]*ast.Node{
		"moduleA": statements[0], "moduleB": statements[1], "function": statements[2],
		"variable": statements[3].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0],
		"binary":   statements[4].Expression(), "call": statements[5].Expression(),
		"identifier": statements[6].Expression(), "access": statements[7].Expression(),
	}
	steps := [][]string{
		{"variable", "function"}, {"binary", "variable", "binary"}, {"call", "function"},
		{"identifier", "access"}, {"moduleA", "function"}, {"moduleA", "moduleB"},
		{"moduleA", "identifier"}, {"function", "moduleA"}, {"access", "call", "variable"},
	}
	rows := []any{}
	for _, sequence := range steps {
		symbol := &ast.Symbol{}
		selected := []string{}
		for _, label := range sequence {
			binder.SetValueDeclaration(symbol, nodes[label])
			identity := ""
			for name, node := range nodes {
				if symbol.ValueDeclaration == node {
					identity = name
					break
				}
			}
			if identity == "" {
				panic("Selected value declaration is outside the legitimate parsed nodes")
			}
			selected = append(selected, identity)
		}
		rows = append(rows, map[string]any{"steps": sequence, "selected": selected})
	}
	return map[string]any{"textBase64": supportBytes(helperValueSource), "rows": rows}
}

func helperContainerFlags() []any {
	inputs := []struct {
		text string
		kind core.ScriptKind
	}{
		{`class C {
 p; q=1; constructor() {} get g(){return 1;} set g(v){} m(){} static {}
}
const CE=class { m(){} get g(){return 1;} set g(v){} };
const O={m(){},get p(){return 1;},set p(v){}};
enum E {a} interface I {m():void;(x:number):void;new():I;[k:string]:number;}
type T={p:number}; type F=(x:number)=>number; type CT=new()=>C;
type M<T>={[K in keyof T]:T[K]}; namespace N { export let x=1; }
function f(){{let x;} return 1;} const fe=function(){}; const ar=()=>{};
try{}catch(e){} for(;;){} for(let x in O){} for(let x of []){}
switch(x){case 1:break;default:break;}`, core.ScriptKindTS},
		{`const v=<C x={1} {...other}/>;`, core.ScriptKindTSX},
		{`/** @typedef {{p:number}} T */
var x;`, core.ScriptKindJS},
	}
	rows := []any{}
	for _, input := range inputs {
		file := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: "/same.ts", PathKey: "/same.ts"}, input.text, input.kind)
		nodes := []any{}
		var visit func(*ast.Node) bool
		visit = func(node *ast.Node) bool {
			var parentKind *int
			if node.Parent != nil {
				k := int(node.Parent.Kind)
				parentKind = &k
			}
			nodes = append(nodes, map[string]any{"kind": int(node.Kind), "pos": node.Pos(), "end": node.End(), "parentKind": parentKind, "flags": int(binder.GetContainerFlags(node))})
			node.ForEachChild(visit)
			return false
		}
		visit(file.AsNode())
		rows = append(rows, map[string]any{"textBase64": supportBytes(input.text), "scriptKind": int(input.kind), "nodes": nodes})
	}
	return rows
}
