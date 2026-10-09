// Authored provenance of the sole original deferred target constructor.
package checker

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"reflect"
	"slices"
	"unsafe"
)

type oracleCollectionsDeferredInput struct {
	checker          *Checker
	parent           *ast.Node
	parameters       []*Type
	targetPCs        []uintptr
	targetIdentities []uintptr
	targets          []func() *Type
}

var oracleCollectionsDeferredInputs = map[*TypeMapper]oracleCollectionsDeferredInput{}

func oracleCollectionsCaptureDeferred(c *Checker, m *TypeMapper, parent *ast.Node, parameters []*Type) {
	data, ok := m.data.(*DeferredTypeMapper)
	if !ok {
		panic("Deferred producer has another mapper payload")
	}
	pcs := make([]uintptr, len(data.targets))
	identities := make([]uintptr, len(data.targets))
	for i, target := range data.targets {
		pcs[i] = reflect.ValueOf(target).Pointer()
		identities[i] = *(*uintptr)(unsafe.Pointer(&target))
	}
	oracleCollectionsDeferredInputs[m] = oracleCollectionsDeferredInput{c, parent, slices.Clone(parameters), pcs, identities, slices.Clone(data.targets)}
}

type OracleCollectionsDeferredTarget struct {
	Parent         *ast.Node
	TypeParameters []*Type
	Index          int
}

func OracleCollectionsDeferredTargets(c *Checker, m *TypeMapper) []OracleCollectionsDeferredTarget {
	data, ok := m.data.(*DeferredTypeMapper)
	if !ok {
		panic("Expected actual deferred mapper")
	}
	input, ok := oracleCollectionsDeferredInputs[m]
	if !ok || input.checker != c || input.parent == nil || len(data.sources) != len(input.parameters) || len(data.targets) != len(input.targetPCs) || len(data.targets) != len(input.parameters) {
		panic("Unknown or inconsistent deferred constructor provenance")
	}
	out := []OracleCollectionsDeferredTarget{}
	for i, source := range data.sources {
		if source != input.parameters[i] || data.targets[i] == nil || reflect.ValueOf(data.targets[i]).Pointer() != input.targetPCs[i] || *(*uintptr)(unsafe.Pointer(&data.targets[i])) != input.targetIdentities[i] {
			panic("Deferred target source, closure identity or code changed after original constructor")
		}
		out = append(out, OracleCollectionsDeferredTarget{input.parent, input.parameters, i})
	}
	return out
}

func OracleCollectionsIsDeferred(m *TypeMapper) bool {
	_, ok := m.data.(*DeferredTypeMapper)
	return ok
}
