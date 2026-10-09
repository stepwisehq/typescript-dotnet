// Authored flow-key provenance captures only the existing writer's entity slots.
package checker

import (
	"bytes"
	"encoding/binary"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/zeebo/xxh3"
	"reflect"
	"sync/atomic"
	"unsafe"
)

type oracleCollectionsFlowSlot struct {
	offset, width int
	kind          string
	owner         any
}
type oracleCollectionsFlowInput struct {
	stream    []byte
	slots     []oracleCollectionsFlowSlot
	ambiguous bool
}

var oracleCollectionsFlowBuilders = map[*keyBuilder][]oracleCollectionsFlowSlot{}
var oracleCollectionsFlowKeys = map[*Checker]map[CacheHashKey]oracleCollectionsFlowInput{}

func oracleCollectionsFlowStart(c *Checker, b *keyBuilder) { oracleCollectionsFlowBuilders[b] = nil }
func oracleCollectionsFlowSlotCapture(c *Checker, b *keyBuilder, kind string, owner any, width int) {
	if slots, ok := oracleCollectionsFlowBuilders[b]; ok {
		oracleCollectionsFlowBuilders[b] = append(slots, oracleCollectionsFlowSlot{len(b.overflowBuffer) + b.inlineLength - width, width, kind, owner})
	}
}
func oracleCollectionsFlowEnd(c *Checker, b *keyBuilder, key CacheHashKey, success bool) {
	slots, ok := oracleCollectionsFlowBuilders[b]
	delete(oracleCollectionsFlowBuilders, b)
	if !ok || !success {
		return
	}
	stream := append([]byte{}, b.overflowBuffer...)
	stream = append(stream, b.inlineBuffer[:b.inlineLength]...)
	input := oracleCollectionsFlowInput{stream, slots, false}
	entries := oracleCollectionsFlowKeys[c]
	if entries == nil {
		entries = map[CacheHashKey]oracleCollectionsFlowInput{}
		oracleCollectionsFlowKeys[c] = entries
	}
	if old, exists := entries[key]; exists {
		input.ambiguous = old.ambiguous || !bytes.Equal(old.stream, stream) || len(old.slots) != len(slots)
		if !input.ambiguous {
			for i, slot := range slots {
				if old.slots[i] != slot {
					input.ambiguous = true
				}
			}
		}
	}
	entries[key] = input
}

type OracleCollectionsFlowToken struct {
	Kind, Bytes string
	Owner       any
}

func OracleCollectionsStoredNodeID(node *ast.Node) uint64 {
	if node == nil {
		return 0
	}
	field := reflect.ValueOf(node).Elem().FieldByName("id")
	return (*atomic.Uint64)(unsafe.Pointer(field.UnsafeAddr())).Load()
}
func oracleCollectionsFlowTokens(c *Checker, key CacheHashKey) []OracleCollectionsFlowToken {
	if key == nonDottedNameCacheKey {
		return []OracleCollectionsFlowToken{{Kind: "bytes", Bytes: "?"}}
	}
	input, ok := oracleCollectionsFlowKeys[c][key]
	if !ok || input.ambiguous || CacheHashKey(xxh3.Hash128(input.stream)) != key {
		panic("Unknown flow reference key producer")
	}
	out := []OracleCollectionsFlowToken{}
	offset := 0
	for _, slot := range input.slots {
		if slot.offset < offset || slot.offset+slot.width > len(input.stream) {
			panic("Overlapping flow key tokens")
		}
		if slot.offset > offset {
			out = append(out, OracleCollectionsFlowToken{Kind: "bytes", Bytes: string(input.stream[offset:slot.offset])})
		}
		segment := input.stream[slot.offset : slot.offset+slot.width]
		valid := false
		switch owner := slot.owner.(type) {
		case *Type:
			valid = slot.kind == "type" && slot.width == 4 && owner.id != 0 && (owner.checker == nil || owner.checker == c) && TypeId(binary.LittleEndian.Uint32(segment)) == owner.id
		case *ast.Symbol:
			id := OracleCollectionsStoredSymbolID(owner)
			valid = slot.kind == "symbol" && slot.width == 8 && id != 0 && binary.LittleEndian.Uint64(segment) == id
		case *ast.Node:
			id := OracleCollectionsStoredNodeID(owner)
			valid = slot.kind == "node" && slot.width == 8 && id != 0 && binary.LittleEndian.Uint64(segment) == id
		}
		if !valid {
			panic("Invalid flow key entity association")
		}
		out = append(out, OracleCollectionsFlowToken{Kind: slot.kind, Owner: slot.owner})
		offset = slot.offset + slot.width
	}
	if offset < len(input.stream) {
		out = append(out, OracleCollectionsFlowToken{Kind: "bytes", Bytes: string(input.stream[offset:])})
	}
	return out
}

