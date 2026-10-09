// Authored nonforcing storage observer. Original payload fields are discovered
// independently through reflection; no type/member/signature resolver is called.
package main

import (
	"encoding/base64"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"github.com/microsoft/TypeScript/tsc/internal/collections"
	"math"
	"reflect"
	"sort"
	"strings"
	"sync/atomic"
)

type StoredValue struct {
	Kind   string         `json:"kind"`
	Null   bool           `json:"null"`
	Ref    *int           `json:"ref"`
	Values []*StoredValue `json:"values"`
	Fields []StoredField  `json:"fields"`
	Bytes  string         `json:"bytes"`
	Number int64          `json:"number"`
	Bool   bool           `json:"bool"`
}
type StoredField struct {
	Name  string       `json:"name"`
	Value *StoredValue `json:"value"`
}
type StoredRecord struct {
	ID     int           `json:"id"`
	Name   string        `json:"name"`
	Fields []StoredField `json:"fields"`
}
type StoredNodeType struct {
	Node int  `json:"node"`
	Type *int `json:"type"`
}
type StoredState struct {
	OwnerKind string       `json:"ownerKind"`
	Owner     int          `json:"owner"`
	Store     string       `json:"store"`
	Value     *StoredValue `json:"value"`
}
type StoredGraph struct {
	GlobalCaches   []StoredField     `json:"globalCaches"`
	Inventories    []StoredInventory `json:"inventories"`
	States         []StoredState     `json:"states"`
	Roots          []*int            `json:"roots"`
	SyntheticTypes []StoredNodeType  `json:"syntheticTypes"`
	Records        []*StoredRecord   `json:"records"`
}
type StoredInventory struct {
	Store     string `json:"store"`
	OwnerKind string `json:"ownerKind"`
	Null      bool   `json:"null"`
	Count     int    `json:"count"`
	Owners    []int  `json:"owners"`
}
type storedEncoder struct {
	c       *checker.Checker
	binding *graph
	ids     map[any]int
	queue   []any
	out     StoredGraph
}

func newStoredEncoder(c *checker.Checker, g *graph) *storedEncoder {
	return &storedEncoder{c: c, binding: g, ids: map[any]int{}, out: StoredGraph{Roots: []*int{}, Records: []*StoredRecord{}, SyntheticTypes: []StoredNodeType{}, States: []StoredState{}}}
}
func (s *storedEncoder) ref(x any) *int {
	if x == nil {
		return nil
	}
	v := reflect.ValueOf(x)
	if v.Kind() == reflect.Pointer && v.IsNil() {
		return nil
	}
	if id, ok := s.ids[x]; ok {
		return ptr(id)
	}
	id := len(s.queue)
	s.ids[x] = id
	s.queue = append(s.queue, x)
	s.out.Records = append(s.out.Records, &StoredRecord{ID: id, Fields: []StoredField{}})
	return ptr(id)
}
func storageName(v reflect.Value) string { return v.Type().Elem().Name() }

type rawStoredField struct {
	name  string
	value reflect.Value
}

