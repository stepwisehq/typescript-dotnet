// Independent read-only observations of actual upstream named-type links.
package checker

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"slices"
)

type OracleDataPropertiesEntry struct {
	Include       TypeFlags
	IncludeOrigin bool
	Value         *Type
}

func OracleObserveDataPropertiesTypesNil(c *Checker) bool { return c.propertiesTypes == nil }
func OracleObserveDataPropertiesTypes(c *Checker, t *Type) []OracleDataPropertiesEntry {
	result := []OracleDataPropertiesEntry{}
	for key, value := range c.propertiesTypes {
		if key.typeId == t.id {
			result = append(result, OracleDataPropertiesEntry{key.include, key.includeOrigin, value})
		}
	}
	slices.SortFunc(result, func(a, b OracleDataPropertiesEntry) int {
		if a.Include != b.Include {
			return int(a.Include) - int(b.Include)
		}
		if a.IncludeOrigin == b.IncludeOrigin {
			return 0
		}
		if a.IncludeOrigin {
			return 1
		}
		return -1
	})
	return result
}

type OracleDataLink struct {
	Name string
	Data any
}

func OracleObserveDataSymbolLinks(c *Checker, s *ast.Symbol) []OracleDataLink {
	result := []OracleDataLink{}
	add := func(name string, value any) { result = append(result, OracleDataLink{name, value}) }
	if x := c.valueSymbolLinks.TryGet(s); x != nil {
		add("valueSymbolLinks", x)
	} else {
		add("valueSymbolLinks", nil)
	}
	if x := c.typeAliasLinks.TryGet(s); x != nil {
		add("typeAliasLinks", x)
	} else {
		add("typeAliasLinks", nil)
	}
	if x := c.declaredTypeLinks.TryGet(s); x != nil {
		add("declaredTypeLinks", x)
	} else {
		add("declaredTypeLinks", nil)
	}
	if x := c.deferredSymbolLinks.TryGet(s); x != nil {
		add("deferredSymbolLinks", x)
	} else {
		add("deferredSymbolLinks", nil)
	}
	if x := c.mappedSymbolLinks.TryGet(s); x != nil {
		add("mappedSymbolLinks", x)
	} else {
		add("mappedSymbolLinks", nil)
	}
	if x := c.symbolReferenceLinks.TryGet(s); x != nil {
		add("symbolReferenceLinks", x)
	} else {
		add("symbolReferenceLinks", nil)
	}
	if x := c.moduleSymbolLinks.TryGet(s); x != nil {
		add("moduleSymbolLinks", x)
	} else {
		add("moduleSymbolLinks", nil)
	}
	return result
}

func OracleObserveDataNodeLinks(c *Checker, n *ast.Node) []OracleDataLink {
	result := []OracleDataLink{}
	if x := c.nodeLinks.TryGet(n); x != nil {
		result = append(result, OracleDataLink{"nodeLinks", x})
	} else {
		result = append(result, OracleDataLink{"nodeLinks", nil})
	}
	if x := c.typeNodeLinks.TryGet(n); x != nil {
		result = append(result, OracleDataLink{"typeNodeLinks", x})
	} else {
		result = append(result, OracleDataLink{"typeNodeLinks", nil})
	}
	return result
}
func OracleObserveDataResolvedSymbol(c *Checker, n *ast.Node) *ast.Symbol {
	if x := c.symbolNodeLinks.TryGet(n); x != nil {
		return x.resolvedSymbol
	}
	return nil
}
func OracleObserveDataMergedSymbol(c *Checker, s *ast.Symbol) (*ast.Symbol, bool) {
	value, present := c.mergedSymbols[s]
	return value, present
}

type OracleDataInstantiation struct {
	Arguments       []*Type
	Alias           *TypeAlias
	SingleSignature bool
	Family          string
	Membership      bool
}

