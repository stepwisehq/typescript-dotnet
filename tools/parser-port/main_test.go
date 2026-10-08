package main

import (
 "go/ast"
 "go/parser"
 "go/token"
 "strings"
 "testing"
)
func translateFixture(t *testing.T, text string) string {
 t.Helper(); sourceSet=token.NewFileSet(); output.Reset(); indent=0
 f,err:=parser.ParseFile(sourceSet,"fixture.go","package parser\n"+text,0);if err!=nil{t.Fatal(err)}
 methods=map[string]*ast.FuncDecl{}
 for _,d:=range f.Decls{if fn,ok:=d.(*ast.FuncDecl);ok{methods[fn.Name.Name]=fn}}
 for _,d:=range f.Decls{if fn,ok:=d.(*ast.FuncDecl);ok{function(fn)}}
 return output.String()
}
func TestRangeBodyAndScopedVariables(t *testing.T) {
 got:=translateFixture(t,`func Sum(xs []int) int { sum:=0; for i,v:=range xs { if v<0 {continue}; sum+=i+v }; return sum }`)
 for _,part:=range []string{"GoRuntime.Range(xs)",".Key;",".Value;","continue;","sum += (i + v);"}{if !strings.Contains(got,part){t.Errorf("missing %s in %s",part,got)}}
}
func TestFallthroughAndReceiverClosure(t *testing.T) {
 got:=translateFixture(t,`type Parser struct{}; func (p *Parser) Choice(x int) int { f:=func(p *Parser) int {return p.Choice(1)}; switch x {case 0:x++;fallthrough;case 1:return f(p);default:return 2} }`)
 if !strings.Contains(got,"goto case 1;"){t.Fatal(got)}
 if !strings.Contains(got,"Func<Parser, int> f = (p)"){t.Fatal(got)}
 if !strings.Contains(got,"return p.Choice(1);"){t.Fatal(got)}
}
func TestUnsupportedStatementFails(t *testing.T) {
 defer func(){if recover()==nil{t.Fatal("unsupported goroutine was silently accepted")}}()
 translateFixture(t,`func Bad(){go Bad()}`)
}

func TestPartiallyNewShortDeclaration(t *testing.T) {
 got:=translateFixture(t,`func Pair()(int,int){return 1,2};func Partial()int{a:=0;a,b:=Pair();return a+b}`)
 if strings.Contains(got,"var (a, b)"){t.Fatal(got)}
 if !strings.Contains(got,"var b = tuple"){t.Fatal(got)}
}

func TestSwitchTerminalPaths(t *testing.T) {
 got:=translateFixture(t,`func Paths(x int) int {switch x {case 0:panic("x");default:switch x {case 1:return 1;default:return 2}}}`)
 if strings.Contains(got,"break;"){t.Fatal("terminal path emitted redundant break: "+got)}
}
func TestModifierListAndBuilderTypes(t *testing.T) {
 got:=translateFixture(t,`func Types(m *ast.ModifierList) *ast.ModifierList {var b strings.Builder;b.WriteString("a");return m}`)
 if !strings.Contains(got,"ModifierList Types(ModifierList m)")||!strings.Contains(got,"GoBuilder b = new GoBuilder();"){t.Fatal(got)}
}
func TestMakeSliceRetainsLength(t *testing.T) {
 got:=translateFixture(t,`func Slice() []*ast.Node {xs:=make([]*ast.Node,1,8);xs[0]=nil;return xs}`)
 if !strings.Contains(got,"GoRuntime.MakeSlice<Node>(1)"){t.Fatal(got)}
}
