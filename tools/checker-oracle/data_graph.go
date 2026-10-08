// Independent discovery of original data-type storage; no generated C# schema.
package main

import (
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"reflect"
	"slices"
	"strings"
)

type DataGraph struct {
	PropertiesCacheNull bool                     `json:"propertiesCacheNull"`
	Declared            []DeclaredObservation    `json:"declared"`
	Types               []*DataTypeObservation   `json:"types"`
	Aliases             []*DataRecord            `json:"aliases"`
	Mappers             []*DataRecord            `json:"mappers"`
	Indexes             []*DataRecord            `json:"indexes"`
	Links               []DataLinkObservation    `json:"links"`
	Widenings           []WidenObservation       `json:"widenings"`
	Caches              []ObjectCacheObservation `json:"caches"`
	ResolvedSymbols     []DataResolvedSymbol     `json:"resolvedSymbols"`
	Keys                []DataKeyObservation     `json:"keys"`
}
type DataResolvedSymbol struct {
	Node   int  `json:"node"`
	Symbol *int `json:"symbol"`
}
type DataKeyObservation struct {
	Owner     int    `json:"owner"`
	Other     *int   `json:"other"`
	Name      string `json:"name"`
	BoolValue bool   `json:"boolValue"`
}
type DeclaredObservation struct {
	Node             int  `json:"node"`
	BoundSymbol      *int `json:"boundSymbol"`
	Symbol           int  `json:"symbol"`
	Type             int  `json:"type"`
	RepeatedIdentity bool `json:"repeatedIdentity"`
}
type DataRecord struct {
	ID       int         `json:"id"`
	DataType string      `json:"dataType"`
	Fields   []DataField `json:"fields"`
}
type DataTypeObservation struct {
	PropertiesCache []DataPropertiesEntry       `json:"propertiesCache"`
	Type            int                         `json:"type"`
	Alias           *int                        `json:"alias"`
	Fields          []DataField                 `json:"fields"`
	PropertiesNull  bool                        `json:"propertiesNull"`
	Properties      []ObjectPropertyObservation `json:"properties"`
}
type DataPropertiesEntry struct {
	Include       uint32 `json:"include"`
	IncludeOrigin bool   `json:"includeOrigin"`
	Value         *int   `json:"value"`
}
type DataLinkObservation struct {
	OwnerKind string      `json:"ownerKind"`
	Owner     int         `json:"owner"`
	Name      string      `json:"name"`
	Present   bool        `json:"present"`
	Fields    []DataField `json:"fields"`
}
type DataField struct {
	Name         string           `json:"name"`
	Kind         string           `json:"kind"`
	Null         bool             `json:"null"`
	Ref          *int             `json:"ref"`
	Refs         []int            `json:"refs"`
	Number       int64            `json:"number"`
	Bool         bool             `json:"bool"`
	Bytes        string           `json:"bytes"`
	Entries      []DataEntry      `json:"entries"`
	NamedEntries []DataNamedEntry `json:"namedEntries"`
}
type DataNamedEntry struct {
	Key  string `json:"keyBase64"`
	Node *int   `json:"node"`
}
type DataEntry struct {
	Key   int `json:"key"`
	Value int `json:"value"`
}
type dataEncoder struct {
	g       *semanticGraph
	out     *DataGraph
	aliases map[*checker.TypeAlias]int
	mappers map[*checker.TypeMapper]int
	indexes map[*checker.IndexInfo]int
}

