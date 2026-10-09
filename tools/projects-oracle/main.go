// Authored independent bounded-project protocol over the pinned Program/default resolver.
package main

import (
	"bufio"
	"context"
	"crypto/sha256"
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
	"path/filepath"
	"reflect"
	"sort"
	"strings"
)

var specifierProjectionMode bool

type ProjectRequest struct {
	Name                   string            `json:"name"`
	Files                  map[string]string `json:"files"`
	Config                 string            `json:"config"`
	Queries                []Selector        `json:"queries"`
	SpecifierTarget        string            `json:"specifierTarget,omitempty"`
	SpecifierContext       string            `json:"specifierContext,omitempty"`
	SpecifierOutsideShadow bool              `json:"specifierOutsideShadow,omitempty"`
}
type Selector struct {
	File string `json:"file"`
	Pos  int    `json:"pos"`
	Kind int    `json:"kind"`
	End  *int   `json:"end,omitempty"`
}
type FullDiagnostic struct {
	Code        int32             `json:"code"`
	Category    int32             `json:"category"`
	Pos         int               `json:"pos"`
	End         int               `json:"end"`
	Key         string            `json:"key"`
	Args        []string          `json:"argsBase64"`
	Text        string            `json:"text"`
	File        *int              `json:"fileIndex"`
	Source      string            `json:"sourceBase64"`
	Unnecessary bool              `json:"reportsUnnecessary"`
	Deprecated  bool              `json:"reportsDeprecated"`
	Skip        bool              `json:"skippedOnNoEmit"`
	Chain       []*FullDiagnostic `json:"chain"`
	Related     []*FullDiagnostic `json:"related"`
}

func full(ds []*ast.Diagnostic, files map[*ast.SourceFile]int) []*FullDiagnostic {
	if ds == nil {
		return nil
	}
	out := []*FullDiagnostic{}
	for _, d := range ds {
		var args []string
		if d.MessageArgs() != nil {
			args = []string{}
			for _, a := range d.MessageArgs() {
				args = append(args, bytes(a))
			}
		}
		row := &FullDiagnostic{Code: d.Code(), Category: int32(d.Category()), Pos: d.Pos(), End: d.End(), Key: string(d.MessageKey()), Args: args, Text: d.String(), Source: bytes(d.Source()), Unnecessary: d.ReportsUnnecessary(), Deprecated: d.ReportsDeprecated(), Skip: d.SkippedOnNoEmit(), Chain: full(d.MessageChain(), files), Related: full(d.RelatedInformation(), files)}
		if d.File() != nil {
			id, ok := files[d.File()]
			if !ok {
				panic("Diagnostic has unknown file owner")
			}
			row.File = ptr(id)
		}
		if d.RepopulateInfo() != nil {
			panic("Unexpected incremental diagnostic state")
		}
		out = append(out, row)
	}
	return out
}

type TypeRow struct {
	ID                 int     `json:"id"`
	DataType           string  `json:"dataType"`
	Flags              uint32  `json:"flags"`
	ObjectFlags        uint32  `json:"objectFlags"`
	Symbol             *int    `json:"symbol"`
	AliasSymbol        *int    `json:"aliasSymbol"`
	AliasArguments     []*int  `json:"aliasArguments"`
	Target             *int    `json:"target"`
	Arguments          []*int  `json:"arguments"`
	Constituents       []*int  `json:"constituents"`
	Constraint         *int    `json:"constraint"`
	Default            *int    `json:"default"`
	Members            *int    `json:"members"`
	Properties         []*int  `json:"properties"`
	Signatures         []*int  `json:"signatures"`
	CallSignatureCount int     `json:"callSignatureCount"`
	IntrinsicName      *string `json:"intrinsicNameBase64"`
	LiteralKind        *string `json:"literalKind"`
	LiteralValue       *string `json:"literalValue"`
}
type SignatureRow struct {
	ID             int    `json:"id"`
	Flags          uint32 `json:"flags"`
	Min            int    `json:"minArgumentCount"`
	ResolvedMin    int    `json:"resolvedMinArgumentCount"`
	Declaration    *int   `json:"declaration"`
	TypeParameters []*int `json:"typeParameters"`
	Parameters     []*int `json:"parameters"`
	This           *int   `json:"thisParameter"`
	Return         *int   `json:"returnType"`
	Target         *int   `json:"target"`
}
type semantics struct {
	c          *checker.Checker
	g          *graph
	types      map[*checker.Type]int
	signatures map[*checker.Signature]int
	tq         []*checker.Type
	sq         []*checker.Signature
	tr         []*TypeRow
	sr         []*SignatureRow
}

