// Reached-object membership in both actual intersection key families. These are
// ephemeral key rebuilds over already allocated fields, never semantic resolvers.
package checker

func OracleCollectionsInternMembership(c *Checker, t *Type) (bool, bool) {
	allocated := func(value *Type) {
		if value == nil || value.id == 0 || value.checker != nil && value.checker != c {
			panic("Unallocated or foreign composite key term")
		}
	}
	allocated(t)
	for _, child := range t.Types() {
		allocated(child)
	}
	if t.alias != nil {
		if t.alias.symbol == nil || OracleCollectionsStoredSymbolID(t.alias.symbol) == 0 {
			panic("Unallocated composite alias key symbol")
		}
		for _, child := range t.alias.typeArguments {
			allocated(child)
		}
	}
	if t.flags&TypeFlagsUnion != 0 {
		origin := t.AsUnionType().origin
		if origin != nil {
			allocated(origin)
			if origin.flags&(TypeFlagsUnion|TypeFlagsIntersection) != 0 {
				for _, child := range origin.Types() {
					allocated(child)
				}
			}
		}
		return OracleObserveDataInternMembership(c, t), false
	}
	if t.flags&TypeFlagsIntersection != 0 {
		return OracleObserveDataInternMembership(c, t), c.intersectionTypes[getIntersectionKey(t.Types(), IntersectionFlagsNoConstraintReduction, t.alias)] == t
	}
	panic("Noncomposite intern membership")
}
