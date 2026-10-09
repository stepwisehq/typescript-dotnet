// Authored read-only global cache observations and finite producer captures.
// Captures receive only already-computed inputs after the original cache stores.
package checker

import (
	"fmt"
	"reflect"
	"sync/atomic"
	"unsafe"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
)

type oracleCollectionsIndexedInput struct {
	object, index *Type
	flags         AccessFlags
	alias         *TypeAlias
}

var oracleCollectionsSignatureArguments = map[*Checker]map[CachedSignatureKey][]*Type{}
var oracleCollectionsIndexedInputs = map[*Checker]map[CacheHashKey]oracleCollectionsIndexedInput{}

type oracleCollectionsWidenedInput struct {
	source, value *Type
	ambiguous     bool
}

var oracleCollectionsWidenedOwners = map[*Checker]map[TypeId]oracleCollectionsWidenedInput{}

func oracleCollectionsCaptureWidenedOwner(c *Checker, source, value *Type) {
	entries := oracleCollectionsWidenedOwners[c]
	if entries == nil {
		entries = map[TypeId]oracleCollectionsWidenedInput{}
		oracleCollectionsWidenedOwners[c] = entries
	}
	// The original typed key stores source.id; its result can erase source.
	input := oracleCollectionsWidenedInput{source: source, value: value}
	if old, present := entries[source.id]; present {
		input.ambiguous = old.ambiguous || old.source != source || old.value != value
	}
	entries[source.id] = input
}

func OracleCollectionsWidenedInputs(c *Checker) []*Type {
	result := []*Type{}
	for id, input := range oracleCollectionsWidenedOwners[c] {
		source := input.source
		oracleCollectionsValidateStoredType(c, source)
		key := CachedTypeKey{kind: CachedTypeKindWidened, typeId: id}
		if input.ambiguous || source == nil || source.id != id {
			panic("Invalid actual Widened source provenance")
		}
		if value, present := c.cachedTypes[key]; !present || value != input.value {
			panic("Widened source has no actual stored cache key")
		}
		result = append(result, source)
	}
	return result
}

func oracleCollectionsCaptureSignatureArguments(c *Checker, key CachedSignatureKey, arguments []*Type) {
	entries := oracleCollectionsSignatureArguments[c]
	if entries == nil {
		entries = map[CachedSignatureKey][]*Type{}
		oracleCollectionsSignatureArguments[c] = entries
	}
	// The original key contains only length+IDs, so nil and zero-length input
	// headers are deliberately represented by one nonnil encoded empty sequence.
	entries[key] = append([]*Type{}, arguments...)
}

func oracleCollectionsCaptureIndexedInput(c *Checker, key CacheHashKey, object, index *Type, flags AccessFlags, alias *TypeAlias) {
	entries := oracleCollectionsIndexedInputs[c]
	if entries == nil {
		entries = map[CacheHashKey]oracleCollectionsIndexedInput{}
		oracleCollectionsIndexedInputs[c] = entries
	}
	entries[key] = oracleCollectionsIndexedInput{object, index, flags, alias}
}

func oracleCollectionsReleaseGlobalInputs(c *Checker) {
	delete(oracleCollectionsSignatureArguments, c)
	delete(oracleCollectionsIndexedInputs, c)
	delete(oracleCollectionsWidenedOwners, c)
}

func oracleCollectionsValidateStoredType(c *Checker, t *Type) {
	if t != nil && (t.id == 0 || t.checker != nil && t.checker != c) {
		panic("Unallocated or foreign global cache type")
	}
}

func oracleCollectionsKnownType(c *Checker, known map[TypeId]*Type, id TypeId, detail ...string) *Type {
	t := known[id]
	if id == 0 || t == nil || t.id != id {
		context := "global cache"
		if len(detail) != 0 {
			context = detail[0]
		}
		panic(fmt.Sprintf("Unrooted global cache type key: cache=%s missingId=%d knownTypes=%d", context, id, len(known)))
	}
	oracleCollectionsValidateStoredType(c, t)
	return t
}

