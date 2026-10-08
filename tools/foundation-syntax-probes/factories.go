package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"unsafe"
)

func factories() any {
	events := []any{}
	f := ast.NewNodeFactory(ast.NodeFactoryHooks{OnCreate: func(n *ast.Node) { events = append(events, []any{"create", snap(n)}) }, OnUpdate: func(n, o *ast.Node) { events = append(events, []any{"update", snap(n), snap(o)}) }, OnClone: func(n, o *ast.Node) { events = append(events, []any{"clone", snap(n), snap(o)}) }})
	name := f.NewIdentifier("alpha")
	number := f.NewNumericLiteral("12", ast.TokenFlagsNone)
	original := f.NewVariableDeclaration(name, nil, nil, number)
	original.Loc = core.NewTextRange(10, 20)
	original.Flags = ast.NodeFlagsAwaitContext
	original.Parent = parse("", core.ScriptKindTS).AsNode()
	events = []any{}
	unchanged := f.UpdateVariableDeclaration(original.AsVariableDeclaration(), name, nil, nil, number)
	replacement := f.NewNumericLiteral("20", 0)
	changed := f.UpdateVariableDeclaration(original.AsVariableDeclaration(), name, nil, nil, replacement)
	clone := original.Clone(f)
	lists := []any{}
	for _, a := range [][]*ast.Node{nil, {}, {name, number}} {
		list := f.NewNodeList(a)
		list.Loc = core.NewTextRange(4, 8)
		copy := list.Clone(f)
		lists = append(lists, []any{list != copy, list.Nodes == nil, copy.Nodes == nil, unsafe.SliceData(list.Nodes) == unsafe.SliceData(copy.Nodes), copy.Pos(), copy.End(), nodes(copy.Nodes)})
	}
	return map[string]any{"unchangedIdentity": unchanged == original, "changedIdentity": changed != original, "changedParentNil": changed.Parent == nil, "changedInitializer": changed.Initializer() == replacement, "cloneParentNil": clone.Parent == nil, "cloneSharedName": clone.Name() == name, "events": events, "lists": lists, "nodeCount": f.NodeCount(), "textCount": f.TextCount()}
}
func visitors() any {
	f := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	a := f.NewIdentifier("a")
	b := f.NewIdentifier("b")
	c := f.NewIdentifier("c")
	d := f.NewIdentifier("d")
	list := f.NewNodeList([]*ast.Node{a, b, c})
	list.Loc = core.NewTextRange(3, 9)
	same := ast.NewNodeVisitor(func(n *ast.Node) *ast.Node { return n }, f, ast.NodeVisitorHooks{}).VisitNodes(list)
	v := ast.NewNodeVisitor(func(n *ast.Node) *ast.Node {
		if n == a {
			return nil
		}
		if n == b {
			return f.NewSyntaxList([]*ast.Node{b, d})
		}
		return n
	}, f, ast.NodeVisitorHooks{})
	changed := v.VisitNodes(list)
	empty := ast.NewNodeVisitor(func(*ast.Node) *ast.Node { return nil }, f, ast.NodeVisitorHooks{}).VisitNodes(f.NewNodeList([]*ast.Node{a}))
	one := ast.NewNodeVisitor(func(*ast.Node) *ast.Node { return f.NewSyntaxList([]*ast.Node{d}) }, f, ast.NodeVisitorHooks{}).VisitNode(a)
	multi := ast.NewNodeVisitor(func(*ast.Node) *ast.Node { return f.NewSyntaxList([]*ast.Node{a, b}) }, f, ast.NodeVisitorHooks{})
	block := multi.VisitEmbeddedStatement(a)
	return map[string]any{"sameList": same == list, "changedList": changed != list, "changedLoc": []int{changed.Pos(), changed.End()}, "changedOrder": nodes(changed.Nodes), "deletedNodesNil": empty.Nodes == nil, "oneLiftIdentity": one == d, "multiNodeThrows": caught(func() { multi.VisitNode(a) }), "embeddedBlock": snap(block), "embeddedChildren": nodes(block.AsBlock().Statements.Nodes), "nilInput": v.VisitNode(nil) == nil, "nilList": v.VisitNodes(nil) == nil}
}

func accessors() any {
	f := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	id := f.NewIdentifier("x")
	jsx := f.NewJsxText("text", false)
	literal := f.NewStringLiteral("é", 0)
	return map[string]any{"identifierParameterListThrows": caught(func() { id.ParameterList() }), "identifierParametersThrows": caught(func() { id.Parameters() }), "jsxTextThrows": caught(func() { jsx.Text() }), "literalText": b(literal.Text())}
}
