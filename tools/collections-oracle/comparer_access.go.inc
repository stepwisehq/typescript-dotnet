// Authored constructor provenance. A Go function-value identity is used only
// as a private registry lookup, never emitted as a semantic or canonical ID.
package checker

import (
	"reflect"
	"unsafe"
)

type oracleCollectionsComparerInput struct {
	checker  *Checker
	function TypeComparer
	owner    any
	family   string
	state    uint32
	code     uintptr
}

var oracleCollectionsComparers = map[uintptr]oracleCollectionsComparerInput{}
var oracleCollectionsContextComparers = map[*InferenceContext]oracleCollectionsComparerInput{}

func oracleCollectionsComparerIdentity(function TypeComparer) uintptr {
	return *(*uintptr)(unsafe.Pointer(&function))
}
func oracleCollectionsCaptureComparer(c *Checker, function TypeComparer, owner any, family string, state uint32) TypeComparer {
	if function != nil {
		oracleCollectionsComparers[oracleCollectionsComparerIdentity(function)] = oracleCollectionsComparerInput{c, function, owner, family, state, reflect.ValueOf(function).Pointer()}
	}
	return function
}
func oracleCollectionsCaptureInferenceComparer(c *Checker, n *InferenceContext, function TypeComparer) {
	if input, ok := oracleCollectionsComparers[oracleCollectionsComparerIdentity(function)]; ok && input.checker == c {
		oracleCollectionsContextComparers[n] = input
	}
}
func OracleCollectionsCurrentRelation(c *Checker, r *Relater) string {
	if r.c != c {
		panic("Foreign comparer relater owner")
	}
	switch r.relation {
	case nil:
		return "nil"
	case c.assignableRelation:
		return "assignable"
	case c.comparableRelation:
		return "comparable"
	case c.identityRelation:
		return "identity"
	case c.subtypeRelation:
		return "subtype"
	case c.strictSubtypeRelation:
		return "strict-subtype"
	default:
		panic("Unknown current comparer relation identity")
	}
}
func OracleCollectionsComparer(c *Checker, n *InferenceContext) (string, any, string, uint32) {
	input, ok := oracleCollectionsContextComparers[n]
	if !ok || input.checker != c || oracleCollectionsComparerIdentity(n.compareTypes) != oracleCollectionsComparerIdentity(input.function) || reflect.ValueOf(n.compareTypes).Pointer() != input.code {
		panic("Unknown stored comparer constructor provenance")
	}
	if input.family == "checker-assignable" {
		if input.owner != c {
			panic("Foreign default comparer owner")
		}
		return input.family, c, "assignable", input.state
	}
	r, ok := input.owner.(*Relater)
	if !ok {
		panic("Unknown comparer captured owner")
	}
	return input.family, r, OracleCollectionsCurrentRelation(c, r), input.state
}
