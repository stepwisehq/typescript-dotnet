// Independent structural observations; reflection reads original Go storage only.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"reflect"
)

type ObjectGraph struct {
	Types         []*ObjectTypeObservation  `json:"types"`
	PropertyLinks []PropertyLinkObservation `json:"propertyLinks"`
	Widenings     []WidenObservation        `json:"widenings"`
	Caches        []ObjectCacheObservation  `json:"caches"`
	RelationKeys  []ObjectKeyObservation    `json:"relationKeys"`
}
type ObjectKeyObservation struct {
	Name      string `json:"name"`
	BoolValue bool   `json:"boolValue"`
}
type ObjectTypeObservation struct {
	Type               int                         `json:"type"`
	Flags              uint32                      `json:"flags"`
	ObjectFlags        uint32                      `json:"objectFlags"`
	Members            *int                        `json:"members"`
	PropertiesNull     bool                        `json:"propertiesNull"`
	Properties         []ObjectPropertyObservation `json:"properties"`
	RawPropertiesNull  bool                        `json:"rawPropertiesNull"`
	RawProperties      []int                       `json:"rawProperties"`
	SignaturesNull     bool                        `json:"signaturesNull"`
	SignatureCount     int                         `json:"signatureCount"`
	CallSignatureCount int                         `json:"callSignatureCount"`
	IndexInfosNull     bool                        `json:"indexInfosNull"`
	IndexInfoCount     int                         `json:"indexInfoCount"`
	Target             *int                        `json:"target"`
}
type ObjectPropertyObservation struct {
	Symbol             int    `json:"symbol"`
	Type               int    `json:"type"`
	ModifierFlags      uint32 `json:"modifierFlags"`
	DeclarationSymbols []*int `json:"declarationSymbols"`
}
type PropertyLinkObservation struct {
	Symbol                       int  `json:"symbol"`
	Present                      bool `json:"present"`
	Target                       *int `json:"target"`
	ResolvedType                 *int `json:"resolvedType"`
	WriteType                    *int `json:"writeType"`
	NameType                     *int `json:"nameType"`
	ContainingType               *int `json:"containingType"`
	FunctionOrConstructorChecked bool `json:"functionOrConstructorChecked"`
}
type WidenObservation struct {
	Node             int  `json:"node"`
	Source           int  `json:"source"`
	Widened          int  `json:"widened"`
	RepeatedIdentity bool `json:"repeatedIdentity"`
}
type ObjectCacheObservation struct {
	Type    int  `json:"type"`
	Widened *int `json:"widened"`
	Regular *int `json:"regular"`
}

func (g *semanticGraph) rawType(id int) *checker.Type {
	for value, valueID := range g.types {
		if valueID == id {
			return value
		}
	}
	panic("Missing canonical type")
}
func (g *semanticGraph) objectGraph(widenNodes []int, queried map[int]*checker.Type) *ObjectGraph {
	out := &ObjectGraph{Types: []*ObjectTypeObservation{}, PropertyLinks: []PropertyLinkObservation{}, Widenings: []WidenObservation{}, Caches: []ObjectCacheObservation{}}
	for _, key := range checker.OracleObserveRelationKeyEquality(g.c) {
		out.RelationKeys = append(out.RelationKeys, ObjectKeyObservation{key.Name, key.BoolValue})
	}
	for _, node := range widenNodes {
		source := queried[node]
		if source == nil {
			panic("Widening source must be a queried node")
		}
		first := g.c.GetWidenedType(source)
		out.Widenings = append(out.Widenings, WidenObservation{node, *g.typ(source), *g.typ(first), first == g.c.GetWidenedType(source)})
	}
	propertySymbols := []*ast.Symbol{}
	seenProperties := map[*ast.Symbol]bool{}
	addProperty := func(symbol *ast.Symbol) {
		if symbol != nil && !seenProperties[symbol] {
			seenProperties[symbol] = true
			propertySymbols = append(propertySymbols, symbol)
		}
	}
	typeCursor, linkCursor := 0, 0
	for typeCursor < len(g.result.Types) || linkCursor < len(propertySymbols) {
		for typeCursor < len(g.result.Types) {
			id := typeCursor
			typeCursor++
			t := g.rawType(id)
			if t.Flags()&checker.TypeFlagsStructuredType == 0 {
				continue
			}
			props := g.c.GetPropertiesOfType(t)
			storage := t.AsStructuredType()
			value := reflect.ValueOf(storage).Elem()
			members := readable(value.FieldByName("members")).Interface().(ast.SymbolTable)
			signatures := readable(value.FieldByName("signatures"))
			indexes := readable(value.FieldByName("indexInfos"))
			calls := int(readable(value.FieldByName("callSignatureCount")).Int())
			if signatures.Len() != 0 || indexes.Len() != 0 || calls != 0 {
				panic("Non-data structured type in object oracle")
			}
			record := &ObjectTypeObservation{Type: id, Members: g.table(members), PropertiesNull: props == nil, Properties: []ObjectPropertyObservation{}, RawPropertiesNull: storage.Properties() == nil, RawProperties: []int{}, SignaturesNull: signatures.IsNil(), SignatureCount: signatures.Len(), CallSignatureCount: calls, IndexInfosNull: indexes.IsNil(), IndexInfoCount: indexes.Len()}
			for _, symbol := range storage.Properties() {
				record.RawProperties = append(record.RawProperties, *g.symbol(symbol))
			}
			if t.Flags()&checker.TypeFlagsObject != 0 {
				data := reflect.ValueOf(t.AsObjectType()).Elem()
				if !readable(data.FieldByName("mapper")).IsNil() || readable(data.FieldByName("instantiations")).Len() != 0 {
					panic("Object mapper/instantiation outside contract")
				}
				record.Target = g.typ(t.Target())
			}
			for _, symbol := range props {
				addProperty(symbol)
				observation := ObjectPropertyObservation{Symbol: *g.symbol(symbol), Type: *g.typ(g.c.GetTypeOfSymbol(symbol)), ModifierFlags: uint32(checker.GetDeclarationModifierFlagsFromSymbol(symbol)), DeclarationSymbols: []*int{}}
				for _, declaration := range symbol.Declarations {
					observation.DeclarationSymbols = append(observation.DeclarationSymbols, g.symbol(declaration.Symbol()))
				}
				record.Properties = append(record.Properties, observation)
			}
			out.Types = append(out.Types, record)
			if t.Flags()&checker.TypeFlagsObject != 0 {
				out.Caches = append(out.Caches, ObjectCacheObservation{id, g.typ(checker.OracleObserveObjectCache(g.c, t, "Widened")), g.typ(checker.OracleObserveObjectCache(g.c, t, "RegularObjectLiteral"))})
			}
		}
		for linkCursor < len(propertySymbols) {
			symbol := propertySymbols[linkCursor]
			linkCursor++
			links := checker.OracleObserveObjectValueLinks(g.c, symbol)
			addProperty(links.Target)
			out.PropertyLinks = append(out.PropertyLinks, PropertyLinkObservation{*g.symbol(symbol), links.Present, g.symbol(links.Target), g.typ(links.ResolvedType), g.typ(links.WriteType), g.typ(links.NameType), g.typ(links.ContainingType), links.FunctionOrConstructorChecked})
		}
	}
	for _, record := range out.Types {
		t := g.rawType(record.Type)
		record.Flags = uint32(t.Flags())
		record.ObjectFlags = uint32(t.ObjectFlags())
	}
	return out
}