func (s *semantics) t(t *checker.Type) *int {
	if t == nil {
		return nil
	}
	if id, ok := s.types[t]; ok {
		return ptr(id)
	}
	id := len(s.tq)
	s.types[t] = id
	s.tq = append(s.tq, t)
	s.tr = append(s.tr, nil)
	return ptr(id)
}
func (s *semantics) s(sig *checker.Signature) *int {
	if sig == nil {
		return nil
	}
	if id, ok := s.signatures[sig]; ok {
		return ptr(id)
	}
	id := len(s.sq)
	s.signatures[sig] = id
	s.sq = append(s.sq, sig)
	s.sr = append(s.sr, nil)
	return ptr(id)
}
func (s *semantics) ts(values []*checker.Type) []*int {
	if values == nil {
		return nil
	}
	out := []*int{}
	for _, v := range values {
		out = append(out, s.t(v))
	}
	return out
}
func (s *semantics) symbols(values []*ast.Symbol) []*int {
	if values == nil {
		return nil
	}
	out := []*int{}
	for _, v := range values {
		out = append(out, s.g.symbol(v))
	}
	return out
}
func (s *semantics) drain() {
	ti, si := 0, 0
	for ti < len(s.tq) || si < len(s.sq) {
		for ti < len(s.tq) {
			t := s.tq[ti]
			d := checker.OracleProjectTypeState(t)
			row := &TypeRow{ID: ti, DataType: checker.OracleProjectPayloadName(s.c, t), Flags: uint32(t.Flags()), ObjectFlags: uint32(t.ObjectFlags()), Symbol: s.g.symbol(t.Symbol()), Target: s.t(d.Target), Arguments: s.ts(d.Arguments), Constituents: s.ts(d.Constituents), Constraint: s.t(d.Constraint), Default: s.t(d.Default), Members: s.g.table(d.Members), Properties: s.symbols(d.Properties), CallSignatureCount: d.CallSignatureCount}
			if a := t.Alias(); a != nil {
				row.AliasSymbol = s.g.symbol(a.Symbol())
				row.AliasArguments = s.ts(a.TypeArguments())
			}
			if d.Signatures != nil {
				row.Signatures = []*int{}
				for _, sig := range d.Signatures {
					row.Signatures = append(row.Signatures, s.s(sig))
				}
			}
			if row.DataType == "IntrinsicType" {
				v := bytes(t.AsIntrinsicType().IntrinsicName())
				row.IntrinsicName = &v
			}
			if row.DataType == "LiteralType" {
				kind, value := "", ""
				switch v := t.AsLiteralType().Value().(type) {
				case string:
					kind = "bytes"
					value = bytes(v)
				case jsnum.Number:
					kind = "number"
					value = fmt.Sprintf("%016x", math.Float64bits(float64(v)))
				case bool:
					kind = "bool"
					value = fmt.Sprint(v)
				default:
					panic("Unknown selected literal scalar")
				}
				row.LiteralKind = &kind
				row.LiteralValue = &value
			}
			s.tr[ti] = row
			ti++
		}
		for si < len(s.sq) {
			sig := s.sq[si]
			d := checker.OracleProjectSignatureState(sig)
			s.sr[si] = &SignatureRow{ID: si, Flags: uint32(sig.Flags()), Min: sig.MinArgumentCount(), ResolvedMin: d.ResolvedMinArgumentCount, Declaration: s.g.node(sig.Declaration()), TypeParameters: s.ts(sig.TypeParameters()), Parameters: s.symbols(sig.Parameters()), This: s.g.symbol(sig.ThisParameter()), Return: s.t(s.c.GetReturnTypeOfSignature(sig)), Target: s.s(sig.Target())}
			si++
		}
	}
}
func run(r ProjectRequest) any {
	if !specifierProjectionMode && (r.SpecifierTarget != "" || r.SpecifierContext != "" || r.SpecifierOutsideShadow) {
		panic("Source-API selectors require --specifier-projection")
	}
	if r.Config == "" {
		r.Config = `{"files":["main.ts"]}`
	}
	fsFiles := map[string]string{"/project/tsconfig.json": r.Config}
	if r.SpecifierOutsideShadow {
		fsFiles["/project.ts"] = "export const shadow=1;"
	}
	for path, text := range r.Files {
		if strings.HasPrefix(path, "/") || path == ".." || strings.HasPrefix(path, "../") || strings.Contains(path, "/../") {
			panic("Noncanonical fixture path")
		}
		fsFiles["/project/"+path] = text
	}
	libs := []tspath.RootedFilePath{}
	for _, name := range []string{"lib.es5.d.ts", "lib.decorators.d.ts", "lib.decorators.legacy.d.ts"} {
		b, e := os.ReadFile(filepath.Join("upstream/TypeScript/tsc/internal/bundled/libs", name))
		if e != nil {
			panic(e)
		}
		hashes := map[string]string{"lib.es5.d.ts": "45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215", "lib.decorators.d.ts": "1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4", "lib.decorators.legacy.d.ts": "b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da"}
		if fmt.Sprintf("%x", sha256.Sum256(b)) != hashes[name] {
			panic("Pinned complete library bytes changed")
		}
		path := tspath.RootedFilePath("/lib/" + name)
		libs = append(libs, path)
		fsFiles[path.AsString()] = string(b)
	}
	options := &core.CompilerOptions{Target: core.ScriptTargetESNext, Module: core.ModuleKindESNext, ModuleResolution: core.ModuleResolutionKindBundler, NoLib: core.TSTrue, NoResolve: core.TSFalse, SkipLibCheck: core.TSTrue, NoEmit: core.TSTrue, AllowImportingTsExtensions: core.TSTrue, NoUncheckedSideEffectImports: core.TSTrue, ResolveJsonModule: core.TSFalse, AllowJs: core.TSFalse, Types: []string{}, StrictNullChecks: core.TSTrue, StrictFunctionTypes: core.TSTrue, NoImplicitAny: core.TSTrue, NoImplicitThis: core.TSTrue, StrictBindCallApply: core.TSFalse, NoImplicitReturns: core.TSFalse, ExactOptionalPropertyTypes: core.TSFalse, NoUncheckedIndexedAccess: core.TSFalse, StrictPropertyInitialization: core.TSFalse, StrictBuiltinIteratorReturn: core.TSFalse, UseUnknownInCatchVariables: core.TSFalse, ForceConsistentCasingInFileNames: core.TSTrue}
	fs := vfstest.FromMap(fsFiles, tspath.CaseSensitive)
	parsed, configErrors := tsoptions.GetParsedCommandLineOfConfigFile("/project/tsconfig.json", options, nil, fs, nil)
	if parsed == nil {
		panic(configErrors)
	}
	declaredRoots := parsed.FileNames()
	roots := append(libs, declaredRoots...)
	parsed = parsed.WithFileNames(roots)
	p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, SingleThreaded: core.TSTrue}, ProgramHosts: compiler.ProgramHosts{Host: compiler.NewCompilerHost(fs, "/lib", nil, nil, nil)}})
	ctx := context.Background()
	semantic := p.GetSemanticDiagnostics(ctx, nil)
	global := p.GetGlobalDiagnostics(ctx)
	program := p.GetProgramDiagnostics()
	config := p.GetConfigFileParsingDiagnostics()
	perFile := map[*ast.SourceFile][]*ast.Diagnostic{}
	for _, f := range p.SourceFiles() {
		perFile[f] = p.GetSemanticDiagnostics(ctx, f)
	}
	c, done := p.GetTypeChecker(ctx)
	defer done()
	if specifierProjectionMode {
		return moduleSpecifierProjection(p, c, r)
	}
	files := p.SourceFiles()
	ids := map[*ast.SourceFile]int{}
	e := encoder{file: files[len(files)-1], ids: map[*ast.Node]int{}}
	for i, f := range files {
		ids[f] = i
		e.node(f.AsNode())
	}
	cfg := parsed.ConfigFile.SourceFile
	ids[cfg] = len(files)
	e.node(cfg.AsNode())
	g := newGraph(&e)
	g.projectNames = func(name string, s *ast.Symbol) *Name {
		for _, d := range s.Declarations {
			if d.Kind == ast.KindSourceFile && d.AsSourceFile().ExternalModuleIndicator != nil && (d.Symbol() == s || checker.OracleProjectExportTypeState(c, s).Target == d.Symbol()) {
				want := "\"" + d.AsSourceFile().FileName().RemoveFileExtension().AsString() + "\""
				if name != want {
					panic("Module generated name differs from source owner")
				}
				return &Name{Kind: "module", Bytes: ptr(bytes(name)), OwnerNode: g.node(d)}
			}
		}
		return nil
	}
	s := semantics{c: c, g: g, types: map[*checker.Type]int{}, signatures: map[*checker.Signature]int{}, tr: []*TypeRow{}, sr: []*SignatureRow{}}
	queries := []any{}
	selected := []*ast.Node{}
	if r.Queries != nil {
		for _, q := range r.Queries {
			var found *ast.Node
			for _, n := range e.raw {
				f := ast.GetSourceFileOfNode(n)
				if f != nil && f.FileName().AsString() == "/project/"+q.File && n.Pos() == q.Pos && int(n.Kind) == q.Kind && (q.End == nil || n.End() == *q.End) {
					if found != nil {
						panic("Ambiguous source selector")
					}
					found = n
				}
			}
			if found == nil {
				panic("Missing source selector")
			}
			selected = append(selected, found)
		}
	} else {
		for _, n := range e.raw {
			f := ast.GetSourceFileOfNode(n)
			if f == nil || f.IsDeclarationFile || f == cfg {
				continue
			}
			if (ast.IsVariableDeclaration(n) || ast.IsImportSpecifier(n) || ast.IsNamespaceImport(n) || ast.IsImportClause(n)) && n.Name() != nil && ast.IsIdentifier(n.Name()) {
				selected = append(selected, n.Name())
			}
			if ast.IsCallExpression(n) {
				selected = append(selected, n)
			}
		}
	}
	for _, n := range selected {
		t := c.GetTypeAtLocation(n)
		symbol := c.GetSymbolAtLocation(n)
		var sig *checker.Signature
		var same any
		if ast.IsCallExpression(n) {
			sig = c.GetResolvedSignature(n)
			same = sig == c.GetResolvedSignature(n)
		}
		queries = append(queries, map[string]any{"nodeId": e.ids[n], "symbol": g.symbol(symbol), "type": s.t(t), "text": c.TypeToString(t), "signature": s.s(sig), "repeatType": t == c.GetTypeAtLocation(n), "repeatSymbol": symbol == c.GetSymbolAtLocation(n), "repeatSignature": same})
	}
	s.drain()
	modules := []any{}
	aliases := []any{}
	for _, f := range files {
		if f.ExternalModuleIndicator == nil {
			continue
		}
		symbol := f.AsNode().Symbol()
		es := c.GetExportsOfModule(symbol)
		sort.Slice(es, func(i, j int) bool { return es[i].Name < es[j].Name })
		out := []any{}
		for _, x := range es {
			out = append(out, map[string]any{"nameBase64": bytes(x.Name), "symbol": g.symbol(x)})
		}
		second := c.GetExportsOfModule(symbol)
		same := len(es) == len(second)
		for _, x := range es {
			found := false
			for _, y := range second {
				found = found || x == y
			}
			same = same && found
		}
		state := checker.OracleProjectModuleState(c, symbol)
		stars := []any{}
		keys := []string{}
		for k := range state.TypeOnlyStars {
			keys = append(keys, k)
		}
		sort.Strings(keys)
		for _, k := range keys {
			stars = append(stars, map[string]any{"nameBase64": bytes(k), "node": g.node(state.TypeOnlyStars[k])})
		}
		modules = append(modules, map[string]any{"symbol": g.symbol(symbol), "exports": out, "repeatSymbols": same, "storedPresent": state.Present, "storedExports": g.table(state.Exports), "typeOnlyStarsNull": state.TypeOnlyStars == nil, "typeOnlyStars": stars, "checked": state.Checked})
	}
	globals := g.table(checker.OracleProjectGlobals(c))
	globalThis := g.symbol(checker.OracleProjectGlobals(c)["globalThis"])
	binding := g.encode(files[len(files)-1])
	for i := 0; i < len(g.output.Symbols); i++ {
		var symbol *ast.Symbol
		for key, id := range g.symbolIDs {
			if id == i {
				symbol = key
				break
			}
		}
		if symbol.Flags&ast.SymbolFlagsAlias == 0 {
			continue
		}
		immediate := c.GetImmediateAliasedSymbol(symbol)
		target := c.GetAliasedSymbol(symbol)
		only := c.GetTypeOnlyAliasDeclaration(symbol)
		state := checker.OracleProjectAliasState(c, symbol)
		aliases = append(aliases, map[string]any{"symbol": i, "immediateTarget": g.symbol(immediate), "ultimateTarget": g.symbol(target), "typeOnlyDeclaration": g.node(only), "repeatTarget": target == c.GetAliasedSymbol(symbol), "storedPresent": state.Present, "storedImmediateTarget": g.symbol(state.ImmediateTarget), "storedAliasTarget": g.symbol(state.AliasTarget), "referenced": state.Referenced, "storedTypeOnlyDeclaration": g.node(state.TypeOnlyDeclaration)})
	}
	exportTypes := []any{}
	for i := 0; i < len(g.output.Symbols); i++ {
		var symbol *ast.Symbol
		for key, id := range g.symbolIDs {
			if id == i {
				symbol = key
				break
			}
		}
		state := checker.OracleProjectExportTypeState(c, symbol)
		if state.Present {
			exportTypes = append(exportTypes, map[string]any{"symbol": i, "storedPresent": true, "target": g.symbol(state.Target), "originatingImport": g.node(state.OriginatingImport)})
		}
	}
	for len(g.output.Nodes) < len(e.raw) {
		i := len(g.output.Nodes)
		out := BoundNode{NodeID: i, Fields: []BoundField{}}
		g.fields(readable(reflect.ValueOf(e.raw[i]).Elem().FieldByName("data")).Elem().Elem(), &out)
		g.output.Nodes = append(g.output.Nodes, out)
	}
	for i, n := range e.raw {
		if n.Parent != nil {
			parent := &Parent{Kind: n.Parent.Kind, Pos: n.Parent.Pos(), End: n.Parent.End()}
			if id, ok := e.ids[n.Parent]; ok {
				parent.ID = ptr(id)
			}
			e.output[i].Parent = parent
		}
	}
	binding = g.output
	unrooted := []any{}
	inventory := []any{}
	for _, store := range checker.OracleProjectStoreKeys(c) {
		owners := []int{}
		for _, symbol := range store.Keys {
			id, ok := g.symbolIDs[symbol]
			if !ok {
				if store.Name != "exportTypeLinks" {
					panic("Unrooted actual alias/module key")
				}
				state := checker.OracleProjectExportTypeState(c, symbol)
				if !state.Present || state.Target == nil || len(symbol.Declarations) != 1 || symbol.Declarations[0].Kind != ast.KindSourceFile || symbol.Declarations[0].Symbol() != state.Target || symbol.Name != state.Target.Name || symbol.Flags != (state.Target.Flags|ast.SymbolFlagsTransient) || symbol.CheckFlags != 0 || symbol.Parent != state.Target.Parent || symbol.ValueDeclaration != state.Target.ValueDeclaration {
					panic("Unknown unrooted export clone producer")
				}
				same := func(a, b ast.SymbolTable) bool {
					if (a == nil) != (b == nil) || len(a) != len(b) {
						return false
					}
					for k, v := range a {
						if b[k] != v {
							return false
						}
					}
					return true
				}
				if !same(symbol.Members, state.Target.Members) || !same(symbol.Exports, state.Target.Exports) {
					panic("Unrooted clone table differs from target")
				}
				target, known := g.symbolIDs[state.Target]
				if !known {
					panic("Unrooted clone target")
				}
				var origin *int
				if state.OriginatingImport != nil {
					id, known := e.ids[state.OriginatingImport]
					if !known {
						panic("Unrooted clone import")
					}
					origin = ptr(id)
				}
				unrooted = append(unrooted, map[string]any{"target": target, "originatingImport": origin, "ownerNode": e.ids[symbol.Declarations[0]], "flags": uint32(symbol.Flags), "checkFlags": uint32(symbol.CheckFlags), "nameBase64": bytes(symbol.Name), "count": 1})
				continue
			}
			owners = append(owners, id)
		}
		sort.Ints(owners)
		inventory = append(inventory, map[string]any{"store": store.Name, "null": store.Keys == nil, "count": len(store.Keys), "owners": owners, "unrooted": len(store.Keys) - len(owners)})
	}
	sort.Slice(unrooted, func(i, j int) bool {
		a := unrooted[i].(map[string]any)
		b := unrooted[j].(map[string]any)
		if a["ownerNode"] != b["ownerNode"] {
			return a["ownerNode"].(int) < b["ownerNode"].(int)
		}
		get := func(v any) int {
			if v == nil {
				return -1
			}
			return *v.(*int)
		}
		if get(a["originatingImport"]) != get(b["originatingImport"]) {
			return get(a["originatingImport"]) < get(b["originatingImport"])
		}
		if a["target"] != b["target"] {
			return a["target"].(int) < b["target"].(int)
		}
		return a["nameBase64"].(string) < b["nameBase64"].(string)
	})
	compact := []any{}
	for _, item := range unrooted {
		if len(compact) > 0 {
			a := compact[len(compact)-1].(map[string]any)
			b := item.(map[string]any)
			aa := map[string]any{}
			for k, v := range a {
				aa[k] = v
			}
			aa["count"] = 1
			x, _ := json.Marshal(aa)
			y, _ := json.Marshal(b)
			if string(x) == string(y) {
				a["count"] = a["count"].(int) + 1
				continue
			}
		}
		compact = append(compact, item)
	}
	resolutions := []any{}
	records := []any{}
	owners := []any{}
	local := make([]int, len(ids))
	for i, f := range append(files, cfg) {
		records = append(records, map[string]any{"index": i, "fileName": f.FileName().AsString(), "sha256": fmt.Sprintf("%x", sha256.Sum256([]byte(f.Text()))), "rootNodeId": e.ids[f.AsNode()], "programFile": f != cfg, "declaration": f.IsDeclarationFile, "defaultLibrary": f != cfg && p.IsSourceFileDefaultLibrary(f.PathKey()), "externalModule": f.ExternalModuleIndicator != nil, "skipTypeChecking": f != cfg && p.SkipTypeChecking(f, false), "isBound": f.IsBound(), "symbolCount": f.SymbolCount, "parseDiagnostics": full(f.Diagnostics(), ids), "bindDiagnostics": full(f.BindDiagnostics(), ids), "semanticDiagnostics": full(perFile[f], ids)})
		if f == cfg {
			continue
		}
		for _, sp := range f.Imports() {
			r := p.GetResolvedModuleFromModuleSpecifier(f, sp)
			row := map[string]any{"fileIndex": i, "specifierNodeId": e.ids[sp], "specifierBase64": bytes(sp.Text()), "mode": p.GetModeForUsageLocation(f, sp), "present": r != nil, "resolved": r.IsResolved(), "fileName": nil, "path": nil, "originalPath": nil, "extension": nil, "usingTsExtension": false, "usingExtraExtensions": false, "externalLibrary": false, "custom": false, "alternateResult": nil, "repeatSame": r == p.GetResolvedModuleFromModuleSpecifier(f, sp)}
			if r != nil {
				row["fileName"] = r.ResolvedFileName
				row["path"] = r.ResolvedPath
				row["originalPath"] = r.OriginalPath
				row["extension"] = r.Extension
				row["usingTsExtension"] = r.ResolvedUsingTsExtension
				row["usingExtraExtensions"] = r.ResolvedUsingExtraExtensions
				row["externalLibrary"] = r.IsExternalLibraryImport
				row["custom"] = r.IsCustomResolution
				row["alternateResult"] = r.AlternateResult
			}
			resolutions = append(resolutions, row)
		}
	}
	for i, n := range e.raw {
		f := ast.GetSourceFileOfNode(n)
		if f == nil {
			panic("Unknown generated node owner")
		}
		index, ok := ids[f]
		if !ok {
			panic("Node outside project/config sources")
		}
		owners = append(owners, map[string]any{"nodeId": i, "fileIndex": index, "fileNodeId": local[index]})
		local[index]++
	}
	exit := 0
	if len(semantic)+len(global)+len(program)+len(config) > 0 {
		exit = 1
	}
	return map[string]any{"exitCode": exit, "unsupportedFeature": nil, "files": records, "nodes": e.output, "nodeOwners": owners, "binding": binding, "globals": globals, "globalThisSymbol": globalThis, "queries": queries, "types": s.tr, "signatures": s.sr, "aliases": aliases, "modules": modules, "exportTypes": exportTypes, "unrootedExportClones": compact, "resolutions": resolutions, "inventories": inventory, "globalDiagnostics": full(global, ids), "programDiagnostics": full(program, ids), "configDiagnostics": full(config, ids), "declaredRoots": declaredRoots, "options": map[string]int{"Target": int(p.Options().Target), "Module": int(p.Options().Module), "ModuleResolution": int(p.Options().ModuleResolution), "NoLib": int(p.Options().NoLib), "NoResolve": int(p.Options().NoResolve), "SkipLibCheck": int(p.Options().SkipLibCheck), "NoEmit": int(p.Options().NoEmit), "AllowImportingTsExtensions": int(p.Options().AllowImportingTsExtensions), "NoUncheckedSideEffectImports": int(p.Options().NoUncheckedSideEffectImports), "ResolveJsonModule": int(p.Options().ResolveJsonModule), "AllowJs": int(p.Options().AllowJs), "StrictNullChecks": int(p.Options().StrictNullChecks), "StrictFunctionTypes": int(p.Options().StrictFunctionTypes), "NoImplicitAny": int(p.Options().NoImplicitAny), "NoImplicitThis": int(p.Options().NoImplicitThis), "StrictBindCallApply": int(p.Options().StrictBindCallApply), "NoImplicitReturns": int(p.Options().NoImplicitReturns), "ExactOptionalPropertyTypes": int(p.Options().ExactOptionalPropertyTypes), "NoUncheckedIndexedAccess": int(p.Options().NoUncheckedIndexedAccess), "StrictPropertyInitialization": int(p.Options().StrictPropertyInitialization), "StrictBuiltinIteratorReturn": int(p.Options().StrictBuiltinIteratorReturn), "UseUnknownInCatchVariables": int(p.Options().UseUnknownInCatchVariables), "ForceConsistentCasingInFileNames": int(p.Options().ForceConsistentCasingInFileNames), "DownlevelIteration": int(p.Options().DownlevelIteration)}, "typesOption": p.Options().Types}
}
func main() {
	if len(os.Args) == 2 && os.Args[1] == "--specifier-projection" {
		specifierProjectionMode = true
	} else if len(os.Args) > 1 && os.Args[1] != "--support-projections" {
		panic("Unknown oracle arguments")
	}
	if len(os.Args) == 2 && os.Args[1] == "--support-projections" {
		if e := json.NewEncoder(os.Stdout).Encode(supportProjection()); e != nil {
			panic(e)
		}
		return
	}
	scanner := bufio.NewScanner(os.Stdin)
	scanner.Buffer(make([]byte, 4096), 20<<20)
	out := json.NewEncoder(os.Stdout)
	for scanner.Scan() {
		var r ProjectRequest
		if e := json.Unmarshal(scanner.Bytes(), &r); e != nil {
			panic(e)
		}
		out.Encode(run(r))
	}
	if e := scanner.Err(); e != nil {
		panic(e)
	}
}