func oracleCollectionsCheckKnownType(c *Checker, known map[TypeId]*Type, t *Type) {
	if t == nil || oracleCollectionsKnownType(c, known, t.id) != t {
		panic("Global cache type term identity mismatch")
	}
}

// Nonallocating observation of the existing node ID, matching the original
// atomic field. getTupleKey may only read a label after this nonzero guard.
func oracleCollectionsStoredNodeID(node *ast.Node) uint64 {
	value := reflect.ValueOf(node).Elem().FieldByName("id")
	return (*atomic.Uint64)(unsafe.Pointer(value.UnsafeAddr())).Load()
}

type OracleCollectionsTupleEntry struct {
	Value    *Type
	Elements []TupleElementInfo
	Readonly bool
}

func OracleCollectionsTupleTargets(c *Checker) (bool, int, []OracleCollectionsTupleEntry) {
	cache := c.tupleTypes
	if cache == nil {
		return true, 0, nil
	}
	count := len(cache)
	entries := make([]OracleCollectionsTupleEntry, 0, count)
	used := map[CacheHashKey]bool{}
	for key, value := range cache {
		if value == nil {
			panic("Absent tuple target cache value")
		}
		oracleCollectionsValidateStoredType(c, value)
		tuple, ok := value.data.(*TupleType)
		if !ok || tuple.target != value {
			panic("Global tuple cache value is not a tuple target")
		}
		for _, element := range tuple.elementInfos {
			if element.labeledDeclaration != nil && oracleCollectionsStoredNodeID(element.labeledDeclaration) == 0 {
				panic("Tuple cache label lacks already allocated node ID")
			}
		}
		rebuilt := getTupleKey(tuple.elementInfos, tuple.readonly)
		if rebuilt != key || used[rebuilt] || cache[rebuilt] != value {
			panic("Tuple target cache association mismatch")
		}
		used[rebuilt] = true
		entries = append(entries, OracleCollectionsTupleEntry{value, tuple.elementInfos, tuple.readonly})
	}
	if len(cache) != count || len(entries) != count || len(used) != count {
		panic("Tuple cache cardinality changed")
	}
	return false, count, entries
}

type OracleCollectionsSignatureEntry struct {
	Owner, Value *Signature
	Arguments    []*Type
	Family       string
}

func OracleCollectionsCachedSignatures(c *Checker, known map[TypeId]*Type) (bool, int, []OracleCollectionsSignatureEntry) {
	cache := c.cachedSignatures
	if cache == nil {
		return true, 0, nil
	}
	count := len(cache)
	entries := make([]OracleCollectionsSignatureEntry, 0, count)
	used := map[CachedSignatureKey]bool{}
	for key, value := range cache {
		if key.sig == nil || value == nil {
			panic("Absent signature cache owner or value")
		}
		entry := OracleCollectionsSignatureEntry{Owner: key.sig, Value: value}
		var rebuilt CacheHashKey
		switch key.key {
		case SignatureKeyBase:
			entry.Family, rebuilt = "base", SignatureKeyBase
		case SignatureKeyCanonical:
			entry.Family, rebuilt = "canonical", SignatureKeyCanonical
		case SignatureKeyErased:
			entry.Family, rebuilt = "erased", SignatureKeyErased
		case SignatureKeyInner:
			entry.Family, rebuilt = "inner", SignatureKeyInner
		case SignatureKeyOuter:
			entry.Family, rebuilt = "outer", SignatureKeyOuter
		default:
			entry.Family = "type-list"
			arguments, found := oracleCollectionsSignatureArguments[c][key]
			if !found {
				panic("Missing actual signature cache argument provenance")
			}
			for _, argument := range arguments {
				oracleCollectionsCheckKnownType(c, known, argument)
			}
			entry.Arguments = arguments
			rebuilt = getTypeListKey(arguments)
		}
		reconstructed := CachedSignatureKey{sig: key.sig, key: rebuilt}
		if reconstructed != key || used[reconstructed] || cache[reconstructed] != value {
			panic("Global signature cache association mismatch")
		}
		used[reconstructed] = true
		entries = append(entries, entry)
	}
	if len(cache) != count || len(entries) != count || len(used) != count {
		panic("Signature cache cardinality changed")
	}
	return false, count, entries
}

