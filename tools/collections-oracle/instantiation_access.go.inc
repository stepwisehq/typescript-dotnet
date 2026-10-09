// Authored metadata-only producer capture for keys whose result erases inputs.
// Calls are overlaid at the original stores after their values are computed.
package checker

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"reflect"
	"slices"
)

type oracleCollectionsInstantiationInput struct {
	checker        *Checker
	cachePointer   uintptr
	value          *Type
	arguments      []*Type
	aliasSymbol    *ast.Symbol
	aliasArguments []*Type
	family         string
	forConstraint  bool
}

var oracleCollectionsInstantiationInputs = map[any]map[CacheHashKey]oracleCollectionsInstantiationInput{}

func oracleCollectionsCaptureInstantiation(c *Checker, owner any, cache map[CacheHashKey]*Type, key CacheHashKey, arguments []*Type, alias *TypeAlias, family string, forConstraint bool, value *Type) {
	// No checker methods, IDs, key creation, mapper calls or semantic evaluation.
	entries := oracleCollectionsInstantiationInputs[owner]
	if entries == nil {
		entries = map[CacheHashKey]oracleCollectionsInstantiationInput{}
		oracleCollectionsInstantiationInputs[owner] = entries
	}
	var symbol *ast.Symbol
	var aliasArguments []*Type
	if alias != nil {
		symbol = alias.symbol
		aliasArguments = slices.Clone(alias.typeArguments)
	}
	entries[key] = oracleCollectionsInstantiationInput{c, uintptr(reflect.ValueOf(cache).UnsafePointer()), value, slices.Clone(arguments), symbol, aliasArguments, family, forConstraint}
}

type OracleCollectionsInstantiationEntry struct {
	Value          *Type
	Arguments      []*Type
	AliasSymbol    *ast.Symbol
	AliasArguments []*Type
	Family         string
	ForConstraint  bool
}

func OracleCollectionsInstantiations(c *Checker, owner any) (bool, int, []OracleCollectionsInstantiationEntry) {
	var cache map[CacheHashKey]*Type
	switch x := owner.(type) {
	case *TypeAliasLinks:
		cache = x.instantiations
	case *ConditionalRoot:
		cache = x.instantiations
	case *Type:
		cache = x.AsObjectType().instantiations
	default:
		panic("Unknown instantiation producer owner")
	}
	if cache == nil {
		return true, 0, nil
	}
	entries := []OracleCollectionsInstantiationEntry{}
	inputs := oracleCollectionsInstantiationInputs[owner]
	count := len(cache)
	used := map[CacheHashKey]bool{}
	validate := func(types []*Type) {
		for _, t := range types {
			if t == nil || t.id == 0 || t.checker != nil && t.checker != c {
				panic("Absent, unallocated or foreign instantiation producer argument")
			}
		}
	}
	for key, value := range cache {
		input, ok := inputs[key]
		if !ok || input.checker != c || input.cachePointer != uintptr(reflect.ValueOf(cache).UnsafePointer()) || input.value != value {
			panic("Missing or stale instantiation producer association")
		}
		validate(input.arguments)
		validate(input.aliasArguments)
		if input.aliasSymbol != nil && OracleCollectionsStoredSymbolID(input.aliasSymbol) == 0 {
			panic("Unallocated instantiation alias symbol")
		}
		var b keyBuilder
		b.writeTypes(input.arguments)
		if input.family != "alias-seed" {
			b.writeByte(0)
			if input.aliasSymbol != nil { // writeAlias equivalent, already allocated IDs only.
				b = keyBuilder{}
				b.writeTypes(input.arguments)
				b.writeByte(1)
				b.writeUint64(OracleCollectionsStoredSymbolID(input.aliasSymbol))
				b.writeTypes(input.aliasArguments)
			}
			if input.forConstraint {
				b.writeByte('!')
			}
		}
		reconstructed := b.hash()
		if key != reconstructed || cache[reconstructed] != value || used[reconstructed] {
			panic("Instantiation producer key membership mismatch")
		}
		used[reconstructed] = true
		// A source key encodes len, erasing nil versus empty input headers. Expose
		// encoded sequences, not irrecoverable producer header/alias-object identity.
		arguments := append([]*Type{}, input.arguments...)
		var aliasArgs []*Type
		if input.aliasSymbol != nil {
			aliasArgs = append([]*Type{}, input.aliasArguments...)
		}
		entries = append(entries, OracleCollectionsInstantiationEntry{value, arguments, input.aliasSymbol, aliasArgs, input.family, input.forConstraint})
	}
	if len(cache) != count || len(entries) != count || len(used) != count {
		panic("Instantiation producer cardinality changed")
	}
	return false, count, entries
}

func oracleCollectionsCaptureObjectInstantiation(c *Checker, owner *Type, cache map[CacheHashKey]*Type, key CacheHashKey, arguments []*Type, alias *TypeAlias, single bool, value *Type) {
	family := "anonymous"
	if owner.objectFlags&ObjectFlagsMapped != 0 {
		family = "mapped"
	} else if owner.objectFlags&ObjectFlagsReference != 0 {
		family = "deferred-reference"
	}
	oracleCollectionsCaptureInstantiation(c, owner, cache, key, arguments, alias, family, single, value)
}

func OracleCollectionsIsReducedObject(t *Type) bool {
	return t.objectFlags&ObjectFlagsMapped != 0 || t.AsTypeReference() != nil && t.AsInterfaceType() == nil && t.AsTypeReference().node != nil
}
