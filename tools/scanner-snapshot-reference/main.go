// Authored differential witness. Uses the pinned scanner's public Mark/Rewind
// and CommentDirectives APIs; does not inspect capacity or private state.
package main

import (
 "encoding/json"
 "os"
 "github.com/microsoft/TypeScript/tsc/internal/scanner"
)
type directive struct { Start int `json:"start"`; End int `json:"end"`; Type int32 `json:"type"` }
func observe(s *scanner.Scanner) []directive {
 result:=[]directive{}
 for _,d:=range s.CommentDirectives(){result=append(result,directive{d.Loc.Pos(),d.Loc.End(),int32(d.Kind)})}
 return result
}
func main(){
 s:=scanner.NewScanner()
 source:="// @ts-ignore\na\n// @ts-ignore\nb\n// @ts-ignore\nc\n// @ts-ignore\nd\n"
 alternate:="// @ts-ignore\na\n// @ts-ignore\nb\n// @ts-ignore\nc\n// @ts-expect-error\nd\n"
 s.SetText(source)
 s.Scan();s.Scan();s.Scan()
 shorter:=s.Mark()
 beforeShort:=observe(s)
 s.Scan()
 longer:=s.Mark()
 beforeLong:=observe(s)
 s.SetText(alternate)
 s.Rewind(shorter)
 s.Scan()
 afterAppend:=observe(s)
 s.Rewind(longer)
 afterLong:=observe(s)
 if err:=json.NewEncoder(os.Stdout).Encode(struct{
  ShortBefore []directive `json:"shortBefore"`
  LongBefore []directive `json:"longBefore"`
  AfterReplacement []directive `json:"afterReplacement"`
  RewoundLong []directive `json:"rewoundLong"`
 }{beforeShort,beforeLong,afterAppend,afterLong});err!=nil{panic(err)}
}
