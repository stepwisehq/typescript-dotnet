// Authored public diagnostic projection reuses accepted no-emit diagnostic fields.
package compiler_test
import (
 "context"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
)
type tsRepopulateValue struct {Kind int; ModuleReference string; Mode int; PackageName string}
type tsDiagnosticValue struct {
 File *string
 Start,Length,End,Code,Category int
 Key,Message string
 Args []string
 Source string
 ReportsUnnecessary,ReportsDeprecated,SkippedOnNoEmit bool
 Chain,Related []tsDiagnosticValue
 Repopulate *tsRepopulateValue
}
type tsDiagnosticChannels struct {Config,Program,Syntactic,Global,Semantic []tsDiagnosticValue}
func tsProjectDiagnostics(ds []*ast.Diagnostic) []tsDiagnosticValue {
 result:=make([]tsDiagnosticValue,0,len(ds))
 for _,d:=range ds {
  var file *string;if d.File()!=nil {name:=d.File().FileName().AsString();file=&name}
  var repopulate *tsRepopulateValue;if r:=d.RepopulateInfo();r!=nil {repopulate=&tsRepopulateValue{int(r.Kind),r.ModuleReference,int(r.Mode),r.PackageName}}
  result=append(result,tsDiagnosticValue{File:file,Start:d.Pos(),Length:d.Len(),End:d.End(),Code:int(d.Code()),Category:int(d.Category()),Key:string(d.MessageKey()),Message:d.String(),Args:d.MessageArgs(),Source:d.Source(),ReportsUnnecessary:d.ReportsUnnecessary(),ReportsDeprecated:d.ReportsDeprecated(),SkippedOnNoEmit:d.SkippedOnNoEmit(),Chain:tsProjectDiagnostics(d.MessageChain()),Related:tsProjectDiagnostics(d.RelatedInformation()),Repopulate:repopulate})
 }
 return result
}
func tsPublicDiagnostics(p *compiler.Program) tsDiagnosticChannels {
 // Check all applicable files first, then observe any globals discovered by semantics.
 semantic:=p.GetSemanticDiagnostics(context.Background(),nil)
 return tsDiagnosticChannels{Config:tsProjectDiagnostics(p.GetConfigFileParsingDiagnostics()),Program:tsProjectDiagnostics(p.GetProgramDiagnostics()),Syntactic:tsProjectDiagnostics(p.GetSyntacticDiagnostics(context.Background(),nil)),Global:tsProjectDiagnostics(p.GetGlobalDiagnostics(context.Background())),Semantic:tsProjectDiagnostics(semantic)}
}
