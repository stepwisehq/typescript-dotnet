// Authored metadata-only capture of the completed original relation-key stream.
// No constraint, type-argument, mapper, semantic getter or allocating ID calls.
package checker

import (
	"bytes"
	"encoding/binary"
	"sync"

	"github.com/zeebo/xxh3"
)

type oracleCollectionsRelationKeyInput struct {
	preimage       []byte
	invalid        bool
	source, target *Type
}

var oracleCollectionsRelationKeyLock sync.Mutex
var oracleCollectionsRelationKeys = map[*Checker]map[CacheHashKey]oracleCollectionsRelationKeyInput{}

func oracleCollectionsCaptureRelationKey(source, target *Type, b *keyBuilder, key CacheHashKey) {
	// Preserve original execution even if provenance is ambiguous; the read-only
	// accessor rejects such an entry rather than adding a checker failure here.
	owner := source.checker
	if owner == nil {
		owner = target.checker
	}
	if owner == nil {
		return
	}
	invalid := source.checker != nil && target.checker != nil && source.checker != target.checker
	stream := make([]byte, 0, len(b.overflowBuffer)+b.inlineLength)
	stream = append(stream, b.overflowBuffer...)
	stream = append(stream, b.inlineBuffer[:b.inlineLength]...)
	oracleCollectionsRelationKeyLock.Lock()
	defer oracleCollectionsRelationKeyLock.Unlock()
	entries := oracleCollectionsRelationKeys[owner]
	if entries == nil {
		entries = map[CacheHashKey]oracleCollectionsRelationKeyInput{}
		oracleCollectionsRelationKeys[owner] = entries
	}
	if previous, exists := entries[key]; exists {
		invalid = invalid || previous.invalid || !bytes.Equal(previous.preimage, stream)
	}
	entries[key] = oracleCollectionsRelationKeyInput{stream, invalid, source, target}
}

// Actual arguments already passed to original getRelationKey; never a reverse
// inference of generic-key parameter or wrapper identity. Only passive roots.
func OracleCollectionsRelationInputs(c *Checker) []*Type {
	oracleCollectionsRelationKeyLock.Lock()
	defer oracleCollectionsRelationKeyLock.Unlock()
	result := []*Type{}
	for _, input := range oracleCollectionsRelationKeys[c] {
		if input.invalid {
			panic("Ambiguous relation input provenance")
		}
		result = append(result, input.source, input.target)
	}
	return result
}

type OracleCollectionsRelationArgument struct {
	Kind      string
	Index     int
	Type      *Type
	Reference *OracleCollectionsRelationReference
}

type OracleCollectionsRelationReference struct {
	Target    *Type
	Arguments []OracleCollectionsRelationArgument
}

type OracleCollectionsRelationEntry struct {
	Family                           string
	Result                           uint32
	IntersectionState                uint32
	Source, Target                   *Type
	SourceReference, TargetReference *OracleCollectionsRelationReference
}

type OracleCollectionsRelationCache struct {
	Name    string
	IsNull  bool
	Count   int
	Entries []OracleCollectionsRelationEntry
}

