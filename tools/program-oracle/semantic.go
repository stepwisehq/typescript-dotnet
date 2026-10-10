// Authored opt-in observations of genuine Program semantic APIs.
// Accepted construction-only requests keep their existing output unchanged.
package main

import (
	"context"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type semanticInput struct {
	PerFile, Repeat  bool
	IncrementalFiles []string
	Queries          []semanticQuery
}

type semanticQuery struct {
	File, Identifier, Operation string
	Occurrence                  int
}

func observeSemanticQuery(o *observer, p *compiler.Program, q semanticQuery) any {
	if q.Occurrence < 0 {
		panic("Negative semantic identifier occurrence")
	}
	file := p.GetSourceFile(tspath.RootedFilePath(q.File))
	if file == nil {
		panic("Unknown semantic query file")
	}
	var selected *ast.Node
	occurrence := 0
	var visit ast.Visitor
	visit = func(node *ast.Node) bool {
		if ast.IsIdentifier(node) && node.Text() == q.Identifier {
			if occurrence == q.Occurrence {
				selected = node
			}
			occurrence++
		}
		node.ForEachChild(visit)
		return false
	}
	visit(file.AsNode())
	if selected == nil {
		panic("Unknown semantic identifier occurrence")
	}
	c, done := p.GetTypeChecker(context.Background())
	defer done()
	result := map[string]any{"file": o.file(file), "pos": selected.Pos(), "end": selected.End(), "kind": selected.Kind, "operation": q.Operation}
	if q.Operation == "symbol" {
		symbol := c.GetSymbolAtLocation(selected)
		if symbol == nil {
			result["symbol"] = nil
		} else {
			result["symbol"] = map[string]any{"nameBase64": b64(symbol.Name), "flags": symbol.Flags}
		}
		return result
	}
	t := c.GetTypeAtLocation(selected)
	if q.Operation == "declared" || q.Operation == "base" {
		symbol := c.GetSymbolAtLocation(selected)
		if symbol == nil {
			panic("Declared-type query requires actual symbol")
		}
		t = c.GetDeclaredTypeOfSymbol(symbol)
	}
	result["typeBase64"] = b64(c.TypeToString(t))
	switch q.Operation {
	case "type", "declared":
	case "properties":
		properties := c.GetPropertiesOfType(t)
		var rows []any
		if properties != nil {
			rows = []any{}
		}
		for _, prop := range properties {
			// Actual public display belongs to this same property object. Raw escaped
			// symbol names may contain incidental source node/symbol allocation IDs.
			rows = append(rows, map[string]any{"displayNameBase64": b64(c.SymbolToString(prop)), "typeBase64": b64(c.TypeToString(c.GetTypeOfSymbol(prop)))})
		}
		result["properties"] = rows
	case "base":
		bases := c.GetBaseTypes(t)
		var rows []any
		if bases != nil {
			rows = []any{}
		}
		for _, base := range bases {
			rows = append(rows, b64(c.TypeToString(base)))
		}
		result["bases"] = rows
	case "construct":
		signatures := c.GetSignaturesOfType(t, checker.SignatureKindConstruct)
		var rows []any
		if signatures != nil {
			rows = []any{}
		}
		for _, signature := range signatures {
			parameters := signature.Parameters()
			var parameterRows []any
			if parameters != nil {
				parameterRows = []any{}
			}
			for _, parameter := range parameters {
				parameterRows = append(parameterRows, map[string]any{"nameBase64": b64(parameter.Name), "typeBase64": b64(c.TypeToString(c.GetTypeOfSymbol(parameter)))})
			}
			rows = append(rows, map[string]any{"flags": signature.Flags(), "minArgumentCount": signature.MinArgumentCount(), "parameters": parameterRows, "returnTypeBase64": b64(c.TypeToString(c.GetReturnTypeOfSignature(signature)))})
		}
		result["constructSignatures"] = rows
	default:
		panic("Unknown semantic query operation")
	}
	return result
}

func observeSemantics(o *observer, p *compiler.Program, input *semanticInput) any {
	ctx := context.Background()
	result := map[string]any{
		"globalBefore": o.diagnostics(p.GetGlobalDiagnostics(ctx)),
		"bulk":         o.diagnostics(p.GetSemanticDiagnostics(ctx, nil)),
	}
	if input.PerFile {
		rows := []any{}
		for _, file := range p.SourceFiles() {
			rows = append(rows, map[string]any{"file": o.file(file), "diagnostics": o.diagnostics(p.GetSemanticDiagnostics(ctx, file))})
		}
		result["perFile"] = rows
	}
	if input.IncrementalFiles != nil {
		files := []*ast.SourceFile{}
		for _, name := range input.IncrementalFiles {
			file := p.GetSourceFile(tspath.RootedFilePath(name))
			if file == nil {
				panic("Unknown incremental semantic fixture file")
			}
			files = append(files, file)
		}
		actual := p.GetSemanticDiagnosticsForIncremental(ctx, files)
		rows := []any{}
		for _, file := range files {
			rows = append(rows, map[string]any{"file": o.file(file), "diagnostics": o.diagnostics(actual[file])})
		}
		result["incremental"] = rows
	}
	result["globalAfter"] = o.diagnostics(p.GetGlobalDiagnostics(ctx))
	if input.Repeat {
		result["repeatBulk"] = o.diagnostics(p.GetSemanticDiagnostics(ctx, nil))
		result["repeatGlobal"] = o.diagnostics(p.GetGlobalDiagnostics(ctx))
	}
	if input.Queries != nil {
		rows := []any{}
		for _, query := range input.Queries {
			rows = append(rows, observeSemanticQuery(o, p, query))
		}
		result["queries"] = rows
		result["globalAfterQueries"] = o.diagnostics(p.GetGlobalDiagnostics(ctx))
	}
	return result
}