type OracleCollectionsFlowLoopEntry struct {
	Flow  *ast.FlowNode
	Key   []OracleCollectionsFlowToken
	Value *Type
}

func OracleCollectionsFlowLoops(c *Checker) (bool, int, []OracleCollectionsFlowLoopEntry) {
	var entries []OracleCollectionsFlowLoopEntry
	if c.flowLoopCache != nil {
		entries = []OracleCollectionsFlowLoopEntry{}
	}
	for key, value := range c.flowLoopCache {
		entries = append(entries, OracleCollectionsFlowLoopEntry{key.flowNode, oracleCollectionsFlowTokens(c, key.refKey), value})
	}
	return c.flowLoopCache == nil, len(c.flowLoopCache), entries
}

type OracleCollectionsNarrowedEntry struct {
	Type, Candidate          *Type
	AssumeTrue, CheckDerived bool
	Value                    *Type
}

func OracleCollectionsNarrowedTypes(c *Checker) (bool, int, []OracleCollectionsNarrowedEntry) {
	var entries []OracleCollectionsNarrowedEntry
	if c.narrowedTypes != nil {
		entries = []OracleCollectionsNarrowedEntry{}
	}
	for key, value := range c.narrowedTypes {
		entries = append(entries, OracleCollectionsNarrowedEntry{key.t, key.candidate, key.assumeTrue, key.checkDerived, value})
	}
	return c.narrowedTypes == nil, len(c.narrowedTypes), entries
}

type OracleCollectionsAssignmentEntry struct{ Declared, Assigned, Value *Type }

func OracleCollectionsAssignmentReduced(c *Checker, known map[TypeId]*Type) (bool, int, []OracleCollectionsAssignmentEntry) {
	var entries []OracleCollectionsAssignmentEntry
	if c.assignmentReducedTypes != nil {
		entries = []OracleCollectionsAssignmentEntry{}
	}
	for key, value := range c.assignmentReducedTypes {
		declared, assigned := known[key.id1], known[key.id2]
		if declared == nil || assigned == nil || declared.id != key.id1 || assigned.id != key.id2 || declared.checker != nil && declared.checker != c || assigned.checker != nil && assigned.checker != c {
			panic("Unrooted assignment reduced key")
		}
		entries = append(entries, OracleCollectionsAssignmentEntry{declared, assigned, value})
	}
	return c.assignmentReducedTypes == nil, len(c.assignmentReducedTypes), entries
}

type OracleCollectionsReachableEntry struct {
	Flow  *ast.FlowNode
	Value bool
}

