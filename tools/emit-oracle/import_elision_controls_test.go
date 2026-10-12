// Authored genuine Program host adaptation of original20 ImportElision assertions.
package compiler_test
import (
 "context"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/bundled"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/printer"
 "github.com/microsoft/TypeScript/tsc/internal/testutil/emittestutil"
 "github.com/microsoft/TypeScript/tsc/internal/testutil/parsetestutil"
 "github.com/microsoft/TypeScript/tsc/internal/transformers"
 "github.com/microsoft/TypeScript/tsc/internal/transformers/tstransforms"
 "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
 "github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)
func TestOriginalImportElisionGenuineProgramAdapter(t *testing.T) {
	data := []struct {
		title  string
		input  string
		output string
		other  string
		jsx    bool
	}{
		{title: "ImportEquals#1", input: "import x = require(\"other\"); x;", output: "import x = require(\"other\");\nx;"},
		{title: "ImportEquals#2", input: "import x = require(\"other\");", output: ""},
		{title: "ImportDeclaration#1", input: `import "m";`, output: `import "m";`},
		{title: "ImportDeclaration#2", input: "import * as x from \"other\"; x;", output: "import * as x from \"other\";\nx;"},
		{title: "ImportDeclaration#3", input: "import x from \"other\"; x;", output: "import x from \"other\";\nx;"},
		{title: "ImportDeclaration#4", input: "import { x } from \"other\"; x;", output: "import { x } from \"other\";\nx;"},
		{title: "ImportDeclaration#5", input: "import * as x from \"other\";", output: ""},
		{title: "ImportDeclaration#6", input: "import x from \"other\";", output: ""},
		{title: "ImportDeclaration#7", input: "import { x } from \"other\";", output: ""},
		{title: "ExportDeclaration#1", input: "export * from \"other\";", other: "export let x;", output: "export * from \"other\";"},
		{title: "ExportDeclaration#2", input: "export * as x from \"other\";", other: "export let x;", output: "export * as x from \"other\";"},
		{title: "ExportDeclaration#3", input: "export * from \"other\";", other: "export let x;", output: "export * from \"other\";"},
		{title: "ExportDeclaration#4", input: "export * as x from \"other\";", other: "export let x;", output: "export * as x from \"other\";"},
		{title: "ExportDeclaration#5", input: "export { x } from \"other\";", other: "export let x;", output: "export { x } from \"other\";"},
		{title: "ExportDeclaration#6", input: "export { x } from \"other\";", other: "export type x = any;", output: ""},
		{title: "ExportDeclaration#7", input: "export { x }; let x;", output: "export { x };\nlet x;"},
		{title: "ExportDeclaration#8", input: "export { x }; type x = any;", output: ""},
		{title: "ExportDeclaration#9", input: "import { x } from \"other\"; export { x };", other: "export type x = any;", output: ""},
		{title: "ExportAssignment#1", input: "let x; export default x;", output: "let x;\nexport default x;"},
		{title: "ExportAssignment#2", input: "type x = any; export default x;", output: ""},
	}

 for _,rec:=range data {t.Run(rec.title,func(t *testing.T) {
  fileName:=tspath.RootedFilePath("/main.ts");if rec.jsx {fileName="/main.tsx"}
  files:=map[string]string{fileName.AsString():rec.input};if len(rec.other)>0 {files["/node_modules/other/index.ts"]=rec.other}
  fs:=bundled.WrapFS(vfstest.FromMap(files,tspath.CaseSensitive))
  options:=&core.CompilerOptions{Module:core.ModuleKindESNext,NoLib:core.TSTrue}
  p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{fileName},nil,"/",fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
  f:=p.GetSourceFile(fileName);parsetestutil.CheckDiagnostics(t,f)
  if len(rec.other)>0 {other:=p.GetSourceFile("/node_modules/other/index.ts");if other==nil {t.Fatal("original other module not loaded")};parsetestutil.CheckDiagnostics(t,other)}
  c,done:=p.GetTypeCheckerForFile(context.Background(),f);defer done()
  ec:=printer.NewEmitContext();r:=c.NewEmitResolver(ec)
  opt:=&transformers.TransformOptions{CompilerOptions:options,Context:ec,EmitResolver:r,Resolver:r}
  f=tstransforms.NewTypeEraserTransformer(opt).TransformSourceFile(f)
  f=tstransforms.NewImportElisionTransformer(opt).TransformSourceFile(f)
  emittestutil.CheckEmit(t,nil,f,rec.output)
 })}
}
