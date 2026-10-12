// Authored full TypeScript-stage pipeline comparisons, genuine Program/bundled libs.
// This is not original upstream test assertions or whole compiler emission.
package compiler_test
import (
 "context"
 "encoding/json"
 "os"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/bundled"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/printer"
 "github.com/microsoft/TypeScript/tsc/internal/transformers"
 "github.com/microsoft/TypeScript/tsc/internal/transformers/tstransforms"
 "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
 "github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)
type metadataCase struct {
 Name,Input,Other,Target string
 StrictNullChecks,Legacy bool
 Expected *string
 DiagnosticCodes []int32
 FullDiagnostics *tsDiagnosticChannels
}
func TestAuthoredTSMetadataPipeline(t *testing.T) {
 path:=os.Getenv("EMIT_METADATA_CASES");raw,err:=os.ReadFile(path);if err!=nil {t.Fatal(err)}
 var cases []metadataCase;if err=json.Unmarshal(raw,&cases);err!=nil {t.Fatal(err)}
 capture:=os.Getenv("EMIT_METADATA_CAPTURE")!=""
 captureDiagnostics:=os.Getenv("EMIT_METADATA_CAPTURE_DIAGNOSTICS")!=""
 for i:=range cases {tc:=&cases[i];t.Run(tc.Name,func(t *testing.T){
  target:=core.ScriptTargetES2020;if tc.Target=="ES2019" {target=core.ScriptTargetES2019}
  strict:=core.TSFalse;if tc.StrictNullChecks {strict=core.TSTrue};legacy:=core.TSFalse;if tc.Legacy {legacy=core.TSTrue}
  options:=&core.CompilerOptions{Module:core.ModuleKindESNext,Target:target,SkipLibCheck:core.TSTrue,StrictNullChecks:strict,ExperimentalDecorators:legacy,EmitDecoratorMetadata:core.TSTrue}
  files:=map[string]string{"/main.ts":tc.Input};if tc.Other!="" {files["/node_modules/other/index.ts"]=tc.Other}
  fs:=bundled.WrapFS(vfstest.FromMap(files,tspath.CaseSensitive))
  p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{"/main.ts"},nil,"/",fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
  file:=p.GetSourceFile("/main.ts");if len(file.Diagnostics())!=0 {t.Fatalf("fixture parse diagnostics: %d",len(file.Diagnostics()))};c,done:=p.GetTypeCheckerForFile(context.Background(),file);defer done()
  codes:=make([]int32,0);for _,d:=range c.GetDiagnostics(context.Background(),file) {codes=append(codes,d.Code())}
  fullDiagnostics:=tsPublicDiagnostics(p)
  ec:=printer.NewEmitContext();r:=c.NewEmitResolver(ec)
  opt:=&transformers.TransformOptions{Context:ec,CompilerOptions:options,EmitResolver:r,Resolver:r,GetEmitModuleFormatOfFile:p.GetEmitModuleFormatOfFile}
  file=tstransforms.NewMetadataTransformer(opt).TransformSourceFile(file)
  file=tstransforms.NewTypeEraserTransformer(opt).TransformSourceFile(file)
  file=tstransforms.NewImportElisionTransformer(opt).TransformSourceFile(file)
  file=tstransforms.NewRuntimeSyntaxTransformer(opt).TransformSourceFile(file)
  if tc.Legacy {file=tstransforms.NewLegacyDecoratorsTransformer(opt).TransformSourceFile(file)}
  text:=printer.NewPrinter(printer.PrinterOptions{NewLine:core.NewLineKindLF},printer.PrintHandlers{},ec).EmitSourceFile(file)
  if capture {tc.Expected=&text;tc.DiagnosticCodes=codes} else {if tc.Expected==nil||*tc.Expected!=text {t.Fatalf("complete TS pipeline output differs\n%s",text)};a,_:=json.Marshal(codes);b,_:=json.Marshal(tc.DiagnosticCodes);if string(a)!=string(b){t.Fatalf("diagnostic codes differ: %s != %s",a,b)}}
  if captureDiagnostics {tc.FullDiagnostics=&fullDiagnostics} else if !capture {a,_:=json.Marshal(fullDiagnostics);b,_:=json.Marshal(tc.FullDiagnostics);if string(a)!=string(b){t.Fatalf("full public diagnostics differ: %s != %s",a,b)}}
  if len(file.Diagnostics())!=0 {t.Fatalf("parse diagnostics after transform: %d",len(file.Diagnostics()))};t.Logf("diagnosticCodes=%v outputBytes=%d",codes,len(text))
 })}
 if capture||captureDiagnostics {raw,err=json.MarshalIndent(cases,"","  ");if err!=nil {t.Fatal(err)};if err=os.WriteFile(path,append(raw,'\n'),0644);err!=nil {t.Fatal(err)}}
}
