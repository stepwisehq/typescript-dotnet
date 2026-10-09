// Authored cold-binding observations; every computed value calls the pinned Go API.
package main

import (
 "bufio"
 "encoding/json"
 "fmt"
 "os"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/binder"
)

type CompletionRequest struct {
 Parse Request `json:"parse"`
 WarmBeforeBind bool `json:"warmBeforeBind"`
 ResolveJSDocAfterBind bool `json:"resolveJSDocAfterBind"`
}
type SymbolAPI struct {
 SymbolID int `json:"symbolId"`
 SourceOwned bool `json:"sourceOwned"`
 ExternalModule bool `json:"externalModule"`
 Static bool `json:"static"`
 CombinedFlags uint32 `json:"combinedFlags"`
 DisplayName Name `json:"displayName"`
 CheckFlags uint32 `json:"checkFlags"`
}
type ContainerAPI struct {
 NodeID int `json:"nodeId"`
 Flags uint32 `json:"flags"`
}
type CompletionResult struct {
 Graph Result `json:"graph"`
 MetadataNulls map[string]bool `json:"metadataNulls"`
 ObservationOrder []string `json:"observationOrder"`
 BindDiagnosticsNull bool `json:"bindDiagnosticsNull"`
 BindDiagnosticStates []DiagnosticState `json:"bindDiagnosticStates"`
 SymbolHelpers []SymbolAPI `json:"symbolHelpers"`
 ContainerHelpers []ContainerAPI `json:"containerHelpers"`
 ObservedSyntax *Response `json:"observedSyntax"`
}
func coldRequest(r Request) Request {
 r.ResolveJSDoc = false
 r.FoundationObservations = false
 r.TextProbeIDs = nil
 return r
}
func bindSnapshot(r Request, file *ast.SourceFile) (Response,Binding,*graph) {
 after:=runParser(r,file)
 g:=newGraph(lastSyntaxEncoder)
 binding:=g.encode(file)
 after.Nodes=g.syntax.output
 return after,binding,g
}
func metadataNulls(file *ast.SourceFile) map[string]bool {
 return map[string]bool{"imports":file.Imports()==nil,"moduleAugmentations":file.ModuleAugmentations==nil,"ambientModuleNames":file.AmbientModuleNames==nil,"commentDirectives":file.CommentDirectives==nil,"reparsedClones":file.ReparsedClones==nil,"pragmas":file.Pragmas==nil,"referencedFiles":file.ReferencedFiles==nil,"typeReferenceDirectives":file.TypeReferenceDirectives==nil,"libReferenceDirectives":file.LibReferenceDirectives==nil,"parseDiagnostics":file.Diagnostics()==nil,"jsDiagnostics":file.JSDiagnostics()==nil,"jsDocDiagnostics":file.JSDocDiagnostics()==nil}
}
func runCompletion(r CompletionRequest) CompletionResult {
 cold:=coldRequest(r.Parse)
 // Baseline getters visit a distinct parse. Actual source reaches BindSourceFile untouched.
 before:=runParser(cold,parseFile(cold))
 file:=parseFile(cold)
 order:=[]string{"independent-unbound-parse-snapshot","fresh-primary-parse"}
 if r.WarmBeforeBind {
  warm:=r.Parse
  // Only this explicit interaction control intentionally warms the primary AST.
  runParser(warm,file)
  order=append(order,"explicit-prebind-parser-observations")
 }
 binder.BindSourceFile(file)
 order=append(order,"bind","readonly-cold-graph-snapshot")
 after,binding,firstGraph:=bindSnapshot(cold,file)
 firstNulls:=metadataNulls(file)
 first,err:=json.Marshal(struct{After Response;Binding Binding;MetadataNulls map[string]bool;BindDiagnosticsNull bool;BindDiagnosticStates []DiagnosticState}{after,binding,firstNulls,file.BindDiagnostics()==nil,diagnosticStates(file.BindDiagnostics(),file)});if err!=nil{panic(err)}
 binder.BindSourceFile(file)
 secondAfter,secondBinding,secondGraph:=bindSnapshot(cold,file)
 assertSameIDs(firstGraph.syntax.ids,secondGraph.syntax.ids)
 assertSameIDs(firstGraph.symbolIDs,secondGraph.symbolIDs)
 assertSameIDs(firstGraph.tableIDs,secondGraph.tableIDs)
 assertSameIDs(firstGraph.flowIDs,secondGraph.flowIDs)
 assertSameIDs(firstGraph.listIDs,secondGraph.listIDs)
 second,err:=json.Marshal(struct{After Response;Binding Binding;MetadataNulls map[string]bool;BindDiagnosticsNull bool;BindDiagnosticStates []DiagnosticState}{secondAfter,secondBinding,metadataNulls(file),file.BindDiagnostics()==nil,diagnosticStates(file.BindDiagnostics(),file)});if err!=nil{panic(err)}
 if string(first)!=string(second){panic("Cold repeated bind changed published graph")}
 order=append(order,"repeat-bind-identities-and-values-before-helper-observations")
 result:=CompletionResult{Graph:Result{before,after,binding,true},MetadataNulls:firstNulls,ObservationOrder:order,
  BindDiagnosticsNull:file.BindDiagnostics()==nil,BindDiagnosticStates:diagnosticStates(file.BindDiagnostics(),file),SymbolHelpers:[]SymbolAPI{},ContainerHelpers:[]ContainerAPI{}}
 ordered:=make([]*ast.Symbol,len(firstGraph.symbolIDs))
 for symbol,id:=range firstGraph.symbolIDs{ordered[id]=symbol}
 for id,symbol:=range ordered {
  if symbol.CheckFlags!=0{panic("Binder produced checker-owned CheckFlags")}
  result.SymbolHelpers=append(result.SymbolHelpers,SymbolAPI{id,ast.GetSourceFileOfSymbol(symbol)==file,symbol.IsExternalModule(),symbol.IsStatic(),uint32(symbol.CombinedLocalAndExportSymbolFlags()),firstGraph.name(ast.SymbolName(symbol),symbol),uint32(symbol.CheckFlags)})
 }
 for id,node:=range firstGraph.syntax.raw {result.ContainerHelpers=append(result.ContainerHelpers,ContainerAPI{id,uint32(binder.GetContainerFlags(node))})}
 result.ObservationOrder=append(result.ObservationOrder,"published-symbol-ownership-name-flags-and-container-helpers")
 if r.ResolveJSDocAfterBind {
  observe:=cold;observe.ResolveJSDoc=true
  syntax:=runParser(observe,file)
  result.ObservedSyntax=&syntax
  result.ObservationOrder=append(result.ObservationOrder,"explicit-lazy-jsdoc-syntax-observation-after-binding")
 }
 return result
}
func main(){
 if len(os.Args)==2 && os.Args[1]=="--support-fixtures"{if err:=json.NewEncoder(os.Stdout).Encode(supportObservations());err!=nil{panic(err)};return}
 if len(os.Args)==2 && os.Args[1]=="--helper-fixtures"{if err:=json.NewEncoder(os.Stdout).Encode(completionHelperObservations());err!=nil{panic(err)};return}
 if len(os.Args)!=1{panic("Expected no arguments or --support-fixtures")}
 in:=bufio.NewScanner(os.Stdin);in.Buffer(make([]byte,65536),32*1024*1024);out:=json.NewEncoder(os.Stdout)
 for in.Scan(){var r CompletionRequest;if err:=json.Unmarshal(in.Bytes(),&r);err!=nil{panic(err)};if err:=out.Encode(runCompletion(r));err!=nil{panic(err)}}
 if err:=in.Err();err!=nil{panic(fmt.Errorf("read completion requests: %w",err))}
}
