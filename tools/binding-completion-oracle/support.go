// Authored observations of actual pinned Go binding support APIs.
// Seeded graph controls are separate from naturally produced binder graphs.
package main

import (
	"encoding/base64"
	"errors"
	"fmt"
	"reflect"
	"sync"
	"sync/atomic"
	"time"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/binder"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
)

func supportParse(text string) *ast.SourceFile {
	return parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: "/same.ts", PathKey: "/same.ts"}, text, core.ScriptKindTS)
}

func supportBytes(text string) string { return base64.StdEncoding.EncodeToString([]byte(text)) }

// Maps are not Go-comparable. Compare actual runtime map identities, exactly as
// the canonical graph oracle does, without inserting observation-only entries.
func supportSameTable(a, b ast.SymbolTable) bool {
	return reflect.ValueOf(a).UnsafePointer() == reflect.ValueOf(b).UnsafePointer()
}

func supportObservations() map[string]any {
	return map[string]any{
		"Tables": supportTables(), "Symbols": supportSymbols(), "AtomicIds": supportAtomicIDs(),
		"SyntheticFlows": supportSyntheticFlows(), "BindOnceSuccess": supportBindOnce(false),
		"BindOnceFailure": supportBindOnce(true), "CloneBindingState": supportCloneBindingState(),
		"FileIsolation": supportFileIsolation(),
	}
}

func supportTables() map[string]any {
	symbol := &ast.Symbol{}
	file := supportParse("")
	initialNil := symbol.Members == nil && symbol.Exports == nil && file.Locals == nil
	members := ast.GetMembers(symbol)
	exports := ast.GetExports(symbol)
	locals := ast.GetLocals(file.AsNode())
	empty := len(members) == 0 && len(exports) == 0 && len(locals) == 0
	repeat := supportSameTable(members, ast.GetMembers(symbol)) && supportSameTable(exports, ast.GetExports(symbol)) && supportSameTable(locals, ast.GetLocals(file.AsNode()))
	separate := !supportSameTable(members, exports) && !supportSameTable(members, locals) && !supportSameTable(exports, locals)
	absent := members["absent"] == nil
	entry := &ast.Symbol{Name: "x", Flags: ast.SymbolFlagsProperty}
	members["x"] = entry
	other := &ast.Symbol{Members: members, Exports: exports}
	seededShared := supportSameTable(ast.GetMembers(other), members) && supportSameTable(ast.GetExports(other), exports) && other.Members["x"] == entry
	missingClass := supportParse("class {}")
	binder.BindSourceFile(missingClass)
	emptyType := supportParse("type T = {};")
	binder.BindSourceFile(emptyType)
	return map[string]any{
		"initialNil": initialNil, "empty": empty, "repeat": repeat,
		"separate": separate, "absent": absent, "seededShared": seededShared,
		"membersCount": len(members), "naturalEmptyLocals": missingClass.Locals != nil && len(missingClass.Locals) == 0,
		"emptyTypeMembersNil": emptyType.Statements.Nodes[0].Type().Symbol().Members == nil,
	}
}

func supportSymbols() map[string]any {
	file := supportParse("export class C { #p=1; static s=2; } export var x=1; export var x=2;")
	binder.BindSourceFile(file)
	module := file.Symbol
	c := module.Exports["C"]
	x := module.Exports["x"]
	var privateSymbol *ast.Symbol
	for _, s := range c.Members {
		if s.ValueDeclaration != nil && s.ValueDeclaration.Name() != nil && s.ValueDeclaration.Name().Kind == ast.KindPrivateIdentifier {
			if privateSymbol != nil {
				panic("Multiple private members in support fixture")
			}
			privateSymbol = s
		}
	}
	if privateSymbol == nil {
		panic("Missing private member in support fixture")
	}
	prototype := c.Exports["prototype"]
	xLocal := file.Locals["x"]
	transient := &ast.Symbol{Flags: ast.SymbolFlagsTransient, Declarations: x.Declarations}
	owners := []any{}
	checkFlagsZero := true
	for _, s := range []*ast.Symbol{module, c, x, privateSymbol, prototype, xLocal} {
		owners = append(owners, ast.GetSourceFileOfSymbol(s) == file)
		checkFlagsZero = checkFlagsZero && s.CheckFlags == 0
	}
	escaped := []any{}
	for _, name := range []string{"x", "__x", ast.InternalSymbolNameType, "a" + ast.InternalSymbolNamePrefix + "b", "___x"} {
		escaped = append(escaped, []any{supportBytes(name), supportBytes(ast.EscapeAllInternalSymbolNames(name)), supportBytes(ast.EscapeInternalSymbolName(name)), supportBytes(ast.EscapeSymbolName(name))})
	}
	return map[string]any{
		"owners": owners, "transientOwnerNull": ast.GetSourceFileOfSymbol(transient) == nil,
		"moduleExternal": module.IsExternalModule(), "classExternal": c.IsExternalModule(),
		"static":         []any{c.Exports["s"].IsStatic(), privateSymbol.IsStatic(), prototype.IsStatic()},
		"privateDisplay": supportBytes(ast.SymbolName(privateSymbol)), "privateRawDiffers": privateSymbol.Name != ast.SymbolName(privateSymbol),
		"xDeclarationCount": len(x.Declarations), "xLocalExport": xLocal.ExportSymbol == x,
		"combined": uint32(xLocal.CombinedLocalAndExportSymbolFlags()), "escaped": escaped, "checkFlagsZero": checkFlagsZero,
	}
}

