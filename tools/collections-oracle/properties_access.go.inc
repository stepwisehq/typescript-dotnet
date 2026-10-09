// Authored whole properties-types associations, reusing the accepted key family.
package checker

type OracleCollectionsPropertiesEntry struct {
	Owner         *Type
	Include       uint32
	IncludeOrigin bool
	Value         *Type
}

func OracleCollectionsPropertiesTypes(c *Checker, known map[TypeId]*Type) (bool, int, []OracleCollectionsPropertiesEntry) {
	var entries []OracleCollectionsPropertiesEntry
	if c.propertiesTypes != nil {
		entries = []OracleCollectionsPropertiesEntry{}
	}
	keys := map[PropertiesTypesKey]bool{}
	for key, value := range c.propertiesTypes {
		owner := oracleCollectionsKnownType(c, known, key.typeId, "properties-types")
		if key.include & ^(TypeFlagsStringLike|TypeFlagsNumberLike|TypeFlagsESSymbolLike) != 0 {
			panic("Invalid properties type include mask")
		}
		oracleCollectionsValidateStoredType(c, value)
		rebuilt := PropertiesTypesKey{owner.id, key.include, key.includeOrigin}
		found, present := c.propertiesTypes[rebuilt]
		if rebuilt != key || keys[rebuilt] || !present || found != value {
			panic("Properties type association mismatch")
		}
		keys[rebuilt] = true
		entries = append(entries, OracleCollectionsPropertiesEntry{owner, uint32(key.include), key.includeOrigin, value})
	}
	if len(entries) != len(c.propertiesTypes) || len(keys) != len(c.propertiesTypes) {
		panic("Properties type cardinality mismatch")
	}
	return c.propertiesTypes == nil, len(c.propertiesTypes), entries
}
func OracleCollectionsPropertiesValues(c *Checker) []*Type {
	out := []*Type{}
	for _, value := range c.propertiesTypes {
		out = append(out, value)
	}
	return out
}
