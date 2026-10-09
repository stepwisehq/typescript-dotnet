// Independent upstream checker oracle: real Program, no-library single-file host.
package main

import (
	"bufio"
	"context"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/jsnum"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
	"math"
	"os"
	"reflect"
	"slices"
	"strings"
)

var lastSyntaxEncoder *encoder

type SemanticRequest struct {
	Profile              string `json:"profile"`
	CallableObservations bool   `json:"callableObservations"`
	Request
	QueryNodeIDs       []int    `json:"queryNodeIds"`
	RelationPairs      [][2]int `json:"relationPairs"`
	ObjectObservations bool     `json:"objectObservations"`
	DataObservations   bool     `json:"dataObservations"`
	DeclaredNodeIDs    []int    `json:"declaredNodeIds"`
	WidenNodeIDs       []int    `json:"widenNodeIds"`
}
type SemanticDiagnostic struct {
	Diagnostic
	FileName           *string              `json:"fileNameBase64"`
	ArgsNull           bool                 `json:"argsNull"`
	ReportsUnnecessary bool                 `json:"reportsUnnecessary"`
	ReportsDeprecated  bool                 `json:"reportsDeprecated"`
	Source             string               `json:"sourceBase64"`
	SkippedOnNoEmit    bool                 `json:"skippedOnNoEmit"`
	Related            []SemanticDiagnostic `json:"related"`
	Chain              []SemanticDiagnostic `json:"chain"`
}

func semanticDiagnostics(items []*ast.Diagnostic) []SemanticDiagnostic {
	out := []SemanticDiagnostic{}
	for _, d := range items {
		ordinary := diagnostics([]*ast.Diagnostic{d})[0]
		value := SemanticDiagnostic{Diagnostic: ordinary, ArgsNull: d.MessageArgs() == nil, ReportsUnnecessary: d.ReportsUnnecessary(), ReportsDeprecated: d.ReportsDeprecated(), Source: bytes(d.Source()), SkippedOnNoEmit: d.SkippedOnNoEmit(), Related: semanticDiagnostics(d.RelatedInformation()), Chain: semanticDiagnostics(d.MessageChain())}
		if d.File() != nil {
			value.FileName = ptr(bytes(d.File().FileName().AsString()))
		}
		if d.RepopulateInfo() != nil {
			panic("Unexpected checker repopulation info")
		}
		out = append(out, value)
	}
	return out
}

type SemanticType struct {
	ID                 int     `json:"id"`
	Flags              uint32  `json:"flags"`
	ObjectFlags        uint32  `json:"objectFlags"`
	DataType           string  `json:"dataType"`
	Text               string  `json:"text"`
	IntrinsicName      *string `json:"intrinsicNameBase64"`
	LiteralValue       any     `json:"literalValue"`
	Types              []int   `json:"types"`
	FreshType          *int    `json:"freshType"`
	RegularType        *int    `json:"regularType"`
	Symbol             *int    `json:"symbol"`
	AliasSymbol        *int    `json:"aliasSymbol"`
	AliasTypeArguments []int   `json:"aliasTypeArguments"`
}
type SemanticSymbol struct {
	ID               int    `json:"id"`
	Flags            uint32 `json:"flags"`
	CheckFlags       uint32 `json:"checkFlags"`
	Name             string `json:"nameBase64"`
	DeclarationsNull bool   `json:"declarationsNull"`
	Declarations     []int  `json:"declarations"`
	ValueDeclaration *int   `json:"valueDeclaration"`
	Parent           *int   `json:"parent"`
	ExportSymbol     *int   `json:"exportSymbol"`
	Members          *int   `json:"members"`
	Exports          *int   `json:"exports"`
}
type SemanticTable struct {
	ID      int             `json:"id"`
	Entries []SemanticEntry `json:"entries"`
}
type SemanticEntry struct {
	Key    string `json:"keyBase64"`
	Symbol int    `json:"symbolId"`
}
type Query struct {
	NodeID int  `json:"nodeId"`
	Type   int  `json:"type"`
	Symbol *int `json:"symbol"`
}
type Relation struct {
	Source     int  `json:"sourceNodeId"`
	Target     int  `json:"targetNodeId"`
	Assignable bool `json:"assignable"`
}
type SemanticResult struct {
	CallableGraph      *CallableGraph       `json:"callableGraph,omitempty"`
	Parser             Response             `json:"parser"`
	BindDiagnostics    []SemanticDiagnostic `json:"bindDiagnostics"`
	CheckerDiagnostics []SemanticDiagnostic `json:"checkerDiagnostics"`
	GlobalDiagnostics  []SemanticDiagnostic `json:"globalDiagnostics"`
	Types              []*SemanticType      `json:"types"`
	Symbols            []*SemanticSymbol    `json:"symbols"`
	Tables             []*SemanticTable     `json:"tables"`
	Queries            []Query              `json:"queries"`
	Relations          []Relation           `json:"relations"`
	GlobalThisSymbol   *int                 `json:"globalThisSymbol"`
	Locals             *int                 `json:"locals"`
	Globals            *int                 `json:"globals"`
	Options            map[string]any       `json:"options"`
	ObjectGraph        *ObjectGraph         `json:"objectGraph,omitempty"`
	DataGraph          *DataGraph           `json:"dataGraph,omitempty"`
}
type semanticGraph struct {
	callableObservations bool
	callableQueries      []CallableQueryIdentity
	c                    *checker.Checker
	syntax               *encoder
	result               *SemanticResult
	types                map[*checker.Type]int
	symbols              map[*ast.Symbol]int
	tables               map[uintptr]int
}