func OracleObserveDataInstantiation(c *Checker, owner, value *Type) OracleDataInstantiation {
	var out OracleDataInstantiation
	var key CacheHashKey
	if owner.AsInterfaceType() != nil && value.AsTypeReference() != nil {
		out.Arguments = value.AsTypeReference().resolvedTypeArguments
		out.Family = "reference"
		key = getTypeListKey(out.Arguments)
	} else if owner.objectFlags&ObjectFlagsAnonymous != 0 {
		declarations := owner.symbol.Declarations
		if len(declarations) == 0 {
			panic("Anonymous cache owner has no declaration")
		}
		links := c.typeNodeLinks.TryGet(declarations[0])
		if links == nil || len(links.outerTypeParameters) == 0 {
			panic("Anonymous cache has no stored outer parameters")
		}
		parameters := links.outerTypeParameters
		out.Family = "anonymous"
		if owner == value {
			out.Arguments = parameters
			out.Alias = owner.alias
		} else {
			if value.Target() != owner {
				panic("Anonymous cache value has another target")
			}
			out.SingleSignature = owner.objectFlags&ObjectFlagsSingleSignatureType != 0
			mapper := value.Mapper()
			if out.SingleSignature && mapper != nil {
				if composite, ok := mapper.data.(*CompositeTypeMapper); ok {
					mapper = composite.m1
				}
			}
			if mapper == nil {
				panic("Unknown stored anonymous key mapper")
			}
			switch m := mapper.data.(type) {
			case *SimpleTypeMapper:
				if len(parameters) != 1 || parameters[0] != m.source {
					panic("Anonymous mapper source mismatch")
				}
				out.Arguments = []*Type{m.target}
			case *ArrayTypeMapper:
				if !slices.Equal(parameters, m.sources) || len(m.targets) != len(parameters) {
					panic("Anonymous mapper source mismatch")
				}
				out.Arguments = m.targets
			default:
				panic("Unknown stored anonymous key mapper")
			}
			out.Alias = value.alias
		}
		key = getTypeInstantiationKey(out.Arguments, out.Alias, out.SingleSignature)
	} else {
		panic("Unknown instantiated-object key family")
	}
	out.Membership = owner.AsObjectType().instantiations[key] == value
	return out
}
func OracleObserveDataKeyMembership(c *Checker, t, value *Type) bool {
	return OracleObserveDataInstantiation(c, t, value).Membership
}
func OracleObserveDataCacheAssociation(c *Checker, t *Type) bool {
	used := map[CacheHashKey]bool{}
	for actual, value := range t.AsObjectType().instantiations {
		entry := OracleObserveDataInstantiation(c, t, value)
		var key CacheHashKey
		if entry.Family == "reference" {
			key = getTypeListKey(entry.Arguments)
		} else {
			key = getTypeInstantiationKey(entry.Arguments, entry.Alias, entry.SingleSignature)
		}
		if actual != key || used[key] || !entry.Membership {
			return false
		}
		used[key] = true
	}
	return true
}
func OracleObserveDataInternMembership(c *Checker, t *Type) bool {
	if t.flags&TypeFlagsUnion != 0 {
		return c.unionTypes[getUnionKey(t.Types(), t.AsUnionType().origin, t.alias)] == t
	}
	if t.flags&TypeFlagsIntersection != 0 {
		return c.intersectionTypes[getIntersectionKey(t.Types(), IntersectionFlagsNone, t.alias)] == t
	}
	panic("Noncomposite intern membership")
}
func OracleObserveDataKeyEquality(source, target *Type, alias1, alias2 *TypeAlias) []struct {
	Name      string
	BoolValue bool
} {
	types := []*Type{source, target}
	reverse := []*Type{target, source}
	forward, constrained := getRelationKey(source, target, IntersectionStateNone, false, false)
	again, _ := getRelationKey(source, target, IntersectionStateNone, false, false)
	identity, _ := getRelationKey(source, target, IntersectionStateNone, true, false)
	identityReverse, _ := getRelationKey(target, source, IntersectionStateNone, true, false)
	reversed, _ := getRelationKey(target, source, IntersectionStateNone, false, false)
	ignored, _ := getRelationKey(source, target, IntersectionStateNone, false, true)
	return []struct {
		Name      string
		BoolValue bool
	}{
		{"type-list.same", getTypeListKey(types) == getTypeListKey(types)},
		{"type-list.reverse", getTypeListKey(types) == getTypeListKey(reverse)},
		{"alias.same", getAliasKey(alias1) == getAliasKey(alias1)},
		{"alias.other", getAliasKey(alias1) == getAliasKey(alias2)},
		{"union.alias", getUnionKey(types, nil, alias1) == getUnionKey(types, nil, alias2)},
		{"intersection.alias", getIntersectionKey(types, IntersectionFlagsNone, alias1) == getIntersectionKey(types, IntersectionFlagsNone, alias2)},
		{"intersection.no-constraint-alias", getIntersectionKey(types, IntersectionFlagsNoConstraintReduction, alias1) == getIntersectionKey(types, IntersectionFlagsNoConstraintReduction, alias2)},
		{"instantiation.alias", getTypeAliasInstantiationKey(types, alias1) == getTypeAliasInstantiationKey(types, alias2)},
		{"relation.same", forward == again}, {"relation.reverse", forward == reversed}, {"relation.identity-reverse", identity == identityReverse}, {"relation.ignore", forward == ignored}, {"relation.constrained", constrained},
	}
}
func OracleObserveDataReferenceKeys(t *Type) []struct {
	Name      string
	BoolValue bool
} {
	forward, constrained := getRelationKey(t, t, IntersectionStateNone, false, false)
	again, _ := getRelationKey(t, t, IntersectionStateNone, false, false)
	ignored, _ := getRelationKey(t, t, IntersectionStateNone, false, true)
	state, _ := getRelationKey(t, t, IntersectionStateSource, false, false)
	return []struct {
		Name      string
		BoolValue bool
	}{{"reference-key.generic-arguments", isTypeReferenceWithGenericArguments(t)}, {"reference-key.same", forward == again}, {"reference-key.ignore-equivalent", forward == ignored}, {"reference-key.state-distinct", forward != state}, {"reference-key.constrained", constrained}}
}