func OracleCollectionsRelations(c *Checker, knownTypes map[TypeId]*Type) []OracleCollectionsRelationCache {
	caches := []struct {
		name     string
		relation *Relation
	}{
		{"assignable", c.assignableRelation},
		{"comparable", c.comparableRelation},
		{"identity", c.identityRelation},
		{"subtype", c.subtypeRelation},
		{"strict-subtype", c.strictSubtypeRelation},
	}
	oracleCollectionsRelationKeyLock.Lock()
	defer oracleCollectionsRelationKeyLock.Unlock()
	metadata := oracleCollectionsRelationKeys[c]
	output := make([]OracleCollectionsRelationCache, 0, len(caches))
	for _, named := range caches {
		cache := named.relation.results
		row := OracleCollectionsRelationCache{Name: named.name, IsNull: cache == nil, Count: len(cache)}
		if cache == nil {
			output = append(output, row)
			continue
		}
		row.Entries = make([]OracleCollectionsRelationEntry, 0, len(cache))
		used := map[CacheHashKey]bool{}
		for key, result := range cache {
			input, exists := metadata[key]
			if !exists || input.invalid {
				panic("Missing or ambiguous completed relation key provenance")
			}
			stream := input.preimage
			if len(stream) < 13 || CacheHashKey(xxh3.Hash128(stream)) != key {
				panic("Truncated or mismatched completed relation key preimage")
			}
			offset, end := 1, len(stream)-4
			readType := func() *Type {
				if offset > end-4 {
					panic("Truncated completed relation type term")
				}
				id := TypeId(binary.LittleEndian.Uint32(stream[offset : offset+4]))
				offset += 4
				t := knownTypes[id]
				if id == 0 || t == nil || t.id != id || t.checker != nil && t.checker != c {
					panic("Unrooted, unallocated or foreign completed relation type term")
				}
				return t
			}
			entry := OracleCollectionsRelationEntry{Result: uint32(result)}
			switch stream[0] {
			case 's':
				entry.Family = "simple"
				entry.Source, entry.Target = readType(), readType()
				if named.name == "identity" && entry.Source.id > entry.Target.id {
					panic("Unnormalized identity relation key")
				}
			case 'g':
				entry.Family = "generic-reference"
				parameterCount := uint64(0)
				var readReference func(int, byte) *OracleCollectionsRelationReference
				readReference = func(depth int, terminator byte) *OracleCollectionsRelationReference {
					if depth > 4 {
						panic("Completed relation reference exceeds original key depth")
					}
					reference := &OracleCollectionsRelationReference{Target: readType(), Arguments: []OracleCollectionsRelationArgument{}}
					for offset < end && stream[offset] != terminator {
						kind := stream[offset]
						offset++
						switch kind {
						case '=':
							if offset > end-8 {
								panic("Truncated normalized relation parameter term")
							}
							index := binary.LittleEndian.Uint64(stream[offset : offset+8])
							offset += 8
							if index > parameterCount {
								panic("Unknown normalized relation parameter index")
							}
							if index == parameterCount {
								parameterCount++
							}
							reference.Arguments = append(reference.Arguments, OracleCollectionsRelationArgument{Kind: "parameter", Index: int(index)})
						case '-':
							reference.Arguments = append(reference.Arguments, OracleCollectionsRelationArgument{Kind: "type", Index: -1, Type: readType()})
						case '<':
							nested := readReference(depth+1, '>')
							if offset >= end || stream[offset] != '>' {
								panic("Missing completed relation reference terminator")
							}
							offset++
							reference.Arguments = append(reference.Arguments, OracleCollectionsRelationArgument{Kind: "reference", Index: -1, Reference: nested})
						default:
							panic("Unknown completed relation argument term")
						}
					}
					return reference
				}
				entry.SourceReference = readReference(0, ',')
				if offset >= end || stream[offset] != ',' {
					panic("Missing completed relation direction separator")
				}
				offset++
				entry.TargetReference = readReference(0, 0)
			default:
				panic("Unknown completed relation key family")
			}
			if offset != end {
				panic("Trailing completed relation key terms")
			}
			entry.IntersectionState = binary.LittleEndian.Uint32(stream[end:])
			if entry.IntersectionState & ^uint32(IntersectionStateSource|IntersectionStateTarget) != 0 {
				panic("Unknown completed relation intersection state")
			}
			const mask = RelationComparisonResultSucceeded | RelationComparisonResultFailed | RelationComparisonResultReportsMask | RelationComparisonResultOverflow
			if result == RelationComparisonResultNone || result & ^mask != 0 {
				panic("Unknown completed relation result flags")
			}
			if used[key] || cache[key] != result {
				panic("Completed relation key association mismatch")
			}
			used[key] = true
			row.Entries = append(row.Entries, entry)
		}
		if len(cache) != row.Count || len(row.Entries) != row.Count || len(used) != row.Count {
			panic("Completed relation cache cardinality changed")
		}
		output = append(output, row)
	}
	return output
}

// Request-lifetime cleanup only, after the complete stored observation finishes.
func oracleCollectionsReleaseRelationKeys(c *Checker) {
	oracleCollectionsRelationKeyLock.Lock()
	defer oracleCollectionsRelationKeyLock.Unlock()
	delete(oracleCollectionsRelationKeys, c)
}
