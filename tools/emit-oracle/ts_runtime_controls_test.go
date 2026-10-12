// Authored complete TS portion output/diagnostics with genuine bundled Program/resolver.
package compiler_test
import (
 "context"
 "encoding/json"
 "fmt"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/bundled"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/collections"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/printer"
 "github.com/microsoft/TypeScript/tsc/internal/transformers"
 "github.com/microsoft/TypeScript/tsc/internal/transformers/tstransforms"
 "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
 "github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)
func runtimeTransform(options *transformers.TransformOptions, f *ast.SourceFile) *ast.SourceFile {
 // compiler/emitter.go139–163 exact TS stage sequence; ES/module families follow separately.
 if options.CompilerOptions.EmitDecoratorMetadata.IsTrue() {f=tstransforms.NewMetadataTransformer(options).TransformSourceFile(f)}
 f=tstransforms.NewTypeEraserTransformer(options).TransformSourceFile(f)
 if !options.CompilerOptions.VerbatimModuleSyntax.IsTrue()&&!ast.IsInJSFile(f.AsNode()) {f=tstransforms.NewImportElisionTransformer(options).TransformSourceFile(f)}
 f=tstransforms.NewRuntimeSyntaxTransformer(options).TransformSourceFile(f)
 if options.CompilerOptions.ExperimentalDecorators.IsTrue() {f=tstransforms.NewLegacyDecoratorsTransformer(options).TransformSourceFile(f)}
 return f
}
func TestAuthoredTsRuntimeOutputs(t *testing.T) {
 cases:=[]struct{name,input string;preserve bool}{
{"NumericStringEnum","enum E { A, B = 2, C = \"text\", D = B + 2 }",false},
{"NonFiniteEnum","enum E { A = -1, B = Infinity, C = NaN, D = 1 / 0, E = 0 / 0 }",false},
{"ConstEnumErased","const enum E { A = 1 } const value = E.A;",false},
{"ConstEnumPreserved","const enum E { A = 1 } const value = E.A;",true},
{"NamespaceValues","namespace N { export const x = 1; export function f() { return x; } } N.f();",false},
{"DottedNamespace","namespace A.B.C { export const x = 1; export function f() { return x; } } A.B.C.f();",false},
{"ExportDestructuring","namespace N { export const [x, y] = [1, 2]; export const { z } = { z: 3 }; export const sum = x + y + z; }",false},
{"ExportClass","namespace N { export class C { value = 1; } export function f() { return new C(); } } N.f();",false},
{"MergedScopes","enum E { A } enum E { B = 2 } namespace E { export const value = A; } namespace N { export const a = 1; } namespace N { export const b = a + 1; }",false},
{"ImportEqualsValue","namespace A { export const n = 1; } import B = A; B.n;",false},
{"ParameterProperties","class C { constructor(public x: number = 1, private readonly y = 2) {} }",false},
{"DerivedParameterProperties","class B {} class C extends B { constructor(public value: number) { super(); console.log(value); } }",false},
{"TrySuperParameterProperties","class B {} class C extends B { constructor(public value: number) { try { super(); } catch (e) { throw e; } console.log(value); } }",false},
{"BorrowedUnicodeParameter","class C { constructor(public π: number) { \"use strict\"; this.π++; } }",false},
{"NamespaceShorthand","namespace N { export const x = 1; export function object() { return { x }; } }",false},
{"NamespaceComments","/** outer */ namespace A.B { /** member */ export const x = 1; } // trailing",false},
 }
 for _,tc:=range cases {t.Run(tc.name,func(t *testing.T){
  fs:=bundled.WrapFS(vfstest.FromMap(map[string]string{"/main.ts":tc.input},tspath.CaseSensitive))
  options:=&core.CompilerOptions{Module:core.ModuleKindESNext,Target:core.ScriptTargetESNext,SkipLibCheck:core.TSTrue,StrictNullChecks:core.TSTrue}
  if tc.preserve {options.PreserveConstEnums=core.TSTrue}
  p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{"/main.ts"},nil,"/",fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
  f:=p.GetSourceFile("/main.ts");c,done:=p.GetTypeCheckerForFile(context.Background(),f);defer done()
  codes:=[]int{};for _,d:=range c.GetDiagnostics(context.Background(),f) {codes=append(codes,int(d.Code()))}
  fullDiagnostics:=tsPublicDiagnostics(p)
  ec:=printer.NewEmitContext();r:=c.NewEmitResolver(ec);opt:=&transformers.TransformOptions{CompilerOptions:options,Context:ec,Resolver:r,EmitResolver:r}
  transformed:=runtimeTransform(opt,f);output:=printer.NewPrinter(printer.PrinterOptions{NewLine:core.NewLineKindLF},printer.PrintHandlers{},ec).EmitSourceFile(transformed)
  encoded,err:=json.Marshal(struct{Name string `json:"name"`;Output string `json:"output"`;Codes []int `json:"diagnosticCodes"`;FullDiagnostics tsDiagnosticChannels `json:"fullDiagnostics"`}{tc.name,output,codes,fullDiagnostics});if err!=nil {t.Fatal(err)}
  fmt.Printf("RUNTIME_OUTPUT=%s\n",encoded)
 })}
}

func TestAuthoredMultiMapNegativeHint(t *testing.T) {
 m:=collections.NewMultiMapWithSizeHint[string,int](-1)
 if m.Len()!=0 {t.Fatal("negative hint map must start empty")}
 m.Add("key",7)
 if m.Len()!=1||len(m.Get("key"))!=1||m.Get("key")[0]!=7 {t.Fatal("negative hint map must remain usable")}
 t.Log("negative hint accepted; empty then Add/Get7")
}

func TestAuthoredTsLegacyOutputs(t *testing.T) {
 cases:=[]struct{name,input string;target core.ScriptTarget}{
{"LegacyStaticAlias","declare function dec(...args: any[]): any; @dec class C { static value = C; static get() { return C.value; } }",core.ScriptTargetESNext},
{"LegacyOlderAlias","declare function dec(...args: any[]): any; @dec class C { static get() { return C; } }",core.ScriptTargetES2015},
{"LegacyNestedAliases","declare function dec(...args: any[]): any; @dec class A { static get() { @dec class B { static value = B; static outer = A; } return B; } }",core.ScriptTargetESNext},
{"LegacyConstructorParameters","declare function dec(...args: any[]): any; class C { constructor(@dec value: number) {} }",core.ScriptTargetESNext},
{"LegacyAccessors","declare function dec(...args: any[]): any; class C { @dec get value() { return 1; } set value(@dec next: number) {} }",core.ScriptTargetESNext},
{"LegacyComputedName","declare function dec(...args: any[]): any; const key = () => \"field\"; class C { @dec [key()] = 1; }",core.ScriptTargetESNext},
{"LegacyPrivateExpression","class Outer { static #dec(...args: any[]): any {} method() { class C { @(Outer.#dec) method() {} } return C; } }",core.ScriptTargetESNext},
{"LegacyDefaultExport","declare function dec(...args: any[]): any; @dec export default class { @dec method() {} }",core.ScriptTargetESNext},
{"LegacyBorrowedAlias","declare function dec(...args: any[]): any; @dec class Ω { static value = Ω; }",core.ScriptTargetESNext},
 }
 for _,tc:=range cases {t.Run(tc.name,func(t *testing.T){
  fs:=bundled.WrapFS(vfstest.FromMap(map[string]string{"/main.ts":tc.input},tspath.CaseSensitive))
  options:=&core.CompilerOptions{Module:core.ModuleKindESNext,Target:tc.target,SkipLibCheck:core.TSTrue,StrictNullChecks:core.TSTrue,ExperimentalDecorators:core.TSTrue}
  p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{"/main.ts"},nil,"/",fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
  f:=p.GetSourceFile("/main.ts");c,done:=p.GetTypeCheckerForFile(context.Background(),f);defer done()
  codes:=[]int{};for _,d:=range c.GetDiagnostics(context.Background(),f) {codes=append(codes,int(d.Code()))}
  fullDiagnostics:=tsPublicDiagnostics(p)
  ec:=printer.NewEmitContext();r:=c.NewEmitResolver(ec);opt:=&transformers.TransformOptions{CompilerOptions:options,Context:ec,Resolver:r,EmitResolver:r}
  transformed:=runtimeTransform(opt,f);output:=printer.NewPrinter(printer.PrinterOptions{NewLine:core.NewLineKindLF},printer.PrintHandlers{},ec).EmitSourceFile(transformed)
  encoded,err:=json.Marshal(struct{Name string `json:"name"`;Output string `json:"output"`;Codes []int `json:"diagnosticCodes"`;FullDiagnostics tsDiagnosticChannels `json:"fullDiagnostics"`}{tc.name,output,codes,fullDiagnostics});if err!=nil {t.Fatal(err)}
  fmt.Printf("RUNTIME_OUTPUT=%s\n",encoded)
 })}
}
