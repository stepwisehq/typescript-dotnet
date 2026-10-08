// Authored original-Go full syntax graph probes of deep clone behavior.
package main

import (
	"encoding/json"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"os"
)

func cloneGraph(file *ast.SourceFile, root *ast.Node) []*Node {
	e := &encoder{file: file, ids: map[*ast.Node]int{}, output: []*Node{}, foundation: true, listIDs: map[*ast.NodeList]int{}}
	e.node(root)
	for i, n := range e.raw {
		if n.Parent != nil {
			p := &Parent{Kind: n.Parent.Kind, Pos: n.Parent.Pos(), End: n.Parent.End()}
			if id, ok := e.ids[n.Parent]; ok {
				p.ID = ptr(id)
			}
			e.output[i].Parent = p
		}
	}
	return e.output
}
func foundationCloneFixtures() {
	out := []map[string]any{}
	for _, row := range []struct {
		name, source string
		kind         core.ScriptKind
		doc          bool
	}{
		{"MixedSyntax", "import x from 'm'; @d export class Café<T> extends B {readonly p: string='é'; m(a=1){return `${a}`}}; enum E{A=1,B};", core.ScriptKindTS, false},
		{"JSX", "const x=<A p={1}>{/*x*/}<B/>text</A>;", core.ScriptKindTSX, false},
		{"LazyJSDoc", "/** @typedef {{é: string, p?: number}} N */\n/** @type {N} */\nconst x={é:'a'};", core.ScriptKindJS, true},
		{"AwaitReparse", "/** docs */ const p=await x; const q=async()=>await y;", core.ScriptKindTS, false},
	} {
		file := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: "/index.ts", PathKey: "/index.ts"}, row.source, row.kind)
		roots := []*ast.Node{file.AsNode()}
		if row.doc {
			file.ForEachChild(func(n *ast.Node) bool { roots = append(roots, n.JSDoc(file)...); return false })
		}
		for i, root := range roots {
			for _, mode := range []string{"deep", "reparse", "shallow"} {
				f := ast.NewNodeFactory(ast.NodeFactoryHooks{})
				var clone *ast.Node
				switch mode {
				case "deep":
					clone = f.DeepCloneNode(root)
				case "reparse":
					clone = f.DeepCloneReparse(root)
				case "shallow":
					clone = root.Clone(f)
				}
				out = append(out, map[string]any{"cloneMode": mode, "name": row.name, "rootIndex": i, "source": row.source, "scriptKind": row.kind, "original": cloneGraph(file, root), "clone": cloneGraph(file, clone), "distinct": root != clone})
			}
		}
	}
	if err := json.NewEncoder(os.Stdout).Encode(out); err != nil {
		panic(err)
	}
}
