// Authored differential witness; source builder/hash/writer bodies copied unchanged
// from pinned checker.go:17764–17828. These are not original upstream test assertions.
package main

import (
 "encoding/binary"
 "encoding/hex"
 "encoding/json"
 "fmt"
 "os"
 "strconv"

 "github.com/zeebo/xxh3"
)

type CacheHashKey xxh3.Uint128

func (k CacheHashKey) IsZero() bool {
	return xxh3.Uint128(k) == xxh3.Uint128{}
}

type keyBuilder struct {
	inlineLength   int
	overflowBuffer []byte
	inlineBuffer   [192]byte
}

func (b *keyBuilder) hash() CacheHashKey {
	if b.overflowBuffer == nil {
		return CacheHashKey(xxh3.Hash128(b.inlineBuffer[:b.inlineLength]))
	}
	return CacheHashKey(xxh3.Hash128(append(b.overflowBuffer, b.inlineBuffer[:b.inlineLength]...)))
}

// spill moves the buffered bytes onto the end of overflowBuffer, so the key's byte
// stream stays overflowBuffer followed by inlineBuffer.
func (b *keyBuilder) spill() {
	b.overflowBuffer = append(b.overflowBuffer, b.inlineBuffer[:b.inlineLength]...)
	b.inlineLength = 0
}

func (b *keyBuilder) writeByte(c byte) {
	if b.inlineLength == len(b.inlineBuffer) {
		b.spill()
	}
	b.inlineBuffer[b.inlineLength] = c
	b.inlineLength++
}

func (b *keyBuilder) writeString(s string) {
	if b.inlineLength+len(s) > len(b.inlineBuffer) {
		b.spill()
		if len(s) > len(b.inlineBuffer) {
			b.overflowBuffer = append(b.overflowBuffer, s...)
			return
		}
	}
	b.inlineLength += copy(b.inlineBuffer[b.inlineLength:], s)
}

func (b *keyBuilder) writeUint32(v uint32) {
	if b.inlineLength+4 > len(b.inlineBuffer) {
		b.spill()
	}
	binary.LittleEndian.PutUint32(b.inlineBuffer[b.inlineLength:], v)
	b.inlineLength += 4
}

func (b *keyBuilder) writeUint64(v uint64) {
	if b.inlineLength+8 > len(b.inlineBuffer) {
		b.spill()
	}
	binary.LittleEndian.PutUint64(b.inlineBuffer[b.inlineLength:], v)
	b.inlineLength += 8
}

func (b *keyBuilder) writeInt(value int) {
	b.writeUint64(uint64(value))
}


type operation struct {
 Kind string `json:"kind"`
 Value string `json:"value,omitempty"`
 Count int `json:"count,omitempty"`
}
type snapshot struct { After int `json:"after"`; Hash string `json:"hash"` }
type vector struct { Name string `json:"name"`; Operations []operation `json:"operations"`; Snapshots []snapshot `json:"snapshots"` }
func hashString(b *keyBuilder) string { h := xxh3.Uint128(b.hash()); return fmt.Sprintf("%016x%016x", h.Hi,h.Lo) }
func pattern(n int) string { data:=make([]byte,n);for i:=range data{data[i]=byte(i*137+17)};return string(data) }
func writeRecursive(b *keyBuilder, depth int) { b.writeByte(byte(depth));b.writeUint32(uint32(depth*17+3));if depth>0{writeRecursive(b,depth-1)};b.writeByte(0xff) }
func main() {
 cases:=[]vector{
  {Name:"empty"},
  {Name:"byte-191",Operations:[]operation{{Kind:"bytes",Count:191}}},
  {Name:"byte-192",Operations:[]operation{{Kind:"bytes",Count:192}}},
  {Name:"byte-193",Operations:[]operation{{Kind:"bytes",Count:193}}},
  {Name:"string-boundaries",Operations:[]operation{{Kind:"raw",Value:hex.EncodeToString([]byte(pattern(191)))},{Kind:"raw",Value:"00ff"},{Kind:"raw",Value:hex.EncodeToString([]byte(pattern(192)))}}},
  {Name:"large-string-spill",Operations:[]operation{{Kind:"raw",Value:hex.EncodeToString([]byte(pattern(193)))},{Kind:"u32",Value:"ffffffff"},{Kind:"raw",Value:hex.EncodeToString([]byte(pattern(1025)))}}},
  {Name:"integer-boundaries",Operations:[]operation{{Kind:"bytes",Count:189},{Kind:"u32",Value:"12345678"},{Kind:"bytes",Count:185},{Kind:"u64",Value:"fedcba9876543210"}}},
  {Name:"mixed-raw-and-integers",Operations:[]operation{{Kind:"raw",Value:"00ffc3a9eda080"},{Kind:"u32",Value:"80000001"},{Kind:"u64",Value:"0123456789abcdef"},{Kind:"int",Value:"-1"},{Kind:"int",Value:"0"}}},
  {Name:"hash-then-append",Operations:[]operation{{Kind:"bytes",Count:193},{Kind:"u64",Value:"ffffffffffffffff"},{Kind:"bytes",Count:400},{Kind:"raw",Value:"00"}}},
  {Name:"recursive-ref-stream",Operations:[]operation{{Kind:"recurse",Count:40},{Kind:"raw",Value:"7461696c"}}},
 }
 for i:=range cases {
  b:=keyBuilder{};cases[i].Snapshots=append(cases[i].Snapshots,snapshot{After:0,Hash:hashString(&b)})
  for n,op:=range cases[i].Operations{
   switch op.Kind{
   case "bytes":for j:=0;j<op.Count;j++{b.writeByte(byte(j*137+17))}
   case "raw":data,e:=hex.DecodeString(op.Value);if e!=nil{panic(e)};b.writeString(string(data))
   case "u32":v,e:=strconv.ParseUint(op.Value,16,32);if e!=nil{panic(e)};b.writeUint32(uint32(v))
   case "u64":v,e:=strconv.ParseUint(op.Value,16,64);if e!=nil{panic(e)};b.writeUint64(v)
   case "int":v,e:=strconv.ParseInt(op.Value,10,64);if e!=nil{panic(e)};b.writeInt(int(v))
   case "recurse":writeRecursive(&b,op.Count)
   default:panic("Unknown operation")
   }
   value:=hashString(&b);if hashString(&b)!=value{panic("Repeated hash mutated logical stream")};cases[i].Snapshots=append(cases[i].Snapshots,snapshot{After:n+1,Hash:value})
  }
 }
 if e:=json.NewEncoder(os.Stdout).Encode(cases);e!=nil{panic(e)}
}
