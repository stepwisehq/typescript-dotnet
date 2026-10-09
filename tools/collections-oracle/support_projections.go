// Authored deterministic projections of actual pinned support operations.
// These are additional behavioral controls, apart from the adapted original set prefix.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/collections"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"slices"
)

func supportProjections() map[string]any {
	out := map[string]any{}
	factory := ast.NewNodeFactory(ast.NodeFactoryHooks{})
	one, two, three, four, absent := factory.NewIdentifier("same"), factory.NewIdentifier("same"), factory.NewIdentifier("three"), factory.NewIdentifier("four"), factory.NewIdentifier("same")
	labels := map[*ast.Node]int{one: 1, two: 2, three: 3, four: 4}
	set := collections.OrderedSet[*ast.Node]{}
	zero := []any{set.Size(), set.Has(one)}
	set.Add(one)
	set.Add(two)
	set.Add(three)
	prefix := []bool{set.Has(one), set.Has(two), set.Has(three)}
	set.Add(two)
	initial := []int{}
	for node := range set.Values() {
		initial = append(initial, labels[node])
	}
	initialSize := set.Size()
	visited := []int{}
	for node := range set.Values() {
		visited = append(visited, labels[node])
		if node == one {
			set.Add(four)
		}
	}
	stopped := []int{}
	for node := range set.Values() {
		stopped = append(stopped, labels[node])
		break
	}
	out["orderedSet"] = map[string]any{"zero": zero, "originalPrefixHas": prefix, "absentHas": set.Has(absent), "initial": initial, "initialSize": initialSize, "visited": visited, "size": set.Size(), "hasFour": set.Has(four), "stopped": stopped}
	caller := make([]int, 3, 4)
	copy(caller, []int{1, 2, 3})
	indices := []int{}
	mapped := core.MapIndex(caller, func(value, index int) int {
		indices = append(indices, index)
		if index == 0 {
			caller = append(caller, 99)
			caller[2] = 4
		}
		return value + index
	})
	out["mapIndex"] = map[string]any{"nil": core.MapIndex[int, int](nil, func(v, i int) int { panic("nil callback") }) == nil, "emptyNonNil": core.MapIndex([]int{}, func(v, i int) int { return v + i }) != nil, "indices": indices, "result": mapped, "caller": caller}
	original := []int{1, 2, 3}
	unchanged := core.SameMap(original, func(v int) int { return v })
	changed := core.SameMap(original, func(v int) int {
		if v == 2 {
			return 20
		}
		return v
	})
	appender := make([]int, 3, 4)
	copy(appender, original)
	appIndices := []int{}
	appended := core.SameMapIndex(appender, func(v, i int) int {
		appIndices = append(appIndices, i)
		if i == 0 {
			appender = append(appender, 99)
		}
		return v
	})
	out["sameMap"] = map[string]any{"nil": core.SameMap[int](nil, func(v int) int { panic("nil callback") }) == nil, "unchangedSame": core.Same(original, unchanged), "changedSame": core.Same(original, changed), "changed": changed, "original": original, "appendIndices": appIndices, "appendResult": appended, "appendCaller": appender}
	counter := make([]int, 3, 4)
	copy(counter, original)
	countVisited := []int{}
	count := core.CountWhere(counter, func(v int) bool {
		countVisited = append(countVisited, v)
		if v == 1 {
			counter = append(counter, 99)
		}
		return v%2 == 1
	})
	out["countWhere"] = map[string]any{"nil": core.CountWhere[int](nil, func(v int) bool { panic("nil callback") }), "count": count, "visited": countVisited, "caller": counter}
	left, right, empty := []int{1, 2}, []int{3}, []int{}
	out["concatenate"] = map[string]any{"nilEmptyIsNil": core.Concatenate[int](nil, empty) == nil, "leftSame": core.Same(left, core.Concatenate(left, empty)), "rightSame": core.Same(right, core.Concatenate(empty, right)), "result": core.Concatenate(left, right), "left": left, "nilEmptySame": core.Same[int](nil, empty), "wholeSame": core.Same(left, left), "independentSame": core.Same(left, []int{1, 2})}
	replacement := core.ReplaceElement(original, 1, 20)
	deletionCaller := []int{1, 2, 3, 4}
	noDelete := slices.Delete(deletionCaller, 1, 1)
	deleted := slices.Delete(deletionCaller, 1, 3)
	out["replaceDelete"] = map[string]any{"replacement": replacement, "original": original, "replacementSame": core.Same(original, replacement), "emptyDeleteSame": core.Same(noDelete, deletionCaller), "deleted": deleted, "caller": deletionCaller}
	out["equalContains"] = map[string]any{"nilEmptyEqual": slices.Equal([]int(nil), []int{}), "nilContains": slices.Contains([]int(nil), 1), "equal": slices.Equal([]int{1, 2}, []int{1, 2}), "reversedEqual": slices.Equal([]int{1, 2}, []int{2, 1}), "contains": slices.Contains([]int{1, 2}, 2)}
	return out
}