func (s *storedEncoder) rawFields(v reflect.Value) []rawStoredField {
	out := []rawStoredField{}
	for i := 0; i < v.NumField(); i++ {
		f := v.Type().Field(i)
		value := readable(v.Field(i))
		if f.Anonymous {
			if f.Name != "Type" && f.Name != "TypeMapper" {
				out = append(out, s.rawFields(value)...)
			}
			continue
		}
		if f.Name == "checker" || f.Name == "c" {
			if !value.IsNil() && value.Interface() != s.c {
				panic("Foreign stored checker ownership")
			}
			continue
		}
		if f.Name == "data" {
			continue
		}
		name := f.Name
		if name == "id" {
			name = "idAllocated"
			value = reflect.ValueOf(value.Uint() != 0)
		}
		out = append(out, rawStoredField{name, value})
	}
	return out
}
func (s *storedEncoder) renderFields(raw []rawStoredField, owner any) []StoredField {
	sort.Slice(raw, func(i, j int) bool {
		if raw[i].name == "instantiations" {
			return false
		}
		if raw[j].name == "instantiations" {
			return true
		}
		return raw[i].name < raw[j].name
	})
	out := []StoredField{}
	for i, f := range raw {
		if i > 0 && raw[i-1].name == f.name {
			panic("Duplicate flattened stored field " + f.name)
		}
		var value *StoredValue
		if context, ok := owner.(*checker.InferenceContext); ok && f.name == "compareTypes" && !f.value.IsNil() {
			value = s.comparer(context)
		} else if m, ok := owner.(*checker.TypeMapper); ok && f.name == "targets" && checker.OracleCollectionsIsDeferred(m) {
			value = s.deferredTargets(m)
		} else if f.name == "instantiations" && owner != nil && f.value.Kind() == reflect.Map && f.value.Len() != 0 {
			switch x := owner.(type) {
			case *checker.Type:
				if checker.OracleCollectionsIsReducedObject(x) {
					value = s.instantiationCache(x, true)
				} else {
					value = s.referenceCache(x)
				}
			case *checker.TypeAliasLinks, *checker.ConditionalRoot:
				value = s.instantiationCache(owner, false)
			default:
				panic("Unknown nonempty instantiation owner")
			}
		} else {
			value = s.value(f.value)
		}
		out = append(out, StoredField{f.name, value})
	}
	return out
}
func (s *storedEncoder) fields(v reflect.Value) []StoredField {
	return s.renderFields(s.rawFields(v), nil)
}
func (s *storedEncoder) describe(x any) (string, []StoredField) {
	if owner, ok := x.(*checker.Relater); ok {
		return "RelaterComparerOwner", []StoredField{{"currentRelation", s.value(reflect.ValueOf(checker.OracleCollectionsCurrentRelation(s.c, owner)))}}
	}
	v := reflect.ValueOf(x).Elem()
	name := v.Type().Name()
	if v.Kind() == reflect.Array {
		return name, []StoredField{{"tables", s.value(v)}}
	}
	raw := s.rawFields(v)
	owner := x
	switch x.(type) {
	case *checker.Type, *checker.TypeMapper:
		d := readable(v.FieldByName("data"))
		if d.IsNil() {
			panic("Nil storage payload")
		}
		p := reflect.ValueOf(d.Interface())
		name = p.Type().Elem().Name()
		if t, ok := x.(*checker.Type); ok {
			owner = t
			raw = append(raw, s.rawFields(p.Elem())...)
		} else {
			raw = append(raw, s.rawFields(p.Elem())...)
		}
	}
	fields := s.renderFields(raw, owner)
	if t, ok := x.(*checker.Type); ok {
		if origin, present := checker.OracleCollectionsObjectProducer(s.c, t); present {
			fields = append(fields, StoredField{"authoredObjectProducer", s.objectProducer(origin)})
		}
	}
	if t, ok := x.(*checker.Type); ok && t.Flags()&(checker.TypeFlagsUnion|checker.TypeFlagsIntersection) != 0 {
		normal, star := checker.OracleCollectionsInternMembership(s.c, t)
		fields = append(fields, StoredField{"authoredInternMembership", inlineStored(StoredField{"noConstraintReduction", s.val(star)}, StoredField{"normal", s.val(normal)})})
	}
	if t, ok := x.(*checker.Type); ok && t.Flags()&checker.TypeFlagsUniqueESSymbol != 0 {
		owner := t.Symbol()
		if owner == nil {
			panic("Unique symbol lacks actual owner")
		}
		payload := reflect.ValueOf(readable(v.FieldByName("data")).Interface()).Elem()
		actual := readable(payload.FieldByName("name")).String()
		storedID := readable(reflect.ValueOf(owner).Elem().FieldByName("id")).Addr().Interface().(*atomic.Uint64).Load()
		if storedID == 0 {
			panic("Unique symbol owner lacks already allocated ID")
		}
		expected := ast.InternalSymbolNamePrefix + "@" + owner.Name + "@" + fmt.Sprint(storedID)
		if actual != expected {
			panic("Unique symbol name preimage mismatch")
		}
		for i := range fields {
			if fields[i].Name == "name" {
				value := &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: []StoredField{
					{"symbol", s.value(reflect.ValueOf(owner))},
					{"symbolName", s.value(reflect.ValueOf(owner.Name))},
				}}
				fields[i].Value = value
			}
		}
	}
	sort.Slice(fields, func(i, j int) bool { return fields[i].Name < fields[j].Name })
	return name, fields
}

