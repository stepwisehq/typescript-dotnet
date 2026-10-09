// Authored observer sensitivity controls; these seeded graphs are not claimed as
// naturally produced binding aliases. Directive lifecycle invokes actual scanner.
package main
import (
 "encoding/json"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/binder"
 "github.com/microsoft/TypeScript/tsc/internal/scanner"
)
func completionHelperObservations() map[string]any {
 result:=helperObservations();result["DirectiveLifecycle"]=directiveLifecycle();result["ObserverSensitivity"]=observerSensitivity();result["OrderSensitivity"]=orderSensitivity();return result
}
func directiveLifecycle() []any {
 s:=scanner.NewScanner();rows:=[]any{}
 capture:=func(stage string){directives:=[]any{};for _,d:=range s.CommentDirectives(){directives=append(directives,map[string]any{"pos":d.Loc.Pos(),"end":d.Loc.End(),"kind":int(d.Kind)})};rows=append(rows,map[string]any{"stage":stage,"nil":s.CommentDirectives()==nil,"directives":directives,"token":int(s.Token()),"start":s.TokenStart(),"end":s.TokenEnd()})}
 capture("new");s.SetText("// @ts-ignore\nx;\n// @ts-expect-error\ny;");capture("set-directives");zero:=s.Mark();s.Scan();capture("first");one:=s.Mark();s.Scan();capture("semicolon");s.Scan();capture("second");s.Rewind(one);capture("rewind-one");s.Rewind(zero);capture("rewind-zero");s.Scan();capture("replay-first");s.SetText("plain;");capture("set-plain");s.Scan();capture("plain-token");s.SetText("");capture("clear-by-empty-text");s.Reset();capture("reset");s.SetText("// @ts-ignore\nx;");s.Scan();capture("new-directive");s.Reset();capture("reset-populated");s.Reset();capture("repeat-reset");return rows
}
func observerSensitivity() map[string]any {
 file:=supportParse("a; b;");binder.BindSourceFile(file)
 shared:=ast.SymbolTable{};owner:=&ast.Symbol{Name:"observer",Flags:ast.SymbolFlagsProperty,Members:shared,Exports:shared};file.Symbol=owner;file.Locals=shared
 tail:=&ast.FlowList{Flow:&ast.FlowNode{Flags:ast.FlowFlagsStart}}
 head:=&ast.FlowList{Flow:&ast.FlowNode{Flags:ast.FlowFlagsAssignment},Next:tail}
 loop:=&ast.FlowNode{Flags:ast.FlowFlagsLoopLabel,Antecedents:head};head.Flow.Antecedent=loop
 file.Statements.Nodes[0].Expression().AsIdentifier().FlowNode=loop
 file.Statements.Nodes[1].Expression().AsIdentifier().FlowNode=&ast.FlowNode{Flags:ast.FlowFlagsBranchLabel,Antecedents:tail}
 r:=coldRequest(Request{});_,binding,_:=bindSnapshot(r,file)
 encoded,err:=json.Marshal(binding);if err!=nil{panic(err)}
 var value any;if err:=json.Unmarshal(encoded,&value);err!=nil{panic(err)}
 return map[string]any{"seededOnly":true,"binding":value}
}

func orderSensitivity() map[string]any {
 r:=CompletionRequest{Parse:Request{Text:"class A { #foo=1; m(){ const { #foo: x } = this; let bar; ({ #foo: bar } = this); }}",FileName:"/same.ts",ScriptKind:3}}
 before:=runCompletion(r)
 for i:=0;i<8;i++{runCompletion(CompletionRequest{Parse:Request{Text:"class Prefix { #p=1; }",FileName:"/same.ts",ScriptKind:3}})}
 after:=runCompletion(r)
 first,e:=json.Marshal(before);if e!=nil{panic(e)};second,e:=json.Marshal(after);if e!=nil{panic(e)}
 kinds:=[]string{};for _,symbol:=range after.SymbolHelpers {kinds=append(kinds,symbol.DisplayName.Kind)}
 return map[string]any{"sameAfterPrivatePrefixes":string(first)==string(second),"displayKinds":kinds,"hasGeneratedPrivateDisplay":containsDisplayKind(kinds,"private"),"hasOrdinaryPrivateDisplay":hasOrdinaryPrivateDisplay(after)}
}

func containsDisplayKind(kinds []string,kind string)bool {for _,value:=range kinds {if value==kind{return true}};return false}
func hasOrdinaryPrivateDisplay(result CompletionResult)bool{for _,symbol:=range result.SymbolHelpers{if symbol.DisplayName.Kind=="raw"&&symbol.DisplayName.Bytes!=nil&&*symbol.DisplayName.Bytes==bytes("#foo"){return true}};return false}
