// Scanner oracle against the pinned Microsoft TypeScript Go implementation.
package main

import (
 "bufio"
 "encoding/base64"
 "encoding/json"
 "fmt"
 "os"

 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/diagnostics"
 "github.com/microsoft/TypeScript/tsc/internal/scanner"
)

type Operation struct { Op string `json:"op"`; Argument bool `json:"argument"`; Position int `json:"position"`; Text string `json:"text"` }
type UtilityOperation struct { Op string `json:"op"`; Position int `json:"position"`; Line int `json:"line"`; Character int `json:"character"`; LineStarts []int `json:"lineStarts"`; AllowEdits bool `json:"allowEdits"`; StopAfterLineBreak bool `json:"stopAfterLineBreak"`; StopAtComments bool `json:"stopAtComments"`; InJSDoc bool `json:"inJSDoc"` }
type Comment struct { Kind ast.Kind `json:"kind"`; Start int `json:"start"`; End int `json:"end"`; HasTrailingNewLine bool `json:"hasTrailingNewLine"` }
type UtilityResult struct { Op string `json:"op"`; Number int `json:"number"`; Boolean bool `json:"boolean"`; TextBytesBase64 string `json:"textBytesBase64"`; Comments []Comment `json:"comments"` }
type Request struct { Name string `json:"name"`; Text string `json:"text"`; TextBytesBase64 *string `json:"textBytesBase64"`; SkipTrivia *bool `json:"skipTrivia"`; SkipJSDocLeadingAsterisks bool `json:"skipJSDocLeadingAsterisks"`; LanguageVariant int32 `json:"languageVariant"`; ScriptTarget int32 `json:"scriptTarget"`; Operations []Operation `json:"operations"`; UtilityOperations []UtilityOperation `json:"utilityOperations"` }
type Token struct { Kind ast.Kind `json:"kind"`; FullStart int `json:"fullStart"`; Start int `json:"start"`; End int `json:"end"`; Text string `json:"textBytesBase64"`; Value string `json:"valueBytesBase64"`; Flags ast.TokenFlags `json:"flags"` }
type Diagnostic struct { Code int32 `json:"code"`; Start int `json:"start"`; Length int `json:"length"`; Args []string `json:"args"` }
type Directive struct { Start int `json:"start"`; End int `json:"end"`; Type ast.CommentDirectiveKind `json:"type"` }
type Response struct { Tokens []Token `json:"tokens"`; Diagnostics []Diagnostic `json:"diagnostics"`; CommentDirectives []Directive `json:"commentDirectives"`; UtilityResults []UtilityResult `json:"utilityResults,omitempty"` }