func (s *storedEncoder) applicabilityValue(a *checker.OracleCollectionsApplicabilityOrigin) *StoredValue {
	if a == nil {
		return &StoredValue{Kind: "inline", Null: true, Values: []*StoredValue{}, Fields: []StoredField{}}
	}
	var report any
	if a.ReportErrors != nil {
		report = *a.ReportErrors
	}
	return inlineStored(StoredField{"checkMode", s.val(a.Mode)}, StoredField{"node", s.val(a.Node)}, StoredField{"phase", s.val(a.Phase)}, StoredField{"relation", s.val(a.Relation)}, StoredField{"reportErrors", s.val(report)}, StoredField{"signature", s.val(a.Signature)})
}
func (s *storedEncoder) applicabilitySeed(a *checker.OracleCollectionsApplicabilityOrigin) string {
	if a == nil {
		return "null"
	}
	report := "null"
	if a.ReportErrors != nil {
		report = fmt.Sprint(*a.ReportErrors)
	}
	out := fmt.Sprintf("phase:%s/node:%d/mode:%d/relation:%s/report:%s/signature:", a.Phase, s.nodeSeed(a.Node), a.Mode, a.Relation, report)
	if a.Signature == nil {
		return out + "null"
	}
	v := reflect.ValueOf(a.Signature).Elem()
	declaration := readable(v.FieldByName("declaration"))
	decl := "null"
	if !declaration.IsNil() {
		decl = fmt.Sprint(s.nodeSeed(declaration.Interface().(*ast.Node)))
	}
	out += fmt.Sprintf("%d/%s", readable(v.FieldByName("flags")).Uint(), decl)
	if id, ok := s.ids[a.Signature]; ok {
		out += fmt.Sprintf("/root:%d", id)
	}
	return out
}

