// Authored nonforcing whole iteration-cache associations, including the genuine
// negative all-nil result. Fixed ES5 roots do not provide Iterable declarations.
package checker

type OracleCollectionsIterationEntry struct {
	Owner               *Type
	Use                 uint32
	Yield, Return, Next *Type
}

func OracleCollectionsIterationCache(c *Checker, known map[TypeId]*Type) (bool, int, []OracleCollectionsIterationEntry) {
	var entries []OracleCollectionsIterationEntry
	if c.iterationTypesCache != nil {
		entries = []OracleCollectionsIterationEntry{}
	}
	seen := map[IterationTypesKey]bool{}
	for key, value := range c.iterationTypesCache {
		owner := oracleCollectionsKnownType(c, known, key.typeId, "iteration-types")
		if key.use & ^IterationUseCacheFlags != 0 {
			panic("Invalid iteration cache flags")
		}
		rebuilt := IterationTypesKey{typeId: owner.id, use: key.use}
		actual, present := c.iterationTypesCache[rebuilt]
		if seen[rebuilt] || !present || actual != value || rebuilt != key {
			panic("Iteration cache association mismatch")
		}
		seen[rebuilt] = true
		for _, t := range []*Type{value.yieldType, value.returnType, value.nextType} {
			oracleCollectionsValidateStoredType(c, t)
		}
		entries = append(entries, OracleCollectionsIterationEntry{owner, uint32(key.use), value.yieldType, value.returnType, value.nextType})
	}
	if len(entries) != len(c.iterationTypesCache) || len(seen) != len(c.iterationTypesCache) {
		panic("Iteration cache cardinality mismatch")
	}
	return c.iterationTypesCache == nil, len(c.iterationTypesCache), entries
}
