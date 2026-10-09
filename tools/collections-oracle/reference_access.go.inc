// Authored read-only private observation. No source compiler behavior changes.
package checker

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"reflect"
	"sync/atomic"
	"unsafe"
)

// Authored read-only atomic field access, never the allocating GetSymbolId API.
func OracleCollectionsStoredSymbolID(symbol *ast.Symbol) uint64 {
	v := reflect.ValueOf(symbol).Elem().FieldByName("id")
	return (*atomic.Uint64)(unsafe.Pointer(v.UnsafeAddr())).Load()
}

type OracleCollectionsReferenceEntry struct {
	Value     *Type
	Arguments []*Type
	Target    *Type
	IsSelf    bool
}

func OracleCollectionsReferenceInstantiations(c *Checker, owner *Type) (bool, int, []OracleCollectionsReferenceEntry) {
	if owner == nil || owner.AsObjectType() == nil {
		panic("Reference cache owner is not an object")
	}
	if owner.checker != nil && owner.checker != c {
		panic("Foreign reference cache owner")
	}
	cache := owner.AsObjectType().instantiations
	if cache == nil {
		return true, 0, nil
	}
	entries := []OracleCollectionsReferenceEntry{}
	if len(cache) == 0 {
		return false, 0, entries
	}
	if owner.AsInterfaceType() == nil || owner.objectFlags&ObjectFlagsReference == 0 || owner.AsTypeReference().target != owner {
		panic("Nonempty object cache outside retained reference-target family")
	}
	used := map[CacheHashKey]bool{}
	for key, value := range cache {
		if value == nil || value.AsTypeReference() == nil || value.AsTypeReference().target != owner {
			panic("Stored reference value has another target")
		}
		if value.checker != nil && value.checker != c {
			panic("Foreign reference cache value")
		}
		arguments := value.AsTypeReference().resolvedTypeArguments
		for _, argument := range arguments {
			if argument == nil || argument.id == 0 || argument.checker != nil && argument.checker != c {
				panic("Absent, unallocated or foreign reference argument")
			}
		}
		reconstructed := getTypeListKey(arguments)
		if reconstructed != key || used[reconstructed] || cache[reconstructed] != value {
			panic("Reference cache association mismatch")
		}
		used[reconstructed] = true
		entries = append(entries, OracleCollectionsReferenceEntry{value, arguments, value.AsTypeReference().target, value == owner})
	}
	if len(entries) != len(cache) || len(used) != len(cache) {
		panic("Reference cache cardinality changed")
	}
	return false, len(cache), entries
}

// Anonymous cache keys are reconstructed only from actual retained outer
// parameters, mapper sources/targets and alias. No mapper is invoked.
type OracleCollectionsObjectEntry struct {
	Value           *Type
	Arguments       []*Type
	Alias           *TypeAlias
	SingleSignature bool
	Family          string
	Target          *Type
	IsSelf          bool
}

func OracleCollectionsObjectInstantiations(c *Checker, owner *Type) (bool, int, []OracleCollectionsObjectEntry) {
	if owner == nil || owner.AsObjectType() == nil || owner.checker != nil && owner.checker != c {
		panic("Absent, nonobject or foreign object cache owner")
	}
	cache := owner.AsObjectType().instantiations
	if cache == nil {
		return true, 0, nil
	}
	entries := []OracleCollectionsObjectEntry{}
	if len(cache) == 0 {
		return false, 0, entries
	}
	if owner.AsInterfaceType() != nil && owner.objectFlags&ObjectFlagsReference != 0 {
		_, count, refs := OracleCollectionsReferenceInstantiations(c, owner)
		for _, entry := range refs {
			entries = append(entries, OracleCollectionsObjectEntry{Value: entry.Value, Arguments: entry.Arguments, Family: "reference", Target: entry.Target, IsSelf: entry.IsSelf})
		}
		return false, count, entries
	}
	if owner.objectFlags&ObjectFlagsAnonymous == 0 {
		panic("Unknown nonempty instantiated-object key family")
	}
	if owner.symbol == nil || len(owner.symbol.Declarations) == 0 {
		panic("Anonymous cache owner has no actual declaration")
	}
	links := c.typeNodeLinks.TryGet(owner.symbol.Declarations[0])
	if links == nil || len(links.outerTypeParameters) == 0 {
		panic("Anonymous cache has no stored outer type parameters")
	}
	parameters := links.outerTypeParameters
	validate := func(types []*Type) {
		for _, t := range types {
			if t == nil || t.id == 0 || t.checker != nil && t.checker != c {
				panic("Anonymous key type is absent, unallocated or foreign")
			}
		}
	}
	validate(parameters)
	count := len(cache)
	used := map[CacheHashKey]bool{}
	for key, value := range cache {
		if value == nil || value.checker != nil && value.checker != c {
			panic("Absent or foreign anonymous cache value")
		}
		self := value == owner
		single := false
		var arguments []*Type
		var alias *TypeAlias
		if self {
			arguments = parameters
			alias = owner.alias
		} else {
			if value.Target() != owner {
				panic("Anonymous cache value has another target")
			}
			single = owner.objectFlags&ObjectFlagsSingleSignatureType != 0
			mapper := value.Mapper()
			if mapper != nil && single {
				if composite, ok := mapper.data.(*CompositeTypeMapper); ok {
					mapper = composite.m1
				}
			}
			if mapper == nil {
				panic("Missing stored anonymous mapping")
			}
			switch m := mapper.data.(type) {
			case *SimpleTypeMapper:
				if len(parameters) != 1 || m.source != parameters[0] {
					panic("Simple anonymous mapping source mismatch")
				}
				arguments = []*Type{m.target}
			case *ArrayTypeMapper:
				if len(m.sources) != len(parameters) || len(m.targets) != len(parameters) {
					panic("Array anonymous mapping count mismatch")
				}
				for i, t := range parameters {
					if m.sources[i] != t {
						panic("Array anonymous mapping source mismatch")
					}
				}
				arguments = m.targets
			default:
				panic("Unknown stored anonymous key mapping")
			}
			alias = value.alias
		}
		validate(arguments)
		if alias != nil {
			if alias.symbol == nil || OracleCollectionsStoredSymbolID(alias.symbol) == 0 {
				panic("Anonymous cache alias lacks a stored symbol ID")
			}
			validate(alias.typeArguments)
		}
		reconstructed := getTypeInstantiationKey(arguments, alias, single)
		if reconstructed != key || used[key] || cache[reconstructed] != value {
			panic("Anonymous cache key association mismatch")
		}
		used[key] = true
		entries = append(entries, OracleCollectionsObjectEntry{value, arguments, alias, single, "anonymous", owner, self})
	}
	if len(cache) != count || len(used) != count || len(entries) != count {
		panic("Anonymous cache cardinality changed")
	}
	return false, count, entries
}

func OracleCollectionsStoredPropertyType(c *Checker, symbol *ast.Symbol) *Type {
	// TryGet uses arena IDs. Never call it for an unallocated symbol while
	// deriving ordering descriptors; an absent stored ID means no arena slot.
	if OracleCollectionsStoredSymbolID(symbol) == 0 {
		return nil
	}
	if links := c.valueSymbolLinks.TryGet(symbol); links != nil {
		return links.resolvedType
	}
	return nil
}
func OracleCollectionsStoredMembers(owner *Type) ast.SymbolTable {
	return owner.AsStructuredType().members
}