// Descriptor ordering never uses hash bits, compiler allocation IDs or map order.
// It is a bounded source-rooted seed. A collision fails rather than conflating
// distinct arguments; more source-derived provenance is required in that case.
func (s *storedEncoder) mapperSeed(m *checker.TypeMapper, types map[*checker.Type]int, seen map[*checker.TypeMapper]bool) string {
	if m == nil {
		return "null"
	}
	if id, ok := s.ids[m]; ok {
		return fmt.Sprintf("root:%d", id)
	}
	if seen[m] {
		panic("Unrooted mapper seed cycle")
	}
	seen[m] = true
	defer delete(seen, m)
	v := reflect.ValueOf(readable(reflect.ValueOf(m).Elem().FieldByName("data")).Interface()).Elem()
	name := v.Type().Name()
	field := func(n string) reflect.Value { return readable(v.FieldByName(n)) }
	one := func(n string) string { return s.typeSeed(field(n).Interface().(*checker.Type), types) }
	list := func(n string) string {
		v := field(n)
		out := []string{}
		for i := 0; i < v.Len(); i++ {
			out = append(out, s.typeSeed(v.Index(i).Interface().(*checker.Type), types))
		}
		return strings.Join(out, ";")
	}
	switch name {
	case "SimpleTypeMapper":
		return "simple(" + one("source") + "," + one("target") + ")"
	case "ArrayTypeMapper":
		return "array(" + list("sources") + "=>" + list("targets") + ")"
	case "ArrayToSingleTypeMapper":
		return "array-single(" + list("sources") + "=>" + one("target") + ")"
	case "MergedTypeMapper", "CompositeTypeMapper":
		return name + "(" + s.mapperSeed(field("m1").Interface().(*checker.TypeMapper), types, seen) + "," + s.mapperSeed(field("m2").Interface().(*checker.TypeMapper), types, seen) + ")"
	case "DeferredTypeMapper":
		parts := []string{}
		for _, target := range checker.OracleCollectionsDeferredTargets(s.c, m) {
			args := []string{}
			for _, t := range target.TypeParameters {
				args = append(args, s.typeSeed(t, types))
			}
			parts = append(parts, fmt.Sprintf("%d/%d(", s.nodeSeed(target.Parent), target.Index)+strings.Join(args, ";")+")")
		}
		return "deferred(" + list("sources") + "=>" + strings.Join(parts, ";") + ")"
	default:
		panic("Unrooted mapper cache provenance: " + name)
	}
}
func (s *storedEncoder) typeSeed(t *checker.Type, stack map[*checker.Type]int) string {
	if t == nil {
		return "null"
	}
	if index, ok := stack[t]; ok {
		return fmt.Sprintf("cycle:%d", index)
	}
	stack[t] = len(stack)
	defer delete(stack, t)
	v := reflect.ValueOf(readable(reflect.ValueOf(t).Elem().FieldByName("data")).Interface())
	name := v.Type().Elem().Name()
	seed := fmt.Sprintf("%d|%d|%s", t.Flags(), t.ObjectFlags(), name)
	if id, ok := s.ids[t]; ok {
		seed += fmt.Sprintf("|root:%d", id)
	}
	if symbol := t.Symbol(); symbol != nil {
		id, ok := s.binding.symbolIDs[symbol]
		if !ok {
			panic("Unrooted cache argument symbol seed")
		}
		seed += fmt.Sprintf("|symbol:%d", id)
	}
	if alias := t.Alias(); alias != nil {
		id, ok := s.binding.symbolIDs[alias.Symbol()]
		if !ok {
			panic("Unrooted type seed alias")
		}
		seed += fmt.Sprintf("|alias:%d(", id)
		for _, arg := range alias.TypeArguments() {
			seed += s.typeSeed(arg, stack) + ";"
		}
		seed += ")"
	}
	if object := t.AsObjectType(); object != nil {
		p := v.Elem()
		for _, field := range []string{"target"} {
			x := readable(p.FieldByName(field))
			if !x.IsNil() {
				seed += "|" + field + "(" + s.typeSeed(x.Interface().(*checker.Type), stack) + ")"
			}
		}
		mapper := readable(p.FieldByName("mapper"))
		if !mapper.IsNil() {
			seed += "|mapper(" + s.mapperSeed(mapper.Interface().(*checker.TypeMapper), stack, map[*checker.TypeMapper]bool{}) + ")"
		}
	}
	switch name {
	case "MappedType":
		p := v.Elem()
		declaration := readable(p.FieldByName("declaration"))
		if !declaration.IsNil() {
			seed += fmt.Sprintf("|declaration:%d", s.nodeSeed(declaration.Interface().(*ast.MappedTypeNode).AsNode()))
		}
		for _, field := range []string{"typeParameter", "constraintType", "nameType", "templateType", "modifiersType", "resolvedApparentType"} {
			x := readable(p.FieldByName(field))
			var child *checker.Type
			if !x.IsNil() {
				child = x.Interface().(*checker.Type)
			}
			seed += "|" + field + "(" + s.typeSeed(child, stack) + ")"
		}
	case "IndexType":
		seed += "|type(" + s.typeSeed(readable(v.Elem().FieldByName("target")).Interface().(*checker.Type), stack) + ")"
	case "IndexedAccessType":
		for _, field := range []string{"objectType", "indexType"} {
			seed += "|" + field + "(" + s.typeSeed(readable(v.Elem().FieldByName(field)).Interface().(*checker.Type), stack) + ")"
		}
		seed += fmt.Sprintf("|access:%d", readable(v.Elem().FieldByName("accessFlags")).Uint())
	case "SubstitutionType":
		for _, field := range []string{"baseType", "constraint"} {
			seed += "|" + field + "(" + s.typeSeed(readable(v.Elem().FieldByName(field)).Interface().(*checker.Type), stack) + ")"
		}
	case "ObjectType":
		if origin, present := checker.OracleCollectionsObjectProducer(s.c, t); present {
			nodeID, ok := s.binding.syntax.ids[origin.Node]
			if !ok {
				panic("Unrooted object producer node")
			}
			seed += fmt.Sprintf("|producer:%d/%d/context(", nodeID, origin.Mode) + s.typeSeed(origin.ContextualType, stack) + ")/source(" + s.typeSeed(origin.Source, stack) + ")"
			seed += "/applicability(" + s.applicabilitySeed(origin.Applicability) + ")"
		}
		members := checker.OracleCollectionsStoredMembers(t)
		names := []string{}
		for key := range members {
			names = append(names, key)
		}
		sort.Strings(names)
		for _, key := range names {
			symbol := members[key]
			seed += "|member:" + bytes(key) + fmt.Sprintf("/%d/%d/", symbol.Flags, symbol.CheckFlags) + s.typeSeed(checker.OracleCollectionsStoredPropertyType(s.c, symbol), stack)
		}
	case "TypeParameter":
		p := v.Elem()
		seed += fmt.Sprintf("|this:%t|distributed:%t", readable(p.FieldByName("isThisType")).Bool(), readable(p.FieldByName("isDistributed")).Bool())
		for _, field := range []string{"target", "constraint"} {
			x := readable(p.FieldByName(field))
			var value *checker.Type
			if !x.IsNil() {
				value = x.Interface().(*checker.Type)
			}
			seed += "|" + field + "(" + s.typeSeed(value, stack) + ")"
		}
	case "IntrinsicType":
		seed += "|" + bytes(t.AsIntrinsicType().IntrinsicName())
	case "LiteralType":
		payload := v.Elem()
		fresh := readable(payload.FieldByName("freshType"))
		regular := readable(payload.FieldByName("regularType"))
		seed += fmt.Sprintf("|freshSelf:%t|regularSelf:%t", !fresh.IsNil() && fresh.Interface() == t, !regular.IsNil() && regular.Interface() == t)
		x := reflect.ValueOf(t.AsLiteralType().Value())
		if x.Kind() == reflect.String {
			seed += "|" + bytes(x.String())
		} else if x.Kind() == reflect.Float64 {
			seed += fmt.Sprintf("|%016x", math.Float64bits(x.Float()))
		} else if x.Kind() == reflect.Bool {
			seed += fmt.Sprintf("|%t", x.Bool())
		} else {
			panic("Unsupported literal cache seed: " + x.Type().String())
		}
	case "TypeReference", "InterfaceType", "TupleType":
		x := readable(v.Elem().FieldByName("resolvedTypeArguments"))
		parts := []string{}
		for i := 0; i < x.Len(); i++ {
			parts = append(parts, s.typeSeed(x.Index(i).Interface().(*checker.Type), stack))
		}
		seed += "|args(" + strings.Join(parts, ";") + ")"
	case "UnionType", "IntersectionType":
		parts := []string{}
		for _, child := range t.Types() {
			parts = append(parts, s.typeSeed(child, stack))
		}
		seed += "|types(" + strings.Join(parts, ";") + ")"
	}
	return seed
}

