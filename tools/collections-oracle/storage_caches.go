// Authored nonforcing selected whole-cache consumer; no compiler resolver calls.
package main

import (
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"reflect"
	"sort"
	"strings"
)

// Failure diagnostics only; never a graph ID, descriptor or successful output.
func relationPayload(t *checker.Type) any {
	if t == nil {
		return nil
	}
	return readable(reflect.ValueOf(t).Elem().FieldByName("data")).Interface()
}

type orderedCacheRow struct {
	seed   string
	render func() *StoredValue
}

func (s *storedEncoder) seed(t *checker.Type) string { return s.typeSeed(t, map[*checker.Type]int{}) }
func (s *storedEncoder) nodeSeed(node *ast.Node) int {
	id, ok := s.binding.syntax.ids[node]
	if !ok {
		panic("Unrooted stored-cache node term")
	}
	return id
}
func (s *storedEncoder) signatureSeed(sig *checker.Signature, stack map[*checker.Signature]bool) string {
	if stack[sig] {
		panic("Signature cache seed cycle")
	}
	stack[sig] = true
	defer delete(stack, sig)
	v := reflect.ValueOf(sig).Elem()
	field := func(name string) reflect.Value { return readable(v.FieldByName(name)) }
	declaration := "null"
	if !field("declaration").IsNil() {
		declaration = fmt.Sprint(s.nodeSeed(field("declaration").Interface().(*ast.Node)))
	}
	seed := fmt.Sprintf("flags:%d/decl:%s", field("flags").Uint(), declaration)
	if id, ok := s.ids[sig]; ok {
		seed += fmt.Sprintf("/root:%d", id)
	}
	if !field("target").IsNil() {
		seed += "/target(" + s.signatureSeed(field("target").Interface().(*checker.Signature), stack) + ")"
	}
	if !field("mapper").IsNil() {
		m := field("mapper").Interface().(*checker.TypeMapper)
		if id, ok := s.ids[m]; ok {
			seed += fmt.Sprintf("/mapper-root:%d", id)
		} else {
			d := reflect.ValueOf(readable(reflect.ValueOf(m).Elem().FieldByName("data")).Interface()).Elem()
			switch d.Type().Name() {
			case "SimpleTypeMapper":
				seed += "/simple(" + s.seed(readable(d.FieldByName("source")).Interface().(*checker.Type)) + "," + s.seed(readable(d.FieldByName("target")).Interface().(*checker.Type)) + ")"
			case "ArrayTypeMapper":
				parts := func(name string) string {
					v := readable(d.FieldByName(name))
					out := []string{}
					for i := 0; i < v.Len(); i++ {
						out = append(out, s.seed(v.Index(i).Interface().(*checker.Type)))
					}
					return strings.Join(out, ";")
				}
				seed += "/array(" + parts("sources") + "=>" + parts("targets") + ")"
			default:
				panic("Unrooted signature cache mapper provenance")
			}
		}
	}
	if !field("composite").IsNil() {
		c := field("composite").Elem()
		sigs := readable(c.FieldByName("signatures"))
		parts := []string{}
		for i := 0; i < sigs.Len(); i++ {
			parts = append(parts, s.signatureSeed(sigs.Index(i).Interface().(*checker.Signature), stack))
		}
		seed += fmt.Sprintf("/composite:%t(%s)", readable(c.FieldByName("isUnion")).Bool(), strings.Join(parts, ";"))
	}
	return seed
}
func inlineStored(fields ...StoredField) *StoredValue {
	return &StoredValue{Kind: "inline", Values: []*StoredValue{}, Fields: fields}
}
func (s *storedEncoder) val(value any) *StoredValue { return s.value(reflect.ValueOf(value)) }
func (s *storedEncoder) cache(name string, isNull bool, count int, rows []orderedCacheRow) {
	if len(rows) != count {
		panic("Cache cardinality mismatch: " + name)
	}
	sort.Slice(rows, func(i, j int) bool { return rows[i].seed < rows[j].seed })
	out := &StoredValue{Kind: "cache", Null: isNull, Values: []*StoredValue{}, Fields: []StoredField{{"count", s.val(count)}}}
	for i, row := range rows {
		if i > 0 && rows[i-1].seed == row.seed {
			panic("Cache descriptor collision: " + name + " seed=" + row.seed)
		}
		out.Values = append(out.Values, row.render())
	}
	s.out.GlobalCaches = append(s.out.GlobalCaches, StoredField{name, out})
}
func (s *storedEncoder) knownCacheTypes() map[checker.TypeId]*checker.Type {
	known := map[checker.TypeId]*checker.Type{}
	seen := map[any]bool{}
	var walk func(reflect.Value)
	walk = func(v reflect.Value) {
		if !v.IsValid() {
			return
		}
		v = readable(v)
		if v.Kind() == reflect.Interface {
			if !v.IsNil() {
				walk(v.Elem())
			}
			return
		}
		if v.Kind() == reflect.Pointer {
			if v.IsNil() {
				return
			}
			x := v.Interface()
			switch x.(type) {
			case *ast.Node, *ast.Symbol, *ast.FlowNode, *ast.FlowList, *checker.Relater:
				return
			}
			if v.Type().Elem().PkgPath() != "github.com/microsoft/TypeScript/tsc/internal/checker" {
				return
			}
			if seen[x] {
				return
			}
			seen[x] = true
			if t, ok := x.(*checker.Type); ok {
				id := checker.TypeId(readable(v.Elem().FieldByName("id")).Uint())
				if id != 0 {
					if old := known[id]; old != nil && old != t {
						panic("Duplicate actual type ID")
					}
					known[id] = t
				}
				if origin, present := checker.OracleCollectionsObjectProducer(s.c, t); present {
					walk(reflect.ValueOf(origin.Source))
					walk(reflect.ValueOf(origin.ContextualType))
					if origin.Applicability != nil {
						walk(reflect.ValueOf(origin.Applicability.Signature))
					}
				}
			}
			walk(v.Elem())
			return
		}
		switch v.Kind() {
		case reflect.Struct:
			for i := 0; i < v.NumField(); i++ {
				name := v.Type().Field(i).Name
				if name != "checker" && name != "c" {
					walk(v.Field(i))
				}
			}
		case reflect.Slice, reflect.Array:
			for i := 0; i < v.Len(); i++ {
				walk(v.Index(i))
			}
		case reflect.Map:
			for _, key := range v.MapKeys() {
				walk(v.MapIndex(key))
			}
		}
	}
	for _, v := range s.queue {
		walk(reflect.ValueOf(v))
	}
	types, sigs := checker.OracleCollectionsGlobalCacheValues(s.c)
	for _, t := range types {
		walk(reflect.ValueOf(t))
	}
	for _, sig := range sigs {
		walk(reflect.ValueOf(sig))
	}
	for _, t := range checker.OracleCollectionsFlowValues(s.c) {
		walk(reflect.ValueOf(t))
	}
	for _, slot := range checker.OracleCollectionsStoredTypeSlots(s.c) {
		walk(reflect.ValueOf(slot.Type))
		walk(reflect.ValueOf(slot.Signature))
	}
	for _, t := range checker.OracleCollectionsRelationInputs(s.c) {
		walk(reflect.ValueOf(t))
	}
	for _, t := range checker.OracleCollectionsWidenedInputs(s.c) {
		walk(reflect.ValueOf(t))
	}
	for _, t := range checker.OracleCollectionsPropertiesValues(s.c) {
		walk(reflect.ValueOf(t))
	}
	return known
}
func (s *storedEncoder) readGlobalCaches() {
	s.out.GlobalCaches = []StoredField{}
	known := s.knownCacheTypes()
	isNull, count, tuples := checker.OracleCollectionsTupleTargets(s.c)
	rows := []orderedCacheRow{}
	for _, e := range tuples {
		elements := reflect.ValueOf(e.Elements)
		parts := []string{}
		for i := 0; i < elements.Len(); i++ {
			f := elements.Index(i)
			label := readable(f.FieldByName("labeledDeclaration"))
			node := "null"
			if !label.IsNil() {
				node = fmt.Sprint(s.nodeSeed(label.Interface().(*ast.Node)))
			}
			parts = append(parts, fmt.Sprint(readable(f.FieldByName("flags")).Uint())+"/"+node)
		}
		seed := strings.Join(parts, ";") + fmt.Sprintf("/%t", e.Readonly)
		rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
			return inlineStored(StoredField{"elements", s.val(e.Elements)}, StoredField{"membership", s.val(true)}, StoredField{"readonly", s.val(e.Readonly)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("tuple-targets", isNull, count, rows)
	isNull, count, signatures := checker.OracleCollectionsCachedSignatures(s.c, known)
	rows = []orderedCacheRow{}
	for _, e := range signatures {
		parts := []string{}
		for _, a := range e.Arguments {
			parts = append(parts, s.seed(a))
		}
		seed := s.signatureSeed(e.Owner, map[*checker.Signature]bool{}) + "/" + e.Family + "(" + strings.Join(parts, ";") + ")"
		rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
			return inlineStored(StoredField{"arguments", s.val(e.Arguments)}, StoredField{"family", s.val(e.Family)}, StoredField{"membership", s.val(true)}, StoredField{"owner", s.val(e.Owner)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("cached-signatures", isNull, count, rows)
	for _, cache := range checker.OracleCollectionsTypeCaches(s.c, known) {
		rows = []orderedCacheRow{}
		for _, e := range cache.Entries {
			seed := ""
			var fields func() []StoredField
			switch cache.Name {
			case "cached-types":
				seed = fmt.Sprint(e.Kind) + "/" + s.seed(e.Owner)
				fields = func() []StoredField { return []StoredField{{"kind", s.val(e.Kind)}, {"owner", s.val(e.Owner)}} }
			case "indexed-access":
				alias := "null"
				if e.AliasSymbol != nil {
					id, ok := s.binding.symbolIDs[e.AliasSymbol]
					if !ok {
						panic("Unrooted indexed alias symbol")
					}
					alias = fmt.Sprint(id)
				}
				parts := []string{}
				for _, t := range e.AliasArguments {
					parts = append(parts, s.seed(t))
				}
				seed = s.seed(e.Object) + "/" + s.seed(e.Index) + fmt.Sprintf("/%d/alias:%s(%s)", e.AccessFlags, alias, strings.Join(parts, ";"))
				fields = func() []StoredField {
					return []StoredField{{"accessFlags", s.val(e.AccessFlags)}, {"aliasArguments", s.val(e.AliasArguments)}, {"aliasSymbol", s.val(e.AliasSymbol)}, {"index", s.val(e.Index)}, {"object", s.val(e.Object)}}
				}
			case "substitution":
				seed = s.seed(e.Base) + "/" + s.seed(e.Constraint)
				fields = func() []StoredField {
					return []StoredField{{"base", s.val(e.Base)}, {"constraint", s.val(e.Constraint)}}
				}
			case "template-literal":
				parts := []string{}
				for _, text := range e.Texts {
					parts = append(parts, bytes(text))
				}
				types := []string{}
				for _, t := range e.Types {
					types = append(types, s.seed(t))
				}
				seed = strings.Join(parts, ";") + "(" + strings.Join(types, ";") + ")"
				fields = func() []StoredField { return []StoredField{{"texts", s.val(e.Texts)}, {"types", s.val(e.Types)}} }
			case "reverse-homomorphic-mapped", "reverse-mapped":
				seed = s.seed(e.Source) + "/" + s.seed(e.Target) + "/" + s.seed(e.Constraint)
				fields = func() []StoredField {
					return []StoredField{{"constraint", s.val(e.Constraint)}, {"source", s.val(e.Source)}, {"target", s.val(e.Target)}}
				}
			default:
				panic("Unknown global type cache " + cache.Name)
			}
			rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
				f := fields()
				f = append(f, StoredField{"membership", s.val(true)}, StoredField{"value", s.val(e.Value)})
				sort.Slice(f, func(i, j int) bool { return f[i].Name < f[j].Name })
				return inlineStored(f...)
			}})
		}
		s.cache(cache.Name, cache.IsNull, cache.Count, rows)
	}
	for _, cache := range checker.OracleCollectionsRelations(s.c, known) {
		rows = []orderedCacheRow{}
		descriptors := map[string]checker.OracleCollectionsRelationEntry{}
		for _, e := range cache.Entries {
			seed := e.Family + fmt.Sprintf("/%d/", e.IntersectionState) + s.seed(e.Source) + "/" + s.seed(e.Target) + "/" + s.relationSeed(e.SourceReference) + "/" + s.relationSeed(e.TargetReference)
			if previous, present := descriptors[seed]; present {
				panic(fmt.Sprintf("Relation descriptor collision: seed=%s previous=%#v current=%#v previousSource=%#v source=%#v previousTarget=%#v target=%#v previousMembers=%s members=%s", seed, previous, e, relationPayload(previous.Source), relationPayload(e.Source), relationPayload(previous.Target), relationPayload(e.Target), checker.OracleCollectionsDescriptorMembers(s.c, previous.Source), checker.OracleCollectionsDescriptorMembers(s.c, e.Source)))
			}
			descriptors[seed] = e
			rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
				return inlineStored(StoredField{"family", s.val(e.Family)}, StoredField{"intersectionState", s.val(e.IntersectionState)}, StoredField{"membership", s.val(true)}, StoredField{"result", s.val(e.Result)}, StoredField{"source", s.val(e.Source)}, StoredField{"sourceReference", s.relationValue(e.SourceReference)}, StoredField{"target", s.val(e.Target)}, StoredField{"targetReference", s.relationValue(e.TargetReference)})
			}})
		}
		s.cache("relation-"+cache.Name, cache.IsNull, cache.Count, rows)
	}
	propertiesNull, propertiesCount, propertiesEntries := checker.OracleCollectionsPropertiesTypes(s.c, known)
	rows = []orderedCacheRow{}
	for _, e := range propertiesEntries {
		rows = append(rows, orderedCacheRow{s.seed(e.Owner) + fmt.Sprintf("/%d/%t", e.Include, e.IncludeOrigin), func() *StoredValue {
			return inlineStored(StoredField{"include", s.val(e.Include)}, StoredField{"includeOrigin", s.val(e.IncludeOrigin)}, StoredField{"membership", s.val(true)}, StoredField{"owner", s.val(e.Owner)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("properties-types", propertiesNull, propertiesCount, rows)
	iterationNull, iterationCount, iterationEntries := checker.OracleCollectionsIterationCache(s.c, known)
	rows = []orderedCacheRow{}
	for _, entry := range iterationEntries {
		rows = append(rows, orderedCacheRow{s.seed(entry.Owner) + fmt.Sprintf("/%d", entry.Use), func() *StoredValue {
			return inlineStored(StoredField{"membership", s.val(true)}, StoredField{"next", s.val(entry.Next)}, StoredField{"owner", s.val(entry.Owner)}, StoredField{"return", s.val(entry.Return)}, StoredField{"use", s.val(entry.Use)}, StoredField{"yield", s.val(entry.Yield)})
		}})
	}
	s.cache("iteration-types", iterationNull, iterationCount, rows)
	for _, cache := range checker.OracleCollectionsExpressionCaches(s.c) {
		rows = []orderedCacheRow{}
		for _, entry := range cache.Entries {
			rows = append(rows, orderedCacheRow{fmt.Sprint(s.nodeSeed(entry.Node)), func() *StoredValue {
				return inlineStored(StoredField{"membership", s.val(true)}, StoredField{"node", s.val(entry.Node)}, StoredField{"value", s.val(entry.Value)})
			}})
		}
		s.cache(cache.Name, cache.IsNull, cache.Count, rows)
	}
	knownNodes := map[ast.NodeId]*ast.Node{}
	for _, node := range s.binding.syntax.raw {
		id := ast.NodeId(checker.OracleCollectionsStoredNodeID(node))
		if id != 0 {
			if knownNodes[id] != nil && knownNodes[id] != node {
				panic("Duplicate actual node ID")
			}
			knownNodes[id] = node
		}
	}
	discrNull, discrCount, discrEntries := checker.OracleCollectionsDiscriminatedTypes(s.c, knownNodes, known)
	rows = []orderedCacheRow{}
	for _, entry := range discrEntries {
		rows = append(rows, orderedCacheRow{fmt.Sprint(s.nodeSeed(entry.Node)) + "/" + s.seed(entry.ContextualType), func() *StoredValue {
			return inlineStored(StoredField{"contextualType", s.val(entry.ContextualType)}, StoredField{"membership", s.val(true)}, StoredField{"node", s.val(entry.Node)}, StoredField{"value", s.val(entry.Value)})
		}})
	}
	s.cache("discriminated-contextual-types", discrNull, discrCount, rows)
	s.readFlowCaches(known)
}
func (s *storedEncoder) relationSeed(r *checker.OracleCollectionsRelationReference) string {
	if r == nil {
		return "null"
	}
	parts := []string{}
	for _, a := range r.Arguments {
		switch a.Kind {
		case "parameter":
			parts = append(parts, fmt.Sprint("parameter:", a.Index))
		case "type":
			parts = append(parts, "type:"+s.seed(a.Type))
		case "reference":
			parts = append(parts, "reference:"+s.relationSeed(a.Reference))
		default:
			panic("Unknown relation token")
		}
	}
	return s.seed(r.Target) + "(" + strings.Join(parts, ";") + ")"
}
func (s *storedEncoder) relationValue(r *checker.OracleCollectionsRelationReference) *StoredValue {
	if r == nil {
		return &StoredValue{Kind: "inline", Null: true, Values: []*StoredValue{}, Fields: []StoredField{}}
	}
	args := &StoredValue{Kind: "list", Values: []*StoredValue{}, Fields: []StoredField{}}
	for _, a := range r.Arguments {
		args.Values = append(args.Values, inlineStored(StoredField{"index", s.val(a.Index)}, StoredField{"kind", s.val(a.Kind)}, StoredField{"reference", s.relationValue(a.Reference)}, StoredField{"type", s.val(a.Type)}))
	}
	return inlineStored(StoredField{"arguments", args}, StoredField{"target", s.val(r.Target)})
}
func (s *storedEncoder) flowSeed(flow *ast.FlowNode) string {
	id, ok := s.binding.flowIDs[flow]
	if !ok {
		panic("Unrooted retained flow cache owner")
	}
	return fmt.Sprint(id)
}
func (s *storedEncoder) readFlowCaches(known map[checker.TypeId]*checker.Type) {
	tokenSeed := func(t checker.OracleCollectionsFlowToken) string {
		if t.Kind == "bytes" {
			return "bytes:" + bytes(t.Bytes)
		}
		switch owner := t.Owner.(type) {
		case *checker.Type:
			return "type:" + s.seed(owner)
		case *ast.Symbol:
			id, ok := s.binding.symbolIDs[owner]
			if !ok {
				panic("Unrooted flow symbol term")
			}
			return fmt.Sprint("symbol:", id)
		case *ast.Node:
			return fmt.Sprint("node:", s.nodeSeed(owner))
		default:
			panic("Unknown flow token")
		}
	}
	tokenValue := func(t checker.OracleCollectionsFlowToken) *StoredValue {
		v := s.val(t.Owner)
		if t.Kind == "bytes" {
			v = s.val(t.Bytes)
		}
		return inlineStored(StoredField{"kind", s.val(t.Kind)}, StoredField{"value", v})
	}
	isNull, count, loops := checker.OracleCollectionsFlowLoops(s.c)
	rows := []orderedCacheRow{}
	for _, e := range loops {
		parts := []string{}
		for _, t := range e.Key {
			parts = append(parts, tokenSeed(t))
		}
		seed := s.flowSeed(e.Flow) + "/" + strings.Join(parts, ";")
		rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
			k := &StoredValue{Kind: "list", Values: []*StoredValue{}, Fields: []StoredField{}}
			for _, t := range e.Key {
				k.Values = append(k.Values, tokenValue(t))
			}
			return inlineStored(StoredField{"flow", s.val(e.Flow)}, StoredField{"key", k}, StoredField{"membership", s.val(true)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("flow-loop", isNull, count, rows)
	isNull, count, narrowed := checker.OracleCollectionsNarrowedTypes(s.c)
	rows = []orderedCacheRow{}
	for _, e := range narrowed {
		seed := s.seed(e.Type) + "/" + s.seed(e.Candidate) + fmt.Sprintf("/%t/%t", e.AssumeTrue, e.CheckDerived)
		rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
			return inlineStored(StoredField{"assumeTrue", s.val(e.AssumeTrue)}, StoredField{"candidate", s.val(e.Candidate)}, StoredField{"checkDerived", s.val(e.CheckDerived)}, StoredField{"membership", s.val(true)}, StoredField{"type", s.val(e.Type)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("narrowed-types", isNull, count, rows)
	isNull, count, assigned := checker.OracleCollectionsAssignmentReduced(s.c, known)
	rows = []orderedCacheRow{}
	for _, e := range assigned {
		seed := s.seed(e.Declared) + "/" + s.seed(e.Assigned)
		rows = append(rows, orderedCacheRow{seed, func() *StoredValue {
			return inlineStored(StoredField{"assigned", s.val(e.Assigned)}, StoredField{"declared", s.val(e.Declared)}, StoredField{"membership", s.val(true)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("assignment-reduced", isNull, count, rows)
	isNull, count, reachable, last, value := checker.OracleCollectionsReachableFlows(s.c)
	rows = []orderedCacheRow{}
	for _, e := range reachable {
		rows = append(rows, orderedCacheRow{s.flowSeed(e.Flow), func() *StoredValue {
			return inlineStored(StoredField{"flow", s.val(e.Flow)}, StoredField{"membership", s.val(true)}, StoredField{"value", s.val(e.Value)})
		}})
	}
	s.cache("flow-reachable", isNull, count, rows)
	s.out.GlobalCaches = append(s.out.GlobalCaches, StoredField{"last-flow", inlineStored(StoredField{"flow", s.val(last)}, StoredField{"reachable", s.val(value)})})
	stackNull, stackCount, mapperCount, cacheCount := checker.OracleCollectionsFlowLifetime(s.c)
	if stackCount != 0 || mapperCount != 0 || cacheCount != 0 {
		panic("Unclosed active flow/mapper lifetime")
	}
	s.out.GlobalCaches = append(s.out.GlobalCaches, StoredField{"active-lifetimes", inlineStored(StoredField{"activeMapperCacheCount", s.val(cacheCount)}, StoredField{"activeMapperCount", s.val(mapperCount)}, StoredField{"loopStackCount", s.val(stackCount)}, StoredField{"loopStackNull", s.val(stackNull)})})
}
