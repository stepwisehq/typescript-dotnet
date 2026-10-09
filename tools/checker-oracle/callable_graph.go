// Independently discovered callable fields; no C# field manifest is used.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"reflect"
)

type CallableQueryIdentity struct {
	Node           int  `json:"node"`
	TypeIdentity   bool `json:"typeIdentity"`
	SymbolIdentity bool `json:"symbolIdentity"`
}

type CallableCall struct {
	Node             int  `json:"node"`
	Signature        *int `json:"signature"`
	RepeatedIdentity bool `json:"repeatedIdentity"`
}
type CallableCache struct {
	Owner            int    `json:"owner"`
	Name             string `json:"name"`
	Value            int    `json:"value"`
	RepeatedIdentity bool   `json:"repeatedIdentity"`
}
type CallableDiagnosticState struct {
	Value       SemanticDiagnostic        `json:"value"`
	RelatedNull bool                      `json:"relatedNull"`
	ChainNull   bool                      `json:"chainNull"`
	Related     []CallableDiagnosticState `json:"related"`
	Chain       []CallableDiagnosticState `json:"chain"`
}
type CallableGraph struct {
	SourceFileLinksPresent bool                      `json:"sourceFileLinksPresent"`
	SourceFileLinks        *DataRecord               `json:"sourceFileLinks"`
	Queries                []CallableQueryIdentity   `json:"queries"`
	NoPredicateSentinel    *int                      `json:"noPredicateSentinel"`
	Signatures             []*DataRecord             `json:"signatures"`
	Predicates             []*DataRecord             `json:"predicates"`
	Composites             []*DataRecord             `json:"composites"`
	Calls                  []CallableCall            `json:"calls"`
	SignatureLinks         []DataLinkObservation     `json:"signatureLinks"`
	CachedSignaturesNull   bool                      `json:"cachedSignaturesNull"`
	CachedSignatures       []CallableCache           `json:"cachedSignatures"`
	BindDiagnosticsNull    bool                      `json:"bindDiagnosticsNull"`
	FileDiagnosticsNull    bool                      `json:"fileDiagnosticsNull"`
	GlobalDiagnosticsNull  bool                      `json:"globalDiagnosticsNull"`
	BindDiagnosticStates   []CallableDiagnosticState `json:"bindDiagnosticStates"`
	FileDiagnosticStates   []CallableDiagnosticState `json:"fileDiagnosticStates"`
	GlobalDiagnosticStates []CallableDiagnosticState `json:"globalDiagnosticStates"`
}
type callableEncoder struct {
	d           *dataEncoder
	out         *CallableGraph
	signatures  map[*checker.Signature]int
	predicates  map[*checker.TypePredicate]int
	composites  map[*checker.CompositeSignature]int
	cacheCursor int
}