func supportAtomicIDs() map[string]any {
	factory := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	a, b := factory.NewIdentifier("a"), factory.NewIdentifier("b")
	s, t := &ast.Symbol{}, &ast.Symbol{}
	var nodes, symbols [32]uint64
	var done sync.WaitGroup
	for i := range nodes {
		done.Add(1)
		go func() {
			defer done.Done()
			nodes[i] = uint64(ast.GetNodeId(a))
			symbols[i] = uint64(ast.GetSymbolId(s))
		}()
	}
	done.Wait()
	nodeStable, symbolStable := true, true
	for i := range nodes {
		nodeStable = nodeStable && nodes[i] != 0 && nodes[i] == nodes[0]
		symbolStable = symbolStable && symbols[i] != 0 && symbols[i] == symbols[0]
	}
	return map[string]any{"nodeStable": nodeStable, "symbolStable": symbolStable, "nodeDistinct": uint64(ast.GetNodeId(b)) != nodes[0], "symbolDistinct": uint64(ast.GetSymbolId(t)) != symbols[0]}
}

func supportSyntheticFlows() map[string]any {
	node := supportParse("switch(x){case 1:break;}").Statements.Nodes[0]
	rangeNode := ast.NewFlowSwitchClauseData(node, 1, 3)
	empty := ast.NewFlowSwitchClauseData(node, 2, 2)
	target, head := &ast.FlowNode{Flags: ast.FlowFlagsBranchLabel}, &ast.FlowNode{Flags: ast.FlowFlagsStart}
	tail := &ast.FlowList{Flow: head}
	list := &ast.FlowList{Flow: target, Next: tail}
	reduce := ast.NewFlowReduceLabelData(target, list)
	r := rangeNode.AsFlowSwitchClauseData()
	d := reduce.AsFlowReduceLabelData()
	return map[string]any{
		"switch": []any{int(rangeNode.Kind), rangeNode.Pos(), rangeNode.End(), uint32(rangeNode.Flags), r.SwitchStatement == node, r.ClauseStart, r.ClauseEnd, r.IsEmpty(), empty.AsFlowSwitchClauseData().IsEmpty()},
		"reduce": []any{int(reduce.Kind), reduce.Pos(), reduce.End(), uint32(reduce.Flags), d.Target == target, d.Antecedents == list, d.Antecedents.Next == tail, tail.Flow == head},
	}
}

func supportAwait(ch <-chan struct{}, description string) {
	select {
	case <-ch:
	case <-time.After(10 * time.Second):
		panic("Binding support timeout: " + description)
	}
}