func (s *storedEncoder) referenceCache(owner *checker.Type) *StoredValue {
	isNull, count, entries := checker.OracleCollectionsObjectInstantiations(s.c, owner)
	type ordered struct {
		seed  string
		entry checker.OracleCollectionsObjectEntry
	}
	rows := []ordered{}
	used := map[string]bool{}
	for _, entry := range entries {
		parts := []string{}
		for _, argument := range entry.Arguments {
			parts = append(parts, s.typeSeed(argument, map[*checker.Type]int{}))
		}
		seed := entry.Family + "|" + strings.Join(parts, ";") + fmt.Sprintf("|single:%t", entry.SingleSignature)
		if entry.Alias != nil {
			id, ok := s.binding.symbolIDs[entry.Alias.Symbol()]
			if !ok {
				panic("Unrooted anonymous alias symbol seed")
			}
			seed += fmt.Sprintf("|alias:%d(", id)
			for _, a := range entry.Alias.TypeArguments() {
				seed += s.typeSeed(a, map[*checker.Type]int{}) + ";"
			}
			seed += ")"
		}
		if used[seed] {
			panic(fmt.Sprintf("Reference argument descriptor collision: ownerFlags=%d seed=%s", owner.ObjectFlags(), seed))
		}
		used[seed] = true
		rows = append(rows, ordered{seed, entry})
	}
	sort.Slice(rows, func(i, j int) bool { return rows[i].seed < rows[j].seed })
	out := &StoredValue{Kind: "cache", Null: isNull, Values: []*StoredValue{}, Fields: []StoredField{}}
	for _, row := range rows {
		entry := row.entry
		out.Values = append(out.Values, &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: []StoredField{
			{"alias", s.value(reflect.ValueOf(entry.Alias))},
			{"arguments", s.value(reflect.ValueOf(entry.Arguments))},
			{"family", s.value(reflect.ValueOf(entry.Family))},
			{"isSelf", s.value(reflect.ValueOf(entry.IsSelf))},
			{"membership", s.value(reflect.ValueOf(true))},
			{"singleSignature", s.value(reflect.ValueOf(entry.SingleSignature))},
			{"target", s.value(reflect.ValueOf(entry.Target))},
			{"value", s.value(reflect.ValueOf(entry.Value))},
		}})
	}
	if len(out.Values) != count {
		panic("Reference cache count mismatch")
	}
	return out
}
func (s *storedEncoder) instantiationCache(owner any, object bool) *StoredValue {
	isNull, count, entries := checker.OracleCollectionsInstantiations(s.c, owner)
	type ordered struct {
		seed  string
		entry checker.OracleCollectionsInstantiationEntry
	}
	rows := []ordered{}
	used := map[string]bool{}
	for _, e := range entries {
		seed := e.Family + "|"
		for _, t := range e.Arguments {
			seed += s.typeSeed(t, map[*checker.Type]int{}) + ";"
		}
		seed += fmt.Sprintf("|flag:%t", e.ForConstraint)
		if e.AliasSymbol != nil {
			id, ok := s.binding.symbolIDs[e.AliasSymbol]
			if !ok {
				panic("Unrooted instantiation alias")
			}
			seed += fmt.Sprintf("|alias:%d(", id)
			for _, t := range e.AliasArguments {
				seed += s.typeSeed(t, map[*checker.Type]int{}) + ";"
			}
			seed += ")"
		}
		if used[seed] {
			panic("Instantiation descriptor collision: " + seed)
		}
		used[seed] = true
		rows = append(rows, ordered{seed, e})
	}
	sort.Slice(rows, func(i, j int) bool { return rows[i].seed < rows[j].seed })
	out := &StoredValue{Kind: "cache", Null: isNull, Values: []*StoredValue{}, Fields: []StoredField{}}
	for _, r := range rows {
		e := r.entry
		fields := []StoredField{
			{"aliasArguments", s.value(reflect.ValueOf(e.AliasArguments))}, {"aliasSymbol", s.value(reflect.ValueOf(e.AliasSymbol))},
			{"arguments", s.value(reflect.ValueOf(e.Arguments))}, {"family", s.value(reflect.ValueOf(e.Family))},
		}
		if object {
			fields = append(fields, StoredField{"isSelf", s.value(reflect.ValueOf(e.Value == owner))})
		} else {
			fields = append(fields, StoredField{"forConstraint", s.value(reflect.ValueOf(e.ForConstraint))})
		}
		fields = append(fields, StoredField{"membership", s.value(reflect.ValueOf(true))})
		if object {
			fields = append(fields, StoredField{"singleSignature", s.value(reflect.ValueOf(e.ForConstraint))})
		}
		fields = append(fields, StoredField{"value", s.value(reflect.ValueOf(e.Value))})
		out.Values = append(out.Values, &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: fields})
	}
	if len(out.Values) != count {
		panic("Instantiation cache count mismatch")
	}
	return out
}
func (s *storedEncoder) objectProducer(origin checker.OracleCollectionsObjectOrigin) *StoredValue {
	return &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: []StoredField{
		{"contextualType", s.value(reflect.ValueOf(origin.ContextualType))}, {"mode", s.value(reflect.ValueOf(origin.Mode))}, {"node", s.value(reflect.ValueOf(origin.Node))}, {"source", s.value(reflect.ValueOf(origin.Source))},
		{"applicability", s.applicabilityValue(origin.Applicability)},
	}}
}
func (s *storedEncoder) deferredTargets(m *checker.TypeMapper) *StoredValue {
	out := &StoredValue{Kind: "list", Values: []*StoredValue{}, Fields: []StoredField{}}
	for _, target := range checker.OracleCollectionsDeferredTargets(s.c, m) {
		out.Values = append(out.Values, &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: []StoredField{
			{"index", s.value(reflect.ValueOf(target.Index))}, {"parent", s.value(reflect.ValueOf(target.Parent))}, {"typeParameters", s.value(reflect.ValueOf(target.TypeParameters))},
		}})
	}
	return out
}
func (s *storedEncoder) comparer(context *checker.InferenceContext) *StoredValue {
	family, owner, relation, state := checker.OracleCollectionsComparer(s.c, context)
	var ownerValue *StoredValue
	if owner == s.c {
		ownerValue = &StoredValue{Kind: "checker-owner", Bool: true, Values: []*StoredValue{}, Fields: []StoredField{}}
	} else {
		ownerValue = s.value(reflect.ValueOf(owner))
	}
	return &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: []StoredField{
		{"currentRelation", s.value(reflect.ValueOf(relation))}, {"family", s.value(reflect.ValueOf(family))}, {"intersectionState", s.value(reflect.ValueOf(state))}, {"owner", ownerValue},
	}}
}
func (s *storedEncoder) value(v reflect.Value) *StoredValue {
	o := &StoredValue{Values: []*StoredValue{}, Fields: []StoredField{}}
	if !v.IsValid() {
		o.Kind = "null"
		o.Null = true
		return o
	}
	v = readable(v)
	if v.Kind() == reflect.Interface {
		if v.IsNil() {
			o.Kind = "null"
			o.Null = true
			return o
		}
		return s.value(v.Elem())
	}
	if v.Kind() == reflect.Pointer {
		o.Null = v.IsNil()
		switch x := v.Interface().(type) {
		case *ast.FlowNode:
			o.Kind = "flow"
			o.Ref = s.binding.flow(x)
			return o
		case *ast.Symbol:
			o.Kind = "symbol"
			o.Ref = s.binding.symbol(x)
			return o
		case interface{ AsNode() *ast.Node }:
			o.Kind = "node"
			if !o.Null {
				o.Ref = s.binding.node(x.AsNode())
			}
			return o
		}
		o.Kind = "reference"
		if o.Null {
			return o
		}
		if v.Type().Elem().PkgPath() != "github.com/microsoft/TypeScript/tsc/internal/checker" {
			panic("Unknown stored pointer " + v.Type().String())
		}
		o.Ref = s.ref(v.Interface())
		return o
	}

	if table, ok := v.Interface().(ast.SymbolTable); ok {
		o.Kind = "table"
		o.Null = table == nil
		o.Ref = s.binding.table(table)
		return o
	}
	if v.Type() == reflect.TypeFor[collections.OrderedSet[*ast.Node]]() {
		// Bounded API projection, not the private OrderedMap backing topology.
		set := v.Addr().Interface().(*collections.OrderedSet[*ast.Node])
		values := &StoredValue{Kind: "list", Values: []*StoredValue{}, Fields: []StoredField{}}
		membership := &StoredValue{Kind: "list", Values: []*StoredValue{}, Fields: []StoredField{}}
		for n := range set.Values() {
			values.Values = append(values.Values, s.value(reflect.ValueOf(n)))
			membership.Values = append(membership.Values, s.value(reflect.ValueOf(set.Has(n))))
		}
		if len(values.Values) != set.Size() {
			panic("Ordered node set cardinality mismatch")
		}
		o.Kind = "inline"
		o.Fields = []StoredField{{"membership", membership}, {"size", s.value(reflect.ValueOf(set.Size()))}, {"values", values}}
		return o
	}
	switch v.Kind() {
	case reflect.String:
		o.Kind = "bytes"
		o.Bytes = base64.StdEncoding.EncodeToString([]byte(v.String()))
	case reflect.Bool:
		o.Kind = "bool"
		o.Bool = v.Bool()
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		o.Kind = "number"
		o.Number = v.Int()
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
		o.Kind = "number"
		o.Number = int64(v.Uint())
	case reflect.Array, reflect.Slice:
		o.Kind = "list"
		o.Null = v.Kind() == reflect.Slice && v.IsNil()
		for i := 0; i < v.Len(); i++ {
			o.Values = append(o.Values, s.value(v.Index(i)))
		}
	case reflect.Struct:
		o.Kind = "inline"
		o.Fields = s.fields(v)
	case reflect.Map:
		// A nonempty opaque map must be matched by an actual key-family observer,
		// never flattened into a sorted value bag or serialized by hash bits.
		o.Kind = "cache"
		o.Null = v.IsNil()
		if v.Len() != 0 {
			panic(fmt.Sprintf("Unobserved stored cache key family %s (%d entries)", v.Type(), v.Len()))
		}
	case reflect.Func:
		o.Kind = "function"
		o.Null = v.IsNil()
		if !o.Null {
			panic("Unobserved function provenance " + v.Type().String())
		}
	case reflect.Float64:
		o.Kind = "float"
		o.Bytes = fmt.Sprintf("%016x", math.Float64bits(v.Float()))
	default:
		panic("Unknown stored scalar " + v.Type().String())
	}
	return o
}
func (s *storedEncoder) tryGet(storeName string, key any) reflect.Value {
	v := readable(reflect.ValueOf(s.c).Elem().FieldByName(storeName))
	method := v.MethodByName("TryGet")
	if !method.IsValid() {
		method = v.Addr().MethodByName("TryGet")
	}
	if !method.IsValid() {
		panic("Missing actual stored TryGet " + storeName)
	}
	return method.Call([]reflect.Value{reflect.ValueOf(key)})[0]
}
func (s *storedEncoder) nodeStates(id int, n *ast.Node) {
	names := []string{"arrayLiteralLinks", "nodeLinks", "signatureLinks", "sourceFileLinks", "symbolNodeLinks", "typeNodeLinks"}
	for _, name := range names {
		var key any = n
		if name == "sourceFileLinks" {
			if n.Kind != ast.KindSourceFile {
				continue
			}
			key = n.AsSourceFile()
		}
		value := s.tryGet(name, key)
		if value.IsNil() {
			continue
		}
		if name == "symbolNodeLinks" {
			value = readable(value.Elem().FieldByName("resolvedSymbol"))
			if value.IsNil() {
				continue
			}
			name += ".resolvedSymbol"
		}
		s.out.States = append(s.out.States, StoredState{"node", id, name, s.value(value)})
	}
}
func (s *storedEncoder) symbolStates(id int, symbol *ast.Symbol) {
	names := []string{"ReverseMappedSymbolLinks", "declaredTypeLinks", "deferredSymbolLinks", "exportTypeLinks", "mappedSymbolLinks", "markedAssignmentSymbolLinks", "membersAndExportsLinks", "moduleSymbolLinks", "symbolReferenceLinks", "symbolContainerLinks", "typeAliasLinks", "valueSymbolLinks", "varianceLinks"}
	for _, name := range names {
		value := s.tryGet(name, symbol)
		if !value.IsNil() {
			s.out.States = append(s.out.States, StoredState{"symbol", id, name, s.value(value)})
		}
	}
}
func (s *storedEncoder) encode(types []*checker.Type) StoredGraph {
	for _, t := range types {
		s.out.Roots = append(s.out.Roots, s.ref(t))
	}
	nextRecord, nextNode, nextSymbol := 0, 0, 0
	globalCachesRead := false
	for !globalCachesRead || nextRecord < len(s.queue) || nextNode < len(s.binding.syntax.raw) || nextSymbol < len(s.binding.symbolIDs) {
		for nextRecord < len(s.queue) {
			name, fields := s.describe(s.queue[nextRecord])
			s.out.Records[nextRecord].Name = name
			s.out.Records[nextRecord].Fields = fields
			nextRecord++
		}
		for nextNode < len(s.binding.syntax.raw) {
			n := s.binding.syntax.raw[nextNode]
			s.nodeStates(nextNode, n)
			if n.Kind == ast.KindSyntheticExpression {
				raw := n.AsSyntheticExpression().Type
				var t *checker.Type
				if raw != nil {
					var ok bool
					t, ok = raw.(*checker.Type)
					if !ok {
						panic("Unknown synthetic checker type")
					}
				}
				s.out.SyntheticTypes = append(s.out.SyntheticTypes, StoredNodeType{nextNode, s.ref(t)})
			}
			nextNode++
		}
		symbols := make([]*ast.Symbol, len(s.binding.symbolIDs))
		for symbol, id := range s.binding.symbolIDs {
			symbols[id] = symbol
		}
		for nextSymbol < len(symbols) {
			s.symbolStates(nextSymbol, symbols[nextSymbol])
			nextSymbol++
		}
		if !globalCachesRead && nextRecord == len(s.queue) && nextNode == len(s.binding.syntax.raw) && nextSymbol == len(s.binding.symbolIDs) {
			s.readGlobalCaches()
			globalCachesRead = true
		}
	}
	s.out.Inventories = []StoredInventory{}
	names := []string{"arrayLiteralLinks", "nodeLinks", "signatureLinks", "sourceFileLinks", "typeNodeLinks", "ReverseMappedSymbolLinks", "declaredTypeLinks", "deferredSymbolLinks", "exportTypeLinks", "mappedSymbolLinks", "markedAssignmentSymbolLinks", "membersAndExportsLinks", "moduleSymbolLinks", "symbolContainerLinks", "symbolReferenceLinks", "typeAliasLinks", "varianceLinks"}
	for i, name := range names {
		store := readable(reflect.ValueOf(s.c).Elem().FieldByName(name))
		entries := readable(store.FieldByName("entries"))
		kind := "symbol"
		if i < 5 {
			kind = "node"
		}
		owners := []int{}
		for _, key := range entries.MapKeys() {
			id := 0
			ok := false
			if kind == "node" {
				var node *ast.Node
				if name == "sourceFileLinks" {
					node = key.Interface().(*ast.SourceFile).AsNode()
				} else {
					node = key.Interface().(*ast.Node)
				}
				id, ok = s.binding.syntax.ids[node]
				if !ok {
					panic(fmt.Sprintf("Unrooted logical node store owner: %s/%v", name, node.Kind))
				}
			} else {
				id, ok = s.binding.symbolIDs[key.Interface().(*ast.Symbol)]
				if !ok {
					panic("Unrooted logical symbol store owner: " + name)
				}
			}
			owners = append(owners, id)
		}
		sort.Ints(owners)
		for j := 1; j < len(owners); j++ {
			if owners[j-1] == owners[j] {
				panic("Collapsed logical store owner identity")
			}
		}
		s.out.Inventories = append(s.out.Inventories, StoredInventory{name, kind, entries.IsNil(), entries.Len(), owners})
	}
	// Unmodeled later-stage/source-excluded stores must actually remain nil in
	// the original runtime. Nonnil reopens the boundary; no managed default is
	// substituted for these absent models.
	for _, name := range []string{"enumMemberLinks", "assertionLinks", "switchStatementLinks", "jsxElementLinks", "computedNameLinks", "aliasSymbolLinks", "lateBoundLinks", "spreadLinks"} {
		entries := readable(readable(reflect.ValueOf(s.c).Elem().FieldByName(name)).FieldByName("entries"))
		if !entries.IsNil() {
			panic("Reached unmodeled logical store " + name)
		}
	}
	return s.out
}
