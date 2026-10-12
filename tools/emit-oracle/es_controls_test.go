// Authored full TS→ES target-chain outputs and complete public diagnostics.
// Original compiler-corpus/module/Program emission acceptance is separate.
package compiler_test

import (
 "context"
 "encoding/json"
 "os"
 "strings"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/bundled"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/printer"
 "github.com/microsoft/TypeScript/tsc/internal/transformers"
 "github.com/microsoft/TypeScript/tsc/internal/transformers/estransforms"
 "github.com/microsoft/TypeScript/tsc/internal/transformers/tstransforms"
 "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
 "github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)
type esObservedCase struct {
 Name,Input,Other,Target,Module string
 JSX bool
 UseDefineForClassFields,ExperimentalDecorators,EmitDecoratorMetadata *bool
 StrictNullChecks *bool
 Output *string
 FullDiagnostics *tsDiagnosticChannels
}
func esTri(v *bool) core.Tristate {if v==nil{return core.TSUnknown};if *v{return core.TSTrue};return core.TSFalse}
func esTarget(name string) core.ScriptTarget {
 switch name {
 case "ES2015":return core.ScriptTargetES2015
 case "ES2016":return core.ScriptTargetES2016
 case "ES2017":return core.ScriptTargetES2017
 case "ES2018":return core.ScriptTargetES2018
 case "ES2019":return core.ScriptTargetES2019
 case "ES2020":return core.ScriptTargetES2020
 case "ES2021":return core.ScriptTargetES2021
 case "ES2022":return core.ScriptTargetES2022
 case "ESNext":return core.ScriptTargetESNext
 default:panic("unknown target: "+name)
 }
}
func TestAuthoredESFullTargetChains(t *testing.T) {
 capture:=os.Getenv("EMIT_ES_CAPTURE")!=""
 for _,path:=range strings.Split(os.Getenv("EMIT_ES_CASES"),",") {
  raw,err:=os.ReadFile(path);if err!=nil {t.Fatal(err)}
  var cases []esObservedCase;if err=json.Unmarshal(raw,&cases);err!=nil {t.Fatal(err)}
  for i:=range cases {tc:=&cases[i];t.Run(tc.Name,func(t *testing.T){
   module:=core.ModuleKindESNext
   switch tc.Module {case "","ESNext":case "CommonJS":module=core.ModuleKindCommonJS;case "Preserve":module=core.ModuleKindPreserve;default:t.Fatalf("unknown module: %s",tc.Module)}
   strict:=core.TSTrue;if tc.StrictNullChecks!=nil {strict=esTri(tc.StrictNullChecks)}
   options:=&core.CompilerOptions{Target:esTarget(tc.Target),Module:module,SkipLibCheck:core.TSTrue,StrictNullChecks:strict,UseDefineForClassFields:esTri(tc.UseDefineForClassFields),ExperimentalDecorators:esTri(tc.ExperimentalDecorators),EmitDecoratorMetadata:esTri(tc.EmitDecoratorMetadata)}
   name:=tspath.RootedFilePath("/main.ts");if tc.JSX{name="/main.tsx"}
   files:=map[string]string{name.AsString():tc.Input};if tc.Other!=""{files["/node_modules/other/index.ts"]=tc.Other}
   fs:=bundled.WrapFS(vfstest.FromMap(files,tspath.CaseSensitive))
   p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{name},nil,"/",fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
   original:=p.GetSourceFile(name);c,done:=p.GetTypeCheckerForFile(context.Background(),original);defer done()
   fullDiagnostics:=tsPublicDiagnostics(p)
   ec:=printer.NewEmitContext();r:=c.NewEmitResolver(ec)
   opts:=&transformers.TransformOptions{Context:ec,CompilerOptions:options,EmitResolver:r,Resolver:r,GetEmitModuleFormatOfFile:p.GetEmitModuleFormatOfFile}
   file:=original
   if options.EmitDecoratorMetadata.IsTrue(){file=tstransforms.NewMetadataTransformer(opts).TransformSourceFile(file)}
   file=tstransforms.NewTypeEraserTransformer(opts).TransformSourceFile(file)
   if !options.VerbatimModuleSyntax.IsTrue()&&!ast.IsInJSFile(original.AsNode()){file=tstransforms.NewImportElisionTransformer(opts).TransformSourceFile(file)}
   file=tstransforms.NewRuntimeSyntaxTransformer(opts).TransformSourceFile(file)
   if options.ExperimentalDecorators.IsTrue(){file=tstransforms.NewLegacyDecoratorsTransformer(opts).TransformSourceFile(file)}
   if tx:=estransforms.GetESTransformer(opts);tx!=nil {file=tx.TransformSourceFile(file)}
   file=estransforms.NewUseStrictTransformer(opts).TransformSourceFile(file)
   output:=printer.NewPrinter(printer.PrinterOptions{NewLine:core.NewLineKindLF,Target:options.Target},printer.PrintHandlers{},ec).EmitSourceFile(file)
   if capture {tc.Output=&output;tc.FullDiagnostics=&fullDiagnostics} else {
    if tc.Output==nil||*tc.Output!=output{t.Fatalf("complete TS→ES output differs\n%s",output)}
    a,_:=json.Marshal(tc.FullDiagnostics);b,_:=json.Marshal(fullDiagnostics);if string(a)!=string(b){t.Fatalf("full public diagnostics differ: %s != %s",a,b)}
   }
   t.Logf("target=%s outputBytes=%d semanticDiagnostics=%d",tc.Target,len(output),len(fullDiagnostics.Semantic))
  })}
  if capture {raw,err=json.MarshalIndent(cases,"","  ");if err!=nil{t.Fatal(err)};if err=os.WriteFile(path,append(raw,'\n'),0644);err!=nil{t.Fatal(err)}}
 }
}
