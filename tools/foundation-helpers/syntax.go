// Authored syntax helper observer; imports unchanged pinned upstream algorithms.
package main

import (
 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/parser"
)

type predicateRow struct{Name string;TrueKinds []int}
type precedenceRow struct{Kind int;Flags int;Precedence int}
type syntaxNodeRow struct{
 Kind int;Pos int;End int;Predicates []bool;Meaning int
 ExpressionPrecedence int;Leftmost []int;Ancestors []int;AncestorOrQuit int
 ReturnNodes []int;StoppedReturns []int;ReturnResult bool;StoppedReturnResult bool
 ModifierSame bool;ModifiedKinds []int
 Accessors []int
}
type syntaxSource struct{Text string;ScriptKind int;Nodes []syntaxNodeRow}
type syntaxOutput struct{KindCount int;Predicates []predicateRow;Operators []precedenceRow;Binary []int;Parsed []syntaxSource;PragmaArguments []string}
func syntaxRows()syntaxOutput{
 factory:=ast.NewNodeFactory(ast.NodeFactoryHooks{})
 output:=syntaxOutput{KindCount:int(ast.KindCount),PragmaArguments:[]string{ast.GetPragmaArgument(nil,"factory"),ast.GetPragmaArgument(&ast.Pragma{},"factory"),ast.GetPragmaArgument(&ast.Pragma{Args:map[string]ast.PragmaArgument{"factory":{Value:"react"}}},"factory")}}
 predicates:=[]struct{name string;fn func(*ast.Node)bool}{
  {"IsModifierLike",ast.IsModifierLike},{"IsObjectBindingOrAssignmentElement",ast.IsObjectBindingOrAssignmentElement},
  {"IsAccessor",ast.IsAccessor},{"IsMemberName",ast.IsMemberName},{"IsClassOrInterfaceLike",ast.IsClassOrInterfaceLike},
  {"IsTypeElement",ast.IsTypeElement},{"IsJsxChild",ast.IsJsxChild},{"IsJsxAttributeLike",ast.IsJsxAttributeLike},
  {"IsStatement",ast.IsStatement},{"IsStatementButNotDeclaration",ast.IsStatementButNotDeclaration},
  {"CanHaveSymbol",ast.CanHaveSymbol},{"CanHaveModifiers",ast.CanHaveModifiers},{"CanHaveIllegalModifiers",ast.CanHaveIllegalModifiers},
  {"HasImportAttributes",ast.HasImportAttributes},
  {"IsJsxCallLike",ast.IsJsxCallLike},{"IsParameterLike",ast.IsParameterLike},{"IsVariableLike",ast.IsVariableLike},
  {"IsObjectTypeDeclaration",ast.IsObjectTypeDeclaration},{"IsClassOrTypeElement",ast.IsClassOrTypeElement},
  {"IsTypeKeywordToken",ast.IsTypeKeywordToken},{"IsStringTextContainingNode",ast.IsStringTextContainingNode},
  {"IsTemplateLiteralToken",ast.IsTemplateLiteralToken},{"IsImportOrImportEqualsDeclaration",ast.IsImportOrImportEqualsDeclaration},
 }
 for _,predicate:=range predicates{
  row:=predicateRow{Name:predicate.name,TrueKinds:[]int{}}
  for kind:=ast.KindUnknown;kind<ast.KindCount;kind++{if predicate.fn(factory.NewToken(kind)){row.TrueKinds=append(row.TrueKinds,int(kind))}}
  output.Predicates=append(output.Predicates,row)
 }
 for kind:=ast.KindUnknown;kind<ast.KindCount;kind++{
  for flags:=0;flags<4;flags++ {output.Operators=append(output.Operators,precedenceRow{int(kind),flags,int(ast.GetOperatorPrecedence(kind,ast.KindPlusToken,ast.OperatorPrecedenceFlags(flags)))})}
  output.Binary=append(output.Binary,int(ast.GetBinaryOperatorPrecedence(kind)))
 }
 inputs:=[]struct{text string;kind core.ScriptKind}{
  {"const a = new X; const b=new X(); const c=a?.b?.(x)[0]!; const d=x+y*z**2, e=x?y:z; const f=(x as T) satisfies U; const g=tag`a${x}`;",core.ScriptKindTS},
  {"class C { get a(){return 1;} set a(v){} static get a(){return 2;} get ['b'](){return 3;} set ['b'](v){} get [key](){return 4;} @d method(@d x:number) { if(x)return x; function nested(){return 0;} try {for(;;){return 2;}} finally {return 3;} } }",core.ScriptKindTS},
  {"namespace N { export type X=number; interface I{} namespace M{const value=1;} } import X=require('m'); export {X}; function f(this:C,x:number){return x;} const [a,{b}]=list;",core.ScriptKindTS},
  {"const view=<ns:tag a='x'>  {value} <Child/> { /*empty*/ } </ns:tag>;",core.ScriptKindTSX},
  {"const source={a:1,get b(){return 2;},set b(v){}}; module.exports=source; const {a}=require('m').value; source['x'];",core.ScriptKindJS},
  {"/** Documentation */ function f(x) { while(x){if(x) return x;} return 0; }",core.ScriptKindJS},
 }
 for _,input:=range inputs{
  file:=parser.ParseSourceFile(ast.SourceFileParseOptions{FileName:"/helpers.ts",PathKey:"/helpers.ts"},input.text,input.kind)
  nodes:=[]*ast.Node{};ids:=map[*ast.Node]int{}
  var collect func(*ast.Node)bool
  collect=func(node *ast.Node)bool{ids[node]=len(nodes);nodes=append(nodes,node);node.ForEachChild(collect);return false};collect(file.AsNode())
  id:=func(n *ast.Node)int{if n==nil{return -1};value,ok:=ids[n];if !ok{panic("helper returned a node outside ordinary syntax graph")};return value}
  source:=syntaxSource{Text:input.text,ScriptKind:int(input.kind)}
  for _,node:=range nodes{
   row:=syntaxNodeRow{Kind:int(node.Kind),Pos:node.Pos(),End:node.End(),Meaning:int(ast.GetMeaningFromDeclaration(node)),ExpressionPrecedence:-99,AncestorOrQuit:-1,ReturnNodes:[]int{},StoppedReturns:[]int{}}
   row.Predicates=[]bool{ast.IsArrayBindingOrAssignmentElement(node),ast.IsAliasSymbolDeclaration(node),ast.IsCallLikeExpression(node),ast.IsCallLikeOrFunctionLikeExpression(node),ast.IsSourcePhaseImport(node),ast.IsTypeOnlyImportOrExportDeclaration(node),ast.IsValidTypeOnlyAliasUseSite(node),ast.HasInitializer(node),ast.IsFunctionLike(node)&&ast.HasContextSensitiveParameters(node),ast.IsUnterminatedLiteral(node),ast.IsNamedEvaluationSource(node),ast.IsNonWhitespaceToken(node)}
   if ast.IsExpression(node){row.ExpressionPrecedence=int(ast.GetExpressionPrecedence(node));row.Leftmost=[]int{id(ast.GetLeftmostExpression(node,false)),id(ast.GetLeftmostExpression(node,true))}}
   for _,ancestor:=range ast.FindManyAncestors(node,ast.IsSourceFile,ast.IsClassLike,ast.IsFunctionLike,ast.IsSourceFile){row.Ancestors=append(row.Ancestors,id(ancestor))}
   row.AncestorOrQuit=id(ast.FindAncestorOrQuit(node,func(n *ast.Node)ast.FindAncestorResult{if ast.IsBlock(n){return ast.FindAncestorQuit};return ast.ToFindAncestorResult(ast.IsSourceFile(n))}))
   if ast.IsBlock(node){row.ReturnResult=ast.ForEachReturnStatement(node,func(n *ast.Node)bool{row.ReturnNodes=append(row.ReturnNodes,id(n));return false});row.StoppedReturnResult=ast.ForEachReturnStatement(node,func(n *ast.Node)bool{row.StoppedReturns=append(row.StoppedReturns,id(n));return true})}
   if ast.CanHaveModifiers(node){row.ModifierSame=ast.ReplaceModifiers(factory,node,node.Modifiers())==node;mods:=factory.NewModifierList([]*ast.Node{factory.NewToken(ast.KindExportKeyword),factory.NewToken(ast.KindAsyncKeyword)});modified:=ast.ReplaceModifiers(factory,node,mods);for _,m:=range modified.Modifiers().Nodes{row.ModifiedKinds=append(row.ModifiedKinds,int(m.Kind))}}
   if ast.IsAccessor(node){var members []*ast.Node;if ast.IsClassLike(node.Parent){members=node.Parent.Members()}else{members=node.Parent.Properties()};pair:=ast.GetAllAccessorDeclarations(members,node);var getter,setter *ast.Node;if pair.GetAccessor!=nil{getter=pair.GetAccessor.AsNode()};if pair.SetAccessor!=nil{setter=pair.SetAccessor.AsNode()};row.Accessors=[]int{id(pair.FirstAccessor),id(pair.SecondAccessor),id(getter),id(setter)}}
   source.Nodes=append(source.Nodes,row)
  }
  output.Parsed=append(output.Parsed,source)
 }
 return output
}