func run(r Request) Response {
 s := scanner.NewScanner()
 text := r.Text
 if r.TextBytesBase64 != nil { b,err := base64.StdEncoding.DecodeString(*r.TextBytesBase64); if err != nil { panic(err) }; text = string(b) }
 s.SetText(text)
 s.SetSkipTrivia(r.SkipTrivia == nil || *r.SkipTrivia)
 s.SetSkipJSDocLeadingAsterisks(r.SkipJSDocLeadingAsterisks)
 s.SetLanguageVariant(core.LanguageVariant(r.LanguageVariant))
 s.SetScriptTarget(core.ScriptTarget(r.ScriptTarget))
 result := Response{Tokens:[]Token{}, Diagnostics:[]Diagnostic{}, CommentDirectives:[]Directive{}}
 s.SetOnError(func(d *diagnostics.Message, start,length int,args ...any) { strings := []string{}; for _,arg := range args { strings=append(strings,fmt.Sprint(arg)) }; result.Diagnostics=append(result.Diagnostics,Diagnostic{d.Code(),start,length,strings}) })
 capture := func() { result.Tokens=append(result.Tokens,Token{s.Token(),s.TokenFullStart(),s.TokenStart(),s.TokenEnd(),base64.StdEncoding.EncodeToString([]byte(s.TokenText())),base64.StdEncoding.EncodeToString([]byte(s.TokenValue())),s.TokenFlags()}) }
 if len(r.Operations)==0 { for i:=0;i<len(text)*3+10;i++ { s.Scan(); capture(); if s.Token()==ast.KindEndOfFile { break }; if i==len(text)*3+9 { panic("Scanner did not reach EOF within progress guard") } } } else {
  var saved scanner.ScannerState
  for _,op := range r.Operations {
   switch op.Op {
   case "Reset": s.Reset(); continue
   case "SetText": s.SetText(op.Text); continue
   case "Mark": saved=s.Mark(); continue
   case "Rewind": s.Rewind(saved); continue
   case "ResetPos": s.ResetPos(op.Position); continue
   case "ResetTokenState": s.ResetTokenState(op.Position); continue
   case "Scan": s.Scan()
   case "ReScanLessThanToken": s.ReScanLessThanToken()
   case "ReScanGreaterThanToken": s.ReScanGreaterThanToken()
   case "ReScanTemplateToken": s.ReScanTemplateToken(op.Argument)
   case "ReScanAsteriskEqualsToken": s.ReScanAsteriskEqualsToken()
   case "ReScanSlashToken": s.ReScanSlashToken(op.Argument)
   case "ReScanJsxToken": s.ReScanJsxToken(op.Argument)
   case "ReScanHashToken": s.ReScanHashToken()
   case "ReScanQuestionToken": s.ReScanQuestionToken()
   case "ScanJsxToken": s.ScanJsxToken()
   case "ScanJsxTokenEx": s.ScanJsxTokenEx(op.Argument)
   case "ScanJsxIdentifier": s.ScanJsxIdentifier()
   case "ScanJsxAttributeValue": s.ScanJsxAttributeValue()
   case "ReScanJsxAttributeValue": s.ReScanJsxAttributeValue()
   case "ScanJSDocCommentTextToken": s.ScanJSDocCommentTextToken(op.Argument)
   case "ScanJSDocToken": s.ScanJSDocToken()
   default: panic("Unknown operation "+op.Op)
   }
   capture()
  }
 }
 for _,op := range r.UtilityOperations {
  item:=UtilityResult{Op:op.Op,Comments:[]Comment{}}
  starts:=make([]core.TextPos,len(op.LineStarts)); for i,v:=range op.LineStarts { starts[i]=core.TextPos(v) }
  switch op.Op {
  case "SkipTrivia": item.Number=scanner.SkipTrivia(text,op.Position)
  case "SkipTriviaEx": item.Number=scanner.SkipTriviaEx(text,op.Position,&scanner.SkipTriviaOptions{StopAfterLineBreak:op.StopAfterLineBreak,StopAtComments:op.StopAtComments,InJSDoc:op.InJSDoc})
  case "GetShebang": item.TextBytesBase64=base64.StdEncoding.EncodeToString([]byte(scanner.GetShebang(text)))
  case "IsValidIdentifier": item.Boolean=scanner.IsValidIdentifier(text)
  case "GetIdentifierToken": item.Number=int(scanner.GetIdentifierToken(text))
  case "IsIdentifierStart": item.Boolean=scanner.IsIdentifierStart(rune(op.Character))
  case "IsIdentifierPart": item.Boolean=scanner.IsIdentifierPart(rune(op.Character))
  case "IsIdentifierPartEx": item.Boolean=scanner.IsIdentifierPartEx(rune(op.Character),core.LanguageVariant(r.LanguageVariant))
  case "ComputeLineOfPosition": item.Number=scanner.ComputeLineOfPosition(starts,op.Position)
  case "ComputePositionOfLineAndByteOffset": item.Number=scanner.ComputePositionOfLineAndByteOffset(starts,op.Line,op.Character)
  case "ComputePositionOfLineAndUTF16Character": item.Number=scanner.ComputePositionOfLineAndUTF16Character(starts,op.Line,core.UTF16Offset(op.Character),text,op.AllowEdits)
  case "GetLeadingCommentRanges","GetTrailingCommentRanges":
   factory:=ast.NewNodeFactory(ast.NodeFactoryHooks{})
   ranges:=scanner.GetLeadingCommentRanges(factory,text,op.Position)
   if op.Op=="GetTrailingCommentRanges" { ranges=scanner.GetTrailingCommentRanges(factory,text,op.Position) }
   for c:=range ranges { item.Comments=append(item.Comments,Comment{c.Kind,c.Pos(),c.End(),c.HasTrailingNewLine}) }
  default: panic("Unknown utility operation "+op.Op)
  }
  result.UtilityResults=append(result.UtilityResults,item)
 }
 for _,d := range s.CommentDirectives() { result.CommentDirectives=append(result.CommentDirectives,Directive{d.Loc.Pos(),d.Loc.End(),d.Kind}) }
 return result
}
func main() {
 in:=bufio.NewScanner(os.Stdin); in.Buffer(make([]byte,65536),32*1024*1024)
 out:=json.NewEncoder(os.Stdout)
 for in.Scan() { var r Request; if err:=json.Unmarshal(in.Bytes(),&r); err!=nil { panic(err) }; if err:=out.Encode(run(r));err!=nil {panic(err)} }
 if err:=in.Err();err!=nil {panic(err)}
}