func OracleCollectionsReachableFlows(c *Checker) (bool, int, []OracleCollectionsReachableEntry, *ast.FlowNode, bool) {
	var entries []OracleCollectionsReachableEntry
	if c.flowNodeReachable != nil {
		entries = []OracleCollectionsReachableEntry{}
	}
	for key, value := range c.flowNodeReachable {
		entries = append(entries, OracleCollectionsReachableEntry{key, value})
	}
	return c.flowNodeReachable == nil, len(c.flowNodeReachable), entries, c.lastFlowNode, c.lastFlowNodeReachable
}
func OracleCollectionsFlowLifetime(c *Checker) (bool, int, int, int) {
	return c.flowLoopStack == nil, len(c.flowLoopStack), len(c.activeMappers), len(c.activeTypeMappersCaches)
}
func OracleCollectionsFlowValues(c *Checker) []*Type {
	out := []*Type{}
	for _, values := range []map[*ast.Node]*Type{c.contextFreeTypes, c.flowTypeCache} {
		for _, value := range values {
			out = append(out, value)
		}
	}
	for _, value := range c.discriminatedContextualTypes {
		out = append(out, value)
	}
	for _, value := range c.flowLoopCache {
		out = append(out, value)
	}
	for key, value := range c.narrowedTypes {
		out = append(out, key.t, key.candidate, value)
	}
	for _, value := range c.assignmentReducedTypes {
		out = append(out, value)
	}
	for key := range c.flowLoopCache {
		for _, token := range oracleCollectionsFlowTokens(c, key.refKey) {
			if t, ok := token.Owner.(*Type); ok {
				out = append(out, t)
			}
		}
	}
	return out
}
func oracleCollectionsReleaseFlowKeys(c *Checker) { delete(oracleCollectionsFlowKeys, c) }

// Original retained expression/context maps, read without check/get operations.
type OracleCollectionsExpressionEntry struct {
	Node  *ast.Node
	Value *Type
}
type OracleCollectionsExpressionCache struct {
	Name    string
	IsNull  bool
	Count   int
	Entries []OracleCollectionsExpressionEntry
}

func OracleCollectionsExpressionCaches(c *Checker) []OracleCollectionsExpressionCache {
	out := []OracleCollectionsExpressionCache{}
	for _, item := range []struct {
		Name   string
		Values map[*ast.Node]*Type
	}{{"context-free-types", c.contextFreeTypes}, {"flow-type-cache", c.flowTypeCache}} {
		row := OracleCollectionsExpressionCache{Name: item.Name, IsNull: item.Values == nil, Count: len(item.Values)}
		if item.Values != nil {
			row.Entries = []OracleCollectionsExpressionEntry{}
		}
		for node, value := range item.Values {
			actual, present := item.Values[node]
			if node == nil || !present || actual != value {
				panic("Expression cache membership mismatch")
			}
			row.Entries = append(row.Entries, OracleCollectionsExpressionEntry{node, value})
		}
		if len(row.Entries) != len(item.Values) {
			panic("Expression cache cardinality mismatch")
		}
		out = append(out, row)
	}
	return out
}

type OracleCollectionsDiscriminatedEntry struct {
	Node                  *ast.Node
	ContextualType, Value *Type
}

func OracleCollectionsDiscriminatedTypes(c *Checker, knownNodes map[ast.NodeId]*ast.Node, knownTypes map[TypeId]*Type) (bool, int, []OracleCollectionsDiscriminatedEntry) {
	var entries []OracleCollectionsDiscriminatedEntry
	if c.discriminatedContextualTypes != nil {
		entries = []OracleCollectionsDiscriminatedEntry{}
	}
	seen := map[DiscriminatedContextualTypeKey]bool{}
	for key, value := range c.discriminatedContextualTypes {
		node, t := knownNodes[key.nodeId], knownTypes[key.typeId]
		if node == nil || key.nodeId == 0 || OracleCollectionsStoredNodeID(node) != uint64(key.nodeId) || t == nil || t.id == 0 || t.id != key.typeId || t.checker != nil && t.checker != c {
			panic("Unrooted discriminated contextual cache key")
		}
		rebuilt := DiscriminatedContextualTypeKey{key.nodeId, t.id}
		actual, present := c.discriminatedContextualTypes[rebuilt]
		if seen[rebuilt] || rebuilt != key || !present || actual != value {
			panic("Discriminated contextual cache membership mismatch")
		}
		seen[rebuilt] = true
		entries = append(entries, OracleCollectionsDiscriminatedEntry{node, t, value})
	}
	if len(seen) != len(c.discriminatedContextualTypes) || len(entries) != len(c.discriminatedContextualTypes) {
		panic("Discriminated contextual cache cardinality mismatch")
	}
	return c.discriminatedContextualTypes == nil, len(c.discriminatedContextualTypes), entries
}