func supportBindOnce(failing bool) map[string]any {
	file := supportParse("")
	entered, release, firstDone := make(chan struct{}), make(chan struct{}), make(chan struct{})
	started, returned := make(chan struct{}, 6), make(chan struct{}, 6)
	var attempts, callbackCompleted atomic.Int32
	var callerStates [6]bool
	failure := errors.New("deliberate bind failure")
	var releaseOnce sync.Once
	unblock := func() { releaseOnce.Do(func() { close(release) }) }
	defer unblock()
	go func() {
		defer close(firstDone)
		defer func() {
			r := recover()
			if failing && r != failure {
				panic(fmt.Sprintf("Unexpected support panic: %v", r))
			}
			if !failing && r != nil {
				panic(r)
			}
		}()
		file.BindOnce(func() {
			attempts.Add(1)
			if failing {
				defer callbackCompleted.Store(1)
				file.SymbolCount, file.GlobalExports = 71, make(ast.SymbolTable)
			}
			close(entered)
			supportAwait(release, "callback release")
			if failing {
				panic(failure)
			}
			file.SymbolCount, file.GlobalExports = 123, make(ast.SymbolTable)
		})
		if failing {
			panic("Missing expected support panic")
		}
	}()
	supportAwait(entered, "callback entry")
	unpublished := !file.IsBound()
	var competitors sync.WaitGroup
	for i := range callerStates {
		competitors.Add(1)
		go func() {
			defer competitors.Done()
			started <- struct{}{}
			file.BindOnce(func() { attempts.Add(1) })
			if failing {
				callerStates[i] = callbackCompleted.Load() == 1 && !file.IsBound() && file.SymbolCount == 71 && file.GlobalExports != nil && len(file.GlobalExports) == 0
			} else {
				callerStates[i] = file.IsBound() && file.SymbolCount == 123 && file.GlobalExports != nil && len(file.GlobalExports) == 0
			}
			returned <- struct{}{}
		}()
	}
	for range callerStates {
		supportAwait(started, "competitor entry")
	}
	waitersHeld := false
	select {
	case <-returned:
	case <-time.After(100 * time.Millisecond):
		waitersHeld = true
	}
	unblock()
	allDone := make(chan struct{})
	go func() { competitors.Wait(); close(allDone) }()
	supportAwait(allDone, "competitors finish")
	supportAwait(firstDone, "first callback finish")
	table := file.GlobalExports
	file.BindOnce(func() { attempts.Add(1) })
	if failing {
		binder.BindSourceFile(file)
	}
	tableEmpty := table != nil && len(table) == 0
	tableStable := supportSameTable(table, file.GlobalExports)
	states := []any{}
	for _, state := range callerStates {
		states = append(states, state)
	}
	result := map[string]any{"unpublished": unpublished, "waitersHeld": waitersHeld, "callerStates": states, "bound": file.IsBound(), "attempts": attempts.Load(), "symbolCount": file.SymbolCount}
	if failing {
		result["partialTable"], result["partialStable"] = tableEmpty, tableStable
	} else {
		result["tablePublished"], result["tableStable"] = tableEmpty, tableStable
	}
	return result
}

func supportFreshState(file *ast.SourceFile) []any {
	return []any{file.IsBound(), file.Symbol == nil, file.Locals == nil, file.NextContainer == nil, file.BindDiagnostics() == nil, file.SymbolCount, file.PatternAmbientModules == nil, file.GlobalExports == nil}
}

func supportCloneBindingState() map[string]any {
	file := supportParse("export class C {} export let x=1; export let x=2;")
	binder.BindSourceFile(file)
	factory := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	clone := file.AsNode().Clone(factory).AsSourceFile()
	unchanged := factory.UpdateSourceFile(file, file.Statements, file.EndOfFileToken)
	changed := factory.UpdateSourceFile(file, factory.NewNodeList(file.Statements.Nodes), file.EndOfFileToken).AsSourceFile()
	declaration := file.Statements.Nodes[0]
	declClone := declaration.Clone(factory)
	return map[string]any{
		"originalBound": file.IsBound(), "originalDiagnostics": len(file.BindDiagnostics()) > 0,
		"cloneState": supportFreshState(clone), "changedState": supportFreshState(changed),
		"unchangedIdentity": unchanged == file.AsNode(), "cloneStatementsShared": clone.Statements == file.Statements,
		"cloneModuleIndicatorShared": clone.ExternalModuleIndicator == file.ExternalModuleIndicator,
		"cloneFlags":                 uint32(clone.Flags), "flagsEqual": clone.Flags == file.Flags,
		"declaration": []any{declClone != declaration, declClone.Symbol() == nil, declClone.LocalSymbol() == nil, declClone.Locals() == nil, declClone.Name() == declaration.Name(), declClone.Flags == declaration.Flags, declClone.Pos() == declaration.Pos(), declClone.End() == declaration.End()},
	}
}

func supportFileIsolation() map[string]any {
	const text = "export class C { #p=1; } export let x=1;"
	a, b := supportParse(text), supportParse(text)
	binder.BindSourceFile(a)
	binder.BindSourceFile(b)
	symbol, locals, exports, class := a.Symbol, a.Locals, a.Symbol.Exports, a.Symbol.Exports["C"]
	binder.BindSourceFile(a)
	return map[string]any{
		"different":   []any{a != b, a.Symbol != b.Symbol, !supportSameTable(a.Locals, b.Locals), !supportSameTable(a.Symbol.Exports, b.Symbol.Exports), class != b.Symbol.Exports["C"], !supportSameTable(class.Members, b.Symbol.Exports["C"].Members)},
		"repeat":      []any{symbol == a.Symbol, supportSameTable(locals, a.Locals), supportSameTable(exports, a.Symbol.Exports), class == a.Symbol.Exports["C"]},
		"owners":      []any{ast.GetSourceFileOfSymbol(class) == a, ast.GetSourceFileOfSymbol(b.Symbol.Exports["C"]) == b},
		"countsEqual": a.SymbolCount == b.SymbolCount,
	}
}
