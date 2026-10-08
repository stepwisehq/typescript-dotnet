package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/diagnostics"
	"reflect"
)

func diagnosticSnapshot(d *ast.Diagnostic) any {
	args := []string{}
	for _, a := range d.MessageArgs() {
		args = append(args, b(a))
	}
	chain := []any{}
	for _, c := range d.MessageChain() {
		chain = append(chain, diagnosticSnapshot(c))
	}
	related := []any{}
	for _, c := range d.RelatedInformation() {
		related = append(related, diagnosticSnapshot(c))
	}
	return map[string]any{"code": d.Code(), "category": d.Category(), "pos": d.Pos(), "end": d.End(), "key": d.MessageKey(), "source": b(d.Source()), "messageText": b(d.MessageText()), "text": d.String(), "args": args, "argsNull": d.MessageArgs() == nil, "chain": chain, "chainNull": d.MessageChain() == nil, "related": related, "relatedNull": d.RelatedInformation() == nil, "unnecessary": d.ReportsUnnecessary(), "deprecated": d.ReportsDeprecated(), "skipped": d.SkippedOnNoEmit()}
}
func diagnosticData() any {
	file := parse("const x=1", core.ScriptKindTS)
	chain := ast.NewDiagnosticFromText(nil, core.NewTextRange(0, 0), 999, diagnostics.CategoryError, "chain é", nil, nil, false, false)
	related := ast.NewDiagnosticFromText(file, core.NewTextRange(1, 2), 998, diagnostics.CategoryWarning, "related", nil, nil, false, false)
	d := ast.NewDiagnosticFromSerialized(file, core.NewTextRange(3, 7), 2575, diagnostics.CategoryError, "No_overload_expects_0_arguments_but_overloads_do_exist_that_expect_either_1_or_2_arguments_2575", []string{"7", "2", "3"}, []*ast.Diagnostic{chain}, []*ast.Diagnostic{related}, true, true, true)
	clone := d.Clone()
	first := diagnosticSnapshot(d)
	d.SetExternalData("mapped", "localized é")
	second := diagnosticSnapshot(d)
	unknown := ast.NewDiagnosticFromSerialized(nil, core.NewTextRange(0, 0), 123, diagnostics.CategoryError, "nonexistent-key", nil, nil, nil, false, false, false)
	col := &ast.DiagnosticsCollection{}
	emptyGlobal := col.GetGlobalDiagnostics() == nil
	emptyAll := col.GetDiagnostics() == nil
	canonical := col.Add(clone)
	duplicate := col.Add(clone.Clone())
	return map[string]any{"serialized": first, "externalOverride": second, "unknownKeyThrows": caught(func() { unknown.String() }), "cloneDistinct": clone != d, "cloneSharesArgs": reflect.ValueOf(clone.MessageArgs()).Pointer() == reflect.ValueOf(d.MessageArgs()).Pointer(), "cloneSharesChain": reflect.ValueOf(clone.MessageChain()).Pointer() == reflect.ValueOf(d.MessageChain()).Pointer(), "canonicalIdentity": canonical == clone, "duplicateIdentity": duplicate == clone, "globalInitiallyNil": emptyGlobal, "allInitiallyNil": emptyAll, "equivalentFileCount": len(col.GetDiagnosticsForFile(parse("", core.ScriptKindTS)))}
}
