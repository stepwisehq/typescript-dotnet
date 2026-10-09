// Request-scoped authored observer metadata. Releasing registries after encoding
// changes neither checker stores nor returned graph/diagnostic payloads.
package checker

func OracleCollectionsRelease(c *Checker) {
	delete(oracleCollectionsApplicabilityOrigins, c)
	oracleCollectionsReleaseRelationKeys(c)
	oracleCollectionsReleaseFlowKeys(c)
	oracleCollectionsReleaseGlobalInputs(c)
	for t := range oracleCollectionsObjectOrigins {
		if t.checker == c {
			delete(oracleCollectionsObjectOrigins, t)
		}
	}
	for owner, entries := range oracleCollectionsInstantiationInputs {
		for _, entry := range entries {
			if entry.checker == c {
				delete(oracleCollectionsInstantiationInputs, owner)
			}
			break
		}
	}
	for owner, entry := range oracleCollectionsDeferredInputs {
		if entry.checker == c {
			delete(oracleCollectionsDeferredInputs, owner)
		}
	}
	for id, entry := range oracleCollectionsComparers {
		if entry.checker == c {
			delete(oracleCollectionsComparers, id)
		}
	}
	for context, entry := range oracleCollectionsContextComparers {
		if entry.checker == c {
			delete(oracleCollectionsContextComparers, context)
		}
	}
}