func (g *semanticGraph) node(n *ast.Node) *int {
	if n == nil {
		return nil
	}
	id, ok := g.syntax.ids[n]
	if !ok {
		panic("Semantic symbol declaration outside single-file syntax")
	}
	return ptr(id)
}
func (g *semanticGraph) symbol(s *ast.Symbol) *int {
	if s == nil {
		return nil
	}
	if id, ok := g.symbols[s]; ok {
		return ptr(id)
	}
	id := len(g.result.Symbols)
	g.symbols[s] = id
	v := &SemanticSymbol{ID: id, Flags: uint32(s.Flags), CheckFlags: uint32(s.CheckFlags), Name: bytes(s.Name), DeclarationsNull: s.Declarations == nil, Declarations: []int{}}
	g.result.Symbols = append(g.result.Symbols, v)
	for _, n := range s.Declarations {
		v.Declarations = append(v.Declarations, *g.node(n))
	}
	v.ValueDeclaration = g.node(s.ValueDeclaration)
	v.Parent = g.symbol(s.Parent)
	v.ExportSymbol = g.symbol(s.ExportSymbol)
	v.Members = g.table(s.Members)
	v.Exports = g.table(s.Exports)
	return ptr(id)
}
func (g *semanticGraph) table(table ast.SymbolTable) *int {
	if table == nil {
		return nil
	}
	identity := reflect.ValueOf(table).Pointer()
	if id, ok := g.tables[identity]; ok {
		return ptr(id)
	}
	id := len(g.result.Tables)
	g.tables[identity] = id
	v := &SemanticTable{ID: id, Entries: []SemanticEntry{}}
	g.result.Tables = append(g.result.Tables, v)
	keys := make([]string, 0, len(table))
	for key := range table {
		keys = append(keys, key)
	}
	slices.Sort(keys)
	for _, key := range keys {
		v.Entries = append(v.Entries, SemanticEntry{bytes(key), *g.symbol(table[key])})
	}
	return ptr(id)
}
func (g *semanticGraph) typ(t *checker.Type) *int {
	if t == nil {
		return nil
	}
	if id, ok := g.types[t]; ok {
		return ptr(id)
	}
	id := len(g.result.Types)
	g.types[t] = id
	data := readable(reflect.ValueOf(t).Elem().FieldByName("data")).Elem().Elem()
	v := &SemanticType{ID: id, Flags: uint32(t.Flags()), ObjectFlags: uint32(t.ObjectFlags()), DataType: data.Type().Name(), Types: []int{}, AliasTypeArguments: []int{}}
	g.result.Types = append(g.result.Types, v)
	v.Text = g.c.TypeToString(t)
	v.Symbol = g.symbol(t.Symbol())
	if alias := t.Alias(); alias != nil {
		v.AliasSymbol = g.symbol(alias.Symbol())
		for _, arg := range alias.TypeArguments() {
			v.AliasTypeArguments = append(v.AliasTypeArguments, *g.typ(arg))
		}
	}
	switch v.DataType {
	case "IntrinsicType":
		v.IntrinsicName = ptr(bytes(t.AsIntrinsicType().IntrinsicName()))
	case "LiteralType":
		literal := t.AsLiteralType()
		switch value := literal.Value().(type) {
		case string:
			v.LiteralValue = map[string]any{"kind": "string", "bytesBase64": bytes(value)}
		case bool:
			v.LiteralValue = map[string]any{"kind": "bool", "value": value}
		case jsnum.Number:
			v.LiteralValue = map[string]any{"kind": "number", "bitsHex": fmt.Sprintf("%016x", math.Float64bits(float64(value)))}
		case jsnum.PseudoBigInt:
			literal := map[string]any{"kind": "bigint", "text": value.String()}
			if g.callableObservations {
				literal["negative"] = value.Negative
				literal["base10ValueBase64"] = bytes(value.Base10Value)
			}
			v.LiteralValue = literal
		case nil:
			v.LiteralValue = map[string]any{"kind": "nil"}
		default:
			panic("Unknown literal value")
		}
		v.FreshType = g.typ(literal.FreshType())
		v.RegularType = g.typ(literal.RegularType())
	case "UnionType", "IntersectionType":
		for _, member := range t.Types() {
			v.Types = append(v.Types, *g.typ(member))
		}
	default: // Broad upstream gap probes retain actual public type text/flags/symbols.
	}
	return ptr(id)
}
func runChecker(r SemanticRequest) SemanticResult {
	if r.ScriptKind != 0 && r.ScriptKind != core.ScriptKindTS {
		panic("Oracle phase requires a TS input")
	}
	r.ScriptKind = core.ScriptKindTS
	if r.FileName == "" {
		r.FileName = "/main.ts"
	}
	text := r.Text
	if r.TextBytesBase64 != nil {
		data, err := base64.StdEncoding.DecodeString(*r.TextBytesBase64)
		if err != nil {
			panic(err)
		}
		text = string(data)
	}
	config := `{"files":[` + fmt.Sprintf("%q", r.FileName) + `],"compilerOptions":{"target":"esnext","noLib":true,"noResolve":true,"strictNullChecks":true,"strictBindCallApply":false,"noImplicitAny":false}}`
	if r.Profile == "Callable" {
		config = strings.Replace(config, `"noImplicitAny":false`, `"noImplicitAny":true,"noImplicitThis":true,"strictFunctionTypes":true`, 1)
	} else if r.Profile != "" && r.Profile != "Legacy" {
		panic("Unknown checker profile")
	}
	fs := vfstest.FromMap(map[string]string{r.FileName: text, "/tsconfig.json": config}, tspath.CaseSensitive)
	parsed, errors := tsoptions.GetParsedCommandLineOfConfigFile("/tsconfig.json", &core.CompilerOptions{}, nil, fs, nil)
	if len(errors) != 0 {
		panic(fmt.Sprintf("Config errors:%v", errors))
	}
	host := compiler.NewCompilerHost(fs, tspath.RootedDirectoryPathFromNormalized("/no-library"), nil, nil, nil)
	program := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue, SkipModuleResolution: true}, ProgramHosts: compiler.ProgramHosts{Host: host}})
	file := program.GetSourceFile(tspath.RootedFilePathFromNormalized(r.FileName))
	if file == nil || len(program.SourceFiles()) != 1 {
		panic("Single-file no-library host contract failed")
	}
	result := SemanticResult{Parser: runParser(r.Request, file), Types: []*SemanticType{}, Symbols: []*SemanticSymbol{}, Tables: []*SemanticTable{}, Queries: []Query{}, Relations: []Relation{}, Options: map[string]any{"target": "ESNext", "noLib": true, "noResolve": true, "skipModuleResolution": true, "strictNullChecks": true, "strictBindCallApply": false, "noImplicitAny": false}}
	if r.Profile == "Callable" {
		result.Options["noImplicitAny"] = true
		result.Options["noImplicitThis"] = true
		result.Options["strictFunctionTypes"] = true
		result.Options["profile"] = "Callable"
	}
	c, _ := checker.NewChecker(program, nil)
	g := semanticGraph{callableObservations: r.CallableObservations, c: c, syntax: lastSyntaxEncoder, result: &result, types: map[*checker.Type]int{}, symbols: map[*ast.Symbol]int{}, tables: map[uintptr]int{}}
	fileDiagnostics := c.GetDiagnostics(context.Background(), file)
	ids := r.QueryNodeIDs
	if ids == nil {
		for i, n := range g.syntax.raw {
			if ast.IsIdentifier(n) || ast.IsLiteralExpression(n) || n.Kind == ast.KindTrueKeyword || n.Kind == ast.KindFalseKeyword || n.Kind == ast.KindNullKeyword {
				ids = append(ids, i)
			}
		}
	}
	queried := map[int]*checker.Type{}
	for _, id := range ids {
		if id < 0 || id >= len(g.syntax.raw) {
			panic("Invalid query node ID")
		}
		n := g.syntax.raw[id]
		t := c.GetTypeAtLocation(n)
		queried[id] = t
		symbol := c.GetSymbolAtLocation(n)
		result.Queries = append(result.Queries, Query{id, *g.typ(t), g.symbol(symbol)})
		if r.CallableObservations {
			g.callableQueries = append(g.callableQueries, CallableQueryIdentity{id, t == c.GetTypeAtLocation(n), symbol == c.GetSymbolAtLocation(n)})
		}
	}
	for _, pair := range r.RelationPairs {
		source, ok := queried[pair[0]]
		target, ok2 := queried[pair[1]]
		if !ok || !ok2 {
			panic("Relation endpoints must be query nodes")
		}
		result.Relations = append(result.Relations, Relation{pair[0], pair[1], c.IsTypeAssignableTo(source, target)})
	}
	result.GlobalThisSymbol = g.symbol(c.GetGlobalSymbol("globalThis", ast.SymbolFlagsModule, nil))
	result.Locals = g.table(file.Locals)
	globals := readable(reflect.ValueOf(c).Elem().FieldByName("globals")).Interface().(ast.SymbolTable)
	result.Globals = g.table(globals)
	if globals["globalThis"] == nil || reflect.ValueOf(globals).Pointer() != reflect.ValueOf(globals["globalThis"].Exports).Pointer() {
		panic("Globals/globalThis exports identity differs")
	}
	result.BindDiagnostics = semanticDiagnostics(file.BindDiagnostics())
	result.CheckerDiagnostics = semanticDiagnostics(fileDiagnostics)
	result.GlobalDiagnostics = semanticDiagnostics(c.GetGlobalDiagnostics())
	if r.ObjectObservations {
		result.ObjectGraph = g.objectGraph(r.WidenNodeIDs, queried)
	}
	if r.DataObservations || r.CallableObservations {
		result.DataGraph = g.dataGraph(r, queried)
	}
	if result.CallableGraph != nil {
		out := result.CallableGraph
		global := c.GetGlobalDiagnostics()
		out.BindDiagnosticsNull = file.BindDiagnostics() == nil
		out.FileDiagnosticsNull = fileDiagnostics == nil
		out.GlobalDiagnosticsNull = global == nil
		out.BindDiagnosticStates = callableDiagnosticStates(file.BindDiagnostics())
		out.FileDiagnosticStates = callableDiagnosticStates(fileDiagnostics)
		out.GlobalDiagnosticStates = callableDiagnosticStates(global)
	}
	return result
}
func main() {
	input := bufio.NewScanner(os.Stdin)
	input.Buffer(make([]byte, 65536), 32*1024*1024)
	out := json.NewEncoder(os.Stdout)
	for input.Scan() {
		var request SemanticRequest
		if err := json.Unmarshal(input.Bytes(), &request); err != nil {
			panic(err)
		}
		if err := out.Encode(runChecker(request)); err != nil {
			panic(err)
		}
	}
	if err := input.Err(); err != nil {
		panic(err)
	}
}