// Only fields relevant to Name are populated. Nil values are preserved; a
// present null mapping is not replaced by an absent entry or fallback type.
type OracleCollectionsTypeCacheEntry struct {
	Value            *Type
	Owner            *Type
	Kind             int32
	Object, Index    *Type
	AccessFlags      uint32
	AliasSymbol      *ast.Symbol
	AliasArguments   []*Type
	Base, Constraint *Type
	Source, Target   *Type
	Texts            []string
	Types            []*Type
}

type OracleCollectionsTypeCache struct {
	Name    string
	IsNull  bool
	Count   int
	Entries []OracleCollectionsTypeCacheEntry
}

func OracleCollectionsTypeCaches(c *Checker, known map[TypeId]*Type) []OracleCollectionsTypeCache {
	output := make([]OracleCollectionsTypeCache, 0, 6)
	row := OracleCollectionsTypeCache{Name: "cached-types", IsNull: c.cachedTypes == nil, Count: len(c.cachedTypes)}
	if !row.IsNull {
		row.Entries = []OracleCollectionsTypeCacheEntry{}
	}
	for key, value := range c.cachedTypes {
		if key.kind < CachedTypeKindLiteralUnionBaseType || key.kind > CachedTypeKindSyntheticType {
			panic("Unknown global cached type kind")
		}
		owner := oracleCollectionsKnownType(c, known, key.typeId, fmt.Sprintf("cached-types kind=%d", key.kind))
		oracleCollectionsValidateStoredType(c, value)
		if key.kind == CachedTypeKindWidened {
			input, present := oracleCollectionsWidenedOwners[c][key.typeId]
			if !present || input.ambiguous || input.source != owner || input.value != value {
				panic("Missing or changed actual Widened cache publication")
			}
		}

		rebuilt := CachedTypeKey{kind: key.kind, typeId: owner.id}
		found, present := c.cachedTypes[rebuilt]
		if rebuilt != key || !present || found != value {
			panic("Typed global cache association mismatch")
		}
		row.Entries = append(row.Entries, OracleCollectionsTypeCacheEntry{Value: value, Owner: owner, Kind: int32(key.kind)})
	}
	if len(row.Entries) != row.Count || len(c.cachedTypes) != row.Count {
		panic("Typed global cache cardinality changed")
	}
	output = append(output, row)
	row = OracleCollectionsTypeCache{Name: "indexed-access", IsNull: c.indexedAccessTypes == nil, Count: len(c.indexedAccessTypes)}
	if !row.IsNull {
		row.Entries = []OracleCollectionsTypeCacheEntry{}
	}
	used := map[CacheHashKey]bool{}
	for key, value := range c.indexedAccessTypes {
		input, found := oracleCollectionsIndexedInputs[c][key]
		if !found || value == nil {
			panic("Missing indexed-access cache producer input or value")
		}
		oracleCollectionsValidateStoredType(c, value)
		oracleCollectionsCheckKnownType(c, known, input.object)
		oracleCollectionsCheckKnownType(c, known, input.index)
		indexed, ok := value.data.(*IndexedAccessType)
		if !ok || indexed.objectType != input.object || indexed.indexType != input.index || indexed.accessFlags != input.flags&AccessFlagsPersistent || value.alias != input.alias {
			panic("Indexed-access payload does not match original key producer")
		}
		if input.alias != nil {
			if input.alias.symbol == nil || OracleCollectionsStoredSymbolID(input.alias.symbol) == 0 {
				panic("Indexed cache alias symbol lacks allocated ID")
			}
			for _, argument := range input.alias.typeArguments {
				oracleCollectionsCheckKnownType(c, known, argument)
			}
		}
		rebuilt := getIndexedAccessKey(input.object, input.index, input.flags, input.alias)
		if rebuilt != key || used[rebuilt] || c.indexedAccessTypes[rebuilt] != value {
			panic("Indexed-access cache association mismatch")
		}
		used[rebuilt] = true
		entry := OracleCollectionsTypeCacheEntry{Value: value, Kind: -1, Object: input.object, Index: input.index, AccessFlags: uint32(input.flags)}
		if input.alias != nil {
			entry.AliasSymbol = input.alias.symbol
			entry.AliasArguments = append([]*Type{}, input.alias.typeArguments...)
		}
		row.Entries = append(row.Entries, entry)
	}
	if len(row.Entries) != row.Count || len(used) != row.Count || len(c.indexedAccessTypes) != row.Count {
		panic("Indexed cache cardinality changed")
	}
	output = append(output, row)
	row = OracleCollectionsTypeCache{Name: "substitution", IsNull: c.substitutionTypes == nil, Count: len(c.substitutionTypes)}
	if !row.IsNull {
		row.Entries = []OracleCollectionsTypeCacheEntry{}
	}
	for key, value := range c.substitutionTypes {
		base, constraint := oracleCollectionsKnownType(c, known, key.baseId), oracleCollectionsKnownType(c, known, key.constraintId)
		if value == nil {
			panic("Absent substitution cache value")
		}
		oracleCollectionsValidateStoredType(c, value)
		substitution, ok := value.data.(*SubstitutionType)
		if !ok || substitution.baseType != base || substitution.constraint != constraint {
			panic("Substitution payload/key mismatch")
		}
		rebuilt := SubstitutionTypeKey{baseId: base.id, constraintId: constraint.id}
		if rebuilt != key || c.substitutionTypes[rebuilt] != value {
			panic("Substitution cache association mismatch")
		}
		row.Entries = append(row.Entries, OracleCollectionsTypeCacheEntry{Value: value, Kind: -1, Base: base, Constraint: constraint})
	}
	if len(row.Entries) != row.Count || len(c.substitutionTypes) != row.Count {
		panic("Substitution cache cardinality changed")
	}
	output = append(output, row)
	row = OracleCollectionsTypeCache{Name: "template-literal", IsNull: c.templateLiteralTypes == nil, Count: len(c.templateLiteralTypes)}
	if !row.IsNull {
		row.Entries = []OracleCollectionsTypeCacheEntry{}
	}
	used = map[CacheHashKey]bool{}
	for key, value := range c.templateLiteralTypes {
		if value == nil {
			panic("Absent template cache value")
		}
		oracleCollectionsValidateStoredType(c, value)
		template, ok := value.data.(*TemplateLiteralType)
		if !ok || len(template.types) == 0 || len(template.texts) != len(template.types)+1 {
			panic("Invalid stored template cache payload")
		}
		for _, term := range template.types {
			oracleCollectionsCheckKnownType(c, known, term)
		}
		rebuilt := getTemplateTypeKey(template.texts, template.types)
		if rebuilt != key || used[rebuilt] || c.templateLiteralTypes[rebuilt] != value {
			panic("Template cache association mismatch")
		}
		used[rebuilt] = true
		row.Entries = append(row.Entries, OracleCollectionsTypeCacheEntry{Value: value, Kind: -1, Texts: template.texts, Types: template.types})
	}
	if len(row.Entries) != row.Count || len(used) != row.Count || len(c.templateLiteralTypes) != row.Count {
		panic("Template cache cardinality changed")
	}
	output = append(output, row)
	for _, named := range []struct {
		name  string
		cache map[ReverseMappedTypeKey]*Type
	}{{"reverse-homomorphic-mapped", c.reverseHomomorphicMappedCache}, {"reverse-mapped", c.reverseMappedCache}} {
		row = OracleCollectionsTypeCache{Name: named.name, IsNull: named.cache == nil, Count: len(named.cache)}
		if !row.IsNull {
			row.Entries = []OracleCollectionsTypeCacheEntry{}
		}
		for key, value := range named.cache {
			source, target, constraint := oracleCollectionsKnownType(c, known, key.sourceId), oracleCollectionsKnownType(c, known, key.targetId), oracleCollectionsKnownType(c, known, key.constraintId)
			oracleCollectionsValidateStoredType(c, value)
			rebuilt := ReverseMappedTypeKey{sourceId: source.id, targetId: target.id, constraintId: constraint.id}
			found, present := named.cache[rebuilt]
			if rebuilt != key || !present || found != value {
				panic("Reverse cache association mismatch")
			}
			row.Entries = append(row.Entries, OracleCollectionsTypeCacheEntry{Value: value, Kind: -1, Source: source, Target: target, Constraint: constraint})
		}
		if len(row.Entries) != row.Count || len(named.cache) != row.Count {
			panic("Reverse cache cardinality changed")
		}
		output = append(output, row)
	}
	return output
}

