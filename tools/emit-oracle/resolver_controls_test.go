// Authored direct pinned Go semantic emit observations.
package compiler_test
import (
 "context"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/binder"
 "github.com/microsoft/TypeScript/tsc/internal/bundled"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/checker"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/nodebuilder"
 "github.com/microsoft/TypeScript/tsc/internal/printer"
 "github.com/microsoft/TypeScript/tsc/internal/testutil/parsetestutil"
 "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
 "github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)
func declarations(file *ast.SourceFile)(map[string]*ast.Node,*ast.Node) {
 named:=map[string]*ast.Node{};var reference *ast.Node;var visit func(*ast.Node)bool
 visit=func(n *ast.Node)bool {if n.Kind==ast.KindFunctionDeclaration||n.Kind==ast.KindVariableDeclaration {named[n.Name().Text()]=n};if n.Kind==ast.KindExpressionStatement {reference=n.Expression()};n.ForEachChild(visit);return false};file.AsNode().ForEachChild(visit);return named,reference
}
func TestAuthoredEmitReferenceFallback(t *testing.T) {
 for _,tc:=range []struct{text string; found bool}{{"let value = 1; value;",false},{"export {}; let value = 1; value;",true}} {
  f:=parsetestutil.ParseTypeScript(tc.text,false);binder.BindSourceFile(f);named,reference:=declarations(f)
  r:=binder.NewReferenceResolver(&core.CompilerOptions{},binder.ReferenceResolverHooks{});got:=r.GetReferencedValueDeclaration(reference)
  if tc.found {if got!=named["value"] {t.Fatal("module declaration")};ds:=r.GetReferencedValueDeclarations(reference);if len(ds)!=1||ds[0]!=got {t.Fatal("declaration order")}} else if got!=nil {t.Fatal("global source resolves without globals")}
  t.Logf("global=%v valueDeclarationFound=%v",!tc.found,got!=nil)
 }
}
func TestAuthoredEmitProgramProjection(t *testing.T) {
 fs:=bundled.WrapFS(vfstest.FromMap(map[string]string{"/main.ts":"export function choose<T extends string>(value: T): T { return value; } export const narrow = \"ok\"; export const negative = -4; export const huge = 1e999; export const truth = true; export const computed = { [\"x\"]: 1 }; export declare const uniqueKey: unique symbol; export declare const synthesized: { [K in typeof uniqueKey]: number }; export const specialized = choose<\"fixed\">; export declare const quoted: \"quoted\"; export const accessors = { /** read */ get value(): string { return \"\"; }, /** write */ set value(v: number) {} }; export const autoClass = class { /** auto */ accessor auto = 1; };"},tspath.CaseSensitive))
 p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(&core.CompilerOptions{SkipLibCheck:core.TSTrue},[]tspath.RootedFilePath{"/main.ts"},nil,"/",fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
 f:=p.GetSourceFile("/main.ts");c,done:=p.GetTypeCheckerForFile(context.Background(),f);defer done();if ds:=c.GetDiagnostics(context.Background(),f);len(ds)!=0 {t.Fatalf("fixture diagnostics %d",len(ds))}
 named,_:=declarations(f);ec:=printer.NewEmitContext();r:=c.NewEmitResolver(ec);fn:=named["choose"];flags:=nodebuilder.FlagsNoTruncation
 ps:=r.CreateTypeParametersOfSignatureDeclaration(fn,fn,flags,nodebuilder.InternalFlagsNone,nil);if len(ps)!=1||ps[0].Name().Text()!="T"||ps[0].AsTypeParameterDeclaration().Constraint.Kind!=ast.KindStringKeyword {t.Fatal("type parameter")}
 ret:=r.CreateReturnTypeOfSignatureDeclaration(fn,fn,flags,nodebuilder.InternalFlagsNone,nil);if ret.Kind!=ast.KindTypeReference||ret.AsTypeReferenceNode().TypeName.Text()!="T" {t.Fatal("return type")}
 expr:=r.CreateTypeOfExpression(named["narrow"].Initializer(),f.AsNode(),flags,nodebuilder.InternalFlagsNone,nil);if expr.Kind!=ast.KindLiteralType||expr.AsLiteralTypeNode().Literal.Kind!=ast.KindStringLiteral||expr.AsLiteralTypeNode().Literal.Text()!="ok" {t.Fatalf("expression kind=%v",expr.Kind)}
 str:=r.CreateLiteralConstValue(named["narrow"],nil);if str.Kind!=ast.KindStringLiteral||str.Text()!="ok" {t.Fatal("string")}
 neg:=r.CreateLiteralConstValue(named["negative"],nil);if neg.Kind!=ast.KindPrefixUnaryExpression||neg.AsPrefixUnaryExpression().Operator!=ast.KindMinusToken||neg.AsPrefixUnaryExpression().Operand.Text()!="4" {t.Fatal("negative")}
 inf:=r.CreateLiteralConstValue(named["huge"],nil);if inf.Kind!=ast.KindIdentifier||inf.Text()!="Infinity" {t.Fatal("infinite")}
 if r.CreateLiteralConstValue(named["truth"],nil).Kind!=ast.KindTrueKeyword {t.Fatal("boolean")};r.PrecalculateDeclarationEmitVisibility(f);if !r.IsDeclarationVisible(fn) {t.Fatal("visibility")}
 b:=checker.NewNodeBuilder(c,printer.NewEmitContext())
 cp:=c.GetPropertiesOfType(c.GetTypeOfSymbol(named["computed"].Symbol()))[0]
 cn:=b.SymbolToNode(cp,ast.SymbolFlagsValue,named["computed"],flags,nodebuilder.InternalFlagsWriteComputedProps,nil)
 if cn!=cp.ValueDeclaration.Name()||cn.Kind!=ast.KindComputedPropertyName {t.Fatal("computed declaration name")}
 sp:=c.GetPropertiesOfType(c.GetTypeOfSymbol(named["synthesized"].Symbol()))[0]
 sn:=b.SymbolToNode(sp,ast.SymbolFlagsValue,named["synthesized"],flags,nodebuilder.InternalFlagsWriteComputedProps,nil)
 if sn.Kind!=ast.KindComputedPropertyName||sn.Expression().Text()!="uniqueKey" {t.Fatal("mapped computed symbol name")}
 sig:=c.GetSignaturesOfType(c.GetTypeOfSymbol(named["specialized"].Symbol()),checker.SignatureKindCall)[0]
 sigNode:=b.SignatureToSignatureDeclaration(sig,ast.KindCallSignature,named["specialized"],flags,nodebuilder.InternalFlagsNone,nil)
 if sigNode.Type().Kind!=ast.KindLiteralType||sigNode.Type().AsLiteralTypeNode().Literal.Text()!="fixed"||sigNode.Parameters()[0].Type().AsLiteralTypeNode().Literal.Text()!="fixed" {t.Fatal("mapped signature types")}
 quoted:=r.CreateTypeOfDeclaration(named["quoted"],named["quoted"],flags|nodebuilder.FlagsUseSingleQuotesForStringLiteralType,nodebuilder.InternalFlagsNone,nil)
 if quoted.Kind!=ast.KindLiteralType||quoted.AsLiteralTypeNode().Literal.AsStringLiteral().TokenFlags&ast.TokenFlagsSingleQuote==0 {t.Fatal("quoted annotation")}
 accessorType:=r.CreateTypeOfDeclaration(named["accessors"],named["accessors"],flags,nodebuilder.InternalFlagsNone,nil)
 ap:=c.GetPropertiesOfType(c.GetTypeOfSymbol(named["accessors"].Symbol()))[0]
 getter:=ast.GetDeclarationOfKind(ap,ast.KindGetAccessor);setter:=ast.GetDeclarationOfKind(ap,ast.KindSetAccessor)
 members:=accessorType.AsTypeLiteralNode().Members.Nodes
 if len(members)!=2||members[0].Kind!=ast.KindGetAccessor||members[1].Kind!=ast.KindSetAccessor||ec.CommentRange(members[0])!=getter.Loc||ec.CommentRange(members[1])!=setter.Loc {t.Fatal("accessor comment ranges")}
 autoType:=r.CreateTypeOfDeclaration(named["autoClass"],named["autoClass"],flags|nodebuilder.FlagsWriteClassExpressionAsTypeLiteral,nodebuilder.InternalFlagsNone,nil)
 autoMembers:=autoType.AsTypeLiteralNode().Members.Nodes[0].Type().AsTypeLiteralNode().Members.Nodes
 autoDeclaration:=named["autoClass"].Initializer().AsClassExpression().Members.Nodes[0]
 if len(autoMembers)!=2||autoMembers[0].Kind!=ast.KindGetAccessor||autoMembers[1].Kind!=ast.KindSetAccessor||ec.CommentRange(autoMembers[0])!=autoDeclaration.Loc||ec.CommentRange(autoMembers[1])!=autoMembers[1].Loc {t.Fatal("auto-accessor exact asymmetric comment ranges")}
 t.Log("typeParameter=T:string return=T expression=literal:ok const=ok negative=-4 infinity=Infinity truth=true visible=true computed=retained mappedComputed=uniqueKey mappedSignature=fixed->fixed quote=single accessors=sourceRanges autoAccessor=getterSource/setterSynthetic")
}