// Cache-only identities are seeded independently of map/hash iteration. Require
// unique descriptors; a collision is an oracle error, never merged identity.
func (d *dataEncoder) seedKey(t *checker.Type) string {
	parts := []string{fmt.Sprint(uint32(t.Flags())), fmt.Sprint(uint32(t.ObjectFlags())), d.g.c.TypeToString(t)}
	if s := t.Symbol(); s != nil {
		parts = append(parts, s.Name)
		for _, n := range s.Declarations {
			parts = append(parts, fmt.Sprintf("%d:%d:%d", n.Kind, n.Pos(), n.End()))
		}
	}
	if r := t.AsTypeReference(); r != nil {
		value := reflect.ValueOf(r).Elem()
		args := readable(value.FieldByName("resolvedTypeArguments")).Interface().([]*checker.Type)
		for _, a := range args {
			parts = append(parts, fmt.Sprintf("arg:%d:%d:%s", a.Flags(), a.ObjectFlags(), d.g.c.TypeToString(a)))
			if s := a.Symbol(); s != nil {
				parts = append(parts, s.Name)
			}
		}
	}
	return strings.Join(parts, "\x00")
}

func (d *dataEncoder) alias(a *checker.TypeAlias) *int {
	if a == nil {
		return nil
	}
	if id, ok := d.aliases[a]; ok {
		return ptr(id)
	}
	id := len(d.out.Aliases)
	d.aliases[a] = id
	r := &DataRecord{ID: id, DataType: "TypeAlias", Fields: []DataField{}}
	d.out.Aliases = append(d.out.Aliases, r)
	r.Fields = d.fields(reflect.ValueOf(a).Elem())
	return ptr(id)
}
func (d *dataEncoder) mapper(m *checker.TypeMapper) *int {
	if m == nil {
		return nil
	}
	if id, ok := d.mappers[m]; ok {
		return ptr(id)
	}
	id := len(d.out.Mappers)
	d.mappers[m] = id
	payload := readable(reflect.ValueOf(m).Elem().FieldByName("data")).Elem().Elem()
	r := &DataRecord{ID: id, DataType: payload.Type().Name(), Fields: []DataField{}}
	d.out.Mappers = append(d.out.Mappers, r)
	r.Fields = d.fields(payload)
	return ptr(id)
}
func (d *dataEncoder) index(i *checker.IndexInfo) *int {
	if i == nil {
		return nil
	}
	if id, ok := d.indexes[i]; ok {
		return ptr(id)
	}
	id := len(d.out.Indexes)
	d.indexes[i] = id
	r := &DataRecord{ID: id, DataType: "IndexInfo", Fields: []DataField{}}
	d.out.Indexes = append(d.out.Indexes, r)
	r.Fields = d.fields(reflect.ValueOf(i).Elem())
	return ptr(id)
}
func (d *dataEncoder) fields(value reflect.Value) []DataField {
	result := []DataField{}
	typ := value.Type()
	for i := 0; i < value.NumField(); i++ {
		schema := typ.Field(i)
		field := readable(value.Field(i))
		if schema.Anonymous {
			if schema.Name == "Type" || schema.Name == "TypeMapper" {
				continue
			} // wrapper already canonical; never duplicate owner/id/data backrefs
			result = append(result, d.fields(field)...)
			continue
		}
		if schema.Name == "c" && field.Type() == reflect.TypeFor[*checker.Checker]() {
			if field.Interface().(*checker.Checker) != d.g.c {
				panic("Foreign mapper checker")
			}
			continue // irrelevant verified owner backref
		}
		result = append(result, d.field(schema.Name, field))
	}
	return result
}
func (d *dataEncoder) field(name string, v reflect.Value) DataField {
	f := DataField{Name: name, Refs: []int{}, Entries: []DataEntry{}, NamedEntries: []DataNamedEntry{}}
	value := v.Interface()
	switch x := value.(type) {
	case *checker.Type:
		f.Kind = "type"
		f.Null = x == nil
		f.Ref = d.g.typ(x)
	case *ast.Symbol:
		f.Kind = "symbol"
		f.Null = x == nil
		f.Ref = d.g.symbol(x)
	case *ast.Node:
		f.Kind = "node"
		f.Null = x == nil
		f.Ref = d.g.node(x)
	case *checker.TypeMapper:
		f.Kind = "mapper"
		f.Null = x == nil
		f.Ref = d.mapper(x)
	case ast.SymbolTable:
		f.Kind = "table"
		f.Null = x == nil
		f.Ref = d.g.table(x)
	case []*checker.Type:
		f.Kind = "types"
		f.Null = x == nil
		for _, item := range x {
			f.Refs = append(f.Refs, *d.g.typ(item))
		}
	case []*ast.Symbol:
		f.Kind = "symbols"
		f.Null = x == nil
		for _, item := range x {
			f.Refs = append(f.Refs, *d.g.symbol(item))
		}
	case []*ast.Node:
		f.Kind = "nodes"
		f.Null = x == nil
		for _, item := range x {
			f.Refs = append(f.Refs, *d.g.node(item))
		}
	case []*checker.IndexInfo:
		f.Kind = "indexes"
		f.Null = x == nil
		for _, item := range x {
			f.Refs = append(f.Refs, *d.index(item))
		}
	case []*checker.Signature:
		f.Kind = "signatures"
		f.Null = x == nil
		if len(x) != 0 {
			panic("Source signature dependency outside data contract")
		}
	case map[checker.CacheHashKey]*checker.Type:
		f.Kind = "instantiations"
		f.Null = x == nil
		// Hash representations differ intentionally. Preserve exact value identities;
		// actual key equality is checked by independent private-helper observations.
		pending := []*checker.Type{}
		for _, item := range x {
			pending = append(pending, item)
		}
		unrooted := []*checker.Type{}
		keys := map[*checker.Type]string{}
		used := map[string]*checker.Type{}
		for _, item := range pending {
			if _, ok := d.g.types[item]; !ok {
				key := d.seedKey(item)
				if previous := used[key]; previous != nil && previous != item {
					panic("Ambiguous instantiation seed descriptor")
				}
				used[key] = item
				keys[item] = key
				unrooted = append(unrooted, item)
			}
		}
		slices.SortFunc(unrooted, func(a, b *checker.Type) int { return strings.Compare(keys[a], keys[b]) })
		for _, item := range unrooted {
			d.g.typ(item)
		}
		slices.SortFunc(pending, func(a, b *checker.Type) int {
			aid, aok := d.g.types[a]
			bid, bok := d.g.types[b]
			if !aok || !bok {
				panic("Unrooted instantiation cache type requires explicit canonical seed")
			}
			return aid - bid
		})
		for _, item := range pending {
			if _, ok := d.g.types[item]; !ok {
				panic("Unrooted instantiation cache type")
			}
			f.Refs = append(f.Refs, *d.g.typ(item))
		}
	case map[string]*ast.Node:
		f.Kind = "node-map"
		f.Null = x == nil
		keys := []string{}
		for key := range x {
			keys = append(keys, key)
		}
		slices.Sort(keys)
		for _, key := range keys {
			f.NamedEntries = append(f.NamedEntries, DataNamedEntry{bytes(key), d.g.node(x[key])})
		}
	case map[*checker.Type]*checker.Type:
		f.Kind = "type-map"
		f.Null = x == nil
		keys := []*checker.Type{}
		for key := range x {
			keys = append(keys, key)
		}
		for _, key := range keys {
			if _, ok := d.g.types[key]; !ok {
				panic("Unrooted discriminant key requires canonical seed")
			}
		}
		slices.SortFunc(keys, func(a, b *checker.Type) int { return *d.g.typ(a) - *d.g.typ(b) })
		for _, key := range keys {
			f.Entries = append(f.Entries, DataEntry{*d.g.typ(key), *d.g.typ(x[key])})
		}
	case string:
		f.Kind = "bytes"
		f.Bytes = bytes(x)
	case bool:
		f.Kind = "bool"
		f.Bool = x
	default:
		switch v.Kind() {
		case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
			f.Kind = "number"
			f.Number = v.Int()
		case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
			f.Kind = "number"
			f.Number = int64(v.Uint())
		default:
			panic(fmt.Sprintf("Unclassified data field %s: %s", name, v.Type()))
		}
	}
	return f
}
func (g *semanticGraph) dataGraph(r SemanticRequest, queried map[int]*checker.Type) *DataGraph {
	out := &DataGraph{Declared: []DeclaredObservation{}, Types: []*DataTypeObservation{}, Aliases: []*DataRecord{}, Mappers: []*DataRecord{}, Indexes: []*DataRecord{}, Links: []DataLinkObservation{}, Widenings: []WidenObservation{}, Caches: []ObjectCacheObservation{}}
	d := dataEncoder{g: g, out: out, aliases: map[*checker.TypeAlias]int{}, mappers: map[*checker.TypeMapper]int{}, indexes: map[*checker.IndexInfo]int{}}
	out.PropertiesCacheNull = checker.OracleObserveDataPropertiesTypesNil(g.c)
	for _, id := range r.DeclaredNodeIDs {
		node := g.syntax.raw[id]
		symbol := g.c.GetSymbolAtLocation(node.Name())
		if symbol == nil {
			panic("Missing named declaration symbol")
		}
		t := g.c.GetDeclaredTypeOfSymbol(symbol)
		out.Declared = append(out.Declared, DeclaredObservation{id, g.symbol(node.Symbol()), *g.symbol(symbol), *g.typ(t), t == g.c.GetDeclaredTypeOfSymbol(symbol)})
	}
	for _, id := range r.WidenNodeIDs {
		source := queried[id]
		if source == nil {
			panic("Missing queried widen node")
		}
		wide := g.c.GetWidenedType(source)
		out.Widenings = append(out.Widenings, WidenObservation{id, *g.typ(source), *g.typ(wide), wide == g.c.GetWidenedType(source)})
	}
	// Public properties first; snapshot underlying storage only after resolving
	// every discovered property type in canonical order. Work queue handles cycles.
	out.ResolvedSymbols = []DataResolvedSymbol{}
	out.Keys = []DataKeyObservation{}
	for id, n := range g.syntax.raw {
		for _, link := range checker.OracleObserveDataNodeLinks(g.c, n) {
			item := DataLinkObservation{OwnerKind: "node", Owner: id, Name: link.Name, Present: link.Data != nil, Fields: []DataField{}}
			if link.Data != nil {
				item.Fields = d.fields(reflect.ValueOf(link.Data).Elem())
			}
			out.Links = append(out.Links, item)
		}
		out.ResolvedSymbols = append(out.ResolvedSymbols, DataResolvedSymbol{id, g.symbol(checker.OracleObserveDataResolvedSymbol(g.c, n))})
	}
	cursor, linkCursor := 0, 0
	for cursor < len(g.result.Types) || linkCursor < len(g.result.Symbols) {
		for cursor < len(g.result.Types) {
			id := cursor
			cursor++
			t := g.rawType(id)
			record := &DataTypeObservation{Type: id, Alias: d.alias(t.Alias()), Fields: []DataField{}, Properties: []ObjectPropertyObservation{}, PropertiesCache: []DataPropertiesEntry{}}
			out.Types = append(out.Types, record)
			if t.Flags()&checker.TypeFlagsStructuredType != 0 {
				props := g.c.GetPropertiesOfType(t)
				record.PropertiesNull = props == nil
				for _, s := range props {
					item := ObjectPropertyObservation{Symbol: *g.symbol(s), Type: *g.typ(g.c.GetTypeOfSymbol(s)), ModifierFlags: uint32(checker.GetDeclarationModifierFlagsFromSymbol(s)), DeclarationSymbols: []*int{}}
					for _, node := range s.Declarations {
						item.DeclarationSymbols = append(item.DeclarationSymbols, g.symbol(node.Symbol()))
					}
					record.Properties = append(record.Properties, item)
				}
			}
			payload := readable(reflect.ValueOf(t).Elem().FieldByName("data")).Elem().Elem()
			if payload.Type().Name() != "IntrinsicType" && payload.Type().Name() != "LiteralType" {
				record.Fields = d.fields(payload)
			} // exact leaf payload already encoded by base graph
			for _, entry := range checker.OracleObserveDataPropertiesTypes(g.c, t) {
				record.PropertiesCache = append(record.PropertiesCache, DataPropertiesEntry{uint32(entry.Include), entry.IncludeOrigin, g.typ(entry.Value)})
			}
			if t.Flags()&checker.TypeFlagsObject != 0 {
				out.Caches = append(out.Caches, ObjectCacheObservation{id, g.typ(checker.OracleObserveObjectCache(g.c, t, "Widened")), g.typ(checker.OracleObserveObjectCache(g.c, t, "RegularObjectLiteral"))})
			}
		}
		// Link stores are observational TryGet only: no cache materialization.
		for linkCursor < len(g.result.Symbols) {
			id := linkCursor
			linkCursor++
			var symbol *ast.Symbol
			for s, sid := range g.symbols {
				if sid == id {
					symbol = s
					break
				}
			}
			for _, link := range checker.OracleObserveDataSymbolLinks(g.c, symbol) {
				item := DataLinkObservation{OwnerKind: "symbol", Owner: id, Name: link.Name, Present: link.Data != nil, Fields: []DataField{}}
				if link.Data != nil {
					item.Fields = d.fields(reflect.ValueOf(link.Data).Elem())
				}
				out.Links = append(out.Links, item)
			}
			merged, present := checker.OracleObserveDataMergedSymbol(g.c, symbol)
			fields := []DataField{}
			if present {
				fields = append(fields, d.field("target", reflect.ValueOf(merged)))
			}
			out.Links = append(out.Links, DataLinkObservation{"symbol", id, "mergedSymbols", present, fields})
		}
	}
	for _, t := range out.Types {
		value := g.rawType(t.Type)
		g.result.Types[t.Type].Flags = uint32(value.Flags())
		g.result.Types[t.Type].ObjectFlags = uint32(value.ObjectFlags())
	}
	for _, record := range out.Types {
		t := g.rawType(record.Type)
		if t.ObjectFlags()&checker.ObjectFlagsReference != 0 {
			for _, key := range checker.OracleObserveDataReferenceKeys(t) {
				out.Keys = append(out.Keys, DataKeyObservation{record.Type, ptr(record.Type), key.Name, key.BoolValue})
			}
		}
		if t.Flags()&checker.TypeFlagsUnionOrIntersection != 0 {
			out.Keys = append(out.Keys, DataKeyObservation{record.Type, nil, "composite.intern-membership", checker.OracleObserveDataInternMembership(g.c, t)})
		}
		for _, field := range record.Fields {
			if field.Kind == "instantiations" {
				if len(field.Refs) > 0 {
					out.Keys = append(out.Keys, DataKeyObservation{record.Type, nil, "reference.instantiations-all-key-associated", checker.OracleObserveDataCacheAssociation(g.c, t)})
				}
				for _, id := range field.Refs {
					out.Keys = append(out.Keys, DataKeyObservation{record.Type, ptr(id), "reference.instantiation-membership", checker.OracleObserveDataKeyMembership(g.c, t, g.rawType(id))})
				}
			}
		}
	}
	if len(g.result.Types) > 1 {
		var a1, a2 *checker.TypeAlias
		for _, record := range out.Types {
			if a := g.rawType(record.Type).Alias(); a != nil {
				if a1 == nil {
					a1 = a
				} else if a2 == nil && a != a1 {
					a2 = a
				}
			}
		}
		for _, key := range checker.OracleObserveDataKeyEquality(g.rawType(0), g.rawType(1), a1, a2) {
			out.Keys = append(out.Keys, DataKeyObservation{0, ptr(1), key.Name, key.BoolValue})
		}
	}
	return out
}