// Passive value/input roots, never semantic resolution or type-ID lookup. The
// consumer must canonically order genuine entities before assigning graph IDs.
func OracleCollectionsGlobalCacheValues(c *Checker) ([]*Type, []*Signature) {
	types := []*Type{}
	signatures := []*Signature{}
	add := func(t *Type) {
		if t != nil {
			oracleCollectionsValidateStoredType(c, t)
			types = append(types, t)
		}
	}
	for _, value := range c.tupleTypes {
		add(value)
	}
	for key, value := range c.cachedSignatures {
		signatures = append(signatures, key.sig, value)
		for _, argument := range oracleCollectionsSignatureArguments[c][key] {
			add(argument)
		}
	}
	for _, value := range c.cachedTypes {
		add(value)
	}
	for key, value := range c.indexedAccessTypes {
		add(value)
		if input, ok := oracleCollectionsIndexedInputs[c][key]; ok {
			add(input.object)
			add(input.index)
			if input.alias != nil {
				for _, argument := range input.alias.typeArguments {
					add(argument)
				}
			}
		}
	}
	for _, value := range c.substitutionTypes {
		add(value)
	}
	for _, value := range c.templateLiteralTypes {
		add(value)
	}
	for _, value := range c.reverseHomomorphicMappedCache {
		add(value)
	}
	for _, value := range c.reverseMappedCache {
		add(value)
	}
	return types, signatures
}

