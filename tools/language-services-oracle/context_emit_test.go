// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// Authored differential control grounded in server.go diagnostic-flake flow.
// Pinned TypeScript fed0bf24149fb1ed36039212648bafdafc1ea10e; additive overlay only.
package project_test

import (
 "context"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "fmt"
 "slices"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
 "github.com/microsoft/TypeScript/tsc/internal/lsp/lsproto"
 "github.com/microsoft/TypeScript/tsc/internal/testutil/projecttestutil"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
)

func TestAuthoredSessionDiagnosticsEmitContext(t *testing.T) {
 for _, noEmitOnError := range []bool{false, true} {
 for _, row := range []struct{Name, Text string}{
  {"Valid", "export const value: number = 1;\n"},
  {"Invalid", "export function echo(value) { return value; }\nexport const value: number = \"bad\";\n"},
 } {
  t.Run(fmt.Sprintf("%s-NoEmitOnError-%v", row.Name, noEmitOnError), func(t *testing.T) {
   session, _ := projecttestutil.Setup(map[string]any{
    "/fixture/tsconfig.json": fmt.Sprintf(`{"compilerOptions":{"skipLibCheck":true,"strict":true,"declaration":true,"target":"esnext","module":"esnext","noEmitOnError":%v},"files":["input.ts"]}`,noEmitOnError),
    "/fixture/input.ts": row.Text,
   })
   defer session.Close()
   ctx := core.WithCheckerLifetime(core.WithRequestID(context.Background(), "authored-context-emit-"+row.Name), core.CheckerLifetimeDiagnostics)
   uri := lsproto.DocumentUri("file:///fixture/input.ts")
   session.DidOpenFile(ctx, uri, 1, row.Text, lsproto.LanguageKindTypeScript)
   service, err := session.GetLanguageService(ctx, uri); if err != nil { t.Fatal(err) }
   before, err := service.ProvideDiagnostics(ctx, uri); if err != nil { t.Fatal(err) }
   beforeJSON, err := tsjson.Marshal(before); if err != nil { t.Fatal(err) }
   type Output struct { Name string; Text string }
   var outputs []Output
   emitted := service.GetProgram().Emit(ctx, compiler.EmitOptions{WriteFile: func(name tspath.RootedFilePath, text string, _ *compiler.WriteFileData) error {
    outputs = append(outputs, Output{string(name), text}); return nil
   }})
   if emitted == nil { t.Fatal("uncancelled emit returned nil") }
   after, err := service.ProvideDiagnostics(ctx, uri); if err != nil { t.Fatal(err) }
   afterJSON, err := tsjson.Marshal(after); if err != nil { t.Fatal(err) }
   if string(beforeJSON) != string(afterJSON) { t.Fatalf("diagnostic flake before=%s after=%s", beforeJSON, afterJSON) }
   slices.SortFunc(outputs, func(a,b Output) int { if a.Name<b.Name{return -1};if a.Name>b.Name{return 1};return 0 })
   observation := struct{ Name string; NoEmitOnError bool; Before string; After string; EmitSkipped bool; Diagnostics []contextEmitDiagnostic; Outputs []Output }{row.Name,noEmitOnError,string(beforeJSON),string(afterJSON),emitted.EmitSkipped,contextEmitProjectDiagnostics(emitted.Diagnostics),outputs}
   encoded, err := tsjson.Marshal(observation); if err != nil {t.Fatal(err)}
   fmt.Printf("CONTEXT_EMIT_OBSERVATION %s\n",encoded)
  })
 }
}

}

// Complete accepted public diagnostic field projection, reused for EmitResult.
type contextEmitRepopulateValue struct {Kind int; ModuleReference string; Mode int; PackageName string}
type contextEmitDiagnostic struct {
 File *string
 Start,Length,End,Code,Category int
 Key,Message string
 Args []string
 Source string
 ReportsUnnecessary,ReportsDeprecated,SkippedOnNoEmit bool
 Chain,Related []contextEmitDiagnostic
 Repopulate *contextEmitRepopulateValue
}
func contextEmitProjectDiagnostics(ds []*ast.Diagnostic) []contextEmitDiagnostic {
 result:=make([]contextEmitDiagnostic,0,len(ds))
 for _,d:=range ds {
  var file *string;if d.File()!=nil {name:=d.File().FileName().AsString();file=&name}
  var repopulate *contextEmitRepopulateValue;if r:=d.RepopulateInfo();r!=nil {repopulate=&contextEmitRepopulateValue{int(r.Kind),r.ModuleReference,int(r.Mode),r.PackageName}}
  result=append(result,contextEmitDiagnostic{File:file,Start:d.Pos(),Length:d.Len(),End:d.End(),Code:int(d.Code()),Category:int(d.Category()),Key:string(d.MessageKey()),Message:d.String(),Args:d.MessageArgs(),Source:d.Source(),ReportsUnnecessary:d.ReportsUnnecessary(),ReportsDeprecated:d.ReportsDeprecated(),SkippedOnNoEmit:d.SkippedOnNoEmit(),Chain:contextEmitProjectDiagnostics(d.MessageChain()),Related:contextEmitProjectDiagnostics(d.RelatedInformation()),Repopulate:repopulate})
 }
 return result
}
