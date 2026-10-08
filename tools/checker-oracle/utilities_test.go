// Independent observations of actual private checker and public AST/core stores.
package checker

import (
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"reflect"
	"testing"
)

type oracleProbeValue struct{ Value int }
type oracleObservation struct {
	Name string `json:"name"`
	Bool *bool  `json:"boolValue,omitempty"`
	Int  *int   `json:"intValue,omitempty"`
}

func TestOracleSemanticUtilities(t *testing.T) {
	observations := []oracleObservation{}
	flag := func(name string, value bool) {
		observations = append(observations, oracleObservation{Name: name, Bool: &value})
	}
	count := func(name string, value int) {
		observations = append(observations, oracleObservation{Name: name, Int: &value})
	}
	var store core.PagedLinkStore[oracleProbeValue]
	flag("paged.unallocated", store.TryGet(1) == nil && !store.Has(1))
	first := store.Get(0)
	first.Value = 7
	flag("paged.same-page-zero", store.Has(1) && store.TryGet(1) != nil && store.TryGet(1).Value == 0)
	flag("paged.stable-distinct-slots", store.Get(0) == first && store.TryGet(1) != first)
	flag("paged.other-page-unallocated", store.TryGet(256) == nil)
	low := (uint64(65535) << 8) + 255
	high := uint64(65536) << 8
	lowSlot, highSlot := store.Get(low), store.Get(high)
	flag("paged.last-list-first-map-stable", store.Get(low) == lowSlot && store.Get(high) == highSlot && lowSlot != highSlot)
	flag("paged.high-map-neighbor-zero", store.Has(high+1) && store.TryGet(high+1).Value == 0)
	flag("paged.high-other-page-unallocated", store.TryGet(high+256) == nil)
	var factory ast.NodeFactory
	var left, right *ast.Node
	for {
		left, right = factory.NewIdentifier("left"), factory.NewIdentifier("right")
		if uint64(ast.GetNodeId(left))>>8 == uint64(ast.GetNodeId(right))>>8 {
			break
		}
	}
	var nodeStore nodeLinkStore[oracleProbeValue]
	flag("node.unallocated", nodeStore.TryGet(left) == nil && !nodeStore.Has(left))
	nodeSlot := nodeStore.Get(left)
	nodeSlot.Value = 19
	flag("node.same-page-zero", nodeStore.Has(right) && nodeStore.TryGet(right) != nil && nodeStore.TryGet(right).Value == 0)
	flag("node.stable-identities", nodeStore.Get(left) == nodeSlot && nodeStore.TryGet(right) == nodeStore.Get(right) && nodeStore.TryGet(right) != nodeSlot)
	var a, b *ast.Symbol
	for {
		a, b = &ast.Symbol{Name: "a"}, &ast.Symbol{Name: "b"}
		if uint64(ast.GetSymbolId(a))>>8 == uint64(ast.GetSymbolId(b))>>8 {
			break
		}
	}
	var symbolStore symbolArenaLinkStore[oracleProbeValue]
	flag("symbol.unallocated", symbolStore.TryGet(a) == nil && !symbolStore.Has(a))
	symbolSlot := symbolStore.Get(a)
	symbolSlot.Value = 29
	flag("symbol.same-page-still-nil", symbolStore.TryGet(b) == nil && !symbolStore.Has(b))
	otherSymbolSlot := symbolStore.Get(b)
	flag("symbol.stable-distinct-identities", symbolStore.Get(a) == symbolSlot && symbolStore.TryGet(a) == symbolSlot && otherSymbolSlot != symbolSlot && otherSymbolSlot.Value == 0)
	file := parser.ParseSourceFile(ast.SourceFileParseOptions{}, "", core.ScriptKindTS)
	symbol := &ast.Symbol{Name: "probe"}
	flag("tables.initially-nil", file.Locals == nil && symbol.Members == nil && symbol.Exports == nil)
	locals, members, exports := ast.GetLocals(file.AsNode()), ast.GetMembers(symbol), ast.GetExports(symbol)
	flag("tables.allocated-empty", locals != nil && members != nil && exports != nil)
	count("tables.locals-count", len(locals))
	count("tables.members-count", len(members))
	count("tables.exports-count", len(exports))
	identity := func(table ast.SymbolTable) uintptr { return reflect.ValueOf(table).Pointer() }
	flag("tables.stable-lazy-identities", identity(ast.GetLocals(file.AsNode())) == identity(locals) && identity(ast.GetMembers(symbol)) == identity(members) && identity(ast.GetExports(symbol)) == identity(exports))
	flag("tables.distinct-identities", identity(locals) != identity(members) && identity(members) != identity(exports) && identity(locals) != identity(exports))
	encoded, err := json.Marshal(observations)
	if err != nil {
		t.Fatal(err)
	}
	fmt.Printf("UTILITY_RESULT:%s\n", encoded)
}