func callableDiagnosticStates(ds []*ast.Diagnostic) []CallableDiagnosticState {
	out := []CallableDiagnosticState{}
	for _, v := range ds {
		out = append(out, CallableDiagnosticState{semanticDiagnostics([]*ast.Diagnostic{v})[0], v.RelatedInformation() == nil, v.MessageChain() == nil, callableDiagnosticStates(v.RelatedInformation()), callableDiagnosticStates(v.MessageChain())})
	}
	return out
}
func (a *callableEncoder) signature(s *checker.Signature) *int {
	if s == nil {
		return nil
	}
	if id, ok := a.signatures[s]; ok {
		return ptr(id)
	}
	id := len(a.out.Signatures)
	a.signatures[s] = id
	r := &DataRecord{ID: id, DataType: "Signature", Fields: []DataField{}}
	a.out.Signatures = append(a.out.Signatures, r)
	// These selected public observations intentionally resolve return and predicate caches.
	a.d.g.c.GetReturnTypeOfSignature(s)
	a.d.g.c.GetTypePredicateOfSignature(s)
	for _, p := range s.Parameters() {
		a.d.g.c.GetTypeOfSymbol(p)
	}
	v := reflect.ValueOf(s).Elem()
	for i := 0; i < v.NumField(); i++ {
		field := v.Type().Field(i)
		if field.Name == "id" {
			continue
		}
		r.Fields = append(r.Fields, a.d.field(field.Name, readable(v.Field(i))))
	}
	return ptr(id)
}
func (a *callableEncoder) predicate(p *checker.TypePredicate) *int {
	if p == nil {
		return nil
	}
	if id, ok := a.predicates[p]; ok {
		return ptr(id)
	}
	id := len(a.out.Predicates)
	a.predicates[p] = id
	r := &DataRecord{ID: id, DataType: "TypePredicate", Fields: []DataField{}}
	a.out.Predicates = append(a.out.Predicates, r)
	r.Fields = a.d.fields(reflect.ValueOf(p).Elem())
	return ptr(id)
}
func (a *callableEncoder) composite(p *checker.CompositeSignature) *int {
	if p == nil {
		return nil
	}
	if id, ok := a.composites[p]; ok {
		return ptr(id)
	}
	id := len(a.out.Composites)
	a.composites[p] = id
	r := &DataRecord{ID: id, DataType: "CompositeSignature", Fields: []DataField{}}
	a.out.Composites = append(a.out.Composites, r)
	r.Fields = a.d.fields(reflect.ValueOf(p).Elem())
	return ptr(id)
}
func (d *dataEncoder) startCallables() {
	out := &CallableGraph{Signatures: []*DataRecord{}, Predicates: []*DataRecord{}, Composites: []*DataRecord{}, Calls: []CallableCall{}, SignatureLinks: []DataLinkObservation{}, CachedSignatures: []CallableCache{}, BindDiagnosticStates: []CallableDiagnosticState{}, FileDiagnosticStates: []CallableDiagnosticState{}, GlobalDiagnosticStates: []CallableDiagnosticState{}}
	d.callables = &callableEncoder{d, out, map[*checker.Signature]int{}, map[*checker.TypePredicate]int{}, map[*checker.CompositeSignature]int{}, 0}
	d.g.result.CallableGraph = out
	out.Queries = d.g.callableQueries
	out.CachedSignaturesNull = checker.OracleObserveCallableCachedSignaturesNil(d.g.c)
	for id, n := range d.g.syntax.raw {
		if n.Kind == ast.KindCallExpression {
			s := d.g.c.GetResolvedSignature(n)
			out.Calls = append(out.Calls, CallableCall{id, d.callables.signature(s), s == d.g.c.GetResolvedSignature(n)})
		}
	}
	for id, n := range d.g.syntax.raw {
		p := checker.OracleObserveCallableSignatureLinks(d.g.c, n)
		fields := []DataField{}
		if p != nil {
			fields = d.fields(reflect.ValueOf(p).Elem())
		}
		out.SignatureLinks = append(out.SignatureLinks, DataLinkObservation{"node", id, "signatureLinks", p != nil, fields})
	}
	links := checker.OracleObserveCallableSourceFileLinks(d.g.c, d.g.syntax.file)
	out.SourceFileLinksPresent = links != nil
	if links != nil {
		record := &DataRecord{ID: 0, DataType: "SourceFileLinks", Fields: []DataField{}}
		v := reflect.ValueOf(links).Elem()
		for i := 0; i < v.NumField(); i++ {
			field := v.Type().Field(i)
			value := readable(v.Field(i))
			if field.Name == "deferredNodes" {
				value = reflect.ValueOf(checker.OracleObserveCallableDeferredNodes(links))
			}
			record.Fields = append(record.Fields, d.field(field.Name, value))
		}
		out.SourceFileLinks = record
	}
	out.NoPredicateSentinel = d.callables.predicate(checker.OracleObserveCallableNoTypePredicate(d.g.c))
}
func (d *dataEncoder) finishCallables() {
	for d.callables.cacheCursor < len(d.callables.out.Signatures) {
		cursor := d.callables.cacheCursor
		d.callables.cacheCursor++
		var owner *checker.Signature
		for s, id := range d.callables.signatures {
			if id == cursor {
				owner = s
				break
			}
		}
		for _, e := range checker.OracleObserveCallableCachedSignatures(d.g.c, owner) {
			same := false
			for _, again := range checker.OracleObserveCallableCachedSignatures(d.g.c, owner) {
				if again.Name == e.Name {
					same = e.Value == again.Value
				}
			}
			d.callables.out.CachedSignatures = append(d.callables.out.CachedSignatures, CallableCache{cursor, e.Name, *d.callables.signature(e.Value), same})
		}
	}
}
