package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
)

func modeControls() any {
	r := []any{}
	for _, k := range []core.ScriptKind{0, 1, 2, 3, 4, 5, 6, 7, 999, -1} {
		var f *ast.SourceFile
		threw := caught(func() { f = parse("const x=1", k) })
		var data any
		if f != nil {
			data = []any{int(f.ScriptKind), int(f.LanguageVariant), snap(f.AsNode()), len(f.Diagnostics())}
		}
		r = append(r, []any{int(k), threw, data})
	}
	return r
}
func entityGraph(n *ast.Node) any {
	if n == nil {
		return nil
	}
	children := []any{}
	n.ForEachChild(func(c *ast.Node) bool { children = append(children, entityGraph(c)); return false })
	text := ""
	if n.Kind == ast.KindIdentifier {
		text = n.Text()
	}
	return []any{snap(n), b(text), children}
}
func entityControls() any {
	r := []any{}
	for _, s := range []string{"A", "A.B.C", " café . 名 ", "A.default", "A..B", "a()", "", "A<B>", "\\u0041.B"} {
		r = append(r, []any{s, entityGraph(parser.ParseIsolatedEntityName(s))})
	}
	return r
}
func mutationControls() any {
	f := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	a := f.NewIdentifier("a")
	b := f.NewIdentifier("b")
	c := f.NewIdentifier("c")
	rows := []any{}
	for _, mode := range []string{"ForEachChild", "VisitSliceSame", "VisitSliceChanged", "SyntaxList"} {
		caller := []*ast.Node{a, b}
		seen := []string{}
		var result []*ast.Node
		switch mode {
		case "ForEachChild":
			list := f.NewNodeList(caller)
			block := f.NewBlock(list, true)
			block.ForEachChild(func(n *ast.Node) bool {
				seen = append(seen, n.Text())
				if len(seen) == 1 {
					list.Nodes = append(list.Nodes, c)
				}
				return false
			})
			caller = list.Nodes
			result = caller
		case "VisitSliceSame", "VisitSliceChanged":
			v := ast.NewNodeVisitor(func(n *ast.Node) *ast.Node {
				seen = append(seen, n.Text())
				if len(seen) == 1 {
					caller = append(caller, c)
				}
				if mode == "VisitSliceChanged" && n == a {
					return nil
				}
				return n
			}, f, ast.NodeVisitorHooks{})
			result, _ = v.VisitSlice(caller)
		case "SyntaxList":
			list := f.NewSyntaxList(caller)
			v := ast.NewNodeVisitor(func(n *ast.Node) *ast.Node {
				seen = append(seen, n.Text())
				if len(seen) == 1 {
					list.AsSyntaxList().Children = append(list.AsSyntaxList().Children, c)
				}
				return n
			}, f, ast.NodeVisitorHooks{})
			r := list.VisitEachChild(v)
			caller = list.AsSyntaxList().Children
			result = r.AsSyntaxList().Children
		}
		rows = append(rows, []any{mode, seen, nodes(result), nodes(caller), result == nil})
	}
	return rows
}
