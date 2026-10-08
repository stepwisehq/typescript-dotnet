// Authored read-only foundation observations; all computed values call the pin.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"reflect"
	"slices"
)

type FactObservation struct {
	NodeID             int     `json:"nodeId"`
	InitialCache       *uint32 `json:"initialCache"`
	First              uint32  `json:"first"`
	CacheAfterFirst    *uint32 `json:"cacheAfterFirst"`
	Repeated           uint32  `json:"repeated"`
	CacheAfterRepeated *uint32 `json:"cacheAfterRepeated"`
}
type PositionObservation struct {
	Offset      int `json:"offset"`
	UTF8ToUTF16 int `json:"utf8ToUtf16"`
	UTF16ToUTF8 int `json:"utf16ToUtf8"`
}
type DiagnosticState struct {
	FileNameBase64     *string           `json:"fileNameBase64"`
	FileMatchesSource  bool              `json:"fileMatchesSource"`
	SourceBase64       string            `json:"sourceBase64"`
	MessageTextBase64  string            `json:"messageTextBase64"`
	ArgsNull           bool              `json:"argsNull"`
	ChainNull          bool              `json:"chainNull"`
	RelatedNull        bool              `json:"relatedNull"`
	ReportsUnnecessary bool              `json:"reportsUnnecessary"`
	ReportsDeprecated  bool              `json:"reportsDeprecated"`
	SkippedOnNoEmit    bool              `json:"skippedOnNoEmit"`
	Chain              []DiagnosticState `json:"chain"`
	Related            []DiagnosticState `json:"related"`
}
type Foundation struct {
	Facts                  []FactObservation     `json:"facts"`
	Visits                 []VisitObservation    `json:"visits"`
	AsciiOnly              bool                  `json:"asciiOnly"`
	PositionMapReused      bool                  `json:"positionMapReused"`
	Positions              []PositionObservation `json:"positions"`
	ParseStates            []DiagnosticState     `json:"parseStates"`
	JSStates               []DiagnosticState     `json:"jsStates"`
	JSDocStates            []DiagnosticState     `json:"jsDocStates"`
	DiagnosticChannelsNull []bool                `json:"diagnosticChannelsNull"`
}
type VisitObservation struct {
	NodeID     int   `json:"nodeId"`
	Same       bool  `json:"same"`
	ChildOrder []int `json:"childOrder"`
}

// Field discovery is independent of the C# schema. The private atomic slot is
// read without calling SubtreeFacts and therefore without populating a cache.
func factCache(n *ast.Node) *uint32 {
	value := readable(reflect.ValueOf(n).Elem().FieldByName("data")).Elem().Elem()
	cache := value.FieldByName("facts")
	if !cache.IsValid() {
		return nil
	}
	if cache.Type().String() != "atomic.Uint32" {
		panic("Unclassified facts cache: " + cache.Type().String())
	}
	slot := cache.FieldByName("v")
	if !slot.IsValid() || slot.Kind() != reflect.Uint32 {
		panic("Unknown atomic.Uint32 layout")
	}
	return ptr(uint32(slot.Uint()))
}
func diagnosticStates(ds []*ast.Diagnostic, file *ast.SourceFile) []DiagnosticState {
	out := []DiagnosticState{}
	for _, d := range ds {
		var name *string
		if d.File() != nil {
			name = ptr(bytes(d.File().FileName().AsString()))
		}
		out = append(out, DiagnosticState{FileNameBase64: name, FileMatchesSource: d.File() == file,
			SourceBase64: bytes(d.Source()), MessageTextBase64: bytes(d.MessageText()),
			ArgsNull: d.MessageArgs() == nil, ChainNull: d.MessageChain() == nil, RelatedNull: d.RelatedInformation() == nil,
			ReportsUnnecessary: d.ReportsUnnecessary(), ReportsDeprecated: d.ReportsDeprecated(), SkippedOnNoEmit: d.SkippedOnNoEmit(),
			Chain: diagnosticStates(d.MessageChain(), file), Related: diagnosticStates(d.RelatedInformation(), file)})
	}
	return out
}
func (e *encoder) foundationObservation() *Foundation {
	text := e.file.Text()
	result := &Foundation{Facts: []FactObservation{}, Positions: []PositionObservation{}}
	// Snapshot every initial slot first. The subsequent first-value calls follow
	// canonical DFS identity order and can populate descendants' slots.
	for i, n := range e.raw {
		result.Facts = append(result.Facts, FactObservation{NodeID: i, InitialCache: factCache(n)})
	}
	for i, n := range e.raw {
		result.Facts[i].First = uint32(n.SubtreeFacts())
		result.Facts[i].CacheAfterFirst = factCache(n)
		result.Facts[i].Repeated = uint32(n.SubtreeFacts())
		result.Facts[i].CacheAfterRepeated = factCache(n)
	}
	result.Visits = []VisitObservation{}
	for i, n := range e.raw {
		order := []int{}
		visitor := ast.NewNodeVisitor(func(child *ast.Node) *ast.Node {
			id, ok := e.ids[child]
			if !ok {
				panic("Unregistered identity-visitor child")
			}
			order = append(order, id)
			return child
		}, nil, ast.NodeVisitorHooks{})
		updated := n.VisitEachChild(visitor)
		result.Visits = append(result.Visits, VisitObservation{i, updated == n, order})
	}
	pm := e.file.GetPositionMap()
	result.AsciiOnly = pm.IsAsciiOnly()
	result.PositionMapReused = pm == e.file.GetPositionMap()
	offsets := []int{-1, 0, len(text), len(text) + 1}
	for _, n := range e.raw {
		offsets = append(offsets, n.Pos(), n.End())
	}
	slices.Sort(offsets)
	for _, offset := range slices.Compact(offsets) {
		result.Positions = append(result.Positions, PositionObservation{offset, pm.UTF8ToUTF16(offset), pm.UTF16ToUTF8(offset)})
	}
	result.ParseStates = diagnosticStates(e.file.Diagnostics(), e.file)
	result.JSStates = diagnosticStates(e.file.JSDiagnostics(), e.file)
	result.JSDocStates = diagnosticStates(e.file.JSDocDiagnostics(), e.file)
	result.DiagnosticChannelsNull = []bool{e.file.Diagnostics() == nil, e.file.JSDiagnostics() == nil, e.file.JSDocDiagnostics() == nil}
	return result
}
