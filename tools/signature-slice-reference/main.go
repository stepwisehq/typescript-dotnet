// Authored Go slice-boundary witness: public Clip/write/append observations,
// not compiler private-state or allocator growth-capacity parity.
package main
import("encoding/json";"os";"slices")
type token struct{ label string }
func main(){
 a,b,c,d,e:=&token{"a"},&token{"b"},&token{"c"},&token{"d"},&token{"e"}
 backing:=make([]*token,3,5);copy(backing,[]*token{a,b,c})
 calls:=slices.Clip(backing[:2]); constructs:=slices.Clip(backing[2:])
 calls[0]=d;shared:=backing[0]==d
 appended:=append(calls,e);appended[0]=a
 detached:=backing[0]==d&&constructs[0]==c&&len(calls)==2&&len(appended)==3
 internal:=backing[:2];appendInternal:=append(internal,e)
 unclipped:=constructs[0]==e&&len(backing)==3&&len(internal)==2&&len(appendInternal)==3
 var nilValues []*token;empty:=make([]*token,0)
 result:=map[string]bool{"clippedExistingSlotsShare":shared,"clippedAppendDetaches":detached,"unclippedAppendCanUseConstructSlot":unclipped,"clipPreservesNil":slices.Clip(nilValues)==nil,"clipPreservesAllocatedEmpty":slices.Clip(empty)!=nil}
 if err:=json.NewEncoder(os.Stdout).Encode(result);err!=nil{panic(err)}
}