// Finite original Checker direct-field census, not automatic graph roots.
// Values (including null) are read without resolving globals or assigning IDs.
type OracleCollectionsStoredTypeSlot struct {
	Name      string
	Type      *Type
	Signature *Signature
}

func OracleCollectionsStoredTypeSlots(c *Checker) []OracleCollectionsStoredTypeSlot {
	return []OracleCollectionsStoredTypeSlot{
		{Name: "varianceTypeParameter", Type: c.varianceTypeParameter},
		{Name: "nanType", Type: c.nanType},
		{Name: "anyType", Type: c.anyType},
		{Name: "autoType", Type: c.autoType},
		{Name: "wildcardType", Type: c.wildcardType},
		{Name: "blockedStringType", Type: c.blockedStringType},
		{Name: "errorType", Type: c.errorType},
		{Name: "unresolvedType", Type: c.unresolvedType},
		{Name: "nonInferrableAnyType", Type: c.nonInferrableAnyType},
		{Name: "intrinsicMarkerType", Type: c.intrinsicMarkerType},
		{Name: "unknownType", Type: c.unknownType},
		{Name: "undefinedType", Type: c.undefinedType},
		{Name: "undefinedWideningType", Type: c.undefinedWideningType},
		{Name: "missingType", Type: c.missingType},
		{Name: "undefinedOrMissingType", Type: c.undefinedOrMissingType},
		{Name: "optionalType", Type: c.optionalType},
		{Name: "nullType", Type: c.nullType},
		{Name: "nullWideningType", Type: c.nullWideningType},
		{Name: "stringType", Type: c.stringType},
		{Name: "numberType", Type: c.numberType},
		{Name: "bigintType", Type: c.bigintType},
		{Name: "regularFalseType", Type: c.regularFalseType},
		{Name: "falseType", Type: c.falseType},
		{Name: "regularTrueType", Type: c.regularTrueType},
		{Name: "trueType", Type: c.trueType},
		{Name: "booleanType", Type: c.booleanType},
		{Name: "esSymbolType", Type: c.esSymbolType},
		{Name: "voidType", Type: c.voidType},
		{Name: "neverType", Type: c.neverType},
		{Name: "silentNeverType", Type: c.silentNeverType},
		{Name: "implicitNeverType", Type: c.implicitNeverType},
		{Name: "unreachableNeverType", Type: c.unreachableNeverType},
		{Name: "nonPrimitiveType", Type: c.nonPrimitiveType},
		{Name: "stringOrNumberType", Type: c.stringOrNumberType},
		{Name: "stringNumberSymbolType", Type: c.stringNumberSymbolType},
		{Name: "numberOrBigIntType", Type: c.numberOrBigIntType},
		{Name: "templateConstraintType", Type: c.templateConstraintType},
		{Name: "numericStringType", Type: c.numericStringType},
		{Name: "uniqueLiteralType", Type: c.uniqueLiteralType},
		{Name: "emptyObjectType", Type: c.emptyObjectType},
		{Name: "emptyJsxObjectType", Type: c.emptyJsxObjectType},
		{Name: "emptyFreshJsxObjectType", Type: c.emptyFreshJsxObjectType},
		{Name: "emptyTypeLiteralType", Type: c.emptyTypeLiteralType},
		{Name: "unknownEmptyObjectType", Type: c.unknownEmptyObjectType},
		{Name: "unknownUnionType", Type: c.unknownUnionType},
		{Name: "emptyGenericType", Type: c.emptyGenericType},
		{Name: "anyFunctionType", Type: c.anyFunctionType},
		{Name: "noConstraintType", Type: c.noConstraintType},
		{Name: "circularConstraintType", Type: c.circularConstraintType},
		{Name: "resolvingDefaultType", Type: c.resolvingDefaultType},
		{Name: "markerSuperType", Type: c.markerSuperType},
		{Name: "markerSubType", Type: c.markerSubType},
		{Name: "markerOtherType", Type: c.markerOtherType},
		{Name: "markerSuperTypeForCheck", Type: c.markerSuperTypeForCheck},
		{Name: "markerSubTypeForCheck", Type: c.markerSubTypeForCheck},
		{Name: "globalObjectType", Type: c.globalObjectType},
		{Name: "globalFunctionType", Type: c.globalFunctionType},
		{Name: "globalCallableFunctionType", Type: c.globalCallableFunctionType},
		{Name: "globalNewableFunctionType", Type: c.globalNewableFunctionType},
		{Name: "globalArrayType", Type: c.globalArrayType},
		{Name: "globalReadonlyArrayType", Type: c.globalReadonlyArrayType},
		{Name: "globalStringType", Type: c.globalStringType},
		{Name: "globalNumberType", Type: c.globalNumberType},
		{Name: "globalBooleanType", Type: c.globalBooleanType},
		{Name: "globalRegExpType", Type: c.globalRegExpType},
		{Name: "globalThisType", Type: c.globalThisType},
		{Name: "anyArrayType", Type: c.anyArrayType},
		{Name: "autoArrayType", Type: c.autoArrayType},
		{Name: "anyReadonlyArrayType", Type: c.anyReadonlyArrayType},
		{Name: "deferredGlobalImportMetaExpressionType", Type: c.deferredGlobalImportMetaExpressionType},
		{Name: "emptyStringType", Type: c.emptyStringType},
		{Name: "zeroType", Type: c.zeroType},
		{Name: "zeroBigIntType", Type: c.zeroBigIntType},
		{Name: "typeofType", Type: c.typeofType},
		{Name: "anySignature", Signature: c.anySignature},
		{Name: "unknownSignature", Signature: c.unknownSignature},
		{Name: "resolvingSignature", Signature: c.resolvingSignature},
		{Name: "silentNeverSignature", Signature: c.silentNeverSignature},
	}
}
