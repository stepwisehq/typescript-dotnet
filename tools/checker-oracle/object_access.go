// Independent read-only observations of actual upstream private object caches.
package checker

import "github.com/microsoft/TypeScript/tsc/internal/ast"

type OracleObjectValueLinks struct {
	Present                                           bool
	Target                                            *ast.Symbol
	ResolvedType, WriteType, NameType, ContainingType *Type
	FunctionOrConstructorChecked                      bool
}

func OracleObserveObjectValueLinks(c *Checker, symbol *ast.Symbol) OracleObjectValueLinks {
	links := c.valueSymbolLinks.TryGet(symbol)
	if links == nil {
		return OracleObjectValueLinks{}
	}
	if links.mapper != nil {
		panic("Object oracle encountered unsupported property mapper")
	}
	return OracleObjectValueLinks{true, links.target, links.resolvedType, links.writeType, links.nameType, links.containingType, links.functionOrConstructorChecked}
}
func OracleObserveObjectCache(c *Checker, source *Type, kind string) *Type {
	switch kind {
	case "Widened":
		return c.cachedTypes[CachedTypeKey{kind: CachedTypeKindWidened, typeId: source.id}]
	case "RegularObjectLiteral":
		return c.cachedTypes[CachedTypeKey{kind: CachedTypeKindRegularObjectLiteral, typeId: source.id}]
	default:
		panic("Unknown original object cache kind")
	}
}

// Compare actual original relation keys, never their hash representation.
func OracleObserveRelationKeyEquality(c *Checker) []struct {
	Name      string
	BoolValue bool
} {
	source, target := c.numberType, c.stringType
	forward, constrained := getRelationKey(source, target, IntersectionStateNone, false, false)
	again, _ := getRelationKey(source, target, IntersectionStateNone, false, false)
	reverse, _ := getRelationKey(target, source, IntersectionStateNone, false, false)
	identity, _ := getRelationKey(source, target, IntersectionStateNone, true, false)
	identityReverse, _ := getRelationKey(target, source, IntersectionStateNone, true, false)
	stateSource, _ := getRelationKey(source, target, IntersectionStateSource, false, false)
	stateTarget, _ := getRelationKey(source, target, IntersectionStateTarget, false, false)
	ignoreConstraints, _ := getRelationKey(source, target, IntersectionStateNone, false, true)
	return []struct {
		Name      string
		BoolValue bool
	}{
		{"relation-key.same-input-stable", forward == again},
		{"relation-key.direction-distinct", forward != reverse},
		{"relation-key.identity-order-normalized", identity == identityReverse},
		{"relation-key.state-source-distinct", forward != stateSource},
		{"relation-key.state-target-distinct", forward != stateTarget && stateSource != stateTarget},
		{"relation-key.nongeneric-unconstrained", !constrained && forward == ignoreConstraints},
	}
}
