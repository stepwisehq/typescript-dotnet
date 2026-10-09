// Authored graph projection: actual original loader results, no semantic checker.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"sort"
)

func (o *observer) graph(parsed *tsoptions.ParsedCommandLine, h *host) any {
	graph := compiler.ObserveResolutionGraph(parsed, h)
	files := []any{}
	ids := map[*ast.SourceFile]int{}
	for _, file := range graph.Files {
		id := o.file(file)
		ids[file] = *id
		files = append(files, map[string]any{"identity": id, "fileName": value(file.FileName()), "path": value(file.PathKey()), "text": value(file.Text()), "scriptKind": file.ScriptKind, "isDeclarationFile": file.IsDeclarationFile, "externalModule": file.ExternalModuleIndicator != nil, "parseDiagnostics": o.diagnostics(file.Diagnostics()), "metadata": value(graph.Metadata[file.PathKey()])})
	}
	ownerID := func(file *ast.SourceFile) int {
		id, ok := ids[file]
		if !ok {
			panic("Unrooted graph association owner")
		}
		return id
	}
	target := func(file *ast.SourceFile) any {
		if file == nil {
			return nil
		}
		return o.file(file)
	}
	moduleIDs := map[any]int{}
	moduleID := func(result any) int {
		if id, ok := moduleIDs[result]; ok {
			return id
		}
		id := len(moduleIDs)
		moduleIDs[result] = id
		return id
	}
	sort.SliceStable(graph.Edges, func(i, j int) bool {
		a, b := graph.Edges[i], graph.Edges[j]
		if _, ok := ids[a.File]; !ok {
			panic("Unrooted module edge owner")
		}
		if _, ok := ids[b.File]; !ok {
			panic("Unrooted module edge owner")
		}
		if ownerID(a.File) != ownerID(b.File) {
			return ownerID(a.File) < ownerID(b.File)
		}
		return a.ImportIndex < b.ImportIndex
	})
	edges := []any{}
	for _, edge := range graph.Edges {
		var errorText any
		if edge.Error != nil {
			errorText = b64(edge.Error.Error())
		}
		edges = append(edges, map[string]any{"file": o.file(edge.File), "specifier": map[string]any{"kind": edge.Specifier.Kind, "pos": edge.Specifier.Pos(), "end": edge.Specifier.End()}, "reference": value(edge.Name), "mode": edge.Mode, "importIndex": edge.ImportIndex, "syntheticKind": edge.SyntheticKind, "identity": moduleID(edge.Resolution), "resolution": o.module(edge.Resolution), "trace": traces(edge.Traces), "errorBase64": errorText, "scheduling": edge.Scheduling, "target": target(edge.Target)})
	}
	sort.SliceStable(graph.Paths, func(i, j int) bool {
		a, b := graph.Paths[i], graph.Paths[j]
		if ownerID(a.File) != ownerID(b.File) {
			return ownerID(a.File) < ownerID(b.File)
		}
		return a.Reference.Pos() < b.Reference.Pos()
	})
	sort.SliceStable(graph.Types, func(i, j int) bool {
		a, b := graph.Types[i], graph.Types[j]
		if ownerID(a.File) != ownerID(b.File) {
			return ownerID(a.File) < ownerID(b.File)
		}
		return a.Reference.Pos() < b.Reference.Pos()
	})
	paths := []any{}
	for _, edge := range graph.Paths {
		paths = append(paths, map[string]any{"file": o.file(edge.File), "reference": value(edge.Reference), "resolvedFile": value(edge.ResolvedFile), "resolvedPath": value(edge.ResolvedPath), "diagnostic": o.diagnostics(singleDiagnostic(edge.Diagnostic)), "scheduling": edge.Scheduling, "target": target(edge.Target)})
	}
	types := []any{}
	for _, edge := range graph.Types {
		types = append(types, map[string]any{"file": o.file(edge.File), "reference": value(edge.Reference), "mode": edge.Mode, "resolution": o.directive(edge.Resolution), "trace": traces(edge.Traces), "scheduling": edge.Scheduling, "target": target(edge.Target)})
	}
	externalLibs := []any{}
	for _, file := range graph.Files {
		for _, ref := range file.LibReferenceDirectives {
			externalLibs = append(externalLibs, map[string]any{"file": o.file(file), "reference": value(ref), "scheduling": "ExternalLibraryEnvironment"})
		}
	}
	sort.Slice(graph.Missing, func(i, j int) bool { return graph.Missing[i] < graph.Missing[j] })
	return map[string]any{"roots": value(graph.Roots), "files": files, "edges": edges, "pathReferences": paths, "typeDirectives": types, "externalLibReferences": externalLibs, "missingFiles": value(graph.Missing), "inclusionDiagnostics": o.diagnostics(graph.InclusionDiagnostics)}
}
func singleDiagnostic(d *ast.Diagnostic) []*ast.Diagnostic {
	if d == nil {
		return nil
	}
	return []*ast.Diagnostic{d}
}
