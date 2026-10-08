// Authored read-only oracle for foundation helper comparisons, not an upstream test.
// Upstream implementation imported unchanged at fed0bf24149fb1ed36039212648bafdafc1ea10e.
package main

import (
 "encoding/base64"
 "encoding/json"
 "fmt"
 "math"
 "os"
 "sort"

 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/jsnum"
 "github.com/microsoft/TypeScript/tsc/internal/parser"
 "github.com/microsoft/TypeScript/tsc/internal/scanner"
)

type sourceText struct{text string;lines []core.TextPos}
func(s sourceText)Text()string{return s.text}
func(s sourceText)ECMALineMap()[]core.TextPos{return s.lines}

type numberRow struct{Family string;Bits string;Text string;ParsedBits string}
func numberPatterns()[]numberRow{
 rows:=[]numberRow{}
 add:=func(family string,bits uint64){
  n:=jsnum.Number(math.Float64frombits(bits))
  text:=n.String()
  rows=append(rows,numberRow{family,fmt.Sprintf("%016x",bits),text,fmt.Sprintf("%016x",math.Float64bits(float64(jsnum.FromString(text))))})
 }
 // Every representable binary power, and both adjacent values for each sign.
 for exponent:=-1074;exponent<=1023;exponent++{
  n:=math.Ldexp(1,exponent)
  for _,candidate:=range []float64{math.Nextafter(n,0),n,math.Nextafter(n,math.Inf(1))}{
   bits:=math.Float64bits(candidate)
   add("binary-powers-neighbors",bits)
   add("binary-powers-neighbors",bits|(uint64(1)<<63))
  }
 }
 for _,value:=range []float64{0,math.Copysign(0,-1),math.Inf(1),math.Inf(-1),math.NaN(),1e-6,1e21,float64(jsnum.MaxSafeInteger),float64(jsnum.MinSafeInteger)}{
  add("presentation-boundaries",math.Float64bits(value))
  if !math.IsNaN(value)&&!math.IsInf(value,0){add("presentation-boundaries",math.Float64bits(math.Nextafter(value,math.Inf(-1))));add("presentation-boundaries",math.Float64bits(math.Nextafter(value,math.Inf(1))))}
 }
 // Fixed integer recurrence: reproducible bit-pattern samples, not a fuzz campaign.
 bits:=uint64(0x31a56793fac239b0)
 for range 4096 {bits^=bits<<13;bits^=bits>>7;bits^=bits<<17;add("deterministic-bits",bits)}
 return rows
}

type positionRow struct{Pos int;Line int;UTF16 int;ByteOffset int}
type inverseRow struct{Line int;Offset int;UTF16Position int;BytePosition int}
type coordinateRow struct{TextBase64 string;Lines []core.TextPos;UTF16Length int;Forward []positionRow;Inverse []inverseRow}
func coordinateRows()[]coordinateRow{
 texts:=[]string{"","ascii\r\nnext\rfinal\n","α🦀\nβ\u2028x\u2029","\xE2\x82x\r\n\xFFy", "\xED\xA0\x80\n"}
 result:=[]coordinateRow{}
 for _,text:=range texts{
  lines:=core.ComputeECMALineStarts(text)
  source:=sourceText{text,lines}
  row:=coordinateRow{TextBase64:base64.StdEncoding.EncodeToString([]byte(text)),Lines:lines,UTF16Length:int(core.UTF16Len(text))}
  for pos:=0;pos<=len(text);pos++{
   line,units:=scanner.GetECMALineAndUTF16CharacterOfPosition(source,pos)
   byteLine,offset:=scanner.GetECMALineAndByteOffsetOfPosition(source,pos)
   if line!=byteLine{panic("line API disagreement")}
   row.Forward=append(row.Forward,positionRow{pos,line,int(units),offset})
  }
  for line,start:=range lines{
   end:=len(text);if line+1<len(lines){end=int(lines[line+1])}
   length:=int(core.UTF16Len(text[int(start):end]))
   for units:=0;units<=length;units++{
    row.Inverse=append(row.Inverse,inverseRow{line,units,scanner.GetECMAPositionOfLineAndUTF16Character(source,line,core.UTF16Offset(units)),scanner.GetECMAPositionOfLineAndByteOffset(source,line,units)})
   }
  }
  result=append(result,row)
 }
 return result
}

type identifierRow struct{NameBase64 string;Variant int;Valid bool;Keyword int;Intrinsic bool}
func identifierRows()[]identifierRow{
 result:=[]identifierRow{}
 for _,name:=range []string{"","a","A","div","Component","my-widget","Upper-tag","-","α","αβ","α-β","source","sourceValue","class","a:b","a.b","\xFFx","\xED\xA0\x80"}{
  for _,variant:=range []core.LanguageVariant{core.LanguageVariantStandard,core.LanguageVariantJSX}{
   identifier:=ast.NewNodeFactory(ast.NodeFactoryHooks{}).NewIdentifier(name)
   result=append(result,identifierRow{base64.StdEncoding.EncodeToString([]byte(name)),int(variant),scanner.IsIdentifierText(name,variant),int(scanner.IdentifierToKeywordKind(identifier.AsIdentifier())),scanner.IsIntrinsicJsxName(name)})
  }
 }
 return result
}

type jsdocRow struct{Source string;TextBase64 string}
func jsdocRows()[]jsdocRow{
 result:=[]jsdocRow{}
 for _,source:=range []string{"/** α text  */\nconst x=1;","/** Text {@link Name} and {@linkcode Thing} {@linkplain Other text}  */\nconst x=1;","/** ending\u00a0\u200b\ufeff */\nconst x=1;","/**\n * plain\n * {@link Q}\n */\nconst x=1;"}{
  file:=parser.ParseSourceFile(ast.SourceFileParseOptions{FileName:"/input.ts",PathKey:"/input.ts"},source,core.ScriptKindTS)
  doc:=file.Statements.Nodes[0].JSDoc(file)[0].AsJSDoc()
  result=append(result,jsdocRow{source,base64.StdEncoding.EncodeToString([]byte(scanner.GetTextOfJSDocComment(doc.Comment)))})
 }
 return result
}

type tokenStringRow struct{Kind int;Panics bool;Text string}
func tokenStrings()[]tokenStringRow {
 rows:=[]tokenStringRow{}
 for _,kind:=range []ast.Kind{-1,ast.KindCount,ast.KindCount+1,ast.KindTypeLiteral,ast.KindIdentifier,ast.KindSourceKeyword}{
  row:=tokenStringRow{Kind:int(kind)}
  func(){defer func(){if recover()!=nil{row.Panics=true}}();row.Text=scanner.TokenToString(kind)}()
  rows=append(rows,row)
 }
 return rows
}
func main(){
 mode:="helpers";if len(os.Args)>1{mode=os.Args[1]}
 encoder:=json.NewEncoder(os.Stdout)
 if mode=="syntax"{if err:=encoder.Encode(syntaxRows());err!=nil{panic(err)};return}
 if mode=="numbers"{if err:=encoder.Encode(numberPatterns());err!=nil{panic(err)};return}
 keywords:=scanner.GetViableKeywordSuggestions();sort.Strings(keywords)
 output:=struct{Coordinates []coordinateRow;Identifiers []identifierRow;JSDoc []jsdocRow;Keywords []string;TokenStrings []tokenStringRow;Filters []filterRow}{coordinateRows(),identifierRows(),jsdocRows(),keywords,tokenStrings(),filterRows()}
 if err:=encoder.Encode(output);err!=nil{panic(err)}
}
